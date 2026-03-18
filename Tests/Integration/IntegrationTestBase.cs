using Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Tests.Integration;

/// <summary>
/// Base class per integration tests con SQLite in-memory.
/// Crea un DB pulito per ogni test.
/// Implementa IAsyncLifetime per setup asincrono (override InitializeAsync).
/// </summary>
public abstract class IntegrationTestBase : IDisposable, IAsyncLifetime
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

    /// <summary>
    /// Override per setup asincrono (es. seed dati test).
    /// </summary>
    public virtual Task InitializeAsync() => Task.CompletedTask;

    /// <summary>
    /// Override per cleanup asincrono se necessario.
    /// </summary>
    public virtual Task DisposeAsync() => Task.CompletedTask;

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
