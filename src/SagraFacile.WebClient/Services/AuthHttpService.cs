using System.Net.Http.Json;
using SagraFacile.Contracts.Auth;

namespace SagraFacile.WebClient.Services;

public class AuthHttpService(HttpClient httpClient) : IAuthService
{
    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/auth/login", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: ct)
               ?? throw new InvalidOperationException("Login response was empty.");
    }

    public Task LogoutAsync(CancellationToken ct = default)
        => Task.CompletedTask;
}
