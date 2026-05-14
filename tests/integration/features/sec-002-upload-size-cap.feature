# SEC-HIGH-003 — Upload size cap mismatch
# spec2cloud sec-002 — align Web.config IIS limits to 5 MB
# Linked FRD: F-002 Course Management with Teaching Materials
#
# Bug today:
#   Web.config <httpRuntime maxRequestLength="10240"/>           (10 MB in KB)
#   Web.config <requestLimits maxAllowedContentLength="10485760"/> (10 MB in bytes)
#   CoursesController.Create rejects ContentLength > 5 MB via ModelState
#
#   Effect: a 6-10 MB upload is accepted by IIS, the entire body crosses the
#   wire, ASP.NET buffers it, the controller deserializes the multipart form,
#   then returns View(course) with "File size must be less than 5MB" in HTML.
#   This is a DoS vector (eats memory/bandwidth) and contradicts F-002 NFR.
#
# Fix:
#   Set Web.config maxRequestLength="5120" + maxAllowedContentLength="5242880"
#   so IIS request filtering rejects oversized POSTs with HTTP 413 before the
#   ASP.NET pipeline (and before the application reads the body).

Feature: Course teaching-material upload size cap is enforced at the IIS layer
  As an operator of ContosoUniversity
  I want the configured Web.config request-size limits to match the application's 5 MB cap
  So that oversized uploads are rejected at IIS before consuming application resources

  @existing-behavior @feature-F-002
  Scenario: GET /Courses/Create renders the create page
    Given the legacy ContosoUniversity app is reachable at the configured base URL
    When I GET "/Courses/Create"
    Then the response status should be 200
    And the response body should contain "Create"

  @red-baseline @sec-high-003 @security
  Scenario: IIS rejects POST bodies larger than 5 MB at the request-filtering layer
    Given the legacy ContosoUniversity app is reachable at the configured base URL
    When I POST a body of 6291456 bytes to "/Courses/Create"
    Then the response status should be 413

  @existing-behavior @feature-F-002 @security
  Scenario: IIS allows POST bodies at or below the 5 MB cap to reach the application
    Given the legacy ContosoUniversity app is reachable at the configured base URL
    When I POST a body of 4194304 bytes to "/Courses/Create"
    Then the response status should not be 413
