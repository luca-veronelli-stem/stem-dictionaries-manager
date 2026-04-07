#if WINDOWS
using Core.Models;
using GUI.Windows.Abstractions;
using GUI.Windows.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Services.Interfaces;
using Tests.Unit.GUI.Mocks;

namespace Tests.Unit.GUI.ViewModels;

/// <summary>
/// Test per MainViewModel.
/// SESSION_035: DeviceListViewModel ora richiede IDeviceService.
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
        services.AddSingleton<MockCommandService>();
        services.AddSingleton<MockDeviceService>();
        services.AddSingleton<INavigationService>(_navigationService);
        services.AddSingleton<IDialogService>(_dialogService);
        services.AddSingleton<IMessageService>(_messageService);

        // Register ViewModels
        services.AddTransient(sp => new DeviceListViewModel(
            sp.GetRequiredService<INavigationService>(),
            sp.GetRequiredService<MockDeviceService>(),
            sp.GetRequiredService<MockBoardService>(),
            sp.GetRequiredService<IDialogService>(),
            sp.GetRequiredService<IMessageService>()));

        services.AddTransient(sp => new DictionaryListViewModel(
            sp.GetRequiredService<MockDictionaryService>(),
            sp.GetRequiredService<INavigationService>(),
            sp.GetRequiredService<IDialogService>(),
            sp.GetRequiredService<IMessageService>()));

        services.AddTransient(sp => new DictionaryEditViewModel(
            sp.GetRequiredService<MockDictionaryService>(),
            sp.GetRequiredService<MockVariableService>(),
            sp.GetRequiredService<MockBoardService>(),
            sp.GetRequiredService<INavigationService>(),
            sp.GetRequiredService<IDialogService>(),
            sp.GetRequiredService<IMessageService>()));

        services.AddTransient(sp => new CommandListViewModel(
            sp.GetRequiredService<MockCommandService>(),
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
        Assert.StartsWith("Stem Dictionaries Manager", _viewModel.Title);
    }

    [Fact]
    public void Constructor_DoesNotNavigateUntilUserIsSet()
    {
        Assert.Null(_viewModel.CurrentViewModel);
    }

    [Fact]
    public void CanGoBack_IsFalse_Initially()
    {
        Assert.False(_viewModel.CanGoBack);
    }

    [Fact]
    public void CurrentUserDisplayName_ReturnsUserDisplayName()
    {
        _viewModel.SetUserAndNavigate(User.Restore(1, "test.user", "Test User"));

        Assert.Equal("Test User", _viewModel.CurrentUserDisplayName);
    }

    [Fact]
    public void CurrentUserDisplayName_WhenNoUser_ReturnsDash()
    {
        Assert.Equal("—", _viewModel.CurrentUserDisplayName);
    }

    [Fact]
    public void CanGoBack_IsTrue_AfterNavigation()
    {
        _navigationService.NavigateTo(ViewType.DictionaryEdit);

        Assert.True(_viewModel.CanGoBack);
    }

    [Fact]
    public void CurrentViewChanged_UpdatesCurrentViewModel()
    {
        _navigationService.NavigateTo(ViewType.DictionaryEdit);

        Assert.IsType<DictionaryEditViewModel>(_viewModel.CurrentViewModel);
    }

    [Fact]
    public void GoBackCommand_UpdatesCanGoBack()
    {
        _navigationService.NavigateTo(ViewType.DictionaryEdit);
        Assert.True(_viewModel.CanGoBack);

        _navigationService.GoBack();

        Assert.False(_viewModel.CanGoBack);
    }

    [Fact]
    public void IsBusy_DefaultsFalse()
    {
        Assert.False(_viewModel.IsBusy);
    }

    [Fact]
    public void IsLoggedIn_FalseByDefault()
    {
        Assert.False(_viewModel.IsLoggedIn);
    }

    [Fact]
    public void SetUserAndNavigate_SetsIsLoggedInTrue()
    {
        _viewModel.SetUserAndNavigate(User.Restore(1, "admin", "Admin"));

        Assert.True(_viewModel.IsLoggedIn);
        Assert.Equal("Admin", _viewModel.CurrentUserDisplayName);
    }

    [Fact]
    public void SetUserAndNavigate_NavigatesToInitialView()
    {
        _viewModel.SetUserAndNavigate(User.Restore(1, "admin", "Admin"));

        Assert.NotNull(_viewModel.CurrentViewModel);
    }

    [Fact]
    public void SetUserAndNavigate_ResetsNavigationHistory()
    {
        _viewModel.SetUserAndNavigate(User.Restore(1, "admin", "Admin"));
        _navigationService.NavigateTo(ViewType.DictionaryEdit);
        _navigationService.NavigateTo(ViewType.VariableEdit);

        _viewModel.SetUserAndNavigate(User.Restore(2, "user2", "User 2"));

        Assert.False(_navigationService.CanGoBack);
        Assert.Equal(ViewType.DeviceList, _navigationService.CurrentView);
    }

    [Fact]
    public void NavigateToDevicesCommand_NavigatesToDeviceList()
    {
        _viewModel.NavigateToDevicesCommand.Execute(null);

        Assert.Equal(ViewType.DeviceList, _navigationService.CurrentView);
    }

    [Fact]
    public void NavigateToDeviceList_UpdatesTitle()
    {
        _viewModel.NavigateToDevicesCommand.Execute(null);

        Assert.Equal("Dispositivi", _viewModel.PageTitle);
        Assert.Contains("Dispositivi", _viewModel.Title);
    }

    [Fact]
    public void DeviceDetail_UpdatesTitle()
    {
        _navigationService.NavigateTo(ViewType.DeviceDetail, new NavigationParameter
        {
            DeviceId = 9
        });

        Assert.Equal("Dettaglio Dispositivo", _viewModel.PageTitle);
    }

    [Fact]
    public void DeviceEdit_UpdatesTitle()
    {
        _navigationService.NavigateTo(ViewType.DeviceEdit, new NavigationParameter
        {
            EntityId = null
        });

        Assert.Equal("Modifica Dispositivo", _viewModel.PageTitle);
    }

    [Fact]
    public void NavigateToView_WhenCreateViewModelThrows_DoesNotCrash()
    {
        var navService = new MockNavigationService();
        var services = new ServiceCollection();
        services.AddTransient<DictionaryListViewModel>(_ =>
            throw new InvalidOperationException("Errore risoluzione DI"));
        var throwingProvider = services.BuildServiceProvider();

        var vm = new MainViewModel(
            navService, _dialogService, _messageService, throwingProvider);

        navService.NavigateTo(ViewType.DictionaryList);

        Assert.Null(vm.CurrentViewModel);
        Assert.Equal("Dizionari", vm.PageTitle);
    }

    [Fact]
    public void NavigateToView_WhenCreateViewModelThrows_ShowsErrorInStatusBar()
    {
        var navService = new MockNavigationService();
        var msgService = new MockMessageService();
        var services = new ServiceCollection();
        services.AddTransient<DictionaryListViewModel>(_ =>
            throw new InvalidOperationException("Errore risoluzione DI"));
        var throwingProvider = services.BuildServiceProvider();

        var vm = new MainViewModel(
            navService, _dialogService, msgService, throwingProvider);

        navService.NavigateTo(ViewType.DictionaryList);

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
        _messageService.Show("Salvataggio completato", MessageSeverity.Success);

        Assert.Equal("Salvataggio completato", _viewModel.StatusMessage);
        Assert.Equal(MessageSeverity.Success, _viewModel.StatusSeverity);
    }

    [Fact]
    public void MessageService_Show_Error_UpdatesSeverity()
    {
        _messageService.Show("Errore di rete", MessageSeverity.Error);

        Assert.Equal("Errore di rete", _viewModel.StatusMessage);
        Assert.Equal(MessageSeverity.Error, _viewModel.StatusSeverity);
    }

    [Fact]
    public void MessageService_Clear_ClearsStatusMessage()
    {
        _messageService.Show("Messaggio", MessageSeverity.Warning);
        Assert.NotNull(_viewModel.StatusMessage);

        _messageService.Clear();

        Assert.Null(_viewModel.StatusMessage);
    }

    [Fact]
    public void MessageService_ShowMultiple_LastWins()
    {
        _messageService.Show("Primo", MessageSeverity.Info);
        _messageService.Show("Secondo", MessageSeverity.Success);

        Assert.Equal("Secondo", _viewModel.StatusMessage);
        Assert.Equal(MessageSeverity.Success, _viewModel.StatusSeverity);
    }

    // === Test GoBackCommand con HasChanges ===

    [Fact]
    public async Task GoBackCommand_WithNoEditableViewModel_GoesBackDirectly()
    {
        _navigationService.NavigateTo(ViewType.DictionaryList);

        await _viewModel.GoBackCommand.ExecuteAsync(null);

        Assert.True(_navigationService.GoBackCalled);
    }

    [Fact]
    public async Task GoBackCommand_WithEditableViewModel_NoChanges_GoesBackDirectly()
    {
        _navigationService.NavigateTo(ViewType.DictionaryEdit);

        await _viewModel.GoBackCommand.ExecuteAsync(null);

        Assert.DoesNotContain(_dialogService.Calls, c => c.Type == "Confirm");
    }

    [Fact]
    public async Task GoBackCommand_WithEditableViewModel_HasChanges_ShowsWarning()
    {
        _navigationService.NavigateTo(ViewType.DictionaryEdit);
        var editVm = (DictionaryEditViewModel)_viewModel.CurrentViewModel!;
        await editVm.InitializeAsync(null);
        editVm.Name = "Modified";
        _dialogService.ConfirmResult = DialogResult.Yes;

        await _viewModel.GoBackCommand.ExecuteAsync(null);

        Assert.Contains(_dialogService.Calls, c =>
            c.Type == "Confirm" && c.Message.Contains("modifiche non salvate"));
        Assert.True(_navigationService.GoBackCalled);
    }

    [Fact]
    public async Task GoBackCommand_WithEditableViewModel_HasChanges_UserCancels_StaysOnPage()
    {
        _navigationService.NavigateTo(ViewType.DictionaryEdit);
        var editVm = (DictionaryEditViewModel)_viewModel.CurrentViewModel!;
        await editVm.InitializeAsync(null);
        editVm.Name = "Modified";
        _dialogService.ConfirmResult = DialogResult.No;

        await _viewModel.GoBackCommand.ExecuteAsync(null);

        Assert.Contains(_dialogService.Calls, c => c.Type == "Confirm");
        Assert.False(_navigationService.GoBackCalled);
        Assert.Equal(ViewType.DictionaryEdit, _navigationService.CurrentView);
    }

    // === Test GoBack reload per cached list VMs ===

    [Fact]
    public void GoBack_ToCachedCommandList_ReloadsData()
    {
        _navigationService.NavigateTo(ViewType.CommandList);
        var mockCmdService = _serviceProvider.GetRequiredService<MockCommandService>();

        _navigationService.NavigateTo(ViewType.CommandEdit);
        mockCmdService.MethodCalls.Clear();

        _navigationService.GoBack();

        Assert.Contains(mockCmdService.MethodCalls, m => m == "GetAllAsync");
        Assert.IsType<CommandListViewModel>(_viewModel.CurrentViewModel);
    }

    [Fact]
    public void GoBack_ToCachedDictionaryList_ReloadsData()
    {
        _navigationService.NavigateTo(ViewType.DictionaryList);
        var mockDictService = _serviceProvider.GetRequiredService<MockDictionaryService>();

        _navigationService.NavigateTo(ViewType.DictionaryEdit);
        mockDictService.MethodCalls.Clear();

        _navigationService.GoBack();

        Assert.Contains(mockDictService.MethodCalls, m => m == "GetAllAsync");
        Assert.IsType<DictionaryListViewModel>(_viewModel.CurrentViewModel);
    }

    // === Test GoBack reload per DeviceList ===

    [Fact]
    public void GoBack_ToCachedDeviceList_ReloadsData()
    {
        _navigationService.NavigateTo(ViewType.DeviceList);
        var mockDeviceService = _serviceProvider.GetRequiredService<MockDeviceService>();

        _navigationService.NavigateTo(ViewType.DeviceEdit);
        mockDeviceService.MethodCalls.Clear();

        _navigationService.GoBack();

        Assert.Contains(mockDeviceService.MethodCalls, m => m == "GetAllAsync");
        Assert.IsType<DeviceListViewModel>(_viewModel.CurrentViewModel);
    }
}
#endif
