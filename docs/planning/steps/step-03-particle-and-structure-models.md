# Step 03 — Particle and Structure Models

**Phase:** B. Model definition  
**Depends on:** 02
**Delivers:** Particle, particle-set, space-group, cell-parameter, cell-site, and dummy-position models with
validation and a stable public API.

## Implementation status

- **Status:** In progress
- **Step branch:** `step/03-particle-and-structure-models`
- **Step CI:** —
- **Main merge:** —
- **Step tag:** —
- **Audit:** Not requested
- **Design review:** [Ready with notes — accepted](../reviews/step-03.md)
- **Feature list:** Approved 2026-11-08; step branch created
- **Last updated:** 2026-11-08

| Feature | Branch | Status | Local gates | Step CI | Squash commit |
| --- | --- | --- | --- | --- | --- |
| `particle-model` | `feature/step-03-particle-and-structure-models/particle-model` | Done | `dotnet`, `structure`, `api` pass; Core tests 199/199 | — | `3974596` |
| `structure-model` | `feature/step-03-particle-and-structure-models/structure-model` | Done | `dotnet`, `structure`, `api` pass; Core tests 223/223 | — | `dd642d6` |
| `model-envelope-and-contracts` | `feature/step-03-particle-and-structure-models/model-envelope-and-contracts` | Done | `dotnet`, `structure`, `api`, `contracts`, `docs` pass; Core tests 242/242 | — | `7c4e748` |
| `structure-preview-and-benchmark` | `feature/step-03-particle-and-structure-models/structure-preview-and-benchmark` | Done | `dotnet`, `structure`, `api`, `contracts`, `docs` pass; Core 242/242, Symmetry 145/145; benchmark 15.8 µs / 6.7 µs | — | `67e39e1` |
| `queries-and-editing` | `feature/step-03-particle-and-structure-models/queries-and-editing` | Done | `dotnet`, `structure`, `api`, `contracts`, `docs` pass; Core 261/261, Symmetry 146/146 | — | `8a75bc6` |
| `validation-composition` | `feature/step-03-particle-and-structure-models/validation-composition` | Done | `dotnet`, `structure`, `api`, `contracts`, `docs` pass; Core 261/261, Symmetry 146/146, Packages 40/40 | — | `18940a1` |
| `samples-and-docs` | `feature/step-03-particle-and-structure-models/samples-and-docs` | Awaiting approval | `dotnet`, `structure`, `api`, `contracts`, `docs` pass; Core 263/263, Symmetry 148/148, Packages 40/40 | — | — |
| `closeout` | `feature/step-03-particle-and-structure-models/closeout` | Planned | — | — | — |

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

The existing `ModelDefinition` is an empty root. Step 03 gives it the first three persisted sections:

```csharp
public sealed record ModelDefinition(
    ImmutableArray<Particle> Particles,
    ImmutableArray<ParticleSet> ParticleSets,
    StructureModel Structure);

public sealed record ParticleModel(
    ImmutableArray<Particle> Particles,
    ImmutableArray<ParticleSet> ParticleSets);
```

- `ModelDefinition` is the root model envelope and its JSON shape is exactly
  `model.particles`, `model.particleSets`, and `model.structure`. Later steps add transition, energy, lattice,
  and simulation sections directly at the model root.
- `ParticleModel` is an in-memory grouping for queries and tests. It is not a persisted property of
  `ModelDefinition`; persisting it would nest the arrays under `model.particles.particles` and violate the
  project contract.
- `ModelDefinition.Empty` is the default for a project with `"model": {}`: no particles or sets, and
  `StructureModel.Default` (P1, `a=b=c=1 Å`, all angles 90°, no sites or dummies, name `New Structure`).
  Missing model sections therefore load as defaults; the project `schemaVersion` remains 1 because the new
  fields are additive and the model shape is still pre-release. Use optional init properties or a
  parameterless factory so `System.Text.Json` can construct the default model from `{}`; do not rely on a
  positional constructor passing null/default `ImmutableArray` values.
- `ProjectJsonContext`, `ProjectJson`, `ProjectSchema`, and `ProjectValidator` are extended for the new
  sections. The generated manifest/schema is the cross-language contract; `project-minimal.mocprj` and
  `samples/blank/blank.mocprj` must continue to load unchanged.

#### Persisted JSON mapping

The following rules are part of the v1 project contract and need explicit converters and tests:

- `Fractional3` is written as a three-number array `[a, b, c]` and read as exactly three finite numbers.
  Do not serialize it as `{ "a": …, "b": …, "c": … }`.
- `AxisSetting`, `OriginChoice`, and `SiteStability` are written as camelCase strings. Reuse
  `CamelCaseEnumJsonConverter<T>`; do not rely on a default numeric enum representation.
- `Particle.IsVoid` is a computed property and is marked `[JsonIgnore]`. It is never written or read as a
  project field.
- `ParticleId.Void` and `ParticleSetId.Empty` are reserved identifiers. Authored particles and particle sets
  must not use them; the reserved values are created only by the model and compiler.
