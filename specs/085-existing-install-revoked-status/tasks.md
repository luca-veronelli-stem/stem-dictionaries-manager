# Tasks — #85 ExistingInstallationRevoked status-code mapping

Single-slice bug fix landing inside the existing
`001-bootstrap-registration` feature; no new feature surface. The SDD
artifacts are updated in place (`contracts/register.md`, the
`spec.md` 2026-06-04 clarification, `data-model.md`); the behavior
change and its RED->GREEN proof land as one vertical worker commit.

Context: `RegistrationOutcome.ExistingInstallationRevoked` (added in
#71) was not enumerated in `StatusFor`, so it fell through the
`_ => 401` default — contradicting the 2026-05-18 FR-002 narrowing.
It fires only after token + client-scope validation, so it leaks no
token-scope information and must not be 401. Target: `423 Locked`
(consistent with `TokenRevoked`). See issue #85.

## Tasks

- [X] T001 [docs] Align the SDD artifacts to the `423 Locked` mapping
  for `ExistingInstallationRevoked`: the status-map row + the
  re-registration prose in `contracts/register.md`, the 2026-06-04
  clarification session in `spec.md`, and the outcome-table row +
  Outcome-vs-wire-status note in `data-model.md`. Orchestrator-owned;
  lands in a `docs:` commit.
- [X] T002 [impl] Enumerate `RegistrationOutcome.ExistingInstallationRevoked`
  in `StatusFor` (`src/API/Endpoints/Auth/RegistrationEndpoints.cs`)
  as `423 Locked`; drop the "identical to `ClientScopeMismatch`" claim
  from the enum XML doc (`src/Core/Enums/Auth/RegistrationOutcome.cs`);
  add a RED-first integration test in
  `tests/Tests/Integration/API/Auth/RegisterEndpointTests.cs` that
  seeds a Revoked Installation matching the descriptor's
  `(ClientApp, InstallGuid)` plus a fresh valid token, POSTs
  `/register`, and asserts `423` + the
  `{"error":"registration failed"}` body + an
  `ExistingInstallationRevoked` audit outcome. One vertical,
  bisect-safe commit; body carries `Tasks: T002`.
