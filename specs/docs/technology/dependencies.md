# Dependency Inventory — ContosoUniversity

_Extracted on 2026-05-13. This is a factual catalog of all declared dependencies._

## Summary

| Metric | Value |
|--------|------:|
| Manifest files found | 2 |
| Direct + transitive runtime NuGet packages (flattened in `packages.config`) | 45 |
| Direct dev-only packages declared | 0 |
| BCL `<Reference>` assemblies (no `HintPath`) | 21 |
| Lock file present | N/A — `packages.config` (legacy NuGet) is itself the flattened, resolved package list (no separate `packages.lock.json`) |
| Ecosystems | NuGet (.NET Framework) |

## Manifest Files

| Path | Format | Role |
|------|--------|------|
| `src/ContosoUniversity/packages.config` | Legacy NuGet (XML) | Flattened list of every NuGet package the project requires (direct + transitive together) |
| `src/ContosoUniversity/ContosoUniversity.csproj` | MSBuild XML | Declares per-package `<Reference Include="…"><HintPath>packages\…</HintPath></Reference>` entries plus implicit BCL `<Reference>` assemblies |

> **About `packages.config` and "lock files":** The legacy `packages.config` NuGet workflow does not generate a separate lock file (`packages.lock.json` is opt-in only when migrated to PackageReference). In `packages.config`, the file itself is fully resolved and flattened — direct and transitive packages are co-mingled and pinned to exact versions, with no version ranges. The "Resolved Version" column below therefore equals the declared version for every entry.

## Manifest: `src/ContosoUniversity/packages.config`

### Runtime Dependencies (direct + transitive — flattened)

