using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SagraFacile.Infrastructure.Data;

namespace SagraFacile.Infrastructure.Tests;

/// <summary>
/// Creates a fresh SQLite in-memory ApplicationDbContext for each test.
/// SQLite is used (instead of EF InMemory) because it supports bulk operations such as ExecuteUpdateAsync.
/// Implements IDbContextFactory so it can be passed directly to repositories that use the factory pattern.
/// </summary>
public sealed class TestDbContextFactory : IDbContextFactory<ApplicationDbContext>, IDisposable
{
    private readonly SqliteConnection _connection;

    public TestDbContextFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        using var ctx = CreateDbContext();
        ctx.Database.EnsureCreated();
    }

    public ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new ApplicationDbContext(options);
    }

    public void Dispose() => _connection.Dispose();
}

