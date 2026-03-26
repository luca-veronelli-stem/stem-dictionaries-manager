using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Enums;
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
    public int VariableCount { get; }

    public DictionaryItem(int id, string name, string semantic, int variableCount)
    {
        Id = id;
        Name = name;
        Semantic = semantic;
        VariableCount = variableCount;
    }
}

/// <summary>
/// Item per la lista schede di un device.
/// </summary>
public record BoardListItem
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int FirmwareType { get; init; }
    public int BoardNumber { get; init; }
    public string ProtocolAddress { get; init; } = string.Empty;
    public string? PartNumber { get; init; }
    public string? DictionaryName { get; init; }
    public bool IsPrimary { get; init; }
}

/// <summary>
/// ViewModel per il dettaglio di un tipo dispositivo.
/// Mostra dizionari (derivati) e schede del device (F5.2).
/// </summary>
public partial class DeviceDetailViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private readonly IDictionaryService _dictionaryService;
    private readonly IBoardService _boardService;
    private readonly IDialogService _dialogService;
    private readonly IMessageService _messageService;

    [ObservableProperty]
    private DeviceType? _deviceType;

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
        IDialogService dialogService,
        IMessageService messageService)
    {
        _navigationService = navigationService;
        _dictionaryService = dictionaryService;
        _boardService = boardService;
        _dialogService = dialogService;
        _messageService = messageService;
    }

    /// <summary>
    /// Carica dizionari e schede per il device specificato.
    /// Chiamato da MainViewModel.InitializeViewModelAsync.
    /// </summary>
    public async Task LoadAsync(DeviceType deviceType)
    {
        DeviceType = deviceType;
        DeviceName = GetDeviceName(deviceType);
        await LoadDataAsync();
    }

    /// <summary>
    /// Ricarica solo le schede (usato dopo GoBack da BoardEdit).
    /// </summary>
    public async Task ReloadBoardsAsync()
    {
        if (DeviceType is null) return;
        await LoadBoardsAsync(DeviceType.Value);
    }

    private static string GetDeviceName(DeviceType deviceType) => deviceType switch
    {
        Core.Enums.DeviceType.SherpaSlim => "Sherpa Slim",
        Core.Enums.DeviceType.TopLiftM => "TopLift-M",
        Core.Enums.DeviceType.EdenXp => "Eden-XP",
        Core.Enums.DeviceType.Gradino => "Gradino",
        Core.Enums.DeviceType.Spyke => "Spyke",
        Core.Enums.DeviceType.Spark => "Spark",
        Core.Enums.DeviceType.TopLiftA2 => "TopLift-A2",
        Core.Enums.DeviceType.O3zTech => "O3Z-Tech",
        Core.Enums.DeviceType.OptimusXp => "Optimus-XP",
        Core.Enums.DeviceType.R3lXp => "R3L-XP",
        Core.Enums.DeviceType.EdenBs8 => "Eden-BS8",
        _ => deviceType.ToString()
    };

    private async Task LoadDataAsync()
    {
        if (DeviceType is null) return;

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var dt = DeviceType.Value;

            // Carica board di questo device
            var boards = await _boardService.GetByDeviceTypeAsync(dt);

            // Popola sezione schede
            Boards = new ObservableCollection<BoardListItem>(
                boards.OrderBy(b => b.BoardNumber).Select(b => new BoardListItem
                {
                    Id = b.Id,
                    Name = b.Name,
                    FirmwareType = b.FirmwareType,
                    BoardNumber = b.BoardNumber,
                    ProtocolAddress = $"0x{b.ProtocolAddress:X8}",
                    PartNumber = b.PartNumber,
                    DictionaryName = b.DictionaryName,
                    IsPrimary = b.IsPrimary
                }));

            // Popola sezione dizionari (derivati da board)
            var linkedDictIds = new HashSet<int>(
                boards.Where(b => b.DictionaryId.HasValue)
                      .Select(b => b.DictionaryId!.Value));

            var allDicts = await _dictionaryService.GetAllAsync();
            var relevantDicts = allDicts
                .Where(d => d.IsStandard || linkedDictIds.Contains(d.Id))
                .ToList();

            var items = relevantDicts.Select(d =>
            {
                var semantic = d.IsStandard ? "Standard" : "Specifico";
                return new DictionaryItem(d.Id, d.Name, semantic, d.Variables.Count);
            });

            Dictionaries = new ObservableCollection<DictionaryItem>(
                items.OrderBy(d => d.Name));
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

    private async Task LoadBoardsAsync(DeviceType dt)
    {
        try
        {
            var boards = await _boardService.GetByDeviceTypeAsync(dt);
            Boards = new ObservableCollection<BoardListItem>(
                boards.OrderBy(b => b.BoardNumber).Select(b => new BoardListItem
                {
                    Id = b.Id,
                    Name = b.Name,
                    FirmwareType = b.FirmwareType,
                    BoardNumber = b.BoardNumber,
                    ProtocolAddress = $"0x{b.ProtocolAddress:X8}",
                    PartNumber = b.PartNumber,
                    DictionaryName = b.DictionaryName,
                    IsPrimary = b.IsPrimary
                }));
        }
        catch (Exception ex)
        {
            _messageService.Show($"Errore caricamento schede: {ex.Message}",
                MessageSeverity.Error);
        }
    }

    [RelayCommand]
    private void OpenDictionary()
    {
        if (SelectedDictionary is null) return;

        _navigationService.NavigateTo(ViewType.DictionaryEdit, new NavigationParameter
        {
            EntityId = SelectedDictionary.Id
        });
    }

    [RelayCommand]
    private void AddBoard()
    {
        if (DeviceType is null) return;

        _navigationService.NavigateTo(ViewType.BoardEdit, new NavigationParameter
        {
            EntityId = null,
            DeviceType = DeviceType.Value
        });
    }

    [RelayCommand]
    private void EditBoard(BoardListItem? item)
    {
        if (item is null) return;

        _navigationService.NavigateTo(ViewType.BoardEdit, new NavigationParameter
        {
            EntityId = item.Id,
            DeviceType = DeviceType
        });
    }

    [RelayCommand]
    private async Task DeleteBoardAsync(BoardListItem? item)
    {
        if (item is null || DeviceType is null) return;

        var result = await _dialogService.ShowConfirmAsync(
            "Conferma eliminazione",
            $"Eliminare la scheda '{item.Name}'?");

        if (result != DialogResult.Yes) return;

        try
        {
            await _boardService.DeleteAsync(item.Id);
            _messageService.Show($"Scheda '{item.Name}' eliminata",
                MessageSeverity.Success);
            await LoadBoardsAsync(DeviceType.Value);
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Errore",
                $"Impossibile eliminare: {ex.Message}");
        }
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }
}
