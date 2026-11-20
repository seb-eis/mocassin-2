# Step 14 — Simulation Context Initialization

**Phase:** D. Solver
**Depends on:** 13
**Delivers:** A deterministic, allocation-bounded simulation context built once per job from the validated
`JobPackage`: lattice and environment arenas, a compact neighbour-link CSR, the cluster runtime, the energy
background and environment energy vectors, the selection pools, the KMC jump status, the mobile/static tracker
index space, counters, goals, normalization, the time step, and the resume hook. No energy deltas, no KMC/MMC
cycle, no tracker values, and no `.mcs` output.

## Implementation status

- **Status:** Design frozen — `Ready for review`
- **Step branch:** — (not created; the step branch is created from the reviewed `main` commit after the start gate)
- **Step CI:** —
- **Main merge:** —
- **Step tag:** —
- **Audit:** Not requested
- **Design review:** `Ready for review` at the freeze commit recorded in
  [`docs/planning/reviews/step-14.md`](../reviews/step-14.md)
- **Sizing:** Complexity class **High**; owner acceptance as one High unit is requested at the start gate (no
  split proposed). See §4.
- **Last updated:** 2026-11-20 (design freeze)

| Feature | Branch | Status | Local gates | Step CI | Squash commit |
| --- | --- | --- | --- | --- | --- |
| 1 Context skeleton, arenas, limits, errors | `feature/step-14-simulation-context-initialization/context-arenas-and-limits` | Planned | — | — | — |
| 2 Neighbour-link CSR and cluster runtime | `feature/step-14-simulation-context-initialization/link-csr-and-clusters` | Planned | — | — | — |
| 3 Energy runtime and initial lattice energy | `feature/step-14-simulation-context-initialization/energy-runtime-and-initial-energy` | Planned | — | — | — |
| 4 Selection pools and jump status | `feature/step-14-simulation-context-initialization/selection-pools-and-jump-status` | Planned | — | — | — |
| 5 Goals, counters, normalization, time step, resume | `feature/step-14-simulation-context-initialization/goals-normalization-and-resume` | Planned | — | — | — |
| 6 Fixtures, benchmarks, documentation, and closeout | `feature/step-14-simulation-context-initialization/fixtures-benchmarks-and-closeout` | Planned | — | — | — |

### Feature list (scope and outcomes)

| # | Feature | Branch | Scope | Outcome |
| --- | --- | --- | --- | --- |
| 1 | Context skeleton, arenas, limits, errors | `feature/step-14-simulation-context-initialization/context-arenas-and-limits` | Module layout; `ContextOptions`/`ContextLimits`/`ContextError`/`MemoryReport`/`Dimensions`; lattice/flag/mobile-tracker arenas; index/wrap helpers; phase pipeline and cancellation; error/exit mapping; invariant sweep | A fresh ceria `JobPackage` yields a context with the correct dimensions, lattice, flags, empty links/clusters/pools/jump status, zeroed counters, and a `MemoryReport`; every limit and the cancel flag return the documented error |
| 2 | Neighbour-link CSR and cluster runtime | `feature/step-14-simulation-context-initialization/link-csr-and-clusters` | Compact per-P1 link CSR and absolute-target resolution; deterministic sort; per-link cluster association; cluster instance/membership validation; the unique-image geometry rule; codes/code ids | Links equal a brute-force payload expansion for every cell; the populated fixture's cluster resolves; a zero/multiple-image cluster fails closed; a missing code fails at init |
| 3 | Energy runtime and initial lattice energy | `feature/step-14-simulation-context-initialization/energy-runtime-and-initial-energy` | eV→kT factor; site table; site+pair vectors; per-instance cluster energy; exact initial total; debug recomputation helper; `field_scale` | The hand-computed site/pair/cluster total matches; the vectors equal a brute-force sum; `step-13-populated` initializes with the documented energies |
| 4 | Selection pools and jump status | `feature/step-14-simulation-context-initialization/selection-pools-and-jump-status` | Direction-count pools; mask/optimizer application; `selectable_jump_count`; O(1) slot maps and update helpers; per-`(P1, direction)` jump status with the structural filter, derived bound, cluster touches, and `resolve_node` | Pool counts/removal/swap-remove are correct; jump links equal a naive per-cell recomputation; a cross-bucket direction resolves at the selected bucket; `step-13-centered` passes |
| 5 | Goals, counters, normalization, time step, resume | `feature/step-14-simulation-context-initialization/goals-normalization-and-resume` | Mobile/static tracker index space; goals/block size; counters; `K_fix`/`K_auto`/`K_norm`/`dof`/`nu_max`/`dt`; `ResumeState`/`ResumePhase` validation, re-derived mapping, `AlreadyCompleted` | Goal/normalization arithmetic matches the hand values; resume restores exactly and rejects mismatches; the MMC context has no pools/jump status/time |
| 6 | Fixtures, benchmarks, documentation, and closeout | `feature/step-14-simulation-context-initialization/fixtures-benchmarks-and-closeout` | New 10×10×10 real-writer fixture and .NET rebuild test; context-summary snapshots; allocation test; `context` Criterion benchmark and `rustContext` baseline; docs/parity/status/closeout | `< 100 ms`/`< 20 ms` and memory budgets measured on the recorded runner class; zero-allocation proof; docs build strictly; closeout evidence passes its checker; audit request complete |

## Goal

Turn one validated `JobPackage` into the exact data structures the Monte Carlo loops need, once, before the
first cycle. The context must be **deterministic** (same package + same binary + same platform ⇒ identical
structured state), **allocation-bounded** (every hot array sized once; no growth during a run), **fast**
(short jobs are not dominated by setup: `< 100 ms` for the 10×10×10 acceptance fixture), and **fail-closed**
(a package the context cannot represent exactly produces a structured error, never a silently wrong neighbour
map, cluster code, or energy vector).

Step 14 ends before any physics. It builds inputs; Steps 15–18 consume them.

## Original reference

Read `/workspace/mocassin-original` as a read-only reference. Do not copy GPL code or data.

- `InitializeContextForSimulation` (`SimulationContextInitialization.c`):
  - allocates the environment lattice `[A][B][C][D]`, the per-environment energy and cluster state buffers,
    the abort/state buffers, and the selection pools;
  - loads `run.mcs`/`prerun.mcs` for resume, otherwise copies the DB lattice and the RNG state
    (`CopyDefaultMobileTrackersToMainState`, `SyncDynamicModelToMainState`);
  - sets each environment's particle, mobility, stability, lattice vector, definition, and tracker id;
  - computes `PrerunGoalMcs = PreRunMcsp * mobileCount`,
    `TotalGoal = PrerunGoalMcs + TargetMcsp * mobileCount`, rounds `TotalGoal` **up** to a multiple of 100,
    and sets `McsCountPerExecutionPhase = TotalGoal / 100` (`SetCycleCounterStateToDefault` in
    `StatisticsRoutines.c`, which returns `ERR_NOMOBILES` for a zero mobile count);
  - converts energies from eV to kT (`1 / (T * k_B)`) and computes
    `K_auto = 1 / RawMax`, `K_norm = K_fix * K_auto` (or `K_fix` when not finite), and
    `dt = dof * K_norm / (nu_max * selectableCount)` with `dof = 2` for the dual-DOF flag
    (`GetCurrentTimeStepPerJumpAttempt`, `GetTotalJumpNormalization`).
- Environment linking (`EnvironmentRoutines.c`): each environment gets an `EnvironmentLink` per pair image with
  its target environment, the target pair index, and the cluster links that reference that pair; links are
  sorted by target pair id, cluster links by `(cluster_id, code_byte)`.
- Jump status (`JumpStatusInititialization.c`): for every `(cell, direction)` the periodic-trimmed path is
  rebuilt from the direction's relative `JumpSequence`; for every ordered stable path-node pair
  `(sender, receiver)` with a sender link to the receiver, a `JumpLink{sender_path_id, link_id}` is stored;
  the buffer is `JUMPS_JUMPLINK_LIMIT = JUMPS_JUMPLENGTH_MAX * (JUMPS_JUMPLENGTH_MAX - 1) = 56` entries with no
  bounds check (a real overflow for a 9th path node, which the original cannot encode anyway).
  `TryFindConstVirtualJumpCorrection` derives a static S2 correction only when the affected links carry no
  cluster link; otherwise the rule stays dynamic.
