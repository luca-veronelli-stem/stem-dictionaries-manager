# Changelog

All notable changes to DictionariesManager follow [Semantic Versioning](https://semver.org/) and are recorded here in [Keep a Changelog](https://keepachangelog.com/) format.

## [Unreleased]

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
