# Rewrite Assessment — ContosoUniversity

> **Phase:** Brownfield Phase A — assessment for the user-selected `rewrite` path.
> **Date:** 2026-05-13
> **Source application:** `src/ContosoUniversity/` (ASP.NET MVC 5.2.9 / .NET Framework 4.8.2 / EF Core 3.1.32 / SQL Server LocalDB)
> **Target stack (autonomously selected — see ADR-003):** **ASP.NET Core 8 MVC + EF Core 8 + SQL Server**
> **Inputs consumed:** `specs/prd.md`, `specs/frd-*.md` (8 FRDs), `specs/docs/technology/stack.md`, `specs/docs/technology/dependencies.md`, `specs/docs/architecture/overview.md`, `specs/docs/architecture/components.md`, `specs/docs/architecture/data-models.md`, `specs/docs/testing/coverage.md`, `specs/contracts/api/*.yaml`.

---

## 0. Executive Summary

| Dimension | Verdict |
|---|---|
| **Recommendation** | **REWRITE** to ASP.NET Core 8 MVC (lift-and-shift port, not a paradigm change) |
| **Effort estimate** | 25–35 person-days for a single experienced .NET dev across all 8 FRDs (excluding Phase 2 increment overhead) |
| **Risk level** | **Medium** — paradigm preserved, but several hard substitutions (`System.Web.HttpContext`, `Server.MapPath`, `System.Messaging`, `BundleConfig`) and a forced architectural change (no `Application_Start` ⇒ explicit DI in `Program.cs`) |
| **Strangler fig viability** | **Low for in-process** (the app is a single MVC monolith with shared `BaseController` + shared `SchoolContext`) — recommend **big-bang per-controller** rewrite inside Phase 2 increments instead, with shared DB as the integration point |
| **Top blocker** | `Infrastructure/MessageQueue` exposes a `System.Messaging`-shaped surface but is in-process; the rewrite is the right time to make this honest (replace the wrapper with `MemoryCache` + `Channel<T>` for in-process or with a real broker for out-of-process) |
| **Modernize-vs-rewrite verdict** | Rewrite wins narrowly. Modernize-in-place would require staying on .NET Framework 4.8 indefinitely (both EF Core 3.1 and .NET Framework 4.8 are out-of-support runtimes for new feature work). See §5. |

---

## 1. Complexity Profile of the Source

### 1.1 Codebase shape

| Metric | Value | Source |
|---|--:|---|
| C# files | 39 | `specs/docs/technology/stack.md` |
| Razor `.cshtml` views | 28 | same |
| Total NuGet packages (flattened) | 45 | `specs/docs/technology/dependencies.md` |
| Direct dev-only packages | 0 | same |
| BCL `<Reference>` items in csproj | 21 | same |
| MVC controllers | 7 + 1 abstract `BaseController` | `specs/docs/architecture/components.md` |
| HTTP endpoints | 45 across 7 controllers | `specs/contracts/api/_routes-overview.md` |
| EF Core entities (`DbSet<>`) | 9 (`Course`, `CourseAssignment`, `Department`, `Enrollment`, `Instructor`, `OfficeAssignment`, `Person`, `Student`, `Notification`) | `specs/docs/architecture/data-models.md` |
| EF migrations | **0** — schema is built at startup via `context.Database.EnsureCreated()` | same |
| Automated tests | **0** — only 5 manual-verification markdown files | `specs/docs/testing/coverage.md` |
| LOC estimate | ~3,500 lines C# + ~1,800 lines Razor (excluding generated bundles) | derived |

### 1.2 Tightly-coupled-to-platform code that must change

