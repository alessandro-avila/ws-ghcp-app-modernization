using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ContosoUniversity.Web.Services;

/// <summary>
/// rw-007 (F-006): hosted background service that drains
/// <see cref="INotificationQueue"/> in a tight loop and persists each envelope
/// via <see cref="INotificationService.PersistAsync"/>. Replaces the legacy
/// in-process MSMQ shim that drained on demand inside the controller's
/// GetNotifications action (which had its own pile of try/catch swallowers).
///
/// Per-message scope: <see cref="INotificationService"/> is Scoped because
/// <see cref="SchoolContext"/> is Scoped. Resolving a fresh scope per dequeued
/// envelope keeps the DbContext lifetime correct without requiring the queue
/// itself to be scoped.
/// </summary>
public sealed class NotificationProcessorBackgroundService : BackgroundService
{
    private readonly INotificationQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationProcessorBackgroundService> _logger;

    public NotificationProcessorBackgroundService(
        INotificationQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<NotificationProcessorBackgroundService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Notification processor started");
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                NotificationEnvelope envelope;
                try
                {
                    envelope = await _queue.DequeueAsync(stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<INotificationService>();
                try
                {
                    await service.PersistAsync(envelope, stoppingToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    // Persistence error: log and continue. Dropping a single
                    // envelope on the floor is preferable to taking down the
                    // background service (which would silently stop persisting
                    // every subsequent notification). The error is observable
                    // via the structured-log entry; downstream alerting can
                    // pick it up.
                    _logger.LogError(ex,
                        "Failed to persist notification {EntityType}/{EntityId} {Operation}",
                        envelope.EntityType, envelope.EntityId, envelope.Operation);
                }
            }
        }
        finally
        {
            _logger.LogInformation("Notification processor stopped");
        }
    }
}
