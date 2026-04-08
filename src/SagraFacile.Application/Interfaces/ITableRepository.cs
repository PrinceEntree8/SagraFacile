using SagraFacile.Domain.Features.Reservations;

namespace SagraFacile.Application.Interfaces;

public interface ITableRepository
{
    Task<Table?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<Table?> GetByNumberAsync(string tableNumber, CancellationToken cancellationToken);
    Task<List<Table>> GetAllAsync(string? status, CancellationToken cancellationToken);
    Task AddAsync(Table table, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
