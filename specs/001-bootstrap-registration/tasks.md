# Tasks: Bootstrap registration for per-installation API credentials

**Input**: Design documents from `specs/001-bootstrap-registration/`
**Prerequisites**: [`plan.md`](./plan.md), [`spec.md`](./spec.md), [`research.md`](./research.md), [`data-model.md`](./data-model.md), [`contracts/`](./contracts/), [`quickstart.md`](./quickstart.md)

**Tests**: Test tasks are **REQUIRED** for this feature. Per `.specify/memory/constitution.md` v1.0.1 Principle III ("Test-First, Manual Fakes, Integration over Mocks") and the security-critical nature of this feature (FR-002 unified-401, FR-013 audit-or-no-issue, SC-002 byte-identical failure responses), every layer is implemented test-first.

**Organization**: Tasks are grouped by user story (US1 = P1 client registration, US2 = P2 admin mint, US3 = P3 admin list/revoke), with shared cross-cutting work in Foundational (Phase 2). All file paths assume the existing archetype-A layout from `plan.md` ("Project Structure" section).

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Different files, no dependencies on incomplete tasks — safe to run in parallel.
- **[Story]**: Maps task to a user story (US1/US2/US3). Setup, Foundational, and Polish phases have no `[Story]` label.

---

## Phase 1: Setup

**Purpose**: Project-level prerequisites with no story affinity.

- [ ] T001 Add `Microsoft.Extensions.Caching.Memory` to `Directory.Packages.props` (latest .NET 10 stable) and reference it from `src/API/API.csproj` only — Core/Services/Infrastructure must not depend on it (per `research.md` R6, the cache is composed at the API layer).
- [ ] T002 [P] Add empty `AdminApiKeys` array to `src/API/appsettings.json` and a populated dev value to `src/API/appsettings.Development.json` so the new admin auth surface has a config seam (per `quickstart.md` step 1).

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core domain types, persistence, cryptographic helpers, and middleware that every user story depends on. Constitution Principle III: each implementation task here has its tests written first (the test task immediately precedes the implementation task it covers).

**CRITICAL**: No US1/US2/US3 task may begin until this phase is complete. The Constitution Check in `plan.md` is verified in this phase (composition root, no banned APIs in Core/Services, manual DI).

### Domain enums and models

- [ ] T003 [P] Create `src/Core/Enums/Auth/BootstrapTokenStatus.cs` with values `Issued, Used, Expired, Revoked` per `data-model.md` § BootstrapToken state machine.
- [ ] T004 [P] Create `src/Core/Enums/Auth/InstallationStatus.cs` with values `Active, Revoked` per `data-model.md` § Installation.
- [ ] T005 [P] Create `src/Core/Enums/Auth/RegistrationOutcome.cs` with values `Success, TokenMissing, TokenInvalid, TokenAlreadyUsed, TokenExpired, TokenRevoked, ClientScopeMismatch, DescriptorMalformed, AuditFailure` per `data-model.md` § RegistrationOutcome enum.
- [ ] T006 [P] Create `src/Core/Models/Auth/BootstrapToken.cs` (domain model — `Id, ClientApp, SecretHash, MintedAt, ExpiresAt, Status, UsedAt, ConsumedByInstallationId, RevokedAt`) per `data-model.md`. Constructor enforces `ClientApp` non-empty (`ArgumentException.ThrowIfNullOrWhiteSpace`) and `ExpiresAt - MintedAt ∈ [1 h, 90 d]` (FR-007).
- [ ] T007 [P] Create `src/Core/Models/Auth/Installation.cs` (`Id, ClientApp, OsUserId, MachineId, InstallGuid, AppVersion?, DescriptorJson, RegisteredAt, Status, RevokedAt`) per `data-model.md`. Constructor enforces all four identity fields non-empty/non-default; `AppVersion` trimmed-non-empty when present.
- [ ] T008 [P] Create `src/Core/Models/Auth/InstallationApiCredential.cs` (`Id, InstallationId, SecretHash, IssuedAt, Status, RevokedAt`) per `data-model.md`.
- [ ] T009 [P] Create `src/Core/Models/Auth/RegistrationEvent.cs` (`Id, OccurredAt, ClaimedClientApp?, ClaimedOsUserId?, ClaimedMachineId?, ClaimedInstallGuid?, ClaimedAppVersion?, SourceIp, DescriptorJson?, Outcome, ResultingInstallationId?`) per `data-model.md`.
- [ ] T010 [P] Create `src/Core/Models/Auth/InstallationDescriptor.cs` (DTO-shaped value object — `ClientApp, OsUserId, MachineId, InstallGuid, AppVersion?`) used by `RegistrationService` to pass parsed descriptor data through the domain.

