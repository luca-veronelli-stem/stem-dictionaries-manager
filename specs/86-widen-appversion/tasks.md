# Tasks — #86 widen `AppVersion` / `ClaimedAppVersion` to `nvarchar(128)`

One bisect-safe implementation slice (single worker, single commit).

- [X] **T001** — `fix(infra): widen AppVersion columns to nvarchar(128)`

  Vertical slice closing all code-side acceptance criteria.

  **RED (write first, observe failing):** a model-metadata test in
  `tests/Tests/Unit/Infrastructure/` (e.g. `AppVersionColumnLengthTests`)
  asserting the EF model `GetMaxLength()` is **128** for
  `InstallationEntity.AppVersion` and
  `RegistrationEventEntity.ClaimedAppVersion`. Fails RED today (== 50).

  **GREEN:**
  - `src/Infrastructure/AppDbContext.cs:300` `HasMaxLength(50)` -> `(128)`.
  - `src/Infrastructure/AppDbContext.cs:342` `HasMaxLength(50)` -> `(128)`.
  - New EF migration `WidenAppVersionColumns` altering both columns
    `nvarchar(50)` -> `nvarchar(128)` (SQL-Server-shaped; do not edit
    existing migrations). Normalize the regenerated migration files
    (strip BOM, CRLF->LF) before commit.

  **Gate:** `./gate.ps1` green (build + whitespace format + full test
  suite incl. the new model-metadata test).

  Files owned: `src/Infrastructure/AppDbContext.cs`,
  `src/Infrastructure/Migrations/**`,
  `tests/Tests/Unit/Infrastructure/**`.

  AC deviation: the issue's "/register expects 200" integration test is
  **dropped** (vacuous on SQLite — see `plan.md`).

## Orchestrator-owned (not worker tasks)

- CHANGELOG entry for #86.
- PR body: completed scope + `ValidationPending` operator tail (idempotent
  script artifact + prod re-run, owner Luca) + the dropped-test deviation.
