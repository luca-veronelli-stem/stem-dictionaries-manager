# Feature Specification: Atomic re-registration on existing installation

**Feature Branch**: `fix/71-register-reregistration` (carried on the
issue PR; no separate feature branch — see `resolve-ticket` protocol).
**Created**: 2026-05-19
**Status**: Draft
**Input**: GitHub issue
[luca-veronelli-stem/stem-dictionaries-manager#71](https://github.com/luca-veronelli-stem/stem-dictionaries-manager/issues/71) —
*"fix(auth): /register 500s with swallowed SQL exception on duplicate
InstallGuid; implement atomic re-registration (option B)"*.

## Background

Spec 001 (`specs/001-bootstrap-registration/`) shipped the first-time
registration flow. The first real consumer
(`luca-veronelli-stem/button-panel-tester`) has now exercised an
operationally-expected recovery scenario that 001 did not specify:
a technician whose `credential.dpapi` is lost (machine reimage, profile
corruption, hardware swap with partial `%LOCALAPPDATA%` migration)
re-runs the registration dialog on the same machine — same
`InstallGuid`, fresh bootstrap token — and the API returns
`500 audit failure` with the actual cause silently dropped.

The defect has two faces:

1. **Behavioral gap**: 001 left "what happens when `/register` lands on
   an existing `Installations.InstallGuid`" unspecified. The current
   implementation only knows how to insert; the unique index throws,
   the catch swallows the exception, and the client sees a misleading
   500.
2. **Observability gap**: even before the behavior is fixed, the
   endpoint's broad catch in `RegistrationEndpoints.Register` discards
   the exception. Today the only way to recover the actual cause is to
   tail Entity Framework's debug logs.

This spec resolves both. See `specs/001-bootstrap-registration/spec.md`
FR-018 for the *"clients may lose credentials and recover"* design intent
that this spec finally operationalises.

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Technician recovers a lost installation credential (Priority: P1)

A STEM-app technician's installation credential is lost or unusable
(profile reimaged, DPAPI store wiped, machine cloned). The technician
asks the STEM admin for a fresh bootstrap token. The admin mints one
out-of-band (same `clientApp` scope) and hands it to the technician.
The technician launches the consumer app and pastes the new token into
its registration dialog. The same machine — identified by its
persistent `installGuid` sidecar — is now reachable to the API as an
**existing installation that needs a new credential**.

**Why this priority**: this is the primary failure mode that issue #71
was filed against and the operational recovery path called out by
FR-018 of spec 001. Without this, the documented "credentials may be
lost and the user can recover" property is on paper only; the supplier
must call STEM, STEM must manually run SQL, and the user is misled by
a "service is down" message.

**Independent Test**: end-to-end against a test API instance:
1. Successfully register an installation with token T1 → credential C1
   persisted.
2. Delete the client-side credential store but keep the `installGuid`
   sidecar.
3. Mint a fresh bootstrap token T2 (same `clientApp`).
4. Submit the registration dialog again. Observe: HTTP 200, new
   credential plaintext is returned, the prior credential is no longer
   accepted by `ApiKeyMiddleware` once the existing positive-cache TTL
   elapses, and the audit log shows a re-registration event distinct
   from a first-time `Success`.

**Acceptance Scenarios**:

1. **Given** an `Installation` row with `InstallGuid = G` and an
   `Active` `InstallationApiCredential`, **and** a fresh `Issued`
   `BootstrapToken` scoped to the same `ClientApp`, **when** the client
   submits `POST /register` with that token and a descriptor whose
   `installGuid = G`, **then** the response is 200 with a new
   credential plaintext, the prior credential's status becomes
   `Revoked` with `RevokedAt = now`, a new `Active` credential is
   persisted, the bootstrap token transitions to `Used`, and the audit
   log gains one row classified as a re-registration outcome.
2. **Given** the same precondition, **when** the request completes,
   **then** the prior credential's `SecretHash` value is still present
   in the database (preserved for forensics; only `Status` and
   `RevokedAt` change).
3. **Given** the same precondition, **when** the request completes,
   **then** the returned credential plaintext is not equal to the
   prior credential's plaintext.

### User Story 2 — Operator diagnoses an unexpected registration failure (Priority: P1)

An operator watching production application logs needs to see the
proximate cause of any `POST /register` exception in the
application-log layer (the same `ILogger<RegistrationEndpoints>` stream
as the rest of API logging). Today the broad catch in
`RegistrationEndpoints.Register` discards the exception object: only
EF's deep `Microsoft.EntityFrameworkCore.Update` channel records it,
and only at debug verbosity. When a future regression surfaces, the
operator should not need to enable EF verbose logging to find out what
threw.

**Why this priority**: even after US1 lands and removes the
duplicate-InstallGuid 500, the catch will still serve as the
last-resort handler for genuine infrastructure failures (DB down,
audit insert race-loss, etc.). Those failures must be diagnosable
from the application log alone. This is also independently shippable
ahead of US1 if needed.

**Independent Test**: trigger any uncaught failure inside
`RegistrationService.RegisterAsync` (e.g., by stubbing the audit
repository to throw on `AddAsync`). Observe: the response is still
`500 audit failure` (FR-013 unchanged from spec 001), but the
application log contains an error-level entry with the exception
attached, sourced from the registration endpoint logger category.

**Acceptance Scenarios**:

1. **Given** the registration endpoint, **when** the service layer
   raises any exception before the success response is composed,
   **then** the application log records an error-level entry that
   carries the exception object before the 500 audit-failure response
   is written.
2. **Given** a successful registration or any
   classified-failure outcome (`TokenInvalid`, `TokenAlreadyUsed`,
   etc.), **when** the request completes, **then** no new error-level
   log entry is emitted by the endpoint (the catch is a last-resort
   handler, not a tee on every request).

### Edge Cases

- **Cross-app `InstallGuid` reuse**: the matched `Installation` row's
  `ClientApp` does not equal the request's `ClientApp`. The request
  MUST be rejected through the existing conflated 401 path; no row in
  the `Installations` or `InstallationApiCredentials` tables MUST be
  mutated. This preserves the FR-002 invariant that scope-related
  outcomes never leak which app a token or `InstallGuid` belongs to.
- **Revoked installation**: the matched `Installation` row's own
  `Status` is `Revoked`. The request MUST be rejected with `423 Locked`
  (`ExistingInstallationRevoked`; since #85 — distinguishable per the
  narrowed FR-002, as it fires only after token + scope validation).
  Re-registration MUST NOT auto-unrevoke
  an installation — operators revoke an installation deliberately, and
  a fresh `Issued` bootstrap token alone is not enough to clear that
  state. To recover, the operator must explicitly mint a flow that
  un-revokes (separate feature; out of scope here).
- **No existing installation**: the request's `InstallGuid` is not yet
  in the table. Behavior is unchanged from spec 001 — the first-time
  registration path runs and inserts a new `Installation` + credential.
- **Bootstrap token already `Used` or `Revoked`**: the existing
  classification path handles these before re-registration is even
  considered. Re-registration only activates after the outcome has
  classified as `Success`.
- **Concurrent re-registration**: two requests hit `/register`
  simultaneously, both reach the re-registration branch for the same
  `Installation`. One wins the credential insert; the loser must
  cleanly land on the existing race-loser audit path (either
  `TokenAlreadyUsed` if the token races first, or a defensively-
  reachable race outcome on the credential insert). Either way, the
  loser MUST NOT surface a 500.
- **Existing credential already revoked**: only `Active` credentials
  are revoked at re-registration time. Pre-existing `Revoked` rows
  remain `Revoked`; their `RevokedAt` is not rewritten.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST detect, after outcome classification
  reaches `Success`, whether an `Installation` row already exists for
  the request's `InstallGuid`. When one exists, the system MUST route
  to the re-registration path described below instead of the
  first-time insert path.
- **FR-002**: The re-registration path MUST require the matched
  `Installation.ClientApp` to byte-equal the request's `ClientApp`.
  A mismatch MUST route to the existing conflated 401
  (`ClientScopeMismatch`) outcome and MUST NOT mutate any row outside
  the `RegistrationEvents` audit table.
- **FR-003**: The re-registration path MUST require the matched
  `Installation.Status` to be `Active`. A `Revoked` installation MUST
  cause the request to be rejected with `423 Locked`
  (`ExistingInstallationRevoked`; distinguishable since #85, as it fires
  only after token + scope validation); the installation MUST NOT be
  auto-unrevoked.
- **FR-004**: A successful re-registration MUST set every `Active`
  `InstallationApiCredential` row for the matched installation to
  `Status = Revoked` with `RevokedAt = now`, and MUST insert a new
  `InstallationApiCredential` row with `Status = Active`,
  `IssuedAt = now`, and a freshly-generated `SecretHash`. The prior
  rows' `SecretHash` values MUST be preserved.
- **FR-005**: A successful re-registration MUST transition the
  bootstrap token from `Issued` to `Used` with the same single-use
  semantics as a first-time registration (FR-004 from spec 001).
- **FR-006**: A successful re-registration MUST write a single
  `RegistrationEvent` audit row whose outcome value is **distinct
  from** the first-time `Success` outcome — so an operator querying
  the audit log can identify re-registrations by outcome alone,
  without joining against the `Installations.RegisteredAt` field.
- **FR-007**: The full set of writes for one re-registration (revoke
  prior credentials + insert new credential + transition token +
  insert audit row) MUST commit as a single database transaction. A
  partial commit MUST NOT be observable to readers (invariant 3 from
  spec 001 — *audit-or-no-issue*).
- **FR-008**: The registration endpoint handler MUST log every
  exception that escapes the service layer at error level, with the
  exception object attached, **before** writing the
  `500 audit-failure` response. No exception MUST be silently
  swallowed.
- **FR-009**: The `POST /register` contract document MUST be updated
  to describe the re-registration path (matching conditions, side
  effects, response shape) explicitly.
- **FR-010**: The data-model document MUST be updated to record that
  the `InstallationApiCredentials` table holds **multiple rows per
  Installation** over the installation's lifetime — historically at
  most one with `Status = Active`, zero-or-more with `Status =
  Revoked`.
- **FR-011**: The capability to revoke all active credentials for a
  given installation MUST be exposed as a reusable service-layer
  operation, suitable for invocation from both the re-registration
  flow (this spec) and a future admin-driven revoke flow
  (issue #68). The specific shape of the operation (interface,
  parameter list, error model) is a planning concern; the
  reusability is a spec-level invariant because it gates whether
  #68 can be built without duplicating logic.

### Key Entities

- **`Installation`**: persistent row keyed by `Id`, identified by the
  client-supplied `InstallGuid`. One row represents one logical
  "machine identity" within a `ClientApp` over time. Through this
  spec, an `Installation` becomes a long-lived entity that may carry a
  succession of credentials.
- **`InstallationApiCredential`**: now explicitly **multi-row-per-
  Installation**. At most one row per `(InstallationId)` has
  `Status = Active` at any instant; historical rows linger with
  `Status = Revoked` for forensic value. Spec 001's
  `data-model.md` implicitly modeled this as one-to-one; this spec
  formalises the one-to-many.
- **`RegistrationEvent`**: gains one new outcome category that flags
  a re-registration success distinctly from a first-time `Success`,
  so the audit log remains queryable by outcome alone.
- **`BootstrapToken`**: unchanged; its `Issued → Used` transition is
  reused verbatim by the re-registration path.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A technician whose installation credential is lost can
  successfully re-register on the same machine using a fresh bootstrap
  token, with no manual operator intervention (no SQL, no admin
  pre-revoke), and without any user-facing "service unavailable"
  message. Verified end-to-end against a test API instance for the
  scenario in US1.
- **SC-002**: After a re-registration completes, the prior
  credential's plaintext is rejected by `ApiKeyMiddleware` within the
  existing 5-second positive-cache TTL from spec 001 (FR-005). No
  additional cache-invalidation hook is required; if one becomes
  required, the design must justify it.
- **SC-003**: Operators can identify every re-registration event in
  the audit log without computing joins or correlating timestamps —
  the `RegistrationEvent.Outcome` value alone is sufficient. Verified
  by querying the audit table for the dedicated outcome value over
  the test scenarios.
- **SC-004**: For any 500 response emitted by `POST /register`, the
  application log layer contains the proximate exception at error
  level. Verified by injecting a service-layer fault under integration
  test and inspecting the captured log output.
- **SC-005**: The duplicate-`InstallGuid` 500 reproduced in issue #71
  no longer occurs on the happy path described in US1. The
  unique-index path remains reachable only under genuine concurrent-
  write races, which fall through to the FR-008 logged 500.
- **SC-006**: The credential-revocation capability introduced by this
  spec is invoked from at least one site today (the re-registration
  flow) and is callable from a second site (an admin endpoint) in the
  future without changing its surface — verified at planning time by
  inspecting the signature against the anticipated #68 call site.

## Assumptions

- The existing 5-second positive-cache TTL on
  `InstallationCredentialValidator` is sufficient for revocation
  propagation. This was the SC-004 target of spec 001 and is
  inherited unchanged.
- A `Revoked` installation (`Installation.Status = Revoked`) is an
  operator-deliberate state that re-registration MUST NOT clear
  automatically. The recovery path for a revoked installation is a
  separate, deliberate admin flow (out of scope).
- Cross-app `InstallGuid` reuse is operationally rare and is treated
  as a privacy event, not a legitimate scenario — the conflated 401
  path is acceptable for it.
- `ITokenGenerator.GenerateApiCredential()` is collision-free in
  practice (random 256-bit base64url), so the new credential's
  plaintext can be assumed unequal to any prior plaintext for the
  same installation without explicit anti-collision logic.
- The audit row written by the re-registration path is the only audit
  surface mutated; no new logging tables, columns, or telemetry are
  added beyond the existing `RegistrationEvent` shape.

## Out of Scope

- The admin list and admin revoke endpoints from issue #68. This spec
  introduces the **service-layer revocation primitive** that #68 will
  later wrap; the HTTP surface, auth wiring, list filters, and
  idempotency contract for #68 remain its own work.
- Per-supplier rate limiting on `POST /register` (a real concern for
  a reimage-in-a-loop scenario, but separate).
- Bulk re-registration or "rotate every installation for clientApp X"
  flows.
- Any client-side change in consumer apps. `button-panel-tester`
  already persists the `install.guid` sidecar and treats every 200
  response as "credential persisted"; option B is transparent to it.
- Auto-unrevoke of a `Revoked` installation. A separate admin flow
  (deliberately opt-in) covers that case.
- Bootstrap-token admin list/revoke endpoints. The `BootstrapToken`
  state model already supports them; no admin surface is in scope
  here.
