using System.Threading.Channels;
using SagraFacile.Application.Features.Reservations;

namespace SagraFacile.Web.Hubs;

/// <summary>
/// Singleton channel shared between all Scoped <see cref="SignalRReservationNotifier"/> instances
/// (producers) and the singleton <see cref="ReservationNotificationDispatcher"/> (consumer).
/// </summary>
public sealed class ReservationNotificationChannel
{
    private readonly Channel<ReservationNotificationMessage> _channel =
        Channel.CreateBounded<ReservationNotificationMessage>(
            new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            });

    public ChannelWriter<ReservationNotificationMessage> Writer => _channel.Writer;
    public ChannelReader<ReservationNotificationMessage> Reader => _channel.Reader;
}