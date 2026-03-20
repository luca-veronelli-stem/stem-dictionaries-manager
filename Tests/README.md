# Tests

> **Suite di test xUnit per Stem.Dictionaries.Manager — Unit e Integration tests.**  
> **Ultimo aggiornamento:** 2026-03-20

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
| **Unit Tests** | ✅ | ~450 test per Core + Services/Mapping + GUI |
| **Integration Tests** | ✅ | ~200 test per Infrastructure + Services + GUI |
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
| Services | Business logic (futuro) |
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
│   │   ├── AuditEntryTests.cs        # 12 test
│   │   ├── BitInterpretationTests.cs # 8 test
│   │   ├── BoardTests.cs             # 10 test
│   │   ├── BoardTypeTests.cs         # 8 test
│   │   ├── CommandDeviceStateTests.cs# 6 test
│   │   ├── CommandTests.cs           # 10 test
│   │   ├── DictionaryTests.cs        # 14 test
│   │   ├── UserTests.cs              # 8 test
│   │   └── VariableTests.cs          # 17 test
│   ├── Infrastructure/
│   │   └── DependencyInjectionTests.cs    # 13 test
│   ├── GUI/                               # Test GUI (solo Windows)
│   │   ├── Mocks/
│   │   │   ├── MockServices.cs            # Mock per GUI services
│   │   │   └── MockDataServices.cs        # Mock per data services (4 mock)
│   │   ├── ViewModels/
│   │   │   ├── DictionaryListViewModelTests.cs   # 14 test
│   │   │   ├── DictionaryEditViewModelTests.cs   # 17 test
│   │   │   ├── MainViewModelTests.cs             # 7 test
│   │   │   ├── VariableListViewModelTests.cs     # 14 test
│   │   │   ├── VariableEditViewModelTests.cs     # 54 test
│   │   │   ├── WordBitGroupTests.cs               # 9 test
│   │   │   ├── CommandListViewModelTests.cs      # 14 test
│   │   │   ├── CommandEditViewModelTests.cs      # 16 test
│   │   │   ├── BoardListViewModelTests.cs        # 13 test
│   │   │   ├── BoardEditViewModelTests.cs        # 14 test
│   │   │   ├── UserListViewModelTests.cs         # 14 test
│   │   │   └── SettingsViewModelTests.cs         # 3 test
│   │   ├── Converters/
│   │   │   └── NullableNumericConverterTests.cs  # 18 test
│   │   ├── Services/
│   │   │   └── NavigationServiceTests.cs         # 12 test
│   │   └── DependencyInjectionTests.cs           # 21 test
│   └── Services/
├── DependencyInjectionTests.cs    # 10 test
│       └── Mapping/
│           ├── UserMapperTests.cs             # 10 test
│           ├── BoardTypeMapperTests.cs        # 10 test
│           ├── VariableMapperTests.cs         # 11 test
│           ├── CommandMapperTests.cs          # 14 test
│           ├── DictionaryMapperTests.cs       # 15 test
│           ├── BitInterpretationMapperTests.cs    # 10 test
│           └── CommandDeviceStateMapperTests.cs   # 10 test
└── Integration/
    ├── IntegrationTestBase.cs        # Base class SQLite in-memory (IAsyncLifetime)
    ├── Infrastructure/
    │   ├── AuditEntryRepositoryTests.cs       # 5 test
    │   ├── AuditFieldsTests.cs                # 4 test
    │   ├── BoardRepositoryTests.cs            # 12 test
    │   ├── BoardTypeRepositoryTests.cs        # 10 test
    │   ├── CommandRepositoryTests.cs          # 11 test
    ├── CrudScenariosTests.cs              # 18 test
    │   ├── DatabaseCreationTests.cs           # 3 test
    │   ├── DictionaryRepositoryTests.cs       # 15 test
    │   ├── UserRepositoryTests.cs             # 6 test
    │   ├── BitInterpretationRepositoryTests.cs    # 14 test
    │   └── CommandDeviceStateRepositoryTests.cs   # 10 test
    └── Services/
        ├── UserServiceTests.cs            # 16 test
        ├── DictionaryServiceTests.cs      # 17 test
        ├── BoardServiceTests.cs           # 17 test
        ├── CommandServiceTests.cs         # 15 test
        └── VariableServiceTests.cs        # 28 test
```

> **Nota:** `Integration/GUI/VariableEditFlowTests.cs` (12 test) include mock services inline per test flow completi.

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

| Area | Test | Descrizione |
|------|------|-------------|
| Unit/Enums | 22 | Valori, count, casting |
| Unit/Models | 97 | Costruttori, validazione, metodi |
| Unit/Services/Mapping | 80 | Mapper Entity ↔ Domain (8 mapper) |
| Unit/Infrastructure/DI | 13 | Registrazione DI repositories |
| Unit/Services/DI | 10 | Registrazione DI services |
| Unit/GUI/ViewModels | 206 | 12 ViewModels (incl. WordBitGroup) con CRUD, navigation, validation, bitmapped |
| Unit/GUI/Converters | 18 | NullableInt/Double converters |
| Unit/GUI/Services | 12 | NavigationService |
| Unit/GUI/DI | 21 | Registrazione ViewModels + UI services |
| Integration/Infrastructure | 108 | Repository, audit, DB, CRUD scenarios, SyncByVariableId |
| Integration/Services | 93 | Business logic, validazione, smart update |
| Integration/GUI | 12 | VariableEdit flow completo + bitmapped |
| **Totale CI** | **~450** | net10.0 (Linux) |
| **Totale Windows** | **1160** | net10.0 + net10.0-windows |

---

## Multi-Target

Il progetto supporta due target framework:

| Target | Piattaforma | Include | Uso |
|--------|-------------|---------|-----|
| `net10.0` | Cross-platform | Core, Infrastructure, Services | CI/CD (Linux) |
| `net10.0-windows` | Windows | + GUI.Windows (63 test) | Test locali + GUI |

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

→ [Tests/ISSUES.md](./ISSUES.md) — 1 issue aperta, 5 risolte (0 critiche, 0 alte, 0 medie, 1 bassa)

---

## Links

- [Core/README.md](../Core/README.md) - Modelli ed enum testati
- [Infrastructure/README.md](../Infrastructure/README.md) - Repository testati
- [Docs/Standards/Templates/](../Docs/Standards/Templates/) - Template documentazione
