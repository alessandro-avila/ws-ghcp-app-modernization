# FRD: Student Management

**Feature ID**: F-001
**Status**: Draft
**Priority**: P0
**Last Updated**: 2026-05-13

> **Notation.** Acceptance Criteria marked **(CURRENT BEHAVIOR)** describe what the code does today and may diverge from documented intent. Each such item has a corresponding entry in §Known Limitations and is a Track A green-baseline test capture candidate.

## Description

Student Management is the canonical CRUD surface of ContosoUniversity for the `Student` subtype of the `Person` table-per-hierarchy. It gives any web visitor the ability to list, search, sort, paginate, view, create, edit, and delete student records, and the data flows through the unified notification pipeline (F-006) so that every successful create/update/delete becomes a notification event.

The list page (`/Students`) supports server-side substring search across last name and first name, sortable columns (Name asc/desc, Date asc/desc), and 3-row pagination using the project-local `PaginatedList<T>` helper. The detail page (`/Students/Details/{id}`) eager-loads the student's enrollments and the courses for those enrollments so course history is visible without further round-trips.

This feature is the most exercised path in the application and the one with the most user-facing affordances (search, sort, paging), making it the **primary baseline** against which any modernization or rewrite increment should be measured.

## User Stories

### US-F-001-001: List and find students

**As a** Anonymous Web User (the de facto persona)
**I want to** browse, search, and sort the student roster
**So that** I can locate a specific student record quickly

**Acceptance Criteria:**
- GIVEN the Students index page WHEN I open `/Students` THEN I see a paginated table of students sorted by `LastName` ascending by default
- GIVEN the index page WHEN I enter a substring in the search box and submit THEN the table is filtered to students whose `LastName` or `FirstMidName` contains the substring (case-insensitive per EF Core SQL collation)
- GIVEN the index page WHEN I click the `Last Name` column header THEN the sort toggles between ascending and `name_desc`
- GIVEN the index page WHEN I click the `Enrollment Date` column header THEN the sort switches to `Date` ascending or `date_desc`
- GIVEN a result set with more than 3 rows WHEN I navigate paging links THEN the active filter is preserved through the `currentFilter` query parameter

### US-F-001-002: View a student's enrollment history

**As a** Anonymous Web User
**I want to** see one student's full record including their enrollments
**So that** I can review the courses they are taking and the grades posted

**Acceptance Criteria:**
- GIVEN a valid student `id` WHEN I open `/Students/Details/{id}` THEN I see the student's name, enrollment date, and a list of their enrollments with course titles
- GIVEN a missing `id` WHEN I open `/Students/Details` THEN the response is HTTP 400 (`HttpStatusCodeResult(BadRequest)`)
- **(CURRENT BEHAVIOR)** GIVEN an `id` with no matching student WHEN I open `/Students/Details/{id}` THEN the response is HTTP 500 because `StudentsController.Details` calls `.Single()` (raises `InvalidOperationException` instead of returning the unreachable `HttpNotFoundResult`). See §Known Limitations KL-F-001-001.

### US-F-001-003: Create a student

**As a** Anonymous Web User
**I want to** add a new student to the system
**So that** they can be enrolled in courses

**Acceptance Criteria:**
- GIVEN I open `/Students/Create` THEN the form is pre-populated with `EnrollmentDate = today`
- GIVEN I submit a valid form with antiforgery token WHEN the data passes validation THEN a new `Person` row is inserted with discriminator `Student`, a notification is enqueued with operation `CREATE`, and I am redirected to `/Students/Index`
- GIVEN I submit `LastName = ""` or `FirstMidName = ""` THEN model state is invalid and the form is re-rendered with field-level errors
- GIVEN I submit `EnrollmentDate = DateTime.MinValue` THEN model state is invalid and the form is re-rendered
- GIVEN the database is unavailable THEN `DataException` is caught, a generic ModelState error is added, and the form is re-rendered

### US-F-001-004: Edit a student

**As a** Anonymous Web User
**I want to** update an existing student's name or enrollment date
**So that** records remain accurate after data-entry errors or status changes

