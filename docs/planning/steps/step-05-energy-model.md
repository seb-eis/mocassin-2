# Step 05 — Energy Model

**Phase:** B. Model definition  
**Depends on:** 03, 04
**Delivers:** Stable/unstable interaction environments, cutoff filters, multi-body clusters, and site energies,
with validation and cost previews.

## Implementation status

- **Status:** In progress
- **Step branch:** `step/05-energy-model`
- **Step CI:** —
- **Main merge:** —
- **Step tag:** —
- **Audit:** Not requested
- **Design review:** [Ready for implementation — accepted at `0b2a571`](../reviews/step-05.md)
- **Feature list:** Approved 2026-11-09
- **Last updated:** 2026-11-09

| Feature | Branch | Status | Local gates | Step CI | Squash commit |
| --- | --- | --- | --- | --- | --- |
| `energy-model-and-persistence` | `feature/step-05-energy-model/energy-model-and-persistence` | Awaiting approval | `verify.sh dotnet`, `structure`, `api` pass | — | — |
| `core-validation` | `feature/step-05-energy-model/core-validation` | Awaiting approval | `verify.sh dotnet`, `structure` pass | — | — |
| `symmetry-geometry-and-validation` | `feature/step-05-energy-model/symmetry-geometry-and-validation` | Not started | — | — | — |
| `queries-and-editing` | `feature/step-05-energy-model/queries-and-editing` | Not started | — | — | — |
| `energy-preview-and-benchmark` | `feature/step-05-energy-model/energy-preview-and-benchmark` | Not started | — | — | — |
| `contracts-and-fixtures` | `feature/step-05-energy-model/contracts-and-fixtures` | Not started | — | — | — |
| `samples-and-docs` | `feature/step-05-energy-model/samples-and-docs` | Not started | — | — | — |
| `closeout` | `feature/step-05-energy-model/closeout` | Not started | — | — | — |

## Goal

Let a user define the interaction model that the compiler will sample into pair and cluster tables. The
definition is geometric and symbolic; energy values are supplied later by parameterization templates.

## Non-goals

- Compiled pair/cluster tables, occupation codes, and canonical indices. Those are Step 08.
- Parameterization values, template creation, and chiral value binding. Those are Step 09.
- The solver energy engine and total-lattice-energy convention. Those are Step 15.
- Lattice, doping, and simulation definitions. Those are Step 06.
- Energy, cluster, and site-energy GUI editors. Those are Step 25.
- Emission and consolidated documentation of the cross-domain `ENM018` KMC path warning. That is Step 07.
- Legacy `.mocprj`/`.msl`/`.mcs` compatibility. ADR 0001 keeps the clean break.

## Original reference

- `IStableEnvironmentInfo`: maximum interaction range, radial filters, and defect (site) energies. Default
  range 1.0 Å.
- `IUnstableEnvironment`: one per unstable site, with its own range, filters, pair interactions, and group
  interactions.
- `IInteractionFilter`: center/partner site, start/end radius, applicability.
- Pair interactions are **auto-sampled** from the environment; users do not add them directly. Each pair has a
  distance, chirality flag, chiral partner, and energy entries.
- `IGroupInteraction`: center site plus 2–8 surrounding offsets (3–9 positions total),
  symmetry-reduced occupation-state energies. `GroupSize` counts the surrounding positions, not the center.
- `IDefectEnergy`: site × particle × energy in eV. These are the site energies.
- Limits: positions per stable environment warn 500 / max 5000; per unstable environment warn 100 / max 1000;
  group permutation count warn 500 / max 5000; pair/group energy −100..100 eV; group size 2..8.
- Known issue: pair interactions between distinct sublattices can require asymmetric `A-B` vs `B-A` values,
  but the original forces them to be symmetry-equivalent. The new model must make chirality explicit.
- Known issue: the solver's total lattice energy is half the sum of position energies, valid only for pair
  interactions; cluster contributions need a consistent counting convention. The new solver must document and
  test its convention.

## Design

### 1. Model composition and persistence

`ModelDefinition` gains the flat energy sections alongside the Step 03/04 particle, structure, and transition
sections:

```csharp
public sealed record ModelDefinition
{
    // Step 03/04 sections omitted here.
    StableEnvironment StableEnvironment { get; init; }
    ImmutableArray<UnstableEnvironment> UnstableEnvironments { get; init; }
    ImmutableArray<Cluster> Clusters { get; init; }
    ImmutableArray<SiteEnergySet> SiteEnergies { get; init; }
}
```

- The model stays flat on disk: `model.stableEnvironment`, `model.unstableEnvironments`, `model.clusters`, and
  `model.siteEnergies`.
- `ModelDefinitionJsonConverter` reads and writes the new sections. `ProjectJsonContext` is an internal nested
  type in `Project/ProjectJson.cs`, not a separate file; the serializer context is extended there.
- `ProjectJson.CanonicalizeModel` is extended so the energy sections survive every canonical write. It must:
  - replace a default/absent `StableEnvironment` with `StableEnvironment.Default`;
  - normalize default `ImmutableArray` values to empty arrays;
  - sort `UnstableEnvironments` by `SiteId`, and each environment's filters by
    `(Start, End, SiteAId, SiteBId)`;
  - sort `Clusters` by `Id`, preserving each `OffsetVectors` definition order;
  - sort `SiteEnergies` by `SiteId`, and each set's `Entries` by `ParticleId`.
  `ProjectJsonConverter.Write` already replaces the model with `CanonicalizeModel`, and `ProjectStore.SaveAsync`
  uses that converter, so the energy sections are persisted rather than dropped.
