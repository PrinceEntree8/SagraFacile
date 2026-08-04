using SagraFacile.Domain.Common;
using SagraFacile.Domain.Features.Events;
using SagraFacile.Domain.Features.Orders;
using Xunit;

namespace SagraFacile.Application.Tests.Features.Orders;

public class OrderAggregateTests
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

    [Fact]
    public void Create_ValidParameters_CreatesDraftOrderAndRaisesEvent()
    {
        var @event = CreateEvent();

        var order = Order.Create(@event, 101, OrderContext.Table, 5, "Table 5", covers: 4, coverChargeInCents: 150, createdByUserId: "user1");

        Assert.Equal(1, order.EventId);
        Assert.Equal(101, order.OrderNumber);
        Assert.Equal(OrderStatus.Draft, order.Status);
        Assert.Equal(OrderContext.Table, order.Context);
        Assert.Equal(5, order.ContextReferenceId);
        Assert.Equal("Table 5", order.ContextLabel);
        Assert.Equal(4, order.Covers);
        Assert.Equal(150, order.CoverChargeInCents);
        Assert.Equal("user1", order.CreatedByUserId);
        Assert.True(order.IsEditable);
        Assert.Equal(600, order.TotalInCents); // 4 * 150 = 600

        var createdEvent = Assert.Single(order.DomainEvents.OfType<OrderCreated>());
        Assert.Equal(order.EventId, createdEvent.EventId);
        Assert.Equal(order.OrderNumber, createdEvent.OrderNumber);
        Assert.Equal(order.Context, createdEvent.Context);
        Assert.Equal(order.ContextReferenceId, createdEvent.ContextReferenceId);
    }

    [Fact]
    public void AddLine_ValidLine_AddsLineIncrementsPositionAndUpdatesTotal()
    {
        var @event = CreateEvent();
        var order = Order.Create(@event, 1, OrderContext.Takeaway, null, null, covers: 0, coverChargeInCents: 0);

        var line1 = order.AddLine(10, "Burger", 800, 2, "No onion");
        var line2 = order.AddLine(11, "Fries", 300, 1);

        Assert.Equal(2, order.Lines.Count);
        Assert.Equal(1, line1.Position);
        Assert.Equal(2, line2.Position);
        Assert.Equal(1600, line1.TotalInCents);
        Assert.Equal(300, line2.TotalInCents);
        Assert.Equal(1900, order.LinesTotalInCents);
        Assert.Equal(1900, order.TotalInCents);

        var linesChangedEvents = order.DomainEvents.OfType<OrderLinesChanged>().ToList();
        Assert.Equal(2, linesChangedEvents.Count);
        Assert.Equal(1900, linesChangedEvents.Last().TotalInCents);
    }

    [Fact]
    public void UpdateLineQuantity_ExistingLine_UpdatesQuantityAndTotal()
    {
        var @event = CreateEvent();
        var order = Order.Create(@event, 1, OrderContext.Takeaway, null, null, covers: 0, coverChargeInCents: 0);
        var line = order.AddLine(10, "Burger", 800, 1);
        line.Id = 1; // Simulated persistence ID

        order.UpdateLineQuantity(1, 3);

        Assert.Equal(3, line.Quantity);
        Assert.Equal(2400, order.TotalInCents);
    }

    [Fact]
    public void RemoveLine_ExistingLine_RemovesLineAndUpdatesTotal()
    {
        var @event = CreateEvent();
        var order = Order.Create(@event, 1, OrderContext.Takeaway, null, null, covers: 0, coverChargeInCents: 0);
        var line1 = order.AddLine(10, "Burger", 800, 1);
        var line2 = order.AddLine(11, "Fries", 300, 1);
        line1.Id = 1;
        line2.Id = 2;

        order.RemoveLine(1);

        Assert.Single(order.Lines);
        Assert.Equal(300, order.TotalInCents);
    }
}
