using Infrastructure.Interfaces;
using Infrastructure.Interfaces.Auth;
using Infrastructure.Repositories;
using Infrastructure.Repositories.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// SQLite database file name.
    /// </summary>
    private const string SqliteDatabaseFileName = "sqldb-dictionaries-manager-test.db";

    /// <summary>
    /// Registers Infrastructure services (DbContext, Repositories).
    /// If useSqlServer=true uses SQL Server, otherwise SQLite.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services,
        string connectionString, bool useSqlServer = false)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            if (useSqlServer)
            {
                options.UseSqlServer(connectionString);
            }
            else
            {
                options.UseSqlite(connectionString);
            }
        });

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IDeviceRepository, DeviceRepository>();
        services.AddScoped<IBoardRepository, BoardRepository>();
        services.AddScoped<IDictionaryRepository, DictionaryRepository>();
        services.AddScoped<IVariableRepository, VariableRepository>();
        services.AddScoped<ICommandRepository, CommandRepository>();
        services.AddScoped<IAuditEntryRepository, AuditEntryRepository>();
        services.AddScoped<IBitInterpretationRepository, BitInterpretationRepository>();
        services.AddScoped<ICommandDeviceStateRepository, CommandDeviceStateRepository>();
        services.AddScoped<IStandardVariableOverrideRepository, StandardVariableOverrideRepository>();

        // Bootstrap registration repositories (spec 001)
        services.AddScoped<IBootstrapTokenRepository, BootstrapTokenRepository>();
        services.AddScoped<IInstallationRepository, InstallationRepository>();
        services.AddScoped<IInstallationApiCredentialRepository, InstallationApiCredentialRepository>();
        services.AddScoped<IRegistrationEventRepository, RegistrationEventRepository>();

        return services;
    }

    /// <summary>
    /// Resolves the database connection string.
    /// For SQLite: if the conn string is empty, uses the default path under AppData.
    /// For SQL Server: uses the conn string from config (appsettings or env var ConnectionStrings__SqlServer).
    /// </summary>
    public static string ResolveConnectionString(
        string? configuredConnString, bool isSqlServer)
    {
        if (isSqlServer)
        {
            return configuredConnString
                ?? throw new InvalidOperationException(
                    "Connection string not found. Configure 'ConnectionStrings:SqlServer' in appsettings.json or the env var 'ConnectionStrings__SqlServer'.");
        }

        return string.IsNullOrWhiteSpace(configuredConnString)
            ? $"Data Source={GetDefaultSqlitePath()}"
            : configuredConnString;
    }

    /// <summary>
    /// Returns the SQLite database path under the APP_DATA v1.9.0 root
    /// (<c>%LocalAppData%\Stem\DictionariesManager\db\</c>) and migrates any
    /// database left at the legacy Roaming path
    /// (<c>%AppData%\STEM\DictionariesManager\</c>) into place on first call.
    /// Creates the folder if it does not exist.
    /// </summary>
    public static string GetDefaultSqlitePath()
    {
        StemAppDataMigration.MigrateDefaultDatabase(SqliteDatabaseFileName);
        return Path.Combine(StemAppData.GetDbDir(), SqliteDatabaseFileName);
    }
}
