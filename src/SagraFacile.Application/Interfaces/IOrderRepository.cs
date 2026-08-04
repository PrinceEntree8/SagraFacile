using SagraFacile.Domain.Features.Orders;

namespace SagraFacile.Application.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Order?> GetByIdWithLinesAsync(int id, CancellationToken ct = default);
    Task<Order?> GetByIdWithHistoryAsync(int id, CancellationToken ct = default);
    Task<Order?> GetByEventAndNumberAsync(int eventId, int orderNumber, CancellationToken ct = default);
    Task<int> GetNextOrderNumberAsync(int eventId, CancellationToken ct = default);
    Task<(List<Order> Items, int TotalCount)> GetPagedAsync(int eventId, int page, int pageSize,
        IReadOnlyCollection<OrderStatus>? statuses, CancellationToken ct = default);
    Task<List<Order>> GetByStatusAsync(int eventId, OrderStatus status, CancellationToken ct = default);
    Task AddAsync(Order order, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
