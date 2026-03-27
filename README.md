# STEM Dictionaries Manager

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![Tests](https://img.shields.io/badge/tests-1461%20passing-brightgreen)](./Tests/)
[![License](https://img.shields.io/badge/license-Proprietary-red)](#licenza)

> **Applicazione per la gestione centralizzata dei dizionari dispositivi STEM (comandi + variabili).**

> **Ultimo aggiornamento:** 2026-03-26


---

## Panoramica

**Stem.Dictionaries.Manager** è un'applicazione desktop WPF per centralizzare la gestione dei dizionari dei dispositivi STEM (medical equipment). 

### Problema

- Dizionari attualmente in file Excel (`Dizionari.xlsx`)
- Excel copiato/incollato tra progetti → rischio inconsistenze
- Modifiche da più punti → propagazione errori
- Consumer devono fare import manuale → perdita tempo
- Firmware in continua evoluzione → dizionari cambiano spesso

### Soluzione

- **Database centralizzato** su Azure SQL (single source of truth)
- **Applicativo desktop** per gestione CRUD
- **Versionamento modifiche** con audit trail completo
- **API per consumer** (es. Stem.Production.Tracker)

---

## Caratteristiche

| Feature | Stato | Descrizione |
|---------|-------|-------------|
| **Modelli dominio** | ✅ | 10 models + 6 enums (Variable, Dictionary, Board, VariableDeviceState, etc.) |
| **Domain v2** | ✅ | IsStandard flag, Board→Dictionary diretto, semantica derivata |
| **Persistenza** | ✅ | EF Core + SQLite (dev) / Azure SQL (prod), 1 migration Domain v2 |
| **Audit Trail** | ✅ | Traccia ogni modifica con JSON completo |
| **Repository Pattern** | ✅ | 9 repository con interfacce |
| **Services Layer** | ✅ | 5 services + 9 mappers + business rules |
| **GUI Desktop** | ✅ | WPF + MVVM con 15 ViewModels, 14 Views attive, dark theme STEM, custom dialogs, status bar |
| **Comandi per device** | ✅ | Stato attivo/disattivo comandi per DeviceType con override persistente |
| **Variabili standard per device** | ✅ | Stato attivo/disattivo variabili standard per DeviceType (BR-009/011) |
| **Test Suite** | ✅ | ~915 metodi test / 1461 test cases (unit + integration, 2 target framework) |

---

## Requisiti

- **.NET 10.0** o superiore
- **Visual Studio 2022/2026** (per sviluppo WPF)
- **SQLite** (sviluppo) / **Azure SQL** (produzione)

---

## Quick Start

```bash
# Clone repository
git clone https://bitbucket.org/stem-fw/stem-dictionaries-manager.git
cd stem-dictionaries-manager

# Restore e build
dotnet restore
dotnet build

# Eseguire i test
dotnet test Tests/Tests.csproj --framework net10.0

# Applicare migrations (crea DB sviluppo)
dotnet ef database update -p Infrastructure -s GUI.Windows
```

---

## Struttura Soluzione

```
Stem.Dictionaries.Manager/
├── Core/                  # Modelli dominio, enums (10 classi, 6 enum)
│   ├── Enums/             # DeviceType, AccessMode, DataTypeKind, etc.
│   └── Models/            # Variable, Dictionary, Board, VariableDeviceState, etc.
├── Services/              # Business logic, mapping Entity ↔ Domain
│   ├── Interfaces/        # Service interfaces (5)
│   └── Mapping/           # Mapper bidirezionali (9)
├── Infrastructure/        # EF Core, SQLite, Repositories
│   ├── Entities/          # Entity classes (9)
│   ├── Repositories/      # Repository implementations (9)
│   └── Migrations/        # 1 migration Domain v2
├── GUI.Windows/           # Applicazione WPF (MVVM, 15 ViewModels, 14 Views attive)
│   ├── Abstractions/      # Interfaces navigazione, dialoghi, messaggi, IEditableViewModel
│   ├── ViewModels/        # 15 ViewModels + helper classes
│   ├── Views/             # 15 Views XAML (incl. LoginView, DarkDialog, DeviceCommandsView, DeviceVariablesView)
│   ├── Converters/        # 6 converter (Bool, Inverse, Null, NullableInt/Double, SeverityToColor)
│   └── Services/          # NavigationService, DialogService, MessageService
├── Tests/                 # Unit & integration tests (~915 metodi / 1461 cases)
│   ├── Unit/              # Core, Services/Mapping, Infrastructure/DI, GUI
│   └── Integration/       # Infrastructure, Services, GUI (SQLite in-memory)
├── Docs/                  # Documentazione
│   ├── Dictionaries/      # CSV originali per riferimento
│   ├── Standards/         # Template documentazione
│   └── ER-schema.puml     # Schema database
├── .copilot/              # Copilot instructions e agents
└── ISSUES_TRACKER.md      # Riepilogo globale issue (13 aperte, 33 risolte)
```

---

## Documentazione

| Documento | Descrizione |
|-----------|-------------|
| [Core/README.md](./Core/README.md) | Modelli dominio ed enumerazioni |
| [Services/README.md](./Services/README.md) | Business logic, mapping, services |
| [Infrastructure/README.md](./Infrastructure/README.md) | Persistenza, EF Core, Repositories |
| [GUI.Windows/README.md](./GUI.Windows/README.md) | Applicazione WPF, ViewModels, Navigation |
| [Tests/README.md](./Tests/README.md) | Suite di test, convenzioni |
| [ISSUES_TRACKER.md](./ISSUES_TRACKER.md) | Riepilogo globale issue e metriche qualità |
| [Docs/ER-schema.puml](./Docs/ER-schema.puml) | Schema ER database |
| [.copilot/copilot-instructions.md](./.copilot/copilot-instructions.md) | Formalizzazione Lean 4, workflow |

---

## Issue Tracking

→ **[ISSUES_TRACKER.md](./ISSUES_TRACKER.md)** — Riepilogo globale: **13 aperte**, 33 risolte

| Componente | Issue File | Aperte | Risolte | Priorità Max |
|------------|------------|:------:|:-------:|:------------:|
| Core | [Core/ISSUES.md](./Core/ISSUES.md) | 3 | 4 | Media |
| Infrastructure | [Infrastructure/ISSUES.md](./Infrastructure/ISSUES.md) | 2 | 6 | Media |
| Services | [Services/ISSUES.md](./Services/ISSUES.md) | 4 | 7 | Media |
| GUI.Windows | [GUI.Windows/ISSUES.md](./GUI.Windows/ISSUES.md) | 2 | 6 | Media |
| Tests | [Tests/ISSUES.md](./Tests/ISSUES.md) | 1 | 8 | Media |
| Trasversali | [ISSUES_TRACKER.md](./ISSUES_TRACKER.md#issue-trasversali-t-xxx) | 1 | 2 | Bassa |

✅ **0 issue alta priorità aperte**

---

## Palette Colori STEM

| Ruolo | Colore | Hex |
|-------|--------|-----|
| 🔵 Accent (base) | ![#004682](https://via.placeholder.com/12/004682/004682.png) | `#004682` |
| 🔵 Hover (scuro) | ![#003461](https://via.placeholder.com/12/003461/003461.png) | `#003461` |
| 🔵 Pressed (chiaro) | ![#2668A0](https://via.placeholder.com/12/2668A0/2668A0.png) | `#2668A0` |
| 🔵 Selezione inattiva | ![#1E3A54](https://via.placeholder.com/12/1E3A54/1E3A54.png) | `#1E3A54` |
| 🟢 Successo | ![#98D801](https://via.placeholder.com/12/98D801/98D801.png) | `#98D801` |
| 🔴 Errore | ![#E40032](https://via.placeholder.com/12/E40032/E40032.png) | `#E40032` |
| 🟡 Warning | ![#FFC04A](https://via.placeholder.com/12/FFC04A/FFC04A.png) | `#FFC04A` |

---

## CI/CD

Pipeline Bitbucket configurata:

```yaml
# Build & Test su ogni push
- dotnet build --configuration Release
- dotnet test --framework net10.0
```

Badge: [![Build](https://img.shields.io/badge/CI-Bitbucket%20Pipelines-blue)](./bitbucket-pipelines.yml)

---

## Relazione con Production.Tracker

**Stem.Production.Tracker** è un **consumer** di questo progetto:

| Attualmente | Futuro |
|-------------|--------|
| Usa JSON files (`DeviceDefinitions/`) | Consumerà API/DB di Dictionaries.Manager |
| Import manuale | Sincronizzazione automatica |

---

## Architettura

```
┌─────────────────────────────────────────────────────────────┐
│                      GUI.Windows (WPF)                      │
│      MVVM, 15 ViewModels, 14 Views, Dark Theme STEM, Status Bar │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                         Services                            │
│        Business Logic, Mapping, Business Rules              │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                      Infrastructure                         │
│         EF Core, 9 Repositories, 1 Migration                │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                           Core                              │
│            Domain Models (10), Enums (6)                    │
└─────────────────────────────────────────────────────────────┘
```

---

## Contribuire

1. Leggere la documentazione e seguire le convenzioni di codifica
2. Seguire il pattern **TDD**: Test → Implement → Refactor
3. Nomenclatura **inglese** per codice, **italiana** per documentazione
4. Ogni PR deve passare i test CI

---

## Licenza

- **Proprietario:** STEM E.m.s.
- **Autore:** Luca Veronelli (l.veronelli@stem.it)
- **Data di Creazione:** 2026-03-18
- **Licenza:** Proprietaria - Tutti i diritti riservati