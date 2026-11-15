# Step 10 — Job Templates and Supercell Generation

**Phase:** C. Compilation  
**Depends on:** 06, 08, 09
**Delivers:** Job template collections/configurations, deterministic job expansion, and doping-aware supercell
generation with charge compensation.

## Implementation status

- **Status:** In progress
- **Feature list:** Approved 2026-11-14 (eight features; step branch created)
- **Step branch:** `step/10-job-templates`
- **Step CI:** —
- **Main merge:** —
- **Step tag:** —
- **Audit:** Not requested
- **Design review:** Ready for implementation at `c5454af050ccd3f1e02f892424ccd714c5d56f89` (round 5) — [record](../reviews/step-10.md); accepted by the human owner 2026-11-14
- **Last updated:** 2026-11-15

### Proposed feature list

| # | Feature | Branch | Status |
| --- | --- | --- | --- |
| 1 | Job template model and persistence | `feature/step-10-job-templates/job-template-model` | Done |
| 2 | Settings inheritance and validation | `feature/step-10-job-templates/settings-inheritance-and-validation` | In progress |
| 3 | Job expansion and deterministic seeds | `feature/step-10-job-templates/job-expansion-and-seeds` | Proposed |
| 4 | Supercell generation | `feature/step-10-job-templates/supercell-generation` | Proposed |
| 5 | Doping and charge compensation | `feature/step-10-job-templates/doping-and-charge-compensation` | Proposed |
| 6 | Lattice preview service | `feature/step-10-job-templates/lattice-preview-service` | Proposed |
| 7 | Selection optimizers and build preview | `feature/step-10-job-templates/selection-optimizers-and-build-preview` | Proposed |
| 8 | Documentation, parity, and closeout | `feature/step-10-job-templates/documentation-parity-and-closeout` | Proposed |

The feature list is a design proposal. It is approved once by the human owner together with the design verdict; it does
not create the step branch and does not mark the step `In progress`.

### Feature log

| Feature | Branch | Status | Local gates | Step CI | Squash commit |
| --- | --- | --- | --- | --- | --- |
| `job-template-model` | `feature/step-10-job-templates/job-template-model` | Done | `dotnet`, `structure`, `api`, `contracts`, `docs` — reported green by the feature-1 agent | — | `c8d5269` |
| `settings-inheritance-and-validation` | `feature/step-10-job-templates/settings-inheritance-and-validation` | In progress | See outcome below | — | — |

Feature 1 was squash-merged as `c8d5269` (`step-10: add job template model and persistence`). It added the typed
job-template records, project JSON/manifest/schema persistence, canonical extension ordering, and the
project-job-templates fixture. No per-feature CI run was triggered.

#### Feature 2 outcome and deviations

`settings-inheritance-and-validation` adds the Core structural `JOB001`–`JOB020` validator, the nullable
`JobSettingsValidator`, the Packages `ResolvedJobSettings`/`JobSettingsResolver`, and the optional
`ValidationContext.CompiledModel`. Structural job errors now appear in `validate` without a compiled model.

Local gates:

- `./tools/verify.sh dotnet` — exit 0; build 0 warnings/0 errors; Core 677, Symmetry 223, Compilation 96,
  Packages 226, Cli 7, App 1, Contracts 15, Results 1, Architecture 1 tests passed; `dotnet format` clean.
- `./tools/verify.sh structure` — exit 0; architecture structure test passed.
- `./tools/verify.sh api` — exit 0; licence policy 51 packages, no vulnerable packages, ApiCompat compatible
  (the `Mocassin.Core` and `Mocassin.Packages` baselines were refreshed).
- `./tools/verify.sh contracts` — exit 0; 18 generated artifacts current, 5 model and 4 parameterization
  projections OK, 274 space-group settings current, process consistency OK, 31 tool tests passed, SPDX 488
  files, no secrets, cargo-deny OK.
- `./tools/verify.sh docs` — exit 0; links OK, Zensical strict build clean, DocFX 0 warnings/0 errors,
  rustdoc generated, site built.

Recorded deviations and limitations:

- `JOB004` currently checks only the `1..256` cell-size range. The compiled-bound total-position cap remains in
  the Step 10 build stage, as designed.
- `JOB015` validates each explicit override layer and its kind applicability. A structurally incomplete
  inherited value is reported by the Packages resolver when it becomes effective; no compiled model exists in
  `validate`.
- A non-neutral base lattice remains a reported model property and is not diagnosed by this structural layer.
  The `JOB012`/`JOB016` doping outcomes remain in later features.
- The existing job-template persistence fixture is intentionally reference-only; the envelope round-trip tests
  exclude `JOB` diagnostics for it, while the dedicated `JobTemplateValidatorTests` cover the structural codes.
- No per-feature CI run was triggered.

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
    JobCollectionId Id,
    string Name,
    SimulationId SimulationId,
    JobSettings BaseSettings,
    int? JobCountPerConfig,
    string? RngSeed,
    ImmutableArray<SelectionOptimizer> Optimizers,
    ImmutableArray<JobConfig> Configs);

public sealed record JobConfig(
    JobConfigId Id,
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
    ImmutableArray<byte> Seed);

// Build-time generated lattice in Mocassin.Packages.Jobs.
public sealed record GeneratedLattice(
    int SizeA, int SizeB, int SizeC,
    int PositionCount,
    ImmutableArray<byte> Particles,
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
- **Master seed bytes.** Resolve `seedText` as the first non-empty of: collection `RngSeed`, simulation
  `RngSeed`, project `masterSeed`. The project seed is required and is 32 bytes of lowercase hexadecimal. Any
  seed text that is exactly 64 lowercase hex characters is decoded; every other explicit seed text is UTF-8
  hashed with `SHA-256("mocassin/seed-text/v1" || UTF8(seedText))`. The rule never depends on whether another
  seed changed. The build never creates or persists a seed. A custom seed `ceria-seed-1` with
  collection/config/job all zero yields job seed
  `b6d2d1614b2b819445764523ef5613a47b7712a218e303d1a3d2cdb4f71a5c2e`; that is the second seed golden
  vector.
- **Job seed.** This follows ADR 0005's tuple order and is the normative cross-language contract:

```text
job_seed = SHA-256(
  master_seed[32] ||
  u32_le(collection_index) ||
  u32_le(config_index) ||
  u32_le(job_index) ||
  UTF8("mocassin/job/v1"))
```

  The fields are fixed-width except the terminal purpose string, so no length framing is needed. Step 13's
  Rust helper hashes the same byte order; its parameter order is only a Rust API convenience.
- **Job-seed golden vector.** With `master_seed = 000102…1e1f`, collection/config/job all zero, the expected
  seed is `884a9517b16620af2257945215f5552c1f5af0580ed2c911456b1a29a4f54ce7`. It is a mandatory
  cross-language/golden test vector.
- **Lattice request hash.** The projection is deliberately parameterization-independent and uses only:

```text
structure_hash = ProjectHasher.ComputeStructure(project.Model.Structure)
particle_hash  = SHA-256(canonical JSON of particles sorted by id and particleSets sorted by id)
lattice_hash   = SHA-256(canonical JSON of occupationExchanges sorted by id and dopings sorted by priority, then id)

lattice_request_hash = SHA-256(
  "mocassin/lattice-request/v1" ||
  structure_hash[32] ||
  particle_hash[32] ||
  lattice_hash[32] ||
  u32_le(A) || u32_le(B) || u32_le(C) ||
  u32_le(doping_count) ||
  for each request doping in ascending resolved priority, then DopingId:
      doping_id[16] (RFC 4122 big-endian) ||
      f64_le(fraction) ||
  u32_le(UTF8_length(space_group_data_version)) || UTF8(space_group_data_version) ||
  space_group_data_sha256[32] ||
  u32_le(UTF8_length(symmetry_contract_version)) || UTF8(symmetry_contract_version))
```

  Every `[32]` input is a SHA-256 digest decoded from its canonical 64-character lowercase hex form, including
  `structure_hash`, `particle_hash`, `lattice_hash`, and `SpaceGroupDataSha256`; the two version strings are
  UTF-8 with a `u32_le` byte-length prefix. `ProjectHasher.ComputeParticles` and
  `ProjectHasher.ComputeLattice` are Step 10 additions; they canonicalize the particle/particle-set and
  lattice/doping definitions exactly once. The hash excludes transitions, parameterization values,
  temperatures, and other runtime settings. A synthetic request-hash vector fixes the projection:
  structure/particle/lattice hashes `00..1f`, `20..3f`, `40..5f`; `A=B=C=10`; one doping
  `00112233-4455-6677-8899-aabbccddeeff` at `0.01`; space-group data version `2026.1` with digest `60..7f`;
  symmetry contract `symmetry-1`. The expected request hash is
  `5719de5efea8b57ef6960229bf2248de0da3bb497c2f29507d1742157f878295`, and with the custom job seed above
  the lattice seed is `a497141d26d005eb5caa17025b3c1e3564f31f8d46518a7a36d311623be464d2`.
- **Lattice seed and RNG.** `lattice_seed = SHA-256("mocassin/lattice/v1" || job_seed[32] || lattice_request_hash[32])`.
  The build-side sampler uses a deterministic SHA-256 counter DRBG:

```text
block_i = SHA-256("mocassin/lattice-rng/v1" || lattice_seed[32] || u64_le(counter++))
next_u64 = little_endian_u64(block_i[0..8])
```

  `NextBounded(upper)` uses Lemire's 64-bit multiply-and-reject method with a 128-bit intermediate; the
  implementation is normative and has no platform-dependent path. Sampling without replacement is a
  Fisher–Yates shuffle over the candidate list sorted by ascending linear position: for `i = 0..k-1`,
  `j = i + NextBounded(n - i)`, then swap `i` and `j`. Reservoir sampling is not used. This is a build-side
  stream only; ADR 0005's ChaCha12 solver stream is unchanged, and no new NuGet dependency is introduced.
- **Lattice golden vectors.** With the job seed above and `lattice_request_hash` set to 32 zero bytes, the
  expected lattice seed is `7f601350f114b61ba560fa983218cfd41e41df83953072ff415eafa21adbf394`.
  For a ten-element candidate list and `k = 3`, the expected selected indices are `[1, 7, 2]`. These vectors
  freeze the sampler contract.
- The same job descriptor always produces the same lattice.

### 3. Supercell generation

Inputs: base structure, particle sets, occupation exchanges/dopings, lattice size, doping fractions, seed.

Algorithm:

1. Build the base lattice:
   - replicate the unit cell `A × B × C`;
   - stable site default = first particle of its set;
   - unstable site default = void.
2. For each doping in ascending priority, applied to the **current** lattice after all higher-priority
   dopings:
   a. Resolve the primary exchange: site, source particle, target particle.
   b. `available = count of source particles on the exchanged site in the current lattice`.
   c. `requested = floor(fraction × available)` using finite `fraction`; reject `NaN`/infinity as `JOB005`.
   d. If `available == 0` or `requested == 0`, apply zero and record `JOB010` only when the requested
      fraction was positive and no source particle exists. This is not a `JOB012` failure because no
      compensation was required by a nonzero primary application.
   e. If charge compensation is active and `requested > 0`:
      - compute `Δq_p = q_target_p − q_source_p` and `Δq_d = q_target_d − q_source_d`;
      - required ratio `ratio = −Δq_p / Δq_d`; Step 06 guarantees nonzero opposite-sign deltas;
      - for `n_p` from `requested` downward to `1`, compute `n_d_real = n_p × ratio`; accept the first
        `n_p` where `|n_d_real − round(n_d_real)| ≤ 1e-6` and
        `0 ≤ round(n_d_real) ≤ dependent_available`;
      - if no such `n_p` exists, the lattice is not buildable: emit `JOB012` as an **error** and stop
        generation. The preview returns `null` with the same diagnostic.
      - the tolerance is the named `ChargeNeutralityToleranceElementary = 1e-6`; it matches the original
        dependent-count reduction tolerance and is not a silent change.
   f. Sample `n_p` positions uniformly without replacement from the current source positions and replace the
      source with the target. Candidate positions are sorted by linear index before sampling.
   g. If compensation is active, recompute the dependent source positions on the current lattice, then sample
      `n_d` positions and replace. The dependent exchange is always applied after the primary.
   h. Record requested, available, applied primary, applied dependent, and effective fraction.
3. Validate the final lattice:
   - no particle is outside its site's set;
   - unstable sites are void (they are always void in the initial state);
   - doping counts match the report;
   - every applied compensated pair must itself satisfy
     `|n_p × Δq_p + n_d × Δq_d| ≤ 1e-6 × max(1, n_p + n_d)`; otherwise `JOB012` fails the build;
   - track `hasUncompensatedCharge`, true when an applied doping has compensation off and a nonzero charge
     delta (isovalent/zero-delta dopings are excluded);
   - a non-neutral **base** lattice is a model property, not a doping failure: it is reported through
     `TotalChargeBefore`/`IsChargeNeutral` but never triggers `JOB012` or `JOB016` by itself. Step 06 has no
     base-neutrality invariant, so Step 10 does not invent one;
   - if `hasUncompensatedCharge` and the final lattice is non-neutral, report `JOB016` (warning) and
     `IsChargeNeutral = false`; this also covers a mixed config with one compensated and one intentionally
     non-compensated charged doping;
   - `JOB012` is only the unrealizable/unbalanced compensated-pair failure above; it is never used for a
     pre-existing base charge.
4. Return the lattice plus a `DopingReport`.

- Same-site dopings are ordinary sequential dopings: after a partial `A→B`, a later `A→C` applies to the
  remaining `A` particles. There is no "must be the result of the earlier doping" restriction; the Step 06
  "particles remaining" rule is authoritative.
- The algorithm does not place dopants in "zones" or gradients; that limitation is documented.
- Supercell limits: A/B/C 1–256; total positions ≤ 500,000 (warning above 100,000). `JOB019` caps total
  expanded jobs at 99,999 (the original limit). Other countable bounds are in §8.

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
    int Priority,
    int AvailablePrimary,
    int RequestedPrimary,
    int AppliedPrimary,
    int AppliedDependent,
    double EffectiveFraction);
```

- `EffectiveFraction = AvailablePrimary == 0 ? 0.0 : AppliedPrimary / (double)AvailablePrimary`.
- `IsChargeNeutral = |TotalChargeAfter| <= ChargeNeutralityToleranceElementary × max(1, TotalPositions)` with the
  named `1e-6` tolerance. This predicate describes the **absolute final** lattice; a non-neutral base lattice
  can therefore set it false without any diagnostic. `JOB016` requires both `hasUncompensatedCharge` and
  `IsChargeNeutral == false`.
- The full `DopingReport` is build-time/preview data, not a package field. The package stores the compact
  `DopingInfo` summary, which is sufficient to reconstruct the displayed counts but not the per-site state.
  The GUI shows the full report from the same preview/build service.
- `DopingInfo` is machine-readable and identifies each doping:
  `"v1:{dopingId}:{priority}:{requestedPrimary}:{appliedPrimary}:{appliedDependent}:{effectiveFraction:R}"`,
  joined by `;`, ordered by ascending priority, using invariant culture. `{dopingId}` is the canonical
  lowercase hyphenated GUID (`Guid.ToString("D")`). A template with no doping values has an empty string. A
  template whose entries all apply zero counts still carries the entries, so "no doping configured" and
  "doping applied zero" remain distinguishable.
- The build fails with `JOB012` if any compensation-requested doping cannot realize a positive pair, or if the
  applied pair itself is not charge-balanced within tolerance.
- The build warns with `JOB011` if the effective fraction differs from the requested fraction.

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
- Unknown dopings, non-finite fractions, fractions outside `[0,1]`, `(A, B, C)` outside 1–256, and impossible
  compensation return `null` plus diagnostics. A null project or compiled model is a programmer error and
  throws; all project/user data problems are diagnostics, matching the Step 03–05 preview boundary.
- `TryCreateJobCountPreview` is scoped by `simulationId`: `DefaultJobCount` is that simulation's
  `Simulation.JobCount`; `EffectiveJobCount` is the job total for the template collections bound to that
  simulation after collection/config overrides; `TotalJobs` is the whole-template total across all
  simulations. It does not generate lattices and has no separate count-override parameter; the
  template/collection is the only source.
- The preview uses the same `SupercellGenerator`/doping application path as the build. A test generates the
  same request and asserts that the preview report and the lattice's `DopingReport` are identical.
- The preview cache is an LRU bounded to **8 entries and 256 MiB**, keyed by
  `(project model hash, lattice request hash, canonical doping list, catalog data version, space-group data
  SHA-256, symmetry contract version)`, and is thread-safe. A test evicts past both limits.
- `Mocassin.Packages` directly references `Core`, `Compilation`, and `Contracts`; `Symmetry` is transitive via
  `Compilation`. The service uses `ProjectHasher`, `CompiledModel`, and the contract DTOs without reversing a
  dependency.
- Step 25 keeps the last successful `CompiledModel` keyed by the project model hash. When the model changes,
  it marks the compiled model stale, clears the preview, and either recompiles through the shared Step 08
  compiler with cancellation/debounce or disables the preview with a "compile to preview" action. The preview
  service never compiles. Step 25 owns that UI/compile-cache policy; Step 10 owns only the pure preview.
- Step 06 owns only the model definitions and validation.

### 5. Selection optimizers

- An optimizer removes a `(site, particle)` start option from the KMC selection pool.
- It does not remove the particle from the lattice; it only prevents it from being selected as a transition
  start.
- **P1 mapping.** One project optimizer `(siteId, particleId)` expands to one `.msl` optimizer entry for every
  canonical P1 position owned by that site (`CompiledSite.PositionIndices`), using the compiled particle index.
  The entries are sorted by `(position_id, particle_id)`. This is the mapping Step 14 consumes; it removes the
  particle at every periodic image of the reference site, not only the canonical image.
- **Compensation masks and dual DOF.** Start from `CompiledSimulation.Intent.UseDualDofCorrection`. The
  environment set is **every `CompiledStructureModel.Environments` entry**, not the active jump collections
  (those own rules/directions, not environments). For each environment `e`:
  - original mask = `e.AllowedParticleMask`;
  - `e` is *reduced* when at least one optimizer targets a site that owns a P1 position with
    `CompiledPosition.EnvironmentIndex == e` and the optimizer particle is allowed at that position;
  - the compiled model is immutable; no mask is mutated. The predicate is computed per job.
  Clear `UseDualDofCorrection` only when at least one optimizer exists and **every** environment is reduced;
  keep it when no optimizer exists, when any environment is unchanged (including the mixed case), or when the
  bit was already clear. This reproduces the original `JumpSelectionOptimizer.Run` rule
  `isReducedMask |= optimizedMask == originalMask`; a test with three environments covers one, some, and all
  reduced. MMC always keeps `UseDualDofCorrection = false`.
- Optimizers are applied after compilation and before job encoding. A KMC simulation with no selectable start
  option for any of its transitions after optimization is `JOB009` (error); MMC simulations ignore
  optimizers.
- Validation:
  - site exists and is stable;
  - particle exists and can occupy the site;
  - no duplicate `(site, particle)` optimizer;
  - the optimizer does not remove all start options for a required mechanism (`JOB009`).
- The build report shows how many start options each optimizer removed and whether dual-DOF was cleared.

### 6. Job template validation

| Code | Severity | Rule |
| --- | --- | --- |
| `JOB001` | Error | Template has no collections |
| `JOB002` | Error | Collection references unknown simulation |
| `JOB003` | Error | Collection has no configs |
| `JOB004` | Error | Cell size outside 1–256, or total positions > 500,000 |
| `JOB005` | Error | Doping fraction is non-finite or outside [0,1] |
| `JOB006` | Error | Doping reference unknown |
| `JOB007` | Error | Duplicate collection/config name |
| `JOB008` | Error | Job count outside 1–1000 |
| `JOB009` | Error | Optimizer reference invalid or removes every usable start option for a required mechanism |
| `JOB010` | Warning | Requested doping cannot be realized; 0 applied |
| `JOB011` | Warning | Effective doping fraction differs from requested |
| `JOB012` | Error | Compensation pair cannot be realized, or an applied compensated pair is not charge-balanced |
| `JOB013` | Warning | Total jobs exceed 10,000 |
| `JOB014` | Warning | Supercell positions exceed 100,000 |
| `JOB015` | Error | Inherited settings missing a required value or not applicable to the collection's simulation kind |
| `JOB016` | Warning | Final lattice is not charge-neutral because an uncompensated charged doping was applied (including a mixed compensated/non-compensated config) |
| `JOB017` | Error | Duplicate doping reference in a config, or duplicate `(site, particle)` optimizer |
| `JOB018` | Error | Extension id is empty/duplicated, config is not a JSON object, or an extension bound is exceeded |
| `JOB019` | Error | Total expanded jobs exceed 99,999 |
| `JOB020` | Error | Template/collection/config/optimizer/doping count exceeds a Step 10 structural cap |

Validation ownership is split so `validate` catches project-level problems without a compiled model:

| Owner | Codes | Runs in |
| --- | --- | --- |
| Core `JobTemplateValidator` | `JOB001`–`JOB003`, `JOB005`–`JOB008`, `JOB009` (reference check only), `JOB015`, `JOB017`, `JOB018`, `JOB020`; cell-range part of `JOB004` | `ProjectValidator` on load, save, and edit; also inside build validation |
| Packages `JobTemplateValidationStage` (`IValidationStage`) | total-position part of `JOB004`, `JOB009` (removes-all check), `JOB010`–`JOB014`, `JOB016`, `JOB019` | Step 07 `Build` stage when a compiled model is supplied |

Duplicate `(site, particle)` optimizers are owned only by `JOB017`; `JOB009` does not duplicate that rule.

### 7. Build-stage validation and diagnostics

`ValidationContext` gains an optional `CompiledModel? CompiledModel` property (additive; defaults to `null`).
The Step 10 `JobTemplateValidationStage` delegates to the Core validator first, then runs the compiled-bound
checks; it runs only when `RunBuildStage` is true, the context carries a compiled model, and earlier stages
have no error.

- `validate` keeps `RunBuildStage = false`; it still runs the Core `JobTemplateValidator` through
  `ProjectValidator`, so structural `JOB` errors appear without a compile. This is a deliberate change from
  the earlier draft.
- `build` and `mocassin build --preview` supply the compiled model, register the Step 10 stage, and run the
  compiled-bound checks. Step 12 owns that registration and the `ValidationContext` construction.
- Step 07's `ValidationContext` snippet is updated in this design to carry the optional model; the pipeline
  remains the only Build-stage extension point.
- `JOB001`–`JOB020` are registered in the Step 07 `DiagnosticCatalog`; Step 07 does not stub them.

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
- `Extensions` is a nullable, ordered array of `{id, config}` objects, serialized inside
  `baseSettings`/`overrides` (not as a config sibling). `null` inherits; a non-null array **replaces** the
  inherited list as a whole. Step 10 validates a non-empty unique `id` (≤64 chars), an object-valued `config`
  of at most 64 KiB, at most 16 extensions and 256 KiB total per effective settings list; it preserves the
  config as canonical JSON (sorted property names, normalized numbers) so project hashes are deterministic.
  The concrete extension schema and semantic validation are owned by Steps 27/28; Step 10 does not invent an
  MMCFE schema. Invalid extension content/bounds are `JOB018`.
- **Hard limits.** At most 100 job templates per project, 100 collections per template, 1,000 configs per
  collection, 1,000 dopings per config, 1,000 optimizers per collection, and 99,999 total expanded jobs.
  `JobId` is a 1-based `int` in `1..99,999`. Count violations are `JOB020`; total-job overflow is `JOB019`.
  These bounds make a malformed project fail during validation rather than during expansion.
- `JobTemplate` (moved from `Mocassin.Core.Model`), `JobCollection`, `JobConfig`, `JobSettings`,
  `LatticeRequest`, `DopingValue`, and `SelectionOptimizer` live in `Mocassin.Core.Model.Jobs`;
  `JobCollectionId` and `JobConfigId` are new typed IDs. The move and new public surface update
  `PublicAPI.Unshipped.txt`, the ApiCompat baseline, JSON source-generation context, and namespace-using tests
  in the same feature. `ProjectJson`/`ProjectHasher` gain a canonical template converter: templates sorted by
  `JobTemplateId`, collections and configs in authored order, dopings in ascending resolved doping priority
  with ties rejected, optimizer arrays in authored order, and extensions sorted by `Id`. Empty
  collections/configs/dopings arrays are omitted on write.
- The project format remains `schemaVersion = 1` (additive pre-release change, same exception as Steps 03–09).
  `contracts/manifest.json`'s `jobTemplates.itemFields`, the generated project schema, and the project
  fixtures are updated together.
- `doping_info` is a pre-implementation `.msl` field defined here and written by Step 11. Because `.msl` is
  not implemented yet, this is a contract update without a schema-version bump; it is recorded in the step
  page and audit trail. No published package is affected, and no `.msr`/`.mcs` change is made.

### 9. Build-time types and step interfaces

```csharp
public sealed record ResolvedJobSettings(
    double TemperatureK,
    long TargetMcsp,
    int WriteOutCount,
    long? TimeLimitSeconds,
    double? MinSuccessRate,
    ExecutionFlags Flags,
    string? Instruction,
    long PreRunMcsp,
    double ElectricFieldModulusVm,
    Fractional3 ElectricFieldDirection,
    double? NormalizationEnergyEv,
    double? MaxAttemptFrequencyHz,
    double BreakTolerance,
    long BreakSampleLengthMcsp,
    long BreakSampleIntervalMcsp,
    long ResultSampleMcsp,
    ImmutableArray<JobExtension> Extensions);
```

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
- `DopingInfo` is the invariant-culture, priority-ordered machine-readable summary defined in §4. The full
  `DopingReport` is build-time/preview data; the string is the only doping metadata persisted in the package.
  The package lattice signature still covers the particle array and cell size, not the report.
- The applied count is `floor(fraction × available)`; the charge-compensation search reduces `n_p` from the
  requested value until the dependent count is an integer within tolerance and fits. Requested, available,
  applied primary, and applied dependent counts are always reported.

### 11. `mocassin build --preview` boundary

Step 10 adds only the preview path: load → validate → compile → parameterize → expand → preview. It never
opens, writes, or replaces a package and never invokes the Step 11 writer. Step 12 owns the full `build`
command, package writing, `--jobs`, `--parallel`, `--force`, partial builds, and progress events; it reuses the
same expansion and preview services. If Step 12 has not landed, the preview command is still useful and cannot
produce a half-written `.msl`.

### 12. Feature sizing

The roadmap estimate is **3–4 person-weeks**, within the four-week split trigger. The eight features above are
the complete implementation list and are owner-approved. Eight is within the roadmap's "roughly 6–8" feature
range; the features have distinct, testable outcomes and are not merged, so no 10A/10B split or further owner
exception is needed.

## Implementation tasks

- [ ] Implement typed job-template/collection/config records, `JobSettings`, extensions, and inheritance.
- [ ] Move `JobTemplate` into `Mocassin.Core.Model.Jobs`, add typed IDs, and update JSON/PublicAPI/ApiCompat.
- [ ] Implement project JSON canonicalization, manifest/schema, and persistence round-trip.
- [ ] Implement job expansion, deterministic IDs, master/job seed derivation, and structural limits.
- [ ] Implement supercell generation with sequential doping, truncation, charge compensation, and the report.
- [ ] Implement the deterministic SHA-256 counter sampler and golden vectors.
- [ ] Implement `ILatticePreviewService`, `LatticePreview`, and `JobCountPreview` over the same generator as the
  build; cover the try/failure boundary, request echo, per-site populations, bounded cache, and preview/build
  report equality.
- [ ] Implement selection optimizers, P1 image mapping, `UseDualDofCorrection`, and their validation.
- [ ] Implement the Core structural validator and the Packages compiled-bound Build stage.
- [ ] Implement `mocassin build --preview` (no package write) and register the build stage in Step 12.
- [ ] Add ceria temperature/doping sweep samples.
- [ ] Add tests for edge cases (0 %, 100 %, tiny cells, NaN/Inf, impossible compensation, same-site sequence).

## Acceptance criteria

- [ ] The ceria KMC sample expands to the expected number of jobs with deterministic IDs.
- [ ] For a 10×10×10 ceria cell and 1 % Ce doping, the applied counts and vacancy compensation are correct.
- [ ] A requested fraction that cannot be realized is adjusted downward and reported.
- [ ] An impossible charge compensation fails with `JOB012`.
- [ ] The lattice preview echoes the request, reports requested/applied counts, per-site remaining populations,
  total charge before/after, and neutrality, and matches the generated lattice's report for the same request.
- [ ] The preview returns `null` plus diagnostics for an unknown doping, a non-finite or out-of-range
  fraction, an invalid supercell, or impossible compensation; it never returns a partial preview.
- [ ] The job-count preview reports the default, effective, and total job counts without generating lattices.
- [ ] Same job descriptor + seed produces an identical lattice across runs and OSes, and the documented
  job-seed/lattice-seed/sampler golden vectors match.
- [ ] Changing a simulation-only setting does not change the lattice for the same job.
- [ ] A same-site sequential doping applies to the particles remaining after earlier dopings.
- [ ] A non-compensated charged lattice (including a mixed compensated/non-compensated config) is permitted
  with `JOB016` when `IsChargeNeutral` is false; an unrealizable/unbalanced compensated pair is `JOB012`.
  `IsChargeNeutral` uses `1e-6 × max(1, total_positions)` and never fails a pre-existing base charge by itself.
- [ ] `validate` reports structural `JOB001`–`JOB008`, the `JOB009` reference check, `JOB015`, `JOB017`,
  `JOB018`, and `JOB020` without a compiled model; `JOB009`'s removes-all check and the other compiled-bound
  codes require the build stage.
- [ ] The 99,999-job cap and the structural count caps fail with `JOB019`/`JOB020`.
- [ ] Optimizers remove only start options at every P1 image of the target site, never lattice particles, and
  clear dual-DOF only under the documented all-masks rule.
- [ ] Documentation written

## Documentation

At this point, document job templates and supercells:

- `docs/user-guide/job-templates.md` — collections/configs, inheritance, duplicates, optimizers.
- `docs/user-guide/lattice.md` — update the page created in Step 06 with doping preview, effective fractions,
  truncation, and charge compensation.
- `docs/reference/project-format.md` — job-template JSON, canonical ordering, seed derivation, and limits.
- `docs/reference/diagnostics.md` — register and describe `JOB001`–`JOB020` in the generated catalog table.
- `docs/reference/cli.md` — `build --preview` inputs, outputs, and no-write guarantee.
- `docs/development/compilation-pipeline.md` — supercell generation, truncation, charge compensation, seed
  derivation, and the shared preview service.
- `docs/development/design-decisions.md` — the build-side SHA-256 sampler, `ValidationContext.CompiledModel`,
  and the Packages/Compilation boundary.

## Tests

| Test | Assertion | Tier |
| --- | --- | --- |
| Project JSON round-trip | Nested job templates canonicalize, sort, reject unknown properties, and preserve authored IDs/order | PR-fast |
| Persistence hashes | Template/doping/extension edits change `ProjectHasher` and document hashes; empty arrays are omitted | PR-fast |
| Expansion | Job counts/IDs/names match template order and the 99,999 cap | PR-fast |
| Inheritance | Base/config/job overrides, optional job count, and extension replacement resolve correctly | PR-fast |
| Seeds | Documented job-seed/lattice-seed golden vectors match; independent index changes change the seed | PR-fast |
| Sampling | Fisher–Yates golden vector matches; uniform without replacement; deterministic per seed | PR-medium |
| Supercell | Default occupations, replication order, row-major index formula | PR-fast |
| Doping | Counts, truncation, priority order, multiple dopings, same-site sequential doping | PR-medium |
| Charge | Integer and fractional charge ratios, 1e-6 tolerance; `JOB016` only with an applied uncompensated charged doping; compensated `JOB012`; a pre-existing non-neutral base sets `IsChargeNeutral=false` with no `JOB012`/`JOB016` | PR-medium |
| Doping report | Available/requested/applied counts, `EffectiveFraction`, `DopingInfo` round-trip, no-doping vs zero-applied | PR-fast |
| Doping property | Random dopings keep applied counts within available bounds and stay deterministic per seed | PR-medium |
| Sampling property | Random valid requests never exceed candidates; determinism holds across repeated runs | PR-medium |
| Lattice preview | Request/doping echo, applied counts, per-site populations, total charge, neutrality | PR-fast |
| Preview failure | Unknown doping, non-finite/out-of-range fraction, invalid supercell, impossible compensation -> null + diagnostics; no partial preview | PR-fast |
| Preview/build equality | The preview report equals the generated lattice's `DopingReport` for the same request | PR-fast |
| Preview cache | Keys separate project model, request, doping list, and catalog/contract versions; 8-entry/256-MiB eviction | PR-fast |
| Job-count preview | Simulation-scoped default/effective counts, whole-template total, template overrides, no lattice generation, no duplicate override parameter | PR-fast |
| Validation | `validate` catches structural `JOB` errors; `build` catches compiled-bound `JOB` errors | PR-fast |
| Optimizers | Every P1 position of the site removed; lattice unchanged; dual-DOF one/some/all environment reduction; `JOB009`; duplicate `JOB017` | PR-fast |
| Limits | Collection/config/doping/optimizer/extension caps and `JOB019`/`JOB020` | PR-fast |
| Edge cases | 0 %, 100 %, tiny cell, impossible ratio, NaN/Inf | PR-fast |
| Performance | 10⁴ small jobs expand in < 10 s; generation peak stays under 1 GiB with cancellation every job | PR-medium |
| Writer/reader integration | Later Step 11/13 round-trip test; deferred to those steps, not claimed here | nightly |

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Charge-compensation rounding surprises users | Preview report; explicit requested vs. applied; documented 1e-6 tolerance |
| Fractional/charge-inconsistent dopings | Downward compensation search and neutral-compensated check; non-compensated `JOB016` |
| Multiple/same-site dopings interact unpredictably | Sequential remaining-particle application; preview sequence; property tests |
| Preview and generated lattice diverge | One generator; preview/build equality test |
| Preview cache returns stale data | Cache key includes the project model hash, request, doping list, and catalog/contract versions; bounded LRU |
| Non-deterministic sampling | Golden seeds/vectors; sorted candidates; fixed SHA-256 counter stream |
| Cross-language seed mismatch | ADR 0005 tuple order, fixed byte encoding, and golden vectors shared with Step 13 |
| Memory for huge lattices/jobs | 99,999-job cap, structural caps, lazy streaming, and cancellation |
| Optimizer over/under-removal | Explicit P1 image expansion and all-masks dual-DOF test |

## Done when

A job template can be expanded into deterministic job descriptors and valid supercells, with a report that
explains every applied doping count and charge: compensated pairs are balanced, non-compensated charge is
warned, and a pre-existing base charge is reported without being misclassified as a doping failure.
