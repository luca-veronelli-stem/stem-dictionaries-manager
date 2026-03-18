# STEM Dictionaries Manager

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![Tests](https://img.shields.io/badge/tests-752%20passing-brightgreen)](./Tests/)
[![License](https://img.shields.io/badge/license-Proprietary-red)](#licenza)

> **Applicazione per la gestione centralizzata dei dizionari dispositivi STEM (comandi + variabili).**

> **Ultimo aggiornamento:** 2026-03-18

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
| **Modelli dominio** | ✅ | 9 models + 6 enums (Variable, Dictionary, Command, etc.) |
| **Persistenza** | ✅ | EF Core + SQLite (dev) / Azure SQL (prod) |
| **Audit Trail** | ✅ | Traccia ogni modifica con JSON completo |
| **Repository Pattern** | ✅ | 9 repository con interfacce |
| **Services Layer** | ✅ | 5 services + 8 mappers |
| **GUI Desktop** | ⏳ | WPF application (da sviluppare) |
| **Test Suite** | ✅ | 752 test (202 unit + 550 integration) |

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
├── Core/                  # Modelli dominio, enums
│   ├── Enums/             # DeviceType, AccessMode, DataTypeKind, etc.
│   └── Models/            # Variable, Dictionary, Command, User, etc.
├── Services/              # Business logic, mapping Entity ↔ Domain
│   ├── Interfaces/        # Service interfaces (5)
│   └── Mapping/           # Mapper bidirezionali (8)
├── Infrastructure/        # EF Core, SQLite, Repositories
│   ├── Entities/          # Entity classes (9)
│   ├── Repositories/      # Repository implementations (9)
│   └── Migrations/        # EF Core migrations
├── GUI.Windows/           # Applicazione WPF (da sviluppare)
├── Tests/                 # Unit & integration tests
│   ├── Unit/              # Test isolati (Core, Mapping)
│   └── Integration/       # Test con DB (Infrastructure, Services)
├── Docs/                  # Documentazione
│   ├── Dictionaries/      # CSV originali per riferimento
│   ├── Standards/         # Template documentazione
│   └── ER-schema.puml     # Schema database
└── .copilot/              # Copilot instructions e agents
```

---

## Documentazione

| Documento | Descrizione |
|-----------|-------------|
| [Core/README.md](./Core/README.md) | Modelli dominio ed enumerazioni |
| [Services/README.md](./Services/README.md) | Business logic, mapping, services |
| [Infrastructure/README.md](./Infrastructure/README.md) | Persistenza, EF Core, Repositories |
| [Tests/README.md](./Tests/README.md) | Suite di test, convenzioni |
| [Docs/ER-schema.puml](./Docs/ER-schema.puml) | Schema ER database |
| [.copilot/copilot-instructions.md](./.copilot/copilot-instructions.md) | Formalizzazione Lean 4, workflow |

---

## Issue Tracking

| Componente | Issue File | Status |
|------------|------------|--------|
| Core | [Core/ISSUES.md](./Core/ISSUES.md) | 5 aperte (2 medie, 3 basse) |
| Services | [Services/ISSUES.md](./Services/ISSUES.md) | 6 aperte, 1 risolta (2 medie, 4 basse) |
| Infrastructure | [Infrastructure/ISSUES.md](./Infrastructure/ISSUES.md) | 4 aperte, 2 risolte |
| Tests | [Tests/ISSUES.md](./Tests/ISSUES.md) | 3 aperte, 3 risolte |

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
│                         MVVM Pattern                        │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                         Services                            │
│              Business Logic, Mapping, Validation            │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                      Infrastructure                         │
│            EF Core, Repositories, Migrations                │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                           Core                              │
│              Domain Models, Enums, Interfaces               │
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