# FRD: Department Management

**Feature ID**: F-004
**Status**: Draft
**Priority**: P0
**Last Updated**: 2026-05-13

## Description

Department Management is the CRUD surface for the `Department` entity. It is the **only** feature in the application with optimistic concurrency control: `Department.RowVersion` is a `[Timestamp]` column that EF Core attaches to every `UPDATE` and `DELETE` predicate. When the predicate misses (because another caller has modified the same row in the meantime), the controller catches `DbUpdateConcurrencyException`, diffs the posted values against the current database row, surfaces per-property "current value" model errors, and re-renders the form with the refreshed `RowVersion` so the user can review and resubmit.

Each department has an optional `Administrator` (a foreign key to a `Person` row that is an `Instructor` per the TPH discriminator) and a `Budget` of type `decimal` (mapped to SQL `money`). Deleting a department is straightforward; Instructor deletion is the side that nullifies this FK to keep departments intact (see F-003).

## User Stories

### US-F-004-001: List departments with administrator names

**As a** Anonymous Web User
**I want to** browse all departments and see who administers each one
**So that** I have an at-a-glance view of department leadership

**Acceptance Criteria:**
- GIVEN I open `/Departments/Index` THEN I see every department's `Name`, `Budget` (formatted as currency), `StartDate`, and `Administrator.FullName` (blank when no administrator is assigned)

### US-F-004-002: View a department

**As a** Anonymous Web User
**I want to** view one department's details
**So that** I can verify its budget, start date, and administrator

**Acceptance Criteria:**
- GIVEN a valid `id` THEN the details page renders with administrator eager-loaded
- GIVEN a null `id` THEN the response is 400
- GIVEN a missing `id` THEN the response is 404

### US-F-004-003: Create a department

**As a** Anonymous Web User
**I want to** add a new department
**So that** courses can be assigned to it

**Acceptance Criteria:**
- GIVEN the create form WHEN I supply `Name`, `Budget`, `StartDate`, optionally `InstructorID`, with antiforgery token THEN a row is inserted, a `CREATE` notification is enqueued, and I am redirected to `/Departments/Index`
- `DepartmentID` is database-generated and is not bound from the form

### US-F-004-004: Edit a department with concurrency safety

**As a** Anonymous Web User
**I want to** update a department's metadata, with detection if someone else changed it first
**So that** I do not silently overwrite a colleague's edit

**Acceptance Criteria:**
- GIVEN the edit form THEN the hidden `RowVersion` field is populated from the loaded entity
- GIVEN I submit a valid edit AND no other caller has modified the row THEN the row is updated, a `UPDATE` notification is enqueued, and I am redirected to `/Departments/Index`
- GIVEN another caller has modified the row between my GET and POST THEN EF Core throws `DbUpdateConcurrencyException`; the controller diffs the posted values against the current database row and adds per-property `ModelState` errors of the form *"Current value: {value}"* for `Name`, `Budget`, `StartDate`, and `InstructorID` whenever they differ; the form is re-rendered with the refreshed `RowVersion`
- GIVEN I resubmit the form WITH the refreshed `RowVersion` and accept (or override) the conflict THEN the update succeeds
- GIVEN a `RetryLimitExceededException` or `DataException` THEN a generic ModelState error is added and the form is re-rendered

### US-F-004-005: Delete a department

**As a** Anonymous Web User
**I want to** remove an empty department
**So that** retired departments are cleaned up

**Acceptance Criteria:**
- GIVEN a valid `id` WHEN I confirm the delete with antiforgery token THEN the row is deleted and a `DELETE` notification is enqueued
- GIVEN the department has any associated `Course` rows THEN the operation may fail with a foreign-key error from EF Core (the controller does not pre-check)

## Functional Requirements

### FR-F-004-001: Department listing with administrator eager load

- **Input**: none
- **Processing**: `db.Departments.Include(d => d.Administrator).ToList()`
- **Output**: Razor view `Views/Departments/Index.cshtml` bound to `IEnumerable<Department>`
- **Error handling**: global only

### FR-F-004-002: Optimistic concurrency on Edit

