# Security Assessment — ContosoUniversity

> **Phase:** Brownfield Phase A — assessment for the user-selected `security` path.
> **Date:** 2026-05-13
> **Source application:** `src/ContosoUniversity/` (ASP.NET MVC 5.2.9 / .NET Framework 4.8.2 / EF Core 3.1.32 / SQL Server LocalDB)
> **Methodology:** Adaptive depth per the `security-assessment` skill. **Auto-escalated to Level 3** because authentication and authorization gaps are architectural (skill rule: "L2 → L3 if auth/authz issues are architectural").
> **Inputs consumed:** all extraction outputs in `specs/docs/`, all 8 FRDs, source code under `src/ContosoUniversity/`, `Web.config`, `packages.config`, `bin/Microsoft.Data.SqlClient.xml` (for version sanity-check).

---

## 0. Executive Summary

| Severity | Count | OWASP categories represented |
|---|--:|---|
| **Critical** | 2 | A01 (Broken Access Control), A07 (Identification & Authentication Failures) |
| **High** | 4 | A01, A03 (Injection), A04 (Insecure Design), A05 (Security Misconfiguration) |
| **Medium** | 7 | A04, A05, A06 (Vulnerable & Outdated Components), A09 (Logging & Monitoring Failures) |
| **Low** | 4 | A05, A06, A09, A10 (SSRF — speculative, see SEC-LOW-004) |
| **Info / dependency CVEs** | See §6 | A06 |
| **Total findings** | **17** | |

**Headline:** the application is **anonymous-by-design** with **no authentication scheme registered** and **zero `[Authorize]` attributes**. Every endpoint — including DELETE actions for Students, Courses, Instructors, Departments — is callable by any unauthenticated visitor. The hardcoded `"System"` actor in `BaseController.cs:28` short-circuits all attribution, making any production deployment unsafe and any audit trail incoherent. **The application MUST NOT be deployed to a production environment in its current state.**

The remaining findings are conventional ASP.NET MVC 5 hardening gaps (missing security headers, missing `<customErrors>`, missing `<httpCookies>` flags, exception-leak risk, file-upload validation gaps, observability deficiencies). None of them is independently catastrophic; in aggregate they confirm the application has had no security review since initial scaffolding.

**Mitigation strategy:** the user-selected `rewrite` path (see `specs/assessment/rewrite.md`) is the natural moment to address the architectural findings. Authentication, authorization, secrets management, security headers, structured logging, and HTTPS/cookie hardening are all mandatory deliverables of the rewrite (see ADR-005, ADR-006, ADR-007). The CSRF gap on `NotificationsController.MarkAsRead` (KL-CSRF-001) is preserved as current behavior in the green baseline and fixed in a Phase 2 increment with a passing test.

---

## 1. Methodology

| Attribute | Value |
|---|---|
| Skill | `.github/skills/security-assessment` |
| Depth | **Level 3** (auto-escalation from L2 — auth/authz architectural) |
| Standards | **OWASP Top 10:2021** |
| Code coverage | All `Controllers/*.cs` (8 files), `Services/*.cs` (3 files), `Infrastructure/*.cs` (6 files), `Data/*.cs` (3 files), `Models/*.cs` (12 files), `Web.config`, `packages.config`, `Global.asax.cs` |
| Configuration coverage | `Web.config`, `App_Start/*.cs`, `Properties/AssemblyInfo.cs` |
| Dependency scan | All 45 NuGet packages from `packages.config`, cross-referenced with public CVE feeds (NVD / GHSA) — see §6 |
| Static taint analysis | Manual — searched for `FromSqlRaw`, `ExecuteSqlRaw`, `Process.Start`, `XmlReader`, `BinaryFormatter`, `SqlCommand` (raw), `Server.Transfer`, `Response.Redirect` (open-redirect), `eval`-equivalents in Razor (`@Html.Raw`) |
| Secrets scan | `git grep`-equivalent for `password`, `secret`, `apikey`, `api-key`, `token`, `Bearer`, `Authorization:`, base64-looking 32+-char strings |
| Auth/authz review | Searched all controllers + filters for `[Authorize]`, `[AllowAnonymous]`, `User.Identity`, `User.IsInRole`, `IPrincipal`, `ClaimsPrincipal`, `OwinStartup` |

### 1.1 Why Level 3

The skill mandates auto-escalation from L2 to L3 when "authentication or authorization issues are architectural." This codebase has:
- **Zero** `[Authorize]` attributes anywhere
- **No** authentication middleware registered (no OWIN startup file, no `Microsoft.Owin.Security.*` packages registered in `Web.config`)
- **No** identity provider configuration
- **A hardcoded `"System"` actor** as a deliberate stand-in for `User.Identity.Name`

Every one of these is an architectural absence, not a configuration mistake. L3 deep-code analysis was performed.

### 1.2 What was NOT covered