- Canonical model JSON sorts `Particles`, `ParticleSets`, `Sites`, and `DummyPositions` by their stable ID.
  `ParticleSet.ParticleIds` keeps its authored order because the first particle is the default occupation.
  `ProjectHasher.Compute` hashes this canonical model shape, so shuffling authored arrays must not change the
  model hash.
- `ParticleSets` array order inside `ModelDefinition` is not semantic; only `ParticleSet.ParticleIds` order is.

#### Project envelope symmetry metadata

The optional project envelope `symmetry` object needs a Core type and an explicit JSON mapping:

```csharp
public sealed record ProjectSymmetryMetadata(
    string? SpaceGroupDataVersion,
    string? SpaceGroupDataSha256,
    string? SymmetryContractVersion);
```

- `Project` gains `ProjectSymmetryMetadata? Symmetry`; `ProjectDto` gains an optional `symmetry` property.
  `ProjectJsonContext`, the reader/writer, and `ProjectCanonicalizer` include it.
- Missing `symmetry` is `null` and accepted. A present-but-partial object compares only the fields it contains.
- `ProjectHasher.ComputeDocument` includes the metadata; `ProjectHasher.Compute` hashes only the model section.
- `Mocassin.Core` stores and serializes the metadata but never reads the embedded catalog.
  `Mocassin.Packages.Projects.ProjectValidationService` compares it with
  `SymmetryVersionInfo.FromCatalog(SpaceGroupCatalog.Default)` and emits `ProjectDiagnosticCodes.SymmetryMetadataMismatch`
  (`PROJ034`) when a present value differs.

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

- The **void particle** is implicit, reserved, and not stored in `Particles`. It has the well-known reserved
  `ParticleId.Void`, symbol `∅`, charge 0, and `IsVacancy = false`; `IsVoid` is a computed property. The
  compiled index 0 is assigned by Step 08, not by the Step 03 model.
- The **empty particle set** is implicit and reserved for internal use.
- An authored particle or particle set that reuses `ParticleId.Void` or `ParticleSetId.Empty` is `PAR010`
  error. These IDs are reserved for the implicit objects and the compiler; they are not user-visible model
  objects.
- `Particle.Symbol` is a short string used as a mass-conservation proxy. Species with the same symbol may
  exchange without violating mass conservation. The recommended v1 pattern is
  `^[A-Z][A-Za-z0-9+\-()]{0,10}$`; a symbol that does not match it emits `PAR008` warning, not an error, so
  existing scientific labels remain usable.
- `IsVacancy` marks pseudo-particles that represent empty states. Validation warns if a vacancy and its default
  occupation both have non-zero charge (the original documented pitfall).
- Particle names are unique (case-insensitive) within the project; symbols are not required to be unique.
- Up to 63 custom particles; the 64th slot is the void.
- `ParticleSet.ParticleIds` order is semantic: the first particle is the default occupation of a stable site.
  The containing `ParticleSets` array order is not semantic.
- Particle sets may not contain the void; unstable sites implicitly default to void.
- Particle-set names are unique (case-insensitive); empty or duplicate names are `PAR011` error. Repeated
  particle IDs inside one set are `PAR012` error.
- Two authored particle sets with the same ordered `ParticleIds` are allowed but emit `PAR009` warning, because
  duplicate sets usually indicate an editing mistake. They are not merged automatically.
- Particle sets are immutable; editing produces a new set and revalidates references.
- `ParticleModel` (in-memory) exposes only authored particles and sets; the void particle and empty set remain
  implicit.

### 3. Structure model

```csharp
public sealed record StructureModel(
    string Name,
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

- `StructureModel.Name` is the user-visible structure name preserved from the original `IStructureInfo`
  concept. It is persisted as `structure.name`; `New Structure` is the deliberate new-project/UI default
  (the original model default was `Unnamed`). An empty or whitespace-only name is `STR013` error.
- `SiteStability` is `Stable` or `Unstable`; the persisted JSON uses `"stability": "stable"` or `"unstable"`.
- `CellDummyPositionId` is a typed ID in `Mocassin.Core.Ids`, like the other persisted object IDs; it reuses the
  generic typed-ID JSON converter.
- A structure may define at most 1000 reference sites; more is `STR012` error. This preserves the original
  `BasePositionCount` hard limit.
- Reference positions must be inside `[0,1)` before trimming. A component within `FractionalTrimTolerance`
  (`1e-10`) of 0 or 1 is snapped and wrapped to the equivalent canonical value; a component outside the
  tolerance is `STR004` error, not silently wrapped.
- Stable sites require a particle set. Unstable sites also require a set (the transition-state occupation
  options), but their default occupation is void. A site whose `ParticleSetId` is `ParticleSetId.Empty` or the
  default value is missing a set and emits `STR006`; an ID that does not resolve to any set is `PROJ010`.
- The space group is selected from the catalog by `(number, axis, origin)`. The Hermann–Mauguin symbol, Hall
  symbol, crystal system, operations, and data version are derived from the catalog and are not stored in the
  project.
- Reference positions remain `Fractional3` doubles in the project; Symmetry converts them to the exact
  `24 × 10^12` grid for P1 expansion, overlap detection, and canonical ordering.
- Cell parameters are validated against the crystal system. The UI may lock dependent fields as a convenience,
  but the API rejects inconsistent values with `STR003`; it never silently overwrites user input.
- Dummy positions are visual helpers, may coincide with sites, and are excluded from all simulation
  compilation. Dummy names are unique (case-insensitive); empty or duplicate names are `STR015` error.

### 4. Derived structure data (preview, not compiled)

The model layer exposes a `StructurePreview` for the UI and validation:

```csharp
public sealed record StructurePreview(
    ImmutableArray<ExpandedSite> PositionList,
    UnitCellVectorEncoder Encoder,
    ImmutableArray<Cartesian3> CartesianPositions,
    double CellVolumeAngstrom3,
    ImmutableArray<SiteOrbitInfo> Orbits,
    int CellCountA,
    int CellCountB,
    int CellCountC);
