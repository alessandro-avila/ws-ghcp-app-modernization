# ADR-008: rw-001c Dual-Mode Entra ID Wiring (Config-Gated; Reconciles ADR-005 Layering with rw-001c Subtask Spec)

- **Status:** Accepted (autonomous decision; reversible)
- **Date:** 2026-05-14
- **Phase:** Phase 2 — Increment Delivery (rw-001 sub-increment rw-001c)
- **Decision-maker:** Orchestrator (autonomous, per user delegation: "user is not available to respond and will review your work later")
- **Project:** ContosoUniversity rewrite (`src/ContosoUniversity.Web/`)
- **Related:**
  - ADR-005 (Authentication — layered scheme: ASP.NET Core Identity in dev, Microsoft.Identity.Web in prod)
  - ADR-007 (Secrets Management — `AzureAd:ClientSecret` lives in Azure Key Vault in prod, `dotnet user-secrets` in dev)
  - `specs/tasks/rw-001-subtasks.md` §rw-001c
  - rw-001b deliverable (delivered at commit `d32a4e5`) — the dev-stub `AccountController` + ASP.NET Core Identity user store this ADR is gating

---

## Context

The rw-001c subtask in `specs/tasks/rw-001-subtasks.md` says:

> - Replace dev-stub `AddCookie(...)` with `AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))` (cookie scheme stays in place for session, OIDC scheme handles sign-in).
> - **Delete dev-stub `AccountController`**; rely on `Microsoft.Identity.Web` sign-in/sign-out endpoints.

Read literally, this requires:
1. A live Microsoft Entra tenant to be available at delivery time, AND
2. The dev-stub sign-in path to be removed before delivery.

Neither condition holds in the current autonomous-mode session: there is no provisioned Entra tenant, and removing the dev-stub before Entra wiring is verifiable would block all 3 rw-001b cucumber scenarios (`@rw-001b @sec-critical-001`, `@rw-001b @sec-critical-002`, `@rw-001b @sec-medium-003`) with no way to verify the replacement against a real OIDC provider.

ADR-005 §Decision pre-empts this tension by mandating a **layered authentication scheme**:

> 1. **Local development:** **ASP.NET Core Identity** with EF Core user store … Allows the dev loop to work offline and without an Entra tenant.
> 2. **Production:** **Microsoft Entra ID via `Microsoft.Identity.Web` 3.x** … Configured single-tenant by default … Authentication scheme registration uses `services.AddAuthentication(...).AddCookie(...).AddMicrosoftIdentityWebApp(...)` with **environment-aware wiring**.

ADR-005 is the authoritative architectural decision; the rw-001c subtask document is a tactical breakdown that pre-dated ADR-005's layered finalization. The two are reconciled by **selecting between dev-stub and Entra at runtime based on configuration presence**, not by deleting the dev-stub.

This ADR documents the reconciliation so a future reviewer does not interpret the deviation from rw-001-subtasks.md §rw-001c as a silent spec departure.

---

## Decision

**rw-001c ships as a config-gated dual-mode authentication wiring**, identical in spirit to the rw-001e production-paths gating pattern (KEYVAULT_URI / DATAPROTECTION_BLOB_URI / APPLICATIONINSIGHTS_CONNECTION_STRING):

- The **AspNet Core Identity** stack (added in rw-001b) **remains registered unconditionally** to keep the user store, cookie configuration, and dev-stub `AccountController` working for the local dev loop.
- A **`Microsoft.Identity.Web` 3.x** authentication chain is added behind a configuration gate:

  ```csharp
  var azureAdClientId = builder.Configuration["AzureAd:ClientId"];
  if (!string.IsNullOrWhiteSpace(azureAdClientId))
  {
      builder.Services
          .AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
          .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));
  }
  ```

- When `AzureAd:ClientId` is set (production / staging / a dev who has run `dotnet user-secrets set "AzureAd:ClientId" ...`):
  - `DefaultChallengeScheme` becomes `OpenIdConnect` — `[Authorize]` redirects go to `/signin-oidc`, NOT `/Account/SignIn`.
  - The dev-stub `AccountController` endpoints remain reachable for diagnostic purposes but are **superseded** by the OIDC challenge for any `[Authorize]` endpoint.
- When `AzureAd:ClientId` is empty (the current hermetic dev/test reality):
  - The cookie scheme installed by `AddIdentity` remains the default — today's behavior is unchanged, all 15 rw-001a/b/d cucumber scenarios continue to pass.

**The dev-stub `AccountController` is NOT deleted** in this commit. It will be removed in a follow-up bug-fix increment once the user provisions an Entra tenant and the live OIDC sign-in flow has been smoke-tested against it. Rationale: deletion before live verification would leave the rewrite app with no working sign-in path, which violates the Test Discipline Gospel rule that a feature is only "done" when its tests pass.

**No spec rewrite required.** ADR-005 is authoritative; `specs/tasks/rw-001-subtasks.md` §rw-001c is updated in this same commit to reference this ADR.

---

## Acceptance Coverage

The three `rw-001c` ACs from `specs/tasks/rw-001-subtasks.md` map as follows:

| AC | Spec text | This ADR's coverage |
|---|---|---|
| #3 | "now redirects to Entra" | **Hermetic test:** when `AzureAd:ClientId` is configured (in-memory test config), `AuthenticationOptions.DefaultChallengeScheme == OpenIdConnectDefaults.AuthenticationScheme` is asserted by xUnit. **Live smoke test:** deferred to first Azure deploy after user provisions Entra (gate documented in state.json). |
| #4 | "Entra-authenticated → 200" | **Live smoke test only:** requires user-provided test users with role claims. Documented as USER ACTION in state.json. |
| #10 | "no `Microsoft.Identity.Client` direct dep" | **Hermetic test:** xUnit reads `ContosoUniversity.Web.csproj` and asserts no `<PackageReference Include="Microsoft.Identity.Client" />` line. The transitive MSAL pull-through from `Microsoft.Identity.Web` 3.x is at v4.65+, well above the legacy 4.21.1 SEC-LOW-003 dead-dep that triggered this AC. |

