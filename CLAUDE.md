# CLAUDE.md — DictionariesManager

**Archetype:** A
**Standard version:** v1.18.1

This repo follows the STEM standards documented in [`docs/Standards/`](./docs/Standards/) (inline copies pinned to the version above). Upstream source of truth lives in [`standards`](https://github.com/luca-veronelli-stem/standards/tree/v1.18.1/shared/standards).

## Repo-specific notes

<!--
Anything that's not in the standards but is load-bearing for this repo.
Examples: vendor SDK that requires a specific runtime, hardware quirk,
non-default port for a development service, security exception.
-->

### Workflow deviations from the reusable standards templates

Two of the three standards workflows keep a customized inline body instead of
the v1.18.1 reusable caller stub, because the reusable bodies assume project
conventions this repo has not adopted yet. The deviations track the reusable
behaviour by hand (self-extract exe, Bitbucket Downloads, action pins) and are
revisited when the underlying convention lands.

- **`.github/workflows/release.yml`** — the reusable `release-archetype-a.yml`
  publishes `src/<App>.GUI` (= `src/DictionariesManager.GUI`), but this repo's
  desktop GUI is the legacy WPF project `src/GUI.Windows` (the Avalonia
  `<App>.GUI` rename is Phase 5). The reusable exposes no GUI-project-path
  input, so the caller stub would republish a non-existent project and break the
  release (regression of #61). Resolve by adopting the stub once Phase 5 renames
  `GUI.Windows` -> `DictionariesManager.GUI`.

## Language choices that deviate from defaults

<!--
Per LANGUAGE standard: each project that uses a non-default language
records a one-sentence justification here.

Examples:
- `<App>.GUI.Windows`: C# — wraps a vendor's Win32 SDK whose generated
  bindings are C#-only.
- `<App>.LegacyImporter`: C# — predates the F# migration; planned for
  archetype migration phase 3.
-->

- _none yet_

## Active migrations

- [x] Phase 1: structural adoption of the STEM standards — restructure to `src/` + `tests/`, CPM, GitHub Actions CI, inline standards under `docs/Standards/`, in-tree ISSUES files migrated to GitHub Issues (#2–#18). Standard version bumped to v1.18.1 in #129 (docs/config reconciliation + reusable-workflow caller stubs).
- [ ] Phase 2: Italian → English translation pass for in-tree XML doc comments, inline comments, and XAML UI strings (≈125 files across `src/` and `tests/`). Pre-v1 component READMEs were dropped in #32; the residual translation work is tracked in #33 with a per-layer PR breakdown.
- [ ] Phase 3: F# migration of `Core` (and any cross-cutting domain logic that lands in F# alongside).
- [ ] Phase 4: F# migration of `Services`.
- [ ] Phase 5: Avalonia + FuncUI migration of `GUI.Windows` → `GUI` (cross-platform, retiring WPF).

<!-- SPECKIT START -->
Active feature plan: [`specs/001-bootstrap-registration/plan.md`](./specs/001-bootstrap-registration/plan.md)
(spec, clarifications, research, data model, contracts, quickstart all in
the same directory). Read the plan first when working anywhere under
`src/{Core,Services,Infrastructure,API}/Auth/` or
`tests/Tests/{Unit,Integration}/**/Auth/`.
<!-- SPECKIT END -->
