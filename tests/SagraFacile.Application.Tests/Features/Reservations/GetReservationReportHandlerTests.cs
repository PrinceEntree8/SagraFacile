using NSubstitute;
using SagraFacile.Application.Features.Reservations;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Events;
using SagraFacile.Domain.Features.Reservations;

namespace SagraFacile.Application.Tests.Features.Reservations;

public class GetReservationReportHandlerTests
{
    private readonly IReservationRepository _repository = Substitute.For<IReservationRepository>();
    private readonly IEventRepository _eventRepository = Substitute.For<IEventRepository>();
    private readonly GetReservationReport.Handler _handler;

    public GetReservationReportHandlerTests()
    {
        _handler = new GetReservationReport.Handler(_repository, _eventRepository);
    }

    [Fact]
    public async Task Handle_NoEventFilter_CallsRepositoryWithNulls()
    {
        _repository.GetByDateRangeAsync(null, null, null, Arg.Any<ReservationStatusFilter>(), Arg.Any<CancellationToken>())
            .Returns(new List<Reservation>());

        var result = await _handler.Handle(new GetReservationReport.Query(), CancellationToken.None);

        await _repository.Received(1).GetByDateRangeAsync(null, null, null, Arg.Any<ReservationStatusFilter>(), Arg.Any<CancellationToken>());
        Assert.Empty(result.Reports);
        Assert.Equal(0, result.Statistics.TotalPeople);
    }