```

`StructurePreview` and `UnitCellVectorEncoder` are Symmetry types. A Symmetry service builds the preview from
a Core `StructureModel` and the requested cell counts `(A, B, C)`; the Core model never holds a preview, a
resolved catalog setting, or supercell dimensions.

- `PositionList` is the canonical P1 expansion from step 02.
- `PositionList` is in ascending canonical `P` order. `ExpandedSite` exposes the P index and its `Vector4I`
  encoding; `Encoder` owns the fractional ↔ `Vector4I` mapping and the linear-index check.
- The preview takes the requested cell counts `(A, B, C)` from its caller and rejects a structure whose total
  supercell product `A*B*C*P` exceeds the signed 32-bit index cap. The persisted `StructureModel` does not own
  supercell dimensions; the build/job model supplies them. The counts are echoed on the preview so a preview is
  self-describing and cannot be confused with a different supercell.
- `structureHash` is `ProjectHasher.ComputeStructure(structure)`: SHA-256 over the canonical structure-model
  projection (name, space group, cell, sites, dummies) with the same ID sorting as the project hash. It is not
  the whole-model hash.
- It is cached by `(structureHash, cellCountA, cellCountB, cellCountC, catalogDataVersion, catalogDataSha256,
  symmetryContractVersion)` and invalidated when any component changes. `StructurePreviewService` accepts an
  optional `SymmetryVersionInfo` for tests so a different catalog/contract version produces a different key
  even when the operations are unchanged. The cache is scoped to the preview service, bounded like the Step 02
  caches, and thread-safe.
- The preview service never throws for invalid user data. `TryCreatePreview` returns diagnostics for an invalid
  space-group reference, cell, non-finite coordinate, or index-cap overflow; an invalid structure produces no
  partial preview. Index-cap overflow is `STR014` error and is checked with
  `UnitCellVectorEncoder.IsLatticeSizeSupported` before allocation.
- `StructurePreviewService` and `StructureValidator` use a structured orbit reference input:
  `StructureSiteReference(CellSiteId Id, Fractional3 Position)` and the additive
  `UnitCellVectorEncoder.TryCreate` overload that returns `OrbitOverlap(FirstSiteId, SecondSiteId,
  Fractional3 Position)`. The overload emits `STR007` diagnostics with `ModelPath("cellSite", id)` and related
  paths, so the validator never parses message text and never duplicates the orbit comparison. The existing
  Step 02 overload remains for callers that do not need IDs.
- Determinism is asserted on the canonical P1 position list, orbit data, cell vectors, and cell counts. There is
  no persisted preview byte format; the test must not invent one.
- The UI shows the orbit size, multiplicity, fractional/Cartesian coordinates, and stability for each site.
- The preview is not the compiled model; step 08 produces the authoritative compiled structure.

#### Preview benchmark

- Add `StructurePreviewCeria` and `StructurePreviewBariumZirconate` BenchmarkDotNet cases to
  `Mocassin.Benchmarks/Structure/StructurePreviewBenchmarks.cs`.
- Fixtures: the sample ceria and barium zirconate structures, P1 counts recorded in the benchmark output.
- Command: `dotnet run -c Release --project src/dotnet/Mocassin.Benchmarks -- --filter "*StructurePreview*"`.
- Record the runner class and raw results in `tests/reference/bench/baselines.json` and
  `tests/reference/bench/`; target mean < 100 ms on `linux-x64-github-ubuntu-latest-dotnet10`.
- Tier: Nightly/release. Do not assert wall-clock time in a PR-fast test.

### 5. Validation rules

| Code | Severity | Rule |
| --- | --- | --- |
| `PAR001` | Error | Particle name empty or duplicate (case-insensitive) |
| `PAR002` | Error | More than 63 custom particles |
| `PAR004` | Warning | Vacancy and default occupation both charged |
| `PAR005` | Error | Particle set empty or contains void |
| `PAR006` | Error | Particle set references an unknown particle |
| `PAR007` | Error | More than 100 particle sets |
| `PAR008` | Warning | Particle symbol is empty, too long, or does not match the recommended pattern |
| `PAR009` | Warning | Two authored particle sets have the same ordered particle list |
| `PAR010` | Error | Authored particle or particle set reuses `ParticleId.Void` or `ParticleSetId.Empty` |
| `PAR011` | Error | Particle-set name empty or duplicate (case-insensitive) |
| `PAR012` | Error | Particle set contains the same particle ID more than once |
| `STR001` | Error | Space-group number outside 1..230 or `(number, axis, origin)` not found |
| `STR002` | Error | Cell length outside 0.1..1000 Å |
| `STR003` | Error | Cell angles are not strictly between 0° and 180°, the Gram determinant is not positive, the condition number exceeds `1e12`, or the crystal-system constraints fail |
| `STR004` | Error | Reference site is outside `[0,1)` before trim, beyond `FractionalTrimTolerance` |
| `STR005` | Error | Site name empty or duplicate (case-insensitive) |
| `STR006` | Error | Site has no particle set (`ParticleSetId.Empty` or the default value) |
| `STR007` | Error | Two sites occupy the same P1 orbit position |
| `STR009` | Warning | Total P1 positions exceed the 10,000-position performance budget |
| `STR010` | Error | Dummy position has non-finite coordinates or is outside `[0,1)` before trim |
| `STR012` | Error | More than 1000 reference sites |
| `STR013` | Error | Structure name is empty or whitespace-only |
| `STR014` | Error | Requested `A*B*C*P` exceeds the signed 32-bit linear-index cap |
| `STR015` | Error | Dummy position name empty or duplicate (case-insensitive) |
| `PROJ034` | Warning | Project symmetry metadata differs from the embedded catalog/contract versions |

- `PAR003` is not used in v1. `ChargeElementary` already enforces the −1000..1000 finite range; an invalid
  charge is rejected by the JSON reader or typed value constructor as `PROJ002`, not as a separate particle
  diagnostic.
- `PAR006` no longer mentions deprecated particles: the new model has no deprecation state. An ID that does not
  resolve is `PAR006` for a set member and `PROJ010` for a site's particle-set reference.
- `PAR010` is the reserved-ID rule. The editor should reject the reserved IDs before constructing a project
  edit; the validator still checks hand-written JSON and library callers.
- `STR008` and `STR011` are not used in v1. A single-site orbit cannot exceed the catalog group order (maximum
  192), and total-position pressure is covered by `STR009`. `STR011` would duplicate `PAR005`, `PAR006`, and
  `STR006`; no distinct "cannot be occupied" case exists. The codes are left unused rather than renumbered.
- `STR009` is deliberately a warning, not the original hard 10,000-position limit. The hard limit in v1 is the
  signed 32-bit linear-index cap (`STR014`) enforced by `UnitCellVectorEncoder`; `STR009` warns before large
  builds. This is a recorded validation deviation, not a silent relaxation.
- `STR012`, `STR013`, and `STR015` preserve the original 1000-base-position limit, the structure-name concept,
  and dummy-name uniqueness. They are Core diagnostics because they need only the project graph.
- `STR014` is emitted by the preview service when the requested supercell product exceeds `int.MaxValue`; the
  encoder's own `GetLinearIndex` exception remains a programmer-error guard.
- `PROJ034` is a project-level warning. It does not block loading; the compiled payload records the
  authoritative versions. `Mocassin.Core` parses and stores the optional envelope metadata but never reads the
  catalog. `Mocassin.Packages.Projects.ProjectValidationService` compares it with
  `SymmetryVersionInfo.FromCatalog(SpaceGroupCatalog.Default)` and appends
  `ProjectDiagnosticCodes.SymmetryMetadataMismatch` (`PROJ034`) when any present value differs; missing
  metadata is accepted. `ProjectValidator` remains Core-only.

Diagnostic ownership follows the assembly boundary. Core emits diagnostics that depend only on the project
graph and primitive types: `PAR001`–`PAR012`, `STR004`, `STR005`, `STR006`, `STR010`, `STR012`, `STR013`, and
`STR015`. Symmetry emits diagnostics that require the catalog, cell geometry, or P1 expansion: `STR001`,
`STR002`, `STR003`, `STR007`, `STR009`, and `STR014`. A composed validator runs both and returns one stable,
sorted list. The Core class is named `CoreStructureDiagnosticCodes` to avoid confusion with the Symmetry
`StructureDiagnosticCodes` class.

- Site-overlap detection uses exact grid coordinates after P1 expansion; no tolerance-based hash sets. The
  grid is the `24 × 10^12` fractional grid defined in Step 02. The structured encoder overload emits `STR007`
  with `CellSiteId` paths and `OrbitOverlap` records; the validator does not parse messages or reimplement
  the orbit comparison.
- Duplicate detection compares the full orbit, not only the seed position.
- A site whose orbit overlaps another site is an error because it makes particle assignment ambiguous.
- Structure validation also computes statistics used by step 07: orbit sizes, total positions, site counts.

Validation deviations from the original are deliberate and recorded here:

- A reference position outside `[0,1)` beyond `FractionalTrimTolerance` is `STR004` error; the original emitted
  a warning and kept the value. Silent wrapping is not allowed.
- Duplicate site or particle names are errors; the original used warnings for several duplicate cases. The
  stricter rule prevents ambiguous editor references.
- Duplicate particle sets are allowed but warn (`PAR009`) instead of being a critical warning.
- More than 10,000 P1 positions is `STR009` warning; the original setting was a hard limit. The signed 32-bit
  linear-index cap remains the hard technical limit.
- The particle symbol pattern is a warning (`PAR008`), not a hard error, so scientific labels are not rejected.

- An unknown `axis` or `origin` enum string is malformed JSON (`PROJ002`), not a valid but unsupported setting.
- `uniqueAxisA` is a known reserved enum value: the parser accepts it, and the validator rejects it with
  `STR001`. It is not treated as malformed JSON.
- `STR001` reports the allowed axis/origin combinations for the number when the catalog can list them.

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
    SpaceGroupSetting? TryResolve(
        SpaceGroupRef reference,
        out ImmutableArray<Diagnostic> diagnostics);

    StructurePreview? TryCreatePreview(
        StructureModel structure,
        int cellCountA,
        int cellCountB,
        int cellCountC,
        out ImmutableArray<Diagnostic> diagnostics);
}

public interface IStructureValidator
{
    ImmutableArray<Diagnostic> Validate(StructureModel structure);
}

// Mocassin.Symmetry.Geometry
public readonly record struct StructureSiteReference(CellSiteId Id, Fractional3 Position);

public sealed record OrbitOverlap(
    CellSiteId FirstSiteId,
    CellSiteId SecondSiteId,
    Fractional3 Position);
```

