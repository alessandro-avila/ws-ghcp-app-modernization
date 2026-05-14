#nullable disable
using Microsoft.AspNetCore.Identity;

namespace ContosoUniversity.Web.Identity;

/// <summary>
/// Application identity principal for the rewrite app's dev-stub sign-in (rw-001b).
/// Extends <see cref="IdentityUser"/> with no extra fields - placeholder for
/// future profile data (e.g., DisplayName, Department, etc.). The dev-stub will
/// be superseded by Microsoft Entra ID in rw-001c, at which point claims from
/// the OIDC token take over and this class survives as the local user store
/// shell only.
/// </summary>
public class ApplicationUser : IdentityUser
{
}
