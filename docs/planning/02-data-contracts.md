# 02 — Data Contracts

This document defines the boundaries between the .NET model builder, the Avalonia application, the Rust
solver, and external tooling. Contracts are versioned and covered by golden and cross-language tests.

> **Clean-break policy.** These are new formats. There is no reader or writer for the original `.mocprj`,
> `.msl`, or `.mcs` files. The original formats may be studied for semantics only.

## 1. Identifiers and ordering

- Every user-created object has a stable `id` string. New objects use UUIDv7; imported or hand-written
  fixtures may use any RFC 4122 UUID.
- References are by `id`, never by array position.
- Arrays are ordered only where order is semantic:
  - state-change IDs inside a group,
  - state-change group order inside a chain,
  - chain linkers,
  - cluster offset vectors,
  - doping priority,
  - reference path positions,
  - particle IDs in a particle set,
  - job collections/configurations.
- All other arrays of identifiable objects are sorted by `id` when serialized, so identical models produce
  byte-identical JSON. A section may define a more specific canonical key; for example, energy filters and
  site-energy entries have no `id` and use the keys stated in §2.2 Energy.
- Compiled objects receive dense integer indices (`ParticleIndex`, `PositionIndex`, `JumpCollectionIndex`,
  `RuleIndex`) during compilation. These indices are **not** stable across model edits; they are valid only
  inside one compiled package and are recorded with the model signature.

### 1.1 Contract manifest and generated checks

The field/type/version descriptions in this document are backed by a machine-readable manifest:

```text
contracts/manifest.json
contracts/generated/…
tools/contracts/…
```

- The manifest is the single source for field names, types, required/optional flags, format versions, and
  section identifiers across project JSON, `.msl`, `.msr`, `.mcs`, `solver.json`, job folders, JSONL events,
  and `mmcfe.db`.
- `tools/contracts` validates the manifest and generates:
  - JSON Schema fragments used by documentation and validation tests;
  - canonical signature-projection layouts (ADR 0010);
  - language-neutral fixture descriptors and expected canonical JSON summaries;
  - version/enum/event constants for the C#, Rust, and Python implementations.
- C#, Rust, and Python DTOs may remain hand-written; generated checks detect drift between them and the
  manifest.
- CI fails if generated files are stale or if a DTO/validator disagrees with the manifest.
- A manifest change requires a version bump and updated fixtures, exactly like a schema change.

### 1.2 Hash policy

Use one hash algorithm across the plan: **SHA-256**.

| Use | Algorithm | Why |
| --- | --- | --- |
| Project/compiled/parameterized model signatures | **SHA-256** | Stable 256-bit content identity; BCL-native in .NET and ubiquitous in Rust/Python |
| Job/lattice seed derivation and lattice request hashes | **SHA-256** | Deterministic domain-separated derivation; inputs are small, so no specialized hash is needed |
| `.mcs` result blob integrity (`run_state_sha256` in `.msr`) | **SHA-256** | Same algorithm; standard verification tooling |
| Release artifact checksums / SBOM digests | **SHA-256** | Ubiquitous verification tooling |

- SHA-256 is **not** used as a security boundary for simulation seeds; it provides deterministic, well-distributed
  derivation from a master seed.
- Never use `string.GetHashCode()` for persisted identity, seeds, or compatibility checks.
- Do not introduce a second hash algorithm without an ADR and a format version bump.

### 1.3 Canonical signature projection

Signatures are cryptographic identities of logical content, not of the stored MessagePack payload. The
canonical projection is defined by ADR 0010 and `contracts/manifest.json`:

- **Structs become MessagePack arrays** in manifest field order. Dynamic maps use sorted UTF-8 keys.
- **Integers are fixed-width:** `uint64` for non-negative values, `int64` for signed values.
- **Floats are `float64`.** Normalize `-0.0` to `+0.0`; reject NaN/Inf in required numeric fields.
- **Strings are UTF-8 bytes as stored.** Byte arrays use `bin`.
- **Every field is present.** Absent optional values are `nil`; defaults are not omitted.
- **No compression, timestamps, runner metadata, file names, or SQLite layout** is included.
- **Domain separation** is applied before hashing:
  - `SHA-256("mocassin/model-signature/v1" || 0x00 || projection)`
  - `SHA-256("mocassin/parameterization-signature/v1" || 0x00 || projection)`
  - `SHA-256("mocassin/lattice-signature/v1" || 0x00 || projection)`

The stored `.msl` payload may remain a compressed MessagePack map. Readers decode it, re-encode the canonical
projection, and compare the digest. The manifest is the source for projection field order and types; a change
to either is a format-breaking change.

Step 08's compiled-model signature covers geometry, rules, encoding, indexing, and the symmetry/catalog
versions; it excludes parameterization values and runtime simulation settings. Step 09's parameterization
signature covers the compiled-model signature plus the canonical applied values. Its `ParameterizationSignatureV1`
projection is a fixed six-field MessagePack array:

```text
[formatVersion(uint64), modelSignature(string),
 pairTables([[float64 × nParticles²] × pairTableCount]),
 groupSets([[float64 × occupationCount] × clusterTableCount]),
 siteEnergies([float64 × positionCount × particleCount]),
 transitionRules([float64 × ruleCount])]
```

- Values are in compiled table/rule order and are the **quantized applied** values, not the sparse template.
  `modelSignature` binds the order and identity; no stable IDs are projected, so a reader can reproduce the
  digest from the `.msl` payload alone.
- `siteEnergies` is the dense `[position][particle]` background with void at index 0 of each row.
- Excluded: template ID/name/timestamps, set headers, display descriptors, unset markers, diagnostics,
  summaries, timings, file names, and every runtime job/simulation setting.
- Every float is quantized to 12 decimals (round half away from zero) and `-0.0` normalizes to `+0.0`.
- `contracts/manifest.json` `signatures.parameterization` is finalized by Step 09 (`status: final`, typed
  fields, `projectionType: ParameterizationSignatureV1`); the generated descriptor and
  `tests/contracts/projections/parameterization/*.bin` vectors (a subdirectory, so the model checker's
  `projections/*.bin` glob does not consume them) are checked by `verify.sh contracts`. ADR 0010's
  pre-release signature-layout exception authorizes this finalization: no released `.msl` exists, and the
  entry is closed with golden vectors and the Python checker in Step 09 feature 2.

The lattice signature is a five-field canonical projection, finalized in Step 11 under the same pre-release
exception:

```text
[formatVersion(uint64), modelSignature(string), latticeSize([uint64 × 3]),
 positionCount(uint64), particles(bin)]
```

- `modelSignature` binds the compiled particle-index space; `particles` is the row-major initial lattice byte
  array. The doping summary and build metadata are excluded, so identical lattices deduplicate across jobs.
- A reader re-encodes the projection from the lattice payload plus the referenced model's `model_signature`.

The signatures are deliberately distinct; see §3.3.

## 2. Project file (`.mocprj`, JSON)

### 2.1 Envelope

```json
{
  "$schema": "https://mocassin.dev/schemas/project-v1.json",
  "schemaVersion": 1,
  "projectId": "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e6f",
  "masterSeed": "9f2c…64-hex-chars…",
  "name": "Ceria",
  "createdUtc": "2026-11-02T12:00:00Z",
  "modifiedUtc": "2026-11-02T12:30:00Z",
  "symmetry": {
    "spaceGroupDataVersion": "spglib-2.8.0/catalog-1",
    "spaceGroupDataSha256": "…",
    "symmetryContractVersion": "symmetry-1"
  },
  "model": { "...": "section 2.2" },
  "parameterizations": [],
  "jobTemplates": [],
  "buildTemplates": [],
  "viewSettings": {}
}
```

- UTF-8, no BOM, 2-space indentation, `\n` line endings.
- `masterSeed` is a 32-byte hex value generated at project creation; it seeds job/lattice derivation and is
  part of the project metadata.
- `symmetry` is optional advisory metadata written by the application: the embedded catalog data version,
  catalog checksum, and symmetry contract version. Missing metadata is accepted. If present and any value
  differs from the embedded values, the composition-layer loader emits `PROJ034` warning and does not rewrite
  the project; the
  compiled payload records the authoritative build versions. Parsing stays in `Mocassin.Core`;
  `Mocassin.Packages.Projects.ProjectValidationService` performs the catalog comparison and appends
  `PROJ034`, because Core must not reference `Mocassin.Symmetry`.
- `symmetry` is an additive optional v1 field; `schemaVersion` remains `1` while the model shape is pre-release.
  Step 03 adds fixtures with and without the field. A present-but-partial metadata object compares only the
  fields it contains; an absent object is accepted without warning.
- `Mocassin.Core` represents the object as `ProjectSymmetryMetadata?` on `Project` and
  `ProjectDto.Symmetry`; it is included in the canonical document hash but not the model hash.
  `ProjectStore.LoadBytesCore` uses `Mocassin.Packages.Projects.ProjectValidationService` by default so the
  loaded diagnostics include Core, Symmetry, and `PROJ034` results.
- `schemaVersion` is an integer. Readers accept `1..current`; migrations are pure functions in
  `Mocassin.Core.Project.ProjectMigrations`.
- Unknown properties are rejected by default (`JsonUnmappedMemberHandling.Disallow`) to catch typos; schema
  evolution adds properties explicitly and bumps the version when semantics change.
- The file is written atomically (temp file + rename).

**Canonical project hash.** `ProjectHasher` does not hash the pretty-printed file. It hashes a canonical JSON
projection:

- UTF-8 without BOM, no insignificant whitespace, `\n` only where a separator is required;
- object keys in schema order (manifest order), not hash-map order;
- non-semantic arrays sorted by `id`;
- numbers in invariant, shortest round-trip form; no NaN/Inf; normalize `-0.0` to `0.0`, except inside
  opaque extension-config objects, where the authored JSON number token is preserved verbatim (see §2.4);
  the exception keeps integers above 2^53 and values outside binary64 exact and makes `1` and `1.0` distinct
  tokens, while every other project number keeps the shortest round-trip rule;
- timestamps normalized to UTC `Z` with fixed fractional digits;
- unknown fields excluded (they are rejected by the reader anyway);
- `symmetry` is envelope metadata: it is included in the document hash, not the model hash.

The hash is used for dirty tracking and cache keys, not as a cross-language model signature. Model,
parameterization, and lattice signatures use the MessagePack projection (section 1.3 and ADR 0010).

### 2.2 Model

```json
{
  "particles": [],
  "particleSets": [],
  "structure": {},
  "stateChanges": [],
  "stateChangeGroups": [],
  "chains": [],
  "kineticTransitions": [],
  "metropolisTransitions": []
}
```

- Steps 03–06 add the sections. A missing `particles`, `particleSets`, or `structure` property loads as the
  empty/default model so that a Step 01 `"model": {}` document remains valid without a schema-version bump.
  The transition sections are flat at the model root, matching the Step 03 model envelope; there is no nested
  `transitions` object. Energy, lattice, and simulation sections are likewise added flat by their steps.
  The default structure is P1, `a=b=c=1 Å`, all angles 90°, name `New Structure`, with no sites or dummies.
- The persisted arrays `particles`, `particleSets`, `structure.sites`, and `structure.dummyPositions` are
  non-semantic and sort by `id` in canonical JSON. `particleIds` inside a particle set is semantic and keeps
  its authored order.
- `ModelDefinition` in code is flat: it owns `particles`, `particleSets`, and `structure` directly. An
  in-memory `ParticleModel` grouping must not nest these arrays in the JSON document.

#### Particles

```json
{
  "id": "…",
  "name": "O2-",
  "symbol": "O",
  "charge": -2.0,
  "isVacancy": false
}
```

- `symbol` groups species for mass-conservation rules. Species with the same symbol may exchange without
  violating mass conservation. The recommended pattern is `^[A-Z][A-Za-z0-9+\-()]{0,10}$`; a mismatch emits
  `PAR008` warning, not an error.
- `charge` is in units of the elementary charge and is restricted to the finite range −1000..1000 by
  `ChargeElementary`. An out-of-range value is a parse/construction error (`PROJ002`), not a separate
  particle diagnostic.
- `isVacancy` marks a pseudo-particle that represents an empty state. Vacancies have `charge = 0` or their
  default occupation does; the validator warns (`PAR004`) when both are non-zero.
- The implicit void particle is not serialized; it uses the reserved `ParticleId.Void`. Authored particles
  must not reuse that ID. `isVoid` is computed and is never a JSON field.

#### Particle sets

```json
{
  "id": "…",
  "name": "Oxygen site",
  "particleIds": ["…", "…"]
}
```

- The first particle is the default occupation of a stable site. Unstable sites always default to void.
- The void particle is implicit and must not be listed. The empty particle set uses the reserved
  `ParticleSetId.Empty`; authored sets must not reuse that ID. Reusing `ParticleId.Void` or
  `ParticleSetId.Empty` is `PAR010` error.
- Set names are unique (case-insensitive); empty or duplicate names are `PAR011` error. A repeated particle ID
  inside one set is `PAR012` error.
- Two authored sets with the same ordered `particleIds` are allowed but emit `PAR009` warning; they are not
  merged automatically.

#### Structure

```json
{
  "name": "Ceria",
  "spaceGroup": { "number": 225, "axis": "none", "origin": "none" },
  "cell": { "a": 5.41, "b": 5.41, "c": 5.41, "alpha": 90.0, "beta": 90.0, "gamma": 90.0 },
  "sites": [
    {
      "id": "…",
      "name": "Ce",
      "fractional": [0.0, 0.0, 0.0],
      "particleSetId": "…",
      "stability": "stable"
    }
  ]
}
```

- `name` is the user-visible structure name and is required; it defaults to `New Structure` for a new project
  but an empty or whitespace-only persisted value is `STR013` error.
- Lengths are in Å and must be in `0.1..1000`; angles are in degrees and must satisfy
  `0 < α, β, γ < 180°` and a positive Gram determinant.
