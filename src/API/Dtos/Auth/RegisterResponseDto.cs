namespace API.Dtos.Auth;

/// <summary>
/// Wire shape of the <c>POST /register</c> success response. The
/// <see cref="ApiCredential"/> plaintext is returned exactly once and
/// never logged (data-model invariant 4).
/// </summary>
public sealed record RegisterResponseDto(int InstallationId, string ApiCredential, DateTime IssuedAt);
