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
are produced once; the in-memory entry reports `Load` as skipped only when no caller diagnostics are supplied.
`validate` never runs build-stage checks; Steps 09–12 append them through `IValidationStage` with
`Stage = Build` and `RunBuildStage`. The diagnostic catalog is the stable registry, and validation never
mutates the project.
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

Five SHA-256 hashes are exposed:

- `ProjectHasher.Compute` hashes the canonical **model section** as the basis for the compiled-model
  signature;
- `ProjectHasher.ComputeDocument` hashes the complete canonical document and is used by `ProjectDocument` for
  dirty tracking;
- `ProjectHasher.ComputeStructure` hashes the canonical **structure section** (name, space group, cell, sites,
  dummies) and is used as the structure-preview cache key. It is a public API because the Symmetry preview
  service calls it;
- `ProjectHasher.ComputeEnergy` hashes the canonical **energy section** (stable and unstable environments,
  clusters, site energies) and is the energy half of the Step 08 compilation cache key;
- `ProjectHasher.ComputeTransition` hashes one transition closure (the transition, its chain, referenced
  groups/state changes, and the full particle order) and is the transition-preview cache key.

They are deliberately distinct. The Step 08 compiled-model signature is a different value again, computed over
the canonical compiled geometry/rules/encoding projection and excluding parameterization values and runtime
settings. Step 09 computes the parameterization signature over the model signature plus the canonical
energy/frequency values; both follow ADR 0010's canonical projection rules.

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

## Synchronous, pure compiler (Step 08)

`IModelCompiler.Compile` is synchronous and takes a `CancellationToken`. Compilation is CPU-bound, has no I/O,
and must not be made async merely to look modern. Step 12 owns the async orchestration and progress events.
`Mocassin.Compilation` does not reference `Mocassin.Contracts` or `Mocassin.Packages`; user-data failures
return diagnostics, while programmer errors, contract violations, and cancellation throw.

## Compilation assumes validation

The compiler consumes a `Project` that passed `ProjectValidationPipeline`. It does not accept or duplicate a
`ValidationResult`, run the load/model/cross-domain/feasibility stages, or add an `IValidationStage`.
`IValidationStage` lives in `Mocassin.Packages` and the dependency direction forbids `Compilation` from
referencing it. Step 12 owns the Build stage and calls the compiler after validation. A stage service that
rejects invalid input is reported as `CMP001` with the source diagnostic preserved in `Related`, so the
compiler fails closed without re-authoring validation rules.

## Compilation cache lifetime

Stage caches default to one `Compile` call and are released on return. `CompilationOptions.Cache` allows a
caller-owned, thread-safe, bounded cache for repeated builds; Step 12 may keep one per build/session. There is
no static or process-wide cache, because a long-lived cache across projects complicates invalidation and
lifetime without a measured need. Cache entries are immutable, keyed by content hashes plus catalog/contract
versions, and insertion order never affects output. The rule cache key includes
`ProjectHasher.ComputeSimulationSelection`, so adding or removing a simulation's transition reference
invalidates the rules; runtime-only simulation edits rebuild the encoding defaults but not the rules.

## Model signature versus parameterization signature

Step 08 computes `CompiledModel.ModelSignature` over geometry, rules, encoding, indexing, and catalog/contract
versions; it excludes runtime simulation settings and all energy/frequency values. Step 09 treats that value
as opaque, stores it in the parameterization template, and computes the parameterization signature over the
model signature plus the canonical values. The `.msl` `models.signature` and `.mcs`
`metadata.modelSignature` are the parameterization signature; the `.msl` payload also carries
`model_signature` for template/model identity. A geometry change invalidates templates; a value-only change
does not.

## Parameterization templates are sparse project data

A parameterization template is part of the `.mocprj` document, not a compiled artifact or a separate file.
`ParameterizationId` is UUIDv7 and is preserved by rebase so build templates keep working. The template
shape is versioned by the project `schemaVersion`; there is no second template version axis in v1. Step 09
extends the Step 01 stub additively before the first release.

Values are sparse: absence means unset, not zero. Pair/group absence defaults to `0 eV`, rule-frequency
absence to the compiled `1e13 Hz`, and site-override absence to the authored `model.siteEnergies` background.
Creation writes only set headers, so a fresh template is visibly unparameterized and `PRM003`/`PRM006` can
report it. This is deliberately different from the original dense default-filled sets: it distinguishes
"reviewed and left at default" from "not reviewed" and keeps large templates small.

