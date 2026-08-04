using NSubstitute;
using SagraFacile.Application.Features.OrderingRules;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Orders;

namespace SagraFacile.Application.Tests.Features.OrderingRules;

public class OrderingRuleCommandValidatorTests
{
    private readonly IRuleExpressionValidator _expressionValidator = new RuleExpressionValidator();
    private readonly IOrderingRuleRepository _repository = Substitute.For<IOrderingRuleRepository>();

    [Fact]
    public void CreateValidator_ValidCommand_Passes()
    {
        var validator = new CreateOrderingRule.Validator(_expressionValidator);
        var command = new CreateOrderingRule.Command(
            EventId: 1,
            Scope: OrderingRuleScope.Category,
            TargetCode: "primi",
            Expression: "category.primi <= covers + 1",
            ErrorMessage: "Too many primi",
            WarningMessage: null,
            Priority: 0,
            IsActive: true);

        var result = validator.Validate(command);

        Assert.True(result.IsValid, string.Join(", ", result.Errors.Select(e => e.ErrorMessage)));
    }

    [Fact]
    public void CreateValidator_InvalidExpression_Fails()
    {
        var validator = new CreateOrderingRule.Validator(_expressionValidator);
        var command = new CreateOrderingRule.Command(
            EventId: 1,
            Scope: OrderingRuleScope.Category,
            TargetCode: "primi",
            Expression: "category.primi <=", // Malformed
            ErrorMessage: "Error",
            WarningMessage: null,
            Priority: 0,
            IsActive: true);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateOrderingRule.Command.Expression));
    }

    [Fact]
    public void CreateValidator_DisallowedFunction_Fails()
    {
        var validator = new CreateOrderingRule.Validator(_expressionValidator);
        var command = new CreateOrderingRule.Command(
            EventId: 1,
            Scope: OrderingRuleScope.Order,
            TargetCode: null,
            Expression: "round(covers, 2) > 0",
            ErrorMessage: "Error",
            WarningMessage: null,
            Priority: 0,
            IsActive: true);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateOrderingRule.Command.Expression));
    }

    [Fact]
    public void UpdateValidator_InvalidExpression_Fails()
    {
        var validator = new UpdateOrderingRule.Validator(_expressionValidator);
        var command = new UpdateOrderingRule.Command(
            Id: 10,
            Scope: OrderingRuleScope.Order,
            TargetCode: null,
            Expression: "secret.variable > 0",
            ErrorMessage: "Error",
            WarningMessage: null,
            Priority: 0);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateOrderingRule.Command.Expression));
    }

    [Fact]
    public async Task SetActiveValidator_ActivatingRuleWithInvalidExpression_Fails()
    {
        var invalidRule = OrderingRule.Create(
            1, OrderingRuleScope.Order, null, "covers > 0", "Err", null, 0, false);

        // Simulate an invalid expression stored in DB (e.g. from an old version)
        _repository.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(invalidRule);

        var badValidator = new SetOrderingRuleActive.Validator(_repository, new FakeInvalidValidator());
        var command = new SetOrderingRuleActive.Command(10, IsActive: true);

        var result = await badValidator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("Cannot activate rule"));
    }

    private class FakeInvalidValidator : IRuleExpressionValidator
    {
        public RuleExpressionValidationResult Validate(string rawExpression)
            => new(false, "Fake parse error", Array.Empty<string>());

        public NCalc.Expression GetOrParse(string rawExpression)
            => new(rawExpression);
    }
}
