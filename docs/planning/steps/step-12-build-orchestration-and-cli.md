# Step 12 — Build Orchestration and `mocassin` CLI

**Phase:** C. Compilation
**Depends on:** 10, 11
**Delivers:** A headless CLI that loads a project, selects a build recipe, validates, compiles,
parameterizes, expands jobs, writes an immutable package, verifies it, reports progress, and supports
cancellation and partial job selections — plus the shared build-orchestration library that the Avalonia build
UI consumes later.

## Implementation status

- **Status:** Not started — the design changed on 2026-11-17 (the hand-written parser is replaced by
  `System.CommandLine` 3.0.0); a round-3 design re-review is pending; no step branch yet.
- **Design review:** Re-review required — round 3 reviews `8af67f8` (design freeze); round 2 was
  `Ready with notes` at `4ad40ac` (reviewed `05b7297`) — [record](../reviews/step-12.md) (round 1
  `Changes required`; round 2 `Ready with notes`, R2-1–R2-4 open; round 3 pending)
- **Feature list:** Proposed 2026-11-16 (seven features); feature 1 revised 2026-11-17 (library adoption, no
  parser/help/completion writers); awaiting one owner approval with the design verdict.
- **Sizing:** Revised 2026-11-17 to 2.75–3.75 person-weeks (was 3.0–4.0), still a single-step exception,
  seven features, no split, with the 2.25 pw contingency. The revision is presented for owner confirmation at
  the round-3 start gate; `04-roadmap.md` §3/§3.1 and `05-open-decisions.md` record it.
- **Parser:** `System.CommandLine` 3.0.0 (MIT, pinned, CLI-only reference); Native AOT smoke-verified on
  `net11.0`/`linux-x64`; no shell-completion command in Step 12 (§5.2, §13.8).
- **Step branch:** —
- **Step CI:** —
- **Main merge:** —
- **Step tag:** —
- **Audit:** Not requested
- **Last updated:** 2026-11-17

### Proposed feature list

| # | Feature | Branch | Planned scope | Outcome | Order |
| ---: | --- | --- | --- | --- | --- |
| 1 | CLI shell, parser configuration, and report contracts | `feature/step-12-build-orchestration-and-cli/cli-shell-and-report-contracts` | `System.CommandLine` 3.0.0 adoption (`RootCommand` graph, command/option/argument definitions, typed `ParseResult` binding), parse-error→`CLI001`/exit-2 mapping, help/version, common option definitions, exit-code type, Contracts-owned `CLI` diagnostic descriptors, shared report DTOs/writers, the seven `reports` manifest descriptors and generated artifacts, parser-convention/usage/schema tests, AOT publish smoke check; existing `validate` output and `build --preview` behaviour preserved | One library-defined command graph with stable exit codes and seven manifest-registered report schemas; `validation-v1` output unchanged; no `completion` command | 1 |
| 2 | Build orchestration core | `feature/step-12-build-orchestration-and-cli/build-orchestration-core` | `Mocassin.Packages.Build`: `IBuildPipeline`, request/options/result/failure types, stage enum, event stream, selection resolver, `JobSelection` parser and normalizer, progress estimator, stage timings, cancellation, parallel lattice generation with serialized writes, real-project tests | A UI-independent pipeline with typed events, deterministic selection, identity/failure results, cancellation, and real-project tests | 2 |
| 3 | Build and preview command | `feature/step-12-build-orchestration-and-cli/build-and-preview-command` | `build` and `build --preview`, `--jobs`, `--parallel`, `--force`, `--compact`, `--warnings-as-errors`, `--quiet`, `--json`/JSON-Lines, human progress, post-publication verification, Ctrl+C/SIGTERM handling, the sample build, partial/cancel/force/parallel/compact/exit-code/schema tests | `build` and `build --preview` write/verify a package or preview with progress, cancellation, and the documented exit codes | 3 |
| 4 | Project and compile commands | `feature/step-12-build-orchestration-and-cli/project-and-compile-commands` | `new` (embedded blank and ceria templates, fresh identity, safe create), `compile` (load/validate/compile, optional `compiled-inspection-v1` output, summary report), tests | `new` creates a valid fresh project and `compile` reports/optionally writes the inspection document | 4 |
| 5 | Package inspection commands | `feature/step-12-build-orchestration-and-cli/package-inspection-commands` | `inspect` and `verify` wrappers over the Step 11 inspector/verifier, reports, exit codes, corrupt/truncated/missing-package tests | `inspect`/`verify` read a real package read-only with schema-valid reports and correct exit codes | 5 |
| 6 | Doctor and dry-run clean | `feature/step-12-build-orchestration-and-cli/doctor-and-dry-run-clean` | `doctor` checks and report, `clean --dry-run` listing, healthy-CI test, injected-failure tests, no-deletion test | `doctor` is green on CI and `clean --dry-run` lists without deleting anything | 6 |
| 7 | Documentation, parity, and closeout | `feature/step-12-build-orchestration-and-cli/documentation-parity-and-closeout` | CLI/user/troubleshooting/diagnostics/dev pages, parity rows, 10⁴-job evidence, closeout evidence, audit request, full local gate | The documentation, parity, evidence, and audit request are complete and the full local gate is green | 7 |

The list is a proposal. It is approved once by the human owner together with the design verdict; that approval
creates `step/12-build-orchestration-and-cli` and marks the step `In progress`. Individual features are then
squash-merged without a further approval.

| Feature | Branch | Status | Local gates | Step CI | Squash commit |
| --- | --- | --- | --- | --- | --- |
| — | — | — | — | — | — |

## 1. Goal

Make the entire model-building pipeline usable without the GUI. A user must be able to create or open a
project, validate it, compile and inspect the compiled model, build an immutable `.msl` package with a chosen
job selection, verify the package, diagnose the environment, and script all of it. The GUI build UI in Step 26
consumes the same library pipeline and event stream, so there is no second build implementation.

This de-risks the Avalonia work, enables automation and CI, and gives the solver team a stable way to produce
test packages before Step 13.

## 2. Sizing and split-trigger check

**Estimate: 2.75–3.75 person-weeks** (revised 2026-11-17 from 3.0–4.0) for one experienced implementor, including
tests, documentation, and review. The roadmap row is **1–3 pw**; the design exceeds it because the command
surface grew after the roadmap was written: Step 10 added the no-write preview, Step 11 added the read-only
inspector/verifier, and Step 12 now owns seven JSON report contracts plus environment diagnosis. Feature 1 is
smaller than in the round-2 design because `System.CommandLine` replaces the command table, hand-written parser,
help writer, and completion writer with command definitions, `ParseResult` binding, parse-error mapping, and an
AOT smoke check; the remaining six features are unchanged.

Roadmap §3.1 check:

- **Features:** seven, within the “roughly 6–8” range. Each feature has a distinct observable outcome.
- **Duration:** the upper bound of 3.75 pw is below the “more than about four weeks” trigger.
- **Acceptance stories:** one coherent story — a headless build CLI. The auxiliary commands reuse the same
  command graph, report envelope, and build primitives; they are not independent solver/contract work.
- **Risk mix:** the step mixes low-risk command formatting with the high-risk package/publication path.
  That risk is owned by Steps 11 and 12 together and is why Step 12 follows Step 11 directly.
- **Audit sitting:** seven features and one library surface are auditable in one sitting if the feature log
  stays current.

**No split is proposed.** Splitting into 12A (CLI shell + build) and 12B (auxiliary commands + docs) would
leave a CLI without `inspect`, `verify`, `doctor`, or `clean --dry-run`, which is a worse deliverable for the
solver team and would add a roadmap step and change the Phase C closeout. **Owner decision (2026-11-16):**
the single-step exception, seven features, and no split were accepted at 3.0–4.0 pw; the parser decision on
2026-11-17 revises the estimate to 2.75–3.75 pw and the revision is presented for owner confirmation at the
round-3 start gate. `04-roadmap.md` §3/§3.1 and `05-open-decisions.md` record the exception and the revised
figure. **Contingency:** if features 1–3 exceed 2.25 pw, stop and ask the owner to re-open the split before
starting feature 4. The estimate is not a licence to exceed four weeks.

## 3. Original reference

- The GUI’s `SimulationLibraryBuilder` performed: template compatibility check → open/create `.msl` → reset
  model project → push project data → push customization → build model context → convert job templates →
  build job package models → add metadata → save.
- Progress was a `LibraryBuildStatus` enum with 25 states; cancellation used a `CancellationToken`; errors were
  strings in a log console.
- There was no headless CLI for model building; users had to use the GUI or write C# code.
- The original allowed create vs. append builds and a 99,999-job cap.
- The new implementation keeps the good decomposition (select recipe → compile/parameterize → expand → build
  lattices → write) but replaces the 25-state enum with a small typed event stream, turns string errors into
  stable diagnostics, and adds a real headless surface. Append builds remain out of scope; rebuild is safer
  for an immutable package.

## 4. Scope and non-goals

### 4.1 In scope

- One `mocassin` command surface for `new`, `validate`, `compile`, `build` (including `--preview`), `inspect`,
  `verify`, `doctor`, `clean --dry-run`, help, and version.
- One `System.CommandLine` command graph with typed `ParseResult` binding, stable `CLI001` usage errors, and
  explicit mapping from parser/handler outcomes to this project's exit codes.
- A shared, UI-independent build orchestration library with a typed stage/event stream, deterministic job
  selection, progress, cancellation, and parallel lattice generation.
- `build`/`inspect`/`verify` package work through the Step 11 writer and the read-only inspector/verifier.
- Build-template, job-template, and parameterization selection, including the Step 09 implicit default.
- Partial builds by full-expansion job ID, recorded in `schema_info.job_selection`.
- Atomic replacement with `--force`, temp cleanup, and post-publication verification.
- Stable exit codes and `CLI` diagnostics; the `build-v1` and per-command JSON reports; the JSON-Lines build
  events; generated schemas and fixtures.
- `doctor` environment checks and a `clean --dry-run` that never deletes.
- Documentation, parity-matrix rows, and the 10⁴-job evidence.

### 4.2 Non-goals

- **No GUI work.** Step 26 consumes the library; Step 12 does not add windows, dialogs, or view models.
- **No solver execution.** `mocassin simulate` is reserved for Steps 19/27; Step 12 does not launch
  `mocassin-sim`.
- **No results collection or evaluation.** `collect` (Step 20), `evaluate`/`export` (Step 21), and the full
  `clean` deletion behaviour (Step 28) are reserved. Step 12 implements only `clean --dry-run` because that
  is the safety-relevant half and it must not touch a package or a results store.
- **No `.msr` or `.mcs` work.** No results store is created, read, or written.
- **No package append.** `.msl` remains immutable; `--force` replaces, never appends.
- **No legacy formats.** No `.mocprj`/`.msl`/`.mcs` legacy reader or writer.
- **No parser framework in the library.** `System.CommandLine` is a CLI-only dependency; `Mocassin.Packages`,
  `Mocassin.Contracts`, and `Mocassin.Results` never reference it, and no scientific or build logic moves into
  command handlers.
- **No shell-completion command or scripts.** `mocassin completion` is not part of Step 12; shell integration
  is a Step 31 packaging decision (§5.2, §13.8).
- **No new package-format or signature change.** `job_selection` already exists in the Step 11 writer and
  schema; `build-v1` is a CLI report, not a package field.
- **No scientific algorithm in the CLI.** Selection, compilation, parameterization, expansion, lattice
  generation, writing, inspection, and verification are library calls; the CLI parses, sequences, formats, and
  maps outcome to exit codes.
- **No remote deployment, HPC scheduler, or extension loading.** Steps 19/26/28 own those.

## 5. Command surface and parser

### 5.1 Surface

```text
mocassin new <name> [-o <dir>] [--template blank|ceria] [--json]
mocassin validate <project.mocprj> [--json] [--warnings-as-errors] [--stats]
mocassin compile <project.mocprj> [-o <compiled-inspection.json>] [--json]
mocassin build <project.mocprj> [-o <project.msl>]
                [--preview]
                [--template <buildTemplateId>]
                [--job-template <jobTemplateId>]
                [--parameterization <parameterizationId>]
                [--jobs <range>]
                [--parallel <n>]
                [--compact]
                [--force]
                [--warnings-as-errors]
                [--quiet]
                [--json]
mocassin inspect <project.msl> [--json]
mocassin verify <project.msl> [--json]
mocassin doctor [--json]
mocassin clean <project.msl> [--dir <dir>] [--older-than <days>] --dry-run [--json]

mocassin --help
mocassin --version
mocassin <command> --help

# Reserved; the command graph recognises them and exits 2 with a clear "available in Step N" message.
mocassin simulate ...    # Step 19/27
mocassin collect ...     # Step 20
mocassin evaluate ...    # Step 21
mocassin export ...      # Step 21
mocassin clean ...       # deletion, Step 28
```

- `--help` and `--version` are provided by `System.CommandLine` actions: they are global only before a
  command; after a command they are command help. Both exit 0; help output shows the executable name.
- Options use the library's tokenizer: `--name value`, `--name=value`, and `-o value` are accepted; flags take
  no value; option names are case-sensitive. Numbers use invariant culture. `--parallel` accepts `1..256`;
  `--older-than` accepts `0..36500`.
- The library treats a value beginning with `-` as the value when the option expects one. `--jobs -1`,
  `--parallel -1`, and `-o -file` therefore reach the handlers as `-1`, `-1`, and `-file`; the first two are
  rejected by `CLI005`/range checks, and the last names a path. `--` ends option parsing so a positional path
  beginning with `-` can be passed. Feature 1 pins these conventions and the response-file (`@file`) behaviour
  with tests; the library's exact error sentence is not a contract (§5.2).
- `System.CommandLine` validates arity, required options/arguments, and option applicability before a handler
  runs. Handlers receive typed values; they never re-parse `string[]`.
