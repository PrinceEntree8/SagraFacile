namespace SagraFacile.Contracts.Users;

public record CreateUserRequest(string UserName, string DisplayName, string Email, string Password, List<string> Roles);