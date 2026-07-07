using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public static class AuthService
{
    public static async Task<string> LoginAsync(TestConfig config, CancellationToken cancellationToken = default)
    {
        using var httpClient = new HttpClient
        {
            BaseAddress = new Uri(config.BaseUrl)
        };

        using var response = await httpClient.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest(config.Username, config.Password),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Authentication failed with status {(int)response.StatusCode} ({response.StatusCode}). Response: {errorBody}");
        }

        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>(
            cancellationToken: cancellationToken);

        if (string.IsNullOrWhiteSpace(loginResponse?.Token))
            throw new JsonException("Authentication response does not contain a valid token.");

        return loginResponse.Token;
    }

    private sealed record LoginRequest(string Username, string Password);

    private sealed record LoginResponse(string Token);
}
