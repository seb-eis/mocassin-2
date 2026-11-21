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
normalizes timestamps to UTC with seven fractional digits and `-0.0` to `0.0`. Opaque extension-config
objects are the documented exception: their JSON number tokens are preserved verbatim so Steps 27/28
configurations round-trip exactly (including integers above 2^53 and values outside binary64), and `1` and
`1.0` therefore hash differently. Every non-extension project number keeps the shortest round-trip rule.

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
  `IsAutosaveDue` and `MarkAutosaved`; Step 23B's `AutosaveScheduler`/`AutosaveTimer` owns the UI tick.
- Autosave does not update `modifiedUtc`; only a real save does.

## Desktop editing, validation, and recovery (Step 23B)

- `ProjectEditCoordinator` is the only writer. It delegates to `ProjectDocument`/`ProjectEditor`, exposes
  `Apply`/`Undo`/`Redo`, and raises `ProjectChanged`/`EditRejected`; no Core event or second undo stack is added.
  The GUI document uses `MaxUndoDepth = 200`; the library default remains unbounded.
- The save point is content-based and calls the no-arg `ProjectDocument.MarkSaved()` so save → undo → redo is
  clean. The store's returned `modifiedUtc` is not installed in the document.
- Validation is seeded from the single retained `ProjectSession.Validation` and never re-runs on open. Edits
  debounce 300 ms, run off-thread over an immutable snapshot, and apply only when the captured version/snapshot
  is current. Statistics are an explicit, cached user action.
- `ProjectSession` owns a per-session `SemaphoreSlim` gate and `IsClosing`; save/autosave/close/reload/recovery
  serialize through it, and autosave skips busy/closing sessions. Close/reload return `UnsavedChanges` when a
  dirty session has not been confirmed; cancel/failure clears `IsClosing`.
- Recovery has three sources: the `<file>.autosave` sidecar, a fallback `recovery/` snapshot, and a crash
  snapshot. A `mocassin.recovery` manifest is app-local. `RecoveryComparison` normalizes only `modifiedUtc` and
  reads `ProjectDocument.IsDirty`, so the ViewModels layer never hashes. A deferred `PendingRecovery` candidate
  is preserved across clean close and cleared by the first accepted content edit (not a no-op).
- `recovery/` keeps a seven-day retention sweep; `.autosave` sidecars are never swept. A deferred `recovery/`
  candidate can expire (accepted m-14 limitation).
- The crash dialog captures dirty sessions on the UI thread and writes them asynchronously through
  `CrashRecoveryWriter` with a 2 s budget. The dead-UI path writes no recovery files.
- `autosave`/`session` are additive v1 settings blocks with no schema bump and no contract-manifest entry.

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
stage-free; `mocassin build --preview` registers the Step 10 stage after compilation, and Step 12 reuses the
same wiring for the full build.

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

`ValidationContext` carries an optional `CompiledModel?` property, defaulting to `null`. It is additive: the
load, model, cross-domain, and feasibility stages never need it, and `validate` keeps `RunBuildStage = false`.
`ProjectValidationPipeline.Validate` and `ValidateFileAsync` accept the optional model; the pipeline runs its
Build stages only when `RunBuildStage` is true **and** a compiled model is present, and otherwise records them
as skipped. Earlier-stage errors also skip the Build stages.

The implemented `Mocassin.Packages.Projects.Validation.JobTemplateValidationStage` is the Step 10 Build stage
(`Stage = ValidationStage.Build`). It delegates to the Core structural validator first, then adds the
compiled-bound checks:

- total-position `JOB004`, large-supercell `JOB014`, and job-total `JOB013`/`JOB019`;
- the optimizer removes-all `JOB009` check through `SelectionOptimizerResolver`;
- doping realisation, truncation, compensation, and neutrality (`JOB010`–`JOB012` and `JOB016`) through the
  shared `LatticePreviewService`, so validation and generation cannot diverge.

`mocassin build --preview` compiles the project first and then calls the pipeline with the compiled model and
`RunBuildStage = true`; Step 12's full build constructs the same pipeline with
`additionalStages: [new JobTemplateValidationStage(previewService)]`. It adds no new Build stage, and a
default `ProjectValidationPipeline` with `RunBuildStage = true` is a silent no-op, so the registration is
mandatory and tested.
This is deliberately not a second validation pipeline. The Build stage remains the only extension point, the
Core structural validator remains available to `validate` without a compiled model, and a model that is missing
or stale relative to the project is a caller contract problem, not a user-data diagnostic.

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

## Build orchestration home (Step 12)

Build orchestration lives in `Mocassin.Packages.Build`, not in `Mocassin.Cli` and not in
`Mocassin.Compilation`. The CLI and the Avalonia build UI must consume one pipeline and one typed event
stream; the CLI only parses, formats, and maps outcomes to exit codes. `Mocassin.Compilation` stays a pure
synchronous compiler with no project loading, templates, jobs, or package I/O. `Mocassin.Packages` already
owns every stage the pipeline sequences, so no new project is needed.

