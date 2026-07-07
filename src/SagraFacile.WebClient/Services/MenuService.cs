using System.Net.Http.Json;
using SagraFacile.Contracts.Menu;

namespace SagraFacile.WebClient.Services;

public class MenuService(HttpClient httpClient) : IMenuService
{
    public async Task<IReadOnlyList<MenuItemDto>> GetMenuAsync(int eventId, bool includeUnavailable = false, CancellationToken ct = default)
        => await httpClient.GetFromJsonAsync<List<MenuItemDto>>($"api/events/{eventId}/menu?includeUnavailable={includeUnavailable.ToString().ToLowerInvariant()}", ct) ?? [];

    public async Task<IReadOnlyList<MenuCategoryDto>> GetCategoriesAsync(int eventId, CancellationToken ct = default)
        => await httpClient.GetFromJsonAsync<List<MenuCategoryDto>>($"api/events/{eventId}/menu/categories", ct) ?? [];

    public async Task<CreateMenuCategoryResponse> CreateCategoryAsync(int eventId, CreateMenuCategoryRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync($"api/events/{eventId}/menu/categories", request, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreateMenuCategoryResponse>(cancellationToken: ct))!;
    }

    public async Task<UpdateMenuCategoryResponse> UpdateCategoryAsync(int eventId, int categoryId, UpdateMenuCategoryRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PutAsJsonAsync($"api/events/{eventId}/menu/categories/{categoryId}", request, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UpdateMenuCategoryResponse>(cancellationToken: ct))!;
    }

    public async Task<DeleteMenuCategoryResponse> DeleteCategoryAsync(int eventId, int categoryId, CancellationToken ct = default)
    {
        var response = await httpClient.DeleteAsync($"api/events/{eventId}/menu/categories/{categoryId}", ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<DeleteMenuCategoryResponse>(cancellationToken: ct))!;
    }

    public async Task<CreateMenuItemResponse> CreateItemAsync(int eventId, CreateMenuItemRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync($"api/events/{eventId}/menu/items", request, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreateMenuItemResponse>(cancellationToken: ct))!;
    }

    public async Task<UpdateMenuItemResponse> UpdateItemAsync(int eventId, int itemId, UpdateMenuItemRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PutAsJsonAsync($"api/events/{eventId}/menu/items/{itemId}", request, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UpdateMenuItemResponse>(cancellationToken: ct))!;
    }

    public async Task<DeleteMenuItemResponse> DeleteItemAsync(int eventId, int itemId, CancellationToken ct = default)
    {
        var response = await httpClient.DeleteAsync($"api/events/{eventId}/menu/items/{itemId}", ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<DeleteMenuItemResponse>(cancellationToken: ct))!;
    }

    public async Task<IReadOnlyList<AllergenDto>> GetAllergensAsync(CancellationToken ct = default)
        => await httpClient.GetFromJsonAsync<List<AllergenDto>>("api/allergens", ct) ?? [];

    public Task<MenuDetailsDto?> GetMenuDetailsAsync(int eventId, CancellationToken ct = default) 
        => httpClient.GetFromJsonAsync<MenuDetailsDto?>($"api/events/{eventId}/menu/details", ct);

    public async Task<UpdateMenuDetailsResponse> UpdateMenuDetailsAsync(int eventId, UpdateMenuDetailsRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PutAsJsonAsync($"api/events/{eventId}/menu/details", request, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UpdateMenuDetailsResponse>(cancellationToken: ct))!;
    }
}
