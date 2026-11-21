# Step 15 — Energy Engine

**Phase:** D. Solver  
**Depends on:** 14
**Delivers:** The transition energy calculator: S0/S1/S2, pair/cluster deltas, electric-field terms,
state application, and lattice-energy bookkeeping.

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

Compute the energy change of a proposed transition exactly and efficiently, with a clear unit convention and
tests against hand calculations. This is the physics core shared by KMC and MMC.

## Original reference

- All energies are kT internally; output converts to eV with `EnergyFactorKtToEv`.
- Initial environment energy:
  - site/defect background;
  - pair tables `[center_particle][partner_particle]`;
  - cluster tables `[mapped_row][occupation_code]`.
- KMC transition:
  - `S0 = Σ_i E[path_i][S0_i]`;
  - `RawS1 = Σ_{i≥1} E[path_i][S1_i]` (node 0 contributes zero);
  - `S2` read via a static correction or computed by backing up affected energies, applying all deltas, and
    restoring;
  - default `ΔE_conf = S2 − S0`, `S1 = S0 + RawS1 + 0.5 ΔE_conf`.
- Field:
  - `E_field = −(direction.ElectricFieldFactor * rule.ElectricFieldFactor)`;
  - `b01 = (S1 − S0) + E_field`;
  - `b10 = (S1 − S2) − E_field`.
- Acceptance: `P_raw = exp(−b01)`, `P_norm = P_raw * K_norm`, accept if `P_norm >= U[0,1)`.
- Outcomes:
  - `b10_noField ≤ 0` → end-unstable;
  - `b01_noField ≤ 0` → start-unstable;
  - otherwise accept/reject based on `P_norm`.
- Lattice energy = `0.5 * Σ_stable E[current]` in eV (pair double-count correction).
- Deltas: for each environment link, add `PairDelta[old][new][center]` plus cluster deltas for changed codes.
- Fast exp: optional IEEE-754 bit-trick approximation.
- Known issues: the 0.5 lattice-energy correction is valid only for pair interactions; cluster counting must be
  documented; the field sign and `DualDof` factor need tests.

## Design

### 1. Energy engine API

```rust
pub struct TransitionEnergies {
    pub s0: f64,
    pub s1: f64,
    pub s2: f64,
    pub barrier_forward: f64,   // S1 - S0 without field
    pub barrier_backward: f64,  // S1 - S2 without field
    pub field_term: f64,
}

pub struct EnergyEngine<'a> { ctx: &'a mut SimulationContext }

impl<'a> EnergyEngine<'a> {
    /// Borrows the context mutably for one evaluation; the view is zero-sized and allocation-free.
    pub fn new(ctx: &'a mut SimulationContext) -> Self;

    pub fn transition_energies(
        &mut self,
        direction: &JumpDirection,
        rule: &JumpRule,
        apply: bool,
    ) -> TransitionEnergies;

    pub fn apply_state_change(&mut self, direction: &JumpDirection, rule: &JumpRule);
    pub fn environment_energy(&self, env: EnvId, particle: ParticleId) -> f64;
    pub fn lattice_energy_ev(&self) -> f64;
}
```

- Energies are `f64` in kT inside the engine; conversion to eV only at output boundaries.
- `apply=false` computes S2 via a reversible delta simulation; `apply=true` commits the state.
- The engine never allocates in the hot path.
- **Mutability (review F-16).** The engine holds `&mut SimulationContext`: `transition_energies` mutates the
  particle array and the cluster codes while it evaluates S2 and restores them, and `apply_state_change` commits
  them. A shared `&SimulationContext` cannot express that. The context exposes no interior mutability; Step 14
  freezes the API with this signature (its `&mut` mutators for particles/flags/pools are the only writes).
  `environment_energy`/`lattice_energy_ev` take `&self` and are the read-only accessors.
