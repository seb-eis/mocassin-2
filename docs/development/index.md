# Development

Developer documentation for the reimplementation.

## Architecture

The repository separates concerns into three languages and one set of contracts:

```mermaid
flowchart LR
  Symmetry --> Core
  Compilation --> Symmetry
  Contracts --> Compilation
  Packages --> Contracts
  Results --> Contracts
  Cli --> Packages
  Cli --> Results
  App[App + ViewModels + Views] --> Contracts
  App --> Packages
  App --> Results
  App --> Rendering
```

Arrows point from a project to the project it depends on.

- .NET owns the model builder, compilation, package writer, results API, CLI, and Avalonia application.
- Rust owns the solver, `.msl` reader, RNG, state writer, and solver CLI.
- Python owns the read-only analysis package.
- `contracts/manifest.json` is the machine-readable source for shared fields, types, and versions.

The detailed architecture is in the internal plan; the repository layout is summarized in
[Reference → Repository and file layout](../reference/file-layout.md).

## Guides

- [Architecture](architecture.md) — immutable domain kernel, dependency direction, project store/edit split.
- [Code structure](code-structure.md) — folder ownership, file naming, limits, and the structure test.
- [Design decisions](design-decisions.md) — stable IDs, explicit units, canonical hashing, locking, and schema ownership.
- [Compilation pipeline](compilation-pipeline.md) — symmetry, transitions, energy, lattice/simulation input, and how
  Step 10/11 consume it.
- [Contributing](contributing.md) — branches, review gates, local quality gates, tests, and docs-as-code.
- [Agent workflow](agent-workflow.md) — context handoff, prompts, evidence reports, and diff review.
- [Testing and parity](testing-and-parity.md) — testing trophy, mocks/fakes policy, fixtures, and test tiers.

## Current phase

Steps 00–02 are complete: the build/test/docs skeleton, the immutable project kernel and `.mocprj` format, and
the symmetry/geometry kernels. Step 03 — Particle and Structure Models is next. Scientific model domains, the
solver, and the GUI are added in later steps. Do not treat placeholder pages as implemented features.
