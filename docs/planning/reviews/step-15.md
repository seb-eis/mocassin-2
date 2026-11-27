# Step 15 Design Review

> Review agent: write the evidence, findings, and verdict directly into
> `docs/planning/reviews/step-15.md`. You may edit only your own metadata (`Review agent`, `Review date`, `Record state`), the checklist completion marks and inline verification notes (not item text), the
> `Evidence`, `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message
> and `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 15 — Energy Engine |
| Request owner | Dev (request assembled by the design agent at the design freeze; the dev takes ownership of resolutions at the start gate) |
| Lead | — (spawns the review agent on request of the design agent/dev) |
| Dev | Not assigned — created from the reviewed `main` commit after the start-gate approval |
| Review initiated by | Lead, on request of the design agent |
| Design class | Full |
| Reviewed commit | `31d5e3438bf6efca66922ba343fdb519a80a3334` (design freeze) |
| Docs branch | `docs/step-15-design` (from `main` = `536c8b3`; Step 14 merged and tagged `step-14`) |
| Review date | 2026-11-22 (round 1) |
| Review agent | GLM-5.3 (opencode-go) — fresh context, no shared conversation with the design agent, dev, lead, or other review agents |

Step page: [`Step 15 — Energy Engine`](../steps/step-15-energy-engine.md)

**Freeze note.** The design docs are committed as the freeze commit `31d5e34`; this request record is a
documentation-only follow-up so the freeze SHA can be written. The freeze contains the Step 15 page rework
(design class `Full`, complexity class `High` with the split decision and owner-acceptance request, the
`fix/step-14-structure-and-tests` precondition, the read-only S2 evaluation, the API corrections, the F-2
criterion, the deviations D-1–D-6, the feature list, tests, budgets, risks), the roadmap §3.1 "Recorded
class — Step 15" note, the `05-open-decisions.md` gated row (Step 15 complexity class) and deferred row
(static S2 corrections), and the `solver-architecture.md` eV→kT wording correction. No code, test,
configuration, contract artifact, or generated file is part of the freeze.

**No code exists yet.** Step 15 has no branch and no implementation. The design is judged on whether the
engine API, the frozen-convention restatements, the evaluation/commit procedures, the test plan, and the
documentation plan are ready to implement.

**Precondition.** The Step 14 audit (owner-accepted 2026-11-21) deferred F-1–F-3/N-1–N-3 to the maintenance
branch `fix/step-14-structure-and-tests`, which must merge into `main` before the step branch is cut. F-2 (the
owner-major delta criterion) is owned by this step; F-3 splits `context/mod.rs` (1,502 lines) and
`context/limits.rs` (863 lines) below the 800-line limit, and the engine lands in that split layout. The start
gate follows that merge.

> Coordination: the lead spawns a fresh review agent after the design agent has frozen the design and completed
> this request, and relays the verdict and owner decisions. The design agent and dev update the design docs,
> apply fixes on the design docs or a maintenance branch, add resolutions without altering review text, and ask
> the lead for re-review until the review agent no longer identifies issues. The lead does not edit the design
> or the review record. The human owner approves the final verdict and the start of implementation.
>
> Design depth: this is a `Full` review. It uses the full checklist and stops when no Blocker or Major remains.
> Minor and Note findings are accepted or deferred and do not trigger another round. A third Full round needs
> an owner decision.

## Step summary

| Field | Value |
| --- | --- |
| Phase | D. Solver |
| Depends on | 14 (Done — merged as `dd5a811`, tagged `step-14`; audit `Passed with notes` owner-accepted 2026-11-21; the deferral branch `fix/step-14-structure-and-tests` must be in `main` before the step branch is cut). Consumes the frozen Step 14 context API (§15 of that page) and the `step-11-amend-2` `ClusterTable::lookup` contract via the context wrapper. |
| Delivers | The transition energy calculator: S0/S1/S2 and the field term (read-only evaluation), the commit-side owner-major pair/cluster delta propagation, `apply_state_change`/`apply_exchange` with the fixed update order, the incremental lattice-energy bookkeeping with its debug consistency check, the MMC exchange energies, and the acceptance-exponential kernel (libm default, fast-exp behind the job flag). No KMC/MMC loop, no counters, no tracker values, no `.mcs`. |
| Sizing | Complexity class **High** (cross-step interfaces — Steps 16–18 consume the engine API; behavioural irreversibility — the delta orientation, the S2 evaluation, the field application, the incremental bookkeeping). **No split proposed**; owner acceptance as one High unit is requested at the start gate per [`04-roadmap.md` §3.1](../04-roadmap.md#31-step-sizing-and-split-triggers). Four features. The §1 API and the §2–§6 conventions are frozen; a forced convention change is a stop-and-report re-review trigger (and an AGENTS.md §10 invariant change additionally needs an ADR). |

## Design decisions reviewed

- [x] **Design class and complexity class.** `Full` (new public Rust API consumed by Steps 16–18; irreversible
      scientific conventions) against the triggers in
      [`design-proportionality.md`](../../development/design-proportionality.md) §1; **High** with the split
      decision and owner-acceptance request per [`04-roadmap.md` §3.1](../04-roadmap.md#31-step-sizing-and-split-triggers)
      (step page §0.2, roadmap note, `05-open-decisions.md` §3 row).
      — *Verified. The `Full` triggers (new public Rust API; irreversible scientific conventions) and the High
      dimensions (cross-step interfaces 16–18, behavioural irreversibility, test-matrix breadth) match
      `design-proportionality.md` §1 and `04-roadmap.md` §3.1; the no-split decision, the owner-acceptance
      request with fallback, the convention guard, the roadmap "Recorded class — Step 15" note, and the
      `05-open-decisions.md` §3 gated row are all present and consistent.*
- [x] **Engine API (step page §1).** `EnergyEngine<'a>` over `&'a mut SimulationContext`; `TransitionEnergies`
      `{s0, s1, s2, barrier_forward, barrier_backward, field_term}` with the with-field barriers and the named
      no-field classification accessors; `ExchangeEnergies`; evaluation read-only and `Result`-returning;
      separate `apply_state_change`/`apply_exchange` with resolve-before-mutate; the single-mutable-borrow
      composition rule (Step 14 F-16/F-19) covering both Step 16's per-call views and a hosting loop context.
      — *Verified. Every accessor/mutator the page names exists in the frozen Step 14 contract (§15 table plus
      the feature-3/4 evidence: `node_energy`, `incoming`, `owner_link`, `cluster_energy`, `cluster_code`,
      `cluster_lookup`, `occupation_code`, `centres`, `link_range`, `partner_of`, `update_particle_ids`,
      `field_scale`/`field_direction`, `ev_to_kt`, `recompute_lattice_energy_kt`, `resolve_node`/`resolve_path`,
      `set_particle`, `set_selectable`); the composition rule restates the F-16/F-19 resolutions verbatim and
      Step 16 §1 already conforms. The "hosting loop context" alternative has no named context accessor in §1
      and Step 17 §1 still sketches the forbidden `ctx` + `EnergyEngine` pair — F-4.*
- [x] **Frozen-convention restatement (step page §2).** The S0/S1/S2 midpoint formulas, the 0.5 pair
      convention, the field-term sign and the `charge_transport_vector` definition, and the cluster-centre
      convention match AGENTS.md §10, the data contract §3.3/§3.4, `physics/energies.md`, and Step 14 §7/§11 —
      restated, not re-decided; the units discipline (kT internally, `ev_to_kt` once per model-table read).
      — *Verified against AGENTS.md §10 (0.5 pair factor, cluster at centre), data contract §3.3 (owner-major
      row/column, `charge_transport_vector` endpoint-moment formula, `field = -0.5·(|E|/kT)·(v·ê)`,
      `tracker_order` as the global index), `physics/energies.md` (site+pair-only vector, per-centre cluster,
      void zero-row, kT), Step 14 §7/§11, and the original (`SetKmcStartTransitionBaseAndFieldEnergyStatesOnContext`,
      `SetDefaultKmcTransitionStateEnergy`, `SetKmcJumpProbabilitiesOnContext`,
      `GetCurrentElectricFieldJumpInfluence`): the restatement is faithful. Two citation defects: the S1
      midpoint rule has no pre-existing normative home among those named (F-7), and the transport-vector
      residual-risk pointer to `05-open-decisions.md` is stale (F-6).*
- [x] **Read-only S2 evaluation (step page §3).** The direct-sum evaluation replacing the original's
      backup/apply/restore; per-receiver owner-major deltas over the receiver's **own** link range (all links,
      including duplicate periodic images — deviation D-2, not the precomputed jump-status links);
      override-based cluster codes via `occupation_code`/`cluster_lookup`; the void-member zero-row rule
      (Step 14 D5-2); no mutation, no scratch, no memory-plan change.
      — *Verified structurally: the direct-sum equivalence premise matches the original's backup/apply/restore
      (`CreateAndBackupKmcTransitionDelta` → `SetFinalKmcStateEnergyOnContext` → restore), `occupation_code`
      overrides and the D5-2 void rule are Step 14 contract, and the D-2 all-links walk is exactness over the
      original's first-link quirk (confirmed: `AddEnvPairEnergyByOccupation` sums every image link into the
      vectors). **Except** the step-2 enumeration restricts receivers to `S0_i ≠ S2_i` and so drops the pair
      deltas at unchanged path nodes that own links to changed nodes — F-1.*
