# MOCASSIN — Modern Reimplementation

A modern, cross-platform reimplementation of **MOCASSIN** (*Monte Carlo for Solid State Ionics*), the
Markov-chain Monte Carlo suite for defect thermodynamics and transport in crystalline solids originally
developed by Sebastian Eisele and Steffen Neitzel-Grieshammer at Forschungszentrum Jülich / RWTH Aachen.

> **Status: planning.** This repository currently contains the reimplementation plan. No production code
> has been written yet.

## Target stack

| Component | Technology |
| --- | --- |
| Model building system | C# / .NET 10, platform-portable class libraries and CLI |
| Desktop GUI | C# / Avalonia UI, MVVM, cross-platform (Windows, Linux, macOS) |
| Solver | Rust, native cross-platform binaries |
| Shared contracts | Versioned JSON project files, SQLite simulation packages, MessagePack state files |

## Goals

- Preserve the scientific model and physics of the original: MMC/KMC, space-group symmetry reduction,
  pair/cluster/site energies, doping and supercell generation, movement tracking, and result evaluation.
- Replace the Windows-only WPF/DirectX GUI with a portable Avalonia application.
- Replace the hand-written C solver with a safe, fast, testable Rust implementation.
- Modernize the model-building API without carrying forward legacy file formats or unmanaged struct blobs.
- Provide statistical parity with the original solver, validated against reference simulations.

## Repository layout (target)

```text
docs/planning/        Reimplementation plan, architecture, data contracts, step pages, ADRs
src/dotnet/           .NET solution: core model, symmetry, compilation, jobs, results, CLI, Avalonia app
src/rust/             Rust workspace: solver, model/package reader, RNG, trackers, state writer, CLI
samples/              Small example projects and reference inputs
tests/                Cross-language integration and statistical validation assets
```

## Documentation

Start at [`docs/planning/README.md`](docs/planning/README.md) for the roadmap and reading order.

## Lineage, licence, and citation

This project is an independent reimplementation of
[`seb-eis/mocassin`](https://github.com/seb-eis/mocassin). The new code and documentation are licensed
under the **MIT License**; the original project remains under GPL-3.0-or-later and is attributed in
[`NOTICE.md`](NOTICE.md). See [`LICENSE`](LICENSE) for the new license.

If you use MOCASSIN in scientific work, the original authors request that you cite:

> S. Eisele and S. Neitzel-Grieshammer, *J. Comput. Chem.* **2021**, 42, 737–748.
> <https://doi.org/10.1002/jcc.26418>

The reimplementation targets the model concepts and numerical behaviour of the original, not its source
code. Legacy `.mocprj`, `.msl`, and `.mcs` files are intentionally **not** supported; the new formats are
versioned and documented in [`docs/planning/02-data-contracts.md`](docs/planning/02-data-contracts.md).
