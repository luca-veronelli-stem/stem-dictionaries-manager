# Stem.Dictionaries.Manager - Issue Tracker

> **Ultimo aggiornamento:** 2026-04-03

---

## Riepilogo Globale

| Componente | Aperte | Risolte | Totale |
|------------|--------|---------|--------|
| [Core](./Core/ISSUES.md) | 3 | 5 | 8 |
| [Infrastructure](./Infrastructure/ISSUES.md) | 2 | 7 | 9 |
| [Services](./Services/ISSUES.md) | 5 | 7 | 12 |
| [GUI.Windows](./GUI.Windows/ISSUES.md) | 3 | 6 | 9 |
| [Tests](./Tests/ISSUES.md) | 2 | 8 | 10 |
| **Trasversali** | **4** | **2** | **6** |
| **Totale** | **19** | **35** | **54** |

---

## Distribuzione per Priorità

| Priorità | Aperte | % |
|----------|--------|---|
| **Critica** | 0 | 0% |
| **Alta** | 1 | 5% |
| **Media** | 1 | 5% |
| **Bassa** | 17 | 89% |
| **Totale** | **19** | 100% |

```
Critica:     ░░░░░░░░░░░░░░░░░░░░  0
Alta:        █░░░░░░░░░░░░░░░░░░░  1  (T-006)
Media:       █░░░░░░░░░░░░░░░░░░░  1
Bassa:       █████████████████░░░ 17
```

---

## Issue Alta Priorità

| ID | Componente | Titolo | Status |
|----|------------|--------|--------|
| **T-006** | **Trasversale** | **StandardVariableOverride per-dizionario (Domain v7)** | ⚠️ **Aperto** |
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

⚠️ **1 issue alta priorità aperta: T-006 (Domain v7)**

---

## Issue Trasversali (T-xxx)