- `--json` is a per-command option, not a global prefix. `validate` and the seven `reports`-backed commands
  (`new`, `compile`, `build`, `inspect`, `verify`, `doctor`, `clean`) print exactly one schema-valid report
  for every outcome, including a usage error. `--help`, `--version`, and the reserved commands
  (`simulate`, `collect`, `evaluate`, `export`, and full `clean`) do not accept `--json`; they print human
  usage/diagnostic text to stderr and exit 2 (or 0 for help/version), with no report. An unknown command is
  the same: human usage, exit 2, no report.
- `--json` switches stderr to JSON Lines only: human stage/progress lines are suppressed whenever `--json` is
  set, whether or not `--quiet` is used. `--quiet` additionally suppresses the `build.stage`,
  `build.progress`, and `log` events; diagnostics, `build.started`, `build.finished`, and the final report
  remain. `--quiet` does not suppress errors.
- `--version` prints the assembly informational version and the runtime description, then exits 0.
- Step 12 ships no `completion` command and no shell-completion scripts; the library's `[suggest]` directive is
  accepted but is not a contract (§13.8).

### 5.2 Parser choice

**Decision: `System.CommandLine` 3.0.0, pinned in central package management and referenced only by
`Mocassin.Cli`.** The human owner replaced the round-2 hand-written-parser decision on 2026-11-17 after an
AOT/full-trim verification showed that the design's stated reason (an AOT/trimming risk) was wrong.

Owner-supplied verification (`aot-check`, 2026-11-17; `dotnet` `11.0.100`, app TFM `net11.0`, RID `linux-x64`,
ILC `11.0.0`):

- 3.0.0 is the current stable release and is MIT-licensed (SPDX expression). Its `net10.0` asset is the one
  selected for `net11.0`; that asset declares `IsTrimmable` **and** `IsAotCompatible`. The `net10.0`
  dependency group is empty, so `dotnet list package --include-transitive` resolves only `System.CommandLine`.
  The `netstandard2.0` fallback pulls `System.Memory`; the repository targets `net11.0`, so that asset is not
  used, and it is untested.
- `dotnet publish -c Release -r linux-x64 -p:PublishAot=true -p:TrimMode=full` (also with
  `TrimmerSingleWarn=false`, `InvariantGlobalization=false`, and with
  `PublishTrimmed=true`/`TrimMode=full`) produced a native binary with **zero** IL2xxx/IL3xxx warnings. A
  positive control that called `JsonSerializer.Serialize(new object())` emitted IL2026 ×2 and IL3050 ×2,
  proving the trim/AOT analyzers and the ILC warning channel were active and not suppressed.
- The native binary parsed `--name value` and `--name=value`, negative numbers, required and typed options;
  printed help/version; reported parse errors with exit **1**; and answered the `[suggest]` directive the same
  way as the JIT build. The package's `build/` target supplies
  `System.CommandLine.ExecutableName`, so `--help` shows the executable name in the native binary.
- The owner's review also cites the Microsoft Learn page updated 2025-12-04, which describes the library as
  trim-friendly and AOT-capable.

Consequences recorded in this design:

- **Parse errors must be remapped.** `System.CommandLine` returns 1 for a parse error and 0 for its
  help/version actions. The CLI never exposes the library's parse-error code: `CliApplication` inspects
  `ParseResult.Errors`, emits `CLI001`, and returns **2**; help/version return 0; handler results map through
  §11.1. The CLI owns the stable `CLI001` message and may keep the library text as a hint; tests assert the
  CLI-owned text and the offending token, not the library's exact sentence.
- **Completion is narrowed, not reimplemented.** 3.0.0 provides the `[suggest]` protocol consumed by
  `dotnet-suggest`, not shell-script generation. `dotnet-suggest` is not installed in the development image,
  and the verification observed `[suggest]` returning root-level tokens only at every tested command
  position. Step 12 therefore ships **no `mocassin completion` command and no scripts**; the directive is
  accepted but is not a contract, and shell integration is a Step 31 packaging decision (§13.8).
- **AOT smoke test.** Feature 1 adds a minimal AOT smoke host under `tests/aot/` that references
  `System.CommandLine` 3.0.0 and defines a representative command/option/argument shape. It is published for
  `linux-x64` with `PublishAot=true`, `TrimMode=full`, and strict trim warnings; the check asserts zero
  IL2xxx/IL3xxx warnings, runs the positive control (which must emit IL2026/IL3050), and runs the native
  binary for `--help`, `--version`, and one parse-error case. The real `Mocassin.Cli` publish is also
  attempted and recorded as closeout evidence, but it is **not** a pass/fail gate in Step 12:
  `Microsoft.Data.Sqlite` and `MessagePack` are not verified for AOT, so dependency-owned warnings are
  attributed and Step 31's gated decision owns the final platform matrix and the AOT/single-file choice. Only
  `linux-x64` is exercised; Windows/macOS AOT and the `netstandard2.0` asset are untested.
- **Executable name.** The CLI sets the root command name (and `ToolCommandName` where the package's build
  target reads it) to `mocassin`, so help/usage and the native binary show `mocassin`, not `Mocassin.Cli`.
  Feature 1 asserts it in the help test.
- **Dependency policy.** The version is pinned in `Directory.Packages.props` at implementation time and is
  referenced only by `Mocassin.Cli`. The NuGet licence gate covers the MIT expression;
  `requireLicenseAcceptance=true` is package metadata and does not prompt in this repository's
  non-interactive restore. No transitive package is added for `net10.0+`. The library is not added to
  `Mocassin.Packages`, `Mocassin.Contracts`, `Mocassin.Results`, or any test project except through the CLI.

The Step 07 `validate` and Step 10 `build --preview` hand-written parsers move onto the shared graph in
feature 1; feature 3 folds preview into `build`.

## 6. Build pipeline and event stream

### 6.1 Home and types

The orchestration lives in `Mocassin.Packages.Build` (namespace `Mocassin.Packages.Build`), not in
`Mocassin.Compilation` and not in `Mocassin.Cli`:

- `Mocassin.Compilation` is a pure, synchronous compiler with no file I/O, templates, jobs, or packages.
- `Mocassin.Cli` owns parsing, formatting, and exit-code policy; Step 26’s view models must be able to call
  the same pipeline without referencing the CLI.
- `Mocassin.Packages` already owns project persistence, parameterization, job expansion, supercells, and the
  `.msl` writer. Build orchestration is the sequencing of those capabilities.

Public surface (XML-documented; added to `PublicAPI.Unshipped.txt`; the ApiCompat baseline is refreshed
only if an intentional rebaseline is recorded):

```csharp
namespace Mocassin.Packages.Build;

public interface IBuildPipeline
{
    IAsyncEnumerable<BuildEvent> BuildAsync(BuildRequest request, CancellationToken cancellationToken = default);

    Task<BuildResult> BuildToCompletionAsync(BuildRequest request, CancellationToken cancellationToken = default);
}

public sealed record BuildRequest
{
    public required string ProjectPath { get; init; }
    public string? TargetPath { get; init; }             // required unless Preview
    public BuildTemplateId? BuildTemplateId { get; init; }
    public JobTemplateId? JobTemplateId { get; init; }
    public ParameterizationId? ParameterizationId { get; init; }
    public string JobSelection { get; init; } = "all";
    public bool Preview { get; init; }
    public bool Overwrite { get; init; }
    public bool Compact { get; init; }
    public int MaxDegreeOfParallelism { get; init; } = Environment.ProcessorCount;
    public bool WarningsAsErrors { get; init; }
    public DateTimeOffset? CreatedUtc { get; init; }
    public BuildOptions Options { get; init; } = new();
}

public sealed record BuildOptions
{
    public TimeSpan ProgressInterval { get; init; } = TimeSpan.FromMilliseconds(100);
    public TimeProvider TimeProvider { get; init; } = TimeProvider.System;
    public int EventChannelCapacity { get; init; } = 256;   // tests may lower it
}

public enum BuildStage
{
    Load = 0,
    Select = 1,
    Validate = 2,
    Compile = 3,
    ValidateBuild = 4,
    Parameterize = 5,
    ExpandJobs = 6,
    GenerateAndWriteJobs = 7,
    Publish = 8,
}

public enum BuildStatus { Succeeded = 0, Failed = 1, Cancelled = 2 }

public enum BuildFailureKind
{
    None = 0, Io, Load, TemplateSelection, JobSelection, Compilation, Validation, Parameterization,
    JobExpansion, LatticeGeneration, Preview, PackageWrite, Internal,
}

public sealed record BuildIdentity(
    Guid? ProjectId,
    string? ModelHash,
    string? DocumentHash,
    string? ModelSignature,
    string? ParameterizationSignature);

public sealed record BuildPreview(
    string JobTemplateId,
    string JobTemplateName,
    int TotalJobs,
    ImmutableArray<BuildPreviewCollection> Collections,
    TimeSpan Elapsed);

public sealed record BuildPreviewCollection(
    string Id,
    string Name,
    string SimulationId,
    string Kind,
    bool UseDualDofCorrection,
    int JobCount,
    int ConfigCount,
    ImmutableArray<BuildPreviewOptimizer> Optimizers,
    ImmutableArray<BuildPreviewConfig> Configs);

public sealed record BuildPreviewOptimizer(
    string SiteId,
    string ParticleId,
    int TargetCount,
    int? RemovedStartOptions);

public sealed record BuildPreviewConfig(
    string Id,
    string Name,
    int JobCount,
    int CellA,
    int CellB,
    int CellC,
    int PositionCount,
    double TotalChargeBefore,
    double TotalChargeAfter,
    bool IsChargeNeutral,
    ImmutableArray<BuildPreviewDoping> Dopings);

public sealed record BuildPreviewDoping(
    string DopingId,
    int Priority,
    double RequestedFraction,
    int AvailablePrimary,
    int RequestedPrimary,
    int AppliedPrimary,
    int AppliedDependent,
    double EffectiveFraction);

public abstract record BuildEvent;
public sealed record BuildStarted(int ExpandedJobCount, int SelectedJobCount) : BuildEvent;
public sealed record BuildStageStarted(BuildStage Stage) : BuildEvent;
public sealed record BuildStageCompleted(BuildStage Stage, TimeSpan Elapsed) : BuildEvent;
public sealed record BuildProgress(BuildStage Stage, int Completed, int Total, string? Current, TimeSpan Elapsed)
    : BuildEvent;
public sealed record BuildDiagnostic(Diagnostic Diagnostic) : BuildEvent;
public sealed record BuildLog(string Level, string Message) : BuildEvent;
public sealed record BuildFinished(BuildResult Result) : BuildEvent;

public sealed record BuildResult(
    BuildStatus Status,
    BuildFailureKind Failure,
    BuildIdentity Identity,
    ImmutableArray<Diagnostic> Diagnostics,
    BuildSelection? Selection,
    JobSelectionResult? JobSelection,
    BuildPreview? Preview,
    PackageWriteResult? Package,
    ImmutableArray<BuildStageTiming> Timings,
    TimeSpan Elapsed);

public sealed record BuildStageTiming(BuildStage Stage, TimeSpan Elapsed);

public sealed record BuildSelection(
    BuildTemplate? BuildTemplate,
    JobTemplate JobTemplate,
    ParameterizationTemplate? Parameterization,
    bool ImplicitDefaultParameterization,
    string? OutputName);

public sealed record JobSelectionResult(string Expression, ImmutableArray<int> JobIds, int ExpandedCount);
```

- `BuildFailureKind.None` accompanies `Succeeded` and `Cancelled`. `Io` covers project/output/disk and writer
  I/O; `Load` covers a parse, content, or load-time validation failure; `JobSelection` covers a malformed,
  empty, or out-of-range `--jobs` expression; `Preview` covers a preview evaluation that returns invalid
  lattice/doping data. The CLI uses the diagnostic code to distinguish `PROJ010` (unknown template reference,
  exit 1) from `CLI004` (ambiguous or wrong-kind selection, exit 2) and `CLI005` (job selection, exit 2).
- `BuildResult.Identity` is the authoritative source for the `build-v1` `projectId`, `modelHash`,
  `documentHash`, `modelSignature`, and `parameterizationSignature` fields. Each value stays `null` until its
  stage has run, so an early failure or cancellation still produces a valid report; the CLI never recomputes
  a hash or signature for the report.
- **`BuildResult.Preview` is the single preview output.** When `BuildRequest.Preview` is true, the pipeline
  evaluates the selected configurations **inside the pipeline**, after expansion and `--jobs` filtering, with
  the injected `ILatticePreviewService` and the same mapping code that produces the Step 10 preview. The
  result is a typed `BuildPreview` payload (job template, total, collections/configs/optimizers/dopings,
  elapsed time); it is `null` in a full build. The CLI and Step 26 GUI map this payload to their own views;
  neither reloads the project nor recompiles the model, and no second preview implementation exists. A
  preview-evaluation error is `BuildFailureKind.Preview` and exit 1.
- `BuildPreview.Elapsed` is the preview evaluation time. The `build-v1` `preview` timing entry comes from it;
  it is not a `BuildStage`, emits no `build.stage` event, and is not part of `BuildResult.Timings` (which lists
  pipeline stages only).
- `BuildResult.Diagnostics` is deduplicated, deterministically sorted, and authoritative. The
  `BuildDiagnostic` events are for live feedback and are not the report source. The library does not add
  CLI-only diagnostics such as `CLI006`; the CLI composes those into the report.
- `BuildResult.Package` carries the Step 11 `PackageWriteResult` (path, SHA-256, counts, size, advisory
  warnings). The `build-v1` `package.published` flag is `Package is not null`; a post-publication
  cancellation therefore reports `published = true` with the package object intact.
- `BuildRequest.CreatedUtc` is the build timestamp; `null` means `request.Options.TimeProvider.GetUtcNow()`.
  The CLI passes `null`; tests pass a fixed value for determinism.
- `BuildRequest.Options` is the only configuration injection point for `ProgressInterval`, `TimeProvider`, and
  the test-tunable `EventChannelCapacity`. The pipeline reads `request.Options` at the start of each call; it
  does not read ambient/static configuration and has no options constructor.
