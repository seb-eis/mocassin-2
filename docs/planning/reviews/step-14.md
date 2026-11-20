# Step 14 Design Review

> Reviewer: write the evidence, findings, and verdict directly into
> `docs/planning/reviews/step-14.md`. You may edit only your own metadata (`Reviewer`, `Review date`, `Record state`), the checklist completion marks and inline verification notes (not item text), the
> `Evidence`, `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message
> and `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 14 — Simulation Context Initialization |
| Request owner | Planner (design freeze); the step agent takes ownership of resolutions at the start gate |
| Orchestrator | Orchestrator agent (spawns the planning auditor) |
| Step agent (implementor) | Not assigned — created from the reviewed `main` commit after the start-gate approval |
| Review initiated by | Orchestrator, on request of the planner |
| Reviewed commit | `7c7ff0de83a4ae4c5f9849650e0601c7238494b8` |
| Docs branch | `docs/step-14-design` (from `main` = `bbc2d083ed96fa6e26526e335304107b20a7fdcb`, `step-13` merged and tagged) |
| Review date | 2026-11-20 |
| Reviewer | — (fresh planning auditor; the orchestrator spawns it) |

Step page: [`Step 14 — Simulation Context Initialization`](../steps/step-14-simulation-context-initialization.md)

**Freeze note.** The design docs are committed as the freeze commit; this request record is a documentation-only
follow-up so the freeze SHA can be written. The design freeze includes the step page, the architecture, data
contract, quality-strategy, roadmap, open-decision, parity, code-structure, solver-architecture, and physics
updates, plus the Step 15 interface reconciliation. No code, test, configuration, contract artifact, or
generated file is part of the freeze.

**No code exists yet.** Step 14 has no branch and no implementation. The design is judged on whether the runtime
layout, the cross-step interfaces, the two gated decisions, the fixtures, the test/budget plan, and the
documentation plan are ready to implement.

> Orchestration: the orchestrator spawns a fresh planning auditor after the planner has frozen the design and
> completed this request, and relays the verdict and owner decisions. The planner and step agent update the
> design docs, apply fixes on the design docs or a maintenance branch, add resolutions without altering reviewer
> text, and ask the orchestrator for re-review until the auditor no longer identifies issues. The orchestrator
> does not edit the design or the review record. The human owner approves the final verdict and the start of
> implementation.

## Step summary