| ID | Titolo | Priorità | Status | Componenti Coinvolti |
|----|--------|----------|--------|----------------------|
| [T-006](#t-006--standardvariableoverride-per-dizionario-domain-v7) | StandardVariableOverride per-dizionario (Domain v7) | **Alta** | **Aperto** | Core, Infrastructure, Services, GUI.Windows, Tests |
| [T-005](#t-005--rendere-espliciti-parametri-semantici-nei-domain-models) | Rendere espliciti parametri semantici nei domain models | Bassa | Aperto | Core, Tests |
| [T-004](#t-004--aggiungere-db-constraints-per-regole-di-business) | Aggiungere DB constraints per regole di business | Bassa | Aperto | Infrastructure |
| [T-003](#t-003--aggiungere-logging-infrastructure) | Aggiungere logging infrastructure | Bassa | Aperto | Infrastructure, Services, GUI.Windows |
| ~~T-002~~ | Rimozione BoardType e link diretto Board→Dictionary | Alta | ✅ **Risolto** | Core, Infrastructure, Services, GUI.Windows, Tests |
| ~~T-001~~ | Dizionario Standard deve essere unico | Alta | ✅ **Risolto** | Services |

### T-006 — StandardVariableOverride per-dizionario (Domain v7)

**Descrizione:**  
Refactoring completo del domain model per correggere la semantica delle variabili standard. Ogni dizionario non-standard **eredita automaticamente** tutte le variabili del template Standard (indirizzi 0x00xx sempre riservati). L'override è **per-dizionario**, non per-device.

**Problema risolto:**  
Le variabili standard non sono un blocco separato, ma fanno parte di ogni dizionario. Ogni scheda implementa tutte le variabili standard, ma può modificare solo `IsEnabled`, `Description` e `BitInterpretations` per il suo contesto.

**Status:** Aperto  
**Priorità:** Alta — cambiamento di dominio fondamentale  
**Branch proposto:** `domain/domain-v7`  
**Data Apertura:** 2026-03-30  
**Effort stimato:** L (8-16h)

**Cambiamenti principali:**

| Aspetto | Prima (v6) | Dopo (v7) |
|---------|------------|-----------|
| Override variabili standard | `VariableDeviceState` (per-device) | `StandardVariableOverride` (per-dizionario) |
| BitInterpretation scope | `DeviceId?` (per-device) | `DictionaryId?` (per-dizionario) |
| Eredità variabili standard | Dizionario Standard "visibile" da tutti | Ogni dizionario eredita automaticamente le 24 variabili standard |
| DeviceVariablesView | Presente | **Rimossa** — override dentro DictionaryEditView |
| AuditEntityType | 6 valori | 7 valori (+StandardVariableOverride, -VariableDeviceState) |

**Sub-issue:**

| # | ID | Componente | Titolo | Effort |
|---|-----|------------|--------|--------|
| 1 | CORE-008 | Core | Creare StandardVariableOverride, rimuovere VariableDeviceState | S |
| 2 | INFRA-009 | Infrastructure | Entity + Repository + Migration per Domain v7 | M |
| 3 | SVC-012 | Services | Mapper + Service per StandardVariableOverride | M |
| 4 | GUI-009 | GUI.Windows | Rimuovere DeviceVariables, aggiornare DictionaryEdit | M |
| 5 | TEST-010 | Tests | Aggiornare/riscrittura test per Domain v7 | M |

**File da CREARE:**
- `Core/Models/StandardVariableOverride.cs`
- `Infrastructure/Entities/StandardVariableOverrideEntity.cs`
- `Infrastructure/Interfaces/IStandardVariableOverrideRepository.cs`
- `Infrastructure/Repositories/StandardVariableOverrideRepository.cs`
- `Services/Mapping/StandardVariableOverrideMapper.cs`

**File da ELIMINARE:**
- `Core/Models/VariableDeviceState.cs`
- `Infrastructure/Entities/VariableDeviceStateEntity.cs`
- `Infrastructure/Interfaces/IVariableDeviceStateRepository.cs`
- `Infrastructure/Repositories/VariableDeviceStateRepository.cs`
- `Services/Mapping/VariableDeviceStateMapper.cs`
- `GUI.Windows/ViewModels/DeviceVariablesViewModel.cs`
- `GUI.Windows/ViewModels/VariableDeviceItem.cs`
- `GUI.Windows/Views/DeviceVariablesView.xaml(.cs)`

**File da MODIFICARE (principali):**
- `Core/Enums/AuditEntityType.cs`
- `Infrastructure/Entities/BitInterpretationEntity.cs` (DeviceId → DictionaryId)
- `Infrastructure/AppDbContext.cs`
- `Infrastructure/DatabaseSeeder.cs`
- `Services/VariableService.cs` + `IVariableService.cs`
- `GUI.Windows/ViewModels/DictionaryEditViewModel.cs`
- `GUI.Windows/Abstractions/INavigationService.cs` (rimuovere DeviceVariables da ViewType)
- ~15-20 file test

**Business Rules aggiornate:**
- BR-009 (v7): Stato effettivo variabile standard → per-dizionario
- BR-010 (v7): Unicità override → (DictionaryId, StandardVariableId)
- BR-011 (v7): Coerenza override → per-dizionario
- BR-017 (v7): Unicità BitInterpretation → (VariableId, DictionaryId, WordIndex, BitIndex)
- BR-018 (v7): Risoluzione BitInterpretation → per-dizionario > template
- BR-020 (v7, NUOVA): Descrizione effettiva → override.Description > template

**Benefici Attesi:**
- Semantica corretta: ogni dizionario eredita le variabili standard
- Modello più semplice: un solo punto di override (dizionario)
- GUI unificata: override variabili standard dentro DictionaryEditView
- Eliminazione DeviceVariablesView (view non più necessaria)

---

**Descrizione:**  
Diversi constructor e factory method `Restore` nei domain models hanno parametri opzionali con default che nascondono scelte semantiche di dominio. Il pattern è stato corretto per `BitInterpretation.DeviceId` (SESSION_037), ma rimane in altri model. L'obiettivo è rimuovere i default dove il valore ha un significato di dominio, forzando il chiamante a dichiarare sempre l'intento.

**Status:** Aperto  
**Priorità:** Bassa  
**Data Apertura:** 2026-03-30

**Precedente:**  
`BitInterpretation(deviceId = null)` → reso obbligatorio in SESSION_037. Il default nascondeva "interpretazione comune a tutti i device". In v7, `DeviceId` → `DictionaryId` (T-006).

**Parametri da valutare:**

| # | Model | Parametro | Default | Rischio | Note |
|---|-------|-----------|---------|---------|------|
| 1 | Board | `machineCode` | `= 0` | Medio | 0 è illegale per BR-014. In Restore (da DB) potrebbe mascherare un mapper incompleto |
| 2 | Board.Restore | `machineCode` | `= 0` | Medio | Stesso problema — dato denormalizzato da Device |
| 3 | CommandDeviceState | `isEnabled` | `= true` | Basso | Restore esplicito, constructor usato solo nei test |
| ~~4~~ | ~~VariableDeviceState~~ | ~~`isEnabled`~~ | ~~`= true`~~ | ~~Basso~~ | ~~Risolto da T-006 (entity rimossa)~~ |
| 5 | Command | `isResponse` | `= false` | Basso | Restore esplicito, mapper passa il valore |
| 6 | Dictionary | `isStandard` | `= false` | Basso | Restore esplicito, GUI passa esplicitamente |
| 7 | Variable | `isEnabled` | `= true` | Basso | Restore completamente esplicito |

**Criteri di priorità:**
- **Medio**: il default è un valore illegale o il Restore lo ha opzionale
- **Basso**: il Restore è già esplicito e il mapper passa sempre il valore

**Effort stimato:** S-M (2-4h) — rimozione default + aggiornamento call site nei test

**Benefici Attesi:**
- Coerenza con il pattern già applicato su `BitInterpretation.DeviceId`
- Impossibilità di dimenticare un parametro semantico
- Codice auto-documentante: ogni call site dichiara l'intento

---

### T-004 — Aggiungere DB constraints per regole di business

**Descrizione:**  
Diverse regole di business sono attualmente protette solo a livello codice (Core constructor, Services). Aggiungere guard a livello DB (unique indexes, partial indexes, CHECK constraints) come ultima trincea contro dati corrotti.

**Status:** Aperto  
**Priorità:** Bassa  
**Data Apertura:** 2026-03-27

**Componenti Coinvolti:**
- Infrastructure (AppDbContext.OnModelCreating, nuova migration)

**Constraint da aggiungere:**

| # | Regola | Tipo DB | EF Core API | Entità |
|---|--------|---------|-------------|--------|
| 1 | BR-004: Max 1 dizionario Standard | Partial unique index `(IsStandard) WHERE IsStandard = 1` | `HasIndex().IsUnique().HasFilter()` | Dictionary |
| 2 | BR-005: Max 1 primary board per device | Partial unique index `(DeviceId) WHERE IsPrimary = 1` | `HasIndex().IsUnique().HasFilter()` | Board |
| 3 | BR-014: MachineCode > 0 | CHECK constraint | `HasCheckConstraint()` | Device |
| 4 | BR-016: Command.Name univoco | Unique index (**oggi manca!**) | `HasIndex().IsUnique()` | Command |
| 5 | BitIndex ≥ 0 | CHECK constraint | `HasCheckConstraint()` | BitInterpretation |
| 6 | WordIndex ≥ 0 | CHECK constraint | `HasCheckConstraint()` | BitInterpretation |
| 7 | BR-010: Unicità override standard | Unique index `(DictionaryId, StandardVariableId)` | `HasIndex().IsUnique()` | StandardVariableOverride |

**Esclusi (restano solo nel codice):**

| Regola | Motivazione |
|--------|-------------|
| BR-011: Override coerenza (Variable.IsEnabled + Override) | Cross-entity, servirebbe trigger SQL — troppo complesso |
| BR-015: MachineCode ≠ 6 (BLE reserved) | Regola business che potrebbe cambiare — meglio flessibile |

**Effort stimato:** S (1-2h) — solo configurazione EF Core + migration

**Benefici Attesi:**
- Protezione DB come ultima trincea contro dati corrotti
- Impossibilità di bypassare vincoli anche con bug nel codice
- Allineamento DB ↔ regole di business
- Costo zero (solo configurazione)

---

### T-003 — Aggiungere logging infrastructure

**Descrizione:**  
L'applicazione non ha una struttura di logging. Attualmente viene usato solo `Debug.WriteLineIf` per warning in sviluppo. Serve aggiungere `ILogger<T>` (già disponibile via `Host.CreateDefaultBuilder()`) per:
- Troubleshooting in produzione
- Monitoraggio performance
- Tracciamento errori

**Status:** Aperto  
**Priorità:** Bassa  
**Data Apertura:** 2026-03-25

**Componenti Coinvolti:**
- Infrastructure (RepositoryBase, AppDbContext)
- Services (tutti i Service)
- GUI.Windows (ViewModels critici, App.xaml.cs)

**Soluzione Proposta:**

1. **Configurare logging in App.xaml.cs:**
```csharp
Host.CreateDefaultBuilder()
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddDebug();  // Output Window in VS
        logging.AddFile("logs/app-{Date}.log");  // File in AppData (opzionale)
    })
```

2. **Iniettare `ILogger<T>` dove serve:**
```csharp
public class DictionaryService : IDictionaryService
{
    private readonly ILogger<DictionaryService> _logger;

    public DictionaryService(..., ILogger<DictionaryService> logger)
    {
        _logger = logger;
    }

    public async Task<Dictionary> AddAsync(...)
    {
        _logger.LogInformation("Adding dictionary {Name}", dictionary.Name);
        // ...
    }
}
```

3. **Sostituire `Debug.WriteLineIf` con `ILogger`:**
```csharp
// Prima (INFRA-002)
Debug.WriteLineIf(result.Count > 500, "[PERFORMANCE WARNING]...");

// Dopo
_logger.LogWarning("GetAllAsync returned {Count} records for {Entity}", result.Count, typeof(TEntity).Name);
```

**Sub-issue potenziali:**
| # | Componente | Titolo | Effort |
|---|------------|--------|--------|
| 1 | Infrastructure | Aggiungere ILogger a RepositoryBase | S |
| 2 | Services | Aggiungere ILogger a tutti i Service | S |
| 3 | GUI.Windows | Configurare logging in App.xaml.cs | S |
| 4 | GUI.Windows | Aggiungere ILogger a MainViewModel | S |

**Effort totale stimato:** M (4-8h)

**Benefici Attesi:**
- Troubleshooting più semplice in produzione
- Monitoraggio centralizzato
- Sostituzione Debug.WriteLineIf con logging strutturato
- Preparazione per futuro Azure Application Insights

---

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

### Core (3 issue aperte, 5 risolte)

| ID | Titolo | Priorità | Categoria |
|----|--------|----------|-----------|
| ~~CORE-008~~ | ~~Creare StandardVariableOverride, rimuovere VariableDeviceState (T-006)~~ | ~~Alta~~ | ✅ **Risolto** |
| ~~CORE-001~~ | ~~AuditEntityType contiene "Device" non esistente~~ | ~~Media~~ | ✅ **Risolto** |
| ~~CORE-002~~ | ~~Variable.Category deriva solo da AddressHigh == 0x00~~ | ~~Media~~ | ✅ **Risolto** |
| ~~CORE-007~~ | ~~Refactoring Core models per Domain v2 (T-002)~~ | ~~Alta~~ | ✅ **Risolto** |
| ~~CORE-006~~ | ~~Dictionary.Restore bypassa validazione unicità indirizzi~~ | ~~Media~~ | ✅ **Risolto** |
| [CORE-003](./Core/ISSUES.md#core-003--dictionaryremovevariable-non-verifica-esistenza) | Dictionary.RemoveVariable non verifica esistenza | Bassa | Bug |
| [CORE-004](./Core/ISSUES.md#core-004--mancanza-di-metodi-update-sui-modelli) | Mancanza di metodi Update sui modelli | Bassa | API |
| [CORE-005](./Core/ISSUES.md#core-005--bitinterpretationvariableid-non-ha-validazione-positiva) | BitInterpretation.VariableId non ha validazione | Bassa | API |

### Infrastructure (2 issue aperte, 7 risolte)

| ID | Titolo | Priorità | Categoria |
|----|--------|----------|-----------|
| ~~INFRA-009~~ | ~~Entity + Repository + Migration per Domain v7 (T-006)~~ | ~~Alta~~ | ✅ **Risolto** |
| ~~INFRA-001~~ | ~~DeleteAsync non solleva eccezione~~ | ~~Alta~~ | ✅ **Risolto** |
| ~~INFRA-008~~ | ~~Refactoring Infrastructure per Domain v2 (T-002)~~ | ~~Alta~~ | ✅ **Risolto** |
| ~~INFRA-007~~ | ~~DatabaseSeeder.CreateBoard usa boardTypeId~~ | ~~Alta~~ | ✅ **Risolto (T-002)** |
| ~~INFRA-002~~ | ~~GetAllAsync senza paginazione~~ | ~~Media~~ | ✅ **Risolto** |
| ~~INFRA-003~~ | ~~DesignTimeDbContextFactory path fragile~~ | ~~Media~~ | ✅ **Risolto** |
| ~~INFRA-004~~ | ~~Mancano repository per BitInterpretation e CommandDeviceState~~ | ~~Media~~ | ✅ **Risolto** |
| [INFRA-005](./Infrastructure/ISSUES.md#infra-005--commandentityparametersjson-non-ha-conversione-json-tipizzata) | ParametersJson stringa grezza | Bassa | Design |
| [INFRA-006](./Infrastructure/ISSUES.md#infra-006--dictionaryrepositorygetbynameasync-non-normalizza-input) | GetByNameAsync non normalizza input | Bassa | Bug |

### Services (5 issue aperte, 7 risolte)

| ID | Titolo | Priorità | Categoria |
|----|--------|----------|-----------|
| **SVC-012** | **Mapper + Service per StandardVariableOverride (T-006)** | **Alta** | **Refactoring** |
| ~~SVC-001~~ | ~~Services dipendono da AppDbContext~~ | ~~Media~~ | ✅ **Risolto** |
| ~~SVC-011~~ | ~~Refactoring Services per Domain v2 (T-002)~~ | ~~Alta~~ | ✅ **Risolto** |
| ~~SVC-008~~ | ~~DictionaryService.AddAsync blocca Shared Peripheral~~ | ~~Alta~~ | ✅ **Risolto (T-002)** |
| ~~SVC-004~~ | ~~BoardMapper overload mancanti~~ | ~~Bassa~~ | ✅ **Risolto (T-002)** |
| ~~SVC-009~~ | ~~VariableMapper.ToDomain non mappa Format~~ | ~~Media~~ | ✅ **Risolto** |
| ~~SVC-003~~ | ~~GetAllAsync senza paginazione~~ | ~~Media~~ | ⚪ **Wontfix** |
| [SVC-002](./Services/ISSUES.md#svc-002--manca-iauditservice-per-gestione-audit-trail) | Manca IAuditService per gestione audit trail | Alta | Feature |
| [SVC-005](./Services/ISSUES.md#svc-005--commandservicegetwithdevicestatesasync-non-espone-devicestates) | CommandService.GetWithDeviceStatesAsync non espone DeviceStates | Bassa | UX |
| [SVC-006](./Services/ISSUES.md#svc-006--manca-validazione-business-rules-centralizzata) | Manca validazione centralizzata | Bassa | Design |
| [SVC-007](./Services/ISSUES.md#svc-007--dependencyinjection-non-valida-prerequisiti) | DI non valida prerequisiti | Bassa | Robustezza |
| ~~SVC-010~~ | ~~Class1.cs placeholder non rimosso~~ | ~~Bassa~~ | ✅ **Risolto** |

### GUI.Windows (3 issue aperte, 6 risolte)

| ID | Titolo | Priorità | Categoria |
|----|--------|----------|-----------|
| **GUI-009** | **Rimuovere DeviceVariables, aggiornare DictionaryEdit (T-006)** | **Alta** | **Refactoring** |
| ~~GUI-001~~ | ~~Mancano ViewModels per ViewType dichiarate~~ | ~~Media~~ | ✅ **Risolto** |
| ~~GUI-004~~ | ~~Refactoring grafico completo e migrazione login~~ | ~~Media~~ | ✅ **Risolto** |
| ~~GUI-008~~ | ~~Refactoring GUI per Domain v2 (T-002)~~ | ~~Alta~~ | ✅ **Risolto** |
| ~~GUI-007~~ | ~~DictionaryListItem non mostra DeviceType~~ | ~~Media~~ | ✅ **Risolto (T-002)** |
| ~~GUI-005~~ | ~~NavigateToView async void senza error handling~~ | ~~Alta~~ | ✅ **Risolto** |
| ~~GUI-006~~ | ~~LoginViewModel registrato due volte nel DI~~ | ~~Media~~ | ✅ **Risolto** |
| [GUI-002](./GUI.Windows/ISSUES.md#gui-002--appservices-è-static-e-impedisce-testabilità) | AppServices è static e impedisce testabilità | Media | UX |
| [GUI-003](./GUI.Windows/ISSUES.md#gui-003--dialogservice-usa-messagebox-sincrono-wrappato-in-task) | DialogService finto async | Bassa | Design |

### Tests (2 issue aperte, 8 risolte)

| ID | Titolo | Priorità | Categoria |
|----|--------|----------|-----------|
| **TEST-010** | **Aggiornare/riscrittura test per Domain v7 (T-006)** | **Alta** | **Copertura** |
| ~~TEST-001~~ | ~~Mancano test BoardRepository e CommandRepository~~ | ~~Media~~ | ✅ **Risolto** |
| ~~TEST-002~~ | ~~Mancano test BoardTypeRepository~~ | ~~Media~~ | ✅ **Risolto** |
| ~~TEST-003~~ | ~~Uso .Wait() bloccante~~ | ~~Media~~ | ✅ **Risolto** |
| ~~TEST-004~~ | ~~Mancano test DI~~ | ~~Bassa~~ | ✅ **Risolto** |
| ~~TEST-005~~ | ~~Mancano test scenari update/delete~~ | ~~Bassa~~ | ✅ **Risolto** |
| ~~TEST-009~~ | ~~Aggiornamento test per Domain v2 (T-002)~~ | ~~Alta~~ | ✅ **Risolto** |
| ~~TEST-007~~ | ~~Manca test Shared Peripheral~~ | ~~Alta~~ | ✅ **Risolto (T-002)** |
| ~~TEST-008~~ | ~~VariableMapperTests non testa Format round-trip~~ | ~~Media~~ | ✅ **Risolto** |
| [TEST-006](./Tests/ISSUES.md#test-006--test-gui-usano-costruttori-deprecati) | Test GUI usano costruttori deprecati | Bassa | Manutenibilità |

---

## Roadmap: Refactoring Domain v7 (T-006) — IN CORSO

| # | ID | Componente | Titolo | Status |
|---|-----|------------|--------|--------|
| 1 | CORE-008 | Core | Creare StandardVariableOverride, rimuovere VariableDeviceState | ✅ Risolto |
| 2 | INFRA-009 | Infrastructure | Entity + Repository + Migration per Domain v7 | ✅ Risolto |
| 3 | SVC-012 | Services | Mapper + Service per StandardVariableOverride | ⬜ Da fare |
| 4 | GUI-009 | GUI.Windows | Rimuovere DeviceVariables, aggiornare DictionaryEdit | ⬜ Da fare |
| 5 | TEST-010 | Tests | Aggiornare/riscrittura test per Domain v7 | ⬜ Da fare |

**Branch proposto:** `domain/domain-v7`  
**Data apertura:** 2026-03-30  
**Issue risolte automaticamente dal refactoring:**
- (T-005 parzialmente: VariableDeviceState eliminata)

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
| 1 | **T-006** | Trasversale | **StandardVariableOverride per-dizionario (Domain v7)** | **L** |
| 2 | SVC-002 | Services | Manca IAuditService | M |
| 3 | T-003 | Trasversale | Logging infrastructure | M |

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
| **Architecture** | ⚠️ 85% | Domain v7 formalizzato, T-006 aperta (refactoring pendente) |
| **Thread Safety** | ✅ 95% | Modelli immutabili |
| **Input Validation** | ✅ 85% | BR-011 (StandardVariableOverride v7), CORE-006, CORE-005 residui |
| **Data Integrity** | ✅ 95% | SVC-009 risolta |
| **Performance** | ✅ 100% | INFRA-002 + SVC-003 (Wontfix, coperto da INFRA-002) |
| **Resilience** | ✅ 90% | GUI-005 risolta, navigazione protetta |
| **Code Consistency** | ✅ 90% | INFRA-006 residuo |
| **Test Coverage** | ✅ 95% | ~1575 test cases, TEST-008 risolta |

---

## Issue per Categoria

| Categoria | Count | Issue |
|-----------|-------|-------|
| **Refactoring** | 5 | CORE-008, INFRA-009, SVC-012, GUI-009, TEST-010 (T-006) |
| **Bug** | 1 | INFRA-006 |
| **Design** | 3 | SVC-005, SVC-006, INFRA-005 |
| **UX** | 1 | GUI-002 |
| **Performance** | 0 | - |
| **Copertura** | 0 | - |
| **API** | 3 | CORE-003, CORE-004, CORE-005 |
| **Manutenibilità** | 1 | TEST-006 |
| **Code Smell** | 0 | - |
| **Feature** | 1 | SVC-002 |
| **Robustezza** | 1 | SVC-007 |
| **Trasversale** | 4 | T-003, T-004, T-005, T-006 |

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
| 2026-04-07 | ✅ **CORE-008 + INFRA-009 risolte** — StandardVariableOverride entity+repository+migration creati. VariableDeviceState eliminato. BitInterpretation.DeviceId→DictionaryId. Migration reset. 19 aperte, 35 risolte. |
| 2026-03-30 | ⚠️ **T-006 aperta (Domain v7)**
| 2026-03-25 | ✅ **SVC-010 risolta** — Eliminato Class1.cs placeholder. 13 aperte, 33 risolte. |
| 2026-03-25 | ⚪ **SVC-003 Wontfix**
| 2026-03-25 | ✅ **INFRA-003 risolta**
| 2026-03-25 | ⚠️ **T-003 aperta**
| 2026-03-25 | ✅ **INFRA-002 risolta**
| 2026-03-25 | ✅ **GUI-006 risolta**
| 2026-03-25 | ✅ **CORE-006 risolta**
| 2026-03-25 | ✅ **SVC-009 + TEST-008 risolte**
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
