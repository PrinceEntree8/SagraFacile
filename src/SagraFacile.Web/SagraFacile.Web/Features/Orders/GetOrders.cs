using MediatR;
using Microsoft.EntityFrameworkCore;
using SagraFacile.Web.Data;

namespace SagraFacile.Web.Features.Orders;

public static class GetOrders
{
    public record Query(int Page = 1, int PageSize = 10) : IRequest<Result>;

    public record Result(List<OrderDto> Orders, int TotalCount);

    public record OrderDto(int Id, string OrderNumber, string CustomerName, string Status, decimal TotalAmount, DateTime CreatedAt);

    public class Handler : IRequestHandler<Query, Result>
    {
        private readonly ApplicationDbContext _context;

        public Handler(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Result> Handle(Query request, CancellationToken cancellationToken)
        {
            var totalCount = await _context.Orders.CountAsync(cancellationToken);

            var orders = await _context.Orders
                .OrderByDescending(o => o.CreatedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
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
}
