# 07 — Reference Build and Data Generation

This page records how to build the **original MOCASSIN solver and model-builder libraries** to generate
reference data for statistical parity. It is a verified recipe for the archived repository, not a
production build of the original project.

- Original repository: `https://github.com/seb-eis/mocassin`
- Verified commit: `876dcc9e6928e1af0dbea64c92e7e17bc6dcca70` (2022-12-13)
- Local reference clone: `/workspace/mocassin-original` (read-only; do not modify)
- Work copy and outputs: outside the original clone, e.g. `/tmp/opencode/mocassin-reference`

The original clone must remain untouched. All recipes copy it to a work directory first.

## 1. Components

The original solution has three parts:

| Component | Path | Build |
| --- | --- | --- |
| Monte Carlo solver (C11) | `src/McSolver` | CMake + GNU GCC |
| Model builder API (.NET Standard 2.0) | `src/ModelBuilder` | .NET SDK |
| GUI (WPF, .NET Framework 4.8) | `src/ModelBuilder/Mocassin.UI.GUI` | Not built; Windows-only |

Only the solver and model-builder libraries are needed for reference data.

## 2. Prerequisites

The MOCASSIN development image provides everything:

- GCC 14+, CMake, Make
- .NET SDK 10.0.x
- `sqlite3`, `rsync`, `git`

## 3. Build the solver

### Verified command

```sh
cp -a /workspace/mocassin-original /tmp/opencode/mocassin-reference
cd /tmp/opencode/mocassin-reference/src/McSolver
cmake -S . -B build -DCMAKE_BUILD_TYPE=Release \
  -DCMAKE_C_FLAGS="-Wno-error=incompatible-pointer-types"
cmake --build ./build --config Release -j"$(nproc)"
```

Or use [`tools/reference/build-solver.sh`](../../tools/reference/build-solver.sh).

### GCC 14 compatibility note

The original `CmdArgumentResolver.c` has:

```c
static struct _iobuf* freopen_utf8(...)
{
    return freopen(filename, mode, file);
}
```

On glibc, `FILE` is not `struct _iobuf`, and GCC 14 treats the incompatible pointer type as an error.
`-Wno-error=incompatible-pointer-types` is sufficient; no source change is required.

### Expected outputs

```text
src/McSolver/build/Mocassin.Simulator
src/McSolver/build/Mocassin.Utility
src/McSolver/build/libframework.so
src/McSolver/build/libsimulator.so
src/McSolver/build/libsqlite3.so
src/McSolver/build/libjobloader.so
src/McSolver/build/libprogressprint*.so
src/McSolver/build/libutility.so
src/McSolver/build/libmmcfe.mocext.so
```

Smoke check:

```sh
./build/Mocassin.Simulator --help
```

Without the required `-dbPath`, `-jobId`, and `-ioPath`, it exits with the expected argument error.

## 4. Build the model-builder libraries

Build the non-GUI chain through `Mocassin.Tools.Evaluation`:

```sh
cd /tmp/opencode/mocassin-reference/src/ModelBuilder
dotnet build Mocassin.Tools.Evaluation/Mocassin.Tools.Evaluation.csproj -c Release
```

Or use [`tools/reference/build-model-builder.sh`](../../tools/reference/build-model-builder.sh).

### Required compatibility patches

The original projects target .NET Standard 2.0 and pin old EF Core packages. EF Core 3.1.6 fails at runtime
on .NET 10 with:

```text
System.TypeInitializationException
  Microsoft.EntityFrameworkCore.EnumerableMethods
  Sequence contains more than one matching element
```

In the **work copy only**, update:

| Package | Original | Patched |
| --- | --- | --- |
| `Microsoft.EntityFrameworkCore*` | `3.1.6` | `3.1.32` |
| `SQLitePCLRaw.bundle_e_sqlite3` | `2.0.4` | `2.1.2` |

```sh
find /tmp/opencode/mocassin-reference/src/ModelBuilder -name '*.csproj' -print0 \
  | xargs -0 sed -i \
      -e 's/Version="3.1.6"/Version="3.1.32"/g' \
      -e 's/SQLitePCLRaw.bundle_e_sqlite3" Version="2.0.4"/SQLitePCLRaw.bundle_e_sqlite3" Version="2.1.2"/g'
find /tmp/opencode/mocassin-reference/src/ModelBuilder -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} +
```

The source code itself is unchanged. The original projects emit `NU1903` warnings for old
`Newtonsoft.Json`/SQLite packages; those are acceptable only for isolated reference-data generation.

### Expected outputs

