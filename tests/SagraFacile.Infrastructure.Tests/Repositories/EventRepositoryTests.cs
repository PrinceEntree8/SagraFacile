using SagraFacile.Domain.Features.Events;
using SagraFacile.Infrastructure.Repositories;
using SagraFacile.Infrastructure.Tests;

namespace SagraFacile.Infrastructure.Tests.Repositories;

public class EventRepositoryTests
{
    [Fact]
    public async Task AddAsync_ThenGetById_ReturnsEvent()
    {
        // Arrange
        using var factory = new TestDbContextFactory();
        await using var repo = new EventRepository(factory);
        var ev = new Event { Name = "Sagra 2026", Description = "Test", Date = DateTime.UtcNow, Currency = "EUR", CurrencySymbol = "€" };

        // Act
        await repo.AddAsync(ev, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        var found = await repo.GetByIdAsync(ev.Id, CancellationToken.None);

        // Assert
        Assert.NotNull(found);
        Assert.Equal("Sagra 2026", found!.Name);
    }

    [Fact]
    public async Task GetAllOrderedByDateDesc_ReturnsMostRecentFirst()
    {
        // Arrange
        using var factory = new TestDbContextFactory();
        await using var repo = new EventRepository(factory);

        await repo.AddAsync(new Event { Name = "Old", Date = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), Currency = "EUR", CurrencySymbol = "€" }, CancellationToken.None);
        await repo.AddAsync(new Event { Name = "New", Date = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), Currency = "EUR", CurrencySymbol = "€" }, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        // Act
        var events = await repo.GetAllOrderedByDateDescAsync(CancellationToken.None);

        // Assert
        Assert.Equal(2, events.Count);
        Assert.Equal("New", events[0].Name);
        Assert.Equal("Old", events[1].Name);
    }

    [Fact]
    public async Task DeactivateAllAsync_SetsIsActiveFalseForAllActiveEvents()
    {
        // Arrange
        using var factory = new TestDbContextFactory();
        await using var repo = new EventRepository(factory);

        await repo.AddAsync(new Event { Name = "E1", IsActive = true, Date = DateTime.UtcNow, Currency = "EUR", CurrencySymbol = "€" }, CancellationToken.None);
        await repo.AddAsync(new Event { Name = "E2", IsActive = true, Date = DateTime.UtcNow, Currency = "EUR", CurrencySymbol = "€" }, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        // Act
        await repo.DeactivateAllAsync(CancellationToken.None);

        // Open a new repository instance to verify the bulk update was persisted
        await using var repo2 = new EventRepository(factory);
        var events = await repo2.GetAllOrderedByDateDescAsync(CancellationToken.None);

        // Assert
        Assert.All(events, e => Assert.False(e.IsActive));
    }
}
