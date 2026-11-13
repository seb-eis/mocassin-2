# Step 08 Audit Request

> Reviewer: write the evidence, findings, and verdict directly into
> `docs/planning/audits/step-08.md`. You may edit only your own metadata (`Auditor`, `Audit date`,
> `Record state`), the checklist completion marks and inline verification notes (not item text), the
> `Evidence`, `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message
> and `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 08 — Model Compilation Pipeline |
| Request owner | Step agent |
| Orchestrator (step agent) | Step 08 implementor |
| Audit initiated by | Human owner |
| Audited commit | `ebc0e46a052d4fb9d12dd0111f905be13ec51849` |
| Step branch | `step/08-model-compilation-pipeline` |
| Step tag | — |
| Closeout CI run | [`43520011439`](https://github.com/seb-eis/mocassin-2/actions/runs/43520011439) — green at `ebc0e46a052d4fb9d12dd0111f905be13ec51849` |
| Closeout CI head SHA | `ebc0e46a052d4fb9d12dd0111f905be13ec51849` |
| Main SHA at freeze | `884ffe78f8b8f2a9fa6093afcce94856a6094c73` |
| Merge-tree CI | Not required; `main` was merged into the step branch before the freeze |
| Closeout evidence | `docs/planning/closeout/step-08.json` |
| Platform CI | Not run (Ubuntu-only policy; no platform-specific code) |
| Audit date | — |
| Auditor | — |

Step page: [`Step 08 — Model Compilation Pipeline`](../steps/step-08-model-compilation-pipeline.md)

> Orchestration: the step agent (the orchestrator) freezes the step branch, requests the auditor, applies fixes
> autonomously on a new feature branch, reruns closeout CI, adds resolutions without altering reviewer text,
> and requests re-audit until the auditor no longer identifies issues. The human owner approves the final
> verdict, merge, and tag.

## Step summary

| Field | Value |
| --- | --- |
| Phase | C. Compilation |
| Depends on | Steps 02–07 |
| Delivers | A pure, staged compiler from a validated `Project` to a deterministic, densely indexed `CompiledModel` with structure, environments, pair/cluster tables, transition rules, simulation encoding, signatures, caches, diagnostics, and JSON inspection. |

## Step-specific acceptance checklist

- [ ] Ceria compiles to the expected P1 positions, pair sets/tables, cluster sets, and oxygen migration rules.
- [ ] A 2-site MMC model compiles forward and inverse exchange rules.
- [ ] A 3-site migration compiles forward and inverse rules, including a chiral case, and keeps the KMC dual-DOF default.
- [ ] A 5-site vehicle compiles unidirectional rules only, with no automatic inverse; as a KMC simulation it keeps the dual-DOF default unless a documented Step 10 optimizer rule clears it.
- [ ] Rule generation enforces unstable positions void in S0/S2, stable positions void in S1, and the documented mass/charge conservation rules; rejected counts are reported.
- [ ] Charge transport vectors give `qΔx` for a single charge, the correct vacancy sign, zero for an identical-ion exchange, and non-zero for a distinct-charge exchange or a non-collinear multi-site path.
- [ ] Rule ambiguity is detected as `CMP004` with both conflicting transitions/rules.
- [ ] Every compiled direction/rule is reachable through `jump_count`/`jump_mapping`; every selectable `(position, particle, relative slot)` has at most one rule for its `state0`.
- [ ] Tracker definitions/mappings are emitted for KMC and absent for MMC; a 3-site migration's `tracker_order` follows particle identity.
- [ ] `CompiledModel.ModelSignature` is stable across recompilation, excludes runtime settings and parameterization values, and changes exactly when compiled geometry/rules/encoding changes.
- [ ] Compiling the same project twice yields byte-identical `CompiledModelJson` and signature on Ubuntu, Windows, and macOS.
- [ ] Stage caches reuse unaffected stages after a single-domain edit and never change the output.
- [ ] Ceria compiles in `< 1 s`; a 10k-position model compiles in `< 30 s` with `< 2 GiB` managed memory.
- [ ] Cancellation returns no partial model and leaves the project unchanged.
- [ ] Every `CMP` code has a catalog descriptor, documentation anchor, and emission test.
- [ ] Documentation written.

## Standard audit checklist

- [ ] Required tests from the step page pass.
- [ ] Public/protected XML documentation policy is followed.
- [ ] Documentation, status dashboard, feature log, and parity matrix are current.
- [ ] Closeout CI is green at the audited commit.
- [ ] Closeout evidence matches the audited commit and the merge candidate.
- [ ] If `main` moved after the freeze, a merge-tree CI run or a re-audit is recorded.
- [ ] Windows/macOS runs are recorded, or their absence is accepted because the step has no platform-specific code.
- [ ] Format/version/fixture changes are documented and tested.
- [ ] Security, dependency, licence, and secret-scan gates pass.
- [ ] Code structure policy passes; no new root-level or catch-all folders.
- [ ] No unrelated changes or unapproved scope.
- [ ] Branch, tag, and status hygiene are correct.

## Evidence

Commands run:

```text

```

## Known notes for the auditor

- The `vehicle5` and `interstitialcy5` samples originally repeated the same donor/acceptor direction on both
  sides of the complex chain. They were corrected to alternating groups; `interstitialcy5` uses an explicit
  spectator self state change at its interior stable centre. `TRN002` now permits a self state change only at
  an interior stable position with unstable neighbours.
- The charge transport vector is computed from charge-weighted endpoint moments, equivalent to the
  particle-identity sum for balanced counts and covering charge-state changes. Movement follows species flow in
  path order, so a chemically unchanged spectator site still moves physically.
- No `vehicle6` sample or golden exists; the acceptance target is the 5-site vehicle. Windows/macOS golden runs
  were not performed (manual platform policy).
- Performance is guarded by a local ceria `< 2 s` test; no 10k-position or benchmark-project evidence is
  recorded in this implementation.
- Closeout CI run `43519214591` covered the earlier `ec10960` commit; run `43520011439` is the replacement
  closeout run for the audited commit.

## Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| — | — | — |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The auditor writes the findings and verdict
directly. The step agent adds resolutions in the separate `Resolution status` section without altering the
auditor's findings, evidence, or verdict.

## Record state

**State:** Ready for audit

`Re-audit required` is a record state, not a verdict.

## Reviewer independence

> I am a fresh context. I did not author or implement the reviewed work and have no shared conversation with the
> planner or implementor.

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

The step agent completes this section before requesting human approval. It must cover the full history, not
only the latest round.

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
