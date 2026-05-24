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
    private readonly IReservationNotifier _notifier = Substitute.For<IReservationNotifier>();
    private readonly VoidReservation.Handler _handler;

    public VoidReservationHandlerTests()
    {
        _handler = new VoidReservation.Handler(_repository, _notifier);
    }

    [Fact]
    public async Task Handle_WaitingReservation_SetsStatusToVoided()
    {
        // Arrange
        var reservation = new Reservation { Id = 1, EventId = 1, SequenceNumber = 1, Status = ReservationStatus.Waiting };
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(reservation);

        // Act
        var result = await _handler.Handle(new VoidReservation.Command(1), CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(ReservationStatus.Voided, reservation.Status);
        Assert.NotNull(reservation.VoidedAt);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _notifier.Received(1).NotifyReservationVoidedAsync(
            reservation.Id,
            reservation.SequenceNumber,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SeatedReservation_ReturnsFailure()
    {
        // Arrange
        var reservation = new Reservation { Id = 2, EventId = 1, SequenceNumber = 2, Status = ReservationStatus.Seated };
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
        var reservation = new Reservation { Id = 3, EventId = 1, SequenceNumber = 3, Status = ReservationStatus.Voided };
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
        var reservation = new Reservation { Id = 1, EventId = 1, SequenceNumber = 1, Status = ReservationStatus.Waiting };
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(reservation);
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new RepositoryConcurrencyException());

        // Act
        var result = await _handler.Handle(new VoidReservation.Command(1), CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("modified by another user", result.Message);
        await _notifier.DidNotReceive().NotifyReservationVoidedAsync(
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }
}
