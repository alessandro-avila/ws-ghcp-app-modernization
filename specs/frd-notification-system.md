# FRD: Real-Time Notification System

**Feature ID**: F-006
**Status**: Draft
**Priority**: P1
**Last Updated**: 2026-05-13

## Description

The Notification System produces an in-app activity feed of every entity create / update / delete. Every CRUD action across F-001..F-004 calls `BaseController.SendEntityNotification(entityType, entityId, displayName, operation)` after a successful `SaveChanges`, which delegates to `NotificationService.SendNotification(...)`, which ultimately enqueues a `Notification` object onto an **in-process, in-memory queue** managed by `MessageQueueManager`. The browser polls `/Notifications/GetNotifications` and drains up to 10 notifications per call from the same queue (destructive read). The drained notifications are rendered as a dropdown UI with the bell icon in the site header.

The READMEs (`NOTIFICATION_SYSTEM_README.md`, `README_MessageQueue.md`) describe the system as built on **MSMQ (Microsoft Message Queueing)** with durable persistence, retry semantics, and an "MSMQ shim" abstraction. **The current code does NOT use MSMQ.** `MessageQueueManager` is a pure in-process implementation backed by `ConcurrentQueue<>`-style data structures held in a singleton instance for the lifetime of the IIS worker process. Restarts lose all queued messages. There is no durable persistence, no retry, no dead-letter queue, no cross-process delivery, and no consumer outside this same worker process.

The `MarkAsRead` endpoint is a stub: `NotificationService.MarkAsRead(int id)` exists and the JSON endpoint returns `{ success: true }`, but the underlying operation is a no-op. Notifications have no read/unread state preserved beyond the act of draining them from the queue.

## User Stories

### US-F-006-001: See an activity feed in the site header

**As a** Anonymous Web User
**I want to** see notifications appear in real time as other users create, edit, or delete data
**So that** I know what is happening in the system

**Acceptance Criteria:**
- GIVEN any user (myself or another) successfully creates, edits, or deletes a Student, Course, Instructor, or Department THEN a `Notification` is enqueued with `EntityType`, `EntityId`, `EntityDisplayName`, `Operation`, and `CreatedBy = "System"`
- GIVEN I have the site loaded WHEN the client-side polling timer fires THEN the browser issues a GET to `/Notifications/GetNotifications`
- GIVEN the queue contains messages THEN the response is `{ success: true, notifications: [...], count: N }` and the bell-icon dropdown is updated with up to 10 items
- GIVEN the queue is empty THEN the response is `{ success: true, notifications: [], count: 0 }`
- GIVEN any exception is thrown during the drain THEN the response is `{ success: false, message: "Error retrieving notifications" }` (HTTP 200, JSON body)

### US-F-006-002: Mark a notification as read

**As a** Anonymous Web User
**I want to** dismiss a notification from my view
**So that** the dropdown stays clean

**Acceptance Criteria:**
- GIVEN I click "mark as read" on a notification WHEN the browser POSTs to `/Notifications/MarkAsRead` with `id` THEN the server returns `{ success: true }`
- (CURRENT BEHAVIOR) the server-side handler does **nothing else** — it does not persist a "read" state because the in-memory queue has no concept of one

### US-F-006-003: Visit the admin dashboard

**As a** Anonymous Web User (documented as "Administrator" but not enforced)
**I want to** open a dashboard view of the notification subsystem
**So that** I can manually inspect activity

**Acceptance Criteria:**
- GIVEN I open `/Notifications/Index` THEN a Razor view (`Views/Notifications/Index.cshtml`) is rendered
- (CURRENT BEHAVIOR) the page is described as the "admin notification dashboard" but **no authorization is enforced** — anyone can reach it

## Functional Requirements

### FR-F-006-001: In-process notification publication

- **Input**: `entityType`, `entityId`, `entityDisplayName`, `EntityOperation`, `userName` (always literal `"System"` from `BaseController`)
- **Processing**: `NotificationService.SendNotification(...)` constructs a `Notification` and enqueues it onto the queue named by `Web.config <appSettings key="NotificationQueuePath">` via `MessageQueueManager`
- **Output**: queue depth increments by 1
- **Error handling**: any exception is caught in `BaseController.SendEntityNotification` and logged via `System.Diagnostics.Debug.WriteLine` — **the user-facing CRUD operation is never affected by a notification failure**

### FR-F-006-002: Drain endpoint with bounded batch

- **Input**: GET to `/Notifications/GetNotifications`, no parameters
- **Processing**: loop calling `NotificationService.ReceiveNotification()` (which polls the in-memory queue with a 1-second timeout per call), accumulating non-null results until the count reaches 10 OR a null is returned
- **Output**: JSON `{ success, notifications, count }`
- **Error handling**: any exception is converted to `{ success: false, message: "Error retrieving notifications" }`

### FR-F-006-003: MarkAsRead stub

- **Input**: POST `id`
- **Processing**: `NotificationService.MarkAsRead(id)` — currently a no-op
- **Output**: `{ success: true }` on success, `{ success: false, message: "Error updating notification" }` on exception
- **Error handling**: try/catch around the no-op
- **Note**: this endpoint **does NOT carry `[ValidateAntiForgeryToken]`** — see NFR

### FR-F-006-004: Admin dashboard view

- **Input**: GET `/Notifications/Index`
- **Processing**: returns a Razor view with no model
- **Output**: HTML

## Non-Functional Requirements

### NFR-F-006-001: Non-durable queue

The queue is held in process memory by `MessageQueueManager`. **An IIS recycle, an `iisreset`, or any unhandled exception that crashes the worker process loses every message that has not yet been drained.** The READMEs describe MSMQ-backed durability — that is not the current behavior.

