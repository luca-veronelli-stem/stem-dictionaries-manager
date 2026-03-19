using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models;
using GUI.Windows.Abstractions;
using Services.Interfaces;

namespace GUI.Windows.ViewModels;

/// <summary>
/// Item per la lista variabili.
/// </summary>
public record VariableListItem
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string DataType { get; init; } = string.Empty;
    public string AccessMode { get; init; } = string.Empty;
    public bool IsEnabled { get; init; }
    public string? Description { get; init; }
}

/// <summary>
/// ViewModel per la lista delle variabili di un dizionario.
/// </summary>
public partial class VariableListViewModel : ObservableObject
{
    private readonly IVariableService _variableService;
    private readonly IDictionaryService _dictionaryService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private readonly IMessageService _messageService;

    private int _dictionaryId;
    private bool _isInitialized;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string _dictionaryName = string.Empty;

    private List<VariableListItem> _allVariables = [];

    [ObservableProperty]
    private List<VariableListItem> _variables = [];

    [ObservableProperty]
    private VariableListItem? _selectedItem;

    [ObservableProperty]
    private string _searchText = string.Empty;

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    public VariableListViewModel(
        IVariableService variableService,
        IDictionaryService dictionaryService,
        INavigationService navigationService,
        IDialogService dialogService,
        IMessageService messageService)
    {
        _variableService = variableService;
        _dictionaryService = dictionaryService;
        _navigationService = navigationService;
        _dialogService = dialogService;
        _messageService = messageService;
    }

    /// <summary>
    /// Inizializza il ViewModel con l'ID del dizionario.
    /// </summary>
    public async Task InitializeAsync(int dictionaryId)
    {
        if (_isInitialized && _dictionaryId == dictionaryId) return;

        _dictionaryId = dictionaryId;

        // Carica il nome del dizionario
        var dictionary = await _dictionaryService.GetByIdAsync(dictionaryId);
        if (dictionary is not null)
        {
            DictionaryName = dictionary.Name;
        }

        await RefreshAsync();
        _isInitialized = true;
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var variables = await _variableService.GetByDictionaryIdAsync(_dictionaryId);

            _allVariables = [.. variables
                .Select(v => new VariableListItem
                {
                    Id = v.Id,
                    Name = v.Name,
                    Address = $"0x{v.FullAddress:X4}",
                    DataType = v.DataTypeRaw,
                    AccessMode = v.AccessMode.ToString(),
                    IsEnabled = v.IsEnabled,
                    Description = v.Description
                })
                .OrderBy(v => v.Address)];

            ApplyFilter();
            _messageService.Show($"Caricate {_allVariables.Count} variabili", MessageSeverity.Success);
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
        _navigationService.NavigateTo(ViewType.VariableEdit, new NavigationParameter 
        { 
            EntityId = null,
            ParentId = _dictionaryId
        });
    }

    [RelayCommand]
    private void Edit(VariableListItem? item)
    {
        if (item is null) return;
        _navigationService.NavigateTo(ViewType.VariableEdit, new NavigationParameter 
        { 
            EntityId = item.Id,
            ParentId = _dictionaryId
        });
    }

    [RelayCommand]
    private async Task DeleteAsync(VariableListItem? item)
    {
        if (item is null) return;

        var result = await _dialogService.ShowConfirmAsync(
            "Conferma eliminazione",
            $"Eliminare la variabile '{item.Name}'?");

        if (result != DialogResult.Yes) return;

        try
        {
            IsBusy = true;
            await _variableService.DeleteAsync(item.Id);
            _messageService.Show($"Variabile '{item.Name}' eliminata", MessageSeverity.Success);
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
    private void GoBack()
    {
        _navigationService.GoBack();
    }

    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            Variables = _allVariables;
            return;
        }

        var term = SearchText.Trim();
        Variables = [.. _allVariables.Where(v =>
            v.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
            v.Address.Contains(term, StringComparison.OrdinalIgnoreCase) ||
            v.DataType.Contains(term, StringComparison.OrdinalIgnoreCase) ||
            (v.Description?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false))];
    }
}
