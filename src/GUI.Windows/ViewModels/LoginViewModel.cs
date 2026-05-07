using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models;
using Services.Interfaces;

namespace GUI.Windows.ViewModels;

/// <summary>
/// ViewModel for the login screen (user selection).
/// </summary>
public partial class LoginViewModel : ObservableObject
{
    private readonly IUserService _userService;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmLoginCommand))]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private List<User> _availableUsers = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmLoginCommand))]
    private User? _selectedUser;

    /// <summary>
    /// Event fired when the user confirms the login.
    /// </summary>
    public event Action<User>? LoginConfirmed;

    public LoginViewModel(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Loads users from the database.
    /// </summary>
    public async Task LoadUsersAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            IReadOnlyList<User> users = await _userService.GetAllAsync();
            AvailableUsers = [.. users];

            if (AvailableUsers.Count == 0)
            {
                ErrorMessage = "No users available. Contact the administrator.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading users: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanConfirmLogin() => SelectedUser is not null && !IsLoading;

    [RelayCommand(CanExecute = nameof(CanConfirmLogin))]
    private void ConfirmLogin()
    {
        if (SelectedUser is not null)
        {
            LoginConfirmed?.Invoke(SelectedUser);
        }
    }

    /// <summary>
    /// Clears event subscriptions to avoid duplicate invocations.
    /// </summary>
    public void ClearSubscriptions()
    {
        LoginConfirmed = null;
    }
}
