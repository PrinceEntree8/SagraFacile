using Microsoft.AspNetCore.SignalR;

namespace SagraFacile.Web.Hubs;

/// <summary>
/// SignalR hub for reservation notifications.
/// All server-to-client messages are dispatched by <see cref="ReservationNotificationDispatcher"/>.
/// Clients use group membership to filter which events they receive.
/// </summary>
public class ReservationHub : Hub<IReservationHubClient>
{
    public async Task JoinReservationGroup(string groupName)
        => await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

    public async Task LeaveReservationGroup(string groupName)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
}
