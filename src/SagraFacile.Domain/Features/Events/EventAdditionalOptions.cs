namespace SagraFacile.Domain.Features.Events;

public class EventAdditionalOptions
{
    public ReservationOptions Reservations { get; init; } = new();
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
