# Services

> **Layer di business logic con mapping Entity ↔ Domain e orchestrazione dei repository.**  
> **Ultimo aggiornamento:** 2026-03-18

---

## Panoramica

Il progetto **Services** implementa la logica di business per Stem.Dictionaries.Manager. È il ponte tra Infrastructure (persistenza) e i consumer (GUI/API):

- **Mapping** - Conversione bidirezionale Entity ↔ Domain Model
- **Business Logic** - Validazioni, regole di dominio
- **Orchestrazione** - Coordinamento tra repository multipli
- **Aggregate Root** - Dictionary come punto di accesso per Variables

Questo layer espone Domain Models (Core) e nasconde i dettagli di persistenza (Infrastructure).

---

## Caratteristiche

| Feature | Stato | Descrizione |
|---------|-------|-------------|
| **Services** | ✅ | 5 services con interface |
| **Mappers** | ✅ | 8 mapper bidirezionali |
| **DI Extension** | ✅ | AddServices() per registrazione |
| **Aggregate Pattern** | ✅ | Dictionary gestisce Variables |
| **Validation** | ✅ | Unicità, esistenza, business rules |

---

## Requisiti

- **.NET 10.0** o superiore

### Dipendenze Progetto

| Progetto | Uso |
|----------|-----|
| Core | Domain Models, Enums |
| Infrastructure | Repository, DbContext, Entities |

---

## Quick Start

```csharp
using Infrastructure;
using Services;
using Microsoft.Extensions.DependencyInjection;

// Registrazione servizi (richiede Infrastructure già registrato)
services.AddInfrastructure("Data Source=dictionaries.db");
services.AddServices();

// Uso service
public class MyController
{
    private readonly IDictionaryService _dictionaries;
    
    public MyController(IDictionaryService dictionaries)
    {
        _dictionaries = dictionaries;
    }
    
    public async Task<Dictionary?> GetDictionaryAsync(int id)
    {
        return await _dictionaries.GetWithVariablesAsync(id);
    }
    
    public async Task AddVariableAsync(int dictId, Variable variable)
    {
        await _dictionaries.AddVariableAsync(dictId, variable);
    }
}
```

---

## Struttura

```
Services/
├── Interfaces/
│   ├── IDictionaryService.cs      # Aggregate root (Dictionary + Variables)
│   ├── IVariableService.cs        # Variabili singole + BitInterpretation
│   ├── ICommandService.cs         # Comandi + DeviceState
│   ├── IBoardService.cs           # Board + BoardType
│   └── IUserService.cs            # Utenti
├── Mapping/
│   ├── UserMapper.cs              # User Entity ↔ Domain
│   ├── BoardTypeMapper.cs         # BoardType Entity ↔ Domain
│   ├── BoardMapper.cs             # Board Entity ↔ Domain
│   ├── VariableMapper.cs          # Variable Entity ↔ Domain
│   ├── DictionaryMapper.cs        # Dictionary Entity ↔ Domain (con Variables)
│   ├── CommandMapper.cs           # Command Entity ↔ Domain (JSON params)
│   ├── CommandDeviceStateMapper.cs# CommandDeviceState Entity ↔ Domain
│   └── BitInterpretationMapper.cs # BitInterpretation Entity ↔ Domain
├── DictionaryService.cs           # Implementazione aggregate root
├── VariableService.cs             # Implementazione
├── CommandService.cs              # Implementazione
├── BoardService.cs                # Implementazione
├── UserService.cs                 # Implementazione
├── DependencyInjection.cs         # Extension method AddServices()
├── README.md                      # Questa documentazione
└── ISSUES.md                      # 7 issue tracciate
```

---

## API / Componenti

### Service Interfaces

| Interface | Metodi Principali | Aggregate |
|-----------|-------------------|-----------|
| `IDictionaryService` | GetWithVariablesAsync, AddVariableAsync, RemoveVariableAsync | ✅ Root |
| `IVariableService` | GetByDictionaryIdAsync, GetByAddressAsync, AddBitInterpretationAsync | - |
| `ICommandService` | GetByCodeAsync, GetWithDeviceStatesAsync, SetDeviceStateAsync | - |
| `IBoardService` | GetByDeviceTypeAsync, GetByProtocolAddressAsync, AddBoardTypeAsync | - |
| `IUserService` | GetByUsernameAsync, UsernameExistsAsync | - |

### IDictionaryService (Aggregate Root)

```csharp
public interface IDictionaryService
{
    // CRUD Base
    Task<Dictionary?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Dictionary>> GetAllAsync(CancellationToken ct = default);
    Task<Dictionary> AddAsync(Dictionary dictionary, CancellationToken ct = default);
    Task UpdateAsync(Dictionary dictionary, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    
    // Query
    Task<Dictionary?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<Dictionary?> GetByBoardTypeIdAsync(int boardTypeId, CancellationToken ct = default);
    Task<Dictionary?> GetStandardDictionaryAsync(CancellationToken ct = default);
    
    // Aggregate Operations
    Task<Dictionary?> GetWithVariablesAsync(int id, CancellationToken ct = default);
    Task<Variable> AddVariableAsync(int dictionaryId, Variable variable, CancellationToken ct = default);
    Task RemoveVariableAsync(int dictionaryId, int variableId, CancellationToken ct = default);
}
```

### Mapper Pattern

