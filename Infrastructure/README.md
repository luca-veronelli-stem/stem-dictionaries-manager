# Infrastructure

> **Layer di persistenza con Entity Framework Core, SQLite e pattern Repository.**  
> **Ultimo aggiornamento:** 2026-04-07

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
| **Entities** | ✅ | 10 entity classes con IAuditable |
| **Repositories** | ✅ | 10 repository + base generica |
| **Migrations** | ✅ | 1 migration (InitialCreate Domain v7) |
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
│   ├── UserEntity.cs                  # Utente sistema
│   ├── BoardEntity.cs                 # Scheda con FirmwareType, DictionaryId?, IsPrimary
│   ├── VariableEntity.cs              # Variabile dizionario (incl. Format)
│   ├── DictionaryEntity.cs            # Dizionario con IsStandard flag
│   ├── BitInterpretationEntity.cs     # Interpretazione bit bitmapped (v7: DictionaryId?)
│   ├── CommandEntity.cs               # Comando protocollo (ParametersJson)
│   ├── CommandDeviceStateEntity.cs    # Stato comando per device
│   ├── DeviceEntity.cs                # Dispositivo STEM
│   ├── StandardVariableOverrideEntity.cs # Override per-dizionario variabile standard (v7)
│   └── AuditEntryEntity.cs            # Audit trail (no IAuditable)
├── Interfaces/
│   ├── IAuditable.cs                  # Interface per audit fields
│   ├── IRepository.cs                 # Interface generica CRUD
│   ├── IUserRepository.cs
│   ├── IBoardRepository.cs
│   ├── IDictionaryRepository.cs
│   ├── IVariableRepository.cs
│   ├── ICommandRepository.cs
│   ├── IBitInterpretationRepository.cs
│   ├── ICommandDeviceStateRepository.cs
│   ├── IDeviceRepository.cs
│   ├── IStandardVariableOverrideRepository.cs
│   └── IAuditEntryRepository.cs
├── Repositories/
│   ├── RepositoryBase.cs              # Implementazione CRUD comune
│   ├── UserRepository.cs
│   ├── BoardRepository.cs
│   ├── DictionaryRepository.cs
│   ├── VariableRepository.cs
│   ├── CommandRepository.cs
│   ├── BitInterpretationRepository.cs
│   ├── CommandDeviceStateRepository.cs
│   ├── DeviceRepository.cs
│   ├── StandardVariableOverrideRepository.cs
│   └── AuditEntryRepository.cs
├── Migrations/
│   └── InitialCreate                  # Schema completo Domain v7
├── AppDbContext.cs                    # DbContext con audit automatico (10 DbSet)
├── DatabaseSeeder.cs                  # Dati demo per sviluppo
├── DesignTimeDbContextFactory.cs      # Factory per migrations CLI
└── DependencyInjection.cs             # Extension method AddInfrastructure()
```

---

## API / Componenti

### Entities

| Entity | Tabella | IAuditable | Note |
|--------|---------|:----------:|------|
| `UserEntity` | Users | ✅ | Username univoco |
| `BoardEntity` | Boards | ✅ | FirmwareType, DictionaryId?, IsPrimary, ProtocolAddress |
| `VariableEntity` | Variables | ✅ | FK → Dictionary, Format, unique (DictionaryId, AddressHigh, AddressLow) |
| `DictionaryEntity` | Dictionaries | ✅ | IsStandard flag, Name univoco |
| `BitInterpretationEntity` | BitInterpretations | ✅ | FK → Variable, Dictionary? (v7: DictionaryId?) |
| `CommandEntity` | Commands | ✅ | ParametersJson, unique (CodeHigh, CodeLow, IsResponse) |
| `CommandDeviceStateEntity` | CommandDeviceStates | ✅ | FK → Command, DeviceType, unique (CommandId, DeviceType) |
| `DeviceEntity` | Devices | ✅ | Name, MachineCode, Description |
| `StandardVariableOverrideEntity` | StandardVariableOverrides | ✅ | FK → Dictionary, Variable, unique (DictionaryId, StandardVariableId) BR-010 |
| `AuditEntryEntity` | AuditEntries | ❌ | Immutabile, FK → User |

### Repository Interfaces

| Interface | Metodi Custom |
|-----------|---------------|
| `IRepository<T>` | GetByIdAsync, GetAllAsync, AddAsync, UpdateAsync, DeleteAsync |
| `IUserRepository` | GetByUsernameAsync |
| `IBoardRepository` | GetByDeviceTypeAsync, GetByProtocolAddressAsync |
| `IDictionaryRepository` | GetByNameAsync, GetWithVariablesAsync, GetStandardDictionaryAsync, ExistsAsync |
| `IVariableRepository` | GetByDictionaryIdAsync, GetByAddressAsync, GetWithBitInterpretationsAsync, ExistsAsync |
| `ICommandRepository` | GetByCodeAsync, GetWithDeviceStatesAsync |
| `IBitInterpretationRepository` | GetByVariableIdAsync, GetByVariableAndDictionaryAsync, SyncByVariableIdAsync |
| `ICommandDeviceStateRepository` | GetByCommandAndDeviceAsync, GetByCommandIdAsync |
| `IDeviceRepository` | GetByNameAsync, GetByMachineCodeAsync |
| `IStandardVariableOverrideRepository` | GetByDictionaryIdAsync, GetByDictionaryAndVariableAsync, GetByVariableIdAsync |
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

→ [Infrastructure/ISSUES.md](./ISSUES.md) — 2 issue aperte, 7 risolte

---

## Links

- [Core/README.md](../Core/README.md) - Modelli dominio ed enums
- [Docs/ER-schema.puml](../Docs/ER-schema.puml) - Schema ER database
- [.copilot/copilot-instructions.md](../.copilot/copilot-instructions.md) - Architettura e decisioni
