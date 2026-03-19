using GUI.Windows.Abstractions;
using GUI.Windows.Services;
using GUI.Windows.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace GUI.Windows;

/// <summary>
/// Extension methods per la registrazione dei servizi GUI nel DI container.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra tutti i servizi del layer GUI.
    /// Richiede che Infrastructure e Services siano già registrati.
    /// </summary>
    public static IServiceCollection AddGUI(this IServiceCollection services)
    {
        // UI Services (Singleton - condivisi tra tutti i ViewModel)
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IMessageService, MessageService>();

        // ViewModels (Transient - nuova istanza per ogni navigazione)
        services.AddTransient<MainViewModel>();
        services.AddTransient<DictionaryListViewModel>();
        services.AddTransient<DictionaryEditViewModel>();
        services.AddTransient<VariableListViewModel>();
        services.AddTransient<VariableEditViewModel>();
        services.AddTransient<CommandListViewModel>();
        services.AddTransient<CommandEditViewModel>();
        services.AddTransient<BoardListViewModel>();
        services.AddTransient<BoardEditViewModel>();
        services.AddTransient<UserListViewModel>();
        services.AddTransient<SettingsViewModel>();

        return services;
    }
}
