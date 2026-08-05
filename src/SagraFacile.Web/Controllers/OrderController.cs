using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SagraFacile.Application.Features.Orders;
using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Contracts.Ordering;
using SagraFacile.Domain.Features.Orders;
using SagraFacile.Web.Extensions;

namespace SagraFacile.Web.Controllers;

[ApiController]
[Route("api/events/{eventId:int}/orders")]
public class OrderController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Admin,Supervisore,Cassiere")]
    public async Task<IActionResult> CreateDraft(int eventId, [FromBody] CreateDraftOrderRequest request, CancellationToken ct)
    {
        var lines = request.Lines?.Select(l => new CreateDraftOrder.DraftLine(l.MenuItemId, l.Quantity, l.Notes)).ToList() ?? [];
        var result = await mediator.SendAsync(new CreateDraftOrder.Command(
            eventId, request.Context, request.ContextReferenceId, request.ContextLabel,
            request.Covers, request.CustomerName, request.Notes, lines, User.ToOrderActor()), ct);

        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("preorder")]
    [AllowAnonymous]
    public async Task<IActionResult> CreateAndSubmitPreorder(int eventId, [FromBody] SubmitPreorderRequest request, CancellationToken ct)
    {
        var actor = User.ToOrderActor();
        var draftLines = request.Lines?.Select(l => new CreateDraftOrder.DraftLine(l.MenuItemId, l.Quantity, l.Notes)).ToList() ?? [];
        var draftResult = await mediator.SendAsync(new CreateDraftOrder.Command(
            eventId, request.Context, request.ContextReferenceId, request.ContextLabel,
            request.Covers, request.CustomerName, request.Notes, draftLines, actor), ct);

        if (!draftResult.Success || draftResult.Data is null)
            return BadRequest(draftResult);

        var submitResult = await mediator.SendAsync(new SubmitPreorder.Command(draftResult.Data.OrderId, actor), ct);
        return submitResult.Success ? Ok(submitResult) : BadRequest(submitResult);
    }

    [HttpPost("{id:int}/submit")]
    [Authorize]
    public async Task<IActionResult> SubmitPreorder(int eventId, int id, CancellationToken ct)
    {
        var result = await mediator.SendAsync(new SubmitPreorder.Command(id, User.ToOrderActor()), ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{id:int}/confirm")]
    [Authorize(Roles = "Admin,Supervisore,Cassiere")]
    public async Task<IActionResult> Confirm(int eventId, int id, CancellationToken ct)
    {
        var result = await mediator.SendAsync(new ConfirmOrder.Command(id, User.ToOrderActor()), ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{id:int}/reject")]
    [Authorize(Roles = "Admin,Supervisore,Cassiere")]
    public async Task<IActionResult> Reject(int eventId, int id, [FromBody] RejectOrderRequest request, CancellationToken ct)
    {
        var result = await mediator.SendAsync(new RejectOrder.Command(id, User.ToOrderActor(), request?.Reason), ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{id:int}/cancel")]
    [Authorize]
    public async Task<IActionResult> Cancel(int eventId, int id, [FromBody] CancelOrderRequest? request, CancellationToken ct)
    {
        var result = await mediator.SendAsync(new CancelOrder.Command(id, User.ToOrderActor(), request?.Reason), ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{id:int}/advance")]
    [Authorize(Roles = "Admin,Supervisore,Cassiere,Cucina")]
    public async Task<IActionResult> Advance(int eventId, int id, [FromBody] AdvanceOrderRequest? request, CancellationToken ct)
    {
        var result = await mediator.SendAsync(new AdvanceOrderStatus.Command(id, User.ToOrderActor(), request?.TargetStatus, request?.Reason), ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("{id:int}/follow-up")]
    [Authorize(Roles = "Admin,Supervisore,Cassiere")]
    public async Task<IActionResult> CreateFollowUp(int eventId, int id, [FromBody] CreateFollowUpOrderRequest request, CancellationToken ct)
    {
        var lines = request.Lines?.Select(l => new CreateDraftOrder.DraftLine(l.MenuItemId, l.Quantity, l.Notes)).ToList() ?? [];
        var result = await mediator.SendAsync(new CreateFollowUpOrder.Command(id, lines, User.ToOrderActor()), ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<IActionResult> GetById(int eventId, int id, CancellationToken ct)
    {
        var result = await mediator.QueryAsync(new GetOrder.Query(id, User.ToOrderActor().Role), ct);
        return result is not null ? Ok(result) : NotFound();
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetPaged(
        int eventId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] OrderStatus[]? status = null,
        CancellationToken ct = default)
    {
        var result = await mediator.QueryAsync(new GetOrders.Query(eventId, page, pageSize, status), ct);
        return Ok(result);
    }
}