- Crystal-system constraints apply: equal lengths/angles, 90°/120° requirements, unique-axis mapping, and
  rhombohedral `0 < α < 120°`. Inconsistent values are rejected; they are never silently normalized.
- The cell-matrix condition number `||M||∞ · ||M⁻¹||∞` must not exceed `1e12`; otherwise `STR003`.
- A structure may define at most 1000 reference sites; more is `STR012` error.
- `fractional` components must be inside `[0, 1)` before trimming. Values within `FractionalTrimTolerance`
  (`1e-10`) of a boundary snap to the equivalent canonical position; values outside `[0,1)` beyond that
  tolerance are `STR004` error and are not wrapped. The canonical P1 order uses the exact `24 × 10^12`
  fractional grid defined in Step 02; the stored decimal remains the user-facing value.
- Dummy positions use the same coordinate rule and are `STR010` error when invalid; dummy names are unique
  (case-insensitive) and empty/duplicate names are `STR015` error.
- More than 10,000 total P1 positions emits `STR009` warning, not an error. A requested supercell product
  `A*B*C*P` above the signed 32-bit linear-index cap is `STR014` error. This deliberately relaxes the original
  10,000 hard limit.
- `number` is the International Tables space-group number, 1..230.
- `axis` identifies the unique axis or the hexagonal/rhombohedral description; `origin` identifies the origin
  choice. Both are explicit and use `none` when the setting has no such choice.
- `axis` values are `none`, `uniqueAxisB`, `uniqueAxisC`, `hexagonal`, and `rhombohedral`. `uniqueAxisA` is
  reserved: the JSON parser accepts the enum value, and the catalog validator rejects it with `STR001` in v1.
  Step 03 marks it reserved and unsupported in the manifest and generated schema. `origin` values are `none`,
  `originChoice1`, and `originChoice2`.
- `fractional` is a three-number JSON array `[a, b, c]`, not an object. `axis`, `origin`, and `stability` are
  camelCase JSON strings.
- The catalog resolves `(number, axis, origin)` to the Hermann–Mauguin symbol, Hall symbol, crystal system, and
  operations. These derived values are never persisted in the project.
- The default structure is P1: `{ "number": 1, "axis": "none", "origin": "none" }`.
- `stability` is `stable` or `unstable`. Stable sites may define interactions and host particles; unstable
  sites are transition states.

#### Transitions

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
      "referencePath": [[0.125, 0.125, 0.125], [0.375, 0.125, 0.125], [0.625, 0.125, 0.125]],
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

- `pattern` is one of `metropolis2`, `migration3`, `interstitialcy5`, `interstitialcy7`, `vehicle5`,
  `vehicle6`, or `custom`. The pattern fixes the number of positions and the allowed linker sequence:
  - `metropolis2`: 2 positions, `dynamic`, reverse generated.
  - `migration3`: 3 positions, `dynamic, dynamic`, reverse generated.
  - `interstitialcy5`: 5 positions, all `dynamic`, no reverse.
  - `interstitialcy7`: 7 positions, all `dynamic`, no reverse.
  - `vehicle5`: 5 positions, `dynamic, static, static, dynamic`, no reverse.
  - `vehicle6`: 6 positions, `dynamic, dynamic, static, dynamic, dynamic`, no reverse.
  - `custom`: 2–8 positions with an explicit, validated `D/S` linker sequence; a 2-position custom chain is a
    Metropolis chain, and a 3–8-position custom chain is kinetic. Kinetic custom chains accept `D{2,7}` or the
    vehicle-shaped `D+ S+ D+`; the single-static `D S D` sequence is invalid. Automatic reverse generation is
    false; define a `reverseTransitionId` instead.
- `stateChangeIds`, `groupIds`, `linkers`, and `referencePath` are semantic-order arrays and are never
  re-sorted.
- A state-change group must be homogeneous: it must not mix physical exchanges (different particle symbols)
  with charge-state exchanges (same symbol, different charge). Charge-transfer mechanisms use homogeneous
  charge-state groups.
- `referencePath` positions use the unit-cell fractional coordinate system and may lie outside `[0, 1)`.
  Persisted values are not wrapped. Site matching expands each reference site to its symmetry-equivalent P1
  orbit and uses the named `PathMatchTolerance`. The cell offset is the minimizing integer shift between the
  raw point and the matched P1 position (ties toward zero); a point must match exactly one `(P, offset)` pair
  and owning reference site.
- A `referencePath` contains a ring when any two resolved positions have the same full encoded position
  (`P` plus cell offset). A point in a different periodic cell has a different offset and is a distinct
  position, not a duplicate.
- The coordinates in the JSON example above are illustrative. A concrete path point must match a
  symmetry-equivalent P1 position of the referenced structure; otherwise it is `TRN020`.
- `generatesReverse` must be `true` for `metropolis2` and `migration3` and `false` for every other pattern.
- `reverseTransitionId` is an optional same-kind link to the explicitly defined reverse transition. It is
  symmetric and may not be self-referential. Complex mechanisms use it instead of automatic inverse rules.
- MMC transitions bind two reference sites; selecting the same site twice means an exchange within one
  sublattice. Both sites must exist and be stable.
- In memory, simulations reference either kind through `TransitionRef`. The persisted simulation shape remains
  the kind-specific `kineticTransitionIds` and `metropolisTransitionIds` arrays.
- Transition collections are sorted by `id` when serialized; the model sections are additive and the project
  `schemaVersion` remains 1.

#### Energy

```json
{
  "stableEnvironment": {
    "cutoff": 6.0,
    "filters": [
      { "start": 0.0, "end": 3.0, "siteAId": "…", "siteBId": "…" }
    ]
  },
  "unstableEnvironments": [
    {
      "siteId": "…",
      "cutoff": 4.0,
      "filters": []
    }
  ],
  "clusters": [
    {
      "id": "…",
      "name": "Migration edge",
      "centerSiteId": "…",
      "offsetVectors": [[0.25, 0.25, 0.0], [0.25, 0.0, 0.25]]
    }
  ],
  "siteEnergies": [
    {
      "siteId": "…",
      "entries": [{ "particleId": "…", "energyEv": 0.48 }]
    }
  ]
}
```

- `cutoff` and filter radii are in Å. Every environment cutoff (stable and unstable) must be positive and
  finite; a cutoff above half the smallest cell dimension warns about periodic ambiguity.
- A missing `stableEnvironment` loads the default cutoff `1.0 Å` with no filters. The default environment is
  omitted on write when it equals the default. The empty `unstableEnvironments`, `clusters`, and
  `siteEnergies` arrays are omitted when empty, so a model without authored energy data keeps its previous
  canonical JSON and hashes.
- Filters remove pair interactions whose centre-to-partner distance is in the inclusive range
  `[start, end]`, with the 1e-5 Å distance-equality tolerance. A stable-environment filter is symmetric:
  `(siteAId, siteBId)` removes both directions. An unstable-environment filter is one-directional:
  `siteAId` must equal the environment's `siteId`, and `siteBId` is the partner. Filter order is not semantic;
  filters serialize by `(start, end, siteAId, siteBId)`.
- Cluster offsets are absolute fractional vectors relative to the cluster centre, in definition order.
  `offsetVectors` contains the surrounding positions only: 2 to 8 offsets (3 to 9 positions including the
  centre). The `offsetVectors` array is semantic and is never re-sorted. Periodic identity uses the Step 02
  `UnitCellVectorEncoder`; duplicate or centre-equivalent encoded positions are rejected.
- Site energies are in eV; missing entries default to 0. Energy entries are sorted by `particleId` when
  serialized. A `(siteId, particleId)` pair may appear at most once.
- Unstable environments are sorted by `siteId`, clusters by `id`, and site-energy sets by `siteId`. Each
  environment's `filters` array and each site-energy set's `entries` array use their stated sort keys.
- `ProjectJson.CanonicalizeModel` applies these rules before save and hashing. The generated project schema is
  produced separately from `contracts/manifest.json` by `tools/contracts/generate.py`.
  `ProjectHasher.Compute` and `ComputeDocument` therefore include the energy sections, and
  `ProjectHasher.ComputeEnergy(ModelDefinition)` hashes only the canonical energy projection for Step 08's
  energy-model cache key.
- The energy sections are additive and the project `schemaVersion` remains 1 under the Step 03/04 additive-v1
  precedent. ADR 0009's version-bump rule is not waived generally: the manifest `status`/`notes` and fixtures
  are updated now, and the first released format starts the normal bump discipline.

#### Lattice and doping

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

- Occupation exchanges are sorted by `id` when serialized. Occupation-exchange and doping names are unique
  case-insensitively within their collection.
- Dopings are serialized in ascending `priority`; priorities are unique and in `0..9999`. The order is the
  deterministic application order.
- Each doping fraction applies to the particles remaining after higher-priority dopings. The fractions and the
  supercell are job-template data (Step 10), not model fields.
- `dependentExchangeId` must be `null` when `chargeCompensation` is `false`. When compensation is `true`, the
  dependent exchange is required, both charge deltas must be non-zero, and they must have opposite signs. The
  integer ratio and any downward truncation are Step 10 checks.
- The default building block is implied: first particle of each stable site's set, void on unstable sites. It is
  derived by `ILatticeQuery.DefaultBuildingBlock` and is never persisted.
- Model-level validation covers references, names, priorities, and charge-compensation shape (`LAT001`–`LAT011`).
  Job-template structure, fractions, supercell/job limits, same-site application, truncation,
  charge-compensation counts, extensions, and the lattice preview are Step 10 (`JOB001`–`JOB020`). Structural
  `JOB` codes run in `validate`; compiled-bound codes run in the build stage.
- Simulations are stored under `model.simulations`, sorted by `id`. The kind-specific transition ID arrays
  preserve authored order, as required by Step 04; they are never re-sorted. `executionFlags` is a sorted array
  of camelCase flag names.
- Empty `occupationExchanges`, `dopings`, and `simulations` arrays are omitted on write, matching the optional
  transition/energy sections. Missing arrays load as empty, and the canonical model treats default arrays as
  empty, so projects without lattice/simulation data keep their pre-Step-06 model and document hashes.
- The lattice and simulation sections are additive and the project `schemaVersion` remains 1 under the Step
  03–05 pre-release precedent. The manifest `status`/`notes`, generated schema, descriptors, and fixtures are
  updated now; ADR 0009's version-bump rule resumes with the first released format.

#### Simulations

The persisted simulation is a flat object. `kind` selects the concrete kind; only the matching transition array
and kind-specific settings are present. The other kind's fields are absent, not `null`. The JSON schema uses a
flat object with optional kind-specific fields because the contract generator has no `oneOf`/discriminator
support; the validator rejects a field that belongs to the other kind (`SIM023`).

```json
[
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
      "preRunMcsp": 100,
      "electricFieldModulusVm": 1.0e7,
      "electricFieldDirection": [1.0, 0.0, 0.0],
      "normalizationEnergyEv": null,
      "maxAttemptFrequencyHz": null
    }
  },
  {
    "id": "…",
    "name": "O exchange 1000 K",
    "kind": "mmc",
    "metropolisTransitionIds": ["…"],
    "settings": {
      "temperatureK": 1000.0,
      "targetMcsp": 1000,
      "writeOutCount": 100,
      "timeLimitSeconds": 31536000,
      "minSuccessRate": null,
      "jobCount": 12,
      "rngSeed": null,
      "executionFlags": [],
      "instruction": null,
      "breakTolerance": 0.0,
      "breakSampleLengthMcsp": 1000,
      "breakSampleIntervalMcsp": 100,
      "resultSampleMcsp": 200
    }
  }
]
```

- `kind` is `kmc` or `mmc`; it must agree with the transition array and the kind-specific settings.
- `kineticTransitionIds` and `metropolisTransitionIds` preserve authored order and may not contain duplicate
  `(kind, id)` references.
- `writeOutCount` is the number of intermediate data-out calls (0 = final state only).
- `timeLimitSeconds` is the wall-clock limit; `null` means unlimited. The default is 31,536,000 s (P1Y).
- `minSuccessRate` is in MCS/s; `null` or 0 disables the abort, and 0 canonicalizes to `null`. It is not a
  fraction.
- `rngSeed` is optional; `null` or empty derives the seed from the project/job seed (ADR 0005), and empty
  canonicalizes to `null`. The original GUID fallback is not reproduced.
- `executionFlags` is a sorted array of camelCase names: `noSaving`, `noJumpLogging`, `useFastExp`,
  `deterministic`. The empty array means `None`; the `.msl` job payload uses the `u32` bitmask.
  `deterministic` selects the canonical RNG-consumption path by disabling the Step 16 frequency pre-check and
  optional shortcuts; baseline reproducibility is already guaranteed by ADR 0005.
  `kmcSimulation`/`mmcSimulation`, `usePrerun`, and
  `useDualDofCorrection` are derived or compiler/optimizer-owned and must not appear in the project
  `executionFlags` array. The `.msl` flag word uses the new bit values: `noSaving`=1, `noJumpLogging`=2,
  `useFastExp`=4, `deterministic`=8, `useDualDofCorrection`=16, `usePrerun`=32, `kmcSimulation`=64,
  `mmcSimulation`=128. These are a clean-break contract and do not reuse the original flag bit positions.
- KMC `preRunMcsp` 0 disables the pre-run (the original −1 sentinel is not persisted).
  `normalizationEnergyEv` `null` means no normalization (the original default 0); `0.0` is accepted and
  canonicalizes to `null`, and a positive value enables fixed normalization. `maxAttemptFrequencyHz` `null`
  means the maximum from the transition rules.
- MMC `breakTolerance` 0 disables the equilibrium break check; sample lengths/intervals and the result sample
  are MCSP counts.
- `instruction` is an optional advanced/legacy free-text extension instruction; structured `extensions` in job
  configurations are preferred.
- Lattice, job-count, and effective-count previews are Step 10 derived data and are never persisted in the
  project.

### 2.3 Parameterization templates

