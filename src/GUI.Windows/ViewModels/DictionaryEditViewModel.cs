using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models;
using GUI.Windows.Abstractions;
using Services.Interfaces;

namespace GUI.Windows.ViewModels;

/// <summary>
/// Item for the variable list embedded in the dictionary view.
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
/// Read-only item for the inherited standard-variables section.
/// Shows the effective state (template + override). Editing happens in VariableEdit.
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
/// ViewModel for creating/editing a dictionary.
/// Unified view: dictionary form on top + variable list at the bottom.
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

    // === Dictionary fields ===

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNameInvalid))]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private bool _isStandard;

    /// <summary>
    /// True if the "Standard" checkbox should be visible.
    /// Visible only when this dictionary already IS the standard, or when no standard exists yet.
    /// </summary>
    [ObservableProperty]
    private bool _canSetStandard;

    public bool IsNew => _editingId is null;
    public string FormTitle => IsNew ? "New Dictionary" : "Edit Dictionary";

    // === Per-field validation properties (visible only after the first save attempt) ===

    public bool IsNameInvalid => _showValidation && string.IsNullOrWhiteSpace(Name);

    // === Variable list ===

    private List<VariableListItem> _allVariables = [];

    [ObservableProperty]
    private List<VariableListItem> _variables = [];

    [ObservableProperty]
    private VariableListItem? _selectedVariable;

    [ObservableProperty]
    private string _variableSearchText = string.Empty;

    [ObservableProperty]
    private bool _showOnlyEnabled;

    // === Standard-variables section (editable through overrides) ===

    private List<StandardVariableItem> _allStandardVariables = [];

    [ObservableProperty]
    private List<StandardVariableItem> _standardVariables = [];

    [ObservableProperty]
    private bool _isStandardExpanded;

    [ObservableProperty]
    private StandardVariableItem? _selectedStandardVariable;

    /// <summary>
    /// True if the specific-variables section (0x80xx) is expanded.
    /// Default: true (open on create/edit).
    /// </summary>
    [ObservableProperty]
    private bool _isSpecificExpanded = true;

    /// <summary>
    /// True if the collapsible standard section should be visible.
    /// Visible only for existing non-standard dictionaries.
    /// </summary>
    public bool ShowStandardSection => !IsStandard && !IsNew;

    // === Board selection (for non-standard dictionaries) ===

    [ObservableProperty]
    private List<BoardSelectItem> _availableBoards = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBoardInvalid))]
    private BoardSelectItem? _selectedBoard;

    /// <summary>
    /// True if the board ComboBox should be visible.
    /// Visible only for non-standard dictionaries.
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
                    await _dialogService.ShowErrorAsync("Error", "Dictionary not found.");
                    _navigationService.GoBack();
                    return;
                }

                Name = dictionary.Name;
                Description = dictionary.Description ?? string.Empty;
                IsStandard = dictionary.IsStandard;

                await LoadVariablesAsync();

                // For non-standard dictionaries, load the standard variables as a collapsible section
                if (!dictionary.IsStandard)
                {
                    await LoadStandardVariablesAsync();
                }

                // Derive DeviceId from the board that references this dictionary
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

            // Load the device's boards for the ComboBox (non-standard only)
            if (_deviceId.HasValue)
            {
                await LoadAvailableBoardsAsync(_deviceId.Value, dictionaryId);
            }

            // Decide whether the Standard checkbox is visible:
            // visible if this dictionary is already standard, or no standard exists yet
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
            await _dialogService.ShowErrorAsync("Error", $"Unable to load: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Reloads specific and standard variables (used after GoBack to preserve dictionary state).
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
            _messageService.Show($"Error loading variables: {ex.Message}", MessageSeverity.Error);
        }
    }

    /// <summary>
    /// Loads variables of the standard dictionary, merging in the per-dictionary overrides.
    /// BR-009: effective state = template + override.
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

            // Load the existing overrides for this dictionary
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
            _messageService.Show($"Error loading standard variables: {ex.Message}", MessageSeverity.Error);
        }
    }

    /// <summary>
    /// Loads the device's boards for the board-selection ComboBox.
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

            // Preselect the board that references this dictionary
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
            _messageService.Show($"Error loading boards: {ex.Message}", MessageSeverity.Error);
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

                // Link the selected board to the new dictionary
                if (!IsStandard && SelectedBoard is not null)
                {
                    await LinkBoardToDictionaryAsync(SelectedBoard.Id, created.Id);
                }

                _messageService.Show($"Dictionary '{Name}' created", MessageSeverity.Success);

                OnPropertyChanged(nameof(IsNew));
                OnPropertyChanged(nameof(FormTitle));

                // Load the inherited standard variables for the new dictionary
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
                    await _dialogService.ShowErrorAsync("Error", "Dictionary not found.");
                    return;
                }

                var updated = Core.Models.Dictionary.Restore(
                    existing.Id,
                    Name,
                    string.IsNullOrWhiteSpace(Description) ? null : Description,
                    IsStandard,
                    existing.Variables);

                await _dictionaryService.UpdateAsync(updated);

                // Update the board link if it changed
                if (!IsStandard && SelectedBoard is not null
                    && SelectedBoard.Id != _originalBoardId)
                {
                    // Unlink the old board
                    if (_originalBoardId.HasValue)
                    {
                        await UnlinkBoardFromDictionaryAsync(_originalBoardId.Value);
                    }

                    // Link the new board
                    await LinkBoardToDictionaryAsync(SelectedBoard.Id, _editingId!.Value);
                    _originalBoardId = SelectedBoard.Id;
                }

                _messageService.Show($"Dictionary '{Name}' updated", MessageSeverity.Success);
            }

            HasChanges = false;
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Save error", ex.Message);
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
            "Confirm deletion",
            $"Deleting dictionary '{Name}' and all its variables is irreversible. Continue?");

        if (result != Abstractions.DialogResult.Yes)
        {
            return;
        }

        try
        {
            IsBusy = true;
            await _dictionaryService.DeleteAsync(_editingId.Value);
            _messageService.Show($"Dictionary '{Name}' deleted", MessageSeverity.Success);
            HasChanges = false;
            _navigationService.GoBack();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Error",
                $"Unable to delete: {ex.Message}");
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
                "Discard changes",
                "Are you sure you want to discard the changes?");

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
            missing.Add("Name");
        }

        if (!IsStandard && SelectedBoard is null)
        {
            missing.Add("Board");
        }

        if (missing.Count > 0)
        {
            _messageService.Show($"Required fields missing: {string.Join(", ", missing)}", MessageSeverity.Warning);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Updates Board.DictionaryId to link a board to a dictionary.
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
    /// Removes the Board.DictionaryId link (sets it to null).
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
/// Item for the board-selection dropdown in DictionaryEdit.
/// </summary>
public class BoardSelectItem
{
    public int Id { get; init; }
    public required string Name { get; init; }

    public override string ToString() => Name;
}
