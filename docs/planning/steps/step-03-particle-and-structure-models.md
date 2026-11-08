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
- **Step tag:** —
- **Audit:** Not requested
- **Design review:** [Ready for review](../reviews/step-03.md)
- **Last updated:** 2026-11-08

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

### 1. Model composition

The existing `ModelDefinition` is an empty root. Step 03 gives it the first two sections:

```csharp
public sealed record ParticleModel(
    ImmutableArray<Particle> Particles,
    ImmutableArray<ParticleSet> ParticleSets);

public sealed record ModelDefinition(
    ParticleModel Particles,
    StructureModel Structure);
```

- `ParticleModel` stores only authored custom particles and particle sets; the void particle and empty set
  remain implicit.
- `ModelDefinition` remains the root model envelope. Later steps add transition, energy, lattice, and
  simulation sections.
- `ProjectJsonContext`, `ProjectJson`, `ProjectSchema`, and `ProjectValidator` are extended for the new
  sections. The generated manifest/schema is the cross-language contract; the project `schemaVersion` remains
  1 because the fields are additive.

### 2. Particle model

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

### 3. Structure model

```csharp
public sealed record StructureModel(
    SpaceGroupRef SpaceGroup,
    CellParameters Cell,
    ImmutableArray<CellSite> Sites,
    ImmutableArray<CellDummyPosition> DummyPositions);

public sealed record SpaceGroupRef(
    int Number,
    AxisSetting Axis,
    OriginChoice Origin);

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
    CellDummyPositionId Id,
    string Name,
    Fractional3 Fractional);
```

All types in this block live in `Mocassin.Core`. `SpaceGroupRef` is the persisted reference; the resolved
`SpaceGroupSetting`, `StructurePreview`, and catalog-dependent validation are Symmetry services, so Core does
not reference `Mocassin.Symmetry`.

- `SiteStability` is `Stable` or `Unstable`; the persisted JSON uses `"stability": "stable"` or `"unstable"`.
- `CellDummyPositionId` is a typed ID in `Mocassin.Core.Ids`, like the other persisted object IDs.
- Reference positions must be in `[0,1)` at definition time; they are trimmed with a clear diagnostic if
  within `1e-10` of a boundary.
- Stable sites require a particle set. Unstable sites also require a set (the transition-state occupation
  options), but their default occupation is void.
- The space group is selected from the catalog by `(number, axis, origin)`. The Hermann–Mauguin symbol, Hall
  symbol, crystal system, operations, and data version are derived from the catalog and are not stored in the
  project.
- Reference positions remain `Fractional3` doubles in the project; Symmetry converts them to the exact
  `24 × 10^12` grid for P1 expansion, overlap detection, and canonical ordering.
- Cell parameters are validated against the crystal system. The UI may lock dependent fields as a convenience,
  but the API rejects inconsistent values with `STR003`; it never silently overwrites user input.
- Dummy positions are visual helpers, may coincide with sites, and are excluded from all simulation
  compilation.

### 4. Derived structure data (preview, not compiled)

The model layer exposes a `StructurePreview` for the UI and validation:

```csharp
public sealed record StructurePreview(
    ImmutableArray<ExpandedSite> PositionList,
    UnitCellVectorEncoder Encoder,
    ImmutableArray<Cartesian3> CartesianPositions,
    double CellVolumeAngstrom3,
    ImmutableArray<SiteOrbitInfo> Orbits);
```

`StructurePreview` and `UnitCellVectorEncoder` are Symmetry types. A Symmetry service builds the preview from
a Core `StructureModel` and the requested cell counts `(A, B, C)`; the Core model never holds a preview, a
resolved catalog setting, or supercell dimensions.

- `PositionList` is the canonical P1 expansion from step 02.
- `PositionList` is in ascending canonical `P` order. `ExpandedSite` exposes the P index and its `Vector4I`
  encoding; `Encoder` owns the fractional ↔ `Vector4I` mapping and the linear-index check.
- The preview takes the requested cell counts `(A, B, C)` from its caller and rejects a structure whose total
  supercell product `A*B*C*P` exceeds the signed 32-bit index cap. The persisted `StructureModel` does not own
  supercell dimensions; the build/job model supplies them.
