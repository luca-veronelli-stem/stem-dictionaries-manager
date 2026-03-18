# Tests - ISSUES

> **Scopo:** Questo documento traccia problemi di struttura, copertura, significatività e consistenza per la suite di test del progetto **Stem.Dictionaries.Manager**.

> **Ultimo aggiornamento:** 2026-03-18

---

## Riepilogo

| Priorità | Aperte | Risolte |
|----------|--------|---------|
| **Critica** | 0 | 0 |
| **Alta** | 0 | 0 |
| **Media** | 3 | 0 |
| **Bassa** | 3 | 0 |

**Totale aperte:** 6  
**Totale risolte:** 0

---

## Indice Issue Aperte

- [TEST-001 - Mancano test per BoardRepository e CommandRepository](#test-001--mancano-test-per-boardrepository-e-commandrepository)
- [TEST-002 - Mancano test per BoardTypeRepository](#test-002--mancano-test-per-boardtyperepository)
- [TEST-003 - IntegrationTestBase.SetupTestUser usa .Wait() bloccante](#test-003--integrationtestbasesetuptestuser-usa-wait-bloccante)
- [TEST-004 - Manca cartella Unit/Infrastructure per test DependencyInjection](#test-004--manca-cartella-unitinfrastructure-per-test-dependencyinjection)
- [TEST-005 - Mancano test per scenari di rilavorazione/update entities](#test-005--mancano-test-per-scenari-di-rilavorazioneupdate-entities)
- [TEST-006 - Magic strings ripetute nei test](#test-006--magic-strings-ripetute-nei-test)

## Indice Issue Risolte

*(Nessuna issue risolta)*

---

## Copertura Attuale

| Componente | Unit | Integration | Copertura |
|------------|------|-------------|-----------|
| Core/Enums (6) | ✅ 25 | - | 100% |
| Core/Models (9) | ✅ 97 | - | 100% |
| Infrastructure/Repositories (7) | - | ⚠️ 23 | ~60% |
| Services (0) | - | - | N/A |
| GUI.Windows | - | - | N/A |

---

## Priorità Media

### TEST-001 - Mancano test per BoardRepository e CommandRepository

**Categoria:** Copertura  
**Priorità:** Media  
**Impatto:** Medio  
**Status:** Aperto  
**Data Apertura:** 2026-03-18  

#### Descrizione

I repository `BoardRepository` e `CommandRepository` non hanno file di test dedicati. Solo 3 dei 7 repository hanno integration tests.

#### File Mancanti

- `Tests/Integration/Infrastructure/BoardRepositoryTests.cs`
- `Tests/Integration/Infrastructure/CommandRepositoryTests.cs`

#### Repository Testati vs Non Testati

| Repository | File Test | Status |
|------------|-----------|--------|
| UserRepository | ✅ UserRepositoryTests.cs | Testato |
| DictionaryRepository | ✅ DictionaryRepositoryTests.cs | Testato |
| AuditEntryRepository | ✅ AuditEntryRepositoryTests.cs | Testato |
| BoardRepository | ❌ - | **Non testato** |
| BoardTypeRepository | ❌ - | **Non testato** |
| CommandRepository | ❌ - | **Non testato** |
| VariableRepository | ⚠️ Parziale (in DictionaryRepositoryTests) | Parziale |

#### Soluzione Proposta

Creare test file per i repository mancanti:

```csharp
// Tests/Integration/Infrastructure/BoardRepositoryTests.cs
public class BoardRepositoryTests : IntegrationTestBase
{
    [Fact]
    public async Task GetByDeviceTypeAsync_ReturnsMatchingBoards() { }
    
    [Fact]
    public async Task GetByProtocolAddressAsync_ReturnsBoard() { }
    
    [Fact]
    public async Task AddAsync_CalculatesProtocolAddress() { }
}

// Tests/Integration/Infrastructure/CommandRepositoryTests.cs
public class CommandRepositoryTests : IntegrationTestBase
{
    [Fact]
    public async Task GetByCodeAsync_ReturnsCommand() { }
    
    [Fact]
    public async Task GetWithDeviceStatesAsync_IncludesStates() { }
}
```

#### Benefici Attesi

- Copertura repository completa
- Confidenza nelle query custom
- Regressioni catturate prima

---

### TEST-002 - Mancano test per BoardTypeRepository

**Categoria:** Copertura  
**Priorità:** Media  
**Impatto:** Medio  
**Status:** Aperto  
**Data Apertura:** 2026-03-18  

#### Descrizione

`BoardTypeRepository` ha metodi custom (`GetByNameAsync`, `GetByFirmwareTypeAsync`) non testati.

#### File Mancante

- `Tests/Integration/Infrastructure/BoardTypeRepositoryTests.cs`

#### Metodi Non Testati

```csharp
// BoardTypeRepository.cs
public async Task<BoardTypeEntity?> GetByNameAsync(string name, ...)
public async Task<BoardTypeEntity?> GetByFirmwareTypeAsync(int firmwareType, ...)
```

#### Soluzione Proposta

```csharp
public class BoardTypeRepositoryTests : IntegrationTestBase
{
    [Fact]
    public async Task GetByNameAsync_ExistingName_ReturnsBoardType() { }
    
    [Fact]
    public async Task GetByNameAsync_NotFound_ReturnsNull() { }
    
    [Fact]
    public async Task GetByFirmwareTypeAsync_ExistingType_ReturnsBoardType() { }
    
    [Fact]
    public async Task GetByFirmwareTypeAsync_UniqueConstraint_Enforced() { }
}
```

#### Benefici Attesi

- Verifica comportamento lookup per nome/firmwareType
- Copertura constraint unicità

---

### TEST-003 - IntegrationTestBase.SetupTestUser usa .Wait() bloccante

**Categoria:** Anti-Pattern  
**Priorità:** Media  
**Impatto:** Basso  
**Status:** Aperto  
**Data Apertura:** 2026-03-18  

#### Descrizione

In `AuditEntryRepositoryTests`, il costruttore chiama `SetupTestUser().Wait()` che è un anti-pattern (blocking su async).

#### File Coinvolti

- `Tests/Integration/Infrastructure/AuditEntryRepositoryTests.cs` (righe 18-19)

#### Codice Problematico

```csharp
public AuditEntryRepositoryTests()
{
    _repository = new AuditEntryRepository(Context);
    SetupTestUser().Wait();  // <-- Anti-pattern: blocking call
}

private async Task SetupTestUser()
{
    _testUser = new UserEntity { Username = "admin", DisplayName = "Admin" };
    Context.Users.Add(_testUser);
    await Context.SaveChangesAsync();
}
```

#### Problema Specifico

- `.Wait()` può causare deadlock in alcuni contesti
- Viola la regola "async all the way"
- xUnit supporta costruttori async tramite `IAsyncLifetime`

#### Soluzione Proposta

Implementare `IAsyncLifetime`:

```csharp
public class AuditEntryRepositoryTests : IntegrationTestBase, IAsyncLifetime
{
    private readonly AuditEntryRepository _repository;
    private UserEntity _testUser = null!;

    public AuditEntryRepositoryTests()
    {
        _repository = new AuditEntryRepository(Context);
    }

    public async Task InitializeAsync()
    {
        _testUser = new UserEntity { Username = "admin", DisplayName = "Admin" };
        Context.Users.Add(_testUser);
        await Context.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;
    
    // ... tests
}
```

#### Benefici Attesi

- Elimina anti-pattern blocking
- Coerenza con best practice xUnit
- Previene potenziali deadlock

---

## Priorità Bassa

### TEST-004 - Manca cartella Unit/Infrastructure per test DependencyInjection

**Categoria:** Struttura  
**Priorità:** Bassa  
**Impatto:** Basso  
**Status:** Aperto  
**Data Apertura:** 2026-03-18  

#### Descrizione

Il metodo `AddInfrastructure()` in `DependencyInjection.cs` non ha unit test. La struttura cartelle non prevede `Unit/Infrastructure/`.

#### Struttura Attuale vs Proposta

```
Tests/
├── Unit/
│   ├── Enums/           ✅
│   ├── Models/          ✅
│   └── Infrastructure/  ❌ MANCA
│       └── DependencyInjectionTests.cs
└── Integration/
    └── Infrastructure/  ✅
```

#### Test Mancanti

```csharp
// Tests/Unit/Infrastructure/DependencyInjectionTests.cs
public class DependencyInjectionTests
{
    [Fact]
    public void AddInfrastructure_RegistersAllRepositories()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure("Data Source=:memory:");
        var provider = services.BuildServiceProvider();
        
        Assert.NotNull(provider.GetService<IUserRepository>());
        Assert.NotNull(provider.GetService<IDictionaryRepository>());
        // ... tutti i repository
    }
    
    [Fact]
    public void AddInfrastructure_RegistersDbContext()
    {
        // ...
    }
}
```

#### Benefici Attesi

- Verifica registrazione DI corretta
- Struttura cartelle più consistente
- Cattura errori di configurazione DI

---

### TEST-005 - Mancano test per scenari di rilavorazione/update entities

**Categoria:** Copertura  
**Priorità:** Bassa  
**Impatto:** Medio  
**Status:** Aperto  
**Data Apertura:** 2026-03-18  

#### Descrizione

I test di integrazione coprono principalmente scenari CRUD base (Add, GetById). Mancano test per scenari più complessi.

#### Scenari Non Testati

| Scenario | Descrizione | File Test |
|----------|-------------|-----------|
| Update entity esistente | Modifica e salva | ❌ |
| Delete con FK cascade | Elimina Dictionary → Variables | ❌ |
| Unique constraint violation | Doppio username/name | ⚠️ Parziale |
| Concurrent updates | Due update stesso record | ❌ |
| Audit trail completo | Create → Update → Delete | ❌ |

#### Soluzione Proposta

```csharp
// Tests/Integration/Infrastructure/CrudScenariosTests.cs
public class CrudScenariosTests : IntegrationTestBase
{
    [Fact]
    public async Task UpdateAsync_ModifiesEntity()
    {
        // Arrange
        var user = new UserEntity { Username = "old", DisplayName = "Old" };
        await Context.Users.AddAsync(user);
        await Context.SaveChangesAsync();
        
        // Act
        user.DisplayName = "New";
        Context.Users.Update(user);
        await Context.SaveChangesAsync();
        
        // Assert
        var updated = await Context.Users.FindAsync(user.Id);
        Assert.Equal("New", updated!.DisplayName);
        Assert.NotNull(updated.UpdatedAt);
    }
    
    [Fact]
    public async Task DeleteDictionary_CascadesDeleteToVariables() { }
}
```

#### Benefici Attesi

- Copertura scenari reali di utilizzo
- Verifica comportamento FK
- Confidenza in operazioni complesse

---

### TEST-006 - Magic strings ripetute nei test

**Categoria:** Manutenibilità  
**Priorità:** Bassa  
**Impatto:** Basso  
**Status:** Aperto  
**Data Apertura:** 2026-03-18  

#### Descrizione

Molti test usano magic strings ripetute per nomi, username, dataTypeRaw. Se cambia il formato atteso, bisogna modificare molti file.

#### File Coinvolti

- Tutti i file in `Tests/Unit/Models/`
- Tutti i file in `Tests/Integration/Infrastructure/`

#### Codice Problematico

```csharp
// VariableTests.cs
var variable = new Variable(
    name: "Firmware macchina",
    dataTypeRaw: "uint16_t",  // <-- Ripetuto ovunque
    // ...

// DictionaryTests.cs
var variable = new Variable("Test", 0x00, 0x01, DataTypeKind.UInt8, 
    AccessMode.ReadOnly, "uint8_t");  // <-- Magic string

// UserRepositoryTests.cs
var user = new UserEntity { Username = "luca", DisplayName = "Luca V." };
```

#### Soluzione Proposta

Creare una classe `TestData` con costanti/factory:

```csharp
// Tests/TestData.cs
public static class TestData
{
    public static class DataTypes
    {
        public const string UInt8 = "uint8_t";
        public const string UInt16 = "uint16_t";
        public const string String20 = "String[20]";
    }
    
    public static class Users
    {
        public static UserEntity CreateAdmin() => 
            new() { Username = "admin", DisplayName = "Admin" };
    }
    
    public static Variable CreateVariable(
        string name = "TestVar",
        byte addressHigh = 0x00,
        byte addressLow = 0x01) => 
        new(name, addressHigh, addressLow, DataTypeKind.UInt8, 
            AccessMode.ReadOnly, DataTypes.UInt8);
}
```

#### Benefici Attesi

- Single point of change
- Test più leggibili
- Refactoring più sicuro

---

## Issue Risolte

*(Nessuna issue risolta)*

---

## Wontfix

*(Nessuna issue in wontfix)*
