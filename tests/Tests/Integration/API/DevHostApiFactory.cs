using Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Tests.Integration.API;

/// <summary>
/// Boots the real <c>Program.cs</c> in its default (Development) environment
/// against a fresh, empty in-memory SQLite database. Unlike the auth factories,
/// it deliberately does NOT pre-create or seed the schema: proving that the
/// Development-gated startup block in <c>Program.cs</c> runs
/// <c>EnsureCreated</c> + <c>DatabaseSeeder.SeedAsync</c> on its own (#87) is
/// the whole point. The shared connection is held open for the host's lifetime
/// so the in-memory schema survives across request scopes.
/// </summary>
internal sealed class DevHostApiFactory : WebApplicationFactory<Program>
{
    /// <summary>ButtonPanelTester dev key from <c>appsettings.json</c> § ApiKeys.</summary>
    public const string ApiKey = "STEM-BT-DEV-KEY-2026";

    private readonly SqliteConnection _connection;

    public DevHostApiFactory()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Swap the real DbContext registration for the shared in-memory
            // connection. The schema stays empty here on purpose.
            ServiceDescriptor? optionsDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (optionsDescriptor is not null)
            {
                services.Remove(optionsDescriptor);
            }
            services.AddDbContext<AppDbContext>(o => o.UseSqlite(_connection));
        });

        // No EnsureCreated / seed here: the Development-gated startup block in
        // Program.cs owns that, and this factory exists to prove it does.
        return base.CreateHost(builder);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _connection.Dispose();
        }
        base.Dispose(disposing);
    }
}
