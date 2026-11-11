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
- numbers in invariant, shortest round-trip form; no NaN/Inf; normalize `-0.0` to `0.0`;
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
  Fractions, supercell limits, same-site application, truncation, charge-compensation counts, and the lattice
  preview are Step 10 (`JOB004`–`JOB015`).
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

```json
{
  "id": "…",
  "name": "Ceria parameters",
  "modelSignature": "sha256:…",
  "createdUtc": "…",
  "stablePairSets": [
    {
      "interactionId": "…",
      "entries": [
        { "centerParticleId": "…", "partnerParticleId": "…", "energyEv": 0.0 }
      ]
    }
  ],
  "unstablePairSets": [],
  "groupSets": [
    {
      "interactionId": "…",
      "entries": [
        {
          "centerParticleId": "…",
          "surroundingParticleIds": ["…", "…"],
          "energyEv": 0.48
        }
      ]
    }
  ],
  "siteEnergyOverrides": [
    { "siteId": "…", "particleId": "…", "energyEv": 0.0 }
  ],
  "transitionRuleSets": [
    {
      "transitionId": "…",
      "rules": [
        { "ruleId": "…", "frequencyHz": 1.0e13 }
      ]
    }
  ]
}
```

- `modelSignature` is a SHA-256 hash of the canonical compiled model (structure, transitions, environments,
  interaction geometry, indices). A parameterization whose signature does not match the current model cannot
  be used to build jobs; the GUI offers "rebase" and shows which sets changed.
- Rule IDs are stable strings derived from the rule's state triplet and direction, so recompilation can match
  existing frequencies.
- The original could not edit site energies through a parameterization template. The new format includes
  `siteEnergyOverrides` as an intentional improvement; the GUI marks them as overrides.
