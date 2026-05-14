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

    [Fact(DisplayName = "GET / returns 200 and contains 'ContosoUniversity (rewrite)' marker")]
    public async Task RootEndpointReturnsRewriteMarker()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/");
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Contains("ContosoUniversity (rewrite)", body);
    }

    [Fact(DisplayName = "GET /Health returns 200 and 'Healthy'")]
    public async Task HealthEndpointReturnsHealthy()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/Health");
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal("Healthy", body);
    }
}