### Cryptographic helpers (test-first)

- [ ] T011 Write `tests/Tests/Unit/Services/Auth/PasswordHasherTests.cs` covering: round-trip (hash a plaintext, verify the same plaintext returns true), wrong-plaintext returns false, format string parses as `pbkdf2-sha256$<iterations>$<salt-b64>$<hash-b64>` per `research.md` R3, and `FixedTimeEquals` is used (no early return on mismatch). Tests MUST FAIL initially (no implementation yet).
- [ ] T012 Implement `src/Services/Auth/PasswordHasher.cs` (PBKDF2-HMAC-SHA256, 600 000 iterations, 16-byte CSPRNG salt, 32-byte derived key, self-describing string format per R3). Validation via `CryptographicOperations.FixedTimeEquals`. T011 tests MUST PASS after this task.
- [ ] T013 [P] Write `tests/Tests/Unit/Services/Auth/TokenGeneratorTests.cs` covering: bootstrap-token shape `stbt_` + 43 base64url chars (no `=` padding), credential shape `stak_` + 43 base64url chars, two consecutive calls return different values (entropy smoke test).
- [ ] T014 Implement `src/Services/Auth/TokenGenerator.cs` (CSPRNG via `RandomNumberGenerator.GetBytes(32)`, base64url encode without padding, prefix per R5). Two methods: `GenerateBootstrapToken()` and `GenerateApiCredential()`.

### EF Core entities and repositories

- [ ] T015 [P] Create `src/Infrastructure/Entities/Auth/BootstrapTokenEntity.cs` mirroring the domain model (T006). EF mapping: index on `Status`, unique index on `SecretHash`, unique filtered index on `ConsumedByInstallationId WHERE NOT NULL` (data-model invariant 1).
- [ ] T016 [P] Create `src/Infrastructure/Entities/Auth/InstallationEntity.cs`. EF mapping: composite index on `(ClientApp, OsUserId, MachineId)`, unique index on `InstallGuid`.
- [ ] T017 [P] Create `src/Infrastructure/Entities/Auth/InstallationApiCredentialEntity.cs`. EF mapping: index on `InstallationId`, unique index on `SecretHash`, FK `InstallationId → Installations.Id` with `OnDelete(Restrict)`.
- [ ] T018 [P] Create `src/Infrastructure/Entities/Auth/RegistrationEventEntity.cs`. EF mapping: indices on `OccurredAt`, `(ClaimedClientApp, OccurredAt)`, `SourceIp`.
- [ ] T019 Update `src/Infrastructure/AppDbContext.cs` to add `DbSet<BootstrapTokenEntity> BootstrapTokens`, `DbSet<InstallationEntity> Installations`, `DbSet<InstallationApiCredentialEntity> InstallationApiCredentials`, `DbSet<RegistrationEventEntity> RegistrationEvents`, plus their `OnModelCreating` configuration calls. (Sequential — single file edit.)
- [ ] T020 [P] Create `src/Infrastructure/Interfaces/Auth/IBootstrapTokenRepository.cs` with `GetByIdAsync`, `ListByStatusAsync`, `AddAsync`, `UpdateAsync`. (No `GetBySecretHashAsync` — lookup-by-plaintext requires hashing every row, handled in `BootstrapTokenService`.)
- [ ] T021 [P] Create `src/Infrastructure/Interfaces/Auth/IInstallationRepository.cs` with `GetByIdAsync`, `ListAsync(string? clientApp, InstallationStatus? status)`, `AddAsync`, `UpdateAsync`.
- [ ] T022 [P] Create `src/Infrastructure/Interfaces/Auth/IInstallationApiCredentialRepository.cs` with `GetByInstallationIdAsync`, `ListAllActiveAsync`, `AddAsync`, `UpdateAsync`.
- [ ] T023 [P] Create `src/Infrastructure/Interfaces/Auth/IRegistrationEventRepository.cs` with `AddAsync`, `ListBySourceAsync(string sourceIp, DateTime since)`.
- [ ] T024 [P] Implement `src/Infrastructure/Repositories/Auth/BootstrapTokenRepository.cs`.
- [ ] T025 [P] Implement `src/Infrastructure/Repositories/Auth/InstallationRepository.cs`.
- [ ] T026 [P] Implement `src/Infrastructure/Repositories/Auth/InstallationApiCredentialRepository.cs`.
- [ ] T027 [P] Implement `src/Infrastructure/Repositories/Auth/RegistrationEventRepository.cs`.

### Audit enum extension and admin user seed