- [x] **Static-correction fail-closed decision (step page §3, D-4).** `None` (the only v1 payload value) is
      dynamic; a present value fails closed (exit 12) because the non-null semantics are undefined in `.msl` v1
      (3-vector payload field; the original's scalar NaN sentinel is reader-rejected here); the optimization is
      deferred to `05-open-decisions.md` §4 with a named follow-up (new projection version per
      step-08-amend-1, compiler-side computation, equivalence tests).
      — *Verified. `TransitionCompilationStage.cs:178` writes `StaticCorrection: null` for every v1 rule; the
      Rust reader rejects non-finite components when a value is present (`map/transition.rs:199-200`), so `None`
      is the only reachable v1 value; the original's correction is a scalar added to S2
      (`KMC_SetStateEnergiesWithStaticCorrection`), so the 3-vector's non-null semantics are genuinely unwritten
      and fail-closed is the only safe behaviour; the deferral row exists in `05-open-decisions.md` §4.*
- [x] **Commit-side deltas and state application (step page §4/§6).** The owner-major incoming-based rule with
      the whole `pc` vector updated; the fixed update order (resolve, particles/flags, tracker ids, pools,
      owner deltas, cluster rebuild, lattice energy); no counters/tracker values (Steps 16–18).
      — *Verified: the `incoming`-based whole-`pc` owner update matches Step 14 §6.1/§7.2 and data contract
      §3.3 (no reverse-link synthesis); the fixed order matches D4-4 (`set_particle` raw-mutator contract),
      D-10 (once-from-final-state codes), and D-6; the mobile-permutation source matches the contract's
      `tracker_order` = global-index definition. **Except** the affected-instance enumeration is member-side
      only (via `ClusterAssoc` on `incoming(V)` links) and misses instances whose centre changes while no
      member changes — F-3.*
- [x] **Incremental lattice energy (step page §5).** The exact on-the-fly `ΔU` decomposition with the 0.5
      applied once, stability crossings handled, and the debug recomputation after every accepted step;
      consistent with Step 14 §7.4 / OD-2.
      — *Verified: the decomposition continues Step 14 §7.4/OD-2 (site once, pair ×0.5 over stable rows,
      cluster per instance × cell, no stability weight on cluster rows), and the changed-environment brackets
      carry the σ weights for stability crossings. **Except** the unchanged-owner `δ_O` term has no σ_O
      stability weight while `U` sums pair rows over stable environments only — F-2.*
- [x] **MMC exchange (step page §7).** The two-site energies with the linked/unlinked cross-term split,
      matching Step 17 §3; `a == b` a no-op.
      — *Verified: the two-site sums, the per-side cross terms, and the §6 commit sequence are internally
      consistent, `a == b` is a no-op that Step 17 filters before calling, and the no-S1/no-field policy matches
      Step 17 §3. Two qualifications: the per-side delta for one-directional links is exact but is not
      recorded in the D-1–D-6 deviations table relative to the original's both-or-nothing rule (F-5), and
      Step 17 §1's engine sketch holds the forbidden double mutable borrow (F-4).*
- [x] **Numerics and the exp kernel (step page §8).** `f64`; computed non-finite results rejected; the
      acceptance-exponential kernel with the ≤ 3.5 % bound and off-by-default fast-exp; Steps 16/17 own the
      draw and comparison.
      — *Verified. `f64` per AGENTS.md §8; `use_fast_exp` is bit 2 of `SolverJobFlags` (data contract §3.4 and
      `mocassin-types/src/settings.rs`); the error bound restates the original's `FastExp32RmsError` class;
      the draw, `K_norm` multiplication, and comparison stay with Steps 16/17 per Step 16 §5.*
- [x] **F-2 ownership (step page acceptance criteria, tests, §9).** The Step 14 audit's deferred owner-major
      delta criterion is stated literally as an acceptance criterion and test row of this step.
      — *Verified. The criterion appears verbatim as an acceptance item ("**F-2 (Step 14 audit deferral, owned
      here):**…"), a §9 hand system, and a test row ("Pair orientation (F-2)"), matching the Step 14 audit
      record's deferral of F-2 to this step (`audits/step-14.md` resolution table).*
- [x] **Feature list, tests, budgets, risks (step page feature table, §10, Tests, Risks).** Four
      `feature/step-15-energy-engine/<slug>` branches; the hand-computed/property/real-package/allocation/
      benchmark matrix; the zero-allocation and no-new-context-array budgets; realistic risks with
      mitigations.
      — *Verified. The four branches cover §1–§8 inside the reserved `energy/` concept module; every fixture
      package named exists with the claimed table counts (`msl-v1` 0 pair/0 cluster, `step-13-populated` 6/1,
      `step-13-centered` 4/0, `step-11-amend-2` 5/1, `step-14-multi-centre-cluster` 12/1 — checked in the
      committed `package-summary-v1.json` files); `benches/energy.rs` and the same-class `baselines.json`
      entry are named in quality strategy §13; the counting allocator is an allowed boundary double. The risk
      table is concrete, but the "incremental `ΔU` drift" row does not cover F-2's unstable-owner case or
      F-3's centre-only rebuild, and the matrix lacks the three test rows those findings name.*
- [x] **Documentation plan (step page Documentation).** The named pages exist (the draft's
      `docs/physics/units-constants.md` is corrected to the glossary notation page's Units section);
      implementation-time ownership is explicit.
      — *Verified. `docs/physics/energies.md`, `docs/glossary/notation.md` (Units section),
      `docs/development/solver-architecture.md`, `docs/development/testing-and-parity.md`, and
      `docs/troubleshooting/errors.md` all exist; the parity rows named ("Pair/cluster/site energy",
      "Electric field", "Fast exponential") are present as Planned for Step 15; the audit request is correctly
      deferred to closeout per AGENTS.md §3.10 (not pre-seeded).*

## Dependency and interface checks

- [x] Dependencies are `Done` and their contracts are stable — Step 14 (merged/tagged; the context API frozen
      at its feature 3) and the `step-11-amend-2`/`step-11.1` cluster-code contract; the
      `fix/step-14-structure-and-tests` precondition is recorded with its effect on the module layout.
      — *Verified. Step 14 is Done (merged `dd5a811`, tagged `step-14`, audit `Passed with notes` owner-accepted
      2026-11-21) and `step-11.1` is merged and tagged; the precondition is recorded on the step page, in the
      roadmap §3.1 note, and in the `05-open-decisions.md` §3 row. State observed at review time: the
      maintenance work exists as **unmerged local commits** (tip `b632565`, containing the F-3 split
      `context/build.rs`, `context/invariants.rs`, `context/limits/tests.rs`); `main` is still `536c8b3` with
      `context/mod.rs` at 1,502 lines and `context/limits.rs` at 863. The start gate must wait for that merge,
      as the record states.*
- [x] Inputs, outputs, types, and formats are defined — the engine consumes the Step 14 accessors and mutators
      plus crate-internal helpers; no public Step 14 API break; no new context arrays or plan entries.
      — *Verified. Every §1-listed accessor exists in the frozen Step 14 §15 contract or its feature evidence;
      the engine is zero-sized with bounded locals (path ≤ 10, cluster slots ≤ 9); the freeze diff touches no
      code, `Cargo.toml`, or manifest, so no dependency or API surface moves.*
- [x] Versioning and migration behavior are defined — no `.msl`/`.msr`/`.mcs`/manifest/schema/version/signature
      change; the static-correction payload field is consumed exactly as frozen.
      — *Verified. The freeze commit `31d5e34` changes exactly four documentation files; the payload
      `static_correction` semantics (writer emits `null` per `TransitionCompilationStage`, reader enforces
      finite components) are consumed as written; `05-open-decisions.md` §4 carries the future projection
      version requirement.*
- [x] No unresolved cross-step interface question remains — Step 16 (per-call engine views, barriers and
      classification accessors, the exp kernel), Step 17 (the exchange entry points), and Step 18 (the
      tracker-value boundary) are each stated; the KMC/MMC loop, counters, and outcome classification are
      explicit non-goals.
      — *Verified for Step 16 (§1/§5 of its page conform to the F-19 composition and consume
      `barrier_forward/backward` and the no-field accessors) and Step 18 (values/inverse-mapping boundary
      stated; the engine's permutation source matches the contract's `tracker_order` = global index). For
      Step 17 the exchange entry points are defined, but its page still sketches the `ctx` +
      `EnergyEngine<'a>` double mutable borrow that §1 forbids (F-4), and its §3 wording predates the §7
      per-side refinement (F-5); Step 18's page wording on `tracker_order_code` is stale (F-9 note).*

## Standard design-review checklist

Use this full checklist for a `Full` step.

- [x] Design class (`Full`) is recorded and matches the triggers
      — *Reviewed. Recorded on the step page and in this request; the triggers (new public Rust API consumed by
      Steps 16–18; irreversible scientific conventions) match `design-proportionality.md` §1, and the page
      correctly narrows the review scope to API and physics consistency rather than contract migration.*
- [x] Scope, non-goals, and acceptance criteria are explicit and testable
      — *Reviewed. The non-goals are unusually explicit (no KMC/MMC loop, no counters/tracker values/`.mcs`, no
      format/manifest change, no .NET change, no new crate, no context memory-plan change, no alpha-S1, no
      `OPT_LINK_ONLY_MOBILES`/parallel/fast-math); every acceptance criterion maps to a named §9 system or test
      row, except the three gaps named by F-1/F-2/F-3.*
- [x] Complexity class and split decision are recorded and justified per
      [`04-roadmap.md` §3.1](../04-roadmap.md#31-step-sizing-and-split-triggers)
      — *Reviewed. High on cross-step interfaces and behavioural irreversibility with one acceptance story; the
      no-split decision carries the seam argument, the owner-acceptance request, the fallback, and the
      convention guard, recorded on the page, in the roadmap "Recorded class — Step 15" note, and in
      `05-open-decisions.md` §3.*
- [x] Design decisions are complete and internally consistent
      — *Reviewed. Mostly complete; F-1/F-2/F-3 are material inconsistencies inside the frozen §3/§4/§5
      formulas (unchanged-receiver pair deltas, the unstable-owner `ΔU` weight, the centre-only-changed cluster
      rebuild), each contradicting another part of the same design; F-4–F-8 are smaller corrections.*
- [x] Required tests, property tests, golden fixtures, and performance targets are defined
      — *Reviewed. The hand-computed/real-package/property/allocation/benchmark matrix is defined, reuses five
      committed real-writer fixtures (verified table counts), names the Criterion benchmark and same-class
      baseline per quality strategy §13, and uses only the counting allocator as a test double (an allowed
      boundary). F-1/F-2/F-3 each name a missing test row (spectator receiver, unstable link owner,
      centre-only cluster change).*
- [x] Documentation, status, parity, and audit updates are planned
      — *Reviewed. All named documentation pages exist; the parity rows are Planned for Step 15 and move as the
      features land; the audit request is correctly created at closeout rather than pre-seeded. (The status
      dashboard's design-review column still reads "Not requested" — step-agent-owned bookkeeping updated at
      the start gate per Step 14 §23's note; status bookkeeping is explicitly not a design finding under
      `design-proportionality.md` §2.)*
- [x] Licensing, provenance, and dependency checks are resolved
      — *Reviewed. The freeze touches no code, `Cargo.toml`, or manifest, so no new dependency exists; the
      original is cited read-only and nothing is copied (the Original reference section paraphrases behaviour);
      `verify.sh policy` passes the SPDX (1,111 files) and cargo-deny licence checks; NOTICE/spglib provenance
      is unchanged by this step.*
- [x] Risks and mitigations are realistic
      — *Reviewed. The risk rows are concrete (delta orientation with the F-2 oracle, duplicate images, void
      tuples, field sign, static fail-closed, partial apply, `ΔU` drift, fp drift, read-only regression), but
      the "incremental `ΔU` drift (stability crossings, shared owners)" mitigation does not cover F-2's
      unstable-unchanged-owner case or F-3's centre-only rebuild, and no listed test would expose F-1.*
- [x] Code layout and namespace plan follow the structure policy
      — *Reviewed. `energy/` is reserved by `code-structure.md` §2.2 as its own concept module, distinct from
      `context/energy.rs` (the runtime tables); the engine must land below the 800-line limit in the post-F-3
      split layout, which the precondition enforces; test/bench file names follow the Step 14 pattern
      (`tests/context_acceptance.rs`, `tests/allocation.rs`, `benches/*.rs`).*
- [x] No hidden scope, stale placeholders, or unowned decisions
      — *Reviewed. The deferrals are explicit with owners (static S2 in `05-open-decisions.md` §4 with the
      projection-version requirement; alpha-S1 to the Step 19 ABI; `OPT_LINK_ONLY_MOBILES` and the fast-math
      measurements to Step 29); the two genuinely new irreversible behaviours (D-4 fail-closed, D-3 raw-S1
      rule) are documented with tests. F-6 records the stale registry pointer for the transport-vector
      residual risk; the deviations table is missing the MMC one-directional-link refinement (F-5).*

## Known limitations and deferrals

- **Static S2 corrections are deferred** (step page §3/D-4, `05-open-decisions.md` §4): the v1 payload writes
  `static_correction = null` for every rule, so dynamic S2 is the only v1 mode; a present value fails closed.
  Landing the optimization needs a compiler-side computation, a defined payload projection (a new `model-v1`
  projection version per step-08-amend-1), and dynamic-vs-static equivalence tests.
- **`charge_transport_vector` trust boundary** (step page §2): the vector is consumed verbatim from the
  untrusted `.msl`; the full positional formula cannot be recomputed because v1 carries no fractional P1
  coordinates. Non-finite results are rejected; the residual risk and the future `.msl` revision stay recorded
  (Step 13/14 assignment, `05-open-decisions.md`).
- **Fixture coverage:** no new committed fixture package; the ceria fixtures are table-free (site-only rules),
  so the pair/cluster acceptance rides the existing `step-13-populated`, `step-11-amend-2`, and
  `step-14-multi-centre-cluster` packages plus in-process hand-built rule/lattice variants (the Step 14 test
  pattern).
- **The maintenance-branch precondition:** this design assumes `fix/step-14-structure-and-tests` (F-1–F-3,
  N-1–N-3) is in `main` before the step branch is cut; the F-3 file split is not duplicated or pre-empted
  here — the page only constrains the engine to land below the 800-line limit in that layout.
- **Alpha-method S1** and other interpolation variants are explicit non-goals (a future Step 19 ABI decision).
- **The two-state detailed-balance test** uses a minimal in-test Metropolis driver over the public engine;
  the production sampler is Step 17's.

## Evidence

Files reviewed:

- `AGENTS.md` §3 (design-review process, roles, write scope), §4.8 (no silent scientific corrections), §8 (Rust
  conventions), §10 (scientific invariants), §12 (testing expectations), §13 (licensing).
- `docs/planning/reviews/README.md` (statuses, severities, review-agent write scope, the audit-plan note that
  audit records are created at closeout); this request record `docs/planning/reviews/step-15.md`.
- The Step 15 page (all 700 lines): §0.1 scope/non-goals, §0.2 complexity class, §1 API, §2 transition
  energies, §3 S2 evaluation, §4 commit deltas, §5 lattice energy, §6 state application, §7 MMC exchange,
  §8 numerics, §9 validation systems, §10 budgets, §11 deviations D-1–D-6, tasks/acceptance/tests/risks.
- `docs/planning/steps/step-14-simulation-context-initialization.md` — §2/§3 (flags, `STABLE`/`MOBILE`),
  §6.1–§6.3 (owner-major CSR, inverse index, cluster instances/codes, `occupation_code`), §7.1–§7.4 (units,
  site+pair vectors, cluster vectors, initial lattice energy), §9.1–§9.5 (path resolution, jump links, static
  corrections), §10 (tracker index space), §11.2 (transport-formula contract), §13 (exit table), §14
  (deviations D-2/D-3/D-5/D-7/D-9/D-10/D-11, D2-2/D2-3, D4-4, D5-1/D5-2), §15 (the Step 15 interface table),
  §23 (reconciliation).
- `docs/planning/steps/step-13-rust-solver-foundation-and-package-reader.md` — §3.5 risks and §12 interfaces
  (the `charge_transport_vector` trust-boundary assignment, `static_correction` reader rule at line 816).
- `docs/planning/steps/step-16-kmc-engine.md`, `step-17-mmc-engine.md`, `step-18-tracking-statistics-and-state-writer.md`
  (consumer interfaces: composition, barriers/no-field classification, exchange entry points, `a == b`
  filtering, tracker-value boundary).
- `docs/planning/02-data-contracts.md` §3.3 (pair-link orientation and row/column meaning, cluster
  codes/expansions/`particle_table_mapping`, `charge_transport_vector`, `static_correction`, `tracker_order`,
  state codes) and §3.4 (`SolverJobFlags` bit 2 `useFastExp`); `docs/physics/energies.md` (the total-lattice
  decomposition, owner-major rule, void-member rule, units); `docs/planning/04-roadmap.md` §3.1 and the
  "Recorded class — Step 15" note; `docs/planning/05-open-decisions.md` §2/§3/§4 (OD-2, the Step 15 class row,
  the static-S2 deferral row — and the absence of any transport-vector row);
  `docs/planning/appendix-b-parity-matrix.md` §7 (the Step 15 rows); `docs/planning/03-quality-strategy.md`
  §9.5/§13; `docs/development/design-proportionality.md` §1/§2 (triggers, stop rule, non-findings);
  `docs/development/code-structure.md` §2.2 (the `energy/` reservation, the 800-line rule);
  `docs/development/solver-architecture.md` (the corrected eV→kT wording in context); `NOTICE.md`.
- `docs/planning/audits/step-14.md` (F-1–F-3/N-1–N-3 severities, deferrals, the F-2 ownership assignment) and
  `docs/planning/reviews/step-14.md` (F-16/F-19 composition resolutions, F-26, F-29).
- Merged code consulted for the design's factual claims: `src/rust/crates/mocassin-solver/src/context/energy.rs`
  (`ev_to_kt` applied per read; model tables remain eV at line 204), `src/rust/crates/mocassin-msl/src/map/transition.rs`
  (`static_correction` finiteness, lines 199–217), `src/rust/crates/mocassin-types/src/runtime/transition.rs`
  (`static_correction: Option<[f64; 3]>`), `src/rust/crates/mocassin-types/src/settings.rs` (flag bits),
  `src/dotnet/Mocassin.Compilation/Transitions/TransitionCompilationStage.cs:178` (`StaticCorrection: null`),
  `wc -l src/rust/crates/mocassin-solver/src/context/{mod,limits}.rs` (1,502/863 on this branch).
- Fixtures: `tests/contracts/packages/{msl-v1,step-13-populated,step-13-centered,step-11-amend-2,
  step-14-multi-centre-cluster}/package-summary-v1.json` — pair/cluster table counts 0/0, 6/1, 4/0, 5/1, 12/1.
- The original (read-only GPL reference clone `/workspace/mocassin-original`): `MainRoutines.c`
  (`SetDefaultKmcTransitionStateEnergy` — `S1 = S0 + RawS1 + 0.5(S2−S0)`; `SetKmcJumpProbabilitiesOnContext` —
  `b01 = (S1−S0)+E_field`, `b10 = (S1−S2)−E_field`; the alpha-method plugin variant; the outcome order),
  `EnvironmentRoutines.c` (`SetKmcStartTransitionBaseAndFieldEnergyStatesOnContext` — node 0 contributes zero
  to `RawS1`; `AddEnvPairEnergyByOccupation`/`AddEnvClusterEnergyByOccupation` — every image link and the
  centre-folded cluster enter `EnergyStates`; `CreateAndBackupKmcTransitionDelta`/`Set/LoadFinalStateEnergyBackup`
  — the backup/apply/restore S2; `InvokeJumpLinkDeltas` — the delta at every receiver's `StateCode2` row for
  every jump link; `KMC_SetStateEnergiesWithStaticCorrection` — the scalar correction),
  `StatisticsRoutines.h:134-143` (`GetCurrentElectricFieldJumpInfluence`), `KineticSimulationModelBuilder.cs`
  (`GetChargeTransportVector`).

Commands run:

- `./tools/verify.sh docs` → **exit 0** (run twice): Zensical strict build "0 warning(s), 0 error(s)";
  DocFX "0 Warning(s), 0 Error(s)"; rustdoc generated for all seven crates; "Documentation math conventions
  OK (208 files scanned)"; "Documentation links OK (211 files scanned)".
- `./tools/verify.sh policy` → **exit 0**: ApiCompat "Public API is compatible with the checked-in baseline"
  (both checked assemblies); "Contract artifacts are current (26 files)"; model/parameterization/lattice
  projection checks OK (5/4/2 fixtures); space-group catalog current (274 settings, sha256 `1f3bf67d…`);
  process consistency OK; 62 tool tests OK; "SPDX headers present in 1111 source files"; "No secret patterns
  found in 1640 tracked text files"; "advisories ok, bans ok, licenses ok, sources ok".
- `git diff --stat 31d5e34 ed31fa8` → exactly `docs/planning/reviews/step-15.md` (+256); `git show --stat
  31d5e34` → four documentation files (`step-15-energy-engine.md`, `04-roadmap.md`, `05-open-decisions.md`,
  `solver-architecture.md`), no code/test/configuration/contract artifact in the freeze; `git status` clean
  before this record edit.
- Fixture-table counts via a JSON scan of the five committed `package-summary-v1.json` files (see Files
  reviewed); code greps listed above; `wc -l` on the two F-3 context files.

## Findings

| Severity | Origin | Finding | Evidence |
| --- | --- | --- | --- |
| **Major** | design | **F-1 — The §3 S2 delta enumeration drops unchanged receivers, so `s2` is wrong whenever a path node that does not change owns a pair link to a node that does.** Step 2 accumulates `Δ_i` only "for each path node `i` with `S0_i ≠ S2_i`", but step 3 sums `E[p_i][S2_i] + Δ_i` over every path node, and the section's own preamble — "the S2-state energy of a path node differs from the current vector entry only through the partners that changed, and every one of those partners is a path node" — holds for **unchanged** nodes exactly as for changed ones: an unchanged node's current entry still reflects its partners' `S0` particles, and its `S2`-state value needs `pair_table[ℓ][S2_i = S0_i][S2_j] − pair_table[ℓ][S0_i][S0_j]` at its own row when a linked partner `j` changes. A rule with a spectator node (`S0_i == S2_i`) whose payload link is stored at that node and points at a changed node therefore gets a short `s2`, and the error flows into `conf`, `s1`, both barriers, and acceptance. This contradicts §7 (the MMC form includes `Δ_b` at an unchanged `b`), Step 14 §9.2 (the receiver filter of the corresponding jump-link set is *structural stability*, not a change filter, and the links cover every ordered pair), and the original (`InvokeJumpLinkDeltas` applies every sender's delta at every receiver's `StateCode2` row, changed or not). No §9 system or test row covers a spectator receiver, so the matrix would validate the wrong formula. Fix: drop the `S0_i ≠ S2_i` receiver filter (walk every path node's own links; the partner filter `S0_j ≠ S2_j` may remain as a zero-delta skip), state explicitly whether the receiver/partner set keeps the §9.2 structural-stability filter (matching the original) or is unfiltered (exact), and add a hand-computed spectator-receiver system with its test row. | Step 15 §3 preamble (lines 338–340) vs step 2 (lines 345–347) and step 3 (line 363); §7 `Δ_b` at an unchanged side (lines 492–498); Step 14 §9.2 ("For every ordered pair … with both nodes structurally stable"); original `EnvironmentRoutines.c:679-707` (`InvokeJumpLinkDeltas` updates the receiver's `StateCode2` row for every jump link) and `MainRoutines.c` `SetFinalKmcStateEnergyOnContext` (S2 sums every path id); §9/test matrix contain no spectator-node case. |
| **Major** | design | **F-2 — The §5 incremental `ΔU` adds the unchanged-owner delta `δ_O` without the stability weight that `U`'s `Σ_stable` requires, so the lattice energy drifts whenever an unstable environment owns a link to a changed environment.** `U` counts pair rows over **stable** environments only (`U = Σ_stable site + 0.5 Σ_stable pair + Σ cluster`, restated in §5 line 431), and an unchanged owner's `STABLE` flag cannot change; the correct pair-row change at an unchanged owner `O` is therefore `σ_O·δ_O`, not `δ_O`. As written, `ΔU` injects `0.5·δ_O` for unstable unchanged owners whose rows `U` never counted — pair links owned by unstable environments are a supported payload shape (`physics/energies.md`: "pair interactions sampled from stable–stable and unstable–stable environments") — corrupting `lattice_energy_ev`, the MMC break check, and Step 18's reporting. The first bracket correctly carries `σ_V`/`σ'_V` for changed environments, and the risk row names "stability crossings" only for changed environments, so the gap is unmitigated; no listed test places an unstable link owner in the property-test lattice. Fix: weight `δ_O` by `σ_O` (or restrict the `O` sum to stable owners) and add an unstable-owner row to the incremental-vs-full property test. | Step 15 §5 formula (lines 443–449: the second bracket `Σ_{V} Σ_{O ∈ incoming(V)\changed} δ_O` has no `σ_O`) and `δ_O` text (line 451) vs the `Σ_stable` decomposition (line 431) and Step 14 §7.4 (`Σ_stable` for site and pair); `physics/energies.md` §"Energy Contributions" (unstable–stable pair sampling); §9/test matrix name no unstable link owner. |
| **Major** | design | **F-3 — The §4/§6 affected-instance enumeration is member-side only, so an instance whose centre changes while none of its members changes is never rebuilt and goes stale.** The cluster rebuild is defined over "each `ClusterAssoc { instance, slot }` on `L`" where `L` comes from `ctx.incoming(V)` of each **changed** environment `V` — that reaches exactly the instances in which a changed environment occupies a member slot. An instance centred at a changed path node with all members off-path or unchanged is in no changed member's `incoming` set (its own `cluster_assoc` entries ride on its *forward* links, reachable only via a changed member), yet its slot-0 code, its energy rows, and its `U` term all change — §5's own parenthetical "(a centre particle change moves the row)" shows the intent. With the enumeration as written, `codes[instance, cell]` keeps the old centre particle, the stored rows keep the old code's energies, the `ΔU` cluster bracket misses `C' − C`, and every later `s0` read at that centre is wrong. Step 14 §7.3 confirms the two precomputed indexes are `centres` (position → instances centred there) and the incoming/`cluster_assoc` route; only the latter is specified on the commit side. Fix: define the affected-instance set as the deduplicated union over changed `V` of the member-side route **and** `centres(position(V))`, and add a test that changes only a cluster's centre (members unchanged) and asserts the rebuilt code/rows and `ΔU`. | Step 15 §4 (lines 400–418: `incoming(V)` → `L` → `ClusterAssoc`) and §6 step 6 ("Once per affected instance", no centre route); §5 cluster bracket and "(a centre particle change moves the row)" (lines 448, 454); §2 "an instance whose centre is not a path node … is updated when the state is committed" (line 288-290) — the centre-on-path counterpart is unspecified; Step 14 §7.3 ("Step 14 precomputes both indexes"); §9/test matrix name no centre-only change. |
| **Minor** | design | **F-4 — The Step 17 consumer contradicts the frozen composition rule, and the "hosting loop context" alternative has no named accessor.** Step 17 §1 sketches `MmcEngine<'a> { ctx: &'a mut SimulationContext, energy: EnergyEngine<'a>, … }` — exactly the "No struct may hold both `ctx: &'a mut SimulationContext` and an `EnergyEngine<'a>`" double mutable borrow that Step 15 §1 forbids and the compiler rejects; Step 16's page was corrected for Step 14 F-19 but Step 17's was not. Step 15 §1 offers the hosting form ("a loop context may itself host the engine view and access the context through it") but the engine's `ctx` field is private and §1 lists no context accessor, so the hosting form is not implementable as specified — Step 17 must either use Step 16's per-call form or the design must name an additive accessor. Both are Step 17 design-time decisions, but this freeze's "no unresolved cross-step interface question" claim needs the qualification. | Step 15 §1 composition paragraph (lines 250–254); Step 17 §1 (lines 63–68, the `MmcEngine` struct); Step 16 §1 (lines 66–71, 98–101 — the corrected F-19 form); `docs/planning/reviews/step-14.md` F-16/F-19 resolution rows. |
| **Minor** | design | **F-5 — The MMC per-side delta for one-directional links is an unrecorded deviation from the original's both-or-nothing rule.** §7 gives each side its own cross-term (`Δ_a` from `a`'s own links to `b`, `Δ_b` symmetrically, zero per side when that side has no link), which is exact and correctly tested against brute force — but the original's MMC (Step 17 §3 original reference) applies the two-position deltas only when the sites "are linked in both directions" and otherwise reads the entries uncorrected, so a one-directionally linked exchange yields different `s2` values in the two implementations. §11 presents D-1–D-6 as the deviation list and none records this; AGENTS.md §4.8 requires the chosen interpretation to be documented *as a deviation* with its test. Add it to the D-table (or extend D-2's rationale to the exchange case) and make the asymmetric one-directional exchange an explicit MMC test case. | Step 15 §7 (lines 492–499: "the owner-major rule still applies per side and no reverse link is synthesised") and the §11 table's absence of the case; Step 17 §3 original reference ("If both positions are within the interaction range and links exist both ways, apply the deltas for both positions …; otherwise … no cross-term"); test row "MMC exchange | Linked and unlinked two-site deltas equal brute force". |
| **Minor** | documentation | **F-6 — The transport-vector residual risk cites a registry row that does not exist.** §2's trust-boundary paragraph says the residual risk and the future `.msl` revision "stay recorded in [`05-open-decisions.md`] and Step 14 §23"; neither contains them (verified by grep — `05-open-decisions.md` has no transport-vector/field row in any section, and Step 14 §23 is a document-link reconciliation list). The substance is recorded in Step 13 §3.5/§12 and restated in Step 15 §2 itself, so no decision is lost, but the named open-decisions entry the reader is sent to is absent and the future-format item is untracked in the registry. Fix: add the row (deferred: fractional P1 positions or a signed transport vector in a future `.msl` revision, owner Step 13/14, trigger the next `.msl` revision) or correct the two citations to Step 13 §3.5/§12. | Step 15 §2 (lines 325–331); `grep -n 'transport\|fractional P1' docs/planning/05-open-decisions.md` → only ADR 0012 and OD-1 rows; Step 14 §23 (lines 1696–1714, no transport-vector entry); Step 13 §3.5/§12 (lines 1364, 1385–1386 — the actual record). |
| **Minor** | documentation | **F-7 — The "frozen plan content" citation for the S0/S1/S2 midpoint formulas overstates their pre-existing homes.** §0.2/§2 cite "AGENTS.md §10, ADR 0012 context, `docs/physics/energies.md`" as where the formulas are frozen; AGENTS.md §10 freezes the 0.5 pair factor and cluster-at-centre but not the S1 midpoint rule; `energies.md` does not state S1 at all (the Documentation plan adds "S0/S1/S2 and the midpoint rule" to that page at implementation time); ADR 0012 is the transport/MSD decision. The formulas themselves are faithful to the original (verified `SetDefaultKmcTransitionStateEnergy`: `S1 = S0 + RawS1 + 0.5·conf`, the alpha variant plugin-only) and the page's guard covers changes, but the midpoint rule's first normative home is this page — "restated, not re-decided" should name the original's default plus this freeze rather than the three cited homes. | Step 15 §0.2 (lines 144–146) and §2 (line 281); `grep -rn 'midpoint\|RawS1' docs/physics/ docs/planning/02-data-contracts.md AGENTS.md` → no midpoint-rule statement; original `MainRoutines.c:838-843`; the Documentation section's own "energies.md — S0/S1/S2 and the midpoint rule" implementation-time item. |
| **Minor** | documentation | **F-8 — Step 14 §7.2's units wording contradicts the per-read conversion this design relies on.** The merged Step 14 page says "Pair tables are converted to kT once at initialization", which reads as an in-place conversion of the runtime tables; the merged code converts at read and leaves the model tables in eV (`context/energy.rs:204` reads `table.energies[…] * ev_to_kt`), which is exactly what Step 15 §2 and the freeze-corrected `solver-architecture.md` wording require ("Step 14 converts the site table, environment vectors, and cluster rows … Step 15 scales each remaining model-table read … exactly once per read"). An implementer reading Step 14 §7.2 alone could skip the per-read factor and mix eV deltas into kT vectors. The Step 15 text is right; touch up the Step 14 wording on the already-open maintenance branch when convenient. | Step 14 §7.2 (line 1048) vs Step 15 §2 (lines 297–300) and the `31d5e34` `solver-architecture.md` diff; `src/rust/crates/mocassin-solver/src/context/energy.rs:132,170,203-204,293` (all direct reads multiply by `ev_to_kt`; the runtime model keeps eV). |
| **Note** | documentation | **F-9 — Step 18's mobile-tracker wording predates the contract's `tracker_order` definition.** Step 18 §1 says the mobile tracker is transferred "according to the rule's `tracker_order_code`", but the data contract §3.3 and Step 15 §6 define `tracker_order` as the compiled **global-tracker** index and derive the mobile permutation from the state-code movement. No decision is lost (the boundary — the engine permutes `mobile_tracker` ids, Step 18 owns values and the live inverse mapping — is stated on both sides), but Step 18's own design review should reconcile its §1 wording with the contract. | Step 15 §6 step 3; `02-data-contracts.md` §3.3 (`tracker_order[i]` = global tracker index, `0xFF` sentinel); Step 18 §1 (line 82 vs the definition at line 83); Step 14 §10 ("Step 18 owns values and `tracker_order` following"). |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. Origin: **design**, **implementation**,
**evidence**, or **documentation**. The review agent writes the findings and verdict directly. The dev adds
resolutions in the separate `Resolution status` section without altering the review agent's findings, evidence,
or verdict.

## Record state

**State:** Re-review required

`Re-review required` is a record state, not a verdict.

## Reviewer independence

> I am a fresh context. I did not author or implement the reviewed work and have no shared conversation with
> the design agent, dev, or lead.

## Verdict

- [ ] Ready for implementation
- [ ] Ready with notes
- [x] Changes required
- [ ] Waived

Follow-up owner: Design agent and dev — correct the §3/§4/§5 enumerations and the citation defects on the
`docs/step-15-design` branch, add the resolutions without altering this round's text, and mark the record
`Re-review required`.
Follow-up step: Step 15 design re-review (round 2) at the corrected freeze commit; the start gate then still
waits for the `fix/step-14-structure-and-tests` merge into `main` and the owner's acceptance of the High
complexity class.

## Approval summary

The request owner (dev or design agent) completes this section before the lead presents the approval
request. It must cover the full history, not only the latest round.

- [ ] Requested decision and frozen commit recorded
- [ ] Every review/audit round recorded (round, date, commit, review agent, verdict)
- [ ] Every finding recorded (ID, severity, issue, evidence, resolution, fix commit, status)
- [ ] Accepted notes and waivers recorded with rationale
- [ ] Residual risks, deferred items, and follow-up owners recorded
- [ ] Explicit no-Blocker/no-Major statement, or the exact exceptions being waived
- [ ] Final CI and documentation-build results recorded

Round history:

| Round | Date | Commit | Review agent | Verdict |
| --- | --- | --- | --- | --- |
| — | — | — | — | — |

Findings and resolutions:

| Finding | Severity | Issue | Resolution | Commit | Status |
| --- | --- | --- | --- | --- | --- |
| — | — | — | — | — | — |

## Human approval

- [ ] Accepted: Ready for implementation
- [ ] Accepted: Ready with notes
- [ ] Changes required
- [ ] Waived

Approved by:
Date:

## Resolution status

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| F-1 | Fixed | Design agent | `af9ded8` |
| F-2 | Fixed | Design agent | `af9ded8` |
| F-3 | Fixed | Design agent | `af9ded8` |
| F-4 | Fixed in Step 15; Step 17 action deferred to its design review | Design agent | `af9ded8` |
| F-5 | Fixed | Design agent | `af9ded8` |
| F-6 | Fixed | Design agent | `af9ded8` |
| F-7 | Fixed | Design agent | `af9ded8` |
| F-8 | Correct rule fixed in Step 15; Step 14 wording deferred to a docs maintenance edit | Design agent | `af9ded8` |
| F-9 | Deferred to Step 18's design review | Design agent | `af9ded8` |

Resolution detail:

- **F-1 (fixed).** Step page §3 now walks **every** path node's own forward links, changed or spectator; the
  `S0_i ≠ S2_i` receiver filter is removed, the partner-change filter is only a zero-delta skip, and the walk is
  explicitly unfiltered by structural stability (the jump-status set is not the evaluation source). D-1/D-2, the
  spectator-receiver hand system, test row, acceptance criterion, and risk row are added.
- **F-2 (fixed).** Step page §5 weights every unchanged owner's per-link delta by `σ_O`, with `δ_{(O,V)}`
  defined per link; the `Σ_stable` rationale is stated. The unstable-link-owner hand system, test row,
  acceptance criterion, property-test coverage, and risk row are added.
- **F-3 (fixed).** Step page §4/§6 define the affected set as the deduplicated union of the member-side
  `ClusterAssoc` route and `centres(position(V)) × cell(V)` for every changed `V`, rebuilt and accounted once per
  `(instance, cell)`. The centre-only hand system, test row, acceptance criterion, and risk row are added.
- **F-4 (fixed in Step 15).** Step page §1 removes the unimplementable "hosting loop context" alternative and
  states that the per-call `EnergyEngine::new(&mut *self.ctx)` form is the only supported composition; it names
  Step 17's §1 sketch as the forbidden double borrow and defers the Step 17 edit to that step's design review.
- **F-5 (fixed).** Step page §7 records the independent per-side one-directional exchange as deviation D-7, with
  the asymmetric one-directional MMC test and the linked/unlinked/one-directional matrix row.
- **F-6 (fixed).** `05-open-decisions.md` §4 gains a fractional-P1-positions / signed-transport-vector row, and
  Step page §2 now cites Step 13 §3.5/§12 plus that row instead of the non-existent Step 14 §23 entry.
- **F-7 (fixed).** Step page §0.2/§2 name this page as the S1 midpoint rule's first normative home, restated
  from the original's default; AGENTS.md §10, ADR 0012, and `energies.md` are no longer cited for S1.
- **F-8 (deferred, Step 15 rule fixed).** Step page §2 states the per-read conversion unambiguously and records
  that the merged Step 14 §7.2 shorthand means the site table and context vectors; the pair/cluster model tables
  stay in eV. Correcting the completed Step 14 page is a documentation maintenance edit outside this design
  re-freeze (owner: dev, a `docs/` branch).
- **F-9 (deferred).** Step 18 §1's `tracker_order_code` wording is reconciled at Step 18's design review; Step 15
  §6 already states that `tracker_order` is the global index and derives the mobile permutation from the
  state-code movement (owner: Step 18 design).

The dev fills this section after the verdict. Do not alter the review agent-authored findings, evidence, or
verdict.

## Review rounds

Append each new round below. Never overwrite an earlier round.

### Review round 1

#### Independence and frozen state

I am a fresh, independent review agent (GLM-5.3, opencode-go). I did not author or implement the Step 15 design
and have no shared conversation with the design agent, the dev, the lead, or any other review agent. I received
only the frozen commit, the request record, the repository, and the required commands/scope; no desired verdict
or design summary was supplied. I read the repository at the frozen commit myself — the step page, the Step
13/14/16/17/18 pages, the data contracts, the physics and process pages, the merged Rust/.NET code that the
design's factual claims depend on, the committed fixtures, and the original solver sources (read-only GPL
reference) — rather than relying on the request's summary. The review was read-only apart from this record: I
edited and committed only `docs/planning/reviews/step-15.md`; I did not merge, tag, push `main`, delete
branches, or fix any finding.

