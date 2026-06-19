namespace SagraFacile.Contracts.Menu;

public record MenuDto(
    int EventId,
    MenuDetailsDto MenuDetails,
    List<MenuCategoryDto> MenuCategories,
    List<MenuItemDto> MenuItems);