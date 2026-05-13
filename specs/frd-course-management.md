# FRD: Course Management

**Feature ID**: F-002
**Status**: Draft
**Priority**: P0
**Last Updated**: 2026-05-13

## Description

Course Management lets any web visitor list, view, create, edit, and delete `Course` records and (optionally) attach a per-course **teaching-material image** that is stored on the local filesystem under `~/Uploads/TeachingMaterials/`. Each course belongs to one `Department`, and `CourseID` is **manually assigned** at create time (the `Course.CourseID` property is annotated with `DatabaseGeneratedOption.None`, so the user supplies the integer key — there is no identity column).

The image-upload subsystem validates extension (`.jpg`, `.jpeg`, `.png`, `.gif`, `.bmp`), enforces a 5 MB size cap, generates collision-free filenames in the form `course_{CourseID}_{Guid}{ext}`, and physically deletes the previous image when an edit replaces it or when the course is deleted. There is no MIME sniffing, antivirus scan, or content-type verification — only the file extension is checked.

Like every CRUD feature in the application, every successful create/update/delete enqueues a notification onto the in-process queue (F-006).

## User Stories

### US-F-002-001: List courses with department names

**As a** Anonymous Web User
**I want to** see all courses with their owning department
**So that** I can browse the catalog

**Acceptance Criteria:**
- GIVEN I open `/Courses/Index` THEN I see every course with its `CourseID`, `Title`, `Credits`, owning `Department.Name`, and a thumbnail of the teaching-material image (if one is attached)

### US-F-002-002: View a course

**As a** Anonymous Web User
**I want to** see a single course's details
**So that** I can review credits, department, and the full-size teaching-material image

**Acceptance Criteria:**
- GIVEN a valid `id` WHEN I open `/Courses/Details/{id}` THEN I see the course with department name and (if set) the full-size teaching-material image (max 300×300 per documentation)
- GIVEN a null `id` THEN the response is 400
- GIVEN an unknown `id` THEN the response is 404

### US-F-002-003: Create a course with optional image

**As a** Anonymous Web User
**I want to** add a new course to the catalog with an optional textbook image
**So that** the curriculum stays current

**Acceptance Criteria:**
- GIVEN the create form WHEN I supply `CourseID`, `Title`, `Credits`, `DepartmentID` and submit with antiforgery token THEN a row is inserted, a notification is enqueued with `CREATE`, and I am redirected to `/Courses/Index`
- GIVEN I attach a file with extension `.jpg`/`.jpeg`/`.png`/`.gif`/`.bmp` and size ≤ 5 MB THEN the file is written to `~/Uploads/TeachingMaterials/course_{CourseID}_{Guid}{ext}` and `course.TeachingMaterialImagePath` is set to the relative virtual path
- GIVEN I attach a file with a disallowed extension THEN ModelState is invalid with the message *"Only image files (JPG, PNG, GIF, BMP) are allowed."*
- GIVEN I attach a file larger than 5 MB THEN ModelState is invalid with the message *"File size cannot exceed 5MB."*
- GIVEN a `DbUpdateException` from a duplicate `CourseID` THEN the form is re-rendered with a generic database error

### US-F-002-004: Edit a course and replace the image

**As a** Anonymous Web User
**I want to** update a course's metadata and (optionally) replace its teaching-material image
**So that** the catalog stays accurate

**Acceptance Criteria:**
- GIVEN a valid `id` WHEN I post the edit form with antiforgery token THEN the row is updated and a `UPDATE` notification is enqueued
- GIVEN the course already has a `TeachingMaterialImagePath` AND I attach a valid replacement image THEN the previous file is physically deleted from `~/Uploads/TeachingMaterials/` and the new file is written
- GIVEN I do not attach a new image THEN the existing `TeachingMaterialImagePath` is preserved unchanged

### US-F-002-005: Delete a course

**As a** Anonymous Web User
**I want to** remove a course from the catalog
**So that** retired courses don't clutter the listing

**Acceptance Criteria:**
- GIVEN I confirm the delete with antiforgery token THEN the row is deleted, the on-disk image (if any) is removed, and a `DELETE` notification is enqueued
- GIVEN I am redirected to `/Courses/Index`

## Functional Requirements

### FR-F-002-001: Course listing with eager Department load

- **Input**: none
- **Processing**: `db.Courses.Include(c => c.Department).ToList()`
- **Output**: Razor view `Views/Courses/Index.cshtml` bound to `IEnumerable<Course>`
- **Error handling**: none beyond global `HandleErrorAttribute`

### FR-F-002-002: Manually-assigned `CourseID` on create

- **Input**: required `CourseID` (integer) supplied by the user
- **Processing**: standard EF Core insert; `Course.CourseID` is `[DatabaseGeneratedOption.None]` so the user value is persisted verbatim
- **Output**: 302 redirect on success; re-rendered create form on duplicate-key DB exception
- **Error handling**: catches `DbUpdateException` and adds a generic ModelState error; does not surface the duplicate-key detail

### FR-F-002-003: Image upload validation

- **Input**: optional `HttpPostedFileBase teachingMaterialImage` with `Content-Type: multipart/form-data`
- **Processing**: extension check against the allow-list, size check ≤ 5 MB, filename generation `course_{CourseID}_{Guid}{ext}`
- **Output**: writes to `~/Uploads/TeachingMaterials/`, creates the directory if missing, sets `course.TeachingMaterialImagePath` to the virtual path
- **Error handling**: per-validation ModelState error; on filesystem exception the controller re-renders the form

