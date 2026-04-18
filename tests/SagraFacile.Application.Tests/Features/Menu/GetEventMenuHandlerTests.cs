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
        // Arrange
        var items = new List<MenuItem>
        {
            new() { Id = 1, EventId = 5, Name = "Bruschetta", Price = 4m, Category = MenuCategory.Starters, MenuItemAllergens = new List<MenuItemAllergen>() },
            new() { Id = 2, EventId = 5, Name = "Pasta", Price = 9m, Category = MenuCategory.MainCourse, MenuItemAllergens = new List<MenuItemAllergen>() }
        };
        _repo.GetByEventIdAsync(5, false, Arg.Any<CancellationToken>()).Returns(items);

        // Act
        var result = await _handler.Handle(new GetEventMenu.Query(5), CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, i => Assert.Equal(5, i.EventId));
    }

    [Fact]
    public async Task Handle_MapsCategoriesCorrectly()
    {
        // Arrange
        var items = new List<MenuItem>
        {
            new() { Id = 1, EventId = 1, Name = "Tiramisu", Price = 5m, Category = MenuCategory.Dessert, MenuItemAllergens = new List<MenuItemAllergen>() }
        };
        _repo.GetByEventIdAsync(1, false, Arg.Any<CancellationToken>()).Returns(items);

        // Act
        var result = await _handler.Handle(new GetEventMenu.Query(1), CancellationToken.None);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(MenuCategory.Dessert, result.Items[0].Category);
    }

    [Fact]
    public async Task Handle_WithAllergens_MapsAllergenDtos()
    {
        // Arrange
        var allergen = new Allergen { Id = 1, Code = "GLUTEN", Name = "Gluten (cereals)", NameIt = "Glutine (cereali)" };
        var items = new List<MenuItem>
        {
            new()
            {
                Id = 1, EventId = 1, Name = "Bread", Price = 2m, Category = MenuCategory.Starters,
                MenuItemAllergens = new List<MenuItemAllergen>
                {
                    new() { MenuItemId = 1, AllergenId = 1, Allergen = allergen }
                }
            }
        };
        _repo.GetByEventIdAsync(1, false, Arg.Any<CancellationToken>()).Returns(items);

        // Act
        var result = await _handler.Handle(new GetEventMenu.Query(1), CancellationToken.None);

        // Assert
        Assert.Single(result.Items[0].Allergens);
        Assert.Equal("GLUTEN", result.Items[0].Allergens[0].Code);
    }
}
