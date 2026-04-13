# API - ISSUES

> **Scopo:** Questo documento traccia problemi, miglioramenti e code smells per il progetto **API** di Stem.Dictionaries.Manager.

> **Ultimo aggiornamento:** 2026-04-13

---

## Riepilogo

| Priorità | Aperte | Risolte |
|----------|--------|---------|
| **Critica** | 0 | 0 |
| **Alta** | 0 | 0 |
| **Media** | 0 | 0 |
| **Bassa** | 4 | 0 |

**Totale aperte:** 4  
**Totale risolte:** 0

---

## Indice Issue Aperte

- [API-001 - Swagger UI non supporta API Key authentication](#api-001--swagger-ui-non-supporta-api-key-authentication)
- [API-002 - Endpoint non hanno response type annotations](#api-002--endpoint-non-hanno-response-type-annotations)
- [API-003 - Manca rate limiting](#api-003--manca-rate-limiting)
- [API-004 - Endpoint restituiscono 500 con stacktrace se DB non raggiungibile](#api-004--endpoint-restituiscono-500-con-stacktrace-se-db-non-raggiungibile)

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

### API-004 — Endpoint restituiscono 500 con stacktrace se DB non raggiungibile

| Campo | Valore |
|-------|--------|
| **ID** | API-004 |
| **Categoria** | Robustezza |
| **Priorità** | Bassa |
| **Status** | Aperto |
| **Data Apertura** | 2026-04-13 |
| **Correlata** | [GUI-010](../GUI.Windows/ISSUES.md#gui-010--manca-gestione-errore-connessione-db-allavvio) (stesso problema lato GUI) |

**Descrizione:**  
Se Azure SQL non è raggiungibile (rete assente, firewall, DNS, timeout), i 10 endpoint business restituiscono `500 Internal Server Error` con stacktrace `SqlException` nel body. L'health check `/health` ritorna correttamente `503 Unhealthy`, ma gli endpoint business non hanno gestione strutturata dell'errore.

**Scenario:**
1. Azure SQL temporaneamente non raggiungibile
2. Consumer chiama `GET /api/devices` con API Key valida
3. EF Core lancia `SqlException` (timeout/connection refused)
4. ASP.NET ritorna 500 con stacktrace (in Development) o body vuoto (in Production)
5. Consumer non riceve un messaggio JSON strutturato

**Soluzione proposta:**  
Aggiungere un global exception handler middleware che cattura `SqlException` e `TimeoutException` e ritorna un JSON strutturato `503 Service Unavailable`:

```csharp
app.Use(async (context, next) =>
{
    try
    {
        await next(context);
    }
    catch (Exception ex) when (ex is SqlException or TimeoutException)
    {
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            error = "Database non raggiungibile. Riprovare tra qualche minuto."
        });
    }
});
```

**Note:**
- L'health check `/health` già segnala `Unhealthy` quando il DB è giù — i consumer possono usarlo per monitoraggio
- Il middleware va registrato **prima** di `UseRouting` per catturare le eccezioni degli endpoint
- Priorità bassa: l'API gira su Azure con DB Azure SQL nella stessa region (Italy North), interruzioni rare

---
