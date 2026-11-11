# Design Decisions

This page records decisions introduced by Step 01 that are not already captured by an ADR. It follows the
project rule that expensive or surprising choices must be explainable from the plan, not only from the code.

## Stable typed identifiers

Every user-created object uses a strongly typed UUID wrapper. New objects use UUIDv7 so identifiers sort by
creation time without relying on database order. References are typed IDs rather than array positions.

The implicit void particle and empty particle set use fixed reserved GUIDs rather than `Guid.Empty`, so an
uninitialized value is not mistaken for a meaningful object. Dense indices exist only in compiled packages.

## Explicit units, not a generic SI quantity

The kernel uses explicit `LengthAngstrom`, `EnergyEv`, `TemperatureKelvin`, `ChargeElementary`, `TimeSeconds`,
`FrequencyHz`, `ElectricFieldVm`, and `AngleDegrees` structs.

A generic SI value (a `double` plus dimension/prefix metadata) was considered. It would be more flexible for
adding units, but it has significant costs here:

- type safety would move from compile time to runtime unless dimensions became generic type parameters;
- domain ranges (non-negative lengths, charge bounds, angle bounds) would move to call sites;
- eV, ångström, elementary charge, and degrees are not represented by SI base units and prefixes alone;
- the project/package/state JSON schemas deliberately carry human-readable units in field names;
- a generic quantity would cross the Rust/Python boundary as a plain `f64` anyway.

Explicit semantic types keep the public API readable and prevent energy/temperature/length mix-ups. Public
generic vector/matrix math (`Vector3<T>`, `Vector4<T>`, `Matrix3<T>`, and `Matrix4<T>`, introduced by Step 02
amendment 1) remains separate from the semantic coordinate wrappers, and conversion helpers can be added where a
solver or analysis boundary genuinely needs SI values.

## One project per file

A `.mocprj` file contains exactly one project. The application workspace is the set of open project files and
recent files, not a multi-project database. This keeps save, lock, diff, and recovery semantics simple.

## Flattened project envelope

The C# domain type groups metadata in `ProjectMetadata`, but the JSON envelope is flat: `projectId`,
`masterSeed`, `name`, timestamps, and optional description/author/notes are root properties, as specified by
the data contract. A custom converter performs the mapping so the public API and the persisted shape can both
stay natural.

## Flat model envelope (Step 03)

`ModelDefinition` is flat: `particles`, `particleSets`, and `structure` are direct model properties, so the JSON
shape remains `model.particles`, `model.particleSets`, and `model.structure`. `ParticleModel` is an in-memory
grouping only and is never nested in the document. `ModelDefinition.Empty` supplies the default structure for
`"model": {}` and for missing model sections. Custom model and structure converters apply defaults for missing
fields and reject unknown or duplicate properties; they exist because source-generated construction of
`init` properties would overwrite constructor defaults with `default` values. Parameterization, job, and build
templates keep their Step 01 shape until Steps 09–10 extend them.

## Model JSON mapping

- `Fractional3` is a three-number array `[a, b, c]`, never an object.
- `AxisSetting`, `OriginChoice`, and `SiteStability` use camelCase strings.
- `Particle.IsVoid` is computed and ignored by serialization.
- Canonical JSON sorts `particles`, `particleSets`, `sites`, and `dummyPositions` by stable ID.
  `ParticleSet.ParticleIds` keeps its authored order because the first particle is the default occupation.
- The canonical model hash is therefore invariant under authoring order but sensitive to the semantic set
  order.
- `ModelDefinition` and `StructureModel` use explicit converters so `{}` and missing sections load as defaults
  without weakening the unknown-field rejection contract.

## Particle and structure validation ownership

Core validates rules that depend only on the project graph: particle names, symbols, sets, reserved IDs,
structure names, site/dummy names, positions, limits, and site-to-set references. Symmetry validates rules that
need the catalog, cell geometry, or P1 expansion: space-group resolution, cell/crystal-system constraints,
orbit overlap, the P1 budget, and the index cap. `Mocassin.Packages.Projects.ProjectValidationService` is the
single composition point; it also emits the `PROJ034` metadata warning and the cross-domain `PAR004`
charged-vacancy/default-occupation warning.

