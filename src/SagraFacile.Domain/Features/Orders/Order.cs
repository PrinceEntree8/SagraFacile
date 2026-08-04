using SagraFacile.Domain.Common;
using SagraFacile.Domain.Features.Events;

namespace SagraFacile.Domain.Features.Orders;

public class Order : HasDomainEvents
{
    public int Id { get; init; }
    public int EventId { get; init; }
    public int OrderNumber { get; init; }
    public OrderStatus Status { get; set; } = OrderStatus.Draft;
    public OrderContext Context { get; init; }
    public int? ContextReferenceId { get; init; }
    public string? ContextLabel { get; init; }
    public int Covers { get; init; }
    public int CoverChargeInCents { get; init; }
    public string? CustomerName { get; set; }
    public string? Notes { get; init; }
    public string? CreatedByUserId { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public long Version { get; set; }

    public Event Event { get; init; } = null!;
    public ICollection<OrderLine> Lines { get; init; } = new List<OrderLine>();
    public ICollection<OrderStatusTransition> Transitions { get; init; } = new List<OrderStatusTransition>();

    public bool IsEditable => OrderStatusRules.IsEditable(Status);
    public int LinesTotalInCents => Lines.Sum(l => l.UnitPriceInCents * l.Quantity);
    public int TotalInCents => LinesTotalInCents + Covers * CoverChargeInCents;

    public static Order Create(
        Event @event,
        int orderNumber,
        OrderContext context,
        int? contextReferenceId,
        string? contextLabel,
        int covers,
        int coverChargeInCents,
        string? createdByUserId = null)
    {
        ArgumentNullException.ThrowIfNull(@event);

        if (orderNumber <= 0)
            throw new DomainRuleViolationException("OrderNumber must be positive.");
        if (covers < 0)
            throw new DomainRuleViolationException("Covers cannot be negative.");
        if (coverChargeInCents < 0)
            throw new DomainRuleViolationException("CoverChargeInCents cannot be negative.");
        if (!@event.AdditionalOptions.Orders.EnabledContexts.Contains(context))
            throw new DomainRuleViolationException($"Order context {context} is not enabled for this event.");

        OrderContextRules.ValidateReference(context, contextReferenceId);

        var order = new Order
        {
            EventId = @event.Id,
            OrderNumber = orderNumber,
            Status = OrderStatus.Draft,
            Context = context,
            ContextReferenceId = contextReferenceId,
            ContextLabel = contextLabel,
            Covers = covers,
            CoverChargeInCents = coverChargeInCents,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };

        order.Raise(new OrderCreated(order.Id, order.EventId, order.OrderNumber, order.Context, order.ContextReferenceId));
        return order;
    }

    public OrderLine AddLine(int menuItemId, string menuItemName, int unitPriceInCents, int quantity, string? notes = null)
    {
        EnsureEditable();
        if (quantity < 1)
            throw new DomainRuleViolationException("Quantity must be at least 1.");
        if (unitPriceInCents < 0)
            throw new DomainRuleViolationException("UnitPriceInCents cannot be negative.");
        if (string.IsNullOrWhiteSpace(menuItemName))
            throw new DomainRuleViolationException("MenuItemName is required.");

        var line = new OrderLine
        {
            OrderId = Id,
            Order = this,
            MenuItemId = menuItemId,
            MenuItemName = menuItemName,
            UnitPriceInCents = unitPriceInCents,
            Quantity = quantity,
            Notes = notes,
            Position = Lines.Count == 0 ? 1 : Lines.Max(l => l.Position) + 1
        };

        Lines.Add(line);
        RaiseLinesChanged();
        return line;
    }

    public void UpdateLineQuantity(int lineId, int quantity)
    {
        EnsureEditable();
        if (quantity < 1)
            throw new DomainRuleViolationException("Quantity must be at least 1.");

        var line = Lines.FirstOrDefault(l => l.Id == lineId)
                   ?? throw new DomainRuleViolationException($"Order line {lineId} not found.");

        line.Quantity = quantity;
        RaiseLinesChanged();
    }

    public void RemoveLine(int lineId)
    {
        EnsureEditable();
        var line = Lines.FirstOrDefault(l => l.Id == lineId)
                   ?? throw new DomainRuleViolationException($"Order line {lineId} not found.");

        Lines.Remove(line);
        RaiseLinesChanged();
    }

    public void TransitionTo(OrderStatus target, string? userId = null, string? reason = null)
    {
        if (!OrderStatusRules.CanTransition(Status, target))
            throw new DomainRuleViolationException($"Cannot transition order from {Status} to {target}.");

        var from = Status;
        var now = DateTime.UtcNow;
        Status = target;

        switch (target)
        {
            case OrderStatus.Confirmed: ConfirmedAt = now; break;
            case OrderStatus.Completed: CompletedAt = now; break;
            case OrderStatus.Cancelled: CancelledAt = now; break;
        }

        Transitions.Add(new OrderStatusTransition
        {
            OrderId = Id,
            Order = this,
            FromStatus = from,
            ToStatus = target,
            OccurredAt = now,
            UserId = userId,
            Reason = reason
        });

        Raise(new OrderStatusChanged(Id, EventId, from, target, userId));

        switch (target)
        {
            case OrderStatus.Confirmed: Raise(new OrderConfirmed(Id, EventId, TotalInCents)); break;
            case OrderStatus.Completed: Raise(new OrderCompleted(Id, EventId)); break;
            case OrderStatus.Cancelled: Raise(new OrderCancelled(Id, EventId, reason)); break;
        }
    }

    private void EnsureEditable()
    {
        if (!IsEditable)
            throw new DomainRuleViolationException($"Order lines cannot be modified while the order is {Status}.");
    }

    private void RaiseLinesChanged() => Raise(new OrderLinesChanged(Id, EventId, Lines.Count, TotalInCents));
}