A parameterization template is stored inside the project `parameterizations` array. There is no separate
template document or sidecar in v1. Templates are identified by a UUIDv7 `ParameterizationId`; a
`BuildTemplate.parameterizationId` or an explicit build selection references one by ID. Multiple templates may
target the same compiled model.

```json
{
  "id": "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e6f",
  "name": "Ceria parameters",
  "modelSignature": "812a…",
  "createdUtc": "2026-11-14T12:00:00Z",
  "stablePairSets": [
    {
      "energyId": "…",
      "centerSiteId": "…",
      "partnerSiteId": "…",
      "distanceBucketFm": 248,
      "isChiral": false,
      "entries": [
        { "centerParticleId": "…", "partnerParticleId": "…", "energyEv": 0.0 }
      ]
    }
  ],
  "unstablePairSets": [],
  "groupSets": [
    {
      "clusterEnergyId": "…",
      "clusterId": "…",
      "centerSiteId": "…",
      "entries": [
        { "occupationParticleIds": ["…", "…"], "energyEv": 0.48 }
      ]
    }
  ],
  "siteEnergyOverrides": [
    { "siteId": "…", "particleId": "…", "energyEv": 0.0 }
  ],
  "transitionRuleSets": [
    {
      "transitionKind": "kinetic",
      "transitionId": "…",
      "rules": [
        { "ruleGeometryId": "…", "frequencyHz": 1.0e13 }
      ]
    }
  ]
}
```

- `modelSignature` is the Step 08 compiled-model signature: a SHA-256 hash of the canonical compiled geometry
  (structure, transitions, environments, interaction geometry, indices), excluding parameterization values and
  runtime simulation settings. It is distinct from the Step 09 parameterization signature stored in the `.msl`
  `models.signature`. A parameterization whose model signature does not match the current compiled model
  cannot be used to build jobs; `Apply` rejects it with `PRM001`, and the GUI offers "rebase" and shows which
  sets changed.
- **Sparse values.** Absence means "not parameterized": a missing pair/group value defaults to `0 eV`, a
  missing rule frequency defaults to the compiled `1e13 Hz` default, and a missing site-energy override means
  the authored model background. An explicit `0 eV` or explicit `1e13 Hz` is a real entry. Creation writes one
  set header for every compiled pair table, cluster table, and kinetic transition collection but no value
  entries; `ParameterizationService.Describe` enumerates the individual required slots.
- **Pair identity** is `energyId`, the Step 05/08 shared pair energy identity. It equals `GeometryId` for an
  achiral or unstable–stable pair and is the lexicographically smaller `GeometryId` for a reciprocal chiral
  pair. Each direction keeps its own `GeometryId` for display, but a template stores one value per
  `EnergyId`; independent A→B/B→A values are unrepresentable. `centerSiteId`, `partnerSiteId`,
  `distanceBucketFm`, and `isChiral` are display/rebase descriptors, not identity; for a chiral table they
  come from the direction whose `GeometryId` equals the shared `EnergyId`. A descriptor mismatch for the same
  ID is `PRM002` and the compiled model wins. A chiral table is symmetric under particle exchange: setting
  `(X, Y, E)` also sets `(Y, X, E)`, the template stores the smaller particle-ID orientation, and a mirrored
  duplicate with a different value is `PRM014`. Achiral and unstable–stable tables are ordered.
- **Group identity** is `clusterEnergyId`, the Step 08 stable cluster energy ID, plus the canonical
  `occupationParticleIds` tuple in site order (center first). The tuple uses authored particle IDs, not
  compiled indices. `clusterId` and `centerSiteId` are display descriptors.
- **Rule identity** is `ruleGeometryId`, the Step 08 stable rule geometry ID, inside a
  `(transitionKind, transitionId)` set. Only kinetic transitions are parameterizable; MMC rules keep their
  compiled default and are not written into a template. Both kind and ID are stored so a hand-written or stale
  Metropolis set is unambiguous and is warned/ignored with `PRM011`, and because kinetic and Metropolis
  transitions are separate domains. The dense `.msl` `rule_id` is never a template identity.
- **Site overrides** are keyed by `(siteId, particleId)`. Site energies are per reference site, not per P1
  position. `siteEnergyOverrides` is an intentional improvement over the original, which could not edit site
  energies through a template. A missing override uses the authored `model.siteEnergies` background; removing
  an override is "reset to model".
- **Versioning.** The template shape is part of the project `schemaVersion`; it has no separate
  `formatVersion`. Step 09 extends the Step 01 template stub additively before the first released `.mocprj`,
  so `schemaVersion` stays `1` and no migration is needed. After release, a semantic template change requires
  a `schemaVersion` bump and a migration in `Core.Project.ProjectMigrations`. Never reinterpret an existing
  field.
- **Canonical order.** `parameterizations` sorts by `id`. Within a template: pair sets sort by `energyId`,
  pair entries by `(centerParticleId, partnerParticleId)`, group sets by `clusterEnergyId`, group entries by
  the `occupationParticleIds` sequence in site order, site overrides by `(siteId, particleId)`, rule sets by
  `(transitionKind, transitionId)`, and rule entries by `ruleGeometryId`. The canonicalizer applies this at
  write time (record construction may also normalize for convenience); `with`-based edits cannot bypass it.
  Values format invariantly and `-0.0` normalizes to `0.0`.
- **Limits.** `ParameterizationLimits` caps templates per project, sets and entries per template, name and
  stable-ID lengths, and total explicit entries. A violation is `PRM013`/`PRM014`; the project file cap still
  applies. The exact values are in the Step 09 page.
- **Manifest and schema.** The project-format descriptor in `contracts/manifest.json`
  (`formats.project.fields.parameterizations.itemFields`) and the generated
  `contracts/generated/schema/project-v1.schema.json` are updated in the same Step 09 feature as the record
  shape. The generator is run, the new template-bearing fixtures are added to `ProjectSchemaTests.fixturePaths`,
  and `verify.sh contracts` is a gate. The manifest remains the single source; no hand-edited schema.
- **Validation.** Structural checks (`PRM004`, `PRM005`, `PRM010`, `PRM013`, `PRM014`) run in Core
  `ProjectValidator` on load, save, and edit. Compiled-bound checks (`PRM001`–`PRM003`, `PRM006`, `PRM008`,
  `PRM009`, `PRM011`, `PRM012`, `PRM015`, `PRM016`) run in `Mocassin.Packages` after compilation. `PRM007` is
  retired and not registered.

### 2.4 Job templates

```json
{
  "id": "…",
  "name": "KMC temperature series",
  "collections": [
    {
      "id": "…",
      "name": "T773",
      "simulationId": "…",
      "baseSettings": { "temperatureK": 773.0, "targetMcsp": 200 },
      "jobCountPerConfig": 4,
      "rngSeed": "018f…",
      "selectionOptimizers": [
        { "siteId": "…", "particleId": "…" }
      ],
      "configs": [
        {
          "id": "…",
          "name": "x001",
          "overrides": {
            "extensions": [
              { "id": "mmcfe", "config": { "alphaCount": 50, "relaxCycles": 20000000 } }
            ]
          },
          "lattice": { "a": 10, "b": 10, "c": 10 },
          "dopings": [{ "dopingId": "…", "fraction": 0.01 }]
        }
      ]
    }
  ]
}
```

- `baseSettings` and `config.overrides` are partial; `null`/absent means inherit. Explicit `null` cannot
  mean "set to zero"; use the numeric value for that. The shared fields are `temperatureK`, `targetMcsp`,
  `writeOutCount`, `timeLimitSeconds`, `minSuccessRate`, `flags`, `instruction`, `preRunMcsp`,
  `electricFieldModulusVm`, `electricFieldDirection`, `normalizationEnergyEv`, `maxAttemptFrequencyHz`,
  `breakTolerance`, `breakSampleLengthMcsp`, `breakSampleIntervalMcsp`, and `resultSampleMcsp`; KMC-only
  overrides are invalid on an MMC collection and vice versa.
- `jobCountPerConfig` is optional; when omitted it inherits the simulation's `JobCount`. It creates duplicate
  jobs with different derived seeds.
- Selection optimizers exclude a particle at a reference site from the KMC starting-point pool. The project
  stores `(siteId, particleId)`; the `.msl` payload stores the corresponding canonical P1 `position_id` and
  compiled `particle_id`.
- `extensions` are structured, versioned configuration objects for optional solver routines (e.g. MMCFE).
  They live inside `baseSettings`/`overrides`: `null` inherits, a non-null array replaces the inherited list as
  a whole, ids are unique and ≤64 characters, at most 16 entries and 256 KiB total are allowed, and each
  `config` is a JSON object kept as canonical JSON. Extension-config number tokens are the documented
  canonical-hash exception: they are preserved verbatim rather than normalized to shortest round-trip
  binary64 (see §2.1). A raw `instruction` string remains available for advanced/legacy workflows but is not the
  preferred configuration path.

### 2.5 Build templates

```json
{
  "id": "…",
  "name": "KMC build",
  "jobTemplateId": "…",
  "parameterizationId": "…",
  "outputName": "ceria-kmc"
}
```

The model is implied by the project. A build template is a reusable recipe; the CLI can also accept explicit
IDs instead of a template.

## 3. Simulation package (`.msl`, SQLite)

### 3.1 Purpose

The `.msl` file is the immutable compiled artifact consumed by the Rust solver and the evaluation API. It is
not the editable project and it does not contain results. Rebuilding is expected after model or
parameterization changes; collected results live in the separate `.msr` store (section 3.6).

### 3.2 Schema (version 1)

```sql
PRAGMA journal_mode = DELETE;            -- published package; build may use WAL temporarily
PRAGMA foreign_keys = ON;

CREATE TABLE schema_info (
    key   TEXT PRIMARY KEY,
    value TEXT NOT NULL
);
-- rows: ('format_version','1'), ('app_version','x.y.z'), ('created_utc','…'),
--       ('project_id','…'), ('generator','mocassin .NET x.y.z'),
--       ('job_selection','all' | '1-100,250'),
--       ('symmetry_contract_version','symmetry-1'),
--       ('space_group_data_version','spglib-2.8.0/catalog-1'),
--       ('space_group_data_sha256','…')

CREATE TABLE models (
    model_id       INTEGER PRIMARY KEY,
    signature      TEXT    NOT NULL UNIQUE,   -- Step 09 parameterization signature (ADR 0010)
    format_version INTEGER NOT NULL,
    payload        BLOB    NOT NULL            -- Zstd(MessagePack(compiled model payload, §3.3))
);

CREATE TABLE lattices (
    lattice_id     INTEGER PRIMARY KEY,
    signature      TEXT    NOT NULL UNIQUE,   -- SHA-256 canonical projection (ADR 0010)
    format_version INTEGER NOT NULL,
    payload        BLOB    NOT NULL            -- Zstd(MessagePack(InitialLattice))
);

CREATE TABLE jobs (
    job_id            INTEGER PRIMARY KEY,
    model_id          INTEGER NOT NULL REFERENCES models(model_id),
    lattice_id        INTEGER NOT NULL REFERENCES lattices(lattice_id),
    kind              TEXT    NOT NULL CHECK (kind IN ('kmc','mmc')),
    collection_name   TEXT    NOT NULL,
    config_name       TEXT    NOT NULL,
    collection_index  INTEGER NOT NULL,
    config_index      INTEGER NOT NULL,
    job_index         INTEGER NOT NULL,
    temperature_k     REAL    NOT NULL,
    electric_field_vm REAL    NOT NULL DEFAULT 0.0,
    target_mcsp       INTEGER NOT NULL,
    write_out_count   INTEGER NOT NULL DEFAULT 100,
    pre_run_mcsp      INTEGER NOT NULL DEFAULT 0,
    time_limit_s      INTEGER,
    min_success_rate  REAL,
    lattice_a         INTEGER NOT NULL,
    lattice_b         INTEGER NOT NULL,
    lattice_c         INTEGER NOT NULL,
    doping_info       TEXT    NOT NULL DEFAULT '',
    flags             INTEGER NOT NULL DEFAULT 0,
    seed              BLOB    NOT NULL,        -- 32 bytes, derived per job
    payload           BLOB    NOT NULL,        -- Zstd(MessagePack(JobPayload))
    created_utc       TEXT    NOT NULL,
    UNIQUE (collection_index, config_index, job_index)
);

CREATE INDEX ix_jobs_temperature ON jobs(temperature_k);
CREATE INDEX ix_jobs_collection  ON jobs(collection_name, config_name);

```

Rules:

- `job_id` is a deterministic 1-based integer assigned at build time in template order
  (collection index, config index, job index) and never reused within a package. A partial build keeps the full
  expansion's IDs and may therefore leave gaps; `job_id` is always unique and ascending in template order.
- `schema_info.job_selection` records the **canonical normal form** of the selection, not the user's text. Step
  12 writes `all` when every expanded job is selected; otherwise it writes a sorted, merged, comma-separated
  list of 1-based full-expansion IDs and ranges (`1-3,5,7-9`). Equivalent inputs (`1,2,3` and `1-3`) therefore
  produce the same metadata and the same package, and a full selection normalized to `all` is
  indistinguishable from an omitted selection. Unselected jobs are absent from `jobs` and from the
  content-addressed `lattices`; their IDs remain reserved by the full expansion.
- The writer always sets the denormalized columns from the resolved job payload; SQL defaults (including
  `write_out_count DEFAULT 100`) are fallbacks only. `created_utc` is build metadata rather than a payload copy,
  so verification checks the copy relationship only for the payload-derived columns.
- The published package has no `results` table. Results live in `.msr` (section 3.6).
- `application_id` is `0x4D4F4353` (`"MOCS"`) and `user_version` mirrors the package format version (`1`); both
  are set in addition to `schema_info` for tooling.
- The solver opens the package read-only (`SQLITE_OPEN_READONLY`) and never writes it.
- `schema_info` includes `format_version`, `app_version`, `created_utc`, `project_id`, `generator`,
  `job_selection`, and the three symmetry provenance rows. Every model in one package shares the same symmetry
  provenance.
