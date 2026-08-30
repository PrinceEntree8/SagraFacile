using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SagraFacile.Application.Features.Events;
using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Contracts.Events;

namespace SagraFacile.Web.Controllers;

[ApiController]
[Route("api/events")]
public class EventController(IMediator mediator) : ControllerBase
{
    [Authorize(Policy = "AdminOrSupervisore")]
    [HttpGet]
    [EndpointName("Events_List")]
    [ProducesResponseType(typeof(IEnumerable<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetEvents(CancellationToken ct)
    {
        var result = await mediator.QueryAsync(new GetEvents.Query(), ct);
        return Ok(result.Events.Select(MapEvent));
    }

    [AllowAnonymous]
    [HttpGet("active")]
    [EndpointName("Events_GetActive")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GetActiveEvent(CancellationToken ct)
    {
        var result = await mediator.QueryAsync(new GetActiveEvent.Query(), ct);
        if (result.ActiveEvent is null)
            return NoContent();

        var e = result.ActiveEvent;
        return Ok(new EventDto(e.Id, e.Name, string.Empty, DateTime.UtcNow, e.Currency, e.CurrencySymbol, true, DateTime.UtcNow));
    }

    [Authorize(Policy = "AdminOrSupervisore")]
    [HttpGet("{id:int}")]
    [EndpointName("Events_GetById")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetEventById(int id, CancellationToken ct)
    {
        var result = await mediator.QueryAsync(new GetEvents.Query(), ct);
        var ev = result.Events.FirstOrDefault(x => x.Id == id);
        if (ev is null)
            return NotFound();
        return Ok(MapEvent(ev));
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPost]
    [EndpointName("Events_Create")]
    [ProducesResponseType(typeof(CreateEventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateEventRequest request, CancellationToken ct)
    {
        var result = await mediator.SendAsync(new CreateEvent.Command(request.Name, request.Description, request.Date, request.Currency, request.CurrencySymbol), ct);
        return Ok(new CreateEventResponse(result.Id, result.Name));
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPut("{id:int}/activate")]
    [EndpointName("Events_Activate")]
    [ProducesResponseType(typeof(ActivateEventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Activate(int id, CancellationToken ct)
    {
        var result = await mediator.SendAsync(new ActivateEvent.Command(id), ct);
        return Ok(new ActivateEventResponse(result.Success, result.Message));
    }

    [Authorize(Policy = "AdminOrSupervisore")]
    [HttpGet("{id:int}/options")]
    [EndpointName("Events_GetOptions")]
    [ProducesResponseType(typeof(EventAdditionalOptionsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetOptions(int id, CancellationToken ct)
    {
        var result = await mediator.QueryAsync(new GetEventAdditionalOptions.Query(id), ct);
        if (result is null)
            return NotFound();

        return Ok(new EventAdditionalOptionsDto(
            result.AdditionalOptions.Reservations.PartyCompletion.Enabled,
            result.AdditionalOptions.Reservations.PartyCompletion.MinPartySize,
            result.AdditionalOptions.View.ShowNotesField,
            result.AdditionalOptions.View.CounterPeopleFirst,
            result.AdditionalOptions.View.ShowCallCount,
            result.AdditionalOptions.View.MaxWaitTimeMinutes));
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPut("{id:int}/options")]
    [EndpointName("Events_UpdateOptions")]
    [ProducesResponseType(typeof(UpdateEventOptionsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateOptions(int id, [FromBody] UpdateEventAdditionalOptionsRequest request, CancellationToken ct)
    {
        var result = await mediator.SendAsync(new UpdateEventAdditionalOptions.Command(
            id,
            request.IsPartyCompletionEnabled,
            request.MinPartySize,
            request.ShowNotesField,
            request.CounterPeopleFirst,
            request.ShowCallCount,
            request.MaxWaitTimeMinutes), ct);
        return Ok(new UpdateEventOptionsResponse(result.Success, result.Error));
    }

    private static EventDto MapEvent(GetEvents.EventDto e)
        => new(e.Id, e.Name, e.Description, e.Date, e.Currency, e.CurrencySymbol, e.IsActive, e.CreatedAt);
}
