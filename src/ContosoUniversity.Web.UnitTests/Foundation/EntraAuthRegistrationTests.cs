using System.IO;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ContosoUniversity.Web.UnitTests.Foundation;

/// <summary>
/// rw-001c authentication-scheme verification: prove that Microsoft Entra ID
/// (OpenID Connect) becomes the default challenge scheme when
/// <c>AzureAd:ClientId</c> is configured, and that the dev-stub cookie path
/// remains the default when it is not. See ADR-008 for the dual-mode rationale.
///
/// All tests are hermetic: they configure placeholder Entra values via
/// in-memory configuration so no live tenant is required. The end-to-end
/// browser-driven OIDC redirect against a real tenant is documented as a
/// USER ACTION smoke test in ADR-008 §USER ACTION.
/// </summary>
public class EntraAuthRegistrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    // The well-known scheme name registered by Microsoft.Identity.Web's OpenID Connect
    // handler. Using the literal here (instead of OpenIdConnectDefaults.AuthenticationScheme)
    // keeps the test project free of a direct Microsoft.AspNetCore.Authentication.OpenIdConnect
    // package reference — it is brought in transitively by Microsoft.Identity.Web in the Web
    // project and never needs to surface in the test project. The value is fixed by the OIDC
    // standard and the ASP.NET Core source: see
    // https://github.com/dotnet/aspnetcore/blob/main/src/Security/Authentication/OpenIdConnect/src/OpenIdConnectDefaults.cs.
    private const string OpenIdConnectSchemeName = "OpenIdConnect";

    private readonly WebApplicationFactory<Program> _factory;

    public EntraAuthRegistrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact(DisplayName = "OpenID Connect is the default challenge scheme when AzureAd:ClientId is configured (rw-001c AC #3, ADR-008)")]
    public async Task OpenIdConnectIsDefaultChallengeWhenAzureAdConfigured()
    {
        // Arrange: WebApplicationBuilder.Configuration is read by Program.cs BEFORE
        // builder.Build() runs, so configuration sources added via WithWebHostBuilder
        // (which only fire during Build) are invisible to the AzureAd:ClientId gate.
        // The only configuration source loaded by WebApplication.CreateBuilder() that
        // we can mutate from a test is the process environment. Setting double-
        // underscore env vars matches the default ConfigurationManager mapping
        // ("AzureAd__ClientId" -> "AzureAd:ClientId"). Values are syntactically valid
        // GUIDs so Microsoft.Identity.Web's option-validation succeeds; they are NOT
        // real and no network call is made because the test never issues a challenge.
        var entries = new Dictionary<string, string?>
        {
            ["AzureAd__Instance"] = "https://login.microsoftonline.com/",
            ["AzureAd__Domain"] = "contoso.onmicrosoft.com",
            ["AzureAd__TenantId"] = "00000000-0000-0000-0000-000000000000",
            ["AzureAd__ClientId"] = "00000000-0000-0000-0000-000000000001",
            ["AzureAd__CallbackPath"] = "/signin-oidc",
            ["AzureAd__SignedOutCallbackPath"] = "/signout-callback-oidc",
        };

        var previous = new Dictionary<string, string?>();
        foreach (var kv in entries)
        {
            previous[kv.Key] = Environment.GetEnvironmentVariable(kv.Key);
            Environment.SetEnvironmentVariable(kv.Key, kv.Value);
        }

        try
        {
            // Build a brand-new factory (NOT the class-fixture instance) so the
            // host is constructed AFTER the env vars are in place. The class
            // fixture's host was built at construction time and would have already
            // baked in the (empty) AzureAd:ClientId value.
            await using var entraFactory = new WebApplicationFactory<Program>();
            var schemeProvider = entraFactory.Services.GetRequiredService<IAuthenticationSchemeProvider>();

            // Act
            var defaultChallengeScheme = await schemeProvider.GetDefaultChallengeSchemeAsync();

            // Assert
            Assert.NotNull(defaultChallengeScheme);
            Assert.Equal(OpenIdConnectSchemeName, defaultChallengeScheme!.Name);
        }
        finally
        {
            // Restore process environment so other tests in this run see no leakage.
            foreach (var kv in previous)
            {
                Environment.SetEnvironmentVariable(kv.Key, kv.Value);
            }
        }
    }

    [Fact(DisplayName = "Cookie scheme remains the default challenge when AzureAd:ClientId is empty — dev-stub fallback (ADR-008)")]
    public async Task CookieRemainsDefaultChallengeWhenAzureAdNotConfigured()
    {
        // Act: the default factory has no AzureAd config so the dev-stub cookie path
        // (registered by AddIdentity, scheme name = IdentityConstants.ApplicationScheme)
        // must remain the default challenge. This is a regression lock that fails the
        // moment a future change makes the OIDC scheme the default unconditionally.
        var schemeProvider = _factory.Services.GetRequiredService<IAuthenticationSchemeProvider>();
        var defaultChallengeScheme = await schemeProvider.GetDefaultChallengeSchemeAsync();

        // Assert
        Assert.NotNull(defaultChallengeScheme);
        Assert.Equal(IdentityConstants.ApplicationScheme, defaultChallengeScheme!.Name);
    }

    [Fact(DisplayName = "Microsoft.Identity.Client is NOT a direct package reference (rw-001c AC #10, SEC-LOW-003)")]
    public void LegacyMicrosoftIdentityClientIsNotADirectDependency()
    {
        // Arrange: locate the Web project's csproj relative to this test assembly's
        // execution directory. Tests run from src/ContosoUniversity.Web.UnitTests/
        // bin/{Configuration}/net8.0/ so we walk up the tree to find the solution
        // root marker (ContosoUniversity.sln) and descend to the target csproj.
        var searchRoot = new DirectoryInfo(AppContext.BaseDirectory);
        while (searchRoot is not null && !File.Exists(Path.Combine(searchRoot.FullName, "ContosoUniversity.sln")))
        {
            searchRoot = searchRoot.Parent;
        }

        Assert.NotNull(searchRoot);

        var csprojPath = Path.Combine(
            searchRoot!.FullName,
            "src",
            "ContosoUniversity.Web",
            "ContosoUniversity.Web.csproj");

        Assert.True(File.Exists(csprojPath), $"Expected csproj at {csprojPath}");

        // Act
        var csprojContent = File.ReadAllText(csprojPath);

        // Assert: AC #10 explicitly forbids a direct PackageReference to
        // Microsoft.Identity.Client. The transitive pull-through from
        // Microsoft.Identity.Web 3.x is required (it is the supported wrapper) and
        // is at v4.65+, well above the legacy 4.21.1 SEC-LOW-003 dead-dep that
        // triggered this AC. This test fails the moment a future change introduces
        // a direct reference to the bare MSAL package in the rewrite csproj.
        Assert.DoesNotContain(
            "<PackageReference Include=\"Microsoft.Identity.Client\"",
            csprojContent,
            StringComparison.OrdinalIgnoreCase);
    }
}
