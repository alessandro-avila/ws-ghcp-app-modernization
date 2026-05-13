# ADR-005: Authentication Mechanism — ASP.NET Core Identity (Dev) + Microsoft Entra ID (Production)

- **Status:** Accepted (autonomous decision; reversible)
- **Date:** 2026-05-13
- **Phase:** Brownfield Phase A — Assessment (security-driven)
- **Decision-maker:** Orchestrator (autonomous, per user delegation)
- **Project:** ContosoUniversity (`src/ContosoUniversity/`)
- **Related:**
  - ADR-002 (rewrite chosen)
  - ADR-003 (target: ASP.NET Core 8 MVC)
  - Security assessment: [specs/assessment/security.md](../assessment/security.md) §2 (SEC-CRITICAL-001, SEC-CRITICAL-002), §7 (auth review)
  - ADR-006 (authorization model — depends on this ADR)
  - ADR-007 (secrets management — used to store auth secrets)

---

## Context

The security assessment found two critical authentication failures (`specs/assessment/security.md` §2):
- **SEC-CRITICAL-001** — application has no authentication scheme registered; every endpoint is anonymous
- **SEC-CRITICAL-002** — hardcoded `"System"` actor propagates into the audit trail (`Notification.CreatedBy`), making forensic investigation impossible

The rewrite to ASP.NET Core 8 (ADR-003) is the natural moment to introduce authentication. The choice of mechanism affects:
- The user-management story (do we manage users, or does an identity provider?)
- The deployment story (does this run in Azure, on-premise, hybrid?)
- The Phase 2 increment plan (auth wiring is part of Increment 1 per ADR-004)
- The dev-loop story (is local development blocked by Entra ID availability?)

The PRD does not specify a user population; FRDs F-001…F-008 describe all data flows in terms of "the user." For an educational-management application, plausible user populations include:
- Internal-only (faculty + administrators) → Entra ID (single-tenant) is the natural fit
- Mixed internal + external (students self-service) → Entra External ID (formerly Azure AD B2C) or a hybrid
- Demo / portfolio-style → ASP.NET Core Identity (local DB users) is sufficient

Given the absence of explicit user-population guidance and the user's path selection (`["rewrite", "security"]`), the conservative choice is the one that supports the broadest deployment scenarios with the smallest dev-loop friction.

---

## Decision

**Use a layered authentication scheme:**

1. **Local development:** **ASP.NET Core Identity** with EF Core user store (in the same `SchoolContext` / Azure SQL database, segregated under `dbo.AspNetUsers` etc.). Allows the dev loop to work offline and without an Entra tenant.
2. **Production:** **Microsoft Entra ID via `Microsoft.Identity.Web` 3.x** with cookie + OpenID Connect flow. Configured single-tenant by default; multi-tenant or External ID is a configuration switch.
3. **Authentication scheme registration** uses `services.AddAuthentication(...).AddCookie(...).AddMicrosoftIdentityWebApp(...)` with environment-aware wiring (Identity in Development, Entra in Staging/Production).

**MUST NOT** use bare `Microsoft.Identity.Client` (MSAL.NET) directly — `Microsoft.Identity.Web` is the supported wrapper for web applications.

**The dead `Microsoft.Identity.Client` 4.21.1 reference in `packages.config` (SEC-LOW-003) is removed.**

### Cookie configuration (mandatory)

```csharp
options.Cookie.HttpOnly = true;
options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
options.Cookie.SameSite = SameSiteMode.Strict;
options.Cookie.Name = "__Host-ContosoUniversity.Auth";   // __Host- prefix enforces Secure + Path=/
options.SlidingExpiration = true;
options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
options.LoginPath = "/Account/Login";
options.LogoutPath = "/Account/Logout";
options.AccessDeniedPath = "/Home/AccessDenied";
```

### `Microsoft.Identity.Web` configuration (production)

```csharp
builder.Services
    .AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));
```

