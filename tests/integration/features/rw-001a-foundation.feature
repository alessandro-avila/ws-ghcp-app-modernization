# tests/integration/features/rw-001a-foundation.feature
#
# rw-001a — Project scaffold + EF Core 8 baseline + legacy parity (no auth yet)
# Closes: subset of rw-001 ACs (#1 dotnet build, #2 dotnet test, #5 __EFMigrationsHistory)
# FRD: none (foundation increment - no functional behavior yet)
# Spec: specs/tasks/rw-001-subtasks.md §rw-001a
#
# Pre-req: the new ASP.NET Core 8 app must be running on https://localhost:7001
#         (start with: dotnet run --project src/ContosoUniversity.Web --launch-profile https)
#         The `https` profile binds both 7001 (HTTPS) and 7000 (HTTP); the rewrite
#         Cucumber tests default to https://localhost:7001 so __Host- prefixed cookies
#         used by rw-001b are accepted.
#
# Tags
# ----
# @rewrite        — targets the new ASP.NET Core 8 app at https://localhost:7001
# @rw-001a        — sub-increment ID
# @feature-rw-001a-foundation — feature ID for filtered runs

@rewrite @rw-001a @feature-rw-001a-foundation
Feature: rw-001a - ASP.NET Core 8 project foundation boots and serves a placeholder home page

  As the spec2cloud agent delivering the rewrite foundation
  I want the new ASP.NET Core 8 MVC project at src/ContosoUniversity.Web/ to boot and serve a minimal home page
  So that subsequent sub-increments (rw-001b cookie auth, rw-001d security headers, etc.) have a working host to extend.

  Background:
    Given the rewrite ContosoUniversity app is reachable at the configured base URL

  Scenario: GET / returns 200 and the placeholder rewrite home page
    When I GET "/"
    Then the response status should be 200
    And the response body should contain "ContosoUniversity (rewrite)"

  Scenario: GET /Health returns 200 OK so the app advertises liveness for downstream sub-increments
    When I GET "/Health"
    Then the response status should be 200
    And the response body should contain "Healthy"
