# Step 14 — Simulation Context Initialization

**Phase:** D. Solver  
**Depends on:** 13  
**Delivers:** A fully initialized simulation context: lattice, environment states, neighbor links, energy
lookup tables, selection pools, jump-status caches, counters, goals, and tracker state.

## Goal

Transform the validated package into the data structures the Monte Carlo loop needs, once, before the first
cycle. Initialization must be deterministic, allocation-bounded, and fast enough that short jobs are not
dominated by setup.

## Original reference

- `InitializeContextForSimulation`:
  - creates the environment lattice `[A][B][C][D]`, energy state arrays, cluster state arrays, abort buffer,
    state buffer, selection pools;
  - loads `run.mcs`/`prerun.mcs` for resume, otherwise copies the DB lattice and RNG state;
  - sets each environment's particle, mobility, stability, lattice vector, definition, tracker id;
  - computes `PrerunGoalMcs = PreRunMcsp * mobiles`, `TotalGoal = PrerunGoal + TargetMcsp * mobiles`, rounded
    to a multiple of 100; block sizes for ~1 % progress;
  - converts energy tables from eV to kT, pre-multiplies field factors, sets PCG state/increment;
  - computes `K_norm = K_fix / RawMax` (or `K_fix`), `Δt = dof * K_norm / (νmax * selectable)`.
- Environment linking:
  - tags constant tables;
  - allocates links to neighboring environments within the interaction range;
  - optionally links only into stable mobile targets;
  - builds cluster links for every cluster referencing a pair;
  - sorts links by target pair id and cluster links by `(cluster_id, code_byte)`.
- Jump status initialization (KMC):
  - for each cell and direction, rebuild the periodic-trimmed path;
  - collect sender/receiver links;
  - cap at 56 per path;
  - compute static S2 corrections where universally valid.
- Initial lattice energy: half the sum over stable sites of the current environment energy.
- Pool registration: direction count ≤ −1 = static/not selectable; 0 = mobile/passive; >0 = selectable if the
  selection mask includes the particle; selectable adds its directions to the pool.
- Known quirks: `1 << particleId` 32-bit undefined for IDs ≥32; mobile trackers include passive species;
  `RawMax` is not reset; `IsStable` is `particle != void`.

## Design

### 1. Context construction

```rust
pub struct SimulationContext {
    pub model: Arc<CompiledModel>,
    pub job: JobPackage,
    pub lattice: Lattice,
    pub cells: CellCounts,
    pub environments: EnvironmentArena,
    pub links: LinkArena,
    pub energy: EnergyRuntime,
    pub jumps: JumpRuntime,
    pub pool: SelectionPool,
    pub trackers: TrackerState,
    pub counters: CycleCounters,
    pub goals: Goals,
    pub rng: Rng,
    pub time_s: f64,
    pub lattice_energy_ev: f64,
    pub normalization: NormalizationState,
    pub flags: RuntimeFlags,
}

impl SimulationContext {
    pub fn initialize(job: JobPackage, options: &Options) -> Result<Self>;
}
```

- Allocate all hot arrays once; no growth during the run.
- Use `u32`/`u8` indices; validate at load.
- Keep unit-cell position count `P` and supercell dimensions `A,B,C` explicit.

### 2. Lattice and environment states

- Lattice: `Vec<ParticleId>` length `A*B*C*P`, with `index(a,b,c,p) = p + c*P + b*P*C + a*P*C*B`.
- Environment states (per lattice position):
  - current particle;
  - mobile flag (can participate in a transition);
  - stable flag (particle != void and position stable);
  - mobile tracker id (`Option<u32>`);
  - `Vector4I` lattice vector;
  - reference to the unit-cell environment definition.
- Use a struct-of-arrays layout for hot fields:
  - `particles: Vec<ParticleId>`,
  - `flags: Vec<EnvFlags>` (bitflags for stable/mobile/tracker),
  - `energy: Vec<f64>` with per-environment stride,
  - `cluster_codes: Vec<u64>` with per-environment offsets.
