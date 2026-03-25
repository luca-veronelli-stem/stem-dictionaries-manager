# Tests

> **Suite di test xUnit per Stem.Dictionaries.Manager — Unit e Integration tests.**  
> **Ultimo aggiornamento:** 2026-03-25

---

## Panoramica

Il progetto **Tests** contiene tutti i test automatizzati per la soluzione Stem.Dictionaries.Manager:

- **Unit Tests** - Test isolati per modelli, enum, mapper e ViewModels (Core, Services/Mapping, GUI)
- **Integration Tests** - Test con database SQLite in-memory (Infrastructure, Services)

I test sono eseguibili cross-platform (Linux CI) e su Windows con target multipli.

---

## Caratteristiche

| Feature | Stato | Descrizione |
|---------|-------|-------------|
| **Unit Tests** | ✅ | ~500 test per Core + Services/Mapping + GUI (15 ViewModels) |
| **Integration Tests** | ✅ | ~230 test per Infrastructure + Services + GUI |
| **Multi-target** | ✅ | net10.0 (CI/Linux) + net10.0-windows (GUI tests) |
| **SQLite In-Memory** | ✅ | DB pulito per ogni test |
| **IntegrationTestBase** | ✅ | Base class per setup/teardown (IAsyncLifetime) |

---

## Requisiti

- **.NET 10.0** o superiore
- **xUnit 2.9.3**

### Dipendenze

| Package | Versione | Uso |
|---------|----------|-----|
| xunit | 2.9.3 | Framework test |
| xunit.runner.visualstudio | 3.1.4 | Test runner VS |
| Microsoft.NET.Test.Sdk | 17.14.1 | Test SDK |
| Microsoft.EntityFrameworkCore.Sqlite | 10.0.5 | DB in-memory |
| coverlet.collector | 6.0.4 | Code coverage |

### Dipendenze Progetto

| Progetto | Uso |
|----------|-----|
| Core | Modelli ed enum da testare |
| Infrastructure | Repositories e DbContext |
| Services | Business logic, Mapper, DI |
| GUI.Windows | UI tests (solo Windows) |

---

## Quick Start

```bash
# Eseguire tutti i test (CI, cross-platform)
dotnet test Tests/Tests.csproj --framework net10.0

# Eseguire tutti i test (Windows, include GUI)
dotnet test Tests/Tests.csproj --framework net10.0-windows

# Eseguire con output dettagliato
dotnet test Tests/Tests.csproj -v normal

# Eseguire test specifici
dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~VariableTests"

# Con code coverage
dotnet test Tests/Tests.csproj --collect:"XPlat Code Coverage"
```

---

## Struttura

```
Tests/
├── Unit/
│   ├── Enums/
│   │   ├── AccessModeTests.cs        # 4 test
│   │   ├── AuditEntityTypeTests.cs   # 4 test
│   │   ├── AuditOperationTests.cs    # 4 test
│   │   ├── DataTypeKindTests.cs      # 4 test
│   │   ├── DeviceTypeTests.cs        # 5 test
│   │   └── VariableCategoryTests.cs  # 4 test
│   ├── Models/
│   │   ├── AuditEntryTests.cs        # 6 test
│   │   ├── BitInterpretationTests.cs # 6 test
│   │   ├── BoardTests.cs             # 13 test (FirmwareType, DictionaryId?, IsPrimary)
│   │   ├── CommandDeviceStateTests.cs# 5 test
│   │   ├── CommandTests.cs           # 7 test
│   │   ├── DictionaryTests.cs        # 17 test (IsStandard, Restore validation)
│   │   ├── UserTests.cs              # 7 test
│   │   ├── VariableTests.cs          # 15 test
│   │   └── VariableDeviceStateTests.cs # 8 test (BR-009/010/011)
│   ├── Infrastructure/
│   │   └── DependencyInjectionTests.cs    # 14 test
│   ├── GUI/                               # Test GUI (solo Windows)
│   │   ├── Mocks/
│   │   │   ├── MockServices.cs            # Mock per GUI services
│   │   │   └── MockDataServices.cs        # Mock per data services
│   │   ├── ViewModels/
│   │   ├── MainViewModelTests.cs             # 17 test (login/logout, nav, error handling)
│   │   │   ├── LoginViewModelTests.cs            # 8 test
│   │   │   ├── DeviceListViewModelTests.cs       # 12 test
│   │   │   ├── DeviceDetailViewModelTests.cs     # 20 test
│   │   │   ├── DictionaryListViewModelTests.cs   # 18 test
│   │   │   ├── DictionaryEditViewModelTests.cs   # 22 test (IsStandard)
│   │   │   ├── VariableListViewModelTests.cs     # 16 test
│   │   │   ├── VariableEditViewModelTests.cs     # 50 test (Bitmapped + DeviceStates)
│   │   │   ├── WordBitGroupTests.cs              # 9 test
│   │   │   ├── CommandListViewModelTests.cs      # 16 test
│   │   │   ├── CommandEditViewModelTests.cs      # 14 test
│   │   │   ├── BoardListViewModelTests.cs        # 14 test
│   │   │   ├── BoardEditViewModelTests.cs        # 17 test (FirmwareType, DictionaryId?)
│   │   │   ├── UserListViewModelTests.cs         # 18 test
│   │   │   └── SettingsViewModelTests.cs         # 3 test
│   │   ├── Converters/
│   │   │   └── NullableNumericConverterTests.cs  # 20 test
│   │   ├── Services/
│   │   │   └── NavigationServiceTests.cs         # 15 test
│   │   └── DependencyInjectionTests.cs           # 22 test
│   └── Services/
│       ├── DependencyInjectionTests.cs        # 10 test
│       └── Mapping/
│           ├── UserMapperTests.cs             # 10 test
│           ├── BoardMapperTests.cs            # 10 test
│           ├── VariableMapperTests.cs         # 10 test
│           ├── CommandMapperTests.cs          # 13 test
│           ├── DictionaryMapperTests.cs       # 14 test
│           ├── BitInterpretationMapperTests.cs    # 10 test
│           ├── CommandDeviceStateMapperTests.cs   # 11 test
│           └── VariableDeviceStateMapperTests.cs  # 9 test
└── Integration/
    ├── IntegrationTestBase.cs        # Base class SQLite in-memory (IAsyncLifetime)
    ├── Infrastructure/
    │   ├── AuditEntryRepositoryTests.cs       # 5 test
    │   ├── AuditFieldsTests.cs                # 3 test
    │   ├── BoardRepositoryTests.cs            # 11 test
    │   ├── CommandRepositoryTests.cs          # 12 test
    │   ├── CrudScenariosTests.cs              # 18 test
    │   ├── DatabaseCreationTests.cs           # 2 test
    │   ├── DictionaryRepositoryTests.cs       # 14 test
    │   ├── UserRepositoryTests.cs             # 9 test
    │   ├── BitInterpretationRepositoryTests.cs    # 13 test
    │   ├── CommandDeviceStateRepositoryTests.cs   # 10 test
    │   └── VariableDeviceStateRepositoryTests.cs  # 10 test
    ├── Services/
    │   ├── UserServiceTests.cs            # 15 test
    │   ├── DictionaryServiceTests.cs      # 21 test (IsStandard uniqueness)
    │   ├── BoardServiceTests.cs           # 23 test
    │   ├── CommandServiceTests.cs         # 18 test
    │   └── VariableServiceTests.cs        # 37 test (DeviceStates BR-009/010/011)
    └── GUI/                               # Solo Windows
        └── VariableEditFlowTests.cs       # 11 test (flow completo + bitmapped)
```

