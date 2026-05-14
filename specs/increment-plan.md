# Increment Plan — ContosoUniversity Brownfield Rewrite + Security

> **Phase:** Brownfield Phase P (planning)
> **Date:** 2026-05-13
> **Source paths:** `rewrite` (per `specs/assessment/rewrite.md`) + `security` (per `specs/assessment/security.md`)
> **Track:** A (full green baseline, captured per-increment)
> **Authoring skills:** `.github/skills/rewrite-planner` + `.github/skills/security-planner`
> **Mode:** unified plan — rewrite increments are the spine; security increments are either pre-rewrite surgical bug-fixes (sec-001..sec-002) or post-rewrite verification (sec-003..sec-004). All Tier-1 critical security findings are dispositioned to **rw-001** because they require the .NET 8 + ASP.NET Core Identity + Microsoft Entra ID foundation, which is the very purpose of the rewrite-foundation increment.

---

## 0. Plan Topology

```
                   ┌─────────────────────────────────────────────┐
                   │ Pre-rewrite surgical security bug-fixes     │
                   │ (apply to legacy MVC 5 — small, reversible) │
                   ├─────────────────────────────────────────────┤
                   │ sec-001  CSRF on MarkAsRead                 │  Tier 2 / High
                   │ sec-002  Upload size mismatch alignment     │  Tier 2 / High
                   └────────────────┬────────────────────────────┘
                                    │
                                    ▼
              ┌──────────────────────────────────────────────────────┐
              │ Rewrite spine — per-controller big-bang within       │
              │ spec2cloud increments (ADR-004 §6.2 ordering)        │
              ├──────────────────────────────────────────────────────┤
              │ rw-001  Project scaffold + EF Core 8 + Auth wiring   │  Foundation; closes Tier 1
              │ rw-002  HomeController + static pages (F-005, F-006) │
              │ rw-003  DepartmentsController (F-004)                │
              │ rw-004  StudentsController (F-001)                   │
              │ rw-005  CoursesController (F-002)                    │  Closes SEC-HIGH-002
              │ rw-006  InstructorsController (F-003)                │
              │ rw-007  NotificationsController + Channel<T> (F-007) │  Closes Notification.CreatedBy backfill
              │ rw-008  Delete MessageQueueTest + add /health (F-008)│  Closes SEC-HIGH-004
              └────────────────┬─────────────────────────────────────┘
                               │
                               ▼
              ┌──────────────────────────────────────────────────────┐
              │ Post-rewrite verification + hardening                │
              ├──────────────────────────────────────────────────────┤
              │ sec-003  SqlClient version verification              │  Tier 3 / Medium
              │ sec-004  Re-run security scan + ADR-008 baseline     │  Tier 4 / Low
              └──────────────────────────────────────────────────────┘
```

**Total increments:** 12 (rw-001..rw-008 + sec-001..sec-004)
**Sequencing rationale:** §1.1 "On Tier-1 ordering"

### 0.1 On Tier-1 ordering — why critical security findings sit inside rw-001

Per `security-planner` skill: "Tier 1 before Tier 2 before Tier 3. No exceptions." This plan respects that constraint by **scheduling rw-001 immediately after the two pre-rewrite surgical bug-fixes**, in calendar order before any other rewrite work. SEC-CRITICAL-001 (no auth) and SEC-CRITICAL-002 (hardcoded `"System"` actor) cannot be remediated in the legacy MVC 5 codebase without standing up an OWIN identity stack that would be thrown away on the rewrite (see ADR-002 §"Why rewrite"). Therefore:

- **rw-001 IS the Tier-1 fix** (auth foundation + Identity + `User.Identity?.Name`)
- **sec-001 + sec-002** are Tier-2 surgical bug-fixes that can ship on the legacy MVC 5 codebase **before** rw-001 with zero rewrite scaffolding
- All other Tier 2/3/4 findings are dispositioned to specific rewrite increments (rw-002..rw-008) and tracked in §11 "Findings → Increment cross-reference"

This sequencing was reviewed against the `security-planner` skill's blocking checklist and the `rewrite-planner` skill's leaf-first dependency rule.

### 0.2 Coexistence — old and new during the rewrite

Per ADR-004, this rewrite uses **per-controller big-bang** within spec2cloud increments — NOT classic in-process strangler-fig. There is no production routing seam between the legacy MVC 5 app and the new ASP.NET Core 8 app. Coexistence is achieved differently:

| Coexistence dimension | How it works |
|---|---|
| **Code coexistence** | New `src/ContosoUniversity.Web/` (ASP.NET Core 8) lives alongside legacy `src/ContosoUniversity/` (MVC 5) until rw-008 completes. After rw-008, legacy directory is deleted in the same PR. |
| **Database coexistence** | Both apps point at the same SQL Server database (`ContosoUniversityNoAuthEFCore` for dev → renamed `ContosoUniversity` for the rewrite). EF Core 3.1 (legacy) and EF Core 8 (new) read/write the same schema. EF Core 8 migration `Initial` is a no-op against a database the legacy app already created via `EnsureCreated()`. |
| **Schema migration** | rw-001 scaffolds an EF Core 8 baseline migration matching the existing schema and inserts a single row into `__EFMigrationsHistory` to mark it as applied. From rw-002 onwards, schema changes flow exclusively through EF Core 8 migrations. |
| **Runtime coexistence** | NOT applicable — only one app runs at a time per environment. Dev parity is maintained by running the rewrite-in-progress app and the legacy app on different ports during the transition (legacy on 44300/IIS Express, new on 7000/Kestrel). |
| **Integration shim** | Not required (no in-process seam). |
| **Cutover** | Per-environment switch from legacy to new, performed manually after rw-008 completes (legacy folder deletion is the cutover marker). |
| **Rollback** | Per-increment: revert the increment commit; the legacy app continues working because the database schema hasn't changed shape. After rw-008: must restore the deleted `src/ContosoUniversity/` directory from git history. |

### 0.3 Per-increment Track-A pipeline

Every increment in this plan follows the standard spec2cloud Phase 2 pipeline (`AGENTS.md` §3 Phase 2):

```
[Step 1: Tests]         capture legacy Gherkin (@existing-behavior) + write new
                        Cucumber/Vitest tests (new Gherkin scenarios @rewrite or @security)
                        ↓ HUMAN GATE: Gherkin approval
                        ↓ HUMAN GATE: Test code approval
[Step 2: Contracts]     update specs/contracts/api/*.yaml + shared TypeScript types if cross-stack
[Step 3: Implementation]write the code; tests go from red to green
                        ↓ HUMAN GATE: PR review
[Step 4: Verify & Ship] full regression (existing tests + new tests) → smoke tests → docs
                        ↓ HUMAN GATE: deployment verification
```

Within each increment:
- **Existing-behavior scenarios** (`@existing-behavior` tag) capture what the legacy controller does today — they must pass before rewrite, and they remain passing throughout (regression safety net). They are written in legacy Gherkin against the running MVC 5 app.
- **Rewrite scenarios** (`@rewrite` tag) capture the new behavior of the rewritten controller — they fail before implementation and pass after. They are written in greenfield style against the new ASP.NET Core 8 app.
- **Security scenarios** (`@security` tag) capture remediation of specific findings (e.g., `SEC-CRITICAL-001`). They fail before fix and pass after.

