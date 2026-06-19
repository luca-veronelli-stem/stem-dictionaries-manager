# Persistence: provider policy

DictionariesManager runs on two EF Core providers, with a fixed split:

| Provider | Schema strategy | Why |
| --- | --- | --- |
| **SQL Server** (production / Azure SQL) | **always `Migrate`** | Versioned, reviewable migrations under `src/Infrastructure/Migrations/`. |
| **SQLite** (local dev / CI) | **always `EnsureCreated`** | The migrations are SQL Server-only (they contain `nvarchar(max)` and other T-SQL that SQLite cannot parse — `SQLite Error 1: 'near "max": syntax error'`). `EnsureCreated` builds the schema straight from the model instead. |

This is wired in `GUI.Windows/App.xaml.cs` (`OnStartup`).

## Seed data must use `HasData`, not migration `InsertData`

Because SQLite never runs migrations, **seed rows inserted via
`migrationBuilder.InsertData(...)` never reach a SQLite database** —
`EnsureCreated` honors only `HasData(...)` declared in
`AppDbContext.OnModelCreating`.

So all baseline seed rows live in `HasData`. The reference case is the
`system-admin` user (data-model.md "Audit split"): admin API-key callers have
no organic per-request user, so `AdminAuthenticationMiddleware` attributes
audit entries to this row. It is seeded via `HasData` (Id `1`) so it is present
on **both** providers.

### Reconciling `HasData` with an already-applied SQL Server migration

The `system-admin` row was originally inserted by
`20260507131528_AddBootstrapRegistration` via `InsertData` (identity Id `1`).
Moving the seed into `HasData` makes EF want a fresh migration that re-inserts
it — which would violate the primary key on any SQL Server database that
already ran the original migration. The reconcile migration
(`20260619094158_SeedSystemAdminUser`) is therefore an **intentional no-op**:
its only purpose is to bring the model snapshot in sync with `HasData`. The
`HasData` Id matches the existing row, so the SQL Server `Migrate` path stays
consistent and never double-inserts.
