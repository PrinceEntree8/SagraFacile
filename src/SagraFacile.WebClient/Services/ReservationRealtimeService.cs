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
    private readonly WeakAsyncEvent<ReservationConnectionState> connectionStateChanged = new();
    private readonly WeakAsyncEvent<ReservationStatusChangedNotification> reservationStatusChanged = new();
    private readonly WeakAsyncEvent<int> availableSeatsUpdated = new();
    private readonly WeakAsyncEvent<List<ReservationCounterDto>> countersUpdated = new();
    private bool lifecycleHandlersRegistered;

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

        RegisterLifecycleHandlers();
    }

    public Task EnsureStartedAsync() => EnsureConnectedAsync();

    public async Task NotifyAvailableSeatsUpdatedAsync(int seats)
    {
        await EnsureConnectedAsync();
        await hubConnection.SendAsync("NotifyAvailableSeatsUpdated", seats);
    }

    public IDisposable SubscribeConnectionStateChanged(Func<ReservationConnectionState, Task> handler) => connectionStateChanged.Subscribe(handler);

    public IDisposable SubscribeReservationStatusChanged(Func<ReservationStatusChangedNotification, Task> handler) => reservationStatusChanged.Subscribe(handler);

    public IDisposable SubscribeAvailableSeatsUpdated(Func<int, Task> handler) => availableSeatsUpdated.Subscribe(handler);

    public IDisposable SubscribeCountersUpdated(Func<List<ReservationCounterDto>, Task> handler) => countersUpdated.Subscribe(handler);

    private async Task EnsureConnectedAsync()
    {
        if (hubConnection.State is HubConnectionState.Connected or HubConnectionState.Connecting or HubConnectionState.Reconnecting)
        {
            return;
        }

        await startLock.WaitAsync();
        try
        {
            if (hubConnection.State == HubConnectionState.Disconnected)
            {
                await RegisterHandlersIfNeededAsync();
                await connectionStateChanged.RaiseAsync(ReservationConnectionState.Connecting);

                try
                {
                    await hubConnection.StartAsync();
                    await connectionStateChanged.RaiseAsync(ReservationConnectionState.Connected);
                }
                catch
                {
                    await connectionStateChanged.RaiseAsync(ReservationConnectionState.Disconnected);
                    throw;
                }
            }
        }
        finally
        {
            startLock.Release();
        }
    }

    private bool handlersRegistered;

    private void RegisterLifecycleHandlers()
    {
        if (lifecycleHandlersRegistered)
        {
            return;
        }

        lifecycleHandlersRegistered = true;

        hubConnection.Reconnecting += _ => connectionStateChanged.RaiseAsync(ReservationConnectionState.Reconnecting);
        hubConnection.Reconnected += _ => connectionStateChanged.RaiseAsync(ReservationConnectionState.Connected);
        hubConnection.Closed += _ => connectionStateChanged.RaiseAsync(ReservationConnectionState.Disconnected);
    }

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