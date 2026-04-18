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
        var command = new CreateMenuItem.Command(1, "Pizza", "Tomato sauce", 850, 1, new List<int>());

        var result = await _handler.Handle(command, CancellationToken.None);

        await _repo.Received(1).AddAsync(
            Arg.Is<MenuItem>(m => m.EventId == 1 && m.Name == "Pizza" && m.PriceInCents == 850),
            Arg.Any<CancellationToken>());
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        Assert.Equal("Pizza", result.Name);
    }

    [Fact]
    public async Task Handle_ValidCommand_WithAllergens_SetsAllergenIds()
    {
        var command = new CreateMenuItem.Command(1, "Pasta", "", 700, 1, new List<int> { 1, 3 });

        await _handler.Handle(command, CancellationToken.None);

        await _repo.Received(1).AddAsync(
            Arg.Is<MenuItem>(m => m.MenuItemAllergens.Count == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Validator_EmptyName_Fails()
    {
        var validator = new CreateMenuItem.Validator();
        var command = new CreateMenuItem.Command(1, "", "desc", 500, 1, new List<int>());
        var result = validator.Validate(command);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(command.Name));
    }

    [Fact]
    public void Validator_NegativePrice_Fails()
    {
        var validator = new CreateMenuItem.Validator();
        var command = new CreateMenuItem.Command(1, "Item", "desc", -1, 1, new List<int>());
        var result = validator.Validate(command);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(command.PriceInCents));
    }

    [Fact]
    public void Validator_ValidCommand_Passes()
    {
        var validator = new CreateMenuItem.Validator();
        var command = new CreateMenuItem.Command(1, "Item", "desc", 0, 1, new List<int>());
        var result = validator.Validate(command);
        Assert.True(result.IsValid);
    }
}