**Acceptance Criteria:**
- GIVEN a valid student `id` WHEN I open `/Students/Edit/{id}` THEN the form is rendered with current values
- GIVEN I submit a valid edit with antiforgery token THEN `Person` is `UPDATE`-d, a notification is enqueued with operation `UPDATE`, and I am redirected to `/Students/Index`
- GIVEN I submit invalid data THEN the form is re-rendered with errors and no database mutation occurs

### US-F-001-005: Delete a student

**As a** Anonymous Web User
**I want to** remove a student from the system
**So that** stale records can be cleaned up

**Acceptance Criteria:**
- GIVEN a valid student `id` WHEN I open `/Students/Delete/{id}` THEN a confirmation page is rendered
- GIVEN I confirm the delete with antiforgery token THEN the row is removed, a notification is enqueued with operation `DELETE`, and I am redirected to `/Students/Index`
- GIVEN the delete throws an exception THEN `Trace.TraceError` is called, `TempData["ErrorMessage"]` is set, and I am redirected to `/Students/Index` with the error available for display

## Functional Requirements

### FR-F-001-001: Paginated, searchable student listing

- **Input**: optional `sortOrder`, `currentFilter`, `searchString`, `page` query parameters
- **Processing**: `db.Students.Where(...).OrderBy(...)` translated to SQL by EF Core 3.1; results materialized into `PaginatedList<Student>` (page size = 3)
- **Output**: Razor view `Views/Students/Index.cshtml` bound to `PaginatedList<Student>`
- **Error handling**: none; database errors propagate to the global `HandleErrorAttribute`

### FR-F-001-002: Student-detail eager loading

- **Input**: required path `id`
- **Processing**: `db.Students.Include(s => s.Enrollments).ThenInclude(e => e.Course).Single(s => s.ID == id)`
- **Output**: Razor view `Views/Students/Details.cshtml`
- **Error handling**: null `id` → 400; `Single()` raises `InvalidOperationException` if no match (documented as an issue, see Known Limitations)

### FR-F-001-003: Student creation with antiforgery and bind whitelisting

- **Input**: form fields `LastName`, `FirstMidName`, `EnrollmentDate` (whitelisted via `[Bind(Include=...)]`)
- **Processing**: validate model state → reject `EnrollmentDate == MinValue` → `db.Students.Add` → `SaveChanges` → enqueue notification
- **Output**: 302 redirect to `/Students/Index` on success, otherwise re-rendered create form with errors
- **Error handling**: `DataException` is caught and converted to a single generic ModelState error

### FR-F-001-004: Student update preserving navigation properties

- **Input**: form fields `ID`, `LastName`, `FirstMidName`, `EnrollmentDate`
- **Processing**: bind whitelisted properties only → `db.Entry(student).State = Modified` → `SaveChanges` → enqueue notification
- **Output**: 302 redirect on success, otherwise re-rendered edit form
- **Error handling**: `DataException` is caught and converted to a generic ModelState error

### FR-F-001-005: Student deletion with notification

- **Input**: required path `id`
- **Processing**: load student → remove → `SaveChanges` → enqueue `DELETE` notification
- **Output**: 302 redirect to `/Students/Index`
- **Error handling**: any `Exception` is logged via `Trace.TraceError` and surfaced to the user via `TempData["ErrorMessage"]`

## Non-Functional Requirements

### NFR-F-001-001: Pagination is hardcoded at 3 rows per page

The page size is a constant inside `StudentsController.Index`. There is no per-user preference, no query parameter override, and no admin setting. This may not scale gracefully past a few thousand students.

### NFR-F-001-002: CSRF protection on all state-changing POSTs

All three state-changing endpoints (`Create`, `Edit`, `Delete`) carry `[ValidateAntiForgeryToken]`. The Razor views must therefore render `@Html.AntiForgeryToken()` inside their forms.

### NFR-F-001-003: No authorization

Despite the inline comment "Admins and Teachers can view", no authorization is enforced. **Anyone who can reach the URL can perform any operation.** This is a security NFR that will likely become a target of the security path.

## Dependencies

