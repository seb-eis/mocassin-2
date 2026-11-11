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
- **Design review:** Ready for review
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
    public int CellCountA { get; init; } = 1;
    public int CellCountB { get; init; } = 1;
    public int CellCountC { get; init; } = 1;
    public int MaxEstimatedRuleCount { get; init; } = 1_000_000;
    public long MaxEstimatedMemoryBytes { get; init; } = 2L * 1024 * 1024 * 1024;
    public static ValidationOptions Default { get; } = new();
}

public sealed record DiagnosticSummary(int Errors, int Warnings, int Infos);

public sealed record ValidationStageResult(
    ValidationStage Stage,
    ValidationStageStatus Status,
    int DiagnosticCount,
    TimeSpan Elapsed);

public sealed record ValidationResult(
    Guid ProjectId,
    string ModelHash,
    string DocumentHash,
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

    public ValidationResult Validate(
        Project project,
        ImmutableArray<Diagnostic> loadDiagnostics = default,
        ValidationOptions? options = null,
        CancellationToken cancellationToken = default);
}

public interface IValidationStage
{
    ValidationStage Stage { get; }
    ImmutableArray<Diagnostic> Validate(ValidationContext context, CancellationToken cancellationToken);
}
```

- `IsValid` is true only when no `Error` remains after applying `WarningsAsErrors` and
  `RequireBuildable` severity promotion.
- `Summary` always reports the raw Error/Warning/Info counts. A separate policy flag decides the exit code; the
  JSON report carries both `valid` and `exitCode`.
- `Statistics` is `null` unless `IncludeStatistics` is set and stages `Load`–`CrossDomain` completed without
  errors.
- `Stages` records skipped stages and per-stage elapsed time for diagnostics and performance tests.
- `additionalStages` is the extension point for builds (Step 09 parameterization, Step 10 job templates,
  Step 12 build orchestration). Step 07 does not implement stage `Build`.

### 3. Stage order and ownership

| Stage | Responsibility | Owner | Runs in `validate` |
| --- | --- | --- | --- |
| `Load` | File bounds, UTF-8, JSON/schema, version/migration, lock/I/O diagnostics | `ProjectStore` + pipeline | Always |
| `Model` | Core graph rules, domain invariants, catalog/cell/geometry, `PROJ034`, `PAR004` | `ProjectValidationService` | If load succeeded |
| `CrossDomain` | Transition paths versus energy cutoffs, simulation/transition/doping relationships not expressible in one domain | Step 07 `CrossDomainValidator` | If `Model` has no errors |
| `Feasibility` | Mobile starts, unstable sites, buildability warnings, estimated rule/memory hard limits | Step 07 `FeasibilityValidator` | If `Model` has no errors |
| `Build` | Parameterization signatures, job-template compatibility, supercell/package limits | Steps 09/10/12 via `IValidationStage` | Only `build` |

- Load errors stop the pipeline. `Model` errors stop `CrossDomain` and `Feasibility`; `Statistics` is omitted.
- Cross-domain and feasibility diagnostics are still sorted with the model diagnostics into one stable list.
- `validate` never runs stage `Build`; it reports only stages 1–4. `build` can append stage 5 without changing
  the `ValidationResult` shape.

### 4. Cross-domain and feasibility rules

Step 07 adds only rules that cannot live in one domain. Existing domain diagnostics are aggregated, never
re-emitted.

| Code | Severity | Rule | Notes |
| --- | --- | --- | --- |
| `ENM018` | Warning | A KMC transition path leaves the relevant stable or unstable environment cutoff. | Step 05 defines the constant/help; Step 07 emits it using the resolved transition path and energy geometry. |
| `SIM038` | Warning | A simulation has no mobile start particle. | Buildability warning; promoted to error by `RequireBuildable`. |
| `SIM039` | Warning | A KMC simulation has no unstable site. | Buildability warning; promoted to error by `RequireBuildable`. |
| `SIM040` | Warning | The model has no mobile particle for any authored simulation. | Buildability warning; promoted to error by `RequireBuildable`. |
| `SIM041` | Error | Estimated compiled rule count exceeds the configured hard budget. | Uses the advisory transition preview; Step 08 remains authoritative. |
| `SIM042` | Error | Estimated energy-table memory exceeds the configured hard budget. | Uses `EnergyCostPreview.EstimatedMemoryBytes`; configurable in `ValidationOptions` via a future settings record. |

- `SIM003` (no transitions) remains a Step 06 warning. `RequireBuildable` promotes it together with
  `SIM038`–`SIM040` and `ENM018`; `WarningsAsErrors` promotes every warning.
- Do **not** duplicate `PAR004`, `PROJ034`, `LAT`, `TRN`, `ENM001`–`ENM017`, `ENM019`–`ENM021`, or `SIM001`–
  `SIM037`; they come from stage `Model`.
- Parameterization signature checks, job-template references, supercell feasibility, and package limits are
  **not** Step 07 rules. They belong to Steps 09, 10, 11, and 12 and plug into stage `Build`.
- `ENM018` is emitted only when the transition path resolves. If path resolution already produced `TRN020` or
  another error, skip `ENM018` for that transition to avoid cascades.

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
  - `Mocassin.Core.Diagnostics.CoreDiagnosticCatalog` registers the Core code classes.
  - `Mocassin.Symmetry.Diagnostics.SymmetryDiagnosticCatalog` becomes public and registers `STR`/`SYM` codes,
    including the currently internal constants.
  - `Mocassin.Packages.Projects.Validation.DiagnosticCatalog` composes Core + Symmetry + package codes and
    Step 07 codes.
  - `JOB` codes are reserved for Step 10 and are added when Step 10 defines them. They are not stubbed in Step 07.
- `DiagnosticCatalog.All` is sorted by ordinal code. Duplicate codes are a test failure.
- A contract test proves that every public code constant in Core and Symmetry appears exactly once in the
  catalog. A second test proves every catalog code appears in `docs/reference/diagnostics.md` and that the page
  has no extra code.
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
    int EstimatedRuleCount,
    long EstimatedMemoryBytes,
    int CellCountA,
    int CellCountB,
    int CellCountC,
    long EstimatedPositions);
```

