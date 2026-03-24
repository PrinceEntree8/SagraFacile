using SagraFacile.Domain.Features.Events;

namespace SagraFacile.Application.Interfaces;

public interface IEventRepository
{
    Task<Event?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<List<Event>> GetAllOrderedByDateDescAsync(CancellationToken cancellationToken);
    Task<Event?> GetActiveAsync(CancellationToken cancellationToken);
    Task AddAsync(Event ev, CancellationToken cancellationToken);
    Task DeactivateAllAsync(CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
