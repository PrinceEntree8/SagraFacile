using FluentValidation;
using NSubstitute;
using SagraFacile.Application.Features.Menu;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Menu;

namespace SagraFacile.Application.Tests.Features.Menu;

public class CreateMenuCategoryHandlerTests
{
    private readonly IMenuCategoryRepository _repo = Substitute.For<IMenuCategoryRepository>();
    private readonly CreateMenuCategory.Handler _handler;

    public CreateMenuCategoryHandlerTests()
    {
        _handler = new CreateMenuCategory.Handler(_repo);
    }

    [Fact]
    public async Task Handle_ValidCommand_AddsCategoryAndSaves()
    {
        var command = new CreateMenuCategory.Command("Starters", 1);

        _repo.When(r => r.AddAsync(Arg.Any<MenuCategory>(), Arg.Any<CancellationToken>()))
            .Do(ci => ci.Arg<MenuCategory>().Id = 10);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal("Starters", result.Name);
        Assert.Equal(10, result.Id);
        await _repo.Received(1).AddAsync(
            Arg.Is<MenuCategory>(c => c.Name == "Starters" && c.DisplayOrder == 1),
            Arg.Any<CancellationToken>());
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DefaultDisplayOrder_IsZero()
    {
        var command = new CreateMenuCategory.Command("Drinks");

        MenuCategory? saved = null;
        _repo.When(r => r.AddAsync(Arg.Any<MenuCategory>(), Arg.Any<CancellationToken>()))
            .Do(ci => saved = ci.Arg<MenuCategory>());

        await _handler.Handle(command, CancellationToken.None);

        Assert.NotNull(saved);
        Assert.Equal(0, saved!.DisplayOrder);
    }

    [Fact]
    public void Validator_EmptyName_Fails()
    {
        var validator = new CreateMenuCategory.Validator();
        var result = validator.Validate(new CreateMenuCategory.Command(""));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateMenuCategory.Command.Name));
    }

    [Fact]
    public void Validator_NameTooLong_Fails()
    {
        var validator = new CreateMenuCategory.Validator();
        var longName = new string('x', 101);
        var result = validator.Validate(new CreateMenuCategory.Command(longName));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validator_ValidCommand_Passes()
    {
        var validator = new CreateMenuCategory.Validator();
        var result = validator.Validate(new CreateMenuCategory.Command("Dessert", 5));
        Assert.True(result.IsValid);
    }
}
