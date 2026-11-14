# Step 10 — Job Templates and Supercell Generation

**Phase:** C. Compilation  
**Depends on:** 06, 08, 09
**Delivers:** Job template collections/configurations, deterministic job expansion, and doping-aware supercell
generation with charge compensation.

## Implementation status

- **Status:** Not started
- **Feature list:** Proposed 2026-11-14 (eight features; pending design review and owner approval)
- **Step branch:** —
- **Step CI:** —
- **Main merge:** —
- **Step tag:** —
- **Audit:** Not requested
- **Design review:** Ready for review at `c66b483f6ef064858d0e8082543d2fcec8386ed3` — [record](../reviews/step-10.md)
- **Last updated:** 2026-11-14

### Proposed feature list

| # | Feature | Branch | Status |
| --- | --- | --- | --- |
| 1 | Job template model and persistence | `feature/step-10-job-templates/job-template-model` | Proposed |
| 2 | Settings inheritance and validation | `feature/step-10-job-templates/settings-inheritance-and-validation` | Proposed |
| 3 | Job expansion and deterministic seeds | `feature/step-10-job-templates/job-expansion-and-seeds` | Proposed |
| 4 | Supercell generation | `feature/step-10-job-templates/supercell-generation` | Proposed |
| 5 | Doping and charge compensation | `feature/step-10-job-templates/doping-and-charge-compensation` | Proposed |
| 6 | Lattice preview service | `feature/step-10-job-templates/lattice-preview-service` | Proposed |
| 7 | Selection optimizers and build preview | `feature/step-10-job-templates/selection-optimizers-and-build-preview` | Proposed |
| 8 | Documentation, parity, and closeout | `feature/step-10-job-templates/documentation-parity-and-closeout` | Proposed |

The feature list is a design proposal. It is approved once by the human owner together with the design verdict; it does
not create the step branch and does not mark the step `In progress`.

## Goal

Turn one parameterized model into a reproducible set of simulation jobs and initial lattices. A user must be
able to define a parameter sweep, preview the effective settings and doping counts, and get identical
lattices for the same seed.

The build-time chain starts from the Step 09 `Mocassin.Packages.Parameterization.ParameterizedModel`
(`CompiledModel` + applied `ParameterizationValues` + parameterization signature), not project JSON. Job
expansion itself needs the project and the compiled model, not the parameterization values; lattice generation
is deliberately parameterization-independent. The lattice request hash therefore excludes parameterization
values, so changing energies or frequencies does not change a lattice for the same job coordinates.

## Original reference

- `ProjectJobSetTemplate` contains KMC and MMC job package descriptions.
- `JobPackageData`:
  - RNG seed, `JobCountPerConfig`, selection optimizers;
  - simulation reference;
  - base job description and per-config overrides.
- `JobConfigData` (all numeric fields stored as strings so empty = inherit):
  - execution flags, target MCSP, time limit, temperature, minimal success rate;
  - lattice configuration (size A/B/C + doping values);
  - instruction string.
- KMC config adds pre-run MCSP, electric field modulus, normalization energy, max attempt frequency.
- MMC config adds break tolerance/sample length/interval/result sample.
- `JobPackageDataProvider` seeds a `PcgRandom32` from the collection seed (or simulation seed), then for each
  config creates `jobCount` duplicates with 16 random bytes for RNG state/increment.
- Doping fractions are applied in priority order; each applies to the remaining particles.
- Integer truncation is adjusted downward until the dependent exchange can be expressed as an integer.
- The original caps a package at 99,999 jobs.
- Selection optimizers remove a particle at a reference site from the KMC start pool.

## Design

### 1. Job template model

```csharp
public sealed record JobTemplate(
    JobTemplateId Id,
    string Name,
    ImmutableArray<JobCollection> Collections);

public sealed record JobCollection(
    Guid Id,
    string Name,
    SimulationId SimulationId,
    JobSettings BaseSettings,
    int? JobCountPerConfig,
    string? RngSeed,
    ImmutableArray<SelectionOptimizer> Optimizers,
    ImmutableArray<JobConfig> Configs);

public sealed record JobConfig(
    Guid Id,
    string Name,
    JobSettings? Overrides,
    LatticeRequest Lattice,
    ImmutableArray<DopingValue> Dopings);

public sealed record LatticeRequest(int A, int B, int C);
public sealed record DopingValue(DopingId DopingId, double Fraction);
public sealed record SelectionOptimizer(CellSiteId SiteId, ParticleId ParticleId);
```

