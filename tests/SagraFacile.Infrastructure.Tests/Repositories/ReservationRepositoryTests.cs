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
        var r = new TableReservation { ReservationId = "1", CustomerName = "Mario", PartySize = 4, Status = "Waiting", CreatedAt = DateTime.UtcNow };

        // Act
        await repo.AddAsync(r, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        var found = await repo.GetByIdAsync(r.Id, CancellationToken.None);

        // Assert
        Assert.NotNull(found);
        Assert.Equal("Mario", found.CustomerName);
        Assert.Equal(r.CreatedAt.ToString("yyyyMMdd"), found.Date);
        Assert.Equal("1", found.QueueNumber);
        Assert.Equal(r.ReservationId, found.ReservationId);
    }

    [Fact]
    public async Task GetLastByDatePrefix_ReturnsHighestQueueNumberForDate()
    {
        // Arrange
        using var factory = new TestDbContextFactory();
        await using var repo = new ReservationRepository(factory);
        var today = DateTime.Today;

        await repo.AddAsync(new TableReservation { ReservationId = "1", CustomerName = "A", PartySize = 1, Status = "Waiting", CreatedAt = today }, CancellationToken.None);
        await repo.AddAsync(new TableReservation { ReservationId = "3", CustomerName = "B", PartySize = 2, Status = "Waiting", CreatedAt = today }, CancellationToken.None);
        await repo.AddAsync(new TableReservation { ReservationId = "2", CustomerName = "C", PartySize = 3, Status = "Waiting", CreatedAt = today }, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        // Act
        var last = await repo.GetLastByDatePrefixAsync(today, CancellationToken.None);

        // Assert
        Assert.NotNull(last);
        Assert.Equal(today.ToString("yyyyMMdd"), last!.Date);
        Assert.Equal("3", last.QueueNumber);
    }

    [Fact]
    public async Task GetPagedAsync_FiltersOutSeatedAndVoidedByDefault()
    {
        // Arrange
        using var factory = new TestDbContextFactory();
        await using var repo = new ReservationRepository(factory);

        await repo.AddAsync(new TableReservation { ReservationId = "1", CustomerName = "A", PartySize = 1, Status = "Waiting", CreatedAt = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc) }, CancellationToken.None);
        await repo.AddAsync(new TableReservation { ReservationId = "2", CustomerName = "B", PartySize = 2, Status = "Seated", CreatedAt = new DateTime(2026, 1, 1, 10, 5, 0, DateTimeKind.Utc) }, CancellationToken.None);
        await repo.AddAsync(new TableReservation { ReservationId = "3", CustomerName = "C", PartySize = 3, Status = "Voided", CreatedAt = new DateTime(2026, 1, 1, 10, 10, 0, DateTimeKind.Utc) }, CancellationToken.None);
        await repo.AddAsync(new TableReservation { ReservationId = "4", CustomerName = "D", PartySize = 2, Status = "Called", CreatedAt = new DateTime(2026, 1, 1, 10, 15, 0, DateTimeKind.Utc) }, CancellationToken.None);
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

        await repo.AddAsync(new TableReservation { ReservationId = "2", CustomerName = "B", PartySize = 2, Status = "Called", CreatedAt = now }, CancellationToken.None);
        await repo.AddAsync(new TableReservation { ReservationId = "1", CustomerName = "A", PartySize = 3, Status = "Called", CreatedAt = now.AddMinutes(-5) }, CancellationToken.None);
        await repo.AddAsync(new TableReservation { ReservationId = "3", CustomerName = "C", PartySize = 1, Status = "Waiting", CreatedAt = now.AddMinutes(-10) }, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        // Act
        var called = await repo.GetCalledReservationsOrderedByCreatedAtAsync(CancellationToken.None);

        // Assert
        Assert.Equal(2, called.Count);
        Assert.Equal("1", called[0].QueueNumber); // Older one first
        Assert.Equal("2", called[1].QueueNumber);
    }

    [Fact]
    public async Task AddCallAsync_AssociatesCallWithReservation()
    {
        // Arrange
        using var factory = new TestDbContextFactory();
        await using var repo = new ReservationRepository(factory);

        var reservation = new TableReservation { ReservationId = "1", CustomerName = "A", PartySize = 1, Status = "Waiting", CreatedAt = DateTime.UtcNow };
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

    [Fact]
    public async Task GetByDateRangeAsync_NoBounds_ReturnsAll()
    {
        // Arrange
        using var factory = new TestDbContextFactory();
        await using var repo = new ReservationRepository(factory);
        var now = DateTime.UtcNow;

        await repo.AddAsync(new TableReservation { ReservationId = "1", CustomerName = "A", PartySize = 1, Status = "Waiting", CreatedAt = now.AddDays(-5) }, CancellationToken.None);
        await repo.AddAsync(new TableReservation { ReservationId = "2", CustomerName = "B", PartySize = 2, Status = "Seated", CreatedAt = now.AddDays(-3) }, CancellationToken.None);
        await repo.AddAsync(new TableReservation { ReservationId = "3", CustomerName = "C", PartySize = 3, Status = "Voided", CreatedAt = now.AddDays(-1) }, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        // Act
        var items = await repo.GetByDateRangeAsync(null, null, CancellationToken.None);

        // Assert — no filter returns all
        Assert.Equal(3, items.Count);
    }

    [Fact]
    public async Task GetByDateRangeAsync_WithStartDate_ExcludesOlderReservations()
    {
        // Arrange
        using var factory = new TestDbContextFactory();
        await using var repo = new ReservationRepository(factory);
        var now = DateTime.UtcNow;
        var cutoff = now.AddDays(-2);

        await repo.AddAsync(new TableReservation { ReservationId = "1", CustomerName = "Old", PartySize = 1, Status = "Waiting", CreatedAt = now.AddDays(-5) }, CancellationToken.None);
        await repo.AddAsync(new TableReservation { ReservationId = "2", CustomerName = "New", PartySize = 2, Status = "Waiting", CreatedAt = now.AddDays(-1) }, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        // Act
        var items = await repo.GetByDateRangeAsync(cutoff, null, CancellationToken.None);

        // Assert
        Assert.Single(items);
        Assert.Equal("New", items[0].CustomerName);
    }

    [Fact]
    public async Task GetByDateRangeAsync_WithEndDate_ExcludesNewerReservations()
    {
        // Arrange
        using var factory = new TestDbContextFactory();
        await using var repo = new ReservationRepository(factory);
        var now = DateTime.UtcNow;
        var cutoff = now.AddDays(-2);

        await repo.AddAsync(new TableReservation { ReservationId = "1", CustomerName = "Old", PartySize = 1, Status = "Waiting", CreatedAt = now.AddDays(-5) }, CancellationToken.None);
        await repo.AddAsync(new TableReservation { ReservationId = "2", CustomerName = "New", PartySize = 2, Status = "Waiting", CreatedAt = now.AddDays(-1) }, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        // Act
        var items = await repo.GetByDateRangeAsync(null, cutoff, CancellationToken.None);

        // Assert
        Assert.Single(items);
        Assert.Equal("Old", items[0].CustomerName);
    }

    [Fact]
    public async Task GetByDateRangeAsync_WithBothBounds_ReturnsOnlyInRange()
    {
        // Arrange
        using var factory = new TestDbContextFactory();
        await using var repo = new ReservationRepository(factory);
        var now = DateTime.UtcNow;

        await repo.AddAsync(new TableReservation { ReservationId = "1", CustomerName = "TooOld", PartySize = 1, Status = "Waiting", CreatedAt = now.AddDays(-10) }, CancellationToken.None);
        await repo.AddAsync(new TableReservation { ReservationId = "2", CustomerName = "InRange", PartySize = 2, Status = "Seated", CreatedAt = now.AddDays(-3) }, CancellationToken.None);
        await repo.AddAsync(new TableReservation { ReservationId = "3", CustomerName = "TooNew", PartySize = 3, Status = "Voided", CreatedAt = now.AddDays(-1) }, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        // Act
        var items = await repo.GetByDateRangeAsync(now.AddDays(-5), now.AddDays(-2), CancellationToken.None);

        // Assert
        Assert.Single(items);
        Assert.Equal("InRange", items[0].CustomerName);
    }

    [Fact]
    public async Task GetByDateRangeAsync_ReturnsOrderedByCreatedAt()
    {
        // Arrange
        using var factory = new TestDbContextFactory();
        await using var repo = new ReservationRepository(factory);
        var now = DateTime.UtcNow;

        await repo.AddAsync(new TableReservation { ReservationId = "3", CustomerName = "Third", PartySize = 1, Status = "Waiting", CreatedAt = now.AddMinutes(-1) }, CancellationToken.None);
        await repo.AddAsync(new TableReservation { ReservationId = "1", CustomerName = "First", PartySize = 2, Status = "Waiting", CreatedAt = now.AddMinutes(-30) }, CancellationToken.None);
        await repo.AddAsync(new TableReservation { ReservationId = "2", CustomerName = "Second", PartySize = 3, Status = "Waiting", CreatedAt = now.AddMinutes(-15) }, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        // Act
        var items = await repo.GetByDateRangeAsync(null, null, CancellationToken.None);

        // Assert — ordered by CreatedAt ascending
        Assert.Equal(3, items.Count);
        Assert.Equal("First", items[0].CustomerName);
        Assert.Equal("Second", items[1].CustomerName);
        Assert.Equal("Third", items[2].CustomerName);
    }

    [Fact]
    public async Task GetPagedAsync_WithExplicitStatusFilter_ReturnsOnlyMatchingStatus()
    {
        // Arrange
        using var factory = new TestDbContextFactory();
        await using var repo = new ReservationRepository(factory);

        await repo.AddAsync(new TableReservation { ReservationId = "1", CustomerName = "A", PartySize = 1, Status = "Waiting", CreatedAt = DateTime.UtcNow }, CancellationToken.None);
        await repo.AddAsync(new TableReservation { ReservationId = "2", CustomerName = "B", PartySize = 2, Status = "Seated", CreatedAt = DateTime.UtcNow }, CancellationToken.None);
        await repo.AddAsync(new TableReservation { ReservationId = "3", CustomerName = "C", PartySize = 3, Status = "Called", CreatedAt = DateTime.UtcNow }, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        // Act — explicit status "Seated"
        var (items, total) = await repo.GetPagedAsync("Seated", 1, 50, CancellationToken.None);

        // Assert
        Assert.Equal(1, total);
        Assert.Single(items);
        Assert.Equal("Seated", items[0].Status);
    }
}
