# Step 18 Audit Request

> Reviewer: write the evidence, findings, and verdict directly into
> `docs/planning/audits/step-18.md`. You may edit only your own metadata (`Auditor`, `Audit date`, `Audit
> status`), the checklist completion marks and inline verification notes (not item text), the `Evidence`,
> `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message and
> `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 18 — Tracking, Statistics, and State Writer |
| Request owner | Step agent |
| Orchestrator (step agent) | — |
| Audit initiated by | Planner / implementor / human owner |
| Audit status | Not requested |
| Audited commit | — |
| Step branch | — |
| Step tag | — |
| Closeout CI run | — |
| Platform CI | Not run (Ubuntu-only policy) |
| Audit date | — |
| Auditor | — |

Step page: [`Step 18 — Tracking, Statistics, and State Writer`](../steps/step-18-tracking-statistics-and-state-writer.md)

> Orchestration: the step agent (the orchestrator) freezes the step branch, requests the auditor, applies fixes
> autonomously on a new feature branch, reruns closeout CI, adds resolutions without altering reviewer text,
> and requests re-audit until the auditor no longer identifies issues. The human owner approves the final
> verdict, merge, and tag.

## Step summary

| Field | Value |
| --- | --- |
| Phase | D. Solver |
| Depends on | 16, 17 |
| Delivers | Global/static/mobile movement tracking, jump histograms, statistics dumps, derived transport quantities, and the versioned MessagePack state writer/reader. |

## Step-specific acceptance checklist

- [ ] A single-particle random walk produces the expected origin-based MSD, D, and tracker displacements.
- [ ] Per-species and total transport values use the tracked ensemble count, not total particles.
- [ ] Global trackers separate transition contributions correctly.
- [ ] Static trackers reproduce per-position flow for a simple lattice.
- [ ] Mobile trackers follow particle identity across a 3-site migration.
- [ ] Histograms bin values correctly, including under/overflow.
- [ ] MessagePack state files round-trip through the reader and pass semantic validation.
- [ ] Resume restores lattice, counters, trackers, histograms, RNG state, `K_fix`/`K_norm`/`RawMax`, goals, and time, and continues identically.
- [ ] Corrupt/truncated state files are rejected.
- [ ] Statistics output contains no NaN/Inf for zero-field MMC/KMC cases.
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
