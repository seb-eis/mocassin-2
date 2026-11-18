# Step 19 — Solver CLI, HPC Workflows, and Extensions

**Phase:** D. Solver  
**Depends on:** 18
**Delivers:** A production `mocassin-sim` CLI, batch/worker execution, SLURM/PBS-friendly scripts, graceful
signals, deterministic seeds, and a versioned extension ABI (including MMCFE parity).

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

Make the solver usable on a laptop and on HPC systems, with predictable exit codes, logs, job folders, and
optional custom routines. The default path must be safe; extensions are explicit and isolated.

## Original reference

- `Mocassin.Simulator -dbPath <msl> -jobId <id> -ioPath <dir> [-stdout <name>] [-extDir <dir>]`.
- Outputs `run.mcs`, `prerun.mcs`; optional stdout/stderr logs in the I/O directory.
- Extensions: shared libraries scanned in `-extDir`, matched by a 16-byte UUID from the job's `RoutineData`;
  entry function receives the full C context. No versioning, no sandbox.
- MMCFE extension: simulated-annealing MMC, logs to `mmcfelog.db`, resumes from the last entry.
- HPC: Python `mocassin_mt.py` (sequential/shared/MPI/hybrid modes), SLURM submit wrapper, collector.
- Job folders: `Job00001`, `Job00002`, … with `run.mcs`, `prerun.mcs`, `stdout.log`, `stderr.log`.
- Exit code equals the internal error code; already-completed exits 0.

## Design

### 1. `mocassin-sim` CLI

```text
mocassin-sim --db <project.msl> --job <id> --io <dir>
             [--stdout <file>] [--seed <hex32>]
             [--threads <n>] [--jump-log-max-ev <f64>]
             [--extension <path>] [--extension-config <json>] [--no-extensions]
             [--log-level <error|warn|info|debug|trace>]
             [--log-format human|json] [--log-file <path>]
             [--dry-run] [--validate-only] [--no-state]
             [--version]

mocassin-sim batch --db <project.msl> --jobs <range|file>
                   [--io-root <dir>] [--parallel <n>] [--fail-fast]
                   [--continue-on-error] [--json]
```

- `--job` runs exactly one job (HPC-friendly).
- `--jobs` accepts `1-100`, `1,3,5`, `1-10,15,20-30`, or `@jobs.txt`.
- `batch` is a solver-only convenience for workstations; it runs jobs in worker threads/processes, each with
  its own folder and solver context. Default `parallel = min(CPU count, jobs)`. The .NET `mocassin simulate`
  command (Step 28) is the higher-level orchestrator that adds package/status/UI integration.
- `--threads` controls initialization/analysis parallelism within one job; the Monte Carlo trajectory is
  single-threaded and deterministic. Document this explicitly.
- `--seed` overrides the package seed for debugging; it is recorded in the state metadata and logs.
- `--dry-run` loads and validates the package/job, prints a summary, and exits.
- `--validate-only` validates the model/job without initializing a full context.
- `--no-state` disables `.mcs` writes (equivalent to `NoSaving`), useful for benchmarks.
- `--version` prints semver, git SHA, package format range, and build profile.

### 2. Job folders and I/O

- Default folder: `<io-root>/Job%05d` if `--io-root` is given; otherwise `--io` is used verbatim.
- Files (contract: `02-data-contracts.md` §5.3):
  - `run.mcs` — final/main MessagePack state;
  - `prerun.mcs` — KMC pre-run MessagePack state;
  - `stdout.log` — statistics dump (if `--stdout` or default log routing);
  - `stderr.log` — structured/human errors and progress;
  - `solver.json` — machine-readable summary (format/version, exit code, abort reason, wall time, state paths,
    counters);
  - `mmcfe.db` — optional MMCFE log database (contract: `02-data-contracts.md` §5.4).
- Writes are atomic (temp + rename) and never leave a partially written `.mcs`.
- On resume, existing `run.mcs`/`prerun.mcs` are loaded from the same folder.
- `--stdout` redirects the human statistics dump; structured logs always go to stderr/log file.
- The solver never writes to `.msl` or `.msr`.

### 2.1 Data lifecycle

- The solver never deletes job folders, logs, or state files.
- `mocassin clean` is the explicit removal path; it requires a selection, prints a dry-run by default, and
  asks for confirmation before deleting job folders or `.msr` rows.
- Before a long run, the queue/CLI checks free space and warns at 90 % usage; it does not delete anything
  automatically.
- Temporary files are cleaned on failure. Autosave/recovery and log retention follow the Step 23 defaults.
- `.msr` archives and job folders are user data; no background cleanup runs in the GUI or CLI.

### 3. Exit codes and signals

