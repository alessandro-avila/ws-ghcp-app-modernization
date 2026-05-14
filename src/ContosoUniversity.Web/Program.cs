using ContosoUniversity.Web.Data;
using ContosoUniversity.Web.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

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
