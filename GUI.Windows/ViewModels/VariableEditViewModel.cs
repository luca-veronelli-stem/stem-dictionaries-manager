using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Enums;
using Core.Models;
using GUI.Windows.Abstractions;
using Services.Interfaces;
using System.Collections.ObjectModel;

namespace GUI.Windows.ViewModels;

/// <summary>
/// ViewModel per la creazione/modifica di una variabile.
/// </summary>
public partial class VariableEditViewModel : ObservableObject, IEditableViewModel
{
    private readonly IVariableService _variableService;
    private readonly IDictionaryService _dictionaryService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private readonly IMessageService _messageService;

    private int? _editingId;
    private int _dictionaryId;
    private bool _isStandardDictionary;
    private bool _isInitialized;
    private bool _isLoading;
    private bool _showValidation;

    /// <summary>
    /// Se valorizzato, la view è in modalità DeviceContext: campi variabile read-only,
    /// WordGroups editabili con DeviceId. Null = modalità Normal.
    /// </summary>
    private int? _deviceContextId;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _hasChanges;

    // === Campi editabili ===

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FullAddressDisplay))]
    [NotifyPropertyChangedFor(nameof(IsNameInvalid))]
    private string _name = string.Empty;

    /// <summary>
    /// AddressHigh calcolato automaticamente dal tipo di dizionario.
    /// 0x00 = Dizionario Standard, 0x80 = altri dizionari.
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

    /// <summary>
    /// Notifica validazione quando cambia il DataTypeParam (Size per Array/String).
    /// </summary>
    partial void OnDataTypeParamChanged(int? value)
    {
        OnPropertyChanged(nameof(IsDataTypeParamInvalid));
    }

    /// <summary>
    /// Quando cambia il tipo dato: se Bitmapped, crea Word 0 automaticamente.
    /// </summary>
    partial void OnSelectedDataTypeKindChanged(DataTypeKind value)
    {
        if (_isLoading) return;

        if (value == DataTypeKind.Bitmapped)
        {
            if (WordGroups.Count == 0)
                CreateInitialWordGroup();
        }
        else
        {
            WordGroups.Clear();
            OnPropertyChanged(nameof(CanRemoveWord));
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
    /// Gruppi di bit per word (solo per DataType == Bitmapped).
    /// Ogni WordBitGroup contiene max 16 BitInterpretationItem.
    /// </summary>
    public ObservableCollection<WordBitGroup> WordGroups { get; } = [];

    // === Computed Properties ===

    public bool IsNew => _editingId is null;

    /// <summary>
    /// True se in modalità DeviceContext (campi variabile read-only, solo bit editabili).
    /// </summary>
    public bool IsDeviceContext => _deviceContextId.HasValue;

    /// <summary>
    /// True se i campi variabile sono editabili (modalità Normal).
    /// </summary>
    public bool IsNotDeviceContext => !IsDeviceContext;

    public string FormTitle => IsDeviceContext
        ? "Interpretazione Bit (Device)"
        : IsNew ? "Nuova Variabile" : "Modifica Variabile";

    public string SaveButtonLabel => IsDeviceContext
        ? "?? Salva Bit" : "?? Salva";

    /// <summary>
    /// True se il DataTypeKind selezionato è Other (mostra TextBox custom).
    /// </summary>
    public bool IsDataTypeOther => SelectedDataTypeKind == DataTypeKind.Other;

    /// <summary>
    /// True se il tipo richiede un parametro numerico (solo Array, String).
    /// Bitmapped usa WordGroups.Count al posto del TextBox.
    /// </summary>
    public bool RequiresDataTypeParam => SelectedDataTypeKind is DataTypeKind.Array or DataTypeKind.String;

    /// <summary>
    /// True se è Bitmapped (mostra Word Count / Word Size).
    /// </summary>
    public bool IsBitmapped => SelectedDataTypeKind == DataTypeKind.Bitmapped;

    /// <summary>
    /// Label per il parametro tipo (con asterisco).
    /// </summary>
    public string DataTypeParamLabel => SelectedDataTypeKind switch
    {
        DataTypeKind.Array or DataTypeKind.String => "Size (bytes) *",
        _ => "Parametro"
    };

    /// <summary>
    /// Tipo dato per il salvataggio (dal dropdown o custom).
    /// Bitmapped usa WordGroups.Count come parametro.
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

            var baseName = SelectedDataTypeKind.ToString();

            // Bitmapped: parametro derivato da WordGroups.Count
            if (IsBitmapped && WordGroups.Count > 0)
                return $"{baseName}[{WordGroups.Count}]";

            // Array/String: parametro dal TextBox
            if (RequiresDataTypeParam && DataTypeParam.HasValue)
                return $"{baseName}[{DataTypeParam.Value}]";

            return baseName;
        }
    }

    /// <summary>
    /// Validazione: AddressLow deve essere hex valido.
    /// </summary>
    public bool IsAddressLowValid => IsValidHex(AddressLowHex);

    /// <summary>
    /// Validazione: Min deve essere minore o uguale a Max (se entrambi specificati).
    /// </summary>
    public bool IsMinMaxValid => !MinValue.HasValue || !MaxValue.HasValue || MinValue.Value <= MaxValue.Value;

    // === Proprietà di validazione per-campo (visibili solo dopo primo tentativo di salvataggio) ===

    public bool IsNameInvalid => _showValidation && string.IsNullOrWhiteSpace(Name);
    public bool IsAddressLowInvalid => _showValidation && string.IsNullOrWhiteSpace(AddressLowHex);
    public bool IsDescriptionInvalid => _showValidation && string.IsNullOrWhiteSpace(Description);
    public bool IsDataTypeParamInvalid => _showValidation && RequiresDataTypeParam && !DataTypeParam.HasValue;

    /// <summary>
    /// True se ci sono almeno 2 word (la rimozione è possibile).
    /// </summary>
    public bool CanRemoveWord => IsBitmapped && WordGroups.Count > 1;
    public bool IsCustomDataTypeInvalid => _showValidation && IsDataTypeOther && string.IsNullOrWhiteSpace(CustomDataType);

    /// <summary>
    /// Indirizzo completo in formato 0xHHLL.
    /// </summary>
    public string FullAddressDisplay
    {
        get
        {
            var high = ParseHexByte(AddressHighHex);
            var low = ParseHexByte(AddressLowHex);
            return $"0x{(high << 8 | low):X4}";
        }
    }

    // === Helper per validazione e parsing hex ===

    private static bool IsValidHex(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return true; // vuoto è ok
        return hex.All(c => char.IsAsciiHexDigit(c));
    }

    private static byte ParseHexByte(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return 0;
        if (byte.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var result))
            return result;
        return 0;
    }

    /// <summary>
    /// Filtra l'input per accettare solo caratteri hex.
    /// </summary>
    public static string FilterHexInput(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return new string([.. input.Where(char.IsAsciiHexDigit)]).ToUpperInvariant();
    }

    // === Enum values per ComboBox ===

    public IReadOnlyList<DataTypeKind> DataTypeKinds { get; } = Enum.GetValues<DataTypeKind>();
    public IReadOnlyList<AccessMode> AccessModes { get; } = Enum.GetValues<AccessMode>();

    public VariableEditViewModel(
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
    /// Inizializza il ViewModel.
    /// deviceId != null ? DeviceContext mode (read-only + bit editabili per device).
    /// </summary>
    public async Task InitializeAsync(int? variableId, int dictionaryId, int? deviceId = null)
    {
        if (_isInitialized) return;

        try
        {
            IsBusy = true;
            _isLoading = true;
            _editingId = variableId;
            _dictionaryId = dictionaryId;
            _deviceContextId = deviceId;

            // Carica il dizionario per determinare AddressHigh
            var dictionary = await _dictionaryService.GetByIdAsync(dictionaryId);
            _isStandardDictionary = dictionary?.IsStandard ?? false;
            OnPropertyChanged(nameof(AddressHighHex));
            OnPropertyChanged(nameof(FullAddressDisplay));
            OnPropertyChanged(nameof(IsDeviceContext));
            OnPropertyChanged(nameof(IsNotDeviceContext));

            if (variableId.HasValue)
            {
                var variable = await _variableService.GetByIdAsync(variableId.Value);
                if (variable is null)
                {
                    await _dialogService.ShowErrorAsync("Errore", "Variabile non trovata.");
                    _navigationService.GoBack();
                    return;
                }

                LoadFromVariable(variable);

                // DeviceContext: sovrascrive IsEnabled con lo stato per device
                if (_deviceContextId.HasValue)
                {
                    var deviceState = await _variableService.GetDeviceStateAsync(
                        variableId.Value, _deviceContextId.Value);
                    // Se esiste override ? usa quello, altrimenti default = true
                    IsEnabled = deviceState?.IsEnabled ?? true;
                }

                if (variable.DataTypeKind == DataTypeKind.Bitmapped)
                {
                    // DeviceContext: carica interpretazioni per device (con fallback a comuni)
                    // Normal: carica tutte le interpretazioni comuni
                    var bits = _deviceContextId.HasValue
                        ? await _variableService.GetBitInterpretationsForDeviceAsync(
                            variableId.Value, _deviceContextId.Value)
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

            // Se nuovo e tipo è Bitmapped (improbabile ma safe), crea Word 0
            if (IsBitmapped && WordGroups.Count == 0)
                CreateInitialWordGroup();

            OnPropertyChanged(nameof(IsNew));
            OnPropertyChanged(nameof(FormTitle));
            OnPropertyChanged(nameof(CanRemoveWord));
        }
        catch (Exception ex)
        {
            _isLoading = false;
            ErrorMessage = ex.Message;
            await _dialogService.ShowErrorAsync("Errore", $"Impossibile caricare: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void LoadFromVariable(Variable v)
    {
        Name = v.Name;
        // AddressHighHex è computed automaticamente da _isStandardDictionary
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

        // Imposta custom type se è Other
        if (v.DataTypeKind == DataTypeKind.Other)
        {
            CustomDataType = v.DataTypeRaw;
        }
    }

    private bool Validate()
    {
        _showValidation = true;

        // Notifica tutte le proprietà di validazione
        OnPropertyChanged(nameof(IsNameInvalid));
        OnPropertyChanged(nameof(IsAddressLowInvalid));
        OnPropertyChanged(nameof(IsDescriptionInvalid));
        OnPropertyChanged(nameof(IsDataTypeParamInvalid));
        OnPropertyChanged(nameof(IsCustomDataTypeInvalid));

        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(Name)) missing.Add("Nome");
        if (string.IsNullOrWhiteSpace(AddressLowHex)) missing.Add("Indirizzo");
        if (string.IsNullOrWhiteSpace(Description)) missing.Add("Descrizione");
        if (RequiresDataTypeParam && !DataTypeParam.HasValue) missing.Add(DataTypeParamLabel.TrimEnd(' ', '*'));
        if (IsDataTypeOther && string.IsNullOrWhiteSpace(CustomDataType)) missing.Add("Tipo dato custom");
        if (!IsAddressLowValid) missing.Add("Indirizzo (formato hex non valido)");
        if (!IsMinMaxValid) missing.Add("Min/Max (Min deve essere ? Max)");

        if (missing.Count > 0)
        {
            _messageService.Show($"Campi obbligatori mancanti: {string.Join(", ", missing)}", MessageSeverity.Warning);
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

            if (IsDeviceContext)
            {
                // DeviceContext: salva stato variabile per device + bit interpretations
                await SaveDeviceStateAsync();
                await SaveBitInterpretationsForDeviceAsync();
            }
            else
            {
                // Normal: salva variabile + bit comuni
                if (!Validate()) return;
                await SaveVariableAsync();
                await SaveCommonBitInterpretationsAsync();
            }

            HasChanges = false;
            _navigationService.GoBack();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Errore", $"Impossibile salvare: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SaveVariableAsync()
    {
        var addressHigh = ParseHexByte(AddressHighHex);
        var addressLow = ParseHexByte(AddressLowHex);
        var dataTypeParam = IsBitmapped ? WordGroups.Count : DataTypeParam;

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
                description: Description);

            var created = await _variableService.AddAsync(_dictionaryId, variable);
            _editingId = created.Id;
            _messageService.Show($"Variabile '{Name}' creata", MessageSeverity.Success);
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
                description: Description);

            await _variableService.UpdateAsync(existing);
            _messageService.Show($"Variabile '{Name}' aggiornata", MessageSeverity.Success);
        }
    }

    private async Task SaveCommonBitInterpretationsAsync()
    {
        if (SelectedDataTypeKind != DataTypeKind.Bitmapped) return;

        var bitsToSave = WordGroups
            .SelectMany(g => g.Items)
            .Select(b => new BitInterpretation(
                variableId: _editingId!.Value,
                wordIndex: b.WordIndex,
                bitIndex: b.BitIndex,
                meaning: b.Meaning,
                deviceId: null))
            .ToList();
        await _variableService.UpdateBitInterpretationsAsync(_editingId!.Value, bitsToSave);
    }

    private async Task SaveBitInterpretationsForDeviceAsync()
    {
        if (SelectedDataTypeKind != DataTypeKind.Bitmapped) return;

        var bitsToSave = WordGroups
            .SelectMany(g => g.Items)
            .Select(b => new BitInterpretation(
                variableId: _editingId!.Value,
                wordIndex: b.WordIndex,
                bitIndex: b.BitIndex,
                meaning: b.Meaning,
                deviceId: _deviceContextId))
            .ToList();
        await _variableService.UpdateBitInterpretationsForDeviceAsync(
            _editingId!.Value, _deviceContextId, bitsToSave);
    }

    private async Task SaveDeviceStateAsync()
    {
        if (_deviceContextId is null || _editingId is null) return;
        await _variableService.SetDeviceStateAsync(
            _editingId.Value, _deviceContextId.Value, IsEnabled);
        _messageService.Show("Stato variabile per device salvato", MessageSeverity.Success);
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        if (HasChanges)
        {
            var result = await _dialogService.ShowConfirmAsync(
                "Annulla modifiche",
                "Sei sicuro di voler annullare le modifiche?");
            if (result != DialogResult.Yes) return;
        }

        _navigationService.GoBack();
    }

    [RelayCommand]
    private void AddBitToWord(WordBitGroup? group)
    {
        if (group is null) return;
        if (group.TryAddBit())
        {
            var newItem = group.Items[^1];
            newItem.PropertyChanged += (_, _) => HasChanges = true;
            HasChanges = true;
        }
    }

    [RelayCommand]
    private void RemoveBitFromWord(BitInterpretationItem? item)
    {
        if (item is null) return;
        var group = WordGroups.FirstOrDefault(g => g.WordIndex == item.WordIndex);
        if (group?.TryRemoveBit(item) == true)
            HasChanges = true;
    }

    [RelayCommand]
    private void RemoveLastBitFromWord(WordBitGroup? group)
    {
        if (group is null) return;
        if (group.TryRemoveLastBit())
            HasChanges = true;
    }

    /// <summary>
    /// Aggiunge una nuova Word (WordBitGroup) con 1 bit iniziale.
    /// </summary>
    [RelayCommand]
    private void AddWord()
    {
        var newGroup = new WordBitGroup(WordGroups.Count);
        newGroup.TryAddBit();
        newGroup.Items[0].PropertyChanged += (_, _) => HasChanges = true;
        WordGroups.Add(newGroup);
        OnPropertyChanged(nameof(CanRemoveWord));
        OnPropertyChanged(nameof(DataTypeForSave));
        HasChanges = true;
    }

    /// <summary>
    /// Rimuove una Word con conferma se contiene meanings non vuoti.
    /// </summary>
    [RelayCommand]
    private async Task RemoveWordAsync(WordBitGroup? group)
    {
        if (group is null || !CanRemoveWord) return;

        if (group.HasNonEmptyMeanings)
        {
            var result = await _dialogService.ShowConfirmAsync(
                "Rimuovi Word",
                $"La {group.Label} contiene definizioni. Sei sicuro di volerla rimuovere?");
            if (result != DialogResult.Yes) return;
        }

        WordGroups.Remove(group);
        ReindexWordGroups();
        OnPropertyChanged(nameof(CanRemoveWord));
        OnPropertyChanged(nameof(DataTypeForSave));
        HasChanges = true;
    }

    /// <summary>
    /// Crea la Word 0 iniziale con 1 bit.
    /// </summary>
    private void CreateInitialWordGroup()
    {
        var group = new WordBitGroup(0);
        group.TryAddBit();
        group.Items[0].PropertyChanged += (_, _) => HasChanges = true;
        WordGroups.Add(group);
        OnPropertyChanged(nameof(CanRemoveWord));
        OnPropertyChanged(nameof(DataTypeForSave));
    }

    /// <summary>
    /// Re-indicizza i WordGroups e i loro items dopo una rimozione.
    /// </summary>
    private void ReindexWordGroups()
    {
        for (var i = 0; i < WordGroups.Count; i++)
        {
            WordGroups[i].WordIndex = i;
            foreach (var item in WordGroups[i].Items)
                item.WordIndex = i;
        }
    }

    /// <summary>
    /// Rigenera i WordGroups per il wordCount specificato.
    /// Preserva gli items esistenti per le word che già esistono.
    /// </summary>
    private void RegenerateWordGroups(int wordCount, List<BitInterpretationItem>? existingItems = null)
    {
        existingItems ??= [.. WordGroups.SelectMany(g => g.Items)];

        WordGroups.Clear();

        for (var w = 0; w < wordCount; w++)
        {
            var group = new WordBitGroup(w);
            var wordItems = existingItems
                .Where(i => i.WordIndex == w)
                .OrderBy(i => i.BitIndex)
                .ToList();

            if (wordItems.Count > 0)
            {
                foreach (var item in wordItems)
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
