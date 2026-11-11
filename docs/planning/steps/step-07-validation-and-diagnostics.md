# Step 07 — Validation, Diagnostics, and Model Statistics

**Phase:** B. Model definition  
**Depends on:** 03, 04, 05, 06
**Delivers:** A cross-domain validation pipeline, a stable diagnostic catalog, model statistics, and the
headless `mocassin validate` command.

## Implementation status

- **Status:** Not started
- **Step branch:** —
- **Step CI:** —
- **Main merge:** —
- **Step tag:** —
- **Audit:** Not requested
- **Design review:** Re-review required at `60437ca` (round 2: Changes required at `4ddd571`; round 1: Changes required at `dc73ee4`)
- **Last updated:** 2026-11-11

| Feature | Branch | Status | Local gates | Step CI | Squash commit |
| --- | --- | --- | --- | --- | --- |
| `validation-core` | `feature/step-07-validation-and-diagnostics/validation-core` | Not started | — | — | — |
| `diagnostic-catalog` | `feature/step-07-validation-and-diagnostics/diagnostic-catalog` | Not started | — | — | — |
| `cross-domain-validation` | `feature/step-07-validation-and-diagnostics/cross-domain-validation` | Not started | — | — | — |
| `model-statistics` | `feature/step-07-validation-and-diagnostics/model-statistics` | Not started | — | — | — |
| `validate-cli` | `feature/step-07-validation-and-diagnostics/validate-cli` | Not started | — | — | — |
| `diagnostics-docs-and-fixtures` | `feature/step-07-validation-and-diagnostics/diagnostics-docs-and-fixtures` | Not started | — | — | — |
| `closeout` | `feature/step-07-validation-and-diagnostics/closeout` | Not started | — | — | — |

## Goal

Give users a single, trustworthy answer to "is this model valid, and if not, what exactly is wrong?" before
compilation or package building. Validation must be fast, deterministic, navigable, and testable. It must also
be usable from the CLI and the GUI through the same result contract.

## Original reference

- Validation is a reflection-discovered pipeline of `DataValidator` operations per domain, with
  `ValueSetting<T>` bounds (warning/hard limits) and `WarningMessage.IsCritical`.
- Conflict handling is a second post-validation pipeline that mutates dependent data; critical conflicts can
  leave the model mutated without rollback.
- The GUI validator re-pushes the entire project into the model, inspects `IOperationReport.IsGood`, and runs a
  customization-template feasibility test.
- There is no single diagnostic catalog; messages are produced by `ModelMessageSource` and string formatting.
- Known defects: post-mutation conflicts, off-by-one particle-set count, empty-list indexing, disabled
  self-exchange check, ambiguous end-state selection.

## Design

### 1. Existing foundation and ownership

Steps 03–06 already provide the domain validators and the first composition point:

- `Mocassin.Core.Diagnostics` owns `Diagnostic`, `DiagnosticSeverity`, `ModelPath`, and the stable-sorting
  `DiagnosticBag`.
- `Mocassin.Core` and `Mocassin.Symmetry` own the per-domain validators and their stable code constants.
- `Mocassin.Packages.Projects.ProjectValidationService` composes Core, Symmetry, `PROJ034`, and `PAR004`
  diagnostics. `ProjectStore` uses it by default; `ProjectEditor` keeps the Core-only default.

Step 07 does **not** replace that composition. It adds a higher-level pipeline in
`Mocassin.Packages.Projects.Validation`:

- `ProjectValidationService.Validate` remains the public stage-2 composition for existing callers
  (`ProjectStore`, `ProjectEditor`, editor code).
- `ProjectValidationPipeline` runs the load/structural stage, calls `ProjectValidationService.Validate` for
  the model/domain stage, then adds cross-domain and feasibility stages.
- `Mocassin.Cli` and `Mocassin.App.ViewModels` consume `ValidationResult`; they do not reimplement any rule.
- `Mocassin.Packages` remains the owner. No new .NET project and no new dependency are introduced.

The pipeline is stateless and thread-safe. Preview services are injected with bounded caches; the pipeline does
not keep its own diagnostic cache.

### 2. Pipeline API

