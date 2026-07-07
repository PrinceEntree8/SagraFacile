# Plan: Restore Voided Reservations (Issue #66)

## Context

Issue #66 ("Consentire il ripristino di una prenotazione annullata") requests that staff can restore a voided/cancelled reservation back into the active queue. Currently, voiding is irreversible from the UI. Two views need the feature: Receptionist page and ReservationOverview (Panoramica) page. The issue asks to restore to the prior state "if possible"; since `Reservation` has no `PreviousStatus` field, the implementation restores to `Waiting` (the safe, operationally meaningful default — staff can still call the party again).

---

## Goal

Add a `RestoreReservation` command that transitions a `Voided` reservation back to `Waiting`, expose it via REST and SignalR, and wire up UI filters + restore buttons in the Receptionist and ReservationOverview pages.

---

## Approach

Follow the exact CQRS vertical-slice pattern already used by `VoidReservation.cs`. No database schema migration is required — only a status update is needed (the existing `Version` concurrency token handles conflicts). The restore notification reuses the existing `IReservationNotifier` pipeline (StatusChanged + CountersUpdated), so real-time updates work automatically across all connected clients.

---

## File Changes

### Application layer
- **Create** `src/SagraFacile.Application/Features/Reservations/RestoreReservation.cs`
  - `Command(int ReservationId)` with FluentValidation (`ReservationId > 0`)
  - Handler: fetch by ID → assert `status == Voided` → set `Status = Waiting`, clear `VoidedAt` → `SaveChangesAsync` → enqueue `StatusChangedAsync` + `CountersUpdatedAsync`
  - Returns `CommandResult`

### Web (server-side)
- **Modify** `src/SagraFacile.Web/Controllers/ReservationController.cs`
  - Add `POST /api/reservations/{id}/restore` endpoint (mirrors the `call` endpoint structure)
- **Modify** `src/SagraFacile.Web/Hubs/ReservationHub.cs`
  - Add `RestoreReservation(int reservationId)` hub method (mirrors `VoidReservation`)

### Client service
- **Modify** `src/SagraFacile.WebClient/Services/ReservationService.cs`
  - Add `RestoreAsync(int id, CancellationToken ct)` calling `POST api/reservations/{id}/restore`

### Blazor components
- **Modify** `src/SagraFacile.WebClient/Components/Buttons/ServiceButton.razor`
  - Add `[Parameter] EventCallback OnRestore`
  - Show restore button (e.g., arrow-counterclockwise icon) when `Status == "Voided"` (currently the component renders nothing for Voided status)

- **Modify** `src/SagraFacile.WebClient/Components/Modals/ViewReservationModal.razor`
  - Add `[Parameter] EventCallback OnRestore` passed down to `ServiceButton`

- **Modify** `src/SagraFacile.WebClient/Components/Pages/Receptionist.razor`
  - Add "Annullate" filter chip (using `ReservationStatusFilter.Voided`)
  - When Voided filter is active, existing chip list renders voided reservations
  - Wire up restore callback: call `ReservationService.RestoreAsync` then reload

- **Modify** `src/SagraFacile.WebClient/Components/Pages/ReservationOverview.razor`
  - Add a toggle ("Mostra annullate") that, when active, loads voided reservations alongside active ones (or as a separate expandable section)
  - Wire up `OnRestore` on the `ViewReservationModal` invoked from this page

### Tests
- **Create** `tests/SagraFacile.Application.Tests/Features/Reservations/RestoreReservationHandlerTests.cs`
  - Test: `Voided → Waiting` succeeds, `VoidedAt` cleared
  - Test: Cannot restore a `Waiting` reservation (wrong status)
  - Test: Cannot restore a `Seated` reservation
  - Test: Concurrency exception is handled and returned as error result

---

## Implementation Steps

### Task 1: Application command
1. Create `RestoreReservation.cs` in `src/SagraFacile.Application/Features/Reservations/`
   - Copy structure from `VoidReservation.cs`
   - Validator: `ReservationId > 0`
   - Handler: fetch → guard `status != Voided` → `reservation.Status = ReservationStatus.Waiting; reservation.VoidedAt = null;` → save → notify

