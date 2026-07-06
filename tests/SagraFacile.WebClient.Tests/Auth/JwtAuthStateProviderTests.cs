using System.Text;
using System.Text.Json;
using Microsoft.JSInterop;
using SagraFacile.WebClient.Auth;

namespace SagraFacile.WebClient.Tests.Auth;

public class JwtAuthStateProviderTests
{
    [Fact]
    public async Task GetAuthenticationStateAsync_ValidToken_ReturnsAuthenticatedUser()
    {
        var jsRuntime = new FakeJsRuntime();
        var tokenStorageService = new TokenStorageService(jsRuntime);
        var token = CreateToken(DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds(), "user-1", "operator");
        await tokenStorageService.SetTokenAsync(token);
        var provider = new JwtAuthStateProvider(tokenStorageService);

        var authState = await provider.GetAuthenticationStateAsync();

        Assert.True(authState.User.Identity?.IsAuthenticated);
        Assert.Equal("user-1", authState.User.FindFirst("sub")?.Value);
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_ExpiredToken_ReturnsAnonymousUser()
    {
        var jsRuntime = new FakeJsRuntime();
        var tokenStorageService = new TokenStorageService(jsRuntime);
        var token = CreateToken(DateTimeOffset.UtcNow.AddMinutes(-1).ToUnixTimeSeconds(), "user-1", "operator");
        await tokenStorageService.SetTokenAsync(token);
        var provider = new JwtAuthStateProvider(tokenStorageService);

        var authState = await provider.GetAuthenticationStateAsync();

        Assert.False(authState.User.Identity?.IsAuthenticated);
        Assert.Null(await tokenStorageService.GetTokenAsync());
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_MalformedToken_ReturnsAnonymousUser()
    {
        var jsRuntime = new FakeJsRuntime();
        var tokenStorageService = new TokenStorageService(jsRuntime);
        await tokenStorageService.SetTokenAsync("invalid-token");
        var provider = new JwtAuthStateProvider(tokenStorageService);

        var authState = await provider.GetAuthenticationStateAsync();

        Assert.False(authState.User.Identity?.IsAuthenticated);
        Assert.Null(await tokenStorageService.GetTokenAsync());
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_MissingIdentityClaim_ReturnsAnonymousUser()
    {
        var jsRuntime = new FakeJsRuntime();
        var tokenStorageService = new TokenStorageService(jsRuntime);
        var token = CreateTokenWithoutIdentity(DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds());
        await tokenStorageService.SetTokenAsync(token);
        var provider = new JwtAuthStateProvider(tokenStorageService);

        var authState = await provider.GetAuthenticationStateAsync();

        Assert.False(authState.User.Identity?.IsAuthenticated);
        Assert.Null(await tokenStorageService.GetTokenAsync());
    }

    private static string CreateToken(long expUnixSeconds, string sub, string role)
    {
        var payload = new Dictionary<string, object>
        {
            ["sub"] = sub,
            ["exp"] = expUnixSeconds,
            ["role"] = role
        };

        return BuildJwt(payload);
    }

    private static string CreateTokenWithoutIdentity(long expUnixSeconds)
    {
        var payload = new Dictionary<string, object>
        {
            ["exp"] = expUnixSeconds,
            ["role"] = "operator"
        };

        return BuildJwt(payload);
    }

    private static string BuildJwt(Dictionary<string, object> payload)
    {
        var header = new Dictionary<string, object>
        {
            ["alg"] = "none",
            ["typ"] = "JWT"
        };

        return $"{Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(header))}.{Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(payload))}.";
    }

    private static string Base64UrlEncode(byte[] data)
    {
        return Convert.ToBase64String(data)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private sealed class FakeJsRuntime : IJSRuntime
    {
        private readonly Dictionary<string, string> _storage = [];

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            return identifier switch
            {
                "localStorage.getItem" => new ValueTask<TValue>((TValue)(object?)GetItem(args)!),
                "localStorage.setItem" => SetItem<TValue>(args),
                "localStorage.removeItem" => RemoveItem<TValue>(args),
                _ => throw new NotSupportedException($"Unsupported JS interop method: {identifier}")
            };
        }

        private string? GetItem(object?[]? args)
        {
            var key = args?[0]?.ToString();
            return key is not null && _storage.TryGetValue(key, out var value) ? value : null;
        }

        private ValueTask<TValue> SetItem<TValue>(object?[]? args)
        {
            var key = args?[0]?.ToString();
            var value = args?[1]?.ToString();
            if (!string.IsNullOrWhiteSpace(key) && value is not null)
                _storage[key] = value;

            return new ValueTask<TValue>((TValue)(object?)null!);
        }

        private ValueTask<TValue> RemoveItem<TValue>(object?[]? args)
        {
            var key = args?[0]?.ToString();
            if (!string.IsNullOrWhiteSpace(key))
                _storage.Remove(key);

            return new ValueTask<TValue>((TValue)(object?)null!);
        }
    }
}