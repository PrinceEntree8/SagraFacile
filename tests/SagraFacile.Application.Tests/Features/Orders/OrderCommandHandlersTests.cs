using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SagraFacile.Application.Features.Orders;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Events;
using SagraFacile.Domain.Features.Menu;
using SagraFacile.Domain.Features.Orders;
using Xunit;

namespace SagraFacile.Application.Tests.Features.Orders;

public class OrderCommandHandlersTests
{
    private readonly IOrderRepository _orderRepo = Substitute.For<IOrderRepository>();
    private readonly IEventRepository _eventRepo = Substitute.For<IEventRepository>();
    private readonly IMenuRepository _menuRepo = Substitute.For<IMenuRepository>();
    private readonly IOrderingRulesService _rulesService = Substitute.For<IOrderingRulesService>();
    private readonly IOrderStateMachine _stateMachine = new OrderStateMachine(NullLogger<OrderStateMachine>.Instance);

    private static Event CreateTestEvent() => new()
    {
        Id = 1,
        Name = "Test Event",
        AdditionalOptions = new EventAdditionalOptions()
    };

    [Fact]
    public async Task CreateDraftOrder_ValidInput_CreatesDraft()
    {
        var @event = CreateTestEvent();
        _eventRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(@event);
        _menuRepo.GetByEventIdAsync(1, true, Arg.Any<CancellationToken>())
            .Returns([new MenuItem { Id = 10, Name = "Pasta", PriceInCents = 1000 }]);
        _orderRepo.GetNextOrderNumberAsync(1, Arg.Any<CancellationToken>()).Returns(101);

        var handler = new CreateDraftOrder.Handler(_orderRepo, _eventRepo, _menuRepo, _stateMachine);
        var command = new CreateDraftOrder.Command(
            1, OrderContext.Takeaway, null, null, 0, "Mario", "Notes",
            [new CreateDraftOrder.DraftLine(10, 2)],
            new OrderActorDescriptor("cashier1", OrderActor.Cashier));

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(101, result.Data.OrderNumber);
        await _orderRepo.Received(1).AddAsync(Arg.Is<Order>(o => o.OrderNumber == 101), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitPreorder_ValidOrder_TransitionsToPreorder()
    {
        var @event = CreateTestEvent();
        var order = Order.Create(@event, 1, OrderContext.Takeaway, null, null, 0, 0);
        _orderRepo.GetByIdWithLinesAndEventAsync(1, Arg.Any<CancellationToken>()).Returns(order);
        _rulesService.ValidateOrderAsync(Arg.Any<OrderValidationInput>(), Arg.Any<CancellationToken>())
            .Returns(new OrderValidationResult(true, [], []));

        var handler = new SubmitPreorder.Handler(_orderRepo, _stateMachine, _rulesService);
        var command = new SubmitPreorder.Command(1, OrderActorDescriptor.Customer("cust1"));

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(OrderStatus.Preorder, order.Status);
    }

    [Fact]
    public async Task ConfirmOrder_CustomerActor_ReturnsFailure()
    {
        var @event = CreateTestEvent();
        var order = Order.Create(@event, 1, OrderContext.Takeaway, null, null, 0, 0);
        _orderRepo.GetByIdWithLinesAndEventAsync(1, Arg.Any<CancellationToken>()).Returns(order);

        var handler = new ConfirmOrder.Handler(_orderRepo, _stateMachine, _rulesService);
        var command = new ConfirmOrder.Command(1, OrderActorDescriptor.Customer("cust1"));

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Customers cannot confirm orders", result.Message);
    }

    [Fact]
    public async Task ConfirmOrder_ValidCashier_ConfirmsOrder()
    {
        var @event = CreateTestEvent();
        var order = Order.Create(@event, 1, OrderContext.Takeaway, null, null, 0, 0);
        _orderRepo.GetByIdWithLinesAndEventAsync(1, Arg.Any<CancellationToken>()).Returns(order);
        _rulesService.ValidateOrderAsync(Arg.Any<OrderValidationInput>(), Arg.Any<CancellationToken>())
            .Returns(new OrderValidationResult(true, [], []));

        var handler = new ConfirmOrder.Handler(_orderRepo, _stateMachine, _rulesService);
        var command = new ConfirmOrder.Command(1, new OrderActorDescriptor("cashier1", OrderActor.Cashier));

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public async Task RejectOrder_PreorderWithReason_RejectsOrder()
    {
        var @event = CreateTestEvent();
        var order = Order.Create(@event, 1, OrderContext.Takeaway, null, null, 0, 0);
        order.Status = OrderStatus.Preorder;
        _orderRepo.GetByIdWithEventAsync(1, Arg.Any<CancellationToken>()).Returns(order);

        var handler = new RejectOrder.Handler(_orderRepo, _stateMachine);
        var command = new RejectOrder.Command(1, new OrderActorDescriptor("supervisor", OrderActor.Supervisor), "Out of ingredients");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(OrderStatus.Rejected, order.Status);
    }

    [Fact]
    public async Task CancelOrder_CustomerActor_SetsCancelledByCustomer()
    {
        var @event = CreateTestEvent();
        var order = Order.Create(@event, 1, OrderContext.Takeaway, null, null, 0, 0);
        _orderRepo.GetByIdWithEventAsync(1, Arg.Any<CancellationToken>()).Returns(order);

        var handler = new CancelOrder.Handler(_orderRepo, _stateMachine);
        var command = new CancelOrder.Command(1, OrderActorDescriptor.Customer("cust1"));

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(OrderStatus.CancelledByCustomer, order.Status);
    }

    [Fact]
    public async Task CancelOrder_OperatorActor_SetsCancelledByOperator()
    {
        var @event = CreateTestEvent();
        var order = Order.Create(@event, 1, OrderContext.Takeaway, null, null, 0, 0);
        _orderRepo.GetByIdWithEventAsync(1, Arg.Any<CancellationToken>()).Returns(order);

        var handler = new CancelOrder.Handler(_orderRepo, _stateMachine);
        var command = new CancelOrder.Command(1, new OrderActorDescriptor("cashier1", OrderActor.Cashier));

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(OrderStatus.CancelledByOperator, order.Status);
    }

    [Fact]
    public async Task AdvanceOrderStatus_ImplicitNext_AdvancesConfirmedToFulfilled()
    {
        var @event = CreateTestEvent();
        var order = Order.Create(@event, 1, OrderContext.Takeaway, null, null, 0, 0);
        order.Status = OrderStatus.Confirmed;
        _orderRepo.GetByIdWithEventAsync(1, Arg.Any<CancellationToken>()).Returns(order);

        var handler = new AdvanceOrderStatus.Handler(_orderRepo, _stateMachine);
        var command = new AdvanceOrderStatus.Command(1, new OrderActorDescriptor("kitchen1", OrderActor.Kitchen));

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(OrderStatus.Fulfilled, order.Status);
    }
}
