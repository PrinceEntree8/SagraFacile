using SagraFacile.Application.Features.Reservations;
using Microsoft.EntityFrameworkCore;
using SagraFacile.Application.Exceptions;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Reservations;
using SagraFacile.Infrastructure.Data;

namespace SagraFacile.Infrastructure.Repositories;

public class ReservationRepository : IReservationRepository, IAsyncDisposable
{
    private const int DatePrefixLength = 8;
    private readonly ApplicationDbContext _db;

    public ReservationRepository(IDbContextFactory<ApplicationDbContext> factory)
        => _db = factory.CreateDbContext();

    public Task<TableReservation?> GetByIdAsync(int id, CancellationToken cancellationToken)
        => GetByIdInternalAsync(id, cancellationToken);

    public Task<TableReservation?> GetLastByDatePrefixAsync(DateTime datePrefix, CancellationToken cancellationToken)
        => GetLastByDatePrefixInternalAsync(datePrefix, cancellationToken);

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
        => GetCalledReservationsOrderedByCreatedAtInternalAsync(cancellationToken);

    public async Task<List<TableReservation>> GetByDateRangeAsync(
        DateTime? startDateUtc, DateTime? endDateUtc, CancellationToken cancellationToken)
    {
        var query = _db.TableReservations.AsQueryable();

        if (startDateUtc.HasValue)
            query = query.Where(r => r.CreatedAt >= startDateUtc.Value);

        if (endDateUtc.HasValue)
            query = query.Where(r => r.CreatedAt <= endDateUtc.Value);

        var items = await query.OrderBy(r => r.CreatedAt).ToListAsync(cancellationToken);

        return items;
    }

    public async Task<List<GetCounters.ReservationCounter>> GetCountersAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var todayPrefix = now.ToString("yyyyMMdd");

        return await _db.TableReservations
            .Where(r => r.ReservationId.StartsWith(todayPrefix))
            .GroupBy(r => r.Status)
            .Select(g => new GetCounters.ReservationCounter(
                g.Key,
                g.Count(),
                g.Sum(r => r.PartySize)
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(TableReservation reservation, CancellationToken cancellationToken)
    {
        await _db.TableReservations.AddAsync(reservation, cancellationToken);
    }

    public async Task AddCallAsync(ReservationCall call, CancellationToken cancellationToken)
        => await _db.ReservationCalls.AddAsync(call, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        foreach (var entry in _db.ChangeTracker.Entries<TableReservation>())
        {
            if (entry.State == EntityState.Modified)
                entry.Entity.Version++;
        }
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new RepositoryConcurrencyException();
        }
    }

    public ValueTask DisposeAsync() => _db.DisposeAsync();

    private async Task<TableReservation?> GetByIdInternalAsync(int id, CancellationToken cancellationToken)
    {
        var reservation = await _db.TableReservations.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        return reservation;
    }

    private async Task<TableReservation?> GetLastByDatePrefixInternalAsync(DateTime datePrefix, CancellationToken cancellationToken)
    {
        var reservation = await _db.TableReservations
            .Where(r => r.CreatedAt.Date >= datePrefix.ToUniversalTime() && r.CreatedAt.Date <= datePrefix.ToUniversalTime().AddDays(1))
            .OrderByDescending(r => r.ReservationId)
            .FirstOrDefaultAsync(cancellationToken);

        return reservation;
    }

    private async Task<List<TableReservation>> GetCalledReservationsOrderedByCreatedAtInternalAsync(CancellationToken cancellationToken)
    {
        var reservations = await _db.TableReservations
            .Where(r => r.Status == "Called")
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        return reservations;
    }
}
