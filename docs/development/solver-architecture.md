# Solver Architecture

The Monte Carlo solver is a separate Rust process, `mocassin-sim`. It consumes only the immutable simulation
package (`.msl`) and command-line arguments. It never reads project JSON and never writes `.msl` or `.msr`.
Results are written as per-job state files, which a separate `mocassin collect` command stores in the `.msr`
sidecar.

This page describes the Step 13 foundation: the crate graph, the package-reader pipeline, the runtime data
model, the RNG streams, and the CLI skeleton. Steps 14–19 add the simulation context, energy engine, KMC/MMC
loops, tracking, state writing, and the production CLI.

## Crate graph

```text
mocassin-cli
   ├──► mocassin-msl ──► mocassin-types
   ├──► mocassin-rng
   └──► mocassin-types
mocassin-solver (Step 14+)
   ├──► mocassin-types
   ├──► mocassin-msl
   ├──► mocassin-rng
   ├──► mocassin-state
   └──► mocassin-extension
```

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
- The CLI uses the reader and RNG directly for inspection; the solver core is a library and does not own a
  second reader.
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
   match. The reader requires the tables and columns it uses and the nine `schema_info` keys, and checks the
   symmetry/space-group provenance against the model payload.
3. **Decode.** Each payload is one standard Zstd frame. The reader checks the declared decompressed size (or
   streams with a hard limit when none is declared) against a byte guard before allocating. The MessagePack
   scan walks the document without allocating per element and rejects an element budget, depth, declared length,
   duplicate known key, or per-field count violation. Only then is the typed document decoded.
4. **Validate.** Every index and length in the particle, structure, environment, energy, transition, simulation,
   job, and lattice sections is checked; NaN/Inf is rejected where the schema requires a finite value; state
   codes and tracker entries are checked against the particle count and path length; jump-count prefix sums,
   kind bits, and normalized field directions are checked.
5. **Signatures.** The six-field parameterization projection and the five-field lattice projection are
   re-encoded with the fixed-width canonical writer and compared with the stored signatures. The compiled-model
   signature is an identity string and is not re-encoded.
6. **Job.** The job row's payload-derived columns must equal the payload values; the seed must be 32 bytes. The
   result is a validated `JobPackage`.

The reader does **not** run `PRAGMA integrity_check` and does **not** mirror the `PKG` diagnostics. Those belong
to `mocassin verify`; the solver checks only the data it consumes. The package format and its fields are in the
[simulation package format](../reference/msl-format.md).

## Read limits

| Limit | Default |
| --- | ---: |
| Model / job / lattice payload | 512 MiB / 8 MiB / 512 MiB |
| Model / job MessagePack elements | 16,000,000 / 1,000,000 |
| MessagePack depth | 8 |
| Total positions (`A*B*C*P`) | 100,000,000 (also ≤ signed 32-bit linear index) |
| P1 positions per cell | 1,000,000 |
| Particles | 64 |
| Rules / directions / collections | 10M / 10M / 4,096 |
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
- `JobPackage`: job id, kind, shared model, initial lattice, settings, effective seed, optimizers, extensions,
  instruction, and doping summary.

Hot data uses flat arrays, `u32`/`u8` indices, and `u64` masks. `HashMap` is not used in a per-cycle structure.
The model cache may use a hash map; it is not a hot path. All hot arrays are allocated at load/initialization;
Step 14 owns the no-allocation-in-cycle proof.

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
- Changing the cipher, derivation, draw semantics, or state layout is format-breaking and needs a new ADR and a
  new algorithm id.

The RNG benchmark measures draws per second, 64-word refill cost, state round-trip, and fork cost. The budgets
are recorded in the Step 13 design and the test evidence; a regression above 10 % fails the release gate.

## CLI foundation

Step 13 implements the inspection path:

```text
mocassin-sim --db <project.msl> --job <id> --dry-run [--seed <hex32>] [--log-level ...] [--log-format ...]
```

- `--dry-run` loads, validates, and prints a stable summary (schema, model/lattice/job counts and signatures,
  settings, transition/tracker counts, RNG algorithm and seed hash). It writes nothing: it does not create the
  I/O directory, does not write a log file, and rejects `--stdout`.
- A non-dry-run invocation is a usage error until the simulation engines land; Step 16 enables a run and Step 19
  completes the option surface (`batch`, `--validate-only`, `--no-state`, `--log-file`, extensions).
- Logs go to stderr (`tracing`, human or JSON). stdout is reserved for the human report.
- Exit codes: 0 success, 10 invalid arguments, 11 package error, 12 model/data error, 13 numerical failure,
  14 limit abort, 15 cancelled, 16 I/O. Parse errors are remapped to 10; help and `--version` exit 0.

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
