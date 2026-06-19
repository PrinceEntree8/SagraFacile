using SagraFacile.Contracts.Auth;

namespace SagraFacile.WebClient.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task LogoutAsync(CancellationToken ct = default);
}