- The package format version, the model/job/lattice payload versions, and `CompiledModel.FormatVersion` are
  independent version domains. Version 1 writers write `1` for each; a change in any one of them is a change to
  that domain only.
- Version 1 writes exactly one `models` row per package. The foreign key stays in the schema so a later version
  can store several parameterizations of one compiled model.
- Derived solver values are not columns: Step 16 derives attempt-frequency factors from the payload's absolute
  `jump_rules[].frequency_hz` and the job's `max_attempt_frequency_hz`.

### 3.3 Compiled model payload

MessagePack map with snake_case keys. Top-level:

| Key | Type | Meaning |
| --- | --- | --- |
| `format_version` | u32 | Payload version |
| `signature` | string | SHA-256 of canonical parameterized model (geometry + energy/frequency values); the Step 09 parameterization signature |
| `model_signature` | string | SHA-256 of canonical compiled geometry/rules/encoding (Step 08 model signature), excluding parameterization values and runtime settings |
| `symmetry_contract_version` | string | Symmetry contract version, e.g. `symmetry-1` |
| `space_group_data_version` | string | Catalog data version, e.g. `spglib-2.8.0/catalog-1` |
| `space_group_data_sha256` | string | SHA-256 of the canonical catalog payload |
| `particle` | object | `charges`, `symbols`, `is_vacancy`, count; index 0 is void |
| `structure` | object | cell vectors, P1 positions, site definitions, environments, interaction range |
| `energy` | object | pair tables, cluster tables, defect background |
| `transitions` | object | jump collections, directions, rules, mapping tables |
| `simulations` | array | simulation encodings (active collections, settings defaults, trackers) |

- `particle` indices are compiled indices: `0` is void, then authored particles in ascending `ParticleId`
  order. This is not the preview/state-change-matrix order (authored particles then void); consumers map by
  `ParticleId`, never by preview array position.
- `signature` is the Step 09 parameterization signature and the `models.signature` uniqueness key;
  `model_signature` is the Step 08 compiled-model signature used by parameterization templates and inspection.
- The parameterization projection is re-encodable from this payload (applied tables plus absolute rule
  frequencies). The model projection is **not** re-encodable from it: it signs stable IDs and derived
  classification data that are inspection-only. Readers therefore treat `model_signature` as an identity
  string, and the writer cross-checks it against `ParameterizedModel.ModelSignature` at write time.
- The Step 08 `model-v1` and Step 09 `parameterization-v1` projections are finalized once before the first
  released `.msl`; ADR 0010 records the pre-release signature-layout exception for both.
  `signatures.model.status = "final"` and `signatures.parameterization.status = "final"` are not release
  markers. A value-only parameterization change changes `signature` but not `model_signature`.
- The model-signature projection includes the symmetry contract version and the catalog data version and
  checksum, so a rule or catalog change invalidates parameterizations.
- `model_signature` is additive before the first released `.msl`. All payload versions stay `1` because no
  released package exists, matching the pre-release additive-v1 precedent; Step 11 adds the manifest payload
  entries, the generated schema, and the package fixtures.
- The payload is the parameterized form. Step 08's internal `CompiledModel` is not the persisted DTO; Step 11
  maps `CompiledModel` + the Contracts `ParameterizationValues` to this payload. The build-time
  `ParameterizedModel` used by Steps 10–11 lives in `Mocassin.Packages.Parameterization` and wraps
  `CompiledModel`, `ParameterizationValues`, and the parameterization signature; it is not the persisted DTO.
  Stable geometry/rule ID strings, expansion maps, equivalent images, input hashes, timings, diagnostics,
  cache statistics, derived classification flags, and the index→ID map are JSON-inspection only
  (`compiled-inspection-v1`) and are not persisted in `.msl`.
- The parameterization signature covers the applied pair/group/site values and rule frequencies in dense
  compiled order. It does not include stable ID strings, so Step 13 can re-encode it from this payload without
  persisting the inspection-only `rule_geometry_id`. A value-only change changes `signature` but not
  `model_signature`; a geometry/rules/catalog change changes both.

`structure` fields:

| Key | Type | Meaning |
| --- | --- | --- |
| `cell_vectors_ang` | `[[f64;3];3]` | Cartesian a/b/c vectors in Å |
| `positions` | array | `{vector4:[i32;4], site_index:u32, stable:bool, particle_ids:u8[]}` in ascending `P` order; `vector4` is `[cellA, cellB, cellC, P]`, with base positions `[0,0,0,P]` |
| `position_count` | u32 | Length of `positions` |
| `site_count` | u32 | Number of user reference sites |
| `environments` | array | Per position: `selection_particle_mask:u64`, `pairs:[{target_position:u32, offset:[i32;3], energy_id:u32}]`, `clusters:[{cluster_id:u32, byte_position:u8}]`, `update_particle_ids:u8[]` |
| `interaction_range` | `[i32;4]` | MMC interaction cube |
| `static_trackers_per_cell` | u32 | |
| `global_tracker_count` | u32 | |

- `position_count = Pcount`.
- `site_index` identifies the reference site; reference sites are sorted by `CellSiteId` during compilation,
  so the index is stable for a given structure. It is not the P index.
- `positions` are in ascending `P` order. `vector4` is the full encoded position `[cellA, cellB, cellC, P]`
  with base positions `[0,0,0,P]`. `particle_ids` is the ascending list of compiled particle indices set in
  `AllowedParticleMask`, including void (`0`) when the position may be empty; consumers reconstruct the mask
  from it.
- `environments` is indexed by position. Each entry carries `selection_particle_mask:u64`, `pairs`,
  `clusters`, and `update_particle_ids`. A pair link is
  `{target_position:u32, offset:[i32;3], energy_id:u32}` and there is **one entry per periodic image**:
  - `target_position` is the partner canonical P1 index; `energy_id` is the shared pair table index.
  - `offset` is the image's relative cell offset `[cellA, cellB, cellC]` from the centre position. It is
    required: the same `target_position` occurs once per image, images can use different pair tables, and
    without the offset the solver cannot place the target instance.
  - The runtime target is the centre's supercell cell plus `offset`, wrapped component-wise into
    `[0,A)×[0,B)×[0,C)`, at `target_position`. Step 14 owns this expansion; Step 15 consumes the resulting
    `(target, pair_id)` links.
  - The compiled source is `CompiledPairLink`, one entry per periodic image: `PairCompilationStage` expands
    each `PairShell.EquivalentImages` (via the additive `PairInteractionPreview.EquivalentImages`) into one
    link per image, so the model signature continues to sign every runtime image with its existing four-field
    link schema. The writer maps the links one-to-one; `energy_id = PairTableIndex`.
  - `distance_bucket_fm` is not stored in the package: the pair table index is the energy identity, and the
    bucket remains signed in the compiled link and used by parameterization/inspection.
- A cluster link is `{cluster_id:u32, byte_position:u8}` used for incremental occupation-code updates.
- `interaction_range` is the MMC local-update cube `[maxCellA, maxCellB, maxCellC, 0]`, derived from the
  maximum stable/unstable cutoff and the cell interplanar spacings plus one boundary cell.
- `static_trackers_per_cell = position_count * (particle_count - 1)`; void (index 0) has no tracker.
- `global_tracker_count` is the sum of per-collection non-void particle tracker counts. These are tracker
  **definitions**, not mutable values; Step 18 owns the values.
- The linear index is
  `P + cellC*Pcount + cellB*Pcount*Ccount + cellA*Pcount*Ccount*Bcount`.
- The index is a signed 32-bit integer. The total `Acount*Bcount*Ccount*Pcount` is validated in 64-bit and
  must not exceed `2,147,483,647`; larger lattices are rejected before allocation.
- Negative cell offsets are relative-path coordinates and are not linearized directly.

`energy` fields:

| Key | Type | Meaning |
| --- | --- | --- |
| `pair_tables` | array | `{object_id, energies:[f64; n_particles*n_particles]}` |
| `cluster_tables` | array | `{object_id, occupation_codes:[u64], energies:[f64], particle_table_mapping:[u8]}` |
| `defect_background` | array | `[position][particle]` site energy in eV, row-major; Step 09 resolves the authored `siteEnergies` background, applies template overrides, and quantizes it |

- Pair tables are in canonical shell order: `(distanceBucket, targetP, cellA, cellB, cellC)`, then site
  indices. The stable geometry ID is a content hash used for parameterization matching, not table order; its
  projection includes the center and partner P1 position indices (`centerP`, `partnerP`) as well as the site
  IDs, periodic offset, and distance bucket. The compiled structure environments carry each shell's geometry
  ID, shared energy ID, center/partner P1 indices, distance bucket, representative offset, equivalent-offset
  count, and optional chiral partner geometry ID.
- Pair `object_id` is the shared `EnergyId`; the table is `n_particles × n_particles` row-major
  `[centerParticle][partnerParticle]`. Entries for particle combinations the site pair cannot realize are
  zero; the position's `selection_particle_mask` is authoritative.
- Chiral partners share one `object_id` (the shared `EnergyId`) and one energy table. Each direction keeps its
  own `GeometryId` and records the inverse direction's `ChiralPartnerGeometryId`.
- Cluster occupation codes use 6 bits per site, site 0 in the least significant bits, up to 9 sites; unused
  high bits are zero. Codes are canonical under the induced site-permutation group.
- Cluster `object_id` is the stable cluster energy identity
  `SHA-256("mocassin/cluster-energy-id/v1" || clusterId || centerSiteId || canonical geometry hash)`.
  `occupation_codes` are sorted ascending; `particle_table_mapping` holds one row per allowed center particle
  in ascending compiled particle index.
- Pair and cluster energies are the **applied** Step 09 values in compiled table/occupation order, never the
  compiler's zero defaults. `defect_background` is likewise the resolved background, not the authored sparse
  site-energy list.
- Pair shells use the `1 fm` distance bucket. Candidate enumeration uses a `cutoff + 1 fm` margin and includes
  shells with `bucket <= cutoffBucket`.
- Minimum-image and radial-search results always carry the cell offset; no implicit wrapping is allowed.

`transitions` fields:

| Key | Type | Meaning |
| --- | --- | --- |
| `jump_collections` | array | `{kind, transition_id, mobile_particle_mask:u64, rule_ids:[u32], direction_ids:[u32]}` |
| `jump_directions` | array | `{object_id, position_id, collection_id, path:[i32;4], movement:[[f64;3]], path_vector:[f64;3], length, is_inverse}` |
| `jump_rules` | array | `{rule_id, collection_id, state0:u64, state1:u64, state2:u64, frequency_hz:f64, charge_transport_vector:[f64;3], static_correction, tracker_order:u8[]}` |
| `jump_count` | array | `[position][particle] -> u32` |
| `jump_mapping` | array | Flat `u32` concatenation in `[position][particle][relative]` order; offsets are the prefix sums of `jump_count` |
| `static_tracker_mapping` | array | Dense `[position][particle] -> i32` tracker offset, `-1` when none |
| `global_tracker_mapping` | array | Dense `[collection][particle] -> i32` tracker index, `-1` when none |

- A collection is identified by its dense array index and its authored `transition_id`; it has no separate
  stable `object_id`. A direction's `object_id` is the compiled `CompiledJumpDirection.ObjectId`.

- `rule_id` is a dense `u32` assigned by canonical collection order, then `(state0, state1, state2)` and the
  representative direction; stable rule geometry IDs are inspection-only and are not persisted.
- `state0`/`state1`/`state2` use 6 bits per path node, node 0 least significant, unused high bits zero. For
  MMC, `state1` is `0` and is ignored.
- Step 09 applies an absolute frequency per rule (defaulting missing entries to the compiled `1e13 Hz`).
  Step 11 writes that absolute value as `frequency_hz`; it is the authoritative model value and is part of
  the parameterization signature. Step 16 derives any per-job normalization or pre-check factor from
  `frequency_hz` and the job's `max_attempt_frequency_hz`; the factor is not persisted in the model payload
  and is not part of the model signature. Step 08's internal default is not a second runtime source.
- `movement` is the authoritative fractional displacement per path node. `path_vector` is the Cartesian net
  displacement, obtained by converting each movement with the cell geometry and summing; `length` is its
  magnitude. `charge_transport_vector` is the particle-identity sum
  `Σ_p q_p * (x_p(S2) - x_p(S0))` in e·Å and is the exact field input; a particle that moves contributes once,
  void contributes nothing. Step 15 computes `field = -0.5*(|E|/kT)*(charge_transport_vector·ê)`. A direction
  has no stored field scalar. `static_correction` is `null`/NaN in v1 (dynamic S2).
- `tracker_order[i]` is the compiled **global tracker index** that follows the mobile particle at path node `i`,
  or the sentinel `0xFF` for a non-mobile node. Its length is exactly the rule's path-node count (the
  representative direction's `path` length and the state-code node count); it is a variable-length `u8[]`, not
  a fixed eight-entry array. The compiled source is `CompiledJumpRule.TrackerOrder`, whose length the model
  signature also projects. Tracker mappings are definitions; Step 18 owns the values and updates.
- Within one collection, `state0` maps to at most one rule; the compiler rejects ambiguity. A direction is
  listed in `jump_mapping` only when at least one rule can start from that `(position, particle)`.

`simulations` fields:

| Key | Type | Meaning |
| --- | --- | --- |
| `id` | string | Project simulation ID |
| `kind` | string | `kmc` / `mmc` |
| `jump_collection_ids` | `u32[]` | Active collections |
| `default_settings` | object | Temperature, MCSP, write-out count, pre-run, limits, field, normalization, MMC samples, flags |
| `tracker_models` | array | Global/static tracker definitions and indices; empty/absent for MMC |

- `tracker_models` are definitions only. Global trackers are `(collection, particle)`; static trackers are
  `(position, particle)` per cell, and their indices match `global_tracker_mapping`/`static_tracker_mapping`.
  Step 18 owns mutable values, mobile-tracker assignment, and `.mcs` output.
