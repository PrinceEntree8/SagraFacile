using SagraFacile.Domain.Features.Orders;

namespace SagraFacile.Application.Interfaces;

public interface IOrderingRuleRepository
{
    Task<OrderingRule?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<OrderingRule>> GetActiveByEventAsync(int eventId, CancellationToken ct = default);
    Task<IReadOnlyList<OrderingRule>> GetByEventAsync(int eventId, CancellationToken ct = default);
    Task AddAsync(OrderingRule rule, CancellationToken ct = default);
    Task UpdateAsync(OrderingRule rule, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
