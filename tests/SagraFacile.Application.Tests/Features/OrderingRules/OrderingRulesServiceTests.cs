using NSubstitute;
using SagraFacile.Application.Features.OrderingRules;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Menu;
using SagraFacile.Domain.Features.Orders;

namespace SagraFacile.Application.Tests.Features.OrderingRules;

public class OrderingRulesServiceTests
{
    private readonly IOrderingRuleRepository _ruleRepo = Substitute.For<IOrderingRuleRepository>();
    private readonly IMenuRepository _menuRepo = Substitute.For<IMenuRepository>();
    private readonly IRuleExpressionValidator _expressionValidator = new RuleExpressionValidator();

    private OrderingRulesService CreateService()
    {
        var contextBuilder = new RuleEvaluationContextBuilder(_menuRepo);
        return new OrderingRulesService(_ruleRepo, contextBuilder);
    }

    [Fact]
    public async Task ValidateOrderAsync_RuleSatisfied_ReturnsValid()
    {
        var catPrimi = new MenuCategory { Id = 1, Name = "Primi", Code = "primi" };
        var item1 = new MenuItem { Id = 10, Name = "Pasta", Code = "pasta", Category = catPrimi, CategoryId = 1 };

        _menuRepo.GetByEventIdAsync(1, true, Arg.Any<CancellationToken>())
            .Returns(new List<MenuItem> { item1 });

        var rule = OrderingRule.Create(
            1, OrderingRuleScope.Category, "primi", "category.primi <= covers + 1", "Too many primi", null, 0, true);

        _ruleRepo.GetActiveByEventAsync(1, Arg.Any<CancellationToken>())
            .Returns(new List<OrderingRule> { rule });

        var service = CreateService();
        var input = new OrderValidationInput(1, Covers: 4, Lines: new List<OrderValidationLine> { new(10, Quantity: 5) });

        var result = await service.ValidateOrderAsync(input);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public async Task ValidateOrderAsync_BlockingRuleViolated_ReturnsInvalidAndError()
    {
        var catPrimi = new MenuCategory { Id = 1, Name = "Primi", Code = "primi" };
        var item1 = new MenuItem { Id = 10, Name = "Pasta", Code = "pasta", Category = catPrimi, CategoryId = 1 };

        _menuRepo.GetByEventIdAsync(1, true, Arg.Any<CancellationToken>())
            .Returns(new List<MenuItem> { item1 });

        var rule = OrderingRule.Create(
            1, OrderingRuleScope.Category, "primi", "category.primi <= covers + 1", "Too many primi", null, 0, true);

        _ruleRepo.GetActiveByEventAsync(1, Arg.Any<CancellationToken>())
            .Returns(new List<OrderingRule> { rule });

        var service = CreateService();
        var input = new OrderValidationInput(1, Covers: 4, Lines: new List<OrderValidationLine> { new(10, Quantity: 6) });

        var result = await service.ValidateOrderAsync(input);

        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal("Too many primi", result.Errors.First().Message);
        Assert.True(result.Errors.First().IsBlocking);
    }

    [Fact]
    public async Task ValidateOrderAsync_WarningOnlyRuleViolated_ReturnsValidAndWarning()
    {
        var catPrimi = new MenuCategory { Id = 1, Name = "Primi", Code = "primi" };
        var item1 = new MenuItem { Id = 10, Name = "Pasta", Code = "pasta", Category = catPrimi, CategoryId = 1 };

        _menuRepo.GetByEventIdAsync(1, true, Arg.Any<CancellationToken>())
            .Returns(new List<MenuItem> { item1 });

        var rule = OrderingRule.Create(
            1, OrderingRuleScope.Category, "primi", "category.primi <= covers", null, "More primi than covers", 0, true);

        _ruleRepo.GetActiveByEventAsync(1, Arg.Any<CancellationToken>())
            .Returns(new List<OrderingRule> { rule });

        var service = CreateService();
        var input = new OrderValidationInput(1, Covers: 4, Lines: new List<OrderValidationLine> { new(10, Quantity: 5) });

        var result = await service.ValidateOrderAsync(input);

        Assert.True(result.IsValid); // No errors, only warning!
        Assert.Empty(result.Errors);
        Assert.Single(result.Warnings);
        Assert.Equal("More primi than covers", result.Warnings.First().Message);
        Assert.False(result.Warnings.First().IsBlocking);
    }

    [Fact]
    public async Task ValidateOrderAsync_AbsentCategoryScope_IsSkipped()
    {
        var catBevande = new MenuCategory { Id = 2, Name = "Bevande", Code = "bevande" };
        var itemWater = new MenuItem { Id = 20, Name = "Acqua", Code = "acqua", Category = catBevande, CategoryId = 2 };

        _menuRepo.GetByEventIdAsync(1, true, Arg.Any<CancellationToken>())
            .Returns(new List<MenuItem> { itemWater });

        // Category rule for "primi", but the order only has "bevande"
        var rule = OrderingRule.Create(
            1, OrderingRuleScope.Category, "primi", "category.primi <= 2", "Error", null, 0, true);

        _ruleRepo.GetActiveByEventAsync(1, Arg.Any<CancellationToken>())
            .Returns(new List<OrderingRule> { rule });

        var service = CreateService();
        var input = new OrderValidationInput(1, Covers: 1, Lines: new List<OrderValidationLine> { new(20, Quantity: 5) });

        var result = await service.ValidateOrderAsync(input);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ValidateOrderAsync_MissingVariableResolvesToZero()
    {
        var catBevande = new MenuCategory { Id = 2, Name = "Bevande", Code = "bevande" };
        var itemWater = new MenuItem { Id = 20, Name = "Acqua", Code = "acqua", Category = catBevande, CategoryId = 2 };

        _menuRepo.GetByEventIdAsync(1, true, Arg.Any<CancellationToken>())
            .Returns(new List<MenuItem> { itemWater });

        // Order scope rule checking category.dolci (which is not in order) <= 5 -> 0 <= 5 is true
        var rule = OrderingRule.Create(
            1, OrderingRuleScope.Order, null, "category.dolci <= 5", "Error", null, 0, true);

        _ruleRepo.GetActiveByEventAsync(1, Arg.Any<CancellationToken>())
            .Returns(new List<OrderingRule> { rule });

        var service = CreateService();
        var input = new OrderValidationInput(1, Covers: 2, Lines: new List<OrderValidationLine> { new(20, Quantity: 1) });

        var result = await service.ValidateOrderAsync(input);

        Assert.True(result.IsValid);
    }
}
