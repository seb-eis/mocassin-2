# Step 18 — Tracking, Statistics, and State Writer

**Phase:** D. Solver  
**Depends on:** 16, 17
**Delivers:** Global/static/mobile movement tracking, jump histograms, statistics dumps, derived transport
quantities, and the versioned MessagePack state writer/reader.

## Implementation status

- **Status:** Not started
- **Step branch:** —
- **Step CI:** —
- **Main merge:** —
- **Last updated:** —

| Feature | Branch | Status | Local gates | Step CI | Squash commit |
| --- | --- | --- | --- | --- | --- |
| — | — | — | — | — | — |

## Goal

Record enough information to evaluate conductivity, mobility, diffusion, correlations, local flow, and jump
barriers without storing full trajectories. Write a safe, self-describing state file that .NET can read and
Rust can resume from.

## Original reference

- Trackers are fractional `Vector3` accumulators:
  - **global**: per `(jump collection, particle)`; every mobile path node adds its movement;
  - **mobile**: one per potentially mobile particle (including passive); tracker id transferred using
    `JumpRule.TrackerOrderCode`;
  - **static**: per `(cell, position, particle)`; every path node adds movement for its current particle.
- Jump statistics: four histograms per global tracker:
  1. raw `Σ E_i(S1)`;
  2. positive conformation `ΔE_conf`;
  3. negative conformation `|ΔE_conf|`;
  4. total barrier including field.
- Histogram: 1000 bins, default `[0,5) eV`, under/overflow counters, `stepping_inverse = 1000/(max-min)`.
- The original logs rejected and end-unstable cycles; accepted/start-unstable data is added via the tracking
  advance.