```csharp
namespace Mocassin.Packages.Projects.Validation;

public enum ValidationStage
{
    Load,
    Model,
    CrossDomain,
    Feasibility,
    Build,
}

public enum ValidationStageStatus
{
    Completed,
    Skipped,
    Failed,
}

public sealed record ValidationOptions
{
    public bool IncludeStatistics { get; init; }
    public bool RequireBuildable { get; init; }
    public bool WarningsAsErrors { get; init; }
    public bool RunBuildStage { get; init; }
    public int CellCountA { get; init; } = 1;
    public int CellCountB { get; init; } = 1;
    public int CellCountC { get; init; } = 1;
    public long MaxEstimatedRuleCount { get; init; } = 1_000_000;
    public long MaxEstimatedMemoryBytes { get; init; } = 2L * 1024 * 1024 * 1024;
    public static ValidationOptions Default { get; } = new();
}

public sealed record DiagnosticSummary(int Errors, int Warnings, int Infos);

public sealed record ValidationStageResult(
    ValidationStage Stage,
    ValidationStageStatus Status,
    int DiagnosticCount,
    TimeSpan Elapsed);

public sealed record ValidationContext(
    Project Project,
    ValidationOptions Options,
    DiagnosticCatalog Catalog,
    ImmutableArray<ValidationStageResult> PreviousStages);

public sealed record ValidationResult(
    Guid? ProjectId,
    string? ModelHash,
    string? DocumentHash,
    ProjectLoadFailureKind? LoadFailure,
    bool IsValid,
    DiagnosticSummary Summary,
    ImmutableArray<Diagnostic> Diagnostics,
    ModelStatistics? Statistics,
    ImmutableArray<ValidationStageResult> Stages);

public sealed class ProjectValidationPipeline
{
    public ProjectValidationPipeline(
        IStructurePreviewService? structurePreview = null,
        IEnergyPreviewService? energyPreview = null,
        ITransitionPreviewService? transitionPreview = null,
        IEnumerable<IValidationStage>? additionalStages = null);

    // In-memory entry: Load is caller-owned and is reported as Skipped.
    public ValidationResult Validate(
        Project project,
        ImmutableArray<Diagnostic> loadDiagnostics = default,
        ValidationOptions? options = null,
        CancellationToken cancellationToken = default);

    // File entry: performs the Load stage through ProjectStore with ValidateOnLoad = false.
    public Task<ValidationResult> ValidateFileAsync(
        string projectPath,
        ProjectStore? store = null,
        ValidationOptions? options = null,
        CancellationToken cancellationToken = default);
}

public interface IValidationStage
{
    // Only Build-stage extensions are accepted; any other value is a constructor error.
    ValidationStage Stage { get; }
    ImmutableArray<Diagnostic> Validate(ValidationContext context, CancellationToken cancellationToken);
}
```

- `ProjectId`, `ModelHash`, and `DocumentHash` are non-null for any successfully loaded document and `null` in
  the result of a pre-load failure, matching the `validation-v1` report. `LoadFailure` is `null` when the
  document loaded and carries the `ProjectLoadFailureKind` returned by `ProjectStore` otherwise, so
  `ValidateFileAsync` propagates the parse-vs-I/O distinction to the CLI without re-reading `PROJ002`.
- `IsValid` is true only when no `Error` remains after applying `WarningsAsErrors` and
  `RequireBuildable` severity promotion.
- `Summary` always reports the raw Error/Warning/Info counts. A separate policy flag decides the exit code; the
  JSON report carries both `valid` and `exitCode`.
- `Statistics` availability is independent of policy promotion and of feasibility/budget errors: it is non-null
  exactly when `IncludeStatistics` is set, the document loaded, the `Model` stage produced no `Error`, and every
  required preview succeeded. It is `null` otherwise. A `validation-v1` report may therefore have
  `valid: false` and a non-null `statistics` object (for example `SIM040`/`SIM041` budget errors or a promoted
  warning). `--stats` never changes diagnostics or `valid`.
- `Stages` records skipped stages and per-stage elapsed time. `Completed` means the stage ran (even with
  warnings); `Failed` means the stage ran and produced at least one raw `Error`; `Skipped` means it did not run.
  `DiagnosticCount` is the number of that stage's diagnostics that survive global sorting and deduplication, so
  the counts sum to the final `Diagnostics.Length`. `Elapsed` is measured for every stage the pipeline runs. In
  the in-memory entry, `Load` is `Skipped` with count `0` and `TimeSpan.Zero` when the caller supplies no load
  diagnostics; when the caller supplies load diagnostics it is `Failed` (if any is an `Error`) or `Completed`,
  with the surviving count and `TimeSpan.Zero`, because the caller owns the timing. Statistics-preview
  diagnostics are attributed to the `Feasibility` stage, which is where the opt-in estimates and budget checks
  run; `Feasibility` is never skipped while statistics are computed.
