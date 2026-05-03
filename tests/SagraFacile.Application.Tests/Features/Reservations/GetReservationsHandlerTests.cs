using NSubstitute;
using SagraFacile.Application.Features.Reservations;
using SagraFacile.Application.Interfaces;
using SagraFacile.Domain.Features.Reservations;

namespace SagraFacile.Application.Tests.Features.Reservations;

public class GetReservationsHandlerTests
{
    private readonly IReservationRepository _repository = Substitute.For<IReservationRepository>();
    private readonly GetReservations.Handler _handler;

    public GetReservationsHandlerTests()
    {
        _handler = new GetReservations.Handler(_repository);
    }

    [Fact]
    public async Task Handle_ReturnsPagedResultsWithCorrectTotalCount()
    {
        var now = DateTime.UtcNow;
        var reservations = new List<TableReservation>
        {
            new() { Id = 1, QueueNumber = "0001", CustomerName = "Mario", PartySize = 4, Status = "Waiting", Notes = "", CreatedAt = now.AddMinutes(-10) },
            new() { Id = 2, QueueNumber = "0002", CustomerName = "Luigi", PartySize = 2, Status = "Called", Notes = "Birthday", CreatedAt = now.AddMinutes(-5) },
        };
        _repository.GetPagedAsync(null, 1, 50, Arg.Any<CancellationToken>())
            .Returns((reservations, 2));

        var result = await _handler.Handle(new GetReservations.Query(), CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Reservations.Count);
    }

    [Fact]
    public async Task Handle_ComputesWaitingTime()
    {
        var now = DateTime.UtcNow;
        var created = now.AddMinutes(-15);
        var reservations = new List<TableReservation>
        {
            new() { Id = 1, QueueNumber = "0001", CustomerName = "Test", PartySize = 2, Status = "Waiting", Notes = "", CreatedAt = created },
        };
        _repository.GetPagedAsync(null, 1, 50, Arg.Any<CancellationToken>())
            .Returns((reservations, 1));

        var result = await _handler.Handle(new GetReservations.Query(), CancellationToken.None);

        var dto = result.Reservations[0];
        // WaitingTime = now - CreatedAt; should be approximately 15 minutes
        Assert.True(dto.WaitingTime >= TimeSpan.FromMinutes(14));
        Assert.True(dto.WaitingTime <= TimeSpan.FromMinutes(16));
    }

    [Fact]
    public async Task Handle_WithLastCalledAt_ComputesTimeSinceLastCall()
    {
        var now = DateTime.UtcNow;
        var lastCalled = now.AddMinutes(-3);
        var reservations = new List<TableReservation>
        {
            new()
            {
                Id = 1, QueueNumber = "0001", CustomerName = "Test", PartySize = 2,
                Status = "Called", Notes = "", CreatedAt = now.AddMinutes(-10),
                LastCalledAt = lastCalled, CallCount = 1
            },
        };
        _repository.GetPagedAsync(null, 1, 50, Arg.Any<CancellationToken>())
            .Returns((reservations, 1));

        var result = await _handler.Handle(new GetReservations.Query(), CancellationToken.None);

        var dto = result.Reservations[0];
        Assert.NotNull(dto.TimeSinceLastCall);
        // TimeSinceLastCall should be approximately 3 minutes
        Assert.True(dto.TimeSinceLastCall!.Value >= TimeSpan.FromMinutes(2));
        Assert.True(dto.TimeSinceLastCall!.Value <= TimeSpan.FromMinutes(4));
    }

    [Fact]
    public async Task Handle_WithoutLastCalledAt_TimeSinceLastCallIsNull()
    {
        var reservations = new List<TableReservation>
        {
            new() { Id = 1, QueueNumber = "0001", CustomerName = "Test", PartySize = 2, Status = "Waiting", Notes = "", CreatedAt = DateTime.UtcNow },
        };
        _repository.GetPagedAsync(null, 1, 50, Arg.Any<CancellationToken>())
            .Returns((reservations, 1));

        var result = await _handler.Handle(new GetReservations.Query(), CancellationToken.None);

        Assert.Null(result.Reservations[0].TimeSinceLastCall);
    }

    [Fact]
    public async Task Handle_PassesStatusFilterToRepository()
    {
        _repository.GetPagedAsync("Called", 1, 50, Arg.Any<CancellationToken>())
            .Returns((new List<TableReservation>(), 0));

        await _handler.Handle(new GetReservations.Query(Status: "Called"), CancellationToken.None);

        await _repository.Received(1).GetPagedAsync("Called", 1, 50, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PassesPaginationToRepository()
    {
        _repository.GetPagedAsync(null, 2, 10, Arg.Any<CancellationToken>())
            .Returns((new List<TableReservation>(), 0));

        await _handler.Handle(new GetReservations.Query(Page: 2, PageSize: 10), CancellationToken.None);

        await _repository.Received(1).GetPagedAsync(null, 2, 10, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MapsAllDtoFields()
    {
        var now = DateTime.UtcNow;
        var firstCalled = now.AddMinutes(-8);
        var lastCalled = now.AddMinutes(-2);
        var reservations = new List<TableReservation>
        {
            new()
            {
                Id = 7, QueueNumber = "0007", CustomerName = "Franco", PartySize = 6,
                Status = "Called", Notes = "High chair needed", CreatedAt = now.AddMinutes(-10),
                FirstCalledAt = firstCalled, LastCalledAt = lastCalled, CallCount = 2
            },
        };
        _repository.GetPagedAsync(null, 1, 50, Arg.Any<CancellationToken>())
            .Returns((reservations, 1));

        var result = await _handler.Handle(new GetReservations.Query(), CancellationToken.None);

        var dto = result.Reservations[0];
        Assert.Equal(7, dto.Id);
        Assert.Equal("0007", dto.QueueNumber);
        Assert.Equal("Franco", dto.CustomerName);
        Assert.Equal(6, dto.PartySize);
        Assert.Equal("Called", dto.Status);
        Assert.Equal("High chair needed", dto.Notes);
        Assert.Equal(firstCalled, dto.FirstCalledAt);
        Assert.Equal(lastCalled, dto.LastCalledAt);
        Assert.Equal(2, dto.CallCount);
    }
}