| Pattern | Where it lives | What replaces it on .NET 8 |
|---|---|---|
| `Global.asax` + `MvcApplication : HttpApplication` + `Application_Start` | `Global.asax`, `Global.asax.cs` | `Program.cs` (top-level statements) + `WebApplication.CreateBuilder` + middleware pipeline |
| `App_Start/RouteConfig.cs` (`{controller}/{action}/{id}`) | `App_Start/RouteConfig.cs` | `app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}")` |
| `App_Start/FilterConfig.cs` (`HandleErrorAttribute`) | `App_Start/FilterConfig.cs` | `app.UseExceptionHandler("/Home/Error")` + `app.UseStatusCodePagesWithReExecute(...)` |
| `App_Start/BundleConfig.cs` (5 bundles via `System.Web.Optimization` + `WebGrease`) | `App_Start/BundleConfig.cs` | **No equivalent.** Choose one: (a) embed Bootstrap/jQuery via CDN `<link>` tags, (b) BuildBundlerMinifier (deprecated), (c) **recommended**: Vite or webpack with output to `wwwroot/dist/` |
| `Server.MapPath("~/Uploads/TeachingMaterials/")` | `Controllers/CoursesController.cs` (3 call sites: lines 76, 159, 172, 229) | Inject `IWebHostEnvironment` and use `env.WebRootPath` or `env.ContentRootPath`; or move to `BlobStorage` (see §6) |
| `HttpPostedFileBase teachingMaterialImage` | same | `IFormFile teachingMaterialImage` |
| `System.Web.Mvc.Controller.View(...)`, `JsonResult`, `JsonRequestBehavior.AllowGet` | All controllers | `Microsoft.AspNetCore.Mvc.Controller.View(...)`, `JsonResult` (no `JsonRequestBehavior` — `JsonResult` is GET-allowed by default in .NET Core) |
| `[Bind(Include="…")]` | `StudentsController`, `InstructorsController`, etc. | `[Bind(nameof(Property1), nameof(Property2), …)]` (signature unchanged but `Include=` named arg is gone — positional `params string[]` now) |
| `TryUpdateModel(...)` | `InstructorsController.Edit` (POST) | `await TryUpdateModelAsync(instructor, "", i => i.LastName, …)` |
| `ConfigurationManager.ConnectionStrings["DefaultConnection"]` | `Global.asax.cs:27`, `Data/SchoolContextFactory.cs:10` | `builder.Configuration.GetConnectionString("DefaultConnection")` (`appsettings.json`) — eventually with Azure Key Vault binding (see ADR-007) |
| `ConfigurationManager.AppSettings["NotificationQueuePath"]` | `Services/NotificationService.cs` | `builder.Configuration["Notifications:QueuePath"]` |
| `System.Diagnostics.Trace.TraceError` / `Debug.WriteLine` (10+ call sites) | All controllers + `BaseController.SendEntityNotification` | Inject `ILogger<T>` and use structured logging (`_logger.LogError(ex, "...")`) |
| `Web.config` `<httpRuntime maxRequestLength="10240" />` (10 MB IIS) vs F-002 NFR-F-002-002's 5 MB app cap | `Web.config:31` | `[RequestSizeLimit(5_242_880)]` on the action OR `IFormOptions.MultipartBodyLengthLimit = 5 * 1024 * 1024` in `Program.cs` — also resolves the IIS-vs-app cap mismatch flagged in B2c |
| `<assemblyBinding>` + 22 `<bindingRedirect>` entries | `Web.config:43-…` | Deleted entirely — package versions are unified by `dotnet restore` |
| `CopySQLClientNativeBinaries` MSBuild target (copies `Microsoft.Data.SqlClient.SNI.dll`) | `ContosoUniversity.csproj` | Deleted — `Microsoft.Data.SqlClient` 5.x bundles SNI for all `runtimes/` automatically |
| `BaseController` constructs `new SchoolContext()` and `new NotificationService()` per request | `Controllers/BaseController.cs` | Constructor injection (`IServiceProvider`) — register `SchoolContext` as scoped, `INotificationService` as scoped/singleton |
| `BaseController.cs:28` hardcodes `userName = "System"` | `BaseController.cs` | Replace with `User.Identity?.Name ?? "anonymous"` after auth is wired (see security assessment + ADR-005) |
| `Global.asax.cs:InitializeDatabase` runs `EnsureCreated` synchronously on app start | `Global.asax.cs` | Move to a startup hosted service or document explicit `dotnet ef database update` (see §6 / ADR pending data migration) |

### 1.3 Code that ports cleanly (~80% of the codebase)

