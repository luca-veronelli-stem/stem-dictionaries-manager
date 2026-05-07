# Phase 0 — Research

**Feature**: Bootstrap registration for per-installation API credentials
**Branch**: `001-bootstrap-registration`
**Date**: 2026-05-07
**Inputs**: [`spec.md`](./spec.md), [`.specify/memory/constitution.md`](../../.specify/memory/constitution.md) v1.0.1

This document closes the technical questions that the spec deliberately
deferred to planning, plus a handful of stack/placement decisions the
plan template requires. Each subsection follows the
**Decision / Rationale / Alternatives considered** shape.

## R1 — Admin surface shape

> Spec FR-009/FR-010/FR-011 require an admin interface but do not
> prescribe its shape. Plan-mode plan listed this as deferred.

**Decision**: HTTP endpoints under `/api/admin/*`, authenticated by the
existing `ApiKeyMiddleware` (admin keys live alongside service keys in
the `ApiKeys` configuration section, gated by a new `AdminApiKeys`
section so non-admin keys cannot mint or revoke).

Endpoints (full contracts in `contracts/`):

- `POST /api/admin/bootstrap-tokens` — mint
- `GET  /api/admin/installations` — list
- `POST /api/admin/installations/{id}/revoke` — revoke

**Rationale**:

- Single deliverable. The repo already exposes an HTTP surface and the
  same middleware/CI/auth path serves it. A CLI would need its own
  connection-string resolution, its own configuration story
  (`appsettings.json` discovery), and its own Bitbucket-mirror-friendly
  release surface.
- Operator workflow already lives over HTTP (curl/Postman against the
  API server). A CLI would be a second tool to install on each admin
  workstation.
- Auth split via a separate config section keeps the existing union
  semantics (`ApiKeys` continues to authorize non-admin endpoints) and
  isolates admin authority without inventing a role system in this
  feature.

**Alternatives considered**:

- `dotnet` CLI tool only — rejected: duplicates auth/connection wiring,
  needs its own publish pipeline, and offers no operator value over
  curl + an HTTP endpoint.
- Both CLI and HTTP — rejected: rule-of-three not met. If a second
  surface becomes necessary later (e.g., for offline minting) it can be
  added without touching the HTTP path.

## R2 — Registration audit storage

> Spec FR-012/FR-013 require a per-attempt audit record with fields
> (timestamp, claimed app, claimed user, claimed machine, source IP,
> full descriptor, outcome). Constitution Security & Auditability
> says the storage shape — extend `AuditEntry` vs. a dedicated table —
> is decided per-feature in `/speckit-plan`.

**Decision**: A dedicated `RegistrationEvent` table (entity
`RegistrationEventEntity`, repository `IRegistrationEventRepository`),
**not** a reuse of `AuditEntry`.

**Rationale**:

- `AuditEntryEntity` is keyed on `(EntityType, EntityId, ChangedById)`
  with a non-nullable `ChangedById` FK to `UserEntity`. A registration
  attempt happens **before** any authentication has succeeded — there
  is no current user to attribute the event to.
- Failure-mode attempts (invalid token, malformed descriptor,
  client-scoping mismatch) do not produce an entity, so `EntityId`
  would have to become nullable across the whole audit table — a
  semantic regression on the existing CRUD audit invariants.
- The spec's required field set (claimed app, claimed user, claimed
  machine, source IP, full descriptor JSON, outcome category,
  on-success installation id) is disjoint from `AuditEntryEntity`'s
  field set. Squeezing it into `Notes` JSON would defeat queryability
  for SC-005's "100% of attempts in audit log within 2 s" assertion.

**Note on admin-side mutations** (mint, revoke): these happen
*authenticated* via `AdminApiKeys` and are state mutations, so the
constitution's "Audit on every state mutation" applies. They write to
the existing `AuditEntry` table (`AuditEntityType` extended with
`BootstrapToken` and `Installation`). To satisfy `AuditEntry`'s
non-nullable `ChangedById` FK without inventing a per-request user
mapping, the migration seeds a single `UserEntity { Username =
"system-admin", DisplayName = "System Admin (API key)" }` and the API
sets `ICurrentUserProvider.CurrentUserId` to that user's id when the
incoming `X-Api-Key` matches an entry in `AdminApiKeys`. Multi-admin
attribution (per-key user) is a follow-up if operationally needed.

**Alternatives considered**:

- Extend `AuditEntry` with nullable `ChangedById` and a new
  `AuditEntityType.RegistrationAttempt` — rejected: weakens the
  user-attributable invariant of the existing audit path; forces every
  consumer of `AuditEntry.ChangedBy` to handle null.
- Append-only log file — rejected: SC-005 wants assertable presence in
  ≤ 2 s, and DB/file divergence is a known-bad failure mode.
