using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SagraFacile.Application.Features.Menu;
using SagraFacile.Application.Infrastructure.CQRS;
using SagraFacile.Contracts.Menu;

namespace SagraFacile.Web.Controllers;

[ApiController]
[IgnoreAntiforgeryToken]
[Route("api/events/{eventId:int}/menu")]
public class MenuController(IMediator mediator) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("")]
    [EndpointName("Menu_GetItems")]
    [ProducesResponseType(typeof(IEnumerable<MenuItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMenu(int eventId, [FromQuery] bool includeUnavailable, CancellationToken ct)
    {
        var menuResult = await mediator.QueryAsync(new GetEventMenu.Query(eventId, includeUnavailable), ct);
        return Ok(menuResult.Items);
    }

    [AllowAnonymous]
    [HttpGet("details")]
    [EndpointName("Menu_GetDetails")]
    [ProducesResponseType(typeof(MenuDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GetMenuDetails(int eventId, CancellationToken ct)
    {
        var result = await mediator.QueryAsync(new GetMenuDetails.Query(eventId), ct);
        return result.Details is null ? NoContent() : Ok(result.Details);
    }

    [Authorize(Policy = "AdminOrSupervisore")]
    [HttpPut("details")]
    [EndpointName("Menu_UpdateDetails")]
    [ProducesResponseType(typeof(UpdateMenuDetailsResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateMenuDetails(int eventId, [FromBody] UpdateMenuDetailsRequest request, CancellationToken ct)
    {
        var result = await mediator.SendAsync(new UpdateMenuDetails.Command(eventId, request.WarningMessage, request.Header, request.Footer), ct);
        return Ok(new UpdateMenuDetailsResultResponse(result.Success));
    }

    [AllowAnonymous]
    [HttpGet("categories")]
    [EndpointName("Menu_GetCategories")]
    [ProducesResponseType(typeof(IEnumerable<MenuCategoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories(CancellationToken ct)
    {
        var result = await mediator.QueryAsync(new GetMenuCategories.Query(), ct);
        return Ok(result.Categories);
    }

    [Authorize(Policy = "AdminOrSupervisore")]
    [HttpPost("categories")]
    [EndpointName("Menu_CreateCategory")]
    [ProducesResponseType(typeof(CreateMenuCategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateCategory([FromBody] CreateMenuCategoryRequest request, CancellationToken ct)
    {
        var r = await mediator.SendAsync(new CreateMenuCategory.Command(request.Name, request.DisplayOrder), ct);
        return Ok(new CreateMenuCategoryResponse(r.Id, r.Name));
    }

    [Authorize(Policy = "AdminOrSupervisore")]
    [HttpPut("categories/{catId:int}")]
    [EndpointName("Menu_UpdateCategory")]
    [ProducesResponseType(typeof(UpdateMenuCategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateCategory(int catId, [FromBody] UpdateMenuCategoryRequest request, CancellationToken ct)
    {
        var r = await mediator.SendAsync(new UpdateMenuCategory.Command(catId, request.Name, request.DisplayOrder), ct);
        return Ok(new UpdateMenuCategoryResponse(r.Success, r.Message));
    }

    [Authorize(Policy = "AdminOrSupervisore")]
    [HttpDelete("categories/{catId:int}")]
    [EndpointName("Menu_DeleteCategory")]
    [ProducesResponseType(typeof(DeleteMenuCategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteCategory(int catId, CancellationToken ct)
    {
        var r = await mediator.SendAsync(new DeleteMenuCategory.Command(catId), ct);
        return Ok(new DeleteMenuCategoryResponse(r.Success, r.Message));
    }

    [Authorize(Policy = "AdminOrSupervisore")]
    [HttpPost("items")]
    [EndpointName("Menu_CreateItem")]
    [ProducesResponseType(typeof(CreateMenuItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateItem(int eventId, [FromBody] CreateMenuItemRequest request, CancellationToken ct)
    {
        var r = await mediator.SendAsync(new CreateMenuItem.Command(eventId, request.Name, request.Description, request.PriceCents, request.CategoryId, request.AllergenIds), ct);
        return Ok(new CreateMenuItemResponse(r.Id, r.Name));
    }

    [Authorize(Policy = "AdminOrSupervisore")]
    [HttpPut("items/{itemId:int}")]
    [EndpointName("Menu_UpdateItem")]
    [ProducesResponseType(typeof(UpdateMenuItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateItem(int itemId, [FromBody] UpdateMenuItemRequest request, CancellationToken ct)
    {
        var r = await mediator.SendAsync(new UpdateMenuItem.Command(itemId, request.Name, request.Description, request.PriceCents, request.CategoryId, request.AllergenIds, request.IsAvailable), ct);
        return Ok(new UpdateMenuItemResponse(r.Success, r.Message));
    }

    [Authorize(Policy = "AdminOrSupervisore")]
    [HttpDelete("items/{itemId:int}")]
    [EndpointName("Menu_DeleteItem")]
    [ProducesResponseType(typeof(DeleteMenuItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteItem(int itemId, CancellationToken ct)
    {
        var r = await mediator.SendAsync(new DeleteMenuItem.Command(itemId), ct);
        return Ok(new DeleteMenuItemResponse(r.Success, r.Message));
    }

    [AllowAnonymous]
    [HttpGet("/api/allergens")]
    [EndpointName("Menu_GetAllergens")]
    [ProducesResponseType(typeof(IEnumerable<AllergenDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllergens(CancellationToken ct)
    {
        var result = await mediator.QueryAsync(new GetAllergens.Query(), ct);
        return Ok(result.Allergens);
    }
}
