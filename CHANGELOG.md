# Changelog

All notable changes to DictionariesManager follow [Semantic Versioning](https://semver.org/) and are recorded here in [Keep a Changelog](https://keepachangelog.com/) format.

## [Unreleased]

### Changed

- Adopted `llm-settings v1.3.2` standards: repo restructured to `src/` + `tests/`, Central Package Management via `Directory.Packages.props`, common toolchain files at root, GitHub Actions CI / mirror-bitbucket / release workflows, inline standards under `docs/Standards/`. Issue trackers migrated from in-tree `ISSUES.md` files to GitHub Issues.

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
