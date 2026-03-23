#if WINDOWS
using Core.Models;
using GUI.Windows.Abstractions;
using GUI.Windows.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Tests.Unit.GUI.Mocks;

namespace Tests.Unit.GUI.ViewModels;

/// <summary>
/// Test per MainViewModel.
/// </summary>
public class MainViewModelTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly MockNavigationService _navigationService;
    private readonly MockDialogService _dialogService;
    private readonly MockMessageService _messageService;
    private readonly MainViewModel _viewModel;

    public MainViewModelTests()
    {
        _navigationService = new MockNavigationService();
        _dialogService = new MockDialogService();
        _messageService = new MockMessageService();

        // Create a minimal service provider for testing
        var services = new ServiceCollection();
        services.AddSingleton<MockDictionaryService>();
        services.AddSingleton<MockBoardService>();
        services.AddSingleton<INavigationService>(_navigationService);
        services.AddSingleton<IDialogService>(_dialogService);
        services.AddSingleton<IMessageService>(_messageService);

        // Register ViewModels
        services.AddTransient(sp => new DictionaryListViewModel(
            sp.GetRequiredService<MockDictionaryService>(),
            sp.GetRequiredService<INavigationService>(),
            sp.GetRequiredService<IDialogService>(),
            sp.GetRequiredService<IMessageService>()));

        services.AddTransient(sp => new DictionaryEditViewModel(
            sp.GetRequiredService<MockDictionaryService>(),
            sp.GetRequiredService<MockBoardService>(),
            sp.GetRequiredService<INavigationService>(),
            sp.GetRequiredService<IDialogService>(),
            sp.GetRequiredService<IMessageService>()));

        _serviceProvider = services.BuildServiceProvider();

        _viewModel = new MainViewModel(
            _navigationService,
            _dialogService,
            _messageService,
            _serviceProvider);
    }

    [Fact]
    public void Constructor_SetsDefaultTitle()
    {
        // Assert - Title includes view suffix since it navigates to DictionaryList
        Assert.StartsWith("Stem Dictionaries Manager", _viewModel.Title);
    }

    [Fact]
    public void Constructor_DoesNotNavigateUntilUserIsSet()
    {
        // Assert - CurrentViewModel should be null until user logs in
        Assert.Null(_viewModel.CurrentViewModel);
    }

    [Fact]
    public void CanGoBack_IsFalse_Initially()
    {
        // Assert
        Assert.False(_viewModel.CanGoBack);
    }

    [Fact]
    public void CurrentUserDisplayName_ReturnsUserDisplayName()
    {
        // Arrange
        _viewModel.SetUserAndNavigate(User.Restore(1, "test.user", "Test User"));

        // Assert
        Assert.Equal("Test User", _viewModel.CurrentUserDisplayName);
    }

    [Fact]
    public void CurrentUserDisplayName_WhenNoUser_ReturnsDash()
    {
        // Assert - no user set
        Assert.Equal("—", _viewModel.CurrentUserDisplayName);
    }

    [Fact]
    public void CanGoBack_IsTrue_AfterNavigation()
    {
        // Act
        _navigationService.NavigateTo(ViewType.DictionaryEdit);

        // Assert
        Assert.True(_viewModel.CanGoBack);
    }

    [Fact]
    public void CurrentViewChanged_UpdatesCurrentViewModel()
    {
        // Act
        _navigationService.NavigateTo(ViewType.DictionaryEdit);

        // Assert
        Assert.IsType<DictionaryEditViewModel>(_viewModel.CurrentViewModel);
    }

    [Fact]
    public void GoBackCommand_UpdatesCanGoBack()
    {
        // Arrange
        _navigationService.NavigateTo(ViewType.DictionaryEdit);
        Assert.True(_viewModel.CanGoBack);

        // Act
        _navigationService.GoBack();

        // Assert
        Assert.False(_viewModel.CanGoBack);
    }

    [Fact]
    public void IsBusy_DefaultsFalse()
    {
        // Assert
        Assert.False(_viewModel.IsBusy);
    }

    [Fact]
    public void IsLoggedIn_FalseByDefault()
    {
        // Assert
        Assert.False(_viewModel.IsLoggedIn);
    }

    [Fact]
    public void SetUserAndNavigate_SetsIsLoggedInTrue()
    {
        // Act
        _viewModel.SetUserAndNavigate(User.Restore(1, "admin", "Admin"));

        // Assert
        Assert.True(_viewModel.IsLoggedIn);
        Assert.Equal("Admin", _viewModel.CurrentUserDisplayName);
    }

    [Fact]
    public void SetUserAndNavigate_NavigatesToInitialView()
    {
        // Act
        _viewModel.SetUserAndNavigate(User.Restore(1, "admin", "Admin"));

        // Assert
        Assert.NotNull(_viewModel.CurrentViewModel);
    }

    [Fact]
    public void NavigateToDevicesCommand_NavigatesToDeviceList()
    {
        // Act
        _viewModel.NavigateToDevicesCommand.Execute(null);

        // Assert
        Assert.Equal(ViewType.DeviceList, _navigationService.CurrentView);
    }

    [Fact]
    public void NavigateToDeviceList_UpdatesTitle()
    {
        // Act
        _viewModel.NavigateToDevicesCommand.Execute(null);

        // Assert
        Assert.Equal("Dispositivi", _viewModel.PageTitle);
        Assert.Contains("Dispositivi", _viewModel.Title);
    }

    [Fact]
    public void DeviceDetail_UpdatesTitle()
    {
        // Act
        _navigationService.NavigateTo(ViewType.DeviceDetail, new NavigationParameter
        {
            DeviceType = Core.Enums.DeviceType.OptimusXp
        });

        // Assert
        Assert.Equal("Dettaglio Dispositivo", _viewModel.PageTitle);
    }
}
#endif
