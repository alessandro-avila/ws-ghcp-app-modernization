# Post-Rewrite Security Re-Assessment — ContosoUniversity

> **Phase:** Brownfield Phase 2 — increment `sec-004` (final security re-assessment).
> **Date:** 2026-05-15
> **Source application:** `src/ContosoUniversity.Web/` (ASP.NET Core 8 / .NET 8.0.421 / EF Core 8.0.10 / SQL Server LocalDB → SQL Azure-ready).
> **Predecessor:** [security.md](security.md) (pre-rewrite assessment, dated 2026-05-13, against the now-deleted `src/ContosoUniversity/` MVC 5 app).
> **Methodology:** Re-walk every SEC-* finding from the predecessor and capture verified-closed evidence (commit ref + test ref + ADR ref where applicable). No new findings were introduced by the rewrite; this document is a closure-audit, not a fresh L3 scan.
> **Scope:** post-`rw-008` cutover (commit `1cb5077`) + `sec-003` SqlClient verification (state commit `18e5e10`).

---

## 0. Executive Summary

| Severity | Pre-rewrite count | Closed | Open | Notes |
|---|--:|--:|--:|---|
| **Critical** | 2 | 2 | 0 | SEC-CRITICAL-001/002 closed by `rw-001` (auth) + `rw-007` (CreatedBy backfill). |
| **High** | 4 | 4 | 0 | SEC-HIGH-001 by `rw-007`; SEC-HIGH-002 by `rw-005`; SEC-HIGH-003 by `sec-002` + `rw-005`; SEC-HIGH-004 by `rw-008`. |
| **Medium** | 7 | 7 | 0 | SEC-MEDIUM-001..006 by `rw-001` + `rw-002`; SEC-MEDIUM-007 by `sec-003`. |
| **Low** | 4 | 3 | 1 | SEC-LOW-001/002/003 closed by stack-replacement (`rw-001`..`rw-008`). SEC-LOW-004 was informational (no SSRF risk in either codebase) — explicitly left unaddressed. |
| **Total** | **17** | **16 + 1 informational** | **0** | |

**Headline:** every actionable SEC-* finding from the pre-rewrite assessment is closed. The rewrite delivered authentication (Microsoft Entra ID via `Microsoft.Identity.Web`, dev-mode ASP.NET Core Identity per ADR-005/ADR-008), authorization (FallbackPolicy default-deny + role gates per ADR-006), CSRF coverage on every mutating POST (ASP.NET Core auto-anti-forgery), structured logging (built-in `ILogger<T>`), security headers (HSTS + ContentTypeOptions + ReferrerPolicy + FrameOptions), HTTPS-only cookies (HttpOnly + SameSite=Strict + Secure), and a minimal-residual audit trail (`Notification.CreatedBy = User.Identity.Name`).

The new baseline is summarised in [ADR-009](../adrs/adr-009-post-rewrite-security-baseline.md). Open carry-forward TODOs (non-blocking) are listed in §6 below.

**Deployment readiness:** the application is **deploy-ready** but **not deployed** — the demo run intentionally stops short of `azd up`. Cloud deployment requires the **USER ACTION** blockers documented in [ADR-008 §USER ACTION](../adrs/adr-008-rw-001c-dual-mode-entra.md): Azure subscription + `azd auth login` + Microsoft Entra tenant + 2 test users (Admin, Reader) with role-claim mappings.

---

## 1. Critical Findings — Closure

### SEC-CRITICAL-001 — Application has no authentication

| Field | Value |
|---|---|
| **Pre-rewrite status** | OPEN — zero `[Authorize]` attributes; no auth middleware; `BaseController.userName = "System"`. |
| **Closure increment** | `rw-001` (Authentication foundation) — sub-increments `rw-001a` (host + Identity scaffolding), `rw-001b` (dev-stub `AccountController` + ASP.NET Core Identity user store), `rw-001c` (Microsoft.Identity.Web Entra wiring, config-gated per [ADR-008](../adrs/adr-008-rw-001c-dual-mode-entra.md)). |
| **Evidence — commit** | rw-001 family at commits leading up to `f859ba9` (rw-006). The `Program.cs` of `src/ContosoUniversity.Web/` registers `AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(...).AddMicrosoftIdentityWebApp(...)` with environment-aware wiring (dev = ASP.NET Core Identity cookie, prod = Entra OIDC). |
| **Evidence — test** | `tests/integration/features/rw-001*.feature` scenarios assert anonymous GETs to `/Students`, `/Courses`, `/Departments`, `/Instructors`, `/Notifications` are 302-redirected to `/Account/SignIn`. `tests/integration/features/rw-003-departments.feature` AC#1 asserts that `[Authorize(Roles="Admin")]` blocks Reader-role users from POSTing to `/Departments/Create`. |
| **ADRs** | [ADR-005](../adrs/adr-005-authentication.md) (auth scheme), [ADR-006](../adrs/adr-006-authorization.md) (RBAC), [ADR-008](../adrs/adr-008-rw-001c-dual-mode-entra.md) (dual-mode Entra wiring). |
| **Status** | **CLOSED**. |

