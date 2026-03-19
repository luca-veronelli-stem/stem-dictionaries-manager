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
    private string _name = string.Empty;

    [ObservableProperty]
    private byte _addressHigh;

    [ObservableProperty]
    private byte _addressLow;

    [ObservableProperty]
    private DataTypeKind _selectedDataTypeKind = DataTypeKind.UInt8;

    [ObservableProperty]
    private string _dataTypeRaw = string.Empty;

    [ObservableProperty]
    private int? _dataTypeParam;

    [ObservableProperty]
    private AccessMode _selectedAccessMode = AccessMode.ReadOnly;

    [ObservableProperty]
    private string? _format;

    [ObservableProperty]
    private double? _minValue;

    [ObservableProperty]
    private double? _maxValue;

    [ObservableProperty]
    private string? _unit;

    [ObservableProperty]
    private string? _usage;

    [ObservableProperty]
    private string? _description;

    [ObservableProperty]
    private bool _isEnabled = true;

    // === Computed Properties ===

    public bool IsNew => _editingId is null;
    public string FormTitle => IsNew ? "Nuova Variabile" : "Modifica Variabile";
    public string FullAddressDisplay => $"0x{(AddressHigh << 8 | AddressLow):X4}";

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
        AddressHigh = v.AddressHigh;
        AddressLow = v.AddressLow;
        SelectedDataTypeKind = v.DataTypeKind;
        DataTypeRaw = v.DataTypeRaw;
        DataTypeParam = v.DataTypeParam;
        SelectedAccessMode = v.AccessMode;
        Format = v.Format;
        MinValue = v.MinValue;
        MaxValue = v.MaxValue;
        Unit = v.Unit;
        Usage = v.Usage;
        Description = v.Description;
        IsEnabled = v.IsEnabled;

        OnPropertyChanged(nameof(AddressHigh));
        OnPropertyChanged(nameof(AddressLow));
        OnPropertyChanged(nameof(FullAddressDisplay));
    }

    private bool CanSave() => !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(DataTypeRaw);

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        try
        {
            IsBusy = true;

            if (IsNew)
            {
                var variable = new Variable(
                    name: Name,
                    addressHigh: AddressHigh,
                    addressLow: AddressLow,
                    dataTypeKind: SelectedDataTypeKind,
                    accessMode: SelectedAccessMode,
                    dataTypeRaw: DataTypeRaw,
                    dataTypeParam: DataTypeParam,
                    isEnabled: IsEnabled,
                    format: Format,
                    minValue: MinValue,
                    maxValue: MaxValue,
                    unit: Unit,
                    usage: Usage,
                    description: Description);

                await _variableService.AddAsync(_dictionaryId, variable);
                _messageService.Show($"Variabile '{Name}' creata", MessageSeverity.Success);
            }
            else
            {
                var existing = Variable.Restore(
                    id: _editingId!.Value,
                    name: Name,
                    addressHigh: AddressHigh,
                    addressLow: AddressLow,
                    dataTypeKind: SelectedDataTypeKind,
                    dataTypeRaw: DataTypeRaw,
                    dataTypeParam: DataTypeParam,
                    accessMode: SelectedAccessMode,
                    isEnabled: IsEnabled,
                    format: Format,
                    minValue: MinValue,
                    maxValue: MaxValue,
                    unit: Unit,
                    usage: Usage,
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
