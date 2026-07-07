using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SagraFacile.Application.Exceptions;
using SagraFacile.Application.Features.Reservations;
using SagraFacile.Application.Interfaces;
using SagraFacile.Contracts.Reservations;
using SagraFacile.Domain.Features.Reservations;

namespace SagraFacile.Application.Tests.Features.Reservations;

public class RestoreReservationHandlerTests
{
    private readonly IReservationRepository _repository = Substitute.For<IReservationRepository>();
    private readonly IReservationNotifier _notifier = Substitute.For<IReservationNotifier>();
    private readonly RestoreReservation.Handler _handler;

    public RestoreReservationHandlerTests()
    {
        _handler = new RestoreReservation.Handler(_repository, _notifier);
    }

    [Fact]
    public async Task Handle_VoidedReservation_SetsStatusToWaiting()
    {
        // Arrange
        var reservation = new Reservation { Id = 1, EventId = 1, SequenceNumber = 5, Status = ReservationStatus.Voided, VoidedAt = DateTime.UtcNow };
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(reservation);
        _repository.GetCountersAsync(1, Arg.Any<CancellationToken>()).Returns([]);

        // Act
        var result = await _handler.Handle(new RestoreReservation.Command(1), CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(ReservationStatus.Waiting, reservation.Status);
        Assert.Null(reservation.VoidedAt);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _notifier.Received(1).EnqueueStatusChangedAsync(
            Arg.Is<ReservationStatusChangedNotification>(x =>
                x.ReservationId == reservation.Id &&
                x.SequenceNumber == reservation.SequenceNumber &&
                x.NewStatus == ReservationStatus.Waiting &&
                x.OldStatus == ReservationStatus.Voided &&
                x.CallCount == null),
            Arg.Any<CancellationToken>());
        await _notifier.Received(1).EnqueueCountersUpdatedAsync(
            Arg.Any<CountersUpdatedNotification>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WaitingReservation_ReturnsFailure()
    {
        // Arrange
        var reservation = new Reservation { Id = 2, EventId = 1, SequenceNumber = 2, Status = ReservationStatus.Waiting };
        _repository.GetByIdAsync(2, Arg.Any<CancellationToken>()).Returns(reservation);

        // Act
        var result = await _handler.Handle(new RestoreReservation.Command(2), CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SeatedReservation_ReturnsFailure()
    {
        // Arrange
        var reservation = new Reservation { Id = 3, EventId = 1, SequenceNumber = 3, Status = ReservationStatus.Seated };
        _repository.GetByIdAsync(3, Arg.Any<CancellationToken>()).Returns(reservation);

        // Act
        var result = await _handler.Handle(new RestoreReservation.Command(3), CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReservationNotFound_ReturnsFailure()
    {
        // Arrange
        _repository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Reservation?)null);

        // Act
        var result = await _handler.Handle(new RestoreReservation.Command(99), CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ConcurrentModification_ReturnsFailure()
    {
        // Arrange
        var reservation = new Reservation { Id = 1, EventId = 1, SequenceNumber = 1, Status = ReservationStatus.Voided };
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(reservation);
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new RepositoryConcurrencyException());

        // Act
        var result = await _handler.Handle(new RestoreReservation.Command(1), CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("modified by another user", result.Message);
        await _notifier.DidNotReceive().EnqueueStatusChangedAsync(
            Arg.Any<ReservationStatusChangedNotification>(),
            Arg.Any<CancellationToken>());
    }
}