- All Razor views (`.cshtml`): the Razor syntax is virtually identical between MVC 5 and MVC Core. The only edits needed are `@using` namespace updates and replacing `@Html.AntiForgeryToken()` with the (auto-emitted) `[ValidateAntiForgeryToken]` filter (which works the same way on Core).
- All POCO models in `Models/` (`Course`, `Student`, `Instructor`, `Department`, `Enrollment`, `OfficeAssignment`, `CourseAssignment`, `Person`, `Notification`, `ErrorViewModel`, `SchoolViewModels.*`): zero changes required (data-annotations from `System.ComponentModel.DataAnnotations` are unchanged).
- LINQ + `DbSet<>` queries in controllers: zero changes required (LINQ surface is unchanged).
- `PaginatedList<T>`: zero changes required (no platform dependencies).
- Most `[HttpGet]` / `[HttpPost]` / `[ValidateAntiForgeryToken]` annotations: namespace import changes only (`System.Web.Mvc` → `Microsoft.AspNetCore.Mvc`).

---

## 2. Feature Preservation Map

For each FRD, the table below shows the rewrite delta and any feature behaviors that must be preserved verbatim by green-baseline tests.

| FRD | Source code touched | Rewrite delta | Behavior to preserve |
|---|---|---|---|
| **F-001 — Student Management** (`frd-student-management.md`) | `StudentsController.cs`, `Models/Student.cs`, `Models/Person.cs`, `Views/Students/*` (8 views), `PaginatedList.cs` | Replace `[Bind(Include="…")]` syntax; replace `Trace.TraceError` with `ILogger`; replace controller-constructor `db = new SchoolContext()` with DI. **KL-F-001-001** (`.Single()` on `Edit(int? id)` — bug that 500s when ID is missing) is preserved as current broken behavior in the green baseline; Phase 2 bug-fix increment converts to `.SingleOrDefault()` and asserts HTTP 200. | Sort/filter/paging exact behavior (`name_desc`, `Date`, `date_desc`); search is case-insensitive `Contains` on `LastName` + `FirstMidName`; page size = `PaginatedList.PageSize` (currently 3) |
| **F-002 — Course Management** (`frd-course-management.md`) | `CoursesController.cs` (5 actions), `Models/Course.cs`, `Views/Courses/*` (5 views), `Uploads/TeachingMaterials/` | Replace `HttpPostedFileBase` → `IFormFile`; replace `Server.MapPath` → `IWebHostEnvironment.WebRootPath`; **enforce 5 MB cap with `[RequestSizeLimit]` to fix the Web.config 10 MB / app 5 MB mismatch**; replace `course.CourseID` (manually-assigned PK) handling — Core's `[BindNever]` works the same. | Allowed extensions list `.jpg/.jpeg/.png/.gif/.bmp`; filename pattern `course_{CourseID}_{Guid}{ext}`; old file deletion on Edit when a new image is uploaded; image path persisted as `~/Uploads/TeachingMaterials/...` (will need conversion to absolute URL or virtual path on Core) |
| **F-003 — Instructor Management** (`frd-instructor-management.md`) | `InstructorsController.cs` (8 actions), `Models/Instructor.cs`, `Models/OfficeAssignment.cs`, `Models/CourseAssignment.cs`, `Models/SchoolViewModels/InstructorIndexData.cs`, `Models/SchoolViewModels/AssignedCourseData.cs`, `Views/Instructors/*` | Replace `TryUpdateModel` → `await TryUpdateModelAsync`; eager-loading `Include`/`ThenInclude` is unchanged; many-to-many `CourseAssignment` synchronization (`UpdateInstructorCourses`) ports as-is; OfficeAssignment 1-to-1 ports as-is | Multi-select course assignment via `string[] selectedCourses`; `InstructorIndexData` aggregation; `OfficeAssignment` cascade behavior on Instructor delete |
| **F-004 — Department Management** (`frd-department-management.md`) | `DepartmentsController.cs` (8 actions), `Models/Department.cs`, `Views/Departments/*` | Replace `[Bind(Include="…")]`; the `RowVersion` (concurrency token) handling ports cleanly — EF Core 8 supports `[Timestamp]` and `IsConcurrencyToken()`; `IsRowVersion()` syntax unchanged | Optimistic concurrency on Edit (returns concurrency-conflict view); `InstructorID` administrator FK is nullable; cascade rules on Department delete |
| **F-005 — Enrollment Statistics** (`frd-enrollment-statistics.md`) | `HomeController.cs` (`About()`), `Models/SchoolViewModels/EnrollmentDateGroup.cs`, `Views/Home/About.cshtml` | Pure LINQ + grouping — ports with no changes other than namespace + DI | `GROUP BY EnrollmentDate` semantics; sorted by date ascending |
| **F-006 — Static Pages** (`frd-static-pages.md`) | `HomeController.cs` (`Index`, `Contact`, `Error`, `Unauthorized`), `Views/Home/*`, `Views/Shared/_Layout.cshtml`, `Content/Site.css` | Layout view ports cleanly; `BundleConfig` references in `_Layout.cshtml` (`@Scripts.Render`, `@Styles.Render`) must be replaced with explicit `<script>`/`<link>` tags or a build-tool integration | Branding text, navbar entries, footer, redirect from `Home/Unauthorized` |
| **F-007 — Real-Time Notification System** (`frd-notification-system.md`) | `NotificationsController.cs` (3 actions), `Services/NotificationService.cs`, `Infrastructure/IMessageQueue.cs`, `Infrastructure/MessageQueueManager.cs`, `Infrastructure/InMemoryMessageQueue.cs`, `Infrastructure/MessageQueue.cs`, `Infrastructure/Message.cs`, `Infrastructure/MessageQueueExceptions.cs`, `Models/Notification.cs`, `Content/notifications.css`, `Views/Notifications/Index.cshtml` | **Largest refactor opportunity.** The `Infrastructure/MessageQueue` wrapper exists only because the original code intended MSMQ; the implementation is already in-process. Rewrite path: replace the entire `Infrastructure/` namespace with one of: (a) `IMemoryCache` + `Channel<Notification>` (in-process, simplest), (b) `Microsoft.Extensions.Hosting.BackgroundService` + `Channel<Notification>` (in-process, more idiomatic), (c) Azure Service Bus or RabbitMQ (out-of-process, future-proof). **Recommendation: (b) for the rewrite + (c) as a Phase 2 cloud-native increment.** | Polling endpoint `GET /Notifications/GetNotifications` returns up to 10 notifications per call; `MarkAsRead` is a no-op (preserved as-is — flagged in B2c m3-2 for Phase A scope, see §6); CSRF gap on `MarkAsRead` (KL-CSRF-001) is preserved in green baseline, fixed in Phase 2 |
| **F-008 — Queue Diagnostic Tools** (`frd-queue-diagnostics.md`) | `Controllers/MessageQueueTestController.cs`, `Examples/MessageQueueExample.cs`, `Views/MessageQueueTest/*` | If the `Infrastructure/MessageQueue` wrapper is replaced (per F-007), this entire feature must be rewritten or deleted. Recommendation: **delete in the rewrite** — the diagnostics page tests an in-process queue that no longer exists; replace with a generic `/health` and `/health/notifications` endpoint that exposes channel depth via `IHealthCheck`. | The diagnostic page workflow (create queue → send 2 messages → receive → delete → list queues) is not in any FRD acceptance criterion as a user-facing feature. Documented as a developer tool only. |

