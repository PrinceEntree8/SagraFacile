using NSubstitute;
using SagraFacile.Application.Features.Menu;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Menu;

namespace SagraFacile.Application.Tests.Features.Menu;

public class DeleteMenuItemHandlerTests
{
    private readonly IMenuRepository _repo = Substitute.For<IMenuRepository>();
    private readonly DeleteMenuItem.Handler _handler;

    public DeleteMenuItemHandlerTests()
    {
        _handler = new DeleteMenuItem.Handler(_repo);
    }

    [Fact]
    public async Task Handle_ExistingItem_DeletesAndReturnsSuccess()
    {
        // Arrange
        var item = new MenuItem { Id = 1, Name = "Tiramisu", EventId = 1 };
        _repo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(item);

        // Act
        var result = await _handler.Handle(new DeleteMenuItem.Command(1), CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        await _repo.Received(1).DeleteAsync(1, Arg.Any<CancellationToken>());
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ItemNotFound_ReturnsFalse()
    {
        // Arrange
        _repo.GetByIdAsync(42, Arg.Any<CancellationToken>()).Returns((MenuItem?)null);

        // Act
        var result = await _handler.Handle(new DeleteMenuItem.Command(42), CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Item not found", result.Message);
        await _repo.DidNotReceive().DeleteAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _repo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
