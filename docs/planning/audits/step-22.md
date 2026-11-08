# Step 22 Audit Request

> Reviewer: write the evidence, findings, and verdict directly into
> `docs/planning/audits/step-22.md`. You may edit only your own metadata (`Auditor`, `Audit date`, `Audit
> status`), the checklist completion marks and inline verification notes (not item text), the `Evidence`,
> `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message and
> `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 22 — Python Analysis API |
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

Step page: [`Step 22 — Python Analysis API`](../steps/step-22-python-analysis-api.md)

## Step summary

| Field | Value |
| --- | --- |
| Phase | E. Results |
| Depends on | 18, 20 |
| Delivers | A small, read-only Python package for `.mcs`/`.msr` files with basic multi-file evaluations, pytest coverage, and mkdocstrings-generated API docs inside the Zensical site. |

## Step-specific acceptance checklist

- [ ] `read_mcs` reads KMC and MMC fixtures and returns the expected metadata, lattice, counters, trackers, and histograms.
- [ ] `read_msr` reads results/status metadata and extracts `.mcs` blobs with package binding checks.
- [ ] The Python reader matches the .NET reader on the same fixture.
- [ ] Single-file helpers match hand calculations and .NET evaluation values within tolerance, including per-species/total transport and origin-based MSD.
- [ ] Multi-file filtering/grouping/aggregation works and reports excluded files.
- [ ] Zero-field and `z == 0` cases return `None`/`nan`, never `Infinity`/`NaN` silently.
- [ ] Malformed/unsupported files raise typed errors with path and field context.
- [ ] `pytest` passes on Python 3.11–3.13 with coverage ≥ 85 %.
- [ ] `mkdocstrings` renders the API under `/api/python/` in the Zensical site.
- [ ] The wheel/sdist builds and installs in a clean environment.
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