- `ProjectHasher.Compute` (model hash) and `ProjectHasher.ComputeDocument` therefore include the energy
  sections automatically. A new `ProjectHasher.ComputeEnergy(ModelDefinition model)` hashes only the canonical
  energy projection (default environment, unstable environments, clusters, and site energies). It is the
  producer for Step 08's “energy model hash + structure hash” cache key and is invariant to structure and
  transition edits.
- A missing `stableEnvironment` loads `StableEnvironment.Default`: cutoff `1.0 Å`, no filters. The default is
  omitted on write when the value equals the default, and the empty `unstableEnvironments`, `clusters`, and
  `siteEnergies` arrays are omitted when empty. This follows the Step 04 rule that empty optional sections are
  omitted, and it keeps existing project and document hashes unchanged for models that author no energy data.
- Semantic array order:
  - `Cluster.OffsetVectors` — definition order relative to the center; never re-sorted.
  All other collections are sorted when serialized:
  - `UnstableEnvironments` by `siteId`;
  - `Clusters` by `id`;
  - `SiteEnergies` by `siteId`;
  - `InteractionFilter` entries by `(start, end, siteAId, siteBId)`;
  - `SiteEnergySet.Entries` by `particleId`.
- The project `schemaVersion` remains 1 because the energy sections are additive and the project format is
  still pre-release. This follows the Step 03/04 additive-v1 precedent. ADR 0009's version-bump rule is not
  waived generally: the manifest `status`/`notes` are updated, the generated schema and fixtures are refreshed,
  and the first released format will start the normal version-bump discipline.
- Pair interactions, cost previews, and derived chiral links are computed data and are never persisted.

### 2. Environments

```csharp
public sealed record StableEnvironment(
    LengthAngstrom Cutoff,
    ImmutableArray<InteractionFilter> Filters);

public sealed record UnstableEnvironment(
    CellSiteId SiteId,
    LengthAngstrom Cutoff,
    ImmutableArray<InteractionFilter> Filters);

public sealed record InteractionFilter(
    LengthAngstrom Start,
    LengthAngstrom End,
    CellSiteId SiteAId,
    CellSiteId SiteBId);
```

- One stable environment defines the sampling range for all stable sites. `StableEnvironment.Default` is
  cutoff `1.0 Å`, no filters.
- Each unstable site has an optional environment; if omitted, it inherits the stable cutoff but samples
  unstable-stable pairs only.
- `Cutoff` must be positive and finite for **every** environment (stable and unstable). A cutoff above half
  the smallest cell dimension is a periodic-ambiguity warning, not an error.
- A filter removes pair interactions whose center-to-partner distance is inside the inclusive range
  `[Start, End]`. Boundary comparisons use `GeometryTolerances.DistanceEqualityTolerance` (1e-5 Å), matching
  the original inclusive `AlmostEqualByRange` behavior. `Start` must be non-negative and `End` must not exceed
  the owning environment's cutoff.
- Stable-environment filters are symmetric: `(SiteA, SiteB)` removes both `A→B` and `B→A`. Unstable-environment
  filters are one-directional, matching the original `UnstableInteractionFilter`: `SiteAId` must equal the
  environment's `SiteId` (the center is implicit) and `SiteBId` is the partner. The same `InteractionFilter`
  record stores both cases.
- Filters must lie within the cutoff; overlapping filters are allowed but redundant ones warn. A filter is
  redundant when it is identical to an earlier filter after canonical sorting.
- Filter order is not semantic; filters serialize in the canonical `(Start, End, SiteAId, SiteBId)` order.
- Filters are part of the model signature; changing one invalidates pair/cluster parameterizations.

### 3. Clusters

```csharp
public sealed record Cluster(
    ClusterId Id,
    string Name,
    CellSiteId CenterSiteId,
    ImmutableArray<Fractional3> OffsetVectors);
```

- The center is an existing stable or unstable reference site; an unstable center requires its unstable
  environment. Only the surrounding positions must be stable. Offsets are absolute fractional vectors
  relative to the center. `OffsetVectors` contains the **surrounding** positions only.
- Between 2 and 8 offsets (3–9 positions including the center), matching the original `AtomsPerGroup` 2..8
  setting. One offset is rejected as a degenerate cluster; more than eight is rejected because the 6-bit
  occupation code supports at most nine sites.
- Offsets may be outside `[0,1)`.
- Periodic identity uses the Step 02 `UnitCellVectorEncoder`, not `Fractional3.TrimToUnitCell()`. Each offset
  is resolved to exactly one encoded position `(cellA, cellB, cellC, P)` with the Step 04 minimizing-shift rule
  (ties toward zero). Two offsets are duplicates when their full encoded positions are equal.
- `ENM009` rejects duplicate surrounding offsets. `ENM011` rejects center-equivalent positions and any other
  duplicate in the full geometry (center + offsets), including a zero offset or an offset that resolves to the
  center's encoded position. The two codes do not double-report: `ENM009` owns a surrounding-surrounding
  duplicate pair, and `ENM011` adds the center-involving cases. This is the operational definition of the
  original “ring” check (`GeometryGroupAnalyzer.ContainsRing` compares every pair of full geometry positions);
  a star of distinct positions is valid.
- Every surrounding position must resolve to an existing **stable** site and must not be removed by a filter
  of the center's environment. Unresolvable, unstable, or filtered surrounding positions are `ENM010`
  geometry errors, matching the original `GroupGeometryValidity` checks.
- The maximum minimum-image distance from the center to every offset must not exceed the center environment's
  cutoff (`ENM010`).
- The cluster is symmetry-expanded as an entity; permutations related by the induced permutation group `P` share
  one energy.
- A cluster only contributes to the energy of its center site. To model the same cluster from another
  surrounding position, define another cluster with that position as center.