- The unit-cell environment definition is shared; per-cell state is only the particle and derived flags.
- Mobile tracker ids are assigned sequentially to positions whose particle can move (jump count ≥ 0 and
  particle != void), including passive 0-direction species. The mapping is stable for the run.

### 3. Energy runtime

- Convert pair and cluster tables from eV to kT once at initialization:
  `factor = 1 / (k_B_eV * T)`.
- Store pair tables as flat `Vec<f64>` with strides `[center_particle][partner_particle]`.
- Store cluster tables as:
  - sorted `Vec<u64>` occupation codes;
  - flat `Vec<f64>` energies;
  - `particle_table_mapping: [u8; 64]` from center particle to row.
- Site energy background is a unit-cell table `[position][particle]` in eV, converted to kT once. It is
  indexed by `position_id` (P index), not replicated per cell; no per-cell background array is allocated.
- Compute each environment's energy vector for all allowed center particles:
  - start with site energy;
  - add pair contributions from linked neighbors;
  - add cluster contributions for clusters centered on this position.
- Compute each environment's cluster code ids from the current neighbor occupations.
- Initial lattice energy = `0.5 * sum(stable environments) energy[current_particle]` in eV.
- The 0.5 factor corrects pair double counting. Cluster contributions follow the documented per-center
  convention; tests assert the convention.

### 4. Neighbor links

- For each unit-cell position and each periodic image within the interaction range:
  - create a link to the target environment;
  - record the pair interaction id;
  - record cluster links `(cluster_id, code_byte)` for clusters that reference this pair.
- Build links as CSR:
  - `link_offsets: Vec<u32>` length `P+1`;
  - `links: Vec<Link>` with `target_position_offset`, `pair_id`, `cluster_start`, `cluster_len`;
  - `cluster_links: Vec<ClusterLink>`.
- Sort deterministically by target pair id, then cluster id, then code byte.
- Optimization: optionally skip links into immobile targets for MMC, matching the original's
  `OPT_LINK_ONLY_MOBILES`. The cache is rebuilt before saving if the lattice changes mobility.
- Constant tables (all entries equal) can be skipped, but only after a correctness test; make it a runtime
  option, not a default until validated.

### 5. Selection pool

- For each lattice position and particle, the compiled model provides a direction count:
  - `-1` / absent: static, not selectable;
  - `0`: mobile but no selectable directions (passive);
  - `>0`: selectable if the simulation's selection mask includes the particle.
- A `DirectionPool` groups all selectable `(position, particle)` combinations with the same direction count.
- `SelectableJumpCount` is the sum of directions across selectable combinations.
- Pool entries store `(position_index, relative_direction_count)`.
- Removal/addition on particle change uses swap-remove and updates the total.
- The pool is deterministic: positions are inserted in ascending linear index order; particle changes use
  swap-remove, which changes order but is deterministic given the trajectory.
- Selection optimizers remove specific `(position, particle)` combinations before the run.

### 6. Jump status (KMC)

- For each jump direction, compute the periodic-trimmed path of environment positions.
- For each path, collect sender/receiver links:
  - sender = path node whose particle changes;
  - receiver = linked neighbor whose energy changes.
- Store as CSR `[cell][direction] -> links`, with a documented cap (original 56). If a path exceeds the cap,
  fall back to dynamic computation for that direction and log a warning.
- Static S2 correction:
  - If the energy change at every receiver is independent of the sender's surrounding state, precompute the
    correction and mark the rule static.
  - Otherwise mark it dynamic and compute S2 by applying deltas at runtime.
  - Start with dynamic-only in step 15; add static corrections after correctness is proven.
- If all rules are static, the entire cache can be dropped to save memory.

### 7. Goals and counters

