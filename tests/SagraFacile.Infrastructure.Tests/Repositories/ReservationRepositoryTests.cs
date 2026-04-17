using SagraFacile.Domain.Features.Reservations;
using SagraFacile.Infrastructure.Repositories;
using SagraFacile.Infrastructure.Tests;

namespace SagraFacile.Infrastructure.Tests.Repositories;

public class ReservationRepositoryTests
{
    [Fact]
    public async Task AddAsync_ThenGetById_ReturnsReservation()
    {
        // Arrange
        using var factory = new TestDbContextFactory();
        await using var repo = new ReservationRepository(factory);
        var r = new TableReservation { Date = "20260101", QueueNumber = "0001", CustomerName = "Mario", PartySize = 4, Status = "Waiting", CreatedAt = DateTime.UtcNow };

        // Act
        await repo.AddAsync(r, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        var found = await repo.GetByIdAsync(r.Id, CancellationToken.None);

        // Assert
        Assert.NotNull(found);
        Assert.Equal("Mario", found!.CustomerName);
        Assert.Equal("20260101", found.Date);
        Assert.Equal("0001", found.QueueNumber);
        Assert.Equal("202601010001", found.ReservationId);
    }

    [Fact]
    public async Task GetLastByDatePrefix_ReturnsHighestQueueNumberForDate()
    {
        // Arrange
        using var factory = new TestDbContextFactory();
        await using var repo = new ReservationRepository(factory);
        var today = "20260815";

        await repo.AddAsync(new TableReservation { Date = today, QueueNumber = "0001", CustomerName = "A", PartySize = 1, Status = "Waiting", CreatedAt = DateTime.UtcNow }, CancellationToken.None);
        await repo.AddAsync(new TableReservation { Date = today, QueueNumber = "0003", CustomerName = "B", PartySize = 2, Status = "Waiting", CreatedAt = DateTime.UtcNow }, CancellationToken.None);
        await repo.AddAsync(new TableReservation { Date = today, QueueNumber = "0002", CustomerName = "C", PartySize = 3, Status = "Waiting", CreatedAt = DateTime.UtcNow }, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        // Act
        var last = await repo.GetLastByDatePrefixAsync(today, CancellationToken.None);

        // Assert
        Assert.NotNull(last);
        Assert.Equal(today, last!.Date);
        Assert.Equal("0003", last.QueueNumber);
    }

    [Fact]
    public async Task GetPagedAsync_FiltersOutSeatedAndVoidedByDefault()
    {
        // Arrange
        using var factory = new TestDbContextFactory();
        await using var repo = new ReservationRepository(factory);

        await repo.AddAsync(new TableReservation { Date = "20260101", QueueNumber = "0001", CustomerName = "A", PartySize = 1, Status = "Waiting", CreatedAt = DateTime.UtcNow }, CancellationToken.None);
        await repo.AddAsync(new TableReservation { Date = "20260101", QueueNumber = "0002", CustomerName = "B", PartySize = 2, Status = "Seated", CreatedAt = DateTime.UtcNow }, CancellationToken.None);
        await repo.AddAsync(new TableReservation { Date = "20260101", QueueNumber = "0003", CustomerName = "C", PartySize = 3, Status = "Voided", CreatedAt = DateTime.UtcNow }, CancellationToken.None);
        await repo.AddAsync(new TableReservation { Date = "20260101", QueueNumber = "0004", CustomerName = "D", PartySize = 2, Status = "Called", CreatedAt = DateTime.UtcNow }, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        // Act — null status = default (exclude Seated + Voided)
        var (items, total) = await repo.GetPagedAsync(null, 1, 50, CancellationToken.None);

        // Assert
        Assert.Equal(2, total);
        Assert.All(items, r => Assert.NotEqual("Seated", r.Status));
        Assert.All(items, r => Assert.NotEqual("Voided", r.Status));
    }

    [Fact]
    public async Task GetCalledReservationsOrderedByCreatedAt_ReturnsOnlyCalledInOrder()
    {
        // Arrange
        using var factory = new TestDbContextFactory();
        await using var repo = new ReservationRepository(factory);
        var now = DateTime.UtcNow;

        await repo.AddAsync(new TableReservation { Date = "20260101", QueueNumber = "0002", CustomerName = "B", PartySize = 2, Status = "Called", CreatedAt = now }, CancellationToken.None);
        await repo.AddAsync(new TableReservation { Date = "20260101", QueueNumber = "0001", CustomerName = "A", PartySize = 3, Status = "Called", CreatedAt = now.AddMinutes(-5) }, CancellationToken.None);
        await repo.AddAsync(new TableReservation { Date = "20260101", QueueNumber = "0003", CustomerName = "C", PartySize = 1, Status = "Waiting", CreatedAt = now.AddMinutes(-10) }, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        // Act
        var called = await repo.GetCalledReservationsOrderedByCreatedAtAsync(CancellationToken.None);

        // Assert
        Assert.Equal(2, called.Count);
        Assert.Equal("0001", called[0].QueueNumber); // Older one first
        Assert.Equal("0002", called[1].QueueNumber);
    }

    [Fact]
    public async Task AddCallAsync_AssociatesCallWithReservation()
    {
        // Arrange
        using var factory = new TestDbContextFactory();
        await using var repo = new ReservationRepository(factory);

        var reservation = new TableReservation { Date = "20260101", QueueNumber = "0001", CustomerName = "A", PartySize = 1, Status = "Waiting", CreatedAt = DateTime.UtcNow };
        await repo.AddAsync(reservation, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        var call = new ReservationCall { TableReservationId = reservation.Id, CalledAt = DateTime.UtcNow, CalledBy = "Test" };

        // Act
        await repo.AddCallAsync(call, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        // Assert
        Assert.True(call.Id > 0);
        Assert.Equal(reservation.Id, call.TableReservationId);
    }
}
