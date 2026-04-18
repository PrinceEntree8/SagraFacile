using NSubstitute;
using SagraFacile.Application.Features.Menu;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Menu;

namespace SagraFacile.Application.Tests.Features.Menu;

public class GetEventMenuHandlerTests
{
    private readonly IMenuRepository _repo = Substitute.For<IMenuRepository>();
    private readonly GetEventMenu.Handler _handler;

    public GetEventMenuHandlerTests()
    {
        _handler = new GetEventMenu.Handler(_repo);
    }

    [Fact]
    public async Task Handle_ReturnsItemsForGivenEventId()
    {
        var cat1 = new MenuCategory { Id = 1, Name = "Starters", NameIt = "Antipasti" };
        var cat2 = new MenuCategory { Id = 2, Name = "Main Course", NameIt = "Primi" };
        var items = new List<MenuItem>
        {
            new() { Id = 1, EventId = 5, Name = "Bruschetta", PriceInCents = 400, CategoryId = 1, Category = cat1, MenuItemAllergens = new List<MenuItemAllergen>() },
            new() { Id = 2, EventId = 5, Name = "Pasta", PriceInCents = 900, CategoryId = 2, Category = cat2, MenuItemAllergens = new List<MenuItemAllergen>() }
        };
        _repo.GetByEventIdAsync(5, false, Arg.Any<CancellationToken>()).Returns(items);

        var result = await _handler.Handle(new GetEventMenu.Query(5), CancellationToken.None);

        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, i => Assert.Equal(5, i.EventId));
    }

    [Fact]
    public async Task Handle_MapsCategoriesCorrectly()
    {
        var cat = new MenuCategory { Id = 4, Name = "Dessert", NameIt = "Dolci" };
        var items = new List<MenuItem>
        {
            new() { Id = 1, EventId = 1, Name = "Tiramisu", PriceInCents = 500, CategoryId = 4, Category = cat, MenuItemAllergens = new List<MenuItemAllergen>() }
        };
        _repo.GetByEventIdAsync(1, false, Arg.Any<CancellationToken>()).Returns(items);

        var result = await _handler.Handle(new GetEventMenu.Query(1), CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal(4, result.Items[0].CategoryId);
        Assert.Equal("Dessert", result.Items[0].CategoryName);
    }

    [Fact]
    public async Task Handle_WithAllergens_MapsAllergenDtos()
    {
        var allergen = new Allergen { Id = 1, Code = "GLUTEN", Name = "Gluten (cereals)", NameIt = "Glutine (cereali)", Icon = "🌾" };
        var cat = new MenuCategory { Id = 1, Name = "Starters", NameIt = "Antipasti" };
        var items = new List<MenuItem>
        {
            new()
            {
                Id = 1, EventId = 1, Name = "Bread", PriceInCents = 200, CategoryId = 1, Category = cat,
                MenuItemAllergens = new List<MenuItemAllergen>
                {
                    new() { MenuItemId = 1, AllergenId = 1, Allergen = allergen }
                }
            }
        };
        _repo.GetByEventIdAsync(1, false, Arg.Any<CancellationToken>()).Returns(items);

        var result = await _handler.Handle(new GetEventMenu.Query(1), CancellationToken.None);

        Assert.Single(result.Items[0].Allergens);
        Assert.Equal("GLUTEN", result.Items[0].Allergens[0].Code);
        Assert.Equal("🌾", result.Items[0].Allergens[0].Icon);
    }
}
