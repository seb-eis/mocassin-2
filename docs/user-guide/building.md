# Building a Simulation Package

A build turns a validated project into one immutable `.msl` package: the shared compiled model, one initial
lattice per job, and each job's resolved settings. The package is the complete input for the Rust solver; the
solver never reads `.mocprj`. The format and its verification rules are in
[Simulation package format](../reference/msl-format.md).

## Pipeline

| Stage | What happens |
| --- | --- |
| Load and validate | Read the `.mocprj`, migrate it if needed, and run the project validation pipeline (load/model/cross-domain/feasibility). Build only proceeds without error diagnostics. |
| Compile | `ModelCompiler` produces the immutable compiled model (positions, sites, pair/cluster links, transitions, simulations) and its Step 08 signature. |
| Validate job templates | The build stage runs the compiled-bound job-template, position, optimizer, and doping checks with the compiled model. |
| Parameterize | `ParameterizationService` applies the selected parameterization, or the implicit default when the project defines none, and produces the applied tables and the Step 09 signature that becomes `models.signature`. |
| Expand jobs | `JobTemplateExpander` resolves collections and configurations in authored order and emits deterministic job descriptors with 1-based, gap-free job IDs and a 32-byte seed per job. |
| Generate supercells | `SupercellGenerator` applies the requested supercell size and doping, and reports the applied versus requested fractions. |
| Write and publish | `SimulationPackageWriter` inserts the model row, then streams each job: lattice payload (content-addressed and deduplicated), job payload, and the payload-derived columns, in batches. It decodes every payload it writes as a self-check, then checkpoints, fsyncs, hashes, and atomically renames the temp file to the target. |
| Verify | The CLI runs the read-only package verifier after publication and reports the ordered `PKG` checks. |

## Options

`PackageBuildOptions` controls the build:

| Option | Default | Meaning |
| --- | ---: | --- |
| `CompressionLevel` | `3` | Zstd quality for every payload, `1..19`. It maps by identity to the in-box encoder quality. Values outside the range are rejected. |
| `Overwrite` | `false` | Replaces an existing target. Without it, a build never overwrites a published package. |
| `Vacuum` | `false` | Runs `VACUUM` before publication. It costs time and is not needed for correctness. The CLI’s `--compact` sets this **and** `CompressionLevel = 9` as one size preset. |
| `BatchSize` | `500` | Job rows per committed transaction. |
| `AdvisorySizeBytes` | 2 GiB | Emits the `PKG009` advisory warning when the package is larger. It is a warning, not a failure. |

The build request also carries the target path, the project ID, a non-empty job-selection expression (for
example `all` or `1-100,250`), and a caller-supplied `created_utc`. The clock is never read implicitly, so a
rebuild is comparable.

## Reproducibility

Two builds of the same project, parameterization, and job set produce:

- identical model, parameterization, and lattice signatures;
- an identical `package-summary-v1` projection;
- deterministic job IDs and seeds;
- payload bytes that are stable on the same runtime patch.

Build metadata (`created_utc`, `app_version`, `generator`), file size, and payload byte hashes may differ, and a
future Zstandard runtime patch may change compressed bytes. None of that changes the logical content, the
signatures, or the canonical summary. Compare builds with `package-summary-v1`, not with raw file bytes.

## Failure behaviour

The writer builds in a temp file beside the target, uses WAL and batch commits, then checkpoints to
`journal_mode=DELETE` before publication. A cancelled or failed build deletes the temp file and leaves an
existing package untouched. A disk-full or I/O error surfaces as an `IOException` and does not truncate a valid
package. Publication is the filesystem atomic rename after the file (and, on Unix, the containing directory) is
fsynced; see [Repository and file layout](../reference/file-layout.md). Windows keeps the file fsync plus atomic
replace and skips the directory fsync, which has no equivalent there.

## Verification

`PackageVerifier` runs ten ordered checks and reports `PKG001`–`PKG010` with their documented severities; see
[Diagnostics](../reference/diagnostics.md). Verification opens the package read-only, never mutates it, and is
also where a hostile package is contained: payload bounds are checked before allocation and a malformed package
produces diagnostics, not a crash. A package with any error-severity `PKG` code is not a valid build result.

