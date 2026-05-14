@rewrite @rw-001b @feature-rw-001b-auth
Feature: Cookie auth + ASP.NET Core Identity dev-stub (rw-001b)
  As a security-aware operator of the rewrite app
  I want unauthenticated requests to be rejected and authenticated sessions
  to be carried by hardened cookies
  So that the rewrite enforces SEC-CRITICAL-001 (no anonymous access),
  SEC-CRITICAL-002 (server-validated authentication), and SEC-MEDIUM-003
  (cookie hardening) at the application boundary, even before the
  Microsoft Entra ID handshake lands in rw-001c.

  # Test endpoints used by these scenarios:
  #   GET /Dashboard           — protected by FallbackPolicy=RequireAuthenticatedUser
  #   GET /Account/SignIn      — anonymous, returns the dev-stub sign-in form
  #   POST /Account/SignIn     — anonymous, validates email + password against the
  #                              ASP.NET Core Identity store seeded with the two
  #                              dev users (admin@contoso.test / reader@contoso.test).
  #
  # Seeded users (dev-stub only — superseded by Entra ID in rw-001c):
  #   admin@contoso.test  — role: Admin     — password: Adm1n!Pass
  #   reader@contoso.test — role: Reader    — password: Read3r!Pass

  Background:
    Given the rewrite ContosoUniversity app is reachable over HTTPS

  @rw-001b @sec-critical-001 @red-baseline
  Scenario: Anonymous request to a protected endpoint redirects to sign-in
    When I GET "/Dashboard"
    Then the response status should be 302
    And the response Location header should start with "/Account/SignIn"

  @rw-001b @sec-critical-002 @red-baseline
  Scenario: Successful sign-in as the seeded admin grants access to the dashboard
    Given I have signed in as the seeded admin user
    When I GET "/Dashboard"
    Then the response status should be 200
    And the response body should contain "admin@contoso.test"

  @rw-001b @sec-medium-003 @red-baseline
  Scenario: The auth cookie has the __Host- prefix and security flags
    Given I have signed in as the seeded admin user
    Then a Set-Cookie header for "__Host-ContosoUniversity.Auth" should have been issued
    And that Set-Cookie header should include the HttpOnly attribute
    And that Set-Cookie header should include the Secure attribute
    And that Set-Cookie header should include "SameSite=Strict"