- Counts that can exceed `int` — P1/stable/unstable positions, pair interactions, cluster permutations, memory,
  and estimated positions — are `long`. The 63⁹ cluster case would overflow `int`.
- `P1PositionCount` is the total unit-cell P1 position count. `StablePositionCount` and
  `UnstablePositionCount` partition it by the owning reference site's stability.
- `EstimatedPositions = P1PositionCount * CellCountA * CellCountB * CellCountC`. Step 07 defaults the cell counts
  to `1×1×1`; Step 10 passes the authored default-supercell counts.
- Sources:
  - particle/particle-set/reference-site/transition/cluster counts: the immutable model;
  - P1 and stability counts: `IStructurePreviewService`;
  - pair-interaction, cluster-permutation, and memory estimates: `IEnergyPreviewService` and
    `EnergyCostPreview`/`ClusterCostPreview`;
  - rule count: the sum of `TransitionPreview.ExpectedRuleCount` over the authored kinetic and Metropolis
    transitions. This is **before** conservation filtering and deduplication; Step 08 remains authoritative.
- If a required preview fails, `Statistics` is `null` and its diagnostics are appended to the result. The
  pipeline does not throw.
- Statistics are not persisted in the project and are not part of the model hash.

### 7. CLI contract

```text
mocassin validate <project.mocprj> [--json] [--warnings-as-errors] [--stats]
```

- Loads the project read-only through `ProjectStore`; it never writes, locks for edit, migrates in place, or
  mutates the model. `ProjectStoreOptions.ValidateOnLoad` is set to `false` for this path so the pipeline runs
  the model/domain stage exactly once; existing store callers keep the default `true`.
- Exit codes:
  - `0` — valid under the selected policy (warnings allowed unless `--warnings-as-errors`);
  - `1` — validation/model errors, or warnings promoted by `--warnings-as-errors`/`RequireBuildable`;
  - `2` — usage/argument error;
  - `3` — file not found, unreadable, locked for editing, or other I/O failure.
