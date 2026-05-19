# Deploying the API to Azure App Service

Operations runbook for the dictionaries-manager **API** (`src/API/`). The GUI desktop app is shipped separately through `.github/workflows/release.yml`.

The deploy pipeline is `.github/workflows/deploy-api.yml` and triggers on `v*.*.*` tag pushes (and manual `workflow_dispatch`). It:

1. Builds and publishes the API framework-dependent against the runtime that App Service hosts.
2. Generates an **idempotent** EF Core migration script and applies it to Azure SQL.
3. Zip-deploys the publish output to App Service via `azure/webapps-deploy@v3`.
4. Runs a post-deploy smoke check against `/health` and `/api/version`.

Steps 2 and 4 fail fast — a broken migration or a smoke failure leaves the previous slot's binary untouched (manual remediation, see [Rollback](#rollback)).

## Target

| Field | Value |
| --- | --- |
| App Service | `app-dictionaries-manager-prod` |
| URL | <https://app-dictionaries-manager-prod.azurewebsites.net> |
| Runtime | .NET 10 (Linux) |
| Database | Azure SQL — `sqldb-dictionaries-manager-prod` |
| GitHub Environment | `production` (reviewer protection lives here, not on `main`) |

## App Service Configuration

These keys live under App Service → **Settings → Configuration → Application settings**. Anything marked **secret** must also be a Key Vault reference in real prod; the leftmost column is what the API code reads.

| Key | Required | Secret | Value |
| --- | --- | --- | --- |
| `DatabaseProvider` | yes | no | `SqlServer` |
| `ConnectionStrings__SqlServer` | yes | **yes** | ADO.NET connection string for `sqldb-dictionaries-manager-prod`. Use SQL auth or AAD auth; the API never parses it, just passes it to EF Core. |
| `AdminApiKeys__0`, `AdminApiKeys__1`, ... | yes | **yes** | Admin API keys accepted on `/api/admin/*` and (additively) on every other API path via `ApiKeyMiddleware`. Use long random secrets. At least one entry required to mint bootstrap tokens. |
| `ApiKeys__<ClientApp>` | optional | yes | Legacy named API keys for clients that haven't migrated to `/register` yet. Names match the consumers (`ButtonPanelTester`, `GlobalService`, `StemDeviceManager`, `ProductionTracker`). Safe to omit when the consumer ships via `/register`. |
| `ASPNETCORE_ENVIRONMENT` | yes | no | `Production`. Disables Swagger UI and Development-only middleware. |
| `Logging__LogLevel__Default` | optional | no | `Information` recommended. App Service surfaces logs in Log stream and Application Insights if attached. |

Why double-underscore? ASP.NET Core maps `Section:Key` to `Section__Key` for env vars, and App Service Application settings become env vars on the host. `ConnectionStrings:SqlServer` → `ConnectionStrings__SqlServer`. Arrays (`AdminApiKeys`) are indexed by integer suffix.

## Provisioning federated identity (OIDC)

The deploy workflow uses GitHub OIDC, not a stored publish profile. Setup is one-time per environment.

### Step 1 — App Registration

1. Azure Portal → **Microsoft Entra ID → App registrations → New registration**.
2. Name: `gh-actions-dictionaries-manager-prod`. Single-tenant. No redirect URI.
3. Note the **Application (client) ID** and **Directory (tenant) ID**.

### Step 2 — Federated Credentials

App registration → **Certificates & secrets → Federated credentials → Add credential**. Add one entry for the `production` environment:

| Field | Value |
| --- | --- |
| Federated credential scenario | GitHub Actions deploying Azure resources |
| Organization | `luca-veronelli-stem` |
| Repository | `stem-dictionaries-manager` |
| Entity type | Environment |
| GitHub environment name | `production` |
| Name | `gh-prod-environment` |
| Audience | `api://AzureADTokenExchange` (default) |

The federation matches when GitHub Actions runs a job targeting the `production` environment. The workflow declares `environment: production` so this binds correctly.

If you want to gate ad-hoc tag pushes without the `production` environment, add a second credential with **Entity type = Branch** and the tag pattern — but the recommended path is environment-only, so the environment's required reviewers (set on the GitHub side) gate the deploy.

### Step 3 — Role assignments

The App Registration's service principal needs:

- **Website Contributor** (or finer-grained `Microsoft.Web/sites/publish/Action`) on `app-dictionaries-manager-prod` — required for zip-deploy.
- **SQL Server Contributor** on `sqldb-dictionaries-manager-prod`'s server *only if* migrations run as the federated identity rather than via the SQL connection-string secret. The default path uses SQL auth from the connection string, so this is **not required**.

Portal: App Service → **Access control (IAM) → Add role assignment → Service principal → `gh-actions-dictionaries-manager-prod`**.

### Step 4 — GitHub repository variables

Repo Settings → **Secrets and variables → Actions → Variables tab**. These are not sensitive (client and tenant IDs can be public).

| Variable | Value |
| --- | --- |
| `AZURE_CLIENT_ID` | App Registration's Application (client) ID. |
| `AZURE_TENANT_ID` | Directory (tenant) ID. |
| `AZURE_SUBSCRIPTION_ID` | Subscription that hosts the App Service. |

### Step 5 — GitHub repository secrets

Repo Settings → **Secrets and variables → Actions → Secrets tab**.

| Secret | Value |
| --- | --- |
| `AZURE_SQL_CONNECTION_STRING` | ADO.NET connection string used by the migration step. Distinct from the App Service Configuration entry because the workflow runs **outside** App Service; the App Service runtime reads its own copy. Use a least-privilege login that has `db_ddladmin` + `db_datareader` + `db_datawriter`. |

### Step 6 — GitHub Environment protection

Repo Settings → **Environments → New environment → `production`**.

