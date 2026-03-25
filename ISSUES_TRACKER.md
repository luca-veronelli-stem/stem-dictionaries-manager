# Stem.Dictionaries.Manager - Issue Tracker

> **Ultimo aggiornamento:** 2026-03-25

---

## Riepilogo Globale

| Componente | Aperte | Risolte | Totale |
|------------|--------|---------|--------|
| [Core](./Core/ISSUES.md) | 4 | 3 | 7 |
| [Infrastructure](./Infrastructure/ISSUES.md) | 4 | 4 | 8 |
| [Services](./Services/ISSUES.md) | 6 | 5 | 11 |
| [GUI.Windows](./GUI.Windows/ISSUES.md) | 3 | 5 | 8 |
| [Tests](./Tests/ISSUES.md) | 1 | 8 | 9 |
| **Trasversali** | **0** | **2** | **2** |
| **Totale** | **18** | **27** | **45** |

---

## Distribuzione per Priorità

| Priorità | Aperte | % |
|----------|--------|---|
| **Critica** | 0 | 0% |
| **Alta** | 0 | 0% |
| **Media** | 6 | 33% |
| **Bassa** | 12 | 67% |
| **Totale** | **18** | 100% |

```
Critica:     ░░░░░░░░░░░░░░░░░░░░  0
Alta:        ░░░░░░░░░░░░░░░░░░░░  0
Media:       ██████░░░░░░░░░░░░░░  6
Bassa:       ████████████░░░░░░░░ 12
```

---

## Issue Alta Priorità

| ID | Componente | Titolo | Status |
|----|------------|--------|--------|
| ~~T-002~~ | Trasversale | Rimozione BoardType e link diretto Board→Dictionary | ✅ **Risolto** |
| ~~CORE-007~~ | Core | Refactoring Core models per Domain v2 | ✅ **Risolto** |
| ~~INFRA-008~~ | Infrastructure | Refactoring Infrastructure per Domain v2 | ✅ **Risolto** |
| ~~SVC-011~~ | Services | Refactoring Services per Domain v2 | ✅ **Risolto** |
| ~~GUI-008~~ | GUI.Windows | Refactoring GUI per Domain v2 | ✅ **Risolto** |
| ~~TEST-009~~ | Tests | Aggiornamento test per Domain v2 | ✅ **Risolto** |
| ~~SVC-008~~ | Services | DictionaryService.AddAsync blocca Shared Peripheral | ✅ **Risolto (T-002)** |
| ~~INFRA-007~~ | Infrastructure | DatabaseSeeder.CreateBoard usa boardTypeId | ✅ **Risolto (T-002)** |
| ~~GUI-005~~ | GUI.Windows | NavigateToView async void senza error handling | ✅ **Risolto** |
| ~~TEST-007~~ | Tests | Manca test Shared Peripheral | ✅ **Risolto (T-002)** |
| ~~INFRA-001~~ | Infrastructure | DeleteAsync non solleva eccezione | ✅ **Risolto** |
| ~~T-001~~ | Trasversale | Dizionario Standard deve essere unico | ✅ **Risolto** |

✅ **0 issue alta priorità aperte.**

---

## Issue Trasversali (T-xxx)

| ID | Titolo | Priorità | Status | Componenti Coinvolti |
|----|--------|----------|--------|----------------------|
| ~~T-002~~ | Rimozione BoardType e link diretto Board→Dictionary | Alta | ✅ **Risolto** | Core, Infrastructure, Services, GUI.Windows, Tests |
| ~~T-001~~ | Dizionario Standard deve essere unico | Alta | ✅ **Risolto** | Services |

### T-002 — Rimozione BoardType e link diretto Board→Dictionary

**Descrizione:**  
Refactoring completo del domain model: rimozione entità `BoardType`, spostamento `FirmwareType` su `Board`, link diretto `Board→Dictionary` (nullable), sostituzione semantica 3-tuple `(DeviceType?, BoardType?)` con `IsStandard` flag e semantica derivata.

**Status:** ✅ **Risolto**  
**Branch:** `domain/ridefinizione-dominio-v2`  
**Data Apertura:** 2026-03-25  
**Data Risoluzione:** 2026-03-25

**Sub-issue (tutte risolte):**

