using Services.Interfaces;

namespace Services;

/// <summary>
/// ICurrentUserProvider implementation.
/// Singleton: the GUI sets it after login; services read it for auditing.
/// </summary>
public class CurrentUserProvider : ICurrentUserProvider
{
    public int? CurrentUserId { get; set; }
}
