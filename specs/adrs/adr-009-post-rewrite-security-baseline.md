# ADR-009: Post-Rewrite Security Baseline

- **Status:** Accepted (autonomous decision; reversible via re-assessment).
- **Date:** 2026-05-15
- **Phase:** Phase 2 — Increment Delivery (final increment `sec-004`).
- **Decision-maker:** Orchestrator (autonomous, per user delegation: "act as I approve all the human gates").
- **Project:** ContosoUniversity rewrite (`src/ContosoUniversity.Web/`).
- **Related:**
  - [ADR-002](adr-002-rewrite-vs-modernize.md) — selected the rewrite path.
  - [ADR-003](adr-003-target-stack-aspnet-core-8.md) — target stack.
  - [ADR-005](adr-005-authentication.md) — authentication scheme (layered: ASP.NET Core Identity in dev, Microsoft.Identity.Web Entra ID in prod).
  - [ADR-006](adr-006-authorization.md) — authorization (FallbackPolicy default-deny + Admin/Reader role gates).
  - [ADR-007](adr-007-secrets-management.md) — secrets in Azure Key Vault (prod) / `dotnet user-secrets` (dev).
  - [ADR-008](adr-008-rw-001c-dual-mode-entra.md) — config-gated dual-mode Entra wiring.
  - [security-post-rewrite.md](../assessment/security-post-rewrite.md) — full closure walk-through.

---

## Context

The brownfield rewrite path (`rw-001`..`rw-008` + `sec-001`..`sec-003`) closed every actionable SEC-* finding from the [pre-rewrite security assessment](../assessment/security.md). With `sec-003` delivered (state commit `18e5e10`), the codebase reached a stable post-rewrite security baseline that did not exist when ADR-005..ADR-008 were authored.

ADR-005..ADR-008 are **per-decision** ADRs (one architectural decision each). They do not, individually, capture the **aggregate posture** of the application after all 11 prior increments shipped. Without an aggregate baseline ADR, future contributors would have to manually re-stitch the per-decision ADRs against the post-rewrite security re-assessment to know what "good" looks like for this codebase.

This ADR closes that gap by **freezing the aggregate baseline** as of commit `18e5e10` so that any future regression — for example, an EF Core upgrade that drifts SqlClient back below 5.1.3, or a refactor that accidentally drops `[ValidateAntiForgeryToken]` from a mutating POST — has a single reference document to assert against.

---

## Decision

The aggregate post-rewrite security baseline for `src/ContosoUniversity.Web/` is **accepted as-is** with the closure evidence captured in [security-post-rewrite.md](../assessment/security-post-rewrite.md). Every future change to the codebase MUST preserve every property listed in §Properties below.

### Properties (the things that must remain true)

| # | Property | Verified by | Closure increment |
|--:|---|---|---|
| 1 | Every endpoint requires an authenticated user unless explicitly marked `[AllowAnonymous]`. The allow-list is: `Home/Index`, `Home/Privacy`, `/health`, `Account/SignIn`, `Account/SignOut`. | FallbackPolicy in `Program.cs` + cucumber `@rw-001*` scenarios. | `rw-001`. |
| 2 | Mutating actions on Departments/Students/Courses/Instructors require the `Admin` role; read actions require Admin OR Reader. | `[Authorize(Roles="...")]` on each action + cucumber `@rw-003`/`@rw-004`/`@rw-005`/`@rw-006` AC#1..AC#3. | `rw-003`..`rw-006`. |
| 3 | Every non-GET request automatically validates anti-forgery tokens. | `AutoValidateAntiforgeryTokenAttribute` global filter + cucumber `@rw-007` AC#3/AC#4. | `rw-001`, reinforced by `rw-007`. |
| 4 | `Notification.CreatedBy` is `User.Identity.Name`, never `"System"`. | `Services/NotificationService.cs` + cucumber `@rw-007` AC#5. Historical rows backfilled by `infra/migrations/rw-007-backfill-createdby.sql`. | `rw-007`. |
| 5 | File uploads enforce: 5 MB cap → 413; extension allow-list; MIME content-type allow-list; magic-byte sniff; storage in `App_Data/uploads/` (NON-web-rooted). | `[RequestSizeLimit]` + `IUploadValidator` xUnit tests + cucumber `@rw-005` AC#7. | `rw-005`. |
| 6 | `/health` is anonymous, returns JSON, and includes a `database` connectivity probe. | `MapHealthChecks("/health").AllowAnonymous()` + cucumber `@rw-008` AC#1..AC#3 + xUnit `HealthEndpointReturnsJsonReportWithDatabaseCheck`. | `rw-008`. |
| 7 | `Microsoft.Data.SqlClient` is on the supported 5.1.x branch (≥ 5.1.3 to clear CVE-2024-0056). | `dotnet list package --include-transitive` (sec-003 evidence). | `sec-003`. |
| 8 | HSTS, X-Content-Type-Options, Referrer-Policy, X-Frame-Options, HTTPS redirection, HttpOnly + Secure + SameSite=Strict cookies are all on. | `Program.cs` middleware registration. | `rw-001`, `rw-002`. |
| 9 | The legacy `src/ContosoUniversity/` MVC 5 codebase no longer exists in the repo; the new app at `src/ContosoUniversity.Web/` is the SOLE codebase. | `git ls-files src/` shows only `ContosoUniversity.Web/` and `ContosoUniversity.Web.UnitTests/`. | `rw-008`. |
| 10 | Structured logging via `ILogger<T>`; no `Debug.WriteLine` / `Trace.TraceError` in production code. | `grep_search` for `Debug.WriteLine\|Trace\.TraceError` in `src/ContosoUniversity.Web/` returns 0 matches. | `rw-001`..`rw-008`. |