| Field | Value |
| --- | --- |
| Phase | D. Solver |
| Depends on | 13 (`Done`; merged into `main` as `2613a0dcbc3ddbfe4bb5b54f47821d3ffa951789`, tagged `step-13`, accepted 2026-11-20) |
| Delivers | A deterministic, allocation-bounded simulation context built once per job from the validated `JobPackage`: lattice/environment arenas, a translation-compact per-P1 neighbour-link CSR, the cluster instance/code/energy runtime, site+pair environment energy vectors and per-centre cluster energy, the exact initial lattice energy, KMC selection pools, per-`(P1, direction)` jump status, the tracker index space, counters, goals, normalization, the time step, and the resume hook. No energy engine, KMC/MMC loop, trackers, or `.mcs` output. |
| Sizing | Complexity class **High** (cross-step interfaces, behavioural irreversibility, test-matrix breadth). **No split proposed**; owner acceptance as one High unit is requested at the start gate under [`04-roadmap.md` §3.1](../04-roadmap.md#31-step-sizing-and-split-triggers). Six features. API frozen at feature 3; a forced API rework is a stop-and-report re-review trigger. |

## Design decisions reviewed

- [ ] **Context ownership and phasing.** One `SimulationContext` per job from the validated `JobPackage`; no
      I/O, no package re-read, no interior mutability; the twelve-phase build order; `InitOutcome`
      (`Ready`/`AlreadyCompleted`); `Send` but not `Sync`; the frozen accessor surface.
- [ ] **Data layout and bounds.** `[A,B,C,P]` dimensions and the documented linear index; struct-of-arrays
      arenas; `u32`/`u8`/`u64`/`f64` choices; explicit energy stride; the `ContextLimits` table and the
      checked-arithmetic rule that a limit violation is exit 12, not a panic.
- [ ] **Neighbour links.** The translation-compact per-P1 CSR versus the original per-supercell link array;
      `wrap(cell + offset) * P + target_position`; the deterministic sort (`pair_id`, target, offset) as a
      reproducibility contract; the per-link cluster association; the reader-cap consistency.
- [ ] **Cluster runtime and the v1 geometry limitation (OD-1).** `byte_position` = the environment's own slot;
      slot 0 = centre; instance/membership/code representation; code-existence validation; the unique-image rule
      and the fail-closed rejection of zero/multiple images; the permanent fix as a future `.msl` revision.
- [ ] **Cluster table layout correction.** One energy per occupation code with slot 0 = centre particle;
      `particle_table_mapping` as the ascending allowed-centre list, not a `[u8; 64]` row map; the Step 15
      `[center_row][code]` wording replaced.
- [ ] **Energy runtime and initial lattice energy (OD-2).** eV→kT once; the site table separate from the
      site+pair vector; per-instance cluster energy; the exact total (site once, pair ×0.5, cluster once at the
      centre); the documented deviation from the original's uniform 0.5 and its hand-computed test.
- [ ] **Selection pools.** Direction-count buckets ascending; the `selection_particle_mask` condition; optimizer
      removal and the re-add-after-change semantics; O(1) `slot_of`/`pool_of` swap-remove; `selectable_jump_count`
      bounds; the `-1`/`0` collapse because the payload uses `u32`.
- [ ] **KMC jump status.** Status identity `direction * P + position`; `resolve_node` with node 0 replaced by the
      selected bucket (cross-bucket mapping); sender/receiver link collection with `link_id` relative to the
      sender P1 list; the structural receiver filter (deviation from the original's initial-occupation filter);
      the `L*(L-1) ≤ 90` derived bound; cluster touches per direction; the Step 16 exact-occupation data
      interface.
- [ ] **Goals, counters, tracker index space.** `mobile_count` = non-void positions (passive included);
      `prerun`/`main`/`total` and the round-up-to-100 block; the mobile/static index formulas; counters zeroed
      and tracker values deferred to Step 18; the zero-mobile/zero-selectable errors.
- [ ] **Normalization, time step, and `nu_max`.** `K_fix = exp(E_norm/kT)`; `K_auto = 1/RawMax`; the
      non-finite fallback; `dof`; `nu_max` derived from the job cap or the maximum active rule frequency because
      the v1 payload does not persist the original's build-time modulus; `dt` finite/positive.
- [ ] **Transport-formula input contract (Step 13 assignment).** `field_scale = |E|*1e-10/kT_eV` and the
      normalized direction; the stored `charge_transport_vector` used verbatim; no recomputation from
      movements; fractional P1 coordinates unavailable; the residual risk and future-format note.
- [ ] **Resume.** The plain `ResumeState`/`ResumePhase` input; no `mocassin-state` dependency; every validation;
      the re-derived mobile mapping; `RawMax` preserved; `Completed` returns `AlreadyCompleted` and is never
      resumed; exit mapping for a bad RNG state.
- [ ] **Errors, exits, determinism, cancellation.** The `ContextError` → exit 12/13/15 table; no `unwrap` on
      package data; no hash iteration; fixed f64 summation order; no `rayon`; per-phase and every-4096
      cancellation; `MemoryReport`.
- [ ] **Step 15/16/17/18/19 interfaces.** The §15 interface table and whether each consumer has what it needs;
      the Step 16 exact-occupation check ownership; the Step 18 tracker-value boundary; the Step 19 resume/CLI
      boundary; no hidden Step 14 physics.
- [ ] **Complexity class and feature list.** High on cross-step interfaces, irreversibility, and test-matrix
      breadth; one acceptance story; the no-split case; the six feature branches with their scopes and outcomes;
      the API-freeze guard; the roadmap §3.1 record and the owner-acceptance request.
- [ ] **Fixtures, tests, and budgets.** Reuse of `msl-v1`, `amend-1-net10`, `step-13-populated`,
      `step-13-centered`; the new real-writer 10×10×10 fixture and why it is not a schema change; the
      context-summary snapshot; the allocation test binary; the Criterion `context` benchmark and the
      `rustContext` same-class baseline; the `< 100 ms`/`< 20 ms`/memory budgets; the nightly 500k stress.
- [ ] **Documentation, parity, and closeout plan.** `solver-architecture.md`, `physics/energies.md`,
      `performance/index.md`, `troubleshooting/errors.md`, `testing-and-parity.md`, `msl-format.md`; the new
      parity row; the closeout evidence and audit sequence; the note that the status dashboard is
      step-agent-owned.

## Known limitations and gated decisions

- **OD-1 (gated, Step 14 start gate + future format revision).** The v1 cluster link does not persist the
  member's relative cell offset and the payload has no fractional P1 coordinates. Step 14 derives the offset only
  from a unique centre→member pair image and rejects zero or multiple images. A writer-valid cross-image cluster
  is therefore rejected. The permanent fix is a future `.msl` payload field and needs its own decision,
  amendment, fixtures, and version handling. Recorded in the step page §6.4, the data contract §3.3, and
  `05-open-decisions.md` §3.
- **OD-2 (gated, Step 14 start gate).** The original applies its uniform 0.5 factor to the site and cluster
  contributions as well as to pairs. Step 14 initializes the exact total (site once, pair ×0.5, cluster once at
  the centre), consistent with `physics/energies.md`, and Step 15 continues with that decomposition. The
  deviation is recorded with a hand-computed test and assigned to Step 29 parity.
- **API-freeze guard.** The context API is frozen at feature 3; if the real-package acceptance cannot pass
  without changing it, the step agent stops and reports before feature 5.
- **Deferred by design.** No `OPT_LINK_ONLY_MOBILES`, no static S2 correction, no fast-exp, no parallel
  initialization; each is a Step 15/29 optimisation with a measurement, not a Step 14 omission.

## Dependency and interface checks

- [ ] Step 13 is `Done`, merged (`2613a0dcbc3ddbfe4bb5b54f47821d3ffa951789`) and tagged `step-13`, and the
      runtime types, fixture set, exit-code model, and reader contract that Step 14 consumes are stable. Verify
      the `JobPackage`/`ParameterizedModel`/`EnvironmentModel`/`EnergyModel`/`TransitionModel` fields the design
      names, the `DirectionPool`/`JumpStatus` containers, and the real fixtures and their summaries.
- [ ] Steps 11/12 `.msl` v1 schema/payload contract is unchanged by Step 14 (no field, version, manifest,
      signature, or fixture change). Verify the design adds only runtime expansion and clarifications.
- [ ] Step 15 can compute S0/S1/S2, pair/cluster deltas, the field term, state application, and the incremental
      lattice energy from the Step 14 interface, with the corrected cluster accounting; Step 15's page is
      consistent with the Step 14 freeze.
- [ ] Step 16 can select from the pools, resolve the selected direction's path, run the exact per-direction
      occupation precheck, and update the pools without a Step 14 change.
- [ ] Step 17 can run MMC from the same runtime (pools, interaction range, link query, energies) with no pools/
      jump status/time.
- [ ] Step 18 can own tracker values, histograms, and the `.mcs` writer/reader from the tracker index space,
      counters, and `ResumeState`; Step 19 can map `InitOutcome`/`ContextError`.
- [ ] No unresolved cross-step interface question remains. Two decisions are gated on the owner (OD-1, OD-2);
      neither changes a persisted format, and both are recorded with a fallback, a test, and an owner.
- [ ] Versioning and migration behaviour: no format change; `.msl` v1 current-major support unchanged; the
      cluster limitation is documented rather than silently versioned.

## Standard design-review checklist

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

## Evidence

Files reviewed:

Commands run:

## Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| — | — | — |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The planning auditor writes the findings and
verdict directly. The step agent adds resolutions in the separate `Resolution status` section without altering
the auditor's findings, evidence, or verdict.

## Record state

**State:** Ready for review

## Reviewer independence

> I am a fresh context. I did not author or implement the reviewed work and have no shared conversation with the
> planner, implementor, or orchestrator.

## Verdict

- [ ] Ready for implementation
- [ ] Ready with notes
- [ ] Changes required
- [ ] Waived

Follow-up owner:
Follow-up step:

## Approval summary

The request owner (step agent or planner) completes this section before the orchestrator presents the approval
request. It must cover the full history, not only the latest round.

- [ ] Requested decision and frozen commit recorded
- [ ] Every review/audit round recorded (round, date, commit, reviewer, verdict)
- [ ] Every finding recorded (ID, severity, issue, evidence, resolution, fix commit, status)
- [ ] Accepted notes and waivers recorded with rationale
- [ ] Residual risks, deferred items, and follow-up owners recorded
- [ ] Explicit no-Blocker/no-Major statement, or the exact exceptions being waived
- [ ] Final CI and documentation-build results recorded

Round history:

| Round | Date | Commit | Reviewer | Verdict |
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

The step agent fills this section after the verdict. Do not alter the reviewer-authored findings, evidence, or
verdict.

## Review rounds

Append each new round below. Never overwrite an earlier round.

### Review round 1

#### Independence and frozen state

#### Commands and results

#### Findings

#### Verdict
