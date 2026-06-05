# Quickstart — Atomic re-registration

End-to-end smoke procedure exercising US1 (technician credential
recovery) and US2 (operator log visibility). Mirrors the
acceptance scenarios in `spec.md`.

## Prerequisites

- API running locally (`dotnet run --project src/API`) against a
  SQLite dev DB.
- `dotnet ef database update -p src/Infrastructure -s src/API` has
  been run so the `MultiActiveCredentialPerInstallationGuard` index
  is in place.
- An admin API key registered under `AdminApiKeys` in
  `appsettings.Development.json`.

## Happy-path re-registration

```bash
# 1. Mint T1
T1=$(curl -s -X POST http://localhost:5000/api/admin/bootstrap-tokens \
  -H "X-Api-Key: <admin-key>" -H "Content-Type: application/json" \
  -d '{"clientApp":"ButtonPanelTester","ttlHours":24}' | jq -r .plaintext)

# 2. Register with T1 → credential C1
G=$(uuidgen)
C1=$(curl -s -X POST http://localhost:5000/register \
  -H "Content-Type: application/json" \
  -d "{\"bootstrapToken\":\"$T1\",\"descriptor\":{\"clientApp\":\"ButtonPanelTester\",\"osUserId\":\"u1\",\"machineId\":\"m1\",\"installGuid\":\"$G\",\"appVersion\":\"1.0.0\"}}" \
  | jq -r .apiCredential)

# 3. Verify C1 works
curl -s -o /dev/null -w "%{http_code}\n" \
  http://localhost:5000/api/dictionaries \
  -H "X-Api-Key: $C1"
# Expected: 200

# 4. Mint T2 (same clientApp)
T2=$(curl -s -X POST http://localhost:5000/api/admin/bootstrap-tokens \
  -H "X-Api-Key: <admin-key>" -H "Content-Type: application/json" \
  -d '{"clientApp":"ButtonPanelTester","ttlHours":24}' | jq -r .plaintext)

# 5. Re-register against same InstallGuid → credential C2
C2=$(curl -s -X POST http://localhost:5000/register \
  -H "Content-Type: application/json" \
  -d "{\"bootstrapToken\":\"$T2\",\"descriptor\":{\"clientApp\":\"ButtonPanelTester\",\"osUserId\":\"u1\",\"machineId\":\"m1\",\"installGuid\":\"$G\",\"appVersion\":\"1.0.0\"}}" \
  | jq -r .apiCredential)

# 6. C2 ≠ C1
[ "$C1" != "$C2" ] && echo OK || echo FAIL

# 7. C2 works
curl -s -o /dev/null -w "%{http_code}\n" \
  http://localhost:5000/api/dictionaries \
  -H "X-Api-Key: $C2"
# Expected: 200

# 8. After 5 s positive-cache TTL, C1 is dead
sleep 6
curl -s -o /dev/null -w "%{http_code}\n" \
  http://localhost:5000/api/dictionaries \
  -H "X-Api-Key: $C1"
# Expected: 401
```

## Audit-log inspection

```sql
SELECT Id, OccurredAt, ClaimedClientApp, Outcome, ResultingInstallationId
FROM RegistrationEvents
WHERE ClaimedInstallGuid = '<G>'
ORDER BY OccurredAt;
```

Expected output: two rows, both with `ResultingInstallationId` set
to the same Installation row, and outcomes:

| Row 1 | Row 2 |
|---|---|
| `Success` | `ReRegistrationSuccess` |

## Cross-app reuse (rejected)

Mint T3 scoped to `GlobalService`, attempt to re-register against
the same `installGuid`:

```bash
T3=$(curl -s -X POST http://localhost:5000/api/admin/bootstrap-tokens \
  -H "X-Api-Key: <admin-key>" -H "Content-Type: application/json" \
  -d '{"clientApp":"GlobalService","ttlHours":24}' | jq -r .plaintext)

curl -s -o /dev/null -w "%{http_code}\n" -X POST http://localhost:5000/register \
  -H "Content-Type: application/json" \
  -d "{\"bootstrapToken\":\"$T3\",\"descriptor\":{\"clientApp\":\"GlobalService\",\"osUserId\":\"u1\",\"machineId\":\"m1\",\"installGuid\":\"$G\",\"appVersion\":\"1.0.0\"}}"
# Expected: 401
```

Audit row appears with `Outcome = ClientScopeMismatch`. No row in
`Installations` or `InstallationApiCredentials` is mutated.

## Revoked-installation rejection

(Requires admin revoke endpoint from issue #68 — out of scope for
this PR. Verifiable today by manually flipping
`Installations.Status` in SQLite and rerunning the re-registration
step.)

```bash
sqlite3 src/API/bin/Debug/net10.0/dictionaries.db \
  "UPDATE Installations SET Status = 1, RevokedAt = '$(date -u +%Y-%m-%dT%H:%M:%SZ)' WHERE InstallGuid = '$G';"

# Now re-register with a fresh token
curl -s -o /dev/null -w "%{http_code}\n" -X POST http://localhost:5000/register ...
# Expected: 423   (ExistingInstallationRevoked; was 401 before #85)
```

Audit row appears with `Outcome = ExistingInstallationRevoked` (the
new server-only outcome).

## FR-008 — log visibility on swallowed failures

Triggerable by stubbing the audit repository to throw in an
integration test (see
`tests/Tests/Integration/API/Auth/RegisterEndpointTests.cs` →
`Post_SwallowedExceptionInService_LogsErrorAndReturns500`). The
captured log output must include an error-level entry with the
exception attached and one of the structured fields (`SourceIp`,
`ClientApp`, `InstallGuid`).
