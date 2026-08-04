using SagraFacile.Domain.Features.Orders;

namespace SagraFacile.Application.Interfaces;

public record OrderValidationLine(int MenuItemId, int Quantity);

public record OrderValidationInput(
    int EventId,
    int Covers,
    IReadOnlyCollection<OrderValidationLine> Lines);

public record RuleViolation(
    int RuleId,
    OrderingRuleScope Scope,
    string? TargetCode,
    string Expression,
    string Message,
    bool IsBlocking);

public record OrderValidationResult(
    bool IsValid,
    IReadOnlyCollection<RuleViolation> Errors,
    IReadOnlyCollection<RuleViolation> Warnings);

public interface IOrderingRulesService
{
    Task<OrderValidationResult> ValidateOrderAsync(OrderValidationInput input, CancellationToken ct = default);
}
