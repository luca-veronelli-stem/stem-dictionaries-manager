# Stem.Dictionaries.Manager - Issue Tracker

> **Ultimo aggiornamento:** 2026-03-25

---

## Riepilogo Globale

| Componente | Aperte | Risolte | Totale |
|------------|--------|---------|--------|
| [Core](./Core/ISSUES.md) | 5 | 2 | 7 |
| [Infrastructure](./Infrastructure/ISSUES.md) | 6 | 2 | 8 |
| [Services](./Services/ISSUES.md) | 10 | 1 | 11 |
| [GUI.Windows](./GUI.Windows/ISSUES.md) | 6 | 2 | 8 |
| [Tests](./Tests/ISSUES.md) | 4 | 5 | 9 |
| **Trasversali** | **1** | **1** | **2** |
| **Totale** | **32** | **13** | **45** |

---

## Distribuzione per Priorità

| Priorità | Aperte | % |
|----------|--------|---|
| **Critica** | 0 | 0% |
| **Alta** | 10 | 31% |
| **Media** | 9 | 28% |
| **Bassa** | 13 | 41% |
| **Totale** | **32** | 100% |

```
Critica:     ░░░░░░░░░░░░░░░░░░░░  0
Alta:        ██████████░░░░░░░░░░ 10  ⚠️ Refactoring Domain v2 pianificato
Media:       █████████░░░░░░░░░░░  9
Bassa:       █████████████░░░░░░░ 13
```

---

## Issue Alta Priorità

| ID | Componente | Titolo | Status |
|----|------------|--------|--------|
| **T-002** | Trasversale | **Rimozione BoardType e link diretto Board→Dictionary** | 🔴 **Aperto** |
| **CORE-007** | Core | Refactoring Core models per Domain v2 | 🔴 **Aperto** |
| **INFRA-008** | Infrastructure | Refactoring Infrastructure per Domain v2 | 🔴 **Aperto** |
| **SVC-011** | Services | Refactoring Services per Domain v2 | 🔴 **Aperto** |
| **GUI-008** | GUI.Windows | Refactoring GUI per Domain v2 | 🔴 **Aperto** |
| **TEST-009** | Tests | Aggiornamento test per Domain v2 | 🔴 **Aperto** |
| **SVC-008** | Services | DictionaryService.AddAsync blocca Shared Peripheral | 🟡 **Superseded da T-002** |
| **INFRA-007** | Infrastructure | DatabaseSeeder.CreateBoard usa boardTypeId | 🟡 **Superseded da T-002** |
| **GUI-005** | GUI.Windows | NavigateToView async void senza error handling | 🔴 **Aperto** |
| **TEST-007** | Tests | Manca test Shared Peripheral | 🟡 **Superseded da T-002** |
| ~~INFRA-001~~ | Infrastructure | DeleteAsync non solleva eccezione | ✅ **Risolto** |
| ~~T-001~~ | Trasversale | Dizionario Standard deve essere unico | ✅ **Risolto** |

⚠️ **10 issue alta priorità aperte — 6 refactoring Domain v2 (T-002), 3 superseded, 1 crash (GUI-005).**

> **NOTA:** SVC-008, INFRA-007, TEST-007, SVC-004, GUI-007 verranno **risolte automaticamente** dal refactoring T-002.

---

## Issue Trasversali (T-xxx)

| ID | Titolo | Priorità | Status | Componenti Coinvolti |
|----|--------|----------|--------|----------------------|
| **T-002** | Rimozione BoardType e link diretto Board→Dictionary | Alta | 🔴 **Aperto** | Core, Infrastructure, Services, GUI.Windows, Tests |
| ~~T-001~~ | Dizionario Standard deve essere unico | Alta | ✅ **Risolto** | Services |

### T-002 — Rimozione BoardType e link diretto Board→Dictionary

**Descrizione:**  
Refactoring completo del domain model: rimozione entità `BoardType`, spostamento `FirmwareType` su `Board`, link diretto `Board→Dictionary` (nullable), sostituzione semantica 3-tuple `(DeviceType?, BoardType?)` con `IsStandard` flag e semantica derivata.

