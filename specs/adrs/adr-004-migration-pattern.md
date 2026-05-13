# ADR-004: Migration Pattern — Per-Controller Big-Bang Within spec2cloud Increments (No In-Process Strangler-Fig)

- **Status:** Accepted (autonomous decision; reversible)
- **Date:** 2026-05-13
- **Phase:** Brownfield Phase A — Assessment
- **Decision-maker:** Orchestrator (autonomous, per user delegation)
- **Project:** ContosoUniversity (`src/ContosoUniversity/`)
- **Related:**
  - ADR-002 (rewrite chosen)
  - ADR-003 (target stack: ASP.NET Core 8 MVC)
  - Rewrite assessment: [specs/assessment/rewrite.md](../assessment/rewrite.md) §6 (strangler-fig evaluation)
  - AGENTS.md §2 Phase 2 increment delivery loop

---

## Context

The `rewrite-assessment` skill mandates explicit evaluation of the strangler-fig pattern as a rewrite execution strategy. Three options exist:

1. **Classic in-process strangler-fig** — run ASP.NET MVC 5 and ASP.NET Core 8 in the same process, with a routing seam (YARP, `Microsoft.AspNetCore.SystemWebAdapters`) that gradually redirects URLs from old to new
2. **Per-controller big-bang within spec2cloud increments** — each Phase 2 increment rewrites one controller (with its views, services, and DI registrations) in a single PR, with the green-baseline regression tests as the safety net
3. **Single big-bang rewrite in one PR** — rewrite everything at once

ContosoUniversity is a single-process monolith with a shared `BaseController`, a single `SchoolContext`, no out-of-process integration boundaries, and ~3,500 lines of C#.

---

## Decision

**Use per-controller big-bang within spec2cloud Phase 2 increments (Option 2).**

Each Phase 2 increment rewrites one controller plus its slice of dependencies (its views, the relevant DI registrations, any service classes scoped to that controller). The Track A green-baseline tests (per ADR-001) act as the regression safety net at every increment boundary.

**Increment ordering** (low coupling and risk first; details and rationale in `specs/assessment/rewrite.md` §6.2):

| # | Scope | Reason |
|---:|---|---|
| 1 | Project scaffold (`Program.cs`, csproj SDK-style, DI plumbing, `appsettings.json`, layout view) + EF Core 8 migration scaffold + auth wiring (ADR-005) | Foundation for every controller; nothing else can compile without it |
| 2 | `HomeController` + static pages (F-005, F-006) + bundling replacement | Smallest, validates the scaffold |
| 3 | `DepartmentsController` (F-004) | Self-contained; concurrency token tests EF Core 8 migration |
| 4 | `StudentsController` (F-001) | Stand-alone; introduces sort/filter/paging template |
| 5 | `CoursesController` (F-002) | Depends on Departments; introduces file-upload pattern |
| 6 | `InstructorsController` (F-003) | Largest; depends on Courses + Office/CourseAssignment |
| 7 | `NotificationsController` + `Channel<Notification>` infrastructure (F-007) | Replaces `Infrastructure/MessageQueue` |
| 8 | Delete `MessageQueueTestController`, replace with `/health` endpoint (F-008) | Cleanup |

---

## Rationale

### Why not classic in-process strangler-fig (Option 1)

| Concern | Verdict |
|---|---|
| `BaseController` is shared by 6 of 7 controllers | A clean split would require duplicating the base class twice (once for the old MVC 5 surface, once for ASP.NET Core 8) and routing requests differently — high complexity for a small codebase |
| Single `SchoolContext` shared across all controllers | The DbContext lifetime models in MVC 5 (per-request from `BaseController`) and ASP.NET Core (DI scoped) are incompatible; sharing across the seam requires a custom factory and potentially shared connection strings |
| Operational complexity (two app pools, shared session/auth, reverse proxy or `SystemWebAdapters` shim) | Justified for 100K+ LOC enterprise apps; **not** justified for 3,500 LOC |
| `Microsoft.AspNetCore.SystemWebAdapters` package availability | Available, but adds a non-trivial dependency tree and constrains which packages can be used in the new code |

