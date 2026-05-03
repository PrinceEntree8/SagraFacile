using NSubstitute;
using SagraFacile.Application.Features.Menu;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Menu;

namespace SagraFacile.Application.Tests.Features.Menu;

public class GetMenuCategoriesHandlerTests
{
    private readonly IMenuCategoryRepository _repo = Substitute.For<IMenuCategoryRepository>();
    private readonly GetMenuCategories.Handler _handler;

    public GetMenuCategoriesHandlerTests()
    {
        _handler = new GetMenuCategories.Handler(_repo);
    }

    [Fact]
    public async Task Handle_ReturnsAllCategoriesAsDtos()
    {
        var categories = new List<MenuCategory>
        {
            new() { Id = 1, Name = "Starters", DisplayOrder = 1 },
            new() { Id = 2, Name = "Main Course", DisplayOrder = 2 },
            new() { Id = 3, Name = "Dessert", DisplayOrder = 4 },
        };
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(categories);

        var result = await _handler.Handle(new GetMenuCategories.Query(), CancellationToken.None);

        Assert.Equal(3, result.Categories.Count);
        Assert.Equal(1, result.Categories[0].Id);
        Assert.Equal("Starters", result.Categories[0].Name);
        Assert.Equal(1, result.Categories[0].DisplayOrder);
    }

    [Fact]
    public async Task Handle_EmptyRepository_ReturnsEmptyList()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<MenuCategory>());

        var result = await _handler.Handle(new GetMenuCategories.Query(), CancellationToken.None);

        Assert.Empty(result.Categories);
    }

    [Fact]
    public async Task Handle_MapsAllDtoFields()
    {
        var categories = new List<MenuCategory>
        {
            new() { Id = 7, Name = "Drinks", DisplayOrder = 5 },
        };
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(categories);

        var result = await _handler.Handle(new GetMenuCategories.Query(), CancellationToken.None);

        var dto = result.Categories[0];
        Assert.Equal(7, dto.Id);
        Assert.Equal("Drinks", dto.Name);
        Assert.Equal(5, dto.DisplayOrder);
    }
}