`Mocassin.Packages` does not reference `Mocassin.Results`. Post-publication verification is composed by the
caller with `IPackageVerifier`; this preserves the one-reader rule and avoids a writer-to-reader project edge.

## `System.CommandLine` for the CLI (Step 12)

The CLI uses `System.CommandLine` 3.0.0 (MIT, pinned, referenced only by `Mocassin.Cli`); the decision is
recorded in ADR 0015 in `docs/planning/adr/`. The owner replaced the round-2
hand-written-parser decision on 2026-11-17 after an AOT/full-trim verification: on `net11.0`/`linux-x64` with
`PublishAot=true` and `TrimMode=full`, 3.0.0 produced zero IL2xxx/IL3xxx warnings, with a positive control
proving the analyzers and ILC warning channel were active; the native binary parsed, validated, printed
help/version, and reported parse errors like the JIT build. The `net10.0` asset declares `IsTrimmable` and
`IsAotCompatible`, and the dependency graph is empty for `net10.0+`. The `netstandard2.0` fallback and
Windows/macOS AOT are untested.

The library owns command-line tokenization (after the CLI expands response files), arity, help, and the
`[suggest]` directive. The CLI owns the root `--version` action, the command graph, typed `ParseResult`
binding, `CLI001` wording, and exit-code policy. The library's default parse-error
exit code (1) is remapped to 2. Feature 1 adds the required `verify.sh aot` gate: a zero-warning minimal host
plus a **separate** positive control that must emit IL2026/IL3050 (the control project opts out of
warnings-as-errors for its own build; the host stays strict), and a host parse-error case that exercises the
`CLI001`/2 remap. clang/lld/ILCompiler are Tier 1 and missing tools hard-fail, never skip; their versions are
observed apt/NuGet resolutions checked by `06` §9, not a containerfile pin, and a version mismatch is not a gate
failure. The gate is local: feature 7 runs `verify.sh all --require` at closeout and records the output, but the
closeout GitHub workflow does not run `verify.sh`. Because `aot` is in `all`, every step closeout from Step 12
requires the Tier 1 toolchain. The real CLI publish is recorded as evidence, not a gate, because
SQLite/MessagePack are unverified. Only `linux-x64` AOT is proven, so Step 31's packaging gate still owns the
platform matrix and the AOT/single-file decision.

Response files (`@file`) are a CLI contract, not the library default. The CLI owns tokenization and recursive
expansion and disables the library hook; the documented syntax and limits are 1 MiB of raw bytes per file,
4 MiB of raw response-file bytes across the command line, depth 8, 10,000 tokens contributed by response files, and 32 files. Missing,
unreadable, invalid, or oversized files are `CLI001`/2, not `CLI002`/3. The full contract is the step page §5.3
and `reference/cli.md`.

No `completion` command is shipped: the owner decided on 2026-11-17 to remove it. The library provides the
`[suggest]` protocol for `dotnet-suggest`, not shell-script generation, and `dotnet-suggest` is not installed.
Shell integration is deferred to Step 31 packaging and is not part of the Step 12 contract.

## CLI exit codes and diagnostics (Step 12)

The .NET CLI uses `0` success, `1` validation/model/package-invalid/unhealthy-doctor, `2` usage, `3` I/O,
`4` compilation/parameterization, `5` reserved partial success, `6` cancelled, and `10` unexpected internal.
`validate` keeps its Step 07 0/1/2/3 subset. `build` is all-or-nothing, so it never emits `5`; a selected job
that fails lattice generation aborts the whole package.

CLI-only failures use `CLI001`–`CLI009` descriptors in `Mocassin.Contracts.Cli`, composed into the default
diagnostic catalog like the `PKG` descriptors. Domain failures keep `PROJ`/`STR`/`TRN`/`ENM`/`SIM`/`CMP`/
`PRM`/`JOB`/`PKG` codes; the CLI never rewrites a domain diagnostic. A parser-library parse error is the only
source of `CLI001`; it is mapped to exit 2, never the library's default 1, and the library's sentence is a hint
rather than the stable message.

## CLI reports and `build-v1` (Step 12)

`mocassin validate --json` keeps `validation-v1`. Every report-backed command prints exactly one report object
with `--json`; each report is a manifest-owned `reports` descriptor with its own `schemaVersion` and generated
schema. `build-v1` replaces the Step 10 interim `build-preview-v1`: one schema, `mode = build` or `preview`,
and a single `--json` shape for both paths. `build-preview-v1` was never manifest-registered, so no consumer
loses a published contract.

JSON-Lines events stay on stderr and the final report on stdout. Step 12 adds the additive `build.stage` event
and optional `stage`/`current`/`etaS` fields; the events schema version stays 1. The manifest records required
event fields only; optional fields are documented in the data contract and asserted by the CLI event tests.

`BuildResult` carries a `BuildIdentity` (project ID, model/document hashes, model/parameterization
signatures) and a failure kind. The CLI, not the library, maps the result to an exit code; `Io` is separate
from `Load` so project/output/disk failures map to 3 while parse/content/validation failures map to 1. The
library reports cancellation as a status and the CLI adds `CLI006`; no CLI-specific diagnostic leaks into the
GUI-facing pipeline. `build-v1` deliberately has no package-size estimate: a preview is size-neutral, and a
full build’s measured `package.sizeBytes` is authoritative.

