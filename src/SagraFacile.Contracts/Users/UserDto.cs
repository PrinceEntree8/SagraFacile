namespace SagraFacile.Contracts.Users;

public record UserDto(string Id, string UserName, string DisplayName, string Email, List<string> Roles);