| # | Package | Declared Version | Resolved Version | Target Framework | Category | Purpose |
|---:|---------|------------------|------------------|------------------|----------|---------|
| 1 | `Antlr` | `3.4.1.9004` | `3.4.1.9004` | `net482` | Build / parsing | Pulled in by `WebGrease`; ANTLR 3 runtime used by WebGrease's CSS parsing pipeline |
| 2 | `bootstrap` | `5.3.3` | `5.3.3` | `net482` | Styling | Bootstrap 5 CSS/JS framework (vendored under `Content/` and `Scripts/`) |
| 3 | `jQuery` | `3.7.1` | `3.7.1` | `net482` | UI library | jQuery 3 client-side JavaScript library |
| 4 | `jQuery.Validation` | `1.21.0` | `1.21.0` | `net482` | Validation | jQuery client-side validation plugin |
| 5 | `Microsoft.AspNet.Mvc` | `5.2.9` | `5.2.9` | `net482` | Web framework | ASP.NET MVC 5 — controllers, routing, action results, model binding |
| 6 | `Microsoft.AspNet.Razor` | `3.2.9` | `3.2.9` | `net482` | View engine | Razor view engine for `.cshtml` templates |
| 7 | `Microsoft.AspNet.Web.Optimization` | `1.1.3` | `1.1.3` | `net482` | Build tooling (runtime) | Bundling and minification used in `App_Start/BundleConfig.cs` |
| 8 | `Microsoft.AspNet.WebPages` | `3.2.9` | `3.2.9` | `net482` | Web framework | Razor / Web Pages support libraries (transitive of MVC) |
| 9 | `Microsoft.Bcl.AsyncInterfaces` | `1.1.1` | `1.1.1` | `net482` | BCL polyfill | Backport of `IAsyncEnumerable<T>` and friends — used by EF Core 3.1 on .NET Framework |
| 10 | `Microsoft.Bcl.HashCode` | `1.1.1` | `1.1.1` | `net482` | BCL polyfill | Backport of `System.HashCode` for .NET Framework |
| 11 | `Microsoft.CodeDom.Providers.DotNetCompilerPlatform` | `2.0.1` | `2.0.1` | `net482` | Build tooling (runtime) | Roslyn-based C# compiler used by the ASP.NET runtime to compile views/code |
| 12 | `Microsoft.Data.SqlClient` | `2.1.4` | `2.1.4` | `net482` | Database client | Modern SQL Server ADO.NET data provider (used as EF Core 3.1's underlying provider) |
| 13 | `Microsoft.Data.SqlClient.SNI.runtime` | `2.1.1` | `2.1.1` | `net482` | Native dependency | Native SNI (TDS) shared libraries for `Microsoft.Data.SqlClient`; copied into `bin/` by the `CopySQLClientNativeBinaries` MSBuild target |
| 14 | `Microsoft.Identity.Client` | `4.21.1` | `4.21.1` | `net482` | Authentication | MSAL.NET — Microsoft Authentication Library for OAuth2 / Microsoft Entra ID |
| 15 | `Microsoft.EntityFrameworkCore` | `3.1.32` | `3.1.32` | `net482` | Database / ORM | EF Core 3.1 LTS runtime |
| 16 | `Microsoft.EntityFrameworkCore.Abstractions` | `3.1.32` | `3.1.32` | `net482` | Database / ORM | EF Core abstractions (interfaces, attributes) — transitive of `Microsoft.EntityFrameworkCore` |
| 17 | `Microsoft.EntityFrameworkCore.Analyzers` | `3.1.32` | `3.1.32` | `net482` | Build tooling | Roslyn analyzers for EF Core (compile-time only) |
| 18 | `Microsoft.EntityFrameworkCore.Relational` | `3.1.32` | `3.1.32` | `net482` | Database / ORM | EF Core relational-database support (SQL building, migrations) |
| 19 | `Microsoft.EntityFrameworkCore.SqlServer` | `3.1.32` | `3.1.32` | `net482` | Database / ORM | EF Core provider for SQL Server (uses `Microsoft.Data.SqlClient`) |
| 20 | `Microsoft.EntityFrameworkCore.Tools` | `3.1.32` | `3.1.32` | `net482` | Build tooling | EF Core Package Manager Console tools (`Add-Migration`, `Update-Database`) |
| 21 | `Microsoft.Extensions.Caching.Abstractions` | `3.1.32` | `3.1.32` | `net482` | Caching | EF Core 3.1 dependency — caching abstractions |
| 22 | `Microsoft.Extensions.Caching.Memory` | `3.1.32` | `3.1.32` | `net482` | Caching | EF Core 3.1 dependency — in-memory cache implementation |
| 23 | `Microsoft.Extensions.Configuration` | `3.1.32` | `3.1.32` | `net482` | Configuration | EF Core / `Microsoft.Extensions.*` configuration primitives |
| 24 | `Microsoft.Extensions.Configuration.Abstractions` | `3.1.32` | `3.1.32` | `net482` | Configuration | Configuration abstractions |
| 25 | `Microsoft.Extensions.Configuration.Binder` | `3.1.32` | `3.1.32` | `net482` | Configuration | Configuration → POCO binding |
| 26 | `Microsoft.Extensions.DependencyInjection` | `3.1.32` | `3.1.32` | `net482` | Dependency injection | Microsoft DI container (used internally by EF Core; not wired into MVC's request pipeline by this project) |
| 27 | `Microsoft.Extensions.DependencyInjection.Abstractions` | `3.1.32` | `3.1.32` | `net482` | Dependency injection | DI abstractions (`IServiceCollection`, `IServiceProvider`) |
| 28 | `Microsoft.Extensions.Logging` | `3.1.32` | `3.1.32` | `net482` | Logging | `ILogger`, `ILoggerFactory` runtime |
| 29 | `Microsoft.Extensions.Logging.Abstractions` | `3.1.32` | `3.1.32` | `net482` | Logging | Logging abstractions |
| 30 | `Microsoft.Extensions.Options` | `3.1.32` | `3.1.32` | `net482` | Configuration | `IOptions<T>` pattern primitives |
| 31 | `Microsoft.Extensions.Primitives` | `3.1.32` | `3.1.32` | `net482` | Utility | Shared primitive types (`ChangeToken`, `StringValues`, …) |
| 32 | `Microsoft.jQuery.Unobtrusive.Validation` | `4.0.0` | `4.0.0` | `net482` | Validation | ASP.NET MVC unobtrusive client-side validation glue |
| 33 | `Microsoft.Web.Infrastructure` | `2.0.1` | `2.0.1` | `net482` | Web framework | Pre-application start hooks (`PreApplicationStartMethod`) used by ASP.NET MVC |
| 34 | `Modernizr` | `2.6.2` | `2.6.2` | `net482` | UI library | HTML5 / CSS3 feature-detection JavaScript library (vendored under `Scripts/`) |
| 35 | `NETStandard.Library` | `2.0.3` | `2.0.3` | `net482` | BCL polyfill | netstandard 2.0 façade assemblies enabling EF Core 3.1 (`netstandard2.0`) on .NET Framework |
| 36 | `Newtonsoft.Json` | `13.0.3` | `13.0.3` | `net482` | Utility | JSON serialization (Json.NET) |
| 37 | `System.Buffers` | `4.5.1` | `4.5.1` | `net482` | BCL polyfill | `System.Buffers` for .NET Framework (transitive of EF Core / SqlClient) |
| 38 | `System.Collections.Immutable` | `1.7.1` | `1.7.1` | `net482` | Utility | Immutable collections (transitive of EF Core / Roslyn) |
| 39 | `System.ComponentModel.Annotations` | `4.7.0` | `4.7.0` | `net482` | Validation | Data-annotations attributes (`[Required]`, `[StringLength]`, …) used on model classes |
| 40 | `System.Diagnostics.DiagnosticSource` | `4.7.1` | `4.7.1` | `net482` | Telemetry / observability | `DiagnosticSource` / `Activity` instrumentation pipeline (used by EF Core / SqlClient) |
| 41 | `System.Memory` | `4.5.4` | `4.5.4` | `net482` | BCL polyfill | `Span<T>`, `Memory<T>` for .NET Framework |
| 42 | `System.Numerics.Vectors` | `4.5.0` | `4.5.0` | `net482` | BCL polyfill | SIMD vector types (transitive of `System.Memory`) |
| 43 | `System.Runtime.CompilerServices.Unsafe` | `4.5.3` | `4.5.3` | `net482` | BCL polyfill | Low-level unsafe helpers (transitive of `System.Memory`) |
| 44 | `System.Threading.Tasks.Extensions` | `4.5.4` | `4.5.4` | `net482` | BCL polyfill | `ValueTask<T>` and friends for .NET Framework |
| 45 | `WebGrease` | `1.5.2` | `1.5.2` | `net482` | Build tooling (runtime) | CSS/JS minification engine — transitive of `Microsoft.AspNet.Web.Optimization` |

### Direct vs Transitive Classification

`packages.config` does **not** distinguish direct from transitive packages — they are flattened together. From the csproj `<Reference>` entries (which only list the assemblies the project itself directly references) and the README/code, the **directly used** packages are:

- `Microsoft.AspNet.Mvc` → MVC controllers, routing
- `Microsoft.AspNet.Razor` → Razor views
- `Microsoft.AspNet.Web.Optimization` → bundling
- `Microsoft.AspNet.WebPages` → Razor/Web Pages helpers
- `Microsoft.EntityFrameworkCore` (+ `.SqlServer`, `.Relational`, `.Abstractions`, `.Tools`, `.Analyzers`) → ORM
- `Microsoft.Data.SqlClient` (+ `.SNI.runtime`) → SQL Server data provider
- `Microsoft.Identity.Client` → MSAL authentication
- `Newtonsoft.Json` → JSON
- `Microsoft.CodeDom.Providers.DotNetCompilerPlatform` → Roslyn compiler at runtime
- `bootstrap`, `jQuery`, `jQuery.Validation`, `Microsoft.jQuery.Unobtrusive.Validation`, `Modernizr` → client-side assets
- `System.ComponentModel.Annotations` → data annotations on model classes
- `Microsoft.Web.Infrastructure` → ASP.NET pre-app-start hook

All other packages (the `Microsoft.Extensions.*` family, the `Microsoft.Bcl.*` polyfills, the `System.*` polyfills, `NETStandard.Library`, `WebGrease`, `Antlr`) appear to be **transitive dependencies** flattened in by the direct ones.

### Dev Dependencies

None declared as a separate dev category. `packages.config` does not support `<DevelopmentDependency>` markers. Tooling-only packages (`Microsoft.EntityFrameworkCore.Tools`, `Microsoft.EntityFrameworkCore.Analyzers`) are listed in the runtime block above.

### Peer / Optional Dependencies

Not applicable — NuGet does not expose `peer` or `optional` dependency scopes.

## Manifest: `src/ContosoUniversity/ContosoUniversity.csproj`

The csproj's `<Reference>` items reference assemblies (DLLs), not NuGet packages. Each `<Reference>` either resolves to a NuGet package (via `<HintPath>packages\…\lib\…\X.dll</HintPath>`) or to a Base Class Library (BCL) assembly (no `<HintPath>`).

### NuGet-Backed `<Reference>` Items

These all resolve to packages already listed above. The csproj points at the following DLLs from the `packages/` folder (one assembly per package shown):

```text
packages\bootstrap.5.3.3\…   (CSS/JS — content only; no DLL reference)
packages\jQuery.3.7.1\…       (JS — content only; no DLL reference)
packages\Microsoft.AspNet.Mvc.5.2.9\lib\net45\System.Web.Mvc.dll
packages\Microsoft.AspNet.Razor.3.2.9\lib\net45\System.Web.Razor.dll
packages\Microsoft.AspNet.Web.Optimization.1.1.3\lib\net40\System.Web.Optimization.dll
packages\Microsoft.AspNet.WebPages.3.2.9\lib\net45\…
packages\Microsoft.Bcl.AsyncInterfaces.1.1.1\lib\net461\Microsoft.Bcl.AsyncInterfaces.dll
packages\Microsoft.Bcl.HashCode.1.1.1\lib\net461\Microsoft.Bcl.HashCode.dll
packages\Microsoft.CodeDom.Providers.DotNetCompilerPlatform.2.0.1\lib\net45\
packages\Microsoft.Data.SqlClient.2.1.4\lib\net46\Microsoft.Data.SqlClient.dll
packages\Microsoft.Data.SqlClient.SNI.runtime.2.1.1\runtimes\win-{x86,x64}\native\Microsoft.Data.SqlClient.SNI.dll
packages\Microsoft.EntityFrameworkCore.3.1.32\lib\netstandard2.0\Microsoft.EntityFrameworkCore.dll
packages\Microsoft.EntityFrameworkCore.Abstractions.3.1.32\lib\netstandard2.0\…
packages\Microsoft.EntityFrameworkCore.Relational.3.1.32\lib\netstandard2.0\…
packages\Microsoft.EntityFrameworkCore.SqlServer.3.1.32\lib\netstandard2.0\…
packages\Microsoft.Extensions.{Caching,Configuration,DependencyInjection,Logging,Options,Primitives}*.3.1.32\lib\netstandard2.0\…
packages\Microsoft.Identity.Client.4.21.1\lib\net461\Microsoft.Identity.Client.dll
packages\Microsoft.Web.Infrastructure.2.0.1\lib\net40\Microsoft.Web.Infrastructure.dll
packages\Newtonsoft.Json.13.0.3\lib\net45\Newtonsoft.Json.dll
packages\System.Buffers.4.5.1\lib\net461\…
packages\System.Collections.Immutable.1.7.1\lib\netstandard2.0\…
packages\System.ComponentModel.Annotations.4.7.0\lib\net461\…
packages\System.Diagnostics.DiagnosticSource.4.7.1\lib\net46\…
packages\System.Memory.4.5.4\lib\net461\…
packages\System.Numerics.Vectors.4.5.0\lib\net46\…
packages\System.Runtime.CompilerServices.Unsafe.4.5.3\lib\net461\…
packages\System.Threading.Tasks.Extensions.4.5.4\lib\net461\…
packages\WebGrease.1.5.2\lib\…
packages\Antlr.3.4.1.9004\lib\…
```

### BCL `<Reference>` Items (no NuGet backing)

These come from the .NET Framework reference assemblies installed on the build machine and are not "dependencies" in the package-manager sense, but are required for the project to compile and run:

| Assembly | Why It's Referenced |
|----------|---------------------|
| `System` | BCL core |
| `System.Configuration` | `ConfigurationManager.AppSettings` / `ConnectionStrings` |
| `System.ComponentModel.DataAnnotations` | Data-annotations attributes (alongside the NuGet polyfill) |
| `System.Data` | ADO.NET (`DataTable`, `DataSet`) |
| `System.Data.DataSetExtensions` | DataSet LINQ extensions |
| `System.Drawing` | Image processing — used in teaching-material image upload flow |
| `System.EnterpriseServices` | COM+ / DTC support — required transitively by some legacy paths |
| `System.Messaging` | **MSMQ** — used by `Infrastructure/MessageQueue.cs`, `Services/NotificationService.cs`, `Controllers/MessageQueueTestController.cs` |
| `System.Net.Http` | `HttpClient` |
| `System.Net.Http.WebRequest` | Legacy `WebRequest` adapter for `HttpClient` |
| `System.Web` | ASP.NET pipeline, `HttpContext`, `HttpPostedFileBase` |
| `System.Web.Abstractions` | ASP.NET abstractions used by MVC |
| `System.Web.ApplicationServices` | Membership / role / profile providers (legacy) |
| `System.Web.DynamicData` | Dynamic Data scaffolding (legacy MVC dependency) |
| `System.Web.Entity` | EF integration with ASP.NET DynamicData |
| `System.Web.Extensions` | AJAX, JSON, `ScriptManager` |
| `System.Web.Routing` | ASP.NET routing |
| `System.Web.Services` | ASMX web services (BCL) |
| `System.Xml` | XML APIs |
| `System.Xml.Linq` | LINQ-to-XML |
| `Microsoft.CSharp` | `dynamic` keyword runtime support |
| `netstandard 2.0.0.0` | netstandard façade (paired with the `NETStandard.Library` NuGet) |

Total: 21 BCL `<Reference>` entries (some implicit for an MVC project, some hand-added — notably `System.Messaging` for MSMQ).

## Dependency Tree Summary

### Deepest Chains (qualitative — `packages.config` flattens everything)

The legacy `packages.config` format collapses the dependency tree into a single flat list, so exact tree depth cannot be reconstructed without a separate lock file. The shape can still be inferred from package-vendor metadata:

- `Microsoft.AspNet.Mvc 5.2.9` → `Microsoft.AspNet.WebPages 3.2.9` → `Microsoft.AspNet.Razor 3.2.9` → `Microsoft.Web.Infrastructure 2.0.1` (depth: 4)
- `Microsoft.EntityFrameworkCore 3.1.32` → `Microsoft.EntityFrameworkCore.Abstractions 3.1.32` → `Microsoft.Bcl.AsyncInterfaces 1.1.1` → `System.Threading.Tasks.Extensions 4.5.4` → `System.Runtime.CompilerServices.Unsafe 4.5.3` (depth: 5)
- `Microsoft.EntityFrameworkCore.SqlServer 3.1.32` → `Microsoft.EntityFrameworkCore.Relational 3.1.32` → `Microsoft.EntityFrameworkCore 3.1.32` → … (depth: 5+)
- `Microsoft.Data.SqlClient 2.1.4` → `Microsoft.Data.SqlClient.SNI.runtime 2.1.1` (native) (depth: 2)
- `Microsoft.Data.SqlClient 2.1.4` → `Microsoft.Identity.Client 4.21.1` (depth: 2; MSAL is a token-acquisition optional path for SQL auth)
- `Microsoft.AspNet.Web.Optimization 1.1.3` → `WebGrease 1.5.2` → `Antlr 3.4.1.9004` (depth: 3)

### Shared Transitive Dependencies

| Package | Pulled in by (observed) |
|---------|-------------------------|
| `NETStandard.Library 2.0.3` | All `Microsoft.EntityFrameworkCore.*` and `Microsoft.Extensions.*` (because they target `netstandard2.0`) |
| `System.Memory 4.5.4` | `Microsoft.Data.SqlClient`, `Microsoft.EntityFrameworkCore`, multiple `Microsoft.Extensions.*` |
| `System.Buffers 4.5.1` | `Microsoft.Data.SqlClient`, EF Core 3.1 stack |
| `System.Diagnostics.DiagnosticSource 4.7.1` | `Microsoft.Data.SqlClient`, `Microsoft.EntityFrameworkCore` |
| `System.Runtime.CompilerServices.Unsafe 4.5.3` | `System.Memory`, `Microsoft.Bcl.AsyncInterfaces`, several `Microsoft.Extensions.*` |
| `Microsoft.Bcl.AsyncInterfaces 1.1.1` | `Microsoft.EntityFrameworkCore`, `Microsoft.Extensions.Caching.Abstractions` |
| `Microsoft.Bcl.HashCode 1.1.1` | `Microsoft.EntityFrameworkCore` |

### Multiple Version Instances

None observed. `packages.config` enforces a single resolved version per package id, and no entry appears more than once in the file.

## Version Constraint Patterns

- **All versions are exactly pinned.** `packages.config` only stores exact versions (no `^`, `~`, or range syntax exists in the format).
- **All `targetFramework` attributes are `net482`** (uniform .NET Framework 4.8.2 target).
- **No separate lock file** (`packages.lock.json`) is present — legacy `packages.config` does not produce one. The lock file is implicit because `packages.config` already lists exact resolved versions.
- **No version-pinning side-files**: no `.nvmrc`, `.python-version`, `.tool-versions`, `global.json`, or `nuget.config` is present in `src/ContosoUniversity/`.
- **The repository commits `packages.config`** to source control (the file is in the project tree).
- **The repository also commits the `packages/` directory** (NuGet restored binaries, including the `bin/` reference DLLs) — the `bin/` listing in the workspace tree shows `Microsoft.Data.SqlClient.xml`, `Microsoft.EntityFrameworkCore.xml`, etc., evidencing a full restored package cache is checked in.
- **No lock file refresh metadata** (timestamps, hashes) exists since `packages.config` carries none.
