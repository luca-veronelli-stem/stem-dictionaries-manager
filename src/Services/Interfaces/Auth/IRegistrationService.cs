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
/// Per FR-002 the endpoint never reveals failure detail to the client —
/// the unified 401 body is emitted regardless of which failure case
/// fired. The success case carries the plaintext API credential which
/// must be returned to the client exactly once.
/// </summary>
public abstract record RegistrationResult
{
    private RegistrationResult() { }

    public sealed record Success(int InstallationId, string ApiCredentialPlaintext,
        DateTime IssuedAt) : RegistrationResult;

    public sealed record Failure() : RegistrationResult;
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
