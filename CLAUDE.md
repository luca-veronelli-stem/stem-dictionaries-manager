# CLAUDE.md — DictionariesManager

**Archetype:** A
**Standard version:** v1.3.2

This repo follows the STEM v1 standards documented in [`docs/Standards/`](./docs/Standards/) (inline copies pinned to the version above). Upstream source of truth lives in [`llm-settings`](https://github.com/luca-veronelli-stem/llm-settings/tree/v1.3.2/shared/standards) (private).

## Repo-specific notes

<!--
Anything that's not in the standards but is load-bearing for this repo.
Examples: vendor SDK that requires a specific runtime, hardware quirk,
non-default port for a development service, security exception.
-->

- _none yet_

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

- [x] Phase 1: structural adoption of `llm-settings v1.3.2` — restructure to `src/` + `tests/`, CPM, GitHub Actions CI, inline standards under `docs/Standards/`, in-tree ISSUES files migrated to GitHub Issues (#2–#18).
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
