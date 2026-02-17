using Microsoft.EntityFrameworkCore;
using SagraFacile.Web.Data;

namespace SagraFacile.Web.Features.Orders;

public static class GetOrders
{
    public record Query(int Page = 1, int PageSize = 10);

    public record Result(List<OrderDto> Orders, int TotalCount);

    public record OrderDto(int Id, string OrderNumber, string CustomerName, string Status, decimal TotalAmount, DateTime CreatedAt);

    public static async Task<Result> Handle(Query query, ApplicationDbContext context, CancellationToken cancellationToken)
    {
        var totalCount = await context.Orders.CountAsync(cancellationToken);

        var orders = await context.Orders
            .OrderByDescending(o => o.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(o => new OrderDto(
                o.Id,
                o.OrderNumber,
                o.CustomerName,
                o.Status,
                o.TotalAmount,
                o.CreatedAt))
            .ToListAsync(cancellationToken);

        return new Result(orders, totalCount);
    }
}
