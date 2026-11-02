# AGENTS.md

Guidance for AI coding agents and human contributors working in this repository.

## 1. What this project is

This is a modern, cross-platform reimplementation of **MOCASSIN** (*Monte Carlo for Solid State Ionics*),
the fixed-lattice MMC/KMC simulation suite for defects and transport in crystalline solids originally developed
at Forschungszentrum Jülich / RWTH Aachen.

**Current state: planning only.** The repository contains the reimplementation plan, architecture, data
contracts, and parity matrix. There is no production .NET solution or Rust workspace yet. Do not claim that a
feature is implemented until code and tests exist.

Target stack:

| Component | Technology |
| --- | --- |
| Model builder, compilation, package writer, results API, CLI | C# / .NET 10 |
| Desktop GUI | Avalonia UI + CommunityToolkit.Mvvm, cross-platform |
| Solver | Rust (`mocassin-sim`) |
| Project format | Versioned UTF-8 JSON (`.mocprj`) |
| Simulation package | SQLite (`.msl`) with versioned MessagePack/Zstd payloads |
| Result state | MessagePack document (`.mcs`) |
| Charts | LiveCharts2 (`LiveChartsCore.SkiaSharpView.Avalonia`) |

## 2. Read this first

Before changing anything, read these in order:

1. [`docs/planning/README.md`](docs/planning/README.md) — index and current step table.
2. [`docs/planning/00-scope-and-principles.md`](docs/planning/00-scope-and-principles.md) — scope, decisions,
   non-goals, success criteria.
3. [`docs/planning/01-target-architecture.md`](docs/planning/01-target-architecture.md) — repository layout,
   project boundaries, runtime architecture.
4. [`docs/planning/02-data-contracts.md`](docs/planning/02-data-contracts.md) — the canonical project/package/
   state/CLI contracts.
5. [`docs/planning/03-quality-strategy.md`](docs/planning/03-quality-strategy.md) — test and parity strategy.
6. [`docs/planning/04-roadmap.md`](docs/planning/04-roadmap.md) — phases, dependencies, milestones.
7. The step page you intend to work on, plus its dependencies.
8. [`docs/planning/appendix-b-parity-matrix.md`](docs/planning/appendix-b-parity-matrix.md) — what “done” means.

The plan is the source of truth. If code and plan disagree, stop and reconcile them rather than silently
following one.

## 3. Branch and merge rules (mandatory)

These rules override convenience. If you are unsure, stop and ask the user.

- **Never work on `main` or `master`.** Do not edit files, commit, rebase, force-push, or run generated
  changes there.
- Before making any change, create a dedicated branch from the current main branch:
  - feature work: `agent/<feature>` (e.g. `agent/particle-editor`)
  - bug fixes: `agent/fix-<short-description>` (e.g. `agent/fix-kmc-timestep`)
  - use lowercase kebab-case and keep the name short and specific.
- If the working tree is on `main`/`master`, stop and create a branch first. If there are uncommitted
  changes, move them to the new branch with `git switch -c agent/<name>`; never commit them on main.
- All commits for the task must be on the `agent/*` branch.
- **No merging with `main` or `master` is allowed until the user explicitly allows it.** This includes local
  merges, fast-forward merges, PR merges, rebases that rewrite shared history, and force-pushes.
- Do not push the branch or open a pull request unless the user asks. Pushing the feature branch is allowed
  only when requested.
- Do not delete another contributor's branch or rewrite shared history.
- When the task is finished, report the branch name, commit hash(es), test results, and ask for explicit
  permission before any merge.
- If the user authorizes a merge, follow their instruction exactly; otherwise leave the branch unmerged.

## 4. Non-negotiable decisions

These are recorded decisions (see [`docs/planning/adr/`](docs/planning/adr/)). Do not reverse them without an
explicit new ADR and owner approval.

1. **Clean break on legacy formats.** No reader/writer for the original `.mocprj`, `.msl`, or `.mcs` formats.
   Do not add “just a small compatibility shim” without a new decision.
2. **MIT licence for the new implementation.** The original project remains GPL-3.0-or-later. Do not copy
   original source code, documentation, or database content into MIT-licensed files. See `NOTICE.md` and
   [ADR 0007](docs/planning/adr/0007-mit-license.md).
3. **Rust solver process boundary.** The solver consumes only the compiled `.msl` package and CLI arguments.
   It never reads project JSON and never writes `.msl`. .NET never calls solver code in-process for production
   runs.
4. **Statistical parity, not bit-exact trajectories.** Use ChaCha12 and BLAKE3-derived seeds. Do not reintroduce
   PCG32 trajectory compatibility.