- **Composition with Step 16 (review F-19).** `EnergyEngine<'a>` owns the mutable context borrow, so a
  `KmcEngine` must **not** hold both `ctx: &mut SimulationContext` and an `EnergyEngine<'a>`; that would be two
  simultaneous mutable borrows. `KmcEngine` holds the context, and each energy call constructs a short-lived
  view with `EnergyEngine::new(&mut *self.ctx)` (a reborrow, no allocation). Step 16 §1/§5 use this form.
  Alternatively a later refactor can move the engine into `SimulationContext` methods; the `&mut` context rule
  stays.
- `env` is an `EnvId` (absolute supercell environment). `PositionId` remains the P1 index; do not pass it here.

### 2. Transition energies

Given a direction with path positions `p_0..p_L-1` and a rule with state codes `S0,S1,S2`:

$$
\begin{aligned}
s_0 &= \sum_i E[p_i][S0_i] + \sum_{c} C[c][S0_c] \\
\mathrm{raw}\,s_1 &= \sum_{i=1}^{L-1} E[p_i][S1_i] \qquad (p_0\text{ contributes zero}) \\
s_2 &= \text{energy after applying the state change} \\
\mathrm{conf} &= s_2 - s_0 \\
s_1 &= s_0 + \mathrm{raw}\,s_1 + \tfrac{1}{2}\,\mathrm{conf} \\
\mathrm{field} &= -\tfrac{1}{2}\,(|E|/kT)\,(\text{transport vector}\cdot\hat{e}) \\
\mathrm{barrier\_forward} &= (s_1 - s_0) + \mathrm{field} \\
\mathrm{barrier\_backward} &= (s_1 - s_2) - \mathrm{field}
\end{aligned}
$$

- `E[env][p]` is the **site+pair** owner vector built by Step 14; `C[c][p]` is the separate per-centre cluster
  energy, indexed per **instance** (a table on a multi-position centre site has several instances). Cluster
  interactions are counted once at their centre, so the `Σ_c` runs over **every instance centred at a path node**
  (`ctx.centres(position)` yields one or more instances per position); an instance whose centre is not a path node
  is not read during the transition evaluation but is updated when the state is committed. There is no double
  count: `E` does not contain the cluster term. Step 14 precomputes the centre index, not a per-direction touch
  table.
- A cluster whose code depends on several path nodes is evaluated **once from the final state**: build the S2
  raw tuple with `ctx.occupation_code(instance, centre_cell, overrides)` and resolve it once with
  `ctx.cluster_lookup(instance, raw)`, which wraps the approved `ClusterTable::lookup`. Step 15 matches only the
  successful canonical id; the two failure variants are already mapped to the Step 14 errors and cannot occur
  after a successful initialization. Never sort/minimise the slot particles or re-run symmetry here (F-29).
  Do not mutate code bytes per jump link: an intermediate occupation tuple may not exist in the table (Step 14
  D-10).

- The state codes are packed particle IDs in path order; decode with bounds checks.
- For MMC, S1 is unused; `s0` and `s2` are the two-site site+pair energies plus the affected instances'
  cluster terms.
- The field input is the per-rule Cartesian **charge transport vector** compiled in Step 08:
  - `rule.charge_transport_vector = Σ_node (q(S2,node) − q(S0,node)) · x(path node)`, in e·Å. It is the
    node-wise endpoint-moment difference, not a particle-identity pairing; a node with unchanged charge
    contributes nothing, and void carries no charge. It also covers a particle that changes charge at a fixed
    site (for example A⁻ → A).
  - `ê` is the unit field direction; `|E|` is the field modulus and `kT` the thermal energy.
  - `field = -0.5 * (|E| / kT) * (rule.charge_transport_vector · ê)`, using the same Å/V/m scale as the
    existing field convention. Step 14 precomputes `field_scale = |E| * 1e-10 / kT_eV` in `e·Å` per eV
    (V/m → eV/Å) and the normalized direction; use them rather than re-deriving the constant, so the unit chain
    lives in one place.
  - This is exact for non-collinear multi-site paths. For a single charge `q` moving by `Δx`, it reduces to
    `-0.5 q E·Δx/kT`; a vacancy hop carries the correct sign because the ion moves once and the void has no
    charge. An identical-ion exchange has zero net charge transport; a distinct-charge exchange does not.
  - `direction.path_vector` is display/tracking only and is not used to derive the field.
