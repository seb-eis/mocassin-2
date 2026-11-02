# MOCASSIN Reimplementation Plan

This directory is the working plan for rebuilding MOCASSIN on a modern, cross-platform stack:

- **.NET 10 / C#** for the model-building system, simulation-package builder, result evaluation, and CLI.
- **Avalonia UI / C# MVVM** for the desktop application.
- **Rust** for the Monte Carlo solver.

The plan is deliberately split into small, independently reviewable steps. Each step page states the
original behaviour to preserve, the target design, concrete tasks, acceptance criteria, tests, and risks.

## Reading order

1. [`00-scope-and-principles.md`](00-scope-and-principles.md) — what is being rebuilt, what is explicitly
   out of scope, and the decisions that constrain the design.
2. [`01-target-architecture.md`](01-target-architecture.md) — repository layout, project boundaries,
   runtime architecture, and technology choices.
3. [`02-data-contracts.md`](02-data-contracts.md) — project JSON, simulation package (`.msl`), binary
   state (`.mcs`), and CLI contracts shared by .NET and Rust.
4. [`03-quality-strategy.md`](03-quality-strategy.md) — unit/property/golden/statistical testing, CI,
   determinism, and performance targets.
5. [`04-roadmap.md`](04-roadmap.md) — phases, milestones, dependency graph, estimates, and parallel work.
6. Step pages [`steps/`](steps/) — the implementation sequence.
7. [`appendix-a-original-inventory.md`](appendix-a-original-inventory.md) — inventory of the original
   solution and its mapping to new modules.
8. [`appendix-b-parity-matrix.md`](appendix-b-parity-matrix.md) — feature-by-feature parity checklist.
9. [`adr/`](adr/) — architecture decision records for choices that are expensive to reverse.

## Decisions already made

| Decision | Choice |
| --- | --- |
| Legacy file compatibility | **Clean break.** New project, package, and state formats. No `.mocprj`/`.msl`/`.mcs` import or export. |
| Delivery scope | **Phased full parity.** A usable KMC/MMC core and model editors first; 3D viewer, MMCFE, advanced evaluation and HPC tooling follow. |
| Solver numerical parity | **Statistical parity.** Same physics, validated within statistical error; new deterministic RNG; not bit-exact trajectories. |
| GUI direction | **Redesign, preserve concepts.** Keep the scientific workflow and model semantics; modernize navigation, editors, validation, and visualization. |
| Licence | **MIT** for the new reimplementation; the original project remains GPL-3.0-or-later. |

Additional technical decisions (clean-break format encoding, Avalonia MVVM toolkit, Rust solver boundary,
3D renderer) are recorded as ADRs in [`adr/`](adr/).

## Step index

