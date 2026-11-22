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
| Review date | — |
| Review agent | — |

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

- [ ] **Design class and complexity class.** `Full` (new public Rust API consumed by Steps 16–18; irreversible
      scientific conventions) against the triggers in
      [`design-proportionality.md`](../../development/design-proportionality.md) §1; **High** with the split
      decision and owner-acceptance request per [`04-roadmap.md` §3.1](../04-roadmap.md#31-step-sizing-and-split-triggers)
      (step page §0.2, roadmap note, `05-open-decisions.md` §3 row).
- [ ] **Engine API (step page §1).** `EnergyEngine<'a>` over `&'a mut SimulationContext`; `TransitionEnergies`
      `{s0, s1, s2, barrier_forward, barrier_backward, field_term}` with the with-field barriers and the named
      no-field classification accessors; `ExchangeEnergies`; evaluation read-only and `Result`-returning;
      separate `apply_state_change`/`apply_exchange` with resolve-before-mutate; the single-mutable-borrow
      composition rule (Step 14 F-16/F-19) covering both Step 16's per-call views and a hosting loop context.
- [ ] **Frozen-convention restatement (step page §2).** The S0/S1/S2 midpoint formulas, the 0.5 pair
      convention, the field-term sign and the `charge_transport_vector` definition, and the cluster-centre
      convention match AGENTS.md §10, the data contract §3.3/§3.4, `physics/energies.md`, and Step 14 §7/§11 —
      restated, not re-decided; the units discipline (kT internally, `ev_to_kt` once per model-table read).
- [ ] **Read-only S2 evaluation (step page §3).** The direct-sum evaluation replacing the original's
      backup/apply/restore; per-receiver owner-major deltas over the receiver's **own** link range (all links,
      including duplicate periodic images — deviation D-2, not the precomputed jump-status links);
      override-based cluster codes via `occupation_code`/`cluster_lookup`; the void-member zero-row rule
      (Step 14 D5-2); no mutation, no scratch, no memory-plan change.
- [ ] **Static-correction fail-closed decision (step page §3, D-4).** `None` (the only v1 payload value) is
      dynamic; a present value fails closed (exit 12) because the non-null semantics are undefined in `.msl` v1
      (3-vector payload field; the original's scalar NaN sentinel is reader-rejected here); the optimization is
      deferred to `05-open-decisions.md` §4 with a named follow-up (new projection version per
      step-08-amend-1, compiler-side computation, equivalence tests).
- [ ] **Commit-side deltas and state application (step page §4/§6).** The owner-major incoming-based rule with
      the whole `pc` vector updated; the fixed update order (resolve, particles/flags, tracker ids, pools,
      owner deltas, cluster rebuild, lattice energy); no counters/tracker values (Steps 16–18).
- [ ] **Incremental lattice energy (step page §5).** The exact on-the-fly `ΔU` decomposition with the 0.5
      applied once, stability crossings handled, and the debug recomputation after every accepted step;
      consistent with Step 14 §7.4 / OD-2.
- [ ] **MMC exchange (step page §7).** The two-site energies with the linked/unlinked cross-term split,
      matching Step 17 §3; `a == b` a no-op.
- [ ] **Numerics and the exp kernel (step page §8).** `f64`; computed non-finite results rejected; the
      acceptance-exponential kernel with the ≤ 3.5 % bound and off-by-default fast-exp; Steps 16/17 own the
      draw and comparison.
- [ ] **F-2 ownership (step page acceptance criteria, tests, §9).** The Step 14 audit's deferred owner-major
      delta criterion is stated literally as an acceptance criterion and test row of this step.
- [ ] **Feature list, tests, budgets, risks (step page feature table, §10, Tests, Risks).** Four
      `feature/step-15-energy-engine/<slug>` branches; the hand-computed/property/real-package/allocation/
      benchmark matrix; the zero-allocation and no-new-context-array budgets; realistic risks with
      mitigations.
- [ ] **Documentation plan (step page Documentation).** The named pages exist (the draft's
      `docs/physics/units-constants.md` is corrected to the glossary notation page's Units section);
      implementation-time ownership is explicit.

## Dependency and interface checks

- [ ] Dependencies are `Done` and their contracts are stable — Step 14 (merged/tagged; the context API frozen
      at its feature 3) and the `step-11-amend-2`/`step-11.1` cluster-code contract; the
      `fix/step-14-structure-and-tests` precondition is recorded with its effect on the module layout.
- [ ] Inputs, outputs, types, and formats are defined — the engine consumes the Step 14 accessors and mutators
      plus crate-internal helpers; no public Step 14 API break; no new context arrays or plan entries.
- [ ] Versioning and migration behavior are defined — no `.msl`/`.msr`/`.mcs`/manifest/schema/version/signature
      change; the static-correction payload field is consumed exactly as frozen.
- [ ] No unresolved cross-step interface question remains — Step 16 (per-call engine views, barriers and
      classification accessors, the exp kernel), Step 17 (the exchange entry points), and Step 18 (the
      tracker-value boundary) are each stated; the KMC/MMC loop, counters, and outcome classification are
      explicit non-goals.

## Standard design-review checklist

Use this full checklist for a `Full` step.

- [ ] Design class (`Full`) is recorded and matches the triggers
- [ ] Scope, non-goals, and acceptance criteria are explicit and testable
- [ ] Complexity class and split decision are recorded and justified per
      [`04-roadmap.md` §3.1](../04-roadmap.md#31-step-sizing-and-split-triggers)
- [ ] Design decisions are complete and internally consistent
- [ ] Required tests, property tests, golden fixtures, and performance targets are defined
- [ ] Documentation, status, parity, and audit updates are planned
- [ ] Licensing, provenance, and dependency checks are resolved
- [ ] Risks and mitigations are realistic
- [ ] Code layout and namespace plan follow the structure policy
- [ ] No hidden scope, stale placeholders, or unowned decisions

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

Commands run:

## Findings

| Severity | Origin | Finding | Evidence |
| --- | --- | --- | --- |
| — | — | — | — |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. Origin: **design**, **implementation**,
**evidence**, or **documentation**. The review agent writes the findings and verdict directly. The dev adds
resolutions in the separate `Resolution status` section without altering the review agent's findings, evidence,
or verdict.

## Record state

**State:** Ready for review

`Re-review required` is a record state, not a verdict.

## Reviewer independence

> I am a fresh context. I did not author or implement the reviewed work and have no shared conversation with
> the design agent, dev, or lead.

## Verdict

- [ ] Ready for implementation
- [ ] Ready with notes
- [ ] Changes required
- [ ] Waived

Follow-up owner:
Follow-up step:

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
| — | — | — | — |

The dev fills this section after the verdict. Do not alter the review agent-authored findings, evidence, or
verdict.

## Review rounds

Append each new round below. Never overwrite an earlier round.

### Review round 1

#### Independence and frozen state

#### Commands and results

#### Findings

#### Verdict
