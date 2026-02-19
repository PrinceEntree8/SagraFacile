using Microsoft.AspNetCore.SignalR;

namespace SagraFacile.Web.Hubs;

public class ReservationHub : Hub<IReservationHubClient>
{
    public async Task NotifyReservationCreated(int reservationId, string queueNumber, string customerName, int partySize)
    {
        await Clients.All.ReservationCreated(reservationId, queueNumber, customerName, partySize);
    }

    public async Task NotifyReservationCalled(int reservationId, string queueNumber, int callCount)
    {
        await Clients.All.ReservationCalled(reservationId, queueNumber, callCount);
    }

    public async Task NotifyReservationVoided(int reservationId, string queueNumber)
    {
        await Clients.All.ReservationVoided(reservationId, queueNumber);
    }

    public async Task NotifyReservationSeated(int reservationId, string queueNumber)
    {
        await Clients.All.ReservationSeated(reservationId, queueNumber);
    }

    public async Task NotifyAvailableSeatsUpdated(int availableSeats)
    {
        await Clients.All.AvailableSeatsUpdated(availableSeats);
    }

    public async Task JoinReservationGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    public async Task LeaveReservationGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }
}
