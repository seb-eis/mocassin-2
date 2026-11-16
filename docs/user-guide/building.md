# Building a Simulation Package

A build turns a validated project into one immutable `.msl` package: the shared compiled model, one initial
lattice per job, and each job's resolved settings. The package is the complete input for the Rust solver; the
solver never reads `.mocprj`. The format and its verification rules are in
[Simulation package format](../reference/msl-format.md).

## Pipeline

| Stage | What happens |
| --- | --- |
| Load and validate | Read the `.mocprj`, migrate it if needed, and run the project validation pipeline. Build only proceeds without error diagnostics. |
| Compile | `ModelCompiler` produces the immutable compiled model (positions, sites, pair/cluster links, transitions, simulations) and its Step 08 signature. |
| Parameterize | `ParameterizationService` applies the selected parameterization, or the implicit default when the project defines none, and produces the applied tables and the Step 09 signature that becomes `models.signature`. |
| Expand jobs | `JobTemplateExpander` resolves collections and configurations in authored order and emits deterministic job descriptors with 1-based, gap-free job IDs and a 32-byte seed per job. |
| Generate supercells | `SupercellGenerator` applies the requested supercell size and doping, and reports the applied versus requested fractions. |
| Write | `SimulationPackageWriter` inserts the model row, then streams each job: lattice payload (content-addressed and deduplicated), job payload, and the payload-derived columns, in batches. |
| Verify and publish | The writer decodes every payload it writes as a self-check, then checkpoints, fsyncs, hashes, and atomically renames the temp file to the target. |

## Options

`PackageBuildOptions` controls the build:

| Option | Default | Meaning |
| --- | ---: | --- |
| `CompressionLevel` | `3` | Zstd quality for every payload, `1..19`. It maps by identity to the in-box encoder quality. Values outside the range are rejected. |
| `Overwrite` | `false` | Replaces an existing target. Without it, a build never overwrites a published package. |
| `Vacuum` | `false` | Runs `VACUUM` before publication. It costs time and is not needed for correctness. |
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

The full build command is part of the Step 12 build orchestration:

```sh
mocassin build samples/jobs/ceria-temperature-sweep.mocprj -o ceria.msl
mocassin verify ceria.msl
```

Step 12 adds the `build`, `inspect`, and `verify` commands, their job-selection syntax, and the exit-code
mapping. Until then the writer, inspector, and verifier are exercised through their library APIs and the Step 11
test suite; the pipeline and options above are the contract the command implements.

## Sizing guidance

The measured reference workload (10,000 jobs at 10×10×10 ceria, about 1,000 positions) builds in a few seconds
and produces a package of roughly 10 MB with about 120 MB peak RSS on the benchmark laptop class; the full budget
and the net10 comparison are in the Step 11 benchmark evidence. Packages grow with the number of distinct
lattices, the supercell volume, and the model tables. The writer reports the lattice reuse factor and an advisory
size warning. If a campaign is too large, reduce duplicate campaigns, use a smaller supercell, choose the
`--compact` level (`9`), or move derived results to the `.msr` store.
