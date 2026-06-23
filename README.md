# DictionariesManager

[![CI](https://github.com/luca-veronelli-stem/stem-dictionaries-manager/actions/workflows/ci.yml/badge.svg)](https://github.com/luca-veronelli-stem/stem-dictionaries-manager/actions/workflows/ci.yml)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-Proprietary-red)](#license)

> **Centralized management for STEM device dictionaries (commands + variables), with REST API for external consumers.**
> **Standard:** v1.18.1 — see [`docs/Standards/`](./docs/Standards/).

---

## Overview

`DictionariesManager` replaces the Excel-based workflow used to maintain STEM device dictionaries. The dictionaries describe the commands and variables exposed by each device (firmware), and have historically been copy-pasted across consumer projects, with the inevitable drift and import overhead. This repo ships:

- a **centralised database** (Azure SQL in production, SQLite in development) as single source of truth;
- a **WPF desktop GUI** (`GUI.Windows`) for CRUD on devices, dictionaries, boards, variables, commands;
- a **REST API** (`API`) consumed by Production.Tracker, ButtonPanel.Tester, and the global comms service;
- a **change audit trail** so every edit is traceable.

The desktop GUI is currently WPF; an Avalonia + FuncUI migration is tracked in `CLAUDE.md` under "Active migrations".

## Quick Start

```powershell
dotnet restore
dotnet build
dotnet test tests/Tests/Tests.csproj --framework net10.0
dotnet run --project src/GUI.Windows   # or: dotnet run --project src/API
```

SQLite is the default in development. Both the desktop GUI and a development API run create and seed the SQLite database on first launch via `EnsureCreated` + `DatabaseSeeder`, so `dotnet run --project src/API` serves data immediately — there is no longer any need to launch the GUI first. To target Azure SQL, set `DatabaseProvider=SqlServer` and the appropriate connection string in `appsettings.json` (or via environment variables in production).

## Solution Structure

```
src/
├── Core/                domain models + enums
├── Services/            business logic + Entity↔Domain mappers
├── Infrastructure/      EF Core (SQLite + SQL Server), repositories, migrations
├── API/                 ASP.NET Core Minimal API (REST endpoints, API-key middleware)
└── GUI.Windows/         WPF desktop GUI (MVVM via CommunityToolkit.Mvvm)
tests/
└── Tests/               xUnit (unit + integration + E2E + API)
docs/                    documentation; Dictionaries/ holds the legacy CSV/XLSX seed data
eng/                     hooks installer
```

## Documentation

- Standards followed: [`docs/Standards/`](./docs/Standards/) — pinned to `v1.18.1`.
- Changelog: [`CHANGELOG.md`](./CHANGELOG.md).
- Repo-specific notes: [`CLAUDE.md`](./CLAUDE.md).

## License

- **Owner:** STEM E.m.s.
- **Author:** Luca Veronelli
- **Creation Date:** 2026
- **License:** Proprietary — All rights reserved.