- `JobSettings` is a partial record with nullable fields; `null` means inherit.
- Inheritance order: simulation defaults → collection `BaseSettings` → config `Overrides` → duplicate job
  (seed only).
- `JobCountPerConfig` overrides the simulation's default job count.
- `RngSeed` is optional; if absent, derive from the project/job seed.
- Config names are unique within a collection; collection names unique within a template.

### 2. Job expansion

```csharp
public sealed record JobDescriptor(
    int JobId,
    int CollectionIndex,
    int ConfigIndex,
    int JobIndex,
    string CollectionName,
    string ConfigName,
    SimulationId SimulationId,
    int SimulationIndex,
    SimulationKind Kind,
    bool UseDualDofCorrection,
    ResolvedJobSettings Settings,
    LatticeRequest Lattice,
    ImmutableArray<DopingValue> Dopings,
    ImmutableArray<SelectionOptimizer> Optimizers,
    byte[] Seed);

// Build-time generated lattice in Mocassin.Packages.Jobs.
public sealed record GeneratedLattice(
    int SizeA, int SizeB, int SizeC,
    int PositionCount,
    byte[] Particles,
    DopingReport Report,
    string DopingInfo);
```

- Expand collections in order, configs in order, duplicates in ascending `JobIndex`.
- `JobId` is 1-based and deterministic; it is the solver's `--job` argument.
- Step 11 wraps a `JobDescriptor` plus its `GeneratedLattice` into a `JobPackage` and maps the lattice to the
  `Mocassin.Contracts` serialized `InitialLattice` DTO.
- `JobIndex` is the duplicate index within a config (0-based).
- `UseDualDofCorrection` is set by the compiler/optimizer stage for KMC jobs; it is not a user setting.
  Step 08 keeps the original default (every KMC job true); Step 10 clears it only when the selection optimizer
  reduces **every** environment selection mask, and keeps it when any mask is unchanged (including the mixed
  case), matching the original `JumpSelectionOptimizer.Run` accumulation. `Mocassin.Packages`, which references
  Contracts, composes the `.msl` `SolverJobFlags` word from `Kind`,
  the authored execution flags, `PreRunMcsp > 0`, and this boolean. `Mocassin.Compilation` does not reference
  Contracts.
- Effective settings are resolved with explicit inheritance and validated.
- Seed derivation:

```text
seed = SHA-256(
  "mocassin/job/v1" ||
  master_seed ||
  "job" ||
  collection_index_u32 ||
  config_index_u32 ||
  job_index_u32)
```

- `master_seed` is the collection seed if provided, else the simulation seed, else the project
  `masterSeed`. If absent, derive it from `SHA-256("mocassin/project-seed/v1" || projectId)` and store it.
- `lattice_request_hash` is a SHA-256 hash of the canonical **structure/particle/doping subset** (not the
  full model signature, which also covers transitions/encodings), the space-group catalog data version,
  supercell `A/B/C`, and ordered doping fractions. It deliberately excludes transitions, parameterization values, and simulation settings, so
  changing energies, temperature, or MCSP does not change the lattice for the same job coordinates.
- Lattice generation uses a separate domain-separated seed:
  `SHA-256("mocassin/lattice/v1" || seed || lattice_request_hash)`.
- The same job descriptor always produces the same lattice.

### 3. Supercell generation

Inputs: base structure, particle sets, occupation exchanges/dopings, lattice size, doping fractions, seed.

Algorithm:

1. Build the base lattice:
   - replicate the unit cell `A × B × C`;
   - stable site default = first particle of its set;
   - unstable site default = void.
2. For each doping in ascending priority:
   a. Resolve the primary exchange: site, source particle, target particle.
   b. If the source particle no longer exists at any position of that site, the doping contributes 0 and a
      warning is recorded.
   c. `available = count of source particles on the target site`.
   d. `requested = floor(fraction × available)`.
   e. If charge compensation is active:
      - compute charge deltas `Δq_p` and `Δq_d`;
      - required ratio `n_d / n_p = −Δq_p / Δq_d`;
      - reduce to the smallest integer pair `(p, d)`;
      - decrement `n_p` until `n_p` is a multiple of `p` and `n_d = n_p × d / p` fits the dependent
        exchange's available positions;
      - if no feasible `n_p > 0` exists, the doping contributes 0 and a warning is recorded.
   f. Sample `n_p` positions uniformly without replacement from the available source positions and replace
      the source with the target.
   g. If compensation is active, sample `n_d` positions from the dependent source positions and replace.
   h. Record requested vs. applied counts.
