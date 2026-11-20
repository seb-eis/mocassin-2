# 01 — Target Architecture

## 1. Architectural principles

1. **The model is data, not a service graph.** A project is an immutable-ish aggregate that can be
   serialized, validated, diffed, and compiled without a UI or database.
2. **Compilation is an explicit pipeline.** User model → validated model → compiled model → simulation
   package. Each stage has typed inputs/outputs, diagnostics, and cacheable content hashes.
3. **One contract, three languages.** .NET writes packages and reads results; Rust reads packages and writes
   state; Python reads state for analysis. No Rust code reads project JSON and no .NET code participates in
   the Monte Carlo loop.
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
├── Mocassin.slnx                    # root solution over src/dotnet projects
├── zensical.toml                    # Zensical site config (docs_dir = "docs")
├── docs/
│   ├── index.md
│   ├── getting-started/
│   ├── user-guide/
│   ├── physics/
│   ├── tutorials/
│   ├── reference/                   # CLI, formats, diagnostics, extension ABI
│   ├── development/                 # architecture, code structure, design decisions
│   ├── performance/
│   ├── troubleshooting/
│   ├── migration/
│   ├── examples/
│   ├── glossary/
│   ├── api/                         # landing + placeholders; DocFX/rustdoc/mkdocstrings output
│   └── planning/                    # excluded from the published site
├── src/
│   ├── dotnet/
│   │   ├── Mocassin.Core/           # domain model, IDs, units, primitive geometry, diagnostics
│   │   ├── Mocassin.Symmetry/       # space groups, cell geometry, encoding, symmetry algorithms
│   │   ├── Mocassin.Compilation/    # P1 extension, environments, rules, compiled model
│   │   ├── Mocassin.Contracts/      # package/state DTOs, schema versions, contract manifest reader
│   │   ├── Mocassin.Packages/       # project persistence, templates, supercells, package encoding, .msl writer
│   │   ├── Mocassin.Results/        # .msl/.msr/.mcs readers, collection, evaluation
│   │   ├── Mocassin.Cli/            # `mocassin` headless CLI
│   │   ├── Mocassin.App/            # Avalonia application shell
│   │   ├── Mocassin.App.ViewModels/
│   │   ├── Mocassin.App.Views/
│   │   ├── Mocassin.Rendering/      # 3D abstraction + Avalonia host
│   │   └── Mocassin.*.Tests/        # unit/integration/property/golden tests
│   ├── python/
│   │   ├── pyproject.toml           # `mocassin-analysis` package
│   │   ├── mocassin/                # read-only .mcs reader + basic evaluations
│   │   └── tests/                   # pytest
│   └── rust/
│       ├── Cargo.toml               # workspace
│       ├── crates/
│       │   ├── mocassin-types/      # package/state schema types shared with .NET contract
│       │   ├── mocassin-msl/        # SQLite package reader
│       │   ├── mocassin-rng/        # deterministic RNG streams
│       │   ├── mocassin-solver/     # context, energy, KMC, MMC, tracking
│       │   ├── mocassin-state/      # MessagePack .mcs writer/reader
│       │   ├── mocassin-extension/  # extension ABI and MMCFE-compatible hooks
│       │   └── mocassin-cli/        # `mocassin-sim`
│       └── benches/
├── samples/
│   ├── ceria/
│   ├── barium-zirconate/
│   └── vehicle-demo/
├── tests/
│   ├── contracts/                   # cross-language .msl/.msr/.mcs fixtures
│   └── reference/                   # reference observables for statistical parity
├── contracts/
│   ├── manifest.json                # single source for format fields/types/versions
│   └── generated/                   # generated schemas/checks/docs fragments
└── tools/
    ├── contracts/                   # manifest validator/generator
    ├── format/                      # schema generation and validation
    ├── docfx/                       # DocFX configuration for the .NET API
    ├── docs/                        # Zensical/DocFX/rustdoc build and serve scripts
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
Mocassin.Contracts
   ▲             ▲
