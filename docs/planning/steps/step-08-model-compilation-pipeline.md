# Step 08 — Model Compilation Pipeline

**Phase:** C. Compilation  
**Depends on:** 02–07
**Delivers:** A pure, staged compiler that turns the validated model into a P1-extended, symmetry-reduced,
densely indexed `CompiledModel` with environments, pair/cluster interactions, transition rules, and simulation
encoding.

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

This is the scientific heart of the model builder. The compiler must be deterministic, explainable, and
independently testable. It consumes an immutable `Project` and produces a `CompiledModel` that can be
serialized into a simulation package; it performs no I/O and has no UI dependency.

## Original reference

- Two stages existed:
  1. lazy, event-invalidated cache ports inside `Mocassin.Model` (P1 lists, encoders, pair finder, group
     analyzer, rule setters);
  2. `Mocassin.Model.Translator`’s `ProjectModelContextBuilder`, which built Structure/Energy/Transition/
     Simulation contexts in parallel and linked them.
- The translator assigned dense `ModelId` indices and encoded arrays for the C solver.
- Key algorithms:
  - `SpaceGroupService.GetUnitCellP1PositionExtension` / path extension;
  - `PairInteractionFinder` (radial search, distance buckets, center-stabilizer `G0` reduction, chiral
    partners);
  - `GeometryGroupAnalyzer` / `PointOperationGroup.GetUniquePermutations`;
  - `TransitionRuleGenerator` (donor/acceptor permutation, inverse/dependent rules, conservation);
  - energy table expansion and jump/mapping/tracker tables.
- Known performance issues: repeated point-group construction per pair, linear `Single` lookups in group
  table creation, O(R²) inverse/dependent pairing, whole-cache invalidation.
- Known defects: conflict handling after mutation, ambiguous end-state selection, unused settings.

## Design

### 1. Pipeline stages

```csharp
public interface IModelCompiler
{
    CompilationResult Compile(Project project, CompilationOptions options, CancellationToken ct);
}
```

Stages:

1. **Resolve** — map IDs to typed objects, detect dangling references (normally already validated).
2. **Structure** — P1-expand positions, build the canonical encoder, precompute Cartesian positions, cell
   volume, and site orbits.
3. **Environments** — sample periodic images within each cutoff, apply filters, bucket by distance, and
   produce pair interaction geometry.
4. **Pair reduction** — reduce candidates by the center stabilizer `G0`, assign stable geometry IDs, detect chiral
   partners, and build pair interaction sets.
5. **Clusters** — expand cluster geometry, compute the point group, enumerate/canonicalize occupation
   permutations, and build cluster interaction sets.
6. **Transition rules** — enumerate state-change combinations, build S0/S1/S2, apply mass/charge conservation,
   generate forward/inverse/dependent rules, and deduplicate.
7. **Simulation encoding** — build jump collections, directions, rules, field factors, jump count/mapping
   tables, and tracker mappings for each MMC/KMC simulation.
8. **Indexing** — assign dense, deterministic indices to particles, positions, interactions, rules,
   collections, directions, and trackers.
9. **Signing** — compute the model signature from canonical compiled content.
10. **Report** — return the compiled model plus counts, warnings, timings, and diagnostics.

Stages are pure functions over immutable inputs. Expensive stages may run in parallel where they are
independent, but the output order is fixed.

### 2. Compiled model shape

```csharp
public sealed record CompiledModel(
    int FormatVersion,
    string ModelSignature,
    string SymmetryContractVersion,
    string SpaceGroupDataVersion,
    string SpaceGroupDataSha256,
    CompiledParticleModel Particles,
    CompiledStructureModel Structure,
    CompiledEnergyModel Energy,
    CompiledTransitionModel Transitions,
    ImmutableArray<CompiledSimulation> Simulations);
```

- `Particles`: charges, symbols, vacancy flags, count; index 0 is void.
- `Structure`: cell vectors, P1 positions, site mapping, environments, interaction range, tracker counts.
- `Energy`: pair tables, cluster tables, defect/site background.
- `Transitions`: jump collections, directions, rules, jump count/mapping, tracker mapping.
- `Simulations`: kind, active collections, default settings, tracker models.
- `SymmetryContractVersion`, `SpaceGroupDataVersion`, and `SpaceGroupDataSha256` record the algorithms and
  catalog used to produce the model. They are part of the canonical compiled projection used for the model
  signature.