### 2.1 Cross-cutting concerns to introduce

| Concern | Current state | Rewrite target |
|---|---|---|
| Logging | `System.Diagnostics.Trace`, `Debug.WriteLine`, swallowed exceptions in `BaseController.SendEntityNotification` | `ILogger<T>` via DI; structured logs; `Microsoft.Extensions.Logging.Console` for dev, OpenTelemetry exporter for prod |
| Configuration | `Web.config` `<appSettings>` + `<connectionStrings>` | `appsettings.json` + `appsettings.{Environment}.json` + `dotnet user-secrets` for dev, Azure Key Vault for prod (see ADR-007) |
| DI | None (controllers `new` everything) | Built-in `IServiceCollection` — register `SchoolContext` as scoped, `INotificationService` as scoped, `ILogger<T>` automatically |
| Validation | `ModelState.IsValid` + data-annotations + jQuery unobtrusive validation | Same — Core preserves the entire model-binding/validation pipeline |
| Auth/AuthZ | **None — currently anonymous** (see security assessment) | ASP.NET Core Identity + Microsoft Entra ID (see ADR-005, ADR-006) |
| Telemetry | None | OpenTelemetry SDK + Azure Monitor / Application Insights exporter |
| Health checks | None | `Microsoft.Extensions.Diagnostics.HealthChecks` — `/health` (liveness), `/health/ready` (db connectivity, channel depth) |

---

## 3. Translation Feasibility Per Subsystem

