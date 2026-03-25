using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Enums;
using Core.Models;
using GUI.Windows.Abstractions;
using Services.Interfaces;

namespace GUI.Windows.ViewModels;

/// <summary>
/// ViewModel per la creazione/modifica di una scheda.
/// Domain v2: FirmwareType diretto, DictionaryId?, nessun BoardType.
/// </summary>
public partial class BoardEditViewModel : ObservableObject, IEditableViewModel
{
    private readonly IBoardService _boardService;
    private readonly IDictionaryService _dictionaryService;
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

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private DeviceType _selectedDeviceType = DeviceType.OptimusXp;

    [ObservableProperty]
    private int _firmwareType;

    [ObservableProperty]
    private int _boardNumber = 1;

    [ObservableProperty]
    private string? _partNumber;

    [ObservableProperty]
    private bool _isPrimary;

    [ObservableProperty]
    private DictionarySelectItem? _selectedDictionary;

    [ObservableProperty]
    private List<DictionarySelectItem> _availableDictionaries = [];

    public bool IsNew => _editingId is null;
    public string FormTitle => IsNew ? "Nuova Scheda" : "Modifica Scheda";
    public IReadOnlyList<DeviceType> DeviceTypes { get; } = Enum.GetValues<DeviceType>();

    public BoardEditViewModel(
        IBoardService boardService,
        IDictionaryService dictionaryService,
        INavigationService navigationService,
        IDialogService dialogService,
        IMessageService messageService)
    {
        _boardService = boardService;
        _dictionaryService = dictionaryService;
        _navigationService = navigationService;
        _dialogService = dialogService;
        _messageService = messageService;
    }

    public async Task InitializeAsync(int? boardId)
    {
        if (_isInitialized) return;

        try
        {
            IsBusy = true;
            _editingId = boardId;

            // Carica i dizionari disponibili per il dropdown
            var dictionaries = await _dictionaryService.GetAllAsync();
            AvailableDictionaries = [.. dictionaries
                .Select(d => new DictionarySelectItem { Id = d.Id, Name = d.Name })];

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
        FirmwareType = b.FirmwareType;
        BoardNumber = b.BoardNumber;
        PartNumber = b.PartNumber;
        IsPrimary = b.IsPrimary;
        SelectedDictionary = b.DictionaryId.HasValue
            ? AvailableDictionaries.FirstOrDefault(d => d.Id == b.DictionaryId.Value)
            : null;
    }

    private bool CanSave() => !string.IsNullOrWhiteSpace(Name);

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        try
        {
            IsBusy = true;

            if (IsNew)
            {
                var board = new Board(
                    deviceType: SelectedDeviceType,
                    name: Name,
                    firmwareType: FirmwareType,
                    boardNumber: BoardNumber,
                    partNumber: PartNumber,
                    isPrimary: IsPrimary,
                    dictionaryId: SelectedDictionary?.Id);

                await _boardService.AddAsync(board);
                _messageService.Show($"Scheda '{Name}' creata", MessageSeverity.Success);
            }
            else
            {
                var existing = Board.Restore(
                    id: _editingId!.Value,
                    deviceType: SelectedDeviceType,
                    name: Name,
                    firmwareType: FirmwareType,
                    boardNumber: BoardNumber,
                    partNumber: PartNumber,
                    isPrimary: IsPrimary,
                    dictionaryId: SelectedDictionary?.Id);

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
                "Annulla modifiche",
                "Sei sicuro di voler annullare le modifiche?");
            if (result != DialogResult.Yes) return;
        }

        _navigationService.GoBack();
    }
}

/// <summary>
/// Item per il dropdown di selezione dizionario.
/// </summary>
public class DictionarySelectItem
{
    public int Id { get; init; }
    public required string Name { get; init; }

    public override string ToString() => Name;
}
