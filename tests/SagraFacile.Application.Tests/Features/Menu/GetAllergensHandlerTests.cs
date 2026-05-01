using NSubstitute;
using SagraFacile.Application.Features.Menu;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Menu;

namespace SagraFacile.Application.Tests.Features.Menu;

public class GetAllergensHandlerTests
{
    private readonly IAllergenRepository _repo = Substitute.For<IAllergenRepository>();
    private readonly GetAllergens.Handler _handler;

    public GetAllergensHandlerTests()
    {
        _handler = new GetAllergens.Handler(_repo);
    }

    [Fact]
    public async Task Handle_ReturnsAllAllergensAsDtos()
    {
        var allergens = new List<Allergen>
        {
            new() { Id = 1, Code = "GLUTEN", Icon = "🌾" },
            new() { Id = 2, Code = "MILK", Icon = "🥛" },
        };
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(allergens);

        var result = await _handler.Handle(new GetAllergens.Query(), CancellationToken.None);

        Assert.Equal(2, result.Allergens.Count);
        Assert.Equal("GLUTEN", result.Allergens[0].Code);
        Assert.Equal("🌾", result.Allergens[0].Icon);
        Assert.Equal(1, result.Allergens[0].Id);
    }

    [Fact]
    public async Task Handle_EmptyRepository_ReturnsEmptyList()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Allergen>());

        var result = await _handler.Handle(new GetAllergens.Query(), CancellationToken.None);

        Assert.Empty(result.Allergens);
    }

    [Fact]
    public async Task Handle_MapsAllAllergenFields()
    {
        var allergens = new List<Allergen>
        {
            new() { Id = 5, Code = "PEANUTS", Icon = "🥜" },
        };
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(allergens);

        var result = await _handler.Handle(new GetAllergens.Query(), CancellationToken.None);

        var dto = result.Allergens[0];
        Assert.Equal(5, dto.Id);
        Assert.Equal("PEANUTS", dto.Code);
        Assert.Equal("🥜", dto.Icon);
    }
}
