using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models;
using GUI.Windows.Abstractions;
using Microsoft.Extensions.Logging;
using Services.Interfaces;

namespace GUI.Windows.ViewModels;

/// <summary>
/// ViewModel for creating/editing a device.
/// Fields: Name, MachineCode, Description.
/// </summary>
public partial class DeviceEditViewModel : ObservableObject, IEditableViewModel
{
    private readonly IDeviceService _deviceService;
    private readonly IBoardService _boardService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private readonly IMessageService _messageService;
    private readonly ILogger<DeviceEditViewModel> _logger;

    private int? _editingId;
    private bool _showValidation;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _hasChanges;

    // === Editable fields ===

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNameInvalid))]
    private string _name = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMachineCodeInvalid))]
    private string _machineCode = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    /// <summary>
    /// Informational hint shown under the MachineCode field (visible only on create).
    /// </summary>
    [ObservableProperty]
    private string? _machineCodeHint;

    // === Validation ===

    public bool IsNameInvalid => _showValidation && string.IsNullOrWhiteSpace(Name);
    public bool IsMachineCodeInvalid => _showValidation
        && (!int.TryParse(MachineCode, out int code) || code <= 0);
    public bool IsNew => _editingId is null;

    public DeviceEditViewModel(
        IDeviceService deviceService,
        IBoardService boardService,
        INavigationService navigationService,
        IDialogService dialogService,
        IMessageService messageService,
        ILogger<DeviceEditViewModel> logger)
    {
        _deviceService = deviceService;
        _boardService = boardService;
        _navigationService = navigationService;
        _dialogService = dialogService;
        _messageService = messageService;
        _logger = logger;
    }

    public async Task InitializeAsync(int? deviceId)
    {
        if (deviceId is not null)
        {
            Device? device = await _deviceService.GetByIdAsync(deviceId.Value);
            if (device is null)
            {
                ErrorMessage = $"Device #{deviceId} not found.";
                return;
            }

            _editingId = device.Id;
            Name = device.Name;
            MachineCode = device.MachineCode.ToString();
            Description = device.Description ?? string.Empty;
        }
        else
        {
            // Pre-fill with the first available MachineCode
            int nextCode = await _deviceService.GetNextAvailableMachineCodeAsync();
            MachineCode = nextCode.ToString();
            MachineCodeHint = $"First available value suggested ({nextCode})";
        }

        HasChanges = false;
    }

    partial void OnNameChanged(string value) => HasChanges = true;
    partial void OnMachineCodeChanged(string value) => HasChanges = true;
    partial void OnDescriptionChanged(string value) => HasChanges = true;

    private bool Validate()
    {
        _showValidation = true;
        OnPropertyChanged(nameof(IsNameInvalid));
        OnPropertyChanged(nameof(IsMachineCodeInvalid));

        if (IsNameInvalid || IsMachineCodeInvalid)
        {
            _messageService.Show("Fill in all required fields.",
                MessageSeverity.Warning);
            return false;
        }

        return true;
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

            string? desc = string.IsNullOrWhiteSpace(Description) ? null : Description;
            int code = int.Parse(MachineCode);

            if (_editingId is null)
            {
                var device = new Device(Name.Trim(), code, desc);
                await _deviceService.AddAsync(device);
                _logger.LogInformation("Created device with machine code {MachineCode}", code);
                _messageService.Show($"Device '{Name}' created.",
                    MessageSeverity.Success, autoHideSeconds: 3);
            }
            else
            {
                var device = Device.Restore(_editingId.Value, Name.Trim(),
                    code, desc);
                await _deviceService.UpdateAsync(device);
                _messageService.Show($"Device '{Name}' updated.",
                    MessageSeverity.Success, autoHideSeconds: 3);
            }

            HasChanges = false;
            _navigationService.GoBack();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save device with machine code {MachineCode}", MachineCode);
            _messageService.Show($"Save error: {ex.Message}",
                MessageSeverity.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteDeviceAsync()
    {
        if (_editingId is null)
        {
            return;
        }

        // Compute counts for the warning
        IReadOnlyList<Board> boards = await _boardService.GetByDeviceIdAsync(_editingId.Value);
        int boardCount = boards.Count;
        int dictCount = boards
            .Where(b => b.DictionaryId.HasValue)
            .Select(b => b.DictionaryId!.Value)
            .Distinct()
            .Count();

        string message = $"Delete device '{Name}'?\n" +
            $"{boardCount} boards will be deleted";
        if (dictCount > 0)
        {
            message += $" along with the associated dedicated dictionaries";
        }

        message += ".";

        DialogResult result = await _dialogService.ShowConfirmAsync(
            "Delete device", message);

        if (result != Abstractions.DialogResult.Yes)
        {
            return;
        }

        try
        {
            await _deviceService.DeleteAsync(_editingId.Value);
            HasChanges = false;
            _messageService.Show($"Device '{Name}' deleted.",
                MessageSeverity.Success, autoHideSeconds: 3);
            _navigationService.GoBack();
        }
        catch (Exception ex)
        {
            _messageService.Show($"Delete error: {ex.Message}",
                MessageSeverity.Error);
        }
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        if (HasChanges)
        {
            DialogResult result = await _dialogService.ShowConfirmAsync(
                "Discard changes",
                "There are unsaved changes. Exit without saving?");
            if (result != DialogResult.Yes)
            {
                return;
            }
        }

        _navigationService.GoBack();
    }
}
