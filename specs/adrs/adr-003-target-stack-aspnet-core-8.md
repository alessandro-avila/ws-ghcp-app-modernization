# ADR-003: Target Stack — ASP.NET Core 8 MVC + EF Core 8 + SQL Server

- **Status:** Accepted (autonomous decision; reversible by user request)
- **Date:** 2026-05-13
- **Phase:** Brownfield Phase A — Assessment (target-stack ADR for the rewrite path)
- **Decision-maker:** Orchestrator (autonomous, per user delegation: "Work autonomously and make good decisions")
- **Project:** ContosoUniversity (`src/ContosoUniversity/`)
- **Supersedes:** none
- **Superseded by:** none
- **Related:**
  - ADR-002 (rewrite vs modernize — establishes "rewrite to .NET 8" but does not name the framework)
  - Rewrite assessment: [specs/assessment/rewrite.md](../assessment/rewrite.md)
  - Technology stack: [specs/docs/technology/stack.md](../docs/technology/stack.md)
  - Data models: [specs/docs/architecture/data-models.md](../docs/architecture/data-models.md)

---

## Context

ADR-002 commits to rewriting on .NET 8 but is silent on the specific web-application paradigm (MVC vs Minimal APIs vs Razor Pages vs Blazor) and on the data-access library (EF Core vs Dapper vs raw ADO.NET). The user delegated this choice to the orchestrator.

The source application uses:
- **ASP.NET MVC 5.2.9** with Razor views (controller + action + view triad)
- **EF Core 3.1.32** (Code-First with `EnsureCreated`)
- **SQL Server LocalDB** for development

The 28 Razor views, 7 controllers (45 endpoints), and 9 entity classes represent ~80% of the codebase by file count. A target stack that preserves the controller-action-view paradigm minimizes mechanical translation cost; a paradigm change (e.g., to Blazor or to a SPA + Web API split) would discard the existing UX shape and require redesigning the front end.

---

## Decision

**Target stack for the rewrite: ASP.NET Core 8 MVC + EF Core 8 + SQL Server.**

Specific package versions (current as of 2026-05-13):
- **Runtime:** .NET 8 LTS (`net8.0`)
- **Web framework:** `Microsoft.AspNetCore.App` (shared framework — no NuGet reference needed beyond the SDK)
- **ORM:** `Microsoft.EntityFrameworkCore` 8.0.x, `Microsoft.EntityFrameworkCore.SqlServer` 8.0.x, `Microsoft.EntityFrameworkCore.Design` 8.0.x (dev-only)
- **Database driver:** `Microsoft.Data.SqlClient` 5.x (transitive)
- **Identity:** `Microsoft.AspNetCore.Identity.EntityFrameworkCore` 8.0.x + `Microsoft.Identity.Web` 3.x for Entra ID (per ADR-005)
- **Hosting:** Kestrel; reverse-proxied behind IIS in dev, Azure App Service / Container Apps in prod
- **Configuration:** `appsettings.json` + `appsettings.{Environment}.json` + `dotnet user-secrets` for dev + Azure Key Vault for prod (per ADR-007)
- **Logging:** `Microsoft.Extensions.Logging` + OpenTelemetry exporter

---

## Rationale

### 1. Paradigm preservation: MVC over Razor Pages, Blazor, or SPA + Web API

| Option | Verdict | Reason |
|---|---|---|
| **ASP.NET Core MVC** (chosen) | ✅ | Direct one-to-one mapping for every existing controller, action, and view. ~80% of controller code ports as namespace renames + DI introduction. |
| Razor Pages | ❌ | Page-centric model breaks the existing controller-action structure; would force restructuring `StudentsController.Index/Create/Edit/Delete` into 4 separate `.cshtml.cs` page handlers. Cost: extra mechanical work for no behavioral benefit. |
| Blazor (Server or WebAssembly) | ❌ | Component-tree paradigm is fundamentally different from server-rendered Razor; would require redesigning every form, every list, every navigation pattern. Behavioral parity uncertain. |
| ASP.NET Core Web API + SPA (React/Vue/Angular) | ❌ | Splits the application into two deployables, two build chains, two test surfaces. Justified only if the user wanted SPA-grade interactivity, which the FRDs do not demand. |
| Minimal APIs | ❌ | Designed for small JSON APIs without view rendering. Not suitable for the 28-view Razor surface. |

### 2. Data access: EF Core 8 over Dapper or raw ADO.NET

| Option | Verdict | Reason |
|---|---|---|
| **EF Core 8** (chosen) | ✅ | Source uses EF Core 3.1.32 with all queries written in LINQ. Direct upgrade preserves all 30+ LINQ query expressions across controllers. EF Core 8 is LTS (support through Nov 2026). The TPH inheritance (`Person` → `Student`/`Instructor`), the optimistic concurrency token (`Department.RowVersion`), and the navigation-property graph all port verbatim. |
| Dapper | ❌ | Would require rewriting every LINQ query as raw SQL. Loses the Code-First model + migration story. Inferior abstraction for the entity-relationship-heavy data layer. |
| Raw ADO.NET | ❌ | Massive regression; not considered. |

