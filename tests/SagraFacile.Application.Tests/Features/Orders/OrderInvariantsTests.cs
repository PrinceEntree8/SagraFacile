using SagraFacile.Domain.Common;
using SagraFacile.Domain.Features.Events;
using SagraFacile.Domain.Features.Orders;
using Xunit;

namespace SagraFacile.Application.Tests.Features.Orders;

public class OrderInvariantsTests
{
    private static Event CreateEvent(params OrderContext[] contexts) => new()
    {
        Id = 1,
        Name = "Test Event",
        AdditionalOptions = new EventAdditionalOptions
        {
            Orders = new OrderOptions
            {
                EnabledContexts = contexts.Length == 0
                    ? [OrderContext.Table, OrderContext.Reservation, OrderContext.Takeaway]
                    : contexts
            }
        }
    };

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_InvalidOrderNumber_ThrowsDomainRuleViolationException(int orderNumber)
    {
        var @event = CreateEvent();
        Assert.Throws<DomainRuleViolationException>(() =>
            Order.Create(@event, orderNumber, OrderContext.Takeaway, null, null, 0, 0));
    }

    [Fact]
    public void Create_NegativeCoversOrCharge_ThrowsDomainRuleViolationException()
    {
        var @event = CreateEvent();
        Assert.Throws<DomainRuleViolationException>(() =>
            Order.Create(@event, 1, OrderContext.Takeaway, null, null, -1, 0));
        Assert.Throws<DomainRuleViolationException>(() =>
            Order.Create(@event, 1, OrderContext.Takeaway, null, null, 0, -100));
    }

    [Fact]
    public void Create_ContextNotEnabled_ThrowsDomainRuleViolationException()
    {
        var @event = CreateEvent(OrderContext.Table); // Only Table enabled
        Assert.Throws<DomainRuleViolationException>(() =>
            Order.Create(@event, 1, OrderContext.Takeaway, null, null, 0, 0));
    }

    [Theory]
    [InlineData(OrderContext.Table, null)]
    [InlineData(OrderContext.Table, 0)]
    [InlineData(OrderContext.Table, -5)]
    [InlineData(OrderContext.Reservation, null)]
    [InlineData(OrderContext.Reservation, 0)]
    [InlineData(OrderContext.Reservation, -1)]
    public void Create_TableOrReservationWithInvalidReference_ThrowsDomainRuleViolationException(OrderContext context, int? referenceId)
    {
        var @event = CreateEvent();
        Assert.Throws<DomainRuleViolationException>(() =>
            Order.Create(@event, 1, context, referenceId, "Label", 0, 0));
    }

    [Fact]
    public void Create_TakeawayWithNonNullReference_ThrowsDomainRuleViolationException()
    {
        var @event = CreateEvent();
        Assert.Throws<DomainRuleViolationException>(() =>
            Order.Create(@event, 1, OrderContext.Takeaway, 10, null, 0, 0));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddLine_InvalidQuantity_ThrowsDomainRuleViolationException(int quantity)
    {
        var @event = CreateEvent();
        var order = Order.Create(@event, 1, OrderContext.Takeaway, null, null, 0, 0);

        Assert.Throws<DomainRuleViolationException>(() =>
            order.AddLine(1, "Item", 100, quantity));
    }

    [Fact]
    public void AddLine_NegativePriceOrEmptyName_ThrowsDomainRuleViolationException()
    {
        var @event = CreateEvent();
        var order = Order.Create(@event, 1, OrderContext.Takeaway, null, null, 0, 0);

        Assert.Throws<DomainRuleViolationException>(() =>
            order.AddLine(1, "Item", -10, 1));
        Assert.Throws<DomainRuleViolationException>(() =>
            order.AddLine(1, "", 100, 1));
    }

    [Theory]
    [InlineData(OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Preparing)]
    [InlineData(OrderStatus.Ready)]
    [InlineData(OrderStatus.Completed)]
    [InlineData(OrderStatus.Cancelled)]
    public void LineMutations_NonEditableStatus_ThrowsDomainRuleViolationException(OrderStatus status)
    {
        var @event = CreateEvent();
        var order = Order.Create(@event, 1, OrderContext.Takeaway, null, null, 0, 0);
        var line = order.AddLine(1, "Item", 100, 1);
        line.Id = 1;
        order.Status = status;

        Assert.Throws<DomainRuleViolationException>(() => order.AddLine(2, "Item 2", 100, 1));
        Assert.Throws<DomainRuleViolationException>(() => order.UpdateLineQuantity(1, 2));
        Assert.Throws<DomainRuleViolationException>(() => order.RemoveLine(1));
    }
}
