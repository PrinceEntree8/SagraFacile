namespace SagraFacile.Contracts.Common;

public record CommandResult(bool Success, string? Message);
public record CommandResult<T>(bool Success, T? Data, string? Message = null) : CommandResult(Success, Message);