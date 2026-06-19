using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;
using SagraFacile.Contracts.Menu;

namespace SagraFacile.Application.Features.Menu;

public static class GetEventMenu
{
    public record Query(int EventId, bool IncludeUnavailable = false) : IQuery<Result>;
    public record Result(List<MenuItemDto> Items);

    public class Handler(IMenuRepository repo, IMenuCacheService cache) : IQueryHandler<Query, Result>
    {
        public async Task<Result> Handle(Query query, CancellationToken ct)
        {
            if (!query.IncludeUnavailable && cache.TryGetMenuItems(query.EventId, out var cached)) return cached!;
            var items = await repo.GetByEventIdAsync(query.EventId, query.IncludeUnavailable, ct);
            var dtos = items.Select(i => new MenuItemDto(
                i.Id, i.Name, i.Description, i.PriceInCents,
                i.CategoryId,
                i.Category?.Name ?? string.Empty,
                i.DisplayOrder, i.IsAvailable,
                i.MenuItemAllergens.Select(mia => new AllergenDto(
                    mia.Allergen.Id, mia.Allergen.Code, mia.Allergen.Icon)).ToList()
            )).ToList();
            var result = new Result(dtos);
            if (!query.IncludeUnavailable) cache.SetMenuItems(query.EventId, result);
            return result;
        }
    }
}
