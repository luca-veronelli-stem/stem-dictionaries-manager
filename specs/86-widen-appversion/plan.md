# Plan — #86 widen `AppVersion` / `ClaimedAppVersion` past `nvarchar(50)`

Lightweight plan (one bisect-safe slice). Spec lives in the issue; no
separate `spec.md`/`data-model.md`/`contracts/` for a single-column widen.

## Problem

`POST /register` returns HTTP 500 (`audit failure`) when
`Descriptor.AppVersion` exceeds 50 chars. NerdBank.GitVersioning emits a
prerelease informational version for PR/dev builds
(`0.0.0-pr107-<sha>+<sha>`, 50+ chars); SQL Server rejects it with
`String or binary data would be truncated` on `Installations.AppVersion`
(`nvarchar(50)`). The same trap exists on
`RegistrationEvents.ClaimedAppVersion` (audited on failure paths). The
bootstrap token is not consumed (transaction rolls back), so retry is
possible once the column is widened.

## Fix

One vertical slice:

1. `src/Infrastructure/AppDbContext.cs:300` — `AppVersion`
   `HasMaxLength(50)` -> `HasMaxLength(128)`.
2. `src/Infrastructure/AppDbContext.cs:342` — `ClaimedAppVersion`
   `HasMaxLength(50)` -> `HasMaxLength(128)`.
3. A new EF Core migration `WidenAppVersionColumns` altering both columns
   `nvarchar(50)` -> `nvarchar(128)`. Generated SQL-Server-shaped via the
   existing `DesignTimeDbContextFactory` (`UseSqlServer`), consistent with
   the existing migrations. Do **not** edit any existing migration.
4. A model-metadata regression test (see Proof strategy).

128 is the agreed cap: comfortably fits any NBGV shape (incl. longer
prerelease tags like `-alpha.123+sha`) without going `nvarchar(max)`, and
stays index-friendly even though the column is not indexed today.

## Proof strategy (the load-bearing part)

**The /register integration tests run against SQLite in-memory**
(`RegisterApiFactory`: `Data Source=:memory:` + `EnsureCreated()`).
SQLite type affinity **ignores** `nvarchar(50)` length — a 60-char
`AppVersion` is stored untruncated and never throws. So the issue's AC
"integration test exercises `/register` with a 60+ char `AppVersion` and
expects 200" is **vacuous on this harness**: it passes GREEN on `main`
*without* the fix. The truncation 500 only happens on SQL Server (prod),
which CI never runs. Shipping that test would be a false-green regression
guard, so we **drop it** (AC deviation, recorded here and in the PR body).

**In-CI RED -> GREEN (the bisect-safe behavior proof):** a
model-metadata test in `tests/Tests/Unit/Infrastructure/` (peer of
`DependencyInjectionTests.cs`). It reads the EF model
(`IModel.FindEntityType(...).FindProperty(...).GetMaxLength()`), which
reflects `HasMaxLength`, **not** the database:

```csharp
var p = ctx.Model.FindEntityType(typeof(InstallationEntity))!
          .FindProperty(nameof(InstallationEntity.AppVersion))!;
Assert.Equal(128, p.GetMaxLength());
// and the same for RegistrationEventEntity.ClaimedAppVersion
```

Fails RED today (== 50), passes GREEN after the `AppDbContext` edit
(== 128). Deterministic, no SQL Server needed.

**Live-boundary diagnostic** ("what boundary does this exercise that the
unit suite cannot?"): the real prod schema on SQL Server. CI cannot reach
it (no SQL Server; SQLite `EnsureCreated` bypasses migrations entirely —
see #88). The migration's real schema effect is therefore an
**operator / validation tail**, not a CI test:

- `dotnet ef migrations script --idempotent -p src/Infrastructure -s src/Infrastructure`
  — confirm it applies cleanly and is a no-op on re-run; capture the
  output as the artifact.
- After deploy, re-run the previously-failing prod registration
  (`installGuid=e63144d7-3f1b-4d81-ba4b-6f7ec301f7f7`, sourceIp
  `77.43.96.108`, original failure 2026-05-22) end-to-end; confirm 200.

This is the resolve-ticket **Validation Gate**: CI-green MERGES the code
slice (code-complete); the feature is `ValidationPending` until the prod
re-run passes. Owner of both operator steps: **Luca**. The code slice is
**not** blocked on the prod re-run.

## Slice shape (bisect-safe)

ONE vertical commit: migration + the two `AppDbContext` edits + the
model-metadata test. Builds green; the model-metadata test passes (it
guards the config the migration mirrors). The migration files are
regenerated UTF-8-with-BOM + CRLF by `dotnet ef`; they are normalized
in place (strip BOM, CRLF->LF) before commit so the CHARSET/ENDOFLINE
analyzers (`TreatWarningsAsErrors`) stay green.

Commit subject: `fix(infra): widen AppVersion columns to nvarchar(128)`.

## Out of scope (separate tickets)

- **#88** — migrations are SqlServer-only / SQLite `EnsureCreated`
  divergence. This slice just adds a widening migration consistent with
  the existing SqlServer migrations; it does not make migrations
  SQLite-compatible.
- **#87** — README `EnsureCreated` drift.
