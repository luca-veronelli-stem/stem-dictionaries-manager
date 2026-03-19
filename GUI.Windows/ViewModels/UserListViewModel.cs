using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models;
using GUI.Windows.Abstractions;
using Services.Interfaces;

namespace GUI.Windows.ViewModels;

/// <summary>
/// Item per la lista utenti.
/// </summary>
public record UserListItem
{
    public int Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
}

/// <summary>
/// ViewModel per la lista degli utenti.
/// </summary>
public partial class UserListViewModel : ObservableObject
{
    private readonly IUserService _userService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private readonly IMessageService _messageService;

    private bool _isInitialized;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private List<UserListItem> _users = [];

    [ObservableProperty]
    private UserListItem? _selectedItem;

    [ObservableProperty]
    private string _searchText = string.Empty;

    // === Campi per nuovo utente inline ===

    [ObservableProperty]
    private string _newUsername = string.Empty;

    [ObservableProperty]
    private string _newDisplayName = string.Empty;

    public UserListViewModel(
        IUserService userService,
        INavigationService navigationService,
        IDialogService dialogService,
        IMessageService messageService)
    {
        _userService = userService;
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

            var users = await _userService.GetAllAsync();

            Users = [.. users
                .Select(u => new UserListItem
                {
                    Id = u.Id,
                    Username = u.Username,
                    DisplayName = u.DisplayName
                })
                .OrderBy(u => u.Username)];

            _messageService.Show($"Caricati {Users.Count} utenti", MessageSeverity.Success);
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

    private bool CanAdd() => !string.IsNullOrWhiteSpace(NewUsername) && !string.IsNullOrWhiteSpace(NewDisplayName);

    [RelayCommand(CanExecute = nameof(CanAdd))]
    private async Task AddAsync()
    {
        try
        {
            IsBusy = true;

            var user = new User(NewUsername, NewDisplayName);
            await _userService.AddAsync(user);
            
            _messageService.Show($"Utente '{NewUsername}' creato", MessageSeverity.Success);
            
            NewUsername = string.Empty;
            NewDisplayName = string.Empty;
            
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Errore", $"Impossibile creare: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteAsync(UserListItem? item)
    {
        if (item is null) return;

        var result = await _dialogService.ShowConfirmAsync(
            "Conferma eliminazione",
            $"Eliminare l'utente '{item.Username}'?");

        if (result != DialogResult.Yes) return;

        try
        {
            IsBusy = true;
            await _userService.DeleteAsync(item.Id);
            _messageService.Show($"Utente '{item.Username}' eliminato", MessageSeverity.Success);
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
