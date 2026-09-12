using Refit;
using SagraFacile.Contracts.Users;

namespace SagraFacile.WebClient.Services;

public interface IUserService
{
    [Get("/api/users")]
    Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken ct = default);

    [Post("/api/users")]
    Task<UserDto> CreateUserAsync([Body] CreateUserRequest request, CancellationToken ct = default);

    [Get("/api/users/{userId}/roles")]
    Task<IReadOnlyList<string>> GetRolesAsync(string userId, CancellationToken ct = default);

    [Post("/api/users/{userId}/roles")]
    Task AssignRolesAsync(string userId, [Body] AssignRolesRequest request, CancellationToken ct = default);
}
