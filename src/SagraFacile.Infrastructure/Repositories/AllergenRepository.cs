using Microsoft.EntityFrameworkCore;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Menu;
using SagraFacile.Infrastructure.Data;

namespace SagraFacile.Infrastructure.Repositories;

public class AllergenRepository : IAllergenRepository, IAsyncDisposable
{
    private readonly ApplicationDbContext _db;

    public AllergenRepository(IDbContextFactory<ApplicationDbContext> factory) => _db = factory.CreateDbContext();

    public Task<List<Allergen>> GetAllAsync(CancellationToken ct) => _db.Allergens.OrderBy(a => a.Id).ToListAsync(ct);

    public ValueTask DisposeAsync() => _db.DisposeAsync();
}