- Both `AuditEntry` row + `RegistrationEvent` row for the unauth
  `/register` path — rejected: double bookkeeping for one event, no
  operator gain. Note this is distinct from the *admin*-side dual write
  described in the previous paragraph (which is one event in
  `AuditEntry` only — admin actions have no row in `RegistrationEvent`).

## R3 — Hashing algorithm and salt strategy

> Spec FR-004/FR-014 require non-reversible at-rest storage for both
> bootstrap tokens and API credentials. Constitution Security &
> Auditability lists `PBKDF2 / SHA-256 HMAC` as acceptable; choice is
> per-feature.

**Decision**: `PBKDF2-HMAC-SHA256`, 600 000 iterations, per-secret
16-byte cryptographically random salt, 32-byte derived key. Stored as
`pbkdf2-sha256$<iterations>$<salt-b64>$<hash-b64>` in a single string
column. Same scheme for bootstrap tokens and per-installation API
credentials. Verification via
`CryptographicOperations.FixedTimeEquals` on the derived bytes.

**Rationale**:

- The plaintext secrets are server-minted, 32 bytes of CSPRNG output
  base64url-encoded — there is no offline-dictionary risk. The
  derivation cost is paid as a hedge: if a DB dump ever combines with a
  weakly-generated token (custom client or future migration), PBKDF2's
  iteration count converts the attack from instantaneous to bounded.
- 600 000 iterations is the OWASP 2023 PBKDF2-SHA256 baseline. On the
  API host this costs ~50 ms per call. The validation hot path is
  cached (R4); the cost is paid once per `/register` and once per
  cache-miss on subsequent requests.
- Per-secret random salt rules out rainbow tables and prevents
  cross-secret correlation in a DB dump.
- The self-describing string format (algorithm$iterations$salt$hash)
  keeps the iteration count next to the bytes it produced — no
  parallel `iterations` column to drift out of sync.
- `Rfc2898DeriveBytes` and `CryptographicOperations.FixedTimeEquals`
  are in `System.Security.Cryptography` — no new package dependency.

**Alternatives considered**:

- Plain SHA-256 HMAC with a server-side pepper — rejected: pepper
  rotation is its own ops problem and provides no benefit over PBKDF2's
  embedded iteration count for this threat model.
- Argon2id — rejected: not in `System.Security.Cryptography`; pulling
  `Konscious.Security.Cryptography` for one secret type is unjustified
  scope.
- bcrypt — rejected: not in stdlib, same reason as Argon2id.

## R4 — Validation cache for the 5-second revocation latency

> Spec FR-011/SC-004 require revocation to take effect within 5 s.
> Edge case "Revocation latency" explicitly permits an in-process
> cache with TTL ≤ 5 s or explicit invalidation.

**Decision**: `IMemoryCache` per process, 5-second absolute TTL on the
key `Sha256(plaintext)` → `(installationId, isActive)`. On admin
revoke, also call `cache.Remove(key)` synchronously after the DB
update commits. The TTL is the safety net against multi-instance
deployments; single-process deployments benefit from explicit
invalidation as the steady-state path.

**Rationale**:

- Without a cache, every authenticated call would PBKDF2-verify the
  credential against the DB row → ~50 ms per request. Unacceptable
  steady-state.
- The cache key is `Sha256(plaintext)` (not the plaintext itself) so
  that the cache contents are not sensitive in a memory dump beyond
  what the caller has already supplied.
- 5 s TTL is the spec ceiling. Going lower buys nothing and trades
  cache-hit ratio for nothing.

**Alternatives considered**:

- No cache — rejected: regresses authenticated-request latency by ~50
  ms per call.
- Explicit invalidation only (no TTL) — rejected: silently breaks the
  5 s ceiling in multi-instance future deployments.
- Distributed cache (Redis) — rejected: spec assumption is
  single-tenant, single API server. Redis is unjustified ops weight.

## R5 — Token wire format

> Spec does not constrain the wire format of bootstrap tokens or API
> credentials beyond "opaque" and "non-reversible storage". The plan
> needs a concrete shape so contracts can describe response payloads.

**Decision**: 32 bytes of CSPRNG output, base64url-encoded (no `=`
padding), prefixed with a literal type tag for at-a-glance disambig:

- Bootstrap tokens: `stbt_` + base64url(32 random bytes)
- API credentials: `stak_` + base64url(32 random bytes)

Total length: 5 + 43 = 48 ASCII chars.

**Rationale**:

- 32 bytes = 256 bits of entropy. Comfortably above any practical
  brute-force horizon.
- Type prefix is an operational affordance (grep, log triage) not a
  security claim — entropy comes from the 32 bytes.
- Base64url avoids `+` and `/` so the token is URL-safe and shell-safe
  for out-of-band distribution (USB drop, email, internal portal).

**Alternatives considered**:

- GUID — rejected: 122 bits of entropy, visually conflatable with
  internal IDs.
- JWT — rejected: adds wire size and a JWT library dependency for an
  opaque single-use credential. No claims to carry.

