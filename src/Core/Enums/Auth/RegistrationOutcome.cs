namespace Core.Enums.Auth;

/// <summary>
/// Server-side categorisation of a <c>POST /register</c> attempt,
/// recorded on every <c>RegistrationEvent</c>.
/// </summary>
/// <remarks>
/// Per FR-002, this value is never disclosed to the client; the wire
/// response is always the unified 401 body for any non-<see cref="Success"/>
/// outcome (or a 500 on <see cref="AuditFailure"/>, which is a sentinel
/// not persisted to the DB).
/// </remarks>
public enum RegistrationOutcome
{
    Success,
    TokenMissing,
    TokenInvalid,
    TokenAlreadyUsed,
    TokenExpired,
    TokenRevoked,
    ClientScopeMismatch,
    DescriptorMalformed,
    AuditFailure
}
