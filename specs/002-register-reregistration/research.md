# Phase 0 — Research

**Feature**: Atomic re-registration on existing installation
**Branch**: `fix/71-register-reregistration`
**Date**: 2026-05-19

Spec 002 is mostly mechanical extension of the spec-001 plumbing.
Only one design question warrants a research-level decision; the rest
of the plan reuses the patterns already settled in spec 001's
`research.md`.

## R1 — Enforcing "at most one Active credential per Installation"

### Decision

Enforce the invariant at the database level via a **filtered unique
index** on `InstallationApiCredentials.InstallationId WHERE Status =
Active`. Apply on both SQLite (test/dev) and SQL Server (prod) via
EF Core 10's `HasFilter("...")` modeling.

### Rationale

Spec 002 turns `InstallationApiCredential` from 1:1-with-Installation
into 1:N-with-at-most-one-Active. Without an enforcement
mechanism, the invariant is wishful thinking — a concurrent
re-registration race (two valid bootstrap tokens against the same
`InstallGuid` arriving inside the same window) would leave two rows
with `Status = Active` for the same installation, silently breaking
both the audit log's exactly-one-active semantics and the
`InstallationCredentialValidator`'s cache assumptions.

Database-level enforcement has the strongest properties:

- **Outlives refactors.** If the service layer is rewritten (or
  re-implemented in F# during Migration Phase 4), the invariant
  remains protected. Application-layer enforcement is brittle in this
  exact dimension.
- **Cheap.** One index, ~no measurable write penalty for the access
  patterns at play (insert + occasional update).
- **Cross-provider.** EF Core 10 generates `CREATE UNIQUE INDEX ...
  WHERE [Status] = 0` (or equivalent) for both SQL Server and SQLite.
  Verified that the SQLite test path picks up the filter correctly.

### Alternatives considered

- **Serializable transaction isolation** during `CommitReRegistration`.
  Rejected: surfaces as `SqlException` / `SqliteException` on the
  loser, costs throughput, and the SQLite-in-memory test harness has
  notoriously different isolation semantics than SQL Server, so we'd
  test one thing and ship another. Filtered index has identical
  semantics across providers.
- **Application-level re-check inside the transaction.** Rejected:
  TOCTOU is exactly the failure mode the invariant exists to prevent;
  application checks can be regressed in a future refactor without
  catching a single broken test.
- **No enforcement, accept rare divergence.** Rejected: silently
  diverging from the documented invariant is the worst outcome —
  operators querying the audit log would see "one active credential"
  per installation as a property that "usually" holds, which is the
  category of bug spec 001's data-model deliberately wrote out.

### Migration shape

```powershell
dotnet ef migrations add MultiActiveCredentialPerInstallationGuard `
    -p src/Infrastructure -s src/API
```

The migration adds the filtered unique index. No data migration is
needed: existing rows are all 1:1 (from spec 001), so no row violates
the new constraint.

### Test surface

A new RED-first integration test under
`tests/Tests/Integration/Infrastructure/AppDbContextTests.cs` (or
folded into the existing infra test surface): seed an Installation +
one Active credential, attempt to insert a second Active credential,
expect a unique-constraint violation. After GREEN, this same test
documents the invariant for future maintainers.

## R2 — Revocation primitive shape (spec-review Finding 1)

### Decision

Add `RevokeActiveAsync(int installationId, DateTime revokedAt,
CancellationToken)` to `IInstallationCredentialService`. Returns
`int` (count of rows flipped). Does NOT mutate the Installation
row itself — that responsibility belongs to a future, separate
`RevokeInstallationAsync` owned by issue #68's admin endpoint.

### Rationale

Issue #71 needs to revoke just the credentials (the Installation
remains Active — it gets a new credential). Issue #68 needs to
revoke the Installation row AND all its credentials. Those are two
different operations with different audit semantics. Sharing one
"revoke" method would force one of the call sites to either over-
mutate or under-audit.

The credential-only primitive is the natural shared building block:
the future admin `RevokeInstallationAsync` will internally flip
`Installation.Status` and then delegate to `RevokeActiveAsync` for
the credentials. No duplication, clean responsibility split. This
satisfies SC-006 of the spec.

### Signature pinning against #68

`specs/001-bootstrap-registration/contracts/admin-installations.md`
documents the admin revoke endpoint's side effects as:

> - `Installation.Status` transitions `Active → Revoked`,
>   `Installation.RevokedAt = DateTime.UtcNow`.
> - `InstallationApiCredential.Status` transitions `Active → Revoked`
>   for the owning installation, with the same `RevokedAt`.
> - `AuditEntry` row inserted via `IAuditService.LogUpdateAsync`...
> - The validation cache is invalidated...

`RevokeActiveAsync(installationId, revokedAt, ct)` is sufficient for
the second bullet. The first and third bullets are admin-specific.
The fourth (cache invalidation) lives at the `InstallationCredential
Validator` surface — already public via `Invalidate(plaintext)`,
which #68 will call for each revoked credential's plaintext... wait,
the admin endpoint doesn't have the plaintext, only the hash. The
cache key is hash-based, so admin revocation will need a different
invalidation path (or rely on the 5 s TTL). That's #68's design
concern, not 002's.

### Alternatives considered

- **One unified `RevokeInstallationAsync` now.** Rejected: 002 doesn't
  need or want to mutate the Installation row. Issuing a unified
  method would force the re-registration path to either skip the
  Installation flip (defeating the unified surface) or do it (wrong
  for re-registration). The two responsibilities are distinct.
- **Free function / static helper.** Rejected: revocation needs to
  drive the `IInstallationApiCredentialRepository` and is a stateful
  service-layer operation (mirrors `IssueAsync`'s shape on the same
  interface). Consistency wins.

## R3 — Audit outcome for revoked-installation rejection (spec-review Finding 2)

> **Update — #85 (0.9.1):** the decision below (conflated **401**) was
> later reversed. The "New wire status (`423 Locked`)" alternative this
> section rejected on FR-002 grounds became viable once the 2026-05-18
> FR-002 clarification narrowed the conflation to pre-token/scope-validation
> outcomes only. `ExistingInstallationRevoked` fires *after* token and scope
> validation, so it leaks no scope info, and #85 adopted the **`423 Locked`**
> mapping. The #71 reasoning below is preserved as the historical record.

### Decision

Add a new server-only `RegistrationOutcome.ExistingInstallationRevoked`.
On the wire, it maps to the existing conflated 401 + standard failure
body — identical bytes to `ClientScopeMismatch` / `TokenInvalid`.
Internally, the audit row's `Outcome` column carries the dedicated
value, so an admin querying "did anyone try to re-register a revoked
installation?" can filter by outcome alone.

### Rationale

Two requirements pull against each other here:

- **Spec FR-003** says the request MUST be rejected through the
  existing conflated 401 path (no info leak to the client).
- **Operations** wants to know which rejections were
  revoked-installation re-attempts (a signal that an admin's revoke
  decision is being challenged out-of-band, possibly worth
  follow-up).

A server-only outcome value resolves both. Spec 001 uses exactly the
same pattern: the `RegistrationOutcome` enum is recorded faithfully
in the audit table, while the wire response collapses
`TokenInvalid` / `ClientScopeMismatch` / policy-lookup-miss into a
single 401 body.

Same approach for the happy path: `ReRegistrationSuccess` is the
audit-row outcome, but the wire response is identical (200 + new
credential body) to a first-time `Success`.

### Alternatives considered

- **Reuse `ClientScopeMismatch`.** Rejected: loses audit fidelity for
  the exact operational question the admin would have ("did a revoked
  installation try to come back?"). The audit log is the only place
  the admin can answer that.
- **New wire status (e.g. `423 Locked` for the revoked case).**
  Rejected: breaks the FR-002 conflation invariant; tells the client
  that the installation exists in a particular state, which is
  exactly the kind of token-validity oracle spec 001 was designed to
  hide.

## Open follow-ups

None. All spec-002 design questions are pinned. Spec 001's
TODO(LEAN_WORKSPACE) note is inherited — the re-registration state
transition is a candidate for the future Lean preservation track but
is out of scope for this PR (the Lean workspace doesn't exist yet).
