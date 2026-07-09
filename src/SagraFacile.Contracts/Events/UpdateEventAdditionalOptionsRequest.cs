namespace SagraFacile.Contracts.Events;

public record UpdateEventAdditionalOptionsRequest(
    bool IsPartyCompletionEnabled,
    int MinPartySize,
    bool ShowNotesField,
    bool CounterPeopleFirst,
    bool ShowCallCount,
    int MaxWaitTimeMinutes);