Any future PR that breaks any of these properties MUST be either (a) reverted, or (b) accompanied by a successor ADR that explicitly supersedes this one and re-walks the aggregate baseline.

### Out-of-Scope (Carry-Forward TODOs)

The following are tracked in [security-post-rewrite.md §6](../assessment/security-post-rewrite.md#6-carry-forward-todos-non-blocking) and are **not** properties of this baseline:

- SixLabors.ImageSharp 3.1.5 vulnerabilities (NU1903 + NU1902) — bump when upstream patches.
- CSP / SRI for jsDelivr CDN resources — add when the post-deploy hardening pass runs.
- `App_Data/uploads/` → Azure Blob Storage — swap when cloud-deployed.
- Application Insights — wire connection-string secret from Key Vault when USER ACTION provisioning lands.

---

## Consequences

### Positive

1. **Single source of truth** for the post-rewrite security baseline — future regressions have a clear reference document to assert against.
2. **Deploy-ready posture** — the codebase satisfies its own assessment, and the only blockers are USER ACTION (subscription + Entra tenant + 2 test users), not code.
3. **Auditable closure** — every SEC-* finding has a commit-ref + test-ref pair in [security-post-rewrite.md](../assessment/security-post-rewrite.md).

### Negative / Trade-offs

1. **Carry-forward TODOs are not blocking but are not zero.** The baseline accepts ImageSharp 3.1.5 vulns + missing CSP/SRI + ephemeral local-disk uploads as known acceptable risks for the demo profile. A production rollout MUST address all four §6 TODOs first.
2. **No penetration test was run** — this baseline is static-assessment only. A dynamic test (DAST or pentest) is recommended before any public-facing deploy.
3. **No threat model artifact (STRIDE/PASTA)** — the assessment used OWASP Top 10:2021 as the framework. Future contributors who want a per-flow threat model should produce one as a separate artifact under `specs/assessment/`.

### Reversibility

This ADR is reversible: superseding it requires (a) a new ADR (e.g. ADR-010) with status=Accepted that explicitly says "supersedes ADR-009", and (b) a new version of `security-post-rewrite.md` capturing the new baseline. Until then, ADR-009 is canonical.

---

## USER ACTION — None Required for This ADR

No USER ACTION is required to **accept this baseline** — it is documentation of the existing post-rewrite state, not a forward-looking commitment.

USER ACTION **is** required before any cloud deploy can validate the baseline against a live URL. See [ADR-008 §USER ACTION](adr-008-rw-001c-dual-mode-entra.md) for the canonical list (Azure subscription + `azd auth login` + Entra tenant + 2 test users).

---

## Notes

- The increment plan originally referred to this artifact as "ADR-008 post-rewrite security baseline." During delivery, ADR-008 was claimed by the rw-001c dual-mode Entra reconciliation (delivered earlier, before sec-004 was authored), so this ADR is renumbered to **ADR-009** to avoid collision. The increment-plan reference is still consistent — the ID `ADR-008-pending` in `state.json` `brownfield.increments[sec-004].adrs` resolves to this file (ADR-009) and is updated in the same commit that introduces this ADR.
