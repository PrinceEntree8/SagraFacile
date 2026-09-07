using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace SagraFacile.Web.Auth;

/// <summary>
/// Minimal server-side <see cref="AuthenticationStateProvider"/> used during static SSR rendering.
/// Public pages are anonymous; operative pages run as InteractiveWebAssembly and rely on
/// <c>JwtAuthStateProvider</c> (registered in SagraFacile.WebClient) once the WASM circuit boots.
/// </summary>
public class AnonymousAuthenticationStateProvider : AuthenticationStateProvider
{
    private static readonly Task<AuthenticationState> AnonymousState =
        Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));

    public override Task<AuthenticationState> GetAuthenticationStateAsync() => AnonymousState;
}
