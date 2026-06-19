# Changelog

All notable changes to DictionariesManager follow [Semantic Versioning](https://semver.org/) and are recorded here in [Keep a Changelog](https://keepachangelog.com/) format.

## [Unreleased]

### Added

- **Core**: `User`, `Dictionary`, and `Command` now expose focused
  `Update*` mutators (`User.UpdateUsername` / `UpdateDisplayName`,
  `Dictionary.UpdateName` / `UpdateDescription`, `Command.UpdateName`)
  that centralize the constructors' validation, so callers no longer
  rebuild a whole instance to change a single property. `Command` is kept
  minimal pending the Services-layer changes. Closes #5.
- **Infrastructure**: the production protocol command seed now includes the
  `0x802C` "Start Calibrazione IMU risposta" reply (1-byte `Esito`, bit 0:
  `1` = accepted / machine stale, `0` = rejected / machine in motion). This
  reply inverts the dictionary's usual `0 = ok, !0 = codice errore`
  convention, so the seed entry carries a clarifying comment. The companion
  `0x802B` "Unlock Cambio Firmware Schede H7 risposta" reply remains absent
  from the seed -- it was patched into production via the admin GUI and never
  backported, and re-seeding it is out of scope here. This fixes fresh installs
  only; already-provisioned databases need `0x802C` added the same way via the
  admin GUI. Closes #79.

### Changed

- **Build**: removed the redundant `<Nullable>` and `<ImplicitUsings>`
  properties from the `Core`, `Infrastructure`, `Services`, and `API`
  project files. Both are already enabled in root `Directory.Build.props`,
  so the per-project copies were dead weight that only risked drift.
  Closes #2.
- **Infrastructure**: the default SQLite database path now follows APP_DATA
  v1.9.0 — `%LocalAppData%\Stem\DictionariesManager\db\` (Local not Roaming,
  PascalCase `Stem`, `db\` sub-folder) instead of
  `%AppData%\STEM\DictionariesManager\`. A transient one-shot migration moves
  an existing database from the legacy path on first launch, guarded by a
  `.appdata-version` marker. Closes #90.
- **Infrastructure**: `CommandEntity.Parameters` is now a typed `List<string>`
  persisted through an EF Core value conversion, replacing the raw
  `ParametersJson` string and the manual (de)serialization scattered across
  `CommandMapper` and the seeder. The column name and shape are unchanged, so
  no schema migration is required. Closes #7.

### Fixed

- **Core**: `BitInterpretation` now rejects a non-positive `VariableId`
  with `ArgumentOutOfRangeException`, matching the existing
  `WordIndex` / `BitIndex` fail-fast guards. Closes #6.
- **Core**: `Dictionary.RemoveVariable` now throws
  `InvalidOperationException` when the variable is absent (and
  `ArgumentNullException` on null) instead of silently discarding the
  result of `List<T>.Remove`, restoring symmetry with `AddVariable`.
  Closes #4.
- **API**: the `https` launch profile now binds `https://localhost:7065`
  (previously `7290`), matching the consumer consensus port that
  `button-panel-tester` and other clients expect. A fresh
  `dotnet run --project src/API` is now reachable from those consumers
  without an `ASPNETCORE_URLS` override. Closes #89.
- **CI**: every `dotnet restore` failed with `NU1903` (NuGetAudit +
  `TreatWarningsAsErrors`) because `SQLitePCLRaw.lib.e_sqlite3` 2.1.11,
  pulled transitively by `Microsoft.EntityFrameworkCore.Sqlite` 10.0.8,
  carries CVE-2025-6965 (advisory GHSA-2m69-gcr7-jv3q) with no patched
  2.1.x release. This blocked `main`'s scheduled CI and every open PR.
  Resolved with a documented `NuGetAuditSuppress` in
  `Directory.Packages.props`: real exposure is nil (embedded local SQLite,
  EF-generated queries, no untrusted SQL), and forward-pinning the chain
  to 3.x is not achievable from central package management without an
  out-of-scope bump of the `Microsoft.Extensions.*` family. Remove the
  suppression once EF Core's dependency chain moves onto a patched native
  lib. Closes #107.
- **Infrastructure**: `DictionaryRepository.GetByNameAsync` now normalizes its
  input and matches case-insensitively (mirroring
  `UserRepository.GetByUsernameAsync`), so `GetByNameAsync("OPTIMUS-XP")` finds
  a row stored as `optimus-xp`. Closes #8.
- **Infrastructure**: the `system-admin` user is now seeded via `HasData` in
  `OnModelCreating` instead of migration `InsertData`, so it is present on
  SQLite databases built with `EnsureCreated` (previously it was missing on
  SQLite dev/CI, silently breaking `AdminAuthenticationMiddleware` audit
  attribution for `/api/admin/*`). A no-op reconcile migration keeps the SQL
  Server `Migrate` path consistent (no double-insert). The
  "SQLite EnsureCreated / SQL Server Migrate, seed via HasData" policy is
  documented in `docs/Persistence.md`. Closes #88.
- **Infrastructure**: `DatabaseSeeder.SeedAsync` no longer short-circuits on a
  fresh database. Its "already seeded" guard checked `Users.AnyAsync()`, but the
  `system-admin` user added via `HasData` (#88) is now always present right after
  `EnsureCreated`, so the guard tripped on every fresh DB and seeded no users,
  devices, commands, or boards at all. The guard now excludes the `system-admin`
  seed user. Caught by the new `0x802C` seeder test. Regression from #88.

## [0.9.1] - 2026-06-04

Patch release: two production `/register` fixes shipped together — the
`nvarchar(50)` → `nvarchar(128)` `AppVersion` widening (#86) that resolves
the NBGV PR-build `500`, and the `ExistingInstallationRevoked → 423` status
correction (#85). The deploy workflow's `/api/version` smoke is also
hardened against cold-start flakiness (#80).

### Fixed

- **API**: `POST /register` no longer returns `500` (`audit failure`)
  when `Descriptor.AppVersion` exceeds 50 characters. NerdBank.GitVersioning
  emits a prerelease informational version for PR/dev builds
  (`0.0.0-prNNN-<sha>+<sha>`, 50+ chars) that overflowed
  `Installations.AppVersion` and `RegistrationEvents.ClaimedAppVersion`
  (`nvarchar(50)`), which SQL Server rejected with `String or binary data
  would be truncated`. The bootstrap token was not consumed (the
  transaction rolled back), so retry succeeds once the schema is widened.
  Both columns are widened to `nvarchar(128)` via a new
  `WidenAppVersionColumns` migration; a model-metadata regression test
  guards the EF model max length. The SQLite integration harness cannot
  reproduce the truncation (type affinity ignores `nvarchar` length), so
  the regression proof reads the EF model rather than exercising
  `/register`. Closes #86.
- **API**: `POST /register` now returns `423 Locked` (previously
  `401 Unauthorized`) when a fresh, valid bootstrap token is presented
  against an existing **revoked** installation
  (`RegistrationOutcome.ExistingInstallationRevoked`). The outcome
  fires only after the token's validity and client-app scope have been
  verified, so per the narrowed FR-002 it leaks no token-scope
  information and must not be conflated into the scope-related 401 -- a
  prior gap where it fell through the `StatusFor` default. A consumer
  can now distinguish "this installation was revoked by an admin --
  reinstall the app" from the misleading "token not accepted". Closes
  #85.
- **CI**: `.github/workflows/deploy-api.yml` post-deploy smoke now
  retries `/api/version` with the same 12-attempt × 10-second cadence
  already in place for `/health`. The v0.9.0 deploy succeeded but the
  workflow status flipped to `failure` because the very first hit to
  `/api/version` after the zip-deploy transiently 500'd (route binding
  / JIT / DI graph warm-up race that `/health` won); a manual hit one
  second later returned the expected `0.9.0+<sha>`. Single-shot smoke
  on `/api/version` was the bug, not the deploy. Closes #80.

## [0.9.0] - 2026-05-21

Admin management surface for per-installation API credentials (#68 / US3
of spec 001). Operators can now list every registered installation and
independently revoke any one with a single audited HTTP call, closing
FR-010 / FR-011 and the documented gap from #1 ("the data model
supports it but no endpoint exercises it"). A model-wide DateTime
UTC-pinning convention rides along and closes a subtle wire-format
bug where existing endpoints returned timestamps without the `Z`
suffix.

### Added

- **API**: `GET /api/admin/installations?clientApp=…&status=…` —
  list installations with the contract metadata fields per
  [`contracts/admin-installations.md`](specs/001-bootstrap-registration/contracts/admin-installations.md).
  Server-side filtering by exact `clientApp` and by
  `active | revoked | all`; an unrecognised `status` value yields a
  400 with a validation error. Behind `AdminAuthenticationMiddleware`
  (`AdminApiKeys` config gate). Closes FR-010.
- **API**: `POST /api/admin/installations/{id}/revoke` — idempotent,
  atomic revoke. First call flips `Installation.Status` and every
  owning `InstallationApiCredential.Status` from `Active` to
  `Revoked`, stamps `RevokedAt`, writes one
  `AuditEntityType.Installation` audit row via
  `IAuditService.LogUpdateAsync`, and invalidates the validator cache
  after commit. Second call on an already-revoked installation returns
  the original `revokedAt` with no mutation and no audit row. Unknown
  id returns `404 { "error": "installation not found" }`. Closes
  FR-011.
- **Services**: `IInstallationService` (new) carries the admin-facing
  `ListAsync` + `RevokeAsync` + `RevokeResult` discriminated union
  (`Success(revokedAt, wasFirstRevoke)` / `NotFound`). Single-
  responsibility split from `IInstallationCredentialService`, which
  stays focused on per-credential operations called by
  `RegistrationService` (`IssueAsync` + `RevokeActiveAsync`).
- **Services**: `IInstallationCredentialValidator.Invalidate(int installationId)`
  overload. The existing `Invalidate(string plaintext)` is unreachable
  from the admin revoke path (FR-014 plaintext-once), so the validator
  now keeps a same-TTL `icv-byinst:{id}` side-entry in `IMemoryCache`
  on every positive resolution and the admin revoke flow uses it to
  evict the cache immediately after the durable write commits — keeping
  SC-004 well under the 5 s ceiling (local smoke: ~20 ms post-warm
  rejection) instead of riding it.
- **API config**: `appsettings.json` ships a second
  `ButtonPanelTesterSeedRefresh` entry under `ApiKeys` so the
  `refresh-seed.ps1` dry-run flow has a non-rotating dev key (rotation
  of the main `ButtonPanelTester` key no longer breaks local
  refresh-seed checks).

### Fixed

- **Infrastructure**: `AppDbContext.ConfigureConventions` now applies a
  UTC-pinning `ValueConverter` to every `DateTime` / `DateTime?`
  column in the model. SQLite stores DateTime as TEXT without a `Kind`
  marker, and SQL Server's `datetime2` has the same gap, so EF Core
  returned `Kind = Unspecified` on read; downstream JSON serialization
  then emitted the value without a trailing `Z` and parsers treated it
  as local time. Symptoms before this fix: every `registeredAt`,
  `issuedAt`, `mintedAt`, `expiresAt`, `occurredAt` field in API
  responses came back without a `Z` suffix; the first revoke's
  `revokedAt` (in-memory) and the idempotent re-revoke's `revokedAt`
  (DB round-trip) serialized differently. Every domain DateTime in
  this repo is written as UTC, so re-stamping `DateTimeKind.Utc` on
  read is provider-agnostic and idempotent. Covered by
  `Tests/Integration/Infrastructure/DateTimeKindConventionTests.cs`
  across the four spec 001 entities.

### Changed

- **Docs**: `specs/001-bootstrap-registration/contracts/admin-installations.md`
  clarifies the BR-API-004 nulls-omitted convention. The example
  response no longer shows `"revokedAt": null` for active rows; the
  contract now explicitly states an absent `revokedAt` is semantically
  equivalent to `null`.
- **Repo hygiene**: `.gitignore` now excludes `WIP.md` and `.llm/` —
  the per-worktree scratch files the `worktrees` skill maintains. No
  effect on shipped code; prevents the files from polluting
  `git status` and reaching commits accidentally.

## [0.8.0] - 2026-05-19

Atomic re-registration on existing installation (#71). Closes the recovery gap exposed by v0.7.2's `500 audit failure` on the duplicate-`InstallGuid` path: technicians can now recover from a lost-credential scenario (machine reimage, profile corruption, hardware swap) by re-running the standard registration ceremony with a fresh bootstrap token — no admin pre-revoke required, no spurious "service unavailable" surfaced to the user. Includes the prerequisite data-model shift to multi-credential-per-installation enforced by a filtered unique active-only index, plus an endpoint-layer logging fix so future `/register` exceptions surface at the API layer instead of being swallowed.

### Added

- **Auth**: `POST /register` now atomically re-registers an existing
  `InstallGuid` when a fresh bootstrap token is presented (option B
  from #71). The prior credential is revoked, a new one is issued,
  the bootstrap token transitions `Issued → Used`, and an audit row
  with the dedicated server-only outcome `ReRegistrationSuccess` is
  written — all in one transaction. Lets technicians recover from a
  lost-credential scenario (machine reimage, profile corruption,
  hardware swap) without admin pre-revoke, without any user-facing
  "service unavailable" message, and without the `500` that v0.7.2
  surfaced on the duplicate-`InstallGuid` path. Closes #71.
- **Services**: `IInstallationCredentialService.RevokeActiveAsync` —
  service-layer primitive for revoking every `Active` credential on a
  given installation. Reusable by the future admin revoke endpoint
  (#68); the credential-only shape means the admin path will
  delegate here and own its own Installation-row flip.
- **Infrastructure**: filtered unique index
  `UX_InstallationApiCredentials_Active` on
  `InstallationApiCredentials.InstallationId WHERE Status = Active`,
  carried by migration `MultiActiveCredentialPerInstallationGuard`.
  Enforces the at-most-one-Active invariant (data-model § 6) at the
  DB level on both SQL Server (prod) and SQLite (dev/test).
- **Infrastructure**: `IInstallationRepository.FindByInstallGuidAsync`
  and `IInstallationApiCredentialRepository.ListActiveByInstallationIdAsync`
  for the re-registration lookup path.

### Fixed

- **API**: `POST /register` no longer silently swallows exceptions.
  The parameterless `catch` in `RegistrationEndpoints.Register`
  discarded the exception object, leaving operators dependent on EF
  verbose logging to find out what threw. Replaced with a typed
  `catch (Exception ex)` that logs at error level with the exception
  object attached (plus structured `sourceIp` / `clientApp` /
  `installGuid` fields) before returning the `500 audit failure`
  response. The response shape is unchanged. Closes Part 1 of #71.

### Changed

- **Data model**: `InstallationApiCredentials` now holds **multiple
  rows per installation** over an installation's lifetime — at most
  one with `Status = Active` at any instant, plus zero-or-more
  `Revoked` historical rows preserved for forensics. The EF mapping
  shifted from `HasOne.WithOne` to `HasMany.WithOne`; the implicit
  unique constraint on `InstallationId` is dropped by migration
  `MultiCredentialPerInstallationModel` and replaced by the filtered
  unique active-only index above.
- **Audit**: `RegistrationOutcome` gains two server-only values —
  `ReRegistrationSuccess` (re-registration happy path; wire response
  identical to `Success` → 200) and `ExistingInstallationRevoked`
  (re-registration rejected because the matched Installation is
  itself revoked; wire response identical to `ClientScopeMismatch` →
  conflated 401). Operators can now filter the audit log for
  re-registrations or revoked-installation re-attempts by outcome
  alone.

## [0.7.2] - 2026-05-19

Hotfix for v0.7.1's API deploy. The `Microsoft.EntityFrameworkCore.Design` reference was missing from `src/API`, so `dotnet ef migrations script --startup-project src/API` failed during the v0.7.1 deploy run before it could touch Azure SQL or App Service. Latent since `0.7.0` — never surfaced because EF tooling had only ever been invoked locally against `Infrastructure` as both project and startup project.

### Fixed

- **API**: `src/API/API.csproj` now references `Microsoft.EntityFrameworkCore.Design` with `<PrivateAssets>all</PrivateAssets>` + `<IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>` (the canonical EF design-time-only pattern, already used by `src/Infrastructure`). Design assemblies stay out of the published runtime and the API code's compile surface — they exist on disk under `bin/` only so `dotnet ef` can load them via reflection during the deploy workflow.

## [0.7.1] - 2026-05-19

Operations cycle — closes the gap between "API code on `main`" and "running production on Azure App Service" that v0.7.0's manual ship procedure exposed. Two bug fixes ride along: the `/register` outcome-classification fix from #58, and the `release.yml` path fix that left v0.5.0–v0.7.0 with no GitHub Release artifact.

### Added

- **CI**: `.github/workflows/deploy-api.yml` — tag-triggered API deploy. On a `v*.*.*` tag push the workflow checks out the tagged commit, publishes `src/API` framework-dependent, generates an idempotent EF Core migration script (`dotnet ef migrations script --idempotent`), applies it to Azure SQL via `Invoke-Sqlcmd` ahead of the swap, zip-deploys to `app-dictionaries-manager-prod` via `azure/webapps-deploy@v3`, and smoke-checks `/health` (retried for up to 2 min over App Service cold-start) and `/api/version` (asserts the deployed `InformationalVersion` matches the tag). Authenticates via GitHub OIDC federated identity against the `production` environment. Closes #57.
- **CI**: Generated migration script is also uploaded as a 90-day Actions artifact (`migrations-<tag>`) for audit and manual replay.
- **Docs**: `docs/Deploy.md` ops runbook — App Service Configuration matrix, federated-identity provisioning (App Registration + federated credential + GitHub Variables/Secrets + environment protection), bootstrap-token mint procedure, manual deploy and rollback procedures, dry-run procedure, troubleshooting for the common failures (AADSTS70021, SQL firewall, version mismatch, unhealthy health-check).

### Fixed

- **Auth**: `POST /register` now surfaces `TokenAlreadyUsed → 409` and `TokenRevoked → 423` on the non-race path. Previously `BootstrapTokenService.LookupAsync` filtered to `Issued`-only rows, so a Used or Revoked token surfaced as `TokenInvalid → 401`, conflated with the unknown-token branch. The lookup now iterates all statuses; `RegistrationService.ClassifyOutcome` branches `Used` / `Revoked` to their contracted outcomes before the existing expiry check. The race-loser branch in `CommitSuccessAsync` is unchanged. Closes #58.
- **CI**: `.github/workflows/release.yml` publishes from `src/GUI.Windows/GUI.Windows.csproj` (post-standards-adoption path) instead of the obsolete `src/DictionariesManager.GUI`. The v0.7.0 tag run had failed at the publish step with `MSB1009`, producing an empty GitHub Release; v0.7.1 is the first tag through the fixed pipeline. Closes #61.

## [0.7.0] - 2026-05-18

Bootstrap registration system (spec 001): clients obtain per-installation API credentials via a one-shot admin-issued bootstrap token. End-to-end ceremony is now functional: admin mints a token, the consumer calls `/register` from a fresh install, the server returns a long-lived API credential exactly once and records every attempt in an audit log. Also: `llm-settings v1.3.2` standards adoption and an Italian → English translation pass across the codebase.

### Added

- **API**: `POST /register` bootstrap registration endpoint — validates a single-use bootstrap token + a per-installation descriptor, on success creates a new `Installation` + `InstallationApiCredential` + writes the `RegistrationEvent` audit row in a single atomic transaction (data-model invariant 3 — audit-or-no-issue). The plaintext API credential is returned exactly once and never re-emitted.
- **API**: `POST /api/admin/bootstrap-tokens` admin-only mint endpoint (US2). Admin authentication via the `AdminApiKeys` config section + `AdminAuthenticationMiddleware`; plaintext token is returned exactly once at mint time.
- **API**: Returned per-installation credentials authenticate against the existing API surface in union-mode alongside legacy `ApiKeys` (FR-005). `ApiKeyMiddleware` allow-lists `/register`; admin keys traverse the middleware before hitting the admin gate.
- **Auth crypto**: `PasswordHasher` (PBKDF2-HMAC-SHA256, 600 000 iterations, 16-byte CSPRNG salt, 32-byte derived key, self-describing format) and `TokenGenerator` (CSPRNG-backed `stbt_`/`stak_` 43-char base64url tokens). `InstallationCredentialValidator` with a 5-second positive cache to absorb hot-path load.
- **Auth**: Per-`clientApp` `DescriptorPolicy` mechanism (two-bool record `OsUserIdRequired` / `MachineIdRequired`) at the service layer. Today's only registered consumer is `ButtonPanelTester` (strict); future loose-policy consumers (mobile / web / headless) can register their own entries without entity changes.
- **Auth**: Granular `RegistrationOutcome` classification recorded on every audit row — `Success`, `TokenMissing`, `TokenInvalid`, `TokenAlreadyUsed`, `TokenExpired`, `TokenRevoked`, `ClientScopeMismatch`, `DescriptorMalformed`, `InstallGuidInvalid`, `DescriptorMissingField`, `AuditFailure`. Each maps to a distinct wire status code (400 / 401 / 409 / 410 / 423 / 500); 401 is deliberately conflated across the three scope-related modes (unknown token, scope mismatch, unknown `clientApp`) to hide token scope.
- **Auth**: SemVer 2.0 validation for `descriptor.appVersion` at the service layer (`[GeneratedRegex]` against the canonical semver.org pattern). Malformed values → `DescriptorMalformed → 400`.
- **Persistence**: New EF entities `BootstrapTokenEntity`, `InstallationEntity`, `InstallationApiCredentialEntity`, `RegistrationEventEntity` with appropriate composite/unique/filtered indexes. Migration `AddBootstrapRegistration`.
- **Spec / docs**: Project constitution v1.0.0; full spec-kit bootstrap; spec 001 (`specs/001-bootstrap-registration/`) with `spec.md`, `plan.md`, `tasks.md`, `data-model.md`, `quickstart.md`, `research.md`, three contract docs (`register.md`, `admin-bootstrap-tokens.md`, `admin-installations.md`), and a clarifications log. Privacy posture section in `register.md` codifies SHA-256 SHOULD/MUST guidance for `osUserId` / `machineId`.
- **Standards**: Inline copies of the `llm-settings v1.3.2` standards under `docs/Standards/` and the standards adoption marker (`.stem-standard.json`).

### Changed

- **Repo layout**: Adopted `llm-settings v1.3.2` standards — restructured to `src/` + `tests/`, Central Package Management via `Directory.Packages.props`, common toolchain files at root, GitHub Actions CI / mirror-bitbucket / release workflows. Issue trackers migrated from in-tree `ISSUES.md` files to GitHub Issues.
- **API**: `/register` failure responses now use distinct status codes per the narrowed FR-002. 401 conflates only the three scope-related modes (unknown token, scope mismatch, unknown `clientApp`); other failures use `400` (descriptor issues, including `InstallGuidInvalid` and `DescriptorMissingField`), `409` (token already used), `410` (token expired), `423` (token revoked), `500` (audit failure). The body envelope `{"error":"..."}` is uniform across statuses.
- **Persistence**: `Installations.OsUserId` and `Installations.MachineId` are now nullable (migration `NullableInstallationDescriptorFields`). Per-`clientApp` `DescriptorPolicy` decides whether the consumer must transmit them; loose-policy consumers can register without those identifiers.
- **i18n**: Italian → English translation pass across `src/Core`, `src/Services`, `src/Infrastructure`, `src/GUI.Windows`, XAML views, and the `DatabaseSeeder`. XML doc comments, inline comments, and UI strings are now English by default (legacy seed data still in Italian by design).
- **Dependencies**: `Microsoft.AspNetCore.OpenApi` 10.0.7 → 10.0.8, `Microsoft.AspNetCore.Mvc.Testing` 10.0.7 → 10.0.8, and the `Microsoft.Extensions` group bumped.

## [0.6.0] - 2026-04-13

REST API, DB constraints, board parameter auto-fill, DB error handling.

### Added

- **API**: ASP.NET Core Minimal API project — 12 endpoints (10 business + health check + version), API key authentication via `X-Api-Key` header, JSON camelCase with null omission, dual SQLite/SQL Server provider, Swagger UI in development, deployed to Azure App Service (Linux F1, Italy North).
- **API**: Resolved-variables endpoint merges standard + dictionary-specific + per-dictionary overrides; commands-per-device endpoint returns enabled/disabled state with default-enabled rule; board-definition endpoint matches Production.Tracker's expected shape.
- **API**: 4 consumers configured — Production.Tracker, ButtonPanel.Tester, global comms service, Stem.Device.Manager.
- **Services**: `IDeviceService.GetNextAvailableMachineCodeAsync()` returns the lowest free MachineCode (max+1, skipping the BLE-reserved 6 per BR-015); `IBoardService.GetNextAvailableFirmwareTypeAsync()` returns the lowest free FirmwareType.
- **GUI**: `DeviceEditView` and `BoardEditView` pre-fill MachineCode / FirmwareType with the next available value on creation; "Machine Type" column added to the boards grid in `DeviceDetailView`; `BoardEditView` shows the device name instead of its raw ID.
- **Infrastructure**: 6 DB constraints encoding business rules; migration `AddBusinessRuleConstraints` for Azure SQL.
- **Tests**: 49 new API tests (unit ApiMapper + ApiKeyMiddleware + integration endpoint coverage) and 14 new GUI/service tests for the auto-fill flow; `ApiIntegrationTestBase` covers the Device + Board + Standard + Override scenario.

### Changed

- **Core**: `Board.machineCode` is now a required constructor / `Restore` parameter — was defaulted to `0`, which is illegal per BR-014. The compiler now forces callers to declare the value (T-005).
- **Services**: `BoardMapper.ToDomain` throws `InvalidOperationException` if the related `Device` is not loaded, instead of silently using `MachineCode = 0`.
- **GUI.Windows**: `BoardEditViewModel` injects `IDeviceService` to load `MachineCode` from the device — fixes a pre-existing bug where `ProtocolAddress` was saved as `0x00000000` for boards created via the GUI.
- **Infrastructure**: Connection string resolution centralised in `DependencyInjection.ResolveConnectionString()`; default SQLite path centralised in `DependencyInjection.GetDefaultSqlitePath()`; `GetDatabasePath()` duplicate removed from `GUI.Windows`.
- **API**: `appsettings.json` excluded from publish (`CopyToPublishDirectory=Never`) — Azure consumes its own env var; default `DatabaseProvider` is `Sqlite` in local dev, `SqlServer` in production via env var.

### Fixed

- **GUI**: DB connection failure at startup is now handled with a retry loop and a `DarkDialog` (Retry / Quit), instead of crashing.
- **GUI**: `DarkDialog` owner is now null-safe during startup — WPF was assigning the first instantiated window as `MainWindow`, which could cause `Owner = self` crashes.
- **API**: Endpoints now return `503 Service Unavailable` with structured JSON when the DB is unreachable, instead of `500` with a stacktrace (API-004).

### Removed

- **API**: `UseHttpsRedirection()` (Azure handles HTTPS in production; not needed in dev).
- **API**: `appsettings.Development.json` (override was identical to the base file).
