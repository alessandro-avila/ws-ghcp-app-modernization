# FRD: Enrollment Statistics

**Feature ID**: F-005
**Status**: Draft
**Priority**: P2
**Last Updated**: 2026-05-13

## Description

Enrollment Statistics is a small read-only reporting feature exposed through the `HomeController.About()` action. It groups every `Student` row by `EnrollmentDate`, counts the rows per group, and renders the result as a table on the `/Home/About` page. The grouping is performed in memory by EF Core 3.1 (LINQ-to-Objects after the materialization) using a `GroupBy` over `db.Students`. There is no caching, no date-range filter, no per-day breakdown beyond the distinct enrollment dates that already exist in the data, and no chart — only a tabular `EnrollmentDateGroup { EnrollmentDate, StudentCount }` listing.

This is the only "analytical" surface in the application. There is no separate enrollment management feature: enrollments themselves are managed by the legacy seeded data (`DbInitializer`) and indirectly via the student-detail eager loads in F-001.

## User Stories

### US-F-005-001: View enrollment counts by date

**As a** Anonymous Web User
**I want to** see how many students enrolled on each date
**So that** I have a sense of historical enrollment volume

**Acceptance Criteria:**
- GIVEN I open `/Home/About` THEN I see a table with one row per distinct `EnrollmentDate` containing the date and the count of students who enrolled on that date
- GIVEN there are no students THEN the table is empty (no error)
- GIVEN the database is unavailable THEN the global `HandleErrorAttribute` produces an error page

## Functional Requirements

### FR-F-005-001: Group students by enrollment date

- **Input**: none
- **Processing**: read all `Student` rows; project to `IEnumerable<EnrollmentDateGroup>` via `GroupBy(s => s.EnrollmentDate).Select(g => new EnrollmentDateGroup { EnrollmentDate = g.Key, StudentCount = g.Count() })`
- **Output**: Razor view `Views/Home/About.cshtml` bound to `IEnumerable<EnrollmentDateGroup>`
- **Error handling**: none beyond the global `HandleErrorAttribute`

## Non-Functional Requirements

### NFR-F-005-001: No caching

The query runs on every page load. With the current dataset size this is acceptable, but it would not scale.

### NFR-F-005-002: Authorization

Anonymous; no role check.

### NFR-F-005-003: No filtering or drill-down

There is no date-range picker, no department filter, no instructor breakdown. The page is intentionally minimal.

## Dependencies

| Dependency | Type | Direction | Description |
|---|---|---|---|
| F-001 Student Management | Feature | Upstream | Reads from the same `Person`/`Student` data |
| `EnrollmentDateGroup` view model | Internal | — | `Models/SchoolViewModels/EnrollmentDateGroup.cs` |
| EF Core 3.1.32 + LocalDB | External | — | Persistence |

---

## Current Implementation (Brownfield Extension)

### Files Involved

| File Path | Role |
|---|---|
| `src/ContosoUniversity/Controllers/HomeController.cs` | `About()` action handler |
| `src/ContosoUniversity/Models/SchoolViewModels/EnrollmentDateGroup.cs` | Result view model |
| `src/ContosoUniversity/Views/Home/About.cshtml` | Tabular display |

### Architecture Pattern

Trivial controller action that materializes a LINQ aggregation into a view model. No service layer, no caching, no API.

### Test Coverage

| Test Type | Files | Assertions | Coverage |
|---|---|---|---|
| Unit | — | 0 | 0% |
| Integration | — | 0 | 0% |
| E2E | — | 0 | 0% |

### Known Limitations

- No filtering UI.
- The grouping is materialized after a `ToList()` if the EF Core LINQ provider cannot translate the projection — there is no explicit guard either way; current behavior depends on the provider version.
- No accessibility metadata (`<th scope>`, captions) called out in code review.

### Integration Points

| External System | Protocol | Purpose | Config Location |
|---|---|---|---|
| SQL Server LocalDB | TCP/SQL via EF Core 3.1 | Read-only query against `Person` (Student TPH subset) | `Web.config <connectionStrings>` |
