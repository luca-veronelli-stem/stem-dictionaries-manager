# Implementation Plan: Bootstrap registration for per-installation API credentials

**Branch**: `001-bootstrap-registration` | **Date**: 2026-05-07 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `specs/001-bootstrap-registration/spec.md`

**Note**: This file is filled in by the `/speckit-plan` command. See `.specify/templates/plan-template.md` for the execution workflow.

## Summary

Issue #1 of `stem-dictionaries-manager`: replace the embed-an-API-key-in-
each-client-installer pattern with a single-use, time-bounded bootstrap
token that the client trades on first launch (`POST /register`) for a
per-installation API credential. Server validates the token, creates a
per-`(client app, OS user, machine)` Installation, and returns a fresh
long-lived API credential exactly once. Per-installation credentials are
independently revocable. Existing legacy `appsettings.json` `ApiKeys`
keys remain valid in parallel (union mode, non-breaking).

Technical approach (full rationale in [`research.md`](./research.md)):

- Admin surface: HTTP under `/api/admin/*`, gated by a new
  `AdminApiKeys` config section.
- Audit split: pre-auth `POST /register` events go in a new
  `RegistrationEvents` table; admin mutations go in the existing
  `AuditEntry` table (enum extended with `BootstrapToken` and
  `Installation`).
- At-rest hashing: PBKDF2-HMAC-SHA256, 600 000 iterations, per-secret
  16-byte salt, self-describing string format. Same scheme for
  bootstrap tokens and installation credentials.
- Validation cache: `IMemoryCache`, 5 s TTL, explicit invalidation on
  revoke (satisfies SC-004 ≤ 5 s revocation latency).
- Module placement: under existing layers using `Auth/` subfolders, no
  new project. Future extraction to `Stem.Auth.Bootstrap` follows the
  folder seams.

## Technical Context

