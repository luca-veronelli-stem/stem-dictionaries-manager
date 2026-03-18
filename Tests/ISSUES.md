# Tests - ISSUES

> **Scopo:** Questo documento traccia problemi di struttura, copertura, significatività e consistenza per la suite di test del progetto **Stem.Dictionaries.Manager**.

> **Ultimo aggiornamento:** 2026-03-18

---

## Riepilogo

| Priorità | Aperte | Risolte |
|----------|--------|---------|
| **Critica** | 0 | 0 |
| **Alta** | 0 | 0 |
| **Media** | 0 | 3 |
| **Bassa** | 1 | 2 |

**Totale aperte:** 1  
**Totale risolte:** 5

---

## Indice Issue Aperte

- [TEST-006 - Magic strings ripetute nei test](#test-006--magic-strings-ripetute-nei-test)

## Indice Issue Risolte

- [TEST-001 - Mancano test per BoardRepository e CommandRepository](#test-001--mancano-test-per-boardrepository-e-commandrepository)
- [TEST-002 - Mancano test per BoardTypeRepository](#test-002--mancano-test-per-boardtyperepository)
- [TEST-003 - Uso di .Wait() bloccante nei costruttori test](#test-003--uso-di-wait-bloccante-nei-costruttori-test)
- [TEST-004 - Mancano test per DependencyInjection](#test-004--mancano-test-per-dependencyinjection-infrastructure-e-services)
- [TEST-005 - Mancano test per scenari di rilavorazione/update entities](#test-005--mancano-test-per-scenari-di-rilavorazioneupdate-entities)

---

## Copertura Attuale

| Componente | Unit | Integration | Copertura |
|------------|------|-------------|-----------|
| Core/Enums (6) | ✅ 25 | - | 100% |
| Core/Models (9) | ✅ 97 | - | 100% |
| Services/Mapping (8) | ✅ 80 | - | ~100% |
| Infrastructure/DI | ✅ 13 | - | 100% |
| Services/DI | ✅ 10 | - | 100% |
| Infrastructure/Repositories (9) | - | ✅ 86 | ~98% |
| Services (5) | - | ✅ 88 | ~95% |
| GUI.Windows (ViewModels, Services, DI) | ✅ 63 | - | ~80% ✨ |

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

### TEST-001 - Mancano test per BoardRepository e CommandRepository

**Categoria:** Copertura  
**Priorità:** Media  
**Impatto:** Medio  
**Status:** Risolto  
**Data Apertura:** 2026-03-18  
**Data Risoluzione:** 2026-03-18  
**Branch:** fix/test-001-002  

#### Descrizione

I repository `BoardRepository` e `CommandRepository` non avevano file di test dedicati.

#### Soluzione Implementata

Creati i file di test:

**`Tests/Integration/Infrastructure/BoardRepositoryTests.cs` (12 test):**
- `AddAsync_CreatesBoard`
- `GetByIdAsync_ReturnsBoard_WithBoardType`
- `GetByIdAsync_NotFound_ReturnsNull`
- `GetByDeviceTypeAsync_ReturnsMatchingBoards`
- `GetByDeviceTypeAsync_NoMatch_ReturnsEmptyList`
- `GetByDeviceTypeAsync_IncludesBoardType`
- `GetByProtocolAddressAsync_ReturnsBoard`
- `GetByProtocolAddressAsync_NotFound_ReturnsNull`
- `GetByProtocolAddressAsync_IncludesBoardType`
- `DeleteAsync_RemovesBoard`
- `DeleteAsync_NotFound_ThrowsKeyNotFoundException`

**`Tests/Integration/Infrastructure/CommandRepositoryTests.cs` (11 test):**
- `AddAsync_CreatesCommand`
- `GetByIdAsync_ReturnsCommand`
- `GetByIdAsync_NotFound_ReturnsNull`
- `GetByCodeAsync_ReturnsCommand`
- `GetByCodeAsync_DistinguishesRequestFromResponse`
- `GetByCodeAsync_NotFound_ReturnsNull`
- `GetWithDeviceStatesAsync_ReturnsCommand_WithDeviceStates`
- `GetWithDeviceStatesAsync_NoStates_ReturnsEmptyCollection`
- `GetWithDeviceStatesAsync_NotFound_ReturnsNull`
- `GetAllAsync_ReturnsAllCommands`
- `DeleteAsync_RemovesCommand`
- `DeleteAsync_NotFound_ThrowsKeyNotFoundException`

#### Benefici Ottenuti

- Copertura repository completa ✅
- Confidenza nelle query custom ✅
- Regressioni catturate prima ✅

---

### TEST-002 - Mancano test per BoardTypeRepository

**Categoria:** Copertura  
**Priorità:** Media  
**Impatto:** Medio  
**Status:** Risolto  
**Data Apertura:** 2026-03-18  
**Data Risoluzione:** 2026-03-18  
**Branch:** fix/test-001-002  

#### Descrizione

`BoardTypeRepository` non aveva test per i metodi custom (`GetByNameAsync`, `GetByFirmwareTypeAsync`).

#### Soluzione Implementata

Creato il file di test:

**`Tests/Integration/Infrastructure/BoardTypeRepositoryTests.cs` (10 test):**
- `AddAsync_CreatesBoardType`
- `GetByIdAsync_ReturnsBoardType`
- `GetByIdAsync_NotFound_ReturnsNull`
- `GetByNameAsync_ExistingName_ReturnsBoardType`
- `GetByNameAsync_NotFound_ReturnsNull`
- `GetByFirmwareTypeAsync_ExistingType_ReturnsBoardType`
- `GetByFirmwareTypeAsync_NotFound_ReturnsNull`
- `GetAllAsync_ReturnsAllBoardTypes`
- `DeleteAsync_RemovesBoardType`
- `DeleteAsync_NotFound_ThrowsKeyNotFoundException`

#### Benefici Ottenuti

- Verifica comportamento lookup per nome/firmwareType ✅
- Copertura metodi custom ✅

---

### TEST-003 - Uso di .Wait() bloccante nei costruttori test

**Categoria:** Anti-Pattern  
**Priorità:** Media  
**Impatto:** Medio  
**Status:** Risolto  
**Data Apertura:** 2026-03-18  
**Data Risoluzione:** 2026-03-18  
**Branch:** fix/test-003  

#### Descrizione

Diversi test di integrazione usavano `.Wait()` nei costruttori per setup asincroni, che è un anti-pattern (blocking su async).

#### File Coinvolti (3 file)

- `Tests/Integration/Infrastructure/AuditEntryRepositoryTests.cs`
- `Tests/Integration/Services/DictionaryServiceTests.cs`
- `Tests/Integration/Services/BoardServiceTests.cs`

#### Soluzione Implementata

1. **IntegrationTestBase** ora implementa `IAsyncLifetime` con metodi virtuali:
   - `InitializeAsync()` - default vuoto, override per setup async
   - `DisposeAsync()` - default vuoto, override per cleanup async

2. **Classi derivate** fanno `override InitializeAsync()` invece di `.Wait()`:

```csharp
// Prima (anti-pattern)
public AuditEntryRepositoryTests()
{
    SetupTestUser().Wait();  // ❌ Blocking call
}

// Dopo (best practice)
public override async Task InitializeAsync()
{
    await SetupTestUser();  // ✅ Async all the way
}
```

#### Benefici Ottenuti

- Eliminato anti-pattern blocking ✅
- Coerenza con best practice xUnit ✅
- Previene potenziali deadlock ✅
- Pattern riusabile per futuri test ✅

---

### TEST-004 - Mancano test per DependencyInjection (Infrastructure e Services)

**Categoria:** Struttura/Copertura  
**Priorità:** Bassa  
**Impatto:** Basso  
**Status:** Risolto  
**Data Apertura:** 2026-03-18  
**Data Risoluzione:** 2026-03-18  
**Branch:** fix/test-004  

#### Descrizione

I metodi `AddInfrastructure()` e `AddServices()` non avevano unit test. La struttura cartelle non prevedeva `Unit/Infrastructure/` né `Unit/Services/DependencyInjectionTests.cs`.

#### Soluzione Implementata

Creati i file di test:

**`Tests/Unit/Infrastructure/DependencyInjectionTests.cs` (13 test):**
- `AddInfrastructure_ReturnsServiceCollection`
- `AddInfrastructure_RegistersAppDbContext`
- `AddInfrastructure_RegistersUserRepository`
- `AddInfrastructure_RegistersBoardTypeRepository`
- `AddInfrastructure_RegistersBoardRepository`
- `AddInfrastructure_RegistersDictionaryRepository`
- `AddInfrastructure_RegistersVariableRepository`
- `AddInfrastructure_RegistersCommandRepository`
- `AddInfrastructure_RegistersAuditEntryRepository`
- `AddInfrastructure_RegistersBitInterpretationRepository`
- `AddInfrastructure_RegistersCommandDeviceStateRepository`
- `AddInfrastructure_RegistersRepositoriesAsScoped`
- `AddInfrastructure_SameScopeReturnsSameInstance`

**`Tests/Unit/Services/DependencyInjectionTests.cs` (10 test):**
- `AddServices_ReturnsServiceCollection`
- `AddServices_RegistersDictionaryService`
- `AddServices_RegistersVariableService`
- `AddServices_RegistersCommandService`
- `AddServices_RegistersBoardService`
- `AddServices_RegistersUserService`
- `AddServices_RegistersServicesAsScoped`
- `AddServices_SameScopeReturnsSameInstance`
- `AddServices_WithoutInfrastructure_ThrowsOnResolve`
- `AddServices_AllServicesResolvable`

#### Benefici Ottenuti

- Verifica registrazione DI corretta ✅
- Struttura cartelle più consistente ✅
- Cattura errori di configurazione DI ✅
- Test scoped lifetime verificato ✅

---

### TEST-005 - Mancano test per scenari di rilavorazione/update entities

**Categoria:** Copertura  
**Priorità:** Bassa  
**Impatto:** Medio  
**Status:** Risolto  
**Data Apertura:** 2026-03-18  
**Data Risoluzione:** 2026-03-18  
**Branch:** fix/test-004  

#### Descrizione

I test di integrazione coprivano principalmente scenari CRUD base (Add, GetById). Mancavano test per scenari più complessi.

#### Soluzione Implementata

Creato il file di test:

**`Tests/Integration/Infrastructure/CrudScenariosTests.cs` (18 test):**

**Update Scenarios (4 test):**
- `UpdateAsync_User_ModifiesDisplayName`
- `UpdateAsync_Dictionary_PreservesVariables`
- `UpdateAsync_Variable_SetsUpdatedAt`
- `UpdateAsync_Command_ModifiesParameters`

**Delete with Cascade (4 test):**
- `DeleteDictionary_CascadesDeleteToVariables`
- `DeleteVariable_CascadesDeleteToBitInterpretations`
- `DeleteCommand_CascadesDeleteToDeviceStates`
- `DeleteBoardType_WithBoards_ThrowsException`

**Unique Constraint Violations (6 test):**
- `AddUser_DuplicateUsername_ThrowsDbUpdateException`
- `AddDictionary_DuplicateName_ThrowsDbUpdateException`
- `AddVariable_DuplicateAddressInSameDictionary_ThrowsDbUpdateException`
- `AddVariable_SameAddressDifferentDictionary_Succeeds`
- `AddCommand_DuplicateCode_ThrowsDbUpdateException`
- `AddCommand_SameCodeDifferentIsResponse_Succeeds`

**Audit Trail Complete Lifecycle (2 test):**
- `AuditTrail_CreateUpdateDelete_TracksAllChanges`
- `AuditTrail_MultipleUpdates_UpdatesTimestampEachTime`

**Repository Update/Delete (2 test):**
- `UserRepository_UpdateAsync_ModifiesEntity`
- `DictionaryRepository_DeleteAsync_RemovesCascadedVariables`

#### Benefici Ottenuti

- Copertura scenari reali di utilizzo ✅
- Verifica comportamento FK (Cascade vs Restrict) ✅
- Test unique constraint violations ✅
- Verifica audit trail completo ✅
- Confidenza in operazioni complesse ✅

---

## Wontfix

*(Nessuna issue in wontfix)*
