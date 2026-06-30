using NSubstitute;
using SagraFacile.Application.Features.Reservations;
using SagraFacile.Application.Interfaces;
using SagraFacile.Contracts.Reservations;
using SagraFacile.Domain.Features.Events;
using SagraFacile.Domain.Features.Reservations;

namespace SagraFacile.Application.Tests.Features.Reservations;

public class StatusTimestampsTests
{
    private readonly IReservationRepository _repository = Substitute.For<IReservationRepository>();
    private readonly IReservationNotifier _notifier = Substitute.For<IReservationNotifier>();
    private readonly IEventRepository _eventRepository = Substitute.For<IEventRepository>();

    public StatusTimestampsTests()
    {
        _repository.GetCountersAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<ReservationCounterDto>());
        _eventRepository.GetByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new Event { Id = 1 });
    }

    [Fact]
    public async Task CreateReservation_SetsCreatedAtAsUtc()
    {
        _repository.GetNextSequenceNumberAsync(1, Arg.Any<CancellationToken>()).Returns(1);
        Reservation? saved = null;

        _repository.When(r => r.AddAsync(Arg.Any<Reservation>(), Arg.Any<CancellationToken>()))
            .Do(ci => saved = ci.Arg<Reservation>());

        var handler = new CreateReservation.Handler(_repository, _notifier, _eventRepository);

        await handler.Handle(new CreateReservation.Command(1, "Mario", 4), CancellationToken.None);

        Assert.NotNull(saved);
        Assert.Equal(DateTimeKind.Utc, saved!.CreatedAt.Kind);
    }

    [Fact]
    public async Task CallReservation_SetsCallTimestampsAsUtc()
    {
        var reservation = new Reservation
        {
            Id = 1,
            EventId = 1,
            SequenceNumber = 1,
            Status = ReservationStatus.Waiting,
            Event = new Event { AdditionalOptions = new EventAdditionalOptions() }
        };

        _repository.GetByIdWithEventAsync(1, Arg.Any<CancellationToken>()).Returns(reservation);

        var handler = new CallReservation.Handler(_repository, _notifier);

        await handler.Handle(new CallReservation.Command(1), CancellationToken.None);

        Assert.Equal(DateTimeKind.Utc, reservation.FirstCalledAt!.Value.Kind);
        Assert.Equal(DateTimeKind.Utc, reservation.LastCalledAt!.Value.Kind);
    }

    [Fact]
    public async Task SeatReservation_SetsSeatedAtAsUtc()
    {
        var reservation = new Reservation
        {
            Id = 1,
            EventId = 1,
            SequenceNumber = 1,
            Status = ReservationStatus.Called
        };

        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(reservation);

        var handler = new SeatReservation.Handler(_repository, _notifier);

        await handler.Handle(new SeatReservation.Command(1), CancellationToken.None);

        Assert.Equal(DateTimeKind.Utc, reservation.SeatedAt!.Value.Kind);
    }

    [Fact]
    public async Task VoidReservation_SetsVoidedAtAsUtc()
    {
        var reservation = new Reservation
        {
            Id = 1,
            EventId = 1,
            SequenceNumber = 1,
            Status = ReservationStatus.Waiting
        };

        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(reservation);

        var handler = new VoidReservation.Handler(_repository, _notifier);

        await handler.Handle(new VoidReservation.Command(1), CancellationToken.None);

        Assert.Equal(DateTimeKind.Utc, reservation.VoidedAt!.Value.Kind);
    }
}