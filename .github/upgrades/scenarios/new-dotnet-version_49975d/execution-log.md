
## [2026-03-25 21:00] TASK-001: Atomic framework and dependency upgrade

Status: Paused

**Files Modified**:
- ContosoUniversity.csproj: Converted to SDK-style, updated to net10.0, packages upgraded
- All controllers: Updated namespaces from System.Web.Mvc to Microsoft.AspNetCore.Mvc
- BaseController: Migrated to use DI instead of factory pattern
- HomeController, NotificationsController: Added constructors for DI
- SchoolContextFactory: Updated to use IConfiguration
- Program.cs: Created with ASP.NET Core startup
- appsettings.json: Created with migrated settings from Web.config

**Files Deleted**:
- App_Start folder (BundleConfig, FilterConfig, RouteConfig)
- Global.asax and Global.asax.cs

**Code Changes**:
- All controller namespaces updated to ASP.NET Core
- ActionResult → IActionResult conversions started
- HttpPostedFileBase → IFormFile in Courses  
- [Bind(Include=...)] → [Bind("...")] in all controllers
- Error.cshtml updated
- PaginatedList namespace fixed in Student Index view

**Issues Remaining**: 68 compilation errors
- ConfigurationManager.AppSettings usage in NotificationService
- JsonRequestBehavior in NotificationsController (ASP.NET Core doesn't need it)
- HttpStatusCodeResult/HttpNotFound conversion (partially done)
- SelectList usings needed in InstructorsController
- Many ActionResult → IActionResult conversions remaining

Build errors reduced from 2,403 to 68. Migration in progress.

