using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SagraFacile.Application.Features.Reservations;
using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Contracts.Common;
using SagraFacile.Contracts.Reservations;
using SagraFacile.Domain.Features.Reservations;

namespace SagraFacile.Web.Controllers;

[ApiController]
[Route("api/reservations")]
[Authorize(Policy = "Cassiere")]
public class ReservationController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [EndpointName("Reservations_List")]
    [ProducesResponseType(typeof(ReservationsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Get(
        [FromQuery] int eventId,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var filter = ParseFilter(status);
        var result = await mediator.QueryAsync(new GetReservations.Query(eventId, page, pageSize, filter), ct);
        return Ok(new ReservationsDto(result.Reservations.Select(r => new ReservationDto(
                r.Id,
                r.SequenceNumber,
                r.CustomerName,
                r.PartySize,
                r.Status,
                r.Notes,
                r.CreatedAt,
                r.FirstCalledAt,
                r.LastCalledAt,
                r.CallCount,
                r.WaitingTime,
                r.TimeSinceLastCall)).ToList(), result.TotalCount));
    }

    [HttpGet("last-called")]
    [AllowAnonymous]
    [EndpointName("Reservations_LastCalled")]
    [ProducesResponseType(typeof(IList<CalledEntry>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        [FromQuery] int eventId,
        [FromQuery] int maxEntries = 10,
        CancellationToken ct = default)
    {
        if (User.Identity is {IsAuthenticated: false})
        {
            maxEntries = maxEntries > 10 ? 10 : maxEntries;
        }
        
        var result = await mediator.QueryAsync(new GetLastCalled.Query(eventId, maxEntries), ct);
        return Ok(result);
    }

    [HttpGet("counters")]
    [EndpointName("Reservations_Counters")]
    [ProducesResponseType(typeof(IList<ReservationCounterDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCounters([FromQuery] int eventId, CancellationToken ct)
    {
        var result = await mediator.QueryAsync(new GetCounters.Query(eventId), ct);
        return Ok(result);
    }

    [HttpGet("best-fit")]
    [EndpointName("Reservations_BestFit")]
    [ProducesResponseType(typeof(IList<ReservationMatchDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetBestFit([FromQuery] int eventId, [FromQuery] int availableSeats, CancellationToken ct)
    {
        var result = await mediator.QueryAsync(new GetBestFitReservation.Query(eventId, availableSeats), ct);
        return Ok(result);
    }

    [Authorize(Policy = "AdminOrSupervisore")]
    [HttpGet("report")]
    [EndpointName("Reservations_Report")]
    [ProducesResponseType(typeof(IEnumerable<ReservationReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetReport([FromQuery] int eventId, CancellationToken ct)
    {
        var result = await mediator.QueryAsync(new GetReservationReport.Query(eventId), ct);
        return Ok(result.Reports.Select(r => new ReservationReportDto(
            r.Id,
            r.SequenceNumber,
            r.CustomerName,
            r.PartySize,
            r.Status.ToString(),
            r.CreatedAt,
            r.FirstCalledAt,
            r.SeatedAt,
            r.VoidedAt,
            r.CallCount,
            r.WaitTimeUntilFirstCall,
            r.TotalWaitTime)));
    }

    [HttpPost]
    [EndpointName("Reservations_Create")]
    [ProducesResponseType(typeof(CommandResult<CreateReservationResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateReservationRequest req, CancellationToken ct)
        => Ok(await mediator.SendAsync(
            new CreateReservation.Command(req.EventId, req.CustomerName, req.PartySize, req.Notes, req.PartyComplete), ct));

    [HttpPost("{id:int}/call")]
    [EndpointName("Reservations_Call")]
    [ProducesResponseType(typeof(CommandResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Call(int id, [FromBody] CallReservationRequest req, CancellationToken ct)
        => Ok(await mediator.SendAsync(new CallReservation.Command(id, req.CalledBy, req.Notes), ct));

    [HttpPost("{id:int}/seat")]
    [EndpointName("Reservations_Seat")]
    [ProducesResponseType(typeof(CommandResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Seat(int id, CancellationToken ct)
        => Ok(await mediator.SendAsync(new SeatReservation.Command(id), ct));

    [HttpPost("call-and-seat")]
    [EndpointName("Reservations_CallAndSeat")]
    [ProducesResponseType(typeof(CommandResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CallAndSeat([FromBody] CallAndSeatRequest req, CancellationToken ct)
        => Ok(await mediator.SendAsync(new CallAndSeatReservation.Command(req.EventId, req.SequenceNumber), ct));

    [HttpPut("{id:int}")]
    [EndpointName("Reservations_Edit")]
    [ProducesResponseType(typeof(CommandResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Edit(int id, [FromBody] EditReservationRequest req, CancellationToken ct)
        => Ok(await mediator.SendAsync(new EditReservation.Command(id, req.CustomerName, req.PartySize, req.Notes, req.Status), ct));

    [HttpPost("{id:int}/party-complete")]
    [EndpointName("Reservations_MarkPartyComplete")]
    [ProducesResponseType(typeof(CommandResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> MarkPartyComplete(int id, CancellationToken ct)
        => Ok(await mediator.SendAsync(new MarkPartyComplete.Command(id), ct));

    [HttpDelete("{id:int}")]
    [EndpointName("Reservations_Void")]
    [ProducesResponseType(typeof(CommandResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Void(int id, CancellationToken ct)
        => Ok(await mediator.SendAsync(new VoidReservation.Command(id), ct));

    [HttpPost("{id:int}/restore")]
    [EndpointName("Reservations_Restore")]
    [ProducesResponseType(typeof(CommandResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Restore(int id, CancellationToken ct)
        => Ok(await mediator.SendAsync(new RestoreReservation.Command(id), ct));

    private static ReservationStatusFilter ParseFilter(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return ReservationStatusFilter.AllWaiting;

        return Enum.TryParse(status, out ReservationStatusFilter @case) 
            ? @case 
            : ReservationStatusFilter.AllWaiting;
    }
}
