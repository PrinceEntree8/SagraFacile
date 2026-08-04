using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;

namespace SagraFacile.Application.Features.OrderingRules;

public static class ValidateOrderComposition
{
    public record Query(
        int EventId,
        int Covers,
        IReadOnlyCollection<OrderValidationLine> Lines) : IQuery<OrderValidationResult>;

    public class Handler(IOrderingRulesService rulesService) : IQueryHandler<Query, OrderValidationResult>
    {
        public async Task<OrderValidationResult> Handle(Query query, CancellationToken ct)
        {
            var input = new OrderValidationInput(query.EventId, query.Covers, query.Lines);
            return await rulesService.ValidateOrderAsync(input, ct);
        }
    }
}
