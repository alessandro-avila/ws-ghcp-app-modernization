# tests/integration/features/rw-004-students.feature
#
# rw-004 - StudentsController CRUD + sort/filter/paging template (F-001)
# Depends on: rw-001 (auth pipeline + dev-stub seeded users) + rw-002 (Home/Layout)
#             + rw-003 (DepartmentsController template - copy/paste pattern reused)
#
# Establishes the sort/filter/paging template that subsequent CRUD controllers
# (Courses, Instructors) will reuse. PaginatedList<T> is ported from
# src/ContosoUniversity/PaginatedList.cs as the first reusable rewrite helper.
#
# ADR-006 (authorization model) - Reader role can read (Index/Details), Admin role can
# mutate (Create/Edit/Delete). Anonymous requests are redirected to /Account/SignIn by
# the auth pipeline. F-001 NFR-F-001-002 (CSRF on all mutating POSTs) is preserved via
# ASP.NET Core AutoValidateAntiforgeryTokenAttribute (already wired by rw-001b).
#
# F-001 acceptance criteria covered (current increment plan rw-004 ACs #1-#7):
#   AC#1 Anonymous GET /Students redirects to /Account/SignIn (auth boundary preserved)
#   AC#2 Reader-role GET /Students returns 200 with paginated student list
#   AC#3 Reader-role GET /Students?sortOrder=name_desc returns 200 with descending name sort
#   AC#4 Reader-role GET /Students?searchString=foo returns 200 with filtered list
#        (LastName or FirstMidName contains)
#   AC#5 Reader-role POST /Students/Create returns 403 Forbidden (read-only role)
#   AC#6 Admin-role full CRUD (Create round-trip verified; Edit/Delete covered by
#        the controller pattern shared with rw-003 DepartmentsController)
#   AC#7 Regression - rw-001 + rw-002 + rw-003 stay green; CSP unchanged
#        (covered by other features in the suite)
#
# F-001 implementation notes (decisions for the rewrite, NOT a Track A capture):
#   - Page size: FRD §NFR-F-001-001 documents 3 students per page; legacy code actually
#     uses 10 (drift between docs and code in the original app). The rewrite honors the
#     documented intent (pageSize = 3) so test scenarios on small seed sets are
#     deterministic. This is recorded in the rw-004 impl commit message and noted in
#     the StudentsController XML doc comment for traceability.
#   - KL-F-001-001 (.Single() bug): legacy Details uses `.Single()` which throws
#     InvalidOperationException -> HTTP 500 for missing students. The rewrite uses
#     `.SingleOrDefault()` -> HTTP 404 to honor the documented intent of US-F-001-002.
#     This is recorded in the rw-004 impl commit message and noted in the
#     StudentsController XML doc comment.
#   - Student entity does NOT have a RowVersion / [Timestamp] field in the legacy
#     schema, so optimistic-concurrency handling on Edit (the rw-003 pattern) does
#     NOT apply here. The Edit/Delete paths simply mirror the rw-003 [Authorize]
#     gating and CSRF wiring without the concurrency-token diff logic.
#   - NotificationService calls are intentionally NOT ported in rw-004 (deferred to
#     rw-007 when the notification subsystem ships).
#
# Pre-req: the rewrite app must be running on https://localhost:7001
#          (start with: dotnet run --project src/ContosoUniversity.Web --launch-profile https)
#          and the SchoolContext must have at least two seeded Student rows for
#          sort/search assertions to be deterministic. SeedSchoolData (extended in
#          rw-004 implementation) inserts:
#            * LastName="Alpha", FirstMidName="Reader-Sortable", EnrollmentDate=2024-01-01
#            * LastName="Zulu",  FirstMidName="Reader-Sortable", EnrollmentDate=2024-06-01
#          Both share the FirstMidName substring "Reader-Sortable" so the search-filter
#          scenario can use a substring that matches both, and the sort scenario can
#          use the deterministic LastName order (Alpha < Zulu).
#
# Tags
# ----
# @rewrite        - targets the new ASP.NET Core 8 app at https://localhost:7001
# @rw-004         - sub-increment ID for filtered runs
# @feature-F-001  - FRD ID for filtered runs
# @red-baseline   - scenarios that are expected to FAIL until rw-004 implementation lands

