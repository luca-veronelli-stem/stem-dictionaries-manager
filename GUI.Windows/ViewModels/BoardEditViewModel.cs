using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Enums;
using Core.Models;
using GUI.Windows.Abstractions;
using Services.Interfaces;

namespace GUI.Windows.ViewModels;

/// <summary>
/// ViewModel per la creazione/modifica di una scheda.
/// </summary>
public partial class BoardEditViewModel : ObservableObject
{
    private readonly IBoardService _boardService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private readonly IMessageService _messageService;

    private int? _editingId;
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
    private DeviceType _selectedDeviceType = DeviceType.OptimusXp;

    [ObservableProperty]
    private BoardTypeItem? _selectedBoardType;

    [ObservableProperty]
    private int _boardNumber = 1;

    [ObservableProperty]
    private string? _partNumber;

    [ObservableProperty]
    private bool _isPrimary;

    [ObservableProperty]
    private List<BoardTypeItem> _availableBoardTypes = [];

    // === Computed Properties ===

    public bool IsNew => _editingId is null;
    public string FormTitle => IsNew ? "Nuova Scheda" : "Modifica Scheda";

    public IReadOnlyList<DeviceType> DeviceTypes { get; } = Enum.GetValues<DeviceType>();

    public BoardEditViewModel(
        IBoardService boardService,
        INavigationService navigationService,
        IDialogService dialogService,
        IMessageService messageService)
    {
        _boardService = boardService;
        _navigationService = navigationService;
        _dialogService = dialogService;
        _messageService = messageService;
    }

    /// <summary>
    /// Inizializza il ViewModel.
    /// </summary>
    public async Task InitializeAsync(int? boardId)
    {
        if (_isInitialized) return;

        try
        {
            IsBusy = true;
            _editingId = boardId;

            // Carica i BoardType disponibili
            var boardTypes = await _boardService.GetBoardTypesAsync();
            AvailableBoardTypes = [.. boardTypes
                .Select(bt => new BoardTypeItem
                {
                    Id = bt.Id,
                    Name = bt.Name,
                    FirmwareType = bt.FirmwareType
                })];

            if (boardId.HasValue)
            {
                var board = await _boardService.GetByIdAsync(boardId.Value);
                if (board is null)
                {
                    await _dialogService.ShowErrorAsync("Errore", "Scheda non trovata.");
                    _navigationService.GoBack();
                    return;
                }

                LoadFromBoard(board);
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
        SelectedDeviceType = b.DeviceType;
        SelectedBoardType = AvailableBoardTypes.FirstOrDefault(bt => bt.Id == b.BoardType.Id);
        BoardNumber = b.BoardNumber;
        PartNumber = b.PartNumber;
        IsPrimary = b.IsPrimary;
    }

    private bool CanSave() => !string.IsNullOrWhiteSpace(Name) && SelectedBoardType is not null;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        if (SelectedBoardType is null) return;

        try
        {
            IsBusy = true;

            // Recupera il BoardType completo
            var boardType = await _boardService.GetBoardTypeByNameAsync(SelectedBoardType.Name);
            if (boardType is null)
            {
                await _dialogService.ShowErrorAsync("Errore", "BoardType non valido.");
                return;
            }

            if (IsNew)
            {
                var board = new Board(
                    deviceType: SelectedDeviceType,
                    boardType: boardType,
                    name: Name,
                    boardNumber: BoardNumber,
                    partNumber: PartNumber,
                    isPrimary: IsPrimary);

                await _boardService.AddAsync(board);
                _messageService.Show($"Scheda '{Name}' creata", MessageSeverity.Success);
            }
            else
            {
                var existing = Board.Restore(
                    id: _editingId!.Value,
                    deviceType: SelectedDeviceType,
                    boardType: boardType,
                    name: Name,
                    boardNumber: BoardNumber,
                    partNumber: PartNumber,
                    isPrimary: IsPrimary);

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
