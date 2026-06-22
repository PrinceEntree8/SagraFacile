using System.Collections.Concurrent;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using SagraFacile.Contracts.Reservations;
using SagraFacile.WebClient.Auth;

namespace SagraFacile.WebClient.Services;

public sealed class ReservationRealtimeService : IReservationRealtimeService, IAsyncDisposable
{
    private readonly HubConnection hubConnection;

    private readonly SemaphoreSlim startLock = new(1, 1);
    private readonly WeakAsyncEvent<ReservationStatusChangedNotification> reservationStatusChanged = new();
    private readonly WeakAsyncEvent<int> availableSeatsUpdated = new();
    private readonly WeakAsyncEvent<List<ReservationCounterDto>> countersUpdated = new();

    public ReservationRealtimeService(TokenStorageService tokenStorage, NavigationManager navigationManager)
    {
        var hubUri = navigationManager.ToAbsoluteUri("/hubs/reservations");
        hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUri, options =>
            {
                options.AccessTokenProvider = () => tokenStorage.GetTokenAsync().AsTask();
            })
            .WithAutomaticReconnect()
            .Build();
    }

    public Task EnsureStartedAsync() => EnsureConnectedAsync();

    public async Task NotifyAvailableSeatsUpdatedAsync(int seats)
    {
        await EnsureConnectedAsync();
        await hubConnection.SendAsync("NotifyAvailableSeatsUpdated", seats);
    }

    public IDisposable SubscribeReservationStatusChanged(Func<ReservationStatusChangedNotification, Task> handler) => reservationStatusChanged.Subscribe(handler);

    public IDisposable SubscribeAvailableSeatsUpdated(Func<int, Task> handler) => availableSeatsUpdated.Subscribe(handler);

    public IDisposable SubscribeCountersUpdated(Func<List<ReservationCounterDto>, Task> handler) => countersUpdated.Subscribe(handler);

    private async Task EnsureConnectedAsync()
    {
        if (hubConnection.State == HubConnectionState.Connected)
        {
            return;
        }

        await startLock.WaitAsync();
        try
        {
            if (hubConnection.State != HubConnectionState.Connected)
            {
                await RegisterHandlersIfNeededAsync();
                await hubConnection.StartAsync();
            }
        }
        finally
        {
            startLock.Release();
        }
    }

    private bool handlersRegistered;

    private Task RegisterHandlersIfNeededAsync()
    {
        if (handlersRegistered)
        {
            return Task.CompletedTask;
        }

        handlersRegistered = true;

        hubConnection.On<ReservationStatusChangedNotification>(
            nameof(IReservationHubClient.ReservationStatusChanged),
            notification => reservationStatusChanged.RaiseAsync(notification));

        hubConnection.On<int>("AvailableSeatsUpdated", seats => availableSeatsUpdated.RaiseAsync(seats));
        hubConnection.On<List<ReservationCounterDto>>("CountersUpdated", counters => countersUpdated.RaiseAsync(counters));

        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await hubConnection.DisposeAsync();
        startLock.Dispose();
    }

    private sealed class WeakAsyncEvent<TPayload>
    {
        private readonly ConcurrentDictionary<Guid, WeakReference<Func<TPayload, Task>>> handlers = new();

        public IDisposable Subscribe(Func<TPayload, Task> handler)
        {
            var key = Guid.NewGuid();
            handlers[key] = new WeakReference<Func<TPayload, Task>>(handler);
            return new Subscription(() => handlers.TryRemove(key, out _));
        }

        public async Task RaiseAsync(TPayload payload)
        {
            foreach (var entry in handlers)
            {
                if (entry.Value.TryGetTarget(out var handler))
                {
                    await handler(payload);
                    continue;
                }

                handlers.TryRemove(entry.Key, out _);
            }
        }

        private sealed class Subscription(Action unsubscribe) : IDisposable
        {
            private Action? unsubscribe = unsubscribe;

            public void Dispose()
            {
                var action = Interlocked.Exchange(ref unsubscribe, null);
                action?.Invoke();
            }
        }
    }
}