# Step 06 — Lattice and Simulation Models

**Phase:** B. Model definition  
**Depends on:** 03, 04
**Delivers:** Occupation exchanges, ordered dopings, and MMC/KMC simulation definitions with defaults and
validation.

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

Complete the user-authored model: describe how supercells are doped and which transitions/settings define each
simulation. These definitions feed step 10 (job expansion and supercells) and step 08 (simulation encoding).

## Original reference

- Lattice model:
  - `IBuildingBlock` — default cell entries (implicitly the first particle of each stable site and void on
    unstable sites).
  - `IDoping` — primary and counter doping, building block, priority, `UseCounterDoping`.
  - `IDopingCombination` — dopant, dopable, site, charge delta.
  - `FastDopedByteLatticeSource` builds `byte[a,b,c,P]` lattices with an occupation roll table.
  - Doping fractions are applied in ascending priority; each applies to the remaining particles.
  - Counter-doping must have the opposite charge delta.
- Simulation model:
  - `ISimulation` — custom RNG seed, temperature, target MCSP, write-out count, job count, run time limit,
    lower success rate limit.
  - `IKineticSimulation` adds pre-run MCSP, electric field vector/magnitude, normalization energy, transitions.
  - `IMetropolisSimulation` adds break tolerance, break sample length/interval, result sample MCSP,
    transitions.
  - Limits: temperature 0.1–10000 K, MCSP 1–1,000,000, job count 1–1000, supercell positions 1–500,000,
    cells per direction 1–256, transitions 0–100.
  - Empty RNG seeds are replaced by a GUID.
- Known issues: the default building block is read-only and implicit; doping integer truncation must account
  for dependent-exchange ratios; charge compensation can reduce the applied fraction below the requested value.

## Design

### 1. Lattice model

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
```

- The default building block is derived, not stored:
  - stable site: first particle of its particle set;
  - unstable site: void.
- `OccupationExchange`:
  - `SiteId` must be a stable site;
  - `SourceParticleId` must be in the site's particle set and must equal the current default occupation at the
    time the exchange is applied;
  - `TargetParticleId` must be in the site's particle set and differ from the source;
  - the exchange is a reusable pool item; it has no effect until referenced by a doping.
- `Doping`:
  - `Priority` is unique; ties are rejected (deterministic application order).
  - If `ChargeCompensation` is false, `DependentExchangeId` must be null or ignored.
  - If true, the dependent exchange must have a charge delta opposite to the primary exchange, and the ratio
    must be rational and realizable in the supercell.
  - The primary exchange defines the requested fraction; the dependent exchange is applied automatically to
    maintain charge neutrality.
  - A doping with no dependent exchange is allowed for isovalent substitutions.
- The lattice preview shows, for a given supercell size and fraction:
  - requested vs. actual replaced counts,
  - dependent count and ratio,
  - remaining particles,
  - total charge before/after (must be neutral within tolerance),
  - which sites are affected.

### 2. Simulation model

```csharp
public abstract record Simulation(
    SimulationId Id,
    string Name,
    SimulationKind Kind,
    double TemperatureK,
    long TargetMcsp,
    long? TimeLimitSeconds,
    double? MinSuccessRate,
    int JobCount,
    string? RngSeed,
    ExecutionFlags Flags,
    string? Instruction,
    ImmutableArray<TransitionRef> TransitionIds);

public sealed record KineticSimulation(
    ... : Simulation,
    long PreRunMcsp,
    double ElectricFieldModulusVm,
    Fractional3 ElectricFieldDirection,
    double? NormalizationEnergyEv,
    double? MaxAttemptFrequencyHz) : Simulation;

public sealed record MetropolisSimulation(
    ... : Simulation,
    double BreakTolerance,
    long BreakSampleLengthMcsp,
    long BreakSampleIntervalMcsp,
    long ResultSampleMcsp) : Simulation;