### FR-F-002-004: Image lifecycle on edit and delete

- **Input**: existing `course.TeachingMaterialImagePath` and an optional replacement file
- **Processing**: on edit-with-new-file → delete previous file via `Server.MapPath` then write new file; on delete-course → delete the file before removing the row
- **Output**: filesystem mutation, no API surface

### FR-F-002-005: Notification publication

- **Input**: any successful create/update/delete
- **Processing**: `notificationService.SendNotification("Course", CourseID, Title, EntityOperation.{CREATE|UPDATE|DELETE}, "System")` via `BaseController.SendEntityNotification`
- **Output**: notification enqueued onto the in-process queue (F-006)
- **Error handling**: notification exceptions are caught in `BaseController` and logged via `System.Diagnostics.Debug.WriteLine`; the user-facing operation is not affected

## Non-Functional Requirements

### NFR-F-002-001: File-extension-only upload validation

The upload check inspects `Path.GetExtension()` only — there is no MIME sniffing, no magic-number check, and no antivirus scan. A renamed `.exe.jpg` would pass validation. This is a documented security gap.

### NFR-F-002-002: 5 MB hardcoded size limit

The 5 MB limit is a constant inside `CoursesController`. There is no `Web.config <httpRuntime maxRequestLength>` or `<requestLimits>` configured to align with it, so very large uploads may be rejected at the IIS pipeline before the app sees them with a less-helpful error.

### NFR-F-002-003: Filesystem coupling defeats horizontal scaling

Images are written to a local directory under the application's physical path. A multi-instance deployment would not share these files unless additional infrastructure (Azure Files, blob storage, S3) were introduced.

### NFR-F-002-004: CSRF protection on all state-changing POSTs

`Create`, `Edit`, `Delete` all carry `[ValidateAntiForgeryToken]`.

### NFR-F-002-005: No authorization

Anyone who reaches the URL can create, edit, or delete any course. Image uploads are equally unauthenticated.

## Dependencies

| Dependency | Type | Direction | Description |
|---|---|---|---|
| F-004 Department Management | Feature | Upstream | `Course.DepartmentID` is a required FK; `Department` rows must exist before a course can be created |
| F-006 Real-Time Notification System | Feature | Downstream | All CRUD operations enqueue notifications |
| `Course` entity | Data | — | `Models/Course.cs` |
| Filesystem (`~/Uploads/TeachingMaterials/`) | External | — | Local disk; must be writable |
| ASP.NET MVC 5.2.9 + `HttpPostedFileBase` | Framework | — | Upload handling |
| EF Core 3.1.32 + LocalDB | External | — | Persistence |

---

## Current Implementation (Brownfield Extension)

### Files Involved

| File Path | Role |
|---|---|
| `src/ContosoUniversity/Controllers/CoursesController.cs` | Route handlers (7 actions, including upload handling) |
| `src/ContosoUniversity/Controllers/BaseController.cs` | DbContext + notification helper |
| `src/ContosoUniversity/Models/Course.cs` | Entity model with `[DatabaseGeneratedOption.None]` on `CourseID` |
| `src/ContosoUniversity/Views/Courses/*.cshtml` | Index/Details/Create/Edit/Delete views |
| `src/ContosoUniversity/Uploads/TeachingMaterials/` | Image storage directory (preserved in git via `.gitkeep`; image binaries are gitignored) |
| `src/ContosoUniversity/TEACHING_MATERIAL_UPLOAD.md` | Feature documentation (matches current code) |

### Architecture Pattern

ASP.NET MVC 5 controller-direct-to-DbContext, identical to F-001. No service layer for course logic. Image upload is performed inline in the controller actions; there is no `IImageStorageService` abstraction. Filesystem access uses `Server.MapPath` and direct `System.IO.File` calls.

### Test Coverage

| Test Type | Files | Assertions | Coverage |
|---|---|---|---|
| Unit | — | 0 | 0% |
| Integration | — | 0 | 0% |
| E2E | — | 0 | 0% |

### Known Limitations

- Extension-only file validation; no MIME or magic-number check (security gap).
- 5 MB constant is duplicated between `Create` and `Edit` actions; not centralized.
- `DbUpdateException` from a duplicate `CourseID` is collapsed into a generic error — the user does not learn that the ID conflicts.
- No transaction spanning the file write and the `SaveChanges` call: a crash between the two leaves an orphan file or an orphan DB row.
- The READMEs claim "automatic cleanup when courses are removed" — confirmed in `DeleteConfirmed`, but cleanup is **best-effort** (any I/O exception during file deletion is currently uncaught and would propagate).
- No virus scan, no image dimension limit, no transcoding.

### Integration Points

| External System | Protocol | Purpose | Config Location |
|---|---|---|---|
| SQL Server LocalDB | TCP/SQL via EF Core 3.1 | Persistent storage | `Web.config <connectionStrings>` |
| Local filesystem | OS file I/O | Teaching-material image storage | Hardcoded `~/Uploads/TeachingMaterials/` |
| In-process notification queue (F-006) | In-memory call | CRUD eventing | `Services/NotificationQueueService.cs` |
