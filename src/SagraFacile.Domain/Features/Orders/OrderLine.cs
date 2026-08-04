namespace SagraFacile.Domain.Features.Orders;

public class OrderLine
{
    public int Id { get; set; }
    public int OrderId { get; init; }
    public int MenuItemId { get; init; }
    public string MenuItemName { get; init; } = string.Empty;
    public int UnitPriceInCents { get; init; }
    public int Quantity { get; set; }
    public string? Notes { get; init; }
    public int Position { get; init; }

    public Order Order { get; init; } = null!;

    public int TotalInCents => UnitPriceInCents * Quantity;
}
