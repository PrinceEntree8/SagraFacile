using SagraFacile.Contracts.Reservations;

namespace SagraFacile.WebClient.Services;

public interface IReservationRealtimeService
{
    Task EnsureStartedAsync();
    Task NotifyAvailableSeatsUpdatedAsync(int seats);
    IDisposable SubscribeReservationStatusChanged(Func<ReservationStatusChangedNotification, Task> handler);
    IDisposable SubscribeAvailableSeatsUpdated(Func<int, Task> handler);
    IDisposable SubscribeCountersUpdated(Func<List<ReservationCounterDto>, Task> handler);
}