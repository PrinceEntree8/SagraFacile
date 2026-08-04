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
    public IReadOnlyCollection<OrderContext> EnabledContexts { get; init; } =
        [OrderContext.Table, OrderContext.Reservation, OrderContext.Takeaway];
    public bool CoverChargeEnabled { get; init; } = false;
    public int DefaultCoverChargeInCents { get; init; } = 0;
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