- **Trust boundary (Step 13 assignment).** `rule.charge_transport_vector` comes from the untrusted `.msl` and is
  not covered by the parameterization signature. Step 13 validates that it is finite and zero when no node
  changes charge, but cannot recompute the full `Σ Δq x` formula because the v1 payload carries no fractional P1
  coordinates (only `vector4 = [0,0,0,P]` and the fractional movements). Step 15 owns the field term and must
  reject a non-finite result and record the residual risk; a future `.msl` can add fractional positions or sign
  the transport vector. Do not silently clamp or recompute a different vector.
- `rule.charge_transport_vector` is three `f64` components; zero field yields zero.

### 3. S2 computation

Two modes:

**Dynamic (default in v1)**

1. Back up the current particle ids of all path positions and the codes of the centres whose clusters will change.
2. Apply the state change to the path positions (without updating pools/trackers).
3. Apply the pair deltas to every **owner** returned by `ctx.incoming(changed_env)` for each changed environment,
   using the owner's link and the owner's row (§4); the owner may or may not be a path node.
4. Rebuild each affected instance's occupation code once from the final state, look up its code id, and read the
   instance's cluster energy (§2).
5. Read `s2 = Σ_i E[p_i][S2_i] + Σ_{instances centred at a path node} C[instance][S2 centre]`.
6. Restore the backup and energies (including the instances' codes and energies).

- The backup/restore must be exact and allocation-free; use small fixed arrays for path positions and a
  scratch buffer for affected environments.
- If an owner is affected by multiple path positions, its pair deltas are applied sequentially; each pair link is
  independent, so the result is order-independent. Clusters are not updated incrementally: each affected
  instance is rebuilt once after all particle changes (§3 step 4), so there is no order effect and no missing intermediate
  code.

**Static (optimization)**

- If the S2 contribution of every affected neighbor is independent of the sender's surrounding state, the
  compiler can precompute a static correction.
- The rule then stores `static_correction`; S2 is computed from S0 plus the correction.
- Dynamic rules store `NaN` and use the backup/apply/restore path.
- Static corrections are optional; they must be validated against dynamic S2 for every rule in tests before
  being enabled by default.

### 4. Environment deltas

When a position `V` changes from particle `old` to `new`, the environments whose vectors change are the **owners**
of the links that point at `V`. Never iterate `links(V)` — those are `V`'s own vectors, and applying their tables
to the other side transposes the pair table. The rule is the one in Step 14 §6.1/§7.2 and in the original's
`DistributeEnvironmentUpdate`/`InvokeJumpLinkDeltas`:

- For every `IncomingLink { owner_position, link_id }` in `ctx.incoming(V)`, let
  `L = links(owner_position)[link_id]`, `owner = wrap(cell(V) - L.offset) * P + owner_position`, and `p_owner` be
  the owner's particle in the state being evaluated (S0/S1/S2 for a path-node owner, the current particle
  otherwise). Apply

  $$
  E(\text{owner}, p_\text{owner}) \mathrel{+}=
    \texttt{pair\_table}[L.\texttt{pair\_id}][p_\text{owner}][new]
    - \texttt{pair\_table}[L.\texttt{pair\_id}][p_\text{owner}][old]
  $$

  for every `p_owner` the owner supports (the whole `pc` vector is materialised, so this is every particle).
  The table and the row both belong to the owner; a reverse link is not required and must not be synthesised.
- For each `ClusterAssoc { instance, slot }` on `L`, the owner is the instance's centre: rebuild the
  instance's raw tuple with the new particle in `slot` (once, from the final state; Step 14 D-10), resolve it
  with `ctx.cluster_lookup(instance, raw)`, and update `cluster_energy[instance, cell(owner), p_owner]` from the
  table for every allowed centre particle. This is the original's `InvokeEnvironmentLinkClusterUpdates`/`InvokeLocalEnvironmentLinkClusterDeltas`
  orientation.
