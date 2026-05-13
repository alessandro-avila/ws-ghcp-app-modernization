# FRD: Queue Diagnostic Tools

**Feature ID**: F-007
**Status**: Draft
**Priority**: P3
**Last Updated**: 2026-05-13

## Description

Queue Diagnostic Tools is a developer/operator-facing test page that exercises the in-process queue subsystem (F-006) directly. It surfaces four operations on a single Razor view (`Views/MessageQueueTest/Index.cshtml`) that uses POST forms with a status banner: send a test notification, drain notifications from the default queue, run a basic round-trip smoke test against an ad-hoc queue, and report the depth of every registered queue.

**This feature is the single reason `MessageQueueTestController` exists.** It is the only controller that does **not** inherit from `BaseController` — it inherits directly from `Controller`. As a result, it does not get the `BaseController`-managed `db` field or the `notificationService` field; instead it constructs its own `NotificationService` per request via `new NotificationService()` (and reads its own `User.Identity.Name` rather than the hardcoded `"System"`). It is also the only controller whose state-changing POST endpoints are **not** protected by `[ValidateAntiForgeryToken]`.

The "queue" being exercised is the same in-process `MessageQueueManager` registry that the rest of the application uses; there is no separation between the production queue and a test queue beyond the queue name.

## User Stories

### US-F-007-001: Open the diagnostics dashboard

**As a** Operator/Developer
**I want to** open a single page that gives me buttons for the queue subsystem
**So that** I can verify behavior without writing custom code

**Acceptance Criteria:**
- GIVEN I open `/MessageQueueTest/Index` THEN the page renders with `ViewBag.Message = "Message Queue Test Page"` and four POST forms

### US-F-007-002: Send a test notification

**As a** Operator/Developer
**I want to** push a single synthetic notification onto the default queue
**So that** I can confirm the producer side is working end-to-end with browser-side reception

**Acceptance Criteria:**
- GIVEN I click "Send Test Notification" WHEN the form posts to `/MessageQueueTest/SendTestNotification` THEN a `Notification { EntityType = "Test", EntityId = Guid.NewGuid().ToString(), CreatedBy = User.Identity.Name ?? "TestUser" }` is enqueued onto the default queue (`NotificationQueuePath`)
- GIVEN success THEN the page is re-rendered with `ViewBag.Message = "<success message>"` and `ViewBag.MessageType = "success"`
- GIVEN failure THEN `ViewBag.MessageType = "error"`

### US-F-007-003: Drain notifications

**As a** Operator/Developer
**I want to** pull up to 10 messages off the default queue and see them
**So that** I can verify what the application has produced without using the bell-icon UI

**Acceptance Criteria:**
- GIVEN I click "Receive Notifications" WHEN the form posts to `/MessageQueueTest/ReceiveNotifications` THEN up to 10 notifications are drained (destructive read), bound to `ViewBag.Notifications`, with `ViewBag.Message = "Received N notifications"`
- (NOTE) this competes with the bell-icon UI — both endpoints drain the same queue

### US-F-007-004: Round-trip smoke test on an ad-hoc queue

**As a** Operator/Developer
**I want to** create a transient named queue, push two messages, drain them, delete the queue
**So that** I can validate the queue API end-to-end in isolation

**Acceptance Criteria:**
- GIVEN I click "Test Basic Queue" WHEN the form posts to `/MessageQueueTest/TestBasicQueue` THEN the controller creates a queue named `TestBasicQueue`, sends a string message and an anonymous JSON object, receives both, deletes the queue, and surfaces the round-trip result via `ViewBag.Message` / `ViewBag.MessageType`

### US-F-007-005: Report depth of every queue

**As a** Operator/Developer
**I want to** see the depth of every registered queue in one shot
**So that** I can spot stuck producers/consumers

**Acceptance Criteria:**
- GIVEN I click "Get Queue Status" WHEN the form posts to `/MessageQueueTest/GetQueueStatus` THEN the controller enumerates `MessageQueueManager.GetAllQueueNames()` and reports `IMessageQueue.Count` for each as a multi-line `ViewBag.Message` (or "No queues found"); `ViewBag.MessageType = "info"` on success, `"error"` otherwise

## Functional Requirements

### FR-F-007-001: Diagnostic dashboard view

- **Input**: GET `/MessageQueueTest/Index`
- **Processing**: returns the view with banner text
- **Output**: HTML
- **Error handling**: global only

### FR-F-007-002: Synthetic-notification producer

- **Input**: POST `/MessageQueueTest/SendTestNotification`
- **Processing**: build a `Notification` with synthetic GUID id, send via `NotificationService` to `NotificationQueuePath`
- **Output**: re-rendered Index view with status banner
- **Error handling**: try/catch sets `ViewBag.MessageType = "error"`