3. Validate the final lattice:
   - total charge is neutral within tolerance (or the configured tolerance policy);
   - no particle is outside its site's set;
   - unstable sites are void unless a transition is active (they are always void in the initial state);
   - doping counts match the report.
4. Return the lattice plus a `DopingReport`.

- Sampling uses a deterministic Fisher–Yates shuffle or reservoir sampling over the candidate list. The
  candidate list is sorted by linear position index before sampling, so the result is independent of hash
  iteration order.
- If a doping targets the same site as a higher-priority doping, the source particle must be the result of the
  earlier doping; otherwise the doping contributes 0 with a diagnostic.
- The algorithm does not place dopants in "zones" or gradients; that limitation is documented.
- Supercell limits: A/B/C 1–256; total positions ≤ 500,000 (warning above 100,000).

### 4. Doping report

```csharp
public sealed record DopingReport(
    ImmutableArray<DopingApplication> Applications,
    int TotalPositions,
    double TotalChargeBefore,
    double TotalChargeAfter,
    bool IsChargeNeutral);

public sealed record DopingApplication(
    DopingId DopingId,
    int RequestedPrimary,
    int AppliedPrimary,
    int AppliedDependent,
    double EffectiveFraction);
```

- The report is stored in the job metadata (`DopingInfo`) and shown in the GUI.
- The build fails if charge neutrality cannot be achieved and compensation was requested.
- The build warns if the effective fraction differs from the requested fraction.

### 4.1 Lattice and job-count preview

The GUI and `build --preview` use one preview service over the same generator. There is no second doping,
truncation, or compensation implementation.

```csharp
public sealed record ParticlePopulation(ParticleId ParticleId, int Count);
public sealed record SitePopulation(CellSiteId SiteId, ImmutableArray<ParticlePopulation> Particles);

public sealed record LatticePreview(
    LatticeRequest Request,
    ImmutableArray<DopingValue> Dopings,
    DopingReport Report,
    ImmutableArray<SitePopulation> RemainingPopulations);

public sealed record JobCountPreview(
    SimulationId SimulationId,
    int DefaultJobCount,
    int EffectiveJobCount,
    long TotalJobs);

public interface ILatticePreviewService
{
    LatticePreview? TryCreatePreview(
        Project project,
        CompiledModel model,
        LatticeRequest request,
        ImmutableArray<DopingValue> dopings,
        out ImmutableArray<Diagnostic> diagnostics);

    JobCountPreview? TryCreateJobCountPreview(
        Project project,
        JobTemplate template,
        SimulationId simulationId,
        int? jobCountPerConfiguration,
        out ImmutableArray<Diagnostic> diagnostics);
}
```

- The service takes the current `Project` and its `CompiledModel`: the project supplies the authored model/hash,
  the compiled model supplies the P1 positions, sites, particle indices, and selection masks. A preview never
  triggers a compilation.
- The service and the generator live in `Mocassin.Packages.Jobs`, not `Mocassin.Compilation`. The architecture
  assigns job templates, supercells, and parameterization-dependent build data to `Packages`; `Compilation`
  stays a pure model compiler with no job-template or supercell knowledge.

- `TryCreatePreview` echoes the requested `(A, B, C)` and doping list, so a preview cannot be confused with a
  different supercell or fraction set.
- `Report` is the same `DopingReport` returned by generation: requested vs. applied primary counts, dependent
  counts, effective fraction, total positions, total charge before/after, and the neutrality flag.
- `RemainingPopulations` gives the per-site remaining particle counts required by the Step 25 editor.
- Unknown dopings, fractions outside `[0,1]`, invalid `(A, B, C)`, and impossible compensation return `null`
  plus diagnostics. The service never throws for invalid user data; a null project or non-positive `(A, B, C)`
  is a programmer error, matching the Step 03–05 preview boundary.
- `TryCreateJobCountPreview` resolves the simulation's default job count, the template/collection override, and
  the total jobs across the template's collections/configs. It does not generate lattices.
- The preview uses the same `SupercellGenerator`/doping application path as the build. A test generates the
  same request and asserts that the preview report and the lattice's `DopingReport` are identical.
- The preview is cached by `(project model hash, lattice request hash, canonical doping list, catalog data
  version, space-group data SHA-256, symmetry contract version)`, is bounded, and is thread-safe.
