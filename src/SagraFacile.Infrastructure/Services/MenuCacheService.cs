using Microsoft.Extensions.Caching.Memory;
using SagraFacile.Application.Features.Menu;
using SagraFacile.Application.Interfaces;

namespace SagraFacile.Infrastructure.Services;

public class MenuCacheService(IMemoryCache cache) : IMenuCacheService
{
    private static string ItemsKey(int eventId) => $"menu-items:{eventId}";
    private static string DetailsKey(int eventId) => $"menu-details:{eventId}";
    private static readonly MemoryCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
    };

    public bool TryGetMenuItems(int eventId, out GetEventMenu.Result? result)
        => cache.TryGetValue(ItemsKey(eventId), out result);

    public void SetMenuItems(int eventId, GetEventMenu.Result result)
        => cache.Set(ItemsKey(eventId), result, CacheOptions);

    public bool TryGetMenuDetails(int eventId, out GetMenuDetails.Result? result)
        => cache.TryGetValue(DetailsKey(eventId), out result);

    public void SetMenuDetails(int eventId, GetMenuDetails.Result result)
        => cache.Set(DetailsKey(eventId), result, CacheOptions);

    public void InvalidateMenu(int eventId)
    {
        cache.Remove(ItemsKey(eventId));
        cache.Remove(DetailsKey(eventId));
    }
}
