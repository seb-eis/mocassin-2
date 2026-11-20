# Step 16 — Kinetic Monte Carlo Engine

**Phase:** D. Solver  
**Depends on:** 15
**Delivers:** A rejection KMC engine with direction selection, rule lookup, normalization pre-run, time
evolution, all cycle outcomes, abort conditions, and deterministic seeding.

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

Implement the time-dependent solver that reproduces the original KMC physics: select a possible event, compute
its barrier, accept/reject according to normalized Boltzmann statistics, advance time, and track the system
until the target MCSP or an abort condition.

## Original reference

- Rejection KMC loop in blocks of ~1 % of the total MCSP.
- Selection:
  - draw a uniform integer over `SelectableJumpCount`;
  - walk direction-count pools ascending;
  - choose an environment and relative direction uniformly over selectable **directions** (not rate-weighted).
- Path: build the periodic-trimmed path from the direction's `JumpSequence`.
- Rule: find the rule whose `StateCode0` matches the path's start state; none → site blocked.
- Frequency pre-check: at job initialization derive
  `FrequencyFactor = rule.frequency_hz / job.max_attempt_frequency_hz` (when the job cap is `null`, use the
  maximum of the model's absolute `frequency_hz` values); if `FrequencyFactor < 1 − ε` and `< U`, skip
  without energy work. The factor is a derived job value, not a model payload field.
- Energies: S0/S1/S2, field terms; `P_raw = exp(−b01)`, `P_norm = P_raw * K_norm`.
- Outcomes:
  - `b10_noField ≤ 0` → end-unstable (count, histogram, advance time, no state change);
  - `b01_noField ≤ 0` → start-unstable (count, tracking, advance state, no time);
  - accepted → MCS++, advance time, tracking, state, pool update;
  - rejected → count, advance time, histogram;
  - blocked → count, advance time;
  - skipped → count, advance time.
- Time step: `dt = dof * K_norm / (νmax * selectable_count)`; recomputed when `K_norm` or count changes.
- Pre-run:
  - chunks of `PreRunMcsp * mobiles / 100`;
  - after each chunk, `K_auto = 1 / RawMax` (ignore values > 1, with a back-jump safety term);
  - before pre-run, `RawMax = 0` so only `K_fix` applies;
  - finish resets counters, histograms, trackers, simulated time, and adds overshoot to the main goal.
- Aborts: pre-run target, timeout (after ≥1 s, ETA > limit), rate (`CycleRate < MinimalSuccessRate`), completion
  (`McsCount ≥ TotalGoal`).
- Known quirks: `MinimalSuccessRate` compares cycles/s, not MCS/s; a pre-run < 100 cycles can hang; abort checks
  happen between blocks, so overshoot is possible; `RawMax` is not reset on resume.

## Design

### 1. Engine API

```rust
pub struct KmcEngine<'a> {
    ctx: &'a mut SimulationContext,
    energy: EnergyEngine<'a>,
    stats: StatisticsCollector,
}

pub struct KmcOutcome {
    pub kind: CycleOutcome,
    pub selected: SelectedJump,
    pub energies: Option<TransitionEnergies>,
    pub accepted: bool,
    pub time_advance_s: f64,
}

pub enum CycleOutcome {
    Accepted,
    Rejected,
    Skipped,
    SiteBlocked,
    StartUnstable,
    EndUnstable,
}

impl KmcEngine<'_> {
    pub fn run(&mut self, cancel: &AtomicBool, progress: &mut dyn ProgressSink) -> Result<KmcSummary>;
    pub fn run_cycle(&mut self) -> KmcOutcome;
}
```

- `run_cycle` is deterministic and allocation-free.
- `run` manages blocks, pre-run, aborts, statistics, and state saving.

### 2. Selection

- `SelectableJumpCount` is maintained by the context (step 14).
- Draw `r = rng.next_bounded(selectable_count)`.
- Walk direction pools in ascending `direction_count` order:
  - subtract `pool.count` until `r < pool.count`;
  - `env = pool[r / direction_count]`;
  - `rel = r % direction_count`.
- If the selected `(position, particle)` is no longer valid (stale pool), repair the pool and retry; this
  should be an invariant violation, not a normal path.
- The selection is uniform over directions, matching the original. Do not switch to rate-weighted selection in
  v1; it would change the rejection algorithm and normalization semantics.
- Record the selected collection, direction, particle, and position for counters/tracking.

### 3. Path and rule lookup

- Build the path from the direction's relative offsets:
  `path[0] = selected position`; `path[i] = trim(path[i-1] + offset[i-1])`.
- Decode the start state code from the path's current particles.
- Rule lookup:
  - build a per-collection map from `StateCode0` to rule indices at initialization;
  - lookup is O(1)/O(log R), not a linear scan;
  - if multiple rules share `StateCode0`, the compiler must have rejected ambiguity; the solver asserts.
- If no rule matches, the cycle is `SiteBlocked`.
- **Exact occupation precheck (Step 13 M-3 assignment).** After the rule is found and before any energy work,
  validate every non-void particle in the rule's `state0`/`state1`/`state2` at each path node against the
  resolved path position's allowed mask. Void is the empty occupation and is always allowed; the masks do not
  list void. A mismatch is a model/data error (exit 12), not `SiteBlocked` and not a silent skip. Because the
  selected direction and its path are known here, the check is exact and does not reject a rule that is valid
  only along its own direction. This is the Step 16 assignment recorded in Step 13; Step 14 builds the
  context/pools but does not have the rule→selected-direction pair.

### 4. Frequency pre-check

- Build `frequency_factor = rule.frequency_hz / max_attempt_frequency_hz` once at job initialization (the
  denominator is the job's configured cap or the model maximum). If `frequency_factor < 1 − 1e-12`:
  - draw `u = rng.next_f64()`;
  - if `u > frequency_factor`, increment skip and time, and return `Skipped` without energy work.
- The pre-check uses one RNG draw and must occur before any energy computation, matching the original.
- The pre-check is optional (`UseFrequencyPrecheck`); disabling it changes trajectory but not the long-time
  distribution. Default: enabled. The `Deterministic` job flag disables it so the RNG stream does not depend on
  the pre-check shortcut.

### 5. Energy and acceptance

- Compute `TransitionEnergies` (step 15).
- `field_term` is included in forward/backward barriers.
- Acceptance:
  - `p_raw = exp(-barrier_forward)`;
  - `p_norm = p_raw * K_norm`;
  - accept if `p_norm >= rng.next_f64()`.
- Outcome order (match the original):
  1. If `barrier_backward_no_field <= 0` → `EndUnstable`.
  2. If `barrier_forward_no_field <= 0` → `StartUnstable`.
  3. Else draw `u` and accept/reject.
- `StartUnstable` applies the state change, increments the unstable-start counter, and updates the pool, but
  does **not** increment MCS and does **not** advance time.
- `EndUnstable` does not change state, increments the unstable-end counter, logs histograms, and advances time.
- `Rejected` advances time and logs histograms.
- `SiteBlocked` advances time and counts.
- `Skipped` advances time and counts.
- `Accepted` applies the state change, advances time, counts MCS, and updates trackers.

### 6. Time step

- `dt = dof * K_norm / (nu_max * selectable_count)`.
- `dof = 2` if the `.msl` job flag `useDualDofCorrection` (bit 4) is set, else 1; document that the original
  used this to account for forward/backward directions.
- `nu_max` is the maximum attempt frequency in the simulation (`AttemptFrequencyModulus`), optionally
  overridden by `MaxAttemptFrequencyHz`.
- Recompute `dt` when:
  - `K_norm` changes (pre-run normalization update);
  - `selectable_count` changes due to a state change/pool update.
- Time advances on every outcome except `StartUnstable`.
- `simulated_time_s` is accumulated as `f64`; avoid catastrophic cancellation by using a compensated sum if
  needed for long runs.
- Time-step consistency test: for a single barrier with one direction, `dt = 1 / (ν0 exp(-ΔE/kT))` after
  normalization.

### 7. Pre-run and normalization

- Pre-run runs `PreRunMcsp * mobile_count` cycles in 100 chunks (at least 1 cycle per chunk).
- Each cycle uses the same selection/energy/acceptance logic but does not count toward the main MCS goal.
- After each chunk:
  - compute the observed raw maximum acceptance probability;
  - update `RawMax = max(RawMax, observed)` (never decrease);
  - `K_auto = 1 / RawMax` (if finite and > 0);
  - `K_norm = K_fix * K_auto`;
  - recompute `dt`.
- The raw maximum should include a safety term for the reverse/back-jump probability; implement the same
  safety logic as the original or document the chosen conservative bound.
- At pre-run completion:
  - reset counters, histograms, trackers, and simulated time;
  - clear the pre-run flag;
  - add the overshoot (`pre_run_cycles - pre_run_goal`) to the main goal;
  - save `prerun.mcs` (unless `NoSaving`).
- If `PreRunMcsp * mobile_count < 100`, use a chunk size of 1 and log a warning (avoid the original hang).
- A pre-run of 0 disables normalization; `K_norm = K_fix`.

### 8. Main loop, blocks, and aborts

- `total_goal = pre_run_goal + target_mcsp * mobile_count`, rounded up to a multiple of 100.
- `cycles_per_block = max(1, total_goal / 100)`.
- Each block runs `cycles_per_block` cycles, then:
  - saves state (unless `NoSaving`);
  - prints statistics;
  - checks abort conditions;
  - reports progress.
- Abort conditions:
  - `mcs >= main_goal` → completed;
  - elapsed wall time > `time_limit_s` (check after ≥1 s) → timeout;
  - `mcs_rate < min_success_rate` → rate abort (define as MCS/s; document the deviation from the original
    cycles/s);
  - cancellation token → cancelled;
  - numerical failure (NaN energy, invalid state) → error.
- Abort checks between blocks allow up to one block of overshoot; document it.
- The main loop must not allocate; statistics collection uses preallocated buffers.

### 9. Resume and completion

- If `run.mcs` exists and is complete, return `AlreadyCompleted` with a distinct exit code/message.
- If `prerun.mcs` exists, resume the pre-run or transition to the main run according to flags.
- Resume restores:
  - lattice and environment state;
  - counters, trackers, histograms;
  - RNG state;
  - simulated time;
  - `K_norm`, `RawMax`, `K_fix`.
- Never reset `RawMax` on resume.
- Validate the state's model/lattice signature and sizes.

### 10. Determinism

- One RNG stream for the simulation, seeded from the job seed with a domain separation.
- Draw order per cycle:
  1. `next_bounded(selectable_count)` for selection;
  2. optional `next_f64` for frequency pre-check;
  3. optional `next_f64` for acceptance.
- No other RNG calls in the cycle.
- Pool iteration order and link order are deterministic.
- Floating-point operation order is fixed; avoid FMA contraction.
- Same package/binary/platform/seed → identical state content (lattice, counters, trackers, RNG, time);
  provenance timestamps may differ.

## Implementation tasks

- [ ] Implement selection pools and path building.
- [ ] Implement rule lookup maps and ambiguity assertions.
- [ ] Implement the cycle state machine and all outcomes.
- [ ] Implement acceptance, field terms, and time step.
- [ ] Implement the pre-run and normalization updates.
- [ ] Implement blocks, statistics, progress, save, and aborts.
- [ ] Implement resume/completion handling.
- [ ] Add a per-cycle trace mode for debugging (RNG state, selected direction, energies, outcome).
- [ ] Add analytic and regression tests.

## Acceptance criteria

- [ ] A single-barrier system reproduces the analytic residence time within 1 %.
- [ ] A 3-site migration runs to completion and conserves mass/charge.
- [ ] The exact per-direction occupation precheck rejects a deliberately invalid state particle at the selected
      direction's path position; void is exempt; a rule valid only along its own direction is not rejected.
- [ ] All six outcomes are exercised by targeted fixtures.
- [ ] Normalization converges and `K_norm` is stable.
- [ ] Pre-run reset zeroes time/counters/trackers but preserves `RawMax`.
- [ ] Abort conditions trigger at the expected boundaries.
- [ ] Resume from `prerun.mcs` continues correctly.
- [ ] Same seed produces identical counters and state across runs.
- [ ] No allocations in the cycle loop.
- [ ] Documentation written

## Documentation

At this point, document KMC:

- `docs/physics/kmc.md` — selection, acceptance, normalization, time step, all cycle outcomes.
- `docs/user-guide/running.md` — KMC pre-run, MCSP, field, abort conditions.
- `docs/development/solver-architecture.md` — KMC engine state machine and RNG draw order.
- `docs/troubleshooting/errors.md` — KMC aborts, unstable events, low rates.

## Tests

| Test | Assertion |
| --- | --- |
| KMC property | Any accepted sequence preserves mass/charge and never yields negative time |
| Acceptance | A real package runs a short KMC job within the analytic toy selection/time bounds |
| Residence time | Matches `1/(ν exp(-ΔE/kT))` |
| Selection | Uniform over directions; pool updates |
| Occupation precheck | Exact rule/direction state particle check; void exempt; own-direction-only rule accepted; mismatch is exit 12 |
| Outcomes | Forced barriers produce each outcome |
| Normalization | `K_auto`, `K_fix`, combined |
| Pre-run | Reset + RawMax preservation |
| Time | `dt` and total time for a known system |
| Aborts | MCSP, timeout, rate, cancel |
| Resume | State and counters restored |
| Determinism | Identical trace/counters for a seed |
| Conservation | Mass/charge after every accepted step |

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Normalization/time-step mismatch | Analytic tests and per-cycle trace |
| Selection bias | Uniform-direction tests and long-run statistics |
| Outcome ordering | Explicit state machine and fixtures |
| Pre-run edge cases | Chunk size floor, zero/very small pre-run tests |
| Abort overshoot | Document and test block boundaries |
| Resume corruption | Signature/lattice checks and golden states |

## Done when

KMC runs a package to completion with correct selection, energies, normalization, time, outcomes, aborts, and
resume, and matches analytic/statistical references.
