using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ContosoUniversity.Web.Services;

/// <summary>
/// rw-007 (F-006): default <see cref="INotificationQueue"/> implementation
/// backed by a bounded <see cref="Channel{T}"/>. Capacity 1024 is small enough
/// to surface a wedged consumer (producers will await on full) without losing
/// envelopes — the legacy MSMQ shim silently swallowed errors via try/catch
/// inside the controllers.
///
/// <see cref="BoundedChannelFullMode.Wait"/> ensures producers await rather
/// than the channel dropping the oldest item; under the dev/test load this is
/// indistinguishable from unbounded (the background service drains in a tight
/// loop with no work between dequeues), but the cap is in place so a stuck
/// consumer cannot leak unbounded memory in production.
/// </summary>
public sealed class ChannelNotificationQueue : INotificationQueue
{
    private const int Capacity = 1024;
    private readonly Channel<NotificationEnvelope> _channel;

    public ChannelNotificationQueue()
    {
        _channel = Channel.CreateBounded<NotificationEnvelope>(
            new BoundedChannelOptions(Capacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            });
    }

    public ValueTask PublishAsync(NotificationEnvelope envelope, CancellationToken cancellationToken)
        => _channel.Writer.WriteAsync(envelope, cancellationToken);

    public ValueTask<NotificationEnvelope> DequeueAsync(CancellationToken cancellationToken)
        => _channel.Reader.ReadAsync(cancellationToken);
}
