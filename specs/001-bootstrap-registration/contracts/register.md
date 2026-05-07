# Contract — `POST /register`

The bootstrap registration endpoint. Public (unauthenticated), per
issue #1: it is the entry point that *establishes* authentication for
a new installation.

## Request

```http
POST /register HTTP/1.1
Content-Type: application/json

{
  "bootstrapToken": "stbt_<43-char-base64url>",
  "descriptor": {
    "clientApp":   "ButtonPanelTester",
    "osUserId":    "S-1-5-21-2127521184-1604012920-1887927527-72713",
    "machineId":   "8a5e9b3c-6f4d-4d2a-9c1b-7d8e3f4b6c2a",
    "installGuid": "f3a8c2e6-2b4d-4f1e-9c3a-8e7d6f5b4a3c",
    "appVersion":  "1.0.0"
  }
}
```

**Field constraints**:

- `bootstrapToken`: required, non-empty string. Server-side validation
  is by lookup of its PBKDF2 hash; format is informational.
- `descriptor.clientApp`: required, non-empty. Must byte-match the
  bootstrap token's recorded `ClientApp` scope (mismatch ⇒ 401 with
  the same body shape as any other failure). Free-text on the wire,
  but **by convention** the consumer sends the same string the admin
  used at mint time, which is the `ApiKeys` config-key form
  (`ButtonPanelTester`, `GlobalService`, `StemDeviceManager`,
  `ProductionTracker`). The server does not maintain a repo-name → config-key
  map.
- `descriptor.osUserId`: required, non-empty. Stable per-(OS user)
  identifier — server-opaque. Consumers MAY send a raw value
  (Windows SID, POSIX `UID:username`) or a SHA-256 hex digest of one
  for privacy reasons; the server stores whatever string is sent.
- `descriptor.machineId`: required, non-empty. Stable per-machine
  fingerprint — server-opaque. Same raw-or-hashed flexibility as
  `osUserId`.
- `descriptor.installGuid`: required, parseable as a `Guid`. Server
  rejects the all-zeros `Guid`.
- `descriptor.appVersion`: optional. Semver string for ops correlation
  (e.g. matching version-specific bug reports to installations). When
  present MUST be non-empty after trim; the server does NOT validate
  against the semver grammar.

Additional properties on `descriptor` are accepted, persisted into the
audit's `descriptorJson`, and ignored for validation (forward-compat).

## Response — success

```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "installationId": 142,
  "apiCredential":  "stak_<43-char-base64url>",
  "issuedAt":       "2026-05-07T10:23:45.000Z"
}
```

The plaintext `apiCredential` is returned **exactly once**, here. The
client MUST persist it under the OS's per-user secret store (DPAPI
`CurrentUser` on Windows, equivalent elsewhere) and MUST erase the
bootstrap token from any client-side storage immediately after this
response.

## Response — failure (any reason)

```http
HTTP/1.1 401 Unauthorized
Content-Type: application/json

{ "error": "registration failed" }
```

This identical body is returned for every failure mode:

- Bootstrap token missing or empty
- Bootstrap token unknown (no matching hash)
- Bootstrap token already used
- Bootstrap token expired
- Bootstrap token revoked
- Bootstrap token's `ClientApp` ≠ descriptor's `clientApp`
- Descriptor missing required fields or malformed
- Descriptor's `installGuid` is the zero GUID
- Audit DB write fails (FR-013) — note: this is the one failure mode
  that returns 500, not 401, because returning 401 on an audit failure
  would falsely tell the client "your token is bad" when in fact the
  server failed.

The `error` text is the same single string regardless of
`Outcome` recorded server-side. Per FR-002, the response MUST NOT
reveal which condition was violated.

## Side effects

On every call (success or failure):

- A `RegistrationEvent` row is committed before the response is
  returned (FR-012/FR-013). If the audit write fails, the registration
  itself fails and the response is 500 with body
  `{ "error": "audit failure" }` (operator-actionable; does not leak
  token validity).

On success only:

- `BootstrapToken.Status` transitions `Issued → Used`,
  `BootstrapToken.UsedAt` is set, and
  `BootstrapToken.ConsumedByInstallationId` is set to the new
  Installation's `Id`.
- A new `Installation` row is inserted.
- A new `InstallationApiCredential` row is inserted with the PBKDF2
  hash of the freshly-minted plaintext credential.

All four writes are committed in a single SaveChangesAsync transaction
(invariant 3 in `data-model.md`).

## Authentication / middleware

`POST /register` is publicly reachable. The `ApiKeyMiddleware` skips
it via the same allow-list path used today for `/openapi`, `/swagger`,
`/health`, `/api/version`. Update the middleware's allow-list to
include `/register`.

## Idempotency

Not idempotent. A client that retries the same `POST /register` with
the same bootstrap token on the second attempt will receive the
failure response (token already used). The first response was the
only opportunity to capture the plaintext `apiCredential`. Clients
MUST NOT retry blindly; on network failure mid-call, the recovery
path is operator-side: admin revokes any installation that may have
been created and mints a new bootstrap token.
