# Plan: ServiceButton – Context-Aware Reservation Action Button

## Context

The Receptionist page currently has duplicate inline button logic inside the reservation chip loop: a conditional that renders either a "Mark Party Complete" or "Call" button, hardcoded with Bootstrap classes and localization keys. The `ViewReservationModal` is read-only with no action buttons. This plan extracts the next-action button into a reusable `ServiceButton` component that computes its own label, icon, and color from the current reservation status and the event-level `PartyCompletionEnabled` flag, then replaces the inline buttons in `Receptionist.razor` and wires the component into `ViewReservationModal`.

---

## Status Flow Reference

| PartyCompletionEnabled | Allowed transitions |
|---|---|
| `true`  | `Waiting → PartyCompleted → Called → Seated` |
| `false` | `Waiting → Called → Seated` |

Seating is always performed by HeadWaiter. The ServiceButton covers only the Receptionist-side transitions.

### Button appearance by state

| Current Status | PartyCompletion | Next Action | Bootstrap class | Icon | Callback fired |
|---|---|---|---|---|---|
| `Waiting` | `true` | Mark Party Complete | `btn-warning` (orange) | `bi-person-arms-up` | `OnMarkPartyComplete` |
| `Waiting` | `false` | Call | `btn-primary` (blue) | `bi-megaphone` | `OnCall` |
| `PartyCompleted` | — | Call | `btn-primary` | `bi-megaphone` | `OnCall` |
| `Called` | — | Call (re-call) | `btn-primary` | `bi-megaphone` | `OnCall` |
| `Seated` / `Voided` | — | *(nothing rendered)* | — | — | — |

Colors map to the CSS variables in `app.css` (`reservation-status-partycomplete` → orange → `btn-warning`; `reservation-status-called` → blue → `btn-primary`).

---

## Approach

A single Blazor component (`ServiceButton.razor`) computes the next action from `Reservation.Status + PartyCompletionEnabled` and exposes two optional `EventCallback` parameters (`OnMarkPartyComplete`, `OnCall`). The parent page owns service calls and passes lambdas. This keeps the component stateless and free of service injection. The button renders nothing for terminal statuses (`Seated`, `Voided`), making it safe to drop anywhere without extra guards.

---

## File Changes

### 1. Implement `ServiceButton.razor`
**Modify** `src/SagraFacile.WebClient/Components/Buttons/ServiceButton.razor`  
Replace the stub with the full component.

**Parameters:**
```csharp
[Parameter] public required ReservationDto Reservation { get; set; }
[Parameter] public bool PartyCompletionEnabled { get; set; }
[Parameter] public EventCallback OnMarkPartyComplete { get; set; }
[Parameter] public EventCallback OnCall { get; set; }
[Parameter] public bool IsDisabled { get; set; }
```

**Computed properties (in @code block):**
- `_isPartyCompleteAction` → `PartyCompletionEnabled && Reservation.Status == "Waiting"`
- `_isVisible` → `Reservation.Status is "Waiting" or "PartyCompleted" or "Called"`
- `_btnClass` → `"btn-warning"` when `_isPartyCompleteAction`, else `"btn-primary"`
- `_icon` → `"bi-person-arms-up"` when `_isPartyCompleteAction`, else `"bi-megaphone"`
- `_labelKey` → `"Receptionist_Button_PartyComplete"` when `_isPartyCompleteAction`, else `"Receptionist_ActionCall"`
- `_callback` → `OnMarkPartyComplete` when `_isPartyCompleteAction`, else `OnCall`

**Template:**
```razor
@inject IStringLocalizer<SharedResource> L

@if (_isVisible)
{
    <button class="btn btn-sm @_btnClass"
            @onclick="_callback"
            disabled="@IsDisabled">
        <i class="bi @_icon"></i>
        <span class="d-none d-xl-inline">@L[_labelKey]</span>
    </button>
}
```

Required `@using`:
```razor
@using SagraFacile.Contracts.Reservations
@using SagraFacile.WebClient.Resources
```

---

### 2. Update `Receptionist.razor`
**Modify** `src/SagraFacile.WebClient/Components/Pages/Receptionist.razor`

**Add using at top:**
```razor
@using SagraFacile.WebClient.Components.Buttons
```

**Replace** the inline button block at lines 132–144 (the `@if (partyCompletionEnabled && ...)` / `else` block that renders Party Complete or Call buttons) with:

```razor
<ServiceButton Reservation="reservation"
               PartyCompletionEnabled="partyCompletionEnabled"
               OnMarkPartyComplete="@(() => MarkPartyCompleteAsync(reservation.Id))"
               OnCall="@(() => CallReservationAsync(reservation.Id))"
               IsDisabled="@(actionInProgress == reservation.Id)" />
```

The outer `@if (reservation.Status is "Waiting" or "Called" or "PartyCompleted")` guard and the Edit button remain unchanged. Only the two status-action buttons are replaced.

---

### 3. Update `ViewReservationModal.razor`
**Modify** `src/SagraFacile.WebClient/Components/Modals/ViewReservationModal.razor`

**Add using:**
```razor
@using SagraFacile.WebClient.Components.Buttons
```

**Add parameters to `@code`:**
```csharp
[Parameter] public bool PartyCompletionEnabled { get; set; }
[Parameter] public EventCallback OnMarkPartyComplete { get; set; }
[Parameter] public EventCallback OnCall { get; set; }
[Parameter] public bool IsActionDisabled { get; set; }
```

