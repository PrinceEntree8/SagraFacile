using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SagraFacile.Application.Features.Reservations;
using SagraFacile.Application.Infrastructure.CQRS;

namespace SagraFacile.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "Cassiere")]
[IgnoreAntiforgeryToken]
public class ReservationsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReservationRequest req, CancellationToken ct)
        => Ok(await mediator.SendAsync(
            new CreateReservation.Command(req.EventId, req.CustomerName, req.PartySize, req.Notes, req.PartyComplete), ct));

    [HttpPost("{id:int}/call")]
    public async Task<IActionResult> Call(int id, [FromBody] CallReservationRequest req, CancellationToken ct)
        => Ok(await mediator.SendAsync(new CallReservation.Command(id, req.CalledBy, req.Notes), ct));

    [HttpPost("{id:int}/seat")]
    public async Task<IActionResult> Seat(int id, CancellationToken ct)
        => Ok(await mediator.SendAsync(new SeatReservation.Command(id), ct));

    [HttpPost("call-and-seat")]
    public async Task<IActionResult> CallAndSeat([FromBody] CallAndSeatRequest req, CancellationToken ct)
        => Ok(await mediator.SendAsync(new CallAndSeatReservation.Command(req.EventId, req.SequenceNumber), ct));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Void(int id, CancellationToken ct)
        => Ok(await mediator.SendAsync(new VoidReservation.Command(id), ct));
}

public record CreateReservationRequest(int EventId, string CustomerName, int PartySize, string? Notes = null, bool PartyComplete = false);
public record CallReservationRequest(string CalledBy = "Receptionist", string? Notes = null);
public record CallAndSeatRequest(int EventId, int SequenceNumber);
