using Infrastructure.Interfaces;
using Infrastructure.Repositories;
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
    /// Returns the SQLite database path under AppData.
    /// Creates the folder if it does not exist.
    /// Path: %APPDATA%/STEM/DictionariesManager/sqldb-dictionaries-manager-test.db
    /// </summary>
    public static string GetDefaultSqlitePath()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string folder = Path.Combine(appData, "STEM", "DictionariesManager");
        Directory.CreateDirectory(folder);
        return Path.Combine(folder, SqliteDatabaseFileName);
    }
}
