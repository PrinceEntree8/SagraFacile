# Plan: SignalR Group-Based Routing by Event

## Goal

Replace `Clients.All` broadcasts with targeted SignalR group delivery:
- **`Public`** group — receives only `ReservationCalled` (for public display pages)
- **`event:subscribe#{eventId}`** groups — receive all events scoped to that event (for authenticated staff pages)

Auto-join `Public` on connect. Provide client API to join/leave event groups on login/navigation.

---

## Current State

| Aspect | Current |
|--------|---------|
| Dispatcher | `hubContext.Clients.All` for every message |
| Groups | `JoinReservationGroup`/`LeaveReservationGroup` exist on hub but are never used in production |
| Notifications | No `EventId` on any notification record |
| Client | `ReservationRealtimeService` never joins any group |

---

## Layer-by-Layer Changes

### 1. Contracts — `Notifications.cs`

Add `int EventId` as first parameter to all three notification records:

```csharp
public record ReservationStatusChangedNotification(
    int EventId,
    int ReservationId,
    int SequenceNumber,
    string CustomerName,
    int PartySize,
    ReservationStatus NewStatus,
    ReservationStatus? OldStatus = null,
    int? CallCount = null
);

public record ReservationCalledNotification(
    int EventId,
    int ReservationId,
    bool IsActive,
    int SequenceNumber,
    string CustomerName,
    int PartySize,
    int CallCount
);

public record CountersUpdatedNotification(
    int EventId,
    List<ReservationCounterDto> Counters
);
```

### 2. Application — 8 Command Handlers

All handlers that construct notifications must pass `reservation.EventId` as the first argument.

| Handler | File | `EventId` source |
|---------|------|-------------------|
| `CreateReservation` | `CreateReservation.cs:79,93` | `reservation.EventId` |
| `EditReservation` | `EditReservation.cs:77,91` | `reservation.EventId` |
| `CallReservation` | `CallReservation.cs:86,96,109` | `reservation.EventId` |
| `SeatReservation` | `SeatReservation.cs:65,75,88` | `reservation.EventId` |
| `VoidReservation` | `VoidReservation.cs:52,64` | `reservation.EventId` |
| `MarkPartyComplete` | `MarkPartyComplete.cs:54,68` | `reservation.EventId` |
| `RestoreReservation` | `RestoreReservation.cs:48,62` | `reservation.EventId` |
| `CallAndSeatReservation` | `CallAndSeatReservation.cs:78,90,103` | `reservation.EventId` |

Example diff for `CallReservation.cs`:
```csharp
// Before
notifier.EnqueueStatusChangedAsync(new ReservationStatusChangedNotification(
    reservation.Id, ...

// After
notifier.EnqueueStatusChangedAsync(new ReservationStatusChangedNotification(
    reservation.EventId,
    reservation.Id, ...
```

### 3. Web — `ReservationNotificationDispatcher.cs`

Replace `Clients.All` with group-targeted delivery:

```csharp
case StatusChangedMessage m:
    await hubContext.Clients
        .Group(GroupNameHelper.EventGroup(m.Notification.EventId))
        .ReservationStatusChanged(m.Notification)
        .WaitAsync(stoppingToken);
    break;

case CountersUpdatedMessage m:
    await hubContext.Clients
        .Group(GroupNameHelper.EventGroup(m.Notification.EventId))
        .CountersUpdated(m.Notification.Counters)
        .WaitAsync(stoppingToken);
    break;

case CalledMessage m:
    var calledTask = hubContext.Clients
        .Group(GroupNameHelper.PublicGroup)
        .ReservationCalled(m.Notification)
        .WaitAsync(stoppingToken);
    var eventTask = hubContext.Clients
        .Group(GroupNameHelper.EventGroup(m.Notification.EventId))
        .ReservationCalled(m.Notification)
        .WaitAsync(stoppingToken);
    await Task.WhenAll(calledTask, eventTask);
    break;
```

### 4. Web — New `GroupNameHelper.cs`

Centralize group name format in `src/SagraFacile.Web/Hubs/GroupNameHelper.cs`:

```csharp
public static class GroupNameHelper
{
    public const string PublicGroup = "Public";
    public static string EventGroup(int eventId) => $"event:subscribe#{eventId}";
}
```

### 5. Web — `ReservationHub.cs`

Add `OnConnectedAsync` to auto-join `Public` group. Add typed event group methods with validation:

```csharp
public override async Task OnConnectedAsync()
{
    await Groups.AddToGroupAsync(Context.ConnectionId, GroupNameHelper.PublicGroup);
    await base.OnConnectedAsync();
}

[AllowAnonymous]
public async Task JoinEventGroup(int eventId)
    => await Groups.AddToGroupAsync(Context.ConnectionId, GroupNameHelper.EventGroup(eventId));

[AllowAnonymous]
public async Task LeaveEventGroup(int eventId)
    => await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupNameHelper.EventGroup(eventId));
```

Keep existing `JoinReservationGroup`/`LeaveReservationGroup` for backward compatibility with test scenarios.

### 6. WebClient — `IReservationRealtimeService.cs`

Add event group management methods:

```csharp
Task JoinEventGroupAsync(int eventId);
Task LeaveEventGroupAsync(int eventId);
```

### 7. WebClient — `ReservationRealtimeService.cs`

