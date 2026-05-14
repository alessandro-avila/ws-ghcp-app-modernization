using ContosoUniversity.Web.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ContosoUniversity.Web.UnitTests.Foundation;

/// <summary>
/// rw-001a smoke tests: boot the rewrite host in-process via WebApplicationFactory&lt;Program&gt;
/// and verify the dependency-injection container is wired correctly.
/// These tests do NOT touch the database — only the service registrations are validated.
/// </summary>
public class HostStartupSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HostStartupSmokeTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact(DisplayName = "Host boots and SchoolContext can be resolved from DI")]
    public void HostBootsAndSchoolContextResolvesFromDi()
    {
        // Act
        using var scope = _factory.Services.CreateScope();
        var schoolContext = scope.ServiceProvider.GetService<SchoolContext>();

        // Assert: the DbContext is registered and uses the SQL Server provider
        // (matches legacy storage so the rewrite can co-exist on the same database).
        Assert.NotNull(schoolContext);
        Assert.True(
            schoolContext!.Database.IsSqlServer(),
            "SchoolContext must be configured against SQL Server to preserve legacy schema parity (rw-001a AC #4).");
    }

    [Fact(DisplayName = "GET / returns 200 and contains the Home jumbotron marker")]
    public async Task RootEndpointReturnsHomeJumbotronMarker()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/");
        var body = await response.Content.ReadAsStringAsync();

        // Assert: rw-002 ported the legacy Home/Index jumbotron (F-005). The
        // earlier rw-001a placeholder ("ContosoUniversity (rewrite)") was
        // intentionally replaced; the smoke test now tracks the new contract
        // by asserting the legacy welcome string ported verbatim.
        response.EnsureSuccessStatusCode();
        Assert.Contains("Welcome to Contoso University", body);
    }

    [Fact(DisplayName = "rw-008: GET /health returns 200 with HealthChecks JSON body containing 'database' check")]
    public async Task HealthEndpointReturnsJsonReportWithDatabaseCheck()
    {
        // rw-008 (closes SEC-HIGH-004): the rw-001a plain-text HealthController
        // is replaced by the standard ASP.NET Core HealthChecks middleware
        // wired up in Program.cs via AddHealthChecks().AddDbContextCheck<SchoolContext>("database").
        // The endpoint is reachable at both /health and /Health (case-insensitive
        // routing) and returns a structured JSON document so downstream probes
        // can distinguish liveness from readiness.

        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Contains("\"status\":\"Healthy\"", body);
        Assert.Contains("\"name\":\"database\"", body);
    }
}
