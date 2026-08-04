namespace SagraFacile.Domain.Features.Orders;

public class OrderStatusTransition
{
    public int Id { get; init; }
    public int OrderId { get; init; }
    public OrderStatus FromStatus { get; init; }
    public OrderStatus ToStatus { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public string? UserId { get; init; }
    public string? Reason { get; init; }

    public Order Order { get; init; } = null!;
}
