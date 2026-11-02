# 01 — Target Architecture

## 1. Architectural principles

1. **The model is data, not a service graph.** A project is an immutable-ish aggregate that can be
   serialized, validated, diffed, and compiled without a UI or database.
2. **Compilation is an explicit pipeline.** User model → validated model → compiled model → simulation
   package. Each stage has typed inputs/outputs, diagnostics, and cacheable content hashes.
3. **One contract, two languages.** .NET writes the simulation package; Rust reads it. No Rust code reads
   project JSON and no .NET code participates in the Monte Carlo loop.
4. **UI is a consumer.** The Avalonia app uses the same public APIs as the CLI. No scientific logic lives in
   view models.
5. **Data-oriented solver.** The Rust solver uses flat arrays, dense integer IDs, and precomputed tables.
   Object graphs are a build-time concern.
6. **Diagnostics over exceptions.** User errors produce structured diagnostics with object paths; exceptions
   are reserved for programmer errors, I/O failures, and contract violations.
7. **Version everything that crosses a process or language boundary.** Project, package, state, and CLI
   output each carry an explicit schema version.
8. **No legacy compatibility debt.** New formats are designed for the new architecture; legacy files are
   not parsed.

## 2. Repository layout

```text
mocassin/
├── README.md
├── LICENSE
├── NOTICE.md
├── Directory.Build.props            # shared .NET settings (nullable, analyzers, deterministic build)
├── Directory.Packages.props         # central NuGet versions
├── global.json                      # pins .NET SDK 10.x
├── docs/
│   ├── planning/
│   ├── user-guide/
│   └── reference/                   # generated API and format docs
├── src/
│   ├── dotnet/
│   │   ├── Mocassin.sln
│   │   ├── Mocassin.Core/           # domain model, IDs, units, diagnostics, project aggregate
│   │   ├── Mocassin.Symmetry/       # space groups, geometry, coordinate encoding
│   │   ├── Mocassin.Compilation/    # P1 extension, environments, rules, compiled model
│   │   ├── Mocassin.Packages/       # templates, supercells, package encoding, .msl reader/writer
│   │   ├── Mocassin.Results/        # .mcs reader, evaluation API, exporters
│   │   ├── Mocassin.Cli/            # `mocassin` headless CLI
│   │   ├── Mocassin.App/            # Avalonia application shell
│   │   ├── Mocassin.App.ViewModels/
│   │   ├── Mocassin.App.Views/
│   │   ├── Mocassin.Rendering/      # 3D abstraction + Avalonia host
│   │   └── Mocassin.Tests.*/        # unit/integration/property/golden tests
│   └── rust/
│       ├── Cargo.toml               # workspace
│       ├── crates/
│       │   ├── mocassin-types/      # package/state schema types shared with .NET contract
│       │   ├── mocassin-msl/        # SQLite package reader
│       │   ├── mocassin-rng/        # deterministic RNG streams
│       │   ├── mocassin-solver/     # context, energy, KMC, MMC, tracking
│       │   ├── mocassin-state/      # .mcs writer
│       │   ├── mocassin-extension/  # extension ABI and MMCFE-compatible hooks
│       │   └── mocassin-cli/        # `mocassin-sim`
│       └── benches/
├── samples/
│   ├── ceria/
│   ├── barium-zirconate/
│   └── vehicle-demo/
├── tests/
│   ├── contracts/                   # cross-language .msl/.mcs fixtures
│   └── reference/                   # reference observables for statistical parity
└── tools/
    ├── format/                      # schema generation and validation
    └── hpc/                         # SLURM/PBS templates and wrappers
```

## 3. .NET project graph

```text
Mocassin.Core
   ▲
Mocassin.Symmetry
   ▲
Mocassin.Compilation
   ▲
Mocassin.Packages ──────► Mocassin.Results
   ▲                         ▲
Mocassin.Cli ────────────────┘
   ▲
Mocassin.App ──► Mocassin.App.ViewModels ──► Mocassin.App.Views
                     │
                     └──► Mocassin.Rendering
```

Rules:

- `Core` has no dependency on SQLite, Avalonia, or EF Core. Only BCL.
- `Symmetry` may depend on `Core` and a SQLite/JSON data source for space groups.
- `Compilation` depends on `Core` + `Symmetry`; it has no I/O and no UI.
- `Packages` depends on `Compilation`; it owns project serialization, templates, supercells, and `.msl`.
- `Results` depends on `Core` + `Packages` (for model context restoration from package metadata) and owns
  `.mcs` reading and evaluation.
- `Cli` depends on `Packages` + `Results`; it may launch the Rust solver.
- `App` depends on view models only; view models depend on public library APIs, never on views.
- `Rendering` is independent of the project model; it consumes render primitives.

### 3.1 Responsibilities

