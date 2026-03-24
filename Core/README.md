# Core

> **Libreria di dominio contenente modelli ed enumerazioni per la gestione dizionari STEM.**  
> **Ultimo aggiornamento:** 2026-03-24

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
    accessMode: AccessMode.ReadOnly,
    dataTypeRaw: "INT16"
);

// 3 semantiche di dizionario:
// ① Standard (null, null) — variabili comuni a tutti i device
var standard = new Dictionary("standard", description: "Variabili comuni");

// ② Periferica condivisa (null, BoardType) — periferica usata da più device
var shared = new Dictionary("pulsantiere", boardType: boardType);

// ③ Dedicato (DeviceType, BoardType) — specifico per un device
var dedicated = new Dictionary("optimus-xp", DeviceType.OptimusXp, boardType);
dedicated.AddVariable(variable);
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
    ├── AuditEntry.cs           # Traccia modifiche con JSON completo
    ├── BitInterpretation.cs    # Significato bit per variabili bitmapped
    ├── Board.cs                # Scheda con calcolo indirizzo protocol
    ├── BoardType.cs            # Tipo scheda (Madre, Pulsantiera, etc.)
    ├── Command.cs              # Comando protocollo
    ├── CommandDeviceState.cs   # Stato comando per device specifico
    ├── Dictionary.cs           # Set di variabili con 3 semantiche (Standard, Condiviso, Dedicato)
    ├── User.cs                 # Utente sistema (audit)
    └── Variable.cs             # Variabile dizionario con tipo e permessi
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
| `AuditEntityType` | 7 valori | Tipo entità per audit trail |

### Modelli Principali

| Modello | Descrizione | Relazioni |
|---------|-------------|-----------|
| `Dictionary` | Set di variabili — 3 semantiche: Standard, Condiviso, Dedicato | → DeviceType?, BoardType?, Variable[] |
| `Variable` | Variabile con indirizzo, tipo, permessi, formato | → Dictionary, BitInterpretation[] |
| `BoardType` | Tipo scheda con firmwareType | → Dictionary[], Board[] |
| `Board` | Istanza scheda in un device | → BoardType, DeviceType |
| `Command` | Comando protocollo universale | → CommandDeviceState[] |
| `CommandDeviceState` | Stato comando per device specifico | → Command, DeviceType |
| `BitInterpretation` | Significato bit per variabili bitmapped | → Variable |
| `User` | Utente sistema (audit) | — |
| `AuditEntry` | Traccia modifiche | previousValue/newValue JSON |

### Semantiche Dizionario

| Semantica | DeviceType | BoardType | Esempio |
|-----------|------------|-----------|----------|
| **Standard** | `null` | `null` | Variabili comuni a tutti i device |
| **Periferica condivisa** | `null` | ✅ | Pulsantiera 4x4 usata da più device |
| **Dedicato** | ✅ | ✅ | Madre Optimus-XP |

> Combinazione invalida: `(DeviceType, null)` — se c'è il device, serve il BoardType.

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

→ [Core/ISSUES.md](./ISSUES.md) — 4 issue aperte, 2 risolte (0 critiche, 0 alte, 1 media, 3 basse)

---

## Links

- [Infrastructure/README.md](../Infrastructure/README.md) - Layer persistenza (entities, repositories)
- [Docs/ER-schema.puml](../Docs/ER-schema.puml) - Schema ER database
- [.copilot/copilot-instructions.md](../.copilot/copilot-instructions.md) - Formalizzazione Lean 4 del dominio
