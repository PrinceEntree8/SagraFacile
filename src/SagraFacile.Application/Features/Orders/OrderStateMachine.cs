using Microsoft.Extensions.Logging;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Events;
using SagraFacile.Domain.Features.Orders;

namespace SagraFacile.Application.Features.Orders;

public sealed class OrderStateMachine(ILogger<OrderStateMachine> logger) : IOrderStateMachine
{
    public OrderTransitionPolicy PolicyFor(Event @event)
    {
        var o = @event?.AdditionalOptions?.Orders?.Lifecycle ?? new OrderLifecycleOptions();
        var map = OrderStatusRules.Default.ToDictionary(r => (r.From, r.To), r => r);

        foreach (var ov in o.TransitionOverrides)
        {
            if (ov.From == ov.To)
            {
                logger.LogWarning("Ignoring invalid self-transition override {From}", ov.From);
                continue;
            }

            var key = (ov.From, ov.To);
            if (!ov.Enabled) { map.Remove(key); continue; }

            var actors = ov.Actors is { Count: > 0 }
                ? ov.Actors.Cast<OrderActor>().ToList()
                : map.TryGetValue(key, out var existing) ? existing.Actors.ToList() : [OrderActor.Admin];
            map[key] = new OrderTransitionRule(ov.From, ov.To, actors);
        }

        return new OrderTransitionPolicy(map.Values, o.AllowEditAfterConfirmation,
            o.RequireReasonOnRejection, o.RequireReasonOnCancellation,
            o.AllowFollowUpOrders, o.AutoConfirmPreorders, o.DefaultConfirmerRole);
    }
}
