#if WINDOWS
using GUI.Windows.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;
using Tests.Shared;

namespace Tests.Unit.GUI.ViewModels;

/// <summary>
/// Test per SettingsViewModel.
/// </summary>
public class SettingsViewModelTests
{
    private readonly MockNavigationService _navigationService;
    private readonly MockMessageService _messageService;
    private readonly SettingsViewModel _viewModel;

    public SettingsViewModelTests()
    {
        _navigationService = new MockNavigationService();
        _messageService = new MockMessageService();

        _viewModel = new SettingsViewModel(
            _navigationService,
            _messageService,
            NullLogger<SettingsViewModel>.Instance);
    }

    [Fact]
    public async Task InitializeAsync_SetsDatabasePathUnderConformingLocalAppDataRoot()
    {
        // Act
        await _viewModel.InitializeAsync();

        // Assert: APP_DATA v1.9.0 (#135) -- Local root, PascalCase Stem, single
        // DictionariesManager token, db\ sub-folder; never the legacy Roaming
        // %AppData%\Stem.Dictionaries.Manager\ shape it used to surface.
        string conformingDbDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Stem", "DictionariesManager", "db");
        Assert.Equal(conformingDbDir, _viewModel.DatabasePath);
        Assert.DoesNotContain("Stem.Dictionaries.Manager", _viewModel.DatabasePath);
    }

    [Fact]
    public void AppVersion_HasDefaultValue()
    {
        // Assert
        Assert.NotEmpty(_viewModel.AppVersion);
    }

    [Fact]
    public void GoBackCommand_CallsNavigationGoBack()
    {
        // Act
        _viewModel.GoBackCommand.Execute(null);

        // Assert
        Assert.True(_navigationService.GoBackCalled);
    }
}
#endif