- [ ] T028 Extend `src/Core/Enums/AuditEntityType.cs` with `BootstrapToken` and `Installation` values appended at the end (additive, no existing consumer breaks per `data-model.md` § Audit split).
- [ ] T029 Generate the EF migration: `dotnet ef migrations add AddBootstrapRegistration -p src/Infrastructure -s src/API`. Verify the generated `src/Infrastructure/Migrations/<timestamp>_AddBootstrapRegistration.cs` creates exactly the four new tables (no alterations to existing tables) and seeds a single `UserEntity { Username = "system-admin", DisplayName = "System Admin (API key)" }` row via `migrationBuilder.InsertData(...)` per `data-model.md` § Audit split. Snapshot file (`AppDbContextModelSnapshot.cs`) MUST be regenerated by the EF tooling.

### Credential validation surface (used by middleware)

- [ ] T030 [P] Create `src/Services/Interfaces/Auth/IInstallationCredentialValidator.cs` with `ValidateAsync(string plaintext, CancellationToken ct) → Task<int? installationId>` (returns the active installation id on a hit, `null` on miss/revoked) and `Invalidate(string plaintext) → void` (synchronous cache eviction used by revoke).
- [ ] T031 Write `tests/Tests/Unit/Services/Auth/InstallationCredentialValidatorTests.cs` covering: cache hit short-circuits the DB lookup; revoked credential invalidated synchronously by `Invalidate(...)`; absolute 5-second TTL ceiling (R4 / SC-004) verified via a fake `IMemoryCache` time provider; key is `Sha256(plaintext)` not the plaintext itself (R4 rationale).
- [ ] T032 Implement `src/Services/Auth/InstallationCredentialValidator.cs` per `research.md` R4 — `IMemoryCache` keyed on `Sha256(plaintext)` → `(installationId, isActive)`, 5-second absolute TTL, explicit invalidation on revoke. Repository miss path PBKDF2-verifies the candidate against every active credential's `SecretHash` and caches the resolution. T031 tests MUST PASS after this task.

### Authentication middleware (union mode + admin)

- [ ] T033 Modify `src/API/Middleware/ApiKeyMiddleware.cs` to add `/register` to the unauth allow-list (alongside `/openapi`, `/swagger`, `/health`, `/api/version` per `contracts/register.md` § Authentication / middleware) AND to validate `X-Api-Key` against EITHER the legacy `ApiKeys` config section OR `IInstallationCredentialValidator.ValidateAsync(...)` (FR-005 union mode). Existing legacy-key behavior MUST be preserved bit-for-bit; only the union branch is new.
- [ ] T034 [P] Create `src/API/Middleware/AdminAuthenticationMiddleware.cs` per `data-model.md` § Audit split — runs after `ApiKeyMiddleware` for paths starting with `/api/admin/`, sets `ICurrentUserProvider.CurrentUserId` to the seeded `system-admin` user's id whenever the incoming `X-Api-Key` matches an entry in the new `AdminApiKeys` section. Returns 401 if the key is in `ApiKeys` but not in `AdminApiKeys` (admin endpoints reject non-admin keys).
- [ ] T035 Update `src/Services/CurrentUserProvider.cs` (or replace its DI registration in the API composition root) to be `HttpContext`-aware per-request when running under the API host, while preserving the existing singleton-set behavior used by the GUI host. New surface: `int? CurrentUserId { get; set; }` continues to work for the GUI; under the API the value is read from an `HttpContext.Items` slot populated by `AdminAuthenticationMiddleware`. (Sequential — composition-root change.)

### Wire DI in API composition root

- [ ] T036 Update `src/API/Program.cs` to register the new types — `IPasswordHasher → PasswordHasher`, `ITokenGenerator → TokenGenerator`, `IInstallationCredentialValidator → InstallationCredentialValidator` (singleton, holds the cache), all four repositories (scoped), `BootstrapTokenService`, `InstallationCredentialService`, `RegistrationService` (scoped). Add `services.AddMemoryCache()`. Add `app.UseMiddleware<AdminAuthenticationMiddleware>()` after the existing `UseMiddleware<ApiKeyMiddleware>()`. Bind `AdminApiKeys` from configuration.

**Checkpoint**: Foundational complete. The DB schema, hashing, validator, and auth boundary are in place. User stories may now proceed.

---

## Phase 3: User Story 1 — Client app obtains its API credential on first launch (Priority: P1) 🎯 MVP

**Goal**: A fresh client trades a valid bootstrap token for a per-installation API credential via `POST /register`. The token is consumed atomically with the installation+credential creation; failure modes return a unified 401 (FR-002).

**Independent Test**: Per `spec.md` § US1 Independent Test — seed a valid token, call `POST /register` from a fresh client environment, verify the returned credential authenticates a subsequent `GET /api/dictionaries`, then confirm the same token is rejected on the second call.

### Tests for User Story 1 (write first; MUST FAIL until implementation lands)