- `--json` prints one JSON report to stdout; human progress and errors go to stderr. The report is the
  `validation-v1` contract in [`02-data-contracts.md`](../02-data-contracts.md) §5.6.
- Human output is a stable table: severity, code, message, object/property, hint. The summary follows the table.
- `--stats` computes and prints/emits statistics. Without it, the JSON report has `"statistics": null` and the
  human output omits the table.
- The command uses the same `ValidationResult` as the GUI. `valid` in the JSON report is the policy-adjusted
  result; `summary` keeps raw counts.
- Step 07 adds a minimal hand-written command parser for `validate`. Step 12 extends the same command surface
  and may replace the parser behind it if the full CLI needs a framework. No new CLI dependency is required in
  Step 07.

### 8. GUI contract (steps 24–25)

- The GUI calls `ProjectValidationPipeline.Validate` with a cancellation token and a debounce policy; it does
  not call domain validators directly.
- With no errors, the status bar shows the raw warning/info counts and a "model valid" indicator.
- With errors, the validator panel lists `Diagnostic.Code`, `Severity`, `Message`, `Path.Domain`,
  `Path.ObjectId`, and `Path.Property`.
- Double-click navigates via `ModelPath` to the owning editor and property. `Related` paths are secondary
  navigation targets.
- "Revalidate" runs the pipeline with cancellation and a timestamp. It never mutates the project.
- Statistics are shown in the model-overview panel only when `--stats`/the overview option is enabled; clicking a
  count navigates to the relevant editor.
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
- Cancellation is checked between stages, per transition in the cross-domain stage, and per transition/cluster
  in the statistics service. A cancelled run returns no partial result and exits non-zero without writing.
- No allocation-heavy reflection over diagnostics in the hot path; the catalog is static.

### 10. Determinism and no mutation

- `DiagnosticBag` ordering is the contract: severity (Error, Warning, Info), code, domain, object ID, property,
  message. Step 07 reuses it and adds deterministic deduplication.
- Validation never mutates the project. The pipeline takes the immutable `Project` and returns a new result.
  A test compares the model and document hashes before/after validation.
- Same project, options, catalog, and contract versions produce the same `ValidationResult` diagnostics and
  statistics (timings excepted).

### 11. Deferred scope

- Build-stage checks (parameterization, job templates, supercells, package limits) are Steps 09/10/11/12.
- GUI panels, navigation, and editor integration are Steps 24–25.
- Compiled rule generation and rule ambiguity are Step 08.
- Localization is post-1.0; messages are English and keyed by code.
- A persistent diagnostic result cache is not introduced; preview caches are enough.

## Implementation tasks

- [ ] Implement `ProjectValidationPipeline`, `ValidationOptions`, `ValidationResult`, and stage results.
- [ ] Add `ProjectStoreOptions.ValidateOnLoad` and use the parse-only load path from `validate`.
- [ ] Refactor/keep `ProjectValidationService` as the stage-2 composition.
- [ ] Implement the load/model/cross-domain/feasibility stage order and skipped-stage behavior.
- [ ] Implement `ENM018` and `SIM038`–`SIM042` with the documented severities.
- [ ] Implement `DiagnosticDescriptor`, `CoreDiagnosticCatalog`, `SymmetryDiagnosticCatalog`, and the composed
      `DiagnosticCatalog`.
- [ ] Add catalog completeness, duplicate-code, and documentation-drift tests.
- [ ] Implement `ModelStatistics` and `ModelStatisticsService` with the preview services.
- [ ] Implement `mocassin validate` with human/JSON output, exit codes, and `--stats`.
- [ ] Add the `validation-v1` contract and generated schema/check.
- [ ] Add diagnostics reference and troubleshooting pages.
- [ ] Add the ceria good/bad validation fixtures and the per-code emission matrix.
- [ ] Add performance tests for `validate` and `validate --stats`.
- [ ] Add the GUI contract tests for `ModelPath` navigation coverage.

