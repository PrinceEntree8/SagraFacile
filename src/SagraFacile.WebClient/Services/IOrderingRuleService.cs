using SagraFacile.Contracts.Ordering;

namespace SagraFacile.WebClient.Services;

public interface IOrderingRuleService
{
    Task<IReadOnlyList<OrderingRuleDto>> GetRulesAsync(int eventId, CancellationToken ct = default);
    Task<OrderingRuleActionResponse?> CreateRuleAsync(int eventId, CreateOrderingRuleRequest request, CancellationToken ct = default);
    Task<OrderingRuleActionResponse?> UpdateRuleAsync(int eventId, int ruleId, UpdateOrderingRuleRequest request, CancellationToken ct = default);
    Task<OrderingRuleActionResponse?> DeleteRuleAsync(int eventId, int ruleId, CancellationToken ct = default);
    Task<OrderingRuleActionResponse?> SetActiveAsync(int eventId, int ruleId, bool isActive, CancellationToken ct = default);
}
