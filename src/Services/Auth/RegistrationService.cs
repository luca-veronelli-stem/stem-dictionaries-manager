using System.Text.RegularExpressions;
using Core.Enums.Auth;
using Core.Models.Auth;
using Infrastructure;
using Infrastructure.Entities.Auth;
using Infrastructure.Interfaces.Auth;
using Microsoft.Extensions.Logging;
using Services.Interfaces.Auth;

namespace Services.Auth;

/// <summary>
/// Orchestrates <c>POST /register</c>. Token lookup, descriptor
/// validation, and outcome classification happen in memory; the success
/// path then commits an <c>Installation</c> + <c>InstallationApiCredential</c>
/// + token state transition + <c>RegistrationEvent</c> in a single
/// explicit transaction (data-model invariant 3 — audit-or-no-issue).
/// The failure path commits only the <c>RegistrationEvent</c>; if that
/// throws, the exception propagates so the endpoint returns 500
/// (FR-013) — never 401, which would falsely tell the client their
/// token is bad when in fact the server failed.
/// </summary>
public partial class RegistrationService : IRegistrationService
{
    /// <summary>
    /// Canonical SemVer 2.0 regex from https://semver.org/. Used to validate
    /// `appVersion` at the request-processing layer; malformed values become
    /// <see cref="RegistrationOutcome.DescriptorMalformed"/> → 400. Source-
    /// generated for compile-time validation and zero-allocation execution.
    /// </summary>
    [GeneratedRegex(@"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-((?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+([0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$")]
    private static partial Regex SemVer2Regex();

    private readonly AppDbContext _db;
    private readonly IBootstrapTokenService _bootstrapTokens;
    private readonly IInstallationCredentialService _credentials;
    private readonly IInstallationRepository _installations;
    private readonly IRegistrationEventRepository _events;
    private readonly IReadOnlyDictionary<string, DescriptorPolicy> _descriptorPolicies;
    private readonly TimeProvider _time;
    private readonly ILogger<RegistrationService> _logger;

    public RegistrationService(
        AppDbContext db,
        IBootstrapTokenService bootstrapTokens,
        IInstallationCredentialService credentials,
        IInstallationRepository installations,
        IRegistrationEventRepository events,
        IReadOnlyDictionary<string, DescriptorPolicy> descriptorPolicies,
        ILogger<RegistrationService> logger,
        TimeProvider? time = null)
    {
        _db = db;
        _bootstrapTokens = bootstrapTokens;
        _credentials = credentials;
        _installations = installations;
        _events = events;
        _descriptorPolicies = descriptorPolicies;
        _logger = logger;
        _time = time ?? TimeProvider.System;
    }

    public async Task<RegistrationResult> RegisterAsync(RegisterRequest request,
        CancellationToken ct = default)
    {
        DateTime now = _time.GetUtcNow().UtcDateTime;

        BootstrapToken? token = string.IsNullOrEmpty(request.BootstrapTokenPlaintext)
            ? null
            : await _bootstrapTokens.LookupAsync(request.BootstrapTokenPlaintext, ct)
                .ConfigureAwait(false);

        InstallationDescriptor? descriptor = TryBuildDescriptor(request);
        RegistrationOutcome outcome = ClassifyOutcome(request, token, descriptor, now);

        if (outcome == RegistrationOutcome.Success)
        {
            // Spec 002 (#71): when the descriptor's InstallGuid matches an
            // existing Installation row, branch out of the first-time
            // insert path and either reject (cross-app reuse / revoked
            // installation) or re-register atomically (slice 6).
            InstallationEntity? existing = await _installations
                .FindByInstallGuidAsync(descriptor!.InstallGuid, ct)
                .ConfigureAwait(false);
            if (existing is not null)
            {
                if (!string.Equals(existing.ClientApp, descriptor.ClientApp,
                    StringComparison.Ordinal))
                {
                    // Cross-app InstallGuid reuse — conflated 401, no
                    // mutation outside the audit row.
                    return await CommitFailureAsync(request, now,
                        RegistrationOutcome.ClientScopeMismatch, ct)
                        .ConfigureAwait(false);
                }
                if (existing.Status != InstallationStatus.Active)
                {
                    // Revoked installation — server-only outcome, same
                    // conflated 401 wire shape.
                    return await CommitFailureAsync(request, now,
                        RegistrationOutcome.ExistingInstallationRevoked, ct)
                        .ConfigureAwait(false);
                }
                // Active existing installation — atomic re-registration
                // (option B from #71): revoke prior credentials, issue a
                // new one against the same Installation row, transition
                // the bootstrap token, audit with ReRegistrationSuccess.
                return await CommitReRegistrationAsync(existing, token!, request,
                    now, ct).ConfigureAwait(false);
            }
            return await CommitSuccessAsync(token!, descriptor, request, now, ct)
                .ConfigureAwait(false);
        }

        return await CommitFailureAsync(request, now, outcome, ct).ConfigureAwait(false);
    }

