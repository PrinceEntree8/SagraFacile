# Plan: Dedicated `ReservationCalled` SignalR Event

## Goal
Add a new SignalR event that only fires when a reservation becomes `Called` (add to display) or stops being `Called` (remove from display). The 3 public display components will subscribe to this instead of the general `ReservationStatusChanged`.

## Layer-by-layer changes

### 1. Contracts — `src/SagraFacile.Contracts/Reservations/Notifications.cs`

Add after `CountersUpdatedNotification`:

```csharp
public record ReservationCalledNotification(
    int ReservationId,
    bool IsActive,
    int SequenceNumber,
    string CustomerName,
    int PartySize,
    int CallCount
);
```

Add to the message hierarchy:

```csharp
public record CalledMessage(ReservationCalledNotification Notification) : ReservationNotificationMessage;
```

### 2. Contracts — `src/SagraFacile.Contracts/Reservations/IReservationHubClient.cs`

Add after `ReservationStatusChanged`:

```csharp
Task ReservationCalled(ReservationCalledNotification notification);
```

### 3. Application — `src/SagraFacile.Application/Interfaces/IReservationNotifier.cs`

Add after `EnqueueCountersUpdatedAsync`:

```csharp
ValueTask EnqueueCalledAsync(
    ReservationCalledNotification notification,
    CancellationToken cancellationToken = default);
```

### 4. Web — `src/SagraFacile.Web/Hubs/SignalRReservationNotifier.cs`

Add implementation:

```csharp
public ValueTask EnqueueCalledAsync(
    ReservationCalledNotification notification,
    CancellationToken cancellationToken)
    => channel.Writer.WriteAsync(new CalledMessage(notification), cancellationToken);
```

### 5. Web — `src/SagraFacile.Web/Hubs/ReservationNotificationDispatcher.cs`

Add case in the switch:

```csharp
case CalledMessage m:
    await hubContext.Clients.All
        .ReservationCalled(m.Notification)
        .WaitAsync(stoppingToken);
    break;
```

### 6. Application — 4 command handlers

Each handler already has `notifier` injected. Add `EnqueueCalledAsync` call after the existing `EnqueueStatusChangedAsync` call.

#### `src/SagraFacile.Application/Features/Reservations/CallReservation.cs`

After the `EnqueueStatusChangedAsync` call (around line 94), add:

```csharp
notifier.EnqueueCalledAsync(new ReservationCalledNotification(
    reservation.Id,
    IsActive: true,
    reservation.SequenceNumber,
    reservation.CustomerName,
    reservation.PartySize,
    reservation.CallCount
), cancellationToken).Forget();
```

#### `src/SagraFacile.Application/Features/Reservations/SeatReservation.cs`

After the `EnqueueStatusChangedAsync` call (around line 69), add:

```csharp
notifier.EnqueueCalledAsync(new ReservationCalledNotification(
    reservation.Id,
    IsActive: false,
    reservation.SequenceNumber,
    reservation.CustomerName,
    reservation.PartySize,
    reservation.CallCount
), cancellationToken).Forget();
```

#### `src/SagraFacile.Application/Features/Reservations/VoidReservation.cs`

After the `EnqueueStatusChangedAsync` call (around line 60), add a conditional:

```csharp
if (oldStatus == ReservationStatus.Called)
{
    notifier.EnqueueCalledAsync(new ReservationCalledNotification(
        reservation.Id,
        IsActive: false,
        reservation.SequenceNumber,
        reservation.CustomerName,
        reservation.PartySize,
        reservation.CallCount
    ), cancellationToken).Forget();
}
```

#### `src/SagraFacile.Application/Features/Reservations/CallAndSeatReservation.cs`

After the `EnqueueStatusChangedAsync` call (around line 86), add a conditional:

```csharp
if (oldStatus == ReservationStatus.Called)
{
    await notifier.EnqueueCalledAsync(new ReservationCalledNotification(
        reservation.Id,
        IsActive: false,
        reservation.SequenceNumber,
        reservation.CustomerName,
        reservation.PartySize,
        reservation.CallCount
    ), cancellationToken);
}
```