Mocassin.Packages  Mocassin.Results
   ▲             ▲
   └──── Mocassin.Cli ────┘
             │
             └──► Mocassin.Compilation (read-only compile consumer)
             ▲
Mocassin.App ──► Mocassin.App.Views ──► Mocassin.App.ViewModels
     │                                      │
     │                                      └──► Contracts/Packages/Results
     └──► Mocassin.Rendering
```

Rules:

- `Core` has no dependency on SQLite, Avalonia, or EF Core. Only BCL.
- `Core` must not reference `Symmetry`. Persisted vocabulary (`AxisSetting`, `OriginChoice`) and the
  catalog-derived `CrystalSystem` live in `Core`; the catalog, resolved settings, previews, and
  catalog-dependent validation are `Symmetry` services that accept Core model types.
- `Symmetry` may depend on `Core` and the committed, embedded space-group JSON catalog. It must not require
  a runtime database or a build-time Python/spglib dependency.
- `Compilation` depends on `Core` + `Symmetry`; it has no I/O and no UI.
- `Contracts` depends on `Compilation`; it owns versioned package/state DTOs and schema constants and does not
  depend on `Packages` or `Results`.
- `Packages` depends on `Compilation` + `Contracts`; it owns project serialization, templates, supercells,
  `.msl` writing, and build orchestration. Parameterization follows the same direction: the persisted template
  and value records are in `Core`, the applied `ParameterizationValues` and signature projection are in
  `Contracts`, and the factory, apply, validation, diff, and rebase services are in `Packages`. `Compilation`
  never sees a template or an energy/frequency value. `.msl` structural reading and verification live in
  `Results`, so one reader serves the build CLI, evaluation, and external tools. `Packages` does not reference
  `Results`; post-build verification is composed by the caller.
- `Results` depends on `Core` + `Contracts` (not `Packages`) and owns `.msl`/`.msr`/`.mcs` reading, structural
  package inspection/verification, collection, and evaluation.
- Package-verification diagnostics (`PKG`) are Contracts-owned descriptors. CLI diagnostics (`CLI`) are also
  Contracts-owned so `Packages` composes them into its `DiagnosticCatalog` for the docs-drift check and the
  CLI constructs them without a reverse edge. `Results` constructs its diagnostics from the same descriptors
  and must not reference `Mocassin.Packages` or its catalog.
- `Cli` depends on `Packages` + `Results`; it may launch the Rust solver. It also references
  `Mocassin.Compilation` read-only for `mocassin compile` and the `compiled-inspection-v1` writer. It owns
  command parsing, build sequencing, progress rendering, JSON/report formatting, and exit-code policy only.

- `App` is the composition root and references views, view models, and rendering. Views reference view models;
  view models depend on public library APIs and never reference views or controls.
- `Rendering` is independent of the project model; it consumes render primitives.

### 3.1 Responsibilities

| Project | Owns | Must not own |
| --- | --- | --- |
| `Mocassin.Core` | IDs, units, primitive geometry (`Fractional3`, `Cartesian3`, `Vector4I`), persisted symmetry vocabulary (`AxisSetting`, `OriginChoice`), catalog-derived `CrystalSystem`, model objects (`SpaceGroupRef`, `CellParameters`, `CellSite`, `StructureModel`), parameterization template records, project aggregate, diagnostics, validation contracts, undo/redo primitives | File I/O, SQL, UI, solver data, catalog-dependent preview/validation, applied parameterization values |
| `Mocassin.Symmetry` | Space-group catalog, `SpaceGroupSetting`, `SymmetryOperation`, exact operations, cell geometry transforms, unit-cell encoding, orbit/permutation reduction, structure preview, catalog-dependent structure validation | Model semantics, persistence, Core model ownership |
| `Mocassin.Compilation` | P1 extension, environment sampling, interaction discovery, transition rule generation, index assignment, compiled model | Job templates, supercells, parameterization values, site-energy background, I/O |
| `Mocassin.Contracts` | Package/results/state DTOs (`ParameterizationValues`, compiled-model payload, job/lattice payloads, `.mcs` document types, job-folder/solver/event contracts), the parameterization and lattice signature projections, schema versions, package and CLI diagnostic descriptors, manifest reader | Build logic, SQL, UI, solver execution |
| `Mocassin.Packages` | Project JSON persistence, composed project validation (Core + Symmetry + `PROJ034`), parameterization factory/apply/validation/rebase, job-template expansion, supercell/doping generation and preview, package encoding, immutable `.msl` writing, build orchestration (stage sequencing, selection, progress events, job selection, and the typed `BuildPreview` payload) | Solver execution, UI, `.msl`/`.msr` reading, post-publication verification |
| `Mocassin.Results` | `.msl` structural reading/inspection/verification, `.msr`/`.mcs` parsing, result collection, evaluation queries, derived quantities, exporters | Simulation execution, package building/writing |
| `Mocassin.Cli` | Command parsing, build/preview/compile/inspect/verify/doctor/clean sequencing, mapping the library’s `BuildResult.Preview`, process launch, progress/logging, JSON reports, exit-code policy | Scientific algorithms, package encoding/decoding, verification logic, a second preview implementation |
| `Mocassin.App*` | Presentation, commands, navigation, dialogs | Scientific algorithms |
| `Mocassin.Rendering` | GPU abstraction, scene graph, picking, camera, overlays | Model validation |

### 3.2 UI composition and state ownership (Step 23)

The Avalonia track is a consumer of the same public library APIs as the CLI. Step 23 fixes its composition,
dependency direction, and state ownership; Steps 24–28 add editors, build UI, 3D, and run monitoring through
registration seams without changing the shell.

```text
Mocassin.App  ──►  Mocassin.App.Views  ──►  Mocassin.App.ViewModels  ──►  Packages / Results / Contracts / Core
      │                                                                  (public library APIs only)
      ├──► Avalonia, Microsoft.Extensions.*, Serilog
      └──► Mocassin.Rendering (unused until Step 27)
