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
| `model` | yes | Scientific model body. Steps 03–05 add `particles`, `particleSets`, `structure`, the flat transition sections, and the flat energy sections; lattice and simulation sections are added by Step 06. |
| `parameterizations` | yes | Parameterization templates. Step 09 extends the Step 01 identity stub with sparse pair, group, site-energy, and rule-frequency value sets. |
| `jobTemplates` | yes | Job templates. Step 10 extends the Step 01 identity stub with collections, configurations, inheritable settings, lattice requests, dopings, and selection optimizers. |
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

For authoring guidance see [Particles and particle sets](../user-guide/particles.md) and
[Crystal structure](../user-guide/structure.md). The ceria and barium-zirconate sample projects under
`samples/` are generated by the real project writer and mirrored in `tests/contracts/`.

The catalog is embedded, not stored in the project. The data version is `spglib-2.8.0/catalog-1`; see
[Compilation Pipeline](../development/compilation-pipeline.md) and
[Symmetry and Geometry](../physics/symmetry.md) for the exact rules.

## Transitions (Step 04 fields)

Step 04 adds five flat transition sections. Missing sections load as empty arrays; a transition-free project
keeps its Step 01/03 canonical hash because empty sections are omitted when writing.

```json
{
  "stateChanges": [
    { "id": "…", "name": "O2- -> VO", "donorParticleId": "…", "acceptorParticleId": "…" }
  ],
  "stateChangeGroups": [
    { "id": "…", "name": "O vacancy", "stateChangeIds": ["…"] }
  ],
  "chains": [
    {
      "id": "…",
      "name": "3-site migration",
      "pattern": "migration3",
      "groupIds": ["…", "…", "…"],
      "linkers": ["dynamic", "dynamic"],
      "generatesReverse": true
    }
  ],
  "kineticTransitions": [
    {
      "id": "…",
      "name": "O migration",
      "chainId": "…",
      "referencePath": [[0.25, 0.25, 0.25], [0.0, 0.25, 0.25], [0.25, 0.25, 0.75]],
      "reverseTransitionId": null
    }
  ],
  "metropolisTransitions": [
    {
      "id": "…",
      "name": "O exchange",
      "chainId": "…",
      "siteAId": "…",
      "siteBId": "…",
      "reverseTransitionId": null
    }
  ]
}
```

- `stateChangeIds`, `groupIds`, `linkers`, and `referencePath` are semantic-order arrays and are never
  re-sorted. All five transition collections sort by `id` in canonical JSON.
- `pattern` is one of `metropolis2`, `migration3`, `interstitialcy5`, `interstitialcy7`, `vehicle5`,
  `vehicle6`, or `custom`; `linkers` are `dynamic` or `static`; enums are camelCase strings.
- `linkers.length` equals `groupIds.length - 1`. `generatesReverse` is true only for `metropolis2` and
  `migration3`.
- A `referencePath` is an array of three-number fractional arrays and may leave `[0,1)`. Points must match
  exactly one P1 position/owning site within `PathMatchTolerance = 1e-6`; see
  [Transitions reference](transitions.md).
- A `metropolisTransitions` entry binds two stable reference sites. The same site may be selected twice for an
  intra-sublattice exchange.
- `reverseTransitionId` is an optional symmetric same-kind link. Complex mechanisms use it instead of automatic
  inverse rules; a missing link is `TRN018` warning.
- The project `schemaVersion` stays `1`: the sections are additive under the pre-release v1 format, and the
  manifest/schema/fixtures are regenerated in the same step.
- `TransitionPreview` is derived data and is never written to the project.

`ProjectHasher.ComputeTransition(project, transitionRef)` hashes the transition closure for preview caching. It
is separate from the model and document hashes.

## Energy (Step 05 fields)

Step 05 adds four flat energy sections. Missing sections load as the default/empty energy model, and the
default stable environment plus empty collections are omitted on write so pre-Step-05 hashes stay stable.

```json
{
  "stableEnvironment": {
    "cutoff": 2.7,
    "filters": [
      { "start": 0.0, "end": 1.0, "siteAId": "…", "siteBId": "…" }
    ]
  },
  "unstableEnvironments": [
    { "siteId": "…", "cutoff": 1.5, "filters": [] }
  ],
  "clusters": [
    {
      "id": "…",
      "name": "Oxygen migration edge",
      "centerSiteId": "…",
      "offsetVectors": [[-0.25, 0.0, 0.0], [0.25, 0.0, 0.0]]
    }
  ],
  "siteEnergies": [
    { "siteId": "…", "entries": [{ "particleId": "…", "energyEv": -0.5 }] }
  ]
}
```

