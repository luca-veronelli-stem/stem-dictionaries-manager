namespace Core.Models.Auth;

/// <summary>
/// Parsed descriptor value object passed from the API layer down through
/// <c>RegistrationService</c>. Mirrors the wire DTO but lives in
/// <see cref="Core"/> so the domain layer can validate and persist it
/// without depending on API DTOs. <see cref="OsUserId"/> and
/// <see cref="MachineId"/> are nullable in storage; per-<c>clientApp</c>
/// presence policy is enforced upstream by <c>RegistrationService</c>
/// (see <c>contracts/register.md</c>).
/// </summary>
public sealed record InstallationDescriptor(
    string ClientApp,
    string? OsUserId,
    string? MachineId,
    Guid InstallGuid,
    string? AppVersion);
