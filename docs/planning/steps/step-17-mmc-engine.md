# Step 17 — Metropolis Monte Carlo Engine

**Phase:** D. Solver  
**Depends on:** 15
**Delivers:** A Metropolis MMC engine with two-site exchanges, equilibrium break detection, time-independent
state evolution, and shared outcome/statistics infrastructure.

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

Implement the equilibrium sampler used for defect distributions, annealing, and initial states. It must obey
detailed balance, converge to the Boltzmann distribution for simple systems, and share the energy/state
machinery with KMC without inheriting KMC-only concepts (time, field, normalization).

## Original reference

- MMC uses the same uniform pool selection as KMC, plus a uniform offset cell over the whole supercell.
- Path:
  - `p0` = selected environment;
  - `p1` = environment at `(offset_cell.A, offset_cell.B, offset_cell.C, JumpSequence[0].D)`;
  - only the target sublattice index comes from the jump vector; the partner cell is uniform over the
    supercell.
- Rule lookup by `StateCode0`.
- `S0 = E[p0][code0_0] + E[p1][code0_1]`.
- If both positions are within the interaction range and links exist both ways, apply both pair/cluster deltas,
  read `S2`, restore backups; otherwise read `S2` directly (each environment energy already includes its
  independent neighborhood).
- `P = exp(-(S2 - S0))`; accept if `P >= U[0,1)`.
- No normalization, field, or unstable handling.
- Accepted:
  - append `ΔE` to the abort buffer;
  - `Mcs++`;
  - advance both positions and all links;
  - update pools.
- MMC never advances simulated time.
- Break check:
  - `CurrentSum = Σ accepted ΔE` in the sample window, converted to eV;
  - abort when `|LatticeEnergy * AbortTolerance| >= |CurrentSum|`;
  - `AbortSequenceLength`/`AbortSampleInterval` were unused in the original.
- Counters per start particle: skip, MCS, rejection, site blocking, unstable start, unstable end.
- Known quirks: no pre-run; no time; the abort buffer semantics are subtle; no energy trace in the state file
  by default.

## Design

### 1. Engine API

```rust
pub struct MmcEngine<'a> {
    ctx: &'a mut SimulationContext,
    energy: EnergyEngine<'a>,
    stats: StatisticsCollector,
    break_check: BreakCheck,
}

pub enum MmcOutcome {
    Accepted { delta_energy: f64 },
    Rejected,
    SiteBlocked,
}

impl MmcEngine<'_> {
    pub fn run(&mut self, cancel: &AtomicBool, progress: &mut dyn ProgressSink) -> Result<MmcSummary>;
    pub fn run_cycle(&mut self) -> MmcOutcome;
}
```

- `run_cycle` is deterministic and allocation-free.
- `run` handles blocks, break checks, aborts, statistics, and state saving.

### 2. Selection and path

- Draw a uniform direction from the selectable pool (same as KMC).
- Draw a uniform offset cell index over all supercell cells (`rng.next_bounded(cell_count)`).
- `p0` = selected position.
- `p1` = `trim(offset_cell, target_sublattice_from_direction)`.
- If `p0 == p1`, the exchange is a no-op; treat it as rejected (or skip, depending on the rule) and do not
  apply state changes.
- Build the two-site state code from the current particles.
- Rule lookup by `StateCode0`; none → site blocked.

### 3. Energy

- `s0 = E[p0][code0_0] + E[p1][code0_1]`.
- Compute `s2`:
  - If `p0` and `p1` are within the interaction range and linked in both directions, apply the deltas for both
    positions, read `s2`, then restore.
  - Otherwise, compute each position's energy after its own particle change independently (no cross-term).
- `delta = s2 - s0`.
- Accept if `delta <= 0` or `rng.next_f64() < exp(-delta)`.
- No electric field, no normalization, no S1.
- The energy engine's pair/cluster delta logic is shared with KMC.

### 4. Acceptance and detailed balance

- Metropolis criterion:
  `P_accept = min(1, exp(-(E_new - E_old)))`.
- Proposal symmetry: exchanging `A` at `p0` with `B` at `p1` must have the same proposal probability as the
  reverse exchange. The uniform direction + uniform offset scheme must be symmetric; tests verify it.
- For a two-state system with degeneracy `g`, the equilibrium occupation ratio must be
  `(g_new/g_old) * exp(-ΔE/kT)`. The rule/selection must account for degeneracy; if not, document and test the
  actual ratio.
- Use `>=`/`<` consistently for the acceptance boundary; document the choice.

### 5. State application

- On accept:
  - swap/assign particles at `p0` and `p1`;
  - update flags, tracker ids (MMC has no trackers; keep counters), and pool membership;
  - apply energy deltas to all linked neighbors;
  - update cluster codes;
  - update lattice energy by `delta`;
  - increment counters and append `delta` to the break-check window.
- Update order is fixed and deterministic.
- If `p0`/`p1` are the same or particles are identical, no state change; count as rejected.

### 6. Break check

- Goal: stop when the system has reached thermal equilibrium (or when the user-defined sample indicates it).
- Configuration:
  - `break_tolerance` (relative energy tolerance, 0 disables);
  - `break_sample_length_mcsp`: number of MCS in a sample window;
  - `break_sample_interval_mcsp`: number of MCS between checks;
  - `result_sample_mcsp`: how often to write an energy sample.
