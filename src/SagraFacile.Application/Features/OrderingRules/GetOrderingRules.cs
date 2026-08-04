using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;
using SagraFacile.Contracts.Ordering;

namespace SagraFacile.Application.Features.OrderingRules;

public static class GetOrderingRules
{
    public record Query(int EventId) : IQuery<Result>;
    public record Result(IReadOnlyList<OrderingRuleDto> Rules);

    public class Handler(IOrderingRuleRepository repository) : IQueryHandler<Query, Result>
    {
        public async Task<Result> Handle(Query query, CancellationToken ct)
        {
            var rules = await repository.GetByEventAsync(query.EventId, ct);
            var dtos = rules.Select(r => new OrderingRuleDto(
                r.Id,
                r.EventId,
                r.Scope.ToString(),
                r.TargetCode,
                r.Expression,
                r.ErrorMessage,
                r.WarningMessage,
                r.IsActive,
                r.Priority,
                r.IsBlocking,
                r.Message)).ToList();

            return new Result(dtos);
        }
    }
}
