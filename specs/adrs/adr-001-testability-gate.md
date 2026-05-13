# ADR-001: Brownfield Testability Gate — Track A (Full Green Baseline)

- **Status:** Accepted
- **Date:** 2026-05-13
- **Phase:** Brownfield Testability Gate (between B2c and Track execution)
- **Decision-maker:** alavil
- **Project:** ContosoUniversity (`src/ContosoUniversity/`)
- **Supersedes:** none
- **Superseded by:** none
- **Related:**
  - PRD: [specs/prd.md](../prd.md) §Discrepancies Between Documentation and Implementation
  - Test discovery: [specs/docs/testing/coverage.md](../docs/testing/coverage.md)
  - Architecture: [specs/docs/architecture/overview.md](../docs/architecture/overview.md)
  - Technology stack: [specs/docs/technology/stack.md](../docs/technology/stack.md)
  - Hint pre-declared by user: `selectedPaths = ["rewrite", "security"]`

---

## Context

Per [AGENTS.md](../../AGENTS.md) §3a Testability Gate, the orchestrator MUST classify the brownfield application as **Track A (testable / green baseline)**, **Track B (documentation-only)**, or **Track Hybrid (per-feature)** before any test scaffolding can begin. The decision is locked by an ADR, recorded in `.spec2cloud/state.json`, and cannot be revised without a successor ADR.

ContosoUniversity is an ASP.NET MVC 5.2.9 web application targeting .NET Framework 4.8.2, persisted via Entity Framework Core 3.1.32 against SQL Server LocalDB. It exposes 45 HTTP endpoints across 7 controllers (per `specs/contracts/api/_routes-overview.md`). It has **zero** existing automated tests (per `specs/docs/testing/coverage.md` — 0 unit, 0 integration, 0 e2e, 5 manual verification documents).

The user has pre-declared the intent to pursue **Rewrite + Security** in Phase A path selection. A green-baseline regression suite is therefore especially valuable: it gives the rewrite path a concrete behavior contract to honor and gives the security path a verifiable starting point against which fixes can be validated without breaking documented current behavior.

---

## Decision

**Choose Track A — Full Green Baseline — for all 8 FRDs (F-001..F-008).**

Rationale: 5 of 6 testability checklist items pass with concrete evidence (item 6 is N/A — no pre-existing tests means no execution-prerequisite to satisfy). All 8 FRDs are exercisable through the same surface (HTTP + browser); no feature is blocked by an unreachable dependency. Splitting into Hybrid would add complexity without removing any blocker.

---

## Testability Checklist (verified evidence)

