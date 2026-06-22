using Infrastructure;
using Infrastructure.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tests.Shared;

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

        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new AppDbContext(options);
        Context.Database.EnsureCreated();
    }

    /// <summary>
    /// Crea un utente di test per soddisfare FK AuditEntry.ChangedById.
    /// Chiamare nei test che usano i Service (che ora generano audit).
    /// </summary>
    protected void SeedTestUser()
    {
        if (!Context.Users.Any(u => u.Username == "test-user"))
        {
            Context.Users.Add(TestData.CreateUser("test-user", "Test User"));
            Context.SaveChanges();
        }
    }

    /// <summary>
    /// Override per setup asincrono (es. seed dati test).
    /// </summary>
    public virtual Task InitializeAsync() => Task.CompletedTask;

    /// <summary>
    /// Override per cleanup asincrono se necessario.
    /// </summary>
    public virtual Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Crea Device di test nel DB per soddisfare FK constraints.
    /// I DeviceId corrispondono ai MachineCode per semplicità.
    /// </summary>
    protected async Task SeedTestDevicesAsync()
    {
        Context.Devices.AddRange(
            new DeviceEntity { Name = "Sherpa Slim", MachineCode = 1 },
            new DeviceEntity { Name = "TopLift-M", MachineCode = 2 },
            new DeviceEntity { Name = "Eden-XP", MachineCode = 3 },
            new DeviceEntity { Name = "Gradino", MachineCode = 4 },
            new DeviceEntity { Name = "Spyke", MachineCode = 5 },
            new DeviceEntity { Name = "Spark", MachineCode = 7 },
            new DeviceEntity { Name = "TopLift-A2", MachineCode = 8 },
            new DeviceEntity { Name = "O3Z-Tech", MachineCode = 9 },
            new DeviceEntity { Name = "Optimus-XP", MachineCode = 10 },
            new DeviceEntity { Name = "R3L-XP", MachineCode = 11 },
            new DeviceEntity { Name = "Eden-BS8", MachineCode = 12 }
        );
        await Context.SaveChangesAsync();
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
