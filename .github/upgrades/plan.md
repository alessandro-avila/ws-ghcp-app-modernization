# ContosoUniversity .NET Framework 4.8 to .NET 10.0 Upgrade Plan

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Migration Strategy](#migration-strategy)
3. [Detailed Dependency Analysis](#detailed-dependency-analysis)
4. [Implementation Timeline](#implementation-timeline)
5. [Project-by-Project Migration Plans](#project-by-project-migration-plans)
6. [Package Update Reference](#package-update-reference)
7. [Breaking Changes Catalog](#breaking-changes-catalog)
8. [Risk Management](#risk-management)
9. [Testing & Validation Strategy](#testing--validation-strategy)
10. [Complexity & Effort Assessment](#complexity--effort-assessment)
11. [Source Control Strategy](#source-control-strategy)
12. [Success Criteria](#success-criteria)

---

## Executive Summary

### Scenario Overview

This plan details the upgrade of **ContosoUniversity** from **.NET Framework 4.8** to **.NET 10.0 (Long Term Support)**. This upgrade represents a **major architectural transition** from legacy ASP.NET Framework to modern ASP.NET Core.

### Scope & Metrics

| Metric | Value | Status |
|--------|-------|--------|
| **Projects** | 1 project | All require upgrade |
| **Project Dependencies** | 0 | Simple structure |
| **Current Framework** | .NET Framework 4.8 | Legacy |
| **Target Framework** | .NET 10.0 (LTS) | Modern |
| **Total NuGet Packages** | 45 | 26 need upgrade, 2 incompatible |
| **Code Files** | 66 files | 27 files with incidents |
| **Lines of Code** | 4,406 LOC | 565+ LOC to modify (12.8%) |
| **Total Issues** | 621 incidents | High volume |
| **API Incompatibilities** | 565 APIs | 521 binary incompatible |

### Complexity Assessment

**Overall Complexity: ?? High**

**Key Challenges:**
1. **Architectural Migration**: Complete transition from ASP.NET Framework (System.Web.*) to ASP.NET Core
   - 541 incompatible APIs (95.8% of issues) require ASP.NET Core equivalents
   - MVC pattern migration: Controllers, Views, Routing, Configuration
   - Bundling/Minification system replacement
   
2. **Security Vulnerabilities**: 
   - `Microsoft.Data.SqlClient 2.1.4` ? CVE-2024-0056
   - `Microsoft.Identity.Client 4.21.1` ? Deprecated

3. **Package Ecosystem Shift**:
   - 7 packages functionality now included in framework
   - 2 packages incompatible/deprecated
   - 24 packages require major version upgrades

4. **Breaking API Changes**:
   - 521 binary incompatible APIs (require code changes)
   - 44 source incompatible APIs (recompilation + fixes)
   - Entity Framework Core 3.1.32 ? 10.0.1 (major version jump)

### Selected Strategy

**All-at-Once Strategy** - Atomic upgrade of the single project in coordinated phases.

**Rationale**:
- Single project with no dependencies enables atomic conversion
- Complete framework shift requires coordinated changes across project file, packages, and code
- All-at-once approach avoids complex multi-targeting scenarios
- Clear testing boundary (entire application validates together)

**Approach**:
1. **Phase 0: Prerequisites** - SDK validation, tooling readiness
2. **Phase 1: Project Conversion** - SDK-style project file, target framework update
3. **Phase 2: Package Updates** - Address security vulnerabilities, update compatible packages
4. **Phase 3: ASP.NET Core Migration** - Replace System.Web.* APIs with ASP.NET Core equivalents
5. **Phase 4: Build & Compilation** - Resolve breaking changes, achieve clean build
6. **Phase 5: Testing & Validation** - Functional verification

### Critical Path

The migration follows a strict dependency order:
```
Prerequisites ? Project Conversion ? Package Updates ? ASP.NET Core Migration ? Build ? Validation
```

Each phase must complete successfully before proceeding.

### Risk Highlights

| Risk | Severity | Mitigation |
|------|----------|------------|
| ASP.NET Core architectural differences | ?? High | Incremental code migration, reference ASP.NET Core migration guide |
| Security vulnerabilities in current packages | ?? High | Prioritize package updates in Phase 2 |
| Entity Framework 3.1 ? 10.0 breaking changes | ?? Medium | Review EF Core 10.0 breaking changes, test database operations |
| Configuration system changes (web.config ? appsettings.json) | ?? Medium | Systematic configuration migration |
| Bundling/minification replacement | ?? Medium | Adopt WebOptimizer or Vite |

### Timeline Summary

- **Total Phases**: 6 (0-5)
- **Estimated Complexity**: High (architectural migration)
- **Recommended Approach**: Dedicated migration effort with testing at each phase
- **Rollback Strategy**: Git branch isolation, atomic commits per phase

---

## Migration Strategy

### Selected Approach: All-at-Once Strategy

**Definition**: Upgrade the entire ContosoUniversity project simultaneously in a single coordinated operation, updating target framework, packages, and code in atomic phases.

### Strategy Rationale

| Factor | Decision Driver |
|--------|----------------|
| **Project Count** | Single project ? No coordination complexity |
| **Dependencies** | Zero inter-project dependencies ? No sequencing constraints |
| **Current Framework** | .NET Framework 4.8 ? Complete architectural shift needed |
| **Target Framework** | .NET 10.0 LTS ? Single target, no multi-targeting |
| **Package Compatibility** | All packages have known upgrade paths or replacements |
| **Risk Tolerance** | Isolated branch + atomic commits ? Controlled rollback |

### Why Not Incremental?

Incremental (phased) migration is **not suitable** because:
- Only 1 project (no coordination needed)
- ASP.NET Framework ? ASP.NET Core requires complete architectural shift (cannot run partially migrated)
- No intermediate multi-targeting benefit (Framework 4.8 and .NET 10.0 are incompatible architectures)

### Migration Phases

The All-at-Once strategy executes through **6 structured phases**:

#### Phase 0: Prerequisites (Validation)
**Scope**: Environment readiness validation  
**Actions**:
- Verify .NET 10.0 SDK installed
- Validate global.json compatibility (if present)
- Ensure Visual Studio/tooling supports .NET 10.0

**Deliverable**: Ready development environment

---

#### Phase 1: Project Conversion (Atomic)
**Scope**: Convert project file structure and target framework  
**Actions**:
- Convert ContosoUniversity.csproj from classic to SDK-style format
- Update `<TargetFramework>` from `net48` to `net10.0`
- Remove obsolete project file elements (e.g., assembly references now included in framework)
- Add `<Project Sdk="Microsoft.NET.Sdk.Web">` declaration

**Deliverable**: SDK-style project targeting .NET 10.0

---

#### Phase 2: Package Updates (Security-First)
**Scope**: Update all NuGet packages following security-first priority  
**Actions**:
1. **Security Updates** (immediate):
   - Microsoft.Data.SqlClient: 2.1.4 ? 6.1.3
   - Microsoft.Identity.Client: 4.21.1 ? 4.80.0

2. **Remove Packages** (functionality included in framework):
   - Microsoft.AspNet.Mvc, Microsoft.AspNet.Razor, Microsoft.AspNet.WebPages
   - Microsoft.CodeDom.Providers.DotNetCompilerPlatform
   - Microsoft.Web.Infrastructure
   - NETStandard.Library, System.Buffers, System.ComponentModel.Annotations, System.Memory, System.Numerics.Vectors, System.Threading.Tasks.Extensions

3. **Major Version Upgrades**:
   - Entity Framework Core: 3.1.32 ? 10.0.1 (all packages)
   - Microsoft.Extensions.*: 3.1.32 ? 10.0.1 (all packages)
   - System.Collections.Immutable: 1.7.1 ? 10.0.1
   - Others (see Package Update Reference)

4. **Replace Incompatible Packages**:
   - Microsoft.AspNet.Web.Optimization ? Remove (replace with WebOptimizer or Vite)
   - Antlr 3.4.1.9004 ? Antlr4 4.6.6

5. **Minor Updates**:
   - Newtonsoft.Json: 13.0.3 ? 13.0.4

**Deliverable**: Updated packages.config or PackageReference elements with .NET 10.0-compatible versions

---

#### Phase 3: ASP.NET Core Migration (Architectural)
**Scope**: Replace ASP.NET Framework APIs with ASP.NET Core equivalents  
**Actions**:
1. **MVC Controller Migration**:
   - Replace `System.Web.Mvc` namespace with `Microsoft.AspNetCore.Mvc`
   - Update controller base class: `Controller` remains, but namespace changes
   - Replace `ActionResult` types: `ViewResult`, `JsonResult`, `RedirectToRouteResult`, etc.
   - Update attribute routing: `[Route]`, `[HttpPost]`, `[HttpGet]`
   - Replace `ModelState`, `ViewBag`, `TempData` with ASP.NET Core equivalents

2. **Configuration Migration**:
   - Migrate web.config ? appsettings.json
   - Replace `ConfigurationManager.AppSettings` ? `IConfiguration` dependency injection
   - Update connection strings configuration

3. **Startup/Program.cs**:
   - Create Program.cs with WebApplication.CreateBuilder
   - Configure services (dependency injection)
   - Configure middleware pipeline
   - Set up Entity Framework Core DbContext

4. **View Engine**:
   - Verify Razor views compatibility (.cshtml files)
   - Update `@using` directives for ASP.NET Core namespaces
   - Replace `@Html.AntiForgeryToken()` ? `@Html.AntiForgeryToken()` (syntax compatible, but uses ASP.NET Core implementation)

5. **File Upload**:
   - Replace `HttpPostedFileBase` ? `IFormFile`
   - Update `Server.MapPath()` ? `IWebHostEnvironment.WebRootPath`

6. **Bundling/Minification**:
   - Remove `System.Web.Optimization` usage (BundleConfig.cs)
   - Replace with WebOptimizer NuGet package or Vite build tool
   - Update `_Layout.cshtml` to use new bundling approach

**Deliverable**: ASP.NET Core-compatible code

---

#### Phase 4: Build & Compilation (Iterative)
**Scope**: Resolve all compilation errors from framework and package upgrades  
**Actions**:
- Restore NuGet packages: `dotnet restore`
- Build solution: `dotnet build`
- Address compilation errors:
  - Namespace resolution errors
  - Type/method signature mismatches
  - Breaking API changes from EF Core 3.1 ? 10.0
  - Breaking API changes from Microsoft.Extensions.* 3.1 ? 10.0
- Iterate until **0 build errors**

**Deliverable**: Solution builds successfully with 0 errors

---

#### Phase 5: Testing & Validation
**Scope**: Functional verification of migrated application  
**Actions**:
- Run application locally
- Test core functionality:
  - Student CRUD operations
  - Course management
  - Department management
  - Instructor management
  - Entity relationships (enrollments, assignments)
  - File uploads (teaching materials)
  - Database operations (EF Core queries, migrations)
  - Configuration loading
- Verify no runtime errors
- Check application logs for warnings/errors

**Deliverable**: Functional application with verified operations

---

## Implementation Timeline

The All-at-Once strategy executes through **6 sequential phases** with clear deliverables and validation gates.

### Phase Overview

| Phase | Scope | Deliverable | Risk Level |
|-------|-------|-------------|------------|
| **Phase 0** | Prerequisites | Ready environment | ?? Low |
| **Phase 1** | Project Conversion | SDK-style project targeting net10.0 | ?? Medium |
| **Phase 2** | Package Updates | Security-patched, .NET 10.0-compatible packages | ?? High |
| **Phase 3** | ASP.NET Core Migration | ASP.NET Core-compatible code | ?? High |
| **Phase 4** | Build & Compilation | Solution builds with 0 errors | ?? Medium |
| **Phase 5** | Testing & Validation | Functional application | ?? Medium |

### Detailed Timeline

---

#### Phase 0: Prerequisites

**Objective**: Validate development environment readiness

**Actions**:
1. Verify .NET 10.0 SDK installed:
   ```bash
   dotnet --list-sdks
   # Expected: 10.0.xxx
   ```

2. Check global.json compatibility (if present):
   - Ensure SDK version constraint allows .NET 10.0
   - Update if necessary

3. Validate tooling:
   - Visual Studio 2022 (17.12+) or VS Code with C# extension
   - Entity Framework Core CLI tools

**Deliverable**: ? Environment ready for .NET 10.0 development

**Validation**: SDK version check passes

---

#### Phase 1: Project Conversion

**Objective**: Convert classic project to SDK-style format targeting .NET 10.0

**Actions** (performed as single coordinated operation):
1. Unload ContosoUniversity.csproj in Visual Studio
2. Edit .csproj file:
   - Replace project XML with SDK-style structure:
     ```xml
     <Project Sdk="Microsoft.NET.Sdk.Web">
       <PropertyGroup>
         <TargetFramework>net10.0</TargetFramework>
         <RootNamespace>ContosoUniversity</RootNamespace>
         <Nullable>enable</Nullable>
         <ImplicitUsings>enable</ImplicitUsings>
       </PropertyGroup>
       <!-- PackageReference elements -->
     </Project>
     ```
   - Remove obsolete elements: `<Reference>` for framework assemblies, `<Compile>` includes, assembly info
3. Convert packages.config ? `<PackageReference>` (if not already done)
4. Reload project

**Deliverable**: SDK-style project file targeting net10.0

**Validation**: Project loads without errors in IDE

---

#### Phase 2: Package Updates

**Objective**: Update all NuGet packages to .NET 10.0-compatible versions, prioritizing security fixes

**Actions** (performed as single coordinated batch):

1. **Remove Framework-Included Packages**:
   ```xml
   <!-- DELETE these PackageReference elements -->
   <PackageReference Include="Microsoft.AspNet.Mvc" Version="5.2.9" />
   <PackageReference Include="Microsoft.AspNet.Razor" Version="3.2.9" />
   <PackageReference Include="Microsoft.AspNet.WebPages" Version="3.2.9" />
   <PackageReference Include="Microsoft.CodeDom.Providers.DotNetCompilerPlatform" Version="2.0.1" />
   <PackageReference Include="Microsoft.Web.Infrastructure" Version="2.0.1" />
   <PackageReference Include="NETStandard.Library" Version="2.0.3" />
   <PackageReference Include="System.Buffers" Version="4.5.1" />
   <PackageReference Include="System.ComponentModel.Annotations" Version="4.7.0" />
   <PackageReference Include="System.Memory" Version="4.5.4" />
   <PackageReference Include="System.Numerics.Vectors" Version="4.5.0" />
   <PackageReference Include="System.Threading.Tasks.Extensions" Version="4.5.4" />
   ```

2. **Security Updates** (CRITICAL - do first):
   ```xml
   <PackageReference Include="Microsoft.Data.SqlClient" Version="6.1.3" />
   <PackageReference Include="Microsoft.Identity.Client" Version="4.80.0" />
   ```

3. **Entity Framework Core Upgrades**:
   ```xml
   <PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.1" />
   <PackageReference Include="Microsoft.EntityFrameworkCore.Abstractions" Version="10.0.1" />
   <PackageReference Include="Microsoft.EntityFrameworkCore.Analyzers" Version="10.0.1" />
   <PackageReference Include="Microsoft.EntityFrameworkCore.Relational" Version="10.0.1" />
   <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.1" />
   <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="10.0.1">
     <PrivateAssets>all</PrivateAssets>
     <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
   </PackageReference>
   ```

4. **Microsoft.Extensions.*** Upgrades:
   ```xml
   <PackageReference Include="Microsoft.Extensions.Caching.Abstractions" Version="10.0.1" />
   <PackageReference Include="Microsoft.Extensions.Caching.Memory" Version="10.0.1" />
   <PackageReference Include="Microsoft.Extensions.Configuration" Version="10.0.1" />
   <PackageReference Include="Microsoft.Extensions.Configuration.Abstractions" Version="10.0.1" />
   <PackageReference Include="Microsoft.Extensions.Configuration.Binder" Version="10.0.1" />
   <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="10.0.1" />
   <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.0.1" />
   <PackageReference Include="Microsoft.Extensions.Logging" Version="10.0.1" />
   <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="10.0.1" />
   <PackageReference Include="Microsoft.Extensions.Options" Version="10.0.1" />
   <PackageReference Include="Microsoft.Extensions.Primitives" Version="10.0.1" />
   ```

5. **Other Updates**:
   ```xml
   <PackageReference Include="Microsoft.Bcl.AsyncInterfaces" Version="10.0.1" />
   <PackageReference Include="Microsoft.Bcl.HashCode" Version="6.0.0" />
   <PackageReference Include="System.Collections.Immutable" Version="10.0.1" />
   <PackageReference Include="System.Diagnostics.DiagnosticSource" Version="10.0.1" />
   <PackageReference Include="System.Runtime.CompilerServices.Unsafe" Version="6.1.2" />
   <PackageReference Include="Newtonsoft.Json" Version="13.0.4" />
   ```

6. **Replace Incompatible Packages**:
   ```xml
   <ItemGroup>
     <!-- Antlr replacement -->
     <PackageReference Include="Antlr4" Version="4.6.6" />
     
     <!-- Bundling/Minification replacement -->
     <PackageReference Include="LigerShark.WebOptimizer.Core" Version="3.0.422" />
     <PackageReference Include="LigerShark.WebOptimizer.Sass" Version="3.0.119" />
   </ItemGroup>
   ```

7. **Client-Side Packages** (no changes needed):
   ```xml
   <ItemGroup>
     <PackageReference Include="bootstrap" Version="5.3.3" />
     <PackageReference Include="jQuery" Version="3.7.1" />
     <PackageReference Include="jQuery.Validation" Version="1.21.0" />
     <PackageReference Include="Microsoft.jQuery.Unobtrusive.Validation" Version="4.0.0" />
     <PackageReference Include="Modernizr" Version="2.6.2" />
     <PackageReference Include="WebGrease" Version="1.5.2" />
   </ItemGroup>
   ```

8. **Restore packages**:
   ```bash
   dotnet restore
   ```

**Expected Outcome**: All packages restored successfully for .NET 10.0

---

#### Phase 3: ASP.NET Core Migration

**Objective**: Replace ASP.NET Framework APIs with ASP.NET Core equivalents

**Actions** (performed as single coordinated operation):

[Reference Breaking Changes Catalog section for comprehensive list]

1. **Create Program.cs** (new file):
   ```csharp
   var builder = WebApplication.CreateBuilder(args);
   
   // Add services
   builder.Services.AddControllersWithViews();
   builder.Services.AddDbContext<SchoolContext>(options =>
       options.UseSqlServer(builder.Configuration.GetConnectionString("SchoolContext")));
   
   var app = builder.Build();
   
   // Configure middleware
   if (!app.Environment.IsDevelopment())
   {
       app.UseExceptionHandler("/Home/Error");
       app.UseHsts();
   }
   
   app.UseHttpsRedirection();
   app.UseStaticFiles();
   app.UseRouting();
   app.UseAuthorization();
   
   app.MapControllerRoute(
       name: "default",
       pattern: "{controller=Home}/{action=Index}/{id?}");
   
   app.Run();
   ```

2. **Delete Files** (no longer needed):
   - Global.asax / Global.asax.cs
   - App_Start/RouteConfig.cs
   - App_Start/FilterConfig.cs
   - App_Start/BundleConfig.cs (replace with WebOptimizer configuration)
   - web.config (migrate settings to appsettings.json)

3. **Create appsettings.json**:
   ```json
   {
     "ConnectionStrings": {
       "SchoolContext": "<migrate from web.config>"
     },
     "Logging": {
       "LogLevel": {
         "Default": "Information",
         "Microsoft.AspNetCore": "Warning"
       }
     },
     "AllowedHosts": "*"
   }
   ```

4. **Update Controllers** (all 27 files with incidents):
   - Replace `using System.Web.Mvc;` ? `using Microsoft.AspNetCore.Mvc;`
   - Update action results:
     - `ViewResult` ? `IActionResult` or `ViewResult` (namespace Microsoft.AspNetCore.Mvc)
     - `JsonResult` ? `IActionResult` with `return Json(data)`
     - `RedirectToRouteResult` ? `RedirectToActionResult`
     - `HttpStatusCodeResult` ? `StatusCodeResult` or `BadRequestResult`, `NotFoundResult`
   - Replace `HttpPostedFileBase` ? `IFormFile`
   - Replace `Server.MapPath()` ? Inject `IWebHostEnvironment`, use `webHostEnvironment.WebRootPath`
   - Update attributes:
     - `[ValidateAntiForgeryToken]` ? Same (namespace Microsoft.AspNetCore.Mvc)
     - `[Bind]` ? `[Bind]` (namespace Microsoft.AspNetCore.Mvc)
     - `[HttpPost]`, `[HttpGet]` ? Same (namespace Microsoft.AspNetCore.Mvc)

5. **Update Views** (.cshtml files):
   - Update `@using` directives:
     ```cshtml
     @using Microsoft.AspNetCore.Mvc
     @using ContosoUniversity.Models
     ```

   - Replace `@Html.AntiForgeryToken()` ? Same (ASP.NET Core implementation)
   - Update bundling references in _Layout.cshtml (if using WebOptimizer)

6. **Update Configuration Access**:
   - Replace `ConfigurationManager.AppSettings["key"]`:
     ```csharp
     // Before
     var value = ConfigurationManager.AppSettings["NotificationQueuePath"];
     
     // After (inject IConfiguration)
     private readonly IConfiguration _configuration;
     public NotificationService(IConfiguration configuration)
     {
         _configuration = configuration;
     }
     var value = _configuration["NotificationQueuePath"];
     ```

7. **Entity Framework DbContext**:
   - Update context registration in Program.cs (already shown above)
   - Review EF Core 10.0 breaking changes (see Breaking Changes Catalog)

8. **Breaking API Fixes**:
   - `TimeSpan.FromSeconds(double)` ? `TimeSpan.FromSeconds(int)` where applicable
   - Address other source incompatibilities (see Breaking Changes Catalog)

**Deliverable**: ASP.NET Core-compatible codebase

---

##### Code Change Patterns

[Refer to detailed code change patterns in Breaking Changes Catalog section]

| Issue | Resolution |
|-------|------------|
| `'HttpPostedFileBase' not found` | Change to `IFormFile` |
| `'ActionResult' is ambiguous` | Fully qualify: `Microsoft.AspNetCore.Mvc.ActionResult` or use `IActionResult` |
| `'Server' does not exist` | Inject `IWebHostEnvironment`, use `_webHostEnvironment.WebRootPath` |
| `'ConfigurationManager' not found` | Inject `IConfiguration`, use `_configuration["key"]` |
| `'ViewBag' not found` | Add `using Microsoft.AspNetCore.Mvc;` |
| `'ModelState' not found` | Add `using Microsoft.AspNetCore.Mvc;` |
| `Cannot implicitly convert type 'System.Threading.Tasks.Task<>' to 'IActionResult'` | Use `return await View(...)` or `return RedirectToAction(...)` |
| `The name 'Content' does not exist in the current context` | Use `_webHostEnvironment.WebRootPath` to access wwwroot |
| `Multiple types don't have a binary-compatible membership` (RowVersion) | Ensure RowVersion property is byte[] and nullable |
| `The current type is not a valid enumeration type` | Check enum definitions and values in database |
| `InvalidOperationException: Call to a member function...`| Ensure correct async/await usage with EF Core queries |
| `Value cannot be null. (Parameter 'source')`| Check for null values in navigation properties or query results |

**Note**: This is not an exhaustive list. Developer discretion and reference to ASP.NET Core documentation is advised.

---

#### Phase 4: Build & Compilation

**Objective**: Achieve clean build (0 errors)

**Actions** (iterative until success):
1. Restore dependencies:
   ```bash
   dotnet restore
   ```

2. Build solution:
   ```bash
   dotnet build
   ```

3. Address compilation errors:
   - Namespace resolution errors ? Add missing `using` directives
   - Type mismatches ? Update types per ASP.NET Core equivalents
   - Method signature changes ? Consult breaking changes catalog
   - EF Core 10.0 breaking changes ? Update LINQ queries, context configuration

4. Common error resolutions:
   
   | Error | Resolution |
   |-------|------------|
   | `'HttpPostedFileBase' not found` | Change to `IFormFile` |
   | `'ActionResult' is ambiguous` | Fully qualify: `Microsoft.AspNetCore.Mvc.ActionResult` or use `IActionResult` |
   | `'Server' does not exist` | Inject `IWebHostEnvironment`, use `_webHostEnvironment.WebRootPath` |
   | `'ConfigurationManager' not found` | Inject `IConfiguration`, use `_configuration["key"]` |
   | `'ViewBag' not found` | Add `using Microsoft.AspNetCore.Mvc;` |
   | `'ModelState' not found` | Add `using Microsoft.AspNetCore.Mvc;` |

5. Repeat until:
   ```
   Build succeeded.
       0 Warning(s)
       0 Error(s)
   ```

**Deliverable**: Solution builds with 0 errors

---

#### Phase 5: Testing & Validation

**Objective**: Verify application functions correctly

**Actions**:
1. Run application:
   ```bash
   dotnet run --project ContosoUniversity.csproj
   ```
   Expected: Application starts on https://localhost:5001 (or configured port)

2. Database Initialization:
   - Verify database connection
   - Run migrations if needed:
     ```bash
     dotnet ef database update
     ```

3. Functional Tests:

   | Feature | Test Steps | Expected Result |
   |---------|------------|-----------------|
   | **Home Page** | Navigate to / | Home page loads |
   | **Students - List** | Navigate to /Students | Student list displays |
   | **Students - Create** | Click Create, fill form, submit | New student created |
   | **Students - Edit** | Click Edit on student, modify, save | Student updated |
   | **Students - Delete** | Click Delete, confirm | Student removed |
   | **Students - Details** | Click Details | Student details page |
   | **Courses - List** | Navigate to /Courses | Course list displays |
   | **Courses - Create** | Create course with file upload | Course created, file uploaded |
   | **Courses - Edit** | Edit course, upload new file | Course updated, file replaced |
   | **Departments - List** | Navigate to /Departments | Department list displays |
   | **Departments - Edit** | Edit department (test concurrency) | Optimistic concurrency works |
   | **Instructors - List** | Navigate to /Instructors | Instructor list with courses |
   | **Relationships** | View enrollments, course assignments | Related data displays correctly |

4. **Configuration Verification**:
   - Verify `appsettings.json` values load
   - Check connection string works
   - Validate `NotificationQueuePath` setting accessible

5. **Error Handling**:
   - Test invalid input (e.g., blank required fields)
   - Verify ModelState validation works
   - Check error pages display correctly

6. **Logging**:
   - Review console output for errors/warnings
   - Check log files (if configured)

**Deliverable**: Functional ASP.NET Core application

**Validation**: All critical user flows complete without errors

---

## Risk Management

### Risk Assessment Matrix

| Risk | Severity | Probability | Impact | Mitigation | Contingency |
|------|----------|-------------|--------|------------|-------------|
| **ASP.NET Core architectural differences** | ?? Critical | High | 541 API incompatibilities require code changes | Incremental migration following Breaking Changes Catalog; reference Microsoft migration guide | Allocate extra time for API migration; consider hiring ASP.NET Core expert |
| **Security vulnerabilities remain unpatched** | ?? Critical | Medium | CVE-2024-0056 in Microsoft.Data.SqlClient | Prioritize security updates in Phase 2; test database operations immediately after update | Rollback to Phase 1 if database operations fail; patch vulnerabilities separately |
| **Entity Framework Core 3.1 ? 10.0 breaking changes** | ?? Medium | Medium | Query behavior changes, performance regressions | Review EF Core 10.0 breaking changes documentation; test all database operations; compare generated SQL | Keep EF Core 3.1 test branch; detailed logging of query performance |
| **Configuration migration errors** | ?? Medium | Medium | Missing appsettings values cause runtime failures | Systematic web.config ? appsettings.json mapping; validate all configuration keys accessed | Keep web.config backup; add extensive configuration validation at startup |
| **File upload functionality broken** | ?? Medium | Low | Teaching material uploads fail | Test file upload immediately after migration; verify upload paths with IWebHostEnvironment | Implement fallback upload mechanism; detailed error logging |
| **Bundling/minification replacement fails** | ?? Medium | Low | CSS/JS assets not loading correctly | Test WebOptimizer configuration; verify bundles load in browser dev tools | Use individual file references temporarily; consider Vite as alternative |
| **Build errors blocking progress** | ?? Low | Medium | Compilation failures from breaking changes | Iterative build-fix-rebuild cycle; leverage Breaking Changes Catalog | Rollback to previous phase; seek community help with specific errors |
| **Dependency injection scope issues** | ?? Low | Low | DbContext lifetime issues causing data inconsistencies | Ensure DbContext registered with Scoped lifetime; test within request boundaries | Add explicit DbContext disposal; review DI best practices |
| **Routing configuration errors** | ?? Low | Low | Controller actions not accessible | Test all routes after migration; verify attribute routing | Refer to ASP.NET Core routing documentation; add explicit route debugging |
| **Performance degradation** | ?? Low | Low | Application slower than Framework version | Benchmark key scenarios; profile with dotnet-trace | Optimize query translation; enable response caching |

### High-Risk Areas

#### 1. ASP.NET Core Migration (Phase 3)

**Why High Risk**:
- 541 API incompatibilities (95.8% of issues)
- Complete architectural shift
- 565+ LOC require modification (12.8% of codebase)
- 27 files affected

**Mitigation Strategy**:
1. **Incremental Approach**: Migrate one controller at a time
2. **Pattern Consistency**: Apply Breaking Changes Catalog patterns systematically
3. **Frequent Builds**: Build after each controller migration to catch errors early
4. **Code Reviews**: Review migrated code for ASP.NET Core best practices
5. **Reference Implementation**: Keep Microsoft ASP.NET Core samples open for reference

**Contingency**:
- If migration complexity exceeds estimate, consider hiring ASP.NET Core consultant
- Allocate buffer time (20-30% of estimated effort)
- Have rollback plan per controller (use Git commits)

---

#### 2. Security Vulnerabilities (Phase 2)

**Why High Risk**:
- CVE-2024-0056 in Microsoft.Data.SqlClient 2.1.4
- Deprecated Microsoft.Identity.Client 4.21.1
- Immediate remediation required

**Mitigation Strategy**:
1. **Priority 1**: Update security packages FIRST in Phase 2
2. **Immediate Testing**: Test database operations immediately after update
3. **Rollback Plan**: If database operations fail, isolate security package update to separate branch
4. **Documentation**: Review security advisories for CVE-2024-0056

**Contingency**:
- If Microsoft.Data.SqlClient 6.1.3 causes compatibility issues, try intermediate versions (e.g., 5.x)
- Consider SystemWebAdapters package for interim compatibility
- Consult Microsoft documentation for migration path

---

#### 3. Entity Framework Core 10.0 (Phase 2 & 4)

**Why Medium-High Risk**:
- Major version jump (3.1.32 ? 10.0.1)
- Query translation behavior changes
- Potential SQL generation differences
- Migration tool updates

**Mitigation Strategy**:
1. **Before Migration**: Capture baseline query performance metrics
2. **After Migration**: 
   - Test all CRUD operations
   - Verify relationship navigation (Students-Enrollments, Courses-Departments, etc.)
   - Test concurrency handling (Department RowVersion)
   - Compare generated SQL for complex queries
3. **Testing**: Execute comprehensive database operation test suite
4. **Logging**: Enable EF Core logging to debug query issues

**Contingency**:
- Keep EF Core 3.1 test branch for comparison
- If performance degrades, profile with dotnet-trace and optimize queries
- Review [EF Core 10.0 breaking changes](https://docs.microsoft.com/ef/core/what-is-new/ef-core-10.0/breaking-changes) for specific issues

---

### Rollback Strategy

#### Per-Phase Rollback

Each phase should commit atomically, enabling surgical rollback:

| Phase | Rollback Command | Impact |
|-------|------------------|--------|
| **Phase 1** | `git reset --hard <phase-0-commit>` | Revert to classic project format |
| **Phase 2** | `git reset --hard <phase-1-commit>` | Revert package updates, keep SDK-style project |
| **Phase 3** | `git reset --hard <phase-2-commit>` | Revert ASP.NET Core migration, keep updated packages |
| **Phase 4** | `git checkout <file>` for specific files | Revert specific code changes causing build errors |
| **Phase 5** | `git reset --hard <phase-4-commit>` | Return to last successful build |

#### Full Migration Rollback

To abandon entire migration:

```bash
# Delete upgrade branch
git checkout main
git branch -D upgrade-to-NET10

# Or reset branch to starting point
git checkout upgrade-to-NET10
git reset --hard <starting-commit-sha>
```

**When to Rollback**:
- Blocker issue with no resolution path
- Estimated effort exceeds available time/budget
- Critical dependency incompatibility discovered
- Business priority changes

---

## Source Control Strategy

### Branching Strategy

**Branch Structure**:

```
main (or master)
??? upgrade-to-NET10 (feature branch)
    ??? Commit 0: Initial commit (starting point)
    ??? Commit 1: Phase 0 - Prerequisites validated
    ??? Commit 2: Phase 1 - Project converted to SDK-style, targeting net10.0
    ??? Commit 3: Phase 2 - All packages updated
    ??? Commit 4: Phase 3 - ASP.NET Core migration complete
    ??? Commit 5: Phase 4 - Solution builds with 0 errors
    ??? Commit 6: Phase 5 - Testing validated, migration complete
```

**Branch Naming**: `upgrade-to-NET10` (as specified)

**Branch Lifecycle**:
1. **Create Branch**: Before Phase 0
   ```bash
   git checkout -b upgrade-to-NET10
   ```

2. **Work Incrementally**: Commit after each phase completion

3. **Merge to Main**: After Phase 5 validation complete
   ```bash
   git checkout main
   git merge upgrade-to-NET10 --no-ff
   git tag v10.0.0-migration-complete
   ```

4. **Archive Branch**: Keep for reference
   ```bash
   # Don't delete immediately; keep for 30-60 days post-production
   ```

---

### Commit Strategy (All-at-Once)

**Principle**: **Atomic commits per phase** - Each phase results in ONE commit, enabling clean rollback.

#### Commit Structure

**Phase 0: Prerequisites**
```bash
git commit -m "Phase 0: Validate .NET 10.0 SDK and tooling

- Verified .NET 10.0 SDK installed
- Validated Visual Studio 2022 version
- Checked global.json compatibility (if exists)
- Environment ready for migration"
```

**Phase 1: Project Conversion**
```bash
git commit -m "Phase 1: Convert to SDK-style project targeting net10.0

- Converted ContosoUniversity.csproj to SDK-style format
- Updated TargetFramework to net10.0
- Removed obsolete project elements
- Deleted packages.config
- Project loads successfully in IDE"
```

**Phase 2: Package Updates**
```bash
git commit -m "Phase 2: Update all NuGet packages for .NET 10.0

Security Updates (CRITICAL):
- Microsoft.Data.SqlClient 2.1.4 ? 6.1.3 (CVE-2024-0056)
- Microsoft.Identity.Client 4.21.1 ? 4.80.0

Removed (framework-included):
- Microsoft.AspNet.Mvc, Razor, WebPages
- Microsoft.CodeDom.Providers.DotNetCompilerPlatform
- Microsoft.Web.Infrastructure
- NETStandard.Library, System.Buffers, etc. (11 packages)

Upgraded:
- Entity Framework Core 3.1.32 ? 10.0.1 (6 packages)
- Microsoft.Extensions.* 3.1.32 ? 10.0.1 (11 packages)
- System.Collections.Immutable, Diagnostics.DiagnosticSource, etc. (7 packages)

Replaced:
- Antlr 3.4.1.9004 ? Antlr4 4.6.6
- Microsoft.AspNet.Web.Optimization ? LigerShark.WebOptimizer.Core 3.0.422

Packages restored successfully"
```

**Phase 3: ASP.NET Core Migration**
```bash
git commit -m "Phase 3: Migrate to ASP.NET Core architecture

Infrastructure:
- Created Program.cs with WebApplicationBuilder
- Created appsettings.json with migrated configuration
- Deleted Global.asax, Global.asax.cs
- Deleted App_Start folder (RouteConfig, FilterConfig, BundleConfig)

Controllers (27 files):
- Replaced System.Web.Mvc ? Microsoft.AspNetCore.Mvc namespace
- Updated return types: ActionResult ? IActionResult
- Added dependency injection for SchoolContext
- Migrated file upload: HttpPostedFileBase ? IFormFile
- Replaced Server.MapPath() ? IWebHostEnvironment.WebRootPath

Services:
- Migrated ConfigurationManager ? IConfiguration injection
- Fixed TimeSpan API ambiguities (explicit double literals)

Views:
- Updated _ViewImports.cshtml namespaces
- Replaced bundling references in _Layout.cshtml with WebOptimizer

All ASP.NET Framework APIs replaced with ASP.NET Core equivalents"
```

**Phase 4: Build & Compilation**
```bash
git commit -m "Phase 4: Resolve compilation errors, achieve clean build

- Fixed namespace resolution errors
- Updated type mismatches per ASP.NET Core
- Resolved Entity Framework Core 10.0 breaking changes
- Added missing using directives
- Solution builds with 0 errors, 0 warnings"
```

**Phase 5: Testing & Validation**
```bash
git commit -m "Phase 5: Testing validated, migration complete

Functional Testing:
- All CRUD operations tested (Students, Courses, Departments, Instructors)
- File upload tested (teaching materials)
- Entity relationships verified (enrollments, assignments)
- Concurrency handling tested (Department RowVersion)
- Configuration loading validated

Integration Testing:
- Database operations functional
- Entity Framework Core queries execute correctly
- No runtime errors observed

Migration to .NET 10.0 complete and validated"
```

---

### Commit Frequency

**Recommended**:
- **1 commit per phase** (atomic phase commits)
- **Optional**: Intermediate commits within Phase 3 or 4 if needed (e.g., per-controller commits in Phase 3)

**Rationale**: Atomic commits enable clean rollback to any phase.

---

### Merge Strategy

**When to Merge to Main**:
- ? **After Phase 5 validation complete** (all tests pass)
- ? **After code review** (if team has review process)
- ? **After stakeholder approval** (if required)

**Merge Command** (recommended - preserve history):
```bash
git checkout main
git merge upgrade-to-NET10 --no-ff -m "Merge .NET 10.0 upgrade

Complete migration from .NET Framework 4.8 to .NET 10.0:
- Project converted to SDK-style format
- All packages updated (security vulnerabilities patched)
- ASP.NET Core architecture implemented
- All tests validated"

git tag v10.0.0
```

**Alternative** (squash merge - single commit):
```bash
git checkout main
git merge upgrade-to-NET10 --squash
git commit -m "Complete .NET 10.0 migration from Framework 4.8

[Include comprehensive summary of all changes]"
```

**Recommended**: `--no-ff` (no fast-forward) to preserve phase history.

---

### Code Review Process

**Review Checkpoints**:
1. **After Phase 2**: Review package updates (verify security patches)
2. **After Phase 3**: Review ASP.NET Core migration code (critical checkpoint)
3. **After Phase 5**: Final review before merge

**Review Checklist**:
- [ ] All `System.Web.Mvc` references removed
- [ ] Dependency injection implemented correctly
- [ ] Configuration migration complete (no web.config references)
- [ ] File upload code uses IFormFile
- [ ] TimeSpan API ambiguities resolved
- [ ] Entity Framework Core queries optimized
- [ ] Error handling implemented
- [ ] Logging configured
- [ ] Security best practices followed

**Tools**: GitHub Pull Request, Azure DevOps PR, or manual code review

---

### Rollback Procedures

#### Scenario 1: Rollback Single Phase

**Use Case**: Phase N failed; return to Phase N-1

**Command**:
```bash
git reset --hard <phase-N-1-commit-sha>
# OR
git revert <phase-N-commit-sha>
```

**Example**: Phase 3 ASP.NET Core migration caused issues, rollback to Phase 2:
```bash
git log --oneline  # Find Phase 2 commit SHA
git reset --hard abc1234  # Phase 2 commit
```

---

#### Scenario 2: Full Migration Rollback

**Use Case**: Abandon entire migration

**Command**:
```bash
git checkout main
git branch -D upgrade-to-NET10
```

**Caution**: Only use if migration unrecoverable. Consider keeping branch for future retry.

---

#### Scenario 3: Selective File Revert

**Use Case**: Specific file in Phase N has issues

**Command**:
```bash
git checkout <phase-N-1-commit-sha> -- path/to/file.cs
git add path/to/file.cs
git commit -m "Revert file.cs to Phase N-1 state"
```

---

### Branch Protection

**Recommended Settings** (if using GitHub/Azure DevOps):

**Main Branch**:
- ? Require pull request reviews (1+ approvals)
- ? Require status checks to pass (build, tests)
- ? Enforce linear history (optional)
- ? Block force pushes

**Upgrade Branch**:
- ?? Allow force pushes (for cleanup if needed)
- ?? No review required (working branch)

---

### Commit Message Best Practices

**Format**:
```
<Phase>: <Short description>

<Detailed description>
- Bullet point 1
- Bullet point 2

[Optional: Related work items, issue numbers]
```

**Example**:
```
Phase 3: Migrate StudentsController to ASP.NET Core

- Replaced System.Web.Mvc namespace
- Updated return types (ActionResult ? IActionResult)
- Added SchoolContext dependency injection
- Fixed ModelState validation
- Tested all CRUD operations

Related: #123
```

---

### .gitignore Updates

Ensure `.gitignore` excludes:
```gitignore
## .NET

### Build results
bin/
obj/
[Dd]ebug/
[Rr]elease/

### User-specific files
*.suo
*.user
*.sln.docstates

### NuGet
packages/
*.nupkg
project.lock.json

### Visual Studio
.vs/
*.vssscc
*.vsscc

### Entity Framework
*.mdf
*.ldf

### Uploads (runtime generated)
wwwroot/Uploads/
```

---

## Success Criteria

### Technical Success Criteria

The migration is considered **technically complete** when ALL of the following are met:

#### 1. Build Success
- [ ] `dotnet restore` completes without errors
- [ ] `dotnet build --configuration Release` succeeds with **0 errors**
- [ ] (Stretch goal) 0 warnings

#### 2. Project Configuration
- [ ] ContosoUniversity.csproj is SDK-style format (`<Project Sdk="Microsoft.NET.Sdk.Web">`)
- [ ] TargetFramework is `net10.0`
- [ ] All PackageReference elements use .NET 10.0-compatible versions

#### 3. Security Vulnerabilities Addressed
- [ ] Microsoft.Data.SqlClient updated to **6.1.3** (CVE-2024-0056 patched)
- [ ] Microsoft.Identity.Client updated to **4.80.0** (no longer deprecated)
- [ ] No security warnings from `dotnet list package --vulnerable`

#### 4. Code Migration
- [ ] **Zero** `System.Web.*` namespace references in code
- [ ] All controllers inherit from `Microsoft.AspNetCore.Mvc.Controller`
- [ ] All action methods return `IActionResult` or specific ASP.NET Core result types
- [ ] Dependency injection implemented for DbContext, IConfiguration, IWebHostEnvironment
- [ ] File upload uses `IFormFile`
- [ ] Configuration loads from `appsettings.json`
- [ ] Program.cs exists with WebApplicationBuilder
- [ ] Global.asax and App_Start folder deleted

#### 5. Package Ecosystem
- [ ] 7 framework-included packages removed
- [ ] 24 packages upgraded to .NET 10.0 versions
- [ ] 2 packages replaced (Antlr4, WebOptimizer)
- [ ] 19 compatible packages retained
- [ ] No NuGet dependency conflicts

---

### Functional Success Criteria

The migration is considered **functionally complete** when ALL of the following are met:

#### 6. Application Startup
- [ ] Application starts without exceptions: `dotnet run`
- [ ] Home page loads successfully at https://localhost:5001 (or configured port)
- [ ] No startup errors in console logs

#### 7. Database Operations
- [ ] Database connection successful (connection string from appsettings.json)
- [ ] Migrations apply successfully: `dotnet ef database update`
- [ ] All Entity Framework Core queries execute without errors

#### 8. CRUD Operations (Students)
- [ ] **List**: /Students displays student list
- [ ] **Create**: Create new student succeeds
- [ ] **Read**: Student details page displays correctly
- [ ] **Update**: Edit student succeeds
- [ ] **Delete**: Delete student succeeds

#### 9. CRUD Operations (Courses)
- [ ] **List**: /Courses displays course list
- [ ] **Create**: Create course with file upload succeeds
- [ ] **Read**: Course details page displays correctly
- [ ] **Update**: Edit course with new file upload succeeds
- [ ] **Delete**: Delete course succeeds

#### 10. CRUD Operations (Departments)
- [ ] **List**: /Departments displays department list
- [ ] **Create**: Create department succeeds
- [ ] **Read**: Department details page displays correctly
- [ ] **Update**: Edit department succeeds (test concurrency with RowVersion)
- [ ] **Delete**: Delete department succeeds

#### 11. CRUD Operations (Instructors)
- [ ] **List**: /Instructors displays instructor list with assigned courses
- [ ] Instructor details show enrolled students in courses

#### 12. File Upload
- [ ] Teaching material file upload works (Courses/Create)
- [ ] Uploaded files saved to `wwwroot/Uploads/TeachingMaterials/`
- [ ] File paths stored correctly in database
- [ ] Uploaded files accessible via browser (direct URL)

#### 13. Entity Relationships
- [ ] Student-Enrollment relationships display correctly
- [ ] Course-Department relationships display correctly
- [ ] Instructor-Course assignments display correctly
- [ ] Navigation properties work (lazy/eager loading)

#### 14. Validation & Error Handling
- [ ] ModelState validation works (required fields, data types)
- [ ] Validation errors display in UI
- [ ] Error pages render correctly (/Home/Error)
- [ ] Anti-forgery tokens validated on POST requests

#### 15. Configuration
- [ ] Connection string loads from appsettings.json
- [ ] NotificationQueuePath setting accessible
- [ ] All migrated AppSettings values load correctly
- [ ] Environment-specific settings work (Development vs Production)

---

### Quality Success Criteria

#### 16. Testing Coverage
- [ ] All functional test scenarios completed (see Testing Strategy section)
- [ ] Smoke tests pass
- [ ] Integration tests pass
- [ ] (If exists) Unit tests pass

#### 17. Performance
- [ ] Home page load time ? baseline + 10%
- [ ] Student list (100 records) load time ? baseline + 10%
- [ ] Course create (with file) ? baseline + 10%
- [ ] Complex database queries ? baseline + 10%

#### 18. Code Quality
- [ ] No code smells or anti-patterns
- [ ] Dependency injection used correctly (scoped, singleton, transient)
- [ ] Async/await used appropriately
- [ ] Error handling implemented
- [ ] Logging configured

#### 19. Documentation
- [ ] Migration plan completed (this document)
- [ ] Breaking changes documented
- [ ] Configuration changes documented (web.config ? appsettings.json mapping)
- [ ] Known issues documented (if any)
- [ ] Rollback procedure documented

---

### Process Success Criteria

#### 20. Source Control
- [ ] All phases committed to `upgrade-to-NET10` branch
- [ ] Commit messages follow convention
- [ ] Branch merged to `main` with `--no-ff`
- [ ] Migration tagged: `v10.0.0`

#### 21. Code Review
- [ ] Code review completed (if required)
- [ ] All review comments addressed
- [ ] Approval obtained from tech lead/senior developer

#### 22. Stakeholder Approval
- [ ] Project manager approval
- [ ] Product owner sign-off (if required)
- [ ] QA sign-off on functional testing

---

### Deployment Success Criteria (if applicable)

#### 23. Deployment Readiness
- [ ] Application runs on target server (e.g., Azure App Service, IIS, Kestrel)
- [ ] IIS/Kestrel configuration correct
- [ ] Database migrations run successfully on target database
- [ ] Static files serve correctly (wwwroot)
- [ ] SSL certificate configured (HTTPS)
- [ ] Environment variables configured correctly

#### 24. Production Validation
- [ ] Application accessible via production URL
- [ ] Smoke tests pass in production
- [ ] Monitoring configured (Application Insights, logging)
- [ ] No errors in production logs

---

### Definition of Done

**Migration is DONE when**:
- ? All 24 Success Criteria met
- ? No open blockers or critical issues
- ? Stakeholders sign off
- ? Production deployment successful (if applicable)

---

### Post-Migration Checklist

**After migration complete**:
- [ ] Update project documentation (README, wiki)
- [ ] Archive legacy .NET Framework 4.8 branch (for reference)
- [ ] Communicate migration completion to team
- [ ] Monitor production logs for 1 week
- [ ] Conduct retrospective (lessons learned)
- [ ] Document technical debt for future work

---

### Failure Criteria (When to Rollback/Abort)

**Abort migration if**:
- ? Critical security vulnerability cannot be resolved
- ? Performance degrades > 50% with no resolution path
- ? Database operations consistently fail
- ? Unresolvable NuGet dependency conflicts
- ? Effort exceeds 2x estimated time
- ? Business priority changes (migration no longer needed)

**In case of abortion**:
- Follow Rollback Strategy (see Source Control Strategy section)
- Document lessons learned
- Preserve `upgrade-to-NET10` branch for future retry

---

### Success Metrics (Post-Migration)

**Track these metrics for 30 days post-migration**:

| Metric | Target | Tracking Method |
|--------|--------|-----------------|
| **Application Uptime** | ? 99.9% | Monitoring tool (App Insights) |
| **Error Rate** | < 0.1% of requests | Application logs |
| **Performance** | ? baseline + 10% | APM tool |
| **User Complaints** | 0 critical issues | Support tickets |
| **Security Vulnerabilities** | 0 | `dotnet list package --vulnerable` |

**If any metric fails**: Investigate, fix, or rollback if necessary.

---

**End of Plan**