- [ ] T037 [P] [US1] Write `tests/Tests/Integration/API/Auth/RegisterEndpointTests.cs` with a passing-path test (`Register_WithValidToken_Returns200AndCredential`) that seeds a valid token via repository, posts `/register` with a well-formed descriptor, asserts 200 + body shape per `contracts/register.md`, asserts the returned `apiCredential` then authenticates `GET /api/dictionaries` (FR-005 union — credential goes through `ApiKeyMiddleware`), asserts the token row's `Status = Used`, `UsedAt` is set, and `ConsumedByInstallationId` matches the new installation. Maps to acceptance scenarios 1 + 5.
- [ ] T038 [P] [US1] Add to `RegisterEndpointTests.cs` failure-mode tests proving SC-002 byte-identical responses: token-already-used, token-expired (mint with TTL = 1 h, advance system clock past expiry via fake `TimeProvider`), token-revoked, client-scope-mismatch, descriptor-missing-required-field, descriptor-malformed-json, all-zeros `installGuid`, missing-token. Each test asserts `401`, `Content-Type: application/json`, body byte-equal to `{"error":"registration failed"}`. Maps to acceptance scenarios 2/3/4 and edge cases.
- [ ] T039 [P] [US1] Add `Register_ConcurrentSameToken_OnlyOneSucceeds` to `RegisterEndpointTests.cs` proving SC-003 — two simultaneous `POST /register` calls with the same valid token; assert exactly one returns 200 and exactly one returns 401, exactly one `Installation` row exists with `BootstrapToken.ConsumedByInstallationId = T.Id` (data-model invariant 1).
- [ ] T040 [P] [US1] Add `Register_AuditWriteFailure_NoCredentialIssued` to `RegisterEndpointTests.cs` proving FR-013 — inject a `RegistrationEventRepository` that throws on `AddAsync`, post a valid `/register`, assert 500 with `{"error":"audit failure"}`, assert no `Installation`/`InstallationApiCredential` row was committed (single-transaction rollback per data-model invariant 3).
- [ ] T041 [P] [US1] Add `Register_PlaintextNotInLogs` to `RegisterEndpointTests.cs` proving SC-007 invariant 4 — capture `ILogger` output during a successful `/register`, assert the returned plaintext credential string does not appear anywhere in captured log lines.
- [ ] T042 [P] [US1] Write `tests/Tests/Unit/Services/Auth/BootstrapTokenStateMachineTests.cs` covering the transition matrix per `data-model.md` § BootstrapToken state machine: `Issued → Used` is irreversible, `Issued → Revoked` is irreversible, `Used → *` throws, `Revoked → *` throws, `Expired` is computed (not stored) when `Now > ExpiresAt && Status = Issued`. One test per transition, asserting post-state invariants — shape matches the eventual Lean preservation theorems flagged in `plan.md` § Constitution Check Principle III TODO(LEAN_WORKSPACE).

### Implementation for User Story 1

- [ ] T043 [P] [US1] Create DTOs `src/API/Dtos/Auth/InstallationDescriptorDto.cs`, `src/API/Dtos/Auth/RegisterRequestDto.cs`, `src/API/Dtos/Auth/RegisterResponseDto.cs` matching `contracts/register.md` field names and types (`appVersion` optional, additional descriptor properties round-tripped via `JsonExtensionData`).
- [ ] T044 [P] [US1] Create `src/Services/Interfaces/Auth/IBootstrapTokenService.cs` with `LookupAsync(string plaintext, CancellationToken ct) → Task<BootstrapToken?>` (PBKDF2-verifies plaintext against active rows; returns `null` on miss) and `MarkUsedAsync(int tokenId, int installationId, CancellationToken ct) → Task` (state transition + repository update).
- [ ] T045 [US1] Implement `src/Services/Auth/BootstrapTokenService.cs` `Lookup` and `MarkUsed` (mint comes in US2). Verify is constant-time via `IPasswordHasher.Verify(...)`; iteration over candidate rows is bounded by `IBootstrapTokenRepository.ListByStatusAsync(Issued)`.
- [ ] T046 [P] [US1] Create `src/Services/Interfaces/Auth/IInstallationCredentialService.cs` with `IssueAsync(int installationId, CancellationToken ct) → Task<(InstallationApiCredential record, string plaintext)>` (list/revoke come in US3).
- [ ] T047 [US1] Implement `src/Services/Auth/InstallationCredentialService.cs` `IssueAsync` — generates plaintext via `ITokenGenerator`, hashes via `IPasswordHasher`, persists row, returns plaintext-once-only.
- [ ] T048 [P] [US1] Create `src/Services/Interfaces/Auth/IRegistrationService.cs` with `RegisterAsync(string bootstrapTokenPlaintext, InstallationDescriptor descriptor, string sourceIp, CancellationToken ct) → Task<RegistrationResult>` (`RegistrationResult` is a discriminated record with `Success(int installationId, string apiCredentialPlaintext, DateTime issuedAt)` and `Failure()` cases — no failure detail returned upward, matching FR-002).
- [ ] T049 [US1] Implement `src/Services/Auth/RegistrationService.cs`. Single `SaveChangesAsync` transaction wraps: validate token (state, expiry, client-scope match) → if fail, write `RegistrationEvent` with the matching `Outcome` and return `Failure` → if success, generate credential, insert `Installation`, insert `InstallationApiCredential`, transition `BootstrapToken.Status → Used`, insert `RegistrationEvent` with `Outcome = Success` and `ResultingInstallationId`, commit. On audit-write throw, propagate up (handled in the endpoint as 500 per `contracts/register.md`).
- [ ] T050 [US1] Create `src/API/Endpoints/Auth/RegistrationEndpoints.cs` exposing `POST /register` per `contracts/register.md`. Reads `HttpContext.Connection.RemoteIpAddress`, parses descriptor leniently (additional properties accepted, captured into `DescriptorJson`), maps `RegistrationResult` to either 200 + `RegisterResponseDto` or unified 401. Catches the audit-failure exception path explicitly and returns 500 with `{"error":"audit failure"}` (FR-013). Wire via `app.MapPost(...)` in `Program.cs`.

