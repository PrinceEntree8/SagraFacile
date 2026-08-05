using SagraFacile.Domain.Common;

namespace SagraFacile.Domain.Features.Orders;

public sealed class OrderTransitionNotAllowedException : DomainRuleViolationException
{
    public OrderStatus From { get; }
    public OrderStatus To { get; }
    public OrderActor Actor { get; }

    public OrderTransitionNotAllowedException(OrderStatus from, OrderStatus to, OrderActor actor)
        : base($"Actor {actor} is not allowed to move the order from {from} to {to}.")
    {
        From = from;
        To = to;
        Actor = actor;
    }
}
