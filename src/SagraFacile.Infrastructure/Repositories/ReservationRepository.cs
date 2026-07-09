using SagraFacile.Application.Features.Reservations;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SagraFacile.Application.Exceptions;
using SagraFacile.Application.Interfaces;
using SagraFacile.Contracts.Reservations;
using SagraFacile.Domain.Features.Reservations;
using SagraFacile.Infrastructure.Data;

namespace SagraFacile.Infrastructure.Repositories;

public class ReservationRepository : IReservationRepository, IAsyncDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly IReservationNotifier _notifier;

    public ReservationRepository(
        IDbContextFactory<ApplicationDbContext> factory,
        IReservationNotifier notifier)
    {
        _db = factory.CreateDbContext();
        _notifier = notifier;
    }

    public Task<Reservation?> GetByIdAsync(int id, CancellationToken cancellationToken)
        => _db.Reservations.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<Reservation?> GetByIdWithEventAsync(int id, CancellationToken cancellationToken)
        => _db.Reservations
            .Include(r => r.Event)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<Reservation?> GetByEventAndSequenceAsync(int eventId, int sequenceNumber, CancellationToken cancellationToken)
        => _db.Reservations
            .Include(r => r.Event)
            .FirstOrDefaultAsync(r => r.EventId == eventId && r.SequenceNumber == sequenceNumber, cancellationToken);

    public async Task<int> GetNextSequenceNumberAsync(int eventId, CancellationToken cancellationToken)
    {
        var last = await _db.Reservations
            .Where(r => r.EventId == eventId)
            .MaxAsync(r => (int?)r.SequenceNumber, cancellationToken);

        return (last ?? 0) + 1;
    }

    public async Task<(List<Reservation> Items, int TotalCount)> GetPagedAsync(
        int eventId, int page, int pageSize, ReservationStatusFilter filter, CancellationToken cancellationToken)
    {
        var query = _db.Reservations.Where(r => r.EventId == eventId);

        if (filter != ReservationStatusFilter.None)
        {
            var statusFilter = filter.ToStatusArray();
            query = query.Where(r => statusFilter.Contains(r.Status));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<List<Reservation>> GetCalledReservationsOrderedByCreatedAtAsync(int eventId, CancellationToken cancellationToken)
        => _db.Reservations
            .Where(r => r.EventId == eventId && r.Status == ReservationStatus.Called)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<List<Reservation>> GetByDateRangeAsync(
        int? eventId, DateTime? startDateUtc, DateTime? endDateUtc, ReservationStatusFilter filter, CancellationToken cancellationToken)
    {
        var query = _db.Reservations.AsQueryable();

        if (eventId.HasValue)
            query = query.Where(r => r.EventId == eventId.Value);

        if (startDateUtc.HasValue)
            query = query.Where(r => r.CreatedAt >= startDateUtc.Value);

        if (endDateUtc.HasValue)
            query = query.Where(r => r.CreatedAt <= endDateUtc.Value);

        return await query.OrderBy(r => r.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<List<ReservationCounterDto>> GetCountersAsync(int eventId, CancellationToken cancellationToken)
    {
        return await _db.Reservations
            .Where(r => r.EventId == eventId)
            .GroupBy(r => r.Status)
            .Select(g => new ReservationCounterDto(
                g.Key.ToString(),
                g.Count(),
                g.Sum(r => r.PartySize)
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Reservation reservation, CancellationToken cancellationToken)
    {
        await _db.Reservations.AddAsync(reservation, cancellationToken);
    }

    public async Task AddCallAsync(ReservationCall call, CancellationToken cancellationToken)
        => await _db.ReservationCalls.AddAsync(call, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new RepositoryConcurrencyException();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            throw new RepositoryUniqueConstraintException("A unique constraint violation occurred.", ex);
        }
    }

    public Task<List<Reservation>> GetLastCalledAsync(int eventId, int maxEntries = 10,
        CancellationToken cancellationToken = default)
    {
        return _db.Reservations
            .Where(r => r.EventId == eventId && r.Status == ReservationStatus.Called)
            .OrderByDescending(r => r.LastCalledAt)
            .Take(maxEntries)
            .ToListAsync(cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return _db.DisposeAsync();
    }
}