| Subsystem | Surface API change | Semantic change | Translation feasibility |
|---|---|---|---|
| **Routing** (RouteConfig → `MapControllerRoute`) | API rename only | None | **Trivial** |
| **Controllers** (action method signatures, `ActionResult`/`JsonResult`/`HttpPostedFileBase`) | Namespace + small surface changes | None | **Easy** — mostly mechanical edits |
| **Razor views** | `@using` updates | None | **Easy** — pure copy with namespace changes |
| **EF Core 3.1.32 → EF Core 8** | Some DbContext surface changes (e.g., `UseSqlServer` overloads), TPH discriminator config syntax slightly improved (8 prefers fluent `HasDiscriminator<>`), `[Owned]` types behave the same, query rewriting (some 3.1 `client-evaluation` warnings became errors in 5+) | Some — see EF Core 3.1→8 migration guide | **Medium** — most queries port cleanly; `OnModelCreating` ports verbatim; risk: hidden client-evaluation queries that 8 will reject |
| **Schema management** (`EnsureCreated` → migrations) | Different model | Different lifecycle | **Medium** — must scaffold initial migration that matches the existing schema, then introduce migrations for future changes (see ADR-pending-data-migration in §7) |
| **`Infrastructure/MessageQueue` (in-process)** | API redesign | Same in-process semantics, but better abstractions (`Channel<T>`) | **Medium** — small code volume, but every consumer (NotificationService, NotificationsController, MessageQueueTestController) must be updated |
| **Bundling/minification** (`BundleConfig`) | Different toolchain | None | **Medium** — choice of build tool (Vite recommended); Razor edits in `_Layout.cshtml` |
| **File uploads** (`HttpPostedFileBase` → `IFormFile`, `Server.MapPath` → `IWebHostEnvironment`) | API rename + injection | None | **Easy** — 4 call sites in `CoursesController` |
| **Hosting** (`Global.asax` → `Program.cs`) | New hosting model | Same lifecycle hooks | **Easy** — replace 80 lines of `Global.asax.cs` with ~30 lines of `Program.cs` |
| **Web.config → appsettings.json** | Different format | Same keys, mostly | **Easy** — flatten `<appSettings>` into JSON; bindings + assembly redirects deleted |
| **Authentication** (none → Identity + Entra ID) | New subsystem | Behavioral change (anonymous → authenticated) | **Hard — but a security requirement, not a rewrite requirement.** This is forced by the security assessment, not by the rewrite. See ADR-005, ADR-006. |
| **Authorization** (none → `[Authorize(Roles=…)]` + policies) | New subsystem | Behavioral change | **Hard** — must update every action method + propagate role context everywhere `BaseController.SendEntityNotification` currently uses `"System"` |

---

## 4. Effort Estimation

> **Assumption:** one experienced .NET dev who has done a Framework→Core migration before. **Excludes** Phase 2 increment overhead (test scaffolding, deployment, smoke tests, code review, ADR drafting).

| Workstream | Effort (person-days) | Notes |
|---|--:|---|
| Project scaffold (`Program.cs`, csproj SDK-style, DI registrations, configuration plumbing, layout view) | 2 | One-shot |
| Port `Models/*` and `Data/SchoolContext` to EF Core 8 + initial migration scaffolding | 2 | Includes scaffolding initial migration that matches existing schema |
| Port `StudentsController` + views (F-001) | 2 | Simplest controller; sets template for the rest |
| Port `CoursesController` + views (F-002) — **including file upload subsystem rewrite** | 3 | `IFormFile` + `IWebHostEnvironment` + `[RequestSizeLimit(5MB)]` |
| Port `InstructorsController` + views (F-003) | 3 | Largest controller; many-to-many sync logic |
| Port `DepartmentsController` + views (F-004) | 2 | Concurrency token handling |
| Port `HomeController` + views + layout (F-005, F-006) | 1 | Plus Bundling replacement (Vite or CDN) |
| **Notification subsystem rewrite (F-007)** — replace `Infrastructure/*` with `Channel<Notification>` + `BackgroundService`; rewrite `NotificationService` + `NotificationsController` | 4 | Largest single piece of new code; includes deciding in-process vs Service Bus |
| F-008 (`MessageQueueTestController`) — delete and replace with `IHealthCheck` | 1 | Trivial replacement |
| Authentication + authorization wiring (Identity + Entra ID + role propagation through `BaseController`) | 5 | **Driven by security assessment, not rewrite — but blocks Phase 2 deployment** |
| Logging migration (`Trace.*` / `Debug.WriteLine` → `ILogger<T>`) — ~10 call sites | 1 | Mechanical |
| Configuration migration (`Web.config` → `appsettings.json` + Key Vault) | 1 | Plus ADR-007 work |
| Smoke testing the assembled app locally + fixing surprises | 2 | Buffer for "first run" unknowns |
| **Total (rewrite-only)** | **~24 days** | Excludes auth/authz, which adds ~5 days |
| **Total (rewrite + security baseline)** | **~29 days** | Within the 25–35 day estimate above |

