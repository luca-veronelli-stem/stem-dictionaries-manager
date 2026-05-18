namespace Services.Interfaces.Auth;

/// <summary>
/// Wire-flat input for <see cref="IRegistrationService.RegisterAsync"/>.
/// Strings are nullable so the service can classify malformed payloads
/// (<c>DescriptorMalformed</c>, <c>TokenMissing</c>) without the API
/// layer doing partial validation. <paramref name="DescriptorJson"/>
/// is the raw body captured pre-parse for the audit trail.
/// </summary>
public sealed record RegisterRequest(
    string? BootstrapTokenPlaintext,
    string? ClientApp,
    string? OsUserId,
    string? MachineId,
    Guid? InstallGuid,
    string? AppVersion,
    string? DescriptorJson,
    string SourceIp);

/// <summary>
/// Discriminated result of <see cref="IRegistrationService.RegisterAsync"/>.
/// The success case carries the plaintext API credential which must be
/// returned to the client exactly once. The failure case carries the
/// classified <see cref="Core.Enums.Auth.RegistrationOutcome"/> so the
/// endpoint can map it to the RFC-meaningful status code documented in
/// <c>contracts/register.md</c> (per the narrowed FR-002: 401 conflates
/// only the three scope-related failure modes — token unknown,
/// scope-mismatch, unknown clientApp — and every other failure uses
/// its own status: 400 / 409 / 410 / 423).
/// </summary>
public abstract record RegistrationResult
{
    private RegistrationResult() { }

    public sealed record Success(int InstallationId, string ApiCredentialPlaintext,
        DateTime IssuedAt) : RegistrationResult;

    public sealed record Failure(Core.Enums.Auth.RegistrationOutcome Outcome) : RegistrationResult;
}

/// <summary>
/// Orchestrates <c>POST /register</c>: token validation, installation +
/// credential creation, audit write — all under one atomic transaction
/// per <c>data-model.md</c> invariant 3.
/// </summary>
public interface IRegistrationService
{
    /// <summary>
    /// Validates the bootstrap token and descriptor, on success creates an
    /// <c>Installation</c> + <c>InstallationApiCredential</c> + transitions
    /// the token to <c>Used</c> + writes the <c>RegistrationEvent</c>; on
    /// failure writes only the <c>RegistrationEvent</c>. An audit-write
    /// failure propagates as an exception so the endpoint can return 500.
    /// </summary>
    Task<RegistrationResult> RegisterAsync(RegisterRequest request,
        CancellationToken ct = default);
}