### NFR-F-006-002: Single-process delivery only

There is no transport between processes. A multi-instance deployment (web farm, container scale-out, blue/green) would result in **partition by instance**: a notification produced on one instance is only ever consumed by clients connected to that same instance.

### NFR-F-006-003: Destructive read

`/Notifications/GetNotifications` removes messages from the queue. Two browsers polling the same instance race for the same messages — each notification is delivered to exactly one client.

### NFR-F-006-004: CSRF gap on `MarkAsRead`

`/Notifications/MarkAsRead` is a state-changing POST that **does not validate antiforgery**. This is a documented CSRF gap. Although the underlying handler is currently a no-op, the endpoint is still callable cross-origin.

### NFR-F-006-005: No authorization on the admin dashboard

`/Notifications/Index` and the JSON endpoints are reachable anonymously, in conflict with the README's "Administrator" persona claim.

### NFR-F-006-006: Polling cadence is fixed client-side

The cadence (and any backoff behavior) is JavaScript in `Views/Shared/_Layout.cshtml` and `Content/notifications.css` companions. There is no server-side rate limit.

### NFR-F-006-007: User attribution is hardcoded

`BaseController.SendEntityNotification` always passes `userName = "System"`. No real-user identity is propagated, even though `MessageQueueTestController.SendTestNotification` reads `User.Identity.Name`. This inconsistency is documented in the discrepancies section of `specs/prd.md`.

## Dependencies

| Dependency | Type | Direction | Description |
|---|---|---|---|
| F-001..F-004 (all CRUD features) | Features | Upstream producers | Every successful CRUD operation enqueues a notification |
| F-007 Queue Diagnostic Tools | Feature | Sibling consumer | `MessageQueueTestController` consumes from the same queue |
| `MessageQueueManager` | Internal | — | `Services/MessageQueueManager.cs`, the in-process registry of queues |
| `NotificationService` | Internal | — | `Services/NotificationService.cs` |
| `Notification` model | Data | — | `Models/Notification.cs` |
| `EntityOperation` enum | Data | — | `Models/EntityOperation.cs` |
| `NotificationQueuePath` config | Config | — | `Web.config <appSettings>` |
| ASP.NET MVC 5.2.9 antiforgery infrastructure | Framework | — | Used everywhere except this controller's `MarkAsRead` |

---

## Current Implementation (Brownfield Extension)

### Files Involved

| File Path | Role |
|---|---|
| `src/ContosoUniversity/Controllers/NotificationsController.cs` | HTTP surface (`Index`, `GetNotifications`, `MarkAsRead`) |
| `src/ContosoUniversity/Controllers/BaseController.cs` | Producer helper invoked by every CRUD controller |
| `src/ContosoUniversity/Services/NotificationService.cs` | Enqueue + receive façade |
| `src/ContosoUniversity/Services/MessageQueueManager.cs` | In-process queue registry; backs `IMessageQueue` instances |
| `src/ContosoUniversity/Services/IMessageQueue.cs` | Queue abstraction (Send/Receive/Count/etc.) |
| `src/ContosoUniversity/Models/Notification.cs` | Payload type |
| `src/ContosoUniversity/Models/EntityOperation.cs` | CREATE/UPDATE/DELETE enum |
| `src/ContosoUniversity/Views/Notifications/Index.cshtml` | Admin dashboard view |
| `src/ContosoUniversity/Views/Shared/_Layout.cshtml` | Bell-icon UI + polling JS |
| `src/ContosoUniversity/Content/notifications.css` | Dropdown styling |
| `src/ContosoUniversity/NOTIFICATION_SYSTEM_README.md` | Feature documentation (out of date — claims MSMQ) |
| `src/ContosoUniversity/README_MessageQueue.md` | Feature documentation (out of date — claims MSMQ) |

### Architecture Pattern

Producer/consumer with **in-process** message queue. The producer side is invoked synchronously from controller actions after `SaveChanges` (no domain events bus, no outbox). The consumer side is browser-driven HTTP polling. The transport is `MessageQueueManager` — a process-wide singleton that hands out `IMessageQueue` instances by name; messages are delivered exactly once per process and lost on restart.

### Test Coverage

| Test Type | Files | Assertions | Coverage |
|---|---|---|---|
| Unit | — | 0 | 0% |
| Integration | — | 0 | 0% |
| E2E | — | 0 | 0% |

### Known Limitations

- **README claims do not match code**: NOTIFICATION_SYSTEM_README.md and README_MessageQueue.md describe MSMQ-backed durable, cross-process delivery with retry semantics. The code is in-process, non-durable, no retry. This is the most significant doc/code discrepancy in the codebase.
- `MarkAsRead` is a stub — the in-memory queue has no read/unread concept.
- No `[ValidateAntiForgeryToken]` on `MarkAsRead` — CSRF gap on a state-changing endpoint (currently low impact because the action is a no-op, but the endpoint is callable cross-origin).
- No authorization on `/Notifications/Index`, in conflict with the README's "admin dashboard" framing.
- All notifications attributed to `"System"` from `BaseController`; the only place that reads `User.Identity.Name` is `MessageQueueTestController`.
- Two browser tabs polling the same server race for messages (destructive read).
- Polling, not SSE/WebSockets — wasteful at high frequency and laggy at low frequency.
- No bound on the in-memory queue depth — a producer outpacing the drain can grow memory unboundedly.

### Integration Points

| External System | Protocol | Purpose | Config Location |
|---|---|---|---|
| (none — fully in-process) | — | — | — |
| Browser polling client | HTTP/JSON | Drain notifications | `Views/Shared/_Layout.cshtml` (JavaScript) |
| `Web.config <appSettings>` | Config | `NotificationQueuePath` queue name | `Web.config` |