Identities are content identities, never dense indices: `EnergyId`, `ClusterEnergyId` plus the canonical
occupation tuple, `RuleGeometryId` inside `(transitionKind, transitionId)`, and `(siteId, particleId)` for site
overrides. A model-signature change invalidates the template before ID matching; diff/rebase is explicit and
reported, and values are never silently reused across changed geometry.

### Diff and rebase are reported, never implicit

`Diff` is a pure description by stable identity. It classifies template entries as `Matched`, `Added`, or
`Removed` and groups related pair/group removals and additions as display-only `GeometryChanges`. Rebase is a
separate explicit operation with three strategies:

- `KeepMatching` copies only exact stable IDs and defaults every new slot.
- `NearestGeometry` is pair-only and opt-in. It copies a removed pair value only into a unique added slot with
  the same unordered site pair, chiral flag, stability kind, particle pair, and a distance bucket within the
  configured tolerance. Ambiguous candidates copy nothing and are counted. Groups, rules, and site overrides
  remain exact-ID-only because the template stores no offset or geometry hash that could justify a nearest
  match.
- `Reset` preserves the template identity and creation time, adopts the new signature and headers, and clears
  every value.

`ParameterizationRebaseReport` records copied/defaulted/dropped/ambiguous counts plus every nearest copy, and
`PRM009` carries the same counts. A changed chiral link has no exact ID and is excluded from nearest copying,
so the shared value is dropped rather than reused across the break. The service never mutates its inputs or
saves the result; Step 26 will show the report before the user commits the rebound template.

The checked-in golden fixtures are generated through the real compiler and apply path, not a synthetic value
pattern. `ParameterizationGoldenFixtureTests` pins the ceria, ceria-KMC, barium-zirconate, and P1 chiral
applied values and projections; `ParameterizationChiralFixtureTests` regenerates the P1/P4mm link-change
projects and proves the drop/no-copy rule. Intentional regeneration uses `MOCASSIN_UPDATE_GOLDENS=1`, then the
manifest signature vectors and `tools/contracts/generate.py`; `verify.sh contracts` rejects stale fixtures.


## Parameterization values, signature, and site background

`Mocassin.Contracts` owns the applied `ParameterizationValues` and the `ParameterizationSignatureV1`
projection; `Mocassin.Packages` owns the factory, apply, validation, diff, and rebase and returns a build-time
`ParameterizedModel` (compiled model + applied values + signature). The `.msl` payload is assembled by
Step 11 from those parts. `Mocassin.Compilation` stays parameterization-free and does not reference Contracts.

The parameterization projection is values-only: `formatVersion`, `modelSignature`, dense `pairTables`,
`groupSets`, `siteEnergies`, and `transitionRules`. The model signature binds geometry and order, so stable
IDs are unnecessary and Step 13 can re-encode the digest from the payload. The applied values are quantized
to 12 decimals before storage and signing; the sparse template is not signed directly, so two templates with
the same effective values produce the same package signature.

Step 08's implemented `CompiledEnergyModel` has pair and cluster tables but no site-energy background.
Step 09 resolves the authored `model.siteEnergies` onto the compiled `[position][particle]` table, applies
template overrides, and Step 11 writes it as `energy.defect_background`. Step 15 consumes the same table.
This keeps the completed compiler unchanged and the authored site energies in their existing Core home.

### Site-energy background resolution

`ParameterizationService.Apply` builds the dense background directly from the supplied `Project` snapshot:

1. Resolve each authored `SiteEnergySet` by `CellSiteId` to the compiled reference site and each
   `SiteEnergyEntry` by `ParticleId` to the compiled particle index.
2. Copy the value to every `CompiledPosition` owned by that reference site, at row-major index
   `P * particleCount + particleIndex`. Particle `0` (void) and particles outside the site's allowed mask stay
   `0 eV`; a missing authored entry also stays `0 eV`.
3. Apply template `SiteEnergyOverrides` last. An override replaces the authored value at every owned position,
   so "reset to model" is removal of the override, never a copied value.
