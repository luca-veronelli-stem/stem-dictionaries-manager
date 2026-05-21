# Contract — Admin installation management

Admin-only. Two endpoints to list and revoke per-installation
identities, satisfying FR-010 and FR-011.

## Authentication

`X-Api-Key` header, present in `AdminApiKeys` configuration. Same
auth boundary as `POST /api/admin/bootstrap-tokens`.

## `GET /api/admin/installations`

Lists installations.

### Request

```http
GET /api/admin/installations?clientApp=ButtonPanelTester&status=active HTTP/1.1
X-Api-Key: <admin-key>
```

**Query parameters** (all optional):

- `clientApp` — filter by client app identifier (exact match).
- `status` — `active` | `revoked` | `all` (default `all`).

### Response

```http
HTTP/1.1 200 OK
Content-Type: application/json

[
  {
    "installationId": 142,
    "clientApp":      "ButtonPanelTester",
    "osUserId":       "S-1-5-21-2127521184-1604012920-1887927527-72713",
    "machineId":      "8a5e9b3c-6f4d-4d2a-9c1b-7d8e3f4b6c2a",
    "installGuid":    "f3a8c2e6-2b4d-4f1e-9c3a-8e7d6f5b4a3c",
    "registeredAt":   "2026-05-07T10:23:45.000Z",
    "status":         "active"
  },
  {
    "installationId": 141,
    "clientApp":      "ButtonPanelTester",
    "osUserId":       "S-1-5-21-9876543210-1234567890-2468013579-12345",
    "machineId":      "1b2c3d4e-5f6a-7b8c-9d0e-1f2a3b4c5d6e",
    "installGuid":    "a1b2c3d4-e5f6-7890-abcd-ef0123456789",
    "registeredAt":   "2026-05-06T14:00:12.000Z",
    "status":         "revoked",
    "revokedAt":      "2026-05-07T09:15:00.000Z"
  }
]
```

Empty list ⇒ `200 OK` with `[]`. The endpoint never returns the
plaintext API credential or its hash; only the metadata fields above.
Per the global BR-API-004 JSON convention (`JsonIgnoreCondition
.WhenWritingNull`), nullable fields are **omitted** when their value
is `null`. So `revokedAt` is present (with an ISO-8601 UTC timestamp)
only on revoked rows; on active rows it is absent. Consumers MUST
treat an absent `revokedAt` as semantically equivalent to
`revokedAt = null`.

### Side effects

None. Read-only.

## `POST /api/admin/installations/{id}/revoke`

Revokes one specific installation (and atomically its credential).

### Request

```http
POST /api/admin/installations/142/revoke HTTP/1.1
X-Api-Key: <admin-key>
```

No request body.

### Response — success

```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "installationId": 142,
  "status":         "revoked",
  "revokedAt":      "2026-05-07T11:00:00.000Z"
}
```

Idempotent: revoking an already-revoked installation returns `200 OK`
with the original `revokedAt` (no second mutation, no second audit
row).

### Response — failure

| Status | Body | Cause |
|--------|------|-------|
| 401 | `{ "error": "API key missing or invalid." }` | not in `AdminApiKeys`. |
| 404 | `{ "error": "installation not found" }` | no row with `Id = {id}`. |

### Side effects

On the first (state-changing) revoke:

- `Installation.Status` transitions `Active → Revoked`,
  `Installation.RevokedAt = DateTime.UtcNow`.
- `InstallationApiCredential.Status` transitions `Active → Revoked`
  for the owning installation, with the same `RevokedAt`.
- `AuditEntry` row inserted via
  `IAuditService.LogUpdateAsync(EntityType: Installation,
  EntityId: 142, ChangedById: <admin-id>, …)` with the
  Active→Revoked status change in the `previousValue`/`newValue`
  payload.
- The validation cache (R4 in `research.md`) is invalidated for the
  revoked credential's hash, so authenticated calls using the
  credential start failing in well under the 5-second SC-004 ceiling.
