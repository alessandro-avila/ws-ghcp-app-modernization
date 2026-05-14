# tests/integration/features/rw-006-instructors.feature
#
# rw-006 — InstructorsController CRUD with master/detail/sub-detail Index +
# OfficeAssignment (1:1 optional) + CourseAssignment (M:N composite key) (F-003)
#
# Depends on: rw-001 (auth pipeline + dev-stub seeded users + DbContext)
#             + rw-002 (Home/Layout — Instructors nav link)
#             + rw-003 (DepartmentsController — Department.InstructorID nullified on Delete)
#             + rw-005 (CoursesController + seeded Course 99001 used as the
#               assignment target for the cascading dropdown scenarios)
#
# F-003 acceptance criteria covered (current increment plan rw-006 ACs):
#   AC#1  Anonymous GET /Instructors redirects to /Account/SignIn
#   AC#2  Reader-role GET /Instructors returns 200 with instructor list
#         (eager-loaded OfficeAssignment.Location)
#   AC#3  Reader-role GET /Instructors/Details/{id} returns 200
#   AC#4  Reader-role GET /Instructors?id={id} returns 200 with the assigned-
#         courses panel populated for the selected instructor (master/detail)
#   AC#5  Reader-role GET /Instructors?id={id}&courseID={c} returns 200 with the
#         enrollments panel populated for the selected course (master/detail/sub)
#   AC#6  Reader-role POST /Instructors/Create returns 403 (read-only role per ADR-006)
#   AC#7  Admin-role GET /Instructors/Create returns 200 with the courses
#         checkbox grid (course titles rendered as labels)
#   AC#8  Admin-role POST /Instructors/Create with valid form data including
#         OfficeAssignment.Location and selectedCourses[] returns 302 → /Instructors
#   AC#9  Admin-role GET /Instructors/Edit/{id} returns 200 with current
#         OfficeAssignment.Location pre-filled and currently-assigned course
#         checkboxes pre-checked
#   AC#10 Admin-role POST /Instructors/Edit/{id} with modified course assignments
#         and a blank OfficeAssignment.Location returns 302 (assignments synced;
#         OfficeAssignment row deleted because Location is whitespace per FR-F-003-003)
#
# F-003 implementation notes (decisions for the rewrite, NOT a Track A capture):
#   - Class-level [Authorize(Roles="Admin,Reader")] gates Index + Details so
#     anonymous requests redirect to /Account/SignIn (cookie auth challenge).
#   - Action-level [Authorize(Roles="Admin")] on Create/Edit/Delete makes
#     reader-role POSTs return 403 BEFORE the antiforgery filter (ADR-006).
#   - InstructorIndexData composite ViewModel mirrors legacy
#     Models/SchoolViewModels/InstructorIndexData.cs (Instructors + Courses +
#     Enrollments). Cascading panes are server-rendered (no AJAX) so the
#     scenarios can assert against full-page HTML.
#   - SeedSchoolData (extended in rw-006 step-1) inserts:
#       * Instructor: LastName="Rewrite-Seed-Instructor-rw006",
#                     FirstMidName="Rew", HireDate=2024-01-01
#       * OfficeAssignment: Location="Office 99-001", InstructorID=<seed instructor>
#       * CourseAssignment: InstructorID=<seed instructor>, CourseID=99001
#         (the rw-005 seeded "Reader-Visible Seed Course")
#       * Enrollment: CourseID=99001, StudentID=<seed Alpha student>, Grade=A
#         so the AC#5 sub-detail pane has at least one row to render.
#   - NotificationService calls intentionally NOT ported in rw-006 (deferred
#     to rw-007 with the Channel<T> infrastructure).
#
# Pre-req: rewrite app running on https://localhost:7001
#          (start with: dotnet run --project src/ContosoUniversity.Web --launch-profile https)
#          and SchoolContext seeded (rw-006 SeedSchoolData extension).
#
# Tags
# ----
# @rewrite        — targets the new ASP.NET Core 8 app at https://localhost:7001
# @rw-006         — sub-increment ID for filtered runs
# @feature-F-003  — FRD ID for filtered runs
# @red-baseline   — scenarios that are expected to FAIL until rw-006 implementation lands

