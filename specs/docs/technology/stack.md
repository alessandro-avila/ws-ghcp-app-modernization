# Technology Stack — ContosoUniversity

_Extracted on 2026-05-13. This is a factual inventory of the project as it exists at `src/ContosoUniversity/`._

## Languages

| Language | File Count | Percentage | Config Indicator |
|----------|-----------:|-----------:|------------------|
| C# | 39 | 47.6% | `ContosoUniversity.csproj`, `ContosoUniversity.sln` |
| Razor (CSHTML) | 28 | 34.1% | `Views/Web.config`, `_ViewStart.cshtml` |
| Markdown | 5 | 6.1% | — |
| XML config | 3 | 3.7% | `Web.config`, `Web.Debug.config`, `Web.Release.config` |
| CSS | 2 | 2.4% | `Content/Site.css`, `Content/notifications.css` |
| JSON | 2 | 2.4% | — |
| Other (`.asax`, `.csproj`, `.sln`, `.gitignore`, `.gitkeep`, `.user`) | 6 | 7.3% | — |

_Counts exclude `packages/`, `bin/`, `obj/`, `.vs/`, and `Scripts/` (third-party JavaScript bundled as content)._

## Frameworks

| Framework | Version | Category | Detected From |
|-----------|---------|----------|---------------|
| .NET Framework | 4.8 (assemblies) / `net482` (NuGet target) | Runtime | `ContosoUniversity.csproj` `<TargetFrameworkVersion>v4.8</TargetFrameworkVersion>`, `packages.config` `targetFramework="net482"` |
| ASP.NET MVC | 5.2.9 | Web framework | `packages.config` `Microsoft.AspNet.Mvc 5.2.9`; `<ProjectTypeGuids>` includes `{349c5851-65df-11da-9384-00065b846f21}` (MVC project flavor) |
| ASP.NET Razor | 3.2.9 | View engine | `Microsoft.AspNet.Razor 3.2.9`; `Views/*.cshtml` |
| ASP.NET Web Pages | 3.2.9 | Razor support | `Microsoft.AspNet.WebPages 3.2.9` |
| ASP.NET Web Optimization | 1.1.3 | Bundling/minification | `Microsoft.AspNet.Web.Optimization 1.1.3`, `App_Start/BundleConfig.cs` |
| Entity Framework Core | 3.1.32 | ORM | `Microsoft.EntityFrameworkCore 3.1.32`, `Microsoft.EntityFrameworkCore.SqlServer 3.1.32`, `Data/SchoolContext.cs` |
| Microsoft.Data.SqlClient | 2.1.4 | SQL Server data provider | `packages.config`; native SNI DLLs copied via `CopySQLClientNativeBinaries` MSBuild target |
| Microsoft.Extensions.* (DI, Configuration, Caching, Logging, Options, Primitives) | 3.1.32 | DI/config/logging primitives (used as EF Core dependencies) | `packages.config` |
| Microsoft.Identity.Client (MSAL.NET) | 4.21.1 | Identity / OAuth client | `packages.config` |
| System.Messaging | (BCL — .NET Framework) | Message queue (MSMQ) client | `<Reference Include="System.Messaging">` in csproj; `Infrastructure/MessageQueue.cs`, `Services/NotificationService.cs`, `NOTIFICATION_SYSTEM_README.md` |
| Bootstrap | 5.3.3 | CSS framework | `packages.config` `bootstrap 5.3.3` |
| jQuery | 3.7.1 | Client-side JS | `packages.config` `jQuery 3.7.1` |
| jQuery.Validation | 1.21.0 | Client-side validation | `packages.config` |
| Microsoft.jQuery.Unobtrusive.Validation | 4.0.0 | MVC unobtrusive validation | `packages.config` |
| Modernizr | 2.6.2 | HTML5 feature detection | `packages.config` |
| Newtonsoft.Json | 13.0.3 | JSON serialization | `packages.config` |
| Antlr (3.4.1.9004) | 3.4.1.9004 | Transitive (WebGrease/bundling) | `packages.config` |
| WebGrease | 1.5.2 | Bundling/optimization (legacy) | `packages.config` |
| Microsoft.CodeDom.Providers.DotNetCompilerPlatform | 2.0.1 | Roslyn-based ASP.NET runtime compiler | `packages.config` |
| Microsoft.Bcl.HashCode | 1.1.1 | BCL polyfill | `packages.config` |
| Microsoft.Bcl.AsyncInterfaces | 1.1.1 | BCL polyfill | `packages.config` |
| NETStandard.Library | 2.0.3 | NetStandard meta-package | `packages.config` |

