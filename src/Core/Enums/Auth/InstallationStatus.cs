namespace Core.Enums.Auth;

/// <summary>
/// Lifecycle state of an <c>Installation</c> and its bound
/// <c>InstallationApiCredential</c>. Transitions are monotonic:
/// <see cref="Active"/> → <see cref="Revoked"/>.
/// </summary>
/// <remarks>
/// Enum ordinals are load-bearing — the filtered unique index on
/// <c>InstallationApiCredentials</c>
/// (<c>HasFilter("[Status] = 0")</c>) references <c>0</c> as
/// <see cref="Active"/>. Reordering this enum changes the filter's
/// semantics on existing databases. Append new values at the end;
/// do not reorder.
/// </remarks>
public enum InstallationStatus
{
    Active = 0,
    Revoked = 1
}
