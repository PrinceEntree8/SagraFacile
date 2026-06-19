namespace SagraFacile.Contracts.Events;

public record EventDto(
    int Id,
    string Name,
    string Description,
    DateTime Date,
    string Currency,
    string CurrencySymbol,
    bool IsActive,
    DateTime CreatedAt);