| Exit | Meaning |
| ---: | --- |
| 0 | Completed |
| 5 | Partial success (batch: at least one job failed, others completed) |
| 10 | Invalid arguments |
| 11 | Package/schema error |
| 12 | Model/data inconsistency |
| 13 | Numerical failure or internal invariant violation |
| 14 | Aborted by limit (time/rate/equilibrium) |
| 15 | Cancelled |
| 16 | I/O error |
| 17 | Extension error |
| 18 | Already completed |

- Codes 0, 5, and 10–16 are the canonical Step 13/architecture table; Step 19 adds 17 (extension error) and
  18 (already completed) and uses 5 for batch partial success. Do not redefine 13 as “numerical only”.

- `SIGINT`/`SIGTERM` request a graceful stop:
  - finish the current cycle/block;
  - write the state unless `NoSaving`;
  - flush logs;
  - exit `15`.
- A second signal exits immediately with `15`.
- `SIGUSR1` (Unix) dumps current statistics without stopping.
- On Windows, Ctrl+C is handled equivalently.

### 4. Batch execution

- `batch` expands the job selector, creates folders, and runs jobs with a bounded worker pool.
- Each worker owns its `SimulationContext`/RNG; no shared mutable state.
- Progress: one line per completed job plus periodic aggregate progress.
- Failure policy:
  - default `--continue-on-error`: record failures, continue, exit `5` if any failed;
  - `--fail-fast`: stop scheduling new jobs, wait for running ones, exit with the first failure code.
- `--json` emits JSON Lines events and a final summary.
- Batch mode is for workstations/small clusters; HPC schedulers should use one process per job for isolation.

### 5. HPC integration

Provide `tools/hpc/`:

- `slurm-array.sbatch`:
  - maps `SLURM_ARRAY_TASK_ID` or a job list to `mocassin-sim --job`;
  - sets `--io-root` from `SLURM_SUBMIT_DIR`;
  - loads modules via comments.
- `slurm-submit.py`:
  - generates an array job or a job list for non-contiguous IDs;
  - supports `--jobs 1-1000`, `--parallel`, `--account`, `--partition`, `--time`, `--mem`;
  - prints the `sbatch` command and optionally submits.
- `pbs-array.pbs` for PBS/Torque.
- `mpi-run.py` (optional):
  - one job per MPI rank, using `mpiexec`/`srun`;
  - contiguous or round-robin assignment;
  - no MPI inside the solver.
- `collect` is provided by the .NET CLI (step 20), not a Python script, so collection is validated and
  transactional.
- Job folder naming follows the original `Job%05d` layout so existing scripts can be adapted, but the files
  use the new contracts; no legacy file is read or written.

### 6. Extension ABI

Extensions are optional native libraries loaded only when explicitly requested.

```c
// mocassin_extension.h
#define MOCASSIN_EXTENSION_ABI_VERSION 1

typedef struct MocassinExtensionHostV1 MocassinExtensionHostV1;

typedef struct MocassinExtensionV1 {
    uint32_t abi_version;
    const char* name;
    const char* version;

    int32_t (*initialize)(MocassinExtensionHostV1* host, const uint8_t* config, size_t config_len);
    int32_t (*before_cycle)(MocassinExtensionHostV1* host);
    int32_t (*after_cycle)(MocassinExtensionHostV1* host);
    int32_t (*finalize)(MocassinExtensionHostV1* host);
} MocassinExtensionV1;

#if defined(_WIN32)
#define MOCASSIN_EXPORT __declspec(dllexport)
#else
#define MOCASSIN_EXPORT __attribute__((visibility("default")))
#endif

MOCASSIN_EXPORT const MocassinExtensionV1* mocassin_extension_entry(void);
```

The host callback table is versioned and append-only. Its first field is `size`, so newer hosts can add
callbacks without breaking older extensions:

```c
typedef struct MocassinExtensionHostV1 {
    uint32_t size;                    // sizeof(MocassinExtensionHostV1)
    uint32_t abi_version;
    /* read job/model metadata, positions, energies, logging, state-save requests */
} MocassinExtensionHostV1;
```

Rules (ADR 0014):

- C ABI, `extern "C"`, `#[repr(C)]`; the only exported symbol is `mocassin_extension_entry`.
- Required function pointers are non-null; optional callbacks may be null.
- Strings are UTF-8, NUL-terminated, and valid only for the call. The host copies anything it retains.
- Extension config is bounded UTF-8 JSON. The host owns the buffer during `initialize`; the extension must not
  retain it.