```

| Project | Owns | Must not own |
| --- | --- | --- |
| `Mocassin.App` | `Program`, `App`, `ViewLocator`, the composition root (`AppHost`, registration, startup options, app paths, logging setup, crash boundary), startup coordination, and the app-local I/O implementations: the settings store, the recovery store, the project file probe, and log retention | View models, views, or scientific logic |
| `Mocassin.App.ViewModels` | Shell and region view models, document model, navigation service and target registry, project session and edit coordinator, validation coordinator, settings model and pure validator/migrations, recovery/theme contracts, command catalog and palette, dialog/picker/dispatcher contracts, all virtualization of library values for binding | Any Avalonia or control type, views, `System.IO`/`Environment`/`Application`/resource dictionaries, scientific algorithms, a second store/validator/hash/catalog |
| `Mocassin.App.Views` | Shell window and views, region hosts, dialog overlay host, console/problems views, settings and palette views, theme/density/high-contrast resource dictionaries, the theme applier, the storage-picker and dispatcher implementations | Library calls, scientific logic, persistence decisions |

Rules:

- `Mocassin.App.ViewModels` references `CommunityToolkit.Mvvm` and the public libraries only; it has no
  Avalonia, view, or control reference. `Mocassin.App.Views` may use controls but never constructs domain
  state. `Mocassin.App` is the only project that resolves the DI container.
- **I/O boundary (Step 23).** `Mocassin.App.ViewModels` may call public library APIs that perform I/O
  (`ProjectStore`, `ProjectValidationPipeline`, `ProjectTemplateFactory`) because those are the contracts the
  CLI also uses. It must not use `System.IO`, `Environment`, `Application`, or resource dictionaries. App-local
  files and theme application go through ViewModels contracts implemented in `Mocassin.App`
  (`IAppSettingsStore`, `IRecoveryStore`, `IProjectFileProbe`, `AppPaths`) and `Mocassin.App.Views`
  (`IThemeApplier`, `IStorageService`, `IUiDispatcher`). A source-token test in the architecture suite enforces
  the ban, so the rule is checkable rather than aspirational.
- The GUI never references `Mocassin.Cli`. Behaviour the GUI needs must live in a library; the CLI and the GUI
  share `ProjectStore`, `ProjectTemplateFactory`, `ProjectNameRules`, `ProjectDocument`, and the validation
  pipeline.
- One writer per project: a `ProjectEditCoordinator` over the Step 01 `ProjectDocument`. View models never
  mutate a `Project` or a domain object, never compute a project hash, and never build a `ProjectChange`.
- App-scoped UI state (theme, density, layout, recent files, session) is stored in the application settings
  file; document-scoped view state (camera, visibility, per-object style) is stored in the project
  `viewSettings`. There are no ad-hoc UI state files.
- Shell state is UI-thread confined. Project edits happen on the UI thread; validation runs on an immutable
  snapshot off-thread and is applied only when it is not stale.
- Contribution seams (navigation providers, explorer nodes, inspector content, menu items, console channels,
  status-bar items) are explicit interfaces in `Mocassin.App.ViewModels`, registered in the composition root.
  No assembly scanning, no reflection-based discovery, no service locator.
- A later step adds a provider and a registration call; it does not edit the shell. If a later step needs a new
  seam, it adds the interface, registers it, and records the change in its own step page.

## 4. Rust workspace

```text
mocassin-cli
   ├──► mocassin-msl ──► mocassin-types
   ├──► mocassin-rng
   ├──► mocassin-types
   └──► mocassin-solver (placeholder in Step 13)
              │
              ├──► mocassin-state
              ├──► mocassin-rng
              ├──► mocassin-msl
              ├──► mocassin-types
              └──► mocassin-extension
