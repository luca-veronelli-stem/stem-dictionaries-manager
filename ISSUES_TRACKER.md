# Stem.Dictionaries.Manager - Issue Tracker

> **Ultimo aggiornamento:** 2026-03-24

---

## Riepilogo Globale

| Componente | Aperte | Risolte | Totale |
|------------|--------|---------|--------|
| [Core](./Core/ISSUES.md) | 4 | 2 | 6 |
| [Infrastructure](./Infrastructure/ISSUES.md) | 5 | 2 | 7 |
| [Services](./Services/ISSUES.md) | 9 | 1 | 10 |
| [GUI.Windows](./GUI.Windows/ISSUES.md) | 5 | 2 | 7 |
| [Tests](./Tests/ISSUES.md) | 3 | 5 | 8 |
| **Trasversali** | **0** | **1** | **1** |
| **Totale** | **26** | **13** | **39** |

---

## Distribuzione per Priorità

| Priorità | Aperte | % |
|----------|--------|---|
| **Critica** | 0 | 0% |
| **Alta** | 4 | 15% |
| **Media** | 9 | 35% |
| **Bassa** | 13 | 50% |
| **Totale** | **26** | 100% |

```
Critica:     ░░░░░░░░░░░░░░░░░░░░  0
Alta:        ████░░░░░░░░░░░░░░░░  4  ⚠️ Bug attivi
Media:       █████████░░░░░░░░░░░  9
Bassa:       █████████████░░░░░░░ 13
```

---

## Issue Alta Priorità

| ID | Componente | Titolo | Status |
|----|------------|--------|--------|
| **SVC-008** | Services | DictionaryService.AddAsync blocca Shared Peripheral | 🔴 **Aperto** |
| **INFRA-007** | Infrastructure | DatabaseSeeder.CreateBoard usa boardTypeId invece di FirmwareType | 🔴 **Aperto** |
| **GUI-005** | GUI.Windows | MainViewModel.NavigateToView async void senza error handling | 🔴 **Aperto** |
| **TEST-007** | Tests | Manca test integration per Shared Peripheral (copre SVC-008) | 🔴 **Aperto** |
| ~~INFRA-001~~ | Infrastructure | DeleteAsync non solleva eccezione | ✅ **Risolto** |
| ~~T-001~~ | Trasversale | Dizionario Standard deve essere unico | ✅ **Risolto** |

⚠️ **4 issue alta priorità aperte — 2 bug, 1 crash potenziale, 1 gap copertura critico.**

---

## Issue Trasversali (T-xxx)

| ID | Titolo | Priorità | Status | Componenti Coinvolti |
|----|--------|----------|--------|----------------------|
| ~~T-001~~ | Dizionario Standard deve essere unico | Alta | ✅ **Risolto** | Services |

### T-001 — Dizionario Standard deve essere unico

**Descrizione:**  
Il dizionario "Standard" (senza `BoardType`) deve essere unico nel sistema. Attualmente non esiste alcun vincolo che impedisca la creazione di più dizionari senza `BoardTypeId`.

**Status:** ✅ **Risolto** (branch `fix/t-001`)  
**Data Risoluzione:** 2026-03-19

**Soluzione Implementata:**
1. `DictionaryService.AddAsync()`: se `BoardType == null`, verifica che non esista già un dizionario Standard via `GetStandardDictionaryAsync()`
2. `DictionaryService.UpdateAsync()`: se si cambia `BoardType` da non-null a null, stessa verifica
3. Business Rule **BR-007** codificata con `InvalidOperationException`

**Test aggiunti:**
- `AddAsync_SecondStandardDictionary_ThrowsInvalidOperationException`
- `UpdateAsync_ChangingToStandard_WhenOneExists_ThrowsInvalidOperationException`

---

## Issue per Componente

### Core (4 issue aperte, 2 risolte)