    [Fact]
    public async Task Handle_WithEventFilter_UsesSelectedEventDayRange()
    {
        var eventDate = new DateTime(2026, 8, 20, 13, 30, 0, DateTimeKind.Unspecified);
        _eventRepository.GetByIdAsync(7, Arg.Any<CancellationToken>())
            .Returns(new Event { Id = 7, Date = eventDate, Name = "Sagra test" });

        _repository.GetByDateRangeAsync(Arg.Any<int?>(), Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<ReservationStatusFilter>(), Arg.Any<CancellationToken>())
            .Returns(new List<Reservation>());

        await _handler.Handle(new GetReservationReport.Query(7), CancellationToken.None);

        await _repository.Received(1).GetByDateRangeAsync(
            7,
            Arg.Is<DateTime?>(d => d == new DateTime(2026, 8, 20, 0, 0, 0, DateTimeKind.Utc)),
            Arg.Is<DateTime?>(d => d == new DateTime(2026, 8, 20, 23, 59, 59, 999, DateTimeKind.Utc).AddTicks(9999)), 
            Arg.Any<ReservationStatusFilter>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SeatedReservations_ComputesStatisticsCorrectly()
    {
        var now = DateTime.UtcNow;
        var reservations = new List<Reservation>
        {
            new()
            {
                Id = 1, EventId = 1, SequenceNumber = 1, CustomerName = "Mario", PartySize = 4,
                Status = ReservationStatus.Seated, CreatedAt = now.AddMinutes(-30),
                FirstCalledAt = now.AddMinutes(-20),
                SeatedAt = now.AddMinutes(-10),
                VoidedAt = null, CallCount = 1
            },
            new()
            {
                Id = 2, EventId = 1, SequenceNumber = 2, CustomerName = "Luigi", PartySize = 2,
                Status = ReservationStatus.Seated, CreatedAt = now.AddMinutes(-60),
                FirstCalledAt = now.AddMinutes(-50),
                SeatedAt = now.AddMinutes(-40),
                VoidedAt = null, CallCount = 2
            },
        };
        _repository.GetByDateRangeAsync(1, null, null, Arg.Any<ReservationStatusFilter>(), Arg.Any<CancellationToken>()).Returns(reservations);

        var result = await _handler.Handle(new GetReservationReport.Query(1), CancellationToken.None);

        Assert.Equal(2, result.Statistics.TotalReservations);
        Assert.Equal(6, result.Statistics.TotalPeople);
        Assert.Equal(2, result.Statistics.SeatedCount);
        Assert.Equal(0, result.Statistics.VoidedCount);
        Assert.Equal(0, result.Statistics.WaitingCount);
        Assert.NotNull(result.Statistics.MaxWaitTime);
        Assert.NotNull(result.Statistics.MinWaitTime);
        Assert.True(result.Statistics.AverageWaitTime >= result.Statistics.MinWaitTime!.Value);
        Assert.True(result.Statistics.AverageWaitTime <= result.Statistics.MaxWaitTime!.Value);
    }

    [Fact]
    public async Task Handle_VoidedReservation_CountedInVoidedStat()
    {
        var now = DateTime.UtcNow;
        var reservations = new List<Reservation>
        {
            new()
            {
                Id = 1, EventId = 1, SequenceNumber = 1, CustomerName = "Anna", PartySize = 2,
                Status = ReservationStatus.Voided, CreatedAt = now.AddMinutes(-10),
                VoidedAt = now.AddMinutes(-5), CallCount = 0
            },
        };
        _repository.GetByDateRangeAsync(1, null, null, Arg.Any<ReservationStatusFilter>(), Arg.Any<CancellationToken>()).Returns(reservations);

        var result = await _handler.Handle(new GetReservationReport.Query(1), CancellationToken.None);

        Assert.Equal(1, result.Statistics.VoidedCount);
        Assert.Equal(0, result.Statistics.SeatedCount);
    }

    [Fact]
    public async Task Handle_WaitingReservation_CountedInWaitingStat()
    {
        var now = DateTime.UtcNow;
        var reservations = new List<Reservation>
        {
            new()
            {
                Id = 1, EventId = 1, SequenceNumber = 1, CustomerName = "Carlo", PartySize = 3,
                Status = ReservationStatus.Waiting, CreatedAt = now.AddMinutes(-5), CallCount = 0
            },
        };
        _repository.GetByDateRangeAsync(1, null, null, Arg.Any<ReservationStatusFilter>(), Arg.Any<CancellationToken>()).Returns(reservations);

        var result = await _handler.Handle(new GetReservationReport.Query(1), CancellationToken.None);

        Assert.Equal(1, result.Statistics.WaitingCount);
        Assert.Equal(3, result.Statistics.TotalPeople);
    }

    [Fact]
    public async Task Handle_CalledReservation_CountedInWaitingStat()
    {
        var now = DateTime.UtcNow;
        var reservations = new List<Reservation>
        {
            new()
            {
                Id = 1, EventId = 1, SequenceNumber = 1, CustomerName = "Giulia", PartySize = 2,
                Status = ReservationStatus.Called, CreatedAt = now.AddMinutes(-15),
                FirstCalledAt = now.AddMinutes(-5), CallCount = 1
            },
        };
        _repository.GetByDateRangeAsync(1, null, null, Arg.Any<ReservationStatusFilter>(), Arg.Any<CancellationToken>()).Returns(reservations);

        var result = await _handler.Handle(new GetReservationReport.Query(1), CancellationToken.None);

        Assert.Equal(1, result.Statistics.WaitingCount);
    }

    [Fact]
    public async Task Handle_NoSeatedReservations_StatisticsAreZeroOrNull()
    {
        var reservations = new List<Reservation>
        {
            new()
            {
                Id = 1, EventId = 1, SequenceNumber = 1, CustomerName = "A", PartySize = 1,
                Status = ReservationStatus.Waiting, CreatedAt = DateTime.UtcNow, CallCount = 0
            },
        };
        _repository.GetByDateRangeAsync(1, null, null, Arg.Any<ReservationStatusFilter>(), Arg.Any<CancellationToken>()).Returns(reservations);

        var result = await _handler.Handle(new GetReservationReport.Query(1), CancellationToken.None);

        Assert.Equal(TimeSpan.Zero, result.Statistics.AverageWaitTime);
        Assert.Equal(TimeSpan.Zero, result.Statistics.MedianWaitTime);
        Assert.Null(result.Statistics.MaxWaitTime);
        Assert.Null(result.Statistics.MinWaitTime);
    }

    [Fact]
    public async Task Handle_ReservationWithFirstCalledAt_ComputesWaitUntilFirstCall()
    {
        var now = DateTime.UtcNow;
        var created = now.AddMinutes(-30);
        var firstCalled = now.AddMinutes(-20);
        var reservations = new List<Reservation>
        {
            new()
            {
                Id = 1, EventId = 1, SequenceNumber = 1, CustomerName = "Test", PartySize = 2,
                Status = ReservationStatus.Called, CreatedAt = created, FirstCalledAt = firstCalled, CallCount = 1
            },
        };
        _repository.GetByDateRangeAsync(1, null, null, Arg.Any<ReservationStatusFilter>(), Arg.Any<CancellationToken>()).Returns(reservations);

        var result = await _handler.Handle(new GetReservationReport.Query(1), CancellationToken.None);

        Assert.Single(result.Reports);
        var report = result.Reports[0];
        Assert.NotNull(report.WaitTimeUntilFirstCall);
        Assert.Equal(firstCalled - created, report.WaitTimeUntilFirstCall!.Value);
        Assert.Equal(DateTimeKind.Utc, report.CreatedAt.Kind);
        Assert.Equal(DateTimeKind.Utc, report.FirstCalledAt!.Value.Kind);
    }

    [Fact]
    public async Task Handle_OddNumberOfSeatedReservations_ComputesMedianCorrectly()
    {
        // 3 items → median is the middle one
        var now = DateTime.UtcNow;
        var reservations = new List<Reservation>
        {
            new() { Id = 1, EventId = 1, SequenceNumber = 1, CustomerName = "A", PartySize = 1, Status = ReservationStatus.Seated, CreatedAt = now.AddMinutes(-10), SeatedAt = now.AddMinutes(-8), CallCount = 0 },
            new() { Id = 2, EventId = 1, SequenceNumber = 2, CustomerName = "B", PartySize = 2, Status = ReservationStatus.Seated, CreatedAt = now.AddMinutes(-20), SeatedAt = now.AddMinutes(-10), CallCount = 0 },
            new() { Id = 3, EventId = 1, SequenceNumber = 3, CustomerName = "C", PartySize = 3, Status = ReservationStatus.Seated, CreatedAt = now.AddMinutes(-30), SeatedAt = now.AddMinutes(-15), CallCount = 0 },
        };
        _repository.GetByDateRangeAsync(1, null, null, Arg.Any<ReservationStatusFilter>(), Arg.Any<CancellationToken>()).Returns(reservations);

        var result = await _handler.Handle(new GetReservationReport.Query(1), CancellationToken.None);

        // wait times: 2min, 10min, 15min → median = 10min
        Assert.Equal(TimeSpan.FromMinutes(10), result.Statistics.MedianWaitTime);
        Assert.All(result.Reports, report => Assert.Equal(DateTimeKind.Utc, report.CreatedAt.Kind));
    }

    [Fact]
    public async Task Handle_EvenNumberOfSeatedReservations_ComputesMedianAsAvgOfTwoMiddle()
    {
        // 2 items → median is average of the two
        var now = DateTime.UtcNow;
        var reservations = new List<Reservation>
        {
            new() { Id = 1, EventId = 1, SequenceNumber = 1, CustomerName = "A", PartySize = 1, Status = ReservationStatus.Seated, CreatedAt = now.AddMinutes(-10), SeatedAt = now.AddMinutes(-6), CallCount = 0 },
            new() { Id = 2, EventId = 1, SequenceNumber = 2, CustomerName = "B", PartySize = 2, Status = ReservationStatus.Seated, CreatedAt = now.AddMinutes(-20), SeatedAt = now, CallCount = 0 },
        };
        _repository.GetByDateRangeAsync(1, null, null, Arg.Any<ReservationStatusFilter>(), Arg.Any<CancellationToken>()).Returns(reservations);

        var result = await _handler.Handle(new GetReservationReport.Query(1), CancellationToken.None);

        // wait times: 4min, 20min → median = 12min
        Assert.Equal(TimeSpan.FromMinutes(12), result.Statistics.MedianWaitTime);
        Assert.All(result.Reports, report => Assert.Equal(DateTimeKind.Utc, report.CreatedAt.Kind));
    }
}
