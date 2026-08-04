using Microsoft.EntityFrameworkCore;
using Npgsql;
using SagraFacile.Application.Exceptions;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Orders;
using SagraFacile.Infrastructure.Data;

namespace SagraFacile.Infrastructure.Repositories;

public class OrderingRuleRepository : IOrderingRuleRepository, IAsyncDisposable
{
    private readonly ApplicationDbContext _db;

    public OrderingRuleRepository(IDbContextFactory<ApplicationDbContext> factory)
    {
        _db = factory.CreateDbContext();
    }

    public async Task<OrderingRule?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _db.OrderingRules.FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<OrderingRule>> GetActiveByEventAsync(int eventId, CancellationToken ct = default)
        => await _db.OrderingRules
            .Where(r => r.EventId == eventId && r.IsActive)
            .OrderBy(r => r.Priority)
            .ThenBy(r => r.Id)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<OrderingRule>> GetByEventAsync(int eventId, CancellationToken ct = default)
        => await _db.OrderingRules
            .Where(r => r.EventId == eventId)
            .OrderBy(r => r.Priority)
            .ThenBy(r => r.Id)
            .ToListAsync(ct);

    public async Task AddAsync(OrderingRule rule, CancellationToken ct = default)
        => await _db.OrderingRules.AddAsync(rule, ct);

    public Task UpdateAsync(OrderingRule rule, CancellationToken ct = default)
    {
        _db.OrderingRules.Update(rule);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var rule = await GetByIdAsync(id, ct);
        if (rule is not null)
        {
            _db.OrderingRules.Remove(rule);
        }
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            throw new RepositoryUniqueConstraintException("A unique constraint violation occurred.", ex);
        }
    }

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return _db.DisposeAsync();
    }
}
