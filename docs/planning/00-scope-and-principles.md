# 00 — Scope, Principles, and Success Criteria

## 1. Purpose

Rebuild MOCASSIN as a maintained, cross-platform scientific application without carrying forward the
constraints of the 2018–2021 codebase: .NET Standard 2.0 libraries, a .NET Framework 4.8 WPF/DirectX
GUI, unmanaged C structs exposed through EF Core blobs, and Windows-only distribution.

The reimplementation must remain scientifically faithful. Users should be able to define the same models,
run the same MMC/KMC physics, obtain equivalent observables, and analyse results with the same conceptual
workflow. The implementation, architecture, file formats, and user interface may differ.

## 2. Product definition

MOCASSIN is a fixed-lattice Markov-chain Monte Carlo suite for crystalline solids. A model consists of:

- **Particles and particle sets** — species, charge states, vacancies, and allowed occupations.
- **Crystal structure** — space group, cell parameters, stable/unstable reference sites.
- **Transitions** — state changes, groups, chains, and symmetry-bound MMC/KMC events.
- **Energy model** — cutoff environments, pair interactions, multi-body clusters, site energies.
- **Lattice/doping model** — occupation exchanges and ordered dopings for supercell generation.
- **Simulation model** — MMC or KMC definitions with active transitions and default settings.
- **Parameterization** — numerical energies and attempt frequencies for the compiled model.
- **Job templates** — collections/configurations that expand into thousands of simulation jobs.
- **Simulation package** — compiled, indexed data consumed by the solver.
- **Results** — MessagePack state, statistics, trackers, histograms, and evaluation API.
- **Desktop GUI** — the primary authoring, validation, build, run, and analysis environment.

## 3. Decisions and constraints

### 3.1 Recorded decisions

| Topic | Decision | Consequence |
| --- | --- | --- |
| Legacy files | Clean break | No reader/writer for old `.mocprj`, `.msl`, `.mcs`; new formats are versioned from day one. |
| Delivery | Phased full parity | Core KMC/MMC and editors first; viewer, MMCFE, advanced analytics and HPC later. |
| Numerical parity | Statistical parity | New deterministic RNG; validate observables within statistical error, not identical trajectories. |
| GUI | Redesign, preserve concepts | New information architecture and editors; do not port WPF screens one-to-one. |
| Licence | MIT | New code and docs are MIT; the original project remains GPL-3.0-or-later and is attributed in NOTICE. |

### 3.2 Technical constraints

- **.NET 10** is the minimum runtime for all managed components. No .NET Framework, no `netstandard2.0`.
- **Avalonia UI** must run on Windows 10/11, mainstream Linux desktops, and macOS (Apple Silicon and Intel).
- **Rust** solver must build on the same three platforms with stable Rust and no platform-specific
  toolchain requirement beyond a C linker for the bundled SQLite build.
- **64-bit only.** The original supported x86_64 only; the new solver targets `x86_64` and `aarch64`.
- **Offline-first.** The application must work without network access. No telemetry, no cloud dependency.
- **Scientific reproducibility.** Same project, package, solver version, and seed must reproduce the same
  state on the same platform. Cross-platform results must agree statistically, not bit-for-bit.
- **Large jobs.** The design target is a 20×20×20 supercell with up to 63 particles and thousands of jobs
  per package, on a workstation or HPC node.

## 4. Goals

### 4.1 Scientific goals

1. Preserve the unified MMC/KMC model: fixed positions, space-group symmetry, Symmetry-reduced model
   definition, and P1-extended simulation data.
2. Support all original transition mechanisms: 2-site Metropolis, 3-site migration, 5-/7-site
   interstitialcy, 5-/6-site vehicle, and charge-transfer/polaron hops.
3. Support pair interactions, user-defined multi-body clusters (up to 9 positions), site energies, stable
   and unstable environments, cutoff filters, and external electric fields.
4. Preserve supercell generation semantics: default occupations, ordered dopings, integer truncation,
   dependent exchanges and charge compensation.
