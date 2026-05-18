namespace Core.Enums.Auth;

/// <summary>
/// Server-side categorisation of a <c>POST /register</c> attempt,
/// recorded on every <c>RegistrationEvent</c>. Per the narrowed FR-002
/// (clarification 2026-05-18), only the three scope-related outcomes —
/// <see cref="TokenInvalid"/>, <see cref="ClientScopeMismatch"/>, and
/// the policy-lookup-miss branch — are conflated into 401 on the wire.
/// Every other outcome maps to its own status code per
/// <c>contracts/register.md</c>. The full outcome value is still
/// recorded in the audit log on every attempt.
/// </summary>
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
    InstallGuidInvalid,
    DescriptorMissingField,
    AuditFailure
}
