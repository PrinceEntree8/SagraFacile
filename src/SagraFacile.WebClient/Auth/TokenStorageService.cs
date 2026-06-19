using Microsoft.JSInterop;

namespace SagraFacile.WebClient.Auth;

public class TokenStorageService(IJSRuntime jsRuntime)
{
    private const string TokenStorageKey = "auth.token";

    public ValueTask<string?> GetTokenAsync()
        => jsRuntime.InvokeAsync<string?>("localStorage.getItem", TokenStorageKey);

    public ValueTask SetTokenAsync(string token)
        => jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenStorageKey, token);

    public ValueTask RemoveTokenAsync()
        => jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenStorageKey);
}
