# Tests

> **Suite di test xUnit per Stem.Dictionaries.Manager — Unit e Integration tests.**  
> **Ultimo aggiornamento:** 2026-03-18

---

## Panoramica

Il progetto **Tests** contiene tutti i test automatizzati per la soluzione Stem.Dictionaries.Manager:

- **Unit Tests** - Test isolati per modelli, enum e mapper (Core, Services/Mapping)
- **Integration Tests** - Test con database SQLite in-memory (Infrastructure, Services)

I test sono eseguibili cross-platform (Linux CI) e su Windows con target multipli.

---

## Caratteristiche

| Feature | Stato | Descrizione |
|---------|-------|-------------|
| **Unit Tests** | ✅ | 202 test per Core + Services/Mapping |
| **Integration Tests** | ✅ | 550 test per Infrastructure + Services |
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
│   └── Services/
│       └── Mapping/
│           ├── UserMapperTests.cs             # 10 test
│           ├── BoardTypeMapperTests.cs        # 10 test
│           ├── VariableMapperTests.cs         # 11 test
│           ├── CommandMapperTests.cs          # 14 test
│           ├── DictionaryMapperTests.cs       # 15 test
│           ├── BitInterpretationMapperTests.cs    # 10 test ✨
│           └── CommandDeviceStateMapperTests.cs   # 10 test ✨
└── Integration/
    ├── IntegrationTestBase.cs        # Base class SQLite in-memory
    ├── Infrastructure/
    │   ├── AuditEntryRepositoryTests.cs       # 5 test
    │   ├── AuditFieldsTests.cs                # 4 test
    │   ├── BoardRepositoryTests.cs            # 12 test
    │   ├── BoardTypeRepositoryTests.cs        # 10 test
    │   ├── CommandRepositoryTests.cs          # 11 test
    │   ├── DatabaseCreationTests.cs           # 3 test
    │   ├── DictionaryRepositoryTests.cs       # 15 test (6+9 per SVC-001)
    │   ├── UserRepositoryTests.cs             # 6 test
    │   ├── BitInterpretationRepositoryTests.cs    # 10 test ✨
    │   └── CommandDeviceStateRepositoryTests.cs   # 10 test ✨
    └── Services/
        ├── UserServiceTests.cs            # 16 test
        ├── DictionaryServiceTests.cs      # 17 test
        ├── BoardServiceTests.cs           # 17 test
        ├── CommandServiceTests.cs         # 15 test
        └── VariableServiceTests.cs        # 23 test ✨
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

| Area | Test | Descrizione |
|------|------|-------------|
| Unit/Enums | 25 | Valori, count, casting |
| Unit/Models | 97 | Costruttori, validazione, metodi |
| Unit/Services/Mapping | 80 | Mapper Entity ↔ Domain (8 mapper) |
| Integration/Infrastructure | 86 | Repository, audit, DB |
| Integration/Services | 88 | Business logic, validazione |
| **Totale CI** | **376** | net10.0 (Linux) |
| **Totale Windows** | **376** | net10.0-windows (include GUI futuro) |

---

## Multi-Target

Il progetto supporta due target framework:

| Target | Piattaforma | Include | Uso |
|--------|-------------|---------|-----|
| `net10.0` | Cross-platform | Core, Infrastructure, Services | CI/CD (Linux) |
| `net10.0-windows` | Windows | + GUI.Windows | Test locali + GUI |

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

→ [Tests/ISSUES.md](./ISSUES.md) — 3 issue aperte, 3 risolte (0 critiche, 0 alte, 0 medie, 3 basse)

---

## Links

- [Core/README.md](../Core/README.md) - Modelli ed enum testati
- [Infrastructure/README.md](../Infrastructure/README.md) - Repository testati
- [Docs/Standards/Templates/](../Docs/Standards/Templates/) - Template documentazione
