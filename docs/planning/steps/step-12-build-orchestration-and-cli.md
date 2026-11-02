# Step 12 — Build Orchestration and `mocassin` CLI

**Phase:** C. Compilation  
**Depends on:** 10, 11  
**Delivers:** A headless CLI that loads a project, validates, compiles, parameterizes, expands jobs, writes a
package, reports progress, and supports cancellation and partial builds.

## Goal

Make the entire model-building pipeline usable without the GUI. This de-risks the Avalonia work, enables
automation/CI, and gives the solver team a stable way to produce test packages.

## Original reference

- The GUI’s `SimulationLibraryBuilder` performed: template compatibility check → open/create `.msl` → reset
  model project → push project data → push customization → build model context → convert job templates →
  build job package models → add metadata → save.
- Progress was a `LibraryBuildStatus` enum with 25 states; cancellation used a `CancellationToken`; errors were
  strings in a log console.
- There was no headless CLI for model building; users had to use the GUI or write C# code.
- The original allowed create vs. append builds and a 99,999-job cap.

## Design

### 1. Command surface

```text
mocassin new <name> [-o <dir>] [--template blank|ceria]
mocassin validate <project.mocprj> [--json] [--warnings-as-errors] [--stats]
mocassin compile <project.mocprj> [-o <compiled.json>] [--json]
mocassin build <project.mocprj> -o <project.msl>
                [--template <buildTemplateId>]
                [--parameterization <id>]
                [--jobs <range>]
                [--parallel <n>]
                [--compact]
                [--force]
                [--json]
mocassin inspect <project.msl> [--json]
mocassin verify <project.msl> [--json]
mocassin simulate ...        # step 19/27
mocassin collect ...         # step 20
mocassin evaluate ...        # step 21
mocassin export ...          # step 21
mocassin doctor [--json]
```

- `build` selects a build template if one is provided; otherwise it requires exactly one usable template or
  explicit `--parameterization` plus a job template (future flag).
- `--jobs` applies after full expansion: job IDs are assigned from the complete template, then only the
  selected IDs are written. This keeps IDs stable across partial builds.
- `--parallel` controls compilation/lattice generation concurrency; package writes are serialized.
- `--force` allows overwriting an existing package; without it, an existing file is an error.
- `--json` emits a single JSON result object; progress events go to stderr as JSON Lines.

### 2. Build pipeline

```text
Load project
  → validate (stage 1–4)
  → select build template + parameterization
  → compile model (step 08)
  → apply parameterization (step 09)
  → expand jobs (step 10)
  → for each selected job: generate lattice + encode job
  → write package (step 11)
  → verify package
  → report
```

- The pipeline is an async `IAsyncEnumerable<BuildEvent>`:

```csharp
public abstract record BuildEvent;
public sealed record BuildStageStarted(string Stage) : BuildEvent;
public sealed record BuildProgress(string Stage, int Completed, int Total, string? Current) : BuildEvent;
public sealed record BuildDiagnostic(Diagnostic Diagnostic) : BuildEvent;
public sealed record BuildCompleted(string Path, int JobCount, TimeSpan Elapsed) : BuildEvent;
public sealed record BuildFailed(string Stage, string Message, int ExitCode) : BuildEvent;
```

- The GUI and CLI consume the same event stream.
- Cancellation is cooperative at stage boundaries and every N jobs; the temp package is deleted.
- Errors stop the build; warnings are collected and reported at the end.

### 3. Error model and exit codes

| Exit | Meaning |
| ---: | --- |
| 0 | Success |
| 1 | Validation/model error |
| 2 | Usage/argument error |
| 3 | I/O error (project, output, disk) |
| 4 | Compilation/parameterization error |
| 5 | Partial success (some jobs skipped/failed) |
| 6 | Cancelled |
| 10 | Unexpected internal error |

- Every error has a diagnostic code or a stable `CLIxxx` code.
- `--json` returns `{ "ok": false, "exitCode": 1, "diagnostics": [...] }`.
- Human output includes the stage, object path, and hint.

### 4. Progress reporting

Human mode:

```text
[1/6] Loading project…            done (42 ms)
[2/6] Validating…                 0 errors, 2 warnings (18 ms)
[3/6] Compiling model…            done (610 ms)
[4/6] Parameterizing…             done (12 ms)
[5/6] Expanding jobs…             1,000 jobs (120 ms)
[6/6] Writing package…            [##########] 1000/1000 (4.2 s)
Package written: ceria.msl (312 MB, 1000 jobs)
```

JSON Lines mode:

```json
{"event":"build.progress","stage":"writing","completed":250,"total":1000,"current":"T773/x001/2"}
```

- Progress is throttled (e.g. 10 Hz) to avoid overwhelming the GUI.
- ETA is computed from a rolling average.
- `--quiet` suppresses human progress but not errors.

### 5. Build templates

- A build template references a parameterization and a job template.
- If the project has exactly one build template, `build` uses it automatically.
- If multiple, `--template` is required; the error lists the available IDs/names.
- `--parameterization` can override the template’s parameterization for experimentation; the signature is
  checked.
- `--dry-run` prints the effective job count, lattice sizes, doping reports, and package size estimate without
  writing.

### 6. Partial builds

- `--jobs 1-100,250,300-350` parses ranges.
- Job IDs remain the full-expansion IDs; gaps are allowed.
- The package records the selected job set in `schema_info` (`job_selection` key) for reproducibility.
- Results for unselected jobs are absent; `inspect` shows the selection.
- Partial builds are useful for testing and incremental HPC campaigns.

### 7. Doctor

`mocassin doctor` checks:

- .NET runtime version and OS/architecture;
- write permissions for the output directory;
- SQLite availability/version;
- `mocassin-sim` presence and version (if configured);
- symmetry catalog checksum/version;
- sample project validation;
- available disk space for the estimated package.

It prints a diagnosis table and exits 0 if healthy, 1 otherwise.

## Implementation tasks

- [ ] Implement argument parsing (`System.CommandLine` or a small hand-written parser) with help/completions.
- [ ] Implement the build pipeline and event stream.
- [ ] Implement `new`, `validate`, `compile`, `build`, `inspect`, `verify`, `doctor`.
- [ ] Implement error/exit-code mapping and JSON output.
- [ ] Implement progress, ETA, throttling, and quiet mode.
- [ ] Implement build-template selection and partial jobs.
- [ ] Implement `--dry-run` preview.
- [ ] Add integration tests that build the ceria sample and verify the package.
- [ ] Add CLI docs and shell completion generation.

## Acceptance criteria

- [ ] `mocassin validate samples/ceria/ceria.mocprj` exits 0 with the expected warnings.
- [ ] `mocassin build samples/ceria -o /tmp/ceria.msl` produces a package that `mocassin verify` accepts.
- [ ] `--jobs 1-3` writes exactly jobs 1–3 with stable IDs.
- [ ] Cancelling a build leaves no temp file and preserves an existing package.
- [ ] `--json` output validates against a schema and contains the exit code.
- [ ] `doctor` reports a healthy environment on CI.
- [ ] The CLI can build 10⁴ jobs without the GUI.

## Tests

| Test | Assertion |
| --- | --- |
| Commands | Help, required args, invalid ranges |
| Build | Ceria package content and counts |
| Partial | Selected IDs, gaps, metadata |
| Cancellation | Temp cleanup, old package intact |
| Errors | Exit codes for each failure class |
| JSON | Schema and machine-readable progress |
| Doctor | Healthy/unhealthy scenarios |
| Templates | Auto-select, ambiguous, override |
| Performance | Build throughput and memory |

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| CLI and GUI diverge | Both consume the same pipeline/events |
| Partial builds confuse IDs | Full-expansion IDs and recorded selection |
| Progress overhead | Throttle and use a bounded channel |
| Argument parsing complexity | One command parser, shared option definitions, tests |
| Silent warnings | Summary and `--warnings-as-errors` |

## Done when

The CLI can create, validate, compile, build, inspect, verify, and diagnose a project end-to-end, with stable
IDs, progress, cancellation, and machine-readable output.
