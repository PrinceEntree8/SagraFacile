using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SagraFacile.Application.Exceptions;
using SagraFacile.Application.Features.Reservations;
using SagraFacile.Application.Interfaces;
using SagraFacile.Contracts.Reservations;
using SagraFacile.Domain.Features.Events;
using SagraFacile.Domain.Features.Reservations;

namespace SagraFacile.Application.Tests.Features.Reservations;

public class MarkPartyCompleteHandlerTests
{
    private readonly IReservationRepository _repository = Substitute.For<IReservationRepository>();
    private readonly IReservationNotifier _notifier = Substitute.For<IReservationNotifier>();
    private readonly MarkPartyComplete.Handler _handler;

    public MarkPartyCompleteHandlerTests()
    {
        _repository.GetCountersAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<ReservationCounterDto>());
        _handler = new MarkPartyComplete.Handler(_repository, _notifier);
    }

    private static Reservation CreateReservation(int id, ReservationStatus status, bool partyCompletionEnabled)
    {
        return new Reservation
        {
            Id = id,
            SequenceNumber = 10,
            CustomerName = "Test Customer",
            PartySize = 4,
            Status = status,
            EventId = 1,
            Event = new Event
            {
                Id = 1,
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
            }
        };
    }

    [Fact]
    public async Task Handle_ValidRequest_Success()
    {
        // Arrange
        var reservation = CreateReservation(1, ReservationStatus.Waiting, true);
        _repository.GetByIdWithEventAsync(1, Arg.Any<CancellationToken>()).Returns(reservation);

        // Act
        var result = await _handler.Handle(new MarkPartyComplete.Command(1), CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(ReservationStatus.PartyCompleted, reservation.Status);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _notifier.Received(1).EnqueueStatusChangedAsync(
            Arg.Is<ReservationStatusChangedNotification>(x =>
                x.ReservationId == reservation.Id &&
                x.SequenceNumber == reservation.SequenceNumber &&
                x.CustomerName == reservation.CustomerName &&
                x.PartySize == reservation.PartySize &&
                x.NewStatus == ReservationStatus.PartyCompleted &&
                x.OldStatus == ReservationStatus.Waiting &&
                x.CallCount == 0),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_FeatureDisabled_ReturnsFailure()
    {
        // Arrange
        var reservation = CreateReservation(1, ReservationStatus.Waiting, false);
        _repository.GetByIdWithEventAsync(1, Arg.Any<CancellationToken>()).Returns(reservation);

        // Act
        var result = await _handler.Handle(new MarkPartyComplete.Command(1), CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("not enabled", result.Message);
        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_StatusNotWaiting_ReturnsFailure()
    {
        // Arrange
        var reservation = CreateReservation(1, ReservationStatus.Called, true);
        _repository.GetByIdWithEventAsync(1, Arg.Any<CancellationToken>()).Returns(reservation);

        // Act
        var result = await _handler.Handle(new MarkPartyComplete.Command(1), CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("not in waiting status", result.Message);
    }

    [Fact]
    public async Task Handle_ReservationNotFound_ReturnsFailure()
    {
        // Arrange
        _repository.GetByIdWithEventAsync(1, Arg.Any<CancellationToken>()).Returns((Reservation?)null);

        // Act
        var result = await _handler.Handle(new MarkPartyComplete.Command(1), CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Reservation not found", result.Message);
    }

    [Fact]
    public async Task Handle_ConcurrencyException_ReturnsFailure()
    {
        // Arrange
        var reservation = CreateReservation(1, ReservationStatus.Waiting, true);
        _repository.GetByIdWithEventAsync(1, Arg.Any<CancellationToken>()).Returns(reservation);
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new RepositoryConcurrencyException());

        // Act
        var result = await _handler.Handle(new MarkPartyComplete.Command(1), CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("modified by another user", result.Message);
    }
}
