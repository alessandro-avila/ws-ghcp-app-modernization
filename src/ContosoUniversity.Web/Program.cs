using Azure.Identity;
using ContosoUniversity.Web.Data;
using ContosoUniversity.Web.Identity;
using ContosoUniversity.Web.Middleware;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// rw-001e (SEC-MEDIUM-004 + ADR-007): in production, app secrets (DB connection
// strings, Application Insights connection string, Identity provider client
// secret) are sourced from Azure Key Vault using the host's managed identity.
// In dev, this branch is skipped and `dotnet user-secrets` provides equivalent
// out-of-source-tree storage. The KEYVAULT_URI environment variable / config
// key is the gate so the dev test suite never reaches Azure.
var keyVaultUri = builder.Configuration["KEYVAULT_URI"];
if (!string.IsNullOrWhiteSpace(keyVaultUri))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUri),
        new DefaultAzureCredential());
}

// rw-001d (SEC-MEDIUM-005): replace the default text console formatter with the
// JSON formatter so every log line is emitted as a single-line JSON document
// suitable for ingestion by Application Insights / Log Analytics / Loki. The
// AddJsonConsole call removes any console formatter added by CreateBuilder so
// duplicate ConsoleLoggerProvider registrations do not double-emit log lines.
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.JsonWriterOptions = new System.Text.Json.JsonWriterOptions
    {
        Indented = false
    };
});

// Add services to the container.
builder.Services.AddControllersWithViews();

// EF Core 8: bind SchoolContext to the same SQL Server (LocalDB) database the legacy app uses,
// so the rewrite reads/writes identical schema during co-existence (rw-001a AC #4).
builder.Services.AddDbContext<SchoolContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SchoolContext")));

// rw-001b: ASP.NET Core Identity dev-stub. AspNet* tables live alongside the legacy domain
// tables in the same SchoolContext (extended to IdentityDbContext<ApplicationUser>). The
// dev-stub credential check is replaced entirely by Microsoft Entra ID OIDC in rw-001c.
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // Tighten password rules slightly above the framework defaults; the seeded users
        // (admin@contoso.test / reader@contoso.test) satisfy these.
        options.Password.RequiredLength = 10;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireUppercase = true;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<SchoolContext>()
    .AddDefaultTokenProviders();

// Cookie hardening (SEC-MEDIUM-003 + rw-001b AC #8). __Host- prefix mandates Secure=true,
// Path=/, and no Domain attribute, so a malicious sibling subdomain cannot overwrite the
// cookie. SameSite=Strict eliminates cross-site request inclusion. SlidingExpiration lets
// active sessions stay alive without forcing a re-auth at the 60-minute mark.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "__Host-ContosoUniversity.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.LoginPath = "/Account/SignIn";
    options.LogoutPath = "/Account/SignOut";
    options.AccessDeniedPath = "/Account/SignIn";
});

// Default-deny: every endpoint requires an authenticated user unless explicitly
// marked [AllowAnonymous] (Home/Index, Health/Index, Account/SignIn, Account/SignOut).
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// rw-001d (SEC-MEDIUM-006 + ADR-006): every inbound request gets a hard
// 30-second deadline. Long-running endpoints can opt in to a custom policy via
// [RequestTimeout] later; everything else falls back to this default and is
// safely cancelled if the call exceeds the budget.
builder.Services.AddRequestTimeouts(options =>
{
    options.DefaultPolicy = new RequestTimeoutPolicy
    {
        Timeout = TimeSpan.FromSeconds(30),
        TimeoutStatusCode = StatusCodes.Status504GatewayTimeout
    };
});

// rw-001e (SEC-MEDIUM-004 + ADR-007): explicit ApplicationDiscriminator pins the
// data-protection key isolation boundary to a stable identifier instead of the
// (potentially renamed) host assembly. Persistence is environment-aware:
//   * Dev: file-system under %LOCALAPPDATA%/ContosoUniversity/keys (per-developer key ring).
//   * Prod: Azure Blob storage holds the key ring, optionally protected at rest by an
//          Azure Key Vault key. Both Azure paths are gated on environment variables so the
//          dev test suite remains hermetic and never reaches the cloud.
var dataProtectionBuilder = builder.Services
    .AddDataProtection()
    .SetApplicationName("ContosoUniversity");