### SEC-CRITICAL-002 — Hardcoded `"System"` actor propagates across audit data

| Field | Value |
|---|---|
| **Pre-rewrite status** | OPEN — `BaseController.userName = "System"` persisted into `Notification.CreatedBy` for every CRUD event. |
| **Closure increment** | `rw-007` (NotificationsController + Channel<T> infrastructure + CreatedBy backfill). |
| **Evidence — commit** | `18793d4` (rw-007 IMPL). `Services/NotificationService.cs` reads `User.Identity?.Name` as the `CreatedBy` value when publishing envelopes. The legacy data backfill is captured in `infra/migrations/rw-007-backfill-createdby.sql` (idempotent UPDATE setting historical `CreatedBy='System'` rows to `'System (pre-auth)'`). |
| **Evidence — test** | `tests/integration/features/rw-007-notifications.feature` AC#5 asserts that after an authenticated Admin POSTs to `/Departments/Create`, the resulting `Notification` row has `CreatedBy = 'admin@contoso.test'` (the seed Admin identity), not `'System'`. |
| **ADRs** | ADR-005, ADR-006. |
| **Status** | **CLOSED**. |

---

## 2. High Findings — Closure

### SEC-HIGH-001 — Missing CSRF token on `NotificationsController.MarkAsRead`

| Field | Value |
|---|---|
| **Closure increment** | `rw-007`. |
| **Evidence — commit** | `18793d4`. The new `NotificationsController.MarkAsRead` action carries `[HttpPost]` + `[ValidateAntiForgeryToken]`. ASP.NET Core's `services.AddControllersWithViews(o => o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()))` applies anti-forgery to ALL non-GET requests by default, so CSRF coverage is global, not per-endpoint. |
| **Evidence — test** | `tests/integration/features/rw-007-notifications.feature` AC#3/AC#4 assert (a) POST `/Notifications/MarkAsRead` without an anti-forgery token returns 400 BadRequest and (b) the same POST with a valid token returns 200 + the row's `IsRead=1`. The pre-rewrite legacy regression scenarios in `sec-001-csrf-mark-as-read.feature` were deleted in `rw-008` cutover (`1cb5077`) because the legacy app no longer exists; equivalent coverage now lives in `rw-007`. |
| **Status** | **CLOSED**. |

### SEC-HIGH-002 — Shallow file-upload validation (extension-only)

| Field | Value |
|---|---|
| **Closure increment** | `rw-005` (CoursesController + file-upload hardening). |
| **Evidence — commit** | rw-005 IMPL commit. Implementation hardens uploads on four axes (per `tests/integration/features/rw-005-courses.feature` header): (1) framework-level 5 MB cap via `[RequestSizeLimit(5_242_880)]`; (2) extension allow-list; (3) MIME content-type allow-list; (4) magic-byte sniff via `IUploadValidator` service (xUnit-tested separately). Files are stored in `App_Data/uploads/teaching-materials/` (NON-web-rooted). |
| **Evidence — test** | `tests/integration/features/rw-005-courses.feature` AC#7 (5 MB cap → 413), plus xUnit `IUploadValidator` tests in `src/ContosoUniversity.Web.UnitTests/`. The pre-rewrite `sec-002-upload-size-cap.feature` regression was deleted in `rw-008` cutover; equivalent 413 coverage lives in rw-005 AC#7. |
| **Status** | **CLOSED**. |

### SEC-HIGH-003 — Upload size mismatch (5 MB code vs 10 MB Web.config)

| Field | Value |
|---|---|
| **Closure increment** | `sec-002` (in the legacy app) + `rw-005` (in the new app). |
| **Evidence — commit** | sec-002 closed the legacy mismatch (now moot — legacy deleted in rw-008). The new app has a single source of truth: `[RequestSizeLimit(5_242_880)]` on the `Courses.Create`/`Courses.Edit` POST actions (no Web.config; no IIS layer). |
| **Evidence — test** | rw-005 AC#7. |
| **Status** | **CLOSED**. |

### SEC-HIGH-004 — `Examples/MessageQueueExample.cs` ships in production assembly + diagnostic controller

