# Core

> **Libreria di dominio contenente modelli ed enumerazioni per la gestione dizionari STEM.**  
> **Ultimo aggiornamento:** 2026-03-19

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
| **Modelli dominio** | ✅ | 9 classi (User, Board, Variable, Dictionary, etc.) |
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

// Creare un tipo di scheda
var boardType = new BoardType("Madre", firmwareType: 17);

// Creare una variabile
var variable = new Variable(
    name: "Temperatura",
    addressHigh: 0x80,
    addressLow: 0x01,
    dataTypeKind: DataTypeKind.Int16,
    dataTypeRaw: "INT16",
    accessMode: AccessMode.ReadOnly,
    isEnabled: true
);

// Creare un dizionario
var dictionary = new Dictionary("optimus-xp", boardType);
dictionary.AddVariable(variable);
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
│   ├── DeviceType.cs           # SherpaSlim, Optimus, Eden, etc. (12 tipi)
│   └── VariableCategory.cs     # Standard (0x00xx), DeviceSpecific (0x80xx)
└── Models/
    ├── AuditEntry.cs           # Traccia modifiche con JSON completo
    ├── BitInterpretation.cs    # Significato bit per variabili bitmapped
    ├── Board.cs                # Scheda con calcolo indirizzo protocol
    ├── BoardType.cs            # Tipo scheda (Madre, Pulsantiera, etc.)
    ├── Command.cs              # Comando protocollo
    ├── CommandDeviceState.cs   # Stato comando per device specifico
    ├── Dictionary.cs           # Set di variabili per BoardType
    ├── User.cs                 # Utente sistema (audit)
    └── Variable.cs             # Variabile dizionario con tipo e permessi
```

---

## API / Componenti

### Enumerazioni

| Enum | Valori | Uso |
|------|--------|-----|
| `DeviceType` | 12 valori | Tipo dispositivo STEM (Optimus, Eden, etc.) |
| `AccessMode` | 3 valori | Permessi variabile (ReadOnly, ReadWrite, WriteOnly) |
| `DataTypeKind` | 11 valori | Tipo dato (UInt8, Int16, String, Bitmapped, etc.) |
| `VariableCategory` | 2 valori | Standard (0x00xx) o DeviceSpecific (0x80xx) |
| `AuditOperation` | 3 valori | Operazione audit (Create, Update, Delete) |
| `AuditEntityType` | 8 valori | Tipo entità per audit trail |

### Modelli Principali

| Modello | Descrizione | Relazioni |
|---------|-------------|-----------|
| `Variable` | Variabile con indirizzo, tipo, permessi | → Dictionary, BitInterpretation |
| `Dictionary` | Set di variabili per un BoardType | → BoardType, Variable[] |
| `BoardType` | Tipo scheda con firmwareType | → Dictionary, Board[] |
| `Board` | Istanza scheda in un device | → BoardType, DeviceType |
| `Command` | Comando protocollo universale | → CommandDeviceState[] |
| `AuditEntry` | Traccia modifiche | previousValue/newValue JSON |

### Calcolo Indirizzo Protocol

```csharp
// Board.CalculateAddress compone l'indirizzo da:
// - MACHINE (DeviceType)
// - FIRMWARE_TYPE (BoardType)
// - BOARD_NUMBER

int address = Board.CalculateAddress(machine: 10, fwType: 17, boardNum: 1);
// Risultato: (10 << 16) | ((17 & 0x03FF) << 6) | (1 & 0x003F)
```

---

## Issue Correlate

→ [Core/ISSUES.md](./ISSUES.md) — 3 issue aperte, 2 risolte (0 critiche, 0 alte, 0 medie, 3 basse)

---

## Links

- [Infrastructure/README.md](../Infrastructure/README.md) - Layer persistenza (entities, repositories)
- [Docs/ER-schema.puml](../Docs/ER-schema.puml) - Schema ER database
- [.copilot/copilot-instructions.md](../.copilot/copilot-instructions.md) - Formalizzazione Lean 4 del dominio
