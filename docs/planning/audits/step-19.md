# Step 19 Audit Request

> Reviewer: write the evidence, findings, and verdict directly into
> `docs/planning/audits/step-19.md`. You may edit only your own metadata (`Auditor`, `Audit date`, `Audit
> status`), the checklist completion marks and inline verification notes (not item text), the `Evidence`,
> `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message and
> `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 19 — Solver CLI, HPC Workflows, and Extensions |
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

Step page: [`Step 19 — Solver CLI, HPC Workflows, and Extensions`](../steps/step-19-solver-cli-hpc-and-extensions.md)

## Step summary

| Field | Value |
| --- | --- |
| Phase | D. Solver |
| Depends on | 18 |
| Delivers | A production `mocassin-sim` CLI, batch/worker execution, SLURM/PBS-friendly scripts, graceful signals, deterministic seeds, and a versioned extension ABI (including MMCFE parity). |

## Step-specific acceptance checklist

- [ ] `mocassin-sim --db ceria.msl --job 1 --io ./run1` produces `run.mcs`, logs, and `solver.json`.
- [ ] A completed job exits `18` on rerun, not silent success.
- [ ] SIGINT during a run writes a resumable state and exits `15`.
- [ ] `batch --jobs 1-10 --parallel 4` runs all jobs with independent seeds.
- [ ] The SLURM array script runs the ceria sample on a scheduler test harness.
- [ ] A sample extension loads, logs, and modifies a position; an incompatible ABI is rejected.
- [ ] MMCFE runs an annealing schedule and produces a resumable `mmcfe.db` matching the contract.
- [ ] Logs are structured and contain job/model identifiers.
- [ ] No job can write outside its I/O directory (path validation).
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