- Track `_currentEventId` for reconnection recovery
- Implement `JoinEventGroupAsync` / `LeaveEventGroupAsync` via `hubConnection.SendAsync`
- On `Reconnected`: re-join event group if `_currentEventId` is set
- `Public` group is auto-joined server-side on connect, no client action needed

```csharp
private int? _currentEventId;

public async Task JoinEventGroupAsync(int eventId)
{
    await EnsureConnectedAsync();
    await hubConnection.SendAsync("JoinEventGroup", eventId);
    _currentEventId = eventId;
}

public async Task LeaveEventGroupAsync(int eventId)
{
    await hubConnection.SendAsync("LeaveEventGroup", eventId);
    if (_currentEventId == eventId)
        _currentEventId = null;
}
```

Reconnection handler update:
```csharp
hubConnection.Reconnected += _ =>
{
    if (_currentEventId is { } eventId)
        _ = hubConnection.SendAsync("JoinEventGroup", eventId);
    return connectionStateChanged.RaiseAsync(ReservationConnectionState.Connected);
};
```

### 8. WebClient — `NowCalling.razor`

After `LoadInitialCalledReservationsAsync`, join the event group:

```csharp
await LoadInitialCalledReservationsAsync(activeEvent.Id);
await ReservationRealtimeService.JoinEventGroupAsync(activeEvent.Id);
await InitializeSignalRAsync();
```

On dispose, leave the event group:
```csharp
public async ValueTask DisposeAsync()
{
    connectionSubscription?.Dispose();
    reservationSubscription?.Dispose();
    if (activeEvent is not null)
        await ReservationRealtimeService.LeaveEventGroupAsync(activeEvent.Id);
}
```

### 9. Tests

Update test assertions in these files to include `EventId` in expected notifications:

| File | Lines |
|------|-------|
| `CreateReservationHandlerTests.cs` | 52 |
| `CallReservationHandlerTests.cs` | 64, 155 |
| `MarkPartyCompleteHandlerTests.cs` | 68 |
| `VoidReservationHandlerTests.cs` | 38, 93 |
| `RestoreReservationHandlerTests.cs` | 39, 111 |
| `SeatReservationHandlerTests.cs` | 42, 67, 85, 105 |

---

## Broadcast Routing Summary

| Message | Target Groups | Who Receives |
|---------|---------------|--------------|
| `ReservationCalled` | `Public` + `event:subscribe#{eventId}` | Public displays + authenticated staff |
| `ReservationStatusChanged` | `event:subscribe#{eventId}` | Authenticated staff only |
| `CountersUpdated` | `event:subscribe#{eventId}` | Authenticated staff only |

## Group Membership Lifecycle

| Event | Action |
|-------|--------|
| SignalR connect | Auto-join `Public` (server-side `OnConnectedAsync`) |
| Navigate to `NowCalling` | `JoinEventGroupAsync(eventId)` |
| Navigate away from `NowCalling` | `LeaveEventGroupAsync(eventId)` |
| SignalR reconnect | Re-join `Public` (auto) + re-join event group if `_currentEventId` set |
| Login | No automatic group change — group is joined when navigating to event-scoped page |

---

## Files Changed (14 total)

| # | File | Change |
|---|------|--------|
| 1 | `src/SagraFacile.Contracts/Reservations/Notifications.cs` | Add `EventId` to all 3 records |
| 2 | `src/SagraFacile.Application/Features/Reservations/CreateReservation.cs` | Pass `EventId` |
| 3 | `src/SagraFacile.Application/Features/Reservations/EditReservation.cs` | Pass `EventId` |
| 4 | `src/SagraFacile.Application/Features/Reservations/CallReservation.cs` | Pass `EventId` |
| 5 | `src/SagraFacile.Application/Features/Reservations/SeatReservation.cs` | Pass `EventId` |
| 6 | `src/SagraFacile.Application/Features/Reservations/VoidReservation.cs` | Pass `EventId` |
| 7 | `src/SagraFacile.Application/Features/Reservations/MarkPartyComplete.cs` | Pass `EventId` |
| 8 | `src/SagraFacile.Application/Features/Reservations/RestoreReservation.cs` | Pass `EventId` |
| 9 | `src/SagraFacile.Application/Features/Reservations/CallAndSeatReservation.cs` | Pass `EventId` |
| 10 | `src/SagraFacile.Web/Hubs/GroupNameHelper.cs` | **New file** |
| 11 | `src/SagraFacile.Web/Hubs/ReservationHub.cs` | `OnConnectedAsync` + `JoinEventGroup`/`LeaveEventGroup` |
| 12 | `src/SagraFacile.Web/Hubs/ReservationNotificationDispatcher.cs` | Group-targeted delivery |
| 13 | `src/SagraFacile.WebClient/Services/IReservationRealtimeService.cs` | Add `JoinEventGroupAsync`/`LeaveEventGroupAsync` |
| 14 | `src/SagraFacile.WebClient/Services/ReservationRealtimeService.cs` | Implement group methods + reconnect |
| 15 | `src/SagraFacile.WebClient/Components/Pages/NowCalling.razor` | Join/leave event group |
| 16 | `tests/SagraFacile.Application.Tests/Features/Reservations/*HandlerTests.cs` | Update assertions (6 files) |