```

| Crate | Responsibility |
| --- | --- |
| `mocassin-types` | `serde` payload DTOs for the compiled package and state metadata, typed dense IDs, the validated runtime model/job/lattice types, `ReaderLimits`; no I/O |
| `mocassin-msl` | Read `.msl` SQLite read-only, verify schema/version/provenance and model/lattice signatures, boundedly decode model/job/lattice payloads, re-encode canonical projections |
| `mocassin-rng` | Deterministic, platform-independent ChaCha12 streams; SHA-256 seed derivation; bounded draws; 64-byte `RngStateV1` |
| `mocassin-solver` | Simulation context, energy engine, KMC, MMC, trackers, statistics |
| `mocassin-state` | MessagePack `.mcs` schema, writer, and reader |
| `mocassin-extension` | Stable extension trait plus C ABI for optional custom routines |
| `mocassin-cli` | `mocassin-sim` argument parsing, logging, signal handling, dry-run inspection, future run path, exit codes |

The solver core must be usable as a library without the CLI, so tests and future embedding (e.g. a
long-running worker) do not need to spawn processes. The CLI uses `mocassin-msl` and `mocassin-rng` directly
for the inspection path; there is only one package-reader implementation.

Rules:

- `mocassin-types` has no SQLite, Zstd, tracing, CLI, or file I/O dependency; it is the shared vocabulary for
  the reader, CLI, and engine.
- `mocassin-msl` is the only crate that opens SQLite, decodes Zstd/MessagePack, and re-encodes canonical
  projections. It never writes a database.
- `mocassin-rng` is the only crate that names `rand_chacha` state APIs. The `RngStateV1` bytes are a
  first-party format, not a third-party memory layout.
- The CLI keeps its existing `mocassin-solver` dependency for the future run path; Step 13's inspection path
  calls `mocassin-msl`/`mocassin-rng` directly and does not call the placeholder solver.
- `Package` is not `Sync`; keep one package per thread/process, and a future batch worker opens its own
  package/connection.

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
Simulation package (.msl, immutable SQLite)
        │  job id + CLI args
        ▼
Rust solver (mocassin-sim) ──► job folder: run.mcs/prerun.mcs, solver.json, logs
        │  collect (reads job folders)
        ▼
Results store (.msr, SQLite) ──► .NET evaluation API ──► tables/plots/CSV/JSON
        │
        └──► run.mcs/prerun.mcs and .msr ──► Python `mocassin` API ──► NumPy arrays / evaluations
```