- `mobile_count = number of positions with a potentially mobile particle` (original definition).
- `prerun_goal = pre_run_mcsp * mobile_count` (KMC).
- `main_goal = target_mcsp * mobile_count`.
- `total_goal = prerun_goal + main_goal`, rounded up to a multiple of 100.
- `cycles_per_block = max(1, total_goal / 100)`; the run reports ~1 % progress per block.
- Counters per particle: attempts/success/skip/rejected/blocked/start-unstable/end-unstable.
- Counters per tracker: same plus movement.
- `cycles` counts all attempts; `mcs` counts accepted state advances.
- Time starts at 0; resume restores `simulated_time`, RNG state, normalization, and counters.

### 8. Normalization and time step

- KMC normalization:
  - `K_fix = exp(-normalization_energy / kT)^-1` when a normalization energy is set, else 1.
  - `K_auto = 1 / RawMax` during the pre-run; `RawMax` starts at 0.
  - `K_norm = K_fix * K_auto`.
  - If `K_norm` is not finite, use `K_fix`.
- Time step:
  - `dof = 2` if the simulation flag `DualDof` is set, else 1.
  - `dt = dof * K_norm / (nu_max * selectable_jump_count)`.
  - Recompute only when `K_norm` or `selectable_jump_count` changes.
- MMC has no normalization and no simulated time.

### 9. Resume

- If `run.mcs` or `prerun.mcs` exists, load the lattice, counters, trackers, RNG state, simulated time,
  normalization/RawMax, and flags.
- Validate the state against the job/model signature and lattice size; refuse a mismatched state.
- Resume preserves `RawMax`; never reset it on resume.
- A completed state exits with a distinct "already completed" result (not a silent success).

## Implementation tasks

- [ ] Implement `Lattice` and environment state arenas.
- [ ] Implement unit-cell energy/cluster state initialization.
- [ ] Implement neighbor link CSR and cluster links.
- [ ] Implement the selection pool and optimizer application.
- [ ] Implement jump-status CSR with cap/fallback.
- [ ] Implement goals, counters, and block sizing.
- [ ] Implement normalization/time-step initialization.
- [ ] Implement resume-state loading hooks (writer in step 18).
- [ ] Add context-init benchmarks and memory reports.

## Acceptance criteria

- [ ] Ceria context initialization produces the expected mobile count, selectable count, and block sizes.
- [ ] Initial lattice energy matches a hand-computed small system.
- [ ] Environment energy vectors match a brute-force neighbor sum.
- [ ] Links and cluster links are sorted deterministically.
- [ ] Selection pools exclude optimized combinations and sum correctly.
- [ ] A context initializes in < 100 ms for a 10×10×10 ceria job.
- [ ] Peak memory is reported and within budget.
- [ ] No allocation occurs in the per-cycle loop (verified by a counting allocator).

## Tests

| Test | Assertion |
| --- | --- |
| Lattice indexing | Round-trip `(a,b,c,p)` ↔ linear index |
| Initial energy | Hand-computed pair/cluster/site system |
| Links | Neighbor set matches brute force |
| Pool | Counts, optimizer removal, swap-remove consistency |
| Jump status | Paths/links match a naive recomputation |
| Goals | Prerun/main/block arithmetic |
| Normalization | Fix/auto combinations |
| Resume | State restores exactly; mismatched state rejected |
| Allocation | No allocations in a simulated cycle stub |

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Link graph memory explosion | CSR, bounded interaction range, profiling, optional mobile-only links |
| Jump-status cache size | Per-direction lazy fallback, cap, static-rule pruning |
| Energy convention errors | Hand-computed tests; document pair/cluster counting |
| Pool update bugs | Invariant checks in debug builds; deterministic tests |
| Resume incompatibility | Signature/lattice checks and clear errors |

## Done when

A job package becomes a fully initialized context with correct lattice, energies, links, pools, jump status,
goals, counters, RNG, and normalization, ready for the first Monte Carlo cycle.
