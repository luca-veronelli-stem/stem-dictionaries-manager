# Implementation Plan: Atomic re-registration on existing installation

**Branch**: `fix/71-register-reregistration` | **Date**: 2026-05-19 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `specs/002-register-reregistration/spec.md`

> **Update — #85 (0.9.1):** this #71-era plan documents
> `ExistingInstallationRevoked` as mapping to the conflated **401** path.
> Issue #85 reversed that: the 2026-05-18 FR-002 narrowing made the outcome
> distinguishable (it fires only after token + scope validation), so it now
> maps to **`423 Locked`**. Treat any `401` for the revoked-installation
> case below as historical; see
> `specs/001-bootstrap-registration/contracts/register.md` for the current
> mapping.

## Summary

Issue #71 of `stem-dictionaries-manager`: when `POST /register` receives
a fresh bootstrap token that classifies as `Success` and the request's
`InstallGuid` already exists, the API must atomically revoke all
active credentials on the matched `Installation`, issue a new one,
mark the token `Used`, and audit the event — all in one transaction
(option B from the issue). Separately, the endpoint's broad catch
must log every exception it swallows so future 500s are diagnosable
from the application log alone.

Technical approach (full rationale in
[`research.md`](./research.md)):

- **Re-registration branch** lives in `RegistrationService`, between
  the existing `ClassifyOutcome → Success` check and the existing
  `CommitSuccessAsync` call. The branch routes by the result of a new
  `InstallationRepository.FindByInstallGuidAsync` lookup.
- **Credential revocation primitive** lands on
  `IInstallationCredentialService` as `RevokeActiveAsync(int
  installationId, DateTime revokedAt, CancellationToken)` returning
  the count of rows flipped. The Installation row itself stays
  untouched — re-registration only swaps the credential. Issue #68's
  future admin endpoint will own its own `RevokeInstallationAsync`
  (separate responsibility: flip `Installation.Status`, then call
  `RevokeActiveAsync` for the credentials, then audit + cache
  invalidation). Pinning addresses spec-review Finding 1.
- **Two new server-only `RegistrationOutcome` values**:
  `ReRegistrationSuccess` (audits the happy path, wire response
  identical to `Success` → 200), and `ExistingInstallationRevoked`
  (audits the FR-003 reject case, wire response identical to
  `ClientScopeMismatch` → conflated 401). Both preserve the FR-002
  no-info-leak invariant from spec 001. Addresses spec-review
  Finding 2.
- **Multi-row-per-Installation invariant** is enforced at the DB level
  via a *filtered unique index* on
  `InstallationApiCredentials.InstallationId WHERE Status = Active`
  (one EF migration, supported on both SQLite and SQL Server). Without
  this, a concurrent two-token race against the same Installation
  could leave two `Active` credentials behind.
- **Endpoint logging** uses `ILogger<RegistrationEndpoints>` injected
  via the existing `WebApplication.Services.GetRequiredService<T>()`
  composition pattern (no new infrastructure).

## Technical Context

