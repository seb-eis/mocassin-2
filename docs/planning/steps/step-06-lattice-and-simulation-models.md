# Step 06 — Lattice and Simulation Models

**Phase:** B. Model definition  
**Depends on:** 03, 04
**Delivers:** Occupation exchanges, ordered dopings, and MMC/KMC simulation definitions with defaults and
validation.

## Implementation status

- **Status:** Done
- **Step branch:** `step/06-lattice-and-simulation-models` (merged and deleted)
- **Step CI:** [closeout green](https://github.com/seb-eis/mocassin-2/actions/runs/43085605850)
- **Main merge:** `41cfa4d` (main CI [43091007170](https://github.com/seb-eis/mocassin-2/actions/runs/43091007170) green)
- **Step tag:** `step-06`
- **Audit:** Passed with notes at `c390dcc` (code frozen at `ea6293e`); F1 and N1/N3 accepted by the human owner on 2026-11-11
- **Design review:** [Ready with notes — accepted at `6173e61`](../reviews/step-06.md)
- **Last updated:** 2026-11-11

| Feature | Branch | Status | Local gates | Step CI | Squash commit |
| --- | --- | --- | --- | --- | --- |
| `lattice-model` | `feature/step-06-lattice-and-simulation-models/lattice-model` | Done | `verify.sh dotnet`, `structure`, `api` pass | — | `bc333f3` |
| `simulation-model` | `feature/step-06-lattice-and-simulation-models/simulation-model` | Done | `verify.sh dotnet`, `structure`, `api` pass | — | `1c64c96` |
| `persistence-and-contracts` | `feature/step-06-lattice-and-simulation-models/persistence-and-contracts` | Done | `verify.sh dotnet`, `contracts`, `api`, `structure` pass | — | `48e1f20` |
| `queries-and-editing` | `feature/step-06-lattice-and-simulation-models/queries-and-editing` | Done | `verify.sh dotnet`, `structure`, `api` pass | — | `cfeb7b6` |
| `samples-and-docs` | `feature/step-06-lattice-and-simulation-models/samples-and-docs` | Done | `verify.sh dotnet`, `docs`, `structure` pass | — | `46ea087` |
| `closeout` | `feature/step-06-lattice-and-simulation-models/closeout` | Done | `verify.sh all` pass | [closeout green](https://github.com/seb-eis/mocassin-2/actions/runs/43085605850) | `ea6293e` |

## Goal

Complete the user-authored model: describe how supercells are doped and which transitions/settings define each
simulation. These definitions feed step 10 (job expansion and supercells) and step 08 (simulation encoding).

## Non-goals

- No lattice generation, doping-fraction application, integer truncation, charge-compensation counts, or
  lattice preview. Step 10 owns the single supercell generator and its preview.
- No job-template expansion, effective job counts, total-job preview, or package encoding. Steps 10 and 11 own
  those.
- No compiled transition rules, rule deduplication, inverse/dependent-rule pairing, or conservation filtering.
  Step 08 owns those.
- No transition definition or transition-path validation. Step 04 owns those.
- No energy/interaction model. Step 05 owns those.
- No GUI. Steps 24 and 25 own the editors and consume the model, query, and preview contracts defined here and
  in Step 10.
- No legacy `.mocprj` lattice or simulation import, and no persistence of derived previews, building blocks, or
  effective job counts.

## Original reference

- Lattice model:
  - `IBuildingBlock` — default cell entries (implicitly the first particle of each stable site and void on
    unstable sites).
  - `IDoping` — primary and counter doping, building block, priority, `UseCounterDoping`. The original
    `Priority` property is documented as a simultaneous-doping group ID, but the generator orders the
    dictionary by `Priority` and applies the entries sequentially.
  - `IDopingCombination` — dopant, dopable, site, charge delta.
  - `FastDopedByteLatticeSource` builds `byte[a,b,c,P]` lattices from a population table; the primary count is
    `floor(fraction × available)` and the dependent count is rounded and then reduced until the charge balance
    holds within `1e-6`.
  - Dopings are applied in ascending priority; each applies to the particles remaining after the earlier
    entries.
  - The original counter-doping validator emits a critical warning when the primary and counter charge deltas
    have the same sign; it does not reject the pair.
- Simulation model:
  - `ISimulation` — custom RNG seed, temperature, target MCSP, `WriteOutCount`, job count, save-run time limit,
    lower success-rate limit.
  - `IKineticSimulation` adds pre-run MCSP, electric-field vector/magnitude, normalization energy, and
    transitions. The max attempt frequency is the KMC job-configuration base frequency (Step 10); Step 06
    keeps an optional simulation-level default for job templates to inherit.
  - `IMetropolisSimulation` adds break tolerance, break sample length/interval, result sample MCSP,
    transitions.
  - Original UI defaults: temperature 1000 K, target MCSP 1000, write-out count 100, job count 12, time limit
    P1Y, KMC pre-run 100, field 1e7 V/m along `(1,0,0)`, normalization 0, MMC break tolerance 0, break sample
    length 1000, break sample interval 100, result sample 200.
  - Original setting ranges: temperature 0.1–10,000 K (recommended 100–5000); target MCSP 1–1,000,000
    (recommended up to 1000); job count 1–1000 (recommended 5–100); write-out count 0–1000 (recommended 5–100);
    pre-run MCSP −1–1,000,000 (recommended up to 1000); field modulus 0–`double.MaxValue` (recommended
    1e4–1e9 V/m); normalization −1–`double.MaxValue` (recommended up to 10); break tolerance 0–1 (recommended
    up to 0.1); break sample length 0–100,000 (recommended up to 10,000); break sample interval 1–1000
    (recommended up to 100); result sample 0–1,000,000 (recommended up to 10,000); transition count 0–100 per
    authoring simulation (recommended 1–10; a build requires at least one); seed string at most 100 characters;
    lower success rate 0–`int.MaxValue` (recommended at least 10 MCS/s).
  - Original empty RNG seeds are replaced by a GUID; the new implementation derives them from the project/job
    seed instead (ADR 0005; recorded deviation).
- Known issues: the default building block is read-only and implicit; duplicate priorities have no defined
  tie-break; doping integer truncation must account for dependent-exchange ratios; charge compensation can
  reduce the applied fraction below the requested value. Step 10 owns the truncation and compensation-count
  semantics.

## Design

### 1. Model composition and persistence

`ModelDefinition` gains the flat lattice and simulation sections alongside the Step 03–05 sections:

```csharp
public sealed record ModelDefinition
{
    // Step 03–05 sections omitted here.
    ImmutableArray<OccupationExchange> OccupationExchanges { get; init; }
    ImmutableArray<Doping> Dopings { get; init; }
    ImmutableArray<Simulation> Simulations { get; init; }
}
```

- The model stays flat on disk: `model.occupationExchanges`, `model.dopings`, and `model.simulations`.
- `ModelDefinitionJsonConverter`, the nested `ProjectJsonContext` in `Project/ProjectJson.cs`,
  `ProjectJson.CanonicalizeModel`, `ProjectValidator`, and `ProjectHasher` are extended for the new sections.
  Canonical persistence is part of the contract, not an implementation detail:
  - `ProjectJson.CanonicalizeModel` rebuilds and canonicalizes all model sections;
  - `ProjectJsonConverter.Write` and `ProjectStore.SaveAsync` consume the canonical model, so the sections are
    written rather than dropped;
  - `ProjectHasher.Compute` hashes the canonical model, so lattice/simulation edits change the model hash and
    `ProjectHasher.ComputeDocument` changes with them;
  - `ProjectEditor.Apply` therefore sees a changed document hash instead of treating the edit as a no-op.
- Empty lattice/simulation arrays are omitted on write, matching the existing behavior for optional transition
  and energy arrays. Missing arrays load as empty, and `CanonicalizeModel` treats default arrays as empty. A
  project created before Step 06 therefore keeps its canonical model/document hash; a hash-stability fixture
  and test prove this.
- `ProjectSchema` needs no change while `schemaVersion` remains 1; it owns only the version constants. There is
  no standalone `ProjectJsonContext.cs` file.
- Semantic ordering:
  - `Dopings` are serialized in ascending `Priority`; priorities are unique.
  - `OccupationExchanges` and `Simulations` are sorted by `id`.
  - `ExecutionFlags` are serialized as an array sorted by flag value; `None` is the empty array.
  - `TransitionIds` preserve authored order, matching Step 04's semantic-order rule. The persisted
    `kineticTransitionIds` and `metropolisTransitionIds` arrays preserve that order and are never re-sorted.
    Transition assignment order is not physically significant, but it is user-visible and must not change
    silently.
- The project `schemaVersion` remains 1 because the lattice/simulation sections are additive and the format is
  pre-release. This follows the Step 03–05 additive-v1 precedent: the manifest `status`/`notes` are updated,
  the generated schema, descriptors, and fixtures are refreshed, and the first released format resumes ADR
  0009's normal version-bump discipline. The exemption is not general.
- Defaults:
  - no occupation exchanges, dopings, or simulations;
  - the default building block is derived, not stored;
  - a new simulation uses the documented defaults in section 3.
- Lattice previews, job-count previews, effective job counts, and applied-doping reports are derived data and
  are never persisted.

### 2. Lattice model

```csharp
public sealed record OccupationExchange(
    OccupationExchangeId Id,
    string Name,
    CellSiteId SiteId,
    ParticleId SourceParticleId,
    ParticleId TargetParticleId);

public sealed record Doping(
    DopingId Id,
    string Name,
    int Priority,
    OccupationExchangeId PrimaryExchangeId,
    OccupationExchangeId? DependentExchangeId,
    bool ChargeCompensation);

public sealed record BuildingBlockEntry(
    CellSiteId SiteId,
    ParticleId ParticleId);
```

- The default building block is derived, not stored:
  - stable site: first particle of its particle set;
  - unstable site: `ParticleId.Void`.
  It is exposed as `ILatticeQuery.DefaultBuildingBlock` and is recomputed from the immutable project snapshot;
  it is never persisted.
- `OccupationExchange`:
  - `SiteId` must be an existing stable site;
  - `SourceParticleId` and `TargetParticleId` must be members of the site's particle set;
  - the source and target must differ;
  - the exchange is a reusable pool item; it has no effect until referenced by a doping;
  - the exchange's charge delta is `target.charge − source.charge` and is derived, not stored.
- `Doping`:
  - `Priority` is unique and in `0..9999`; ties are rejected so the application order is deterministic.
  - If `ChargeCompensation` is false, `DependentExchangeId` must be null and the primary exchange may have any
    charge delta, including zero for an isovalent substitution.
  - If true, the dependent exchange is required, both charge deltas must be non-zero, and they must have
    opposite signs. The integer ratio/realizability is a Step 10 check because it depends on the supercell and
    fraction.
  - The primary exchange defines the requested fraction; the dependent exchange is applied automatically to
    maintain charge neutrality.
  - Doping and occupation-exchange names are unique case-insensitively within their collection, matching the
    Step 04 naming rule.
- Step 06 validates only the model-level definition. It does not know a fraction or supercell. Step 10 owns:
  - fraction range and unknown-doping references (`JOB005`/`JOB006`);
  - requested-versus-applied counts, truncation, and compensation ratios (`JOB010`–`JOB012`);
  - the same-site application sequence and effective fraction (`JOB011`);
  - supercell and job-count limits (`JOB004`/`JOB008`/`JOB013`/`JOB014`).
- Deviations from the original, recorded deliberately:
  - priorities must be unique; the original allowed duplicate group IDs with dictionary-order tie-breaks;
  - a wrong-sign dependent exchange is an error, not a warning, because the pair cannot compensate charge;
  - occupation-exchange and doping names are unique case-insensitively; the original did not enforce this;
  - the model stores one derived building block instead of a read-only `IBuildingBlock` object;
  - negative normalization energy is rejected; the original −1/0 values both meant “no normalization” and
    the UI coerced non-positive values to 0, so the new model uses `null` for that state.
- A doping preview is not part of Step 06. Step 10 owns the single lattice preview service and the shared
  supercell generator.

### 3. Simulation model

```csharp
public abstract record Simulation
{
    public required SimulationId Id { get; init; }
    public required string Name { get; init; }
    public abstract SimulationKind Kind { get; }
    public double TemperatureK { get; init; } = 1000.0;
    public long TargetMcsp { get; init; } = 1000;
    public int WriteOutCount { get; init; } = 100;
    public long? TimeLimitSeconds { get; init; } = 31_536_000;
    public double? MinSuccessRate { get; init; }
    public int JobCount { get; init; } = 12;
    public string? RngSeed { get; init; }
    public ExecutionFlags Flags { get; init; }
    public string? Instruction { get; init; }
    public ImmutableArray<TransitionRef> TransitionIds { get; init; } = [];
}

public sealed record KineticSimulation : Simulation
{
    public override SimulationKind Kind => SimulationKind.Kmc;
    public long PreRunMcsp { get; init; } = 100;
    public double ElectricFieldModulusVm { get; init; } = 1.0e7;
    public Fractional3 ElectricFieldDirection { get; init; } = new(1.0, 0.0, 0.0);
    public double? NormalizationEnergyEv { get; init; }
    public double? MaxAttemptFrequencyHz { get; init; }
}

public sealed record MetropolisSimulation : Simulation
{
    public override SimulationKind Kind => SimulationKind.Mmc;
    public double BreakTolerance { get; init; }
    public long BreakSampleLengthMcsp { get; init; } = 1000;
    public long BreakSampleIntervalMcsp { get; init; } = 100;
    public long ResultSampleMcsp { get; init; } = 200;
}

[Flags]
public enum ExecutionFlags
{
    None = 0,
    NoSaving = 1,
    NoJumpLogging = 2,
    UseFastExp = 4,
    Deterministic = 8
}
```

- `Kind` is derived from the concrete record type and is not settable. The persisted DTO carries `kind` so the
  reader can select the concrete type; a `kind` that disagrees with the kind-specific fields is `SIM023`.
  Switching kind is a delete-and-create editor operation with reference checks.
- `TransitionRef` is defined in Step 04 (`Mocassin.Core.Model.Transitions`). It carries a `TransitionKind`
  (`Kinetic` or `Metropolis`) and the referenced ID. The kind mapping is exact: `SimulationKind.Kmc` ↔
  `TransitionKind.Kinetic` and `SimulationKind.Mmc` ↔ `TransitionKind.Metropolis`; a simulation rejects a
  reference of the other kind (`SIM002`). The persisted simulation DTO writes only the matching
  `kineticTransitionIds` or `metropolisTransitionIds` array.
- `TransitionIds` is an authored-order array; duplicates of the same `(kind, id)` are rejected (`SIM022`). A
  simulation may reference transitions that are also used by other simulations.
- `TargetMcsp` is the number of successful steps per mobile particle for the main run.
- `WriteOutCount` is the number of intermediate data-out calls; 0 writes only the final state. It is the
  original `ISimulation.WriteOutCount` / UI “simulation block count”.
- `TimeLimitSeconds` is the wall-clock run limit; null means no limit. The default is P1Y (31,536,000 s).
- `MinSuccessRate` is the lower abort rate in MCS/s (Step 16 defines the abort); null or 0 disables it, and
  0 canonicalizes to null so equivalent settings hash identically. It is deliberately not a fraction.
- `RngSeed` is optional; null or empty means derive from the project/job seed, and empty canonicalizes to null.
  A non-empty seed is preserved verbatim and is limited to 100 characters (`SIM020`). The original replaced an
  empty seed with a GUID; that is a recorded deviation required by ADR 0005's deterministic seed rule.
- KMC:
  - `PreRunMcsp` is the relaxation/normalization pre-run; 0 disables it. The original used −1 as the disabled
    sentinel; the new format uses 0 so the contract field can stay unsigned.
  - `ElectricFieldDirection` is a fractional vector; the solver normalizes it to Cartesian. A zero vector with
    a non-zero modulus is invalid (`SIM007`).
  - `NormalizationEnergyEv` null means no normalization (the original default 0); `0.0` is accepted and
    canonicalizes to null, and a positive value enables fixed normalization.
  - `MaxAttemptFrequencyHz` null means use the maximum from the transition rules; a positive value rescales
    all frequencies while preserving ratios. This is a simulation-level default for Step 10's KMC job
    configuration (the original job `BaseFrequency`).
- MMC:
  - `BreakTolerance` 0 disables the equilibrium break check.
  - Sample lengths/intervals and the result sample are MCSP counts.
- `Flags` contains `NoSaving`, `NoJumpLogging`, `UseFastExp`, and `Deterministic`; the project JSON persists
  camelCase flag names, while the `.msl` job payload uses the `u32` bitmask.
  - `NoSaving` skips `.mcs` state/checkpoint writes.
  - `NoJumpLogging` disables the KMC jump histogram.
  - `UseFastExp` selects the documented fast exponential approximation instead of libm `exp`.
  - `Deterministic` selects the canonical RNG-consumption path by disabling Step 16's `UseFrequencyPrecheck`
    and any optional shortcut that skips or reorders RNG draws. Baseline reproducibility is already guaranteed
    by ADR 0005; this flag changes the trajectory by disabling the pre-check, not the long-time distribution.
    The default `None` leaves the pre-check enabled. It is a new flag with no original counterpart.
  - `KmcSimulation`/`MmcSimulation` are derived from `Kind`, `UsePrerun` is derived from `PreRunMcsp > 0`, and
    `UseDualDofCorrection` is a compiler/optimizer-derived KMC time-stepping flag. They must not appear in the
    project `executionFlags` array (`SIM035`).
  - The `.msl` word is a separate `SolverJobFlags` contract in `Mocassin.Contracts`, not the user-authored
    `ExecutionFlags` enum. `Mocassin.Compilation` does not reference Contracts: the compiler/job builder carries
    the derived intent (`Kind`, `PreRunMcsp`, `UseDualDofCorrection`) on the job descriptor, and
    `Mocassin.Packages` composes the `SolverJobFlags` word when writing the `.msl` payload. Steps 14/16 read
    the resulting bits.
- Deviations from the original, recorded deliberately:
  - write-out count 0 is a supported final-state-only mode; `SIM028` does not warn for it even though the
    original recommended band starts at 5, because 0 is a valid hard-range value and a deliberate mode.
- `Instruction` is an extension/routine instruction (MMCFE); the structured `extensions` configuration is a
  job-template override (Steps 10/27) and is preferred, but the string is preserved for advanced/legacy jobs.
- Defaults (matching the original user experience):
  - temperature 1000 K;
  - target MCSP 1000;
  - write-out count 100;
  - time limit 31,536,000 s (P1Y);
  - min success rate null (disabled);
  - job count 12;
  - KMC pre-run 100;
  - KMC field 1e7 V/m along `(1,0,0)`;
  - KMC normalization null (none);
  - KMC max attempt frequency null (from the rules);
  - MMC break tolerance 0;
  - MMC break sample length 1000, interval 100, result sample 200.
- Simulation names are unique case-insensitively within the project (`SIM009` error).
- Multiple simulations may share transitions; a transition can be active in several simulations.
- Job count: `Simulation.JobCount` is the default duplicate count. Step 10's job collection may override it
  (`jobCountPerConfig`), computes the effective count, and owns the total-job and package limits. Step 06 does
  not compute effective counts.

### 4. Validation rules

Step 06 owns the model-level lattice/simulation rules. Cross-step ownership is explicit:

| Owner | Rules |
| --- | --- |
| Step 06 | `LAT001`–`LAT011`, `SIM001`–`SIM037`: definition-level references, ranges, names, kind compatibility, defaults, and shape |
| Step 04 | `TRN001`–`TRN028`: transition definition and resolved-path validity |
| Step 07 | feasibility: every simulation's transition set is non-empty at build time, zero-mobile checks, and cross-domain preconditions |
| Step 10 | `JOB001`–`JOB020`: template structure, fractions, supercell/job limits, same-site application, truncation, charge compensation, extensions, and effective counts; structural codes run in `validate`, compiled-bound codes in the build stage |

| Code | Severity | Rule |
| --- | --- | --- |
| `LAT001` | Error | Occupation exchange references an unknown site or particle |
| `LAT002` | Error | Site is unstable |
| `LAT003` | Error | Source particle is not in the site's particle set |
| `LAT004` | Error | Target particle is not in the site's particle set |
| `LAT005` | Error | Source equals target |
| `LAT006` | Error | Doping priority is duplicated |
| `LAT007` | Error | Doping references an unknown occupation exchange |
| `LAT008` | Error | Dependent exchange and `ChargeCompensation` disagree (dependent present when compensation is off, or compensation on without a dependent) |
| `LAT009` | Error | When compensation is on, the dependent exchange does not compensate: a charge delta is zero, or the deltas have the same sign |
| `LAT010` | Error | Doping priority is outside `0..9999` |
| `LAT011` | Error | Occupation-exchange or doping name is empty or duplicated (case-insensitive) |
| `SIM001` | Error | Simulation references an unknown transition |
| `SIM002` | Error | MMC simulation references a KMC transition (or vice versa) |
| `SIM003` | Warning | Simulation has no transitions; it cannot be built until Step 07/10 feasibility passes |
| `SIM004` | Error | Temperature is non-finite or outside 0.1–10,000 K |
| `SIM005` | Error | Target MCSP outside 1–1,000,000 |
| `SIM006` | Error | Job count outside 1–1000 |
| `SIM007` | Error | KMC field direction has a non-finite component, or is zero while the modulus is non-zero |
| `SIM008` | Error | KMC pre-run MCSP outside 0–1,000,000 |
| `SIM009` | Error | Simulation name is empty, whitespace-only, or duplicated (case-insensitive) |
| `SIM010` | Error | Write-out count outside 0–1000 |
| `SIM011` | Error | Time limit is present but not positive |
| `SIM012` | Error | Min success rate is negative, non-finite, or above `int.MaxValue` |
| `SIM013` | Error | KMC field modulus is negative or non-finite |
| `SIM014` | Error | KMC normalization energy is negative or non-finite |
| `SIM015` | Error | KMC max attempt frequency is zero, negative, or non-finite |
| `SIM016` | Error | MMC break tolerance is non-finite or outside 0–1 |
| `SIM017` | Error | MMC break sample length outside 0–100,000 |
| `SIM018` | Error | MMC break sample interval outside 1–1000 |
| `SIM019` | Error | MMC result sample outside 0–1,000,000 |
| `SIM020` | Error | RNG seed is longer than 100 characters |
| `SIM021` | Error | A simulation references more than 100 transitions |
| `SIM022` | Error | A simulation references the same `(kind, id)` transition more than once |
| `SIM023` | Error | `kind`, the kind-specific transition array/settings, and the concrete record disagree |
| `SIM024` | Warning | Temperature outside the recommended 100–5000 K |
| `SIM025` | Warning | Target MCSP above the recommended 1000 |
| `SIM026` | Warning | Job count outside the recommended 5–100 |
| `SIM027` | Warning | KMC pre-run above the recommended 1000 |
| `SIM028` | Warning | Write-out count outside the recommended 5–100 (0 is allowed) |
| `SIM029` | Warning | KMC field modulus outside the recommended 1e4–1e9 V/m |
| `SIM030` | Warning | MMC break tolerance above the recommended 0.1 |
| `SIM031` | Warning | MMC break sample length above the recommended 10,000 |
| `SIM032` | Warning | MMC break sample interval above the recommended 100 |
| `SIM033` | Warning | MMC result sample above the recommended 10,000 |
| `SIM034` | Warning | KMC normalization energy above the recommended 10 eV |
| `SIM035` | Error | The project execution-flag array contains a duplicate or a derived/compiler-owned flag |
| `SIM036` | Warning | A simulation references 11–100 transitions (recommended 1–10) |
| `SIM037` | Warning | Min success rate is positive but below 10 MCS/s (recommended at least 10) |

- `LAT011` replaces the earlier draft's same-site application-order rule. That rule needs counts and a
  supercell, so it is Step 10's `JOB011`; Step 06 does not duplicate it.
- The original warning-only settings checks become hard errors at the original minimum/maximum, while the
  original recommended bands remain warnings. This is the same severity policy used by Steps 03–05: a value
  that cannot be built is an error, and an unusual but buildable value is a warning.
- All numeric range rules treat NaN and infinities as errors; non-finite values are checked before
  comparisons. `ElectricFieldDirection` must contain three finite components.
- The original wrong-sign counter-doping warning becomes `LAT009` error. The chosen interpretation is
  recorded here and tested; it is not a silent scientific correction.
- A newly created simulation has no transitions (`SIM003` warning), matching the original transition-count
  minimum of 0. Step 07/10 blocks a build until the set is non-empty.
- `Mocassin.Core.Model.Lattice.LatticeModelValidator` owns `LAT001`–`LAT011`.
  `Mocassin.Core.Model.Simulations.SimulationModelValidator` owns `SIM001`–`SIM037`. Both need only the model
  graph and primitive types. `Mocassin.Packages.Projects.ProjectValidationService` composes them with the
  existing Core/Symmetry validators.
- Every used code has a positive and negative test; each diagnostic carries a stable `ModelPath`, a hint, and a
  help anchor documented by Step 07.

### 5. Editing operations

- Add/update/remove occupation exchanges and dopings.
- Reorder doping priorities with a single operation that renumbers deterministically.
- Add/update/remove simulations; switch kind is a delete-and-create with reference checks.
- Assign transitions through a multi-select view model; the editor preserves the user's assignment order.
- Duplicate a simulation with new IDs, the same settings, and a copied authored-order transition list.
- Editing lives in partial `ProjectEditor.Lattice.cs` and `ProjectEditor.Simulations.cs` files so the existing
  `ProjectEditor.cs` stays below the 800-line limit.

### 6. Public API

```csharp
// Mocassin.Core.Model.Lattice
public interface ILatticeQuery
{
    ImmutableArray<OccupationExchange> OccupationExchanges { get; }
    ImmutableArray<Doping> Dopings { get; }
    ImmutableArray<BuildingBlockEntry> DefaultBuildingBlock { get; }

    OccupationExchange? Find(OccupationExchangeId id);
    Doping? Find(DopingId id);
}

// Mocassin.Core.Model.Simulations
public interface ISimulationQuery
{
    ImmutableArray<Simulation> Simulations { get; }
    Simulation? Find(SimulationId id);
}

// Mocassin.Core.Project
public interface IProjectQuery :
    IParticleQuery,
    IStructureQuery,
    ITransitionQuery,
    IEnergyQuery,
    ILatticeQuery,
    ISimulationQuery
{
}
```

- `ILatticeQuery` and `ISimulationQuery` are implemented by the immutable `Project` snapshot and added to
  `IProjectQuery`, so compilation, preview, and the GUI consume one same-snapshot object.
- `DefaultBuildingBlock` is derived from the snapshot; invalid or missing sets are reported by the validator
  rather than throwing.
- The lattice preview, job-count preview, applied-doping report, and effective job counts are Step 10
  contracts. Step 06 deliberately exposes no preview service; this avoids a second truncation/compensation
  implementation.

## Code layout

The new code follows the structure policy: PascalCase domain folders, namespaces that match folders, no new
root-level files, and no folder above 20 direct files.

```text
Mocassin.Core/
  Model/
    ModelDefinition.cs                  # add lattice/simulation sections and empty defaults
    ModelDefinitionJsonConverter.cs     # read/write the flat sections and the simulation DTO
    Lattice/
      OccupationExchange.cs
      Doping.cs
      BuildingBlockEntry.cs
      ILatticeQuery.cs
      LatticeModelValidator.cs
    Simulations/
      Simulation.cs
      KineticSimulation.cs
      MetropolisSimulation.cs
      SimulationKind.cs
      ExecutionFlags.cs
      ExecutionFlagsJsonConverter.cs
      ISimulationQuery.cs
      SimulationModelValidator.cs
  Diagnostics/
    LatticeDiagnosticCodes.cs           # LAT001-LAT011
    SimulationDiagnosticCodes.cs        # SIM001-SIM037
  Editing/
    ProjectEditor.cs                    # existing Apply/validation core
    ProjectEditor.Lattice.cs            # occupation exchanges, dopings, priority reorder
    ProjectEditor.Simulations.cs        # add/update/remove/duplicate/switch kind
  Project/
    Project.cs                          # implements ILatticeQuery/ISimulationQuery
    IProjectQuery.cs                    # add ILatticeQuery and ISimulationQuery
    ProjectJson.cs                      # canonical model, nested ProjectJsonContext
    ProjectHasher.cs                    # canonical model/document hash already covers new sections
    ProjectValidator.cs                 # compose lattice/simulation validators
```

The typed IDs `OccupationExchangeId`, `DopingId`, and `SimulationId` already exist in `Ids/TypedIds.cs`
(added before Step 06); do not duplicate them. `SimulationKind`, `ExecutionFlags`, and the flag converter are new.

Tests mirror the source concept:

- `Mocassin.Core.Tests/Lattice/OccupationExchangeTests.cs`
- `Mocassin.Core.Tests/Lattice/DopingTests.cs`
- `Mocassin.Core.Tests/Lattice/LatticeModelTests.cs`
- `Mocassin.Core.Tests/Simulations/SimulationTests.cs`
- `Mocassin.Core.Tests/Simulations/SimulationModelTests.cs`
- `Mocassin.Core.Tests/Project/LatticeSimulationPersistenceTests.cs`
- `Mocassin.Packages.Tests/Projects/ProjectValidationServiceTests.cs` (composition)

Samples live under `samples/lattice/` and `samples/simulations/`. The new user-guide pages are added to
`zensical.toml` navigation in the same step.

## Implementation tasks

- [x] Extend `ModelDefinition` with the flat lattice/simulation sections and empty defaults; update
  `ModelDefinitionJsonConverter`, the nested `ProjectJsonContext`, `ProjectJson.CanonicalizeModel`,
  `ProjectValidator`, and `ProjectHasher`.
- [x] Add save/load and document-hash tests proving the new sections survive `ProjectJsonConverter.Write` and
  change `ProjectHasher.Compute`/`ComputeDocument`; cover empty-array omission and a pre-Step-06 hash-stability
  fixture.
- [x] Canonicalize equivalent simulation settings before save/hash: `minSuccessRate` 0 -> null, empty
  `rngSeed` -> null, `normalizationEnergyEv` 0.0 -> null.
- [x] Implement `OccupationExchange`, `Doping`, `BuildingBlockEntry`, and the derived default building block.
- [x] Implement charge-delta calculation and compensation sign/zero validation.
- [x] Implement `ILatticeQuery`, `ISimulationQuery`, and their `IProjectQuery`/`Project` integration.
- [x] Implement `Simulation`, `KineticSimulation`, `MetropolisSimulation`, `SimulationKind`, `ExecutionFlags`,
  and the sorted-camelCase-array/u32 flag converter.
- [x] Implement transition-kind compatibility, authored-order transition lists, duplicate-reference rejection,
  and the flat persisted simulation shape.
- [x] Implement simulation defaults, hard limits, recommended-band warnings, and seed validation.
- [x] Implement editor operations in partial `ProjectEditor.Lattice.cs`/`ProjectEditor.Simulations.cs`.
- [x] Add the Core lattice/simulation diagnostic codes and help text for `LAT001`–`LAT011` and
  `SIM001`–`SIM037`.
- [x] Update `contracts/manifest.json`: set `status` to `step-06`, rewrite `notes`, add the lattice/doping and
  simulation model fields, add the `project-lattice-minimal`, `project-lattice-populated`,
  `project-simulations-minimal`, and `project-simulations-populated` fixtures, and refresh
  `contracts/generated/fixtures/descriptors.json` and the generated project schema. Keep the flat-object
  simulation shape (the generator has no `oneOf`/discriminator support) and document the additive-v1/ADR 0009
  reconciliation.
- [x] Define the `SolverJobFlags` contract and bit table in `Mocassin.Contracts` (authored bits plus the
  compiler-derived `useDualDofCorrection`, `usePrerun`, and kind bits), with the mapping tests shared by Step
  11.
- [x] Add XML docs, update `PublicAPI.Unshipped.txt` (including `Mocassin.Contracts`), and refresh the
  ApiCompat baseline for every new public type and member.
- [x] Add `SPDX-License-Identifier: MIT` to every new `.cs` file; no new third-party dependency.
- [x] Add ceria reduction/hydration and barium-zirconate doping samples.
- [x] Add KMC/MMC simulation samples covering the documented defaults and a non-default configuration.
- [x] Add the new user-guide/reference pages to `zensical.toml` navigation and to `docs/user-guide/index.md`,
  `docs/reference/index.md`, `docs/development/index.md`, and `samples/README.md`.
- [x] Update the parity matrix rows for occupation exchanges, dopings, charge compensation, simulation kinds,
  settings, defaults, and limits to `Implemented`/`Partial` as appropriate; leave generation/preview rows with
  Step 10 and the editors with Step 25.
- [x] At closeout, update `docs/planning/status.md`, the Step 06 status block, and
  `docs/planning/audits/step-06.md` (including the step-specific checklist: add round-trip, schema, PublicAPI,
  defaults/limits, and ordering items; remove the lattice-preview item, which moves to Step 10), run
  `./tools/verify.sh all`, dispatch the single Ubuntu closeout CI run, and record the run.
- [x] Add a task to Step 10 for the shared lattice/job-count preview service and its tests; Step 06 does not
  implement a preview.

## Acceptance criteria

- [x] The ceria reduction doping (Ce⁴⁺→Ce³⁺ primary, O²⁻→V_O dependent) validates at the model level: both
  deltas are non-zero and opposite in sign, and the derived building block is exposed.
- [x] An isovalent doping without a dependent exchange validates.
- [x] A dependent exchange with the wrong charge sign is rejected with `LAT009`.
- [x] Doping priorities are unique, in `0..9999`, and serialize in ascending order; duplicates are `LAT006`.
- [x] A KMC simulation cannot select an MMC transition, and vice versa (`SIM002`).
- [x] All original simulation settings are represented: temperature, target MCSP, write-out count, time limit,
  min success rate, job count, RNG seed, execution flags, KMC pre-run/field/normalization/max frequency, and
  MMC break tolerance/sample length/interval/result sample.
- [x] New simulations use the documented defaults, and every hard range and recommended band has a diagnostic
  and a boundary test.
- [x] The lattice/simulation sections round-trip through the project JSON context, the canonical model, save,
  and load; dopings preserve priority order and simulations preserve kind-specific settings and authored-order
  transition arrays.
- [x] Lattice/simulation edits change `ProjectHasher.Compute` and `ComputeDocument`; no section is dropped by
  canonicalization, empty arrays are omitted, and a pre-Step-06 project keeps its canonical hash.
- [x] The generated project schema accepts the flat lattice/simulation sections and rejects unknown fields;
  fixtures cover empty and populated models.
- [x] Public API files and the ApiCompat baseline are current.
- [x] Documentation written.

## Documentation

At this point, document lattice/doping and simulation definitions:

- `docs/user-guide/lattice.md` — occupation exchanges, ordered dopings, charge compensation, and the derived
  building block. Step 10 extends this page with generation, truncation, and preview.
- `docs/user-guide/simulations.md` — MMC/KMC settings, defaults, limits, transition assignment, and
  validation.
- `docs/reference/project-format.md` — lattice/doping and simulation JSON fields, ordering, defaults, and
  validation rules.
- `docs/development/compilation-pipeline.md` — how these definitions feed compilation and Step 10 generation.
- `docs/reference/diagnostics.md` — Step 07 owns the consolidated table; Step 06 adds the `LAT`/`SIM` help
  anchors and links.

Add the new user-guide pages to `zensical.toml` navigation and the section indexes in the same step. Update the
parity matrix, status dashboard, and audit record at closeout.

## Tests

Tiers: PR-fast for unit/acceptance/contract tests; PR-medium for random property sweeps. The lattice preview and
job-count preview tests belong to Step 10; there is no Step 06 benchmark target.

| Test | Assertion | Tier |
| --- | --- | --- |
| Charge delta | Primary/dependent deltas, signs, zero-delta rejection, isovalent acceptance | PR-fast |
| Priority | Unique, `0..9999`, ascending serialization, duplicate and out-of-range diagnostics | PR-fast |
| Names | Empty/duplicate occupation-exchange and doping names | PR-fast |
| Building block | Stable site uses the first set particle; unstable site uses void; derived only | PR-fast |
| Compensation shape | Dependent without compensation, compensation without dependent, wrong sign | PR-fast |
| Simulation defaults | Every documented default, including write-out, time limit, field, normalization (0.0 canonicalizes to null), and MMC samples | PR-fast |
| Simulation limits | Boundary values for every hard range and recommended band in `SIM004`–`SIM037` | PR-fast |
| Kind compatibility | MMC/KMC transition mixing, duplicate references, and `kind`/record/array disagreement | PR-fast |
| Execution flags | CamelCase names, sorted array, `None` empty, duplicate and derived-bit rejection in the project, and the `SolverJobFlags` u32 mapping | PR-fast |
| Model envelope | Empty, lattice-only, simulation-only, and combined models round-trip through the project JSON context | PR-fast |
| Persistence/hash | Save/load preserves the sections; lattice/simulation edits change the model and document hashes; canonicalization does not drop them; empty arrays are omitted and a pre-Step-06 project keeps its hash | PR-fast |
| Canonical equivalence | `minSuccessRate` 0 and null, `rngSeed` empty and null, and `normalizationEnergyEv` 0.0 and null produce the same canonical model/document hash | PR-fast |
| Schema | Generated project schema accepts the lattice/simulation sections and rejects unknown fields; empty and populated fixtures | PR-fast |
| Ordering | Doping priority and authored-order transition arrays survive serialization; no silent re-sort | PR-fast |
| Queries | `Project` implements `ILatticeQuery`/`ISimulationQuery` through `IProjectQuery`; default building block matches the model | PR-fast |
| Editing | Reorder/duplicate/remove/switch-kind keeps references valid or reports dependents | PR-fast |
| Diagnostics | Positive/negative case for every `LAT001`–`LAT011` and `SIM001`–`SIM037` code | PR-fast |
| Property tests | Random valid doping/simulation models round-trip canonically and preserve order | PR-medium |
| Samples | Ceria reduction, BZO doping, KMC, and MMC samples validate | PR-fast |

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Canonicalization drops lattice/simulation data | Explicit `CanonicalizeModel` extension, save/load and document-hash tests |
| Defaults or limits drift from the original | Documented defaults/ranges, boundary tests, and the original-reference table |
| Charge compensation reduces fractions unexpectedly | Step 10 preview and report; Step 06 validates only the sign/zero invariants |
| Multiple dopings on the same site interact | Step 10 applies them in priority order and reports zero/truncated applications; Step 06 rejects only unknown references and duplicate priorities |
| Simulation settings drift from job templates | Single inheritance model; job templates only override; Step 10 owns effective counts |
| Field/units confusion | Explicit unit suffixes, fractional direction normalization in the solver, and boundary tests |
| Two lattice generators diverge | Step 06 defines no generator; Step 10 owns the single generator and preview |
| Transition-list order changes silently | Authored-order persistence, no canonical re-sort, and an order-preservation test |
| Manifest/schema shape drifts | Flat DTO fields, manifest status/notes/fixtures, generated-schema freshness check |

## Done when

The complete lattice/doping and simulation definitions can be authored, validated, persisted, queried, and
consumed by the Step 10 job builder, with documented defaults and limits.