- It is cached by a content hash of the structure model and invalidated on edit.
- The UI shows the orbit size, multiplicity, fractional/Cartesian coordinates, and stability for each site.
- The preview is not the compiled model; step 08 produces the authoritative compiled structure.

### 5. Validation rules

| Code | Severity | Rule |
| --- | --- | --- |
| `PAR001` | Error | Particle name empty or duplicate |
| `PAR002` | Error | More than 63 custom particles |
| `PAR003` | Error | Particle charge outside −1000..1000 or non-finite |
| `PAR004` | Warning | Vacancy and default occupation both charged |
| `PAR005` | Error | Particle set empty or contains void |
| `PAR006` | Error | Particle set references unknown/deprecated particle |
| `PAR007` | Error | More than 100 particle sets |
| `STR001` | Error | Space-group number outside 1..230 or `(number, axis, origin)` not found |
| `STR002` | Error | Cell length outside 0.1..1000 Å |
| `STR003` | Error | Cell angles are not strictly between 0° and 180°, the Gram determinant is not positive, the condition number exceeds `1e12`, or the crystal-system constraints fail |
| `STR004` | Error | Reference site outside `[0,1)` after trim |
| `STR005` | Error | Site name empty or duplicate |
| `STR006` | Error | Stable/unstable site has no particle set |
| `STR007` | Error | Two sites occupy the same P1 orbit position |
| `STR008` | Warning | Site orbit is large (> 192 positions) |
| `STR009` | Warning | Total P1 positions exceed the performance budget |
| `STR010` | Error | Dummy position references invalid coordinates |
| `STR011` | Warning | Site cannot be occupied by any particle |
| `PROJ034` | Warning | Project symmetry metadata differs from the embedded catalog/contract versions |

- `PROJ034` is a project-level warning finalized in Step 07. It does not block loading; the compiled payload
  records the authoritative versions.

Diagnostic ownership follows the assembly boundary. Core emits diagnostics that depend only on the project
graph and primitive types: `STR004`, `STR005`, `STR006`, `STR010`, and `STR011`. Symmetry emits diagnostics
that require the catalog, cell geometry, or P1 expansion: `STR001`, `STR002`, `STR003`, `STR007`, `STR008`,
and `STR009`. A composed validator runs both and returns one stable, sorted list.

- An unknown `axis` or `origin` enum string is malformed JSON (`PROJ002`), not a valid but unsupported setting.
- `uniqueAxisA` is a known reserved enum value: the parser accepts it, and the validator rejects it with
  `STR001`. It is not treated as malformed JSON.
- `STR001` reports the allowed axis/origin combinations for the number when the catalog can list them.
- Site-overlap detection uses exact grid coordinates after P1 expansion; no tolerance-based hash sets. The
  grid is the `24 × 10^12` fractional grid defined in Step 02.
- Duplicate detection compares the full orbit, not only the seed position.
- A site whose orbit overlaps another site is an error because it makes particle assignment ambiguous.
- Structure validation also computes statistics used by step 07: orbit sizes, total positions, site counts.

### 6. Public API

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
    CellSite? Find(CellSiteId id);
}

// Mocassin.Symmetry
public interface IStructurePreviewService
{
    SpaceGroupSetting Resolve(SpaceGroupRef reference);
    StructurePreview CreatePreview(StructureModel structure, int cellCountA, int cellCountB, int cellCountC);
}