- A missing `stableEnvironment` loads a `1.0 Å` cutoff with no filters. The default environment is omitted on
  write when it equals the default; empty `unstableEnvironments`, `clusters`, and `siteEnergies` arrays are
  also omitted.
- `cutoff` and filter radii are in ångström. Every environment cutoff must be positive and finite; a cutoff
  above half the smallest cell dimension is a periodic-ambiguity warning.
- Filters remove distances in the inclusive `[start, end]` range with the `1e-5 Å` tolerance. Stable filters
  are symmetric; unstable filters are one-directional and must start at the environment site.
- `offsetVectors` is semantic and never re-sorted. It contains the surrounding positions only: 2–8 offsets
  (3–9 positions including the centre).
- `unstableEnvironments` sort by `siteId`, `clusters` by `id`, `siteEnergies` by `siteId`, filters by
  `(start, end, siteAId, siteBId)`, and site-energy entries by `particleId`.
- Site energies are sparse; a missing entry is `0 eV`. A `(siteId, particleId)` pair may appear once and the
  value must be finite and within `-100..100 eV`.
- The project `schemaVersion` stays `1`: the sections are additive under the pre-release v1 format and the
  manifest/schema/fixtures are regenerated in Step 05.
- Pair interactions, cost previews, `GeometryId`, and `EnergyId` are derived data and are never written to the
  project.

For authoring guidance see [Energy model](../user-guide/energy.md). The ceria and barium-zirconate samples
include energy sections and are mirrored in `tests/contracts/`.

## Lattice and doping (Step 06 fields)

Step 06 adds two flat lattice sections. Missing sections load as empty, and empty arrays are omitted on write so
pre-Step-06 model and document hashes stay stable.

```json
{
  "occupationExchanges": [
    { "id": "…", "name": "Ce4+ -> Ce3+", "siteId": "…", "sourceParticleId": "…", "targetParticleId": "…" }
  ],
  "dopings": [
    {
      "id": "…",
      "name": "Reduce ceria",
      "priority": 0,
      "primaryExchangeId": "…",
      "dependentExchangeId": "…",
      "chargeCompensation": true
    }
  ]
}
```

- `occupationExchanges` sort by `id`; `dopings` sort by ascending `priority` and then `id`.
- Occupation-exchange and doping names are unique case-insensitively within their collection.
- `priority` is unique and in `0..9999`; the order is the deterministic application order.
- `dependentExchangeId` is absent when `chargeCompensation` is `false`. When compensation is `true`, it is
  required and the primary/dependent charge deltas must be non-zero with opposite signs.
