using SagraFacile.Application.Features.Menu;

namespace SagraFacile.Application.Interfaces;

public interface IMenuCacheService
{
    bool TryGetMenuItems(int eventId, out GetEventMenu.Result? result);
    void SetMenuItems(int eventId, GetEventMenu.Result result);
    bool TryGetMenuDetails(int eventId, out GetMenuDetails.Result? result);
    void SetMenuDetails(int eventId, GetMenuDetails.Result result);
    void InvalidateMenu(int eventId);
}