var dataProtectionBlobUri = builder.Configuration["DATAPROTECTION_BLOB_URI"];
if (!string.IsNullOrWhiteSpace(dataProtectionBlobUri))
{
    dataProtectionBuilder.PersistKeysToAzureBlobStorage(
        new Uri(dataProtectionBlobUri),
        new DefaultAzureCredential());
}
else
{
    var localKeyDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ContosoUniversity",
        "keys");
    Directory.CreateDirectory(localKeyDir);
    dataProtectionBuilder.PersistKeysToFileSystem(new DirectoryInfo(localKeyDir));
}

var dataProtectionKeyVaultKeyUri = builder.Configuration["DATAPROTECTION_KEYVAULT_KEY_URI"];
if (!string.IsNullOrWhiteSpace(dataProtectionKeyVaultKeyUri))
{
    dataProtectionBuilder.ProtectKeysWithAzureKeyVault(
        new Uri(dataProtectionKeyVaultKeyUri),
        new DefaultAzureCredential());
}

// rw-001e: register the Application Insights pipeline. The connection string is
// read from configuration (key APPLICATIONINSIGHTS_CONNECTION_STRING). In dev
// the value is empty and telemetry is silently dropped, which is the desired
// behaviour for hermetic test runs.
builder.Services.AddApplicationInsightsTelemetry();

var app = builder.Build();

// rw-001d (SEC-MEDIUM-001): centralised friendly error handling. Both branches
// must run in every environment — including Development — so the Cucumber
// hardening scenarios can verify that no internal details leak. The DeveloperExceptionPage
// is intentionally NOT registered: leaking stack traces is what SEC-MEDIUM-001 explicitly forbids.
app.UseExceptionHandler("/Error/500");
app.UseStatusCodePagesWithReExecute("/Error/{0}");

// HSTS: only meaningful when responses are served over HTTPS. The custom
// SecurityHeadersMiddleware (below) also writes a Strict-Transport-Security
// header unconditionally so the Cucumber harness can assert on it even when
// the request lands on the HTTP listener and is then redirected.
app.UseHsts();

// rw-001d (SEC-MEDIUM-002): append the five standard security response headers
// to every response. Registered BEFORE UseHttpsRedirection so the headers also
// land on the 307/308 redirect responses, and BEFORE UseRouting so a routing
// 404 still carries them.
app.UseMiddleware<SecurityHeadersMiddleware>();

// rw-001d (SEC-MEDIUM-006 + ADR-006): enforce the 30-second default request
// budget configured above. Placed before UseRouting so timed-out requests are
// terminated before the endpoint pipeline runs.
app.UseRequestTimeouts();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// rw-001d (SEC-MEDIUM-001): catch-all for routes that no controller matches.
// Without this, the FallbackPolicy turns "no endpoint" into a 302 redirect to
// /Account/SignIn, masking the underlying 404. The MapFallback endpoint is
// AllowAnonymous so it bypasses the fallback authorization policy and lets
// UseStatusCodePagesWithReExecute re-execute /Error/404.
app.MapFallback(static (HttpContext ctx) =>
{
    ctx.Response.StatusCode = StatusCodes.Status404NotFound;
    return Task.CompletedTask;
}).AllowAnonymous();

// rw-001b: apply pending migrations and seed the dev-stub identity store on startup.
// Wrapped in try/catch so a transient LocalDB outage does not prevent the host from booting
// (the request handlers will surface DB errors at request time instead).
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var startupLogger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = services.GetRequiredService<SchoolContext>();
        await db.Database.MigrateAsync();
        if (app.Environment.IsDevelopment())
        {
            await SeedAuthData.SeedAsync(services);
        }
    }
    catch (Exception ex)
    {
        startupLogger.LogError(ex,
            "Database migration or identity seeding failed at startup. The app will continue to boot; request handlers may fail when they touch the database.");
    }
}

app.Run();

// Expose the implicit Program class so WebApplicationFactory<Program> in
// ContosoUniversity.Web.UnitTests can boot the app in-process for the DI smoke test.
public partial class Program { }
