namespace SagraFacile.Contracts.Events;

public record CreateEventRequest(string Name, string Description, DateTime Date, string Currency, string CurrencySymbol);