5. Preserve tracking semantics: global/transition-based, mobile/particle-based, static/position-based,
   jump histograms, conductivity, mobility, diffusion, and Onsager correlations.
6. Provide statistically equivalent results to the original reference implementation on canonical
   systems (ceria, doped barium zirconate, and at least one vehicle/interstitialcy case).

### 4.2 Engineering goals

1. One codebase, three platforms, reproducible builds, and CI for every supported OS.
2. A public, documented .NET API for model building, validation, package building, and evaluation.
3. A headless CLI that can do everything the GUI can do (except interactive visualization).
4. A Rust solver with safe abstractions at the boundaries, no undefined behaviour in safe code, and
   explicit, documented `unsafe` only where measurement proves it necessary.
5. Versioned, documented data contracts with golden-file and cross-language tests.
6. Actionable diagnostics instead of exceptions or silent corrections.
7. Performance within 20 % of the original C solver per core, with a path to exceed it.

### 4.3 Usability goals

1. A new user can create, validate, parameterize, build, run, and analyse the ceria sample without
   reading source code.
2. Every model object is discoverable through the project tree and editable in a detail panel.
3. Validation errors link to the offending object and explain how to fix them.
4. Long-running builds and simulations report progress, can be cancelled, and leave useful logs.
5. The 3D viewer makes structure, transitions, clusters, and parameterization visible and selectable.

## 5. Non-goals

The following are explicitly out of scope for the first parity release:

- Reading or writing legacy `.mocprj`, `.msl`, or `.mcs` files.
- Bit-exact reproduction of original trajectories or RNG streams.
- 32-bit platforms.
- A web or mobile client.
- A cloud service, account system, or telemetry.
- Molecular dynamics, NEB, DFT, or force-field calculation. MOCASSIN consumes energies; it does not
  compute them.
- A general-purpose crystal structure editor or CIF import/export. The model is an idealized fixed-position
  lattice, not a relaxed structure.
- Arbitrary user scripts/plugins executing inside the solver by default. Extensions are an explicit,
  documented ABI with a trust boundary.
- Windows installer/MSI parity. New distribution uses self-contained archives and platform packages.

## 6. Functional scope

The parity target is the feature set documented in the original user guide and implemented by the original
solution. The inventory is maintained in
[`appendix-a-original-inventory.md`](appendix-a-original-inventory.md), and the release checklist in
[`appendix-b-parity-matrix.md`](appendix-b-parity-matrix.md).

### 6.1 Model building

- Particle definitions (symbol, charge, vacancy flag) and particle sets.
- Space group selection, cell parameters with crystal-system constraints, stable/unstable sites.
- State exchanges, exchange groups, abstract chains with dynamic/static linkers, KMC/MMC transitions bound
  to reference paths.
- Stable/unstable cutoff environments and sublattice filters.
- Group interaction clusters with up to 9 positions and symmetry-reduced permutations.
- Site energies.
- Occupation exchanges, ordered dopings, and charge-compensating dependent exchanges.
- MMC and KMC simulation definitions with active transition sets and default settings.
- Validation with errors/warnings and model statistics.

### 6.2 Compilation and package building

- P1 symmetry extension and deterministic index assignment.
- Environment sampling, pair interaction discovery, cluster permutation generation.
- Transition rule generation with mass/charge conservation and forward/backward rules.
- Symmetry-reduced parameterization templates and attempt-frequency editing.
- Job template collections/configurations, inheritance/overrides, selection optimizers, duplicate jobs.
- Doping-aware supercell generation and charge balancing.
- Compiled package encoding and `.msl` writing.
- Build preview (job count, lattice sizes, warnings) and cancellation.

### 6.3 Solver

- MMC and rejection KMC in a unified engine.
- KMC pre-run, automatic and fixed normalization, time-step calculation.
- MMC equilibrium break detection.
- Electric-field influence, pair/cluster/site energies, fast exponential option.
- Trackers, histograms, statistics, progress output, abort conditions.
- Deterministic seeding, duplicate jobs with independent streams.
- Extension hook for custom routines (MMCFE parity).

