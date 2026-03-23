using NSubstitute;
using SagraFacile.Application.Features.Events;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Events;

namespace SagraFacile.Application.Tests.Features.Events;

public class CreateEventHandlerTests
{
    private readonly IEventRepository _repository = Substitute.For<IEventRepository>();
    private readonly CreateEvent.Handler _handler;

    public CreateEventHandlerTests()
    {
        _handler = new CreateEvent.Handler(_repository);
    }

    [Fact]
    public async Task Handle_ValidCommand_AddsEventAndSavesChanges()
    {
        // Arrange
        var command = new CreateEvent.Command("Sagra 2026", "Descrizione", DateTime.UtcNow, "EUR", "€");

        _repository.When(r => r.AddAsync(Arg.Any<Event>(), Arg.Any<CancellationToken>()))
            .Do(ci => ci.Arg<Event>().Id = 1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal("Sagra 2026", result.Name);
        await _repository.Received(1).AddAsync(Arg.Is<Event>(e => e.Name == "Sagra 2026"), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SetsDateToUtc()
    {
        // Arrange
        var localDate = new DateTime(2026, 8, 15, 12, 0, 0);
        var command = new CreateEvent.Command("Test", "", localDate, "EUR", "€");

        Event? savedEvent = null;
        _repository.When(r => r.AddAsync(Arg.Any<Event>(), Arg.Any<CancellationToken>()))
            .Do(ci => savedEvent = ci.Arg<Event>());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(savedEvent);
        Assert.Equal(DateTimeKind.Utc, savedEvent!.Date.Kind);
        Assert.False(savedEvent.IsActive);
    }
}
