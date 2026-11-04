# Step 07 Audit Request

## Request

| Field | Value |
| --- | --- |
| Step | 07 — Validation, Diagnostics, and Model Statistics |
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
| Phase | B. Model definition |
| Depends on | 03, 04, 05, 06 |
| Delivers | A cross-domain validation pipeline, a stable diagnostic catalog, model statistics, and the headless `mocassin validate` command. |

Step page: [`Step 07 — Validation, Diagnostics, and Model Statistics`](../steps/step-07-validation-and-diagnostics.md)

## Step-specific acceptance checklist

- [ ] The ceria sample validates with zero errors and a documented warning set.
- [ ] Each diagnostic code has a test that triggers it and asserts severity, path, and hint.
- [ ] Validation never mutates the project or throws for user data.
- [ ] `mocassin validate --json` output validates against a JSON schema.
- [ ] Exit codes match the contract.
- [ ] Validation of the ceria sample completes in < 200 ms.
- [ ] The GUI can navigate from every diagnostic to its object (verified in steps 24–25).
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