### 6.4 Evaluation and analysis

- Read `.mcs` state files and `.msl` result tables.
- Particle counts/densities, lattice metadata.
- Ensemble and transition mobility/conductivity.
- Displacement, mean square displacement, diffusion coefficients.
- Static tracker flow fields, mobile tracker displacements.
- Ensemble correlations/Onsager coefficients.
- Coordination analysis and jump statistics.
- Export to CSV/JSON and plot-ready series.
- Batch evaluation across job sets with filtering.

### 6.5 Desktop GUI

- Project lifecycle (new/open/save/recent/autosave/undo-redo).
- Editors for every model domain.
- Live validation and model statistics.
- Parameterization and job-template editors.
- Build, deploy, run, monitor, cancel, collect.
- 3D structure/transition/cluster/parameterization viewer.
- Charts: energy traces, jump histograms, Arrhenius plots, conductivity/diffusion summaries.
- MMCFE configuration and result view.

### 6.6 HPC and automation

- `mocassin` CLI for validation, package building, collection, evaluation, and export.
- `mocassin-sim` solver CLI with job ranges and deterministic seeds.
- SLURM/PBS-friendly job templates and a process-per-job runner.
- Result collector that writes into `.msl` atomically.
- Exit codes and machine-readable JSON logs.

## 7. Quality attributes

| Attribute | Target |
| --- | --- |
| Correctness | Physics tests, statistical parity on reference systems, no silent model corrections |
| Performance | Solver within 20 % of original C per core; package build ≥ 1 job/s on a laptop for small models |
| Scalability | 20×20×20 lattice, 63 particles, 10⁴ jobs per package, 10⁸ cycles per job |
| Reliability | Atomic writes, resumable collection, crash-safe job status, no corrupt `.msl` |
| Portability | Windows 10/11, Ubuntu 22.04+/Fedora, macOS 13+ (arm64/x64) |
| Maintainability | Layered libraries, no UI logic in domain, ≥ 70 % line coverage on core libraries |
| Usability | All validation errors navigable; build/run cancellable; no blocking UI |
| Security | Untrusted project/package parsing is bounded and fuzzed; extensions opt-in |
| Accessibility | Keyboard navigation, scalable UI, high-contrast theme, screen-reader labels |

## 8. Success criteria for the first parity release

1. The ceria user-guide project can be recreated from scratch and produces a valid package.
2. KMC and MMC reference jobs run to completion on Windows, Linux, and macOS.
3. Conductivity, diffusion, and energy results match the original solver within the documented statistical
   tolerance on at least three reference systems.
4. Every model object in the original GUI has a functional editor in the new GUI.
5. The 3D viewer displays the unit cell/supercell, particles, transitions, clusters, and parameterization
   values, with selection synchronized to the project tree.
6. A 10⁴-job package can be built, deployed to SLURM, run, collected, and evaluated without manual SQL.
7. A new contributor can build and test the repository using only the README and `docs/`.

## 9. Major risks and mitigations

| Risk | Impact | Mitigation |
| --- | --- | --- |
| Scientific semantics are subtle and underdocumented | Wrong results that look plausible | Derive behaviour from source + docs; encode invariants in tests; statistical parity suite |
| Symmetry reduction is complex | Invalid P1 data or missed interactions | Property tests against brute-force expansion; golden tests for all 230 groups |
| Rust rewrite diverges from C | Non-comparable results | Reference observables, per-module validation, side-by-side runs |
| 3D viewer scope expands | Delays GUI | Renderer spike in step 26; start with instanced spheres/bonds and picking; defer advanced shading |
| Format churn | Wasted migration work | Freeze contracts early; schema versions; golden round-trip tests |
| Performance regressions | Unusable large jobs | Benchmarks from step 13; profile before optimizing; data-oriented layout |
| Single-developer bandwidth | Long schedule | Parallel tracks (model, solver, UI) with contract-first boundaries; MVP milestones |
