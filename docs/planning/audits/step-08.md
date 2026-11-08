# Step 08 Audit Request

> Reviewer: write the evidence, findings, and verdict directly into
> `docs/planning/audits/step-08.md`. You may edit only your own metadata (`Auditor`, `Audit date`, `Audit
> status`), the checklist completion marks and inline verification notes (not item text), the `Evidence`,
> `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message and
> `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 08 — Model Compilation Pipeline |
| Request owner | Step agent |
| Audit initiated by | Planner / implementor / human owner |
| Audit status | Not requested |
| Audited commit | — |
| Step branch | — |
| Step tag | — |
| Closeout CI run | — |
| Platform CI | Not run (Ubuntu-only policy) |
| Audit date | — |
| Auditor | — |

Step page: [`Step 08 — Model Compilation Pipeline`](../steps/step-08-model-compilation-pipeline.md)

## Step summary

| Field | Value |
| --- | --- |
| Phase | C. Compilation |
| Depends on | 02–07 |
| Delivers | A pure, staged compiler that turns the validated model into a P1-extended, symmetry-reduced, densely indexed `CompiledModel` with environments, pair/cluster interactions, transition rules, and simulation encoding. |

## Step-specific acceptance checklist

- [ ] The ceria model compiles to the expected P1 positions, pair sets, cluster sets, and oxygen migration rules.
- [ ] A 2-site MMC model compiles forward and inverse rules.
- [ ] A 3-site migration compiles forward and inverse (including chiral) rules.
- [ ] A 5-site vehicle compiles unidirectional rules only.
- [ ] Rule generation enforces unstable positions void in S0/S2 and stable positions void in S1.
- [ ] Mass and charge conservation reject invalid combinations and report counts.
- [ ] Rule ambiguity is detected and reported.
- [ ] Compilation is deterministic: same project → byte-identical canonical compiled model.
- [ ] Stage caches invalidate correctly after edits.
- [ ] Ceria compilation is < 1 s; a 10k-position model is < 30 s.
- [ ] Documentation written

## Standard audit checklist

- [ ] Required tests from the step page pass.
- [ ] Public/protected XML documentation policy is followed.
- [ ] Documentation, status dashboard, feature log, and parity matrix are current.
- [ ] Closeout CI is green at the audited commit.
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

## Known notes for the auditor

- Record known limitations, accepted deviations, or areas needing extra attention.

## Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| — | — | — |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The auditor writes the findings and verdict
directly. The step agent adds resolutions in the separate `Resolution status` section without altering the
auditor's findings, evidence, or verdict.

## Verdict

- [ ] Passed
- [ ] Passed with notes
- [ ] Fixes required
- [ ] Re-audit required
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