### 4. Site energies

```csharp
public sealed record SiteEnergySet(
    CellSiteId SiteId,
    ImmutableArray<SiteEnergyEntry> Entries);

public sealed record SiteEnergyEntry(
    ParticleId ParticleId,
    EnergyEv Energy);
```

- Default is 0 eV for every valid particle/site combination.
- Entries are sparse; missing entries mean zero.
- A `SiteEnergySet` contains at most one entry per `ParticleId`. A duplicate `(siteId, particleId)` entry is
  `ENM014`, as is a reference to an unknown site or particle. A value outside −100..100 eV or non-finite is
  `ENM015`. An entry for a particle the site's occupation set cannot contain is `ENM016` (warning).
- Site energies are used as a local offset and are also the mechanism for fixed migration barriers on unstable
  sites.
- Site energies can be overridden by a parameterization template (new capability; see step 09), but the model
  value remains the default.

### 5. Pair interactions (derived, not authored)

Pair interactions are discovered by the compiler:

- Sample all stable-stable pairs within the stable cutoff.
- Sample all unstable-stable pairs for each unstable environment.
- Apply filters.
- Reduce by the center stabilizer `G0`.
- Detect chiral pairs: the inverse direction is not in the same `G0` orbit. This is an orbit relationship,
  not physical chirality; in P1 every directed pair is its own orbit.
- Assign two stable identifiers to every directed pair candidate:
  - `GeometryId` is a lowercase SHA-256 over the domain-separated canonical projection of
    `(centerSiteId, centerP, partnerSiteId, partnerP, offsetA, offsetB, offsetC, distanceBucketFm)`.
    `centerP` and `partnerP` are the canonical P1 position indices from the Step 02 `Vector4I` encoding; they
    are required because one reference site can expand to several P1 positions. Omitting them would let two
    distinct shells with the same site pair, cell offset, and distance bucket collide and silently merge
    parameterization values. The ID identifies one directed geometry and survives recompilation while the
    geometry is unchanged.
  - `EnergyId` is the shared energy-table identity. For an achiral pair it equals `GeometryId`. For a chiral
    pair it is the lexicographically smaller of the two directions' `GeometryId` values, so both directions map
    to exactly one energy table and one parameterization set. The compiled model records each direction's
    `GeometryId`, its shared `EnergyId`, and the inverse direction's `ChiralPartnerGeometryId`.
  This resolves the cross-step ambiguity: Step 08's `object_id` and Step 09's pair-set ID are the `EnergyId`,
  not a per-direction center/partner key. Step 09 therefore stores one value per chiral pair; independent values
  for the two directions are unrepresentable, and `PRM007` is retired as unused.
- Unstable-stable pairs are asymmetric by nature. They have `GeometryId == EnergyId` and no chiral partner.
- The model layer exposes a preview with counts, distances, `GeometryId`, `EnergyId`, and the chiral partner
  geometry ID; the compiled interaction sets are produced in Step 08.

### 6. Cost preview

Before compilation, compute and show:

- number of P1 positions in the cell: every stable reference-site orbit expanded to P1 positions, not the
  number of stable reference sites; this is `EnergyCostPreview.StableP1PositionCount`, the stable subset of
  Step 07's total `P1PositionCount`;
- number of pair images within the cutoff;
- number of unique pair interactions after symmetry reduction;
- expected raw cluster permutations per cluster:
  `centerParticleCount × Π(surroundingParticleSetSize)` — the center factor is required and was missing from
  the earlier draft, matching the original `GetGroupPermutationCount`;
- unique permutations after symmetry reduction, computed as the number of orbits of the induced permutation
  group `P` on the raw state set (Burnside count in `long`; do not enumerate the full set);
- estimated memory for energy tables: pair tables `uniquePairInteractions × particleCount² × 8`, cluster tables
  `uniquePermutationCount × 8`; `EstimatedMemoryBytes` is the `long` sum;
- the original warning/hard limits below, surfaced as diagnostics rather than a bare warning count.

All count fields are `long`. The largest legal case (63 center particles and eight 63-particle surroundings)
is about `63^9 ≈ 1.6 × 10^16`, which fits in `long`; `int` would overflow.

The preview uses the same sampling kernel as compilation (Step 08). That kernel is
`Mocassin.Symmetry.Energy.IEnergyGeometrySampler` / `EnergyGeometrySampler`; it samples pair images, applies
filters, and builds the unique pair geometries without allocating energy tables. Cluster permutation
reduction uses the Step 02 `PointGroup`/`SitePermutation` utilities, so the preview and compiler share the same
reduction. Step 08 adds table allocation and canonical indices on top of the same kernel. The preview service
caches by
`(ProjectHasher.ComputeEnergy(model), ProjectHasher.ComputeStructure(model.Structure), cellCountA, cellCountB,
cellCountC, catalog version, symmetry contract version)`.

### 6.1 Limits and diagnostics

| Limit | Warning | Hard |
| --- | ---: | ---: |
| Stable-environment positions | 500 (`ENM017`) | 5000 (`ENM019`) |
| Unstable-environment positions | 100 (`ENM017`) | 1000 (`ENM019`) |
| Cluster raw permutations | 500 (`ENM012`) | 5000 (`ENM013`) |
| Clusters per center site | 4 (`ENM020`) | 10 (`ENM021`) |
| Cluster surrounding offsets | — | 2..8 (`ENM008`) |

The position-count estimates use the same pair-image sampling as the preview. An environment with zero
positions also warns (`ENM017`) because it defines no interaction. The default empty model suppresses the
sampling warnings until it has at least one site; this avoids warning on a placeholder `1 Å` cell.

### 7. Validation rules

