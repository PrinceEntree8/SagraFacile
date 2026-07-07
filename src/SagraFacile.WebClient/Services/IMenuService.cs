using SagraFacile.Contracts.Menu;

namespace SagraFacile.WebClient.Services;

public interface IMenuService
{
    Task<IReadOnlyList<MenuItemDto>> GetMenuAsync(int eventId, bool includeUnavailable = false, CancellationToken ct = default);
    Task<IReadOnlyList<MenuCategoryDto>> GetCategoriesAsync(int eventId, CancellationToken ct = default);
    Task<CreateMenuCategoryResponse> CreateCategoryAsync(int eventId, CreateMenuCategoryRequest request, CancellationToken ct = default);
    Task<UpdateMenuCategoryResponse> UpdateCategoryAsync(int eventId, int categoryId, UpdateMenuCategoryRequest request, CancellationToken ct = default);
    Task<DeleteMenuCategoryResponse> DeleteCategoryAsync(int eventId, int categoryId, CancellationToken ct = default);
    Task<CreateMenuItemResponse> CreateItemAsync(int eventId, CreateMenuItemRequest request, CancellationToken ct = default);
    Task<UpdateMenuItemResponse> UpdateItemAsync(int eventId, int itemId, UpdateMenuItemRequest request, CancellationToken ct = default);
    Task<DeleteMenuItemResponse> DeleteItemAsync(int eventId, int itemId, CancellationToken ct = default);
    Task<IReadOnlyList<AllergenDto>> GetAllergensAsync(CancellationToken ct = default);
    Task<MenuDetailsDto> GetMenuDetailsAsync(int eventId, CancellationToken ct = default);
    Task<UpdateMenuDetailsResponse> UpdateMenuDetailsAsync(int eventId, UpdateMenuDetailsRequest request, CancellationToken ct = default);
}
