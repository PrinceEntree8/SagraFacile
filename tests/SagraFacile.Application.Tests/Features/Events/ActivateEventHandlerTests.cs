using NSubstitute;
using SagraFacile.Application.Features.Events;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Events;

namespace SagraFacile.Application.Tests.Features.Events;

public class ActivateEventHandlerTests
{
    private readonly IEventRepository _repository = Substitute.For<IEventRepository>();
    private readonly ActivateEvent.Handler _handler;

    public ActivateEventHandlerTests()
    {
        _handler = new ActivateEvent.Handler(_repository);
    }

    [Fact]
    public async Task Handle_ExistingEvent_DeactivatesAllAndActivatesTarget()
    {
        // Arrange
        var ev = new Event { Id = 5, Name = "Test", IsActive = false };
        _repository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(ev);

        // Act
        var result = await _handler.Handle(new ActivateEvent.Command(5), CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.True(ev.IsActive);
        await _repository.Received(1).DeactivateAllAsync(Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NonExistentEvent_ReturnsFailure()
    {
        // Arrange
        _repository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Event?)null);

        // Act
        var result = await _handler.Handle(new ActivateEvent.Command(99), CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Event not found", result.Message);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