5. **Avalonia redesign, not a WPF port.** Preserve scientific concepts and workflows; do not port WPF controls,
   service-locator patterns, or `System.Windows.*` types.
6. **LiveCharts2 for charts.** Do not use ScottPlot.
7. **Phased full parity.** A working KMC/MMC core and model editors come before 3D, MMCFE, advanced analytics,
   and HPC polish. Do not build later-phase features while a blocking earlier step is unfinished.
8. **No silent scientific corrections.** If the original behaviour is ambiguous or wrong, document the chosen
   interpretation in the step page and add a test. Do not “fix” physics without evidence.

## 5. Repository layout

Current repository:

```text
mocassin/
├── AGENTS.md
├── README.md
├── LICENSE                 # MIT
├── NOTICE.md               # original GPL attribution + MIT boundary
├── docs/planning/          # the plan, 30 step pages, ADRs, appendices
├── docs/user-guide/        # target: rewritten user guide
├── docs/reference/         # target: formats, diagnostics, physics reference
├── src/dotnet/             # target: .NET solution
├── src/rust/               # target: Rust workspace
├── samples/                # target: ceria, barium zirconate, vehicle, interstitialcy
├── tests/contracts/        # target: cross-language .msl/.mcs fixtures
├── tests/reference/        # target: statistical parity fixtures/reports
└── tools/hpc/              # target: SLURM/PBS helpers
```

Target .NET dependency direction:

```text
Core <- Symmetry <- Compilation <- Packages <- Cli
                                  └──► Results
App -> App.ViewModels -> Packages/Results
App -> App.Views -> App.ViewModels
App -> Rendering
```

Target Rust workspace:

```text
mocassin-cli -> mocassin-solver -> mocassin-state
                    ├── mocassin-rng
                    ├── mocassin-msl
                    └── mocassin-types
mocassin-extension
```

Rules:

- `Mocassin.Core` has no SQLite, Avalonia, EF Core, or UI dependency.
- `Mocassin.Compilation` is pure and has no I/O.
- `Mocassin.Packages` owns project JSON, supercells, and `.msl`.
- `Mocassin.Results` owns `.mcs` reading and evaluation, not simulation.
- View models never reference views or controls.
- Scientific algorithms never live in view models or views.
- The Rust solver core is a library first; the CLI is a thin wrapper.

## 6. Build and test commands

These commands become valid as the corresponding projects are created. Do not run them expecting success
today.

```bash
# .NET
dotnet restore Mocassin.slnx
dotnet build Mocassin.slnx -c Release
dotnet test Mocassin.slnx -c Release
dotnet format Mocassin.slnx --verify-no-changes

# Rust
cargo fmt --manifest-path src/rust/Cargo.toml --check
cargo clippy --manifest-path src/rust/Cargo.toml --workspace -- -D warnings
cargo test --manifest-path src/rust/Cargo.toml --workspace
cargo build --manifest-path src/rust/Cargo.toml --workspace --release

# End-to-end (once the CLI exists)
dotnet run --project src/dotnet/Mocassin.Cli -- validate samples/ceria/ceria.mocprj
dotnet run --project src/dotnet/Mocassin.Cli -- build samples/ceria/ceria.mocprj -o /tmp/ceria.msl
cargo run --manifest-path src/rust/Cargo.toml -p mocassin-cli -- \
  --db /tmp/ceria.msl --job 1 --io /tmp/Job00001
dotnet run --project src/dotnet/Mocassin.Cli -- collect /tmp/ceria.msl --dir /tmp
```

Quality gates for any change:

- No new compiler/clippy warnings; warnings are errors.
- `dotnet format` and `cargo fmt --check` clean.
- Tests added/updated for the change.
- Contract fixtures updated if a format changed.
- Performance benchmarks run if the change touches a hot path.
- Step page and parity matrix updated.

## 7. Working on a step

The 30 steps are in [`docs/planning/steps/`](docs/planning/steps/). Use them as work packages, not suggestions.

1. Choose the lowest-numbered unblocked step unless the owner assigns another.
2. Check its **Depends on** line; do not start before dependencies are done.
3. Read the step’s **Original reference**, **Design**, **Acceptance criteria**, **Tests**, and **Risks**.
4. Create an `agent/*` branch as described in section 3, e.g. `agent/step-03-particle-structure`.
5. Implement only that step’s scope. Resist adding later-phase features.
6. Add the tests named in the step, plus tests for every bug you find.
7. Update the step page:
   - mark tasks/acceptance criteria as done;
   - record deviations and decisions;
   - link the implementation/PR/commit.
