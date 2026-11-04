# Step 01 — Domain Kernel and Project Format

**Phase:** A. Foundations  
**Depends on:** 00
**Delivers:** The immutable domain kernel, stable IDs, units, diagnostics, and the versioned JSON project file
with canonical serialization and migrations.

## Implementation status

- **Status:** In progress
- **Step branch:** `step/01-domain-kernel`
- **Step CI:** —
- **Main merge:** —
- **Step tag:** —
- **Last updated:** 2026-11-04

| Feature | Branch | Status | Local gates | Step CI | Squash commit |
| --- | --- | --- | --- | --- | --- |
| `core-kernel` | `feature/step-01-domain-kernel/core-kernel` | Squashed | Core tests 90/90; full solution tests pass; `dotnet format --verify-no-changes` passes | `41903504097` — green | `5f662f5` |
| `project-format` | `feature/step-01-domain-kernel/project-format` | Squashed | Core tests 106/106; full solution tests pass; `dotnet format --verify-no-changes` passes | `41906159043` — Windows newline failure; fixed below | `a36f878` |
| `fix-windows-newline` | `feature/step-01-domain-kernel/fix-windows-newline` | Awaiting approval | Full solution tests pass; `dotnet format --verify-no-changes` passes | — | — |

## Goal

Define the data model that every later step builds on. A project must round-trip losslessly, serialize
deterministically, validate references, and evolve through explicit schema migrations. The kernel must be free
of UI, SQL, and solver dependencies.

## Original reference

- Original project persistence: SQLite table `MocassinProjectGraphs(Id, Name, ProjectGuid, ProjectName,
  ProjectJson)`; `ProjectJson` is a Newtonsoft.Json graph with `$id`, `$ref`, and assembly-qualified `$type`.
- Original model objects: mutable `ModelObject` with a settable `Index` and a string `Key`; references are
  stubs resolved by `ModelDataTracker`.
- Original versioning: `MocassinProject.Version` is informational only; no migration pipeline.
- Original customization compatibility: `ModelHash = ProjectModelData.Json.GetHashCode()`, which is
  process-randomized on modern .NET and therefore unusable.

The new design replaces the graph serializer with a plain JSON document, stable IDs, and content hashes.

## Design

### 1. Project document

```csharp
public sealed record Project(
    int SchemaVersion,
    ProjectMetadata Metadata,
    ModelDefinition Model,
    ImmutableArray<ParameterizationTemplate> Parameterizations,
    ImmutableArray<JobTemplate> JobTemplates,
    ImmutableArray<BuildTemplate> BuildTemplates,
    ViewSettings ViewSettings);
```

- One `.mocprj` file contains one project. The application's "workspace" is the set of open project files and
  recent files, not a multi-project database.
- All collections are `ImmutableArray<T>` in the kernel; editing produces a new `Project` value.
- `ProjectMetadata` holds `ProjectId` (UUIDv7), `MasterSeed` (32-byte hex; generated at creation if absent),
  name, description, created/modified timestamps, and optional author/notes.
- `ViewSettings` holds camera, render box, visibility flags, and per-object style. It is UI state but belongs
  to the project so a shared project opens with the same view.

### 2. Identity

Define strongly typed IDs as `readonly record struct` wrappers over `Guid`:

```csharp
public readonly record struct ParticleId(Guid Value);
public readonly record struct ParticleSetId(Guid Value);
public readonly record struct CellSiteId(Guid Value);
public readonly record struct StateChangeId(Guid Value);
public readonly record struct StateChangeGroupId(Guid Value);
public readonly record struct TransitionChainId(Guid Value);
public readonly record struct KineticTransitionId(Guid Value);
public readonly record struct MetropolisTransitionId(Guid Value);
public readonly record struct ClusterId(Guid Value);
public readonly record struct OccupationExchangeId(Guid Value);
public readonly record struct DopingId(Guid Value);
public readonly record struct SimulationId(Guid Value);
public readonly record struct ParameterizationId(Guid Value);
public readonly record struct JobTemplateId(Guid Value);
public readonly record struct BuildTemplateId(Guid Value);
```

