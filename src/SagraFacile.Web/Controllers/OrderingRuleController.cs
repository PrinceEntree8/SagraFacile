using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SagraFacile.Application.Features.OrderingRules;
using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Application.Interfaces;
using SagraFacile.Contracts.Ordering;

namespace SagraFacile.Web.Controllers;

[ApiController]
[Route("api/events/{eventId:int}/ordering-rules")]
public class OrderingRuleController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "AdminOrSupervisore")]
    public async Task<IActionResult> List(int eventId, CancellationToken ct)
    {
        var result = await mediator.QueryAsync(new GetOrderingRules.Query(eventId), ct);
        return Ok(result.Rules);
    }

    [HttpPost]
    [Authorize(Policy = "AdminOrSupervisore")]
    public async Task<IActionResult> Create(int eventId, [FromBody] CreateOrderingRuleRequest request, CancellationToken ct)
    {
        var result = await mediator.SendAsync(new CreateOrderingRule.Command(
            eventId,
            request.Scope,
            request.TargetCode,
            request.Expression,
            request.ErrorMessage,
            request.WarningMessage,
            request.Priority,
            request.IsActive), ct);

        return Ok(new OrderingRuleActionResponse(result.Success, result.Message));
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "AdminOrSupervisore")]
    public async Task<IActionResult> Update(int eventId, int id, [FromBody] UpdateOrderingRuleRequest request, CancellationToken ct)
    {
        var result = await mediator.SendAsync(new UpdateOrderingRule.Command(
            id,
            request.Scope,
            request.TargetCode,
            request.Expression,
            request.ErrorMessage,
            request.WarningMessage,
            request.Priority), ct);

        return Ok(new OrderingRuleActionResponse(result.Success, result.Message));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOrSupervisore")]
    public async Task<IActionResult> Delete(int eventId, int id, CancellationToken ct)
    {
        var result = await mediator.SendAsync(new DeleteOrderingRule.Command(id), ct);
        return Ok(new OrderingRuleActionResponse(result.Success, result.Message));
    }

    [HttpPatch("{id:int}/active")]
    [Authorize(Policy = "AdminOrSupervisore")]
    public async Task<IActionResult> SetActive(int eventId, int id, [FromBody] SetOrderingRuleActiveRequest request, CancellationToken ct)
    {
        var result = await mediator.SendAsync(new SetOrderingRuleActive.Command(id, request.IsActive), ct);
        return Ok(new OrderingRuleActionResponse(result.Success, result.Message));
    }

    [HttpPost("validate")]
    [Authorize(Roles = "Admin,Supervisore,Cassiere")]
    public async Task<IActionResult> ValidateComposition(int eventId, [FromBody] ValidateOrderCompositionRequest request, CancellationToken ct)
    {
        var lines = request.Lines?.Select(l => new OrderValidationLine(l.MenuItemId, l.Quantity)).ToList()
                    ?? new List<OrderValidationLine>();

        var result = await mediator.QueryAsync(new ValidateOrderComposition.Query(eventId, request.Covers, lines), ct);
        return Ok(result);
    }
}
