# ADR 0002 — Rust Solver Boundary

- **Status:** Accepted
- **Date:** 2026-11-02
- **Deciders:** Project owner

## Context

The original solver is C11 linked into the GUI process only at build time; the GUI never runs simulations, it
writes a database and launches `Mocassin.Simulator` as a separate process. The C solver depends on
x86-64 native struct layouts, SQLite, and a shared-library extension ABI.

The new stack mandates Rust for the solver and .NET 10 for model building. The boundary can be:

1. A Rust library embedded in .NET via P/Invoke/C ABI.
2. A separate Rust process consuming a compiled package.
3. Rust reimplementing the whole model builder as well.

## Decision

The Rust solver is a **separate process** (`mocassin-sim`) that consumes only the compiled simulation package
(`.msl`) and CLI arguments. .NET never calls into solver code in-process for production runs. The solver is
also usable as a Rust library for tests and future embedding, but the supported product boundary is the
process plus package contract.

The .NET model builder is the only producer of `.msl`; Rust never reads project JSON. The solver writes
per-job state files and stdout; a separate `mocassin collect` command writes results back into `.msl`.

## Consequences

**Positive**

- Process isolation: a solver crash cannot corrupt the project or GUI; memory is reclaimed per job.
- Natural cancellation, timeouts, and HPC scheduling.
- No C ABI or unsafe marshalling for the core solver.
- Rust can be tested against package fixtures without .NET.
- The solver can be replaced or scaled independently.

**Negative**

- The package schema is a cross-language contract and needs strict versioning/tests.
- Startup cost per job (acceptable; jobs run for seconds to hours).
- Two implementations of the package structs (writer in .NET, reader in Rust) must stay in sync.
- Extensions need a separate, explicit ABI (see step 19).

## Alternatives considered

- **In-process P/Invoke:** faster startup and shared memory, but unsafe boundary, complex error handling,
  difficult cancellation, and no isolation. Rejected for the core; may be offered later for interactive
  previews.
- **Rust model builder:** would reduce duplicate structs, but conflicts with the mandated .NET model-building
  stack and loses the mature .NET UI/API ecosystem.

## Rules

- The `.msl` format is versioned; the solver refuses incompatible versions.
- The solver never writes to `.msl`.
- The solver's stdout is the human statistics dump; structured logs go to stderr.
- Any future in-process embedding must go through a new ADR and a stable FFI layer.
