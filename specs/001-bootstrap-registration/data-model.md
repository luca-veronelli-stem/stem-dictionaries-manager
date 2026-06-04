# Phase 1 — Data Model

**Feature**: Bootstrap registration for per-installation API credentials
**Branch**: `001-bootstrap-registration`
**Date**: 2026-05-07

Maps the spec's Key Entities into Core domain types and Infrastructure
EF Core entities. State machines from the spec are reproduced here as
explicit transition rules; tests will assert preservation across each
mutation path.

## Entities

### BootstrapToken

Single-use, time-bounded, client-app-scoped credential that authorizes
one and only one registration.

**Domain (`Core/Models/Auth/BootstrapToken.cs`)**

| Field | Type | Notes |
|-------|------|-------|
| `Id` | `int` | Surrogate primary key. |
| `ClientApp` | `string` | Free-text scope (matches `ApiKeys` config keys: `ButtonPanelTester`, `GlobalService`, `StemDeviceManager`, `ProductionTracker`). Required, non-empty. |
| `SecretHash` | `string` | PBKDF2 derivation of the plaintext token, per R3 format. Never the plaintext. |
| `MintedAt` | `DateTime` (UTC) | Monotonically assigned at creation. |
| `ExpiresAt` | `DateTime` (UTC) | `MintedAt + ttl`, where `ttl ∈ [1 h, 90 d]` (FR-007). |
| `Status` | `BootstrapTokenStatus` enum | `Issued | Used | Expired | Revoked`. |
| `UsedAt` | `DateTime?` (UTC) | Set when transitioning to `Used`. |
| `ConsumedByInstallationId` | `int?` | FK to `Installation.Id` when `Status = Used`. |
| `RevokedAt` | `DateTime?` (UTC) | Set when transitioning to `Revoked`. |

**State machine** (per spec Key Entities):

```
        register success
Issued ─────────────────────► Used        (irreversible)
   │
   │ time > ExpiresAt          (logical, evaluated at read time;
   ├─────────────────────► Expired         no DB update needed)
   │
   │ admin revoke action
   └─────────────────────► Revoked        (out of scope for this
                                           feature — revoke surface
                                           is on Installation, not
                                           on tokens — but the
                                           enum value exists for
                                           future use and contract
                                           completeness.)
```

`Expired` is a derived state evaluated as `Now > ExpiresAt && Status =
Issued`. The DB stores `Status` as `Issued` until the row transitions
to `Used` or `Revoked`. The validator computes effective state on read.

**Validation rules**:

- `ClientApp` MUST be non-empty (`ArgumentException.ThrowIfNullOrWhiteSpace`).
- `ExpiresAt - MintedAt ∈ [1 hour, 90 days]` enforced at mint time
  (FR-007); values outside the interval throw at construction.
- `Status` transitions out of `Issued` are irreversible and
  monotonic. Asserted by `BootstrapTokenService` and an integration
  test ("transition matrix").

**Infrastructure (`Infrastructure/Entities/Auth/BootstrapTokenEntity.cs`)**

Same field set as the domain model. EF mapping:

- Index on `Status` (admin list queries).
- Unique index on `SecretHash` (defensive — collisions are
  cryptographically impossible but make the invariant load-bearing).

### Installation

Per-(client app, OS user, machine) identity created by a successful
registration.

**Domain (`Core/Models/Auth/Installation.cs`)**

| Field | Type | Notes |
|-------|------|-------|
| `Id` | `int` | Surrogate PK. |
| `ClientApp` | `string` | Inherited from the consuming token's scope. Required. |
| `OsUserId` | `string?` | From the descriptor (e.g. Windows SID, POSIX `UID:username`, or a SHA-256 hash of either — server-opaque). Nullable — required only when the consumer's `DescriptorPolicy` sets `OsUserIdRequired = true` (see `contracts/register.md`). |
| `MachineId` | `string?` | From the descriptor (per-machine fingerprint, raw or hashed — server-opaque). Nullable — required only when the consumer's `DescriptorPolicy` sets `MachineIdRequired = true`. |
| `InstallGuid` | `Guid` | From the descriptor (per-install unique, client-generated 128-bit GUID). Required and non-`Guid.Empty` — universal contract-level invariant, not per-policy (every platform can generate a GUID). |
| `AppVersion` | `string?` | From the descriptor. Nullable — older clients may omit. When present MUST conform to SemVer 2.0 (validated at request-processing time; malformed → `DescriptorMalformed → 400`). |
| `DescriptorJson` | `string` | Full descriptor as submitted, serialized. For audit/ops introspection. |
| `RegisteredAt` | `DateTime` (UTC) | Set at creation. |
| `Status` | `InstallationStatus` enum | `Active | Revoked`. |
| `RevokedAt` | `DateTime?` (UTC) | Set when transitioning to `Revoked`. |

