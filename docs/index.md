# MOCASSIN Documentation

MOCASSIN (*Monte Carlo for Solid State Ionics*) is a fixed-lattice MMC/KMC suite for defects and transport in
crystalline solids. This site documents the modern, cross-platform reimplementation.

!!! note "Status"
    Steps 00–02 are complete: repository/toolchain, the immutable domain kernel and `.mocprj` format, and the
    symmetry/geometry kernels. Step 03 — Particle and Structure Models is next. Scientific model domains, the
    solver, and the GUI arrive in later steps. Commands that depend on unfinished components are marked in each
    section.

## Start here

- [Getting started](getting-started/index.md) — prerequisites and the first build.
- [User guide](user-guide/index.md) — the model-building, build, run, and analysis workflow.
- [Physics](physics/index.md) — MMC/KMC, symmetry, energies, transport, and tracking.
- [Tutorials](tutorials/index.md) — worked examples.
- [Reference](reference/index.md) — CLI, file formats (including the [project format](reference/project-format.md)),
  and repository layout.
- [Development](development/index.md) — architecture, contributing, testing, and the agent workflow.
- [API](api/index.md) — generated .NET, Rust, and Python reference documentation.

## Design at a glance

```mermaid
flowchart LR
  P[Project JSON] --> C[Compiled model]
  C --> M[Immutable .msl package]
  M --> S[Rust solver]
  S --> J[Job folder: .mcs + logs]
  J --> R[.msr results store]
  R --> E[.NET and Python evaluation]
```

The editing model, compiled package, solver, and results store are separate contracts. The Rust solver
consumes only the compiled `.msl` package; it never reads project JSON. Results live in a separate `.msr`
store so the package stays immutable.

## Repository and plan

The implementation plan, ADRs, and step pages live in the repository under `docs/planning/`. They are internal
working documents and are intentionally excluded from this published site. The public entry point is the
[repository README](https://github.com/seb-eis/mocassin-2/blob/main/README.md).