## R6 — Module placement

> Plan-mode plan suggested `src/Auth.Registration/` as a new project.
> Re-evaluated against `docs/Standards/REPO_STRUCTURE.md` and the
> existing archetype-A layer split.

**Decision**: No new project. Add the registration code under the
existing layers using an `Auth/` subfolder in each:

- `src/Core/Models/Auth/` — `BootstrapToken`, `Installation`,
  `InstallationApiCredential`, `RegistrationEvent`,
  `InstallationDescriptor`
- `src/Core/Enums/Auth/` — `BootstrapTokenStatus`,
  `InstallationStatus`, `RegistrationOutcome`
- `src/Services/Auth/` — `BootstrapTokenService`,
  `RegistrationService`, `InstallationCredentialService`,
  `InstallationCredentialValidator`
- `src/Infrastructure/Entities/Auth/` and
  `src/Infrastructure/Repositories/Auth/` — entities + repositories,
  with EF Core configuration alongside
- `src/API/Endpoints/Auth/` — `RegistrationEndpoints` (the
  `POST /register` endpoint) and `AdminAuthEndpoints` (the three
  admin endpoints)
- `src/API/Middleware/` — modify `ApiKeyMiddleware` to validate either
  legacy `ApiKeys` config keys **or** DB-issued installation
  credentials (union)

**Rationale**:

- The repo's current layer split is `Core / Services / Infrastructure
  / API / GUI.Windows`. A new `Auth.Registration` project would either
  duplicate that split internally (over-engineered) or sit
  layer-orthogonally and break the onion shape.
- Folder boundaries inside each layer give a clean future extraction
  path: when a second API needs the same flow, lift each
  `*/Auth/*` folder into a `Stem.Auth.Bootstrap` package along the
  same seams.
- v1 REPO_STRUCTURE cheat-sheet says domain types go in `Core`, use
  cases in `Services`, EF in `Infrastructure`. The decision matches
  that without inventing a new column.

**Alternatives considered**:

- Dedicated `src/Auth.Registration/` project — rejected per above.
- Flat addition (no `Auth/` subfolder) — rejected: loses the future
  extraction boundary at zero structural cost.

## R7 — Hardware-bound descriptor element

> Spec assumption: "A hardware-bound element of the
> installation descriptor (machine GUID binding). Noted in issue #1
> as a related design question for a follow-up; not part of this
> feature."

**Decision**: Out of scope, confirmed. The `machineId` field on the
descriptor is operator-supplied free-form (typically a stable per-
machine fingerprint the client computes — e.g. Windows machine GUID,
POSIX `/etc/machine-id`). The server records it on the Installation
and emits it in the audit row but does **not** cryptographically bind
the API credential to it. A follow-up feature can layer that on
without breaking the wire contract by introducing a server-issued
nonce that the client must HMAC with a hardware-bound key.

## Stack confirmations (no decision needed, recorded for plan template)

- **Language/version**: C# 13 on .NET 10 (`net10.0`), `<Nullable>enable</Nullable>`
  per repo BUILD_CONFIG. F# migration of `Core` is Phase 3 of repo CLAUDE.md;
  this feature stays C# to land before that migration.
- **Primary dependencies**: ASP.NET Core minimal APIs (already in `API`),
  EF Core 10 (Sqlite + SqlServer providers, already in `Infrastructure`),
  `Microsoft.Extensions.Caching.Memory` (new — for R4),
  `System.Security.Cryptography` (BCL — for R3 and R5).
- **Storage**: existing `AppDbContext` (SQLite for dev/test, SQL Server
  for prod, per existing `DatabaseProvider` config switch). Three new
  tables: `BootstrapTokens`, `Installations`,
  `InstallationApiCredentials`, `RegistrationEvents`.
- **Testing**: xUnit in `tests/Tests/Tests.csproj`, with new
  integration tests under `tests/Tests/Integration/API/Auth/` (using
  the existing `ApiIntegrationTestBase` SQLite-in-memory fixture) and
  unit tests for the credential hash + validator under
  `tests/Tests/Unit/Services/Auth/`. Per the constitution Principle
  III, manual fakes only — no Moq/NSubstitute.
- **Target platform**: ASP.NET Core API server, cross-platform. Client
  side (DPAPI usage) is out of scope for this server-side feature.
- **Project type**: Web service (existing `API` project).
- **Performance goals**: SC-001 (registration end-to-end < 5 s under
  normal network), SC-004 (revoke effective within 5 s), SC-005 (100%
  of attempts in audit within 2 s). The validation cache (R4) keeps
  per-call auth cost in the sub-millisecond range steady-state.
- **Constraints**: must not regress legacy `ApiKeys` auth (FR-005,
  union mode); must not store any plaintext secret server-side
  (FR-004/FR-014/SC-007).
- **Scale/scope**: STEM-internal — order of tens of installations per
  client app family across all of STEM. Single-tenant.
