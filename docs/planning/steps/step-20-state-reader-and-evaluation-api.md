# Step 20 — State Reader, Result Collection, and Evaluation API

**Phase:** E. Results  
**Depends on:** 18, 11  
**Delivers:** A safe `.mcs` reader, transactional result collection into `.msl`, job querying, and the public
evaluation API for transport, displacement, diffusion, correlation, coordination, and jump statistics.

## Goal

Turn solver output into trustworthy scientific data. Users must be able to load a subset of jobs, evaluate
standard quantities with documented units and formulas, and export results without loading entire databases
into memory.

## Original reference

- `MslEvaluationContext` opened `.msl` through EF Core, loaded jobs with `Include` of result blobs, and
  restored the model from embedded project XML.
- `JobContext` wrapped an `McsContentReader` over a pinned `byte[]`; primary/secondary state selection.
- `JobEvaluation<T>` ran per-job evaluations sequentially or in parallel.
- Evaluation classes: particle count, lattice/ensemble meta, global/mobile/static tracking, displacement,
  MSD, diffusion, mobility (transition and ensemble), correlation/Onsager, coordination, jump statistics.
- `.mcs` was a raw buffer with no magic/version/checksum; sections unaligned; MMC detected by a negative
  offset; 32-bit offsets.
- Known issues: eager blob loading; pinned whole-file buffers; no disposal guards; `D_eff` charge factor
  ambiguity; `A+B+C` unit-cell bug; correlation matrix asymmetry; no lattice length validation.

## Design

### 1. Package access

```csharp
public sealed class ResultPackage : IAsyncDisposable
{
    public static Task<ResultPackage> OpenReadOnlyAsync(string path, CancellationToken ct);
    public IAsyncEnumerable<JobInfo> QueryJobsAsync(JobQuery query, CancellationToken ct);
    public Task<JobContext> LoadJobAsync(int jobId, CancellationToken ct);
    public Task<ParameterizedModel> LoadModelAsync(string signature, CancellationToken ct);
}
```

- Use `Microsoft.Data.Sqlite` directly, not EF Core.
- Open read-only (`Mode=ReadOnly`, `Cache=Private`).
- Query metadata columns first; load blobs only for selected jobs.
- `JobQuery` filters on kind, collection/config, temperature range, field range, status, MCSP, and job ID.
- Project only `length(run_state)` for list views; load the blob on demand.
- Load the compiled model once per signature and cache it.
- Dispose all statements/readers; no pinned `byte[]` for the lifetime of the context.

### 2. `.mcs` reader

```csharp
public sealed class McsReader : IDisposable
{
    public static McsReader Open(string path);
    public static McsReader FromBytes(ReadOnlyMemory<byte> bytes);
    public McsDocument State { get; }
    public McsMetadata Metadata => State.Metadata;
    public ReadOnlyMemory<byte> Lattice => State.Lattice;
    public IReadOnlyList<McsCycleCounter> Counters => State.Counters;
    public IReadOnlyList<McsVector3> GlobalTrackers => State.GlobalTrackers;
    public IReadOnlyList<McsVector3> MobileTrackers => State.MobileTrackers;
    public IReadOnlyList<McsVector3> StaticTrackers => State.StaticTrackers;
    public IReadOnlyList<uint> MobileMapping => State.MobileMapping;
    public IReadOnlyList<McsJumpStatistics> JumpStatistics => State.JumpStatistics;
    public IReadOnlyList<McsEnergySample> EnergySamples => State.EnergySamples;
}
```

- Deserialize the whole document with `MessagePackSerializer.Deserialize<McsDocument>(stream, options)`.
- Validate `format == "mocassin.mcs"`, `formatVersion`, metadata, lattice length, particle indices, tracker
  counts, mobile mapping, histogram bins, and finite numeric values.
- The `lattice` field is a MessagePack `bin`; trackers are arrays of `[f64;3]`; counters/histograms are arrays.
- Missing optional fields (especially KMC fields in MMC states) are treated as empty.
- Unknown fields are ignored; unknown `format`/version is rejected.
- Verify the `.msl` `run_state_sha256` before deserializing when the state came from a package result.
- No memory mapping, pinned arrays, native structs, or section table. The documented workload is < 4 GiB, so
  in-memory deserialization is acceptable; stream if profiling shows a need.

### 3. Job and model context

```csharp
public sealed class JobContext : IAsyncDisposable
{
    public JobInfo Info { get; }
    public ParameterizedModel Model { get; }
    public McsReader PrimaryState { get; }
    public McsReader? PreRunState { get; }
    public SimulationModel Simulation { get; }
    public LatticeGeometry Geometry { get; }
}
```

- `ParameterizedModel` comes from the package's `models` payload (not the project JSON).
- `LatticeGeometry` wraps cell vectors, P1 positions, position count, and linear index mapping.
- The evaluation layer shares the compiled model across jobs with the same signature.
- `JobContext` validates that the state's model signature/lattice size match the job.

### 4. Evaluation API

