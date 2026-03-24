using Microsoft.EntityFrameworkCore;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Events;
using SagraFacile.Infrastructure.Data;

namespace SagraFacile.Infrastructure.Repositories;

public class EventRepository : IEventRepository, IAsyncDisposable
{
    private readonly ApplicationDbContext _db;

    public EventRepository(IDbContextFactory<ApplicationDbContext> factory)
        => _db = factory.CreateDbContext();

    public Task<Event?> GetActiveAsync(CancellationToken cancellationToken)
        => _db.Events.FirstOrDefaultAsync(e => e.IsActive, cancellationToken);

    public Task<Event?> GetByIdAsync(int id, CancellationToken cancellationToken)
        => _db.Events.FindAsync([id], cancellationToken).AsTask();

    public Task<List<Event>> GetAllOrderedByDateDescAsync(CancellationToken cancellationToken)
        => _db.Events.OrderByDescending(e => e.Date).ToListAsync(cancellationToken);

    public async Task AddAsync(Event ev, CancellationToken cancellationToken)
        => await _db.Events.AddAsync(ev, cancellationToken);

    public Task DeactivateAllAsync(CancellationToken cancellationToken)
        => _db.Events
            .Where(e => e.IsActive)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.IsActive, false), cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _db.SaveChangesAsync(cancellationToken);

    public ValueTask DisposeAsync() => _db.DisposeAsync();
}
