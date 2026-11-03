# Step 28 — Run Monitoring, Analysis UI, and MMCFE

**Phase:** F. GUI  
**Depends on:** 26, 19–21
**Delivers:** A run queue that launches and monitors `mocassin-sim`, live progress/statistics/charts, result
collection, analysis dashboards, the MMCFE workflow, and the lattice-recycle tool.

## Implementation status

- **Status:** Not started
- **Step branch:** —
- **Step CI:** —
- **Main merge:** —
- **Step tag:** —
- **Last updated:** —

| Feature | Branch | Status | Local gates | Step CI | Squash commit |
| --- | --- | --- | --- | --- | --- |
| — | — | — | — | — | — |

## Goal

Let users run simulations and understand the results without leaving the application. Long HPC jobs remain
script-driven, but local runs and post-processing should be first-class.

## Original reference

- No run monitoring: the GUI built `.msl` files; simulations were launched manually or via Python/SLURM.
- The MMCFE import tool scanned `Job%05d` folders, read `mmcfelog.db`, and wrote `mmcfe.raw.db`/`mmcfe.eval.db`
  with progress/logging, optional zip, optional folder deletion.
- The lattice recycle tool imported a final lattice from one `.msl` as the initial lattice of another.
- Evaluation was API/script-only; no charts.
- No live job status, no resume/cancel UI, no notifications.

## Design

### 1. Run queue

```csharp
public sealed class RunQueueService
{
    public Task EnqueueAsync(RunRequest request, CancellationToken ct);
    public Task CancelAsync(int runId);
    public IAsyncEnumerable<RunEvent> Events { get; }
}

public sealed record RunRequest(
    string PackagePath,
    IReadOnlyList<int> JobIds,
    string OutputRoot,
    int Parallelism,
    string? SolverPath,
    bool Resume,
    string? ExtensionPath);
```

- The queue launches one `mocassin-sim` process per job (or `batch` for local runs).
- Configurable parallelism; default `min(CPU count, 4)`.
- Each run gets an output folder `Job%05d`, log files, and a `solver.json`.
- Run states: queued, starting, running, completed, aborted, failed, cancelled, already-completed.
- Cancellation sends SIGINT/terminate, waits, then force-kills.
- Resume uses existing `run.mcs`/`prerun.mcs`.
- The queue survives navigation; notifications on completion/failure.
- The GUI never blocks on a run; all I/O is async.

### 2. Live monitoring

- Parse solver JSONL events from stderr (step 19/contracts):
  - `schemaVersion`, event name, job id, stage, MCS/target, rate, ETA, energy, normalization, abort reason.
  - Unknown events/fields are ignored; a future event schema major is rejected.
- Per-job view:
  - progress bar, MCS/cycles, simulated time, rate, ETA, lattice energy, norm factor;
  - per-particle counters and transport values;
  - stdout log (tail) and stderr log;
  - state file sizes and last save time.
- Aggregate view:
  - jobs by status, throughput, failures;
  - queue progress and estimated completion.
- Charts (LiveCharts2):
  - MCS vs wall time;
  - rate vs time;
  - lattice energy (MMC) vs MCS;
  - normalization factor (KMC) vs pre-run progress;
  - per-particle conductivity/diffusion once available.
- Charts update at a throttled rate (e.g. 2 Hz) and retain a bounded history.
- "Open job folder", "copy command", "view solver.json".

### 3. Result collection

- `Collect` action calls the step-20 collector for selected jobs/all.
- Validates state files and writes the `.msr` results store; the `.msl` is never modified.
- Shows collected/skipped/invalid/failed counts and a link to Problems.
- Idempotent; safe to run while other jobs are still running (only completed folders).
- After collection, enable analysis actions.

### 4. Analysis dashboard

- Uses step 21 analyses.
- Views:
  - **Overview** — job counts, temperatures, dopings, completion status.
  - **Transport** — Arrhenius conductivity/diffusion, mobility, activation energy, Haven ratio.
  - **Microscopy** — jump-barrier histograms, transition contributions, static flow.
  - **MMC** — energy convergence, equilibrium, free energy (MMCFE).
  - **Comparison** — overlay job sets/collections, fit table.
- Filter bar: kind, collection, config, temperature, field, status, particle, transition.
- Charts are interactive (zoom/pan, hover values, export).
- Tables show the underlying data with units and source job.
- Export chart data/plots/report via step 21.
- Reproducibility: the dashboard records filters and analysis version; "export report" captures them.

### 5. MMCFE workflow

**Configuration**

- Select a KMC/MMC simulation and a job template/config.
- MMCFE parameters:
  - histogram size/range;
  - alpha count/min/max/current;
  - relax phase cycles, log phase cycles;
  - temperature base;
  - output folder.
- The configuration is stored in the job template as a structured extension config (not a free-text
  instruction), while still allowing a raw instruction for advanced users.
- Validation: cycles > 0, alpha range, histogram size, supercell size.

**Run**

