using NSubstitute;
using SagraFacile.Application.Features.Reservations;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Reservations;

namespace SagraFacile.Application.Tests.Features.Reservations;

public class SeatReservationHandlerTests
{
    private readonly IReservationRepository _repository = Substitute.For<IReservationRepository>();
    private readonly SeatReservation.Handler _handler;

    public SeatReservationHandlerTests()
    {
        _handler = new SeatReservation.Handler(_repository);
    }

    [Fact]
    public async Task Handle_CalledReservation_SetsStatusToSeated()
    {
        // Arrange
        var reservation = new TableReservation { Id = 1, QueueNumber = "202601010001", Status = "Called" };
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(reservation);

        // Act
        var result = await _handler.Handle(new SeatReservation.Command(1), CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Seated", reservation.Status);
        Assert.NotNull(reservation.SeatedAt);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AlreadySeatedReservation_ReturnsFailure()
    {
        // Arrange
        var reservation = new TableReservation { Id = 2, Status = "Seated" };
        _repository.GetByIdAsync(2, Arg.Any<CancellationToken>()).Returns(reservation);

        // Act
        var result = await _handler.Handle(new SeatReservation.Command(2), CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_VoidedReservation_ReturnsFailure()
    {
        // Arrange
        var reservation = new TableReservation { Id = 3, Status = "Voided" };
        _repository.GetByIdAsync(3, Arg.Any<CancellationToken>()).Returns(reservation);

        // Act
        var result = await _handler.Handle(new SeatReservation.Command(3), CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
