# Changelog

Tutte le modifiche rilevanti a questo progetto sono documentate in questo file.

Il formato si basa su [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
e questo progetto aderisce a [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

### Legenda

- **Added**: Nuove funzionalità
- **Changed**: Modifiche a funzionalità esistenti
- **Deprecated**: Funzionalità che verranno rimosse
- **Removed**: Funzionalità rimosse
- **Fixed**: Bug corretti
- **Security**: Vulnerabilità corrette

---

## [Unreleased]

### Added

- **API**: Progetto ASP.NET Core Minimal API con 10 endpoint REST read-only
- **API**: Autenticazione via API Key header `X-Api-Key` con chiavi multiple per consumer (BR-API-001)
- **API**: Endpoint variabili risolte con merge standard + specifiche + override per-dizionario (BR-API-002)
- **API**: Endpoint comandi per device con stato attivo/disattivo e default enabled (BR-API-003)
- **API**: JSON camelCase con null omessi per payload leggeri (BR-API-004)
- **API**: Board definition endpoint in formato compatibile Production.Tracker (BR-API-005)
- **API**: 7 DTO record, ApiMapper, ApiKeyMiddleware
- **API**: Swagger UI in Development, file `.http` per test da Visual Studio
- **API**: Dual DB provider SQLite/SQL Server con logica centralizzata
- **Tests**: 49 nuovi test API (13 unit ApiMapper, 8 unit ApiKeyMiddleware, 28 integration endpoint)
- **Tests**: ApiIntegrationTestBase con scenario completo (Device+Board+Standard+Override)

### Changed

- **Infrastructure**: Centralizzata logica risoluzione connection string in `DependencyInjection.ResolveConnectionString()`
- **Infrastructure**: Centralizzato path default SQLite in `DependencyInjection.GetDefaultSqlitePath()`
- **GUI.Windows**: Rimosso `GetDatabasePath()` duplicato, usa metodo centralizzato di Infrastructure

### Removed

- **API**: Rimosso `UseHttpsRedirection()` (non necessario in dev, Azure gestisce HTTPS in prod)
- **API**: Rimosso `appsettings.Development.json` (override identico a base, nessun effetto)

---

## [0.5.0] - 2026-04-09

Prima release interna per test.

### Added

- **Core**: 10 domain models (Variable, Dictionary, Board, Device, Command, User, AuditEntry, BitInterpretation, CommandDeviceState, StandardVariableOverride) + 5 enums
- **Domain v7**: StandardVariableOverride per-dizionario, BitInterpretation.DictionaryId, eredità automatica variabili standard
- **Infrastructure**: EF Core con dual provider SQLite (dev) / Azure SQL (prod), selezionabile via `appsettings.json`
- **Infrastructure**: 10 repository con interfacce + RepositoryBase generico
- **Infrastructure**: Audit automatico CreatedAt/UpdatedAt in SaveChanges
- **Infrastructure**: Migration InitialCreate per SQL Server (Azure SQL)
- **Infrastructure**: DatabaseSeeder con 14 dizionari + Standard, 5 utenti, dati completi per tutti i device STEM
- **Services**: 7 service + 10 mapper bidirezionali Entity ↔ Domain
- **Services**: AuditService con log automatico Create/Update/Delete integrato in 5 service (16 punti)
- **Services**: ICurrentUserProvider (Singleton) per tracciare utente corrente
- **Services**: Business rules BR-001..BR-023 (unicità indirizzi, Standard unico, override coerenza, cascade delete, auto-assign FW)
- **GUI**: Applicazione WPF desktop con dark theme STEM (#004682 accent, #98D801 success, #E40032 error)
- **GUI**: 14 ViewModels + 14 Views XAML con CommunityToolkit.Mvvm
- **GUI**: LoginView integrata nella MainWindow con selezione utente
- **GUI**: NavigationService con history stack, parametri e ViewModel caching
- **GUI**: DialogService con DarkDialog custom modale
- **GUI**: MessageService con status bar globale, colori per severity e auto-hide
- **GUI**: IEditableViewModel con guard per unsaved changes su navigazione indietro
- **GUI**: DeviceListView, DeviceEditView, DeviceDetailView per CRUD dispositivi
- **GUI**: DictionaryListView, DictionaryEditView con 2 sezioni (variabili standard ereditate + specifiche)
- **GUI**: VariableEditView con supporto Bitmapped (WordGroups, WordSize 8/16/32) e modalità override standard
- **GUI**: CommandListView, CommandEditView con parametri e CodeHigh computed da IsResponse
- **GUI**: DeviceCommandsView per stato attivo/disattivo comandi per device (checkbox, salvataggio bulk)
- **GUI**: BoardEditView con FirmwareType, DictionaryId opzionale, IsPrimary
- **GUI**: Filtro `Mostra solo abilitate` per variabili specifiche e standard in DictionaryEdit
- **GUI**: Ricerca client-side case-insensitive in tutte le liste
- **GUI**: 7 converter (BoolToVisibility, InverseBool, NullToVisibility, BoolToErrorBrush, NullableInt, NullableDouble, SeverityToColor)
- **Tests**: ~1370 metodi test / ~2230 test cases (unit + integration + E2E)
- **Tests**: Multi-target net10.0 (CI/Linux) + net10.0-windows (GUI)
- **Tests**: 170 E2E test per DatabaseSeeder (verifica dati seed completi)
- **Docs**: README per ogni componente (Core, Infrastructure, Services, GUI.Windows, Tests)
- **Docs**: ISSUES_TRACKER.md con riepilogo globale e metriche qualità
- **Docs**: Schema ER database (PlantUML)
- **Docs**: Formalizzazione Lean 4 del dominio in copilot-instructions.md

### Note

- Richiede .NET 10.0 Desktop Runtime
- SQLite: il DB viene creato automaticamente all'avvio (EnsureCreated)
- Azure SQL: richiede connection string via User Secrets (`ConnectionStrings:SqlServer`)
- 14 issue bassa priorità aperte, 0 alta/media — tutte le feature pianificate per v0.5.0 completate

---

## Storico URL versioni

[Unreleased]: https://bitbucket.org/stem-fw/stem-dictionaries-manager/branches/compare/HEAD..v0.5.0
[0.5.0]: https://bitbucket.org/stem-fw/stem-dictionaries-manager/src/v0.5.0/