- `additionalStages` is the extension point for build checks (Step 09 parameterization, Step 10 job templates,
  Step 11 package limits, Step 12 build orchestration). Every supplied stage must declare
  `ValidationStage.Build`; the constructor throws `ArgumentException` otherwise. Step 07 does not implement
  stage `Build`.
- `ValidateFileAsync` is the only entry that performs I/O. It uses a `ProjectStore` configured with
  `ValidateOnLoad = false`; if an injected store has `ValidateOnLoad = true`, the pipeline throws
  `ArgumentException` rather than validate twice. CLI and GUI consumers use this entry (or a store already
  configured for parse-only load) so load diagnostics are produced exactly once.
- Cell counts and budgets are validated when the pipeline runs. `CellCountA/B/C < 1` is a programmer error and
  throws `ArgumentOutOfRangeException`; project content problems remain diagnostics, not exceptions.

### 3. Stage order and ownership

| Stage | Responsibility | Owner | Runs in `validate` |
| --- | --- | --- | --- |
| `Load` | File bounds, UTF-8, JSON/schema, version/migration, I/O diagnostics (OS sharing failures only; the read-only path never checks the editing lock) | `ProjectStore` via `ValidateFileAsync`; caller-owned in the in-memory entry | `ValidateFileAsync` only |
| `Model` | Core graph rules, domain invariants, catalog/cell/geometry, `PROJ034`, `PAR004` | `ProjectValidationService` | If load succeeded |
| `CrossDomain` | Transition paths versus energy cutoffs, simulation/transition/doping relationships not expressible in one domain | Step 07 `CrossDomainValidator` | If `Model` has no errors |
| `Feasibility` | Mobile starts, unstable sites, buildability warnings, estimated rule/memory hard limits | Step 07 `FeasibilityValidator` | If `Model` has no errors |
| `Build` | Parameterization signatures, job-template compatibility, supercell/package limits | Steps 09–12 via `IValidationStage` | Only when `RunBuildStage` is true |

- Load errors stop the pipeline and the result has no `Model`/`CrossDomain`/`Feasibility` diagnostics;
  `CrossDomain` and `Feasibility` are skipped when `Model` has errors; `Statistics` is omitted when it cannot
  be computed (see §2), not merely because a feasibility warning exists.
- Cross-domain and feasibility diagnostics are still sorted with the model diagnostics into one stable list.
- `validate` sets `RunBuildStage = false` and reports stages 1–4. `build` sets `RunBuildStage = true`; additional
  `Build` stages then run after `Feasibility`, in constructor registration order, and only when no earlier stage
  produced an `Error`. A skipped Build stage still appears in `Stages` with `Skipped`.
- Consumers that already hold a `Project` call the in-memory entry; its `Load` stage is reported as `Skipped`
  with zero elapsed, and load diagnostics must be passed in by the caller. GUI and CLI file paths use
  `ValidateFileAsync`, so both report the same stage list.

### 4. Cross-domain and feasibility rules

Step 07 adds only rules that cannot live in one domain. Existing domain diagnostics are aggregated, never
re-emitted.

| Code | Severity | Rule | Notes |
| --- | --- | --- | --- |
| `ENM018` | Warning | A KMC transition path leaves the relevant stable or unstable environment cutoff. | Precise boundary in the bullet below. Step 05 defines the constant/help; Step 07 emits it. |
| `SIM038` | Warning | A KMC simulation that has at least one transition has no mobile start particle. | One per affected KMC simulation; suppressed when `SIM003` fires for it or when `SIM039` fires. Promoted by `RequireBuildable`. |
| `SIM039` | Warning | The model authors at least one KMC simulation with transitions but has no particle mobile in any of them. | One model-level diagnostic; when it fires, all per-simulation `SIM038` are suppressed. Promoted by `RequireBuildable`. |
| `SIM040` | Error | Estimated compiled rule count exceeds `MaxEstimatedRuleCount`. | Advisory `TransitionPreview` sum (`long`); Step 08 remains authoritative. |
| `SIM041` | Error | Estimated energy-table memory exceeds `MaxEstimatedMemoryBytes`. | Uses `EnergyCostPreview.EstimatedMemoryBytes`; both budgets live in `ValidationOptions`. |