    private RegistrationOutcome ClassifyOutcome(RegisterRequest request,
        BootstrapToken? token, InstallationDescriptor? descriptor, DateTime now)
    {
        if (string.IsNullOrEmpty(request.BootstrapTokenPlaintext))
        {
            return RegistrationOutcome.TokenMissing;
        }
        if (token is null)
        {
            return RegistrationOutcome.TokenInvalid;
        }
        // Non-race terminal states must surface before the expiry check so a
        // Used-and-now-expired token still maps to TokenAlreadyUsed (the
        // actionable user-facing message), not TokenExpired (#58). The
        // race-loser path in CommitSuccessAsync remains a second line of
        // defence for tokens that flip between LookupAsync and MarkUsedAsync.
        if (token.Status == BootstrapTokenStatus.Used)
        {
            return RegistrationOutcome.TokenAlreadyUsed;
        }
        if (token.Status == BootstrapTokenStatus.Revoked)
        {
            return RegistrationOutcome.TokenRevoked;
        }
        if (token.IsExpiredAt(now))
        {
            return RegistrationOutcome.TokenExpired;
        }
        // Guid.Empty has its own distinct outcome (400) so a buggy client
        // (hardcoded Guid.Empty, defaulted default(Guid)) surfaces on the
        // first attempt instead of via the unique-index 500 on the second.
        if (request.InstallGuid is Guid g && g == Guid.Empty)
        {
            return RegistrationOutcome.InstallGuidInvalid;
        }
        // Policy lookup is also the clientApp-required check: a missing,
        // empty, or unknown clientApp fails the lookup and falls into the
        // conflated 401 path. The token's recorded scope and the request's
        // claimed clientApp must also match — that mismatch shares the same
        // 401 to hide which apps a token was scoped to.
        if (string.IsNullOrWhiteSpace(request.ClientApp)
            || !_descriptorPolicies.TryGetValue(request.ClientApp, out DescriptorPolicy? policy))
        {
            return RegistrationOutcome.ClientScopeMismatch;
        }
        if (!string.Equals(token.ClientApp, request.ClientApp, StringComparison.Ordinal))
        {
            return RegistrationOutcome.ClientScopeMismatch;
        }
        // Per-policy field-presence checks. The DescriptorPolicy decides
        // whether the client must transmit OsUserId / MachineId; storage
        // accepts nulls when the active policy permits.
        if (policy.OsUserIdRequired && string.IsNullOrWhiteSpace(request.OsUserId))
        {
            return RegistrationOutcome.DescriptorMissingField;
        }
        if (policy.MachineIdRequired && string.IsNullOrWhiteSpace(request.MachineId))
        {
            return RegistrationOutcome.DescriptorMissingField;
        }
        // Anything else that broke descriptor construction (unparseable
        // InstallGuid type, bad SemVer appVersion) lands here.
        if (descriptor is null)
        {
            return RegistrationOutcome.DescriptorMalformed;
        }
        return RegistrationOutcome.Success;
    }