> **Note:** EF Core 3.1.32 (a `netstandard2.0` library) is being consumed from a `.NET Framework 4.8.2` web app — an unusual but legal combination, supported through the `netstandard` reference and binding redirects in `Web.config`.

## Build Tools

| Tool | Version | Purpose | Config Indicator |
|------|---------|---------|------------------|
| MSBuild | ToolsVersion `15.0` (csproj) / VS 17.5.2 (sln) | Build engine | `ContosoUniversity.csproj`, `ContosoUniversity.sln` |
| NuGet (packages.config format) | — | Package management (legacy, non-PackageReference) | `packages.config`, `packages/` directory |
| Roslyn (`Microsoft.CodeDom.Providers.DotNetCompilerPlatform`) | 2.0.1 | C# compiler used by ASP.NET runtime | `packages.config` |
| `WebApplication.targets` | VS WebApplications targets | Web project build | `Microsoft.WebApplication.targets` import in csproj |
| Custom MSBuild target `CopySQLClientNativeBinaries` | — | Copies `Microsoft.Data.SqlClient.SNI.dll` (x64/x86) into `bin/` after build | `<Target Name="CopySQLClientNativeBinaries">` in csproj |
| Custom MSBuild target `MvcBuildViews` (disabled — `MvcBuildViews=false`) | — | Optional precompilation of Razor views | csproj `<Target Name="MvcBuildViews">` |

## Runtime Dependencies (from `packages.config`)

Server-side / .NET libraries (44 total NuGet packages):

- `Antlr 3.4.1.9004`
- `bootstrap 5.3.3`
- `jQuery 3.7.1`, `jQuery.Validation 1.21.0`, `Microsoft.jQuery.Unobtrusive.Validation 4.0.0`
- `Microsoft.AspNet.Mvc 5.2.9`, `Microsoft.AspNet.Razor 3.2.9`, `Microsoft.AspNet.Web.Optimization 1.1.3`, `Microsoft.AspNet.WebPages 3.2.9`
- `Microsoft.Bcl.AsyncInterfaces 1.1.1`, `Microsoft.Bcl.HashCode 1.1.1`
- `Microsoft.CodeDom.Providers.DotNetCompilerPlatform 2.0.1`
- `Microsoft.Data.SqlClient 2.1.4`, `Microsoft.Data.SqlClient.SNI.runtime 2.1.1`
- `Microsoft.Identity.Client 4.21.1`
- `Microsoft.EntityFrameworkCore 3.1.32`, `Microsoft.EntityFrameworkCore.Abstractions 3.1.32`, `Microsoft.EntityFrameworkCore.Analyzers 3.1.32`, `Microsoft.EntityFrameworkCore.Relational 3.1.32`, `Microsoft.EntityFrameworkCore.SqlServer 3.1.32`, `Microsoft.EntityFrameworkCore.Tools 3.1.32`
- `Microsoft.Extensions.Caching.Abstractions 3.1.32`, `Microsoft.Extensions.Caching.Memory 3.1.32`
- `Microsoft.Extensions.Configuration 3.1.32`, `Microsoft.Extensions.Configuration.Abstractions 3.1.32`, `Microsoft.Extensions.Configuration.Binder 3.1.32`
- `Microsoft.Extensions.DependencyInjection 3.1.32`, `Microsoft.Extensions.DependencyInjection.Abstractions 3.1.32`
- `Microsoft.Extensions.Logging 3.1.32`, `Microsoft.Extensions.Logging.Abstractions 3.1.32`
- `Microsoft.Extensions.Options 3.1.32`, `Microsoft.Extensions.Primitives 3.1.32`
- `Microsoft.Web.Infrastructure 2.0.1`
- `Modernizr 2.6.2`
- `NETStandard.Library 2.0.3`
- `Newtonsoft.Json 13.0.3`
- `System.Buffers 4.5.1`, `System.Collections.Immutable 1.7.1`, `System.ComponentModel.Annotations 4.7.0`, `System.Diagnostics.DiagnosticSource 4.7.1`, `System.Memory 4.5.4`, `System.Numerics.Vectors 4.5.0`, `System.Runtime.CompilerServices.Unsafe 4.5.3`, `System.Threading.Tasks.Extensions 4.5.4`
- `WebGrease 1.5.2`

