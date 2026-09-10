namespace SagraFacile.Application.Interfaces;

public interface IReservationSequenceLock
{
    Task AcquireAsync(int eventId, CancellationToken cancellationToken = default);
    void Release(int eventId);
}