| ID | Titolo | Priorità | Categoria |
|----|--------|----------|-----------|
| ~~CORE-001~~ | ~~AuditEntityType contiene "Device" non esistente~~ | ~~Media~~ | ✅ **Risolto** |
| ~~CORE-002~~ | ~~Variable.Category deriva solo da AddressHigh == 0x00~~ | ~~Media~~ | ✅ **Risolto** |
| [CORE-006](./Core/ISSUES.md#core-006--dictionaryrestore-bypassa-validazione-unicità-indirizzi) | Dictionary.Restore bypassa validazione unicità indirizzi | Media | Bug |
| [CORE-003](./Core/ISSUES.md#core-003--dictionaryremovevariable-non-verifica-esistenza) | Dictionary.RemoveVariable non verifica esistenza | Bassa | API |
| [CORE-004](./Core/ISSUES.md#core-004--mancanza-di-metodi-update-sui-modelli) | Mancanza di metodi Update sui modelli | Bassa | API |
| [CORE-005](./Core/ISSUES.md#core-005--bitinterpretationvariableid-non-ha-validazione-positiva) | BitInterpretation.VariableId non ha validazione | Bassa | API |

### Infrastructure (5 issue aperte, 2 risolte)

| ID | Titolo | Priorità | Categoria |
|----|--------|----------|-----------|
| ~~INFRA-001~~ | ~~DeleteAsync non solleva eccezione~~ | ~~Alta~~ | ✅ **Risolto** |
| [INFRA-007](./Infrastructure/ISSUES.md#infra-007--databaseseedercreateboard-usa-boardtypeid-invece-di-firmwaretype) | **DatabaseSeeder.CreateBoard usa boardTypeId invece di FirmwareType** | **Alta** | **Bug** |
| [INFRA-002](./Infrastructure/ISSUES.md#infra-002--getallasync-senza-paginazione-rischia-performance-issues) | GetAllAsync senza paginazione | Media | Performance |
| [INFRA-003](./Infrastructure/ISSUES.md#infra-003--designtimedbcontextfactory-ha-path-hardcoded-fragile) | DesignTimeDbContextFactory path fragile | Media | Manutenibilità |
| ~~INFRA-004~~ | ~~Mancano repository BitInterpretation/CommandDeviceState~~ | ~~Bassa~~ | ✅ **Risolto** (SVC-001) |
| [INFRA-005](./Infrastructure/ISSUES.md#infra-005--commandentityparametersjson-non-ha-conversione-json-tipizzata) | ParametersJson stringa grezza | Bassa | Design |
| [INFRA-006](./Infrastructure/ISSUES.md#infra-006--dictionaryrepositorygetbynameasync-non-normalizza-input) | GetByNameAsync non normalizza input | Bassa | Bug |

### Services (9 issue aperte, 1 risolta)

| ID | Titolo | Priorità | Categoria |
|----|--------|----------|-----------|
| ~~SVC-001~~ | ~~Services dipendono da AppDbContext~~ | ~~Media~~ | ✅ **Risolto** |
| [SVC-008](./Services/ISSUES.md#svc-008--dictionaryserviceaddasync-blocca-shared-peripheral-se-standard-esiste) | **DictionaryService.AddAsync blocca Shared Peripheral** | **Alta** | **Bug** |
| [SVC-002](./Services/ISSUES.md#svc-002--manca-iauditservice-per-gestione-audit-trail) | Manca IAuditService | Media | Feature |
| [SVC-003](./Services/ISSUES.md#svc-003--getallasync-senza-paginazione-nei-services) | GetAllAsync senza paginazione | Media | Performance |
| [SVC-009](./Services/ISSUES.md#svc-009--variablemappertodomain-non-mappa-format) | VariableMapper.ToDomain non mappa Format | Media | Bug (Data Loss) |
| [SVC-004](./Services/ISSUES.md#svc-004--mancano-mapper-per-boardmapper-con-overload) | BoardMapper overload mancanti | Bassa | Code Smell |
| [SVC-005](./Services/ISSUES.md#svc-005--commandservicegetwithdevicestatesasync-non-espone-devicestates) | GetWithDeviceStates non espone stati | Bassa | Design |
| [SVC-006](./Services/ISSUES.md#svc-006--manca-validazione-business-rules-centralizzata) | Manca validazione centralizzata | Bassa | Design |
| [SVC-007](./Services/ISSUES.md#svc-007--dependencyinjection-non-valida-prerequisiti) | DI non valida prerequisiti | Bassa | Robustezza |
| [SVC-010](./Services/ISSUES.md#svc-010--class1cs-placeholder-non-rimosso) | Class1.cs placeholder non rimosso | Bassa | Code Smell |

### GUI.Windows (5 issue aperte, 2 risolte)

| ID | Titolo | Priorità | Categoria |
|----|--------|----------|-----------|
| ~~GUI-001~~ | ~~Mancano ViewModels per ViewType dichiarate~~ | ~~Media~~ | ✅ **Risolto** |
| ~~GUI-004~~ | ~~Refactoring grafico completo e migrazione login~~ | ~~Media~~ | ✅ **Risolto** |
| [GUI-005](./GUI.Windows/ISSUES.md#gui-005--mainviewmodelnavigatetoview-è-async-void-senza-error-handling) | **NavigateToView async void senza error handling** | **Alta** | **Bug** |
| [GUI-006](./GUI.Windows/ISSUES.md#gui-006--loginviewmodel-registrato-due-volte-nel-di-container) | LoginViewModel registrato due volte nel DI | Media | Code Smell |
| [GUI-007](./GUI.Windows/ISSUES.md#gui-007--dictionarylistitem-non-mostra-devicetype-semantica-dedicato) | DictionaryListItem non mostra DeviceType | Media | UX |
| [GUI-002](./GUI.Windows/ISSUES.md#gui-002--appservices-è-static-e-impedisce-testabilità) | App.Services static impedisce testabilità | Bassa | Design |
| [GUI-003](./GUI.Windows/ISSUES.md#gui-003--dialogservice-usa-messagebox-sincrono-wrappato-in-task) | DialogService finto async | Bassa | Design |

### Tests (3 issue aperte, 5 risolte)

| ID | Titolo | Priorità | Categoria |
|----|--------|----------|-----------|
| ~~TEST-001~~ | ~~Mancano test BoardRepository e CommandRepository~~ | ~~Media~~ | ✅ **Risolto** |
| ~~TEST-002~~ | ~~Mancano test BoardTypeRepository~~ | ~~Media~~ | ✅ **Risolto** |
| ~~TEST-003~~ | ~~Uso .Wait() bloccante~~ | ~~Media~~ | ✅ **Risolto** |
| ~~TEST-004~~ | ~~Mancano test DI~~ | ~~Bassa~~ | ✅ **Risolto** |
| ~~TEST-005~~ | ~~Mancano test scenari update/delete~~ | ~~Bassa~~ | ✅ **Risolto** |
| [TEST-007](./Tests/ISSUES.md#test-007--manca-test-integration-per-shared-peripheral-in-dictionaryservice) | **Manca test Shared Peripheral (copre SVC-008)** | **Alta** | **Copertura** |
| [TEST-008](./Tests/ISSUES.md#test-008--variablemappertests-non-testa-format-round-trip) | VariableMapperTests non testa Format round-trip | Media | Copertura |
| [TEST-006](./Tests/ISSUES.md#test-006--magic-strings-ripetute-nei-test) | Magic strings ripetute | Bassa | Manutenibilità |

---

## Top 5 Issue da Risolvere

| # | ID | Componente | Titolo | Effort |
|---|-----|------------|--------|--------|
| 1 | **SVC-008** | Services | DictionaryService.AddAsync blocca Shared Peripheral | S |
| 2 | **INFRA-007** | Infrastructure | DatabaseSeeder.CreateBoard usa boardTypeId (indirizzi errati) | S |
| 3 | **GUI-005** | GUI.Windows | NavigateToView async void senza try/catch (crash) | S |
| 4 | **SVC-009** | Services | VariableMapper.ToDomain non mappa Format (data loss) | S |
| 5 | **TEST-007** | Tests | Manca test Shared Peripheral (insieme a fix SVC-008) | S |

**Effort:** S = 1-2h, M = 4-8h, L = 1-2 giorni

> ⚠️ **Le issue #1 e #5 devono essere risolte insieme** (fix SVC-008 + test TEST-007).  
> ⚠️ **La issue #4 va risolta con TEST-008** (fix SVC-009 + test round-trip Format).

---

## Copertura Test Attuale

| Componente | Unit | Integration | Copertura |
|------------|------|-------------|-----------|
| Core/Enums (6) | ✅ 21 | - | 100% |
| Core/Models (9) | ✅ 100 | - | 100% |
| Infrastructure/Repositories (9) | - | ✅ 86 | ~98% |
| Services/Mapping (8) | ✅ 80 | - | ~100% |
| Services (5) | - | ✅ 90 | ~95% |
| GUI.Windows/ViewModels (12) | ✅ 216 | ✅ 10 | ~95% |
| GUI.Windows/Services (3) | ✅ 12 | - | ~85% |
| GUI.Windows/Converters (2) | ✅ 18 | - | 100% |
| GUI.Windows/DI | ✅ 21 | - | 100% |

**Totale test:** ~440 CI (net10.0) / 1112 Windows (net10.0-windows)

---

## Metriche Qualità

| Aspetto | Stato | Note |
|---------|-------|------|
| **Architecture** | ✅ 95% | Layer separation corretta, Services decoupled |
| **Thread Safety** | ✅ 95% | Modelli immutabili |
| **Input Validation** | ⚠️ 70% | SVC-008 (Shared Peripheral), CORE-006, CORE-005 |
| **Data Integrity** | ⚠️ 75% | SVC-009 (Format data loss), INFRA-007 (indirizzi errati) |
| **Performance** | ⚠️ 70% | GetAllAsync senza paginazione (INFRA-002, SVC-003) |
| **Resilience** | ⚠️ 75% | GUI-005 (crash su navigazione), INFRA-003 (path fragile) |
| **Code Consistency** | ✅ 85% | INFRA-006, GUI-006, SVC-010 |
| **Test Coverage** | ✅ 88% | TEST-007/008 gap su 3 semantiche e Format |

---

## Issue per Categoria

| Categoria | Count | Issue |
|-----------|-------|-------|
| **Bug** | 5 | **SVC-008**, **INFRA-007**, **SVC-009**, CORE-006, INFRA-006 |
| **Design** | 4 | SVC-005, SVC-006, INFRA-005, GUI-002 |
| **UX** | 2 | GUI-003, GUI-007 |
| **Performance** | 2 | INFRA-002, SVC-003 |
| **Copertura** | 2 | **TEST-007**, TEST-008 |
| **API** | 3 | CORE-003, CORE-004, CORE-005 |
| **Manutenibilità** | 2 | INFRA-003, TEST-006 |
| **Code Smell** | 3 | SVC-004, SVC-010, GUI-006 |
| **Feature** | 1 | SVC-002 |
| **Robustezza** | 2 | SVC-007, **GUI-005** |

---

## Come Contribuire

1. Seleziona issue da priorità alta o Top 5
2. Crea branch: `fix/{issue-id}` (es. `fix/infra-001`)
3. Implementa soluzione proposta nel file ISSUES.md del componente
4. Aggiungi test se applicabile
5. Aggiorna status issue a "Risolto" con data e branch
6. Pull Request verso `main`

---

## Links

- [ISSUES_TEMPLATE.md](./Docs/Standards/Templates/ISSUES_TEMPLATE.md) - Template per nuove issue
- [copilot-instructions.md](.copilot/copilot-instructions.md) - Istruzioni progetto
- [issues-agent.md](.copilot/agents/issues-agent.md) - Agent per analisi issue

---

## Changelog

| Data | Modifica |
|------|----------|
| 2026-03-24 | 🔍 **Audit completo tutti i componenti** — Analisi approfondita Core, Infrastructure, Services, GUI.Windows, Tests. +10 nuove issue (CORE-006, INFRA-007, SVC-008/009/010, GUI-005/006/007, TEST-007/008). 4 issue alta priorità identificate: 2 bug (SVC-008 Shared Peripheral, INFRA-007 indirizzi), 1 crash (GUI-005 async void), 1 gap test (TEST-007). Totale: 26 aperte, 13 risolte |
| 2026-03-20 | :sparkles: **Dictionary Uniqueness** - Aggiunto `DeviceType` a Dictionary, migrazione su `DictionaryEntity` con unique constraint `(DeviceType, BoardType)`, rimosso `DeviceType` da `BitInterpretation` (branch `fix/unicita-dizionario`) |
| 2026-03-20 | ✅ **GUI-004 risolta** - Refactoring grafico completo: dark theme VS Code-style, login integrato nella MainWindow (pattern PT), rimosso CurrentUserService, +11 test (1112 totali Windows) (branch `gui/refactoring-completo`) |
| 2026-03-19 | ✨ **Filtro/ricerca nelle liste** - SearchText filtra client-side in tutti i 5 ListViewModel (case-insensitive), +15 test (1111 totali Windows) (branch `feature/filtro-ricerca`) |
| 2026-03-19 | ✨ **Selezione utente all'avvio**
| 2026-03-19 | ✅ **CORE-001 + CORE-002 risolte**
| 2026-03-19 | ✅ **T-001 risolta**
| 2026-03-19 | ✅ **GUI-001 risolta** - Implementati 8 ViewModels mancanti (Variable, Command, Board, User, Settings), +109 test GUI (1007 totali Windows) (branch `gui/view-models-mancanti`) |
| 2026-03-19 | ✨ **GUI.Windows aggiunto** - README + ISSUES creati, 3 issue identificate (GUI-001/002/003), 63 test unit aggiunti (branch `feature/gui-base`) |
| 2026-03-18 | ⚠️ **T-001 aggiunta** - Dizionario Standard deve essere unico (priorità Alta, trasversale Core/Services/Infrastructure) |
| 2026-03-18 | ✅ **SVC-001 + INFRA-004 risolte** - Services decoupled da AppDbContext, creati BitInterpretationRepository e CommandDeviceStateRepository, +144 test (752 totali) (branch `fix/svc-001`) |
| 2026-03-18 | ✅ **TEST-001/002 risolte** - Aggiunti 33 test per BoardRepository, BoardTypeRepository, CommandRepository (branch `fix/test-001-002`) |
| 2026-03-18 | ✅ **INFRA-001 risolta** - DeleteAsync ora lancia KeyNotFoundException (branch `fix/infra-001`) |
| 2026-03-18 | Creazione iniziale con 17 issue da 3 componenti (Core, Infrastructure, Tests) |