public interface IStructureValidator
{
    ImmutableArray<Diagnostic> Validate(StructureModel structure);
}
```

- Core query interfaces are implemented by the immutable `Project` snapshot, not by mutable managers.
- `IStructureQuery` exposes only Core model types.
- `IStructurePreviewService` and `IStructureValidator` are Symmetry services. They accept a Core
  `StructureModel`, resolve `SpaceGroupRef` through the catalog, and return a preview or diagnostics.
- The CLI, UI, and compiler compose the Core query with the Symmetry services; Core never references
  `Mocassin.Symmetry`.
- A composed validator in the composition layer (`Mocassin.Packages` or `Mocassin.Cli`) runs
  `ProjectValidator` and `IStructureValidator` and merges their diagnostics.
- The UI and compiler consume these interfaces.
- Mutations go through `ProjectEditor` methods such as `AddParticle`, `UpdateParticle`, `AddCellSite`, which
  return a new project and diagnostics.

## Code layout

The new code follows the structure policy: PascalCase domain folders, namespaces that match folders, file names
that match the primary type, no new root-level files, and no folder above 20 direct files.

```text
Mocassin.Core/
  Ids/
    TypedIds.cs                         # add CellDummyPositionId; reuse ParticleId, ParticleSetId, CellSiteId
  Model/
    ModelDefinition.cs                  # add Particles and Structure sections
    CellParameters.cs                   # existing type, reused by StructureModel
    Particles/
      Particle.cs
      ParticleSet.cs
      ParticleModel.cs
      IParticleQuery.cs
    Structure/
      StructureModel.cs
      SpaceGroupRef.cs
      CellSite.cs
      CellDummyPosition.cs
      SiteStability.cs
      IStructureQuery.cs
  Editing/
    ProjectEditor.cs                    # add particle/set/site/dummy/parameter operations
  Diagnostics/
    ParticleDiagnosticCodes.cs          # PAR001-PAR007
    StructureDiagnosticCodes.cs         # Core-side STR004, STR005, STR006, STR010, STR011
  Project/
    ProjectJson.cs
    ProjectJsonContext.cs
    ProjectSchema.cs
    ProjectValidator.cs

Mocassin.Symmetry/
  Diagnostics/
    StructureDiagnosticCodes.cs         # existing; extend with STR001 and STR008
  Structure/
    StructurePreview.cs
    StructurePreviewService.cs
    StructureValidator.cs
    IStructurePreviewService.cs
    IStructureValidator.cs
