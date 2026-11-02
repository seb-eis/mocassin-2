# 03 — Quality Strategy

## 1. Principles

1. **Specify behaviour before code.** Each step page names the invariant, formula, or user workflow that
   must hold. Tests encode it.
2. **Test the contract, not the implementation.** Cross-language tests use `.mocprj`/`.msl`/`.mcs` fixtures,
   not shared code.
3. **Property tests for geometry and symmetry.** Brute-force expansion is the oracle for symmetry reduction.
4. **Statistical tests for physics.** A single lucky trajectory proves nothing; compare ensembles within a
   documented tolerance.
5. **Performance is a test.** Benchmarks run on every release candidate; regressions above 10 % fail.
6. **Fail loudly on invalid data.** Parsers and compilers reject malformed input with diagnostics, never
   silently coerce.

## 2. Test pyramid

| Layer | Scope | Tools | Runs |
| --- | --- | --- | --- |
| Unit | Pure functions, value types, formulas | xUnit, FsCheck, `proptest` | Every PR |
| Component | One library/domain, in-memory | xUnit, `insta` | Every PR |
| Contract | .NET ↔ Rust `.msl`/`.mcs` | xUnit + Rust integration tests + fixture files | Every PR |
| Integration | Build → simulate → collect → evaluate on a tiny system | CLI + Rust + SQLite | Every PR (short) / nightly (long) |
| Statistical | Reference systems vs known observables | Custom harness + CSV/JSON comparison | Nightly and release |
| Performance | Solver throughput, build speed, memory | Criterion, BenchmarkDotNet | Weekly and release |
| Python API | `.mcs` reader, helpers, aggregation | pytest | Every PR |
| UI | View-model commands, validation, navigation | xUnit + Avalonia headless | Every PR |
| End-to-end | GUI workflows on each OS | Avalonia headless + screenshot tests | Nightly |

## 3. Domain test strategy

### 3.1 Core and project format

- JSON round-trip: model → JSON → model is identity for all fixtures.
- Canonicalization: object order changes do not change serialized bytes.
- Migration tests: each schema version has a fixture and an expected migrated model.
- Validation tests: one test per diagnostic code, asserting severity, path, and hint.
- Property tests for units (no negative lengths, valid angles, charge finite).

### 3.2 Symmetry and geometry

- For every space group in the catalog:
  - operations compose associatively and have inverses;
  - the identity is present;
  - applying all operations to a general position yields the expected multiplicity;
  - the P1 expansion of a special position is closed under the group.
- Brute-force oracle: expand a cell by explicit translation + rotation, deduplicate by tolerance, and compare
  with the optimized orbit generator for random cells and sites.
- Coordinate round-trips: fractional → 4D → fractional, fractional → Cartesian → fractional, including
  positions outside `[0,1)` and periodic images.
- Distance tests: minimum-image distances match a brute-force search over neighboring cells.
- Permutation reduction: for random pair/cluster geometries, the reduced set is a complete and
  non-redundant representation of the orbit.

### 3.3 Compilation

- Golden compiled models for ceria, barium zirconate, and a vehicle/interstitialcy model. A golden file
  records counts, indices, interaction energies, rule state triplets, and tracker mappings.
- Invariants:
  - every active transition rule has valid particle indices;
  - mass and charge are conserved per rule (except explicitly allowed charge-transfer rules);
  - every rule has a corresponding direction and collection entry;
  - every stable pair interaction has a table entry;
  - every cluster permutation maps to a valid occupation code;
  - P1 counts match brute-force expansion.
- Determinism: compiling the same model twice yields byte-identical payloads.

### 3.4 Supercell and doping

- Doping fractions respect integer truncation and dependent-exchange ratios.
- Charge compensation is exact (total charge within tolerance) or the build fails with a diagnostic.
- Doping order is respected; changing priority changes the lattice deterministically.
- Same seed + same job coordinates yields the same lattice across runs.
- Edge cases: zero doping, 100 % doping, supercell too small for requested fraction, dependent exchange
  impossible, multiple dopings on the same site, vacancy default occupation.
- Property test: for random valid inputs, the number of replaced particles never exceeds the available
  count and every replaced particle came from the source set.

### 3.5 Solver physics

