# Step 11 — Simulation Package and `.msl` Writer

**Phase:** C. Compilation  
**Depends on:** 08, 09, 10
**Delivers:** A versioned SQLite simulation package with content-addressed compiled models and lattices,
version checks, atomic writes, and inspection tools.

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

Write the complete, reproducible input for the Rust solver: one immutable file containing shared compiled
models, per-job initial lattices, and settings. Results are written to a separate `.msr` store by Step 20.
The writer must be streaming enough for thousands of jobs and safe against partial/corrupt output.

## Original reference

- `.msl` is an EF Core SQLite database with separate tables for job packages, structure/energy/transition/
  lattice models, environments, pair/cluster tables, jump collections/directions, job metadata, and results.
- Large arrays are raw native structs (`Array_t`) in blobs; jobs reference shared model rows by integer IDs.
- The builder creates the DB, adds rows, saves changes, and embeds the project XML and build template XML.
- Hard limit: 99,999 jobs per package.
- Results are stored as MessagePack `.mcs` blobs in `JobResultData`.
- Journal mode is WAL during build, then switched to DELETE; `vacuum` is run on save.

The new format keeps SQLite (great for a single-file, transactional artifact) but uses a small, explicit,
versioned schema and cross-language payloads.

## Design

### 1. Schema

Defined in [`02-data-contracts.md`](../02-data-contracts.md):

- `schema_info` — format version, app version, project ID, created timestamp, generator.
- `models` — content-addressed compiled models (`signature`, `format_version`, Zstd+MessagePack payload).
- `lattices` — content-addressed initial lattices (`signature`, `format_version`, Zstd+MessagePack payload).
- `jobs` — one row per job with queryable metadata, seed, and a small job payload.
- No `results` table or `job_metadata` view. Results live in the `.msr` store (Step 20); scripts join the
  two databases.

Why content-addressed tables:

- A compiled model is shared by all jobs; storing it once avoids gigabytes of duplication.
- Duplicate jobs often have identical or near-identical lattices; hashing deduplicates exact matches.
- The solver can verify that the payload matches the signature before using it.
- Rebuilding with unchanged content produces the same signatures and enables diffing.

### 2. SQLite settings

- `application_id` = a fixed MOCASSIN identifier (e.g. `0x4D4F4353`, "MOCS").
- `user_version` = schema version (mirrors `schema_info`).
- Build with `journal_mode=WAL`, `synchronous=NORMAL`; on commit, checkpoint and switch to
  `journal_mode=DELETE` for a portable single file.
- Use one transaction per batch of ~100–1000 jobs, not one per job.
- `PRAGMA foreign_keys=ON`.
- `PRAGMA page_size=8192` (large blobs benefit from larger pages).
- `PRAGMA temp_store=MEMORY` during build.
- No `vacuum` by default; optional `--compact` runs `VACUUM`.

### 3. Writer pipeline

```csharp
public interface ISimulationPackageWriter : IAsyncDisposable
{
    Task WriteModelAsync(ParameterizedModel model, CancellationToken ct);
    Task WriteJobAsync(JobPackage job, CancellationToken ct);
    Task CompleteAsync(CancellationToken ct);
}

// JobPackage = Step 10 JobDescriptor + generated InitialLattice.
// JobDescriptor/JobPackage are build-time Packages types; serialized job/lattice DTOs live in Contracts.
public sealed record JobPackage(JobDescriptor Descriptor, InitialLattice Lattice);
```

Flow:

1. Create a temp file `project.msl.tmp-<pid>` in the target directory.
2. Open SQLite, set pragmas, create schema, write `schema_info`.
3. Compile/parameterize the model (step 08/09) once; serialize and insert the model row.
4. Stream job descriptors from step 10:
   - generate the lattice (step 10);
   - serialize the lattice; insert or reuse by signature;
   - serialize the job payload; insert the job row;
   - update progress and honor cancellation.
5. Flush, checkpoint WAL, switch journal mode, close.
6. `fsync` and atomically rename to the final path; optionally keep a `.bak`.
7. On any failure/cancel, delete the temp file and leave the previous package untouched.

The writer never holds all jobs/lattices in memory. Job generation is an `IAsyncEnumerable<JobPackage>` or a
pull-based iterator.

### 4. Payload encoding

- MessagePack maps with snake_case keys, Zstd level 3 (fast) or 9 (`--compact`).
- The model payload is the canonical `ParameterizedModel` from `Mocassin.Contracts` (compiled geometry/rules
  plus energy and frequency values, with the parameterization signature).
- The lattice payload is `{format_version, lattice_size, position_count, lattice}`.
- The job payload is the full `JobDescriptor`/settings (including optimizers) plus the model/simulation index.
  Its `flags` word is a `SolverJobFlags` value in `Mocassin.Contracts` carrying the authored flags plus the
  compiler/job-builder-derived bits (`useDualDofCorrection`, `usePrerun`, and the kind bit). This package layer
  composes the word from the `JobDescriptor`; `Mocassin.Compilation` does not reference Contracts, and the
  project JSON never contains the derived bits.
- The writer verifies round-trip decoding of every payload before insert (debug builds) or computes the
  canonical hash and compares it.
- Payloads are deterministic: same parameterized model → identical bytes → same signature.
- Package metadata records the symmetry contract version, space-group data version, and space-group data
  SHA-256 from `SymmetryVersionInfo` (`symmetry_contract_version`, `space_group_data_version`,
  `space_group_data_sha256`).

### 5. Metadata columns

Keep queryable fields in `jobs` so tools can filter without decoding payloads. These columns are **denormalized**;
the writer must keep them identical to the corresponding job/lattice payload values and `verify` must check it:

- job id, model id, lattice id, kind;
- collection/config names and indices;
- temperature, field modulus, target/pre-run MCSP, write-out count, time limit, min success rate;
- lattice A/B/C, doping info, flags, seed;
- created timestamp.

The writer always writes these columns from the resolved job payload; the SQL defaults are fallbacks only. The
`write_out_count` default is 100 to match the model default, but a resolved job may legitimately write 0 for a
final-state-only run.

Additional settings live in the job payload. The `.msr`/evaluation layer joins job metadata with results; the
package itself exposes no combined view.

### 6. Results boundary

- The package writer does **not** create, update, or embed results. `.msl` is immutable after publication.
- `mocassin collect` writes the separate `.msr` results store (Step 20): `status`, `exit_code`, timestamps,
  stdout, and `.mcs` blobs.
- The `.msr` stores SHA-256 of each state blob; the reader verifies it.
- A future external-results mode belongs to `.msr`, not `.msl`, and can store paths/hashes for very large HPC
  campaigns.
- A package without results is valid; evaluation requires a matching `.msr`.

### 7. Validation and inspection

`mocassin inspect <project.msl>` prints:

- schema/app version;
- model signatures and sizes;
- job count by kind/collection/config;
- lattice count and dedup ratio;
- temperature/field ranges;
- package-level integrity errors.

`mocassin verify <project.msl>` checks:

- SQLite integrity (`PRAGMA integrity_check`);
- schema version and required tables/columns;
- every job references an existing model/lattice;
- every payload decodes and its signature matches;
- every job id is unique/contiguous as expected;
- the package contains no `results` table and requires no `-wal`/`-shm` files.

Build-stage integration: package/supercell limits plug into the Step 07 pipeline as an `IValidationStage` with
`Stage = Build`, run only when `ValidationOptions.RunBuildStage` is true and earlier stages produced no errors,
and report through the shared `ValidationResult`/`DiagnosticCatalog`.

### 8. Package size and performance

Targets:

- 10,000 small jobs (10×10×10 ceria, ~1000 positions) in < 10 minutes and < 2 GB.
- Model payload < 50 MB for a complex model.
- Job insertion throughput ≥ 100 jobs/s after compilation.
- Deduplication report shows model/lattice sharing.

If the package exceeds a configurable size, the builder warns and suggests:
- fewer duplicates,
- smaller supercells,
- external result storage in `.msr` (future),
- `--compact` Zstd level.

## Implementation tasks

- [ ] Implement the schema creation and version metadata, including the symmetry version fields.
- [ ] Implement MessagePack/Zstd payload serializers and the canonical signature projection (ADR 0010).
- [ ] Implement content-addressed model/lattice insertion.
- [ ] Implement streaming job insertion with batching, progress, and cancellation.
- [ ] Implement atomic temp-file/rename and failure cleanup.
- [ ] Ensure the package schema contains no results table or combined metadata view.
- [ ] Implement `mocassin inspect` and `mocassin verify`.
- [ ] Implement package-size/dedup reporting.
- [ ] Add contract fixtures: one model, two lattices, several jobs.
- [ ] Add a Rust reader smoke test against the fixture (completed in step 13).

## Acceptance criteria

- [ ] A package built from the ceria sample opens in SQLite and contains the expected tables/rows.
- [ ] A published package opens read-only with no `-wal`/`-shm` files and has no results table.
- [ ] Every job references a valid model and lattice; signatures verify.
- [ ] Package metadata contains `symmetry_contract_version`, `space_group_data_version`, and
  `space_group_data_sha256`.
- [ ] Rebuilding the same project produces identical model/lattice signatures; stored compressed bytes may
  differ but the canonical projections must match.
- [ ] A cancelled build leaves no temp file and does not modify an existing package.
- [ ] A simulated disk-full/error leaves the previous package intact.
- [ ] `mocassin inspect` and `verify` report accurate information.
- [ ] 10⁴ small jobs build within the time/size budget.
- [ ] No EF Core or native structs are used.
- [ ] Documentation written

## Documentation

At this point, document the simulation package:

- `docs/reference/msl-format.md` — SQLite schema, content-addressed models/lattices, job payloads, versioning.
- `docs/reference/file-layout.md` — package location, immutable package rules, `.msr` sidecar, atomic-write
  behavior.
- `docs/user-guide/building.md` — what a package contains and how to inspect/verify it.
- `docs/development/architecture.md` — `Mocassin.Packages` responsibilities and boundaries.

## Tests

| Test | Assertion |
| --- | --- |
| Schema | Tables, columns, indices, pragmas, version |
| Round-trip | Payload encode/decode identity |
| Dedup | Same model/lattice inserted once |
| Atomicity | Cancel/fail leaves old package intact |
| Signatures | Model/lattice canonical projections and hashes verify |
| Immutable | Published package has no results table and needs no WAL files |
| Job IDs | Deterministic, unique, contiguous |
| Inspect/verify | Reports counts/errors correctly |
| Performance | Throughput and size budgets |
| Corruption | Verify detects truncated/corrupt payloads |

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| SQLite bloat for large campaigns | Content addressing, compression, `--compact`, external results in `.msr` later |
| MessagePack schema drift | Explicit versions, canonical tests, strict readers |
| Long transactions lock the file | Batch commits; build only in a temp file |
| Cancellation leaves partial data | Temp file + delete on failure; atomic rename |
| Determinism across SQLite versions | Hash payloads, not pages; test signatures |

## Done when

A valid, versioned `.msl` package can be built atomically from a project, inspected, verified, and read by the
Rust solver, with deterministic content and bounded memory.
