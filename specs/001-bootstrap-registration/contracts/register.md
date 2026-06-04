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
  bootstrap token's recorded `ClientApp` scope **and** must be a
  registered key in the per-`clientApp` descriptor-policy registry
  (see *Per-clientApp descriptor policies* below). A scope mismatch or
  a lookup miss both fail with **401**, deliberately conflated with
  the "token unknown" failure to hide which app a token was scoped to.
  Free-text on the wire, but **by convention** the consumer sends the
  same string the admin used at mint time, which is the `ApiKeys`
  config-key form (`ButtonPanelTester`, `GlobalService`,
  `StemDeviceManager`, `ProductionTracker`). The server does not
  maintain a repo-name → config-key map.
- `descriptor.osUserId`: nullable at the schema level. Presence is
  required per the active `DescriptorPolicy` for the request's
  `clientApp` (see below). When the policy requires it and the field
  is missing/empty, the response is **400 Bad Request** with
  `DescriptorMissingField`. Server-opaque storage. See *Privacy
  posture* for the raw-vs-hashed guidance.
- `descriptor.machineId`: nullable at the schema level. Same
  policy-driven enforcement as `osUserId`.
- `descriptor.installGuid`: required, parseable as a `Guid`. The
  all-zeros `Guid` is rejected with `InstallGuidInvalid → 400`. This
  is a universal invariant — every platform can generate a random
  128-bit GUID client-side, so the requirement is not per-policy.