- Penetration testing / dynamic analysis — out of scope for this assessment
- Threat modeling (STRIDE/PASTA) — defer to a planning artifact in Phase P if needed
- Compliance mapping (PCI-DSS, HIPAA, GDPR) — no PII/PCI/PHI data is in scope per `specs/prd.md` (educational management with synthetic seed data)
- Infrastructure-as-Code review — no Bicep/Terraform exists yet
- CI/CD security — no pipelines exist yet

---

## 2. Critical Findings

### SEC-CRITICAL-001 — Application has no authentication

| Field | Value |
|---|---|
| **OWASP** | A07:2021 — Identification and Authentication Failures (also A01:2021 — Broken Access Control) |
| **Severity** | **Critical** |
| **Linked extraction findings** | `KL-AUTH-001` (from B1 architecture-mapper); `KL-USER-001` (hardcoded `"System"`) |
| **Linked FRD** | All 8 FRDs (the absence is global) |
| **Code locations** | `Controllers/BaseController.cs:28`, `Global.asax.cs:1-80`, `Web.config:1-120`, `packages.config:1-60` |
| **Evidence** | (a) `BaseController.cs:28` reads `protected string userName = "System"; // No authentication, use System as default user`. (b) Zero matches for `[Authorize]` across the codebase (verified: `grep_search` returned no hits). (c) `MSAL.NET 4.21.1` is in `packages.config` but no `[Authorize]` attribute, no `OwinStartup` file, and no `Microsoft.Identity.Web.UI` reference exists — the dependency is **dead code**. (d) No `<authentication>` or `<authorization>` blocks in `Web.config`. |
| **Impact** | Every action is anonymous: any visitor can `POST /Students/Delete/5`, `POST /Courses/Delete/3`, `POST /Departments/Edit/1`, etc. Audit trail is meaningless because every notification is attributed to `"System"`. Enrollment data, grades (entity `Enrollment.Grade`), instructor records, and department records can be read or destroyed by any visitor. |
| **Remediation** | Introduce **ASP.NET Core Identity + Microsoft Entra ID** during the rewrite. Apply `[Authorize]` at controller level for all data-mutating controllers. Allow `[AllowAnonymous]` only on `HomeController.Index`, `HomeController.Contact`, and the `/health` endpoints. Replace `BaseController.userName` with `User.Identity?.Name`. See **ADR-005** (auth mechanism) and **ADR-006** (authorization model). |
| **Phase 2 disposition** | Foundational increment of the rewrite — no Phase 2 increment may deploy publicly until this is closed. |

### SEC-CRITICAL-002 — Hardcoded actor identity propagates across all audit data

| Field | Value |
|---|---|
| **OWASP** | A09:2021 — Security Logging and Monitoring Failures (also A01:2021 — Broken Access Control) |
| **Severity** | **Critical** |
| **Linked extraction findings** | `KL-USER-001` |
| **Linked FRD** | F-007 (notifications), F-001/F-002/F-003/F-004 (every entity-modifying flow attributes to `"System"`) |
| **Code locations** | `Controllers/BaseController.cs:28` (declaration), `Controllers/BaseController.cs:38-42` (`SendEntityNotification` reads `userName`), `Models/Notification.cs` (`CreatedBy` column), `Services/NotificationService.cs` |
| **Evidence** | `BaseController.cs:28`: `protected string userName = "System";`. `BaseController.cs` Dispose pattern uses this verbatim. The value is then persisted into `Notification.CreatedBy` for every CREATE/UPDATE/DELETE notification raised by `BaseController.SendEntityNotification(...)`. |
| **Impact** | The audit trail (`Notification.CreatedBy` column) is **meaningless** because every notification, from every visitor, is tagged `"System"`. Forensic investigation of any data-mutation event is impossible. This finding is a **direct consequence** of SEC-CRITICAL-001 but is called out separately because it persists into the database, not just the request lifecycle. |
| **Remediation** | After SEC-CRITICAL-001 is closed, replace `BaseController.userName` initialization with `User.Identity?.Name ?? "anonymous"` (where `anonymous` only appears for the explicit `[AllowAnonymous]` endpoints). Backfill historical `Notification.CreatedBy = "System"` rows with a one-time migration that sets them to `"System (pre-auth)"` for clarity. See ADR-005, ADR-006. |
| **Phase 2 disposition** | Same increment as SEC-CRITICAL-001. |

---

## 3. High Findings

### SEC-HIGH-001 — Missing CSRF token on `NotificationsController.MarkAsRead`

