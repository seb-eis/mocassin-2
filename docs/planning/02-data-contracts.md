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
  - state-change group order inside a chain,
  - chain linkers,
  - doping priority,
  - reference path positions,
  - job collections/configurations.
- All other arrays are sorted by `id` when serialized, so identical models produce byte-identical JSON.
- Compiled objects receive dense integer indices (`ParticleIndex`, `PositionIndex`, `JumpCollectionIndex`,
  `RuleIndex`) during compilation. These indices are **not** stable across model edits; they are valid only
  inside one compiled package and are recorded with the model signature.

## 2. Project file (`.mocprj`, JSON)

### 2.1 Envelope

```json
{
  "$schema": "https://mocassin.dev/schemas/project-v1.json",
  "schemaVersion": 1,
  "projectId": "018f4c9a-7f6b-7c3a-9d2e-1a2b3c4d5e6f",
  "name": "Ceria",
  "createdUtc": "2026-11-02T12:00:00Z",
  "modifiedUtc": "2026-11-02T12:30:00Z",
  "model": { "...": "section 2.2" },
  "parameterizations": [],
  "jobTemplates": [],
  "buildTemplates": [],
  "viewSettings": {}
}
```

- UTF-8, no BOM, 2-space indentation, `\n` line endings.
- `schemaVersion` is an integer. Readers accept `1..current`; migrations are pure functions in
  `Mocassin.Core.Project.ProjectMigrations`.
- Unknown properties are rejected by default (`JsonUnmappedMemberHandling.Disallow`) to catch typos; schema
  evolution adds properties explicitly and bumps the version when semantics change.
- The file is written atomically (temp file + rename).

### 2.2 Model

```json
{
  "particles": [],
  "particleSets": [],
  "structure": {},
  "transitions": {},
  "energy": {},
  "lattice": {},
  "simulations": []
}
```

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
  violating mass conservation.
- `charge` is in units of the elementary charge.
- `isVacancy` marks a pseudo-particle that represents an empty state. Vacancies have `charge = 0` or their
  default occupation does; the validator warns when both are non-zero.
- The implicit void particle is not serialized; it is created by the compiler as a reserved ID.

#### Particle sets

```json
{
  "id": "…",
  "name": "Oxygen site",
  "particleIds": ["…", "…"]
}
```

- The first particle is the default occupation of a stable site. Unstable sites always default to void.
- The void particle is implicit and must not be listed.

#### Structure

```json
{
  "spaceGroup": { "number": 225, "setting": "origin-choice-1", "hallSymbol": "F -4 2 3" },
  "cell": { "a": 5.41, "b": 5.41, "c": 5.41, "alpha": 90.0, "beta": 90.0, "gamma": 90.0 },
  "sites": [
    {
      "id": "…",
      "name": "Ce",
      "fractional": [0.0, 0.0, 0.0],
      "particleSetId": "…",
      "stable": true
    }
  ]
}
```