| Code | Severity | Owner | Rule |
| --- | --- | --- | --- |
| `ENM001` | Error | Core | Any environment cutoff ≤ 0 or non-finite |
| `ENM002` | Warning | Symmetry | Any environment cutoff > half the smallest cell dimension when the model has at least one site (periodic ambiguity) |
| `ENM003` | Error | Core | Filter start < 0, start > end, or end outside the owning environment cutoff |
| `ENM004` | Error | Core | Filter references an unknown site, identical sites, or (in an unstable environment) a `SiteAId` other than the environment site |
| `ENM005` | Warning | Core | Duplicate/redundant filter after canonical sorting |
| `ENM006` | Error | Core | Unstable environment on a stable site or duplicate environment per site |
| `ENM007` | Error | Core | Cluster center unknown, not a stable/unstable reference site, or an unstable center without an unstable environment |
| `ENM008` | Error | Core | Cluster has fewer than 2 or more than 8 surrounding offsets |
| `ENM009` | Error | Symmetry | Duplicate surrounding offset after periodic encoding |
| `ENM010` | Error | Symmetry | A surrounding position is unknown, unstable, or removed by a center filter, or the cluster exceeds the center environment cutoff |
| `ENM011` | Error | Symmetry | Cluster full geometry has a center-equivalent position or another full-geometry duplicate not already reported by `ENM009` |
| `ENM012` | Warning | Symmetry | Cluster raw permutation count > 500 |
| `ENM013` | Error | Symmetry | Cluster raw permutation count > 5000 |
| `ENM014` | Error | Core | Site energy references an unknown site/particle, or repeats a `(siteId, particleId)` entry |
| `ENM015` | Error | Core | Site energy outside −100..100 eV or non-finite |
| `ENM016` | Warning | Core | Site energy for a particle the site cannot occupy |
| `ENM017` | Warning | Symmetry | Environment sampling estimate is below 1 or above the warning limit (stable 500, unstable 100) |
| `ENM018` | Warning | Step 07 | A KMC transition path leaves the relevant stable/unstable environment cutoff |
| `ENM019` | Error | Symmetry | Environment sampling estimate exceeds the hard limit (stable 5000, unstable 1000) |
| `ENM020` | Warning | Core | More than 4 clusters share one center site |
| `ENM021` | Error | Core | More than 10 clusters share one center site |

Validation ownership follows the assembly boundary, matching Steps 03 and 04:

- `Mocassin.Core.Model.Energy.EnergyModelValidator` owns the model-only rules: `ENM001`, `ENM003`–`ENM008`,
  `ENM014`–`ENM016`, `ENM020`, and `ENM021`.
- `Mocassin.Symmetry.Energy.EnergyGeometryValidator` owns the rules that need the catalog, cell geometry, or
  periodic encoder: `ENM002`, `ENM009`–`ENM013`, `ENM017`, and `ENM019`. It uses the same
  `IEnergyGeometrySampler` as the preview and Step 08.
- `Mocassin.Packages.Projects.ProjectValidationService` composes the Core and Symmetry results, as it already
  does for Steps 03 and 04. `ProjectEditor` is constructed with that composed validator when energy editing
  needs geometry errors to block an edit.
- The `ENM` codes are defined once in `Mocassin.Core.Diagnostics.EnergyDiagnosticCodes`; Symmetry references
  that Core constants class. There is no duplicate code table.
- `ENM018` is emitted by the Step 07 cross-domain pipeline because it relates Step 04 transition paths to
  Step 05 cutoffs. Step 05 defines the constant and help anchor; Step 07 owns emission and the consolidated
  diagnostics documentation. Step 04 does not depend on energy data.
- `ENM002` and the sampling warnings are suppressed while the model has no sites, so the default empty model
  (`a=b=c=1 Å`, default `1.0 Å` cutoff) does not warn.

### 7.1 Validation deviations from the original

The original validator reported many model-breaking conditions as warnings, and it did not range-check site
energies. Step 05 promotes the model-breaking warnings to errors and adds explicit v1 checks so `validate` and
`build` fail closed instead of proceeding with silently unusable energy data. This is a recorded deviation,
not a silent relaxation. Advisory and performance conditions stay warnings.

| Condition | Original behavior | Step 05 | Type | Rationale |
| --- | --- | --- | --- | --- |
| Environment cutoff non-positive or non-finite | No validator rule; `MaxInteractionRange` is a bare double | `ENM001` Error | New v1 | Every environment needs a usable sampling range |
| Environment cutoff > half the smallest cell dimension | Not checked | `ENM002` Warning | New v1 (advisory) | Periodic ambiguity is possible but not always wrong |
| Filter range outside the environment cutoff | Warning | `ENM003` Error | Promoted | The filter cannot be applied deterministically |
| Unknown/identical filter sites, or an unstable filter not centered on the environment site | Warning / not checked | `ENM004` Error | Promoted + new | The filter cannot be resolved or applied in the intended direction |
| Unstable environment on a stable site / duplicate per site | Warning | `ENM006` Error | Promoted | The environment is unusable or ambiguous |
| Unknown cluster center or missing unstable environment | Warning | `ENM007` Error | Promoted | The cluster has no owner or no applicable environment |
| Offset count outside 2..8 | Warning | `ENM008` Error | Promoted | The cluster is degenerate or exceeds the 6-bit/9-site code |
| Duplicate surrounding offsets | Warning | `ENM009` Error | Promoted | The cluster geometry is ambiguous |
| Invalid surrounding position (unknown/unstable/filtered) or cluster outside the center cutoff | Warning | `ENM010` Error | Promoted | The cluster geometry is not valid or reaches outside the environment |
| Full-geometry duplicate or center-equivalent position | Warning | `ENM011` Error | Promoted | The original “ring” check finds degenerate geometry |
| Cluster raw permutations >5000 | Warning | `ENM013` Error | Promoted | The original `PermutationsPerGroup` maximum is exceeded |
| Duplicate site-energy entry or void-on-stable | Warning | `ENM014` Error | Promoted | The entry is ambiguous or occupies an invalid state |
| Unknown site/particle reference in a site-energy entry | Not checked / partly structural | `ENM014` Error | New v1 | The entry cannot be resolved |
| Site energy non-finite (NaN/Inf) | Warning | `ENM015` Error | Promoted | A non-finite value cannot be used |
| Site energy outside −100..100 eV | No site-energy range check; `PairEnergies`/`GroupEnergies` use −100..100 | `ENM015` Error | New v1 (range half) | Site energies are also migration barriers; the v1 range matches the pair/group setting |
| Sampling hard limits (stable >5000, unstable >1000) | Warning | `ENM019` Error | Promoted | The requested model cannot be compiled within the budget |
| More than 10 clusters per center | Warning | `ENM021` Error | Promoted | The cluster budget is exceeded |

