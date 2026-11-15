# Step 11 — Simulation Package and `.msl` Writer

**Phase:** C. Compilation  
**Depends on:** 08, 09, 10
**Delivers:** A versioned SQLite simulation package containing one parameterized model, content-addressed
initial lattices, and streamed job payloads, with a verified publication barrier, contract fixtures, and a
read-only inspection/verification library for the build CLI.

## Implementation status

- **Status:** Not started
- **Step branch:** —
- **Step CI:** —
- **Main merge:** —
- **Step tag:** —
- **Audit:** Not requested
- **Design review:** Ready for review — [request record](../reviews/step-11.md)
- **Feature list:** Proposed 2026-11-15 (seven features; awaiting start-gate approval)
- **Last updated:** 2026-11-15

| Feature | Branch | Status | Local gates | Step CI | Squash commit |
| --- | --- | --- | --- | --- | --- |
| — | — | — | — | — | — |

### Proposed feature list

| # | Feature | Branch | Planned scope |
| --- | --- | --- | --- |
| 1 | Package contracts and lattice signature | `feature/step-11-simulation-package-and-msl-writer/package-contracts-and-lattice-signature` | `Mocassin.Contracts.Packages` DTOs for the model, job, and lattice payloads; payload format constants; `LatticeSignatureV1`; `PKG` diagnostic descriptors; `PublicAPI.Unshipped.txt`; golden lattice-projection vectors and the projection checker |
| 2 | Package manifest and schema | `feature/step-11-simulation-package-and-msl-writer/package-manifest-and-schema` | Finalize `signatures.lattice`; add `formats.package.payloads` top-level descriptors; remove the undefined job columns; extend `tools/contracts/generate.py`; regenerate the package schema/constants/descriptors; register fixture descriptors |
| 3 | SQLite schema and publication barrier | `feature/step-11-simulation-package-and-msl-writer/sqlite-schema-and-publication` | Writer options/factory, schema creation, pragmas, `application_id`/`user_version`, `schema_info`, batching, WAL→DELETE, fsync file and directory, atomic rename, cancellation/failure cleanup, result SHA-256 |
| 4 | Model payload encoding | `feature/step-11-simulation-package-and-msl-writer/model-payload-encoding` | `ParameterizedModel` → Contracts model payload (particles, structure, energy, transitions, simulations); Zstd+MessagePack; content-addressed insert; write-time signature re-encode; unit/property tests |
| 5 | Job and lattice payload encoding | `feature/step-11-simulation-package-and-msl-writer/job-and-lattice-encoding` | `GeneratedLattice` → `InitialLattice`; `JobDescriptor` → job payload (flags composition, Cartesian field direction, optimizers, extension JSON, doping summary); lattice dedup; denormalized columns; progress/cancellation |
| 6 | Package verification and inspection | `feature/step-11-simulation-package-and-msl-writer/package-verification` | Read-only `MslPackageInspector`/`PackageVerifier` in `Mocassin.Results.Packages`; bounded decode; `PKG` checks; reports; corruption/truncation/decompression-bomb tests |
| 7 | Fixtures, performance, documentation, and closeout | `feature/step-11-simulation-package-and-msl-writer/fixtures-performance-and-docs` | Checked-in `.msl` fixtures plus canonical summaries and extracted payloads; the 10⁴-job budget evidence; reference/developer pages; parity and status updates; closeout evidence and audit request |

The feature list is a design proposal. It is approved once by the human owner together with the design verdict;
it does not create the step branch and does not mark the step `In progress`.

### Feature log

Filled during implementation. Each squash records the branch, local gates, and the squash commit; no CI runs
per feature.

## Goal

Write the complete, reproducible input for the Rust solver: one immutable file containing the shared
parameterized model, per-job initial lattices, and settings. Results are written to a separate `.msr` store by
Step 20. The writer must be streaming enough for thousands of jobs and safe against partial or corrupt output,
and a read-only verifier must be able to prove that a published package is internally consistent.

## Scope

**In scope**

- The `.msl` v1 SQLite schema, pragmas, publication barrier, and the `models`/`lattices`/`jobs` tables.
- The Contracts model, job, and lattice payload DTOs and their Zstd+MessagePack encoding.
- The mapping from the real Steps 08–10 types (`CompiledModel`, `ParameterizedModel`, `ParameterizationValues`,
  `JobDescriptor`, `GeneratedLattice`, `DopingReport`, `CompiledOptimizers`, `SolverJobFlagsMapper`).
- The finalization of the lattice signature projection under ADR 0010's pre-release exception, and the
  parameterization-signature re-encode that proves the stored model payload is self-consistent.
