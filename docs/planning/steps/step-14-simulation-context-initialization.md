# Step 14 — Simulation Context Initialization

**Phase:** D. Solver
**Depends on:** 13 and `step-11.1` (the `step-11-amend-2` reader/runtime cluster-code lookup; see §22)
**Delivers:** A deterministic, allocation-bounded simulation context built once per job from the validated
`JobPackage`: lattice and environment arenas, an owner-major neighbour-link CSR with an inverse routing index,
the cluster runtime, the energy background and environment energy vectors, the supercell selection pools, the
receiver-major KMC jump status, the mobile/static tracker index space, counters, goals, normalization, the time
step, and the resume hook. No energy deltas, no KMC/MMC cycle, no tracker values, and no `.mcs` output.

## Implementation status

- **Status:** In audit — `Passed with notes` accepted by the owner 2026-11-21; merge/tag in progress
- **Step branch:** `step/14-simulation-context-initialization`
- **Step CI:** [44922414484](https://github.com/seb-eis/mocassin-2/actions/runs/44922414484) — Ubuntu closeout, green at `3fd4516` (no per-feature CI)
- **Main merge:** —
- **Step tag:** —
- **Audit:** `Passed with notes` — accepted by the owner 2026-11-21; F-1–F-3/N-1–N-3 deferred to `fix/step-14-structure-and-tests` ([record](../audits/step-14.md))
- **Design review:** `Ready with notes` — accepted by the human owner 2026-11-21. Round 4 at record `620f20a`
  (freeze `a430d64`) closed F-29 and F-30–F-38; F-39–F-43 are minor/note corrections the step agent records
  with the start-gate status. Rounds 1–3 returned `Changes required` (F-1–F-38). Record:
  [`docs/planning/reviews/step-14.md`](../reviews/step-14.md). **F-39** (stale status block) and **F-40**
  (the "feature 2 fallback" wording) are corrected in this block; **F-41** in §22, **F-42** in §20, and
  **F-43** in the roadmap row.
- **Cross-step dependency:** F-29 (cluster-code expansion) is resolved at the format level by
  [`step-11-amend-2`](../amendments/step-11-amend-2-cluster-code-expansion.md), approved and merged
  (`2f3a6fe`, design freeze `bf273ff`), and implemented by `step-11.1` (merged 2026-11-21, `58c8e9a`).
  Step 14 consumes `ClusterTable::lookup` and the three-way
  `Canonical`/`MissingFromMap`/`ExpansionMapAbsent` result. Feature 2's cluster work is gated on that merge
  and has **no** direct-`occupation_codes` fallback (review F-40). F-29's formal closure remains a Step 14
  gate.
- **Sizing:** Complexity class **High** — accepted by the human owner at the start gate 2026-11-21 as one
  unit; no split proposed. See §4.
- **Last updated:** 2026-11-21 (closeout CI green at `3fd4516`; audit requested)

| Feature | Branch | Status | Local gates | Step CI | Squash commit |
| --- | --- | --- | --- | --- | --- |
| 1 Context skeleton, arenas, limits, errors | `feature/step-14-simulation-context-initialization/context-arenas-and-limits` | Squashed | `cargo fmt --check`, `cargo clippy --workspace --all-targets -- -D warnings`, `cargo test --workspace` (36 lib + 9 acceptance tests, 1 `Send` doctest, 1 `!Sync` compile-fail doctest), `cargo build --release`, `structure`, `docs --require` all exit 0 | Not run (no per-feature CI) | `1a73969860715c55260cd1db0257c8a59241a647` |
| 2 Neighbour-link CSR and cluster runtime | `feature/step-14-simulation-context-initialization/link-csr-and-clusters` | Squashed | `cargo fmt --check`, `cargo clippy --workspace --all-targets -- -D warnings`, `cargo test --workspace` (38 lib + 15 link + 16 cluster + 9 skeleton acceptance tests, 1 `Send` + 1 `!Sync` doctest), `cargo build --release`, `structure` (7 passed), `docs --require` all exit 0 | Not run (no per-feature CI) | `486389ec5736571f50adda0d77ae95aa8f9ea4cb` |
| 3 Energy runtime and initial lattice energy | `feature/step-14-simulation-context-initialization/energy-runtime-and-initial-energy` | Squashed | `cargo fmt --check`, `cargo clippy --workspace --all-targets -- -D warnings`, `cargo test --workspace` (38 lib + 8 energy + 16 cluster + 15 link + 9 skeleton acceptance tests, 1 `Send` + 1 `!Sync` doctest), `cargo build --release`, `structure` (7 passed), `docs --require` all exit 0 | Not run (no per-feature CI) | `2f284cce2f65532007a9be883eaaea83ae4500b5` |
| 4 Selection pools and jump status | `feature/step-14-simulation-context-initialization/selection-pools-and-jump-status` | Squashed | `cargo fmt --check`, `cargo clippy --workspace --all-targets -- -D warnings`, `cargo test --workspace` (38 lib + 10 pools + 10 jump + 8 energy + 16 cluster + 13 link + 9 skeleton acceptance tests, 1 `Send` + 1 `!Sync` doctest), `cargo build --release`, `structure` (7 passed), `docs --require` all exit 0 | Not run (no per-feature CI) | `1613cfc6da1832fc4faacba0b0073e001cd30f1f` |
| 5 Goals, counters, normalization, time step, resume | `feature/step-14-simulation-context-initialization/goals-normalization-and-resume` | Squashed | `cargo fmt --check`, `cargo clippy --workspace --all-targets -- -D warnings`, `cargo test --workspace` (38 lib + 15 goals + 7 resume + 10 pools + 10 jump + 8 energy + 17 cluster + 13 link + 9 skeleton acceptance tests, 1 `Send` + 1 `!Sync` doctest), `cargo build --release`, `structure` (7 passed), `docs --require` all exit 0 | Not run (no per-feature CI) | `6500055d6cf14e544a85d09db02dd27b1e2f9e5f` |
| 6 Fixtures, benchmarks, documentation, and closeout | `feature/step-14-simulation-context-initialization/fixtures-benchmarks-and-closeout` | Squashed | `./tools/verify.sh all --require` exit 0 at `8c09a8c` (all .NET tests, Rust, Python, docs strict, contracts/policy, AOT evidence); the only later delta to the audited commit is the benchmark-evidence provenance string | [44922414484](https://github.com/seb-eis/mocassin-2/actions/runs/44922414484) — Ubuntu, green at `3fd4516` | `00fceabce7fa6d910cb79f701f175aa4b681bf5f` |

### Feature list (scope and outcomes)

| # | Feature | Branch | Scope | Outcome |
| --- | --- | --- | --- | --- |
| 1 | Context skeleton, arenas, limits, errors | `feature/step-14-simulation-context-initialization/context-arenas-and-limits` | Module layout; `ContextOptions`/`ContextLimits`/`ContextError`/`MemoryReport`/`Dimensions`; lattice/flag/mobile-tracker arenas; index/wrap helpers; phase pipeline and cancellation; error/exit mapping; invariant sweep | A fresh ceria `JobPackage` yields a context with the correct dimensions, lattice, flags, empty links/clusters/pools/jump status, zeroed counters, and a `MemoryReport`; every limit and the cancel flag return the documented error |
| 2 | Neighbour-link CSR and cluster runtime | `feature/step-14-simulation-context-initialization/link-csr-and-clusters` | Owner-major per-P1 link CSR, inverse routing index, partner resolution; deterministic sort; per-link cluster association; **one instance per slot-0 link** (member-site validation, candidate/unique-image slot resolution), centre index, codes/code ids | Link and inverse sets equal a brute-force payload expansion for every cell; the populated fixture's cluster resolves; a multi-centre cluster resolves per instance; an absent/ambiguous image fails closed; a missing code fails at init |
| 3 | Energy runtime and initial lattice energy | `feature/step-14-simulation-context-initialization/energy-runtime-and-initial-energy` | eV→kT factor; site table; site+pair vectors; per-instance cluster energy; exact initial total; debug recomputation helper; `field_scale` | The hand-computed site/pair/cluster total matches; the vectors equal a brute-force sum; `step-13-populated` initializes with the documented energies |
| 4 | Selection pools and jump status | `feature/step-14-simulation-context-initialization/selection-pools-and-jump-status` | Supercell direction-count pools with reserved capacity; mask/optimizer application; `selectable_jump_count`; O(1) slot maps and update helpers; per-`(P1, direction)` receiver-major jump status with the structural filter, derived bound, link-carried cluster associations, and `resolve_node`; **retires `DirectionPool` / reshapes `JumpLink` (public Rust API break recorded, Step 13 invariant tests updated)** | Pool counts/removal/swap-remove are correct in several cells; jump links equal a naive receiver-major recomputation; a cross-bucket direction resolves at the selected bucket; `step-13-centered` passes |
| 5 | Goals, counters, normalization, time step, resume | `feature/step-14-simulation-context-initialization/goals-normalization-and-resume` | Mobile/static tracker index space; goals/block size; counters; `K_fix`/`K_auto`/`K_norm`/`dof`/`nu_max`/`dt`; `ResumeState`/`ResumePhase` validation, re-derived mapping, `AlreadyCompleted` | Goal/normalization arithmetic matches the hand values; resume restores exactly and rejects mismatches; the MMC context has no pools/jump status/time |
| 6 | Fixtures, benchmarks, documentation, and closeout | `feature/step-14-simulation-context-initialization/fixtures-benchmarks-and-closeout` | New 10×10×10 real-writer fixture and .NET rebuild test; new multi-centre cluster fixture from the centred sample; context-summary snapshots; allocation test; `context` Criterion benchmark and `rustContext` baseline; docs/parity/status/closeout | `< 100 ms` and `≤ 64 MiB` budgets measured on the recorded runner class; zero-allocation proof; docs build strictly; closeout evidence passes its checker; audit request complete |

### Feature 1 evidence — context-arenas-and-limits

- **Branch:** `feature/step-14-simulation-context-initialization/context-arenas-and-limits` (squashed; squash
  commit recorded by the feature-2 row update).
- **Scope delivered:** the `mocassin-solver` `context/` module layout (`mod`, `dimensions`, `error`, `goals`,
  `jumps`, `lattice`, `limits`, `links`, `pools`), the `Dimensions`/`EnvId` index and `i64`-`rem_euclid` wrap
  contract, `ContextLimits`, the phase-0 `MemoryPlan` with checked `u64` arithmetic and `LimitExceeded`/exit 12,
  `MemoryReport` (`plan == report` on planned lengths; capacity is a diagnostic), the particle/flag/mobile-tracker
  arenas, the zeroed counter block, the empty link/cluster/pool/jump containers, the phased pipeline with
  boundary and 4096-environment cancellation, the error/exit mapping, and the `Send`/`!Sync` marker.
- **Cargo:** `mocassin-msl` moved to `[dev-dependencies]`; `mocassin-state`, `mocassin-extension`, and `rayon`
  removed as designed in §22. No new external crate.
- **Tests:** index/wrap round-trip (including `i32::MIN`/`i32::MAX` offsets), dimension overflow and the
  position/particle/total caps, `plan == report` equality, the ascending phase trace, pre-set cancellation,
  the arena container sizes (`Link` 28 B, `ClusterInstance` 16 B, `PoolMember` 8 B), the exit mapping, and the
  `msl-v1` ceria job (6×6×6, P=4, pc=5, 864 environments) building the skeleton context with flags, sequential
  mobile tracker ids, zeroed counters, empty later-feature containers, and a matching memory report.
- **Known limitations / deferred to later features:** links, clusters, energy vectors, pools, jump status,
  goals/normalization/time `dt`, the tracker index space, and resume are type shells or absent; `MemoryPlan`
  currently plans the lattice/environment and counter arrays and each later feature appends its own arrays and
  enforces its own caps; `ContextOptions` currently carries only `limits` (feature 4 adds
  `build_jump_status`/`apply_optimizers`, feature 5 adds `resume`). No public Rust API break in this feature
  (`DirectionPool`/`JumpLink` are untouched until feature 4). No new fixture and no `.msl`/manifest/signature
  change.

### Feature 2 evidence — link-csr-and-clusters

- **Branch:** `feature/step-14-simulation-context-initialization/link-csr-and-clusters` (squashed; squash commit
  recorded by the feature-3 row update).
- **Scope delivered:**
  - **Links:** the owner-major per-P1 forward CSR with the stable `(pair_id, partner_position, offset)` sort, the
    compact per-P1 inverse routing index, and `i64` `rem_euclid` partner resolution. `partner_of`,
    `incoming_owner`, `owner_link`, and `linked(a, b)` (either-direction membership, never a synthesised reverse
    link) are exposed. Duplicate periodic images are kept; a link's inverse entry is its exact forward entry.
  - **Clusters:** one instance per slot-0 link (`K`), the centre CSR, flat slot geometry, slot 0 = the centre
    `[0,0,0]`, candidate-position resolution with the multi-centre member-site fallback, and fail-closed
    `ClusterGeometry` for a slot-0 site span, a non-contiguous or duplicated slot, an absent or ambiguous member
    image, the same link in two slots, or more than 9 slots.
  - **Codes:** the raw 6-bit slot tuple (slot 0 least significant) is resolved only with
    `ClusterTable::lookup`: `Canonical(index)` is stored, `MissingFromMap` is `MissingOccupationCode`, and
    `ExpansionMapAbsent` is the distinct recompile-required error (both exit 12). No solver-side symmetry, tuple
    sorting, or direct `occupation_codes` indexing. The context exposes `cluster_lookup`, `cluster_row`,
    `cluster_code`, `cluster_code_id`, and the allocation-free `occupation_code`. The per-instance energy-row
    prefix (`energy_start`) and the zeroed energy array are allocated; feature 3 fills the values.
  - **Plan/caps:** `MemoryPlan` adds the forward/inverse link arrays and associations and every cluster array,
    enforcing `max_pair_links`, `max_links_per_position`, `max_cluster_tables`, `max_cluster_instances`,
    `max_link_bytes`, `max_cluster_state_bytes`, `max_cluster_energy_bytes`, and the `max_context_bytes` total
    before the first allocation. The invariant sweep now compares every planned length with the built array and
    re-checks the cluster associations (owner = centre, slot range, geometry).
- **Invariant/fixture matrix:**

| Fixture / case | Assertion |
| --- | --- |
| `msl-v1` ceria | offset arrays built, zero links/instances, plan/report 5508 B |
| `step-13-centered` | 8 forward/inverse links equal the brute-force payload expansion; deterministic order; no cluster; 996 B |
| `step-13-populated` KMC/MMC | 6 links, 6 inverse, three incoming at P0, both P2 duplicate images kept and sorted by table then offset; one cluster instance `[P1, P0, P2]` with zero offsets; 2 associations; initial code 4160 with no cluster row, `4161→0` and `4162→1`; 16 zeroed energy rows; MMC links/clusters equal KMC; 940 B |
| `step-11-amend-2` | 10 links; one instance `[P0, P4, P1]`, slot cells `[0, [-1,0,0], 0]`; all eight raw assignments resolve `8322→0, 8323→1, 8386→2, 8387→3, 12418→2, 12419→3, 12482→4, 12483→5`; initial 8320 has no row; 1204 B |
| `amend-2-pre-map` | initializes because the reached tuples are canonical; `8322/12418` resolve and `8386/8387` return `ExpansionMapAbsent`; a mutated lattice whose reached tuple is `8386` fails at initialization; removing a tuple from a present map returns `MissingOccupationCode` |
| Synthetic multi-centre (the real centered model, no new package) | four slot-0 links give four instances `[P0,P1]`, `[P3,P2]`, `[P4,P5]`, `[P7,P6]`; the member-site fallback resolves the rotated images; centre CSR; four associations; codes 65 with canonical id 0 |
| Hostile geometry / offsets | slot-0 site span, non-contiguous slot, duplicate slot, zero member link, ambiguous member link, slot 9, two slots on one link, missing table, and `i32::MIN`/`i32::MAX` offsets are all handled without panic (geometry fails closed; forward and inverse offsets wrap exactly, including the widened `-i32::MIN` inverse subtraction) |
| Asymmetric table / missing reverse link | the forward link keeps the owner's table and row orientation; no reverse link is synthesised; `linked` is true only when a real link exists in either direction |
| Shared forward link (synthetic two-table case) | one forward link carries associations from two instances; every association range is contiguous, grouped by link, and names the right instance/slot |
| Determinism / caps | two initializations produce identical links and clusters; each link/cluster cap rejects with the documented `LimitExceeded` label, value, and limit before allocation |

- **Plan arithmetic:** populated 940 B, `step-11-amend-2`/`amend-2-pre-map` 1204 B, `step-13-centered` 996 B,
  `msl-v1` 5508 B; `MemoryReport` equals the plan on every entry and `capacity_bytes ≥ total_bytes`.
- **Clarifications/limitations (recorded):**
  - **D2-1 — unmapped centre particle.** `particle_table_mapping` is the allowed centre-particle list; a
    particle outside it (void included) has no cluster row (data contract §3.3). `code_ids` stores
    `NO_CLUSTER_CODE` (`u32::MAX`) for such a current centre particle, `cluster_row` returns `None`, and
    `cluster_lookup` is defined only for a mapped centre particle (an unmapped one is an internal contract
    violation, exit 13). This is required by the populated/amend-2 fixtures, whose centre is void at
    initialization, and changes no format or physics.
  - **D2-2 — initial-check scope.** Initialization resolves the current tuple for every instance/cell and the
    tuple for every allowed centre particle with the actual member particles (the §6.3 scope). It does not
    enumerate the complete allowed tuple product; a pre-map package whose reached tuples are canonical
    initializes (the pre-map fixture does), and a non-canonical reached tuple fails at initialization (mutated
    lattice test) or through `cluster_lookup` (direct eight-code tests). The reader's expansion-map budget still
    bounds a hostile map, and no solver-side symmetry exists.
  - **D2-3 — instance order.** Instances are ordered by ascending centre position, then table index; `centres`
    therefore lists `0..K` grouped by position. The order is deterministic and fixes the assoc/code order.
  - **D2-4 — zeroed energy array.** Feature 2 reserves the per-instance cluster energy array (zeroed) so the
    plan, report, and cap are complete and feature 3 performs no reallocation; the values are feature 3's.
- **No new fixture, format, manifest, signature, or public API break.** `DirectionPool`/`JumpLink` are untouched
  until feature 4; the context accessors are additive.

### Feature 3 evidence — energy-runtime-and-initial-energy

- **Branch:** `feature/step-14-simulation-context-initialization/energy-runtime-and-initial-energy` (squashed;
  squash commit recorded by the feature-4 row update).
- **Scope delivered:**
  - **Energy runtime:** `ev_to_kt = 1/(k_B_eV·T)`, `kT_eV`, `field_scale = |E|·1e-10/kT_eV`, the job's field
    direction, the unit-cell site table `[P][pc]`, and the per-environment site+pair vectors `[env][pc]`, all in
    kT. The temperature/kT/factor/field checks are `NonFiniteFactor` (exit 13); table lengths and used values are
    `InvalidInput` (exit 12).
  - **Exact total:** `U = Σ_stable site + 0.5·Σ_stable (vector − site) + Σ_instances Σ_cells cluster(centre)`
    (Step 14 §7.4), stored as `lattice_energy_ev = U_kT · kT_eV`. The cluster term is instance × cell at the
    centre particle, never folded into the site+pair vector and never halved.
  - **Cluster energies:** the per-instance array is filled from the canonical code (slot 0 = the mapped centre
    particle, members from the current geometry) through `ClusterTable::lookup`; a non-canonical tuple uses the
    approved expansion map's canonical energy. A particle outside `particle_table_mapping` contributes zero.
  - **APIs:** `node_energy(EnvId, ParticleId)`, `site_energy(PositionId, ParticleId)`,
    `cluster_energy(instance, cell, ParticleId)`, `field_scale`, `field_direction`, `cell_vectors`,
    `ev_to_kt`, `kt_ev`, and the independent `recompute_lattice_energy_kt` plus the debug-only
    `check_lattice_energy` consistency assertion.
  - **Plan:** the `energy.site` and `energy.vectors` entries and the `max_energy_vector_bytes` cap; every
    planned length is checked against the built array in the invariant sweep.
  - No cluster-geometry or lookup change; features 4–5 (pools/jump status, goals/resume) remain.
- **Energy convention / fixture matrix:**

| Case | Assertion |
| --- | --- |
| Toy site+pair (2 cells, P=1, pc=2) | site 2 eV, pair A–A 3 eV, two stable environments: `U = 4 + 0.5·6 = 7 eV`; `node_energy = 5 kT`, `site_energy = 2 kT`; the recomputation agrees; `field_scale` matches its formula; the identity cell vectors and direction are exposed |
| Toy cluster | a two-slot cluster with 10 eV per centre adds 10 eV in each of the two cells: `U = 27 eV`; `cluster_energy` is 10 kT for the mapped centre and 0 for void |
| Real fixtures | `msl-v1` 0 eV; `step-13-centered` 0 eV; `step-13-populated` −3.2 eV (its non-zero site background over 8 cells × (P0 −0.5 + P2 +0.1)); `step-11-amend-2` −8.0 eV (P1/P4 −0.5 each per cell); the independent recomputation agrees in every case |
| Mutated populated (non-zero site/pair/cluster) | every `node_energy` equals a brute-force site + owner-link pair sum; every `cluster_energy` row equals the mapped table value; `lattice_energy_ev` equals the direct site + 0.5·pair + cluster sum |
| Amend-2 non-canonical tuple | centre 2 with members (3,2) → raw 8386 → canonical index 2 → 3 kT; centre 3 → 8387 → index 3 → 4 kT; void centre → 0; total 20 eV |
| Determinism | two initializations produce identical `EnergyRuntime` and totals |
| Cap / non-finite | `energy.site` + `energy.vectors` = 864 B for populated; a cap of 863 rejects with exit 12 and 864 is accepted; `T = 0`/∞, infinite field, and a NaN direction each return `NonFiniteFactor` (exit 13) |
- **Memory plan:** populated 1804 B, `step-11-amend-2`/`amend-2-pre-map` 2644 B, `step-13-centered` 3300 B,
  `msl-v1` 40228 B. The two new entries are `energy.site` (P·pc·8 B) and `energy.vectors` (A·B·C·P·pc·8 B),
  summed and capped by `max_energy_vector_bytes` (default 512 MiB) before allocation. Feature 2's recorded
  totals (940/1204/996/5508 B) are its commit-time values; feature 3 adds these two entries.
- **Clarifications/limitations (recorded):**
  - **D3-1 — no converted table copies.** The audited memory plan has no pair/cluster table copies, so the model
    keeps its eV tables. The context computes the factor once and stores every precomputed value (site, vectors,
    cluster rows, total) in kT; `ev_to_kt()` is exposed so Step 15 scales a model eV delta exactly once per
    delta. This is the audited plan's allocation set, and the physics is unchanged.
  - **D3-2 — the static total has no field term.** §7.4 defines the lattice energy without the field; the
    context supplies `field_scale`/`field_direction` for Step 15's per-rule transition term
    (`-0.5·field_scale·(transport·ê)`), which is not a static site/pair/cluster contribution.
  - **D3-3 — unmapped centre particles.** A centre particle outside `particle_table_mapping` (void included) has
    no cluster row, so `cluster_energy` returns 0 (consistent with D2-1).
  - **D3-4 — non-finite used table values.** The reader already rejects NaN/Inf table values; a synthetic model
    that slips one through is rejected as `InvalidInput` (exit 12) when the value is used, never silently
    propagated.
- **No new fixture, format, manifest, signature, or public API break.**

### Feature 4 evidence — selection-pools-and-jump-status

- **Branch:** `feature/step-14-simulation-context-initialization/selection-pools-and-jump-status` (squashed;
  squash commit recorded by the feature-5 row update).
- **Scope delivered:**
  - **Selection pools:** `SelectionPools` with supercell `PoolMember { env: EnvId, particle }`, a global
    `slot[env·pc + particle]`, a P1 `pool_of[P·pc + particle]`, ascending direction-count pools, and
    `selectable_jump_count`. Members are inserted in ascending environment order; each pool is created with the
    capacity of every compiled-selectable `(environment, particle)` combination that could join it, so
    `set_selectable` never reallocates. The selection mask filters the combinations; the job's optimizers remove
    their `(P1, particle)` members after construction while leaving `pool_of` for re-entry; an MMC context has no
    pools; KMC with no selectable combination returns `NoSelectableJumps`.
  - **Direction filtering:** the `.msl` `jump_count`/`jump_mapping` are a union over all collections (the
    populated fixture mixes KMC and MMC), so the pool direction count, `direction_for`, and the jump-status build
    use only the job simulation's active collections. `direction_is_active` exposes the check; a KMC job never
    offers an MMC direction.
  - **Jump status:** receiver-major per-`(P1, direction)` CSR (`S = P·D`, `status = direction·P + position`),
    built only for active directions (inactive statuses are empty). The receiver filter is the compiled
    structural site stability, not the initial particle; a jump link is emitted only when the receiver's sorted
    forward list has a link whose resolved partner is the sender (first sorted link wins); the link carries the
    receiver's cluster associations. The per-status bound is `L·(L−1) ≤ 90`; the path is at most `max_path_nodes`.
    `resolve_node`/`resolve_path` replace node 0 with the selected bucket and wrap the relative cell offsets;
    `path_len`, `allowed_mask_of`, `rule`, and `collection_rules` complete the Step 16 interface.
  - **Plan/caps:** `pools.headers/members/slot/pool_of` (members planned as reserved capacity) with `max_pools`
    and `max_pool_bytes`; `jumps.offsets/links` added by the counting pass before allocation, with
    `max_status_count`, `max_jump_links`, and `max_jump_status_bytes`. The final plan is stored and the report
    matches it; the invariant sweep checks pool/member/slot consistency, status coverage, receiver links, the
    structural filter, and every planned length.
- **Public Rust API break (no format change):** `mocassin_types::DirectionPool` and its module/exports/tests are
  removed; `mocassin_types::JumpLink` changes from `{ sender_path_id: i32, link_id: i32 }` to
  `{ sender_path_id: u8, receiver_path_id: u8, link_id: u32 }` (8 bytes). `JumpStatus` remains. No reader
  constructed the removed/changed container; the Step 13 page records the retirement.
- **Invariants / fixtures / caps tests:**

| Case | Assertion |
| --- | --- |
| `step-13-populated` KMC | one active direction per key (the MMC direction is filtered out); 1 pool of count 1, 16 members, `selectable_jump_count = 16`; the brute-force selectable count agrees; `direction_for(P0, O, 0) = dir0` and relative 1 is `None`; `direction_for` never returns the MMC dir 2 |
| `step-11-amend-2` KMC | one pool of count 2, 16 members, selectable 32; relative 0/1 map to the two active directions and relative 2 is `None` |
| Populated/centered MMC | pools empty, selectable 0, jump status empty |
| Mask / optimizer | a changed mask removes the affected `(P1, particle)`; an optimizer removes every cell member but `pool_of` still allows `set_selectable` re-entry; the count/slot maps agree |
| `set_selectable` | a particle change remove/adds the member, swap-remove updates the moved member's slot, the pool capacities/pointers do not change across 200 changes, and the count follows |
| Multi-cell | members in different cells have distinct `slot` keys and the same P1 `pool_of` |
| Jump status | for populated/amend-2 every active status equals a naive brute-force recomputation (sorted receiver link, structural filter); inactive statuses are empty; total links 4 (populated) and 8 (amend-2) |
| Structural filter / clusters | P1 is unstable at init and excluded; setting it stable includes its void receiver; the receiver link carries the cluster associations (slot 1/2) without duplicating them in the jump link |
| `resolve_node` | node 0 is the selected bucket; node cells wrap; the centered cross-bucket direction 3 at selected P2 resolves node 1 to P7; an out-of-range node is `None`; `resolve_path` fills a caller slice |
| Ten-node bound | a synthetic 10-position, one-cell model with one 10-node direction yields exactly 90 jump links for status 0 (the `L·(L−1)` bound), all sender/receiver ids in `0..10` |
| Caps | `max_pools`/`max_pool_bytes` (value 720, limit 719), `max_selectable_jump_count` (16/15), `max_status_count` (12/11), `max_jump_links` (4/3), and `max_jump_status_bytes` (84/83) reject before allocation |
| Invalid path / no selectable | an out-of-cell path node and an 11-node path are `InvalidInput`; a job with every selection mask cleared is `NoSelectableJumps` (exit 12) |
- **Plan totals (report):** at the feature-4 commit: populated KMC 2608 B (pools 720 + jumps 84 added to the feature-3 1804), populated
  MMC 1804 B, `step-11-amend-2` 3800 B, `step-13-centered` 3300 B, `msl-v1` 64568 B; the report equals its
  entries and capacity ≥ total. Feature 5 adds the `trackers.mobile_positions` entry; see its evidence for
  the current totals.
- **Clarifications/limitations (recorded):**
  - **D4-1 — active-collection filtering.** The design's §8/§9 read `jump_count`/`jump_mapping` as the job's
    direction table, but the payload tables are a union over collected kinds; the context filters the selection
    and jump-status space to `simulation.jump_collection_ids`. Without this a KMC job would offer MMC directions.
    This is a correctness correction, not a format change; the populated fixture is the evidence.
  - **D4-2 — inactive statuses are empty.** The CSR keeps the global `direction·P + position` index space, but
    statuses of inactive directions are never filled; the plan still counts `S = P·D`.
  - **D4-3 — jump entries are planned at their phase.** `J` is only known after the receiver-link counting pass,
    so the jump entries are appended/updated in the plan after that pass and before the `links` vector is
    allocated; all caps (including the total) are checked then. The phase-0 plan contains zero jump entries.
  - **D4-4 — `set_selectable` semantics.** The method sets the membership for a `(environment, particle)` from
    the lattice's current particle at that environment: the arrived particle is inserted, the departed removed.
    Callers update the particle first; `set_particle` is the raw mutator (flags/tracker/pool refresh is Step 15's
    state application). The optimizer is a start condition and does not clear `pool_of`.
- **No new fixture, format, manifest, or signature change.** The public Rust API break is the placeholder
  retirement above.

### Feature 5 evidence — goals-normalization-and-resume

- **Branch:** `feature/step-14-simulation-context-initialization/goals-normalization-and-resume` (squashed;
  squash commit recorded by the feature-6 row update).
- **Scope delivered:**
  - **Goals and counters:** `mobile_count` is the number of non-void environments; for KMC `prerun =
    pre_run_mcsp × mobile_count`, `main = target_mcsp × mobile_count`, `total` rounded up to the next multiple of
    100, `cycles_per_block = total / 100`. Every product, the sum, and the round-up are checked `u64`; overflow is
    `InvalidInput { what: "goal overflow" }` (exit 12), a zero total is `InvalidInput { what: "total goal" }`
    (exit 12), and zero mobile is `NoMobileParticles` (exit 12) checked after the lattice (before pools, so the
    error is not masked by `NoSelectableJumps`). MMC goals are zero. Counters are zeroed fresh and restored on
    resume (`per_particle`, `cycles`, `mcs`).
  - **Tracker index space:** KMC `mobile_positions[id] = environment` (the inverse of the sequential mobile ids),
    `static_trackers_per_cell`, `static_tracker_count = cells × per_cell` (checked against `u32`), the global
    tracker count, the read-only static/global mappings, and `static_tracker_id(env, particle)`; MMC has an empty
    index space. Step 18 owns tracker values.
  - **Normalization/time:** `K_fix = 1` or `exp(E_norm/kT)`, `K_auto = 1/RawMax` while positive, `K_norm =
    K_fix × K_auto` with the `K_fix` fallback, `nu_max` from the job cap or the maximum active-rule frequency,
    `dof = 2` with `use_dual_dof_correction`, and `dt = dof × K_norm / (nu_max × selectable_count)`. Non-finite
    `K_fix`/`K_norm`/`nu_max`/`dt` are `NonFiniteFactor` (exit 13). MMC uses unit factors, no `nu_max`/`dt`, and
    time 0.
  - **RNG/time/origin:** a fresh `Rng::from_seed(job.seed)` at time 0 (`ContextOrigin::Fresh`), or the exact
    `Rng::from_state` stream and restored simulated time (`ContextOrigin::Resumed`); a bad stream is
    `ContextError::RngState` (exit 13). `state_flags` is stored opaquely (0 fresh).
  - **Resume:** `ResumeState`/`ResumePhase` with the parameterized `model_signature`, recomputed `seed_hash`, job
    id/kind/dimensions/counts, per-particle counters, cycles/mcs, time, normalization, RNG blob, state flags, and
    optional mobile mapping. The resumed particles drive the lattice flags, tracker index, pools, energy vectors,
    cluster codes/energies, goals, and initial lattice energy; `job.lattice` remains the package lattice.
    `AlreadyCompleted` is returned for a completed state (with the job's computed total goal) after full
    validation. Every binding/value mismatch is `ResumeMismatch` (exit 12); the mobile mapping is re-derived and
    compared when present.
  - **Plan:** `trackers.mobile_positions` is updated after the lattice before its allocation; `plan == report`
    and the invariant sweep checks goals, tracker bounds, normalization finiteness/`dt`, time, and the MMC
    zero-time/goal rules.
- **Goals/time/resume test matrix:**

| Case | Assertion |
| --- | --- |
| Populated KMC | mobile 16; goals 800/1600/2400, block 24; counters zeroed; `K_fix=K_auto=K_norm=1`, `RawMax=0`, `nu_max=1e13`, `dof=2`, `dt=1.25e-14`, time 0, origin Fresh |
| `msl-v1` ceria 6×6×6 job 1 (§17 acceptance) | mobile 648, selectable 432, goals 162000/97200/259200, block 2592, `dof=2`, `nu_max=1e13`, `dt=2/(1e13·432)` |
| Round-up | `target=1, pre=0` → main 16, total 100, block 1; `pre=3, target=4` → total 200/block 2 |
| Zero/overflow/mobile | zero total → `total goal`; `target=u64::MAX` or `pre=u64::MAX` → `goal overflow`; an all-void lattice → `NoMobileParticles` (exit 12) |
| Normalization | `E_norm=0.05` gives `K_fix=exp(0.05/kT)` and the matching `dt`; a job cap 2e13 overrides `nu_max`; zero/NaN/negative caps and all-zero active rule frequencies → `NonFiniteFactor "nu_max"`; `E_norm=f64::MAX` → `K_fix` (exit 13) |
| MMC | goals zero, unit factors, `nu_max=1`, `dof=1`, `dt=0`, time 0, no tracker index space |
| Trackers | the mobile mapping is the exact inverse of the mobile ids; void environments have none; static count = cells × 9 and the static id formula; global count/mappings exposed; the plan entry is the mobile count |
| Resume exact | counters/cycles/mcs/time/normalization/RawMax/state flags preserved; the lattice, mobile mapping, goals, energy, and RNG next draw equal the fresh context; `K_norm` preserved with a custom `RawMax=8` |
| Resumed lattice | a changed resumed particle (env0 void) drives 15 mobile environments, 15 pool members, goals 750/1500/2300/23, recomputed energy, and a 15-element plan entry; the job's initial lattice is unchanged |
| Completed | `AlreadyCompleted { mcs, total_goal }` with the job's computed total goal and no context |
| Mismatches | model signature, seed hash, job id, kind, lattice size, position/particle counts, particle array length/index, time NaN/negative, counter length, `K_fix`/`K_auto`/`K_norm`/`RawMax`, and mobile mapping all reject with `ResumeMismatch` (exit 12); a completed state still validates the mapping |
| MMC resume | time is forced to 0, goals zero, unit normalization, no tracker index space |
- **Plan totals (report):** populated KMC 2672 B (the feature-4 2608 + 16 mobile positions × 4), populated MMC
  1804 B, `step-11-amend-2` 3928 B, `step-13-centered` 3300 B, `msl-v1` 67160 B; report equals its entries and
  capacity ≥ total.
- **Clarifications/limitations (recorded):**
  - **D5-1 — per-particle resume counters.** The design's `ResumeState.counters: ParticleCounters` is read as the
    per-particle counter array (`Vec<ParticleCounters>`) because `.mcs` stores one counter entry per particle
    (data contract §4.4); `cycles`/`mcs` remain separate. No format change.
  - **D5-2 — a void cluster member has no row.** The `.msl` occupation table enumerates only the site's allowed
    (non-void) particles; a cluster cell with a void member has no canonical code. Its `cluster_code_id` is
    `None` and every energy row for that cell is 0 (the empty-cluster contribution), instead of failing the
    initialization. This is physically the empty cluster and is exposed by the resumed-lattice test; Step 15 must
    skip `cluster_lookup`/use the zero row for a void-member tuple. The amendment-2 fail-closed behavior for
    non-canonical **non-void** tuples is unchanged.
  - **D5-3 — resume drives state-dependent construction.** The resumed particles are used from the lattice phase
    onward; the context's `job.lattice` remains the package lattice. This is why pools/energy/cluster codes/goals
    match the resumed state.
  - **D5-4 — tracker index is KMC-only.** MMC has empty/zero tracker index space, matching §10; the lattice's
    mobile ids still exist internally for the generic arena.
  - **D5-5 — `nu_max` classification.** A zero/NaN `nu_max` (job cap or active-rule maximum) is
    `NonFiniteFactor` (exit 13), not a data error, per §11.1.
  - **D5-6 — opaque state flags and API adjustment.** `state_flags` is exposed as a context field for Step 18
    (0 fresh, restored on resume) because §12 says it is stored opaquely and Step 14 never interprets it. The
    state-aware builders (`LatticeState::build`, `ClusterArena::build/build_code`, `ClusterArena::occupation_code`)
    now take the particle slice; the public `SimulationContext::occupation_code(instance, cell, overrides)`
    signature is unchanged. The static tracker count is checked against `u32` and fails closed
    (`LimitExceeded { what: "static trackers" }`) for a supercell whose expanded count cannot be addressed.
- **No new fixture, format, manifest, or signature change.**

### Feature 6 evidence — fixtures-benchmarks-and-closeout

- **Branch:** `feature/step-14-simulation-context-initialization/fixtures-benchmarks-and-closeout` (squashed as
  `00fceabce7fa6d910cb79f701f175aa4b681bf5f`).
- **Local gates (feature branch, exact commands):** `cargo fmt --manifest-path src/rust/Cargo.toml --all
  --check` → OK; `cargo clippy --manifest-path src/rust/Cargo.toml --workspace --all-targets -- -D warnings` →
  OK; `cargo test --manifest-path src/rust/Cargo.toml --workspace` → all test binaries pass (including
  `context_fixtures` 3, `allocation` 1, `context_clusters` 17, `context_links` 13, `context_jumps` 10,
  `context_pools` 10, `context_goals` 15, `context_resume` 7, `context_energy` 8, `context_skeleton` 9, and the
  `Send`/`!Sync` doctests); `dotnet format Mocassin.slnx --verify-no-changes` → OK; `dotnet test
  src/dotnet/Mocassin.Results.Tests/Mocassin.Results.Tests.csproj -c Release --filter
  "FullyQualifiedName~Step14CeriaFixtureTests|FullyQualifiedName~Step14MultiCentreFixtureTests"` → 12 passed;
  `./tools/verify.sh docs --require` → exit 0; `./tools/verify.sh structure --require` → 7 passed. The full
  `./tools/verify.sh all --require` and the single closeout CI run are recorded at closeout.
- **New fixtures (real .NET writer, fixed `created_utc = 2026-11-21T12:00:00Z`):**
  - [`tests/contracts/packages/step-14-ceria-10x10x10`](../../../tests/contracts/packages/step-14-ceria-10x10x10/README.md)
    — derived from `samples/step-14-ceria-10x10x10/` (a copy of the job sweep with one KMC job at 10×10×10).
    5 particles, 4 P1 positions, 4 sites, no pair/cluster tables, 4,000 environments; `.msl` 98,304 B, SHA-256
    `0865dd75...`, summary 2,026 B, payloads 3,475/4,061/530 B. The model signature is the msl-v1 ceria model.
  - [`tests/contracts/packages/step-14-multi-centre-cluster`](../../../tests/contracts/packages/step-14-multi-centre-cluster/README.md)
    — derived from `samples/step-14-multi-centre-cluster/` (the centred sample plus a third site C and an
    A-centred two-member cluster); `step-13-centered` stays byte-identical. 4 particles, 12 P1 positions, 3
    sites, 12 pair tables, 1 cluster table, 1 MMC job; `.msl` 98,304 B, SHA-256 `ace6f38e...`, summary 1,960 B.
    The writer emits four slot-0 links (one per A position), so the context builds **four instances**; the Rust
    test asserts the per-instance slots/codes and a per-instance code→energy mapping with a synthetic
    distinct-energy table.
  - **Model-validator deviation (recorded):** the project model's `ENM008` requires 2–8 surrounding offsets, so
    the design's one-member cluster cannot be authored. The fixture uses two **distinct** member sites (B and C),
    not two images of one member site (the rejected OD-1 ambiguity); this still exercises the multi-centre
    member-site fallback and one instance per slot-0 link. `step-13-centered` is unchanged; the deviation is
    fixture-only and is recorded for the audit.
- **Context-summary snapshot / determinism:** `tests/context_fixtures.rs` snapshots the 10×10×10 summary
  (kind, cell, counts, mobile/selectable, link histogram, cluster sizes, site/vector sums, total energy, pool
  sizes, jump sizes, goals, normalization, trackers, time/origin/flags) and asserts two initializations have an
  identical summary, memory report, energy, goals, normalization, RNG state, and flags. The committed snapshot is
  `tests/snapshots/context_fixtures__step14_ceria_10x10x10.snap`.
- **Allocation proof:** `tests/allocation.rs` (own test binary, counting global allocator). The 10×10×10 build
  makes **98 allocations / 316,872 B** against a **308,632 B** plan (capacity equal to the plan); the populated
  2×2×2 build makes 103 / 11,861 B against a 2,672 B plan. After construction the hot accessors
  (`node_energy`, `site_energy`, `cluster_energy`, `cluster_code`, `link_range`, `incoming`, `jump_links`,
  `mobile_positions`, mappings, `dt`, goals, normalization) and 200 pool `set_selectable` particle-change cycles
  allocate **zero**; the pool capacities/pointers are unchanged. The test process peak RSS is 11,060 KiB
  (`VmHWM`, non-gating context next to the memory report).
- **Benchmark:** `benches/context.rs` (Criterion, release) measures the real-writer 10×10×10 fixture at
  **75.565 µs** (CI 74.908–76.269) and the populated clone at **315.26 µs** (313.18–317.73); memory plans
  **308,632 B** and **234,832 B** (capacities 308,632/234,864). Both are far below the `< 100 ms`/`≤ 64 MiB`
  budgets. Evidence: `tests/reference/bench/step-14-context.json`, raw log
  `step-14-context-2026-11-21.txt`, `rustContext` in `baselines.json` (runner class
  `linux-x64-local-container-amd-ryzen-7-7840u-rust-1.99.1`).
- **Documentation:** `solver-architecture.md` (implementation evidence, active directions, void-member rule),
  `physics/energies.md` (void-member zero contribution), `performance/index.md` (context bench/allocation
  numbers), `troubleshooting/errors.md` (context failures, exit 12/13/15 vs 14), `testing-and-parity.md`
  (fixtures/tiers/allocation/snapshot), and `msl-format.md` (cluster geometry and void-member rule). The parity
  matrix and status dashboard are updated; the Step 15 void-member rule is recorded.
- **Closeout:** merged current `main` (`929327e`) as `8c09a8c`; `./tools/verify.sh all --require` exited **0** at
  `8c09a8c` (2,007 .NET tests, the Rust workspace tests including `context_fixtures`/`allocation`, Python 3
  passed and sdist/wheel built, docs links 196 files + Zensical strict + DocFX 0 errors + rustdoc clean,
  contracts/policy, and the AOT evidence: the real CLI publish fails 62 IL2xxx/IL3xxx diagnostics and is
  recorded as non-gating). The single Ubuntu closeout run
  [44922414484](https://github.com/seb-eis/mocassin-2/actions/runs/44922414484) is green at `3fd4516`; the
  audited commit and run are recorded in `docs/planning/closeout/step-14.json`; the audit request is
  `docs/planning/audits/step-14.md`.

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

Corrected in this step (each with a test and a recorded deviation, §14): the link graph is translation-compact and
owner-major with an inverse routing index; cluster energy is counted once at its centre; the site background is
counted once; a multi-member cluster change rebuilds the code from the final state; the jump-status receiver
filter is structural rather than initial-occupation-based; and the jump-status bound is derived, not a magic 56.

## Scope

**In scope**

- The `mocassin-solver` context module: `SimulationContext`, `ContextOptions`, `ContextLimits`, `MemoryPlan`,
  `ContextError`, `InitOutcome`, `EnvId`, and the accessors Steps 15–18 consume.
- Lattice and environment state arenas (particles, flags, mobile tracker ids) and the index/wrap helpers.
- The owner-major per-P1 neighbour-link CSR, the inverse routing index, partner resolution, deterministic
  sorting, and the per-link cluster association.
- The cluster runtime: one instance per slot-0 link, member-site validation (slot 0 = `[0,0,0]`, candidate
  position or unique member-site image for slots `1..n`), centre index, occupation codes, code ids,
  per-instance energies, and the v1 cluster-geometry rule (§6.4).
- The energy runtime: the eV→kT factor, the site table, the per-owner site+pair energy vectors, the
  per-instance cluster energy vectors, and the exact initial lattice energy.
- Selection pools (supercell-addressed, direction-count buckets), selection-mask filtering, optimizer
  application, the selectable count, and the deterministic add/remove helpers.
- KMC jump status: per-`(P1 position, direction)` receiver-major links, the derived bound,
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
    pub links: LinkArena,              // owner-major per-P1 CSR + the inverse routing index (§6.1)
    pub clusters: ClusterArena,        // instances, centre index, codes, energies (§6.3)
    pub energy: EnergyRuntime,         // site table, site+pair vectors, kT factor, field scale
    pub pools: SelectionPools,         // KMC only; supercell-addressed (§8); empty for MMC
    pub jumps: JumpRuntime,            // KMC only; receiver-major links (§9); empty for MMC
    pub trackers: TrackerIndex,        // index space + mobile mapping; no values
    pub counters: Counters,            // zeroed; Step 16/17 increment
    pub goals: Goals,
    pub normalization: Normalization,
    pub rng: Rng,
    pub time_s: f64,                   // 0 fresh; restored on resume; 0 for MMC
    pub lattice_energy_ev: f64,        // exact initial total (§7.4)
    pub origin: ContextOrigin,
    pub limits: ContextLimits,
    /// Explicit `!Sync` marker. Every stored field is `Send + Sync`, so without this the compiler would derive
    /// `Sync` and the single-trajectory rule would be documentation only. `Cell<()>` is `Send` and `!Sync`.
    _not_sync: PhantomData<Cell<()>>,
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
- Construction is phased and each phase is validated before the next: **memory plan** → input validation →
  lattice → links (forward + inverse) → clusters → energy vectors → initial energy → pools → jump status →
  tracker index → goals/counters/normalization → RNG/time → invariant sweep. A failed phase returns a
  structured error naming the phase and the offending index; no partial context escapes.
- `Box<SimulationContext>` keeps the large struct out of the enum and off the stack. The type is `Send` and
  explicitly **not `Sync`**: every stored field is `Send + Sync`, so the `_not_sync: PhantomData<Cell<()>>`
  marker is what enforces the single-trajectory rule instead of leaving it as a comment. The marker is asserted
  by a `compile_fail` doctest (a `Sync` bound must not compile) and a positive `Send` assertion.
- `EnvId` is a dense `u32` newtype for an **absolute supercell environment** (`0..A*B*C*P`). `PositionId` stays
  the canonical P1 index. The two are never interchanged: pools, jump links, energy-vector accessors, and the
  link/partner resolution use `EnvId`; P1-position-keyed tables (site table, link owner CSR, `jump_count`,
  masks) use `PositionId`.

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
- `ContextLimits` (public, overridable library limits; defaults below) is validated **before** allocation.

**Phase 0 — the memory plan.** Before the first `Vec` is created, `MemoryPlan::plan(&job, &limits)` computes the
element count and byte size of **every** array with checked `u64` arithmetic, compares each array with its cap and
the sum with `max_context_bytes`, and returns `ContextError::LimitExceeded { what, value, limit }` on the first
violation. Only a passing plan is materialised. `MemoryReport` reports each array's **planned length** times
`size_of` (not `Vec::capacity`, which allocator rounding may exceed); `plan == report` is therefore an exact
equality on lengths and is checked by a debug assertion and a PR-fast test. The summed capacity bytes are recorded
as a separate diagnostic (`capacity_bytes ≥ plan_bytes`) and the benchmark records the process peak RSS;
`max_context_bytes` bounds the planned derived arrays, not RSS. A writer-valid package therefore cannot allocate
past the caps and discover it afterwards; the earlier draft bounded only the site+pair energy and the links and
left `cluster_table_count × A*B*C` and `P*D+1` unchecked.

Per-array derivation (all products checked; `E = A*B*C*P`, `C = A*B*C`, `K` = validated cluster instances = the
number of slot-0 links, `N` = payload pair links, `S = P*D` statuses, `J` = jump links, `M` = selectable members,
`pool_count` = distinct selectable direction counts):

| Array | Elements | Bytes |
| --- | --- | ---: |
| `lattice.particles`, `flags` | `E` | `E`, `E` |
| `lattice.mobile_tracker` | `E` | `E*4` |
| `energy` (site+pair) | `E*pc` | `E*pc*8` |
| `site` | `P*pc` | `P*pc*8` |
| `links.owner_offsets`, `links.inverse_offsets` | `P+1` each | `(P+1)*4` each |
| `links.links` | `N` | `N*28` (`Link` = 4+12+4+4+2, padded to 28) |
| `links.inverse` | `N` | `N*8` (`IncomingLink` = 8) |
| `links.cluster_assoc` | `Σ_k (slots(k) - 1)` | `*8` (`ClusterAssoc { instance: u32, slot: u8 }`, padded to 8) |
| `clusters.instances` | `K` | `K*16` (`ClusterInstance { table, centre_position, slot_start: u32, slot_count: u8 }`) |
| `clusters.slot_start`, `slot_positions`, `slot_cells` | `K+1`, `Σ slots`, `Σ slots` | `(K+1)*4 + Σ slots*16` |
| `clusters.energy_start` | `K+1` | `(K+1)*4` (prefix of the per-instance `mapping_len`) |
| `clusters.codes`, `code_ids` | `K*C` | `K*C*8`, `K*C*4` |
| `clusters.energy` | `C*Σ_k mapping_len(k)` | `*8` (index `cell*Σ mapping_len + energy_start[i] + row`, not `K*C*pc`) |
| `clusters.centre_offsets`, `centres` | `P+1`, `K` | `(P+1)*4 + K*4` |
| `pools` (headers: `Vec<PoolMember>` 24 B + `direction_count` 4 B, padded) | `pool_count` | `pool_count*32` |
| `pools.members` | `M` | `M*8` (each pool reserved to its own count) |
| `pools.slot` | `E*pc` | `E*pc*4` |
| `pools.pool_of` | `P*pc` | `P*pc*4` |
| `jumps.offsets` | `S+1` | `(S+1)*4` |
| `jumps.links` | `J` | `J*8` (`JumpLink` = 8) |
| `counters` | `pc*6 + 2` u64 | `(pc*6 + 2)*8` |
| `trackers.mobile_positions` | `mobile_count` | `mobile_count*4` |

| Limit | Default | Reason |
| --- | ---: | --- |
| `max_positions` | 500,000 | Same envelope as `ReaderLimits::max_positions`; `A*B*C*P` and `≤ i32::MAX` re-checked |
| `max_particles` | 64 | 6-bit state codes and `u64` masks |
| `max_pair_links` | 16,000,000 | Reader envelope; bounds `N` |
| `max_links_per_position` | 65,536 | One P1 position's link vector |
| `max_cluster_tables` | 1,000,000 | Reader envelope |
| `max_cluster_instances` | 100,000 | Validated instances (`K`, the slot-0 link count); the product `K*C` is what the review's F-3 showed unbounded |
| `max_pools` | 65,536 | Distinct selectable direction counts; `pool_of` is `u32` with a checked bound |
| `max_selectable_jump_count` | `u32::MAX` | `Rng::next_bounded(u32)`; a larger count is a context limit error |
| `max_path_nodes` | 10 | Same as `StateCode::MAX_NODES` |

| Byte cap | Default | Bounds |
| --- | ---: | --- |
| `max_energy_vector_bytes` | 536,870,912 (512 MiB) | `energy` + `site` |
| `max_link_bytes` | 805,306,368 (768 MiB) | forward links + inverse + `cluster_assoc` |
| `max_cluster_state_bytes` | 268,435,456 (256 MiB) | every cluster array except `energy` (instance map/geometry, slot CSR, codes, code ids, centre index) |
| `max_cluster_energy_bytes` | 268,435,456 (256 MiB) | `clusters.energy` |
| `max_pool_bytes` | 268,435,456 (256 MiB) | members + slot map + `pool_of` |
| `max_status_count` | 4,000,000 | `P*D`; also bounds the O(`S·L²`) jump-status build |
| `max_jump_links` | 8,000,000 | `J` (the counting pass rejects before allocating) |
| `max_jump_status_bytes` | 268,435,456 (256 MiB) | `jumps.offsets` + `jumps.links` |
| `max_context_bytes` | 1,073,741,824 (1 GiB) | The **sum** of every planned array; the binding cap |

- All size arithmetic is checked; an overflow is `ContextError::LimitExceeded`, never a panic or a wrapped
  allocation. A limit violation is a **model/data error** (exit 12): the package is structurally valid but the
  job cannot be represented within the configured context limits. The byte caps are **per array**, their sum is
  larger than `max_context_bytes`, and the total cap is what a 500k-position job must satisfy; §20 states the
  budget accordingly.
- `max_context_bytes` is distinct from the architecture's exit 14 ("aborted by a configured limit"), which is a
  **run-time** abort (cycle target, wall clock, rate). A context that cannot be represented is detected before
  the run starts and is a data error (exit 12). See §13 and
  [`01-target-architecture.md`](../01-target-architecture.md) §7.3.
- Memory is reported, not guessed: `MemoryReport` returns per-array byte counts (lattice, flags, tracker ids,
  energy vectors, cluster arrays, links forward/inverse, pools, jump status, counters) and a total; the
  acceptance test asserts the total against the budget and the benchmark records the measured process peak RSS.

## 3. Lattice and environment states

- The particle array is `InitialLattice.particles` copied verbatim (already validated length, dimensions,
  particle range, and lattice signature by Step 13). Step 14 does not re-verify the signature.
- `STABLE` = `model.structure.positions[P].stable && particle != void`. The original uses `particle != void`
  only; the new rule additionally requires the compiled site stability. All v1 P1 positions carry the compiled
  flag; the difference is recorded in §14 and covered by a test with a synthetic position whose compiled
  stability is false.
- `MOBILE` = `particle != void && jump_count[P*pc + particle] >= 0`. Because `jump_count` is `u32`, the original's
  `-1` (static) and `0` (passive) collapse to zero; the distinction is not representable in the v1 payload and
  is not needed for the tracker rule. **Assumption recorded (review F-26):** every non-void environment is
  `MOBILE`, even when its particle is absent from every active KMC collection's `mobile_particle_mask`; the
  context does not consult that mask for `MOBILE` (it only bounds the mobile-tracker id space, matching the
  original's passive-inclusive behaviour). A fixture/test asserts that a non-void particle outside every mobile
  mask still gets a mobile tracker and that `mobile_count` counts it; Step 29 parity covers the assumption.
- The environment's `selection_particle_mask` and the position's `allowed_mask` are kept distinct: the mask
  governs pool membership (§8), the position mask governs Step 16's exact occupation check (§9.4). A package
  where they disagree is accepted as authored; the reader already bounded both.
- `update_particle_ids` is stored per position (CSR over P1 positions) and exposed read-only. It is **not** used
  for delta routing: the context computes every `pc` entry of each owner's site+pair vector, and the owner of an
  affected vector is found through the inverse link index (§6.1). The payload list is retained for a later
  optimisation only.
- Site stability, the interaction range `[maxCellA, maxCellB, maxCellC, 0]`, the cell vectors, the particle
  charges/symbols/vacancy flags, and the tracker counts are exposed read-only from the model.

## 4. Complexity class and split decision

**Class: High.** Recorded per [`04-roadmap.md` §3.1](../04-roadmap.md#31-step-sizing-and-split-triggers) and
mirrored in the roadmap's "Recorded class — Step 14" note.

- **Contracts and public surfaces:** no persisted format change, but a new public Rust API
  (`SimulationContext`, `ContextOptions`, `ContextLimits`, `ContextError`, `ResumeState`) that Steps 15, 16, 17,
  18, and 19 consume. A late change ripples into five later steps.
- **New dependencies:** none at the crate level. The cross-step dependency on `step-11.1` (`step-11-amend-2`
  cluster-code lookup) is recorded in §22: it is an approved contract with a running implementation, not a new
  crate or format decision for Step 14.
- **Cross-step interfaces:** five later steps, and the interface fixes the neighbour-link identity, the energy
  vector layout, the cluster-code representation, the pool order, and the jump-status link identity. This is the
  dimension that makes the step High.
- **Irreversibility:** the owner/partner link orientation and the inverse routing rule, the supercell pool key
  space, the pool ordering, the cluster-geometry rule (§6.4), and the site/pair/cluster accounting (§7.4) are
  scientific/behavioural conventions that are expensive to change after merge (fixtures, tests, Step 15's
  deltas, parity expectations).
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
0 plan every array (checked bytes, per-array caps, total cap) and reject early
1 validate inputs (dimensions, arrays, masks, links, tables, settings)
2 build lattice/environment states + tracker index space
3 build the owner-major per-P1 link CSR (sort, resolve) and the inverse routing index
4 validate cluster geometry, build instances/centres, attach cluster associations, initial codes
5 build the site table and per-environment site+pair vectors
6 build per-instance cluster codes and energy vectors
7 compute the exact initial lattice energy
8 build selection pools over absolute environments (KMC) and apply optimizers
9 build jump status (KMC): receiver-major jump links per (P, direction)
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

### 6.1 Link orientation, the owner-major CSR, and the inverse routing index

A payload pair link on canonical P1 position `O` is `{target_position, offset:[i32;3], pair_id}`. `O` is the
**owner** (the centre of the interaction) and the target is its **partner**; the pair table is row-major
`[owner particle][partner particle]` (`02-data-contracts.md` §3.3,
`mocassin-types::runtime::energy::PairTable`). Therefore

$$
E_\text{pair}(\text{owner}, p) = \sum_{l \in \mathrm{links}(\text{owner})}
  \texttt{pair\_table}[l.\texttt{pair\_id}][p][\text{particle at } \mathrm{partner}(l)]
$$

and a change at a partner **V** updates the vectors of the **owners** whose link points at V — never the other
way round. The owner's link set is the property each vector sums, so the forward CSR must be owner-major, and a
second, compact structure is needed to answer "who owns a link to V". Both are per P1 position, so neither grows
with the supercell:

```rust
pub struct LinkArena {
    pub owner_offsets: Vec<u32>,    // length P + 1; grouped by owner P1 position
    pub links: Vec<Link>,           // the owner's forward links
    pub cluster_assoc: Vec<ClusterAssoc>, // referenced by Link::cluster_start/len
    pub inverse_offsets: Vec<u32>,  // length P + 1; grouped by partner P1 position
    pub inverse: Vec<IncomingLink>, // owners whose link points at this partner P1 position
}
pub struct Link {
    pub partner_position: u32,      // < P
    pub offset: [i32; 3],           // partner image relative to the owner's cell
    pub pair_id: u32,               // shared pair-table index
    pub cluster_start: u32,         // into cluster_assoc
    pub cluster_len: u16,
}
pub struct IncomingLink { pub owner_position: u32, pub link_id: u32 }
pub struct ClusterAssoc { pub instance: u32, pub slot: u8 }
```

- **Owner → partner resolution:** `partner = wrap(owner_cell + link.offset) * P + link.partner_position`. The
  addition and `rem_euclid` are evaluated in `i64` (the `u32` cell and the `i32` offset are widened), and the
  wrapped component is cast back only after the `[0, A/B/C)` range check; an `i32` addition can therefore never
  overflow, even for `offset = i32::MAX` (review F-21). The reader's `|offset[i]| ≤ |interaction_range[i]| + 1`
  remains the only offset bound needed; a hostile-offset package is accepted into the arithmetic and either wraps
  exactly or fails the `A*B*C*P`/`i32` index check, and a test feeds `i32::MIN`/`i32::MAX` offsets and asserts no
  panic and the correct wrapped target. This is the only place the periodic image is resolved; Step 15 never
  reconstructs it from the target index.
- **Partner → owners resolution:** for an absolute environment `V`, `inverse(position(V))` lists
  `IncomingLink { owner_position, link_id }`. The owner's absolute environment and the forward link are
  `owner = wrap(cell(V) - link.offset) * P + owner_position` (same `i64` arithmetic) and
  `links(owner_position)[link_id]`; the pair id and the owner row come from that forward link. A missing inverse
  entry means the payload has no interaction in that direction; it is **not** an error and must not be
  synthesised.
- Sorting is deterministic and is part of the reproducibility contract (it fixes the f64 summation order):
  `pair_id` ascending, then `partner_position`, then `offset` lexicographically. The inverse index follows the
  forward order within each partner position. The original sorted by pair id only with an unstable sort; the
  added tie-breakers change no physics, only rounding, and are recorded in §14.
- `cluster_assoc` on a forward link means "the owner is the centre of cluster **instance** `instance`, and this
  link's partner occupies slot `slot`". The cluster term therefore belongs to the link's owner (the cluster
  centre), and a change at the partner updates the owner's cluster code and centre energy. This is the original's
  `EnvironmentLink.ClusterLinks` orientation, expressed in the payload's centre-major link direction.
- Both CSRs are built in one pass over the payload links: `N` forward entries (`28 B` each) and `N` inverse
  entries (`8 B` each), bounded by `max_pair_links` and `max_link_bytes`. The original allocated the per-cell
  link list once per supercell cell, multiplying the link memory by `A·B·C`; the compact form is why a
  500k-position context is bounded. `MemoryPlan` includes both arrays and `cluster_assoc`.
- Validation: `partner_position < P`, `pair_id < pair_table_count`, `|offset[i]| ≤ interaction_range[i] + 1`
  (reader-checked), the wrapped partner index `< A*B*C*P`, the per-owner count ≤ `max_links_per_position`, and
  the inverse entries are exactly the forward entries (a counting-pass equality, asserted in debug and tested).
- **No mobile-only link optimisation in v1.** The original's `OPT_LINK_ONLY_MOBILES` changes S2 when mobility
  changes; it is deferred to Step 29 behind a measured option. Recorded in §14.

### 6.2 Cluster links, centres, and instances

- A cluster table's slot `0` is a centre slot; the payload's per-environment cluster link is
  `{cluster_id, byte_position}` where `byte_position` is **this environment's own slot** in the cluster
  (confirmed against `CompiledClusterLink(ClusterTableIndex, SiteSlot)`).
- **A cluster table can have several centres.** `ClusterCompilationStage` adds slot `0` to every P1 position
  owned by the centre site (`foreach (var p in Sites[centerSite.Index].PositionIndices)`), so a centre site with
  more than one P1 position produces one slot-0 link per position. The runtime therefore has **one
  `ClusterInstance` per slot-0 link** (review F-17): `K` is the number of slot-0 links, not the number of tables.
  `codes`/`code_ids`/`energy` are indexed per instance, so each centre has its own code and centre energy.
- Step 14 validates, per cluster table `t`:
  1. the table is referenced by at least one environment and has at least one slot-0 link;
  2. a `(table, position)` pair carries at most one slot-0 link, and every slot-0 position belongs to the
     table's centre site (all slot-0 links share one `site_index`); each slot-0 link becomes one instance;
  3. every slot `1..=n` is present on at least one position, and all positions carrying slot `j` belong to a
     single reference site `member_site[j]` (the slot's member site); a slot whose positions span two sites is
     `ClusterGeometry`;
  4. the slot set is contiguous (no gap) and the slot count is at most 9 (the .NET
     `ClusterReduction.MaxClusterSites`; a 6-bit state code holds up to 10 and the reader bounds
     `byte_position < 10`);
  5. each slot's candidate positions are exactly the payload positions carrying that slot; a slot may have one
     candidate (the current writer) or several (a future writer that emits one placement per centre), and
     duplicates of the same position are rejected.
  A violation is `ContextError::ClusterGeometry { cluster_table, instance, slot, detail }` (exit 12).
- **Slot `0` is the centre by definition:** for instance `i`, `slot_positions[slot_start(i)] = centre_position` and
  `slot_cells[slot_start(i)] = [0,0,0]`. The image derivation (§6.4) applies only to slots `1..=n`.
- The **centre's** energy vector holds the cluster term (§7.3). The instance's resolved member links carry the
  `cluster_assoc` entries (`instance`, `slot`) on their owner (the centre's forward link), so a particle change at
  a partner updates the instance's code and centre energy through the inverse index (§6.1). Non-centre members
  hold no cluster energy and need no membership record for delta routing; the centre index below is used only to
  read a path node's cluster terms (one per instance centred there).

### 6.3 Cluster instance and code representation

```rust
pub struct ClusterArena {
    pub instances: Vec<ClusterInstance>,            // one per slot-0 link
    pub centre_offsets: Vec<u32>,                   // length P + 1
    pub centres: Vec<u32>,                          // CSR over P1 positions -> instance ids
    pub slot_start: Vec<u32>,                       // length K + 1; flat slot arrays (no per-instance Vec)
    pub slot_positions: Vec<u32>,                   // P1 position per instance slot, slot 0 = centre
    pub slot_cells: Vec<[i32; 3]>,                  // relative cell offset per instance slot, slot 0 = [0,0,0]
    pub codes: Vec<u64>,                            // [instance * A*B*C + cell] current raw tuple
    pub code_ids: Vec<u32>,                         // [same] canonical index from ClusterTable::lookup
    pub energy_start: Vec<u32>,                     // length K + 1; prefix of mapping_len per instance
    pub energy: Vec<f64>,                           // [cell * Σ mapping_len + energy_start[i] + row], kT
}
pub struct ClusterInstance {
    pub table: u32,
    pub centre_position: u32,
    pub slot_start: u32,                            // the instance's range in slot_positions/slot_cells
    pub slot_count: u8,
}
```

- `codes[instance, cell]` is the full occupation code with the centre's particle at slot `0` and every other
  slot's particle read from the member environment at
  `wrap(cell + slot_cells[slot_start(instance) + slot]) * P + slot_positions[slot_start(instance) + slot]`.
  It is built once and rebuilt by Step 15 when the state changes.
- Cluster geometry is stored in **flat CSR arrays**, not a `Vec` per instance: the context is one allocation per
  array, the plan accounts for `Σ slots` over all instances, and no instance performs its own heap allocation.
- A `ClusterInstance` is one per **slot-0 link** (`K` = slot-0 link count), so a cluster table on a multi-position
  centre site yields one instance per centre position and each gets its own slot geometry, code, and energy.
  There is no `table -> instance` map; the instance carries its `table`, and `centres` is a CSR over P1 positions
  listing the instances centred there (one per cluster table centred on that position). `centres` is what Step 15
  uses to add the cluster terms of a path node.
- **Code resolution is the approved amendment contract (review F-29).** The raw tuple is built exactly as
  `codes` is (slot 0 = centre particle, slots `1..=n` = the resolved member particles, 6 bits per slot), then
  resolved through `ClusterTable::lookup(raw) -> ClusterCodeLookup` from the Step 13 runtime
  ([`step-11-amend-2`](../amendments/step-11-amend-2-cluster-code-expansion.md) §6):
  - `Canonical(index)` → `code_ids = index`; the applied energy is `table.energies[index]`.
  - `MissingFromMap(raw)` → `ContextError::MissingOccupationCode { cluster_table, code }`, exit 12. A present
    map is authoritative; a missing raw tuple means the package is internally inconsistent.
  - `ExpansionMapAbsent(raw)` → **`ContextError::ExpansionMapAbsent { cluster_table, code }`**, exit 12, with an
    actionable "this package predates the cluster-code expansion map; rebuild it with the current writer"
    message. It is deliberately distinct from `MissingOccupationCode`: old package versus corrupt package.
  - **No solver-side symmetry.** Never sort/minimise the slot particles, never re-run a reduction, never map to
    a neighbouring code, and never fall back to a direct `occupation_codes` index when the lookup fails. The
    expansion map is the only resolution; a `Canonical` result for a singleton/no-map table still works because
    the table itself is complete.
  - The context stores only the canonical `code_ids`; the expansion map stays in the shared
    `Arc<ParameterizedModel>` and is not copied into the context. The Step 14 code has no symmetry kernel and no
    space-group catalog.
- The initial check resolves the initial tuple for **every instance and cell** (and every allowed centre
  particle for the energy rows) and propagates the lookup error, so an old or inconsistent package fails at
  initialization rather than during a run. The amendment guarantees that every raw tuple enumerated for a
  writer-valid table resolves; a `MissingFromMap` is therefore a package-corruption error and an
  `ExpansionMapAbsent` is the pre-map package case.
- `energy[cell * Σ mapping_len + energy_start[i] + row(p)]` for `p` in the table's `particle_table_mapping`;
  the per-instance base is `energy_start[i]` (prefix of `mapping_len(instance)`), so a table with a different
  allowed-centre set has a computable base. Zero (unused) for other particles. The row count is
  `mapping_len(instance)`, not `pc`, and the plan sizes the array by `cells · Σ mapping_len` (§2). There is no
  centre row: the centre particle is encoded in slot `0`. `particle_table_mapping` is the **ascending list of
  allowed centre particles**, not a `[u8; 64]` particle→row map (the Step 15 draft's `[center_row][code]` wording
  is corrected; §7.3 and the data contract are the source).
- `occupation_code(instance, centre_cell, overrides)` builds a code from the current particles with a small
  caller-supplied slot override list and no allocation; Step 15 uses it to evaluate S2 after all path-node
  changes without mutating the stored code.
### 6.4 Cluster geometry: the v1 limitation (open decision OD-1)

The `.msl` v1 cluster link persists only `{cluster_id, byte_position}`. It does **not** persist the member's
relative cell offset; the compiler knows it (`ResolvedPathPosition.Encoding`) but the writer drops it, and the
payload's positions carry only `vector4 = [0,0,0,P]` (no fractional coordinates). The solver therefore cannot
reconstruct a member image in general.

Step 14 derives each instance's geometry from the payload and fails closed instead of guessing:

- **Slot `0` is `[0,0,0]` by definition** (§6.2): the centre is the slot-0 environment itself, so no image is
  derived for it.
- Let `C_j` be the payload positions carrying slot `j` of the table (all on `member_site[j]`, §6.2). For each
  **instance** `i` (centre `c_i`) and each slot `j = 1..=n`, resolve in this order:
  1. the links from `c_i` whose resolved partner position is in `C_j`. If exactly one, use it (this is the
     original single-centre rule and covers the current writer, where `C_j` has one position).
  2. if step 1 found no link and the table has **more than one slot-0 link** (multi-centre), take the links
     whose partner position belongs to `member_site[j]`; if exactly one, use it. This is the **rotated
     multi-centre image**: `ClusterCompilationStage` writes the member slot on the representative placement
     only, and a different centre position of the same site reaches the member site through its own pair image.
     It is **not** a cross-image acceptance: the image is the space-group transport of the representative
     cluster, so the occupation tuple is a symmetry image of an enumerated tuple (§ D-11). A single-centre table
     does **not** use this fallback: it must resolve the candidate position exactly (the strict OD-1 rule).
  3. otherwise (zero, or more than one) the instance's geometry is ambiguous or absent: initialization fails
     with `ContextError::ClusterGeometry` naming the table, instance (centre position), slot, and member site.
     Zero links also means no delta could ever reach the centre's code, so accepting it would silently freeze
     the cluster.
  The resolved link's `offset` is `slot_cells[slot_start(i) + j]` and its partner position is
  `slot_positions[slot_start(i) + j]`; the link receives `cluster_assoc { instance, slot }`.
- The derived geometry is cross-checked by the §6.3 lookup: a wrong offset yields a raw tuple that is either
  absent from a present map (`MissingFromMap`) or resolves to a different canonical orbit; the initialization
  check runs for every instance and every allowed centre particle. It cannot detect a *valid but differently
  imaged* tuple, which is the inherent OD-1 risk recorded below.
- **Accepted by the rule (not a cross-image).** For a multi-centre table, each instance resolves its own unique
  **member-site** image. This is the space-group transport of the representative placement: the energy table
  enumerates every allowed occupation tuple, and the applied energy is invariant under the full space group
  (§ D-11), so the transported tuple has the same energy. It is accepted, not a limitation.
- **Accepted residual risk (single-centre OD-1).** A single-centre table with exactly one candidate-position
  link is accepted, because that is the only exact reconstruction the payload allows. If the compiler had
  actually used a different periodic image of that member, the solver cannot know; the authoring/compiler side
  must not produce that ambiguity. This is the inherent v1 risk the owner accepts with OD-1, not a new
  acceptance of a cross-image cluster.
- **Rejected (fail closed).** (a) a cross-image member: a single-centre table whose candidate position has no
  unique link (the authored image is another periodic image); (b) a member site with several images from one
  centre; (c) two slots that resolve to the same link or whose fallback site is the same (for example a
  dumbbell whose two images are the same P1 position, or two slots on one member site at a non-representative
  centre); (d) an unresolved multi-centre placement. All are writer-valid, are rejected with the per-instance
  `ClusterGeometry` error, and are recorded as the OD-1 limitation; the writer follow-up emits the per-centre
  member placement (or persists the relative offset).
- **Fixtures (review F-36).** `step-13-populated` has one cluster centred on position 1 with members 0 and 2,
  each with a single `[0,0,0]` image (one slot-0 link); the other Step 13 fixtures have no cluster table. The
  new `step-14-multi-centre-cluster` package (feature 6) is built from the committed
  `samples/step-14-multi-centre-cluster/step-14-multi-centre-cluster.mocprj` (a derivation of the centred sample
  with a third stable site C; the `step-13-centered` package stays byte-identical) and has the full artifact set: `.msl`, `.sha256`,
  `package-summary-v1.json`, `payloads/index.json`, `payloads/model-1.bin`, a README with provenance (writer
  commit, SDK, `created_utc`, job count), a .NET rebuild test, and the usual regeneration command. Its centre
  site owns four P1 positions, so it must yield four instances with per-instance codes/energies. The two member
  offsets resolve to **distinct member sites** (B and C) because the project-model `ENM008` validator requires
  2–8 surrounding offsets; a one-member cluster is not authorable. The **two-member non-P1 case that exercises
  F-29 is the amendment-2 ceria fixture**
  (`tests/contracts/packages/step-11-amend-2/`), not a second multi-centre package, because two images of one
  member site are the ambiguous case above. A synthetic distinct-energy table in the unit tests evidences the
  code→energy mapping, since the sample tables are zero.
- **Writer follow-up (review F-13/F-17).** A writer-valid cross-image or multi-centre-ambiguous cluster still
  reaches the solver and is only rejected at initialization. A build-time diagnostic in `Mocassin.Compilation`
  (which knows `ClusterCompilationStage`'s placement) would surface it at build time. That is a Step 11/12
  maintenance item, not a Step 14 format change: it is recorded as a named follow-up in
  [`05-open-decisions.md`](../05-open-decisions.md) (OD-1) with the owner *planner*, the trigger "next
  maintenance branch touching `ClusterCompilationStage`", and the `msl-format.md`/`troubleshooting/errors.md`
  notes scheduled in §18.
- Consequence: a writer-valid package whose cluster uses a second-nearest image, a member site with several
  images, or an unresolved multi-centre placement is **rejected** with an actionable error. That is a recorded
  v1 limitation, not a silent approximation. The permanent fix is a future `.msl` payload revision that persists
  the per-centre member offset (or fractional P1 coordinates); it is a format change and needs its own decision,
  amendment, and version handling. Recorded in §23 and [`05-open-decisions.md`](../05-open-decisions.md).

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
- The owner's vector is built from the owner's **own** forward links, with the **owner's** particle as the
  table row:

  $$
  E(\text{owner}, p) = \text{site}[P(\text{owner})][p]
    + \sum_{l \in \mathrm{links}(P(\text{owner}))}
      \texttt{pair\_table}[l.\texttt{pair\_id}][p][\text{particle at } \mathrm{partner}(\text{owner}, l)]
  $$

  where `partner(owner, l) = wrap(owner_cell + l.offset) * P + l.partner_position` (§6.1). The sum uses the
  sorted link order. Entries for particles outside the environment's selection mask are still computed (they are
  cheap and Step 15 reads arbitrary rule particles); they are not selectable.
- A particle change at environment `V` updates `E(owner, p)` for every `owner` returned by `incoming(V)`, with
  `p = p_owner` and `pair_table[l.pair_id][p_owner][new] - pair_table[l.pair_id][p_owner][old]`. The sender's
  table is never applied and `pair_table[...][p_owner][new]` is never transposed. Step 15 owns the delta; Step 14
  owns the orientation and exposes `incoming` (§15).
- Pair tables are converted to kT once at initialization; the model's payload order is preserved. Steps 15/17
  must not assume that a reverse link exists for every forward link.

### 7.3 Cluster vectors

- Per instance and cell, `cluster_energy[instance, cell, p]` as in §6.3.
- The cluster term is **not** folded into `energy`: `energy` is the site+pair path-node vector, and the cluster
  term is a separate array counted once at the centre. This is the documented physics convention
  ([`docs/physics/energies.md`](../../physics/energies.md): "A cluster contributes only at its centre") and it
  is what makes the total exact.
- Step 15 reads `cluster_energy` for the affected centres. A path node's cluster terms are **every instance**
  in `centres(position)` (a position can be the centre of more than one table); a changed environment's affected
  instances are the owners returned by `incoming` whose link carries `cluster_assoc` (§6.1). Step 14 precomputes
  both indexes, so the transition-energy sum never scans the supercell and no separate "touched instance" table
  is needed.

### 7.4 Initial lattice energy

$$
U = \sum_{\text{stable } e} E_\text{site}(P(e), p(e))
  + \tfrac{1}{2} \sum_{\text{stable } e} \bigl(E_\text{env}(e, p(e)) - E_\text{site}(P(e), p(e))\bigr)
  + \sum_{\text{instances } i} \sum_{\text{cells } c}
      E_\text{cluster}\bigl(i, c, p(c, P(\text{centre}_i))\bigr)
$$

Equivalently: each site background once, each pair once (the 0.5 corrects the both-ends sum), and each cluster
instance **once per supercell cell** at its centre. An instance is a geometry placed at every cell of its centre
position, so the cluster term is instance × cell (review F-33), not one term per instance; `cluster_energy` is
indexed `[instance, cell, p]` (§6.3). This deviates from the original's `0.5 * Σ_stable E[current]`, which also
halves the site and cluster terms and replicates clusters at every member; the deviation is deliberate,
documented in §14, and covered by a hand-computed test on a **multi-cell** lattice (at least two cells, so a
missing cell factor cannot pass) with a site offset, one pair interaction, and a 3-site cluster. Step 15 owns the
incremental update and must use the same decomposition; a debug-only full recomputation helper is provided for
consistency checks.

The formula is evaluated in kT; `lattice_energy_ev = U * kT_eV` is the reported value. An MMC abort uses energy *fluctuations*, so a constant
scale error would cancel; the absolute value is nevertheless the documented convention and is what `.mcs`
reports from Step 18.

## 8. Selection pools and optimizers

**Selection space.** The KMC loop selects a **supercell environment**, so a pool member is an absolute
environment plus a particle, not a P1 position plus a particle. The direction count and the selection mask are
both P1-level, which is why the pool *id* stays a P1 table but the *slot* is supercell-addressed:

```rust
pub struct SelectionPools {
    pub pools: Vec<SelectionPool>,    // ascending direction_count
    pub slot: Vec<u32>,               // length E*pc; key = env*pc + particle; member slot or u32::MAX
    pub pool_of: Vec<u32>,            // length P*pc; key = P*pc + particle (P1 position); pool id or u32::MAX
    pub selectable_jump_count: u64,
}
pub struct SelectionPool { pub direction_count: u32, pub members: Vec<PoolMember> }
pub struct PoolMember { pub env: EnvId, pub particle: ParticleId }
```

- A `PoolMember` is 8 bytes (`EnvId` `u32` + `ParticleId` `u8` + padding). The Step 13
  `mocassin_types::DirectionPool` placeholder is **not used** by the context: its
  `members: Vec<(PositionId, ParticleId)>` is contractually P1 (`ids.rs`) and its per-pool `slot_of` cannot hold
  the supercell key space. Feature 4 retires the placeholder (no reader constructs it) and records the
  replacement in the Step 13 page's container note. This is an internal placeholder adjustment, not a format or
  contract change.
- Direction count per `(P, particle)` is `jump_count[P*pc + particle]` (`u32`):
  - `0` = no selectable directions (static or passive; the original's `-1`/`0` split is not representable);
  - `>0` = selectable **iff** the position's `selection_particle_mask` has the particle bit set.
- One pool per distinct selectable direction count, ordered ascending by `direction_count` (Step 16 walks pools
  in this order). Members are inserted in ascending `(env, particle)` order over the whole supercell, so the
  ordering is deterministic and independent of the cell traversal.
- `slot[env*pc + particle]` is the member's slot inside its pool (`u32::MAX` = absent); a swap-remove updates it
  for the moved member. `pool_of[P1*pc + particle]` gives the pool id (`u32::MAX` = not selectable) and is
  enough for the O(1) add/remove, because a combination's direction count and mask depend only on its P1
  position. `pools.len() ≤ max_pools` (default 65,536) and the id is `u32`, so there is no silent wrap; the
  check is in the plan.
- `selectable_jump_count = Σ pool.members.len() * pool.direction_count` (as `u64`) and must be
  `≤ max_selectable_jump_count` and `> 0` for KMC (`ContextError::NoSelectableJumps`, exit 12). Step 16 draws
  `r < selectable_jump_count`, walks the pools subtracting `members.len()*direction_count`, then takes member
  `r / direction_count` and direction `r % direction_count`.
- Optimizers: each job `SelectionOptimizer(position, particle)` removes every supercell member with that
  `(P1, particle)` from its pool after construction. An optimizer that is not in any pool is a no-op logged at
  debug level, not an error; the reader already validated the indices. Removing the last member of a pool leaves
  an empty pool, and `selectable_jump_count` is reduced accordingly.
- State-change helpers (used by Steps 15/16/17, not by Step 14): `set_selectable(env, particle)` recomputes the
  direction count and the mask bit from the P1 tables, then swap-removes or inserts the `(env, particle)` member
  and updates `selectable_jump_count`/`slot`. A combination re-enters the pool after a particle change (the
  optimizer is a start-condition hint, matching the original), which is documented and tested. Callers remove
  the departed and add the arrived particle for each changed environment; a debug invariant asserts that every
  member's `particle` equals `particles[env]`.
- Memory is bounded by `max_pool_bytes` and planned: `M*8 B` members + `E*pc*4 B` slot map + `P*pc*4 B`
  `pool_of`. The earlier draft's `pools × P*pc × 4 B` was a per-cell estimate for a supercell structure; the
  plan's numbers replace it.
- **Capacity is reserved per pool (review F-22).** The plan's counting pass computes, for every distinct
  direction count, the number of selectable `(env, particle)` combinations with that count; each
  `SelectionPool::members` is created with exactly that capacity (`Vec::with_capacity`), and `slot`/`pool_of`
  are allocated once. Optimizer removal and swap-remove only reduce `len`, so `set_selectable` can never
  reallocate. The zero-allocation acceptance criterion therefore covers the pool add/remove helpers, and
  `capacity_bytes` is recorded (F-23). A debug assertion checks
  `members.capacity() >= planned_capacity` (over-allocation is allowed; `with_capacity` guarantees only `>=`),
  and the report compares the planned count, not the capacity (review F-34).

## 9. KMC jump status

### 9.1 Status identity and path resolution

- A status is `(P1 position, direction)` indexed `status = direction * P + position`, i.e. one entry per
  `P * direction_count`. This is the translation-compact equivalent of the original's
  `[cell][direction]` array: the sender/receiver geometry is cell-independent, and the absolute path is
  resolved at use.
- `resolve_node(selected: EnvId, direction, node) -> EnvId`:
  `node 0 = selected`; for `i ≥ 1`,
  `trim(cell(selected) + direction.path[i].cellA/B/C) * P + direction.path[i].P`, where `trim` is the
  component-wise wrap into `[0,A)×[0,B)×[0,C)`. Node 0's stored `P` is the
  representative position and is **replaced** by the selected position, so a direction mapped into another
  symmetry-equivalent start bucket is applied at the selected bucket. This is the Step 16 contract.
- The helper is bounds-checked (`node < path_len`) and does not allocate; it writes into a caller-provided
  `[EnvId; 10]`/slice.

### 9.2 Jump links: the receiver owns the link and the table

A jump link exists because a **receiver** environment (a path node) owns a pair link whose partner is the
**sender** environment (another path node) that changes particle. The receiver's vector is the one that changes,
and the pair table row is the receiver's particle. This is the original's energy rule; the original's *storage* is
the dual: `LinkEnvironmentToSurroundings` stores the link on the changed/partner environment with
`TargetEnvironmentId` = the centre (the environment whose energy changes), and `DistributeEnvironmentUpdate` /
`InvokeJumpLinkDeltas` switch to that target, apply its pair table to its own energy entry, and
`BufferJumpLinksOfJumpPath` stores the index into the sender's link list (the receiver-owned list). Our
`JumpLink { sender_path_id, receiver_path_id, link_id }` names the receiver explicitly and indexes the
receiver's forward list, which is the same relation read from the other side. It is the same rule as §6.1/§7.2.
The earlier draft searched the sender's links and let Step 15 apply `energy[sender_target]` with the sender's
table, which is the transposed table unless every pair table happens to be symmetric.

- For each status (P1 start `s`, direction), resolve the path nodes. For every ordered pair
  `(sender_path_id, receiver_path_id)`, `sender != receiver`, with both nodes structurally stable:
  - walk the **receiver's** forward link range (`links(position(receiver_abs))`) and find the link whose resolved
    partner is `sender_abs`. Take the first in sorted order; if several receiver links reach the same sender
    (possible in small cells), the first sorted link wins and the choice is documented.
  - if no such link exists, no delta is needed in that direction (the payload has no interaction from the
    receiver to the sender; do not synthesise one) and no entry is emitted.
  - otherwise append `JumpLink { sender_path_id, receiver_path_id, link_id }` where `link_id` is the index into
    the **receiver's** forward link list. The receiver's absolute environment is
    `resolve_node(selected, direction, receiver_path_id)`; the pair id and the row particle come from
    `links(position(receiver_abs))[link_id]` and the receiver's path node.
- The `link_id` index space is therefore the receiver's owner-major list, matching the Step 13 `JumpLink` doc
  ("the environment link whose energy changes"). The Step 13 placeholder gains the explicit `receiver_path_id`
  field (`sender_path_id: u8`, `receiver_path_id: u8`, `link_id: u32`; 8 bytes) in feature 4, so Step 15 never
  reverse-searches the node array. The CSR shape (`offsets`, `links`) is unchanged.
- **Receiver filter: structural, not initial-occupation-based.** The original skips path nodes whose initial
  particle is void. Step 14 includes every path node whose P1 position is structurally stable, so a receiver that
  is void at initialization but occupied later still receives its delta. This is a correctness fix over the
  original, recorded in §14 and tested in Step 16 (a path node void at init, occupied later).
- Bound: at most `L*(L-1) ≤ 90` links per status (the original's 56 is the `L=8` case). The builder uses a fixed
  90-entry scratch array; exceeding it is an internal invariant error (exit 13), never a silent truncation and
  never the original's unchecked overflow. A status with zero links is valid and common.
- CSR: `offsets: Vec<u32>` length `P*D + 1` (with `max_status_count` bounding `P*D`), `links: Vec<JumpLink>`,
  both grouped by ascending status index. The link count is determined by a counting pass and checked against
  `max_jump_links` **before** the vector is allocated (F-3).


### 9.3 Cluster associations per jump link

There is no separate cluster-touch table. The receiver's forward link already carries `cluster_assoc` entries
(§6.1/§6.2): the receiver is the cluster centre, and the touched slot is the sender's slot in that centre's
instance. The earlier `ClusterTouch { instance, slot, sender_path_id }` list was a second, unbounded derived
array (review F-3) and was oriented from the member, which conflicted with the owner-major rule (F-2).

- For a jump link `(sender, receiver, link_id)`, the receiver's cluster changes are
  `links(position(receiver_abs))[link_id].cluster_start..cluster_len`. Step 15 updates the centre's occupation
  code for slot `slot` and reads the centre's new cluster term.
- A cluster with a member outside the path is not touched during S0/S1/S2 evaluation; its code and energy are
  updated when the state is committed, through the same `incoming` mechanism (§15).
- A cluster whose slot link is missing, or whose slot has several images, is rejected at initialization (§6.4),
  so a jump link can never point at a cluster that the solver cannot update.
- The association array is bounded by `9·K` entries (`ClusterAssoc` is 8 bytes) and is part of `max_link_bytes`.

### 9.4 Interface for Step 16's exact occupation check


Step 14 does **not** resolve a rule to a selected direction and does not check every `(rule, direction)` pair
(the Step 13 M-3 assignment). It exposes the data Step 16 needs after the direction and rule are known:

- `allowed_mask_of(env: EnvId) -> u64` (the position's allowed mask, void exempt),
- `path_len(direction) -> u8`,
- `resolve_node(selected: EnvId, direction, node) -> EnvId`,
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
  `cycles_per_block = total / 100`.
- `mobile_count == 0` for a KMC job is `ContextError::NoMobileParticles` (exit 12): the original returns
  `ERR_NOMOBILES` from `SetCycleCounterStateToDefault`, and every abort condition would be degenerate.
- `goals.total == 0` (both `pre_run_mcsp` and `target_mcsp` are zero) for a KMC job is
  `ContextError::InvalidInput { what: "total goal", .. }` (exit 12), matching the original's
  `ERR_DATACONSISTENCY` in `SetCycleCounterStateToDefault`. The earlier `max(1, total/100)` hid the zero case
  and silently turned a meaningless job into a one-block run; the round-up already guarantees
  `total ≥ 100 ⇒ total/100 ≥ 1` for every accepted job, so the `max` is removed.
- **Checked goal arithmetic (review F-37).** `target_mcsp` and `pre_run_mcsp` are untrusted `u64` job settings
  and are not range-bounded by the reader. `pre_run_mcsp * mobile_count`, `target_mcsp * mobile_count`, the
  `prerun + main` sum, and the round-up to the next multiple of 100 are evaluated with checked `u64` arithmetic;
  an overflow is `ContextError::InvalidInput { what: "goal overflow", .. }` (exit 12), consistent with §2's
  checked-arithmetic rule. No wrapping and no saturating clamp.
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
  The job payload persists **only the optional job cap** (`JobSettings.max_attempt_frequency_hz`, mapped from
  `job.settings.max_attempt_frequency_hz`); it does not persist the original's derived build-time
  `AttemptFrequencyModulus` (`BaseFrequency`). The fallback is therefore the maximum active-rule frequency, and
  it is also Step 16's frequency-precheck denominator. `nu_max > 0` and finite, else exit **13**
  (`ContextError::NonFiniteFactor`): the value feeds `dt`, and a zero/NaN maximum makes the time step
  non-finite. It is classified with `kT`, `K_fix`, and `K_norm`, not with the data errors, because the package
  is structurally valid and the failure is the numerical denominator (review F-20; §13 and the quality strategy
  agree).
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
    /// `.mcs` `metadata.modelSignature` = `ParameterizedModel.signature` (geometry **and** applied values).
    pub model_signature: String,
    /// `.mcs` `metadata.seedHash`, recomputed by Step 14 from `job.seed`; binds the state to this job.
    pub seed_hash: String,
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
    pub mobile_positions: Option<Vec<u32>>,  // optional cross-check; Step 18 writes it
}
```

- The `.mcs` decode is Step 18's. Step 19 (or the Step 16 run path) builds `ResumeState` from the document and
  passes it in `ContextOptions.resume`. Step 14 performs no file I/O and does not depend on `mocassin-state`.
- **Field sources are fixed (review F-7).** `model_signature` is the `.mcs` `metadata.modelSignature`, which
  `02-data-contracts.md` §4.3 defines as the **parameterized** model signature
  (`ParameterizedModel.signature`, geometry + applied values), *not* the Step 08 `model_signature` identity
  string. The `.mcs` has **no** lattice signature field, so `ResumeState` has none: the state↔package binding is
  `model_signature` + `seed_hash` + `job_id` + the dimension/count checks. `seed_hash` is the SHA-256 hex of
  `job.seed` (`.mcs` `metadata.seedHash`); it distinguishes two jobs that share a model. Step 18/19 must not
  invent a `latticeSignature` in the state document; a future field is a Step 18 format decision.
- Validation (all → exit 12 unless noted): `model_signature == job.model.signature`, `seed_hash` equal to the
  recomputed `SHA-256(job.seed)` hex, job id, kind, `lattice_size`, `position_count`, `particle_count`,
  `particles.len() == A*B*C*P`, every particle index, `simulated_time_s` finite and `≥ 0`, `K_fix/K_auto/K_norm`
  finite and `> 0`, `RawMax ≥ 0` and finite, a per-particle counter array of length `pc`, and the mobile mapping
  re-derivation. A bad RNG blob is `ContextError::RngState` (exit 13) because it is an internal/cryptographic
  state failure.
- **Not validated by Step 14:** `.mcs` `totalGoal`/`prerunGoal`, `flags`, and `abortReason`. A pre-run finish
  legitimately adds its overshoot to the main goal (Step 16), so a recomputed-goal equality check would reject a
  valid pre-run state; `state_flags` is stored opaquely (Step 14 never interprets it), and `abortReason` belongs
  to the run orchestration. `mcs`/`cycles`/counters/time are restored verbatim.
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
| `MissingOccupationCode { cluster_table, code }` | 12 | A raw cluster tuple is absent from a **present** expansion map: the package is internally inconsistent |
| `ExpansionMapAbsent { cluster_table, code }` | 12 | No expansion map, and the raw tuple is not canonical: the package predates the map and must be rebuilt with the current writer. Distinct from `MissingOccupationCode` (old package vs corrupt package); never guessed around |
| `NoSelectableJumps` | 12 | KMC job with `selectable_jump_count == 0` |
| `NoMobileParticles` | 12 | KMC job with `mobile_count == 0` |
| `ResumeMismatch { what }` | 12 | Resume state does not match the package/job (signature, seed hash, id, kind, dimensions, counts, mapping) |
| `LimitExceeded { what, value, limit }` | 12 | A context limit or the memory plan (§2) rejects the job **before** allocation |
| `NonFiniteFactor { what }` | 13 | `kT`, `K_fix`, `K_norm`, `nu_max`, or `dt` is not finite/positive |
| `RngState(..)` | 13 | `RngStateV1` magic/version/word position/stream invalid |
| `Invariant { what }` | 13 | Debug/release invariant violation (CSR, inverse/forward equality, pool total, code id, jump-status bound) |
| `Cancelled` | 15 | The caller's cancellation flag was set |

- **Exit 12 vs exit 14 (review F-14).** `LimitExceeded` is 12, not 14. Exit 14 is the architecture's
  "aborted by a configured limit" for a **run-time** abort (cycle target, wall clock, rate) and is produced by
  Step 16, not by context construction. A job whose context cannot be represented is a model/data error
  detected before the first cycle, exactly like a malformed table; mapping it to 14 would conflate "the run hit
  a limit" with "the package cannot be run". The distinction is stated in
  [`01-target-architecture.md`](../01-target-architecture.md) §7.3 and in `troubleshooting/errors.md`.
- **Cluster-code lookup outcomes (review F-29).** `Canonical(index)` is the only success; `MissingFromMap` is
  `MissingOccupationCode` (exit 12, corrupt package) and `ExpansionMapAbsent` is the distinct recompile-required
  error (exit 12, pre-map package). A map that exceeds the reader's expansion budget is rejected earlier by
  Step 13's `ReaderLimits::max_cluster_expansion_bytes` (package error, exit 11), not by the context.
- Every diagnostic names the phase, the dense index (position, particle, cluster table, slot, status), and the
  fixture-independent reason; no `unwrap` on package data.
- `tracing` emits one `info` line per completed phase (counts and bytes) and debug lines for skips (e.g. an
  optimizer that was not in the pool). No file output.

## 14. Deviations from the original (each with a test)

| # | Deviation | Rationale | Test |
| --- | --- | --- | --- |
| D-1 | Compact per-P1 link CSR (owner-major) plus a compact inverse routing index instead of a per-supercell-cell link array | Memory; translation invariance; same physics; the inverse index reproduces the original's receiver-owned link list without `A·B·C` copies | Link set and inverse set equal a brute-force payload expansion for every supercell cell |
| D-2 | Link sort adds `partner_position`/`offset` tie-breakers | Deterministic f64 summation order | Byte-stable context summary across runs |
| D-3 | Cluster energy counted once at the centre; site counted once; pair with 0.5 | Exact total; the original's uniform 0.5 is documented as a pair correction and misapplies to site/cluster | Hand-computed pair/cluster/site total |
| D-4 | Cluster row layout is 1-D per code (slot 0 = centre particle) | The .msl payload encodes the centre particle in slot 0; there is no centre row | Cluster energy vector vs. brute-force table lookup |
| D-5 | Jump-status receiver filter is structural stability, not the initial particle | A node void at init but occupied later must still receive deltas | Step 16: node void at init, occupied later |
| D-6 | Jump-status bound is `L*(L-1)` with an invariant error, not a fixed 56 with an overflow | `max_path_nodes = 10`; silent overflow is unacceptable | A 10-node direction yields ≤ 90 links; an 11th node is rejected by the reader |
| D-7 | `IsStable` requires the compiled site stability flag | Honours the compiled model; the original ignored it | Synthetic unstable position is never stable |
| D-8 | Mobile mapping re-derived on resume and compared | Stronger than restoring a stored mapping | Mismatched mapping is rejected; matching mapping resumes |
| D-9 | No `OPT_LINK_ONLY_MOBILES`, no static S2, no fast-exp, no parallel init | Deferred to Steps 15/29 with measurements | Absent by construction; recorded in the parity notes |
| D-10 | A changed state rebuilds each affected cluster's occupation code **once from its slots**, instead of mutating code bytes incrementally per jump link | The original can hit an intermediate occupation code that the table does not contain when several members of one cluster change in one transition; rebuilding from the final state is order-independent and cannot fail that way | A synthetic transition that changes two members of one cluster evaluates S2 and commits without a missing-code error; the final code/energy equals a brute-force rebuild |
| D-11 | A cluster instance's member slot is resolved from the slot's **member site** and the instance centre's unique pair image (the member-site fallback is used only for multi-centre tables); one instance per slot-0 link | The v1 payload drops the original's per-environment `PairIndexCoding` (slot → pair id) and stores the member link only for the representative placement. **Soundness (review F-38):** (i) the cluster reduction enumerates the **complete product** of the allowed centre and member occupation sets and canonicalises under the representative centre stabilizer's induced site permutations, so every allowed tuple is in exactly one orbit; (ii) the applied energy is invariant under the **full space group**: the other centre position is an image of the representative under an operation that maps the cluster geometry to itself, so its slot particles are the transported tuple, whose orbit carries the same energy; (iii) the approved `code_expansions` map resolves the transported raw tuple to that orbit's canonical index. The centre-site stabilizer alone is not the argument — it canonicalises the representative table; the full-group invariance plus the complete tuple product is what makes the transported instance exact | The multi-centre centred-model fixture resolves four instances and asserts a non-zero/known synthetic per-instance energy (the sample tables are zero), so the code→energy mapping is evidenced; an ambiguous/absent image is rejected with `ClusterGeometry` |

## 15. Interfaces consumed by later steps

| Consumer | Interface | Step 14 obligation |
| --- | --- | --- |
| 15 | `node_energy(EnvId, particle) -> f64`; `incoming(EnvId) -> &[IncomingLink]`; `owner_link(owner, link_id) -> (&Link, EnvId)`; `cluster_energy(instance, cell, particle)`; `cluster_code(instance, cell)`; `cluster_lookup(instance, raw_code) -> Result<u32, ContextError>` (wraps `ClusterTable::lookup` and maps the two failures to the §13 errors); `occupation_code(instance, centre_cell, overrides)`; `centres(position) -> &[u32]`; `link_range(PositionId)`; `update_particle_ids(PositionId)`; `field_scale`, `field_direction`; `cell_vectors`; `&mut` mutators for particles/flags/pools | Tables/codes/vectors/links/inverse in fixed order; no delta logic; the cluster lookup is the amendment-2 contract, never solver-side symmetry |
| 16 | `pools` (ordered, supercell members, with `selectable_jump_count`), `set_selectable`, `direction_for`, `path_len`, `resolve_node`, `jump_links(status)`, `allowed_mask_of`, `goals`, `normalization`, `dt`, `rng` | Pool/jump-status construction and the exact-occupation data; no selection or acceptance |
| 17 | `pools` (empty for MMC), `interaction_range`, `incoming(EnvId)`, `linked(a,b)`, `node_energy`, `cluster_energy`, counters | Same runtime, no MMC-specific field or time; the pair/cluster delta rule is Step 15's |
| 18 | `trackers.mobile_positions`, static/global index space, counters, `lattice_energy_ev`, `rng.export_state`, `time_s`, `normalization`, `model.signature`, `job.seed` | Index space and values Step 18 reads; no `.mcs` |
| 19 | `InitOutcome`, `ResumeState`, `ContextError`/exit mapping, `MemoryReport` | Resume hook and errors; no CLI option |

`linked(a: EnvId, b: EnvId) -> bool` is defined for Step 17 (review F-25) as: the payload has a pair interaction
with owner `a` and partner `b`, **or** owner `b` and partner `a` — either direction, no reverse-link synthesis. It
is implemented by scanning `link_range(position(a))` for a link whose resolved partner is `b` and, if none, by
scanning `incoming(a)` for an `IncomingLink` whose owner is `b`; both use the §6.1 resolution helpers. It is a
local-update membership test for MMC, not an energy shortcut: the energy of the pair is still the owner's table
row (§7.2). A link that is absent in both directions means the two environments do not interact in v1's payload.

## 16. Implementation tasks

- [x] Create the `mocassin-solver` context module layout (§2.2 of `code-structure.md`) with the `ContextOptions`,
      `ContextLimits`, `ContextError`, `MemoryPlan`/`MemoryReport`, `EnvId`, and `Dimensions`; make
      `Cargo.toml` match §22 (`mocassin-msl` dev-only; no `mocassin-state`, `mocassin-extension`, or `rayon`).
- [x] Implement the lattice/environment arenas, index/wrap helpers, flags, tracker id space, and cancellation.
- [x] Implement the memory plan (checked bytes for every derived array, per-array caps, total cap) and reject
      before the first allocation.
- [x] Implement the owner-major per-P1 link CSR, the inverse routing index (i64 `rem_euclid` resolution),
      partner resolution, sorting, and the cluster associations.
- [x] Implement the **one-instance-per-slot-0-link** cluster runtime: centre/member-site validation, the
      per-instance slot resolution (candidate position, then member-site image), the centre index, codes,
      code ids, and energies; reject an absent/ambiguous image with `ClusterGeometry`.
- [x] Consume the approved cluster-code expansion contract: build the raw tuple, call
      `ClusterTable::lookup`, store `Canonical(index)` as `code_id`, map `MissingFromMap` to
      `MissingOccupationCode` and `ExpansionMapAbsent` to the distinct recompile-required error; no solver-side
      symmetry and no direct `occupation_codes` fallback. Gate this task on the `step-11.1` merge (see §22).
- [x] Implement the site table, per-owner site+pair vectors, per-instance cluster energies, and the exact
      initial lattice energy.
- [x] Implement the supercell selection pools (`EnvId` members, global slot map, `pool_of`, `max_pools` check,
      per-pool reserved capacity), mask/optimizer application, selectable count, and `set_selectable`.
- [x] Implement the per-`(P, direction)` receiver-major jump status (`sender_path_id`, `receiver_path_id`,
      `link_id`), the counting pass and caps, the derived bound, and `resolve_node`; retire the Step 13
      `DirectionPool`/`JumpLink` placeholders or adjust them as §8/§9.2 describes. Record the **public Rust API
      break** in the feature log and update the `mocassin-types` container invariant tests in the same feature.
- [x] Implement the goals (`total/100`, zero-total error), counters, `K_fix`/`K_auto`/`K_norm`, `nu_max`
      (exit 13), `dof`, and `dt`.
- [x] Implement the resume hook (`ResumeState` with `seed_hash`, parameterized `model_signature`, validation,
      re-derived mobile mapping, `AlreadyCompleted`).
- [x] Add the real-writer 10×10×10 acceptance fixture, the **multi-centre cluster fixture** derived from the
      centred sample, the populated-model 10×10×10 clone benchmark, context-summary snapshots, the allocation
      test, the `compile_fail` `!Sync` doctest, the hostile-offset test, and the `context` Criterion benchmark
      with a same-class baseline entry.
- [x] Update the documentation, parity rows, status dashboard, and closeout evidence; write the
      `msl-format.md`/`troubleshooting/errors.md` notes that carry the OD-1 writer follow-up.

## 17. Acceptance criteria

- [x] `msl-v1` job 1 (6×6×6 ceria) and `amend-1-net10` initialize a context whose mobile count, selectable count,
      `prerun`/`main`/`total` goals, block size, `dt`, and tracker counts are asserted against computed values.
- [x] `step-13-populated` KMC and MMC jobs initialize; the pair link set and the inverse set equal a brute-force
      expansion of the payload for every supercell cell; the clustered energy vectors equal a direct table
      lookup; the MMC context has no pools/jump status/time.
- [x] `step-13-centered` initializes; a direction mapped into another start bucket resolves its path at the
      selected bucket; the per-node allowed masks are exposed unchanged.
- [x] The multi-centre cluster fixture initializes with **one instance per slot-0 link** (four instances for the
      centred model's four-position centre site), each with its own slot positions, code, and centre energy; the
      per-instance codes equal a brute-force rebuild from the instance's slots; the committed sample, package
      summary/SHA-256/payload set, README provenance, .NET rebuild test, and regeneration command are present
      (§6.4/F-36).
- [x] The amendment-2 two-member ceria fixture (`step-11-amend-2`) resolves **all eight** raw assignments,
      including the two non-canonical ones; the pre-map fixture returns the distinct `ExpansionMapAbsent`
      (recompile-required) error for those two, and a present map that lacks a tuple returns
      `MissingOccupationCode`; the context never re-derives symmetry (review F-29).
- [x] A multi-centre member whose image is absent or ambiguous returns the documented `ClusterGeometry` (exit 12)
      naming the instance, slot, and member site; a unique rotated multi-centre image is **accepted**, not
      reported as a cross-image (review F-35).
- [x] A transition evaluated on a package whose pair table is **asymmetric** (`t[a][b] ≠ t[b][a]`) and that has
      no reverse link produces the owner-major delta; the result differs from the sender-major delta that the
      draft would have produced.
- [x] A pool contains members in two different supercell cells; selecting, removing, and re-adding each member
      addresses the right environment and never collides in the slot map; no pool insert reallocates (reserved
      capacity).
- [x] A hostile package with `i32::MIN`/`i32::MAX` pair offsets initializes or fails cleanly, never panics, and
      the wrapped target is correct.
- [x] The 10×10×10 real-writer fixture initializes in `< 100 ms` (release, warm, same-runner-class) and the
      reported memory total is within budget; the populated-model 10×10×10 clone is measured in the same run
      and recorded, because the ceria fixture has no pair/cluster tables.
- [x] The initial lattice energy matches a hand-computed site/pair/cluster system exactly on a **multi-cell**
      lattice (at least two cells), so the instance × cell cluster sum is exercised (review F-33).
- [x] A job with `target_mcsp`/`pre_run_mcsp` values whose products or sum overflow `u64` returns the documented
      exit-12 `InvalidInput` (`goal overflow`), never a wrapped goal (review F-37).
- [x] The context summary (counts, link histogram, pool sizes, jump-status sizes, goals, `dt`) is byte-identical
      across two initializations of the same package.
- [x] `MemoryPlan` rejects an over-cap package before any allocation; `plan == report` on planned lengths; the
      capacity diagnostic is recorded.
- [x] `SimulationContext` is `Send` and not `Sync` (the `compile_fail` doctest fails to compile).
- [x] Zero selectable directions, zero mobile particles, a zero total goal, a bad cluster geometry, a missing
      occupation code, a context-limit overflow, a cancelled initialization, and a mismatched resume each return
      the documented
      error/exit.
- [x] Resume restores lattice, counters, RNG, time, and normalization exactly; `RawMax` is preserved; a
      completed state returns `AlreadyCompleted`.
- [x] A counting allocator observes zero allocations in the hot accessors and the pool add/remove helpers after
      construction.
- [x] `MemoryReport` reports each array's planned length × `size_of` and equals the plan (`plan == report`);
      summed capacity bytes are a diagnostic (`>=` plan), not the equality target (review F-32).
- [x] Documentation written: `solver-architecture.md` (context section), `physics/energies.md` (total-energy
      convention), `performance/index.md` (context budget), `troubleshooting/errors.md` (context failures),
      `testing-and-parity.md` (tiers/fixtures), `msl-format.md` (cluster-link limitation and runtime expansion).

## 18. Documentation

Implementation-time documentation (the planner records only the design here):

- `docs/development/solver-architecture.md` — context arenas, owner-major link CSR and inverse index, cluster
  runtime, energy
  vectors, pools, jump status, goals/normalization, resume, memory (the design section is written at this
  freeze; the implementor reconciles it with the shipped code).
- `docs/physics/energies.md` — the total lattice energy convention (site once, pair 0.5, cluster at centre) and
  the initial-energy definition.
- `docs/performance/index.md` — the context-initialization budget, the measured medians, the memory report, and
  the same-runner-class baseline entry. (The Step 14 draft's `docs/performance/benchmarks.md` does not exist;
  this page is the performance page.)
- `docs/troubleshooting/errors.md` — zero-mobile/zero-direction, cluster-geometry rejection, the pre-map
  recompile-required error, context limits, resume mismatch, and the exit codes.
- `docs/development/testing-and-parity.md` — the Step 14 fixtures, snapshot/allocation/benchmark tiers.
- `docs/reference/msl-format.md` — the cluster-link limitation, the `code_expansions` resolution map, the
  per-slot-0-link instance rule, and the center-only cluster semantics.
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
signature), which exercises the owner-major/inverse links, clusters, and pools at 3,000 positions without a
second fixture. Because the ceria writer fixture has no pair or cluster tables, this clone is the recorded
measurement for the pair/cluster-scale budget (§20), not the real-writer fixture.

Feature 6 also adds `tests/contracts/packages/step-14-multi-centre-cluster/`, a real-writer test artifact built
from the committed `samples/step-14-multi-centre-cluster/step-14-multi-centre-cluster.mocprj` (a derivation of
the centred sample, which stays byte-identical, so the `step-13-centered` golden does not move) with one cluster
centred on the four-position site and **two distinct member sites** (B and C). The project model's `ENM008`
validator requires 2–8 surrounding offsets, so the design's literal one-member cluster cannot be authored; using
two **images of one member site** is the rejected ambiguous OD-1 case, so the implemented fixture adds a third
stable site C and two distinct member offsets (`[0,0,0.05]`, `[0,0,0.1]`). This is a recorded fixture-authoring
deviation; it still exercises the multi-centre member-site fallback and the four slot-0 links unchanged. The
artifact set is the same shape as the 10×10×10 fixture: `.msl`,
`.sha256`, `package-summary-v1.json`, `payloads/index.json`, `payloads/model-1.bin`, a README with provenance
(writer commit, SDK, `created_utc`, job count), a .NET rebuild test, and the regeneration command. It is the F-17
acceptance fixture: the centre site's four P1 positions produce four slot-0 links, hence four runtime instances,
each resolving its own member image. A second synthetic case in the same test (built in Rust from the real
model, not a new package) makes the member image absent or ambiguous and asserts the documented
`ClusterGeometry`. The two-member non-P1 code-expansion case is the amendment-2 ceria fixture
(`tests/contracts/packages/step-11-amend-2/`, with `tests/contracts/packages/amend-2-pre-map/` for the absent
map), not a second multi-centre package: two images of one member site are the ambiguous case above (F-36).

| Test | Assertion | Tier |
| --- | --- | --- |
| Index/wrap | `(a,b,c,P)` ↔ linear index round-trip; `wrap` for negative and oversized offsets; `A*B*C*P` checked; `i32::MIN`/`i32::MAX` offsets do not panic and wrap correctly | PR-fast |
| Links | The owner-major link set, the inverse set, and the sorted order equal a brute-force payload expansion; the partner resolves for every cell; a duplicated image is kept; every forward link has exactly one inverse entry | PR-fast |
| Cluster geometry | Slot set/centre/contiguity validation; slot 0 is `[0,0,0]`; one instance per slot-0 link; a single candidate position resolves; the member-site fallback resolves; absent/ambiguous images fail with `ClusterGeometry`; a missing current code fails | PR-fast |
| Cluster instance cardinality | The centred multi-centre fixture yields one instance per centre position, per-instance slot positions/cells/codes, and a per-instance energy vector; a table with one centre yields one instance | PR-medium |
| Cluster energy | `cluster_energy(instance, cell, p)` equals a direct table lookup for every allowed centre particle; `occupation_code` rebuild equals the stored code | PR-fast |
| Cluster code lookup | On `step-11-amend-2`, all eight raw assignments resolve via `ClusterTable::lookup` to the expected canonical index (including the two non-canonical tuples); on `amend-2-pre-map`, the six canonical tuples resolve and the two non-canonical ones return the distinct recompile-required error; a present map with a removed tuple returns `MissingOccupationCode`; no test path sorts/mins the slot tuple or calls a reduction | PR-fast |
| Cluster code→energy mapping | A synthetic table with distinct energies per canonical code gives each instance's `cluster_energy` the expected non-zero value for its raw tuple; the sample zero tables are not the evidence | PR-fast |
| Energy vectors | `node_energy` equals a brute-force site+pair sum over the owner's links; the row is the owner's particle | PR-fast |
| Delta orientation | With an asymmetric pair table and no reverse link, the owner-major delta equals a brute-force recomputation and differs from the sender-major value; a missing reverse link is not an error | PR-fast |
| Initial energy | Hand-computed site+pair+cluster system on a multi-cell lattice matches exactly (instance × cell cluster sum); the debug recomputation agrees | PR-fast |
| Pools | Direction-count buckets; mask filtering; optimizer removal; two-cell members; swap-remove slot update; `selectable_jump_count`; re-add after a particle change; `max_pools` rejection; reserved capacity (no reallocation) | PR-fast |
| Jump status | Link set equals a naive receiver-major recomputation for every cell of a small lattice; the structural receiver filter; `L*(L-1)` bound; `resolve_node` for a cross-bucket direction | PR-fast |
| Goals/normalization | `prerun`/`main`/`total`/block (`total/100`), `K_fix`/`K_auto`/`K_norm`, `dof`, `nu_max` (exit 13 on zero/NaN), `dt` arithmetic; a zero total rejects; `u64` product/sum overflow rejects (`goal overflow`, exit 12) | PR-fast |
| Bounds/cancel | Each limit and the memory plan reject before allocation; `plan == report` on lengths and the capacity diagnostic is recorded; a set cancellation flag returns `Cancelled` | PR-fast |
| Resume | Exact restore; parameterized-signature/seed-hash/size/mapping mismatches rejected; `RawMax` preserved; `AlreadyCompleted` | PR-fast |
| `Send`/`!Sync` | A `compile_fail` doctest cannot assert `SimulationContext: Sync`; a positive assertion requires `Send` | PR-fast (doctest) |
| Mobile assumption | A non-void particle outside every active KMC mobile mask is still `MOBILE`, gets a mobile tracker, and is counted in `mobile_count` | PR-fast |
| Allocation | Zero allocations in hot accessors and pool helpers after construction | PR-fast (own test binary) |
| Real packages | `msl-v1`, `amend-1-net10`, `step-13-populated` (KMC+MMC), `step-13-centered`, `step-11-amend-2`, `amend-2-pre-map`, `step-14-multi-centre-cluster` initialize (or return the documented pre-map error) and satisfy §17 | PR-medium |
| 10×10×10 acceptance | The real-writer fixture initializes in `< 100 ms`; the memory report is within budget | PR-medium |
| Populated 10×10×10 clone | The populated model cloned to 10×10×10 (3,000 positions) is measured for init time and memory; recorded next to the table-free fixture because the ceria model has no pair/cluster tables | PR-medium |
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
| Context init, populated-model 10×10×10 clone (3,000 positions, pair/cluster tables) | `< 100 ms` |
| Context init, `msl-v1` 6×6×6 and `step-13-populated` 2×2×2 | `< 20 ms` each |
| Context memory total (`MemoryReport`), 10×10×10 fixture and populated clone | `≤ 64 MiB` each |
| Context memory total, 500,000 environments × 64 particles (limit case) | `≤ max_context_bytes` = 1 GiB total. A passing example: 500,000 **environments** (≈10,000 P1 positions × 50 cells) × 64 particles, 8M pair links, 2M statuses, no clusters ≈ 660 MiB (energy 244 + site ≈5 (4.9, `P·pc·8` on 10,000 P1 positions) + forward links 214 + inverse 61 + slot map 122 + jump 8). The `energy` array is sized by environments (`A·B·C·P`) and `site` by P1 positions (`P`); the earlier wording used "positions" for both. The earlier "≤ 512 MiB energy vectors + links" row was arithmetically impossible at the per-array caps (16M links alone are 448 MiB forward + 128 MiB inverse); the plan's total cap replaces it. The table-free 10×10×10 ceria fixture is not a pair/cluster-scale measurement — the populated clone is, and both are recorded. |
| Allocation after construction | 0 in hot accessors |
| A same-runner-class Criterion baseline | `tests/reference/bench/baselines.json` `rustContext` entry; > 10 % regression is a release failure |

Evidence: `tests/reference/bench/step-14-context.json` plus the raw Criterion log, and a measured process peak
RSS (Linux `VmHWM`, recorded as non-gating context next to the memory report). Absolute timings are compared
only within the recorded runner class; a different CPU/OS/Rust version records its own entry.

## 21. Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Cluster member offsets are not in `.msl` v1 (OD-1) | Fail closed on zero/multiple images; the §6.3 lookup cross-check; recorded limitation and future format decision; the fixtures pass. The accepted rotated multi-centre member-site image is distinguished from a rejected cross-image in §6.4/OD-1 (review F-35) |
| Energy convention differs from the original | Documented deviation D-3 with a hand-computed test; Step 15 uses the same decomposition; Step 29 parity covers it |
| Link-graph memory at 500k positions | Owner-major CSR + inverse, `MemoryPlan` before allocation, total cap, `MemoryReport`, nightly stress |
| Delta applied with the wrong table or row | Owner/receiver-major rule stated once (§6.1/§7.2/§9.2) and tested with an asymmetric table and a one-directional link |
| Derived-array blowup (`K*C`, `P*D`) | Every array planned and capped before allocation; the counting pass rejects before the `Vec` exists |
| Jump-status staleness or overflow | Structural receiver filter, derived bound with an invariant error, `max_status_count`/`max_jump_links`, Step 16 test |
| Pool update bugs / key collision | Supercell `(env, particle)` members, one global slot map, `pool_of` per P1, two-cell test, invariant checks (total = Σ counts) |
| Resume drift / silent reset | Parameterized signature + seed hash + size/mapping checks, `RawMax` preservation, `AlreadyCompleted`, Step 18 fixtures |
| f64 non-determinism | Fixed summation order, tie-broken sort, no parallel init, no FMA changes |
| API change after Step 15 starts | API frozen at feature 3; a rework is a stop-and-report re-review trigger |
| Zero-selectable/zero-mobile/zero-total packages | Explicit exit-12 errors with actionable messages and a troubleshooting section |
| Fixture absence for pair/cluster scale | The real-writer 10×10×10 fixture is table-free (ceria); the populated-model 10×10×10 clone is measured separately and the limitation is recorded |
| Multi-centre cluster instance drift | One instance per slot-0 link, per-instance slot resolution and codes; the centred-model fixture and the ambiguity test cover it; the writer diagnostic is the follow-up |
| Cluster code lookup for symmetry-reduced tables (F-29) | Consume the approved `code_expansions` map via `ClusterTable::lookup`; no solver-side symmetry or direct-index fallback; the amendment-2 two-member fixture resolves all eight assignments and the pre-map fixture exercises the recompile-required error; the reader expansion budget bounds a hostile map |
| OD-1 rejection only at solver start | Writer follow-up recorded in OD-1 (cross-image, multi-centre ambiguity); `msl-format.md` and `troubleshooting/errors.md` notes scheduled in §18 |

## 22. Dependencies and interfaces

- **Step 13 (Done, merged and tagged `step-13`):** `JobPackage`, `ParameterizedModel`, `InitialLattice`,
  `EnvironmentModel`/`PairLink`/`ClusterLink`, `EnergyModel`/`PairTable`/`ClusterTable`, `TransitionModel`,
  `Rng`/`RngStateV1`, `ReaderLimits`, the real-writer fixtures, and the exit-code model. Step 14 consumes them
  unchanged and does not re-read the package. The type-only `DirectionPool`/`JumpLink` placeholders are adjusted
  or retired as §8 and §9.2 describe (no reader constructs them); the **public Rust API break** (removed
  `DirectionPool`, changed `JumpLink` fields) is recorded in the feature log, and the `mocassin-types` invariant
  tests are updated in the same feature (review round 2 accepted the change as type-only evolution).
- **Crate dependencies (review F-15).** `mocassin-solver/Cargo.toml` currently declares `mocassin-state`,
  `mocassin-extension`, `rayon`, and a normal `mocassin-msl`. Feature 1 makes the manifest match the design:
  `mocassin-msl` becomes a dev-dependency (fixtures only), and `mocassin-state`, `mocassin-extension`, and
  `rayon` are removed. `mocassin-state` returns in Step 18 (`.mcs` decode), `mocassin-extension` in Step 19,
  and `rayon` only in Step 29 with a measured parallel initialization. The AGENTS.md/architecture target graph is
  the end state, not the Step 14 state; no new external crate is added.
- **Step 11/12 (Done) and `step-11-amend-2` (approved, merged `2f3a6fe`, design freeze `bf273ff`):** the `.msl`
  v1 schema and payload field set, plus the additive per-table `code_expansions` map
  (`expanded_codes`/`canonical_indices`), the reader expansion budget, and the
  `ClusterTable::lookup`/`ClusterCodeLookup` runtime contract. Step 14 adds no field and no version.
- **`step-11.1` (implementation dependency, review F-29).** The amendment contract is approved but the
  `step-11.1` implementation is running; the `mocassin-types` `ClusterTable::lookup` API, the `ClusterExpansions`
  runtime member, the reader budget, and the `step-11-amend-2`/`amend-2-pre-map` fixtures must be merged before
  Step 14 feature 2's cluster work starts. If Step 14 is started earlier, feature 1 (skeleton/arenas/limits) may
  proceed, but the cluster code/energy work is blocked. **No fallback direct `occupation_codes` lookup is
  permitted**: it is exactly the F-29 regression. The F-29 closure is a Step 14 gate (it is not a `step-11.1`
  acceptance criterion), and the start gate is expected after `step-11.1` is merged and tagged.
- **Step 15 (next):** owns S0/S1/S2, pair/cluster deltas, the field term, state application, and the incremental
  lattice energy; consumes §15's interface, the owner-major/inverse rule, and the canonical `code_id`. It never
  calls `ClusterTable::lookup` directly after initialization: run-time raw-tuple resolution goes through the
  context's `cluster_lookup` wrapper (§15), which preserves the `Canonical`/`MissingFromMap`/`ExpansionMapAbsent`
  semantics (review F-41).
- **Step 16:** owns selection, rule lookup, the exact per-direction occupation check, acceptance, normalization
  updates, time, pre-run, aborts, and resume orchestration; consumes §9.4 and §15.
- **Step 17:** MMC selection, exchange, Metropolis, break check; consumes the same runtime with no pools/jump
  status/time, and the same incoming-link rule for pair/cluster updates.
- **Step 18:** tracker values, histograms, `.mcs` writer/reader; consumes the tracker index space, the
  parameterized signature and `job.seed` for the `ResumeState` decode.
- **Step 19:** the CLI run path, batch, extensions; maps `InitOutcome`/`ContextError`.
- **Step 29:** statistical parity for the documented deviations, the > 10 % benchmark job, and the optional
  static-S2/mobile-link/parallel optimisations.

## 23. Reconciliation

- [`01-target-architecture.md`](../01-target-architecture.md) §4: the solver context is built once per job from
  the validated `JobPackage`; the link graph is translation-compact with an inverse routing index; the crate
  dependency change is the Step 14 state, and §7.3 gains the exit-12/14 distinction.
- [`02-data-contracts.md`](../02-data-contracts.md) §3.3: pair links are centre/owner-major with the table row =
  owner particle; cluster links carry the environment's own slot and no member offset; a cluster table may have
  one slot-0 link per P1 position of its centre site; cluster tables are 1-D per occupation code with the centre
  particle in slot 0, `particle_table_mapping` as the ascending allowed centre-particle list, and the optional
  `code_expansions` map; Step 14 maps `MissingFromMap` to a data error and `ExpansionMapAbsent` to the distinct
  recompile-required error. No field change in Step 14.
- [`step-11-amend-2`](../amendments/step-11-amend-2-cluster-code-expansion.md): the approved cluster-code
  expansion contract that Step 14 consumes; F-29 is closed against it (implementation in `step-11.1`).
- [`03-quality-strategy.md`](../03-quality-strategy.md): §9.12 for the context layer (links/inverse, pools,
  jump status, memory plan) and §13 rows for both Step 14 benchmarks.
- [`04-roadmap.md`](../04-roadmap.md) §3.1: the recorded complexity class and the owner-acceptance request.
- [`05-open-decisions.md`](../05-open-decisions.md): OD-1 (accepted v1 limitation plus the writer follow-up) and
  OD-2 (accepted exact accounting), and the Step 14 class acceptance row.
- [`appendix-b-parity-matrix.md`](../appendix-b-parity-matrix.md): the new solver row and the selection-optimizer
  note.
- [`code-structure.md`](../../development/code-structure.md) §2.2: the `mocassin-solver` module layout.
- [`solver-architecture.md`](../../development/solver-architecture.md): the context section and the ownership
  table.
- [`physics/energies.md`](../../physics/energies.md): the total-lattice-energy convention and the owner-major
  delta convention.
- **No change:** `cli.md` (no CLI surface), `msl-format.md` (implementor adds the limitation note),
  `contracts/manifest.json`, ADRs, and the status dashboard (step-agent-owned; it is updated at the start gate).

## 24. Done when

A validated job package becomes a fully initialized context — lattice, environment states, owner-major links
with an inverse routing index, clusters, site/pair/cluster energies, exact initial lattice energy, supercell
selection pools, receiver-major KMC jump status, tracker index space, counters, goals, normalization, time step,
RNG, and the resume hook — with the documented errors, a memory plan that rejects before allocation, bounded and
reported memory, deterministic structured state, real-package acceptance, the allocation and `!Sync` proofs, the
recorded benchmarks, and the documentation and parity updates.
