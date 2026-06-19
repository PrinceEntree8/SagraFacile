using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SagraFacile.Application.Exceptions;
using SagraFacile.Application.Features.Reservations;
using SagraFacile.Application.Interfaces;
using SagraFacile.Contracts.Reservations;
using SagraFacile.Domain.Features.Reservations;

namespace SagraFacile.Application.Tests.Features.Reservations;

public class SeatReservationHandlerTests
{
    private readonly IReservationRepository _repository = Substitute.For<IReservationRepository>();
    private readonly IReservationNotifier _notifier = Substitute.For<IReservationNotifier>();
    private readonly SeatReservation.Handler _handler;

    public SeatReservationHandlerTests()
    {
        _handler = new SeatReservation.Handler(_repository, _notifier);
    }

    [Fact]
    public async Task Handle_CalledReservation_SetsStatusToSeated()
    {
        // Arrange
        var reservation = new Reservation { Id = 1, EventId = 1, SequenceNumber = 1, Status = ReservationStatus.Called };
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(reservation);

        // Act
        var result = await _handler.Handle(new SeatReservation.Command(1), CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(ReservationStatus.Seated, reservation.Status);
        Assert.NotNull(reservation.SeatedAt);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _notifier.Received(1).EnqueueStatusChangedAsync(
            Arg.Is<ReservationStatusChangedNotification>(x =>
                x.ReservationId == 1 &&
                x.SequenceNumber == 1 &&
                x.NewStatus == ReservationStatus.Seated &&
                x.OldStatus == ReservationStatus.Called &&
                x.CallCount == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AlreadySeatedReservation_ReturnsFailure()
    {
        // Arrange
        var reservation = new Reservation { Id = 2, EventId = 1, SequenceNumber = 2, Status = ReservationStatus.Seated };
        _repository.GetByIdAsync(2, Arg.Any<CancellationToken>()).Returns(reservation);

        // Act
        var result = await _handler.Handle(new SeatReservation.Command(2), CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _notifier.DidNotReceive().EnqueueStatusChangedAsync(
            Arg.Any<ReservationStatusChangedNotification>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_VoidedReservation_ReturnsFailure()
    {
        // Arrange
        var reservation = new Reservation { Id = 3, EventId = 1, SequenceNumber = 3, Status = ReservationStatus.Voided };
        _repository.GetByIdAsync(3, Arg.Any<CancellationToken>()).Returns(reservation);

        // Act
        var result = await _handler.Handle(new SeatReservation.Command(3), CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _notifier.DidNotReceive().EnqueueStatusChangedAsync(
            Arg.Any<ReservationStatusChangedNotification>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ConcurrentModification_ReturnsFailure()
    {
        // Arrange
        var reservation = new Reservation { Id = 1, EventId = 1, SequenceNumber = 1, Status = ReservationStatus.Called };
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(reservation);
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new RepositoryConcurrencyException());

        // Act
        var result = await _handler.Handle(new SeatReservation.Command(1), CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("modified by another user", result.Message);
        await _notifier.DidNotReceive().EnqueueStatusChangedAsync(
            Arg.Any<ReservationStatusChangedNotification>(),
            Arg.Any<CancellationToken>());
    }
}