- Initial lattice energy: `0.5 * Σ_stable E[env][current]`, where `E` already contains the site background,
  the pair sums, and (for a member environment) the cluster term (`ResynchronizeEnvironmentEnergyStatus`).
- Pool registration: `jumpCount` is `-1` = static/not selectable, `0` = mobile passive, `>0` = selectable when
  the environment's selection mask includes the particle; a selectable combination adds its directions to the
  pool. Pools are walked in ascending direction-count order.
- Known quirks: `1 << particleId` is a 32-bit shift (undefined for ids ≥ 32); mobile trackers include passive
  species; `RawMax` is not reset on resume; `IsStable` is `particle != void` (the compiled site stability flag
  is ignored); the 0.5 factor is applied to the site and cluster terms as well as the pair terms.

Preserved intents: build once, sort deterministically, cache the KMC jump links, derive the goals/normalization
from the mobile count, and resume exactly.

Corrected in this step (each with a test and a recorded deviation, §14): the link graph is translation-compact;
cluster energy is counted once at its centre; the site background is counted once; the jump-status receiver
filter is structural rather than initial-occupation-based; and the jump-status bound is derived, not a magic 56.

## Scope

**In scope**

- The `mocassin-solver` context module: `SimulationContext`, `ContextOptions`, `ContextLimits`,
  `ContextError`, `InitOutcome`, and the accessors Steps 15–18 consume.
- Lattice and environment state arenas (particles, flags, mobile tracker ids) and the index/wrap helpers.
- The compact per-P1 neighbour-link CSR, absolute-target resolution, deterministic sorting, and the per-link
  cluster association.
- The cluster runtime: instance/membership validation, occupation codes, code ids, per-instance energies, and
  the v1 cluster-geometry rule (§6.4).
- The energy runtime: the eV→kT factor, the site table, the per-environment site+pair energy vectors, the
  per-instance cluster energy vectors, and the exact initial lattice energy.
- Selection pools (direction-count buckets), selection-mask filtering, optimizer application, the selectable
  count, and the deterministic add/remove helpers.
- KMC jump status: per-`(P1 position, direction)` sender/receiver links and cluster touches, the derived bound,
  and the path-resolution helper for Step 16's exact occupation precheck.
- Mobile/static tracker **index space** and the mobile-position mapping (values are Step 18).
- Counters, goals, block size, `K_fix`/`K_auto`/`K_norm`, `dof`, `nu_max`, `dt`, and the effective RNG.
- The resume hook: `ResumeState`/`ResumePhase`, validation, and the deterministic re-derivation of the mobile
  mapping; the `.mcs` decode itself is Step 18.
- Bounds, cancellation, checked arithmetic, invariant checks, and a `MemoryReport`.
- The real-writer 10×10×10 acceptance fixture, the context-summary snapshots, the allocation test, and the
  Criterion `context` benchmark with a same-runner-class baseline entry.
- Documentation, parity, open-decision records, and closeout evidence.

**Non-goals (explicitly out of scope)**

- **No energy engine.** No S0/S1/S2, no pair/cluster delta propagation, no state application, no field term
  computation, no incremental lattice-energy update, no fast-exp, no static S2 corrections. Step 15 owns all of
  it; Step 14 supplies the tables, codes, vectors, and conventions.
- **No KMC or MMC loop.** No selection draw, no acceptance, no time advance, no pre-run, no abort condition, no
  block loop, no `run_cycle`. Steps 16/17 own them.
- **No trackers, histograms, or statistics.** Step 14 allocates the index space and the mobile-position mapping
  only; tracker values, `tracker_order` following, jump histograms, energy samples, and `.mcs` writing are
  Step 18. Mutable counters are allocated and zeroed; Step 16/17 increment them and Step 18 writes them.
- **No `.mcs` reader/writer.** `mocassin-state` stays a placeholder. Step 14 defines the plain `ResumeState`
  input and refuses a mismatched state; Step 18 writes the document and Step 19 wires the CLI to it.
- **No CLI surface change.** `mocassin-sim` keeps the Step 13 options and still rejects a non-dry-run invocation
  until Step 16. `--threads` stays validated-but-unused; Step 14 initialization is single-threaded.
- **No package, payload, format, schema, manifest, or signature change.** No `contracts/manifest.json` edit, no
  payload version bump, no new `.msl` field. The cluster-geometry limitation (§6.4) is a v1 limitation, not a
  format change; the fix is a future `.msl` revision recorded as an open decision.
- **No new external crate.** Step 14 uses the existing workspace dependencies (`mocassin-types`,
  `mocassin-rng`, `thiserror`, `tracing`; dev-only `criterion`, `proptest`, `insta`, and `mocassin-msl` for
  fixtures). `rayon` stays unused in `mocassin-solver`; parallel initialization is a Step 29 option.
- **No `.msl` writing and no project JSON.** Rust still reads only the package.
- **No change to ADR 0005, ADR 0012, or any scientific constant.** The site/pair/cluster accounting in §7 is a
  documented interpretation of the original's ambiguous 0.5 factor, recorded with tests; it is not a new
  ADR-level decision.

## 1. Context construction and ownership

```rust
pub struct SimulationContext {
    pub job: JobPackage,               // owns the shared Arc<ParameterizedModel>
    pub kind: SimulationKind,
    pub dimensions: Dimensions,        // [A,B,C] and P, with checked strides
    pub lattice: LatticeState,
    pub links: LinkArena,              // compact per-P1 CSR
    pub clusters: ClusterArena,        // instances, memberships, codes, energies
    pub energy: EnergyRuntime,         // site table, site+pair vectors, kT factor, field scale
    pub pools: SelectionPools,         // KMC only; empty for MMC
    pub jumps: JumpRuntime,            // KMC only; empty for MMC
    pub trackers: TrackerIndex,        // index space + mobile mapping; no values
    pub counters: Counters,            // zeroed; Step 16/17 increment
    pub goals: Goals,
    pub normalization: Normalization,
    pub rng: Rng,
    pub time_s: f64,                   // 0 fresh; restored on resume; 0 for MMC
    pub lattice_energy_ev: f64,        // exact initial total (§7.4)
    pub origin: ContextOrigin,
    pub limits: ContextLimits,
}

impl SimulationContext {
    pub fn initialize(
        job: JobPackage,
        options: &ContextOptions,
        cancel: Option<&AtomicBool>,
    ) -> Result<InitOutcome, ContextError>;

    /// Deterministic accessors consumed by Steps 15–18 (§15).
    pub fn memory_report(&self) -> MemoryReport;
}

pub enum InitOutcome {
    Ready(Box<SimulationContext>),
    /// The resume state carries `ResumePhase::Completed`. The run orchestration (Step 16/19) maps this to the
    /// distinct "already completed" outcome; Step 14 never resumes it silently.
    AlreadyCompleted { mcs: u64, total_goal: u64 },
}

pub struct ContextOptions {
    pub limits: ContextLimits,
    /// Build the KMC jump status (§9). Defaults to `job.kind == Kmc`; MMC never builds it.
    pub build_jump_status: bool,
    /// Apply the job's selection optimizers after pool construction. Defaults to `true`.
    pub apply_optimizers: bool,
    /// Resume input. `None` builds a fresh context.
    pub resume: Option<ResumeState>,
}
```

- One context per job; no interior mutability, no `HashMap` in a hot structure, no `Arc` inside the arenas.
- `initialize` is a pure function of `JobPackage` + options + resume state (plus cancellation). It performs no
  I/O, opens no package, and writes nothing.
- Construction is phased and each phase is validated before the next: input validation → lattice → links →
  clusters → energy vectors → initial energy → pools → jump status → tracker index → goals/counters/
  normalization → RNG/time → invariant sweep. A failed phase returns a structured error naming the phase and
  the offending index; no partial context escapes.
- `Box<SimulationContext>` keeps the large struct out of the enum and off the stack; the type is `Send` but not
  `Sync` (the RNG is `&mut`-only and the context is single-threaded per trajectory).

## 2. Data layout, dimensions, and bounds

- `Dimensions { a: u32, b: u32, c: u32, p: u32, pc: u32 }` with the documented linear index
  `idx(a, b, c, q) = q + c*P + b*P*C + a*P*C*B` (`P`/`C`/`B` are the counts; identical to `InitialLattice`;
  `A*B*C*P ≤ i32::MAX`, re-checked in checked `u64`).