- `BuildPipeline` is constructed with production services by default: a `LatticePreviewService`, a
  `ModelCompiler`, a `ParameterizationService`, and a `ProjectValidationPipeline` built with
  `additionalStages: [new JobTemplateValidationStage(previewService)]`. The same preview service instance is
  shared by the build-stage validation and the preview builder. The production constructor never runs
  `RunBuildStage = true` on a default pipeline, where it would be a silent no-op. An internal constructor
  accepts the services for real-object tests.
- The library never chooses an exit code, prints to the console, or reads `Environment.ExitCode`.

### 6.2 Ordered stages

```text
1  Load            new ProjectStore(options: new ProjectStoreOptions { ValidateOnLoad = false })
                     .LoadAsync(projectPath)                  // load diagnostics, no model validation
2  Select          build template / job template / parameterization, output path
3  Validate        validationPipeline.Validate(
                     project: project,
                     loadDiagnostics: loadResult.Diagnostics,
                     options: new ValidationOptions {
                         RunBuildStage = false,
                         WarningsAsErrors = request.WarningsAsErrors },
                     cancellationToken: cancellationToken)    // stages 1–4
4  Compile         IModelCompiler.Compile(project, options, cancellationToken)
5  ValidateBuild   validationPipeline.Validate(
                     project: project,
                     loadDiagnostics: loadResult.Diagnostics,
                     options: new ValidationOptions {
                         RunBuildStage = true,
                         WarningsAsErrors = request.WarningsAsErrors },
                     compiledModel: compilation.Model,
                     cancellationToken: cancellationToken)    // stages 1–5
6  Parameterize    IParameterizationService.Validate, then Apply (or the implicit default);
                     promote warnings per §13.3
7  ExpandJobs      JobTemplateExpander.Expand over the complete template; apply JobSelection
   Preview         (request.Preview only) BuildPreviewBuilder over the selected descriptors; stop
8  Generate+Write  parallel SupercellGenerator + serialized package writes
9  Publish         ISimulationPackageWriter.CompleteAsync (checkpoint, fsync, atomic rename)
```

- The build uses `LoadAsync` plus `Validate(project, loadDiagnostics, ...)`, not `ValidateFileAsync`, so the
  project is loaded exactly once. `mocassin validate` keeps `ValidateFileAsync`, which constructs the same
  `ValidateOnLoad = false` store internally.
- `validationPipeline` is constructed as
  `new ProjectValidationPipeline(additionalStages: [new JobTemplateValidationStage(previewService)])`, using
  the same `previewService` instance as the preview builder. This registration is mandatory: a default
  `ProjectValidationPipeline` has no Build stage, so `RunBuildStage = true` alone is a silent no-op.
- Both `Validate` calls use named arguments (`loadDiagnostics:`, `options:`, `compiledModel:`,
  `cancellationToken:`) so the optional compiled model cannot be mistaken for the token, and the build token
  reaches validation. This follows the accepted Step 10 audit note R2-5.

- **Validate, compile, then validate the build stage.** The pre-compile call runs load/model/cross-domain/
  feasibility and fails a model error with exit 1 before paying for a compile. The post-compile call runs the
  same pipeline with `RunBuildStage = true` and the compiled model, so `JobTemplateValidationStage` can run
  its compiled-bound job, position, optimizer, and doping checks. The second call re-runs stages 1–4, which
  were already clean; the ceria cost is below the Step 07 200 ms budget and the report deduplicates exact
  `(code, path, message)` triples. This uses the existing pipeline entry point and adds no second validation
  API.
- `validate` never compiles and never runs the build stage; `build` and `build --preview` do.
- Selection runs before compilation so a missing or ambiguous template fails without paying for a compile.
- Expansion runs over the **complete** template before `--jobs` filtering, so IDs, seeds, and lattice
  requests are identical to a full build.
- `BuildStarted` is emitted after expansion, when both counts are known. `BuildFinished` is emitted exactly
  once in a `finally`, including for early load/selection failures and cancellation. Consequently, the
  `build.stage` events for load/select/validate/compile/validateBuild/parameterize/expand precede
  `build.started`; `build.started` means “job generation starts and the selected total is known”, not “the
  process started”. Consumers must accept stage events before it (§12.5).
- Stage events are emitted for every started/completed stage; a skipped stage after a failure is not emitted.
- Cancellation is checked at every stage boundary, before each job, and inside the compiler, validation,
  parameterization, preview, and writer calls that accept a token.
- Errors stop the build. Warnings are collected, emitted as events, and reported at the end.
- `--preview` runs stages 1–7, then evaluates the selected configurations inside the pipeline through
  `BuildPreviewBuilder`/`ILatticePreviewService`, stores the typed result in `BuildResult.Preview`, and stops.
  It never generates duplicate-job lattices, opens a writer, or creates a file. The preview evaluation emits
  no `build.stage` event; its time is `BuildPreview.Elapsed`.
- Post-publication verification is **not** a pipeline stage: it is composed by the caller (CLI or GUI) with
  `IPackageVerifier` so `Mocassin.Packages` does not gain a dependency on `Mocassin.Results`. The `verify`
  timing is therefore caller-composed and is not in `BuildResult.Timings`.

### 6.3 Event delivery and backpressure

- The pipeline writes events to a bounded `Channel<BuildEvent>` whose capacity is
  `request.Options.EventChannelCapacity` (default 256, `FullMode = Wait`). A slow consumer slows the build
  instead of silently losing a completion or diagnostic event.
- Progress is throttled by `request.Options.ProgressInterval` (default 100 ms) using
  `request.Options.TimeProvider`; the last progress for a stage is always emitted. A 10⁴-job build therefore
  emits roughly 10 events per second, not 10⁴.
- `BuildOptions` is always reached through `BuildRequest.Options`; the defaults are production values, and
  tests set a fixed clock, a lower capacity, or a longer interval on the request. There is no ambient clock or
  static configuration.
- The CLI and GUI consume `IAsyncEnumerable<BuildEvent>`; `BuildToCompletionAsync` is a convenience that
  drains the stream and returns the final result.

### 6.4 Parallelism and determinism

- `--parallel <n>` maps to `CompilationOptions.MaxDegreeOfParallelism` and to the lattice-generation degree.
- Lattice generation for the selected jobs runs in parallel; package writes are serialized in **ascending
  `JobId`** order. Completed jobs wait in a bounded reorder window of `max(2, 2 × parallel)` entries, so
  memory is `O(parallel × largest lattice)` plus the window.
- Output order, job rows, lattice deduplication, signatures, and `package-summary-v1` are identical for
  `--parallel 1` and `--parallel N`. The scheduler never lets insertion order depend on completion order.
- The writer is the only component that touches the temp SQLite database; it is never called concurrently.
- `--compact` passes `CompressionLevel = 9` and `Vacuum = true` to the writer. It changes compressed bytes
  and file size, never payload content, signatures, or the canonical summary.

## 7. Build selection

### 7.1 Recipes and precedence

| Input | Meaning |
| --- | --- |
| `--template <buildTemplateId>` | A project `BuildTemplate`: a named `JobTemplateId` + `ParameterizationId` + `OutputName`. |
| `--job-template <jobTemplateId>` | A project job template. Overrides the build template’s job template. |
| `--parameterization <parameterizationId>` | A project parameterization template. Overrides the build template’s parameterization. |

Resolution:

1. If `--template` is given, resolve the build template by ID. A value that matches a **job** template but
   not a build template is `CLI004` with a hint to use `--job-template`. An unknown ID is a `PROJ010`
   reference error (exit 1).
2. If no `--template` is given and the project has exactly one build template, use it. If it has several,
   `CLI004`/exit 2 lists their IDs and names.
3. If no build template is selected (or none exists), the effective job template comes from
   `--job-template`; otherwise exactly one project job template is used; otherwise `CLI004` lists the IDs.
4. The effective parameterization comes from `--parameterization`; otherwise the selected build template’s
   `ParameterizationId`; otherwise exactly one project parameterization; otherwise, if the project has no
   parameterizations, the implicit default (`null` template, no `PRM003`/`PRM006`); otherwise `CLI004` lists
   the IDs.
5. `--job-template`/`--parameterization` may be combined with `--template` for experimentation. The report
   records the effective IDs, not the authored recipe, so a build is reproducible from the report.
6. A project with no job template cannot expand jobs: `CLI004`/exit 2 with a hint that a job template is
   required. A project with job templates but no `buildTemplates` is the normal Step 10/12 sample case and
   is selected automatically when there is exactly one job template.

The CLI reporter passes the effective selection to `BuildRequest`; the pipeline repeats the resolution
defensively and returns the same diagnostics. The GUI uses the same resolver.

### 7.2 Implicit default parameterization

- No template means the Step 09 implicit default: all pair/group values `0 eV`, kinetic rule frequencies the
  compiled `1e13 Hz`, MMC rules at the compiled default, and the authored site-energy background.
- The build report sets `parameterization.implicitDefault = true` and `parameterization.id = null`.
- `PRM003` and `PRM006` are not emitted for the implicit default. An explicit empty template still emits both
  and may still build; `--warnings-as-errors` turns them into exit 4.

### 7.3 Output path

- `-o <path>` is used verbatim (after `Path.GetFullPath`).
- Without `-o`, a selected build template’s `OutputName` is used, with `.msl` appended if missing. The name
  is treated as a file-name stem: a value containing `/`, `\`, or `..` is `CLI004`.
- Without a build template, the default is `<project-file-stem>.msl` in the project’s directory.
- `--preview` accepts no `-o`; it never writes. `--force` and `--compact` are write options and are rejected
  with `--preview` (`CLI001`/exit 2).

## 8. Job selection and `job_selection`

### 8.1 Grammar

```text
selection := "all" | item ("," item)*
item      := integer | integer "-" integer
```

- IDs are 1-based and refer to the complete template expansion.
- Tokens are trimmed; empty tokens, non-integers, `0`, negative values, and descending ranges are `CLI005` /
  exit 2.
- Duplicates are removed; overlapping and adjacent ranges are merged; the result is sorted ascending.
- An ID above the expanded count is `CLI005` with the expanded count in the message.
- An empty selection after parsing is `CLI005`.
- `--jobs all` and omitting `--jobs` are equivalent.

### 8.2 Normal form and metadata

The canonical expression is `all` when every expanded job is selected; otherwise a comma-joined, sorted,
merged form with no spaces (`1-3,5,7-9`). The normal form — not the user’s text — is written to
`schema_info.job_selection` by the Step 11 writer. Therefore:

- `--jobs 1,2,3` and `--jobs 1-3` produce the same `job_selection` and the same package;
- `--jobs 1-1000` on a 1000-job template normalizes to `all`;
- `--jobs 5,2,2` normalizes to `2,5` and writes exactly those IDs with a gap;
- `inspect` shows the recorded expression, so a partial package is self-describing.

Selection is applied after full expansion and before lattice generation. Unselected jobs are never generated,
written, or verified. `job_id` values remain the full-expansion IDs, so a solver `--job` argument from a
campaign script stays stable across partial and full builds.

`build --preview` also accepts `--jobs`; it filters the previewed jobs and reports `expandedCount` and
`selectedCount`. A preview never changes `job_selection` because it never writes.

The CLI pre-validates the selection syntax before loading for a fast error; the pipeline parses
`BuildRequest.JobSelection` again after expansion, where the expanded count is known. A malformed, empty, or
out-of-range selection from either path is `CLI005` and `BuildFailureKind.JobSelection`, exit 2; the pipeline
never generates or writes an unselected job.

## 9. Output, overwrite, compact, and publication

- `build` refuses an existing target unless `--force` is given: `CLI003`, exit 3, and the existing file is
  left byte-identical. The CLI checks before loading; the writer’s `PackageBuildOptions.Overwrite` is the
  final guard against a race.
- `--force` maps to `Overwrite = true`. The writer builds in `<target>.tmp-<pid>-<random>` and publishes by
  checkpoint, `journal_mode=DELETE`, file fsync, Unix directory fsync, and atomic rename. A failure or
  cancellation before publication deletes the temp file and preserves the previous package.
- A failure or cancellation **after** publication leaves the new package on disk; the report says
  `published = true` and `verification = "skipped"`. This is not rolled back, because the package is already
  a valid atomic artifact and a rollback could lose the only copy.
- `--compact` = Zstd quality `9` + `VACUUM` before publication. It is a size/latency trade, not a format
  change; payload content, signatures, job IDs, and `package-summary-v1` are unchanged. The Step 11 page’s
  “`--compact` runs VACUUM” and the Step 11 amendment’s “level 9” are both honoured by this preset.
- `compile -o` replaces its derived inspection file atomically (temp + rename). It is a report, not an
  immutable package, so `--force` is not required. A directory at the target path is exit 3.
- `new` refuses an existing project file without a `--force` option. It creates a temp file and moves it into
  place without overwrite, so two concurrent `new` invocations cannot silently replace each other.

## 10. Progress, ETA, throttling, and quiet

Human mode writes stage/progress lines to **stderr** and the final summary to **stdout**. Progress on stderr
keeps stdout scriptable and matches the `--json` split.

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
Package written: /tmp/ceria.msl (2.1 MB, 6 jobs, selection all)
```

- The full build has ten rendered steps: nine pipeline stages plus the CLI’s verification stage.
  `--preview` skips `GenerateAndWriteJobs`, `Publish`, and `Verify` and renders eight steps, ending with a
  “Previewing configurations” line; the renderer computes the denominator from the plan, not a constant.
  The preview line’s elapsed time is `BuildResult.Preview.Elapsed`, not a `BuildStage`.
- The `GenerateAndWriteJobs` line is the only bar; it reports `completed/total` selected jobs and the
  current `JobId` or collection/config name.
- ETA is computed by `ProgressEstimator` from a rolling window of at least 8 and at most 64
  `(elapsed, completed)` samples, using `request.Options.TimeProvider`. It is display-only and never enters a
  report field that affects reproducibility; the JSON event may carry `etaS` as an advisory number.
