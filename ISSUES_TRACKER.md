# Stem.Dictionaries.Manager - Issue Tracker

> **Ultimo aggiornamento:** 2026-03-19

---

## Riepilogo Globale

| Componente | Aperte | Risolte | Totale |
|------------|--------|---------|--------|
| [Core](./Core/ISSUES.md) | 3 | 2 | 5 |
| [Infrastructure](./Infrastructure/ISSUES.md) | 4 | 2 | 6 |
| [Services](./Services/ISSUES.md) | 6 | 1 | 7 |
| [GUI.Windows](./GUI.Windows/ISSUES.md) | 2 | 1 | 3 |
| [Tests](./Tests/ISSUES.md) | 1 | 5 | 6 |
| **Trasversali** | **0** | **1** | **1** |
| **Totale** | **16** | **12** | **28** |

---

## Distribuzione per Priorità

| Priorità | Aperte | % |
|----------|--------|---|
| **Critica** | 0 | 0% |
| **Alta** | 0 | 0% |
| **Media** | 4 | 25% |
| **Bassa** | 12 | 75% |
| **Totale** | **16** | 100% |

```
Critica:     ░░░░░░░░░░░░░░░░░░░░  0
Alta:        ░░░░░░░░░░░░░░░░░░░░  0  ✅ T-001 risolta
Media:       ██████░░░░░░░░░░░░░░  6
Bassa:       ████████████░░░░░░░░ 12
```

---

## Issue Alta Priorità

| ID | Componente | Titolo | Status |
|----|------------|--------|--------|
| ~~INFRA-001~~ | Infrastructure | DeleteAsync non solleva eccezione | ✅ **Risolto** |
| ~~T-001~~ | Trasversale | Dizionario Standard deve essere unico | ✅ **Risolto** |

*0 issue alta priorità aperte.* ✅

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

### Core (3 issue aperte, 2 risolte)

