using SagraFacile.Application.Features.OrderingRules;

namespace SagraFacile.Application.Tests.Features.OrderingRules;

public class RuleExpressionValidatorTests
{
    private readonly RuleExpressionValidator _validator = new();

    [Theory]
    [InlineData("category.primi <= covers + 1")]
    [InlineData("category.bevande <= covers * 2")]
    [InlineData("totalItems >= 1")]
    [InlineData("item.coca-cola == 0 || covers > 2")]
    [InlineData("min(category.primi, 10) <= max(covers, 1)")]
    [InlineData("floor(category.primi / 2) <= covers")]
    [InlineData("ceil(category.dolci / 3) <= covers")]
    public void Validate_ValidExpressions_ReturnsSuccess(string expression)
    {
        var result = _validator.Validate(expression);
        Assert.True(result.IsValid, result.Error);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Validate_SyntaxError_ReturnsFailure()
    {
        var result = _validator.Validate("covers <=");
        Assert.False(result.IsValid);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public void Validate_DisallowedFunction_ReturnsFailure()
    {
        var result = _validator.Validate("round(covers, 2) > 0");
        Assert.False(result.IsValid);
        Assert.Contains("Disallowed function", result.Error);
    }

    [Fact]
    public void Validate_DisallowedIdentifier_ReturnsFailure()
    {
        var result = _validator.Validate("secret.value > 0");
        Assert.False(result.IsValid);
        Assert.Contains("Disallowed identifier", result.Error);
    }

    [Fact]
    public void Validate_NonBooleanExpression_ReturnsFailure()
    {
        var result = _validator.Validate("covers + 1");
        Assert.False(result.IsValid);
        Assert.Contains("Expression must evaluate to a boolean result", result.Error);
    }
}