- `Mocassin.Packages` already references `Core`, `Symmetry`, `Compilation`, and `Contracts`, so the service can
  use `ProjectHasher`, `CompiledModel`, and the contract DTOs without reversing a dependency.
- Step 25 consumes this service with the current project and compiled model. Step 06 owns only the model
  definitions and validation.

### 5. Selection optimizers

- An optimizer removes a `(site, particle)` start option from the KMC selection pool.
- It does not remove the particle from the lattice; it only prevents it from being selected as a transition
  start.
- Optimizers are applied after compilation and before job encoding.
- Validation:
  - site exists and is stable;
  - particle exists and can occupy the site;
  - the optimizer does not remove all start options for a required mechanism (warning/error).
- The build report shows how many start options each optimizer removed.

### 6. Job template validation

| Code | Severity | Rule |
| --- | --- | --- |
| `JOB001` | Error | Template has no collections |
| `JOB002` | Error | Collection references unknown simulation |
| `JOB003` | Error | Collection has no configs |
| `JOB004` | Error | Lattice size outside 1–256 or total positions > 500,000 |
| `JOB005` | Error | Doping fraction outside [0,1] |
| `JOB006` | Error | Doping reference unknown |
| `JOB007` | Error | Duplicate collection/config name |
| `JOB008` | Error | Job count outside 1–1000 |
| `JOB009` | Error | Optimizer reference invalid |
| `JOB010` | Warning | Requested doping cannot be realized; 0 applied |
| `JOB011` | Warning | Effective doping fraction differs from requested |
| `JOB012` | Error | Charge neutrality cannot be achieved |
| `JOB013` | Warning | Total jobs exceed 10,000 |
| `JOB014` | Warning | Supercell positions exceed 100,000 |
| `JOB015` | Error | Inherited settings missing a required value |

Build-stage integration: job-template/supercell checks plug into the Step 07 pipeline as an `IValidationStage`
with `Stage = Build`, run only when `ValidationOptions.RunBuildStage` is true and earlier stages produced no
errors, and register their `JOB` codes in the Step 07 `DiagnosticCatalog` at that point. Step 07 does not stub
`JOB` codes.

### 7. Build-stage validation and diagnostics

`ValidationContext` gains an optional `CompiledModel? CompiledModel` property (additive; defaults to `null`).
The Step 10 `JobTemplateValidationStage` is the single `Build`-stage owner for `JOB001`–`JOB015`; it runs only
when `RunBuildStage` is true, the context carries a compiled model, and earlier stages have no error.

- `validate` keeps `RunBuildStage = false` and reports stages 1–4. `build` and `mocassin build --preview`
  supply the compiled model and run the job-template stage.
- Structural checks (collections/configs/names/references/fractions/cell ranges/job counts/optimizer
  references) run in the same stage, so there is one diagnostic path and the preview service uses the same
  validator. Only the total-position limit and the doping/compensation checks need the compiled model.
- `JOB004` covers both cell size outside 1–256 and total positions above 500,000; `JOB009` also covers an
  optimizer that removes every usable start option for a required mechanism; `JOB015` also covers a settings
  override that is not applicable to the collection's simulation kind.
- Step 07's `ValidationContext` snippet is updated in this design to carry the optional model; the pipeline
  remains the only Build-stage extension point.

### 8. Job settings, inheritance, and persistence

`JobSettings` is a partial record with nullable fields. `null` means inherit; an explicit
`ExecutionFlags.None` or zero value overrides the inherited value.

```csharp
public sealed record JobSettings
{
    public double? TemperatureK { get; init; }
    public long? TargetMcsp { get; init; }
    public int? WriteOutCount { get; init; }
    public long? TimeLimitSeconds { get; init; }
    public double? MinSuccessRate { get; init; }
    public ExecutionFlags? Flags { get; init; }
    public string? Instruction { get; init; }

    public long? PreRunMcsp { get; init; }
    public double? ElectricFieldModulusVm { get; init; }
    public Fractional3? ElectricFieldDirection { get; init; }
    public double? NormalizationEnergyEv { get; init; }
    public double? MaxAttemptFrequencyHz { get; init; }

    public double? BreakTolerance { get; init; }
    public long? BreakSampleLengthMcsp { get; init; }
    public long? BreakSampleIntervalMcsp { get; init; }
    public long? ResultSampleMcsp { get; init; }

    public ImmutableArray<JobExtension>? Extensions { get; init; }
}

public sealed record JobExtension(string Id, JsonElement Config);
```