- Progress is throttled to `request.Options.ProgressInterval`; the final progress of a stage is always emitted.
- `--json` suppresses all human stage/progress lines and makes stderr JSON Lines only. `--quiet` additionally
  suppresses `build.stage`, `build.progress`, and `log`; neither option suppresses diagnostics, the final
  result, or errors.

## 11. Errors, exit codes, and `CLI` diagnostics

### 11.1 Exit codes

| Exit | Meaning | Emitters |
| ---: | --- | --- |
| 0 | Success (warnings allowed unless promoted) | all commands |
| 1 | Validation, model, package-invalid, or unhealthy-doctor result | `validate`, `build` validation/expansion/lattice/verification, `inspect` invalid package, `verify` invalid package, `doctor` fail |
| 2 | Usage or argument error | parse errors mapped by `CliApplication`, template/parameterization selection ambiguity, `--jobs` syntax/range, reserved command/option, preview with a write option |
| 3 | I/O error (project, output, disk, missing/unreadable input, existing target without `--force`) | load/save, writer, `new`, `compile -o`, `inspect`/`verify` missing file |
| 4 | Compilation or parameterization error | `build`, `compile` `CMP`, `PRM` errors, promoted `PRM` warnings |
| 5 | Partial success | **reserved for `simulate`/`collect`; not emitted by `build` in v1** |
| 6 | Cancelled by the user | `build`/`build --preview` before or after publication |
| 10 | Unexpected internal error | any command after an unhandled exception |

- `build` is **all-or-nothing**: if a selected job fails lattice generation or preview evaluation, the build
  writes nothing and exits 1; I/O failures exit 3; compilation and parameterization failures exit 4; an
  unexpected writer or internal failure exits 10. It never publishes a package that silently omits requested
  jobs. Exit 5 exists for future multi-job commands that process independent jobs; it is recorded here so
  Steps 19/20 share one table.
- The CLI maps every `BuildResult` in one documented place; the library never supplies an exit code:

  | `BuildResult` | Exit |
  | --- | ---: |
  | `Status = Succeeded` | 0 |
  | `Status = Cancelled` | 6 |
  | `Failure = None` with `Status = Failed` (contract violation) | 10 |
  | `Failure = Io` | 3 |
  | `Failure = Load` | 1 |
  | `Failure = TemplateSelection` with any `CLI004` diagnostic | 2 |
  | `Failure = TemplateSelection` with only a `PROJ010` diagnostic | 1 |
  | `Failure = JobSelection` with any `CLI005` diagnostic | 2 |
  | `Failure = Validation`, `JobExpansion`, `LatticeGeneration`, or `Preview` | 1 |
  | `Failure = Compilation` or `Parameterization` | 4 |
  | `Failure = PackageWrite` with a `PKG` error diagnostic | 1 |
  | `Failure = PackageWrite` with no I/O or `PKG` error cause | 10 |
  | `Failure = Internal`, or any unhandled exception at the command boundary | 10 |

  The pipeline classifies `ProjectLoadFailureKind.NotFound`/`TooLarge`/`Io` as `Failure = Io` and
  `InvalidContent` as `Failure = Load`. It classifies a writer `IOException`/`UnauthorizedAccessException` as
  `Io`, other writer exceptions as `Internal`, and reserves `PackageWrite` for a writer-returned `PKG` error
  (none in v1; the row above keeps the mapping total). A promoted parameterization warning is
  `Failure = Parameterization`, because promotion happens before the writer opens. A post-publication
  verification failure is not a `BuildResult` failure: the CLI keeps `Status = Succeeded` (or `Cancelled`),
  sets `verification = "failed"`, and exits 1.
- `CLI006` is composed by the CLI, not by the pipeline. The library reports `Status = Cancelled` with
  `Failure = None`; the CLI adds the `CLI006` info diagnostic to the report and exits 6. The GUI consumes the
  same status without a CLI-specific diagnostic.
- `validate` keeps its Step 07 contract (0/1/2/3) and its `validation-v1` report. The other commands use
  the table above; `build --preview` adopts the build table (compile/parameterization errors become 4), which
  replaces the Step 10 interim preview mapping.
- `CLI001` is composed by the CLI for every parse error. `System.CommandLine` returns 1 from its default
  invocation path; `CliApplication` never returns that value. It reads `ParseResult.Errors`, emits one
  `CLI001` per error, writes human usage text (or the command report when `--json` applies) and returns 2.
  Help and version are library actions and return 0. Handler results map through the table above.
- Usage errors with `--json` print one command report with `ok: false` and `exitCode: 2` for `validate` and
  the seven `reports`-backed commands. `--help`, `--version`, and reserved commands do not accept `--json`
  and print no report (§5.1).
- A missing project file is 3 for `build`/`compile`/`validate`; an unparseable project is 1. A missing
  `.msl` is 3 for `inspect`/`verify`; a readable-but-corrupt `.msl` is 1 with `PKG`/`CLI009` diagnostics.
- An unexpected exception is caught at the command boundary, mapped to `CLI007`/exit 10, and prints the
  exception type and message. A full stack trace is printed only when `MOCASSIN_DEBUG=1`.

### 11.2 `CLI` diagnostic codes

The CLI owns codes for failures that are not project/model/package diagnostics. They live in
`Mocassin.Contracts.Cli` (`CliDiagnosticCodes`, `CliDiagnosticDescriptors`) so that
`Mocassin.Packages.Projects.Validation.DiagnosticCatalog.Default` can compose them and the existing
docs-drift test covers them without a `Packages -> Cli` edge.

| Code | Severity | Meaning |
| --- | --- | --- |
| `CLI001` | error | Usage/argument error: unknown command or option, missing value, conflicting options, invalid option combination. |
| `CLI002` | error | I/O failure with no domain diagnostic: unreadable file, write failure, missing output directory, disk error. |
| `CLI003` | error | Output already exists and overwrite was not requested (`--force`), or `new` would replace a file. |
| `CLI004` | error | Build template, job template, or parameterization selection is missing, ambiguous, or the wrong kind. |
| `CLI005` | error | `--jobs` selection is malformed, empty, or out of range. |
| `CLI006` | info | The operation was cancelled by the user. |
| `CLI007` | error | Unexpected internal error. |
| `CLI008` | error | The requested command or option is reserved for a later step (`simulate`, `collect`, `evaluate`, `export`, full `clean`, `--results`). |
| `CLI009` | error | The package cannot be inspected or read as a package; run `mocassin verify` for `PKG` detail. |

Domain diagnostics keep their own codes: `PROJ` for load/save/reference errors, `STR`/`TRN`/`ENM`/`SIM`
for validation, `CMP` for compilation, `PRM` for parameterization, `JOB` for job templates/expansion/lattice,
and `PKG` for package verification. The CLI never rewrites a domain diagnostic into `CLIxxx`.
`System.CommandLine` parse errors are the only source of `CLI001`; the CLI's message is the stable text and
the library's sentence is an optional hint.

Human errors include the stage, object path, a one-line message, and the descriptor hint. The final summary
prints the raw error/warning/info counts and the exit code.

## 12. JSON reports and JSON Lines events

### 12.1 Report family

`validate --json` keeps the stable `validation-v1` report. The other commands use one report per command,
registered under `reports` in `contracts/manifest.json`:

| Report id | `kind` | Command | Generated schema |
| --- | --- | --- | --- |
| `build` | `build` | `build`, `build --preview` | `build-v1.schema.json` |
| `new-project` | `newProject` | `new` | `new-project-v1.schema.json` |
| `compile` | `compile` | `compile` | `compile-v1.schema.json` |
| `inspect` | `inspect` | `inspect` | `inspect-v1.schema.json` |
| `verify` | `verify` | `verify` | `verify-v1.schema.json` |
| `doctor` | `doctor` | `doctor` | `doctor-v1.schema.json` |
| `clean` | `clean` | `clean --dry-run` | `clean-v1.schema.json` |

Every report shares:

```json
{
  "schemaVersion": 1,
  "kind": "build",
  "ok": false,
  "exitCode": 1,
  "summary": { "errors": 1, "warnings": 2, "infos": 0 },
  "diagnostics": []
}
```

- `ok` is `exitCode == 0`. `summary` counts the deduplicated diagnostics.
- `diagnostics` uses exactly the `validation-v1` element shape: `code`, `severity`, `message`, optional
  `path` (`domain`, `objectId`, optional `property`), optional `hint`, optional `related`.
- Reports are serialized to a buffer and written once to stdout, so a failure cannot truncate the object.
- Numbers use invariant shortest round-trip formatting; JSON uses camelCase and two-space indentation.
- A future field is additive within `schemaVersion = 1`; an unknown field is ignored by consumers.

**Usage-error and early-failure reports.** `validate` and each report-backed command emit one report for every
outcome, including a usage error before any work starts. `--help`, `--version`, and the reserved commands do
not accept `--json` and never emit a report (§5.1). The shared envelope is always populated; every
command-specific field is still present but `null`, `0`, `false`, `[]`, or `"skipped"` as its type allows.
`required` in the generated schema means “present”, not “non-null”: the report descriptors mark payload
objects, strings, and identities nullable, and their nested objects are present with null fields. For
`build-v1`, `mode` defaults to `build`, `selection` and `jobSelection` are `null`, `timings` is `[]`,
`package` and `preview` are `null`, and `verification` is `skipped`. This is why a usage error with `--json`
is schema-valid rather than a shortened envelope.

### 12.2 `build-v1`

`build-v1` replaces the Step 10 interim `build-preview-v1`. It has one schema for both modes:

| Field | Type | Required | Meaning |
| --- | --- | --- | --- |
| `mode` | string enum `build`, `preview` | yes | Full build or no-write preview. |
| `projectId` | UUID or null | yes | Project identity. |
| `modelHash` | string or null | yes | Step 01 canonical model hash. |
| `documentHash` | string or null | yes | Step 01 canonical document hash. |
| `modelSignature` | string or null | yes | Step 08 compiled-model signature. |
| `parameterizationSignature` | string or null | yes | Step 09 `models.signature`, null when parameterization did not run. |
| `selection` | object or null | yes | Effective recipe (§12.3); null before selection or on a usage error. |
| `jobSelection` | object or null | yes | `{ expression, expandedCount, selectedCount }`; null before expansion, and `expression` is null on a usage error. |
| `timings` | array | yes | `[{ stage, ms }]` in pipeline order, plus `preview` in preview mode and `verify` in a full build. |
| `package` | object or null | yes | Published package summary; null in preview and before publication. |
| `preview` | object or null | yes | Direct projection of `BuildResult.Preview`; null in a full build and before preview evaluation. |
| `verification` | string enum `passed`, `failed`, `skipped` | yes | Post-publication verification result. |

`package` fields: `path`, `sha256`, `sizeBytes`, `jobCount`, `modelCount`, `latticeCount`,
`latticeReuseFactor`, and `published` (`Package is not null`; false before publication). `preview` is the
typed `BuildPreview` payload: `jobTemplateId`/`jobTemplateName` (the effective **job** template), `totalJobs`,
and `collections[]` with `id`, `name`, `simulationId`, `kind`, `useDualDofCorrection`, `jobCount`,
`configCount`, `optimizers[]`, and `configs[]` (`id`, `name`, `jobCount`, `cellA`, `cellB`, `cellC`,
`positionCount`, `totalChargeBefore`, `totalChargeAfter`, `isChargeNeutral`, `dopings[]`). The CLI maps this
payload field-for-field; it does not reload the project, recompile the model, or call the preview service
again. `build-v1` does not carry a package-size estimate; after a full build, `package.sizeBytes` is the
authoritative measured size, and a preview is explicitly size-neutral.

`timings` uses the lower-case stage names `load`, `select`, `validate`, `compile`, `validateBuild`,
`parameterize`, `expand`, `preview`, `generate`, `publish`, and `verify`, in that order, omitting stages that
did not run. `preview` comes from `BuildResult.Preview.Elapsed` and is produced inside the pipeline; `verify`
is caller-composed. Neither is a pipeline `BuildStage` or a stage event. `BuildPreview.TotalJobs` is the
selected previewed job count and its collections/configs reflect only selected jobs;
`jobSelection.expandedCount` is the complete expansion and `jobSelection.selectedCount` matches
`TotalJobs`.

### 12.3 Selection object

```json
{
  "buildTemplate": { "id": "…", "name": "KMC build" },
  "jobTemplate": { "id": "…", "name": "Temperature sweep" },
  "parameterization": { "id": null, "name": null, "implicitDefault": true },
  "outputName": "ceria-kmc"
}
```

`buildTemplate` is null when no build template is used. `jobTemplate` is non-null on a successful selection.
`outputName` is the effective stem used to derive a default target, or null when `-o` was explicit.

### 12.4 Other report payloads

- `new-project-v1`: `projectPath`, `projectId`, `template` (`blank`/`ceria`).
- `compile-v1`: `projectId`, `modelHash`, `documentHash`, `modelSignature`, `compilerVersion`,
  `outputPath` (null without `-o`), `outputSha256` (null without `-o`), `counts` (particle, position, site,
  environment, pair-table, cluster-table, direction, rule, and simulation counts), `cache`
  (hits/misses/capacity), and `stages[]` (`name`, `ms`).
- `inspect-v1`: `package` with the Step 11 `PackageInspection` fields: `formatVersion`, `appVersion`,
  `createdUtc`, `projectId`, `generator`, `jobSelection`, `symmetryContractVersion`,
  `spaceGroupDataVersion`, `spaceGroupDataSha256`, `models[]`, `latticeCount`, `jobCount`, `collections[]`,
  `minTemperatureK`, `maxTemperatureK`, `minElectricFieldVm`, `maxElectricFieldVm`, `sizeBytes`, and
  `latticeReuseFactor`.
- `verify-v1`: `valid`, `checks[]` (`code`, `passed`, `message`), `diagnostics[]`, and `package` (the same
  `inspect-v1` object).
- `doctor-v1`: `healthy`, `checks[]` (`name`, `status` enum `ok`/`warn`/`fail`, `message`, optional
  `detail`), and `environment` (`framework`, `os`, `architecture`, `sqliteVersion`, `catalogVersion`,
  `catalogSha256`, `solverPath`, `solverVersion`).