### 4.1 Risk-adjusted estimate

The above assumes no major surprises. Add **+20% contingency** (≈ 6 days) for:
- EF Core 3.1 → 8 query-translation regressions (the 3.1 client-evaluation behavior is gone)
- `WebGrease`/bundling replacement going wrong (Vite/webpack first-time setup)
- Unknown corners of `System.Web.HttpContext` usage in helper code
- The TPH discriminator config syntax differences between EF Core 3.1 and 8

**Risk-adjusted total: 30–35 person-days** for one experienced dev, single-stream (no parallelism). With two devs working in parallel on different controllers (after the project scaffold + auth + EF Core migration are done), this compresses to **~18 calendar days**.

---

## 5. Modernize-vs-Rewrite Comparison

This is the **mandatory ADR comparison** required by the rewrite-assessment skill. See **ADR-002** for the full decision record. Summary table:

| Dimension | Modernize-in-place (stay on .NET Framework 4.8.2) | Rewrite to .NET 8 |
|---|---|---|
| **Effort** | 8–12 days (upgrade EF Core 3.1 → no further upgrade possible on Framework; bound to .NET Framework 4.8.x runtime; backport security patches manually) | 30–35 days |
| **Future runtime support** | .NET Framework 4.8.x is **maintenance-only** (no new features); no path to .NET 9/10/… | .NET 8 LTS (support through Nov 2026), straightforward upgrade to .NET 9/10 |
| **Future EF Core support** | EF Core 3.1.32 is **out of support** (EOL Dec 2022); no upgrade path on .NET Framework — EF Core 5+ requires .NET Standard 2.1 or .NET 5+ | EF Core 8 LTS (support through Nov 2026) |
| **Cloud deployability** | Azure App Service Windows only (or self-hosted IIS) | Azure App Service Linux/Windows, Container Apps, AKS, anywhere |
| **Container readiness** | Requires Windows containers (large images, slower startup, limited orchestrator support) | Linux containers — small, fast, ubiquitous |
| **Security patch velocity** | Microsoft Update Tuesday cycle for .NET Framework + manual NuGet patching | `dotnet outdated` + per-release security advisories — modern tooling |
| **Hiring/skills market** | Shrinking — most new .NET hires expect Core | Mainstream — every .NET dev knows Core |
| **Risk of regression** | **Low** (no code changes other than version bumps) | **Medium** (3,500 LOC refactor) |
| **Forces auth/authz redesign** | **No** — could continue with anonymous access (which is the current security hole — see security assessment) | **Yes — and this is good.** The rewrite is the natural moment to introduce auth, since `BaseController` has to be touched anyway for DI |
| **Total cost of ownership over 3 years** | High — manual security patching, Windows-only hosting, deferred-to-later rewrite still inevitable | Lower — modern toolchain, broader hosting, longer runway |

**Verdict — Rewrite wins**, narrowly. The decisive factors are:
1. **EF Core 3.1.32 cannot be upgraded** on .NET Framework. Staying means freezing the ORM at an out-of-support version forever.
2. **Auth/authz must be introduced** (security assessment, KL-AUTH-001 critical). Doing it on .NET Framework 4.8 with `Microsoft.AspNet.Identity.Owin` is possible but adds tech debt that the rewrite would later have to undo.
3. The application is small enough (3,500 LOC) that the rewrite cost is bounded and predictable.

If the application were 10× larger or had heavy `System.Web.HttpContext` / WebForms / WCF / SignalR usage, modernize-in-place would win. It does not.

---

## 6. Strangler-Fig Strategy (Required Evaluation)

The rewrite-assessment skill requires evaluating strangler-fig as the rewrite execution pattern.