- Effective settings are resolved from the compiled `CompiledRuntimeSettings` (simulation defaults), then the
  collection `BaseSettings`, then the config `Overrides`. A duplicate job changes only its seed, never its
  settings. `JobCollection.JobCountPerConfig` is `int?`; `null` inherits `Simulation.JobCount`.
- KMC-only overrides on an MMC collection (or vice versa) are `JOB015` errors. The effective values are
  validated against the Step 06 ranges before expansion.
- `Extensions` is an optional ordered array of `{id, config}` objects. Step 10 validates only a non-empty
  unique `id` and an object-valued `config`; it preserves the config as canonical JSON (sorted property names,
  normalized numbers) so project hashes are deterministic. The concrete extension schema and semantic
  validation are owned by Steps 27/28; Step 10 does not invent an MMCFE schema.
- `JobTemplate`, `JobCollection`, `JobConfig`, `JobSettings`, `LatticeRequest`, `DopingValue`, and
  `SelectionOptimizer` live in `Mocassin.Core.Model.Jobs`. `ProjectJson`/`ProjectHasher` gain a canonical
  template converter: templates sorted by `JobTemplateId`, collections and configs in authored order, dopings
  in ascending resolved doping priority with ties rejected, optimizer arrays in authored order, and
  extensions sorted by `Id`. Empty collections/configs/dopings arrays are omitted on write.
- The project format remains `schemaVersion = 1` (additive pre-release change, same exception as Steps 03–09).
  `contracts/manifest.json`'s `jobTemplates.itemFields`, the generated project schema, and the project
  fixtures are updated together; no package/results format changes in this step.

### 9. Build-time types and step interfaces

- Build-time `JobDescriptor`, `ResolvedJobSettings`, `GeneratedLattice`, `SupercellGenerator`,
  `JobTemplateExpander`, and `ILatticePreviewService` live in `Mocassin.Packages.Jobs`.
- `JobDescriptor` additionally carries `SimulationId` and the compiled `SimulationIndex`; `Settings` is the
  resolved, non-nullable effective settings. `UseDualDofCorrection` is copied from
  `CompiledSimulation.Intent` and may be cleared only by the documented optimizer rule.
- `GeneratedLattice` carries the particle byte array, its `DopingReport`, and the canonical `DopingInfo`
  summary. The `Mocassin.Contracts` serialized `InitialLattice` DTO is produced by Step 11 from
  `GeneratedLattice`; it is not a Compilation type. Step 11 remains the only package-payload mapper.
- `Mocassin.Compilation` is unchanged: it never sees a job template, doping fraction, optimizer, or lattice
  request. This keeps the architecture's `Packages -> Compilation` dependency direction intact.

### 10. Lattice layout and doping summary

- A supercell is `A × B × C` compiled P1 cells. The flat particle array uses row-major cell order:
  `index = ((a * B + b) * C + c) * PositionCount + P`, with `a` outermost and `P` in canonical P1 order.
  `PositionCount` is the compiled `Structure.Positions.Length` per unit cell. The same layout is used by the
  preview, the `.msl` lattice DTO, and the Step 13 reader.
- Stable sites default to the first particle of their authored particle set; unstable sites default to void
  index 0. Doping replaces particles only within the referenced exchange's site and particle set.
- `DopingInfo` is the invariant-culture, priority-ordered summary
  `"{priority}@{effectiveFraction:R}"` joined by `;`; no applied doping yields the empty string. It is
  display/metadata only; the authoritative counts are in `DopingReport` and the package lattice signature.
- The applied count is `floor(fraction × available)`; the charge-compensation algorithm may reduce it to the
  smallest realizable integer pair. Requested versus applied counts are always both reported.

### 11. `mocassin build --preview` boundary

Step 10 adds only the preview path: load → validate → compile → parameterize → expand → preview. It never
opens, writes, or replaces a package and never invokes the Step 11 writer. Step 12 owns the full `build`
command, package writing, `--jobs`, `--parallel`, `--force`, partial builds, and progress events; it reuses the
same expansion and preview services. If Step 12 has not landed, the preview command is still useful and cannot
produce a half-written `.msl`.

### 12. Feature sizing

The roadmap estimate is **3–4 person-weeks**, within the four-week split trigger. The eight features above are
the complete implementation list; no separate 10A/10B split is planned. The design review may merge adjacent
features (for example 4+5 or 6+7) if it judges the feature count too high; any merge does not change the scope
or acceptance criteria.

