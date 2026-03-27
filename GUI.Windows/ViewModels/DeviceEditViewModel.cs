using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models;
using GUI.Windows.Abstractions;
using Services.Interfaces;

namespace GUI.Windows.ViewModels;

/// <summary>
/// ViewModel per la creazione/modifica di un dispositivo.
/// Campi: Name, MachineCode, Description.
/// </summary>
public partial class DeviceEditViewModel : ObservableObject, IEditableViewModel
{
    private readonly IDeviceService _deviceService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private readonly IMessageService _messageService;

    private int? _editingId;
    private bool _showValidation;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _hasChanges;

    // === Campi editabili ===

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNameInvalid))]
    private string _name = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMachineCodeInvalid))]
    private string _machineCode = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    // === Validazione ===

    public bool IsNameInvalid => _showValidation && string.IsNullOrWhiteSpace(Name);
    public bool IsMachineCodeInvalid => _showValidation
        && (!int.TryParse(MachineCode, out var code) || code <= 0);
    public bool IsNew => _editingId is null;

    public DeviceEditViewModel(
        IDeviceService deviceService,
        INavigationService navigationService,
        IDialogService dialogService,
        IMessageService messageService)
    {
        _deviceService = deviceService;
        _navigationService = navigationService;
        _dialogService = dialogService;
        _messageService = messageService;
    }

    public async Task InitializeAsync(int? deviceId)
    {
        if (deviceId is not null)
        {
            var device = await _deviceService.GetByIdAsync(deviceId.Value);
            if (device is null)
            {
                ErrorMessage = $"Dispositivo #{deviceId} non trovato.";
                return;
            }

            _editingId = device.Id;
            Name = device.Name;
            MachineCode = device.MachineCode.ToString();
            Description = device.Description ?? string.Empty;
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
            _messageService.Show("Compilare tutti i campi obbligatori.",
                MessageSeverity.Warning);
            return false;
        }

        return true;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!Validate()) return;

        try
        {
            IsBusy = true;

            var desc = string.IsNullOrWhiteSpace(Description) ? null : Description;
            var code = int.Parse(MachineCode);

            if (_editingId is null)
            {
                var device = new Device(Name.Trim(), code, desc);
                await _deviceService.AddAsync(device);
                _messageService.Show($"Dispositivo '{Name}' creato.",
                    MessageSeverity.Success, autoHideSeconds: 3);
            }
            else
            {
                var device = Device.Restore(_editingId.Value, Name.Trim(),
                    code, desc);
                await _deviceService.UpdateAsync(device);
                _messageService.Show($"Dispositivo '{Name}' aggiornato.",
                    MessageSeverity.Success, autoHideSeconds: 3);
            }

            HasChanges = false;
            _navigationService.GoBack();
        }
        catch (Exception ex)
        {
            _messageService.Show($"Errore salvataggio: {ex.Message}",
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
        if (_editingId is null) return;

        var result = await _dialogService.ShowConfirmAsync(
            "Elimina dispositivo",
            $"Eliminare il dispositivo '{Name}'?\n" +
            "Verranno eliminati anche le schede associate.");

        if (result != Abstractions.DialogResult.Yes) return;

        try
        {
            await _deviceService.DeleteAsync(_editingId.Value);
            HasChanges = false;
            _messageService.Show($"Dispositivo '{Name}' eliminato.",
                MessageSeverity.Success, autoHideSeconds: 3);
            _navigationService.GoBack();
        }
        catch (Exception ex)
        {
            _messageService.Show($"Errore eliminazione: {ex.Message}",
                MessageSeverity.Error);
        }
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        if (HasChanges)
        {
            var result = await _dialogService.ShowConfirmAsync(
                "Annulla modifiche",
                "Ci sono modifiche non salvate. Uscire senza salvare?");
            if (result != DialogResult.Yes) return;
        }

        _navigationService.GoBack();
    }
}
