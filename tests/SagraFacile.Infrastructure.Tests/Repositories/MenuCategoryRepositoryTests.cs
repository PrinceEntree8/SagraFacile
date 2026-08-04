using SagraFacile.Domain.Features.Menu;
using SagraFacile.Infrastructure.Repositories;
using SagraFacile.Infrastructure.Tests;

namespace SagraFacile.Infrastructure.Tests.Repositories;

public class MenuCategoryRepositoryTests
{
    [Fact]
    public async Task AddAsync_ThenGetById_ReturnsCategory()
    {
        using var factory = new TestDbContextFactory();
        await using var repo = new MenuCategoryRepository(factory);
        var cat = new MenuCategory { Name = "Starters", Code = "starters", DisplayOrder = 1 };

        await repo.AddAsync(cat, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        var found = await repo.GetByIdAsync(cat.Id, CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal("Starters", found!.Name);
        Assert.Equal("starters", found.Code);
        Assert.Equal(1, found.DisplayOrder);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        using var factory = new TestDbContextFactory();
        await using var repo = new MenuCategoryRepository(factory);

        var found = await repo.GetByIdAsync(9999, CancellationToken.None);

        Assert.Null(found);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllOrderedByDisplayOrderThenName()
    {
        using var factory = new TestDbContextFactory();
        await using var repo = new MenuCategoryRepository(factory);

        await repo.AddAsync(new MenuCategory { Name = "Drinks", Code = "drinks", DisplayOrder = 5 }, CancellationToken.None);
        await repo.AddAsync(new MenuCategory { Name = "Dessert", Code = "dessert", DisplayOrder = 4 }, CancellationToken.None);
        await repo.AddAsync(new MenuCategory { Name = "Starters", Code = "starters", DisplayOrder = 1 }, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        var categories = await repo.GetAllAsync(CancellationToken.None);

        Assert.Equal(3, categories.Count);
        Assert.Equal("Starters", categories[0].Name);
        Assert.Equal("Dessert", categories[1].Name);
        Assert.Equal("Drinks", categories[2].Name);
    }

    [Fact]
    public async Task GetAllAsync_SameDisplayOrder_OrdersByName()
    {
        using var factory = new TestDbContextFactory();
        await using var repo = new MenuCategoryRepository(factory);

        await repo.AddAsync(new MenuCategory { Name = "Zucchini", Code = "zucchini", DisplayOrder = 1 }, CancellationToken.None);
        await repo.AddAsync(new MenuCategory { Name = "Antipasto", Code = "antipasto", DisplayOrder = 1 }, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        var categories = await repo.GetAllAsync(CancellationToken.None);

        Assert.Equal(2, categories.Count);
        Assert.Equal("Antipasto", categories[0].Name);
        Assert.Equal("Zucchini", categories[1].Name);
    }

    [Fact]
    public async Task DeleteAsync_RemovesCategory()
    {
        using var factory = new TestDbContextFactory();
        await using var repo = new MenuCategoryRepository(factory);
        var cat = new MenuCategory { Name = "ToDelete", Code = "to-delete", DisplayOrder = 1 };

        await repo.AddAsync(cat, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);
        var id = cat.Id;

        await repo.DeleteAsync(id, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        var found = await repo.GetByIdAsync(id, CancellationToken.None);
        Assert.Null(found);
    }

    [Fact]
    public async Task DeleteAsync_NonExistentId_DoesNotThrow()
    {
        using var factory = new TestDbContextFactory();
        await using var repo = new MenuCategoryRepository(factory);

        // Deleting a non-existent category should not throw
        await repo.DeleteAsync(9999, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task AddAsync_MultipleCategories_AllPersisted()
    {
        using var factory = new TestDbContextFactory();
        await using var repo = new MenuCategoryRepository(factory);

        await repo.AddAsync(new MenuCategory { Name = "Cat1", Code = "cat1", DisplayOrder = 1 }, CancellationToken.None);
        await repo.AddAsync(new MenuCategory { Name = "Cat2", Code = "cat2", DisplayOrder = 2 }, CancellationToken.None);
        await repo.AddAsync(new MenuCategory { Name = "Cat3", Code = "cat3", DisplayOrder = 3 }, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        var all = await repo.GetAllAsync(CancellationToken.None);
        Assert.Equal(3, all.Count);
    }
}
