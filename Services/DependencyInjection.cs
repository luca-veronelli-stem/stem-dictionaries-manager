using Microsoft.Extensions.DependencyInjection;
using Services.Interfaces;

namespace Services;

/// <summary>
/// Extension methods per la registrazione dei servizi nel DI container.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra tutti i servizi del layer Services.
    /// Richiede che Infrastructure sia già registrato (AddInfrastructure).
    /// </summary>
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        // Services
        services.AddScoped<IDictionaryService, DictionaryService>();
        services.AddScoped<IVariableService, VariableService>();
        services.AddScoped<ICommandService, CommandService>();
        services.AddScoped<IBoardService, BoardService>();
        services.AddScoped<IUserService, UserService>();
        
        return services;
    }
}
