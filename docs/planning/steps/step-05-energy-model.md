# Step 05 — Energy Model

**Phase:** B. Model definition  
**Depends on:** 03, 04
**Delivers:** Stable/unstable interaction environments, cutoff filters, multi-body clusters, and site energies,
with validation and cost previews.

## Implementation status

- **Status:** Not started
- **Step branch:** —
- **Step CI:** —
- **Main merge:** —
- **Step tag:** —
- **Audit:** Not requested
- **Design review:** [Ready for review](../reviews/step-05.md)
- **Last updated:** 2026-11-09

| Feature | Branch | Status | Local gates | Step CI | Squash commit |
| --- | --- | --- | --- | --- | --- |
| — | — | — | — | — | — |

## Goal

Let a user define the interaction model that the compiler will sample into pair and cluster tables. The
definition is geometric and symbolic; energy values are supplied later by parameterization templates.

## Original reference

- `IStableEnvironmentInfo`: maximum interaction range, radial filters, and defect (site) energies. Default
  range 1.0 Å.
- `IUnstableEnvironment`: one per unstable site, with its own range, filters, pair interactions, and group
  interactions.
- `IInteractionFilter`: center/partner site, start/end radius, applicability.
- Pair interactions are **auto-sampled** from the environment; users do not add them directly. Each pair has a
  distance, chirality flag, chiral partner, and energy entries.
- `IGroupInteraction`: center site plus surrounding geometry, size 2–8 (center + up to 8 surroundings = up to
  9 positions), symmetry-reduced occupation-state energies.
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
- `ModelDefinitionJsonConverter`, `ProjectJsonContext`, `ProjectSchema`, and `ProjectValidator` are extended
  for the new sections.
- A missing `stableEnvironment` uses the documented default: cutoff `1.0 Å`, no filters.
- Semantic array order:
  - `Cluster.OffsetVectors` — definition order relative to the center.
  All other collections are sorted when serialized:
  - `UnstableEnvironments` by `siteId`;
  - `Clusters` by `id`;
  - `SiteEnergies` by `siteId`;
  - `InteractionFilter` entries by `(start, end, siteAId, siteBId)`;
  - `SiteEnergySet.Entries` by `particleId`.
- The project `schemaVersion` remains 1 because the energy sections are additive.
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

- One stable environment defines the sampling range for all stable sites.
- Each unstable site has an optional environment; if omitted, it inherits the stable cutoff but samples
  unstable-stable pairs only.
- `Cutoff` must be positive and within the cell's geometric limits.
- A filter removes pair interactions between `SiteA` and `SiteB` whose distance is in `[Start, End)`.
- Filters apply symmetrically to both sites.
- Filters must lie within the cutoff; overlapping filters are allowed but redundant ones warn.
- Filters are part of the model signature; changing one invalidates pair/cluster parameterizations.

### 3. Clusters

```csharp
public sealed record Cluster(
    ClusterId Id,
    string Name,
    CellSiteId CenterSiteId,
    ImmutableArray<Fractional3> OffsetVectors);
```

- The center is an existing reference site; offsets are absolute fractional vectors relative to the center.
- Up to 8 offsets (9 positions total).
- Offsets may be outside `[0,1)`.
- The cluster geometry must be within the center site's environment cutoff.
- No two offsets may be identical after periodic trimming.
- The cluster must not contain a ring; each surrounding position must be reachable from the center without
  revisiting a position.
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
- Detect chiral pairs: the inverse direction is not in the same `G0` orbit. Chiral partners share one energy
  table; the parameterization editor mirrors values. This is an orbit relationship, not physical chirality;
  in P1 every directed pair is its own orbit.
- Assign stable IDs derived from the two site IDs, the periodic offset, and a canonical geometry hash. The IDs
  survive recompilation as long as the geometry is unchanged.
- The model layer exposes a preview with counts and distances; the compiled interaction sets are produced in
  step 08.

### 6. Cost preview

Before compilation, compute and show:

- number of stable positions in the P1 cell;
- number of pair images within the cutoff;
- number of unique pair interactions after symmetry reduction;
- expected raw cluster permutations per cluster: product of surrounding particle-set sizes;
- unique permutations after symmetry reduction;
- estimated memory for energy tables (pair tables × particle², cluster tables × permutations);
- warnings when the original limits are exceeded.

