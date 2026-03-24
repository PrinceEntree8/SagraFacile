using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SagraFacile.Application.Exceptions;
using SagraFacile.Application.Features.Reservations;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Reservations;

namespace SagraFacile.Application.Tests.Features.Reservations;

public class VoidReservationHandlerTests
{
    private readonly IReservationRepository _repository = Substitute.For<IReservationRepository>();
    private readonly VoidReservation.Handler _handler;

    public VoidReservationHandlerTests()
    {
        _handler = new VoidReservation.Handler(_repository);
    }

    [Fact]
    public async Task Handle_WaitingReservation_SetsStatusToVoided()
    {
        // Arrange
        var reservation = new TableReservation { Id = 1, QueueNumber = "202601010001", Status = "Waiting" };
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(reservation);

        // Act
        var result = await _handler.Handle(new VoidReservation.Command(1), CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Voided", reservation.Status);
        Assert.NotNull(reservation.VoidedAt);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SeatedReservation_ReturnsFailure()
    {
        // Arrange
        var reservation = new TableReservation { Id = 2, Status = "Seated" };
        _repository.GetByIdAsync(2, Arg.Any<CancellationToken>()).Returns(reservation);

        // Act
        var result = await _handler.Handle(new VoidReservation.Command(2), CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AlreadyVoidedReservation_ReturnsFailure()
    {
        // Arrange
        var reservation = new TableReservation { Id = 3, Status = "Voided" };
        _repository.GetByIdAsync(3, Arg.Any<CancellationToken>()).Returns(reservation);

        // Act
        var result = await _handler.Handle(new VoidReservation.Command(3), CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ConcurrentModification_ReturnsFailure()
    {
        // Arrange
        var reservation = new TableReservation { Id = 1, QueueNumber = "202601010001", Status = "Waiting" };
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(reservation);
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new RepositoryConcurrencyException());

        // Act
        var result = await _handler.Handle(new VoidReservation.Command(1), CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("modified by another user", result.Message);
    }
}