## Implementation tasks

- [ ] Implement job template/collection/config records and inheritance.
- [ ] Implement job expansion and deterministic job IDs/seeds.
- [ ] Implement supercell generation with charge compensation and truncation.
- [ ] Implement deterministic sampling and the doping report.
- [ ] Implement `ILatticePreviewService`, `LatticePreview`, and `JobCountPreview` over the same generator as the
  build; cover the try/failure boundary, request echo, per-site populations, cache keys, and preview/build
  report equality.
- [ ] Implement selection optimizers and their validation.
- [ ] Implement template validation and the job-count preview.
- [ ] Implement `mocassin build --preview` (no package write).
- [ ] Add ceria temperature/doping sweep samples.
- [ ] Add tests for edge cases (0 %, 100 %, tiny cells, impossible compensation).

## Acceptance criteria

- [ ] The ceria KMC sample expands to the expected number of jobs with deterministic IDs.
- [ ] For a 10×10×10 ceria cell and 1 % Ce doping, the applied counts and vacancy compensation are correct.
- [ ] A requested fraction that cannot be realized is adjusted downward and reported.
- [ ] An impossible charge compensation fails with `JOB012`.
- [ ] The lattice preview echoes the request, reports requested/applied counts, per-site remaining populations,
  total charge before/after, and neutrality, and matches the generated lattice's report for the same request.
- [ ] The preview returns `null` plus diagnostics for an unknown doping, an out-of-range fraction, an invalid
  supercell, or impossible compensation; it never returns a partial preview.
- [ ] The job-count preview reports the default, effective, and total job counts without generating lattices.
- [ ] Same job descriptor + seed produces an identical lattice across runs and OSes.
- [ ] Changing a simulation-only setting does not change the lattice for the same job.
- [ ] Optimizers remove only start options, not lattice particles.
- [ ] Documentation written

## Documentation

At this point, document job templates and supercells:

- `docs/user-guide/job-templates.md` — collections/configs, inheritance, duplicates, optimizers.
- `docs/user-guide/lattice.md` — update the page created in Step 06 with doping preview, effective fractions,
  truncation, and charge compensation.
- `docs/reference/project-format.md` — job-template JSON and seed derivation.
- `docs/development/compilation-pipeline.md` — supercell generation, truncation, charge compensation, and the
  shared preview service.

## Tests

| Test | Assertion |
| --- | --- |
| Sampling property | Random valid requests never exceed available candidates and stay deterministic per seed |
| Doping property | Random dopings keep applied counts within requested bounds and preserve charge neutrality or fail |
| Job acceptance | Job descriptors and lattices expand, write, and read back through the real writer/reader |
| Expansion | Job counts/IDs/names match template order |
| Inheritance | Base/config/job overrides resolve correctly |
| Seeds | Deterministic and domain-separated |
| Supercell | Default occupations, replication order |
| Doping | Counts, truncation, order, multiple dopings |
| Charge | Compensation ratios and neutrality |
| Lattice preview | Request/doping echo, applied counts, per-site populations, total charge, neutrality |
| Preview failure | Unknown doping, fraction outside [0,1], invalid supercell, impossible compensation -> null + diagnostics; no partial preview |
| Preview/build equality | The preview report equals the generated lattice's `DopingReport` for the same request |
| Preview cache | Keys separate project model, request, doping list, and catalog/contract versions |
| Job-count preview | Default/effective/total counts, template overrides, no lattice generation |
| Sampling | Uniform without replacement; deterministic |
| Optimizers | Start options removed, lattice unchanged |
| Edge cases | 0 %, 100 %, tiny cell, impossible ratio |
| Performance | 10⁴ small jobs expand in < 10 s |

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Charge-compensation rounding surprises users | Preview report; explicit requested vs. applied |
| Multiple dopings interact unpredictably | Validate source occupation at each step; preview sequence |
| Preview and generated lattice diverge | One generator; preview/build equality test |
| Preview cache returns stale data | Cache key includes the project model hash, request, doping list, and catalog/contract versions |
| Non-deterministic sampling | Sorted candidate lists + fixed RNG stream |
| Memory for huge lattices | Build jobs lazily; stream package writing (step 11) |
| Seed collisions/duplicates | SHA-256 domain separation and job indices |

## Done when

A job template can be expanded into deterministic job descriptors and valid, charge-neutral supercells, with
a report that explains every applied doping count.
