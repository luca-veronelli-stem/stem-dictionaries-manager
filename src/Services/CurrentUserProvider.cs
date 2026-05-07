using Services.Interfaces;

namespace Services;

/// <summary>
/// Implementazione di ICurrentUserProvider.
/// Singleton: la GUI lo setta dopo login, i service lo leggono per audit.
/// </summary>
public class CurrentUserProvider : ICurrentUserProvider
{
    public int? CurrentUserId { get; set; }
}
