using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models;
using GUI.Windows.Abstractions;
using Microsoft.Extensions.Logging;
using Services.Interfaces;

namespace GUI.Windows.ViewModels;

/// <summary>
/// ViewModel for managing per-device command-state for a specific device.
/// SESSION_035: DeviceType enum → int DeviceId.
/// </summary>
public partial class DeviceCommandsViewModel : ObservableObject, IEditableViewModel
{
    private readonly ICommandService _commandService;
    private readonly IDeviceService _deviceService;
    private readonly INavigationService _navigationService;
    private readonly IMessageService _messageService;
    private readonly ILogger<DeviceCommandsViewModel> _logger;

    [ObservableProperty]
    private int? _deviceId;

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
        IDeviceService deviceService,
        INavigationService navigationService,
        IMessageService messageService,
        ILogger<DeviceCommandsViewModel> logger)
    {
        _commandService = commandService;
        _deviceService = deviceService;
        _navigationService = navigationService;
        _messageService = messageService;
        _logger = logger;
    }

    public async Task LoadAsync(int deviceId, string? deviceName = null)
    {
        DeviceId = deviceId;
        DeviceName = deviceName ?? (await _deviceService.GetByIdAsync(deviceId))?.Name ?? $"Device #{deviceId}";

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            IReadOnlyList<Command> allCommands = await _commandService.GetAllAsync();
            IReadOnlyList<CommandDeviceState> deviceStates = await _commandService.GetDeviceStatesForDeviceAsync(deviceId);

            var stateMap = deviceStates.ToDictionary(s => s.CommandId, s => s.IsEnabled);

            IEnumerable<CommandDeviceItem> items = allCommands
                .OrderBy(c => c.CodeLow)
                .ThenBy(c => c.IsResponse)
                .Select(c =>
                {
                    bool isEnabled = !stateMap.TryGetValue(c.Id, out bool overrideEnabled) || overrideEnabled;

                    return new CommandDeviceItem
                    {
                        CommandId = c.Id,
                        Name = c.Name,
                        FullCode = $"0x{c.CodeHigh:X2}{c.CodeLow:X2}",
                        TypeDisplay = c.IsResponse ? "Response" : "Command",
                        OriginalIsEnabled = isEnabled,
                        IsEnabled = isEnabled
                    };
                });

            Commands = new ObservableCollection<CommandDeviceItem>(items);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
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
            _messageService.Show("No changes to save.",
                MessageSeverity.Info, autoHideSeconds: 3);
            return;
        }

        try
        {
            foreach (CommandDeviceItem? item in changedItems)
            {
                await _commandService.SetDeviceStateAsync(
                    item.CommandId, DeviceId!.Value, item.IsEnabled);
            }

            if (DeviceId is not null)
            {
                await LoadAsync(DeviceId.Value, DeviceName);
            }

            _logger.LogInformation(
                "Saved {Count} command states for device {DeviceId}",
                changedItems.Count, DeviceId);
            _messageService.Show(
                $"Saved {changedItems.Count} command states.",
                MessageSeverity.Success, autoHideSeconds: 3);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save command states for device {DeviceId}", DeviceId);
            _messageService.Show(
                $"Save error: {ex.Message}",
                MessageSeverity.Error, autoHideSeconds: 0);
        }
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }
}