Frozen state: the reviewed commit is `31d5e3438bf6efca66922ba343fdb519a80a3334` (the design freeze). The record
HEAD `ed31fa8323e25012c2894dbff5c465dab6e9c523` is the documented documentation-only request commit;
`git diff --name-only 31d5e34..ed31fa8` is exactly this record file, so no design, code, test, configuration,
contract, or generated artifact moved between the freeze and this review. The freeze itself touches four
documentation files (the step page, `04-roadmap.md` §3.1, `05-open-decisions.md` §3/§4, and the
`solver-architecture.md` eV→kT correction); no code exists for Step 15, and the branch matches `origin/docs/step-15-design`.
Precondition state observed during the review: `main` is `536c8b3`; the `fix/step-14-structure-and-tests`
maintenance work exists as unmerged local commits (tip `b632565`, including the F-3 split files
`context/build.rs`, `context/invariants.rs`, `context/limits/tests.rs`), and `context/mod.rs`/`limits.rs` are
still 1,502/863 lines on this branch. The start gate correctly blocks on that merge; nothing in this verdict
depends on it.

#### Commands and results

- `./tools/verify.sh docs` → exit 0 (run twice): Zensical strict 0 warnings/0 errors; DocFX 0 warnings/0
  errors; rustdoc for all seven crates; math conventions OK (208 files); links OK (211 files).