| ID | Titolo | Priorità | Categoria |
|----|--------|----------|-----------|
| ~~CORE-001~~ | ~~AuditEntityType contiene "Device" non esistente~~ | ~~Media~~ | ✅ **Risolto** |
| ~~CORE-002~~ | ~~Variable.Category deriva solo da AddressHigh == 0x00~~ | ~~Media~~ | ✅ **Risolto** |
| [CORE-003](./Core/ISSUES.md#core-003--dictionaryremovevariable-non-verifica-esistenza) | Dictionary.RemoveVariable non verifica esistenza | Bassa | API |
| [CORE-004](./Core/ISSUES.md#core-004--mancanza-di-metodi-update-sui-modelli) | Mancanza di metodi Update sui modelli | Bassa | API |
| [CORE-005](./Core/ISSUES.md#core-005--bitinterpretationvariableid-non-ha-validazione-positiva) | BitInterpretation.VariableId non ha validazione | Bassa | API |

### Infrastructure (4 issue aperte, 2 risolte)

| ID | Titolo | Priorità | Categoria |
|----|--------|----------|-----------|
| ~~INFRA-001~~ | ~~DeleteAsync non solleva eccezione~~ | ~~Alta~~ | ✅ **Risolto** |
| [INFRA-002](./Infrastructure/ISSUES.md#infra-002--getallasync-senza-paginazione-rischia-performance-issues) | GetAllAsync senza paginazione | Media | Performance |
| [INFRA-003](./Infrastructure/ISSUES.md#infra-003--designtimedbcontextfactory-ha-path-hardcoded-fragile) | DesignTimeDbContextFactory path fragile | Media | Manutenibilità |
| ~~INFRA-004~~ | ~~Mancano repository BitInterpretation/CommandDeviceState~~ | ~~Bassa~~ | ✅ **Risolto** (SVC-001) |
| [INFRA-005](./Infrastructure/ISSUES.md#infra-005--commandentityparametersjson-non-ha-conversione-json-tipizzata) | ParametersJson stringa grezza | Bassa | Design |
| [INFRA-006](./Infrastructure/ISSUES.md#infra-006--dictionaryrepositorygetbynameasync-non-normalizza-input) | GetByNameAsync non normalizza input | Bassa | Bug |

### Tests (1 issue aperta, 5 risolte)

| ID | Titolo | Priorità | Categoria |
|----|--------|----------|-----------|
| ~~TEST-001~~ | ~~Mancano test BoardRepository e CommandRepository~~ | ~~Media~~ | ✅ **Risolto** |
| ~~TEST-002~~ | ~~Mancano test BoardTypeRepository~~ | ~~Media~~ | ✅ **Risolto** |
| ~~TEST-003~~ | ~~Uso .Wait() bloccante~~ | ~~Media~~ | ✅ **Risolto** |
| ~~TEST-004~~ | ~~Mancano test DI~~ | ~~Bassa~~ | ✅ **Risolto** |
| ~~TEST-005~~ | ~~Mancano test scenari update/delete~~ | ~~Bassa~~ | ✅ **Risolto** |
| [TEST-006](./Tests/ISSUES.md#test-006--magic-strings-ripetute-nei-test) | Magic strings ripetute | Bassa | Manutenibilità |

### Services (6 issue aperte, 1 risolta)

| ID | Titolo | Priorità | Categoria |
|----|--------|----------|-----------|
| ~~SVC-001~~ | ~~Services dipendono da AppDbContext~~ | ~~Media~~ | ✅ **Risolto** |
| [SVC-002](./Services/ISSUES.md#svc-002--manca-iauditservice-per-gestione-audit-trail) | Manca IAuditService | Media | Feature |
| [SVC-003](./Services/ISSUES.md#svc-003--getallasync-senza-paginazione-nei-services) | GetAllAsync senza paginazione | Media | Performance |
| [SVC-004](./Services/ISSUES.md#svc-004--mancano-mapper-per-boardmapper-con-overload) | BoardMapper overload mancanti | Bassa | Code Smell |
| [SVC-005](./Services/ISSUES.md#svc-005--commandservicegetwithdevicestatesasync-non-espone-devicestates) | GetWithDeviceStates non espone stati | Bassa | Design |
| [SVC-006](./Services/ISSUES.md#svc-006--manca-validazione-business-rules-centralizzata) | Manca validazione centralizzata | Bassa | Design |
| [SVC-007](./Services/ISSUES.md#svc-007--dependencyinjection-non-valida-prerequisiti) | DI non valida prerequisiti | Bassa | Robustezza |

### GUI.Windows (2 issue aperte, 1 risolta)

| ID | Titolo | Priorità | Categoria |
|----|--------|----------|-----------|
| ~~GUI-001~~ | ~~Mancano ViewModels per ViewType dichiarate~~ | ~~Media~~ | ✅ **Risolto** |
| [GUI-002](./GUI.Windows/ISSUES.md#gui-002--appservices-è-static-e-impedisce-testabilità) | App.Services static impedisce testabilità | Bassa | Design |
| [GUI-003](./GUI.Windows/ISSUES.md#gui-003--dialogservice-usa-messagebox-sincrono-wrappato-in-task) | DialogService finto async | Bassa | Design |

---

## Top 5 Issue da Risolvere

| # | ID | Componente | Titolo | Effort |
|---|-----|------------|--------|--------|
| 1 | **SVC-003** | Services | GetAllAsync senza paginazione | M |
| 2 | **INFRA-002** | Infrastructure | GetAllAsync senza paginazione | M |
| 3 | **SVC-002** | Services | Manca IAuditService | M |
| 4 | **INFRA-003** | Infrastructure | DesignTimeDbContextFactory path fragile | S |
| 5 | **CORE-003** | Core | Dictionary.RemoveVariable non verifica esistenza | S |

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
| GUI.Windows/ViewModels (11) | ✅ 205 | ✅ 10 | ~95% |
| GUI.Windows/Services (4) | ✅ 20 | - | ~85% |
| GUI.Windows/Converters (2) | ✅ 18 | - | 100% |
| GUI.Windows/DI | ✅ 23 | - | 100% |

**Totale test:** ~440 CI (net10.0) / 1111 Windows (net10.0-windows)

---

## Metriche Qualità

| Aspetto | Stato | Note |
|---------|-------|------|
| **Architecture** | ✅ 95% | Layer separation corretta, Services decoupled |
| **Thread Safety** | ✅ 95% | Modelli immutabili |
| **Input Validation** | ⚠️ 75% | Alcuni edge-case (CORE-002, CORE-005) |
| **Performance** | ⚠️ 70% | GetAllAsync senza paginazione (INFRA-002, SVC-003) |
| **Code Consistency** | ✅ 85% | Poche inconsistenze (INFRA-006) |
| **Test Coverage** | ✅ 90% | 1111 test Windows, copertura ~95% |

---

## Issue per Categoria

| Categoria | Count | Issue |
|-----------|-------|-------|
| **API** | 3 | CORE-003, CORE-004, CORE-005 |
| **Design** | 3 | INFRA-005, GUI-002, GUI-003 |
| **Copertura** | 0 | *(tutte risolte)* |
| **Performance** | 2 | INFRA-002, SVC-003 |
| **Manutenibilità** | 2 | INFRA-003, TEST-006 |
| **Bug** | 1 | INFRA-006 |
| **Feature** | 1 | SVC-002 |

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
