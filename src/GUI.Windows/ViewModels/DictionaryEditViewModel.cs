using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models;
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
/// Item read-only per la sezione variabili standard ereditate.
/// Mostra lo stato effettivo (template + override). L'editing avviene in VariableEdit.
/// </summary>
public record StandardVariableItem
{
    public int VariableId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string DataType { get; init; } = string.Empty;
    public string AccessMode { get; init; } = string.Empty;
    public bool IsEnabled { get; init; }
    public bool IsGloballyDisabled { get; init; }
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
    private readonly IBoardService _boardService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private readonly IMessageService _messageService;

    private int? _editingId;
    private int? _deviceId;
    private int? _originalBoardId;
    private int? _standardDictionaryId;
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

    [ObservableProperty]
    private bool _showOnlyEnabled;

    // === Sezione variabili standard (editabile per override) ===

    private List<StandardVariableItem> _allStandardVariables = [];

    [ObservableProperty]
    private List<StandardVariableItem> _standardVariables = [];

    [ObservableProperty]
    private bool _isStandardExpanded;

    [ObservableProperty]
    private StandardVariableItem? _selectedStandardVariable;

    /// <summary>
    /// True se la sezione variabili specifiche (0x80xx) è espansa.
    /// Default: true (aperta alla creazione/modifica).
    /// </summary>
    [ObservableProperty]
    private bool _isSpecificExpanded = true;

    /// <summary>
    /// True se la sezione standard collapsible deve essere visibile.
    /// Visibile solo per dizionari non-standard già esistenti.
    /// </summary>
    public bool ShowStandardSection => !IsStandard && !IsNew;

    // === Selezione scheda (per dizionari non-standard) ===

