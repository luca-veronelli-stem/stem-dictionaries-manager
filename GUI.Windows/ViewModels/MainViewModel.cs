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
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private string _title = "Stem Dictionaries Manager";

    [ObservableProperty]
    private object? _currentViewModel;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _canGoBack;

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

        // Naviga alla view iniziale
        NavigateToView(_navigationService.CurrentView);
    }

    private void OnCurrentViewChanged(object? sender, ViewType viewType)
    {
        NavigateToView(viewType);
        CanGoBack = _navigationService.CanGoBack;
    }

    private void NavigateToView(ViewType viewType)
    {
        CurrentViewModel = viewType switch
        {
            ViewType.DictionaryList => _serviceProvider.GetService(typeof(DictionaryListViewModel)),
            ViewType.DictionaryEdit => _serviceProvider.GetService(typeof(DictionaryEditViewModel)),
            // TODO: Altri ViewModel
            _ => null
        };

        UpdateTitle(viewType);
    }

    private void UpdateTitle(ViewType viewType)
    {
        var suffix = viewType switch
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
            _ => ""
        };

        Title = string.IsNullOrEmpty(suffix) 
            ? "Stem Dictionaries Manager" 
            : $"Stem Dictionaries Manager - {suffix}";
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }

    [RelayCommand]
    private async Task ExitAsync()
    {
        var result = await _dialogService.ShowConfirmAsync(
            "Conferma uscita",
            "Vuoi uscire dall'applicazione?");

        if (result == Abstractions.DialogResult.Yes)
        {
            System.Windows.Application.Current.Shutdown();
        }
    }
}
