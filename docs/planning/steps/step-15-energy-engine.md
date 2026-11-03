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

pub struct EnergyEngine<'a> { ctx: &'a SimulationContext }

impl EnergyEngine<'_> {
    pub fn transition_energies(
        &self,
        direction: &JumpDirection,
        rule: &JumpRule,
        apply: bool,
    ) -> TransitionEnergies;

    pub fn apply_state_change(&mut self, direction: &JumpDirection, rule: &JumpRule);
    pub fn environment_energy(&self, env: PositionId, particle: ParticleId) -> f64;
    pub fn lattice_energy_ev(&self) -> f64;
}
```

- Energies are `f64` in kT inside the engine; conversion to eV only at output boundaries.
- `apply=false` computes S2 via a reversible delta simulation; `apply=true` commits the state.
- The engine never allocates in the hot path.

### 2. Transition energies

Given a direction with path positions `p_0..p_L-1` and a rule with state codes `S0,S1,S2`:

```text
s0 = Σ_i E[p_i][S0_i]
raw_s1 = Σ_{i=1..L-1} E[p_i][S1_i]     // p_0 contributes zero
s2 = energy after applying the state change
conf = s2 - s0
s1 = s0 + raw_s1 + 0.5 * conf
field = -(direction.field_projection * rule.field_factor)
barrier_forward = (s1 - s0) + field
barrier_backward = (s1 - s2) - field
```

- The state codes are packed particle IDs in path order; decode with bounds checks.
- For MMC, S1 is unused; `s0` and `s2` are the two-site energies.
- Field factors are precomputed in kT units:
  - `direction.field_projection = 0.5 * (|E| / kT) * (Δx_path · ê)`, where `Δx_path` is the
    Cartesian displacement of the transition path and `ê` is the unit field direction;
  - `rule.field_factor` encodes the net charge projection/sign for that rule; the compiler computes it from
    the path occupation change (Step 08);
  - `field = -(direction.field_projection * rule.field_factor)`.
  For a single charge `q` moving by `Δx`, this reduces to the familiar `-0.5 q E·Δx / kT`.
  - The product must have the same sign convention as the original; tests pin a known case.
- `direction` and `rule` field factors are `f64`; zero field yields zero.

### 3. S2 computation

Two modes:

**Dynamic (default in v1)**

1. Back up the current particle ids of all path positions and affected neighbors.
2. Apply the state change to the path positions (without updating pools/trackers).
3. Apply energy deltas to every linked neighbor (pair and cluster).
4. Read `s2 = Σ_i E[p_i][S2_i]`.
5. Restore the backup and energies.

- The backup/restore must be exact and allocation-free; use small fixed arrays for path positions and a
  scratch buffer for affected environments.
- If a neighbor is affected by multiple path positions, deltas are applied sequentially; the result must be
  order-independent for pair interactions and consistent for clusters.

**Static (optimization)**

- If the S2 contribution of every affected neighbor is independent of the sender's surrounding state, the
  compiler can precompute a static correction.
- The rule then stores `static_correction`; S2 is computed from S0 plus the correction.
- Dynamic rules store `NaN` and use the backup/apply/restore path.
- Static corrections are optional; they must be validated against dynamic S2 for every rule in tests before
  being enabled by default.

### 4. Environment deltas

When a position changes from particle `old` to `new`:

- For each link `(target, pair_id)`:
  `energy[target][center_particle] += pair_table[pair_id][center][new] - pair_table[pair_id][center][old]`
  for every center particle that the target supports.
- For each cluster link `(cluster_id, code_byte)`:
  - update the cluster code bytes at the recorded byte position;
  - look up the new code id;
  - `energy[target][center_particle] += cluster_table[cluster_id][center_row][new_code] -
   cluster_table[cluster_id][center_row][old_code]`.
- Cluster code lookup:
  - codes are sorted; use binary search for > 8 codes, linear for small tables;
  - missing codes are a data error (should not happen for valid occupations).
- Skip zero deltas early (`old == new` or table difference == 0).
- Optional 3D pair-delta tables: `delta[old][new][center]`; use only if profiling shows a win and memory is
  acceptable.

### 5. Lattice energy

- `lattice_energy_ev = 0.5 * Σ_{stable env} energy[env][current_particle] * kT_to_eV`.
- The 0.5 factor corrects pair double counting.
- Cluster contributions are counted once at their center; the engine documents this and tests it.
- After every accepted transition, update the lattice energy incrementally by the accepted `ΔE` rather than
  recomputing the full sum. Periodically (or before saving) recompute and assert consistency in debug builds.
- MMC abort uses the change in lattice energy and the accepted `ΔE` samples.

### 6. State application

- `apply_state_change`:
  - update path position particles;
  - update stable/mobile flags;
  - update tracker ids via the rule's tracker order code;
  - update selection pool membership (particle changes can change direction counts);
  - update environment energies/links for all affected neighbors;
  - update cluster codes;
  - update lattice energy by the accepted delta;
  - increment counters/trackers (step 18).
- The order of these updates is fixed and documented; it must not depend on hash iteration.

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
  - a 3-body cluster;
  - site energy offsets;
  - a 3-site migration with known S0/S1/S2.
- Cross-check dynamic vs. static S2 for every compiled rule.
- Check that reversing a 3-site migration swaps forward/backward barriers.
- Check field sign: a positive charge moving along the field should lower the forward barrier.
- Check detailed balance for a 2-state MMC system.

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
| Cluster delta | 3-body occupation change |
| Site energy | Barrier shift |
| S1 | Raw S1 + 0.5 conformation |
| Field | Sign/magnitude for known displacement |
| Dynamic vs static | Equality for all sample rules |
| Lattice energy | Incremental vs full |
| Detailed balance | MMC two-state distribution |
| Fast exp | Error bound and off-by-default |
| NaN/Inf | Rejected at load; no silent propagation |

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Energy convention bugs | Hand calculations, full-vs-incremental checks, golden models |
| Cluster double counting | Explicit per-center convention and tests |
| Field sign errors | Physical test with a single charge |
| Static correction bugs | Dynamic fallback, cross-check every rule |
| Floating-point drift | Fixed operation order; statistical tests across platforms |

## Done when

The energy engine computes transition and lattice energies correctly for pair, cluster, site, and field
contributions, with dynamic/static consistency and no hidden allocations.