- Core query interfaces are implemented by the immutable `Project` snapshot, not by mutable managers. The
  `Project` type explicitly implements both interfaces.
- `IStructureQuery` exposes only Core model types.
- `IStructurePreviewService` and `IStructureValidator` are Symmetry services. They accept a Core
  `StructureModel`, resolve `SpaceGroupRef` through the catalog, and return a preview or diagnostics. The
  `Try*` methods do not throw for invalid user data; a failed preview returns `null` plus diagnostics.
- `UnitCellVectorEncoder` gains an additive `TryCreate` overload accepting `StructureSiteReference` values and
  returning `OrbitOverlap` entries plus diagnostics. The existing overload remains source-compatible.
- `ProjectStore.LoadBytesCore` uses `ProjectValidationService.Validate` by default, so loading a project
  returns Core, Symmetry, and `PROJ034` diagnostics. `ProjectStore` accepts an injected validator for tests;
  `ProjectEditor` keeps the Core-only default and the UI/CLI pass the composed validator explicitly.
- The CLI, UI, and compiler compose the Core query with the Symmetry services; Core never references
  `Mocassin.Symmetry`.
- `Mocassin.Packages.Projects.ProjectValidationService` is the single composition point. It runs
  `ProjectValidator`, `IStructureValidator`, and the optional `symmetry` metadata comparison, then returns one
  sorted diagnostic list. `ProjectEditor` remains Core-only by default; the UI and CLI construct it with
  `ProjectValidationService.Validate` so Symmetry and `PROJ034` diagnostics are enforced during edits.
