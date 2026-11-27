# Step 15 — Energy Engine

**Phase:** D. Solver  
**Depends on:** 14 (Done — merged as `dd5a811`, tagged `step-14`; audit `Passed with notes`, owner-accepted
2026-11-21, findings F-1–F-3/N-1–N-3 deferred to the maintenance branch `fix/step-14-structure-and-tests`, which
is merged into `main` as `b632565` — the precondition is satisfied)
**Delivers:** The transition energy calculator: S0/S1/S2, pair/cluster deltas, electric-field terms, state
application, incremental lattice-energy bookkeeping, the MMC exchange energies, and the acceptance-exponential
kernel (libm default, fast-exp behind the job flag).
**Design class:** **Full** — the step adds a new public Rust library API (`EnergyEngine`, `TransitionEnergies`,
`ExchangeEnergies`) that Steps 16, 17, and 18 consume, and freezes irreversible scientific conventions (the
owner-major delta application, the S2 evaluation semantics, the field-term application, the incremental
lattice-energy decomposition); both are `Full` triggers in
[`docs/development/design-proportionality.md`](../../development/design-proportionality.md) §1 ("public API",
"irreversible decision"). No `.mocprj`/`.msl`/`.msr`/`.mcs` format, schema, or version changes, no new
dependency, no cross-package interface, and no data migration apply, which narrows the review to API and
physics consistency rather than contract migration.
**Complexity class:** **High** — see §0.2 for the recorded justification, the split decision, and the
owner-acceptance request per
[`04-roadmap.md` §3.1](../04-roadmap.md#31-step-sizing-and-split-triggers).

## Implementation status

- **Status:** In progress — the human owner accepted the round-2 `Ready with notes` verdict, the High complexity
  class with no split, and the four-feature list on 2026-11-27
- **Step branch:** `step/15-energy-engine` (created from the merged design `main` commit `895b20b`;
  `main` `d2ba15f` synced as `6952e37` with the combined gates green)
- **Precondition:** **Satisfied.** `fix/step-14-structure-and-tests` (the Step 14 audit maintenance branch: F-1
  `amend-1-net10` test, F-3 context file split, N-1 dashboard wording, N-2/N-3 robustness) is merged into `main`
  as `b632565` (verified 2026-11-27). F-3 split `src/rust/crates/mocassin-solver/src/context/mod.rs` (now 770
  lines) and `src/rust/crates/mocassin-solver/src/context/limits.rs` (now 737 lines) below the 800-line limit and
  added `context/build.rs`, `context/invariants.rs`, and `context/limits/tests.rs`. The energy engine lands in
  that split layout and must not re-grow any context file past the limit.
- **Step CI:** —
- **Main merge:** —
- **Step tag:** —
- **Audit:** Not requested
- **Design review:** Round 1 `Changes required` (2026-11-22, reviewed `31d5e34`, record commit `04f9a0a`);
  round-2 `Ready with notes` at design freeze `af9ded8` (record `3bec909`) — **accepted by the human owner
  2026-11-27**. See [`docs/planning/reviews/step-15.md`](../reviews/step-15.md).
- **Last updated:** 2026-11-27 (feature 1 squashed as `49ca6af`)

| Feature | Branch | Status | Local gates | Step CI | Squash commit |
| --- | --- | --- | --- | --- | --- |
| 1 Transition evaluation | `feature/step-15-energy-engine/transition-evaluation` | Squashed | All required feature gates pass; see evidence below | — | `49ca6af` |
| 2 State application | `feature/step-15-energy-engine/state-application` | Planned | — | — | — |
| 3 MMC exchange | `feature/step-15-energy-engine/mmc-exchange` | Planned | — | — | — |
| 4 Benchmark, docs, and closeout | `feature/step-15-energy-engine/benchmarks-docs-and-closeout` | Planned | — | — | — |

### Feature 1 evidence

- **Branch / squash:** `feature/step-15-energy-engine/transition-evaluation` → `49ca6af361da38e47a7ae804bf31b423a8227ed8`.
- **Diff summary:** adds the `energy/` module with `EnergyEngine`, `TransitionEnergies`, the read-only S0/raw-S1/S2
  evaluator, and the acceptance-exponential kernel; adds 24 focused integration tests plus real-fixture support;
  updates the feature-1 physics, architecture, testing, error, status, and parity documentation. F-10's filter
  attribution and F-11's pre-overwrite `C` capture point are folded into the frozen step text.
- **Gates:** `cargo fmt --manifest-path src/rust/Cargo.toml --all --check`,
  `cargo clippy --manifest-path src/rust/Cargo.toml --workspace --all-targets -- -D warnings`, and
  `cargo test --manifest-path src/rust/Cargo.toml --workspace` all pass. `./tools/verify.sh structure` passes
  (9/9), `./tools/verify.sh docs --require` passes (strict Zensical, DocFX, rustdoc, links/math),
  `python tools/check_process_consistency.py` reports `Process consistency: OK`, and
  `./tools/verify.sh policy --require` passes (API, contracts, process, licence, secrets, dependency policy).
- **CI:** none; feature branches use local gates only.
- **Limitations / next:** state application, MMC exchange, the allocation proof, and the Criterion benchmark are
  deliberately not in feature 1. `ExchangeEnergies` and the commit methods land with features 2–3; feature 4 owns
  allocation/benchmark/closeout evidence. Feature 2 may start from this squash.

## Goal

Compute the energy change of a proposed transition exactly and efficiently, with a clear unit convention and
tests against hand calculations. This is the physics core shared by KMC and MMC: Step 16 and Step 17 draw,
classify, and accept; this step tells them what the energies are and commits what they accept.

## Original reference

Read `/workspace/mocassin-original` as a read-only reference. Do not copy GPL code or data.

- All energies are kT internally; output converts to eV with `EnergyFactorKtToEv`.
- Initial environment energy: site/defect background; pair tables `[center_particle][partner_particle]`;
  cluster tables `[mapped_row][occupation_code]` with the cluster term folded into the centre environment's
  energy function.
- KMC transition (`SetKmcStartTransitionBaseAndFieldEnergyStatesOnContext`, `MainRoutines.c`):
  - `S0 = Σ_i E[path_i][S0_i]`; `RawS1 = Σ_{i≥1} E[path_i][S1_i]` (node 0 contributes zero);
  - dynamic `S2` is computed by backing up the touched energy entries, applying the state change and the jump
    link deltas, reading the final-state path energies, and restoring; a per-rule **scalar** static correction
    (`StaticVirtualJumpEnergyCorrection`, `NaN` = dynamic) replaces the delta pass when every affected link
    carries no cluster and the delta is state-independent;
  - default `ΔE_conf = S2 − S0`, `S1 = S0 + RawS1 + 0.5 ΔE_conf` (`SetDefaultKmcTransitionStateEnergy`; an
    alpha-interpolation variant exists for plugins and is not the default).
- Field: `E_field = −(direction.ElectricFieldFactor * rule.ElectricFieldFactor)`;
  `b01 = (S1 − S0) + E_field`; `b10 = (S1 − S2) − E_field`; the no-field variants `S1 − S0` and `S1 − S2` feed
  the unstable classification.
- Acceptance: `P_raw = exp(−b01)` (`CalculateExp`: libm `exp`, or the `FastExp32RmsError` bit trick behind the
  approximation flag), `P_norm = P_raw * K_norm`, accept if `P_norm >= U[0,1)`.
- Outcomes: `b10_noField ≤ 0` → end-unstable; `b01_noField ≤ 0` → start-unstable; otherwise accept/reject on
  `P_norm`.
- Lattice energy = `0.5 * Σ_stable E[current]` in eV (pair double-count correction, misapplied to the site and
  cluster terms as well).
- Known quirks: the static correction is a scalar with a NaN sentinel; the jump-link buffer keeps only the
  first link between a path pair (duplicate periodic images with distinct pair tables are undercounted in the
  dynamic S2 delta while the energy vectors sum them); the 0.5 lattice-energy correction is valid only for
  pair interactions; the field sign needs a physical test.

## Design

### 0. Freeze record

#### 0.1 Scope and module layout

**In scope**

- The `mocassin-solver` `energy/` concept module (reserved by
  [`docs/development/code-structure.md`](../../development/code-structure.md) §2.2: "`energy` in `context/` is
  the *runtime tables and vectors*, not the Step 15 delta engine; Step 15 adds `energy/` as its own concept
  module"): `EnergyEngine`, `TransitionEnergies`, `ExchangeEnergies`, the read-only transition evaluation, the
  commit-side delta propagation, the MMC exchange entry points, the acceptance-exponential kernel, and the
  error mapping.
- `apply_state_change`/`apply_exchange`: the fixed-order state application (particles, flags, mobile tracker
  ids, pool membership, owner-vector updates, cluster rebuild) and the on-the-fly exact incremental lattice
  energy.
- The Step 14 audit deferral **F-2**: the owner-major vs transposed/sender-major delta criterion is owned and
  tested here (§9 and the acceptance criteria).
- Tests, benchmarks, documentation, parity rows, and closeout evidence for the above.

**Non-goals (explicitly out of scope)**

- **No KMC or MMC loop.** No selection draw, no acceptance decision, no `exp` call site, no time/MCS advance,
  and no outcome classification (`StartUnstable`/`EndUnstable`/accepted/rejected are Step 16/17 decisions
  made on the values this step returns). The engine never draws from the RNG.
- **No tracker values, histograms, counters, or `.mcs`.** `apply_state_change` maintains the context's
  per-environment index arrays only; Steps 16/17 increment counters and Step 18 owns tracker values, its own
  live inverse mapping, and the state document.
- **No format, schema, version, manifest, or signature change.** No `contracts/manifest.json` edit, no payload
  bump, no new `.msl` field, no new committed fixture package (tests extend the existing real packages and
  hand-build rule/lattice variants in-process, as the Step 14 tests do).
- **No .NET change.** The compiler-side static-S2 computation is explicitly deferred (§3, D-4); the
  `static_correction` payload semantics stay exactly as written today.
- **No new external crate** and **no context memory-plan change**: the engine adds no context arrays, no
  scratch buffers, and no plan entries (§1/§3), because the evaluation is read-only and the commit needs only
  scalar accumulators.
- **No public Step 14 API break.** The engine uses Step 14's public accessors and mutators
  (`set_particle`, `set_selectable`, `node_energy`, `incoming`, `link_range`, `partner_of`, `occupation_code`,
  `cluster_lookup`, `cluster_energy`, `centres`, `field_scale`, `field_direction`, `ev_to_kt`,
  `recompute_lattice_energy_kt`, `resolve_node`/`resolve_path`) plus crate-internal arena helpers added by
  this step (flag derivation, mobile-tracker permutation, cluster code/row rebuild). The public context
  surface is otherwise unchanged.
- **No alpha-method S1** or other interpolation variants; the default midpoint rule only. A future extension
  may add one through the Step 19 ABI; that is not this step's decision.
- **No `OPT_LINK_ONLY_MOBILES`, no parallel evaluation, no fast-math/FMA changes** (deferred with
  measurements, as in Step 14 D-9).

#### 0.2 Complexity class and split decision

**Class: High**, recorded per [`04-roadmap.md` §3.1](../04-roadmap.md#31-step-sizing-and-split-triggers) and
mirrored in the roadmap's "Recorded class — Step 15" note.

- **Contracts and public surfaces:** no persisted format change, but a new public Rust library API
  (`EnergyEngine`, `TransitionEnergies`, `ExchangeEnergies`) consumed by Steps 16, 17, and 18; `missing_docs`
  is denied workspace-wide, so every public item is committed API surface.
- **New dependencies:** none.
- **Cross-step interfaces:** three later steps consume the engine API and the behavioural conventions it
  freezes — the owner-major delta application (§4), the read-only S2 evaluation (§3), the field-term
  application (§2), and the incremental lattice-energy bookkeeping (§5). This is the dimension that makes the
  step High.
- **Irreversibility:** those conventions are scientific/behavioural decisions that are expensive to change
  after merge (fixtures, hand-computed expectations, Step 16/17 building on them, Step 29 parity). The 0.5 pair
  convention and the cluster-at-centre rule are frozen plan content (AGENTS.md §10); the S0/S2 sums, the field
  sign, and the `charge_transport_vector` definition restate the data contract §3.3 and
  [`docs/physics/energies.md`](../../physics/energies.md); the **S1 midpoint rule has its first normative home in
  this page**, restated from the original's default (`SetDefaultKmcTransitionStateEnergy`) and frozen here.
  AGENTS.md §10 does not state S1, ADR 0012 is the transport/MSD decision, and `energies.md` documents the rule
  at implementation time. The formulas are restated, not re-decided.
- **Test-matrix breadth:** hand-computed systems, real-package acceptance, property tests
  (incremental vs. full recomputation), allocation proofs, and a Criterion benchmark on the hot path.
- **Independent acceptance stories:** one — "the engine computes transition and lattice energies correctly and
  commits what the caller accepts". The pair, cluster, site, field, and state-application criteria are parts
  of that one story.
- **Evidence size:** bounded for one audit sitting: four features, one design page, no new fixture packages,
  one benchmark, and the reconciled reference pages.

**Split decision: no split.** The candidate seams all strand a half-engine: "transition evaluation" versus
"state application" would separate the read-only delta sums (§3) from the committing delta pass (§4/§6) that
shares the owner-major table/row rule and the cluster rebuild, duplicating the delta kernel in both halves or
leaving one half untestable against the other; "KMC transition" versus "MMC exchange" would split one shared
kernel at its smallest consumer. Both halves would remain High on cross-step interfaces. The step is therefore
proposed as **one High unit** and requires the owner's explicit acceptance at the start gate; the request and
fallback are recorded in [`05-open-decisions.md`](../05-open-decisions.md) §3 and in the roadmap note.

**Guard (not a time estimate):** the engine API (§1) and the conventions in §2–§6 are frozen at this design. If
the hand-computed acceptance cannot pass without changing the §2 formulas, the owner-major rule in §4, the
field-term definition, or the struct fields in §1, the step agent stops and reports before the next feature; a
silent convention change is a re-review trigger, and a change to a frozen scientific invariant additionally
needs an ADR (AGENTS.md §4.8/§14).

**Round-1 corrections (2026-11-27).** Design-review round 1 found three enumeration defects inside the frozen
§2–§6 conventions: the §3 receiver filter dropped unchanged path nodes (F-1), the §5 unchanged-owner delta lacked
the stability weight (F-2), and the §4/§6 affected-instance route missed centre-only changes (F-3). The fixes
below correct those enumerations; they do not change the energy conventions, the API, or any invariant.

### 1. Energy engine API

```rust
/// The transition-energy view over one simulation context. Zero-sized; allocation-free.
pub struct EnergyEngine<'a> { ctx: &'a mut SimulationContext }

/// The KMC transition energies of one resolved rule application, in kT.
pub struct TransitionEnergies {
    pub s0: f64,
    pub s1: f64,                    // the full S1 = s0 + raw_s1 + 0.5 * conf
    pub s2: f64,
    /// b01 = (s1 - s0) + field_term: the forward acceptance barrier, with field.
    pub barrier_forward: f64,
    /// b10 = (s1 - s2) - field_term: the backward acceptance barrier, with field.
    pub barrier_backward: f64,
    /// -0.5 * field_scale * (rule.charge_transport_vector · ê); zero without a field.
    pub field_term: f64,
}

/// The MMC two-site exchange energies, in kT. `delta = s2 - s0`.
pub struct ExchangeEnergies { pub s0: f64, pub s2: f64 }

impl<'a> EnergyEngine<'a> {
    /// Borrows the context mutably; the view is zero-sized and never allocates.
    pub fn new(ctx: &'a mut SimulationContext) -> Self;

    /// KMC transition energies for one resolved rule application. Read-only:
    /// no context state is mutated during evaluation (§3).
    ///
    /// `selected` is the drawn start environment; the engine resolves the path with
    /// the context's `resolve_node`/`resolve_path` (Step 14's rule, never re-derived)
    /// and the direction data from the shared model by `JumpDirectionId`.
    pub fn transition_energies(
        &self,
        selected: EnvId,
        direction: JumpDirectionId,
        rule: &JumpRule,
    ) -> Result<TransitionEnergies, ContextError>;

    /// MMC two-site exchange energies (no S1, no field, no jump status). Read-only.
    /// `a == b` is a no-op: `s0 == s2`; Step 17 filters it as rejected before calling.
    pub fn exchange_energies(
        &self,
        a: EnvId,
        b: EnvId,
        new_a: ParticleId,
        new_b: ParticleId,
    ) -> Result<ExchangeEnergies, ContextError>;

    /// Commits the KMC state change (§6). Resolves every affected cluster code
    /// before the first mutation, so an error leaves the context unchanged.
    pub fn apply_state_change(
        &mut self,
        selected: EnvId,
        direction: JumpDirectionId,
        rule: &JumpRule,
    ) -> Result<(), ContextError>;

    /// Commits the MMC exchange (§7); same resolve-before-mutate rule.
    pub fn apply_exchange(
        &mut self,
        a: EnvId,
        b: EnvId,
        new_a: ParticleId,
        new_b: ParticleId,
    ) -> Result<(), ContextError>;
}
```

- **Evaluation is read-only; application commits.** `transition_energies`/`exchange_energies` take `&self` and
  mutate nothing (§3 explains why the original's backup/apply/restore is replaced by direct sums). The engine
  holds `&'a mut SimulationContext` because `apply_state_change`/`apply_exchange` commit: particles, flags,
  mobile tracker ids, pool membership, owner energy vectors, cluster codes/rows, and `lattice_energy_ev`.
- **No `apply` flag on the evaluation.** The caller cannot know whether to commit before the energies exist:
  the contract is evaluate → decide (Step 16/17) → apply. A fused `apply=true` call would either recompute the
  evaluation or fuse the acceptance decision into this step, both wrong.
- **`Result`, not panic.** Run-time cluster-code resolution can fail (`MissingOccupationCode` for a tuple
  absent from a present map, `ExpansionMapAbsent` for a pre-map package whose reached tuple is non-canonical —
  a legal initialization can reach it during a run, Step 14 D2-2), and a non-finite field term is exit 13.
  Both propagate as `ContextError` with the Step 14 exit mapping. The commit methods re-resolve every affected
  code before the first mutation, so a failed apply never leaves a half-committed context.
- **Composition (Step 14 reviews F-16/F-19, stated once; round-1 F-4).** Exactly one mutable borrow of the
  context exists at any time. A loop context that owns the context constructs a short-lived
  `EnergyEngine::new(&mut *self.ctx)` per call (Step 16's form; the reborrow is free). That per-call form is the
  **only** supported composition: `EnergyEngine`'s `ctx` field is private and the engine exposes no context
  accessor, so a loop context cannot host the engine view and reach the context through it. No struct may hold
  both `ctx: &'a mut SimulationContext` and an `EnergyEngine<'a>` — two simultaneous mutable borrows, which the
  compiler rejects. Step 17's current §1 sketch
  (`MmcEngine { ctx: &'a mut SimulationContext, energy: EnergyEngine<'a> }`) is that forbidden form; Step 17 must
  adopt this per-call form at its own design review. Until then the cross-step interface is resolved here for
  Step 16 and qualified for Step 17 (round-1 F-4).
- **Allocation and memory.** The engine is zero-sized, adds no context arrays, no scratch buffers, and no
  `MemoryPlan` entries. All locals are fixed-size (path ≤ 10 nodes, cluster slots ≤ 9, override lists bounded
  by both). A counting allocator must observe zero allocations in evaluation and commit (§10).
- `env` arguments are `EnvId` (absolute supercell environments); `PositionId` remains the P1 index and is used
  only through the context's P1-keyed accessors.
- The acceptance-exponential kernel is a free function of the `energy` module (§8.2); Steps 16/17 call it with
  the job's `use_fast_exp` flag.

### 2. Transition energies

Given a direction with path positions `p_0..p_{L-1}` (resolved at the selected environment) and a rule with
state codes `S0,S1,S2`:

$$
\begin{aligned}
s_0 &= \sum_i E[p_i][S0_i] + \sum_{c} C[c][S0_c] \\
\mathrm{raw}\,s_1 &= \sum_{i=1}^{L-1} E[p_i][S1_i] \qquad (p_0\text{ contributes zero}) \\
s_2 &= \text{the S2-state energies, computed read-only (§3)} \\
\mathrm{conf} &= s_2 - s_0 \\
s_1 &= s_0 + \mathrm{raw}\,s_1 + \tfrac{1}{2}\,\mathrm{conf} \\
\mathrm{field} &= -\tfrac{1}{2}\,\texttt{field\_scale}\,(\texttt{rule.charge\_transport\_vector}\cdot\hat{e}) \\
b_{01} &= (s_1 - s_0) + \mathrm{field} \\
b_{10} &= (s_1 - s_2) - \mathrm{field}
\end{aligned}
$$

- **Frozen content and the S1 home (round-1 F-7).** The S0/S2 sums, the 0.5 pair convention, and the
  cluster-at-centre rule are frozen plan content (AGENTS.md §10, data contract §3.3,
  [`docs/physics/energies.md`](../../physics/energies.md)). The **S1 midpoint rule has its first normative home
  in this page**; it restates the original's default
  (`SetDefaultKmcTransitionStateEnergy`: `S1 = S0 + RawS1 + 0.5·conf`), and `docs/physics/energies.md`
  documents it at implementation time. AGENTS.md §10 does not state S1, ADR 0012 is the transport/MSD decision,
  and `energies.md` does not state it today. `s_1` is the full S1; `barrier_forward = b_{01}` and
  `barrier_backward = b_{10}` (with field). The **no-field classification variants**
  `barrier_forward_no_field = s_1 - s_0` and `barrier_backward_no_field = s_1 - s_2` are what Step 16 compares
  against zero for `StartUnstable`/`EndUnstable`; they are derivable from the exposed `s0`/`s1`/`s2` and are
  exposed as named accessors so the two steps cannot disagree.
- `E[env][p]` is the **site+pair** owner vector built by Step 14 (`node_energy`, in kT); `C[c][p]` is the
  separate per-centre cluster energy (Step 14 §7.3, in kT). The `Σ_c` runs over **every instance centred at a
  path node** (`ctx.centres(position(p_i))` yields one or more instances per position, one per cluster table
  centred there); an instance whose centre is not a path node is not read during the transition evaluation
  but is updated when the state is committed. There is no double count: `E` does not contain the cluster term.
- **Why the current-state reads are the S0-state reads:** the caller looked the rule up by the path's current
  particles (`StateCode0`), so the current occupation *is* `S0`; the current vectors and codes are the S0-state
  values. `raw s_1` reads the same current vectors at the `S1_i` particles — the surroundings are unchanged in
  the hypothetical transition state — and reads the **site+pair vector only**: the cluster terms enter S1
  through `s_0` and `conf` (deviation D-3 below; the original's node-energy read folds the centre's cluster
  term into `RawS1`).
- **Units discipline (round-1 F-8).** Every internal sum is in kT. The context's vectors, cluster energy rows,
  `field_scale`, and the initial total are already kT (Step 14 D3-1); the model's pair/cluster tables stay in
  eV, so every direct model-table read made by this step — a pair delta or a raw-tuple cluster energy — is
  multiplied by `ev_to_kt()` exactly once, at the read. eV appears only at output boundaries
  (`lattice_energy_ev`). The merged Step 14 §7.2 shorthand "pair tables are converted to kT once at
  initialization" means the site table and the context vectors; the pair/cluster model tables remain in eV and
  are scaled per read, as the merged code does (`context/energy.rs`). That page's wording is corrected on a
  documentation maintenance branch; the per-read rule stated here is authoritative for this step.
- **State codes** are packed particle IDs in path order; decode with bounds checks (the reader already
  validated them against the particle count).
- **Cluster S2 terms.** For each instance centred at a path node, build the raw tuple from the final state
  with `ctx.occupation_code(instance, centre_cell, overrides)` (the overrides are the path nodes' `S2`
  particles at their slot in that instance) and resolve it once with `ctx.cluster_lookup(instance, raw)`,
  which wraps the approved `ClusterTable::lookup`. Read the energy as
  `table.energies[canonical_index] * ev_to_kt`. Never sort/minimise the slot particles or re-run symmetry
  (Step 14 F-29); never mutate code bytes per jump link (Step 14 D-10 — an intermediate occupation tuple may
  not exist in the table).
- **Void-member rule (Step 14 D5-2).** A cluster cell whose centre or member slot is void has no canonical
  occupation row: the contribution is zero (the empty cluster). The engine must **not** call `cluster_lookup`
  for such a tuple; it uses the absent `cluster_code_id`/zero energy row for the current state and contributes
  zero for the S2 overrides tuple. The non-canonical **non-void** fail-closed behaviour is unchanged.
- **The field input** is the per-rule Cartesian **charge transport vector** compiled in Step 08:
  `rule.charge_transport_vector = Σ_node (q(S2,node) − q(S0,node)) · x(path node)` in e·Å — the node-wise
  endpoint-moment difference, not a particle-identity pairing; a node with unchanged charge contributes
  nothing and void carries no charge; it also covers a particle that changes charge at a fixed site
  (for example A⁻ → A).
  - `ê` is the context's normalized field direction; `field_scale = |E|·1e-10/kT_eV` is precomputed by Step 14;
    use them rather than re-deriving the constant, so the unit chain lives in one place.
  - `field = -0.5 * field_scale * (rule.charge_transport_vector · ê)`, a frozen convention. For a single charge
    `q` moving by `Δx` it reduces to `-0.5 q E·Δx/kT`; a vacancy hop carries the correct sign because the ion
    moves once and the void has no charge. An identical-ion exchange has zero net charge transport; a
    distinct-charge exchange does not. A positive charge moving along the field lowers `b01` — the §9 test.
  - **Trust boundary (Step 13/14 assignment; round-1 F-6).** `rule.charge_transport_vector` comes from the
    untrusted `.msl` and is not covered by the parameterization signature. Step 13 validated it finite and zero
    when no node changes charge; the full positional formula cannot be recomputed because the v1 payload carries
    no fractional P1 coordinates. This step consumes it verbatim, rejects a non-finite field term
    (`NonFiniteFactor`, exit 13), and must not recompute a different vector from the movements. The residual
    risk and the future `.msl` revision (fractional P1 positions or a signed transport vector) are recorded in
    [`05-open-decisions.md`](../05-open-decisions.md) §4 and Step 13 §3.5/§12. (Step 14 §23 is a document-link
    reconciliation list and does not carry this item.)
  - `direction.path_vector` is display/tracking only and is not used to derive the field. Zero field yields a
    zero field term.
- For MMC, S1 and the field are unused (§7).

### 3. The S2 evaluation (read-only, dynamic)

The v2 design replaces the original's backup/apply/restore with **direct sums over read-only data**. The
numbers are identical because the S2-state energy of a path node differs from the current vector entry only
through the partners that changed, and every one of those partners is a path node. **The receiver need not
change:** an unchanged path node still needs its own-row correction when a linked partner changes (round-1
F-1). The per-node correction is therefore accumulated for **every** path node, not only the changed ones:

1. Resolve the path nodes `p_0..p_{L-1}` from `(selected, direction)` with the context's
   `resolve_path`/`resolve_node` — the single implementation of the wrapping rule (Step 14 §9.1; Step 16 §3
   uses the same helper, so the two cannot disagree).
2. For **every** path node `i` — changed (`S0_i ≠ S2_i`) or spectator (`S0_i = S2_i`) — and for **every** link
   `ℓ` in the node's **own** forward link range (`link_range(position(p_i))`) whose resolved partner
   `partner_of(p_i, ℓ)` is a path node `j` with `S0_j ≠ S2_j`, accumulate the pair delta at the row of the state
   being evaluated:

   $$
   \Delta_i
     = \sum_{\ell} \Bigl(
       \texttt{pair\_table}[ℓ.\texttt{pair\_id}][X_i][S2_j]
       - \texttt{pair\_table}[ℓ.\texttt{pair\_id}][X_i][S0_j]
     \Bigr)\cdot \texttt{ev\_to\_kt}
   $$

   where `X_i` is the state's particle at the receiver (`S2_i` for the s2 sum). The link belongs to the
   receiver, so the table is the owner's table and the row is the owner's row — the §4 orientation applied
   from the reading side. The `S0_i ≠ S2_i` receiver filter is **not** applied; only the partner filter
   `S0_j ≠ S2_j` remains, and only as a zero-delta skip. The receiver walk is also **unfiltered by structural
   stability**: the energy vectors sum every forward link of every environment, so the exact S2 sum must too.
   The jump-status link set (Step 14 §9.2) is not the evaluation's source: it keeps one sorted receiver link per
   ordered path-node pair and filters receivers by structural stability. A stable spectator receiver remains in
   that set; the original additionally excluded initially-void receivers, which Step 14 fixed by filtering on
   structural stability rather than initial occupation. The jump-status set still drops duplicate periodic images
   and structurally unstable receivers, so it cannot replace the exact owner-link walk. **All** links in the
   receiver's own range contribute, including several periodic images of the same partner with distinct pair
   tables (deviation D-2; the first-sorted-link rule would undercount them).
3. `s_2 = Σ_i (E[p_i][S2_i] + Δ_i) + Σ_c C_S2[c]` where `C_S2[c]` is the override-resolved cluster term of §2
   and the sum over `i` again runs over **every** path node. Cluster codes are built once per affected
   instance from the final state (`occupation_code` with the `S2` overrides); no intermediate code exists, so
   the two-member-change case cannot hit a missing tuple (Step 14 D-10).

- **No mutation, no restore, no scratch.** Nothing is backed up because nothing is written: the evaluation
  reads the vectors, the model tables, and `occupation_code`'s override builder. This removes the
  restore-exactness failure mode (a partial restore silently corrupting the context) and makes the
  zero-allocation proof structural. It also avoids a context-resident scratch buffer for the affected-owner
  set, which the mutate-based scheme would have needed — a new `MemoryPlan` entry with caps and invariant
  checks, i.e. a Step 14 memory-plan change.
- **Order independence.** The per-node delta sums are plain additions over independent links; the cluster
  terms are functions of the final state alone. Floating-point summation uses the fixed link order (Step 14's
  deterministic sort), so results are reproducible on one platform/compiler.
- **Static correction (fail-closed v1 decision).** `rule.static_correction` is `Option<[f64; 3]>` in the
  runtime model (`null` in the payload); the v1 writer emits `null` for **every** rule
  (`TransitionCompilationStage`, step-08-amend-1: excluded from the `model-v1` projection; "v1 stores `null`
  and Step 15 uses the dynamic S2 path"), and the reader rejects non-finite components when a value *is*
  present — so `None` is the only value a v1 package can carry and NaN is not a legal sentinel in this format.
  The non-null energy semantics are **undefined in `.msl` v1** (the original's correction is a scalar; the
  payload field is a 3-vector whose projection is unwritten). The engine therefore:
  - `None` → the dynamic path above (the only mode reachable from a v1 package);
  - `Some(_)` → the documented fail-closed error (exit 12, "static S2 correction not defined for this package
    version; rebuild the package"), never a guess, never a silent fall-back to dynamic that would drop a
    physics term the author intended.
  The optimization itself (compiler-computed corrections, a defined payload projection, dynamic-vs-static
  equivalence tests) is deferred with a named follow-up in
  [`05-open-decisions.md`](../05-open-decisions.md) §4; landing it needs a new `model-v1` projection version
  per step-08-amend-1.

### 4. Environment deltas on commit

When a position `V` changes from particle `old` to `new` **and the change is committed**, the environments
whose vectors change are the **owners** of the links that point at `V`. Never iterate `links(V)` and apply
those tables to the other side — that transposes the pair table. The rule is Step 14 §6.1/§7.2's and the
original's `DistributeEnvironmentUpdate` orientation:

- For every `IncomingLink { owner_position, link_id }` in `ctx.incoming(V)`, let
  `L = links(owner_position)[link_id]`, `owner = wrap(cell(V) − L.offset) * P + owner_position`, and
  `p_owner` be the owner's current particle. Apply

  $$
  E(\text{owner}, p) \mathrel{+}=
    \texttt{pair\_table}[L.\texttt{pair\_id}][p][new]
    - \texttt{pair\_table}[L.\texttt{pair\_id}][p][old]
    \qquad \text{for every } p \in [0, pc)
  $$

  — the whole `pc` vector is materialised (Step 14), so every row is updated, which is strictly more complete
  than the original's `update_particle_ids` list (retained read-only for a later optimisation). The table and
  the row both belong to the owner; a reverse link is not required and must not be synthesised.
- For each `ClusterAssoc { instance, slot }` on `L` — the **member-side** route — the owner is the instance's
  centre: after all particle changes, rebuild the instance's raw tuple **once** from the committed state (§6's
  fixed order; Step 14 D-10), resolve it with `ctx.cluster_lookup`, and update
  `cluster_energy[instance, cell(owner), p]` from the table (× `ev_to_kt`) for every allowed centre particle. A
  void centre or member contributes the zero row with no lookup (§2).
- **Centre-side route (round-1 F-3).** A changed environment `V` can also be an instance's **centre** while none
  of that instance's members changes. Such an instance is in no changed member's `incoming` set, but its slot-0
  code, energy rows, and `U` term change. For each instance in `ctx.centres(position(V))`, the affected entry is
  `(instance, cell(V))` — the instance copy whose centre is `V`. Add it to the affected set. The **affected set**
  is the deduplicated union, over every changed `V`, of the member-side `(instance, cell(owner))` pairs and the
  centre-side `(instance, cell(V))` pairs. Rebuild and account once per affected `(instance, cell)`; an instance
  may be reached by both routes (deduplicate). The pre-commit row `C` for every affected `(instance, cell)` is
  captured before the rebuild overwrites it (for example during the step-1 resolve pass); the cluster difference
  is accumulated later from that captured value.
- Cluster code lookup is Step 14's `cluster_lookup`/`ClusterTable::lookup` (the `step-11-amend-2` expansion
  map). This step does not search `occupation_codes`, does not sort the slot tuple, and does not apply
  symmetry: `MissingFromMap`/`ExpansionMapAbsent` keep their Step 14 meanings and exits.
- Cluster table layout (Step 14 contract): one energy per occupation code, slot 0 = centre particle, no centre
  row; `particle_table_mapping` is the ascending allowed centre-particle list.
- Skip zero deltas early (`old == new` or a table difference of 0). Multiple changed environments may share
  an owner; pair updates are per-link and order-independent, and the cluster rebuild is a function of the
  committed state. The affected-set deduplication is what makes the centre-only and shared-centre cases exact.

### 5. Lattice energy

- The initial total is Step 14's; the engine continues with the same exact decomposition:
  `U = Σ_stable site[P][particle] + 0.5 * Σ_stable pair[env][current] + Σ_instances Σ_cells
  cluster[instance][cell][centre particle]`, stored as `lattice_energy_ev = U_kT * kt_ev`. The cluster term is
  instance × cell; do not drop the cell dimension.
- The 0.5 factor corrects pair double counting **only**; the site background is counted once and a cluster
  once at its centre (the recorded correction OD-2 / Step 14 D-3;
  [`docs/physics/energies.md`](../../physics/energies.md)).
- **Incremental update.** `apply_state_change`/`apply_exchange` accumulate the exact `ΔU` on the fly during the
  same commit pass — no second pass, no snapshot arrays. Write `site_V = site[P(V)][old_V]`,
  `p_V = pairpart(E(V, old_V))` (the pair part of the vector entry, i.e. the entry minus its site background),
  `σ_V` for the old `STABLE` flag, and the primed forms for the committed state (the after-values are read
  after the §6 owner-delta pass, so they already contain every intra-path change):

  $$
  \Delta U =
    \underbrace{\sum_{V \in \text{changed}}\bigl(\sigma'_V\,\text{site}'_V - \sigma_V\,\text{site}_V\bigr)}_{\text{site terms, weight } 1}
    \;+\; \tfrac{1}{2}\underbrace{\left[\sum_{V \in \text{changed}}\bigl(\sigma'_V\,p'_V - \sigma_V\,p_V\bigr)
      + \sum_{V \in \text{changed}}\sum_{O \in \mathrm{incoming}(V)\setminus \text{changed}}
        \sigma_O\,\delta_{(O,V)}\right]}_{\text{pair rows, weight } 0.5}
    \;+\; \underbrace{\sum_{\text{affected } (i,c)} \bigl(C'_{{i,c}} - C_{{i,c}}\bigr)}_{\text{cluster rows, weight } 1}
  $$

  `δ_{(O,V)}` is the **per-link** pair delta at an unchanged owner `O`'s **current** particle row for the
  incoming link to the changed environment `V`:
  `pair_table[L.pair_id][p_O][new_V] − pair_table[L.pair_id][p_O][old_V]` (× `ev_to_kt`). `σ_O` is the owner's
  `STABLE` flag, which cannot change because `O` is unchanged. **The stability weight is required (round-1
  F-2):** `U` sums pair rows over **stable** environments only, so an unstable unchanged owner's delta must
  contribute zero, not `0.5·δ`. A changed environment's own row is counted in the first bracket (its after-value
  contains the deltas from the other changed environments, which is why the second bracket excludes changed
  environments — the 0.5 applies exactly once per pair row). `C`/`C'` are the affected `(instance, cell)` rows
  at the centre's current particle; a centre particle change moves the row, and the affected set is the
  deduplicated union of the member-side and centre-side routes (§4). `C` is captured before the step-6 rebuild
  overwrites the row (for example in the step-1 resolve pass), and the cluster term is `C' − C` from that
  captured value. `lattice_energy_ev += ΔU * kt_ev`.
- The debug consistency assertion (`check_lattice_energy` against `recompute_lattice_energy_kt`) runs after
  every accepted step in debug builds and in the property tests; release runs recompute periodically (before
  save/abort decisions, Step 16/17's call sites) as a defensive check.
- MMC abort logic uses the change in lattice energy and the accepted `ΔE` samples (Step 17); the values come
  from the same bookkeeping.

### 6. State application

`apply_state_change(selected, direction, rule)` commits the rule's `S2` occupation at the resolved path. The
order is fixed, documented, and never hash-iteration-dependent:

1. **Resolve before mutating.** Rebuild every affected instance's raw tuple from the final state
   (`occupation_code` + `S2` overrides) and resolve the codes through `cluster_lookup`, so a lookup failure
   returns before the first mutation. Capture each affected `(instance, cell)` row `C` in this pass, before any
   rebuild can overwrite it. (The evaluation already resolved the same tuples, so this is a safety net, not a
   second cost centre.)
2. **Particles and flags.** `set_particle` for each path node with `S0_i ≠ S2_i`, then derive the node's
   `flags` entry once per change (crate-internal `LatticeState` helper; `STABLE`/`MOBILE` per Step 14 §3 —
   never a full-array scan).
3. **Mobile tracker ids.** Permute the context's `mobile_tracker` entries so each id follows the moved
   mobile particle (the permutation follows the state-code movement; the rule's `tracker_order` is the Step 18
   *global*-tracker order and is not used here). Step 18 owns tracker values and its live inverse mapping.
4. **Pool membership.** `set_selectable(env, departed)` and `set_selectable(env, arrived)` for each changed
   environment (the context reads the current particle; callers always set particles first, Step 14 D4-4).
5. **Owner deltas.** For each changed environment, distribute the §4 pair deltas to every owner in
   `ctx.incoming(V)` (all `pc` rows; the owner may or may not be a path node — **not** only the path nodes).
6. **Cluster rebuild.** Once per affected `(instance, cell)` — the deduplicated union of the member-side
   `ClusterAssoc` route and the centre-side `centres(position(V)) × cell(V)` route (§4) — store the new code,
   `code_id`, and every allowed centre particle's energy row from the committed state.
7. **Lattice energy.** Accumulate the exact `ΔU` (§5) into `lattice_energy_ev`, over the same affected set,
   using the `C` values captured in step 1 rather than rereading the overwritten cluster rows.
8. Counters, histograms, tracker values, and time are **not** touched here: Steps 16/17 increment, Step 18
   records.

`apply_exchange(a, b, new_a, new_b)` is the same sequence for the two changed environments with no path
resolution (§7).

### 7. MMC exchange

- `exchange_energies(a, b, new_a, new_b)` returns `{s0, s2}` with no S1 and no field:
  `s0 = E[a][old_a] + E[b][old_b] + Σ_{instances centred at a or b} C[current]` and
  `s2 = E[a][new_a] + Δ_a + E[b][new_b] + Δ_b + Σ_{instances centred at a or b} C[S2-override-resolved]`,
  where `Δ_a` contains the pair deltas at `a`'s own links whose partner is `b` (row `new_a`, old/new `b`), and
  `Δ_b` symmetrically — the linked case of Step 17 §3. When `a` and `b` are not linked in a direction, that
  side's delta is zero (the independent case; the owner-major rule still applies per side and no reverse link
  is synthesised). This per-side rule is **deviation D-7**: the original applies both deltas only when links
  exist both ways and otherwise reads the entries uncorrected, so a one-directionally linked exchange differs
  between the two implementations. The independent per-side delta is exact for the payload as written. Cluster
  overrides are the two sites' new particles at their slots.
- `apply_exchange` commits through §6's sequence. `a == b` is a no-op (`s0 == s2`); Step 17 filters it as
  rejected before calling.
- The pair/cluster delta logic is shared with KMC (same §4 kernel); only the entry points and the
  no-field/no-S1 policy differ.

### 8. Numerics

#### 8.1 Types and finiteness

- `f64` everywhere; `f32` never appears in an energy computation.
- Table non-finiteness is rejected upstream (Step 13 reader; Step 14 used-value checks). This step rejects
  **computed** non-finite results: a non-finite field term is `NonFiniteFactor` (exit 13); a non-finite
  returned energy or `ΔU` is an `Invariant` error (exit 13), never silently propagated into the acceptance
  comparison or the lattice total.
- `static_correction` is `None` (dynamic) or a **finite** present value (reader-enforced); there is no NaN
  sentinel in this format, and a present value fails closed (§3).
- No fast-math/FMA contraction that changes results across platforms. If the compiler emits FMA, use explicit
  operations or document the statistical equivalence.

#### 8.2 The acceptance-exponential kernel

- One public free function (named in the `energy` module) computes the acceptance exponential: libm `exp` by
  default; the documented IEEE-754 bit-trick approximation when the job's `use_fast_exp` flag (bit 2 of
  `SolverJobFlags`) is set.
- Tests bound the approximation's relative error (≤ 3.5 %, the documented `FastExp32RmsError` class) and
  verify it is off by default. Steps 16/17 own the draw, the `K_norm` multiplication, and the `>= U[0,1)`
  comparison convention; this step only supplies the kernel and its bound.

### 9. Energy validation

Small hand-computed systems:

- two particles with one pair interaction;
- an asymmetric pair table (`t[a][b] ≠ t[b][a]`) with a one-directional link — the F-2 criterion: prove the
  owner's table and row are used, because the sender-major answer differs;
- a **spectator receiver** (`S0_i = S2_i`) that owns a link to a changed node — its own-row delta is
  accumulated and `s2` equals the brute-force value (round-1 F-1);
- a receiver with two periodic-image links (distinct pair tables) to the same changed sender — both deltas
  accumulate (deviation D-2);
- a 3-body cluster; a transition that changes two members of one cluster in one step (the final-state code is
  built once; no intermediate code exists);
- a cluster whose **centre** changes while none of its members changes — the code, rows, and `ΔU` term are
  rebuilt through the centre-side route (round-1 F-3);
- a cluster centre or member that is void (the zero-row rule, no lookup);
- site-energy offsets shifting a barrier;
- a 3-site migration with known S0/S1/S2, and its reversal (forward/backward barriers swap);
- a cluster-centred path node with `S1_i ≠ S0_i` (the raw-S1 site+pair-only rule, deviation D-3);
- an **unstable unchanged owner** of a link to a changed environment — its `ΔU` pair delta is weighted by
  `σ_O = 0` and the incremental total equals the full recomputation (round-1 F-2);
- field sign: a positive charge moving along the field lowers the forward barrier;
- the MMC exchange in the linked, unlinked, and **one-directionally linked** cases (the last is deviation D-7),
  plus the two-state detailed-balance ratio under a minimal in-test Metropolis driver (test code only; Step 17
  owns the production sampler).

### 10. Performance and memory budgets

| Scenario | Budget |
| --- | ---: |
| Allocation in evaluation and commit (counting allocator) | 0 after construction |
| New context arrays / `MemoryPlan` entries | 0 (structural: the engine is read-only on evaluation and scalar-accumulator-based on commit) |
| `energy` Criterion benchmark (populated 2×2×2; populated-model 10×10×10 clone) | medians recorded in `tests/reference/bench/` with a same-runner-class `baselines.json` entry; > 10 % regression against the recorded class is a release failure |
| Per-cycle wall time | recorded, not gated, here: the meaningful throughput gate is Step 16/17's "≥ 80 % of the original per core" cycle measurement, which this benchmark feeds |

Evidence: `benches/energy.rs` (Criterion, release; named in
[`03-quality-strategy.md`](../03-quality-strategy.md) §13), the raw log, the allocation test extension, and
the `baselines.json` entry. Absolute timings are compared only within the recorded runner class.

### 11. Deviations from the original (each with a test)

| # | Deviation | Rationale | Test |
| --- | --- | --- | --- |
| D-1 | The S2 evaluation is a read-only direct sum, not the original's backup/apply/restore over mutated energy entries | Identical numbers (the S2 entries differ from the current entries only through changed partners, all of which are path nodes; the receiver may be unchanged, see F-1); removes the restore-exactness failure mode and the scratch-buffer requirement; keeps the zero-allocation proof structural | Hand-computed S0/S1/S2 on the 3-site system; the spectator-receiver system; the incremental-vs-full lattice property; the counting allocator |
| D-2 | Every link of every path node (changed or spectator receiver) to a changed sender contributes to the evaluation delta — including several periodic images of one partner with distinct pair tables — instead of the first-found link, and the walk is not filtered by structural stability | The original's jump-link buffer keeps one link per path pair and only structurally stable receivers, so its dynamic S2 undercounts duplicate-image pairs and spectators that the energy vectors themselves sum; the engine walks every receiver's own link range for exactness (the precomputed jump-status links are not the evaluation's source) | A synthetic receiver with two links (distinct pair tables) to one changed sender accumulates both deltas; the spectator-receiver system |
| D-3 | `raw S1` sums the site+pair node vectors only; the cluster terms enter S1 through `s0` and `0.5·conf` | Frozen §2 decomposition (`E` holds site+pair only; the cluster is a separate per-centre term); the original folds the centre's cluster term into the node-energy read for `RawS1` | A cluster-centred path node with `S1 ≠ S0` reproduces the hand-computed value under the frozen formula, not the original's |
| D-4 | `static_correction`: `None` (the only v1 value) selects dynamic S2; a present value fails closed with the documented error | The v1 payload writes `null` for every rule and its non-null semantics are undefined (a 3-vector field; the original's correction is a scalar with a NaN sentinel, which the new reader rejects); never guess a projection, never silently drop an authored term | A hand-mutated `Some` rule returns exit 12; every real sample rule is `None` and takes the dynamic path |
| D-5 | The lattice-energy convention continues Step 14's exact decomposition (site once, pair ×0.5, cluster once per instance and cell), maintained incrementally with an on-the-fly exact `ΔU` | OD-2 / Step 14 D-3; the original's uniform `0.5 × Σ` halves the site and cluster terms | The multi-cell hand-computed total; the debug recomputation after every accepted step |
| D-6 | The flag/tracker/pool refresh is an explicit fixed-order sequence in the engine, not an implicit side effect of a particle write | Step 14 froze `set_particle` as the raw mutator and assigned the derived-state refresh to this step (its D4-4 and its §2 flags rule); a fixed order is reproducible and testable | The applied-state test asserts flags, tracker ids, and pool membership after `apply_state_change` |
| D-7 | The MMC exchange applies each side's cross-term independently when the two sites are linked in only one direction; the original applies both deltas only when links exist both ways and otherwise reads the entries uncorrected | The owner-major rule already defines each direction's contribution; a one-directional link is a real payload shape and the independent per-side delta is exact, while the original's both-or-nothing rule would drop a real interaction (or transpose one side) | An asymmetric one-directional exchange reproduces the brute-force S2 and differs from the original's uncorrected reading |

Deviations D-1/D-2/D-3/D-7 are implementation-procedure corrections inside the frozen §2 conventions; they
change no stored format, no signature, and no AGENTS.md §10 invariant, and Step 29's parity work covers their
observable effect. D-5's decomposition and D-4's fail-closed rule are recorded above with their tests.

## Implementation tasks

- [ ] Add the `energy/` concept module (`EnergyEngine`, `TransitionEnergies`, `ExchangeEnergies`, the
      acceptance-exponential kernel) on top of the post-F-3 split context layout; keep every touched file below
      800 lines.
- [x] Implement the read-only transition evaluation (§2/§3): path resolution via `resolve_path`, S0/raw-S1
      vector reads, per-node owner-major pair deltas over **every** path node's own links (changed and spectator
      receivers; the partner-change filter is a zero-delta skip), override-based cluster S2 codes with the void
      zero-row rule, the field term, and the `Result` error mapping.
- [ ] Implement `apply_state_change` (§6): the fixed update order, the full owner-vector updates via
      `incoming`, the deduplicated member-side and centre-side cluster rebuild from the committed state,
      flags/tracker ids/pool membership, and the on-the-fly exact `ΔU` (§5, with `σ_O` on unchanged-owner
      deltas) with the debug consistency assertion.
- [ ] Implement the MMC exchange entry points (§7) sharing the delta kernel, with the linked/unlinked and
      one-directionally linked split (deviation D-7).
- [x] Reject a `Some` static correction fail-closed (§3); record the deferral in the docs pages.
- [ ] Add the hand-computed, real-package, property, allocation, and benchmark tests (§Tests); run the
      `energy` Criterion benchmark and record the baseline entry.
- [ ] Update the documentation pages, parity rows, status dashboard, and closeout evidence; run the pre-audit
      gate.

## Acceptance criteria

- [x] Hand-computed pair, site, and cluster systems match exactly through `transition_energies` (two
      particles/one pair, a site offset, a 3-body cluster).
- [x] A 3-site migration with known S0/S1/S2 (hand-built rule) produces the expected `TransitionEnergies`;
      reversing the migration swaps the forward/backward barriers.
- [x] **F-2 (Step 14 audit deferral, owned here):** on a package with an asymmetric pair table
      (`t[a][b] ≠ t[b][a]`) and a one-directional link, the transition energies equal a brute-force
      recomputation that uses the owner's table and row, and differ from the transposed/sender-major value.
- [x] Field terms have the correct sign and magnitude for a known charge displacement (a positive charge moving
      along the field lowers the forward barrier); a non-finite field term is rejected (exit 13).
- [x] Cluster codes: the `step-11-amend-2` fixture's eight raw assignments (including the two non-canonical
      tuples) evaluate through the context's `cluster_lookup` with no solver-side symmetry; a void centre or
      member contributes the zero row with no lookup; a run-time non-canonical non-void tuple fails closed
      with the Step 14 error.
- [x] A receiver with duplicate periodic-image links (distinct pair tables) to the same changed sender
      accumulates every link's delta (deviation D-2).
- [x] A **spectator receiver** (`S0_i = S2_i`) that owns a link to a changed node receives its own-row delta;
      `s2` equals a brute-force recomputation over every path node's links (round-1 F-1).
- [ ] The incremental `ΔU` weights an unchanged owner's per-link delta by `σ_O`; an unstable unchanged owner of
      a link to a changed environment contributes no pair row, and the incremental total equals the full
      recomputation (round-1 F-2).
- [ ] A cluster whose **centre** changes while no member changes is rebuilt (code, `code_id`, rows) and its
      `C' − C` term enters `ΔU`; an instance reached by both the member-side and centre-side routes is rebuilt
      once (round-1 F-3).
- [x] A rule with `static_correction = Some(..)` (hand-mutated; the v1 writer emits `None` for every rule) is
      rejected fail-closed with the documented exit-12 error; every real sample rule is `None` and dynamic.
- [ ] `apply_state_change` updates every owner of a changed environment — including owners outside the path —
      plus particles, flags, mobile tracker ids, pool membership, and the affected instances' codes and rows
      (including a centre-only instance).
- [ ] The incremental lattice energy equals the full recomputation after every accepted step (debug assertion
      and property test) on a multi-cell lattice with pair and cluster terms and an unstable link owner.
- [ ] MMC exchange energies equal brute-force recomputations in the linked, unlinked, and one-directionally
      linked cases (the last is deviation D-7), and the two-state detailed-balance ratio holds under a minimal
      in-test Metropolis driver.
- [ ] Fast-exp is off by default and its relative error is bounded (≤ 3.5 %) when on.
- [ ] A counting allocator observes zero allocations in the evaluation and commit hot paths; the engine adds
      no context arrays and no memory-plan entries.
- [ ] The `energy` Criterion benchmark records same-runner-class medians with a `baselines.json` entry.
- [ ] Documentation written.

## Documentation

Implementation-time documentation (this freeze records only the design):

- `docs/physics/energies.md` — S0/S1/S2 and the midpoint rule (the first normative home is this step page), the
  read-only evaluation, pair/cluster/site deltas, field terms, the 0.5 convention and its scope, the raw-S1
  site+pair-only rule, and the dominance of stable pair rows (`σ_O` on unchanged-owner deltas, F-2).
- `docs/glossary/notation.md` (Units) — eV/kT conversion, the field-factor scale and sign conventions. (The
  draft's `docs/physics/units-constants.md` does not exist; the units content lives in the glossary's
  notation page.)
- `docs/development/solver-architecture.md` — the energy-engine API, the ownership/borrow composition, delta
  propagation (every path node's own links, F-1), the member-side + centre-side affected set (F-3), and the
  state-application order.
- `docs/development/testing-and-parity.md` — the hand-computed systems (including the spectator receiver, the
  unstable link owner, and the centre-only change), the property/allocation/benchmark tiers, and the
  void-member rule's engine-side test.
- `docs/troubleshooting/errors.md` — the run-time cluster-code errors, the `Some`-static-correction error, and
  the exit-12/13 mapping for engine failures.
- Parity: the "Pair/cluster/site energy", "Electric field", and "Fast exponential" rows (§7 of the matrix) move
  as the features land; the deviations D-1–D-7 are recorded in the notes.
- `docs/planning/closeout/step-15.json` plus `python tools/check_closeout_evidence.py`; the audit request in
  `docs/planning/audits/step-15.md` is created at closeout, not now.

## Tests

Fixtures: the existing real-writer packages are consumed unchanged — `msl-v1` (site-only ceria; its rules have
no pair/cluster tables), `step-13-populated` (3 P1 positions, 6 pair tables, 1 cluster table, KMC + MMC),
`step-13-centered` (cross-bucket directions), `step-11-amend-2` (the two-member cluster-code expansion), and
`step-14-multi-centre-cluster` (four instances). No new committed package: hand-built rule/lattice variants
are constructed in-process exactly as the Step 14 tests do (synthetic mutations of real packages), and the
3-site/hand-computed systems are toy contexts on the real models.

| Test | Assertion |
| --- | --- |
| Energy property | The incremental lattice energy equals the full recomputation across random accepted/rejected transition sequences on a multi-cell lattice (debug assertion after every step), including an unstable link owner and a centre-only cluster change |
| Acceptance (real packages) | Real rules on `msl-v1` (site-only) and `step-13-populated`/`step-11-amend-2` (pair/cluster) reproduce hand-computed golden contributions |
| Pair delta | Two-particle swap energy |
| Pair orientation (F-2) | Asymmetric table + one-directional link: the owner-major result equals a brute-force recomputation and differs from the transposed/sender-major result — the Step 14 audit's deferred criterion, owned and closed here |
| Spectator receiver (round-1 F-1) | An unchanged path node (`S0_i = S2_i`) linked to a changed node contributes its own-row delta; `s2` equals the brute-force value over every path node's links |
| Duplicate images | Two links (distinct pair tables) from one receiver to the same changed sender both contribute (deviation D-2) |
| Cluster delta | 3-body occupation change |
| Cluster rebuild | Two members of one cluster change in one transition; the S2 code is built once from the final state; no intermediate code is ever resolved |
| Centre-only cluster change (round-1 F-3) | A transition that changes only a cluster's centre (no member changes) rebuilds the code, `code_id`, and rows and contributes `C' − C` to `ΔU`; an instance reached by both routes is rebuilt once |
| Canonical code path | The amendment-2 fixture evaluates transitions that move the cluster members through all eight raw assignments; the two non-canonical tuples contribute the canonical orbit's energy; no symmetry is re-derived |
| Void member | A void centre or member slot contributes the zero row with no `cluster_lookup`; a non-void non-canonical tuple still fails closed |
| Applied state | Every owner of a changed environment is updated, including owners outside the path; flags, tracker ids, and pool membership match the committed particles; a centre-only instance is rebuilt |
| Site energy | Barrier shift equals the configured site energy |
| S1 | Raw S1 (site+pair only, node 0 zero) + 0.5 conformation; the cluster-centred path-node case reproduces the frozen formula (deviation D-3) |
| Field | Sign/magnitude for a known charge displacement; non-finite rejected (exit 13) |
| Static correction | `None` (every v1 rule) is dynamic; a hand-mutated `Some` rule fails closed (exit 12) |
| Lattice energy | Incremental vs. full on a multi-cell lattice, so the instance × cell cluster sum is covered; an unstable unchanged owner's delta is weighted by `σ_O = 0` (round-1 F-2) |
| MMC exchange | Linked, unlinked, and one-directionally linked (D-7) two-site deltas equal brute force; the two-state detailed-balance ratio holds under a minimal in-test Metropolis driver |
| Fast exp | Error bound (≤ 3.5 %) and off-by-default |
| Non-finite | A computed non-finite energy/`ΔU` is an `Invariant` error; nothing propagates into comparisons |
| Allocation | Zero allocations in evaluation and commit after construction (own test binary) |
| Benchmark | Criterion `energy` medians on the populated fixtures; evidence and same-class `baselines.json` entry |

No mocks: real packages via `mocassin_msl::Package::open_readonly`/`load_job`, real domain values, and the
counting global allocator as the only test double (a true process/allocator boundary). The in-test Metropolis
driver is test code exercising the public engine, not a production sampler.

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Energy convention bugs | Hand calculations, incremental-vs-full checks, golden real-package values |
| Delta applied with the wrong table row (sender-major/transposed) | The owner-major rule stated once (§4) and the F-2 asymmetric-table + one-directional-link acceptance criterion with a brute-force oracle |
| Spectator receiver undercounted (a path node that does not change but owns a link to a changed node) | §3 walks **every** path node's own links (round-1 F-1) and the spectator-receiver test compares against a brute-force recomputation |
| Duplicate-image pairs undercounted (original's first-link quirk) | The evaluation walks every receiver's own links (deviation D-2) with a dedicated test; the jump-status links are not the evaluation's source |
| Cluster double counting or a missing intermediate code | The per-centre convention (§2) and the once-from-final-state rebuild (§3/§6) with the two-member and centre-only tests |
| Centre-only cluster change left stale | The §4/§6 deduplicated member-side + `centres(position(V))` affected set (round-1 F-3) and the centre-only test |
| Void-member tuples misclassified as missing codes | The D5-2 zero-row rule is explicit (§2) with its own test row |
| Field sign errors | The single-charge physical test; the frozen `-0.5·field_scale·(transport·ê)` formula and the precomputed context factors |
| `Some` static corrections guessed instead of rejected | Fail-closed exit 12 with a hand-mutated rule test; the deferral and follow-up owner recorded |
| Partial state after a failed apply | Resolve-before-mutate (§6 step 1); the apply methods return before the first mutation on a lookup failure |
| Incremental `ΔU` drift (stability crossings, unstable unchanged owners, shared owners, centre-only rebuilds) | The §5 decomposition with the 0.5 exactly once and `σ_O` on every unchanged-owner delta (round-1 F-2); the deduplicated affected set (F-3); the debug recomputation after every accepted step and the property test |
| MMC one-directional exchange diverges from the original | Deviation D-7 with an asymmetric one-directional test; the exact per-side rule is documented rather than silently inherited |
| Floating-point drift | Fixed operation order over the deterministic link sort; no FMA/fast-math; statistical tests across platforms are Step 29 |
| Read-only evaluation regressions into a mutating scheme | The allocation proof and the §3 freeze; a context-resident scratch buffer would be a Step 14 memory-plan change and a stop-and-report trigger |

## Done when

The energy engine computes transition and lattice energies correctly for pair, cluster, site, and field
contributions — read-only on evaluation, exact on commit, allocation-free, with the F-2 owner-major criterion
closed, the round-1 spectator-receiver/unstable-owner/centre-only cases (F-1–F-3) covered, the deviations
D-1–D-7 recorded with tests, the benchmark and allocation evidence recorded, and the documentation and parity
updates written.