- `descriptor.appVersion`: optional. When present MUST conform to the
  [SemVer 2.0](https://semver.org/spec/v2.0.0.html) grammar
  (`MAJOR.MINOR.PATCH[-prerelease][+build]`, all parts of which are
  validated by the standard SemVer regex). Malformed values are
  rejected with `DescriptorMalformed → 400`. Used for ops correlation
  (matching version-specific bug reports to installations).

Additional properties on `descriptor` are accepted, persisted into the
audit's `descriptorJson`, and ignored for validation (forward-compat).

## Per-clientApp descriptor policies

Different consumer apps run on platforms with different identity
guarantees. A Windows desktop tool has a strong machine UUID and a
stable OS-user SID; a mobile app has neither; a headless service may
have no per-user identity at all. The server therefore enforces
descriptor-field presence per `clientApp`, not uniformly.

The policy registry is a server-side `IReadOnlyDictionary<string,
DescriptorPolicy>` keyed by `clientApp`:

```csharp
public sealed record DescriptorPolicy(
    bool OsUserIdRequired,
    bool MachineIdRequired);
```

Today's only registered consumer:

| `clientApp` | `OsUserIdRequired` | `MachineIdRequired` |
|---|---|---|
| `ButtonPanelTester` | `true` | `true` |

When the lookup misses (`clientApp` field absent, empty, or carrying a
string that no policy is registered for), the response is **401**
conflated with token-unknown / scope-mismatch. New consumer apps must
land a policy entry before they can register installations.

The policy record intentionally has two bools, not four. `clientApp`
required-ness is enforced by the lookup mechanism itself; `installGuid`
required-ness is a contract-level invariant (every platform can produce
a GUID) and lives at the DTO / `InstallGuidInvalid` outcome layer.

## Privacy posture

The server stores `osUserId` and `machineId` as opaque strings — it
never parses, joins, or correlates them against external systems. Two
guidance levels apply, depending on the deployment topology of the
consumer:

- **All consumers SHOULD** transmit a SHA-256 hex digest of the raw
  identifier rather than the raw value. The hash is collision-resistant
  enough (2^256) to function as a per-machine / per-user fingerprint for
  revocation and forensics, and the server doesn't need the raw value
  for any of its responsibilities.
- **Supplier-deployed consumers MUST** hash. When the consumer runs on
  hardware owned by an external organization (e.g.
  `button-panel-tester` shipped to a STEM-Ems supplier), the OS user
  ID and machine ID cross an organizational data boundary, and STEM
  must not receive raw values. This is a cross-org compliance posture,
  not a defense-in-depth nicety.

The wire format is identical in both cases (a string); the consumer
chooses the source before sending. The audit log records exactly what
the consumer transmitted.

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

## Response — failure

Every failure response shares the envelope shape:

```http
HTTP/1.1 <status>
Content-Type: application/json

{ "error": "<short message>" }
```

The status code distinguishes the failure class; the body always uses
the same `{ "error": "..." }` envelope. The `error` message is a short
operator/developer hint, not a token-validity oracle.

### Status → outcome map

| Status | `RegistrationOutcome` | Trigger |
|---|---|---|
| `400 Bad Request` | `TokenMissing` | The `bootstrapToken` field is absent or empty. (Client bug, not a token-validity oracle — distinguishing missing from invalid does not reveal any token's scope.) |
| `400 Bad Request` | `DescriptorMalformed` | Descriptor JSON is malformed, an unparseable Guid string, or `appVersion` fails the SemVer 2.0 grammar. |
| `400 Bad Request` | `DescriptorMissingField` | A descriptor field required by the active `DescriptorPolicy` (e.g. `osUserId` for `ButtonPanelTester`) is missing or empty. |
| `400 Bad Request` | `InstallGuidInvalid` | `descriptor.installGuid` parsed to `Guid.Empty` (the all-zeros GUID). |
| `401 Unauthorized` | `TokenInvalid` *or* `ClientScopeMismatch` *or* policy-lookup miss | **Deliberately conflated.** The bootstrap token is unknown, OR the token's `ClientApp` scope does not match the descriptor's `clientApp`, OR the request's `clientApp` is absent / not in the policy registry. The response does not distinguish these three causes — see *Threat model* below. |
| `409 Conflict` | `TokenAlreadyUsed` | The bootstrap token has been consumed by a prior successful registration. The race-loser branch of a concurrent ceremony also lands here. |
| `410 Gone` | `TokenExpired` | The bootstrap token's TTL has elapsed. |
| `423 Locked` | `TokenRevoked` | The bootstrap token has been administratively revoked. |
| `423 Locked` | `ExistingInstallationRevoked` | A fresh, valid bootstrap token validated against an existing **Revoked** Installation with matching `clientApp` (spec 002 / #71). Fires only **after** the token's validity and the client-app scope have been verified, so it reveals no token-scope information and is distinguishable per the narrowed FR-002 (clarification 2026-06-04). Mirrors `TokenRevoked`'s 423 — a revoked/locked resource. The Installation is NOT auto-unrevoked; recovery requires a separate admin flow. |
| `500 Internal Server Error` | `AuditFailure` | The pre-response `RegistrationEvent` write failed (FR-013). Body becomes `{ "error": "audit failure" }` — this is the only failure mode that doesn't use "registration failed" as the message, because operator-actionable distinct from "your token is bad". |

Server-only outcome (spec 002 / #71) — wire response identical to a
first-time `Success`, distinguishable only in
`RegistrationEvents.Outcome`:

| Wire shape | `RegistrationOutcome` | Trigger |
|---|---|---|
| `200 OK` (Success-shape body, new credential plaintext) | `ReRegistrationSuccess` | A fresh bootstrap token validated against an existing **Active** Installation with matching ClientApp. The atomic re-registration path ran. See *Re-registration path* below. |

`ExistingInstallationRevoked` was historically listed here as a
`401`-conflated server-only outcome; since #85 it maps to its own
`423 Locked` (in the status map above), because it fires only after
token + scope validation and therefore leaks no token-scope
information.

The audit log records the exact `RegistrationOutcome` value for every
attempt regardless of which status code was returned — server-side ops
sees the full picture, the client sees only the actionable status.

### Threat model — what the 401 conflation buys

Three distinct causes share the 401 response: an unknown token, a
token whose scope does not match the claimed `clientApp`, and a
request whose `clientApp` is not in the policy registry. The
conflation hides which apps a given token was scoped to. A token
holder cannot probe "is this token scoped to `GlobalService`?" by
trying different `clientApp` values — every wrong guess returns
the same 401 as an unknown token. The bootstrap token's entropy
(43-char base64url ≈ 2^258) makes random-guess enumeration
infeasible regardless, so this conflation is defense-in-depth, not
the primary line.

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

### Re-registration path (spec 002 / #71)

When all of the following hold:

- The bootstrap token validates as `Issued` and matches the request's
  `clientApp` scope.
- The descriptor validates fully.
- An `Installation` row already exists for the request's
  `installGuid`, its `ClientApp` byte-matches the request's
  `ClientApp`, and its `Status` is `Active`.

…the endpoint takes the **re-registration path** (option B from
issue #71, FR-018 from spec 001):

1. Every `Active` `InstallationApiCredential` row for the matched
   installation is flipped to `Status = Revoked`, `RevokedAt = now`.
   Prior `SecretHash` values are preserved (forensic value).
2. A new `InstallationApiCredential` row is inserted with
   `Status = Active` and a freshly-generated `SecretHash`.
3. The bootstrap token transitions `Issued → Used` exactly as in the
   first-time path.
4. A `RegistrationEvent` audit row is inserted with
   `Outcome = ReRegistrationSuccess` — server-only outcome; wire
   response is byte-identical to a first-time `Success` (200 + new
   credential body).

All four writes commit in a single SaveChangesAsync transaction.

When the matched installation's `ClientApp` differs from the
request's `ClientApp`, the request is rejected through the existing
conflated 401 path (`Outcome = ClientScopeMismatch`); no row in
`Installations` or `InstallationApiCredentials` is mutated.

When the matched installation's `Status` is `Revoked`, the request is
rejected with `423 Locked` (`Outcome = ExistingInstallationRevoked`).
This outcome fires only after the token and client-app scope have
already validated, so per the narrowed FR-002 (clarification
2026-06-04) it gets its own RFC-meaningful status rather than the
conflated 401 — mirroring `TokenRevoked`. The installation is **not**
auto-unrevoked; a separate admin flow is required to recover.

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
