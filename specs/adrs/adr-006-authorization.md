# ADR-006: Authorization Model — Role-Based with `Admin`/`Reader` for Rewrite, Policy-Based for Future

- **Status:** Accepted (autonomous decision; reversible)
- **Date:** 2026-05-13
- **Phase:** Brownfield Phase A — Assessment (security-driven)
- **Decision-maker:** Orchestrator (autonomous, per user delegation)
- **Project:** ContosoUniversity (`src/ContosoUniversity/`)
- **Related:**
  - ADR-005 (authentication — provides the authenticated principal that authorization gates)
  - Security assessment: [specs/assessment/security.md](../assessment/security.md) §2 (SEC-CRITICAL-001), §3 (SEC-HIGH-001, SEC-HIGH-002), §7 (authz review)
  - All 8 FRDs (F-001..F-008)

---

## Context

ADR-005 introduces authentication; this ADR addresses **authorization** (deciding what an authenticated user is allowed to do). The current application has zero `[Authorize]` attributes and no role concept. After ADR-005, every user will be authenticated, but the question of "who can delete a Student?" remains open.

The PRD does not enumerate user roles; FRDs describe behaviors as if every actor is a generic "user." For an educational-management application of this size, plausible role hierarchies range from very simple (1–2 roles) to very granular (Department-Admin, Course-Instructor, Enrollment-Advisor, Read-Only-Viewer, etc.).

The principle of "start simple, refine when concrete need emerges" (Smallest Useful Set) argues against premature granularity.

---

## Decision

**Two roles for the rewrite: `Admin` and `Reader`.**

| Role | Capabilities |
|---|---|
| **`Admin`** | All CRUD operations on Students, Courses, Instructors, Departments, OfficeAssignments, CourseAssignments. Can read all Notifications. Can mark notifications as read. |
| **`Reader`** | Read-only access to all entity views (`Index`, `Details`). Can read own Notifications. CANNOT mutate. |
| **(Anonymous)** | `HomeController.Index`, `HomeController.Contact`, `HomeController.About`, `/health`, `/Account/Login`, `/Account/Logout`, `/Account/AccessDenied`. Nothing else. |

**Authorization mechanism:** `[Authorize(Roles = "Admin")]` at the controller level for all data-mutating controllers (Students, Courses, Instructors, Departments, Notifications). Read-only methods (`Index`, `Details`) are decorated with `[Authorize(Roles = "Admin,Reader")]`. `HomeController` uses `[AllowAnonymous]` on the public-facing actions.

**Default deny:** the global authorization policy is set to require an authenticated user; explicit `[AllowAnonymous]` is required for any anonymous endpoint:

```csharp
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
```

This means new controllers added in the future are authenticated by default — no risk of accidental anonymous exposure (SEC-CRITICAL-001 cannot recur).

**Future expansion** (out of Phase 2 scope, documented for forward-looking design):
- Per-resource authorization (e.g., `Instructor` can only edit their own `OfficeAssignment`) → `IAuthorizationHandler` policy with resource argument
- Department-scoped roles (e.g., `DepartmentAdmin` for the Math department only) → claim-based with `DepartmentId` claim
- These are introduced when concrete user populations dictate, not preemptively

---

## Rationale

### Why two roles, not three or seven

- The FRDs do not justify finer-grained roles. Adding more now is speculative.
- A two-role system is testable in isolation (one test for Admin, one for Reader, one for Anonymous on each protected endpoint).
- The role explosion problem ("Department.Math.Admin", "Department.History.Admin", "Course.123.Instructor") is well-known and best avoided until concrete demand surfaces.

### Why role-based, not policy-based or claim-based first

- Role-based is the simplest mental model that closes SEC-CRITICAL-001.
- Policies and claim-based authorization are strict generalizations of roles and can be added per-action without restructuring.
- ASP.NET Core supports `[Authorize(Roles = "...")]` and `[Authorize(Policy = "...")]` side-by-side.

### Why default-deny

- SEC-CRITICAL-001's root cause was that `[Authorize]` was opt-in. Inverting the default to opt-out (`[AllowAnonymous]` is the explicit decision) makes the same bug impossible.
- Aligns with OWASP A01 hardening guidance and Microsoft's Secure-by-Default ASP.NET Core docs.

### Authorization for the file-upload endpoint (SEC-HIGH-002)

`Courses/Create` and `Courses/Edit` both accept file uploads. Both are `[Authorize(Roles = "Admin")]` — Reader cannot upload. Combined with the magic-byte file validation (SEC-HIGH-002 remediation), this closes the file-upload threat at two layers (authentication + content validation).