The classic strangler-fig pattern shines when an application is too large to rewrite in a single coordinated effort. ContosoUniversity is small enough that an 8-step incremental rewrite is the lighter-weight option.

### Why not single big-bang in one PR (Option 3)

- Skips the iterative-delivery promise of spec2cloud (Phase 2 increments)
- Prevents partial deployment / partial verification
- Concentrates all risk into a single huge PR
- Eliminates the human-gate cadence at PR review

### Why per-controller big-bang within increments (Option 2) wins

- **Each increment ships independently.** PR 2 (HomeController) can deploy and validate before PR 3 (DepartmentsController) starts.
- **Risk is bounded per increment** — if one controller's rewrite goes wrong, only that controller is affected; previous increments stay deployed.
- **Track A green baseline (ADR-001) catches behavioral regressions** at every PR boundary.
- **Database stays the same throughout** — old MVC 5 controllers (still in `src/ContosoUniversity/`) and new .NET 8 controllers (in a new project, e.g., `src/ContosoUniversity.Web/`) point at the same SQL Server schema. No data sync needed.
- **The legacy app keeps running.** Until increment 8 deploys, the old MVC 5 app remains the public-facing system. The new .NET 8 app comes online in a parallel deployment slot. Users are cut over at increment 8 (or per-controller via routing if desired).

This is sometimes called "**per-feature parallel run**" or "**migration via greenfield + cutover**" — distinguished from classic strangler-fig by not requiring an in-process seam.

---

## Alternatives Considered (full list)

### Alternative 1 — Classic in-process strangler-fig with `Microsoft.AspNetCore.SystemWebAdapters`

**Rejected.** Operational complexity (shared session, shared auth, dual hosting model) exceeds the rewrite cost for an app of this size. Documented in detail in `specs/assessment/rewrite.md` §6.1.

### Alternative 2 — YARP-based external strangler-fig (reverse proxy routes per controller)

**Rejected (for now).** A YARP routing layer in front of both apps would allow per-controller cutover without an in-process shim — but it requires an additional deployable component (the YARP proxy). For a small app on a single host, the operational cost outweighs the cutover-flexibility benefit. Could be revisited if a cloud-native path is added to the project later.

### Alternative 3 — Full big-bang single PR

**Rejected.** Violates spec2cloud's iterative-delivery model and concentrates risk.

### Alternative 4 — Branch-by-abstraction (refactor to interfaces first, then swap implementations)

**Rejected.** Branch-by-abstraction is excellent for replacing implementations of a single subsystem (e.g., swapping a payment gateway). It is overkill for a full-stack framework migration where every layer changes.

---

## Consequences

### Positive

- 8 independently deployable increments; each one can be paused or rolled back without affecting the others.
- Track A green baseline catches per-controller regressions at the increment boundary.
- Risk is bounded per increment; risk-adjusted total estimate (`specs/assessment/rewrite.md` §4.1) is 30–35 person-days.
- The legacy app stays operational throughout — no service interruption.
- Database schema is preserved; no data migration window required.

### Negative

- **The two apps must coexist for the duration of the migration** (potentially weeks). This requires:
  - Both apps point at the same SQL Server (acceptable — schema is identical)
  - Both apps may attempt to write to `Notification` table simultaneously during increment 7 — acceptable since `Notification` rows are append-only
  - The `NotificationQueuePath` and other in-process state are NOT shared — increment 7 explicitly addresses this by replacing the queue
- **Cutover at the end (increment 8) requires a coordinated DNS/load-balancer change** — minor operational task.
- **Two CI pipelines temporarily** (one for the legacy app, one for the new project) until cutover.

### Neutral

- The increment ordering is a recommendation, not a hard constraint. The user can reorder during the increment-plan-approval gate.

---

## Reversibility

If during execution the user decides classic in-process strangler-fig is preferable (e.g., because the apps must share session state for some reason), this ADR can be superseded by a new ADR. The cost of switching at increment 4 or later, however, would be high — the new app would need to be re-hosted under the old app's process model. Recommendation: validate this choice at the increment-plan-approval gate before Increment 1 begins.

---

## Sign-off

- [x] Decision-maker (orchestrator, autonomous): **selected on 2026-05-13** per user delegation
- [ ] User review (deferred until Phase A review gate)

Signed pending user review.
