@rewrite @rw-001d @feature-rw-001d-hardening
Feature: Production-hardening middleware (rw-001d)
  As a security-aware operator of the rewrite app
  I want every response to carry standard security headers, and unhandled
  exceptions / unmatched routes to be intercepted by a friendly error page
  that does not leak internal stack traces
  So that the rewrite closes SEC-MEDIUM-001 (no raw exception leakage) and
  SEC-MEDIUM-002 (HTTP security headers) at the application boundary.

  # Test endpoints used by these scenarios:
  #   GET /Health                       — anonymous, returns "Healthy" text
  #   GET /this-page-does-not-exist     — 404, re-executed by StatusCodePages
  #                                       middleware to render the friendly error page
  #   GET /Diag/Throw                   — Development-only test seam that throws an
  #                                       InvalidOperationException so the exception
  #                                       handler middleware can be exercised end-to-end.
  #                                       The endpoint is mapped only when
  #                                       app.Environment.IsDevelopment() and is
  #                                       deliberately anonymous to keep the test
  #                                       hermetic (no sign-in required).

  Background:
    Given the rewrite ContosoUniversity app is reachable over HTTPS

  @rw-001d @sec-medium-002 @red-baseline
  Scenario: All five security headers are present on every response
    When I GET "/Health"
    Then the response status should be 200
    And the response should include header "Strict-Transport-Security" matching "max-age="
    And the response should include header "Content-Security-Policy" matching "default-src 'self'"
    And the response should include header "X-Frame-Options" with value "DENY"
    And the response should include header "X-Content-Type-Options" with value "nosniff"
    And the response should include header "Referrer-Policy" with value "strict-origin-when-cross-origin"

  @rw-001d @sec-medium-001 @red-baseline
  Scenario: Unmatched routes return a friendly 404 page
    When I GET "/this-page-does-not-exist"
    Then the response status should be 404
    And the response body should contain "Sorry, something went wrong"

  @rw-001d @sec-medium-001 @red-baseline
  Scenario: Server-side exceptions render a friendly page with no internal details
    When I GET "/Diag/Throw"
    Then the response status should be 500
    And the response body should contain "Sorry, something went wrong"
    And the response body should not contain "InvalidOperationException"
    And the response body should not contain "at ContosoUniversity.Web"
