using SagraFacile.Domain.Features.Menu;
using SagraFacile.Infrastructure.Repositories;
using SagraFacile.Infrastructure.Tests;

namespace SagraFacile.Infrastructure.Tests.Repositories;

public class MenuRepositoryTests
{
    [Fact]
    public async Task AddAsync_ThenGetById_ReturnsMenuItem()
    {
        // Arrange
        using var factory = new TestDbContextFactory();
        await using var repo = new MenuRepository(factory);
        var item = new MenuItem { EventId = 1, Name = "Pizza", Price = 8m, Category = MenuCategory.MainCourse };

        // Act
        await repo.AddAsync(item, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);
        var found = await repo.GetByIdAsync(item.Id, CancellationToken.None);

        // Assert
        Assert.NotNull(found);
        Assert.Equal("Pizza", found!.Name);
        Assert.Equal(8m, found.Price);
    }

    [Fact]
    public async Task GetByEventIdAsync_FiltersCorrectly()
    {
        // Arrange
        using var factory = new TestDbContextFactory();
        await using var repo = new MenuRepository(factory);
        await repo.AddAsync(new MenuItem { EventId = 1, Name = "Item1", Price = 5m, Category = MenuCategory.Starters }, CancellationToken.None);
        await repo.AddAsync(new MenuItem { EventId = 2, Name = "Item2", Price = 5m, Category = MenuCategory.Drinks }, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        // Act
        var items = await repo.GetByEventIdAsync(1, true, CancellationToken.None);

        // Assert
        Assert.Single(items);
        Assert.Equal("Item1", items[0].Name);
    }

    [Fact]
    public async Task GetByEventIdAsync_ExcludesUnavailableWhenFlagFalse()
    {
        // Arrange
        using var factory = new TestDbContextFactory();
        await using var repo = new MenuRepository(factory);
        await repo.AddAsync(new MenuItem { EventId = 1, Name = "Available", Price = 5m, Category = MenuCategory.Starters, IsAvailable = true }, CancellationToken.None);
        await repo.AddAsync(new MenuItem { EventId = 1, Name = "Unavailable", Price = 5m, Category = MenuCategory.Starters, IsAvailable = false }, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        // Act
        var items = await repo.GetByEventIdAsync(1, false, CancellationToken.None);

        // Assert
        Assert.Single(items);
        Assert.Equal("Available", items[0].Name);
    }

    [Fact]
    public async Task GetByEventIdAsync_IncludesUnavailableWhenFlagTrue()
    {
        // Arrange
        using var factory = new TestDbContextFactory();
        await using var repo = new MenuRepository(factory);
        await repo.AddAsync(new MenuItem { EventId = 1, Name = "Available", Price = 5m, Category = MenuCategory.Starters, IsAvailable = true }, CancellationToken.None);
        await repo.AddAsync(new MenuItem { EventId = 1, Name = "Unavailable", Price = 5m, Category = MenuCategory.Starters, IsAvailable = false }, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        // Act
        var items = await repo.GetByEventIdAsync(1, true, CancellationToken.None);

        // Assert
        Assert.Equal(2, items.Count);
    }

    [Fact]
    public async Task DeleteAsync_RemovesItem()
    {
        // Arrange
        using var factory = new TestDbContextFactory();
        await using var repo = new MenuRepository(factory);
        var item = new MenuItem { EventId = 1, Name = "ToDelete", Price = 3m, Category = MenuCategory.Dessert };
        await repo.AddAsync(item, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);
        var id = item.Id;

        // Act
        await repo.DeleteAsync(id, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);
        var found = await repo.GetByIdAsync(id, CancellationToken.None);

        // Assert
        Assert.Null(found);
    }
}