- Chiral pair sets share one interaction identity and one energy table. Each direction has its own stable
  geometry ID for display and migration, and the shared `EnergyId` is the canonical pair-set identity used by
  the compiled `object_id` and the parameterization template. A template stores one value per `EnergyId`, so
  two independent values for a linked chiral pair are unrepresentable.

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
          "overrides": {},
          "lattice": { "a": 10, "b": 10, "c": 10 },
          "dopings": [{ "dopingId": "…", "fraction": 0.01 }],
          "extensions": [
            { "id": "mmcfe", "config": { "alphaCount": 50, "relaxCycles": 20000000 } }
          ]
        }
      ]
    }
  ]
}
```

- `baseSettings` and `config.overrides` are partial; `null`/absent means inherit. Explicit `null` cannot
  mean "set to zero"; use the numeric value for that.
- `jobCountPerConfig` creates duplicate jobs with different derived seeds.
- Selection optimizers exclude a particle at a reference site from the KMC starting-point pool.
- `extensions` are structured, versioned configuration objects for optional solver routines (e.g. MMCFE);
  they inherit/override like other settings. A raw `instruction` string remains available for advanced/legacy
  workflows but is not the preferred configuration path.

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
--       ('job_selection','all' | '1-100,250')

CREATE TABLE models (
    model_id       INTEGER PRIMARY KEY,
    signature      TEXT    NOT NULL UNIQUE,   -- SHA-256 canonical projection (ADR 0010)
    format_version INTEGER NOT NULL,
    payload        BLOB    NOT NULL            -- Zstd(MessagePack(ParameterizedModel))
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
    norm_factor       REAL,
    base_frequency_hz REAL,
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
  (collection index, config index, job index) and never reused within a package.
- The writer always sets the denormalized columns from the resolved job payload; SQL defaults (including
  `write_out_count DEFAULT 100`) are fallbacks only.
- The published package has no `results` table. Results live in `.msr` (section 3.6).
- The database uses `application_id`/`user_version` pragmas in addition to `schema_info` for tooling.
- The solver opens the package read-only (`SQLITE_OPEN_READONLY`) and never writes it.
- `schema_info` includes `format_version`, `app_version`, `created_utc`, `project_id`, `generator`, and
  `job_selection`.

### 3.3 Compiled model payload

MessagePack map with snake_case keys. Top-level:

| Key | Type | Meaning |
| --- | --- | --- |
| `format_version` | u32 | Payload version |
| `signature` | string | SHA-256 of canonical parameterized model (geometry + energy/frequency values) |
| `symmetry_contract_version` | string | Symmetry contract version, e.g. `symmetry-1` |
| `space_group_data_version` | string | Catalog data version, e.g. `spglib-2.8.0/catalog-1` |
| `space_group_data_sha256` | string | SHA-256 of the canonical catalog payload |
| `particle` | object | `charges`, `symbols`, `is_vacancy`, count; index 0 is void |
| `structure` | object | cell vectors, P1 positions, site definitions, environments, interaction range |
| `energy` | object | pair tables, cluster tables, defect background |
| `transitions` | object | jump collections, directions, rules, mapping tables |
| `simulations` | array | simulation encodings (active collections, settings defaults, trackers) |

- The model-signature projection includes the symmetry contract version and the catalog data version and
  checksum, so a rule or catalog change invalidates parameterizations.

`structure` fields:

| Key | Type | Meaning |
| --- | --- | --- |
| `cell_vectors_ang` | `[[f64;3];3]` | Cartesian a/b/c vectors in Å |
| `positions` | array | `{vector4:[i32;4], site_index:u32, stable:bool, particle_ids:u8[]}` in ascending `P` order; `vector4` is `[cellA, cellB, cellC, P]`, with base positions `[0,0,0,P]` |
| `position_count` | u32 | Length of `positions` |
| `site_count` | u32 | Number of user reference sites |
| `environments` | array | Per position: `selection_particle_mask:u64`, pair interactions, cluster interactions, `update_particle_ids` |
| `interaction_range` | `[i32;4]` | MMC interaction cube |
| `static_trackers_per_cell` | u32 | |
| `global_tracker_count` | u32 | |

- `position_count = Pcount`.
- `site_index` identifies the reference site; it is not the P index.
- The linear index is
  `P + cellC*Pcount + cellB*Pcount*Ccount + cellA*Pcount*Ccount*Bcount`.
- The index is a signed 32-bit integer. The total `Acount*Bcount*Ccount*Pcount` is validated in 64-bit and
  must not exceed `2,147,483,647`; larger lattices are rejected before allocation.
- Negative cell offsets are relative-path coordinates and are not linearized directly.

`energy` fields:

| Key | Type | Meaning |
| --- | --- | --- |
| `pair_tables` | array | `{object_id, energies:[f64; n_particles*n_particles]}` |
| `cluster_tables` | array | `{object_id, occupation_codes:[u64], energies:[f64], particle_table_mapping:[u8; n_particles]}` |
| `defect_background` | array | `[position][particle]` site energy in eV |

- Pair tables are in canonical shell order: `(distanceBucket, targetP, cellA, cellB, cellC)`, then site
  indices. The stable geometry ID is a content hash used for parameterization matching, not table order; its
  projection includes the center and partner P1 position indices (`centerP`, `partnerP`) as well as the site
  IDs, periodic offset, and distance bucket. The compiled structure environments carry each shell's geometry
  ID, shared energy ID, center/partner P1 indices, distance bucket, representative offset, equivalent-offset
  count, and optional chiral partner geometry ID.
- Chiral partners share one `object_id` (the shared `EnergyId`) and one energy table. Each direction keeps its
  own `GeometryId` and records the inverse direction's `ChiralPartnerGeometryId`.
- Cluster occupation codes use 6 bits per site, site 0 in the least significant bits, up to 9 sites; unused
  high bits are zero. Codes are canonical under the induced site-permutation group.
- Pair shells use the `1 fm` distance bucket. Candidate enumeration uses a `cutoff + 1 fm` margin and includes
  shells with `bucket <= cutoffBucket`.
- Minimum-image and radial-search results always carry the cell offset; no implicit wrapping is allowed.

`transitions` fields:

| Key | Type | Meaning |
| --- | --- | --- |
| `jump_collections` | array | `{object_id, mobile_particle_mask:u64, rule_ids:[u32], direction_ids:[u32]}` |
| `jump_directions` | array | `{object_id, position_id, collection_id, path:[i32;4], movement:[[f64;3]], field_factor, length}` |
| `jump_rules` | array | `{rule_id, state0:u64, state1:u64, state2:u64, frequency_factor, field_factor, static_correction, tracker_order:[u8;8]}` |
| `jump_count` | array | `[position][particle] -> u32` |
| `jump_mapping` | array | `[position][particle][relative] -> direction index` |
| `static_tracker_mapping` | array | `[position][particle] -> tracker offset` |
| `global_tracker_mapping` | array | `[collection][particle] -> tracker index` |

`simulations` fields:

| Key | Type | Meaning |
| --- | --- | --- |
| `id` | string | Project simulation ID |
| `kind` | string | `kmc` / `mmc` |
| `jump_collection_ids` | `u32[]` | Active collections |
| `default_settings` | object | Temperature, MCSP, write-out count, pre-run, limits, field, normalization, MMC samples, flags |
| `tracker_models` | array | Global/static tracker definitions and indices |

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
| `electric_field_direction` | `[f64;3]` | Normalized Cartesian direction |
| `normalization_energy_ev` | f64? | `null` = no normalization; `0.0` canonicalizes to `null` |
| `max_attempt_frequency_hz` | f64? | `null` = maximum from the transition rules |
| `break_tolerance` | f64? | MMC only; 0 disables the break check |
| `break_sample_length_mcsp` | u64? | MMC only |
| `break_sample_interval_mcsp` | u64? | MMC only |
| `result_sample_mcsp` | u64? | MMC only |
| `flags` | u32 | `SolverJobFlags` word: bit 0 `noSaving`, bit 1 `noJumpLogging`, bit 2 `useFastExp`, bit 3 `deterministic`, bit 4 `useDualDofCorrection`, bit 5 `usePrerun`, bit 6 `kmcSimulation`, bit 7 `mmcSimulation` |
| `selection_optimizers` | array | `{position_id:u32, particle_id:u8}` |
| `extensions` | array | `{id: string, config: map}` for optional solver routines |
| `instruction` | string? | Advanced/legacy free-text instruction |
| `doping_info` | string | Human-readable `[index@value]` summary |

The lattice itself is stored once in the `lattices` table and referenced by the job:

| Key | Type | Meaning |
| --- | --- | --- |
| `format_version` | u32 | Payload version |
| `lattice_size` | `[u32;3]` | Supercell a,b,c |
| `position_count` | u32 | P1 positions per unit cell |
| `lattice` | `u8[]` | Initial particle index per linearized position; length = a*b*c*position_count |
| `doping_info` | string | Human-readable applied-doping summary |

The solver derives the site-energy background from the compiled model's `[position][particle]` table repeated
over the supercell. Per-job background overrides are not part of version 1; a future extension can add a
sparse `background_overrides` map without changing the base layout.

### 3.5 Publication and read-only guarantees

The `.msl` is immutable after a successful build:

1. Build in a temporary file in the target directory. WAL may be used during the build.
2. Run `PRAGMA wal_checkpoint(TRUNCATE)`, switch to `PRAGMA journal_mode=DELETE`, and close the database.
3. `fsync` the file, then atomically rename it to the final `.msl` path.
4. Never modify the published file. `collect` writes `.msr`; it does not update `.msl`.

A published `.msl` must be readable without `-wal` or `-shm` files. This makes it safe on read-only media and
shared HPC filesystems. The solver and evaluation readers open it with
`SQLITE_OPEN_READONLY`, `query_only=ON`, and `foreign_keys=ON`. Readers must not rely on WAL semantics.

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
mocassin new <name> [-o <dir>] [--template ceria|blank]
mocassin validate <project.mocprj> [--json] [--warnings-as-errors] [--stats]
mocassin compile <project.mocprj> -o <compiled-model.json> [--json]
mocassin build <project.mocprj> -o <project.msl> [--template <id>]
                [--jobs <range>] [--parallel <n>] [--force] [--json]
mocassin simulate <project.msl> --jobs <range> [--io <dir>] [--solver <path>]
                  [--parallel <n>] [--json]
mocassin collect <project.msl> [--dir <dir>] [--results <file.msr>]
                 [--external-states] [--json]
mocassin evaluate <project.msl> [--results <file.msr>] --query <name> [--where <expr>]
                  [-o <file>] [--format csv|json|parquet]
mocassin export <project.mocprj> -o <dir> [--format json|csv] [--json]
mocassin clean <project.msl> [--results <file.msr>] [--dir <dir>]
               [--older-than <days>] [--dry-run] [--json]
mocassin doctor [--json]
```