| # | ID | Componente | Titolo | Status |
|---|-----|------------|--------|--------|
| 1 | ~~CORE-007~~ | Core | Refactoring Core models | ✅ Risolto |
| 2 | ~~INFRA-008~~ | Infrastructure | Refactoring Infrastructure | ✅ Risolto |
| 3 | ~~SVC-011~~ | Services | Refactoring Services | ✅ Risolto |
| 4 | ~~GUI-008~~ | GUI.Windows | Refactoring GUI | ✅ Risolto |
| 5 | ~~TEST-009~~ | Tests | Aggiornamento test | ✅ Risolto |

**Issue risolte automaticamente dal refactoring:**
- ~~SVC-008~~ (AddAsync blocca Shared Peripheral) ✅
- ~~INFRA-007~~ (DatabaseSeeder usa boardTypeId) ✅
- ~~TEST-007~~ (Manca test Shared Peripheral) ✅
- ~~SVC-004~~ (BoardMapper overload mancanti) ✅
- ~~GUI-007~~ (DictionaryListItem non mostra DeviceType) ✅

**Totale issue risolte da T-002: 11**

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

### Core (4 issue aperte, 3 risolte)

| ID | Titolo | Priorità | Categoria |
|----|--------|----------|-----------|
| ~~CORE-001~~ | ~~AuditEntityType contiene "Device" non esistente~~ | ~~Media~~ | ✅ **Risolto** |
| ~~CORE-002~~ | ~~Variable.Category deriva solo da AddressHigh == 0x00~~ | ~~Media~~ | ✅ **Risolto** |
| ~~CORE-007~~ | ~~Refactoring Core models per Domain v2 (T-002)~~ | ~~Alta~~ | ✅ **Risolto** |
| [CORE-006](./Core/ISSUES.md#core-006--dictionaryrestore-bypassa-validazione-unicità-indirizzi) | Dictionary.Restore bypassa validazione unicità indirizzi | Media | Bug |
| [CORE-003](./Core/ISSUES.md#core-003--dictionaryremovevariable-non-verifica-esistenza) | Dictionary.RemoveVariable non verifica esistenza | Bassa | API |
| [CORE-004](./Core/ISSUES.md#core-004--mancanza-di-metodi-update-sui-modelli) | Mancanza di metodi Update sui modelli | Bassa | API |
| [CORE-005](./Core/ISSUES.md#core-005--bitinterpretationvariableid-non-ha-validazione-positiva) | BitInterpretation.VariableId non ha validazione | Bassa | API |

### Infrastructure (4 issue aperte, 4 risolte)

| ID | Titolo | Priorità | Categoria |
|----|--------|----------|-----------|
| ~~INFRA-001~~ | ~~DeleteAsync non solleva eccezione~~ | ~~Alta~~ | ✅ **Risolto** |
| ~~INFRA-008~~ | ~~Refactoring Infrastructure per Domain v2 (T-002)~~ | ~~Alta~~ | ✅ **Risolto** |
| ~~INFRA-007~~ | ~~DatabaseSeeder.CreateBoard usa boardTypeId~~ | ~~Alta~~ | ✅ **Risolto (T-002)** |
| [INFRA-002](./Infrastructure/ISSUES.md#infra-002--getallasync-senza-paginazione-rischia-performance-issues) | GetAllAsync senza paginazione | Media | Performance |
| [INFRA-003](./Infrastructure/ISSUES.md#infra-003--designtimedbcontextfactory-ha-path-hardcoded-fragile) | DesignTimeDbContextFactory path fragile | Media | Manutenibilità |
| ~~INFRA-004~~ | ~~Mancano repository BitInterpretation/CommandDeviceState~~ | ~~Bassa~~ | ✅ **Risolto** (SVC-001) |
| [INFRA-005](./Infrastructure/ISSUES.md#infra-005--commandentityparametersjson-non-ha-conversione-json-tipizzata) | ParametersJson stringa grezza | Bassa | Design |
| [INFRA-006](./Infrastructure/ISSUES.md#infra-006--dictionaryrepositorygetbynameasync-non-normalizza-input) | GetByNameAsync non normalizza input | Bassa | Bug |

### Services (7 issue aperte, 4 risolte)

| ID | Titolo | Priorità | Categoria |
|----|--------|----------|-----------|
| ~~SVC-001~~ | ~~Services dipendono da AppDbContext~~ | ~~Media~~ | ✅ **Risolto** |
| ~~SVC-011~~ | ~~Refactoring Services per Domain v2 (T-002)~~ | ~~Alta~~ | ✅ **Risolto** |
| ~~SVC-008~~ | ~~DictionaryService.AddAsync blocca Shared Peripheral~~ | ~~Alta~~ | ✅ **Risolto (T-002)** |
| ~~SVC-004~~ | ~~BoardMapper overload mancanti~~ | ~~Bassa~~ | ✅ **Risolto (T-002)** |
| ~~SVC-009~~ | ~~VariableMapper.ToDomain non mappa Format~~ | ~~Media~~ | ✅ **Risolto** |
| [SVC-002](./Services/ISSUES.md#svc-002--manca-iauditservice-per-gestione-audit-trail) | Manca IAuditService per gestione audit trail | Alta | Feature |
| [SVC-003](./Services/ISSUES.md#svc-003--getallasync-senza-paginazione-nei-services) | GetAllAsync senza paginazione | Media | Performance |
| [SVC-005](./Services/ISSUES.md#svc-005--commandservicegetwithdevicestatesasync-non-espone-devicestates) | CommandService.GetWithDeviceStatesAsync non espone DeviceStates | Bassa | UX |
| [SVC-006](./Services/ISSUES.md#svc-006--manca-validazione-business-rules-centralizzata) | Manca validazione centralizzata | Bassa | Design |
| [SVC-007](./Services/ISSUES.md#svc-007--dependencyinjection-non-valida-prerequisiti) | DI non valida prerequisiti | Bassa | Robustezza |
| [SVC-010](./Services/ISSUES.md#svc-010--class1cs-placeholder-non-rimosso) | Class1.cs placeholder non rimosso | Bassa | Code Smell |

### GUI.Windows (3 issue aperte, 5 risolte)

| ID | Titolo | Priorità | Categoria |
|----|--------|----------|-----------|
| ~~GUI-001~~ | ~~Mancano ViewModels per ViewType dichiarate~~ | ~~Media~~ | ✅ **Risolto** |
| ~~GUI-004~~ | ~~Refactoring grafico completo e migrazione login~~ | ~~Media~~ | ✅ **Risolto** |
| ~~GUI-008~~ | ~~Refactoring GUI per Domain v2 (T-002)~~ | ~~Alta~~ | ✅ **Risolto** |
| ~~GUI-007~~ | ~~DictionaryListItem non mostra DeviceType~~ | ~~Media~~ | ✅ **Risolto (T-002)** |
| ~~GUI-005~~ | ~~NavigateToView async void senza error handling~~ | ~~Alta~~ | ✅ **Risolto** |
| [GUI-006](./GUI.Windows/ISSUES.md#gui-006--loginviewmodel-registrato-due-volte-nel-di-container) | LoginViewModel registrato due volte nel DI | Media | Code Smell |
| [GUI-002](./GUI.Windows/ISSUES.md#gui-002--appservices-è-static-e-impedisce-testabilità) | App.Services static impedisce testabilità | Bassa | Design |
| [GUI-003](./GUI.Windows/ISSUES.md#gui-003--dialogservice-usa-messagebox-sincrono-wrappato-in-task) | DialogService finto async | Bassa | Design |

### Tests (2 issue aperte, 7 risolte)

| ID | Titolo | Priorità | Categoria |
|----|--------|----------|-----------|
| ~~TEST-001~~ | ~~Mancano test BoardRepository e CommandRepository~~ | ~~Media~~ | ✅ **Risolto** |
| ~~TEST-002~~ | ~~Mancano test BoardTypeRepository~~ | ~~Media~~ | ✅ **Risolto** |
| ~~TEST-003~~ | ~~Uso .Wait() bloccante~~ | ~~Media~~ | ✅ **Risolto** |
| ~~TEST-004~~ | ~~Mancano test DI~~ | ~~Bassa~~ | ✅ **Risolto** |
| ~~TEST-005~~ | ~~Mancano test scenari update/delete~~ | ~~Bassa~~ | ✅ **Risolto** |
| ~~TEST-009~~ | ~~Aggiornamento test per Domain v2 (T-002)~~ | ~~Alta~~ | ✅ **Risolto** |
| ~~TEST-007~~ | ~~Manca test Shared Peripheral~~ | ~~Alta~~ | ✅ **Risolto (T-002)** |
| ~~TEST-008~~ | ~~VariableMapperTests non testa Format round-trip~~ | ~~Media~~ | ✅ **Risolto** |
| [TEST-006]

---

## Roadmap: Refactoring Domain v2 (T-002) — ✅ COMPLETATA

| # | ID | Componente | Titolo | Status |
|---|-----|------------|--------|--------|
| 1 | ~~CORE-007~~ | Core | Refactoring Core models | ✅ Risolto |
| 2 | ~~INFRA-008~~ | Infrastructure | Refactoring Infrastructure + Migration | ✅ Risolto |
| 3 | ~~SVC-011~~ | Services | Refactoring Services + Mappers | ✅ Risolto |
| 4 | ~~GUI-008~~ | GUI.Windows | Refactoring ViewModels + Views | ✅ Risolto |
| 5 | ~~TEST-009~~ | Tests | Aggiornamento/riscrittura test | ✅ Risolto |

**Branch:** `domain/ridefinizione-dominio-v2`  
**Data completamento:** 2026-03-25  
**Issue risolte automaticamente:** SVC-008, INFRA-007, TEST-007, SVC-004, GUI-007

## Issue da Risolvere (prossime)

| # | ID | Componente | Titolo | Effort |
|---|-----|------------|--------|--------|
| 1 | **SVC-002** | Services | Manca IAuditService | M |
| 2 | **CORE-006** | Core | Dictionary.Restore bypassa validazione | S |

**Effort:** S = 1-2h, M = 4-8h, L = 1-2 giorni

---

## Copertura Test Attuale

| Componente | Unit | Integration | Copertura |
|------------|------|-------------|-----------|
| Core/Enums (6) | ✅ 21 | - | 100% |
| Core/Models (10) | ✅ 107 | - | 100% |
| Infrastructure/Repositories (10) | - | ✅ 96 | ~98% |
| Infrastructure/DI | ✅ 14 | - | 100% |
| Services/Mapping (9) | ✅ 89 | - | ~100% |
| Services (5) | - | ✅ 98 | ~95% |
| Services/DI | ✅ 10 | - | 100% |
| GUI.Windows/ViewModels (15) | ✅ 254 | ✅ 11 | ~90% |
| GUI.Windows/Services (3) | ✅ 15 | - | ~70% |
| GUI.Windows/Converters (2) | ✅ 20 | - | 100% |
| GUI.Windows/DI | ✅ 22 | - | 100% |

**Totale test:** ~490 CI (net10.0) / 1254 Windows (net10.0-windows)

---

## Metriche Qualità

| Aspetto | Stato | Note |
|---------|-------|------|
| **Architecture** | ✅ 98% | Domain v2 completo, VariableDeviceState aggiunto |
| **Thread Safety** | ✅ 95% | Modelli immutabili |
| **Input Validation** | ✅ 85% | BR-011 (VariableDeviceState), CORE-006, CORE-005 residui |
| **Data Integrity** | ✅ 95% | SVC-009 risolta |
| **Performance** | ⚠️ 70% | GetAllAsync senza paginazione (INFRA-002, SVC-003) |
| **Resilience** | ✅ 90% | GUI-005 risolta, navigazione protetta |
| **Code Consistency** | ✅ 90% | INFRA-006, GUI-006, SVC-010 residui |
| **Test Coverage** | ✅ 95% | 1254 test, TEST-008 risolta |

---

## Issue per Categoria

| Categoria | Count | Issue |
|-----------|-------|-------|
| **Bug** | 2 | CORE-006, INFRA-006 |
| **Design** | 4 | SVC-005, SVC-006, INFRA-005, GUI-002 |
| **UX** | 1 | GUI-003 |
| **Performance** | 2 | INFRA-002, SVC-003 |
| **Copertura** | 0 | - |
| **API** | 3 | CORE-003, CORE-004, CORE-005 |
| **Manutenibilità** | 2 | INFRA-003, TEST-006 |
| **Code Smell** | 2 | SVC-010, GUI-006 |
| **Feature** | 1 | SVC-002 |
| **Robustezza** | 1 | SVC-007 |

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
| 2026-03-25 | ✅ **SVC-009 + TEST-008 risolte** — VariableMapper ora mappa Format in tutte le direzioni (ToDomain/ToEntity/UpdateEntity). +4 assert Format nei test esistenti. 18 aperte, 27 risolte. Branch: `fix/svc-009` |
| 2026-03-25 | ✅ **GUI-005 risolta**
| 2026-03-25 | ✅ **T-002 completata** — Domain v2 implementato: BoardType rimosso, Board→Dictionary diretto, IsStandard flag, semantica derivata. 11 issue risolte (T-002, CORE-007, INFRA-008, SVC-011, GUI-008, TEST-009, SVC-008, INFRA-007, TEST-007, SVC-004, GUI-007). +VariableDeviceState (BR-009/010/011). 1252 test verdi. Branch: `domain/ridefinizione-dominio-v2` |
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