- The serialized form is defined in [`02-data-contracts.md`](../02-data-contracts.md).

### 3. Indexing rules

- `ParticleIndex`: 0 = void, then particles sorted by `ParticleId` (stable) or by user order? Choose **sorted by
  ParticleId** for determinism; the UI shows the resulting index. The original used insertion order; clean
  break allows a canonical rule.
- `PositionIndex`: canonical P1 order from step 02.
- `SiteIndex`: reference sites sorted by `CellSiteId`; used for environment definitions.
- `PairInteractionIndex`: canonical shell order `(distanceBucket, targetP, cellA, cellB, cellC)`, then site
  indices. Stable geometry IDs are content hashes used for parameterization matching, not table ordering.
- `ClusterIndex`: cluster ID order.
- `TransitionIndex`: transitions sorted by ID within kind.
- `JumpCollectionIndex`: one per KMC transition, in transition index order.
- `JumpDirectionIndex`: collection order, then path order.
- `RuleIndex`: collection order, then canonical state triplet order.
- `GlobalTrackerIndex`: (collection, particle) in ascending indices.
- `StaticTrackerIndex`: (position, particle) per cell, in ascending position/particle order.
- All indices are dense from 0; no gaps; no reuse across compilations.
- Freeze post-compilation lookup maps as `FrozenDictionary`/`FrozenSet`; never mutate compiled arrays after signing.
- The model signature includes the indexing rules and all geometry; any change invalidates parameterizations.

### 4. Pair interaction compilation

- For each stable site (center) and each periodic image within the cutoff:
  - use the Step 02 `RadialSearch` and minimum-image policy;
  - compute distance and partner position;
  - apply filters;
  - bucket by the `1 fm` distance bucket with the cutoff `+ 1 fm` margin;
  - assign a candidate geometry.
- Reduce candidates by the center stabilizer `G0`:
  - sort by `(distanceBucket, targetP, cellA, cellB, cellC)`;
  - take the first unassigned candidate as the shell representative;
  - mark every candidate in the same distance bucket and the same `G0` orbit as equivalent;
  - assign a new `PairInteractionId`.
- Build a symmetric pair set for stable-stable interactions; ensure `A-B` and `B-A` map to one table unless
  chiral.
- Chirality:
  - compute the inverse geometry `B-A` with the opposite periodic offset;
  - if the inverse is not in the same `G0` orbit, the pair is chiral;
  - link both directions to one energy table and one parameterization set;
  - the compiled model records the chiral partner index;
  - this is an orbit relationship, not physical chirality; in P1 both directions are separate orbits.
- Unstable-stable pairs are compiled per unstable environment and are asymmetric by nature.
- Each pair set carries:
  - `CenterSiteIndex`, `PartnerSiteIndex`;
  - reference offset vector and distance;
  - `DistanceBucketFm`;
  - `ChiralPartnerId` (or none);
  - multiplicity (`EquivalentModels` count);
  - allowed particle permutations and the particle-table mapping.
- Pair energy tables are `n_particles × n_particles` in eV; entries not valid for the site pair are zero and
  flagged.

### 5. Cluster compilation

- Expand the cluster geometry by the space group.
- For each unique geometry, build the `PointGroup` at the center: `G0`, `Gset`, `Gseq`, and `P`.
- Enumerate allowed occupation states from the center and surrounding particle sets.
- Canonicalize under `P`; keep the lexicographically smallest tuple in site order and record every equivalent
  tuple/permutation in the expansion map.
- Build a cluster table:
  - occupation codes: 6 bits per site, site 0 in the least significant bits, up to 9 sites (54 bits), unused
    high bits zero;
  - particle-table mapping for the center particle;
  - energy values (default 0, parameterized later).
- Preserve chiral distinctions; do not merge mirror tuples unless the mirror permutation is in `P`.
- Clusters only contribute to the center site; this is documented in the energy engine.
- Cluster tables are indexed by a canonical order (center site, geometry hash, occupation code).

### 6. Transition rule generation

