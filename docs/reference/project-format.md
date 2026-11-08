# Project Format (`.mocprj`)

A MOCASSIN project is a single UTF-8 JSON document with the `.mocprj` extension. The format is versioned by
`schemaVersion`; the current version is `1`. There is no reader or writer for the original MOCASSIN project
format.

The machine-readable source for field names, types, and versions is
[`contracts/manifest.json`](https://github.com/seb-eis/mocassin-2/blob/main/contracts/manifest.json). The
generated JSON Schema is
[`contracts/generated/schema/project-v1.schema.json`](https://github.com/seb-eis/mocassin-2/blob/main/contracts/generated/schema/project-v1.schema.json).

## Envelope

```json
{
  "$schema": "https://mocassin.dev/schemas/project-v1.json",
  "schemaVersion": 1,
  "projectId": "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e61",
  "masterSeed": "bbbb…64-hex-chars…",
  "name": "Ceria",
  "description": "Reduced-ceria skeleton project.",
  "createdUtc": "2026-11-04T12:00:00.0000000Z",
  "modifiedUtc": "2026-11-04T12:00:00.0000000Z",
  "model": {},
  "parameterizations": [],
  "jobTemplates": [],
  "buildTemplates": [],
  "viewSettings": {}
}
```

| Field | Required | Meaning |
| --- | --- | --- |
| `$schema` | no | Optional JSON Schema URI. |
| `schemaVersion` | yes | Project schema version. New files write `1`. |
| `projectId` | yes | RFC 4122 UUID string; identifies the project independently of its path. |
| `masterSeed` | yes | 32-byte lowercase hexadecimal seed used for deterministic job/lattice derivation. |
| `name` | yes | User-visible project name. |
| `description` | no | Optional description. |
| `author` | no | Optional author. |
| `notes` | no | Optional free-text notes. |
| `createdUtc` | yes | ISO-8601 UTC creation timestamp. |
| `modifiedUtc` | yes | ISO-8601 UTC last-modified timestamp. |
| `model` | yes | Scientific model body. It is an empty object in Step 01 and is extended by Steps 03–06. |
| `parameterizations` | yes | Parameterization templates. Step 01 stores identity and model signature only. |
| `jobTemplates` | yes | Job templates. Step 01 stores identity and name only. |
| `buildTemplates` | yes | Build recipes binding a job template to a parameterization. |
| `viewSettings` | yes | Camera, render box, and per-object style state. |

The file is written with two-space indentation and LF line endings, without a byte-order mark. Reads reject
a byte-order mark, invalid UTF-8, unknown properties, and a `schemaVersion` newer than the application supports.

## Identity

All user-created objects use strongly typed UUID identifiers. New objects use UUIDv7 so creation order is
sortable. References are typed IDs, never array positions. A `ParticleSetId` cannot be substituted for a
`CellSiteId` at the C# API boundary.

The JSON representation is a lowercase hyphenated UUID string:

```json
{ "id": "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e61" }
```

The implicit void particle and empty particle set use fixed, reserved identifiers. They are not serialized as
ordinary model objects. Dense compiled indices (`ParticleIndex`, `PositionIndex`, `RuleIndex`, …) never appear
in the project file; they belong to the compiled package.

Arrays are sorted by `id` when serialized unless their order is semantic. Semantic orders in later sections
include particle IDs in a particle set, state-change group order, chain linkers, doping priority, reference
path positions, and job configuration order.

## Units

Units are part of the API and schema, not an implicit convention:

| Type | JSON unit | Example field |
| --- | --- | --- |
| `LengthAngstrom` | ångström | cell lengths, cutoffs, distances |
| `EnergyEv` | electronvolt | interaction and site energies |
| `TemperatureKelvin` | kelvin | simulation temperature |
| `ChargeElementary` | elementary charge | particle charge |
| `TimeSeconds` | second | simulation time |
| `FrequencyHz` | hertz | attempt frequency |
| `ElectricFieldVm` | volt per metre | field magnitude |
| `AngleDegrees` | degree | cell angles |

The unit value structs validate finiteness and their allowed range at construction. JSON stores the plain
number; the property name or schema description carries the unit. There are no implicit conversions between
unit types.

## Particles, structure, and symmetry (Step 03 fields)

Step 02 supplies the catalog, cell validation, and coordinate vocabulary. The particle and structure sections
are part of the generated v1 project schema as of Step 03. A missing `particles`, `particleSets`, or
`structure` property loads as the empty/default model, so a Step 01 `"model": {}` document remains valid.

```json
{
  "particles": [
    {
      "id": "…",
      "name": "O2-",
      "symbol": "O",
      "charge": -2.0,
      "isVacancy": false
    }
  ],
  "particleSets": [
    {
      "id": "…",
      "name": "Oxygen site",
      "particleIds": ["…"]
    }
  ],
  "structure": {
    "name": "Ceria",
    "spaceGroup": { "number": 225, "axis": "none", "origin": "none" },
    "cell": { "a": 5.41, "b": 5.41, "c": 5.41, "alpha": 90.0, "beta": 90.0, "gamma": 90.0 },
    "sites": [],
    "dummyPositions": []
  }
}
```

- The implicit void particle and empty particle set use reserved IDs, are not serialized as ordinary objects,
  and must not be reused by authored objects (`PAR010`). `isVoid` is computed and never appears in JSON.
- `particleIds` order is semantic: the first particle is the default occupation of a stable site.
  `particles`, `particleSets`, `sites`, and `dummyPositions` sort by `id` in canonical JSON.
- Particle-set names are unique (case-insensitive) and required (`PAR011`); repeated particle IDs inside a set
  are `PAR012` error.
- The recommended particle-symbol pattern is `^[A-Z][A-Za-z0-9+\-()]{0,10}$`; a mismatch is `PAR008` warning.
- `structure.name` is required in the model and defaults to `New Structure` for a new project; an empty or
  whitespace-only value is `STR013`.
- `spaceGroup` persists only `(number, axis, origin)`. The Hermann–Mauguin symbol, Hall symbol, crystal
  system, operations, and catalog data version are resolved from the embedded catalog.
- `axis` is one of `none`, `uniqueAxisB`, `uniqueAxisC`, `hexagonal`, or `rhombohedral`. `uniqueAxisA` is
  reserved for a future catalog extension and is rejected by the v1 validator with `STR001`.
- `origin` is `none`, `originChoice1`, or `originChoice2`.
- `fractional` is a three-number array `[a, b, c]`. `axis`, `origin`, and `stability` are camelCase strings.
- Cell lengths are in ångström and must be in `0.1..1000 Å` (`STR002`). Angles are in degrees and must be
  strictly between `0°` and `180°`; the Gram determinant must be positive and above `1e-12`; the cell-matrix
  condition number must not exceed `1e12`; crystal-system constraints must hold (`STR003`).
- A structure may define at most 1000 reference sites (`STR012`). Reference positions must be inside `[0,1)`
  before trimming; values within `FractionalTrimTolerance = 1e-10` of a boundary snap, values beyond it are
  `STR004` error. Positions then convert to the exact `24 × 10^12` grid for P1 expansion. The JSON remains
  `double`-based.
- Dummy positions use the same coordinate rule (`STR010`); dummy names are unique (case-insensitive) and
  empty/duplicate names are `STR015` error.
- More than 10,000 total P1 positions is `STR009` warning. A requested supercell product `A*B*C*P` above the
  signed 32-bit linear-index cap is `STR014` error. This deliberately relaxes the original hard 10,000 limit.
- The optional envelope `symmetry` metadata carries `spaceGroupDataVersion`, `spaceGroupDataSha256`, and
  `symmetryContractVersion`. `Mocassin.Packages.Projects.ProjectValidationService` compares it with the
  embedded values and warns `PROJ034` on a mismatch; the compiled payload is authoritative.

The catalog is embedded, not stored in the project. The data version is `spglib-2.8.0/catalog-1`; see
[Compilation Pipeline](../development/compilation-pipeline.md) and
[Symmetry and Geometry](../physics/symmetry.md) for the exact rules.

## Model and templates in Step 01 (historical)

Step 01 deliberately establishes the project envelope before the scientific model domains. At that
checkpoint the model body was an empty object; the particle and structure sections above are added by Step 03.
The template records are still the Step 01 shape:

- a parameterization template has `id`, `name`, `modelSignature`, and `createdUtc`;
- a job template has `id` and `name`;
- a build template has `id`, `name`, `jobTemplateId`, `parameterizationId`, and `outputName`.

Steps 03–06 extend `model` with particles, structure, transitions, energy, lattice, and simulations. Steps 09
and 10 extend the template records. Any semantic change to a released schema requires a migration and a
format decision.

## View settings

`viewSettings` is persisted with the project so a shared project opens with the same view:

- `camera`: `position`, `target`, `up` (three-number arrays), `projection` (`perspective` or `orthographic`),
  `fieldOfViewDegrees`, `nearPlane`, and `farPlane`;
- `renderBox`: `min`, `max`, and `visible`;
- `background`: optional background key;
- `objectStyles`: an array of per-object styles keyed by stable object ID.

Known style keys are `visible`, `color`, `material`, `quality`, `scale`, and `wireframe`. Additional
primitive keys are preserved on round-trip but are not interpreted by the current renderer.

`camera` and `renderBox` are required. `camera.projection`, `renderBox.visible`, `objectStyles`, and the
object-style `visible`, `scale`, and `wireframe` fields are optional. Their defaults are `perspective`, `false`,
an empty array, `false`, `1.0`, and `false` respectively. Missing required camera/render-box objects are
rejected as invalid documents.

## Canonical JSON

`ProjectCanonicalizer.ToCanonicalJson(project)` produces the stable form used for hashing and golden tests:

- UTF-8 without a byte-order mark;
- no insignificant whitespace, LF-only separators;
- object keys in schema order;
- non-semantic arrays sorted by stable ID;
- invariant shortest round-trip numbers;
- `-0.0` normalized to `0.0`;
- timestamps normalized to UTC with seven fractional digits and a `Z` suffix;
- enums written as camelCase strings;
- optional null properties omitted.

`ProjectHasher.Compute(project)` hashes the canonical **model section** and is the Step 01 model-content
identity used as the basis for the compiled-model signature. It is not the compiled model signature from
Step 08. `ProjectHasher.ComputeDocument(project)` hashes the complete canonical project document, including
metadata and view settings, and is what `ProjectDocument` uses for dirty tracking. Step 03 adds the public
`ProjectHasher.ComputeStructure(structure)` for the canonical **structure section** (name, space group, cell,
sites, dummies); it is the structure-preview cache key, not a model or document identity. All use SHA-256 and
return lowercase hexadecimal.

## Migrations

`ProjectMigrations` applies pure `IProjectMigration` transformations in order until the document reaches the
current schema version. A migration must be deterministic and is covered by a fixture from the old version.

The version-1 release has no predecessor, so the migration list is empty and the v1 pipeline is a no-op.
Documents newer than the current version are rejected with `PROJ001` and an actionable message. Documents
that need an unavailable migration are rejected with `PROJ002`.

## Validation and diagnostics

`ProjectValidator.Validate(project)` returns diagnostics in stable severity/code/path order. Validation never
throws for user data. The Step 01 project codes are:

| Code | Meaning |
| --- | --- |
| `PROJ001` | Document schema version is newer than this application supports. |
| `PROJ002` | Document is malformed, unreadable, or cannot be migrated. |
| `PROJ003` | Two objects in one domain share an identifier. |
| `PROJ010` | A reference points to an identifier that does not exist. |
| `PROJ011` | A reference points to an identifier in a different domain. |
| `PROJ020` | Project or template metadata is missing or invalid. |
| `PROJ021` | View settings are invalid or incomplete. |
| `PROJ030` | A save failed; the previous file was left intact. |
| `PROJ031` | Another session holds the editing lock. |
| `PROJ032` | A stale lock was reclaimed. |
| `PROJ033` | An autosave write failed. |
| `PROJ034` | Project symmetry metadata differs from the embedded catalog/contract versions (warning). |

Step 03 adds the `PAR001`–`PAR012` particle codes and the `STR001`–`STR015` structure codes; `PAR003`,
`STR008`, and `STR011` are unused. Their user-facing catalogue and help anchors are completed in Step 07.

## Persistence

`Mocassin.Packages.Projects.ProjectStore` owns all file I/O; `Mocassin.Core` has no file, SQL, or UI
dependency.

- `LoadAsync` reads UTF-8, checks the size cap, parses, migrates, deserializes, and validates references.
  Validation errors still return the loaded project so callers can inspect it.
- `SaveAsync` updates `modifiedUtc`, writes to a temporary file in the target directory, flushes to disk, and
  atomically replaces the target. A failed save never truncates the existing file. Optional `.bak` copies are
  supported.
- `AcquireLockAsync` creates `<project>.mocprj.lock` with PID, host, token, and acquisition time. A same-host
  dead PID or a lock older than the configured timeout is reclaimed; an active lock returns `PROJ031`.
- `WriteAutosaveAsync` writes `<project>.mocprj.autosave` atomically. `LoadAutosaveAsync` reads it, and
  `DeleteAutosave` removes it after recovery. Autosave does not update the project's modified timestamp.
- Lock and autosave files are ignored by Git.

The application layer decides when to autosave. `ProjectDocument` exposes `IsAutosaveDue` and
`MarkAutosaved`; the UI timer belongs to Step 23.

## Editing

`ProjectEditor` applies one pure mutation to an immutable `Project`, validates the candidate, and commits it
only when there are no errors. `ProjectChange` stores before/after snapshots so a bulk edit is one undo step.
`ProjectDocument` owns the current and last-saved snapshots, undo/redo stacks, dirty tracking, and autosave
timing. It is I/O-free; the application saves through `ProjectStore` and calls `MarkSaved`.

## Fixtures and samples

The real writer produces these Step 01 fixtures:

- `samples/blank/blank.mocprj` and `tests/contracts/project-minimal.mocprj` — minimal valid project;
- `samples/ceria/ceria.mocprj` and `tests/contracts/project-ceria.mocprj` — ceria skeleton.

Tests validate both the generated JSON Schema and canonical round-trips. Property tests exercise arbitrary
JSON bytes to ensure the parser fails only with a structured JSON error.