## Acceptance criteria

- [ ] The ceria sample validates with zero errors and a documented warning set.
- [ ] Every code emitted by the pipeline and the stage-2 validators has a catalog entry and a test that triggers
      it with the expected severity, path, and hint (existing Step 03–06 tests count).
- [ ] The catalog has no duplicate or undocumented codes.
- [ ] Validation never mutates the project; model and document hashes are unchanged.
- [ ] Diagnostics are deterministically sorted and deduplicated.
- [ ] `mocassin validate --json` validates against the `validation-v1` schema.
- [ ] Exit codes match the contract, including warnings-as-errors and I/O versus validation errors.
- [ ] `--stats` is opt-in, deterministic, and omitted on invalid models.
- [ ] Ceria validation completes in `< 200 ms`; ceria `--stats` completes in `< 2 s`.
- [ ] The GUI contract can navigate from every diagnostic `ModelPath` to an object/property (verified in
      Steps 24–25).
- [ ] Documentation written.

## Documentation

At this point, document validation and diagnostics:

- `docs/reference/diagnostics.md` — complete catalog, severities, owners, paths, hints; checked against
  `DiagnosticCatalog`.
- `docs/troubleshooting/errors.md` — common validation failures and fixes.
- `docs/reference/cli.md` — the `validate` command and `validation-v1` report; Step 30 completes the full CLI
  reference.
- `docs/development/testing-and-parity.md` — validation pipeline, catalog completeness, determinism, per-code
  fixtures.
- `docs/user-guide/` — inline validation/help behavior for the affected editors, added with Steps 24–25.

## Tests

| Test | Assertion |
| --- | --- |
| Pipeline order | Load → Model → CrossDomain → Feasibility; later stages skip on earlier errors |
| No mutation | Model and document hashes unchanged after validation |
| Catalog completeness | Every public code constant appears once; no duplicate or undocumented code |
| Catalog docs drift | `docs/reference/diagnostics.md` matches the catalog exactly |
| Per-code emission | Every emitted code has a real fixture/test with severity, path, and hint |
| Cross-domain | `ENM018` fires only for resolved paths outside the relevant cutoff; no cascade after `TRN020` |
| Feasibility | `SIM038`–`SIM042` trigger at the documented boundaries; `RequireBuildable` promotes the buildability set |
| Determinism | Same project/options yields identical ordered diagnostics and statistics |
| Deduplication | Exact duplicate `(code, path, message)` is emitted once |
| Statistics | Counts and estimates match hand-computed small models; `null` on invalid models |
| Statistics sources | P1/stability counts match `StructurePreview`; memory/permutations match `EnergyCostPreview` |
| CLI human output | Stable columns and summary for good/bad projects |
| CLI JSON | `validation-v1` schema, exit code, `valid`, `summary`, `--stats` nullability |
| CLI errors | Missing file, malformed JSON, lock, and usage errors map to 3/1/3/2 |
| Performance | Ceria `< 200 ms` without stats and `< 2 s` with stats |
| Cancellation | Cancelled validation returns no partial success and writes nothing |

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Diagnostic codes become unstable | Treat them as API; catalog completeness and docs-drift tests |
| Duplicate/conflicting messages | Deduplicate by code + path + message; aggregate, never re-emit stage-2 codes |
| Expensive feasibility/statistics | `--stats` opt-in; reuse preview caches; cancellation; hard budgets |
| Validation diverges from compilation | Share sampling/rule/preview code; Step 08 parity tests; mark estimates advisory |
| Stage 5 is invented too early | Keep `IValidationStage` as the only extension point; Steps 09/10/12 own their checks |
| `ENM018` cascades after path errors | Emit only for resolved paths; skip on existing path errors |
| CLI parser rework in Step 12 | Isolate parsing behind the command surface; Step 12 may replace the parser without changing `validate` semantics |

## Done when

A user can validate any project headlessly or from the GUI, see precise and navigable diagnostics from one
catalog, trust that validation does not mutate the model, and opt into accurate model statistics before a build.