For each transition:

1. Determine the position path from the reference geometry and symmetry extension.
2. For each state-change group, enumerate the possible start occupations:
   - donor particles are the primary start options;
   - acceptor particles may be start options for reverse/dependent rules;
   - the void particle is allowed only where the position is empty in that state.
3. Build the S0 occupation code:
   - stable path positions receive one start particle from their group;
   - unstable path positions are **void** in S0, and their group must be a `[D,∅]` pair.
4. Build S2 by applying the state change at each group; unstable path positions remain **void** in S2.
5. Build S1:
   - stable positions are **void**;
   - unstable positions are occupied by the group's transition species (the donor or a designated
     pseudoparticle);
   - MMC has no S1.
6. Apply conservation:
   - mass: the multiset of particle symbols must be preserved (void ignored);
   - charge: the total charge of S0 must equal S2 within tolerance;
   - charge-transfer transitions may change the formal charge carrier but must conserve total charge;
   - reject combinations that violate either rule, and count them in the report.
7. Generate the forward rule.
8. Generate inverse rules only for `Metropolis2` and `Migration3` (swap S0/S2 and reverse the path).
9. Generate dependent rules for symmetry-equivalent path directions that are not explicitly listed; record
   their mapping.
10. Deduplicate by `(state0, state1, state2, path direction)`.
11. Assign a stable rule ID derived from the canonical state triplet and direction.
12. Attach the default attempt frequency (1e13 Hz) and field factor; parameterization overrides the frequency.

- Rule lookup in the solver is by `StateCode0`; the compiler must ensure that for a given position/particle
  and selected direction there is at most one matching rule, or that the solver can resolve ambiguity
  deterministically. Ambiguity is a compile error.
- The compiler records:
  - total raw combinations;
  - rejected by mass/charge;
  - unique forward rules;
  - inverse rules;
  - dependent rules;
  - rules per collection.
- The original's `TransitionRuleGenerator` is O(R²) for inverse/dependent pairing; the new compiler should use
  hash maps keyed by state triplet and path direction.

### 7. Simulation encoding

For each KMC simulation:

- **Jump collections**: one per transition; mobile particle mask; rule list; direction list.
- **Jump directions**:
  - position ID;
  - relative path as `Vector4I` offsets;
  - movement sequence in fractional coordinates for tracking;
  - electric-field projection factor.
