using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace ContosoUniversity.Web.UnitTests.Foundation;

/// <summary>
/// rw-001e DI-side verification for the runtime services that have no good
/// HTTP-observable test:
/// <list type="bullet">
///   <item><description>Data Protection: the configured ApplicationDiscriminator must be the explicit value <c>ContosoUniversity</c> so that keys remain shareable across cluster replicas regardless of the runtime assembly name. Default WebApplicationBuilder behaviour leaves this as the assembly name (<c>ContosoUniversity.Web</c>), which is brittle.</description></item>
///   <item><description>Application Insights: the <see cref="TelemetryClient"/> must resolve from DI so request handlers can emit custom telemetry. Without an explicit <c>AddApplicationInsightsTelemetry()</c> call this resolution fails.</description></item>
/// </list>
/// </summary>
public class RuntimeServicesTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RuntimeServicesTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact(DisplayName = "Data Protection ApplicationDiscriminator is set to 'ContosoUniversity' (rw-001e)")]
    public void DataProtectionApplicationDiscriminatorMatchesContosoUniversity()
    {
        // Act
        var dataProtectionOptions = _factory.Services
            .GetRequiredService<IOptions<DataProtectionOptions>>()
            .Value;

        // Assert
        Assert.Equal("ContosoUniversity", dataProtectionOptions.ApplicationDiscriminator);
    }

    [Fact(DisplayName = "TelemetryClient is registered in DI (rw-001e)")]
    public void ApplicationInsightsTelemetryClientIsRegistered()
    {
        // Act
        var telemetryClient = _factory.Services.GetService<TelemetryClient>();

        // Assert
        Assert.NotNull(telemetryClient);
    }
}
