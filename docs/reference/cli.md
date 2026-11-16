# Command-line interface

The `mocassin` CLI is the headless model-building and package workflow. It creates projects, validates and
compiles models, builds immutable `.msl` packages, inspects and verifies packages, diagnoses the environment,
and previews cleanup. Results collection, evaluation, and solver execution arrive in later steps; the parser
recognises those commands and tells you which step owns them.

Every command has a stable exit code and, with `--json`, exactly one machine-readable report. Human progress
goes to stderr; the final result goes to stdout.

## Global options

```text
mocassin --help
mocassin --version
mocassin <command> --help
```

`--version` prints the CLI assembly version and the runtime description. `--help` prints the command list or
the command’s options and exits 0. An unknown command exits 2.

Options use `--name value` or `--name=value`; `-o value` is the short output option. A flag takes no value.
`--` ends option parsing so a path beginning with `-` can be passed. Option names are case-sensitive.
Numbers use invariant culture: `--parallel` accepts `1..256` and `--older-than` accepts `0..36500`.

`--quiet` suppresses human progress. With `--json`, it also suppresses the `build.stage`, `build.progress`,
and `log` JSON-Lines events; diagnostics and the final report remain. It never suppresses errors.

`MOCASSIN_DEBUG=1` includes a stack trace for an unexpected internal error. `MOCASSIN_SIM` names the solver
executable that `doctor` probes (the solver arrives in a later step).

## `mocassin new`

```text
mocassin new <name> [-o <dir>] [--template blank|ceria] [--json]
```

Creates `<dir>/<name>.mocprj` with a fresh project ID and master seed. `-o` defaults to the current directory;
the directory is created if missing.

| Option | Meaning |
| --- | --- |
| `--template blank` | An empty, valid project. |
| `--template ceria` | The ceria model-definition sample as a starter model. It has no job template, so it is not buildable until you add one. |

`name` is both the display name and the file stem. It is trimmed, one `.mocprj` suffix is removed, and path
separators, `..`, control characters, and `: * ? " < > |` are rejected. The command never replaces an
existing project: a collision exits 3.

## `mocassin validate`

```text
mocassin validate <project.mocprj> [--json] [--warnings-as-errors] [--stats]
```

The command loads the project read-only. It never writes, acquires the editing lock, migrates in place, or
mutates the model. The load and model stages run exactly once: the command calls
`ProjectValidationPipeline.ValidateFileAsync`, which loads through a `ProjectStore` whose `ValidateOnLoad` is
`false` and runs the model stage once.

| Option | Meaning |
| --- | --- |
| `--json` | Print exactly one `validation-v1` JSON report to stdout. Human errors go to stderr. |
| `--warnings-as-errors` | Treat every warning as invalid for the exit code and `valid` flag. `summary` keeps the raw counts. |
| `--stats` | Compute and include advisory model statistics. It never changes diagnostics or `valid`. |

`validate` never compiles and never runs build-stage job checks. Stale or incomplete parameterization
templates that need a compiled model are reported by `mocassin build` after compilation.

### Exit codes

| Code | Meaning |
| --- | --- |
| `0` | Valid under the selected policy. |
| `1` | Validation/model errors, promoted warnings, or project content that cannot be parsed or migrated. |
| `2` | Usage or argument error. |
| `3` | The file is missing, unreadable, oversized, or failed with an operating-system/I/O error. |

A usage error with `--json` still prints one report with `exitCode: 2`, `valid: false`, and null identity
fields.

### `validation-v1` report

```json
{
  "schemaVersion": 1,
  "kind": "validation",
  "projectId": "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e80",
  "modelHash": "9f2c…",
  "documentHash": "4a71…",
  "valid": false,
  "exitCode": 1,
  "summary": { "errors": 2, "warnings": 1, "infos": 0 },
  "diagnostics": [],
  "statistics": null
}
```

- `projectId`, `modelHash`, and `documentHash` are `null` before a successful load. Hashes are raw lowercase
  hexadecimal without a prefix.
- `diagnostics` is ordered by severity, code, domain, object ID, property, and message. Exact duplicate
  `(code, path, message)` triples are removed.
- `statistics` is non-null only when `--stats` was requested and statistics could be computed: the document
  loaded, the model stage produced no error, and every required preview succeeded.
- Usage errors keep `diagnostics` empty when no store diagnostic exists.

