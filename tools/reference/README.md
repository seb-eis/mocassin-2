# Reference Build Tools

Scripts for building the **archived original MOCASSIN** solver/model builder and generating reference data.
The original clone is never modified; everything runs in a work copy.

Full recipe and compatibility notes: [`docs/planning/07-reference-build.md`](../../docs/planning/07-reference-build.md).

## Usage

From the MOCASSIN repository root:

```sh
# Build the original C solver
tools/reference/build-solver.sh

# Build the non-GUI model-builder libraries
tools/reference/build-model-builder.sh

# Generate an original-schema .msl from a .mocprj
tools/reference/build-package.sh \
  /workspace/mocassin-original/docs/project-examples/User-Guide-Project.mocprj \
  /tmp/opencode/User-Guide-Project.msl

# Run one job and write run.mcs/prerun.mcs
tools/reference/run-solver.sh \
  /tmp/opencode/User-Guide-Project.msl \
  1 \
  /tmp/opencode/refrun
```

Defaults:

- Original clone: `/workspace/mocassin-original`
- Work copy: `/tmp/opencode/mocassin-reference`

Override with `MOCASSIN_ORIGINAL` and `MOCASSIN_REFERENCE_WORK`.

## Scripts

| Script | Purpose |
| --- | --- |
| `build-solver.sh` | Copy the original and build the C solver with the GCC 14 compatibility flag |
| `build-model-builder.sh` | Copy, patch EF Core/SQLite package pins, build the non-GUI libraries |
| `build-package.sh` | Extract `ProjectJson` and build an `.msl` with the harness |
| `run-solver.sh` | Run one solver job and write `run.mcs`/`prerun.mcs` |

`Mocassin.ReferenceHarness` is the small console program that loads the extracted project JSON, applies the
randomized model-hash workaround, and calls the original `SimulationLibraryBuilder`.
