namespace SagraFacile.Contracts.Menu;

public record UpdateMenuItemRequest(
    string Name, 
    string Description, 
    int PriceCents, 
    int CategoryId, 
    List<int> AllergenIds, 
    bool IsAvailable = true);