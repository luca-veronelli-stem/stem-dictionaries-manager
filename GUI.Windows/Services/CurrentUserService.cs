using Core.Models;
using GUI.Windows.Abstractions;

namespace GUI.Windows.Services;

/// <summary>
/// Implementazione singleton per tracciare l'utente corrente.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    public User? CurrentUser { get; private set; }

    public bool IsUserSelected => CurrentUser is not null;

    public bool LogoutRequested { get; set; }

    public void SetCurrentUser(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        CurrentUser = user;
    }
}