`ProjectStore` uses the composed validator by default. `ProjectEditor` keeps the Core-only default and the
UI/CLI pass the composed validator explicitly, which keeps the Core editing layer free of catalog dependencies.
Step 07 adds `ProjectValidationPipeline` in `Mocassin.Packages.Projects.Validation`; it keeps
`ProjectValidationService` as the model/domain stage and adds cross-domain, feasibility, and optional statistics
stages. The pipeline's file entry loads through `ProjectStoreOptions.ValidateOnLoad = false`, so load diagnostics
are produced once; the in-memory entry reports `Load` as skipped. `validate` never runs build-stage checks;
Steps 09–12 append them through `IValidationStage` with `Stage = Build` and `RunBuildStage`. The diagnostic
catalog is the stable registry, and validation never mutates the project.
`PROJ034` is advisory; it never rewrites the project or blocks loading. The compiled payload records the
authoritative catalog and contract versions.

## Structure preview

`StructurePreviewService` derives a deterministic preview from the authored structure: resolve the setting,
validate the cell, expand P1 positions, compute Cartesian positions and cell volume, and expose per-site orbit
information and the requested cell counts. Overlap detection uses the exact grid and returns structured
`OrbitOverlap` records with `CellSiteId` paths.

The preview cache is scoped to the service and keyed by the canonical structure hash, cell counts, catalog data
version/SHA-256, and symmetry contract version. The preview is not the compiled model and has no persisted
byte format; the compilation pipeline produces the authoritative structure.

## Canonical JSON and hashes

Canonical JSON is whitespace-free, LF-only, schema-ordered, and sorts non-semantic arrays by stable ID. It
normalizes timestamps to UTC with seven fractional digits and `-0.0` to `0.0`.

Three SHA-256 hashes are exposed:

- `ProjectHasher.Compute` hashes the canonical **model section** as the basis for the compiled-model
  signature;
- `ProjectHasher.ComputeDocument` hashes the complete canonical document and is used by `ProjectDocument` for
  dirty tracking;
- `ProjectHasher.ComputeStructure` hashes the canonical **structure section** (name, space group, cell, sites,
  dummies) and is used as the structure-preview cache key. It is a public API because the Symmetry preview
  service calls it.

They are deliberately distinct. The compiled-model signature in Step 08 is a different value again, based on
the canonical MessagePack projection (ADR 0010).

## Manifest as schema source

`contracts/manifest.json` remains the single source for field names, types, and versions. The contract
generator was extended to emit nested object properties, array item schemas, enum constraints, and primitive
additional properties for the open per-object style map. Generated schema files are never hand-edited.

## Open style keys

`ViewSettings.ObjectStyle` has known fields (`visible`, `color`, `material`, `quality`, `scale`, `wireframe`)
and preserves additional primitive keys as a flat open map. A newer renderer can add a style key without a
format change, while older readers still round-trip it unchanged. Nested style values are not supported.

## Atomic save, locking, and autosave

- Saves write a temporary file in the target directory, flush to disk, and atomically replace the target.
- A failed save leaves the previous file intact.
- The lock file stores PID, host, token, and acquisition time. A same-host dead PID or an expired lock is
  reclaimed; active locks are reported as `PROJ031`.
- Lock release checks the token before deleting, so a session cannot remove another session's lock.
- Autosave is an explicit `ProjectStore.WriteAutosaveAsync` operation. `ProjectDocument` exposes
  `IsAutosaveDue` and `MarkAutosaved`; the UI timer is added in Step 23.
- Autosave does not update `modifiedUtc`; only a real save does.

## Diagnostics over exceptions for user data

Invalid project data produces diagnostics with stable codes and object/property paths. Validation never
throws. Exceptions remain reserved for programmer errors, I/O failures, contract violations, and cancellation.

## Line endings and encoding

Project files are UTF-8 without a byte-order mark and use LF line endings on every platform. The serializer
sets `NewLine = "\n"` explicitly; relying on the platform default caused a Windows-only CI failure during
Step 01 and is now covered by a test.