### 5.1 Process model

- The GUI and CLI run in .NET; the Python analysis API runs in its own Python process.
- A build is in-process and parallelised across jobs (bounded by CPU count).
- A simulation is a separate `mocassin-sim` process. This gives isolation, simple cancellation, clean
  memory reclamation between jobs, and natural HPC scheduling.
- The GUI may run multiple solver processes (local run queue) with a configurable concurrency.
- The CLI may launch a range of jobs sequentially or with a worker pool.
- `.msl` is immutable and never written after build; solver and readers open it read-only.
- Results are collected by reading job folders and writing to `.msr` in a transaction. One collector owns a
  results store at a time.

### 5.2 Determinism and seeds

- A project has an optional master seed. Each job derives a stable 256-bit seed from
  `(master_seed, domain, collection_index, config_index, job_index)` using a documented hash (SHA-256); the
  job domain is the fixed `mocassin/job/v1` string.
- The solver uses ChaCha12 (or a later documented PRNG) seeded from that value, with separate streams for
  lattice generation and simulation. Step 13 fixes the derivation byte order, the `chacha12-v1` draw
  semantics, and the 64-byte `RngStateV1` resume blob.
- The simulation stream is continuous across a KMC pre-run and its main run; the pre-run state carries the RNG
  state the main run resumes. Extensions receive a domain-separated fork so optional draws cannot perturb the
  core trajectory.
- Supercell generation uses its own deterministic stream, so changing simulation settings does not change
  the generated lattice for the same job coordinates.
- Same package + same solver version + same platform must reproduce the same state content (excluding
  timestamp/runner metadata). Cross-platform results must agree statistically; the test suite checks both
  properties.

### 5.3 Concurrency and threading

| Area | Policy |
| --- | --- |
| UI thread | View-model property changes and command state updates happen on the UI thread. Project edits (`ProjectEditCoordinator` → `ProjectDocument`) are UI-thread confined; validation runs off-thread over an immutable snapshot and is applied only when its document hash is still current. Background work returns through the dispatcher; no blocking waits in commands. |
| Build | Bounded `Parallel.ForEachAsync`/worker pool; immutable inputs; per-stage caches are thread-safe; cancellation stops scheduling new work. |
| Solver | One process per job. Monte Carlo trajectories are sequential; job-level parallelism only. The GUI run queue caps concurrent processes. |
| Project files | One writer per project via `.mocprj.lock`; readers use an immutable snapshot; autosave is atomic. |
| `.msl` | Immutable after publication; any number of readers and solver processes may open it read-only. |
| `.msr` | Single collector via `.msr.lock`; readers use read-only snapshots; collection is transactional. |
| SQLite | Explicit `busy_timeout` and retry policy; no WAL dependence for published `.msl`; `.msr` uses `journal_mode=DELETE`. |
| Logging | Thread-safe sinks; no UI-thread blocking; bounded channels for progress events. |

The detailed file-sharing rules are in ADR 0011.

## 6. Technology choices