- `clean-v1`: `dryRun` (the requested mode), `packagePath`, `directory`, `jobSelection`, `jobCount`,
  `folders[]` (`path`, `jobId`, `lastWriteUtc`, `sizeBytes`), `totalBytes`, `olderThanDays`, and
  `removedCount`. With `--dry-run` (the only supported mode) `dryRun = true` and `removedCount = 0`; without
  it, the command is a usage error: `dryRun = false`, `removedCount = 0`, `CLI008`, exit 2, and no deletion
  ever occurs in Step 12.

### 12.5 JSON Lines events

`--json` makes **stderr JSON Lines only** — human stage/progress lines are suppressed — and emits exactly one
report object on **stdout**. Every line has `schemaVersion = 1` and a stable `event` name. Step 12 adds one
event and optional fields; the `events` schema version remains 1 because the change is additive.

| Event | Required fields | Optional fields | Meaning |
| --- | --- | --- | --- |
| `build.started` | `total` | — | Selected job count known; generation begins. |
| `build.stage` | `stage`, `status` | `ms` | A stage started (`status = "started"`) or completed. |
| `build.progress` | `completed`, `total` | `stage`, `current`, `etaS` | Throttled job progress. |
| `build.finished` | `exitCode`, `wallTimeS` | `published` | Final outcome; emitted exactly once. |
| `diagnostic` | `code`, `severity`, `message` | `path` | One structured diagnostic. |
| `log` | `level`, `message` | — | Forwarded human log. |

- `build.started` is emitted after expansion; `build.finished` is emitted even when the run fails before
  `build.started`. The `build.stage` events for load/select/validate/compile/validateBuild/parameterize/expand
  therefore precede `build.started`; consumers must not require `build.started` to be the first line.
  `build.started` means “job generation begins and the selected total is known”, not “the process started”.
- `build.progress.completed` is monotonic within a stream; `total` is the selected job count.
- The events manifest records **required fields only**. The optional fields (`ms`, `stage`, `current`,
  `etaS`, `published`, `path`) are documented here and in `02-data-contracts.md` §5.5 and asserted by the CLI
  event tests; Step 12 does not add an `optionalFields` manifest key, so the generated `events-v1` schema keeps
  its `x-event-required-fields` shape. Adding a required field is a versioned contract change; adding an
  optional field is additive.
- Consumers ignore unknown events and fields. Ordering is guaranteed within one stream.
- With `--json`, human stderr output is already suppressed; `--quiet` additionally suppresses `build.stage`,
  `build.progress`, and `log`; `diagnostic`, `build.started`, and `build.finished` remain.
- `tests/contracts/events-v1.jsonl` gains a real build sequence; the CLI schema test accepts it now, and the
  Step 26 GUI event parser will accept it when that parser exists.

## 13. Commands

### 13.1 `new`

- Creates `<dir>/<name>.mocprj`; `-o` defaults to the current directory. The directory is created if missing.
- `--template blank` creates an empty, valid project with a fresh UUIDv7, a fresh 32-byte lowercase-hex master
  seed, the supplied name, and `TimeProvider` timestamps.
- `--template ceria` starts from an embedded copy of the ceria model-definition sample (no job template), then
  replaces the identity, seed, name, and timestamps. The embedded template and `samples/ceria/ceria.mocprj`
  must have the same canonical **model** hash; a freshness test enforces it. The sample is a model starter,
  not a buildable job sample.
- Templates live in `Mocassin.Packages.Projects.Templates` as embedded `.mocprj` resources and are exposed by
  `ProjectTemplateFactory` so Step 23’s creation UI uses the same assets. The folder, its files, and the
  `PublicAPI.Unshipped.txt` entries are part of the §16.5 layout/public-API plan. The owner approved shipping
  the ceria starter with the model-hash freshness test on 2026-11-16; `blank` remains.
- `name` is a display name and a file stem: trim it, strip one `.mocprj` suffix, reject empty values, path
  separators, `..`, control characters, and platform-invalid characters (`: * ? " < > |`), and cap it at 128
  characters. A rejected name is `CLI001`/exit 2.
- The project is validated in memory before saving; a template defect is an internal error (`CLI007`, exit
  10), not a user error. `new` writes to a temp file and moves it without overwrite (`CLI003` on collision).
- The command never opens the editing lock or overwrites an existing project.

### 13.2 `compile`

- Loads parse-only, validates with `RunBuildStage = false`, and compiles. Validation errors exit 1 before
  compilation; `CMP` errors exit 4.
- `-o <path>` writes the canonical `compiled-inspection-v1` document atomically. It replaces an existing
  derived file; a directory target is exit 3.
- `--json` prints `compile-v1`; human mode prints a summary table (positions, sites, environments, pair and
  cluster tables, rules, directions, simulations, cache, stage timings) and the model signature.
- `compile` never applies a parameterization, expands jobs, or writes a package.

### 13.3 `build` and `build --preview`

- `build` runs §6 and writes/verifies the package. `--preview` runs stages 1–7 and the pipeline’s preview
  evaluation, then emits `build-v1` with `mode = "preview"`, `package = null`, `preview` projected from
  `BuildResult.Preview`, and `verification = "skipped"`. It never generates duplicate-job lattices, opens a
  writer, or creates a file.
- `--preview` still compiles, validates, parameterizes, expands, and evaluates the selected configurations,
  so the preview exercises the same selection and parameterization path as the build. The CLI only projects
  the typed `BuildPreview` payload; it does not reload, recompile, or re-evaluate.
- `--warnings-as-errors` is plumbed as `ValidationOptions.WarningsAsErrors` on both validation calls and
  promotes validation-stage warnings (including `JOB` structural warnings) to exit 1. `ParameterizationOptions`
  has no warning flag, so the pipeline inspects the `Validate`/`Apply` diagnostics and fails with
  `Failure = Parameterization` (exit 4) before the writer opens if any parameterization warning is present.
  Compile (`CMP`) and writer (`PKG`) warnings stay warnings; they are reported, not promoted.
- After `CompleteAsync`, the command runs `IPackageVerifier.VerifyAsync` with default options. Verification
  errors exit 1 and are reported as `PKG` diagnostics; the published file remains (it is already atomic).
- With `--json`, stderr is JSON Lines only; human progress is suppressed without needing `--quiet`.
- The command installs a first-signal cancellation handler for SIGINT (`Console.CancelKeyPress`) and, on
  Unix, SIGTERM (`PosixSignalRegistration`). The first signal requests cancellation; a second signal is left
  to the default handler. The token is passed to the pipeline and writer.
- The step page’s former `--dry-run` bullet is retired: `--preview` is the single no-write build mode.
  `clean --dry-run` keeps its separate meaning.

### 13.4 `inspect`

- Thin wrapper over `IMslPackageInspector.InspectAsync`. It never opens the package writable and never
  requires `-wal`/`-shm`.
- Human mode prints the package header, model signatures, job/lattice counts, collections, temperature and
  field ranges, size, reuse factor, and recorded `jobSelection`.
- `--json` prints `inspect-v1`. A missing file is exit 3; `InvalidDataException` is `CLI009`/exit 1 with a
  hint to run `verify`.

### 13.5 `verify`

- Thin wrapper over `IPackageVerifier.VerifyAsync` with `PackageVerificationOptions` defaults.
- Prints the ordered checks and `PKG` diagnostics; `--json` prints `verify-v1`. Exit 0 when valid, 1 when
  invalid, 3 when the file is missing/unreadable.
- It never mutates the package and never writes a results store.

### 13.6 `doctor`

Checks, in order:

1. .NET runtime version, OS description, and process architecture.
2. SQLite availability and version by opening an in-memory connection and querying `sqlite_version()`.
3. Write permission in the current directory by creating and deleting a temp file.
4. Free disk space for the current directory.
5. Embedded symmetry catalog: it loads, reports `DataVersion` and `DataSha256`, and has the expected setting
   count. A load failure or malformed checksum is `fail`.
6. Embedded blank and ceria project templates: they load and validate with no errors. A defect is `fail`.
7. Configured solver: if `MOCASSIN_SIM` is set, the path exists and `--version` succeeds; otherwise look for
   `mocassin-sim` on `PATH`. A missing solver is `warn` (the solver arrives in Step 13/19), not `fail`; a
   configured-but-broken solver is `fail`.

- `healthy` is true when no check is `fail`; warnings do not fail the command. Exit 0/1.
- Every check implements an internal `IDoctorCheck` seam (`Name`, `RunAsync` returning
  `DoctorCheckResult(name, status, message, detail)`). `DoctorCommand` takes an optional
  `IEnumerable<IDoctorCheck>`; production uses the seven real checks, and tests inject passing/warning/failing
  checks to exercise the healthy, warning-only, and failure reports without spoofing the OS. The runtime,
  SQLite, permission, disk, catalog, and template checks are real in the healthy CI path; failure injection
  replaces the check, not the operating system.
- The solver check is the only process boundary; it uses an `ISolverProbe` seam so tests do not launch a
  process, and the real probe has a 5 s timeout.
- `doctor` must exit 0 on CI. The solver check is deliberately a warning until Step 19.

### 13.7 `clean --dry-run`

- Step 12 implements **only** the dry run. Without `--dry-run` the command exits 2 with `CLI008` and a
  pointer to Step 28; no file or row is ever deleted in this step. With `--json` it still emits one
  `clean-v1` report with `dryRun = false`, `removedCount = 0`, and the `CLI008` diagnostic.
- `--dir` defaults to the package’s directory. The command lists `Job%05d` directories in that directory,
  optionally filtered by `--older-than <days>` using each directory’s last-write time, and reports their
  paths, IDs, timestamps, and sizes.
- The package is opened read-only through the inspector to report its `jobSelection` and `jobCount`. The
  listing is directory-scoped; Step 28 adds exact per-package ID matching and actual deletion.
- `--results <file.msr>` is reserved for Step 20/28 and is `CLI008`/exit 2.
- The `.msl`, `.msr`, and job folders are never modified.

### 13.8 Shell completion (no command in Step 12)

- Step 12 does **not** ship `mocassin completion` and does not generate bash, zsh, fish, or PowerShell scripts.
  `System.CommandLine` 3.0.0 provides the `[suggest]` protocol that `dotnet-suggest` consumes, not script
  generation; `dotnet-suggest` is an external global tool and is not installed in the development image.
- The `[suggest]` directive is accepted by the command graph (the library handles it before a handler), but it
  is not a versioned contract and Step 12 makes no command-scoped suggestion guarantee: the verification
  observed root-level tokens only at every tested position. A test only asserts that the directive is accepted
  without an unhandled exception; it does not assert a suggestion set.
- Shell integration, if any, is decided in Step 31 with the packaging/installation story (document
  `dotnet-suggest`, ship registration snippets, or add generated scripts). This is a deferred packaging
  decision, not a Step 12 acceptance criterion and not part of the command contract.
- An unknown command remains an ordinary parse error: `CLI001`, exit 2, human usage, no report.

### 13.9 Reserved commands

`simulate`, `collect`, `evaluate`, `export`, and full `clean` are defined in the command graph and exit 2 with
`CLI008` and the owning step. They are not silently treated as unknown commands, so scripts fail with a
useful message. They do not accept `--json` and print no report; the one-report guarantee covers `validate`
and the seven `reports`-backed commands only.

## 14. Cancellation and partial-build behaviour

| Point of cancellation | Behaviour |
| --- | --- |
| During load/select/validation/compile/parameterize/expansion/preview | The token reaches `Validate`, the compiler, the parameterizer, the expander, and the preview builder; no writer exists. Exit 6; `CLI006`. |
| Before the writer opens | No temp file; exit 6; diagnostics `CLI006`. |
| During lattice generation or job writes | Writer disposal deletes the temp file; an existing package is untouched; exit 6. |
| During `CompleteAsync` before the rename | The writer’s own cancellation path deletes the temp file; an existing package is untouched; exit 6. |
| After the rename, before/during verification | The new package remains; report `package.published = true`, `verification = "skipped"`, exit 6. |
| Second signal | The handler is released and the process terminates with the platform default. |

- A cancelled build never returns a partial `BuildResult` with `Status = Succeeded`.
- The library returns `Status = Cancelled` and `Failure = None`, with whatever identity/selection/package
  fields it reached; it does not add a CLI diagnostic. The CLI adds the `CLI006` info to the report
  diagnostics, sets `ok = false`, and exits 6.
- A partial **selection** (`--jobs`) is not a cancellation or a partial success; it is a successful build of
  a recorded subset.

## 15. Dependency direction and layering

```text
Mocassin.Cli ──► Mocassin.Packages ──► Mocassin.Contracts ──► Mocassin.Compilation ──► Mocassin.Symmetry
     │                  │                                                            │
     │                  └──► Mocassin.Core ◄───────────────────────────────────────────┘
     ├──► Mocassin.Results ──► Mocassin.Contracts / Mocassin.Core
     └──► Mocassin.Compilation (read-only use for `compile` and the inspection writer)
```

- `Mocassin.Packages.Build` uses `ProjectStore`, `ProjectValidationPipeline`, `ModelCompiler`,
  `ParameterizationService`, `JobTemplateExpander`, `SupercellGenerator`, and `SimulationPackageWriter`.
- `Mocassin.Packages` does **not** reference `Mocassin.Results`. Post-build verification is composed by the
  CLI (`Mocassin.Cli` already references `Results`) and by the GUI later.
- `Mocassin.Contracts.Cli` owns the `CLI` descriptors; `Mocassin.Packages` composes them into
  `DiagnosticCatalog.Default`; `Mocassin.Cli` constructs the diagnostics. There is no `Packages -> Cli` or
  `Results -> Packages` edge.
- `Mocassin.Cli -> Mocassin.Compilation` becomes an explicit project reference for `compile`; it is the same
  dependency the current preview already uses transitively. The CLI still owns no scientific algorithm.
