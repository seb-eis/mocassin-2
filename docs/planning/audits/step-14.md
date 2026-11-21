# Step 14 Audit Request

> Reviewer: write the evidence, findings, and verdict directly into
> `docs/planning/audits/step-14.md`. You may edit only your own metadata (`Auditor`, `Audit date`, `Record state`), the checklist completion marks and inline verification notes (not item text), the `Evidence`,
> `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message and
> `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 14 — Simulation Context Initialization |
| Request owner | Step agent (implementor) |
| Orchestrator | — (relayed by the orchestrator; do not edit) |
| Step agent (implementor) | Step 14 step agent |
| Audit initiated by | Orchestrator, on request of the step agent |
| Step branch | `step/14-simulation-context-initialization` |
| Closeout evidence | [`docs/planning/closeout/step-14.json`](../closeout/step-14.json) |
| Audit date | — |
| Auditor | — |

The closeout evidence file holds the audited commit, closeout CI run and `head_sha`, `main` SHA at freeze, merge candidate,
merge-tree CI, platform CI, and tag. Do not restate them here: one source, one place.

Step page: [`Step 14 — Simulation Context Initialization`](../steps/step-14-simulation-context-initialization.md)

> Orchestration: the orchestrator spawns a fresh auditor when the step agent has frozen the step branch and
> completed the audit request, and relays the verdict and owner decisions. The step agent applies fixes on a new
> feature branch, reruns closeout CI, adds resolutions without altering reviewer text, and asks the orchestrator
> for re-audit until the auditor no longer identifies issues. The orchestrator does not fix findings or edit the
> audit record. The human owner approves the final verdict, merge, and tag.

## Step summary

| Field | Value |
| --- | --- |
| Phase | D. Solver |
| Depends on | 13 and `step-11.1` (the `step-11-amend-2` reader/runtime cluster-code lookup) |
| Delivers | A deterministic, allocation-bounded `SimulationContext` built once per job from the validated `JobPackage`: lattice and environment arenas, an owner-major neighbour-link CSR with an inverse routing index, the cluster runtime, the energy background and environment energy vectors, the supercell selection pools, the receiver-major KMC jump status, the mobile/static tracker index space, counters, goals, normalization, the time step, and the resume hook. No energy deltas, no KMC/MMC cycle, no tracker values, and no `.mcs` output. |

## Step-specific acceptance checklist

Copy the acceptance criteria from the step page, preserving their wording:

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

## Standard audit checklist

- [ ] Required tests from the step page pass.
- [ ] Public/protected XML documentation policy is followed.
- [ ] Documentation, status dashboard, feature log, and parity matrix are current.
- [ ] Closeout CI is green at the audited commit.
- [ ] Closeout evidence matches the audited commit and the merge candidate.
- [ ] If `main` moved after the freeze, a merge-tree CI run or a re-audit is recorded.
- [ ] Windows/macOS runs are recorded, or their absence is accepted because the step has no platform-specific
      code.
- [ ] Format/version/fixture changes are documented and tested.
- [ ] Security, dependency, licence, and secret-scan gates pass.
- [ ] Code structure policy passes; no new root-level or catch-all folders.
- [ ] No unrelated changes or unapproved scope.
- [ ] Branch, tag, and status hygiene are correct.

## Evidence

Commands run:

```text

```

Evidence file: `docs/planning/closeout/step-14.json`

## Known notes for the auditor

- **Multi-centre fixture authoring deviation.** The project model's `ENM008` validator requires 2–8 surrounding
  offsets, so the design's literal one-member cluster cannot be authored. The implemented
  `step-14-multi-centre-cluster` sample adds a third stable site C and uses two **distinct** member sites (B and
  C) at offsets `[0,0,0.05]` and `[0,0,0.1]`; two images of one member site remain the rejected ambiguous OD-1
  case. The centred sample stays byte-identical, and the four-instance/member-site-fallback acceptance is
  unchanged. Recorded in the step page §19 and the fixture READMEs.
- **The 10×10×10 ceria fixture is table-free.** The real-writer fixture has no pair or cluster tables, so it
  measures the table-free 10×10×10 path. The `step-13-populated` model cloned onto a hand-built 10×10×10
  `InitialLattice` is the pair/cluster-scale measurement; both are recorded in the benchmark evidence.
- **Benchmark class.** The context benchmark was run on the local container class
  `linux-x64-local-container-amd-ryzen-7-7840u-rust-1.99.1` (AMD Ryzen 7 7840U, Debian 13, Rust 1.99.1,
  Criterion 0.8.2, release). The 10 % rule applies only within that class; Step 29 owns the automated
  same-class regression job. Absolute timings are not compared across CPUs/OS/Rust versions.
- **Void-member rule for Step 15.** A cluster cell with a void centre/member has no canonical occupation row and
  contributes zero (the empty cluster). Step 15 must use the zero energy row/absent `cluster_code_id`, not
  `cluster_lookup`; the non-canonical non-void `code_expansions` fail-closed behavior is unchanged.
- **Platform CI.** Not run. Step 14 adds Rust library/test/benchmark code and Markdown/JSON fixtures; it adds no
  P/Invoke, `LibraryImport`, native library, RID-specific package, Windows/macOS path/case/locking behaviour, or
  GUI code. Ubuntu is the only affected platform; the absence is an accepted risk.
- **AOT evidence.** The strict smoke host and the IL2026/IL3050 positive control pass; the real `Mocassin.Cli`
  publish is expected to fail with reflection-based JSON converter diagnostics, recorded as non-gating evidence
  (Step 31 owns the AOT decision), as in Step 13.
- **Main at freeze.** `main` `929327e` (Step 23B design start) was merged into the step branch before the
  audited commit; the closeout CI covers the merged tree. If `main` moves again, the merge-tree rule applies.
- **Feature 6 squash.** `00fceab` on the step branch; the evidence file records the audited/merged commit in
  `docs/planning/closeout/step-14.json`.

## Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| — | — | — |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The auditor writes the findings and verdict
directly. The step agent adds resolutions in the separate `Resolution status` section without altering the
auditor's findings, evidence, or verdict.

## Record state

**State:** In audit

`In audit` means the step branch is frozen at the request and a fresh audit context is pending or reviewing it.

## Reviewer independence

> I am a fresh context. I did not author or implement the reviewed work and have no shared conversation with the
> planner, implementor, or orchestrator.

## Verdict

- [ ] Passed
- [ ] Passed with notes
- [ ] Fixes required
- [ ] Waived

Follow-up owner:
Follow-up issue/step:

## Resolution status

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| — | — | — | — |

The step agent fills this section after the verdict. Do not alter the reviewer-authored findings, evidence, or
verdict.

## Approval summary

The step agent completes this section before a merge, tag, or Blocker/Major waiver request. It must cover the
full history, not only the latest round.

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

- [ ] Accepted: Passed
- [ ] Accepted: Passed with notes
- [ ] Fixes required
- [ ] Waived

Approved by:
Date:

## Audit rounds

Append each new round below. Never overwrite an earlier round.

### Audit round 1

#### Independence and frozen state

#### Commands and results

#### Findings

#### Verdict