- IDs are generated with UUIDv7 for sortability.
- JSON serializes IDs as strings; converters reject non-UUID values.
- References are typed IDs; a `ParticleSetId` cannot accidentally be used where a `CellSiteId` is expected.
- The implicit void particle and empty particle set use fixed, well-known IDs documented in the schema.
- Compiled dense indices are separate types (`ParticleIndex`, `PositionIndex`, `RuleIndex`, …) and never
  appear in the project file.

### 3. Units

Use explicit unit structs at API and serialization boundaries:

```csharp
public readonly record struct LengthAngstrom(double Value);
public readonly record struct EnergyEv(double Value);
public readonly record struct TemperatureKelvin(double Value);
public readonly record struct ChargeElementary(double Value);
public readonly record struct TimeSeconds(double Value);
public readonly record struct FrequencyHz(double Value);
public readonly record struct ElectricFieldVm(double Value);
public readonly record struct AngleDegrees(double Value);
```

- Each validates finiteness and allowed range in its constructor or a `TryCreate`.
- JSON converters write plain numbers; the property name or schema description carries the unit.
- Internal solver physics uses eV and kT; the .NET compilation layer converts where required.
- No implicit conversions between unit types. A `double` must be wrapped explicitly.

### 4. Diagnostics

```csharp
public enum DiagnosticSeverity { Info, Warning, Error }

public sealed record Diagnostic(
    string Code,
    DiagnosticSeverity Severity,
    string Message,
    ModelPath? Path = null,
    string? Hint = null,
    ImmutableArray<ModelPath> Related = default);

public sealed record ModelPath(string Domain, Guid ObjectId, string? Property = null);
```

- `DiagnosticBag` accumulates diagnostics and exposes `HasErrors`, `Errors`, `Warnings`, and a stable sort
  (severity, code, path).
- Codes are stable strings (`MODEL001`, `SYM002`, `JOB010`, …) and documented in
  `docs/reference/diagnostics.md` (created in step 07).
- Validation never throws for user data. Exceptions are reserved for contract violations and I/O failures.

### 5. Canonical JSON

- `System.Text.Json` with a source-generated `JsonSerializerContext`.
- `JsonSerializerOptions`:
  - `PropertyNamingPolicy = camelCase`,
  - `WriteIndented = true` for files, `false` for hashing,
  - `DefaultIgnoreCondition = WhenWritingNull`,
  - `NumberHandling = Strict`,
  - `UnmappedMemberHandling = Disallow`,
  - invariant culture (JSON is culture-independent by definition).
- Arrays of model objects are sorted by `Id` before writing, except semantically ordered arrays (chain
  groups, linkers, doping priority, job configs).
- Enums are serialized as camelCase strings, not numbers.
- `ProjectCanonicalizer.ToCanonicalJson(project)` produces a stable byte sequence for hashing and tests.
- `ProjectHasher.Compute(project)` returns a SHA-256 hash of the canonical model section, used for dirty
  tracking and as the basis for the compiled-model signature. It is distinct from the compiled-model
  signature in Step 09.

### 6. Migrations

- `IProjectMigration` has `int FromVersion`, `int ToVersion`, and `JsonNode Apply(JsonNode document)`.
- `ProjectMigrations` applies migrations in order until `SchemaVersion` is current.
- A migration must be pure, deterministic, and covered by a fixture from the old version.
- Readers reject documents newer than the current schema with a clear diagnostic and a link to the app
  version that supports them.
- The first release starts at `schemaVersion = 1`; the framework exists from day one.

### 7. Persistence (`Mocassin.Packages.Projects`)

`ProjectStore` lives in `Mocassin.Packages`, not `Mocassin.Core`, so the domain kernel remains I/O-free.

- `ProjectStore.LoadAsync(path, ct)` reads, validates UTF-8, parses, migrates, and validates references.
- `ProjectStore.SaveAsync(project, path, ct)`:
  1. serialize to a temp file in the same directory,
  2. flush and fsync,
  3. atomically replace the target (with a `.bak` of the previous file optional),
  4. update `modifiedUtc`.
- A failed save never truncates the existing project.
- A project lock file (`.mocprj.lock`) prevents two app instances from editing the same file; stale locks are
  detected by PID/host and a timeout.
