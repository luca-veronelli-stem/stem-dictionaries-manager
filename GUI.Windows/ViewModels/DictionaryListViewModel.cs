using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GUI.Windows.Abstractions;
using Services.Interfaces;

namespace GUI.Windows.ViewModels;

/// <summary>
/// ViewModel per la lista dei dizionari.
/// </summary>
public partial class DictionaryListViewModel : ObservableObject
{
    private readonly IDictionaryService _dictionaryService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private readonly IMessageService _messageService;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private List<DictionaryListItem> _dictionaries = [];

    [ObservableProperty]
    private DictionaryListItem? _selectedItem;

    [ObservableProperty]
    private string _searchText = string.Empty;

    public DictionaryListViewModel(
        IDictionaryService dictionaryService,
        INavigationService navigationService,
        IDialogService dialogService,
        IMessageService messageService)
    {
        _dictionaryService = dictionaryService;
        _navigationService = navigationService;
        _dialogService = dialogService;
        _messageService = messageService;
    }

    /// <summary>
    /// Carica i dati iniziali. Da chiamare dopo la navigazione.
    /// </summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var dictionaries = await _dictionaryService.GetAllAsync();

            Dictionaries = dictionaries
                .Select(d => new DictionaryListItem
                {
                    Id = d.Id,
                    Name = d.Name,
                    Description = d.Description,
                    BoardTypeName = d.BoardType?.Name,
                    VariableCount = d.Variables.Count
                })
                .ToList();

            _messageService.Show($"Caricati {Dictionaries.Count} dizionari", MessageSeverity.Success);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _messageService.Show($"Errore: {ex.Message}", MessageSeverity.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Add()
    {
        _navigationService.NavigateTo(ViewType.DictionaryEdit, new NavigationParameter { EntityId = null });
    }

    [RelayCommand]
    private void Edit(DictionaryListItem? item)
    {
        if (item is null) return;
        _navigationService.NavigateTo(ViewType.DictionaryEdit, new NavigationParameter { EntityId = item.Id });
    }

    [RelayCommand]
    private async Task DeleteAsync(DictionaryListItem? item)
    {
        if (item is null) return;

        var result = await _dialogService.ShowConfirmAsync(
            "Conferma eliminazione",
            $"Vuoi eliminare il dizionario '{item.Name}'?\nQuesta operazione non può essere annullata.");

        if (result != Abstractions.DialogResult.Yes) return;

        try
        {
            IsBusy = true;
            await _dictionaryService.DeleteAsync(item.Id);
            _messageService.Show($"Dizionario '{item.Name}' eliminato", MessageSeverity.Success);
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Errore", $"Impossibile eliminare: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void OpenVariables(DictionaryListItem? item)
    {
        if (item is null) return;
        _navigationService.NavigateTo(ViewType.VariableList, new NavigationParameter { ParentId = item.Id });
    }
}

/// <summary>
/// Item di visualizzazione per la lista dizionari.
/// </summary>
public class DictionaryListItem
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? BoardTypeName { get; init; }
    public int VariableCount { get; init; }

    /// <summary>
    /// Nome del BoardType o "Standard" se non specificato.
    /// </summary>
    public string BoardTypeDisplay => BoardTypeName ?? "Standard";
}
