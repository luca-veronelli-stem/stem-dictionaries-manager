using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    private readonly ICurrentUserService _currentUserService;
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

    /// <summary>
    /// Nome visualizzato dell'utente corrente per la sidebar.
    /// </summary>
    public string CurrentUserDisplayName =>
        _currentUserService.CurrentUser?.DisplayName ?? "—";

    public MainViewModel(
        INavigationService navigationService,
        IDialogService dialogService,
        IMessageService messageService,
        ICurrentUserService currentUserService,
        IServiceProvider serviceProvider)
    {
        _navigationService = navigationService;
        _dialogService = dialogService;
        _messageService = messageService;
        _currentUserService = currentUserService;
        _serviceProvider = serviceProvider;

        // Sottoscrivi ai cambiamenti di navigazione
        _navigationService.CurrentViewChanged += OnCurrentViewChanged;

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
        var viewModel = CreateViewModel(viewType);

        if (viewModel is not null)
        {
            // Inizializza il ViewModel con i parametri appropriati
            await InitializeViewModelAsync(viewModel, parameter);
        }

        CurrentViewModel = viewModel;
        UpdateTitle(viewType);
    }

    private object? CreateViewModel(ViewType viewType)
    {
        return viewType switch
        {
            ViewType.DictionaryList => _serviceProvider.GetService(typeof(DictionaryListViewModel)),
            ViewType.DictionaryEdit => _serviceProvider.GetService(typeof(DictionaryEditViewModel)),
            ViewType.VariableList => _serviceProvider.GetService(typeof(VariableListViewModel)),
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

            case VariableListViewModel vm when parameter?.ParentId is int dictionaryId:
                await vm.InitializeAsync(dictionaryId);
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
            ViewType.DictionaryList => "Dizionari",
            ViewType.DictionaryEdit => "Modifica Dizionario",
            ViewType.VariableList => "Variabili",
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
    private void GoBack()
    {
        _navigationService.GoBack();
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        var result = await _dialogService.ShowConfirmAsync(
            "Conferma logout",
            "Vuoi cambiare utente?");

        if (result != Abstractions.DialogResult.Yes) return;

        // Segnala logout e chiude la finestra attiva
        _currentUserService.LogoutRequested = true;
        foreach (System.Windows.Window w in System.Windows.Application.Current.Windows)
        {
            if (w.IsActive) { w.Close(); break; }
        }
    }
}
