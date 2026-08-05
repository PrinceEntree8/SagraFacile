namespace SagraFacile.Domain.Features.Orders;

public sealed record OrderTransitionRule(
    OrderStatus From,
    OrderStatus To,
    IReadOnlyCollection<OrderActor> Actors);

public static class OrderStatusRules
{
    private static readonly OrderActor[] Operators =
        [OrderActor.Cashier, OrderActor.Supervisor, OrderActor.Admin];

    public static readonly IReadOnlyList<OrderTransitionRule> Default =
    [
        new(OrderStatus.Draft,     OrderStatus.Preorder,            [OrderActor.Customer, OrderActor.Cashier]),
        new(OrderStatus.Draft,     OrderStatus.Confirmed,           Operators),
        new(OrderStatus.Draft,     OrderStatus.CancelledByCustomer, [OrderActor.Customer]),
        new(OrderStatus.Draft,     OrderStatus.CancelledByOperator, Operators),
        new(OrderStatus.Preorder,  OrderStatus.Confirmed,           Operators),
        new(OrderStatus.Preorder,  OrderStatus.Rejected,            Operators),
        new(OrderStatus.Preorder,  OrderStatus.CancelledByCustomer, [OrderActor.Customer]),
        new(OrderStatus.Preorder,  OrderStatus.CancelledByOperator, Operators),
        new(OrderStatus.Confirmed, OrderStatus.Fulfilled,           [OrderActor.Kitchen, OrderActor.Cashier, OrderActor.Supervisor, OrderActor.Admin]),
        new(OrderStatus.Confirmed, OrderStatus.CancelledByOperator, [OrderActor.Supervisor, OrderActor.Admin]),
        new(OrderStatus.Fulfilled, OrderStatus.Delivered,           [OrderActor.Cashier, OrderActor.Kitchen, OrderActor.Supervisor, OrderActor.Admin]),
    ];

    // Linear chain used by AdvanceOrderStatus when no explicit target is supplied.
    public static OrderStatus? NextInChain(OrderStatus from) => from switch
    {
        OrderStatus.Confirmed => OrderStatus.Fulfilled,
        OrderStatus.Fulfilled => OrderStatus.Delivered,
        _ => null
    };
}
