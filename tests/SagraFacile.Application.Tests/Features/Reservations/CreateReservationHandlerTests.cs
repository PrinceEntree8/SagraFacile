using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SagraFacile.Application.Exceptions;
using SagraFacile.Application.Features.Reservations;
using SagraFacile.Application.Interfaces;
using SagraFacile.Contracts.Reservations;
using SagraFacile.Domain.Features.Events;
using SagraFacile.Domain.Features.Reservations;

namespace SagraFacile.Application.Tests.Features.Reservations;

public class CreateReservationHandlerTests
{
    private readonly IReservationRepository _repository = Substitute.For<IReservationRepository>();
    private readonly IReservationNotifier _notifier = Substitute.For<IReservationNotifier>();
    private readonly IEventRepository _eventRepository = Substitute.For<IEventRepository>();
    private readonly CreateReservation.Handler _handler;

    public CreateReservationHandlerTests()
    {
        _eventRepository.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(ci => new Event { Id = ci.Arg<int>() });
        _handler = new CreateReservation.Handler(_repository, _notifier, _eventRepository);
    }

    [Fact]
    public async Task Handle_ValidCommand_SetsEventIdAndSequenceNumber()
    {
        // Arrange
        _repository.GetNextSequenceNumberAsync(1, Arg.Any<CancellationToken>()).Returns(1);

        Reservation? saved = null;
        _repository.When(r => r.AddAsync(Arg.Any<Reservation>(), Arg.Any<CancellationToken>()))
            .Do(ci => { saved = ci.Arg<Reservation>(); saved.Id = 1; });

        var command = new CreateReservation.Command(1, "Mario Rossi", 4);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.Data.SequenceNumber);
        Assert.Equal(1, result.Data.Id);
        Assert.Equal(1, saved!.EventId);
        Assert.Equal(1, saved.SequenceNumber);
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _notifier.Received(1).EnqueueStatusChangedAsync(
            Arg.Is<ReservationStatusChangedNotification>(x =>
                x.ReservationId == result.Data.Id &&
                x.SequenceNumber == result.Data.SequenceNumber &&
                x.CustomerName == command.CustomerName &&
                x.PartySize == command.PartySize &&
                x.NewStatus == ReservationStatus.Waiting &&
                x.OldStatus == null &&
                x.CallCount == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidCommand_SetsStatusWaiting()
    {
        // Arrange
        _repository.GetNextSequenceNumberAsync(1, Arg.Any<CancellationToken>()).Returns(1);

        Reservation? saved = null;
        _repository.When(r => r.AddAsync(Arg.Any<Reservation>(), Arg.Any<CancellationToken>()))
            .Do(ci => saved = ci.Arg<Reservation>());

        // Act
        await _handler.Handle(new CreateReservation.Command(1, "Test", 3), CancellationToken.None);

        // Assert
        Assert.NotNull(saved);
        Assert.Equal(ReservationStatus.Waiting, saved!.Status);
    }

    [Fact]
    public async Task Handle_ValidCommand_NullNotes_IsAccepted()
    {
        // Arrange
        _repository.GetNextSequenceNumberAsync(1, Arg.Any<CancellationToken>()).Returns(1);

        Reservation? saved = null;
        _repository.When(r => r.AddAsync(Arg.Any<Reservation>(), Arg.Any<CancellationToken>()))
            .Do(ci => saved = ci.Arg<Reservation>());

        // Act
        await _handler.Handle(new CreateReservation.Command(1, "Test", 3, null), CancellationToken.None);

        // Assert
        Assert.NotNull(saved);
        Assert.Null(saved!.Notes);
    }

    [Fact]
    public async Task Handle_UniqueViolationOnFirstAttempt_Retries()
    {
        // Arrange
        var callCount = 0;
        _repository.GetNextSequenceNumberAsync(1, Arg.Any<CancellationToken>()).Returns(1);

        _repository.When(r => r.AddAsync(Arg.Any<Reservation>(), Arg.Any<CancellationToken>()))
            .Do(ci => ci.Arg<Reservation>().Id = 1);

        // First call throws unique violation, second succeeds
        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                callCount++;
                if (callCount == 1)
                    throw new RepositoryUniqueConstraintException();
                return Task.CompletedTask;
            });

        var command = new CreateReservation.Command(1, "Mario", 4);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert — SaveChanges was called twice (one failure + one success)
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task Handle_UniqueViolationExceedsMaxRetries_Throws()
    {
        // Arrange
        _repository.GetNextSequenceNumberAsync(1, Arg.Any<CancellationToken>()).Returns(1);

        _repository.When(r => r.AddAsync(Arg.Any<Reservation>(), Arg.Any<CancellationToken>()))
            .Do(ci => ci.Arg<Reservation>().Id = 1);

        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new RepositoryUniqueConstraintException());

        var command = new CreateReservation.Command(1, "Mario", 4);

        // Act & Assert
        await Assert.ThrowsAsync<RepositoryUniqueConstraintException>(() => _handler.Handle(command, CancellationToken.None));
    }
    private static Event CreateEventWithOptions(int id, bool enabled, int minPartySize)
    {
        return new Event
        {
            Id = id,
            AdditionalOptions = new EventAdditionalOptions
            {
                Reservations = new ReservationOptions
                {
                    PartyCompletion = new PartyCompletionOptions
                    {
                        Enabled = enabled,
                        MinPartySize = minPartySize
                    }
                }
            }
        };
    }

