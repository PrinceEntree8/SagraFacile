namespace SagraFacile.Contracts.Events;

public record CreateEventResponse(int Id, string Name);

public record ActivateEventResponse(bool Success, string Message);

public record UpdateEventOptionsResponse(bool Success, string? Error);