- The UI and compiler consume these interfaces.
- `ProjectEditor` exposes typed editing operations rather than arbitrary mutations:
  `AddParticle`, `UpdateParticle`, `RemoveParticle`, `AddParticleSet`, `UpdateParticleSet`,
  `RemoveParticleSet`, `SetStructureName`, `SetSpaceGroup`, `SetCellParameters`, `AddCellSite`,
  `UpdateCellSite`, `RemoveCellSite`, `AddDummyPosition`, `UpdateDummyPosition`, and `RemoveDummyPosition`.
  Each takes the current `Project` and the new value or typed ID and returns a `ProjectEditResult`; a failed
  edit returns the unchanged project and diagnostics.

## Code layout

The new code follows the structure policy: PascalCase domain folders, namespaces that match folders, file names
that match the primary type, no new root-level files, and no folder above 20 direct files.

```text
Mocassin.Core/
  Ids/
    TypedIds.cs                         # add CellDummyPositionId; reuse ParticleId, ParticleSetId, CellSiteId
  Geometry/
    Fractional3JsonConverter.cs         # [a,b,c] array representation
  Model/
    ModelDefinition.cs                  # flat Particles, ParticleSets, Structure; Empty default
    CellParameters.cs                   # existing type, reused by StructureModel
    Particles/
      Particle.cs
      ParticleSet.cs
      ParticleModel.cs                  # in-memory query grouping, not persisted
      IParticleQuery.cs
    Structure/
      StructureModel.cs                 # includes Name and Default
      SpaceGroupRef.cs
      CellSite.cs
      CellDummyPosition.cs
      SiteStability.cs
      IStructureQuery.cs
  Editing/
    ProjectEditor.cs                    # typed particle/set/site/dummy/parameter operations
  Diagnostics/
    ParticleDiagnosticCodes.cs          # PAR001-PAR012
    CoreStructureDiagnosticCodes.cs     # Core-side STR004-STR006, STR010, STR012-STR013, STR015
  Project/
    ProjectDiagnosticCodes.cs          # add PROJ034 SymmetryMetadataMismatch
    ProjectHasher.cs                   # add public ComputeStructure for the preview cache key
    ProjectJson.cs
    ProjectJsonContext.cs
    ProjectSchema.cs
    ProjectSymmetryMetadata.cs
    ProjectValidator.cs

Mocassin.Symmetry/
  Diagnostics/
    StructureDiagnosticCodes.cs         # existing; extend with STR001 and STR014
  Geometry/
    StructureSiteReference.cs
    OrbitOverlap.cs
  Structure/
    StructurePreview.cs
    StructurePreviewService.cs
    StructureValidator.cs
    IStructurePreviewService.cs
    IStructureValidator.cs

Mocassin.Packages/
  Projects/
    ProjectValidationService.cs         # Core + Symmetry + PROJ034 composition

Mocassin.Benchmarks/
  Structure/
    StructurePreviewBenchmarks.cs       # ceria/BZO preview benchmark
```