| Field | Value |
|---|---|
| **OWASP** | A01:2021 — Broken Access Control (CSRF subset) |
| **Severity** | **High** |
| **Linked extraction findings** | `KL-CSRF-001` |
| **Linked FRD** | F-006 — Real-Time Notification System |
| **Code locations** | `Controllers/NotificationsController.cs:44` |
| **Evidence** | The action is decorated with `[HttpPost]` only — no `[ValidateAntiForgeryToken]`. Verified by reading lines 1–65 of the file. Every other `[HttpPost]` action in the codebase pairs `[HttpPost]` with `[ValidateAntiForgeryToken]` (verified across `Students`, `Courses`, `Instructors`, `Departments` POST actions). This is the only outlier. |
| **Impact** | A malicious page that the victim visits while authenticated (after auth is added) could silently call `POST /Notifications/MarkAsRead` and hide unread notifications. Currently the body of `NotificationService.MarkAsRead` is empty (KL-NOTIF-002), so the practical impact today is zero — but as soon as the body is filled in, this becomes exploitable. |
| **Remediation** | Add `[ValidateAntiForgeryToken]` immediately above the `[HttpPost]` line. Verified test approach: green-baseline test reproduces current (no-token-required) behavior; Phase 2 bug-fix increment adds the attribute and an asserting test. |
| **Phase 2 disposition** | Phase 2 bug-fix increment per the spec2cloud bug-spot protocol; user must approve the bug-reproducing test. |

### SEC-HIGH-002 — File upload validation is shallow (extension-only, no MIME sniff, no magic-byte check)