**Language/Version**: C# 13 on .NET 10 (`net10.0`); `<Nullable>enable</Nullable>` per BUILD_CONFIG.
**Primary Dependencies**: ASP.NET Core minimal APIs (existing in `src/API`); EF Core 10 (Sqlite + SqlServer providers, existing in `src/Infrastructure`); `Microsoft.Extensions.Caching.Memory` (NEW package — for R4); `System.Security.Cryptography` (BCL, no package — for R3, R5).
**Storage**: existing `AppDbContext` (SQLite for dev/test via `DatabaseProvider=Sqlite`, SQL Server in prod). Four new tables: `BootstrapTokens`, `Installations`, `InstallationApiCredentials`, `RegistrationEvents`. Single migration `AddBootstrapRegistration` per `data-model.md`.
**Testing**: xUnit in `tests/Tests/Tests.csproj`, single project per TESTING standard. New test surfaces:
- `tests/Tests/Integration/API/Auth/RegisterEndpointTests.cs`
- `tests/Tests/Integration/API/Auth/AdminBootstrapTokenEndpointTests.cs`
- `tests/Tests/Integration/API/Auth/AdminInstallationEndpointTests.cs`
- `tests/Tests/Unit/Services/Auth/PasswordHasherTests.cs` (PBKDF2 round-trip + format)
- `tests/Tests/Unit/Services/Auth/BootstrapTokenStateMachineTests.cs` (transition matrix)
- `tests/Tests/Unit/Services/Auth/InstallationCredentialValidatorTests.cs` (cache TTL + explicit invalidation)
Per Principle III: manual fakes only, no Moq/NSubstitute. Integration tests use SQLite-in-memory via existing `ApiIntegrationTestBase`.
**Target Platform**: ASP.NET Core API server, cross-platform (`net10.0` only — no `net10.0-windows` leg). Client-side DPAPI usage is out of scope for this server-side feature; covered in `stem-device-manager#94`.
**Project Type**: Web service (existing `src/API` project).
**Performance Goals**: SC-001 (registration end-to-end < 5 s under normal network), SC-004 (revoke effective within 5 s), SC-005 (100% of attempts in audit within 2 s). Cached credential validation in the sub-millisecond range steady-state.
**Constraints**: must not regress legacy `ApiKeys` auth (FR-005, union mode); must not store any plaintext secret server-side (FR-004/FR-014/SC-007); response body byte-identical across all `POST /register` failure modes (FR-002/SC-002).
**Scale/Scope**: STEM-managed API server, single-tenant (R4). Installations run on consumer-owned hosts: a mix of STEM-internal services and **external-supplier sites** (e.g. `stem-button-panel-tester` is installed on supplier machines to test STEM's button panels). Order of tens of installations per client app family across STEM. The cross-org deployment is the threat-model rationale for why `OsUserId`/`MachineId` are server-opaque (FR-001) and why the consumer-side privacy choice on those fields is left to the consumer (Clarifications Session 2 Q4).

## Constitution Check

Gates evaluated against `.specify/memory/constitution.md` v1.0.1.

| Principle / Section | Compliance |
|---|---|
| **I. Spec-Driven Development** | ✅ Feature is on the speckit pipeline: `specify` → `clarify` (3 markers resolved) → `plan` (this file). Branch `001-bootstrap-registration` matches the speckit prefix. `spec.md` describes WHAT/WHY only — `HOW` lives in this plan and the contracts. |
| **II. STEM v1 Standards Are the Contract** | ✅ Module placement (R6) consults REPO_STRUCTURE; layers stay onion-shaped. No edits to `docs/Standards/*.md`. New code respects MODULE_SEPARATION (composition root in `API/Program.cs` only; no service-locator). PORTABILITY: no banned-API introductions in `Core`/`Services`. |
| **III. Test-First, Manual Fakes, Integration over Mocks** | ✅ Test surfaces enumerated above (Technical Context > Testing). Tests written before each layer per `tasks.md` (next phase). Integration tests use the existing SQLite-in-memory `ApiIntegrationTestBase`. **TODO(LEAN_WORKSPACE)**: this feature has the canonical state machines (`BootstrapToken: Issued → Used | Expired | Revoked`, `Installation: Active → Revoked`) for the future Lean preservation theorems. The constitution flags the Lean workspace as not-yet-bootstrapped; this plan does NOT bootstrap it (out of scope) but **does** structure xUnit tests to mirror the eventual preservation shape (one test per transition, each checks the post-state invariants the Lean theorem will encode). See `data-model.md` § "Cross-cutting invariants". |
| **IV. Pragmatic .NET — Explicit, Nullable, Exceptional** | ✅ `Nullable=enable` on every new file (inherited from `Directory.Build.props`). Manual DI in `API/Program.cs`. Exceptions, not `null`/`Result<T>`. Constructor validation via `ArgumentException.ThrowIfNullOrWhiteSpace`. C# (not F#) per the repo's pre-Phase-3 archetype. Function bodies ≤ 15 LOC where reasonable; cryptographic helpers may exceed (justified inline). |
| **V. Workflow Discipline** | ✅ All work flows through `001-bootstrap-registration` → PR on GitHub. Conventional commits, no AI attribution. Mirror to Bitbucket via existing `.github/workflows/mirror-bitbucket.yml`. CI (build + Tests on `ubuntu-latest` + `windows-latest`) MUST pass before merge. CHANGELOG.md updated under `[Unreleased]` with each PR. Rebase merge preferred. |
| **Security & Auditability** | ✅ Per-installation credential model implemented (issue #1's whole point). Bootstrap tokens: single-use (FR-007/FR-008), time-bounded (FR-007), client-scoped (FR-008). Credentials: independently revocable (FR-006). Hashed at rest (R3). Failure paths return identical `401 { "error": "registration failed" }` body (FR-002/SC-002). Audit on every state mutation: `POST /register` → `RegistrationEvent`; admin mint/revoke → `AuditEntry`. Banned APIs: nothing in `Core`/`Services` references `System.Drawing`/`Win32.Registry`/`System.Management`/`System.IO.Ports`/hardcoded paths. |
| **Quality Gates** | Will be verified pre-PR per the `pr` skill: `dotnet format whitespace --verify-no-changes --no-restore`; `dotnet build -c Release` (warnings-as-errors); `dotnet test -c Release` on both OS legs. CHANGELOG.md entry under `[Unreleased]`. Standard version unchanged in this PR. |

**Result**: ✅ All gates pass. No deviations recorded; Complexity Tracking section is empty.

## Project Structure

### Documentation (this feature)

```text
specs/001-bootstrap-registration/
├── plan.md                                # This file (/speckit-plan output)
├── spec.md                                # /speckit-specify + /speckit-clarify output
├── research.md                            # Phase 0 — design decisions
├── data-model.md                          # Phase 1 — entities, state machines, migration
├── quickstart.md                          # Phase 1 — manual smoke walkthrough
├── checklists/
│   └── requirements.md                    # /speckit-specify quality checklist
├── contracts/
│   ├── register.md                        # POST /register
│   ├── admin-bootstrap-tokens.md          # POST /api/admin/bootstrap-tokens
│   └── admin-installations.md             # GET + revoke /api/admin/installations
└── tasks.md                               # /speckit-tasks output (NOT created here)
```

### Source code (new files only — see `data-model.md` for full layout rationale)

```text
src/
├── Core/
│   ├── Enums/
│   │   └── Auth/
│   │       ├── BootstrapTokenStatus.cs
│   │       ├── InstallationStatus.cs
│   │       └── RegistrationOutcome.cs
│   └── Models/
│       └── Auth/
│           ├── BootstrapToken.cs
│           ├── Installation.cs
│           ├── InstallationApiCredential.cs
│           ├── InstallationDescriptor.cs
│           └── RegistrationEvent.cs
├── Services/
│   ├── Auth/
│   │   ├── BootstrapTokenService.cs               # Mint, lookup-by-hash
│   │   ├── RegistrationService.cs                 # The /register orchestration
│   │   ├── InstallationCredentialService.cs       # Issue, revoke, list
│   │   ├── InstallationCredentialValidator.cs     # Hot-path validate (with R4 cache)
│   │   ├── PasswordHasher.cs                      # PBKDF2 wrapper (R3)
│   │   └── TokenGenerator.cs                      # CSPRNG + base64url + prefix (R5)
│   └── Interfaces/
│       └── Auth/
│           ├── IBootstrapTokenService.cs
│           ├── IRegistrationService.cs
│           ├── IInstallationCredentialService.cs
│           └── IInstallationCredentialValidator.cs
├── Infrastructure/
│   ├── Entities/
│   │   └── Auth/
│   │       ├── BootstrapTokenEntity.cs
│   │       ├── InstallationEntity.cs
│   │       ├── InstallationApiCredentialEntity.cs
│   │       └── RegistrationEventEntity.cs
│   ├── Repositories/
│   │   └── Auth/
│   │       ├── BootstrapTokenRepository.cs
│   │       ├── InstallationRepository.cs
│   │       ├── InstallationApiCredentialRepository.cs
│   │       └── RegistrationEventRepository.cs
│   ├── Interfaces/
│   │   └── Auth/
│   │       └── (corresponding I*Repository.cs files)
│   └── Migrations/
│       └── 2026MMDDHHMMSS_AddBootstrapRegistration.cs    # generated by EF
└── API/
    ├── Dtos/
    │   └── Auth/
    │       ├── InstallationDescriptorDto.cs
    │       ├── RegisterRequestDto.cs
    │       ├── RegisterResponseDto.cs
    │       ├── MintBootstrapTokenRequestDto.cs
    │       ├── MintBootstrapTokenResponseDto.cs
    │       ├── InstallationListItemDto.cs
    │       └── RevokeInstallationResponseDto.cs
    ├── Endpoints/
    │   └── Auth/
    │       ├── RegistrationEndpoints.cs           # POST /register
    │       └── AdminAuthEndpoints.cs              # /api/admin/{bootstrap-tokens, installations, …}
    └── Middleware/
        ├── ApiKeyMiddleware.cs                    # MODIFIED: union (legacy + DB-issued)
        └── AdminAuthenticationMiddleware.cs       # NEW: sets ICurrentUserProvider for admin keys

tests/Tests/
├── Integration/
│   └── API/
│       └── Auth/
│           ├── RegisterEndpointTests.cs
│           ├── AdminBootstrapTokenEndpointTests.cs
│           └── AdminInstallationEndpointTests.cs
└── Unit/
    └── Services/
        └── Auth/
            ├── PasswordHasherTests.cs
            ├── TokenGeneratorTests.cs
            ├── BootstrapTokenStateMachineTests.cs
            └── InstallationCredentialValidatorTests.cs
```

**Structure Decision**: Existing archetype-A layout (`Core` / `Services` /
`Infrastructure` / `API` / `GUI.Windows`). Add a sibling `Auth/`
subfolder inside each affected layer. This keeps the onion shape, names
the future-extraction seam cleanly (`Stem.Auth.Bootstrap` lifts each
`*/Auth/*` folder), and matches REPO_STRUCTURE's "what lives where"
cheat-sheet (domain in `Core`, use cases in `Services`, EF in
`Infrastructure`). No new project. Full rationale in `research.md` § R6.

## Complexity Tracking

> Constitution Check passed with no violations. This section is intentionally empty.
