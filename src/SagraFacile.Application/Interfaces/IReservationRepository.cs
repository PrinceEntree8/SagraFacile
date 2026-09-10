using SagraFacile.Contracts.Reservations;
using SagraFacile.Domain.Features.Reservations;

namespace SagraFacile.Application.Interfaces;

public interface IReservationRepository
{
    Task<Reservation?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Reservation?> GetByIdWithEventAsync(int id, CancellationToken cancellationToken = default);
    Task<Reservation?> GetByEventAndSequenceAsync(int eventId, int sequenceNumber, CancellationToken cancellationToken = default);
    Task<int> GetNextSequenceNumberAsync(int eventId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Begins a transaction and acquires a database-level lock scoped to the given event so that concurrent
    /// sequence-number allocation for the same EventId is serialized across any number of app instances.
    /// Must be paired with a subsequent call to SaveChangesAsync, which commits/rolls back the transaction.
    /// </summary>
    Task AcquireEventLockAsync(int eventId, CancellationToken cancellationToken = default);
    Task<(List<Reservation> Items, int TotalCount)> GetPagedAsync(
        int eventId, int page, int pageSize, ReservationStatusFilter filter, CancellationToken cancellationToken = default);
    Task<List<Reservation>> GetCalledReservationsOrderedByCreatedAtAsync(int eventId, CancellationToken cancellationToken = default);
    Task<List<Reservation>> GetByDateRangeAsync(
        int? eventId, DateTime? startDateUtc, DateTime? endDateUtc, ReservationStatusFilter filter, CancellationToken cancellationToken = default);
    Task<List<ReservationCounterDto>> GetCountersAsync(int eventId, CancellationToken cancellationToken = default);
    Task AddAsync(Reservation reservation, CancellationToken cancellationToken = default);
    Task AddCallAsync(ReservationCall call, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<List<Reservation>> GetLastCalledAsync(int eventId, int maxEntries = 10,
        CancellationToken cancellationToken = default);
}
