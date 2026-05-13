# FRD: Static Pages

**Feature ID**: F-008
**Status**: Draft
**Priority**: P3
**Last Updated**: 2026-05-13

## Description

Static Pages covers the three model-less pages served by `HomeController`: `/Home/Index` (the landing page), `/Home/Contact` (a static contact page), `/Home/Error` (the target view of the global `HandleErrorAttribute`), and `/Home/Unauthorized` (a static "unauthorized access" page that is **not currently linked from any other code path**). All four are pure Razor view renders with no model and no side effects.

The `Index` action is also the application root via the default route (`/` → `Home/Index`), making it the most-loaded page of the application. The `Error` action is what users land on when an unhandled exception escapes the MVC pipeline (because `HandleErrorAttribute` is registered globally in `App_Start/FilterConfig.cs`).

## User Stories

### US-F-008-001: Visit the landing page

**As a** Anonymous Web User
**I want to** open the application's home page
**So that** I see the application brand and the primary navigation

**Acceptance Criteria:**
- GIVEN I open `/`, `/Home`, or `/Home/Index` THEN the landing page is rendered

### US-F-008-002: Open the contact page

**As a** Anonymous Web User
**I want to** see the static contact information
**So that** I know how to reach the team

**Acceptance Criteria:**
- GIVEN I open `/Home/Contact` THEN the contact page is rendered

### US-F-008-003: See an error page when something breaks

**As a** Anonymous Web User
**I want to** see a friendly error page rather than a stack trace
**So that** unhandled exceptions are not surfaced raw

**Acceptance Criteria:**
- GIVEN an unhandled exception escapes any controller action AND `customErrors` allow the redirect THEN `HandleErrorAttribute` routes me to `/Home/Error` and the error view is rendered

### US-F-008-004: See an unauthorized page (currently dead code)

**As a** Anonymous Web User
**I want to** see a clear page when I lack permission
**So that** I know access was denied rather than silently failing

**Acceptance Criteria:**
- GIVEN I open `/Home/Unauthorized` THEN the unauthorized page is rendered
- (CURRENT BEHAVIOR) **no other code path links to or redirects to this page** — it exists but is unreachable through normal navigation

## Functional Requirements

### FR-F-008-001: Render the landing page

- **Input**: GET `/`, `/Home`, `/Home/Index`
- **Processing**: returns `View()` with no model
- **Output**: HTML
- **Error handling**: global only

### FR-F-008-002: Render the contact page

- **Input**: GET `/Home/Contact`
- **Processing**: returns `View()` with no model
- **Output**: HTML
- **Error handling**: global only

### FR-F-008-003: Render the error page

- **Input**: GET `/Home/Error` (typically reached through `HandleErrorAttribute`)
- **Processing**: returns `View()` with no model
- **Output**: HTML
- **Error handling**: this IS the error handler

### FR-F-008-004: Render the unauthorized page (unlinked)

- **Input**: GET `/Home/Unauthorized`
- **Processing**: returns `View()` with no model
- **Output**: HTML
- **Error handling**: global only

## Non-Functional Requirements

### NFR-F-008-001: No authorization

Static pages are anonymous, which is correct for these views.

### NFR-F-008-002: No caching headers

ASP.NET MVC default caching applies (none). For pure static content, an opportunity exists to apply `[OutputCache]`.

### NFR-F-008-003: `Unauthorized` is dead code

The view exists but is never linked or redirected to. Either the feature should be wired to a real flow or the action+view should be removed.

## Dependencies

| Dependency | Type | Direction | Description |
|---|---|---|---|
| `_Layout.cshtml` | Internal | — | Shared layout that all four views inherit |
| `HandleErrorAttribute` | Framework | — | Global filter registered in `App_Start/FilterConfig.cs` that targets `/Home/Error` |
| Razor views | Framework | — | All four action views in `Views/Home/` |

---

## Current Implementation (Brownfield Extension)

### Files Involved

| File Path | Role |
|---|---|
| `src/ContosoUniversity/Controllers/HomeController.cs` | All four actions |
| `src/ContosoUniversity/Views/Home/Index.cshtml` | Landing page |
| `src/ContosoUniversity/Views/Home/Contact.cshtml` | Contact page |
| `src/ContosoUniversity/Views/Home/Error.cshtml` | Generic error page |
| `src/ContosoUniversity/Views/Home/Unauthorized.cshtml` | Unauthorized page (orphan) |
| `src/ContosoUniversity/App_Start/FilterConfig.cs` | Registers `HandleErrorAttribute` globally |
| `src/ContosoUniversity/App_Start/RouteConfig.cs` | Default route maps `/` to `Home/Index` |

### Architecture Pattern

Trivial controller with `View()` returns. The error page is the global exception fallback courtesy of MVC's `HandleErrorAttribute`.

### Test Coverage

| Test Type | Files | Assertions | Coverage |
|---|---|---|---|
| Unit | — | 0 | 0% |
| Integration | — | 0 | 0% |
| E2E | — | 0 | 0% |

### Known Limitations

- `Unauthorized` view is dead code.
- No `[OutputCache]` on the static actions.
- The Error page does not display correlation id / trace id, making support harder.

### Integration Points

| External System | Protocol | Purpose | Config Location |
|---|---|---|---|
| (none) | — | — | — |
