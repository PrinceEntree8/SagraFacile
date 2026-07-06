using System.Security.Claims;
using System.Text.Json;

namespace SagraFacile.WebClient.Auth;

public static class JwtTokenValidator
{
    private static readonly string[] RequiredIdentityClaimTypes = [ClaimTypes.NameIdentifier, "sub", "unique_name", ClaimTypes.Name];
    private const string ExpirationClaimType = "exp";

    public static ClaimsIdentity? ValidateAndParse(string token)
    {
        return TryParseValidatedClaims(token, out var claims)
            ? new ClaimsIdentity(claims, authenticationType: "jwt")
            : null;
    }

    private static bool TryParseValidatedClaims(string token, out List<Claim> claims)
    {
        claims = [];

        if (!TryParsePayload(token, out var payloadClaims))
            return false;

        claims = ParseClaims(payloadClaims);
        return HasRequiredIdentityClaims(claims) && HasValidExpiration(claims);
    }

    private static bool TryParsePayload(string token, out Dictionary<string, JsonElement> payloadClaims)
    {
        payloadClaims = [];

        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3)
                return false;

            var payload = parts[1]
                .Replace('-', '+')
                .Replace('_', '/');

            var mod = payload.Length % 4;
            if (mod > 0)
                payload = payload.PadRight(payload.Length + (4 - mod), '=');

            var payloadJson = Convert.FromBase64String(payload);
            var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payloadJson);
            if (keyValuePairs is null)
                return false;

            payloadClaims = keyValuePairs;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static List<Claim> ParseClaims(Dictionary<string, JsonElement> keyValuePairs)
    {
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

    private static bool HasRequiredIdentityClaims(IEnumerable<Claim> claims)
    {
        return claims.Any(claim =>
            RequiredIdentityClaimTypes.Contains(claim.Type, StringComparer.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(claim.Value));
    }

    private static bool HasValidExpiration(IEnumerable<Claim> claims)
    {
        var expiration = claims.FirstOrDefault(claim =>
            string.Equals(claim.Type, ExpirationClaimType, StringComparison.OrdinalIgnoreCase));

        if (expiration is null || !long.TryParse(expiration.Value, out var expSeconds))
            return false;

        var expirationUtc = DateTimeOffset.FromUnixTimeSeconds(expSeconds);
        return expirationUtc > DateTimeOffset.UtcNow;
    }
}
