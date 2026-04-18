using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;

namespace SagraFacile.Application.Features.Menu;

public static class GetMenuCategories
{
    public record Query() : IQuery<Result>;
    public record Result(List<CategoryDto> Categories);
    public record CategoryDto(int Id, string Name, string NameIt, int DisplayOrder);

    public class Handler : IQueryHandler<Query, Result>
    {
        private readonly IMenuCategoryRepository _repo;

        public Handler(IMenuCategoryRepository repo) => _repo = repo;

        public async Task<Result> Handle(Query query, CancellationToken ct)
        {
            var cats = await _repo.GetAllAsync(ct);
            return new Result(cats.Select(c => new CategoryDto(c.Id, c.Name, c.NameIt, c.DisplayOrder)).ToList());
        }
    }
}
