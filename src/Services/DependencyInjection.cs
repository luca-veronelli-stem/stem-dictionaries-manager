using Microsoft.Extensions.DependencyInjection;
using Services.Interfaces;

namespace Services;

/// <summary>
/// Extension methods to register the Services layer in the DI container.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers all services from the Services layer.
    /// Requires that Infrastructure is already registered (AddInfrastructure).
    /// </summary>
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        // Singleton: current user shared across all scopes
        services.AddSingleton<ICurrentUserProvider, CurrentUserProvider>();

        // Services
        services.AddScoped<IDictionaryService, DictionaryService>();
        services.AddScoped<IVariableService, VariableService>();
        services.AddScoped<ICommandService, CommandService>();
        services.AddScoped<IBoardService, BoardService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IDeviceService, DeviceService>();
        services.AddScoped<IAuditService, AuditService>();

        return services;
    }
}
