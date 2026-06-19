using System.Net.Http.Json;
using SagraFacile.Contracts.Users;

namespace SagraFacile.WebClient.Services;

public class UserService(HttpClient httpClient) : IUserService
{
    public async Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken ct = default)
        => await httpClient.GetFromJsonAsync<List<UserDto>>("api/users", ct) ?? [];

    public async Task<UserDto> CreateUserAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/users", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserDto>(cancellationToken: ct)
               ?? throw new InvalidOperationException("Create user response was empty.");
    }

    public async Task<IReadOnlyList<string>> GetRolesAsync(string userId, CancellationToken ct = default)
        => await httpClient.GetFromJsonAsync<List<string>>($"api/users/{userId}/roles", ct) ?? [];

    public async Task AssignRolesAsync(string userId, AssignRolesRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync($"api/users/{userId}/roles", request, ct);
        response.EnsureSuccessStatusCode();
    }
}