**State machine**:

```
              admin revoke action
Active ─────────────────────────────► Revoked    (irreversible)
```

**Validation rules**:

- `ClientApp` and `InstallGuid` MUST be non-empty / non-default — the
  two universal identity fields. `ClientApp` must additionally be a
  registered key in the per-`clientApp` `DescriptorPolicy` registry
  (unknown `clientApp` → 401, conflated with token-unknown).
  `InstallGuid` must not be `Guid.Empty` (`InstallGuidInvalid → 400`).
- `OsUserId` and `MachineId` are nullable in storage. The per-`clientApp`
  `DescriptorPolicy` decides whether the *consumer* must transmit a
  value; the entity itself accepts `null` when the active policy
  permits absence. When present, they are stored as opaque strings —
  no format validation server-side. Consumers MAY hash them
  client-side; see *Privacy posture* in `contracts/register.md` for
  the SHA-256 SHOULD/MUST guidance. The server makes no semantic
  distinction between raw and hashed values.
- `AppVersion`, when present, MUST conform to SemVer 2.0. Malformed
  values are rejected at the request-processing layer
  (`DescriptorMalformed → 400`); only validated values reach the
  entity.
- `Status` transitions are monotonic Active → Revoked.

**Infrastructure (`Infrastructure/Entities/Auth/InstallationEntity.cs`)**

EF mapping:

- Composite index on `(ClientApp, OsUserId, MachineId)` for admin
  list queries and duplicate-detection in tests. The `OsUserId` and
  `MachineId` columns are `NULL`-allowed; the index still functions
  for non-null entries.
- Unique index on `InstallGuid` (non-filtered — `InstallGuid` is
  non-nullable in storage). Defensive: install GUIDs are
  client-generated UUIDs, so collisions are theoretical only, but the
  invariant is load-bearing if a client ever ships a buggy GUID
  generator — see the `InstallGuidInvalid → 400` outcome documented
  in `contracts/register.md`.

### InstallationApiCredential

Long-lived authentication secret bound to one Installation.

**Domain (`Core/Models/Auth/InstallationApiCredential.cs`)**

| Field | Type | Notes |
|-------|------|-------|
| `Id` | `int` | Surrogate PK. |
| `InstallationId` | `int` | FK to `Installation.Id`. |
| `SecretHash` | `string` | PBKDF2 derivation of the plaintext credential, per R3. Never the plaintext. |
| `IssuedAt` | `DateTime` (UTC) | Set at creation. |
| `Status` | `InstallationStatus` enum | `Active | Revoked`. Mirrors the owning Installation's status. |
| `RevokedAt` | `DateTime?` (UTC) | Set when transitioning to `Revoked`. |