| # | Question | Verdict | Evidence |
|---|---|---|---|
| 1 | Can the application be built and started locally? | ✅ **Pass** | MSBuild succeeded (`MSBuild.exe ContosoUniversity.csproj -t:Build -p:Configuration=Debug` exit 0, terminal session 2026-05-13). IIS Express present at `C:\Program Files\IIS Express\iisexpress.exe`. Bindings configured for the project: HTTPS `https://localhost:44300`, HTTP `http://localhost:58801` (per `src/ContosoUniversity/.vs/ContosoUniversity/config/applicationhost.config` lines 168-171). |
| 2 | Are external dependencies reachable, mockable, or fakeable? | ✅ **Pass** | Single external dependency: SQL Server LocalDB. `sqllocaldb info` reports `MSSQLLocalDB v15.0.4382.1` installed (auto-create=Yes, last start 2026-05-13). Connection string in `Web.config` line 11 targets `(LocalDb)\MSSQLLocalDB;Initial Catalog=ContosoUniversityNoAuthEFCore`. EF Core uses `EnsureCreated` (no migrations to apply). The in-process `MessageQueueManager` is in-memory and needs no external mock. No third-party SaaS, no MSMQ broker, no Active Directory call (despite documentation drift to the contrary — see PRD §Discrepancies). |
| 3 | Can API endpoints be exercised via HTTP? | ✅ **Pass** | 45 endpoints documented in `specs/contracts/api/*.yaml`. No `[Authorize]` attribute anywhere in the codebase; every endpoint is anonymous. CSRF tokens are required only on POSTs from BaseController-derived controllers; tests can fetch the form, extract the token, and submit. F-007 POSTs require no token at all. |
| 4 | Can the UI be rendered and interacted with via browser automation? | ✅ **Pass** | Server-rendered Razor views; no SPA bootstrap step. Standard form posts with `data-*` attributes and stable URL routes — Playwright-compatible. Note: Playwright is **not yet installed** (`npx playwright --version` reports the package is missing); the green-baseline bootstrap will add `npm i -D @playwright/test @cucumber/cucumber` as the first task. |
| 5 | Is there a working dev/test environment configuration? | ✅ **Pass** | `Web.config` carries the only required setting (LocalDB connection string + `httpRuntime maxRequestLength=10240` / `executionTimeout=3600`). No environment variables, no secrets file, no `appsettings.{Env}.json` indirection. Greenfield-style config separation will be added in Phase 2 increments, but the existing app starts with `Web.config` alone. |
| 6 | Can the existing test suite be executed? | ⚪ **N/A** | No pre-existing tests (per `specs/docs/testing/coverage.md`: 0 unit, 0 integration, 0 e2e files; 5 markdown manual-verification docs only). Nothing to execute, nothing to break. |

**Verdict count:** 5 ✅ Pass · 0 ❌ Fail · 1 ⚪ N/A → **Track A** is justified.

---

## Per-FRD Track Assignment

All 8 FRDs map to **Track A**:

| Feature ID | Name | Priority | Track | Notes |
|---|---|---|---|---|
| F-001 | Student Management | P0 | A | Full CRUD + paging + sorting; KL-F-001-001 (`.Single()` bug) is a Track A green-baseline candidate that must be captured **as the current behavior** (HTTP 500 on second-page navigation), then converted to a bug-fix increment in Phase 2. |
| F-002 | Course Management with Teaching Materials | P0 | A | Full CRUD + image upload (5 MB limit, extension allow-list). Image lifecycle on edit/delete is Playwright-testable; the filesystem coupling NFR is honored by writing tests against the local `Uploads/` directory. |
| F-003 | Instructor Management | P0 | A | Master/detail/sub-detail Index requires composite query parameter handling (`?id=`, `?courseID=`); office-assignment nullification + course-assignment diff are observable via the database and the rendered checkboxes. |
| F-004 | Department Management | P0 | A | The application's only optimistic-concurrency feature. Concurrency conflict scenarios require two-tab simulation, fully scriptable in Playwright. |
| F-005 | Enrollment Statistics | P2 | A | Single page; pure read query. Trivial to capture. |
| F-006 | Real-Time Notification System | P1 | A | In-memory queue with 5 s polling means tests must wait for the next poll cycle. JS bell-icon UI is observable via the DOM. |
| F-007 | Queue Diagnostic Tools | P3 | A | Four POST forms with no CSRF token — direct POST from test runner is straightforward. KL-CSRF-001 is captured here. |
| F-008 | Static Pages | P3 | A | Four pure GET pages, including the dead-code `/Home/Unauthorized` (US-F-008-004 captures the *current* unreachability as the green baseline; any future Auth increment will add the inbound link). |

---

## Implications

### What Track A means for Phase 2 delivery

- **Bootstrap task:** create `package.json`, install `@playwright/test` and `@cucumber/cucumber`, scaffold `e2e/` and `tests/` directories per AGENTS.md §7, wire a Playwright config that points at `https://localhost:44300`.
- **Green baseline first:** before any rewrite or security increment touches code, `gherkin-generation` (mode: `capture-existing`) and `test-generation` (mode: `green-baseline`) run for each FRD. The `@existing-behavior` tagged scenarios MUST pass against the unchanged ASP.NET MVC application.
- **KL-F-001-001 special handling:** the `.Single()` bug must be captured as a green-baseline test that **asserts the current 500 response**, NOT what the spec wishes happened. The bug-fix increment then changes the test to assert HTTP 200 and changes `.Single()` → `.SingleOrDefault()` in lockstep, with the test approval as the human gate.
- **Lifecycle invariants:** when the rewrite path begins replacing controllers, the Track A green-baseline acts as the regression net that keeps each strangler-fig step honest.

