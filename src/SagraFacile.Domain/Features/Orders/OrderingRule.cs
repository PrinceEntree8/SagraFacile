using SagraFacile.Domain.Common;

namespace SagraFacile.Domain.Features.Orders;

public class OrderingRule
{
    public int Id { get; private set; }
    public int EventId { get; private set; }
    public OrderingRuleScope Scope { get; private set; }
    public string? TargetCode { get; private set; }
    public string Expression { get; private set; } = string.Empty;
    public string? ErrorMessage { get; private set; }
    public string? WarningMessage { get; private set; }
    public bool IsActive { get; private set; }
    public int Priority { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; private set; }

    public bool IsBlocking => ErrorMessage is not null;
    public string Message => ErrorMessage ?? WarningMessage!;

    private OrderingRule() { }

    public static OrderingRule Create(int eventId, OrderingRuleScope scope, string? targetCode,
        string expression, string? errorMessage, string? warningMessage, int priority, bool isActive)
    {
        if (eventId <= 0)
            throw new DomainRuleViolationException("EventId must be positive.");

        var rule = new OrderingRule
        {
            EventId = eventId,
            CreatedAt = DateTime.UtcNow
        };
        rule.Update(scope, targetCode, expression, errorMessage, warningMessage, priority);
        rule.IsActive = isActive;
        return rule;
    }

    public void Update(OrderingRuleScope scope, string? targetCode, string expression,
        string? errorMessage, string? warningMessage, int priority)
    {
        if (string.IsNullOrWhiteSpace(expression))
            throw new DomainRuleViolationException("Expression is required.");
        if (scope is OrderingRuleScope.Category or OrderingRuleScope.Item && string.IsNullOrWhiteSpace(targetCode))
            throw new DomainRuleViolationException("TargetCode is required for Category/Item scope.");
        if (scope == OrderingRuleScope.Order && !string.IsNullOrWhiteSpace(targetCode))
            throw new DomainRuleViolationException("TargetCode must be empty for Order scope.");
        if (string.IsNullOrWhiteSpace(errorMessage) && string.IsNullOrWhiteSpace(warningMessage))
            throw new DomainRuleViolationException("Either ErrorMessage or WarningMessage is required.");
        if (priority < 0)
            throw new DomainRuleViolationException("Priority must be >= 0.");

        Scope = scope;
        TargetCode = string.IsNullOrWhiteSpace(targetCode) ? null : targetCode.Trim().ToLowerInvariant();
        Expression = expression.Trim();
        ErrorMessage = string.IsNullOrWhiteSpace(errorMessage) ? null : errorMessage.Trim();
        WarningMessage = string.IsNullOrWhiteSpace(warningMessage) ? null : warningMessage.Trim();
        Priority = priority;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }
}
