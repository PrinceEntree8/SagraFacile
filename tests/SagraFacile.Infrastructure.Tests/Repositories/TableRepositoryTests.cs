using SagraFacile.Domain.Features.Reservations;
using SagraFacile.Infrastructure.Repositories;
using SagraFacile.Infrastructure.Tests;

namespace SagraFacile.Infrastructure.Tests.Repositories;

public class TableRepositoryTests
{
    [Fact]
    public async Task AddAsync_ThenGetById_ReturnsTable()
    {
        // Arrange
        using var factory = new TestDbContextFactory();
        await using var db = factory.CreateDbContext();
        var repo = new TableRepository(db);
        var table = new Table { TableNumber = "T01", CoverCount = 4, Status = "Available", CreatedAt = DateTime.UtcNow };

        // Act
        await repo.AddAsync(table, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        var found = await repo.GetByIdAsync(table.Id, CancellationToken.None);

        // Assert
        Assert.NotNull(found);
        Assert.Equal("T01", found!.TableNumber);
        Assert.Equal(4, found.CoverCount);
    }

    [Fact]
    public async Task GetByNumberAsync_ReturnsCorrectTable()
    {
        // Arrange
        using var factory = new TestDbContextFactory();
        await using var db = factory.CreateDbContext();
        var repo = new TableRepository(db);

        await repo.AddAsync(new Table { TableNumber = "T01", CoverCount = 4, Status = "Available", CreatedAt = DateTime.UtcNow }, CancellationToken.None);
        await repo.AddAsync(new Table { TableNumber = "T02", CoverCount = 6, Status = "Available", CreatedAt = DateTime.UtcNow }, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        // Act
        var table = await repo.GetByNumberAsync("T02", CancellationToken.None);

        // Assert
        Assert.NotNull(table);
        Assert.Equal(6, table!.CoverCount);
    }

    [Fact]
    public async Task GetAllAsync_NoFilter_ReturnsAllOrderedByTableNumber()
    {
        // Arrange
        using var factory = new TestDbContextFactory();
        await using var db = factory.CreateDbContext();
        var repo = new TableRepository(db);

        await repo.AddAsync(new Table { TableNumber = "T03", CoverCount = 2, Status = "Available", CreatedAt = DateTime.UtcNow }, CancellationToken.None);
        await repo.AddAsync(new Table { TableNumber = "T01", CoverCount = 4, Status = "Available", CreatedAt = DateTime.UtcNow }, CancellationToken.None);
        await repo.AddAsync(new Table { TableNumber = "T02", CoverCount = 6, Status = "Occupied", CreatedAt = DateTime.UtcNow }, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        // Act
        var tables = await repo.GetAllAsync(null, CancellationToken.None);

        // Assert
        Assert.Equal(3, tables.Count);
        Assert.Equal("T01", tables[0].TableNumber);
        Assert.Equal("T02", tables[1].TableNumber);
        Assert.Equal("T03", tables[2].TableNumber);
    }

    [Fact]
    public async Task GetAllAsync_WithStatusFilter_ReturnsOnlyMatchingStatus()
    {
        // Arrange
        using var factory = new TestDbContextFactory();
        await using var db = factory.CreateDbContext();
        var repo = new TableRepository(db);

        await repo.AddAsync(new Table { TableNumber = "T01", CoverCount = 4, Status = "Available", CreatedAt = DateTime.UtcNow }, CancellationToken.None);
        await repo.AddAsync(new Table { TableNumber = "T02", CoverCount = 6, Status = "Occupied", CreatedAt = DateTime.UtcNow }, CancellationToken.None);
        await repo.SaveChangesAsync(CancellationToken.None);

        // Act
        var available = await repo.GetAllAsync("Available", CancellationToken.None);

        // Assert
        Assert.Single(available);
        Assert.Equal("T01", available[0].TableNumber);
    }
}
