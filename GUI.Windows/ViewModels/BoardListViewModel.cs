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
    public string BoardType { get; init; } = string.Empty;
    public int BoardNumber { get; init; }
    public string ProtocolAddress { get; init; } = string.Empty;
    public string? PartNumber { get; init; }
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

    [ObservableProperty]
    private List<BoardListItem> _boards = [];

    [ObservableProperty]
    private BoardListItem? _selectedItem;

    [ObservableProperty]
    private string _searchText = string.Empty;

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

            Boards = [.. boards
                .Select(b => new BoardListItem
                {
                    Id = b.Id,
                    Name = b.Name,
                    DeviceType = b.DeviceType.ToString(),
                    BoardType = b.BoardType.Name,
                    BoardNumber = b.BoardNumber,
                    ProtocolAddress = $"0x{b.ProtocolAddress:X8}",
                    PartNumber = b.PartNumber
                })
                .OrderBy(b => b.DeviceType)
                .ThenBy(b => b.BoardNumber)];

            _messageService.Show($"Caricate {Boards.Count} schede", MessageSeverity.Success);
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

    [RelayCommand]
    private async Task DeleteAsync(BoardListItem? item)
    {
        if (item is null) return;

        var result = await _dialogService.ShowConfirmAsync(
            "Conferma eliminazione",
            $"Eliminare la scheda '{item.Name}'?");

        if (result != DialogResult.Yes) return;

        try
        {
            IsBusy = true;
            await _boardService.DeleteAsync(item.Id);
            _messageService.Show($"Scheda '{item.Name}' eliminata", MessageSeverity.Success);
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Errore", $"Impossibile eliminare: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }
}
