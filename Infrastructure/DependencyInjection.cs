using Infrastructure.Interfaces;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Nome del file database SQLite.
    /// </summary>
    private const string SqliteDatabaseFileName = "sqldb-dictionaries-manager-test.db";

    /// <summary>
    /// Registra i servizi di Infrastructure (DbContext, Repositories).
    /// Se useSqlServer=true usa SQL Server, altrimenti SQLite.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services,
        string connectionString, bool useSqlServer = false)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            if (useSqlServer)
                options.UseSqlServer(connectionString);
            else
                options.UseSqlite(connectionString);
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
    /// Risolve la connection string per il database.
    /// Per SQLite: se la conn string è vuota, usa il path di default in AppData.
    /// Per SQL Server: cerca nella config, poi nella env var STEM_DICTIONARIES_CONN_STRING.
    /// </summary>
    public static string ResolveConnectionString(
        string provider, string? configuredConnString, bool isSqlServer)
    {
        if (isSqlServer)
        {
            return configuredConnString
                ?? Environment.GetEnvironmentVariable("STEM_DICTIONARIES_CONN_STRING")
                ?? throw new InvalidOperationException(
                    "Connection string non trovata. Configura 'ConnectionStrings:SqlServer' o env var 'STEM_DICTIONARIES_CONN_STRING'.");
        }

        return string.IsNullOrWhiteSpace(configuredConnString)
            ? $"Data Source={GetDefaultSqlitePath()}"
            : configuredConnString;
    }

    /// <summary>
    /// Restituisce il path del database SQLite in AppData.
    /// Crea la cartella se non esiste.
    /// Path: %APPDATA%/STEM/DictionariesManager/sqldb-dictionaries-manager-test.db
    /// </summary>
    public static string GetDefaultSqlitePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var folder = Path.Combine(appData, "STEM", "DictionariesManager");
        Directory.CreateDirectory(folder);
        return Path.Combine(folder, SqliteDatabaseFileName);
    }
}
