using SagraFacile.Domain.Common;
using SagraFacile.Domain.Features.Orders;

namespace SagraFacile.Application.Tests.Features.OrderingRules;

public class OrderingRuleInvariantsTests
{
    [Fact]
    public void Create_ValidOrderScopedRule_Succeeds()
    {
        var rule = OrderingRule.Create(
            eventId: 1,
            scope: OrderingRuleScope.Order,
            targetCode: null,
            expression: "covers > 0",
            errorMessage: "Covers must be greater than 0.",
            warningMessage: null,
            priority: 10,
            isActive: true);

        Assert.Equal(1, rule.EventId);
        Assert.Equal(OrderingRuleScope.Order, rule.Scope);
        Assert.Null(rule.TargetCode);
        Assert.Equal("covers > 0", rule.Expression);
        Assert.Equal("Covers must be greater than 0.", rule.ErrorMessage);
        Assert.Null(rule.WarningMessage);
        Assert.True(rule.IsBlocking);
        Assert.Equal("Covers must be greater than 0.", rule.Message);
        Assert.True(rule.IsActive);
        Assert.Equal(10, rule.Priority);
    }

    [Fact]
    public void Create_CategoryScopedRule_RequiresTargetCode()
    {
        var ex = Assert.Throws<DomainRuleViolationException>(() =>
            OrderingRule.Create(1, OrderingRuleScope.Category, null, "category.primi <= covers", "Error", null, 1, true));

        Assert.Contains("TargetCode is required", ex.Message);
    }

    [Fact]
    public void Create_OrderScopedRule_TargetCodeMustBeEmpty()
    {
        var ex = Assert.Throws<DomainRuleViolationException>(() =>
            OrderingRule.Create(1, OrderingRuleScope.Order, "primi", "covers > 0", "Error", null, 1, true));

        Assert.Contains("TargetCode must be empty for Order scope", ex.Message);
    }

    [Fact]
    public void Create_WithoutMessages_ThrowsException()
    {
        var ex = Assert.Throws<DomainRuleViolationException>(() =>
            OrderingRule.Create(1, OrderingRuleScope.Order, null, "covers > 0", null, null, 1, true));

        Assert.Contains("Either ErrorMessage or WarningMessage is required", ex.Message);
    }

    [Fact]
    public void Create_NegativePriority_ThrowsException()
    {
        var ex = Assert.Throws<DomainRuleViolationException>(() =>
            OrderingRule.Create(1, OrderingRuleScope.Order, null, "covers > 0", "Error", null, -1, true));

        Assert.Contains("Priority must be >= 0", ex.Message);
    }

    [Fact]
    public void WarningOnlyRule_IsNotBlocking()
    {
        var rule = OrderingRule.Create(
            1, OrderingRuleScope.Category, "primi", "category.primi <= covers + 1", null, "High number of primi", 0, true);

        Assert.False(rule.IsBlocking);
        Assert.Equal("High number of primi", rule.Message);
    }

    [Fact]
    public void SetActive_UpdatesActiveState()
    {
        var rule = OrderingRule.Create(1, OrderingRuleScope.Order, null, "covers > 0", "Error", null, 0, true);
        Assert.True(rule.IsActive);

        rule.SetActive(false);
        Assert.False(rule.IsActive);
    }
}
