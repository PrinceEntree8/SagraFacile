using Microsoft.AspNetCore.SignalR;
using SagraFacile.Application.Interfaces;

namespace SagraFacile.Web.Hubs;

public class SignalRReservationNotifier : IReservationNotifier
{
    private readonly IHubContext<ReservationHub, IReservationHubClient> _hubContext;

    public SignalRReservationNotifier(IHubContext<ReservationHub, IReservationHubClient> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotifyReservationCreatedAsync(int reservationId, string queueNumber, string customerName, int partySize, CancellationToken cancellationToken)
    {
        return _hubContext.Clients.All
            .ReservationCreated(reservationId, queueNumber, customerName, partySize)
            .WaitAsync(cancellationToken);
    }

    public Task NotifyReservationCalledAsync(int reservationId, string queueNumber, string customerName, int partySize, int callCount, CancellationToken cancellationToken)
    {
        return _hubContext.Clients.All
            .ReservationCalled(reservationId, queueNumber, customerName, partySize, callCount)
            .WaitAsync(cancellationToken);
    }

    public Task NotifyReservationVoidedAsync(int reservationId, string queueNumber, CancellationToken cancellationToken)
    {
        return _hubContext.Clients.All
            .ReservationVoided(reservationId, queueNumber)
            .WaitAsync(cancellationToken);
    }
}