New v1 checks without a direct original counterpart include, illustratively: `ENM001` (positive/finite
environment cutoffs), `ENM002` (periodic-ambiguity warning), the unstable-filter direction constraint in
`ENM004`, unknown site/particle references in `ENM014`, the site-energy −100..100 eV range in `ENM015`, and
the Step 07-owned `ENM018` KMC path warning. Duplicate site-energy entries and non-finite site energies are
promoted warnings (`ENM014`, `ENM015`), not new checks. The original
`StableEnvironmentInfoValidator` warned on duplicates, void-on-stable, and NaN/Inf only; it did not apply the
pair/group −100..100 eV setting to site energies.

The following remain warnings because they are advisory or performance-related: `ENM002` (periodic
ambiguity), `ENM005` (duplicate filter), `ENM012` (cluster permutation warning), `ENM016` (forbidden
occupancy), `ENM017` (sampling warning), `ENM018` (KMC path), and `ENM020` (clusters-per-center warning).
The inclusive filter boundary, the 2..8 surrounding-offset range, and the stable-or-unstable center rule
match the original semantics; they are not deviations.

### 8. Editing operations

- Update stable environment cutoff and filters.
- Add/update/remove unstable environments.
- Add/update/remove clusters and offsets.
- Set/clear site energies.
- Changing the structure (sites) invalidates environments, clusters, and site energies that reference removed
  sites; the editor offers cascade removal or rebase.
- `ProjectEditor` is already 506 lines. Energy operations go into a partial file
  `Editing/ProjectEditor.Energy.cs` in the same folder and namespace so the 800-line file limit remains
  satisfied. The partial uses the composed Core + Symmetry validator when the edit depends on geometry.

### 9. Public API

```csharp
// Mocassin.Core
public interface IEnergyQuery
{
    StableEnvironment StableEnvironment { get; }
    ImmutableArray<UnstableEnvironment> UnstableEnvironments { get; }
    ImmutableArray<Cluster> Clusters { get; }
    ImmutableArray<SiteEnergySet> SiteEnergies { get; }

    UnstableEnvironment? FindEnvironment(CellSiteId siteId);
    Cluster? FindCluster(ClusterId id);
    SiteEnergySet? FindSiteEnergies(CellSiteId siteId);
}

// Existing interface, extended additively.
public interface IProjectQuery : IParticleQuery, IStructureQuery, ITransitionQuery, IEnergyQuery;

public static class EnergyLimits
{
    public const int StablePositionsWarning = 500;
    public const int StablePositionsHard = 5000;
    public const int UnstablePositionsWarning = 100;
    public const int UnstablePositionsHard = 1000;
    public const int ClusterPermutationsWarning = 500;
    public const int ClusterPermutationsHard = 5000;
    public const int ClustersPerPositionWarning = 4;
    public const int ClustersPerPositionHard = 10;
    public const int ClusterOffsetsMin = 2;
    public const int ClusterOffsetsMax = 8;
    public static EnergyEv MinEnergy { get; } = new(-100.0);
    public static EnergyEv MaxEnergy { get; } = new(100.0);
}

// Mocassin.Symmetry
public sealed record ClusterCostPreview(
    ClusterId ClusterId,
    long RawPermutationCount,
    long UniquePermutationCount,
    long EstimatedBytes);

public sealed record EnergyCostPreview(
    long StableP1PositionCount,
    long PairImageCount,
    long UniquePairInteractionCount,
    ImmutableArray<ClusterCostPreview> ClusterCosts,
    long EstimatedMemoryBytes);

public sealed record PairInteractionPreview(
    string GeometryId,
    string EnergyId,
    CellSiteId SiteAId,
    int CenterP,
    CellSiteId SiteBId,
    int PartnerP,
    double DistanceAngstrom,
    bool IsChiral,
    string? ChiralPartnerGeometryId);

public interface IEnergyGeometrySampler
{
    bool TrySamplePairs(
        ModelDefinition model,
        int cellCountA,
        int cellCountB,
        int cellCountC,
        out ImmutableArray<PairInteractionPreview> pairs,
        out ImmutableArray<Diagnostic> diagnostics);
}

public interface IEnergyPreviewService
{
    EnergyCostPreview? TryCreatePreview(
        ModelDefinition model,
        int cellCountA,
        int cellCountB,
        int cellCountC,
        out ImmutableArray<Diagnostic> diagnostics);

    ImmutableArray<PairInteractionPreview>? TryCreatePairPreview(
        ModelDefinition model,
        int cellCountA,
        int cellCountB,
        int cellCountC,
        out ImmutableArray<Diagnostic> diagnostics);
}
```

