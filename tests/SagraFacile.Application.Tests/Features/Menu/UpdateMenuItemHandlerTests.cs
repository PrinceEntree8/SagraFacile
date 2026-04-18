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
        var item = new MenuItem { Id = 1, EventId = 1, Name = "Old Name", PriceInCents = 500, CategoryId = 1, MenuItemAllergens = new List<MenuItemAllergen>() };
        _repo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(item);

        var command = new UpdateMenuItem.Command(1, "New Name", "New desc", 1000, 2, new List<int> { 2 }, true);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("New Name", item.Name);
        Assert.Equal(1000, item.PriceInCents);
        Assert.Single(item.MenuItemAllergens);
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ItemNotFound_ReturnsFalse()
    {
        _repo.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((MenuItem?)null);
        var command = new UpdateMenuItem.Command(99, "Name", "", 500, 1, new List<int>(), true);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Item not found", result.Message);
        await _repo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Validator_EmptyName_Fails()
    {
        var validator = new UpdateMenuItem.Validator();
        var command = new UpdateMenuItem.Command(1, "", "", 500, 1, new List<int>(), true);
        var result = validator.Validate(command);
        Assert.False(result.IsValid);
    }
}