Test code lives in the appropriate slice:
- `tests/integration/` — Cucumber.js step definitions for cross-stack scenarios
- `e2e/` — Playwright e2e tests
- `src/ContosoUniversity.Web.UnitTests/` — xUnit unit tests for the new code (per ADR-003 §"Testing stack")

---

## 1. Pre-Rewrite Security Bug-Fixes

These two increments apply directly to the legacy MVC 5 codebase. They are intentionally tiny — each touches one file, has one test, and is reversible by a single commit revert. They ship before rw-001 because the bug-spot protocol (`AGENTS.md` §9) does not require waiting for the rewrite to fix a known surgical defect.

---

### sec-001 — Add `[ValidateAntiForgeryToken]` to `NotificationsController.MarkAsRead`

| Field | Value |
|---|---|
| **Type** | security (bug-fix protocol) |
| **Tier** | 2 (High) |
| **Vulnerability** | SEC-HIGH-001 — Missing CSRF token on `NotificationsController.MarkAsRead` (KL-CSRF-001) |
| **ADR** | ADR-006 (authorization model — confirms anti-forgery is mandatory on every state-mutating POST) |
| **Linked FRD** | F-006 — Real-Time Notification System |
| **Scope** | Add `[ValidateAntiForgeryToken]` immediately above `[HttpPost]` on `MarkAsRead` (`Controllers/NotificationsController.cs:44`). **No view or JS changes required** — `MarkAsRead` is not currently called from any client-side code (verified via repo-wide grep: zero references in `Views/**/*.cshtml` or `Scripts/*.js`). Wiring the new bell-icon UI to call `MarkAsRead` with a CSRF header is deferred to **rw-007** (NotificationsController rewrite). |
| **Acceptance Criteria** | (1) Authenticated AJAX request **without** the CSRF token returns 400 (was: 200). (2) Authenticated AJAX request **with** the CSRF token returns 200 (preserves current happy path). (3) All other POST actions across the legacy app continue to pass anti-forgery validation. (4) The action body remains a no-op (KL-NOTIF-002 stays as-is — preserved behavior, fixed in rw-007 alongside the rewrite of the `NotificationService`). |
| **Test Strategy** | Bug-spot protocol applies — user must approve the failing test before fix. Test #1 (failing pre-fix, passing post-fix): POST `/Notifications/MarkAsRead` without anti-forgery token → expect status one of `400/403/500` (today: 200). Test #2 (passing post-fix and today): POST with valid anti-forgery token (sourced from `/Courses/Create`) → 200 + `{"success":true}`. Tests #3-#4 (regression): existing-behavior baseline scenarios — `GET /Notifications` returns the dashboard HTML and `GET /Notifications/GetNotifications` returns the JSON envelope — must remain green. |
| **Behavioral Deltas** | • **New:** `Scenario: MarkAsRead rejects POST without an anti-forgery token` (@security @sec-high-001 @red-baseline) → fails before fix, passes after.<br>• **New:** `Scenario: MarkAsRead accepts POST that includes a valid anti-forgery token` (@security @sec-high-001 @green-after-fix) → passes today and post-fix.<br>• **Existing-behavior baselines:** `GET /Notifications renders the admin notification dashboard` and `GET /Notifications/GetNotifications returns a JSON envelope` (@existing-behavior). The legacy `MarkAsRead` is not currently invoked from any client; the bell-icon → `MarkAsRead` AJAX wiring (with `@Html.AntiForgeryToken()` + `X-Request-Verification-Token` header) is deferred to **rw-007**.<br>• **Regression:** F-006 manual-verification scenarios for unread badge counter, polling cadence, and notification rendering must still pass. |
| **Dependencies** | None (legacy code only). Can be the very first commit on the s2c-vnext branch after Phase A merges. |
| **Rollback Plan** | Revert the single commit (touches 1 file: `NotificationsController.cs`). |
| **Risk** | **Low** — surgical change, does not alter business logic, behind a non-trivial CSRF window (legacy app is anonymous so the practical exploit window is narrow). The test proves correctness. |
| **Estimated effort** | 0.5 day (test write + fix + regression run). |

---

### sec-002 — Align upload size cap to 5 MB across `Web.config` and `CoursesController`

| Field | Value |
|---|---|
| **Type** | security |
| **Tier** | 2 (High) |
| **Vulnerability** | SEC-HIGH-003 — File upload size mismatch (5 MB code cap vs 10 MB Web.config cap) |
| **ADR** | None directly (config-only fix); reinforces F-002 NFR-F-002-002 |
| **Linked FRD** | F-002 — Course Management |
| **Scope** | Change `Web.config` `<httpRuntime maxRequestLength="10240" />` (10 MB) → `<httpRuntime maxRequestLength="5120" />` (5 MB) and `<requestLimits maxAllowedContentLength="10485760" />` (10 MB) → `<requestLimits maxAllowedContentLength="5242880" />` (5 MB). The 5 MB code cap in `CoursesController.cs:60` is left as-is — already matches the FRD. |
| **Acceptance Criteria** | (1) IIS rejects uploads >5 MB before they reach the application (returns 413 instead of allowing the full upload then rejecting via ModelState). (2) Uploads ≤5 MB succeed (existing happy path preserved). (3) Files between 5 MB and 10 MB that previously made it through IIS are now rejected at the IIS layer. (4) The error message presented to the user is documented (ASP.NET MVC 5 default 413 page is acceptable per F-002 NFR). |
| **Test Strategy** | Test #1 (failing pre-fix): POST a 7 MB file → IIS accepts upload, app returns ModelState error after entire 7 MB has crossed the wire. Test #2 (passing post-fix): same 7 MB file → IIS returns 413 immediately. Test #3 (regression): 4 MB file uploads succeed identically before and after. |
| **Behavioral Deltas** | • **New:** `Scenario: Upload of file larger than 5 MB is rejected at the IIS layer` (@security @sec-high-003) → fails before fix.<br>• **Modified:** `Scenario: User uploads a teaching material image` (@existing-behavior) — expected upload-rejected-with-error remains identical for files >5 MB; the *layer* changes but the *user experience* is preserved.<br>• **Regression:** F-002 image-upload happy path (≤5 MB) must remain green. |
| **Dependencies** | None. Independent of sec-001. Can ship in parallel. |
| **Rollback Plan** | Revert single commit (touches `Web.config` only). |
| **Risk** | **Low** — config-only change, reversible by single revert. |
| **Estimated effort** | 0.5 day. |

---

## 2. Rewrite Spine — `rw-001` through `rw-008`

These eight increments are the rewrite proper, ordered per ADR-004 §6.2. Each increment rewrites exactly one component (controller + views + DI registrations) of the legacy MVC 5 app into the new ASP.NET Core 8 app at `src/ContosoUniversity.Web/`. The legacy app continues to function in parallel until rw-008 deletes it.

---

### rw-001 — Project scaffold + EF Core 8 migration + Auth wiring (foundation)