`validation-v1` is the only command-specific report that predates Step 12; its shape is unchanged. The other
commands use the shared report family described below.

## `mocassin compile`

```text
mocassin compile <project.mocprj> [-o <compiled-inspection.json>] [--json]
```

Loads, validates, and compiles the model. It never applies a parameterization, expands jobs, or writes a
package.

- `-o` writes the canonical `compiled-inspection-v1` document atomically. It replaces an existing derived
  file; a directory target exits 3.
- `--json` prints a `compile-v1` summary with the model and document hashes, the compiled-model signature,
  compiler version, counts, cache statistics, stage timings, and diagnostics.
- Validation errors exit 1 before compilation; `CMP` compile errors exit 4.
- Without `-o`, the inspection document is not written; the command still reports the summary.

## `mocassin build`

```text
mocassin build <project.mocprj> [-o <project.msl>]
                [--preview]
                [--template <buildTemplateId>]
                [--job-template <jobTemplateId>]
                [--parameterization <parameterizationId>]
                [--jobs <range>] [--parallel <n>] [--compact] [--force]
                [--warnings-as-errors] [--quiet] [--json]
```

The build loads, selects a recipe, validates the project, compiles, validates the job templates with the
compiled model, parameterizes, expands the complete job template, generates the selected lattices and job
payloads, writes the package, publishes it atomically, and verifies it.

### Recipe selection

| Option | Meaning |
| --- | --- |
| `--template` | A project **build template** (job template + parameterization + output name). A job-template ID here is a usage error with a hint. |
| `--job-template` | A project job template. Overrides the build template’s job template. |
| `--parameterization` | A project parameterization template. Overrides the build template’s parameterization. |

1. `--template` selects a build template; an unknown ID is a `PROJ010` reference error.
2. Without `--template`, exactly one project build template is selected automatically. Several templates
   without a selection are a usage error listing their IDs and names.
3. The job template comes from `--job-template`, then the build template, then exactly one project job
   template. A project with no job template cannot build.
4. The parameterization comes from `--parameterization`, then the build template, then exactly one project
   parameterization. A project with **no** parameterizations uses the implicit default: all pair/group values
   `0 eV`, kinetic rule frequencies the compiled `1e13 Hz`, and the authored site-energy background.
5. Explicit overrides are allowed for experimentation. The report records the effective IDs.

### Output and replacement

- `-o` sets the package path. Without it, a build template’s output name is used; without a build template,
  `<project-file-stem>.msl` is written next to the project. A build-template output name is a file-name stem;
  separators and `..` are rejected.
- An existing package is never replaced unless `--force` is given. Without it, the command exits 3 and the
  existing file is untouched.
- Publication is atomic: the writer builds in a temp file beside the target, checkpoints, fsyncs, and renames.
  A failure or cancellation before publication leaves the previous package byte-identical. A failure after
  publication leaves the new package in place and reports `published: true`.
- `--compact` is the size preset: Zstd quality 9 plus `VACUUM`. It is slower and smaller; payload content,
  signatures, job IDs, and the canonical summary are unchanged.

### Job selection

`--jobs` accepts `all`, `1-100`, `1,3,5`, and mixed forms. IDs are 1-based and refer to the complete template
expansion. The command normalizes the selection:

- a full selection becomes `all`;
- duplicates, overlaps, and adjacent ranges are merged and sorted (`1,2,3,5` becomes `1-3,5`);
- zero, negative, descending, empty, and out-of-range selections exit 2.

The normalized expression is written to the package’s `schema_info.job_selection`; `mocassin inspect` shows
it. Unselected jobs are not generated or written, and their IDs stay reserved, so a partial package has gaps
but never renumbers jobs.

### Preview

```text
mocassin build <project.mocprj> --preview [--template <buildTemplateId>] [--job-template <id>]
                [--parameterization <id>] [--jobs <range>] [--json]
```

`--preview` runs the same load/select/compile/validate/parameterize/expand path and evaluates the selected
configurations through the shared lattice-preview service, but it generates no duplicate-job lattices, opens
no writer, and creates no file. It reports `mode: "preview"`, `package: null`, and the per-collection
configuration, optimizer, lattice, doping, and charge data. `--preview` rejects `-o`, `--force`, and
`--compact`.

### Progress, cancellation, and warnings

