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
    public int? ParentOrderId { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? FulfilledAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public long Version { get; set; }

    public Event Event { get; init; } = null!;
    public Order? Parent { get; init; }
    public ICollection<Order> FollowUps { get; init; } = new List<Order>();
    public ICollection<OrderLine> Lines { get; init; } = new List<OrderLine>();
    public ICollection<OrderStatusTransition> Transitions { get; init; } = new List<OrderStatusTransition>();

    public bool IsEditable(OrderTransitionPolicy? policy = null)
        => (policy ?? OrderTransitionPolicy.CreateDefault()).IsEditable(Status);
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
        string? createdByUserId = null,
        int? parentOrderId = null)
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
            ParentOrderId = parentOrderId,
            CreatedAt = DateTime.UtcNow
        };

        order.Raise(new OrderCreated(order.Id, order.EventId, order.OrderNumber, order.Context, order.ContextReferenceId));
        return order;
    }

    public static Order CreateFollowUp(Order parent, Event @event, int orderNumber, string? createdByUserId = null)
    {
        ArgumentNullException.ThrowIfNull(parent);
        if (parent.Status.IsTerminal())
            throw new DomainRuleViolationException("Cannot create a follow-up for a closed order.");

        var order = Create(@event, orderNumber, parent.Context, parent.ContextReferenceId,
                           parent.ContextLabel, covers: 0, coverChargeInCents: 0,
                           createdByUserId, parentOrderId: parent.Id);
        order.Raise(new OrderFollowUpCreated(order.Id, parent.Id, @event.Id));
        return order;
    }

    public OrderLine AddLine(int menuItemId, string menuItemName, int unitPriceInCents, int quantity, string? notes = null)
        => AddLine(OrderTransitionPolicy.CreateDefault(), menuItemId, menuItemName, unitPriceInCents, quantity, notes);

    public OrderLine AddLine(OrderTransitionPolicy policy, int menuItemId, string menuItemName, int unitPriceInCents, int quantity, string? notes = null)
    {
        EnsureEditable(policy);
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
        => UpdateLineQuantity(OrderTransitionPolicy.CreateDefault(), lineId, quantity);

    public void UpdateLineQuantity(OrderTransitionPolicy policy, int lineId, int quantity)
    {
        EnsureEditable(policy);
        if (quantity < 1)
            throw new DomainRuleViolationException("Quantity must be at least 1.");

        var line = Lines.FirstOrDefault(l => l.Id == lineId)
                   ?? throw new DomainRuleViolationException($"Order line {lineId} not found.");

        line.Quantity = quantity;
        RaiseLinesChanged();
    }

    public void RemoveLine(int lineId)
        => RemoveLine(OrderTransitionPolicy.CreateDefault(), lineId);

    public void RemoveLine(OrderTransitionPolicy policy, int lineId)
    {
        EnsureEditable(policy);
        var line = Lines.FirstOrDefault(l => l.Id == lineId)
                   ?? throw new DomainRuleViolationException($"Order line {lineId} not found.");

        Lines.Remove(line);
        RaiseLinesChanged();
    }

    public void TransitionTo(
        OrderStatus target,
        OrderTransitionPolicy policy,
        OrderActorDescriptor actor,
        string? reason = null)
    {
        ArgumentNullException.ThrowIfNull(policy);

        if (!policy.CanTransition(Status, target))
            throw new DomainRuleViolationException($"Cannot transition order from {Status} to {target}.");
        if (!policy.IsAllowedFor(Status, target, actor.Role))
            throw new OrderTransitionNotAllowedException(Status, target, actor.Role);
        if (policy.RequiresReason(target) && string.IsNullOrWhiteSpace(reason))
            throw new DomainRuleViolationException($"A reason is required to move the order to {target}.");

        var from = Status;
        var now = DateTime.UtcNow;
        Status = target;

        switch (target)
        {
            case OrderStatus.Confirmed: ConfirmedAt = now; break;
            case OrderStatus.Fulfilled: FulfilledAt = now; break;
            case OrderStatus.Delivered: CompletedAt = now; break;
            case OrderStatus.Rejected:
            case OrderStatus.CancelledByCustomer:
            case OrderStatus.CancelledByOperator: CancelledAt = now; break;
        }

        Transitions.Add(new OrderStatusTransition
        {
            OrderId = Id,
            Order = this,
            FromStatus = from,
            ToStatus = target,
            OccurredAt = now,
            UserId = actor.UserId,
            ActorRole = actor.Role,
            Reason = reason
        });

        Raise(new OrderStatusChanged(Id, EventId, from, target, actor.UserId, actor.Role));

        switch (target)
        {
            case OrderStatus.Preorder:  Raise(new OrderPreordered(Id, EventId)); break;
            case OrderStatus.Confirmed: Raise(new OrderConfirmed(Id, EventId, TotalInCents)); break;
            case OrderStatus.Fulfilled: Raise(new OrderFulfilled(Id, EventId)); break;
            case OrderStatus.Delivered: Raise(new OrderDelivered(Id, EventId)); break;
            case OrderStatus.Rejected:  Raise(new OrderRejected(Id, EventId, reason)); break;
            case OrderStatus.CancelledByCustomer:
            case OrderStatus.CancelledByOperator: Raise(new OrderCancelled(Id, EventId, reason)); break;
        }
    }

    private void EnsureEditable(OrderTransitionPolicy policy)
    {
        if (!IsEditable(policy))
            throw new DomainRuleViolationException($"Order lines cannot be modified while the order is {Status}.");
    }

    private void RaiseLinesChanged() => Raise(new OrderLinesChanged(Id, EventId, Lines.Count, TotalInCents));
}
