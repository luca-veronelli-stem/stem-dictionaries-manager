namespace API.Dtos.Auth;

/// <summary>
/// Wire shape of one item in the <c>GET /api/admin/installations</c>
/// response array. See <c>contracts/admin-installations.md</c>. The
/// credential plaintext and its hash are never included — only the
/// metadata fields below.
/// </summary>
public sealed record InstallationListItemDto(
    int InstallationId,
    string ClientApp,
    string? OsUserId,
    string? MachineId,
    Guid InstallGuid,
    DateTime RegisteredAt,
    string Status,
    DateTime? RevokedAt);