- `./tools/verify.sh policy` → exit 0: ApiCompat clean against the baseline; contract artifacts current
  (26 files); model/parameterization/lattice projections OK (5/4/2 fixtures); catalog current (274 settings);
  process consistency OK; 62 tool tests OK; SPDX 1,111 files; no secrets in 1,640 files; cargo-deny
  advisories/bans/licenses OK.
- `git diff --stat 31d5e34 ed31fa8` → only `docs/planning/reviews/step-15.md`; `git show --stat 31d5e34` → the
  four freeze files; `git status` clean before the record edit.
- Original-source consultation and fixture/code checks are listed in the Evidence section (read-only).

#### Findings

**Major (3):**

- **F-1** — The §3 read-only S2 evaluation accumulates `Δ_i` only for path nodes with `S0_i ≠ S2_i`, but sums
  `E[p_i][S2_i] + Δ_i` over **all** path nodes. An unchanged path node that owns a link to a changed node needs
  its own-row correction (`pair_table[ℓ][S0_i][S2_j] − pair_table[ℓ][S0_i][S0_j]`) and gets none, so `s2`
  under-counts whenever a rule has a spectator node linked to a changed node. This contradicts §3's own
  preamble, §7's MMC per-side form, Step 14 §9.2's structurally-stable (not change-based) receiver set, and the
  original's `InvokeJumpLinkDeltas`. The fix is to walk every path node's own links (the partner filter may stay
  as a zero-delta skip), state whether the receiver set keeps the §9.2 structural-stability filter, and add a
  spectator-receiver hand system and test.