---

## Convenzioni

### Naming

- **Classi:** `{Classe}Tests` (es. `VariableTests`)
- **Metodi:** `{Method}_{Scenario}_{ExpectedResult}`

```csharp
// Esempi
Constructor_ValidInput_CreatesVariable()
Constructor_NullName_ThrowsArgumentException()
AddVariable_DuplicateAddress_ThrowsInvalidOperation()
```

### Pattern AAA

```csharp
[Fact]
public void Constructor_ValidInput_CreatesVariable()
{
    // Arrange
    var name = "Temperatura";
    var addressHigh = (byte)0x80;
    
    // Act
    var variable = new Variable(name, addressHigh, ...);
    
    // Assert
    Assert.Equal(name, variable.Name);
}
```

### Integration Test Base

```csharp
public class MyRepositoryTests : IntegrationTestBase
{
    [Fact]
    public async Task GetByIdAsync_ExistingEntity_ReturnsEntity()
    {
        // Context è già inizializzato con DB pulito
        var repository = new MyRepository(Context);
        
        // ... test
    }
}
```

---

## Conteggi Test

| Area | Metodi Test | Descrizione |
|------|-------------|-------------|
| Unit/Enums | 14 | Valori, count, casting |
| Unit/Models | 84 | Costruttori, validazione, metodi (IsStandard, FirmwareType, DeviceStates) |
| Unit/Services/Mapping | 87 | Mapper Entity ↔ Domain (9 mapper incl. VariableDeviceState) |
| Unit/Infrastructure/DI | 14 | Registrazione DI repositories |
| Unit/Services/DI | 10 | Registrazione DI services |
| Unit/GUI/ViewModels | 252 | 15 ViewModels (incl. WordBitGroup, Device*, Login) |
| Unit/GUI/Converters | 20 | NullableInt/Double converters |
| Unit/GUI/Services | 15 | NavigationService |
| Unit/GUI/DI | 22 | Registrazione ViewModels + UI services |
| Integration/Infrastructure | 107 | Repository, audit, DB, CRUD scenarios, SyncByVariableId |
| Integration/Services | 114 | Business logic, IsStandard, DeviceStates, smart update |
| Integration/GUI | 11 | VariableEdit flow completo + bitmapped |
| **Totale metodi test** | **~750** | Tutti i target combinati |

> **Nota:** I metodi `[Theory]` con `[InlineData]` generano più test case nel runner xUnit. Il conteggio effettivo nel test runner è superiore ai 736 metodi elencati.

---

## Multi-Target

Il progetto supporta due target framework:

| Target | Piattaforma | Include | Uso |
|--------|-------------|---------|-----|
| `net10.0` | Cross-platform | Core, Infrastructure, Services | CI/CD (Linux) |
| `net10.0-windows` | Windows | + GUI.Windows (~320 metodi test) | Test locali + GUI |

```xml
<!-- Tests.csproj -->
<TargetFrameworks>net10.0;net10.0-windows</TargetFrameworks>

<!-- GUI.Windows solo per Windows -->
<ItemGroup Condition="'$(TargetFramework)' == 'net10.0-windows'">
  <ProjectReference Include="..\GUI.Windows\GUI.Windows.csproj" />
</ItemGroup>
```

---

## Esecuzione CI

```yaml
# bitbucket-pipelines.yml
- step:
    name: Test
    script:
      - dotnet test Tests/Tests.csproj --framework net10.0 --logger "console;verbosity=normal"
```

---

## Issue Correlate

→ [Tests/ISSUES.md](./ISSUES.md) — 1 issue aperta, 8 risolte (0 critiche, 0 alte, 0 medie, 1 bassa)

---

## Links

- [Core/README.md](../Core/README.md) - Modelli ed enum testati
- [Infrastructure/README.md](../Infrastructure/README.md) - Repository testati
- [Docs/Standards/Templates/](../Docs/Standards/Templates/) - Template documentazione