| Dependency | Type | Direction | Description |
|---|---|---|---|
| F-006 Real-Time Notification System | Feature | Downstream consumer of CRUD events | `BaseController.SendEntityNotification` enqueues a notification on every successful create/update/delete |
| `Person` / `Student` (TPH) | Data | Upstream | All operations target the `Person` table with `Discriminator = "Student"` |
| `Enrollment`, `Course` | Data | Upstream | Eager-loaded on `/Students/Details` |
| `PaginatedList<T>` helper | Internal | — | `PaginatedList.cs` at the project root |
| `SchoolContextFactory` | Infrastructure | — | DbContext is created per request via the factory invoked by `BaseController` |
| EF Core 3.1.32 + SQL Server LocalDB | External | — | Entity Framework Core is the data-access stack; the runtime database is `(LocalDb)\MSSQLLocalDB\ContosoUniversityNoAuthEFCore` |
| ASP.NET MVC 5.2.9 antiforgery | Framework | — | Pairs with `Html.AntiForgeryToken()` in views |

---

## Current Implementation (Brownfield Extension)

### Files Involved

| File Path | Role |
|---|---|
| `src/ContosoUniversity/Controllers/StudentsController.cs` | Route handlers (8 actions) |
| `src/ContosoUniversity/Controllers/BaseController.cs` | DbContext acquisition + notification helper |
| `src/ContosoUniversity/Models/Student.cs` (TPH on `Person`) | Entity model |
| `src/ContosoUniversity/Models/Person.cs` | Base type for TPH |
| `src/ContosoUniversity/PaginatedList.cs` | Pagination helper |
| `src/ContosoUniversity/Views/Students/Index.cshtml` | List view (search/sort/paging) |
| `src/ContosoUniversity/Views/Students/Details.cshtml` | Detail view |
| `src/ContosoUniversity/Views/Students/Create.cshtml` | Create form |
| `src/ContosoUniversity/Views/Students/Edit.cshtml` | Edit form |
| `src/ContosoUniversity/Views/Students/Delete.cshtml` | Delete confirmation |
| `src/ContosoUniversity/Data/SchoolContext.cs` | EF Core DbContext (DbSet<Student>) |
| `src/ContosoUniversity/Data/DbInitializer.cs` | Seeds initial students |

### Architecture Pattern

Classic ASP.NET MVC 5 controller pattern. `StudentsController` inherits from `BaseController` which constructs the `SchoolContext` directly via `SchoolContextFactory.Create()` — there is no DI container in this application. Notifications are sent inline from controller actions via `BaseController.SendEntityNotification` rather than through a domain-events bus or interceptor.

### Test Coverage

| Test Type | Files | Assertions | Coverage |
|---|---|---|---|
| Unit | — | 0 | 0% |
| Integration | — | 0 | 0% |
| E2E | — | 0 | 0% |

**Untested paths**: 100% of this feature is untested. See `specs/docs/testing/coverage.md`.

### Known Limitations

- **KL-F-001-001 — `Details` lookup uses `.Single()` (Track A green-baseline candidate; bug-fix increment scoped).** `StudentsController.Details(int? id)` calls `db.Students.Single(...)`. A missing or unknown `id` raises `InvalidOperationException` and surfaces as HTTP 500. The documented intent is HTTP 404 via the (unreachable) `HttpNotFoundResult` branch. A bug-fix increment must change `.Single()` → `.SingleOrDefault()` and update both the AC in US-F-001-002 and the green-baseline test in lockstep — otherwise the regression net will assert the bug forever.
- Page size of 3 is hardcoded in `StudentsController.Index` and would normally be a configurable value.
- The inline comment "Admins and Teachers can view" is documentation-only — there is no role-based check.
- Concurrent edits to the same student are not detected (no concurrency token on `Person`); last write wins.
- `[Bind(Include="LastName,FirstMidName,EnrollmentDate")]` style is the legacy ASP.NET MVC 5 mass-assignment guard; modern stacks (ASP.NET Core) use input view models instead.

### Integration Points

| External System | Protocol | Purpose | Config Location |
|---|---|---|---|
| SQL Server LocalDB | TCP/SQL via EF Core 3.1 | Persistent storage | `Web.config <connectionStrings name="DefaultConnection">` |
| In-process notification queue (F-006) | In-memory method call | Activity-feed eventing | `Services/NotificationQueueService.cs`, `Web.config <appSettings key="NotificationQueuePath">` |