- **F-2** — The §5 incremental `ΔU` adds `δ_O` for unchanged owners without the `σ_O` stability weight, while
  `U` sums pair rows over stable environments only. An unstable unchanged owner of a link to a changed
  environment (a supported payload shape) injects a pair-row delta `U` never counted, drifting
  `lattice_energy_ev`, the MMC break check, and Step 18 reporting. Weight `δ_O` by `σ_O` (or restrict the sum to
  stable owners) and add an unstable link owner to the incremental-vs-full property lattice.
- **F-3** — The §4/§6 affected-instance enumeration reaches only instances in which a **changed** environment
  occupies a member slot (`ClusterAssoc` on links from `incoming(V)`); an instance whose **centre** changes
  while no member changes is in no changed member's incoming set, so its slot-0 code, energy rows, and `U` term
  go stale — despite §5's own "(a centre particle change moves the row)". Define the affected set as the
  deduplicated union of the member-side route and `centres(position(V))` for each changed `V`, and add a
  centre-only-change test.

All three are internal contradictions inside the frozen §2–§6 conventions: each contradicts another part of the
same design (F-1 vs the §3 preamble and §7; F-2 vs the §5 `Σ_stable` decomposition; F-3 vs §5's centre-row
note), each produces silently wrong energies in reachable package shapes, and the §9/test matrix as listed
would not expose any of them.

