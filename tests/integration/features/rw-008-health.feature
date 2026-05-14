# tests/integration/features/rw-008-health.feature
#
# rw-008 — Replace plain-text /Health controller with ASP.NET Core
# HealthChecks middleware exposing /health (lowercase) with a SchoolContext
# DB-connectivity probe; delete the legacy src/ContosoUniversity/ directory.
#
# Closes: SEC-HIGH-004 (legacy MessageQueueExample.cs + MessageQueueTestController
#         + MVC 5 project ship in production assembly until the legacy folder is
#         removed). After rw-008 the only application that runs is the new
#         ASP.NET Core 8 app at src/ContosoUniversity.Web/.
#
# FRD: specs/frd-static-pages.md (F-008 §FR-F-008-005 health endpoint clause)
# Spec: specs/increment-plan.md §rw-008
#
# Pre-req: dotnet run --project src/ContosoUniversity.Web --launch-profile https
#         (HTTP 7000 + HTTPS 7001).
#
# Red-baseline rationale (will FAIL until rw-008 implementation lands):
#   - GET /health (lowercase): currently 404 because only the capital-H HealthController
#                              exists. Scenario asserts 200 + JSON body containing
#                              '"status":"Healthy"'.
#   - GET /health includes a DB connectivity check named 'database' in the JSON body.
#   - The legacy MVC 5 project at src/ContosoUniversity/ no longer exists in the
#     working tree (verified out-of-band by the bash skip-detection scan + the
#     dotnet build of the new project succeeding without the legacy .csproj).
#
# Regression coverage (must stay GREEN after rw-008 lands):
#   - rw-001a /Health (capital H) MUST keep returning 200 OK because downstream
#     tooling (Aspire wait, App Service liveness probes) and the rw-001a smoke
#     test still hit the capital-H endpoint. The implementation keeps the
#     HealthController OR aliases the lowercase route; the cucumber regression
#     verifies both paths return 200.
#
@rewrite @rw-008 @feature-F-008 @red-baseline
Feature: rw-008 - HealthChecks /health endpoint + legacy directory removal

  Background:
    Given the rewrite ContosoUniversity app is reachable over HTTPS

  @rw-008
  Scenario: AC#1 - Anonymous GET /health returns 200 with HealthChecks JSON body
    When I GET "/health"
    Then the response status should be 200
    And the response body should contain "Healthy"

  @rw-008
  Scenario: AC#2 - GET /health includes a 'database' connectivity check in the report
    When I GET "/health"
    Then the response status should be 200
    And the response body should contain "database"

  @rw-008
  Scenario: AC#3 - Legacy /Health (capital H) endpoint continues to return 200 for back-compat
    When I GET "/Health"
    Then the response status should be 200
    And the response body should contain "Healthy"
