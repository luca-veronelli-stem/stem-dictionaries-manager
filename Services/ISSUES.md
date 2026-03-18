# Services - ISSUES

> **Scopo:** Questo documento traccia bug, code smells, performance issues, opportunità di refactoring e violazioni di best practice per il componente **Services**.

> **Ultimo aggiornamento:** 2026-03-18

---

## Riepilogo

| Priorità | Aperte | Risolte |
|----------|--------|---------|
| **Critica** | 0 | 0 |
| **Alta** | 0 | 0 |
| **Media** | 3 | 0 |
| **Bassa** | 4 | 0 |

**Totale aperte:** 7  
**Totale risolte:** 0

---

## Indice Issue Aperte

- [SVC-001 - Services dipendono direttamente da AppDbContext](#svc-001--services-dipendono-direttamente-da-appdbcontext)
- [SVC-002 - Manca IAuditService per gestione audit trail](#svc-002--manca-iauditservice-per-gestione-audit-trail)
- [SVC-003 - GetAllAsync senza paginazione nei services](#svc-003--getallasync-senza-paginazione-nei-services)
- [SVC-004 - Mancano mapper per BoardMapper con overload](#svc-004--mancano-mapper-per-boardmapper-con-overload)
- [SVC-005 - CommandService.GetWithDeviceStatesAsync non espone DeviceStates](#svc-005--commandservicegetwithdevicestatesasync-non-espone-devicestates)
- [SVC-006 - Manca validazione business rules centralizzata](#svc-006--manca-validazione-business-rules-centralizzata)
- [SVC-007 - DependencyInjection non valida prerequisiti](#svc-007--dependencyinjection-non-valida-prerequisiti)

## Indice Issue Risolte

*(Nessuna issue risolta)*

---

## Priorità Media

### SVC-001 - Services dipendono direttamente da AppDbContext

**Categoria:** Design/Architettura  
**Priorità:** Media  
**Impatto:** Medio  
**Status:** Aperto  
**Data Apertura:** 2026-03-18  

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

---

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

### SVC-004 - Mancano mapper per BoardMapper con overload

**Categoria:** Code Smell  
**Priorità:** Bassa  
**Impatto:** Basso  
**Status:** Aperto  
**Data Apertura:** 2026-03-18  

#### Descrizione

`BoardMapper` richiede che `BoardType` sia caricato via Include, ma se non lo è lancia eccezione. Manca un overload che gestisca gracefully il caso senza BoardType.

#### File Coinvolti

- `Services/Mapping/BoardMapper.cs` (righe 36-40)

#### Codice Problematico

```csharp
public static Board ToDomain(BoardEntity entity)
{
    if (entity.BoardType == null)
        throw new InvalidOperationException(
            $"BoardType not loaded for Board {entity.Id}. Use Include() or provide BoardType.");
    // ...
}
```

#### Problema Specifico

- Eccezione runtime se BoardType non caricato
- Non chiaro all'utilizzatore che deve usare Include
- Inconsistenza con altri mapper che non lanciano eccezioni

#### Soluzione Proposta

```csharp
// Opzione A: Metodo TryToDomain
public static bool TryToDomain(BoardEntity entity, out Board? board)
{
    if (entity.BoardType == null)
    {
        board = null;
        return false;
    }
    board = ToDomain(entity);
    return true;
}

// Opzione B: Documentazione più chiara + attributo Required
/// <summary>
/// Requires BoardType to be loaded via Include().
/// </summary>
[RequiresBoardTypeLoaded]
public static Board ToDomain(BoardEntity entity) { ... }
```

#### Benefici Attesi

- API più chiara e predicibile
- Meno eccezioni runtime inattese

---

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

## Issue Risolte

*(Nessuna issue risolta)*

---

## Wontfix

*(Nessuna issue in wontfix)*

---

## Metriche Qualità Services

| Metrica | Valore | Target |
|---------|--------|--------|
| Copertura test | ~80% | 90% |
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
