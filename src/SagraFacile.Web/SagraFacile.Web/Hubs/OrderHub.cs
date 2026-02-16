using Microsoft.AspNetCore.SignalR;

namespace SagraFacile.Web.Hubs;

public class OrderHub : Hub
{
    public async Task SendOrderUpdate(string orderId, string status)
    {
        await Clients.All.SendAsync("ReceiveOrderUpdate", orderId, status);
    }

    public async Task JoinOrderGroup(string orderId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"order-{orderId}");
    }

    public async Task LeaveOrderGroup(string orderId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"order-{orderId}");
    }

    public async Task NotifyOrderCreated(int orderId, string orderNumber)
    {
        await Clients.All.SendAsync("OrderCreated", orderId, orderNumber);
    }
}