- `SIM003` (no transitions) remains a Step 06 warning. `RequireBuildable` promotes `SIM003`, `SIM038`,
  `SIM039`, and `ENM018`; `WarningsAsErrors` promotes every warning.
- `SIM038` and `SIM039` are deliberately non-overlapping and are KMC-only: MMC transitions have no start pool.
  Evaluation order is: (1) skip any simulation with zero transitions because Step 06 `SIM003` already reports
  it; (2) skip any simulation whose transitions are rule-invalid (`SIM001`/`TRN` errors) because those already
  explain why no usable transition exists; (3) among the remaining KMC simulations, if none has a mobile start
  particle emit `SIM039` once and suppress all `SIM038`; otherwise emit `SIM038` only for the simulations that
  lack a mobile start particle. In a mixed model with one empty-transition simulation and one transition-bearing
  simulation without a mobile start, `SIM003` covers only the first and `SIM039` still fires for the second;
  `SIM003` never suppresses the model-wide diagnostic.
- Do **not** duplicate `PAR004`, `PROJ034`, `LAT`, `TRN`, `ENM001`–`ENM017`, `ENM019`–`ENM021`, or `SIM001`–
  `SIM037`; they come from stage `Model`.
- Parameterization signature checks, job-template references, supercell feasibility, and package limits are
  **not** Step 07 rules. They belong to Steps 09–12 and plug into stage `Build`.
- `ENM018` boundary: for each kinetic transition whose resolved path exists, walk consecutive reference-path
  positions `(p_i, p_i+1)`. The applicable cutoff is the stable environment cutoff
  (`Model.StableEnvironment ?? StableEnvironment.Default`) when `p_i` is stable, and the cutoff of the
  `UnstableEnvironment` for `p_i`'s owning reference site when `p_i` is unstable (falling back to the stable
  cutoff when that environment is absent, mirroring `EnergyGeometryContext`). The rule fires when the
  minimum-image distance is strictly greater than that cutoff; equality is allowed. Emit at most one `ENM018`
  per transition, at the first offending hop, with `Path.Domain = "kineticTransition"`,
  `Path.ObjectId =` the transition's ID, and `Path.Property = "referencePath[i]"` naming the position left. If the usable cutoff is non-positive or non-finite, skip the check because `ENM` validation already
  reports the invalid cutoff. If path resolution produced `TRN020`, `TRN028`, or another path error for that
  transition, skip `ENM018` to avoid a cascade.

### 5. Diagnostic catalog

The catalog is the stable, documented surface of all diagnostics. It is not a replacement for the code
constants; it is a registry over them.

```csharp
namespace Mocassin.Packages.Projects.Validation;

public sealed record DiagnosticDescriptor(
    string Code,
    DiagnosticSeverity DefaultSeverity,
    string Owner,
    string Title,
    string Help,
    string DocumentationAnchor);

public sealed class DiagnosticCatalog
{
    public static DiagnosticCatalog Default { get; }
    public ImmutableArray<DiagnosticDescriptor> All { get; }
    public bool TryGet(string code, out DiagnosticDescriptor descriptor);
}
```

- Composition:
  - `Mocassin.Core.Diagnostics.CoreDiagnosticCatalog` registers the Core code classes in
    `Mocassin.Core.Diagnostics` and `Mocassin.Core.Project` (including `ProjectDiagnosticCodes`).
  - `Mocassin.Symmetry.Diagnostics.SymmetryDiagnosticCatalog` is a new public type in `Mocassin.Symmetry` that
    registers `STR`/`SYM` codes by reading the existing internal `StructureDiagnosticCodes` and
    `SymmetryDiagnosticCodes` constants inside their own assembly. The constants stay internal; no reflection is
    used.
  - `Mocassin.Packages.Projects.Validation.DiagnosticCatalog` composes Core + Symmetry + package codes and
    Step 07 codes.
  - `JOB` codes are reserved for Step 10 and are added when Step 10 defines them. They are not stubbed in Step 07.