**Minor (5):**

- **F-4** — Step 17 §1 sketches the `ctx` + `EnergyEngine<'a>` double mutable borrow that §1 forbids (Step 16
  was corrected for F-19; Step 17 was not), and §1's "hosting loop context" alternative has no named context
  accessor, so the hosting form is not implementable as written. Reconcile at Step 17's design.
- **F-5** — The MMC per-side delta for one-directional links is exact but differs from the original's
  both-or-nothing exchange rule and is absent from the D-1–D-6 deviations list; record it with its test.
- **F-6** — §2 cites `05-open-decisions.md` and Step 14 §23 for the `charge_transport_vector` residual risk;
  neither contains it (the substance is in Step 13 §3.5/§12 and §2 itself). Add the registry row or fix the
  citations.
- **F-7** — The S0/S1/S2 midpoint formulas' "frozen plan content" homes (AGENTS.md §10, ADR 0012,
  `energies.md`) do not contain the S1 midpoint rule; name the original's default plus this freeze instead.
- **F-8** — Step 14 §7.2's "Pair tables are converted to kT once at initialization" reads as an in-place
  conversion; the code and this design convert per read with the model tables in eV. Touch up the merged page's
  wording on the open maintenance branch.

**Note (1):**

- **F-9** — Step 18 §1's "transfer/accumulate the mobile tracker according to the rule's `tracker_order_code`"
  predates the contract's definition of `tracker_order` as the global-tracker index; reconcile at Step 18's
  design review.