```csharp
public abstract class JobEvaluation<TResult>
{
    public IReadOnlyList<TResult> Result { get; }
    public Task<IReadOnlyList<TResult>> RunAsync(CancellationToken ct = default);
    protected abstract TResult Evaluate(JobContext job);
    protected virtual void Prepare() { }
}
```

- Evaluations are pure functions of a `JobContext`; they do not mutate it.
- `RunAsync` uses bounded parallelism (`MaxDegreeOfParallelism`) and returns results indexed by job order.
- No sync-over-async; no `Task.Result` in UI paths.
- Cancellation is checked per job.

#### Particle count and meta

- `ParticleCountEvaluation`: count each particle index in the lattice; index 0 (void) included.
- `LatticeMetaEvaluation`: `A,B,C` from job metadata, `P` from the model, volume `= V_cell * A*B*C` in m³.
- `EnsembleMetaEvaluation`: density `= count / volume` in 1/m³.
- Fix the original `A+B+C` bug: unit-cell count is `A*B*C`.

#### Displacement and MSD

- Global trackers are per `(transition, particle)`; sum over transitions for the particle total.
- Convert fractional → Cartesian (Å) → meters.
- `EnsembleDisplacement`:
  - `VectorR = Σ d` (sum of vectors);
  - `RadialR = Σ |d|` (sum of lengths, not length of sum);
  - `MeanVector = VectorR / N`, `MeanRadial = RadialR / N`.
- `SquaredDisplacementEvaluation`:
  - `VectorR2 = Σ (dx², dy², dz²)`;
  - `RadialR2 = Σ (dx²+dy²+dz²)`;
  - means divide by `N`.
- `DiffusionEvaluation`:
  - `D_x = MSD_x / (2t)`, `D_y`, `D_z`;
  - `D_r = MSD_r / (6t)`.
- Mobile trackers give per-particle displacement and current position; use for MSD/correlation.

#### Mobility and conductivity

- `ê` = normalized Cartesian field direction; `|E|` = field modulus in V/m; `t` = simulated time.
- `u = (MeanR · ê) / (|E| t)` in m²/(V·s).
- `σ = u * z * n * e` in S/m, with `z` the charge number and `n` the particle density.
- `σ_norm = u * n * e` (normalized to z=1).
- `D_sigma = σ * k_B_SI * T / (n * (z*e)²)` in m²/s (Nernst–Einstein).
- `D_tracer` from MSD; Haven ratio `H_R = D_tracer / D_sigma`.
- If `|E| = 0` or `z = 0` (void/vacancy), transport values are `null`/`n/a`, never `Infinity`/`NaN`.
- Vacancies are excluded from ionic mobility/conductivity by default; they may be reported separately as
  non-charge-carrying species.
- Transition mobility uses a single global tracker to isolate a transition's contribution.

#### Correlation and Onsager

- Mobile trackers give per-particle displacements `d_i`.
- `RiRi = Σ_i |d_i|²` for each particle ensemble.
- `RiRj = Σ_{i≠j} d_i · d_j` for i,j (including cross-ensembles).
- Means:
  - `⟨r_i·r_i⟩ = RiRi / N_i`;
  - `⟨r_i·r_j⟩ = RiRj / (N_i (N_i-1))` for i=j;
  - `= RiRj / (N_i N_j)` otherwise.
- Onsager (Kubo–Green, cubic assumption):
  - `L_ij = RiRj / (6 t V k_B T)` for i≠j;
  - `L_ii = (RiRi + RiRj_cross) / (6 t V k_B T)`.
- Produce a full symmetric matrix; do not leave one triangle empty.
- Units: `1/(J·m·s)`; document.
- `SkipVacancies` and `YieldImmobile` are explicit options; do not silently drop cancelling ensembles.

#### Static tracking

- For each static tracker `(cell, position, particle)`:
  - `v = ToCartesian(displacement) / t` in m/s.
- Provide position (fractional/4D), particle, velocity vector, and magnitude.
- This is a local velocity, not mobility; no field normalization.

#### Jump statistics

- Attempt histograms (all attempted transitions) and accepted counters are separate.
- Per global tracker:
  - `AttemptCount = Σ bins + underflow + overflow`;
  - `AcceptedCount` from counters;
  - `EstimatedJumpCount` from the attempt histogram reweighted by `K_norm * exp(-E/kT)` if needed;
  - `MigrationRate = EstimatedJumpCount / (t N)` or `AcceptedCount/(tN)`; expose both.
- `GetEdgeEntries`, `GetPositiveConfigurationEntries`, `GetNegativeConfigurationEntries`,
  `GetTotalBarrierEntries` return `(energy, count)` arrays.
- Document the difference from the original's rejected-only logging.

#### Coordination

- For each center site/position and center particle, sample neighbors within `maxDistance` (Å).
- Shells are grouped by distance in femtometres; output sorted by distance.
- `CN = count / number_of_centers`;
- Average per symmetry-equivalent site (`CellSiteCoordination`) or per lattice position
  (`LocalSiteCoordination`).
- Skip void occupations; map target particles by index.
- Memory: local coordination is large; document and stream/limit.

#### MMCFE free energy

