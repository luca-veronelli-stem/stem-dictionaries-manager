using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models;
using GUI.Windows.Abstractions;

namespace GUI.Windows.ViewModels;

/// <summary>
/// ViewModel principale dell'applicazione.
/// Gestisce la shell e la navigazione tra le view.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private readonly IMessageService _messageService;
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private string _title = "Stem Dictionaries Manager";

    [ObservableProperty]
    private object? _currentViewModel;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _canGoBack;

    [ObservableProperty]
    private string _pageTitle = "Dizionari";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLoggedIn))]
    [NotifyPropertyChangedFor(nameof(CurrentUserDisplayName))]
    private User? _currentUser;

    /// <summary>
    /// True se l'utente ha effettuato il login.
    /// </summary>
    public bool IsLoggedIn => CurrentUser is not null;

    /// <summary>
    /// Nome visualizzato dell'utente corrente per la sidebar.
    /// </summary>
    public string CurrentUserDisplayName => CurrentUser?.DisplayName ?? "—";

    /// <summary>
    /// Messaggio corrente della status bar.
    /// </summary>
    [ObservableProperty]
    private string? _statusMessage;

    /// <summary>
    /// Severità del messaggio corrente.
    /// </summary>
    [ObservableProperty]
    private MessageSeverity _statusSeverity;

    /// <summary>
    /// Evento fired quando l'utente effettua il logout.
    /// App.xaml.cs lo usa per mostrare di nuovo la LoginView.
    /// </summary>
    public event Action? LoggedOut;

    public MainViewModel(
        INavigationService navigationService,
        IDialogService dialogService,
        IMessageService messageService,
        IServiceProvider serviceProvider)
    {
        _navigationService = navigationService;
        _dialogService = dialogService;
        _messageService = messageService;
        _serviceProvider = serviceProvider;

        // Sottoscrivi ai cambiamenti di navigazione
        _navigationService.CurrentViewChanged += OnCurrentViewChanged;

        // Sottoscrivi ai messaggi della status bar
        _messageService.MessageChanged += OnMessageChanged;
    }

    private void OnMessageChanged(object? sender, EventArgs e)
    {
        StatusMessage = _messageService.CurrentMessage;
        StatusSeverity = _messageService.CurrentSeverity;
    }

    /// <summary>
    /// Imposta l'utente corrente e naviga alla view iniziale.
    /// </summary>
    public void SetUserAndNavigate(User user)
    {
        CurrentUser = user;

        // Naviga alla view iniziale
        NavigateToView(_navigationService.CurrentView, null);
    }

    private void OnCurrentViewChanged(object? sender, ViewType viewType)
    {
        // Recupera il parametro dal NavigationService
        var parameter = _navigationService.CurrentParameter;
        NavigateToView(viewType, parameter);
        CanGoBack = _navigationService.CanGoBack;
    }

    private async void NavigateToView(ViewType viewType, NavigationParameter? parameter)
    {
        try
        {
            // GoBack: riusa il ViewModel cached (preserva stato utente)
            var cached = _navigationService.CachedViewModel;
            if (cached is not null)
            {
                if (cached is DictionaryEditViewModel dictEditVm)
                    await dictEditVm.ReloadVariablesAsync();

                CurrentViewModel = cached;
                UpdateTitle(viewType);
                return;
            }

            // Forward: crea nuovo ViewModel
            var viewModel = CreateViewModel(viewType);

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
                $"Errore durante la navigazione: {ex.Message}",
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
            ViewType.DictionaryList => _serviceProvider.GetService(typeof(DictionaryListViewModel)),
            ViewType.DictionaryEdit => _serviceProvider.GetService(typeof(DictionaryEditViewModel)),
            ViewType.VariableEdit => _serviceProvider.GetService(typeof(VariableEditViewModel)),
            ViewType.CommandList => _serviceProvider.GetService(typeof(CommandListViewModel)),
            ViewType.CommandEdit => _serviceProvider.GetService(typeof(CommandEditViewModel)),
            ViewType.BoardList => _serviceProvider.GetService(typeof(BoardListViewModel)),
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
            // List ViewModels - caricano i dati iniziali
            case DictionaryListViewModel vm:
                await vm.LoadAsync();
                break;

            case CommandListViewModel vm:
                await vm.InitializeAsync();
                break;

            case BoardListViewModel vm:
                await vm.InitializeAsync();
                break;

            case UserListViewModel vm:
                await vm.InitializeAsync();
                break;

            case SettingsViewModel vm:
                await vm.InitializeAsync();
                break;

            // Device Detail - carica dizionari per il device selezionato
            case DeviceDetailViewModel vm when parameter?.DeviceType is not null:
                await vm.LoadAsync(parameter.DeviceType.Value);
                break;

            // Edit ViewModels - caricano entità esistente o preparano per nuova
            case DictionaryEditViewModel vm:
                await vm.InitializeAsync(parameter?.EntityId);
                break;

            case VariableEditViewModel vm when parameter?.ParentId is int dictionaryId:
                await vm.InitializeAsync(parameter?.EntityId, dictionaryId);
                break;

            case CommandEditViewModel vm:
                await vm.InitializeAsync(parameter?.EntityId);
                break;

            case BoardEditViewModel vm:
                await vm.InitializeAsync(parameter?.EntityId);
                break;
        }
    }

    private void UpdateTitle(ViewType viewType)
    {
        PageTitle = viewType switch
        {
            ViewType.DeviceList => "Dispositivi",
            ViewType.DeviceDetail => "Dettaglio Dispositivo",
            ViewType.DictionaryList => "Dizionari",
            ViewType.DictionaryEdit => "Modifica Dizionario",
            ViewType.VariableEdit => "Modifica Variabile",
            ViewType.CommandList => "Comandi",
            ViewType.CommandEdit => "Modifica Comando",
            ViewType.BoardList => "Schede",
            ViewType.BoardEdit => "Modifica Scheda",
            ViewType.UserList => "Utenti",
            ViewType.Settings => "Impostazioni",
            _ => "Stem Dictionaries Manager"
        };

        Title = $"Stem Dictionaries Manager - {PageTitle}";
    }

    [RelayCommand]
    private void NavigateToDevices() =>
        _navigationService.NavigateTo(ViewType.DeviceList);

    [RelayCommand]
    private void NavigateToDictionaries() =>
        _navigationService.NavigateTo(ViewType.DictionaryList);

    [RelayCommand]
    private void NavigateToCommands() =>
        _navigationService.NavigateTo(ViewType.CommandList);

    [RelayCommand]
    private void NavigateToBoards() =>
        _navigationService.NavigateTo(ViewType.BoardList);

    [RelayCommand]
    private void NavigateToUsers() =>
        _navigationService.NavigateTo(ViewType.UserList);

    [RelayCommand]
    private void NavigateToSettings() =>
        _navigationService.NavigateTo(ViewType.Settings);

    [RelayCommand]
    private async Task GoBackAsync()
    {
        // Se il ViewModel corrente ha modifiche non salvate, avvisa
        if (CurrentViewModel is IEditableViewModel { HasChanges: true })
        {
            var result = await _dialogService.ShowConfirmAsync(
                "Modifiche non salvate",
                "Ci sono modifiche non salvate. Vuoi tornare indietro senza salvare?");

            if (result != DialogResult.Yes)
                return;
        }

        _navigationService.GoBack();
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        var result = await _dialogService.ShowConfirmAsync(
            "Conferma logout",
            "Vuoi cambiare utente?");

        if (result != Abstractions.DialogResult.Yes) return;

        // Pulisci utente corrente
        CurrentUser = null;
        CurrentViewModel = null;
        PageTitle = "Login";

        // Notifica App.xaml.cs per mostrare la LoginView
        LoggedOut?.Invoke();
    }
}
