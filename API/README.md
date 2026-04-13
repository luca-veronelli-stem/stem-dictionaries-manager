# API

> **API REST read-only per l'accesso ai dizionari STEM da consumer esterni (Global Service, Production.Tracker, ButtonPanel.Tester, Stem.Device.Manager).**  
> **Ultimo aggiornamento:** 2026-04-13

---

## Panoramica

Il progetto **API** è un'applicazione ASP.NET Core Minimal API che espone 12 endpoint (10 business read-only + health check + version) per consultare dispositivi, dizionari, comandi e board definition. È il punto di accesso per tutti i software consumer che necessitano dei dati dizionario senza accedere direttamente al database.

### Consumer Previsti

| Consumer | API Key Config | Uso |
|----------|---------------|-----|
| Stem.Production.Tracker | `ApiKeys:ProductionTracker` | Board definition per collaudo |
| ButtonPanel.Tester | `ApiKeys:ButtonPanelTester` | Comandi e variabili per test pulsantiere |
| Global Service | `ApiKeys:GlobalService` | Dizionari per comunicazione seriale |
| Stem.Device.Manager | `ApiKeys:StemDeviceManager` | Dizionari per gestione dispositivi |

---

## Caratteristiche

| Feature | Stato | Descrizione |
|---------|-------|-------------|
| **10 Endpoint REST** | ✅ | GET read-only per devices, dictionaries, commands, boards |
| **Health Check** | ✅ | `GET /health` — verifica connessione DB (no auth) |
| **Version** | ✅ | `GET /api/version` — versione e environment (no auth) |
| **Autenticazione API Key** | ✅ | Header `X-Api-Key`, chiavi multiple per consumer (BR-API-001) |
| **Variabili risolte** | ✅ | Merge standard + specifiche con override per-dizionario (BR-API-002) |
| **Comandi per device** | ✅ | Solo comandi abilitati, default enabled (BR-API-003) |
| **JSON camelCase** | ✅ | Null omessi per payload leggeri (BR-API-004) |
| **Board definition** | ✅ | Formato compatibile Production.Tracker (BR-API-005) |
| **Swagger UI** | ✅ | Documentazione interattiva in Development |
| **File .http** | ✅ | Test endpoint integrato in Visual Studio |
| **Dual DB provider** | ✅ | SQLite (dev) / SQL Server (prod), logica centralizzata |
| **Deploy Azure** | ✅ | App Service F1 (Free), deploy via CLI |

---

## Requisiti

- **.NET 10.0** o superiore
- **SQLite** (sviluppo) / **Azure SQL** (produzione)

### Dipendenze Progetto

| Progetto | Ruolo |
|----------|-------|
| Infrastructure | DbContext, Repositories |
| Services | Business logic, domain models |
| Core | Domain models, enums |

### Pacchetti NuGet

| Pacchetto | Versione | Scopo |
|-----------|----------|-------|
| Swashbuckle.AspNetCore.SwaggerUI | 9.0.1 | Swagger UI in Development |
| Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore | 10.0.5 | Health check con verifica DB |

---

## Quick Start

```bash
# Da root soluzione
cd API
dotnet run

# Output atteso:
# Now listening on: http://localhost:5062
```

### Test da Visual Studio

Aprire `API/API.http` e cliccare **"Send Request"** sopra ogni endpoint.

### Test da curl

```bash
curl -H "X-Api-Key: STEM-PT-DEV-KEY-2026" http://localhost:5062/api/devices
```

### Swagger UI

Navigare a `http://localhost:5062/swagger` (solo in Development, senza autenticazione).

---

## Endpoint

| # | Metodo | Route | Auth | Descrizione |
|---|--------|-------|------|-------------|
| 1 | GET | `/api/devices` | ✅ | Lista dispositivi |
| 2 | GET | `/api/devices/{id}` | ✅ | Dettaglio device con board |
| 3 | GET | `/api/devices/{id}/boards` | ✅ | Board di un device |
| 4 | GET | `/api/dictionaries` | ✅ | Lista dizionari con conteggio variabili abilitate |
| 5 | GET | `/api/dictionaries/standard` | ✅ | Dizionario Standard (solo variabili abilitate) |
| 6 | GET | `/api/dictionaries/{id}` | ✅ | Dettaglio dizionario con variabili abilitate |
| 7 | GET | `/api/dictionaries/{id}/resolved` | ✅ | Variabili risolte (standard + specifiche, con override) |
| 8 | GET | `/api/commands` | ✅ | Lista comandi con parametri |
| 9 | GET | `/api/commands/device/{deviceId}` | ✅ | Comandi abilitati per device |
| 10 | GET | `/api/boards/{id}/definition` | ✅ | Board definition (formato Production.Tracker) |
| 11 | GET | `/health` | ❌ | Health check — `Healthy` / `Unhealthy` |
| 12 | GET | `/api/version` | ❌ | Versione e environment |

