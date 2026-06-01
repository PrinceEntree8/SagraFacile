using SagraFacile.Application.Features.Reservations;
using Microsoft.AspNetCore.SignalR;
using SagraFacile.Application.Interfaces;

namespace SagraFacile.Web.Hubs;

public class SignalRReservationNotifier(IHubContext<ReservationHub, IReservationHubClient> hubContext)
    : IReservationNotifier
{
    public Task NotifyReservationCreatedAsync(int reservationId, int sequenceNumber, string customerName, int partySize, CancellationToken cancellationToken)
    {
        return hubContext.Clients.All
            .ReservationCreated(reservationId, sequenceNumber, customerName, partySize)
            .WaitAsync(cancellationToken);
    }

    public Task NotifyReservationPartyCompleteAsync(int reservationId, int sequenceNumber, string customerName, int partySize, CancellationToken cancellationToken)
    {
        return hubContext.Clients.All
            .ReservationPartyComplete(reservationId, sequenceNumber, customerName, partySize)
            .WaitAsync(cancellationToken);
    }

    public Task NotifyReservationCalledAsync(int reservationId, int sequenceNumber, string customerName, int partySize, int callCount, CancellationToken cancellationToken)
    {
        return hubContext.Clients.All
            .ReservationCalled(reservationId, sequenceNumber, customerName, partySize, callCount)
            .WaitAsync(cancellationToken);
    }

    public Task NotifyReservationVoidedAsync(int reservationId, int sequenceNumber, CancellationToken cancellationToken)
    {
        return hubContext.Clients.All
            .ReservationVoided(reservationId, sequenceNumber)
            .WaitAsync(cancellationToken);
    }

    public Task NotifyReservationSeatedAsync(int reservationId, int sequenceNumber, CancellationToken cancellationToken)
    {
        return hubContext.Clients.All
            .ReservationSeated(reservationId, sequenceNumber)
            .WaitAsync(cancellationToken);
    }

    public Task NotifyCountersUpdatedAsync(List<GetCounters.ReservationCounter> counters, CancellationToken cancellationToken)
    {
        return hubContext.Clients.All
            .CountersUpdated(counters)
            .WaitAsync(cancellationToken);
    }
}
