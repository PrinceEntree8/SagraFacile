using Microsoft.EntityFrameworkCore;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Menu;
using SagraFacile.Infrastructure.Data;

namespace SagraFacile.Infrastructure.Repositories;

public class AllergenRepository : IAllergenRepository
{
    private readonly ApplicationDbContext _db;

    public AllergenRepository(ApplicationDbContext db) => _db = db;

    public Task<List<Allergen>> GetAllAsync(CancellationToken ct) => _db.Allergens.OrderBy(a => a.Id).ToListAsync(ct);
}