| Concern | Choice | Rationale |
| --- | --- | --- |
| Runtime | .NET 11 (STS) | Required target; modern C#, nullable, source generators, performance |
| GUI | Avalonia 12.x (pinned 12.1.6) | Current cross-platform XAML UI, MVVM friendly, active ecosystem |
| MVVM | CommunityToolkit.Mvvm | Source-generated observable properties/commands, minimal ceremony |
| Model editor layout | Master-detail table/list + type-specific editor; Step 24 layout spike | No paid grid or third-party grid dependency; final layout chosen from owner-reviewed POCs |
| DI/hosting | Microsoft.Extensions.* | Standard configuration, logging, DI, lifetime management |
| JSON | System.Text.Json | Built-in, source-generated contexts, trimming/AOT friendly |
| CLI parsing (.NET) | `System.CommandLine` 3.0.0 | Stable, MIT, CLI-only, no transitive dependency on `net10.0+`; AOT/trim-verified on `linux-x64`; typed binding and the library help action without a hand-written parser; the CLI supplies the root-only `--version` action. Bounded `@file` response files are CLI-owned. No shell-script generation; `[suggest]` is not a contract |
| SQLite (.NET) | Microsoft.Data.Sqlite | Lightweight, no EF Core required for `.msl`/`.msr` |
| SQLite (Rust) | `rusqlite` with `bundled` | Reproducible builds, no system dependency; reads `.msl` and `mmcfe.db` |
| Package encoding | MessagePack + Zstd | Compact, schema-friendly, fast; shared conceptually with Rust |
| State encoding | MessagePack | Same ecosystem as package payloads; no custom container; < 4 GiB workload |
| Contract source | `contracts/manifest.json` + generator | One field/type/version source for C#, Rust, Python, docs, and fixtures |
| Rust serialization | `serde` + `rmp-serde` | Matches package and state schema |
| Rust CLI | `clap` | Ergonomic, shell completions, validation |
| Rust errors | `thiserror` | Typed errors at crate boundaries |
| Rust logging | `tracing` + `tracing-subscriber` | Structured logs, JSON output option |
| Rust parallelism | `rayon` for build/analysis helpers; process-per-job for MC | MC chains are sequential; job-level parallelism is the right granularity |
| Rust RNG | `rand_chacha` | Reproducible, high-quality, portable |
| Charts | LiveCharts2 (`LiveChartsCore.SkiaSharpView.Avalonia`) | Scientific plotting, cross-platform, SkiaSharp rendering/export; verify Avalonia 12 compatibility before Step 27 |
| Narrative docs | Zensical | Markdown-first, MkDocs-compatible, navigation/search, strict link validation |
| .NET API docs | DocFX | XML-doc/assembly metadata, .NET navigation and cross-references |
| Rust API docs | rustdoc (`cargo doc`) | Canonical Rust docs, intra-doc links, doc tests |
| Python API docs | mkdocstrings | Native Zensical integration, docstrings/type hints, no separate site |
| Python tests | pytest | Standard scientific Python test framework |
| Docs validation | Zensical `--strict`, DocFX, rustdoc, mkdocstrings | Broken links and missing API docs/docstrings block CI |
| 3D | Silk.NET + Avalonia native host (spike), abstraction layer | Cross-platform GPU, control over instancing/picking; fallback to OpenGL control |
| Testing (.NET) | xUnit + FluentAssertions + FsCheck | Standard, property-based geometry tests |
| Testing (Rust) | built-in + `proptest` + `criterion` + `insta` | Unit, property, benchmarks, snapshots |
| CI | GitHub Actions matrix | Windows/Linux/macOS, .NET 11, stable Rust |

### 6.1 Modern .NET/C# usage

Use modern features where they remove real duplication, allocations, or ambiguity; keep public scientific
types explicit.

| Feature | Where it fits | Why |
| --- | --- | --- |
| `readonly record struct`, `required`, `init` | IDs, units, DTOs, immutable model records | Value semantics, non-null initialization, less boilerplate |
| Generic math (`INumber<T>`, `IFloatingPointIeee754<T>`) | Public vector/matrix math (`Vector3<T>`, `Vector4<T>`, `Matrix3<T>`, `Matrix4<T>`), internal numeric helpers | Shared arithmetic without collapsing semantic public coordinate types; floating-point matrices exclude `decimal`; exact integer 3×3 remains `Int3x3` |
| `FrozenDictionary` / `FrozenSet` | Compiled particle/site/rule index maps, lookup tables | Fast immutable lookups after compilation |
| `System.Numerics.Tensors` (`TensorPrimitives`) | MSD, displacement, correlation, and array reductions in Results | Vectorized reductions without hand-written SIMD |
| `System.Threading.Lock` | Shared caches, session state, package-writer coordination | Cheaper, clearer mutual exclusion than `lock(object)` |
| `TimeProvider` | Build/run timestamps, tests, deterministic clock injection | Avoid static `DateTime.UtcNow`; make time testable |
| `Parallel.ForEachAsync` / `Channel<T>` | Job expansion, build stages, progress pipelines | Bounded parallelism and backpressure without thread-blocking |
| `SearchValues<T>` / `[GeneratedRegex]` | Space-group literal parsing, connector strings, filter expressions | Allocation-light parsing with compile-time regexes |
| `SHA256.HashData` / `IncrementalHash` | Result integrity, package verification, manifests | Standard hashing without custom checksum code |
| `LoggerMessage` / `Meter` / `ActivitySource` | Build, solver orchestration, evaluation | Structured, low-overhead logs and metrics |
| `CollectionsMarshal.AsSpan` / `MemoryMarshal` | Tight loops over compiled arrays after profiling | Avoid copies; keep `Span` scoped to synchronous code |
| `BitOperations` | Particle masks and bitmask validation | Correct 64-bit operations and popcounts |
| `PublishAot` / single-file publish | CLI and helper tools, if dependencies allow | Smaller, faster-starting tools; not a requirement for the GUI |

