namespace Services.Auth;

/// <summary>
/// Per-<c>clientApp</c> presence policy for descriptor fields whose
/// availability is platform-dependent. Looked up by
/// <c>RegistrationService</c> via an
/// <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey,TValue}"/>
/// keyed on the request's <c>clientApp</c> string; a lookup miss
/// (unknown <c>clientApp</c>) is treated as a scope mismatch and the
/// request is rejected with 401 (conflated with the existing
/// unknown-token / scope-mismatch 401, per the narrowed FR-002).
/// </summary>
/// <remarks>
/// The record intentionally has only two bools. <c>clientApp</c>
/// required-ness is enforced by the lookup mechanism itself
/// (a missing or unknown <c>clientApp</c> can never match a policy).
/// <c>installGuid</c> required-ness is a contract-level invariant —
/// every platform can generate a random 128-bit GUID client-side, so
/// per-policy nullability has no realistic consumer; the universal
/// non-empty rule lives at the <see cref="Core.Enums.Auth.RegistrationOutcome.InstallGuidInvalid"/>
/// layer instead.
/// </remarks>
public sealed record DescriptorPolicy(
    bool OsUserIdRequired,
    bool MachineIdRequired);
