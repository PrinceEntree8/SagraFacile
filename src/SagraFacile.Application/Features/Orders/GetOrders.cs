using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;
using SagraFacile.Contracts.Ordering;
using SagraFacile.Domain.Features.Orders;

namespace SagraFacile.Application.Features.Orders;

public static class GetOrders
{
    public record Result(IReadOnlyCollection<OrderSummaryDto> Items, int TotalCount, int Page, int PageSize);

    public record Query(
        int EventId,
        int Page = 1,
        int PageSize = 20,
        IReadOnlyCollection<OrderStatus>? Statuses = null
    ) : IQuery<Result>;

    public class Handler(IOrderRepository repository) : IQueryHandler<Query, Result>
    {
        public async Task<Result> Handle(Query query, CancellationToken ct)
        {
            var page = query.Page < 1 ? 1 : query.Page;
            var pageSize = query.PageSize is < 1 or > 100 ? 20 : query.PageSize;

            var (items, total) = await repository.GetPagedAsync(query.EventId, page, pageSize, query.Statuses, ct);

            var dtos = items.Select(o => new OrderSummaryDto(
                o.Id, o.EventId, o.OrderNumber, o.Status, o.Context,
                o.ContextLabel, o.Covers, o.TotalInCents, o.CustomerName, o.CreatedAt
            )).ToList();

            return new Result(dtos, total, page, pageSize);
        }
    }
}
