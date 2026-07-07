using Microsoft.AspNetCore.Components.Authorization;
using SagraFacile.Contracts.Auth;
using SagraFacile.WebClient.Services;

namespace SagraFacile.WebClient.Auth;

public class AuthService(IAuthService authHttpService, AuthenticationStateProvider authenticationStateProvider)
{
    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var login = await authHttpService.LoginAsync(request, ct);

        if (authenticationStateProvider is JwtAuthStateProvider jwtAuthStateProvider)
            await jwtAuthStateProvider.NotifyUserAuthenticationAsync(login.Token);

        return login;
    }

    public async Task LogoutAsync()
    {
        if (authenticationStateProvider is JwtAuthStateProvider jwtAuthStateProvider)
            await jwtAuthStateProvider.NotifyUserLogoutAsync();
    }
}
