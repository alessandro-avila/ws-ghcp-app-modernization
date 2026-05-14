#nullable disable
using ContosoUniversity.Web.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ContosoUniversity.Web.Data;

/// <summary>
/// Seeds the dev-stub identity store (rw-001b) with two well-known users on first run.
/// Idempotent — safe to call on every host start.
///
/// Dev-stub only. The credentials below are checked into source intentionally because:
///   - The rewrite is a brownfield modernization in progress; rw-001c will replace this
///     dev-stub sign-in entirely with Microsoft Entra ID OIDC.
///   - The seeded users only exist in the local LocalDB ContosoUniversity database, which
///     is bound to the developer's machine and never deployed to a shared environment.
///   - The Program.cs guard enforces seeding only when ASPNETCORE_ENVIRONMENT=Development.
///
/// rw-001c will delete this file along with <c>AccountController</c> and the dev-stub view.
/// </summary>
public static class SeedAuthData
{
    public const string AdminEmail = "admin@contoso.test";
    public const string ReaderEmail = "reader@contoso.test";

    private const string AdminPassword = "Adm1n!Pass";
    private const string ReaderPassword = "Read3r!Pass";

    public const string AdminRole = "Admin";
    public const string ReaderRole = "Reader";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var logger = services.GetRequiredService<ILogger<object>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var role in new[] { AdminRole, ReaderRole })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var roleResult = await roleManager.CreateAsync(new IdentityRole(role));
                if (!roleResult.Succeeded)
                {
                    logger.LogWarning("Failed to seed role {Role}: {Errors}",
                        role, string.Join("; ", roleResult.Errors.Select(e => e.Description)));
                }
            }
        }

        await EnsureUserAsync(userManager, logger, AdminEmail, AdminPassword, AdminRole);
        await EnsureUserAsync(userManager, logger, ReaderEmail, ReaderPassword, ReaderRole);
    }

    private static async Task EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        ILogger logger,
        string email,
        string password,
        string role)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing != null)
        {
            if (!await userManager.IsInRoleAsync(existing, role))
            {
                await userManager.AddToRoleAsync(existing, role);
            }
            return;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };
        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            logger.LogWarning("Failed to seed user {Email}: {Errors}",
                email, string.Join("; ", createResult.Errors.Select(e => e.Description)));
            return;
        }

        var roleResult = await userManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
        {
            logger.LogWarning("Failed to assign role {Role} to {Email}: {Errors}",
                role, email, string.Join("; ", roleResult.Errors.Select(e => e.Description)));
        }
    }
}
