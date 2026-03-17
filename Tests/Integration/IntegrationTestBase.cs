using Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Tests.Integration;

/// <summary>
/// Base class per integration tests con SQLite in-memory.
/// Crea un DB pulito per ogni test.
/// </summary>
public abstract class IntegrationTestBase : IDisposable
{
    private readonly SqliteConnection _connection;
    protected readonly AppDbContext Context;

    protected IntegrationTestBase()
    {
        // SQLite in-memory richiede connessione aperta
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new AppDbContext(options);
        Context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