Test files mirror the source concept in `Mocassin.Core.Tests`, `Mocassin.Symmetry.Tests`, and
`Mocassin.Packages.Tests`:

- `Particles/ParticleTests.cs`, `Particles/ParticleSetTests.cs`
- `Structure/StructureModelTests.cs`, `Structure/CellValidationTests.cs`
- `Structure/StructurePreviewTests.cs`, `Structure/StructureValidatorTests.cs`
- `Projects/ProjectValidationServiceTests.cs`

`CellParameters` stays in `Model/` because it is an existing Step 01 type; it is referenced from
`Model/Structure/StructureModel.cs` with a matching namespace. If `TypedIds.cs` approaches the 800-line limit,
split the new IDs into `Ids/StructureIds.cs`.

## Implementation tasks

- [ ] Extend `ModelDefinition` with flat `Particles`, `ParticleSets`, and `Structure` sections plus
  `ModelDefinition.Empty`; update `ProjectJsonContext`, `ProjectJson`, `ProjectSchema`, and `ProjectValidator`.
- [ ] Add `ProjectSymmetryMetadata`, `Project.Symmetry`, `ProjectDto.Symmetry`, and JSON/canonical-document
  support; keep it out of the model hash.
- [ ] Implement `Particle`, `ParticleSet`, `ParticleModel` (in-memory only), and the implicit void/empty objects.
- [ ] Implement `StructureModel` with `Name` and `Default`, `SpaceGroupRef`, `CellParameters`, `CellSite`,
  `CellDummyPosition`, and `SiteStability`.
- [ ] Add the `CellDummyPositionId` typed ID and reuse the generic typed-ID JSON converter.
- [ ] Add the `Fractional3` array converter and apply `CamelCaseEnumJsonConverter<T>` to `AxisSetting`,
  `OriginChoice`, and `SiteStability`; mark `Particle.IsVoid` with `[JsonIgnore]`.
- [ ] Implement canonical model serialization: sort particles, sets, sites, and dummies by ID; preserve
  `ParticleSet.ParticleIds` order; add shuffle-invariance hash tests.
- [ ] Implement public `ProjectHasher.ComputeStructure` for the preview cache key; add it to
  `PublicAPI.Unshipped.txt` and refresh the ApiCompat baseline.
- [ ] Implement crystal-system validation, the 1000-reference-site limit, names, reserved IDs, and duplicate
  set members.
- [ ] Add the `UnitCellVectorEncoder.TryCreate` overload with `StructureSiteReference`/`OrbitOverlap`;
  reuse the encoder for `StructurePreview`, `StructurePreviewService`, and `StructureValidator`.
- [ ] Add `STR014` index-cap validation to the preview service; keep `GetLinearIndex` throws for programmer
  errors only.
- [ ] Implement `IParticleQuery` and `IStructureQuery` on the immutable `Project` snapshot.
- [ ] Implement the typed `ProjectEditor` operations and `ProjectValidationService` in `Mocassin.Packages`;
  make `ProjectStore.LoadBytesCore` use the composed validator by default.
- [ ] Add sample definitions for ceria and barium zirconate and populate the existing ceria fixture/sample.
- [ ] Add `PAR010`–`PAR012`, `STR014`, and `STR015`; remove `STR008`/`STR011` usage and document them as
  unused. Final help anchors and `docs/reference/diagnostics.md` belong to Step 07.
- [ ] Add `Mocassin.Benchmarks/Structure/StructurePreviewBenchmarks.cs` and a baseline for ceria/BZO; do not
  add a PR-fast wall-clock assertion.
- [ ] Update `contracts/manifest.json` with the `structure` (including `name`) and optional envelope `symmetry`
  metadata; regenerate `contracts/generated/schema/project-v1.schema.json`. The project `schemaVersion`
  remains 1 because the fields are additive and the model shape is pre-release.
- [ ] Add project fixtures with and without the optional `symmetry` metadata, with `"model": {}`, and with a
  mismatch; test `PROJ034` in `ProjectValidationService` and `ProjectStore`.