- `cell(env) = env / P`, `position(env) = env % P`, `index(cell, pos) = cell*P + pos`; `wrap(cell + offset)`
  uses `rem_euclid` per component.
- Environment state is struct-of-arrays over absolute environments, length `A*B*C*P`:
  - `particles: Vec<ParticleId>` (1 byte each; equal to the lattice after initialization);
  - `flags: Vec<u8>` with `STABLE = 1`, `MOBILE = 2`, `HAS_MOBILE_TRACKER = 4`; `flags[env]` is derived once
    per state change by the Step 15 helper, never recomputed by a full scan;
  - `mobile_tracker: Vec<u32>` (`u32::MAX` = none).
- Energy is `Vec<f64>` in kT with the explicit stride `pc`: `energy[env * pc + particle]`. No `Vec<Vec<f64>>`,
  no per-environment heap allocation.
- `u32` offsets, `u8` ids, `u64` masks, `f64` energies. No `f32`. Particle masks are never shifted on a 32-bit
  integer.
- `ContextLimits` (public, overridable library limits; defaults below) is validated **before** allocation:

| Limit | Default | Reason |
| --- | ---: | --- |
| `max_positions` | 500,000 | Same envelope as `ReaderLimits::max_positions`; `A*B*C*P` and `≤ i32::MAX` re-checked |
| `max_particles` | 64 | 6-bit state codes and `u64` masks |
| `max_pair_links` | 16,000,000 | Same envelope as the reader's total pair-link cap |
| `max_links_per_position` | 65,536 | One P1 position's link vector |
| `max_cluster_tables` | 1,000,000 | Reader envelope |
| `max_cluster_links_per_position` | 4,096 | Reader envelope |
| `max_selectable_jump_count` | `u32::MAX` | `Rng::next_bounded(u32)`; a larger count is a context limit error |
| `max_energy_vector_bytes` | 536,870,912 (512 MiB) | `A*B*C*P*pc*8`; rejects a context the budget cannot hold |
| `max_jump_status_bytes` | 268,435,456 (256 MiB) | `P*D*(L*(L-1))` links plus cluster touches |
| `max_path_nodes` | 10 | Same as `StateCode::MAX_NODES` |

- All size arithmetic is checked; an overflow is `ContextError::LimitExceeded`, never a panic or a wrapped
  allocation. A limit violation is a **model/data error** (exit 12): the package is structurally valid but the
  job cannot be represented within the configured context limits.
- Memory is reported, not guessed: `MemoryReport` returns per-array byte counts (lattice, flags, tracker ids,
  energy vectors, cluster arrays, links, pools, jump status, counters) and a total; the acceptance test asserts
  the total against the budget and the benchmark records the measured process peak RSS.

## 3. Lattice and environment states

- The particle array is `InitialLattice.particles` copied verbatim (already validated length, dimensions,
  particle range, and lattice signature by Step 13). Step 14 does not re-verify the signature.
- `STABLE` = `model.structure.positions[P].stable && particle != void`. The original uses `particle != void`
  only; the new rule additionally requires the compiled site stability. All v1 P1 positions carry the compiled
  flag; the difference is recorded in §14 and covered by a test with a synthetic position whose compiled
  stability is false.
- `MOBILE` = `particle != void && jump_count[P*pc + particle] >= 0`. Because `jump_count` is `u32`, the original's
  `-1` (static) and `0` (passive) collapse to zero; the distinction is not representable in the v1 payload and
  is not needed (the mobile-tracker rule is `>= 0`). Recorded in §14.
- The environment's `selection_particle_mask` and the position's `allowed_mask` are kept distinct: the mask
  governs pool membership (§8), the position mask governs Step 16's exact occupation check (§9.4). A package
  where they disagree is accepted as authored; the reader already bounded both.
- `update_particle_ids` is stored per position (CSR over P1 positions) and exposed to Step 15 for delta
  propagation.
- Site stability, the interaction range `[maxCellA, maxCellB, maxCellC, 0]`, the cell vectors, the particle
  charges/symbols/vacancy flags, and the tracker counts are exposed read-only from the model.

## 4. Complexity class and split decision