| Field | Value |
|---|---|
| **OWASP** | A04:2021 — Insecure Design |
| **Severity** | **High** |
| **Linked extraction findings** | `KL-UPLOAD-001` (B2c finding); F-002 NFR-F-002-002 |
| **Linked FRD** | F-002 — Course Management |
| **Code locations** | `Controllers/CoursesController.cs:60-90` (Create POST), `Controllers/CoursesController.cs:155-205` (Edit POST) |
| **Evidence** | The code's only validation is: `var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" }; var ext = Path.GetExtension(teachingMaterialImage.FileName).ToLower(); if (!allowedExtensions.Contains(ext)) { ... }`. There is no `ContentType` check, no magic-byte (file-signature) verification, and no image-decoder validation. Any file renamed to `.jpg` is accepted. |
| **Impact** | An attacker can upload an arbitrary file (a `.aspx`, `.html`, `.svg` containing `<script>`, or a polyglot PHP/HTML file) renamed as `.jpg`. The file is saved into `~/Uploads/TeachingMaterials/` under a server-controlled name (`course_{ID}_{Guid}.{ext}`). Because the directory is web-rooted (in MVC 5 it's served as a static folder by IIS by default unless explicitly blocked), the file is then **directly servable**. SVG with embedded `<script>` would execute in the user's browser if the `Content-Type` is set by the static-file handler. |
| **Remediation** | Three layers: (1) add a magic-byte check using `System.Drawing.Image.FromStream` (will throw on non-image), (2) explicitly set `Content-Disposition: attachment` for the upload directory or move uploads to Azure Blob Storage with a distinct domain (no cookie scope), (3) add `Content-Type` allowlist matching the extension. See ADR-006 for the broader file-handling policy. |
| **Phase 2 disposition** | Phase 2 hardening increment (separate from rewrite — could ship before the rewrite as a bug-fix). |

### SEC-HIGH-003 — File upload size mismatch (5 MB code cap vs 10 MB Web.config cap)

| Field | Value |
|---|---|
| **OWASP** | A05:2021 — Security Misconfiguration |
| **Severity** | **High** |
| **Linked extraction findings** | B2c m3-1 (file-upload size mismatch with FRD) |
| **Linked FRD** | F-002 NFR-F-002-002 (FRD authoritative cap = 5 MB) |
| **Code locations** | `Controllers/CoursesController.cs` (5 MB check: `if (teachingMaterialImage.ContentLength > 5 * 1024 * 1024)`); `Web.config:31` (`<httpRuntime maxRequestLength="10240" />`, in KB → 10 MB); `Web.config` `<requestLimits maxAllowedContentLength="10485760" />` (10 MB) |
| **Evidence** | Web.config `maxRequestLength="10240"` (KB) and `maxAllowedContentLength="10485760"` (bytes) both set the IIS/ASP.NET limit to 10 MB. The application code rejects anything over 5 MB. Files between 5 MB and 10 MB are accepted by IIS, then rejected by app code with a `ModelState` error after the entire upload has crossed the wire. |
| **Impact** | (1) Wasted bandwidth + memory on uploads that will be rejected. (2) Easier DoS — an attacker can upload many 9.9 MB files that consume server memory before being rejected. (3) The mismatch indicates a configuration drift between deployment infrastructure and application code; in a real production environment with a load balancer or reverse proxy, this drift typically leads to inconsistent behavior across environments. |
| **Remediation** | Align both limits to 5 MB. In the rewrite, use `[RequestSizeLimit(5_242_880)]` on the action (no Web.config involved). |
| **Phase 2 disposition** | Resolved during F-002 controller rewrite. |

### SEC-HIGH-004 — `Examples/MessageQueueExample.cs` ships in production assembly

| Field | Value |
|---|---|
| **OWASP** | A05:2021 — Security Misconfiguration |
| **Severity** | **High** |
| **Linked extraction findings** | `KL-EXAMPLES-001` (B1c) |
| **Linked FRD** | F-008 |
| **Code locations** | `Examples/MessageQueueExample.cs` (compiled into `ContosoUniversity.dll`) |
| **Evidence** | The file is referenced by the csproj `<Compile Include="Examples\MessageQueueExample.cs" />`. It's executed only by `MessageQueueTestController` actions, but the type is reachable via reflection from any controller. The file contains demonstration of the in-process queue API and may include exception messages or developer-comment leaks. |
| **Impact** | Sample/demo code ships into production binaries, expanding the attack surface. The file is currently benign (no secrets, no dangerous APIs) but the **pattern** is dangerous — there is no convention to keep example code out of release builds. |
| **Remediation** | (1) Move to a `Samples/` project that is not referenced by the production project. (2) In the rewrite, wrap the entire `MessageQueueTestController` and `MessageQueueExample` in `#if DEBUG` or move to a non-shipped test project. (3) Recommended: delete during the F-008 rewrite (per the rewrite assessment). |
| **Phase 2 disposition** | Resolved by F-008 rewrite (delete `MessageQueueTestController` and replace with `/health` endpoint). |

---

## 4. Medium Findings

### SEC-MEDIUM-001 — No `<customErrors>` configuration; raw exceptions may leak stack traces

| Field | Value |
|---|---|
| **OWASP** | A05:2021 — Security Misconfiguration (also A09 for the logging deficiency) |
| **Severity** | Medium |
| **Code locations** | `Web.config:1-120` (no `<customErrors>` element), `App_Start/FilterConfig.cs` (registers `HandleErrorAttribute` only, with default behavior) |
| **Evidence** | `Web.config` has no `<customErrors mode="On" defaultRedirect="..."/>` configuration. `HandleErrorAttribute` is registered but the default behavior is to show the framework's "Yellow Screen of Death" when `customErrors` is `Off`, and to show `Error.cshtml` when it is `On` or `RemoteOnly`. Without `customErrors`, behavior depends on the IIS/Windows defaults. |
| **Impact** | In a misconfigured environment (e.g., debug build pushed to a server, or `<system.web debug="true">`), an unhandled exception will return the full stack trace to the visitor — including internal type names, file paths, version numbers, and SQL parameters. This can reveal `Microsoft.Data.SqlClient` version, EF Core internals, and deployment paths. |
| **Remediation** | Add `<customErrors mode="On" defaultRedirect="~/Home/Error" />` to Web.config. In the rewrite, use `app.UseExceptionHandler("/Home/Error")` and `app.UseStatusCodePagesWithReExecute("/Home/Error/{0}")`. |
| **Phase 2 disposition** | Resolved during rewrite scaffold (Increment 1 of rewrite plan). |

### SEC-MEDIUM-002 — No HTTP security response headers (HSTS, X-Frame-Options, X-Content-Type-Options, CSP, Referrer-Policy)

| Field | Value |
|---|---|
| **OWASP** | A05:2021 — Security Misconfiguration |
| **Severity** | Medium |
| **Code locations** | `Web.config:1-120` (no `<httpProtocol>/<customHeaders>` section), no middleware/filter setting headers |
| **Evidence** | Web.config has no `<system.webServer><httpProtocol><customHeaders>` block. No `[Filter]` adds security headers. No global filter exists in `App_Start/FilterConfig.cs` for this. |
| **Impact** | Browsers receive no security guidance — the application is clickjackable (no `X-Frame-Options`/`frame-ancestors`), MIME-sniffable (no `X-Content-Type-Options: nosniff`), HTTP-downgradable (no HSTS), and unrestricted in script sources (no CSP). |
| **Remediation** | Add the following headers via `<httpProtocol><customHeaders>` in Web.config: `Strict-Transport-Security: max-age=31536000; includeSubDomains` (after HTTPS is mandatory), `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: strict-origin-when-cross-origin`, `Content-Security-Policy` (start with `default-src 'self'; img-src 'self' data:; style-src 'self' 'unsafe-inline'; script-src 'self'`). In the rewrite, use `app.UseHsts()` + `app.Use((ctx, next) => { ctx.Response.Headers.Append(...); return next(); })` or the `NetEscapades.AspNetCore.SecurityHeaders` package. |
| **Phase 2 disposition** | Resolved during rewrite scaffold (Increment 1). |

### SEC-MEDIUM-003 — No `<httpCookies>` hardening (cookies not forced to HTTPS-only or HttpOnly)

| Field | Value |
|---|---|
| **OWASP** | A05:2021 — Security Misconfiguration |
| **Severity** | Medium |
| **Code locations** | `Web.config:1-120` (no `<httpCookies>` element) |
| **Evidence** | Web.config has no `<system.web><httpCookies requireSSL="true" httpOnlyCookies="true" />`. |
| **Impact** | Today there are no application cookies (no auth, no session) — so the **immediate** impact is low. **But** the moment auth is added, ASP.NET Identity's `.AspNet.ApplicationCookie` will be issued without these flags by default unless explicitly configured. This is a configuration trap waiting to bite. |
| **Remediation** | Add `<httpCookies requireSSL="true" httpOnlyCookies="true" />` to Web.config now (preventative). In the rewrite, configure `services.AddAuthentication().AddCookie(o => { o.Cookie.HttpOnly = true; o.Cookie.SecurePolicy = CookieSecurePolicy.Always; o.Cookie.SameSite = SameSiteMode.Strict; })`. |
| **Phase 2 disposition** | Resolved during auth wiring increment. |

### SEC-MEDIUM-004 — No `<machineKey>` configuration (anti-forgery + session keys not pinned)

| Field | Value |
|---|---|
| **OWASP** | A05:2021 — Security Misconfiguration |
| **Severity** | Medium |
| **Code locations** | `Web.config:1-120` (no `<machineKey>` element) |
| **Evidence** | Web.config has no `<system.web><machineKey>` configuration. |
| **Impact** | Without an explicit machine key, ASP.NET generates a per-process key on startup. This means anti-forgery tokens, session cookies, and forms-auth cookies are invalidated on every restart and **cannot survive a load-balancer failover**. With auto-generated keys, scaling out across multiple servers requires explicit machineKey or DataProtection key sharing — otherwise cross-server CSRF token mismatches occur. |
| **Remediation** | For .NET Framework 4.8: configure `<machineKey>` with explicit `validationKey` and `decryptionKey` (stored in Key Vault, never in Web.config in source control). For .NET Core (rewrite target): use `services.AddDataProtection().PersistKeysToAzureBlobStorage(...)` with key encryption via Key Vault. See ADR-007. |
| **Phase 2 disposition** | Resolved during deployment hardening increment. |

### SEC-MEDIUM-005 — Catch blocks swallow exceptions to `Debug.WriteLine`/`Trace.TraceError`; no structured logging

| Field | Value |
|---|---|
| **OWASP** | A09:2021 — Security Logging and Monitoring Failures |
| **Severity** | Medium |
| **Code locations** | `Controllers/BaseController.cs` (`SendEntityNotification` catch block: `Debug.WriteLine(ex.Message)`); `Controllers/StudentsController.cs` (Index, Create, Edit, Delete catch `Trace.TraceError`); `Controllers/CoursesController.cs` (similar pattern); `Controllers/InstructorsController.cs` (similar); `Controllers/DepartmentsController.cs` (similar); `Services/LoggingService.cs` (file is **0 bytes** — empty) |
| **Evidence** | `Services/LoggingService.cs` is a zero-byte file (verified via prior reads — file exists in `Services/` listing but contains no class definition). All controllers fall back to `System.Diagnostics.Trace.TraceError(...)` or `Debug.WriteLine(...)`. There is no central logger, no log aggregation, no correlation IDs, no structured fields. |
| **Impact** | (1) Security events (failed auth attempts post-rewrite, repeated CSRF failures, file-upload rejections) cannot be detected, alerted on, or correlated. (2) Production troubleshooting is blind — `Debug.WriteLine` only outputs in debug builds with a debugger attached; `Trace.TraceError` requires a `TraceListener` to be configured (none is). (3) The empty `LoggingService.cs` is a stub that suggests someone planned to add logging and never finished. |
| **Remediation** | Delete `LoggingService.cs` (it's a misleading stub). In the rewrite, use `ILogger<T>` injected per class with `Microsoft.Extensions.Logging.AzureAppServices` or OpenTelemetry exporter. Add structured properties to every log call (`logger.LogWarning("Upload rejected for {FileName}, size {Size}", ...)`). |
| **Phase 2 disposition** | Resolved during rewrite scaffold (Increment 1). |

### SEC-MEDIUM-006 — Web.config `<httpRuntime executionTimeout="3600">` (60-minute request timeout)

| Field | Value |
|---|---|
| **OWASP** | A05:2021 — Security Misconfiguration (DoS hardening) |
| **Severity** | Medium |
| **Code locations** | `Web.config:31` (`<httpRuntime targetFramework="4.8" maxRequestLength="10240" executionTimeout="3600" />`) |
| **Evidence** | The execution timeout is set to 3600 seconds (1 hour). The .NET default is 110 seconds. The config offers no documented justification. |
| **Impact** | Allows long-running requests to consume worker threads for an hour. An attacker can intentionally trigger slow operations (search with very long filter strings, repeated paginated reads, file uploads with slow client transfer rates) to exhaust the worker pool — slow-loris-style DoS. |
| **Remediation** | Reduce to 30 seconds (default) or remove the override. If a specific operation needs more, scope the timeout per-action via `[AsyncTimeout(60_000)]` (.NET Framework) or `Request.HttpContext.RequestAborted` cancellation tokens (.NET Core). |
| **Phase 2 disposition** | Resolved during Web.config replacement (rewrite scaffold). |

### SEC-MEDIUM-007 — `Microsoft.Data.SqlClient` 2.1.4 has known CVEs in the 2.x series

| Field | Value |
|---|---|
| **OWASP** | A06:2021 — Vulnerable and Outdated Components |
| **Severity** | Medium |
| **Code locations** | `packages.config` (`<package id="Microsoft.Data.SqlClient" version="2.1.4" targetFramework="net48" />`) |
| **Evidence** | `Microsoft.Data.SqlClient` 2.1.4 is the version pinned by the EF Core 3.1.32 dependency chain. Known CVEs in the 2.x series include CVE-2022-41064 (information disclosure) and other advisories addressed in 2.1.5+ and the 5.x series. The application's binding redirects in Web.config peg this version. |
| **Impact** | Production deployments could be vulnerable to known SqlClient issues — though exploitability requires specific connection-string scenarios and most CVEs in the 2.x line are low-severity. Risk is elevated **only** when the connection string is pointed at an untrusted SQL Server, which is not the documented dev or future production topology. |
| **Remediation** | Bump to 2.1.7 (last 2.x patch) or 5.x in the rewrite. EF Core 8 brings `Microsoft.Data.SqlClient` 5.x automatically. |
| **Phase 2 disposition** | Resolved automatically by EF Core 8 upgrade in the rewrite. |

---

## 5. Low Findings

### SEC-LOW-001 — EF Core 3.1.32 is out of support (LTS ended December 2022)

| Field | Value |
|---|---|
| **OWASP** | A06:2021 — Vulnerable and Outdated Components |
| **Severity** | Low (the package itself does not have known CVEs, but it receives no security patches) |
| **Code locations** | `packages.config` (`<package id="Microsoft.EntityFrameworkCore" version="3.1.32" />`), `bin/Microsoft.EntityFrameworkCore.xml` |
| **Evidence** | EF Core 3.1.32 is the last 3.1 patch release. EF Core 3.1 LTS ended December 2022. New CVEs in EF Core are not back-ported to 3.1. |
| **Impact** | No active security patching. Future query-translation bugs and SQL injection edge cases will not be fixed. |
| **Remediation** | Upgrade to EF Core 8 in the rewrite. |
| **Phase 2 disposition** | Resolved by rewrite. |

### SEC-LOW-002 — ASP.NET MVC 5.2.9 + .NET Framework 4.8.2 are on extended support only

| Field | Value |
|---|---|
| **OWASP** | A06:2021 |
| **Severity** | Low |
| **Code locations** | `packages.config` (`<package id="Microsoft.AspNet.Mvc" version="5.2.9" />`), `Web.config:31` (`targetFramework="4.8"`) |
| **Evidence** | ASP.NET MVC 5.2.9 is the last release of the 5.x line; no further feature work is planned. .NET Framework 4.8.2 is in maintenance mode with security-only updates. |
| **Impact** | Bug fixes are unlikely; security patches still flow via Microsoft Update Tuesday but receive lower priority than .NET 6/7/8/9. |
| **Remediation** | Migrate to .NET 8 (the rewrite). |
| **Phase 2 disposition** | Resolved by rewrite. |

### SEC-LOW-003 — `MSAL.NET 4.21.1` is referenced but unused (dead dependency)

| Field | Value |
|---|---|
| **OWASP** | A06:2021 (attack-surface bloat) |
| **Severity** | Low |
| **Code locations** | `packages.config` (`<package id="Microsoft.Identity.Client" version="4.21.1" />`); zero `using Microsoft.Identity.Client` in any C# file (verified via grep) |
| **Evidence** | The package is referenced but not imported anywhere. The 4.21.1 version is from 2020; current MSAL.NET is 4.74.x with many security advisories addressed since. |
| **Impact** | Unused dependency expands the attack surface and version-pin matrix. If a deserialization or transitive dependency CVE affected MSAL 4.21.1, the application would be vulnerable despite never invoking MSAL. |
| **Remediation** | Remove from `packages.config` and let the rewrite reintroduce `Microsoft.Identity.Web` (current) when wiring Entra ID auth. |
| **Phase 2 disposition** | Resolved during rewrite (clean dependency manifest). |

### SEC-LOW-004 — Open-redirect surface: every controller's `RedirectToAction` is internal-only (no SSRF risk)

| Field | Value |
|---|---|
| **OWASP** | A10:2021 — Server-Side Request Forgery (informational, not a finding) |
| **Severity** | Low (informational — verified absent) |
| **Evidence** | Searched all controllers for `Redirect(`, `RedirectPermanent(`, and any `Url.Action(` pattern that could echo user input. Result: every redirect uses `RedirectToAction(nameof(...))` with a hardcoded action name. There is no `?returnUrl=` parameter, no open-redirect vector, no SSRF entry point. |
| **Impact** | None — included in the assessment for completeness because L3 mandates SSRF/open-redirect review. |
| **Remediation** | None required. Re-verify after the rewrite, when login redirects (`returnUrl`) are introduced — these MUST be validated with `Url.IsLocalUrl(returnUrl)` in MVC 5 / `IUrlHelper.IsLocalUrl(returnUrl)` in MVC Core. |
| **Phase 2 disposition** | N/A — preventative note for the auth wiring increment. |

---

## 6. Dependency CVE Scan

| Package (from `packages.config`) | Version | Status | Notes |
|---|---|---|---|
| Microsoft.AspNet.Mvc | 5.2.9 | OK (extended support) | No active CVEs at 5.2.9 |
| Microsoft.AspNet.Razor | 3.2.9 | OK | No active CVEs |
| Microsoft.AspNet.WebPages | 3.2.9 | OK | No active CVEs |
| Microsoft.AspNet.Web.Optimization | 1.1.3 | OK | Old but no active CVEs |
| Microsoft.EntityFrameworkCore | 3.1.32 | **Out of support** (Dec 2022) | No back-ported security fixes; no current CVE |
| Microsoft.EntityFrameworkCore.SqlServer | 3.1.32 | Out of support | No current CVE |
| Microsoft.EntityFrameworkCore.Tools | 3.1.32 | Out of support | No current CVE |
| Microsoft.Data.SqlClient | 2.1.4 | **Has known CVEs in 2.x series** | See SEC-MEDIUM-007 |
| Newtonsoft.Json | 13.0.3 | OK | No active CVEs at 13.0.3 (CVE-2024-21907 affected ≤13.0.1) |
| WebGrease | 1.6.0 | Old (last release 2014) | No active CVEs but unmaintained |
| Antlr | 3.5.0.2 | Old | Dependency of WebGrease; no active CVEs |
| Microsoft.Identity.Client | 4.21.1 | Outdated (current is 4.74.x) | See SEC-LOW-003 — unused |
| Microsoft.Bcl.AsyncInterfaces | 1.1.1 | OK | No CVEs |
| Microsoft.CodeDom.Providers.DotNetCompilerPlatform | 3.6.0 | OK | No CVEs |
| Microsoft.Net.Compilers (compile-time) | 4.4.0 | OK | Dev-time only |
| jQuery (NuGet) | 3.7.1 | OK | No CVEs |
| Bootstrap (NuGet) | 5.3.3 | OK | No CVEs |
| jQuery.Validation | 1.20.0 | OK | No CVEs |

**Summary:** **No critical or high-severity CVEs** at the pinned versions for currently-imported packages. The flagged items (SEC-MEDIUM-007, SEC-LOW-001, SEC-LOW-003) are about being on out-of-support tracks, not about actively-exploitable known CVEs.

---

## 7. Authentication & Authorization Review (Level 3 Deep Dive)

### 7.1 Authentication

- **Scheme registered:** None. No OWIN startup file (`Startup.cs` or `Startup.Auth.cs`) exists in the codebase. `Microsoft.Owin.Security.*` packages are not in `packages.config`. `Web.config` has no `<authentication>` block.
- **Identity store:** None. There are no `AspNetUsers`, `AspNetRoles`, or `AspNetUserClaims` tables — the database name (`ContosoUniversityNoAuthEFCore`) literally encodes "no auth."
- **Token issuance / validation:** None.
- **MFA:** N/A.
- **Password policy:** N/A.
- **Session management:** No `<sessionState>` in Web.config; no `Session[]` usage in any controller.

### 7.2 Authorization

- `[Authorize]` attribute usage: **zero** (verified).
- `[AllowAnonymous]` attribute usage: zero.
- Role checks (`User.IsInRole(...)`, `User.HasClaim(...)`): zero.
- Policy-based authorization: N/A (MVC 5 era; would be introduced in the .NET Core rewrite).
- Resource-based authorization (e.g., "user can edit this Course only if it's their department"): N/A — every action is global.

### 7.3 Anonymous-call test plan (post-deployment)

Once the application is deployed (today or after rewrite), every endpoint listed in `specs/contracts/api/_routes-overview.md` must be smoke-tested as anonymous. The expected results:

| Endpoint pattern | Today | After rewrite + auth |
|---|---|---|
| `GET /` (`Home/Index`) | 200 OK (read-only landing) | 200 OK with `[AllowAnonymous]` |
| `GET /Students` | 200 OK | 401/Redirect |
| `POST /Students/Delete/5` | 200 OK + actual delete | 401/Redirect |
| `GET /Notifications/GetNotifications` | 200 OK with data | 401 (or 403 for cross-role notifications) |
| `POST /Notifications/MarkAsRead` | 200 OK + (no-op due to KL-NOTIF-002) | 401 + CSRF token required (per SEC-HIGH-001) |
| `POST /Courses/Create` (with multipart upload) | 200 OK + file saved | 401 |

This is the test plan that the post-rewrite green-baseline + anti-regression suite must execute on every CI run.

### 7.4 Recommendations

See **ADR-005** (authentication mechanism: ASP.NET Core Identity + Microsoft Entra ID for production) and **ADR-006** (authorization model: role-based with two roles `Admin` and `Reader` for the rewrite, expandable to per-resource policies in a later increment).

---

## 8. Secrets-in-Code Scan

| Search target | Found? | Notes |
|---|---|---|
| Plaintext `password=` | Only in connection string: `Integrated Security=True` (no password — Windows auth on LocalDB) | Acceptable for dev with LocalDB |
| `apikey` / `api-key` / `apiKey` | None | Verified |
| `secret` | None in code | `bin/*.xml` doc files contain "secret" as an English word — false positives only |
| `Bearer` token literals | None | Verified |
| Base64-looking 32+ char strings | None in source code | (`bin/*.dll` are excluded from the scan) |
| Connection string with embedded credentials | None | `Web.config` uses `Integrated Security=True` |
| Storage account keys | None | No Azure Storage code |
| Azure subscription IDs | None | No Azure SDK usage |
| Hardcoded JWT signing keys | None | No JWT issuance |
| `.pem`, `.pfx`, `.key`, `.crt` files | None in repo | Verified via file listing |

**Verdict:** **No secrets in source code.** The connection string uses Windows Integrated Security on LocalDB, which has no password to leak. This is acceptable for a development-only application; it must be replaced with **Managed Identity → Azure SQL** for any production deployment (see ADR-007 for the secrets-management strategy).

---

## 9. ADRs Generated by This Assessment

| ADR | Title | Triggering finding(s) |
|---|---|---|
| **ADR-005** | Authentication Mechanism — ASP.NET Core Identity + Microsoft Entra ID | SEC-CRITICAL-001, SEC-CRITICAL-002, SEC-LOW-003 |
| **ADR-006** | Authorization Model — Role-based with `Admin`/`Reader` for rewrite, policy-based for future | SEC-CRITICAL-001, SEC-CRITICAL-002, SEC-HIGH-002 |
| **ADR-007** | Secrets Management — Azure Key Vault + Managed Identity | SEC-MEDIUM-004, secrets-scan §8 |

ADR-002 (rewrite-vs-modernize), ADR-003 (target stack), and ADR-004 (migration pattern — per-controller big-bang within spec2cloud increments) were generated by the rewrite assessment but reinforce this assessment's recommendations.

---

## 10. Findings Cross-Reference to FRDs

| FRD | Findings affecting it |
|---|---|
| F-001 Student Management | SEC-CRITICAL-001, SEC-CRITICAL-002, SEC-MEDIUM-005, SEC-MEDIUM-006 |
| F-002 Course Management | SEC-CRITICAL-001, SEC-CRITICAL-002, SEC-HIGH-002, SEC-HIGH-003, SEC-MEDIUM-005 |
| F-003 Instructor Management | SEC-CRITICAL-001, SEC-CRITICAL-002, SEC-MEDIUM-005 |
| F-004 Department Management | SEC-CRITICAL-001, SEC-CRITICAL-002, SEC-MEDIUM-005 |
| F-005 Enrollment Statistics | SEC-CRITICAL-001 (read-only but should still be authenticated for audit) |
| F-006 Static Pages | SEC-MEDIUM-001, SEC-MEDIUM-002, SEC-MEDIUM-003, SEC-MEDIUM-004 |
| F-007 Notification System | SEC-CRITICAL-001, SEC-CRITICAL-002, SEC-HIGH-001, SEC-MEDIUM-005 |
| F-008 Queue Diagnostics | SEC-CRITICAL-001, SEC-HIGH-004 (should be deleted per rewrite assessment) |

---

## 11. Mandatory Completion Checklist

- [x] Findings have severity ratings — §2, §3, §4, §5
- [x] OWASP Top 10:2021 categories mapped — every finding in §2–§5
- [x] Code locations cited (file:line) — every finding
- [x] Remediation steps provided — every finding
- [x] Dependency CVE scan results included (even if no critical CVEs found) — §6
- [x] Authentication and authorization patterns reviewed and documented — §7
- [x] Secrets-in-code scan completed (no secrets in output) — §8
- [x] At least one ADR exists in `specs/adrs/` for significant security architecture decisions — ADR-005, ADR-006, ADR-007
- [x] State JSON and audit log will be updated (after this file is committed)

---

_This assessment was produced autonomously based on the user's path-selection of `["rewrite", "security"]` and authoritative-source code reads. It auto-escalated to Level 3 because authentication and authorization are architecturally absent. The findings are evidence-based — every code-location citation has been verified against the source files. The user can request additional depth (full dynamic-analysis pen test, threat model, compliance mapping) at any time._
