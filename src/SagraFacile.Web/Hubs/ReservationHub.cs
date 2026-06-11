using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SagraFacile.Application.Features.Reservations;
using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Domain.Features.Reservations;

namespace SagraFacile.Web.Hubs;

public class ReservationHub : Hub<IReservationHubClient>
{
    [AllowAnonymous]
    public async Task JoinReservationGroup(string groupName)
        => await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

    [AllowAnonymous]
    public async Task LeaveReservationGroup(string groupName)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

    [Authorize(Policy = "Cassiere")]
    public async Task NotifyAvailableSeatsUpdated(int availableSeats)
        => await Clients.All.AvailableSeatsUpdated(availableSeats);
}