    [Fact]
    public async Task Handle_PartyCompletionEnabled_PartyCompleteTrue_SetsStatusPartyCompleted()
    {
        // Arrange
        _eventRepository.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(CreateEventWithOptions(1, true, 4));
        _repository.GetNextSequenceNumberAsync(1, Arg.Any<CancellationToken>()).Returns(1);
        Reservation? saved = null;
        _repository.When(r => r.AddAsync(Arg.Any<Reservation>(), Arg.Any<CancellationToken>()))
            .Do(ci => saved = ci.Arg<Reservation>());

        // Act
        await _handler.Handle(new CreateReservation.Command(1, "Test", 4, PartyComplete: true), CancellationToken.None);

        // Assert
        Assert.Equal(ReservationStatus.PartyCompleted, saved!.Status);
    }

    [Fact]
    public async Task Handle_PartyCompletionEnabled_PartyCompleteFalseButSmallParty_SetsStatusPartyCompleted()
    {
        // Arrange
        _eventRepository.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(CreateEventWithOptions(1, true, 4));
        _repository.GetNextSequenceNumberAsync(1, Arg.Any<CancellationToken>()).Returns(1);
        Reservation? saved = null;
        _repository.When(r => r.AddAsync(Arg.Any<Reservation>(), Arg.Any<CancellationToken>()))
            .Do(ci => saved = ci.Arg<Reservation>());

        // Act
        await _handler.Handle(new CreateReservation.Command(1, "Test", 2, PartyComplete: false), CancellationToken.None);

        // Assert
        Assert.Equal(ReservationStatus.PartyCompleted, saved!.Status);
    }

    [Fact]
    public async Task Handle_PartyCompletionEnabled_PartyCompleteFalseLargeParty_SetsStatusWaiting()
    {
        // Arrange
        _eventRepository.GetByIdAsync(1, Arg.Any<CancellationToken>())
            .Returns(CreateEventWithOptions(1, true, 4));
        _repository.GetNextSequenceNumberAsync(1, Arg.Any<CancellationToken>()).Returns(1);
        Reservation? saved = null;
        _repository.When(r => r.AddAsync(Arg.Any<Reservation>(), Arg.Any<CancellationToken>()))
            .Do(ci => saved = ci.Arg<Reservation>());

        // Act
        await _handler.Handle(new CreateReservation.Command(1, "Test", 5, PartyComplete: false), CancellationToken.None);

        // Assert
        Assert.Equal(ReservationStatus.Waiting, saved!.Status);
    }
}