**Checkpoint**: US1 complete. The MVP is shippable — clients can register, the unified-401 invariant holds, atomic-commit + audit-or-no-issue is enforced. Run `quickstart.md` steps 2 + 4 to validate manually.

---

## Phase 4: User Story 2 — Admin mints a bootstrap token (Priority: P2)

**Goal**: A STEM admin authenticates with an `AdminApiKeys` key and mints a single-use, time-bounded, client-scoped bootstrap token. Plaintext is returned exactly once at mint; subsequent admin queries never return it (FR-014).

**Independent Test**: Per `spec.md` § US2 Independent Test — request token creation, verify plaintext + entropy + expiry in the response, verify the plaintext is unrecoverable on any later admin call, then use the minted token in `POST /register` and confirm US1's flow accepts it.

### Tests for User Story 2 (write first; MUST FAIL until implementation lands)

- [ ] T051 [P] [US2] Write `tests/Tests/Integration/API/Auth/AdminBootstrapTokenEndpointTests.cs` with `Mint_WithDefaultTtl_Returns201AndPlaintextOnce` covering acceptance scenario 1 — POST without `ttlHours`, assert 201 + body per `contracts/admin-bootstrap-tokens.md`, assert `expiresAt - mintedAt = 30 d ± clock-skew`, assert `plaintext` matches `^stbt_[A-Za-z0-9_-]{43}$`, assert one `BootstrapTokens` row exists with `Status = Issued`.
- [ ] T052 [P] [US2] Add `Mint_WithExplicitTtl_HonorsBounds` to `AdminBootstrapTokenEndpointTests.cs` — accept `ttlHours = 1` (lower bound) and `ttlHours = 2160` (upper bound, 90 d), reject `ttlHours = 0` and `ttlHours = 2161` with 400 `{"error":"ttlHours out of range [1, 2160]"}` per FR-007. Maps to acceptance scenario 3.
- [ ] T053 [P] [US2] Add `Mint_PlaintextNotRetrievable` to `AdminBootstrapTokenEndpointTests.cs` proving FR-014 / acceptance scenario 2 — after a successful mint, no admin endpoint returns the plaintext value. Verify by scanning the `GET /api/admin/installations` list response (US3) — should be wired in T067 first; this test guards against a regression introducing a plaintext-leaking admin query.
- [ ] T054 [P] [US2] Add `Mint_WithoutAdminKey_Returns401` to `AdminBootstrapTokenEndpointTests.cs` covering: no `X-Api-Key` header, key in `ApiKeys` but not in `AdminApiKeys` (the key is valid for `/api/dictionaries` but NOT for `/api/admin/*`), key not in either section. All return 401 with body per `contracts/admin-bootstrap-tokens.md`.
- [ ] T055 [P] [US2] Add `Mint_EmptyClientApp_Returns400` and `Mint_AuditEntryWritten` to `AdminBootstrapTokenEndpointTests.cs` — empty `clientApp` returns `400 {"error":"clientApp is required"}`; on success, an `AuditEntry` row is inserted via `IAuditService.LogCreateAsync` with `EntityType = BootstrapToken`, `EntityId = <new-token-id>`, `ChangedById = <system-admin user id>`, per `data-model.md` § Audit split.

### Implementation for User Story 2

