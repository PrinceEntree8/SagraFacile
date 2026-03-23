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
        var reservations = new List<TableReservation>
        {
            new() { Id = 1, QueueNumber = "001", CustomerName = "Mario", PartySize = 4, Status = "Called", CreatedAt = DateTime.UtcNow.AddMinutes(-10) }
        };
        _repository.GetCalledReservationsOrderedByCreatedAtAsync(Arg.Any<CancellationToken>()).Returns(reservations);

        // Act
        var result = await _handler.Handle(new GetBestFitReservation.Query(4), CancellationToken.None);

        // Assert
        Assert.Single(result.Matches);
        Assert.Equal("Perfect", result.Matches[0].MatchQuality);
    }

    [Fact]
    public async Task Handle_OneUnderCoverCount_ReturnsAsGood()
    {
        // Arrange
        var reservations = new List<TableReservation>
        {
            new() { Id = 1, QueueNumber = "001", CustomerName = "Mario", PartySize = 3, Status = "Called", CreatedAt = DateTime.UtcNow }
        };
        _repository.GetCalledReservationsOrderedByCreatedAtAsync(Arg.Any<CancellationToken>()).Returns(reservations);

        // Act
        var result = await _handler.Handle(new GetBestFitReservation.Query(4), CancellationToken.None);

        // Assert
        Assert.Single(result.Matches);
        Assert.Equal("Good", result.Matches[0].MatchQuality);
    }

    [Fact]
    public async Task Handle_ZeroCoverCount_ReturnsAllCalledWithMatchQualityAll()
    {
        // Arrange
        var reservations = new List<TableReservation>
        {
            new() { Id = 1, PartySize = 2, Status = "Called", QueueNumber = "001", CreatedAt = DateTime.UtcNow },
            new() { Id = 2, PartySize = 6, Status = "Called", QueueNumber = "002", CreatedAt = DateTime.UtcNow }
        };
        _repository.GetCalledReservationsOrderedByCreatedAtAsync(Arg.Any<CancellationToken>()).Returns(reservations);

        // Act
        var result = await _handler.Handle(new GetBestFitReservation.Query(0), CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Matches.Count);
        Assert.All(result.Matches, m => Assert.Equal("All", m.MatchQuality));
    }

    [Fact]
    public async Task Handle_PartyLargerThanTable_NotIncluded()
    {
        // Arrange
        var reservations = new List<TableReservation>
        {
            new() { Id = 1, PartySize = 6, Status = "Called", QueueNumber = "001", CreatedAt = DateTime.UtcNow }
        };
        _repository.GetCalledReservationsOrderedByCreatedAtAsync(Arg.Any<CancellationToken>()).Returns(reservations);

        // Act
        var result = await _handler.Handle(new GetBestFitReservation.Query(4), CancellationToken.None);

        // Assert
        Assert.Empty(result.Matches);
    }
}
