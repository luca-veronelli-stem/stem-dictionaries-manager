using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models;
using GUI.Windows.Abstractions;
using Services.Interfaces;

namespace GUI.Windows.ViewModels;

/// <summary>
/// Item per la lista dizionari di un device.
/// </summary>
public partial class DictionaryItem : ObservableObject
{
    public int Id { get; }
    public string Name { get; }
    public string Semantic { get; }
    public int ItemCount { get; }
    public bool IsCommandsEntry { get; init; }
    public bool IsStandard { get; init; }

    public DictionaryItem(int id, string name, string semantic, int itemCount)
    {
        Id = id;
        Name = name;
        Semantic = semantic;
        ItemCount = itemCount;
    }
}

/// <summary>
/// Item per la lista schede di un device.
/// </summary>
public record BoardListItem
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int MachineCode { get; init; }
    public int FirmwareType { get; init; }
    public int BoardNumber { get; init; }
    public string ProtocolAddress { get; init; } = string.Empty;
    public string? PartNumber { get; init; }
    public string? DictionaryName { get; init; }
    public bool IsPrimary { get; init; }
}

/// <summary>
/// ViewModel per il dettaglio di un dispositivo.
/// SESSION_035: DeviceType enum → int DeviceId. Nome dal DB.
/// </summary>
public partial class DeviceDetailViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly IDictionaryService _dictionaryService;
    private readonly IBoardService _boardService;
    private readonly IDeviceService _deviceService;
    private readonly ICommandService _commandService;
    private readonly IDialogService _dialogService;
    private readonly IMessageService _messageService;

    [ObservableProperty]
    private int? _deviceId;

    [ObservableProperty]
    private string _deviceName = string.Empty;

    [ObservableProperty]
    private ObservableCollection<DictionaryItem> _dictionaries = [];

    [ObservableProperty]
    private DictionaryItem? _selectedDictionary;

    [ObservableProperty]
    private ObservableCollection<BoardListItem> _boards = [];

    [ObservableProperty]
    private BoardListItem? _selectedBoard;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    public DeviceDetailViewModel(
        INavigationService navigationService,
        IDictionaryService dictionaryService,
        IBoardService boardService,
        IDeviceService deviceService,
        ICommandService commandService,
        IDialogService dialogService,
        IMessageService messageService)
    {
        _navigationService = navigationService;
        _dictionaryService = dictionaryService;
        _boardService = boardService;
        _deviceService = deviceService;
        _commandService = commandService;
        _dialogService = dialogService;
        _messageService = messageService;
    }

    /// <summary>
    /// Carica dizionari e schede per il device specificato.
    /// </summary>
    public async Task LoadAsync(int deviceId)
    {
        DeviceId = deviceId;

        var device = await _deviceService.GetByIdAsync(deviceId);
        DeviceName = device?.Name ?? $"Device #{deviceId}";

        await LoadDataAsync();
    }

    /// <summary>
    /// Ricarica solo le schede (usato dopo GoBack da BoardEdit).
    /// </summary>
    public async Task ReloadBoardsAsync()
    {
        if (DeviceId is null) return;

        try
        {
            var boards = await _boardService.GetByDeviceIdAsync(DeviceId.Value);
            PopulateBoards(boards);
        }
        catch (Exception ex)
        {
            _messageService.Show($"Errore caricamento schede: {ex.Message}",
                MessageSeverity.Error);
        }
    }

    private async Task LoadDataAsync()
    {
        if (DeviceId is null) return;

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var id = DeviceId.Value;

            // Carica board di questo device
            var boards = await _boardService.GetByDeviceIdAsync(id);
            PopulateBoards(boards);

            // Popola sezione dizionari (derivati da board)
            var linkedDictIds = new HashSet<int>(
                boards.Where(b => b.DictionaryId.HasValue)
                      .Select(b => b.DictionaryId!.Value));

            var allDicts = await _dictionaryService.GetAllAsync();
            var relevantDicts = allDicts
                .Where(d => !d.IsStandard && linkedDictIds.Contains(d.Id))
                .ToList();

            var items = relevantDicts.Select(d =>
            {
                return new DictionaryItem(d.Id, d.Name, "Specifico", d.Variables.Count);
            });

            var sortedItems = items.OrderBy(d => d.Name).ToList();

            // Aggiungi entry "Comandi" con conteggio reale
            var allCommands = await _commandService.GetAllAsync();
            sortedItems.Add(new DictionaryItem(0, "Comandi", "Comandi", allCommands.Count)
            { IsCommandsEntry = true });

            Dictionaries = new ObservableCollection<DictionaryItem>(sortedItems);
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

    private void PopulateBoards(IReadOnlyList<Board> boards)
    {
        Boards = new ObservableCollection<BoardListItem>(
            boards.OrderBy(b => b.BoardNumber).Select(b => new BoardListItem
            {
                Id = b.Id,
                Name = b.Name,
                MachineCode = b.MachineCode,
                FirmwareType = b.FirmwareType,
                BoardNumber = b.BoardNumber,
                ProtocolAddress = $"0x{b.ProtocolAddress:X8}",
                PartNumber = b.PartNumber,
                DictionaryName = b.DictionaryName,
                IsPrimary = b.IsPrimary
            }));
    }

    [RelayCommand]
    private void EditDevice()
    {
        if (DeviceId is null) return;

        _navigationService.NavigateTo(ViewType.DeviceEdit, new NavigationParameter
        {
            EntityId = DeviceId.Value
        });
    }

    [RelayCommand]
    private void OpenDictionary()
    {
        if (SelectedDictionary is null) return;

        if (SelectedDictionary.IsCommandsEntry)
        {
            _navigationService.NavigateTo(ViewType.DeviceCommands, new NavigationParameter
            {
                DeviceId = DeviceId!.Value
            });
            return;
        }

        _navigationService.NavigateTo(ViewType.DictionaryEdit, new NavigationParameter
        {
            EntityId = SelectedDictionary.Id
        });
    }

    [RelayCommand]
    private void AddDictionary()
    {
        if (DeviceId is null) return;

        _navigationService.NavigateTo(ViewType.DictionaryEdit, new NavigationParameter
        {
            EntityId = null,
            DeviceId = DeviceId.Value
        });
    }

    [RelayCommand]
    private void AddBoard()
    {
        if (DeviceId is null) return;

        _navigationService.NavigateTo(ViewType.BoardEdit, new NavigationParameter
        {
            EntityId = null,
            DeviceId = DeviceId.Value
        });
    }

    [RelayCommand]
    private void EditBoard(BoardListItem? item)
    {
        if (item is null) return;

        _navigationService.NavigateTo(ViewType.BoardEdit, new NavigationParameter
        {
            EntityId = item.Id,
            DeviceId = DeviceId
        });
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }
}