Note: This handler uses `await` (not `.Forget()`), so match the existing pattern.

### 7. WebClient — `src/SagraFacile.WebClient/Services/IReservationRealtimeService.cs`

Add after `SubscribeReservationStatusChanged`:

```csharp
IDisposable SubscribeReservationCalled(Func<ReservationCalledNotification, Task> handler);
```

### 8. WebClient — `src/SagraFacile.WebClient/Services/ReservationRealtimeService.cs`

Add field:

```csharp
private readonly WeakAsyncEvent<ReservationCalledNotification> reservationCalled = new();
```

Add method:

```csharp
public IDisposable SubscribeReservationCalled(Func<ReservationCalledNotification, Task> handler)
    => reservationCalled.Subscribe(handler);
```

In `RegisterHandlersIfNeededAsync()`, add:

```csharp
hubConnection.On<ReservationCalledNotification>(
    nameof(IReservationHubClient.ReservationCalled),
    notification => reservationCalled.RaiseAsync(notification));
```

### 9. Update 3 display components

#### `src/SagraFacile.WebClient/Components/Pages/LatestCalledTicket.razor`

Replace the `SubscribeReservationStatusChanged` call in `InitializeSignalRAsync` with `SubscribeReservationCalled`. Simplify the handler:

```csharp
reservationSubscription = realtimeService.SubscribeReservationCalled(async notification =>
{
    if (notification.IsActive)
    {
        latestCalled = new CalledEntry(
            notification.ReservationId,
            notification.SequenceNumber,
            notification.CustomerName,
            notification.PartySize,
            notification.CallCount);
    }
    else if (latestCalled?.Id == notification.ReservationId)
    {
        latestCalled = null;
    }

    await InvokeAsync(StateHasChanged);
});
```

Remove the `using SagraFacile.Domain.Features.Reservations;` import (no longer needed for `ReservationStatus`).

#### `src/SagraFacile.WebClient/Components/Pages/Public/HomeLive.razor`

Replace the `SubscribeReservationStatusChanged` call in `InitializeSignalRAsync` with `SubscribeReservationCalled`. Simplify the handler:

```csharp
reservationSubscription = realtimeService.SubscribeReservationCalled(async notification =>
{
    if (notification.IsActive)
    {
        var entry = new CalledEntry(
            notification.ReservationId,
            notification.SequenceNumber,
            notification.CustomerName,
            notification.PartySize,
            notification.CallCount);

        calledEntries.RemoveAll(existing => existing.Id == entry.Id);
        calledEntries.Insert(0, entry);
        if (calledEntries.Count > 10)
            calledEntries.RemoveRange(10, calledEntries.Count - 10);
    }
    else
    {
        calledEntries.RemoveAll(entry => entry.Id == notification.ReservationId);
    }

    await InvokeAsync(StateHasChanged);
});
```

Remove the `using SagraFacile.Domain.Features.Reservations;` import.

#### `src/SagraFacile.WebClient/Components/Pages/NowCalling.razor`

Replace the `SubscribeReservationStatusChanged` call in `InitializeSignalRAsync` with `SubscribeReservationCalled`. Simplify the handler:

```csharp
reservationSubscription = ReservationRealtimeService.SubscribeReservationCalled(async notification =>
{
    if (notification.IsActive)
    {
        var entry = new CalledEntry(
            notification.ReservationId,
            notification.SequenceNumber,
            notification.CustomerName,
            notification.PartySize,
            notification.CallCount);

        calledEntries.RemoveAll(e => e.Id == notification.ReservationId);
        calledEntries.Insert(0, entry);
        if (calledEntries.Count > MaxEntries)
            calledEntries.RemoveRange(MaxEntries, calledEntries.Count - MaxEntries);
    }
    else
    {
        calledEntries.RemoveAll(e => e.Id == notification.ReservationId);
    }

    await InvokeAsync(StateHasChanged);
});
```

Remove the `using SagraFacile.Domain.Features.Reservations;` import.

### 10. Build & verify

```bash
dotnet build
dotnet test
```
