using SagraFacile.Application.Features.Reservations;

namespace SagraFacile.Application.Interfaces;

public interface IReservationNotifier
{
    Task NotifyReservationCreatedAsync(int reservationId, string queueNumber, string customerName, int partySize, CancellationToken cancellationToken);
    Task NotifyReservationCalledAsync(int reservationId, string queueNumber, string customerName, int partySize, int callCount, CancellationToken cancellationToken);
    Task NotifyReservationVoidedAsync(int reservationId, string queueNumber, CancellationToken cancellationToken);
    Task NotifyReservationSeatedAsync(int reservationId, string queueNumber, CancellationToken cancellationToken);
    Task NotifyCountersUpdatedAsync(List<GetCounters.ReservationCounter> counters, CancellationToken cancellationToken);
}
