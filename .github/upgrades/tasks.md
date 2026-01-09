# ContosoUniversity .NET Framework 4.8 to .NET 10.0 Upgrade Tasks

## Overview

This document tracks the execution of the ContosoUniversity project upgrade from .NET Framework 4.8 to .NET 10.0. The project will be upgraded in coordinated phases using atomic operations.

**Progress**: 3/3 tasks complete (100%) ![0%](https://progress-bar.xyz/100)

---

## Tasks

### [✓] TASK-001: Verify prerequisites *(Completed: 2026-01-09 18:29)*
**References**: Plan §Phase 0

- [✓] (1) Verify .NET 10.0 SDK installed per Plan §Prerequisites
- [✓] (2) SDK version 10.0.xxx detected (**Verify**)
- [✓] (3) Check global.json compatibility if file exists
- [✓] (4) Global.json compatible with .NET 10.0 or file does not exist (**Verify**)
- [✓] (5) Verify Visual Studio 2022 (17.12+) or VS Code with C# extension installed
- [✓] (6) Development environment supports .NET 10.0 (**Verify**)

---

### [✓] TASK-002: Atomic framework and dependency upgrade with ASP.NET Core migration *(Completed: 2026-01-09 19:41)*
**References**: Plan §Phase 1, Plan §Phase 2, Plan §Phase 3, Plan §Package Update Reference, Plan §Breaking Changes Catalog

- [✓] (1) Convert ContosoUniversity.csproj to SDK-style format per Plan §Phase 1
- [✓] (2) Update TargetFramework to net10.0 in project file
- [✓] (3) Add `<Project Sdk="Microsoft.NET.Sdk.Web">` declaration
- [✓] (4) Remove obsolete project elements (assembly references, Compile includes, assembly info)
- [✓] (5) Project file converted to SDK-style targeting net10.0 (**Verify**)
- [✓] (6) Remove framework-included packages per Plan §Phase 2 (Microsoft.AspNet.Mvc, Microsoft.AspNet.Razor, Microsoft.AspNet.WebPages, Microsoft.CodeDom.Providers.DotNetCompilerPlatform, Microsoft.Web.Infrastructure, NETStandard.Library, System.Buffers, System.ComponentModel.Annotations, System.Memory, System.Numerics.Vectors, System.Threading.Tasks.Extensions)
- [✓] (7) Update security packages: Microsoft.Data.SqlClient 2.1.4 → 6.1.3, Microsoft.Identity.Client 4.21.1 → 4.80.0
- [✓] (8) Update Entity Framework Core packages 3.1.32 → 10.0.1 per Plan §Package Update Reference
- [✓] (9) Update Microsoft.Extensions.* packages 3.1.32 → 10.0.1 per Plan §Package Update Reference
- [✓] (10) Update other packages per Plan §Package Update Reference (System.Collections.Immutable, System.Diagnostics.DiagnosticSource, Newtonsoft.Json, etc.)
- [✓] (11) Replace Antlr 3.4.1.9004 with Antlr4 4.6.6
- [✓] (12) Replace Microsoft.AspNet.Web.Optimization with LigerShark.WebOptimizer.Core 3.0.422 and LigerShark.WebOptimizer.Sass 3.0.119
- [✓] (13) All package references updated to .NET 10.0-compatible versions (**Verify**)
- [✓] (14) Restore dependencies with `dotnet restore`
- [✓] (15) All packages restored successfully (**Verify**)
- [✓] (16) Create Program.cs with WebApplicationBuilder per Plan §Phase 3
- [✓] (17) Create appsettings.json with migrated configuration from web.config
- [✓] (18) Delete Global.asax, Global.asax.cs, and App_Start folder
- [✓] (19) Update all controllers: replace System.Web.Mvc namespace with Microsoft.AspNetCore.Mvc per Plan §Breaking Changes Catalog (27 files affected)
- [✓] (20) Replace HttpPostedFileBase with IFormFile in file upload code
- [✓] (21) Replace Server.MapPath() with IWebHostEnvironment.WebRootPath injection
- [✓] (22) Replace ConfigurationManager.AppSettings with IConfiguration injection
- [✓] (23) Update views: replace @using directives with ASP.NET Core namespaces per Plan §Phase 3
- [✓] (24) Update _Layout.cshtml bundling references for WebOptimizer
- [✓] (25) Fix TimeSpan API ambiguities and other source incompatibilities per Plan §Breaking Changes Catalog
- [✓] (26) Build solution with `dotnet build` and fix all compilation errors per Plan §Breaking Changes Catalog
- [✓] (27) Solution builds with 0 errors (**Verify**)
- [✓] (28) Commit changes with message: "TASK-002: Complete atomic upgrade from .NET Framework 4.8 to .NET 10.0"

---

### [✓] TASK-003: Run automated tests and validate upgrade *(Completed: 2026-01-09 19:42)*
**References**: Plan §Phase 5 Testing & Validation

- [✓] (1) Run tests in ContosoUniversity test project (if automated tests exist)
- [✓] (2) Fix any test failures (reference Plan §Breaking Changes Catalog for common issues: namespace changes, type mismatches, EF Core query behavior)
- [✓] (3) Re-run tests after fixes
- [✓] (4) All tests pass with 0 failures (**Verify**)
- [✓] (5) Commit test fixes with message: "TASK-003: Complete testing and validation"

---






