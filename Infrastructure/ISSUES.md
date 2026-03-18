# Infrastructure - ISSUES

> **Scopo:** Questo documento traccia bug, code smells, performance issues, opportunità di refactoring e violazioni di best practice per il componente **Infrastructure**.

> **Ultimo aggiornamento:** 2026-03-18

---

## Riepilogo

| Priorità | Aperte | Risolte |
|----------|--------|---------|
| **Critica** | 0 | 0 |
| **Alta** | 1 | 0 |
| **Media** | 2 | 0 |
| **Bassa** | 3 | 0 |

**Totale aperte:** 6  
**Totale risolte:** 0

---

## Indice Issue Aperte

- [INFRA-001 - RepositoryBase.DeleteAsync non solleva eccezione se entity non trovata](#infra-001--repositorybasedeleteasync-non-solleva-eccezione-se-entity-non-trovata)
- [INFRA-002 - GetAllAsync senza paginazione rischia performance issues](#infra-002--getallasync-senza-paginazione-rischia-performance-issues)
- [INFRA-003 - DesignTimeDbContextFactory ha path hardcoded fragile](#infra-003--designtimedbcontextfactory-ha-path-hardcoded-fragile)
- [INFRA-004 - Mancano repository per BitInterpretation e CommandDeviceState](#infra-004--mancano-repository-per-bitinterpretation-e-commanddevicestate)
- [INFRA-005 - CommandEntity.ParametersJson non ha conversione JSON tipizzata](#infra-005--commandentityparametersjson-non-ha-conversione-json-tipizzata)
- [INFRA-006 - DictionaryRepository.GetByNameAsync non normalizza input](#infra-006--dictionaryrepositorygetbynameasync-non-normalizza-input)

## Indice Issue Risolte

*(Nessuna issue risolta)*

---

## Priorità Alta

### INFRA-001 - RepositoryBase.DeleteAsync non solleva eccezione se entity non trovata

**Categoria:** API  
**Priorità:** Alta  
**Impatto:** Medio  
**Status:** Aperto  
**Data Apertura:** 2026-03-18  

#### Descrizione

Il metodo `DeleteAsync` in `RepositoryBase` non notifica il chiamante se l'entity da eliminare non esiste. Questo può nascondere bug nel codice che assume l'entity fosse presente.

#### File Coinvolti

- `Infrastructure/Repositories/RepositoryBase.cs` (righe 43-51)

#### Codice Problematico

```csharp
public virtual async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
{
    var entity = await GetByIdAsync(id, cancellationToken);
    if (entity != null)  // <-- Silenzioso se entity non trovata
    {
        DbSet.Remove(entity);
        await Context.SaveChangesAsync(cancellationToken);
    }
}
```

#### Problema Specifico

- Chiamare `DeleteAsync(999)` con ID inesistente non genera errore
- Il chiamante non sa se l'operazione ha avuto effetto
- Potrebbe nascondere bug di logica (es. delete su ID sbagliato)
- Inconsistenza: un DB reale fallirebbe silenziosamente

#### Soluzione Proposta

**Opzione A: Throw se non trovato (raccomandata)**

```csharp
public virtual async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
{
    var entity = await GetByIdAsync(id, cancellationToken)
        ?? throw new KeyNotFoundException($"Entity with Id {id} not found.");
    
    DbSet.Remove(entity);
    await Context.SaveChangesAsync(cancellationToken);
}
```

**Opzione B: Return bool**

```csharp
public virtual async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
{
    var entity = await GetByIdAsync(id, cancellationToken);
    if (entity == null)
        return false;
    
    DbSet.Remove(entity);
    await Context.SaveChangesAsync(cancellationToken);
    return true;
}
```

#### Benefici Attesi

- Fail-fast su errori di logica
- API più prevedibile
- Coerenza con pattern REST (404 se non trovato)

---

## Priorità Media

### INFRA-002 - GetAllAsync senza paginazione rischia performance issues

**Categoria:** Performance  
**Priorità:** Media  
**Impatto:** Alto (futuro)  
**Status:** Aperto  
**Data Apertura:** 2026-03-18  

#### Descrizione

Tutti i metodi `GetAllAsync` in `RepositoryBase` e nei repository specifici caricano l'intera tabella in memoria senza paginazione. Con migliaia di variabili/comandi, questo causerà problemi di performance.

#### File Coinvolti

- `Infrastructure/Repositories/RepositoryBase.cs` (righe 25-28)
- `Infrastructure/Interfaces/IRepository.cs` (riga 8)

#### Codice Problematico

```csharp
public virtual async Task<IReadOnlyList<TEntity>> GetAllAsync(
    CancellationToken cancellationToken = default)
{
    return await DbSet.ToListAsync(cancellationToken);  // <-- Carica TUTTO
}
```

#### Problema Specifico

- Un dizionario con 500+ variabili verrebbe caricato tutto
- La tabella AuditEntries crescerà indefinitamente
- GC pressure e memory spikes su dataset grandi
- Non scala per uso produzione

#### Soluzione Proposta

**Opzione A: Aggiungere overload con paginazione**

```csharp
public interface IRepository<TEntity> where TEntity : class
{
    // Esistente (per retrocompatibilità)
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken ct = default);
    
    // Nuovo metodo paginato
    Task<PagedResult<TEntity>> GetPagedAsync(int page, int pageSize, 
        CancellationToken ct = default);
}

public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);
```

**Opzione B: Limite implicito (pragmatico)**

```csharp
public virtual async Task<IReadOnlyList<TEntity>> GetAllAsync(
    CancellationToken cancellationToken = default)
{
    return await DbSet.Take(1000).ToListAsync(cancellationToken);
}
```

#### Benefici Attesi

- Scalabilità su dataset grandi
- Minore GC pressure
- API più robusta per produzione

---

### INFRA-003 - DesignTimeDbContextFactory ha path hardcoded fragile

**Categoria:** Manutenibilità  
**Priorità:** Media  
**Impatto:** Basso  
**Status:** Aperto  
**Data Apertura:** 2026-03-18  

#### Descrizione

La `DesignTimeDbContextFactory` usa path relativi hardcoded che dipendono dalla struttura di output del build. Questo può fallire in contesti diversi (CI, Rider, VS Code).

#### File Coinvolti

- `Infrastructure/DesignTimeDbContextFactory.cs` (righe 13-16)

#### Codice Problematico

```csharp
public AppDbContext CreateDbContext(string[] args)
{
    var assemblyPath = Path.GetDirectoryName(
        typeof(DesignTimeDbContextFactory).Assembly.Location);
    var solutionPath = Path.GetFullPath(
        Path.Combine(assemblyPath!, "..", "..", "..", ".."));  // <-- Fragile
    var dbPath = Path.Combine(solutionPath, "Infrastructure", "Data", "development.db");
    // ...
}
```

#### Problema Specifico

- Dipende dal livello di nesting dell'output (`bin/Debug/net10.0/`)
- Cambiando TFM o configurazione, il path potrebbe rompersi
- CI pipelines potrebbero avere strutture diverse
- Non c'è validazione che il path sia corretto

#### Soluzione Proposta

**Opzione A: Cercare verso l'alto fino a .sln**

```csharp
public AppDbContext CreateDbContext(string[] args)
{
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory != null && !directory.GetFiles("*.sln").Any())
    {
        directory = directory.Parent;
    }
    
    if (directory == null)
        throw new InvalidOperationException("Solution directory not found.");
    
    var dbPath = Path.Combine(directory.FullName, "Infrastructure", "Data", "development.db");
    // ...
}
```

**Opzione B: Environment variable**

```csharp
var solutionPath = Environment.GetEnvironmentVariable("DICTIONARIES_SOLUTION_PATH")
    ?? FindSolutionPath();
```

#### Benefici Attesi

- Robustezza in diversi ambienti
- Meno errori in CI/CD
- Più facile debug

---

## Priorità Bassa

### INFRA-004 - Mancano repository per BitInterpretation e CommandDeviceState

**Categoria:** API  
**Priorità:** Bassa  
**Impatto:** Basso  
**Status:** Aperto  
**Data Apertura:** 2026-03-18  

#### Descrizione

Le entities `BitInterpretationEntity` e `CommandDeviceStateEntity` non hanno repository dedicati. Sono accessibili solo tramite `Include()` dai parent (Variable, Command).

#### File Coinvolti

- `Infrastructure/Repositories/` (mancano IBitInterpretationRepository, ICommandDeviceStateRepository)
- `Infrastructure/DependencyInjection.cs` (mancano registrazioni)

#### Problema Specifico

- Operazioni CRUD dirette su BitInterpretation richiedono accesso tramite Variable
- Query specifiche (es. "tutti i bit interpretati per DeviceType X") non sono possibili
- Documentato come TODO nelle copilot-instructions, ma non ancora implementato

#### Soluzione Proposta

```csharp
// Interfaces/IBitInterpretationRepository.cs
public interface IBitInterpretationRepository : IRepository<BitInterpretationEntity>
{
    Task<IReadOnlyList<BitInterpretationEntity>> GetByDeviceTypeAsync(
        DeviceType deviceType, CancellationToken ct = default);
    Task<IReadOnlyList<BitInterpretationEntity>> GetByVariableIdAsync(
        int variableId, CancellationToken ct = default);
}

// Interfaces/ICommandDeviceStateRepository.cs
public interface ICommandDeviceStateRepository : IRepository<CommandDeviceStateEntity>
{
    Task<IReadOnlyList<CommandDeviceStateEntity>> GetByDeviceTypeAsync(
        DeviceType deviceType, CancellationToken ct = default);
    Task<CommandDeviceStateEntity?> GetByCommandAndDeviceAsync(
        int commandId, DeviceType deviceType, CancellationToken ct = default);
}
```

#### Benefici Attesi

- API completa per tutte le entities
- Query ottimizzate per casi d'uso specifici
- Coerenza con altri repository

---

### INFRA-005 - CommandEntity.ParametersJson non ha conversione JSON tipizzata

**Categoria:** Design  
**Priorità:** Bassa  
**Impatto:** Basso  
**Status:** Aperto  
**Data Apertura:** 2026-03-18  

#### Descrizione

`CommandEntity.ParametersJson` è una stringa JSON grezza. EF Core 10 supporta conversioni JSON native che renderebbero il codice più type-safe.

#### File Coinvolti

- `Infrastructure/Entities/CommandEntity.cs` (riga 12)
- `Infrastructure/AppDbContext.cs` (configurazione Command)

#### Codice Problematico

```csharp
public class CommandEntity : IAuditable
{
    // ...
    public string ParametersJson { get; set; } = "[]";  // <-- Stringa grezza
}
```

#### Problema Specifico

- Serializzazione/deserializzazione manuale nel Services layer
- Possibili errori JSON non catturati a compile-time
- Non sfrutta le capacità JSON di EF Core 10

#### Soluzione Proposta

```csharp
// Entity
public class CommandEntity : IAuditable
{
    public int Id { get; set; }
    // ...
    public List<string> Parameters { get; set; } = [];
}

// OnModelCreating
modelBuilder.Entity<CommandEntity>(entity =>
{
    entity.Property(e => e.Parameters)
        .HasConversion(
            v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
            v => JsonSerializer.Deserialize<List<string>>(v, JsonSerializerOptions.Default) ?? []);
});
```

#### Benefici Attesi

- Type-safety a compile-time
- Meno boilerplate nel Services layer
- Sfrutta funzionalità EF Core moderne

---

### INFRA-006 - DictionaryRepository.GetByNameAsync non normalizza input

**Categoria:** Bug  
**Priorità:** Bassa  
**Impatto:** Basso  
**Status:** Aperto  
**Data Apertura:** 2026-03-18  

#### Descrizione

`DictionaryRepository.GetByNameAsync` confronta il nome esatto, mentre `UserRepository.GetByUsernameAsync` normalizza l'input con `ToLowerInvariant()`. Inconsistenza tra repository.

#### File Coinvolti

- `Infrastructure/Repositories/DictionaryRepository.cs` (righe 13-17)
- `Infrastructure/Repositories/UserRepository.cs` (righe 13-18 - corretto)

#### Codice Problematico

```csharp
// DictionaryRepository - NON normalizza
public async Task<DictionaryEntity?> GetByNameAsync(string name, 
    CancellationToken cancellationToken = default)
{
    return await DbSet.FirstOrDefaultAsync(d => d.Name == name, cancellationToken);
}

// UserRepository - Normalizza
public async Task<UserEntity?> GetByUsernameAsync(string username, 
    CancellationToken cancellationToken = default)
{
    var normalizedUsername = username.ToLowerInvariant();  // <-- Normalizza
    return await DbSet.FirstOrDefaultAsync(u => u.Username == normalizedUsername, cancellationToken);
}
```

#### Problema Specifico

- `GetByNameAsync("OPTIMUS-XP")` non trova "optimus-xp"
- Dipende da come i dati sono stati inseriti
- Inconsistenza con comportamento UserRepository
- Potrebbe causare bug difficili da debuggare

#### Soluzione Proposta

**Opzione A: Normalizzare nel repository**

```csharp
public async Task<DictionaryEntity?> GetByNameAsync(string name, 
    CancellationToken cancellationToken = default)
{
    var normalizedName = name.ToLowerInvariant();
    return await DbSet.FirstOrDefaultAsync(
        d => d.Name.ToLower() == normalizedName, cancellationToken);
}
```

**Opzione B: Case-insensitive collation (DB level)**

Configurare SQLite con collation NOCASE:
```sql
CREATE TABLE Dictionaries (..., Name TEXT COLLATE NOCASE);
```

#### Benefici Attesi

- Coerenza tra repository
- Ricerca case-insensitive prevedibile
- Meno bug in fase di lookup

---

## Issue Risolte

*(Nessuna issue risolta)*

---

## Wontfix

*(Nessuna issue in wontfix)*
