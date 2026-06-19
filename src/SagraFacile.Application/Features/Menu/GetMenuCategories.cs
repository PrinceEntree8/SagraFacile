using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;
using SagraFacile.Contracts.Menu;

namespace SagraFacile.Application.Features.Menu;

public static class GetMenuCategories
{
    public record Query() : IQuery<Result>;
    public record Result(List<MenuCategoryDto> Categories);

    public class Handler : IQueryHandler<Query, Result>
    {
        private readonly IMenuCategoryRepository _repo;

        public Handler(IMenuCategoryRepository repo) => _repo = repo;

        public async Task<Result> Handle(Query query, CancellationToken ct)
        {
            var cats = await _repo.GetAllAsync(ct);
            return new Result(cats.Select(c => new MenuCategoryDto(c.Id, c.Name, c.DisplayOrder, [])).ToList());
        }
    }
}