Ogni mapper implementa:

| Metodo | Descrizione |
|--------|-------------|
| `ToDomain(Entity)` | Converte Entity in Domain Model |
| `ToEntity(Domain)` | Converte Domain Model in Entity (per creazione) |
| `UpdateEntity(Entity, Domain)` | Aggiorna Entity esistente (preserva Id, audit) |
| `ToDomainList(IEnumerable)` | Converte collezione |

```csharp
// Esempio: UserMapper
public static class UserMapper
{
    public static User ToDomain(UserEntity entity)
    {
        return User.Restore(entity.Id, entity.Username, entity.DisplayName);
    }
    
    public static UserEntity ToEntity(User domain)
    {
        return new UserEntity
        {
            Id = domain.Id,
            Username = domain.Username,
            DisplayName = domain.DisplayName
        };
    }
    
    public static void UpdateEntity(UserEntity entity, User domain)
    {
        entity.Username = domain.Username;
        entity.DisplayName = domain.DisplayName;
    }
}
```

---

## Flusso Dati

```
┌─────────────────────────────────────────────────────────────┐
│                      GUI / API                              │
│                   (Domain Models)                           │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                        Services                             │
│              ┌─────────────────────────────┐                │
│              │         Mappers             │                │
│              │   Entity ↔ Domain Model     │                │
│              └─────────────────────────────┘                │
│              ┌─────────────────────────────┐                │
│              │     Business Logic          │                │
│              │   Validation, Rules         │                │
│              └─────────────────────────────┘                │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                     Infrastructure                          │
│              (Repositories, Entities)                       │
└─────────────────────────────────────────────────────────────┘
```

---

## Business Rules

| Regola | Enforced In | Descrizione |
|--------|-------------|-------------|
| **BR-001** | DictionaryService | Dizionario "Standard" non ha BoardType |
| **BR-002** | DictionaryService | Ogni BoardType ha al massimo UN dizionario |
| **BR-003** | DictionaryService, VariableService | Indirizzo variabile univoco per dizionario |
| **BR-004** | CommandService | Codice comando univoco per (CodeHigh, CodeLow, IsResponse) |
| **BR-005** | UserService | Username univoco |
| **BR-006** | BoardService | FirmwareType univoco per BoardType |

---

## Validazioni

Ogni service valida:

```csharp
// Esistenza entità correlate
var boardType = await _boardTypeRepository.GetByIdAsync(dictionary.BoardType.Id, ct)
    ?? throw new InvalidOperationException($"BoardType with Id {id} not found.");

// Unicità
var existing = await _dictionaryRepository.GetByNameAsync(dictionary.Name, ct);
if (existing is not null)
    throw new InvalidOperationException($"Dictionary with name '{dictionary.Name}' already exists.");

// Appartenenza
if (variable.DictionaryId != dictionaryId)
    throw new InvalidOperationException($"Variable does not belong to dictionary.");
```

---

## Eccezioni

| Eccezione | Quando |
|-----------|--------|
| `KeyNotFoundException` | Entità non trovata per Id |
| `InvalidOperationException` | Violazione business rule (unicità, appartenenza) |
| `ArgumentNullException` | Parametro null |
| `ArgumentException` | Parametro invalido (stringa vuota) |

---

## Configurazione

### Dependency Injection

```csharp
// In Program.cs o Startup.cs
services.AddInfrastructure("Data Source=dictionaries.db");
services.AddServices();  // Richiede AddInfrastructure() prima

// Registra:
// - IDictionaryService → DictionaryService (Scoped)
// - IVariableService → VariableService (Scoped)
// - ICommandService → CommandService (Scoped)
// - IBoardService → BoardService (Scoped)
// - IUserService → UserService (Scoped)
```

---

## Test

| Categoria | File | Test |
|-----------|------|------|
| Unit/Mapping | `UserMapperTests.cs` | 10 |
| Unit/Mapping | `BoardTypeMapperTests.cs` | 10 |
| Unit/Mapping | `VariableMapperTests.cs` | 11 |
| Unit/Mapping | `CommandMapperTests.cs` | 14 |
| Unit/Mapping | `DictionaryMapperTests.cs` | 15 |
| Integration | `UserServiceTests.cs` | 16 |
| Integration | `DictionaryServiceTests.cs` | 17 |
| Integration | `BoardServiceTests.cs` | 17 |
| Integration | `CommandServiceTests.cs` | 15 |
| **Totale** | | **125 test** |

```bash
# Eseguire test Services
dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~Services"
```

---

## Issue Correlate

→ [Services/ISSUES.md](./ISSUES.md) — 7 issue aperte (0 critiche, 0 alte, 3 medie, 4 basse)

### Top Issue

| ID | Priorità | Descrizione |
|----|----------|-------------|
| SVC-001 | Media | Services dipendono direttamente da AppDbContext |
| SVC-002 | Media | Manca IAuditService per gestione audit trail |
| SVC-003 | Media | GetAllAsync senza paginazione |

---

## Links

- [Core/README.md](../Core/README.md) - Domain Models usati dai Services
- [Infrastructure/README.md](../Infrastructure/README.md) - Repository e Entities
- [Tests/README.md](../Tests/README.md) - Test suite
- [.copilot/copilot-instructions.md](../.copilot/copilot-instructions.md) - Architettura e decisioni
