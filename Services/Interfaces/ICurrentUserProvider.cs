namespace Services.Interfaces;

/// <summary>
/// Fornisce l'ID dell'utente corrente per l'audit trail.
/// Settato dalla GUI dopo il login.
/// </summary>
public interface ICurrentUserProvider
{
    /// <summary>
    /// ID dell'utente loggato. Null se nessun utente è loggato.
    /// </summary>
    int? CurrentUserId { get; set; }
}
