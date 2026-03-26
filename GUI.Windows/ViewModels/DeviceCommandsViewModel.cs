using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Enums;
using GUI.Windows.Abstractions;
using Services.Interfaces;

namespace GUI.Windows.ViewModels;

/// <summary>
/// ViewModel per la gestione stato comandi per un device specifico.
/// Mostra tutti i comandi con checkbox "Attivo" editabile.
/// </summary>
public partial class DeviceCommandsViewModel : ObservableObject, IEditableViewModel
{
    private readonly ICommandService _commandService;
    private readonly INavigationService _navigationService;
    private readonly IMessageService _messageService;

    [ObservableProperty]
    private DeviceType? _deviceType;

    [ObservableProperty]
    private string _deviceName = string.Empty;

    [ObservableProperty]
    private ObservableCollection<CommandDeviceItem> _commands = [];

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    public bool HasChanges => Commands.Any(c => c.HasChanged);

    public DeviceCommandsViewModel(
        ICommandService commandService,
        INavigationService navigationService,
        IMessageService messageService)
    {
        _commandService = commandService;
        _navigationService = navigationService;
        _messageService = messageService;
    }

    /// <summary>
    /// Carica tutti i comandi con il loro stato per il device specificato.
    /// </summary>
    public async Task LoadAsync(DeviceType deviceType)
    {
        DeviceType = deviceType;
        DeviceName = DeviceDetailViewModel.GetDeviceDisplayName(deviceType);

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var allCommands = await _commandService.GetAllAsync();
            var deviceStates = await _commandService.GetDeviceStatesForDeviceAsync(deviceType);

            // Mappa: per ogni comando, lo stato è override se presente, altrimenti true (default)
            var stateMap = deviceStates.ToDictionary(s => s.CommandId, s => s.IsEnabled);

            var items = allCommands
                .OrderBy(c => c.CodeLow)
                .ThenBy(c => c.IsResponse)
                .Select(c =>
                {
                    var isEnabled = stateMap.TryGetValue(c.Id, out var overrideEnabled)
                        ? overrideEnabled
                        : true;

                    return new CommandDeviceItem
                    {
                        CommandId = c.Id,
                        Name = c.Name,
                        FullCode = $"0x{c.CodeHigh:X2}{c.CodeLow:X2}",
                        TypeDisplay = c.IsResponse ? "Risposta" : "Comando",
                        OriginalIsEnabled = isEnabled,
                        IsEnabled = isEnabled
                    };
                });

            Commands = new ObservableCollection<CommandDeviceItem>(items);
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
        var changedItems = Commands.Where(c => c.HasChanged).ToList();

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
                await _commandService.SetDeviceStateAsync(
                    item.CommandId, DeviceType!.Value, item.IsEnabled);
            }

            // Aggiorna stato originale dopo il salvataggio
            if (DeviceType is not null)
                await LoadAsync(DeviceType.Value);

            _messageService.Show(
                $"Salvati {changedItems.Count} stati comando.",
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