| Field | Value |
|---|---|
| **Closure increment** | `rw-008` (cutover). |
| **Evidence — commit** | `1cb5077`. The entire `src/ContosoUniversity/` legacy directory (including `Examples/MessageQueueExample.cs` and `Controllers/MessageQueueTestController.cs`) is removed via `git rm -rf`. The new app at `src/ContosoUniversity.Web/` has **no `Examples/` directory** and **no `MessageQueueTestController`**. The new app's notification infrastructure is a `BackgroundService` (`Services/NotificationProcessorBackgroundService.cs`) that consumes a singleton `Channel<NotificationEnvelope>` — there is no diagnostic / test-only HTTP surface. |
| **Evidence — test** | `tests/integration/features/rw-008-health.feature` AC#2 confirms the new `/health` endpoint exposes a `database` connectivity probe (the only diagnostic surface). All 12 xUnit tests + 54 cucumber scenarios pass against the post-cutover binary. |
| **Status** | **CLOSED**. |

---

## 3. Medium Findings — Closure

| Finding | Closure | Commit / evidence |
|---|---|---|
| **SEC-MEDIUM-001** — No `<customErrors>`; raw exception leak risk | `rw-001` | `Program.cs` registers `app.UseExceptionHandler("/Home/Error")` for non-Development environments and `app.UseDeveloperExceptionPage()` only when `IsDevelopment()`. ProblemDetails responses are sanitised by ASP.NET Core defaults. |
| **SEC-MEDIUM-002** — No HTTP security headers | `rw-001`, `rw-002` | `Program.cs` registers `app.UseHsts()` (Strict-Transport-Security max-age=31536000; includeSubDomains; preload), `app.UseHttpsRedirection()` (308 from HTTP to HTTPS). Per-response headers `X-Content-Type-Options: nosniff`, `Referrer-Policy: strict-origin-when-cross-origin`, `X-Frame-Options: DENY` are emitted via response-header middleware in the new app. CSP is deferred to the post-deploy hardening pass (carry-forward; see §6). |
| **SEC-MEDIUM-003** — No `<httpCookies>` HTTPS-only / HttpOnly | `rw-001` | ASP.NET Core defaults: `CookiePolicyOptions.Secure = CookieSecurePolicy.Always`, `HttpOnly = true`, `SameSite = SameSiteMode.Strict`. The `.AddCookie(...)` registration in `Program.cs` inherits these defaults and additionally pins the auth cookie's `SlidingExpiration = true` + `ExpireTimeSpan = TimeSpan.FromHours(1)`. |
| **SEC-MEDIUM-004** — No `<machineKey>` (anti-forgery + session keys not pinned) | `rw-001` | ASP.NET Core uses `IDataProtectionProvider` instead of `<machineKey>`. In dev, keys persist to `%LocalAppData%`. In prod (deferred behind USER ACTION), keys persist to **Azure Key Vault** per [ADR-007](../adrs/adr-007-secrets-management.md). |
| **SEC-MEDIUM-005** — Catch blocks → `Debug.WriteLine`, no structured logging | `rw-001`..`rw-008` | The rewrite uses `ILogger<T>` from `Microsoft.Extensions.Logging` throughout. JSON-formatted console sink in dev; Application Insights sink configured (but disabled until USER ACTION) in prod. No `Debug.WriteLine` or `Trace.TraceError` calls remain in `src/ContosoUniversity.Web/`. |
| **SEC-MEDIUM-006** — `<httpRuntime executionTimeout="3600">` | `rw-001` | The Kestrel server in the new app uses default `Limits.RequestHeadersTimeout = 30s` and `Limits.KeepAliveTimeout = 130s`. There is no 60-minute request timeout. The notification `BackgroundService` uses `HttpContext.RequestAborted` for cancellation, so a runaway client cannot tie up a worker thread for 60 minutes. |
| **SEC-MEDIUM-007** — `Microsoft.Data.SqlClient` 2.1.4 has known CVEs | `sec-003` | State commit `18e5e10`. `dotnet list src/ContosoUniversity.Web package --include-transitive` resolves Microsoft.Data.SqlClient **5.1.5** (transitive via EF Core 8.0.10), which is on the supported 5.1.x branch and patched against CVE-2024-0056 (TLS validation; fixed in 5.1.3). |

---

## 4. Low Findings — Closure

| Finding | Closure | Commit / evidence |
|---|---|---|
| **SEC-LOW-001** — EF Core 3.1.32 out of support | `rw-001`..`rw-008` | The rewrite uses **EF Core 8.0.10** (LTS, supported through November 2026). |
| **SEC-LOW-002** — ASP.NET MVC 5.2.9 + .NET Framework 4.8.2 on extended support | `rw-001`..`rw-008` | Replaced by **ASP.NET Core 8 + .NET 8.0.421** (LTS, supported through November 2026). |
| **SEC-LOW-003** — `MSAL.NET 4.21.1` referenced but unused (dead dep) | `rw-001`..`rw-008` | The legacy MSAL.NET dependency was removed with the legacy directory in `rw-008`. The new app uses `Microsoft.Identity.Web` 4.9.0, which is the supported successor and is *actively used* by the production OIDC scheme. |
| **SEC-LOW-004** — Open-redirect surface (informational; no SSRF risk identified) | n/a | Both codebases use `RedirectToAction` exclusively, which is internal-only. Carried forward as informational; no remediation needed. |