```text
[1/10] Loading project…               done (42 ms)
[2/10] Selecting build recipe…        Ceria temperature sweep → implicit default (3 ms)
[3/10] Validating project…            0 errors, 2 warnings (18 ms)
[4/10] Compiling model…               done (610 ms)
[5/10] Validating job templates…      0 errors, 0 warnings (4 ms)
[6/10] Parameterizing…                implicit default, signature a9e0… (12 ms)
[7/10] Expanding jobs…                6 jobs (4 ms)
[8/10] Generating and writing jobs…   [##########] 6/6 (1.4 s)
[9/10] Publishing…                    ceria.msl (2.1 MB, sha256 55f9…) (12 ms)
[10/10] Verifying package…            valid, 10 checks (40 ms)
Package written: work/ceria.msl (2.1 MB, 6 jobs, selection all)
```

- Progress is throttled and shows an ETA from a rolling average. The final progress of a stage is always
  shown.
- The first Ctrl+C (or SIGTERM on Unix) requests cancellation. Before publication, the temp file is deleted
  and an existing package is preserved; after publication, the package remains and the command exits 6.
- `--warnings-as-errors` turns validation-stage warnings (including structural `JOB` warnings) into exit 1
  and parameterization warnings into exit 4. Compile (`CMP`) and writer (`PKG`) warnings stay warnings.
- After publication the command runs the package verifier. Verification errors exit 1 and are reported as
  `PKG` diagnostics; the published file remains, because it is already a valid atomic artifact.

### `build-v1` report

`--json` prints one `build-v1` object:

```json
{
  "schemaVersion": 1,
  "kind": "build",
  "mode": "build",
  "ok": true,
  "exitCode": 0,
  "projectId": "…",
  "modelHash": "…",
  "documentHash": "…",
  "modelSignature": "…",
  "parameterizationSignature": "a9e0…",
  "selection": {
    "buildTemplate": null,
    "jobTemplate": { "id": "…", "name": "Temperature sweep" },
    "parameterization": { "id": null, "name": null, "implicitDefault": true },
    "outputName": "ceria"
  },
  "jobSelection": { "expression": "all", "expandedCount": 6, "selectedCount": 6 },
  "timings": [{ "stage": "compile", "ms": 610 }],
  "package": {
    "path": "/tmp/ceria.msl", "sha256": "…", "sizeBytes": 2100000,
    "jobCount": 6, "modelCount": 1, "latticeCount": 4,
    "latticeReuseFactor": 1.5, "published": true
  },
  "preview": null,
  "verification": "passed",
  "summary": { "errors": 0, "warnings": 2, "infos": 0 },
  "diagnostics": []
}
```

In preview mode `package` is `null` and `preview` carries the lattice/doping report. A preview is size-neutral;
only a full build reports a measured `package.sizeBytes`. A cancellation after publication leaves
`published: true` and `verification: "skipped"`.

## `mocassin inspect`

```text
mocassin inspect <project.msl> [--json]
```

Read-only inspection through the shared package inspector. It prints the package header, model signatures,
job and lattice counts, collections, temperature and electric-field ranges, size, lattice reuse factor, and
the recorded `jobSelection`. It never opens the package writable and never needs `-wal`/`-shm` files.

`--json` prints `inspect-v1` with a `package` object containing those fields. A missing file exits 3; an
unreadable package exits 1 with a hint to run `mocassin verify`.

## `mocassin verify`

```text
mocassin verify <project.msl> [--json]
```

Runs the ordered `PKG` checks through the shared verifier and prints each check plus any diagnostics. It
never mutates the package and never writes a results store.

| Code | Meaning |
| --- | --- |
| `0` | The package is valid. |
| `1` | The package is invalid; `PKG` diagnostics explain why. |
| `3` | The file is missing or cannot be read. |

`--json` prints `verify-v1` with `valid`, `checks[]`, `diagnostics[]`, and the same `package` object as
`inspect`.

## `mocassin doctor`

```text
mocassin doctor [--json]
```

Checks the .NET runtime, OS and architecture, SQLite availability and version, write permission and free
space in the current directory, the embedded symmetry catalog version and checksum, the embedded project
templates, and an optional solver.

