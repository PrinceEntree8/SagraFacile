using SagraFacile.Domain.Features.Menu;

namespace SagraFacile.Application.Interfaces;

public interface IMenuRepository
{
    Task<List<MenuItem>> GetByEventIdAsync(int eventId, bool includeUnavailable, CancellationToken ct);
    Task<MenuItem?> GetByIdAsync(int id, CancellationToken ct);
    Task AddAsync(MenuItem item, CancellationToken ct);
    Task DeleteAsync(int id, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
