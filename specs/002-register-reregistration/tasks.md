# Tasks: Atomic re-registration on existing installation

**Input**: design documents under `specs/002-register-reregistration/`
**Prerequisites**: [`spec.md`](./spec.md), [`plan.md`](./plan.md),
[`research.md`](./research.md), [`data-model.md`](./data-model.md),
[`quickstart.md`](./quickstart.md). Issue
[`#71`](https://github.com/luca-veronelli-stem/stem-dictionaries-manager/issues/71).
Reviews under [`llm/reviews/`](../../llm/reviews/) at PR head.

**Tests**: included — Constitution Principle III (Test-First) is
NON-NEGOTIABLE.

**Organization**: nine vertical TDD slices, one work commit per slice.
Each commit must build and pass tests on its own (`bisect-safe`).
Conventional-commit titles supplied per slice; reviewer pins the
final wording at approval time.

## Slice index

| # | Slice | Type | Touches | Story |
|---|---|---|---|---|
| 1 | Repository surface (FindByInstallGuid, ListActiveByInstallationId) | feat(infra) | Infrastructure interfaces + impls + fakes | foundation |
| 2 | Filtered unique index migration | feat(infra) | AppDbContext + new EF migration + test | foundation |
| 3 | `RevokeActiveAsync` service primitive | feat(services) | InstallationCredentialService + tests | foundation, US1 |
| 4 | Enum values (`ReRegistrationSuccess`, `ExistingInstallationRevoked`) | feat(core) | RegistrationOutcome | foundation |
| 5 | Re-registration rejection branches (cross-app, revoked installation) | feat(services) | RegistrationService + service-layer tests | US1 |
| 6 | `CommitReRegistrationAsync` happy path + race-loser | feat(services) | RegistrationService + service-layer + endpoint tests | US1 |
| 7 | Endpoint exception logging | fix(api) | RegistrationEndpoints + Program.cs + endpoint test + CapturingLoggerProvider | US2 |
| 8 | Contract + data-model doc updates (in spec 001) | docs | specs/001/contracts/register.md + specs/001/data-model.md | both |
| 9 | CHANGELOG entry under `[Unreleased]` | docs | CHANGELOG.md | both |

---

## Slice 1 — Repository surface (FindByInstallGuid + ListActiveByInstallationId)

**Foundation.** Read-only methods that downstream slices need. No
behavior change to the live API yet.

**Suggested commit title**: `feat(infra): repository methods for re-registration lookups`

### Tasks

- **T1.1** Write RED unit test
  `tests/Tests/Unit/Infrastructure/Repositories/InstallationRepositoryTests.cs`:
  - `FindByInstallGuidAsync_WhenRowExists_ReturnsRow`
  - `FindByInstallGuidAsync_WhenNoRowExists_ReturnsNull`

  If the file doesn't exist, create it. Use SQLite-in-memory via the
  existing `IntegrationTestBase` pattern (Phase III says integration
  for DB-backed repo tests; choose integration if no unit-level repo
  test surface exists).
- **T1.2** Write RED unit test
  `tests/Tests/Unit/Infrastructure/Repositories/InstallationApiCredentialRepositoryTests.cs`:
  - `ListActiveByInstallationIdAsync_OnlyActive_AreReturned`
  - `ListActiveByInstallationIdAsync_NoMatches_ReturnsEmptyList`
- **T1.3** Extend `src/Infrastructure/Interfaces/Auth/IInstallationRepository.cs`
  with `FindByInstallGuidAsync(Guid installGuid, CancellationToken ct = default)`.
- **T1.4** Implement in
  `src/Infrastructure/Repositories/Auth/InstallationRepository.cs`:
  `_context.Installations.FirstOrDefaultAsync(i => i.InstallGuid == installGuid, ct)`.
- **T1.5** Extend `src/Infrastructure/Interfaces/Auth/IInstallationApiCredentialRepository.cs`
  with `ListActiveByInstallationIdAsync(int installationId, CancellationToken ct = default)`.
- **T1.6** Implement in
  `src/Infrastructure/Repositories/Auth/InstallationApiCredentialRepository.cs`:
  filter `Where(c => c.InstallationId == installationId && c.Status == InstallationStatus.Active).ToListAsync(ct)`.
- **T1.7** Extend fakes:
  `tests/Tests/Unit/Services/Auth/Fakes/FakeInstallationApiCredentialRepository.cs`
  (and a `FakeInstallationRepository.cs` if missing) with the new
  methods. Manual fakes only; mirror the existing in-memory bag
  approach.
- **T1.8** Run `pwsh llm/reviews/gate.ps1` → green. Commit.

**Acceptance**: green gate; HEAD compiles; new tests pass; existing
tests unchanged.

---

## Slice 2 — Filtered unique index migration

**Foundation.** Enforces the "at most one Active credential per
Installation" invariant from R1 / data-model § 6.

**Suggested commit title**: `feat(infra): unique active credential per installation guard`

### Tasks

- **T2.1** Write RED integration test
  `tests/Tests/Integration/Infrastructure/Auth/InstallationApiCredentialUniqueActiveTests.cs`:
  - `Insert_SecondActiveCredentialForSameInstallation_ThrowsUniqueConstraintViolation`
  - `Insert_SecondCredentialAsRevoked_Succeeds` (sanity: filtered
    index only blocks two Active, not one Active + one Revoked)
  - `Insert_TwoActiveForDifferentInstallations_Succeeds`

  Uses `RegisterApiFactory` (or the equivalent integration base) for
  SQLite-in-memory. Pre-migration these tests fail because the
  constraint doesn't exist yet.
- **T2.2** Update `src/Infrastructure/AppDbContext.cs` `OnModelCreating`:
  ```csharp
  modelBuilder.Entity<InstallationApiCredentialEntity>()
      .HasIndex(c => c.InstallationId)
      .HasFilter("[Status] = 0")
      .IsUnique();
  ```
  Note: `[Status] = 0` matches `InstallationStatus.Active` (enum
  ordinal 0). Verify the enum ordering before commit — if the enum
  has been re-ordered, the filter literal must change.
- **T2.3** Generate the EF migration:
  ```powershell
  dotnet ef migrations add MultiActiveCredentialPerInstallationGuard `
      -p src/Infrastructure -s src/API
  ```
  Inspect the generated SQL for both providers (SQL Server +
  SQLite). Confirm the filter is emitted.
- **T2.4** Run the slice-2 tests → green.
- **T2.5** Run `pwsh llm/reviews/gate.ps1` → green. Commit.

**Acceptance**: green gate; the unique-active integration test
passes; no existing test breaks; the migration file is checked in.

---

## Slice 3 — `RevokeActiveAsync` service primitive

**Foundation.** The reusable credential-revocation primitive that
US1's `CommitReRegistrationAsync` (slice 6) will call, and that
issue #68's future admin revoke will wrap. Pins the SC-006
reusability criterion.

**Suggested commit title**: `feat(services): RevokeActiveAsync credential primitive`

### Tasks

- **T3.1** Write RED unit tests
  `tests/Tests/Unit/Services/Auth/InstallationCredentialServiceTests.cs`:
  - `RevokeActiveAsync_NoActiveCredentials_ReturnsZero`
  - `RevokeActiveAsync_OneActive_FlipsStatusAndRevokedAt_ReturnsOne`
  - `RevokeActiveAsync_MixedActiveAndRevoked_OnlyFlipsActive_ReturnsActiveCount`
  - `RevokeActiveAsync_InstallationIdZeroOrNegative_ThrowsArgumentOutOfRange`
  - `RevokeActiveAsync_DoesNotCallSaveChanges_LeavesCommitToCaller`
    (assert: the fake repository's `UpdateAsync` was called N times
    but no SaveChanges happens inside `RevokeActiveAsync` — this is
    the invariant that lets re-registration batch the revoke into the
    surrounding transaction)
- **T3.2** Extend `src/Services/Interfaces/Auth/IInstallationCredentialService.cs`:
  ```csharp
  Task<int> RevokeActiveAsync(int installationId, DateTime revokedAt,
      CancellationToken ct = default);
  ```
- **T3.3** Implement in `src/Services/Auth/InstallationCredentialService.cs`
  per the plan's signature. Function body ≤ 15 LOC.
- **T3.4** Run `pwsh llm/reviews/gate.ps1` → green. Commit.

**Acceptance**: green gate; the five new unit tests pass; the
existing `InstallationCredentialServiceTests` are unchanged.

---

## Slice 4 — Enum values (server-only outcomes)

**Foundation.** Pure data: appends two values to
`RegistrationOutcome`. No production caller yet; consumed by slices 5
and 6.

**Suggested commit title**: `feat(core): re-registration audit outcomes (server-only)`

### Tasks

- **T4.1** Append to `src/Core/Enums/Auth/RegistrationOutcome.cs`:
  ```csharp
  ReRegistrationSuccess,
  ExistingInstallationRevoked
  ```
  Append at the end of the enum (post-`AuditFailure`) so existing
  ordinals are preserved (the filtered-index filter literal in slice
  2 references `Status = 0` for `InstallationStatus.Active`, NOT for
  `RegistrationOutcome`; this enum's ordinal stability matters only
  for any data already persisted with the legacy values, which is
  preserved by appending).
- **T4.2** Update the doc-comment block above the enum to describe
  both new values' server-only / wire-conflated semantics, mirroring
  the existing pattern.
- **T4.3** No new tests (append-only enum has no behavior of its
  own; slices 5 and 6 exercise its values).
- **T4.4** Run `pwsh llm/reviews/gate.ps1` → green. Commit.

**Acceptance**: green gate; all existing tests pass (the enum gets
new values that no switch handles yet — make sure no switch is
`-Werror`-broken; if it is, add `_` fallback there).

---

## Slice 5 — Re-registration rejection branches

**US1 — partial.** Implements the two rejection paths
(cross-app InstallGuid reuse, revoked installation). Each routes
through the existing `CommitFailureAsync` — no new commit logic.

**Suggested commit title**: `feat(services): reject cross-app and revoked re-registration attempts`

### Tasks

- **T5.1** Write RED service-layer integration tests in
  `tests/Tests/Integration/Services/Auth/RegistrationServiceTests.cs`:
  - `RegisterAsync_FreshTokenOnExistingInstallGuid_CrossApp_RoutesToClientScopeMismatch_NoMutation`
    - Seed: Installation `clientApp = "ButtonPanelTester"`. Token
      scoped to `"GlobalService"`.
    - Expect: `RegistrationOutcome.ClientScopeMismatch` audit row;
      no row in `Installations` / `InstallationApiCredentials`
      mutated; no new credential issued.
  - `RegisterAsync_FreshTokenOnExistingInstallGuid_RevokedInstallation_RoutesToExistingInstallationRevoked_NoMutation`
    - Seed: Installation `Status = Revoked`.
    - Expect: `RegistrationOutcome.ExistingInstallationRevoked`
      audit row; no row mutated.
- **T5.2** In `src/Services/Auth/RegistrationService.cs` `RegisterAsync`,
  after `outcome == Success` and before the existing
  `CommitSuccessAsync`, add the existing-installation lookup:
  ```csharp
  InstallationEntity? existing = await _installations
      .FindByInstallGuidAsync(descriptor!.InstallGuid, ct).ConfigureAwait(false);
  if (existing is not null)
  {
      if (!string.Equals(existing.ClientApp, descriptor.ClientApp, StringComparison.Ordinal))
      {
          return await CommitFailureAsync(request, now,
              RegistrationOutcome.ClientScopeMismatch, ct).ConfigureAwait(false);
      }
      if (existing.Status != InstallationStatus.Active)
      {
          return await CommitFailureAsync(request, now,
              RegistrationOutcome.ExistingInstallationRevoked, ct).ConfigureAwait(false);
      }
      // The happy-path branch (slice 6) lands here; for slice 5
      // only, fall through to CommitSuccessAsync — which will throw
      // on the unique-InstallGuid index, then be caught by slice 7's
      // logged 500.
      //
      // BUT: leaving this fall-through unguarded would mean slice 5's
      // RED-test assertions for cross-app + revoked are real, but
      // any OTHER test that exercises the existing-Active-Installation
      // path would NEWLY hit the unique-index throw at slice 5 HEAD.
      //
      // To stay bisect-safe (slice 5 must be green standalone), guard
      // the fall-through with a throw OR a sentinel. Decision: keep
      // CommitSuccessAsync's unique-InstallGuid 500 behavior at slice
      // 5 HEAD (no new test exercises the existing-Active-Installation
      // path until slice 6). If the existing test suite has any test
      // that pre-seeds an Installation row with the same InstallGuid
      // as a fresh-token register, fix that test in this slice as
      // part of the work.
  }
  ```
  (See slice-6 notes below for how the happy path lands.)
- **T5.3** Run `pwsh llm/reviews/gate.ps1` → green. Commit.

**Acceptance**: green gate; the two new RED tests now GREEN; existing
tests still pass.

**Bisect-safety note**: at slice-5 HEAD, the existing-Active-
Installation path falls through to `CommitSuccessAsync` and throws
on the unique-InstallGuid index — same behavior as today's `main`
(per #71's repro). No existing test exercises that combination
because spec 001 didn't have a happy-path test for re-registration.
If a test in the existing suite happens to seed an Active
Installation and then `POST /register` with the same `InstallGuid`,
it must be updated as part of slice 5 — but it's the same
unique-index throw as today, so the test was already failing or the
seed never recurred.

---

## Slice 6 — `CommitReRegistrationAsync` (happy path + race-loser)

**US1 — primary.** The atomic re-registration commit and its
race-loser branch. Slice 6 brings the FR-018 recovery flow to life.

**Suggested commit title**: `feat(services): atomic re-registration on existing installation`

### Tasks

- **T6.1** Write RED service-layer integration tests
  (extend slice 5's test file):
  - `RegisterAsync_FreshTokenOnExistingActiveInstallation_RevokesPriorCredentialsIssuesNew_AuditsReRegistrationSuccess`
    - Seed: Installation `Status = Active` + one Active credential.
    - Expect: `RegistrationResult.Success` with new plaintext ≠
      seeded plaintext; prior credential `Status = Revoked,
      RevokedAt = now`; new credential `Status = Active`; bootstrap
      token `Status = Used, ConsumedByInstallationId = existingId`;
      audit row `Outcome = ReRegistrationSuccess, ResultingInstallationId = existingId`.
  - `RegisterAsync_FreshTokenOnExistingActiveInstallation_TokenRaceLoserOnMarkUsed_RollsBackAndAuditsRaceOutcome`
    - Inject a fake `IBootstrapTokenService.MarkUsedAsync` that
      throws `BootstrapTokenStateException(FoundStatus = Used)`.
    - Expect: `RegistrationResult.Failure(TokenAlreadyUsed)`;
      prior credential's `Status` unchanged (revoke rolled back);
      no new credential inserted; audit row
      `Outcome = TokenAlreadyUsed`.
- **T6.2** Write RED HTTP-level integration tests in
  `tests/Tests/Integration/API/Auth/RegisterEndpointTests.cs`:
  - `Post_FreshTokenOnExistingActiveInstallation_Returns200_NewCredentialDifferent_PriorRevoked_TokenUsed`
- **T6.3** In `src/Services/Auth/RegistrationService.cs`, replace
  the slice-5 fall-through with a call to `CommitReRegistrationAsync`:
  ```csharp
  return await CommitReRegistrationAsync(existing, token!, descriptor,
      request, now, ct).ConfigureAwait(false);
  ```
- **T6.4** Implement `CommitReRegistrationAsync` per the plan's
  body. Mirror `CommitSuccessAsync`'s transaction shape:
  - `BeginTransactionAsync` → revoke → issue → token MarkUsed
    (with race-loser try/catch) → audit `ReRegistrationSuccess` →
    `SaveChangesAsync` → `CommitAsync`.
  - On `BootstrapTokenStateException`: roll back, clear tracker,
    re-classify outcome (TokenAlreadyUsed / TokenRevoked), call
    `CommitFailureAsync`.
- **T6.5** Race-limitation acknowledgement (plan-review Finding 1):
  add inline XML doc on `CommitReRegistrationAsync` noting that the
  two-different-tokens-against-same-Installation concurrent race
  surfaces as `DbUpdateException` from the filtered unique index on
  the slowest writer, propagates up to the endpoint, and is
  logged-then-500'd per FR-008. No new test asserts the 500 path
  here — slice 7's `Post_SwallowedExceptionInService_LogsErrorAndReturns500`
  already covers the FR-008 contract; this race-loser-500 is a
  specific instance of that contract.
- **T6.6** Run `pwsh llm/reviews/gate.ps1` → green. Commit.

**Acceptance**: green gate; all new RED tests now GREEN; existing
tests unchanged. The end-to-end re-registration scenario from US1
acceptance scenario 1 works against an integration test.

---

## Slice 7 — Endpoint exception logging (Part 1 of issue #71)

**US2.** Replace the parameterless catch in
`RegistrationEndpoints.Register` with a typed
`catch (Exception ex) { logger.LogError(ex, ...); ... }`.

**Suggested commit title**: `fix(api): log swallowed exceptions in /register`

### Tasks

- **T7.1** Add `tests/Tests/Unit/Services/Auth/Fakes/CapturingLoggerProvider.cs`
  — a manual fake implementing `ILoggerProvider` that records every
  `LogError` call with (LogLevel, Exception, FormattedMessage). ≤ 30 LOC,
  no dependencies. Plus a small `CapturingLogger` class that
  composes the captures.
- **T7.2** Write RED integration test in
  `tests/Tests/Integration/API/Auth/RegisterEndpointTests.cs`:
  - `Post_SwallowedExceptionInService_LogsErrorAndReturns500`
    - Use `WebApplicationFactory.WithWebHostBuilder` to inject a
      fault-injecting `IRegistrationService` fake that throws.
    - Also inject the `CapturingLoggerProvider`.
    - `POST /register` with any valid-looking body.
    - Expect: `500` + body `{"error":"audit failure"}` (unchanged);
      capturing logger holds exactly one `LogError` entry with the
      thrown exception and a structured-message containing
      `"Registration failed with unhandled exception"` (or whatever
      stable substring the code uses).
- **T7.3** In `src/API/Endpoints/Auth/RegistrationEndpoints.cs`:
  - Add `ILogger<RegistrationEndpoints> logger` to the `Register`
    method's parameter list.
  - Replace the parameterless `catch` with:
    ```csharp
    catch (Exception ex)
    {
        logger.LogError(ex,
            "Registration failed with unhandled exception (sourceIp={SourceIp}, clientApp={ClientApp}, installGuid={InstallGuid}).",
            sourceIp, dto?.Descriptor?.ClientApp, dto?.Descriptor?.InstallGuid);
        return RawJson(AuditFailureBody, StatusCodes.Status500InternalServerError);
    }
    ```
  - `ILogger<RegistrationEndpoints>` is auto-registered by ASP.NET
    Core — no `Program.cs` change needed. Verify with a quick
    composition-root scan.
- **T7.4** Run `pwsh llm/reviews/gate.ps1` → green. Commit.

**Acceptance**: green gate; the FR-008 test passes; previously-
classified failure modes (TokenInvalid, TokenAlreadyUsed, etc.)
still return their original status + body bytes with no extra log
entries from the new catch (US2 acceptance scenario 2).

---

## Slice 8 — Contract + data-model doc updates in spec 001

**Docs-only.** Updates the externally-observable contract for
`POST /register` so the re-registration path is no longer a spec
gap (FR-009 + FR-010). No code change.

**Suggested commit title**: `docs(spec): document re-registration path in 001 contracts + data-model`

### Tasks

- **T8.1** Append the "Re-registration path" subsection to
  `specs/001-bootstrap-registration/contracts/register.md` § "Side
  effects" per the plan's verbatim block.
- **T8.2** Extend the `RegistrationOutcome` table in the same file's
  "Status → outcome map" subsection with the two new server-only
  outcomes (`ReRegistrationSuccess` → 200,
  `ExistingInstallationRevoked` → 423 `Locked` — corrected by #85; this
  #71-era task originally wrote 401 conflated).
- **T8.3** Update `specs/001-bootstrap-registration/data-model.md`
  § `InstallationApiCredential` "Lifecycle relationship to
  Installation": replace the 1:1 prose with the 1:N + at-most-one-
  Active wording from `specs/002-register-reregistration/data-model.md`.
- **T8.4** Extend the same file's "Cross-cutting invariants" list
  with invariant 6 (at-most-one-Active credential per Installation,
  enforced by the filtered unique index).
- **T8.5** Run `pwsh llm/reviews/gate.ps1` → green (gate is
  build/test/format — no test references these docs, so no risk).
  Commit.

**Acceptance**: green gate; the contract doc and data-model are
self-consistent with the implementation as of slice 7.

---

## Slice 9 — CHANGELOG entry under `[Unreleased]`

**Docs-only.** Constitution Quality Gate 6 requires it.

**Suggested commit title**: `docs(changelog): atomic re-registration + logged 500s (#71)`

### Tasks

- **T9.1** Add under `[Unreleased]` in `CHANGELOG.md`:
  ```markdown
  ### Added
  - `POST /register` now atomically re-registers an existing
    `InstallGuid` when a fresh bootstrap token is presented (option
    B from #71). The prior credential is revoked, a new one is
    issued, and an audit row with the dedicated
    `ReRegistrationSuccess` outcome is written, all in one
    transaction. Recovers from lost-credential scenarios without
    admin pre-revoke. (#71)
  - `IInstallationCredentialService.RevokeActiveAsync` —
    service-layer primitive for revoking all `Active` credentials
    on a given installation. Reusable by the future admin revoke
    endpoint (#68). (#71)
  - Database-level guard: filtered unique index on
    `InstallationApiCredentials.InstallationId WHERE Status = Active`.
    Migration `MultiActiveCredentialPerInstallationGuard`. (#71)

  ### Fixed
  - `POST /register` no longer swallows exceptions silently. Any
    unhandled exception is logged at error level with the exception
    object attached before the 500 audit-failure response is
    returned. The 500 response shape is unchanged. (#71)

  ### Changed
  - The `InstallationApiCredentials` table now holds **multiple
    rows per installation** over an installation's lifetime — at
    most one with `Status = Active` at any instant, plus
    zero-or-more `Revoked` historical rows preserved for forensics.
    The `data-model.md` § "InstallationApiCredential" prose updated
    to match. (#71)
  ```
- **T9.2** Run `pwsh llm/reviews/gate.ps1` → green. Commit.

**Acceptance**: green gate; CHANGELOG `[Unreleased]` block reflects
the user-visible impact of this PR.

---

## Done criteria

After all nine slices land:

- All `spec.md` acceptance scenarios (US1 ×3, US2 ×2) have at least
  one corresponding integration test passing green.
- All six success criteria (SC-001 through SC-006) are either
  measurable in the test suite (SC-001..SC-005) or have been
  verified at planning time against the #68 contract doc (SC-006).
- The local `gate.ps1` is green.
- GitHub Actions CI is green on both `ubuntu-latest` and
  `windows-latest`.
- The volatile review commit at PR head still carries the latest
  `state.md`.

Then state advances to `FinalizationRequired` for owner sign-off.
