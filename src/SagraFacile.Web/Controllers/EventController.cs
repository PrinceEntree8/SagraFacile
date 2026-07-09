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
    public async Task<IActionResult> GetEvents(CancellationToken ct)
    {
        var result = await mediator.QueryAsync(new GetEvents.Query(), ct);
        return Ok(result.Events.Select(MapEvent));
    }

    [AllowAnonymous]
    [HttpGet("active")]
    public async Task<IActionResult> GetActiveEvent(CancellationToken ct)
    {
        var result = await mediator.QueryAsync(new GetActiveEvent.Query(), ct);
        if (result.ActiveEvent is null)
            return Ok(null);

        var e = result.ActiveEvent;
        return Ok(new EventDto(e.Id, e.Name, string.Empty, DateTime.UtcNow, e.Currency, e.CurrencySymbol, true, DateTime.UtcNow));
    }

    [Authorize(Policy = "AdminOrSupervisore")]
    [HttpGet("{id:int}")]
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
    public async Task<IActionResult> Create([FromBody] CreateEventRequest request, CancellationToken ct)
        => Ok(await mediator.SendAsync(new CreateEvent.Command(request.Name, request.Description, request.Date, request.Currency, request.CurrencySymbol), ct));

    [Authorize(Policy = "AdminOnly")]
    [HttpPut("{id:int}/activate")]
    public async Task<IActionResult> Activate(int id, CancellationToken ct)
        => Ok(await mediator.SendAsync(new ActivateEvent.Command(id), ct));

    [Authorize(Policy = "AdminOrSupervisore")]
    [HttpGet("{id:int}/options")]
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
    public async Task<IActionResult> UpdateOptions(int id, [FromBody] UpdateEventAdditionalOptionsRequest request, CancellationToken ct)
        => Ok(await mediator.SendAsync(new UpdateEventAdditionalOptions.Command(
            id,
            request.IsPartyCompletionEnabled,
            request.MinPartySize,
            request.ShowNotesField,
            request.CounterPeopleFirst,
            request.ShowCallCount,
            request.MaxWaitTimeMinutes), ct));

    private static EventDto MapEvent(GetEvents.EventDto e)
        => new(e.Id, e.Name, e.Description, e.Date, e.Currency, e.CurrencySymbol, e.IsActive, e.CreatedAt);
}