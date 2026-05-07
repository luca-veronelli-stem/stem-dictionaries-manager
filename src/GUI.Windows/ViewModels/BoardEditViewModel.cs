using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models;
using GUI.Windows.Abstractions;
using Services.Interfaces;

namespace GUI.Windows.ViewModels;

/// <summary>
/// ViewModel per la creazione/modifica di una scheda.
/// SESSION_035: DeviceType enum → int DeviceId.
/// DeviceId bloccato quando si arriva da DeviceDetail.
/// </summary>
public partial class BoardEditViewModel : ObservableObject, IEditableViewModel
{
    private readonly IBoardService _boardService;
    private readonly IDeviceService _deviceService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private readonly IMessageService _messageService;

    private int? _editingId;
    private int? _existingDictionaryId;
    private string? _existingDictionaryName;
    private int _machineCode;
    private bool _isInitialized;
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
    private int _deviceId;

    /// <summary>
    /// Nome del dispositivo (per display read-only).
    /// </summary>
    [ObservableProperty]
    private string _deviceDisplayName = string.Empty;

    /// <summary>
    /// True se il DeviceId è bloccato (arrivo da DeviceDetail).
    /// </summary>
    [ObservableProperty]
    private bool _isDeviceIdLocked;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFirmwareTypeInvalid))]
    private int _firmwareType;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBoardNumberInvalid))]
    private int _boardNumber = 1;

    [ObservableProperty]
    private string? _partNumber;

    [ObservableProperty]
    private bool _isPrimary;

    /// <summary>
    /// Nota informativa sotto il campo FirmwareType (visibile solo in creazione).
    /// </summary>
    [ObservableProperty]
    private string? _firmwareTypeHint;

    public bool IsNew => _editingId is null;
    public string FormTitle => IsNew ? "Nuova Scheda" : "Modifica Scheda";

    // === Validazione

    public bool IsNameInvalid => _showValidation && string.IsNullOrWhiteSpace(Name);
    public bool IsFirmwareTypeInvalid => _showValidation && FirmwareType <= 0;
    public bool IsBoardNumberInvalid => _showValidation
        && (BoardNumber < 1 || BoardNumber > 63);

    // === HasChanges tracking ===
    partial void OnNameChanged(string value) => HasChanges = true;
    partial void OnFirmwareTypeChanged(int value) => HasChanges = true;
    partial void OnBoardNumberChanged(int value) => HasChanges = true;
    partial void OnPartNumberChanged(string? value) => HasChanges = true;
    partial void OnIsPrimaryChanged(bool value) => HasChanges = true;

    public BoardEditViewModel(
        IBoardService boardService,
        IDeviceService deviceService,
        INavigationService navigationService,
        IDialogService dialogService,
        IMessageService messageService)
    {
        _boardService = boardService;
        _deviceService = deviceService;
        _navigationService = navigationService;
        _dialogService = dialogService;
        _messageService = messageService;
    }

    public async Task InitializeAsync(int? boardId, int? presetDeviceId = null)
    {
        if (_isInitialized)
        {
            return;
        }

        try
        {
            IsBusy = true;
            _editingId = boardId;

            // Preset e lock DeviceId se arriva da DeviceDetail
            if (presetDeviceId.HasValue)
            {
                DeviceId = presetDeviceId.Value;
                IsDeviceIdLocked = true;

                // Carica MachineCode dal Device per ProtocolAddress
                Device? device = await _deviceService.GetByIdAsync(presetDeviceId.Value);
                _machineCode = device?.MachineCode ?? 0;
                DeviceDisplayName = device?.Name ?? $"Device #{presetDeviceId}";
            }

            if (boardId.HasValue)
            {
                Board? board = await _boardService.GetByIdAsync(boardId.Value);
                if (board is null)
                {
                    await _dialogService.ShowErrorAsync("Errore", "Scheda non trovata.");
                    _navigationService.GoBack();
                    return;
                }

                LoadFromBoard(board);
            }
            else
            {
                // Pre-compila con il primo FirmwareType disponibile
                int nextFw = await _boardService.GetNextAvailableFirmwareTypeAsync();
                FirmwareType = nextFw;
                FirmwareTypeHint = $"Primo valore disponibile suggerito ({nextFw})";
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

    private void LoadFromBoard(Board b)
    {
        Name = b.Name;
        DeviceId = b.DeviceId;
        DeviceDisplayName = b.DeviceName ?? $"Device #{b.DeviceId}";
        FirmwareType = b.FirmwareType;
        BoardNumber = b.BoardNumber;
        PartNumber = b.PartNumber;
        IsPrimary = b.IsPrimary;
        _existingDictionaryId = b.DictionaryId;
        _existingDictionaryName = b.DictionaryName;
        _machineCode = b.MachineCode;
    }

    private bool Validate()
    {
        _showValidation = true;

        OnPropertyChanged(nameof(IsNameInvalid));
        OnPropertyChanged(nameof(IsFirmwareTypeInvalid));
        OnPropertyChanged(nameof(IsBoardNumberInvalid));

        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(Name))
        {
            missing.Add("Nome");
        }

        if (FirmwareType <= 0)
        {
            missing.Add("Firmware Type");
        }

        if (BoardNumber < 1 || BoardNumber > 63)
        {
            missing.Add("Numero Scheda (1-63)");
        }

        if (missing.Count > 0)
        {
            _messageService.Show(
                $"Campi obbligatori mancanti: {string.Join(", ", missing)}",
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

            if (IsNew)
            {
                var board = new Board(
                    deviceId: DeviceId,
                    name: Name,
                    firmwareType: FirmwareType,
                    boardNumber: BoardNumber,
                    machineCode: _machineCode,
                    partNumber: PartNumber,
                    isPrimary: IsPrimary);

                await _boardService.AddAsync(board);
                _messageService.Show($"Scheda '{Name}' creata", MessageSeverity.Success);
            }
            else
            {
                var existing = Board.Restore(
                    id: _editingId!.Value,
                    deviceId: DeviceId,
                    name: Name,
                    firmwareType: FirmwareType,
                    boardNumber: BoardNumber,
                    partNumber: PartNumber,
                    isPrimary: IsPrimary,
                    dictionaryId: _existingDictionaryId,
                    machineCode: _machineCode);

                await _boardService.UpdateAsync(existing);
                _messageService.Show($"Scheda '{Name}' aggiornata", MessageSeverity.Success);
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
    private async Task DeleteBoardAsync()
    {
        if (_editingId is null)
        {
            return;
        }

        string message = _existingDictionaryName is not null
            ? $"Eliminare la scheda '{Name}'?\n" +
              $"Il dizionario '{_existingDictionaryName}' associato potrebbe essere eliminato."
            : $"Eliminare la scheda '{Name}'?";

        DialogResult result = await _dialogService.ShowConfirmAsync(
            "Conferma eliminazione", message);

        if (result != DialogResult.Yes)
        {
            return;
        }

        try
        {
            IsBusy = true;
            await _boardService.DeleteAsync(_editingId.Value);
            _messageService.Show($"Scheda '{Name}' eliminata", MessageSeverity.Success);
            HasChanges = false;
            _navigationService.GoBack();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Errore",
                $"Impossibile eliminare: {ex.Message}");
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
            DialogResult result = await _dialogService.ShowConfirmAsync(
                "Annulla modifiche",
                "Sei sicuro di voler annullare le modifiche?");
            if (result != DialogResult.Yes)
            {
                return;
            }
        }

        _navigationService.GoBack();
    }
}