- Cluster code lookup is **Step 14's** `cluster_lookup`/`ClusterTable::lookup` (the `step-11-amend-2`
  expansion map). Step 15 does not search `occupation_codes`, does not sort the slot tuple, and does not apply
  symmetry: a `MissingFromMap`/`ExpansionMapAbsent` is a Step 14 error and must not be re-classified here.
- Cluster table layout (Step 14 contract): one energy per occupation code, slot 0 = centre particle, no centre
  row; `particle_table_mapping` is the ascending allowed centre-particle list. A wrong member offset or a missing
  code is rejected at context initialization, not silently skipped.
- Skip zero deltas early (`old == new` or table difference == 0).
- Multiple changed environments may share an owner; the updates are independent per link and therefore
  order-independent for pairs. For clusters, evaluate each affected instance once after all particle changes (§3)
  so no order effect remains.
- Optional 3D pair-delta tables: `delta[old][new][p_owner]`; use only if profiling shows a win and memory is
  acceptable.


### 5. Lattice energy

- The initial total is computed by Step 14; the engine continues with the same exact decomposition:
  `U = Σ_stable site[P][particle] + 0.5 * Σ_stable pair[env][current]
   + Σ_instances Σ_cells cluster[instance][cell][centre particle]`, converted to eV with `kT_to_ev`. The cluster
  term is instance × cell (every instance exists in every supercell cell); do not drop the cell dimension.
- The 0.5 factor corrects pair double counting only. The site background is counted once and a cluster once at
  its centre; do not apply the 0.5 factor to them (the original did, and it is a recorded correction; see
  [`docs/physics/energies.md`](../../physics/energies.md) and the Step 14 design D-3).
- After every accepted transition, update the lattice energy incrementally by the exact accepted `ΔE` rather
  than recomputing the full sum. Periodically (or before saving) recompute and assert consistency in debug
  builds using the context's full-recomputation helper.
- MMC abort uses the change in lattice energy and the accepted `ΔE` samples.

### 6. State application

- `apply_state_change`:
  - update path position particles;
  - update stable/mobile flags;
  - update tracker ids via the rule's tracker order code;
  - update selection pool membership (particle changes can change direction counts) with `set_selectable` for
    the departed and the arrived particle of each changed environment;
  - update every **owner** of a changed environment through `ctx.incoming(changed_env)` (pairs and link-carried
    cluster associations), exactly as in §4 — not only the path nodes;
  - rebuild the affected instances' codes and energies once from the committed state (§3);
  - update lattice energy by the accepted delta;
  - increment counters/trackers (step 18).
- The order of these updates is fixed and documented; it must not depend on hash iteration. Particle changes are
  applied first, then the owner deltas are distributed, then cluster codes/energies are rebuilt, then pools and
  trackers; the per-link pair updates are order-independent and the cluster rebuild is a function of the final
  state.

### 7. Numerics

- Use `f64`; avoid `f32` for energies.
- Reject NaN/Inf in tables at load time; `static_correction` may be NaN as a sentinel.
- Use libm `exp` by default. `UseFastExp` selects the documented bit-trick approximation; tests bound the
  relative error (≤ 3.5 %) and verify that it is off by default.
- Acceptance comparison uses `>= U[0,1)` to match the original convention; document the boundary behavior.
- Do not enable fast-math/FMA contraction that changes results across platforms. If the compiler emits FMA,
  use explicit operations or document the statistical equivalence.

### 8. Energy validation

- Small hand-computed systems:
  - two particles with one pair interaction;
  - an asymmetric pair table (`t[a][b] ≠ t[b][a]`) with a one-directional link, to prove the owner's table and
    row are used (the sender-major answer differs);
  - a 3-body cluster;
  - a transition that changes two members of one cluster in one step (intermediate code absent from the table);
  - site energy offsets;
  - a 3-site migration with known S0/S1/S2.