- Content addressing and deduplication of models and lattices, streaming job insertion, and bounded memory.
- The package contract manifest entries, generated schema, fixture descriptors, and checked-in `.msl` fixtures.
- A read-only inspection/verification library over a published package, including `PKG` diagnostics.
- Reference and developer documentation named below.

**Non-goals**

- No CLI command. `mocassin build`, `inspect`, `verify`, `--jobs`, `--parallel`, `--force`, and progress
  events belong to Step 12, which consumes this step's writer and verifier (`AGENTS.md` §5; Step 12 design).
- No results storage, no `.msr` schema, no `.mcs` decoding, and no evaluation queries (Steps 18–20).
- No Rust reader (Step 13). This step only produces the contract and a fixture the reader consumes.
- No project JSON, parameterization-template, job-template, or supercell changes. Steps 06/09/10 own those.
- No `IValidationStage`. Step 12 owns the Build-stage adapter; this step returns a build report and warnings.
- No append-to-existing-package path, no external-results mode, and no `.bak` backup file (all deferred, ADR
  0011 / `05-open-decisions.md`).
- No compression of the `.mcs` state format and no custom binary header anywhere: `.msl` payloads are standard
  MessagePack maps inside standard Zstd frames (ADR 0004/0010).

## Dependency and interface reconciliation

Steps 08, 09, and 10 are `Done` and merged (`945dddf` is the Step 10 merge). The following cross-step
interfaces were checked against the shipped code at the design commit and are the inputs this step may rely on:

| Step | Shipped interface | Step 11 use | Result |
| --- | --- | --- | --- |
| 08 | `CompiledModel` header (`ModelSignature`, `SymmetryContractVersion`, `SpaceGroupDataVersion`, `SpaceGroupDataSha256`) and the particle/structure/energy/transition/simulation tables | Model payload mapping | consistent |
| 08 | `CompiledStructureModel.CellVectors`, `InteractionRange`, `Positions[].AllowedParticleMask`, `Environments`, `CompiledTransitionModel.JumpCounts/JumpOffsets/JumpMappings/GlobalTrackers/StaticTrackers` | Structure/transition payload; row-major jump arrays; tracker definitions; Cartesian field direction | consistent |
| 08 | `CompiledJumpRule.FrequencyHz` is the compiled default (`1e13 Hz`); `ParameterizationValues` is the only applied-value source | Absolute `jump_rules[].frequency_hz` comes from Step 09, never from the compiled default or a factor × maximum | consistent |
| 08 | `CompiledIndexMaps` and stable ID strings are inspection-only | Not persisted except the `jump_directions[].object_id` that has a compiled source | consistent |
| 09 | `ParameterizedModel(CompiledModel, ParameterizationValues, Signature)` with `ModelSignature` | `models.signature` = `Signature`; payload `signature`/`model_signature`; applied tables | consistent |
| 09 | `ParameterizationSignatureV1.Compute(modelSignature, values)` and its six-field projection | Writer self-check and verifier re-encode | consistent |
| 09 | Implicit default parameterization for `parameterizations: []`; `Apply` returns dense values | Packages built from samples with no template are valid | consistent |
| 09 | `PRM` validation is called by Step 12 after compile; no Step 09 validation stage | Step 11 does not validate templates; it encodes the applied result | consistent |
| 10 | `JobDescriptor` (1-based `JobId`, collection/config/job indices and names, `SimulationIndex`, `Kind`, `UseDualDofCorrection`, `Settings`, `Seed`, `CompiledOptimizers`) | Job row, payload, flags | consistent |
| 10 | `ResolvedJobSettings` with nullable limits and authored `ExecutionFlags` | Job payload settings | consistent |
| 10 | `GeneratedLattice(SizeA/B/C, PositionCount, Particles, Report, DopingInfo)` | `InitialLattice` payload, `doping_info` in the job payload and column | consistent |
| 10 | `SolverJobFlagsMapper.Compose` in `Mocassin.Packages.Jobs` | Composed `flags` word | consistent |
| 10 | Row-major layout `((a*B + b)*C + c)*PositionCount + P` | Lattice payload and signature | consistent |
| 10 | `SupercellGenerator` is the only lattice generator | The caller (Step 12 or the acceptance harness) pairs descriptor+lattice; the writer never generates | clarified in this design |
| 02 | `SymmetryVersionInfo` (`symmetry-1`, catalog version, catalog SHA-256) | `schema_info` and model payload provenance | consistent |

Cross-step statements corrected by this design (no behaviour change, only stale text):