- `default_settings.flags` contains only the authored bits (`noSaving`, `noJumpLogging`, `useFastExp`,
  `deterministic`). Derived intent (`UseDualDofCorrection`, `UsePrerun`, kind) is not a model-payload field:
  Step 10 carries it on the job descriptor and Step 11 composes the job `flags` word.
- `default_settings.field_direction` is the normalized Cartesian unit vector, using the same conversion as the
  job payload, so the model and job settings never disagree on the coordinate space.
- Runtime default settings are not part of `model_signature`; they are job-expansion inputs and are excluded
  from the template-invalidating signature.

Exact array lengths and invariants are validated by `mocassin-msl` before the solver allocates memory.

### 3.4 Job payload

| Key | Type | Meaning |
| --- | --- | --- |
| `format_version` | u32 | |
| `simulation_index` | u32 | Index into `simulations` |
| `kind` | string | `kmc` / `mmc` |
| `temperature_k` | f64 | |
| `target_mcsp` | u64 | |
| `write_out_count` | u32 | Intermediate data-out calls; 0 = final state only |
| `pre_run_mcsp` | u64 | 0 = pre-run disabled |
| `time_limit_s` | u64? | `null` = no wall-clock limit |
| `min_success_rate` | f64? | MCS/s; `null` or 0 disables the rate abort |
| `electric_field_vm` | f64 | |
| `electric_field_direction` | `[f64;3]` | Normalized Cartesian unit vector, derived from the authored fractional direction with the compiled cell vectors (`fa·a + fb·b + fc·c`) and then normalized. A zero modulus stores the normalized authored direction when non-zero and `(1,0,0)` when the authored vector is also zero. The solver consumes this vector directly. |
| `normalization_energy_ev` | f64? | `null` = no normalization; `0.0` canonicalizes to `null` |
| `max_attempt_frequency_hz` | f64? | Job-level maximum used for normalization/pre-check factor derivation; `null` = maximum of the model's absolute `jump_rules.frequency_hz` values. It does not replace or rescale the persisted model frequencies. |
| `break_tolerance` | f64? | MMC only; 0 disables the break check |
| `break_sample_length_mcsp` | u64? | MMC only |
| `break_sample_interval_mcsp` | u64? | MMC only |
| `result_sample_mcsp` | u64? | MMC only |
| `flags` | u32 | `SolverJobFlags` word: bit 0 `noSaving`, bit 1 `noJumpLogging`, bit 2 `useFastExp`, bit 3 `deterministic`, bit 4 `useDualDofCorrection`, bit 5 `usePrerun`, bit 6 `kmcSimulation`, bit 7 `mmcSimulation`. Step 11 composes it with `SolverJobFlagsMapper.Compose` from the authored flags, the job kind, the KMC pre-run, and the descriptor's dual-DOF intent. |
| `selection_optimizers` | array | `{position_id:u32, particle_id:u8}`; `position_id` is the canonical P1 position index; entries are sorted by `(position_id, particle_id)` |
| `extensions` | array | `{id: string, config_json: string}` for optional solver routines; `config_json` is the UTF-8 canonical JSON text of the authored config, sorted by `id`, preserved verbatim for numeric tokens |
| `instruction` | string? | Advanced/legacy free-text instruction |
| `doping_info` | string | Machine-readable `v1:{dopingId}:{priority}:{requestedPrimary}:{appliedPrimary}:{appliedDependent}:{effectiveFraction:R}` entries joined by `;`; `dopingId` is a lowercase hyphenated GUID; empty when no doping values are configured |

The lattice itself is stored once in the `lattices` table and referenced by the job:

| Key | Type | Meaning |
| --- | --- | --- |
| `format_version` | u32 | Payload version |
| `lattice_size` | `[u32;3]` | Supercell a,b,c |
| `position_count` | u32 | P1 positions per unit cell |
| `lattice` | `u8[]` | Initial particle index per linearized position; length = a*b*c*position_count |

The lattice payload is only the initial state: it has no doping summary, because the same particle array must
dedupe across jobs and the job payload and `jobs.doping_info` column already record the applied doping
provenance. The lattice signature projection (ADR 0010) binds the payload `format_version`, the referenced
model's `model_signature`, `lattice_size`, `position_count`, and `lattice`; it excludes build metadata and the
doping summary.

The `lattice` array uses row-major cell order. With cell indices `a ∈ [0,A)`, `b ∈ [0,B)`, `c ∈ [0,C)` and
canonical P1 position `P ∈ [0,position_count)`:

```text
index = ((a * B + b) * C + c) * position_count + P
```

The same layout is used by Step 10's generator/preview, the `.msl` writer, and the Step 13 reader.

The solver derives the site-energy background from the compiled model's `[position][particle]` table repeated
over the supercell. Per-job background overrides are not part of version 1; a future extension can add a
sparse `background_overrides` map without changing the base layout.

### 3.5 Publication and read-only guarantees

The `.msl` is immutable after a successful build:

1. Build in a temporary file named `<target>.msl.tmp-<pid>-<random>` in the target directory. WAL may be used
   during the build.
2. Run `PRAGMA wal_checkpoint(TRUNCATE)`, switch to `PRAGMA journal_mode=DELETE`, and close the database.
3. Hash the finished file for the build result, `fsync` the file and the containing directory, then atomically
   rename it to the final `.msl` path. There is no `.bak` file: atomic replacement is the safety guarantee.
4. Never modify the published file. `collect` writes `.msr`; it does not update `.msl`.

A published `.msl` must be readable without `-wal` or `-shm` files. This makes it safe on read-only media and
shared HPC filesystems. The solver and evaluation readers open it with
`SQLITE_OPEN_READONLY`, `query_only=ON`, and `foreign_keys=ON`. Readers must not rely on WAL semantics.

#### Canonical package summary (`package-summary-v1`)

Determinism, golden fixtures, and inspection compare a **documented inspector projection**, not raw file bytes.
The projection is UTF-8 JSON (2-space indent, `\n`, invariant shortest round-trip numbers, `-0.0` as `0.0`,
arrays sorted by the keys below):

```json
{
  "format": "mocassin.package-summary",
  "formatVersion": 1,
  "schema": {
    "formatVersion": 1,
    "projectId": "…",
    "jobSelection": "all",
    "symmetryContractVersion": "symmetry-1",
    "spaceGroupDataVersion": "…",
    "spaceGroupDataSha256": "…"
  },
  "models": [
    { "signature": "…", "modelSignature": "…", "payloadFormatVersion": 1,
      "particleCount": 3, "positionCount": 32, "siteCount": 2, "environmentCount": 32,
      "pairTableCount": 4, "clusterTableCount": 0, "collectionCount": 1, "ruleCount": 96 }
  ],
  "lattices": [
    { "signature": "…", "latticeSize": [10, 10, 10], "positionCount": 32, "latticeLength": 32000 }
  ],
  "jobs": [
    { "jobId": 1, "modelSignature": "…", "latticeSignature": "…", "kind": "kmc",
      "collectionName": "T773", "configName": "x001", "collectionIndex": 0, "configIndex": 0,
      "jobIndex": 0, "temperatureK": 773.0, "electricFieldVm": 1.0e7, "targetMcsp": 200,
      "writeOutCount": 100, "preRunMcsp": 0, "timeLimitS": 31536000, "minSuccessRate": null,
      "latticeSize": [10, 10, 10], "dopingInfo": "", "flags": 64, "seedHex": "…" }
  ],
  "counts": { "modelCount": 1, "latticeCount": 1, "jobCount": 1, "latticeReuseFactor": 1.0 }
}
```

- Excluded and normalized: `created_utc`, `app_version`, `generator`, file size, payload byte hashes, SQLite
  page layout, and Zstd/MessagePack implementation details. `seedHex` is the deterministic job seed in
  lowercase hex, so it is stable and comparable.
- `lattices` is the distinct set sorted by `signature`; `models` by `signature`; `jobs` by `jobId`.
  `counts.latticeReuseFactor` is `jobCount / latticeCount` (1.0 means every lattice is unique; higher means
  reuse).
- The summary is produced by the Step 11 inspector (`Mocassin.Results.Packages`) and is the golden artifact
  beside each checked-in `.msl`. It is not stored in the package and is not a package format.

### 3.6 Results store (`.msr`, SQLite)

Results are stored separately so the package remains immutable and the solver never contends with a result
writer (ADR 0011). The default path is `<package>.msr`; `--results` overrides it.

```sql
PRAGMA journal_mode = DELETE;
PRAGMA foreign_keys = ON;

CREATE TABLE schema_info (
    key   TEXT PRIMARY KEY,
    value TEXT NOT NULL
);
-- rows: ('format_version','1'), ('app_version','x.y.z'), ('created_utc','…'),
--       ('last_collected_utc','…'), ('generator','mocassin .NET x.y.z'),
--       ('package_signature','sha256 of exact .msl bytes'), ('package_format_version','1')

CREATE TABLE results (
    job_id              INTEGER PRIMARY KEY,
    status              TEXT NOT NULL CHECK (status IN
                          ('pending','running','completed','failed','cancelled','aborted')),
    exit_code           INTEGER,
    started_utc         TEXT,
    completed_utc       TEXT,
    wall_time_s         REAL,
    stdout              TEXT,
    run_state_storage   TEXT NOT NULL DEFAULT 'embedded'
                          CHECK (run_state_storage IN ('embedded','external')),
    run_state           BLOB,                   -- exact run.mcs bytes when embedded
    run_state_path      TEXT,                   -- relative path when external
    prerun_state_storage TEXT NOT NULL DEFAULT 'embedded'
                          CHECK (prerun_state_storage IN ('embedded','external')),
    prerun_state        BLOB,                   -- exact prerun.mcs bytes when embedded
    prerun_state_path   TEXT,                   -- relative path when external
    run_state_sha256    TEXT,
    prerun_state_sha256 TEXT,
    error_message       TEXT,
    updated_utc         TEXT NOT NULL
);

CREATE INDEX ix_results_status ON results(status);
```

Rules:

- `job_id` matches the immutable `.msl` job id.
- `run_state`/`prerun_state` store the exact `.mcs` bytes when `*_storage = 'embedded'`; the SHA-256 is over
  those bytes.
- SQLite builds have a default maximum blob length of about 1 GiB. `collect` embeds states up to a
  configurable cap (default 1 GiB) and stores larger states as external files referenced by the relative
  `*_path`, with the same SHA-256. `--external-states` forces external storage.
- `.msr` and its external state files travel together; `verify` checks that every external path stays inside
  the `.msr` directory and that the hash matches. This is state spill, not the deferred external-results mode.
- External state files are written atomically before the `.msr` transaction commits. Orphan files are ignored
  and can be removed by `mocassin clean`.
- `schema_info.package_signature` is the SHA-256 of the exact `.msl` file bytes; `package_format_version` is
  the package major version. A mismatch is an error, not a warning. Rebuilding a package invalidates old
  results, even if the logical model is unchanged.
- `collect` takes an exclusive `.msr.lock`, writes in a transaction, fsyncs, and closes. Concurrent collectors
  are rejected. Readers open read-only and see a consistent snapshot.
- New stores are written to a temporary file and atomically renamed. Updating an existing store is a
  transaction; a failed collection leaves the previous state intact.
- `evaluate` and the result readers join `.msl` jobs with `.msr` results (via `ATTACH` or two queries). The
  results store does not duplicate job metadata.
- `.msr` is local/shared-storage-friendly but requires working POSIX locking for collection. Do not run
  multiple collectors against the same file over a network filesystem.

## 4. Result state file (`.mcs`, MessagePack, version 1)

### 4.1 Design

The `.mcs` file is a **single MessagePack document** encoded with `MessagePack-CSharp` in .NET and
`rmp-serde` in Rust. It is not a custom binary container: there is no hand-written header, section table,
alignment scheme, or checksum trailer.

- **Top-level value is a map** with stable camelCase string keys. Readers ignore unknown fields for forward
  compatibility and validate every field they use.
- **`format` + `formatVersion`** identify the document. The current values are `"mocassin.mcs"` and `1`.
- **The lattice is a MessagePack `bin`** (`byte[]` in .NET, `serde_bytes`/`Vec<u8>` in Rust). All other numeric
  payloads are native MessagePack arrays (`uint64`, `float64`, etc.).
- **No compression by default.** MessagePack is already compact; transport/storage compression is an external
  concern and must not change the document format.
- **No embedded checksum.** The collector computes SHA-256 of the file and stores it in
  `results.run_state_sha256` in the `.msr`. A corrupted file is rejected by the collector/reader.
- **Size assumption:** the document is expected to stay below 4 GiB. MessagePack `bin`/array lengths are
  32-bit, so this is also the practical format limit. The solver rejects an encoded state above a configurable
  cap (default 3.5 GiB) with an actionable error rather than producing a truncated file.
- **MMC** omits KMC-only fields (`globalTrackers`, `mobileTrackers`, `staticTrackers`, `mobileMapping`,
  `jumpStatistics`). `prerun.mcs` uses the same schema with `metadata.phase = "prerun"`.
- The metadata is intentionally self-contained for the Python reader: cell vectors, particle charges/symbols,
  temperature, and field are sufficient for basic displacement/diffusion/mobility/conductivity evaluations
  without opening the `.msl` or `.msr`.

**Interoperability rules**

- Objects are encoded as MessagePack **maps with string keys**, not positional arrays. In Rust, use
  `rmp_serde::Serializer::new().with_struct_map()` and `#[serde(rename_all = "camelCase")]`; in .NET, use
  `[MessagePackObject]` with string `[Key("...")]` names or an equivalent generated resolver.
- `lattice` must be a MessagePack `bin` (`byte[]` / `serde_bytes::ByteBuf`), not an array of integers.
- A tracker vector is a 3-element array `[x, y, z]` of `float64`, in that order. In .NET this is `McsVector3`
  with a custom MessagePack formatter; in Rust it is `[f64; 3]`.
