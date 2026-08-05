using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SagraFacile.Application.Features.Orders;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Menu;
using SagraFacile.Domain.Features.Orders;
using SagraFacile.Infrastructure.Repositories;
using Xunit;

namespace SagraFacile.Infrastructure.Tests.Repositories;

public class OrderLifecycleIntegrationTests
{
    private static (OrderRepository repo, EventRepository eventRepo, MenuRepository menuRepo, MenuCategoryRepository catRepo) CreateRepos(TestDbContextFactory factory, IDomainEventDispatcher dispatcher)
    {
        return (
            new OrderRepository(factory, dispatcher),
            new EventRepository(factory),
            new MenuRepository(factory),
            new MenuCategoryRepository(factory)
        );
    }

    private static async Task<MenuCategory> CreateCategoryAsync(MenuCategoryRepository catRepo)
    {
        var cat = new MenuCategory { Name = "Main", Code = "main", DisplayOrder = 1 };
        await catRepo.AddAsync(cat, CancellationToken.None);
        await catRepo.SaveChangesAsync(CancellationToken.None);
        return cat;
    }

    [Fact]
    public async Task CashierHappyPath_CreateDraftConfirmAdvanceToDelivered_PersistsStatusAndTransitions()
    {
        using var factory = new TestDbContextFactory();
        var dispatcher = Substitute.For<IDomainEventDispatcher>();
        var (repo, eventRepo, menuRepo, catRepo) = CreateRepos(factory, dispatcher);
        var cat = await CreateCategoryAsync(catRepo);

        var stateMachine = new OrderStateMachine(NullLogger<OrderStateMachine>.Instance);
        var rulesService = Substitute.For<IOrderingRulesService>();
        rulesService.ValidateOrderAsync(Arg.Any<OrderValidationInput>(), Arg.Any<CancellationToken>())
            .Returns(new OrderValidationResult(true, [], []));

        // Seed menu item
        var menuItem = new MenuItem { EventId = 1, CategoryId = cat.Id, Name = "Pasta", PriceInCents = 800, Code = "P1", IsAvailable = true };
        await menuRepo.AddAsync(menuItem, CancellationToken.None);
        await menuRepo.SaveChangesAsync(CancellationToken.None);

        // 1. CreateDraftOrder
        var createDraftHandler = new CreateDraftOrder.Handler(repo, eventRepo, menuRepo, stateMachine);
        var draftRes = await createDraftHandler.Handle(new CreateDraftOrder.Command(
            1, OrderContext.Table, 5, "Table 5", 2, "Rossi", "No salt",
            [new CreateDraftOrder.DraftLine(menuItem.Id, 2)],
            new OrderActorDescriptor("cashier1", OrderActor.Cashier)
        ), CancellationToken.None);

        Assert.True(draftRes.Success);
        var orderId = draftRes.Data!.OrderId;

        // 2. ConfirmOrder
        var confirmHandler = new ConfirmOrder.Handler(repo, stateMachine, rulesService);
        var confirmRes = await confirmHandler.Handle(
            new ConfirmOrder.Command(orderId, new OrderActorDescriptor("cashier1", OrderActor.Cashier)), CancellationToken.None);
        Assert.True(confirmRes.Success);

        // 3. AdvanceOrderStatus -> Fulfilled
        var advanceHandler = new AdvanceOrderStatus.Handler(repo, stateMachine);
        var advanceRes1 = await advanceHandler.Handle(
            new AdvanceOrderStatus.Command(orderId, new OrderActorDescriptor("kitchen1", OrderActor.Kitchen)), CancellationToken.None);
        Assert.True(advanceRes1.Success);

        // 4. AdvanceOrderStatus -> Delivered
        var advanceRes2 = await advanceHandler.Handle(
            new AdvanceOrderStatus.Command(orderId, new OrderActorDescriptor("cashier1", OrderActor.Cashier)), CancellationToken.None);
        Assert.True(advanceRes2.Success);

        // Verify reloaded order from DB
        var reloaded = await repo.GetByIdWithHistoryAsync(orderId);
        Assert.NotNull(reloaded);
        Assert.Equal(OrderStatus.Delivered, reloaded.Status);
        Assert.Equal(3, reloaded.Transitions.Count);
        Assert.Equal(OrderActor.Cashier, reloaded.Transitions.First().ActorRole);
        Assert.Equal(OrderActor.Kitchen, reloaded.Transitions.ElementAt(1).ActorRole);
    }

