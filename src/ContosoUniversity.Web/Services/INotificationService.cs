using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ContosoUniversity.Web.Domain;

namespace ContosoUniversity.Web.Services;

/// <summary>
/// rw-007 (F-006): notification subsystem facade. Controllers depend on this
/// interface (via DI) instead of the queue / DbContext directly, so the
/// producer surface stays small and the persistence/read paths can evolve
/// independently.
///
/// Surface:
///   * <see cref="PublishAsync"/> — enqueue a CUD-driven notification.
///   * <see cref="PersistAsync"/> — called by the background service to write
///     a dequeued envelope to SQL via <c>SchoolContext</c>.
///   * <see cref="ListRecentAsync"/> — read-side for the dashboard.
///   * <see cref="MarkAsReadAsync"/> — flips IsRead+ReadAt on a single row;
///     fixes legacy KL-NOTIF-002 (controller action was empty body).
/// </summary>
public interface INotificationService
{
    Task PublishAsync(
        string entityType,
        string entityId,
        string displayName,
        EntityOperation operation,
        string createdBy,
        CancellationToken cancellationToken = default);

    Task PersistAsync(NotificationEnvelope envelope, CancellationToken cancellationToken);

    Task<IReadOnlyList<Notification>> ListRecentAsync(int max, CancellationToken cancellationToken = default);

    Task<bool> MarkAsReadAsync(int id, CancellationToken cancellationToken = default);
}
