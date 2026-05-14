# language: en
@legacy @feature-F-006 @notifications
Feature: Notifications — anti-forgery protection on MarkAsRead
  As an administrator of the ContosoUniversity application
  I need state-changing notification endpoints to require an anti-forgery token
  So that an attacker cannot forge requests on behalf of an authenticated session

  Background:
    Given the legacy ContosoUniversity app is reachable at the configured base URL

  # ---------------------------------------------------------------------------
  # Green-baseline (existing behavior) — MUST PASS today on the legacy app.
  # These scenarios describe what the application DOES today, before any fix.
  # ---------------------------------------------------------------------------

  @existing-behavior
  Scenario: GET /Notifications renders the admin notification dashboard
    When I GET "/Notifications"
    Then the response status should be 200
    And the response body should contain "Real-time Notifications"

  @existing-behavior
  Scenario: GET /Notifications/GetNotifications returns a JSON envelope
    When I GET "/Notifications/GetNotifications"
    Then the response status should be 200
    And the response Content-Type should match "application/json"
    And the response JSON field "success" should equal true

  # ---------------------------------------------------------------------------
  # Security delta (NEW behavior introduced by sec-001).
  # @sec-high-001 = closes finding SEC-HIGH-001 (Missing CSRF token on MarkAsRead).
  # Pre-fix:   POST without token returns 200 (vulnerable).
  # Post-fix:  POST without token is rejected (anti-forgery exception).
  # The failing test below proves the bug exists; user approval gate per
  # AGENTS.md §9 (bug-spot protocol) precedes the fix.
  # ---------------------------------------------------------------------------

  @security @sec-high-001 @red-baseline
  Scenario: MarkAsRead rejects POST without an anti-forgery token
    When I POST "/Notifications/MarkAsRead" with form data:
      | id | 1 |
    Then the response status should be one of "400, 403, 500"
    And the response body should not contain "\"success\":true"

  @security @sec-high-001 @green-after-fix
  Scenario: MarkAsRead accepts POST that includes a valid anti-forgery token
    Given I have obtained an anti-forgery token from "/Courses/Create"
    When I POST "/Notifications/MarkAsRead" with the anti-forgery token and form data:
      | id | 1 |
    Then the response status should be 200
    And the response JSON field "success" should equal true
