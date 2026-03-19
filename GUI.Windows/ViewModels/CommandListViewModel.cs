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

    [ObservableProperty]
    private List<CommandListItem> _commands = [];

    [ObservableProperty]
    private CommandListItem? _selectedItem;

    [ObservableProperty]
    private string _searchText = string.Empty;

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

            Commands = [.. commands
                .Select(c => new CommandListItem
                {
                    Id = c.Id,
                    Name = c.Name,
                    Code = $"0x{c.FullCode:X4}",
                    IsResponse = c.IsResponse,
                    ParameterCount = c.Parameters.Count
                })
                .OrderBy(c => c.Code)];

            _messageService.Show($"Caricati {Commands.Count} comandi", MessageSeverity.Success);
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

    [RelayCommand]
    private async Task DeleteAsync(CommandListItem? item)
    {
        if (item is null) return;

        var result = await _dialogService.ShowConfirmAsync(
            "Conferma eliminazione",
            $"Eliminare il comando '{item.Name}'?");

        if (result != DialogResult.Yes) return;

        try
        {
            IsBusy = true;
            await _commandService.DeleteAsync(item.Id);
            _messageService.Show($"Comando '{item.Name}' eliminato", MessageSeverity.Success);
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
