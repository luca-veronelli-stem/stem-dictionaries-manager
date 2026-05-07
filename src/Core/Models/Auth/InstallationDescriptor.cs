namespace Core.Models.Auth;

/// <summary>
/// Parsed descriptor value object passed from the API layer down through
/// <c>RegistrationService</c>. Mirrors the wire DTO but lives in
/// <see cref="Core"/> so the domain layer can validate and persist it
/// without depending on API DTOs.
/// </summary>
public sealed record InstallationDescriptor(
    string ClientApp,
    string OsUserId,
    string MachineId,
    Guid InstallGuid,
    string? AppVersion);
