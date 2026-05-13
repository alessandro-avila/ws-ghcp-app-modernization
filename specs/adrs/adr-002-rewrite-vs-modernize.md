# ADR-002: Rewrite vs Modernize — Choose Rewrite to .NET 8

- **Status:** Accepted
- **Date:** 2026-05-13
- **Phase:** Brownfield Phase A — Assessment (`rewrite-assessment` mandatory ADR)
- **Decision-maker:** alavil (delegated autonomy to orchestrator on 2026-05-13: "Work autonomously and make good decisions")
- **Project:** ContosoUniversity (`src/ContosoUniversity/`)
- **Supersedes:** none
- **Superseded by:** none
- **Related:**
  - Rewrite assessment: [specs/assessment/rewrite.md](../assessment/rewrite.md) §5 (modernize-vs-rewrite comparison)
  - Security assessment: [specs/assessment/security.md](../assessment/security.md) §0, §7 (auth must be added regardless)
  - Testability gate: [ADR-001](adr-001-testability-gate.md) (Track A green baseline confirmed)
  - Technology stack: [specs/docs/technology/stack.md](../docs/technology/stack.md)
  - Dependencies: [specs/docs/technology/dependencies.md](../docs/technology/dependencies.md)

---

## Context

The user selected `["rewrite", "security"]` as the brownfield delivery paths in the Path Selection human gate (commit `6b6d36e`). The `rewrite-assessment` skill mandates an explicit ADR comparing **rewrite** against **modernize-in-place**, with rationale for whichever is chosen.

ContosoUniversity's runtime substrate (.NET Framework 4.8.2 + EF Core 3.1.32) presents a forced decision:
- **EF Core 3.1.32 is out of support** (LTS ended December 2022). On .NET Framework 4.8, no upgrade path exists past 3.1 — EF Core 5+ requires .NET Standard 2.1 (a runtime contract .NET Framework 4.8 does not satisfy).
- **.NET Framework 4.8.2 itself** is in maintenance mode. Microsoft commits to security patches but no new features. The Windows-only deployment target excludes Linux containers, Azure Container Apps, and AKS-on-Linux.
- The user-selected security path requires **adding authentication and authorization**, which would force significant changes to `BaseController` and every controller regardless of rewrite-or-modernize.

The application is small: ~3,500 lines of C# + ~1,800 lines of Razor across 39 .cs files and 28 .cshtml files (per `specs/docs/technology/stack.md`).

---

## Decision

**Rewrite the application to .NET 8** (specific target stack defined in **ADR-003**).

The rewrite is justified by three cumulative constraints, any one of which would alone suggest rewriting; together they make modernize-in-place strictly inferior:

1. **EF Core 3.1.32 is out of support and unupgradable on .NET Framework.** Staying means freezing the ORM at an OOS version forever, with manual security backporting if anything ever breaks.
2. **The security path mandates auth/authz introduction.** This forces touching `BaseController` and every controller — the same surface area the rewrite has to touch anyway. Doing the work once at the new runtime instead of twice (once on Framework, then again at the eventual rewrite) is strictly cheaper.
3. **The codebase is small enough that rewrite cost is bounded.** The risk-adjusted estimate is 30–35 person-days for a single experienced .NET dev (per `specs/assessment/rewrite.md` §4). Modernize-in-place would cost 8–12 days but would deliver no future runway and would require revisiting the auth work later.

---

## Comparison

(See `specs/assessment/rewrite.md` §5 for the full table; key dimensions reproduced here.)

| Dimension | Modernize-in-place (stay on .NET Framework 4.8.2) | **Rewrite to .NET 8** |
|---|---|---|
| Effort | 8–12 person-days | 30–35 person-days |
| Future runtime support | Maintenance only; no .NET 9/10/… path | LTS through Nov 2026; clear upgrade path |
| Future EF Core support | Frozen at 3.1.32 (OOS Dec 2022); no upgrade path | EF Core 8 LTS through Nov 2026 |
| Cloud deployability | Windows hosts only | Linux/Windows, Container Apps, AKS, anywhere |
| Container readiness | Windows containers only (large, slow) | Linux containers (small, fast) |
| Security patch velocity | Microsoft Update Tuesday + manual NuGet patching | `dotnet outdated` + per-release advisories |
| Hiring/skills market | Shrinking | Mainstream |
| Forces auth/authz redesign | No (which is the security hole — see ADR-005) | Yes — and the rewrite is the natural moment |
| Risk of regression | Low (no code changes) | Medium (3,500 LOC refactor) |
| Decisive factor | — | EF Core 3.1.32 cannot be upgraded on Framework |

---

## Alternatives Considered

### Alternative 1 — Modernize-in-place on .NET Framework 4.8.x

**Rejected.** The EF Core 3.1.32 OOS status alone would be enough; the inability to introduce auth without touching the same surface twice eliminates any cost advantage.

### Alternative 2 — Rewrite to .NET 6 or .NET 7

**Rejected.** .NET 6 LTS support ends November 2024 (already past); .NET 7 STS support ended May 2024. Targeting either would require a same-year follow-up upgrade. .NET 8 is the current LTS through November 2026, with .NET 10 LTS following — straightforward upgrade lane.

### Alternative 3 — Rewrite to a different framework family (Spring Boot, Django, Express)

**Rejected.** The team's expertise, the existing C# codebase, the entity model, and the Razor view templates all argue for staying within the .NET ecosystem. A cross-language rewrite would discard 100% of existing artifacts and skill investment.

### Alternative 4 — Hybrid: modernize the old app + greenfield the new one (parallel run)

**Rejected.** Operational overhead of running two apps against the same database, with two auth schemes, exceeds the cost of a clean rewrite for an application of this size. This pattern is standard for 100K+ LOC enterprise apps; ContosoUniversity is two orders of magnitude smaller.

---

## Consequences

### Positive

- Future-proof runtime: .NET 8 LTS now, .NET 10 LTS later via straightforward upgrade.
- Modern toolchain: `dotnet outdated`, source-link, OpenTelemetry, AOT-readiness, hot reload.
- Linux container deployment unblocked → Azure Container Apps, AKS, anywhere.
- Auth/authz introduction happens on the modern runtime where ASP.NET Core Identity + `Microsoft.Identity.Web` are first-class.
- The Track A green-baseline regression suite (per ADR-001) acts as the rewrite safety net — every per-controller increment is gated by the captured behavioral contract.

### Negative

- **30–35 person-day investment** before any new feature ships.
- **Behavioral changes are inevitable** even with the regression net: anonymous → authenticated; in-process queue → `Channel<T>`; bundled assets → static assets or build-tool integration. Each is documented in `specs/assessment/rewrite.md`.
- **Auth introduction breaks every demo workflow** that relied on anonymous access. This is the security goal, but it is a user-facing change that needs communication.

### Neutral

- The data layer (SQL Server, 9 entities, TPH inheritance) is preserved — no data migration, only ORM-version migration.
- The PRD and FRDs do not change. The behavior contract stays the same; only the implementation substrate changes.

---

## Sign-off

- [x] Decision-maker (alavil): **approved via autonomous-mode delegation** Date: **2026-05-13**

Signed. The rewrite path is committed. ADR-003 specifies the target stack; ADR-004 specifies the migration pattern; ADR-005, ADR-006, ADR-007 specify the security architecture introduced as part of the rewrite.