- Implementation:
  - maintain a ring buffer of accepted `ΔE` values for the current window;
  - at the end of a window, `sum = Σ ΔE` (eV);
  - if `|lattice_energy_ev * tolerance| >= |sum|`, declare equilibrium and stop;
  - otherwise start a new window.
- The original's exact check compared `|LatticeEnergy * AbortTolerance| >= |CurrentSum|`; preserve the
  inequality and document the sign convention.
- **Zero-energy guard:** if `|lattice_energy_ev|` is below a small floor (default `1e-12 eV`, configurable),
  skip the break check for that window and log a one-time warning. Otherwise a zero-energy lattice would
  declare equilibrium immediately. This is a deliberate, documented deviation from the original edge case;
  normal non-zero-energy behavior is unchanged.
- `break_sample_interval` controls how often the check runs; if zero, check every window.
- Emit an `EnergySample` (MCS, lattice energy, mean accepted ΔE) for the result state/plots.
- Break check must not allocate per cycle; use a fixed ring buffer sized at initialization.

### 7. Blocks, goals, and aborts

- Goal: `target_mcsp * mobile_count` accepted swaps (successful MCS), rounded to a multiple of 100.
- `cycles_per_block = max(1, total_goal / 100)`.
- Each block:
  - runs cycles;
  - saves state (unless `NoSaving`);
  - prints statistics;
  - checks aborts;
  - reports progress.
- Aborts:
  - MCSP target → completed;
  - time limit → timeout;
  - MCS rate < minimum → rate abort (define as MCS/s);
  - equilibrium break → completed with `EquilibriumReached`;
  - cancellation → cancelled;
  - numerical failure → error.
- MMC has no pre-run and no simulated time; `simulated_time` remains 0 in the state.
- The state file for MMC contains only the lattice and counters (no trackers/histograms).

### 8. Statistics

- Per-particle counters: attempts, accepted, rejected, blocked, start/end unstable (the latter two stay zero
  for MMC, but keep the same layout for compatibility with the result reader).
- Lattice energy, mean energy, acceptance rate, and energy samples.
- No conductivity/mobility/diffusion output (no time/field).
- Print the same `<STATISTICS_DUMP>` structure as KMC where fields apply, with `n/a` for KMC-only values.

### 9. Determinism

- Draw order per cycle:
  1. `next_bounded(selectable_count)` for direction/pool selection;
  2. `next_bounded(cell_count)` for the offset cell;
  3. optional `next_f64` for acceptance (only if `delta > 0`).
- No other RNG calls.
- Pool iteration and link order are deterministic.
- Same seed → identical trajectory/counters.

## Implementation tasks

- [ ] Implement selection and offset-cell path construction.
- [ ] Implement two-site energy calculation and shared delta logic.
- [ ] Implement Metropolis acceptance and state application.
- [ ] Implement the break-check window and energy samples.
- [ ] Implement blocks, goals, aborts, statistics, and state saving.
- [ ] Implement MMC-specific result-state layout.
- [ ] Add analytic detailed-balance and equilibrium tests.
- [ ] Add a per-cycle trace mode.

## Acceptance criteria

- [ ] A two-state system converges to the expected Boltzmann ratio.
- [ ] A small Ising-like lattice reaches the known ground state at low temperature.
- [ ] Detailed balance holds for random pair exchanges.
- [ ] The break check stops a converging system at the configured tolerance.
- [ ] A zero/near-zero lattice energy does not cause an immediate false equilibrium; the guard logs a warning.
- [ ] MMC state files contain lattice + counters only; no time/trackers.
- [ ] Same seed produces identical trajectories.
- [ ] No allocations in the cycle loop.
- [ ] Acceptance rate and energy trace are physically sensible across temperatures.
- [ ] Documentation written

## Documentation

At this point, document MMC:

- `docs/physics/mmc.md` — proposal/acceptance, detailed balance, equilibrium break, no time advance.
- `docs/user-guide/running.md` — MMC settings, break tolerance, energy trace.
- `docs/development/solver-architecture.md` — MMC engine and shared energy/state code.
- `docs/troubleshooting/errors.md` — non-convergence and break-check issues.

## Tests

| Test | Assertion |
| --- | --- |
| MMC property | Detailed balance holds across random two-state energy gaps and temperatures |
| Acceptance | A real ceria MMC run reaches the expected energy distribution on a small system |
| Boltzmann ratio | Two-state occupation vs. `exp(-ΔE/kT)` |
| Detailed balance | Forward/reverse transition rates |
| Ground state | Low-temperature convergence |
| Break check | Stops at tolerance; zero-energy guard prevents immediate abort; no false positives on a flat landscape |
| Energy conservation | Lattice energy equals full recomputation |
| State file | MMC layout and offsets |
| Determinism | Identical trace for a seed |
| Boundaries | Same position, identical particles, zero delta |

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Proposal asymmetry | Explicit forward/reverse rate tests |
| Break check too aggressive | Configurable tolerance/interval and tests |
| Energy drift | Incremental vs. full recomputation checks |
| Degeneracy mishandled | Document and test occupation ratios |
| MMC/KMC code divergence | Shared energy/state/pool code with separate engines |

## Done when

MMC samples the correct equilibrium distribution for analytic systems, detects equilibrium as configured, and
writes a valid time-independent state file.
