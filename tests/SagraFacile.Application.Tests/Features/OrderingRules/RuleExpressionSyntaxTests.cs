using SagraFacile.Application.Features.OrderingRules;

namespace SagraFacile.Application.Tests.Features.OrderingRules;

public class RuleExpressionSyntaxTests
{
    [Theory]
    [InlineData("covers", "[covers]")]
    [InlineData("totalItems", "[totalItems]")]
    [InlineData("category.primi <= covers + 1", "[category.primi] <= [covers] + 1")]
    [InlineData("category.bevande <= covers * 2", "[category.bevande] <= [covers] * 2")]
    [InlineData("item.pizza-margherita > 0", "[item.pizza-margherita] > 0")]
    [InlineData("min(category.primi, 5) + max(1, 2)", "min([category.primi], 5) + max(1, 2)")]
    public void Normalize_RewritesIdentifiersToBrackets(string raw, string expected)
    {
        var normalized = RuleExpressionSyntax.Normalize(raw);
        Assert.Equal(expected, normalized);
    }
}
