using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SagraFacile.Application.Exceptions;
using SagraFacile.Application.Features.Reservations;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Events;
using SagraFacile.Domain.Features.Reservations;

namespace SagraFacile.Application.Tests.Features.Reservations;

public class CallReservationHandlerTests
{
    private readonly IReservationRepository _repository = Substitute.For<IReservationRepository>();
    private readonly IReservationNotifier _notifier = Substitute.For<IReservationNotifier>();
    private readonly CallReservation.Handler _handler;

    public CallReservationHandlerTests()
    {
        _handler = new CallReservation.Handler(_repository, _notifier);
    }

    private static Reservation WithEventOptions(Reservation reservation, bool partyCompletionEnabled)
    {
        reservation.Event = new Event
        {
            AdditionalOptions = new EventAdditionalOptions
            {
                Reservations = new ReservationOptions
                {
                    PartyCompletion = new PartyCompletionOptions
                    {
                        Enabled = partyCompletionEnabled,
                        MinPartySize = 4
                    }
                }
            }
        };

        return reservation;
    }

    [Fact]
    public async Task Handle_WaitingReservation_SetsStatusToCalledAndIncrementsCount()
    {
        // Arrange
        var reservation = WithEventOptions(new Reservation { Id = 1, EventId = 1, SequenceNumber = 1, Status = ReservationStatus.Waiting, CallCount = 0 }, false);
        _repository.GetByIdWithEventAsync(1, Arg.Any<CancellationToken>()).Returns(reservation);

        // Act
        var result = await _handler.Handle(new CallReservation.Command(1, "Receptionist"), CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(ReservationStatus.Called, reservation.Status);
        Assert.Equal(1, reservation.CallCount);
        Assert.NotNull(reservation.FirstCalledAt);
        Assert.NotNull(reservation.LastCalledAt);
        await _repository.Received(1).AddCallAsync(Arg.Any<ReservationCall>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _notifier.Received(1).EnqueueStatusChangedAsync(
            Arg.Is<ReservationStatusChangedNotification>(x =>
                x.ReservationId == reservation.Id &&
                x.SequenceNumber == reservation.SequenceNumber &&
                x.CustomerName == reservation.CustomerName &&
                x.PartySize == reservation.PartySize &&
                x.NewStatus == ReservationStatus.Called &&
                x.OldStatus == ReservationStatus.Waiting &&
                x.CallCount == reservation.CallCount),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AlreadyCalledReservation_DoesNotOverwriteFirstCalledAt()
    {
        // Arrange
        var firstCall = DateTime.UtcNow.AddMinutes(-5);
        var reservation = new Reservation
        {
            Id = 2, EventId = 1, SequenceNumber = 2, Status = ReservationStatus.Called,
            CallCount = 1, FirstCalledAt = firstCall
        };
        _repository.GetByIdWithEventAsync(2, Arg.Any<CancellationToken>()).Returns(WithEventOptions(reservation, false));

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
        var reservation = WithEventOptions(new Reservation { Id = 3, EventId = 1, SequenceNumber = 3, Status = ReservationStatus.Voided }, false);
        _repository.GetByIdWithEventAsync(3, Arg.Any<CancellationToken>()).Returns(reservation);

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
        var reservation = WithEventOptions(new Reservation { Id = 4, EventId = 1, SequenceNumber = 4, Status = ReservationStatus.Seated }, false);
        _repository.GetByIdWithEventAsync(4, Arg.Any<CancellationToken>()).Returns(reservation);

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
        _repository.GetByIdWithEventAsync(99, Arg.Any<CancellationToken>()).Returns((Reservation?)null);

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
        var reservation = WithEventOptions(new Reservation { Id = 1, EventId = 1, SequenceNumber = 1, Status = ReservationStatus.Waiting, CallCount = 0 }, false);
        _repository.GetByIdWithEventAsync(1, Arg.Any<CancellationToken>()).Returns(reservation);
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new RepositoryConcurrencyException());

        // Act
        var result = await _handler.Handle(new CallReservation.Command(1, "Receptionist"), CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("modified by another user", result.Message);
        await _notifier.DidNotReceive().EnqueueStatusChangedAsync(
            Arg.Any<ReservationStatusChangedNotification>(),
            Arg.Any<CancellationToken>());
    }
    [Fact]
    public async Task Handle_PartyCompletionEnabled_WaitingReservation_ReturnsFailure()
    {
        // Arrange
        var reservation = WithEventOptions(new Reservation { Id = 1, Status = ReservationStatus.Waiting }, true);
        _repository.GetByIdWithEventAsync(1, Arg.Any<CancellationToken>()).Returns(reservation);

        // Act
        var result = await _handler.Handle(new CallReservation.Command(1), CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Mark party complete first", result.Message);
    }

    [Fact]
    public async Task Handle_PartyCompletionEnabled_PartyCompletedReservation_SetsStatusToCalled()
    {
        // Arrange
        var reservation = WithEventOptions(new Reservation { Id = 1, Status = ReservationStatus.PartyCompleted }, true);
        _repository.GetByIdWithEventAsync(1, Arg.Any<CancellationToken>()).Returns(reservation);

        // Act
        var result = await _handler.Handle(new CallReservation.Command(1), CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(ReservationStatus.Called, reservation.Status);
    }
}
