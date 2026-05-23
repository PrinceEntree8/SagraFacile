using NSubstitute;
using SagraFacile.Application.Features.Reservations;
using SagraFacile.Application.Interfaces;

namespace SagraFacile.Application.Tests.Features.Reservations;

public class GetCountersHandlerTests
{
    private readonly IReservationRepository _repository = Substitute.For<IReservationRepository>();
    private readonly GetCounters.Handler _handler;

    public GetCountersHandlerTests()
    {
        _handler = new GetCounters.Handler(_repository);
    }

    [Fact]
    public async Task Handle_ReturnsCountersForSpecificEvent()
    {
        // Arrange
        var counters = new List<GetCounters.ReservationCounter>
        {
            new("Waiting", 3, 8),
            new("Called", 1, 2),
        };
        _repository.GetCountersAsync(1, Arg.Any<CancellationToken>()).Returns(counters);

        // Act
        var result = await _handler.Handle(new GetCounters.Query(1), CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Counters.Count);
        await _repository.Received(1).GetCountersAsync(1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DoesNotCountReservationsFromOtherEvents()
    {
        // Arrange — event 2 returns empty
        _repository.GetCountersAsync(2, Arg.Any<CancellationToken>()).Returns(new List<GetCounters.ReservationCounter>());

        // Act
        var result = await _handler.Handle(new GetCounters.Query(2), CancellationToken.None);

        // Assert
        Assert.Empty(result.Counters);
        await _repository.Received(1).GetCountersAsync(2, Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().GetCountersAsync(1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmptyEvent_ReturnsEmptyList()
    {
        // Arrange
        _repository.GetCountersAsync(99, Arg.Any<CancellationToken>()).Returns(new List<GetCounters.ReservationCounter>());

        // Act
        var result = await _handler.Handle(new GetCounters.Query(99), CancellationToken.None);

        // Assert
        Assert.Empty(result.Counters);
    }
}