- Cross-check dynamic vs. static S2 for every compiled rule.
- Check that reversing a 3-site migration swaps forward/backward barriers.
- Check field sign: a positive charge moving along the field should lower the forward barrier.
- Check detailed balance for a 2-state MMC system.
- Check that the applied state updates every owner of a changed environment, not only the path nodes.

## Implementation tasks

- [ ] Implement `EnergyEngine` and `TransitionEnergies`.
- [ ] Implement S0/S1/S2 and field terms.
- [ ] Implement dynamic S2 with exact backup/restore.
- [ ] Implement pair/cluster delta propagation.
- [ ] Implement incremental lattice energy and periodic consistency checks.
- [ ] Implement state application and pool/tracker hooks.
- [ ] Implement fast-exp behind a flag.
- [ ] Add static S2 correction support (compiler + engine) behind a flag.
- [ ] Add hand-computed and property tests.

## Acceptance criteria

- [ ] Hand-computed pair, cluster, and site-energy systems match exactly.
- [ ] A 3-site migration produces the expected S0/S1/S2.
- [ ] Dynamic and static S2 agree for every rule in the sample models.
- [ ] Field terms have the correct sign and magnitude for a known charge displacement.
- [ ] Incremental lattice energy equals a full recomputation after every accepted step (debug).
- [ ] Fast-exp is off by default and bounded when on.
- [ ] No allocations in the energy hot path.
- [ ] Documentation written

## Documentation

At this point, document the energy engine:

- `docs/physics/energies.md` — S0/S1/S2, pair/cluster/site deltas, field terms, 0.5 convention.
- `docs/physics/units-constants.md` — eV/kT, field factors, sign conventions.
- `docs/development/solver-architecture.md` — energy-engine API and delta propagation.
- `docs/development/testing-and-parity.md` — hand-computed and dynamic/static equivalence tests.

## Tests

| Test | Assertion |
| --- | --- |
| Energy property | Incremental deltas equal full recomputation for random accepted/rejected transitions |
| Acceptance | Real ceria rules reproduce golden pair/cluster/site contributions |
| Pair delta | Two-particle swap energy |
| Pair orientation | Asymmetric table + one-directional link: the owner-major result equals a brute-force recomputation and differs from the transposed/sender-major result |
| Cluster delta | 3-body occupation change |
| Cluster rebuild | Two members of one cluster change in one transition; S2 uses the final code once, no missing intermediate code |
| Canonical code path | The amendment-2 two-member ceria fixture evaluates every raw assignment through Step 14's `cluster_lookup`; the two non-canonical tuples contribute the canonical orbit's energy; no symmetry is re-derived |
| Applied state | Every owner of a changed environment is updated, including owners outside the path |
| Site energy | Barrier shift |
| S1 | Raw S1 + 0.5 conformation |
| Field | Sign/magnitude for known displacement |
| Dynamic vs static | Equality for all sample rules |
| Lattice energy | Incremental vs full on a multi-cell lattice, so the instance × cell cluster sum is covered |
| Detailed balance | MMC two-state distribution |
| Fast exp | Error bound and off-by-default |
| NaN/Inf | Rejected at load; no silent propagation |

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Energy convention bugs | Hand calculations, full-vs-incremental checks, golden models |
| Delta applied with the wrong table row (sender-major/transposed) | Owner-major rule in §4, asymmetric-table and one-directional-link tests, Step 14 inverse-index test |
| Cluster double counting | Explicit per-center convention and tests |
| Missing intermediate cluster code | Rebuild from the final state (Step 14 D-10) and the two-member test |
| Field sign errors | Physical test with a single charge |
| Static correction bugs | Dynamic fallback, cross-check every rule |
| Floating-point drift | Fixed operation order; statistical tests across platforms |

## Done when

The energy engine computes transition and lattice energies correctly for pair, cluster, site, and field
contributions, with dynamic/static consistency and no hidden allocations.
