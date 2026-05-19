# Phase 1 — Data Model (delta vs spec 001)

**Feature**: Atomic re-registration on existing installation
**Branch**: `fix/71-register-reregistration`
**Date**: 2026-05-19

This document is a **delta** against
[`specs/001-bootstrap-registration/data-model.md`](../001-bootstrap-registration/data-model.md).
Read 001's data-model first. Only the changes introduced by spec 002
are listed here.

## Entity changes

### `InstallationApiCredential` — lifecycle now 1:N with at-most-one-Active

Spec 001 stated:

> One credential per Installation in this feature. Revoking the
> Installation revokes the credential atomically in the same
> transaction. (Future feature: key rotation will introduce
> many-credentials-per-Installation, deprecating this 1:1 invariant;
> not in scope here.)

Spec 002 promotes that "future feature" to current scope (limited
form). The new lifecycle:

```text
Installation 1 ────── 0..n InstallationApiCredential
                            (∃ at most one with Status = Active
                             at any instant)
```

- One Installation row holds **zero or more** credentials over its
  lifetime.
- At most one of those is in `Status = Active`; the rest are
  `Revoked` (historical, kept for forensics — the `SecretHash` is
  preserved).
- The Active row is the one the `InstallationCredentialValidator`
  honours.

**New invariant** (DB-level): a unique index on `InstallationId`
filtered to `Status = Active`. Implementation in
`AppDbContext.OnModelCreating`:

```csharp
modelBuilder.Entity<InstallationApiCredentialEntity>()
    .HasIndex(c => c.InstallationId)
    .HasFilter("[Status] = 0")   // Active = 0 in the InstallationStatus enum
    .IsUnique();
```

Generates `CREATE UNIQUE INDEX ... WHERE [Status] = 0` on both SQL
Server and SQLite (EF Core 10 verified). The migration
`MultiActiveCredentialPerInstallationGuard` carries the change. No
data migration is needed — all existing rows from spec 001 are 1:1.

### `RegistrationOutcome` — two new values

Appended to the existing enum in
`src/Core/Enums/Auth/RegistrationOutcome.cs`:

| Value | When | Wire status |
|---|---|---|
| `ReRegistrationSuccess` | Re-registration happy path — fresh token + existing `Active` Installation with matching ClientApp. | `200 OK` (same body shape as `Success`). |
| `ExistingInstallationRevoked` | The matched Installation has `Status = Revoked`. Request is rejected. | `401 Unauthorized` (conflated body with `TokenInvalid` / `ClientScopeMismatch`). |

Both are **server-only**: the wire response does not distinguish them
from existing outcomes (FR-002 no-info-leak invariant preserved). The
distinction is only visible in the `RegistrationEvents.Outcome`
column.

### `RegistrationResult` — unchanged

The service still returns `RegistrationResult.Success(installationId,
plaintext, issuedAt)` for both first-time and re-registration. The
endpoint maps `RegistrationResult.Success` to 200 either way. The
audit-row outcome (set inside `CommitReRegistrationAsync`) carries
the discriminator.

## Service-layer surface change

### `IInstallationCredentialService` — new method

```csharp
/// <summary>
/// Flips every <see cref="InstallationStatus.Active"/> credential on
/// the matched installation to <see cref="InstallationStatus.Revoked"/>,
/// setting <see cref="InstallationApiCredentialEntity.RevokedAt"/>.
/// Returns the number of rows flipped.
///
/// Does NOT mutate the parent <see cref="InstallationEntity"/>.
/// Does NOT call <see cref="AppDbContext.SaveChangesAsync"/> — the
/// caller batches the revoke into the surrounding transaction.
/// </summary>
Task<int> RevokeActiveAsync(int installationId, DateTime revokedAt,
    CancellationToken ct = default);
```

Reusable from:

- This PR's `RegistrationService.CommitReRegistrationAsync` (the
  re-registration revoke).
- Issue #68's future `RevokeInstallationAsync` (the admin endpoint —
  not in this PR's scope).

## Repository-layer surface change

### `IInstallationRepository` — new method

```csharp
Task<InstallationEntity?> FindByInstallGuidAsync(Guid installGuid,
    CancellationToken ct = default);
```

Returns the single Installation row matching the supplied
`InstallGuid` (the existing unique index on `InstallGuid` guarantees
0 or 1 match), or null when none exists. Used by
`RegistrationService.RegisterAsync` to detect the re-registration
case.

### `IInstallationApiCredentialRepository` — new method

```csharp
Task<IReadOnlyList<InstallationApiCredentialEntity>>
    ListActiveByInstallationIdAsync(int installationId,
        CancellationToken ct = default);
```

Returns every `Status = Active` credential for the matched
installation. The filtered unique index above means the list is
either empty or of length 1, but the method returns a list to keep
the surface honest against the schema-permitted multiplicity (zero
or more historical Revoked rows, zero or one Active).

The existing `GetByInstallationIdAsync(int)` method is left in place
but is now stale (returns the first match regardless of status, no
production caller). It is **not** removed in this PR — out of scope
unless a future refactor pulls it.

## Cross-cutting invariant updates

The five preservation theorems from spec 001's data-model still
apply, with one extension:

**6. At-most-one-Active credential per Installation**: at any
instant, at most one row in `InstallationApiCredentials` has
`InstallationId = X AND Status = Active` for any installation `X`.
Enforced by the filtered unique index above. The first-time
registration path naturally upholds the invariant (single insert).
The re-registration path upholds it because `RevokeActiveAsync` runs
**before** `IssueAsync` within the same transaction — the prior
Active row is flipped to Revoked before the new Active row is
inserted.

Invariants 1–5 from spec 001 are inherited verbatim; the audit-or-no-
issue invariant (3) now applies to the re-registration outcome too —
a `ReRegistrationSuccess` audit row commits in the same transaction
as the credential flip + insert + token transition.

## Migration

```powershell
dotnet ef migrations add MultiActiveCredentialPerInstallationGuard `
    -p src/Infrastructure -s src/API
```

Generated migration adds one filtered unique index. No alterations to
existing tables, no data migration. Idempotent on a re-run (EF Core's
guard).
