namespace API.Dtos.Auth;

/// <summary>
/// Wire shape of the <c>POST /api/admin/installations/{id}/revoke</c>
/// success response. See <c>contracts/admin-installations.md</c>. On
/// idempotent re-revoke the body carries the original <see cref="RevokedAt"/>
/// — the endpoint does not mutate state on the second call.
/// </summary>
public sealed record RevokeInstallationResponseDto(
    int InstallationId,
    string Status,
    DateTime RevokedAt);
