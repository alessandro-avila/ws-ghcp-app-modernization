# rw-001 Sub-Task Decomposition

> **Source increment:** `specs/increment-plan.md` §2 rw-001 — _"Project scaffold + EF Core 8 migration + Auth wiring (foundation)"_ (4 days estimated, 10 acceptance criteria, 6 ADRs in scope).
>
> **Why decompose:** rw-001 spans project scaffolding, EF Core 8 baseline migration, ASP.NET Core Identity, Microsoft.Identity.Web (Entra ID), security headers, data protection, secrets management, structured logging, and Application Insights. Several of these have **external dependencies** (Microsoft Entra tenant + app registration, Azure Key Vault, Application Insights workspace) that must be provisioned by the user and cannot be completed autonomously. The right move is to decompose into surgical sub-increments, each green at a natural checkpoint, deferring the externally-blocked work until the user provisions the cloud resources.
>
> **Pipeline rule:** every sub-increment goes through Track-A red→green pipeline (Cucumber scenarios → step defs → red baseline → implementation → green → commit) AND keeps the legacy MVC 5 app green. Each sub-increment leaves the working tree shippable.

## Sub-Increments

### rw-001a — Project scaffold + EF Core 8 baseline + legacy parity (no auth yet)
- **Scope**
  - Create `src/ContosoUniversity.Web/` (ASP.NET Core 8 MVC, `net8.0`).
  - Create `src/ContosoUniversity.Web.UnitTests/` (xUnit + Microsoft.AspNetCore.Mvc.Testing).
  - Create root `ContosoUniversity.sln` linking BOTH the legacy MVC 5 project AND the two new projects.
  - Add `global.json` pinning SDK to `8.0.421`.
  - Port the EF Core 3.1 entity model (`Student`, `Course`, `Enrollment`, `Person`, `Instructor`, `OfficeAssignment`, `CourseAssignment`, `Department`, `Notification`) into `src/ContosoUniversity.Web/Domain/` with `net8.0` namespaces. Schema MUST match legacy (`EnsureCreated`-equivalent DDL).
  - Port `SchoolContext` to EF Core 8 (`Microsoft.EntityFrameworkCore.SqlServer` 8.x). Register via DI with scoped lifetime.
  - Scaffold initial migration `0001_InitialFromLegacy` with idempotent `IF NOT EXISTS` guards (or use `dotnet ef migrations script --idempotent` for prod).
  - `Program.cs` minimal: `AddControllersWithViews()`, `AddDbContext<SchoolContext>(...)`, `MapDefaultControllerRoute()`, placeholder `HomeController.Index()` returning a static "ContosoUniversity (rewrite)" view.
  - One xUnit DI smoke test: WebApplicationFactory boots; resolves `SchoolContext` from DI.
  - One Cucumber e2e: `GET http://localhost:7000/` returns 200 + body contains "ContosoUniversity (rewrite)".
- **Acceptance** (subset of rw-001 AC list): #1 (`dotnet build` succeeds), #2 (`dotnet test` passes), #5 (`__EFMigrationsHistory` row for `0001_InitialFromLegacy`).
- **Effort:** 1.5 days
- **Risk:** Medium (EF Core 3.1→8 schema parity verification is non-trivial)
- **External blockers:** none
- **Cucumber tags:** `@rewrite @rw-001a`
- **State step:** `start` → `tests-authored` → `red-baseline-confirmed` → `impl-green` → `delivered`

### rw-001b — Cookie auth + ASP.NET Core Identity + dev-stub sign-in (no Entra yet)
- **Scope**
  - Add `Microsoft.AspNetCore.Identity.EntityFrameworkCore` 8.x.
  - Scaffold ASP.NET Core Identity tables via `0002_AddIdentity` migration (extends `SchoolContext` to `IdentityDbContext<ApplicationUser>`).
  - Wire `AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(...)` with `__Host-ContosoUniversity.Auth` cookie + `Secure=Always` + `HttpOnly` + `SameSite=Strict` + `SlidingExpiration=true` + `ExpireTimeSpan=60min`.
  - Wire `AddAuthorization(o => o.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())` (default-deny).
  - Allow-list `/Account/SignIn`, `/Account/SignOut`, `/Error*` as anonymous via `[AllowAnonymous]`.
  - Seed two users on first-run: `admin@contoso.test` (role `Admin`), `reader@contoso.test` (role `Reader`).
  - Dev-only `AccountController` with username/password sign-in form (LOCAL DEV ONLY — superseded by Entra in rw-001c).
- **Acceptance:** AC #3 (anon → 302 to `/Account/SignIn`), AC #4 (after sign-in → 200), AC #6 (seeded users), AC #8 (cookie flags).
- **Effort:** 1 day
- **Risk:** Low
- **External blockers:** none
- **Cucumber tags:** `@rewrite @rw-001b @sec-critical-001 @sec-critical-002 @sec-medium-003`

### rw-001c — Microsoft Entra ID integration (replaces dev-stub sign-in)
- **Scope**
  - Add `Microsoft.Identity.Web` 3.x.
  - Replace dev-stub `AddCookie(...)` with `AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))` (cookie scheme stays in place for session, OIDC scheme handles sign-in).
  - Map `User.Identity?.Name` to `ICurrentUser.Email` for downstream `CreatedBy` backfill (rw-007).
  - Delete dev-stub `AccountController`; rely on `Microsoft.Identity.Web` sign-in/sign-out endpoints.
  - Verify legacy `Microsoft.Identity.Client` 4.21.1 dead-dep is NOT pulled in transitively.
