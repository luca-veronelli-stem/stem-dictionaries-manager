using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Enums;
using GUI.Windows.Abstractions;
using Services.Interfaces;

namespace GUI.Windows.ViewModels;

/// <summary>
/// ViewModel per la gestione stato variabili standard per un device specifico.
/// Mostra tutte le variabili del dizionario Standard con checkbox "Attivo" editabile.
/// Variabili deprecate (IsEnabled=false) mostrate con checkbox greyed out (BR-009/011).
/// </summary>
public partial class DeviceVariablesViewModel : ObservableObject, IEditableViewModel
{
    private readonly IVariableService _variableService;
    private readonly IDictionaryService _dictionaryService;
    private readonly INavigationService _navigationService;
    private readonly IMessageService _messageService;

    [ObservableProperty]
    private DeviceType? _deviceType;

    [ObservableProperty]
    private string _deviceName = string.Empty;

    [ObservableProperty]
    private ObservableCollection<VariableDeviceItem> _variables = [];

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    public bool HasChanges => Variables.Any(v => v.HasChanged);

    public DeviceVariablesViewModel(
        IVariableService variableService,
        IDictionaryService dictionaryService,
        INavigationService navigationService,
        IMessageService messageService)
    {
        _variableService = variableService;
        _dictionaryService = dictionaryService;
        _navigationService = navigationService;
        _messageService = messageService;
    }

    /// <summary>
    /// Carica tutte le variabili standard con il loro stato per il device specificato.
    /// </summary>
    public async Task LoadAsync(DeviceType deviceType)
    {
        DeviceType = deviceType;
        DeviceName = DeviceDetailViewModel.GetDeviceDisplayName(deviceType);

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            // Trova il dizionario Standard
            var standardDict = await _dictionaryService.GetStandardDictionaryAsync();
            if (standardDict is null)
            {
                ErrorMessage = "Nessun dizionario Standard trovato.";
                Variables = [];
                return;
            }

            // Carica variabili del dizionario Standard
            var allVariables = await _variableService
                .GetByDictionaryIdAsync(standardDict.Id);

            // Carica override per questo DeviceType
            var deviceStates = await _variableService
                .GetDeviceStatesForDeviceAsync(deviceType);
            var stateMap = deviceStates.ToDictionary(
                s => s.VariableId, s => s.IsEnabled);

            // Mappa: per ogni variabile, calcola stato effettivo (BR-009)
            var items = allVariables
                .OrderBy(v => v.AddressLow)
                .Select(v =>
                {
                    var isGloballyDisabled = !v.IsEnabled;

                    // BR-009: se globalmente disabilitata → false
                    // Se override presente → usa override
                    // Altrimenti → true (default attiva)
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
    private async Task SaveAsync()
    {
        var changedItems = Variables
            .Where(v => v.HasChanged && !v.IsGloballyDisabled)
            .ToList();

        if (changedItems.Count == 0)
        {
            _messageService.Show("Nessuna modifica da salvare.",
                MessageSeverity.Info, autoHideSeconds: 3);
            return;
        }

        try
        {
            foreach (var item in changedItems)
            {
                await _variableService.SetDeviceStateAsync(
                    item.VariableId, DeviceType!.Value, item.IsEnabled);
            }

            // Ricarica dopo il salvataggio
            if (DeviceType is not null)
                await LoadAsync(DeviceType.Value);

            _messageService.Show(
                $"Salvati {changedItems.Count} stati variabile.",
                MessageSeverity.Success, autoHideSeconds: 3);
        }
        catch (Exception ex)
        {
            _messageService.Show(
                $"Errore salvataggio: {ex.Message}",
                MessageSeverity.Error, autoHideSeconds: 0);
        }
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }
}
