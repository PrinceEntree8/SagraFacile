namespace SagraFacile.Domain.Features.Events;

public class EventAdditionalOptions
{
    public ReservationOptions Reservations { get; init; } = new();
    public ViewOptions View { get; init; } = new();
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