8. Update the parity matrix status in `appendix-b-parity-matrix.md`.
9. Run the quality gates.
10. Commit with an imperative message, e.g. `model: add particle and particle-set validation`.

A step is not done because the code compiles. Its acceptance criteria, tests, docs, and parity entries must
pass.

## 8. Coding conventions

### .NET

- .NET 10, nullable enabled, implicit usings, warnings as errors.
- Prefer immutable records/`ImmutableArray<T>` for model data; mutable only inside editors/builders.
- Use typed IDs (`ParticleId`, `CellSiteId`, …) in the model; dense integer indices only in compiled data.
- Use explicit unit types (`LengthAngstrom`, `EnergyEv`, `TemperatureKelvin`, …); no bare `double` at API
  boundaries where units matter.
- Use `Diagnostic`/`DiagnosticBag` for user errors; exceptions for programmer errors, I/O, and contract
  violations.
- Use `System.Text.Json` with source-generated contexts. No Newtonsoft.Json in new code.
- No EF Core for `.msl`; use `Microsoft.Data.Sqlite` with explicit SQL.
- Async all the way; accept `CancellationToken`; no sync-over-async.
- `Microsoft.Extensions.*` for DI, configuration, logging. No service locator in view models.
- Avalonia: CommunityToolkit.Mvvm, `ViewLocator`, `DocumentViewModel`, explicit Add/Remove commands.
- Charts: LiveCharts2 only.
- Public APIs need XML docs.

### Rust

- Stable Rust, edition 2024 unless the workspace pins otherwise.
- `#![forbid(unsafe_op_in_unsafe_fn)]`; no `unsafe` without a `// SAFETY:` comment and a test.
- Use `thiserror` at crate boundaries; no `anyhow` in library public APIs.
- Use `clap` for CLI, `tracing` for logs, `serde`/`rmp-serde` for payloads, `rusqlite` (bundled) for `.msl`.
- Hot path: flat arrays, `u32`/`u8` indices, CSR link structures, no `HashMap`, no allocation per cycle.
- All randomness comes from the injected `Rng`; never use `thread_rng` or time-based seeds.
- Energies are `f64`; never `f32`.
- Particle masks are `u64`; never `1 << id` on a 32-bit integer.
- Use MessagePack (`MessagePack-CSharp` / `rmp-serde`) for `.mcs`; do not introduce a custom binary header,
  section table, alignment scheme, or native struct layout.

## 9. Scientific invariants

Read [`docs/planning/02-data-contracts.md`](docs/planning/02-data-contracts.md) and the relevant step pages.
The most important invariants:

- **P1 position order is a contract.** Use exact rational symmetry operations and the canonical order from
  Step 02. Never let database order, hash iteration, or tolerance dedup renumber `P`.
- **Space-group operations are exact.** Integer linear part + rational translation denominator 24. Validate
  identity, inverses, closure, determinant ±1.
- **Cell geometry uses the standard crystallographic matrix.** The legacy missing-`c` bug must not return.
- **Energy units:** model and tables in eV; solver converts to kT once per job. Transport output uses SI.
- **Lattice energy:** pair contributions use the documented 0.5 factor; cluster contributions are counted at
  their center. Test the convention.
- **KMC:** uniform direction selection, rejection acceptance, `K_norm = K_fix * K_auto`,
  `dt = dof * K_norm / (nu_max * selectable_count)`. Start-unstable advances state but not time/MCS;
  end-unstable advances time but not state.
- **MMC:** Metropolis acceptance, no time advance, partner cell selected uniformly over the supercell.
- **Nernst–Einstein:** `D_sigma = sigma * k_B * T / (n * q^2)`, `q = z e`. The legacy `u * z * kT` formula is
  wrong; do not reintroduce it.
- **Unit cells:** `A*B*C`, never `A+B+C`.
- **Tracking:** global per `(transition, particle)`, mobile per particle, static per `(cell, position,
  particle)`. Displacements are fractional until output conversion.
- **Detailed balance and conservation:** every accepted step must preserve mass and charge; add a test.

When the original behaviour is ambiguous, inspect the original source as a **reference only**, document the
interpretation, and encode it in a test. Do not copy GPL code.

## 10. Data and format rules

- `docs/planning/02-data-contracts.md` is the source of truth for field names, versions, and layouts.
- Any format change requires:
  1. an ADR or explicit decision note;
  2. a version bump (`schemaVersion`, `format_version`, or `.mcs` version);
  3. updated contract fixtures in `tests/contracts/`;
  4. cross-language round-trip tests (.NET writer ↔ Rust reader; Rust writer ↔ .NET reader);
  5. updated docs.
