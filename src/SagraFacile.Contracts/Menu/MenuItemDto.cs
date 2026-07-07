namespace SagraFacile.Contracts.Menu;

public record MenuItemDto(
    int Id,
    string Name,
    string Description,
    int PriceCents,
    int CategoryId,
    string CategoryName,
    int DisplayOrder,
    bool IsAvailable,
    List<AllergenDto> Allergens);

public record CreateMenuItemResponse(int Id, string Name);
public record UpdateMenuItemResponse(bool Success, string Message);
public record DeleteMenuItemResponse(bool Success, string Message);