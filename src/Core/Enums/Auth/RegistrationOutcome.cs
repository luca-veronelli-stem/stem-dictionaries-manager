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
/// <remarks>
/// Append new values at the end. Existing rows in
/// <c>RegistrationEvents.Outcome</c> are stored by ordinal; reordering
/// or removing values would silently rewrite historical audit
/// categorisation.
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
    InstallGuidInvalid,
    DescriptorMissingField,
    AuditFailure,
    /// <summary>
    /// Re-registration happy path (spec 002 / #71): a fresh bootstrap
    /// token presented against an existing <c>Active</c> Installation
    /// with matching <c>ClientApp</c>. Server-only — wire response is
    /// identical to <see cref="Success"/> (200 + new credential body).
    /// Lets operators query the audit log for re-registrations
    /// distinctly from first-time successes.
    /// </summary>
    ReRegistrationSuccess,
    /// <summary>
    /// Re-registration rejected because the matched Installation row's
    /// own <c>Status</c> is <c>Revoked</c> (spec 002 / #71). Server-only
    /// — wire response is identical to <see cref="ClientScopeMismatch"/>
    /// (conflated 401 + standard failure body) to preserve the FR-002
    /// no-info-leak invariant. The Installation is NOT auto-unrevoked;
    /// recovery requires a separate admin flow.
    /// </summary>
    ExistingInstallationRevoked
}
