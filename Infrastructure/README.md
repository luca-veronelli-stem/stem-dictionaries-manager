# Infrastructure

> **Layer di persistenza con Entity Framework Core, SQLite e pattern Repository.**  
> **Ultimo aggiornamento:** 2026-03-24

---

## Panoramica

Il progetto **Infrastructure** gestisce la persistenza dati per Stem.Dictionaries.Manager. Implementa:

- **Entity Framework Core** - ORM per accesso dati
- **SQLite** - Database di sviluppo (migrabile ad Azure SQL)
- **Pattern Repository** - Astrazione accesso dati con interfacce
- **Audit automatico** - CreatedAt/UpdatedAt gestiti in SaveChanges

Questo layer è l'unico che conosce il database. I modelli di dominio (Core) sono separati dalle Entity.

---

## Caratteristiche

| Feature | Stato | Descrizione |
|---------|-------|-------------|
| **Entities** | ✅ | 9 entity classes con IAuditable |
| **Repositories** | ✅ | 9 repository + base generica |
| **Migrations** | ✅ | 3 migrations (InitialCreate, DeviceType, IsPrimary) |
| **Audit Fields** | ✅ | CreatedAt/UpdatedAt automatici |
| **DI Extension** | ✅ | AddInfrastructure() per registrazione |
| **Database Seeder** | ✅ | Dati demo per sviluppo ✨ |

---

## Requisiti

- **.NET 10.0** o superiore

### Dipendenze

| Package | Versione | Uso |
|---------|----------|-----|
| Microsoft.EntityFrameworkCore | 10.0.5 | ORM |
| Microsoft.EntityFrameworkCore.Sqlite | 10.0.5 | Provider SQLite |
| Microsoft.EntityFrameworkCore.Design | 10.0.5 | Migrations tooling |

### Dipendenze Progetto

| Progetto | Uso |
|----------|-----|
| Core | Enums (DataTypeKind, AccessMode, etc.) |

---

## Quick Start

```csharp
using Infrastructure;
using Microsoft.Extensions.DependencyInjection;

// Registrazione servizi
services.AddInfrastructure("Data Source=dictionaries.db");

// Uso repository
public class MyService
{
    private readonly IDictionaryRepository _dictionaries;
    
    public MyService(IDictionaryRepository dictionaries)
    {
        _dictionaries = dictionaries;
    }
    
    public async Task<DictionaryEntity?> GetDictionaryAsync(int id)
    {
        return await _dictionaries.GetByIdAsync(id);
    }
}
```

---

## Struttura

```
Infrastructure/
├── Entities/
│   ├── UserEntity.cs              # Utente sistema
│   ├── BoardTypeEntity.cs         # Tipo scheda (Madre, Pulsantiera)
│   ├── BoardEntity.cs             # Scheda con IsPrimary e ProtocolAddress
│   ├── VariableEntity.cs          # Variabile dizionario (incl. Format)
│   ├── DictionaryEntity.cs        # Dizionario con DeviceType? e BoardType?
│   ├── BitInterpretationEntity.cs # Interpretazione bit bitmapped
│   ├── CommandEntity.cs           # Comando protocollo (ParametersJson)
│   ├── CommandDeviceStateEntity.cs# Stato comando per device
│   └── AuditEntryEntity.cs        # Audit trail (no IAuditable)
├── Interfaces/
│   ├── IAuditable.cs              # Interface per audit fields
│   ├── IRepository.cs             # Interface generica CRUD
│   ├── IUserRepository.cs
│   ├── IBoardTypeRepository.cs
│   ├── IBoardRepository.cs
│   ├── IDictionaryRepository.cs
│   ├── IVariableRepository.cs
│   ├── ICommandRepository.cs
│   ├── IBitInterpretationRepository.cs
│   ├── ICommandDeviceStateRepository.cs
│   └── IAuditEntryRepository.cs
├── Repositories/
│   ├── RepositoryBase.cs          # Implementazione CRUD comune
│   ├── UserRepository.cs
│   ├── BoardTypeRepository.cs
│   ├── BoardRepository.cs
│   ├── DictionaryRepository.cs
│   ├── VariableRepository.cs
│   ├── CommandRepository.cs
│   ├── BitInterpretationRepository.cs
│   ├── CommandDeviceStateRepository.cs
│   └── AuditEntryRepository.cs
├── Migrations/
│   ├── InitialCreate                                              # Schema 9 tabelle
│   ├── AddDeviceTypeToDictionary_RemoveDeviceTypeFromBitInterp...  # DeviceType su Dictionary
│   └── AddIsPrimaryToBoard                                        # IsPrimary su Board
├── AppDbContext.cs                # DbContext con audit automatico (9 DbSet)
├── DatabaseSeeder.cs              # Dati demo per sviluppo
├── DesignTimeDbContextFactory.cs  # Factory per migrations CLI
└── DependencyInjection.cs         # Extension method AddInfrastructure()
```

