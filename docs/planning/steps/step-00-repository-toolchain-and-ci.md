# Step 00 — Repository, Toolchain, and CI

**Phase:** A. Foundations  
**Depends on:** —  
**Delivers:** A reproducible monorepo skeleton with .NET 10, Rust, CI on three operating systems, and the
decision records that constrain later steps.

## Goal

Create the repository, build infrastructure, and quality gates before scientific code is written. A new
contributor should be able to clone, build, and run the test suites on Windows, Linux, and macOS with one
documented command per stack.

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
├── docs/
│   ├── planning/
│   ├── user-guide/
│   └── reference/
├── src/dotnet/
├── src/rust/
├── samples/
├── tests/
└── tools/
```

## Implementation tasks

### Repository

- [ ] Create the directory structure and placeholder projects/crates.
- [ ] Write the MIT license to `LICENSE`; add `NOTICE.md` with original authorship, the GPL/MIT boundary, and citation guidance.
- [ ] Add `.gitattributes` for LF normalization, binary formats, and generated files.
- [ ] Add `.gitignore` for .NET, Rust, IDEs, build outputs, and simulation artifacts.
- [ ] Add `README.md` with prerequisites, build/test commands, and a link to the plan.

### .NET

- [ ] Pin the SDK in `global.json` (`10.0.x`, `rollForward: latestFeature`).
- [ ] Create `Directory.Build.props`:
  - `TargetFramework=net10.0`, `LangVersion=latest`, `Nullable=enable`, `ImplicitUsings=enable`;
  - `TreatWarningsAsErrors=true`, `EnableNETAnalyzers=true`, `AnalysisLevel=latest-recommended`;
  - `Deterministic=true`, `ContinuousIntegrationBuild` when `CI=true`;
  - `GenerateDocumentationFile=true` for libraries.
- [ ] Create `Directory.Packages.props` with central versions for Avalonia, CommunityToolkit.Mvvm,
  Microsoft.Extensions.*, Microsoft.Data.Sqlite, MessagePack, ZstdSharp, xUnit, FsCheck, FluentAssertions,
  LiveCharts2, Silk.NET, Serilog.
- [ ] Create the solution with empty projects:
  `Mocassin.Core`, `Mocassin.Symmetry`, `Mocassin.Compilation`, `Mocassin.Packages`, `Mocassin.Results`,
  `Mocassin.Cli`, `Mocassin.App`, `Mocassin.App.ViewModels`, `Mocassin.App.Views`, `Mocassin.Rendering`.
- [ ] Add test projects: `Mocassin.Core.Tests`, `Mocassin.Symmetry.Tests`, `Mocassin.Compilation.Tests`,
  `Mocassin.Packages.Tests`, `Mocassin.Results.Tests`, `Mocassin.App.Tests`.
- [ ] Add a smoke test in each test project so `dotnet test` is meaningful from day one.
- [ ] Add `dotnet format` and a `.editorconfig` with C# style, naming, and analyzer severities.

### Rust

- [ ] Create a workspace `src/rust/Cargo.toml` with resolver 2 and shared `[workspace.package]` metadata.
- [ ] Create crates: `mocassin-types`, `mocassin-msl`, `mocassin-rng`, `mocassin-solver`, `mocassin-state`,
  `mocassin-extension`, `mocassin-cli`.
- [ ] Add workspace dependencies: `serde`, `rmp-serde`, `zstd`, `rusqlite` (bundled), `clap`, `thiserror`,
  `tracing`, `tracing-subscriber`, `rand`, `rand_chacha`, `blake3`, `crc32fast`, `bytemuck`, `memmap2`,
  `rayon` (where justified), `proptest`, `criterion`, `insta`.
- [ ] Add `rustfmt.toml` and `clippy.toml`; deny warnings in CI.
- [ ] Add a smoke test and a `--version` CLI stub.
- [ ] Add `cargo deny` (or `cargo audit`) configuration for licence and vulnerability checks.

### CI

- [ ] GitHub Actions workflow with a matrix: `ubuntu-latest`, `windows-latest`, `macos-latest`.
- [ ] Steps: checkout, setup .NET 10, setup Rust stable, cache NuGet/Cargo/SQLite, restore, build,
  `dotnet test`, `cargo test`, `dotnet format --verify-no-changes`, `cargo fmt --check`, `cargo clippy`.
- [ ] Upload test results and coverage artifacts.
- [ ] Add a scheduled nightly workflow for longer tests and fuzzing (can start as a placeholder).
- [ ] Add a dependency/licence check and a "no secrets" check.

### Fixtures and documentation

- [ ] Create `tests/contracts/` and `tests/reference/` with README placeholders.
- [ ] Add `samples/blank/` and `samples/ceria/` directories with README placeholders.
- [ ] Add the seven ADRs from `docs/planning/adr/`.
- [ ] Add a `CONTRIBUTING.md` with build, test, style, and review expectations.
- [ ] Add issue/PR templates that ask for the affected step and parity-matrix item.

## Acceptance criteria

- [ ] `dotnet build -c Release` succeeds from a clean clone on all three OSes.
- [ ] `dotnet test` runs and passes the smoke tests.
- [ ] `cargo build --workspace` and `cargo test --workspace` succeed on all three OSes.
- [ ] `dotnet format --verify-no-changes`, `cargo fmt --check`, and `cargo clippy -- -D warnings` pass.
- [ ] CI is green on the initial commit and on a pull request.
- [ ] `README.md` describes prerequisites and the exact build/test commands.
- [ ] The repository contains no copied original source files.

## Tests

| Test | Assertion |
| --- | --- |
| .NET smoke | A trivial library test passes in each test project |
| Rust smoke | A workspace test and CLI `--version` pass |
| CI matrix | All three OS jobs complete |
| Licence check | `LICENSE` is MIT and `NOTICE.md` records the original GPL project and attribution |
| Format check | Both formatters report no changes |

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| .NET 10 SDK availability on CI images | Use `actions/setup-dotnet` with an explicit version; pin in `global.json` |
| Rust SQLite build time | Enable Cargo cache; use `rusqlite` bundled only where needed |
| macOS OpenGL deprecation | Not relevant yet; tracked in ADR 0006 |
| Over-configuring before code exists | Keep CI minimal but enforcing; expand in later steps |

## Done when

The repository builds and tests on Windows, Linux, and macOS, the ADRs are present, and the first pull request
passes all quality gates.
