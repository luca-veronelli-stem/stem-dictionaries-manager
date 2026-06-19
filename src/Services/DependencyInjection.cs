using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Services.Auth;
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
        services.AddScoped<Services.Interfaces.Auth.IInstallationCredentialService,
            Services.Auth.InstallationCredentialService>();
        services.AddScoped<Services.Interfaces.Auth.IInstallationService,
            Services.Auth.InstallationService>();
        services.AddScoped<Services.Interfaces.Auth.IRegistrationService,
            Services.Auth.RegistrationService>();

        // Per-clientApp descriptor presence policy (see contracts/register.md).
        // Both consumers are strict (Windows desktop / CLI with full identity
        // guarantees): ButtonPanelTester and TelemetryManager (#110) each send
        // osUserId + machineId. Future loose-policy consumers (mobile, web,
        // headless) register their own entries here; a request whose clientApp
        // has no entry surfaces as ClientScopeMismatch -> 401 (conflated with
        // token-unknown / scope-mismatch).
        services.TryAddSingleton<IReadOnlyDictionary<string, DescriptorPolicy>>(_ =>
            new Dictionary<string, DescriptorPolicy>(StringComparer.Ordinal)
            {
                ["ButtonPanelTester"] = new(OsUserIdRequired: true, MachineIdRequired: true),
                ["TelemetryManager"] = new(OsUserIdRequired: true, MachineIdRequired: true),
            });

        return services;
    }
}