Bookkeeping observations routed to the pre-audit gate per `design-proportionality.md` §2 (not findings): the
status dashboard's Step 15 design-review column still reads "Not requested" while the step page records the
review as requested; the stale wording belongs to the start-gate dashboard update.

#### Verdict

**Changes required.** Three Major design findings (F-1, F-2, F-3) remain in the frozen §3/§4/§5 formulas, so the
Full stop rule ("stop when no Blocker or Major remains") is not satisfied; the design may not proceed to the
start gate as frozen. The five Minor findings and one Note are recorded for correction or acceptance and do not
by themselves require a further round. The rest of the design is strong: the API matches the frozen Step 14
contract in full, the conventions are faithfully restated from the plan homes and the original, the static
fail-closed decision is sound and verified against the writer and reader code, the Step 14 audit's F-2
deferral is properly owned by this step's criterion and test row, the complexity class and split decision follow §3.1 exactly, and both required gates pass. The design
agent and dev correct the three enumeration defects (plus the minors at their discretion) on this branch, add
the resolutions, and request the round-2 re-review; the corrected §2–§6 text should keep the frozen-convention
guard (a forced convention change remains a stop-and-report trigger, and an AGENTS.md §10 invariant change
additionally needs an ADR).

### Re-review round 2

#### Request (round 2)

| Field | Value |
| --- | --- |
| Step | 15 — Energy Engine |
| Request owner | Design agent (design re-freeze); the dev takes ownership of the resolutions at the start gate |
| Lead | — (spawns the review agent on request of the design agent/dev) |
| Dev | Not assigned — created from the reviewed `main` commit after the start-gate approval |
| Review initiated by | Lead, on request of the design agent |
| Design class | Full |
| Reviewed commit | `af9ded889825a8b1932329a657eb7460208178f5` (design re-freeze) |
| Fixed point | `04f9a0a` (round-1 record HEAD). The design delta under review is `git diff 51f6636..af9ded8`; `51f6636` is the `origin/main` merge and is not part of the Step 15 design. |
| Record request commit | The documentation-only record commit that appends this request; its parent is the reviewed commit `af9ded8` |
| Docs branch | `docs/step-15-design`; `origin/main` = `be930c4` merged at `51f6636` (a merge commit, not a rebase) |
| Review date | — (pending round 2) |
| Review agent | — (fresh review agent, spawned by the lead) |

