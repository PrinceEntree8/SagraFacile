using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SagraFacile.Application.Exceptions;
using SagraFacile.Application.Features.Reservations;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Reservations;

namespace SagraFacile.Application.Tests.Features.Reservations;

public class CallReservationHandlerTests
{
    private readonly IReservationRepository _repository = Substitute.For<IReservationRepository>();
    private readonly CallReservation.Handler _handler;

    public CallReservationHandlerTests()
    {
        _handler = new CallReservation.Handler(_repository);
    }

    [Fact]
    public async Task Handle_WaitingReservation_SetsStatusToCalledAndIncrementsCount()
    {
        // Arrange
        var reservation = new TableReservation { Id = 1, QueueNumber = "202601010001", Status = "Waiting", CallCount = 0 };
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(reservation);

        // Act
        var result = await _handler.Handle(new CallReservation.Command(1, "Receptionist"), CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Called", reservation.Status);
        Assert.Equal(1, reservation.CallCount);
        Assert.NotNull(reservation.FirstCalledAt);
        Assert.NotNull(reservation.LastCalledAt);
        await _repository.Received(1).AddCallAsync(Arg.Any<ReservationCall>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AlreadyCalledReservation_DoesNotOverwriteFirstCalledAt()
    {
        // Arrange
        var firstCall = DateTime.UtcNow.AddMinutes(-5);
        var reservation = new TableReservation
        {
            Id = 2, QueueNumber = "202601010002", Status = "Called",
            CallCount = 1, FirstCalledAt = firstCall
        };
        _repository.GetByIdAsync(2, Arg.Any<CancellationToken>()).Returns(reservation);

        // Act
        await _handler.Handle(new CallReservation.Command(2, "Receptionist"), CancellationToken.None);

        // Assert
        Assert.Equal(firstCall, reservation.FirstCalledAt);
        Assert.Equal(2, reservation.CallCount);
    }

    [Fact]
    public async Task Handle_VoidedReservation_ReturnsFailure()
    {
        // Arrange
        var reservation = new TableReservation { Id = 3, Status = "Voided" };
        _repository.GetByIdAsync(3, Arg.Any<CancellationToken>()).Returns(reservation);

        // Act
        var result = await _handler.Handle(new CallReservation.Command(3), CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SeatedReservation_ReturnsFailure()
    {
        // Arrange
        var reservation = new TableReservation { Id = 4, Status = "Seated" };
        _repository.GetByIdAsync(4, Arg.Any<CancellationToken>()).Returns(reservation);

        // Act
        var result = await _handler.Handle(new CallReservation.Command(4), CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NonExistentReservation_ReturnsFailure()
    {
        // Arrange
        _repository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((TableReservation?)null);

        // Act
        var result = await _handler.Handle(new CallReservation.Command(99), CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Reservation not found", result.Message);
    }

    [Fact]
    public async Task Handle_ConcurrentModification_ReturnsFailure()
    {
        // Arrange
        var reservation = new TableReservation { Id = 1, QueueNumber = "202601010001", Status = "Waiting", CallCount = 0 };
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(reservation);
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new RepositoryConcurrencyException());

        // Act
        var result = await _handler.Handle(new CallReservation.Command(1, "Receptionist"), CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("modified by another user", result.Message);
    }
}