- `Mocassin.Packages.Build` uses `ILatticePreviewService`, `JobTemplateValidationStage`, and the shared
  `BuildPreviewBuilder` from `Mocassin.Packages.Jobs`/`Projects.Validation`; it needs no new project reference.
  One preview-service instance is shared by the build-stage validation and the preview evaluation.
- `System.CommandLine` 3.0.0 is the only new production package and is referenced by `Mocassin.Cli` only. It
  adds no project edge and no transitive package for `net10.0+`; it is MIT-licensed, pinned centrally, and
  AOT/trim-verified for `linux-x64` (§5.2). No library project may reference it.
- `System.Threading.Channels` and `System.Text.Json` are in-box; `JsonSchema.Net` is added only to the CLI
  test project (already centrally versioned) for schema validation.
- `Microsoft.Data.Sqlite` reaches the CLI transitively through `Packages` for the `doctor` SQLite check; it is
  already a production dependency of the writer.

## 16. Layout, namespaces, and structure policy

### 16.1 `Mocassin.Packages/Build`

```text
Mocassin.Packages/Build/
  IBuildPipeline.cs
  BuildPipeline.cs
  BuildRequest.cs
  BuildOptions.cs
  BuildResult.cs
  BuildEvent.cs                 # event records + BuildStage/BuildStatus/BuildFailureKind
  BuildSelection.cs
  BuildSelectionResolver.cs
  BuildPathResolver.cs
  BuildPreview.cs               # BuildPreview + collection/config/optimizer/doping records
  JobSelection.cs               # parsed selection + normal form
  ProgressEstimator.cs
  Internal/
    BuildScheduler.cs           # parallel generation + ordered serialization
    BuildDiagnosticMerger.cs
    BuildPreviewBuilder.cs      # shared preview mapping over ILatticePreviewService
```

Namespace `Mocassin.Packages.Build` and `Mocassin.Packages.Build.Internal`. Depth 2. Each folder stays well
under 20 files and 800 lines; `BuildPipeline` is split into stage helpers if it approaches the limit.
`BuildPreview.cs` follows the existing multi-record `PackageInspection.cs` precedent; `BuildPreviewBuilder`
is the only preview-mapping implementation, used by the pipeline and therefore by both the CLI and Step 26.

### 16.2 `Mocassin.Cli`

```text
Mocassin.Cli/
  Program.cs                    # entry point + signal handler
  AssemblyInfo.cs
  CommandLine/
    CliApplication.cs           # RootCommand invocation, dispatch, exit-code policy
    CliCommandGraph.cs          # command/option/argument definitions (the only command table)
    CliBinding.cs               # ParseResult -> typed request records
    CliParseErrors.cs           # parse errors -> CLI001 diagnostics + human usage
    CliExitCode.cs
    Reports/
      ReportDiagnostic.cs
      ReportEnvelope.cs
      JsonReportWriter.cs
      HumanReportWriter.cs
  Diagnostics/
    CliDiagnosticFactory.cs
  NewProject/
    NewProjectCommand.cs
    NewProjectReport.cs
  Compile/
    CompileCommand.cs
    CompileReport.cs
  Build/
    BuildCommand.cs
    PreviewCommand.cs           # removed when feature 3 folds preview into BuildCommand
    BuildReport.cs
    BuildPreviewMapper.cs       # maps BuildResult.Preview to build-v1.preview
    BuildReportWriter.cs
    BuildEventWriter.cs
    BuildProgressRenderer.cs
  Validation/
    ValidateCommand.cs
    ValidationReport.cs
    ValidationReportWriter.cs
  Inspect/
    InspectCommand.cs
    InspectReport.cs
  Verify/
    VerifyCommand.cs
    VerifyReport.cs
  Doctor/
    DoctorCommand.cs
    DoctorReport.cs
    IDoctorCheck.cs             # Name + RunAsync -> DoctorCheckResult
    DoctorCheckResult.cs
    Checks/
      RuntimeCheck.cs
      SqliteCheck.cs
      WritePermissionCheck.cs
      DiskSpaceCheck.cs
      SymmetryCatalogCheck.cs
      ProjectTemplateCheck.cs
      SolverProbeCheck.cs
    ISolverProbe.cs
    ProcessSolverProbe.cs
  Clean/
    CleanCommand.cs
    CleanReport.cs
```

- Folders are command **domains**, not a `Commands/` bucket. `CommandLine` owns the `System.CommandLine`
  graph, `ParseResult` binding, parse-error mapping, exit-code policy, the library help/version actions, and
  shared output; `Diagnostics` owns CLI-specific diagnostic construction. There is no hand-written parser,
  `CommandCatalog`, `CommandDefinition`, `OptionDefinition`, `ParsedCommand`, `HelpWriter`, or
  `CompletionWriter`.
- Namespaces match folders: `Mocassin.Cli.CommandLine`, `Mocassin.Cli.CommandLine.Reports`,
  `Mocassin.Cli.Build`, `Mocassin.Cli.Doctor.Checks`, and so on.
- Each folder stays under 20 direct `.cs` files; `Doctor/Checks` resets the count. The deepest path is
  `Doctor/Checks` (depth 2). Files stay under 800 lines; the build command is split into a command, a
  report writer, an event writer, and a renderer.
- `CliApplication.RunAsync(args, stdout, stderr, cancellationToken)` remains the test entry point. It builds
  or receives the `RootCommand`, maps `ParseResult.Errors` to `CLI001`/2, and dispatches the resolved handler;
  handler results are mapped in one place. `CliBinding` converts a successful `ParseResult` into the typed
  request record each handler takes.
- `PreviewCommand` and the `build-preview-v1` DTOs are removed or folded into `BuildCommand`/`BuildReport`
  during feature 3; no second report shape survives. `BuildPreviewMapper` projects the library payload; it
  does not evaluate anything.
- `DoctorCommand` takes an optional `IEnumerable<IDoctorCheck>`; the seam is internal to the CLI and the test
  assembly uses `InternalsVisibleTo`. The `Doctor/Checks` folder reset keeps the file count under 20.

### 16.3 `Mocassin.Contracts/Cli`

```text
Mocassin.Contracts/Cli/
  CliDiagnosticCodes.cs
  CliDiagnosticDescriptors.cs
```

Namespace `Mocassin.Contracts.Cli`. These are the only new Contracts files; they follow the `PKG`
precedent. `PublicAPI.Unshipped.txt` is updated; the Contracts ApiCompat baseline is refreshed only if an
intentional rebaseline is recorded (additive API does not require one).

### 16.4 Tests

```text
Mocassin.Packages.Tests/Build/            # pipeline, selection, job selection, scheduler, cancellation, preview
Mocassin.Packages.Tests/Projects/Templates/ # embedded template freshness and identity
Mocassin.Cli.Tests/CommandLine/           # library parsing conventions, parse-error mapping, help/version, usage
Mocassin.Cli.Tests/Build/                 # build/preview, reports, events, exit codes
Mocassin.Cli.Tests/Compile/
Mocassin.Cli.Tests/Inspect/
Mocassin.Cli.Tests/Verify/
Mocassin.Cli.Tests/Doctor/
Mocassin.Cli.Tests/Clean/
Mocassin.Cli.Tests/NewProject/
Mocassin.Cli.Tests/Validation/            # existing regression tests
tests/aot/                                # minimal System.CommandLine AOT smoke host + positive control
```

The test root keeps `RepositoryPaths.cs` and the existing files; the 20-file root limit and folder
namespace rules apply.

### 16.5 `Mocassin.Packages/Projects/Templates`

```text
Mocassin.Packages/Projects/Templates/
  ProjectTemplate.cs
  ProjectTemplateFactory.cs
  Assets/
    blank.mocprj
    ceria.mocprj
```

Namespace `Mocassin.Packages.Projects.Templates`. `ProjectTemplate` and `ProjectTemplateFactory` are public
and XML-documented in `PublicAPI.Unshipped.txt`; the two `.mocprj` files are embedded resources and do not
count toward the 20-`.cs`-file or 800-line limits. The folder is depth 2, and `Assets/` holds only the
embedded templates. Step 23's creation UI uses the same factory; a freshness test compares the embedded
ceria asset's canonical model hash with `samples/ceria/ceria.mocprj`.

## 17. Contract manifest, schemas, and fixtures

- Add the seven `reports` descriptors to `contracts/manifest.json` from §12.1, with `schemaVersion = 1`,
  `unknownFields = "ignore"`, and explicit field/type/required/enum descriptors. `tools/contracts/generate.py`
  gains the matching `REPORT_CONSTANTS` entries.
- The manifest’s own `manifestVersion` stays `1`. `generate.py` pins it, and the new report/event entries are
  additive; each report carries its own `schemaVersion = 1`. A `manifestVersion` bump would be a generator
  change and is not part of this step.
- Regenerate `contracts/generated/schema/*.schema.json`, `contracts/generated/constants/**`, and
  `contracts/generated/fixtures/descriptors.json`. `./tools/verify.sh contracts` checks freshness.
- Add `build.stage` with required `stage`/`status` to `formats.events.events`. The events schema version stays
  1 because the change is additive. The manifest records required fields only; the optional `ms`, `stage`,
  `current`, `etaS`, `published`, and `path` fields are documented in §12.5 and `02-data-contracts.md` §5.5
  and exercised by the CLI event tests. Step 12 does not add an `optionalFields` manifest key or change the
  generator’s event-schema shape.
- Add `tests/contracts/events-v1.jsonl` with a real build sequence (started, stage, progress, diagnostic,
  finished). The CLI test validates each line against the generated `events-v1` schema metadata and the
  event required-field table.
- `build-preview-v1` is retired. It was never a manifest-owned report; the Step 10 tests that parse it are
  updated to `build-v1` with `mode = "preview"`.
- `compiled-inspection-v1` and `package-summary-v1` stay canonical projection formats, not new CLI report
  ids. `compile -o` writes the former; `inspect-v1`/`verify-v1` embed the latter’s data. Registering them as
  standalone manifest reports is a Step 30 documentation/API decision.
- No package schema, payload version, signature, or `schema_info` row changes. `job_selection` already exists
  and is now populated with the normalized expression.

## 18. Documentation and test plan

### 18.1 Documentation

- `docs/reference/cli.md` — full command surface, options, selection rules, exit codes, `CLI` codes, report
  ids and field families, JSON-Lines events, parser conventions, reserved commands, and the completion
  non-promise.
- `docs/reference/diagnostics.md` — `CLI001`–`CLI009` rows and headings, included in the docs-drift test.
  These rows land in feature 1 **together with** the Contracts descriptors, because the existing drift test
  requires the page and `DiagnosticCatalog.Default` to agree at every commit; the design-only commit does not
  edit that page.
- `docs/user-guide/building.md` — the end-to-end CLI workflow, recipe selection, partial jobs, progress,
  cancellation, `--force`, `--compact`, verification, and failure behaviour.
- `docs/getting-started/index.md` — a short “create, validate, build, verify” section with the ceria job
  sample. Dedicated `quickstart.md`/`first-project.md` pages are Step 30 work; the step page records that
  reconciliation.
- `docs/troubleshooting/errors.md` — build/CLI exit codes, selection and job-selection failures, stale
  parameterization, package verification failures, cancellation, and `doctor` recovery.
- `docs/development/code-structure.md` — the CLI folder plan and the command-domain rule.
- `docs/development/design-decisions.md` — the decisions in §20.
- `docs/planning/01-target-architecture.md` — CLI responsibilities, the `System.CommandLine` technology
  choice, dependency edge, and the corrected exit codes.
- `docs/planning/02-data-contracts.md` — §5.1 command surface, §5.5 events, new §5.7 reports, §3.2
  `job_selection` normal form, and §6 fixtures.
- `docs/planning/03-quality-strategy.md` — the CLI/build test tier and §9.10.
- `docs/planning/04-roadmap.md` — the Step 12 owner-approved 3.0–4.0 pw exception in §3 and §3.1.
- `docs/planning/05-open-decisions.md` — the resolved sizing, `--template`, ceria-asset, and report-registration
  decisions.
- `docs/planning/appendix-b-parity-matrix.md` — the rows in §18.3.
- `docs/reference/diagnostics.md` — the `CLI001`–`CLI009` rows and headings, added with the Contracts
  descriptors; `docs/reference/cli.md` links to that drift-checked table instead of duplicating it.
- The step page, status dashboard, and closeout evidence as required by the normal process.

### 18.2 Test tiers

| Tier | Coverage |
| --- | --- |
| PR-fast | Library parsing conventions, parse-error→`CLI001`/2 mapping, help/version, usage and exit-code mapping; `JobSelection` parsing/normalization; selection precedence; ceria sample build + verify; preview; partial selection; `--force`; cancellation (in-process token); parallel determinism; `--compact` option mapping; `new`/`compile`/`inspect`/`verify`/`doctor`/`clean --dry-run`; JSON schema validation; `validate` regression |
| PR-medium | Real `.msl` round-trip with the Step 13 reader once it exists; cross-platform path handling; larger sample matrix |
| Nightly/release | 10⁴-job build + verify under 10 min / 2 GB; real solver probe; AOT smoke host with the positive control, plus the recorded real-CLI publish result (§5.2); Windows/macOS platform gate when cancellation/process/path behaviour is touched |

- Test doubles are limited to the true boundaries: `TimeProvider` (through `BuildRequest.Options`), the
  solver process probe, the `IDoctorCheck` list for doctor failure injection, console cancellation, and
  failure injection for write/disk errors. Build orchestration, selection, expansion, lattice generation,
  preview, writing, inspection, and verification use real objects and real SQLite files.
- A CLI test calls `CliApplication.RunAsync(args, stdout, stderr, cancellationToken)` with `StringWriter`s;
  it asserts exit code, stdout/stderr, files, and JSON, not private call counts. It also asserts that `--json`
  leaves stderr JSONL-only and that help/version/reserved commands never emit a report.
- The preview test asserts `BuildResult.Preview` is populated from the same `BuildPreviewBuilder`/service
  instance as the build-stage validation, that the CLI mapping adds no second evaluation, and that
  `build-v1.preview` equals the typed payload.
