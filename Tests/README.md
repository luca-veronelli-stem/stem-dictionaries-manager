# Tests

> **Suite di test xUnit per Stem.Dictionaries.Manager — Unit e Integration tests.**  
> **Ultimo aggiornamento:** 2026-04-08

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
| **Unit Tests** | ✅ | ~750 test per Core + Services/Mapping + GUI (14 ViewModels) |
| **Integration Tests** | ✅ | ~410 test per Infrastructure + Services + GUI + E2E |
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
│   │   ├── AccessModeTests.cs        # 3 test
│   │   ├── AuditEntityTypeTests.cs   # 3 test
│   │   ├── AuditOperationTests.cs    # 3 test
│   │   ├── DataTypeKindTests.cs      # 4 test
│   │   └── VariableCategoryTests.cs  # 3 test
│   ├── Models/
│   │   ├── AuditEntryTests.cs        # 6 test
│   │   ├── BitInterpretationTests.cs # 6 test
│   │   ├── BoardTests.cs             # 18 test (FirmwareType, DictionaryId?, IsPrimary, DictionaryName)
│   │   ├── CommandDeviceStateTests.cs# 5 test
│   │   ├── CommandTests.cs           # 7 test
│   │   ├── DeviceTests.cs            # 10 test (Name, MachineCode, Description)
│   │   ├── DictionaryTests.cs        # 16 test (IsStandard, Restore validation)
│   │   ├── UserTests.cs              # 7 test
│   │   └── VariableTests.cs          # 15 test
│   ├── Infrastructure/
│   │   └── DependencyInjectionTests.cs    # 14 test
│   ├── GUI/                               # Test GUI (solo Windows)
│   │   ├── Mocks/
│   │   │   ├── MockServices.cs            # Mock per GUI services
│   │   │   └── MockDataServices.cs        # Mock per data services
│   │   ├── ViewModels/
│   │   │   ├── MainViewModelTests.cs             # 32 test (login/logout, nav, status bar, unsaved changes guard)
│   │   │   ├── LoginViewModelTests.cs            # 8 test
│   │   │   ├── DeviceListViewModelTests.cs       # 12 test
│   │   │   ├── DeviceEditViewModelTests.cs       # 27 test (Name, MachineCode, Cancel+HasChanges, Delete)
│   │   │   ├── DeviceDetailViewModelTests.cs     # 32 test
│   │   │   ├── DictionaryListViewModelTests.cs   # 14 test
│   │   │   ├── DictionaryEditViewModelTests.cs   # 54 test (form + variabili + standard section + CanSetStandard)
│   │   │   ├── VariableEditViewModelTests.cs     # 106 test (Bitmapped + AddressHigh + DictionaryContext override)
│   │   │   ├── WordBitGroupTests.cs              # 20 test
│   │   │   ├── CommandListViewModelTests.cs      # 13 test
│   │   │   ├── CommandEditViewModelTests.cs      # 52 test (CodeHigh + Delete + Cancel + Params)
│   │   │   ├── CommandParameterItemTests.cs      # 7 test
│   │   │   ├── BoardEditViewModelTests.cs        # 29 test (FirmwareType, DictionaryId?, Cancel, Delete)
│   │   │   ├── DeviceCommandsViewModelTests.cs   # 19 test (load, save, HasChanges)
│   │   │   ├── UserListViewModelTests.cs         # 18 test
│   │   │   └── SettingsViewModelTests.cs         # 3 test
│   │   ├── Converters/
│   │   │   ├── NullableNumericConverterTests.cs  # 20 test
│   │   │   ├── BoolToErrorBrushConverterTests.cs # 3 test
│   │   │   └── SeverityToColorConverterTests.cs  # 2 test
│   │   ├── Services/
│   │   │   └── NavigationServiceTests.cs         # 23 test (history + ViewModel caching)
│   │   └── DependencyInjectionTests.cs           # 23 test
│   └── Services/
│       ├── DependencyInjectionTests.cs        # 13 test
│       └── Mapping/
│           ├── UserMapperTests.cs             # 10 test
│           ├── BoardMapperTests.cs            # 8 test (incl. DictionaryName)
│           ├── VariableMapperTests.cs         # 10 test
│           ├── CommandMapperTests.cs          # 13 test
│           ├── DictionaryMapperTests.cs       # 12 test
│           ├── DeviceMapperTests.cs               # 12 test
│           ├── BitInterpretationMapperTests.cs    # 10 test
│           ├── CommandDeviceStateMapperTests.cs   # 11 test
│           ├── StandardVariableOverrideMapperTests.cs # 8 test
│           └── AuditEntryMapperTests.cs           # 10 test
└── Integration/
    ├── IntegrationTestBase.cs        # Base class SQLite in-memory (IAsyncLifetime)
    ├── Infrastructure/
    │   ├── AuditEntryRepositoryTests.cs       # 8 test
    │   ├── AuditFieldsTests.cs                # 3 test
    │   ├── BoardRepositoryTests.cs            # 10 test
    │   ├── CommandRepositoryTests.cs          # 12 test
    │   ├── CrudScenariosTests.cs              # 17 test
    │   ├── DatabaseCreationTests.cs           # 2 test
    │   ├── DeviceRepositoryTests.cs           # 11 test
    │   ├── DictionaryRepositoryTests.cs       # 14 test
    │   ├── UserRepositoryTests.cs             # 9 test
    │   ├── BitInterpretationRepositoryTests.cs    # 13 test
    │   └── CommandDeviceStateRepositoryTests.cs   # 13 test
    ├── Services/
    │   ├── UserServiceTests.cs            # 15 test
    │   ├── DictionaryServiceTests.cs      # 20 test (IsStandard uniqueness)
    │   ├── BoardServiceTests.cs           # 18 test
    │   ├── CommandServiceTests.cs         # 22 test
    │   ├── DeviceServiceTests.cs          # 16 test
    │   ├── VariableServiceTests.cs        # 50 test (StandardVariableOverride BR-009/010/011/018/020)
    │   └── AuditServiceTests.cs           # 20 test (query + log + validation + full trail)
    ├── E2E/                               # End-to-end workflow tests
    │   ├── AuditTrailTests.cs             # Audit trail workflow
    │   ├── CommandWorkflowTests.cs        # Comandi workflow completo
    │   ├── DatabaseSeederTests.cs         # 21 test — verifica dati seed (Standard + Pulsantiere)
    │   ├── DeviceWorkflowTests.cs         # Dispositivi workflow
    │   ├── DictionaryWorkflowTests.cs     # Dizionari workflow
    │   └── VariableWorkflowTests.cs       # Variabili workflow
    └── GUI/                               # Solo Windows
        ├── LoginFlowTests.cs             # Login/logout flow
        ├── DeviceFlowTests.cs            # CRUD dispositivi flow
        ├── DeviceDetailFlowTests.cs      # Dettaglio device flow
        ├── DeviceCommandsFlowTests.cs    # Stato comandi per device flow
        ├── DictionaryEditFlowTests.cs    # Dizionario + variabili flow
        ├── BoardEditFlowTests.cs         # Board edit flow
        ├── BitInterpretationFlowTests.cs # Bitmapped flow
        ├── CommandEditFlowTests.cs       # Flow completo save/load comandi con parametri
        └── VariableEditFlowTests.cs      # Flow completo + bitmapped + AddressHigh
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
| Unit/Enums | 11 | Valori, count, casting |
| Unit/Models | 90 | Costruttori, validazione, metodi (IsStandard, FirmwareType, DictionaryName, Device) |
| Unit/Services/Mapping | 94 | Mapper Entity ↔ Domain (9 mapper incl. StandardVariableOverrideMapper) |
| Unit/Infrastructure/DI | 14 | Registrazione DI repositories |
| Unit/Services/DI | 11 | Registrazione DI services |
| Unit/GUI/ViewModels | 450 | 14 ViewModels (incl. DictionaryContext override, status bar, unsaved changes) |
| Unit/GUI/Converters | 25 | NullableInt/Double + SeverityToColor + BoolToErrorBrush converters |
| Unit/GUI/Services | 23 | NavigationService (incl. ViewModel caching) |
| Unit/GUI/DI | 23 | Registrazione ViewModels + UI services |
| Integration/Infrastructure | 115 | Repository, audit, DB, CRUD scenarios, SyncByVariableId, DeviceRepository |
| Integration/Services | 140 | Business logic, IsStandard, StandardVariableOverride, DeviceService, smart update |
| Integration/GUI | 25 | VariableEdit + CommandEdit + DictionaryEdit flow completo |
| Integration/E2E | 45 | Workflow completi + DatabaseSeeder tests |
| **Totale metodi test** | **~1160** | Tutti i target combinati |

> **Nota:** I metodi `[Theory]` con `[InlineData]` generano più test case nel runner xUnit. Il conteggio effettivo nel test runner è **~1812 test cases** (multi-target).

---

## Multi-Target

Il progetto supporta due target framework:

| Target | Piattaforma | Include | Uso |
|--------|-------------|---------|-----|
| `net10.0` | Cross-platform | Core, Infrastructure, Services | CI/CD (Linux) |
| `net10.0-windows` | Windows | + GUI.Windows (~560 metodi test) | Test locali + GUI |

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

→ [Tests/ISSUES.md](./ISSUES.md) — 1 issue aperta, 10 risolte

---

## Links

- [Core/README.md](../Core/README.md) - Modelli ed enum testati
- [Infrastructure/README.md](../Infrastructure/README.md) - Repository testati
- [Docs/Standards/Templates/](../Docs/Standards/Templates/) - Template documentazione
