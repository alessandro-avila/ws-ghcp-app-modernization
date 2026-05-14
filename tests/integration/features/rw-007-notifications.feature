# tests/integration/features/rw-007-notifications.feature
#
# rw-007 — NotificationsController + Channel<T>+BackgroundService infrastructure
# (F-006/F-007). Replaces the legacy in-process MessageQueue shim
# (Services/NotificationService + Infrastructure/MessageQueue/*) with
# System.Threading.Channels.Channel<NotificationEnvelope> consumed by a
# BackgroundService that persists rows via INotificationService.PersistAsync,
# and a NotificationsController that exposes Index + GetNotifications +
# MarkAsRead.
#
# Depends on: rw-001 (auth + DbContext), rw-003 (DepartmentsController — used to
# trigger a CRUD-driven publish), rw-004..rw-006 (entity controllers wired to
# call INotificationService.PublishAsync after every successful SaveChanges).
#
# F-006 + F-007 acceptance criteria covered (current increment plan rw-007 ACs):
#   AC#1  Anonymous GET /Notifications redirects to /Account/SignIn
#   AC#2  Reader-role GET /Notifications returns 200 with the dashboard view
#   AC#3  Reader-role GET /Notifications/GetNotifications returns 200 with a
#         JSON envelope shape `{success, notifications, count}`
#   AC#4  POST /Notifications/MarkAsRead WITHOUT anti-forgery token returns 400
#         (sec-001 CSRF protection preserved)
#   AC#5  POST /Notifications/MarkAsRead WITH anti-forgery token returns 200
#         and persists IsRead=true (KL-NOTIF-002 fix — legacy was a no-op)
#   AC#6  Admin POST /Departments/Create publishes a notification with
#         CreatedBy=admin@contoso.test (User.Identity.Name — closes
#         SEC-CRITICAL-002 backfill)
#   AC#7  The published notification is persisted by the BackgroundService and
#         visible via GET /Notifications/GetNotifications
#
# Tags
# ----
# @rewrite        — targets the new ASP.NET Core 8 app at https://localhost:7001
# @rw-007         — sub-increment ID for filtered runs
# @feature-F-006  — FRD ID for filtered runs (notification subsystem)
# @feature-F-007  — FRD ID for filtered runs (queue diagnostics)
# @red-baseline   — scenarios expected to FAIL until rw-007 implementation lands

@rewrite @rw-007 @feature-F-006 @feature-F-007
Feature: rw-007 — NotificationsController + Channel<T> infrastructure (F-006/F-007)

  As a security-aware operator of the rewrite app
  I want notifications to be authenticated, CSRF-protected, and produced by
  every CRUD action with the correct user attribution; AND the legacy no-op
  MarkAsRead handler to actually persist read state
  So that F-006/F-007 ACs hold under role-based authorization, KL-NOTIF-002 is
  closed, and SEC-CRITICAL-002 attribution is preserved end-to-end.

  Background:
    Given the rewrite ContosoUniversity app is reachable over HTTPS

  @rw-007 @red-baseline
  Scenario: AC#1 — Anonymous GET /Notifications redirects to sign-in
    When I GET "/Notifications"
    Then the response status should be 302
    And the response Location header should start with "/Account/SignIn"

  @rw-007 @red-baseline
  Scenario: AC#2 — Reader-role GET /Notifications returns 200 with the dashboard view
    Given I have signed in as the seeded reader user
    When I GET "/Notifications"
    Then the response status should be 200
    And the response body should contain "Notifications"

  @rw-007 @red-baseline
  Scenario: AC#3 — Reader-role GET /Notifications/GetNotifications returns the JSON envelope shape
    Given I have signed in as the seeded reader user
    When I GET "/Notifications/GetNotifications"
    Then the response status should be 200
    And the response Content-Type should match "application/json"
    And the response JSON field "success" should equal true

  @rw-007 @red-baseline
  Scenario: AC#4 — POST /Notifications/MarkAsRead without anti-forgery token returns 400
    Given I have signed in as the seeded reader user
    When I POST "/Notifications/MarkAsRead" with form data:
      | id | 1 |
    Then the response status should be 400

  @rw-007 @red-baseline
  Scenario: AC#5 — POST /Notifications/MarkAsRead with anti-forgery token persists IsRead=true
    Given I have signed in as the seeded reader user
    And a seed notification exists for rw-007 with id captured
    And I have obtained an anti-forgery token from "/Notifications"
    When I POST the seeded notification mark-as-read form
    Then the response status should be 200
    And the seeded notification should be persisted with IsRead=true

  @rw-007 @red-baseline
  Scenario: AC#6 — Admin POST /Departments/Create publishes a notification attributed to the admin user
    Given I have signed in as the seeded admin user
    And I have obtained an anti-forgery token from "/Departments/Create"
    When I POST a new department with Name "rw-007-dept-create" Budget "12345" StartDate "2024-01-01" and no administrator selected
    Then the response status should be 302
    And a notification should have been persisted with EntityType "Department" Operation "CREATE" CreatedBy "admin@contoso.test"

  @rw-007 @red-baseline
  Scenario: AC#7 — The published notification is visible via /Notifications/GetNotifications
    Given I have signed in as the seeded admin user
    When I GET "/Notifications/GetNotifications"
    Then the response status should be 200
    And the response body should contain "Department"