**Status:** 🔴 **Aperto**  
**Branch previsto:** `refactor/domain-v2-remove-boardtype`  
**Data Apertura:** 2026-03-25  
**Riferimento:** Lean 4 Specification v2 (SESSION_024)

**Sub-issue (ordine di esecuzione):**

| # | ID | Componente | Titolo | Effort | Dipende da |
|---|-----|------------|--------|--------|------------|
| 1 | **CORE-007** | Core | Refactoring Core models | S (1-2h) | — |
| 2 | **INFRA-008** | Infrastructure | Refactoring Infrastructure | M (3-4h) | CORE-007 |
| 3 | **SVC-011** | Services | Refactoring Services | M (3-4h) | INFRA-008 |
| 4 | **GUI-008** | GUI.Windows | Refactoring GUI | M (3-4h) | SVC-011 |
| 5 | **TEST-009** | Tests | Aggiornamento test | L (4-6h) | CORE-007..GUI-008 |

**Issue risolte automaticamente dal refactoring:**
- **SVC-008** (AddAsync blocca Shared Peripheral) — logica 3 semantiche rimossa
- **INFRA-007** (DatabaseSeeder usa boardTypeId) — seeder riscritto
- **TEST-007** (Manca test Shared Peripheral) — nuovi test con semantica derivata
- **SVC-004** (BoardMapper overload mancanti) — mapper riscritto
- **GUI-007** (DictionaryListItem non mostra DeviceType) — colonna "Usato da" derivata

**Effort totale stimato:** ~15-20h (2-3 giorni)

---

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

### Core (5 issue aperte, 2 risolte)

