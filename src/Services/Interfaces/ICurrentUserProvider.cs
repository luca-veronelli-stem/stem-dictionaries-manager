namespace Services.Interfaces;

/// <summary>
/// Provides the current user's ID for the audit trail.
/// Set by the GUI after login.
/// </summary>
public interface ICurrentUserProvider
{
    /// <summary>
    /// ID of the logged-in user. Null when no user is logged in.
    /// </summary>
    int? CurrentUserId { get; set; }
}