- Parse the new MMCFE log format (step 19), not the legacy `mmcfelog.db`.
- Implement the Wang–Landau/Valleau–Card chain with explicit per-cell normalization.
- Unit-cell count is `A*B*C`, not `A+B+C`.
- Expose `F(T)`, `U(T)`, `S(T)`, per-defect variants, and SEM over samples.
- Document the normalization and validate against a two-state analytic model.

### 5. Collection

`mocassin collect <project.msl> --dir <root>`:

1. Query all job IDs and expected folders (`Job%05d`).
2. For each folder:
   - read `solver.json` (exit code, abort reason, state paths);
   - read `run.mcs`/`prerun.mcs` if present;
   - read `stdout.log`/`stderr.log`;
   - validate the MessagePack state schema/signature and lattice size;
   - compute SHA-256.
3. Update `results` in a transaction (batch ~100 rows), idempotently:
   - status, exit code, timestamps, wall time, stdout, blobs, hashes, error message.
4. Skip unchanged results unless `--force`.
5. Report counts: collected, skipped, missing, invalid, failed.
6. Never delete job folders.

- `--jobs` limits collection.
- `--dry-run` validates without writing.
- `--json` emits a summary.
- The collector is implemented in .NET, not Python, so it shares the MCS reader/validation.

### 6. Evaluation CLI

```text
mocassin evaluate <project.msl> --query <name> [--where <expr>] [-o <file>]
                  [--format csv|json|parquet] [--jobs <range>] [--parallel <n>]
```

Queries:

- `particle-count`, `ensemble-meta`, `lattice-meta`;
- `displacement`, `msd`, `diffusion`;
- `mobility`, `transition-mobility`;
- `correlation`, `onsager`;
- `static-tracking`, `jump-statistics`, `coordination`;
- `mmcfe`.

- `--where` supports a safe expression subset (kind, collection, config, temperature, field, status) or a small
  filter DSL; no arbitrary code.
- Output includes units, job metadata, and query parameters in a header/manifest.
- CSV is long-format (`job,collection,config,particle,direction,value,unit`).
- JSON is an array of result objects.
- Parquet is optional (step 21) for large datasets.

## Implementation tasks

- [ ] Implement `ResultPackage` with Microsoft.Data.Sqlite and async queries.
- [ ] Implement the MessagePack `McsReader` with schema validation and SHA-256 checks.
- [ ] Implement `JobContext`, model cache, and lattice geometry.
- [ ] Implement `JobEvaluation<T>` with bounded parallelism/cancellation.
- [ ] Implement all evaluation classes and formulas.
- [ ] Implement `mocassin collect` with transactional/idempotent updates.
- [ ] Implement `mocassin evaluate` and CSV/JSON export.
- [ ] Fix the known formula/unit issues (D_sigma, A*B*C, symmetric correlation).
- [ ] Add golden result fixtures and analytic tests.
- [ ] Document units and formulas.

## Acceptance criteria

- [ ] A collected ceria job can be queried and evaluated by ID/metadata.
- [ ] Particle counts, MSD, D, mobility, conductivity, and correlation match analytic/hand calculations.
- [ ] `D_sigma` is dimensionally correct and consistent with `σ = n q² D/(k_B T)`.
- [ ] Zero-field jobs return `n/a` transport values, not NaN/Inf.
- [ ] Correlation/Onsager matrices are symmetric.
- [ ] `collect` is idempotent and validates states before writing.
- [ ] Corrupt/truncated states are rejected with job context.
- [ ] Evaluation of 1,000 jobs streams without loading all blobs.
- [ ] CLI CSV/JSON output includes units and metadata.
- [ ] Documentation written

## Documentation

At this point, document results and evaluation:

- `docs/user-guide/results.md` — collecting, querying, evaluating, exporting results.
- `docs/reference/mcs-format.md` — reader validation and the MessagePack contract.
- `docs/physics/tracking.md` and `docs/physics/transport.md` — formulas, units, assumptions.
- `docs/reference/cli.md` — `collect` and `evaluate`.
- `docs/development/testing-and-parity.md` — golden result fixtures and formula tests.

## Tests

| Test | Assertion |
| --- | --- |
| MCS reader | MessagePack schema/version/lengths; corrupt files rejected |
| Counts | Lattice counts per particle |
| MSD/D | Analytic random walk |
| Mobility/σ | Nernst–Einstein consistency |
| Correlation | Symmetry and known two-particle case |
| Static tracking | Velocity for a known displacement |
| Jump stats | Bins, under/overflow, accepted vs attempts |
| Coordination | Known shells/distances |
| Collect | Idempotent, transactional, invalid states skipped |
| Streaming | Memory bounded for many jobs |

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Formula/unit errors | One `Physics` module, dimensional tests, golden values |
| Large blobs | Metadata-first queries, on-demand loading, bounded in-memory state |
| Reader lifetime bugs | Owned arrays, no pinned buffers, deterministic disposal |
| Attempt vs accepted semantics | Expose both, document |
| Collection races | Transactions, hashes, idempotent updates |

## Done when

Users can collect solver output and evaluate standard transport, diffusion, correlation, coordination, and
jump statistics through a safe, streaming API and CLI, with correct units and formulas.
