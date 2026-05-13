# FRD: Instructor Management

**Feature ID**: F-003
**Status**: Draft
**Priority**: P0
**Last Updated**: 2026-05-13

## Description

Instructor Management is the CRUD surface for the `Instructor` subtype of the `Person` TPH hierarchy plus its two related entities: `OfficeAssignment` (1:1 shared-PK) and `CourseAssignment` (the many-to-many join entity between `Instructor` and `Course`). The Index page (`/Instructors`) implements a master/detail/sub-detail view: selecting an instructor reveals their assigned courses; selecting a course reveals the enrolled students for that course. The view model `InstructorIndexData` bundles all three lists into a single render.

The Create and Edit forms render a checkbox grid of all available courses so that the many-to-many relationship can be set in one form submission. The Edit POST uses `TryUpdateModel` with explicit property whitelisting — distinct from the `[Bind(Include=...)]` pattern used by the other CRUD controllers — and includes special handling for nullifying `OfficeAssignment` when its `Location` field is blank. Delete additionally **nullifies the `Department.InstructorID`** of any department that is administered by the instructor before removing the instructor row, so departments survive the deletion of their administrator.

## User Stories

### US-F-003-001: Master/detail/sub-detail browsing

**As a** Anonymous Web User
**I want to** drill from the instructor list into one instructor's course assignments and from there into one course's enrollments
**So that** I can see the full teaching graph without leaving the page

**Acceptance Criteria:**
- GIVEN I open `/Instructors/Index` THEN I see all instructors with their `OfficeAssignment.Location`
- GIVEN I add `?id={instructorId}` to the URL or click a row THEN the right pane shows that instructor's `CourseAssignments` with course titles
- GIVEN I add `?courseID={courseId}` THEN the third pane shows that course's `Enrollments` with student names
- GIVEN no `id` is provided THEN no detail pane is rendered

### US-F-003-002: View a single instructor

**As a** Anonymous Web User
**I want to** view one instructor's record
**So that** I can confirm their office and hire date

**Acceptance Criteria:**
- GIVEN a valid `id` THEN the page renders the instructor with hire date and office (if any)
- GIVEN a null `id` THEN the response is 400
- GIVEN a missing `id` THEN the response is 404

### US-F-003-003: Create an instructor with office and course assignments

**As a** Anonymous Web User
**I want to** add a new instructor and assign them to courses in one form submission
**So that** I do not have to manage course assignments separately

**Acceptance Criteria:**
- GIVEN the create form WHEN I supply `LastName`, `FirstMidName`, `HireDate`, optionally `OfficeAssignment.Location`, and zero or more `selectedCourses` checkboxes, with antiforgery token THEN a `Person` row is inserted with discriminator `Instructor`, an `OfficeAssignment` row is inserted iff `Location` is provided, `CourseAssignment` rows are inserted for each selected course, and a `CREATE` notification is enqueued
- GIVEN I submit invalid data THEN the form is re-rendered with the courses checkbox grid re-populated and any prior selections preserved

### US-F-003-004: Edit an instructor

**As a** Anonymous Web User
**I want to** change an instructor's name, hire date, office, or course assignments
**So that** records track the real teaching graph

**Acceptance Criteria:**
- GIVEN a valid `id` THEN the form is rendered with current values and the courses checkbox grid pre-checked for currently-assigned courses
- GIVEN I submit updates with antiforgery token AND `OfficeAssignment.Location` is non-empty THEN `OfficeAssignment` is upserted
- GIVEN `OfficeAssignment.Location` is null/whitespace THEN any existing `OfficeAssignment` is nullified (deleted on save)
- GIVEN `selectedCourses` is null (no checkboxes posted) THEN ALL `CourseAssignments` for the instructor are removed
- GIVEN `selectedCourses` differs from current assignments THEN the controller computes the diff and INSERTs new rows / DELETEs removed rows
- A `UPDATE` notification is enqueued on success

### US-F-003-005: Delete an instructor without orphaning departments

**As a** Anonymous Web User
**I want to** remove an instructor from the system
**So that** retired faculty no longer appear in lookups, while their administered department survives

**Acceptance Criteria:**
- GIVEN a valid `id` WHEN I confirm the delete with antiforgery token THEN any `Department.InstructorID = id` is set to NULL, the `Person` row is removed (cascading the `OfficeAssignment` and `CourseAssignment` rows), and a `DELETE` notification is enqueued
- GIVEN the operation succeeds THEN I am redirected to `/Instructors/Index`

## Functional Requirements

### FR-F-003-001: Composite Index view via `InstructorIndexData`

- **Input**: optional `id`, optional `courseID` query parameters
- **Processing**: load all instructors with `Include(i => i.OfficeAssignment).Include(i => i.CourseAssignments).ThenInclude(...)`; if `id` is set, filter Courses to that instructor's; if `courseID` is set, additionally load Enrollments for that course
- **Output**: `InstructorIndexData { Instructors, Courses, Enrollments }` bound to `Views/Instructors/Index.cshtml`
- **Error handling**: none

### FR-F-003-002: Two-step form binding via `TryUpdateModel`

- **Input**: form fields under the empty prefix; `selectedCourses[]` checkbox group
- **Processing**: load `instructorToUpdate` with includes, then call `TryUpdateModel(instructorToUpdate, "", new[]{"LastName","FirstMidName","HireDate","OfficeAssignment"})` — this binds nested `OfficeAssignment.Location` from the form
- **Output**: 302 on success; re-rendered Edit form with errors otherwise
- **Error handling**: catches `RetryLimitExceededException` from EF Core and converts to a generic ModelState error

