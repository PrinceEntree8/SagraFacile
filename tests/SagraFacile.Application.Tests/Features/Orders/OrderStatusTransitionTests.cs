using SagraFacile.Domain.Common;
using SagraFacile.Domain.Features.Events;
using SagraFacile.Domain.Features.Orders;
using Xunit;

namespace SagraFacile.Application.Tests.Features.Orders;

public class OrderStatusTransitionTests
{
    private static Event CreateEvent() => new()
    {
        Id = 1,
        Name = "Test Event",
        AdditionalOptions = new EventAdditionalOptions()
    };

    [Fact]
    public void FullHappyPath_DraftToConfirmedToFulfilledToDelivered_TransitionsAndAppendsAuditTrail()
    {
        var @event = CreateEvent();
        var order = Order.Create(@event, 1, OrderContext.Takeaway, null, null, covers: 2, coverChargeInCents: 100);
        order.AddLine(1, "Pasta", 1000, 1);

        var policy = OrderTransitionPolicy.CreateDefault();

        // Draft -> Confirmed
        order.TransitionTo(OrderStatus.Confirmed, policy, new OrderActorDescriptor("user1", OrderActor.Cashier));
        Assert.Equal(OrderStatus.Confirmed, order.Status);
        Assert.NotNull(order.ConfirmedAt);
        Assert.False(order.IsEditable(policy));

        // Confirmed -> Fulfilled
        order.TransitionTo(OrderStatus.Fulfilled, policy, new OrderActorDescriptor("user2", OrderActor.Kitchen));
        Assert.Equal(OrderStatus.Fulfilled, order.Status);
        Assert.NotNull(order.FulfilledAt);

        // Fulfilled -> Delivered
        order.TransitionTo(OrderStatus.Delivered, policy, new OrderActorDescriptor("user3", OrderActor.Cashier));
        Assert.Equal(OrderStatus.Delivered, order.Status);
        Assert.NotNull(order.CompletedAt);

        Assert.Equal(3, order.Transitions.Count);
        var transitionArray = order.Transitions.ToArray();

        Assert.Equal(OrderStatus.Draft, transitionArray[0].FromStatus);
        Assert.Equal(OrderStatus.Confirmed, transitionArray[0].ToStatus);
        Assert.Equal("user1", transitionArray[0].UserId);
        Assert.Equal(OrderActor.Cashier, transitionArray[0].ActorRole);

        Assert.Equal(OrderStatus.Confirmed, transitionArray[1].FromStatus);
        Assert.Equal(OrderStatus.Fulfilled, transitionArray[1].ToStatus);
        Assert.Equal(OrderActor.Kitchen, transitionArray[1].ActorRole);

        Assert.Equal(OrderStatus.Fulfilled, transitionArray[2].FromStatus);
        Assert.Equal(OrderStatus.Delivered, transitionArray[2].ToStatus);

        Assert.Contains(order.DomainEvents, e => e is OrderConfirmed);
        Assert.Contains(order.DomainEvents, e => e is OrderDelivered);
    }

    [Fact]
    public void TransitionTo_CancelledByCustomer_SetsCancelledAt()
    {
        var @event = CreateEvent();
        var order = Order.Create(@event, 1, OrderContext.Takeaway, null, null, covers: 0, coverChargeInCents: 0);

        var policy = OrderTransitionPolicy.CreateDefault();
        order.TransitionTo(OrderStatus.CancelledByCustomer, policy, OrderActorDescriptor.Customer("cust1"));

        Assert.Equal(OrderStatus.CancelledByCustomer, order.Status);
        Assert.NotNull(order.CancelledAt);

        var transition = Assert.Single(order.Transitions);
        Assert.Equal(OrderActor.Customer, transition.ActorRole);

        Assert.Contains(order.DomainEvents, e => e is OrderCancelled);
    }

    [Fact]
    public void TransitionTo_Reject_RequiresReason()
    {
        var @event = CreateEvent();
        var order = Order.Create(@event, 1, OrderContext.Takeaway, null, null, covers: 0, coverChargeInCents: 0);

        var policy = OrderTransitionPolicy.CreateDefault();
        order.TransitionTo(OrderStatus.Preorder, policy, OrderActorDescriptor.Customer("cust1"));

        Assert.Throws<DomainRuleViolationException>(() =>
            order.TransitionTo(OrderStatus.Rejected, policy, new OrderActorDescriptor("cashier1", OrderActor.Cashier), reason: null));

        order.TransitionTo(OrderStatus.Rejected, policy, new OrderActorDescriptor("cashier1", OrderActor.Cashier), reason: "Out of stock");
        Assert.Equal(OrderStatus.Rejected, order.Status);
    }

    [Theory]
    [InlineData(OrderStatus.Draft, OrderStatus.Fulfilled)]
    [InlineData(OrderStatus.Draft, OrderStatus.Delivered)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Delivered)]
    [InlineData(OrderStatus.Delivered, OrderStatus.Confirmed)]
    public void TransitionTo_InvalidTarget_ThrowsDomainRuleViolationException(OrderStatus initial, OrderStatus target)
    {
        var @event = CreateEvent();
        var order = Order.Create(@event, 1, OrderContext.Takeaway, null, null, covers: 0, coverChargeInCents: 0);
        order.Status = initial;

        var policy = OrderTransitionPolicy.CreateDefault();
        Assert.Throws<DomainRuleViolationException>(() =>
            order.TransitionTo(target, policy, new OrderActorDescriptor("admin", OrderActor.Admin)));
    }

    [Fact]
    public void TransitionTo_UnauthorisedActor_ThrowsOrderTransitionNotAllowedException()
    {
        var @event = CreateEvent();
        var order = Order.Create(@event, 1, OrderContext.Takeaway, null, null, covers: 0, coverChargeInCents: 0);

        var policy = OrderTransitionPolicy.CreateDefault();
        Assert.Throws<OrderTransitionNotAllowedException>(() =>
            order.TransitionTo(OrderStatus.Confirmed, policy, OrderActorDescriptor.Customer("cust1")));
    }
}
