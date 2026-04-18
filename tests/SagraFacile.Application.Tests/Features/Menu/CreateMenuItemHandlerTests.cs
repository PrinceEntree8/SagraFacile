using NSubstitute;
using SagraFacile.Application.Features.Menu;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Menu;

namespace SagraFacile.Application.Tests.Features.Menu;

public class CreateMenuItemHandlerTests
{
    private readonly IMenuRepository _repo = Substitute.For<IMenuRepository>();
    private readonly CreateMenuItem.Handler _handler;

    public CreateMenuItemHandlerTests()
    {
        _handler = new CreateMenuItem.Handler(_repo);
    }

    [Fact]
    public async Task Handle_ValidCommand_AddsItemAndSaves()
    {
        // Arrange
        var command = new CreateMenuItem.Command(1, "Pizza", "Tomato sauce", 8.50m, MenuCategory.MainCourse, new List<int>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _repo.Received(1).AddAsync(Arg.Is<MenuItem>(m =>
            m.EventId == 1 && m.Name == "Pizza" && m.Price == 8.50m), Arg.Any<CancellationToken>());
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        Assert.Equal("Pizza", result.Name);
    }

    [Fact]
    public async Task Handle_ValidCommand_WithAllergens_SetsAllergenIds()
    {
        // Arrange
        var command = new CreateMenuItem.Command(1, "Pasta", "", 7.00m, MenuCategory.MainCourse, new List<int> { 1, 3 });

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _repo.Received(1).AddAsync(
            Arg.Is<MenuItem>(m => m.MenuItemAllergens.Count == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Validator_EmptyName_Fails()
    {
        var validator = new CreateMenuItem.Validator();
        var command = new CreateMenuItem.Command(1, "", "desc", 5m, MenuCategory.Starters, new List<int>());
        var result = validator.Validate(command);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(command.Name));
    }

    [Fact]
    public void Validator_NegativePrice_Fails()
    {
        var validator = new CreateMenuItem.Validator();
        var command = new CreateMenuItem.Command(1, "Item", "desc", -1m, MenuCategory.Starters, new List<int>());
        var result = validator.Validate(command);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(command.Price));
    }

    [Fact]
    public void Validator_ValidCommand_Passes()
    {
        var validator = new CreateMenuItem.Validator();
        var command = new CreateMenuItem.Command(1, "Item", "desc", 0m, MenuCategory.Starters, new List<int>());
        var result = validator.Validate(command);
        Assert.True(result.IsValid);
    }
}