### FR-F-003-003: Office assignment nullification

- **Input**: bound `OfficeAssignment.Location` value (possibly empty)
- **Processing**: if `string.IsNullOrWhiteSpace(instructorToUpdate.OfficeAssignment?.Location)` then `instructorToUpdate.OfficeAssignment = null`
- **Output**: EF Core treats the missing assignment as a delete
- **Error handling**: standard EF error path

### FR-F-003-004: Course-assignment synchronization

- **Input**: `string[] selectedCourses` (each entry is a `CourseID` rendered as string by the checkbox group)
- **Processing**: `UpdateInstructorCourses(selectedCourses, instructorToUpdate)` — computes the symmetric difference between currently-assigned courses and selected courses, then inserts/deletes the correct `CourseAssignment` rows
- **Output**: persisted set matches the posted set exactly
- **Error handling**: standard EF error path

### FR-F-003-005: Cascading nullification on delete

- **Input**: required `id`
- **Processing**: `db.Departments.SingleOrDefault(d => d.InstructorID == id)`; if found, set `InstructorID = null`; then `db.Instructors.Remove(instructor)`; `SaveChanges`
- **Output**: instructor removed, dependent `OfficeAssignment` and `CourseAssignment` rows cascade-removed, departments retained
- **Error handling**: any exception propagates to global handler — there is no per-action try/catch on Delete

## Non-Functional Requirements

### NFR-F-003-001: CSRF protection on all state-changing POSTs

`Create`, `Edit`, `Delete` all carry `[ValidateAntiForgeryToken]`.

### NFR-F-003-002: No authorization

Inline comment "Only admins can delete instructors" is documentation-only; no role check is performed.

### NFR-F-003-003: Single-department-administrator assumption in delete code

`SingleOrDefault(d => d.InstructorID == id)` will throw if more than one department references the same instructor. The schema does not enforce a uniqueness constraint, so this is an implicit assumption that is not codified at the data layer.

## Dependencies

| Dependency | Type | Direction | Description |
|---|---|---|---|
| F-002 Course Management | Feature | Upstream | `Course` rows must exist for the assignment checkboxes to be meaningful |
| F-004 Department Management | Feature | Bidirectional | `Department.InstructorID` references this entity; delete code mutates the dependent department |
| F-006 Real-Time Notification System | Feature | Downstream | CRUD events enqueued |
| `Person`/`Instructor` (TPH) | Data | — | `Models/Instructor.cs`, `Models/Person.cs` |
| `OfficeAssignment` | Data | — | 1:1 shared-PK with `Instructor` |
| `CourseAssignment` | Data | — | M:N join table — composite PK `(InstructorID, CourseID)` |
| `InstructorIndexData` view model | Internal | — | `Models/SchoolViewModels/InstructorIndexData.cs` |

---

## Current Implementation (Brownfield Extension)

### Files Involved

| File Path | Role |
|---|---|
| `src/ContosoUniversity/Controllers/InstructorsController.cs` | Route handlers (8 actions) |
| `src/ContosoUniversity/Controllers/BaseController.cs` | DbContext + notification helper |
| `src/ContosoUniversity/Models/Instructor.cs` | TPH subtype |
| `src/ContosoUniversity/Models/OfficeAssignment.cs` | 1:1 shared-PK related entity |
| `src/ContosoUniversity/Models/CourseAssignment.cs` | M:N join entity |
| `src/ContosoUniversity/Models/SchoolViewModels/InstructorIndexData.cs` | Composite view model |
| `src/ContosoUniversity/Views/Instructors/*.cshtml` | Index/Details/Create/Edit/Delete views |

### Architecture Pattern

The most complex CRUD controller in the application. Combines three entities in a single edit transaction and is the only controller that uses `TryUpdateModel` instead of `[Bind(Include=...)]`. Encapsulates the assignment-sync logic in a private `UpdateInstructorCourses` method (the only piece of non-trivial domain logic in the controllers).

### Test Coverage

| Test Type | Files | Assertions | Coverage |
|---|---|---|---|
| Unit | — | 0 | 0% |
| Integration | — | 0 | 0% |
| E2E | — | 0 | 0% |

### Known Limitations

- `Edit(int? id)` GET uses `db.Instructors.Single(...)`; missing `id` raises `InvalidOperationException` rather than returning the documented 404.
- `Delete` uses `SingleOrDefault` against `Department.InstructorID = id` — will throw if two departments share the same administrator (no schema constraint prevents that scenario).
- Posting the Edit form with no checkboxes will silently clear all course assignments — the user has no warning prompt.
- `OfficeAssignment.Location` of "   " (whitespace) is treated as null, which silently deletes the office assignment.
- No optimistic concurrency token on `Instructor` — last write wins.
- The Index page composes three separate SQL queries when both `id` and `courseID` are provided; not optimized.

### Integration Points

| External System | Protocol | Purpose | Config Location |
|---|---|---|---|
| SQL Server LocalDB | TCP/SQL via EF Core 3.1 | Persistent storage | `Web.config <connectionStrings>` |
| In-process notification queue (F-006) | In-memory call | CRUD eventing | `Services/NotificationQueueService.cs` |
