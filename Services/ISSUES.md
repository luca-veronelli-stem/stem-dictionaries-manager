# Services - ISSUES

> **Scopo:** Questo documento traccia bug, code smells, performance issues, opportunità di refactoring e violazioni di best practice per il componente **Services**.

> **Ultimo aggiornamento:** 2026-03-24

---

## Riepilogo

| Priorità | Aperte | Risolte |
|----------|--------|---------|
| **Critica** | 0 | 0 |
| **Alta** | 0 | 2 |
| **Media** | 3 | 1 |
| **Bassa** | 4 | 1 |

**Totale aperte:** 7  
**Totale risolte:** 4

---

## Indice Issue Aperte

- [SVC-002 - Manca IAuditService per gestione audit trail](#svc-002--manca-iauditservice-per-gestione-audit-trail)
- [SVC-003 - GetAllAsync senza paginazione nei services](#svc-003--getallasync-senza-paginazione-nei-services)
- [SVC-009 - VariableMapper.ToDomain non mappa Format](#svc-009--variablemappertodomain-non-mappa-format)
- [SVC-005 - CommandService.GetWithDeviceStatesAsync non espone DeviceStates](#svc-005--commandservicegetwithdevicestatesasync-non-espone-devicestates)
- [SVC-006 - Manca validazione business rules centralizzata](#svc-006--manca-validazione-business-rules-centralizzata)
- [SVC-007 - DependencyInjection non valida prerequisiti](#svc-007--dependencyinjection-non-valida-prerequisiti)
- [SVC-010 - Class1.cs placeholder non rimosso](#svc-010--class1cs-placeholder-non-rimosso)

## Indice Issue Risolte

- [SVC-011 - Refactoring Services per Domain v2](#svc-011--refactoring-services-per-domain-v2)
- [SVC-008 - DictionaryService.AddAsync blocca Shared Peripheral se Standard esiste](#svc-008--dictionaryserviceaddasync-blocca-shared-peripheral-se-standard-esiste)
- [SVC-004 - Mancano mapper per BoardMapper con overload](#svc-004--mancano-mapper-per-boardmapper-con-overload)
- [SVC-001 - Services dipendono direttamente da AppDbContext](#svc-001--services-dipendono-direttamente-da-appdbcontext-risolto)

---

## Priorità Media

### SVC-002 - Manca IAuditService per gestione audit trail

**Categoria:** Feature Mancante  
**Priorità:** Media  
**Impatto:** Medio  
**Status:** Aperto  
**Data Apertura:** 2026-03-18  

#### Descrizione

Il sistema ha `AuditEntryRepository` in Infrastructure ma manca un service per gestire l'audit trail a livello applicativo. Le operazioni di audit dovrebbero essere gestite tramite un service dedicato.

#### File Mancanti

- `Services/Interfaces/IAuditService.cs`
- `Services/AuditService.cs`

#### Soluzione Proposta

```csharp
// Services/Interfaces/IAuditService.cs
public interface IAuditService
{
    Task<IReadOnlyList<AuditEntry>> GetByEntityAsync(AuditEntityType entityType, int entityId, 
        CancellationToken ct = default);
    Task<IReadOnlyList<AuditEntry>> GetRecentAsync(int count = 100, CancellationToken ct = default);
    Task<IReadOnlyList<AuditEntry>> GetByUserAsync(int userId, CancellationToken ct = default);
    Task<IReadOnlyList<AuditEntry>> GetByDateRangeAsync(DateTime from, DateTime to, 
        CancellationToken ct = default);
    
    // Audit automatico chiamato dai services
    Task LogCreateAsync<T>(T entity, int? userId = null, CancellationToken ct = default);
    Task LogUpdateAsync<T>(T oldEntity, T newEntity, int? userId = null, CancellationToken ct = default);
    Task LogDeleteAsync<T>(T entity, int? userId = null, CancellationToken ct = default);
}
```

#### Benefici Attesi

- Gestione centralizzata dell'audit
- Possibilità di query su audit trail
- Integrazione con autenticazione (userId)

---

### SVC-003 - GetAllAsync senza paginazione nei services

**Categoria:** Performance  
**Priorità:** Media  
**Impatto:** Alto (futuro)  
**Status:** Aperto  
**Data Apertura:** 2026-03-18  

#### Descrizione

Tutti i metodi `GetAllAsync` nei services caricano l'intera collezione in memoria. Con migliaia di dizionari/variabili/comandi, questo causerà problemi di performance.

#### File Coinvolti

- `Services/DictionaryService.cs` (GetAllAsync)
- `Services/VariableService.cs` (GetAllAsync, GetByDictionaryIdAsync)
- `Services/CommandService.cs` (GetAllAsync)
- `Services/BoardService.cs` (GetAllAsync, GetBoardTypesAsync)
- `Services/UserService.cs` (GetAllAsync)

#### Soluzione Proposta

Aggiungere overload con paginazione:

```csharp
// Interfaces
Task<PagedResult<Dictionary>> GetAllAsync(int page, int pageSize, CancellationToken ct = default);

// PagedResult<T> class
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; }
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
```

#### Benefici Attesi

- Performance scalabile
- Riduzione memory footprint
- UX migliore con lazy loading

---

## Priorità Bassa

### SVC-005 - CommandService.GetWithDeviceStatesAsync non espone DeviceStates

**Categoria:** Design  
**Priorità:** Bassa  
**Impatto:** Basso  
**Status:** Aperto  
**Data Apertura:** 2026-03-18  

#### Descrizione

`GetWithDeviceStatesAsync` carica i DeviceStates ma il Domain Model `Command` non li espone. Il chiamante deve usare `GetDeviceStateAsync` separatamente.

#### File Coinvolti

- `Services/CommandService.cs` (righe 85-95)
- `Core/Models/Command.cs` (manca collezione DeviceStates)

#### Codice Problematico

```csharp
public async Task<Command?> GetWithDeviceStatesAsync(int id, CancellationToken ct = default)
{
    var entity = await _repository.GetWithDeviceStatesAsync(id, ct);
    if (entity is null)
        return null;
    
    var command = CommandMapper.ToDomain(entity);
    // Nota: DeviceStates sono caricati ma non esposti nel Domain Model Command.
    // Per accedere agli stati, usare GetDeviceStateAsync o SetDeviceStateAsync.
    return command;
}
```

#### Soluzione Proposta

**Opzione A: Estendere Domain Model**

```csharp
// Core/Models/Command.cs
public class Command
{
    // ... existing
    private readonly List<CommandDeviceState> _deviceStates = [];
    public IReadOnlyList<CommandDeviceState> DeviceStates => _deviceStates.AsReadOnly();
}

// CommandMapper - aggiungere mapping DeviceStates
```

**Opzione B: DTO dedicato (se non vogliamo modificare Domain)**

```csharp
public record CommandWithStates(Command Command, IReadOnlyList<CommandDeviceState> States);
```

#### Benefici Attesi

- API più intuitiva
- Riduzione chiamate N+1

---

### SVC-006 - Manca validazione business rules centralizzata

**Categoria:** Design  
**Priorità:** Bassa  
**Impatto:** Medio  
**Status:** Aperto  
**Data Apertura:** 2026-03-18  

#### Descrizione

Le business rules (unicità nome, unicità indirizzo, etc.) sono sparse nei vari services. Manca un layer di validazione centralizzato.

#### File Coinvolti

- Tutti i files in `Services/*.cs`

#### Esempio Problematico

```csharp
// DictionaryService.cs - validazione inline
var existingByName = await _dictionaryRepository.GetByNameAsync(dictionary.Name, ct);
if (existingByName is not null)
    throw new InvalidOperationException($"Dictionary with name '{dictionary.Name}' already exists.");

// VariableService.cs - stessa logica duplicata
var existingByAddress = await _repository.GetByAddressAsync(...);
if (existingByAddress is not null)
    throw new InvalidOperationException(...);
```

#### Soluzione Proposta

Introdurre un Validator pattern:

```csharp
// Services/Validation/IDictionaryValidator.cs
public interface IDictionaryValidator
{
    Task<ValidationResult> ValidateForCreateAsync(Dictionary dictionary, CancellationToken ct);
    Task<ValidationResult> ValidateForUpdateAsync(Dictionary dictionary, CancellationToken ct);
}

// ValidationResult
public record ValidationResult(bool IsValid, IReadOnlyList<string> Errors);

// Usage in service
public async Task<Dictionary> AddAsync(Dictionary dictionary, CancellationToken ct = default)
{
    var validation = await _validator.ValidateForCreateAsync(dictionary, ct);
    if (!validation.IsValid)
        throw new ValidationException(validation.Errors);
    // ...
}
```

#### Benefici Attesi

- Business rules centralizzate e testabili
- Riuso delle validazioni
- Messaggi di errore consistenti

---

### SVC-007 - DependencyInjection non valida prerequisiti

**Categoria:** Robustezza  
**Priorità:** Bassa  
**Impatto:** Basso  
**Status:** Aperto  
**Data Apertura:** 2026-03-18  

#### Descrizione

`AddServices()` richiede che `AddInfrastructure()` sia stato chiamato prima, ma non lo valida. Se Infrastructure non è registrato, si ottiene errore runtime non chiaro.

#### File Coinvolti

- `Services/DependencyInjection.cs`

#### Codice Attuale

```csharp
public static IServiceCollection AddServices(this IServiceCollection services)
{
    // Richiede che Infrastructure sia già registrato (AddInfrastructure).
    // <-- Ma non lo verifica!
    services.AddScoped<IDictionaryService, DictionaryService>();
    // ...
}
```

#### Soluzione Proposta

```csharp
public static IServiceCollection AddServices(this IServiceCollection services)
{
    // Verifica prerequisiti
    if (!services.Any(s => s.ServiceType == typeof(IDictionaryRepository)))
        throw new InvalidOperationException(
            "Infrastructure services not registered. Call AddInfrastructure() before AddServices().");
    
    services.AddScoped<IDictionaryService, DictionaryService>();
    // ...
    return services;
}
```

#### Benefici Attesi

- Fail-fast con messaggio chiaro
- Documentazione implicita delle dipendenze

---

### SVC-009 - VariableMapper.ToDomain non mappa Format

**Categoria:** Bug (Data Loss)  
**Priorità:** Media  
**Impatto:** Medio  
**Status:** Aperto  
**Data Apertura:** 2026-03-24  

#### Descrizione

`VariableMapper.ToDomain` passa `format: null` con commento errato "Format non è presente in Entity". In realtà `VariableEntity.Format` esiste (riga 16) ed è configurato in `AppDbContext` (riga 121: `HasMaxLength(50)`). Il campo Format viene **perso in round-trip** perché non è mappato né in lettura, né in scrittura, né in update.

#### File Coinvolti

- `Services/Mapping/VariableMapper.cs` (righe 29, 44-61, 68-86)
- `Infrastructure/Entities/VariableEntity.cs` (riga 16)

#### Codice Problematico

```csharp
// ToDomain - riga 29: commento ERRATO
format: null, // Format non è presente in Entity  ← FALSO

// ToEntity - righe 44-61: Format non mappato
return new VariableEntity
{
    // ... tutte le proprietà TRANNE Format
};

// UpdateEntity - righe 68-86: Format non aggiornato
// entity.Format = domain.Format;  ← MANCANTE
```

#### Problema Specifico

- `VariableEntity.Format` esiste e ha `HasMaxLength(50)` in AppDbContext
- `Variable.Format` esiste nel domain model
- Il mapper non copia il valore in **nessuna** direzione
- Qualsiasi dato Format nel DB viene ignorato in lettura
- Qualsiasi dato Format dal domain non viene persistito

#### Soluzione Proposta

```csharp
// ToDomain:
format: entity.Format,

// ToEntity:
Format = domain.Format,

// UpdateEntity:
entity.Format = domain.Format;
```

#### Benefici Attesi

- Round-trip corretto per il campo Format
- Nessuna perdita di dati
- Commento corretto

---

### SVC-010 - Class1.cs placeholder non rimosso

**Categoria:** Code Smell  
**Priorità:** Bassa  
**Impatto:** Nullo  
**Status:** Aperto  
**Data Apertura:** 2026-03-24  

#### Descrizione

`Services/Class1.cs` è il placeholder generato da `dotnet new classlib` rimasto nel progetto. I placeholder di `Core` e `Tests` sono stati rimossi in SESSION_002, ma quello di Services no.

#### File Coinvolti

- `Services/Class1.cs`

#### Codice Problematico

```csharp
namespace Services
{
    public class Class1
    {
    }
}
```

#### Soluzione Proposta

Eliminare il file.

#### Benefici Attesi

- Codebase più pulita
- Nessuna classe vuota nel namespace

---

## Issue Risolte

### SVC-011 - Refactoring Services per Domain v2

**Categoria:** Refactoring  
**Priorità:** Alta  
**Impatto:** Alto  
**Status:** Risolto  
**Data Apertura:** 2026-03-25  
**Data Risoluzione:** 2026-03-25  
**Branch:** domain/ridefinizione-dominio-v2  
**Master Issue:** T-002

#### Soluzione Implementata

1. **DELETE:** `Mapping/BoardTypeMapper.cs`
2. **MODIFY:** `BoardMapper.cs` — FirmwareType da Board, DictionaryId?, IsPrimary
3. **MODIFY:** `DictionaryMapper.cs` — IsStandard flag, no DeviceType/BoardType
4. **MODIFY:** `BoardService.cs`, `DictionaryService.cs` — Standard check via IsStandard

#### Benefici Ottenuti

- Services allineati al Domain v2 ✅
- Risolve anche SVC-008 e SVC-004 ✅

---

### SVC-008 - DictionaryService.AddAsync blocca Shared Peripheral se Standard esiste

**Categoria:** Bug  
**Priorità:** Alta  
**Impatto:** Alto  
**Status:** Risolto  
**Data Apertura:** 2026-03-24  
**Data Risoluzione:** 2026-03-25  
**Branch:** domain/ridefinizione-dominio-v2

#### Soluzione Implementata

Il bug non esiste più: la semantica 3-tuple `(DeviceType?, BoardType?)` è stata sostituita con `IsStandard` flag. La logica di validazione ora controlla solo `if (dictionary.IsStandard)` → verifica unicità.

---

### SVC-004 - Mancano mapper per BoardMapper con overload

**Categoria:** Code Smell  
**Priorità:** Bassa  
**Impatto:** Basso  
**Status:** Risolto  
**Data Apertura:** 2026-03-18  
**Data Risoluzione:** 2026-03-25  
**Branch:** domain/ridefinizione-dominio-v2

#### Soluzione Implementata

BoardMapper riscritto: `BoardType` rimosso, `FirmwareType`/`DictionaryId?`/`IsPrimary` mappati direttamente. Nessun Include richiesto per BoardType.

---


### SVC-001 - Services dipendono direttamente da AppDbContext

**Categoria:** Design/Architettura  
**Priorità:** Media  
**Impatto:** Medio  
**Status:** Risolto  
**Data Apertura:** 2026-03-18  
**Data Risoluzione:** 2026-03-18  

#### Descrizione

Alcuni services (`DictionaryService`, `VariableService`, `CommandService`) dipendono direttamente da `AppDbContext` oltre che dai repository. Questo viola il pattern Repository e crea accoppiamento con l'infrastruttura.

#### File Coinvolti

- `Services/DictionaryService.cs` (riga 18)
- `Services/VariableService.cs` (riga 19)
- `Services/CommandService.cs` (riga 18)

#### Codice Problematico

```csharp
// DictionaryService.cs
public class DictionaryService : IDictionaryService
{
    private readonly IDictionaryRepository _dictionaryRepository;
    private readonly Infrastructure.AppDbContext _context;  // <-- Dipendenza diretta
    
    // Usato per query dirette:
    var entities = await _context.Dictionaries
        .Include(d => d.BoardType)
        .ToListAsync(ct);
}
```

#### Problema Specifico

- Viola separation of concerns (Services non dovrebbero conoscere EF Core)
- Difficile da testare con mock (richiede DbContext reale o in-memory)
- Query duplicate tra Services e Repository
- Accoppiamento forte con Infrastructure

#### Soluzione Proposta

**Opzione A: Estendere Repository (raccomandata)**

Aggiungere metodi mancanti ai repository invece di usare DbContext direttamente:

```csharp
// IDictionaryRepository.cs
Task<IReadOnlyList<DictionaryEntity>> GetAllWithBoardTypeAsync(CancellationToken ct);

// DictionaryService.cs - dopo
public async Task<IReadOnlyList<Dictionary>> GetAllAsync(CancellationToken ct = default)
{
    var entities = await _dictionaryRepository.GetAllWithBoardTypeAsync(ct);
    return DictionaryMapper.ToDomainList(entities);
}
```

**Opzione B: Unit of Work Pattern**

Introdurre `IUnitOfWork` per coordinare repository senza esporre DbContext.

#### Benefici Attesi

- Services layer completamente disaccoppiato da EF Core
- Test più semplici con mock
- Single responsibility per ogni layer

#### Soluzione Implementata

Applicata **Opzione A: Estensione Repository**:

1. **Nuovi metodi su repository esistenti:**
   - `IDictionaryRepository.GetAllWithBoardTypeAsync()`, `ExistsAsync()`
   - `IVariableRepository.ExistsAsync()`, `GetWithBitInterpretationsAsync()`

2. **Nuovi repository creati:**
   - `IBitInterpretationRepository` / `BitInterpretationRepository`
   - `ICommandDeviceStateRepository` / `CommandDeviceStateRepository`

3. **Services refactored:**
   - `DictionaryService` - rimosso `_context`, usa repository methods
   - `VariableService` - rimosso `_context`, usa `_bitInterpretationRepository`
   - `CommandService` - rimosso `_context`, usa `_deviceStateRepository`

4. **Test aggiornati** per riflettere i nuovi costruttori

**Risultato:** 752 test passati, nessuna dipendenza diretta da `AppDbContext` nei Services.

---

## Wontfix

*(Nessuna issue in wontfix)*

---

## Metriche Qualità Services

| Metrica | Valore | Target |
|---------|--------|--------|
| Copertura test | ~95% | 90% |
| Complessità ciclomatica media | Bassa | Bassa |
| Dipendenze esterne | 2 (Core, Infrastructure) | ≤3 |
| LOC per file (media) | ~120 | ≤200 |
| Numero services | 5 | - |
| Numero mapper | 8 | - |

---

## Note per Sviluppo Futuro

### Export/Import Feature

Quando verrà implementata la funzionalità di export (CSV, JSON), considerare:
- `IExportService` per generazione file
- `IImportService` per parsing e validazione
- Formato compatibile con Excel esistente

### API Layer

Se verrà aggiunto un API layer (REST/GraphQL):
- I services sono già pronti per essere esposti
- Aggiungere DTOs specifici per API
- Gestione errori HTTP-friendly