    [Fact]
    public async Task PreorderFlow_SubmitPreorderAndRejectWithReason_PersistsRejectedStatus()
    {
        using var factory = new TestDbContextFactory();
        var dispatcher = Substitute.For<IDomainEventDispatcher>();
        var (repo, eventRepo, menuRepo, catRepo) = CreateRepos(factory, dispatcher);
        var cat = await CreateCategoryAsync(catRepo);

        var stateMachine = new OrderStateMachine(NullLogger<OrderStateMachine>.Instance);
        var rulesService = Substitute.For<IOrderingRulesService>();
        rulesService.ValidateOrderAsync(Arg.Any<OrderValidationInput>(), Arg.Any<CancellationToken>())
            .Returns(new OrderValidationResult(true, [], []));

        var menuItem = new MenuItem { EventId = 1, CategoryId = cat.Id, Name = "Pizza", PriceInCents = 700, Code = "PZ1", IsAvailable = true };
        await menuRepo.AddAsync(menuItem, CancellationToken.None);
        await menuRepo.SaveChangesAsync(CancellationToken.None);

        var createDraftHandler = new CreateDraftOrder.Handler(repo, eventRepo, menuRepo, stateMachine);
        var draftRes = await createDraftHandler.Handle(new CreateDraftOrder.Command(
            1, OrderContext.Takeaway, null, null, 0, "Customer", null,
            [new CreateDraftOrder.DraftLine(menuItem.Id, 1)],
            OrderActorDescriptor.Customer("cust1")
        ), CancellationToken.None);

        var orderId = draftRes.Data!.OrderId;

        var submitHandler = new SubmitPreorder.Handler(repo, stateMachine, rulesService);
        var submitRes = await submitHandler.Handle(new SubmitPreorder.Command(orderId, OrderActorDescriptor.Customer("cust1")), CancellationToken.None);
        Assert.True(submitRes.Success);

        var rejectHandler = new RejectOrder.Handler(repo, stateMachine);
        var rejectRes = await rejectHandler.Handle(new RejectOrder.Command(
            orderId, new OrderActorDescriptor("supervisor1", OrderActor.Supervisor), "Kitchen closed"
        ), CancellationToken.None);
        Assert.True(rejectRes.Success);

        var reloaded = await repo.GetByIdWithHistoryAsync(orderId);
        Assert.NotNull(reloaded);
        Assert.Equal(OrderStatus.Rejected, reloaded.Status);
        Assert.Equal("Kitchen closed", reloaded.Transitions.Last().Reason);
    }

    [Fact]
    public async Task CreateFollowUp_OnConfirmedParent_LinksToParent()
    {
        using var factory = new TestDbContextFactory();
        var dispatcher = Substitute.For<IDomainEventDispatcher>();
        var (repo, eventRepo, menuRepo, catRepo) = CreateRepos(factory, dispatcher);
        var cat = await CreateCategoryAsync(catRepo);

        var stateMachine = new OrderStateMachine(NullLogger<OrderStateMachine>.Instance);
        var rulesService = Substitute.For<IOrderingRulesService>();
        rulesService.ValidateOrderAsync(Arg.Any<OrderValidationInput>(), Arg.Any<CancellationToken>())
            .Returns(new OrderValidationResult(true, [], []));

        var menuItem = new MenuItem { EventId = 1, CategoryId = cat.Id, Name = "Coffee", PriceInCents = 150, Code = "C1", IsAvailable = true };
        await menuRepo.AddAsync(menuItem, CancellationToken.None);
        await menuRepo.SaveChangesAsync(CancellationToken.None);

        var createDraftHandler = new CreateDraftOrder.Handler(repo, eventRepo, menuRepo, stateMachine);
        var parentRes = await createDraftHandler.Handle(new CreateDraftOrder.Command(
            1, OrderContext.Table, 10, "Table 10", 4, "Group", null, [],
            new OrderActorDescriptor("cashier1", OrderActor.Cashier)
        ), CancellationToken.None);

        var parentId = parentRes.Data!.OrderId;

        var confirmHandler = new ConfirmOrder.Handler(repo, stateMachine, rulesService);
        await confirmHandler.Handle(new ConfirmOrder.Command(parentId, new OrderActorDescriptor("cashier1", OrderActor.Cashier)), CancellationToken.None);

        var followUpHandler = new CreateFollowUpOrder.Handler(repo, menuRepo, stateMachine, rulesService);
        var followUpRes = await followUpHandler.Handle(new CreateFollowUpOrder.Command(
            parentId, [new CreateDraftOrder.DraftLine(menuItem.Id, 2)],
            new OrderActorDescriptor("cashier1", OrderActor.Cashier)
        ), CancellationToken.None);

        Assert.True(followUpRes.Success);
        var followUpId = followUpRes.Data!.OrderId;

        var reloadedFollowUp = await repo.GetByIdAsync(followUpId);
        Assert.NotNull(reloadedFollowUp);
        Assert.Equal(parentId, reloadedFollowUp.ParentOrderId);
    }
}
