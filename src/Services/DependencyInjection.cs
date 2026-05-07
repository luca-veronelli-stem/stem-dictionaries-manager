using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
        // Default current-user provider (singleton, used by the GUI host).
        // The API host registers its HttpContext-aware variant first, in which
        // case the TryAdd here is a no-op (spec 001 § data-model.md Audit split).
        services.TryAddSingleton<ICurrentUserProvider, CurrentUserProvider>();

        // Services
        services.AddScoped<IDictionaryService, DictionaryService>();
        services.AddScoped<IVariableService, VariableService>();
        services.AddScoped<ICommandService, CommandService>();
        services.AddScoped<IBoardService, BoardService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IDeviceService, DeviceService>();
        services.AddScoped<IAuditService, AuditService>();

        // Spec 001 — bootstrap registration auth services. Mint comes in US2;
        // for US1 only LookupAsync/MarkUsedAsync are implemented.
        services.AddScoped<Services.Interfaces.Auth.IBootstrapTokenService,
            Services.Auth.BootstrapTokenService>();

        return services;
    }
}