Implicit BCL references (from csproj `<Reference>` elements without `HintPath`): `System`, `System.Data`, `System.Data.DataSetExtensions`, `System.Drawing`, `System.Web`, `System.Web.Abstractions`, `System.Web.ApplicationServices`, `System.Web.DynamicData`, `System.Web.Entity`, `System.Web.Extensions`, `System.Web.Routing`, `System.Web.Services`, `System.Xml`, `System.Xml.Linq`, `System.Configuration`, `System.ComponentModel.DataAnnotations`, `System.EnterpriseServices`, `System.Messaging`, `System.Net.Http`, `System.Net.Http.WebRequest`, `Microsoft.CSharp`, `netstandard 2.0.0.0`.

## Dev Dependencies

No separate dev-only NuGet packages declared (the project uses the `packages.config` format and does not differentiate `<DevelopmentDependency>`). `Microsoft.EntityFrameworkCore.Tools 3.1.32` is the only tool-style package (used for EF migrations / scaffolding) but it is listed alongside runtime deps.

## Entry Points

| File | Type | Start Command / Runtime Host |
|------|------|------------------------------|
| `src/ContosoUniversity/Global.asax` + `Global.asax.cs` (`MvcApplication.Application_Start`) | Web — ASP.NET MVC application bootstrap | IIS Express (configured in csproj: `<IISUrl>https://localhost:44300/</IISUrl>`, `<DevelopmentServerPort>58801</DevelopmentServerPort>`); `scripts/startapp.cmd`, `scripts/startapp.sh` |
| `src/ContosoUniversity/App_Start/RouteConfig.cs` | URL routing entry — single default route `{controller}/{action}/{id}`, defaults to `Home/Index` | Loaded by `Application_Start` |
| `src/ContosoUniversity/App_Start/BundleConfig.cs` | Bundles registration (jQuery, jQueryVal, Modernizr, Bootstrap, CSS) | Loaded by `Application_Start` |
| `src/ContosoUniversity/App_Start/FilterConfig.cs` | Global MVC filters (`HandleErrorAttribute`); `AuthorizeAttribute` is commented out | Loaded by `Application_Start` |
| `src/ContosoUniversity/Data/DbInitializer.cs` (called from `Global.asax.cs::InitializeDatabase`) | Database seeding on app start | Runs once at `Application_Start` |
| `src/ContosoUniversity/scripts/deploy-to-azure.cmd` / `deploy-to-azure.sh` | Deployment script | Manual invocation |
| `src/ContosoUniversity/scripts/cleanup-azure-resources.cmd` / `.sh` | Tear-down script | Manual invocation |

There are no separate background workers, no console / CLI entry points, no serverless function manifests, and no separate API project — the application is a single ASP.NET MVC web app with both UI and HTTP endpoints in the same process.

## Directory Structure

```
src/ContosoUniversity/
├── App_Start/                # MVC startup configuration (BundleConfig, FilterConfig, RouteConfig)
├── Controllers/              # 8 MVC controllers (Base, Home, Students, Courses, Instructors,
│                             #   Departments, Notifications, MessageQueueTest)
├── Data/                     # EF Core DbContext (SchoolContext), DbInitializer, SchoolContextFactory
├── Examples/                 # MessageQueueExample.cs (sample/demo code, compiled into the app)
├── Infrastructure/           # MSMQ wrappers (IMessageQueue, MessageQueue, InMemoryMessageQueue,
│                             #   Message, MessageQueueExceptions, MessageQueueManager)
├── Models/                   # Domain entities (Course, CourseAssignment, Department, Enrollment,
│                             #   Instructor, OfficeAssignment, Person, Student, Notification,
│                             #   ErrorViewModel) and Models/SchoolViewModels/ (3 view models)
├── Services/                 # NotificationService, LoggingService
├── Views/                    # Razor views, organized by controller (Home, Students, Courses,
│                             #   Instructors, Departments, Notifications, MessageQueueTest, Shared)
├── Content/                  # Site.css, Bootstrap CSS, notifications.css
├── Scripts/                  # Vendored jQuery, jQuery.Validation, Bootstrap, Modernizr, respond.js
├── Properties/               # AssemblyInfo.cs
├── Uploads/TeachingMaterials/ # Runtime upload directory (one sample course image present)
├── App_Start/                # (listed above)
├── packages/                 # NuGet packages (legacy packages.config restore output)
├── bin/, obj/                # Build outputs (excluded from extension census above)
├── scripts/                  # Operational shell scripts: startapp / stopapp / deploy-to-azure /
│                             #   cleanup-azure-resources (.cmd + .sh pairs)
├── doc-media/                # Documentation media assets
├── ContosoUniversity.sln     # Visual Studio 17.5.2 solution
├── ContosoUniversity.csproj  # Web Application project (MSBuild ToolsVersion 15.0)
├── Global.asax + Global.asax.cs   # ASP.NET application lifecycle hooks
├── Web.config, Web.Debug.config, Web.Release.config  # ASP.NET configuration + transforms
├── packages.config           # NuGet packages list (44 packages)
├── PaginatedList.cs          # Generic paginated-list helper used by controllers
├── README.md                 # Project README
├── README_MessageQueue.md    # MSMQ subsystem documentation
├── NOTIFICATION_SYSTEM_README.md  # Notification feature documentation
├── SETUP_TESTING_GUIDE.md    # Manual setup / testing guidance
├── TEACHING_MATERIAL_UPLOAD.md  # Documentation for teaching-material image uploads
└── PROMPTS.md                # Misc prompts / notes
```

