# Step 10 — Job Templates and Supercell Generation

**Phase:** C. Compilation  
**Depends on:** 06, 08, 09
**Delivers:** Job template collections/configurations, deterministic job expansion, and doping-aware supercell
generation with charge compensation.

## Implementation status

- **Status:** Not started
- **Step branch:** —
- **Step CI:** —
- **Main merge:** —
- **Step tag:** —
- **Audit:** Not requested
- **Design review:** Not requested
- **Last updated:** —

| Feature | Branch | Status | Local gates | Step CI | Squash commit |
| --- | --- | --- | --- | --- | --- |
| — | — | — | — | — | — |

## Goal

Turn one parameterized model into a reproducible set of simulation jobs and initial lattices. A user must be
able to define a parameter sweep, preview the effective settings and doping counts, and get identical
lattices for the same seed.

The model input is the Step 09 build-time `Mocassin.Packages.Parameterization.ParameterizedModel`
(`CompiledModel` + applied `ParameterizationValues` + parameterization signature), not project JSON. Job and
lattice payloads exclude parameterization values; the lattice request hash deliberately does not depend on
them, so changing energies or frequencies does not change a lattice for the same job coordinates.

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
    int JobCountPerConfig,
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
    SimulationKind Kind,
    bool UseDualDofCorrection,
    JobSettings Settings,
    LatticeRequest Lattice,
    ImmutableArray<DopingValue> Dopings,
    ImmutableArray<SelectionOptimizer> Optimizers,
    byte[] Seed);

// Serialized DTO lives in Mocassin.Contracts; produced by this step.
public sealed record InitialLattice(
    int SizeA, int SizeB, int SizeC,
    int PositionCount,
    byte[] Particles,
    string DopingInfo);
```

- Expand collections in order, configs in order, duplicates in ascending `JobIndex`.
- `JobId` is 1-based and deterministic; it is the solver's `--job` argument.
- Step 11 wraps a `JobDescriptor` plus its generated `InitialLattice` into a `JobPackage` for writing.
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
        IProjectQuery project,
        LatticeRequest request,
        ImmutableArray<DopingValue> dopings,
        out ImmutableArray<Diagnostic> diagnostics);

    JobCountPreview? TryCreateJobCountPreview(
        IProjectQuery project,
        JobTemplate template,
        SimulationId simulationId,
        int? jobCountPerConfiguration,
        out ImmutableArray<Diagnostic> diagnostics);
}
```

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
- The service lives in `Mocassin.Compilation.Jobs`; `Mocassin.Compilation` already references
  `Mocassin.Symmetry`, so P1 site multiplicity is available without a new dependency.
- Step 25 consumes this service. Step 06 owns only the model definitions and validation.

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