The preview uses the same sampling code as compilation (step 08) but does not allocate the full tables.

### 7. Validation rules

| Code | Severity | Rule |
| --- | --- | --- |
| `ENM001` | Error | Stable cutoff ≤ 0 or non-finite |
| `ENM002` | Warning | Stable cutoff > half the smallest cell dimension (periodic ambiguity) |
| `ENM003` | Error | Filter start > end or outside cutoff |
| `ENM004` | Error | Filter references unknown or identical sites |
| `ENM005` | Warning | Duplicate/redundant filter |
| `ENM006` | Error | Unstable environment on a stable site or duplicate per site |
| `ENM007` | Error | Cluster center unknown or unstable |
| `ENM008` | Error | Cluster has fewer than 1 or more than 8 offsets |
| `ENM009` | Error | Duplicate cluster offset |
| `ENM010` | Error | Cluster geometry outside the center environment cutoff |
| `ENM011` | Error | Cluster contains a ring/degenerate geometry |
| `ENM012` | Warning | Cluster permutation count exceeds the warning limit |
| `ENM013` | Error | Cluster permutation count exceeds the hard limit |
| `ENM014` | Error | Site energy references unknown site/particle |
| `ENM015` | Error | Site energy outside −100..100 eV |
| `ENM016` | Warning | Site energy for a particle the site cannot occupy |
| `ENM017` | Warning | Environment sampling estimate exceeds the performance budget |
| `ENM018` | Warning | A KMC transition path leaves the relevant stable/unstable environment cutoff |

`ENM018` is emitted by the Step 07 cross-domain pipeline because it relates Step 04 transition paths to
Step 05 cutoffs. Step 04 does not depend on energy data.

### 8. Editing operations

- Update stable environment cutoff and filters.
- Add/update/remove unstable environments.
- Add/update/remove clusters and offsets.
- Set/clear site energies.
- Changing the structure (sites) invalidates environments, clusters, and site energies that reference removed
  sites; the editor offers cascade removal or rebase.

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

// Mocassin.Symmetry
public sealed record ClusterCostPreview(
    ClusterId ClusterId,
    int RawPermutationCount,
    int UniquePermutationCount,
    long EstimatedBytes);

public sealed record EnergyCostPreview(
    int P1PositionCount,
    int PairImageCount,
    int UniquePairInteractionCount,
    ImmutableArray<ClusterCostPreview> ClusterCosts,
    long EstimatedMemoryBytes,
    int WarningCount);

public sealed record PairInteractionPreview(
    string StableId,
    CellSiteId SiteAId,
    CellSiteId SiteBId,
    double DistanceAngstrom,
    bool IsChiral);

public interface IEnergyPreviewService
{
    EnergyCostPreview CreatePreview(
        StructureModel structure,
        int cellCountA,
        int cellCountB,
        int cellCountC);

    ImmutableArray<PairInteractionPreview> CreatePairPreview(
        StructureModel structure,
        int cellCountA,
        int cellCountB,
        int cellCountC);
}
```

- `IEnergyQuery` is implemented by the immutable `Project` snapshot.
- `IEnergyPreviewService` lives in `Mocassin.Symmetry`; Core never references Symmetry.
- Derived pair interactions use stable geometry IDs and a preview-only `PairInteractionPreview`.
- The preview is advisory; the compiled interaction tables are authoritative in Step 08.

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
      IEnergyQuery.cs
      EnergyModelValidator.cs
  Diagnostics/
    EnergyDiagnosticCodes.cs            # ENM001-ENM018
  Editing/
    ProjectEditor.cs                    # add energy editing operations
  Project/
    ProjectJsonContext.cs
    ProjectSchema.cs
    ProjectValidator.cs

Mocassin.Symmetry/
  Energy/
    EnergyCostPreview.cs
    ClusterCostPreview.cs
    PairInteractionPreview.cs
    EnergyPreviewService.cs
    IEnergyPreviewService.cs
```

Tests mirror the source concept:

- `Mocassin.Core.Tests/Energy/EnvironmentTests.cs`
- `Mocassin.Core.Tests/Energy/ClusterTests.cs`
- `Mocassin.Core.Tests/Energy/SiteEnergyTests.cs`
- `Mocassin.Core.Tests/Energy/EnergyModelTests.cs`
- `Mocassin.Symmetry.Tests/Energy/EnergyPreviewTests.cs`

