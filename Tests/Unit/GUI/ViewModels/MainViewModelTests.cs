#if WINDOWS
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
    private readonly MockCurrentUserService _currentUserService;
    private readonly MainViewModel _viewModel;

    public MainViewModelTests()
    {
        _navigationService = new MockNavigationService();
        _dialogService = new MockDialogService();
        _messageService = new MockMessageService();
        _currentUserService = new MockCurrentUserService();
        _currentUserService.SetCurrentUser(
            Core.Models.User.Restore(1, "test.user", "Test User"));

        // Create a minimal service provider for testing
        var services = new ServiceCollection();
        services.AddSingleton<MockDictionaryService>();
        services.AddSingleton<MockBoardService>();
        services.AddSingleton<INavigationService>(_navigationService);
        services.AddSingleton<IDialogService>(_dialogService);
        services.AddSingleton<IMessageService>(_messageService);
        services.AddSingleton<ICurrentUserService>(_currentUserService);

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
            _currentUserService,
            _serviceProvider);
    }

    [Fact]
    public void Constructor_SetsDefaultTitle()
    {
        // Assert - Title includes view suffix since it navigates to DictionaryList
        Assert.StartsWith("Stem Dictionaries Manager", _viewModel.Title);
    }

    [Fact]
    public void Constructor_NavigatesToInitialView()
    {
        // Assert - Should start with DictionaryList view
        Assert.NotNull(_viewModel.CurrentViewModel);
        Assert.IsType<DictionaryListViewModel>(_viewModel.CurrentViewModel);
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
        Assert.Equal("Test User", _viewModel.CurrentUserDisplayName);
    }

    [Fact]
    public void CurrentUserDisplayName_WhenNoUser_ReturnsDash()
    {
        // Arrange - crea ViewModel con servizio senza utente
        var noUserService = new MockCurrentUserService();
        var services = new ServiceCollection();
        services.AddSingleton<INavigationService>(_navigationService);
        services.AddSingleton<IDialogService>(_dialogService);
        services.AddSingleton<IMessageService>(_messageService);
        services.AddSingleton<ICurrentUserService>(noUserService);
        var sp = services.BuildServiceProvider();

        var vm = new MainViewModel(
            _navigationService, _dialogService, _messageService, noUserService, sp);

        // Assert
        Assert.Equal("—", vm.CurrentUserDisplayName);
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
}
#endif