- [ ] Add XML docs, update `PublicAPI.Unshipped.txt`, and refresh the ApiCompat baseline for every new public
  type and member.
- [ ] Add `SPDX-License-Identifier: MIT` to every new `.cs` file; no new third-party dependency is introduced.
- [ ] Update the parity matrix rows for particles, particle sets, sites, stability, preview, and dummy
  positions to `Implemented`/`Partial` after the acceptance tests pass; mark UI editing rows for Step 24.
- [ ] Add the new user-guide pages to `zensical.toml` navigation; update the existing structure/symmetry pages.

## Acceptance criteria

- [ ] A user can define ceria: 225, a=5.41 Å, name "Ceria", Ce at (0,0,0), O at (0.25,0.25,0.25), and an
  unstable O site at (0.5,0.25,0.25), using the public editing API.
- [ ] The preview shows the correct P1 orbit sizes for each site.
- [ ] Overlapping sites are rejected with a `CellSiteId` path to each offending site through the structured
  `OrbitOverlap` result; no message parsing is used.
- [ ] Crystal-system constraints are enforced without silently overwriting user input.
- [ ] The structure preview is deterministic and cached by `(structureHash, A, B, C, catalogDataVersion,
  catalogDataSha256, symmetryContractVersion)`; `structureHash` is the canonical structure-model hash.
- [ ] The ceria/BZO preview benchmark case and baseline are recorded; the target is mean < 100 ms on the Step
  02 benchmark runner class. The benchmark runs in Nightly/release, not as a PR-fast wall-clock assertion.
- [ ] A preview request whose `A*B*C*P` exceeds `int.MaxValue` returns `STR014` and no preview.
- [ ] `"model": {}` still loads as `ModelDefinition.Empty`, and the existing minimal/ceria fixtures round-trip.
- [ ] JSON round-trips use `[a,b,c]` fractional arrays, camelCase enum strings, and no `isVoid` field.
- [ ] A project with `symmetry` metadata parses in Core, is included in the document hash, and is compared by
  `ProjectValidationService`; a mismatch emits `PROJ034`.
- [ ] `ProjectStore.LoadBytesCore` returns Core, Symmetry, and `PROJ034` diagnostics through the composed
  validator.
- [ ] Shuffling authored particles, sets, sites, or dummies does not change the canonical model hash.
- [ ] All diagnostics are returned by the composed public validation API. CLI exposure is verified in Steps
  07/12; UI exposure is verified in Step 24.
- [ ] The generated project schema accepts the structure (including `name`), particles, and optional symmetry
  fields, and fixtures cover present, absent, default-empty, and mismatched metadata.
- [ ] Public API files and the ApiCompat baseline are current.
- [ ] Documentation written

## Documentation

At this point, document particles and crystal structure:

- `docs/user-guide/particles.md` — new: particles, particle sets, void/default semantics, limits.
- `docs/user-guide/structure.md` — new: structure name, space group, cell parameters, sites, stability,
  dummies.
- `docs/physics/symmetry.md` — update: site orbits, P1 preview, and why positions are fixed.
- `docs/development/architecture.md` — update: particle/structure domain APIs, editor operations, and the
  Core/Symmetry preview and validation boundary.
- `docs/reference/project-format.md` — update: particles, structure name, cell constraints, `PAR`/`STR` codes,
  canonical model rules, and the three hash entry points (`Compute`, `ComputeDocument`, `ComputeStructure`).
  The structure section already exists and must not be duplicated.
- `docs/development/design-decisions.md` — update the “two hashes” section to describe the public structure
  hash used for preview caching.

## Tests

All cases below are PR-fast except the full random property sweep, which is PR-medium, and the
`StructurePreview*` benchmark, which is Nightly/release. The sample P-order goldens remain PR-fast.

