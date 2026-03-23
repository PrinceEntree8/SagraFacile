using Microsoft.EntityFrameworkCore;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Reservations;
using SagraFacile.Infrastructure.Data;

namespace SagraFacile.Infrastructure.Repositories;

public class ReservationRepository : IReservationRepository
{
    private readonly ApplicationDbContext _db;

    public ReservationRepository(ApplicationDbContext db) => _db = db;

    public Task<TableReservation?> GetByIdAsync(int id, CancellationToken cancellationToken)
        => _db.TableReservations.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<TableReservation?> GetLastByDatePrefixAsync(string datePrefix, CancellationToken cancellationToken)
        => _db.TableReservations
            .Where(r => r.QueueNumber.StartsWith(datePrefix))
            .OrderByDescending(r => r.QueueNumber)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<(List<TableReservation> Items, int TotalCount)> GetPagedAsync(
        string? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = _db.TableReservations.AsQueryable();

        query = string.IsNullOrEmpty(status)
            ? query.Where(r => r.Status != "Seated" && r.Status != "Voided")
            : query.Where(r => r.Status == status);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<List<TableReservation>> GetCalledReservationsOrderedByCreatedAtAsync(CancellationToken cancellationToken)
        => _db.TableReservations
            .Where(r => r.Status == "Called")
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<List<TableReservation>> GetByDateRangeAsync(
        DateTime? startDateUtc, DateTime? endDateUtc, CancellationToken cancellationToken)
    {
        var query = _db.TableReservations.AsQueryable();

        if (startDateUtc.HasValue)
            query = query.Where(r => r.CreatedAt >= startDateUtc.Value);

        if (endDateUtc.HasValue)
            query = query.Where(r => r.CreatedAt <= endDateUtc.Value);

        return await query.OrderBy(r => r.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(TableReservation reservation, CancellationToken cancellationToken)
        => await _db.TableReservations.AddAsync(reservation, cancellationToken);

    public async Task AddCallAsync(ReservationCall call, CancellationToken cancellationToken)
        => await _db.ReservationCalls.AddAsync(call, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _db.SaveChangesAsync(cancellationToken);
}
