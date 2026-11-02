# Step 18 — Tracking, Statistics, and State Writer

**Phase:** D. Solver  
**Depends on:** 16, 17  
**Delivers:** Global/static/mobile movement tracking, jump histograms, statistics dumps, derived transport
quantities, and the versioned `MCS2` state writer/reader.

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
N = current particle count
R_vec = Σ tracker displacements (fractional -> Cartesian, meters)
R2_vec = Σ component-wise |d|²
R2_scalar = Σ |d|²

MeanR = R_vec / N
MeanR2 = R2_vec / N
D_x = MeanR2_x / (2t)
D_y = MeanR2_y / (2t)
D_z = MeanR2_z / (2t)
D_r = (MeanR2_x + MeanR2_y + MeanR2_z) / (6t)

u_field = (MeanR · ê) / (|E| t)          // m²/(V·s)
σ = u_field * z * n * e                  // S/m
σ_norm = u_field * n * e                 // z=1
D_sigma = σ * k_B_SI * T / (n * (z*e)^2) // m²/s
MigrationRate = MCS / (t * N)            // Hz
```

- `ê` is the normalized Cartesian field direction; if `|E| = 0`, transport quantities are `n/a` (not NaN).
- `z` is the particle charge number; `e` is the elementary charge.
- `n` is the particle density `N / V` with `V` in m³.
- `D_sigma` is the Nernst–Einstein diffusion coefficient; `D_tracer` is the MSD-based value. The Haven ratio
  is `D_tracer / D_sigma`.
- `D_total` uses the 3D MSD with denominator 6.
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

### 5. `MCS2` writer

Layout defined in [`02-data-contracts.md`](../02-data-contracts.md):

- 64-byte header, 40-byte section entries, 8-byte alignment, CRC32C per section.
- Sections:
  1. metadata JSON (solver version, model signature, job id, flags, abort reason, units);
  2. lattice (u8 per position);
  3. cycle counters (per particle);
  4. global trackers (3×f64 per tracker);
  5. mobile trackers (3×f64);
  6. static trackers (3×f64);
  7. mobile mapping (u32);
  8. jump histograms (4×1000 u64 + under/overflow);
  9. energy samples (MCS, lattice energy, mean ΔE);
  10. particle positions (optional diagnostics).
- Writer:
  1. write a placeholder header/table;
  2. append sections with 8-byte alignment, computing CRC32C incrementally;
  3. back-patch header/table and write the footer/file size;
  4. fsync and atomically rename; keep a `.bak` of the previous state.
- MMC writes only metadata, lattice, counters, and energy samples; absent sections are omitted, not `-1`.
- `NoSaving` skips state writes but still prints statistics.
- State size is bounded; use u64 counts and checked arithmetic.
- The writer must not allocate large temporary buffers; stream sections.

### 6. `MCS2` reader (for resume)

- Validate magic/version/header size/CRC/file size.
- Validate section offsets/lengths/counts and alignment.
- Load only the sections needed for resume (lattice, counters, trackers, histograms, meta).
- Verify the model signature and lattice size match the job.
- Reject NaN/Inf where invalid; reject duplicate section IDs.
- Provide a `StateReader` that can also be used by the .NET reader tests.

### 7. Resume and abort reasons

- Persist:
  - lattice;
  - counters;
  - trackers and mobile mapping;
  - histograms;
  - RNG state;
  - `simulated_time`, `K_norm`, `RawMax`, `K_fix`;
  - state flags (pre-run/main, completed, aborted);
  - abort reason.
- On resume, restore all of the above and continue from the next cycle.
- A completed state is detected by the completed flag and target MCSP; exit with `AlreadyCompleted`.
- Abort reasons: `Completed`, `EquilibriumReached`, `TimeLimit`, `RateLimit`, `Cancelled`, `NumericalError`,
  `UserAbort`, `AlreadyCompleted`.

### 8. Determinism and checksums

- Trackers/histograms are updated in a fixed order.
- Counters use `u64`; no overflow in practical runs.
- CRC32C covers section bytes; a corrupted state is rejected, not silently loaded.
- Golden state fixtures freeze the section layout and counters for a small run.

## Implementation tasks

- [ ] Implement global/static/mobile tracker state and updates.
- [ ] Implement tracker order code transfer.
- [ ] Implement jump histograms and attempt/accepted counters.
- [ ] Implement statistics computation and stdout formatting.
- [ ] Implement the `MCS2` writer with header/section/CRC/atomic rename.
- [ ] Implement the `MCS2` reader for resume.
- [ ] Implement energy samples and abort reason metadata.
- [ ] Implement `--jump-log-max-ev`, `NoSaving`, `NoJumpLogging`.
- [ ] Add golden state fixtures and round-trip tests.
- [ ] Add tracker/statistics analytic tests.

## Acceptance criteria

- [ ] A single-particle random walk produces the expected MSD, D, and tracker displacements.
- [ ] Global trackers separate transition contributions correctly.
- [ ] Static trackers reproduce per-position flow for a simple lattice.
- [ ] Mobile trackers follow particle identity across a 3-site migration.
- [ ] Histograms bin values correctly, including under/overflow.
- [ ] `MCS2` files pass CRC and round-trip through the reader.
- [ ] Resume from a mid-run state continues identically.
- [ ] Corrupt/truncated state files are rejected.
- [ ] Statistics output contains no NaN/Inf for zero-field MMC/KMC cases.

## Tests

| Test | Assertion |
| --- | --- |
| Trackers | Known displacement sums |
| Order code | Particle identity transfer |
| MSD/D | Analytic 1D/3D random walk |
| Conductivity | Nernst–Einstein consistency |
| Histograms | Bin edges, under/overflow |
| State round-trip | Writer → reader → identical fields |
| Resume | Mid-run continuation |
| Corruption | Bad CRC/offset/version rejected |
| MMC state | Reduced sections |
| Output | Parseable statistics lines |

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Tracking semantics ambiguity | Document per-tracker update rules; analytic tests |
| Histogram semantics differ from original | Document attempt vs. accepted; expose both |
| State format bugs | Golden fixtures, CRC, fuzz reader |
| Large state files | Stream sections, optional compression, no full-buffer copy |
| Resume mismatch | Signature/lattice checks; reject incompatible state |

## Done when

Every accepted transition updates the correct trackers, statistics are physically meaningful, and state files
are self-describing, checksummed, resumable, and readable by .NET.
