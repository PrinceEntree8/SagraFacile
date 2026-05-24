using SagraFacile.Domain.Features.Menu;

namespace SagraFacile.Application.Interfaces;

public interface IMenuDetailsRepository
{
    Task<MenuDetails?> GetByEventIdAsync(int eventId, CancellationToken ct);
    Task UpsertAsync(MenuDetails details, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
