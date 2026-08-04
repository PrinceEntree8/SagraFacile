namespace SagraFacile.Contracts.Ordering;

public record OrderingRuleDto(
    int Id,
    int EventId,
    string Scope,
    string? TargetCode,
    string Expression,
    string? ErrorMessage,
    string? WarningMessage,
    bool IsActive,
    int Priority,
    bool IsBlocking,
    string Message);
