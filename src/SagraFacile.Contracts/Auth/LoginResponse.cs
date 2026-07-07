namespace SagraFacile.Contracts.Auth;

public record LoginResponse(
    string Token,
    string Username,
    string DisplayName,
    IList<string> Roles,
    DateTimeOffset ExpiresAt);