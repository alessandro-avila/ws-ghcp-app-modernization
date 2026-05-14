# tests/integration/features/rw-005-courses.feature
#
# rw-005 — CoursesController CRUD + file-upload hardening (F-002) — closes SEC-HIGH-002
# Depends on: rw-001 (auth pipeline + dev-stub seeded users + DbContext + EF migrations)
#             + rw-002 (Home/Layout — Courses nav link)
#             + rw-003 (DepartmentsController + Department dropdown source for the FK)
#             + rw-004 (Bootstrap 5 form patterns + StudentsController template reuse)
#
# Closes SEC-HIGH-002 (shallow file-upload validation on Courses) across four
# attack vectors per the increment-plan rw-005 entry:
#   1. Framework-level 5 MB cap via [RequestSizeLimit(5_242_880)] (matches
#      sec-002 IIS-layer cap; AC#7 → 413 Payload Too Large).
#   2. Extension allowlist (.jpg/.jpeg/.png/.gif/.bmp); .aspx/.svg/.exe rejected
#      (AC#8 → 415 Unsupported Media Type).
#   3. Magic-byte check via SixLabors.ImageSharp.Image.Identify so a renamed
#      .aspx-as-.jpg fails the byte-signature check (also AC#8 → 415; covered
#      by xUnit unit tests on the IUploadValidator service in step-3).
#   4. Sanitized server-generated filename pattern course_{CourseID}_{Guid}{ext}
#      so user-controlled filenames cannot path-traverse (AC#9 → 400 Bad Request).
# A dedicated [Authorize(Roles=Admin,Reader)] Image action streams the stored
# file with Content-Disposition: attachment to defeat inline-render attacks
# (e.g. SVG-with-script payloads) — AC#10.
#
# ADR-006 (authorization model) — Reader role can read (Index/Details/Image),
# Admin role can mutate (Create/Edit/Delete + upload). Anonymous requests are
# redirected to /Account/SignIn by the auth pipeline. F-002 NFR-F-002-004 (CSRF
# on all mutating POSTs) is preserved via ASP.NET Core
# AutoValidateAntiforgeryTokenAttribute (already wired by rw-001b).
#
# F-002 acceptance criteria covered (current increment plan rw-005 ACs #1-#10):
#   AC#1  Anonymous GET /Courses redirects to /Account/SignIn
#   AC#2  Reader-role GET /Courses returns 200 with course list (eager-loaded
#         Department names, includes seeded "Reader-Visible Seed Course")
#   AC#3  Reader-role GET /Courses/Details/{id} returns 200 with course details
#         (Title + Credits + Department name)
#   AC#4  Reader-role POST /Courses/Create returns 403 (read-only role per ADR-006)
#   AC#5  Admin-role GET /Courses/Create returns 200 with the form including the
#         DepartmentID dropdown populated with the seeded department
#   AC#6  Admin-role POST /Courses/Create with valid form data and no file
#         returns 302 → /Courses (round-trip parity with legacy F-002 happy path)
#   AC#7  Admin-role POST /Courses/Create with multipart upload >5 MB returns
#         413 Payload Too Large (closes SEC-HIGH-002 size-cap vector)
#   AC#8  Admin-role POST /Courses/Create with disallowed extension (.aspx)
#         returns 415 Unsupported Media Type (closes SEC-HIGH-002 type-allowlist
#         vector)
#   AC#9  Admin-role POST /Courses/Create with path-traversal filename
#         (../../etc/passwd.jpg) returns 400 Bad Request (closes SEC-HIGH-002
#         path-traversal vector)
#   AC#10 Reader-role GET /Courses/Image/{id} returns 200 with Content-Disposition
#         header starting with "attachment" (closes SEC-HIGH-002 inline-render
#         attack vector)
#
# F-002 implementation notes (decisions for the rewrite, NOT a Track A capture):
#   - Course entity has a SINGLE optional TeachingMaterialImagePath field (string,
#     max 255). The upload happens as part of Create/Edit POST, NOT via a separate
#     /Materials sub-resource (legacy parity per src/ContosoUniversity/Models/Course.cs).
#   - Course.CourseID is [DatabaseGenerated(DatabaseGeneratedOption.None)] — the
#     user supplies the integer key (legacy F-002 FR-F-002-002). Cucumber scenarios
#     pick deterministic IDs in the 90000+ range to avoid colliding with hand-seeded
#     legacy data (CourseIDs 1050..4022 in the legacy seed).
#   - DepartmentID FK: SeedSchoolData inserts a dedicated Department named
#     "Rewrite-Seed-Dept-rw005" so the Department dropdown is populated and the
#     FK constraint can be satisfied. The DepartmentID value is auto-generated
#     by EF Core (IDENTITY column), so the cucumber scenarios resolve the actual
#     ID dynamically by parsing the Create form's DepartmentID dropdown — see
#     "I have resolved the seeded department ID" step.
#   - File storage: rewrite uses App_Data/uploads/teaching-materials/ (NON-web-rooted
#     path, served only via the [Authorize] Image controller action). Legacy used
#     ~/Uploads/TeachingMaterials/ with static-file serving — rejected per
#     SEC-HIGH-002 remediation (inline-render attack vector). The seed file at
#     App_Data/uploads/teaching-materials/course_99001_seed.jpg is a 3-byte
#     placeholder created by SeedSchoolData; the Image action streams it back
#     as Content-Disposition: attachment regardless of file content (RED phase
#     does not validate content).
#   - NotificationService calls are intentionally NOT ported in rw-005 (deferred
#     to rw-007 when the notification subsystem ships).
#
# Pre-req: the rewrite app must be running on https://localhost:7001
#          (start with: dotnet run --project src/ContosoUniversity.Web --launch-profile https)
#          and the SchoolContext must have at least one seeded Department + Course.
#          SeedSchoolData (extended in rw-005 step-1) inserts:
#            * Department: Name="Rewrite-Seed-Dept-rw005", Budget=100000,
#              StartDate=2024-01-01, InstructorID=<seed instructor>
#            * Course: CourseID=99001, Title="Reader-Visible Seed Course",
#              Credits=3, DepartmentID=<seeded department>,
#              TeachingMaterialImagePath="App_Data/uploads/teaching-materials/course_99001_seed.jpg"
#
# Tags
# ----
# @rewrite        — targets the new ASP.NET Core 8 app at https://localhost:7001
# @rw-005         — sub-increment ID for filtered runs
# @feature-F-002  — FRD ID for filtered runs
# @sec-high-002   — security finding closed by this increment
# @red-baseline   — scenarios that are expected to FAIL until rw-005 implementation lands

