using Infrastructure;
using Infrastructure.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Tests.Integration.API.Auth;

/// <summary>
/// Test host for the <c>/api/admin/*</c> endpoints. Replaces the real DB
/// with a per-fixture SQLite in-memory connection and seeds the
/// <c>system-admin</c> user that the
/// <c>AdminAuthenticationMiddleware</c> requires (the row is normally
/// added by the <c>AddBootstrapRegistration</c> migration via
/// <c>InsertData</c>, which is not executed by <c>EnsureCreated()</c>).
/// <para>
/// API keys come from <c>appsettings.Development.json</c> + the env var
/// <c>ASPNETCORE_ENVIRONMENT=Development</c> that
/// <see cref="WebApplicationFactory{TEntryPoint}"/> sets by default —
/// override of <c>ConfigureAppConfiguration</c> via
/// <see cref="WebApplicationFactory{TEntryPoint}"/> hooks does not flow
/// reliably under minimal-API hosting (Program.cs uses
/// <c>WebApplication.CreateBuilder</c>), so we read the canonical dev
/// keys directly.
/// </para>
/// </summary>
internal sealed class AdminAuthApiFactory : WebApplicationFactory<Program>
{
    /// <summary>From <c>appsettings.Development.json</c> § AdminApiKeys[0].</summary>
    public const string AdminKey = "STEM-ADMIN-DEV-KEY-2026";

    /// <summary>From <c>appsettings.json</c> § ApiKeys.ButtonPanelTester — present in ApiKeys but NOT in AdminApiKeys.</summary>
    public const string NonAdminKey = "STEM-BT-DEV-KEY-2026";

    /// <summary>
    /// Fixed wall-clock the host's <see cref="TimeProvider"/> reports.
    /// Lets endpoint tests assert <c>mintedAt</c> exactly (#60) without
    /// depending on PBKDF2 + cold-JIT timing on CI runners.
    /// </summary>
    public static readonly DateTime FixedNow = new(2026, 5, 18, 12, 0, 0, DateTimeKind.Utc);

    private readonly SqliteConnection _connection;

    public AdminAuthApiFactory()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        using AppDbContext bootstrap = NewContext();
        bootstrap.Database.EnsureCreated();
        SeedSystemAdmin(bootstrap);
    }

    public AppDbContext NewContext()
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new AppDbContext(options);
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            ServiceDescriptor? optionsDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (optionsDescriptor is not null)
            {
                services.Remove(optionsDescriptor);
            }
            services.AddDbContext<AppDbContext>(o => o.UseSqlite(_connection));

            ServiceDescriptor? timeDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(TimeProvider));
            if (timeDescriptor is not null)
            {
                services.Remove(timeDescriptor);
            }
            services.AddSingleton<TimeProvider>(new FixedTimeProvider(FixedNow));
        });

        IHost host = base.CreateHost(builder);

        using IServiceScope scope = host.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
        SeedSystemAdmin(db);

        return host;
    }

    private static void SeedSystemAdmin(AppDbContext db)
    {
        if (db.Users.Any(u => u.Username == "system-admin"))
        {
            return;
        }
        db.Users.Add(new UserEntity
        {
            Username = "system-admin",
            DisplayName = "System Admin (API key)",
            CreatedAt = new DateTime(2026, 5, 7, 0, 0, 0, DateTimeKind.Utc)
        });
        db.SaveChanges();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _connection.Dispose();
        }
        base.Dispose(disposing);
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _now;
        public FixedTimeProvider(DateTime utc) => _now = new DateTimeOffset(utc, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => _now;
    }
}