- [ ] T056 [P] [US2] Create DTOs `src/API/Dtos/Auth/MintBootstrapTokenRequestDto.cs` (`ClientApp`, `TtlHours?`) and `src/API/Dtos/Auth/MintBootstrapTokenResponseDto.cs` (`TokenId`, `ClientApp`, `Plaintext`, `MintedAt`, `ExpiresAt`) per `contracts/admin-bootstrap-tokens.md`.
- [ ] T057 [US2] Extend `src/Services/Interfaces/Auth/IBootstrapTokenService.cs` with `MintAsync(string clientApp, TimeSpan? ttl, CancellationToken ct) → Task<(BootstrapToken record, string plaintext)>`.
- [ ] T058 [US2] Implement `MintAsync` in `src/Services/Auth/BootstrapTokenService.cs` — generate plaintext via `ITokenGenerator.GenerateBootstrapToken()`, hash via `IPasswordHasher`, validate TTL ∈ `[1 h, 90 d]` (throws `ArgumentOutOfRangeException` if outside; endpoint translates to 400), persist row, return plaintext-once-only. Also call `IAuditService.LogCreateAsync(EntityType: BootstrapToken, EntityId: <new id>, ...)` so the admin's mint event is recorded with `ChangedById = system-admin user id` (set by `AdminAuthenticationMiddleware`).
- [ ] T059 [US2] Create `src/API/Endpoints/Auth/AdminAuthEndpoints.cs` and expose `POST /api/admin/bootstrap-tokens` per `contracts/admin-bootstrap-tokens.md` (US3 will extend this same file with installation list/revoke). Wire via `app.MapPost(...)` in `Program.cs`. Handles `ArgumentOutOfRangeException` from the service as 400 with the contract-specified body.

**Checkpoint**: US2 complete. End-to-end mint → register flow works against a live API. Run `quickstart.md` step 1 followed by step 2 to validate.

---

## Phase 5: User Story 3 — Admin lists and revokes installations (Priority: P3)

**Goal**: A STEM admin lists all per-installation identities and revokes any one without affecting the others. Revocation takes effect within 5 seconds (SC-004) via the validator's explicit cache invalidation.

**Independent Test**: Per `spec.md` § US3 Independent Test — after several US1 registrations, list installations and verify each has the full metadata set; revoke one and verify the credential stops authenticating (other installations unaffected).

### Tests for User Story 3 (write first; MUST FAIL until implementation lands)

- [ ] T060 [P] [US3] Write `tests/Tests/Integration/API/Auth/AdminInstallationEndpointTests.cs` with `List_ReturnsAllInstallationsWithMetadata` covering acceptance scenario 1 — seed N installations across 2 client apps, call `GET /api/admin/installations`, assert response shape per `contracts/admin-installations.md`, assert no `secretHash` / no plaintext fields appear in the JSON body.
- [ ] T061 [P] [US3] Add `List_WithFilters_Honored` to `AdminInstallationEndpointTests.cs` — query with `?clientApp=...` and `?status=active|revoked|all`; assert filtering works and is server-side (not over-fetch + client-filter).
- [ ] T062 [P] [US3] Add `Revoke_TransitionsActiveToRevoked` to `AdminInstallationEndpointTests.cs` covering acceptance scenario 2 first half — `POST /api/admin/installations/{id}/revoke`, assert 200 + body per contract, assert `Installation.Status = Revoked` and `RevokedAt` set, assert `InstallationApiCredential.Status = Revoked` for the same installation.
- [ ] T063 [P] [US3] Add `Revoke_IsolationFromOtherInstallations` to `AdminInstallationEndpointTests.cs` covering acceptance scenario 2 second half — register two installations of the same `ClientApp` (different `(OsUserId, MachineId)` pairs), revoke installation A, assert installation B's credential continues to authenticate `GET /api/dictionaries`. Maps to FR-006 + data-model invariant 5.
- [ ] T064 [P] [US3] Add `Revoke_RevokedCredentialFailsWithin5s` to `AdminInstallationEndpointTests.cs` proving SC-004 — register, authenticate once (warms the validator cache), revoke, retry the authenticated call. With explicit invalidation it fails immediately on the next call; the test asserts failure within `< 1 s` (well under the 5 s ceiling), validating `IInstallationCredentialValidator.Invalidate(...)` is called.
- [ ] T065 [P] [US3] Add `Revoke_IsIdempotent` to `AdminInstallationEndpointTests.cs` per `contracts/admin-installations.md` — second revoke on the same installation returns 200 with the original `revokedAt`, no second `AuditEntry` row inserted, no second `InstallationApiCredential` row mutation.
- [ ] T066 [P] [US3] Add `Revoke_NotFound_Returns404` and `Revoke_AuditEntryWritten` to `AdminInstallationEndpointTests.cs` — 404 with contract body for unknown id; on the first (state-changing) revoke, `IAuditService.LogUpdateAsync` writes a row with `EntityType = Installation, EntityId = {id}, ChangedById = <system-admin user id>` and the previousValue/newValue payload per `contracts/admin-installations.md`.

