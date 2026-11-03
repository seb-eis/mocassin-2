# MOCASSIN — Modern Reimplementation

A modern, cross-platform reimplementation of **MOCASSIN** (*Monte Carlo for Solid State Ionics*), the
Markov-chain Monte Carlo suite for defect thermodynamics and transport in crystalline solids originally
developed by Sebastian Eisele and Steffen Neitzel-Grieshammer at Forschungszentrum Jülich / RWTH Aachen.

> **Status: Step 00 in progress.** The planning phase and agent workflow are accepted. The repository,
> solution/workspace, CI, contracts, and documentation skeleton are being created. Scientific code starts at
> Step 01.

## Target stack

| Component | Technology |
| --- | --- |
| Model building system | C# / .NET 10, platform-portable class libraries and CLI |
| Desktop GUI | C# / Avalonia UI, MVVM, cross-platform (Windows, Linux, macOS) |
| Solver | Rust, native cross-platform binaries |
| Shared contracts | Versioned JSON project files, immutable SQLite packages, SQLite results stores, MessagePack state files |
| Python analysis | `mocassin-analysis` read-only `.mcs`/`.msr` reader + basic NumPy evaluations, pytest |
| Documentation | Zensical narrative site + DocFX (.NET API) + rustdoc (Rust API) + mkdocstrings (Python API) |

## Goals

- Preserve the scientific model and physics of the original: MMC/KMC, space-group symmetry reduction,
  pair/cluster/site energies, doping and supercell generation, movement tracking, and result evaluation.
- Replace the Windows-only WPF/DirectX GUI with a portable Avalonia application.
- Replace the hand-written C solver with a safe, fast, testable Rust implementation.
- Modernize the model-building API without carrying forward legacy file formats or unmanaged struct blobs.
- Provide statistical parity with the original solver, validated against reference simulations.

## Prerequisites

The repository is developed and validated inside the dedicated image
[`docs/opencode/mocassin.containerfile`](docs/opencode/mocassin.containerfile). The same tools can be installed
directly on a workstation; opencode is not required.

| Tool | Required version | Used for |
| --- | --- | --- |
| .NET SDK | `10.0.403` (pinned in `global.json`) | Libraries, CLI, Avalonia app, tests |
| Rust stable | `1.99.1` with `rustfmt` and `clippy` | Solver workspace |
| Python | `3.11`–`3.13` | Python package, docs, contract tooling |
| Zensical | `0.0.74` (pinned in `tools/docs/requirements.txt`) | Narrative documentation |
| DocFX | `2.81.0` (pinned in `.config/dotnet-tools.json`) | .NET API documentation |
| `cargo-deny` | `0.20.2` (configuration in `deny.toml`) | Rust licence/advisory checks |

Optional later-phase tools (`cargo-llvm-cov`, `cargo-fuzz`, `cargo-nextest`, `miri`) are listed in
[`docs/planning/06-toolchain-requirements.md`](docs/planning/06-toolchain-requirements.md).

Build the development image only if you do not already have the tools:

```sh
# In a checkout of https://github.com/seb-eis/opencode-container:
make base

# In this repository:
podman build -f docs/opencode/mocassin.containerfile -t opencode:mocassin .
```

## Build and test

Run commands from the repository root.

```sh
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

# Python package and tests
python -m pytest src/python/tests
python -m build src/python

# Contract manifest drift check
python tools/contracts/generate.py --check

# Documentation (narrative site, then generated APIs where available)
zensical build --strict
./tools/docs/build-docs.sh      # bash
./tools/docs/build-docs.ps1     # PowerShell
```

Local preview of the documentation site:

```sh
./tools/docs/serve.sh
# or
./tools/docs/serve.ps1
```

The exact end-to-end CLI commands become available as the corresponding steps land; they are documented in
[`docs/planning/02-data-contracts.md`](docs/planning/02-data-contracts.md) §5 and
[`AGENTS.md`](AGENTS.md) §6.

## Repository layout

```text
docs/                 Standalone Zensical site; docs/planning/ is excluded from publication
src/dotnet/           .NET solution: core model, symmetry, compilation, contracts, packages, results, CLI, app
src/rust/             Rust workspace: solver, package reader, RNG, state, extension, CLI
src/python/           `mocassin-analysis`: read-only .mcs/.msr reader + basic Python evaluations
samples/              Small example projects and reference inputs
tests/                Cross-language integration and statistical validation assets
contracts/            Machine-readable package/results/state contract manifest + generated checks
tools/                Contract generation, documentation build, API config, HPC helpers
zensical.toml         Zensical site configuration
```

## Documentation

Start at [`docs/planning/README.md`](docs/planning/README.md) for the roadmap and reading order. The agent
workflow playbook is [`docs/development/agent-workflow.md`](docs/development/agent-workflow.md). The
standalone user/developer documentation site is set up in
[`step 00`](docs/planning/steps/step-00-repository-toolchain-and-ci.md) and completed in
[`step 30`](docs/planning/steps/step-30-standalone-documentation.md).

Contributor expectations are in [`CONTRIBUTING.md`](CONTRIBUTING.md) and
[`docs/development/contributing.md`](docs/development/contributing.md).

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
