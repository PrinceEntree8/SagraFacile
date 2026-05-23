using SagraFacile.Application.Features.Reservations;
using SagraFacile.Domain.Features.Reservations;

namespace SagraFacile.Application.Interfaces;

public interface IReservationRepository
{
    Task<TableReservation?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<TableReservation?> GetByQueueNumberTodayAsync(string queueNumber, CancellationToken cancellationToken);
    Task<TableReservation?> GetLastByDatePrefixAsync(DateTime datePrefix, CancellationToken cancellationToken);
    Task<(List<TableReservation> Items, int TotalCount)> GetPagedAsync(
        string? status, int page, int pageSize, CancellationToken cancellationToken);
    Task<List<TableReservation>> GetCalledReservationsOrderedByCreatedAtAsync(CancellationToken cancellationToken);
    Task<List<TableReservation>> GetByDateRangeAsync(
        DateTime? startDateUtc, DateTime? endDateUtc, CancellationToken cancellationToken);
    Task<List<GetCounters.ReservationCounter>> GetCountersAsync(CancellationToken cancellationToken);
    Task AddAsync(TableReservation reservation, CancellationToken cancellationToken);
    Task AddCallAsync(ReservationCall call, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
