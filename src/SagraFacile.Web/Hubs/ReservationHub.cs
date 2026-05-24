using Microsoft.AspNetCore.SignalR;

namespace SagraFacile.Web.Hubs;

public class ReservationHub : Hub<IReservationHubClient>
{
    public async Task NotifyReservationCreated(int reservationId, int sequenceNumber, string customerName, int partySize)
    {
        await Clients.All.ReservationCreated(reservationId, sequenceNumber, customerName, partySize);
    }

    public async Task NotifyReservationCalled(int reservationId, int sequenceNumber, string customerName, int partySize, int callCount)
    {
        // Broadcast to all connected clients (includes those in the NOW_CALLING group).
        await Clients.All.ReservationCalled(reservationId, sequenceNumber, customerName, partySize, callCount);
    }

    public async Task NotifyReservationVoided(int reservationId, int sequenceNumber)
    {
        await Clients.All.ReservationVoided(reservationId, sequenceNumber);
    }

    public async Task NotifyReservationSeated(int reservationId, int sequenceNumber)
    {
        await Clients.All.ReservationSeated(reservationId, sequenceNumber);
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
