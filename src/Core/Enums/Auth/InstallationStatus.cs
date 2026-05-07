namespace Core.Enums.Auth;

/// <summary>
/// Lifecycle state of an <c>Installation</c> and its bound
/// <c>InstallationApiCredential</c>. Transitions are monotonic:
/// <see cref="Active"/> → <see cref="Revoked"/>.
/// </summary>
public enum InstallationStatus
{
    Active,
    Revoked
}
