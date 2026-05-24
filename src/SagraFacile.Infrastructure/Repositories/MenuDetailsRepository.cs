using Microsoft.EntityFrameworkCore;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Menu;
using SagraFacile.Infrastructure.Data;

namespace SagraFacile.Infrastructure.Repositories;

public class MenuDetailsRepository : IMenuDetailsRepository, IAsyncDisposable
{
    private readonly ApplicationDbContext _db;

    public MenuDetailsRepository(IDbContextFactory<ApplicationDbContext> factory) => _db = factory.CreateDbContext();

    public Task<MenuDetails?> GetByEventIdAsync(int eventId, CancellationToken ct)
        => _db.MenuDetails.FirstOrDefaultAsync(d => d.EventId == eventId, ct);

    public async Task UpsertAsync(MenuDetails details, CancellationToken ct)
    {
        var existing = await _db.MenuDetails.FirstOrDefaultAsync(d => d.EventId == details.EventId, ct);
        if (existing is null)
            await _db.MenuDetails.AddAsync(details, ct);
        else
        {
            existing.WarningMessage = details.WarningMessage;
            existing.Header = details.Header;
            existing.Footer = details.Footer;
        }
    }

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);

    public ValueTask DisposeAsync() => _db.DisposeAsync();
}
