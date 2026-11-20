# Command-line interface

The `mocassin` CLI is the headless model-building and package workflow. It creates projects, validates and
compiles models, builds immutable `.msl` packages, inspects and verifies packages, diagnoses the environment,
and previews cleanup. Results collection, evaluation, and solver execution arrive in later steps; the parser
recognises those commands and tells you which step owns them.

Every command has a stable exit code. `validate` and the seven `reports`-backed commands (`new`, `compile`,
`build`, `inspect`, `verify`, `doctor`, `clean`) print exactly one machine-readable report with `--json`;
`--help`, `--version`, and the reserved commands do not accept `--json`. Human progress goes to
stderr; with `--json` stderr is JSON Lines only. The final report or human result goes to stdout.

## Global options

```text
mocassin --help
mocassin --version
mocassin <command> --help
```

`--version` is root-only and prints the CLI assembly informational version and the runtime description, then
exits 0. It is not inherited by subcommands: `mocassin <command> --version` is an unrecognized option and exits
2 with `CLI001`. `--help` prints the command list or the command’s options and exits 0. An unknown command exits
2.

Options use the standard `System.CommandLine` conventions: `--name value` or `--name=value`, `-o value`, and
flags take no value. A value beginning with `-` is accepted for a value-taking option, so `--jobs -1` and
`--parallel -1` reach the command as `-1` (and are rejected by range checks), and `-o -file` names the path
`-file`. `--` ends option parsing so a path beginning with `-` can be passed. Option names are
case-sensitive; combined short options are not supported. Numbers use invariant culture: `--parallel` accepts
`1..256`; omitting it uses the processor count, while an explicit `--parallel 0` is a range error, not a
synonym for the default. `--older-than` accepts `0..36500`. A parse error exits 2 with a `CLI001` diagnostic;
the library's own parse-error exit code (1) is never exposed.

`--json` makes stderr JSON Lines only and suppresses human progress, whether or not `--quiet` is used.
`--quiet` additionally suppresses the `build.stage` and `build.progress` JSON-Lines events (`log` is
reserved and has no producer in this release); diagnostics, `build.started`, `build.finished`, and the final
report remain. Neither option suppresses errors.

`MOCASSIN_DEBUG=1` includes a stack trace for an unexpected internal error. `MOCASSIN_SIM` names the solver
executable that `doctor` probes (the solver arrives in a later step).

## Response files

A token that starts with `@` and has at least one more character is a response-file reference. The rest of the
token is the path, resolved against the current working directory (not the containing response file). `@` alone
is a literal argument. There is no escape sequence: `@@name` refers to a file named `@name`. Quote a path that
contains spaces as one token, for example `@"my file.rsp"`. The CLI expands and tokenizes response files itself
before `System.CommandLine` parses the resulting token list.

Files are UTF-8 (an optional BOM is accepted); invalid UTF-8 or a NUL byte exits 2 with `CLI001`. LF, CRLF,
and CR line endings are accepted; each line is a token stream. Unicode whitespace separates tokens. Double
quotes group whitespace and are removed; an empty quoted token (`""`) is preserved as an empty argument. Single
quotes and backslashes are literal, and there is no escape for a double quote inside a quoted token. Quotes
inside a token concatenate the parts (`a"b c"d` becomes `ab cd`; `a""b` becomes `ab`); a quote pair that starts
a token followed immediately by another starts a separate token (`"a""b"` becomes `a` and `b`). A line whose
first non-whitespace character is `#` is ignored; `#` elsewhere is literal. Double quotes do not span lines; an
unterminated quote that begins a token discards it (`"x y` gives no token), and one inside a token keeps the
token with the rest of the line and the quote removed (`x"y z` gives `xy z`). A token in a file that starts
with `@` is another response file.

Limits: **1 MiB of raw bytes per file, 4 MiB of raw response-file bytes across the command line, nesting depth
8, 10,000 tokens contributed by response files, and 32 response files**. Missing, unreadable, invalid, or oversized files exit 2 with
`CLI001`; they are usage errors, not `CLI002`/3.

