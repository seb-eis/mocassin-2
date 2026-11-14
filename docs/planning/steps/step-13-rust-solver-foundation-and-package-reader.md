# Step 13 — Rust Solver Foundation and Package Reader

**Phase:** D. Solver  
**Depends on:** 11
**Delivers:** A Rust workspace with typed package structures, a validated `.msl` reader, deterministic RNG,
CLI scaffolding, structured logging, and a dry-run job inspector.

## Implementation status

- **Status:** Not started
- **Step branch:** —
- **Step CI:** —
- **Main merge:** —
- **Step tag:** —
- **Audit:** Not requested
- **Design review:** Not requested
- **Last updated:** —

| Feature | Branch | Status | Local gates | Step CI | Squash commit |
| --- | --- | --- | --- | --- | --- |
| — | — | — | — | — | — |

## Goal

Establish the solver foundation without Monte Carlo physics. The solver must load a simulation package,
validate every index and length, derive a job seed, and expose a safe, data-oriented job model to later
steps.

## Original reference

- `Sqlite3JobLoader.c` reads raw native structs from EF Core blobs; `Array_t` headers encode dimensions and
  blocks; the loader trusts sizes, has a move-sequence over-read bug (bytes/16 instead of bytes/24), and uses
  32-bit masks that are undefined for particle IDs ≥32.
- `JobDbModel.h` defines the in-memory model: lattice, job info/header, structure metadata, environments,
  pair/cluster tables, jump collections/directions/rules, tracker mappings.
- CLI arguments: `-dbPath`, `-jobId`, `-ioPath`, `-stdout`, `-extDir`, `-jumpLogMaxEv`; optional plugin paths.
- Errors exit with the internal error code; already-completed jobs exit 0.

## Design

### 1. Crate responsibilities

| Crate | Contents |
| --- | --- |
| `mocassin-types` | Serde structs for compiled model, job payload, lattice payload; newtypes; unit constants |
| `mocassin-msl` | `rusqlite` reader, schema/version checks, payload decode, invariant validation |
| `mocassin-rng` | SHA-256 seed derivation, ChaCha12 streams, bounded draws, doubles in `[0,1)` |
| `mocassin-solver` | Placeholder API (context/energy/KMC/MMC added in later steps) |
| `mocassin-state` | MessagePack state schema/types and writer/reader (implemented in step 18) |
| `mocassin-cli` | `mocassin-sim` argument parsing, logging, dry-run, error mapping |

### 2. Typed model

```rust
pub struct ParticleId(u8);
pub struct PositionId(u32);
pub struct CellIndex(u32);
pub struct JumpCollectionId(u32);
pub struct JumpDirectionId(u32);
pub struct RuleId(u32);
pub struct StateCode(u64);

pub struct ParameterizedModel {
    pub format_version: u32,
    pub signature: String,          // Step 09 parameterization signature
    pub model_signature: String,    // Step 08 compiled-model signature
    pub particles: ParticleTable,
    pub structure: StructureModel,
    pub energy: EnergyModel,
    pub transitions: TransitionModel,
    pub simulations: Vec<SimulationModel>,
}

pub struct JumpRule {
    pub rule_id: RuleId,
    pub collection_id: JumpCollectionId,
    pub state0: StateCode,
    pub state1: StateCode,
    pub state2: StateCode,
    pub frequency_hz: f64,          // absolute Step 09 value; signed
    pub charge_transport_vector: [f64; 3],
    pub static_correction: Option<[f64; 3]>,
    pub tracker_order: [u8; 8],
}

pub struct JobPackage {
    pub job_id: u32,
    pub kind: SimulationKind,
    pub model: Arc<ParameterizedModel>,
    pub lattice: InitialLattice,
    pub settings: JobSettings,
    pub seed: [u8; 32],
    pub optimizers: Vec<SelectionOptimizer>,
}
```

- Serde uses explicit `#[serde(rename = "...")]` and `#[serde(default)]` for optional fields.
  `.msl` package payloads use snake_case keys; `.mcs` state documents use camelCase keys (see the data
  contracts). Do **not** use `deny_unknown_fields` on forward-compatible payloads; readers ignore unknown
  keys as required by the data contract.
- Integers use the smallest type that proves the limit (particle `u8`, position `u32`, counts `u64`).
- State codes are `u64` with byte accessors; never a signed `i64`.
- Paths are `[i32; 4]` or a compact `[JumpStep; 8]`; movement vectors are `[f64; 3]`.
- All vectors/arrays are `Vec`/boxed slices, not linked nodes.

### 3. `.msl` reader

```rust
pub struct Package { connection: rusqlite::Connection }

impl Package {
    pub fn open_readonly(path: &Path) -> Result<Self>;
    pub fn schema_version(&self) -> Result<u32>;
    pub fn job_ids(&self) -> Result<Vec<u32>>;
    pub fn load_model(&self, model_id: i64) -> Result<Arc<ParameterizedModel>>;
    pub fn load_job(&self, job_id: u32) -> Result<JobPackage>;
    pub fn find_job_ids(&self, selector: &JobSelector) -> Result<Vec<u32>>;
}
```

- Open with `SQLITE_OPEN_READONLY | SQLITE_OPEN_NO_MUTEX`; set `query_only=ON`, `foreign_keys=ON`.
- Read `schema_info`; require `format_version` in the supported range and `application_id` match.
- Query `jobs` by id; join `models` and `lattices`; decode Zstd+MessagePack.
- Verify:
  - payload signatures match the stored `signature` by re-encoding the ADR 0010 canonical projection; the
    parameterization projection uses `model_signature`, the pair/group/site tables, and the absolute
    `jump_rules[].frequency_hz` values read directly from the payload (never a factor × maximum);
  - `lattice.len() == a*b*c*position_count`;
  - every particle index < particle count;
  - every position index < position count;
  - all jump mapping/count/tracker table dimensions match;
  - rule state codes reference valid particles for their positions;
  - no NaN/Inf in required numeric fields.
- Bound every allocation: reject counts that exceed configurable limits (e.g. 10⁸ positions, 10⁷ rules)
  before reserving memory.
- Cache decoded models in an `Arc`/`HashMap` so multiple jobs in one process share them (useful for tests;
  production runs one job per process).
- Never write to the database.

### 4. RNG

```rust
pub fn derive_job_seed(master: &[u8; 32], purpose: &[u8], collection: u32, config: u32, job: u32) -> [u8; 32];
pub fn derive_lattice_seed(job_seed: &[u8; 32], lattice_request_hash: &[u8; 32]) -> [u8; 32];

pub struct Rng {
    inner: ChaCha12Rng,
}

/// Opaque, versioned state for exact resume. `mocassin-rng` owns the byte
/// layout; do not serialize a third-party RNG's private layout directly.
/// The `.mcs` metadata stores `rngAlgorithm = "chacha12-v1"` and this blob.
pub struct RngStateV1(Vec<u8>);

impl Rng {
    pub fn from_seed(seed: [u8; 32]) -> Self;
    pub fn from_state(state: &RngStateV1) -> Result<Self, RngStateError>;
    pub fn export_state(&self) -> RngStateV1;
    pub fn next_u32(&mut self) -> u32;
    pub fn next_bounded(&mut self, upper: u32) -> u32; // Lemire, unbiased
    pub fn next_f64(&mut self) -> f64;                // [0,1)
    pub fn fork(&mut self, domain: &[u8]) -> Self;     // domain-separated stream
}
```

- `Rng::export_state()`/`from_state()` are the only supported resume path; the state writer and reader must
  round-trip `RngStateV1` exactly. Implement a thin serializable ChaCha12 wrapper rather than depending on a
  third-party RNG’s private layout.
- `next_bounded` uses Lemire's multiply-and-reject method, not modulo.
- `next_f64` uses 53 random bits / 2⁵³.
- Separate streams: lattice generation, simulation, extensions.
- Golden tests freeze outputs for a seed; changing the algorithm is a format-breaking change.

### 5. CLI scaffold

```text
mocassin-sim --db <msl> --job <id> --io <dir> [--dry-run] [--log-level] [--log-format]
```

- `clap` derive parser with validation and shell completions.
- `--dry-run` loads the job, prints a summary (kind, lattice size, particles, transitions, rules, seed
  fingerprint), and exits 0.
- Logging: `tracing` to stderr; human or JSON; stdout reserved for the statistics dump.
- Exit codes defined in step 12/architecture.
- Errors use `thiserror` and a top-level handler that prints a stable error code and context.

### 6. Data-oriented structures

Even though physics comes later, fix the layout now:

- Lattice: `Vec<ParticleId>` with `[u32;3]` dimensions and strides.
- Positions: `Vec<Position>` with `Vector4I`, site index, stability, allowed particle mask.
- Environments: CSR-style `env_offsets`, `env_links`, `cluster_links`.
- Pair tables: flat `Vec<f64>` with explicit strides; cluster tables: sorted codes + energy rows.
- Jump status: CSR `[cell][direction]`.
- Selection pools: `Vec<DirectionPool>` with `Vec<u32>` members and swap-remove.
- No `HashMap` in the per-cycle hot path; deterministic arrays only.

### 7. Validation and diagnostics

- Reader errors are structured:

```rust
pub enum PackageError {
    Sqlite(rusqlite::Error),
    SchemaVersion { found: u32, supported: RangeInclusive<u32> },
    MissingTable(&'static str),
    Decode { section: &'static str, source: rmp_serde::decode::Error },
    SignatureMismatch { expected: String, actual: String },
    InvalidIndex { what: &'static str, value: u64, limit: u64 },
    CorruptPayload { what: &'static str, detail: String },
}
```

- Every error includes the job/model id and section.
- `--dry-run` prints a validation report with counts and checks.

## Implementation tasks

- [ ] Create `mocassin-types` with all package structs and newtypes.
- [ ] Create `mocassin-msl` with schema/version checks, payload decode, and validation.
- [ ] Create `mocassin-rng` with SHA-256 derivation, ChaCha12, bounded draws, and golden tests.
- [ ] Implement `mocassin-sim` CLI parsing, logging, exit codes, and `--dry-run`.
- [ ] Implement the data-oriented base structures (lattice, positions, environments, pools).
- [ ] Add a fixture package from step 11 and a Rust integration test.
- [ ] Add fuzz targets for the package reader.
- [ ] Document the job/model structs and invariants.

## Acceptance criteria

- [ ] `mocassin-sim --db ceria.msl --job 1 --dry-run` reports the same counts as `mocassin inspect`.
- [ ] Future schema versions and corrupt/truncated payloads are rejected with clear errors.
- [ ] Signature mismatches are detected.
- [ ] The reader re-encodes the Step 09 parameterization signature exactly from `model_signature`, the
      pair/group/site tables, and the absolute `jump_rules[].frequency_hz` values; a changed frequency is
      detected without any factor × maximum reconstruction.
- [ ] Every index/length is validated before allocation.
- [ ] Seed derivation is deterministic and domain-separated; golden vectors pass.
- [ ] Loading a ceria job takes < 100 ms.
- [ ] The reader never opens the database writable.
- [ ] Fuzzing finds no panic/OOM on malformed input.
- [ ] Documentation written

## Documentation

At this point, document the Rust solver foundation:

- `docs/development/solver-architecture.md` — crates, package reader, data-oriented model, RNG streams.
- `docs/reference/msl-format.md` — the Rust reader’s validation and decoding contract.
- `docs/reference/cli.md` — `mocassin-sim` arguments, `--dry-run`, exit codes.
- `docs/development/testing-and-parity.md` — contract fixtures and fuzzing.

## Tests

| Test | Assertion |
| --- | --- |
| Reader property | Random valid `.msl` payloads round-trip; random truncations/mutations fail without panic |
| Acceptance | A real ceria package built by .NET opens and yields the expected dry-run summary |
| Schema | Version/application id checks |
| Decode | MessagePack/Zstd round-trip |
| Validation | Bad indices, lengths, signatures |
| RNG | Golden vectors, bounded uniformity, double range |
| CLI | Dry-run output, exit codes, logging |
| Fuzz | No panic/OOM |
| Performance | Load time budget |
| Read-only | Write attempt fails |

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Contract drift with .NET | Shared fixtures, canonical projection vectors, and canonical JSON comparison |
| Malicious/corrupt packages | Bounds checks, allocation caps, fuzzing |
| RNG changes break reproducibility | Freeze golden vectors; version the derivation |
| Struct layout confusion | Serde maps, not raw layout; MessagePack schema, no native endianness |
| SQLite build issues | `rusqlite` bundled feature; CI cache |

## Done when

The Rust solver can open a `.msl`, validate and decode one job, derive its seed, and report a complete dry-run
summary without running any physics.