```

Test files mirror the source concept in `Mocassin.Core.Tests` and `Mocassin.Symmetry.Tests`:

- `Particles/ParticleTests.cs`, `Particles/ParticleSetTests.cs`
- `Structure/StructureModelTests.cs`, `Structure/CellValidationTests.cs`
- `Structure/StructurePreviewTests.cs`, `Structure/StructureValidatorTests.cs`

`CellParameters` stays in `Model/` because it is an existing Step 01 type; it is referenced from
`Model/Structure/StructureModel.cs` with a matching namespace. If `TypedIds.cs` approaches the 800-line limit,
split the new IDs into `Ids/StructureIds.cs`.

## Implementation tasks

- [ ] Extend `ModelDefinition` with `ParticleModel` and `StructureModel`; update `ProjectJsonContext`,
  `ProjectJson`, `ProjectSchema`, and `ProjectValidator`.
- [ ] Implement `Particle`, `ParticleSet`, `ParticleModel`, and the implicit void/empty objects.
- [ ] Implement `StructureModel`, `SpaceGroupRef`, `CellParameters`, `CellSite`, `CellDummyPosition`.
- [ ] Add the `CellDummyPositionId` typed ID and JSON converter.
- [ ] Implement crystal-system normalization and validation.
- [ ] Implement `StructurePreview` and `UnitCellVectorEncoder` in `Mocassin.Symmetry` using the symmetry
  engine (not in Core).
- [ ] Implement `StructurePreviewService` and `StructureValidator` in `Mocassin.Symmetry`; pass the requested
  `(A, B, C)` cell counts into the preview.
- [ ] Implement P1 overlap detection in the Symmetry validator.
- [ ] Implement `IParticleQuery` and `IStructureQuery` over the project snapshot.
- [ ] Implement editor operations for particles, sets, cell parameters, sites, and dummies.
- [ ] Add sample definitions for ceria and barium zirconate.
- [ ] Add Core-side particle and structure diagnostic codes, extend the Symmetry structure codes, and add help
  text for every code.
- [ ] Update `contracts/manifest.json` with the `structure` section and optional envelope `symmetry` metadata;
  regenerate `contracts/generated/schema/project-v1.schema.json`. The project `schemaVersion` remains 1 because
  the fields are additive.
- [ ] Add project fixtures with and without the optional `symmetry` metadata and test `PROJ034`.
- [ ] Add XML docs, update `PublicAPI.Unshipped.txt`, and refresh the ApiCompat baseline for every new public
  type and member.
- [ ] Add the new user-guide pages to `zensical.toml` navigation.

## Acceptance criteria

- [ ] A user can define ceria: 225, a=5.41 Å, Ce at (0,0,0), O at (0.25,0.25,0.25), and an unstable O site
  at (0.5,0.25,0.25).
- [ ] The preview shows the correct P1 orbit sizes for each site.
- [ ] Overlapping sites are rejected with a path to the offending site.
- [ ] Crystal-system constraints are enforced without silently overwriting user input.
- [ ] The structure preview is deterministic and cached by content hash.
- [ ] All diagnostics are reachable from the CLI and UI.
- [ ] The generated project schema accepts the structure and optional symmetry fields, and fixtures cover both.
- [ ] Public API files and the ApiCompat baseline are current.
- [ ] Documentation written

## Documentation

At this point, document particles and crystal structure:

- `docs/user-guide/particles.md` — particles, particle sets, void/default semantics, limits.
- `docs/user-guide/structure.md` — space group, cell parameters, sites, stability, dummies.
- `docs/physics/symmetry.md` — site orbits, P1 preview, and why positions are fixed.
- `docs/development/architecture.md` — particle/structure domain APIs, editor operations, and the
  Core/Symmetry preview and validation boundary.
- `docs/reference/project-format.md` — cell parameter constraints, `STR002`/`STR003`, and canonical cell rules
  (added with the structure section).

## Tests

| Test | Assertion |
| --- | --- |
| Model envelope | Empty, particle-only, structure-only, and combined models round-trip through the project JSON context |
| Particle limits | 63 custom particles accepted; 64 rejected |
| Void handling | Void cannot be added to a set; void index is 0 |
| Symbol/charge | Mass symbol preserved; charge validated |
| Crystal systems | Valid/invalid sets for all seven systems, both trigonal axis descriptions, and unique-axis B/C; `uniqueAxisA` is rejected |
| Cell determinant | Near-degenerate and ill-conditioned cells are rejected; valid cells have `det G > 0` and positive volume |
| Cell vectors | Triclinic regression, hexagonal γ=120°, rhombohedral α=60°, and right-handedness |
| Site overlap | Same orbit, special positions, near-boundary cases, and exact grid equality |
| Preview | Orbit sizes and Cartesian positions match hand calculations |
| Determinism | Same structure yields identical preview bytes/hash |
| P order | Preview positions are in canonical P order; shuffling input does not change P |
| Index cap | A supercell product above `int.MaxValue` is rejected |
| Editing | Add/update/remove site produces a valid new project or diagnostics |
| Assembly boundary | `Mocassin.Core` has no reference to `Mocassin.Symmetry`; preview and catalog validation compose over Core models |
| Space-group reference | `(number, axis, origin)` round-trips for every supported enum value; HM/Hall are derived; unknown combinations produce `STR001`; reserved `uniqueAxisA` is rejected |
| Symmetry metadata | Matching versions load silently; mismatched catalog/contract emits `PROJ034`; fixtures with and without the optional field load |
| Sample P order | Ceria and barium zirconate preview P indices are stable and match the Step 02 conventions |

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Site overlap detection is expensive for large cells | Exact grid orbit dedup and early exit; cache by structure hash |
| Crystal-system coupling confuses users | UI locks dependent fields and explains constraints; API rejects inconsistent values |
| Cell-parameter tolerance drift | Named, unit-specific tolerances and boundary tests |
| Particle symbol semantics are implicit | Document and validate; show mass-conservation groups in the editor |
| Preview diverges from compiled model | Compile from the same symmetry engine and compare in step 08 tests |
| Redundant setting fields drift | Persist only `(number, axis, origin)`; derive HM/Hall/system from the catalog |
| P-index drift | Golden P-order tests, shuffle invariance, and the exact grid contract |
| Catalog/contract mismatch | `PROJ034` warning, no automatic rewrite, and the compiled payload records the actual versions |

## Done when

Particles, sets, cell parameters, sites, and dummies can be authored, validated, previewed, and queried, and
the ceria structure produces the expected P1 expansion.