- Launch via the run queue with the MMCFE extension.
- Live view: alpha step, relaxation/log progress, histogram mean/width, ETA.
- Resume from the last log entry.
- Cancel gracefully.

**Import/analysis**

- Read the new `mmcfe.db` format (step 19/20), not legacy `mmcfelog.db`.
- Build `mmcfe.raw`/`mmcfe.eval` datasets (or in-memory + SQLite) with progress.
- Plot `F(T)`, `U(T)`, `S(T)`, per-defect values, and SEM.
- Compare multiple runs/alpha schedules.
- Export CSV/JSON/plots.
- Optional zip/archive and folder cleanup are explicit actions with confirmation. No automatic deletion of
  job folders, `.msr` rows, or results.

### 6. Lattice recycle tool

- Select a source job/result lattice and a target build template.
- Validate compatibility:
  - same model signature/lattice size;
  - same particle set/occupations;
  - same site definitions.
- Import the final lattice as an **initial-lattice override** for a new build:
  - `.msl` is immutable, so never patch the target package;
  - build a new package (or a new job set) whose initial lattice comes from the selected `.mcs` state;
  - record the source package/job/hash in the new package metadata and report it;
  - invalidate no existing results; the new package gets its own `.msr`.
- Preview the imported lattice (counts, charge, differences).
- Require confirmation and keep the previous package; the recycle operation is additive.

### 7. Notifications and logs

- Notification center for run/build completion, failures, and collection results.
- Each notification has an action (open folder, show log, retry).
- Run logs are searchable and exportable.
- Problems panel includes solver errors with job context.

### 8. Performance and robustness

- Monitoring 100+ concurrent jobs uses a bounded event channel and throttled UI updates.
- Log tails are virtualized/limited (last N lines).
- Charts cap points (e.g. 10k) with downsampling.
- Process cleanup on app exit; orphan detection on next start.
- No UI thread blocking on process/IO; use async and `Dispatcher.UIThread` only for view updates.

## Implementation tasks

- [ ] Implement `RunQueueService`, process launch, cancellation, and resume.
- [ ] Implement JSONL event parsing and run state tracking.
- [ ] Implement per-job and aggregate monitoring views.
- [ ] Implement live charts with throttling/downsampling.
- [ ] Implement collection integration and post-collection analysis.
- [ ] Implement the analysis dashboard and filters.
- [ ] Implement MMCFE configuration, run, import, and plots.
- [ ] Implement the lattice recycle tool.
- [ ] Implement notifications, log viewer, and orphan cleanup.
- [ ] Add headless tests with a fake solver process at the process boundary (exit codes, signals, JSONL events).

## Acceptance criteria

- [ ] A local KMC/MMC job can be launched, monitored, cancelled, resumed, and collected into `.msr` from the
  GUI.
- [ ] Progress, ETA, energy, and rate update correctly from solver events.
- [ ] Aggregate status is accurate for 100+ jobs.
- [ ] Collection is idempotent and reports invalid states.
- [ ] Arrhenius/jump/MMC dashboards render from collected results.
- [ ] MMCFE can be configured, run, resumed, imported, and plotted.
- [ ] Lattice recycle validates compatibility and builds a new package with the imported initial lattice;
  it never mutates an existing `.msl`.
- [ ] App exit cleans up child processes; orphans are detected next start.
- [ ] No UI freezes during long runs.
- [ ] Documentation written

## Documentation

At this point, document run monitoring and MMCFE:

- `docs/user-guide/results.md`, `visualization.md`, `mmcfe.md` — run queue, live charts, collection, MMCFE.
- `docs/tutorials/result-analysis.md` — end-to-end monitoring/analysis workflow.
- `docs/performance/hpc.md` — local vs scheduler runs.
- `docs/troubleshooting/errors.md` — aborts, orphan processes, collection failures.

## Tests

| Test | Assertion |
| --- | --- |
| Run queue | Enqueue/parallel/cancel/resume |
| Event parsing | JSONL → state/progress |
| Monitoring | Per-job/aggregate accuracy |
| Charts | Downsampling and bounded history |
| Collection | Idempotent/invalid handling; writes `.msr`, never `.msl` |
| Results binding | `.msr` with a different package signature is rejected |
| Event schema | Unknown events/fields ignored; future major rejected |
| Dashboard | Filters and series values |
| MMCFE | Config/run/resume/import/plot |
| Recycle | Compatibility and package update |
| Notifications | Actions/state |
| Orphans | Detect/clean child processes |

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Process/UI coupling | Queue service + event channel; fake solver in tests |
| Many jobs overwhelm UI | Bounded channels, throttling, virtualization |
| MMCFE formula complexity | Shared analysis module + analytic tests |
| Package mutation on recycle | Recycle is additive: build a new package; never patch an existing `.msl` |
| Cross-platform signals | Use the solver's graceful signal handling; test per OS |

## Done when

Users can run, monitor, cancel, resume, collect, and analyze simulations in the GUI, including MMCFE and
lattice recycling, without leaving the application or freezing the UI.
