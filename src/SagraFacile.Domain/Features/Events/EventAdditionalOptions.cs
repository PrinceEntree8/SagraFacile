using SagraFacile.Domain.Features.Orders;

namespace SagraFacile.Domain.Features.Events;

public class EventAdditionalOptions
{
    public ReservationOptions Reservations { get; init; } = new();
    public ViewOptions View { get; init; } = new();
    public OrderOptions Orders { get; init; } = new();
}

public class OrderOptions
{
    public IReadOnlyCollection<OrderContext> EnabledContexts { get; set; } =
        [OrderContext.Table, OrderContext.Reservation, OrderContext.Takeaway];
    public bool CoverChargeEnabled { get; set; } = false;
    public int DefaultCoverChargeInCents { get; set; } = 0;
    public OrderLifecycleOptions Lifecycle { get; set; } = new();
}

public class OrderLifecycleOptions
{
    public OrderActor DefaultConfirmerRole { get; set; } = OrderActor.Cashier;
    public bool AllowEditAfterConfirmation { get; set; }
    public bool AutoConfirmPreorders { get; set; }
    public bool AllowFollowUpOrders { get; set; } = true;
    public bool RequireReasonOnRejection { get; set; } = true;
    public bool RequireReasonOnCancellation { get; set; }
    public List<OrderTransitionOverride> TransitionOverrides { get; set; } = [];
}

public class OrderTransitionOverride
{
    public OrderStatus From { get; set; }
    public OrderStatus To { get; set; }
    public bool Enabled { get; set; } = true;
    public List<OrderActor>? Actors { get; set; }
}

public class ReservationOptions
{
    public PartyCompletionOptions PartyCompletion { get; init; } = new();
}

public class PartyCompletionOptions
{
    public bool Enabled { get; init; } = false;
    public int MinPartySize { get; init; } = 8;
}

public class ViewOptions
{
    public bool ShowNotesField { get; init; } = false;
    public bool CounterPeopleFirst { get; init; } = true;
    public bool ShowCallCount { get; init; } = false;
    public int MaxWaitTimeMinutes { get; init; } = 45;
}
