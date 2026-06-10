using SagraFacile.Domain.Features.Reservations;
using SagraFacile.Infrastructure.Repositories;
using SagraFacile.Infrastructure.Tests;

namespace SagraFacile.Infrastructure.Tests.Repositories;

public class ReservationRepositoryTests
{
    private const int EventId1 = 1;
    private const int EventId2 = 2;

    [Fact]
    public async Task AddAsync_ThenGetById_ReturnsReservation()
    {
        // Arrange
        using var factory = new TestDbContextFactory();
        await using var repo = new ReservationRepository(factory);
        var r = new Reservation { EventId = EventId1, SequenceNumber = 1, CustomerName = "Mario", PartySize = 4, Status = ReservationStatus.Waiting, CreatedAt = DateTime.UtcNow };

        // Act
        await repo.AddAsync(r, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        var found = await repo.GetByIdAsync(r.Id, CancellationToken.None);

        // Assert
        Assert.NotNull(found);
        Assert.Equal("Mario", found.CustomerName);
        Assert.Equal(1, found.SequenceNumber);
        Assert.Equal(EventId1, found.EventId);
    }

    [Fact]
    public async Task GetNextSequenceNumberAsync_NoReservations_Returns1()
    {
        using var factory = new TestDbContextFactory();
        await using var repo = new ReservationRepository(factory);

        var next = await repo.GetNextSequenceNumberAsync(EventId1, CancellationToken.None);

        Assert.Equal(1, next);
    }

    [Fact]
    public async Task GetNextSequenceNumberAsync_ExistingReservations_ReturnsMaxPlusOne()
    {
        using var factory = new TestDbContextFactory();
        await using var repo = new ReservationRepository(factory);

        await repo.AddAsync(new Reservation { EventId = EventId1, SequenceNumber = 1, CustomerName = "A", PartySize = 1, Status = ReservationStatus.Waiting, CreatedAt = DateTime.UtcNow }, CancellationToken.None);
        await repo.AddAsync(new Reservation { EventId = EventId1, SequenceNumber = 3, CustomerName = "B", PartySize = 2, Status = ReservationStatus.Waiting, CreatedAt = DateTime.UtcNow }, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        var next = await repo.GetNextSequenceNumberAsync(EventId1, CancellationToken.None);

        Assert.Equal(4, next);
    }

    [Fact]
    public async Task GetNextSequenceNumberAsync_MultipleEvents_ReturnsPerEventScope()
    {
        using var factory = new TestDbContextFactory();
        await using var repo = new ReservationRepository(factory);

        await repo.AddAsync(new Reservation { EventId = EventId1, SequenceNumber = 5, CustomerName = "A", PartySize = 1, Status = ReservationStatus.Waiting, CreatedAt = DateTime.UtcNow }, CancellationToken.None);
        await repo.AddAsync(new Reservation { EventId = EventId2, SequenceNumber = 2, CustomerName = "B", PartySize = 2, Status = ReservationStatus.Waiting, CreatedAt = DateTime.UtcNow }, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        var nextEvent1 = await repo.GetNextSequenceNumberAsync(EventId1, CancellationToken.None);
        var nextEvent2 = await repo.GetNextSequenceNumberAsync(EventId2, CancellationToken.None);

        Assert.Equal(6, nextEvent1);
        Assert.Equal(3, nextEvent2);
    }

    [Fact]
    public async Task GetPagedByEventAsync_FiltersByEventId()
    {
        using var factory = new TestDbContextFactory();
        await using var repo = new ReservationRepository(factory);

        await repo.AddAsync(new Reservation { EventId = EventId1, SequenceNumber = 1, CustomerName = "A", PartySize = 1, Status = ReservationStatus.Waiting, CreatedAt = DateTime.UtcNow }, CancellationToken.None);
        await repo.AddAsync(new Reservation { EventId = EventId2, SequenceNumber = 1, CustomerName = "B", PartySize = 2, Status = ReservationStatus.Waiting, CreatedAt = DateTime.UtcNow }, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        var (items, total) = await repo.GetPagedAsync(EventId1, null, 1, 50, ReservationStatusFilter.All, CancellationToken.None);

        Assert.Equal(1, total);
        Assert.All(items, r => Assert.Equal(EventId1, r.EventId));
    }

    [Fact]
    public async Task GetPagedAsync_FiltersOutSeatedAndVoidedByDefault()
    {
        // Arrange
        using var factory = new TestDbContextFactory();
        await using var repo = new ReservationRepository(factory);

        await repo.AddAsync(new Reservation { EventId = EventId1, SequenceNumber = 1, CustomerName = "A", PartySize = 1, Status = ReservationStatus.Waiting, CreatedAt = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc) }, CancellationToken.None);
        await repo.AddAsync(new Reservation { EventId = EventId1, SequenceNumber = 2, CustomerName = "B", PartySize = 2, Status = ReservationStatus.Seated, CreatedAt = new DateTime(2026, 1, 1, 10, 5, 0, DateTimeKind.Utc) }, CancellationToken.None);
        await repo.AddAsync(new Reservation { EventId = EventId1, SequenceNumber = 3, CustomerName = "C", PartySize = 3, Status = ReservationStatus.Voided, CreatedAt = new DateTime(2026, 1, 1, 10, 10, 0, DateTimeKind.Utc) }, CancellationToken.None);
        await repo.AddAsync(new Reservation { EventId = EventId1, SequenceNumber = 4, CustomerName = "D", PartySize = 2, Status = ReservationStatus.Called, CreatedAt = new DateTime(2026, 1, 1, 10, 15, 0, DateTimeKind.Utc) }, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        // Act — null status = default (exclude Seated + Voided)
        var (items, total) = await repo.GetPagedAsync(EventId1, null, 1, 50, ReservationStatusFilter.All, CancellationToken.None);

        // Assert
        Assert.Equal(2, total);
        Assert.All(items, r => Assert.NotEqual(ReservationStatus.Seated, r.Status));
        Assert.All(items, r => Assert.NotEqual(ReservationStatus.Voided, r.Status));
    }

    [Fact]
    public async Task GetCountersByEventAsync_GroupsByStatusString()
    {
        using var factory = new TestDbContextFactory();
        await using var repo = new ReservationRepository(factory);

        await repo.AddAsync(new Reservation { EventId = EventId1, SequenceNumber = 1, CustomerName = "A", PartySize = 2, Status = ReservationStatus.Waiting, CreatedAt = DateTime.UtcNow }, CancellationToken.None);
        await repo.AddAsync(new Reservation { EventId = EventId1, SequenceNumber = 2, CustomerName = "B", PartySize = 3, Status = ReservationStatus.Waiting, CreatedAt = DateTime.UtcNow }, CancellationToken.None);
        await repo.AddAsync(new Reservation { EventId = EventId1, SequenceNumber = 3, CustomerName = "C", PartySize = 4, Status = ReservationStatus.Called, CreatedAt = DateTime.UtcNow }, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        var counters = await repo.GetCountersAsync(EventId1, CancellationToken.None);

        var waiting = counters.FirstOrDefault(c => c.Status == "Waiting");
        var called = counters.FirstOrDefault(c => c.Status == "Called");
        Assert.NotNull(waiting);
        Assert.Equal(2, waiting!.Count);
        Assert.Equal(5, waiting.TotalPeople);
        Assert.NotNull(called);
        Assert.Equal(1, called!.Count);
    }

    [Fact]
    public async Task GetCountersByEventAsync_FiltersByEventId_NotOtherEvents()
    {
        using var factory = new TestDbContextFactory();
        await using var repo = new ReservationRepository(factory);

        await repo.AddAsync(new Reservation { EventId = EventId1, SequenceNumber = 1, CustomerName = "A", PartySize = 2, Status = ReservationStatus.Waiting, CreatedAt = DateTime.UtcNow }, CancellationToken.None);
        await repo.AddAsync(new Reservation { EventId = EventId2, SequenceNumber = 1, CustomerName = "B", PartySize = 3, Status = ReservationStatus.Waiting, CreatedAt = DateTime.UtcNow }, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        var counters = await repo.GetCountersAsync(EventId1, CancellationToken.None);

        Assert.Single(counters);
        Assert.Equal(1, counters[0].Count);
        Assert.Equal(2, counters[0].TotalPeople);
    }

    [Fact]
    public async Task GetCalledReservationsOrderedByCreatedAt_ReturnsOnlyCalledInOrder()
    {
        // Arrange
        using var factory = new TestDbContextFactory();
        await using var repo = new ReservationRepository(factory);
        var now = DateTime.UtcNow;

        await repo.AddAsync(new Reservation { EventId = EventId1, SequenceNumber = 2, CustomerName = "B", PartySize = 2, Status = ReservationStatus.Called, CreatedAt = now }, CancellationToken.None);
        await repo.AddAsync(new Reservation { EventId = EventId1, SequenceNumber = 1, CustomerName = "A", PartySize = 3, Status = ReservationStatus.Called, CreatedAt = now.AddMinutes(-5) }, CancellationToken.None);
        await repo.AddAsync(new Reservation { EventId = EventId1, SequenceNumber = 3, CustomerName = "C", PartySize = 1, Status = ReservationStatus.Waiting, CreatedAt = now.AddMinutes(-10) }, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        // Act
        var called = await repo.GetCalledReservationsOrderedByCreatedAtAsync(EventId1, CancellationToken.None);

        // Assert
        Assert.Equal(2, called.Count);
        Assert.Equal(1, called[0].SequenceNumber); // Older one first
        Assert.Equal(2, called[1].SequenceNumber);
    }

    [Fact]
    public async Task AddCallAsync_AssociatesCallWithReservation()
    {
        // Arrange
        using var factory = new TestDbContextFactory();
        await using var repo = new ReservationRepository(factory);

        var reservation = new Reservation { EventId = EventId1, SequenceNumber = 1, CustomerName = "A", PartySize = 1, Status = ReservationStatus.Waiting, CreatedAt = DateTime.UtcNow };
        await repo.AddAsync(reservation, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        var call = new ReservationCall { ReservationId = reservation.Id, CalledAt = DateTime.UtcNow, CalledBy = "Test" };

        // Act
        await repo.AddCallAsync(call, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        // Assert
        Assert.True(call.Id > 0);
        Assert.Equal(reservation.Id, call.ReservationId);
    }

    [Fact]
    public async Task GetByDateRangeAsync_NoBounds_ReturnsAll()
    {
        // Arrange
        using var factory = new TestDbContextFactory();
        await using var repo = new ReservationRepository(factory);
        var now = DateTime.UtcNow;

        await repo.AddAsync(new Reservation { EventId = EventId1, SequenceNumber = 1, CustomerName = "A", PartySize = 1, Status = ReservationStatus.Waiting, CreatedAt = now.AddDays(-5) }, CancellationToken.None);
        await repo.AddAsync(new Reservation { EventId = EventId1, SequenceNumber = 2, CustomerName = "B", PartySize = 2, Status = ReservationStatus.Seated, CreatedAt = now.AddDays(-3) }, CancellationToken.None);
        await repo.AddAsync(new Reservation { EventId = EventId1, SequenceNumber = 3, CustomerName = "C", PartySize = 3, Status = ReservationStatus.Voided, CreatedAt = now.AddDays(-1) }, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        // Act
        var items = await repo.GetByDateRangeAsync(EventId1, null, null, ReservationStatusFilter.All, CancellationToken.None);

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

        await repo.AddAsync(new Reservation { EventId = EventId1, SequenceNumber = 1, CustomerName = "Old", PartySize = 1, Status = ReservationStatus.Waiting, CreatedAt = now.AddDays(-5) }, CancellationToken.None);
        await repo.AddAsync(new Reservation { EventId = EventId1, SequenceNumber = 2, CustomerName = "New", PartySize = 2, Status = ReservationStatus.Waiting, CreatedAt = now.AddDays(-1) }, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        // Act
        var items = await repo.GetByDateRangeAsync(EventId1, cutoff, null, ReservationStatusFilter.All, CancellationToken.None);

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

        await repo.AddAsync(new Reservation { EventId = EventId1, SequenceNumber = 1, CustomerName = "Old", PartySize = 1, Status = ReservationStatus.Waiting, CreatedAt = now.AddDays(-5) }, CancellationToken.None);
        await repo.AddAsync(new Reservation { EventId = EventId1, SequenceNumber = 2, CustomerName = "New", PartySize = 2, Status = ReservationStatus.Waiting, CreatedAt = now.AddDays(-1) }, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        // Act
        var items = await repo.GetByDateRangeAsync(EventId1, null, cutoff, ReservationStatusFilter.All, CancellationToken.None);

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

        await repo.AddAsync(new Reservation { EventId = EventId1, SequenceNumber = 1, CustomerName = "TooOld", PartySize = 1, Status = ReservationStatus.Waiting, CreatedAt = now.AddDays(-10) }, CancellationToken.None);
        await repo.AddAsync(new Reservation { EventId = EventId1, SequenceNumber = 2, CustomerName = "InRange", PartySize = 2, Status = ReservationStatus.Seated, CreatedAt = now.AddDays(-3) }, CancellationToken.None);
        await repo.AddAsync(new Reservation { EventId = EventId1, SequenceNumber = 3, CustomerName = "TooNew", PartySize = 3, Status = ReservationStatus.Voided, CreatedAt = now.AddDays(-1) }, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        // Act
        var items = await repo.GetByDateRangeAsync(EventId1, now.AddDays(-5), now.AddDays(-2), ReservationStatusFilter.All, CancellationToken.None);

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

        await repo.AddAsync(new Reservation { EventId = EventId1, SequenceNumber = 3, CustomerName = "Third", PartySize = 1, Status = ReservationStatus.Waiting, CreatedAt = now.AddMinutes(-1) }, CancellationToken.None);
        await repo.AddAsync(new Reservation { EventId = EventId1, SequenceNumber = 1, CustomerName = "First", PartySize = 2, Status = ReservationStatus.Waiting, CreatedAt = now.AddMinutes(-30) }, CancellationToken.None);
        await repo.AddAsync(new Reservation { EventId = EventId1, SequenceNumber = 2, CustomerName = "Second", PartySize = 3, Status = ReservationStatus.Waiting, CreatedAt = now.AddMinutes(-15) }, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        // Act
        var items = await repo.GetByDateRangeAsync(EventId1, null, null, ReservationStatusFilter.All, CancellationToken.None);

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

        await repo.AddAsync(new Reservation { EventId = EventId1, SequenceNumber = 1, CustomerName = "A", PartySize = 1, Status = ReservationStatus.Waiting, CreatedAt = DateTime.UtcNow }, CancellationToken.None);
        await repo.AddAsync(new Reservation { EventId = EventId1, SequenceNumber = 2, CustomerName = "B", PartySize = 2, Status = ReservationStatus.Seated, CreatedAt = DateTime.UtcNow }, CancellationToken.None);
        await repo.AddAsync(new Reservation { EventId = EventId1, SequenceNumber = 3, CustomerName = "C", PartySize = 3, Status = ReservationStatus.Called, CreatedAt = DateTime.UtcNow }, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        // Act — explicit status "Seated"
        var (items, total) = await repo.GetPagedAsync(EventId1, "Seated", 1, 50, ReservationStatusFilter.All, CancellationToken.None);

        // Assert
        Assert.Equal(1, total);
        Assert.Single(items);
        Assert.Equal(ReservationStatus.Seated, items[0].Status);
    }
}