Step page: [`Step 15 — Energy Engine`](../steps/step-15-energy-engine.md)

**Freeze note.** The design re-freeze is `af9ded8`, a documentation-only commit on `docs/step-15-design`. It
contains the Step 15 page corrections (F-1–F-8), the `solver-architecture.md` engine-design paragraph, the
roadmap precondition note, and the `05-open-decisions.md` §3/§4 updates. No code, test, configuration, contract
artifact, or generated file is part of the freeze. The reviewed commit's parent is the `main` merge `51f6636`;
`main` itself is `be930c4` (2026-11-27 process adoption included).

**Precondition (verified 2026-11-27).** The Step 14 audit deferral branch `fix/step-14-structure-and-tests` is
**already merged into `main`**. `b632565` ("fix: close the deferred Step 14 audit findings") is a merge commit
on `main` with parents `536c8b3` and `4eb56c9`; the merged tree has `context/mod.rs` at 770 lines,
`context/limits.rs` at 737 lines, and the split files `context/build.rs`, `context/invariants.rs`, and
`context/limits/tests.rs`. The start gate's precondition is satisfied; it is no longer an open question.

**Scope (round 2).** Re-review the round-1 responses in the design documents themselves:

- F-1 — the §3 S2 receiver enumeration now walks every path node's own links (spectator receivers included) and
  is unfiltered by structural stability; D-1/D-2, the hand system, test row, acceptance criterion, and risk row
  are updated.
- F-2 — the §5 `ΔU` now weights every unchanged owner's per-link `δ_{(O,V)}` by `σ_O`; the unstable-link-owner
  hand system, test row, acceptance criterion, property-test coverage, and risk row are added.
- F-3 — the §4/§6 affected-instance set is the deduplicated union of the member-side `ClusterAssoc` route and
  `centres(position(V)) × cell(V)`; the rebuild and `ΔU` are per `(instance, cell)`; the centre-only hand system,
  test row, acceptance criterion, and risk row are added.
- F-4 — the §1 composition rule removes the unimplementable "hosting loop context" alternative and qualifies
  Step 17's sketch; Step 17's own edit is deferred to its design review.
- F-5 — the one-directional MMC exchange is recorded as deviation D-7 with its test.
- F-6 — the transport-vector residual risk is in `05-open-decisions.md` §4 and the citations point to Step 13
  §3.5/§12.
- F-7 — the S1 midpoint rule's first normative home is the Step 15 page; the citation is corrected.
- F-8 — the per-read eV→kT rule is stated unambiguously; the completed Step 14 page's wording is deferred to a
  documentation maintenance edit.
- F-9 — deferred to Step 18's design review; Step 15 §6's `tracker_order` statement is already correct.

The review should confirm that the three Major findings are closed, that the corrections stay inside the frozen
§2–§6 conventions, and that the Minor/Note dispositions are reasonable. It should not re-review the parts of the
design that round 1 already verified unless a correction changed them.

**Non-goals (round 2).** Do not re-open the frozen API shape, the energy conventions, the field/transport
definition, the static-S2 fail-closed deferral, the complexity class and split decision, the feature list, or the
Step 14/16/17/18 designs. No code exists for Step 15. Do not review prose, file layout, status/parity bookkeeping,
or the completed Step 14 page; those are pre-audit-gate or later-step items. The Step 17 composition and Step 18
tracker wording are explicit deferrals to their own design reviews, not findings against this freeze.

#### Design-time checks (Spec and Standards)

Use the axes from [`docs/development/review-axes.md`](../../development/review-axes.md). Label every round-2
finding `Spec` or `Standards`. A design review reviews the Spec axis and the five design questions; the
Standards pass is limited to design-surface standards (contracts, interfaces, ADR consistency), not mechanical
layout or prose.

**Spec axis**

- [ ] **F-1.** The §3 receiver enumeration walks every path node's own forward links, changed or spectator; the
      receiver change filter is gone and the partner-change filter is only a zero-delta skip. The text states
      that the walk is unfiltered by structural stability and why the jump-status receiver set is not the source.
- [ ] **F-2.** The §5 formula weights every unchanged owner's per-link delta `δ_{(O,V)}` by `σ_O`, and the text
      ties this to the `Σ_stable` pair-row definition. An unstable unchanged owner's delta is zero.
- [ ] **F-3.** The §4/§6 affected set is the deduplicated union of the member-side `ClusterAssoc` route and
      `centres(position(V)) × cell(V)` for every changed `V`; the rebuild and `ΔU` are per `(instance, cell)`;
      the centre-only case is reachable and is not double-counted.
- [ ] **F-1/F-2/F-3 coverage.** The §9 hand systems, §Tests matrix, acceptance criteria, and risks each contain
      the spectator-receiver, unstable-link-owner, and centre-only-change cases, so the matrix would expose the
      three original defects.
- [ ] **F-4.** The §1 composition rule names the per-call `EnergyEngine::new(&mut *self.ctx)` form as the only
      supported composition, removes the accessorless hosting alternative, and explicitly defers the Step 17
      struct fix to Step 17's design review.
- [ ] **F-5.** Deviation D-7 records the independent per-side one-directional MMC exchange against the
      original's both-or-nothing rule, and the test matrix names the asymmetric one-directional case.
- [ ] **F-6.** The transport-vector residual risk is tracked in `05-open-decisions.md` §4 and the Step 15 §2
      citations name Step 13 §3.5/§12 (not the absent Step 14 §23 item).
- [ ] **F-7.** The S0/S2 sums, the 0.5 pair convention, and the cluster-at-centre rule are distinguished from the
      S1 midpoint rule; the latter's first normative home is this page, restated from the original's default.
- [ ] **F-8/F-9.** F-8 states the per-read conversion unambiguously and records the Step 14 wording deferral with
      an owner; F-9 is deferred to Step 18's design review with a stated reason. Neither is silently dropped.
- [ ] **Precondition.** The request records that `fix/step-14-structure-and-tests` is merged into `main`
      (`b632565`) and shows the split layout; the step page, roadmap note, and open-decisions row agree.
- [ ] **No new Major.** No Blocker or Major remains in the corrected §1–§6 design; any new Major is reported and
      stops the start gate.

**Standards axis**

- [ ] The re-freeze touches only design and planning documents; no code, test, configuration, contract artifact,
      generated file, ADR text, or historical record is edited.
- [ ] The corrections are inside the frozen §2–§6 conventions and the §1 API: no convention, formula semantics,
      invariant, public type, or feature boundary changes; the §0.2 guard still holds.
- [ ] The design class remains `Full` and the complexity class remains `High`; the split decision,
      owner-acceptance request, and fallback are intact and consistent with `04-roadmap.md` §3.1 and
      `05-open-decisions.md` §3.
- [ ] Round 1's review text, evidence, verdict, and checklist item text are unaltered; round 1 has no backfilled
      `Axis` column; the resolutions are in the separate `Resolution status` section; this round-2 request uses
      the `Spec`/`Standards` vocabulary and design-time phrasing.
- [ ] The step page's implementation status, the review record, and the design pages agree on the round-2 state
      and the precondition; `status.md` is deliberately untouched because step status is the dev's scope (the
      known dashboard drift is reported to the lead, not fixed here).
- [ ] `./tools/verify.sh docs --require` and `./tools/verify.sh policy --require` pass at the request commit.

#### Local gate baseline (request owner)

Commands and results (run on the request tree — freeze `af9ded8` plus this record's request edits; the review
agent re-runs independently):

- `./tools/verify.sh docs --require` → **exit 0**. Documentation math conventions OK (234 files scanned);
  documentation links OK (238 files scanned); Zensical (strict) "No issues found"; DocFX 0 warnings/0 errors;
  rustdoc generated for all seven crates; `Documentation built in …/site`; "Closeout gate completed with
  required checks enabled."
- `./tools/verify.sh policy --require` → **exit 0**. "Public API is compatible with the checked-in baseline"
  (ApiCompat); contract artifacts current (26 files); model/parameterization/lattice projection checks OK
  (5/4/2 fixtures); space-group catalog current (274 settings, sha256 `1f3bf67d…`); "Process consistency: OK";
  68 tool tests OK; "SPDX headers present in 1210 source files"; "No secret patterns found in 1789 tracked text
  files"; "advisories ok, bans ok, licenses ok, sources ok".
