# Step 00 — Repository, Toolchain, and CI

**Phase:** A. Foundations  
**Depends on:** —
**Delivers:** A reproducible monorepo skeleton with .NET 10, Rust, CI on three operating systems, the
decision records that constrain later steps, and the initial standalone documentation site skeleton.

## Implementation status

- **Status:** In progress
- **Step branch:** `step/00-foundation`
- **Step CI:** —
- **Main merge:** —
- **Step tag:** —
- **Last updated:** 2026-11-03

| Feature | Branch | Status | Local gates | Step CI | Squash commit |
| --- | --- | --- | --- | --- | --- |
| `repo-skeleton` | `feature/step-00-foundation/repo-skeleton` | Squashed | Structure/link/YAML checks; no build gates yet | No workflow yet | `0062d54` |
| `docs-site` | `feature/step-00-foundation/docs-site` | Squashed | `zensical build --strict` passes; planning excluded; tool restore passes | No workflow yet | `5fe29a7` |
| `dotnet-solution` | `feature/step-00-foundation/dotnet-solution` | Squashed | restore/build/test/format pass; 7 smoke tests | No workflow yet | `00f3940` |
| `rust-workspace` | `feature/step-00-foundation/rust-workspace` | Squashed | fmt/clippy/test/build/deny pass; `--version` verified | No workflow yet | `0569a27` |
| `python-skeleton` | `feature/step-00-foundation/python-skeleton` | Squashed | pytest 3/3; wheel/sdist build; wheel import checked | No workflow yet | `2784462` |
| `fix-docs-api-links` | `feature/step-00-foundation/fix-docs-api-links` | Squashed | strict docs build; direct API links; redirects verified over HTTP | No workflow yet | `e420a9f` |
| `fix-spdx-headers` | `feature/step-00-foundation/fix-spdx-headers` | Awaiting approval | header check 27/27; .NET and Rust gates re-run | — | — |
| `contracts-manifest` | `feature/step-00-foundation/contracts-manifest` | Planned | — | — | — |
| `ci-workflows` | `feature/step-00-foundation/ci-workflows` | Planned | — | — | — |
| `closeout` | `feature/step-00-foundation/closeout` | Planned | — | — | — |

**Deviations:**

- Feature branches use the `feature/step-<NN>-<slug>/<feature>` prefix. Git cannot store both a step branch
  (`step/00-foundation`) and a nested feature branch (`step/00-foundation/<feature>`) because of the
  directory/file ref conflict. `AGENTS.md` and the agent workflow were corrected before implementation.
- The published API navigation links directly to the first content page of each generated reference
  (`.NET`: `Mocassin.Core.CoreModule`; `Rust`: `mocassin-types`). `/api/dotnet/` and `/api/rust/` are
  generated redirects for directory URLs and tool-internal links. Zensical instant navigation is disabled so
  generated API pages load as normal full-page navigations.

## Goal

Create the repository, build infrastructure, documentation skeleton, and quality gates before scientific code
is written. A new contributor should be able to clone, build, test, and serve the docs on Windows, Linux, and
macOS with one documented command per stack.

The planning documents, ADRs, licence, and contribution rules already exist in the repository. This step
creates the missing build/code/docs skeleton and keeps those files in sync; do not recreate or replace them.

