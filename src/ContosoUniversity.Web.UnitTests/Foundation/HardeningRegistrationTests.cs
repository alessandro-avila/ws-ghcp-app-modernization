using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Xunit;

namespace ContosoUniversity.Web.UnitTests.Foundation;

/// <summary>
/// rw-001d DI-side verification for the production-hardening middleware that
/// has no good HTTP-side test:
/// <list type="bullet">
///   <item><description>SEC-MEDIUM-006: every request gets a 30-second default timeout (asserting against the live HTTP pipeline would require keeping a request open for 30s).</description></item>
///   <item><description>SEC-MEDIUM-005: the JSON console formatter is registered (asserting against stdout is brittle in a parallel test runner).</description></item>
/// </list>
/// The HTTP-observable hardening (5 security headers, friendly 404, masked 500)
/// is covered by the Cucumber harness in tests/integration/features/rw-001d-hardening.feature.
/// </summary>
public class HardeningRegistrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HardeningRegistrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact(DisplayName = "Default request-timeout policy is 30 seconds (SEC-MEDIUM-006 / ADR-006)")]
    public void DefaultRequestTimeoutPolicyIsThirtySeconds()
    {
        // Act
        var timeoutOptions = _factory.Services.GetRequiredService<IOptions<RequestTimeoutOptions>>().Value;

        // Assert
        Assert.NotNull(timeoutOptions.DefaultPolicy);
        Assert.Equal(TimeSpan.FromSeconds(30), timeoutOptions.DefaultPolicy!.Timeout);
    }

    [Fact(DisplayName = "JSON console formatter is registered (SEC-MEDIUM-005)")]
    public void JsonConsoleFormatterIsRegistered()
    {
        // Act: AddJsonConsole(...) registers the configuration delegate with the default options
        // name (""), and the framework's ConsoleFormatter lookup matches the formatter name "json"
        // separately. We verify both: (1) the configured IncludeScopes value flowed through, and
        // (2) the "json" formatter is registered as an IConsoleFormatterFactory implementation.
        var formatterOptions = _factory.Services
            .GetRequiredService<IOptionsMonitor<JsonConsoleFormatterOptions>>()
            .CurrentValue;

        var formatters = _factory.Services
            .GetServices<ConsoleFormatter>()
            .Select(f => f.Name)
            .ToArray();

        // Assert
        Assert.True(
            formatterOptions.IncludeScopes,
            "JSON console formatter should have IncludeScopes=true so structured log queries can group by scope.");
        Assert.Contains(ConsoleFormatterNames.Json, formatters);
    }
}
