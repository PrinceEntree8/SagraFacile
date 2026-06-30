using SagraFacile.Contracts.Reservations;

namespace SagraFacile.WebClient.Services;

public enum ReservationConnectionState
{
    Connecting,
    Connected,
    Reconnecting,
    Disconnected
}

public interface IReservationRealtimeService
{
    Task EnsureStartedAsync();
    Task NotifyAvailableSeatsUpdatedAsync(int seats);
    IDisposable SubscribeConnectionStateChanged(Func<ReservationConnectionState, Task> handler);
    IDisposable SubscribeReservationStatusChanged(Func<ReservationStatusChangedNotification, Task> handler);
    IDisposable SubscribeAvailableSeatsUpdated(Func<int, Task> handler);
    IDisposable SubscribeCountersUpdated(Func<List<ReservationCounterDto>, Task> handler);
}