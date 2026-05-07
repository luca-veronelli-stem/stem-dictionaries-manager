using Infrastructure;
using Infrastructure.Interfaces.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Tests.Integration.API.Auth;

/// <summary>
/// Test host for <c>POST /register</c> integration tests. Replaces the
/// real DB with a per-fixture SQLite in-memory connection and supports
/// swapping in a throwing audit-event repo for FR-013 tests.
/// </summary>
internal sealed class RegisterApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection;

    public IRegistrationEventRepository? EventRepoOverride { get; set; }

    public RegisterApiFactory()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        // Pre-create the schema so direct seeds (NewContext) work before the
        // first HttpClient call has triggered the host build.
        using AppDbContext bootstrap = NewContext();
        bootstrap.Database.EnsureCreated();
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
            // Swap real DbContext registration for the shared in-memory connection.
            ServiceDescriptor? optionsDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (optionsDescriptor is not null)
            {
                services.Remove(optionsDescriptor);
            }
            services.AddDbContext<AppDbContext>(o => o.UseSqlite(_connection));

            // Optionally inject the throwing repo for FR-013 tests — must replace
            // the real registration as a scoped service so it composes with the
            // RegistrationService inside the request scope.
            if (EventRepoOverride is not null)
            {
                ServiceDescriptor? eventDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IRegistrationEventRepository));
                if (eventDescriptor is not null)
                {
                    services.Remove(eventDescriptor);
                }
                services.AddScoped(_ => EventRepoOverride);
            }
        });

        IHost host = base.CreateHost(builder);

        using IServiceScope scope = host.Services.CreateScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();

        return host;
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
