# API - ISSUES

> **Scopo:** Questo documento traccia problemi, miglioramenti e code smells per il progetto **API** di Stem.Dictionaries.Manager.

> **Ultimo aggiornamento:** 2026-04-10

---

## Riepilogo

| Priorità | Aperte | Risolte |
|----------|--------|---------|
| **Critica** | 0 | 0 |
| **Alta** | 0 | 0 |
| **Media** | 0 | 0 |
| **Bassa** | 3 | 0 |

**Totale aperte:** 3  
**Totale risolte:** 0

---

## Indice Issue Aperte

- [API-001 - Swagger UI non supporta API Key authentication](#api-001--swagger-ui-non-supporta-api-key-authentication)
- [API-002 - Endpoint non hanno response type annotations](#api-002--endpoint-non-hanno-response-type-annotations)
- [API-003 - Manca rate limiting](#api-003--manca-rate-limiting)

---

## Issue Aperte

### API-001 — Swagger UI non supporta API Key authentication

| Campo | Valore |
|-------|--------|
| **ID** | API-001 |
| **Categoria** | UX |
| **Priorità** | Bassa |
| **Status** | Aperto |
| **Data Apertura** | 2026-04-10 |

**Descrizione:**  
La Swagger UI (`/swagger`) non mostra il bottone "Authorize" per inserire l'API Key. Gli endpoint restituiscono 401 quando provati da Swagger. L'API di `Microsoft.OpenApi` v2 in .NET 10 ha cambiato le interfacce e la configurazione del SecurityScheme non è compatibile col vecchio pattern.

**Workaround:**  
Usare il file `API.http` integrato in Visual Studio o curl con header `X-Api-Key`.

**Soluzione proposta:**  
Aggiornare la configurazione OpenAPI quando la documentazione di `Microsoft.OpenApi` v2 sarà stabile, oppure usare un `IDocumentFilter` custom.

---

### API-002 — Endpoint non hanno response type annotations

| Campo | Valore |
|-------|--------|
| **ID** | API-002 |
| **Categoria** | API |
| **Priorità** | Bassa |
| **Status** | Aperto |
| **Data Apertura** | 2026-04-10 |

**Descrizione:**  
Gli endpoint Minimal API non hanno `.Produces<T>()` / `.ProducesNotFound()` annotations. La specifica OpenAPI generata non include i tipi di risposta, il che rende la documentazione Swagger meno informativa.

**Soluzione proposta:**  
Aggiungere `.Produces<DeviceSummaryDto[]>(200)` e simili a ogni endpoint mapping.

---

### API-003 — Manca rate limiting

| Campo | Valore |
|-------|--------|
| **ID** | API-003 |
| **Categoria** | Security |
| **Priorità** | Bassa |
| **Status** | Aperto |
| **Data Apertura** | 2026-04-10 |

**Descrizione:**  
Non c'è rate limiting sugli endpoint. Un consumer con API Key valida potrebbe sovraccaricare il server con richieste eccessive.

**Soluzione proposta:**  
Aggiungere `Microsoft.AspNetCore.RateLimiting` con policy per API Key quando l'API sarà in produzione.

---