- Lengths in Å; angles in degrees.
- `fractional` components are in `[0, 1)` for the reference position.
- `setting` selects the space-group setting (origin choice, unique axis) when more than one exists.
- Stable sites may define interactions and host particles; unstable sites are transition states.

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
      "referencePath": [[0.25, 0.25, 0.25], [0.5, 0.25, 0.25], [0.25, 0.25, 0.25]]
    }
  ],
  "metropolisTransitions": [
    { "id": "…", "name": "O exchange", "chainId": "…", "siteAId": "…", "siteBId": "…" }
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
  - `custom`: 2–8 positions with an explicit, validated `D/S` linker sequence; reverse generation is explicit.
- `referencePath` positions use the unit-cell fractional coordinate system and may lie outside `[0, 1)`.
- MMC transitions bind two reference sites; selecting the same site twice means an exchange within one
  sublattice.

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

- `cutoff` and filter radii are in Å.
- Filters remove pair interactions between the two referenced sites in the radial range.
- Cluster offsets are absolute fractional vectors relative to the cluster centre, in definition order.
- Site energies are in eV; missing entries default to 0.

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

- Dopings are applied in ascending `priority`.
- Each doping fraction applies to the particles remaining after higher-priority dopings.
- `dependentExchangeId` may be omitted when `chargeCompensation` is false or the primary exchange is
  self-compensating.
- The default building block is implied: first particle of each stable site's set, void on unstable sites.

#### Simulations

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
      "preRunMcsp": 100,
      "timeLimitSeconds": null,
      "minSuccessRate": null,
      "electricFieldModulusVm": 1.0e7,
      "electricFieldDirection": [1.0, 0.0, 0.0],
      "normalizationEnergyEv": null,
      "maxAttemptFrequencyHz": null,
      "executionFlags": ["NoJumpLogging"]
    }
  }
]
```

MMC uses `metropolisTransitionIds` and settings `breakTolerance`, `breakSampleLengthMcsp`,
`breakSampleIntervalMcsp`, `resultSampleMcsp`.

Common execution flags: `NoSaving`, `NoJumpLogging`, `UseFastExp`, `Deterministic`.

### 2.3 Parameterization templates

```json
{
  "id": "…",
  "name": "Ceria parameters",
  "modelSignature": "blake3:…",
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

- `modelSignature` is a BLAKE3 hash of the canonical compiled model (structure, transitions, environments,
  interaction geometry, indices). A parameterization whose signature does not match the current model cannot
  be used to build jobs; the GUI offers "rebase" and shows which sets changed.
- Rule IDs are stable strings derived from the rule's state triplet and direction, so recompilation can match
  existing frequencies.
- The original could not edit site energies through a parameterization template. The new format includes
  `siteEnergyOverrides` as an intentional improvement; the GUI marks them as overrides.
- Chiral pair sets share one interaction ID and one energy table. The compiler refuses two independent
  values for a linked chiral pair.

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
          "dopings": [{ "dopingId": "…", "fraction": 0.01 }]
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

The `.msl` file is the compiled artifact consumed by the Rust solver and the result database consumed by the
evaluation API. It is not the editable project. Rebuilding is expected after model or parameterization
changes.

### 3.2 Schema (version 1)

```sql
PRAGMA journal_mode = WAL;
PRAGMA foreign_keys = ON;

CREATE TABLE schema_info (
    key   TEXT PRIMARY KEY,
    value TEXT NOT NULL
);
-- rows: ('format_version','1'), ('app_version','x.y.z'), ('created_utc','…'),
--       ('project_id','…'), ('generator','mocassin .NET x.y.z')

CREATE TABLE models (
    model_id       INTEGER PRIMARY KEY,
    signature      TEXT    NOT NULL UNIQUE,   -- blake3:…
    format_version INTEGER NOT NULL,
    payload        BLOB    NOT NULL            -- Zstd(MessagePack(CompiledModel))
);

CREATE TABLE lattices (
    lattice_id     INTEGER PRIMARY KEY,
    signature      TEXT    NOT NULL UNIQUE,   -- blake3 of the initial lattice
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

CREATE TABLE results (
    job_id             INTEGER PRIMARY KEY REFERENCES jobs(job_id) ON DELETE CASCADE,
    status             TEXT NOT NULL CHECK (status IN
                         ('pending','running','completed','failed','cancelled','aborted')),
    exit_code          INTEGER,
    started_utc        TEXT,
    completed_utc      TEXT,
    wall_time_s        REAL,
    stdout             TEXT,
    run_state          BLOB,                   -- .mcs
    prerun_state       BLOB,                   -- .mcs, KMC only
    run_state_sha256   TEXT,
    prerun_state_sha256 TEXT,
    error_message      TEXT
);

CREATE VIEW job_metadata AS
SELECT j.job_id,
       j.collection_name,
       j.config_name,
       j.collection_index,
       j.config_index,
       j.job_index,
       j.kind,
       j.temperature_k,
       j.electric_field_vm,
       j.target_mcsp,
       j.pre_run_mcsp,
       j.lattice_a, j.lattice_b, j.lattice_c,
       j.doping_info,
       r.status,
       r.exit_code
FROM jobs j
LEFT JOIN results r USING (job_id);
```

Rules:

- `job_id` is a deterministic 1-based integer assigned at build time in template order
  (collection index, config index, job index) and never reused within a package.
- Result rows are inserted as `pending` at build time.
- `run_state`/`prerun_state` are written inside a transaction by `mocassin collect`; the hash is checked
  against the file.
- The database uses `application_id`/`user_version` pragmas in addition to `schema_info` for tooling.
- The solver opens the database read-only (`SQLITE_OPEN_READONLY`) and never writes results.

### 3.3 Compiled model payload

MessagePack map with snake_case keys. Top-level:

| Key | Type | Meaning |
| --- | --- | --- |
| `format_version` | u32 | Payload version |
| `signature` | string | BLAKE3 of canonical model |
| `particle` | object | `charges`, `symbols`, `is_vacancy`, count; index 0 is void |
| `structure` | object | cell vectors, P1 positions, site definitions, environments, interaction range |
| `energy` | object | pair tables, cluster tables, defect background |
| `transitions` | object | jump collections, directions, rules, mapping tables |
| `simulations` | array | simulation encodings (active collections, settings defaults, trackers) |

`structure` fields:

| Key | Type | Meaning |
| --- | --- | --- |
| `cell_vectors_ang` | `[[f64;3];3]` | Cartesian a/b/c vectors in Å |
| `positions` | array | `{vector4:[i32;4], site_index:u32, stable:bool, particle_ids:u8[]}` in P1 order |
| `position_count` | u32 | Length of `positions` |
| `site_count` | u32 | Number of user reference sites |
| `environments` | array | Per position: `selection_particle_mask:u64`, pair interactions, cluster interactions, `update_particle_ids` |
| `interaction_range` | `[i32;4]` | MMC interaction cube |
| `static_trackers_per_cell` | u32 | |
| `global_tracker_count` | u32 | |

`energy` fields:

| Key | Type | Meaning |
| --- | --- | --- |
| `pair_tables` | array | `{object_id, energies:[f64; n_particles*n_particles]}` |
| `cluster_tables` | array | `{object_id, occupation_codes:[u64], energies:[f64], particle_table_mapping:[u8; n_particles]}` |
| `defect_background` | array | `[position][particle]` site energy in eV |

`transitions` fields:

| Key | Type | Meaning |
| --- | --- | --- |
| `jump_collections` | array | `{object_id, mobile_particle_mask:u64, rule_ids:[u32], direction_ids:[u32]}` |
| `jump_directions` | array | `{object_id, position_id, collection_id, path:[i32;4], movement:[[f64;3]], field_factor, length}` |
| `jump_rules` | array | `{rule_id, state0:u64, state1:u64, state2:u64, frequency_factor, field_factor, static_correction, tracker_order:[u8;7]}` |
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
| `default_settings` | object | Temperature, MCSP, pre-run, limits, field, flags |
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
| `pre_run_mcsp` | u64 | |
| `time_limit_s` | u64? | |
| `min_success_rate` | f64? | |
| `electric_field_vm` | f64 | |
| `electric_field_direction` | `[f64;3]` | Normalized Cartesian direction |
| `normalization_energy_ev` | f64? | |
| `max_attempt_frequency_hz` | f64? | |
| `flags` | u32 | |
| `selection_optimizers` | array | `{position_id:u32, particle_id:u8}` |
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

## 4. Binary state file (`.mcs`, version 1)

### 4.1 Design

- Little-endian, fixed-width, no language-specific padding.
- A header and section table allow forward-compatible readers to skip unknown sections.
- Arrays are contiguous and 8-byte aligned; readers can create zero-copy spans.
- Optional Zstd compression is a section flag, not an envelope; default is uncompressed for mmap-friendly
  local analysis.
- CRC32C covers each section and the header.

### 4.2 Header (64 bytes)

| Offset | Size | Field |
| --- | --- | --- |
| 0 | 4 | magic `MCS2` |
| 4 | 2 | `version` (u16) = 1 |
| 6 | 2 | `header_size` (u16) = 64 |
| 8 | 4 | `flags` (u32) |
| 12 | 4 | `section_count` (u32) |
| 16 | 8 | `cycle_count` (u64) |
| 24 | 8 | `simulated_time_s` (f64) |
| 32 | 8 | `target_mcsp` (u64) |
| 40 | 8 | `job_id` (u64) |
| 48 | 4 | `header_crc32c` (u32) |
| 52 | 4 | reserved (u32) = 0 |
| 56 | 8 | `file_size` (u64) |

### 4.3 Section table

Immediately after the header, `section_count` entries of 40 bytes:

| Offset | Size | Field |
| --- | --- | --- |
| 0 | 4 | `section_id` (u32) |
| 4 | 4 | `element_type` (u32): 0=u8, 1=u32, 2=u64, 3=f64, 4=utf8 JSON, 5=raw |
| 8 | 8 | `offset` (u64) |
| 16 | 8 | `length_bytes` (u64) |
| 24 | 8 | `count` (u64) |
| 32 | 4 | `flags` (u32): bit 0 = Zstd, bit 1 = optional |
| 36 | 4 | `crc32c` (u32) of the stored bytes |

Section payloads start after the table, aligned to 8 bytes. Unknown section IDs are skipped. A reader must
reject duplicate IDs, out-of-range offsets, and counts that exceed the file size.

### 4.4 Sections

| ID | Name | Element | Shape | Contents |
| --- | --- | --- | --- | --- |
| 1 | `metadata` | JSON | 1 | Solver version, package signature, job metadata, abort reason, flags |
| 2 | `lattice` | u8 | `n_positions` | Final particle index per linearized position |
| 3 | `cycle_counters` | u64 | `n_particles × 8` | Attempts, successes, skips, rejected, blocked, S0-unstable, S2-unstable, accepted |
| 4 | `global_trackers` | f64 | `n_global × 3` | Displacement vector per particle/transition tracker |
| 5 | `mobile_trackers` | f64 | `n_mobile × 3` | Displacement vector per mobile particle |
| 6 | `static_trackers` | f64 | `n_static × 3` | Displacement vector per position/particle tracker |
| 7 | `mobile_mapping` | u32 | `n_mobile` | Position index for each mobile tracker |
| 8 | `jump_histograms` | u64 | `n_global × 4 × 1000 + 2` | Four barrier histograms plus under/overflow counters per global tracker |
| 9 | `energy_samples` | f64 | `n_samples` | Optional MMC energy trace / KMC sample series |
| 10 | `particle_positions` | u32 | `n_mobile` | Initial position index for mobile particles (diagnostics) |

The exact counter field order and histogram layout are frozen in step 18 and verified against the reader in
step 20. Both sides share a single generated constants file (`tests/contracts/mcs-sections.json`).

### 4.5 Validation

A reader must:

1. Check magic, version, header size, and CRC.
2. Check `file_size` against the actual file length.
3. Validate every section offset/length/count and element type.
4. Validate that `lattice` length equals the job's `a*b*c*position_count`.
5. Validate tracker counts against the job's simulation metadata.
6. Reject NaN/Inf in fields where they are not meaningful (e.g. lattice positions).

## 5. CLI contracts

### 5.1 `mocassin` (.NET)

```text
mocassin new <name> [-o <dir>] [--template ceria|blank]
mocassin validate <project.mocprj> [--json] [--warnings-as-errors]
mocassin compile <project.mocprj> -o <compiled-model.json> [--json]
mocassin build <project.mocprj> -o <project.msl> [--template <id>]
                [--jobs <range>] [--parallel <n>] [--force] [--json]
mocassin simulate <project.msl> --jobs <range> [--io <dir>] [--solver <path>]
                  [--parallel <n>] [--json]
mocassin collect <project.msl> [--dir <dir>] [--json]
mocassin evaluate <project.msl> --query <name> [--where <expr>] [-o <file>]
                  [--format csv|json|parquet]
mocassin export <project.mocprj> -o <dir> [--format json|csv] [--json]
mocassin doctor [--json]
```

- `<range>` accepts `1-100`, `1,3,5`, and mixed forms.
- `--json` prints a single JSON object to stdout; human progress goes to stderr.
- `build` refuses to overwrite an existing `.msl` unless `--force`; writes atomically.
- `simulate` launches `mocassin-sim` per job, captures stdout/stderr, and writes status rows.
- `collect` reads `Job%05d/run.mcs`, `prerun.mcs`, and `stdout.log`, validates them, and updates results.

### 5.2 `mocassin-sim` (Rust)

```text
mocassin-sim --db <project.msl> --job <id> --io <dir>
             [--stdout <file>] [--seed <hex32>] [--threads <n>]
             [--extension <path>] [--jump-log-max-ev <f64>]
             [--log-level <error|warn|info|debug|trace>]
             [--log-format human|json] [--dry-run] [--version]
```

- `--io` is created if missing. The solver writes `run.mcs` (and `prerun.mcs` for KMC) there.
- `--stdout` redirects the human statistics dump; otherwise it goes to stdout.
- The solver never writes to the `.msl`; `collect` does.
- Exit codes are defined in [`01-target-architecture.md`](01-target-architecture.md#73-error-handling-and-exit-codes).

### 5.3 Machine-readable events

Both CLIs support JSON Lines progress events on stderr when `--json`/`--log-format json` is active:

```json
{"event":"build.progress","completed":120,"total":1000,"current":"T773/x001/2"}
{"event":"sim.progress","job":42,"mcs":1200000,"target":2000000,"rate":7.4e6}
{"event":"sim.finished","job":42,"exitCode":0,"abortReason":"COMPLETED","wallTimeS":12.4}
```

The GUI consumes these events for progress bars and live charts.

## 6. Contract testing

- `tests/contracts/project-minimal.mocprj` and `project-ceria.mocprj` are canonical fixtures.
- `tests/contracts/model-v1.msgpack.zst` is written by .NET and read by Rust; both dump a canonical JSON
  representation and compare.
- `tests/contracts/state-v1.mcs` is written by Rust and read by .NET; the reader dumps section metadata and
  a checksum.
- A schema-change PR must update the fixture, the version field, this document, and the relevant ADR.
