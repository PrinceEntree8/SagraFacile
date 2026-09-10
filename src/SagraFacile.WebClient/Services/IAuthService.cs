using Refit;
using SagraFacile.Contracts.Auth;

namespace SagraFacile.WebClient.Services;

public interface IAuthService
{
    [Post("/api/auth/login")]
    Task<LoginResponse> LoginAsync([Body] LoginRequest request, CancellationToken ct = default);
}
