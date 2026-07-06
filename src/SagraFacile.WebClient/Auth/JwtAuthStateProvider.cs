using System.Security.Claims;
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
        {
            await tokenStorageService.RemoveTokenAsync();
            return Anonymous;
        }

        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public async Task NotifyUserAuthenticationAsync(string token)
    {
        var identity = BuildIdentity(token);

        if (identity is null)
        {
            await tokenStorageService.RemoveTokenAsync();
        }
        else
        {
            await tokenStorageService.SetTokenAsync(token);
        }

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
        return JwtTokenValidator.ValidateAndParse(token);
    }
}