@rewrite @rw-004 @feature-F-001
Feature: rw-004 - StudentsController CRUD with sort/filter/paging and role-based authorization

  As a security-aware operator of the rewrite app
  I want student endpoints to enforce role-based authorization (Reader for read,
  Admin for write), to provide sortable/filterable/paginated index views, and to
  preserve the documented Details lookup contract (404 on miss, not 500)
  So that F-001 ACs hold under both anonymous and authenticated traffic, the
  rewrite closes SEC-CRITICAL-001 for /Students, and a reusable sort/filter/paging
  template is established for the Courses + Instructors increments that follow.

  Background:
    Given the rewrite ContosoUniversity app is reachable over HTTPS

  @rw-004 @sec-critical-001 @red-baseline
  Scenario: AC#1 - Anonymous GET /Students redirects to sign-in
    When I GET "/Students"
    Then the response status should be 302
    And the response Location header should start with "/Account/SignIn"

  @rw-004 @feature-F-001 @red-baseline
  Scenario: AC#2 - Reader-role GET /Students returns 200 with the paginated student list
    Given I have signed in as the seeded reader user
    When I GET "/Students"
    Then the response status should be 200
    And the response body should contain "Students"
    And the response body should contain "Alpha, Reader-Sortable"
    And the response body should contain "Zulu, Reader-Sortable"

  @rw-004 @feature-F-001 @red-baseline
  Scenario: AC#3 - Reader-role GET /Students?sortOrder=name_desc returns 200 with descending name sort
    Given I have signed in as the seeded reader user
    When I GET "/Students?sortOrder=name_desc"
    Then the response status should be 200
    And the response body should have "Zulu, Reader-Sortable" appear before "Alpha, Reader-Sortable"

  @rw-004 @feature-F-001 @red-baseline
  Scenario: AC#4 - Reader-role GET /Students?searchString=Alpha returns 200 with filtered list
    Given I have signed in as the seeded reader user
    When I GET "/Students?searchString=Alpha"
    Then the response status should be 200
    And the response body should contain "Alpha, Reader-Sortable"
    And the response body should not contain "Zulu, Reader-Sortable"

  @rw-004 @feature-F-001 @red-baseline
  Scenario: AC#5 - Reader-role POST /Students/Create is forbidden (read-only role)
    # Reader cannot fetch an anti-forgery token from /Students/Create - that page
    # is Admin-only per ADR-006, so the role check returns 403 before antiforgery
    # validation runs. POSTing without a token still surfaces the role-check 403,
    # which is the contract this scenario validates: a Reader cannot create a
    # Student, no matter what payload they send. (Same pattern as rw-003.)
    Given I have signed in as the seeded reader user
    When I POST "/Students/Create" with form data:
      | LastName       | rw-004-reader-attempt |
      | FirstMidName   | Forbidden             |
      | EnrollmentDate | 2026-01-01            |
    Then the response status should be 403

  @rw-004 @feature-F-001 @red-baseline
  Scenario: AC#6a - Admin-role GET /Students/Create renders the form including all required fields
    Given I have signed in as the seeded admin user
    When I GET "/Students/Create"
    Then the response status should be 200
    And the response body should contain "LastName"
    And the response body should contain "FirstMidName"
    And the response body should contain "EnrollmentDate"

  @rw-004 @feature-F-001 @red-baseline
  Scenario: AC#6b - Admin-role POST /Students/Create succeeds and redirects to the index
    Given I have signed in as the seeded admin user
    And I have obtained an anti-forgery token from "/Students/Create"
    When I POST "/Students/Create" with the anti-forgery token and form data:
      | LastName       | rw-004-admin-create |
      | FirstMidName   | Successful          |
      | EnrollmentDate | 2026-02-01          |
    Then the response status should be 302
    And the response Location header should start with "/Students"
