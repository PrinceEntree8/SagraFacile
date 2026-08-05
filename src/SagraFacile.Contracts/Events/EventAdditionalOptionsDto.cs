using SagraFacile.Domain.Features.Orders;

namespace SagraFacile.Contracts.Events;

public record EventAdditionalOptionsDto(
    bool IsPartyCompletionEnabled,
    int MinPartySize,
    bool ShowNotesField,
    bool CounterPeopleFirst,
    bool ShowCallCount,
    int MaxWaitTimeMinutes,
    IReadOnlyCollection<OrderContext> EnabledContexts,
    bool CoverChargeEnabled,
    int DefaultCoverChargeInCents,
    OrderActor DefaultConfirmerRole,
    bool AllowEditAfterConfirmation,
    bool AllowFollowUpOrders);