using Infrastructure.Interfaces;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
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
}
