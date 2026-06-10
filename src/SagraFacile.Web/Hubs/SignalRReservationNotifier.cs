using SagraFacile.Application.Features.Reservations;
using SagraFacile.Application.Interfaces;

namespace SagraFacile.Web.Hubs;

/// <summary>
/// Scoped implementation of <see cref="IReservationNotifier"/>.
/// Writes messages to the singleton <see cref="ReservationNotificationChannel"/>;
/// never calls SignalR directly.
/// </summary>
public sealed class SignalRReservationNotifier(ReservationNotificationChannel channel)
    : IReservationNotifier
{
    public ValueTask EnqueueStatusChangedAsync(
        ReservationStatusChangedNotification notification,
        CancellationToken cancellationToken)
        => channel.Writer.WriteAsync(new StatusChangedMessage(notification), cancellationToken);

    public ValueTask EnqueueCountersUpdatedAsync(
        CountersUpdatedNotification notification,
        CancellationToken cancellationToken)
        => channel.Writer.WriteAsync(new CountersUpdatedMessage(notification), cancellationToken);
}