- `DiagnosticCatalog.All` is sorted by ordinal code. Duplicate codes are a test failure.
- Completeness is proved by two assembly-local tests: `Mocassin.Core.Tests` asserts every `public const` code in
  the Core code classes is registered by `CoreDiagnosticCatalog`, and `Mocassin.Symmetry.Tests` does the same for
  the internal Symmetry constants via its existing `InternalsVisibleTo`. The package-level test asserts that the
  composed catalog is exactly the union of the domain catalogs plus the package/Step 07 codes, and that every
  public package code is registered. A docs-drift test proves every catalog code appears in
  `docs/reference/diagnostics.md` and that the page has no extra code.
- Descriptors are split by domain into files under `Mocassin.Packages/Projects/Validation/Diagnostics/`
  (`CoreDescriptors.cs`, `SymmetryDescriptors.cs`, `PackageDescriptors.cs`, `Step07Descriptors.cs`) to stay well
  inside the 800-line/file and 20-files/folder limits.
- `Diagnostic.Hint` remains the actionable per-instance hint. The catalog `Help` is the general help text used
  by the GUI and documentation; it must not contradict the hint.
- `DiagnosticBag` already provides the stable severity/code/domain/object/property/message ordering. The
  pipeline deduplicates exact `(Code, Path, Message)` triples after sorting and records duplicate suppression in
  tests.

### 6. Model statistics

`ModelStatistics` is advisory but accurate enough to warn before an expensive build. It is computed only on
demand; `validate` without `--stats` must not pay for it.

```csharp
public sealed record ModelStatistics(
    int ParticleCount,
    int ParticleSetCount,
    int ReferenceSiteCount,
    long P1PositionCount,
    long StablePositionCount,
    long UnstablePositionCount,
    int KineticTransitionCount,
    int MetropolisTransitionCount,
    int ClusterCount,
    long EstimatedPairInteractionCount,
    long EstimatedUniqueClusterPermutations,
    long EstimatedRuleCount,
    long EstimatedMemoryBytes,
    int CellCountA,
    int CellCountB,
    int CellCountC,
    long EstimatedPositions);
```

- Counts that can exceed `int` — P1/stable/unstable positions, pair interactions, cluster permutations, memory,
  estimated positions, and the rule total — are `long`. The 63⁹ cluster case would overflow `int`. The
  per-transition preview exposes `int ExpectedRuleCount`, but the statistics service accumulates the sum in
  `long`, and `MaxEstimatedRuleCount` is `long` so the budget check cannot overflow.
- `P1PositionCount` is the total unit-cell P1 position count. `StablePositionCount` and
  `UnstablePositionCount` partition it by the owning reference site's stability.
- `EstimatedPositions = P1PositionCount * CellCountA * CellCountB * CellCountC`. Step 07 defaults the cell counts
  to `1×1×1`; Step 10 passes the authored default-supercell counts.
- Sources, field by field:
  - `ParticleCount`, `ParticleSetCount`, `ReferenceSiteCount`, `KineticTransitionCount`,
    `MetropolisTransitionCount`, `ClusterCount`: the immutable model (`ParticleModel`, structure sites, and the
    transition/cluster collections).
  - `P1PositionCount`: `StructurePreview.PositionList.Length`. `StablePositionCount`/`UnstablePositionCount`:
    the same preview's `Orbits` partition by the owning reference site's stability.
  - `EstimatedPairInteractionCount`: `EnergyCostPreview.UniquePairInteractionCount` (distinct `EnergyId` pair
    tables), not the sampled `PairImageCount`. `EstimatedUniqueClusterPermutations` and
    `EstimatedMemoryBytes`: the `ClusterCostPreview` entries and `EnergyCostPreview.EstimatedMemoryBytes`.
  - `EstimatedRuleCount`: the `long` sum of `TransitionPreview.ExpectedRuleCount` over the authored kinetic and
    Metropolis transitions. This is **before** conservation filtering and deduplication; Step 08 remains
    authoritative.
- The 1,000,000-rule and 2 GiB-memory defaults are initial guardrails, not physical constants. They are chosen
  to fail fast long before the package writer's practical limits and are expected to be tuned when Step 08 fixes
  rule generation and Step 11 fixes package/supercell limits; both live in `ValidationOptions` and are documented
  as advisory thresholds.
- If a required preview fails, `Statistics` is `null` and its diagnostics are appended to the result under the
  `Feasibility` stage result (statistics and budgets are feasibility work). The pipeline does not throw for
  project or preview failures.
- Statistics are not persisted in the project and are not part of the model hash.

