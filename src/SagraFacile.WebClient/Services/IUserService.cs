using SagraFacile.Contracts.Users;

namespace SagraFacile.WebClient.Services;

public interface IUserService
{
    Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken ct = default);
    Task<UserDto> CreateUserAsync(CreateUserRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetRolesAsync(string userId, CancellationToken ct = default);
    Task AssignRolesAsync(string userId, AssignRolesRequest request, CancellationToken ct = default);
}
