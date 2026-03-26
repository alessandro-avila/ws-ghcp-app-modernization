# .NET Framework 4.8 → .NET 10.0 Migration Plan
## ContosoUniversity Project

## Table of Contents

- [Executive Summary](#executive-summary)
- [Migration Strategy](#migration-strategy)
- [Detailed Dependency Analysis](#detailed-dependency-analysis)
- [Project-by-Project Plans](#project-by-project-plans)
- [Package Update Reference](#package-update-reference)
- [Breaking Changes Catalog](#breaking-changes-catalog)
- [Testing & Validation Strategy](#testing--validation-strategy)
- [Risk Management](#risk-management)
- [Complexity & Effort Assessment](#complexity--effort-assessment)
- [Source Control Strategy](#source-control-strategy)
- [Success Criteria](#success-criteria)

---

## Executive Summary

### Overview
This plan outlines the migration of **ContosoUniversity**, an ASP.NET MVC 5 application targeting .NET Framework 4.8, to **ASP.NET Core on .NET 10.0**. This represents a major platform shift requiring comprehensive code changes, dependency updates, and architectural adjustments.

### Scope
- **Project**: ContosoUniversity.csproj
- **Current Framework**: .NET Framework 4.8
- **Target Framework**: .NET 10.0 (LTS)
- **Application Type**: ASP.NET MVC 5 → ASP.NET Core MVC
- **Codebase Size**: 4,423 lines of code
- **Impact**: 565+ lines requiring modification (12.8% of codebase)

### Key Challenges
1. **Project System**: Conversion from legacy project format to SDK-style
2. **ASP.NET Migration**: MVC 5 → ASP.NET Core patterns (routing, filters, bundling)
3. **API Compatibility**: 622 breaking changes across multiple APIs
4. **NuGet Packages**: 45 packages total — 26 need upgrades, 19 require replacements
5. **Application Initialization**: Global.asax.cs → Program.cs/Startup pattern

### Migration Complexity
**HIGH** — Full framework migration requiring:
- Complete project restructuring
- Replacement of ASP.NET-specific patterns
- Extensive code refactoring
- Comprehensive testing

### Estimated Timeline
- **Preparation & Setup**: 1-2 hours
- **Core Migration**: 8-12 hours
- **Testing & Validation**: 4-6 hours
- **Total**: 13-20 hours (spread across multiple sessions recommended)

## Migration Strategy

### Approach: All-at-Once Coordinated Migration

Since ContosoUniversity is a single project solution, we'll use a comprehensive single-pass migration approach with phased execution:

#### Phase 1: Foundation (SDK-Style Conversion)
Convert the project file to SDK-style format and update target framework. This is the prerequisite for all subsequent changes.

**Duration**: 30 minutes  
**Risk Level**: Low (automated tooling with rollback capability)

#### Phase 2: Package Modernization
Update, replace, or remove NuGet packages to ensure compatibility with .NET 10.0.

**Duration**: 1-2 hours  
**Risk Level**: Medium (potential for API surface changes)

#### Phase 3: Code Migration
Address API incompatibilities, breaking changes, and feature replacements identified in the assessment.

**Duration**: 6-8 hours  
**Risk Level**: High (manual refactoring required)

#### Phase 4: Application Initialization
Migrate from Global.asax.cs to ASP.NET Core startup pattern (Program.cs/Startup.cs).

**Duration**: 2-3 hours  
**Risk Level**: Medium-High (architectural change)

#### Phase 5: Validation & Testing
Build verification, runtime testing, and functional validation.

**Duration**: 4-6 hours  
**Risk Level**: Low (validation only, no changes)

### Rollback Strategy
- All changes tracked via Git on branch `upgrade-to-NET10-1`
- Atomic commits per phase enable selective rollback
- Original code preserved on `main` branch
- Incremental build validation after each phase catches issues early

### Key Principles
1. **Safety First**: Validate after each phase before proceeding
2. **Incremental Commits**: Small, focused commits enable easier troubleshooting
3. **Test Coverage**: Leverage existing tests to verify behavior preservation
4. **Documentation**: Track all manual changes for future reference

## Detailed Dependency Analysis

### NuGet Package Summary

**Total Packages**: 45  
**Action Required**: 26 packages  
**Included in Framework**: 19 packages

### Critical Package Categories

#### 1. ASP.NET Core Framework Packages
These packages are now part of the ASP.NET Core framework reference and should be **removed**:

- `Microsoft.AspNet.Mvc` (5.3.0) → Remove (included in framework)
- `Microsoft.AspNet.Razor` (3.3.0) → Remove (included in framework)
- `Microsoft.AspNet.WebPages` (3.3.0) → Remove (included in framework)

**Action**: Replace with `<FrameworkReference Include="Microsoft.AspNetCore.App" />`

#### 2. Incompatible ASP.NET Packages
These packages have no direct .NET Core equivalent and require **replacement**:

- `Microsoft.AspNet.Web.Optimization` (1.1.3) → Replace with direct HTML tags or WebOptimizer
- `Microsoft.Web.Infrastructure` (2.0.0) → Remove (not needed in ASP.NET Core)
- `Antlr` (3.5.1) → Remove (bundling dependency, no longer needed)
- `WebGrease` (1.6.0) → Remove (bundling dependency, no longer needed)

#### 3. Entity Framework Migration
**Current**: Entity Framework 6.5.1  
**Target**: Entity Framework Core 9.0+

**Packages to Replace**:
- `EntityFramework` (6.5.1) → `Microsoft.EntityFrameworkCore` (9.0.0)
- `EntityFramework` (6.5.1) → `Microsoft.EntityFrameworkCore.SqlServer` (9.0.0)

**Migration Considerations**:
- EF Core has significant API changes
- DbContext initialization differs
- LINQ query syntax mostly compatible
- Some advanced features may need rework

#### 4. Bootstrap & jQuery
**Current Versions**: Bootstrap 3.4.1, jQuery 3.7.1  
**Recommendation**: Update to latest versions
- `bootstrap` → 5.3.x (breaking changes in markup/classes)
- `jQuery` → 3.7.x (maintain current)

#### 5. Security & Vulnerability Updates
The following packages contain known vulnerabilities and must be updated:

- `System.Text.Json` (9.0.0) → Update to 9.0.1+
- `Microsoft.Data.SqlClient` (5.2.2) → Update to 5.2.3+ (CVE fixes)

### Package Update Priority

**Priority 1 (Critical)**: Security vulnerabilities  
**Priority 2 (Blocking)**: Framework incompatibilities  
**Priority 3 (Required)**: Deprecated packages  
**Priority 4 (Recommended)**: Version updates for compatibility

## Project-by-Project Plans

### ContosoUniversity.csproj

#### Current State
- **Framework**: .NET Framework 4.8
- **Type**: ASP.NET MVC 5 Web Application
- **Format**: Legacy .csproj with explicit file listings
- **Output**: Web application (IIS-hosted)

#### Target State
- **Framework**: .NET 10.0
- **Type**: ASP.NET Core MVC Application
- **Format**: SDK-style .csproj
- **Output**: Self-hosted Kestrel web application

---

### Execution Steps

#### Step 1: SDK-Style Conversion
**Tool**: `upgrade_convert_project_to_sdk_style`

**Before**:
```xml
<Project ToolsVersion="15.0" DefaultTargets="Build" xmlns="...">
  <Import Project="..." />
  <PropertyGroup>
    <TargetFrameworkVersion>v4.8</TargetFrameworkVersion>
    ...
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="System.Web.Mvc, Version=..." />
    ...
  </ItemGroup>
  <ItemGroup>
    <Compile Include="Controllers\HomeController.cs" />
    ...
  </ItemGroup>
</Project>
```

**After**:
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <!-- Package references here -->
  </ItemGroup>
</Project>
```

**Validation**: Build succeeds with SDK-style project

---

#### Step 2: Update NuGet Packages

**2.1 Remove Framework-Included Packages**
```xml
<!-- REMOVE these - now included via FrameworkReference -->
<PackageReference Include="Microsoft.AspNet.Mvc" Version="5.3.0" />
<PackageReference Include="Microsoft.AspNet.Razor" Version="3.3.0" />
<PackageReference Include="Microsoft.AspNet.WebPages" Version="3.3.0" />
```

**2.2 Replace Entity Framework**
```xml
<!-- REPLACE -->
<PackageReference Include="EntityFramework" Version="6.5.1" />

<!-- WITH -->
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="9.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="9.0.0" />
```

**2.3 Remove Incompatible Packages**
```xml
<!-- REMOVE - no longer needed -->
<PackageReference Include="Microsoft.AspNet.Web.Optimization" Version="1.1.3" />
<PackageReference Include="Microsoft.Web.Infrastructure" Version="2.0.0" />
<PackageReference Include="Antlr" Version="3.5.1" />
<PackageReference Include="WebGrease" Version="1.6.0" />
```

**2.4 Update Security-Critical Packages**
```xml
<!-- UPDATE for security fixes -->
<PackageReference Include="System.Text.Json" Version="9.0.1" />
<PackageReference Include="Microsoft.Data.SqlClient" Version="5.2.3" />
```

**Validation**: Package restore succeeds without errors

---

#### Step 3: Code Migration - Breaking Changes

**3.1 System.Web.Mvc → Microsoft.AspNetCore.Mvc**

The entire `System.Web.Mvc` namespace is incompatible. Key replacements:

**Controllers**:
```csharp
// BEFORE
using System.Web.Mvc;

namespace ContosoUniversity.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }
    }
}

// AFTER
using Microsoft.AspNetCore.Mvc;

namespace ContosoUniversity.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
```

**Filters**:
```csharp
// BEFORE
[HandleError]
public class HomeController : Controller { }

// AFTER
// Remove attribute - now handled by middleware
public class HomeController : Controller { }
```

**3.2 Entity Framework 6 → EF Core**

**DbContext Configuration**:
```csharp
// BEFORE (EF6)
public class SchoolContext : DbContext
{
    public SchoolContext() : base("SchoolContext")
    {
    }

    public DbSet<Student> Students { get; set; }

    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
        modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
    }
}

// AFTER (EF Core)
public class SchoolContext : DbContext
{
    public SchoolContext(DbContextOptions<SchoolContext> options)
        : base(options)
    {
    }

    public DbSet<Student> Students { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Conventions handled differently in EF Core
    }
}
```

**Connection String Configuration**:
```csharp
// BEFORE: Web.config
<connectionStrings>
  <add name="SchoolContext" 
       connectionString="Server=..." 
       providerName="System.Data.SqlClient" />
</connectionStrings>

// AFTER: appsettings.json
{
  "ConnectionStrings": {
    "SchoolContext": "Server=..."
  }
}

// Program.cs registration
builder.Services.AddDbContext<SchoolContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SchoolContext")));
```

**3.3 Routing Migration**

**RouteConfig.cs** (REMOVE):
```csharp
// BEFORE
public class RouteConfig
{
    public static void RegisterRoutes(RouteCollection routes)
    {
        routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
        routes.MapRoute(
            name: "Default",
            url: "{controller}/{action}/{id}",
            defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
        );
    }
}
```

**Program.cs** (ADD):
```csharp
// AFTER
var app = builder.Build();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
```

**3.4 Filter Registration**

**FilterConfig.cs** (REMOVE):
```csharp
// BEFORE
public class FilterConfig
{
    public static void RegisterGlobalFilters(GlobalFilterCollection filters)
    {
        filters.Add(new HandleErrorAttribute());
    }
}
```

**Program.cs** (ADD):
```csharp
// AFTER - Use middleware instead
app.UseExceptionHandler("/Home/Error");
app.UseHsts();
```

**Validation**: Code compiles without System.Web references

---

#### Step 4: Application Initialization Migration

**Global.asax.cs** (REMOVE/TRANSFORM):
```csharp
// BEFORE
public class MvcApplication : System.Web.HttpApplication
{
    protected void Application_Start()
    {
        AreaRegistration.RegisterAllAreas();
        FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
        RouteConfig.RegisterRoutes(RouteTable.Routes);
        BundleConfig.RegisterBundles(BundleTable.Bundles);
    }
}
```

**Program.cs** (CREATE):
```csharp
// AFTER
var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<SchoolContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SchoolContext")));

var app = builder.Build();

// Configure the HTTP request pipeline
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

**Validation**: Application starts and runs

---

#### Step 5: Bundling & Minification

**BundleConfig.cs** (REMOVE):
```csharp
// BEFORE
public class BundleConfig
{
    public static void RegisterBundles(BundleCollection bundles)
    {
        bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery-{version}.js"));
        // ... more bundles
    }
}
```

**Views/_Layout.cshtml** (UPDATE):
```html
<!-- BEFORE -->
@Scripts.Render("~/bundles/jquery")
@Scripts.Render("~/bundles/bootstrap")
@Styles.Render("~/Content/css")

<!-- AFTER -->
<link rel="stylesheet" href="~/lib/bootstrap/dist/css/bootstrap.min.css" />
<link rel="stylesheet" href="~/css/site.css" />
<script src="~/lib/jquery/dist/jquery.min.js"></script>
<script src="~/lib/bootstrap/dist/js/bootstrap.bundle.min.js"></script>
```

**Alternative**: Use LibMan or npm for client-side libraries

**Validation**: Static files served correctly

---

#### Step 6: Configuration Migration

**Web.config** → **appsettings.json**

**BEFORE (Web.config)**:
```xml
<configuration>
  <appSettings>
    <add key="webpages:Version" value="3.0.0.0" />
    <add key="MyCustomSetting" value="SomeValue" />
  </appSettings>
  <connectionStrings>
    <add name="SchoolContext" connectionString="..." />
  </connectionStrings>
</configuration>
```

**AFTER (appsettings.json)**:
```json
{
  "ConnectionStrings": {
    "SchoolContext": "Server=..."
  },
  "MyCustomSetting": "SomeValue",
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

**Validation**: Configuration values accessible via `IConfiguration`

---

### Post-Migration Checklist

- [ ] Project builds without errors
- [ ] All NuGet packages restore successfully
- [ ] Application starts and serves requests
- [ ] Database connection works
- [ ] Static files (CSS, JS, images) load correctly
- [ ] Routing works for all controllers/actions
- [ ] Views render without errors
- [ ] Entity Framework queries execute successfully
- [ ] Error handling works (try invalid routes)
- [ ] Configuration values accessible

## Package Update Reference

### Complete Package Migration Map

This section provides the detailed transformation for every package in the solution.

---

#### Category 1: Remove (Included in Framework)

These packages are now part of the ASP.NET Core framework reference and should be removed from PackageReferences.

| Package | Old Version | Action | Reason |
|---------|-------------|--------|--------|
| `Microsoft.AspNet.Mvc` | 5.3.0 | **REMOVE** | Included via `Microsoft.AspNetCore.App` |
| `Microsoft.AspNet.Razor` | 3.3.0 | **REMOVE** | Included via `Microsoft.AspNetCore.App` |
| `Microsoft.AspNet.WebPages` | 3.3.0 | **REMOVE** | Included via `Microsoft.AspNetCore.App` |
| `Microsoft.CodeDom.Providers.DotNetCompilerPlatform` | 4.1.0 | **REMOVE** | Not needed in .NET Core |
| `System.ComponentModel.Annotations` | 5.0.0 | **REMOVE** | Included in .NET 10.0 |
| `System.Diagnostics.DiagnosticSource` | 9.0.0 | **REMOVE** | Included in .NET 10.0 |

**Replacement**: Add framework reference to .csproj
```xml
<ItemGroup>
  <FrameworkReference Include="Microsoft.AspNetCore.App" />
</ItemGroup>
```

---

#### Category 2: Replace (Incompatible with .NET Core)

These packages have no .NET Core equivalent and must be replaced with alternative solutions.

| Package | Old Version | Action | Replacement |
|---------|-------------|--------|-------------|
| `Microsoft.AspNet.Web.Optimization` | 1.1.3 | **REPLACE** | Direct HTML tags or `WebOptimizer` |
| `Microsoft.Web.Infrastructure` | 2.0.0 | **REMOVE** | Not needed (was dependency of optimization) |
| `Antlr` | 3.5.1 | **REMOVE** | Not needed (was dependency of optimization) |
| `WebGrease` | 1.6.0 | **REMOVE** | Not needed (was dependency of optimization) |

**Bundling Replacement Options**:

**Option A: Direct HTML (Simplest)**
```html
<link rel="stylesheet" href="~/lib/bootstrap/dist/css/bootstrap.min.css" />
<script src="~/lib/jquery/dist/jquery.min.js"></script>
```

**Option B: WebOptimizer (Production)**
```bash
dotnet add package LigerShark.WebOptimizer.Core
```
```csharp
// Program.cs
builder.Services.AddWebOptimizer(pipeline =>
{
    pipeline.MinifyCssFiles("css/**/*.css");
    pipeline.MinifyJsFiles("js/**/*.js");
});
```

---

#### Category 3: Upgrade (Entity Framework)

Entity Framework 6 must be replaced with Entity Framework Core for .NET Core compatibility.

| Package | Old Version | New Package | New Version |
|---------|-------------|-------------|-------------|
| `EntityFramework` | 6.5.1 | `Microsoft.EntityFrameworkCore` | 9.0.0+ |
| `EntityFramework` | 6.5.1 | `Microsoft.EntityFrameworkCore.SqlServer` | 9.0.0+ |
| *(new)* | - | `Microsoft.EntityFrameworkCore.Tools` | 9.0.0+ |

**Installation**:
```bash
dotnet remove package EntityFramework
dotnet add package Microsoft.EntityFrameworkCore --version 9.0.0
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 9.0.0
dotnet add package Microsoft.EntityFrameworkCore.Tools --version 9.0.0
```

**Code Changes Required**: See "Breaking Changes Catalog" section for EF Core migration details.

---

#### Category 4: Update (Compatible with .NET Core)

These packages are compatible but should be updated to latest stable versions.

| Package | Old Version | New Version | Notes |
|---------|-------------|-------------|-------|
| `bootstrap` | 3.4.1 | 5.3.3 | Breaking changes in CSS classes |
| `jQuery` | 3.7.1 | 3.7.1 | Current version compatible |
| `Microsoft.jQuery.Unobtrusive.Validation` | 4.0.0 | 4.0.0 | Compatible, verify after update |
| `Microsoft.Data.SqlClient` | 5.2.2 | **5.2.3+** | Security update (CVE fix) |
| `Modernizr` | 2.8.3 | 2.8.3 | Consider removing (outdated) |
| `Newtonsoft.Json` | 13.0.3 | 13.0.3 | Compatible, or use System.Text.Json |
| `System.Text.Json` | 9.0.0 | **9.0.1+** | Security update |

**Bootstrap 3 → 5 Migration**:
If updating Bootstrap 3.4.1 → 5.3.3, be aware of breaking changes:
- `.panel` → `.card`
- `.well` → `.card`
- Glyphicons removed (use Bootstrap Icons or Font Awesome)

---

#### Category 5: Keep (Already Compatible)

These packages work with .NET 10.0 as-is (verify versions).

| Package | Version | Status |
|---------|---------|--------|
| `jQuery.Validation` | 1.21.0 | ✅ Compatible |
| `Microsoft.Extensions.*` | 9.0.0 | ✅ Compatible |

---

### Package Update Sequence

**Phase 1: Remove Framework-Included**
```bash
dotnet remove package Microsoft.AspNet.Mvc
dotnet remove package Microsoft.AspNet.Razor
dotnet remove package Microsoft.AspNet.WebPages
dotnet remove package Microsoft.CodeDom.Providers.DotNetCompilerPlatform
dotnet remove package System.ComponentModel.Annotations
dotnet remove package System.Diagnostics.DiagnosticSource
```

**Phase 2: Remove Incompatible**
```bash
dotnet remove package Microsoft.AspNet.Web.Optimization
dotnet remove package Microsoft.Web.Infrastructure
dotnet remove package Antlr
dotnet remove package WebGrease
```

**Phase 3: Replace Entity Framework**
```bash
dotnet remove package EntityFramework
dotnet add package Microsoft.EntityFrameworkCore --version 9.0.0
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 9.0.0
dotnet add package Microsoft.EntityFrameworkCore.Tools --version 9.0.0
```

**Phase 4: Update Security Fixes**
```bash
dotnet add package Microsoft.Data.SqlClient --version 5.2.3
dotnet add package System.Text.Json --version 9.0.1
```

**Phase 5: Optional Updates**
```bash
dotnet add package bootstrap --version 5.3.3  # If desired
```

---

### Post-Package-Update Checklist

After completing package updates:

- [ ] Run `dotnet restore` successfully
- [ ] Run `dotnet build` successfully
- [ ] No package conflict warnings
- [ ] No deprecated package warnings
- [ ] `<FrameworkReference Include="Microsoft.AspNetCore.App" />` added
- [ ] Web.config `<assemblies>` section removed (no longer needed)
- [ ] All necessary packages for features still present

---

### Package Troubleshooting

**Issue**: `dotnet restore` fails with version conflicts

**Solution**:
```bash
# Clear package cache
dotnet nuget locals all --clear

# Restore again
dotnet restore
```

**Issue**: "Could not load file or assembly" runtime error

**Solution**:
- Check that framework reference is present
- Verify package versions are compatible with .NET 10.0
- Check for duplicate assemblies (framework + package)

**Issue**: Package not found (e.g., older ASP.NET packages)

**Solution**:
- Verify package is compatible with .NET Core
- Check for renamed packages (e.g., `Microsoft.AspNet.*` → `Microsoft.AspNetCore.*`)
- Consult migration documentation for alternatives

## Breaking Changes Catalog

### High-Impact Breaking Changes

#### 1. System.Web.Mvc Namespace Removed
**Impact**: All controller actions, filters, and routing  
**Files Affected**: All controllers, filters, and routing configuration  
**Resolution**: Replace with `Microsoft.AspNetCore.Mvc`

**Key Type Changes**:
| .NET Framework | .NET Core/10.0 |
|----------------|----------------|
| `ActionResult` | `IActionResult` |
| `HttpContext.Current` | Inject `IHttpContextAccessor` |
| `RouteCollection` | `IEndpointRouteBuilder` |
| `GlobalFilterCollection` | Middleware pipeline |
| `ViewBag`/`ViewData` | Same (compatible) |

#### 2. Entity Framework 6 → EF Core Migration
**Impact**: Data access layer  
**Files Affected**: DbContext, models, configurations  
**Resolution**: Refactor for EF Core patterns

**Key Changes**:
- Constructor must accept `DbContextOptions<T>`
- `OnModelCreating` receives `ModelBuilder` instead of `DbModelBuilder`
- No parameterless constructor (requires DI)
- Conventions system redesigned
- Some LINQ methods changed (e.g., `Include` syntax)

#### 3. Configuration System Overhaul
**Impact**: All configuration access  
**Files Affected**: Controllers, services accessing settings  
**Resolution**: Migrate Web.config → appsettings.json

**Key Changes**:
- `ConfigurationManager.AppSettings` → `IConfiguration`
- `ConfigurationManager.ConnectionStrings` → `IConfiguration.GetConnectionString()`
- Must inject `IConfiguration` via DI
- Hierarchical configuration structure

#### 4. Bundling & Minification Removed
**Impact**: All script and style bundles  
**Files Affected**: Views using `@Scripts.Render()` and `@Styles.Render()`  
**Resolution**: Replace with direct HTML tags or use WebOptimizer

**Key Changes**:
- `BundleConfig` no longer exists
- `@Scripts.Render()` → `<script src="..."></script>`
- `@Styles.Render()` → `<link rel="stylesheet" href="..." />`
- Consider LibMan, npm, or WebOptimizer for production bundling

#### 5. Global.asax Replaced
**Impact**: Application startup  
**Files Affected**: Global.asax, Global.asax.cs  
**Resolution**: Migrate to Program.cs with minimal hosting model

**Key Changes**:
- `Application_Start()` → `Program.cs` initialization
- `HttpApplication` lifecycle events → Middleware pipeline
- `Application_Error()` → `app.UseExceptionHandler()`

### Medium-Impact Breaking Changes

#### 6. Dependency Injection Required
**Impact**: Service instantiation patterns  
**Files Affected**: Controllers, services  
**Resolution**: Register services in DI container

**Before**:
```csharp
var context = new SchoolContext();
```

**After**:
```csharp
// Constructor injection
public HomeController(SchoolContext context)
{
    _context = context;
}
```

#### 7. Action Filter Attributes
**Impact**: Custom filters  
**Files Affected**: Custom filter implementations  
**Resolution**: Implement new ASP.NET Core filter interfaces

**Key Changes**:
- `IActionFilter` interface signature changed
- `OnActionExecuting`/`OnActionExecuted` parameter types differ
- `FilterContext` → `ActionExecutingContext`/`ActionExecutedContext`

#### 8. Model Binding Changes
**Impact**: Form/query parameter binding  
**Files Affected**: Action methods with complex parameters  
**Resolution**: Add `[FromBody]`, `[FromQuery]`, `[FromForm]` attributes as needed

**Before** (implicit):
```csharp
public ActionResult Create(Student student)
```

**After** (explicit for non-GET):
```csharp
public IActionResult Create([FromForm] Student student)
```

### Low-Impact Breaking Changes

#### 9. HTML Helper Changes
**Impact**: View helper methods  
**Files Affected**: Razor views  
**Resolution**: Most helpers compatible; some require syntax updates

**Minor Changes**:
- `@Html.ActionLink` → Still works
- `@Html.BeginForm` → Still works
- `@Ajax` helpers → Removed (use JavaScript directly)

#### 10. Routing Attribute Syntax
**Impact**: Attribute-based routing  
**Files Affected**: Controllers using `[Route]`  
**Resolution**: Syntax mostly compatible; verify constraints

**Minor Syntax Changes**:
- Most routing attributes unchanged
- Constraint syntax slightly different
- Route parameter types explicit

### Complete Breaking API List

The assessment identified **622 distinct API incompatibilities**. Key namespaces affected:

1. **System.Web.Mvc** (completely replaced)
2. **System.Web.Optimization** (removed)
3. **System.Web.Routing** (replaced)
4. **System.Data.Entity** (replaced by EF Core)
5. **System.Configuration** (replaced by IConfiguration)

**See Assessment Report** for full API-level details with line numbers.

## Testing & Validation Strategy

### Validation Levels

#### Level 1: Compile-Time Validation
**Goal**: Ensure code compiles without errors

**Checkpoints**:
1. After SDK-style conversion → Build project
2. After package updates → Restore and build
3. After code migration → Full solution build
4. After startup migration → Build + startup check

**Tools**:
- `dotnet build`
- Visual Studio Error List
- `upgrade_build_project` tool

**Success Criteria**:
- Zero compilation errors
- Zero blocking warnings
- All package references resolve

---

#### Level 2: Runtime Validation
**Goal**: Ensure application starts and serves requests

**Test Cases**:
1. **Application Startup**
   - Application starts without exceptions
   - Kestrel listens on configured ports
   - DI container resolves all services

2. **Static Files**
   - CSS files load from `/css/` or `/lib/`
   - JavaScript files load and execute
   - Images display correctly

3. **Routing**
   - Default route `/` returns home page
   - Controller routes work: `/Home/Index`, `/Home/About`
   - Invalid routes show appropriate error page

4. **Database Connectivity**
   - DbContext initializes
   - Connection string resolves
   - Simple query executes (e.g., `Students.FirstOrDefault()`)

**Tools**:
- Browser manual testing
- `dotnet run`
- Application logs

**Success Criteria**:
- Application serves HTTP requests
- No startup exceptions
- Basic navigation works

---

#### Level 3: Functional Validation
**Goal**: Ensure business logic operates correctly

**Test Categories**:

**3.1 CRUD Operations**
- [ ] Create: Add new student record
- [ ] Read: View student list, individual student details
- [ ] Update: Edit existing student record
- [ ] Delete: Remove student record

**3.2 Entity Framework Queries**
- [ ] Simple queries (Find, FirstOrDefault)
- [ ] Filtered queries (Where, OrderBy)
- [ ] Include/navigation properties
- [ ] Paging/sorting operations

**3.3 Views & UI**
- [ ] Layout page renders
- [ ] Partial views render
- [ ] Forms submit and validate
- [ ] Model binding works
- [ ] Validation messages display

**3.4 Error Handling**
- [ ] 404 errors show error page
- [ ] Server errors handled gracefully
- [ ] Model validation errors display
- [ ] Database errors handled

**Success Criteria**:
- All CRUD operations work
- Data persists to database
- UI renders correctly
- Errors handled gracefully

---

#### Level 4: Integration Validation
**Goal**: Verify end-to-end scenarios

**Test Scenarios**:

**Scenario 1: Student Enrollment Flow**
1. Navigate to student list
2. Click "Create New"
3. Fill enrollment form
4. Submit and verify redirect
5. Confirm student appears in list
6. View student details
7. Edit enrollment date
8. Delete student record

**Scenario 2: Search & Filter**
1. Navigate to student list
2. Apply search filter
3. Verify filtered results
4. Sort by different columns
5. Page through results

**Scenario 3: Error Recovery**
1. Submit invalid form data
2. Verify validation errors
3. Correct errors and resubmit
4. Verify success

**Success Criteria**:
- Complete user workflows function
- Data consistency maintained
- Navigation flow correct

---

### Testing Checklist by Phase

#### After Phase 1 (SDK-Style Conversion)
- [x] Project file valid XML
- [ ] Project loads in Visual Studio
- [ ] `dotnet build` succeeds
- [ ] Target framework is `net10.0`

#### After Phase 2 (Package Updates)
- [ ] All packages restore without errors
- [ ] No package conflict warnings
- [ ] `dotnet build` succeeds
- [ ] No deprecated package warnings

#### After Phase 3 (Code Migration)
- [ ] All using statements resolve
- [ ] No `System.Web.*` references
- [ ] Controllers compile
- [ ] Models compile
- [ ] Views have no Razor errors
- [ ] Full solution build succeeds

#### After Phase 4 (Startup Migration)
- [ ] `Program.cs` created
- [ ] `Global.asax.cs` removed or emptied
- [ ] Application starts without errors
- [ ] Home page loads in browser
- [ ] Static files served correctly

#### After Phase 5 (Final Validation)
- [ ] All Level 1-4 tests pass
- [ ] Performance acceptable
- [ ] Logs show no warnings/errors
- [ ] Ready for deployment

---

### Regression Testing

**If Existing Unit Tests**:
1. Run all unit tests after each phase
2. Investigate failures (may need test updates)
3. Update tests for ASP.NET Core patterns if needed

**If No Existing Tests**:
Consider adding smoke tests for critical paths:
- Application starts
- Database connects
- Main CRUD operations work

**Tools**:
- xUnit/NUnit/MSTest (if tests exist)
- `dotnet test`
- `upgrade_run_tests` tool

## Risk Management

### Risk Assessment Matrix

| Risk | Likelihood | Impact | Severity | Mitigation |
|------|-----------|--------|----------|------------|
| SDK conversion corrupts project file | Low | High | **Medium** | Git branch + automated tool with rollback |
| Package conflicts prevent build | Medium | High | **High** | Incremental package updates + validation |
| Breaking API changes break functionality | High | High | **Critical** | Detailed assessment + phased migration |
| EF Core migration breaks data access | Medium | High | **High** | Test database queries incrementally |
| Configuration migration loses settings | Low | Medium | **Low** | Manual verification of all settings |
| Performance regression | Low | Medium | **Low** | Baseline measurements + comparison |

---

### Risk Mitigation Strategies

#### Critical Risk: Breaking API Changes
**Risk**: 622 API incompatibilities could break application functionality

**Mitigation**:
1. **Incremental Fixing**: Address issues file-by-file
2. **Compile-Time Validation**: Build after each major change
3. **Fallback Plan**: Keep original code for reference
4. **Documentation**: Track all manual changes
5. **Testing**: Validate functionality after each controller migration

**If This Fails**:
- Revert to previous phase commit
- Address specific failures one at a time
- Consider hybrid approach (fix critical paths first)

---

#### High Risk: Package Conflicts
**Risk**: Incompatible package versions prevent build

**Mitigation**:
1. **Remove Framework-Included First**: Eliminate duplicate references
2. **Update One Category at a Time**: EF, then ASP.NET, then utilities
3. **Check Compatibility**: Use `upgrade_get_supported_package_version_for_project`
4. **Resolve Conflicts**: Use `dotnet list package --vulnerable` for security issues

**If This Fails**:
- Revert package changes
- Update packages individually
- Check for transitive dependency conflicts
- Temporarily remove non-essential packages

---

#### High Risk: EF Core Migration Issues
**Risk**: Entity Framework behavioral changes break queries

**Mitigation**:
1. **Connection String First**: Verify database connectivity before queries
2. **Simple Query Test**: Run basic `DbSet.ToList()` first
3. **Incremental Complexity**: Test includes, filters, then complex queries
4. **Migration Scripts**: Review/test any database migrations
5. **Logging**: Enable EF Core SQL logging for troubleshooting

**If This Fails**:
- Check connection string format (may differ from EF6)
- Verify SQL Server compatibility
- Review EF Core breaking changes documentation
- Consider temporary EF6 compatibility mode if available

---

#### Medium Risk: Startup Migration
**Risk**: Program.cs misconfiguration prevents application start

**Mitigation**:
1. **Template Reference**: Use official ASP.NET Core MVC template as guide
2. **Service Registration**: Ensure all required services registered
3. **Middleware Order**: Follow correct pipeline order
4. **Logging**: Enable detailed startup logging
5. **Validation**: Test application start before adding complexity

**If This Fails**:
- Review startup exception details
- Compare with working ASP.NET Core application
- Verify DI registrations
- Check middleware pipeline order

---

#### Medium Risk: Configuration Loss
**Risk**: Settings not migrated from Web.config to appsettings.json

**Mitigation**:
1. **Audit First**: List all Web.config appSettings before migration
2. **Side-by-Side Comparison**: Keep Web.config for reference
3. **Runtime Verification**: Check that all settings accessible
4. **Environment Variables**: Consider for sensitive settings

**If This Fails**:
- Restore from Web.config backup
- Use temporary Web.config alongside appsettings.json
- Implement custom configuration provider if needed

---

### Rollback Procedures

#### Phase-Level Rollback
If a phase fails completely:

```bash
# View recent commits
git log --oneline

# Rollback to previous phase
git reset --hard <commit-hash-before-phase>

# Alternative: Create fix commit
git revert <problematic-commit>
```

#### Complete Rollback
If migration must be abandoned:

```bash
# Return to main branch
git checkout main

# Delete upgrade branch
git branch -D upgrade-to-NET10-1
```

---

### Contingency Plans

#### Plan A: Full Migration (Preferred)
Complete all phases as outlined in this plan.

**Time Estimate**: 13-20 hours  
**Success Criteria**: Application runs on .NET 10.0

#### Plan B: Incremental Migration
If full migration too risky:
1. Complete Phase 1-2 (SDK + packages)
2. Deploy as .NET Framework → .NET Standard intermediate
3. Complete Phase 3-4 in subsequent iteration

**Time Estimate**: 8-10 hours + follow-up  
**Success Criteria**: Reduced blast radius

#### Plan C: Parallel Implementation
If migration too complex:
1. Create new ASP.NET Core 10.0 project
2. Port features incrementally
3. Run both versions in parallel during transition

**Time Estimate**: 20-30 hours  
**Success Criteria**: Clean architecture, no legacy baggage

---

### Emergency Contacts & Resources

**Documentation**:
- [ASP.NET Core Migration Guide](https://learn.microsoft.com/aspnet/core/migration/)
- [EF Core Migration from EF6](https://learn.microsoft.com/ef/efcore-and-ef6/)
- [.NET 10.0 Breaking Changes](https://learn.microsoft.com/dotnet/core/compatibility/)

**Support Channels**:
- GitHub Issues for specific packages
- Stack Overflow for common patterns
- Microsoft Q&A for official guidance

## Complexity & Effort Assessment

### Overall Complexity: **HIGH**

This is a **major platform migration**, not a simple version upgrade. The application must transition from the .NET Framework ecosystem to the modern .NET platform, which represents fundamental architectural changes.

---

### Complexity Factors

#### 1. Project System Change
**Complexity**: Low-Medium  
**Automation**: High (tooling available)  
**Effort**: 0.5 hours

SDK-style conversion is largely automated and low-risk.

#### 2. Package Ecosystem Migration
**Complexity**: Medium-High  
**Automation**: Medium (some manual replacements needed)  
**Effort**: 1-2 hours

- 19 packages removed (framework-included)
- 7 packages replaced (incompatible APIs)
- 19 packages updated (version bumps)

#### 3. Code Refactoring
**Complexity**: High  
**Automation**: Low (mostly manual)  
**Effort**: 6-8 hours

- **622 breaking API changes** across 12.8% of codebase
- Multiple namespace replacements
- Pattern migrations (filters → middleware)
- EF6 → EF Core refactoring

#### 4. Application Architecture
**Complexity**: High  
**Automation**: None (manual restructuring)  
**Effort**: 2-3 hours

Complete paradigm shift:
- Global.asax → Program.cs
- RouteConfig → endpoint routing
- BundleConfig → static files
- Web.config → appsettings.json

#### 5. Testing & Validation
**Complexity**: Medium  
**Automation**: Medium (if tests exist)  
**Effort**: 4-6 hours

Comprehensive validation required across all layers.

---

### Effort Breakdown

| Phase | Task | Hours | Confidence |
|-------|------|-------|------------|
| **1** | SDK-style conversion | 0.5 | High |
| **2** | Package updates | 1-2 | Medium-High |
| **3** | Breaking API fixes | 4-5 | Medium |
| **3** | EF Core migration | 2-3 | Medium |
| **4** | Startup migration | 2-3 | Medium |
| **4** | Bundling replacement | 0.5-1 | High |
| **5** | Testing & validation | 4-6 | Medium-Low |
| | **Total** | **14-20** | **Medium** |

**Recommended Schedule**:
- **Session 1** (4 hours): Phases 1-2
- **Session 2** (6 hours): Phase 3 (partial)
- **Session 3** (4 hours): Phase 3 (complete) + Phase 4
- **Session 4** (4-6 hours): Phase 5 + refinement

---

### Code Impact Analysis

#### Files Requiring Changes

**High-Impact Files** (major refactoring):
- `Global.asax.cs` → Complete rewrite as `Program.cs`
- `App_Start/RouteConfig.cs` → Migrate to endpoint routing
- `App_Start/FilterConfig.cs` → Migrate to middleware
- `App_Start/BundleConfig.cs` → Remove or replace
- `Web.config` → Migrate to `appsettings.json`
- All Entity Framework DbContext files → Constructor/DI changes

**Medium-Impact Files** (namespace/API changes):
- All Controllers (16 estimated files)
  - Change using statements
  - Update `ActionResult` → `IActionResult`
  - Update filter attributes

- All Views (30+ estimated files)
  - Remove `@Scripts.Render()` / `@Styles.Render()`
  - Update helper methods if needed

**Low-Impact Files** (minimal changes):
- Model classes (likely no changes if POCOs)
- ViewModels (likely no changes)
- DTOs (likely no changes)

#### Estimated Line Changes
Based on assessment:
- **Total Lines**: 4,423
- **Lines Requiring Changes**: 565+
- **Percentage**: 12.8%

**Distribution**:
- Automated changes: ~30% (SDK, some package refs)
- Semi-automated changes: ~20% (namespace replacements)
- Manual changes: ~50% (logic, patterns, architecture)

---

### Skill Requirements

#### Required Skills
1. **ASP.NET Core MVC** (intermediate-advanced)
   - Middleware pipeline
   - Dependency injection
   - Endpoint routing

2. **Entity Framework Core** (intermediate)
   - DbContext configuration
   - Service registration
   - Query patterns

3. **C# Modern Features** (intermediate)
   - Nullable reference types
   - Implicit usings
   - Top-level statements (Program.cs)

#### Recommended Skills
1. **Git** (basic-intermediate) - For rollback/branching
2. **.NET CLI** (basic) - For build/run commands
3. **JSON Configuration** (basic) - For appsettings.json
4. **Web Development** (basic) - HTML/CSS/JS for bundling changes

---

### Comparison to Other Migration Types

| Migration Type | Complexity | Typical Effort |
|----------------|------------|----------------|
| .NET 6 → .NET 8 | **Low** | 1-2 hours |
| .NET Core 3.1 → .NET 10 | **Low-Medium** | 2-4 hours |
| .NET Framework 4.8 → .NET 10 (ASP.NET Core) | **High** | 14-20 hours ✓ |
| .NET Framework 4.8 → .NET 10 (Console) | **Medium** | 4-8 hours |

This migration is on the **higher end of complexity** due to the ASP.NET MVC → ASP.NET Core transition.

---

### Success Factors

**Factors That Increase Success Probability**:
✅ Single project (no dependencies)  
✅ Automated tooling for SDK conversion  
✅ Clear assessment with 622 identified issues  
✅ Detailed migration plan  
✅ Git branching for safety  

**Factors That Increase Risk**:
⚠️ Large number of breaking changes (622)  
⚠️ EF6 → EF Core migration  
⚠️ Architectural paradigm shift  
⚠️ No existing test suite mentioned  
⚠️ Potential custom code patterns  

**Overall Success Probability**: **75-85%** with careful execution

---

### Optimization Opportunities

#### Short-Term (During Migration)
- Use code search/replace for repetitive namespace changes
- Batch similar file updates together
- Validate incrementally to catch issues early

#### Long-Term (Post-Migration)
- Adopt nullable reference types throughout
- Implement proper logging (ILogger)
- Add health checks
- Add integration tests
- Consider API versioning
- Implement caching strategies
- Optimize startup time

## Source Control Strategy

### Branch Structure

```
main (protected)
  └─ upgrade-to-NET10-1 (working branch) ← Current
```

**Working Branch**: `upgrade-to-NET10-1`  
**Source Branch**: `main`  
**Merge Target**: `main` (after successful validation)

---

### Commit Strategy

Use atomic commits for each major change to enable selective rollback.

#### Recommended Commit Sequence

**Phase 1: Foundation**
```
✓ Already created branch 'upgrade-to-NET10-1'

Next commits:
1. "Convert ContosoUniversity to SDK-style project"
2. "Update target framework to net10.0"
```

**Phase 2: Package Modernization**
```
3. "Remove framework-included packages"
4. "Remove incompatible bundling packages"
5. "Replace Entity Framework 6 with EF Core"
6. "Update packages for security fixes"
7. "Add ASP.NET Core framework reference"
```

**Phase 3: Code Migration**
```
8. "Replace System.Web.Mvc with Microsoft.AspNetCore.Mvc namespaces"
9. "Update controller action return types to IActionResult"
10. "Migrate Entity Framework DbContext to EF Core pattern"
11. "Update EF query syntax for EF Core compatibility"
12. "Remove incompatible filter attributes"
```

**Phase 4: Application Initialization**
```
13. "Create Program.cs with ASP.NET Core startup"
14. "Migrate route configuration to endpoint routing"
15. "Replace Global.asax with middleware pipeline"
16. "Migrate Web.config settings to appsettings.json"
17. "Replace bundling with direct HTML references"
18. "Update layout views for static file references"
```

**Phase 5: Cleanup & Validation**
```
19. "Remove obsolete files (Global.asax, App_Start folder)"
20. "Final build validation and error fixes"
21. "Update README with .NET 10.0 instructions"
```

---

### Commit Message Template

Use conventional commit format for clarity:

```
<type>(<scope>): <subject>

<body>

<footer>
```

**Examples**:

```
feat(project): convert to SDK-style targeting net10.0

- Automated conversion using upgrade tooling
- Updated target framework to net10.0
- Removed explicit file listings

Refs: #123
```

```
refactor(controllers): migrate to ASP.NET Core MVC

- Changed using statements from System.Web.Mvc to Microsoft.AspNetCore.Mvc
- Updated ActionResult to IActionResult
- Removed HandleError attributes (now handled by middleware)

Breaking: Controllers now require ASP.NET Core runtime

Refs: #123
```

---

### Merge Strategy

#### Pre-Merge Checklist
Before merging `upgrade-to-NET10-1` → `main`:

- [ ] All phases complete
- [ ] Application builds successfully
- [ ] Application runs and serves requests
- [ ] All functional tests pass
- [ ] Performance acceptable
- [ ] No security vulnerabilities
- [ ] Documentation updated
- [ ] Team review completed (if applicable)

#### Merge Process

**Option A: Squash Merge (Recommended for Clean History)**
```bash
git checkout main
git merge --squash upgrade-to-NET10-1
git commit -m "Migrate ContosoUniversity to .NET 10.0

Complete migration from .NET Framework 4.8 to .NET 10.0
- Convert to SDK-style project
- Migrate ASP.NET MVC 5 to ASP.NET Core MVC
- Replace Entity Framework 6 with EF Core 9
- Update all packages for compatibility and security

Tested: Full application functionality verified
"
git push origin main
```

**Option B: Regular Merge (Preserve Detailed History)**
```bash
git checkout main
git merge upgrade-to-NET10-1
git push origin main
```

**Option C: Pull Request** (if using GitHub/Azure DevOps)
- Create PR from `upgrade-to-NET10-1` to `main`
- Request review
- Address feedback
- Merge when approved

---

### Rollback Procedures

#### Scenario 1: Rollback Single Commit
If a specific commit introduces issues:

```bash
# View commit history
git log --oneline

# Revert specific commit (creates new commit)
git revert <commit-hash>
```

#### Scenario 2: Rollback Entire Phase
If a phase fails and needs complete redo:

```bash
# Find commit before phase started
git log --oneline

# Reset to that commit (destructive)
git reset --hard <commit-before-phase>

# Or create revert commits (non-destructive)
git revert <commit1> <commit2> <commit3>
```

#### Scenario 3: Abandon Migration
If migration must be abandoned completely:

```bash
# Switch back to main
git checkout main

# Delete working branch (local)
git branch -D upgrade-to-NET10-1

# Delete working branch (remote, if pushed)
git push origin --delete upgrade-to-NET10-1
```

---

### Backup Strategy

#### Before Starting Migration
```bash
# Tag current state
git tag -a pre-net10-migration -m "State before .NET 10.0 migration"
git push origin pre-net10-migration
```

#### During Migration (Optional Checkpoints)
```bash
# After each successful phase
git tag -a net10-phase1-complete -m "SDK conversion complete"
git tag -a net10-phase2-complete -m "Package updates complete"
# ... etc
```

#### Restore from Tag
```bash
# List tags
git tag -l

# Create new branch from tag
git checkout -b restore-from-tag pre-net10-migration
```

---

## Success Criteria

### Build Success Criteria

#### Level 1: Compilation ✅
- [ ] `dotnet build` completes with exit code 0
- [ ] Zero compilation errors
- [ ] Zero blocking warnings
- [ ] All package references resolve
- [ ] Target framework is `net10.0`

#### Level 2: Project Structure ✅
- [ ] Project file is SDK-style format
- [ ] `<FrameworkReference Include="Microsoft.AspNetCore.App" />` present
- [ ] No legacy `<Import>` statements
- [ ] No explicit file listings (uses implicit globbing)
- [ ] `launchSettings.json` configured

---

### Runtime Success Criteria

#### Level 3: Application Startup ✅
- [ ] Application starts without exceptions
- [ ] Kestrel listens on expected ports (5000/5001 or configured)
- [ ] Dependency injection container initializes
- [ ] DbContext registered and available
- [ ] Middleware pipeline configured correctly
- [ ] Startup logs show no errors or warnings

#### Level 4: HTTP Functionality ✅
- [ ] HTTP GET to `/` returns 200 OK
- [ ] Home page HTML renders
- [ ] Static files served (CSS, JS, images)
- [ ] Routing works for all controller actions
- [ ] 404 errors handled gracefully
- [ ] 500 errors handled gracefully

---

### Functional Success Criteria

#### Level 5: Database Operations ✅
- [ ] Database connection succeeds
- [ ] EF Core queries execute without errors
- [ ] CRUD operations work (Create, Read, Update, Delete)
- [ ] Navigation properties load correctly
- [ ] Transactions work correctly
- [ ] Database migrations compatible (if using)

#### Level 6: User Interface ✅
- [ ] All views render without Razor errors
- [ ] Forms submit successfully
- [ ] Model binding works (form data → action parameters)
- [ ] Model validation functions correctly
- [ ] Validation messages display
- [ ] Client-side validation works (if implemented)

#### Level 7: Business Logic ✅
- [ ] All controller actions execute
- [ ] Service classes function correctly
- [ ] Authentication works (if implemented)
- [ ] Authorization works (if implemented)
- [ ] Custom middleware functions (if any)
- [ ] Background services work (if any)

---

### Quality Success Criteria

#### Level 8: Testing ✅
- [ ] All existing unit tests pass (if present)
- [ ] All integration tests pass (if present)
- [ ] Manual smoke testing complete
- [ ] Edge cases validated
- [ ] Error scenarios tested

#### Level 9: Performance ✅
- [ ] Application startup time acceptable (< 5 seconds)
- [ ] Page load times comparable to original
- [ ] Database query performance maintained
- [ ] Memory usage reasonable
- [ ] No obvious performance regressions

#### Level 10: Security ✅
- [ ] No vulnerable packages (`dotnet list package --vulnerable`)
- [ ] HTTPS enforced
- [ ] Security headers configured
- [ ] No exposed secrets in configuration
- [ ] Authentication/authorization preserved

---

### Documentation Success Criteria

#### Level 11: Documentation ✅
- [ ] README updated with .NET 10.0 requirements
- [ ] Setup instructions updated
- [ ] Dependencies documented
- [ ] Known issues documented (if any)
- [ ] Migration notes added for future reference

---

### Final Go/No-Go Decision

**READY FOR PRODUCTION** when ALL of the following are true:

✅ **Build**: Levels 1-2 complete  
✅ **Runtime**: Levels 3-4 complete  
✅ **Functionality**: Levels 5-7 complete  
✅ **Quality**: Levels 8-10 complete  
✅ **Documentation**: Level 11 complete  
✅ **Stakeholder Approval**: Team/manager signoff obtained  

**NOT READY** if ANY of the following exist:

❌ Compilation errors  
❌ Runtime exceptions on startup  
❌ Critical functionality broken  
❌ Security vulnerabilities present  
❌ Significant performance regression  
❌ Data loss or corruption risk  

---

### Post-Deployment Monitoring

After merging to `main` and deploying to production:

**Week 1: Intensive Monitoring**
- Monitor application logs for errors
- Track performance metrics
- Collect user feedback
- Watch for unexpected behavior

**Week 2-4: Normal Monitoring**
- Continue log monitoring
- Review performance trends
- Address any issues promptly

**Success Indicators**:
- Zero production incidents
- Comparable or better performance
- Positive user feedback
- No rollback required

---

## Plan Complete

This migration plan provides a comprehensive roadmap for upgrading ContosoUniversity from .NET Framework 4.8 to .NET 10.0. Follow the phases sequentially, validate at each step, and use the success criteria to ensure a smooth transition.

**Next Steps**: Proceed to Execution phase to begin implementation.