- Step 06 said the solver normalizes the fractional field direction; the data contract already stores the
  normalized Cartesian vector in the job payload, which is what Step 15 consumes. Step 11 performs the
  conversion (see §3) and Step 06 now points at the package contract.
- Step 06 said a positive `MaxAttemptFrequencyHz` "rescales all frequencies"; since Step 09 the model stores
  absolute rule frequencies and Step 16 derives a per-job factor. Step 06 now matches Steps 09/16.
- Step 13's Rust `ParameterizedModel` lacked the symmetry provenance fields and used `particles` for the payload
  key `particle`. Step 13 is corrected to the contract.
- Step 12 and this page both claimed the `inspect`/`verify` commands. The commands stay in Step 12; this step
  owns the library they call.

## Original reference

- `.msl` is an EF Core SQLite database with separate tables for job packages, structure/energy/transition/
  lattice models, environments, pair/cluster tables, jump collections/directions, job metadata, and results.
- Large arrays are raw native structs (`Array_t`) in blobs; jobs reference shared model rows by integer IDs.
- The builder creates the DB, adds rows, saves changes, and embeds the project XML and build template XML.
- Hard limit: 99,999 jobs per package.
- Results are stored as MessagePack `.mcs` blobs in `JobResultData`.
- Journal mode is WAL during build, then switched to DELETE; `vacuum` is run on save.

The new format keeps SQLite (a single-file, transactional artifact) but uses a small, explicit, versioned
schema, standard MessagePack payloads, and Zstd frames, with no native structs and no results table
(ADR 0004, 0010, 0011).

## Design

### 1. Schema, pragmas, and version metadata

The schema is the one in [`02-data-contracts.md`](../02-data-contracts.md) §3.2, with these Step 11
finalizations (recorded as contract deltas in §6):

- `application_id = 0x4D4F4353` (`"MOCS"`), `user_version = 1`, `schema_info.format_version = '1'`.
- `schema_info` rows: `format_version`, `app_version`, `created_utc`, `project_id`, `generator`,
  `job_selection`, `symmetry_contract_version`, `space_group_data_version`, `space_group_data_sha256`.
  `app_version` is the entry assembly's informational version and `generator` is `mocassin .NET <app_version>`;
  `created_utc` is `PackageBuildRequest.CreatedUtc`, never read from the clock.
- The `jobs` table keeps only build-time or payload-derived values. `norm_factor` and `base_frequency_hz` are
  removed because no build-time value exists for them and Step 16 owns every derived factor.
- No `results` table, no `job_metadata` view, no legacy table.

Build-time pragmas:

- `journal_mode=WAL`, `synchronous=NORMAL`, `foreign_keys=ON`, `page_size=8192` (before the first table),
  `temp_store=MEMORY`, `application_id`, `user_version`.