### FR-F-007-003: Default-queue drain

- **Input**: POST `/MessageQueueTest/ReceiveNotifications`
- **Processing**: loop calling `NotificationService.ReceiveNotification()` until null or 10 reached
- **Output**: re-rendered Index view with `ViewBag.Notifications: List<Notification>`
- **Error handling**: try/catch sets `ViewBag.MessageType = "error"`

### FR-F-007-004: Ad-hoc queue round-trip

- **Input**: POST `/MessageQueueTest/TestBasicQueue`
- **Processing**: `MessageQueueManager.CreateQueue("TestBasicQueue")`; send string + anonymous object; receive both; `MessageQueueManager.DeleteQueue("TestBasicQueue")`
- **Output**: status banner with the result narrative
- **Error handling**: try/catch sets `ViewBag.MessageType = "error"`

### FR-F-007-005: Queue census

- **Input**: POST `/MessageQueueTest/GetQueueStatus`
- **Processing**: `MessageQueueManager.GetAllQueueNames()` and per-name `Count`
- **Output**: status banner listing each queue and its depth
- **Error handling**: try/catch sets `ViewBag.MessageType = "error"`

## Non-Functional Requirements

### NFR-F-007-001: No CSRF protection

None of the four state-changing POST endpoints carries `[ValidateAntiForgeryToken]`. This is the **biggest CSRF gap** in the application.

### NFR-F-007-002: No authorization

Anonymous reach. The dashboard's "Send Test Notification" button could be triggered cross-origin to flood the queue. The "Receive Notifications" button drains the same queue the bell-icon UI consumes from, so a hostile request could starve the bell-icon UI.

### NFR-F-007-003: Bypass of `BaseController` conventions

This is the only controller that does **not** inherit from `BaseController`. It does not use the centralized DbContext factory or the centralized notification helper, and it is the only place that reads `User.Identity.Name`. This inconsistency is intentional to keep the diagnostic tool standalone, but it means any cross-cutting refactor of `BaseController` will not affect this controller.

### NFR-F-007-004: Production exposure

There is no environment guard — the diagnostic page is exposed in every environment, including production.

## Dependencies

| Dependency | Type | Direction | Description |
|---|---|---|---|
| F-006 Real-Time Notification System | Feature | Sibling | Shares the same in-process queue and the same `NotificationService` façade |
| `MessageQueueManager` | Internal | — | Direct dependency for `CreateQueue`/`DeleteQueue`/`GetAllQueueNames` |
| `IMessageQueue` | Internal | — | Used to call `Count` per queue |
| `NotificationService` | Internal | — | Constructed per request |
| `Notification`, `EntityOperation` | Data | — | Payload + operation enum |
| `NotificationQueuePath` | Config | — | Default-queue name |

---

## Current Implementation (Brownfield Extension)

### Files Involved

| File Path | Role |
|---|---|
| `src/ContosoUniversity/Controllers/MessageQueueTestController.cs` | All five endpoints |
| `src/ContosoUniversity/Views/MessageQueueTest/Index.cshtml` | Single dashboard view with four POST forms |
| `src/ContosoUniversity/Services/NotificationService.cs` | Producer/consumer façade |
| `src/ContosoUniversity/Services/MessageQueueManager.cs` | Direct queue management API |
| `src/ContosoUniversity/Services/IMessageQueue.cs` | Per-queue API |
| `src/ContosoUniversity/README_MessageQueue.md` | Feature documentation (claims MSMQ — out of date) |

### Architecture Pattern

Single-controller diagnostic page that bypasses the application's standard `BaseController` conventions and instantiates its dependencies inline. There is no service abstraction beyond what already exists for the notification subsystem; the controller calls `MessageQueueManager` static-style methods directly.

### Test Coverage

| Test Type | Files | Assertions | Coverage |
|---|---|---|---|
| Unit | — | 0 | 0% |
| Integration | — | 0 | 0% |
| E2E | — | 0 | 0% |

### Known Limitations

- **No CSRF protection** on any state-changing POST.
- **No authorization** — exposed in production.
- "Receive Notifications" competes with the bell-icon UI for the same messages.
- The dashboard does not display the configured `NotificationQueuePath` value, so an operator does not know which queue is being acted on.
- `TestBasicQueue` does not roll back if the test fails partway through — the ad-hoc queue may leak.
- The READMEs that document this controller still describe MSMQ semantics, which the controller cannot deliver.

### Integration Points

| External System | Protocol | Purpose | Config Location |
|---|---|---|---|
| (none — fully in-process) | — | — | — |
| In-process notification queue (F-006) | Method calls into `MessageQueueManager` | Diagnostic operations on the same queue subsystem | `Web.config <appSettings key="NotificationQueuePath">` |
