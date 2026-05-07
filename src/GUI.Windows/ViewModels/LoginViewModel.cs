using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models;
using Services.Interfaces;

namespace GUI.Windows.ViewModels;

/// <summary>
/// ViewModel per la schermata di login (selezione utente).
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
    /// Evento fired quando l'utente conferma il login.
    /// </summary>
    public event Action<User>? LoginConfirmed;

    public LoginViewModel(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Carica gli utenti dal database.
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
                ErrorMessage = "Nessun utente disponibile. Contattare l'amministratore.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Errore nel caricamento utenti: {ex.Message}";
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
    /// Pulisce le sottoscrizioni agli eventi per evitare chiamate duplicate.
    /// </summary>
    public void ClearSubscriptions()
    {
        LoginConfirmed = null;
    }
}
