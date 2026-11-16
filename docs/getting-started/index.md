# Getting Started

## Prerequisites

| Tool | Version | Purpose |
| --- | --- | --- |
| .NET SDK | `11.0.100` | Libraries, CLI, Avalonia application, tests |
| Rust stable | `1.99.1` | Solver workspace |
| Python | `3.11`–`3.13` | Python package, documentation, contract tooling |
| Zensical | `0.0.74` | This documentation site |
| DocFX | `2.81.0` | .NET API reference |

The reproducible development image is defined in `docs/opencode/mocassin.containerfile` in the repository.
The same tools can be installed directly on Windows, Linux, or macOS.

## Build and test

```sh
dotnet restore Mocassin.slnx
dotnet build Mocassin.slnx -c Release
dotnet test Mocassin.slnx -c Release

cargo test --manifest-path src/rust/Cargo.toml --workspace
python -m pytest src/python/tests
```

## Build the documentation

```sh
zensical build --strict
./tools/docs/build-docs.sh      # bash
./tools/docs/build-docs.ps1     # PowerShell
```

The combined build copies generated DocFX and rustdoc output into `site/api/dotnet` and `site/api/rust` when
those projects and crates exist.

## Create, validate, and build

The headless CLI is the quickest way to prove the toolchain:

```sh
# Create a blank project, or use --template ceria for the ceria model starter.
mocassin new my-project -o work

# Validate the project read-only.
mocassin validate work/my-project.mocprj

# Build a real package from the ceria job sample and verify it.
mocassin build samples/jobs/ceria-temperature-sweep.mocprj -o /tmp/ceria.msl
mocassin verify /tmp/ceria.msl
mocassin inspect /tmp/ceria.msl
```

`--jobs 1-3` builds a stable subset of the full expansion; `--preview` reports the job, lattice, and doping
counts without writing. The full command surface, JSON reports, and exit codes are in the
[CLI reference](../reference/cli.md); the workflow is in [Building a package](../user-guide/building.md).

## Next steps

- Read the [user guide](../user-guide/index.md) for the scientific workflow.
- Read the [development guide](../development/index.md) before contributing.
- Try the [ceria tutorial](../tutorials/index.md) once the model and solver steps are available.
