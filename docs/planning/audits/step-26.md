# Step 26 Audit Request

## Request

| Field | Value |
| --- | --- |
| Step | 26 — Parameterization, Job Templates, and Build UI |
| Request owner | Step agent |
| Audit status | Not requested |
| Audited commit | — |
| Step branch | — |
| Step tag | — |
| CI run | — |
| Audit date | — |
| Auditor | — |

## Step summary

| Field | Value |
| --- | --- |
| Phase | F. GUI |
| Depends on | 25, 09–12 |
| Delivers | The numerical parameterization workspace, job-template editor, build-template selection, package build/deploy UI, progress/cancellation, and result summary. |

Step page: [`Step 26 — Parameterization, Job Templates, and Build UI`](../steps/step-26-parameterization-job-templates-and-build-ui.md)

## Step-specific acceptance checklist

- [ ] A parameterization can be created from the ceria model, edited, and applied.
- [ ] Stale signatures are visible and rebase produces a report.
- [ ] Chiral pair values mirror and conflicts are flagged.
- [ ] Bulk fill/import/export work and validate.
- [ ] A KMC job template with a doping sweep previews correct job counts and charge balance.
- [ ] Building from the GUI produces the same model/lattice/job payload hashes as the CLI for the same inputs (SQLite page layout may differ).
- [ ] Progress/cancel/error paths behave correctly.
- [ ] The build summary matches `mocassin inspect`.
- [ ] Undo/redo works across template edits.
- [ ] Documentation written

## Standard audit checklist

- [ ] Required tests from the step page pass.
- [ ] Public/protected XML documentation policy is followed.
- [ ] Documentation, status dashboard, feature log, and parity matrix are current.
- [ ] CI is green at the audited commit.
- [ ] Format/version/fixture changes are documented and tested.
- [ ] Security, dependency, licence, and secret-scan gates pass.
- [ ] No unrelated changes or unapproved scope.
- [ ] Branch, tag, and status hygiene are correct.

## Evidence

Commands run:

```text

```

## Findings

| Severity | Finding | Evidence | Resolution |
| --- | --- | --- | --- |
| — | — | — | — |

## Verdict

- [ ] Passed
- [ ] Passed with notes
- [ ] Fixes required
- [ ] Re-audit required
- [ ] Waived

Follow-up owner:
Follow-up issue/step:
