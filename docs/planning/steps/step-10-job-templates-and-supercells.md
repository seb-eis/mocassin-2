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
- **Last updated:** —

| Feature | Branch | Status | Local gates | Step CI | Squash commit |
| --- | --- | --- | --- | --- | --- |
| — | — | — | — | — | — |

## Goal

Turn one parameterized model into a reproducible set of simulation jobs and initial lattices. A user must be
able to define a parameter sweep, preview the effective settings and doping counts, and get identical
lattices for the same seed.

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

## Implementation tasks

- [ ] Implement job template/collection/config records and inheritance.
- [ ] Implement job expansion and deterministic job IDs/seeds.
- [ ] Implement supercell generation with charge compensation and truncation.
- [ ] Implement deterministic sampling and the doping report.
- [ ] Implement selection optimizers and their validation.
- [ ] Implement template validation and job-count preview.
- [ ] Implement `mocassin build --preview` (no package write).
- [ ] Add ceria temperature/doping sweep samples.
- [ ] Add tests for edge cases (0 %, 100 %, tiny cells, impossible compensation).

## Acceptance criteria

- [ ] The ceria KMC sample expands to the expected number of jobs with deterministic IDs.
- [ ] For a 10×10×10 ceria cell and 1 % Ce doping, the applied counts and vacancy compensation are correct.
- [ ] A requested fraction that cannot be realized is adjusted downward and reported.
- [ ] An impossible charge compensation fails with `JOB012`.
- [ ] Same job descriptor + seed produces an identical lattice across runs and OSes.
- [ ] Changing a simulation-only setting does not change the lattice for the same job.
- [ ] Optimizers remove only start options, not lattice particles.
- [ ] Documentation written

## Documentation

At this point, document job templates and supercells:

- `docs/user-guide/job-templates.md` — collections/configs, inheritance, duplicates, optimizers.
- `docs/user-guide/lattice.md` — doping preview and effective fractions.
- `docs/reference/project-format.md` — job-template JSON and seed derivation.
- `docs/development/compilation-pipeline.md` — supercell generation, truncation, charge compensation.

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
| Sampling | Uniform without replacement; deterministic |
| Optimizers | Start options removed, lattice unchanged |
| Edge cases | 0 %, 100 %, tiny cell, impossible ratio |
| Performance | 10⁴ small jobs expand in < 10 s |

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Charge-compensation rounding surprises users | Preview report; explicit requested vs. applied |
| Multiple dopings interact unpredictably | Validate source occupation at each step; preview sequence |
| Non-deterministic sampling | Sorted candidate lists + fixed RNG stream |
| Memory for huge lattices | Build jobs lazily; stream package writing (step 11) |
| Seed collisions/duplicates | SHA-256 domain separation and job indices |

## Done when

A job template can be expanded into deterministic job descriptors and valid, charge-neutral supercells, with
a report that explains every applied doping count.
