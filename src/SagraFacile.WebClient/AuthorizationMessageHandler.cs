using System.Net.Http.Headers;
using SagraFacile.WebClient.Auth;

namespace SagraFacile.WebClient;

public class AuthorizationMessageHandler(TokenStorageService tokenStorageService) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await tokenStorageService.GetTokenAsync();
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await base.SendAsync(request, cancellationToken);
    }
}