### What Track A excludes

- No Track B "Expected Behavior Scenarios" markdown sections will be added to FRDs.
- No "Manual Verification Checklist" markdown sections will be generated.
- No "Testability Roadmap" sections are required (testability is already full).

### Unblocked next steps

1. Commit this ADR.
2. Update `.spec2cloud/state.json`: `testability: "full"`, `track: "A"`, `featureTracks: null` (uniform), `humanGates.testability_gate: "approved"`, `adrs.records += {...}`, `adrs.nextNumber: 2`.
3. Proceed to **Path Selection** (next mandatory human gate). User has pre-hinted `["rewrite", "security"]` but must confirm now that the testability picture is final.
4. Phase A assessments for the selected paths (`rewrite-assessment` → ADR-002, `security-assessment` → ADR-003).

---

## Alternatives Considered

### Alternative 1 — Track Hybrid (some FRDs A, some B)

**Rejected.** No FRD has a missing dependency, no FRD requires unreachable infrastructure, no FRD is built on a runtime that cannot be exercised. Hybrid would add per-feature track assignment overhead with zero corresponding benefit.

### Alternative 2 — Track B (documentation-only)

**Rejected.** The application builds, starts, and exposes 45 HTTP endpoints with no auth wall. Refusing to capture executable green-baseline tests would leave the rewrite path without a regression contract and the security path without a verification baseline. The user's pre-declared paths (Rewrite + Security) make documentation-only an actively risky choice.

### Alternative 3 — Defer the gate decision until Phase A planning

**Rejected.** AGENTS.md §3a explicitly forbids skipping the gate: "the human MUST document the rationale for the track decision in an ADR" before any track-aware delivery can begin. The decision is also a state-machine prerequisite — `state.json.testability` must be set before `selectedPaths` can be processed.

---

## Consequences

### Positive

- Every Phase 2 increment ships with executable regression coverage of current behavior.
- The rewrite path can perform component-by-component swaps with the green baseline as the safety net.
- The security path can demonstrate that fixes (e.g., KL-CSRF-001 on F-007 POSTs) preserve all other current behavior.
- The four Pass 3 minor findings (`m3-1`..`m3-4`) get naturally absorbed: `KL-AUTH-001` becomes a tagged regression in every FRD's green baseline, `KL-CSRF-001` becomes an `@existing-behavior` scenario on F-007 that the security-planner converts to `@target-behavior`.

### Negative

- Bootstrap cost: must add Node.js tooling (`package.json`, Playwright, Cucumber) to a repository that currently has none.
- Test execution requires a running IIS Express instance — slower feedback than pure unit tests would be, but unavoidable for an MVC 5 application without a service-layer abstraction.
- The KL-F-001-001 `.Single()` bug must be encoded as the *current* (broken) behavior in the green baseline — anyone reading the test in isolation may misread it as the intended behavior. Mitigation: the test must carry an inline comment pointing at KL-F-001-001 and at the planned bug-fix increment.

### Neutral

- The 5 manual-verification markdown docs already in `src/ContosoUniversity/` (per `specs/docs/testing/coverage.md`) become advisory — they do not replace the Track A regression suite, but they do inform the scenarios it captures.

---

## Sign-off

- [x] Decision-maker (alavil): **approved via chat — option (a)** Date: **2026-05-13**

Signed. The orchestrator has set `humanGates.testability_gate: "approved"`, flipped this ADR to `Accepted`, appended the audit log, and is proceeding to the Path Selection gate.