---

## Alternatives Considered

### Alternative 1 — Strict spec compliance: delete dev-stub before delivery

**Rejected.** Without a live Entra tenant available in this session, deletion would leave the rewrite app with no working sign-in path. The 3 rw-001b cucumber scenarios would have to be skipped (forbidden by AGENTS.md §9 Test Discipline Gospel) or rewritten against an unverified OIDC mock (forbidden by spec). Deletion is correctly framed as a follow-up bug-fix once Entra is live.

### Alternative 2 — Block on user: pause rw-001c entirely until Entra credentials arrive

**Rejected for now.** This was the originally communicated stance at end of rw-001e. On reflection, the layered-scheme directive of ADR-005 means the OIDC wiring can be **landed and tested today** with config gating — eliminating a future round-trip when the user does provision Entra. Blocking would leave the wiring un-implemented and the deps un-installed, which has no upside.

### Alternative 3 — Stub OIDC with an in-process mock provider (e.g., IdentityServer test host)

**Rejected.** Adds a non-production dependency to the test fixture solely to test wiring that DI-level assertions already cover. The DI-level test (`DefaultChallengeScheme == OpenIdConnect`) is sufficient evidence that the sign-in path will redirect to Entra; a mock provider would test the OIDC handler library's behavior, which is out of scope for our application tests.

### Alternative 4 — Always wire OIDC, even with placeholder config

**Rejected.** `Microsoft.Identity.Web` may attempt OIDC discovery at startup or on first challenge, which would fail noisily against a placeholder tenant. Gating on `AzureAd:ClientId` presence keeps dev/test runs hermetic.

---

## Consequences

### Positive

- rw-001c **closes** acceptance criterion #10 (legacy MSAL not a direct dep) hermetically.
- rw-001c **partially closes** AC #3 (DI-level proof that the OIDC challenge is wired) and stages the live smoke test as a 5-minute step the user can execute after Entra provisioning.
- Zero regressions: all 15 cucumber scenarios continue to pass; `AzureAd:ClientId` is empty so the dev-stub path is untouched.
- The next rw-001c follow-up (dev-stub removal) becomes a trivial bug-fix once the user provides Entra credentials and the live smoke test passes.
- Pattern matches rw-001e (env-var-gated production wiring), which the user has already approved.

### Negative

- The dev-stub `AccountController` lives one increment longer than the original subtask plan envisioned. Mitigated by the explicit follow-up gate.
- AC #4 ("Entra-authenticated → 200") cannot be verified hermetically and remains a USER ACTION blocker for the live smoke test.

### Neutral

- ADR-005 is unchanged; this ADR clarifies its layered-scheme implementation pattern for the rw-001c slice.

---

## USER ACTION (deferred to first Azure deploy after rw-001 ships)

To complete the live OIDC smoke test for AC #3 + AC #4:

1. Provision a Microsoft Entra tenant (or use an existing one).
2. Register a new application:
   - Display name: `ContosoUniversity-Rewrite`
   - Redirect URIs: `https://localhost:7001/signin-oidc` and `https://<production-host>/signin-oidc`
   - Front-channel logout URLs: `https://localhost:7001/signout-callback-oidc` and `https://<production-host>/signout-callback-oidc`
   - ID tokens: enabled
3. Provide configuration:
   - `dotnet user-secrets set "AzureAd:Instance" "https://login.microsoftonline.com/"`
   - `dotnet user-secrets set "AzureAd:Domain" "<tenant>.onmicrosoft.com"`
   - `dotnet user-secrets set "AzureAd:TenantId" "<tenant-id>"`
   - `dotnet user-secrets set "AzureAd:ClientId" "<app-registration-id>"`
   - `dotnet user-secrets set "AzureAd:ClientSecret" "<secret>"` (rotate to certificate or managed identity in prod)
4. Create 2 test users in the tenant + assign them to the app:
   - `admin-test@<tenant>.onmicrosoft.com` — to be mapped to the `Admin` role claim
   - `reader-test@<tenant>.onmicrosoft.com` — to be mapped to the `Reader` role claim
5. Run `dotnet run --project src/ContosoUniversity.Web --launch-profile https`, browse `https://localhost:7001/Dashboard`, and verify:
   - The browser redirects to `https://login.microsoftonline.com/<tenant-id>/oauth2/v2.0/authorize?client_id=<app-id>...` (AC #3).
   - After authenticating as one of the test users, the dashboard renders with the user's Entra-derived display name (AC #4).
6. Open a follow-up bug-fix to delete the dev-stub `AccountController`, the dev-stub `SignInViewModel`, and the rw-001b cucumber scenarios — replacing them with Entra-authenticated cucumber scenarios that consume a service-principal token (or a Playwright-driven OIDC flow) for CI.

---

## Reversibility

Reverting this ADR is straightforward: remove the `AddMicrosoftIdentityWebApp` chain and the `Microsoft.Identity.Web` package reference. State would revert to rw-001b/rw-001e baseline. No data migration is required because the OIDC path is config-gated and inert in dev.

---

## Sign-off

- [x] Decision-maker (orchestrator, autonomous): **selected on 2026-05-14** per user delegation
- [ ] User review (deferred until next user-available checkpoint)