**Do not use**

- `INumber<T>` on coordinate vectors; vectors are not scalar numbers.
- `System.Numerics.Vector3` for geometry; it is `float`, not `double`.
- `dynamic`, reflection-based dispatch, or service locators in new code.
- `Span<T>`/`ref struct` across `await` boundaries.
- `record` types with mutable collection properties; use `ImmutableArray`/frozen collections.
- SIMD or generic math complexity before profiling shows a need.

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

- .NET CLI: `0` success, `1` validation/model/package-invalid/unhealthy-doctor, `2` usage error, `3` I/O
  error, `4` compilation/parameterization error, `5` partial success (reserved for multi-job commands;
  `build` is all-or-nothing), `6` cancelled, `10` unexpected internal error. `validate` keeps its Step 07
  0/1/2/3 subset. See `02-data-contracts.md` §5.7.6. The CLI parser library's own parse-error exit code (1)
  is remapped to `CLI001`/2; help and version exit 0. Response-file (`@file`) errors are usage errors
  (`CLI001`/2), not I/O errors (`CLI002`/3).
- Rust solver: `0` completed, `5` partial success for multi-job/batch commands (at least one job failed,
  others completed; reserved until Step 19), `10` invalid arguments, `11` package error, `12` model/data error,
  `13` numerical failure **or internal invariant violation** (a caught panic is reported here), `14` aborted by
  limit, `15` cancelled, `16` I/O error. Parse errors (unknown option, bad enum value, missing value) are `10`,
  not the parser library's default; help and `--version` exit 0.
- The solver table is the `mocassin-sim` process contract. It is separate from the .NET `mocassin` table; do not
  reuse the .NET table for solver exit codes. `5` is the one intentional shared meaning (partial success for
  multi-job/batch), and it is reserved until Step 19. `docs/reference/cli.md` and the Step 13 page mirror this
  table and must not redefine a code.
- The GUI surfaces the same CLI codes for build/collect operations and the solver codes for solver failures,
  plus a human-readable abort reason.

### 7.4 Logging and observability

- .NET: `Microsoft.Extensions.Logging` with console + rolling file sinks; structured properties include
  project id, build id, job id.
- Rust: `tracing` with human and JSON formats; `--log-level`, `--log-file`.
- Solver stdout is reserved for the human-readable statistics dump; structured events go to stderr/JSON.
- The JSONL event catalog in `02-data-contracts.md` §5.5 is versioned. Consumers ignore unknown events and
  fields; a future event schema major is rejected.
- Logs are local only. Default retention is 14 days or 100 MB (whichever first); projects, packages, results,
  and job folders are never auto-deleted.
- Log records must not contain credentials or full remote-deploy secrets. Paths may be logged; user names and
  hostnames are optional and can be redacted.
- Metrics (`Meter`/`ActivitySource`) are local diagnostics, not telemetry; no remote exporter is enabled by
  default.

### 7.5 Security and robustness

**Threat model.** Project, package, state, event, and log files are untrusted input. Extensions are untrusted
native code running in-process. The solver process and the remote-deploy transport are additional trust
boundaries. There is no server component and no remote telemetry.