### 7. CLI contract

```text
mocassin validate <project.mocprj> [--json] [--warnings-as-errors] [--stats]
```

- Loads the project read-only through `ProjectStore`; it never writes, locks for edit, migrates in place, or
  mutates the model. The command calls `ProjectValidationPipeline.ValidateFileAsync`, which uses
  `ProjectStoreOptions.ValidateOnLoad = false` so the model/domain stage runs exactly once. Existing store
  callers keep the default `true`. The pipeline rejects a store configured for validation-on-load instead of
  double validating.
- `ProjectLoadResult` gains a structured `ProjectLoadFailureKind` (`None`, `NotFound`, `TooLarge`, `Io`,
  `InvalidContent`) set by `ProjectStore` at the existing failure points. `ValidateFileAsync` copies it to
  `ValidationResult.LoadFailure`; the CLI maps `NotFound`/`TooLarge`/`Io` to exit `3` and `InvalidContent`
  (BOM, UTF-8, malformed JSON, schema, migration) to exit `1`. The read-only path never acquires the editing
  lock; an OS sharing/I/O denial surfaces as `Io`/exit `3`.
- Exit codes:
  - `0` — valid under the selected policy (warnings allowed unless `--warnings-as-errors`);
  - `1` — validation/model errors, warnings promoted by `--warnings-as-errors`/`RequireBuildable`, or project
    content that cannot be parsed or migrated;
  - `2` — usage/argument error;
  - `3` — file not found, unreadable, or other I/O failure.
- `--json` prints exactly one JSON report to stdout for every invocation, including usage errors and load
  failures; human progress and errors go to stderr. For a pre-load failure the report carries `projectId`,
  `modelHash`, and `documentHash` as `null`, an empty `diagnostics` array when the failure occurred before a
  store diagnostic existed, and the matching `exitCode`/`valid: false`. The report is the `validation-v1`
  contract in [`02-data-contracts.md`](../02-data-contracts.md) §5.6.
- Human output is a stable table: severity, code, message, object/property, hint. The summary follows the table.
- `--stats` computes and prints/emits statistics. Without it, the JSON report has `"statistics": null` and the
  human output omits the table. Statistics remain present for a model that is invalid only because of
  feasibility/budget errors or policy-promoted warnings; see §2.
- The command uses the same `ValidationResult` as the GUI. `valid` in the JSON report is the policy-adjusted
  result; `summary` keeps raw counts.
- Step 07 adds a minimal hand-written command parser for `validate`. Step 12 extends the same command surface
  and may replace the parser behind it if the full CLI needs a framework. No new CLI dependency is required in
  Step 07.

### 8. GUI contract (steps 24–25)

- The GUI calls `ProjectValidationPipeline.ValidateFileAsync` (or passes its already parsed project plus load
  diagnostics to the in-memory entry) with a cancellation token and a debounce policy; it does not call domain
  validators directly and does not let `ProjectStore` validate on load as well.
- With no errors, the status bar shows the raw warning/info counts and a "model valid" indicator.
- With errors, the validator panel lists `Diagnostic.Code`, `Severity`, `Message`, `Path.Domain`,
  `Path.ObjectId`, and `Path.Property`.
- Double-click navigates via `ModelPath` to the owning editor and property. `Related` paths are secondary
  navigation targets.
- "Revalidate" runs the pipeline with cancellation and a timestamp. It never mutates the project.
- Statistics are shown in the model-overview panel only when the overview option enables `IncludeStatistics`;
  clicking a count navigates to the relevant editor.
- Step 07 defines only the `ValidationResult`/`ModelStatistics` contract. The panels and commands are Steps
  24–25.

### 9. Performance, caching, and cancellation

- Reuse the existing `StructurePreviewService`, `EnergyPreviewService`, and `TransitionPreviewService` caches.
  The validation pipeline does not add a second diagnostic cache.
- Target on a laptop for the ceria sample:
  - `validate` without statistics: `< 200 ms`;
  - `validate --stats`: `< 2 s`;
  - a model with 10k P1 positions: validation completes without pathological memory growth; statistics may be
    slower and are opt-in.
- Cancellation is checked between stages, per transition in the cross-domain stage, per simulation in the
  feasibility stage, and before and after each statistics preview call. The preview-service methods themselves
  take no `CancellationToken` in Steps 03–06, so one preview call is atomic; adding tokens to those interfaces
  is a later change and is not required for Step 07. A cancelled run returns no partial result and exits
  non-zero without writing.