- `IProjectQuery` gains `IEnergyQuery` so the same immutable `Project` snapshot exposes particles, structure,
  transitions, and energy. `IEnergyQuery` is implemented by `Project`.
- `EnergyLimits` exposes energy bounds as `EnergyEv` at the public API boundary; count limits stay `int`.
- `IEnergyPreviewService` reads the structure and energy sections from `ModelDefinition` and lives in
  `Mocassin.Symmetry`; Core never references Symmetry. It follows the Step 03/04 preview boundary:
  `TryCreatePreview(..., out diagnostics)` returns `null` on invalid user data and never throws.
- `IEnergyGeometrySampler` is shared by the preview service and Step 08. It samples pair images, applies
  filters, and builds unique pair geometries; cluster reduction uses the Step 02 `PointGroup`/`SitePermutation`
  utilities. It does not allocate energy tables. `TrySamplePairs` returns `false` with an empty pair array and
  diagnostics on invalid input and never throws, matching the preview `TryCreate…` contract.
- `EnergyPreviewService` caches by the energy hash, structure hash, requested `(A, B, C)`, catalog version, and
  symmetry contract version. `ProjectHasher.ComputeEnergy` is the energy-hash producer.
- Derived pair interactions use `GeometryId`, the shared `EnergyId`, and
  `ChiralPartnerGeometryId`; the preview is advisory and the compiled interaction tables are authoritative in
  Step 08.

### 10. Cross-step interfaces

- **Step 03 (done).** Reused without change: `CellSiteId`, `ParticleId`, `StructureModel`, `CellSite`,
  `Fractional3`, `IStructureQuery`, and the `Mocassin.Symmetry` `UnitCellVectorEncoder`/cell geometry. Core
  energy validation stays Core-only; catalog/cell rules use the Symmetry validator.
- **Step 04 (done).** Step 05 does not read transitions in Core. `ENM018` is a Step 07 cross-domain rule that
  reads `KineticTransition.ReferencePath` and `ResolvedTransitionPath`; Step 04 remains energy-independent.
- **Step 06.** No lattice, doping, or simulation field is read or extended by Step 05.
- **Step 07.** Emits `ENM018` and aggregates/documents `ENM001`–`ENM021`. Step 05 defines the `ENM018`
  constant and help anchor only.
- **Step 08.** Uses `IEnergyGeometrySampler`, `ProjectHasher.ComputeEnergy(model)`, `GeometryId`, and `EnergyId`.
  The compiled pair/cluster tables and canonical indices are authoritative; the Step 05 preview is advisory.
- **Step 09.** Pair-set identity is the shared `EnergyId`; one value is stored per chiral pair, so independent
  A→B/B→A values are unrepresentable. The Step 09 page is updated to retire `PRM007` as unused.
- **Step 15.** Consumes the compiled pair/cluster/site tables; the Step 05 model supplies the geometric and
  symbolic definition only.
- **Step 25.** Uses `IEnergyQuery` and `IEnergyPreviewService` for the energy/cluster editors.

No unresolved cross-step interface question remains at this revision.

## Code layout

The new code follows the structure policy: PascalCase domain folders, namespaces that match folders, no new
root-level files, and no folder above 20 direct files.

```text
Mocassin.Core/
  Model/
    ModelDefinition.cs                  # add energy sections
    ModelDefinitionJsonConverter.cs     # read/write the new sections
    Energy/
      StableEnvironment.cs
      UnstableEnvironment.cs
      InteractionFilter.cs
      Cluster.cs
      SiteEnergySet.cs
      SiteEnergyEntry.cs
      EnergyLimits.cs
      IEnergyQuery.cs
      EnergyModelValidator.cs
      EnergyModelValidator.Clusters.cs  # partial only if the file approaches 800 lines
  Diagnostics/
    EnergyDiagnosticCodes.cs            # ENM001-ENM021; ENM018 constant/help anchor
  Editing/
    ProjectEditor.cs                    # unchanged partial root
    ProjectEditor.Energy.cs             # energy editing operations
  Project/
    ProjectJson.cs                      # extend CanonicalizeModel/SerializeCanonicalModel; ProjectJsonContext is nested here
    ProjectHasher.cs                    # add ComputeEnergy
    ProjectValidator.cs                 # Core-only composition

Mocassin.Packages/
  Projects/
    ProjectValidationService.cs         # compose Core + Symmetry energy diagnostics

Mocassin.Symmetry/
  Energy/
    EnergyCostPreview.cs
    ClusterCostPreview.cs
    PairInteractionPreview.cs
    IEnergyGeometrySampler.cs
    EnergyGeometrySampler.cs
    EnergyGeometryValidator.cs
    IEnergyPreviewService.cs
    EnergyPreviewService.cs
```

`ProjectSchema.cs` is unchanged because `schemaVersion` remains 1. `ProjectJsonContext` is not a file.

Tests mirror the source concept:

- `Mocassin.Core.Tests/Energy/EnvironmentTests.cs`
- `Mocassin.Core.Tests/Energy/ClusterTests.cs`
- `Mocassin.Core.Tests/Energy/SiteEnergyTests.cs`
- `Mocassin.Core.Tests/Energy/EnergyModelTests.cs`
- `Mocassin.Core.Tests/Energy/EnergyHashTests.cs`
- `Mocassin.Symmetry.Tests/Energy/EnergyGeometryTests.cs`
- `Mocassin.Symmetry.Tests/Energy/EnergyPreviewTests.cs`
- `Mocassin.Packages.Tests/Projects/EnergyValidationCompositionTests.cs`
- `Mocassin.Benchmarks/Energy/EnergyPreviewBenchmarks.cs`