- The 10⁴-job evidence reuses the Step 11 workload generator pattern (a synthetic project from the ceria job
  sample) and records time, peak RSS, package size, and verification result in the step page and closeout
  evidence.

### 18.3 Parity rows

These are the **intended** transitions, applied to `appendix-b-parity-matrix.md` in feature 7 after the
acceptance criteria pass. The design-freeze commit updates the Step column and notes to include 12 but does
not mark an unimplemented feature `Done`; only a passing acceptance test may do that.

- `Headless mocassin validate` stays `Partial` (07/12) until feature 3; the full command surface then makes
  it `Done`.
- `Headless mocassin compile` stays `Planned` (12) until feature 4; it becomes `Done`.
- `Build template selection` stays `Partial` (12/26) after feature 3: CLI done, UI in 26.
- `Build preview (no package write)` stays `Done` (10/12): the preview exists from Step 10; feature 3 replaces
  `build-preview-v1` with `build-v1` preview mode.
- `Create/overwrite package` stays `Partial` (11/12/26): CLI done after feature 3, UI in 26.
- `Package inspection/verification` stays `Improvement` (11/12): the Step 11 inspector/verifier exists; the
  CLI wrapper lands in feature 5.
- `Build progress/cancel/log` stays `Planned` (12/26) until feature 3; then `Partial` (CLI done, UI in 26).
- `mocassin parameterize CLI` stays `Replaced` (12), with `compile`/`build --parameterization`.
- `Create new project` stays `Planned` (01/12/23) until feature 4; then `Partial` (CLI `new`, creation UI in 23).
- New rows: `CLI doctor` (`Planned`, 12; becomes `Improvement` after feature 6) and `Clean job folders (dry
  run)` (`Planned`, 12/28; becomes `Partial` after feature 6).
- `CLI + format reference` stays `Partial` (07/12/30): the reference grows in feature 7, formats complete in
  30.

## 19. Implementation tasks

### Feature 1 — CLI shell, parser configuration, and report contracts

- [ ] Add `System.CommandLine` 3.0.0 to central package management and reference it from `Mocassin.Cli`
      only; record the MIT/`requireLicenseAcceptance`/empty-transitive-graph review and the AOT evidence.
- [ ] Define the `RootCommand` graph for `new`, `validate`, `compile`, `build` (including the interim
      `--preview`), `inspect`, `verify`, `doctor`, `clean`, and the reserved commands; define options,
      arguments, arity, defaults, and validators.
- [ ] Replace the Step 07 `validate` and Step 10 `build --preview` hand-written parsers with the shared graph;
      keep `CliApplication.RunAsync(args, stdout, stderr, cancellationToken)` as the test entry point and keep
      the `validation-v1` output and the interim preview behaviour unchanged.