- `format`/`formatVersion` are regular map fields; there is no separate binary header.
- Readers must ignore unknown map keys and reject unsupported `format`/`formatVersion` values.

### 4.2 Top-level map

| Key | Type | Required | Meaning |
| --- | --- | --- | --- |
| `format` | string | yes | `"mocassin.mcs"` |
| `formatVersion` | uint32 | yes | `1` |
| `metadata` | map | yes | See 4.3 |
| `lattice` | bin | yes | Particle index per linearized position, `A*B*C*P` bytes |
| `counters` | array of maps | yes | One entry per particle index; see 4.4 |
| `globalTrackers` | array of `[f64;3]` | KMC | Fractional displacement per global tracker |
| `mobileTrackers` | array of `[f64;3]` | KMC | Fractional displacement per mobile particle |
| `staticTrackers` | array of `[f64;3]` | KMC | Fractional displacement per static tracker |
| `mobileMapping` | array of uint32 | KMC | Position index for each mobile tracker |
| `jumpStatistics` | array of maps | KMC | Jump histograms per global tracker; see 4.4 |
| `energySamples` | array of maps | no | MMC energy trace / KMC sample series |
| `particlePositions` | array of uint32 | no | Optional diagnostic positions of mobile particles |

### 4.3 `metadata` map

| Key | Type | Meaning |
| --- | --- | --- |
| `solverVersion` | string | `mocassin-sim` version |
| `modelSignature` | string | Parameterized model signature from the `.msl` (geometry + values) |
| `jobId` | uint32 | Package job id |
| `kind` | string | `"kmc"` or `"mmc"` |
| `phase` | string | `"run"` or `"prerun"` |
| `createdUtc` | string | ISO-8601 UTC timestamp |
| `latticeSize` | `[uint32;3]` | Supercell `A,B,C` |
| `positionCount` | uint32 | P1 positions per unit cell (`P`) |
| `particleCount` | uint32 | Number of particle indices including void |
| `cellVectorsAngstrom` | `[[f64;3];3]` | Cartesian `a/b/c` cell vectors for fractional ↔ Cartesian conversion |
| `particleCharges` | `[f64]` | Charge number per particle index (including void at 0) |
| `particleSymbols` | `[string]` | Human-readable symbol per particle index |
| `particleVacancyFlags` | `[bool]` | Vacancy flag per particle index |
| `temperatureK` | f64 | Simulation temperature |
| `electricFieldVm` | f64 | Field modulus in V/m; 0 if none |
| `electricFieldDirection` | `[f64;3]` | Normalized Cartesian field direction; `[0,0,0]` if none |
| `trackerCounts` | map | `{ "global": u32, "mobile": u32, "static": u32 }` |
| `cycleCount` | uint64 | All simulation cycles/attempts |
| `mcsCount` | uint64 | Successful Monte Carlo steps |
| `targetMcsp` | uint64 | Target successful steps per mobile particle |
| `simulatedTimeS` | f64 | Simulated KMC time in seconds; 0 for MMC |
| `normalizationFactor` | f64 | `K_norm`; 1 for MMC |
| `fixedNormalizationFactor` | f64 | `K_fix`; 1 when no fixed normalization is used |
| `rawMaxJumpProbability` | f64 | Observed raw maximum; 0 for MMC |
| `prerunGoal` | uint64 | Pre-run target cycles; 0 for MMC |
| `totalGoal` | uint64 | Main-run target cycles; 0 for MMC |
| `latticeEnergyEv` | f64 | Current total lattice energy in eV |
| `flags` | uint64 | Solver state flags |
| `rngAlgorithm` | string | `"chacha12-v1"` (versioned with the state format) |
| `rngState` | bin | Opaque, versioned PRNG state needed for exact resume; length/algorithm documented in Step 13 |
| `abortReason` | string | `running`, `prerun`, `completed`, `equilibrium`, `timeLimit`, `rateLimit`, `cancelled`, `error` |
| `seedHash` | string | SHA-256 hash of the job seed (not the raw seed) |
| `units` | map | `{ "energy": "eV", "time": "s", "length": "angstrom", "tracker": "fractional" }` |
| `runner` | map | Optional `{ "os", "arch", "rustVersion" }` |

### 4.4 Counters, histograms, and energy samples

**Counter entry** (array index = particle index):

| Key | Type | Meaning |
| --- | --- | --- |
| `particleIndex` | uint32 | Redundant but makes the document self-describing |
| `skip` | uint64 | Frequency-precheck skips |
| `success` | uint64 | Accepted state advances |
| `rejected` | uint64 | Statistically rejected attempts |
| `blocked` | uint64 | No matching rule / blocked site |
| `startUnstable` | uint64 | S0-unstable outcomes |
| `endUnstable` | uint64 | S2-unstable outcomes |

**Jump statistics entry** (one per global tracker):

| Key | Type | Meaning |
| --- | --- | --- |
| `globalTrackerIndex` | uint32 | Index into `globalTrackers` |
| `particleIndex` | uint32 | Tracked particle |
| `transitionIndex` | uint32 | Compiled transition index |
| `acceptedCount` | uint64 | Accepted jumps for this tracker |
| `edge` | histogram | Raw `Σ E_i(S1)` |
| `positiveConfiguration` | histogram | Positive `ΔE_conf` |
| `negativeConfiguration` | histogram | `|ΔE_conf|` for negative values |
| `total` | histogram | Total barrier including field |

**Histogram**:

| Key | Type | Meaning |
| --- | --- | --- |
| `minEv` | f64 | Inclusive lower edge |
| `maxEv` | f64 | Exclusive upper edge |
| `bins` | array of uint64 | Bin counts; length is the bin count (default 1000) |
| `underflow` | uint64 | Values below `minEv` |
| `overflow` | uint64 | Values at/above `maxEv` |

**Energy sample**:

| Key | Type | Meaning |
| --- | --- | --- |
| `mcs` | uint64 | MCS at which the sample was taken |
| `latticeEnergyEv` | f64 | Lattice energy |
| `acceptedDeltaEv` | f64 | Mean accepted energy change in the window |

### 4.5 Validation

A reader must:

1. Check `format == "mocassin.mcs"` and `formatVersion` is supported.
2. Validate required metadata fields and reject missing/ill-typed values.
3. Validate `lattice.length == latticeSize[0] * latticeSize[1] * latticeSize[2] * positionCount`.
4. Validate every lattice particle index is `< particleCount`.
5. Validate `cellVectorsAngstrom` is 3×3 and non-singular; validate `particleCharges`, `particleSymbols`, and
   `particleVacancyFlags` each have length `particleCount`; validate `electricFieldDirection` is normalized when
   the field modulus is non-zero.
6. Validate `globalTrackers`, `mobileTrackers`, `staticTrackers`, and `mobileMapping` lengths against
   `metadata.trackerCounts`; validate every mapping value is a valid lattice position.
7. Validate `jumpStatistics.globalTrackerIndex` values are unique and in range; validate histogram
   `bins.Length`, `minEv < maxEv`, and finite numeric values.
8. Require KMC fields for `kind == "kmc"`; require them to be absent/empty for `kind == "mmc"`.
9. Reject NaN/Inf where the schema requires a finite value (energies, times, factors).
10. Ignore unknown fields; reject duplicate map keys where the decoder exposes them.
11. Reject an encoded document above the configured size cap.

The collector and `.msr` readers verify `run_state_sha256` before storing or evaluating a state.

### 4.6 Contract testing

- `tests/contracts/state-v1.mcs` is written by Rust and read by .NET and Python; each reader emits a canonical
  JSON summary of metadata, counters, tracker counts, and histogram totals.
- A .NET fixture writer may also emit the same schema for reader tests.
- A future `formatVersion` is rejected with a clear error; additive fields are ignored.
- Fuzzing covers malformed MessagePack, wrong types, oversized arrays, invalid indices, and version mismatches.

## 5. CLI contracts

### 5.1 `mocassin` (.NET)

```text
mocassin new <name> [-o <dir>] [--template blank|ceria] [--json]
mocassin validate <project.mocprj> [--json] [--warnings-as-errors] [--stats]
mocassin compile <project.mocprj> [-o <compiled-inspection.json>] [--json]
mocassin build <project.mocprj> [-o <project.msl>]
                [--preview]
                [--template <buildTemplateId>]
                [--job-template <jobTemplateId>]
                [--parameterization <parameterizationId>]
                [--jobs <range>] [--parallel <n>] [--compact] [--force]
                [--warnings-as-errors] [--quiet] [--json]
mocassin inspect <project.msl> [--json]
mocassin verify <project.msl> [--json]
mocassin doctor [--json]
mocassin clean <project.msl> [--dir <dir>] [--older-than <days>] --dry-run [--json]
mocassin simulate <project.msl> --jobs <range> [--io <dir>] [--solver <path>]
                  [--parallel <n>] [--json]        # reserved, Step 19/27
mocassin collect <project.msl> [--dir <dir>] [--results <file.msr>]
                 [--external-states] [--json]      # Step 20
mocassin evaluate <project.msl> [--results <file.msr>] --query <name> [--where <expr>]
                  [-o <file>] [--format csv|json|parquet]   # Step 21
mocassin export <project.mocprj> -o <dir> [--format json|csv] [--json]  # Step 21
mocassin clean <project.msl> [--results <file.msr>] [--dir <dir>]
               [--older-than <days>]               # deletion, Step 28
```

- `<range>` accepts `1-100`, `1,3,5`, and mixed forms. Step 12 normalizes it to `all` or a sorted, merged
  form and records that normal form in `schema_info.job_selection` (§3.2). Zero, negative, descending, empty,
  and out-of-range selections are usage errors.
- `--json` prints a single JSON object to stdout; human progress goes to stderr. `validate --json` uses the
  dedicated `validation-v1` report in §5.6; the other commands use the reports in §5.7.
- `mocassin compile` is implemented in Step 12. Step 08 provides the pure `IModelCompiler` and the canonical
  `compiled-inspection-v1` JSON projection; `compile -o` writes that projection, and `compile --json` prints
  the `compile-v1` summary. Without `-o` the projection is not written.
- `build` selects a build template, a job template, and a parameterization in that order (§5.7.1). A project
  with no parameterizations uses the Step 09 implicit default. `--template` names a **build** template;
  `--job-template` names a job template; `--parameterization` overrides the recipe. A job-template ID passed
  to `--template` is a usage error with a hint.
- `build` refuses to overwrite an existing `.msl` unless `--force`; writes atomically; never appends.
- `build --preview` runs the same load/select/compile/validate/parameterize/expand path but writes no package
  and reports `build-v1` with `mode = "preview"`. `--preview` rejects `-o`, `--force`, and `--compact`.
- `--compact` is the size preset: Zstd quality `9` plus `VACUUM` before publication. It changes compressed
  bytes and file size, never payload content, signatures, or the canonical summary.
- `--parallel` accepts `1..256` and controls compilation and lattice generation concurrency; omitting it uses
  the processor count, and an explicit `--parallel 0` is a usage error rather than a shorthand for the
  default. Package writes are serialized in ascending `job_id` order, and output is independent of the degree.
- `simulate` launches `mocassin-sim` per job and captures stdout/stderr. It does not modify the `.msl`.
- `collect` reads `Job%05d/solver.json`, `run.mcs`, `prerun.mcs`, and logs, validates them, and writes the
  `.msr` results store. The default is `<package>.msr`; `--results` overrides it.
- `evaluate` requires a `.msr` store (or an explicit `--results` path) in addition to the package.
- `collect --external-states` stores large `.mcs` files next to `.msr` with relative paths/hashes.
- `clean` removes job folders and/or result rows only with an explicit selection and confirmation; `--dry-run`
  reports what would be removed. Step 12 implements **only** the dry run; deletion is Step 28. With `--json`,
  a missing `--dry-run` is a usage error that still emits `clean-v1` with `dryRun = false`, `removedCount = 0`,
  and `CLI008`; it never deletes.
- The CLI parser is `System.CommandLine` 3.0.0 (MIT, pinned, CLI-only; [ADR 0015](adr/0015-cli-parser-system-commandline.md)). It supplies command-line tokenization (after the CLI expands response files), arity and
  option applicability, `--name value`/`--name=value`, negative values such as `--jobs -1`, `--`, the help
  action, and the `[suggest]` directive; the CLI supplies the root `--version` action. The CLI owns the
  command graph, typed binding, `CLI001` wording, and exit-code policy. The library's parse-error exit code
  (1) is never exposed: a parse error is `CLI001` and exit `2`; help and root `--version` exit `0`, while
  `mocassin <command> --version` is `CLI001`/2. POSIX short-option bundling is disabled
  (`EnablePosixBundling = false`); short options are separate tokens.
- **Response files.** A token that starts with `@` and has at least one more character is a response-file
  reference; the remainder is the path, resolved against the process working directory, not the containing
  file. The CLI owns tokenization and recursive expansion; the library's response-file hook is disabled. `@`
  alone is a literal argument; `@@name` names a file `@name`; there is no escape. Files are UTF-8
  (optional BOM); invalid UTF-8 or NUL is `CLI001`/2. Lines are tokenized individually: Unicode whitespace
  separates, double quotes group and are removed, single quotes and backslashes are literal, `#` is a comment
  only as the first non-whitespace character of a line, and quotes do not span lines (an unterminated quote
  that begins a token discards it; one inside a token keeps the token without the quote). A `@file` token
  inside a file is expanded recursively. Limits: **1 MiB of raw bytes per file, 4 MiB of raw response-file
  bytes across the command line, depth 8, 10,000 tokens contributed by response files, 32 files**.
  `@file` before `--` expands; after `--` it is literal. `--input=@file` is literal; `--input @file` expands
  before arity checking, so a multi-token file in a single-value position is a usage error. `--json` inside a
  file is honored; a `--json` inside a file that failed to read cannot enable a report, and no report is
  emitted without a recognised command. Missing, unreadable,
  invalid, or oversized files are `CLI001`/2, not `CLI002`/3. The full contract is the step page §5.3 and
  `docs/reference/cli.md`.