- Bound counts, validate every index/length, reject NaN/Inf where invalid, cap allocations, and fuzz all
  parsers.
- No arbitrary code execution from project/package/state files.
- Extensions are loaded only when explicitly requested (`--extension path`), with the ADR 0014 ABI, panic
  containment, a GUI warning, and a `--no-extensions` option.
- Reject path traversal, symlinks, and zip-slip in job-folder, import, and deploy paths before writing.
- Bound Zstd decompression and MessagePack array/map sizes; reject decompression bombs before allocation.
  The Rust reader additionally enforces a non-allocating MessagePack element budget, nesting-depth limit, and
  per-field count caps before typed decoding (Step 13 §3.3–§3.4).
- Open SQLite inputs read-only and validate schema/version before trusting page content.
- Remote-deploy credentials come from an agent/key store and are never written to a project or log.
- Atomic file writes: write to a temporary file in the same directory, fsync, rename. `.msl` and `.msr`
  writes use a transaction and a schema/version check.
- Residual risk: an in-process extension can crash or compromise the solver. This is accepted for v1 and
  documented for users.

### 7.6 Versioning

| Artifact | Version field | Compatibility rule |
| --- | --- | --- |
| Project JSON | `schemaVersion` integer | New reader accepts older versions via in-memory migrations; rejects newer major |
| `.msl` package | `format_version` + `app_version` | Immutable; reader supports current and previous major (ADR 0013) |
| `.msr` results | `format_version` + package signature | Reader supports current and previous major; bound to one package |
| Compiled payload | `format_version` | Tied to `.msl`; independent of project schema |
| `.mcs` state | `format` + `formatVersion` | Current major only; old states require the release that wrote them |
| `solver.json` | `format` + `formatVersion` | Current and previous major |
| MMCFE log DB | `format_version` | Current and previous major |
| CLI JSON/events | `schemaVersion` | One version per report id and for the event stream; additive changes only within a major version |
| .NET libraries | NuGet SemVer | No breaking public API within a major; `ApiCompat` gate |
| Python package | `__version__` + declared format range | New format major requires a package major; wheel per release |

## 8. Architecture decision records

| ADR | Decision |
| --- | --- |
| [0001](adr/0001-clean-break-file-formats.md) | Clean-break, versioned file formats |
| [0002](adr/0002-rust-solver-boundary.md) | Rust solver consumes only compiled packages |
| [0003](adr/0003-avalonia-mvvm-stack.md) | Avalonia + CommunityToolkit.Mvvm + MS.Extensions |
| [0004](adr/0004-package-and-state-encoding.md) | MessagePack/Zstd package, MessagePack state |
| [0005](adr/0005-statistical-parity-and-rng.md) | Statistical parity and deterministic RNG policy |
| [0006](adr/0006-3d-rendering.md) | 3D renderer abstraction and backend choice |
| [0007](adr/0007-mit-license.md) | MIT license for the reimplementation; original GPL attribution |
| [0008](adr/0008-standalone-documentation-toolchain.md) | Zensical narrative docs; DocFX/rustdoc/Python APIs under one site |
| [0009](adr/0009-contract-manifest-and-contracts-library.md) | Shared contract manifest and `Mocassin.Contracts` |
| [0010](adr/0010-canonical-messagepack-signatures.md) | Canonical MessagePack projection for signatures |
| [0011](adr/0011-immutable-package-and-results-store.md) | Immutable `.msl` package and separate `.msr` results store |
| [0012](adr/0012-transport-and-msd-definitions.md) | Origin-based MSD and per-species transport definitions |
| [0013](adr/0013-format-compatibility-and-api-versioning.md) | Format compatibility and public API versioning |
| [0014](adr/0014-extension-abi-and-trust-boundary.md) | Extension ABI and in-process trust boundary |
| [0015](adr/0015-cli-parser-system-commandline.md) | `System.CommandLine` 3.0.0 for the CLI; parse-error remap; bounded `@file`; no completion command |
