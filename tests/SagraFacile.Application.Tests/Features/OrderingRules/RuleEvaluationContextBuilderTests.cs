using NSubstitute;
using SagraFacile.Application.Features.OrderingRules;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Menu;

namespace SagraFacile.Application.Tests.Features.OrderingRules;

public class RuleEvaluationContextBuilderTests
{
    private readonly IMenuRepository _menuRepo = Substitute.For<IMenuRepository>();

    [Fact]
    public async Task BuildAsync_CalculatesTotalsAndQuantitiesByCode()
    {
        var categoryPrimi = new MenuCategory { Id = 10, Name = "Primi", Code = "primi" };
        var categoryBevande = new MenuCategory { Id = 20, Name = "Bevande", Code = "bevande" };

        var itemPizza = new MenuItem { Id = 1, Name = "Pizza Margherita", Code = "pizza-margherita", Category = categoryPrimi, CategoryId = 10 };
        var itemWater = new MenuItem { Id = 2, Name = "Acqua", Code = "acqua", Category = categoryBevande, CategoryId = 20 };

        _menuRepo.GetByEventIdAsync(1, true, Arg.Any<CancellationToken>())
            .Returns(new List<MenuItem> { itemPizza, itemWater });

        var builder = new RuleEvaluationContextBuilder(_menuRepo);
        var input = new OrderValidationInput(
            EventId: 1,
            Covers: 4,
            Lines: new List<OrderValidationLine>
            {
                new(MenuItemId: 1, Quantity: 3),
                new(MenuItemId: 2, Quantity: 2)
            });

        var context = await builder.BuildAsync(input);

        Assert.Equal(4, context.Covers);
        Assert.Equal(5, context.TotalItems);

        Assert.Equal(3, context.CategoryQuantities["primi"]);
        Assert.Equal(2, context.CategoryQuantities["bevande"]);

        Assert.Equal(3, context.ItemQuantities["pizza-margherita"]);
        Assert.Equal(2, context.ItemQuantities["acqua"]);

        Assert.Contains("primi", context.PresentCategoryCodes);
        Assert.Contains("bevande", context.PresentCategoryCodes);
        Assert.Contains("pizza-margherita", context.PresentItemCodes);
        Assert.Contains("acqua", context.PresentItemCodes);
    }
}
