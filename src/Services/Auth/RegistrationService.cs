using Core.Enums.Auth;
using Core.Models.Auth;
using Infrastructure;
using Infrastructure.Entities.Auth;
using Infrastructure.Interfaces.Auth;
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
public class RegistrationService : IRegistrationService
{
    private readonly AppDbContext _db;
    private readonly IBootstrapTokenService _bootstrapTokens;
    private readonly IInstallationCredentialService _credentials;
    private readonly IInstallationRepository _installations;
    private readonly IRegistrationEventRepository _events;
    private readonly TimeProvider _time;

    public RegistrationService(
        AppDbContext db,
        IBootstrapTokenService bootstrapTokens,
        IInstallationCredentialService credentials,
        IInstallationRepository installations,
        IRegistrationEventRepository events,
        TimeProvider? time = null)
    {
        _db = db;
        _bootstrapTokens = bootstrapTokens;
        _credentials = credentials;
        _installations = installations;
        _events = events;
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
            return await CommitSuccessAsync(token!, descriptor!, request, now, ct)
                .ConfigureAwait(false);
        }

        return await CommitFailureAsync(request, now, outcome, ct).ConfigureAwait(false);
    }

    private static RegistrationOutcome ClassifyOutcome(RegisterRequest request,
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
        if (descriptor is null)
        {
            return RegistrationOutcome.DescriptorMalformed;
        }
        if (!string.Equals(token.ClientApp, descriptor.ClientApp, StringComparison.Ordinal))
        {
            return RegistrationOutcome.ClientScopeMismatch;
        }
        return RegistrationOutcome.Success;
    }

    private static InstallationDescriptor? TryBuildDescriptor(RegisterRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.ClientApp))
        {
            return null;
        }
        if (string.IsNullOrWhiteSpace(r.OsUserId))
        {
            return null;
        }
        if (string.IsNullOrWhiteSpace(r.MachineId))
        {
            return null;
        }
        if (r.InstallGuid is not Guid g)
        {
            return null;
        }
        if (r.AppVersion is not null && string.IsNullOrWhiteSpace(r.AppVersion))
        {
            return null;
        }

        return new InstallationDescriptor(r.ClientApp, r.OsUserId, r.MachineId, g,
            r.AppVersion?.Trim());
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

        return new RegistrationResult.Success(installEntity.Id, plaintext, now);
    }

    private async Task<RegistrationResult> CommitFailureAsync(RegisterRequest request,
        DateTime now, RegistrationOutcome outcome, CancellationToken ct)
    {
        RegistrationEventEntity failureEvent = BuildEvent(request, now, outcome,
            resultingInstallationId: null);
        await _events.AddAsync(failureEvent, ct).ConfigureAwait(false);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);

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