`BuildResult.Preview` is the single preview output. The pipeline evaluates the preview after expansion with
the shared `BuildPreviewBuilder`/`ILatticePreviewService` and returns a typed `BuildPreview`; the CLI and
Step 26 map it and never reload, recompile, or run a second preview. A separate `PreviewAsync` was rejected
because it would create two orchestration paths for the same work. `BuildRequest.Options` is the only clock/
throttle/channel injection point. The report guarantee is limited to `validate` and the seven report-backed
commands: help/version and reserved commands do not accept `--json`; `clean --json` without
`--dry-run` still emits `clean-v1` with `CLI008`. `--json` makes stderr JSONL-only.

## Build template versus job template (Step 12)

`--template` selects a `BuildTemplate` (job template + parameterization + output name); `--job-template`
selects a job template directly; `--parameterization` overrides the recipe. The Step 10 preview used
`--template` for a job template; that pre-release meaning is replaced so the option matches the authored
build-template concept and the `BuildTemplate` record. A job-template ID passed to `--template` is a usage
error with a hint.

## Job selection normalization (Step 12)

`--jobs` is applied after the complete template expansion, so IDs, seeds, and lattice requests are the
full-expansion values. The writer records a canonical normal form — `all`, or sorted, deduplicated, merged
ranges such as `1-3,5,7-9` — not the user's text. Equivalent selections therefore produce identical
`schema_info.job_selection` and package content. Unselected jobs are absent and leave gaps; they are never
renumbered.

## `--compact` preset (Step 12)

`--compact` is one size preset: Zstd quality `9` plus `VACUUM` before publication. The Step 11 page named
`VACUUM`; its amendment and the user guide named quality `9`. Both are honoured. It changes compressed bytes
and file size only; payload content, signatures, job IDs, and `package-summary-v1` are unchanged.

## Cancellation after publication (Step 12)

Cancellation before the writer’s atomic rename deletes the temp file and preserves any existing package.
Cancellation after the rename cannot roll back a valid published artifact, so the package remains, the report
sets `published = true` and `verification = "skipped"`, and the command exits `6`. A second signal is left to
the platform default.

## Clean dry-run scope (Step 12)

Step 12 implements only `clean --dry-run`: it lists `Job%05d` folders, optionally filtered by age, and never
deletes a file, folder, package, or results row. Full deletion and `.msr` cleanup are Step 28. The dry run is
the safety-relevant half and gives operators a preview before the destructive command exists.

## CLI project templates (Step 12)

`new --template blank|ceria` clones an embedded project template from
`Mocassin.Packages.Projects.Templates` with a fresh UUIDv7, master seed, name, and timestamps. The ceria
template is a copy of the model-definition sample, not a buildable job sample; a freshness test compares its
canonical model hash with `samples/ceria/ceria.mocprj`. Step 23A’s creation UI uses the same factory and
assets. The assets are embedded resources, so `new` needs no template directory, network, or install layout.

## CLI auxiliary commands (Step 12)

- `compile` reports the ten compiler stages (`resolve` through `report`) and writes the derived
  `compiled-inspection-v1` document only with `-o`. A directory target, missing parent, or invalid path is
  `CLI002`/3; the command never truncates or replaces a project.
- `inspect` deliberately tolerates non-fatal package defects and reports the metadata it can read. Payload
  corruption and signature mismatches are `verify` findings. Only a fatal structure failure is `CLI009`/1.
  This is an intended improvement over the original model builder, where build/read failures appeared as text
  in a log console and no read-only inspection existed. A missing, directory, or unreadable path is checked
  before the inspector opens the file and is `CLI002`/3.
- `verify-v1` uses the shared envelope’s single `diagnostics[]` for `PKG` findings; `checks[]` is the ordered
  check list, not a second diagnostics channel.
- `doctor` runs the seven named checks in a fixed order. Free space below 100 MiB is a warning, not a failure,
  because the value is advisory and would otherwise make healthy CI hosts dependent on their disk size. A
  throwing check becomes a reported `fail` with its exception type, so one defective check cannot suppress the
  rest of the diagnosis.
- `clean` without `--dry-run` is refused with `CLI008`/2 but still performs the read-only listing, so an
  operator can inspect the selection before Step 28 adds deletion. The reserved `--results` option emits a
  minimal report without opening the package. No deletion code exists in Step 12.
- Failure injection is limited to true boundaries and command seams: the `IDoctorCheck` list and
  `ISolverProbe` for doctor outcomes, the project-template factory and `TimeProvider` for `new`, the inspector
  and verifier seams for package failures, and a scripted `IBuildPipeline` for unreachable failure kinds.
  Build orchestration, selection, expansion, lattice generation, preview, writing, inspection, and verification
  use real objects and real SQLite files. A real project fixture also exercises a promoted compiled-bound
  parameterization warning end to end (`PRM003`/`PRM006` → exit 4), so that rule is no longer seam-only.


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