## Additional Observations

- The solution contains a **single project** (`ContosoUniversity.csproj`); there are no separate API, worker, test, or library projects.
- Project flavor GUIDs in csproj indicate **ASP.NET MVC + C# Web Application** (`349c5851-…` + `fae04ec0-…`).
- The csproj `<TargetFrameworkVersion>` is `v4.8`, while every NuGet `packages.config` entry uses `targetFramework="net482"`. Both refer to .NET Framework 4.8.x; the README states **.NET Framework 4.8.2**.
- The connection string in `Web.config` points to **SQL Server LocalDB** (`(LocalDb)\MSSQLLocalDB`, database `ContosoUniversityNoAuthEFCore`, `Integrated Security=True`).
- `appSettings` declares `NotificationQueuePath = .\Private$\ContosoUniversityNotifications` for **Microsoft Message Queuing (MSMQ)**.
- `Web.config` configures `httpRuntime maxRequestLength="10240"` (10 MB), `executionTimeout="3600"` (60 min), and `requestFiltering maxAllowedContentLength="10485760"` (10 MB).
- The csproj declares **IIS Express integrated authentication only** (`<IISExpressAnonymousAuthentication>disabled</IISExpressAnonymousAuthentication>`, `<IISExpressWindowsAuthentication>enabled</IISExpressWindowsAuthentication>`).
- The project carries a custom **`CopySQLClientNativeBinaries`** MSBuild target that copies `Microsoft.Data.SqlClient.SNI.dll` (x64 + x86) from `packages/` into `bin/` and `bin/x64/`, `bin/x86/`.
- `FilterConfig.cs` registers a global `HandleErrorAttribute`; a previously-present global `AuthorizeAttribute` is commented out with the note "we're implementing role-based authorization".
- There are **no test projects, no test files, no `[TestClass]`/`[Fact]`/`[Test]` attributes, and no test framework references** anywhere in the project. The file `Controllers/MessageQueueTestController.cs` is an MVC controller for an in-app MSMQ test feature, not a unit-test class.
- There is **no CI/CD configuration** in the project (no `.github/workflows/*.yml`, no `azure-pipelines.yml`, no `.gitlab-ci.yml`, no `Jenkinsfile` under `src/ContosoUniversity/`).
- There is **no infrastructure-as-code** under `src/ContosoUniversity/` (no Bicep, Terraform, ARM, or CDK files). The repository-level `vm-creation/terraform/` and `lab-creation/` directories are lab-environment scaffolding, not application infrastructure.
- There are **no Dockerfiles, docker-compose files, or `.dockerignore` files** under `src/ContosoUniversity/`.
- The project includes 5 in-tree documentation files: `README.md`, `README_MessageQueue.md`, `NOTIFICATION_SYSTEM_README.md`, `SETUP_TESTING_GUIDE.md`, `TEACHING_MATERIAL_UPLOAD.md`. The csproj also references a `ROLE_SETUP_GUIDE.md` content item, but no such file is present in the project root.
- There are 4 operational shell scripts under `scripts/` (paired `.cmd` + `.sh`): `startapp`, `stopapp`, `deploy-to-azure`, `cleanup-azure-resources`.
- `Uploads/TeachingMaterials/` contains one runtime-uploaded JPG (`course_1045_2b7f6522-b007-4c5d-9304-57b3ef4a182c.jpg`) and a `.gitkeep`. This indicates **the application persists user-uploaded files to local disk**.
- The `.csproj` lists `Examples/MessageQueueExample.cs` as a `<Compile>` item — sample code is **compiled into the production assembly**, not separated into a sample project.