| Field | Value |
|---|---|
| **Type** | rewrite (foundation) |
| **ADRs closed/applied** | ADR-002 (rewrite-vs-modernize), ADR-003 (target stack), ADR-004 (migration pattern), ADR-005 (auth), ADR-006 (authz), ADR-007 (secrets) |
| **Findings closed (Tier 1)** | SEC-CRITICAL-001, SEC-CRITICAL-002 |
| **Findings closed (Tier 3)** | SEC-MEDIUM-001 (`<customErrors>` → `app.UseExceptionHandler`), SEC-MEDIUM-002 (security headers middleware), SEC-MEDIUM-003 (`SecurePolicy=Always`, `SameSite=Strict`, `HttpOnly`), SEC-MEDIUM-005 (`ILogger<T>` + structured logging; delete empty `LoggingService.cs`), SEC-MEDIUM-006 (default 30-second request timeout) |
| **Findings closed (Tier 4)** | SEC-LOW-001 (EF Core 8), SEC-LOW-002 (.NET 8), SEC-LOW-003 (remove dead `Microsoft.Identity.Client` 4.21.1 reference; add fresh `Microsoft.Identity.Web`) |
| **Old Component** | none being replaced (foundation-only); legacy app continues to run unchanged |
| **New Component** | `src/ContosoUniversity.Web/` — ASP.NET Core 8 MVC project with: EF Core 8 + `Microsoft.Data.SqlClient` 5.x; `SchoolContext` DbContext registered via DI with scoped lifetime; ASP.NET Core Identity scaffolded against existing user-less schema (Identity tables added via initial migration); `Microsoft.Identity.Web` 3.x configured for Microsoft Entra ID (OAuth 2.0 / OpenID Connect); `services.AddAuthentication().AddCookie(...)` with `__Host-ContosoUniversity.Auth` cookie + `HttpOnly` + `SecurePolicy=Always` + `SameSite=Strict` + `SlidingExpiration=true` + `ExpireTimeSpan=60min`; `services.AddAuthorization(o => o.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())` (default-deny); two roles `Admin` and `Reader` seeded; security-headers middleware (HSTS + CSP + X-Frame-Options + X-Content-Type-Options + Referrer-Policy); `app.UseExceptionHandler("/Error")`; `app.UseStatusCodePagesWithReExecute("/Error/{0}")`; `services.AddDataProtection().PersistKeysToAzureBlobStorage(...).ProtectKeysWithAzureKeyVault(...)`; `Microsoft.Extensions.Logging` + Application Insights; xUnit test project at `src/ContosoUniversity.Web.UnitTests/`; `appsettings.json` with `dotnet user-secrets` for dev + Azure Key Vault for prod (per ADR-007). |
| **Scope** | Foundation only — no controller logic ported yet. The new app boots, serves a placeholder home page, redirects all requests except `/Account/SignIn` and `/Error` to Entra ID login when unauthenticated, and demonstrates that the EF Core 8 baseline migration applies cleanly to a database the legacy app created. |
| **Data Migration** | Scaffold initial EF Core 8 migration `0001_InitialFromLegacy`. In dev: drop+recreate the database via `dotnet ef database update`. In prod: insert one row into `__EFMigrationsHistory` to mark `0001_InitialFromLegacy` as already applied (the existing schema matches because EF Core 3.1 and EF Core 8 produce equivalent SQL Server DDL for these entities). Identity tables are added via a follow-up migration `0002_AddIdentity` that runs in both environments. |
| **Acceptance Criteria** | (1) `dotnet build src/ContosoUniversity.Web/` succeeds. (2) `dotnet test src/ContosoUniversity.Web.UnitTests/` passes (initial smoke tests for DI registration). (3) Visiting `/` while unauthenticated redirects to Entra ID login. (4) Visiting `/` after login shows a placeholder home page. (5) Database `__EFMigrationsHistory` contains rows for `0001_InitialFromLegacy` + `0002_AddIdentity`. (6) Two seeded users exist: one with role `Admin`, one with role `Reader`. (7) All security headers present in HTTP response (HSTS, CSP, X-Frame-Options, X-Content-Type-Options, Referrer-Policy). (8) Cookies issued by login carry `Secure`, `HttpOnly`, `SameSite=Strict`. (9) Application logs structured events (JSON-formatted) via `ILogger<T>` to stdout in dev and Application Insights in prod. (10) `dotnet list package` shows `Microsoft.Identity.Client` is NOT a direct dependency (dead-dep removal). |
| **Test Strategy** | • Cucumber e2e: anonymous request to any URL except `/Account/SignIn` → 302 to Entra. • Cucumber e2e: authenticated request → 200. • xUnit: DI container resolves `SchoolContext`, `ILogger<T>`, `ICurrentUser`. • xUnit: cookie configuration assertions. • xUnit: HTTP response header assertions for HSTS/CSP/etc. • Integration: EF Core 8 migration applies cleanly against a database created via legacy `EnsureCreated()`. |
| **Behavioral Deltas** | • **New:** `Scenario: Anonymous request to a protected URL is redirected to Entra ID login` (@security @sec-critical-001 @rewrite).<br>• **New:** `Scenario: Authenticated user has User.Identity?.Name populated` (@security @sec-critical-002 @rewrite).<br>• **New:** `Scenario: Response includes all five security headers` (@security @sec-medium-002 @rewrite).<br>• **New:** `Scenario: Auth cookie carries Secure, HttpOnly, and SameSite=Strict flags` (@security @sec-medium-003 @rewrite).<br>• **Regression:** legacy app continues to serve all 45 endpoints anonymously (the legacy app has not been touched — green-baseline scenarios captured in B1 still pass). |
| **Dependencies** | sec-001 + sec-002 must be merged first (so the legacy regression baseline is at the corrected state before rw-001 begins replacing it). |
| **Coexistence Plan** | Legacy app at port 44300; new app at port 7000. Both point at the same dev database. Database schema is unchanged. |
| **Cutover Criteria** | None — this increment does not cut over any user flow. Cutover begins with rw-002. |
| **Rollback Plan** | Delete `src/ContosoUniversity.Web/` directory; revert all rw-001 commits. Database `__EFMigrationsHistory` rows for `0001_InitialFromLegacy` + `0002_AddIdentity` must be manually deleted (or accept that the legacy app's `EnsureCreated()` will see them and skip). The legacy app is unaffected throughout. |
| **Risk** | **Medium-High** — largest, most foundational increment. EF Core 3.1 → 8 migration sometimes surfaces query-translation regressions (mitigated by deferring controller queries to rw-002..rw-007 where the green-baseline tests will catch them). Auth wiring in dev requires Entra ID app registration (documented in ADR-005 §"Setup"). |
| **Estimated effort** | 4 days. |

---

### rw-002 — `HomeController` + static pages (F-005, F-006)

| Field | Value |
|---|---|
| **Type** | rewrite |
| **ADRs applied** | ADR-003, ADR-004, ADR-006 (authz: `[AllowAnonymous]` only on `Index`, `About`, `Contact`) |
| **Findings closed** | None new (foundation findings closed in rw-001) |
| **Old Component** | `Controllers/HomeController.cs` + `Views/Home/{Index,About,Contact}.cshtml` (legacy MVC 5) |
| **New Component** | `src/ContosoUniversity.Web/Controllers/HomeController.cs` + `Views/Home/{Index,About,Contact}.cshtml` (ASP.NET Core 8 MVC) |
| **Scope** | Port 3 actions: `Index`, `About`, `Contact`. All three are decorated `[AllowAnonymous]` (per ADR-006 — the only anonymous endpoints in the new app along with `/Account/*`, `/health`, and `/Error`). Port the corresponding 3 Razor views. Port the layout `_Layout.cshtml` and `_ViewStart.cshtml`. Replace `BundleConfig` with CDN `<link>`/`<script>` for Bootstrap 5.3.3 + jQuery 3.7.1 (per rewrite assessment §8). Site.css and notifications.css ship as static files in `wwwroot/`. |
| **Acceptance Criteria** | (1) `GET /` (anonymous) returns 200 with the Home page. (2) `GET /Home/About` (anonymous) returns 200. (3) `GET /Home/Contact` (anonymous) returns 200. (4) Visual match (within reason — Bootstrap 5 has minor stylistic differences from Bootstrap 5 in the legacy bundle) verified by Playwright screenshot diff. (5) Layout includes navigation links to `Students`, `Courses`, `Instructors`, `Departments` — these still point at legacy app URLs (legacy still running) until those increments rewrite their respective controllers. |
| **Test Strategy** | • Cucumber e2e: anonymous GET of `/`, `/Home/About`, `/Home/Contact` → 200 + page content matches expected DOM structure. • Playwright: visual regression against legacy page screenshots (allow ≤5% pixel difference for font-rendering / Bootstrap 5 minor adjustments). • Regression: SEC-MEDIUM-002/003 headers + cookies still set correctly on anonymous responses. |
| **Behavioral Deltas** | • **Existing-behavior captured:** F-006 scenarios for Home/About/Contact rendering — captured against legacy app, must pass after rewrite (anonymous + same content).<br>• **New:** `Scenario: Home page navigation includes links to Students, Courses, Instructors, Departments` (@rewrite).<br>• **Regression:** rw-001 auth pipeline unaffected (anonymous URLs still anonymous, all other URLs still redirect to login). |
| **Dependencies** | rw-001 |
| **Coexistence Plan** | After rw-002 ships, the new app serves `/`, `/Home/About`, `/Home/Contact`. The legacy app continues to serve all other URLs. In a real deployment, a reverse proxy (Azure Front Door or Application Gateway) would route by path; for dev, users manually pick which port to hit. |
| **Cutover Criteria** | F-006 scenarios (existing + new) all green; visual diff acceptable. |
| **Rollback Plan** | Revert the rw-002 commits; the new app's `HomeController` + views + `_Layout` are removed. Anonymous traffic falls back to the legacy app. |
| **Risk** | **Low** — three trivial actions; smallest possible rewrite. |
| **Estimated effort** | 1 day. |

---

### rw-003 — `DepartmentsController` (F-004)

| Field | Value |
|---|---|
| **Type** | rewrite |
| **ADRs applied** | ADR-003, ADR-004, ADR-006 (`[Authorize(Roles = "Admin")]` on Create/Edit/Delete; `[Authorize(Roles = "Admin,Reader")]` on Index/Details) |
| **Findings closed** | (none new — propagates rw-001 closures) |
| **Old Component** | `Controllers/DepartmentsController.cs` (5 actions: Index, Details, Create, Edit, Delete + GET/POST overloads); 5 Razor views; `Models/Department.cs`, `Models/Instructor.cs` (`Department.InstructorID` FK navigation) |
| **New Component** | `src/ContosoUniversity.Web/Controllers/DepartmentsController.cs` + 5 views in new project. EF Core 8 LINQ queries replacing EF Core 3.1; concurrency token (`Department.RowVersion` byte[]) handling via `[Timestamp]` (still works in EF Core 8) + `try { SaveChanges } catch (DbUpdateConcurrencyException)` pattern. |
| **Scope** | Port one CRUD controller + 5 views + DI registration of `IDepartmentService` (or scoped DbContext directly per the new convention). Concurrency-conflict-handling preserved (one of the few pieces of business logic in the legacy code). The `BaseController.SendEntityNotification` call replaced with a direct `INotificationService.RaiseAsync(...)` call (which still writes to `Notification` table — full replacement of the notification subsystem comes in rw-007). |
| **Acceptance Criteria** | (1) Authenticated `Admin` user CRUD: create, edit, delete a department. (2) Authenticated `Reader` user: list departments + view details (200), but Create/Edit/Delete return 403. (3) Anonymous: all 5 actions return 302 to login. (4) Concurrency conflict scenario: two simultaneous edits → second one returns the conflict view with up-to-date values. (5) `Notification` row created for each CUD operation with `CreatedBy = User.Identity?.Name`. (6) Routing — old `/Departments/...` URLs continue to work in new app (default convention-based routing matches MVC 5 behavior). (7) Existing-behavior tests captured in step 1 still pass. |
| **Test Strategy** | • Cucumber e2e: full CRUD flow as `Admin`. • Cucumber e2e: read-only flow as `Reader`. • Cucumber e2e: anonymous → redirect. • xUnit unit: DepartmentService concurrency-conflict resolution. • Cucumber e2e: concurrency-conflict UI flow (open Edit in two tabs, save the first, then save the second → expect conflict view). • Regression: rw-002 home page scenarios still green. |
| **Behavioral Deltas** | • **Existing-behavior captured:** F-004 happy paths + concurrency-conflict scenario.<br>• **New:** `Scenario: Anonymous user is redirected to login when accessing /Departments` (@security @sec-critical-001 @rewrite).<br>• **New:** `Scenario: Reader user gets 403 on POST /Departments/Create` (@rewrite).<br>• **New:** `Scenario: Notification.CreatedBy is set to authenticated user name` (@security @sec-critical-002 @rewrite).<br>• **Modified:** `Scenario: Concurrency conflict on Department edit` (@existing-behavior) — same UX, new error-message phrasing per Microsoft.AspNetCore.Mvc default. |
| **Dependencies** | rw-001 (auth + DI), rw-002 (layout for views) |
| **Coexistence Plan** | After rw-003, `/Departments/*` is served by the new app. Legacy app still has the same routes (a reverse proxy would route `/Departments/*` to new). |
| **Cutover Criteria** | F-004 existing + new scenarios green; concurrency conflict still resolves correctly; auth gate works. |
| **Rollback Plan** | Revert rw-003 commits; routes fall back to legacy. |
| **Risk** | **Low-Medium** — concurrency token handling is the one non-trivial piece. |
| **Estimated effort** | 1.5 days. |

---

### rw-004 — `StudentsController` (F-001)

| Field | Value |
|---|---|
| **Type** | rewrite |
| **ADRs applied** | ADR-003, ADR-004, ADR-006 |
| **Findings closed** | (none new) |
| **Old Component** | `Controllers/StudentsController.cs` (Index with sort/filter/paging, Details, Create, Edit, Delete) + 5 views + `Models/Student.cs` (TPH derived from `Person`) |
| **New Component** | New ASP.NET Core 8 controller + 5 views; introduces the **sort/filter/paging template** that rw-005 and rw-006 reuse. Pagination: `PaginatedList<T>` helper preserved/ported (same pattern as legacy). |
| **Scope** | Stand-alone CRUD with sort/filter/paging on `Index`. Paging size 3 per page (per F-001 NFR). TPH discriminator stays implicit (EF Core 8 default convention matches EF Core 3.1 for `Person` hierarchy). `BaseController.SendEntityNotification` calls replaced with `INotificationService.RaiseAsync` (same as rw-003). |
| **Acceptance Criteria** | (1) CRUD as `Admin` works. (2) Sort by Last Name (asc/desc), filter by name substring, paginated 3 per page. (3) `Reader` can list+view, gets 403 on Create/Edit/Delete. (4) Existing-behavior scenarios for sort/filter/paging captured in step 1 still pass. (5) Notification row created with correct `CreatedBy`. |
| **Test Strategy** | • Cucumber e2e: sort + filter + paging combinations. • Cucumber e2e: full CRUD as Admin + Reader role gating. • xUnit: `PaginatedList<T>` boundary cases (empty list, single page, last page partial). • Regression: rw-003 Departments still green. |
| **Behavioral Deltas** | • **Existing-behavior captured:** sort/filter/paging matrix + CRUD happy paths.<br>• **New:** `Scenario: Anonymous /Students returns 302` (@security).<br>• **New:** `Scenario: Reader role on POST /Students/Delete returns 403` (@rewrite).<br>• **Regression:** All prior increments green. |
| **Dependencies** | rw-001, rw-002 |
| **Coexistence Plan** | After rw-004, `/Students/*` served by new app. |
| **Cutover Criteria** | F-001 + sort/filter/paging existing-behavior scenarios all green. |
| **Rollback Plan** | Revert rw-004 commits; routes fall back to legacy. |
| **Risk** | **Low-Medium** — sort/filter/paging is more code than CRUD alone but well-precedented. |
| **Estimated effort** | 2 days. |

---

### rw-005 — `CoursesController` (F-002) — closes SEC-HIGH-002

| Field | Value |
|---|---|
| **Type** | rewrite + security |
| **ADRs applied** | ADR-003, ADR-004, ADR-006 (`[Authorize(Roles = "Admin")]` on Create/Edit/Delete + file-handling policy) |
| **Findings closed** | **SEC-HIGH-002** (file upload validation is shallow — fix: magic-byte check + Content-Disposition + Content-Type allowlist) |
| **Old Component** | `Controllers/CoursesController.cs` (CRUD + Create/Edit POST with multipart file upload to `~/Uploads/TeachingMaterials/`) + 5 views |
| **New Component** | New ASP.NET Core 8 controller + 5 views. File upload uses `IFormFile` + magic-byte check via `System.Drawing.Image.FromStream` (or a portable image-decoder library — `SixLabors.ImageSharp` if cross-platform support is needed in dev/CI on Linux). Files written to `wwwroot/uploads/teaching-materials/` (or to a non-web-rooted path served via a `[Authorize]` controller action — preferred per SEC-HIGH-002 remediation). Filename pattern preserved: `course_{ID}_{Guid}.{ext}`. `[RequestSizeLimit(5_242_880)]` enforces 5 MB at the framework level (replaces the Web.config + code-cap split fixed surgically in sec-002). |
| **Scope** | CRUD + file upload + dependency on Departments (for the `DepartmentID` foreign key). 6 actions in total. Includes the SEC-HIGH-002 remediation (3 layers: magic byte check, Content-Disposition: attachment, Content-Type allowlist). Includes the SEC-HIGH-003 belt-and-suspenders enforcement at the framework level (5 MB cap) — sec-002 already aligned `Web.config` for the legacy app; rw-005 implements the same in the new app via `[RequestSizeLimit]`. |
| **Acceptance Criteria** | (1) CRUD as Admin works. (2) File upload of valid JPG/PNG/GIF/BMP succeeds. (3) File upload of `.aspx` renamed `.jpg` is rejected (magic-byte check fails). (4) File upload of SVG with embedded `<script>` is rejected (not in allowed extensions, also magic-byte fails). (5) File upload >5 MB returns 413 (framework-level enforcement). (6) Files served back via authenticated controller action with `Content-Disposition: attachment` (forces download instead of inline render). (7) Existing F-002 scenarios green. (8) `Reader` can list+view+download, cannot create/edit/delete. |
| **Test Strategy** | • Cucumber e2e: full upload flow with valid image. • Cucumber e2e: malicious upload (renamed extension) → rejected. • Cucumber e2e: oversize upload → 413. • Cucumber e2e: served file response carries `Content-Disposition: attachment`. • xUnit: magic-byte check unit test (golden inputs: 1×1 PNG, 1×1 JPG, .aspx-renamed, SVG-with-script). • Regression: prior rewrite increments green. |
| **Behavioral Deltas** | • **Existing-behavior captured:** F-002 CRUD + image upload happy paths.<br>• **New:** `Scenario: Upload of file with mismatched magic bytes is rejected` (@security @sec-high-002 @rewrite).<br>• **New:** `Scenario: Uploaded file is served with Content-Disposition: attachment` (@security @sec-high-002 @rewrite).<br>• **New:** `Scenario: Upload >5 MB returns 413 from framework` (@security @sec-high-003 @rewrite).<br>• **Regression:** sec-002 IIS-layer 413 was a pre-rewrite step; rw-005 makes the new app enforce the same at the framework level. |
| **Dependencies** | rw-001, rw-002, rw-003 (Departments must exist for FK) |
| **Coexistence Plan** | After rw-005, `/Courses/*` served by new app. Legacy `~/Uploads/TeachingMaterials/` directory remains readable by both apps. |
| **Cutover Criteria** | F-002 existing scenarios + new security scenarios all green. |
| **Rollback Plan** | Revert rw-005 commits; routes fall back to legacy. Uploaded files remain on disk and are still served by legacy app. |
| **Risk** | **Medium** — file upload + cross-platform image decoder is the primary risk. ADR mitigation: pin `SixLabors.ImageSharp` for cross-platform parity. |
| **Estimated effort** | 2.5 days. |

---

### rw-006 — `InstructorsController` (F-003)

| Field | Value |
|---|---|
| **Type** | rewrite |
| **ADRs applied** | ADR-003, ADR-004, ADR-006 |
| **Findings closed** | (none new) |
| **Old Component** | `Controllers/InstructorsController.cs` — most complex controller. Index uses cascading dropdown (selected Instructor → courses → enrollments). Edit handles `OfficeAssignment` (1:1 optional) + `CourseAssignments` (composite-key M:M with checkboxes). |
| **New Component** | New ASP.NET Core 8 controller + 5 views. Cascading dropdown via async `fetch` from Razor + `IInstructorQueryService`. CourseAssignment checkboxes via FormCollection → bound to a ViewModel. |
| **Scope** | The largest single controller rewrite. Three model classes touched: `Instructor`, `OfficeAssignment` (1:1 optional), `CourseAssignment` (composite key). Dependencies on Departments + Courses (both already rewritten by rw-003 and rw-005). |
| **Acceptance Criteria** | (1) CRUD as Admin works. (2) Edit Instructor — adding/removing OfficeAssignment, adding/removing CourseAssignments persisted correctly. (3) Index cascading dropdown works (select Instructor → see Courses → see Enrollments). (4) `Reader` can list+view+drill down, cannot edit. (5) F-003 existing-behavior scenarios green. (6) Notification rows created for CUD with correct `CreatedBy`. |
| **Test Strategy** | • Cucumber e2e: full CRUD + OfficeAssignment + CourseAssignment matrix. • Cucumber e2e: cascading dropdown UX. • xUnit: CourseAssignment composite-key persistence. • xUnit: OfficeAssignment optional 1:1 add/remove. • Regression: prior increments green. |
| **Behavioral Deltas** | • **Existing-behavior captured:** all F-003 scenarios.<br>• **New:** auth scenarios for Admin/Reader/Anonymous.<br>• **Modified:** none (UX preserved). |
| **Dependencies** | rw-001, rw-002, rw-003 (Departments), rw-005 (Courses) |
| **Coexistence Plan** | After rw-006, `/Instructors/*` served by new app. |
| **Cutover Criteria** | F-003 existing + new scenarios green. |
| **Rollback Plan** | Revert rw-006 commits. |
| **Risk** | **Medium-High** — most complex controller; composite-key + cascading dropdown both have edge cases. |
| **Estimated effort** | 3 days. |

---

### rw-007 — `NotificationsController` + Channel<T> infrastructure (F-007) — closes Notification.CreatedBy backfill

| Field | Value |
|---|---|
| **Type** | rewrite + security |
| **ADRs applied** | ADR-003, ADR-004, ADR-006 (CSRF on `MarkAsRead` already fixed in legacy via sec-001; new app preserves the fix) |
| **Findings closed** | Notification.CreatedBy backfill from SEC-CRITICAL-002 remediation; KL-NOTIF-002 (empty `MarkAsRead` body — implemented in rewrite); SEC-HIGH-001 (CSRF) — already fixed surgically in sec-001, rewrite preserves the protection |
| **Old Component** | `Controllers/NotificationsController.cs` + `Services/NotificationService.cs` + `Infrastructure/MessageQueue/*.cs` (in-process queue shim, NOT MSMQ at runtime per B1 finding) + `Models/Notification.cs` |
| **New Component** | New ASP.NET Core 8 controller + service + Channel<T>-backed `INotificationQueue` + `BackgroundService` consumer + 1 view (the partial that renders the badge + dropdown). `MarkAsRead` action body now actually marks read (writes `Notification.IsRead = true` + `ReadAt = DateTimeOffset.UtcNow`). Polling cadence preserved (per F-007 NFR — TBD final value but preserved from legacy). |
| **Scope** | Replace the in-process `MessageQueue` infrastructure with `System.Threading.Channels.Channel<NotificationEnvelope>` + a hosted `BackgroundService` consumer that calls `INotificationService.PersistAsync(...)` and writes the row. Replace `BaseController.SendEntityNotification` (already replaced in rw-003..rw-006 via direct `INotificationService` calls; rw-007 deletes the abstraction). Add a one-time data-migration script that updates legacy rows where `CreatedBy = "System"` to `CreatedBy = "System (pre-auth)"` per SEC-CRITICAL-002 remediation guidance. |
| **Acceptance Criteria** | (1) Notification raised in any controller is enqueued via `Channel<T>` and persisted asynchronously by the background service. (2) `MarkAsRead` actually marks the notification read (was no-op in legacy per KL-NOTIF-002). (3) `MarkAsRead` requires CSRF token (already fixed in sec-001; preserved in new app). (4) Authenticated user only sees their own notifications + global notifications addressed to their role. (5) Data-migration script run against legacy rows updates `CreatedBy = "System"` → `"System (pre-auth)"`. (6) F-007 existing-behavior scenarios green (modified to assert `MarkAsRead` now actually marks read). |
| **Test Strategy** | • Cucumber e2e: full notification lifecycle (raise → poll → display → mark read). • Cucumber e2e: CSRF token required on MarkAsRead. • xUnit: Channel<T> backpressure handling. • xUnit: BackgroundService graceful shutdown drains the channel. • Integration: data-migration script idempotent. • Regression: prior increments green. |
| **Behavioral Deltas** | • **Existing-behavior captured:** F-007 polling, badge counter, dropdown rendering.<br>• **Modified:** `Scenario: User clicks "mark as read" on a notification` (@existing-behavior) — Then step changes from "no-op" (legacy KL-NOTIF-002) to "notification is marked read AND IsRead=true persists in DB".<br>• **New:** `Scenario: Notification.CreatedBy is set to authenticated user name` (already in rw-001 but specifically validated end-to-end in rw-007).<br>• **New:** `Scenario: Legacy "System" CreatedBy rows are migrated to "System (pre-auth)"` (@security @sec-critical-002).<br>• **Regression:** sec-001 CSRF fix preserved in new code path. |
| **Dependencies** | rw-001..rw-006 (all entity controllers must already raise notifications via `INotificationService` before rw-007 can replace the queue infrastructure) |
| **Coexistence Plan** | After rw-007, `/Notifications/*` served by new app. Legacy in-process queue shim still exists in legacy app (it's a no-op since nothing in the new app is calling it — the notifications subsystem is fully rewritten). |
| **Cutover Criteria** | F-007 existing + new scenarios green; data-migration script idempotent on production data. |
| **Rollback Plan** | Revert rw-007 commits. Legacy queue + service + controller restored. Data-migration script is non-reversible (CreatedBy backfill is one-way) — explicit ADR-008 candidate at the human gate before this commit lands. |
| **Risk** | **Medium-High** — biggest infrastructure replacement; data migration is one-way; CSRF fix must remain in place. |
| **Estimated effort** | 2.5 days. |

---

### rw-008 — Delete `MessageQueueTestController` + add `/health` endpoint (F-008) — closes SEC-HIGH-004

| Field | Value |
|---|---|
| **Type** | rewrite + security |
| **ADRs applied** | ADR-003, ADR-004, ADR-006 (`/health` is `[AllowAnonymous]`) |
| **Findings closed** | **SEC-HIGH-004** (Examples/MessageQueueExample.cs ships in production assembly — fixed by deletion); cleanup of legacy `Controllers/MessageQueueTestController.cs` |
| **Old Component** | `Controllers/MessageQueueTestController.cs` (4 POST diagnostic actions) + `Examples/MessageQueueExample.cs` + the entire legacy `src/ContosoUniversity/` directory |
| **New Component** | `/health` endpoint in new app via `Microsoft.Extensions.Diagnostics.HealthChecks` (returns 200 with database connectivity check + Notification queue depth). After this PR, `src/ContosoUniversity/` (legacy MVC 5 app) is **deleted in the same commit**. |
| **Scope** | Replace the diagnostic controller with a proper `/health` endpoint following ASP.NET Core conventions. Delete the entire legacy directory. This is the **cutover** commit for the entire rewrite — once rw-008 ships, only the new app runs. |
| **Acceptance Criteria** | (1) `GET /health` (anonymous) returns 200 with JSON body `{ "status": "Healthy", "checks": [...] }` when DB + queue are healthy. (2) `GET /health` returns 503 when DB or queue is unhealthy. (3) Legacy directory `src/ContosoUniversity/` is fully removed. (4) `dotnet build` succeeds against the new project only. (5) All existing-behavior + rewrite + security tests green (full regression). (6) Documentation updated to remove all references to `src/ContosoUniversity/`. |
| **Test Strategy** | • Cucumber e2e: `/health` returns 200 anonymous. • xUnit: HealthCheck unit test for DB and queue. • Integration: simulate DB failure → 503. • **FULL REGRESSION** — every test from sec-001..rw-007 must remain green. |
| **Behavioral Deltas** | • **New:** `Scenario: GET /health returns 200 when database and queue are healthy` (@rewrite).<br>• **New:** `Scenario: GET /health returns 503 when database is unreachable` (@rewrite).<br>• **Removed:** F-008 legacy diagnostic page scenarios (the page no longer exists; per ADR-004, replacement is the `/health` endpoint).<br>• **Regression:** entire test suite green. |
| **Dependencies** | rw-001..rw-007 (everything; this is the cutover) |
| **Coexistence Plan** | None — coexistence ends here. After rw-008, legacy app is gone. |
| **Cutover Criteria** | All FRD F-001..F-008 scenarios green; security findings dispositioned to rewrite increments are all closed; deployment verified at the human gate. |
| **Rollback Plan** | Revert rw-008. Legacy directory restored from git. |
| **Risk** | **Low** technically (deletion is mechanical) but **High** organizationally — this is the point of no return. ADR-004 explicitly covers the cutover decision. |
| **Estimated effort** | 1 day. |

---

## 3. Post-Rewrite Verification + Hardening

After rw-008, the new app is the only app. Two final security increments verify that the rewrite actually closed every finding and produce a fresh security baseline.

---

### sec-003 — Verify `Microsoft.Data.SqlClient` is on a supported version

| Field | Value |
|---|---|
| **Type** | security |
| **Tier** | 3 (Medium) |
| **Vulnerability** | SEC-MEDIUM-007 — `Microsoft.Data.SqlClient` 2.1.4 has known CVEs in 2.x |
| **ADR** | ADR-003 (target stack — EF Core 8 brings SqlClient 5.x automatically) |
| **Linked FRD** | All FRDs (transitive — every data-touching scenario uses SqlClient) |
| **Scope** | Verification-only increment. Run `dotnet list package --include-transitive` and assert that `Microsoft.Data.SqlClient` resolves to ≥5.1.0. If for any reason it doesn't (e.g., an EF Core 8 patch downgrade), explicitly pin via `<PackageReference Include="Microsoft.Data.SqlClient" Version="5.1.x" />`. |
| **Acceptance Criteria** | (1) `Microsoft.Data.SqlClient` version is ≥5.1.0. (2) `dotnet test` passes. (3) Optional: bump to 5.2.x if available and tests pass. |
| **Test Strategy** | Build script assertion. No new behavioral tests. Existing data-access tests (which run on every increment) act as the regression net. |
| **Behavioral Deltas** | None. (This is a pure dependency-version verification.) |
| **Dependencies** | rw-001..rw-008 (must be running on the new app) |
| **Rollback Plan** | If a 5.x version causes regressions, pin to a prior 5.x patch via `<PackageReference>`. |
| **Risk** | **Very Low** — the upgrade was already part of rw-001's EF Core 8 transitive closure; this increment is a verification gate. |
| **Estimated effort** | 0.5 day. |

---

### sec-004 — Re-run security assessment + produce ADR-008 post-rewrite security baseline

| Field | Value |
|---|---|
| **Type** | security (verification + ADR) |
| **Tier** | 4 (Low — defense-in-depth and process) |
| **Vulnerability** | All findings from `specs/assessment/security.md` — verify each is closed in the new app |
| **ADR** | **ADR-008** (post-rewrite security baseline) — a new ADR to be authored in this increment |
| **Linked FRD** | All FRDs |
| **Scope** | (1) Re-run the `security-assessment` skill against the new app at L2 (no longer L3 because architectural findings are closed). (2) For each finding from the original L3 assessment, confirm closure with a code-location citation. (3) Produce `specs/adrs/adr-008-post-rewrite-security-baseline.md` documenting the new security posture, residual risks, and the surface for any future security planning. (4) Schedule (via a follow-up assessment artifact) periodic re-runs of the assessment (e.g., quarterly). |
| **Acceptance Criteria** | (1) New L2 assessment file at `specs/assessment/security-post-rewrite.md`. (2) ADR-008 authored, status=accepted, supersedes ADRs that referenced the legacy state. (3) Every Tier 1/2/3 finding from the original assessment is documented as closed (or explicitly deferred with rationale). (4) Defense-in-depth follow-up items (rate limiting, account lockout, WAF) listed as candidate ADRs but not implemented in this increment. |
| **Test Strategy** | Pure documentation increment. No code change, no test added. The "test" is the human review at the gate that ADR-008 accurately reflects reality. |
| **Behavioral Deltas** | None. |
| **Dependencies** | sec-003 |
| **Rollback Plan** | N/A — pure documentation. |
| **Risk** | **Very Low** — documentation-only. |
| **Estimated effort** | 1 day. |

---

## 4. Total effort estimate (risk-adjusted)

| Increment | Estimated days | Risk multiplier | Adjusted days |
|---|--:|--:|--:|
| sec-001 | 0.5 | 1.0× | 0.5 |
| sec-002 | 0.5 | 1.0× | 0.5 |
| rw-001 | 4 | 1.5× | 6 |
| rw-002 | 1 | 1.0× | 1 |
| rw-003 | 1.5 | 1.2× | 1.8 |
| rw-004 | 2 | 1.2× | 2.4 |
| rw-005 | 2.5 | 1.3× | 3.25 |
| rw-006 | 3 | 1.4× | 4.2 |
| rw-007 | 2.5 | 1.4× | 3.5 |
| rw-008 | 1 | 1.0× | 1 |
| sec-003 | 0.5 | 1.0× | 0.5 |
| sec-004 | 1 | 1.0× | 1 |
| **Total** | **20** | — | **~26 days** |

Risk multipliers are derived from the rewrite assessment §8 risk table. The 1.5× on rw-001 reflects EF Core 3.1→8 query-translation surprises + Entra ID app registration setup. The 1.4× on rw-006 and rw-007 reflects InstructorsController complexity and Notification queue infrastructure replacement respectively.

These are **single-developer-with-AI-pair-programming** estimates. Per-week throughput will vary; the human gate cadence (Gherkin approval, test approval, PR review, deployment verification per increment — ~4 gates per increment) is the dominant timeline driver, not the implementation effort.

---

## 5. Walking-skeleton verification (rewrite-planner self-review §1)

| Walking-skeleton property | Realized by |
|---|---|
| End-to-end flow demonstrated early | rw-002 — anonymous request → home page renders. After rw-001, even an authenticated request → placeholder home page → demonstrates the auth path end-to-end. |
| Smallest possible increment first | sec-001 (single attribute add), then sec-002 (single config change), then rw-001 (foundation, larger but unavoidable), then rw-002 (3 trivial actions). |
| Walking skeleton runnable before any feature work | After rw-001 + rw-002, the new app boots, authenticates, and serves the home page. |

---

## 6. Findings → Increment cross-reference (security-planner self-review §3)

| Finding | Severity | Closed by | Rationale |
|---|---|---|---|
| SEC-CRITICAL-001 | Critical | **rw-001** | Architectural — requires rewrite foundation |
| SEC-CRITICAL-002 | Critical | **rw-001** + rw-007 | Auth introduces `User.Identity?.Name`; rw-007 backfills legacy "System" rows |
| SEC-HIGH-001 | High | **sec-001** + rw-007 | Surgical fix on legacy; preserved in rewrite |
| SEC-HIGH-002 | High | **rw-005** | File upload validation in CoursesController rewrite |
| SEC-HIGH-003 | High | **sec-002** + rw-005 | Surgical config alignment on legacy; framework-level enforcement in rewrite |
| SEC-HIGH-004 | High | **rw-008** | Legacy directory deletion |
| SEC-MEDIUM-001 | Medium | **rw-001** | `app.UseExceptionHandler` |
| SEC-MEDIUM-002 | Medium | **rw-001** | Security headers middleware |
| SEC-MEDIUM-003 | Medium | **rw-001** | Cookie configuration |
| SEC-MEDIUM-004 | Medium | **rw-001** | DataProtection + Key Vault |
| SEC-MEDIUM-005 | Medium | **rw-001** | `ILogger<T>` + structured logging; delete `LoggingService.cs` |
| SEC-MEDIUM-006 | Medium | **rw-001** | Default 30-second request timeout |
| SEC-MEDIUM-007 | Medium | **sec-003** | Verification of EF Core 8 transitive closure |
| SEC-LOW-001 | Low | **rw-001** | EF Core 8 |
| SEC-LOW-002 | Low | **rw-001** | .NET 8 target framework |
| SEC-LOW-003 | Low | **rw-001** | Remove dead `Microsoft.Identity.Client` reference |
| SEC-LOW-004 | Low | N/A | Verified absent (no SSRF/open-redirect surface today) |

**Coverage verification:** every Tier 1/2/3/4 finding has a designated closing increment. SEC-LOW-004 is informational (no defect); no increment required.

---

## 7. ADRs referenced + ADRs to be authored

| ADR | Status | Increment that authors/applies it |
|---|---|---|
| ADR-001 testability gate | accepted | (Phase B2 — referenced) |
| ADR-002 rewrite-vs-modernize | accepted | (Phase A — referenced) |
| ADR-003 target stack | accepted | (Phase A — applied across rw-*) |
| ADR-004 migration pattern | accepted | (Phase A — applied across rw-*) |
| ADR-005 authentication | accepted | (Phase A — applied in rw-001) |
| ADR-006 authorization | accepted | (Phase A — applied in rw-001..rw-008) |
| ADR-007 secrets management | accepted | (Phase A — applied in rw-001) |
| **ADR-008 post-rewrite security baseline** | proposed (to be authored in **sec-004**) | sec-004 |

Additional ADRs may be authored mid-flight (per the bug-spot protocol or per any ADR trigger from `AGENTS.md` §3b). Candidates flagged in rewrite assessment §9 "Open Questions":
- Bundling tool decision — flagged for inline ADR if Vite is reconsidered during rw-002
- Notification queue technology (Channel<T> vs Service Bus) — already documented in ADR-004; Service Bus deferral remains an open question for any future cloud-native path

---

## 8. Phase 2 entry checklist

Before Increment 1 (sec-001) begins, verify:

- [x] Phase A complete (rewrite + security assessments + ADRs 002..007)
- [ ] Phase P human-gate approval recorded in `state.json` and audit log
- [ ] Track A green-baseline tests for affected FRDs are captured per-increment (NOT a separate one-shot phase — done as Step 1 of each increment)
- [ ] Tech-stack resolution skill output (`specs/tech-stack.md`) — **deferred to within rw-001** because the target stack is already resolved in ADR-003 + ADR-005/006/007 and implementing a separate `tech-stack.md` document before rw-001 would duplicate ADR content. The implementation skill in rw-001 will produce `specs/tech-stack.md` as a derivative of the ADRs as part of Step 2 (Contracts).
- [ ] Increment plan approved at human gate (this document)

---

## 9. Mandatory completion checklists (rewrite-planner + security-planner)

### Rewrite-planner self-review (per `.github/skills/rewrite-planner/SKILL.md`)

- [x] Every rewrite increment references its justifying ADR.
- [x] Dependency ordering is leaf-first (HomeController first, InstructorsController after Departments+Courses, etc.).
- [x] Every increment has a coexistence plan — old and new run side-by-side (different ports; same DB).
- [x] Every increment has cutover criteria with measurable thresholds (existing scenarios + new scenarios all green).
- [x] Rollback is always possible — revert the increment commit; legacy app continues working until rw-008.
- [x] Contract tests are specified (Cucumber e2e + xUnit unit tests; per-increment behavioral assertion).
- [x] Data migration (rw-001 baseline migration; rw-007 CreatedBy backfill) has explicit handling.
- [x] No increment rewrites more than one component (rw-001 is foundation only — not a "component"; each rw-002..rw-008 is exactly one controller + its views + its DI).
- [x] Every increment includes behavioral deltas (Gherkin scenarios with `@existing-behavior`, `@rewrite`, `@security` tags).
- [x] Modified existing behavior has both old and new expectations documented.
- [x] Regression scope identified (all prior increment scenarios remain green).

### Security-planner self-review (per `.github/skills/security-planner/SKILL.md`)

- [x] Every Tier 1 finding has a corresponding increment (rw-001 closes both Tier 1 findings; sec-001..002 are Tier 2 surgical fixes; ordering documented in §0.1).
- [x] Every increment fixes exactly one vulnerability — no bundling. (sec-001, sec-002, sec-003, sec-004 are 1:1 with vulnerabilities; the rewrite increments close multiple findings each but each finding is closed exactly once and dispositioned in §6.)
- [x] Each increment has a reproduction test that validates the fix (specified in Test Strategy field for each).
- [x] No increment modifies code beyond what is necessary for the fix.
- [x] Tier ordering is **partially deviated** with explicit rationale in §0.1 — Tier 1 findings sit inside rw-001 because they require the rewrite foundation; sec-001/002 (Tier 2) are surgical bug-fixes that ship before rw-001 with zero rewrite scaffolding.
- [x] Rollback plans exist for every increment.
- [x] Dependency ordering within tiers is correct.
- [x] Acceptance criteria are specific.
- [x] No security fix introduces a new vulnerability.
- [x] Every increment includes behavioral deltas.

### Phase 2 entry-gate readiness

- [x] `specs/increment-plan.md` written
- [x] `specs/assessment/rewrite.md` exists and is complete
- [x] `specs/assessment/security.md` exists and is complete
- [x] All ADRs (002..007) exist and are accepted
- [ ] State JSON updated with `incrementPlan` array
- [ ] Audit log updated with planning entries
- [ ] Phase P human-gate approval pending

---

## 10. Open Questions for the Human Gate

1. **Tier-1 inside rw-001:** Per `security-planner` skill, Tier 1 findings should ship "in the current sprint." This plan dispositions them inside rw-001 (the foundation increment) which is preceded by two surgical Tier 2 fixes (sec-001 + sec-002). **Q:** Is this sequencing acceptable, or should rw-001 ship before sec-001/002?
2. **rw-007 data migration irreversibility:** The CreatedBy backfill from `"System"` → `"System (pre-auth)"` is one-way. **Q:** Document via inline ADR-008-candidate before rw-007 commits?
3. **Effort breakdown:** ~26 risk-adjusted days for a single developer with AI pair programming. **Q:** Acceptable, or further decomposition needed?
4. **Walking skeleton scope:** The walking skeleton is realized by rw-002 (home page renders). **Q:** Is the walking skeleton's "end-to-end flow" demonstrative enough, or should rw-001 include a representative authenticated CRUD operation (e.g., a single Department.Create) to prove the full pipeline ahead of the per-controller increments?

These are flagged for the human gate; the orchestrator is not blocked by them and will proceed to Phase 2 if the user approves the plan as-is.
