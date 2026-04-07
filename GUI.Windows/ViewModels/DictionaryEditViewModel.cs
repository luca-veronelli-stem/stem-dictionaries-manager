using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GUI.Windows.Abstractions;
using Services.Interfaces;

namespace GUI.Windows.ViewModels;

/// <summary>
/// Item per la lista variabili integrata nella vista dizionario.
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
/// ViewModel per la creazione/modifica di un dizionario.
/// Vista unificata: form dizionario in alto + lista variabili in basso.
/// </summary>
public partial class DictionaryEditViewModel : ObservableObject, IEditableViewModel
{
    private readonly IDictionaryService _dictionaryService;
    private readonly IVariableService _variableService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private readonly IMessageService _messageService;

    private int? _editingId;
    private bool _isInitialized;
    private bool _showValidation;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _hasChanges;

    // === Campi dizionario ===

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNameInvalid))]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private bool _isStandard;

    /// <summary>
    /// True se la checkbox "Standard" deve essere visibile.
    /// Visibile solo se questo dizionario È già standard, oppure non esiste ancora uno standard.
    /// </summary>
    [ObservableProperty]
    private bool _canSetStandard;

    public bool IsNew => _editingId is null;
    public string FormTitle => IsNew ? "Nuovo Dizionario" : "Modifica Dizionario";

    // === Proprietà di validazione per-campo (visibili solo dopo primo tentativo di salvataggio) ===

    public bool IsNameInvalid => _showValidation && string.IsNullOrWhiteSpace(Name);

    // === Lista variabili ===

    private List<VariableListItem> _allVariables = [];

    [ObservableProperty]
    private List<VariableListItem> _variables = [];

    [ObservableProperty]
    private VariableListItem? _selectedVariable;

    [ObservableProperty]
    private string _variableSearchText = string.Empty;

    // === Sezione variabili standard (collapsible, read-only v1) ===

    [ObservableProperty]
    private List<VariableListItem> _standardVariables = [];

    [ObservableProperty]
    private bool _isStandardExpanded;

    /// <summary>
    /// True se la sezione standard collapsible deve essere visibile.
    /// Visibile solo per dizionari non-standard già esistenti.
    /// </summary>
    public bool ShowStandardSection => !IsStandard && !IsNew;

    partial void OnVariableSearchTextChanged(string value) => ApplyVariableFilter();

    public DictionaryEditViewModel(
        IDictionaryService dictionaryService,
        IVariableService variableService,
        INavigationService navigationService,
        IDialogService dialogService,
        IMessageService messageService)
    {
        _dictionaryService = dictionaryService;
        _variableService = variableService;
        _navigationService = navigationService;
        _dialogService = dialogService;
        _messageService = messageService;
    }

    partial void OnNameChanged(string value) => HasChanges = true;
    partial void OnDescriptionChanged(string value) => HasChanges = true;
    partial void OnIsStandardChanged(bool value) => HasChanges = true;

    public async Task InitializeAsync(int? dictionaryId)
    {
        if (_isInitialized) return;

        try
        {
            IsBusy = true;
            _editingId = dictionaryId;

            if (dictionaryId.HasValue)
            {
                var dictionary = await _dictionaryService.GetByIdAsync(dictionaryId.Value);
                if (dictionary is null)
                {
                    await _dialogService.ShowErrorAsync("Errore", "Dizionario non trovato.");
                    _navigationService.GoBack();
                    return;
                }

                Name = dictionary.Name;
                Description = dictionary.Description ?? string.Empty;
                IsStandard = dictionary.IsStandard;

                await LoadVariablesAsync();

                // Per dizionari non-standard, carica le variabili standard come sezione collapsible
                if (!dictionary.IsStandard)
                    await LoadStandardVariablesAsync();
            }

            // Determina se la checkbox Standard è visibile:
            // visibile se questo dizionario è già standard, oppure non ne esiste ancora uno
            var existingStandard = await _dictionaryService.GetStandardDictionaryAsync();
            CanSetStandard = IsStandard || existingStandard is null;

            _isInitialized = true;
            HasChanges = false;

            OnPropertyChanged(nameof(IsNew));
            OnPropertyChanged(nameof(FormTitle));
            OnPropertyChanged(nameof(ShowStandardSection));
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            await _dialogService.ShowErrorAsync("Errore", $"Impossibile caricare: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Ricarica solo le variabili (usato dal GoBack per preservare lo stato dizionario).
    /// </summary>
    public async Task ReloadVariablesAsync()
    {
        if (_editingId is null) return;
        await LoadVariablesAsync();
    }

    private async Task LoadVariablesAsync()
    {
        if (_editingId is null) return;

        try
        {
            var variables = await _variableService.GetByDictionaryIdAsync(_editingId.Value);

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

            ApplyVariableFilter();
        }
        catch (Exception ex)
        {
            _messageService.Show($"Errore caricamento variabili: {ex.Message}", MessageSeverity.Error);
        }
    }

    /// <summary>
    /// Carica le variabili del dizionario standard per la sezione collapsible.
    /// </summary>
    private async Task LoadStandardVariablesAsync()
    {
        try
        {
            var standardDict = await _dictionaryService.GetStandardDictionaryAsync();
            if (standardDict is null) return;

            StandardVariables = [.. standardDict.Variables
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
        }
        catch (Exception ex)
        {
            _messageService.Show($"Errore caricamento variabili standard: {ex.Message}", MessageSeverity.Error);
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!Validate()) return;

        try
        {
            IsBusy = true;

            if (IsNew)
            {
                var dictionary = new Core.Models.Dictionary(
                    Name,
                    string.IsNullOrWhiteSpace(Description) ? null : Description,
                    IsStandard);

                var created = await _dictionaryService.AddAsync(dictionary);
                _editingId = created.Id;
                _messageService.Show($"Dizionario '{Name}' creato", MessageSeverity.Success);

                OnPropertyChanged(nameof(IsNew));
                OnPropertyChanged(nameof(FormTitle));
            }
            else
            {
                var existing = await _dictionaryService.GetByIdAsync(_editingId!.Value);
                if (existing is null)
                {
                    await _dialogService.ShowErrorAsync("Errore", "Dizionario non trovato.");
                    return;
                }

                var updated = Core.Models.Dictionary.Restore(
                    existing.Id,
                    Name,
                    string.IsNullOrWhiteSpace(Description) ? null : Description,
                    IsStandard,
                    existing.Variables);

                await _dictionaryService.UpdateAsync(updated);
                _messageService.Show($"Dizionario '{Name}' aggiornato", MessageSeverity.Success);
            }

            HasChanges = false;
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Errore salvataggio", ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteDictionaryAsync()
    {
        if (_editingId is null) return;

        var result = await _dialogService.ShowConfirmAsync(
            "Conferma eliminazione",
            $"L'eliminazione del dizionario '{Name}' e di tutte le sue variabili è irreversibile. Continuare?");

        if (result != Abstractions.DialogResult.Yes) return;

        try
        {
            IsBusy = true;
            await _dictionaryService.DeleteAsync(_editingId.Value);
            _messageService.Show($"Dizionario '{Name}' eliminato", MessageSeverity.Success);
            HasChanges = false;
            _navigationService.GoBack();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Errore",
                $"Impossibile eliminare: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void AddVariable()
    {
        if (_editingId is null) return;
        _navigationService.NavigateTo(ViewType.VariableEdit, new NavigationParameter
        {
            EntityId = null,
            ParentId = _editingId.Value
        });
    }

    [RelayCommand]
    private void EditVariable(VariableListItem? item)
    {
        if (item is null || _editingId is null) return;
        _navigationService.NavigateTo(ViewType.VariableEdit, new NavigationParameter
        {
            EntityId = item.Id,
            ParentId = _editingId.Value
        });
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        if (HasChanges)
        {
            var result = await _dialogService.ShowConfirmAsync(
                "Annulla modifiche",
                "Sei sicuro di voler annullare le modifiche?");

            if (result != Abstractions.DialogResult.Yes)
                return;
        }

        _navigationService.GoBack();
    }

    private void ApplyVariableFilter()
    {
        if (string.IsNullOrWhiteSpace(VariableSearchText))
        {
            Variables = _allVariables;
            return;
        }

        var term = VariableSearchText.Trim();
        Variables = [.. _allVariables.Where(v =>
            v.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
            v.Address.Contains(term, StringComparison.OrdinalIgnoreCase) ||
            v.DataType.Contains(term, StringComparison.OrdinalIgnoreCase) ||
            (v.Description?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false))];
    }

    private bool Validate()
    {
        _showValidation = true;

        OnPropertyChanged(nameof(IsNameInvalid));

        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(Name)) missing.Add("Nome");

        if (missing.Count > 0)
        {
            _messageService.Show($"Campi obbligatori mancanti: {string.Join(", ", missing)}", MessageSeverity.Warning);
            return false;
        }

        return true;
    }
}
