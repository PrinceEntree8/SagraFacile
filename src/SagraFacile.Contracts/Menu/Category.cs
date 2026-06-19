namespace SagraFacile.Contracts.Menu;

public record MenuCategoryDto(int Id, string Name, int DisplayOrder, List<MenuItemDto> Items);

public record CreateMenuCategoryRequest(string Name, int DisplayOrder);
public record CreateMenuCategoryResponse(int Id, string Name);

public record UpdateMenuCategoryRequest(string Name, int DisplayOrder);
public record UpdateMenuCategoryResponse(bool Success, string Message);

public record DeleteMenuCategoryResponse(bool Success, string Message);