`@file` before `--` is expanded; after `--` it is a literal argument. `--name=@value` is the before-`--` form of
a literal `@`-leading option value; `--input=@file` is literal, while `--input @file` expands before option
arity checking, so a multi-token file in a single-value position is a usage error. A `--json` inside a file is
honored. If a file cannot be read, a `--json` inside it cannot be known, so put `--json` on the command line if
you need a report; a report is produced only when a command is recognised outside the failed file
(`mocassin <command> @missing --json`), while `mocassin @missing --json` prints human usage and no report.

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
existing project: an existing target file is `CLI003`/3. A directory occupying `<dir>/<name>.mocprj` and an
invalid output path are I/O failures, `CLI002`/3. The embedded blank and ceria assets are compiled resources,
so creation needs no template directory or network access.

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

### Human output

The default output is a stable table with severity, code, message, object/property path, and the per-instance
hint, followed by the raw summary, an optional statistics table, and the policy-adjusted result. Step 12 does
not change this format.

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
  file; a directory target, a missing parent directory, or an invalid path is `CLI002`/3.
- `--json` prints a `compile-v1` summary with the model and document hashes, the compiled-model signature,
  compiler version, counts, cache statistics, stage timings, and diagnostics. `stages[]` reports the ten
  compiler stages in order: `resolve`, `structure`, `environments`, `pairs`, `clusters`, `rules`, `encoding`,
  `indexing`, `signing`, and `report`; `report` always runs, even when an earlier stage fails.
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
- An explicit `-o` target is checked before any load: an existing package without `--force` is `CLI003`/3,
  and a directory target, a missing parent directory, or an invalid path is `CLI002`/3, so no build work
  starts. The missing parent is not created; create the directory first. A target derived from a build
  template’s output name needs the recipe, so the CLI performs a preflight load and selection, checks the
  derived target, and then hands the project to the build pipeline, which loads it again for the build. The
  writer’s overwrite guard remains the final race check.
- An existing package is never replaced unless `--force` is given. Without it, the command exits 3 and the
  existing file is untouched.
- Publication is atomic: the writer builds in a temp file beside the target, checkpoints, fsyncs, and renames.
  A failure or cancellation before publication leaves the previous package byte-identical. A failure after
  publication leaves the new package in place and reports `published: true`.
- `--compact` is the size preset: Zstd quality 9 plus `VACUUM`. It is slower and smaller; payload content,
  signatures, job IDs, and the canonical summary are unchanged.

### Job selection

`--jobs` accepts `all`, `1-100`, `1,3,5`, and mixed forms. IDs are 1-based and refer to the complete template
expansion. The CLI validates the grammar before loading the project, so a malformed expression is `CLI005`/2
without any build work; out-of-range IDs need the expanded count and are rejected after expansion. The command
normalizes the selection:

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

`--preview` runs the same load/select/compile/validate/parameterize/expand path and the pipeline’s shared
preview builder, but it generates no duplicate-job lattices, opens no writer, and creates no file. The typed
`BuildPreview` result is the same payload a library caller (Step 26) receives; the CLI only projects it into
`build-v1.preview`, without a reload, recompile, or second evaluation. It reports `mode: "preview"`,
`package: null`, and the per-collection configuration, optimizer, lattice, doping, and charge data.
`--preview` rejects `-o`, `--force`, and `--compact`.

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

Inspection is deliberately tolerant of non-fatal defects: it reports the metadata it can still read, so a
corrupt payload or a wrong signature is diagnosed by `mocassin verify` rather than blocking inspection. This
is an intended improvement over the original model builder, whose build/read failures appeared only as text
in a log console. Only a fatal structure failure makes the package uninspectable. A missing file, a directory,
or an unreadable path is `CLI002`/3 and is checked before the inspector opens the file.

`--json` prints `inspect-v1` with a `package` object containing those fields. A fatal structure failure is
`CLI009`/1 with a hint to run `mocassin verify`; `CLI009` means “this input cannot be inspected as a
simulation package”, not “the package is corrupt in some payload”.

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