Samples live under `samples/energy/`. The new user-guide, physics, and troubleshooting pages are added to
`zensical.toml` navigation in the same step.

## Implementation tasks

- [ ] Extend `ModelDefinition` with the flat energy sections; update `ModelDefinitionJsonConverter`,
  `ProjectJsonContext`, `ProjectSchema`, and `ProjectValidator`.
- [ ] Implement `StableEnvironment`, `UnstableEnvironment`, `InteractionFilter`, `Cluster`, `SiteEnergySet`.
- [ ] Implement crystal/cutoff/filter validation.
- [ ] Implement cluster geometry validation (periodic dedup, ring detection, range).
- [ ] Implement `IEnergyQuery` and `IEnergyPreviewService`.
- [ ] Implement the cost preview using symmetry sampling and the requested `(A, B, C)` cell counts.
- [ ] Implement derived pair-interaction preview with stable geometry IDs.
- [ ] Implement chiral-pair detection and linkage.
- [ ] Implement editor operations and cascade invalidation.
- [ ] Add the Core energy diagnostic codes and help text.
- [ ] Update `contracts/manifest.json` with the energy sections; regenerate the project schema and add fixtures
  with empty and populated energy data. The project `schemaVersion` remains 1 because the fields are additive.
- [ ] Add XML docs, update `PublicAPI.Unshipped.txt`, and refresh the ApiCompat baseline for every new public
  type and member.
- [ ] Add ceria and barium-zirconate energy models.
- [ ] Add the new user-guide, physics, building, and troubleshooting pages to `zensical.toml` navigation.

## Acceptance criteria

- [ ] Ceria's stable environment and oxygen migration-edge cluster validate.
- [ ] A filter that removes a specific sublattice pair is applied symmetrically.
- [ ] A cluster with 8 offsets is accepted; 9 offsets are rejected.
- [ ] Duplicate/ring/out-of-range clusters are rejected with paths.
- [ ] The cost preview reports pair and permutation counts consistent with step 08.
- [ ] Chiral pair partners are detected and cannot receive independent energies.
- [ ] The energy sections round-trip through the project JSON context; semantic arrays preserve order.
- [ ] The generated project schema accepts the energy sections, and fixtures cover empty and populated models.
- [ ] Public API files and the ApiCompat baseline are current.
- [ ] Documentation written

## Documentation

At this point, document the energy model:

- `docs/user-guide/energy.md` — environments, filters, clusters, site energies, cost limits.
- `docs/physics/energies.md` — pair/cluster/site contributions and the environment cutoff.
- `docs/development/compilation-pipeline.md` — environment sampling and derived pair/cluster preview.
- `docs/troubleshooting/errors.md` — oversized clusters/permutations and how to fix them.
- `docs/reference/project-format.md` — energy sections, field names, semantic ordering, and constraints.

Add the new user-guide, physics, and troubleshooting pages to `zensical.toml` navigation in the same step.

## Tests

| Test | Assertion |
| --- | --- |
| Filters | Inclusive/exclusive boundaries, symmetry, overlap |
| Cluster geometry | Valid/invalid offsets, periodic duplicates, ring |
| Site energies | Sparse defaults, range, invalid particles |
| Pair preview | Counts match brute-force sampling for random cells |
| Chiral pairs | Known chiral and achiral geometries |
| Cost limits | Warning and hard-limit thresholds |
| Model envelope | Empty, energy-only, and combined models round-trip through the project JSON context |
| Schema | Generated project schema accepts the energy sections and rejects unknown fields |
| Semantic order | Cluster offset vectors preserve definition order across serialization |
| Invalidation | Removing a site cascades or reports dependents |

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Pair/cluster counting conventions differ between preview and solver | Share the sampling kernel and compare in step 08 |
| Cluster explosion | Hard limits, preview, and UI warnings before compilation |
| Chirality ambiguity | Explicit chiral-partner linkage and tests |
| Periodic cutoff ambiguity | Warn when cutoff exceeds half the smallest cell dimension |

## Done when

A user can define the complete interaction environment, see its cost and derived pair/cluster counts, and
receive precise validation before compilation.
