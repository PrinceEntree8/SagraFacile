namespace SagraFacile.Contracts.Menu;

public record MenuCategoryDto(int Id, string Name, string Code, int DisplayOrder, List<MenuItemDto> Items);

public record CreateMenuCategoryRequest(string Name, string Code, int DisplayOrder = 0);
public record CreateMenuCategoryResponse(int Id, string Name, string Code);

public record UpdateMenuCategoryRequest(string Name, string Code, int DisplayOrder = 0);
public record UpdateMenuCategoryResponse(bool Success, string Message);

public record DeleteMenuCategoryResponse(bool Success, string Message);