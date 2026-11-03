# Step 03 — Particle and Structure Models

**Phase:** B. Model definition  
**Depends on:** 02
**Delivers:** Particle, particle-set, space-group, cell-parameter, cell-site, and dummy-position models with
validation and a stable public API.

## Implementation status

- **Status:** Not started
- **Step branch:** —
- **Step CI:** —
- **Main merge:** —
- **Last updated:** —

| Feature | Branch | Status | Local gates | Step CI | Squash commit |
| --- | --- | --- | --- | --- | --- |
| — | — | — | — | — | — |

## Goal

Implement the first two model domains and their invariants. A user must be able to define species, allowed
occupations, a crystal cell, and stable/unstable reference sites, and see the symmetry-expanded unit cell.

## Original reference

- `Mocassin.Model.Particles`: `IParticle` (`Charge`, `Symbol`, `IsVacancy`, `IsVoid`), `IParticleSet`
  (ordered particles, bitmask), `OccupationState` (ordered particle list).
- `Mocassin.Model.Structures`: `ICellSite` (`Fractional3D Vector`, `IParticleSet OccupationSet`,
  `PositionStability Stability`), `ICellDummyPosition`, `ICellParameters`, `ISpaceGroupInfo`,
  `IStructureInfo`.
- Limits: 64 particles including void (63 custom), 100 particle sets, 1000 base positions, 10,000 total
  positions, cell lengths 0.1–1000 Å.
- Site validation: reference positions in `[0,1)`; no site may fall inside another site's P1/Wyckoff orbit.
- Default: P1, `a=b=c=1 Å`, all angles 90°, structure name "New Structure".
- Dummy positions are visual-only and have no simulation semantics.

## Design

### 1. Particle model

```csharp
public sealed record Particle(
    ParticleId Id,
    string Name,
    string Symbol,
    ChargeElementary Charge,
    bool IsVacancy);

public sealed record ParticleSet(
    ParticleSetId Id,
    string Name,
    ImmutableArray<ParticleId> ParticleIds);
```

- The **void particle** is implicit, reserved, and not stored in `Particles`. It has a well-known ID, index 0,
  symbol `∅`, charge 0, and `IsVacancy = false`; `IsVoid` is a computed property.
- The **empty particle set** is implicit and reserved for internal use.
- `ParticleSymbol` is a short string used as a mass-conservation proxy. Species with the same symbol may
  exchange without violating mass conservation.
- `IsVacancy` marks pseudo-particles that represent empty states. Validation warns if a vacancy and its default
  occupation both have non-zero charge (the original documented pitfall).
- Particle names are unique (case-insensitive) within the project; symbols are not required to be unique.
- Up to 63 custom particles; the 64th slot is the void.
- `ParticleSet` order is semantic: the first particle is the default occupation of a stable site.
- Particle sets may not contain the void; unstable sites implicitly default to void.
- Particle sets are immutable; editing produces a new set and revalidates references.

### 2. Structure model

```csharp
public sealed record StructureModel(
    SpaceGroupRef SpaceGroup,
    CellParameters Cell,
    ImmutableArray<CellSite> Sites,
    ImmutableArray<CellDummyPosition> DummyPositions);

public sealed record SpaceGroupRef(
    int Number,
    AxisSetting Axis,
    OriginChoice Origin,
    string HermannMauguin);

public sealed record CellParameters(
    LengthAngstrom A, LengthAngstrom B, LengthAngstrom C,
    AngleDegrees Alpha, AngleDegrees Beta, AngleDegrees Gamma);

public sealed record CellSite(
    CellSiteId Id,
    string Name,
    Fractional3 Fractional,
    ParticleSetId ParticleSetId,
    SiteStability Stability);

public sealed record CellDummyPosition(
    Guid Id,
    string Name,
    Fractional3 Fractional);
```

- `SiteStability` is `Stable` or `Unstable`.
- Reference positions must be in `[0,1)` at definition time; they are trimmed with a clear diagnostic if
  within `1e-10` of a boundary.
- Stable sites require a particle set. Unstable sites also require a set (the transition-state occupation
  options), but their default occupation is void.
- The space group is selected from the catalog by number/axis/origin; the display string is derived, not
  independently editable.
- Cell parameters are normalized according to the crystal system before validation. The UI locks dependent
  fields; the API rejects inconsistent values rather than silently overwriting them.
- Dummy positions are visual helpers, may coincide with sites, and are excluded from all simulation
  compilation.

### 3. Derived structure data (preview, not compiled)

The model layer exposes a `StructurePreview` for the UI and validation:

```csharp
public sealed record StructurePreview(
    ImmutableArray<ExpandedSite> PositionList,
    UnitCellVectorEncoder Encoder,
    ImmutableArray<Cartesian3> CartesianPositions,
    double CellVolumeAngstrom3,
    ImmutableArray<SiteOrbitInfo> Orbits);
```

- `PositionList` is the canonical P1 expansion from step 02.
- It is cached by a content hash of the structure model and invalidated on edit.
- The UI shows the orbit size, multiplicity, fractional/Cartesian coordinates, and stability for each site.
- The preview is not the compiled model; step 08 produces the authoritative compiled structure.