- Statistics formulas:
  - `R = Σ displacements`, `R²` component-wise and scalar;
  - `Mean<R> = R/count`, `Mean<R²> = R²/count`;
  - `D_x = Mean<R²>_x/(2t)`, `D_total = Mean<R²>/(6t)`;
  - `MigrationRate = Mcs/(t*count)`;
  - `u_x = Mean<R>_x/(normField_x*|E|*t)`, `u_total = (Mean<R>·ê)/(t|E|)`;
  - `σ = u*q*c*e`;
  - `D_σ = σ*kT/(c*q²)` (the original evaluation's `u*z*kT` is dimensionally suspect; re-derive).
- `.mcs` is a raw native buffer dump: 56-byte header, 80-byte meta, lattice, counters, trackers, mapping,
  32,160-byte histograms; no magic/version/checksum/alignment.
- Known quirks: state sections unaligned; MMC uses `-1` offsets; 32-bit offsets cap at 2 GiB; `RawMax` not
  reset; mobile trackers include passive species.

## Design

### 1. Tracker state

```rust
pub struct TrackerState {
    pub global: Vec<[f64; 3]>,        // fractional displacement
    pub mobile: Vec<[f64; 3]>,        // fractional displacement
    pub static_: Vec<[f64; 3]>,       // fractional displacement
    pub mobile_position: Vec<u32>,    // linear position index per mobile tracker
    pub global_models: Vec<GlobalTrackerModel>,
    pub static_models: Vec<StaticTrackerModel>,
}
```

- All displacements are fractional vectors; conversion to Cartesian happens at output.
- Global tracker index = compiled `(collection, particle)` mapping.
- Static tracker index = `cell_index * static_per_cell + static_mapping[position][particle]`.
- Mobile tracker index = assigned at initialization to each potentially mobile position; the mapping is
  updated when a tracked particle moves.
- Trackers accumulate on accepted and start-unstable transitions (state advances), not on rejected/blocked.
- For each path node `i`:
  - `movement[i]` is the fractional displacement of the particle that leaves/enters that node;
  - add it to the global tracker for `(collection, particle_at_node)`;
  - add it to the static tracker for `(cell, position, particle_at_node)`;
  - transfer/accumulate the mobile tracker according to the rule's `tracker_order_code`.
- `tracker_order_code[i]` defines which path node's mobile tracker id receives the movement at node `i`.
  - The rule identifies the moving particle identity across the path.
  - Implement the transfer as a permutation of tracker ids before adding the movement, so the tracker follows
    the physical particle.
- Validate tracker order codes at load time (values < path length or a documented sentinel).

### 2. Jump histograms

```rust
pub struct JumpHistogram {
    pub min_ev: f64,
    pub max_ev: f64,
    pub inverse_step: f64,
    pub overflow: u64,
    pub underflow: u64,
    pub bins: Vec<u64>, // 1000 by default
}

pub struct JumpStatistics {
    pub edge: JumpHistogram,        // raw S1 contribution
    pub positive_conf: JumpHistogram,
    pub negative_conf: JumpHistogram,
    pub total: JumpHistogram,       // total barrier incl. field
}
```

- Default range `[0,5) eV` (the original code's actual default; the user guide's 10 eV is wrong). Make the
  range configurable with `--jump-log-max-ev`.
- Bin index = `(value - min) * inverse_step`; `< 0` → underflow; `>= bins` → overflow.
- Record **every attempted transition** (accepted, rejected, start/end unstable, blocked with energies) in the
  attempt histograms, plus outcome counters. This is a deliberate improvement over the original's
  rejected-only logging; the evaluation API exposes both attempt and accepted-jump statistics.
- For blocked cycles with no rule, do not record energy histograms (no barrier); increment the blocked
  counter.
- For accepted/start-unstable cycles, record the barrier and also increment the per-tracker accepted count.
- `NoJumpLogging` disables allocation and recording.
- Histograms are per global tracker; blocked/no-rule cycles are per particle counter only.

### 3. Derived statistics

At each statistics dump (and at the end), compute per particle and per global tracker:

```text
t = simulated_time_s
N_s = tracked ensemble count (species/global tracker, not total particles)
R_vec_s = Σ tracker displacements in s (fractional -> Cartesian, meters)
R2_vec_s = Σ component-wise |d|²
R2_scalar_s = Σ |d|²

MeanR_s = R_vec_s / N_s
MeanR2_s = R2_vec_s / N_s
D_x,s = MeanR2_x,s / (2t)
D_y,s = MeanR2_y,s / (2t)
D_z,s = MeanR2_z,s / (2t)
D_total,s = (MeanR2_x,s + MeanR2_y,s + MeanR2_z,s) / (6t)

u_field,s = (MeanR_s · ê) / (|E| t)          // m²/(V·s)
σ_s = u_field,s * z_s * n_s * e              // S/m
D_σ,s = σ_s * k_B_SI * T / (n_s * (z_s*e)^2) // m²/s
MigrationRate_s = MCS_s / (t * N_s)          // Hz
σ_total = Σ_s σ_s
```

- `N_s` is the number of particles in the tracked ensemble, not the total lattice particle count. `MCS_s` is
  the corresponding step count.
- `ê` is the normalized Cartesian field direction; if `|E| = 0`, `t = 0`, or `z_s = 0`, transport quantities
  are `n/a` (not NaN). Vacancies and neutral particles are not charge carriers.
- `z_s` is the species charge number; `e` is the elementary charge.
- `n_s` is the species number density `N_s / V` with `V` in m³.
- `D_σ,s` is the per-species Nernst–Einstein coefficient; `D_tracer,s` is the origin-based MSD value. The
  Haven ratio is `H_s = D_tracer,s / D_σ,s`.
- Report per-species and total values; do not silently merge species (ADR 0012).
- Correlation/Onsager coefficients are computed by the evaluation API from mobile trackers (step 20); the
  solver writes the raw trackers.
- Do not emit `inf`/`NaN` for zero field or zero time; use `Option<f64>`/`n/a`.

### 4. Statistics output

Human-readable stdout, matching the original's spirit but with stable, parseable lines:

```text
<STATISTICS_DUMP>
Status => Completion               : 1000000 / 2000000 (+5.000e+001%)
Status => Pre run completion       : 100000 / 100000 (+1.000e+002%)
Cycles => Total                    : 2500000
Cycles => Success                  : 1000000
Time => Program running            : P00DT00H00M12S
Time => Completion ETA             : P00DT00H00M12S
Time => Simulated                  : +7.366583808425e+009
Time => Per cycle                  : +1.347572815534e-007
Rate => Cycle                      : +7.420749279539e+006
Rate => Mcs                        : +7.274904178674e+006
Energy => Lattice                  : +0.000000000000e+000
Probability => Norm. Factor        : +2.888856395003e+018
```

Per-particle blocks include ID, charge, count, cycle counters, conductivity/mobility/diffusion/movement lines
where applicable. Use the original labels where possible so existing analysis scripts can be adapted, but
document the new units and the attempt-histogram semantics.

- Emit every ~1 % of MCSP and at the end.
- Emit a final line with:
  - `ERR_CODE`, state flags, abort reason, wall time, simulated time, output paths.
- Machine-readable structured events go to stderr (JSON Lines) when enabled; stdout remains the statistics
  dump.

### 5. MessagePack state schema

The complete schema is defined in
[`02-data-contracts.md`](../02-data-contracts.md#4-result-state-file-mcs-messagepack-version-1). The writer
serializes a single `McsState` value with `rmp-serde` (Rust) or `MessagePack-CSharp` (.NET).

Top-level map:

```text
format: "mocassin.mcs"
formatVersion: 1
metadata: { ... }
lattice: bin                      // u8 per linearized position
counters: [ { particleIndex, skip, success, rejected, blocked, startUnstable, endUnstable } ]
globalTrackers: [ [f64;3] ]       // KMC only
mobileTrackers: [ [f64;3] ]       // KMC only
staticTrackers: [ [f64;3] ]       // KMC only
mobileMapping: [ u32 ]            // KMC only
jumpStatistics: [ { ... } ]       // KMC only
energySamples: [ { mcs, latticeEnergyEv, acceptedDeltaEv } ]
particlePositions: [ u32 ]        // optional diagnostics
```

- `metadata` contains the solver version, model signature, job id, kind/phase, lattice size, P1 position count,
  particle count, tracker counts, cell vectors, particle charges/symbols/vacancy flags, temperature, electric
  field, cycle/MCS counts, simulated time, normalization, lattice energy, flags, abort reason, seed hash, units,
  and optional runner information. This makes the state self-contained for Python analysis.
- The lattice is a MessagePack `bin`; numeric arrays use native MessagePack types.
- MMC omits all KMC-only fields. `prerun.mcs` uses the same schema with `metadata.phase = "prerun"`.
- There is no custom header, section table, alignment, or CRC. `format`/`formatVersion` are ordinary map fields.
- The document is expected to stay below 4 GiB. The writer checks the encoded size against a configurable cap
  (default 3.5 GiB) and fails with an actionable error instead of truncating.

### 6. Writer

1. Build the `McsState` from the context, trackers, counters, and histograms.
2. Serialize directly to a temp file in the target directory using `rmp_serde::Serializer` /
   `MessagePackSerializer.Serialize(stream, state)`.
3. Flush/fsync, verify the size cap, then atomically rename to `run.mcs` or `prerun.mcs`.
4. Keep the previous file as a `.bak` if configured; never leave a partial `.mcs`.
5. `NoSaving` skips the write but still prints statistics.
6. The collector computes the SHA-256 and stores it in `results.run_state_sha256` in `.msr`; the state itself
   has no checksum.

### 7. Reader for resume

- Deserialize with `rmp_serde::from_read` / `MessagePackSerializer.Deserialize<McsState>`.
- Validate `format`, `formatVersion`, metadata, lattice length, particle indices, tracker counts, mobile
  mapping, histogram bin counts, and finite numeric values.
- Verify the model signature and lattice size match the job.
- Ignore unknown fields for forward compatibility; reject unknown `format` or unsupported `formatVersion`.
- Load the full document into memory for resume; the documented workload is bounded below 4 GiB.
- Provide a `StateReader` that can also be used by the .NET reader tests.

### 8. Determinism and integrity

- Trackers/histograms are updated in a fixed order; counters use `u64`.
- The writer uses a fixed field/array order. MessagePack is deterministic for the same writer/version/platform;
  byte-identical output across C# and Rust is not required.
- Integrity is provided by the `.msr` SHA-256, not by a state-file container checksum.
- A malformed/truncated MessagePack document is rejected by the decoder; a semantically invalid document is
  rejected by schema validation.
- Golden fixtures freeze the canonical JSON summary and counters for a small run.
- The size cap and lattice bounds are checked before writing.

## Implementation tasks

- [ ] Implement global/static/mobile tracker state and updates.
- [ ] Implement tracker order code transfer.
- [ ] Implement jump histograms and attempt/accepted counters.
- [ ] Implement statistics computation and stdout formatting.
- [ ] Implement the MessagePack `McsState` writer with atomic rename and size checks.
- [ ] Implement the MessagePack `McsState` reader for resume.
- [ ] Implement energy samples and abort reason metadata.
- [ ] Implement `--jump-log-max-ev`, `NoSaving`, `NoJumpLogging`.
- [ ] Add golden state fixtures and round-trip tests.
- [ ] Add tracker/statistics analytic tests, including per-species totals and origin-based MSD.

## Acceptance criteria

- [ ] A single-particle random walk produces the expected origin-based MSD, D, and tracker displacements.
- [ ] Per-species and total transport values use the tracked ensemble count, not total particles.
- [ ] Global trackers separate transition contributions correctly.
- [ ] Static trackers reproduce per-position flow for a simple lattice.
- [ ] Mobile trackers follow particle identity across a 3-site migration.
- [ ] Histograms bin values correctly, including under/overflow.
- [ ] MessagePack state files round-trip through the reader and pass semantic validation.
- [ ] Resume restores lattice, counters, trackers, histograms, RNG state, `K_fix`/`K_norm`/`RawMax`, goals, and
  time, and continues identically.
- [ ] Corrupt/truncated state files are rejected.
- [ ] Statistics output contains no NaN/Inf for zero-field MMC/KMC cases.
- [ ] Documentation written

## Documentation

At this point, document tracking and state:

- `docs/physics/tracking.md` — global/mobile/static trackers, MSD, diffusion, correlations.
- `docs/reference/mcs-format.md` — MessagePack schema, metadata, counters, histograms, size/integrity.
- `docs/reference/msr-format.md` — how collected state hashes are stored and verified.
- `docs/user-guide/results.md` — what the state file enables.
- `docs/development/solver-architecture.md` — tracker updates and resume behavior.

## Tests

| Test | Assertion |
| --- | --- |
| Tracker property | Random accepted transitions keep displacement/MSD identities and particle identities consistent |
| State acceptance | Real KMC/MMC runs write `.mcs` files that the .NET and Python readers parse identically |
| Trackers | Known displacement sums |
| Order code | Particle identity transfer |
| MSD/D | Analytic 1D/3D random walk |
| Conductivity | Nernst–Einstein consistency |
| Histograms | Bin edges, under/overflow |
| State round-trip | Writer → reader → identical fields |
| Species transport | Per-species and total conductivity/Haven values match hand calculations |
| Origin MSD | Tracker displacement from the initial state matches the analytic random walk |
| Resume | Mid-run continuation |
| Corruption | Bad MessagePack/version/array length rejected |
| Compatibility | Unknown fields ignored; unknown format version rejected |
| MMC state | KMC-only fields absent/empty |
| Output | Parseable statistics lines |

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Tracking semantics ambiguity | Document per-tracker update rules; analytic tests |
| Histogram semantics differ from original | Document attempt vs. accepted; expose both |
| State format bugs | Golden fixtures, `.msr` SHA-256, fuzz reader |
| Large state files | Stream serialization, 3.5 GiB cap, external compression |
| Resume mismatch | Signature/lattice checks; reject incompatible state |

## Done when

Every accepted transition updates the correct trackers, statistics are physically meaningful, and state files
are self-describing, versioned, resumable, and readable by .NET.
