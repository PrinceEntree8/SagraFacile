using SagraFacile.Application.Features.Reservations;
using SagraFacile.Domain.Features.Reservations;

namespace SagraFacile.Application.Interfaces;

public interface IReservationRepository
{
    Task<Reservation?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<Reservation?> GetByIdWithEventAsync(int id, CancellationToken cancellationToken);
    Task<Reservation?> GetByEventAndSequenceAsync(int eventId, int sequenceNumber, CancellationToken cancellationToken);
    Task<int> GetNextSequenceNumberAsync(int eventId, CancellationToken cancellationToken);
    Task<(List<Reservation> Items, int TotalCount)> GetPagedAsync(
        int eventId, string? status, int page, int pageSize, CancellationToken cancellationToken);
    Task<List<Reservation>> GetCalledReservationsOrderedByCreatedAtAsync(int eventId, CancellationToken cancellationToken);
    Task<List<Reservation>> GetByDateRangeAsync(
        int? eventId, DateTime? startDateUtc, DateTime? endDateUtc, CancellationToken cancellationToken);
    Task<List<GetCounters.ReservationCounter>> GetCountersAsync(int eventId, CancellationToken cancellationToken);
    Task AddAsync(Reservation reservation, CancellationToken cancellationToken);
    Task AddCallAsync(ReservationCall call, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
