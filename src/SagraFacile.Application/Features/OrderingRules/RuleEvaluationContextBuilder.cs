using SagraFacile.Application.Interfaces;

namespace SagraFacile.Application.Features.OrderingRules;

public sealed class RuleEvaluationContext
{
    public int Covers { get; init; }
    public int TotalItems { get; init; }
    public Dictionary<string, int> CategoryQuantities { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> ItemQuantities { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> PresentCategoryCodes { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> PresentItemCodes { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public interface IRuleEvaluationContextBuilder
{
    Task<RuleEvaluationContext> BuildAsync(OrderValidationInput input, CancellationToken ct = default);
}

public sealed class RuleEvaluationContextBuilder(IMenuRepository menuRepository) : IRuleEvaluationContextBuilder
{
    public async Task<RuleEvaluationContext> BuildAsync(OrderValidationInput input, CancellationToken ct = default)
    {
        var menuItems = await menuRepository.GetByEventIdAsync(input.EventId, includeUnavailable: true, ct);
        var itemMap = menuItems.ToDictionary(i => i.Id);

        var itemQty = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var catQty = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in input.Lines)
        {
            if (line.Quantity <= 0) continue;
            if (!itemMap.TryGetValue(line.MenuItemId, out var item)) continue;

            if (!string.IsNullOrWhiteSpace(item.Code))
            {
                itemQty[item.Code] = itemQty.GetValueOrDefault(item.Code) + line.Quantity;
            }

            if (item.Category is not null && !string.IsNullOrWhiteSpace(item.Category.Code))
            {
                catQty[item.Category.Code] = catQty.GetValueOrDefault(item.Category.Code) + line.Quantity;
            }
        }

        return new RuleEvaluationContext
        {
            Covers = input.Covers,
            TotalItems = input.Lines.Where(l => l.Quantity > 0).Sum(l => l.Quantity),
            CategoryQuantities = catQty,
            ItemQuantities = itemQty,
            PresentCategoryCodes = catQty.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase),
            PresentItemCodes = itemQty.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase)
        };
    }
}
