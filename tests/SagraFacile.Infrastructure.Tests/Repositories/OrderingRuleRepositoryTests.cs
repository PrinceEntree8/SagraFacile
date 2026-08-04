using SagraFacile.Domain.Features.Orders;
using SagraFacile.Infrastructure.Repositories;
using SagraFacile.Infrastructure.Tests;
using Xunit;

namespace SagraFacile.Infrastructure.Tests.Repositories;

public class OrderingRuleRepositoryTests
{
    [Fact]
    public async Task AddAsync_ThenGetById_ReturnsRule()
    {
        using var factory = new TestDbContextFactory();
        await using var repo = new OrderingRuleRepository(factory);

        var rule = OrderingRule.Create(1, OrderingRuleScope.Order, null, "covers > 0", "Covers required", null, 1, true);
        await repo.AddAsync(rule);
        await repo.SaveChangesAsync();

        var reloaded = await repo.GetByIdAsync(rule.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(1, reloaded!.EventId);
        Assert.Equal(OrderingRuleScope.Order, reloaded.Scope);
        Assert.Equal("covers > 0", reloaded.Expression);
        Assert.True(reloaded.IsActive);
    }

    [Fact]
    public async Task GetActiveByEventAsync_ReturnsOnlyActiveRulesOrderedByPriorityAndId()
    {
        using var factory = new TestDbContextFactory();
        await using var repo = new OrderingRuleRepository(factory);

        var r1 = OrderingRule.Create(1, OrderingRuleScope.Order, null, "covers > 0", "Err", null, 10, true);
        var r2 = OrderingRule.Create(1, OrderingRuleScope.Category, "primi", "category.primi <= covers + 1", "Err", null, 5, true);
        var r3 = OrderingRule.Create(1, OrderingRuleScope.Order, null, "totalItems > 0", "Err", null, 5, false); // Inactive
        var r4 = OrderingRule.Create(2, OrderingRuleScope.Order, null, "covers > 0", "Err", null, 1, true); // Other event

        await repo.AddAsync(r1);
        await repo.AddAsync(r2);
        await repo.AddAsync(r3);
        await repo.AddAsync(r4);
        await repo.SaveChangesAsync();

        var activeRules = await repo.GetActiveByEventAsync(1);

        Assert.Equal(2, activeRules.Count);
        Assert.Equal(r2.Id, activeRules[0].Id); // Priority 5
        Assert.Equal(r1.Id, activeRules[1].Id); // Priority 10
    }

    [Fact]
    public async Task GetByEventAsync_ReturnsAllRulesForEvent()
    {
        using var factory = new TestDbContextFactory();
        await using var repo = new OrderingRuleRepository(factory);

        var r1 = OrderingRule.Create(1, OrderingRuleScope.Order, null, "covers > 0", "Err", null, 10, true);
        var r2 = OrderingRule.Create(1, OrderingRuleScope.Order, null, "totalItems > 0", "Err", null, 5, false);

        await repo.AddAsync(r1);
        await repo.AddAsync(r2);
        await repo.SaveChangesAsync();

        var rules = await repo.GetByEventAsync(1);

        Assert.Equal(2, rules.Count);
    }

    [Fact]
    public async Task DeleteAsync_RemovesRule()
    {
        using var factory = new TestDbContextFactory();
        await using var repo = new OrderingRuleRepository(factory);

        var rule = OrderingRule.Create(1, OrderingRuleScope.Order, null, "covers > 0", "Err", null, 1, true);
        await repo.AddAsync(rule);
        await repo.SaveChangesAsync();
        var id = rule.Id;

        await repo.DeleteAsync(id);
        await repo.SaveChangesAsync();

        var reloaded = await repo.GetByIdAsync(id);
        Assert.Null(reloaded);
    }
}
