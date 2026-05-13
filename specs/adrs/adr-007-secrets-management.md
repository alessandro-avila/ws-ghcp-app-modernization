# ADR-007: Secrets Management — Azure Key Vault + Managed Identity (Production), `dotnet user-secrets` (Dev)

- **Status:** Accepted (autonomous decision; reversible)
- **Date:** 2026-05-13
- **Phase:** Brownfield Phase A — Assessment (security-driven)
- **Decision-maker:** Orchestrator (autonomous, per user delegation)
- **Project:** ContosoUniversity (`src/ContosoUniversity/`)
- **Related:**
  - ADR-003 (target stack: ASP.NET Core 8 — supports `IConfiguration` providers including Key Vault)
  - ADR-005 (authentication — produces secrets that need storage, e.g., Entra `ClientSecret`)
  - Security assessment: [specs/assessment/security.md](../assessment/security.md) §4 (SEC-MEDIUM-004 machine key), §8 (secrets-in-code scan)

---

## Context

The source application has **no secrets in source code** (verified — security assessment §8). The connection string in `Web.config` uses `Integrated Security=True` against LocalDB, which has no password to leak. This is the only credential surface today.

The rewrite introduces credentials that must be stored:
- **Microsoft Entra ID `ClientSecret`** (per ADR-005) — for the production OIDC flow
- **Azure SQL Database connection string** (production replacement for LocalDB) — though Managed Identity authentication eliminates the password component
- **DataProtection keys** (per ASP.NET Core) — for cookie encryption, anti-forgery token signing, and TempData encryption (closes SEC-MEDIUM-004's `<machineKey>` gap)
- **Future:** Application Insights connection string, OpenTelemetry exporter credentials, any third-party API keys introduced by future increments

A consistent strategy across all of these is needed: where do secrets live in dev? in prod? how are they injected? how are they rotated?

---

## Decision

**Three-tier strategy by environment:**

| Tier | Environment | Mechanism | Rationale |
|---|---|---|---|
| 1 | Local dev | **`dotnet user-secrets`** (per-developer, `secrets.json` outside source tree at `%APPDATA%\Microsoft\UserSecrets\<id>\secrets.json`) | Standard ASP.NET Core dev experience; never enters source control |
| 2 | CI / Test | **GitHub Actions secrets** → injected as environment variables at job start | No persistent storage; reset per workflow run |
| 3 | Production / Staging | **Azure Key Vault + Managed Identity** | No credentials in app config; rotation managed by Azure; least-privilege via Entra |

### Production wiring

```csharp
if (!builder.Environment.IsDevelopment())
{
    var keyVaultUri = new Uri(builder.Configuration["KeyVault:Uri"]
        ?? throw new InvalidOperationException("KeyVault:Uri is required in non-Development environments"));
    builder.Configuration.AddAzureKeyVault(keyVaultUri, new DefaultAzureCredential());
}
```

`DefaultAzureCredential` resolves to **Managed Identity** when running on Azure (App Service / Container Apps / AKS), and falls back to `az login` credentials for local production-config testing.

### DataProtection key persistence

Closes SEC-MEDIUM-004 (the `<machineKey>` gap):

```csharp
if (!builder.Environment.IsDevelopment())
{
    builder.Services
        .AddDataProtection()
        .PersistKeysToAzureBlobStorage(
            builder.Configuration["DataProtection:BlobStorageUri"],
            "dataprotection-keys",
            new DefaultAzureCredential())
        .ProtectKeysWithAzureKeyVault(
            new Uri(builder.Configuration["DataProtection:KeyVaultKeyUri"]),
            new DefaultAzureCredential());
}
```

This means cookies and anti-forgery tokens survive app restarts and load-balancer failovers (the original SEC-MEDIUM-004 risk).

### Secret-name convention

| Secret | Key Vault name | Source environment variable / config key |
|---|---|---|
| Entra ID Client Secret | `AzureAd--ClientSecret` | `AzureAd:ClientSecret` |
| Azure SQL connection string (Managed Identity, no secret) | n/a (use connection string with `Authentication=Active Directory Default`) | `ConnectionStrings:SchoolContext` |
| Application Insights connection | `ApplicationInsights--ConnectionString` | `ApplicationInsights:ConnectionString` |

Key Vault uses `--` as the separator (the convention `IConfiguration` translates to `:`).

### Forbidden patterns

- ❌ Secrets in `appsettings.json` (committed to source control)
- ❌ Secrets in `appsettings.{Environment}.json` (committed to source control)
- ❌ Secrets passed via command-line arguments (visible in process listings)
- ❌ Secrets baked into container images
- ❌ Secrets in environment variables in `Dockerfile` (`ENV SECRET=...`)
- ✅ Secrets in environment variables injected at runtime (App Service config, Container Apps secrets, GitHub Actions secrets)
- ✅ Secrets fetched at startup from Key Vault via Managed Identity

---

## Rationale

### Why Key Vault (and not e.g., a `secrets.yaml` deployed alongside the app)

- Native rotation, audit trail, and access control via Entra ID
- Managed Identity eliminates the bootstrapping problem (the app does not need a Key Vault credential — it has its own identity)
- Standard pattern across Azure-deployed .NET apps
- Native `IConfiguration` integration (`AddAzureKeyVault`)

### Why `dotnet user-secrets` for dev (and not `.env` files)

- Built-in to .NET SDK; no extra package needed
- Stored outside the source tree → no risk of `.env` accidentally committed
- IDE-supported (right-click project → Manage User Secrets)

### Why Managed Identity (and not Service Principal with secret)

- No long-lived secret to leak or rotate
- Identity is bound to the Azure resource (App Service, Container App, AKS pod) — automatically revoked when resource is deleted
- Entra-backed audit trail of every Key Vault access

### Why `DefaultAzureCredential` (and not explicit `ManagedIdentityCredential`)

- Works locally (`az login`) and in production (Managed Identity) with no code change
- Falls back gracefully across multiple credential sources
- Removes the need for environment-specific credential plumbing

---

## Alternatives Considered

### Alternative 1 — HashiCorp Vault

**Rejected.** Self-hosted; adds operational burden. Excellent in multi-cloud / on-premise scenarios; overkill for an Azure-native deployment.

### Alternative 2 — AWS Secrets Manager / GCP Secret Manager

**Rejected.** Cross-cloud burden with no benefit; the deployment direction is Azure-native.

### Alternative 3 — Azure App Configuration with Key Vault references

**Considered, deferred.** Azure App Configuration is excellent for non-secret runtime config (feature flags, environment-specific settings) and supports Key Vault references for the secret subset. For an app this size, the added moving part is not yet justified. Worth revisiting if feature flags or dynamic config becomes a need.

### Alternative 4 — Environment variables only (no Key Vault)

**Rejected for production.** Acceptable for tier 2 (CI), but production secrets benefit from Key Vault's audit trail and rotation. Environment variables alone provide neither.

### Alternative 5 — Encrypted `Web.config` sections (legacy ASP.NET pattern)

**Rejected.** Legacy `aspnet_regiis.exe -pe` encrypted-config-sections approach has no .NET Core equivalent and ties secret storage to filesystem deployment.

### Alternative 6 — Hardcoded secrets (preserve current "Integrated Security=True" approach)

**Rejected.** Acceptable for LocalDB dev only. Production deployment requires real credentials; cannot use Windows Integrated Security against Azure SQL from a Linux container.

---

## Consequences

### Positive

- **Zero secrets in source control** — verified by repository scan today, enforced by tooling going forward (pre-commit hook + GitHub Secret Scanning).
- **Rotation is a Key Vault operation** — no app redeploy needed for credential rotation (Key Vault SDK fetches latest version at startup; for runtime rotation, use `IOptionsMonitor` patterns).
- **Audit trail** — every Key Vault access is logged.
- **DataProtection keys persist** across restarts and across multiple instances (closes SEC-MEDIUM-004).
- **Single mental model** — `IConfiguration["AzureAd:ClientSecret"]` works in dev (user-secrets), CI (env var), and prod (Key Vault) without code changes.

### Negative

- **First production deployment requires Key Vault provisioning + Managed Identity assignment + role assignment** (`Key Vault Secrets User` for the app's identity). This is documented as a Phase P task.
- **Local dev requires `dotnet user-secrets init` + `dotnet user-secrets set`** for each developer (one-time setup, documented in README).
- **DefaultAzureCredential failure modes can be confusing** — when Key Vault auth fails, the error is at startup config load, not at first use. Mitigation: fail-fast with clear error messages in `Program.cs`.

### Neutral

- The Key Vault is a billed Azure resource (~$0.03/10,000 operations). For an app of this size, cost is well under $1/month.
- Bicep / IaC for the Key Vault + Managed Identity + role assignment is added in the cloud-native phase (if/when the user selects that path) or in the first production-deploy increment.

---

## Reversibility

Replacing Key Vault with another secret store (e.g., HashiCorp Vault) requires a new ADR and re-doing the `IConfiguration` provider wiring. The application code (`builder.Configuration["AzureAd:ClientSecret"]`) does not change — only the provider does. Cost of switching is bounded.

---

## Sign-off

- [x] Decision-maker (orchestrator, autonomous): **selected on 2026-05-13** per user delegation
- [ ] User review (deferred until Phase A review gate)

Signed pending user review.
