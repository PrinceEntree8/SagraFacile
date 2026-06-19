using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SagraFacile.Contracts.Users;
using SagraFacile.Infrastructure.Identity;

namespace SagraFacile.Web.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Policy = "AdminOnly")]
public class UserController(UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var users = userManager.Users.ToList();
        var dtos = new List<UserDto>();

        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            dtos.Add(new UserDto(
                user.Id,
                user.UserName ?? string.Empty,
                user.DisplayName,
                user.Email ?? string.Empty,
                roles.ToList()));
        }

        return Ok(dtos);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var user = new ApplicationUser
        {
            UserName = request.UserName,
            DisplayName = request.DisplayName,
            Email = request.Email,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors.Select(e => e.Description));

        if (request.Roles.Count > 0)
        {
            var roleResult = await userManager.AddToRolesAsync(user, request.Roles);
            if (!roleResult.Succeeded)
                return BadRequest(roleResult.Errors.Select(e => e.Description));
        }

        return Ok(new UserDto(user.Id, user.UserName ?? string.Empty, user.DisplayName, user.Email ?? string.Empty, request.Roles));
    }

    [HttpGet("{id}/roles")]
    public async Task<IActionResult> GetRoles(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
            return NotFound();

        var roles = await userManager.GetRolesAsync(user);
        return Ok(roles);
    }

    [HttpPost("{id}/roles")]
    public async Task<IActionResult> AssignRoles(string id, [FromBody] AssignRolesRequest request)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
            return NotFound();

        var current = await userManager.GetRolesAsync(user);
        if (current.Count > 0)
        {
            var removeResult = await userManager.RemoveFromRolesAsync(user, current);
            if (!removeResult.Succeeded)
                return BadRequest(removeResult.Errors.Select(e => e.Description));
        }

        if (request.Roles.Count > 0)
        {
            var addResult = await userManager.AddToRolesAsync(user, request.Roles);
            if (!addResult.Succeeded)
                return BadRequest(addResult.Errors.Select(e => e.Description));
        }

        return Ok();
    }
}