**Class: High.** Recorded per [`04-roadmap.md` §3.1](../04-roadmap.md#31-step-sizing-and-split-triggers) and
mirrored in the roadmap's "Recorded class — Step 14" note.

- **Contracts and public surfaces:** no persisted format change, but a new public Rust API
  (`SimulationContext`, `ContextOptions`, `ContextLimits`, `ContextError`, `ResumeState`) that Steps 15, 16, 17,
  18, and 19 consume. A late change ripples into five later steps.
- **New dependencies:** none.
- **Cross-step interfaces:** five later steps, and the interface fixes the neighbour-link identity, the energy
  vector layout, the cluster-code representation, the pool order, and the jump-status link identity. This is the
  dimension that makes the step High.
- **Irreversibility:** the link/target resolution rule, the pool ordering, the cluster-geometry rule
  (§6.4), and the site/pair/cluster accounting (§7.4) are scientific/behavioural conventions that are expensive
  to change after merge (fixtures, tests, Step 15's deltas, parity expectations).
- **Test-matrix breadth:** unit, property, snapshot, real-package acceptance, allocation, benchmark, and
  memory layers; several failure modes (zero mobile, zero selectable, cluster geometry, resume mismatch,
  cancellation, limits).
- **Independent acceptance stories:** one — "a validated job becomes a fully initialized context". The energy,
  pool, jump-status, and goal criteria are parts of that one story, not separable deliverables.
- **Evidence size:** bounded for one audit sitting: six features, one documented design page, one new fixture
  pair, one benchmark, and the reconciled reference pages.

**Split decision: no split.** The only dependency-clean boundary would separate the context skeleton from the
acceptance that proves it (links/energies/pools/jump status), leaving Step 15 with a half-context or duplicating
the real-package fixtures and the memory/benchmark evidence in both halves. That is the same failure the Step 13
13A/13B split avoided. A second boundary — "energy runtime" versus "selection/jump status" — would leave either
a context without selectable jumps or a pool without energies, and both halves would still be High on
cross-step interfaces. The step is therefore proposed as **one High unit** and requires the owner's explicit
acceptance at the start gate; the request and the acceptance case are recorded in §3.1 of the roadmap and in
[`05-open-decisions.md`](../05-open-decisions.md).

**Guard (not a time estimate):** the context API is frozen at feature 3. If the real-package acceptance
(feature 3/4) cannot pass without changing the accessor surface, the step agent stops and reports before
feature 5; a silent API rework that invalidates the frozen interfaces is a re-review trigger.

## 5. Design — context construction

### 5.1 Phases and determinism

```text
1 validate inputs (dimensions, arrays, masks, links, tables, settings)
2 build lattice/environment states + tracker index space
3 build the compact per-P link CSR (sort, resolve, cluster association)
4 validate cluster geometry, build instances/memberships, initial codes
5 build the site table and per-environment site+pair vectors
6 build per-instance cluster codes and energy vectors
7 compute the exact initial lattice energy
8 build selection pools (KMC) and apply optimizers
9 build jump status (KMC): pair links + cluster touches per (P, direction)
10 counters, goals, normalization, nu_max, dt
11 RNG from the effective seed, time (0/fresh or restored)
12 invariant sweep (debug assertions + release-checkable checks); publish
```

- Every loop iterates ascending dense indices; no hash iteration; no `rayon`; no time or OS entropy.
- Floating-point sums use a fixed order (the sorted link order, ascending particle index) so the energy vectors
  are reproducible on one platform/compiler. Cross-platform bit equality is not a gate (ADR 0005).
- Cancellation is checked at each phase boundary and every 4096 positions/statuses; a cancelled initialization
  returns `ContextError::Cancelled` and drops the partial context.

## 6. Neighbour links and the cluster runtime

### 6.1 Compact per-P1 link CSR

The payload stores pair links per canonical P1 position, one entry per periodic image:
`{target_position, offset:[i32;3], pair_id}`. The same list applies to every supercell cell of that P1
position; only the absolute target changes. Step 14 therefore builds a **compact CSR over P1 positions**, not
over all `A*B*C*P` environments:

```rust
pub struct LinkArena {
    pub offsets: Vec<u32>,          // length P + 1
    pub links: Vec<Link>,           // grouped by centre P1 position
    pub cluster_assoc: Vec<ClusterAssoc>, // referenced by Link::cluster_start/len
}
pub struct Link {
    pub target_position: u32,       // < P
    pub offset: [i32; 3],           // periodic image offset, immutable per P1 link
    pub pair_id: u32,               // shared pair-table index
    pub cluster_start: u32,         // into cluster_assoc
    pub cluster_len: u32,
}
pub struct ClusterAssoc { pub cluster_table: u32, pub slot: u8 } // clusters referencing this link
```

- Absolute target: `wrap(cell_of(centre) + link.offset)` position `link.target_position`, i.e.
  `target = wrap(cell + offset) * P + target_position`. This is the only place the periodic image is resolved;
  Step 15 never reconstructs it from the target index.
- Sorting is deterministic and is part of the reproducibility contract (it fixes the f64 summation order):
  `pair_id` ascending, then `target_position`, then `offset` lexicographically. The original sorted by pair id
  only with an unstable sort; the added tie-breakers change no physics, only rounding, and are recorded in §14.
- `cluster_assoc` answers "which clusters reference this pair" for Step 15's delta routing, in the compact
  per-P form (the absolute receiver is resolved exactly like `Link.target_position`).
- The memory win is the reason for the design: the reader's 16M total pair-link cap bounds the P1 list
  directly, so the compact CSR holds at most the payload's own links. The original allocated that list once per
  supercell cell, multiplying the link memory by `A·B·C`; the memory report states both the formula and the
  measured total.
- Validation: `target_position < P`, `pair_id < pair_table_count`, `|offset[i]| ≤ interaction_range[i] + 1`
  (reader-checked), the wrapped target index `< A*B*C*P`, and the per-P count ≤ `max_links_per_position`.
- The reader's `max_pair_links` cap applies to the payload's P1 list (the same number), so the compact CSR never
  exceeds the package's own link count.
- **No mobile-only link optimisation in v1.** The original's `OPT_LINK_ONLY_MOBILES` changes S2 when mobility
  changes; it is deferred to Step 29 behind a measured option. Recorded in §14.

### 6.2 Cluster links and the centre

- A cluster table's slot `0` is the centre; the payload's per-environment cluster link is
  `{cluster_id, byte_position}` where `byte_position` is **this environment's own slot** in the cluster
  (confirmed against `CompiledClusterLink(ClusterTableIndex, SiteSlot)` and the real populated fixture).
- Step 14 validates, per cluster table `t`:
  1. the table is referenced by at least one environment;
  2. slot `0` exists on exactly one P1 position (the centre position);
  3. every slot `0..=n` exists on exactly one P1 position, and the positions are distinct;
  4. the slot set is contiguous (no gap) and the slot count is at most 9 (the .NET
     `ClusterReduction.MaxClusterSites`; a 6-bit state code holds up to 10 and the reader bounds
     `byte_position < 10`);
  5. the links that reference `t` carry each slot exactly once (the multiset of `byte_position` is
     `0..=n`).
  A violation is `ContextError::ClusterGeometry { cluster_table, detail }` (exit 12).
- The **centre's** energy vector holds the cluster term (§7.3). Non-centre members hold a membership record so a
  particle change can route the delta to the centre; they do not hold a cluster energy.

### 6.3 Cluster instance and code representation

```rust
pub struct ClusterArena {
    pub instances_per_cell: Vec<ClusterInstance>,   // one per cluster table
    pub memberships: Vec<u32>,                      // CSR over P1 positions
    pub membership: Vec<Membership>,
    pub codes: Vec<u64>,                            // [instance * A*B*C + cell] current base code
    pub code_ids: Vec<u32>,                         // index into the table's sorted codes (u32::MAX = absent)
    pub energy: Vec<f64>,                          // [instance * A*B*C + cell] * pc, kT
}
pub struct ClusterInstance {
    pub table: u32,
    pub centre_position: u32,
    pub slot_positions: Vec<u32>,                   // P1 position per slot, slot 0 = centre
    pub slot_offsets: Vec<[i32; 3]>,                // relative cell offset per slot
    pub slot_count: u8,
}
pub struct Membership { pub instance: u32, pub slot: u8, pub centre_position: u32 }
```

- `codes[instance, cell]` is the full occupation code with the centre's particle at slot `0` and every other
  slot's particle read from the member instance at `wrap(cell + slot_offset)`. It is built once and updated by
  Step 15 on every accepted change.
- `code_ids` is the `u32` index of the code in the table's ascending `occupation_codes`; a missing code is a
  **data error** (`ContextError::MissingOccupationCode`, exit 12). Step 14 checks that the code exists for the
  initial lattice for every centre particle in `particle_table_mapping`, so a wrong cluster geometry fails at
  initialization rather than during a run.
- `energy[instance, cell, p]` = `table.energies[code_id(code with slot 0 = p)]` for `p` in the table's
  `particle_table_mapping`; zero (unused) for other particles. The table layout is 1-D per code — there is no
  centre row: the centre particle is encoded in slot `0`. `particle_table_mapping` is the **ascending list of
  allowed centre particles**, not a `[u8; 64]` particle→row map (the Step 15 draft's `[center_row][code]`
  wording is corrected; §7.3 and the data contract are the source).

### 6.4 Cluster geometry: the v1 limitation (open decision OD-1)

The `.msl` v1 cluster link persists only `{cluster_id, byte_position}`. It does **not** persist the member's
relative cell offset; the compiler knows it (`ResolvedPathPosition.Encoding`) but the writer drops it, and the
payload's positions carry only `vector4 = [0,0,0,P]` (no fractional coordinates). The solver therefore cannot
reconstruct a member image in general.

Step 14 fails closed instead of guessing:

- For every instance and every slot, take the unique pair link from the centre's P1 list to
  `slot_positions[slot]`. If there is **exactly one** such image, its `offset` is the slot offset (this covers
  every intra-cell cluster and every cluster whose member has a single periodic image). If there are **zero or
  more than one** images, initialization fails with `ContextError::ClusterGeometry` naming the table, slot, and
  positions.
- The derived geometry is cross-checked by the code-existence rule in §6.3: a wrong offset almost always yields
  a code the table does not contain, which fails at initialization.
- This accepts every checked-in fixture: `step-13-populated` has one cluster centred on position 1 with members
  0 and 2, each with a single `[0,0,0]` image; the other fixtures have no cluster table.
- Consequence: a writer-valid package whose cluster uses a second-nearest image, or a member with several
  periodic images inside the interaction range, is **rejected** with an actionable error. That is a recorded v1
  limitation, not a silent approximation. The permanent fix is a future `.msl` payload revision that persists
  the member offset (or fractional P1 coordinates); it is a format change and needs its own decision, amendment,
  and version handling. Recorded in §23 and [`05-open-decisions.md`](../05-open-decisions.md).

## 7. Energy runtime and initial lattice energy

### 7.1 Units

- Load-time conversion factor `ev_to_kt = 1/(k_B_eV * T)` with `k_B_eV = 8.617333262e-5` eV/K; every energy
  table and the site background are multiplied once. The context stores kT; eV appears only at output
  boundaries. `T > 0` and both factors must be finite (`ContextError::NonFiniteFactor`, exit 13).
- `field_scale = |E| * 1e-10 / kT_eV` in `1/(e·Å)` and the normalized field direction from the job settings.
  The unit chain is `|E| [V/m] × 1e-10 [m/Å] = |E|·1e-10 [eV/(e·Å)]`; multiplying by the rule's
  `charge_transport_vector` `[e·Å]` and dividing by `kT` `[eV]` gives the dimensionless field term. Step 15
  uses `field_scale` and does not re-derive the constant (§11).

### 7.2 Site and pair vectors

- `site: Vec<f64>` length `P*pc`, copied from `defect_background` and converted to kT. It is a unit-cell table;
  no per-cell copy is allocated.
- `energy[env*pc + p] = site[P*pc + p] + Σ_{link in links(P)} pair_table[link.pair_id][p][partner(target)]`
  where `partner(target)` is the current particle at the resolved absolute target. The sum uses the sorted link
  order. Entries for particles outside the environment's selection mask are still computed (they are cheap and
  Step 15 reads arbitrary rule particles); they are not selectable.
- Pair tables are converted to kT once at initialization; the model's payload order is preserved.

### 7.3 Cluster vectors

- Per instance and cell, `cluster_energy[instance, cell, p]` as in §6.3.
- The cluster term is **not** folded into `energy`: `energy` is the site+pair path-node vector, and the cluster
  term is a separate array counted once at the centre. This is the documented physics convention
  ([`docs/physics/energies.md`](../../physics/energies.md): "A cluster contributes only at its centre") and it
  is what makes the total exact.
- Step 15 reads `cluster_energy` for the affected centres; Step 14 exposes the touched instances per direction
  (§9.3) so the transition-energy sum never has to scan.

### 7.4 Initial lattice energy

$$
U = \sum_{\text{stable } e} E_\text{site}(P(e), p(e))
  + \tfrac{1}{2} \sum_{\text{stable } e} \bigl(E_\text{env}(e, p(e)) - E_\text{site}(P(e), p(e))\bigr)
  + \sum_{\text{instances } c} E_\text{cluster}(c, p(c))
$$

Equivalently: each site background once, each pair once (the 0.5 corrects the both-ends sum), each cluster once
at its centre. This deviates from the original's `0.5 * Σ_stable E[current]`, which also halves the site and
cluster terms and replicates clusters at every member; the deviation is deliberate, documented in §14, and
covered by a hand-computed test (a system with a site offset, one pair interaction, and a 3-site cluster must
produce the exact analytic value). Step 15 owns the incremental update and must use the same decomposition; a
debug-only full recomputation helper is provided for consistency checks.

The formula is evaluated in kT; `lattice_energy_ev = U * kT_eV` is the reported value. An MMC abort uses energy *fluctuations*, so a constant
scale error would cancel; the absolute value is nevertheless the documented convention and is what `.mcs`
reports from Step 18.

## 8. Selection pools and optimizers

- Direction count per `(P, particle)` is `jump_count[P*pc + particle]` (`u32`):
  - `0` = no selectable directions (static or passive; the original's `-1`/`0` split is not representable);
  - `>0` = selectable **iff** the environment's `selection_particle_mask` has the particle bit set.
- One `DirectionPool` per distinct selectable direction count, ordered ascending by `direction_count` (Step 16
  walks pools in this order). `members` are `(PositionId, ParticleId)` inserted in ascending
  `(cell-major absolute position, particle)` order; `slot_of` is a flat `P*pc` key map
  (`key = P*pc + particle`, `u32::MAX` = absent) so removal is O(1) and updates the swapped member's slot.
- `SelectionPools { pools: Vec<DirectionPool>, selectable_jump_count: u64, pool_of: Vec<u8> }`:
  `pool_of[key]` gives the pool index (or `u8::MAX`) so a state change can find the member without scanning.
  `selectable_jump_count = Σ members.direction_count` and must be `≤ max_selectable_jump_count` and `> 0` for
  KMC (`ContextError::NoSelectableJumps`, exit 12).
- Optimizers: each job `SelectionOptimizer(position, particle)` removes that combination from its pool after
  construction. An optimizer that is not in the pool (combination not selectable in the initial lattice) is a
  no-op logged at debug level, not an error; the reader already validated the indices. Removing the last member
  of a pool leaves an empty pool, and `selectable_jump_count` is reduced accordingly.
- State-change helpers (used by Steps 15/16/17, not by Step 14): `set_selectable(P, particle, count)` performs a
  swap-remove or insert and updates `selectable_jump_count`/`pool_of`/`slot_of`. A combination re-enters the
  pool after a particle change (the optimizer is a start-condition hint, matching the original), which is
  documented and tested.
- The pools are memory-bounded: `Σ members × 8 B + pools × P*pc × 4 B`.

## 9. KMC jump status

### 9.1 Status identity and path resolution

- A status is `(P1 position, direction)` indexed `status = direction * P + position`, i.e. one entry per
  `P * direction_count`. This is the translation-compact equivalent of the original's
  `[cell][direction]` array: the sender/receiver geometry is cell-independent, and the absolute path is
  resolved at use.
- `resolve_node(selected_position_abs, direction, node) -> u32`:
  `node 0 = selected_position_abs`; for `i ≥ 1`,
  `trim(cell(selected) + direction.path[i].cellA/B/C) * P + direction.path[i].P`, where `trim` is the
  component-wise wrap into `[0,A)×[0,B)×[0,C)`. Node 0's stored `P` is the
  representative position and is **replaced** by the selected position, so a direction mapped into another
  symmetry-equivalent start bucket is applied at the selected bucket. This is the Step 16 contract.
- The helper is bounds-checked (`node < path_len`) and does not allocate; it writes into a caller-provided
  `[u32; 10]`/slice.

### 9.2 Sender/receiver links

- For each status, resolve the path nodes; for every ordered pair `(sender_path_id, receiver_path_id)` with
  `sender != receiver`:
  - find the sender's pair links whose absolute target equals the receiver's absolute environment; take the
    first in sorted order (deterministic); append `JumpLink { sender_path_id, link_id }` where `link_id` is the
    relative index into the sender P1 link list.
  - If several links from the same sender P1 position reach the same absolute target (possible in small cells),
    the first sorted link wins and the choice is documented; Step 15 reads the same link, so the pair table is
    consistent.
- **Receiver filter: structural, not initial-occupation-based.** The original skips path nodes whose initial
  particle is void. Step 14 includes every path node whose P1 position is structurally stable, so a receiver that
  is void at initialization but occupied later still receives its delta. This is a correctness fix over the
  original, recorded in §14 and tested in Step 16 (a path node void at init, occupied later).
- Bound: at most `L*(L-1) ≤ 90` links per status (the original's 56 is the `L=8` case). The builder uses a fixed
  90-entry scratch array; exceeding it is an internal invariant error (exit 13), never a silent truncation and
  never the original's unchecked overflow. A status with zero links is valid and common.
- CSR: `offsets: Vec<u32>` length `P*D + 1`, `links: Vec<JumpLink>`, both grouped by ascending status index.

### 9.3 Cluster touches per direction

```rust
pub struct ClusterTouch { pub instance: u32, pub slot: u8, pub sender_path_id: u8 }
```

- For every non-centre membership whose absolute environment equals a resolved path node, record a touch.
  Step 15 uses the touched instances to update the code and recompute the centre's cluster term for S0/S2 and
  the applied state. The centre itself needs no touch: when the centre is a path node, the rule's node particle
  is already read as `cluster_energy[centre][S0/S2]`, and a non-centre member change always has a membership
  record on the member.
- The touch list is precomputed per status, bounded by `L * memberships_per_position` (validated against
  `max_cluster_links_per_position`), and empty when the model has no cluster tables.

### 9.4 Interface for Step 16's exact occupation check

Step 14 does **not** resolve a rule to a selected direction and does not check every `(rule, direction)` pair
(the Step 13 M-3 assignment). It exposes the data Step 16 needs after the direction and rule are known:

- `allowed_mask_of(abs_env) -> u64` (the position's allowed mask, void exempt),
- `path_len(direction) -> u8`,
- `resolve_node(selected_abs, direction, node) -> u32`,
- `rule(rule_id) -> &JumpRule` / `collection_rules(collection_id) -> &[RuleId]`,
- `direction_for(position, particle, relative) -> JumpDirectionId` via
  `jump_mapping[jump_offsets[key] + relative]`.

Step 16 then validates each non-void `state0/state1/state2` node particle against the resolved node's allowed
mask and reports a mismatch as exit 12. Step 14's jump status uses the same `resolve_node` rule, so the two
cannot disagree.

### 9.5 Static S2 corrections

Not computed in v1. Step 14 stores the rule's `static_correction` (`None` in v1) and the link/cluster data a
future static correction needs. Dynamic S2 is Step 15's default. The `JumpStatus` container therefore carries
only links; no per-rule static flag is introduced.

## 10. Goals, counters, tracker index space, and the mobile mapping

- `mobile_count = # environments with MOBILE` (particle != void; passive species included, matching the
  original). For KMC this is the goal multiplier; `goals.prerun = pre_run_mcsp * mobile_count`,
  `goals.main = target_mcsp * mobile_count`, `goals.total = prerun + main` rounded **up** to a multiple of 100,
  `cycles_per_block = max(1, total / 100)`.
- `mobile_count == 0` for a KMC job is `ContextError::NoMobileParticles` (exit 12): the original returns
  `ERR_NOMOBILES` from `SetCycleCounterStateToDefault`, and every abort condition would be degenerate.
- MMC has no pre-run goal, no total goal, no block size, and no time; `goals` is zeroed and Step 17 uses its own
  sample/break settings.
- Counters: per particle `{skip, success, rejected, blocked, start_unstable, end_unstable}` (`u64`), plus
  `cycles: u64` and `mcs: u64`. All zeroed. Step 16/17 increments; Step 18 writes. No per-tracker counters are
  allocated in Step 14 (Step 18 owns jump statistics).
- Tracker index space (KMC only):
  - `mobile_tracker[env]` = sequential id in ascending absolute-environment order for every `MOBILE`
    environment; `mobile_positions[id]` = the absolute environment. Passive species get a mobile tracker
    (original quirk, preserved).
  - static tracker id = `cell * static_trackers_per_cell + position * (pc - 1) + (particle - 1)` for non-void
    particles (the documented `[cell][position][particle]` numbering); no array is allocated for values.
  - global tracker count and the `global_tracker_mapping`/`static_tracker_mapping` tables are exposed read-only;
    Step 18 owns values and `tracker_order` following.
- `--threads` and `rayon` are not used: single-threaded initialization keeps the determinism proof trivial.

## 11. Normalization, time step, and the Step 15 transport contract

### 11.1 Normalization

- `kT_eV = k_B_eV * T`.
- `K_fix = 1` when `normalization_energy_ev` is `None`; otherwise `K_fix = exp(normalization_energy_ev / kT_eV)`
  (the plan's `exp(-E/kT)^-1`); a non-finite `K_fix` is exit 13.
- `RawMax = 0` fresh; restored on resume. `K_auto = 1/RawMax` while `RawMax > 0` and finite, else `1`.
- `K_norm = K_fix * K_auto`; if not finite or `≤ 0`, `K_norm = K_fix` (the original's fallback), and a
  `K_fix` that is still non-finite/`≤ 0` is exit 13.
- `nu_max = settings.max_attempt_frequency_hz.unwrap_or(max frequency_hz over the active collections' rules)`.
  This is the original's build-time `AttemptFrequencyModulus`, which the v1 payload does not persist; the
  derived value is also Step 16's frequency-precheck denominator. `nu_max > 0` and finite, else exit 12.
- `dof = 2` when the job flag `use_dual_dof_correction` is set, else `1`.
- `dt = dof * K_norm / (nu_max * selectable_jump_count)`; recomputed by Step 16 when `K_norm` or the selectable
  count changes. `dt` must be finite and `> 0` (exit 13).
- MMC: `K_fix = K_norm = 1`, no time step, `time_s = 0`.

### 11.2 Transport-formula input contract (Step 13 assignment)

Step 13 validated that `charge_transport_vector` is finite and zero when no node changes charge, but could not
recompute the positional formula `Σ_node (q(S2,node) − q(S0,node)) · x(path node)` because the v1 payload has no
fractional P1 coordinates. Step 14 supplies everything that *is* available and states the boundary:

- `field_scale` (§7.1) and the normalized `electric_field_direction`;
- `cell_vectors_ang`, the direction's `movement[[f64;3]]`, `path_vector`, `length`, and the relative node
  encodings;
- the per-rule `charge_transport_vector` verbatim, and the per-node charges/state codes.

Step 15 computes `field = -0.5 * field_scale * (rule.charge_transport_vector · ê)`, must reject a non-finite
result (exit 13), and must not recompute a different vector from the movements: the fractional P1 coordinates
that would make the positional formula exact are not in the package. The residual risk and the future `.msl`
revision (fractional P1 positions or a signed transport vector) are recorded in Step 15's page and in §23.

## 12. Resume

```rust
pub enum ResumePhase { Prerun, Run, Completed }

pub struct ResumeState {
    pub phase: ResumePhase,
    pub model_signature: String,
    pub lattice_signature: String,
    pub job_id: u32,
    pub kind: SimulationKind,
    pub lattice_size: [u32; 3],
    pub position_count: u32,
    pub particle_count: u32,
    pub particles: Vec<ParticleId>,
    pub counters: ParticleCounters,
    pub cycles: u64,
    pub mcs: u64,
    pub simulated_time_s: f64,
    pub normalization: NormalizationState,   // K_fix, K_auto, RawMax, K_norm
    pub rng_state: RngStateV1,
    pub state_flags: u64,
    pub mobile_positions: Vec<u32>,          // optional cross-check; Step 18 writes it
}
```

- The `.mcs` decode is Step 18's. Step 19 (or the Step 16 run path) builds `ResumeState` from the document and
  passes it in `ContextOptions.resume`. Step 14 performs no file I/O and does not depend on `mocassin-state`.
- Validation (all → exit 12 unless noted): model/lattice signature equality with the loaded package, job id,
  kind, `lattice_size`, `position_count`, `particle_count`, `particles.len() == A*B*C*P`, every particle index,
  `simulated_time_s` finite and `≥ 0`, `K_fix/K_auto/K_norm` finite and `> 0`, `RawMax ≥ 0` and finite, a
  per-particle counter array of length `pc`, and the mobile mapping re-derivation. A bad RNG blob is
  `ContextError::RngState` (exit 13) because it is an internal/cryptographic state failure.
- The mobile mapping is **re-derived** from the resumed lattice with the same algorithm and compared with
  `mobile_positions` when present; a mismatch is exit 12. (The original restores the stored mapping and then
  re-syncs from it; re-derivation plus equality is strictly stronger and order-independent.)
- `ResumePhase::Completed` returns `InitOutcome::AlreadyCompleted`; Step 14 never resumes it. Step 19 maps that
  to the distinct "already completed" message/exit 0 and never to a silent success.
- Resume preserves `RawMax`; nothing in Step 14 resets it. A fresh context starts at `RawMax = 0`, time 0,
  zeroed counters.

## 13. Errors, exits, and diagnostics

`ContextError` (thiserror at the crate boundary) maps to the canonical solver exit table
([`01-target-architecture.md`](../01-target-architecture.md) §7.3):

| Variant | Exit | Meaning |
| --- | ---: | --- |
| `InvalidInput { what, detail }` | 12 | A validated package field is not usable by the context (index, length, mask, settings) |
| `ClusterGeometry { cluster_table, detail }` | 12 | Slot set, centre, or member image cannot be determined exactly (§6.4) |
| `MissingOccupationCode { cluster_table, code }` | 12 | A current/centre occupation tuple is absent from the table |
| `NoSelectableJumps` | 12 | KMC job with `selectable_jump_count == 0` |
| `NoMobileParticles` | 12 | KMC job with `mobile_count == 0` |
| `ResumeMismatch { what }` | 12 | Resume state does not match the package/job/lattice |
| `LimitExceeded { what, value, limit }` | 12 | A context limit (§2) rejects the job |
| `NonFiniteFactor { what }` | 13 | `kT`, `K_fix`, `K_norm`, `nu_max`, or `dt` is not finite/positive |
| `RngState(..)` | 13 | `RngStateV1` magic/version/word position/stream invalid |
| `Invariant { what }` | 13 | Debug/release invariant violation (CSR, pool total, code id, jump-status bound) |
| `Cancelled` | 15 | The caller's cancellation flag was set |

- Every diagnostic names the phase, the dense index (position, particle, cluster table, slot, status), and the
  fixture-independent reason; no `unwrap` on package data.
- `tracing` emits one `info` line per completed phase (counts and bytes) and debug lines for skips (e.g. an
  optimizer that was not in the pool). No file output.

## 14. Deviations from the original (each with a test)

| # | Deviation | Rationale | Test |
| --- | --- | --- | --- |
| D-1 | Compact per-P1 link CSR instead of a per-supercell-cell link array | Memory; translation invariance; same physics | Link set equals brute force for every supercell cell |
| D-2 | Link sort adds `target_position`/`offset` tie-breakers | Deterministic f64 summation order | Byte-stable context summary across runs |
| D-3 | Cluster energy counted once at the centre; site counted once; pair with 0.5 | Exact total; the original's uniform 0.5 is documented as a pair correction and misapplies to site/cluster | Hand-computed pair/cluster/site total |
| D-4 | Cluster row layout is 1-D per code (slot 0 = centre particle) | The .msl payload encodes the centre particle in slot 0; there is no centre row | Cluster energy vector vs. brute-force table lookup |
| D-5 | Jump-status receiver filter is structural stability, not the initial particle | A node void at init but occupied later must still receive deltas | Step 16: node void at init, occupied later |
| D-6 | Jump-status bound is `L*(L-1)` with an invariant error, not a fixed 56 with an overflow | `max_path_nodes = 10`; silent overflow is unacceptable | A 10-node direction yields ≤ 90 links; an 11th node is rejected by the reader |
| D-7 | `IsStable` requires the compiled site stability flag | Honours the compiled model; the original ignored it | Synthetic unstable position is never stable |
| D-8 | Mobile mapping re-derived on resume and compared | Stronger than restoring a stored mapping | Mismatched mapping is rejected; matching mapping resumes |
| D-9 | No `OPT_LINK_ONLY_MOBILES`, no static S2, no fast-exp, no parallel init | Deferred to Steps 15/29 with measurements | Absent by construction; recorded in the parity notes |

## 15. Interfaces consumed by later steps

| Consumer | Interface | Step 14 obligation |
| --- | --- | --- |
| 15 | `node_energy(abs, particle) -> f64`; `cluster_energy(instance, cell, particle)`; `cluster_code(instance, cell)`; `touched_instances(status)`; `link_range(P)`; `link_target(abs, link)`; `update_particle_ids(P)`; `field_scale`, `field_direction`; `cell_vectors` | Tables/codes/vectors/links in fixed order; no delta logic |
| 16 | `pools` (ordered, with `selectable_jump_count`), `direction_for`, `path_len`, `resolve_node`, `jump_links(status)`, `allowed_mask_of`, `goals`, `normalization`, `dt`, `rng` | Pool/jump-status construction and the exact-occupation data; no selection or acceptance |
| 17 | `pools`, `interaction_range`, `linked(a,b)`, `node_energy`, `cluster_energy`, counters | Same runtime, no MMC-specific field or time |
| 18 | `trackers.mobile_positions`, static/global index space, counters, `lattice_energy_ev`, `rng.export_state`, `time_s`, `normalization` | Index space and values Step 18 reads; no `.mcs` |
| 19 | `InitOutcome`, `ResumeState`, `ContextError`/exit mapping, `MemoryReport` | Resume hook and errors; no CLI option |

## 16. Implementation tasks

- [ ] Create the `mocassin-solver` context module layout (§2.2 of `code-structure.md`) with the `ContextOptions`,
      `ContextLimits`, `ContextError`, `MemoryReport`, and `Dimensions`.
- [ ] Implement the lattice/environment arenas, index/wrap helpers, flags, tracker id space, and cancellation.
- [ ] Implement the compact per-P link CSR, absolute-target resolution, sorting, and the cluster association.
- [ ] Implement the cluster instance/membership validation, geometry rule, codes, code ids, and energies.
- [ ] Implement the site table, per-environment site+pair vectors, per-instance cluster energies, and the exact
      initial lattice energy.
- [ ] Implement the selection pools, mask/optimizer application, selectable count, and add/remove helpers.
- [ ] Implement the per-`(P, direction)` jump status (links + cluster touches), the derived bound, and
      `resolve_node`.
- [ ] Implement the goals, counters, `K_fix`/`K_auto`/`K_norm`, `nu_max`, `dof`, and `dt`.
- [ ] Implement the resume hook (`ResumeState`, validation, re-derived mobile mapping, `AlreadyCompleted`).
- [ ] Add the real-writer 10×10×10 acceptance fixture, context-summary snapshots, the allocation test, and the
      `context` Criterion benchmark with a same-class baseline entry.
- [ ] Update the documentation, parity rows, status dashboard, and closeout evidence.

## 17. Acceptance criteria

- [ ] `msl-v1` job 1 (6×6×6 ceria) and `amend-1-net10` initialize a context whose mobile count, selectable count,
      `prerun`/`main`/`total` goals, block size, `dt`, and tracker counts are asserted against computed values.
- [ ] `step-13-populated` KMC and MMC jobs initialize; the pair link set per P1 position equals a brute-force
      expansion of the payload for every supercell cell; the clustered energy vectors equal a direct table
      lookup; the MMC context has no pools/jump status/time.
- [ ] `step-13-centered` initializes; a direction mapped into another start bucket resolves its path at the
      selected bucket; the per-node allowed masks are exposed unchanged.
- [ ] The 10×10×10 real-writer fixture initializes in `< 100 ms` (release, warm, same-runner-class) and the
      reported memory total is within budget.
- [ ] The initial lattice energy matches a hand-computed site/pair/cluster system exactly.
- [ ] The context summary (counts, link histogram, pool sizes, jump-status sizes, goals, `dt`) is byte-identical
      across two initializations of the same package.
- [ ] Zero selectable directions, zero mobile particles, a bad cluster geometry, a missing occupation code, a
      context-limit overflow, a cancelled initialization, and a mismatched resume each return the documented
      error/exit.
- [ ] Resume restores lattice, counters, RNG, time, and normalization exactly; `RawMax` is preserved; a
      completed state returns `AlreadyCompleted`.
- [ ] A counting allocator observes zero allocations in the hot accessors and the pool add/remove helpers after
      construction.
- [ ] `MemoryReport` matches the actual vector capacities.
- [ ] Documentation written: `solver-architecture.md` (context section), `physics/energies.md` (total-energy
      convention), `performance/index.md` (context budget), `troubleshooting/errors.md` (context failures),
      `testing-and-parity.md` (tiers/fixtures), `msl-format.md` (cluster-link limitation and runtime expansion).

## 18. Documentation

Implementation-time documentation (the planner records only the design here):

- `docs/development/solver-architecture.md` — context arenas, compact link CSR, cluster runtime, energy
  vectors, pools, jump status, goals/normalization, resume, memory (the design section is written at this
  freeze; the implementor reconciles it with the shipped code).
- `docs/physics/energies.md` — the total lattice energy convention (site once, pair 0.5, cluster at centre) and
  the initial-energy definition.
- `docs/performance/index.md` — the context-initialization budget, the measured medians, the memory report, and
  the same-runner-class baseline entry. (The Step 14 draft's `docs/performance/benchmarks.md` does not exist;
  this page is the performance page.)
- `docs/troubleshooting/errors.md` — zero-mobile/zero-direction, cluster-geometry rejection, context limits,
  resume mismatch, and the exit codes.
- `docs/development/testing-and-parity.md` — the Step 14 fixtures, snapshot/allocation/benchmark tiers.
- `docs/reference/msl-format.md` — the cluster-link limitation, the compact expansion rule, and the center-only
  cluster semantics.
- Parity: the new "Simulation context initialization" solver row and the "Selection optimizers" runtime-removal
  note.
- `docs/planning/closeout/step-14.json` plus `python tools/check_closeout_evidence.py`; the audit request in
  `docs/planning/audits/step-14.md` is created at closeout, not now.

## 19. Tests

**Fixtures.** Step 14 consumes the existing real-writer packages unchanged: `msl-v1` (6×6×6 ceria KMC),
`amend-1-net10` (byte-fixed compatibility copy), `step-13-populated` (2×2×2, 3 P1 positions, 6 pair tables,
1 cluster table, one KMC and one MMC job), and `step-13-centered` (2×2×2, 8 P1 positions, 4 pair tables,
cross-bucket `jump_mapping`, per-node mask difference). Feature 6 adds one new **test artifact**:
`tests/contracts/packages/step-14-ceria-10x10x10/`, built by the real .NET writer from a committed sample
`samples/step-14-ceria-10x10x10/step-14-ceria-10x10x10.mocprj` derived from
`samples/jobs/ceria-temperature-sweep.mocprj` (one KMC job, `10×10×10`, the same model), with a canonical
package summary and a .NET rebuild test. It is not a schema change and does not change `contracts/manifest.json`.
The populated model's link/energy scale is covered by cloning the real `step-13-populated` `JobPackage` onto a
hand-built `10×10×10` `InitialLattice` in the test (the model stays real; Step 14 does not re-verify the lattice
signature), which exercises the link CSR, clusters, and pools at 3,000 positions without a second fixture.

| Test | Assertion | Tier |
| --- | --- | --- |
| Index/wrap | `(a,b,c,P)` ↔ linear index round-trip; `wrap` for negative and oversized offsets; `A*B*C*P` checked | PR-fast |
| Links | Per-P link set and sorted order equal a brute-force payload expansion; the absolute target matches for every cell; a duplicated image is kept | PR-fast |
| Cluster geometry | Slot set/centre/contiguity validation; a zero-offset single image resolves; zero/multiple images fail with `ClusterGeometry`; a missing current code fails | PR-fast |
| Cluster energy | `cluster_energy(instance, cell, p)` equals a direct table lookup for every allowed centre particle | PR-fast |
| Energy vectors | `node_energy` equals a brute-force site+pair sum over the resolved links | PR-fast |
| Initial energy | Hand-computed site+pair+cluster system matches exactly; the debug recomputation agrees | PR-fast |
| Pools | Direction-count buckets; mask filtering; optimizer removal; swap-remove slot update; `selectable_jump_count`; re-add after a particle change | PR-fast |
| Jump status | Link set equals a naive per-cell recomputation for every cell of a small lattice; the structural receiver filter; `L*(L-1)` bound; `resolve_node` for a cross-bucket direction | PR-fast |
| Goals/normalization | `prerun`/`main`/`total`/block, `K_fix`/`K_auto`/`K_norm`, `dof`, `nu_max`, `dt` arithmetic | PR-fast |
| Bounds/cancel | Each limit rejects before allocation; a set cancellation flag returns `Cancelled` | PR-fast |
| Resume | Exact restore; signature/size/mapping mismatches rejected; `RawMax` preserved; `AlreadyCompleted` | PR-fast |
| Allocation | Zero allocations in hot accessors and pool helpers after construction | PR-fast (own test binary) |
| Real packages | `msl-v1`, `amend-1-net10`, `step-13-populated` (KMC+MMC), `step-13-centered` initialize and satisfy §17 | PR-medium |
| 10×10×10 acceptance | The new real-writer fixture initializes in `< 100 ms`; the memory report is within budget | PR-medium |
| Determinism snapshot | The context summary is identical across two initializations (insta inline snapshot) | PR-fast |
| Benchmark | Criterion `context` init medians for the fixtures; recorded evidence and a same-class `baselines.json` entry | Nightly/manual |
| Stress (nightly) | A synthetic 500,000-position lattice with the populated model stays within the configured limits and memory | Nightly |

No mocks: real packages via `mocassin_msl::Package::open_readonly`/`load_job`, real temporary directories where a
test needs one, and real domain values. The only test double is the counting global allocator in its own test
binary (a true process/allocator boundary).

## 20. Performance and memory budgets

| Scenario | Budget |
| --- | ---: |
| Context init, 10×10×10 real-writer fixture (release, warm) | `< 100 ms` |
| Context init, `msl-v1` 6×6×6 and `step-13-populated` 2×2×2 | `< 20 ms` each |
| Context memory total (`MemoryReport`), 10×10×10 fixture | `≤ 64 MiB` |
| Context memory total, 500,000 positions × 64 particles (limit case) | `≤ 512 MiB` energy vectors + links; the `ContextLimits` reject above |
| Allocation after construction | 0 in hot accessors |
| A same-runner-class Criterion baseline | `tests/reference/bench/baselines.json` `rustContext` entry; > 10 % regression is a release failure |

Evidence: `tests/reference/bench/step-14-context.json` plus the raw Criterion log, and a measured process peak
RSS (Linux `VmHWM`, recorded as non-gating context next to the memory report). Absolute timings are compared
only within the recorded runner class; a different CPU/OS/Rust version records its own entry.

## 21. Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Cluster member offsets are not in `.msl` v1 (OD-1) | Fail closed on zero/multiple images; code-existence cross-check; recorded limitation and future format decision; the fixtures pass |
| Energy convention differs from the original | Documented deviation D-3 with a hand-computed test; Step 15 uses the same decomposition; Step 29 parity covers it |
| Link-graph memory at 500k positions | Compact per-P CSR, `ContextLimits`, `MemoryReport`, nightly stress |
| Jump-status staleness or overflow | Structural receiver filter, derived bound with an invariant error, Step 16 test |
| Pool update bugs | O(1) slot map, invariant checks (total = Σ counts), property tests |
| Resume drift / silent reset | Signature/size/mapping checks, `RawMax` preservation, `AlreadyCompleted`, Step 18 fixtures |
| f64 non-determinism | Fixed summation order, tie-broken sort, no parallel init, no FMA changes |
| API change after Step 15 starts | API frozen at feature 3; a rework is a stop-and-report re-review trigger |
| Zero-selectable/zero-mobile packages | Explicit exit-12 errors with actionable messages and a troubleshooting section |
| Fixture absence for 10×10×10 | Feature 6 builds the real-writer fixture (a test artifact, no schema change) |

## 22. Dependencies and interfaces

- **Step 13 (Done, merged and tagged `step-13`):** `JobPackage`, `ParameterizedModel`, `InitialLattice`,
  `EnvironmentModel`/`PairLink`/`ClusterLink`, `EnergyModel`/`PairTable`/`ClusterTable`, `TransitionModel`,
  `DirectionPool`, `JumpStatus`/`JumpLink`, `Rng`/`RngStateV1`, `ReaderLimits`, the real-writer fixtures, and
  the exit-code model. Step 14 consumes them unchanged and does not re-read the package.
- **Step 11/12 (Done):** the `.msl` v1 schema and payload field set. Step 14 adds no field and no version.
- **Step 15 (next):** owns S0/S1/S2, pair/cluster deltas, the field term, state application, and the incremental
  lattice energy; consumes §15's interface.
- **Step 16:** owns selection, rule lookup, the exact per-direction occupation check, acceptance, normalization
  updates, time, pre-run, aborts, and resume orchestration; consumes §9.4 and §15.
- **Step 17:** MMC selection, exchange, Metropolis, break check; consumes the same runtime with no pools/jump
  status/time.
- **Step 18:** tracker values, histograms, `.mcs` writer/reader; consumes the tracker index space and provides
  the `ResumeState` decode.
- **Step 19:** the CLI run path, batch, extensions; maps `InitOutcome`/`ContextError`.
- **Step 29:** statistical parity for the documented deviations, the > 10 % benchmark job, and the optional
  static-S2/mobile-link/parallel optimisations.

## 23. Reconciliation

- [`01-target-architecture.md`](../01-target-architecture.md) §4: the solver context is built once per job from
  the validated `JobPackage`; the link graph is translation-compact. No crate or dependency change.
- [`02-data-contracts.md`](../02-data-contracts.md) §3.3: cluster links carry the environment's own slot and no
  member offset; cluster tables are 1-D per occupation code with the centre particle in slot 0 and
  `particle_table_mapping` as the ascending allowed centre-particle list. Clarification only; no field changes.
- [`03-quality-strategy.md`](../03-quality-strategy.md): new §9.12 for the context layer and a §13 benchmark row.
- [`04-roadmap.md`](../04-roadmap.md) §3.1: the recorded complexity class and the owner-acceptance request.
- [`05-open-decisions.md`](../05-open-decisions.md): OD-1 (cluster geometry), OD-2 (energy accounting
  confirmation), and the Step 14 class acceptance row.
- [`appendix-b-parity-matrix.md`](../appendix-b-parity-matrix.md): the new solver row and the selection-optimizer
  note.
- [`code-structure.md`](../../development/code-structure.md) §2.2: the `mocassin-solver` module layout.
- [`solver-architecture.md`](../../development/solver-architecture.md): the context section and the ownership
  table.
- [`physics/energies.md`](../../physics/energies.md): the total-lattice-energy convention.
- **No change:** `cli.md` (no CLI surface), `msl-format.md` (implementor adds the limitation note),
  `contracts/manifest.json`, ADRs, and the status dashboard (step-agent-owned; it is updated at the start gate).

## 24. Done when

A validated job package becomes a fully initialized context — lattice, environment states, compact links,
clusters, site/pair/cluster energies, exact initial lattice energy, selection pools, KMC jump status, tracker
index space, counters, goals, normalization, time step, RNG, and the resume hook — with the documented errors,
bounded and reported memory, deterministic structured state, real-package acceptance, the allocation proof, the
recorded benchmark, and the documentation and parity updates.