- Never “fix” a format by silently reinterpreting existing fields.
- Write files atomically (temp file + fsync + rename). Never truncate a valid file on failure.
- Treat project/package/state input as untrusted: bound counts, validate indices, reject NaN/Inf where invalid,
  cap allocations, fuzz parsers.
- `.msl`: one shared compiled model per signature, content-addressed lattices, deterministic job IDs, pending
  result rows.
- `.mcs`: MessagePack map with `format = "mocassin.mcs"` and `formatVersion`; `lattice` as `bin`; optional
  KMC tracker/histogram fields. No custom header/section table/CRC. Integrity comes from the `.msl` SHA-256;
  enforce the < 4 GiB size assumption.

## 11. Testing expectations

- Unit tests for pure functions and formulas.
- Property-based tests for geometry, symmetry, permutation reduction, supercells, and state round-trips.
- Golden tests for compiled models, `.msl` payloads, `.mcs` documents, and CLI output.
- Contract tests across .NET and Rust for every format.
- Statistical tests for physics: detailed balance, residence time, MSD/diffusion, conductivity, field sign,
  conservation.
- Statistical parity against reference observables on ceria, barium zirconate, and a complex mechanism.
- UI view-model tests and Avalonia headless/screenshot tests.
- Fuzz project JSON, `.msl`, `.mcs` MessagePack documents, and extension config parsers.

A bug fix without a regression test is incomplete. A scientific change without a physics/parity test is
incomplete.

## 12. Licensing rules

- New source files: `SPDX-License-Identifier: MIT`.
- Do not copy original MOCASSIN source, documentation text, or database content into MIT files.
- Any GPL-derived file must be isolated, marked `SPDX-License-Identifier: GPL-3.0-or-later`, and documented in
  `NOTICE.md`.
- The space-group catalog must come from an authoritative source (International Tables/spglib), not the
  original SQLite database. If that changes, review provenance and licensing first.
- Dependencies must be MIT-compatible. GPL/AGPL dependencies require explicit review.
- Keep the original authors’ citation guidance in the README/NOTICE.

## 13. What not to do

- Do not add legacy `.mocprj`/`.msl`/`.mcs` import/export.
- Do not copy original GPL code or data into the MIT codebase.
- Do not use ScottPlot; use LiveCharts2.
- Do not introduce EF Core, Newtonsoft.Json, WPF, `System.Windows.*`, or a service locator.
- Do not let Rust read project JSON or write `.msl`.
- Do not use raw native structs/images, unaligned `Unsafe.AsRef`, or pinned whole-file buffers.
- Do not introduce a custom `.mcs` header/section table/CRC; use the MessagePack schema.
- Do not use PCG32/`string.GetHashCode()` for reproducibility; use ChaCha12/BLAKE3.
- Do not change canonical P1 order, energy conventions, or time-step formulas without an ADR and tests.
- Do not use `A+B+C` for unit-cell counts.
- Do not put scientific logic in view models.
- Do not optimize before correctness is proven by tests.
- Do not mark a step/parity item “done” without passing acceptance criteria.
- Do not work directly on `main` or `master`; always create an `agent/*` branch first.
- Do not merge into `main` or `master` without explicit user permission.
- Do not modify `/workspace/mocassin-original` if it is present. It is a read-only reference clone.

## 14. Useful references in the original repository

If `/workspace/mocassin-original` is available, these are the most useful starting points. Read, do not copy.

- Project/model semantics: `src/ModelBuilder/ICon.Model/`
- Symmetry and geometry: `src/ModelBuilder/ICon.Framework.Symmetry/`
- Compilation/translation: `src/ModelBuilder/ICon.Model.Translator/`
- Evaluation formulas: `src/ModelBuilder/Mocassin.Tools.Evaluation/`
- Solver algorithms: `src/McSolver/Libraries/Simulator/`
- User guide: `docs/userguide-md/`
- Example project: `docs/project-examples/User-Guide-Project.mocprj` (legacy format; do not import)

The analysis notes used to create this plan are not part of the repository. If they are unavailable, re-derive
from the original and the plan’s “Original reference” sections.

## 15. Definition of done for a change

- [ ] The step’s acceptance criteria are met.
- [ ] Unit/property/golden/contract tests pass.
- [ ] No new warnings; formatters and linters clean.
- [ ] Format versions/fixtures updated if applicable.
- [ ] Step page updated with outcome and deviations.
- [ ] Parity matrix updated.
- [ ] No legacy/GPL/unsafe shortcuts introduced.
- [ ] Performance benchmark run for hot-path changes.
- [ ] Commit message is imperative and scoped.
- [ ] The change can be explained in terms of the plan, not just the code.
