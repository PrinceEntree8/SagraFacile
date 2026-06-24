using NSubstitute;
using SagraFacile.Application.Features.Reservations;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Reservations;

namespace SagraFacile.Application.Tests.Features.Reservations;

public class GetBestFitReservationHandlerTests
{
    private readonly IReservationRepository _repository = Substitute.For<IReservationRepository>();
    private readonly GetBestFitReservation.Handler _handler;

    public GetBestFitReservationHandlerTests()
    {
        _handler = new GetBestFitReservation.Handler(_repository);
    }

    [Fact]
    public async Task Handle_ExactMatch_ReturnsAsPerfect()
    {
        // Arrange
        var reservations = new List<Reservation>
        {
            new() { Id = 1, EventId = 1, SequenceNumber = 1, CustomerName = "Mario", PartySize = 4, Status = ReservationStatus.Called, CreatedAt = DateTime.UtcNow.AddMinutes(-10) }
        };
        _repository.GetCalledReservationsOrderedByCreatedAtAsync(1, Arg.Any<CancellationToken>()).Returns(reservations);

        // Act
        var result = await _handler.Handle(new GetBestFitReservation.Query(1, 4), CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal("Perfect", result[0].MatchQuality);
    }

    [Fact]
    public async Task Handle_OneUnderCoverCount_ReturnsAsGood()
    {
        // Arrange
        var reservations = new List<Reservation>
        {
            new() { Id = 1, EventId = 1, SequenceNumber = 1, CustomerName = "Mario", PartySize = 3, Status = ReservationStatus.Called, CreatedAt = DateTime.UtcNow }
        };
        _repository.GetCalledReservationsOrderedByCreatedAtAsync(1, Arg.Any<CancellationToken>()).Returns(reservations);

        // Act
        var result = await _handler.Handle(new GetBestFitReservation.Query(1, 4), CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal("Good", result[0].MatchQuality);
    }

    [Fact]
    public async Task Handle_ZeroCoverCount_ReturnsAllCalledWithMatchQualityAll()
    {
        // Arrange
        var reservations = new List<Reservation>
        {
            new() { Id = 1, EventId = 1, SequenceNumber = 1, PartySize = 2, Status = ReservationStatus.Called, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, EventId = 1, SequenceNumber = 2, PartySize = 6, Status = ReservationStatus.Called, CreatedAt = DateTime.UtcNow }
        };
        _repository.GetCalledReservationsOrderedByCreatedAtAsync(1, Arg.Any<CancellationToken>()).Returns(reservations);

        // Act
        var result = await _handler.Handle(new GetBestFitReservation.Query(1, 0), CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, m => Assert.Equal("All", m.MatchQuality));
    }

    [Fact]
    public async Task Handle_PartyLargerThanTable_NotIncluded()
    {
        // Arrange
        var reservations = new List<Reservation>
        {
            new() { Id = 1, EventId = 1, SequenceNumber = 1, PartySize = 6, Status = ReservationStatus.Called, CreatedAt = DateTime.UtcNow }
        };
        _repository.GetCalledReservationsOrderedByCreatedAtAsync(1, Arg.Any<CancellationToken>()).Returns(reservations);

        // Act
        var result = await _handler.Handle(new GetBestFitReservation.Query(1, 4), CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }
}