| ID | Titolo | Priorità | Categoria |
|----|--------|----------|-----------|
| ~~CORE-001~~ | ~~AuditEntityType contiene "Device" non esistente~~ | ~~Media~~ | ✅ **Risolto** |
| ~~CORE-002~~ | ~~Variable.Category deriva solo da AddressHigh == 0x00~~ | ~~Media~~ | ✅ **Risolto** |
| [CORE-007](./Core/ISSUES.md#core-007--refactoring-core-models-per-domain-v2) | **Refactoring Core models per Domain v2 (T-002)** | **Alta** | **Refactoring** |
| [CORE-006](./Core/ISSUES.md#core-006--dictionaryrestore-bypassa-validazione-unicità-indirizzi) | Dictionary.Restore bypassa validazione unicità indirizzi | Media | Bug |
| [CORE-003](./Core/ISSUES.md#core-003--dictionaryremovevariable-non-verifica-esistenza) | Dictionary.RemoveVariable non verifica esistenza | Bassa | API |
| [CORE-004](./Core/ISSUES.md#core-004--mancanza-di-metodi-update-sui-modelli) | Mancanza di metodi Update sui modelli | Bassa | API |
| [CORE-005](./Core/ISSUES.md#core-005--bitinterpretationvariableid-non-ha-validazione-positiva) | BitInterpretation.VariableId non ha validazione | Bassa | API |

### Infrastructure (6 issue aperte, 2 risolte)

| ID | Titolo | Priorità | Categoria |
|----|--------|----------|-----------|
| ~~INFRA-001~~ | ~~DeleteAsync non solleva eccezione~~ | ~~Alta~~ | ✅ **Risolto** |
| [INFRA-008](./Infrastructure/ISSUES.md#infra-008--refactoring-infrastructure-per-domain-v2) | **Refactoring Infrastructure per Domain v2 (T-002)** | **Alta** | **Refactoring** |
| [INFRA-007](./Infrastructure/ISSUES.md#infra-007--databaseseedercreateboard-usa-boardtypeid-invece-di-firmwaretype) | DatabaseSeeder.CreateBoard usa boardTypeId | Alta | Bug — Superseded da T-002 |
| [INFRA-002](./Infrastructure/ISSUES.md#infra-002--getallasync-senza-paginazione-rischia-performance-issues) | GetAllAsync senza paginazione | Media | Performance |
| [INFRA-003](./Infrastructure/ISSUES.md#infra-003--designtimedbcontextfactory-ha-path-hardcoded-fragile) | DesignTimeDbContextFactory path fragile | Media | Manutenibilità |
| ~~INFRA-004~~ | ~~Mancano repository BitInterpretation/CommandDeviceState~~ | ~~Bassa~~ | ✅ **Risolto** (SVC-001) |
| [INFRA-005](./Infrastructure/ISSUES.md#infra-005--commandentityparametersjson-non-ha-conversione-json-tipizzata) | ParametersJson stringa grezza | Bassa | Design |
| [INFRA-006](./Infrastructure/ISSUES.md#infra-006--dictionaryrepositorygetbynameasync-non-normalizza-input) | GetByNameAsync non normalizza input | Bassa | Bug |

### Services (10 issue aperte, 1 risolta)

| ID | Titolo | Priorità | Categoria |
|----|--------|----------|-----------|
| ~~SVC-001~~ | ~~Services dipendono da AppDbContext~~ | ~~Media~~ | ✅ **Risolto** |
| [SVC-011](./Services/ISSUES.md#svc-011--refactoring-services-per-domain-v2) | **Refactoring Services per Domain v2 (T-002)** | **Alta** | **Refactoring** |
| [SVC-008](./Services/ISSUES.md#svc-008--dictionaryserviceaddasync-blocca-shared-peripheral-se-standard-esiste) | DictionaryService.AddAsync blocca Shared Peripheral | Alta | Bug — Superseded da T-002 |
| [SVC-002](./Services/ISSUES.md#svc-002--manca-iauditservice-per-gestione-audit-trail) | Manca IAuditService | Media | Feature |
| [SVC-003](./Services/ISSUES.md#svc-003--getallasync-senza-paginazione-nei-services) | GetAllAsync senza paginazione | Media | Performance |
| [SVC-009](./Services/ISSUES.md#svc-009--variablemappertodomain-non-mappa-format) | VariableMapper.ToDomain non mappa Format | Media | Bug (Data Loss) |
| [SVC-004](./Services/ISSUES.md#svc-004--mancano-mapper-per-boardmapper-con-overload) | BoardMapper overload mancanti | Bassa | Code Smell |
| [SVC-005](./Services/ISSUES.md#svc-005--commandservicegetwithdevicestatesasync-non-espone-devicestates) | GetWithDeviceStates non espone stati | Bassa | Design |
| [SVC-006](./Services/ISSUES.md#svc-006--manca-validazione-business-rules-centralizzata) | Manca validazione centralizzata | Bassa | Design |
| [SVC-007](./Services/ISSUES.md#svc-007--dependencyinjection-non-valida-prerequisiti) | DI non valida prerequisiti | Bassa | Robustezza |
| [SVC-010](./Services/ISSUES.md#svc-010--class1cs-placeholder-non-rimosso) | Class1.cs placeholder non rimosso | Bassa | Code Smell |

### GUI.Windows (6 issue aperte, 2 risolte)

| ID | Titolo | Priorità | Categoria |
|----|--------|----------|-----------|
| ~~GUI-001~~ | ~~Mancano ViewModels per ViewType dichiarate~~ | ~~Media~~ | ✅ **Risolto** |
| ~~GUI-004~~ | ~~Refactoring grafico completo e migrazione login~~ | ~~Media~~ | ✅ **Risolto** |
| [GUI-008](./GUI.Windows/ISSUES.md#gui-008--refactoring-gui-per-domain-v2) | **Refactoring GUI per Domain v2 (T-002)** | **Alta** | **Refactoring** |
| [GUI-005](./GUI.Windows/ISSUES.md#gui-005--mainviewmodelnavigatetoview-è-async-void-senza-error-handling) | **NavigateToView async void senza error handling** | **Alta** | **Bug** |
| [GUI-006](./GUI.Windows/ISSUES.md#gui-006--loginviewmodel-registrato-due-volte-nel-di-container) | LoginViewModel registrato due volte nel DI | Media | Code Smell |
| [GUI-007](./GUI.Windows/ISSUES.md#gui-007--dictionarylistitem-non-mostra-devicetype-semantica-dedicato) | DictionaryListItem non mostra DeviceType | Media | UX — Superseded da T-002 |
| [GUI-002](./GUI.Windows/ISSUES.md#gui-002--appservices-è-static-e-impedisce-testabilità) | App.Services static impedisce testabilità | Bassa | Design |
| [GUI-003](./GUI.Windows/ISSUES.md#gui-003--dialogservice-usa-messagebox-sincrono-wrappato-in-task) | DialogService finto async | Bassa | Design |

### Tests (4 issue aperte, 5 risolte)

| ID | Titolo | Priorità | Categoria |
|----|--------|----------|-----------|
| ~~TEST-001~~ | ~~Mancano test BoardRepository e CommandRepository~~ | ~~Media~~ | ✅ **Risolto** |
| ~~TEST-002~~ | ~~Mancano test BoardTypeRepository~~ | ~~Media~~ | ✅ **Risolto** |
| ~~TEST-003~~ | ~~Uso .Wait() bloccante~~ | ~~Media~~ | ✅ **Risolto** |
| ~~TEST-004~~ | ~~Mancano test DI~~ | ~~Bassa~~ | ✅ **Risolto** |
| ~~TEST-005~~ | ~~Mancano test scenari update/delete~~ | ~~Bassa~~ | ✅ **Risolto** |
| [TEST-009](./Tests/ISSUES.md#test-009--aggiornamento-test-per-domain-v2) | **Aggiornamento test per Domain v2 (T-002)** | **Alta** | **Refactoring** |
| [TEST-007](./Tests/ISSUES.md#test-007--manca-test-integration-per-shared-peripheral-in-dictionaryservice) | Manca test Shared Peripheral | Alta | Copertura — Superseded da T-002 |
| [TEST-008](./Tests/ISSUES.md#test-008--variablemappertests-non-testa-format-round-trip) | VariableMapperTests non testa Format round-trip | Media | Copertura |
| [TEST-006](./Tests/ISSUES.md#test-006--magic-strings-ripetute-nei-test) | Magic strings ripetute | Bassa | Manutenibilità |

---

## Roadmap: Refactoring Domain v2 (T-002)

| # | ID | Componente | Titolo | Effort | Dipende da |
|---|-----|------------|--------|--------|------------|
| 1 | **CORE-007** | Core | Refactoring Core models | S (1-2h) | — |
| 2 | **INFRA-008** | Infrastructure | Refactoring Infrastructure + Migration | M (3-4h) | CORE-007 |
| 3 | **SVC-011** | Services | Refactoring Services + Mappers | M (3-4h) | INFRA-008 |
| 4 | **GUI-008** | GUI.Windows | Refactoring ViewModels + Views | M (3-4h) | SVC-011 |
| 5 | **TEST-009** | Tests | Aggiornamento/riscrittura test | L (4-6h) | CORE-007..GUI-008 |

**Effort totale:** ~15-20h (2-3 giorni)  
**Branch:** `refactor/domain-v2-remove-boardtype`

> ⚠️ **T-002 risolve automaticamente 5 issue:** SVC-008, INFRA-007, TEST-007, SVC-004, GUI-007.

## Altre Issue da Risolvere (dopo T-002)

| # | ID | Componente | Titolo | Effort |
|---|-----|------------|--------|--------|
| 1 | **GUI-005** | GUI.Windows | NavigateToView async void senza try/catch (crash) | S |
| 2 | **SVC-009** | Services | VariableMapper.ToDomain non mappa Format (data loss) | S |
| 3 | **TEST-008** | Tests | VariableMapperTests non testa Format round-trip | S |

**Effort:** S = 1-2h, M = 4-8h, L = 1-2 giorni

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
| 2026-03-25 | 📋 **T-002 pianificata** — Rimozione BoardType e link diretto Board→Dictionary (Domain v2). +6 issue refactoring (T-002, CORE-007, INFRA-008, SVC-011, GUI-008, TEST-009). 5 issue esistenti marcate come superseded (SVC-008, INFRA-007, TEST-007, SVC-004, GUI-007). Branch: `refactor/domain-v2-remove-boardtype`. Effort stimato: 15-20h |
| 2026-03-24 | 🔍 **Audit completo tutti i componenti**
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
