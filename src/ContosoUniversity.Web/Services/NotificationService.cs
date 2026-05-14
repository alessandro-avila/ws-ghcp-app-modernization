using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ContosoUniversity.Web.Data;
using ContosoUniversity.Web.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ContosoUniversity.Web.Services;

/// <summary>
/// rw-007 (F-006): default <see cref="INotificationService"/> that bridges the
/// in-memory queue (<see cref="INotificationQueue"/>) and SQL persistence
/// (<see cref="SchoolContext"/>).
///
/// Lifetime: Scoped because <see cref="SchoolContext"/> is scoped. Controllers
/// resolve a fresh instance per request; the background service resolves a
/// fresh instance per dequeued envelope (via <c>IServiceScopeFactory</c>).
///
/// Threading: <see cref="PublishAsync"/> is queue-side and trivially safe.
/// <see cref="PersistAsync"/> uses a dedicated DbContext from the background
/// service's per-message scope, so concurrent producers do not race on the
/// same SchoolContext instance.
///
/// Closes:
///   * KL-NOTIF-002 — <see cref="MarkAsReadAsync"/> actually flips IsRead +
///     ReadAt instead of being a no-op.
///   * SEC-CRITICAL-002 — every <see cref="PublishAsync"/> caller passes the
///     authenticated <c>User.Identity?.Name</c> rather than the legacy
///     hardcoded "System" sentinel; legacy rows are migrated via
///     <c>infra/migrations/rw-007-backfill-createdby.sql</c>.
/// </summary>
public sealed class NotificationService : INotificationService
{
    private const int DefaultListLimit = 25;

    private readonly INotificationQueue _queue;
    private readonly SchoolContext _db;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        INotificationQueue queue,
        SchoolContext db,
        ILogger<NotificationService> logger)
    {
        _queue = queue;
        _db = db;
        _logger = logger;
    }

    public Task PublishAsync(
        string entityType,
        string entityId,
        string displayName,
        EntityOperation operation,
        string createdBy,
        CancellationToken cancellationToken = default)
    {
        var envelope = new NotificationEnvelope(
            EntityType: entityType,
            EntityId: entityId,
            Operation: operation,
            DisplayName: displayName,
            // SEC-CRITICAL-002: never substitute a sentinel here. If the caller
            // passed null/empty (e.g. an anonymous endpoint that should not be
            // publishing) we want the row to surface the gap explicitly via
            // "(unknown)" rather than impersonating a real principal.
            CreatedBy: string.IsNullOrWhiteSpace(createdBy) ? "(unknown)" : createdBy,
            CreatedAt: DateTime.UtcNow);
        return _queue.PublishAsync(envelope, cancellationToken).AsTask();
    }

    public async Task PersistAsync(NotificationEnvelope envelope, CancellationToken cancellationToken)
    {
        var row = new Notification
        {
            EntityType = envelope.EntityType,
            EntityId = envelope.EntityId,
            Operation = envelope.Operation.ToString(),
            Message = BuildMessage(envelope),
            CreatedAt = envelope.CreatedAt,
            CreatedBy = envelope.CreatedBy,
            IsRead = false,
            ReadAt = null
        };
        _db.Notifications.Add(row);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation(
            "Persisted notification {NotificationId} {EntityType}/{EntityId} {Operation} createdBy={CreatedBy}",
            row.Id, row.EntityType, row.EntityId, row.Operation, row.CreatedBy);
    }

    public async Task<IReadOnlyList<Notification>> ListRecentAsync(int max, CancellationToken cancellationToken = default)
    {
        var limit = max <= 0 ? DefaultListLimit : Math.Min(max, 100);
        return await _db.Notifications
            .AsNoTracking()
            .OrderByDescending(n => n.Id)
            .Take(limit)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> MarkAsReadAsync(int id, CancellationToken cancellationToken = default)
    {
        var row = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken)
            .ConfigureAwait(false);
        if (row is null)
        {
            return false;
        }
        if (row.IsRead)
        {
            return true;
        }
        row.IsRead = true;
        row.ReadAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    private static string BuildMessage(NotificationEnvelope envelope)
    {
        var label = string.IsNullOrWhiteSpace(envelope.DisplayName)
            ? $"{envelope.EntityType} {envelope.EntityId}"
            : $"{envelope.EntityType} '{envelope.DisplayName}'";
        return envelope.Operation switch
        {
            EntityOperation.CREATE => $"{label} was created",
            EntityOperation.UPDATE => $"{label} was updated",
            EntityOperation.DELETE => $"{label} was deleted",
            _ => $"{label} (unknown operation)"
        };
    }
}