@rewrite @rw-006 @feature-F-003
Feature: rw-006 — InstructorsController CRUD with master/detail/sub-detail Index, OfficeAssignment, and CourseAssignment

  As a security-aware operator of the rewrite app
  I want instructor endpoints to enforce role-based authorization (Reader for
  read, Admin for write) AND to handle the OfficeAssignment 1:1 optional
  relationship plus the CourseAssignment composite-key M:N relationship in a
  single edit transaction
  So that F-003 ACs hold under both anonymous and authenticated traffic, the
  most complex CRUD controller in the application is rewritten without
  behavioral regression, and Reader users can drill from instructor → courses
  → enrollments without leaving the Index page.

  Background:
    Given the rewrite ContosoUniversity app is reachable over HTTPS

  @rw-006 @red-baseline
  Scenario: AC#1 — Anonymous GET /Instructors redirects to sign-in
    When I GET "/Instructors"
    Then the response status should be 302
    And the response Location header should start with "/Account/SignIn"

  @rw-006 @red-baseline
  Scenario: AC#2 — Reader-role GET /Instructors returns 200 with the instructor list including OfficeAssignment.Location
    Given I have signed in as the seeded reader user
    When I GET "/Instructors"
    Then the response status should be 200
    And the response body should contain "Instructors"
    And the response body should contain "Rewrite-Seed-Instructor-rw006"
    And the response body should contain "Office 99-001"

  @rw-006 @red-baseline
  Scenario: AC#3 — Reader-role GET /Instructors/Details/{id} returns 200 with instructor details
    Given I have signed in as the seeded reader user
    And I have resolved the seeded instructor ID for rw-006
    When I GET the seeded instructor details page
    Then the response status should be 200
    And the response body should contain "Rewrite-Seed-Instructor-rw006"
    And the response body should contain "Office 99-001"

  @rw-006 @red-baseline
  Scenario: AC#4 — Reader-role GET /Instructors?id={id} renders the assigned-courses panel for the selected instructor
    Given I have signed in as the seeded reader user
    And I have resolved the seeded instructor ID for rw-006
    When I GET the seeded instructor index page with the instructor selected
    Then the response status should be 200
    And the response body should contain "Reader-Visible Seed Course"

  @rw-006 @red-baseline
  Scenario: AC#5 — Reader-role GET /Instructors?id={id}&courseID={c} renders the enrollments panel for the selected course
    Given I have signed in as the seeded reader user
    And I have resolved the seeded instructor ID for rw-006
    When I GET the seeded instructor index page with the instructor and seeded course selected
    Then the response status should be 200
    And the response body should contain "Alpha"

  @rw-006 @red-baseline
  Scenario: AC#6 — Reader-role POST /Instructors/Create is forbidden (read-only role)
    # Reader cannot fetch an anti-forgery token from /Instructors/Create — that
    # page is Admin-only per ADR-006, so the role check returns 403 before
    # antiforgery validation runs (same pattern as rw-003..rw-005).
    Given I have signed in as the seeded reader user
    When I POST "/Instructors/Create" with form data:
      | LastName     | rw-006-reader-attempt |
      | FirstMidName | Rdr                   |
      | HireDate     | 2025-01-01            |
    Then the response status should be 403

  @rw-006 @red-baseline
  Scenario: AC#7 — Admin-role GET /Instructors/Create renders the form including the courses checkbox grid
    Given I have signed in as the seeded admin user
    When I GET "/Instructors/Create"
    Then the response status should be 200
    And the response body should contain "LastName"
    And the response body should contain "FirstMidName"
    And the response body should contain "HireDate"
    And the response body should contain "selectedCourses"
    And the response body should contain "Reader-Visible Seed Course"

  @rw-006 @red-baseline
  Scenario: AC#8 — Admin-role POST /Instructors/Create with OfficeAssignment.Location and selectedCourses[] succeeds and redirects
    Given I have signed in as the seeded admin user
    And I have obtained an anti-forgery token from "/Instructors/Create"
    When I POST a new instructor with LastName "rw-006-admin-create" FirstMidName "Adm" HireDate "2024-06-01" OfficeAssignmentLocation "Office 99-002" and the seeded course selected
    Then the response status should be 302
    And the response Location header should start with "/Instructors"

  @rw-006 @red-baseline
  Scenario: AC#9 — Admin-role GET /Instructors/Edit/{id} returns 200 with current OfficeAssignment and pre-checked course assignments
    Given I have signed in as the seeded admin user
    And I have resolved the seeded instructor ID for rw-006
    When I GET the seeded instructor edit page
    Then the response status should be 200
    And the response body should contain "Rewrite-Seed-Instructor-rw006"
    And the response body should contain "Office 99-001"
    And the response body should contain "Reader-Visible Seed Course"

  @rw-006 @red-baseline
  Scenario: AC#10 — Admin-role POST /Instructors/Edit/{id} with cleared OfficeAssignment.Location and updated course assignments returns 302
    Given I have signed in as the seeded admin user
    And I have resolved the seeded instructor ID for rw-006
    And I have obtained an anti-forgery token from the seeded instructor edit page
    When I POST the seeded instructor edit form with LastName "rw-006-admin-edit" FirstMidName "Rew" HireDate "2024-01-01" OfficeAssignmentLocation "" and no courses selected
    Then the response status should be 302
    And the response Location header should start with "/Instructors"