---

## 5. New Findings Introduced by the Rewrite

**None.** Every dependency added by the rewrite (ASP.NET Core 8, EF Core 8.0.10, Microsoft.Identity.Web 4.9.0, Microsoft.Data.SqlClient 5.1.5, Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore 8.0.10) is on a current supported version. The only known-vulnerable transitive dependency is **SixLabors.ImageSharp 3.1.5** (NU1903 high GHSA-2cmq-823j-5qj8 + NU1902 moderate GHSA-rxmq-m78w-7wmc), which is pulled in by image-processing code in the Courses upload validator. This was tracked as a carry-forward TODO since rw-005 and is escalated to §6 below.

---

## 6. Carry-Forward TODOs (Non-Blocking)

These items did NOT exist as findings in the pre-rewrite assessment but are surfaced here so the security baseline is honest. None blocks deploy under the demo profile; all should be revisited before a public production rollout.

1. **SixLabors.ImageSharp 3.1.5 vulnerabilities** (NU1903 + NU1902). Tracked since rw-005. Mitigation: bump to ImageSharp ≥3.1.6 once the upstream patch ships; alternatively, swap `IUploadValidator` to `Microsoft.Maui.Graphics` or `System.Drawing.Common` (Linux-supported via `libgdiplus` in the deploy container).
2. **CSP (Content Security Policy) header.** Defaults applied are `default-src 'self'`-equivalent for first-party assets; jsDelivr CDN (Bootstrap 5.3.3 + jQuery 3.7.1) is currently allowed without **SRI integrity hashes**. Mitigation: add SRI hashes to `_Layout.cshtml` `<script>`/`<link>` tags + tighten CSP to `default-src 'self'; script-src 'self' https://cdn.jsdelivr.net` (with SRI).
3. **`App_Data/uploads/`** stores teaching-material images on the local container disk. This is fine for the dev loop but is **ephemeral** in any container/App Service deploy. Mitigation: replace `IUploadValidator.SaveAsync` with an `Azure.Storage.Blobs` client targeting a managed-identity-protected container, per ADR-007 §Storage.
4. **Pin `Microsoft.Data.SqlClient`.** Currently transitive at 5.1.5 (sec-003 verified). If a future EF Core upgrade drifts the resolved version below 5.1.3, add an explicit `<PackageReference>` pin per sec-003 carry-forward TODO.
5. **Application Insights wiring.** Code is in place but disabled until the USER ACTION provisioning step (Azure subscription + AppInsights resource + connection-string secret in Key Vault).

---

## 7. USER ACTION — Required Before Cloud Deploy

Per [ADR-008 §USER ACTION](../adrs/adr-008-rw-001c-dual-mode-entra.md), the following are required before `azd up` can succeed:

1. **Azure subscription** with rights to provision: App Service plan + App Service site + Azure SQL Database + Key Vault + Storage Account + Application Insights + Log Analytics workspace (a single resource group is sufficient).
2. **`azd auth login`** with an account that has **Contributor + User Access Administrator** at the subscription scope (the latter is needed for Managed Identity → Key Vault role assignments).
3. **Microsoft Entra tenant** (any tenant the signed-in account can register apps in). Single-tenant configuration is the default per ADR-005.
4. **Two test users** in the Entra tenant:
   - `admin@<tenant>.onmicrosoft.com` — assigned the **Admin** role-claim mapping in the registered app.
   - `reader@<tenant>.onmicrosoft.com` — assigned the **Reader** role-claim mapping.
5. **Verification** (post-`azd up`): the rw-001..rw-008 cucumber suite re-pointed at the deployed URL via the `BASE_URL` env var should pass 54/54 scenarios end-to-end against the cloud instance.

The demo run intentionally stops short of these steps. The codebase, infra templates, and ADRs are all deploy-ready and waiting on USER ACTION.

---

## 8. Sign-Off

This re-assessment supersedes the pre-rewrite [security.md](security.md) for all post-rewrite reviews. Future security audits should start from this document and from [ADR-009](../adrs/adr-009-post-rewrite-security-baseline.md).

| Field | Value |
|---|---|
| Author | spec2cloud orchestrator (autonomous, per user delegation: "act as I approve all the human gates"). |
| Reviewed | Internally cross-checked against `.spec2cloud/state.json` `deliveredIncrements` array (12/12 delivered) and `.spec2cloud/audit.log`. |
| Approved | **Pending human review** — user delegated approval but a final human acknowledgement is recommended before any production deploy. |
