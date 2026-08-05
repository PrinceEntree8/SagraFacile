using Microsoft.EntityFrameworkCore;
using Npgsql;
using SagraFacile.Application.Exceptions;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Orders;
using SagraFacile.Infrastructure.Data;

namespace SagraFacile.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository, IAsyncDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly IDomainEventDispatcher _dispatcher;

    public OrderRepository(IDbContextFactory<ApplicationDbContext> factory, IDomainEventDispatcher dispatcher)
    {
        _db = factory.CreateDbContext();
        _dispatcher = dispatcher;
    }

    public Task<Order?> GetByIdAsync(int id, CancellationToken ct = default)
        => _db.Orders.FirstOrDefaultAsync(o => o.Id == id, ct);

    public Task<Order?> GetByIdWithEventAsync(int id, CancellationToken ct = default)
        => _db.Orders.Include(o => o.Event)
                     .FirstOrDefaultAsync(o => o.Id == id, ct);

    public Task<Order?> GetByIdWithLinesAsync(int id, CancellationToken ct = default)
        => _db.Orders.Include(o => o.Lines.OrderBy(l => l.Position))
                     .FirstOrDefaultAsync(o => o.Id == id, ct);

    public Task<Order?> GetByIdWithLinesAndEventAsync(int id, CancellationToken ct = default)
        => _db.Orders.Include(o => o.Event)
                     .Include(o => o.Lines.OrderBy(l => l.Position))
                     .FirstOrDefaultAsync(o => o.Id == id, ct);

    public Task<Order?> GetByIdWithHistoryAsync(int id, CancellationToken ct = default)
        => _db.Orders.Include(o => o.Event)
                     .Include(o => o.Lines.OrderBy(l => l.Position))
                     .Include(o => o.Transitions.OrderBy(t => t.OccurredAt))
                     .FirstOrDefaultAsync(o => o.Id == id, ct);

    public Task<Order?> GetByEventAndNumberAsync(int eventId, int orderNumber, CancellationToken ct = default)
        => _db.Orders.Include(o => o.Lines)
                     .FirstOrDefaultAsync(o => o.EventId == eventId && o.OrderNumber == orderNumber, ct);

    public async Task<int> GetNextOrderNumberAsync(int eventId, CancellationToken ct = default)
    {
        var last = await _db.Orders.Where(o => o.EventId == eventId)
                                   .MaxAsync(o => (int?)o.OrderNumber, ct);
        return (last ?? 0) + 1;
    }

    public async Task<(List<Order> Items, int TotalCount)> GetPagedAsync(
        int eventId, int page, int pageSize, IReadOnlyCollection<OrderStatus>? statuses, CancellationToken ct = default)
    {
        var query = _db.Orders.Where(o => o.EventId == eventId);
        if (statuses is { Count: > 0 })
            query = query.Where(o => statuses.Contains(o.Status));

        var totalCount = await query.CountAsync(ct);
        var items = await query.OrderByDescending(o => o.CreatedAt)
                               .Skip((page - 1) * pageSize).Take(pageSize)
                               .ToListAsync(ct);
        return (items, totalCount);
    }

    public Task<List<Order>> GetByStatusAsync(int eventId, OrderStatus status, CancellationToken ct = default)
        => _db.Orders.Where(o => o.EventId == eventId && o.Status == status)
                     .OrderBy(o => o.CreatedAt).ToListAsync(ct);

    public async Task AddAsync(Order order, CancellationToken ct = default)
        => await _db.Orders.AddAsync(order, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        var tracked = _db.ChangeTracker.Entries<Order>().Select(e => e.Entity).ToList();
        foreach (var entry in _db.ChangeTracker.Entries<Order>()
                     .Where(e => e.State is EntityState.Added or EntityState.Modified))
            entry.Entity.Version++;

        var events = tracked.SelectMany(o => o.DomainEvents).ToList();

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new RepositoryConcurrencyException();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            throw new RepositoryUniqueConstraintException("A unique constraint violation occurred.", ex);
        }

        foreach (var order in tracked) order.ClearDomainEvents();
        if (events.Count > 0) await _dispatcher.DispatchAsync(events, ct);
    }

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return _db.DisposeAsync();
    }
}
