using NSubstitute;
using SagraFacile.Application.Features.Menu;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Menu;

namespace SagraFacile.Application.Tests.Features.Menu;

public class UpdateMenuItemHandlerTests
{
    private readonly IMenuRepository _repo = Substitute.For<IMenuRepository>();
    private readonly UpdateMenuItem.Handler _handler;

    public UpdateMenuItemHandlerTests()
    {
        _handler = new UpdateMenuItem.Handler(_repo);
    }

    [Fact]
    public async Task Handle_ExistingItem_UpdatesAndReturnsSuccess()
    {
        // Arrange
        var item = new MenuItem { Id = 1, EventId = 1, Name = "Old Name", Price = 5m, MenuItemAllergens = new List<MenuItemAllergen>() };
        _repo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(item);

        var command = new UpdateMenuItem.Command(1, "New Name", "New desc", 10m, MenuCategory.Dessert, new List<int> { 2 }, true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("New Name", item.Name);
        Assert.Equal(10m, item.Price);
        Assert.Single(item.MenuItemAllergens);
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ItemNotFound_ReturnsFalse()
    {
        // Arrange
        _repo.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((MenuItem?)null);
        var command = new UpdateMenuItem.Command(99, "Name", "", 5m, MenuCategory.Starters, new List<int>(), true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Item not found", result.Message);
        await _repo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Validator_EmptyName_Fails()
    {
        var validator = new UpdateMenuItem.Validator();
        var command = new UpdateMenuItem.Command(1, "", "", 5m, MenuCategory.Starters, new List<int>(), true);
        var result = validator.Validate(command);
        Assert.False(result.IsValid);
    }
}