### Implementation for User Story 3

- [ ] T067 [P] [US3] Create DTOs `src/API/Dtos/Auth/InstallationListItemDto.cs` (per `contracts/admin-installations.md` GET response shape) and `src/API/Dtos/Auth/RevokeInstallationResponseDto.cs` (per the revoke response shape).
- [ ] T068 [US3] Extend `src/Services/Interfaces/Auth/IInstallationCredentialService.cs` with `ListAsync(string? clientApp, InstallationStatus? status, CancellationToken ct) → Task<IReadOnlyList<Installation>>` and `RevokeAsync(int installationId, CancellationToken ct) → Task<RevokeResult>` (`RevokeResult` is a record with `Success(DateTime revokedAt, bool wasFirstRevoke)` and `NotFound()` cases).
- [ ] T069 [US3] Implement `ListAsync` and `RevokeAsync` in `src/Services/Auth/InstallationCredentialService.cs`. `RevokeAsync` is idempotent: if already-revoked, return existing `RevokedAt` without mutation or audit. On first revoke, single-transaction commit of `Installation.Status → Revoked`, `InstallationApiCredential.Status → Revoked`, `AuditEntry` insert, then call `IInstallationCredentialValidator.Invalidate(plaintext-hash)` on the credential's hash AFTER the commit succeeds (cache invalidation MUST follow durable write per R4 rationale).
- [ ] T070 [US3] Extend `src/API/Endpoints/Auth/AdminAuthEndpoints.cs` with `GET /api/admin/installations` and `POST /api/admin/installations/{id}/revoke` per `contracts/admin-installations.md`. Map `RevokeResult.NotFound` to 404 with the contract body. Wire via `app.MapGet(...)` and `app.MapPost(...)` in `Program.cs`.

**Checkpoint**: US3 complete. Full feature set implemented. Run all six `quickstart.md` steps end-to-end.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Quality-gate items mandated by `.specify/memory/constitution.md` § Quality Gates and the `pr` skill's pre-PR checklist.

