# Core - ISSUES

> **Scopo:** Questo documento traccia bug, code smells, performance issues, opportunità di refactoring e violazioni di best practice per il componente **Core**.

> **Ultimo aggiornamento:** 2026-03-18

---

## Riepilogo

| Priorità | Aperte | Risolte |
|----------|--------|---------|
| **Critica** | 0 | 0 |
| **Alta** | 0 | 0 |
| **Media** | 2 | 0 |
| **Bassa** | 3 | 0 |

**Totale aperte:** 5  
**Totale risolte:** 0

---

## Indice Issue Aperte

- [CORE-001 - AuditEntityType contiene "Device" non esistente nel dominio](#core-001--auditentitytype-contiene-device-non-esistente-nel-dominio)
- [CORE-002 - Variable.Category deriva solo da AddressHigh == 0x00](#core-002--variablecategory-deriva-solo-da-addresshigh--0x00)
- [CORE-003 - Dictionary.RemoveVariable non verifica esistenza](#core-003--dictionaryremovevariable-non-verifica-esistenza)
- [CORE-004 - Mancanza di metodi Update sui modelli](#core-004--mancanza-di-metodi-update-sui-modelli)
- [CORE-005 - BitInterpretation.VariableId non ha validazione positiva](#core-005--bitinterpretationvariableid-non-ha-validazione-positiva)

## Indice Issue Risolte

*(Nessuna issue risolta)*

---

## Priorità Media

### CORE-001 - AuditEntityType contiene "Device" non esistente nel dominio

**Categoria:** Design  
**Priorità:** Media  
**Impatto:** Medio  
**Status:** Aperto  
**Data Apertura:** 2026-03-18  

#### Descrizione

L'enum `AuditEntityType` include il valore `Device`, ma nel dominio formalizzato in Lean 4 l'entità `Device` è stata rimossa a favore dell'enum `DeviceType`. Questo crea inconsistenza tra la formalizzazione e il codice.

#### File Coinvolti

- `Core/Enums/AuditEntityType.cs` (riga 10)

#### Codice Problematico

```csharp
public enum AuditEntityType
{
    Variable,
    Command,
    Device,      // <-- Entità non esistente nel dominio
    Board,
    BoardType,
    Dictionary,
    BitInterpretation,
    User
}
```

#### Problema Specifico

- La formalizzazione Lean 4 indica: "RIMOSSO: Device entity (ridondante con DeviceType enum)"
- `Device` nell'enum suggerisce che esista una tabella/entità tracciabile, ma non c'è
- Potenziale confusione per chi legge il codice

#### Soluzione Proposta

**Opzione A: Rimuovere Device**

```csharp
public enum AuditEntityType
{
    Variable,
    Command,
    Board,
    BoardType,
    Dictionary,
    BitInterpretation,
    User
}
```

**Opzione B: Rinominare in CommandDeviceState**

Se si vuole tracciare `CommandDeviceState`, rinominare:

```csharp
CommandDeviceState,  // invece di Device
```

#### Benefici Attesi

- Allineamento con formalizzazione Lean 4
- Minore confusione per sviluppatori
- Enum accurato rispetto alle entità tracciate

---

### CORE-002 - Variable.Category deriva solo da AddressHigh == 0x00

**Categoria:** Design  
**Priorità:** Media  
**Impatto:** Basso  
**Status:** Aperto  
**Data Apertura:** 2026-03-18  

#### Descrizione

La proprietà `Variable.Category` considera "Standard" solo se `AddressHigh == 0x00`, ma qualsiasi altro valore viene mappato a `DeviceSpecific`. Questo potrebbe non essere corretto se esistono altri range di indirizzi.

#### File Coinvolti

- `Core/Models/Variable.cs` (righe 41-43)

#### Codice Problematico

```csharp
public VariableCategory Category => AddressHigh == 0x00 
    ? VariableCategory.Standard 
    : VariableCategory.DeviceSpecific;
```

#### Problema Specifico

- La formalizzazione Lean 4 indica: "Standard (0x00xx) o DeviceSpecific (0x80xx)"
- Il codice attuale mappa tutto ciò che non è `0x00` a `DeviceSpecific`
- Se esistono indirizzi con `AddressHigh = 0x40` (esempio), sarebbero erroneamente classificati

#### Soluzione Proposta

**Opzione A: Validazione stretta (raccomandata)**

Se solo `0x00` e `0x80` sono validi, aggiungere validazione nel costruttore:

```csharp
if (addressHigh != 0x00 && addressHigh != 0x80)
    throw new ArgumentOutOfRangeException(nameof(addressHigh), 
        "AddressHigh must be 0x00 (Standard) or 0x80 (DeviceSpecific).");
```

**Opzione B: Match esplicito**

```csharp
public VariableCategory Category => AddressHigh switch
{
    0x00 => VariableCategory.Standard,
    0x80 => VariableCategory.DeviceSpecific,
    _ => throw new InvalidOperationException($"Unknown AddressHigh: 0x{AddressHigh:X2}")
};
```

#### Benefici Attesi

- Fail-fast su dati invalidi
- Maggiore robustezza nell'import da Excel
- Allineamento con specifiche di dominio

---

## Priorità Bassa

### CORE-003 - Dictionary.RemoveVariable non verifica esistenza

**Categoria:** API  
**Priorità:** Bassa  
**Impatto:** Basso  
**Status:** Aperto  
**Data Apertura:** 2026-03-18  

#### Descrizione

Il metodo `RemoveVariable` non notifica il chiamante se la variabile non esisteva nella lista. `List<T>.Remove()` ritorna `false` silenziosamente.

#### File Coinvolti

- `Core/Models/Dictionary.cs` (righe 55-58)

#### Codice Problematico

```csharp
public void RemoveVariable(Variable variable)
{
    _variables.Remove(variable);  // <-- Non verifica risultato
}
```

#### Problema Specifico

- Chiamare `RemoveVariable` con una variabile non presente non genera errore
- Potrebbe nascondere bug nel chiamante che assume la variabile fosse presente
- Inconsistenza: `AddVariable` valida, `RemoveVariable` no

#### Soluzione Proposta

**Opzione A: Return bool**

```csharp
public bool RemoveVariable(Variable variable)
{
    return _variables.Remove(variable);
}
```

**Opzione B: Throw se non trovata**

```csharp
public void RemoveVariable(Variable variable)
{
    if (!_variables.Remove(variable))
        throw new InvalidOperationException(
            $"Variable '{variable.Name}' not found in dictionary.");
}
```

#### Benefici Attesi

- API più prevedibile
- Bug detection più veloce
- Coerenza con `AddVariable`

---

### CORE-004 - Mancanza di metodi Update sui modelli

**Categoria:** API  
**Priorità:** Bassa  
**Impatto:** Medio  
**Status:** Aperto  
**Data Apertura:** 2026-03-18  

#### Descrizione

I modelli `BoardType`, `User`, `Dictionary`, `Command` non hanno metodi per aggiornare le proprietà. Solo `Variable` e `CommandDeviceState` hanno `Enable()`/`Disable()`.

#### File Coinvolti

- `Core/Models/BoardType.cs`
- `Core/Models/User.cs`
- `Core/Models/Dictionary.cs`
- `Core/Models/Command.cs`

#### Problema Specifico

- Per aggiornare un `BoardType.Name` bisognerebbe creare una nuova istanza
- Il pattern immutabile è buono, ma mancano metodi `With*` o `Update*`
- Il Services layer dovrà gestire mapping complessi per update

#### Soluzione Proposta

**Opzione A: Metodi Update specifici (raccomandata)**

```csharp
// In BoardType.cs
public void UpdateName(string newName)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(newName);
    Name = newName;
}

// In User.cs
public void UpdateDisplayName(string newDisplayName)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(newDisplayName);
    DisplayName = newDisplayName;
}
```

**Opzione B: Pattern With (immutabile puro)**

```csharp
public BoardType WithName(string newName) => 
    new(newName, FirmwareType) { Id = Id };
```

#### Benefici Attesi

- API più ergonomica per update CRUD
- Validazione centralizzata nelle modifiche
- Minore complessità nel Services layer

---

### CORE-005 - BitInterpretation.VariableId non ha validazione positiva

**Categoria:** API  
**Priorità:** Bassa  
**Impatto:** Basso  
**Status:** Aperto  
**Data Apertura:** 2026-03-18  

#### Descrizione

Il costruttore di `BitInterpretation` non valida che `variableId` sia positivo. Un `variableId = 0` o negativo sarebbe invalido ma non genera errore.

#### File Coinvolti

- `Core/Models/BitInterpretation.cs` (righe 17-30)

#### Codice Problematico

```csharp
public BitInterpretation(int variableId, DeviceType deviceType, 
    int wordIndex, int bitIndex, string meaning)
{
    if (wordIndex < 0)
        throw new ArgumentOutOfRangeException(nameof(wordIndex), "...");
    if (bitIndex < 0 || bitIndex > 15)
        throw new ArgumentOutOfRangeException(nameof(bitIndex), "...");
    ArgumentException.ThrowIfNullOrWhiteSpace(meaning);
    
    // Manca: validazione variableId > 0
    VariableId = variableId;
    // ...
}
```

#### Soluzione Proposta

```csharp
public BitInterpretation(int variableId, DeviceType deviceType, 
    int wordIndex, int bitIndex, string meaning)
{
    if (variableId <= 0)
        throw new ArgumentOutOfRangeException(nameof(variableId), 
            "VariableId must be positive.");
    // ... resto validazioni
}
```

#### Benefici Attesi

- Fail-fast su dati invalidi
- Coerenza con altre validazioni nella classe
- Prevenzione bug a runtime

---

## Issue Risolte

*(Nessuna issue risolta)*

---

## Wontfix

*(Nessuna issue in wontfix)*
