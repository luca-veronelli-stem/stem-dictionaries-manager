# Core

> **Libreria di dominio contenente modelli ed enumerazioni per la gestione dizionari STEM.**  
> **Ultimo aggiornamento:** 2026-03-25

---

## Panoramica

Il progetto **Core** rappresenta il cuore del dominio applicativo di Stem.Dictionaries.Manager. Contiene:

- **Modelli di dominio** - Classi che rappresentano le entità business (Variable, Command, Dictionary, etc.)
- **Enumerazioni** - Tipi enumerati per valori discreti (DeviceType, AccessMode, DataTypeKind, etc.)

Questo progetto è **puro dominio**: nessuna dipendenza da framework esterni, database o UI. 
È referenziato da tutti gli altri progetti della soluzione.

---

## Caratteristiche

| Feature | Stato | Descrizione |
|---------|-------|-------------|
| **Modelli dominio** | ✅ | 10 classi (User, Board, Variable, Dictionary, VariableDeviceState, etc.) |
| **Enumerazioni** | ✅ | 6 enum (DeviceType, AccessMode, DataTypeKind, etc.) |
| **Validazione** | ✅ | Logica di validazione nei costruttori |
| **Immutabilità** | ✅ | Private setters, costruttori con parametri |

---

## Requisiti

- **.NET 10.0** o superiore

### Dipendenze

Nessuna dipendenza esterna. Progetto autocontenuto.

---

## Quick Start

```csharp
using Core.Enums;
using Core.Models;

// Creare una variabile
var variable = new Variable(
    name: "Temperatura",
    addressHigh: 0x80,
    addressLow: 0x01,
    dataTypeKind: DataTypeKind.Int16,
    accessMode: AccessMode.ReadOnly,
    dataTypeRaw: "INT16"
);

// Dizionario Standard (variabili comuni 0x00xx)
var standard = new Dictionary("Standard", description: "Variabili comuni", isStandard: true);

// Dizionario specifico (la semantica Dedicated/Shared/Orphan è derivata dai Board)
var optimusXp = new Dictionary("Optimus XP", description: "Variabili scheda madre");
optimusXp.AddVariable(variable);

// Board con FirmwareType diretto e link a Dictionary
var board = new Board(DeviceType.OptimusXp, "Madre Master", firmwareType: 17,
    boardNumber: 1, isPrimary: true, dictionaryId: optimusXp.Id);

// Override per-device su variabili (BR-009)
var state = new VariableDeviceState(variable.Id, DeviceType.SherpaSlim, isEnabled: false);
```

---

## Struttura

```
Core/
├── Enums/
│   ├── AccessMode.cs           # ReadOnly, ReadWrite, WriteOnly
│   ├── AuditEntityType.cs      # Tipi entità per audit trail
│   ├── AuditOperation.cs       # Create, Update, Delete
│   ├── DataTypeKind.cs         # UInt8, Int16, String, Bitmapped, etc.
│   ├── DeviceType.cs           # SherpaSlim, Optimus, Eden, etc. (11 tipi)
│   └── VariableCategory.cs     # Standard (0x00xx), DeviceSpecific (0x80xx)
└── Models/
    ├── AuditEntry.cs              # Traccia modifiche con JSON completo
    ├── BitInterpretation.cs       # Significato bit per variabili bitmapped
    ├── Board.cs                   # Scheda con FirmwareType, DictionaryId?, DictionaryName, calcolo indirizzo
    ├── Command.cs                 # Comando protocollo
    ├── CommandDeviceState.cs      # Stato comando per device specifico
    ├── Dictionary.cs              # Set di variabili, IsStandard flag
    ├── User.cs                    # Utente sistema (audit)
    ├── Variable.cs                # Variabile dizionario con tipo e permessi
    └── VariableDeviceState.cs     # Override per-device su variabili (BR-009)
```

---

## API / Componenti

### Enumerazioni

| Enum | Valori | Uso |
|------|--------|-----|
| `DeviceType` | 11 valori | Tipo dispositivo STEM (Optimus, Eden, Sherpa, etc.) |
| `AccessMode` | 3 valori | Permessi variabile (ReadOnly, ReadWrite, WriteOnly) |
| `DataTypeKind` | 12 valori | Tipo dato (UInt8, Int16, String, Bitmapped, Array, Other, etc.) |
| `VariableCategory` | 2 valori | Standard (0x00xx) o DeviceSpecific (0x80xx) |
| `AuditOperation` | 3 valori | Operazione audit (Create, Update, Delete) |
| `AuditEntityType` | 6 valori | Tipo entità per audit trail |

### Modelli Principali

| Modello | Descrizione | Relazioni |
|---------|-------------|-----------|
| `Dictionary` | Set di variabili, IsStandard flag | → Variable[] |
| `Variable` | Variabile con indirizzo, tipo, permessi, formato | → Dictionary, BitInterpretation[], VariableDeviceState[] |
| `Board` | Scheda fisica con FirmwareType, DictionaryId?, IsPrimary, DictionaryName | → DeviceType, Dictionary? |
| `Command` | Comando protocollo universale | → CommandDeviceState[] |
| `CommandDeviceState` | Stato comando per device specifico | → Command, DeviceType |
| `VariableDeviceState` | Override per-device su variabili (BR-009/010/011) | → Variable, DeviceType |
| `BitInterpretation` | Significato bit per variabili bitmapped | → Variable |
| `User` | Utente sistema (audit) | — |
| `AuditEntry` | Traccia modifiche | previousValue/newValue JSON |

### Semantiche Dizionario (Domain v2)

I dizionari non hanno più DeviceType/BoardType. La semantica è derivata a runtime dai Board che li referenziano:

| Semantica | IsStandard | Board che lo referenziano | Esempio |
|-----------|:----------:|---------------------------|----------|
| **Standard** | `true` | (tutti, implicitamente) | Variabili comuni 0x00xx |
| **Dedicated** | `false` | 1 solo DeviceType | Madre Optimus-XP |
| **Shared** | `false` | 2+ DeviceType diversi | Pulsantiera 4x4 |
| **Orphan** | `false` | nessun Board | Dizionario non ancora assegnato |

> Il flag `IsStandard` è persistito. Le altre 3 semantiche sono **calcolate** dalla relazione Board→Dictionary.

### Calcolo Indirizzo Protocol

```csharp
// Board.CalculateAddress compone l'indirizzo da:
// - machineCode (DeviceType)
// - firmwareType (BoardType)
// - boardNumber

uint address = Board.CalculateAddress(machineCode: 10, firmwareType: 17, boardNumber: 1);
// Risultato: (10 << 16) | ((17 & 0x03FF) << 6) | (1 & 0x003F)
```

---

## Issue Correlate

→ [Core/ISSUES.md](./ISSUES.md) — 3 issue aperte, 4 risolte (0 critiche, 0 alte, 0 medie, 3 basse)

---

## Links

- [Infrastructure/README.md](../Infrastructure/README.md) - Layer persistenza (entities, repositories)
- [Docs/ER-schema.puml](../Docs/ER-schema.puml) - Schema ER database
- [.copilot/copilot-instructions.md](../.copilot/copilot-instructions.md) - Formalizzazione Lean 4 del dominio
