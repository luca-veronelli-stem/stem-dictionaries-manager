# Feature Specification: Bootstrap registration for per-installation API credentials

**Feature Branch**: `001-bootstrap-registration`
**Created**: 2026-05-07
**Status**: Draft
**Input**: GitHub issue [luca-veronelli-stem/stem-dictionaries-manager#1](https://github.com/luca-veronelli-stem/stem-dictionaries-manager/issues/1) — *"feat(api): expose /register bootstrap endpoint for per-installation API credentials"*.

## Clarifications

### Session 2026-05-07

- Q: How is client identity modeled, and what is the granularity of an
  issued credential? → A: A "client" is an **app family** identified by
  a free-text string (matching the existing `ApiKeys` config shape:
  `ButtonPanelTester`, `GlobalService`, `StemDeviceManager`,
  `ProductionTracker`). One issued credential corresponds to one
  **(app, OS user, machine) tuple**, stored under the host OS's
  per-user secret store (DPAPI `CurrentUser` on Windows, equivalent
  elsewhere) so only the OS user that registered can decrypt it.
  Admins mint one bootstrap token per (OS user, machine) pair and
  distribute the plaintext token to the intended user out-of-band
  (email, internal portal, USB, etc.). The token's cryptographic
  scope is the client app family only; the (user, machine) binding
  is enforced operationally (single-use + admin distribution
  discipline), not encoded in the token.
- Q: How quickly must a revoked installation's API credential stop
  authenticating? → A: **Within 5 seconds** of the admin's revoke
  action. This permits an in-process credential-validity cache with a
  TTL of up to 5 seconds (or with explicit invalidation on revoke) on
  the API's authentication hot path, cutting DB load while remaining
  operationally indistinguishable from instant for incident response.
  The 5 s ceiling matches SC-004's existing target; stricter
  guarantees (instant on next request, no caching) are not required.
- Q: What are the lower and upper bounds on bootstrap-token TTL,
  around the 30-day default? → A: **Minimum 1 hour, maximum 90 days,
  default 30 days.** 1 h is enough for an admin to mint a token,
  transmit it out-of-band (email, internal portal, USB) and have the
  user consume it on first launch in worst-case async workflows.
  90 d is a strong upper bound aligned with common credential-
  rotation cadences and caps the worst-case leak exposure of a token
  embedded in distribution media. The 30-day default is unchanged
  and matches issue #1's suggestion. The admin MAY override the
  default per-token within `[1 h, 90 d]`; values outside that
  interval MUST be rejected at mint time.

### Session 2026-05-07 (cont.)

Triggered by review of the consumer-side draft contract at
`stem-button-panel-tester/specs/001-dictionary-from-api/contracts/register-api.md`
(written 2026-05-06, before this spec's first clarify session).

- Q: Should the unified-`401`-on-failure decision (FR-002 / SC-002) be
  reversed in favor of the consumer's distinguishable
  `401 token_invalid` / `403 supplier_revoked` /
  `409 already_registered` shape? → A: **No, keep unified.**
  Distinguishable codes are a token-status oracle for an attacker
  harvesting tokens from disassembled installers (the explicit threat
  model). Differentiation lives in `RegistrationEvents.Outcome`
  server-side, queryable by admins. Idempotent re-registration on
  partial-failure (the consumer's `409 → return existing key`) is also
  rejected — it would require either re-issuing plaintext (violates
  "plaintext returned exactly once") or storing plaintext server-side
  (the anti-pattern this feature exists to fix). Recovery on partial
  failure is operator-side: revoke the half-created Installation, mint
  a fresh bootstrap token.
- Q: Adopt an `appVersion` field on the installation descriptor for
  ops correlation (per consumer contract)? → A: **Yes, optional
  semver string.** Persisted on Installation and RegistrationEvent for
  forward-compat. Older clients that omit it record `null`.
- Q: How does `descriptor.clientApp` relate to repo names vs the
  existing `ApiKeys` config-key convention? → A: **Free-text matching
  the bootstrap token's minted scope, by convention the `ApiKeys`
  config-key form** (`ButtonPanelTester`, `GlobalService`,
  `StemDeviceManager`, `ProductionTracker`). No server-side
  repo-name → config-key map. The admin mints with the canonical
  string; the consumer hard-codes the same string in its source.
- Q: Should the descriptor's `osUserId` and `machineId` be hashed
  client-side (per consumer's draft) or sent raw? → A: **Server-opaque
  — consumer's choice.** The server stores whatever stable string the
  consumer sends; both raw values (Windows SID, POSIX `UID:username`,
  SMBIOS UUID, `/etc/machine-id`) and consumer-applied SHA-256 hashes
  are valid. Hashing is permitted but not required; the privacy
  trade-off (smaller leak blast radius vs. ops debuggability) is the
  consumer's call to make.

### Session 2026-05-18

Triggered by `button-panel-tester`'s integration against the unreleased
`/register` (issue #54). Decisions taken in the design review between
the dictionaries-manager owner and the button-panel-tester owner.

- Q: Should the unified-`401`-on-failure decision (FR-002 / SC-002,
  affirmed in the 2026-05-07 session) be reversed in favor of distinct
  RFC-meaningful status codes per failure mode? → A: **Yes, narrow
  the conflation.** The 401 collapse now applies *only* to
  unknown-token + scope-mismatch + unknown-`clientApp` — the three
  failure modes that would otherwise leak which app a token was
  scoped to. Token already-used → 409; expired → 410; revoked →
  423; descriptor malformed / missing-by-policy / `installGuid` =
  `Guid.Empty` → 400. Refined threat model: the bootstrap token's
  43-char base64url payload (≈ 2^258 entropy) makes random-guess
  token enumeration infeasible regardless of error-code
  distinguishability; preserving distinct codes for non-scope-related
  failures dramatically improves consumer-developer diagnostics
  without giving a real attacker any additional foothold. The audit
  log continues to record the exact `RegistrationOutcome` so
  server-side ops still see the full picture.
- Q: Should descriptor fields (`osUserId`, `machineId`, `installGuid`)
  remain uniformly required at the DTO layer? → A: **No, split the
  enforcement.** `osUserId` and `machineId` move to per-`clientApp`
  `DescriptorPolicy` enforcement at the service layer (mobile apps
  lack a stable per-machine identifier; web apps can't hash a SID;
  headless services may have no per-user identity). `installGuid`
  stays a universal contract-level invariant — every platform can
  generate a random 128-bit GUID client-side, so per-policy nullability
  has no realistic consumer. `Installation.OsUserId` and
  `Installation.MachineId` become nullable on the entity (small EF
  migration); `InstallGuid` stays non-null with its unique index
  intact.
- Q: Should `installGuid` = `Guid.Empty` continue to be conflated
  into the generic `DescriptorMalformed` outcome? → A: **No, split
  into `InstallGuidInvalid → 400`.** The schema-level `Guid?` parse
  alone is not enough — `Guid.Parse("00000000-...")` succeeds. Because
  `InstallGuid` has a unique index, a buggy client (hardcoded
  `Guid.Empty`, defaulted `default(Guid)`) would land its first
  registration silently and hit a confusing 500 on the second attempt.
  A distinct `InstallGuidInvalid → 400` outcome surfaces the bug
  immediately. One-line check at the service layer; same
  `{ "error": "..." }` envelope.
- Q: Should `appVersion` continue to be free-text or move to
  SemVer-validated? → A: **Validate as SemVer 2.0.** Accepting
  `"banana"` in production for an ops-correlation field is a
  foot-gun. Malformed → `DescriptorMalformed → 400`. The field stays
  on `RegisterRequestDto` and on `Installation` (no removal).
- Q: Promote SHA-256 hashing of `osUserId` / `machineId` from "MAY"
  to first-class guidance? → A: **Yes.** All consumers SHOULD hash;
  supplier-deployed consumers (e.g. `button-panel-tester` shipped to
  STEM-Ems suppliers) MUST hash, because the OS user and machine
  identifiers cross an organizational data boundary. The wire format
  is identical in both cases (an opaque string); the consumer chooses
  the source before sending.

### Session 2026-06-04

Triggered by issue #85 -- `ExistingInstallationRevoked` (added in #71)
fell through `StatusFor` to the `401` default, contradicting the
2026-05-18 narrowing.

- Q: Should `ExistingInstallationRevoked` (re-registration rejected
  because the matched Installation row's own `Status` is `Revoked`)
  share the conflated `401`, or get a distinct status code? → A:
  **Distinct -- `423 Locked`.** The outcome fires only *after* the
  bootstrap token's validity and the client-app scope have already
  been verified, so by construction it reveals nothing about which
  apps a token is scoped to; the 2026-05-18 narrowing therefore
  applies to it. `423 Locked` mirrors `TokenRevoked` (both denote a
  revoked/locked resource state) and lets the consumer GUI surface
  "this installation was revoked by an admin -- reinstall the app"
  instead of the misleading "token not accepted". The Installation is
  still NOT auto-unrevoked; recovery remains a separate admin flow
  (out of scope for #85). The audit log continues to record the exact
  `RegistrationOutcome`.

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Client app obtains its API credential on first launch (Priority: P1)

A client application that consumes the API (e.g. `stem-button-panel-tester`,
`stem-device-manager`, `stem-production-tracker`, or any future client —
internal STEM service or external supplier alike) is installed on a
machine the API does not control, by a specific OS user. That user has
received a single-use, time-bounded bootstrap token from a STEM admin
out-of-band, intended for their (user, machine) pair. On first launch,
the client application calls the registration interface with that token
plus a self-reported description of the installation (app identifier,
OS user identifier, machine fingerprint, install GUID). The registration
system validates the token, creates a new identity for the
(app, user, machine) tuple, returns a fresh long-lived API credential
to the client, and marks the bootstrap token used. The client stores
the credential in the OS's per-user secret store (DPAPI `CurrentUser`
on Windows, equivalent elsewhere) so that only the OS user that
registered can decrypt it on subsequent API calls. The bootstrap token
is then erased from the client.

**Why this priority**: This is the core flow of the feature. Without it,
the entire premise — replacing plaintext API keys embedded in client
installers with per-installation credentials issued through a single-use
exchange — does not exist. Every other story in this spec presupposes this
one is in place. It is also a hard prerequisite for the client-side work
tracked in `luca-veronelli-stem/stem-device-manager#94` and for
`stem-button-panel-tester`'s `feat/001-dictionary-from-api` feature.

**Independent Test**: Seed the system with a valid, unused, unexpired,
client-scoped bootstrap token. From a fresh client environment that has
**no** API credentials, invoke the registration interface with that token
and a synthetic installation descriptor. Verify the response contains a
fresh API credential. Use the returned credential to call any existing
authenticated endpoint (e.g. `GET /api/devices`) and confirm it succeeds.
Re-invoke the registration interface with the same bootstrap token and
verify the second call is rejected.

**Acceptance Scenarios**:

1. **Given** a valid, unused, unexpired, client-scoped bootstrap token
   T and a fresh client with no prior credentials, **when** the client
   calls the registration interface with T and a well-formed
   installation descriptor, **then** the system returns a fresh API
   credential, creates a new per-installation identity, and marks T
   used.
2. **Given** the same bootstrap token T already consumed by a prior
   successful registration, **when** any client calls the registration
   interface with T, **then** the system rejects the request with an
   "unauthorized" outcome whose response body does not reveal the
   reason.
3. **Given** an expired bootstrap token T, **when** any client calls
   the registration interface with T, **then** the system rejects with
   the same "unauthorized" outcome and identical response body shape as
   scenario 2.
4. **Given** a bootstrap token T scoped to client A, **when** a caller
   whose installation descriptor identifies client B presents T to the
   registration interface, **then** the system rejects with the same
   "unauthorized" outcome and identical response body shape as
   scenario 2.
5. **Given** the API credential returned in scenario 1, **when** the
   client uses that credential on subsequent API calls, **then** those
   calls succeed and are attributable to that specific installation.

---

### User Story 2 — Admin mints a bootstrap token for a client (Priority: P2)

A STEM administrator needs to enable a specific OS user on a specific
machine to start consuming the API as one of the registered client
apps. Through an administrative interface, the admin creates a new
single-use bootstrap token scoped to that client app and hands the
plaintext value to the intended user out-of-band (email, internal
portal, USB, etc.). The system returns the plaintext value to the
admin exactly once (so it can be transmitted) and stores only its
non-reversible representation server-side. The admin can optionally
configure the token's validity window at mint time. The token's
cryptographic scope is the client app family; the (user, machine)
binding is enforced operationally — one token in flight at a time,
single-use semantics, and admin distribution discipline.

**Why this priority**: P2 because it is a hard precondition for P1 in
production use, but P1 can be tested in isolation by seeding tokens
directly into storage (so P1 is independently shippable as an MVP).
Admin minting is required before any real client can register.

**Independent Test**: As an admin, request the creation of a bootstrap
token scoped to a known client identifier. Verify the system returns
a token value of sufficient entropy. Verify the response includes the
token's expiry timestamp. Confirm the token is not retrievable in
plaintext on any subsequent admin call. Use the minted token in a
registration call and confirm User Story 1 succeeds.

**Acceptance Scenarios**:

1. **Given** an admin authenticated with sufficient privilege,
   **when** they request a new bootstrap token for client C,
   **then** the system returns a fresh token value, records its
   expiry, scopes it to C, and marks it unused.
2. **Given** the token returned in scenario 1, **when** the admin or
   anyone else queries for it later, **then** the plaintext token
   value is **not** retrievable — only its existence, client scope,
   expiry, and used/unused state.
3. **Given** the admin specifies a non-default validity window at mint
   time, **when** the token is created, **then** it expires after the
   specified window rather than the system default.

---

### User Story 3 — Admin lists and revokes per-installation credentials (Priority: P3)

A STEM administrator monitors which installations are active and, when
necessary (compromised machine, decommissioned site, client app
rotation), revokes the API credential of one or more specific
installations without affecting other installations of the same client.

**Why this priority**: P3 because P1 and P2 deliver the core security
improvement (no plaintext keys in installers). P3 adds operational
manageability — important for incident response and lifecycle but not
required for the system to start delivering its security benefit.

**Independent Test**: After several P1 registrations have run, as an
admin, list all per-installation credentials and verify each appears
with its client app, OS user identifier, machine identifier, install
GUID, registration timestamp, and status. Revoke one credential.
Verify the credential's owning installation can no longer authenticate
to the API. Verify other installations of the same client app — and
in particular other (OS user, machine) installations — remain
unaffected.

**Acceptance Scenarios**:

1. **Given** N successful registrations across one or more client apps
   and (OS user, machine) tuples, **when** the admin requests the list
   of installations, **then** each appears with its client app, OS
   user identifier, machine identifier, install GUID, registration
   timestamp, and current status (active or revoked).
2. **Given** an active installation I1 of client app C registered by
   OS user U on machine M, **when** the admin revokes I1, **then** the
   API credential issued to I1 is immediately rejected on subsequent
   API calls; **and** all other active installations — including any
   other (user, machine) tuples for client app C, the same user on
   different machines, or the same machine with different users —
   continue to authenticate normally.
3. **Given** a revoked installation, **when** the admin lists
   installations, **then** the revoked one appears with revoked
   status and a revocation timestamp.

---

### Edge Cases

- **Concurrent registration with the same token.** Two simultaneous
  registration calls present the same valid bootstrap token. Exactly
  one MUST succeed (creating one installation identity) and the other
  MUST be rejected as already-used. The system MUST NOT create two
  installation identities from one token.
- **Token replayed after success.** Any subsequent call with a
  consumed token MUST be rejected with the same response body shape
  as any other failure mode (no leakage that the token *was* valid).
- **Malformed installation descriptor.** Empty, missing, or
  un-parseable descriptors MUST be rejected. The descriptor's exact
  schema is part of the registration contract; out-of-shape
  descriptors are treated the same as token-validation failures (no
  distinguishing response).
- **Clock skew on expiry boundary.** A token at exactly its expiry
  instant MUST be rejected. The server's clock is authoritative; the
  client's clock is not consulted.
- **Audit failure during registration.** If the system cannot persist
  the registration audit record, the registration itself MUST be
  treated as failed (the API credential MUST NOT be issued). Audit is
  not best-effort.
- **Client-scoping mismatch where the descriptor self-claims a
  different client than the token's scope.** The token's recorded
  scope is authoritative; the descriptor is informational. A
  mismatch is a client-scoping failure (rejected with the
  unrevealing "unauthorized" response).
- **Existing legacy API keys (the `ApiKeys` configuration section)
  remain valid in parallel.** Per the union decision recorded in the
  constitution's Security & Auditability section, an in-flight
  authenticated API call MAY be authorized either by a legacy
  configured key or by a per-installation key issued through this
  registration flow. This is **not** a deprecation of the legacy
  keys.
- **Revocation latency.** A revoked installation's API credential
  MUST stop authenticating within 5 seconds of the admin action.
  An in-process credential-validity cache with a TTL of up to
  5 seconds (or with explicit invalidation on revoke) is acceptable
  on the authentication hot path; longer is a security regression,
  not an acceptable optimization.
- **Repeated registration attempts from the same network source
  using invalid tokens.** Out of scope for this feature, but the
  audit trail MUST capture enough information for an operator or
  follow-up tool to detect such patterns.
- **Multiple OS users on the same machine each need to consume
  the API.** Each (OS user, machine) pair is a distinct Installation
  and requires its own bootstrap token; the token's single-use
  property prevents two users from sharing one. Admin distribution
  discipline determines who receives which token.
- **The same OS user runs the same client app on a second
  machine.** That second (user, machine) pair is a distinct
  Installation requiring its own bootstrap token. Credentials are
  not roamed; revoking the first installation does not affect the
  second.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST expose a registration interface that
  accepts a bootstrap token and an installation descriptor as inputs
  and, on success, returns a fresh long-lived API credential as
  output. The installation descriptor MUST include: client app
  identifier (matching the bootstrap token's scope), stable OS user
  identifier (e.g. SID on Windows; UID + username on POSIX), stable
  machine identifier (per-machine fingerprint), and install GUID
  (per-install unique value). The OS user and machine identifiers are
  treated as opaque strings server-side — consumers MAY hash them
  before submission for privacy reasons, but the server makes no
  semantic distinction between raw and hashed values. The descriptor
  MAY also include an `appVersion` semver string for ops correlation
  (forward-compat; older clients omitting it record `null`
  server-side). (Issue #1 prescribes the wire-level path as
  `POST /register`.)
- **FR-002**: The system MUST reject any registration attempt whose
  bootstrap token does not exist, has been used, has expired, is
  scoped to a different client than the requesting installation
  claims, or whose request-claimed `clientApp` has no registered
  descriptor policy. Failure responses MUST use the same
  `{ "error": "<message>" }` envelope shape across all status codes,
  and the response MUST NOT reveal which specific cause was violated
  **for the three scope-related failure modes** (token unknown, token
  scope-mismatch, unknown `clientApp`) — all three MUST collapse to
  `401 Unauthorized` so that an attacker cannot use the response to
  enumerate which apps a token was scoped to. Other failure modes
  (`token already used`, `token expired`, `token revoked`,
  `descriptor malformed`, descriptor field required by the active
  `DescriptorPolicy` is missing, `installGuid` parses to `Guid.Empty`)
  MUST use their RFC-meaningful status (`409`, `410`, `423`, `400`,
  `400`, `400` respectively), because they reveal no information
  about token scope and the legitimate consumer benefits from
  actionable diagnostics. See the 2026-05-18 clarification session
  for the threat-model rationale (43-char base64url token entropy
  ≈ 2^258 makes random-guess enumeration infeasible regardless).
- **FR-003**: On a successful registration, the system MUST create a
  new per-installation identity record, mark the bootstrap token
  used, and issue a fresh long-lived API credential whose plaintext
  value is returned to the client exactly once.
- **FR-004**: The system MUST NOT store the plaintext value of issued
  API credentials at rest. Only a non-reversible representation
  (suitable for verifying a candidate plaintext on subsequent calls)
  is stored.
- **FR-005**: Per-installation API credentials MUST be authenticated
  by the existing API authentication boundary. Per the constitution's
  Security & Auditability section, the existing legacy keys (the
  `ApiKeys` configuration section) MUST continue to authenticate as
  well — the two key sources operate as a union; this is **not** a
  deprecation of the legacy keys.
- **FR-006**: Each per-installation API credential MUST be revocable
  independently of every other credential. Revoking one MUST NOT
  affect the authentication of any other credential, whether from
  the same client or any other.
- **FR-007**: Bootstrap tokens MUST be single-use and time-bounded.
  The system default validity window is 30 days from the moment of
  minting. The admin MAY override the default per-token at mint time
  within `[1 hour, 90 days]`; values outside that interval MUST be
  rejected at mint time.
- **FR-008**: Bootstrap tokens MUST be client-scoped — a token
  issued for client A MUST NOT successfully register an installation
  that claims client B.
- **FR-009**: An admin user MUST be able to mint a bootstrap token
  for a given client app through some administrative interface, and
  retrieve its plaintext value exactly once at mint time so that it
  can be transmitted to the intended OS user out-of-band. Each token
  is intended for one (OS user, machine) installation; the
  (user, machine) binding is enforced operationally (single-use +
  admin distribution discipline), not encoded in the token. (Whether
  the admin interface is HTTP under `/api/admin/*`, a dotnet CLI
  tool, or both is a planning concern, not a spec concern.)
- **FR-010**: An admin user MUST be able to list the per-installation
  identities currently registered, including each one's client app,
  OS user identifier, machine identifier, install GUID, registration
  timestamp, and current status (active or revoked, with revocation
  timestamp where applicable).
- **FR-011**: An admin user MUST be able to revoke any specific
  per-installation identity. Revocation MUST take effect within
  5 seconds (matching SC-004) and MUST NOT propagate to any other
  identity. An in-process credential-validity cache with a TTL of
  up to 5 seconds, or with explicit invalidation on revoke, is
  acceptable on the authentication hot path.
- **FR-012**: The system MUST persist a registration audit record for
  every registration attempt, regardless of outcome (success or any
  failure mode). Each record MUST capture: timestamp, claimed client
  app (from the descriptor), claimed OS user identifier (from the
  descriptor), claimed machine identifier (from the descriptor),
  source IP address, full installation descriptor, outcome, and —
  on success — the resulting per-installation identity's identifier.
- **FR-013**: If the system cannot persist a registration audit
  record, the registration MUST fail; an API credential MUST NOT be
  issued in that case.
- **FR-014**: The plaintext value of a bootstrap token MUST NOT be
  retrievable through any administrative or query interface after
  the moment of minting; only its non-reversible representation,
  scope, expiry, and used/unused state are queryable.

### Key Entities *(include if feature involves data)*

- **Bootstrap Token** — a single-use, time-bounded, client-app-scoped
  capability that authorizes one and only one registration.
  Attributes: opaque token identifier, client app scope (free-text
  identifier matching the existing `ApiKeys` config keys), expiry
  timestamp, used/unused state, mint timestamp, and non-reversible
  representation of the secret value. Lifecycle:
  `Issued → Used | Expired | Revoked`. The state transitions out of
  `Issued` are irreversible. Note: the (OS user, machine) pair the
  token is intended for is **not** encoded in the token; that
  binding is enforced operationally (single-use + admin distribution
  discipline).
- **Installation** — a per-installation identity created by a
  successful registration. Identity granularity: one Installation
  per (client app, OS user, machine) tuple — multiple OS users on
  the same machine produce multiple Installations, one per user;
  the same OS user on multiple machines also produces multiple
  Installations. Attributes: opaque installation identifier, client
  app (inherited from the consuming bootstrap token's scope), OS
  user identifier (from the descriptor; server-opaque), machine
  identifier (from the descriptor; server-opaque), install GUID (from
  the descriptor), optional app version (from the descriptor; null if
  omitted), full installation descriptor as submitted, registration
  timestamp, current status (active / revoked), and revocation
  timestamp where applicable.
- **Installation API Credential** — the long-lived authentication
  secret bound to one Installation. Attributes: opaque credential
  identifier, owning Installation reference, mint timestamp, status
  (active / revoked), and non-reversible representation of the
  secret. Plaintext returned to the client exactly once at issuance
  and stored client-side under the OS's per-user secret store
  (DPAPI `CurrentUser` on Windows, equivalent elsewhere) so only
  the registering OS user can decrypt it. Lifecycle:
  `Active → Revoked`.
- **Registration Event** — the audit-trail record of a registration
  attempt (success or failure). Attributes: timestamp, claimed
  client app, claimed OS user identifier, claimed machine
  identifier, claimed app version (nullable), source IP, full
  installation descriptor as submitted, outcome (success or which
  failure category — recorded server-side; never disclosed to the
  client), and on success the resulting Installation identifier.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A client installation completes its first-launch
  registration end-to-end (token in, API credential out, descriptor
  recorded, audit row written) in under 5 seconds under normal
  network conditions.
- **SC-002**: Across the three scope-related failure modes (token
  unknown, token scope-mismatched, request's `clientApp` unknown to
  the descriptor-policy registry), the response is byte-identical —
  the same `401 Unauthorized` status with the same
  `{ "error": "registration failed" }` body — so an attacker cannot
  use response inspection to enumerate which apps a token was scoped
  to. Other failure modes (already-used, expired, revoked, descriptor
  malformed, missing-required-per-policy, `installGuid` =
  `Guid.Empty`) are intentionally distinguishable by status code (409
  / 410 / 423 / 400 / 400 / 400 respectively), because they reveal no
  token-scope information and the legitimate consumer benefits from
  actionable diagnostics.
- **SC-003**: A bootstrap token successfully consumed once cannot be
  consumed again under any subsequent attempt, including
  concurrent attempts on the original mint — exactly one
  installation identity is created per token.
- **SC-004**: Revoking an installation's API credential causes
  authenticated calls using that credential to start failing within
  5 seconds of the revocation action; other installations'
  credentials remain unaffected.
- **SC-005**: 100% of registration attempts (success or failure)
  appear in the audit log within 2 seconds of the attempt; no
  registration succeeds without an audit row written.
- **SC-006**: A leaked bootstrap token (e.g. extracted from a client
  installer binary) is unusable for any installation it was not
  minted for, even if the attacker also captures a separate valid
  descriptor — client-scoping enforces this independent of descriptor
  contents.
- **SC-007**: The plaintext value of any issued API credential
  appears at most once in any system-internal log, response, or
  storage location — at issuance time, in the registration response
  to the originating client. After that one moment, only the non-
  reversible representation exists anywhere.

## Assumptions

- The existing `ApiKeyMiddleware` and the `ApiKeys` configuration
  section continue to function for trusted internal-service keys.
  This feature **adds** a per-installation credential source that
  the same middleware also accepts; it does not remove the legacy
  source (per the constitution's union decision).
- Client applications run on Windows and have access to DPAPI
  `CurrentUser` (or an equivalent per-user secret store on other
  platforms) for at-rest encryption of the issued API credential.
  The credential is bound to the OS user account that consumed the
  bootstrap token; another OS user on the same machine cannot
  decrypt it. The server makes no assumptions about the client
  platform beyond this.
- The system is single-tenant — one STEM API server fronting one
  set of clients. Multi-region or multi-cluster deployments are
  out of scope.
- Existing authentication, error-handling middleware, JSON
  serialization conventions, and the audit-log foundation are
  preserved (the existing `IAuditService` may be reused or
  paralleled by a registration-specific audit table — that
  decision is in the planning phase, not the spec).
- The system clock on the server is authoritative for token expiry.
  Acceptable clock drift on the server is bounded by the existing
  infrastructure's NTP guarantees.
- Out-of-scope per issue #1 itself, and reaffirmed here:
  - mTLS or OAuth migration. The credential model remains
    API-key-shaped.
  - Client self-service token minting. Bootstrap minting stays
    administrator-only.
  - A hardware-bound element of the installation descriptor
    (machine GUID binding). Noted in issue #1 as a related design
    question for a follow-up; not part of this feature.
