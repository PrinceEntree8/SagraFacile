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
    public void FullHappyPath_DraftToConfirmedToPreparingToReadyToCompleted_TransitionsAndAppendsAuditTrail()
    {
        var @event = CreateEvent();
        var order = Order.Create(@event, 1, OrderContext.Takeaway, null, null, covers: 2, coverChargeInCents: 100);
        order.AddLine(1, "Pasta", 1000, 1); // total = 1200

        // Draft -> Confirmed
        order.TransitionTo(OrderStatus.Confirmed, userId: "user1");
        Assert.Equal(OrderStatus.Confirmed, order.Status);
        Assert.NotNull(order.ConfirmedAt);
        Assert.False(order.IsEditable);

        // Confirmed -> Preparing
        order.TransitionTo(OrderStatus.Preparing, userId: "user2");
        Assert.Equal(OrderStatus.Preparing, order.Status);

        // Preparing -> Ready
        order.TransitionTo(OrderStatus.Ready, userId: "user2");
        Assert.Equal(OrderStatus.Ready, order.Status);

        // Ready -> Completed
        order.TransitionTo(OrderStatus.Completed, userId: "user3");
        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.NotNull(order.CompletedAt);

        Assert.Equal(4, order.Transitions.Count);
        var transitionArray = order.Transitions.ToArray();

        Assert.Equal(OrderStatus.Draft, transitionArray[0].FromStatus);
        Assert.Equal(OrderStatus.Confirmed, transitionArray[0].ToStatus);
        Assert.Equal("user1", transitionArray[0].UserId);

        Assert.Equal(OrderStatus.Confirmed, transitionArray[1].FromStatus);
        Assert.Equal(OrderStatus.Preparing, transitionArray[1].ToStatus);

        Assert.Equal(OrderStatus.Preparing, transitionArray[2].FromStatus);
        Assert.Equal(OrderStatus.Ready, transitionArray[2].ToStatus);

        Assert.Equal(OrderStatus.Ready, transitionArray[3].FromStatus);
        Assert.Equal(OrderStatus.Completed, transitionArray[3].ToStatus);

        Assert.Contains(order.DomainEvents, e => e is OrderConfirmed);
        Assert.Contains(order.DomainEvents, e => e is OrderCompleted);
    }

    [Fact]
    public void TransitionTo_Cancelled_SetsCancelledAtAndReason()
    {
        var @event = CreateEvent();
        var order = Order.Create(@event, 1, OrderContext.Takeaway, null, null, covers: 0, coverChargeInCents: 0);

        order.TransitionTo(OrderStatus.Cancelled, userId: "admin", reason: "Customer left");

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.NotNull(order.CancelledAt);

        var transition = Assert.Single(order.Transitions);
        Assert.Equal("Customer left", transition.Reason);

        var cancelledEvent = Assert.Single(order.DomainEvents.OfType<OrderCancelled>());
        Assert.Equal("Customer left", cancelledEvent.Reason);
    }

    [Theory]
    [InlineData(OrderStatus.Draft, OrderStatus.Preparing)]
    [InlineData(OrderStatus.Draft, OrderStatus.Ready)]
    [InlineData(OrderStatus.Draft, OrderStatus.Completed)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Ready)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Completed)]
    [InlineData(OrderStatus.Preparing, OrderStatus.Completed)]
    [InlineData(OrderStatus.Completed, OrderStatus.Preparing)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Confirmed)]
    public void TransitionTo_InvalidTarget_ThrowsDomainRuleViolationException(OrderStatus initial, OrderStatus target)
    {
        var @event = CreateEvent();
        var order = Order.Create(@event, 1, OrderContext.Takeaway, null, null, covers: 0, coverChargeInCents: 0);
        order.Status = initial;

        Assert.Throws<DomainRuleViolationException>(() => order.TransitionTo(target));
    }
}
