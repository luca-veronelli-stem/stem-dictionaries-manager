# Quickstart — Bootstrap registration

End-to-end walkthrough of the registration flow once this feature is
implemented. Useful for manual smoke-testing during `/speckit-implement`
and as the "happy path" reference for the integration tests.

## Prerequisites

- API server built and running locally:
  ```powershell
  dotnet run --project src/API
  ```
  defaults to `http://localhost:5062` (see
  `src/API/Properties/launchSettings.json`).
- A configured admin key in `src/API/appsettings.Development.json`:
  ```json
  {
    "AdminApiKeys": [
      "STEM-ADMIN-DEV-KEY-2026"
    ]
  }
  ```
  (`AdminApiKeys` is the **new** section introduced by this feature.
  The legacy `ApiKeys` section continues to authorize non-admin
  endpoints.)
- `curl` (or PowerShell `Invoke-RestMethod`).

## Step 1 — Mint a bootstrap token (admin)

```powershell
curl -X POST http://localhost:5062/api/admin/bootstrap-tokens `
  -H "X-Api-Key: STEM-ADMIN-DEV-KEY-2026" `
  -H "Content-Type: application/json" `
  -d '{ "clientApp": "ButtonPanelTester", "ttlHours": 24 }'
```

Expected `201 Created` response:

```json
{
  "tokenId":   1,
  "clientApp": "ButtonPanelTester",
  "plaintext": "stbt_<43-char-base64url>",
  "mintedAt":  "2026-05-07T10:00:00.000Z",
  "expiresAt": "2026-05-08T10:00:00.000Z"
}
```

**Capture the `plaintext` value now** — this is the only response that
will ever contain it.

## Step 2 — Register an installation (client, unauthenticated)

```powershell
$bootstrap = "stbt_<paste-from-step-1>"
$body = @{
  bootstrapToken = $bootstrap
  descriptor     = @{
    clientApp   = "ButtonPanelTester"
    osUserId    = "$([System.Security.Principal.WindowsIdentity]::GetCurrent().User.Value)"
    machineId   = (Get-CimInstance Win32_ComputerSystemProduct).UUID
    installGuid = [guid]::NewGuid().ToString()
  }
} | ConvertTo-Json

Invoke-RestMethod -Method Post `
  -Uri http://localhost:5062/register `
  -ContentType "application/json" `
  -Body $body
```

Expected `200 OK` response:

```json
{
  "installationId": 1,
  "apiCredential":  "stak_<43-char-base64url>",
  "issuedAt":       "2026-05-07T10:01:00.000Z"
}
```

**Capture the `apiCredential` now.** In a real client this would
immediately go into DPAPI `CurrentUser` and the bootstrap token would
be erased.

## Step 3 — Use the credential against an existing endpoint

```powershell
$cred = "stak_<paste-from-step-2>"
Invoke-RestMethod -Uri http://localhost:5062/api/dictionaries `
  -Headers @{ "X-Api-Key" = $cred }
```

Expected: 200 with the dictionaries list. The `ApiKeyMiddleware` now
accepts both legacy `ApiKeys` config keys and DB-issued installation
credentials (FR-005, union mode).

## Step 4 — Confirm single-use

Re-run **Step 2** with the same bootstrap token. Expected: `409
Conflict` with `{ "error": "registration failed" }`. The body shape
is identical to every other failure mode (FR-002); the status code
carries the failure class — token reuse maps to `TokenAlreadyUsed ->
409` per `contracts/register.md`.

## Step 5 — List installations (admin)

```powershell
Invoke-RestMethod `
  -Uri "http://localhost:5062/api/admin/installations?clientApp=ButtonPanelTester" `
  -Headers @{ "X-Api-Key" = "STEM-ADMIN-DEV-KEY-2026" }
```

Expected: a list including the installation from Step 2 with `status:
"active"`.

## Step 6 — Revoke and observe blast-radius isolation

```powershell
Invoke-RestMethod -Method Post `
  -Uri http://localhost:5062/api/admin/installations/1/revoke `
  -Headers @{ "X-Api-Key" = "STEM-ADMIN-DEV-KEY-2026" }
```

Then re-run Step 3. Expected within ≤ 5 seconds (SC-004): `401
Unauthorized`. Other installations (if any) of the same client app
continue to authenticate normally.

## What to watch for during implementation

- The `RegistrationEvent` table grows by one row per `/register`
  call regardless of outcome (FR-012). Verify by inspecting
  `sqldb-dictionaries-manager-test.db` after Steps 2 + 4.
- The `BootstrapTokens.SecretHash` column stores the PBKDF2 string,
  never the plaintext (FR-014). Same for
  `InstallationApiCredentials.SecretHash` (FR-004).
- The `apiCredential` plaintext appears nowhere in the server logs
  (SC-007). Tail `dotnet run` output during Step 2 to confirm.
- `POST /register` is in the unauth allow-list of `ApiKeyMiddleware`
  (no `X-Api-Key` header required).
