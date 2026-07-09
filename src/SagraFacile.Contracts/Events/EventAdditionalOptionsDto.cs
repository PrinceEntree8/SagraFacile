namespace SagraFacile.Contracts.Events;

public record EventAdditionalOptionsDto(
    bool IsPartyCompletionEnabled,
    int MinPartySize,
    bool ShowNotesField,
    bool CounterPeopleFirst,
    bool ShowCallCount,
    int MaxWaitTimeMinutes);