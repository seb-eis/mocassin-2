# Step NN Audit Request

> Auditor handoff: the auditor may complete this template in
> `.tmp/audit/step-NN-rN-<short-sha>-audit.md` instead of editing the tracked request. The step agent or
> scribe transcribes the findings and verdict verbatim into `step-NN.md`.

## Request

| Field | Value |
| --- | --- |
| Step | NN — Step title |
| Request owner | Step agent |
| Audit status | Not requested |
| Audited commit | — |
| Step branch | — |
| Step tag | — |
| CI run | — |
| Audit date | — |
| Auditor | — |

Step page: [`Step NN — Step title`](../steps/step-NN-short-title.md)

## Step summary

| Field | Value |
| --- | --- |
| Phase | — |
| Depends on | — |
| Delivers | — |

## Step-specific acceptance checklist

Copy the acceptance criteria from the step page, preserving their wording:

- [ ] Acceptance criterion 1
- [ ] Acceptance criterion 2

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

## Known notes for the auditor

- Record known limitations, accepted deviations, or areas needing extra attention.

## Findings

| Severity | Finding | Evidence | Resolution |
| --- | --- | --- | --- |
| — | — | — | — |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The auditor fills this section in the handoff
report; the step agent or scribe copies it verbatim into the durable request and adds resolutions.

## Verdict

- [ ] Passed
- [ ] Passed with notes
- [ ] Fixes required
- [ ] Re-audit required
- [ ] Waived

Follow-up owner:
Follow-up issue/step:
