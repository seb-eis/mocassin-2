# Solver Architecture

The Monte Carlo solver is a separate Rust process, `mocassin-sim`. It consumes only the immutable simulation
package (`.msl`) and command-line arguments. It never reads project JSON and never writes `.msl` or `.msr`.
Results are written as per-job state files, which a separate `mocassin collect` command stores in the `.msr`
sidecar.

This page describes the Step 13 foundation and the Step 14 simulation context: the crate graph, the
package-reader pipeline, the runtime data model, the context build, the RNG streams, and the CLI skeleton.
Steps 15–19 add the energy engine, KMC/MMC loops, tracking, state writing, and the production CLI.

## Crate graph

```text
mocassin-cli
   ├──► mocassin-msl ──► mocassin-types
   ├──► mocassin-rng
   ├──► mocassin-types
   └──► mocassin-solver (existing edge; used by the future run path)
mocassin-solver (Step 14+)
   ├──► mocassin-types
   ├──► mocassin-rng
   ├──► mocassin-msl (dev-dependency: fixtures only)
   ├──► mocassin-state (added in Step 18)
   └──► mocassin-extension (added in Step 19)
```

The Step 14 manifest matches this state: `mocassin-msl` is a `[dev-dependencies]` entry, and the unused
`mocassin-state`, `mocassin-extension`, and `rayon` edges are removed in feature 1 (review F-15). The Step 13
manifest still declared all of them; the crate graph above is the target, not the Step 13 state. `rayon` returns
only for a measured parallel initialization in Step 29.

| Crate | Responsibility |
| --- | --- |
| `mocassin-types` | Payload DTOs, typed dense IDs, validated runtime model/job/lattice types, read limits; no I/O |
| `mocassin-msl` | Read-only SQLite reader, schema/version/provenance checks, bounded Zstd+MessagePack decode, validation, canonical projection/signature re-encoding |
| `mocassin-rng` | SHA-256 seed derivation, ChaCha12 streams, bounded draws, 64-byte resume state |
| `mocassin-solver` | Simulation context, energy engine, KMC, MMC, trackers, statistics |
| `mocassin-state` | MessagePack `.mcs` schema, writer, reader |
| `mocassin-extension` | Versioned extension trait and C ABI |
| `mocassin-cli` | `mocassin-sim` parsing, logging, dry-run inspection, exit codes |

Rules:

- `mocassin-types` has no SQLite, Zstd, tracing, or CLI dependency.
- `mocassin-msl` is the only crate that opens SQLite, decodes package payloads, and re-encodes canonical
  projections. It never writes a database.
- `mocassin-rng` is the only crate that names `rand_chacha` state APIs. The `RngStateV1` bytes are a
  first-party contract.
- The CLI uses the reader and RNG directly for inspection; it keeps its existing `mocassin-solver` edge for the
  future run path and does not call the placeholder solver in Step 13. The solver core is a library and does not
  own a second reader.
- `Package` holds a SQLite connection and is not `Sync`. Parallel jobs open separate packages in separate
  processes; a future batch worker opens one package per worker.

The module layout is defined in [Code structure](code-structure.md) §2.2.

## Package read pipeline

```text
jobs row
  → models row (Zstd frame)
  → declared-size check → bounded decompress
  → non-allocating MessagePack scan (budgets, depth, lengths, duplicate keys)
  → typed decode
  → runtime mapping
  → index/length/finite/kind/tracker validation
  → parameterization signature re-encode
  → lattice signature re-encode
  → column/payload cross-check
  → JobPackage
```

1. **Open.** The reader rejects a missing or unreadable path before SQLite opens it, then opens with
   `SQLITE_OPEN_READONLY`, `query_only=ON`, `foreign_keys=ON`, and a bounded busy timeout. A `0444` package on
   read-only media opens normally and needs no `-wal`/`-shm` sidecar.
2. **Identity.** `application_id` (`0x4D4F4353`), `user_version` (1), and `schema_info.format_version` (1) must
   match. The reader requires the tables and columns it uses and the nine `schema_info` keys. The model
   provenance strings must be **non-empty** and then equal the `schema_info` rows; an empty payload and empty
   schema values are rejected rather than accepted as a matching pair.
3. **Decode.** Each payload is one standard Zstd frame. The reader checks the declared decompressed size (or
   streams with a hard limit when none is declared) against a byte guard before allocating. The MessagePack
   scan walks the document without allocating per element and rejects an element budget, depth, declared length,
   duplicate known key, or per-field count violation. Only then is the typed document decoded.
4. **Validate.** Every index and length in the particle, structure, environment, energy, transition, simulation,
   job, and lattice sections is checked; NaN/Inf is rejected where the schema requires a finite value; state
   codes and tracker entries are checked against the particle count and path length; jump-count prefix sums,
   kind bits, and normalized field directions are checked. The particle table requires index 0 to be void with
   zero charge and `is_vacancy == false` (the Step 08 writer marks the synthetic void particle as not an
   authored vacancy); a `true` flag at index 0 is rejected. Each rule must preserve the non-void particle-symbol
   multiset and charge within `1e-9`; `path_vector`/`length` must match the per-node movements and the cell
   vectors; a no-charge-change rule must have a zero transport vector. The full positional
   `charge_transport_vector` formula is assigned to Step 15 because the v1 payload has no fractional P1
   coordinates (see [the simulation package format](../reference/msl-format.md) and the Step 13 design).
5. **Signatures.** The six-field parameterization projection and the five-field lattice projection are
   re-encoded with the fixed-width canonical writer and compared with the stored signatures. The compiled-model
   signature is an identity string and is not re-encoded.
6. **Job.** The job row's payload-derived columns must equal the payload values; the seed must be 32 bytes. A
   job row whose `model_id` or `lattice_id` row is absent is a package-class error (exit 11), not
   `JobNotFound` (exit 10). The result is a validated `JobPackage`.
7. **Effective seed.** The package seed is written into `JobPackage.seed`; an optional `--seed` override is
   passed through `Package::load_job_with_seed`, which replaces that field after validation. A Step 14 consumer
   always reads the effective seed from `JobPackage.seed`, matching design §2.3.

The reader does **not** run `PRAGMA integrity_check` and does **not** mirror the `PKG` diagnostics. Those belong
to `mocassin verify`; the solver checks only the data it consumes. The package format and its fields are in the
[simulation package format](../reference/msl-format.md).

## Read limits

| Limit | Default |
| --- | ---: |
| Model / job / lattice payload | 512 MiB / 8 MiB / 512 MiB |
| Model / job MessagePack elements | 512,000,000 / 1,000,000 |
| MessagePack depth | 8 (the model nests five container levels) |
| Total positions (`A*B*C*P`) | 500,000 (matches `SupercellGenerator.MaximumPositions`; also ≤ signed 32-bit linear index) |
| P1 positions per cell | 10,000 (matches `UnitCellVectorEncoder.MaxP1Positions`) |
| Particles | 64 |
| Rules / directions / collections | 1M / 1M / 4,096 (`max_rules` matches the default `CompilationOptions.MaxRuleCount`, an `init` option; raise `ReaderLimits` if raised) |
| Pair / cluster tables | 1,000,000 each |
| Pair links | 16M total, 65,536 per position |
| Path nodes / tracker entries | 10 |
| Extensions / simulations | 256 / 4,096 |

The lattice guard is `4 × a × b × c × position_count` in checked 64-bit arithmetic, capped by the lattice byte
limit. The expected particle array is exactly `a*b*c*position_count` bytes. A limit violation is a structured
error, never a panic or an unbounded allocation.

## Runtime model

The reader produces the types Step 14 consumes:

- `ParameterizedModel`: particle table, structure (positions and per-position environments), energy tables,
  transitions, simulations, and derived counts.
- Positions are in canonical P1 order with `vector4 = [0, 0, 0, P]`, a site index, a stable flag, and a `u64`
  allowed-particle mask reconstructed from the payload's particle list.
- Environment pairs are one entry per periodic image: `target_position`, relative integer `offset`, and the
  shared pair-table index (payload `energy_id`). Step 14 expands `cell + offset` into the supercell target;
  Step 13 does not.
- Energy tables keep payload order and strides; the pair table is `particle_count²` per table. Step 15 converts
  eV to kT.
- Transitions keep collections, directions, rules, `jump_count`, the derived prefix-sum `jump_offsets`,
  `jump_mapping`, and the tracker mappings. `tracker_order` is variable-length with a `0xFF` sentinel.
- `DirectionPool`: `direction_count`, flat `(PositionId, ParticleId)` members, and the caller-indexed
  `slot_of` map. Its `PositionId` is contractually the P1 index, so it **cannot** represent the supercell pool
  member Step 14 needs; Step 14 replaces it with `context::pools::SelectionPool` and retires the placeholder
  (review F-1). `JumpStatus` is the CSR container (`offsets: Vec<u32>`, `links: Vec<JumpLink>`); Step 14 computes
  the entries and extends `JumpLink` with `receiver_path_id` so the receiver-major orientation is explicit
  (review F-2). Both containers are type-only in Step 13 and have invariant tests (`JumpStatus::is_valid`).
- `JobPackage`: job id, kind, shared model, initial lattice, settings, effective seed, optimizers, extensions,
  instruction, and doping summary.

Hot data uses flat arrays, `u32`/`u8` indices, and `u64` masks. `HashMap` is not used in a per-cycle structure.
The model cache may use a hash map; it is not a hot path. All hot arrays are allocated at load/initialization;
Step 14 owns the no-allocation-in-cycle proof.

## Simulation context (Step 14)

`mocassin_solver::context::SimulationContext::initialize(job, options, cancel)` builds the runtime state once per
job from the validated `JobPackage`. It performs no I/O, never re-reads the package, and allocates every hot
array once. Later steps consume it read-only or through the documented update helpers; they do not add fields.

```text
SimulationContext
├── job: JobPackage                 shared Arc<ParameterizedModel> + lattice + settings + seed
├── dimensions [A,B,C,P], pc        checked index/wrap helpers, EnvId vs PositionId
├── lattice                         particles, flags (stable/mobile/tracker), mobile tracker ids
├── links                           owner-major per-P1 pair-link CSR + inverse routing index + cluster assoc
├── clusters                        instances, centre index, occupation codes/ids, per-centre energies
├── energy                          site table, site+pair owner vectors (kT), field scale
├── pools                           KMC supercell direction-count pools, selectable count, slot maps
├── jumps                           KMC per-(P1,direction) receiver-major links
├── trackers                        mobile/static index space and the mobile-position mapping
├── counters, goals, normalization  zeroed counters, MCSP goals/block size, K_fix/K_auto/K_norm/dof/dt
├── rng, time_s, lattice_energy_ev  effective stream, simulated time, exact initial total energy
└── origin, limits, _not_sync       fresh/resumed, ContextLimits, explicit PhantomData<Cell<()>>
```

**Links and orientation.** A payload pair link on P1 position `O` is stored at its **owner** (the interaction
centre) and points at its partner; the table is row-major `[owner particle][partner particle]`. The runtime keeps
an owner-major CSR over P1 positions (`{partner_position, offset:[i32;3], pair_id, cluster assoc}`) plus a
compact **inverse** CSR over partner P1 positions (`{owner_position, link_id}`). The owner's site+pair vector is
`site + Σ table[pair_id][p_owner][particle at partner]`. A change at `V` updates the vectors of every owner in
`inverse(position(V))`, with the owner's table and the owner's row; the sender's table is never applied. This is
the original's receiver-owned link list, expressed as a compact index instead of one copy per supercell cell, so
500k positions stay bounded. Links are sorted by `pair_id`, then partner, then offset, which fixes the f64
summation order. A missing reverse link means the payload has no interaction in that direction and is not an
error.

**Clusters.** A cluster link's `byte_position` is the environment's own slot (slot `0` = centre); the centre is
the unique environment whose link carries slot `0`. The runtime keeps one instance per referenced table, a
`centres` CSR over P1 positions, a base occupation code per instance and cell with the centre particle in slot
`0`, and a per-centre energy vector. A slot's cluster association is attached to the centre's **forward link**
whose partner is that member, so a change at the member updates the centre's code and centre energy through the
same inverse index. Cluster interactions are counted once at the centre, so they are kept out of the site+pair
vector and the total uses `Σ site + 0.5 × Σ pair + Σ cluster`. The v1 payload does not persist member cell
offsets; the context derives them only from the unique centre→member link (slot 0 is `[0,0,0]` by definition)
and fails closed otherwise (OD-1 in the internal open-decisions register).

**Jump status.** One status per `(P1 position, direction)` (`status = direction * P + position`). The path is
resolved from the direction's relative node encodings with node `0` replaced by the selected position, so a
direction mapped into a symmetry-equivalent start bucket is applied at the selected bucket. For every ordered
sender/receiver path-node pair, the **receiver's** forward link that targets the sender is stored as
`JumpLink { sender_path_id, receiver_path_id, link_id }` (`link_id` relative to the receiver's P1 list); the
receiver's cluster associations travel with that link. The receiver filter is the compiled structural stability,
not the initial particle, and the bound is `L*(L-1) ≤ 90` with an invariant error instead of the original's
unchecked 56-entry buffer. The link count is determined by a counting pass and checked against the jump-status
caps before allocation.

**Goals, normalization, resume.** `mobile_count` is the number of non-void positions (passive species included);
`prerun = pre_run_mcsp × mobile_count`, `main = target_mcsp × mobile_count`, `total` rounded up to a multiple of
100, block = `total/100`; a zero total is a data error. `K_fix = exp(E_norm/kT)`, `K_auto = 1/RawMax`,
`K_norm = K_fix × K_auto`, `dt = dof × K_norm/(nu_max × selectable)`, where `nu_max` is the job cap or the maximum
active rule frequency (the payload persists only the optional job cap, not the original's derived build-time
modulus). Resume takes a plain `ResumeState` carrying the `.mcs` parameterized `modelSignature`, the `seedHash`,
and the counters/time/state, validates it against the package/job, re-derives the mobile mapping, preserves
`RawMax`, and returns a distinct outcome for a completed state. The `.mcs` has no lattice signature field, so
none is required. The `.mcs` decode is Step 18's.

**Memory.** Before the first allocation, `MemoryPlan` computes every array's bytes with checked `u64` arithmetic
(lattice, site+pair vectors, site table, forward/inverse links, cluster assoc, cluster geometry/codes/code ids/
centre energies, pool members + slot map, jump offsets/links, counters) and rejects against per-array caps and a
1 GiB `max_context_bytes` total; `LimitExceeded` is exit 12, not the run-time limit exit 14. `MemoryReport`
reports the allocated capacities and must equal the plan. The 10×10×10 real-writer fixture (table-free ceria) and
the populated-model 10×10×10 clone are both budgeted at `< 100 ms` and `≤ 64 MiB`; the clone is the pair/cluster
measurement because the ceria model has no pair or cluster tables.

## RNG

The algorithm is ChaCha12 with SHA-256-derived seeds (ADR 0005). Job-level parallelism only; a trajectory is
single-threaded.

- Job seed: `SHA-256(master[32] || u32_le(collection) || u32_le(config) || u32_le(job) ||
  UTF8("mocassin/job/v1"))`.
- Lattice seed (build-side): `SHA-256(UTF8("mocassin/lattice/v1") || job_seed[32] || request_hash[32])`.
- Draws: `next_u32` (one word), `next_u64` (two words), `next_f64` (53 bits from two words, `[0,1)`),
  `next_bounded` (Lemire multiply-and-reject, never modulo), and `fork(domain)` (a SHA-256-derived,
  domain-separated stream that does not advance the parent).
- Streams: one continuous simulation stream across a KMC pre-run and its main run; a per-extension fork; the
  lattice stream is .NET build-side only.
- Resume: `RngStateV1` is a 64-byte blob with magic `MCR1`, version 1, a 64-bit stream, a 128-bit word
  position, and the 32-byte key. `rngAlgorithm = "chacha12-v1"` is bound to it. Export/import resumes at the
  exact 32-bit word, including mid-block.
- Changing the draw semantics (word consumption, `next_f64` construction, or `next_bounded`), the 64-byte
  state layout, or the algorithm id is format-breaking: it needs a new ADR and a new algorithm id
  (`chacha12-v2`). Changing the seed or fork derivation changes reproducibility for new runs but not the state
  layout; it still needs an ADR or version note.

The RNG benchmark measures draws per second, 64-word refill cost, state round-trip, and fork cost. The Step 13
medians (AMD Ryzen 7 7840U, release) are `next_u32` 733 M/s, `next_u64` 437 M/s, `next_f64` 393 M/s,
`next_bounded` 569-572 M/s, 64-word refill 75.4 ns, state round-trip 123.4 ns, and 32-byte fork 113.1 ns;
every §4.5 budget is met. The structured evidence is `tests/reference/bench/step-13-rng.json`, the raw log is
`step-13-rng-2026-11-19.txt`, and the `rustRng` entry in `baselines.json` is the same-runner-class baseline. A
regression above 10 % on that class fails the release gate.

## CLI foundation

Step 13 implements the inspection path:

```text
mocassin-sim --db <project.msl> --job <id> --dry-run [--io <dir>] [--seed <hex32>] [--threads <n>]
             [--extension <path>] [--no-extensions] [--jump-log-max-ev <f64>]
             [--log-level ...] [--log-format ...]
```

- `--dry-run` loads, validates, and prints a stable summary (schema, model/lattice/job counts and signatures,
  settings, transition/tracker counts, RNG algorithm and seed hash). It writes nothing: it does not create the
  I/O directory, does not write a log file, and does not accept `--stdout` (any use is an invalid-argument
  error; the run-dump option is Step 19 work).
- `--io` is accepted and never created; `--threads` and `--jump-log-max-ev` are validated but unused; an
  `--extension` request is rejected with exit 10 until the Step 19 ABI; `--no-extensions` is a no-op.
- A non-dry-run invocation is a usage error until the simulation engines land; Step 16 enables a run and Step 19
  completes the option surface (`batch`, `--validate-only`, `--no-state`, `--log-file`, extensions).
- Logs go to stderr (`tracing`, human or JSON; `--log-level` is the only filter and `RUST_LOG` is not
  consulted). stdout is reserved for the human report. A panic in the inspection path is caught and exits 13,
  never 10.
- Exit codes (canonical definition: internal target architecture §7.3): 0 success, 5 partial success for
  multi-job/batch (reserved until Step 19), 10 invalid arguments, 11 package error, 12 model/data error,
  13 numerical failure or internal invariant violation (including a caught panic), 14 aborted by a configured limit, 15 cancelled,
  16 I/O. Parse errors are remapped to 10; help and `--version` exit 0.

The full command reference is in the [CLI reference](../reference/cli.md).

## Step ownership

| Area | Step |
| --- | --- |
| Workspace, payload/runtime types, reader, RNG, dry-run CLI | 13 |
| Simulation context, lattice/environment arenas, links, pools, jump status, goals, normalization | 14 |
| Energy engine, field terms, site/pair/cluster evaluation | 15 |
| KMC engine, pre-run, normalization, time step | 16 |
| MMC engine, equilibrium breaking | 17 |
| Trackers, histograms, statistics, `.mcs` writer | 18 |
| Production CLI, HPC helpers, extension ABI | 19 |
| Statistical parity, performance hardening, extended fuzzing | 29 |

Testing tiers, fixtures, and the mock policy are in [Testing and parity](testing-and-parity.md).
