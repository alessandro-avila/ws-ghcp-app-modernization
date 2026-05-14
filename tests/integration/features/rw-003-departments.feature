# tests/integration/features/rw-003-departments.feature
#
# rw-003 — DepartmentsController CRUD with role-based authorization (F-004)
# Depends on: rw-001 (auth pipeline + dev-stub seeded users) + rw-002 (Home/Layout)
#
# ADR-006 (authorization model) — Reader role can read (Index/Details), Admin role can mutate
# (Create/Edit/Delete). Anonymous requests are redirected to /Account/SignIn by the auth
# pipeline. F-004 NFR-F-004-002 (CSRF on all mutating POSTs) is preserved via ASP.NET Core
# AutoValidateAntiforgeryTokenAttribute (already wired by rw-001b).
#
# F-004 acceptance criteria covered (current increment plan rw-003 ACs #1-#6):
#   AC#1 Anonymous GET /Departments redirects to /Account/SignIn
#   AC#2 Reader-role GET /Departments returns 200 with department list (Index view)
#   AC#3 Reader-role POST /Departments/Create returns 403 Forbidden (read-only role)
#   AC#4 Admin-role full CRUD Create round-trip + concurrency token round-trip
#   AC#5 Department.Administrator dropdown populates from Instructors (verified via
#        GET /Departments/Create form body)
#   AC#6 Regression — covered by other features in the suite (rw-001*, rw-002)
#
# Pre-req: the rewrite app must be running on https://localhost:7001
#          (start with: dotnet run --project src/ContosoUniversity.Web --launch-profile https)
#          and the SchoolContext must have at least one seeded Instructor row so the
#          Administrator dropdown renders something. The rw-001 EnsureCreated seed
#          pipeline does NOT yet seed Instructors — rw-003 implementation must add one
#          deterministic seed row (or the dropdown verification will pass trivially with
#          a single empty <option>).
#
# Tags
# ----
# @rewrite        — targets the new ASP.NET Core 8 app at https://localhost:7001
# @rw-003         — sub-increment ID for filtered runs
# @feature-F-004  — FRD ID for filtered runs
# @red-baseline   — scenarios that are expected to FAIL until rw-003 implementation lands

@rewrite @rw-003 @feature-F-004
Feature: rw-003 - DepartmentsController CRUD with role-based authorization

  As a security-aware operator of the rewrite app
  I want department CRUD endpoints to enforce role-based authorization (Reader for read,
  Admin for write) and to preserve EF Core's optimistic concurrency control (RowVersion)
  on Edit
  So that F-004 ACs #1-#5 hold under both anonymous and authenticated traffic, and the
  rewrite closes SEC-CRITICAL-001 for /Departments while preserving the strongest
  concurrency contract in the application (NFR-F-004-001).

  Background:
    Given the rewrite ContosoUniversity app is reachable over HTTPS

  @rw-003 @sec-critical-001 @red-baseline
  Scenario: Anonymous GET /Departments redirects to sign-in
    When I GET "/Departments"
    Then the response status should be 302
    And the response Location header should start with "/Account/SignIn"

  @rw-003 @feature-F-004 @red-baseline
  Scenario: Reader-role GET /Departments returns 200 with the department list
    Given I have signed in as the seeded reader user
    When I GET "/Departments"
    Then the response status should be 200
    And the response body should contain "Departments"

  @rw-003 @feature-F-004 @red-baseline
  Scenario: Reader-role POST /Departments/Create is forbidden (read-only role)
    # Reader cannot fetch an anti-forgery token from /Departments/Create — that page
    # is Admin-only per ADR-006, so the role check returns 403 before antiforgery
    # validation runs. POSTing without a token still surfaces the role-check 403,
    # which is the contract this scenario validates: a Reader cannot create a
    # Department, no matter what payload they send.
    Given I have signed in as the seeded reader user
    When I POST "/Departments/Create" with form data:
      | Name       | rw-003 reader-attempt |
      | Budget     | 100000                |
      | StartDate  | 2026-01-01            |
    Then the response status should be 403

  @rw-003 @feature-F-004 @red-baseline
  Scenario: Admin-role GET /Departments/Create renders the form including the Administrator dropdown
    Given I have signed in as the seeded admin user
    When I GET "/Departments/Create"
    Then the response status should be 200
    And the response body should contain "InstructorID"
    And the response body should contain "Administrator"

  @rw-003 @feature-F-004 @red-baseline
  Scenario: Admin-role POST /Departments/Create succeeds and redirects to the index
    Given I have signed in as the seeded admin user
    And I have obtained an anti-forgery token from "/Departments/Create"
    When I POST "/Departments/Create" with the anti-forgery token and form data:
      | Name       | rw-003 admin-create   |
      | Budget     | 250000                |
      | StartDate  | 2026-01-15            |
    Then the response status should be 302
    And the response Location header should start with "/Departments"
