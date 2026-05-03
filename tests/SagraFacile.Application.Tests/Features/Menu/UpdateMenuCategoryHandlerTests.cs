using NSubstitute;
using SagraFacile.Application.Features.Menu;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Menu;

namespace SagraFacile.Application.Tests.Features.Menu;

public class UpdateMenuCategoryHandlerTests
{
    private readonly IMenuCategoryRepository _repo = Substitute.For<IMenuCategoryRepository>();
    private readonly UpdateMenuCategory.Handler _handler;

    public UpdateMenuCategoryHandlerTests()
    {
        _handler = new UpdateMenuCategory.Handler(_repo);
    }

    [Fact]
    public async Task Handle_ExistingCategory_UpdatesAndReturnsSuccess()
    {
        var category = new MenuCategory { Id = 1, Name = "Old Name", DisplayOrder = 1 };
        _repo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(category);

        var command = new UpdateMenuCategory.Command(1, "New Name", 3);
        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("New Name", category.Name);
        Assert.Equal(3, category.DisplayOrder);
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NonExistentCategory_ReturnsFailure()
    {
        _repo.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((MenuCategory?)null);

        var result = await _handler.Handle(new UpdateMenuCategory.Command(99, "Name", 1), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Category not found", result.Message);
        await _repo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Validator_InvalidId_Fails()
    {
        var validator = new UpdateMenuCategory.Validator();
        var result = validator.Validate(new UpdateMenuCategory.Command(0, "Valid Name", 1));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateMenuCategory.Command.Id));
    }

    [Fact]
    public void Validator_EmptyName_Fails()
    {
        var validator = new UpdateMenuCategory.Validator();
        var result = validator.Validate(new UpdateMenuCategory.Command(1, "", 1));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateMenuCategory.Command.Name));
    }

    [Fact]
    public void Validator_ValidCommand_Passes()
    {
        var validator = new UpdateMenuCategory.Validator();
        var result = validator.Validate(new UpdateMenuCategory.Command(1, "Valid Name", 2));
        Assert.True(result.IsValid);
    }
}
