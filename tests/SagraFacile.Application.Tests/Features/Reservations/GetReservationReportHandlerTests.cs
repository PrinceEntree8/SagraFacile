using NSubstitute;
using SagraFacile.Application.Features.Reservations;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Reservations;

namespace SagraFacile.Application.Tests.Features.Reservations;

public class GetReservationReportHandlerTests
{
    private readonly IReservationRepository _repository = Substitute.For<IReservationRepository>();
    private readonly GetReservationReport.Handler _handler;

    public GetReservationReportHandlerTests()
    {
        _handler = new GetReservationReport.Handler(_repository);
    }

    [Fact]
    public async Task Handle_NoDateFilter_CallsRepositoryWithNulls()
    {
        _repository.GetByDateRangeAsync(null, null, Arg.Any<CancellationToken>())
            .Returns(new List<TableReservation>());

        var result = await _handler.Handle(new GetReservationReport.Query(), CancellationToken.None);

        await _repository.Received(1).GetByDateRangeAsync(null, null, Arg.Any<CancellationToken>());
        Assert.Empty(result.Reports);
    }

    [Fact]
    public async Task Handle_WithDateFilter_NormalisesToUtc()
    {
        var start = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var end = new DateTime(2026, 8, 31, 23, 59, 59, DateTimeKind.Unspecified);

        _repository.GetByDateRangeAsync(Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(new List<TableReservation>());

        await _handler.Handle(new GetReservationReport.Query(start, end), CancellationToken.None);

        await _repository.Received(1).GetByDateRangeAsync(
            Arg.Is<DateTime?>(d => d.HasValue && d.Value.Kind == DateTimeKind.Utc),
            Arg.Is<DateTime?>(d => d.HasValue && d.Value.Kind == DateTimeKind.Utc),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SeatedReservations_ComputesStatisticsCorrectly()
    {
        var now = DateTime.UtcNow;
        var reservations = new List<TableReservation>
        {
            new()
            {
                Id = 1, QueueNumber = "0001", CustomerName = "Mario", PartySize = 4,
                Status = "Seated", CreatedAt = now.AddMinutes(-30),
                FirstCalledAt = now.AddMinutes(-20),
                SeatedAt = now.AddMinutes(-10),
                VoidedAt = null, CallCount = 1
            },
            new()
            {
                Id = 2, QueueNumber = "0002", CustomerName = "Luigi", PartySize = 2,
                Status = "Seated", CreatedAt = now.AddMinutes(-60),
                FirstCalledAt = now.AddMinutes(-50),
                SeatedAt = now.AddMinutes(-40),
                VoidedAt = null, CallCount = 2
            },
        };
        _repository.GetByDateRangeAsync(null, null, Arg.Any<CancellationToken>()).Returns(reservations);

        var result = await _handler.Handle(new GetReservationReport.Query(), CancellationToken.None);

        Assert.Equal(2, result.Statistics.TotalReservations);
        Assert.Equal(2, result.Statistics.SeatedCount);
        Assert.Equal(0, result.Statistics.VoidedCount);
        Assert.Equal(0, result.Statistics.WaitingCount);
        Assert.NotNull(result.Statistics.MaxWaitTime);
        Assert.NotNull(result.Statistics.MinWaitTime);
        // average wait should be between min and max
        Assert.True(result.Statistics.AverageWaitTime >= result.Statistics.MinWaitTime!.Value);
        Assert.True(result.Statistics.AverageWaitTime <= result.Statistics.MaxWaitTime!.Value);
    }

    [Fact]
    public async Task Handle_VoidedReservation_CountedInVoidedStat()
    {
        var now = DateTime.UtcNow;
        var reservations = new List<TableReservation>
        {
            new()
            {
                Id = 1, QueueNumber = "0001", CustomerName = "Anna", PartySize = 2,
                Status = "Voided", CreatedAt = now.AddMinutes(-10),
                VoidedAt = now.AddMinutes(-5), CallCount = 0
            },
        };
        _repository.GetByDateRangeAsync(null, null, Arg.Any<CancellationToken>()).Returns(reservations);

        var result = await _handler.Handle(new GetReservationReport.Query(), CancellationToken.None);

        Assert.Equal(1, result.Statistics.VoidedCount);
        Assert.Equal(0, result.Statistics.SeatedCount);
    }

    [Fact]
    public async Task Handle_WaitingReservation_CountedInWaitingStat()
    {
        var now = DateTime.UtcNow;
        var reservations = new List<TableReservation>
        {
            new()
            {
                Id = 1, QueueNumber = "0001", CustomerName = "Carlo", PartySize = 3,
                Status = "Waiting", CreatedAt = now.AddMinutes(-5), CallCount = 0
            },
        };
        _repository.GetByDateRangeAsync(null, null, Arg.Any<CancellationToken>()).Returns(reservations);

        var result = await _handler.Handle(new GetReservationReport.Query(), CancellationToken.None);

        Assert.Equal(1, result.Statistics.WaitingCount);
    }

    [Fact]
    public async Task Handle_CalledReservation_CountedInWaitingStat()
    {
        var now = DateTime.UtcNow;
        var reservations = new List<TableReservation>
        {
            new()
            {
                Id = 1, QueueNumber = "0001", CustomerName = "Giulia", PartySize = 2,
                Status = "Called", CreatedAt = now.AddMinutes(-15),
                FirstCalledAt = now.AddMinutes(-5), CallCount = 1
            },
        };
        _repository.GetByDateRangeAsync(null, null, Arg.Any<CancellationToken>()).Returns(reservations);

        var result = await _handler.Handle(new GetReservationReport.Query(), CancellationToken.None);

        Assert.Equal(1, result.Statistics.WaitingCount);
    }

    [Fact]
    public async Task Handle_NoSeatedReservations_StatisticsAreZeroOrNull()
    {
        var reservations = new List<TableReservation>
        {
            new()
            {
                Id = 1, QueueNumber = "0001", CustomerName = "A", PartySize = 1,
                Status = "Waiting", CreatedAt = DateTime.UtcNow, CallCount = 0
            },
        };
        _repository.GetByDateRangeAsync(null, null, Arg.Any<CancellationToken>()).Returns(reservations);

        var result = await _handler.Handle(new GetReservationReport.Query(), CancellationToken.None);

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
        var reservations = new List<TableReservation>
        {
            new()
            {
                Id = 1, QueueNumber = "0001", CustomerName = "Test", PartySize = 2,
                Status = "Called", CreatedAt = created, FirstCalledAt = firstCalled, CallCount = 1
            },
        };
        _repository.GetByDateRangeAsync(null, null, Arg.Any<CancellationToken>()).Returns(reservations);

        var result = await _handler.Handle(new GetReservationReport.Query(), CancellationToken.None);

        Assert.Single(result.Reports);
        var report = result.Reports[0];
        Assert.NotNull(report.WaitTimeUntilFirstCall);
        Assert.Equal(firstCalled - created, report.WaitTimeUntilFirstCall!.Value);
    }

    [Fact]
    public async Task Handle_OddNumberOfSeatedReservations_ComputesMedianCorrectly()
    {
        // 3 items → median is the middle one
        var now = DateTime.UtcNow;
        var reservations = new List<TableReservation>
        {
            new() { Id = 1, QueueNumber = "0001", CustomerName = "A", PartySize = 1, Status = "Seated", CreatedAt = now.AddMinutes(-10), SeatedAt = now.AddMinutes(-8), CallCount = 0 },
            new() { Id = 2, QueueNumber = "0002", CustomerName = "B", PartySize = 2, Status = "Seated", CreatedAt = now.AddMinutes(-20), SeatedAt = now.AddMinutes(-10), CallCount = 0 },
            new() { Id = 3, QueueNumber = "0003", CustomerName = "C", PartySize = 3, Status = "Seated", CreatedAt = now.AddMinutes(-30), SeatedAt = now.AddMinutes(-15), CallCount = 0 },
        };
        _repository.GetByDateRangeAsync(null, null, Arg.Any<CancellationToken>()).Returns(reservations);

        var result = await _handler.Handle(new GetReservationReport.Query(), CancellationToken.None);

        // wait times: 2min, 10min, 15min → median = 10min
        Assert.Equal(TimeSpan.FromMinutes(10), result.Statistics.MedianWaitTime);
    }

    [Fact]
    public async Task Handle_EvenNumberOfSeatedReservations_ComputesMedianAsAvgOfTwoMiddle()
    {
        // 2 items → median is average of the two
        var now = DateTime.UtcNow;
        var reservations = new List<TableReservation>
        {
            new() { Id = 1, QueueNumber = "0001", CustomerName = "A", PartySize = 1, Status = "Seated", CreatedAt = now.AddMinutes(-10), SeatedAt = now.AddMinutes(-6), CallCount = 0 },
            new() { Id = 2, QueueNumber = "0002", CustomerName = "B", PartySize = 2, Status = "Seated", CreatedAt = now.AddMinutes(-20), SeatedAt = now, CallCount = 0 },
        };
        _repository.GetByDateRangeAsync(null, null, Arg.Any<CancellationToken>()).Returns(reservations);

        var result = await _handler.Handle(new GetReservationReport.Query(), CancellationToken.None);

        // wait times: 4min, 20min → median = 12min
        Assert.Equal(TimeSpan.FromMinutes(12), result.Statistics.MedianWaitTime);
    }
}