| Project | Owns | Must not own |
| --- | --- | --- |
| `Mocassin.Core` | IDs, units, model objects, project aggregate, diagnostics, validation contracts, undo/redo primitives | File I/O, SQL, UI, solver data |
| `Mocassin.Symmetry` | Space-group catalog, operations, cell geometry, fractional/Cartesian/4D encoding, orbit and permutation reduction | Model semantics |
| `Mocassin.Compilation` | P1 extension, environment sampling, interaction discovery, transition rule generation, index assignment, compiled model | Job templates, supercells |
| `Mocassin.Packages` | Project JSON, parameterization evaluation, job templates, supercell/doping generation, package encoding, `.msl` | Solver execution, UI |
| `Mocassin.Results` | `.mcs` parsing, evaluation queries, derived quantities, exporters | Simulation execution |
| `Mocassin.Cli` | Command parsing, orchestration, process launch, progress/logging | Scientific algorithms |
| `Mocassin.App*` | Presentation, commands, navigation, dialogs | Scientific algorithms |
| `Mocassin.Rendering` | GPU abstraction, scene graph, picking, camera, overlays | Model validation |

## 4. Rust workspace

```text
mocassin-cli
   ▲
mocassin-solver ──► mocassin-state
   ▲      ▲              ▲
   │      └──► mocassin-rng
   ├──► mocassin-msl
   ├──► mocassin-types
   └──► mocassin-extension
```

| Crate | Responsibility |
| --- | --- |
| `mocassin-types` | `serde` structs for the compiled package and state metadata; no I/O |
| `mocassin-msl` | Read `.msl` SQLite, verify schema version and model signature, decode model/job payloads |
| `mocassin-rng` | Deterministic, platform-independent RNG streams; seed derivation per job |
| `mocassin-solver` | Simulation context, energy engine, KMC, MMC, trackers, statistics |
| `mocassin-state` | Binary `.mcs` writer with section table and checksums |
| `mocassin-extension` | Stable extension trait plus C ABI for optional custom routines |
| `mocassin-cli` | `mocassin-sim` argument parsing, logging, signal handling, exit codes |

The solver core must be usable as a library without the CLI, so tests and future embedding (e.g. a
long-running worker) do not need to spawn processes.

## 5. Runtime data flow

```text
Project JSON (.mocprj)
        │  load + migrate schema
        ▼
Validated model  ──► diagnostics ──► GUI / CLI
        │  compile (P1, environments, interactions, rules)
        ▼
Compiled model + parameterization + job template
        │  expand jobs, generate supercells
        ▼
Simulation package (.msl, SQLite)
        │  job id + CLI args
        ▼
Rust solver (mocassin-sim) ──► stdout/progress + run.mcs/prerun.mcs
        │  collect
        ▼
Results in .msl ──► .NET evaluation API ──► tables/plots/CSV/JSON
```

### 5.1 Process model

- The GUI and CLI run in .NET.
- A build is in-process and parallelised across jobs (bounded by CPU count).
- A simulation is a separate `mocassin-sim` process. This gives isolation, simple cancellation, clean
  memory reclamation between jobs, and natural HPC scheduling.
- The GUI may run multiple solver processes (local run queue) with a configurable concurrency.
- The CLI may launch a range of jobs sequentially or with a worker pool.
- Results are collected by reading job directories and writing to `.msl` in a transaction.

### 5.2 Determinism and seeds

- A project has an optional master seed. Each job derives a stable 256-bit seed from
  `(master_seed, collection_index, config_index, job_index)` using a documented hash (BLAKE3).
- The solver uses ChaCha12 (or a later documented PRNG) seeded from that value, with separate streams for
  lattice generation and simulation.
- Supercell generation uses its own deterministic stream, so changing simulation settings does not change
  the generated lattice for the same job coordinates.
- Same package + same solver version + same platform must reproduce the same `.mcs`. Cross-platform results
  must agree statistically; the test suite checks both properties.

## 6. Technology choices

