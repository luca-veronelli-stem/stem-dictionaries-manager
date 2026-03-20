using Infrastructure.Interfaces;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registra i servizi di Infrastructure (DbContext, Repositories).
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services,
        string connectionString)
    {
        // DbContext con SQLite
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(connectionString));

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IBoardTypeRepository, BoardTypeRepository>();
        services.AddScoped<IBoardRepository, BoardRepository>();
        services.AddScoped<IDictionaryRepository, DictionaryRepository>();
        services.AddScoped<IVariableRepository, VariableRepository>();
        services.AddScoped<ICommandRepository, CommandRepository>();
        services.AddScoped<IAuditEntryRepository, AuditEntryRepository>();
        services.AddScoped<IBitInterpretationRepository, BitInterpretationRepository>();
        services.AddScoped<ICommandDeviceStateRepository, CommandDeviceStateRepository>();

        return services;
    }
}
