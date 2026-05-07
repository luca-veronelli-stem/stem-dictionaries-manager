using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GUI.Windows.Abstractions;

namespace GUI.Windows.ViewModels;

/// <summary>
/// ViewModel for the application settings.
/// TODO: implement specific features.
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
    /// Initializes the ViewModel.
    /// </summary>
    public Task InitializeAsync()
    {
        // TODO: load real settings
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