- **Acceptance:** AC #3 (now redirects to Entra), AC #4 (Entra-authenticated → 200), AC #10 (no `Microsoft.Identity.Client` direct dep).
- **Effort:** 0.5 day
- **Risk:** Medium — depends on user provisioning Entra app registration (tenant ID + client ID + client secret/cert OR managed identity + redirect URI `https://localhost:7001/signin-oidc`).
- **External blockers (USER ACTION REQUIRED):**
  - Microsoft Entra tenant available
  - App registration created in tenant with redirect URI `https://localhost:7001/signin-oidc` and `https://localhost:7001/signout-callback-oidc`
  - User provides `AzureAd:TenantId`, `AzureAd:ClientId`, `AzureAd:ClientSecret` (via `dotnet user-secrets`)
  - Two test users assigned to the app in Entra (mapped to `Admin` and `Reader` claims via group → role mapping or appsettings-based fallback)
- **Cucumber tags:** `@rewrite @rw-001c @sec-critical-001 @sec-critical-002`

### rw-001d — Security headers + exception handler + structured logging + request timeout
- **Scope**
  - Add security-headers middleware: `Strict-Transport-Security: max-age=31536000; includeSubDomains; preload`, `Content-Security-Policy: default-src 'self'`, `X-Frame-Options: DENY`, `X-Content-Type-Options: nosniff`, `Referrer-Policy: strict-origin-when-cross-origin`.
  - Wire `app.UseExceptionHandler("/Error")` + `app.UseStatusCodePagesWithReExecute("/Error/{0}")` + `ErrorController`.
  - Wire `Microsoft.Extensions.Logging` with structured logging to stdout (JSON formatter via `AddJsonConsole()`) in dev.
  - Add request timeout middleware (30 seconds default per ADR-006).
  - Delete empty legacy `LoggingService.cs` reference (legacy app file untouched; just verify the new project does not port it).
- **Acceptance:** AC #7 (5 security headers present), AC #9 (structured JSON logs).
- **Effort:** 0.5 day
- **Risk:** Low
- **External blockers:** none (Application Insights wiring deferred to rw-001e)
- **Cucumber tags:** `@rewrite @rw-001d @sec-medium-001 @sec-medium-002 @sec-medium-005 @sec-medium-006`

### rw-001e — Data Protection + Key Vault + secrets management + Application Insights
- **Scope**
  - Wire `services.AddDataProtection()` with file-system in dev (`%LOCALAPPDATA%/ContosoUniversity/keys`), Azure Blob + Key Vault in prod via `PersistKeysToAzureBlobStorage(...)` + `ProtectKeysWithAzureKeyVault(...)`.
  - Configure `dotnet user-secrets init` for the new project.
  - Add `Azure.Identity` + `Azure.Extensions.AspNetCore.Configuration.Secrets` for Key Vault secret loading in prod.
  - Add `Microsoft.ApplicationInsights.AspNetCore` with `services.AddApplicationInsightsTelemetry()` reading connection string from config.
- **Acceptance:** rw-001 AC list does not enumerate Data Protection / KeyVault / App Insights as separate AC — they are implicit in production-hardening. Verification: `dotnet test` includes one xUnit assertion that `IDataProtectionProvider` resolves from DI.
- **Effort:** 0.5 day
- **Risk:** Low (in dev) / Medium (in prod — depends on user provisioning Key Vault + App Insights)
- **External blockers (deferred to deployment time, not blocking dev):**
  - Azure Key Vault provisioned with `KEYVAULT_URI` known
  - Application Insights workspace provisioned with `APPLICATIONINSIGHTS_CONNECTION_STRING` known
- **Cucumber tags:** `@rewrite @rw-001e @sec-medium-004`

## Total

5 sub-increments × ~0.5–1.5 days each = **4 days** (matches plan estimate).

## Coexistence Plan

- Legacy MVC 5 app at `src/ContosoUniversity/` continues to run unchanged on IIS Express :44300.
- New ASP.NET Core 8 app at `src/ContosoUniversity.Web/` runs on Kestrel :7000 (HTTP) / :7001 (HTTPS).
- Both point at the same `(localdb)\MSSQLLocalDB` `ContosoUniversity` database during dev.
- Cucumber tests are split by tag: `@legacy` runs against :44300; `@rewrite` runs against :7001. Tag-based test runs allow each side to be exercised independently.

## Order of Delivery

```
rw-001a → rw-001b → rw-001d → rw-001e → rw-001c
```

Rationale:
- **rw-001c (Entra) is LAST** because it has the biggest external blocker (user must provision Entra app registration). Sub-increments a, b, d, e can ship without external dependencies.
- **rw-001b before rw-001d** because security-headers + exception-handler middleware are easier to test against an authenticated app (cookie + auth stack already wired).
- **rw-001e before rw-001c** because data-protection key isolation is a prerequisite for cookie auth in a load-balanced topology — even though dev doesn't need it, getting the DI registration in place before Entra wiring reduces churn.

## State Tracking

Each sub-increment gets its own block in `state.json` `currentIncrement` and an entry in `deliveredIncrements` when complete. The parent `incrementPlan.increments[rw-001]` stays `in-progress` until ALL FIVE sub-increments are delivered, at which point it flips to `delivered` and `currentIncrement` advances to `rw-002`.
