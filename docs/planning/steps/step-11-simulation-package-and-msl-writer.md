# Step 11 — Simulation Package and `.msl` Writer

**Phase:** C. Compilation  
**Depends on:** 08, 09, 10  
**Delivers:** A versioned SQLite simulation package with content-addressed compiled models and lattices,
version checks, atomic writes, and inspection tools.

## Goal

Write the complete, reproducible input for the Rust solver: one file containing shared compiled models,
per-job initial lattices and settings, and empty result rows. The writer must be streaming enough for
thousands of jobs and safe against partial/corrupt output.

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
- `results` — one row per job with status, exit code, stdout, and optional `.mcs` blobs.
- `job_metadata` view — stable surface for scripts and diagnostics.

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
    Task WriteModelAsync(CompiledModel model, CancellationToken ct);
    Task WriteJobAsync(JobPackage job, CancellationToken ct);
    Task CompleteAsync(CancellationToken ct);
}
```

Flow:

1. Create a temp file `project.msl.tmp-<pid>` in the target directory.
2. Open SQLite, set pragmas, create schema, write `schema_info`.
3. Compile/parameterize the model (step 08/09) once; serialize and insert the model row.
4. Stream job descriptors from step 10:
   - generate the lattice (step 10);
   - serialize the lattice; insert or reuse by signature;
   - serialize the job payload; insert the job row and a `pending` result row;
   - update progress and honor cancellation.
5. Flush, checkpoint WAL, switch journal mode, close.
6. `fsync` and atomically rename to the final path; optionally keep a `.bak`.
7. On any failure/cancel, delete the temp file and leave the previous package untouched.

The writer never holds all jobs/lattices in memory. Job generation is an `IAsyncEnumerable<JobPackage>` or a
pull-based iterator.

### 4. Payload encoding

- MessagePack maps with snake_case keys, Zstd level 3 (fast) or 9 (`--compact`).
- The model payload is the canonical `CompiledModel` from step 08.
- The lattice payload is `{format_version, lattice_size, position_count, lattice}`.
- The job payload is the full `JobDescriptor`/settings (including optimizers) plus the model/simulation index.
- The writer verifies round-trip decoding of every payload before insert (debug builds) or computes the
  canonical hash and compares it.
- Payloads are deterministic: same compiled model → identical bytes → same signature.

### 5. Metadata columns

Keep queryable fields in `jobs` so tools can filter without decoding payloads:

- job id, model id, lattice id, kind;
- collection/config names and indices;
- temperature, field modulus, target/pre-run MCSP, time limit, min success rate;
- lattice A/B/C, doping info, flags, seed;
- created timestamp.

Additional settings live in the job payload. The `job_metadata` view exposes the common fields for scripts.

### 6. Results table

- Insert `pending` rows at build time.
- `mocassin collect` updates `status`, `exit_code`, timestamps, stdout, and `.mcs` blobs.
- Store SHA-256 of each state blob; the reader verifies it.
- Version 1 stores state as BLOBs for portability. A future `--external-results` mode can store paths/hashes
  for very large HPC campaigns; the schema reserves `run_state_path`/`prerun_state_path` columns for this.
- Results are nullable until collected; a completed job without results is a valid package.

### 7. Validation and inspection

`mocassin inspect <project.msl>` prints:

- schema/app version;
- model signatures and sizes;
- job count by kind/collection/config;
- lattice count and dedup ratio;
- temperature/field ranges;
- results status counts;
- any integrity errors.

`mocassin verify <project.msl>` checks:

- SQLite integrity (`PRAGMA integrity_check`);
- schema version and required tables/columns;
- every job references an existing model/lattice;
- every payload decodes and its signature matches;
- every job id is unique/contiguous as expected;
- every result row references an existing job.

### 8. Package size and performance

Targets:

- 10,000 small jobs (10×10×10 ceria, ~1000 positions) in < 10 minutes and < 2 GB.
- Model payload < 50 MB for a complex model.
- Job insertion throughput ≥ 100 jobs/s after compilation.
- Deduplication report shows model/lattice sharing.

If the package exceeds a configurable size, the builder warns and suggests:
- fewer duplicates,
- smaller supercells,
- external result storage (future),
- `--compact` Zstd level.

## Implementation tasks

- [ ] Implement the schema creation and version metadata.
- [ ] Implement MessagePack/Zstd payload serializers with canonical encoding.
- [ ] Implement content-addressed model/lattice insertion.
- [ ] Implement streaming job insertion with batching, progress, and cancellation.
- [ ] Implement atomic temp-file/rename and failure cleanup.
- [ ] Implement result-row initialization and the `job_metadata` view.
- [ ] Implement `mocassin inspect` and `mocassin verify`.
- [ ] Implement package-size/dedup reporting.
- [ ] Add contract fixtures: one model, two lattices, several jobs.
- [ ] Add a Rust reader smoke test against the fixture (completed in step 13).

## Acceptance criteria

- [ ] A package built from the ceria sample opens in SQLite and contains the expected tables/rows.
- [ ] Every job references a valid model and lattice; signatures verify.
- [ ] Rebuilding the same project produces identical model/lattice signatures and payload hashes.
- [ ] A cancelled build leaves no temp file and does not modify an existing package.
- [ ] A simulated disk-full/error leaves the previous package intact.
- [ ] `mocassin inspect` and `verify` report accurate information.
- [ ] 10⁴ small jobs build within the time/size budget.
- [ ] No EF Core or native structs are used.
- [ ] Documentation written

## Documentation

At this point, document the simulation package:

- `docs/reference/msl-format.md` — SQLite schema, content-addressed models/lattices, job payloads, versioning.
- `docs/reference/file-layout.md` — package location, result rows, atomic-write behavior.
- `docs/user-guide/building.md` — what a package contains and how to inspect/verify it.
- `docs/development/architecture.md` — `Mocassin.Packages` responsibilities and boundaries.

## Tests

| Test | Assertion |
| --- | --- |
| Schema | Tables, columns, indices, pragmas, version |
| Round-trip | Payload encode/decode identity |
| Dedup | Same model/lattice inserted once |
| Atomicity | Cancel/fail leaves old package intact |
| Signatures | Model/lattice hashes verify |
| Job IDs | Deterministic, unique, contiguous |
| Inspect/verify | Reports counts/errors correctly |
| Performance | Throughput and size budgets |
| Corruption | Verify detects truncated/corrupt payloads |

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| SQLite bloat for large campaigns | Content addressing, compression, `--compact`, external results later |
| MessagePack schema drift | Explicit versions, canonical tests, strict readers |
| Long transactions lock the file | Batch commits; build only in a temp file |
| Cancellation leaves partial data | Temp file + delete on failure; atomic rename |
| Determinism across SQLite versions | Hash payloads, not pages; test signatures |

## Done when

A valid, versioned `.msl` package can be built atomically from a project, inspected, verified, and read by the
Rust solver, with deterministic content and bounded memory.
