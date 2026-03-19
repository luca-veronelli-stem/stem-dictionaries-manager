using Core.Models;

namespace GUI.Windows.Abstractions;

/// <summary>
/// Servizio per tracciare l'utente corrente dell'applicazione (singleton).
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// Utente corrente selezionato all'avvio. Null se non ancora selezionato.
    /// </summary>
    User? CurrentUser { get; }

    /// <summary>
    /// Imposta l'utente corrente.
    /// </summary>
    void SetCurrentUser(User user);

    /// <summary>
    /// True se un utente è stato selezionato.
    /// </summary>
    bool IsUserSelected { get; }
}
