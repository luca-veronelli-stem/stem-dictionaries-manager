using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Enums;
using Core.Models;
using GUI.Windows.Abstractions;
using Services.Interfaces;

namespace GUI.Windows.ViewModels;

/// <summary>
/// ViewModel per la creazione/modifica di una variabile.
/// </summary>
public partial class VariableEditViewModel : ObservableObject
{
    private readonly IVariableService _variableService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private readonly IMessageService _messageService;

    private int? _editingId;
    private int _dictionaryId;
    private bool _isInitialized;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _hasChanges;

    // === Campi editabili ===

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FullAddressDisplay))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _name = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FullAddressDisplay))]
    [NotifyPropertyChangedFor(nameof(IsAddressHighValid))]
    private string _addressHighHex = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FullAddressDisplay))]
    [NotifyPropertyChangedFor(nameof(IsAddressLowValid))]
    private string _addressLowHex = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDataTypeOther))]
    [NotifyPropertyChangedFor(nameof(RequiresDataTypeParam))]
    [NotifyPropertyChangedFor(nameof(IsBitmapped))]
    [NotifyPropertyChangedFor(nameof(DataTypeParamLabel))]
    [NotifyPropertyChangedFor(nameof(DataTypeForSave))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private DataTypeKind _selectedDataTypeKind = DataTypeKind.UInt8;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DataTypeForSave))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _customDataType = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private int? _dataTypeParam;

    [ObservableProperty]
    private AccessMode _selectedAccessMode = AccessMode.ReadOnly;

    [ObservableProperty]
    private string? _format;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMinMaxValid))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private double? _minValue;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMinMaxValid))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private double? _maxValue;

    [ObservableProperty]
    private string? _unit;

    [ObservableProperty]
    private string? _description;

    [ObservableProperty]
    private bool _isEnabled = true;

    // === Computed Properties ===

    public bool IsNew => _editingId is null;
    public string FormTitle => IsNew ? "Nuova Variabile" : "Modifica Variabile";

    /// <summary>
    /// True se il DataTypeKind selezionato è Other (mostra TextBox custom).
    /// </summary>
    public bool IsDataTypeOther => SelectedDataTypeKind == DataTypeKind.Other;

    /// <summary>
    /// True se il tipo richiede un parametro (Bitmapped, Array, String).
    /// </summary>
    public bool RequiresDataTypeParam => SelectedDataTypeKind is DataTypeKind.Bitmapped or DataTypeKind.Array or DataTypeKind.String;

    /// <summary>
    /// True se è Bitmapped (mostra Word Count / Word Size).
    /// </summary>
    public bool IsBitmapped => SelectedDataTypeKind == DataTypeKind.Bitmapped;

    /// <summary>
    /// Label per il parametro tipo (con asterisco).
    /// </summary>
    public string DataTypeParamLabel => SelectedDataTypeKind switch
    {
        DataTypeKind.Bitmapped => "Word Count (16 bit) *",
        DataTypeKind.Array or DataTypeKind.String => "Size (bytes) *",
        _ => "Parametro"
    };

    /// <summary>
    /// Tipo dato per il salvataggio (dal dropdown o custom).
    /// </summary>
    public string DataTypeForSave => IsDataTypeOther ? CustomDataType : SelectedDataTypeKind.ToString();

    /// <summary>
    /// Validazione: AddressHigh deve essere hex valido.
    /// </summary>
    public bool IsAddressHighValid => IsValidHex(AddressHighHex);

    /// <summary>
    /// Validazione: AddressLow deve essere hex valido.
    /// </summary>
    public bool IsAddressLowValid => IsValidHex(AddressLowHex);

    /// <summary>
    /// Validazione: Min deve essere minore o uguale a Max (se entrambi specificati).
    /// </summary>
    public bool IsMinMaxValid => !MinValue.HasValue || !MaxValue.HasValue || MinValue.Value <= MaxValue.Value;

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
        INavigationService navigationService,
        IDialogService dialogService,
        IMessageService messageService)
    {
        _variableService = variableService;
        _navigationService = navigationService;
        _dialogService = dialogService;
        _messageService = messageService;
    }

    /// <summary>
    /// Inizializza il ViewModel.
    /// </summary>
    public async Task InitializeAsync(int? variableId, int dictionaryId)
    {
        if (_isInitialized) return;

        try
        {
            IsBusy = true;
            _editingId = variableId;
            _dictionaryId = dictionaryId;

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
            }

            _isInitialized = true;
            HasChanges = false;

            OnPropertyChanged(nameof(IsNew));
            OnPropertyChanged(nameof(FormTitle));
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

    private void LoadFromVariable(Variable v)
    {
        Name = v.Name;
        AddressHighHex = v.AddressHigh.ToString("X2");
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

    private bool CanSave() => 
        !string.IsNullOrWhiteSpace(Name) && 
        !string.IsNullOrWhiteSpace(DataTypeForSave) &&
        IsAddressHighValid &&
        IsAddressLowValid &&
        IsMinMaxValid &&
        (!RequiresDataTypeParam || DataTypeParam.HasValue);

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        try
        {
            IsBusy = true;

            var addressHigh = ParseHexByte(AddressHighHex);
            var addressLow = ParseHexByte(AddressLowHex);

            if (IsNew)
            {
                var variable = new Variable(
                    name: Name,
                    addressHigh: addressHigh,
                    addressLow: addressLow,
                    dataTypeKind: SelectedDataTypeKind,
                    accessMode: SelectedAccessMode,
                    dataTypeRaw: DataTypeForSave,
                    dataTypeParam: DataTypeParam,
                    isEnabled: IsEnabled,
                    format: Format,
                    minValue: MinValue,
                    maxValue: MaxValue,
                    unit: Unit,
                    usage: null,
                    description: Description);

                await _variableService.AddAsync(_dictionaryId, variable);
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
                    dataTypeParam: DataTypeParam,
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

    [RelayCommand]
    private async Task CancelAsync()
    {
        if (HasChanges)
        {
            var result = await _dialogService.ShowConfirmAsync(
                "Conferma",
                "Ci sono modifiche non salvate. Uscire comunque?");
            if (result != DialogResult.Yes) return;
        }

        _navigationService.GoBack();
    }
}