- A missing solver is a **warning** until the solver ships; a configured but broken solver is a failure.
- The command exits 0 when no check fails; warnings do not fail it. It exits 1 when a check fails.
- `--json` prints `doctor-v1` with `healthy`, `checks[]` (`name`, `status`, `message`, optional `detail`), and
  the environment (`framework`, `os`, `architecture`, `sqliteVersion`, `catalogVersion`, `catalogSha256`,
  `solverPath`, `solverVersion`).

## `mocassin clean --dry-run`

```text
mocassin clean <project.msl> [--dir <dir>] [--older-than <days>] --dry-run [--json]
```

Step 12 implements only the dry run. It lists `Job%05d` folders in `--dir` (default: the package directory),
optionally filtered by age, and reports their paths, IDs, timestamps, and sizes. The package is opened
read-only to report its `jobSelection` and `jobCount`.

`--dry-run` is required in this release; without it the command exits 2 and points to the deletion command.
No file, folder, `.msl`, or `.msr` is ever removed. `--results` is reserved for the results-store step.
`--json` prints `clean-v1` with `dryRun: true`, the folder list, total bytes, and `removedCount: 0`.

## `mocassin completion`

```text
mocassin completion <bash|zsh|fish|powershell>
```

Prints a completion script generated from the command table. Completion output is convenience text, not a
versioned contract. An unknown shell exits 2.

## Reserved commands

`simulate`, `collect`, `evaluate`, `export`, and full `clean` are recognised by the parser and exit 2 with a
`CLI008` diagnostic naming the owning step. They are not silently treated as unknown commands.

## Exit codes

| Code | Meaning |
| ---: | --- |
| 0 | Success (warnings allowed unless promoted). |
| 1 | Validation, model, invalid package, or unhealthy `doctor`. |
| 2 | Usage error, selection ambiguity, malformed/out-of-range `--jobs`, reserved command or option. |
| 3 | I/O error, missing/unreadable input, existing target without `--force`. |
| 4 | Compilation or parameterization error, promoted parameterization warning. |
| 5 | Partial success — reserved for multi-job commands; `build` is all-or-nothing and does not emit it. |
| 6 | Cancelled by the user. |
| 10 | Unexpected internal error. |

`validate` uses only 0/1/2/3. A usage error with `--json` prints exactly one report with `exitCode: 2` for a
recognised command.

## `CLI` diagnostics

CLI-only failures have stable codes; domain failures keep their own codes.

| Code | Meaning |
| --- | --- |
| `CLI001` | Usage or argument error. |
| `CLI002` | I/O failure with no domain diagnostic. |
| `CLI003` | Output exists and overwrite was not requested. |
| `CLI004` | Build/job-template/parameterization selection is missing, ambiguous, or the wrong kind. |
| `CLI005` | `--jobs` is malformed, empty, or out of range. |
| `CLI006` | Cancelled by the user. |
| `CLI007` | Unexpected internal error. |
| `CLI008` | Command or option reserved for a later step. |
| `CLI009` | The package cannot be inspected or read; run `verify` for detail. |

The full catalog, including `PROJ`, `STR`, `TRN`, `ENM`, `SIM`, `CMP`, `PRM`, `JOB`, and `PKG`, is in the
[diagnostics reference](diagnostics.md).

## JSON Lines events

With `--json`, build progress is emitted as JSON Lines on **stderr**; the single report object stays on
stdout. Every line has `schemaVersion: 1` and a stable `event` name.

```json
{"schemaVersion":1,"event":"build.started","total":6}
{"schemaVersion":1,"event":"build.stage","stage":"generate","status":"started"}
{"schemaVersion":1,"event":"build.progress","stage":"generate","completed":3,"total":6,"current":"3","etaS":0.8}
{"schemaVersion":1,"event":"diagnostic","code":"JOB011","severity":"warning","message":"…"}
{"schemaVersion":1,"event":"build.finished","exitCode":0,"wallTimeS":1.4,"published":true}
```

| Event | Required fields |
| --- | --- |
| `build.started` | `total` |
| `build.stage` | `stage`, `status` (optional `ms`) |
| `build.progress` | `completed`, `total` (optional `stage`, `current`, `etaS`) |
| `build.finished` | `exitCode`, `wallTimeS` (emitted exactly once) |
| `diagnostic` | `code`, `severity`, `message` (optional `path`) |
| `log` | `level`, `message` |

Consumers ignore unknown events and fields. `--quiet` suppresses `build.stage`, `build.progress`, and `log`;
`build.started`, `build.finished`, and `diagnostic` remain.