4. Report an authored entry or override whose site/particle does not map, or whose particle the site cannot
   occupy, as `PRM008` **Warning** and ignore it. A stale entry therefore cannot silently shift the background.
   A structural mismatch that changes the compiled-model signature is `PRM001` **Error** and produces no
   `ParameterizedModel`.

The table is quantized to 12 decimals before signing, like every other applied value. The dense table is
`[position][particle]` row-major in ascending P order, matching Step 11's `energy.defect_background` and Step
15's per-position consumption. Because the project is the source of the authored background, `Apply` requires
the same `Project` snapshot that produced the compiled model. The compiled model does not persist a project
hash, so Step 12 and the GUI pass a real project/model pair; a mismatched pair is a caller contract violation,
while an individual stale site entry is recoverable user data reported as `PRM008`.

At Step 09, `ValidationContext` had no compiled model, and compiling inside validation would duplicate Step 12,
so Step 09 adds no `IValidationStage`. Structural `PRM` checks run in Core `ProjectValidator`; compiled-bound
checks run in `ParameterizationService.Validate`, called by Step 12 after compilation. For the same reason there
is no `mocassin parameterize` CLI; Step 12 owns `compile` and `build --parameterization`.
Step 10 adds an optional `CompiledModel` to `ValidationContext` for its own `JOB` build stage. Step 09 remains
stage-free; Step 12 registers the Step 10 stage only after compilation.

## Build-side SHA-256 streams (Step 10)

Job and lattice generation must be deterministic across platforms without pulling the solver's ChaCha12
implementation into the build. The build derives a 32-byte job seed with SHA-256 over the master seed and the
fixed-width `u32_le` collection/configuration/job indices plus a terminal domain string. The lattice seed hashes
the job seed with the lattice-request hash, which is parameterization-independent so a runtime or energy edit
does not change a lattice for the same job.

The deterministic sampler is a SHA-256 counter DRBG: `SHA-256("mocassin/lattice-rng/v1" || seed || u64_le(counter))`
supplies each 64-bit block. `NextBounded` uses Lemire's multiply-and-reject method with a 128-bit intermediate,
and sampling without replacement is an in-place Fisher–Yates shuffle over candidates sorted by ascending linear
index. The implementation is explicit and platform-independent: no `System.Random`, no time, no second hash
algorithm, and no new NuGet dependency. ADR 0005's ChaCha12 stream remains the solver's stream; the build never
shares it. Golden vectors pin the job seed, lattice seed, and the `[1, 7, 2]` sampler selection.

The build never creates or persists a seed. Rebuilding from the same project and job coordinates reproduces the
seed, so no seed becomes stale when a template is copied or a run is restarted.

## `ValidationContext.CompiledModel` (Step 10)

`ValidationContext` gains an optional `CompiledModel?` property, defaulting to `null`. It is additive: the load,
model, cross-domain, and feasibility stages never need it, and `validate` keeps `RunBuildStage = false`. The
Step 10 `JobTemplateValidationStage` runs only when `RunBuildStage` is true, a compiled model is supplied, and
earlier stages have no error. It first delegates to the Core structural validator, then runs the compiled-bound
checks (total position count and job limit, optimizer removes-all, doping/compensation/neutrality outcomes).

This is deliberately not a second validation pipeline. The Build stage remains the only extension point, the
Core structural validator remains available to `validate` without a compiled model, and Step 12 owns the
`ValidationContext` construction and stage registration. A model that is missing or stale relative to the
project is a caller contract problem, not a user-data diagnostic.

## Packages/Compilation boundary (Step 10)

`Mocassin.Compilation` stays a pure model compiler. It does not know about job templates, lattice requests,
doping fractions, job counts, seeds, or selection optimizers, and it does not reference `Mocassin.Contracts`.
The typed job templates live in `Mocassin.Core.Model.Jobs`; expansion, supercell generation, the preview
service, and the optimizer plan live in `Mocassin.Packages.Jobs`, which references `Core`, `Compilation`, and
`Contracts`.

This preserves the architecture's `Packages -> Compilation` direction and keeps the compiled model reusable for
every job and lattice. Step 11 is the only package-payload mapper: it composes a `JobDescriptor` and its
`GeneratedLattice` into the `.msl` job and lattice payloads, including `SolverJobFlags` and `DopingInfo`. Step 10
does not write a package and does not add a `.msl` schema version bump; `doping_info` is a pre-implementation
contract extension recorded in the Step 10 page.

