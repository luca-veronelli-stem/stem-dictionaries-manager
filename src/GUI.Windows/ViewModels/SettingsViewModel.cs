using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GUI.Windows.Abstractions;
using Infrastructure;
using Microsoft.Extensions.Logging;

namespace GUI.Windows.ViewModels;

/// <summary>
/// ViewModel for the application settings.
/// TODO: implement specific features.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly IMessageService _messageService;
    private readonly ILogger<SettingsViewModel> _logger;

    [ObservableProperty]
    private string _appVersion = "1.0.0";

    [ObservableProperty]
    private string _databasePath = string.Empty;

    public SettingsViewModel(
        INavigationService navigationService,
        IMessageService messageService,
        ILogger<SettingsViewModel> logger)
    {
        _navigationService = navigationService;
        _messageService = messageService;
        _logger = logger;
    }

    /// <summary>
    /// Initializes the ViewModel.
    /// </summary>
    public Task InitializeAsync()
    {
        // TODO: load real settings.
        // APP_DATA v1.9.0 (#135): surface the conforming Local db root
        // %LocalAppData%\Stem\DictionariesManager\db\ from StemAppData (the
        // single source of truth), not a hand-built legacy Roaming path.
        DatabasePath = StemAppData.BuildDbDir(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));

        _logger.LogDebug("Settings initialized for app version {AppVersion}", AppVersion);

        return Task.CompletedTask;
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }
}