`AzureAd` section in `appsettings.{Environment}.json`:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "<tenant>.onmicrosoft.com",
    "TenantId": "<tenant-id-or-common>",
    "ClientId": "<app-registration-id>",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc"
  }
}
```

The `ClientSecret` lives in **Azure Key Vault** per ADR-007 (never in source-controlled JSON).

### Replacing the hardcoded `"System"` actor

`BaseController.userName` is removed. Every controller that previously read `userName` now reads `User.Identity?.Name ?? throw new InvalidOperationException(...)` for authenticated endpoints. The `[AllowAnonymous]` set is restricted to:
- `HomeController.Index`
- `HomeController.Contact`
- `HomeController.About`
- `/health` endpoint (added by ADR-004 Increment 8)
- `/Account/Login`, `/Account/Logout`, `/Account/AccessDenied`

---

## Alternatives Considered

### Alternative 1 — ASP.NET Core Identity only (no Entra ID)

**Rejected for production.** Local-DB user management (password hashing, password reset, lockout, MFA enrollment) duplicates capabilities Microsoft Entra ID provides as a managed service. Acceptable for portfolio/demo use; insufficient for a real organizational deployment.

### Alternative 2 — Bare `Microsoft.Identity.Client` (MSAL.NET) without `Microsoft.Identity.Web`

**Rejected.** `Microsoft.Identity.Web` is the Microsoft-supported wrapper for web applications; bare MSAL is for desktop/console apps. The existing `packages.config` reference to `Microsoft.Identity.Client` 4.21.1 is dead code and is removed (SEC-LOW-003).

### Alternative 3 — IdentityServer / Duende IdentityServer

**Rejected.** Self-hosting an OAuth/OIDC server adds operational burden, license cost (Duende is commercial above a small-org threshold), and ongoing security responsibility. Entra ID provides a managed equivalent.

### Alternative 4 — Auth0, Okta, or another third-party identity provider

**Rejected.** Adds an external SaaS dependency and a separate billing relationship. Entra ID is already in the Azure-native path implied by the user's deployment direction (Container Apps / App Service / AKS — all are Azure).

### Alternative 5 — Windows Authentication (Negotiate / Kerberos) only

**Rejected.** Forces deployment behind IIS or a domain-joined AKS node pool; incompatible with Linux containers (the rewrite-target deployment model per ADR-003).

### Alternative 6 — Anonymous (preserve current behavior)

**Rejected.** Directly contradicts SEC-CRITICAL-001 and the user's selection of the `security` path.

---

## Consequences

### Positive

- Forensic-quality audit trail: `Notification.CreatedBy` becomes the actual authenticated principal (closes SEC-CRITICAL-002).
- Production deployments are gated by an enterprise IDP (closes SEC-CRITICAL-001).
- Local dev still works offline (ASP.NET Core Identity with seed users) — no Entra tenant required to run tests or develop.
- Dead MSAL 4.21.1 dependency is cleaned up (closes SEC-LOW-003).
- The `__Host-` cookie prefix + `HttpOnly` + `Secure` + `SameSite=Strict` configuration closes SEC-MEDIUM-003.

### Negative

- **Every Track A green-baseline test must adapt** when this increment ships. The current tests assume anonymous access; after this increment, every test must authenticate first. This is foundation work — Increment 1 of the rewrite explicitly includes test-fixture updates that introduce a `LoginAsTestUser()` helper before any subsequent increment runs.
- **First-time setup overhead for new dev environments:** must run `dotnet user-secrets` to seed local-Identity admin user; documented in Increment 1's README.
- **Production rollout requires an Entra app registration** — coordinated with the deploying organization. This is a Phase P planning task.

### Neutral

- The user-table addition (`AspNetUsers`, `AspNetRoles`, etc.) coexists with the existing 9-entity domain model in the same database. The DB name (`ContosoUniversityNoAuthEFCore`) becomes misleading; rename to `ContosoUniversityEFCore` is recommended at first deployment of the new app.

---

## Reversibility

If the user wishes to keep the application anonymous (e.g., for a portfolio demo), this ADR can be superseded — but doing so would block the security path entirely. Documented for completeness.

---

## Sign-off

- [x] Decision-maker (orchestrator, autonomous): **selected on 2026-05-13** per user delegation
- [ ] User review (deferred until Phase A review gate)

Signed pending user review.