- The default building block is derived (first particle of a stable site's set, void on unstable sites) and is
  never persisted.
- Model-level validation covers `LAT001`–`LAT011`. Job-template structure, fractions, supercells, truncation,
  compensation counts, extensions, and previews belong to Step 10 (`JOB001`–`JOB020`).

## Simulations (Step 06 fields)

The `simulations` array holds KMC and MMC definitions. The persisted record is a flat object with a `settings`
object; only the matching transition array and kind-specific settings are present.

```json
{
  "simulations": [
    {
      "id": "…",
      "name": "O migration 773 K",
      "kind": "kmc",
      "kineticTransitionIds": ["…"],
      "settings": {
        "temperatureK": 773.0,
        "targetMcsp": 200,
        "writeOutCount": 100,
        "timeLimitSeconds": 31536000,
        "minSuccessRate": null,
        "jobCount": 12,
        "rngSeed": null,
        "executionFlags": ["noJumpLogging"],
        "instruction": null,
        "preRunMcsp": 250,
        "electricFieldModulusVm": 1.0e7,
        "electricFieldDirection": [1.0, 0.0, 0.0],
        "normalizationEnergyEv": null,
        "maxAttemptFrequencyHz": null
      }
    }
  ]
}
```

- `kind` is `kmc` or `mmc` and must agree with the concrete record, transition array, and kind-specific
  settings (`SIM023`).
- `kineticTransitionIds` and `metropolisTransitionIds` preserve authored order and may not contain duplicate
  `(kind, id)` references. A simulation may reference transitions used by other simulations.
- `executionFlags` is a sorted array of `noSaving`, `noJumpLogging`, `useFastExp`, and `deterministic`; the
  empty array means `None`. The `.msl` payload uses the separate `SolverJobFlags` word, including the derived
  kind, pre-run, and dual-DOF bits.
- `minSuccessRate` `0` canonicalizes to `null`; an empty `rngSeed` canonicalizes to `null`; KMC
  `normalizationEnergyEv` `0.0` canonicalizes to `null`.
- Simulation names are unique case-insensitively (`SIM009`). Step 06 owns `SIM001`–`SIM037`; job expansion,
  effective counts, and package writing belong to Steps 10/11.
- The generated schema keeps optional kind-specific fields because the contract generator has no
  `oneOf`/discriminator support. The validator, not the schema, rejects a mismatched kind shape.

For authoring guidance see [Lattice and doping](../user-guide/lattice.md) and
[Simulations](../user-guide/simulations.md). The populated sample projects are mirrored in `tests/contracts/`
as `project-lattice-populated.mocprj` and `project-simulations-populated.mocprj`.

## Job templates (Step 10 fields)

Step 10 extends the Step 01 `jobTemplates` identity stub with collections, configurations, inheritable settings,
lattice requests, dopings, and selection optimizers. Missing sections load as empty; empty collection,
configuration, and doping arrays are omitted on write so pre-Step-10 documents keep their canonical hash.
`schemaVersion` stays `1` under the same additive pre-release rule as Steps 03–09.

```json
{
  "jobTemplates": [
    {
      "id": "…",
      "name": "Ceria temperature sweep",
      "collections": [
        {
          "id": "…",
          "name": "T773",
          "simulationId": "…",
          "baseSettings": { "temperatureK": 773.0 },
          "jobCountPerConfig": 2,
          "rngSeed": "ceria-seed",
          "selectionOptimizers": [
            { "siteId": "…", "particleId": "…" }
          ],
          "configs": [
            {
              "id": "…",
              "name": "x001",
              "overrides": { "targetMcsp": 150 },
              "lattice": { "a": 6, "b": 6, "c": 6 },
              "dopings": [{ "dopingId": "…", "fraction": 0.01 }]
            }
          ]
        }
      ]
    }
  ]
}
```

| Field | Required | Meaning |
| --- | --- | --- |
| `id`, `name` | yes | Stable template identity and user-visible name. |
| `collections` | no | Collections in authored order. An empty list is `JOB001`. |

A collection:

| Field | Required | Meaning |
| --- | --- | --- |
| `id`, `name` | yes | Stable collection identity and unique, non-empty name (`JOB007`). |
| `simulationId` | yes | Existing simulation; an unknown reference is `JOB002`. |
| `baseSettings` | no | Partial setting overrides; absent inherits the compiled simulation defaults. |
| `jobCountPerConfig` | no | Duplicate count per configuration (`1..1000`); absent inherits `Simulation.jobCount`. |
| `rngSeed` | no | Optional seed text; absent inherits the simulation/project seed. |
| `selectionOptimizers` | no | KMC `(siteId, particleId)` start-option removals in authored order. |
| `configs` | no | Configurations in authored order. An empty list is `JOB003`. |

A configuration:

| Field | Required | Meaning |
| --- | --- | --- |
| `id`, `name` | yes | Stable configuration identity and unique, non-empty name within the collection (`JOB007`). |
| `overrides` | no | Partial setting overrides applied after `baseSettings`. |
| `lattice` | yes | `{ "a", "b", "c" }` cell counts; each in `1..256` (`JOB004`). |
| `dopings` | no | `{ "dopingId", "fraction" }` entries; fraction in `0..1` (`JOB005`, `JOB006`, `JOB017`). |

`baseSettings` and `overrides` share `JobSettings`. A missing or `null` field inherits; an explicit value
replaces it, and an explicit `0`/`ExecutionFlags.None` is a real override. The shared fields are
`temperatureK`, `targetMcsp`, `writeOutCount`, `timeLimitSeconds`, `minSuccessRate`, `flags`, `instruction`, and
`extensions`. KMC-only fields are `preRunMcsp`, `electricFieldModulusVm`, `electricFieldDirection`,
`normalizationEnergyEv`, and `maxAttemptFrequencyHz`; MMC-only fields are `breakTolerance`,
`breakSampleLengthMcsp`, `breakSampleIntervalMcsp`, and `resultSampleMcsp`. A KMC-only field on an MMC collection
or the reverse is `JOB015`. The accepted ranges match the simulation settings in
[Project format — Simulations](#simulations-step-06-fields).

`extensions` is an ordered array of `{ "id": string, "config": object }` entries inside `baseSettings` or
`overrides`. A missing or `null` list inherits; a non-null list replaces the inherited list as a whole. Ids are
unique and non-empty and at most 64 characters; each `config` is a JSON object of at most 64 KiB; at most 16
entries and 256 KiB total are allowed per effective list. The config is stored as canonical JSON (sorted object
property names, array order preserved, number tokens preserved verbatim) so the project hash is deterministic.
Numbers are not reinterpreted as IEEE-754 binary64: integers above 2^53 stay exact, values outside the binary64
range are accepted, and `1.0` is a different token from `1`. The extension consumers (Steps 27/28) own the
schema and must define numeric ranges in terms of JSON numbers, not binary64 round-trips. Invalid content or
bounds are `JOB018`; the concrete extension schema belongs to the extension consumer.

### Canonical ordering

1. `jobTemplates` by ascending `JobTemplateId`.
2. Collections in authored order; configurations in authored order. Collection and configuration names are
   unique case-insensitively within their parent.
3. `selectionOptimizers` in authored order. Duplicate `(siteId, particleId)` pairs are `JOB017`.
4. `dopings` by ascending resolved doping priority, then by `DopingId`. This is the application order; two
   dopings with the same priority are rejected by the Step 06 lattice validator.
5. `extensions` by `Id` ordinal. The `config` object is canonicalized recursively.

### Seeds

The build resolves one master seed text as the first non-empty of the collection `rngSeed`, the simulation
`rngSeed`, and the project `masterSeed` (always required). A 64-character lowercase hexadecimal text is decoded
to 32 bytes; every other text is hashed as
`SHA-256("mocassin/seed-text/v1" || UTF-8(text))`. Each job seed is

```text
SHA-256(master_seed[32] ||
        u32_le(collection_index) ||
        u32_le(config_index) ||
        u32_le(job_index) ||
        UTF8("mocassin/job/v1"))
```

with zero-based indices and little-endian `u32` fields. The build never creates or persists a seed. Changing a
runtime setting alone does not change the seed or the lattice request.

### Limits

| Limit | Value | Diagnostic |
| --- | --- | ---: |
| Templates per project | 100 | `JOB020` |
| Collections per template | 100 | `JOB020` |
| Configurations per collection | 1,000 | `JOB020` |
| Dopings per configuration | 1,000 | `JOB020` |
| Optimizers per collection | 1,000 | `JOB020` |
| Effective jobs per configuration | 1–1,000 | `JOB008` |
| Total expanded jobs per template | 99,999 | `JOB019` |
| Total-job warning threshold | 10,000 | `JOB013` |
| Cell counts per axis | 1–256 | `JOB004` |
| Total positions per supercell | 500,000 | `JOB004` |
| Position warning threshold | 100,000 | `JOB014` |

`validate` reports the structural rules without a compiled model: `JOB001`–`JOB008`, the optimizer reference
half of `JOB009`, `JOB015`, `JOB017`, `JOB018`, and `JOB020`. The build stage reports the compiled-bound
total-position, doping-count, compensation, neutrality, removes-all, and expanded-job checks. The complete list
is in [Diagnostics](diagnostics.md).

The checked-in populated fixture is `tests/contracts/project-job-templates-populated.mocprj`; the buildable sample is
`samples/jobs/ceria-temperature-sweep.mocprj`. See the [job-template guide](../user-guide/job-templates.md) for
authoring and previewing.

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

### Parameterization template values (Step 09)

Step 09 keeps the Step 01 identity fields (`id`, `name`, `modelSignature`, `createdUtc`) and adds five optional
arrays. The arrays are written by the current writer and may be absent in a hand-written or older document; a
missing array loads as empty.

| Field | Contents |
| --- | --- |
| `stablePairSets` | Shared stable-site pair-energy tables keyed by `energyId`. |
| `unstablePairSets` | Shared unstable-site pair-energy tables keyed by `energyId`. |
| `groupSets` | Cluster-energy tables keyed by `clusterEnergyId` plus the canonical `occupationParticleIds` tuple. |
| `siteEnergyOverrides` | Site-energy overrides keyed by `(siteId, particleId)`. |
| `transitionRuleSets` | Kinetic rule frequencies keyed by `(transitionKind, transitionId)` and `ruleGeometryId`. |

- Values are sparse: a missing pair or group value means `0 eV`, a missing rule frequency means the compiled
  default, and a missing site override means the authored model background. An explicit `0` or default frequency
  is a real entry.
- Pair and cluster identities are 64-character lowercase hexadecimal content hashes. A chiral pair table stores
  one shared value per `energyId`; independent A→B and B→A values are not representable.
- The writer sorts sets and entries by their stable keys, formats numbers invariantly, and normalizes `-0.0` to
  `0.0`. Record-level edits cannot bypass this canonicalization.
- Structural limits and malformed keys are reported by the `PRM` diagnostics; see
  [Diagnostics](diagnostics.md).

#### Stable identities

| Value | Identity | Source |
| --- | --- | --- |
| Pair energy | `energyId` | The shared compiled pair identity; the lexicographically smaller directed geometry for a reciprocal chiral pair. |
| Group energy | `clusterEnergyId` + canonical `occupationParticleIds` tuple | The compiled cluster identity and the canonical occupation in site order. |
| Rule frequency | `ruleGeometryId` inside `(transitionKind, transitionId)` | The compiled rule geometry identity. |
| Site-energy override | `(siteId, particleId)` | The authored reference site and particle. |

Dense compiled indices are never template identities. An unknown or malformed key is `PRM014`; a key that no
longer exists in the current model is `PRM002` and is ignored. Applying a template whose `modelSignature`
differs from the compiled model is `PRM001`; rebase is an explicit operation.

#### Canonical ordering

1. `stablePairSets`, `unstablePairSets` by `energyId` ordinal; entries by
   `(centerParticleId, partnerParticleId)`.
2. `groupSets` by `clusterEnergyId` ordinal; entries by the `occupationParticleIds` GUID sequence in site order.
3. `siteEnergyOverrides` by `(siteId, particleId)`.
4. `transitionRuleSets` by `(transitionKind, transitionId)`; `rules` by `ruleGeometryId` ordinal.

The canonical document orders `parameterizations` by `id`. Two templates that differ only beyond the 12th
decimal have different document hashes but the same applied values and parameterization signature: `apply`
quantizes to 12 decimals with round-half-away-from-zero before storing and signing.

#### Limits

| Limit | Value | Diagnostic |
| --- | ---: | --- |
| Templates per project | 256 | `PRM013` |
| Pair sets per template | 100,000 | `PRM013` |
| Pair entries per set | 4,096 table cells (64 compiled particles including void) | `PRM013` |
| Group sets per template | 100,000 | `PRM013` |
| Group entries per set | 1,000,000 | `PRM013` |
| Site overrides per template | 1,000,000 | `PRM013` |
| Transition rule sets per template | 100,000 | `PRM013` |
| Rule entries per set | 1,000,000 | `PRM013` |
| Explicit value entries per template | 2,000,000 | `PRM013` |
| Stable ID length | 64 lowercase hex characters | `PRM014` |
| Template name length | 200 characters | `PRM013` |
| Applied value bytes per apply | 2 GiB | `PRM013` |

Counts are checked with 64-bit arithmetic before allocation. The enclosing project document is also bounded by
`ProjectStoreOptions.MaxDocumentBytes` (64 MiB). Template content is data only: it never selects a file,
command, glob, or SQL statement.

#### Versioning

There is no template-level format version in v1. The template shape is part of the project `schemaVersion`;
Step 09 extends the pre-release Step 01 stub additively, so `schemaVersion` stays `1`. After the first released
`.mocprj`, any semantic template change bumps `schemaVersion` and adds a pure migration in
`ProjectMigrations`. Unknown properties are rejected by the project reader; never reinterpret an existing field.

The checked-in golden templates are `samples/parameterizations/ceria-parameters.mocprj` and
`samples/parameterizations/barium-zirconate-parameters.mocprj`. Their applied values and signatures are pinned
by `tests/contracts/parameterizations/` and `tests/contracts/projections/parameterization/`.

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
- invariant shortest round-trip numbers, except inside opaque extension-config objects, where the authored
  JSON number token is preserved verbatim (`1` and `1.0` differ, integers above 2^53 and values outside
  binary64 stay exact);
- `-0.0` normalized to `0.0` for every non-extension number;
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
`MarkAutosaved`; the UI timer belongs to Step 23B.

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
