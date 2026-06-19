namespace SagraFacile.Contracts.Events;

public record UpdateEventAdditionalOptionsRequest(bool IsPartyCompletionEnabled, int MinPartySize);