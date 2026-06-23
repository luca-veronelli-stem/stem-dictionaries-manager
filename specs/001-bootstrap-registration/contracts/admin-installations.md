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
- `status` — `active` | `revoked` | `all` (default `all`). A value
  outside that set is rejected with **400 Bad Request** and
  `{ "error": "status must be 'active', 'revoked', or 'all'" }`; an
  absent or empty `status` is treated as `all`.

### Response

```http
HTTP/1.1 200 OK
Content-Type: application/json

[
  {
    "installationId": 142,
    "clientApp":      "ButtonPanelTester",
    "osUserId":       "5f633273852092b9d0e6075b4f761b331837e410d5f1dfb7dbe0d654fce37598",
    "machineId":      "b59ad516b32a60478e4331ae1f44793a445c348ec80a5b9f72e811e8914062af",
    "installGuid":    "f3a8c2e6-2b4d-4f1e-9c3a-8e7d6f5b4a3c",
    "registeredAt":   "2026-05-07T10:23:45.000Z",
    "status":         "active"
  },
  {
    "installationId": 141,
    "clientApp":      "ButtonPanelTester",
    "osUserId":       "417c9ec29fc3c9421c8e1832b034f926e39ae6578d69095de4fdbcdaf1f2ba3f",
    "machineId":      "45e1629458283d0fc0f6de76264b077e7a5040480e69ea85c47f5878f1c10906",
    "installGuid":    "a1b2c3d4-e5f6-7890-abcd-ef0123456789",
    "registeredAt":   "2026-05-06T14:00:12.000Z",
    "status":         "revoked",
    "revokedAt":      "2026-05-07T09:15:00.000Z"
  }
]
```

Empty list ⇒ `200 OK` with `[]`. The endpoint never returns the
plaintext API credential or its hash; only the metadata fields above.
`osUserId` / `machineId` are echoed back exactly as the consumer
transmitted them at registration — opaque strings, shown here as the
SHA-256 hex digests the privacy posture of
[`CLIENT_REGISTRATION.md`](../../../docs/Standards/CLIENT_REGISTRATION.md)
mandates on the wire (the server never parses or correlates them).
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
