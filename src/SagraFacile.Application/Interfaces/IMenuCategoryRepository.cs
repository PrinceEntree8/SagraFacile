using SagraFacile.Domain.Features.Menu;

namespace SagraFacile.Application.Interfaces;

public interface IMenuCategoryRepository
{
    Task<List<MenuCategory>> GetAllAsync(CancellationToken ct);
    Task<MenuCategory?> GetByIdAsync(int id, CancellationToken ct);
    Task AddAsync(MenuCategory category, CancellationToken ct);
    Task DeleteAsync(int id, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
