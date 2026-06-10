using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SagraFacile.Application.Features.Reservations;
using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Domain.Features.Reservations;

namespace SagraFacile.Web.Hubs;

[Authorize]
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

    public async Task<GetReservations.Result> GetReservations(
        int eventId,
        int page = 1,
        int pageSize = 50,
        ReservationStatusFilter filter = ReservationStatusFilter.AllWaiting)
        => await mediator.QueryAsync(new GetReservations.Query(eventId, page, pageSize, filter));

    public async Task<GetCounters.Result> GetCounters(int eventId)
        => await mediator.QueryAsync(new GetCounters.Query(eventId));

    public async Task<GetTables.Result> GetTables(string? status = null)
        => await mediator.QueryAsync(new GetTables.Query(status));

    public async Task<GetBestFitReservation.Result> GetBestFitReservation(int eventId, int tableCoverCount)
        => await mediator.QueryAsync(new GetBestFitReservation.Query(eventId, tableCoverCount));

    [Authorize(Policy = "AdminOrSupervisore")]
    public async Task<GetReservationReport.Result> GetReservationReport(int? eventId = null)
        => await mediator.QueryAsync(new GetReservationReport.Query(eventId));

    [Authorize(Policy = "Cassiere")]
    public async Task<CreateReservation.Result> CreateReservation(
        int eventId,
        string customerName,
        int partySize,
        string? notes = null,
        bool partyComplete = false)
        => await mediator.SendAsync(new CreateReservation.Command(eventId, customerName, partySize, notes, partyComplete));

    [Authorize(Policy = "Cassiere")]
    public async Task<EditReservation.Result> EditReservation(
        int id,
        string? customerName = null,
        int? partySize = null,
        string? notes = null,
        ReservationStatus? status = null)
        => await mediator.SendAsync(new EditReservation.Command(id, customerName, partySize, notes, status));

    [Authorize(Policy = "Cassiere")]
    public async Task<CallReservation.Result> CallReservation(
        int reservationId,
        string calledBy = "Receptionist",
        string? notes = null)
        => await mediator.SendAsync(new CallReservation.Command(reservationId, calledBy, notes));

    [Authorize(Policy = "Cassiere")]
    public async Task<MarkPartyComplete.Result> MarkPartyComplete(
        int reservationId,
        string markedBy = "System")
        => await mediator.SendAsync(new MarkPartyComplete.Command(reservationId, markedBy));

    [Authorize(Policy = "Cassiere")]
    public async Task<SeatReservation.Result> SeatReservation(int reservationId)
        => await mediator.SendAsync(new SeatReservation.Command(reservationId));

    [Authorize(Policy = "Cassiere")]
    public async Task<CallAndSeatReservation.Result> CallAndSeatReservation(int eventId, int sequenceNumber)
        => await mediator.SendAsync(new CallAndSeatReservation.Command(eventId, sequenceNumber));

    [Authorize(Policy = "Cassiere")]
    public async Task<VoidReservation.Result> VoidReservation(int reservationId)
        => await mediator.SendAsync(new VoidReservation.Command(reservationId));

    [Authorize(Policy = "Cassiere")]
    public async Task<UpdateTableCover.Result> UpdateTableCover(
        int? tableId,
        string? tableNumber,
        int coverCount)
        => await mediator.SendAsync(new UpdateTableCover.Command(tableId, tableNumber, coverCount));
}
