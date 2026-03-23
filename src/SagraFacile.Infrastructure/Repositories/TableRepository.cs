using Microsoft.EntityFrameworkCore;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Reservations;
using SagraFacile.Infrastructure.Data;

namespace SagraFacile.Infrastructure.Repositories;

public class TableRepository : ITableRepository
{
    private readonly ApplicationDbContext _db;

    public TableRepository(ApplicationDbContext db) => _db = db;

    public Task<Table?> GetByIdAsync(int id, CancellationToken cancellationToken)
        => _db.Tables.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<Table?> GetByNumberAsync(string tableNumber, CancellationToken cancellationToken)
        => _db.Tables.FirstOrDefaultAsync(t => t.TableNumber == tableNumber, cancellationToken);

    public async Task<List<Table>> GetAllAsync(string? status, CancellationToken cancellationToken)
    {
        var query = _db.Tables.AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(t => t.Status == status);

        return await query.OrderBy(t => t.TableNumber).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Table table, CancellationToken cancellationToken)
        => await _db.Tables.AddAsync(table, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _db.SaveChangesAsync(cancellationToken);
}
