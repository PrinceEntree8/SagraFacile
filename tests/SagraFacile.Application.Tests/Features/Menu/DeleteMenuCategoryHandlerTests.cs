using NSubstitute;
using SagraFacile.Application.Features.Menu;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Menu;

namespace SagraFacile.Application.Tests.Features.Menu;

public class DeleteMenuCategoryHandlerTests
{
    private readonly IMenuCategoryRepository _repo = Substitute.For<IMenuCategoryRepository>();
    private readonly DeleteMenuCategory.Handler _handler;

    public DeleteMenuCategoryHandlerTests()
    {
        _handler = new DeleteMenuCategory.Handler(_repo);
    }

    [Fact]
    public async Task Handle_ExistingCategory_DeletesAndReturnsSuccess()
    {
        var category = new MenuCategory { Id = 5, Name = "Starters" };
        _repo.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(category);

        var result = await _handler.Handle(new DeleteMenuCategory.Command(5), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Contains("Starters", result.Message);
        await _repo.Received(1).DeleteAsync(5, Arg.Any<CancellationToken>());
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NonExistentCategory_ReturnsFailure()
    {
        _repo.GetByIdAsync(42, Arg.Any<CancellationToken>()).Returns((MenuCategory?)null);

        var result = await _handler.Handle(new DeleteMenuCategory.Command(42), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Category not found", result.Message);
        await _repo.DidNotReceive().DeleteAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _repo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