### 4. Validation rules

| Code | Severity | Rule |
| --- | --- | --- |
| `PAR001` | Error | Particle name empty or duplicate |
| `PAR002` | Error | More than 63 custom particles |
| `PAR003` | Error | Particle charge outside −1000..1000 or non-finite |
| `PAR004` | Warning | Vacancy and default occupation both charged |
| `PAR005` | Error | Particle set empty or contains void |
| `PAR006` | Error | Particle set references unknown/deprecated particle |
| `PAR007` | Error | More than 100 particle sets |
| `STR001` | Error | Space-group number outside 1..230 or setting not found |
| `STR002` | Error | Cell length outside 0.1..1000 Å |
| `STR003` | Error | Cell angles violate the crystal-system constraints |
| `STR004` | Error | Reference site outside `[0,1)` after trim |
| `STR005` | Error | Site name empty or duplicate |
| `STR006` | Error | Stable/unstable site has no particle set |
| `STR007` | Error | Two sites occupy the same P1 orbit position |
| `STR008` | Warning | Site orbit is large (> 192 positions) |
| `STR009` | Warning | Total P1 positions exceed the performance budget |
| `STR010` | Error | Dummy position references invalid coordinates |
| `STR011` | Warning | Site cannot be occupied by any particle |

- Site-overlap detection uses exact rational coordinates after P1 expansion; no tolerance-based hash sets.
- Duplicate detection compares the full orbit, not only the seed position.
- A site whose orbit overlaps another site is an error because it makes particle assignment ambiguous.
- Structure validation also computes statistics used by step 07: orbit sizes, total positions, site counts.

### 5. Public API

```csharp
public interface IParticleQuery
{
    ImmutableArray<Particle> Particles { get; }
    Particle VoidParticle { get; }
    Particle? Find(ParticleId id);
    ParticleSet? Find(ParticleSetId id);
}

public interface IStructureQuery
{
    StructureModel Structure { get; }
    SpaceGroupSetting SpaceGroup { get; }
    StructurePreview Preview { get; }
    CellSite? Find(CellSiteId id);
}
```

- Query interfaces are implemented by the immutable `Project` snapshot, not by mutable managers.
- The UI and compiler consume these interfaces.
- Mutations go through `ProjectEditor` methods such as `AddParticle`, `UpdateParticle`, `AddCellSite`, which
  return a new project and diagnostics.

## Implementation tasks

- [ ] Implement `Particle`, `ParticleSet`, and the implicit void/empty objects.
- [ ] Implement `StructureModel`, `SpaceGroupRef`, `CellParameters`, `CellSite`, `CellDummyPosition`.
- [ ] Implement crystal-system normalization and validation.
- [ ] Implement `StructurePreview` in `Mocassin.Symmetry` using the symmetry engine (not in Core).
- [ ] Implement P1 overlap detection.
- [ ] Implement `IParticleQuery` and `IStructureQuery` over the project snapshot.
- [ ] Implement editor operations for particles, sets, cell parameters, sites, and dummies.
- [ ] Add sample definitions for ceria and barium zirconate.
- [ ] Add diagnostics and help text for every code.

## Acceptance criteria

- [ ] A user can define ceria: 225, a=5.41 Å, Ce at (0,0,0), O at (0.25,0.25,0.25), and an unstable O site
  at (0.5,0.25,0.25).
- [ ] The preview shows the correct P1 orbit sizes for each site.
- [ ] Overlapping sites are rejected with a path to the offending site.
- [ ] Crystal-system constraints are enforced without silently overwriting user input.
- [ ] The structure preview is deterministic and cached by content hash.
- [ ] All diagnostics are reachable from the CLI and UI.
- [ ] Documentation written

## Documentation

At this point, document particles and crystal structure:

- `docs/user-guide/particles.md` — particles, particle sets, void/default semantics, limits.
- `docs/user-guide/structure.md` — space group, cell parameters, sites, stability, dummies.
- `docs/physics/symmetry.md` — site orbits, P1 preview, and why positions are fixed.
- `docs/development/architecture.md` — particle/structure domain APIs and editor operations.

## Tests

| Test | Assertion |
| --- | --- |
| Particle limits | 63 custom particles accepted; 64 rejected |
| Void handling | Void cannot be added to a set; void index is 0 |
| Symbol/charge | Mass symbol preserved; charge validated |
| Crystal systems | Valid/invalid parameter sets for all seven systems |
| Site overlap | Same orbit, special positions, and near-boundary cases |
| Preview | Orbit sizes and Cartesian positions match hand calculations |
| Determinism | Same structure yields identical preview bytes/hash |
| Editing | Add/update/remove site produces a valid new project or diagnostics |

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Site overlap detection is expensive for large cells | Exact orbit dedup and early exit; cache by structure hash |
| Crystal-system coupling confuses users | UI locks dependent fields and explains constraints |
| Particle symbol semantics are implicit | Document and validate; show mass-conservation groups in the editor |
| Preview diverges from compiled model | Compile from the same symmetry engine and compare in step 08 tests |

## Done when

Particles, sets, cell parameters, sites, and dummies can be authored, validated, previewed, and queried, and
the ceria structure produces the expected P1 expansion.