- `<range>` accepts `1-100`, `1,3,5`, and mixed forms.
- `--json` prints a single JSON object to stdout; human progress goes to stderr. `validate --json` uses the
  dedicated `validation-v1` report in §5.6; the other commands use the generic command result.
- `build` refuses to overwrite an existing `.msl` unless `--force`; writes atomically.
- `simulate` launches `mocassin-sim` per job and captures stdout/stderr. It does not modify the `.msl`.
- `collect` reads `Job%05d/solver.json`, `run.mcs`, `prerun.mcs`, and logs, validates them, and writes the
  `.msr` results store. The default is `<package>.msr`; `--results` overrides it.
- `evaluate` requires a `.msr` store (or an explicit `--results` path) in addition to the package.
- `collect --external-states` stores large `.mcs` files next to `.msr` with relative paths/hashes.
- `clean` removes job folders and/or result rows only with an explicit selection and confirmation; `--dry-run`
  reports what would be removed.

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
{"schemaVersion":1,"event":"build.progress","completed":120,"total":1000,"current":"T773/x001/2"}
{"schemaVersion":1,"event":"sim.progress","jobId":42,"mcs":1200000,"target":2000000,"rate":7.4e6}
{"schemaVersion":1,"event":"sim.finished","jobId":42,"exitCode":0,"abortReason":"completed","wallTimeS":12.4}
```

| Event | Required fields | Meaning |
| --- | --- | --- |
| `build.started` | `total` | Build began |
| `build.progress` | `completed`, `total` | Build progress |
| `build.finished` | `exitCode`, `wallTimeS` | Build ended |
| `sim.started` | `jobId` | Solver job began |
| `sim.progress` | `jobId`, `mcs`, `target` | Solver progress; `rate` optional |
| `sim.finished` | `jobId`, `exitCode`, `abortReason`, `wallTimeS` | Solver job ended |
| `collect.started` | `total` | Collection began |
| `collect.progress` | `completed`, `total`, `jobId` | Collection progress |
| `collect.finished` | `collected`, `skipped`, `failed` | Collection ended |
| `diagnostic` | `code`, `severity`, `message` | Structured diagnostic; `path` optional |
| `log` | `level`, `message` | Human log forwarded as JSON |

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
- The contract is registered in `contracts/manifest.json` under a top-level `reports` object as
  `validation-v1` (not as an entry in `formats`, whose eight ids are fixed). `tools/contracts/generate.py`
  validates the `reports` section and emits `contracts/generated/validation-v1.schema.json`; the schema is
  checked against real CLI output and the generated artifact is freshness-checked by `verify.sh contracts`.

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
- `tests/contracts/events-v1.jsonl` is accepted by the GUI event parser and the CLI schema validator.
- A schema-change PR must update the fixture, the version field, this document, the contract manifest, and the
  relevant ADR.
