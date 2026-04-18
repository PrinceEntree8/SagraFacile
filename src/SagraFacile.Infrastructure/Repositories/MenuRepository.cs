using Microsoft.EntityFrameworkCore;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Menu;
using SagraFacile.Infrastructure.Data;

namespace SagraFacile.Infrastructure.Repositories;

public class MenuRepository : IMenuRepository, IAsyncDisposable
{
    private readonly ApplicationDbContext _db;

    public MenuRepository(IDbContextFactory<ApplicationDbContext> factory) => _db = factory.CreateDbContext();

    public Task<List<MenuItem>> GetByEventIdAsync(int eventId, bool includeUnavailable, CancellationToken ct)
    {
        var query = _db.MenuItems
            .Include(m => m.MenuItemAllergens).ThenInclude(mia => mia.Allergen)
            .Where(m => m.EventId == eventId);
        if (!includeUnavailable)
            query = query.Where(m => m.IsAvailable);
        return query.OrderBy(m => m.Category).ThenBy(m => m.DisplayOrder).ThenBy(m => m.Name).ToListAsync(ct);
    }

    public Task<MenuItem?> GetByIdAsync(int id, CancellationToken ct)
        => _db.MenuItems.Include(m => m.MenuItemAllergens).ThenInclude(mia => mia.Allergen)
            .FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task AddAsync(MenuItem item, CancellationToken ct) => await _db.MenuItems.AddAsync(item, ct);

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var item = await _db.MenuItems.FindAsync([id], ct);
        if (item is not null) _db.MenuItems.Remove(item);
    }

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);

    public ValueTask DisposeAsync() => _db.DisposeAsync();
}