- Autosave writes a recovery file (`.mocprj.autosave`) every N minutes when dirty.

### 8. Editing model

- The kernel is immutable. `ProjectEditor` applies a mutation to a copy and returns a new `Project`.
- `ProjectDocument` holds the current project, the last saved project, and an undo/redo stack of
  `ProjectChange` records (step 23 wires it to the UI).
- Edits are validated before commit; a failed edit leaves the document unchanged and returns diagnostics.
- Bulk operations (create from template, duplicate, paste) are single undoable changes.

### 9. View settings

- Store camera position/target/up, projection mode, FOV, near/far, render box, background, and per-object
  style keys (visible, color, material, quality, scale, wireframe).
- Unknown style keys survive a round-trip but are not interpreted.
- Defaults are provided if settings are missing.

## Implementation tasks

- [ ] Create `Mocassin.Core` namespaces: `Ids`, `Units`, `Diagnostics`, `Model`, `Project`, `Editing`.
- [x] Implement the ID structs, JSON converters, and a UUIDv7 generator.
- [x] Implement the unit structs and their JSON converters.
- [x] Implement `Diagnostic`, `DiagnosticBag`, and `ModelPath`.
- [x] Implement the `Project` envelope and metadata records.
- [x] Implement `JsonSerializerContext` and canonical serialization.
- [ ] Implement `ProjectStore` in `Mocassin.Packages.Projects` with atomic save, lock file, and autosave.
- [x] Implement the migration pipeline and a no-op v1 migration.
- [ ] Implement `ProjectEditor`, `ProjectDocument`, and `ProjectChange`.
- [ ] Add sample projects: blank and ceria skeleton.
- [ ] Add a JSON Schema file generated from the C# types (or maintained by hand and validated in tests).
- [ ] Document the format in `docs/reference/project-format.md`.

## Acceptance criteria

- [ ] A `Project` round-trips through JSON with no loss for all sample projects.
- [ ] Canonical serialization is byte-identical after reordering collections.
- [ ] Invalid references (unknown IDs, wrong domain IDs) produce diagnostics with paths.
- [ ] A future `schemaVersion` is rejected with a clear message.
- [ ] A v1 fixture loads and saves without changes.
- [ ] A failed save (simulated I/O error) leaves the original file intact.
- [ ] A lock file prevents concurrent edits and recovers from a stale lock.
- [ ] The JSON Schema validates all fixtures.
- [ ] Documentation written

## Documentation

At this point, document the persisted project model:

- `docs/reference/project-format.md` — JSON envelope, IDs, units, canonicalization, migrations, atomic save.
- `docs/development/architecture.md` — immutable domain kernel and dependency direction.
- `docs/development/design-decisions.md` — stable IDs, units, diagnostics, one project per file.
- `docs/glossary/notation.md` — ID/unit conventions introduced here.

## Tests

| Test | Assertion |
| --- | --- |
| Round-trip | `Load(Save(project)) == project` for blank, ceria, and a template-heavy fixture |
| Canonical bytes | Shuffled collections produce identical canonical JSON |
| Hash stability | Same model produces the same hash across processes and OSes |
| Migration | Each historical fixture migrates to the current version |
| Future version | Reader rejects with `PROJ001` |
| Reference validation | Unknown/wrong-domain IDs produce `PROJ01x` diagnostics |
| Atomic save | Kill/fail during save; original remains valid |
| Locking | Second writer is rejected; stale lock is reclaimed |
| Fuzz | Random JSON does not crash the parser or allocate unbounded memory |

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Immutable editing is awkward in the UI | Provide `ProjectEditor` helpers and undo/redo; view models never mutate records directly |
| Schema churn | Freeze v1 before step 03; later changes require a migration and ADR |
| ID confusion across domains | Strongly typed IDs and analyzer tests |
| Unit conversion mistakes | Unit structs, no implicit conversions, tests at boundaries |
| Canonicalization bugs | Property-based and golden tests from day one |

## Done when

A project can be created, edited, saved, reloaded, validated, and migrated without a UI, and the format is
documented and covered by golden fixtures.
