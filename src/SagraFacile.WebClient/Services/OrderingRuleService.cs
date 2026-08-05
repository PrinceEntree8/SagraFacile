using System.Net.Http.Json;
using SagraFacile.Contracts.Ordering;

namespace SagraFacile.WebClient.Services;

public class OrderingRuleService(HttpClient httpClient) : IOrderingRuleService
{
    public async Task<IReadOnlyList<OrderingRuleDto>> GetRulesAsync(int eventId, CancellationToken ct = default)
        => await httpClient.GetFromJsonAsync<List<OrderingRuleDto>>($"api/events/{eventId}/ordering-rules", ct) ?? [];

    public async Task<OrderingRuleActionResponse?> CreateRuleAsync(int eventId, CreateOrderingRuleRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync($"api/events/{eventId}/ordering-rules", request, ct);
        return await response.Content.ReadFromJsonAsync<OrderingRuleActionResponse>(cancellationToken: ct);
    }

    public async Task<OrderingRuleActionResponse?> UpdateRuleAsync(int eventId, int ruleId, UpdateOrderingRuleRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PutAsJsonAsync($"api/events/{eventId}/ordering-rules/{ruleId}", request, ct);
        return await response.Content.ReadFromJsonAsync<OrderingRuleActionResponse>(cancellationToken: ct);
    }

    public async Task<OrderingRuleActionResponse?> DeleteRuleAsync(int eventId, int ruleId, CancellationToken ct = default)
    {
        var response = await httpClient.DeleteAsync($"api/events/{eventId}/ordering-rules/{ruleId}", ct);
        return await response.Content.ReadFromJsonAsync<OrderingRuleActionResponse>(cancellationToken: ct);
    }

    public async Task<OrderingRuleActionResponse?> SetActiveAsync(int eventId, int ruleId, bool isActive, CancellationToken ct = default)
    {
        var response = await httpClient.PatchAsJsonAsync($"api/events/{eventId}/ordering-rules/{ruleId}/active", new SetOrderingRuleActiveRequest(isActive), ct);
        return await response.Content.ReadFromJsonAsync<OrderingRuleActionResponse>(cancellationToken: ct);
    }
}