**Add ServiceButton to modal footer** (before the Close button):
```razor
<div class="modal-footer">
    @if (Reservation is not null)
    {
        <ServiceButton Reservation="Reservation"
                       PartyCompletionEnabled="PartyCompletionEnabled"
                       OnMarkPartyComplete="OnMarkPartyComplete"
                       OnCall="OnCall"
                       IsDisabled="IsActionDisabled" />
    }
    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">@L["Common_Close"]</button>
</div>
```

When the parent passes no callbacks (defaults to `EventCallback.Empty`), the ServiceButton still renders the button visually (no-op on click). If the parent intentionally passes null/empty callbacks, that is acceptable since the modal is also used read-only.

---

### 4. Update `ReservationOverview.razor`
**Modify** `src/SagraFacile.WebClient/Components/Pages/ReservationOverview.razor`

**Add fields in `@code`:**
```csharp
private bool partyCompletionEnabled;
private bool isActionInProgress;
```

**Load event options in `OnInitializedAsync`** (after `GetActiveEventAsync`, before `GetReservationsAsync`):
```csharp
if (activeEvent is not null)
{
    var options = await EventService.GetEventOptionsAsync(activeEvent.Id);
    if (options is not null)
        partyCompletionEnabled = options.IsPartyCompletionEnabled;
    // ... existing reservation load
}
```

`EventService.GetEventOptionsAsync` already exists at `src/SagraFacile.WebClient/Services/IEventService.cs` and is used identically in `Receptionist.razor:243`.

**Add action methods:**
```csharp
private async Task CallReservationAsync(int id)
{
    isActionInProgress = true;
    try { await ReservationService.CallAsync(id, new CallReservationRequest("Receptionist")); }
    finally { isActionInProgress = false; }
}

private async Task MarkPartyCompleteAsync(int id)
{
    isActionInProgress = true;
    try { await ReservationService.MarkPartyCompleteAsync(id); }
    finally { isActionInProgress = false; }
}
```

**Update `ViewReservationModal` usage** (line 50):
```razor
<ViewReservationModal Reservation="selectedReservation"
                      PartyCompletionEnabled="partyCompletionEnabled"
                      OnCall="@(selectedReservation is not null ? () => CallReservationAsync(selectedReservation.Id) : EventCallback.Empty)"
                      OnMarkPartyComplete="@(selectedReservation is not null ? () => MarkPartyCompleteAsync(selectedReservation.Id) : EventCallback.Empty)"
                      IsActionDisabled="isActionInProgress" />
```

---

## Localization

No new strings are required. All keys already exist in `SharedResource.resx` (and `.it.resx`):
- `Receptionist_Button_PartyComplete` → "Party Complete"
- `Receptionist_ActionCall` → "Call"

---

## Acceptance Criteria

1. In Receptionist, when `PartyCompletionEnabled = false`: each `Waiting` chip shows one blue "Call" button; `Called` chips show a blue "Call" (re-call) button; no orange button appears.
2. In Receptionist, when `PartyCompletionEnabled = true`: `Waiting` chips show an orange "Party Complete" button; `PartyCompleted` and `Called` chips show a blue "Call" button.
3. `Seated` and `Voided` chips render no ServiceButton (no change from existing behavior).
4. Clicking "Party Complete" in either page calls `ReservationService.MarkPartyCompleteAsync`.
5. Clicking "Call" in either page calls `ReservationService.CallAsync`.
6. Button is disabled while an action is in progress (`actionInProgress` in Receptionist, `isActionInProgress` in ReservationOverview).
7. `ViewReservationModal` shows the ServiceButton in its footer when opened from ReservationOverview, with identical behavior to the Receptionist chip.
8. The inline `if/else` button block in Receptionist (lines 132–144) is fully removed; no duplication with ServiceButton logic remains.
9. No hardcoded UI strings in ServiceButton — all text goes through `IStringLocalizer<SharedResource>`.

---

## Verification Steps

1. Run `dotnet build` — zero errors.
2. Start the app (`cd src/SagraFacile.Web && dotnet run`).
3. Open Receptionist page with an event that has PartyCompletion **disabled**: confirm Waiting reservations show a blue "Call" button only.
4. Enable PartyCompletion in Event Additional Options: confirm Waiting reservations now show an orange "Party Complete" button; after marking complete, the chip shows blue "Call".
5. Open ReservationOverview, click a chip to open ViewReservationModal — confirm the ServiceButton appears in the modal footer and fires the correct action.
6. Confirm disabled state: trigger an action and observe the button becomes unclickable while in progress.

---

## Risks & Mitigations

- **`EventCallback.Empty` as default**: Blazor renders the button even if no callback is wired. For contexts where the modal is truly read-only, callers should not be forced to pass a handler. The current plan wires full handlers in ReservationOverview, which is acceptable since Admins and Supervisors (the only roles with access) should be able to take actions. If a read-only modal is needed in future, a separate `bool ShowActions` parameter can gate rendering.
- **`selectedReservation` captured in lambda**: In ReservationOverview, the lambda captures `selectedReservation` by reference. Since `SelectReservation` assigns a new object before `StateHasChanged`, the captured value is always the reservation that was clicked. This is safe.
- **Re-call on "Called" status**: Existing behavior retained; `CallReservationAsync` is already designed to handle re-calls (it creates a new `ReservationCall` audit record).