| Phase | Step | Outcome |
| --- | --- | --- |
| A. Foundations | [00](steps/step-00-repository-toolchain-and-ci.md) | Repository, solution/workspace, CI, ADRs, docs skeleton, sample fixtures |
| A. Foundations | [01](steps/step-01-domain-kernel-and-project-format.md) | Domain kernel, IDs, diagnostics, versioned project JSON |
| A. Foundations | [02](steps/step-02-symmetry-and-geometry.md) | Space groups, coordinate systems, symmetry reduction |
| B. Model | [03](steps/step-03-particle-and-structure-models.md) | Particle and crystal-structure models |
| B. Model | [04](steps/step-04-transition-model.md) | State changes, groups, chains, KMC/MMC transitions |
| B. Model | [05](steps/step-05-energy-model.md) | Environments, pair/cluster interactions, site energies |
| B. Model | [06](steps/step-06-lattice-and-simulation-models.md) | Doping/lattice model and simulation definitions |
| B. Model | [07](steps/step-07-validation-and-diagnostics.md) | Full model validation and diagnostic UX |
| C. Compilation | [08](steps/step-08-model-compilation-pipeline.md) | P1 extension, environments, interaction and rule generation |
| C. Compilation | [09](steps/step-09-parameterization-templates.md) | Parameterization templates and model signatures |
| C. Compilation | [10](steps/step-10-job-templates-and-supercells.md) | Job templates, inheritance, supercell/doping generation |
| C. Compilation | [11](steps/step-11-simulation-package-and-msl-writer.md) | Compiled package encoding and `.msl` v2 writer |
| C. Compilation | [12](steps/step-12-build-orchestration-and-cli.md) | Headless build orchestration and `mocassin` CLI |
| D. Solver | [13](steps/step-13-rust-solver-foundation-and-package-reader.md) | Rust workspace, CLI, `.msl` reader, job model |
| D. Solver | [14](steps/step-14-simulation-context-initialization.md) | Lattice, neighbor maps, energy background, jump status |
| D. Solver | [15](steps/step-15-energy-engine.md) | Pair/cluster/site energy engine and field terms |
| D. Solver | [16](steps/step-16-kmc-engine.md) | Rejection KMC, normalization, time evolution |
| D. Solver | [17](steps/step-17-mmc-engine.md) | Metropolis MMC and equilibrium breaking |
| D. Solver | [18](steps/step-18-tracking-statistics-and-state-writer.md) | Trackers, histograms, statistics, `.mcs` writer |
| D. Solver | [19](steps/step-19-solver-cli-hpc-and-extensions.md) | Production CLI, HPC helpers, extension ABI |
| E. Results | [20](steps/step-20-state-reader-and-evaluation-api.md) | `.mcs` reader and evaluation API |
| E. Results | [21](steps/step-21-analysis-plots-and-reports.md) | Analysis queries, plots, CSV/JSON reports |
| F. GUI | [22](steps/step-22-avalonia-shell-and-project-lifecycle.md) | Avalonia shell, navigation, project lifecycle, undo/redo |
| F. GUI | [23](steps/step-23-model-editors-particle-structure-transitions.md) | Particle, structure, and transition editors |
| F. GUI | [24](steps/step-24-model-editors-energy-lattice-simulations.md) | Energy, lattice, and simulation editors |
| F. GUI | [25](steps/step-25-parameterization-job-templates-and-build-ui.md) | Parameterization, job templates, build/deploy UI |
| F. GUI | [26](steps/step-26-3d-viewer-and-visualization.md) | Cross-platform 3D viewer and overlays |
| F. GUI | [27](steps/step-27-run-monitoring-and-mmcfe-ui.md) | Live run monitoring, charts, MMCFE workflow |
| G. Release | [28](steps/step-28-validation-parity-and-performance.md) | Statistical parity, performance, fuzzing, hardening |
| G. Release | [29](steps/step-29-standalone-documentation.md) | Documentation completion: coverage, cross-cutting pages, DocFX + rustdoc APIs, publish |
| G. Release | [30](steps/step-30-packaging-and-release.md) | Packaging, docs artifact, samples, release engineering |

## Working conventions

- Every step has an explicit **Done when** section. A step is not complete because code exists; its
  acceptance criteria and tests must pass.
- Contracts in [`02-data-contracts.md`](02-data-contracts.md) change only through an ADR and a schema
  version bump.
- Scientific behaviour is specified in prose and tests before implementation. When the original behaviour
  is ambiguous, document the chosen interpretation in the step page.
- Every step page has a **Documentation** section naming the pages to write at that point. “Documentation
  written” is part of every acceptance checklist.
- No production feature is added directly to the Avalonia app if it belongs in a library; the app is a
  consumer of the same public API as the CLI.
- The Rust solver never reads project JSON. Its only input is the compiled simulation package plus CLI
  arguments, so it can be developed and tested independently.
- Standalone documentation is built with Zensical; .NET and Rust API references are generated with DocFX and
  rustdoc into `/api/dotnet/` and `/api/rust/`. Step 00 creates the skeleton, every feature step writes its
  pages, Step 29 completes the site, and Step 30 packages it. `docs/planning/` is excluded from publication.

## Status

| Area | Status |
| --- | --- |
| Planning | Complete (v1 draft) |
| .NET model builder | Not started |
| Rust solver | Not started |
| Avalonia GUI | Not started |
| Parity validation | Not started |