- Extensions must not unwind across the ABI. Rust extensions catch panics; C++ extensions catch exceptions.
- Hooks run on the solver thread. An extension that spawns threads must join them before returning.
- The host validates `abi_version` and refuses incompatible extensions.
- Extensions run in-process; this is a trust boundary. The CLI requires `--extension` and the GUI shows a
  warning. `--no-extensions` guarantees a pure run.
- An extension error aborts the job with exit `17` and a diagnostic; the state is saved if possible.
- The loader accepts regular files with the platform extension suffix only; no path traversal or world-writable
  locations.
- A Rust `Extension` trait mirrors the ABI for built-in extensions.
- MMCFE is implemented as a built-in extension (or a reference `cdylib`) using the same hooks:
  - simulated-annealing MMC with an alpha schedule;
  - energy histogram logging;
  - resumable `mmcfe.db` (schema in `02-data-contracts.md` §5.4, not legacy `mmcfelog.db`);
  - progress/ETA lines.
- No extension can change the package, results store, or other jobs.

### 7. Logging and diagnostics

- `tracing` with human/JSON formats.
- Fields: job id, model signature, kind, cycle, stage.
- Log levels:
  - `error`: fatal;
  - `warn`: suspicious model/data, fallback paths;
  - `info`: start/finish, normalization, aborts;
  - `debug`: per-block counters;
  - `trace`: per-cycle trace (only with an explicit flag; huge).
- `solver.json` is always written on completion/failure (unless the IO directory is unwritable).
- Errors include a stable code and context (job id, section, index).

### 8. Reproducibility and benchmarking

- `--seed` and package seeds are recorded in `solver.json` and `.mcs` metadata.
- `--no-state` + fixed cycle count is the benchmark mode.
- A `--profile` flag (feature-gated) emits stage timings (load, init, run, save) without changing results.
- HPC scripts pass `--seed` only for debugging; production runs use derived seeds.

## Implementation tasks

- [ ] Implement the full `mocassin-sim` parser and validation.
- [ ] Implement job folder creation, atomic writes, and `solver.json`.
- [ ] Implement graceful signal handling and exit codes.
- [ ] Implement `batch` with worker pool, progress, and failure policy.
- [ ] Add SLURM/PBS scripts and a submit helper.
- [ ] Implement the extension ABI header, versioned host table, panic/exception containment, loader, and Rust
  trait.
- [ ] Implement MMCFE as a built-in extension/reference.
- [ ] Add CLI integration tests, signal tests, and an extension sample.
- [ ] Document HPC usage and the extension ABI.

## Acceptance criteria

- [ ] `mocassin-sim --db ceria.msl --job 1 --io ./run1` produces `run.mcs`, logs, and `solver.json`.
- [ ] A completed job exits `18` on rerun, not silent success.
- [ ] SIGINT during a run writes a resumable state and exits `15`.
- [ ] `batch --jobs 1-10 --parallel 4` runs all jobs with independent seeds.
- [ ] The SLURM array script runs the ceria sample on a scheduler test harness.
- [ ] A sample extension loads, logs, and modifies a position; an incompatible ABI is rejected.
- [ ] MMCFE runs an annealing schedule and produces a resumable `mmcfe.db` matching the contract.
- [ ] Logs are structured and contain job/model identifiers.
- [ ] No job can write outside its I/O directory (path validation).
- [ ] Documentation written

## Documentation

At this point, document the production solver interface:

- `docs/reference/cli.md` — `mocassin-sim`, batch mode, signals, exit codes.
- `docs/performance/hpc.md` — SLURM/PBS scripts, job folders, process-per-job, collection.
- `docs/reference/extension-abi.md` — extension ABI, config, trust boundary, MMCFE hooks.
- `docs/troubleshooting/errors.md` — signal/cancel/extension failures.

## Tests

| Test | Assertion |
| --- | --- |
| CLI | Args, defaults, version, help |
| Job folder | File names, atomic writes, resume |
| Signals | Graceful stop and state save |
| Batch | Parallelism, failure policy, exit codes |
| HPC scripts | Generated submit commands/job lists |
| Extension | ABI version, config, error handling |
| MMCFE | Schedule, histogram, resume |
| Security | Path traversal rejected, extension opt-in |
| Reproducibility | Same seed → identical summary |

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Extension crashes/UB | Trust boundary warning, ABI versioning, panic/exception containment, future subprocess option |
| HPC script drift | Test generated scripts; keep them thin wrappers |
| Signal handling races | Finish block, write state, then exit; second signal force-exits |
| Batch oversubscription | Default parallelism = CPU count; explicit `--parallel` |
| Silent failures | `solver.json`, stable exit codes, summary logs |

## Done when

The solver runs reliably from a laptop or scheduler, writes a complete job folder, handles cancellation, and
supports explicit, versioned extensions including MMCFE parity.
