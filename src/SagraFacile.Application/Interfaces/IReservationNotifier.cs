using SagraFacile.Application.Features.Reservations;

namespace SagraFacile.Application.Interfaces;

public interface IReservationNotifier
{
    Task NotifyReservationCreatedAsync(int reservationId, int sequenceNumber, string customerName, int partySize, CancellationToken cancellationToken);
    Task NotifyReservationPartyCompleteAsync(int reservationId, int sequenceNumber, string customerName, int partySize, CancellationToken cancellationToken);
    Task NotifyReservationCalledAsync(int reservationId, int sequenceNumber, string customerName, int partySize, int callCount, CancellationToken cancellationToken);
    Task NotifyReservationVoidedAsync(int reservationId, int sequenceNumber, CancellationToken cancellationToken);
    Task NotifyReservationSeatedAsync(int reservationId, int sequenceNumber, CancellationToken cancellationToken);
    Task NotifyCountersUpdatedAsync(List<GetCounters.ReservationCounter> counters, CancellationToken cancellationToken);
}