- **Rules**:
  - state codes S0/S1/S2;
  - frequency factor (relative to the simulation's maximum);
  - field factor;
  - optional static S2 correction (NaN when dynamic);
  - tracker order code.
- **Jump count table** `[position][particle]`: number of selectable directions.
- **Jump mapping** `[position][particle][relative]`: direction index.
- **Static tracker mapping** `[position][particle]`: tracker offset.
- **Global tracker mapping** `[collection][particle]`: tracker index.
- Selection optimizers remove `(position, particle)` start options and adjust the pool counts at runtime.

For MMC:

- Jump collections contain the 2-site exchange directions.
- No field, pre-run, or unstable-state handling.
- Partner cell selection is uniform over the supercell, matching the original algorithm.

### 8. Energy background

- Site energies are compiled into a `[position][particle]` defect background in eV.
- The per-job lattice may add a supercell-specific background (e.g. from recycled lattices).
- Pair/cluster tables are stored once per model and shared across jobs.
- The energy engine converts eV to kT at runtime using the job temperature.

### 9. Signature and caching

- `ModelSignature = SHA-256("mocassin/model-signature/v1" || 0x00 || canonical MessagePack projection)` over
  the compiled model without parameterization values, including the space-group catalog data version,
  catalog checksum, and symmetry contract version (ADR 0010).
- `ParameterizationSignature = SHA-256("mocassin/parameterization-signature/v1" || 0x00 || projection)` over
  the model signature plus canonical energy/frequency values.
- The compiler caches stage results keyed by content hashes:
  - structure hash → P1 expansion;
  - energy model hash + structure hash → pair/cluster geometry;
  - transition model hash + structure hash → rules;
  - simulation hash + previous → encoding.
- Cache entries are immutable and thread-safe. Editing one domain invalidates only dependent stages.
- Compilation of the ceria sample should be < 1 s; a 10k-position model should be < 30 s on a laptop.

### 10. Diagnostics and report

`CompilationResult` includes:

- compiled model (on success);
- diagnostics (errors/warnings with stage and path);
- counts and timings per stage;
- a human-readable summary suitable for the CLI and GUI.

Compilation errors are distinct from validation errors; e.g. "rule ambiguity", "too many permutations",
"mass conservation leaves no valid rules".

## Implementation tasks

- [ ] Implement compiler interfaces, stage contracts, and cancellation.
- [ ] Implement structure/P1 compilation.
- [ ] Implement environment sampling and pair reduction with chiral linking.
- [ ] Implement cluster permutation compilation.
- [ ] Implement transition rule generation, conservation, inverse/dependent rules.
- [ ] Implement simulation encoding and tracker mappings.
- [ ] Implement deterministic indexing and signatures.
- [ ] Implement stage caches keyed by content hashes.
- [ ] Implement `mocassin compile` to dump the compiled model as JSON for inspection.
- [ ] Add golden compiled models for ceria, barium zirconate, vehicle, and interstitialcy.
- [ ] Add brute-force oracles for pair/cluster/rule counts.

## Acceptance criteria

- [ ] The ceria model compiles to the expected P1 positions, pair sets, cluster sets, and oxygen migration
  rules.
- [ ] A 2-site MMC model compiles forward and inverse rules.
- [ ] A 3-site migration compiles forward and inverse (including chiral) rules.
- [ ] A 5-site vehicle compiles unidirectional rules only.
- [ ] Rule generation enforces unstable positions void in S0/S2 and stable positions void in S1.
- [ ] Mass and charge conservation reject invalid combinations and report counts.
- [ ] Rule ambiguity is detected and reported.
- [ ] Compilation is deterministic: same project → byte-identical canonical compiled model.
- [ ] Stage caches invalidate correctly after edits.
- [ ] Ceria compilation is < 1 s; a 10k-position model is < 30 s.
- [ ] Documentation written

## Documentation

At this point, document the compiler:

- `docs/development/compilation-pipeline.md` — stages, P1 expansion, pair/cluster reduction, rule generation, encoding.
- `docs/physics/energies.md` and `docs/physics/symmetry.md` — the scientific rules the compiler applies.
- `docs/development/architecture.md` — compiled model boundaries and deterministic indexing.
- `docs/glossary/notation.md` — compiled indices, state codes, rule IDs.

## Tests

| Test | Assertion |
| --- | --- |
| Rule property | Random valid models conserve mass/charge and produce unique, index-valid rules |
| Cache property | Editing one domain rebuilds only dependent stages and keeps unrelated signatures stable |
| P1 positions | Match step 02 encoder for all samples |
| Pair sets | Counts/distances/multiplicities match brute force |
| Pair bucketing | Symmetry-equivalent pairs share a bucket; cutoff-boundary pairs are included with the 1 fm margin |
| Pair order | Pair tables follow the canonical shell order and are stable across recompilation |
| Contract metadata | Symmetry and catalog versions are present and included in the model signature |
| Chirality | Known chiral pairs linked; achiral pairs symmetric |
| Clusters | Unique permutations match brute-force canonicalization |
| Rules | State triplets, stability roles, conservation, inverse/dependent counts |
| Vehicle | Unidirectional only; no inverse generation |
| Encoding | Jump count/mapping/tracker tables consistent |
| Determinism | Byte-identical compiled model across runs/OSes |
| Cache | Editing structure/energy/transition invalidates only affected stages |
| Performance | Ceria and large-model budgets |

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Rule generation combinatorics explode | Hard limits, early conservation checks, progress/cancellation |
| Ambiguous rules | Compile-time uniqueness check; error with the conflicting triplets |
| Pair/cluster reduction bugs | Brute-force oracles and golden models |
| Chiral handling | Explicit tests and a single linked energy table |
| Cache invalidation bugs | Content hashes per stage; tests that edit one domain at a time |
| Static S2 optimization | Start with dynamic S2; add static corrections only after correctness is proven |

## Done when

The compiler produces a deterministic, validated `CompiledModel` for every supported mechanism, with counts
and diagnostics that explain what was generated and why.