    [ObservableProperty]
    private List<BoardSelectItem> _availableBoards = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBoardInvalid))]
    private BoardSelectItem? _selectedBoard;

    /// <summary>
    /// True se la ComboBox scheda deve essere visibile.
    /// Visibile solo per dizionari non-standard.
    /// </summary>
    public bool ShowBoardSelector => !IsStandard;

    public bool IsBoardInvalid => _showValidation && !IsStandard && SelectedBoard is null;

    partial void OnVariableSearchTextChanged(string value) => ApplyVariableFilter();
    partial void OnShowOnlyEnabledChanged(bool value)
    {
        ApplyVariableFilter();
        ApplyStandardFilter();
    }

    public DictionaryEditViewModel(
        IDictionaryService dictionaryService,
        IVariableService variableService,
        IBoardService boardService,
        INavigationService navigationService,
        IDialogService dialogService,
        IMessageService messageService)
    {
        _dictionaryService = dictionaryService;
        _variableService = variableService;
        _boardService = boardService;
        _navigationService = navigationService;
        _dialogService = dialogService;
        _messageService = messageService;
    }

    partial void OnNameChanged(string value) => HasChanges = true;
    partial void OnDescriptionChanged(string value) => HasChanges = true;
    partial void OnIsStandardChanged(bool value) => HasChanges = true;
    partial void OnSelectedBoardChanged(BoardSelectItem? value) => HasChanges = true;

    public async Task InitializeAsync(int? dictionaryId, int? deviceId = null)
    {
        if (_isInitialized)
        {
            return;
        }

        try
        {
            IsBusy = true;
            _editingId = dictionaryId;
            _deviceId = deviceId;

            if (dictionaryId.HasValue)
            {
                Dictionary? dictionary = await _dictionaryService.GetByIdAsync(dictionaryId.Value);
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
                {
                    await LoadStandardVariablesAsync();
                }

                // Deriva il DeviceId dalla board che referenzia questo dizionario
                if (!dictionary.IsStandard && _deviceId is null)
                {
                    IReadOnlyList<Board> allBoards = await _boardService.GetAllAsync();
                    Board? linkedBoard = allBoards.FirstOrDefault(b => b.DictionaryId == dictionaryId.Value);
                    if (linkedBoard is not null)
                    {
                        _deviceId = linkedBoard.DeviceId;
                        _originalBoardId = linkedBoard.Id;
                    }
                }
            }

            // Carica le board del device per la ComboBox (solo per non-standard)
            if (_deviceId.HasValue)
            {
                await LoadAvailableBoardsAsync(_deviceId.Value, dictionaryId);
            }

            // Determina se la checkbox Standard è visibile:
            // visibile se questo dizionario è già standard, oppure non ne esiste ancora uno
            Dictionary? existingStandard = await _dictionaryService.GetStandardDictionaryAsync();
            CanSetStandard = IsStandard || existingStandard is null;

            _isInitialized = true;
            HasChanges = false;

            OnPropertyChanged(nameof(IsNew));
            OnPropertyChanged(nameof(FormTitle));
            OnPropertyChanged(nameof(ShowStandardSection));
            OnPropertyChanged(nameof(ShowBoardSelector));
            OnPropertyChanged(nameof(IsBoardInvalid));
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
    /// Ricarica variabili specifiche e standard (usato dal GoBack per preservare lo stato dizionario).
    /// </summary>
    public async Task ReloadVariablesAsync()
    {
        if (_editingId is null)
        {
            return;
        }

        await LoadVariablesAsync();

        if (!IsStandard)
        {
            await LoadStandardVariablesAsync();
        }
    }

    private async Task LoadVariablesAsync()
    {
        if (_editingId is null)
        {
            return;
        }

        try
        {
            IReadOnlyList<Variable> variables = await _variableService.GetByDictionaryIdAsync(_editingId.Value);

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
    /// Carica le variabili del dizionario standard con merge degli override per-dizionario.
    /// BR-009: stato effettivo = template + override.
    /// </summary>
    private async Task LoadStandardVariablesAsync()
    {
        try
        {
            Dictionary? standardDict = await _dictionaryService.GetStandardDictionaryAsync();
            if (standardDict is null)
            {
                return;
            }

            _standardDictionaryId = standardDict.Id;

            // Carica override esistenti per questo dizionario
            IReadOnlyList<StandardVariableOverride> overrides = _editingId.HasValue
                ? await _variableService.GetOverridesByDictionaryAsync(_editingId.Value)
                : [];

            var overrideMap = overrides.ToDictionary(o => o.StandardVariableId);

            _allStandardVariables = [.. standardDict.Variables
                    .OrderBy(v => v.FullAddress)
                    .Select(v =>
                    {
                        bool hasOverride = overrideMap.TryGetValue(v.Id, out StandardVariableOverride? ov);
                        return new StandardVariableItem
                        {
                            VariableId = v.Id,
                            Name = v.Name,
                            Address = $"0x{v.FullAddress:X4}",
                            DataType = v.DataTypeRaw,
                            AccessMode = v.AccessMode.ToString(),
                            IsGloballyDisabled = !v.IsEnabled,
                            IsEnabled = hasOverride ? ov!.IsEnabled : v.IsEnabled,
                            Description = hasOverride ? (ov!.Description ?? v.Description) : v.Description
                        };
                    })];

            ApplyStandardFilter();
        }
        catch (Exception ex)
        {
            _messageService.Show($"Errore caricamento variabili standard: {ex.Message}", MessageSeverity.Error);
        }
    }

    /// <summary>
    /// Carica le board del device per la ComboBox selezione scheda.
    /// </summary>
    private async Task LoadAvailableBoardsAsync(int deviceId, int? currentDictionaryId)
    {
        try
        {
            IReadOnlyList<Board> boards = await _boardService.GetByDeviceIdAsync(deviceId);
            AvailableBoards = [.. boards
                .OrderBy(b => b.BoardNumber)
                .Select(b => new BoardSelectItem
                {
                    Id = b.Id,
                    Name = $"{b.Name} (FW={b.FirmwareType}, N°{b.BoardNumber})"
                })];

            // Preseleziona la board che referenzia questo dizionario
            if (currentDictionaryId.HasValue)
            {
                Board? linkedBoard = boards.FirstOrDefault(b => b.DictionaryId == currentDictionaryId.Value);
                if (linkedBoard is not null)
                {
                    SelectedBoard = AvailableBoards.FirstOrDefault(b => b.Id == linkedBoard.Id);
                    _originalBoardId = linkedBoard.Id;
                }
            }
        }
        catch (Exception ex)
        {
            _messageService.Show($"Errore caricamento schede: {ex.Message}", MessageSeverity.Error);
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!Validate())
        {
            return;
        }

        try
        {
            IsBusy = true;

            if (IsNew)
            {
                var dictionary = new Core.Models.Dictionary(
                    Name,
                    string.IsNullOrWhiteSpace(Description) ? null : Description,
                    IsStandard);

                Dictionary created = await _dictionaryService.AddAsync(dictionary);
                _editingId = created.Id;

                // Linka la board selezionata al nuovo dizionario
                if (!IsStandard && SelectedBoard is not null)
                {
                    await LinkBoardToDictionaryAsync(SelectedBoard.Id, created.Id);
                }

                _messageService.Show($"Dizionario '{Name}' creato", MessageSeverity.Success);

                OnPropertyChanged(nameof(IsNew));
                OnPropertyChanged(nameof(FormTitle));

                // Carica le variabili standard ereditate per il nuovo dizionario
                if (!IsStandard)
                {
                    await LoadStandardVariablesAsync();
                    OnPropertyChanged(nameof(ShowStandardSection));
                }
            }
            else
            {
                Dictionary? existing = await _dictionaryService.GetByIdAsync(_editingId!.Value);
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

                // Aggiorna il link board se cambiato
                if (!IsStandard && SelectedBoard is not null
                    && SelectedBoard.Id != _originalBoardId)
                {
                    // Scollega la vecchia board
                    if (_originalBoardId.HasValue)
                    {
                        await UnlinkBoardFromDictionaryAsync(_originalBoardId.Value);
                    }

                    // Collega la nuova board
                    await LinkBoardToDictionaryAsync(SelectedBoard.Id, _editingId!.Value);
                    _originalBoardId = SelectedBoard.Id;
                }

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
        if (_editingId is null)
        {
            return;
        }

        DialogResult result = await _dialogService.ShowConfirmAsync(
            "Conferma eliminazione",
            $"L'eliminazione del dizionario '{Name}' e di tutte le sue variabili è irreversibile. Continuare?");

        if (result != Abstractions.DialogResult.Yes)
        {
            return;
        }

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
        if (_editingId is null)
        {
            return;
        }

        _navigationService.NavigateTo(ViewType.VariableEdit, new NavigationParameter
        {
            EntityId = null,
            ParentId = _editingId.Value
        });
    }

    [RelayCommand]
    private void EditVariable(VariableListItem? item)
    {
        if (item is null || _editingId is null)
        {
            return;
        }

        _navigationService.NavigateTo(ViewType.VariableEdit, new NavigationParameter
        {
            EntityId = item.Id,
            ParentId = _editingId.Value
        });
    }

    [RelayCommand]
    private void EditStandardVariable(StandardVariableItem? item)
    {
        if (item is null || _standardDictionaryId is null)
        {
            return;
        }

        _navigationService.NavigateTo(ViewType.VariableEdit, new NavigationParameter
        {
            EntityId = item.VariableId,
            ParentId = _standardDictionaryId.Value,
            DeviceId = _editingId // dictionaryContextId → override mode in VariableEdit
        });
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        if (HasChanges)
        {
            DialogResult result = await _dialogService.ShowConfirmAsync(
                "Annulla modifiche",
                "Sei sicuro di voler annullare le modifiche?");

            if (result != Abstractions.DialogResult.Yes)
            {
                return;
            }
        }

        _navigationService.GoBack();
    }

    private void ApplyVariableFilter()
    {
        IEnumerable<VariableListItem> source = ShowOnlyEnabled
            ? _allVariables.Where(v => v.IsEnabled)
            : _allVariables;

        if (string.IsNullOrWhiteSpace(VariableSearchText))
        {
            Variables = [.. source];
            return;
        }

        string term = VariableSearchText.Trim();
        Variables = [.. source.Where(v =>
            v.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
            v.Address.Contains(term, StringComparison.OrdinalIgnoreCase) ||
            v.DataType.Contains(term, StringComparison.OrdinalIgnoreCase) ||
            (v.Description?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false))];
    }

    private void ApplyStandardFilter()
    {
        StandardVariables = ShowOnlyEnabled
            ? [.. _allStandardVariables.Where(v => v.IsEnabled)]
            : _allStandardVariables;
    }

    private bool Validate()
    {
        _showValidation = true;

        OnPropertyChanged(nameof(IsNameInvalid));
        OnPropertyChanged(nameof(IsBoardInvalid));

        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(Name))
        {
            missing.Add("Nome");
        }

        if (!IsStandard && SelectedBoard is null)
        {
            missing.Add("Scheda");
        }

        if (missing.Count > 0)
        {
            _messageService.Show($"Campi obbligatori mancanti: {string.Join(", ", missing)}", MessageSeverity.Warning);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Aggiorna Board.DictionaryId per collegare una board a un dizionario.
    /// </summary>
    private async Task LinkBoardToDictionaryAsync(int boardId, int dictionaryId)
    {
        Board? board = await _boardService.GetByIdAsync(boardId);
        if (board is null)
        {
            return;
        }

        var updated = Core.Models.Board.Restore(
            board.Id, board.DeviceId, board.Name,
            board.FirmwareType, board.BoardNumber, board.PartNumber,
            board.IsPrimary, dictionaryId: dictionaryId,
            dictionaryName: null, deviceName: null, machineCode: board.MachineCode);
        await _boardService.UpdateAsync(updated);
    }

    /// <summary>
    /// Rimuove il link Board.DictionaryId (setta a null).
    /// </summary>
    private async Task UnlinkBoardFromDictionaryAsync(int boardId)
    {
        Board? board = await _boardService.GetByIdAsync(boardId);
        if (board is null)
        {
            return;
        }

        var updated = Core.Models.Board.Restore(
            board.Id, board.DeviceId, board.Name,
            board.FirmwareType, board.BoardNumber, board.PartNumber,
            board.IsPrimary, dictionaryId: null,
            dictionaryName: null, deviceName: null, machineCode: board.MachineCode);
        await _boardService.UpdateAsync(updated);
    }
}

/// <summary>
/// Item per il dropdown di selezione scheda nel DictionaryEdit.
/// </summary>
public class BoardSelectItem
{
    public int Id { get; init; }
    public required string Name { get; init; }

    public override string ToString() => Name;
}