## Optimizer dual-DOF rule (Step 10)

The compiled intent starts with `UseDualDofCorrection = Kind == Kmc`, matching the original job builder. A
selection optimizer removes a `(position, particle)` start option at every canonical P1 image of the target
site, but it never removes the particle from the lattice and never mutates the compiled model or its masks.

For each compiled environment, the resolver checks whether an optimizer target lies on a position with that
environment index and whether the particle is allowed there. The flag is cleared only when at least one
optimizer exists **and every** environment is reduced. A mixed, partial, or no-optimizer case keeps the flag, and
MMC keeps it clear. This reproduces the original `JumpSelectionOptimizer.Run` accumulation rule
(`isReducedMask |= optimizedMask == originalMask`) and is covered by tests for one, some, and all environments
reduced. The `JOB009` removes-all check uses the compiled jump-count table and the active KMC collections'
mobile masks; when that table is unavailable the plan reports a precise limitation rather than claiming the
check ran.

## Derived solver intent

`CompiledSolverIntent` carries `Kind`, `UsePrerun = Kmc && PreRunMcsp > 0`, and
`UseDualDofCorrection = Kind == Kmc` by default. The default matches the original job builder, which sets the
flag for every KMC job; Step 10 may clear it only through a documented and tested selection-optimizer rule.
The compiler does not infer the flag from the mechanism shape. The compiler does not compose `SolverJobFlags`:
Step 10 copies the intent to `JobDescriptor`, and Step 11 calls `SolverJobFlagsMapper.Compose`. The project
JSON never stores derived bits.

## Tracker definitions versus state

Step 08 emits tracker **definitions and index mappings**: global `(collection, particle)`, static
`(position, particle)`, and per-rule `tracker_order` following particle identity. Step 18 owns mutable tracker
values, mobile-tracker assignment at initialization, accumulation, and `.mcs` output. MMC has no trackers.

## Compile diagnostics

Step 08 owns the `CMP` prefix. `CompilationDiagnosticCatalog` registers `CMP001`–`CMP012` as
`DiagnosticDescriptor` values; `DiagnosticCatalog.Default` composes them with Core, Symmetry, and Step 07.
`CMP001` wraps a failed precondition, adds the source `STR`/`TRN`/`ENM`/`SIM` diagnostic to `Diagnostics`,
and preserves its path in `Related`.
Rule ambiguity (`CMP004`), no valid rules (`CMP005`), compile limits (`CMP006`), path expansion (`CMP007`),
conservation evaluation (`CMP008`), and signature failure (`CMP011`) are errors; unused transitions
(`CMP009`), advisory budgets (`CMP010`), and defensive cache recomputation (`CMP012`) are warnings.

## `mocassin compile` ownership

Step 08 ships the library and a pure canonical JSON inspection writer only. Step 12 owns `mocassin compile`
and the full CLI. This avoids a second parser/orchestration path and keeps the compiler free of loading,
validation, file output, and exit-code policy. The data contract's `compile` command is implemented in
Step 12.

## Charge-transport field convention

The compiler stores a per-rule Cartesian charge transport vector as the difference of the charge-weighted
endpoint moments over the path nodes, `Σ_nodes (q(S2) - q(S0)) * x(node)`. For balanced particle counts this
is the particle-identity sum `Σ_i q_i * Δx_i`; the moment form also covers charge-state changes. Step 15
computes the field energy from its dot product with the normalized field direction. This is exact for
non-collinear multi-site mechanisms and gives the correct single-charge and vacancy signs; the direction's
`path_vector` is display/tracking only. A scalar field factor would lose the sign or cancel for symmetric
exchanges.

## Diagnostics over exceptions for user data

Invalid project data produces diagnostics with stable codes and object/property paths. Validation never
throws. Exceptions remain reserved for programmer errors, I/O failures, contract violations, and cancellation.

## Line endings and encoding

Project files are UTF-8 without a byte-order mark and use LF line endings on every platform. The serializer
sets `NewLine = "\n"` explicitly; relying on the platform default caused a Windows-only CI failure during
Step 01 and is now covered by a test.
