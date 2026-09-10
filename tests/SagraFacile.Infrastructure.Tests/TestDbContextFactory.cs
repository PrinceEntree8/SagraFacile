using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SagraFacile.Domain.Features.Events;
using SagraFacile.Infrastructure.Data;

namespace SagraFacile.Infrastructure.Tests;

/// <summary>
/// Creates a fresh SQLite in-memory ApplicationDbContext for each test.
/// SQLite is used (instead of EF InMemory) because it supports bulk operations such as ExecuteUpdateAsync.
/// </summary>
public sealed class TestDbContextFactory : IDisposable
{
    private readonly SqliteConnection _connection;
    public ApplicationDbContext DbContext { get; }

    public TestDbContextFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;
        DbContext = new ApplicationDbContext(options);
        DbContext.Database.EnsureCreated();

        DbContext.Events.AddRange(
            new Event { Id = 1, Name = "Test Event 1" },
            new Event { Id = 2, Name = "Test Event 2" }
        );
        DbContext.SaveChanges();
    }

    public void Dispose()
    {
        DbContext.Dispose();
        _connection.Dispose();
    }
}

