namespace SagraFacile.Contracts.Menu;

public record MenuDetailsDto(string? Header, string? Footer, string? WarningMessage);

public record UpdateMenuDetailsRequest(string? Header, string? Footer, string? WarningMessage);
public record UpdateMenuDetailsResponse(string? Header, string? Footer, string? WarningMessage);