| Test | Assertion |
| --- | --- |
| Model envelope | Empty, particle-only, structure-only, and combined models round-trip through the project JSON context; `"model": {}` loads as `ModelDefinition.Empty` |
| Persisted JSON mapping | `[a,b,c]` fractional arrays, camelCase enum strings, and no `isVoid` field; a hand-written contract fixture round-trips |
| Canonical model hash | Shuffling particles, sets, sites, and dummies does not change `ProjectHasher.Compute`; `particleIds` order does change the semantic set |
| Particle limits | 63 custom particles accepted; 64 rejected; more than 100 sets rejected |
| Particle names | Empty/duplicate names produce `PAR001`; case-insensitive comparison |
| Void/reserved IDs | Void cannot be added to a set; `ParticleId.Void`/`ParticleSetId.Empty` cannot be authored (`PAR010`); `Find(ParticleId.Void)` returns the void particle |
| Symbol/charge | Mass symbol preserved; out-of-range charge is `PROJ002`; malformed symbol emits `PAR008` |
| Particle sets | Empty/void/unknown members rejected; `PAR011` for empty/duplicate set names; `PAR012` for duplicate members; duplicate ordered sets warn `PAR009` |
| Structure name | Empty name is `STR013`; `StructureModel.Default` uses `New Structure`; name round-trips |
| Base positions | 1000 reference sites accepted; 1001 rejected with `STR012` |
| Crystal systems | Valid/invalid sets for all seven systems, both trigonal axis descriptions, and unique-axis B/C; `uniqueAxisA` is rejected |
| Cell determinant | Near-degenerate and ill-conditioned cells are rejected; valid cells have `det G > 0` and positive volume |
| Cell vectors | Triclinic regression, hexagonal γ=120°, rhombohedral α=60°, and right-handedness |
| Site coordinates | Values within `FractionalTrimTolerance` snap; values outside `[0,1)` beyond tolerance produce `STR004` and are not wrapped |
| Site overlap | Same orbit, special positions, near-boundary cases, exact grid equality, `OrbitOverlap` pairs, and `CellSiteId` paths |
| Site/dummy names and sets | `STR005` duplicate site names; `STR006` missing/default set; `STR010` invalid dummy coordinates; `STR015` empty/duplicate dummy names |
| Preview | Orbit sizes and Cartesian positions match hand calculations; preview echoes `(A,B,C)` |
| Preview cache | Different `(A,B,C)` or catalog/contract versions do not share a cached preview; `structureHash` changes when the structure changes; invalid input returns diagnostics and no partial preview |
| Determinism | Same structure and cell counts yield identical canonical P-list/orbit/cell data |
| P order | Preview positions are in canonical P order; shuffling input does not change P |
| Index cap | A supercell product above `int.MaxValue` returns `STR014` and no preview |
| Occupancy/budget warnings | `STR009` above 10,000 P1 positions; `PAR004` when vacancy and default occupation are both charged |
| Editing | Add/update/remove particle, set, site, dummy, name, space group, and cell parameters produce a valid new project or diagnostics; failed edits leave the project unchanged |
| Composition | `ProjectValidationService` merges Core, Symmetry, and `PROJ034` diagnostics; `ProjectEditor` constructed with it rejects Symmetry errors; `ProjectStore.LoadBytesCore` uses it by default |
| Project symmetry metadata | `Project`/`ProjectDto` round-trip the optional object; document hash changes, model hash does not; missing/partial metadata is accepted |
| Assembly boundary | `Mocassin.Core` has no reference to `Mocassin.Symmetry`; preview and catalog validation compose over Core models |
| Space-group reference | `(number, axis, origin)` round-trips for every supported enum value; HM/Hall are derived; unknown combinations produce `STR001`; reserved `uniqueAxisA` is rejected |
| Symmetry metadata | Matching versions load silently; mismatched catalog/contract emits `PROJ034`; missing/partial metadata is accepted; fixtures cover all cases |
| Property tests | Random particle/structure models round-trip canonically; random site sets match a brute-force overlap/validation oracle |
| Performance | `StructurePreviewCeria`/`StructurePreviewBariumZirconate` BenchmarkDotNet cases record a baseline; mean < 100 ms on the Step 02 benchmark runner class; Nightly/release, not PR-fast |
| Sample P order | Ceria and barium zirconate preview P indices are stable and match the Step 02 conventions |

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| JSON shape drifts from the contract | Explicit flat `ModelDefinition`, `Fractional3`/enum converters, `[JsonIgnore]`, contract fixtures, and round-trip tests |
| Existing v1 files stop loading | `ModelDefinition.Empty` defaults and tests for `"model": {}` and existing minimal/ceria fixtures |
| Site overlap detection is expensive for large cells | Exact grid orbit dedup and early exit; cache keyed by structure hash and cell counts |
| Crystal-system coupling confuses users | UI locks dependent fields and explains constraints; API rejects inconsistent values |
| Cell-parameter tolerance drift | Named, unit-specific tolerances and boundary tests |
| Particle symbol semantics are implicit | Document and warn; show mass-conservation groups in the editor |
| Preview diverges from compiled model | Compile from the same symmetry engine and compare in step 08 tests |
| Redundant setting fields drift | Persist only `(number, axis, origin)`; derive HM/Hall/system from the catalog |
| P-index drift | Golden P-order tests, shuffle invariance, and the exact grid contract |
| Catalog/contract mismatch | `PROJ034` emitted by `ProjectValidationService`, no automatic rewrite, and the compiled payload records the actual versions |
| Unreachable diagnostics | Every used code has a positive and negative test; `PAR003`, `STR008`, and `STR011` are documented as unused |
| Envelope metadata is parsed but ignored | `ProjectSymmetryMetadata` round-trip tests and `ProjectStore` composed-validation test |
| Composed validator is bypassed by the loader | `ProjectStore.LoadBytesCore` uses `ProjectValidationService` by default and the test asserts Symmetry/`PROJ034` diagnostics |
| Preview cache key is incomplete | `ProjectHasher.ComputeStructure` plus catalog/contract version injection and cache-key tests |
| Parity drift for structure name/limits | `StructureModel.Name`, `STR012`, and the recorded validation deviations; parity-matrix update is an implementation task |

## Done when

Particles, sets, cell parameters, sites, and dummies can be authored, validated, previewed, and queried, and
the ceria structure produces the expected P1 expansion.
