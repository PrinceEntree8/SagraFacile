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

    public Task<TableReservation?> GetLastByDatePrefixAsync(string datePrefix, CancellationToken cancellationToken)
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

        foreach (var item in items)
            MapFromReservationId(item);

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
        foreach (var item in items)
            MapFromReservationId(item);

        return items;
    }

    public async Task AddAsync(TableReservation reservation, CancellationToken cancellationToken)
    {
        MapToReservationId(reservation);
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
        return reservation is null ? null : MapFromReservationId(reservation);
    }

    private async Task<TableReservation?> GetLastByDatePrefixInternalAsync(string datePrefix, CancellationToken cancellationToken)
    {
        var reservation = await _db.TableReservations
            .Where(r => r.ReservationId.StartsWith(datePrefix))
            .OrderByDescending(r => r.ReservationId)
            .FirstOrDefaultAsync(cancellationToken);

        return reservation is null ? null : MapFromReservationId(reservation);
    }

    private async Task<List<TableReservation>> GetCalledReservationsOrderedByCreatedAtInternalAsync(CancellationToken cancellationToken)
    {
        var reservations = await _db.TableReservations
            .Where(r => r.Status == "Called")
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        foreach (var reservation in reservations)
            MapFromReservationId(reservation);

        return reservations;
    }

    private static TableReservation MapFromReservationId(TableReservation reservation)
    {
        var (date, queueNumber) = SplitReservationId(reservation.ReservationId);
        reservation.Date = date;
        reservation.QueueNumber = queueNumber;
        return reservation;
    }

    private static void MapToReservationId(TableReservation reservation)
    {
        var reservationId = BuildReservationId(reservation.Date, reservation.QueueNumber);
        if (!string.IsNullOrWhiteSpace(reservationId))
        {
            reservation.ReservationId = reservationId;
            var (date, queueNumber) = SplitReservationId(reservationId);
            reservation.Date = date;
            reservation.QueueNumber = queueNumber;
        }
    }

    private static string BuildReservationId(string? date, string? queueNumber)
    {
        var normalizedDate = (date ?? string.Empty).Trim();
        var normalizedQueueNumber = (queueNumber ?? string.Empty).Trim();

        if (normalizedDate.Length == DatePrefixLength && normalizedDate.All(char.IsDigit) && !string.IsNullOrWhiteSpace(normalizedQueueNumber))
            return normalizedDate + normalizedQueueNumber.PadLeft(4, '0');

        if (!string.IsNullOrWhiteSpace(normalizedQueueNumber))
            return normalizedQueueNumber;

        return string.Empty;
    }

    private static (string Date, string QueueNumber) SplitReservationId(string? reservationId)
    {
        var normalizedReservationId = (reservationId ?? string.Empty).Trim();

        if (normalizedReservationId.Length > DatePrefixLength)
        {
            var date = normalizedReservationId[..DatePrefixLength];
            var queueNumber = normalizedReservationId[DatePrefixLength..];

            if (date.All(char.IsDigit) && queueNumber.All(char.IsDigit))
                return (date, queueNumber);
        }

        return (string.Empty, normalizedReservationId);
    }
}
