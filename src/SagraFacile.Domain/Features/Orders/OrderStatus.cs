namespace SagraFacile.Domain.Features.Orders;

public enum OrderStatus
{
    Draft,
    Preorder,
    Confirmed,
    Rejected,
    CancelledByCustomer,
    CancelledByOperator,
    Fulfilled,
    Delivered
}

public static class OrderStatusExtensions
{
    public static bool IsTerminal(this OrderStatus status) => status
        is OrderStatus.Rejected
        or OrderStatus.CancelledByCustomer
        or OrderStatus.CancelledByOperator
        or OrderStatus.Delivered;

    public static bool IsCancellation(this OrderStatus status) => status
        is OrderStatus.CancelledByCustomer or OrderStatus.CancelledByOperator;
}
