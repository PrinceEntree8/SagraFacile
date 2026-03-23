using Microsoft.EntityFrameworkCore;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Events;
using SagraFacile.Infrastructure.Data;

namespace SagraFacile.Infrastructure.Repositories;

public class EventRepository : IEventRepository
{
    private readonly ApplicationDbContext _db;

    public EventRepository(ApplicationDbContext db) => _db = db;

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
}
