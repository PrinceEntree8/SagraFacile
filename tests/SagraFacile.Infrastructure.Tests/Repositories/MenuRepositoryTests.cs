using SagraFacile.Domain.Features.Menu;
using SagraFacile.Infrastructure.Repositories;
using SagraFacile.Infrastructure.Tests;

namespace SagraFacile.Infrastructure.Tests.Repositories;

public class MenuRepositoryTests
{
    private static async Task<MenuCategory> CreateCategoryAsync(MenuCategoryRepository catRepo)
    {
        var cat = new MenuCategory { Name = "Test", NameIt = "Test IT", DisplayOrder = 1 };
        await catRepo.AddAsync(cat, CancellationToken.None);
        await catRepo.SaveChangesAsync(CancellationToken.None);
        return cat;
    }

    [Fact]
    public async Task AddAsync_ThenGetById_ReturnsMenuItem()
    {
        using var factory = new TestDbContextFactory();
        await using var catRepo = new MenuCategoryRepository(factory);
        var cat = await CreateCategoryAsync(catRepo);

        await using var repo = new MenuRepository(factory);
        var item = new MenuItem { EventId = 1, Name = "Pizza", PriceInCents = 800, CategoryId = cat.Id };

        await repo.AddAsync(item, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);
        var found = await repo.GetByIdAsync(item.Id, CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal("Pizza", found!.Name);
        Assert.Equal(800, found.PriceInCents);
    }

    [Fact]
    public async Task GetByEventIdAsync_FiltersCorrectly()
    {
        using var factory = new TestDbContextFactory();
        await using var catRepo = new MenuCategoryRepository(factory);
        var cat = await CreateCategoryAsync(catRepo);

        await using var repo = new MenuRepository(factory);
        await repo.AddAsync(new MenuItem { EventId = 1, Name = "Item1", PriceInCents = 500, CategoryId = cat.Id }, CancellationToken.None);
        await repo.AddAsync(new MenuItem { EventId = 2, Name = "Item2", PriceInCents = 500, CategoryId = cat.Id }, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        var items = await repo.GetByEventIdAsync(1, true, CancellationToken.None);

        Assert.Single(items);
        Assert.Equal("Item1", items[0].Name);
    }

    [Fact]
    public async Task GetByEventIdAsync_ExcludesUnavailableWhenFlagFalse()
    {
        using var factory = new TestDbContextFactory();
        await using var catRepo = new MenuCategoryRepository(factory);
        var cat = await CreateCategoryAsync(catRepo);

        await using var repo = new MenuRepository(factory);
        await repo.AddAsync(new MenuItem { EventId = 1, Name = "Available", PriceInCents = 500, CategoryId = cat.Id, IsAvailable = true }, CancellationToken.None);
        await repo.AddAsync(new MenuItem { EventId = 1, Name = "Unavailable", PriceInCents = 500, CategoryId = cat.Id, IsAvailable = false }, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        var items = await repo.GetByEventIdAsync(1, false, CancellationToken.None);

        Assert.Single(items);
        Assert.Equal("Available", items[0].Name);
    }

    [Fact]
    public async Task GetByEventIdAsync_IncludesUnavailableWhenFlagTrue()
    {
        using var factory = new TestDbContextFactory();
        await using var catRepo = new MenuCategoryRepository(factory);
        var cat = await CreateCategoryAsync(catRepo);

        await using var repo = new MenuRepository(factory);
        await repo.AddAsync(new MenuItem { EventId = 1, Name = "Available", PriceInCents = 500, CategoryId = cat.Id, IsAvailable = true }, CancellationToken.None);
        await repo.AddAsync(new MenuItem { EventId = 1, Name = "Unavailable", PriceInCents = 500, CategoryId = cat.Id, IsAvailable = false }, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        var items = await repo.GetByEventIdAsync(1, true, CancellationToken.None);

        Assert.Equal(2, items.Count);
    }

    [Fact]
    public async Task DeleteAsync_RemovesItem()
    {
        using var factory = new TestDbContextFactory();
        await using var catRepo = new MenuCategoryRepository(factory);
        var cat = await CreateCategoryAsync(catRepo);

        await using var repo = new MenuRepository(factory);
        var item = new MenuItem { EventId = 1, Name = "ToDelete", PriceInCents = 300, CategoryId = cat.Id };
        await repo.AddAsync(item, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);
        var id = item.Id;

        await repo.DeleteAsync(id, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);
        var found = await repo.GetByIdAsync(id, CancellationToken.None);

        Assert.Null(found);
    }
}
