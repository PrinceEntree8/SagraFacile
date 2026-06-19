using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;

namespace SagraFacile.WebClient.Auth;

public class JwtAuthStateProvider(TokenStorageService tokenStorageService) : AuthenticationStateProvider
{
    private static readonly AuthenticationState Anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await tokenStorageService.GetTokenAsync();
        if (string.IsNullOrWhiteSpace(token))
            return Anonymous;

        var identity = BuildIdentity(token);
        if (identity is null)
            return Anonymous;

        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public async Task NotifyUserAuthenticationAsync(string token)
    {
        await tokenStorageService.SetTokenAsync(token);
        var identity = BuildIdentity(token);
        NotifyAuthenticationStateChanged(Task.FromResult(
            identity is null ? Anonymous : new AuthenticationState(new ClaimsPrincipal(identity))));
    }

    public async Task NotifyUserLogoutAsync()
    {
        await tokenStorageService.RemoveTokenAsync();
        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
    }

    private static ClaimsIdentity? BuildIdentity(string token)
    {
        try
        {
            var claims = ParseClaims(token);
            return claims.Count == 0 ? null : new ClaimsIdentity(claims, authenticationType: "jwt");
        }
        catch
        {
            return null;
        }
    }

    private static List<Claim> ParseClaims(string token)
    {
        var parts = token.Split('.');
        if (parts.Length < 2)
            return [];

        var payload = parts[1]
            .Replace('-', '+')
            .Replace('_', '/');

        var mod = payload.Length % 4;
        if (mod > 0)
            payload = payload.PadRight(payload.Length + (4 - mod), '=');

        var payloadJson = Convert.FromBase64String(payload);
        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payloadJson);
        if (keyValuePairs is null)
            return [];

        var claims = new List<Claim>();
        foreach (var (key, value) in keyValuePairs)
        {
            if (key == ClaimTypes.Role || key == "role" || key == "roles")
            {
                if (value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in value.EnumerateArray())
                    {
                        var role = item.GetString();
                        if (!string.IsNullOrWhiteSpace(role))
                            claims.Add(new Claim(ClaimTypes.Role, role));
                    }
                }
                else
                {
                    var role = value.GetString();
                    if (!string.IsNullOrWhiteSpace(role))
                        claims.Add(new Claim(ClaimTypes.Role, role));
                }

                continue;
            }

            claims.Add(new Claim(key, value.ToString()));
        }

        return claims;
    }
}
