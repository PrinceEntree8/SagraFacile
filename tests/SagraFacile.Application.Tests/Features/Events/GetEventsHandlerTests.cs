using NSubstitute;
using SagraFacile.Application.Features.Events;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Events;

namespace SagraFacile.Application.Tests.Features.Events;

public class GetEventsHandlerTests
{
    private readonly IEventRepository _repository = Substitute.For<IEventRepository>();
    private readonly GetEvents.Handler _handler;

    public GetEventsHandlerTests()
    {
        _handler = new GetEvents.Handler(_repository);
    }

    [Fact]
    public async Task Handle_ReturnsAllEventsAsDto()
    {
        // Arrange
        var events = new List<Event>
        {
            new() { Id = 1, Name = "Sagra 2026", Description = "Desc", Date = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc), Currency = "EUR", CurrencySymbol = "€", IsActive = true },
            new() { Id = 2, Name = "Sagra 2025", Description = "", Date = new DateTime(2025, 8, 1, 0, 0, 0, DateTimeKind.Utc), Currency = "EUR", CurrencySymbol = "€", IsActive = false }
        };
        _repository.GetAllOrderedByDateDescAsync(Arg.Any<CancellationToken>()).Returns(events);

        // Act
        var result = await _handler.Handle(new GetEvents.Query(), CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Events.Count);
        Assert.Equal("Sagra 2026", result.Events[0].Name);
        Assert.True(result.Events[0].IsActive);
    }

    [Fact]
    public async Task Handle_EmptyRepository_ReturnsEmptyList()
    {
        // Arrange
        _repository.GetAllOrderedByDateDescAsync(Arg.Any<CancellationToken>()).Returns([]);

        // Act
        var result = await _handler.Handle(new GetEvents.Query(), CancellationToken.None);

        // Assert
        Assert.Empty(result.Events);
    }
}