---

## API / Componenti

### Entities

| Entity | Tabella | IAuditable | Note |
|--------|---------|:----------:|------|
| `UserEntity` | Users | ✅ | Username univoco |
| `BoardTypeEntity` | BoardTypes | ✅ | FirmwareType univoco |
| `BoardEntity` | Boards | ✅ | FK → BoardType, IsPrimary, ProtocolAddress |
| `VariableEntity` | Variables | ✅ | FK → Dictionary, Format, unique (DictionaryId, AddressHigh, AddressLow) |
| `DictionaryEntity` | Dictionaries | ✅ | DeviceType?, FK → BoardType?, unique (DeviceType, BoardTypeId) |
| `BitInterpretationEntity` | BitInterpretations | ✅ | FK → Variable |
| `CommandEntity` | Commands | ✅ | ParametersJson, unique (CodeHigh, CodeLow, IsResponse) |
| `CommandDeviceStateEntity` | CommandDeviceStates | ✅ | FK → Command, DeviceType |
| `AuditEntryEntity` | AuditEntries | ❌ | Immutabile, FK → User |

### Repository Interfaces

| Interface | Metodi Custom |
|-----------|---------------|
| `IRepository<T>` | GetByIdAsync, GetAllAsync, AddAsync, UpdateAsync, DeleteAsync |
| `IUserRepository` | GetByUsernameAsync |
| `IBoardTypeRepository` | GetByNameAsync, GetByFirmwareTypeAsync |
| `IBoardRepository` | GetByDeviceTypeAsync, GetByProtocolAddressAsync |
| `IDictionaryRepository` | GetByNameAsync, GetByBoardTypeAsync, GetWithVariablesAsync, GetStandardDictionaryAsync, GetByDeviceTypeAndBoardTypeAsync, GetAllWithBoardTypeAsync, ExistsAsync |
| `IVariableRepository` | GetByDictionaryIdAsync, GetByAddressAsync, GetWithBitInterpretationsAsync, ExistsAsync |
| `ICommandRepository` | GetByCodeAsync, GetWithDeviceStatesAsync |
| `IBitInterpretationRepository` | GetByVariableIdAsync, SyncByVariableIdAsync |
| `ICommandDeviceStateRepository` | GetByCommandAndDeviceAsync, GetByCommandIdAsync |
| `IAuditEntryRepository` | GetByEntityAsync, GetByUserAsync, GetRecentAsync |

### Audit Automatico

```csharp
// AppDbContext.SaveChanges() imposta automaticamente:
// - CreatedAt (UTC) per entity Added
// - UpdatedAt (UTC) per entity Modified

public interface IAuditable
{
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
}
```

---

## Configurazione

### Connection String

```csharp
// Sviluppo (SQLite)
services.AddInfrastructure("Data Source=Infrastructure/Data/development.db");

// Produzione (Azure SQL) - futuro
services.AddInfrastructure("Server=...;Database=Dictionaries;...");
```

### Migrations

```bash
# Creare nuova migration
dotnet ef migrations add NomeMigration -p Infrastructure -s GUI.Windows

# Applicare migrations
dotnet ef database update -p Infrastructure -s GUI.Windows

# Rollback
dotnet ef database update PreviousMigration -p Infrastructure -s GUI.Windows
```

---

## Issue Correlate

→ [Infrastructure/ISSUES.md](./ISSUES.md) — 5 issue aperte, 2 risolte (0 critiche, 1 alta, 2 medie, 2 basse)

---

## Links

- [Core/README.md](../Core/README.md) - Modelli dominio ed enums
- [Docs/ER-schema.puml](../Docs/ER-schema.puml) - Schema ER database
- [.copilot/copilot-instructions.md](../.copilot/copilot-instructions.md) - Architettura e decisioni