**Lifecycle relationship to Installation**: One Installation holds
**zero or more** credentials over its lifetime. At any instant, at
most one of those is `Status = Active`; the rest linger as
`Revoked` historical rows preserved for forensics (the prior
`SecretHash` value is kept). The at-most-one-Active invariant is
enforced by a filtered unique index — see *Cross-cutting invariants*
§ 6 below. The relationship was 1:1 in spec 001 (`HasOne.WithOne`
mapping); spec 002 (#71) shifted it to 1:N (`HasMany.WithOne`) to
support the re-registration flow.

Revoking the Installation revokes the Active credential atomically in
the same transaction. Re-registration (spec 002 / #71) flips the
prior Active credential to Revoked and inserts a new Active one
within a single transaction.

**Validation rules**:

- `SecretHash` MUST be the R3 self-describing format.
- `Status` transitions are monotonic Active → Revoked, mirroring
  Installation.

**Infrastructure (`Infrastructure/Entities/Auth/InstallationApiCredentialEntity.cs`)**

EF mapping:

- Index on `InstallationId` (non-unique after spec 002; the 1:1
  `HasOne.WithOne` mapping that implicitly carried a unique constraint
  was replaced by the 1:N `HasMany.WithOne` mapping).
- Filtered unique index `UX_InstallationApiCredentials_Active` on
  `(InstallationId) WHERE Status = 0` (= `Active`) — enforces the
  at-most-one-Active invariant (see § *Cross-cutting invariants* 6).
  Migration `MultiActiveCredentialPerInstallationGuard` introduces it.
- Unique index on `SecretHash` (same defensive rationale as
  BootstrapTokenEntity).

### RegistrationEvent

Audit-trail record of one registration attempt (success or failure).

**Domain (`Core/Models/Auth/RegistrationEvent.cs`)**

| Field | Type | Notes |
|-------|------|-------|
| `Id` | `int` | Surrogate PK. |
| `OccurredAt` | `DateTime` (UTC) | Set at creation. |
| `ClaimedClientApp` | `string?` | From the descriptor; nullable for malformed-descriptor failures. |
| `ClaimedOsUserId` | `string?` | From the descriptor; nullable. Stored opaquely (raw or hashed). |
| `ClaimedMachineId` | `string?` | From the descriptor; nullable. Stored opaquely (raw or hashed). |
| `ClaimedInstallGuid` | `Guid?` | From the descriptor; nullable. |
| `ClaimedAppVersion` | `string?` | From the descriptor; nullable (older clients omit; malformed-descriptor failures also leave this null). |
| `SourceIp` | `string` | From `HttpContext.Connection.RemoteIpAddress`. Required. |
| `DescriptorJson` | `string?` | Full descriptor as submitted, raw JSON. Nullable for empty-body attempts. |
| `Outcome` | `RegistrationOutcome` enum | See below. |
| `ResultingInstallationId` | `int?` | Set only on `Success`. |

**`RegistrationOutcome` enum** (`Core/Enums/Auth/RegistrationOutcome.cs`):

| Value | When |
|-------|------|
| `Success` | Token validated, installation created, credential issued. |
| `TokenMissing` | Request body had no token. |
| `TokenInvalid` | Token did not match any stored hash. |
| `TokenAlreadyUsed` | Token matched a row whose `Status = Used`. |
| `TokenExpired` | Token matched a row whose effective state is `Expired`. |
| `TokenRevoked` | Token matched a row whose `Status = Revoked`. |
| `ClientScopeMismatch` | Token's `ClientApp` ≠ descriptor's `clientApp`. |
| `DescriptorMalformed` | Descriptor missing required fields or unparseable. |
| `AuditFailure` | DB error while writing the event itself; sentinel value used only by tests, never persisted (the failure mode collapses into a 500 response per FR-013). |
| `ReRegistrationSuccess` | Spec 002 / #71 — re-registration happy path. Wire response identical to `Success` (200 + new credential body); audit value distinct so operators can filter for re-registrations. |
| `ExistingInstallationRevoked` | Spec 002 / #71 — re-registration rejected because the matched Installation row's own `Status` is `Revoked`. Maps to `423 Locked` (since #85): fires after token + scope validation, so it is distinguishable per the narrowed FR-002. Installation is NOT auto-unrevoked. |

**Outcome vs. wire status**: the exact `Outcome` value is always
recorded server-side on the `RegistrationEvent` row. Per the narrowed
FR-002 (clarification 2026-05-18), the wire status is conflated to
`401 { "error": "registration failed" }` **only** for the three
scope-related outcomes (`TokenInvalid`, `ClientScopeMismatch`, and the
unknown-`clientApp` policy-lookup miss); every other outcome carries
its own RFC-meaningful status (`400`/`409`/`410`/`423`/`500`) per
`contracts/register.md`. The failure body envelope stays
`{ "error": "registration failed" }` across all failure statuses
(`500` uses `{ "error": "audit failure" }`).

**Infrastructure (`Infrastructure/Entities/Auth/RegistrationEventEntity.cs`)**

EF mapping:

- Index on `OccurredAt` (chronological queries).
- Index on `(ClaimedClientApp, OccurredAt)` (per-client triage).
- Index on `SourceIp` (per-source triage for follow-up rate-limit work
  noted in spec edge cases).

### InstallationDescriptor (DTO)

The wire-level descriptor in the `POST /register` request body. Not a
persisted entity; mirrored by the persisted `Installation` fields and
the `DescriptorJson` blob on both `Installation` and `RegistrationEvent`.

**API DTO (`API/Dtos/Auth/InstallationDescriptorDto.cs`)**

| JSON field | C# type | Schema-required | Per-`clientApp` policy |
|---|---|---|---|
| `clientApp` | `string?` | yes (lookup miss → 401) | n/a — required by the lookup mechanism |
| `osUserId` | `string?` | no | `DescriptorPolicy.OsUserIdRequired` |
| `machineId` | `string?` | no | `DescriptorPolicy.MachineIdRequired` |
| `installGuid` | `Guid?` | yes (non-`Guid.Empty`) | n/a — universal contract-level invariant |
| `appVersion` | `string?` | no (SemVer 2.0 when present) | n/a |

Additional properties are accepted and round-tripped into
`DescriptorJson` for audit but are not validated.

## Relationships

```
BootstrapToken  1 ──── 0..1  Installation       (consumed-by)
Installation    1 ──── 1     InstallationApiCredential
RegistrationEvent  *  ──── 0..1  Installation   (only on Success)
```

`BootstrapToken.ConsumedByInstallationId` is `null` until the
successful registration commits, then becomes the new
`Installation.Id` in the same transaction.

## Audit split — `AuditEntry` vs `RegistrationEvent`

Two audit surfaces, by request authority:

| Event source | Authenticated by admin | Audit table |
|---|---|---|
| Admin mints a bootstrap token | yes | `AuditEntry` (existing) |
| Admin revokes an installation | yes | `AuditEntry` (existing) |
| Admin lists installations (read-only) | yes | (no audit — queries are not state mutations) |
| Client calls `POST /register` (any outcome) | **no** (pre-auth) | `RegistrationEvent` (new) |

Existing `AuditEntityType` enum is extended with two values:

- `BootstrapToken` — for admin mint events.
- `Installation` — for admin revoke events.

Both extensions are additive (new enum values appended at the end);
no existing consumer breaks.

`AuditEntry.ChangedById` is non-nullable (FK → `UserEntity`). Admin
API-key callers do not have an organic per-request `User`. The
migration seeds a single shared user:

```text
UserEntity { Username = "system-admin",
             DisplayName = "System Admin (API key)" }
```

A new `AdminAuthenticationMiddleware` (runs after `ApiKeyMiddleware`,
only on `/api/admin/*`) sets `ICurrentUserProvider.CurrentUserId` to
the seeded user's id whenever the incoming `X-Api-Key` matches an
entry in the new `AdminApiKeys` configuration section. The existing
`ICurrentUserProvider` singleton is replaced with a per-request
`HttpContext`-aware implementation in the API composition root
(behaviour for the GUI is unchanged — it continues to use the
singleton-set value).

The two surfaces stay separate because the unauthenticated `/register`
path has no `User` to attribute, no fixed `EntityId` (failure modes
produce no entity), and a disjoint field set (claimed app/user/machine
+ source IP + descriptor JSON + outcome category). See R2 in
`research.md` for the full rationale.

## Migration

Single EF Core migration, name `AddBootstrapRegistration`:

```powershell
dotnet ef migrations add AddBootstrapRegistration `
    -p src/Infrastructure -s src/API
```

Tables created: `BootstrapTokens`, `Installations`,
`InstallationApiCredentials`, `RegistrationEvents`. No alterations to
existing tables. No data migration needed (legacy `ApiKeys` config keys
remain in `appsettings.json` as today; they are not migrated to DB
rows — that would be the union semantic flipping into a deprecation,
which is explicitly **not** this feature per Edge Case "Existing legacy
API keys remain valid in parallel" and per the constitution).

## Cross-cutting invariants (preservation theorems for future Lean track)

1. **Single-use token consumption**: at most one Installation row
   exists with `BootstrapToken.ConsumedByInstallationId = T.Id` for
   any given token `T`. Enforced by the DB transaction in
   `RegistrationService.RegisterAsync` plus a unique constraint on
   `BootstrapTokens.ConsumedByInstallationId` (filter:
   `ConsumedByInstallationId IS NOT NULL`).
2. **No-credential-without-installation**: every
   `InstallationApiCredential` row has a non-null `InstallationId` FK.
   Enforced by EF's required FK.
3. **Audit-or-no-issue**: every `Success` outcome in
   `RegistrationEvents` has a corresponding
   `InstallationApiCredentials` row, and the audit row commits in the
   same transaction. Enforced by `RegistrationService` wrapping all
   three writes (`BootstrapToken.Status = Used`, `Installation INSERT`,
   `InstallationApiCredential INSERT`, `RegistrationEvent INSERT`) in a
   single SaveChangesAsync call.
4. **Plaintext-once**: the plaintext bootstrap token and plaintext API
   credential each appear in exactly one server-side surface — the
   admin-mint response (token) and the `/register` response
   (credential). Asserted by an integration test that scans the
   request/response/log capture for the plaintexts after each call.
5. **Revocation isolation** (FR-006): revoking
   `InstallationApiCredential` X does not modify the `Status` of any
   other credential. Asserted by integration tests in the
   "Story 3 — independent revoke" suite.
6. **At-most-one-Active credential per Installation** (spec 002 / #71):
   at any instant, at most one row in `InstallationApiCredentials`
   has `(InstallationId = X) AND (Status = Active)` for any
   installation `X`. Enforced by the filtered unique index
   `UX_InstallationApiCredentials_Active` (`HasFilter("[Status] = 0")
   .IsUnique()`). The re-registration path (spec 002) revokes the
   prior Active row **before** inserting the new Active row inside a
   single transaction, upholding the invariant by ordering.
