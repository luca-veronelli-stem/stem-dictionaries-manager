using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GUI.Windows.Abstractions;
using Services.Interfaces;

namespace GUI.Windows.ViewModels;

/// <summary>
/// ViewModel per la gestione stato variabili standard per un device specifico.
/// SESSION_035: DeviceType enum → int DeviceId.
/// </summary>
public partial class DeviceVariablesViewModel : ObservableObject, IEditableViewModel
{
    private readonly IVariableService _variableService;
    private readonly IDictionaryService _dictionaryService;
    private readonly IDeviceService _deviceService;
    private readonly INavigationService _navigationService;
    private readonly IMessageService _messageService;

    private int? _standardDictionaryId;

    [ObservableProperty]
    private int? _deviceId;

    [ObservableProperty]
    private string _deviceName = string.Empty;

    [ObservableProperty]
    private ObservableCollection<VariableDeviceItem> _variables = [];

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private VariableDeviceItem? _selectedVariable;

    public bool HasChanges => false;

    partial void OnSelectedVariableChanged(VariableDeviceItem? value)
    {
        if (value is { IsGloballyDisabled: true })
        {
            _messageService.Show(
                $"\"{value.Name}\" è disattivata globalmente. " +
                "Riattivarla su Dizionari → Standard → seleziona la variabile.",
                MessageSeverity.Warning, autoHideSeconds: 5);
        }
    }

    public DeviceVariablesViewModel(
        IVariableService variableService,
        IDictionaryService dictionaryService,
        IDeviceService deviceService,
        INavigationService navigationService,
        IMessageService messageService)
    {
        _variableService = variableService;
        _dictionaryService = dictionaryService;
        _deviceService = deviceService;
        _navigationService = navigationService;
        _messageService = messageService;
    }

    public async Task LoadAsync(int deviceId, string? deviceName = null)
    {
        DeviceId = deviceId;
        DeviceName = deviceName ?? (await _deviceService.GetByIdAsync(deviceId))?.Name ?? $"Device #{deviceId}";

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var standardDict = await _dictionaryService.GetStandardDictionaryAsync();
            if (standardDict is null)
            {
                ErrorMessage = "Nessun dizionario Standard trovato.";
                Variables = [];
                return;
            }

            _standardDictionaryId = standardDict.Id;

            var allVariables = await _variableService
                .GetByDictionaryIdAsync(standardDict.Id);

            var deviceStates = await _variableService
                .GetDeviceStatesForDeviceAsync(deviceId);
            var stateMap = deviceStates.ToDictionary(
                s => s.VariableId, s => s.IsEnabled);

            var items = allVariables
                .OrderBy(v => v.AddressLow)
                .Select(v =>
                {
                    var isGloballyDisabled = !v.IsEnabled;

                    bool effectiveEnabled;
                    if (isGloballyDisabled)
                    {
                        effectiveEnabled = false;
                    }
                    else if (stateMap.TryGetValue(v.Id, out var overrideEnabled))
                    {
                        effectiveEnabled = overrideEnabled;
                    }
                    else
                    {
                        effectiveEnabled = true;
                    }

                    return new VariableDeviceItem
                    {
                        VariableId = v.Id,
                        Name = v.Name,
                        FullAddress = $"0x{v.AddressHigh:X2}{v.AddressLow:X2}",
                        Description = v.Description ?? string.Empty,
                        DataTypeKind = v.DataTypeKind,
                        IsGloballyDisabled = isGloballyDisabled,
                        OriginalIsEnabled = effectiveEnabled,
                        IsEnabled = effectiveEnabled
                    };
                });

            Variables = new ObservableCollection<VariableDeviceItem>(items);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Errore: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void EditBitInterpretations(VariableDeviceItem? item)
    {
        if (item is null || DeviceId is null || _standardDictionaryId is null) return;

        _navigationService.NavigateTo(ViewType.VariableEdit, new NavigationParameter
        {
            EntityId = item.VariableId,
            ParentId = _standardDictionaryId.Value,
            DeviceId = DeviceId.Value
        });
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }
}
