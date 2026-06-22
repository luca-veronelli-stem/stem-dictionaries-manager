#if WINDOWS
using GUI.Windows.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;
using Tests.Unit.GUI.Mocks;

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
    public async Task InitializeAsync_SetsDatabasePath()
    {
        // Act
        await _viewModel.InitializeAsync();

        // Assert
        Assert.NotEmpty(_viewModel.DatabasePath);
        Assert.Contains("dictionaries.db", _viewModel.DatabasePath);
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