- [ ] T071 [P] Add a `[Unreleased]` section entry to `CHANGELOG.md` documenting the `POST /register` endpoint, the `/api/admin/*` admin surface, the new `AdminApiKeys` configuration section, the schema-migration name `AddBootstrapRegistration`, and the union-mode `ApiKeyMiddleware` behavior. Mention that legacy `ApiKeys` continues to authorize as before (FR-005, non-breaking).
- [ ] T072 [P] Run `quickstart.md` end-to-end against a locally-running `dotnet run --project src/API`, confirming the six steps (mint, register, use, single-use rejection, list, revoke + isolation) all behave as documented. Capture any drift between contract and runtime in this task's git note for follow-up.
- [ ] T073 Run pre-push gates per the `pr` skill: `dotnet format whitespace --verify-no-changes --no-restore`, `dotnet build -c Release` (warnings-as-errors), `dotnet test -c Release --framework net10.0` and `dotnet test -c Release --framework net10.0-windows`. All MUST pass. Fix any format/lint drift via `dotnet format` and re-stage.
- [ ] T074 [P] Open the PR via `gh pr create` per the `pr` skill, body referencing `specs/001-bootstrap-registration/{plan.md, spec.md, data-model.md}`, the constitution gates passed, and the consumer-side coordination ticket [`stem-button-panel-tester#48`](https://github.com/luca-veronelli-stem/stem-button-panel-tester/issues/48). Wait for both `ubuntu-latest` and `windows-latest` CI legs to pass before merging via `gh pr merge --rebase --delete-branch`. After merge, verify the Bitbucket mirror catches up (`.github/workflows/mirror-bitbucket.yml`).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)** — no upstream dependencies. T002 [P] runs alongside T001.
- **Phase 2 (Foundational)** — depends on Phase 1. Internal ordering:
  - Enums (T003–T005 [P]) and domain models (T006–T010 [P]) are independent.
  - Hasher tests + impl (T011 → T012) and TokenGenerator tests + impl (T013 → T014) are independent of each other and of the entities work.
  - Entities (T015–T018 [P]) ⇒ DbContext update (T019, sequential) ⇒ repo interfaces (T020–T023 [P]) ⇒ repo impls (T024–T027 [P]).
  - Audit enum extension (T028) and migration (T029) depend on entities being shaped (T015–T018) and DbContext updated (T019).
  - Validator interface (T030) ⇒ validator tests (T031) ⇒ validator impl (T032) — depends on repos (T024–T027) and `PasswordHasher` (T012).
  - Middleware changes (T033 + T034) depend on the validator (T032) and on `AdminApiKeys` config seam (T002).
  - `CurrentUserProvider` rework (T035) depends on `AdminAuthenticationMiddleware` (T034) shape.
  - DI wiring (T036) depends on every prior Foundational task.
- **Phase 3 (US1)** — depends on Phase 2 complete. Tests (T037–T042) [P] precede implementation (T043–T050). Within implementation: DTOs T043 [P] ⇒ token service T044 → T045 → credential service T046 → T047 → registration service T048 → T049 → endpoint T050.
- **Phase 4 (US2)** — depends on Phase 2 (foundational). MAY start in parallel with Phase 3 if staffed; otherwise sequential. Tests (T051–T055) [P] precede implementation (T056–T059).
- **Phase 5 (US3)** — depends on Phase 2 (foundational); T053 (plaintext-not-retrievable assertion) and T064 (revoke + cache invalidation) cross-reference Phase 4 and Phase 3 surfaces respectively, so US3 is best run AFTER US1 and US2 in single-developer mode.
- **Phase 6 (Polish)** — depends on all desired user story phases. T073 (pre-push gates) gates T074 (PR open).

### Within Each User Story

Per Constitution Principle III: **tests written and failing before implementation**, models before services, services before endpoints. Each story phase ends in a state where the story is independently demoable against `quickstart.md`.

### Parallel Opportunities

- T003–T010: 8 enum/model files in parallel.
- T015–T018: 4 entity files in parallel.
- T020–T023: 4 repository interfaces in parallel; then T024–T027 4 implementations in parallel.
- T037–T042: 6 US1 test files / test methods in parallel (different test classes / methods).
- T051–T055: 5 US2 tests in parallel.
- T060–T066: 7 US3 tests in parallel.
- US2 and US3 can run by separate developers once Foundational is done; US1 should land first in single-developer mode because US3's revoke-isolation test (T063) depends on US1's `/register` working.

---

## Parallel Example: User Story 1 tests

```powershell
# After Foundational (Phase 2) is complete, launch US1 tests together:
# Each test class / test method is a separate file or distinct method, no file conflicts.

# (illustrative — these are tasks the developer schedules, not literal commands)
Task: "T037 RegisterEndpointTests — happy path"
Task: "T038 RegisterEndpointTests — failure modes byte-identical"
Task: "T039 RegisterEndpointTests — concurrent same-token race"
Task: "T040 RegisterEndpointTests — audit failure → no credential"
Task: "T041 RegisterEndpointTests — plaintext not in logs"
Task: "T042 BootstrapTokenStateMachineTests — transition matrix"
```

After all six FAIL, implement T043–T050 sequentially (each layer depends on the prior).

---

## Implementation Strategy

### MVP (US1 only)

1. Phase 1 (Setup) — T001, T002.
2. Phase 2 (Foundational) — T003 through T036, with the parallel batches called out above.
3. Phase 3 (US1) — T037 through T050.
4. **STOP** and validate with `quickstart.md` steps 2 + 4 (skip mint via direct DB seeding).
5. Tag the commit. Demo / deploy the MVP if business wants the security improvement before the admin UX.

### Incremental Delivery

1. Setup + Foundational ⇒ unblocked.
2. US1 ⇒ MVP (clients can register if an admin seeds tokens via DB).
3. US2 ⇒ Mint UX (admins use HTTP, no DB seeding).
4. US3 ⇒ List + revoke UX (full operational manageability).
5. Polish ⇒ CHANGELOG, quickstart smoke, gates, PR.

Each step ends in a green CI state; nothing breaks the prior story.

---

## Notes

- `[P]` = different file, no dependency on incomplete tasks.
- `[US1]` / `[US2]` / `[US3]` = traceability label per the Constitution Principle I (Spec-Driven Development).
- Tests MUST FAIL before the corresponding implementation lands. Verify this on each test task's commit.
- Commit after each task or each parallel batch (Conventional Commits, English-only, no AI attribution per `~/.claude/CLAUDE.md`).
- Pre-push gates (T073) MUST be re-run after every rebase per the `pr` skill.
- `data-model.md` § "Cross-cutting invariants" lists 5 preservation theorems. T042 (`BootstrapTokenStateMachineTests`), T039 (concurrent same-token), T040 (audit-or-no-issue), T041 (plaintext-once), T063 (revocation-isolation) are the xUnit shadows of those theorems and should be the seed for the future Lean track flagged in `plan.md` § Constitution Check Principle III TODO(LEAN_WORKSPACE).
