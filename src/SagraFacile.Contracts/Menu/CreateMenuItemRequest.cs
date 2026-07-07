namespace SagraFacile.Contracts.Menu;

public record CreateMenuItemRequest(int CategoryId, string Name, string Description, int PriceCents, List<int> AllergenIds);