- One transaction per `BatchSize` jobs (default 500), not one per job.
- On completion: `PRAGMA wal_checkpoint(TRUNCATE)`, `PRAGMA journal_mode=DELETE`, `PRAGMA optimize`, close.
- No `VACUUM` by default; `PackageBuildOptions.Vacuum` (Step 12's `--compact`) runs it before publication.

One model row per package in v1. The schema keeps `jobs.model_id` as a foreign key so a later version can
store several parameterizations of one compiled model, but `WriteModelAsync` is called exactly once; a second
call with a different signature is a contract error. This matches the build (one selected parameterization per
package) and removes the ambiguity of which model a job references.

### 2. Writer API and publication pipeline

```csharp
public sealed record JobPackage(JobDescriptor Descriptor, GeneratedLattice Lattice);

public sealed record PackageBuildRequest(
    Guid ProjectId,
    string JobSelection,          // "all" or "1-100,250"; validated non-empty
    DateTimeOffset CreatedUtc);   // supplied, never read from the clock

public sealed record PackageBuildOptions
{
    public bool Overwrite { get; init; }
    public int CompressionLevel { get; init; } = 3;   // 1..19
    public bool Vacuum { get; init; }
    public int BatchSize { get; init; } = 500;
    public long AdvisorySizeBytes { get; init; } = 2L * 1024 * 1024 * 1024;
}

public sealed record PackageWriteResult(
    string Path,
    string Sha256,                // SHA-256 of the exact published bytes
    int JobCount,
    int ModelCount,
    int LatticeCount,
    long SizeBytes,
    ImmutableArray<PackageWarning> Warnings);

public interface ISimulationPackageWriter : IAsyncDisposable
{
    Task WriteModelAsync(ParameterizedModel model, CancellationToken cancellationToken = default);
    Task WriteJobAsync(JobPackage job, CancellationToken cancellationToken = default);
    Task<PackageWriteResult> CompleteAsync(CancellationToken cancellationToken = default);
}
```

`SimulationPackageWriter.Create(PackageBuildRequest request, PackageBuildOptions options)` is the factory
(`Mocassin.Packages` has no DI container yet; the module marker stays a marker). Every call performs bounded
synchronous SQLite/encode work on the caller's task; there is no fake `Task.Run`.

Pipeline:

1. Validate the target path; if it exists and `Overwrite` is `false`, fail before creating anything.
2. Create `<target>.msl.tmp-<pid>-<random>` in the target directory (matching the `.gitignore` pattern) and open
   SQLite on it.
3. Set pragmas, create the schema, and write `schema_info` once the model is known (the symmetry provenance
   comes from the compiled model).
4. Compile/parameterize the model once (Steps 08/09 by the caller); `WriteModelAsync` maps it, encodes the
   payload, re-encodes the parameterization projection from the payload it just built, compares it with
   `ParameterizedModel.Signature`, and inserts the row. A mismatch is a contract error, not a diagnostic.
5. Stream `JobPackage` items from the caller:
   - map the lattice, encode it, and insert/reuse the row by signature;
   - map the job, derive the `SolverJobFlags` word, encode it, and insert the row with the denormalized
     columns (all job rows carry the request's `created_utc`);
   - commit every `BatchSize` jobs and honor cancellation between batches.
6. `CompleteAsync`: checkpoint, switch to DELETE, optionally `VACUUM`, close, hash the temp file, `fsync` it and
   the containing directory, then atomically rename over the target.
7. `DisposeAsync` without `CompleteAsync` deletes the temp file. Any exception deletes the temp file and leaves
   an existing package untouched. Orphaned `.msl.tmp-*` files from a crashed process are inert; Step 12's
   `clean` may remove them.

The writer never holds all jobs or lattices in memory. `WriteJobAsync` requires a model first and
`CompleteAsync` requires exactly one model and at least one job; both are contract guards
(`InvalidOperationException`), because Step 12 has already reported user-data errors.

### 3. Payload encoding

All payloads are MessagePack maps with snake_case keys, encoded with `MessagePack-CSharp` using explicit
`[Key("...")]` names (no contractless resolver) and `MessagePackSecurity.UntrustedData`, then compressed with a
standard Zstd frame (`ZstdSharp.Port`; no dictionaries, no custom framing). Readers ignore unknown keys within
the current major and reject an unsupported `format_version`.

- **Model payload** — the §3.3 map. `format_version = 1` is the payload-encoding version, independent of
  `CompiledModel.FormatVersion`. The mapping is mechanical and one-way:
  - `signature` = `ParameterizedModel.Signature`; `model_signature` = `CompiledModel.ModelSignature`; the three
    symmetry fields come from the `CompiledModel` header.
  - `particle.charges/symbols/is_vacancy/count` come from `CompiledModel.Particles` in dense index order, void
    at index 0.
  - `structure.cell_vectors_ang` is `[a, b, c]` in ångström, so `fractional → Cartesian` is
    `fa·a + fb·b + fc·c`. `positions` are ascending `P` with `vector4 = [cellA, cellB, cellC, P]`,
    `site_index`, `stable`, and `particle_ids` = the ascending compiled indices set in
    `AllowedParticleMask`. `environments` carry `selection_particle_mask`, pair links
    `{target_position, energy_id}` (`energy_id` is the shared pair table index), cluster links
    `{cluster_id, byte_position}` (`byte_position` is the cluster site slot), and `update_particle_ids`.
    `static_trackers_per_cell = position_count × (particle_count − 1)`;
    `global_tracker_count = CompiledTransitionModel.GlobalTrackers.Length`.
  - `energy.pair_tables[i].energies` = `ParameterizationValues.PairTables[i]`;
    `energy.cluster_tables[i].energies` = `ParameterizationValues.GroupSets[i]`, in compiled occupation order;
    `energy.defect_background` = `ParameterizationValues.SiteEnergies` (dense `[position][particle]`, void at
    column 0). The compiled defaults are never encoded.
  - `transitions.jump_collections` = `{kind, transition_id, mobile_particle_mask, rule_ids, direction_ids}`;
    `jump_directions` = `{object_id, position_id, collection_id, path, movement, path_vector, length,
    is_inverse}`; `jump_rules` = `{rule_id, collection_id, state0, state1, state2,
    frequency_hz = ParameterizationValues.TransitionRuleFrequencies[i], charge_transport_vector,
    static_correction = nil, tracker_order}`.
  - `jump_count` is `CompiledTransitionModel.JumpCounts` in `[position][particle]` order; `jump_mapping` is the
    flat concatenation of `JumpMappings` in `[position][particle][relative]` order, so the reader derives the
    offsets from the prefix sums of `jump_count`. A test pins `JumpOffsets` against those prefix sums.
  - `static_tracker_mapping` is a dense `[position][particle] → tracker offset` array with `-1` for "none";
    `global_tracker_mapping` is a dense `[collection][particle] → tracker index` array with `-1` for "none". The
    tracker models in `simulations` reference the same indices.
  - `simulations[].default_settings` carries the compiled runtime defaults; `flags` contains only the authored
    bits 0–3 (derived intent is per job), and `field_direction` is the normalized Cartesian vector so the model
    and job payloads use one representation. `tracker_models` are definitions only; Step 18 owns values.
- **Lattice payload** — `{format_version, lattice_size, position_count, lattice}`. `lattice` is the
  `GeneratedLattice.Particles` byte array (row-major layout of §3.4). No doping summary is stored in the
  lattice: the identical particle array must dedupe regardless of which job's doping request produced it, and
  the job payload already carries the summary.
- **Job payload** — the §3.4 map. `simulation_index`, `kind`, all resolved settings, `flags` from
  `SolverJobFlagsMapper.Compose(settings.Flags, descriptor.Kind, settings.PreRunMcsp,
  descriptor.UseDualDofCorrection)`, `selection_optimizers` from `descriptor.CompiledOptimizers`,
  `doping_info` from `GeneratedLattice.DopingInfo`, and `extensions` as `{id, config_json}`.
- `config_json` is the UTF-8 canonical JSON text of `JobExtension.Config`, produced by the same canonicalizer
  the project hash uses (exposed as a small public Core helper in this step). It is stored as a MessagePack
  `str`, not a map: extension config numbers are opaque and must keep the authored token, which a
  `int64`/`float64` map cannot represent for integers above 2^63. Extensions are sorted by `id`.
- The writer verifies round-trip decoding of every payload it encodes before insert; on mismatch it fails and
  deletes the temp file.

### 4. Content addressing, dedup, and determinism

- `models.signature` is the Step 09 parameterization signature and the uniqueness key. Insert uses
  `INSERT … ON CONFLICT(signature) DO NOTHING` plus a lookup of the existing `model_id`.
- `lattices.signature` is the finalized `LatticeSignatureV1` (ADR 0010), a domain-separated SHA-256 over the
  canonical MessagePack projection `[format_version(uint64), model_signature(string), lattice_size(uint64×3),
  position_count(uint64), particles(bin)]`. It binds the compiled-model identity because lattice entries are
  compiled particle indices, and it excludes build metadata and the doping summary.
- Dedup uses an in-memory `signature → id` map plus the unique index; exact matches insert once. Two jobs with
  the same particles share one row; a doping request that yields different particles produces a different row.
- Determinism: the same project, selected parameterization, and job set produce identical model and lattice
  signatures and an identical canonical summary. Stored bytes may differ only in build metadata
  (`schema_info.created_utc`, `app_version`) and in SQLite/Zstd implementation details; no golden test compares
  raw file bytes.

### 5. Verification and inspection

`Mocassin.Results.Packages` gains the read-only `.msl` structural reader used by Step 12's `mocassin
inspect`/`mocassin verify` and later by Step 20. `Mocassin.Packages` must not own general `.msl` reading
(`01-target-architecture.md` §3.1).

- `MslPackageInspector` returns the schema/app version, model signatures and sizes, job counts by
  kind/collection/config, lattice count and dedup ratio, temperature/field ranges, and package-level errors.
- `PackageVerifier.VerifyAsync(path, options, ct)` returns a `PackageVerificationReport` with ordered checks
  and `Diagnostic`s using `PKG` codes. Checks:
  - SQLite opens read-only, `PRAGMA integrity_check` passes, `application_id`/`user_version`/`schema_info`
    agree, and required tables/columns exist with no `results` table and no `-wal`/`-shm` sidecar;
  - every job references an existing model and lattice; `job_id` is unique and ascending; the
    `(collection_index, config_index, job_index)` triple is unique;
  - every payload decodes within an explicit decompressed-size bound (model 512 MiB, job 8 MiB, lattice
    `4 × a × b × c × position_count` capped at the signed 32-bit index limit) and its payload `format_version`
    matches the column;
  - the model's parameterization projection re-encodes to `models.signature` and the payload's `signature`
    from the stored absolute `frequency_hz` values — never from a factor × maximum; `model_signature` is a
    64-hex value and cannot be re-encoded from the payload because the compiled projection signs stable IDs
    that are inspection-only;
  - the lattice projection re-encodes to `lattices.signature` using `model_signature` from the referenced model
    row; `lattice.Length == a·b·c·position_count`; every particle index is below the model particle count;
  - the denormalized `jobs` columns equal their payload values; `flags` has exactly one kind bit and
    `usePrerun` set if and only if `pre_run_mcsp > 0`;
  - the package size is checked against the advisory limit (warning, not an error).
- Bounds are enforced before allocation; a malformed or hostile package produces a report with `PKG` errors, never
  an unhandled exception or an unbounded allocation. Verification never mutates the package.

Diagnostic ownership: `PKG` descriptors live in `Mocassin.Contracts.Packages` so both `Mocassin.Packages` and
`Mocassin.Results` can compose them into `DiagnosticCatalog.Default`, and `docs/reference/diagnostics.md` stays
the drift-checked source. Step 12 maps `verify` failures to exit code `1`/`3` and `inspect` to human/JSON output.

### 6. Contract deltas and pre-release finalization

`.msl` has never been released, so these are pre-release finalizations, not migrations (ADR 0010's exception;
the "first released format" discipline of ADR 0009 resumes afterwards). Each delta is applied in the same
feature as the code that needs it, with regenerated artifacts and fixtures:

1. `signatures.lattice` is finalized from the Step 00 skeleton to the five fields of §4: `dopingInfo` is
   removed (doping provenance lives on the job) and `modelSignature` is kept (particle indices are
   model-specific). `status` becomes `final`; the generated descriptor and the golden vectors are added.
2. `formats.package.payloads` is added to the manifest with the **top-level** model, job, and lattice payload
   keys, types, required flags, and payload `formatVersion` values. This closes the ADR 0009 gap at the same
   granularity the `.mcs` entry uses (nested maps stay typed as `object`/`array`). The generator validates and
   emits the package schema fragment; fixture descriptors are regenerated.
3. The undefined `jobs.norm_factor` and `jobs.base_frequency_hz` columns are removed from the manifest and the
   SQL contract.
4. `jump_collections.object_id` is removed from §3.3: it has no compiled source and the dense index plus
   `transition_id` already identify a collection. `jump_directions.object_id` stays (its compiled source is
   `CompiledJumpDirection.ObjectId`).
5. The lattice payload drops `doping_info`; the job payload and `jobs.doping_info` keep the canonical summary.
6. `extensions` entries become `{id, config_json}` with the canonical JSON text (see §3.3).
7. `electric_field_direction` is the normalized Cartesian unit vector derived from the authored fractional
   direction; a zero modulus stores the normalized authored direction when non-zero and `(1,0,0)` when the
   authored vector is also zero.
8. `schema_info` gains the three symmetry provenance rows; the writer requires every model it writes to share
   them.
9. The job payload's `jump_mapping`/tracker-mapping shapes and the `particle_ids` ordering are stated
   explicitly (they were implicit).

Fixture consequences: regenerate `contracts/generated/**`, add `tests/contracts/projections/lattice/*.bin`
vectors and their metadata, extend `tests/contracts/signature-vectors.json`, add
`tools/contracts/check_lattice_projection.py` (or fold the check into the parameterization checker) and run it
in `verify.sh contracts`. No previous-major `.msl` fixture exists; the first released major starts the ADR 0013
current+previous policy, and this step records the current-only fixture layout so the policy can start at v2.

### 7. Bounds, performance, and failure behaviour

Targets (benchmark laptop class, .NET 10 Release):

- 10,000 small jobs (10×10×10 ceria, ~1,000 positions) in under 10 minutes and under 2 GB peak RSS.
- Model payload under 50 MB for a complex model.
- Job insertion throughput at least 100 jobs/s after compilation and lattice generation.
- Lattice insertion is dominated by generation; the writer reports the dedup ratio.

The writer warns (through `PackageWriteResult.Warnings`, surfaced by Step 12) when the package exceeds
`AdvisorySizeBytes`, and suggests fewer duplicate jobs, smaller supercells, external result storage later, or
`--compact` compression. There is no silent failure and no unbounded in-memory buffering: payloads are encoded
per model/job, latices one at a time, and `jump`/tracker arrays are written as flat arrays.

Failure behaviour: a cancelled or failed build deletes the temp file and leaves an existing package intact; a
disk-full/IO error surfaces as `IOException` and deletes the temp file; the published file is only ever
replaced by the atomic rename after the file and directory are synced.

### 8. Build integration and step boundaries

- Step 12 owns `mocassin build`: it loads, validates, compiles, parameterizes, expands jobs, generates lattices,
  pairs them into `JobPackage`, feeds the writer, then calls the verifier before reporting success.
- Step 11 does not add an `IValidationStage`. Package-size advice is build output, not user-model validation.
- Step 12 owns `mocassin inspect`/`verify` and maps `PKG` diagnostics to the CLI error model; Step 20 reuses
  the reader for evaluation.
- Step 13 consumes the checked-in `.msl` fixture and the §3.3/§3.4 payload contract; the cross-language read is
  demonstrated there, not here.

### 9. Sizing

Roadmap estimate: **3–4 person-weeks**. This design estimates **3.5–4.0 pw** including tests, fixtures,
documentation, and review. Seven features are within the roadmap's "roughly 6–8" range and the estimate stays
at, not beyond, the four-week trigger, so no 11A/11B split or owner exception is proposed.

## Implementation tasks

- [ ] Add the Contracts package DTOs, payload constants, `LatticeSignatureV1`, and `PKG` descriptors.
- [ ] Finalize `signatures.lattice` and add `formats.package.payloads`; extend the generator and regenerate.
- [ ] Implement schema creation, pragmas, `application_id`/`user_version`, and `schema_info`.
- [ ] Implement the model payload mapper and the write-time parameterization re-encode.
- [ ] Implement the lattice payload/signature and content-addressed dedup.
- [ ] Implement the job payload mapper, `SolverJobFlags` composition, Cartesian field conversion, optimizer
      mapping, extension JSON, and denormalized columns.
- [ ] Implement batching, streaming, progress/cancellation, and bounded memory.
- [ ] Implement the temp file, fsync of file and directory, atomic rename, and failure cleanup.
- [ ] Implement the read-only inspector/verifier with bounded decoding and `PKG` diagnostics.
- [ ] Add checked-in `.msl` fixtures, canonical summaries, extracted payloads, and regeneration command.
- [ ] Add the lattice projection golden vectors and `verify.sh contracts` checker.
- [ ] Add the `Mocassin.Benchmarks/SimulationPackage` benchmark and record the 10⁴-job budget.
- [ ] Update `PublicAPI.Unshipped.txt` and the ApiCompat baseline for all three projects.
- [ ] Write the documentation named below and update parity/status.

## Acceptance criteria

- [ ] A package built from `samples/jobs/ceria-temperature-sweep.mocprj` (six jobs, `parameterizations: []`)
      uses the implicit default parameterization, opens read-only, and contains one model row, the expected
      job rows, and the expected lattice rows.
- [ ] A package built with an explicit in-test parameterization stores the applied pair/group/site/frequency
      values, not compiled defaults, and `models.signature` equals `ParameterizedModel.Signature`.
- [ ] The published package has no `results` table, needs no `-wal`/`-shm` files, and reports
      `application_id = 0x4D4F4353` and `user_version = 1`.
- [ ] Every job references a valid model and lattice; `job_id` is unique and ascending; a partial build leaves
      gaps exactly as recorded in `job_selection`.
- [ ] `verify` re-encodes the parameterization signature from the payload's absolute `frequency_hz` values and
      re-encodes the lattice signature from the particle array and the referenced `model_signature`.
- [ ] Two jobs whose lattices are byte-identical share one `lattices` row; a different particle array does not.
- [ ] Rebuilding the same project produces identical model/lattice signatures and an identical canonical
      summary; stored bytes may differ only in build metadata.
- [ ] The job `flags` word equals `SolverJobFlagsMapper.Compose(...)` and contains exactly one kind bit.
- [ ] The job `electric_field_direction` is a unit vector equal to the compiled cell transform of the authored
      fractional direction (tested on a non-orthogonal cell).
- [ ] `extensions[].config_json` is byte-equal to the project canonical JSON for the same config, including a
      number token above 2^53.
- [ ] A cancelled build leaves no temp file and does not modify an existing package.
- [ ] A simulated disk-full/write error leaves the previous package intact.
- [ ] `verify` detects a truncated payload, a corrupt Zstd frame, a wrong signature, a missing FK, a wrong
      denormalized column, a stray `-wal`, and an injected `results` table, each with the expected `PKG` code.
- [ ] 10⁴ small jobs build within the time/size budget.
- [ ] No EF Core, no native structs, no custom binary header, and no results table.
- [ ] Documentation written

## Documentation

At this point, document the simulation package:

- `docs/reference/msl-format.md` — SQLite schema, `application_id`/`user_version`, content-addressed
  models/lattices, model/job/lattice payloads, versioning, and verification checks (new page plus a nav entry
  in `zensical.toml` and `docs/reference/index.md`).
- `docs/reference/file-layout.md` — package location, immutable package rules, `.msr` sidecar, atomic-write and
  temp-file behaviour, and the checked-in fixture layout.
- `docs/reference/diagnostics.md` — the `PKG` codes and their help text.
- `docs/development/architecture.md` — `Mocassin.Packages` writer versus `Mocassin.Results` reader boundary and
  the payload mapping.
- `docs/development/testing-and-parity.md` — package fixture regeneration and the `.msl` test tiers.
- No user-guide build page: `docs/user-guide/building.md` belongs to Step 12, which owns the build CLI.

## Tests

| Test | Tier | Assertion |
| --- | --- | --- |
| Schema | PR-fast | Tables, columns, indices, pragmas, `application_id`/`user_version`, and `schema_info` rows match the contract |
| Model payload mapping | PR-fast | Every §3.3 field maps from the documented compiled/parameterized source; values equal `ParameterizationValues` exactly |
| Lattice payload and layout | PR-fast | `lattice.Length == a·b·c·position_count`; row-major index order; particle indices valid |
| Job payload mapping | PR-fast | Settings, optimizers, `doping_info`, extension JSON, and `flags` match the descriptor |
| Field direction | PR-fast | Unit vector; non-orthogonal cell; zero-modulus and zero-direction cases |
| Signatures | PR-fast | Parameterization and lattice projections re-encode exactly; golden `.bin` vectors match |
| Dedup | PR-fast | Identical model/lattice inserted once; different particles insert a second row |
| Determinism | PR-fast | Rebuild yields identical signatures and canonical summary; bytes differ only in build metadata |
| Atomicity | PR-fast | Cancel and injected write failure delete the temp file and preserve an existing package |
| Immutability | PR-fast | No `results` table; no `-wal`/`-shm` after publication; read-only open succeeds |
| Job IDs | PR-fast | Unique, ascending, template-ordered; partial-build gaps preserved |
| Verification | PR-fast | Each `PKG` code fires on its corrupt/truncated/inconsistent fixture |
| Decode bounds | PR-fast | A Zstd bomb or oversized count produces `PKG004`, not OOM |
| Property: payload round-trip | PR-fast | Arbitrary valid compiled/parameterized models encode and decode to the same logical values |
| Property: signature stability | PR-fast | Normalized `-0.0`, shuffled map order, and a different Zstd level do not change the signature |
| Cross-language fixture | PR-medium | The checked-in `.msl` and extracted model payload are accepted by the Step 13 reader unchanged |
| Corruption/fuzz | Nightly | Mutated `.msl` files never crash or over-allocate the verifier |
| Performance | Nightly/release | 10⁴ jobs under 10 min / 2 GB; model payload under 50 MB; ≥ 100 jobs/s insertion |

Every test declares its tier. Tests use the real compiler, parameterizer, expander, generator, and writer; no
domain doubles. Temporary directories and injected IO failures are the only boundary doubles.

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| SQLite bloat for large campaigns | Content addressing, Zstd, `--compact`, advisory size report, external results later |
| MessagePack/Zstd drift between .NET and Rust | Standard frames and maps; manifest-backed payload descriptors; checked-in payload fixtures read by Step 13 |
| Pre-release contract delta breaks a golden vector | Apply each delta with its regenerated manifest/schema/vectors in the same feature; `verify.sh contracts` is a gate |
| Signature covers a value the payload cannot reproduce | Only parameterization and lattice projections are re-encoded; `model_signature` is cross-checked at write time and treated as an identity string |
| Long transactions lock the file | Batch commits; build only in a temp file |
| Cancellation or crash leaves partial data | Temp file, delete on failure, fsync + atomic rename; orphaned temp files are inert |
| Determinism across SQLite/Zstd versions | Hash logical projections, not pages or compressed bytes; golden tests compare canonical summaries |
| Extension config loses authored number tokens | Store canonical JSON text, not a numeric map |
| Lattice dedup merges distinct provenance | Doping provenance is kept on the job, not on the lattice |
| Verifier becomes an unbounded parser | Explicit decompressed-size caps, `UntrustedData`, count/limit checks before allocation |
| Placement of `.msl` reading in the wrong project | Read-only inspection lives in `Mocassin.Results`, matching the architecture table; the writer only self-checks its own payloads |

## Done when

A valid, versioned, immutable `.msl` package can be built atomically from a project's compiled and parameterized
model plus streamed jobs, its model/lattice signatures re-encode from the stored payloads, a read-only verifier
and inspector report its content and integrity, the contract manifest and generated schema describe the
payloads, and a checked-in package fixture plus its canonical summary are ready for the Step 13 Rust reader —
with deterministic content signatures and bounded memory.