- `completion` is **not** a Step 12 command. The owner decided on 2026-11-17 to remove it:
  `System.CommandLine` provides the `[suggest]` protocol for `dotnet-suggest`, not shell-script generation;
  the directive is accepted but is not a contract, and shell integration is deferred to Step 31 packaging.
  Step 12 ships no completion output.
- Reserved commands are defined in the command graph and exit `2` with a stable `CLI008` diagnostic and the
  owning step. They do not accept `--json` and emit no report.


### 5.2 `mocassin-sim` (Rust)

```text
mocassin-sim --db <project.msl> --job <id> --io <dir>
             [--stdout <file>] [--seed <hex32>] [--threads <n>]
             [--extension <path>] [--no-extensions] [--jump-log-max-ev <f64>]
             [--log-level <error|warn|info|debug|trace>]
             [--log-format human|json] [--dry-run] [--version]
```

- `--io` is created if missing. The solver writes `run.mcs` (and `prerun.mcs` for KMC) there.
- `--stdout` redirects the human statistics dump; otherwise it goes to stdout.
- The solver never writes to the `.msl` or `.msr`; `collect` writes the `.msr`.
- Exit codes are defined in [`01-target-architecture.md`](01-target-architecture.md#73-error-handling-and-exit-codes).

### 5.3 Job folder and `solver.json`

The solver writes one folder per job:

```text
Job%05d/
├── run.mcs               # final/main state; required on success
├── prerun.mcs            # KMC pre-run state; optional
├── stdout.log            # human-readable statistics dump; optional
├── stderr.log            # structured logs and errors; optional
├── solver.json           # machine-readable summary; required on completion/failure
└── mmcfe.db              # optional MMCFE log database
```

The naming is similar to the original layout so existing scripts can be adapted, but the contents are **not**
legacy-compatible. No legacy file is read or written.

`solver.json` is a JSON object with `format = "mocassin.solver"` and `formatVersion = 1`:

| Field | Type | Required | Meaning |
| --- | --- | --- | --- |
| `format` | string | yes | `"mocassin.solver"` |
| `formatVersion` | uint32 | yes | `1` |
| `jobId` | uint32 | yes | Package job id |
| `exitCode` | int32 | yes | Solver exit code |
| `abortReason` | string | yes | Same vocabulary as `.mcs` `metadata.abortReason` |
| `wallTimeS` | f64 | yes | Wall-clock run time |
| `simulatedTimeS` | f64 | yes | KMC time; 0 for MMC |
| `state` | map | yes | `{ "run": "run.mcs", "prerun": "prerun.mcs" | null }`, relative paths |
| `seedHash` | string | yes | SHA-256 of the job seed, not the seed |
| `counters` | map | no | `cycles`, `mcs`, `success`, `rejected`, `blocked` |
| `error` | string? | no | Human-readable failure message |
| `createdUtc` | string | yes | ISO-8601 UTC timestamp |

- The solver writes `solver.json` atomically: temp file in the job folder, flush/fsync, rename.
- On a hard crash, `solver.json` may be absent. `collect` synthesizes a failed result from the process exit
  code and the log files.
- `state` paths are relative to the job folder and must not contain `..`.
- Unknown fields are ignored by readers; a future major version is rejected.

### 5.4 MMCFE log database (`mmcfe.db`)

MMCFE runs write a resumable SQLite database in the job folder. It is not the legacy `mmcfelog.db`.

```sql
PRAGMA journal_mode = DELETE;
PRAGMA foreign_keys = ON;

CREATE TABLE schema_info (
    key   TEXT PRIMARY KEY,
    value TEXT NOT NULL
);
-- rows: ('format_version','1'), ('app_version','x.y.z'), ('generator','mocassin-sim x.y.z')

CREATE TABLE mmcfe_runs (
    run_id          INTEGER PRIMARY KEY,
    job_id          INTEGER NOT NULL,
    variant         TEXT NOT NULL DEFAULT 'total',  -- total or a defect/species key
    alpha_count     INTEGER NOT NULL,
    alpha_min       REAL NOT NULL,
    alpha_max       REAL NOT NULL,
    cycles_per_alpha INTEGER NOT NULL,
    relax_cycles    INTEGER NOT NULL,
    seed_hash       TEXT NOT NULL,
    status          TEXT NOT NULL CHECK (status IN ('running','completed','failed','cancelled')),
    started_utc     TEXT NOT NULL,
    completed_utc   TEXT,
    error_message   TEXT,
    UNIQUE (job_id, variant)
);

CREATE TABLE mmcfe_alpha (
    run_id          INTEGER NOT NULL REFERENCES mmcfe_runs(run_id),
    alpha_index     INTEGER NOT NULL,
    alpha           REAL NOT NULL,
    cycles          INTEGER NOT NULL,
    accepted        INTEGER NOT NULL,
    energy_min_ev   REAL NOT NULL,
    energy_max_ev   REAL NOT NULL,
    bin_count       INTEGER NOT NULL,
    histogram       BLOB NOT NULL,          -- MessagePack u64[]
    log_dos         BLOB NOT NULL,          -- MessagePack f64[]
    underflow       INTEGER NOT NULL,
    overflow        INTEGER NOT NULL,
    updated_utc     TEXT NOT NULL,
    PRIMARY KEY (run_id, alpha_index)
);

CREATE TABLE mmcfe_resume (
    run_id          INTEGER PRIMARY KEY REFERENCES mmcfe_runs(run_id),
    state           BLOB NOT NULL,          -- MessagePack resume payload
    state_sha256    TEXT NOT NULL,
    updated_utc     TEXT NOT NULL
);
```

- `variant` identifies the total or a per-defect/species free-energy curve; it is unique per `(job_id, variant)`.
- `histogram` and `log_dos` are MessagePack arrays of `u64`/`f64`; `bin_count` must match their lengths.
- `mmcfe_resume` stores the solver-owned resume payload, not a `.mcs` file.
- The database is written by one solver process per job folder; resume updates are transactional.
- `collect` and the evaluation API accept the current and previous major schema version (ADR 0013).

### 5.5 Machine-readable events

Both CLIs support JSON Lines progress events on stderr when `--json`/`--log-format json` is active. Every
event is one JSON object on one line with `schemaVersion = 1` and a stable `event` name:

```json
{"schemaVersion":1,"event":"build.started","total":6}
{"schemaVersion":1,"event":"build.stage","stage":"generate","status":"started"}
{"schemaVersion":1,"event":"build.progress","stage":"generate","completed":120,"total":1000,"current":"T773/x001/2","etaS":41.2}
{"schemaVersion":1,"event":"build.finished","exitCode":0,"wallTimeS":12.4,"published":true}
{"schemaVersion":1,"event":"sim.progress","jobId":42,"mcs":1200000,"target":2000000,"rate":7.4e6}
{"schemaVersion":1,"event":"sim.finished","jobId":42,"exitCode":0,"abortReason":"completed","wallTimeS":12.4}
```

| Event | Required fields | Meaning |
| --- | --- | --- |
| `build.started` | `total` | Job generation began; `total` is the selected job count, known after expansion |
| `build.stage` | `stage`, `status` | A pipeline stage started or completed; `ms` is optional on completion |
| `build.progress` | `completed`, `total` | Build progress; `stage`, `current`, and `etaS` are optional |
| `build.finished` | `exitCode`, `wallTimeS` | Build ended; `published` is optional; emitted once for every recognised run |
| `sim.started` | `jobId` | Solver job began |
| `sim.progress` | `jobId`, `mcs`, `target` | Solver progress; `rate` optional |
| `sim.finished` | `jobId`, `exitCode`, `abortReason`, `wallTimeS` | Solver job ended |
| `collect.started` | `total` | Collection began |
| `collect.progress` | `completed`, `total`, `jobId` | Collection progress |
| `collect.finished` | `collected`, `skipped`, `failed` | Collection ended |
| `diagnostic` | `code`, `severity`, `message` | Structured diagnostic; `path` optional |
| `log` | `level`, `message` | Reserved for a forwarded human log; Step 12 has no producer, and the event fixture does not include it. |

- `build.started` is emitted after job expansion, when the selected total is known. The `build.stage` events
  for load/select/validate/compile/validateBuild/parameterize/expand therefore precede it; consumers must not
  require `build.started` to be the first line. `build.finished` is emitted exactly once at the end,
  including for a failure before `build.started` or for cancellation.
- `build.stage` was added in Step 12; it is an additive event within `schemaVersion = 1`. The manifest event
  descriptor records required fields only (`stage`, `status`); the optional `ms`, `stage`, `current`, `etaS`,
  `published`, and `path` fields are documented here and asserted by the CLI event tests. Step 12 does not add
  an `optionalFields` manifest key or change the generated `events-v1` schema shape.
- `build.progress.completed` is monotonic within one stream; `total` is the selected job count. Progress is
  throttled by the writer of the stream, so consumers must not expect one event per job.
- `--json` makes stderr JSON Lines only; human stage/progress lines are suppressed whether or not `--quiet`
  is used. `--quiet` additionally suppresses `build.stage` and `build.progress`; `log` is reserved and has no
  Step 12 producer. `build.started`,
  `build.finished`, and `diagnostic` remain.
- Consumers ignore unknown events and unknown fields.
- Ordering is guaranteed within one stream; `jobId` is present for per-job events.
- stdout remains reserved for the human statistics dump or a single final JSON object.
- Event names and fields are part of the contract manifest and are covered by cross-language tests.


### 5.6 Validation report (`validation-v1`)

`mocassin validate --json` prints one object with `schemaVersion = 1`. The report is the `validation-v1`
contract; it is not the same shape as the generic command result used by `build`/`simulate`.

```json
{
  "schemaVersion": 1,
  "kind": "validation",
  "projectId": "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e80",
  "modelHash": "9f2c…",
  "documentHash": "4a71…",
  "valid": false,
  "exitCode": 1,
  "summary": { "errors": 2, "warnings": 1, "infos": 0 },
  "diagnostics": [
    {
      "code": "TRN012",
      "severity": "error",
      "message": "Kinetic path position 1 must be unstable.",
      "path": { "domain": "transitions", "objectId": "018f4c9a-…", "property": "referencePath[1]" },
      "hint": "Mark the site as unstable or choose a different reference site.",
      "related": []
    }
  ],
  "statistics": null
}
```

| Field | Type | Required | Meaning |
| --- | --- | --- | --- |
| `schemaVersion` | integer | yes | `1` for this contract. |
| `kind` | string | yes | Always `validation`. |
| `projectId` | UUID or null | yes | Project metadata identity; `null` when the document could not be loaded. |
| `modelHash` | string or null | yes | Canonical model hash from Step 01, lowercase hexadecimal without a prefix; `null` when the document could not be loaded. |
| `documentHash` | string or null | yes | Canonical document hash from Step 01, lowercase hexadecimal without a prefix; `null` when the document could not be loaded. |
| `valid` | boolean | yes | True only when no error remains under the selected policy. |
| `exitCode` | integer | yes | `0`, `1`, `2`, or `3` as defined by the `validate` command. |
| `summary` | object | yes | Raw `errors`, `warnings`, and `infos` counts. |
| `diagnostics` | array | yes | Sorted diagnostics; may be empty. |
| `statistics` | object or null | yes | Non-null only when `--stats` was requested and statistics could be computed; otherwise `null`. |

- `diagnostics[]` fields: `code`, `severity`, `message`, optional `path` (`domain`, `objectId`, optional
  `property`), optional `hint`, and optional `related` paths. `severity` is `error`, `warning`, or `info`.
- Diagnostics are ordered by severity, code, domain, object ID, property, and message. Exact duplicate
  `(code, path, message)` triples are removed.
- `valid` is policy-adjusted; `--warnings-as-errors` makes a warning-only result `valid: false` with
  `exitCode: 1`. `summary` still reports those items as warnings.
- `statistics` uses camelCase fields matching `ModelStatistics` exactly: `particleCount`, `particleSetCount`,
  `referenceSiteCount`, `p1PositionCount`, `stablePositionCount`, `unstablePositionCount`,
  `kineticTransitionCount`, `metropolisTransitionCount`, `clusterCount`, `estimatedPairInteractionCount`,
  `estimatedUniqueClusterPermutations`, `estimatedRuleCount`, `estimatedMemoryBytes`, `cellCountA`,
  `cellCountB`, `cellCountC`, and `estimatedPositions`. Long counts are JSON numbers.
- Statistics availability is independent of `valid`: the object is non-null when statistics could be computed,
  which excludes only a missing `--stats`, a load failure, a `Model`-stage error, or a failed preview. A model
  invalid only because of feasibility/budget errors or promoted warnings can therefore carry statistics;
  `--stats` never changes diagnostics or `valid`.
- `--json` prints exactly one report for every invocation, including usage and load failures. Pre-load failures
  set the three identity fields to `null`; a usage error has `exitCode: 2`, an unreadable/missing file has `3`,
  and unparseable or schema-invalid content has `1` with the store's `PROJ` diagnostic. Human output is not part
  of this JSON contract. It is stable for tests but not for machine consumers.
- The contract is registered in `contracts/manifest.json` under a top-level `reports` object as a descriptor
  `{ "id": "validation", "title": "Validation report", "schemaVersion": 1, "fields": [...] }` (not as an entry
  in `formats`, whose eight ids are fixed). `tools/contracts/generate.py` validates the `reports` section and
  emits `contracts/generated/schema/validation-v1.schema.json`, matching the existing
  `<id>-v<version>.schema.json` convention. The generated `ValidationReportVersion` constant lives with the
  other generated version constants in `Mocassin.Contracts`; the schema is checked against real CLI output and
  the generated artifact is freshness-checked by `verify.sh contracts`.

### 5.7 Command reports (`build-v1` and siblings)

`mocassin build`, `new`, `compile`, `inspect`, `verify`, `doctor`, and `clean --dry-run` print exactly one
JSON object with `--json`. The objects are contract reports, not ad-hoc DTO dumps. Each is registered in
`contracts/manifest.json` under the top-level `reports` object (like `validation-v1`) and generated as
`contracts/generated/schema/<id>-v<version>.schema.json`:

| Report id | `kind` | Command |
| --- | --- | --- |
| `build` | `build` | `build`, `build --preview` |
| `new-project` | `newProject` | `new` |
| `compile` | `compile` | `compile` |
| `inspect` | `inspect` | `inspect` |
| `verify` | `verify` | `verify` |
| `doctor` | `doctor` | `doctor` |
| `clean` | `clean` | `clean --dry-run` |

`validation-v1` (§5.6) remains separate. The reports are serialized to a buffer and written once, so a
failure cannot leave a truncated object. Numeric formatting is invariant shortest round-trip; JSON is
camelCase with two-space indentation. `tools/contracts/generate.py` gains the matching `REPORT_CONSTANTS`
entries and the generated constants live with the other version constants in `Mocassin.Contracts`.

#### 5.7.1 Build selection and job selection

| Input | Meaning |
| --- | --- |
| `--template <buildTemplateId>` | A project `BuildTemplate` (job template + parameterization + output name). |
| `--job-template <jobTemplateId>` | A job template; overrides the build template’s job template. |
| `--parameterization <parameterizationId>` | A parameterization template; overrides the build template’s. |

Resolution order:

1. `--template` selects a build template. A job-template ID passed here is a usage error with a hint; an
   unknown ID is a `PROJ010` reference error.
2. Without `--template`, exactly one project build template is selected automatically; several without a
   selection are a usage error listing IDs and names.
3. The job template comes from `--job-template`, then the build template, then exactly one project job
   template, otherwise a usage error. A project with no job template cannot build.
4. The parameterization comes from `--parameterization`, then the build template, then exactly one project
   parameterization, otherwise the implicit Step 09 default when the project has none, otherwise a usage
   error.
5. Explicit overrides are allowed for experimentation. The report records the **effective** IDs, not the
   authored recipe.

`--jobs` is applied after the complete template expansion. The grammar is `all` or a comma-separated list of
`N`/`A-B` items; IDs are 1-based full-expansion IDs. The stored and reported expression is the canonical
normal form: `all` when every job is selected, otherwise sorted, deduplicated, merged ranges with no spaces
(`1-3,5,7-9`). Zero, negative, descending, empty, and out-of-range selections are usage errors. A partial build
keeps full-expansion IDs, may leave gaps, and never renumbers the unselected jobs.

#### 5.7.2 Shared envelope

```json
{
  "schemaVersion": 1,
  "kind": "build",
  "ok": false,
  "exitCode": 1,
  "summary": { "errors": 1, "warnings": 2, "infos": 0 },
  "diagnostics": [
    {
      "code": "PRM001",
      "severity": "error",
      "message": "The template targets a different model signature.",
      "path": { "domain": "parameterization", "objectId": "…", "property": "modelSignature" },
      "hint": "Rebase the template against the current compiled model.",
      "related": []
    }
  ]
}
```

| Field | Type | Required | Meaning |
| --- | --- | --- | --- |
| `schemaVersion` | integer | yes | `1` for every report in this family. |
| `kind` | string enum | yes | Report kind from the table above. |
| `ok` | boolean | yes | `exitCode == 0`. |
| `exitCode` | integer | yes | The process exit code documented in §5.7.6. |
| `summary` | object | yes | Raw `errors`, `warnings`, and `infos` counts after deduplication. |
| `diagnostics` | array | yes | The `validation-v1` diagnostic element shape; may be empty. |

Diagnostics are ordered by severity, code, domain, object ID, property, and message; exact
`(code, path, message)` duplicates are removed. Domain codes (`PROJ`, `STR`, `TRN`, `ENM`, `SIM`, `CMP`,
`PRM`, `JOB`, `PKG`) keep their meaning. CLI-only failures use `CLI001`–`CLI009`.

`validate` and each report-backed command emit one report for every outcome, including a usage error before
any work. `--help`, `--version`, and the reserved commands do not accept `--json` and emit no report. The
shared envelope is always populated; command-specific fields are present but `null`, `0`, `false`, `[]`, or
`"skipped"`. `required` in the generated schema means “present”, not “non-null”: report
descriptors mark payload objects, strings, and identities nullable, and nested objects are present with null
fields. This is why a usage error with `--json` validates against the same schema as a successful run.
`clean --json` without `--dry-run` is a usage error that still uses `clean-v1` with `dryRun = false`,
`removedCount = 0`, and `CLI008`.

`CLI002`/3 covers an input/output path failure, including a directory passed where a file is required, a
missing parent directory for `build -o`/`compile -o`, or a missing/unreadable input; the path is checked before
the command opens a package. `new` intentionally creates a missing output directory and exits 0. `CLI009`/1 means the input cannot be inspected as a package. `inspect` is tolerant of non-fatal
payload and signature defects, so those are reported by `verify`; only a fatal structure failure is
`CLI009`.

#### 5.7.3 `build-v1`

`build-v1` replaces the Step 10 interim `build-preview-v1` and covers both full builds and previews:

| Field | Type | Required | Meaning |
| --- | --- | --- | --- |
| `mode` | string enum `build`, `preview` | yes | Full build or no-write preview. |
| `projectId` | UUID or null | yes | Project identity. |
| `modelHash` | string or null | yes | Step 01 canonical model hash. |
| `documentHash` | string or null | yes | Step 01 canonical document hash. |
| `modelSignature` | string or null | yes | Step 08 compiled-model signature. |
| `parameterizationSignature` | string or null | yes | Step 09 `models.signature`; null when parameterization did not run. |
| `selection` | object or null | yes | `buildTemplate` (`id`, `name`, nullable), `jobTemplate` (`id`, `name`), `parameterization` (`id`, `name`, `implicitDefault`), `outputName` (string or null); null before selection or on a usage error. |
| `jobSelection` | object or null | yes | `expression`, `expandedCount`, `selectedCount`; null before expansion, and `expression` is null on a usage error. |
| `timings` | array | yes | `[{ "stage": string, "ms": number }]` in pipeline order: `load`, `select`, `validate`, `compile`, `validateBuild`, `parameterize`, `expand`, `preview`, `generate`, `publish`, `verify`; omitted stages are absent. `preview` is produced inside the pipeline from `BuildPreview.Elapsed`; `verify` is caller-composed; neither is a pipeline `BuildStage`. |
| `package` | object or null | yes | `path`, `sha256`, `sizeBytes`, `jobCount`, `modelCount`, `latticeCount`, `latticeReuseFactor`, `published`; null in preview and before publication. |
| `preview` | object or null | yes | Direct projection of `BuildResult.Preview` (`jobTemplateId`/`jobTemplateName` of the effective **job** template, `totalJobs`, `collections[]`); null in a full build and before preview evaluation. |
| `verification` | string enum `passed`, `failed`, `skipped` | yes | Post-publication verification result. |

- `ok`/`exitCode` follow the build table: 0 success, 1 validation/lattice/package-invalid, 2 usage, 3 I/O,
  4 compilation/parameterization, 6 cancelled, 10 internal. Exit 5 is reserved for future multi-job commands
  and is never emitted by `build`.
- In preview mode `package` is null, `verification` is `skipped`, and no file is created. In build mode
  `preview` is null.
- A cancellation after publication leaves `package.published = true`, `verification = "skipped"`, and
  `exitCode = 6`.
- `build-v1` carries no package-size estimate. `package.sizeBytes` is the authoritative measured size after a
  build; a preview is size-neutral. A future estimate needs its own formula and contract decision.

#### 5.7.4 Other report payloads

| Report | Payload fields |
| --- | --- |
| `new-project-v1` | `projectPath`, `projectId`, `template` (`blank`/`ceria`) |
| `compile-v1` | `projectId`, `modelHash`, `documentHash`, `modelSignature`, `compilerVersion`, `outputPath` (nullable), `outputSha256` (nullable), `counts` (particle, position, site, environment, pair-table, cluster-table, direction, rule, and simulation counts), `cache` (`hits`, `misses`, `capacity`), `stages[]` (`name`, `ms`) with the ten stages `resolve`, `structure`, `environments`, `pairs`, `clusters`, `rules`, `encoding`, `indexing`, `signing`, `report`; `report` always runs |
| `inspect-v1` | `package` with the Step 11 `PackageInspection` fields: `formatVersion`, `appVersion`, `createdUtc`, `projectId`, `generator`, `jobSelection`, `symmetryContractVersion`, `spaceGroupDataVersion`, `spaceGroupDataSha256`, `models[]` (`signature`, `modelSignature`, `payloadFormatVersion`, `payloadBytes`), `latticeCount`, `jobCount`, `collections[]` (`kind`, `collectionName`, `configName`, `jobCount`), `minTemperatureK`, `maxTemperatureK`, `minElectricFieldVm`, `maxElectricFieldVm`, `sizeBytes`, `latticeReuseFactor` |
| `verify-v1` | `valid`, `checks[]` (`code`, `passed`, `message`), `package` (the same object as `inspect-v1`). The `PKG` findings are the shared envelope’s `diagnostics[]`; there is no second payload-local diagnostics array. |
| `doctor-v1` | `healthy`, `checks[]` (`name`, `status` enum `ok`/`warn`/`fail`, `message`, optional `detail`), `environment` (`framework`, `os`, `architecture`, `sqliteVersion`, `catalogVersion`, `catalogSha256`, `solverPath`, `solverVersion`). Checks run in the order `runtime`, `sqlite`, `write-permission`, `disk-space`, `symmetry-catalog`, `project-templates`, `solver`; free space below 100 MiB is `warn`, and a throwing check is recorded as `fail`. |
| `clean-v1` | `dryRun` (the requested mode), `packagePath`, `directory`, `jobSelection`, `jobCount`, `folders[]` (`path`, `jobId`, `lastWriteUtc`, `sizeBytes`), `totalBytes`, `olderThanDays` (nullable), `removedCount`. `--dry-run` is the only supported mode (`dryRun = true`, `removedCount = 0`). Without it, the report still carries the read-only folder listing with `dryRun = false`, `removedCount = 0`, `CLI008`, and exit 2. The reserved `--results` option emits a minimal report with no package inspection and an empty listing, also `CLI008`/2. Neither path deletes anything. |

`compiled-inspection-v1` and `package-summary-v1` remain canonical inspection projections rather than new
CLI report ids: `compile -o` writes the former, and `inspect-v1`/`verify-v1` embed the latter’s data.

#### 5.7.5 Versioning and fixtures

- Every report starts at `schemaVersion = 1`; additive fields keep the version. A breaking change increments
  the report’s own version and adds a new generated schema.
- The contract manifest’s `manifestVersion` stays `1`: `tools/contracts/generate.py` pins it, and the new
  report and event entries are additive. Each report carries its own `schemaVersion = 1`.
- `build-preview-v1` is retired. It was never a manifest-owned report; the Step 10 tests move to
  `build-v1` with `mode = "preview"`.
- Real CLI output is validated against the generated schema in PR-fast tests, and
  `tests/contracts/events-v1.jsonl` is validated against the `events` contract.
- The `reports` descriptors use `unknownFields = "ignore"`, matching `validation-v1`.

#### 5.7.6 Exit codes

| Exit | Meaning |
| ---: | --- |
| 0 | Success (warnings allowed unless promoted) |
| 1 | Validation, model, invalid package, or unhealthy `doctor` |
| 2 | Usage/argument error, selection ambiguity, malformed/out-of-range `--jobs`, reserved command/option |
| 3 | I/O error, missing/unreadable input, existing target without `--force` |
| 4 | Compilation or parameterization error, promoted parameterization warning |
| 5 | Partial success — reserved for future multi-job commands; not emitted by `build` |
| 6 | Cancelled by the user |
| 10 | Unexpected internal error |

`validate` keeps its Step 07 0/1/2/3 contract. `build --preview` uses the build table above, replacing the
Step 10 interim preview mapping. `validate` and every command that accepts `--json` print exactly one report
object, including usage errors, once the command name is recognised; `--help`, `--version`, and reserved
commands do not accept `--json` and emit no report. The parser's default parse-error exit code (1) is never
exposed: the CLI maps it to `CLI001` and exit `2`. Response-file errors (missing, unreadable, invalid, or
oversized `@file`) are the same usage class: `CLI001`/2, not `CLI002`/3.

## 6. Contract testing

- `tests/contracts/project-minimal.mocprj` and `project-ceria.mocprj` are canonical fixtures.
- `tests/contracts/model-v1.msgpack.zst` is written by .NET and read by Rust; both dump a canonical JSON
  representation and compare.
- `tests/contracts/state-v1.mcs` is a MessagePack document written by Rust and read by .NET and Python; each
  reader emits a canonical JSON summary of metadata, counters, tracker counts, and histogram totals.
- `tests/contracts/signature-vectors.json` contains canonical projection bytes and expected SHA-256 digests
  for the model, parameterization, and lattice types.
- `tests/contracts/package-v1.msl` and `package-v1.msr` are produced by the real writers and read by all
  readers; previous-major fixtures are retained for the compatibility window.
- `tests/contracts/job-folder/` contains a real solver job folder with `solver.json`, `run.mcs`, `prerun.mcs`,
  logs, and an optional `mmcfe.db`; .NET and Python parse them.
- `tests/contracts/events-v1.jsonl` is accepted by the CLI schema validator; the Step 26 GUI event parser
  will accept it when that parser exists. Step 12 adds a real build sequence (started, stage, progress,
  diagnostic, finished).
- The `build-v1`, `new-project-v1`, `compile-v1`, `inspect-v1`, `verify-v1`, `doctor-v1`, and `clean-v1`
  reports are validated against their generated schemas using real CLI output; `validation-v1` keeps its
  existing schema test. Usage-error reports are validated too.
- A schema-change PR must update the fixture, the version field, this document, the contract manifest, and the
  relevant ADR.
