using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models;
using GUI.Windows.Abstractions;
using Services.Interfaces;

namespace GUI.Windows.ViewModels;

/// <summary>
/// Item for the user list.
/// </summary>
public record UserListItem
{
    public int Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
}

/// <summary>
/// ViewModel for the user list.
/// </summary>
public partial class UserListViewModel : ObservableObject
{
    private readonly IUserService _userService;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private readonly IMessageService _messageService;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    private List<UserListItem> _allUsers = [];

    [ObservableProperty]
    private List<UserListItem> _users = [];

    [ObservableProperty]
    private UserListItem? _selectedItem;

    [ObservableProperty]
    private string _searchText = string.Empty;

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    // === Inline new-user fields ===

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
    /// Loads the initial data. Call after navigating to the view.
    /// </summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        try
        {
            IsBusy = true;
            ErrorMessage = null;

            IReadOnlyList<User> users = await _userService.GetAllAsync();

            _allUsers = [.. users
                .Select(u => new UserListItem
                {
                    Id = u.Id,
                    Username = u.Username,
                    DisplayName = u.DisplayName
                })
                .OrderBy(u => u.Username)];

            ApplyFilter();
            _messageService.Show($"Loaded {_allUsers.Count} users", MessageSeverity.Success);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _messageService.Show($"Error: {ex.Message}", MessageSeverity.Error);
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

            _messageService.Show($"User '{NewUsername}' created", MessageSeverity.Success);

            NewUsername = string.Empty;
            NewDisplayName = string.Empty;

            await RefreshAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Error", $"Unable to create: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteAsync(UserListItem? item)
    {
        if (item is null)
        {
            return;
        }

        DialogResult result = await _dialogService.ShowConfirmAsync(
            "Confirm deletion",
            $"Delete user '{item.Username}'?");

        if (result != DialogResult.Yes)
        {
            return;
        }

        try
        {
            IsBusy = true;
            await _userService.DeleteAsync(item.Id);
            _messageService.Show($"User '{item.Username}' deleted", MessageSeverity.Success);
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Error", $"Unable to delete: {ex.Message}");
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

    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            Users = _allUsers;
            return;
        }

        string term = SearchText.Trim();
        Users = [.. _allUsers.Where(u =>
            u.Username.Contains(term, StringComparison.OrdinalIgnoreCase) ||
            u.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase))];
    }
}
