using Core.Enums.Auth;

namespace Core.Models.Auth;

/// <summary>
/// Thrown when a <see cref="BootstrapToken"/> state transition is attempted
/// from a non-<see cref="BootstrapTokenStatus.Issued"/> state — typically
/// the race-loser of a concurrent <c>POST /register</c> on the same token,
/// where the second caller observes the row already in
/// <see cref="BootstrapTokenStatus.Used"/> or
/// <see cref="BootstrapTokenStatus.Revoked"/>. <see cref="FoundStatus"/>
/// lets <c>RegistrationService</c> classify the audit outcome (data-model
/// invariant 1; spec FR-002 / SC-003).
/// </summary>
/// <remarks>
/// Inherits from <see cref="InvalidOperationException"/> so callers that
/// catch the broader type (existing tests, defensive call sites) keep
/// working while the registration flow can catch this specific subtype.
/// </remarks>
public sealed class BootstrapTokenStateException : InvalidOperationException
{
    public BootstrapTokenStatus FoundStatus { get; }

    public BootstrapTokenStateException(BootstrapTokenStatus foundStatus, string message)
        : base(message)
    {
        FoundStatus = foundStatus;
    }
}