- [ ] Bind a successful `ParseResult` to typed request records and dispatch to handlers; map `ParseResult.Errors`
      to `CLI001` and exit **2** (the library's own parse-error code is 1 and is never returned); map handler
      results through the exit-code table; keep help and version at exit 0.
- [ ] Add `CliExitCode`, the `CLI001`–`CLI009` descriptors in `Mocassin.Contracts.Cli`, and compose them into
      `DiagnosticCatalog.Default`.
- [ ] Add the shared report diagnostic/envelope DTOs and writers; keep `validation-v1` byte-compatible.
- [ ] Add the seven `reports` manifest descriptors and regenerate the contract artifacts.
- [ ] Add `build.stage` to the events manifest and add `tests/contracts/events-v1.jsonl`.
- [ ] Add parser-convention/usage/help/schema tests (arity, `--opt=value`, negative numbers, `--`,
      response-file behaviour, error text, exit codes) and the `validate` regression tests.
- [ ] Add the minimal AOT smoke host and the positive control; attempt and record the real `Mocassin.Cli`
      AOT publish; do not add a `completion` command.

### Feature 2 — Build orchestration core

- [ ] Add `Mocassin.Packages.Build` request/options/result/failure/stage/event/preview types and
      `IBuildPipeline`; `BuildRequest.Options` is the only options injection point.
- [ ] Implement the ordered pipeline, the bounded event channel, progress throttling, and stage timings.
- [ ] Construct the validation pipeline with `JobTemplateValidationStage(previewService)`; use named
      `Validate` arguments and pass the token.
- [ ] Implement the shared `BuildPreviewBuilder` and set `BuildResult.Preview`; never reload or recompile for
      preview.
- [ ] Plumb `WarningsAsErrors` into `ValidationOptions` and the parameterization diagnostic check.
- [ ] Implement build/job-template/parameterization selection and output-path resolution.
- [ ] Implement `JobSelection` parsing, normalization, and the `JobSelectionResult` projection.
- [ ] Implement the parallel generation + serialized write scheduler and cancellation.
- [ ] Add real-project pipeline tests for ceria, preview payload, partial selection, parallel determinism,
      cancellation, every failure kind, and the options injection.

### Feature 3 — Build and preview command

- [ ] Implement `build` and fold `--preview` into it; map `BuildResult.Preview` and retire
      `build-preview-v1`.
- [ ] Implement `--jobs`, `--parallel`, `--force`, `--compact`, `--warnings-as-errors`, `--quiet`, `--json`.
- [ ] Implement human progress/ETA and JSON-Lines events; make `--json` suppress human stderr; install the
      signal handler.
- [ ] Implement post-publication verification and the `build-v1` report, including the `preview` timing from
      `BuildPreview.Elapsed`.
- [ ] Implement the report-backed scope: one report for `validate` and the seven report ids; no report for
      help/version/reserved commands; `clean --json` without `--dry-run` reports `CLI008`.
- [ ] Add the sample build/verify, partial, force, cancel, compact, warnings, exit-code, and schema tests.

### Feature 4 — Project and compile commands

- [ ] Add `Mocassin.Packages/Projects/Templates/` with the embedded blank/ceria assets,
      `ProjectTemplateFactory`, fresh-identity cloning, and the `PublicAPI.Unshipped.txt` entries.
- [ ] Implement `new` with name validation, safe create, and the `new-project-v1` report.
- [ ] Implement `compile` with `-o` `compiled-inspection-v1`, the `compile-v1` report, and exit mapping.
- [ ] Add template freshness, `new` validation, and compile success/failure tests.

### Feature 5 — Package inspection commands

- [ ] Implement `inspect` and `verify` over the Step 11 inspector/verifier.
- [ ] Add `inspect-v1`/`verify-v1`, human output, and exit/`CLI009` mapping.
- [ ] Add valid/corrupt/truncated/missing package and schema tests.

### Feature 6 — Doctor and dry-run clean

- [ ] Implement `doctor` checks through `IDoctorCheck`, the solver probe seam, the `doctor-v1` report, and
      healthy-CI behaviour.
- [ ] Implement `clean --dry-run` with `--dir`/`--older-than`, no deletion, and the `clean-v1` report,
      including the `--json`-without-`--dry-run` `CLI008` report.
- [ ] Add healthy, injected-check-failure, warning-only, and no-deletion tests.

### Feature 7 — Documentation, parity, and closeout

- [ ] Write the pages in §18.1 and update the parity rows in §18.3.
- [ ] Record the 10⁴-job evidence and the final local gates.
- [ ] Fill the closeout evidence, set the audit request, and run `./tools/verify.sh all --require`.
- [ ] Update the step page feature log and status.

## 20. Reconciliations and recorded decisions

- **`--template` means build template; `--job-template` means job template.** Step 12’s command surface and
  §7 define this. The Step 10 preview used `--template` for a job template; that interim meaning is replaced.
  A job-template ID passed to `--template` gets `CLI004` and a hint. The owner accepted this surface on
  2026-11-16 (§21).
- **`--compact` = Zstd quality 9 + `VACUUM`.** Step 11’s page says `--compact` runs `VACUUM`; its amendment
  and the user guide use level 9. Both are honoured as one size preset. It never changes signatures or the
  canonical summary.
- **`--dry-run` on `build` is retired; `--preview` is the no-write mode.** The old step-page bullet is
  replaced. `clean --dry-run` remains and means “list only”.
- **`BuildResult` carries the report identity.** `BuildIdentity` supplies `projectId`, `modelHash`,
  `documentHash`, `modelSignature`, and `parameterizationSignature`; the CLI never recomputes them. Each value
  is null until its stage has run, so an early failure still produces a complete report.
- **`BuildFailureKind.Io` is separate from `Load`.** `Io` covers project/output/disk and writer I/O and maps
  to exit 3; `Load` covers parse/content/validation failures and maps to exit 1. The CLI distinguishes the
  `PROJ010` unknown-template reference (exit 1) from the `CLI004` selection error (exit 2) by diagnostic code.
- **Cancellation is a status, not a CLI diagnostic.** The pipeline returns `Status = Cancelled`; the CLI adds
  `CLI006` to the report and exits 6. The GUI consumes the status without a CLI-specific diagnostic.
- **`build-v1` has no package-size estimate.** `estimatedPackageBytes` is dropped: Step 10 did not emit it, a
  preview is size-neutral, and `package.sizeBytes` is the authoritative measured size after a build. A future
  estimate needs its own formula and contract decision.
- **`BuildResult.Preview` is the single preview output.** The pipeline evaluates the preview after expansion
  and before lattice generation, using the injected `ILatticePreviewService` and the shared
  `BuildPreviewBuilder`; the typed `BuildPreview` carries the collections/configs/optimizers/dopings and its
  `Elapsed` time. The CLI and Step 26 GUI map it; neither reloads, recompiles, nor runs a second preview
  implementation. A separate `IBuildPipeline.PreviewAsync` was rejected: it would create two orchestration
  entry points and duplicate the load/compile/selection path for no additional capability.
- **`preview` is pipeline-composed; `verify` is caller-composed.** Both appear in `build-v1.timings` and the
  rendered steps, but neither is a `BuildStage` value or a stage event. `preview` comes from
  `BuildPreview.Elapsed`; `verify` is appended by the CLI after publication.
- **`BuildRequest.Options` is the only options injection point.** `ProgressInterval`, `TimeProvider`, and the
  test-tunable channel capacity are read from the request, not an ambient/static configuration or a pipeline
  constructor. `BuildOptions.TimeProvider` is non-null with a `TimeProvider.System` default.
- **Every `BuildResult` maps to an exit code.** The table in §11.1 covers every failure kind, including
  `JobSelection`, `Preview`, a `PackageWrite` without an I/O cause, and the `Failure = None`/`Failed`
  contract violation. The pipeline maps load `NotFound`/`TooLarge`/`Io` to `Io` and `InvalidContent` to `Load`.
- **`JobTemplateValidationStage` is mandatory.** The pipeline constructs the validation pipeline with
  `additionalStages: [new JobTemplateValidationStage(previewService)]`; `RunBuildStage = true` on a default
  pipeline is a silent no-op. The stage shares the preview service with the preview builder.
- **`--warnings-as-errors` plumbing.** Both validation calls set `ValidationOptions.WarningsAsErrors`; because
  `ParameterizationOptions` has no warning flag, the pipeline inspects the returned parameterization
  diagnostics and fails with `Failure = Parameterization` before the writer opens. `CMP`/`PKG` warnings stay
  warnings.
- **Named `Validate` arguments and the token.** Both calls use `loadDiagnostics:`, `options:`,
  `compiledModel:`, and `cancellationToken:` and pass the build token, following the accepted Step 10 audit
  note R2-5.
- **Report scope is explicit.** `validate` and the seven report ids emit one report per outcome; help, version,
  and reserved commands do not accept `--json` and emit none. `clean --json` without `--dry-run` emits a
  `clean-v1` report with `dryRun = false`, `removedCount = 0`, and `CLI008`.
- **`--json` makes stderr JSON Lines only.** Human progress is suppressed whenever `--json` is set; `--quiet`
  additionally suppresses stage/progress/log events.
- **`doctor` uses an `IDoctorCheck` seam.** Production runs the seven real checks; tests inject the check list
  to exercise pass/warn/fail without spoofing the OS. The solver process boundary keeps `ISolverProbe`.
- **Exit code 5 is reserved.** `build` is all-or-nothing and never publishes a partial package. Future
  multi-job commands share the code.
- **Validate, compile, then validate the build stage.** The pre-compile `Validate(... RunBuildStage = false)`
  call fails model errors with exit 1 before a compile; the post-compile call runs the same pipeline with
  `RunBuildStage = true` and the compiled model for the build stage. The repeated stages 1–4 are cheap and the
  report deduplicates exact diagnostics; no second validation entry point is added.
- **`build --preview` uses the build exit-code table**, replacing the Step 10 interim mapping. The preview
  report is `build-v1` with `mode = "preview"`.
- **`build-preview-v1` is retired** and becomes `build-v1` preview mode. It was never manifest-registered.
- **`clean` ownership is split:** Step 12 owns the dry-run listing; Step 28 owns deletion. Step 11’s note
  that Step 12 owns `clean` is reconciled to this split.
- **`validate` is unchanged:** same options, `validation-v1` report, and 0/1/2/3 exit codes.
- **`01-target-architecture.md` §7.3** is corrected: the .NET CLI uses 0/1/2/3/4/5/6/10; the Rust solver keeps
  its 10–16 range. The old “4 = solver failure” line described the wrong process.
- **`new` has no `--force`:** it refuses to replace a project. `compile -o` replaces a derived inspection
  atomically without `--force`; only the immutable package requires `--force`.
- **`quickstart.md`/`first-project.md` are Step 30 pages.** Step 12 puts the first-run commands in
  `docs/getting-started/index.md`; the step page’s old documentation list is reconciled to the pages that
  exist.
- **Sizing is an owner-approved exception, revised after the parser change.** The single-step exception, seven
  features, no split, and the 2.25 pw contingency were accepted on 2026-11-16 at 3.0–4.0 pw. The
  `System.CommandLine` decision on 2026-11-17 makes feature 1 smaller and revises the estimate to
  2.75–3.75 pw; the revision is presented for owner confirmation at the round-3 start gate.
  `04-roadmap.md` §3/§3.1 and `05-open-decisions.md` record the exception and the revised figure (Step 09
  precedent).
- **The ceria starter and canonical projections are accepted.** `new --template ceria` ships the embedded
  model starter with the model-hash freshness test; `blank` remains. `compiled-inspection-v1` and
  `package-summary-v1` stay canonical projections with no new CLI report ids until Step 30.
- **Parser: `System.CommandLine` 3.0.0, CLI-only.** The human owner replaced the hand-written parser on
  2026-11-17. The package is stable, MIT-licensed, centrally pinned, and has no transitive dependency for
  `net10.0+`; it is referenced only by `Mocassin.Cli`. The library owns tokenization, arity, help/version,
  and `[suggest]`; the CLI owns the command graph, typed binding, `CLI001` wording, and exit-code policy.
  Step page §5.2, §15, §16.2.
- **Parse errors are exit 2, never the library's 1.** `CliApplication` reads `ParseResult.Errors`, emits
  `CLI001`, and returns 2; help/version return 0; handler results map through §11.1. A test pins the
  library-default-1-to-2 translation. Step page §5.2/§11.1/§23.
- **No `completion` command in Step 12.** `System.CommandLine` provides the `[suggest]` protocol, not scripts;
  `dotnet-suggest` is not installed and the directive's command-scoped behaviour was not demonstrated. The
  command is removed from the surface, the directive is accepted but not a contract, and shell integration is
  Step 31 packaging. Step page §5.2/§13.8.
- **AOT evidence is a dependency smoke test, not a platform promise.** Only `linux-x64` AOT was exercised. A
  minimal smoke host with the same package version and a positive control guards the `System.CommandLine`
  claim; the real CLI publish is recorded as evidence, not a Step 12 gate, because SQLite/MessagePack are
  unverified. Windows/macOS AOT, the `netstandard2.0` asset, and the final AOT/single-file packaging choice
  remain Step 31 gates. Step page §5.2/§18.2/§24.
- **Additive API wording.** New public members update `PublicAPI.Unshipped.txt`; the ApiCompat baseline is
  refreshed only for an intentional rebaseline, not merely because an additive member was added.

## 21. Owner decisions (recorded 2026-11-16)

These decisions were made by the human owner after round 1 and are not open for re-litigation in round 2.

1. **`--template` semantics — accepted.** `--template <buildTemplateId>` selects a build template;
   `--job-template <id>` selects a job template; `--parameterization <id>` overrides the recipe. The Step 10
   interim meaning of `--template` as a job template is replaced.
2. **Sizing — accepted.** 3.0–4.0 pw, seven features, no split, with the 2.25 pw contingency in §2. The
   roadmap row and §3.1 record the owner-approved single-step exception (Step 09 precedent).
3. **`new --template ceria` — accepted.** Ship the embedded ceria starter with the model-hash freshness test;
   keep `blank`. The asset is MIT-authored project data, not copied GPL content.
4. **Report registration — accepted.** `compiled-inspection-v1` and `package-summary-v1` remain canonical
   projections; no new CLI report ids until Step 30.
5. **Parser — replaced (2026-11-17).** Adopt `System.CommandLine` 3.0.0 (MIT, pinned, CLI-only) and remove
   the hand-written parser; the AOT/trimming reason for the earlier decision was disproved by the owner's
   verification. Shell completion is narrowed to the library's `[suggest]` protocol; no `completion` command
   is shipped in Step 12. The estimate is revised to 2.75–3.75 pw and is presented for confirmation with
   this decision.

## 22. Acceptance criteria

- [ ] `mocassin validate samples/ceria/ceria.mocprj` exits 0 with the expected warnings; the human table and
      `validation-v1` report are unchanged from Step 07.
- [ ] `mocassin build samples/jobs/ceria-temperature-sweep.mocprj -o /tmp/ceria.msl` produces a package that
      `mocassin verify` accepts, using the implicit default parameterization (`parameterizations: []`), and
      writes `job_selection = "all"`. The ceria model-definition sample has no job template and is not a
      build sample.
- [ ] `--jobs 1-3` writes exactly jobs 1–3 with full-expansion IDs and `job_selection = "1-3"`; `--jobs 2,5`
      writes exactly those IDs with a gap and the same IDs in a full build.
- [ ] `--jobs` normalizes equivalently (`1,2,3` = `1-3`; a full selection = `all`) and rejects malformed,
      zero, descending, and out-of-range selections with exit 2.
- [ ] Build/job-template/parameterization selection auto-selects a single recipe, lists IDs on ambiguity,
      honours explicit overrides, and uses the implicit default when no template exists.
- [ ] `--force` atomically replaces an existing package; without it, an existing target exits 3 and the
      original file is byte-identical.
- [ ] Cancelling before publication leaves no temp file and preserves an existing package; cancelling after
      publication leaves the package and exits 6.
- [ ] `--json` prints exactly one schema-valid report with `exitCode` for `validate` and the seven
      report-backed commands, including usage errors; help/version and reserved commands emit no report;
      `--json` leaves stderr JSONL-only; JSON-Lines events validate against the `events` contract; `--quiet`
      additionally suppresses stage/progress/log events.
- [ ] `mocassin --help`, `mocassin <command> --help`, and `mocassin --version` exit 0; a parse error exits
      2 with `CLI001` and never exposes the library's default 1; `--jobs -1`, `--parallel -1`, and
      `-o -file` reach the handlers as values; `--` ends option parsing; response-file behaviour is pinned
      by a test.
- [ ] The AOT smoke host publishes for `linux-x64` with `PublishAot=true` and `TrimMode=full` with zero
      IL2xxx/IL3xxx warnings, the positive control emits IL2026/IL3050, and the native binary runs `--help`,
      `--version`, and one parse-error case. The real `Mocassin.Cli` publish result is recorded as evidence;
      dependency-owned warnings are attributed to Step 31 instead of failing Step 12.
- [ ] `build --preview` writes no package, reports `mode = "preview"`, projects `BuildResult.Preview`, and
      exits with the build table. The same payload is available to a library caller (Step 26) without a
      reload, recompile, or CLI reference.
- [ ] `clean --json` without `--dry-run` emits a `clean-v1` report with `dryRun = false`, `removedCount = 0`,
      and `CLI008`, and deletes nothing.
- [ ] `new --template blank` and `new --template ceria` create valid projects with fresh identity and seed;
      `compile -o` writes a `compiled-inspection-v1` document; `inspect`/`verify` read a real package.
- [ ] `doctor` exits 0 on CI; a missing solver is a warning. `clean --dry-run` lists only and never deletes.
- [ ] `--parallel` 1 and 4 produce the same signatures and `package-summary-v1`; output order is ascending
      `JobId`.
- [ ] The CLI builds and verifies 10⁴ jobs without the GUI within the Step 11 budget (< 10 min, < 2 GB).
- [ ] Documentation written (§18.1).

## 23. Tests

| Test | Assertion | Tier |
| --- | --- | --- |
| Parser/help | Library conventions: commands, long/short options, `--opt=value`, `--`, negative numbers (`--jobs -1`, `--parallel -1`, `-o -file`), missing values, unknown options, required options/arguments, duplicate/conflicting options, response-file behaviour; help/version exit 0; parse errors exit 2 with a CLI-owned `CLI001` message and the offending token; the library's default 1 is never returned | PR-fast |
| Exit codes | Every `BuildFailureKind`/status maps to the documented code, including `JobSelection`, `Preview`, `PackageWrite` without an I/O cause, and `None`/`Failed`; exit 5 is never emitted by `build` | PR-fast |
| Options injection | `BuildRequest.Options` reaches the clock, throttle, and channel capacity; a fixed clock makes progress/ETA deterministic | PR-fast |
| Selection | Auto-select, multiple build templates, multiple job templates, multiple parameterizations, overrides, unknown IDs, wrong-kind ID | PR-fast |
| Job selection | Grammar, normalization, dedup/merge, full-selection = `all`, range/gap errors, `inspect` metadata | PR-fast |
| Build | Ceria job sample builds, verifies, counts/signatures match, `job_selection = "all"` | PR-fast |
| Partial | Selected IDs, gaps, stable IDs vs full build, unselected lattices absent | PR-fast |
| Preview | No file, `mode = "preview"`, `BuildResult.Preview` from the shared builder, collections/configs/dopings, `--jobs` filter, no reload/recompile, a library caller gets the same payload, compile errors exit 4 | PR-fast |
| Force | Existing target exit 3; `--force` replaces; failed `--force` leaves the original intact | PR-fast |
| Cancellation | Token before/after publication; temp cleanup; existing package intact; exit 6 | PR-fast |
| Parallel | `--parallel 1` = `--parallel 4` signatures/summary; ascending job rows | PR-fast |
| Compact | Quality 9 + VACUUM mapping; signatures/summary unchanged; package verifies | PR-fast |
| Warnings | Validation warnings exit 1; promoted `PRM` warnings exit 4; unflagged build succeeds | PR-fast |
| JSON | Each report validates against its generated schema; usage errors still print one object; `clean --json` without `--dry-run` reports `CLI008`; help/version/reserved commands emit no report; JSON-Lines schema | PR-fast |
| AOT smoke | The smoke host publishes with `PublishAot=true`/`TrimMode=full` with zero IL2xxx/IL3xxx warnings; the positive control emits IL2026/IL3050; the native binary parses and reports a parse error with exit 2. The real CLI publish result is recorded; dependency-owned warnings are attributed to Step 31 | Nightly/release |
| Events | `build.stage` events precede `build.started`; `build.started`/`progress`/`finished` ordering; `finished` exactly once; `--json` leaves stderr JSONL-only; `--quiet` suppresses stage/progress/log | PR-fast |
| New/compile | Blank/ceria identity, name validation, safe create, inspection output, `CMP` exit 4 | PR-fast |
| Inspect/verify | Valid, corrupt, truncated, missing; `PKG` diagnostics; exit codes | PR-fast |
| Doctor | Healthy CI; injected `IDoctorCheck` pass/warn/fail lists cover runtime/SQLite/permission/disk/catalog/template outcomes; solver warning | PR-fast (check seam + fake probe) |
| Clean | Dry run lists only; no deletion; `.msl` untouched; `--older-than`; `--results` reserved; `--json` without `--dry-run` reports `CLI008` | PR-fast |
| Validate regression | Human/JSON output and exit codes unchanged | PR-fast |
| Performance | 10⁴ jobs build + verify under 10 min / 2 GB; ceria 100 jobs < 5 s | Nightly/release |

## 24. Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| CLI and GUI diverge | Both consume `Mocassin.Packages.Build` and the same event stream; the CLI is formatting only |
| Preview data path diverges or forces a reload/recompile | The pipeline owns `BuildResult.Preview` through the shared `BuildPreviewBuilder`; the CLI/GUI map the typed payload; a test asserts one evaluation and no second service |
| Build-stage validation silently skipped | The pipeline constructs `ProjectValidationPipeline` with `JobTemplateValidationStage` and shares the preview service; a test asserts the compiled-bound `JOB` checks run |
| `--warnings-as-errors` silently unplumbed | `ValidationOptions.WarningsAsErrors` on both calls; parameterization diagnostics are post-filtered; tests pin exit 1/4 |
| Partial builds confuse IDs | Full-expansion IDs, normalized `job_selection`, `inspect` shows the expression |
| Progress overhead | 10 Hz throttle, bounded channel, final-event guarantee |
| Parser/library behaviour changes | Version pinned exactly (3.0.0); the command graph and typed binding are ours; parser-convention tests cover arity, `--opt=value`, negative values, `--`, response files, and error text; an upgrade is a recorded dependency change |
| Library parse errors leak exit 1 | `CliApplication` maps `ParseResult.Errors` to `CLI001`/2 in one place and a test asserts the default 1 is never returned; help/version stay 0 |
| System.CommandLine AOT regresses | Strict AOT smoke host with a positive control runs nightly/release; the real CLI publish is recorded and dependency-owned warnings are attributed; only `linux-x64` is proven, so Windows/macOS and SQLite/MessagePack stay Step 31 gates |
| Completion promise overreaches | `completion` is removed from Step 12; `[suggest]` is accepted but not a contract; shell integration is a Step 31 packaging decision |
| Duplicate/bespoke parser creeps back | Feature 1 deletes the Step 07/10 hand-written parsers; no library project references `System.CommandLine`; structure/API tests and review check the command graph is the only parse path |
| Silent warnings | Report summary, `--warnings-as-errors` for validation/parameterization |
| Doctor checks not deterministic | `IDoctorCheck` seam injects pass/warn/fail checks; real checks run in the healthy CI path |
| 10⁴-job verification cost | Measure; if verification breaches the budget, record a reduced post-build check and leave full verification to `mocassin verify` (deviation, not silent) |
| `--force` destroys a good package before verification | Atomic publication plus write-time self-checks; `--force` is explicit; verification failure is reported with the package left in place |
| `new --template ceria` asset drift | Embedded template model hash equals `samples/ceria/ceria.mocprj`; freshness test |
| Console/cancellation/platform differences | `Console.CancelKeyPress` on all platforms, `PosixSignalRegistration` on Unix; Windows platform gate at closeout |
| Report schema churn | One `schemaVersion = 1` per report; additive fields only; generated schema tests |
| `build-preview-v1` consumer break | Pre-release only; the step page and CLI reference document the replacement; no manifest consumer existed |
| Source `OutputName` path escape | Treated as a file-name stem; separators/`..` are `CLI004` |

## 25. Done when

The CLI can create, validate, compile, build, preview, inspect, verify, and diagnose a project end to end,
with stable full-expansion job IDs, normalized partial selections, progress and cancellation, atomic
replacement, a verified package, schema-valid JSON/JSON-Lines output, a healthy `doctor`, a non-destructive
`clean --dry-run`, and complete documentation/parity/closeout evidence. The GUI can call the same build
pipeline without referencing the CLI and receives the same typed `BuildPreview` payload; there is no second
preview or build implementation.
