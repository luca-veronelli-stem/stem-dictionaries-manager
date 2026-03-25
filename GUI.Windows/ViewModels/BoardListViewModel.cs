using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Enums;
using GUI.Windows.Abstractions;
using Services.Interfaces;

namespace GUI.Windows.ViewModels;

/// <summary>
/// Item per la lista schede.
/// </summary>
public record BoardListItem
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string DeviceType { get; init; } = string.Empty;
    public int FirmwareType { get; init; }
    public int BoardNumber { get; init; }
    public string ProtocolAddress { get; init; } = string.Empty;
    public string? PartNumber { get; init; }
    public string? DictionaryName { get; init; }
    public bool IsPrimary { get; init; }
}

/// <summary>
/// ViewModel per la lista delle schede.
/// </summary>
public partial class BoardListViewModel : ObservableObject
{
    private readonly IBoardService _boardService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private readonly IMessageService _messageService;

    private bool _isInitialized;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    private List<BoardListItem> _allBoards = [];

    [ObservableProperty]
    private List<BoardListItem> _boards = [];

    [ObservableProperty]
    private BoardListItem? _selectedItem;

    [ObservableProperty]
    private string _searchText = string.Empty;

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    [ObservableProperty]
    private DeviceType? _filterDeviceType;

    public IReadOnlyList<DeviceType> DeviceTypes { get; } = Enum.GetValues<DeviceType>();

    public BoardListViewModel(
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
    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        await RefreshAsync();
        _isInitialized = true;
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var boards = FilterDeviceType.HasValue
                ? await _boardService.GetByDeviceTypeAsync(FilterDeviceType.Value)
                : await _boardService.GetAllAsync();

            _allBoards = [.. boards
                .Select(b => new BoardListItem
                {
                    Id = b.Id,
                    Name = b.Name,
                    DeviceType = b.DeviceType.ToString(),
                    FirmwareType = b.FirmwareType,
                    BoardNumber = b.BoardNumber,
                    ProtocolAddress = $"0x{b.ProtocolAddress:X8}",
                    PartNumber = b.PartNumber,
                    DictionaryName = b.DictionaryName,
                    IsPrimary = b.IsPrimary
                })
                .OrderBy(b => b.DeviceType)
                .ThenBy(b => b.BoardNumber)];

            ApplyFilter();
            _messageService.Show($"Caricate {_allBoards.Count} schede", MessageSeverity.Success);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _messageService.Show($"Errore: {ex.Message}", MessageSeverity.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Add()
    {
        _navigationService.NavigateTo(ViewType.BoardEdit, new NavigationParameter { EntityId = null });
    }

    [RelayCommand]
    private void Edit(BoardListItem? item)
    {
        if (item is null) return;
        _navigationService.NavigateTo(ViewType.BoardEdit, new NavigationParameter { EntityId = item.Id });
    }

    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            Boards = _allBoards;
            return;
        }

        var term = SearchText.Trim();
        Boards = [.. _allBoards.Where(b =>
            b.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
            b.DeviceType.Contains(term, StringComparison.OrdinalIgnoreCase) ||
            (b.PartNumber?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false))];
    }
}