Samples live under `samples/energy/`. The new user-guide, physics, and reference pages are added to
`zensical.toml` navigation and the corresponding index pages in the same step.

## Implementation tasks

- [ ] Extend `ModelDefinition` with the flat energy sections; update `ModelDefinitionJsonConverter`,
  `ProjectJson.CanonicalizeModel`/`SerializeCanonicalModel`, `ProjectValidator`, and
  `ProjectValidationService`. `ProjectSchema` is unchanged; `ProjectJsonContext` is extended in
  `Project/ProjectJson.cs`.
- [ ] Add `ProjectHasher.ComputeEnergy(ModelDefinition)` and tests that the model/document hashes include energy
  data and the energy hash is invariant to unrelated edits.
- [ ] Implement `StableEnvironment`, `UnstableEnvironment`, `InteractionFilter`, `Cluster`, `SiteEnergySet`,
  `SiteEnergyEntry`, and `EnergyLimits`.
- [ ] Implement the Core `EnergyModelValidator` (`ENM001`, `ENM003`–`ENM008`, `ENM014`–`ENM016`, `ENM020`,
  `ENM021`) and the Symmetry `EnergyGeometryValidator` (`ENM002`, `ENM009`–`ENM013`, `ENM017`, `ENM019`);
  compose them in `ProjectValidationService`.
- [ ] Implement `IEnergyQuery`, add it to `IProjectQuery`, and implement `IEnergyGeometrySampler`,
  `IEnergyPreviewService`, and the cost/pair previews with `TryCreatePreview(..., out diagnostics)`.
- [ ] Implement cluster geometry validation (encoder-based periodic dedup, center-equivalent/ring detection,
  surrounding-site validity, filter interaction, and cutoff range).
- [ ] Implement derived pair-interaction preview with `GeometryId`, shared `EnergyId`, and
  `ChiralPartnerGeometryId`.
- [ ] Implement chiral-pair detection and shared-energy linkage.
- [ ] Implement `ProjectEditor.Energy.cs`, editor operations, and cascade invalidation.
- [x] Add the Core energy diagnostic codes and help text, including the `ENM018` constant/help anchor used by
  Step 07.
- [ ] Update `contracts/manifest.json`: set `status` to `step-05`, update `notes`, add the energy fields, add
  the `project-energy-minimal` and `project-energy-populated` fixtures, update the fixture/signature vectors,
  and regenerate the schema. The project `schemaVersion` remains 1 under the additive-v1 precedent; ADR 0009's
  bump rule is reconciled explicitly in the manifest notes, as in Step 04.
- [ ] Add `SPDX-License-Identifier: MIT` to every new source, test, tool, and sample file; record in the step log
  that no new dependency or GPL-derived content is introduced, per `NOTICE.md`.
- [ ] Add XML docs, update `PublicAPI.Unshipped.txt` for Core, Packages, and Symmetry, and refresh the ApiCompat
  baseline for every new public type and member.
- [ ] Add ceria and barium-zirconate energy models and fixtures.
- [ ] Add the new user-guide, physics, and reference pages to `zensical.toml` navigation and to the
  user-guide/physics/reference index pages; update `docs/reference/project-format.md`.
- [ ] Update `docs/planning/status.md`, the Step 05 feature log/checkboxes, and
  `docs/planning/appendix-b-parity-matrix.md`; create `docs/planning/audits/step-05.md` from the template at
  closeout.

## Acceptance criteria

- [ ] Ceria's stable environment and oxygen migration-edge cluster validate.
- [ ] A stable filter that removes a specific sublattice pair is applied symmetrically; an unstable filter is
  one-directional and keeps `SiteAId` equal to the environment site.
- [ ] Filter boundaries are inclusive and use the 1e-5 Å distance tolerance.
- [ ] A cluster with 2 or 8 offsets is accepted; 1 or 9 offsets are rejected (`ENM008`).
- [ ] Duplicate, center-equivalent, unknown/unstable/filtered-surrounding, ring/degenerate, and out-of-range
  clusters are rejected with paths (`ENM009`–`ENM011`).
- [ ] The cost preview reports pair counts and permutation counts that match a brute-force oracle, including
  the center-particle factor and `long` overflow-safe limits; the Step 08 cross-check is recorded as a Step 08
  follow-up instead of a Step 05 closeout claim.
- [ ] `GeometryId` includes `centerP`/`partnerP`, so distinct P1 shells with the same site pair, offset, and
  distance bucket do not collide.
- [ ] Chiral pair partners are detected, share one `EnergyId`, and expose `ChiralPartnerGeometryId`; independent
  A→B/B→A energy values are unrepresentable.
- [ ] The energy sections round-trip through the project JSON context; the default `stableEnvironment` loads the
  1.0 Å/no-filter default; empty sections are omitted; semantic arrays preserve order.
- [ ] Canonical collection ordering and `ProjectHasher.ComputeEnergy` are stable under array shuffles and change
  when energy data changes.
- [ ] The generated project schema accepts the energy sections, and fixtures cover empty and populated models.
- [ ] Every `ENM001`–`ENM021` code has a positive or negative test as applicable, including the ENM018
  Step 07 boundary.
- [ ] New files carry SPDX headers and introduce no new dependency.
- [ ] Public API files and the ApiCompat baseline are current.
- [ ] Documentation pages, navigation, index links, status dashboard, and parity matrix are updated.

## Documentation

At this point, document the energy model:

- `docs/user-guide/energy.md` — environments, filters, clusters, site energies, cost limits, and how to fix
  oversized clusters/permutations. This page owns Step 05 error guidance. It does **not** link to
  `docs/troubleshooting/errors.md` at Step 05 closeout because Step 07 creates that page; Step 07 adds the
  cross-link when it exists. Step 05 also does not own or duplicate the consolidated troubleshooting page.
