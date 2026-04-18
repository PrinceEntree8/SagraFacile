using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;

namespace SagraFacile.Application.Features.Menu;

public static class GetAllergens
{
    public record Query() : IQuery<Result>;
    public record Result(List<AllergenDto> Allergens);
    public record AllergenDto(int Id, string Code, string Icon);

    public class Handler : IQueryHandler<Query, Result>
    {
        private readonly IAllergenRepository _repo;

        public Handler(IAllergenRepository repo) => _repo = repo;

        public async Task<Result> Handle(Query query, CancellationToken ct)
        {
            var allergens = await _repo.GetAllAsync(ct);
            return new Result(allergens.Select(a => new AllergenDto(a.Id, a.Code, a.Icon)).ToList());
        }
    }
}
