using System;
using System.Threading;
using System.Threading.Tasks;
using ContosoUniversity.Web.Domain;
using ContosoUniversity.Web.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace ContosoUniversity.Web.UnitTests.Foundation;

/// <summary>
/// rw-007 — Notification subsystem DI-side verification. The HTTP-observable
/// behaviour is covered by tests/integration/features/rw-007-notifications.feature
/// (Cucumber). These tests cover the contracts that have no good HTTP surface:
/// <list type="bullet">
///   <item><description>The bounded notification queue must accept and yield envelopes without re-ordering them — a structural guard against silent regressions if the underlying Channel&lt;T&gt; capacity or full-mode policy is tweaked.</description></item>
///   <item><description>The background-service hosted in DI must be of the
///   expected concrete type so the producer→queue→consumer→DB pipeline
///   actually runs at startup. Without the registration the publish from a
///   controller is silently dropped.</description></item>
/// </list>
/// </summary>
public class NotificationInfrastructureTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public NotificationInfrastructureTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact(DisplayName = "INotificationQueue round-trips envelopes in FIFO order (rw-007)")]
    public async Task NotificationQueueRoundTripsEnvelopesInFifoOrder()
    {
        // NOTE: We instantiate ChannelNotificationQueue directly rather than resolving
        // INotificationQueue from _factory.Services because the running
        // NotificationProcessorBackgroundService consumes from the DI-registered
        // queue and would race the test for every envelope. Both behaviours
        // are still covered: the FIFO contract is verified here, and the
        // hosted-service registration is verified by the sibling test below
        // (the registered ChannelNotificationQueue is the same concrete type).
        var queue = new ChannelNotificationQueue();
        var first = new NotificationEnvelope(
            EntityType: "Department",
            EntityId: "1",
            Operation: EntityOperation.CREATE,
            DisplayName: "Test department",
            CreatedBy: "rw-007-test",
            CreatedAt: DateTime.UtcNow);
        var second = first with { EntityId = "2" };

        await queue.PublishAsync(first, CancellationToken.None);
        await queue.PublishAsync(second, CancellationToken.None);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var dequeuedFirst = await queue.DequeueAsync(cts.Token);
        var dequeuedSecond = await queue.DequeueAsync(cts.Token);

        Assert.Equal("1", dequeuedFirst.EntityId);
        Assert.Equal("2", dequeuedSecond.EntityId);
    }

    [Fact(DisplayName = "NotificationProcessor BackgroundService is registered (rw-007)")]
    public void NotificationProcessorIsRegisteredAsHostedService()
    {
        var hostedServices = _factory.Services.GetServices<IHostedService>();
        Assert.Contains(hostedServices, s => s is NotificationProcessorBackgroundService);
    }
}