    private static InstallationDescriptor? TryBuildDescriptor(RegisterRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.ClientApp))
        {
            return null;
        }
        if (r.InstallGuid is not Guid g)
        {
            return null;
        }

        string? appVersion = r.AppVersion?.Trim();
        if (appVersion is not null && !SemVer2Regex().IsMatch(appVersion))
        {
            return null;
        }

        // OsUserId / MachineId presence is enforced upstream by the
        // per-clientApp DescriptorPolicy. Normalize whitespace / empty
        // strings to null so storage records the absence faithfully.
        string? osUserId = string.IsNullOrWhiteSpace(r.OsUserId) ? null : r.OsUserId;
        string? machineId = string.IsNullOrWhiteSpace(r.MachineId) ? null : r.MachineId;

        return new InstallationDescriptor(r.ClientApp, osUserId, machineId, g, appVersion);
    }

    private async Task<RegistrationResult> CommitSuccessAsync(BootstrapToken token,
        InstallationDescriptor descriptor, RegisterRequest request, DateTime now,
        CancellationToken ct)
    {
        // Explicit transaction: install row needs a SaveChangesAsync to populate
        // its identity column before downstream FKs can reference it. Wrapping
        // in BeginTransactionAsync turns the two saves into one atomic commit
        // unit — invariant 3 holds across both.
        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction txn =
            await _db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

        InstallationEntity installEntity = new()
        {
            ClientApp = descriptor.ClientApp,
            OsUserId = descriptor.OsUserId,
            MachineId = descriptor.MachineId,
            InstallGuid = descriptor.InstallGuid,
            AppVersion = descriptor.AppVersion,
            DescriptorJson = request.DescriptorJson ?? "{}",
            RegisteredAt = now,
            Status = InstallationStatus.Active
        };
        await _installations.AddAsync(installEntity, ct).ConfigureAwait(false);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        (_, string plaintext) = await _credentials
            .IssueAsync(installEntity.Id, now, ct).ConfigureAwait(false);

        try
        {
            await _bootstrapTokens.MarkUsedAsync(token.Id, installEntity.Id, now, ct)
                .ConfigureAwait(false);
        }
        catch (BootstrapTokenStateException ex)
        {
            // Race-loser: a concurrent /register on the same token already
            // flipped the row out of Issued between our LookupAsync read and
            // the MarkUsedAsync re-read inside this transaction. Roll back
            // the in-flight installation + credential, then write a unified
            // failure audit so the endpoint emits the FR-002 401 (SC-003 —
            // exactly one installation per token).
            await txn.RollbackAsync(ct).ConfigureAwait(false);
            _db.ChangeTracker.Clear();
            RegistrationOutcome raceOutcome = ex.FoundStatus == BootstrapTokenStatus.Revoked
                ? RegistrationOutcome.TokenRevoked
                : RegistrationOutcome.TokenAlreadyUsed;
            return await CommitFailureAsync(request, now, raceOutcome, ct)
                .ConfigureAwait(false);
        }

        RegistrationEventEntity successEvent = BuildEvent(request, now,
            RegistrationOutcome.Success, installEntity.Id);
        await _events.AddAsync(successEvent, ct).ConfigureAwait(false);

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        await txn.CommitAsync(ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Registered installation {InstallationId} for client app {ClientApp}",
            installEntity.Id, descriptor.ClientApp);

        return new RegistrationResult.Success(installEntity.Id, plaintext, now);
    }

    /// <summary>
    /// Atomic re-registration on an existing Active Installation
    /// (spec 002 / #71, option B). Revokes every Active credential,
    /// issues a fresh one against the same Installation row,
    /// transitions the bootstrap token <c>Issued → Used</c>, and
    /// writes a <see cref="RegistrationOutcome.ReRegistrationSuccess"/>
    /// audit row — all in one transaction (data-model invariant 3).
    /// </summary>
    /// <remarks>
    /// Known limitation: a concurrent re-registration race using two
    /// different bootstrap tokens against the same Installation will
    /// have one loser hit the filtered unique index on
    /// <c>InstallationApiCredentials.InstallationId WHERE Status = Active</c>
    /// at <c>AppDbContext.SaveChangesAsync</c> time. The
    /// resulting <c>DbUpdateException</c> propagates up to the endpoint
    /// catch and surfaces as a logged 500 (FR-008). A future PR may
    /// reclassify that specific exception as a race outcome.
    /// </remarks>
    private async Task<RegistrationResult> CommitReRegistrationAsync(
        InstallationEntity existing, BootstrapToken token,
        RegisterRequest request, DateTime now, CancellationToken ct)
    {
        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction txn =
            await _db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

        await _credentials.RevokeActiveAsync(existing.Id, now, ct).ConfigureAwait(false);
        (_, string plaintext) = await _credentials
            .IssueAsync(existing.Id, now, ct).ConfigureAwait(false);

        try
        {
            await _bootstrapTokens.MarkUsedAsync(token.Id, existing.Id, now, ct)
                .ConfigureAwait(false);
        }
        catch (BootstrapTokenStateException ex)
        {
            // Race-loser: same shape as CommitSuccessAsync. Roll back the
            // in-flight revoke + new-credential insert, clear the tracker,
            // and audit the race outcome.
            await txn.RollbackAsync(ct).ConfigureAwait(false);
            _db.ChangeTracker.Clear();
            RegistrationOutcome raceOutcome = ex.FoundStatus == BootstrapTokenStatus.Revoked
                ? RegistrationOutcome.TokenRevoked
                : RegistrationOutcome.TokenAlreadyUsed;
            return await CommitFailureAsync(request, now, raceOutcome, ct)
                .ConfigureAwait(false);
        }

        RegistrationEventEntity reRegEvent = BuildEvent(request, now,
            RegistrationOutcome.ReRegistrationSuccess, existing.Id);
        await _events.AddAsync(reRegEvent, ct).ConfigureAwait(false);

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        await txn.CommitAsync(ct).ConfigureAwait(false);

        _logger.LogInformation(
            "Re-registered installation {InstallationId} for client app {ClientApp}",
            existing.Id, existing.ClientApp);

        return new RegistrationResult.Success(existing.Id, plaintext, now);
    }

    private async Task<RegistrationResult> CommitFailureAsync(RegisterRequest request,
        DateTime now, RegistrationOutcome outcome, CancellationToken ct)
    {
        RegistrationEventEntity failureEvent = BuildEvent(request, now, outcome,
            resultingInstallationId: null);
        await _events.AddAsync(failureEvent, ct).ConfigureAwait(false);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogWarning(
            "Registration rejected for client app {ClientApp}: {Outcome}",
            request.ClientApp, outcome);

        return new RegistrationResult.Failure(outcome);
    }

    private static RegistrationEventEntity BuildEvent(RegisterRequest request,
        DateTime now, RegistrationOutcome outcome, int? resultingInstallationId)
        => new()
        {
            OccurredAt = now,
            ClaimedClientApp = request.ClientApp,
            ClaimedOsUserId = request.OsUserId,
            ClaimedMachineId = request.MachineId,
            ClaimedInstallGuid = request.InstallGuid,
            ClaimedAppVersion = request.AppVersion,
            SourceIp = request.SourceIp,
            DescriptorJson = request.DescriptorJson,
            Outcome = outcome,
            ResultingInstallationId = resultingInstallationId
        };
}
