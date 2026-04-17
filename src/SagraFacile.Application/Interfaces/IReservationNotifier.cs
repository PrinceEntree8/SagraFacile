namespace SagraFacile.Application.Interfaces;

public interface IReservationNotifier
{
    Task NotifyReservationCreatedAsync(int reservationId, string queueNumber, string customerName, int partySize, CancellationToken cancellationToken);
    Task NotifyReservationCalledAsync(int reservationId, string queueNumber, string customerName, int partySize, int callCount, CancellationToken cancellationToken);
    Task NotifyReservationVoidedAsync(int reservationId, string queueNumber, CancellationToken cancellationToken);
}
