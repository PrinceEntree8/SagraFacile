using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SagraFacile.Application.Features.Reservations;
using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Contracts.Common;
using SagraFacile.Contracts.Reservations;
using SagraFacile.Domain.Features.Reservations;

namespace SagraFacile.Web.Hubs;

public class ReservationHub(IMediator mediator) : Hub<IReservationHubClient>
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

    public async Task<ReservationsDto> GetReservations(
        int eventId,
        int page = 1,
        int pageSize = 50,
        ReservationStatusFilter filter = ReservationStatusFilter.AllWaiting)
    {
        return await mediator.QueryAsync(new GetReservations.Query(eventId, page, pageSize, filter));
    }

    public async Task<IList<ReservationCounterDto>> GetCounters(int eventId)
        => await mediator.QueryAsync(new GetCounters.Query(eventId));

    public async Task<IList<ReservationMatchDto>> GetBestFitReservation(int eventId, int tableCoverCount)
        => await mediator.QueryAsync(new GetBestFitReservation.Query(eventId, tableCoverCount));

    [Authorize(Policy = "Cassiere")]
    public async Task<CommandResult<(int Id, int SequenceNumber)>> CreateReservation(
        int eventId,
        string customerName,
        int partySize,
        string? notes = null,
        bool partyComplete = false)
        => await mediator.SendAsync(new CreateReservation.Command(eventId, customerName, partySize, notes, partyComplete));

    [Authorize(Policy = "Cassiere")]
    public async Task<CommandResult> EditReservation(
        int id,
        string? customerName = null,
        int? partySize = null,
        string? notes = null,
        ReservationStatus? status = null)
        => await mediator.SendAsync(new EditReservation.Command(id, customerName, partySize, notes, status));

    [Authorize(Policy = "Cassiere")]
    public async Task<CommandResult> CallReservation(
        int reservationId,
        string calledBy = "Receptionist",
        string? notes = null)
        => await mediator.SendAsync(new CallReservation.Command(reservationId, calledBy, notes));

    [Authorize(Policy = "Cassiere")]
    public async Task<CommandResult> MarkPartyComplete(
        int reservationId,
        string markedBy = "System")
        => await mediator.SendAsync(new MarkPartyComplete.Command(reservationId, markedBy));

    [Authorize(Policy = "Cassiere")]
    public async Task<CommandResult> SeatReservation(int reservationId)
        => await mediator.SendAsync(new SeatReservation.Command(reservationId));

    [Authorize(Policy = "Cassiere")]
    public async Task<CommandResult> CallAndSeatReservation(int eventId, int sequenceNumber)
        => await mediator.SendAsync(new CallAndSeatReservation.Command(eventId, sequenceNumber));

    [Authorize(Policy = "Cassiere")]
    public async Task<CommandResult> VoidReservation(int reservationId)
        => await mediator.SendAsync(new VoidReservation.Command(reservationId));

    [Authorize(Policy = "Cassiere")]
    public async Task<CommandResult> RestoreReservation(int reservationId)
        => await mediator.SendAsync(new RestoreReservation.Command(reservationId));

    [Authorize(Policy = "Cassiere")]
    public async Task<CommandResult> UpdateTableCover(
        int? tableId,
        string? tableNumber,
        int coverCount)
        => await mediator.SendAsync(new UpdateTableCover.Command(tableId, tableNumber, coverCount));
}