- **Required reviewers**: list of accounts allowed to approve deploys (at minimum, your own account).
- **Deployment branches**: restrict to `refs/tags/v*.*.*` so only release tags can target this environment.

The workflow file declares `environment: production` so these gates apply automatically.

## Bootstrap-token mint

Before a new client app (e.g. `button-panel-tester`) can hit `/register`, an operator mints a bootstrap token. Contract: [`specs/001-bootstrap-registration/contracts/admin-bootstrap-tokens.md`](../specs/001-bootstrap-registration/contracts/admin-bootstrap-tokens.md).

```bash
ADMIN_KEY="<value of AdminApiKeys__0 from App Service Configuration>"
API="https://app-dictionaries-manager-prod.azurewebsites.net"

curl -X POST "$API/api/admin/bootstrap-tokens" \
  -H "X-Api-Key: $ADMIN_KEY" \
  -H "Content-Type: application/json" \
  -d '{ "clientApp": "ButtonPanelTester", "ttlHours": 24 }'
```

The response contains `plaintext` once — copy it out of the response and hand it to the consumer. There is no recovery path if you lose the plaintext; mint a new one and let the old one expire.

`ttlHours` accepts `1..2160` (90 days max). Omit it to use the server default. The endpoint is idempotent at the audit level (a transaction wraps the mint + audit entry).

## Manual deploy / re-deploy

Use `workflow_dispatch` to deploy a specific tag without re-tagging:

```bash
gh workflow run "Deploy API" -f tag=v0.7.0
gh run watch
```

Or run the same from the Actions tab in the GitHub UI.

## Rollback

App Service does **not** roll back the schema automatically. Rollback is a two-step manual procedure.

### App code

App Service → Deployment Center → **Deployment slots** (if slot-deploy is enabled later) → swap back. Without slot-deploy:

```bash
# Re-run the deploy workflow against the previous tag.
gh workflow run "Deploy API" -f tag=v0.6.0
```

The migration step is idempotent — re-running an older script against an already-migrated database is a no-op (the `__EFMigrationsHistory` table guards each `IF NOT EXISTS` block).

### Schema

For schema rollback, download the **previous** release's `migrations-<tag>` artifact from the corresponding Actions run, write the **inverse** by hand, and apply it via `sqlcmd` or `Invoke-Sqlcmd`. EF Core does not generate downgrade scripts automatically when migrations are committed cumulatively. In practice:

- Most prod issues are app-level; redeploy the previous tag and the old schema continues to work because the migration script is forward-only.
- For genuinely incompatible schema changes (column drops, type changes), restore the database from the most recent Azure SQL backup. Use the [Azure SQL portal point-in-time-restore](https://learn.microsoft.com/azure/azure-sql/database/recovery-using-backups) procedure.

Plan migrations with this in mind — additive changes first, destructive changes only after the app code that needs them has been live for at least one release cycle.

## First dry-run

After provisioning federated identity and secrets (sections above), run the deploy chain end-to-end before the first production tag:

1. **Branch and tag.** From a feature branch:

   ```bash
   git checkout -b chore/deploy-dry-run main
   git tag v0.7.1-rc.1
   git push github chore/deploy-dry-run
   git push github v0.7.1-rc.1
   ```

2. **Approve the deployment.** GitHub Actions queues the workflow against the `production` environment. The required-reviewers gate fires; approve it from the Actions UI.

3. **Watch the run.**

   ```bash
   gh run watch
   ```

   Or `gh run view <id> --log` to scroll the live log.

4. **Confirm each step.**

   - `Generate idempotent migration script` produces `migrations.sql` (downloadable as the `migrations-v0.7.1-rc.1` artifact).
   - `Apply migrations to Azure SQL` exits 0 (idempotent — running it twice in a row should be a no-op).
   - `Deploy to App Service` reports a successful zip-deploy.
   - `Post-deploy smoke` (added in #57 PR 4) GETs `/health` (expects 200, status `Healthy`) and `/api/version` (expects the tag in the `version` field).

5. **Clean up.** Delete the dry-run tag:

   ```bash
   git push github :refs/tags/v0.7.1-rc.1
   git tag -d v0.7.1-rc.1
   ```

   The deployed binary stays in prod until the next deploy overwrites it — that's fine, it's the same code as the previous real tag plus whatever's on the dry-run branch. If the dry-run code is genuinely throwaway, immediately redeploy the previous real tag (see [Rollback](#rollback)).

A real-tag run after a clean dry-run can be done with confidence. Until the dry-run succeeds, **do not push a production tag** — the workflow will deploy whatever it can build and there is no automated rollback.

## Troubleshooting

- **`Azure login` step fails with `AADSTS70021: No matching federated identity record found`** — the federated credential's environment name or repository must match exactly. Re-check Step 2.
- **`Apply migrations to Azure SQL` fails with `Login failed for user`** — the `AZURE_SQL_CONNECTION_STRING` secret either has wrong credentials or the GitHub runner's outbound IP is blocked by the SQL server firewall. Add the GitHub-hosted runner IP ranges to the firewall, or move the migration step into a self-hosted runner inside the VNet, or switch to AAD auth with the federated identity.
- **`Deploy to App Service` succeeds but the smoke check fails on `/api/version`** — the deployed binary likely wasn't stamped with `InformationalVersion`. Confirm the `Build API` step's `-p:InformationalVersion=...` matches what `/api/version` returns. The smoke check compares the tag (minus the leading `v`) to the `version` field.
- **`/health` returns `Unhealthy`** — the database check (`AddDbContextCheck<AppDbContext>`) failed. The App Service can't reach SQL — usually a firewall or connection-string typo. The migration step would normally surface this first, so this typically only fires after a schema change that broke something at runtime.
