using System.Collections.Concurrent;
using SagraFacile.Application.Interfaces;

namespace SagraFacile.Infrastructure.Services;

public sealed class ReservationSequenceLock : IReservationSequenceLock, IDisposable
{
    private readonly ConcurrentDictionary<int, SemaphoreSlim> _locks = new();

    public async Task AcquireAsync(int eventId, CancellationToken cancellationToken = default)
    {
        var semaphore = _locks.GetOrAdd(eventId, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(cancellationToken);
    }

    public void Release(int eventId)
    {
        if (_locks.TryGetValue(eventId, out var semaphore))
        {
            semaphore.Release();
        }
    }

    public void Dispose()
    {
        foreach (var semaphore in _locks.Values)
        {
            semaphore.Dispose();
        }
        _locks.Clear();
    }
}
