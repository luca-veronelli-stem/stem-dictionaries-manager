# Changelog

All notable changes to DictionariesManager follow [Semantic Versioning](https://semver.org/) and are recorded here in [Keep a Changelog](https://keepachangelog.com/) format.

## [Unreleased]

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
