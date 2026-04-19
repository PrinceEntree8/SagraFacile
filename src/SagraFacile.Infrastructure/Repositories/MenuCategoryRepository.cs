using Microsoft.EntityFrameworkCore;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Menu;
using SagraFacile.Infrastructure.Data;

namespace SagraFacile.Infrastructure.Repositories;

public class MenuCategoryRepository : IMenuCategoryRepository, IAsyncDisposable
{
    private readonly ApplicationDbContext _db;

    public MenuCategoryRepository(IDbContextFactory<ApplicationDbContext> factory) => _db = factory.CreateDbContext();

    public Task<List<MenuCategory>> GetAllAsync(CancellationToken ct)
        => _db.MenuCategories.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToListAsync(ct);

    public Task<MenuCategory?> GetByIdAsync(int id, CancellationToken ct)
        => _db.MenuCategories.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task AddAsync(MenuCategory category, CancellationToken ct)
        => await _db.MenuCategories.AddAsync(category, ct);

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var cat = await _db.MenuCategories.FindAsync([id], ct);
        if (cat is not null) _db.MenuCategories.Remove(cat);
    }

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);

    public ValueTask DisposeAsync() => _db.DisposeAsync();
}
