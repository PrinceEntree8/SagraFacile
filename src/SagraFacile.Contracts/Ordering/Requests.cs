using SagraFacile.Domain.Features.Orders;

namespace SagraFacile.Contracts.Ordering;

public record CreateOrderingRuleRequest(
    OrderingRuleScope Scope,
    string? TargetCode,
    string Expression,
    string? ErrorMessage,
    string? WarningMessage,
    int Priority = 0,
    bool IsActive = true);

public record UpdateOrderingRuleRequest(
    OrderingRuleScope Scope,
    string? TargetCode,
    string Expression,
    string? ErrorMessage,
    string? WarningMessage,
    int Priority = 0);

public record SetOrderingRuleActiveRequest(bool IsActive);

public record OrderingRuleActionResponse(bool Success, string Message);