### Task 2: Server exposure
2. In `ReservationController.cs` after the `Void` endpoint (~line 131):
   ```csharp
   [HttpPost("{id:int}/restore")]
   public async Task<IActionResult> Restore(int id, CancellationToken ct)
       => Ok(await mediator.SendAsync(new RestoreReservation.Command(id), ct));
   ```
3. In `ReservationHub.cs` after the `VoidReservation` method (~line 81):
   ```csharp
   [Authorize(Policy = "Cassiere")]
   public async Task<CommandResult> RestoreReservation(int reservationId)
       => await mediator.SendAsync(new RestoreReservation.Command(reservationId));
   ```

### Task 3: Client service
4. In `ReservationService.cs`, add:
   ```csharp
   public async Task<CommandResult> RestoreAsync(int id, CancellationToken ct = default)
   {
       var response = await httpClient.PostAsync($"api/reservations/{id}/restore", null, ct);
       response.EnsureSuccessStatusCode();
       return await response.Content.ReadFromJsonAsync<CommandResult>(cancellationToken: ct)
           ?? throw new InvalidOperationException();
   }
   ```

### Task 4: ServiceButton component
5. Add `[Parameter] EventCallback OnRestore` parameter
6. In the render logic, add an `else if (Status == "Voided")` branch that renders a restore button (localized label "Ripristina") invoking `OnRestore`

### Task 5: ViewReservationModal
7. Add `[Parameter] EventCallback OnRestore`
8. Pass it to `<ServiceButton ... OnRestore="OnRestore" />`

### Task 6: Receptionist page
9. Add "Annullate" filter chip button alongside Waiting/PartyCompleted/Called/All
   - Active filter: `ReservationStatusFilter.Voided`
10. Ensure the reservation chip list renders voided chips correctly
11. Add `RestoreReservation` callback: `await ReservationService.RestoreAsync(id)` → reload reservations

### Task 7: ReservationOverview page
12. Add a toggle button/switch "Mostra annullate" to the page header
13. When toggled on, call `GetReservations` with `ReservationStatusFilter.All` and display voided reservations in a distinct visual section (e.g., greyed-out row or separate card group)
14. Clicking a voided reservation opens `ViewReservationModal` with `OnRestore` wired to the restore service call

### Task 8: Tests
15. Create `RestoreReservationHandlerTests.cs` following the pattern in `VoidReservationHandlerTests.cs`

---

## Acceptance Criteria

1. A receptionist can select an "Annullate" filter in the Receptionist page and see only voided reservations
2. A receptionist can click "Ripristina" on a voided reservation and it immediately moves to `Waiting` status
3. The restored reservation retains its original `SequenceNumber`, `CustomerName`, and `PartySize`; `VoidedAt` is null
4. Real-time updates propagate: all connected clients see the status change via SignalR (`ReservationStatusChanged`)
5. Counters update in real time after restore (`CountersUpdated` notification)
6. From ReservationOverview, staff can toggle voided visibility and restore a reservation
7. Cannot restore a non-Voided reservation (backend returns error result)
8. Concurrency conflict on restore returns an error result without a 500

---

## Verification Steps

```bash
# Run unit tests
dotnet test --filter "FullyQualifiedName~Reservation"

# Start the app
docker compose up -d
cd src/SagraFacile.Web && dotnet run
```

Manual verification:
1. Create a reservation → void it → switch to "Annullate" filter → restore → confirm it reappears in Waiting
2. Open two browser tabs on Receptionist; restore in one → confirm the other updates in real-time
3. From ReservationOverview, toggle "Mostra annullate", verify voided reservations appear, restore one
4. Attempt to restore an already-Waiting reservation via API → expect error response

---

## Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| `VoidedAt` is cleared on restore — if the reservation gets voided again, audit trail loses the first void timestamp | Acceptable: `ReservationCall` history preserves the call audit; a repeated void sets `VoidedAt` again |
| Restoring to `Waiting` may feel wrong if the party was deep in the Called flow | The UI restore button only appears for Voided status; staff controls the queue explicitly after restore |
| `ReservationStatusFilter.Voided` may not be handled by `GetPagedAsync` | Verify `ToStatusArray()` extension includes `Voided = 8`; likely already covered since it's defined in the enum |
