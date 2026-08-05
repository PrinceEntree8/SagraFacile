using Microsoft.EntityFrameworkCore;
using NSubstitute;
using SagraFacile.Application.Exceptions;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Common;
using SagraFacile.Domain.Features.Events;
using SagraFacile.Domain.Features.Orders;
using SagraFacile.Infrastructure.Repositories;
using Xunit;

namespace SagraFacile.Infrastructure.Tests.Repositories;

public class OrderRepositoryTests
{
    private const int EventId1 = 1;
    private const int EventId2 = 2;

    private static Event GetTestEvent(int id = EventId1) => new()
    {
        Id = id,
        Name = $"Test Event {id}",
        AdditionalOptions = new EventAdditionalOptions()
    };

    [Fact]
    public async Task AddAsync_ThenGetById_ReturnsOrder()
    {
        using var factory = new TestDbContextFactory();
        var dispatcher = Substitute.For<IDomainEventDispatcher>();
        await using var repo = new OrderRepository(factory, dispatcher);

        var order = Order.Create(GetTestEvent(), 1, OrderContext.Table, 10, "Table 10", covers: 2, coverChargeInCents: 150);

        await repo.AddAsync(order);
        await repo.SaveChangesAsync();

        var reloaded = await repo.GetByIdAsync(order.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(1, reloaded.OrderNumber);
        Assert.Equal(OrderContext.Table, reloaded.Context);
        Assert.Equal(10, reloaded.ContextReferenceId);
        Assert.Equal("Table 10", reloaded.ContextLabel);
        Assert.Equal(300, reloaded.TotalInCents);
    }

    [Fact]
    public async Task AddAsync_WithLinesAndTransitions_GetByIdWithHistoryAsyncLoadsCollectionsOrdered()
    {
        using var factory = new TestDbContextFactory();
        var dispatcher = Substitute.For<IDomainEventDispatcher>();
        await using var repo = new OrderRepository(factory, dispatcher);

        var order = Order.Create(GetTestEvent(), 1, OrderContext.Takeaway, null, null, covers: 0, coverChargeInCents: 0);
        order.AddLine(100, "Pizza Margherita", 600, 2);
        order.AddLine(101, "Coca Cola", 250, 2);
        order.TransitionTo(OrderStatus.Confirmed, OrderTransitionPolicy.CreateDefault(), new OrderActorDescriptor("cashier", OrderActor.Cashier));

        await repo.AddAsync(order);
        await repo.SaveChangesAsync();

        var reloaded = await repo.GetByIdWithHistoryAsync(order.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(2, reloaded.Lines.Count);
        Assert.Equal("Pizza Margherita", reloaded.Lines.First().MenuItemName);
        Assert.Single(reloaded.Transitions);
        Assert.Equal(OrderStatus.Confirmed, reloaded.Transitions.First().ToStatus);
    }

    [Fact]
    public async Task GetByEventAndNumberAsync_ExistingOrder_ReturnsOrderWithLines()
    {
        using var factory = new TestDbContextFactory();
        var dispatcher = Substitute.For<IDomainEventDispatcher>();
        await using var repo = new OrderRepository(factory, dispatcher);

        var order = Order.Create(GetTestEvent(EventId1), 42, OrderContext.Takeaway, null, null, covers: 0, coverChargeInCents: 0);
        order.AddLine(1, "Water", 100, 1);

        await repo.AddAsync(order);
        await repo.SaveChangesAsync();

        var reloaded = await repo.GetByEventAndNumberAsync(EventId1, 42);

        Assert.NotNull(reloaded);
        Assert.Equal(42, reloaded.OrderNumber);
        Assert.Single(reloaded.Lines);
    }

    [Fact]
    public async Task GetNextOrderNumberAsync_NoOrders_Returns1()
    {
        using var factory = new TestDbContextFactory();
        var dispatcher = Substitute.For<IDomainEventDispatcher>();
        await using var repo = new OrderRepository(factory, dispatcher);

        var next = await repo.GetNextOrderNumberAsync(EventId1);

        Assert.Equal(1, next);
    }

    [Fact]
    public async Task GetNextOrderNumberAsync_ExistingOrders_ReturnsMaxPlusOnePerEvent()
    {
        using var factory = new TestDbContextFactory();
        var dispatcher = Substitute.For<IDomainEventDispatcher>();
        await using var repo = new OrderRepository(factory, dispatcher);

        var o1 = Order.Create(GetTestEvent(EventId1), 1, OrderContext.Takeaway, null, null, 0, 0);
        var o2 = Order.Create(GetTestEvent(EventId1), 5, OrderContext.Takeaway, null, null, 0, 0);
        var o3 = Order.Create(GetTestEvent(EventId2), 2, OrderContext.Takeaway, null, null, 0, 0);

        await repo.AddAsync(o1);
        await repo.AddAsync(o2);
        await repo.AddAsync(o3);
        await repo.SaveChangesAsync();

        Assert.Equal(6, await repo.GetNextOrderNumberAsync(EventId1));
        Assert.Equal(3, await repo.GetNextOrderNumberAsync(EventId2));
    }

    [Fact]
    public async Task GetPagedAsync_FilterByStatuses_ReturnsMatching()
    {
        using var factory = new TestDbContextFactory();
        var dispatcher = Substitute.For<IDomainEventDispatcher>();
        await using var repo = new OrderRepository(factory, dispatcher);

        var draft = Order.Create(GetTestEvent(EventId1), 1, OrderContext.Takeaway, null, null, 0, 0);
        var confirmed = Order.Create(GetTestEvent(EventId1), 2, OrderContext.Takeaway, null, null, 0, 0);
        confirmed.TransitionTo(OrderStatus.Confirmed, OrderTransitionPolicy.CreateDefault(), new OrderActorDescriptor("cashier", OrderActor.Cashier));

        await repo.AddAsync(draft);
        await repo.AddAsync(confirmed);
        await repo.SaveChangesAsync();

        var (items, total) = await repo.GetPagedAsync(EventId1, 1, 10, [OrderStatus.Confirmed]);

        Assert.Equal(1, total);
        Assert.Equal(2, items.First().OrderNumber);
    }

    [Fact]
    public async Task DuplicateOrderNumber_ThrowsDbUpdateException()
    {
        using var factory = new TestDbContextFactory();
        var dispatcher = Substitute.For<IDomainEventDispatcher>();
        await using var repo = new OrderRepository(factory, dispatcher);

        var o1 = Order.Create(GetTestEvent(EventId1), 1, OrderContext.Takeaway, null, null, 0, 0);
        var o2 = Order.Create(GetTestEvent(EventId1), 1, OrderContext.Takeaway, null, null, 0, 0);

        await repo.AddAsync(o1);
        await repo.AddAsync(o2);

        await Assert.ThrowsAsync<DbUpdateException>(() => repo.SaveChangesAsync());
    }

    [Fact]
    public async Task ConcurrentUpdate_StaleVersion_ThrowsRepositoryConcurrencyException()
    {
        using var factory = new TestDbContextFactory();
        var dispatcher = Substitute.For<IDomainEventDispatcher>();
        await using var repo1 = new OrderRepository(factory, dispatcher);
        await using var repo2 = new OrderRepository(factory, dispatcher);

        var order = Order.Create(GetTestEvent(EventId1), 1, OrderContext.Takeaway, null, null, 0, 0);
        await repo1.AddAsync(order);
        await repo1.SaveChangesAsync();

        var o1 = await repo1.GetByIdAsync(order.Id);
        var o2 = await repo2.GetByIdAsync(order.Id);

        Assert.NotNull(o1);
        Assert.NotNull(o2);

        o1.CustomerName = "Modified by 1";
        await repo1.SaveChangesAsync();

        o2.CustomerName = "Modified by 2";
        await Assert.ThrowsAsync<RepositoryConcurrencyException>(() => repo2.SaveChangesAsync());
    }

    [Fact]
    public async Task SaveChangesAsync_DispatchesEventsAndClearsAggregateEvents()
    {
        using var factory = new TestDbContextFactory();
        var dispatcher = Substitute.For<IDomainEventDispatcher>();
        await using var repo = new OrderRepository(factory, dispatcher);

        var order = Order.Create(GetTestEvent(), 1, OrderContext.Takeaway, null, null, covers: 0, coverChargeInCents: 0);
        Assert.NotEmpty(order.DomainEvents);

        await repo.AddAsync(order);
        await repo.SaveChangesAsync();

        Assert.Empty(order.DomainEvents);
        await dispatcher.Received(1).DispatchAsync(Arg.Is<IEnumerable<IDomainEvent>>(e => e.Count() == 1), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveChangesAsync_OnFailure_DoesNotDispatchEvents()
    {
        using var factory = new TestDbContextFactory();
        var dispatcher = Substitute.For<IDomainEventDispatcher>();
        await using var repo = new OrderRepository(factory, dispatcher);

        var o1 = Order.Create(GetTestEvent(EventId1), 1, OrderContext.Takeaway, null, null, 0, 0);
        var o2 = Order.Create(GetTestEvent(EventId1), 1, OrderContext.Takeaway, null, null, 0, 0);

        await repo.AddAsync(o1);
        await repo.AddAsync(o2);

        await Assert.ThrowsAsync<DbUpdateException>(() => repo.SaveChangesAsync());

        await dispatcher.DidNotReceiveWithAnyArgs().DispatchAsync(default!, default);
    }

    [Fact]
    public async Task AddFollowUp_PersistsParentRelationshipAndActorRole()
    {
        using var factory = new TestDbContextFactory();
        var dispatcher = Substitute.For<IDomainEventDispatcher>();
        await using var repo = new OrderRepository(factory, dispatcher);

        var parent = Order.Create(GetTestEvent(), 1, OrderContext.Table, 5, "Table 5", covers: 4, coverChargeInCents: 150);
        parent.TransitionTo(OrderStatus.Confirmed, OrderTransitionPolicy.CreateDefault(), new OrderActorDescriptor("cashier1", OrderActor.Cashier));

        await repo.AddAsync(parent);
        await repo.SaveChangesAsync();

        var followUp = Order.CreateFollowUp(parent, GetTestEvent(), 2, createdByUserId: "cashier1");
        await repo.AddAsync(followUp);
        await repo.SaveChangesAsync();

        var reloadedParent = await repo.GetByIdWithHistoryAsync(parent.Id);
        var reloadedFollowUp = await repo.GetByIdAsync(followUp.Id);

        Assert.NotNull(reloadedParent);
        Assert.NotNull(reloadedFollowUp);
        Assert.Equal(parent.Id, reloadedFollowUp.ParentOrderId);
        Assert.Equal(OrderActor.Cashier, reloadedParent.Transitions.First().ActorRole);
    }
}
