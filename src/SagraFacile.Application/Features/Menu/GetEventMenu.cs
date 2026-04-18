using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;

namespace SagraFacile.Application.Features.Menu;

public static class GetEventMenu
{
    public record Query(int EventId, bool IncludeUnavailable = false) : IQuery<Result>;
    public record Result(List<MenuItemDto> Items);
    public record MenuItemDto(
        int Id,
        int EventId,
        string Name,
        string Description,
        int PriceInCents,
        int CategoryId,
        string CategoryName,
        int DisplayOrder,
        bool IsAvailable,
        List<AllergenDto> Allergens);
    public record AllergenDto(int Id, string Code, string Icon);

    public class Handler : IQueryHandler<Query, Result>
    {
        private readonly IMenuRepository _repo;

        public Handler(IMenuRepository repo) => _repo = repo;

        public async Task<Result> Handle(Query query, CancellationToken ct)
        {
            var items = await _repo.GetByEventIdAsync(query.EventId, query.IncludeUnavailable, ct);
            var dtos = items.Select(i => new MenuItemDto(
                i.Id, i.EventId, i.Name, i.Description, i.PriceInCents,
                i.CategoryId,
                i.Category?.Name ?? string.Empty,
                i.DisplayOrder, i.IsAvailable,
                i.MenuItemAllergens.Select(mia => new AllergenDto(
                    mia.Allergen.Id, mia.Allergen.Code, mia.Allergen.Icon)).ToList()
            )).ToList();
            return new Result(dtos);
        }
    }
}
