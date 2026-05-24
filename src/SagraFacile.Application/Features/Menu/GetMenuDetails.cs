using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;

namespace SagraFacile.Application.Features.Menu;

public static class GetMenuDetails
{
    public record Query(int EventId) : IQuery<Result>;
    public record Result(MenuDetailsDto? Details);
    public record MenuDetailsDto(int EventId, string? WarningMessage, string? Header, string? Footer);

    public class Handler(IMenuDetailsRepository repo, IMenuCacheService cache) : IQueryHandler<Query, Result>
    {
        public async Task<Result> Handle(Query query, CancellationToken ct)
        {
            if (cache.TryGetMenuDetails(query.EventId, out var cached)) return cached!;
            var entity = await repo.GetByEventIdAsync(query.EventId, ct);
            var result = entity is null
                ? new Result(null)
                : new Result(new MenuDetailsDto(entity.EventId, entity.WarningMessage, entity.Header, entity.Footer));
            cache.SetMenuDetails(query.EventId, result);
            return result;
        }
    }
}