**Language/Version**: C# 13 on .NET 10 (`net10.0`); `<Nullable>enable</Nullable>` per BUILD_CONFIG.
**Primary Dependencies**: existing — ASP.NET Core minimal APIs, EF Core 10 (Sqlite + SqlServer providers), `Microsoft.Extensions.Caching.Memory`, `Microsoft.Extensions.Logging.Abstractions`. No new packages.
**Storage**: existing `AppDbContext`. One EF migration `MultiActiveCredentialPerInstallationGuard` adds the filtered unique index above. No schema changes to `InstallationApiCredentials` columns (already has `Status` and `RevokedAt` from spec 001).
**Testing**: xUnit in `tests/Tests/Tests.csproj`. New tests under:
- `tests/Tests/Integration/API/Auth/RegisterEndpointTests.cs` — re-registration HTTP paths (US1 acceptance scenarios + edge cases).
- `tests/Tests/Integration/Services/Auth/RegistrationServiceTests.cs` — service-layer branches.
- `tests/Tests/Unit/Services/Auth/InstallationCredentialServiceTests.cs` — `RevokeActiveAsync` primitive (counts, idempotency, mixed Active/Revoked).
- A small `CapturingLoggerProvider` test helper for the FR-008 log-assertion case (US2). Manual fake, ≤30 LOC, lives under `tests/Tests/Unit/Services/Auth/Fakes/`.
Per Constitution Principle III: manual fakes only. Integration tests use SQLite-in-memory via existing `RegisterApiFactory`.
**Target Platform**: ASP.NET Core API server, cross-platform (`net10.0`). No `net10.0-windows` leg.
**Project Type**: Web service (existing `src/API`).
**Performance Goals**: SC-002 — re-registration revocation propagates within the existing 5 s `InstallationCredentialValidator` TTL (no new cache hook). Re-registration request end-to-end SHOULD stay within the same envelope as a first-time registration (single round-trip transaction).
**Constraints**: must preserve FR-002 (no info leak); audit-or-no-issue invariant (single transaction); response shape for already-classified failures (`TokenInvalid`, `TokenAlreadyUsed`, etc.) unchanged from spec 001.
**Scale/Scope**: small. The change adds ≈ 1 service method, ≈ 1 service branch, ≈ 2 repository methods, ≈ 1 EF migration, ≈ 2 enum entries, ≈ 4 unit tests, ≈ 4 integration tests, ≈ 2 doc updates.

## Constitution Check

Gates evaluated against `.specify/memory/constitution.md` v1.0.1.

