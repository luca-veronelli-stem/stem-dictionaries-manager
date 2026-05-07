using GUI.Windows.Abstractions;
using GUI.Windows.Services;
using GUI.Windows.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace GUI.Windows;

/// <summary>
/// Extension methods for registering GUI services in the DI container.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers every service of the GUI layer.
    /// Requires Infrastructure and Services to be already registered.
    /// </summary>
    public static IServiceCollection AddGUI(this IServiceCollection services)
    {
        // UI Services (Singleton - shared across every ViewModel)
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IMessageService, MessageService>();

        // ViewModels (Transient - new instance per navigation)
        services.AddTransient<MainViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<DeviceListViewModel>();
        services.AddTransient<DeviceDetailViewModel>();
        services.AddTransient<DeviceEditViewModel>();
        services.AddTransient<DeviceCommandsViewModel>();
        services.AddTransient<DictionaryListViewModel>();
        services.AddTransient<DictionaryEditViewModel>();
        services.AddTransient<VariableEditViewModel>();
        services.AddTransient<CommandListViewModel>();
        services.AddTransient<CommandEditViewModel>();
        services.AddTransient<BoardEditViewModel>();
        services.AddTransient<UserListViewModel>();
        services.AddTransient<SettingsViewModel>();

        return services;
    }
}
