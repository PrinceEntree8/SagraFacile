using Microsoft.AspNetCore.SignalR;

namespace SagraFacile.Web.Hubs;

public class ReservationHub : Hub
{
    public async Task NotifyReservationCreated(int reservationId, string queueNumber, string customerName, int partySize)
    {
        await Clients.All.SendAsync("ReservationCreated", reservationId, queueNumber, customerName, partySize);
    }

    public async Task NotifyReservationCalled(int reservationId, string queueNumber, int callCount)
    {
        await Clients.All.SendAsync("ReservationCalled", reservationId, queueNumber, callCount);
    }

    public async Task NotifyReservationVoided(int reservationId, string queueNumber)
    {
        await Clients.All.SendAsync("ReservationVoided", reservationId, queueNumber);
    }

    public async Task NotifyReservationSeated(int reservationId, string queueNumber)
    {
        await Clients.All.SendAsync("ReservationSeated", reservationId, queueNumber);
    }

    public async Task NotifyTableUpdated(int tableId, string tableNumber, int coverCount)
    {
        await Clients.All.SendAsync("TableUpdated", tableId, tableNumber, coverCount);
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
