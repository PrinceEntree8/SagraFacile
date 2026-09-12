using Refit;
using SagraFacile.Contracts.Menu;

namespace SagraFacile.WebClient.Services;

public interface IMenuService
{
    [Get("/api/events/{eventId}/menu")]
    Task<IReadOnlyList<MenuItemDto>> GetMenuAsync(int eventId, [Query] bool includeUnavailable = false, CancellationToken ct = default);

    [Get("/api/events/{eventId}/menu/categories")]
    Task<IReadOnlyList<MenuCategoryDto>> GetCategoriesAsync(int eventId, CancellationToken ct = default);

    [Post("/api/events/{eventId}/menu/categories")]
    Task<CreateMenuCategoryResponse> CreateCategoryAsync(int eventId, [Body] CreateMenuCategoryRequest request, CancellationToken ct = default);

    [Put("/api/events/{eventId}/menu/categories/{categoryId}")]
    Task<UpdateMenuCategoryResponse> UpdateCategoryAsync(int eventId, int categoryId, [Body] UpdateMenuCategoryRequest request, CancellationToken ct = default);

    [Delete("/api/events/{eventId}/menu/categories/{categoryId}")]
    Task<DeleteMenuCategoryResponse> DeleteCategoryAsync(int eventId, int categoryId, CancellationToken ct = default);

    [Post("/api/events/{eventId}/menu/items")]
    Task<CreateMenuItemResponse> CreateItemAsync(int eventId, [Body] CreateMenuItemRequest request, CancellationToken ct = default);

    [Put("/api/events/{eventId}/menu/items/{itemId}")]
    Task<UpdateMenuItemResponse> UpdateItemAsync(int eventId, int itemId, [Body] UpdateMenuItemRequest request, CancellationToken ct = default);

    [Delete("/api/events/{eventId}/menu/items/{itemId}")]
    Task<DeleteMenuItemResponse> DeleteItemAsync(int eventId, int itemId, CancellationToken ct = default);

    [Get("/api/allergens")]
    Task<IReadOnlyList<AllergenDto>> GetAllergensAsync(CancellationToken ct = default);

    [Get("/api/events/{eventId}/menu/details")]
    Task<MenuDetailsDto?> GetMenuDetailsAsync(int eventId, CancellationToken ct = default);

    [Put("/api/events/{eventId}/menu/details")]
    Task<UpdateMenuDetailsResponse> UpdateMenuDetailsAsync(int eventId, [Body] UpdateMenuDetailsRequest request, CancellationToken ct = default);
}