- No allocation-heavy reflection over diagnostics in the hot path; the catalog is static.

### 10. Determinism and no mutation

- `DiagnosticBag` ordering is the contract: severity (Error, Warning, Info), code, domain, object ID, property,
  message. Step 07 reuses it and adds deterministic deduplication.
- Validation never mutates the project. The pipeline takes the immutable `Project` and returns a new result.
  A test compares the model and document hashes before/after validation.
- Same project, options, catalog, and contract versions produce the same `ValidationResult` diagnostics and
  statistics (timings excepted).

### 11. Deferred scope

- Build-stage checks (parameterization, job templates, supercells, package limits) are Steps 09–12.
- GUI panels, navigation, and editor integration are Steps 24–25.
- Compiled rule generation and rule ambiguity are Step 08.
- Localization is post-1.0; messages are English and keyed by code.
- A persistent diagnostic result cache is not introduced; preview caches are enough.

## Implementation tasks

- [ ] Implement `ProjectValidationPipeline`, `ValidationOptions`, `ValidationResult`, `ValidationContext`, and
      stage results, including the in-memory and file entries.
- [ ] Add `ProjectStoreOptions.ValidateOnLoad` (default `true`) and `ProjectLoadFailureKind`, and use the
      parse-only load path from `validate`.
- [ ] Refactor/keep `ProjectValidationService` as the stage-2 composition.
- [ ] Implement the load/model/cross-domain/feasibility stage order, skipped/failed stage reporting, and the
      `RunBuildStage` gating for additional `IValidationStage` stages.
- [ ] Implement `ENM018` and `SIM038`–`SIM041` with the documented severities and suppression rules.
- [ ] Implement `DiagnosticDescriptor`, `CoreDiagnosticCatalog`, `SymmetryDiagnosticCatalog`, and the composed
      `DiagnosticCatalog`, split into domain descriptor files.
- [ ] Add catalog completeness, duplicate-code, and documentation-drift tests, including the assembly-local
      internal-constant checks.
- [ ] Implement `ModelStatistics` and `ModelStatisticsService` with the preview services and `long` accumulators.
- [ ] Implement `mocassin validate` with human/JSON output, the structured load-failure mapping, exit codes, and
      `--stats`.
- [ ] Extend `contracts/manifest.json` with a `reports.validation` entry, extend `tools/contracts/generate.py` to
      validate reports and emit `contracts/generated/schema/validation-v1.schema.json` (matching the existing
      `<id>-v<version>.schema.json` convention), update `tools/contracts/README.md` and the generated
      `ValidationReportVersion` constant in `Mocassin.Contracts`, and add the schema test.
- [ ] Update `PublicAPI.Unshipped.txt`/ApiCompat for new public types in `Mocassin.Packages`,
      `Mocassin.Symmetry`, and `Mocassin.Contracts`.
- [ ] Add SPDX headers to new files and confirm the licensing/provenance checks stay green.
- [ ] Add diagnostics reference and troubleshooting pages, add them to `zensical.toml` navigation and
      `docs/reference/index.md`, and add the CLI/format references.
- [ ] Add the ceria good/bad validation fixtures and the per-code emission matrix.
- [ ] Add performance tests for `validate` and `validate --stats`.
- [ ] Add the GUI contract tests for `ModelPath` navigation coverage.
- [ ] Update the step page, status dashboard, parity matrix, testing-and-parity, closeout evidence, and the
      audit request as part of the closeout feature.

## Sizing

The seven-row feature list (six features plus closeout) matches the roadmap's 2–3 person-week estimate and is
below the 6–8-feature split trigger in `04-roadmap.md` §3.1. No split is proposed; the closeout feature carries
the status, parity, and audit updates.

## Acceptance criteria

- [ ] The ceria sample validates with zero errors and the warning set recorded in its fixture.
- [ ] Every code emitted by the pipeline and the stage-2 validators has a catalog entry. Step 07 adds a
      per-code test with expected severity, path, and hint for every code it emits and records existing Step
      03–06 tests for their severity coverage.
- [ ] The catalog has no duplicate or undocumented codes.
- [ ] Validation never mutates the project; model and document hashes are unchanged.
- [ ] Diagnostics are deterministically sorted and deduplicated.
- [ ] `mocassin validate --json` validates against the `validation-v1` schema, including load-failure reports.
- [ ] Exit codes match the contract, including warnings-as-errors, parse failures, and I/O versus validation
      errors.
