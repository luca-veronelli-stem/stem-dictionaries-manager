using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GUI.Windows.Abstractions;

namespace GUI.Windows.ViewModels;

/// <summary>
/// ViewModel per le impostazioni dell'applicazione.
/// TODO: Da implementare con le funzionalità specifiche.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly IMessageService _messageService;

    [ObservableProperty]
    private string _appVersion = "1.0.0";

    [ObservableProperty]
    private string _databasePath = string.Empty;

    public SettingsViewModel(
        INavigationService navigationService,
        IMessageService messageService)
    {
        _navigationService = navigationService;
        _messageService = messageService;
    }

    /// <summary>
    /// Inizializza il ViewModel.
    /// </summary>
    public Task InitializeAsync()
    {
        // TODO: Caricare impostazioni reali
        DatabasePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
            + @"\Stem.Dictionaries.Manager\dictionaries.db";

        return Task.CompletedTask;
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }
}