### 3. Database: SQL Server (preserved)

The source uses SQL Server LocalDB. The 9-entity schema (4 single-PK identity tables, 1 composite-PK join table, TPH inheritance, 1 rowversion) is purely relational. Switching to PostgreSQL or another database would require re-validating every query (TPH semantics, rowversion semantics, money-type handling, datetime2 mappings). No business need exists.

For production, target **Azure SQL Database** (managed-identity authentication, automated backups, threat detection, geo-replication) — but the EF provider stays the same.

### 4. .NET 8 specifically (not .NET 6 or .NET 7)

| .NET version | Status as of 2026-05-13 | Verdict |
|---|---|---|
| .NET 6 LTS | Support ended Nov 2024 | ❌ already EOL |
| .NET 7 STS | Support ended May 2024 | ❌ already EOL |
| **.NET 8 LTS** | **Support through Nov 2026** | ✅ chosen |
| .NET 9 STS | Support through May 2026 | ⚠ would force a 2026 follow-up upgrade |
| .NET 10 LTS (preview) | Not yet released | ⚠ preview |

.NET 8 is the only currently-supported LTS option that does not force an immediate follow-up upgrade.

---

## Alternatives Considered (full list)

### Alternative 1 — Stay on .NET Framework (modernize-in-place)

**Rejected** — see ADR-002.

### Alternative 2 — ASP.NET Core 8 + Blazor Server

**Rejected.** Razor view → Razor component is not a port, it's a redesign. Outside the rewrite-not-redesign mandate.

### Alternative 3 — ASP.NET Core 8 + Razor Pages

**Rejected.** Cleaner for new code, but the existing controller-action-view code structure has no benefit migrating. Adds work, removes nothing.

### Alternative 4 — ASP.NET Core 8 + Minimal APIs + React SPA

**Rejected.** Out of scope for a rewrite; this would be a full SPA migration project with its own assessment phase.

### Alternative 5 — .NET 9 (current STS as of writing)

**Rejected.** .NET 9 is STS (18-month support); LTS preferred for a foundation rewrite.

### Alternative 6 — .NET 10 (preview)

**Rejected as premature** — preview status is incompatible with a production rewrite. Once .NET 10 ships and stabilizes, an in-place upgrade from .NET 8 is straightforward (`<TargetFramework>net10.0</TargetFramework>` + `dotnet outdated --upgrade`).

### Alternative 7 — Cross-platform reframing (Spring Boot, Django, Express)

**Rejected.** Discards 100% of existing C# + Razor + EF Core artifacts and team .NET expertise.

---

## Consequences

### Positive

- **Maximum code reuse:** ~80% of existing Razor views and POCO model classes port without behavioral change.
- **Modern .NET LTS:** support through November 2026; clear upgrade lane to .NET 10 LTS.
- **Linux container ready:** can deploy to Azure Container Apps, AKS, App Service Linux.
- **First-class auth/authz integration:** ASP.NET Core Identity + Microsoft.Identity.Web are the canonical patterns on this stack.
- **EF Core 8 query stability:** the 3.1 → 8 upgrade is well-documented; client-evaluation rules are stricter (catches latent bugs).

### Negative

- **EF Core 3.1 → 8 query-translation regressions** are likely on a few queries. These will surface as test failures and are diagnosable from the error message; mitigation noted in `specs/assessment/rewrite.md` §8.
- **Bundling subsystem must be replaced.** `System.Web.Optimization` + `WebGrease` + `BundleConfig.cs` have no .NET Core equivalent. Recommended replacement: CDN `<link>`/`<script>` for Bootstrap 5.3.3 + jQuery 3.7.1 (already published to public CDNs); avoid Vite/webpack for an app this small.
- **Authentication is forced.** The current anonymous-everywhere model cannot be ported as-is to a production-bound .NET 8 deployment. ADR-005 covers the auth choice.

### Neutral

- The data layer (entities, TPH, rowversion, datetime2 column types) ports verbatim — no schema migration required (only ORM version migration).
- The Razor view layer requires `@using` namespace updates and a layout-view rewrite for bundle replacement, but no behavioral change.

---

## Reversibility

This decision can be reversed by the user at any time during Phase A or Phase P:
- Switching the target framework version (e.g., to .NET 10 once it ships) is a low-cost edit (csproj + a few NuGet bumps).
- Switching the web paradigm (e.g., to Blazor) requires a new ADR superseding this one and a re-do of the rewrite assessment §3 (translation feasibility).
- Switching the ORM (e.g., to Dapper) requires a new ADR superseding this one and a re-validation of every query.

If the user wishes to override, they should comment on the rewrite assessment or open a follow-up gate before Phase P planning begins.

---

## Sign-off

- [x] Decision-maker (orchestrator, autonomous): **selected on 2026-05-13** per user delegation
- [ ] User review (deferred until Phase A review gate)

Signed pending user review at the Phase A review human gate.