```text
src/ModelBuilder/ICon.Framework.Shared/bin/Release/netstandard2.0/Mocassin.Framework.dll
src/ModelBuilder/ICon.Framework.Mathematics/bin/Release/netstandard2.0/Mocassin.Mathematics.dll
src/ModelBuilder/ICon.Framework.Symmetry/bin/Release/netstandard2.0/Mocassin.Symmetry.dll
src/ModelBuilder/ICon.Model/bin/Release/netstandard2.0/Mocassin.Model.dll
src/ModelBuilder/ICon.Model.Translator/bin/Release/netstandard2.0/Mocassin.Model.Translator.dll
src/ModelBuilder/Mocassin.Tools.UAccess/bin/Release/netstandard2.0/Mocassin.Tools.UAccess.dll
src/ModelBuilder/Mocassin.UI.Xml/bin/Release/netstandard2.0/Mocassin.UI.Data.dll
src/ModelBuilder/Mocassin.Tools.Evaluation/bin/Release/netstandard2.0/Mocassin.Tools.Evaluation.dll
```

## 5. Generate an `.msl` reference package

### The `.mocprj` format

An original `.mocprj` is a **SQLite database**, not JSON. The project payload is stored in:

```sql
SELECT ProjectJson FROM MocassinProjectGraphs LIMIT 1;
```

### Required workarounds

- **EF Core project loader:** on .NET 10, the original `MocassinProjectContext` query path is fragile even
  with EF Core 3.1.32. The verified recipe extracts `ProjectJson` with `sqlite3` and loads it with
  `MocassinProject.FromJson`.
- **Randomized model hash:** `ProjectCustomizationTemplate.Create` uses `string.GetHashCode()`, which is
  randomized per process. Before building, set the template hash to the current process value:
  ```csharp
  template.ProjectCustomizationTemplate.ModelHash = template.ProjectModelData.Json.GetHashCode();
  ```

### Verified command

```sh
sqlite3 -noheader /workspace/mocassin-original/docs/project-examples/User-Guide-Project.mocprj \
  "SELECT ProjectJson FROM MocassinProjectGraphs LIMIT 1;" \
  > /tmp/opencode/Ceria.project.json

dotnet run --project tools/reference/Mocassin.ReferenceHarness -c Release -- \
  /tmp/opencode/Ceria.project.json \
  /tmp/opencode/User-Guide-Project.msl
```

Or use [`tools/reference/build-package.sh`](../../tools/reference/build-package.sh), which performs both steps.

### Expected result

For the Ceria example:

```text
Loaded project: Ceria, builds: 2, jobsets: 2
  Build: MMC-Deploy-Template
  Build: KMC-Deploy-Template
Building template: KMC-Deploy-Template
  ...
  status: BuildProcessCompleted
Built simulation library: /tmp/opencode/User-Guide-Project.msl
File exists: True, size: ~272 KB
```

The generated `.msl` uses the original schema and contains 3 jobs.

## 6. Run the solver

```sh
mkdir -p /tmp/opencode/refrun
cd /tmp/opencode/mocassin-reference/src/McSolver
./build/Mocassin.Simulator \
  -dbPath /tmp/opencode/User-Guide-Project.msl \
  -jobId 1 \
  -ioPath /tmp/opencode/refrun
```

Or use [`tools/reference/run-solver.sh`](../../tools/reference/run-solver.sh).

Expected result:

- exit code `0`
- `run.mcs` (~693 KB for the verified run)
- `prerun.mcs` (~693 KB)
- statistics dump on stdout
- final line with `ERR_CODE=0x00000000`

The verified run did show the original `D_Sigma` `NaN` quirk for some particle species. That is an original
behaviour, not a build error; do not use it as a reference value without recording the known limitation.

## 7. Reproducibility checklist

For every generated reference dataset, record:

- original repository commit (`876dcc9e...`),
- GCC version and the compatibility flag,
- .NET SDK version,
- EF Core/SQLitePCLRaw patch versions,
- project file and project `ProjectGuid`,
- selected build template and job IDs,
- solver binary SHA-256,
- generated `.msl` SHA-256,
- output `.mcs` SHA-256 values,
- random seeds, temperatures, field, MCSP, and job counts.

Store the manifest and outputs in `tests/reference/` when the actual reference campaign starts.

## 8. Scripts

| Script | Purpose |
| --- | --- |
| [`build-solver.sh`](../../tools/reference/build-solver.sh) | Copy the original, apply the GCC 14 flag, build the solver |
| [`build-model-builder.sh`](../../tools/reference/build-model-builder.sh) | Copy, patch EF/SQLite package pins, build the non-GUI libraries |
| [`build-package.sh`](../../tools/reference/build-package.sh) | Extract `ProjectJson` and build an `.msl` with the harness |
| [`run-solver.sh`](../../tools/reference/run-solver.sh) | Run one reference job and write `run.mcs`/`prerun.mcs` |

Default work directory: `/tmp/opencode/mocassin-reference`. Override with `MOCASSIN_REFERENCE_WORK`.

## 9. Related decisions

- [ADR 0005](adr/0005-statistical-parity-and-rng.md) — statistical parity policy.
- [Step 29](steps/step-29-validation-parity-and-performance.md) — parity, fuzzing, benchmarks.
- [`03-quality-strategy.md`](03-quality-strategy.md) — reference systems and tolerances.
- `tests/reference/` — future reference fixtures and reports.
