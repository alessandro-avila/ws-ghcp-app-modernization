# tests/integration/features/rw-002-home-static-pages.feature
#
# rw-002 — HomeController + static pages port (legacy MVC 5 → ASP.NET Core 8)
# Closes: F-005 (Enrollment Statistics — /Home/About) + F-008 (Static Pages — /Home/Index, /Home/Contact)
# FRD: specs/frd-static-pages.md, specs/frd-enrollment-statistics.md
# Spec: specs/increment-plan.md §rw-002
#
# Pre-req: the new ASP.NET Core 8 app must be running on https://localhost:7001
#         (start with: dotnet run --project src/ContosoUniversity.Web --launch-profile https).
#         The `https` profile binds 7000 (HTTP) + 7001 (HTTPS); rewrite Cucumber tests
#         default to https://localhost:7001 so the security pipeline (CSP, __Host- cookies,
#         HSTS) introduced by rw-001b/d is exercised end-to-end.
#
# Red-baseline rationale (will FAIL until rw-002 implementation lands):
#   - GET /                : currently returns the rw-001a placeholder body
#                            "ContosoUniversity (rewrite)"; this scenario asserts the
#                            ported jumbotron text "Welcome to Contoso University".
#   - GET /Home/About      : currently returns 404 (action not yet ported); scenario
#                            asserts 200 + "Student Body Statistics" heading.
#   - GET /Home/Contact    : currently returns 404 (action not yet ported); scenario
#                            asserts 200 + the static contact card (Redmond address).
#   - Layout nav links     : current _Layout only links Home + Privacy; scenario asserts
#                            the four legacy data-area links (Students, Courses,
#                            Instructors, Departments) are present so users can still
#                            reach the legacy app from the rewrite landing page during
#                            the strangler-fig coexistence window (rw-002 → rw-007).
#
# Regression coverage (must stay GREEN after rw-002 lands):
#   - rw-001a foundation (GET / + GET /Health) — content for / changes from
#     "ContosoUniversity (rewrite)" → legacy jumbotron, but status stays 200; the
#     rw-001a content assertion will need to be updated by the rw-002 implementation
#     commit, NOT loosened or skipped.
#   - rw-001b auth pipeline (Dashboard → 302 + sign-in flow) — unaffected because /,
#     /Home/About, and /Home/Contact remain [AllowAnonymous] per ADR-006.
#   - rw-001d hardening (5 security headers + 404 / 500 friendly pages) — unaffected
#     because rw-002 only adds controllers/views, not pipeline middleware.
#
# Tags
# ----
# @rewrite        — runs against the new ASP.NET Core 8 app (https://localhost:7001)
# @rw-002         — sub-increment ID (used by `npm run test:integration:rewrite`)
# @feature-F-005  — traceability to FRD: Enrollment Statistics (/Home/About)
# @feature-F-008  — traceability to FRD: Static Pages (/Home/Index, /Home/Contact)
# @red-baseline   — scenario currently FAILS; turns green when rw-002 implementation lands

@rewrite @rw-002 @feature-F-005 @feature-F-008
Feature: rw-002 - HomeController + static pages ported to ASP.NET Core 8

  As the spec2cloud agent delivering the first rewrite cutover increment
  I want the legacy HomeController's three actions (Index, About, Contact) plus the
  shared _Layout to be re-implemented in the ASP.NET Core 8 project at the same URLs
  with equivalent content
  So that anonymous traffic to the rewrite app sees the canonical Contoso University
  landing page (not the rw-001a placeholder), the About statistics report renders, and
  the contact card is visible — while the new layout still surfaces navigation to the
  four legacy data areas (Students, Courses, Instructors, Departments) until those
  controllers are ported in rw-003..rw-006.

  Background:
    Given the rewrite ContosoUniversity app is reachable over HTTPS

  @rw-002 @feature-F-008 @red-baseline
  Scenario: AC#1 — Anonymous GET / returns 200 with the ported home jumbotron
    When I GET "/"
    Then the response status should be 200
    And the response Content-Type should match "text/html"
    And the response body should contain "Welcome to Contoso University"

  @rw-002 @feature-F-005 @red-baseline
  Scenario: AC#2 — Anonymous GET /Home/About returns 200 with the Student Body Statistics report
    When I GET "/Home/About"
    Then the response status should be 200
    And the response Content-Type should match "text/html"
    And the response body should contain "Student Body Statistics"

  @rw-002 @feature-F-008 @red-baseline
  Scenario: AC#3 — Anonymous GET /Home/Contact returns 200 with the static contact card
    When I GET "/Home/Contact"
    Then the response status should be 200
    And the response Content-Type should match "text/html"
    And the response body should contain "Redmond"

  @rw-002 @feature-F-008 @red-baseline
  Scenario: AC#5 — Layout includes navigation links to the four legacy data areas
    When I GET "/"
    Then the response status should be 200
    And the response body should contain "Students"
    And the response body should contain "Courses"
    And the response body should contain "Instructors"
    And the response body should contain "Departments"
