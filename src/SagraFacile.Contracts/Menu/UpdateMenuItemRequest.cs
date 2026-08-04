namespace SagraFacile.Contracts.Menu;

public record UpdateMenuItemRequest(
    string Name, 
    string Code,
    string Description, 
    int PriceCents, 
    int CategoryId, 
    List<int> AllergenIds, 
    bool IsAvailable = true);