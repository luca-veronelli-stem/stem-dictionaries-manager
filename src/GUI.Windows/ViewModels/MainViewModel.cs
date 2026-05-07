using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models;
using GUI.Windows.Abstractions;
using Services.Interfaces;

namespace GUI.Windows.ViewModels;

/// <summary>
/// Main application ViewModel.
/// Manages the shell and navigation between views.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private readonly IMessageService _messageService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ICurrentUserProvider _currentUserProvider;

    [ObservableProperty]
    private string _title = "Stem Dictionaries Manager";

    [ObservableProperty]
    private object? _currentViewModel;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _canGoBack;

    [ObservableProperty]
    private string _pageTitle = "Dictionaries";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLoggedIn))]
    [NotifyPropertyChangedFor(nameof(CurrentUserDisplayName))]
    private User? _currentUser;

    /// <summary>
    /// True if the user has logged in.
    /// </summary>
    public bool IsLoggedIn => CurrentUser is not null;

    /// <summary>
    /// Display name of the current user for the sidebar.
    /// </summary>
    public string CurrentUserDisplayName => CurrentUser?.DisplayName ?? "—";

    /// <summary>
    /// Current status-bar message.
    /// </summary>
    [ObservableProperty]
    private string? _statusMessage;

    /// <summary>
    /// Severity of the current message.
    /// </summary>
    [ObservableProperty]
    private MessageSeverity _statusSeverity;

    /// <summary>
    /// Event fired when the user logs out.
    /// App.xaml.cs uses it to show the LoginView again.
    /// </summary>
    public event Action? LoggedOut;

    public MainViewModel(
        INavigationService navigationService,
        IDialogService dialogService,
        IMessageService messageService,
        IServiceProvider serviceProvider,
        ICurrentUserProvider currentUserProvider)
    {
        _navigationService = navigationService;
        _dialogService = dialogService;
        _messageService = messageService;
        _serviceProvider = serviceProvider;
        _currentUserProvider = currentUserProvider;

        // Subscribe to navigation changes
        _navigationService.CurrentViewChanged += OnCurrentViewChanged;

        // Subscribe to status-bar messages
        _messageService.MessageChanged += OnMessageChanged;
    }

    private void OnMessageChanged(object? sender, EventArgs e)
    {
        StatusMessage = _messageService.CurrentMessage;
        StatusSeverity = _messageService.CurrentSeverity;
    }

    /// <summary>
    /// Sets the current user and navigates to the initial view.
    /// </summary>
    public void SetUserAndNavigate(User user)
    {
        CurrentUser = user;
        _currentUserProvider.CurrentUserId = user.Id;

        // Reset navigation: each user session starts clean
        _navigationService.Reset();
        NavigateToView(_navigationService.CurrentView, null);
    }

    private void OnCurrentViewChanged(object? sender, ViewType viewType)
    {
        // Retrieve the parameter from the NavigationService
        NavigationParameter? parameter = _navigationService.CurrentParameter;
        NavigateToView(viewType, parameter);
        CanGoBack = _navigationService.CanGoBack;
    }

    private async void NavigateToView(ViewType viewType, NavigationParameter? parameter)
    {
        try
        {
            // GoBack: reuse the cached ViewModel (preserves user state)
            object? cached = _navigationService.CachedViewModel;
            if (cached is not null)
            {
                if (cached is DictionaryEditViewModel dictEditVm)
                {
                    await dictEditVm.ReloadVariablesAsync();
                }

                if (cached is DeviceDetailViewModel deviceDetailVm && deviceDetailVm.DeviceId.HasValue)
                {
                    await deviceDetailVm.LoadAsync(deviceDetailVm.DeviceId.Value);
                }

                if (cached is CommandListViewModel cmdListVm)
                {
                    await cmdListVm.LoadAsync();
                }

                if (cached is DictionaryListViewModel dictListVm)
                {
                    await dictListVm.LoadAsync();
                }

                if (cached is UserListViewModel userListVm)
                {
                    await userListVm.LoadAsync();
                }

                if (cached is DeviceListViewModel deviceListVm)
                {
                    await deviceListVm.LoadAsync();
                }

                CurrentViewModel = cached;
                UpdateTitle(viewType);
                return;
            }

            // Forward: create a new ViewModel
            object? viewModel = CreateViewModel(viewType);

            if (viewModel is not null)
            {
                await InitializeViewModelAsync(viewModel, parameter);
                _navigationService.SetCurrentViewModel(viewModel);
            }

            CurrentViewModel = viewModel;
            UpdateTitle(viewType);
        }
        catch (Exception ex)
        {
            CurrentViewModel = null;
            UpdateTitle(viewType);
            _messageService.Show(
                $"Error during navigation: {ex.Message}",
                Abstractions.MessageSeverity.Error,
                autoHideSeconds: 0);
        }
    }

    private object? CreateViewModel(ViewType viewType)
    {
        return viewType switch
        {
            ViewType.DeviceList => _serviceProvider.GetService(typeof(DeviceListViewModel)),
            ViewType.DeviceDetail => _serviceProvider.GetService(typeof(DeviceDetailViewModel)),
            ViewType.DeviceEdit => _serviceProvider.GetService(typeof(DeviceEditViewModel)),
            ViewType.DeviceCommands => _serviceProvider.GetService(typeof(DeviceCommandsViewModel)),
            ViewType.DictionaryList => _serviceProvider.GetService(typeof(DictionaryListViewModel)),
            ViewType.DictionaryEdit => _serviceProvider.GetService(typeof(DictionaryEditViewModel)),
            ViewType.VariableEdit => _serviceProvider.GetService(typeof(VariableEditViewModel)),
            ViewType.CommandList => _serviceProvider.GetService(typeof(CommandListViewModel)),
            ViewType.CommandEdit => _serviceProvider.GetService(typeof(CommandEditViewModel)),
            ViewType.BoardEdit => _serviceProvider.GetService(typeof(BoardEditViewModel)),
            ViewType.UserList => _serviceProvider.GetService(typeof(UserListViewModel)),
            ViewType.Settings => _serviceProvider.GetService(typeof(SettingsViewModel)),
            _ => null
        };
    }

    private static async Task InitializeViewModelAsync(object viewModel, NavigationParameter? parameter)
    {
        switch (viewModel)
        {
            // List ViewModels - load initial data
            case DeviceListViewModel vm:
                await vm.LoadAsync();
                break;

            case DictionaryListViewModel vm:
                await vm.LoadAsync();
                break;

            case CommandListViewModel vm:
                await vm.LoadAsync();
                break;

            case UserListViewModel vm:
                await vm.LoadAsync();
                break;

            case SettingsViewModel vm:
                await vm.InitializeAsync();
                break;

            // Device Detail - load dictionaries for the selected device
            case DeviceDetailViewModel vm when parameter?.DeviceId is not null:
                await vm.LoadAsync(parameter.DeviceId.Value);
                break;

            // Device Edit - create or edit a device
            case DeviceEditViewModel vm:
                await vm.InitializeAsync(parameter?.EntityId);
                break;

            // Device Commands - load commands with per-device state
            case DeviceCommandsViewModel vm when parameter?.DeviceId is not null:
                await vm.LoadAsync(parameter.DeviceId.Value);
                break;

            // Edit ViewModels - load existing entity or prepare a new one
            case DictionaryEditViewModel vm:
                await vm.InitializeAsync(parameter?.EntityId, parameter?.DeviceId);
                break;

            case VariableEditViewModel vm when parameter?.ParentId is int dictionaryId:
                await vm.InitializeAsync(parameter?.EntityId, dictionaryId, parameter?.DeviceId);
                break;

            case CommandEditViewModel vm:
                await vm.InitializeAsync(parameter?.EntityId);
                break;

            case BoardEditViewModel vm:
                await vm.InitializeAsync(parameter?.EntityId, parameter?.DeviceId);
                break;
        }
    }

    private void UpdateTitle(ViewType viewType)
    {
        PageTitle = viewType switch
        {
            ViewType.DeviceList => "Devices",
            ViewType.DeviceDetail => "Device Detail",
            ViewType.DeviceEdit => "Edit Device",
            ViewType.DeviceCommands => "Device Commands",
            ViewType.DictionaryList => "Dictionaries",
            ViewType.DictionaryEdit => "Dictionary",
            ViewType.VariableEdit => "Edit Variable",
            ViewType.CommandList => "Commands",
            ViewType.CommandEdit => "Edit Command",
            ViewType.BoardEdit => "Edit Board",
            ViewType.UserList => "Users",
            ViewType.Settings => "Settings",
            _ => "Stem Dictionaries Manager"
        };

        Title = $"Stem Dictionaries Manager - {PageTitle}";
    }

    [RelayCommand]
    private void NavigateToDevices() =>
        _navigationService.NavigateTo(ViewType.DeviceList);

    [RelayCommand]
    private async Task NavigateToStandardAsync()
    {
        try
        {
            var dictionaryService = _serviceProvider.GetService(typeof(IDictionaryService))
                as IDictionaryService;
            Dictionary? standard = await dictionaryService!.GetStandardDictionaryAsync();
            if (standard is null)
            {
                _messageService.Show(
                    "No standard dictionary configured.",
                    MessageSeverity.Warning, autoHideSeconds: 0);
                return;
            }

            _navigationService.NavigateTo(ViewType.DictionaryEdit, new NavigationParameter
            {
                EntityId = standard.Id
            });
        }
        catch (Exception ex)
        {
            _messageService.Show(
                $"Error: {ex.Message}",
                MessageSeverity.Error, autoHideSeconds: 0);
        }
    }

    [RelayCommand]
    private void NavigateToCommands() =>
        _navigationService.NavigateTo(ViewType.CommandList);

    [RelayCommand]
    private void NavigateToUsers() =>
        _navigationService.NavigateTo(ViewType.UserList);

    [RelayCommand]
    private void NavigateToSettings() =>
        _navigationService.NavigateTo(ViewType.Settings);

    [RelayCommand]
    private async Task GoBackAsync()
    {
        // If the current ViewModel has unsaved changes, warn the user
        if (CurrentViewModel is IEditableViewModel { HasChanges: true })
        {
            DialogResult result = await _dialogService.ShowConfirmAsync(
                "Unsaved changes",
                "There are unsaved changes. Go back without saving?");

            if (result != DialogResult.Yes)
            {
                return;
            }
        }

        _navigationService.GoBack();
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        DialogResult result = await _dialogService.ShowConfirmAsync(
            "Confirm logout",
            "Switch user?");

        if (result != Abstractions.DialogResult.Yes)
        {
            return;
        }

        // Clear current user
        CurrentUser = null;
        _currentUserProvider.CurrentUserId = null;
        CurrentViewModel = null;
        PageTitle = "Login";

        // Notify App.xaml.cs to display the LoginView
        LoggedOut?.Invoke();
    }
}
