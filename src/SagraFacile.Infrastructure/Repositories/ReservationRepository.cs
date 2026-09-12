using SagraFacile.Application.Features.Reservations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using SagraFacile.Application.Exceptions;
using SagraFacile.Application.Interfaces;
using SagraFacile.Contracts.Reservations;
using SagraFacile.Domain.Features.Reservations;
using SagraFacile.Infrastructure.Data;

namespace SagraFacile.Infrastructure.Repositories;

public class ReservationRepository : IReservationRepository, IAsyncDisposable
{
    private readonly ApplicationDbContext db;
    private readonly IReservationNotifier notifier;

    public ReservationRepository(
        IDbContextFactory<ApplicationDbContext> factory,
        IReservationNotifier notifier)
    {
        db = factory.CreateDbContext();
        this.notifier = notifier;
    }
    
    private IDbContextTransaction? _transaction;

    public async ValueTask DisposeAsync()
    {
        if (_transaction != null) await _transaction.DisposeAsync();
        await db.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Begins a transaction and acquires a Postgres transaction-scoped advisory lock scoped to the given event,
    /// serializing concurrent sequence-number allocation for the same EventId across any number of app instances.
    /// The lock is automatically released when the transaction commits/rolls back (see SaveChangesAsync).
    /// No-op lock acquisition on non-Postgres providers (e.g. SQLite in tests), where only the transaction is started.
    /// </summary>
    public async Task AcquireEventLockAsync(int eventId, CancellationToken cancellationToken)
    {
        _transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        
        if (db.Database.IsNpgsql())
        {
            await db.Database.ExecuteSqlAsync(
                $"SELECT pg_advisory_xact_lock(hashtext('reservation_seq'), {eventId})",
                cancellationToken);
        }
    }

    public Task<Reservation?> GetByIdAsync(int id, CancellationToken cancellationToken)
        => db.Reservations.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<Reservation?> GetByIdWithEventAsync(int id, CancellationToken cancellationToken)
        => db.Reservations
            .Include(r => r.Event)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public Task<Reservation?> GetByEventAndSequenceAsync(int eventId, int sequenceNumber, CancellationToken cancellationToken)
        => db.Reservations
            .Include(r => r.Event)
            .FirstOrDefaultAsync(r => r.EventId == eventId && r.SequenceNumber == sequenceNumber, cancellationToken);

    public async Task<int> GetNextSequenceNumberAsync(int eventId, CancellationToken cancellationToken)
    {   
        var last = await db.Reservations
            .Where(r => r.EventId == eventId)
            .MaxAsync(r => (int?)r.SequenceNumber, cancellationToken);

        return (last ?? 0) + 1;
    }

    public async Task<(List<Reservation> Items, int TotalCount)> GetPagedAsync(
        int eventId, int page, int pageSize, ReservationStatusFilter filter, CancellationToken cancellationToken)
    {
        var query = db.Reservations.Where(r => r.EventId == eventId);

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
        => db.Reservations
            .Where(r => r.EventId == eventId && r.Status == ReservationStatus.Called)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<List<Reservation>> GetByDateRangeAsync(
        int? eventId, DateTime? startDateUtc, DateTime? endDateUtc, ReservationStatusFilter filter, CancellationToken cancellationToken)
    {
        var query = db.Reservations.AsQueryable();

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
        return await db.Reservations
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
        await db.Reservations.AddAsync(reservation, cancellationToken);
    }

    public async Task AddCallAsync(ReservationCall call, CancellationToken cancellationToken)
        => await db.ReservationCalls.AddAsync(call, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await CommitTransactionAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            db.ChangeTracker.Clear();
            await RollbackTransactionAsync(cancellationToken);
            throw new RepositoryConcurrencyException();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            db.ChangeTracker.Clear();
            await RollbackTransactionAsync(cancellationToken);
            throw new RepositoryUniqueConstraintException("A unique constraint violation occurred.", ex);
        }
    }

    private async Task CommitTransactionAsync(CancellationToken cancellationToken)
    {
        if (_transaction is null)
            return;

        await _transaction.CommitAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    private async Task RollbackTransactionAsync(CancellationToken cancellationToken)
    {
        if (_transaction is null)
            return;

        await _transaction.RollbackAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public Task<List<Reservation>> GetLastCalledAsync(int eventId, int maxEntries = 10,
        CancellationToken cancellationToken = default)
    {
        return db.Reservations
            .Where(r => r.EventId == eventId && r.Status == ReservationStatus.Called)
            .OrderByDescending(r => r.LastCalledAt)
            .Take(maxEntries)
            .ToListAsync(cancellationToken);
    }
}