- [ ] `--stats` is opt-in and deterministic. Statistics are `null` only when not requested, the document did
      not load, the model stage failed, or a preview failed; `--stats` never changes diagnostics or `valid`.
- [ ] Ceria validation completes in `< 200 ms`; ceria `--stats` completes in `< 2 s`.
- [ ] The GUI contract can navigate from every diagnostic `ModelPath` to an object/property (verified in
      Steps 24–25).
- [ ] Documentation written.

## Documentation

At this point, document validation and diagnostics:

- `docs/reference/diagnostics.md` — complete catalog, severities, owners, paths, hints; checked against
  `DiagnosticCatalog`. Added to `zensical.toml` navigation and `docs/reference/index.md`.
- `docs/troubleshooting/errors.md` — common validation failures and fixes; linked from the diagnostics page.
- `docs/reference/cli.md` — the `validate` command and `validation-v1` report, including the report's
  `null` identity fields on load failure; Step 30 completes the full CLI reference.
- `docs/development/testing-and-parity.md` — validation pipeline, catalog completeness, determinism, per-code
  fixtures.
- `docs/user-guide/` — inline validation/help behavior for the affected editors, added with Steps 24–25.

## Tests

| Test | Assertion |
| --- | --- |
| Pipeline order | Load → Model → CrossDomain → Feasibility; later stages skip on earlier errors; in-memory Load is `Skipped` |
| No mutation | Model and document hashes unchanged after validation |
| Catalog completeness | Every code constant appears once; no duplicate or undocumented code |
| Catalog docs drift | `docs/reference/diagnostics.md` matches the catalog exactly |
| Per-code emission | Every Step 07 emitted code has a real fixture/test with severity, path, and hint |
| Cross-domain | `ENM018` fires at the documented hop/cutoff boundary; no cascade after `TRN020`/`TRN028` |
| Feasibility | `SIM038`–`SIM041` trigger at the documented boundaries; `SIM039` suppresses `SIM038`; mixed `SIM003`/`SIM039` models behave as specified; MMC is excluded; `RequireBuildable` promotes the buildability set |
| Determinism | Same project/options yields identical ordered diagnostics and statistics |
| Deduplication | Exact duplicate `(code, path, message)` is emitted once |
| Statistics | Counts and estimates match hand-computed small models; `null` only per §2; non-null with `SIM040`/`SIM041` |
| Statistics sources | P1/stability counts match `StructurePreview`; pair interactions/permutations/memory match `EnergyCostPreview`; rule total is a `long` sum |
| CLI human output | Stable columns and summary for good/bad projects |
| CLI JSON | `validation-v1` schema, exit code, `valid`, `summary`, `--stats` nullability, load-failure nulls |
| CLI errors | Missing file, malformed JSON, unreadable file, and usage errors map to 3/1/3/2 with one report; `ValidationResult.LoadFailure` carries the parse/I-O distinction |
| Build extension | A `Build` stage runs only with `RunBuildStage`; non-`Build` stages are rejected; skipped reasons are recorded |
| Performance | Ceria `< 200 ms` without stats and `< 2 s` with stats |
| Cancellation | Cancelled validation returns no partial success and writes nothing |

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Diagnostic codes become unstable | Treat them as API; catalog completeness and docs-drift tests |
| Duplicate/conflicting messages | Deduplicate by code + path + message; aggregate, never re-emit stage-2 codes |
| Expensive feasibility/statistics | `--stats` opt-in; reuse preview caches; cancellation; hard budgets |
| Validation diverges from compilation | Share sampling/rule/preview code; Step 08 parity tests; mark estimates advisory |
| Stage 5 is invented too early | Keep `IValidationStage` as the only extension point, restrict it to `Build`, and gate it on `RunBuildStage`; Steps 09–12 own their checks |
| `ENM018` cascades after path errors | Emit only for resolved paths; skip on existing path errors |
| CLI parser rework in Step 12 | Isolate parsing behind the command surface; Step 12 may replace the parser without changing `validate` semantics |

## Done when

A user can validate any project headlessly or from the GUI, see precise and navigable diagnostics from one
catalog, trust that validation does not mutate the model, and opt into accurate model statistics before a build.
