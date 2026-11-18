# Simulation Package Format (`.msl`)

A simulation package is one immutable SQLite database, `*.msl`, that contains the shared compiled model, the
initial lattice of every job, and each job's resolved settings. It is the complete, reproducible input for the
Rust solver; the solver never reads project JSON. Results live in a separate `.msr` store (Step 20), never in the
package.

The machine-readable source is [`contracts/manifest.json`](https://github.com/seb-eis/mocassin-2/blob/main/contracts/manifest.json)
and the generated schemas under `contracts/generated/schema/`; this page explains the v1 contract and its rules.

## Package identity

| Property | Value |
| --- | --- |
| SQLite `application_id` | `0x4D4F4353` (`MOCS`) |
| SQLite `user_version` | `1` |
| `schema_info.format_version` | `1` |
| Page size | 8192 bytes |
| Published journal mode | `DELETE` (no `-wal`/`-shm` files) |
| Results table | None; a package with a `results` table fails verification (`PKG008`) |

`schema_info` carries `format_version`, `app_version`, `created_utc`, `project_id`, `generator`, `job_selection`,
`symmetry_contract_version`, `space_group_data_version`, and `space_group_data_sha256`. `app_version`,
`created_utc`, and `generator` are build metadata: they are excluded from the canonical summary and may differ
between two logically identical builds.

## Schema

```sql
CREATE TABLE schema_info (key TEXT PRIMARY KEY, value TEXT NOT NULL);

CREATE TABLE models (
    model_id       INTEGER PRIMARY KEY,
    signature      TEXT    NOT NULL UNIQUE,
    format_version INTEGER NOT NULL,
    payload        BLOB    NOT NULL
);

CREATE TABLE lattices (
    lattice_id     INTEGER PRIMARY KEY,
    signature      TEXT    NOT NULL UNIQUE,
    format_version INTEGER NOT NULL,
    payload        BLOB    NOT NULL
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
    seed              BLOB    NOT NULL,
    payload           BLOB    NOT NULL,
    created_utc       TEXT    NOT NULL,
    UNIQUE (collection_index, config_index, job_index)
);

CREATE INDEX ix_jobs_temperature ON jobs(temperature_k);
CREATE INDEX ix_jobs_collection  ON jobs(collection_name, config_name);
```

The job table has two kinds of columns: **payload-derived** columns (`model_id`, `lattice_id`, `kind`,
`temperature_k`, `electric_field_vm`, `target_mcsp`, `write_out_count`, `pre_run_mcsp`, `time_limit_s`,
`min_success_rate`, `lattice_a/b/c`, `doping_info`, `flags`) and **authored/build metadata** (`collection_name`,
`config_name`, the three indices, `seed`, `created_utc`). Verification re-reads the payload and proves that every
payload-derived column equals its payload value; the metadata columns are checked for presence, format, and
uniqueness instead, because the payload has no independent copy.

## Payload encoding

Every `payload` blob is a standard Zstandard frame around one MessagePack map:

1. The contract DTO is serialized with `MessagePackSerializer` and `MessagePackSecurity.UntrustedData`.
2. The bytes are encoded by the in-box `ZstandardEncoder` in exactly one path: `ZstandardCompressionOptions {
   Quality = level }`, `SetSourceLength(messagePack.Length)`, `GetMaxCompressedLength` destination, and
   `Compress(..., isFinalBlock: true)`. The frame declares its exact decompressed size.
3. No dictionary, checksum, long-distance matching, custom framing, or streaming encoder is used. A `.msl`
   payload is one frame per blob.

`PackageBuildOptions.CompressionLevel` is the Zstd quality and maps **by identity** to the native quality in the
supported range `1..19`; the default is `3`. The level is an authoring choice, not part of the format.

Decoding is frame-driven, so a reader accepts any valid standard frame, including one written by a different
codec implementation or a future runtime patch. It reads the declared size (or the frame's upper bound when no
content size is declared), rejects an empty blob, a non-positive guard, and a bound above the guard **before
allocating**, and then deserializes MessagePack with untrusted-data security. Model and job guards are 512 MiB and
8 MiB. A lattice payload is bounded by `4 × a × b × c × position_count` from its referencing job and the model's
`position_count`, always capped by `PackageVerificationOptions.MaxLatticePayloadBytes` (default 512 MiB); an
orphan lattice row uses that cap. Compressed bytes are an implementation detail: ADR 0010 and the data contracts
exclude them from signatures and from the canonical summary.

## Content addressing

- `models.signature` is the Step 09 parameterization signature of the exact applied parameterization. It is the
  content address of the model payload and is re-derived at write time.
- The payload's `model_signature` is the Step 08 compiled-model signature. It is carried inside the model
  payload and is an identity string for readers.
- `lattices.signature` is `LatticeSignatureV1`: a MessagePack projection of `model_signature`, `lattice_size`,
  `position_count`, and the particle array. Byte-identical lattices insert one row and are shared by every job;
  a different particle array inserts a second row.
- Jobs reference `model_id` and `lattice_id`. A package has exactly one model row in v1; the schema does not
  forbid more, but the writer inserts one per build.

The lattice index order is a contract: `index = ((a·B + b)·C + c)·P + p`, where `B`, `C` are the requested
supercell sizes and `P` the canonical position count per unit cell. Particle indices are compiled particle
indices, not IDs.

## Payload contents

The full field list is in `contracts/generated/schema/package-payloads-v1.schema.json`; the groups are:

| Payload | Contents |
| --- | --- |
| Model | `format_version`, `signature` (parameterization), `model_signature` (compiled identity), particles, structure (positions, sites, cell vectors, environments with **one pair entry per periodic image** and its relative integer `offset`), energy (pair/cluster tables, site energies, defect background), transitions (jump collections, rules with variable-length `tracker_order` and `0xFF` sentinels), simulations |
| Lattice | `format_version`, `lattice_size` (`[a,b,c]`), `position_count`, and the particle array (`a·b·c·position_count` bytes, row-major per the index rule; a MessagePack array of `u8` in v1, not `bin`) |
| Job | `format_version`, simulation index, kind, resolved settings (temperature, field modulus and Cartesian unit direction, MCSP targets, sampling, attempt frequency, normalization energy, tolerances, time limits), the composed `flags` word, compiled selection optimizers, canonical extension JSON, and `doping_info` |

`flags` has exactly one kind bit (KMC or MMC); the remaining bits encode `usePrerun` and
`useDualDofCorrection` as composed by `SolverJobFlagsMapper`. The bit assignments are in the manifest; do not
hand-write the word.

## Canonical summary (`package-summary-v1`)

`MslPackageInspector.BuildCanonicalSummaryAsync` projects a package to `package-summary-v1`, a normalized JSON
document used for determinism checks, fixtures, and the benchmark evidence. It contains:

- `format`/`formatVersion` and the `schema_info` logical fields (`formatVersion`, `projectId`, `jobSelection`, and
  the symmetry/space-group provenance);
- the model projection (signature, `modelSignature`, payload format, particle/position/site/environment/pair/
  cluster/collection/rule counts);
- the lattice projection (signature, `latticeSize`, `positionCount`, `latticeLength`);
- the job projection (id, model/lattice signatures, kind, names and indices, temperature, field, MCSP and sampling
  settings, lattice size, `dopingInfo`, `flags`, and the seed in hex);
- `counts` (models, lattices, jobs, lattice reuse factor).

Build metadata (`app_version`, `created_utc`, `generator`), file size, and payload byte hashes are deliberately
excluded, so the summary is stable across hosts, runtime patches, and rebuilds. It is the golden for fixtures and
must never be generated from a hand-edited package.

## Versioning and compatibility

- `.msl` v1 has never been released. Pre-release shape changes are finalized at `format_version = 1` with the
  contract artifacts regenerated in the same change (ADR 0009's pre-release exception).
- The first released major starts ADR 0013's current + previous-major read policy. `.mcs` is current-major only;
  `.msl` readers accept the current and the immediately previous major once a release exists.
- A codec or runtime change does not bump the format: frames are standard Zstd and the logical payloads,
  signatures, and canonical summary are codec-independent.
- Old packages remain readable: a package written before the .NET 11/native-Zstandard switch verifies and
  produces the same canonical summary as an equivalent new build. The checked-in
  [`amend-1-net10`](https://github.com/seb-eis/mocassin-2/tree/main/tests/contracts/packages/amend-1-net10)
  fixture proves it on every test run.

## Solver read path (Step 13)

The Rust solver opens the package read-only and validates only what it consumes. It does not run the ten-check
verifier; `mocassin verify` remains the full structural check.

1. **Open.** `SQLITE_OPEN_READONLY` with no URI handling, `query_only=ON`, `foreign_keys=ON`, and a bounded
   `busy_timeout`. A missing/unreadable path is an I/O error; a corrupt database is a package error.
2. **Identity.** Require `application_id = 0x4D4F4353`, `user_version = 1`, and
   `schema_info.format_version = 1`. Require the `schema_info`, `models`, `lattices`, and `jobs` tables and the
   columns the reader uses. Read the provenance rows and require them to equal the model payload's
   `symmetry_contract_version`, `space_group_data_version`, and `space_group_data_sha256`.
3. **Decode.** Every payload is one standard Zstd frame. The reader checks the declared decompressed size (or
   streams with a hard limit when no size is declared) against a byte guard before allocating. It then scans
   the MessagePack document without allocating per element, enforcing the element budget, nesting depth,
   declared-length-vs-remaining-bytes, duplicate known keys, and per-field count caps. Only then does it decode
   into typed structures.
4. **Validate.** Every particle/position/site/environment/pair/cluster/collection/direction/rule/tracker index
   and length; state-code node values; `tracker_order` length; jump-count prefix sums; kind and flag
   consistency; finite values; normalized field direction; lattice length and particle indices.
5. **Signatures.** The reader re-encodes the six-field parameterization projection and the five-field lattice
   projection with the ADR 0010 fixed-width writer, the C# 12-decimal quantization expression, and the domain
   prefix. It compares both to the stored signatures. `model_signature` is an identity string and is not
   re-encoded.
6. **Job.** The job row's payload-derived columns are compared with the payload; `seed` must be 32 bytes. The
   result is a validated `JobPackage` for Step 14.

Stored payload integers use MessagePack's smallest-width encoding; the fixed-width `uint64`/`int64`/`float64`
rule applies only to the canonical projection. The lattice member is a MessagePack array of `u8` in v1; the
canonical lattice projection uses `bin`. Readers accept both forms.

Default solver limits are 512 MiB per model/lattice payload, 8 MiB per job payload, 16M model / 1M job
MessagePack elements, depth 8, 500,000 total positions, 10,000 P1 positions per cell, 64 particles, 1M
rules/directions, 4,096 collections/simulations, 1M pair/cluster tables, 16M pair links, 10 path nodes, and
256 extensions. The position/P1/rule limits match the current .NET build limits; a future writer that raises
them must raise `ReaderLimits` in the same release. A future package major version, a payload above a guard, or
a document that exceeds a budget is
rejected with an actionable message before unbounded allocation. See
[Solver architecture](../development/solver-architecture.md) for the crate view and the full limit table.

## Verification and immutability

`PackageVerifier.VerifyAsync` runs ten ordered checks and reports `PKG001`–`PKG010` with the documented
severities; see [Diagnostics](diagnostics.md). In summary: readable SQLite and `integrity_check`; identity and
`schema_info`; references and job IDs; payload decode inside the bounds; model invariants; parameterization and
lattice signature re-encoding; payload-derived column equality; kind/flag consistency; and the advisory size
warning. A package is immutable after publication: verification never mutates it, and any later derived data
belongs in `.msr`.

Publication is atomic (temp file beside the target, WAL during the build, checkpoint to `DELETE`, fsync file and
Unix directory, then rename); see [Repository and file layout](file-layout.md).

## Fixtures

| Directory | Purpose |
| --- | --- |
| `tests/contracts/packages/msl-v1/` | Checked-in `.msl` v1 written by the amended .NET 11 writer, with its `package-summary-v1` golden, extracted payloads, and the regeneration command |
| `tests/contracts/packages/amend-1-net10/` | Pre-.NET 11 compatibility package, byte-fixed and never regenerated |
| `tests/contracts/packages/amend-1/` | Pre-amendment codec goldens (raw payloads and ZstdSharp frames at levels 3 and 9) |

The `.msl` bytes are a writer+runtime artifact. The comparable artifacts are the canonical summary, the payload
signatures, and the recorded SHA-256; see
[Testing and parity](../development/testing-and-parity.md) for the regeneration rule.
