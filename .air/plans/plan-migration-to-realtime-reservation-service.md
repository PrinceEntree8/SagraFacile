### Plan Overview

The plan is divided into three main phases: Backend Service Implementation, Real-time Infrastructure Setup, and Frontend Integration.

---

### Phase 1: Backend Service Refactoring (Introducing Real-Time)

The goal of this phase is to establish the `ReservationRealtimeService` and ensure that reservation changes are broadcasted via SignalR instead of solely relying on standard HTTP requests for updates.

**Step 1.1: Clean Up SignalR Interfaces**
*   **Fix `IReservationHubClient` interface** - Remove all RPC methods (CreateReservation, EditReservation, etc.) from this interface. It should ONLY contain server-to-client notification methods:
    - `ReservationStatusChanged(ReservationStatusChangedNotification notification)`
    - `CountersUpdated(List<ReservationCounterDto> counters)`
    - `AvailableSeatsUpdated(int availableSeats)`
*   **Keep `ReservationHub` methods** - All client-to-server RPC methods (CreateReservation, EditReservation, CallReservation, etc.) remain in the hub class only.

**Step 1.2: Ensure Type-Safe Method Invocations**
*   **Update `ReservationRealtimeService`** to use `nameof` for all SignalR method invocations instead of string literals:
    - Change `hubConnection.InvokeAsync<T>("GetReservations", ...)` to `hubConnection.InvokeAsync<T>(nameof(ReservationHub.GetReservations), ...)`
    - Apply this pattern to all method calls: GetCounters, GetBestFitReservation, CreateReservation, EditReservation, CallReservation, MarkPartyComplete, SeatReservation, CallAndSeatReservation, VoidReservation
    - For client callback registrations, use `nameof(IReservationHubClient.ReservationStatusChanged)`, `nameof(IReservationHubClient.AvailableSeatsUpdated)`, `nameof(IReservationHubClient.CountersUpdated)`

**Step 1.3: Implement Broadcasting from Command Handlers**
*   Modify all reservation command handlers (CreateReservation, EditReservation, CallReservation, etc.) to inject `IHubContext<ReservationHub, IReservationHubClient>` and broadcast notifications:
    - After successful CreateReservation: broadcast `ReservationStatusChanged` and `CountersUpdated`
    - After successful EditReservation: broadcast `ReservationStatusChanged`
    - After successful CallReservation: broadcast `ReservationStatusChanged`
    - After successful SeatReservation: broadcast `ReservationStatusChanged` and `CountersUpdated`
    - After successful VoidReservation: broadcast `ReservationStatusChanged` and `CountersUpdated`
    - After successful MarkPartyComplete: broadcast `ReservationStatusChanged`

**Step 1.4: API Endpoint Review (Optional but Recommended)**
*   Determine if any legacy REST endpoints used by `ReservationService` are still necessary for initial data loading. If they are only for initial fetch, keep them; otherwise, focus on removing the REST dependency for real-time updates.

### Phase 2: Real-Time Infrastructure Setup (SignalR)

This phase focuses on setting up the communication channels required to facilitate real-time data flow.

**Step 2.1: Configure SignalR Hub**
*   Define and configure the SignalR Hub that will handle connections from the frontend clients (`Receptionist`, `HeadWaiter`). This hub will receive updates from the backend and push them to connected clients.

**Step 2.2: Data Flow Verification**
*   Verify that when a reservation status changes in the database, it successfully triggers the broadcast through the service layer (Phase 1) and is received by the SignalR Hub.

### Phase 3: Frontend Integration (Updating Razor Components)

This phase involves modifying the specified frontend components to consume real-time data from SignalR instead of making standard REST calls for every update or polling.

**Step 3.1: Update `Receptionist.razor`**
*   Modify this component to establish a SignalR connection upon loading.
*   Implement logic to subscribe to the specific reservation updates relevant to the Receptionist role, ensuring that the displayed data is updated immediately when changes occur on the backend (via the SignalR hub).

**Step 3.2: Update `HeadWaiter.razor`**
*   Similarly, modify this component to establish a SignalR connection.
*   Implement logic to subscribe to reservation updates relevant to the Head Waiter role, ensuring that their view reflects the latest status in real-time.

---

### Summary of Actions

| Phase | Focus Area | Key Deliverables | Affected Files/Modules |
| :--- | :--- | :--- | :--- |
| **1** | Backend Logic | Clean up `IReservationHubClient` (remove RPC methods), implement broadcasting in all command handlers via `IHubContext<ReservationHub, IReservationHubClient>`, update `ReservationRealtimeService` to use `nameof` for type safety. | `IReservationHubClient.cs`, `ReservationRealtimeService.cs`, all reservation command handlers in `SagraFacile.Application/Features/Reservations/`. |
| **2** | Infrastructure | SignalR Hub verification and configuration, ensure all RPC methods remain in `ReservationHub` only. | `ReservationHub.cs`, SignalR hub registration in `Program.cs`. |
| **3** | Frontend UI | Integration of SignalR client logic into Razor pages to subscribe to real-time notifications (`ReservationStatusChanged`, `CountersUpdated`, `AvailableSeatsUpdated`). | `Receptionist.razor`, `HeadWaiter.razor`, any other components displaying reservation data. |