| Test | Oracle |
| --- | --- |
| MMC detailed balance | Boltzmann distribution for a two-state system; chi-square test |
| MMC energy minimization | Known ground state of a small Ising-like lattice |
| KMC residence time | Analytic `τ = ν₀⁻¹ exp(ΔE/kT)` for a single barrier |
| KMC time step | `Δt = K_norm / Σν` matches a hand-computed small system |
| KMC normalization | Fixed and automatic normalization reproduce the same long-time limit |
| Electric field | Drift velocity sign/magnitude for a single charge in a constant field |
| Pair energy | Hand-computed delta for a two-particle swap |
| Cluster energy | Hand-computed delta for a 3-body occupation change |
| Site energy | Barrier shift equals the configured site energy |
| Charge/mass conservation | Sum over lattice unchanged after every accepted step |
| Trackers | Displacement equals final minus initial position for a single particle |
| MSD/diffusion | 1D random walk with known `D = Γa²/2` |
| Conductivity | Nernst–Einstein consistency in the dilute limit |

### 3.6 Statistical parity with the original

Reference systems:

1. **Reduced ceria** — oxygen vacancy migration + polaron hopping (KMC) at 773 K and 1073 K.
2. **Yttrium-doped barium zirconate** — proton/vacancy transport with hydration dopings.
3. **A vehicle or interstitialcy model** — validates complex transition rule generation.

For each system, record:

- equilibrium lattice energy distribution (MMC) and mean energy vs temperature;
- acceptance/rejection/skip/blocked/S0/S2-unstable fractions;
- conductivity and mobility vs temperature (Arrhenius fit: prefactor and activation energy);
- diffusion coefficient and correlation factor;
- jump-barrier histograms;
- particle densities and lattice occupation statistics.

Comparison method:

- Run the original C solver and the Rust solver with the same physical inputs, multiple seeds, and enough
  samples to make the standard error small relative to the tolerance.
- Compare means with Welch's t-test or a confidence-interval overlap criterion; require the difference to be
  within `max(3σ, 2 %)` for intensive quantities and `max(3σ, 5 %)` for transport coefficients.
- Compare distributions with a Kolmogorov–Smirnov test and an effect-size threshold.
- Record the exact reference version, commit, compiler flags, and seeds in `tests/reference/README.md`.

Bit-exact trajectories are explicitly not required (see ADR 0005). Determinism is required only for the new
solver with the same binary, platform, package, and seed.

## 4. Contract tests

- **Project JSON**: .NET writes fixtures; a JSON Schema validator checks them in CI.
- **Compiled model**: .NET writes `.msl`; Rust reads it, validates invariants, and emits canonical JSON;
  compare with the .NET canonical JSON.
- **State file**: Rust writes a MessagePack `.mcs` for a synthetic small run; .NET and Python read it and emit
  canonical JSON summaries; compare all three with the schema version.
- **Version rejection**: each reader must reject a fixture with a future major version and a malformed/
  truncated MessagePack document with a clear error.
- **Fuzzing**: `cargo-fuzz` on the `.msl` and `.mcs` readers; SharpFuzz on the project JSON reader.
  Crashes and OOMs are release blockers.

## 5. UI testing

- View-model tests cover commands, validation display, navigation, dirty tracking, undo/redo, and
  build/run state transitions without a display.
- Avalonia headless tests render key views and assert on the visual tree and accessibility properties.
- Screenshot tests for the shell, each editor, and the 3D viewport on Windows/Linux/macOS (tolerance-based).
- Manual smoke checklist per release: create project, add each object type, validate, parameterize, build,
  run a tiny job, collect, plot, save/reopen.

## 6. CI pipeline

### Pull request

1. `dotnet format --verify-no-changes`
2. `dotnet build -c Release -warnaserror`
3. `dotnet test` (unit, component, contract, short integration)
4. `cargo fmt --check`
5. `cargo clippy -- -D warnings`
6. `cargo test`
7. Python `pytest`, wheel build, and clean-install check
8. Contract fixture round-trips
9. Avalonia headless UI tests
10. License/header check and dependency audit

Matrix: `ubuntu-latest`, `windows-latest`, `macos-latest`; .NET 10 SDK; stable Rust; Python 3.11–3.13.
Cache NuGet, Cargo, pip, and the SQLite build.

### Nightly

- Long integration (10⁶-cycle KMC/MMC)
- Statistical parity suite (reduced sample count)
- Criterion/BenchmarkDotNet with regression thresholds
- Fuzzing (time-boxed)
- Packaging smoke test (self-contained binaries on each OS)

### Release candidate

- Full statistical parity with high sample counts
- 20×20×20 stress test
- 10⁴-job build/collect test
- Upgrade test from previous schema version
- Manual GUI checklist on all three OSes

