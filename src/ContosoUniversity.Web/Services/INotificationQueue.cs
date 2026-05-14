using System.Threading;
using System.Threading.Tasks;

namespace ContosoUniversity.Web.Services;

/// <summary>
/// rw-007 (F-006): bounded in-memory queue contract that decouples the
/// producing controller (Departments/Students/Courses/Instructors CUD) from
/// the consuming background service that writes to SQL.
///
/// The default <see cref="ChannelNotificationQueue"/> implementation is backed
/// by <c>System.Threading.Channels.Channel{T}</c> with a fixed capacity to
/// provide back-pressure; producers await on a full channel rather than
/// dropping notifications silently. This replaces the legacy in-process MSMQ
/// shim that swallowed errors via try/catch in the controllers.
/// </summary>
public interface INotificationQueue
{
    /// <summary>
    /// Enqueue a <see cref="NotificationEnvelope"/> for asynchronous persistence.
    /// Returns once the envelope has been accepted by the underlying channel.
    /// </summary>
    ValueTask PublishAsync(NotificationEnvelope envelope, CancellationToken cancellationToken);

    /// <summary>
    /// Pull the next envelope. Awaited by the
    /// <see cref="NotificationProcessorBackgroundService"/> in a loop.
    /// </summary>
    ValueTask<NotificationEnvelope> DequeueAsync(CancellationToken cancellationToken);
}