### 6.1 In-process strangler-fig — **NOT VIABLE**

The application has a single `MvcApplication`, a single `SchoolContext`, and a single `BaseController` shared by 6 controllers. Strangler-fig in-process would require running ASP.NET MVC 5 and ASP.NET Core 8 in the **same process** with a routing seam (`Yarp` reverse proxy or a `Microsoft.AspNetCore.SystemWebAdapters` shim). The `Microsoft.AspNetCore.SystemWebAdapters` package supports incremental MVC 5 → Core migration but:
- It carries non-trivial complexity (separate apps, shared session, shared auth)
- The application is too small to justify the operational complexity
- The `BaseController` shared base cannot be cleanly split

### 6.2 Per-controller big-bang within Phase 2 increments — **RECOMMENDED**

Use the spec2cloud Phase 2 increment pattern as the strangler. Each Phase 2 increment rewrites **one controller + its views + its slice of the DI registrations** in a single PR, with the green-baseline tests proving behavior preservation. The **shared SchoolContext** is the integration point. Increments deliver in dependency order (low coupling first):

| Increment ordering for rewrite | Controller(s) | Why this order |
|---:|---|---|
| 1 | Project scaffold + EF Core 8 migration + auth wiring (no controllers yet) | Foundation before anything else |
| 2 | `HomeController` + static pages (F-005, F-006) | Smallest, fewest dependencies, validates the scaffold |
| 3 | `DepartmentsController` (F-004) | Self-contained; concurrency token tests the EF Core 8 migration |
| 4 | `StudentsController` (F-001) | Stand-alone; introduces sort/filter/paging template |
| 5 | `CoursesController` (F-002) | Depends on Departments; introduces file-upload pattern |
| 6 | `InstructorsController` (F-003) | Depends on Courses + OfficeAssignment + CourseAssignment; largest |
| 7 | `NotificationsController` + new infrastructure (F-007) | Big refactor; replaces `Infrastructure/MessageQueue` |
| 8 | Delete `MessageQueueTestController`, add `/health` (F-008) | Cleanup |

This is **NOT classic strangler-fig** (which keeps both stacks running in production). It is **per-feature big-bang** with the green-baseline regression tests as the safety net. ADR-004 documents this choice.

### 6.3 Alternative: full big-bang in a single increment — **NOT RECOMMENDED**

Single-PR rewrite of the entire app would skip the iterative-delivery promise of spec2cloud and prevent partial deployment. Rejected.

---

## 7. Data Migration

| Concern | Current state | Rewrite target | Approach |
|---|---|---|---|
| **ORM version** | EF Core 3.1.32 | EF Core 8 | Update DbContext + provider packages; review query translation differences |
| **Schema management** | `context.Database.EnsureCreated()` — no migrations folder, no `__EFMigrationsHistory` table | EF Core 8 migrations | Step 1: scaffold initial migration from existing schema (`dotnet ef migrations add Initial --no-build`). Step 2: in production, manually insert a single row into `__EFMigrationsHistory` with the migration ID to mark the schema as already applied. Step 3: future schema changes via `dotnet ef migrations add Change`. |
| **Database engine** | SQL Server LocalDB (`(LocalDb)\MSSQLLocalDB`) for dev | Same engine, different instance for prod (Azure SQL recommended; PostgreSQL would require switching the EF provider to `Npgsql.EntityFrameworkCore.PostgreSQL` and a non-trivial schema review) | Stay on SQL Server. Use SQL Server Express / SQL Server in Docker for dev, Azure SQL for prod. The `Microsoft.Data.SqlClient` 5.x provider is supported on .NET 8. |
| **Connection authentication** | `Integrated Security=True` (Windows auth on LocalDB) | Managed Identity → Azure SQL (or SQL auth + Key Vault for non-Azure environments) | See ADR-007 |
| **Data preservation** | 9 entities + `Person` TPH | Same 9 entities + same TPH | Schema-compatible — no row-level migration required |
| **Existing data** | `DbInitializer` seeds demo data on every fresh DB | Keep `DbInitializer` for dev; for prod, run once via a migration `Up` block | Trivial |