## Command line

The full workflow is available without the GUI:

```sh
mocassin new ceria-job -o work
mocassin validate work/ceria-job.mocprj
mocassin build work/ceria-job.mocprj -o work/ceria-job.msl
mocassin inspect work/ceria-job.msl
mocassin verify work/ceria-job.msl
```

A useful ceria job sample is `samples/jobs/ceria-temperature-sweep.mocprj`; the model-only
`samples/ceria/ceria.mocprj` has no job template and cannot expand jobs.

### Recipe selection

- `--template <id>` selects a project **build template** (job template + parameterization + output name).
- `--job-template <id>` selects a job template directly and overrides the build template’s job template.
- `--parameterization <id>` overrides the build template’s parameterization.
- With no options, a project with exactly one build template uses it; a project with no build template and
  exactly one job template uses that; several candidates are an error listing the IDs.
- A project with `parameterizations: []` uses the implicit default: `0 eV` pair/group values, the compiled
  `1e13 Hz` kinetic frequencies, and the authored site-energy background. It emits no `PRM003`/`PRM006`.

### Partial jobs

`--jobs` builds a subset of the complete expansion:

```sh
mocassin build ceria.mocprj -o partial.msl --jobs 1-3,7
```

IDs are the full build’s 1-based IDs, so they stay stable between a partial and a full build. The package
records the canonical selection (`1-3,7`; `all` for a full build) in `schema_info.job_selection`, and
`mocassin inspect` shows it. Unselected jobs are absent and leave gaps.

### Preview

`mocassin build ceria.mocprj --preview` runs validation, compilation, parameterization, and expansion, then
reports each collection, configuration, optimizer, lattice size, and doping outcome. It never creates or
modifies a `.msl`.

### Progress and cancellation

Human progress goes to stderr and the final summary to stdout. The first Ctrl+C (or SIGTERM on Unix) requests
cancellation: before publication the temp file is deleted and an existing package is untouched; after
publication the new package remains and the command exits 6. Progress is throttled and shows an ETA.

### Replacement and size

- An existing package is not replaced without `--force`.
- `--compact` uses Zstd quality 9 plus `VACUUM`; it changes compressed bytes and size, not signatures or the
  canonical summary.
- `--parallel <n>` controls compilation and lattice generation; package writes stay serialized in job order,
  so `--parallel 1` and `--parallel N` produce the same logical package.

### Verification

After writing, `build` runs the package verifier and reports `PKG` diagnostics. `mocassin inspect` and
`mocassin verify` can inspect or re-verify a package at any time, read-only. A verification failure after a
build exits 1 and leaves the published package in place.

### Exit codes

| Code | Meaning |
| ---: | --- |
| 0 | Success (warnings allowed unless `--warnings-as-errors` promotes them). |
| 1 | Validation/model error, invalid package, or unhealthy `doctor`. |
| 2 | Usage error, selection ambiguity, malformed `--jobs`, reserved command/option. |
| 3 | I/O error, missing/unreadable input, existing target without `--force`. |
| 4 | Compilation or parameterization error, promoted parameterization warning. |
| 5 | Reserved for multi-job commands; `build` is all-or-nothing. |
| 6 | Cancelled. |
| 10 | Unexpected internal error. |

See the [CLI reference](../reference/cli.md) for every command, option, report, and event.

## Sizing guidance

The measured reference workload (10,000 jobs at 10×10×10 ceria, about 1,000 positions) builds in a few seconds
and produces a package of roughly 10 MB with about 120 MB peak RSS on the benchmark laptop class; the full budget
and the net10 comparison are in the Step 11 benchmark evidence. Packages grow with the number of distinct
lattices, the supercell volume, and the model tables. The writer reports the lattice reuse factor and an advisory
size warning. If a campaign is too large, reduce duplicate campaigns, use a smaller supercell, choose
`--compact`, or move derived results to the `.msr` store.
