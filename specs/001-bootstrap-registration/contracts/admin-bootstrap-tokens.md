# Contract — `POST /api/admin/bootstrap-tokens`

Admin-only. Mints a new single-use bootstrap token scoped to a client
app. Returns the plaintext value exactly once.

The minted `stbt_<...>` plaintext is the "bootstrap token a technician
supplies" in
[`docs/Standards/CLIENT_REGISTRATION.md`](../../../docs/Standards/CLIENT_REGISTRATION.md):
a consumer exchanges it **once**, at the unauthenticated
[`POST /register`](./register.md), for a long-lived `stak_<...>`
installation credential. This endpoint is the server-side mint; the
single-use / not-idempotent / revoke semantics the standard relies on
are enforced at `/register` (see *Side effects* and *Idempotency*
there).

## Authentication

`X-Api-Key` header. Key MUST be present in the new `AdminApiKeys`
configuration section (separate from `ApiKeys`). Returns 401 if
missing or not in `AdminApiKeys`.

## Request

```http
POST /api/admin/bootstrap-tokens HTTP/1.1
Content-Type: application/json
X-Api-Key: <admin-key>

{
  "clientApp":  "ButtonPanelTester",
  "ttlHours":   720
}
```

**Field constraints**:

- `clientApp`: required, non-empty. Free-text identifier matching the
  `ApiKeys` config shape (e.g. `ButtonPanelTester`, `GlobalService`,
  `StemDeviceManager`, `ProductionTracker`). The server does not
  cross-validate the value against any allow-list — admins are
  trusted to spell client app identifiers correctly.
- `ttlHours`: optional. If omitted, defaults to **720** (30 days).
  Must be in the closed interval `[1, 2160]` (1 hour to 90 days, per
  FR-007). Values outside the interval ⇒ 400 Bad Request with
  `{ "error": "ttlHours out of range [1, 2160]" }`.

## Response — success

```http
HTTP/1.1 201 Created
Content-Type: application/json

{
  "tokenId":      37,
  "clientApp":    "ButtonPanelTester",
  "plaintext":    "stbt_<43-char-base64url>",
  "mintedAt":     "2026-05-07T10:00:00.000Z",
  "expiresAt":    "2026-06-06T10:00:00.000Z"
}
```

The `plaintext` field is returned **exactly once** here. No subsequent
admin call returns it (FR-014). Admin transmits it to the intended OS
user out-of-band (email, internal portal, USB).

## Response — failure

| Status | Body | Cause |
|--------|------|-------|
| 400 | `{ "error": "clientApp is required" }` | empty/missing `clientApp`. |
| 400 | `{ "error": "ttlHours out of range [1, 2160]" }` | TTL out of bounds. |
| 401 | `{ "error": "API key missing or invalid." }` | not in `AdminApiKeys`. |

## Side effects

- One row inserted into `BootstrapTokens` with `Status = Issued`.
- The plaintext is **not** persisted; only its PBKDF2 hash.
- No audit row. Mint events are recorded in the standard
  `AuditEntry` table via existing `IAuditService.LogCreateAsync`
  (mint is a CRUD-style event with an authenticated admin user, which
  fits `AuditEntry`'s shape).