### Authorization for `NotificationsController.MarkAsRead` (SEC-HIGH-001)

Today: `[HttpPost]` only — no CSRF token, no auth. After ADR-005 + ADR-006: `[HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,Reader")]`. The CSRF token (SEC-HIGH-001) and the role check both apply.

---

## Implementation Plan (per AGENTS.md §3a Phase 2 increment)

This ADR drives changes across **every** controller. The work is split across the existing 8-step rewrite increment ordering (ADR-004) — there is no "authorization increment"; instead, every controller's rewrite increment introduces its own `[Authorize]` decorations:

| Increment (per ADR-004) | Authorization changes |
|---|---|
| 1 (scaffold + auth + first config) | Global `FallbackPolicy = RequireAuthenticatedUser`. Roles `Admin` and `Reader` seeded into Identity DB. Test users seeded for Track A test fixtures. |
| 2 (HomeController + static pages) | `[AllowAnonymous]` on `Index`, `Contact`, `About`. Authenticated `Admin`/`Reader` can also access (no negative implication). |
| 3 (Departments) | `[Authorize(Roles = "Admin")]` controller-level; `[Authorize(Roles = "Admin,Reader")]` on `Index` and `Details`. |
| 4 (Students) | Same pattern. |
| 5 (Courses) | Same pattern. File upload only on Admin endpoints. |
| 6 (Instructors) | Same pattern. |
| 7 (Notifications) | `[Authorize(Roles = "Admin,Reader")]` on `GetNotifications`; `[Authorize(Roles = "Admin,Reader")] + [ValidateAntiForgeryToken]` on `MarkAsRead`. |
| 8 (cleanup) | `/health` is `[AllowAnonymous]`. |

---

## Alternatives Considered

### Alternative 1 — Single-role (`User`) with no role differentiation

**Rejected.** Anyone with an account could delete any record. Closes SEC-CRITICAL-001 in name only; provides no defense-in-depth against compromised low-privilege accounts.

### Alternative 2 — Per-resource authorization from day one

**Rejected.** No concrete requirement justifies the complexity. Better introduced as a follow-up when the actual user population is defined.

### Alternative 3 — Claim-based authorization (e.g., `Permission` claim with values like `"course:edit"`)

**Rejected for now.** Strictly more powerful than roles, but adds a per-permission management story (where do claims come from, how are they edited, how are they audited) that exceeds the scope of the rewrite. Roles are the smaller decision; claims can be layered on later without breaking changes.

### Alternative 4 — `[AllowAnonymous]` opt-in (the current model)

**Rejected.** This is the failure mode that produced SEC-CRITICAL-001. Inversion to default-deny is the structural fix.

### Alternative 5 — Role-Based-Access-Control (RBAC) library (e.g., FluentAuthorization)

**Rejected.** Built-in ASP.NET Core authorization is sufficient at this scale. Third-party libraries add dependency surface for marginal benefit.

---

## Consequences

### Positive

- SEC-CRITICAL-001 closed: every endpoint requires explicit `[AllowAnonymous]` or fails with 401/403.
- SEC-HIGH-001 (CSRF on `MarkAsRead`) closed by adding `[ValidateAntiForgeryToken]` alongside `[Authorize]`.
- SEC-HIGH-002 (file upload) defense-in-depth: only `Admin` can upload, and uploads are content-validated.
- Default-deny prevents future occurrences of the same root cause.

### Negative

- **Every Track A green-baseline test that exercised mutations now must authenticate as `Admin`.** Test fixtures must be updated as part of Increment 1 to seed an `Admin` user and a `Reader` user, with login helpers in the test base.
- **Role assignment in production requires Entra ID app role configuration** (or AD group → role mapping). This is a deployment-time task documented in Phase P.
- **`Reader` role is unused initially** — there is no UI for assigning it. Acceptable; documented as a hook for future organizational policies.

### Neutral

- The `AspNetRoles` table is added to the database alongside `AspNetUsers`. Schema impact is minor.
- No changes to `Notification.CreatedBy` semantics beyond what ADR-005 already specifies (`User.Identity.Name` instead of `"System"`).

---

## Reversibility

Adding a third role, switching to claim-based authorization, or adding policy-based per-resource checks is additive and does not require superseding this ADR. Removing the role distinction (collapsing back to a single role) does require a successor ADR.

---

## Sign-off

- [x] Decision-maker (orchestrator, autonomous): **selected on 2026-05-13** per user delegation
- [ ] User review (deferred until Phase A review gate)

Signed pending user review.