| Principle / Section | Compliance |
|---|---|
| **I. Spec-Driven Development** | ✅ On the speckit pipeline: `specify` (no clarify needed — 0 markers) → `plan` (this file) → `tasks` (next). Spec describes WHAT/WHY only; HOW lives here and in the contract / data-model amendments. Branch `fix/71-register-reregistration` carries the issue-PR per `resolve-ticket` (one issue → one draft PR; no separate feature branch). |
| **II. STEM v1 Standards Are the Contract** | ✅ Module placement: new code lives under existing `src/Services/Auth/` (service branch, primitive), `src/Infrastructure/Repositories/Auth/` (FindByInstallGuid, ListActiveByInstallationId), `src/API/Endpoints/Auth/` (endpoint logger). Layers stay onion-shaped per REPO_STRUCTURE. No edits to `docs/Standards/*.md`. PORTABILITY: no banned-API introductions; pure layers stay pure. |
| **III. Test-First, Manual Fakes, Integration over Mocks** | ✅ Test-first per slice (RED test → minimal GREEN → next slice). Manual fakes only — extend `FakeInstallationApiCredentialRepository` with the new method. `CapturingLoggerProvider` is a manual fake (no Moq/NSubstitute). Integration tests reuse `RegisterApiFactory` (SQLite-in-memory). |
| **IV. Pragmatic .NET — Explicit, Nullable, Exceptional** | ✅ `Nullable=enable` inherited. Manual DI in `API/Program.cs` (one new line for the logger; the service registration is `AddScoped` already in place). Errors propagate via exceptions; the FR-008 catch logs-then-rethrows-shape (returns 500, doesn't throw past the endpoint). `ArgumentException.ThrowIfNullOrWhiteSpace` not needed (no new string params). Function bodies ≤ 15 LOC; the new `CommitReRegistrationAsync` mirrors `CommitSuccessAsync`'s shape. |
| **V. Workflow Discipline** | ✅ Single fix branch `fix/71-register-reregistration`. Conventional commits per `tasks.md`. Dual-remote in effect (mirror via Actions on merge to `main`). CI green-gate required on both `ubuntu-latest` and `windows-latest`. CHANGELOG.md `[Unreleased]` entry to land with the final work commit. Rebase merge planned. |
| **Security & Auditability** | ✅ FR-002 no-info-leak preserved: `ReRegistrationSuccess` returns the same `200` body as `Success`; `ExistingInstallationRevoked` returns the `{ "error": "registration failed" }` body with a `423 Locked` status (changed from the conflated `401` by #85 — a post-token+scope-validation outcome, so the distinct code leaks no scope info). Audit-or-no-issue: re-registration's revoke + issue + token-flip + audit row all commit in one `BeginTransactionAsync` block, mirroring `CommitSuccessAsync`. Banned APIs: none. |
| **Quality Gates** | Verified pre-PR: `dotnet format whitespace --verify-no-changes --no-restore`; `dotnet build -c Release` (warnings-as-errors); `dotnet test -c Release` on both OS legs. CHANGELOG.md entry under `[Unreleased]`. Standard version unchanged. |

**Result**: ✅ All gates pass. No deviations recorded; Complexity Tracking section is empty.

## Project Structure

### Documentation (this feature)

```text
specs/002-register-reregistration/
├── plan.md                                  # This file (/speckit-plan output)
├── spec.md                                  # /speckit-specify output (already in place)
├── research.md                              # Phase 0 — single design decision (this PR is mostly mechanical)
├── data-model.md                            # Phase 1 — delta against 001-bootstrap-registration's data-model
├── quickstart.md                            # Phase 1 — end-to-end smoke procedure
├── contracts/
│   └── register.md                          # Phase 1 — DELTA against 001's contracts/register.md (the actual file in 001 gets a new "Re-registration side effects" subsection committed in the work phase)
├── tasks.md                                 # Phase 2 — /speckit-tasks output (next)
└── checklists/
    └── requirements.md                      # Already in place from /speckit-specify
```

The work commits also touch:

- `specs/001-bootstrap-registration/contracts/register.md` — new "Re-registration" subsection.
- `specs/001-bootstrap-registration/data-model.md` — `InstallationApiCredential` lifecycle now explicitly multi-row-per-Installation; new outcome enum entries.

### Source Code (repository root)

```text
src/
├── Core/
│   └── Enums/Auth/RegistrationOutcome.cs                                  # + ReRegistrationSuccess, + ExistingInstallationRevoked
├── Infrastructure/
│   ├── Interfaces/Auth/
│   │   ├── IInstallationRepository.cs                                     # + FindByInstallGuidAsync
│   │   └── IInstallationApiCredentialRepository.cs                        # + ListActiveByInstallationIdAsync
│   ├── Repositories/Auth/
│   │   ├── InstallationRepository.cs                                      # + implementation of FindByInstallGuidAsync
│   │   └── InstallationApiCredentialRepository.cs                         # + ListActiveByInstallationIdAsync
│   ├── Entities/Auth/InstallationApiCredentialEntity.cs                   # (unchanged columns; the EF mapping is updated in AppDbContext for the filtered unique index)
│   ├── AppDbContext.cs                                                    # OnModelCreating: HasIndex(c => c.InstallationId).HasFilter("[Status] = 0").IsUnique()
│   └── Migrations/
│       ├── 20260519xxxxxx_MultiActiveCredentialPerInstallationGuard.cs   # filtered unique index
│       ├── 20260519xxxxxx_MultiActiveCredentialPerInstallationGuard.Designer.cs
│       └── AppDbContextModelSnapshot.cs                                   # regenerated
├── Services/
│   ├── Interfaces/Auth/IInstallationCredentialService.cs                  # + RevokeActiveAsync
│   ├── Auth/
│   │   ├── InstallationCredentialService.cs                               # + RevokeActiveAsync impl (≤ 15 LOC)
│   │   └── RegistrationService.cs                                         # + FindByInstallGuid branch in RegisterAsync; + CommitReRegistrationAsync
│   └── DependencyInjection.cs                                             # (no change — existing AddScoped covers new method)
└── API/
    ├── Endpoints/Auth/RegistrationEndpoints.cs                            # + ILogger<RegistrationEndpoints> param; replace parameterless catch with typed catch + LogError
    └── Program.cs                                                         # (no change — ILogger<T> is auto-registered)

tests/Tests/
├── Integration/
│   ├── API/Auth/
│   │   ├── RegisterEndpointTests.cs                                       # + 4 new tests (re-registration HTTP paths + FR-008 log assertion)
│   │   └── RegisterApiFactory.cs                                          # (extend if needed for CapturingLoggerProvider injection)
│   └── Services/Auth/
│       └── RegistrationServiceTests.cs                                    # + 4 new tests (service-layer branches)
└── Unit/Services/Auth/
    ├── InstallationCredentialServiceTests.cs                              # + 3 new tests (RevokeActiveAsync semantics)
    └── Fakes/
        ├── FakeInstallationApiCredentialRepository.cs                     # + ListActiveByInstallationIdAsync, + entry in seed bag
        ├── FakeInstallationRepository.cs                                  # + FindByInstallGuidAsync (if a fake exists; otherwise add minimal new file)
        └── CapturingLoggerProvider.cs                                     # NEW — ≤ 30 LOC, captures (level, exception, message) tuples; used by US2's FR-008 test
```

**Structure Decision**: extends `src/{Core,Services,Infrastructure,API}/Auth/` per 001-bootstrap-registration's onion layout. No new project; no new namespace. The new test helper `CapturingLoggerProvider` is the only added file outside `Auth/` subfolders.

## Phase 0 — Research

This PR is mostly mechanical extension of the spec-001 plumbing. The
only design question worth explicit research is the filtered-unique-
index decision on `InstallationApiCredentials`. See `research.md`
(written alongside this plan):

- **R1**: should the "at most one Active credential per Installation"
  invariant be enforced by a filtered unique index, by a serializable
  transaction, or by application-level re-check? Decision: **filtered
  unique index**, on both SQL Server (native) and SQLite (partial
  index, EF Core 10 generates correctly via `HasFilter("...")`).
  Rationale: cheapest correct enforcement; database-level invariant
  outlives any refactor of the service layer. Alternative considered:
  in-process lock — rejected (single-instance only; doesn't survive
  scale-out, even though scale-out is not on today's roadmap).

## Phase 1 — Design & Contracts

### Data-model delta vs spec 001

(Full content lands in `data-model.md`; pointers here.)

1. **`InstallationApiCredential` lifecycle**: 001 documented this as
   1:1 with Installation. Spec 002 makes it explicitly 1:N with at
   most one row in `Status = Active` at a time. Existing columns
   unchanged. New constraint: filtered unique index on
   `InstallationId WHERE Status = Active`.

2. **`RegistrationOutcome` enum** gains:
   - `ReRegistrationSuccess` — server-only audit value; wire response
     identical to `Success` (200).
   - `ExistingInstallationRevoked` — server-only audit value; wire
     response identical to `ClientScopeMismatch` (conflated 401 +
     standard failure body). Resolves spec-review Finding 2.

3. **`RegistrationResult` discriminated union** (in
   `Core/Models/Auth/RegistrationResult.cs`): unchanged. The service
   returns `RegistrationResult.Success(installationId, plaintext,
   issuedAt)` for both first-time and re-registration paths — the
   wire shape is identical. The audit-row outcome carries the
   discriminator.

### Service-layer surface

```csharp
// IInstallationCredentialService — new method
Task<int> RevokeActiveAsync(int installationId, DateTime revokedAt,
    CancellationToken ct = default);
```

Returns the number of rows flipped (0 when the installation has no
active credentials; ≥ 1 in normal re-registration). Idempotent: a
second call within the same transaction returns 0 and is a no-op.

Implementation in `InstallationCredentialService`:

```csharp
public async Task<int> RevokeActiveAsync(int installationId,
    DateTime revokedAt, CancellationToken ct = default)
{
    if (installationId <= 0)
    {
        throw new ArgumentOutOfRangeException(nameof(installationId));
    }
    IReadOnlyList<InstallationApiCredentialEntity> active =
        await _credentials.ListActiveByInstallationIdAsync(installationId, ct)
            .ConfigureAwait(false);
    foreach (InstallationApiCredentialEntity row in active)
    {
        row.Status = InstallationStatus.Revoked;
        row.RevokedAt = revokedAt;
        await _credentials.UpdateAsync(row, ct).ConfigureAwait(false);
    }
    return active.Count;
}
```

`SaveChangesAsync` is the caller's responsibility (consistent with the
rest of `IInstallationApiCredentialRepository` — Add/Update only
track changes). This is what lets the re-registration transaction
batch revoke + issue + token-flip + audit into a single commit.

### Re-registration branch in `RegistrationService.RegisterAsync`

```csharp
if (outcome == RegistrationOutcome.Success)
{
    InstallationEntity? existing = await _installations
        .FindByInstallGuidAsync(descriptor!.InstallGuid, ct).ConfigureAwait(false);
    if (existing is null)
    {
        return await CommitSuccessAsync(token!, descriptor, request, now, ct)
            .ConfigureAwait(false);
    }
    if (!string.Equals(existing.ClientApp, descriptor.ClientApp, StringComparison.Ordinal))
    {
        // Cross-app InstallGuid reuse — conflated 401, no row mutation
        return await CommitFailureAsync(request, now,
            RegistrationOutcome.ClientScopeMismatch, ct).ConfigureAwait(false);
    }
    if (existing.Status != InstallationStatus.Active)
    {
        return await CommitFailureAsync(request, now,
            RegistrationOutcome.ExistingInstallationRevoked, ct).ConfigureAwait(false);
    }
    return await CommitReRegistrationAsync(existing, token!, descriptor,
        request, now, ct).ConfigureAwait(false);
}
```

### `CommitReRegistrationAsync`

Mirrors `CommitSuccessAsync`'s transaction shape:

```csharp
await using IDbContextTransaction txn =
    await _db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

await _credentials.RevokeActiveAsync(existing.Id, now, ct).ConfigureAwait(false);
(_, string plaintext) = await _credentials.IssueAsync(existing.Id, now, ct).ConfigureAwait(false);

try
{
    await _bootstrapTokens.MarkUsedAsync(token.Id, existing.Id, now, ct)
        .ConfigureAwait(false);
}
catch (BootstrapTokenStateException ex)
{
    // Race-loser: token flipped between LookupAsync and MarkUsedAsync.
    // Roll back, write the race-outcome audit. Same shape as CommitSuccessAsync.
    await txn.RollbackAsync(ct).ConfigureAwait(false);
    _db.ChangeTracker.Clear();
    RegistrationOutcome raceOutcome = ex.FoundStatus == BootstrapTokenStatus.Revoked
        ? RegistrationOutcome.TokenRevoked
        : RegistrationOutcome.TokenAlreadyUsed;
    return await CommitFailureAsync(request, now, raceOutcome, ct).ConfigureAwait(false);
}

RegistrationEventEntity reRegEvent = BuildEvent(request, now,
    RegistrationOutcome.ReRegistrationSuccess, existing.Id);
await _events.AddAsync(reRegEvent, ct).ConfigureAwait(false);

await _db.SaveChangesAsync(ct).ConfigureAwait(false);
await txn.CommitAsync(ct).ConfigureAwait(false);

return new RegistrationResult.Success(existing.Id, plaintext, now);
```

### Endpoint logging

`RegistrationEndpoints.Register` accepts a new injected
`ILogger<RegistrationEndpoints>` parameter (or `ILoggerFactory` —
either works; the typed `ILogger<T>` is cleaner). The parameterless
catch becomes:

```csharp
catch (Exception ex)
{
    logger.LogError(ex,
        "Registration failed with unhandled exception (sourceIp={SourceIp}, clientApp={ClientApp}, installGuid={InstallGuid}).",
        sourceIp, dto?.Descriptor?.ClientApp, dto?.Descriptor?.InstallGuid);
    return RawJson(AuditFailureBody, StatusCodes.Status500InternalServerError);
}
```

Template fields use named parameters (per STEM LOGGING standard).
The structured fields are best-effort: when the body was unparseable,
they fall through as `null`/empty strings.

### Endpoint outcome → status

`StatusFor(RegistrationOutcome)` adds:

| Outcome | Status |
|---|---|
| `ReRegistrationSuccess` | `200 OK` (handled in the result-`Success` branch, not via `StatusFor` — the service returns `RegistrationResult.Success` for both happy paths) |
| `ExistingInstallationRevoked` | `423 Locked` (since #85; this #71-era plan specified `401` conflated, corrected by the #85 status-mapping fix) |

### Contract update

`specs/001-bootstrap-registration/contracts/register.md` gains a new
subsection under "Side effects":

```markdown
### Re-registration path

When all of the following hold:

- The bootstrap token validates as `Issued` and matches the request's
  `clientApp` scope.
- The descriptor validates fully.
- An `Installation` row already exists for the request's
  `installGuid`, and its `ClientApp` matches the request's
  `clientApp`, and its `Status` is `Active`.

…the endpoint takes the re-registration path:

1. Every `Active` `InstallationApiCredential` row for the matched
   installation is flipped to `Status = Revoked`, `RevokedAt = now`.
2. A new `InstallationApiCredential` row is inserted with
   `Status = Active` and a freshly-generated `SecretHash`.
3. The bootstrap token transitions `Issued → Used` exactly as in the
   first-time path.
4. A `RegistrationEvent` audit row is inserted with
   `Outcome = ReRegistrationSuccess` (server-only outcome — wire
   response is identical to `Success`, 200).

If the matched installation's `ClientApp` differs from the request's
`ClientApp`, the request is rejected via the existing conflated 401
path (`Outcome = ClientScopeMismatch`); no rows in `Installations` or
`InstallationApiCredentials` are mutated.

If the matched installation's `Status` is `Revoked`, the request is
rejected via the existing conflated 401 path
(`Outcome = ExistingInstallationRevoked` — server-only audit value).
The installation is **not** auto-unrevoked; a separate admin flow
is required (out of scope).
```

### Quickstart

Reproducible end-to-end smoke (lands in `quickstart.md`):

1. Run `dotnet ef database update -p src/Infrastructure -s src/API`.
2. Mint a bootstrap token T1 via `POST /api/admin/bootstrap-tokens`.
3. `POST /register` with T1 + valid descriptor → 200, credential C1.
4. Mint a fresh bootstrap token T2 (same `clientApp`).
5. `POST /register` with T2 + a descriptor reusing the same
   `installGuid` → 200, credential C2 ≠ C1.
6. Use C1 against a protected endpoint → 401 within 5 s.
7. Use C2 against the same endpoint → 200.
8. Query `RegistrationEvents` → one `Success` row + one
   `ReRegistrationSuccess` row.

## Phase 2 — Tasks (handed to /speckit-tasks)

The task breakdown will be produced by `/speckit-tasks` and live in
`tasks.md`. Anticipated slices (vertical TDD per `bisect-safe` /
`vertical-commits`):

1. Repository surface: `FindByInstallGuidAsync` + `ListActiveByInstallationIdAsync` + their fakes. RED → GREEN.
2. EF migration: filtered unique index on `InstallationApiCredentials.InstallationId WHERE Status = Active`. RED (write-side integration test that fails today because two `Active` rows can coexist) → GREEN.
3. Service primitive: `IInstallationCredentialService.RevokeActiveAsync`. Unit tests RED → GREEN.
4. Enum values: `ReRegistrationSuccess`, `ExistingInstallationRevoked`. Append-only; no test by themselves.
5. Service branch: `RegistrationService` cross-app + revoked-installation rejection paths. RED service-layer tests → GREEN.
6. Service branch: `CommitReRegistrationAsync` happy path + race-loser sub-branch. RED → GREEN.
7. Endpoint logging: `ILogger<RegistrationEndpoints>` + typed catch. RED integration test using `CapturingLoggerProvider` → GREEN.
8. Doc updates: `contracts/register.md` (in 001) + `data-model.md` (in 001) + this spec's `contracts/register.md` cross-reference. Docs-only commit, no test.
9. CHANGELOG entry under `[Unreleased]`. Docs-only commit, no test.

Each slice is its own vertical TDD work commit, reviewed individually
per the `resolve-ticket` protocol.

## Complexity Tracking

> Fill ONLY if Constitution Check has violations that must be justified.

(None — all gates pass without deviation.)