| Concern | Choice | Rationale |
| --- | --- | --- |
| Runtime | .NET 10 LTS | Required target; modern C#, nullable, source generators, performance |
| GUI | Avalonia 11.x | Mature cross-platform XAML UI, MVVM friendly, active ecosystem |
| MVVM | CommunityToolkit.Mvvm | Source-generated observable properties/commands, minimal ceremony |
| DI/hosting | Microsoft.Extensions.* | Standard configuration, logging, DI, lifetime management |
| JSON | System.Text.Json | Built-in, source-generated contexts, trimming/AOT friendly |
| SQLite (.NET) | Microsoft.Data.Sqlite | Lightweight, no EF Core required for `.msl` |
| SQLite (Rust) | `rusqlite` with `bundled` | Reproducible builds, no system dependency |
| Package encoding | MessagePack + Zstd | Compact, schema-friendly, fast; shared conceptually with Rust |
| State encoding | Custom little-endian binary | Zero-copy arrays, documented, stable, no framework coupling |
| Rust serialization | `serde` + `rmp-serde` | Matches package schema |
| Rust CLI | `clap` | Ergonomic, shell completions, validation |
| Rust errors | `thiserror` | Typed errors at crate boundaries |
| Rust logging | `tracing` + `tracing-subscriber` | Structured logs, JSON output option |
| Rust parallelism | `rayon` for build/analysis helpers; process-per-job for MC | MC chains are sequential; job-level parallelism is the right granularity |
| Rust RNG | `rand_chacha` | Reproducible, high-quality, portable |
| Charts | ScottPlot (Avalonia) | Scientific plotting, cross-platform, export |
| 3D | Silk.NET + Avalonia native host (spike), abstraction layer | Cross-platform GPU, control over instancing/picking; fallback to OpenGL control |
| Testing (.NET) | xUnit + FluentAssertions + FsCheck | Standard, property-based geometry tests |
| Testing (Rust) | built-in + `proptest` + `criterion` + `insta` | Unit, property, benchmarks, snapshots |
| CI | GitHub Actions matrix | Windows/Linux/macOS, .NET 10, stable Rust |

## 7. Cross-cutting concerns

### 7.1 Diagnostics

```csharp
public enum DiagnosticSeverity { Info, Warning, Error }

public sealed record Diagnostic(
    string Code,                 // e.g. "MODEL001"
    DiagnosticSeverity Severity,
    string Message,
    ModelObjectPath? Path,       // domain + object id + property
    string? Hint);
```

- Validation returns `IReadOnlyList<Diagnostic>`; it never throws for user data.
- Diagnostic codes are stable and documented; the GUI maps them to help links.
- Compilation refuses to run with errors and reports warnings that affect results.

### 7.2 Units

Use explicit unit types at API boundaries to prevent Å/eV/K mix-ups:

- `Length` (meters internally), `Energy` (eV internally), `Temperature` (K), `Charge` (elementary charge),
  `Time` (seconds), `Frequency` (Hz), `ElectricField` (V/m).
- JSON uses human-friendly units with explicit suffixes: `"a": 5.41` in Å documented by the schema, and
  `"energyEv"` fields. Do not rely on implicit unit conventions.
- The solver converts to SI internally for transport quantities; energies remain eV in the physics core.

### 7.3 Error handling and exit codes

- .NET CLI: `0` success, `1` validation/model error, `2` usage error, `3` I/O error, `4` solver failure,
  `5` partial success (some jobs failed).
- Rust solver: `0` completed, `10` invalid arguments, `11` package error, `12` model/data error,
  `13` numerical failure, `14` aborted by limit, `15` cancelled, `16` I/O error.
- The GUI surfaces the same codes and a human-readable abort reason.

### 7.4 Logging

- .NET: `Microsoft.Extensions.Logging` with console + rolling file sinks; structured properties include
  project id, build id, job id.
- Rust: `tracing` with human and JSON formats; `--log-level`, `--log-file`.
- Solver stdout is reserved for the human-readable statistics dump; structured events go to stderr/JSON.

### 7.5 Security and robustness

- Treat project/package/state input as untrusted: bound counts, validate indices, reject NaN/Inf where
  invalid, cap allocations, and fuzz parsers.
- No arbitrary code execution from project files.
- Extensions are loaded only when explicitly requested (`--extension path`), with a documented ABI and a
  warning in the GUI.
- Atomic file writes: write to a temporary file in the same directory, fsync, rename. `.msl` writes use a
  transaction and a schema/version check.

### 7.6 Versioning

| Artifact | Version field | Compatibility rule |
| --- | --- | --- |
| Project JSON | `schemaVersion` integer | New reader accepts older minor versions via migrations; rejects newer major |
| `.msl` package | `format_version` + `app_version` | Solver accepts the exact supported range; refuses unknown |
| Compiled payload | `format_version` | Tied to `.msl`; independent of project schema |
| `.mcs` state | magic + `version` | Reader supports the current and previous version; rejects unknown |
| CLI JSON | `schemaVersion` | Additive changes only within a major version |

## 8. Architecture decision records

| ADR | Decision |
| --- | --- |
| [0001](adr/0001-clean-break-file-formats.md) | Clean-break, versioned file formats |
| [0002](adr/0002-rust-solver-boundary.md) | Rust solver consumes only compiled packages |
| [0003](adr/0003-avalonia-mvvm-stack.md) | Avalonia + CommunityToolkit.Mvvm + MS.Extensions |
| [0004](adr/0004-package-and-state-encoding.md) | MessagePack/Zstd package, custom binary state |
| [0005](adr/0005-statistical-parity-and-rng.md) | Statistical parity and deterministic RNG policy |
| [0006](adr/0006-3d-rendering.md) | 3D renderer abstraction and backend choice |