---

## Autenticazione (BR-API-001)

Ogni richiesta deve includere l'header `X-Api-Key` con una chiave valida configurata in `appsettings.json`:

```json
{
  "ApiKeys": {
    "ButtonPanelTester": "STEM-BT-DEV-KEY-2026",
    "GlobalService": "STEM-GS-DEV-KEY-2026",
    "StemDeviceManager": "STEM-DM-DEV-KEY-2026",
    "ProductionTracker": "STEM-PT-DEV-KEY-2026"
  }
}
```

| Scenario | Risposta |
|----------|----------|
| Header mancante | 401 `{"error": "API Key mancante o non valida."}` |
| Chiave non valida | 401 `{"error": "API Key mancante o non valida."}` |
| Chiave valida | 200 + payload |
| Path `/swagger`, `/openapi`, `/health`, `/api/version` | Bypass autenticazione |

In produzione (Azure App Service), le chiavi vanno configurate come **App Settings**.

---

## Struttura Progetto

```
API/
├── Dtos/                  # DTO per le risposte JSON (7 record)
├── Endpoints/             # Minimal API endpoint groups (4 classi)
├── Mapping/               # ApiMapper (domain → DTO)
├── Middleware/             # ApiKeyMiddleware
├── Properties/            # launchSettings.json
├── Program.cs             # Entry point, DI, middleware pipeline
├── appsettings.json       # Configurazione (DB, API Keys, Logging)
└── API.http               # File test endpoint per Visual Studio
```

---

## Deploy Azure

L'API è deployata su **Azure App Service** (tier F1 Free, Linux).

**URL produzione:** `https://app-dictionaries-manager-prod.azurewebsites.net`

### Configurazione Azure

Le seguenti Environment Variables sono configurate nell'App Service:

| Setting | Tipo | Descrizione |
|---------|------|-------------|
| `DatabaseProvider` | App Setting | `SqlServer` |
| `ConnectionStrings__SqlServer` | Connection String | Connection string Azure SQL |
| `ApiKeys__ProductionTracker` | App Setting | Chiave prod Production.Tracker |
| `ApiKeys__ButtonPanelTester` | App Setting | Chiave prod ButtonPanel.Tester |
| `ApiKeys__GlobalService` | App Setting | Chiave prod Global Service |
| `ApiKeys__StemDeviceManager` | App Setting | Chiave prod Stem.Device.Manager |
| `SCM_DO_BUILD_DURING_DEPLOYMENT` | App Setting | `false` (deploy binari pre-compilati) |

### Comandi deploy

```powershell
# Pulisci artefatti precedenti
Remove-Item -Recurse -Force API/publish, API/deploy.zip -ErrorAction SilentlyContinue

# Pubblica per Linux x64
dotnet publish API -c Release -r linux-x64 --self-contained false -o API/publish

# Crea zip con path Linux-compatibili
cd API/publish; tar -cf ../deploy.zip --format=zip .; cd ../..

# Deploy su Azure
az webapp deploy --name app-dictionaries-manager-prod --resource-group rg-dictionaries-manager-prod --src-path API/deploy.zip --type zip
```

### Verifica post-deploy

```
https://app-dictionaries-manager-prod.azurewebsites.net/api/version
https://app-dictionaries-manager-prod.azurewebsites.net/health
```

---

## Business Rules API

| ID | Regola | Implementazione |
|----|--------|-----------------|
| BR-API-001 | Autenticazione via API Key header | `ApiKeyMiddleware` |
| BR-API-002 | Variabili risolte = standard abilitate (con override) + specifiche abilitate | `DictionaryEndpoints.GetResolved` |
| BR-API-003 | Comandi per device = tutti i comandi dove `DeviceState?.IsEnabled ?? true` | `CommandEndpoints.GetDeviceCommands` |
| BR-API-004 | JSON camelCase, null omessi | `ConfigureHttpJsonOptions` in Program.cs |
| BR-API-005 | Board definition = device + board + variabili risolte ordinate per indirizzo | `BoardEndpoints.GetDefinition` |

---

## Link

- [ISSUES.md](./ISSUES.md) — Issue tracking per il progetto API
- [README principale](../README.md) — Panoramica soluzione
- [CHANGELOG](../CHANGELOG.md) — Storico release
