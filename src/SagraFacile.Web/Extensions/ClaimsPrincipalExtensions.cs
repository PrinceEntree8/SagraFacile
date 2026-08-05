using System.Security.Claims;
using SagraFacile.Domain.Features.Orders;

namespace SagraFacile.Web.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static OrderActorDescriptor ToOrderActor(this ClaimsPrincipal user)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (user.IsInRole("Admin"))       return new(userId, OrderActor.Admin);
        if (user.IsInRole("Supervisore")) return new(userId, OrderActor.Supervisor);
        if (user.IsInRole("Cucina"))      return new(userId, OrderActor.Kitchen);
        if (user.IsInRole("Cassiere"))    return new(userId, OrderActor.Cashier);
        return new(userId, OrderActor.Customer);
    }
}
