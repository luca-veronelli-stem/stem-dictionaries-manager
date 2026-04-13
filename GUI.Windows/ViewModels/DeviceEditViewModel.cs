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
    private readonly IBoardService _boardService;
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

    /// <summary>
    /// Nota informativa sotto il campo MachineCode (visibile solo in creazione).
    /// </summary>
    [ObservableProperty]
    private string? _machineCodeHint;

    // === Validazione ===

    public bool IsNameInvalid => _showValidation && string.IsNullOrWhiteSpace(Name);
    public bool IsMachineCodeInvalid => _showValidation
        && (!int.TryParse(MachineCode, out var code) || code <= 0);
    public bool IsNew => _editingId is null;

    public DeviceEditViewModel(
        IDeviceService deviceService,
        IBoardService boardService,
        INavigationService navigationService,
        IDialogService dialogService,
        IMessageService messageService)
    {
        _deviceService = deviceService;
        _boardService = boardService;
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
        else
        {
            // Pre-compila con il primo MachineCode disponibile
            var nextCode = await _deviceService.GetNextAvailableMachineCodeAsync();
            MachineCode = nextCode.ToString();
            MachineCodeHint = $"Primo valore disponibile suggerito ({nextCode})";
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

        // Calcola conteggi per il warning
        var boards = await _boardService.GetByDeviceIdAsync(_editingId.Value);
        var boardCount = boards.Count;
        var dictCount = boards
            .Where(b => b.DictionaryId.HasValue)
            .Select(b => b.DictionaryId!.Value)
            .Distinct()
            .Count();

        var message = $"Eliminare il dispositivo '{Name}'?\n" +
            $"Verranno eliminate {boardCount} schede";
        if (dictCount > 0)
            message += $" e i dizionari dedicati associati";
        message += ".";

        var result = await _dialogService.ShowConfirmAsync(
            "Elimina dispositivo", message);

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