- **Input**: bound `RowVersion` byte array (rendered as a hidden field, base64-encoded)
- **Processing**: `db.Entry(department).OriginalValues["RowVersion"] = department.RowVersion; db.Entry(department).State = Modified; SaveChanges`
- **Output**: success → 302; conflict → 200 with per-property errors and refreshed `RowVersion`
- **Error handling**: `DbUpdateConcurrencyException`, `RetryLimitExceededException`, `DataException`

### FR-F-004-003: Per-property conflict reporting

- **Input**: posted `Department` and the current database `Department`
- **Processing**: compare `Name`, `Budget`, `StartDate`, `InstructorID`; for each that differs, `ModelState.AddModelError(propertyName, $"Current value: {currentValue}")`
- **Output**: appears as inline field-level guidance on the re-rendered Edit form
- **Error handling**: n/a

### FR-F-004-004: Notification publication

- **Input**: any successful create/update/delete
- **Processing**: `notificationService.SendNotification("Department", DepartmentID, Name, EntityOperation.{...}, "System")`
- **Output**: notification enqueued onto the in-process queue

## Non-Functional Requirements

### NFR-F-004-001: Concurrency control is the strongest in the application

`RowVersion` provides true optimistic concurrency. **No other entity has equivalent protection.** This is a deliberate inconsistency in the codebase that any modernization effort needs to acknowledge — either extend the pattern to other entities or document why this one is special.

### NFR-F-004-002: CSRF protection on all state-changing POSTs

`Create`, `Edit`, `Delete` all carry `[ValidateAntiForgeryToken]`.

### NFR-F-004-003: No authorization

Anyone can mutate departments, including reassigning the budget.

### NFR-F-004-004: Currency type fixity

`Budget` is mapped to SQL `money` (4-decimal-place fixed-point). Migrating to a different RDBMS will need to decide on the equivalent type explicitly.

## Dependencies

| Dependency | Type | Direction | Description |
|---|---|---|---|
| F-003 Instructor Management | Feature | Upstream | `Department.InstructorID` references a `Person` (Instructor) row; delete-instructor nullifies this column |
| F-002 Course Management | Feature | Downstream | Each `Course` references a `Department` |
| F-006 Real-Time Notification System | Feature | Downstream | CRUD events enqueued |
| `Department` entity | Data | — | `Models/Department.cs` |
| EF Core 3.1.32 + LocalDB | External | — | Persistence + `[Timestamp]` row-versioning |

---

## Current Implementation (Brownfield Extension)

### Files Involved

| File Path | Role |
|---|---|
| `src/ContosoUniversity/Controllers/DepartmentsController.cs` | Route handlers (8 actions) including the concurrency-aware Edit POST |
| `src/ContosoUniversity/Controllers/BaseController.cs` | DbContext + notification helper |
| `src/ContosoUniversity/Models/Department.cs` | Entity model with `[Timestamp] RowVersion` and `[Column(TypeName = "money")] Budget` |
| `src/ContosoUniversity/Views/Departments/*.cshtml` | Views, including hidden `RowVersion` input on Edit |

### Architecture Pattern

ASP.NET MVC 5 controller-direct-to-DbContext, identical pattern to F-001/F-002 except for the concurrency handling on Edit. The conflict-resolution code in `DepartmentsController.Edit` POST is more elaborate than anything in any other controller.

### Test Coverage

| Test Type | Files | Assertions | Coverage |
|---|---|---|---|
| Unit | — | 0 | 0% |
| Integration | — | 0 | 0% |
| E2E | — | 0 | 0% |

**Untested paths**: 100%, including the concurrency-conflict reporting code which is the most complex logic in the file.

### Known Limitations

- `Delete` does not pre-check for dependent `Course` rows; deletion of a department with courses fails at SaveChanges with a less-helpful error.
- The concurrency-conflict UI surfaces per-property "current value" hints but does not clearly indicate to the user that a conflict has occurred — the user must notice the unexpected red text.
- `Department.RowVersion` is the only concurrency token in the entire schema. The pattern is not consistently applied.
- The READMEs do not document the optimistic concurrency feature.

### Integration Points

| External System | Protocol | Purpose | Config Location |
|---|---|---|---|
| SQL Server LocalDB | TCP/SQL via EF Core 3.1 | Persistent storage | `Web.config <connectionStrings>` |
| In-process notification queue (F-006) | In-memory call | CRUD eventing | `Services/NotificationQueueService.cs` |