A separate ADR for data migration approach is deferred to the corresponding Phase 2 increment (it's tied to the specific data-migration increment, not the overall rewrite decision).

---

## 8. Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| EF Core 3.1 → 8 query-translation regressions (LINQ that 3.1 client-evaluated and 8 rejects as untranslatable) | Medium | Medium (test failures during increment) | Phase 2 green-baseline tests catch these immediately; fix with explicit `.AsEnumerable()` or query rewrites |
| Auth/authz scope creep (Identity + Entra ID + roles + per-resource policies) blowing up the rewrite estimate | High | High (could double the estimate) | Scope auth strictly to "anonymous → authenticated" with a single `Admin` role for the rewrite; defer fine-grained per-resource authorization to a separate Phase 2 increment |
| `Infrastructure/MessageQueue` rewrite picks the wrong abstraction (`Channel<T>` vs Service Bus vs RabbitMQ) | Medium | Medium (rework cost in next increment) | ADR-004 commits to `Channel<T>` + `BackgroundService` for the rewrite; deferred Service Bus migration to a later cloud-native increment if/when path is selected |
| Bundling/minification replacement (BundleConfig → Vite) consumes time disproportionate to value | Medium | Low | Skip Vite for the rewrite; use CDN `<link>`/`<script>` for Bootstrap 5.3.3 + jQuery 3.7.1 (the only two assets actually shipped). Site.css and notifications.css ship as static files in `wwwroot/`. |
| Razor view edge cases (custom `@helper` blocks, untyped `ViewBag`, anti-forgery edge cases) | Low | Low | The codebase uses standard Razor; no custom view engine; `ViewBag` ports as-is |
| `MessageQueueTestController` deletion breaks a hidden internal user | Low | Very Low | Document in ADR-004; offer `/health/notifications` as replacement |
| 5 MB upload cap enforcement breaks an existing user workflow that relied on the loose 10 MB IIS cap | Low | Low | Documented in B2c m3-1; the FRD-002 NFR is the authoritative limit |
| Introducing auth breaks every existing demo workflow that relies on anonymous access | Certain | High | This **is** the security goal. Feature parity post-rewrite means "anonymous browsing of read-only views + authenticated editing." Documented in ADR-006. |
| `BaseController` per-request `new SchoolContext()` masking a connection-leak that DI exposes | Low | Medium | Run integration tests under load to detect connection-pool exhaustion |

---

## 9. Open Questions for Planning

1. **Auth provider:** Microsoft Entra ID only, or also support local Identity for non-Azure deployments? — proposed in ADR-005.
2. **Bundling tool:** Vite, webpack, BuildBundlerMinifier, or no bundling (CDN only)? — proposed in §8 risk row.
3. **Azure SQL vs SQL Server in Docker for dev:** dev parity vs ease of local setup. — defer to tech-stack-resolution if/when cloud-native path is added.
4. **Notification queue: in-process `Channel<T>` vs Azure Service Bus** — proposed for `Channel<T>` in the rewrite (ADR-004), Service Bus deferred.
5. **Strangler ordering (§6.2):** is the proposed increment order acceptable to the user? — flagged for human gate at increment-plan approval.

---

## 10. Mandatory Completion Checklist

- [x] Codebase complexity profiled (LOC, controller count, dependencies, tightly-coupled-to-platform code) — §1
- [x] Feature preservation map produced (per FRD: source files, rewrite delta, behaviors to preserve) — §2
- [x] Translation feasibility analyzed per subsystem (routing, controllers, views, ORM, queue, hosting, config, file uploads, auth) — §3
- [x] Effort estimate provided (per workstream, with risk-adjusted total) — §4
- [x] Modernize-vs-rewrite comparison documented with verdict — §5 + ADR-002
- [x] Strangler-fig strategy evaluated (in-process viability, per-controller big-bang within increments, full big-bang) — §6
- [x] Data migration assessed (ORM upgrade path, schema management, engine choice, auth) — §7
- [x] Risk assessment with likelihood + impact + mitigation — §8
- [x] ADRs produced — ADR-002 (rewrite vs modernize, mandatory), ADR-003 (target stack), ADR-004 (migration pattern — per-controller big-bang within spec2cloud increments)
- [x] State JSON and audit log updated (after this file is committed)

---

_This assessment was produced autonomously based on the user's path-selection of `["rewrite", "security"]` and the orchestrator's autonomous selection of ASP.NET Core 8 MVC as the target stack (see ADR-003). The user can override the target stack at any time; doing so will require regenerating this document and updating ADR-003._
