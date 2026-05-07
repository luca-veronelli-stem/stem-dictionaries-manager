namespace Core.Enums.Auth;

/// <summary>
/// Lifecycle state of a <c>BootstrapToken</c>.
/// </summary>
/// <remarks>
/// <see cref="Expired"/> is a derived state evaluated at read time
/// (<c>Now &gt; ExpiresAt &amp;&amp; Status = Issued</c>); it is not
/// persisted. The DB stores <see cref="Issued"/> until the row
/// transitions to <see cref="Used"/> or <see cref="Revoked"/>.
/// </remarks>
public enum BootstrapTokenStatus
{
    Issued,
    Used,
    Expired,
    Revoked
}
