using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Enums;
using Core.Models;
using GUI.Windows.Abstractions;
using Microsoft.Extensions.Logging;
using Services.Interfaces;

namespace GUI.Windows.ViewModels;

/// <summary>
/// ViewModel for creating/editing a variable.
/// </summary>
public partial class VariableEditViewModel : ObservableObject, IEditableViewModel
{
    private readonly IVariableService _variableService;
    private readonly IDictionaryService _dictionaryService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private readonly IMessageService _messageService;
    private readonly ILogger<VariableEditViewModel> _logger;

    private int? _editingId;
    private int _dictionaryId;
    private bool _isStandardDictionary;
    private bool _isInitialized;
    private bool _isLoading;
    private bool _showValidation;

    /// <summary>
    /// When set, the view runs in DictionaryContext mode: variable fields read-only,
    /// WordGroups editable for the given DictionaryId. Null = Normal mode.
    /// </summary>
    private int? _dictionaryContextId;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _hasChanges;

    // === Editable fields ===

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FullAddressDisplay))]
    [NotifyPropertyChangedFor(nameof(IsNameInvalid))]
    private string _name = string.Empty;

    /// <summary>
    /// AddressHigh derived automatically from the dictionary kind.
    /// 0x00 = Standard dictionary, 0x80 = other dictionaries.
    /// </summary>
    public string AddressHighHex => _isStandardDictionary ? "00" : "80";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FullAddressDisplay))]
    [NotifyPropertyChangedFor(nameof(IsAddressLowValid))]
    [NotifyPropertyChangedFor(nameof(IsAddressLowInvalid))]
    private string _addressLowHex = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDataTypeOther))]
    [NotifyPropertyChangedFor(nameof(RequiresDataTypeParam))]
    [NotifyPropertyChangedFor(nameof(IsBitmapped))]
    [NotifyPropertyChangedFor(nameof(DataTypeParamLabel))]
    [NotifyPropertyChangedFor(nameof(DataTypeForSave))]
    [NotifyPropertyChangedFor(nameof(IsDataTypeParamInvalid))]
    [NotifyPropertyChangedFor(nameof(IsCustomDataTypeInvalid))]
    [NotifyPropertyChangedFor(nameof(CanRemoveWord))]
    private DataTypeKind _selectedDataTypeKind = DataTypeKind.UInt8;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DataTypeForSave))]
    [NotifyPropertyChangedFor(nameof(IsCustomDataTypeInvalid))]
    private string _customDataType = string.Empty;

    [ObservableProperty]
    private int? _dataTypeParam;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasWordSize))]
    [NotifyPropertyChangedFor(nameof(IsWordSizeInvalid))]
    [NotifyPropertyChangedFor(nameof(DataTypeForSave))]
    private int? _selectedWordSize;

    /// <summary>
    /// Re-evaluates validation when DataTypeParam (Size for Array/String) changes.
    /// </summary>
    partial void OnDataTypeParamChanged(int? value)
    {
        OnPropertyChanged(nameof(IsDataTypeParamInvalid));
    }

    /// <summary>
    /// On data-type change: when Bitmapped, defer WordGroups creation until WordSize is set.
    /// </summary>
    partial void OnSelectedDataTypeKindChanged(DataTypeKind value)
    {
        if (_isLoading) return;

        if (value == DataTypeKind.Bitmapped)
        {
            SelectedWordSize = null;
            WordGroups.Clear();
            OnPropertyChanged(nameof(CanRemoveWord));
        }
        else
        {
            SelectedWordSize = null;
            WordGroups.Clear();
            OnPropertyChanged(nameof(CanRemoveWord));
        }
    }

    /// <summary>
    /// On WordSize change: handles size reduction with bit truncation and confirmation.
    /// </summary>
    partial void OnSelectedWordSizeChanged(int? oldValue, int? newValue)
    {
        if (_isLoading) return;

        if (newValue.HasValue && IsBitmapped)
        {
            if (WordGroups.Count == 0)
            {
                CreateInitialWordGroup();
                HasChanges = true;
                return;
            }

            // Check whether the reduction truncates existing bits
            var hasOverflowBits = WordGroups.Any(g =>
                g.Items.Any(i => i.BitIndex >= newValue.Value));

            if (hasOverflowBits)
            {
                _ = HandleWordSizeReductionAsync(oldValue, newValue.Value);
            }
            else
            {
                RegenerateWordGroups(WordGroups.Count);
                HasChanges = true;
            }
        }
    }

    /// <summary>
    /// Handles WordSize reduction with confirmation and bit truncation.
    /// </summary>
    private async Task HandleWordSizeReductionAsync(int? previousWordSize, int newWordSize)
    {
        bool hasNonEmptyOverflow = WordGroups.Any(g =>
            g.Items.Any(i => i.BitIndex >= newWordSize
                && !string.IsNullOrWhiteSpace(i.Meaning)));

        if (hasNonEmptyOverflow)
        {
            DialogResult result = await _dialogService.ShowConfirmAsync(
                "Word Size reduction",
                $"Some words contain bits with index ≥ {newWordSize} " +
                "that have definitions. These bits will be removed. Continue?");

            if (result != DialogResult.Yes)
            {
                _isLoading = true;
                SelectedWordSize = previousWordSize;
                _isLoading = false;
                return;
            }
        }

        TruncateBitsToWordSize(newWordSize);
        RegenerateWordGroups(WordGroups.Count);
        HasChanges = true;
    }

    /// <summary>
    /// Removes bits with index ≥ wordSize from every word.
    /// </summary>
    private void TruncateBitsToWordSize(int wordSize)
    {
        foreach (WordBitGroup group in WordGroups)
        {
            var toRemove = group.Items
                .Where(i => i.BitIndex >= wordSize)
                .ToList();

            foreach (BitInterpretationItem? item in toRemove)
            {
                group.TryRemoveBit(item);
            }
        }
    }

    [ObservableProperty]
    private AccessMode _selectedAccessMode = AccessMode.ReadOnly;

    [ObservableProperty]
    private string? _format;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMinMaxValid))]
    private double? _minValue;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMinMaxValid))]
    private double? _maxValue;

    [ObservableProperty]
    private string? _unit;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDescriptionInvalid))]
    private string? _description;

    [ObservableProperty]
    private bool _isEnabled = true;

    /// <summary>
    /// Bit groups per word (only for DataType == Bitmapped).
    /// Each WordBitGroup contains up to WordSize BitInterpretationItem entries.
    /// </summary>
    public ObservableCollection<WordBitGroup> WordGroups { get; } = [];

    // === Computed Properties ===

    public bool IsNew => _editingId is null;

    /// <summary>
    /// True when the view runs in DictionaryContext mode (variable fields read-only, only bits editable).
    /// </summary>
    public bool IsDictionaryContext => _dictionaryContextId.HasValue;

    /// <summary>
    /// True when variable fields are editable (Normal mode).
    /// </summary>
    public bool IsNotDictionaryContext => !IsDictionaryContext;

    public string FormTitle => IsDictionaryContext
        ? "Override Standard Variable"
        : IsNew ? "New Variable" : "Edit Variable";

    public string SaveButtonLabel => IsDictionaryContext
        ? "?? Save Override" : "?? Save";

    /// <summary>
    /// True if the selected DataTypeKind is Other (shows the custom TextBox).
    /// </summary>
    public bool IsDataTypeOther => SelectedDataTypeKind == DataTypeKind.Other;

    /// <summary>
    /// True if the type requires a numeric parameter (only Array, String).
    /// Bitmapped uses WordGroups.Count instead of a TextBox.
    /// </summary>
    public bool RequiresDataTypeParam => SelectedDataTypeKind is DataTypeKind.Array or DataTypeKind.String;

    /// <summary>
    /// True for Bitmapped (shows the Word Size selector).
    /// </summary>
    public bool IsBitmapped => SelectedDataTypeKind == DataTypeKind.Bitmapped;

    /// <summary>
    /// True for Bitmapped when WordSize has been selected (shows WordGroups).
    /// </summary>
    public bool HasWordSize => IsBitmapped && SelectedWordSize.HasValue;

    /// <summary>
    /// Validation: WordSize is required for Bitmapped.
    /// </summary>
    public bool IsWordSizeInvalid => _showValidation && IsBitmapped && !SelectedWordSize.HasValue;

    /// <summary>Options for the WordSize ComboBox.</summary>
    public IReadOnlyList<int> WordSizeOptions { get; } = Variable.AllowedWordSizes;

    /// <summary>
    /// Label for the type parameter (with asterisk).
    /// </summary>
    public string DataTypeParamLabel => SelectedDataTypeKind switch
    {
        DataTypeKind.Array or DataTypeKind.String => "Size (bytes) *",
        _ => "Parameter"
    };

    /// <summary>
    /// Data type to persist on save (from the dropdown or custom field).
    /// Bitmapped uses WordGroups.Count as the parameter.
    /// </summary>
    public string DataTypeForSave
    {
        get
        {
            if (IsDataTypeOther)
            {
                return string.IsNullOrWhiteSpace(CustomDataType)
                    ? SelectedDataTypeKind.ToString()
                    : CustomDataType;
            }

            string baseName = SelectedDataTypeKind.ToString();

            // Bitmapped: parameter derived from WordGroups.Count
            if (IsBitmapped && WordGroups.Count > 0)
            {
                return $"{baseName}[{WordGroups.Count}]";
            }

            // Array/String: parameter from the TextBox
            if (RequiresDataTypeParam && DataTypeParam.HasValue)
            {
                return $"{baseName}[{DataTypeParam.Value}]";
            }

            return baseName;
        }
    }

    /// <summary>
    /// Validation: AddressLow must be a valid hex string.
    /// </summary>
    public bool IsAddressLowValid => IsValidHex(AddressLowHex);

    /// <summary>
    /// Validation: Min must be ≤ Max (when both are set).
    /// </summary>
    public bool IsMinMaxValid => !MinValue.HasValue || !MaxValue.HasValue || MinValue.Value <= MaxValue.Value;

    // === Per-field validation properties (visible only after the first save attempt) ===

    public bool IsNameInvalid => _showValidation && string.IsNullOrWhiteSpace(Name);
    public bool IsAddressLowInvalid => _showValidation && string.IsNullOrWhiteSpace(AddressLowHex);
    public bool IsDescriptionInvalid => _showValidation && string.IsNullOrWhiteSpace(Description);
    public bool IsDataTypeParamInvalid => _showValidation && RequiresDataTypeParam && !DataTypeParam.HasValue;

    /// <summary>
    /// True if there are at least 2 words (removal allowed).
    /// </summary>
    public bool CanRemoveWord => IsBitmapped && WordGroups.Count > 1;
    public bool IsCustomDataTypeInvalid => _showValidation && IsDataTypeOther && string.IsNullOrWhiteSpace(CustomDataType);

    /// <summary>
    /// Full address in 0xHHLL format.
    /// </summary>
    public string FullAddressDisplay
    {
        get
        {
            byte high = ParseHexByte(AddressHighHex);
            byte low = ParseHexByte(AddressLowHex);
            return $"0x{(high << 8 | low):X4}";
        }
    }

    // === Hex validation/parsing helpers ===

    private static bool IsValidHex(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            return true; // empty is ok
        }

        return hex.All(c => char.IsAsciiHexDigit(c));
    }

    private static byte ParseHexByte(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            return 0;
        }

        if (byte.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out byte result))
        {
            return result;
        }

        return 0;
    }

    /// <summary>
    /// Filters input to keep only hex characters.
    /// </summary>
    public static string FilterHexInput(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        return new string([.. input.Where(char.IsAsciiHexDigit)]).ToUpperInvariant();
    }

    // === Enum values for the ComboBoxes ===

    public IReadOnlyList<DataTypeKind> DataTypeKinds { get; } = Enum.GetValues<DataTypeKind>();
    public IReadOnlyList<AccessMode> AccessModes { get; } = Enum.GetValues<AccessMode>();

    public VariableEditViewModel(
        IVariableService variableService,
        IDictionaryService dictionaryService,
        INavigationService navigationService,
        IDialogService dialogService,
        IMessageService messageService,
        ILogger<VariableEditViewModel> logger)
    {
        _variableService = variableService;
        _dictionaryService = dictionaryService;
        _navigationService = navigationService;
        _dialogService = dialogService;
        _messageService = messageService;
        _logger = logger;
    }

    /// <summary>
    /// Initializes the ViewModel.
    /// dictionaryContextId != null ⇒ DictionaryContext mode (read-only + per-dictionary editable bits).
    /// </summary>
    public async Task InitializeAsync(int? variableId, int dictionaryId, int? dictionaryContextId = null)
    {
        if (_isInitialized)
        {
            return;
        }

        try
        {
            IsBusy = true;
            _isLoading = true;
            _editingId = variableId;
            _dictionaryId = dictionaryId;
            _dictionaryContextId = dictionaryContextId;

            // Load the dictionary to determine AddressHigh
            Dictionary? dictionary = await _dictionaryService.GetByIdAsync(dictionaryId);
            _isStandardDictionary = dictionary?.IsStandard ?? false;
            OnPropertyChanged(nameof(AddressHighHex));
            OnPropertyChanged(nameof(FullAddressDisplay));
            OnPropertyChanged(nameof(IsDictionaryContext));
            OnPropertyChanged(nameof(IsNotDictionaryContext));

            if (variableId.HasValue)
            {
                Variable? variable = await _variableService.GetByIdAsync(variableId.Value);
                if (variable is null)
                {
                    await _dialogService.ShowErrorAsync("Error", "Variable not found.");
                    _navigationService.GoBack();
                    return;
                }

                LoadFromVariable(variable);

                // DictionaryContext: overrides IsEnabled and Description with the per-dictionary override
                if (_dictionaryContextId.HasValue)
                {
                    StandardVariableOverride? overrideState = await _variableService.GetOverrideAsync(
                        _dictionaryContextId.Value, variableId.Value);
                    if (overrideState is not null)
                    {
                        IsEnabled = overrideState.IsEnabled;
                        if (overrideState.Description is not null)
                        {
                            Description = overrideState.Description;
                        }
                    }
                }

                if (variable.DataTypeKind == DataTypeKind.Bitmapped)
                {
                    // DictionaryContext: load per-dictionary interpretations (with template fallback)
                    // Normal: load all template interpretations
                    IReadOnlyList<BitInterpretation> bits = _dictionaryContextId.HasValue
                        ? await _variableService.GetBitInterpretationsForDictionaryAsync(
                            variableId.Value, _dictionaryContextId.Value)
                        : await _variableService.GetBitInterpretationsAsync(variableId.Value);

                    var existingItems = bits.Select(b => new BitInterpretationItem
                    {
                        WordIndex = b.WordIndex,
                        BitIndex = b.BitIndex,
                        Meaning = b.Meaning ?? string.Empty
                    }).ToList();

                    RegenerateWordGroups(variable.DataTypeParam ?? 1, existingItems);
                }
            }

            _isLoading = false;
            _isInitialized = true;
            HasChanges = false;

            // If new and the type is Bitmapped with WordSize already set, create Word 0
            if (HasWordSize && WordGroups.Count == 0)
            {
                CreateInitialWordGroup();
            }

            OnPropertyChanged(nameof(IsNew));
            OnPropertyChanged(nameof(FormTitle));
            OnPropertyChanged(nameof(CanRemoveWord));
        }
        catch (Exception ex)
        {
            _isLoading = false;
            ErrorMessage = ex.Message;
            await _dialogService.ShowErrorAsync("Error", $"Unable to load: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void LoadFromVariable(Variable v)
    {
        Name = v.Name;
        // AddressHighHex is computed automatically from _isStandardDictionary
        AddressLowHex = v.AddressLow.ToString("X2");
        SelectedDataTypeKind = v.DataTypeKind;
        DataTypeParam = v.DataTypeParam;
        SelectedAccessMode = v.AccessMode;
        Format = v.Format;
        MinValue = v.MinValue;
        MaxValue = v.MaxValue;
        Unit = v.Unit;
        Description = v.Description;
        IsEnabled = v.IsEnabled;
        SelectedWordSize = v.WordSize;

        // Set the custom type when the kind is Other
        if (v.DataTypeKind == DataTypeKind.Other)
        {
            CustomDataType = v.DataTypeRaw;
        }
    }

    private bool Validate()
    {
        _showValidation = true;

        // Notify every validation property
        OnPropertyChanged(nameof(IsNameInvalid));
        OnPropertyChanged(nameof(IsAddressLowInvalid));
        OnPropertyChanged(nameof(IsDescriptionInvalid));
        OnPropertyChanged(nameof(IsDataTypeParamInvalid));
        OnPropertyChanged(nameof(IsCustomDataTypeInvalid));
        OnPropertyChanged(nameof(IsWordSizeInvalid));

        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(Name))
        {
            missing.Add("Name");
        }

        if (string.IsNullOrWhiteSpace(AddressLowHex))
        {
            missing.Add("Address");
        }

        if (string.IsNullOrWhiteSpace(Description))
        {
            missing.Add("Description");
        }

        if (RequiresDataTypeParam && !DataTypeParam.HasValue)
        {
            missing.Add(DataTypeParamLabel.TrimEnd(' ', '*'));
        }

        if (IsDataTypeOther && string.IsNullOrWhiteSpace(CustomDataType))
        {
            missing.Add("Custom data type");
        }

        if (IsBitmapped && !SelectedWordSize.HasValue)
        {
            missing.Add("Word Size (bits)");
        }

        if (!IsAddressLowValid)
        {
            missing.Add("Address (invalid hex format)");
        }

        if (!IsMinMaxValid)
        {
            missing.Add("Min/Max (Min must be ≤ Max)");
        }

        if (missing.Count > 0)
        {
            _messageService.Show($"Required fields missing: {string.Join(", ", missing)}", MessageSeverity.Warning);
            return false;
        }

        return true;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            IsBusy = true;

            if (IsDictionaryContext)
            {
                // DictionaryContext: save the variable override + per-dictionary bit interpretations
                await SaveOverrideAsync();
                await SaveBitInterpretationsForDictionaryAsync();
            }
            else
            {
                // Normal: save variable + common bits
                if (!Validate())
                {
                    return;
                }

                await SaveVariableAsync();
                await SaveCommonBitInterpretationsAsync();
            }

            HasChanges = false;
            _navigationService.GoBack();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save variable {Name}", Name);
            await _dialogService.ShowErrorAsync("Error", $"Unable to save: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SaveVariableAsync()
    {
        byte addressHigh = ParseHexByte(AddressHighHex);
        byte addressLow = ParseHexByte(AddressLowHex);
        int? dataTypeParam = IsBitmapped ? WordGroups.Count : DataTypeParam;
        int? wordSize = IsBitmapped ? SelectedWordSize : null;

        if (IsNew)
        {
            var variable = new Variable(
                name: Name,
                addressHigh: addressHigh,
                addressLow: addressLow,
                dataTypeKind: SelectedDataTypeKind,
                accessMode: SelectedAccessMode,
                dataTypeRaw: DataTypeForSave,
                dataTypeParam: dataTypeParam,
                isEnabled: IsEnabled,
                format: Format,
                minValue: MinValue,
                maxValue: MaxValue,
                unit: Unit,
                usage: null,
                description: Description,
                wordSize: wordSize);

            Variable created = await _variableService.AddAsync(_dictionaryId, variable);
            _editingId = created.Id;
            _logger.LogInformation(
                "Created variable {VariableId} in dictionary {DictionaryId}",
                created.Id, _dictionaryId);
            _messageService.Show($"Variable '{Name}' created", MessageSeverity.Success);
        }
        else
        {
            var existing = Variable.Restore(
                id: _editingId!.Value,
                name: Name,
                addressHigh: addressHigh,
                addressLow: addressLow,
                dataTypeKind: SelectedDataTypeKind,
                dataTypeRaw: DataTypeForSave,
                dataTypeParam: dataTypeParam,
                accessMode: SelectedAccessMode,
                isEnabled: IsEnabled,
                format: Format,
                minValue: MinValue,
                maxValue: MaxValue,
                unit: Unit,
                usage: null,
                description: Description,
                wordSize: wordSize);

            await _variableService.UpdateAsync(existing);
            _messageService.Show($"Variable '{Name}' updated", MessageSeverity.Success);
        }
    }

    private async Task SaveCommonBitInterpretationsAsync()
    {
        if (SelectedDataTypeKind != DataTypeKind.Bitmapped)
        {
            return;
        }

        var bitsToSave = WordGroups
            .SelectMany(g => g.Items)
            .Select(b => new BitInterpretation(
                variableId: _editingId!.Value,
                wordIndex: b.WordIndex,
                bitIndex: b.BitIndex,
                meaning: b.Meaning,
                dictionaryId: null))
            .ToList();
        await _variableService.UpdateBitInterpretationsAsync(_editingId!.Value, bitsToSave);
    }

    private async Task SaveBitInterpretationsForDictionaryAsync()
    {
        if (SelectedDataTypeKind != DataTypeKind.Bitmapped)
        {
            return;
        }

        var bitsToSave = WordGroups
            .SelectMany(g => g.Items)
            .Select(b => new BitInterpretation(
                variableId: _editingId!.Value,
                wordIndex: b.WordIndex,
                bitIndex: b.BitIndex,
                meaning: b.Meaning,
                dictionaryId: _dictionaryContextId))
            .ToList();
        await _variableService.UpdateBitInterpretationsForDictionaryAsync(
            _editingId!.Value, _dictionaryContextId, bitsToSave);
    }

    private async Task SaveOverrideAsync()
    {
        if (_dictionaryContextId is null || _editingId is null)
        {
            return;
        }

        await _variableService.SetOverrideAsync(
            _dictionaryContextId.Value, _editingId.Value, IsEnabled, Description);
        _messageService.Show("Per-dictionary variable override saved", MessageSeverity.Success);
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        if (HasChanges)
        {
            DialogResult result = await _dialogService.ShowConfirmAsync(
                "Discard changes",
                "Are you sure you want to discard the changes?");
            if (result != DialogResult.Yes)
            {
                return;
            }
        }

        _navigationService.GoBack();
    }

    [RelayCommand]
    private void AddBitToWord(WordBitGroup? group)
    {
        if (group is null)
        {
            return;
        }

        if (group.TryAddBit())
        {
            BitInterpretationItem newItem = group.Items[^1];
            newItem.PropertyChanged += (_, _) => HasChanges = true;
            HasChanges = true;
        }
    }

    [RelayCommand]
    private void RemoveBitFromWord(BitInterpretationItem? item)
    {
        if (item is null)
        {
            return;
        }

        WordBitGroup? group = WordGroups.FirstOrDefault(g => g.WordIndex == item.WordIndex);
        if (group?.TryRemoveBit(item) == true)
        {
            HasChanges = true;
        }
    }

    [RelayCommand]
    private void RemoveLastBitFromWord(WordBitGroup? group)
    {
        if (group is null)
        {
            return;
        }

        if (group.TryRemoveLastBit())
        {
            HasChanges = true;
        }
    }

    /// <summary>
    /// Adds a new Word (WordBitGroup) with a single initial bit.
    /// </summary>
    [RelayCommand]
    private void AddWord()
    {
        var newGroup = new WordBitGroup(WordGroups.Count, SelectedWordSize ?? 16);
        newGroup.TryAddBit();
        newGroup.Items[0].PropertyChanged += (_, _) => HasChanges = true;
        WordGroups.Add(newGroup);
        OnPropertyChanged(nameof(CanRemoveWord));
        OnPropertyChanged(nameof(DataTypeForSave));
        HasChanges = true;
    }

    /// <summary>
    /// Removes a Word, asking for confirmation if it has non-empty meanings.
    /// </summary>
    [RelayCommand]
    private async Task RemoveWordAsync(WordBitGroup? group)
    {
        if (group is null || !CanRemoveWord)
        {
            return;
        }

        if (group.HasNonEmptyMeanings)
        {
            DialogResult result = await _dialogService.ShowConfirmAsync(
                "Remove Word",
                $"{group.Label} contains definitions. Are you sure you want to remove it?");
            if (result != DialogResult.Yes)
            {
                return;
            }
        }

        WordGroups.Remove(group);
        ReindexWordGroups();
        OnPropertyChanged(nameof(CanRemoveWord));
        OnPropertyChanged(nameof(DataTypeForSave));
        HasChanges = true;
    }

    /// <summary>
    /// Creates the initial Word 0 with a single bit.
    /// </summary>
    private void CreateInitialWordGroup()
    {
        var group = new WordBitGroup(0, SelectedWordSize ?? 16);
        group.TryAddBit();
        group.Items[0].PropertyChanged += (_, _) => HasChanges = true;
        WordGroups.Add(group);
        OnPropertyChanged(nameof(CanRemoveWord));
        OnPropertyChanged(nameof(DataTypeForSave));
    }

    /// <summary>
    /// Re-indexes WordGroups and their items after a removal.
    /// </summary>
    private void ReindexWordGroups()
    {
        for (int i = 0; i < WordGroups.Count; i++)
        {
            WordGroups[i].WordIndex = i;
            foreach (BitInterpretationItem item in WordGroups[i].Items)
            {
                item.WordIndex = i;
            }
        }
    }

    /// <summary>
    /// Regenerates WordGroups for the requested word count.
    /// Preserves existing items for words that already exist.
    /// </summary>
    private void RegenerateWordGroups(int wordCount, List<BitInterpretationItem>? existingItems = null)
    {
        existingItems ??= [.. WordGroups.SelectMany(g => g.Items)];

        WordGroups.Clear();

        for (int w = 0; w < wordCount; w++)
        {
            var group = new WordBitGroup(w, SelectedWordSize ?? 16);
            var wordItems = existingItems
                .Where(i => i.WordIndex == w)
                .OrderBy(i => i.BitIndex)
                .ToList();

            if (wordItems.Count > 0)
            {
                foreach (BitInterpretationItem? item in wordItems)
                {
                    item.PropertyChanged += (_, _) => HasChanges = true;
                    group.AddExisting(item);
                }
            }
            else
            {
                group.TryAddBit();
                group.Items[0].PropertyChanged += (_, _) => HasChanges = true;
            }

            WordGroups.Add(group);
        }

        OnPropertyChanged(nameof(CanRemoveWord));
        OnPropertyChanged(nameof(DataTypeForSave));
    }
}