```

- `Kind` is `Mmc` or `Kmc`; a simulation may reference only transitions of the matching kind.
- `TargetMcsp` is the number of successful steps per mobile particle for the main run.
- KMC `PreRunMcsp` is the relaxation/normalization pre-run.
- `ElectricFieldDirection` is a fractional vector; the solver normalizes it to Cartesian. A zero vector with a
  non-zero modulus is invalid.
- `NormalizationEnergyEv` null means automatic normalization; a value enables fixed normalization.
- `MaxAttemptFrequencyHz` null means use the maximum from the transition rules; a value rescales all
  frequencies while preserving ratios.
- `RngSeed` is optional; empty means derive from the project/job seed.
- `Flags` include `NoSaving`, `NoJumpLogging`, `UseFastExp`, `Deterministic`.
- `Instruction` is an extension/routine instruction (MMCFE); the structured `extensions` configuration is a
  job-template override (Steps 10/27) and is preferred, but the string is preserved for advanced/legacy jobs.
- Defaults (matching the original user experience):
  - temperature 1000 K;
  - target MCSP 1000;
  - job count 12;
  - KMC pre-run MCSP 100;
  - MMC break tolerance 0, sample length/interval derived from MCSP, result sample 100.
- Multiple simulations may share transitions; a transition can be active in several simulations.
- Simulation names are unique within the project.

### 3. Job-count relationship

- The simulation's `JobCount` is the default number of duplicate jobs per configuration.
- A job template collection may override it (`jobCountPerConfig`).
- The build preview shows the effective count and total jobs.
- The hard limit is 99,999 jobs per package (the original `.msl` limit); the new format is not limited by
  that number, but the UI warns above 10,000.

### 4. Validation rules

| Code | Severity | Rule |
| --- | --- | --- |
| `LAT001` | Error | Occupation exchange references unknown site/particle |
| `LAT002` | Error | Site is unstable |
| `LAT003` | Error | Source particle not in the site's set |
| `LAT004` | Error | Target particle not in the site's set |
| `LAT005` | Error | Source equals target |
| `LAT006` | Error | Doping priority duplicate |
| `LAT007` | Error | Doping references unknown exchange |
| `LAT008` | Error | Dependent exchange without charge compensation |
| `LAT009` | Error | Dependent exchange does not compensate charge |
| `LAT010` | Warning | Doping fraction cannot be realized exactly in common supercells |
| `LAT011` | Error | More than one doping applies to the same site with an incompatible order |
| `SIM001` | Error | Simulation references unknown transition |
| `SIM002` | Error | MMC simulation references a KMC transition (or vice versa) |
| `SIM003` | Error | Simulation has no transitions |
| `SIM004` | Error | Temperature outside 0.1–10000 K |
| `SIM005` | Error | Target MCSP outside 1–1,000,000 |
| `SIM006` | Error | Job count outside 1–1000 |
| `SIM007` | Error | KMC field direction is zero while modulus is non-zero |
| `SIM008` | Error | Pre-run MCSP is zero for KMC (unless explicitly disabled) |
| `SIM009` | Warning | Simulation name duplicate |
| `SIM010` | Warning | Estimated supercell positions exceed 500,000 |
| `SIM011` | Warning | Requested MCSP/duplicate combination is very large |

### 5. Editing operations

- Add/update/remove occupation exchanges and dopings.
- Reorder doping priorities with a single operation that renumbers deterministically.
- Add/update/remove simulations; switch kind is a delete+create with reference checks.
- Assign transitions through a multi-select view model.
- Duplicate a simulation with new IDs and the same settings.

## Implementation tasks

- [ ] Implement `OccupationExchange`, `Doping`, and derived default building block.
- [ ] Implement charge-delta calculation and rational compensation validation.
- [ ] Implement lattice preview with requested vs. actual counts and charge balance.
- [ ] Implement `Simulation`, `KineticSimulation`, `MetropolisSimulation`, and `ExecutionFlags`.
- [ ] Implement transition-kind compatibility and reference validation.
- [ ] Implement defaults and editor operations.
- [ ] Add ceria reduction/hydration and barium-zirconate doping samples.
- [ ] Add KMC/MMC simulation samples.
- [ ] Add diagnostics/help text.

## Acceptance criteria

- [ ] The ceria reduction doping (Ce⁴⁺→Ce³⁺ primary, O²⁻→V_O dependent) validates and previews charge
  neutrality.
- [ ] An isovalent doping without a dependent exchange validates.
- [ ] A dependent exchange with the wrong charge sign is rejected.
- [ ] Doping priorities are unique and applied in ascending order.
- [ ] A KMC simulation cannot select an MMC transition.
- [ ] All original simulation settings are represented and validated.
- [ ] Documentation written

## Documentation

At this point, document lattice/doping and simulation definitions:

- `docs/user-guide/lattice.md` — occupation exchanges, ordered dopings, charge compensation.
- `docs/user-guide/simulations.md` — MMC/KMC settings, defaults, transition assignment.
- `docs/reference/project-format.md` — doping/simulation JSON fields and validation rules.
- `docs/development/compilation-pipeline.md` — how these definitions feed compilation.

## Tests

| Test | Assertion |
| --- | --- |
| Charge delta | Primary/dependent ratios and signs |
| Truncation preview | Requested vs. actual counts for small supercells |
| Priority | Deterministic application order |
| Simulation limits | Boundary values for temperature, MCSP, job count, field |
| Kind compatibility | MMC/KMC transition mixing rejected |
| Defaults | New simulation matches documented defaults |
| Editing | Reorder/duplicate/remove keeps references valid |

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Charge compensation reduces fractions unexpectedly | Preview before build; explain rounding in the UI |
| Multiple dopings on the same site interact | Validate order and source occupation; preview the sequence |
| Simulation settings drift from job templates | Single inheritance model; job templates only override |
| Field/units confusion | Unit types and explicit field direction normalization |

## Done when

The complete lattice/doping and simulation definitions can be authored, previewed, validated, and used by the
job builder.
