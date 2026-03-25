using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GUI.Windows.Abstractions;
using Services.Interfaces;

namespace GUI.Windows.ViewModels;

/// <summary>
/// Item per la lista comandi.
/// </summary>
public record CommandListItem
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public bool IsResponse { get; init; }
    public int ParameterCount { get; init; }
}

/// <summary>
/// ViewModel per la lista dei comandi.
/// </summary>
public partial class CommandListViewModel : ObservableObject
{
    private readonly ICommandService _commandService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private readonly IMessageService _messageService;

    private bool _isInitialized;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    private List<CommandListItem> _allCommands = [];

    [ObservableProperty]
    private List<CommandListItem> _commands = [];

    [ObservableProperty]
    private CommandListItem? _selectedItem;

    [ObservableProperty]
    private string _searchText = string.Empty;

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    public CommandListViewModel(
        ICommandService commandService,
        INavigationService navigationService,
        IDialogService dialogService,
        IMessageService messageService)
    {
        _commandService = commandService;
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

            var commands = await _commandService.GetAllAsync();

            _allCommands = [.. commands
                .Select(c => new CommandListItem
                {
                    Id = c.Id,
                    Name = c.Name,
                    Code = $"0x{c.FullCode:X4}",
                    IsResponse = c.IsResponse,
                    ParameterCount = c.Parameters.Count
                })
                .OrderBy(c => c.Code)];

            ApplyFilter();
            _messageService.Show($"Caricati {_allCommands.Count} comandi", MessageSeverity.Success);
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
        _navigationService.NavigateTo(ViewType.CommandEdit, new NavigationParameter { EntityId = null });
    }

    [RelayCommand]
    private void Edit(CommandListItem? item)
    {
        if (item is null) return;
        _navigationService.NavigateTo(ViewType.CommandEdit, new NavigationParameter { EntityId = item.Id });
    }

    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            Commands = _allCommands;
            return;
        }

        var term = SearchText.Trim();
        Commands = [.. _allCommands.Where(c =>
            c.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
            c.Code.Contains(term, StringComparison.OrdinalIgnoreCase))];
    }
}
