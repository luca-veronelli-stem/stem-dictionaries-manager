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
        services.AddSingleton<MockVariableService>();
        services.AddSingleton<MockBoardService>();
        services.AddSingleton<INavigationService>(_navigationService);
        services.AddSingleton<IDialogService>(_dialogService);
        services.AddSingleton<IMessageService>(_messageService);

        // Register ViewModels
        services.AddTransient(sp => new DeviceListViewModel(
            sp.GetRequiredService<INavigationService>()));

        services.AddTransient(sp => new DictionaryListViewModel(
            sp.GetRequiredService<MockDictionaryService>(),
            sp.GetRequiredService<INavigationService>(),
            sp.GetRequiredService<IDialogService>(),
            sp.GetRequiredService<IMessageService>()));

        services.AddTransient(sp => new DictionaryEditViewModel(
            sp.GetRequiredService<MockDictionaryService>(),
            sp.GetRequiredService<MockVariableService>(),
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
    public void SetUserAndNavigate_ResetsNavigationHistory()
    {
        // Arrange — primo utente naviga in giro
        _viewModel.SetUserAndNavigate(User.Restore(1, "admin", "Admin"));
        _navigationService.NavigateTo(ViewType.DictionaryEdit);
        _navigationService.NavigateTo(ViewType.VariableEdit);

        // Act — secondo utente fa login
        _viewModel.SetUserAndNavigate(User.Restore(2, "user2", "User 2"));

        // Assert — history pulita, non si può tornare indietro
        Assert.False(_navigationService.CanGoBack);
        Assert.Equal(ViewType.DeviceList, _navigationService.CurrentView);
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

    [Fact]
    public void NavigateToView_WhenCreateViewModelThrows_DoesNotCrash()
    {
        // Arrange - service provider con factory che lancia eccezione
        var navService = new MockNavigationService();
        var services = new ServiceCollection();
        services.AddTransient<DictionaryListViewModel>(_ =>
            throw new InvalidOperationException("Errore risoluzione DI"));
        var throwingProvider = services.BuildServiceProvider();

        var vm = new MainViewModel(
            navService, _dialogService, _messageService, throwingProvider);

        // Act - CreateViewModel → GetService → factory lancia
        navService.NavigateTo(ViewType.DictionaryList);

        // Assert - nessun crash, stato coerente
        Assert.Null(vm.CurrentViewModel);
        Assert.Equal("Dizionari", vm.PageTitle);
    }

    [Fact]
    public void NavigateToView_WhenCreateViewModelThrows_ShowsErrorInStatusBar()
    {
        // Arrange
        var navService = new MockNavigationService();
        var msgService = new MockMessageService();
        var services = new ServiceCollection();
        services.AddTransient<DictionaryListViewModel>(_ =>
            throw new InvalidOperationException("Errore risoluzione DI"));
        var throwingProvider = services.BuildServiceProvider();

        var vm = new MainViewModel(
            navService, _dialogService, msgService, throwingProvider);

        // Act
        navService.NavigateTo(ViewType.DictionaryList);

        // Assert - messaggio di errore nella status bar
        Assert.Contains(msgService.Messages, m =>
            m.Severity == MessageSeverity.Error &&
            m.Message.Contains("Errore risoluzione DI"));
    }

    // === Test StatusMessage / StatusSeverity ===

    [Fact]
    public void StatusMessage_IsNull_Initially()
    {
        Assert.Null(_viewModel.StatusMessage);
    }

    [Fact]
    public void StatusSeverity_IsInfo_Initially()
    {
        Assert.Equal(MessageSeverity.Info, _viewModel.StatusSeverity);
    }

    [Fact]
    public void MessageService_Show_UpdatesStatusMessage()
    {
        // Act
        _messageService.Show("Salvataggio completato", MessageSeverity.Success);

        // Assert
        Assert.Equal("Salvataggio completato", _viewModel.StatusMessage);
        Assert.Equal(MessageSeverity.Success, _viewModel.StatusSeverity);
    }

    [Fact]
    public void MessageService_Show_Error_UpdatesSeverity()
    {
        // Act
        _messageService.Show("Errore di rete", MessageSeverity.Error);

        // Assert
        Assert.Equal("Errore di rete", _viewModel.StatusMessage);
        Assert.Equal(MessageSeverity.Error, _viewModel.StatusSeverity);
    }

    [Fact]
    public void MessageService_Clear_ClearsStatusMessage()
    {
        // Arrange
        _messageService.Show("Messaggio", MessageSeverity.Warning);
        Assert.NotNull(_viewModel.StatusMessage);

        // Act
        _messageService.Clear();

        // Assert
        Assert.Null(_viewModel.StatusMessage);
    }

    [Fact]
    public void MessageService_ShowMultiple_LastWins()
    {
        // Act
        _messageService.Show("Primo", MessageSeverity.Info);
        _messageService.Show("Secondo", MessageSeverity.Success);

        // Assert
        Assert.Equal("Secondo", _viewModel.StatusMessage);
        Assert.Equal(MessageSeverity.Success, _viewModel.StatusSeverity);
    }

    // === Test GoBackCommand con HasChanges ===

    [Fact]
    public async Task GoBackCommand_WithNoEditableViewModel_GoesBackDirectly()
    {
        // Arrange - navigate to a list view (not editable)
        _navigationService.NavigateTo(ViewType.DictionaryList);

        // Act
        await _viewModel.GoBackCommand.ExecuteAsync(null);

        // Assert
        Assert.True(_navigationService.GoBackCalled);
    }

    [Fact]
    public async Task GoBackCommand_WithEditableViewModel_NoChanges_GoesBackDirectly()
    {
        // Arrange - navigate, then set CurrentViewModel to an editable with no changes
        _navigationService.NavigateTo(ViewType.DictionaryEdit);
        var editVm = (DictionaryEditViewModel)_viewModel.CurrentViewModel!;
        // HasChanges is false by default

        // Act
        await _viewModel.GoBackCommand.ExecuteAsync(null);

        // Assert - goes back without dialog
        Assert.DoesNotContain(_dialogService.Calls, c => c.Type == "Confirm");
    }

    [Fact]
    public async Task GoBackCommand_WithEditableViewModel_HasChanges_ShowsWarning()
    {
        // Arrange
        _navigationService.NavigateTo(ViewType.DictionaryEdit);
        var editVm = (DictionaryEditViewModel)_viewModel.CurrentViewModel!;
        await editVm.InitializeAsync(null);
        editVm.Name = "Modified"; // triggers HasChanges = true
        _dialogService.ConfirmResult = DialogResult.Yes;

        // Act
        await _viewModel.GoBackCommand.ExecuteAsync(null);

        // Assert - showed warning, then went back
        Assert.Contains(_dialogService.Calls, c =>
            c.Type == "Confirm" && c.Message.Contains("modifiche non salvate"));
        Assert.True(_navigationService.GoBackCalled);
    }

    [Fact]
    public async Task GoBackCommand_WithEditableViewModel_HasChanges_UserCancels_StaysOnPage()
    {
        // Arrange
        _navigationService.NavigateTo(ViewType.DictionaryEdit);
        var editVm = (DictionaryEditViewModel)_viewModel.CurrentViewModel!;
        await editVm.InitializeAsync(null);
        editVm.Name = "Modified";
        _dialogService.ConfirmResult = DialogResult.No;

        // Act
        await _viewModel.GoBackCommand.ExecuteAsync(null);

        // Assert - showed warning, but stayed
        Assert.Contains(_dialogService.Calls, c => c.Type == "Confirm");
        Assert.False(_navigationService.GoBackCalled);
        Assert.Equal(ViewType.DictionaryEdit, _navigationService.CurrentView);
    }
}
#endif
