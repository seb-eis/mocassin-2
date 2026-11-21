# Installation

MOCASSIN runs on Windows, Linux, and macOS. The desktop application and the CLI share the same .NET
libraries; the solver is a separate Rust process and is not required to inspect or edit a project.

## Prerequisites

| Tool | Version | Purpose |
| --- | --- | --- |
| .NET SDK | `11.0.100` | Libraries, CLI, Avalonia desktop application, tests |
| Rust stable | `1.99.1` | Solver workspace |
| Python | `3.11`–`3.13` | Analysis package, documentation, contract tooling |

The repository pins the .NET SDK in `global.json` and the package versions in `Directory.Packages.props`. Use
the pinned SDK for reproducible builds. The development container in
`docs/opencode/mocassin.containerfile` provides the same tools; it is optional.

## Build from source

```sh
dotnet restore Mocassin.slnx
dotnet build Mocassin.slnx -c Release

cargo build --manifest-path src/rust/Cargo.toml --workspace
```

Run the local gate before using a development build:

```sh
./tools/verify.sh all
```

## Start the desktop application

```sh
dotnet run --project src/dotnet/Mocassin.App -c Release
```

On Linux the desktop session must provide an X11 or Wayland display. The application is GPU-free; the shell,
tabs, console, settings, and read-only project documents do not require a GPU. The 3D viewer is a later step.

Useful startup options:

```sh
# Open a project immediately.
dotnet run --project src/dotnet/Mocassin.App -- --project ./samples/ceria/ceria.mocprj

# Override the application-data root for this run.
dotnet run --project src/dotnet/Mocassin.App -- --data-dir /tmp/mocassin-data

# Override the persisted log level for this run.
dotnet run --project src/dotnet/Mocassin.App -- --log-level debug
```

Unknown arguments print the usage text and exit with code `2`. A failed startup exits with code `1`; a normal
window close exits with `0`. A missing `--project` path does not prevent the shell from starting; the failure is
reported in the Application console.

## Application data

The application stores its settings, logs, cache, and reserved recovery directory under the platform
local-application-data directory:

| Platform | Default root |
| --- | --- |
| Windows | `%LOCALAPPDATA%\Mocassin` |
| Linux | `~/.local/share/Mocassin` |
| macOS | `~/Library/Application Support/Mocassin` |

`--data-dir <path>` overrides the root for one run. `MOCASSIN_DATA_DIR` provides the same override from the
environment; the command-line value wins. The layout and the settings document are documented in
[Repository and file layout](../reference/file-layout.md#application-data-layout).

## First launch checks

- The shell opens with a welcome tab and the Projects, Documents, Inspector, Console, and Status regions.
- `View → Settings…` opens the settings dialog.
- `Help → Keyboard Shortcuts` lists the registered gestures.
- The Application console shows startup warnings, including a rejected settings file.

If the window does not appear, see [Installation troubleshooting](../troubleshooting/installation.md).
