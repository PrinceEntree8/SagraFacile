using SagraFacile.Application.Features.Reservations;

namespace SagraFacile.Application.Interfaces;

/// <summary>
/// Enqueues reservation notifications into an internal channel.
/// Implementations must not block the caller beyond the channel write.
/// </summary>
public interface IReservationNotifier
{
    ValueTask EnqueueStatusChangedAsync(
        ReservationStatusChangedNotification notification,
        CancellationToken cancellationToken);

    ValueTask EnqueueCountersUpdatedAsync(
        CountersUpdatedNotification notification,
        CancellationToken cancellationToken);
}