## 7. Performance targets and benchmarks

| Scenario | Target |
| --- | --- |
| Build compiled model (ceria, 100 jobs) | < 5 s on a 4-core laptop |
| Build 10⁴ small jobs | < 10 min, < 2 GB peak RSS |
| Load `.msl` and start job | < 100 ms |
| KMC throughput (ceria 10×10×10, 1 transition set) | ≥ 80 % of original C solver per core |
| MMC throughput (same) | ≥ 80 % of original C solver per core |
| Memory (20×20×20, 63 particles) | < 2 GB |
| GUI cold start | < 2 s |
| Open/save typical project | < 500 ms |
| 3D viewport (10⁵ atoms) | ≥ 30 FPS on integrated GPU |

Benchmarks:

- `benches/energy.rs`, `benches/kmc.rs`, `benches/mmc.rs`, `benches/context.rs` with Criterion.
- `Mocassin.Benchmarks` with BenchmarkDotNet for compilation, serialization, and `.msl` writes.
- A reproducible benchmark package in `tests/reference/bench/` with fixed seeds and inputs.

## 8. Code quality

- Nullable reference types enabled; warnings as errors.
- `TreatWarningsAsErrors`, analyzers, and `.editorconfig` for both C# and Rust.
- Public APIs documented with XML comments; `GenerateDocumentationFile`.
- Rust: `#![forbid(unsafe_op_in_unsafe_fn)]`, `#![deny(missing_docs)]` on public crates, `clippy::pedantic`
  selectively.
- No `unsafe` in safe parser code; any `unsafe` block has a `// SAFETY:` comment and a test.
- Dependency policy: prefer BCL/std; new dependencies require justification in the PR and a licence check.
- Every bug fix adds a regression test.
- Coverage target: ≥ 70 % line coverage for `Mocassin.Core`, `Symmetry`, `Compilation`, `Packages`, and
  `mocassin-solver`; coverage is a signal, not a gate for UI code.

## 9. Documentation

- **Narrative documentation** uses **Zensical** with `docs_dir = "docs"`. `docs/planning/` is excluded from
  the published site via `exclude_docs`.
- Required standalone sections:
  - user guide (including installation, model building, building/running, results, visualization, MMCFE);
  - physics explanation (MMC/KMC, energies, symmetry, transport, tracking, units);
  - developer architecture guide (code structure, design choices, data flow, testing, release);
  - tutorials (ceria KMC/MMC, BaZrO₃, vehicle, parameter sweep, HPC, analysis);
  - CLI + formats reference, diagnostics, troubleshooting/FAQ, performance/HPC, contributing, migration,
    glossary/notation/units, examples/publications.
- **API references** are generated, not hand-written:
  - .NET via **DocFX** under `/api/dotnet/`;
  - Rust via **rustdoc** (`cargo doc`) under `/api/rust/`;
  - Python via **mkdocstrings** under `/api/python/`;
  - `docs/api/index.md` is the landing page and cross-links to all three.
- **One published site.** Zensical owns the root; API outputs are copied into `site/api/dotnet` and
  `site/api/rust` after the Zensical build.
- **Strict builds.** `zensical build --strict` fails on broken links/anchors; DocFX, rustdoc, and mkdocstrings
  fail on missing public API docs/docstrings/import errors. CI builds the combined site on every PR.
- **Docs-as-code.** Step 00 creates the Zensical skeleton, navigation, build scripts, and Docs CI job. Every
  feature step updates its pages and keeps the strict build green. Step 30 completes coverage, cross-cutting
  content, and API integration; Step 31 packages the site artifact with the release.
- **Tutorials are executable.** CI runs the documented commands against `samples/` and validates expected
  outputs.
- Each step page is updated to `Done` with links to the PRs and any deviations.
- ADRs record decisions that affect formats, public APIs, or scientific semantics.
- In-app help links point to stable documentation anchors.
- Planning docs remain internal; they are not part of the published user/developer site.

## 10. Release gates

A release is blocked if any of the following is true:

- A parity matrix item is marked required but not implemented.
- Statistical parity fails for a reference system.
- A `.msl` or `.mcs` contract test fails on any OS.
- Fuzzing finds a crash, OOM, or unbounded allocation.
- A performance benchmark regresses by more than 10 % without an accepted ADR.
- A GUI workflow in the release checklist fails.
- Licence/attribution files are missing.
- The Zensical/DocFX/rustdoc/mkdocstrings documentation build fails or has broken links.
- The Python package fails to build/install, or its pytest suite fails.
