using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;
using SagraFacile.Contracts.Menu;

namespace SagraFacile.Application.Features.Menu;

public static class GetMenuDetails
{
    public record Query(int EventId) : IQuery<Result>;
    public record Result(MenuDetailsDto? Details);

    public class Handler(IMenuDetailsRepository repo, IMenuCacheService cache) : IQueryHandler<Query, Result>
    {
        public async Task<Result> Handle(Query query, CancellationToken ct)
        {
            if (cache.TryGetMenuDetails(query.EventId, out var cached)) return cached!;
            var entity = await repo.GetByEventIdAsync(query.EventId, ct);
            var result = entity is null
                ? new Result(null)
                : new Result(new MenuDetailsDto(entity.Header, entity.Footer, entity.WarningMessage));
            cache.SetMenuDetails(query.EventId, result);
            return result;
        }
    }
}