The local development image is [`docs/opencode/mocassin.containerfile`](../../opencode/mocassin.containerfile).
It depends only on the `opencode:base` image from
[`seb-eis/opencode-container`](https://github.com/seb-eis/opencode-container) and does not modify that
repository. The same toolchain can be installed directly for development without opencode.

## Original reference

- Original repository: `seb-eis/mocassin`, archived 2022-12-13.
- Original build: Visual Studio solution for the WPF GUI, `dotnet` for netstandard2.0 libraries, CMake/GCC
  for the C solver, PowerShell scripts for release packaging.
- Original test posture: no solver tests, no symmetry tests, no CI configuration in the archive.

The new repository is a fresh history. The original is kept outside the repo as a read-only reference during
planning and validation.

## Design

### Target layout

```text
mocassin/
├── .github/workflows/ci.yml
├── .editorconfig
├── .gitattributes
├── .gitignore
├── Directory.Build.props
├── Directory.Packages.props
├── global.json
├── Mocassin.slnx                 # or Mocassin.sln
├── README.md
├── LICENSE
├── NOTICE.md
├── zensical.toml                # docs_dir = "docs", site_dir = "site", exclude_docs = "planning/"
├── docs/
│   ├── index.md
│   ├── getting-started/
│   ├── user-guide/
│   ├── physics/
│   ├── tutorials/
│   ├── reference/
│   ├── development/
│   ├── performance/
│   ├── troubleshooting/
│   ├── migration/
│   ├── examples/
│   ├── glossary/
│   ├── api/
│   │   └── index.md
│   └── planning/                # internal, excluded from the site
├── contracts/
│   ├── manifest.json            # format fields/types/versions
│   └── generated/
├── src/dotnet/
├── src/rust/
├── src/python/
├── samples/
├── tests/
└── tools/
```

## Implementation tasks

### Repository

- [x] Create the missing directory structure and placeholder projects/crates (`src/`, `samples/`, `tests/`,
  `tools/`, `contracts/`).
- [x] Verify the existing `LICENSE` is MIT and `NOTICE.md` records the original authorship, the GPL/MIT
  boundary, and citation guidance.
- [x] Verify/update the existing `.gitattributes` for LF normalization, binary formats, and generated files.
- [x] Update the existing `.gitignore` for .NET, Rust, IDEs, build outputs, simulation artifacts, and the
  contract-fixture exceptions.
- [x] Update `README.md` with prerequisites, exact build/test/docs commands, and a link to the plan.

### .NET

- [x] Pin the SDK in `global.json` (`10.0.x`, `rollForward: latestFeature`).
- [x] Create `Directory.Build.props`:
  - `TargetFramework=net10.0`, `LangVersion=latest`, `Nullable=enable`, `ImplicitUsings=enable`;
  - `TreatWarningsAsErrors=true`, `EnableNETAnalyzers=true`, `AnalysisLevel=latest-recommended`;
  - `Deterministic=true`, `ContinuousIntegrationBuild` when `CI=true`;
  - `GenerateDocumentationFile=true` for libraries.
- [x] Create `Directory.Packages.props` with central versions for Avalonia, CommunityToolkit.Mvvm,
  Microsoft.Extensions.*, Microsoft.Data.Sqlite, MessagePack, ZstdSharp,
  xUnit, FsCheck, FluentAssertions, System.Numerics.Tensors, Microsoft.CodeAnalysis.PublicApiAnalyzers,
  LiveCharts2, Silk.NET, Serilog.
- [x] Create the solution with empty projects:
  `Mocassin.Core`, `Mocassin.Symmetry`, `Mocassin.Compilation`, `Mocassin.Contracts`,
  `Mocassin.Packages`, `Mocassin.Results`, `Mocassin.Cli`, `Mocassin.App`, `Mocassin.App.ViewModels`,
  `Mocassin.App.Views`, `Mocassin.Rendering`.
- [x] Add test projects: `Mocassin.Core.Tests`, `Mocassin.Symmetry.Tests`, `Mocassin.Compilation.Tests`,
  `Mocassin.Contracts.Tests`, `Mocassin.Packages.Tests`, `Mocassin.Results.Tests`, `Mocassin.App.Tests`.
- [x] Add a smoke test in each test project so `dotnet test` is meaningful from day one.
- [x] Add `dotnet format` and a `.editorconfig` with C# style, naming, and analyzer severities.

### Rust

- [x] Create a workspace `src/rust/Cargo.toml` with resolver 2 and shared `[workspace.package]` metadata.
- [x] Create crates: `mocassin-types`, `mocassin-msl`, `mocassin-rng`, `mocassin-solver`, `mocassin-state`,
  `mocassin-extension`, `mocassin-cli`.
- [x] Add workspace dependencies: `serde`, `serde_bytes`, `rmp-serde`, `zstd`, `rusqlite` (bundled), `clap`, `thiserror`,
  `tracing`, `tracing-subscriber`, `rand`, `rand_chacha`, `sha2`, `crc32fast`, `bytemuck`, `memmap2`,
  `rayon` (where justified), `proptest`, `criterion`, `insta`.
- [x] Add `rustfmt.toml` and `clippy.toml`; clippy runs with `-D warnings` locally and in CI (CI wiring is in
  `ci-workflows`).
- [x] Add a smoke test and a `--version` CLI stub.
- [x] Add `cargo deny` (or `cargo audit`) configuration for licence and vulnerability checks.

### CI

- [ ] GitHub Actions workflow triggered by pushes to `main` and integration branches matching `step/*`, with
  a matrix: `ubuntu-latest`, `windows-latest`, `macos-latest`. Exclude `step/*/*` so feature branches are
  validated locally and do not consume CI runs. Pull-request CI is not enabled by default; add it if PRs are
  introduced later.
- [ ] Steps: checkout, setup .NET 10, setup Rust stable, cache NuGet/Cargo/SQLite, restore, build,
  `dotnet test`, `cargo test`, `dotnet format --verify-no-changes`, `cargo fmt --check`, `cargo clippy`.
- [ ] Upload test results and coverage artifacts.
- [ ] Add the docs job: setup Python, install pinned Zensical requirements, install the Python package in
  editable mode when `src/python/pyproject.toml` exists (from Step 22), run `zensical build --strict`, and
  enable DocFX/rustdoc steps as their projects appear.
- [x] Add a minimal `src/python` placeholder package (`pyproject.toml`, import package, smoke test) so the
  Python CI job is meaningful from day one. Step 22 replaces the placeholder with the real reader.
- [ ] Add the Python job: Python 3.11–3.13, install `src/python` with test extras, run `pytest`, and build the
  wheel/sdist.
- [ ] Add a scheduled nightly workflow for longer tests and fuzzing (can start as a placeholder).
- [ ] Add a dependency/licence check and a "no secrets" check.

### Fixtures and documentation skeleton

- [x] Create `tests/contracts/` and `tests/reference/` with README placeholders.
- [x] Add `samples/blank/` and `samples/ceria/` directories with README placeholders.
- [x] Review the fourteen ADRs already present in `docs/planning/adr/` and keep them in sync; do not recreate
  or renumber them.
- [ ] Create `contracts/manifest.json` and `tools/contracts/` with a generator/validator for project/package/
  results/state/job-folder/event/MMCFE field descriptions, canonical signature projection layouts, JSON Schema
  fragments, fixture descriptors, and version constants.
- [ ] Add a CI step that regenerates contract artifacts and fails on drift.
- [ ] Add public-API baseline files and an `ApiCompat` CI check for published .NET libraries.
- [x] Create the Zensical project skeleton:
  - `zensical.toml` with `docs_dir = "docs"`, `site_dir = "site"`, `exclude_docs = "planning/"`, site
    metadata, repo link, and nav;
  - Mermaid support via Zensical SuperFences custom fences;
  - `docs/index.md` and placeholder pages for every required top-level section:
    getting-started, user-guide, physics, tutorials, reference, development, performance, troubleshooting,
    migration, examples, glossary, and api;
  - `docs/api/index.md` linking to `/api/dotnet/` and `/api/rust/`;
  - placeholder `docs/api/dotnet/index.md`, `docs/api/rust/index.md`, and `docs/api/python/index.md` pages so
    strict link validation passes before the generated API outputs exist. DocFX/rustdoc output is copied over
    the first two in `site/api/...`; Step 22 replaces the Python placeholder with mkdocstrings directives.
- [x] Add `tools/docs/requirements.txt` (or a root `pyproject.toml`) pinning Zensical, Markdown extensions, and
  `mkdocstrings[python]`.
- [x] Add `.config/dotnet-tools.json` with pinned DocFX and `Microsoft.DotNet.ApiCompat.Tool` entries. Rust API
  docs use `cargo doc` directly.
- [x] Add docs build scripts:
  - `tools/docs/build-docs.sh` and `tools/docs/build-docs.ps1`: build Zensical strictly; build DocFX/rustdoc when the
    corresponding projects/crates exist; copy API output into `site/api/...`;
  - `tools/docs/serve.sh` / `tools/docs/serve.ps1`: local preview.
- [ ] Add a `docs` CI job that installs the pinned requirements and runs `zensical build --strict`.
  DocFX/rustdoc steps are enabled as soon as their projects exist; they are not deferred to Step 30.
- [x] Add `docs/CONTRIBUTING.md` or a README section explaining docs-as-code: every feature step updates its
  pages, and the docs build must stay green.
- [x] Add `site/`, `artifacts/docfx/`, and `target/doc/` to `.gitignore`. Keep the placeholder
  `docs/api/*/index.md` pages tracked; generated API output is copied to `site/api/`.
- [x] Add contract-fixture exceptions to `.gitignore` so `tests/contracts/**/*.mcs`, `*.msl`, `*.msr`, logs, and
  `Job*/` fixtures are not excluded by the simulation-artifact patterns.
- [x] Add a `CONTRIBUTING.md` with build, test, style, docs, and review expectations.
- [x] Add issue/PR templates that ask for the affected step, parity-matrix item, and documentation update.

## Acceptance criteria

- [ ] `dotnet build -c Release` succeeds from a clean clone on all three OSes.
- [ ] `dotnet test` runs and passes the smoke tests.
- [ ] `cargo build --workspace` and `cargo test --workspace` succeed on all three OSes.
- [ ] `dotnet format --verify-no-changes`, `cargo fmt --check`, and `cargo clippy -- -D warnings` pass.
- [ ] CI is green on the initial `main` push and on a `step/*` integration-branch push.
- [ ] `README.md` describes prerequisites and the exact build/test/docs commands.
- [ ] `zensical build --strict` succeeds and the skeleton contains every required top-level section.
- [ ] The docs CI job is green and `docs/planning/` is absent from the built site.
- [ ] `contracts/manifest.json` exists, generated checks are current, and the CI drift check passes.
- [ ] The manifest defines the canonical signature-projection layouts and the generator emits the initial
  fixture/vector scaffolding; signed-type vectors are added with the types in Steps 08/09/11.
- [ ] Public-API baseline files and the `ApiCompat` CI check are in place.
- [ ] The repository contains no copied original source files.
- [ ] Documentation written

## Documentation

At this point, document the repository and contributor workflow:

- `README.md` — project purpose, prerequisites, build/test/docs commands.
- `docs/development/contributing.md` — branch rules, step workflow, review gates, docs-as-code.
- `docs/reference/file-layout.md` — repository layout, generated artifacts, `docs/planning` exclusion.
- `docs/development/testing-and-parity.md` — testing trophy, mock/fake policy, fixtures, and tiers, seeded from
  `03-quality-strategy.md`.
- `docs/index.md` and the Zensical nav — the documentation skeleton itself, navigation, and placeholders.
- `docs/api/index.md` — the future API landing page and subpath convention.

## Tests

| Test | Assertion |
| --- | --- |
| .NET smoke | A trivial library test passes in each test project |
| Rust smoke | A workspace test and CLI `--version` pass |
| CI matrix | All three OS jobs complete |
| Licence check | `LICENSE` is MIT and `NOTICE.md` records the original GPL project and attribution |
| Format check | Both formatters report no changes |
| Docs skeleton | Strict Zensical build; required nav sections present; planning excluded |
| Docs CI | Docs job installs pinned requirements and builds strictly |
| Contracts | Manifest generates current schemas/checks; drift check passes |
| Python CI | Python 3.11–3.13 install, pytest, wheel build |

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| .NET 10 SDK availability on CI images | Use `actions/setup-dotnet` with an explicit version; pin in `global.json` |
| Rust SQLite build time | Enable Cargo cache; use `rusqlite` bundled only where needed |
| macOS OpenGL deprecation | Not relevant yet; tracked in ADR 0006 |
| Over-configuring before code exists | Keep CI minimal but enforcing; expand in later steps |

## Done when

The repository builds and tests on Windows, Linux, and macOS, the ADRs and documentation skeleton are present,
the strict docs build is green, and the first pull request passes all quality gates.