- `docs/physics/energies.md` — pair/cluster/site contributions, the environment cutoff, and the chiral-orbit
  relationship.
- `docs/reference/energy.md` — the energy model reference: section shape, defaults, limits, semantic ordering,
  and `GeometryId`/`EnergyId`. It does **not** duplicate the diagnostic catalog; Step 07 owns the complete
  `ENM` catalog in `docs/reference/diagnostics.md` and adds the cross-link when that page exists.
- `docs/reference/project-format.md` — energy sections, field names, semantic ordering, defaults, and
  constraints.
- `docs/development/compilation-pipeline.md` — environment sampling and the derived pair/cluster preview; the
  shared sampler boundary with Step 08.

Add the new user-guide, physics, and reference pages to `zensical.toml` navigation and to
`docs/user-guide/index.md`, `docs/physics/index.md`, and `docs/reference/index.md` in the same step. Update the
Step 05 row in `docs/planning/status.md` and the energy rows in `docs/planning/appendix-b-parity-matrix.md`.
`docs/troubleshooting/errors.md` and `docs/reference/diagnostics.md` remain owned by Step 07; Step 05 only adds
the cross-links in Step 07 when those pages exist. A strict documentation build at Step 05 closeout must not
contain links to pages that do not yet exist.

## Tests

Test tiers:

- **PR-fast:** Core model, validation, canonicalization, hash, and schema round-trip tests. No symmetry sampling.
- **PR-medium:** Symmetry geometry/preview tests, ceria/BZO sample validation, brute-force oracles, and
  diagnostic-composition tests.
- **Nightly/release:** cost-preview benchmark, large-cell stress, and overflow/limit tests.

| Test | Assertion | Tier |
| --- | --- | --- |
| Filters | Inclusive boundaries with 1e-5 Å tolerance, stable symmetry, unstable one-directionality, overlap warnings | PR-fast |
| Cluster geometry | 2/8 accepted; 1/9 rejected; periodic duplicates, center-equivalent zero offset, unknown/unstable/filtered surroundings, cutoff | PR-medium |
| Site energies | Sparse defaults, range, unknown site/particle, duplicate `(site, particle)`, forbidden occupancy | PR-fast |
| Core diagnostics | Positive/negative coverage for `ENM001`, `ENM003`–`ENM008`, `ENM014`–`ENM016`, `ENM020`, `ENM021` | PR-fast |
| Symmetry diagnostics | Positive/negative coverage for `ENM002`, `ENM009`–`ENM013`, `ENM017`, `ENM019` | PR-medium |
| ENM018 boundary | The Step 07-owned path warning is documented; Step 05 only defines the constant/help anchor | PR-fast |
| Default environment | Missing/`{}` model loads the 1.0 Å/no-filter default; default is omitted on write; empty sections omitted | PR-fast |
| Model envelope | Empty, energy-only, and combined models round-trip; semantic offset order preserved; sorted collections stable | PR-fast |
| Hash | Model/document hashes include energy; `ComputeEnergy` is invariant to array shuffle and unrelated edits | PR-fast |
| Schema | Generated project schema accepts the energy sections and rejects unknown fields; fixtures empty/populated | PR-fast |
| Pair preview | Counts and distances match brute-force sampling for random cells | PR-medium |
| Geometry identity | Multi-P1-site pairs with the same site pair, offset, and distance bucket but different `centerP`/`partnerP` produce distinct `GeometryId` values; no collision | PR-medium |
| Chiral pairs | Known chiral/achiral geometries; shared `EnergyId`; `ChiralPartnerGeometryId`; no independent values | PR-medium |
| Permutations | Center factor included; `long` values; Burnside unique count matches brute force; 63^9 case does not overflow | PR-medium/nightly |
| Cost limits | Warning/hard thresholds for stable, unstable, cluster permutations, and clusters per position | PR-medium |
| Preview diagnostics | `TryCreatePreview` returns `null` plus diagnostics on invalid data and never throws | PR-medium |
| Invalidation | Removing a site cascades or reports dependents | PR-fast |
| Benchmark | Ceria preview ≤ 100 ms and BZO ≤ 250 ms on the reference runner; record actuals | Nightly/release |
| Public API | `PublicAPI.Unshipped.txt` and ApiCompat baseline are current | PR-fast |

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Energy data is dropped by canonical save or missing from the model hash | Extend `ProjectJson.CanonicalizeModel`, add `ProjectHasher.ComputeEnergy`, and test save/load/hash round-trips |
| Core validation cannot implement catalog/cell rules | Split Core and Symmetry validators; compose in `ProjectValidationService`; test the composition |
| Chiral identity diverges between preview, compiler, and templates | One shared `EnergyId` canonicalization; Step 08/09 consume it; preview exposes `GeometryId`/`ChiralPartnerGeometryId` |
| Cost preview overflows or undercounts permutations | `long` counts, explicit center factor, Burnside unique count, and 63^9 tests |
| Preview and compiler sampling diverge | One `IEnergyGeometrySampler` kernel used by both; Step 08 cross-check recorded as a Step 08 follow-up |
| Cluster explosion | Hard limits, preview, and UI warnings before compilation |
| Periodic cutoff ambiguity | Warn when cutoff exceeds half the smallest cell dimension, suppressed for the empty default model |
| Energy sections change existing project hashes | Omit the default environment and empty sections; test hash stability for pre-Step-05 projects |

## Done when

A user can define the complete interaction environment, see its cost and derived pair/cluster counts, and
receive precise validation before compilation.
