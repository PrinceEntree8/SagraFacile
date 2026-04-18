using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Menu;

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
        decimal Price,
        MenuCategory Category,
        int DisplayOrder,
        bool IsAvailable,
        List<AllergenDto> Allergens);
    public record AllergenDto(int Id, string Code, string Name, string NameIt);

    public class Handler : IQueryHandler<Query, Result>
    {
        private readonly IMenuRepository _repo;

        public Handler(IMenuRepository repo) => _repo = repo;

        public async Task<Result> Handle(Query query, CancellationToken ct)
        {
            var items = await _repo.GetByEventIdAsync(query.EventId, query.IncludeUnavailable, ct);
            var dtos = items.Select(i => new MenuItemDto(
                i.Id, i.EventId, i.Name, i.Description, i.Price, i.Category, i.DisplayOrder, i.IsAvailable,
                i.MenuItemAllergens.Select(mia => new AllergenDto(
                    mia.Allergen.Id, mia.Allergen.Code, mia.Allergen.Name, mia.Allergen.NameIt)).ToList()
            )).ToList();
            return new Result(dtos);
        }
    }
}