`--json` prints `verify-v1` with `valid`, `checks[]`, and the same `package` object as `inspect`. The report
has one `diagnostics[]` array, the shared envelope; it carries the `PKG` findings. There is no second
payload-local diagnostics array.

## `mocassin doctor`

```text
mocassin doctor [--json]
```

Checks the .NET runtime, OS and architecture, SQLite availability and version, write permission and free
space in the current directory, the embedded symmetry catalog version and checksum, the embedded project
templates, and an optional solver.

The checks run in a fixed order: `runtime`, `sqlite`, `write-permission`, `disk-space`, `symmetry-catalog`,
`project-templates`, and `solver`.

- Free space below **100 MiB** is a `warn`, not a failure: the threshold is advisory and the command stays
  healthy. The code names the check `disk-space` and reports the free bytes.
- A missing solver is a **warning** until the solver ships; a configured but broken solver is a failure.
- A check that throws is recorded as a `fail` with the exception type and message; it never aborts the
  command, so the report stays schema-valid.
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

`--dry-run` is required in this release; without it the command exits 2 with `CLI008` and points to the
deletion command. The refused run is still read-only: it lists the same `Job%05d` folders it would remove,
and with `--json` it emits one `clean-v1` report with `dryRun: false`, the listing, `removedCount: 0`, and
`CLI008`. `--results` is reserved for the results-store step; it also exits 2 with `CLI008` and a minimal
`clean-v1` report (no package inspection and an empty listing). No file, folder, `.msl`, `.msr`, or `.mcs` is
ever removed by either path. With `--dry-run`, `--json` prints `clean-v1` with `dryRun: true`, the folder
list, total bytes, and `removedCount: 0`.

## `mocassin-sim` (Rust solver)

```text
mocassin-sim --db <project.msl> --job <id> [--io <dir>]
             [--seed <hex32>] [--threads <n>]
             [--extension <path>] [--no-extensions] [--jump-log-max-ev <f64>]
             [--log-level <error|warn|info|debug|trace>]
             [--log-format human|json] [--dry-run] [--version]
```

`mocassin-sim` is a separate process that consumes only the immutable `.msl` package. It never reads project
JSON and never writes `.msl` or `.msr`. This release implements the inspection path; the simulation engines,
job-folder output, batch mode, and extensions arrive in Steps 14–19.

### Options

| Option | Meaning |
| --- | --- |
| `--db <path>` | Required `.msl` package. Opened read-only. |
| `--job <id>` | Required 1-based job id. An unknown id is a usage error. |
| `--io <dir>` | Job folder for a future run. Accepted but not created in this release. |
| `--dry-run` | Load, validate, and print the summary; write nothing; exit 0. Required in this release. |
| `--stdout <file>` | Reserved for the run statistics dump (Step 19). Not accepted in this release; any use is an invalid-argument error (10). |
| `--seed <hex32>` | Exactly 64 hex characters; overrides the effective seed for debugging. Only the seed hash is printed. |
| `--threads <n>` | `1..=256`; accepted and validated, unused until Step 19. |
| `--extension <path>` | Rejected until the extension ABI lands (Step 19). |
| `--no-extensions` | Accepted; no-op in this release. Conflicts with `--extension`. |
| `--jump-log-max-ev <f64>` | Finite and `> 0`; accepted, unused until Step 18. |
| `--log-level` | `error`, `warn`, `info`, `debug`, or `trace`; default `info`. |
| `--log-format` | `human` or `json`; default `human`. Logs go to stderr. |
| `--version` | Print the solver version; exit 0. |

### Dry run

`--dry-run` opens the package read-only, validates the schema, model, lattice, and job, and prints one line per
group:

```text
mocassin-sim 0.1.0 dry-run
package: <path>
schema: format_version=1 project_id=<uuid> job_selection=all
model: signature=<64hex> model_signature=<64hex> particles=5 positions=4 sites=4 environments=4 pair_tables=0 cluster_tables=0
lattice: signature=<64hex> size=6x6x6 length=864
job: id=1 kind=kmc simulation_index=0
settings: temperature_k=773 target_mcsp=150 write_out_count=100 pre_run_mcsp=250 time_limit_s=31536000 min_success_rate=null electric_field_vm=10000000 flags=0x72
transitions: collections=1 directions=2 rules=4
trackers: global=2 static_per_cell=16
rng: algorithm=chacha12-v1 seed_hash=<64hex>
validation: ok
```

The counts and signatures equal the canonical package summary (`inspect-v1`/`package-summary-v1`) for the same
job. Build metadata (`app_version`, `created_utc`, `generator`) is not printed. The `settings:` line is the
**resolved job settings** and composed flags, not the model's `simulations[].default_settings`; the fixture's
job target is 150 while simulation 0's default is 200. The dry run creates no directory, no log file, and no
job folder; `--stdout` is therefore a usage error.

Without `--dry-run`, this release loads and validates the job and exits 10 with
`unsupported: simulation engines are implemented in Steps 14–19`. It does not create `--io`, write a state, or
claim success.

### Logging and streams

stdout is the human report. Structured logs and errors go to stderr through `tracing`; `--log-format json`
emits one JSON object per record. `RUST_LOG` is not consulted, so `--log-level` is the only filter.

### Solver exit codes

The canonical table is the internal target architecture §7.3; this table mirrors it. It is the
`mocassin-sim` process contract, not the .NET `mocassin` table.

| Code | Meaning |
| ---: | --- |
| 0 | Completed (including a successful dry run). |
| 5 | Partial success for multi-job/batch commands (at least one job failed, others completed); reserved until Step 19. |
| 10 | Invalid arguments, unknown job, unsupported option, extension requested. |
| 11 | Package error: unusable SQLite, schema/version mismatch, missing element, invalid frame, decode/scan/size error. |
| 12 | Model/data error: signature mismatch, invalid index/length/value, column/payload mismatch. |
| 13 | Numerical failure or internal invariant violation (including a caught panic). |
| 14 | Aborted by a configured limit. |
| 15 | Cancelled. |
| 16 | I/O error: missing or unreadable path. |

Parse errors are remapped to 10, not the parser library's default. Help and `--version` exit 0.

## Shell completion

Step 12 does **not** ship a `completion` command and does not generate bash, zsh, fish, or PowerShell
scripts; this is the owner's decision of 2026-11-17. The CLI is built on `System.CommandLine` 3.0.0, which
provides the `[suggest]` protocol that `dotnet-suggest` consumes, not script generation. The directive is
accepted but is not a contract, and its command-scoped behaviour is not promised in this release. Shell
integration is deferred to Step 31 packaging; until then, script the commands directly.

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

`validate` uses only 0/1/2/3. A usage error with `--json` prints exactly one report with `exitCode: 2` for
`validate` and the seven report-backed commands. `--help`, `--version`, and reserved commands do not accept
`--json` and print no report. A parse error is always `CLI001`/2; the parser library's default exit code (1)
is never exposed.

## `CLI` diagnostics

CLI-only failures have stable `CLI001`–`CLI009` codes; domain failures keep their own codes. The normative,
drift-checked table (including `PROJ`, `STR`, `TRN`, `ENM`, `SIM`, `CMP`, `PRM`, `JOB`, `PKG`, and the CLI
codes) is in the [diagnostics reference](diagnostics.md); this page does not duplicate it.

## JSON Lines events

With `--json`, build progress is emitted as JSON Lines on **stderr** and human progress is suppressed, so
stderr is JSONL-only; the single report object stays on stdout. Every line has `schemaVersion: 1` and a stable
`event` name.

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
| `log` (reserved) | `level`, `message` |

Consumers ignore unknown events and fields. `log` is reserved: the schema accepts it, but Step 12 emits no
`log` events; `--quiet` still names it so the future producer keeps the documented suppression. `--quiet`
suppresses `build.stage` and `build.progress`; `build.started`, `build.finished`, and `diagnostic` remain.