@rewrite @rw-005 @feature-F-002 @sec-high-002
Feature: rw-005 — CoursesController CRUD with role-based authorization and file-upload hardening

  As a security-aware operator of the rewrite app
  I want course endpoints to enforce role-based authorization (Reader for read,
  Admin for write) AND to harden the teaching-material file-upload path against
  the four SEC-HIGH-002 attack vectors (oversize, disallowed type, path-traversal,
  inline-render)
  So that F-002 ACs hold under both anonymous and authenticated traffic, the
  rewrite closes SEC-HIGH-002 across all four vectors, and the new Content-
  Disposition: attachment download path defeats SVG-with-script and similar
  browser-render attacks.

  Background:
    Given the rewrite ContosoUniversity app is reachable over HTTPS

  @rw-005 @red-baseline
  Scenario: AC#1 — Anonymous GET /Courses redirects to sign-in
    When I GET "/Courses"
    Then the response status should be 302
    And the response Location header should start with "/Account/SignIn"

  @rw-005 @red-baseline
  Scenario: AC#2 — Reader-role GET /Courses returns 200 with the course list (eager-loaded department names)
    Given I have signed in as the seeded reader user
    When I GET "/Courses"
    Then the response status should be 200
    And the response body should contain "Courses"
    And the response body should contain "Reader-Visible Seed Course"
    And the response body should contain "Rewrite-Seed-Dept-rw005"

  @rw-005 @red-baseline
  Scenario: AC#3 — Reader-role GET /Courses/Details/{id} returns 200 with course details
    Given I have signed in as the seeded reader user
    When I GET "/Courses/Details/99001"
    Then the response status should be 200
    And the response body should contain "Reader-Visible Seed Course"
    And the response body should contain "Rewrite-Seed-Dept-rw005"

  @rw-005 @red-baseline
  Scenario: AC#4 — Reader-role POST /Courses/Create is forbidden (read-only role)
    # Reader cannot fetch an anti-forgery token from /Courses/Create — that page
    # is Admin-only per ADR-006, so the role check returns 403 before antiforgery
    # validation runs. POSTing without a token still surfaces the role-check 403
    # (same pattern established in rw-003 + rw-004).
    Given I have signed in as the seeded reader user
    When I POST "/Courses/Create" with form data:
      | CourseID     | 99500                          |
      | Title        | rw-005-reader-attempt          |
      | Credits      | 3                              |
      | DepartmentID | 1                              |
    Then the response status should be 403

  @rw-005 @red-baseline
  Scenario: AC#5 — Admin-role GET /Courses/Create renders the form including the department dropdown
    Given I have signed in as the seeded admin user
    When I GET "/Courses/Create"
    Then the response status should be 200
    And the response body should contain "CourseID"
    And the response body should contain "Title"
    And the response body should contain "Credits"
    And the response body should contain "DepartmentID"
    And the response body should contain "Rewrite-Seed-Dept-rw005"

  @rw-005 @red-baseline
  Scenario: AC#6 — Admin-role POST /Courses/Create with valid form data and no file succeeds and redirects to the index
    Given I have signed in as the seeded admin user
    And I have obtained an anti-forgery token from "/Courses/Create"
    And I have resolved the seeded department ID
    When I POST "/Courses/Create" with the anti-forgery token and the resolved department and form data:
      | CourseID | 99100              |
      | Title    | rw-005-admin-create |
      | Credits  | 3                   |
    Then the response status should be 302
    And the response Location header should start with "/Courses"

  @rw-005 @red-baseline @sec-high-002
  Scenario: AC#7 — Admin-role POST /Courses/Create with a file >5 MB returns 413 Payload Too Large
    Given I have signed in as the seeded admin user
    And I have obtained an anti-forgery token from "/Courses/Create"
    And I have resolved the seeded department ID
    When I POST a multipart upload to "/Courses/Create" with the anti-forgery token, course form fields CourseID="99101" Title="rw-005-oversize" Credits="3", and a file field "teachingMaterialImage" of 5500000 bytes named "big.jpg" of type "image/jpeg"
    Then the response status should be 413

  @rw-005 @red-baseline @sec-high-002
  Scenario: AC#8 — Admin-role POST /Courses/Create with a disallowed extension (.aspx) returns 415 Unsupported Media Type
    Given I have signed in as the seeded admin user
    And I have obtained an anti-forgery token from "/Courses/Create"
    And I have resolved the seeded department ID
    When I POST a multipart upload to "/Courses/Create" with the anti-forgery token, course form fields CourseID="99102" Title="rw-005-aspx" Credits="3", and a file field "teachingMaterialImage" of 1024 bytes named "evil.aspx" of type "application/octet-stream"
    Then the response status should be 415

  @rw-005 @red-baseline @sec-high-002
  Scenario: AC#9 — Admin-role POST /Courses/Create with a path-traversal filename returns 400 Bad Request
    Given I have signed in as the seeded admin user
    And I have obtained an anti-forgery token from "/Courses/Create"
    And I have resolved the seeded department ID
    When I POST a multipart upload to "/Courses/Create" with the anti-forgery token, course form fields CourseID="99103" Title="rw-005-traversal" Credits="3", and a file field "teachingMaterialImage" of 1024 bytes named "../../etc/passwd.jpg" of type "image/jpeg"
    Then the response status should be 400

  @rw-005 @red-baseline @sec-high-002
  Scenario: AC#10 — Reader-role GET /Courses/Image/{id} returns 200 with Content-Disposition attachment header
    Given I have signed in as the seeded reader user
    When I GET "/Courses/Image/99001"
    Then the response status should be 200
    And the response Content-Disposition header should start with "attachment"
