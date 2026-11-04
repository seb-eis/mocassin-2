# Step 09 Audit Request

## Request

| Field | Value |
| --- | --- |
| Step | 09 — Parameterization Templates |
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
| Phase | C. Compilation |
| Depends on | 08 |
| Delivers | Versioned parameterization templates that quantify a compiled model, with stable identities, signature compatibility, rebase/migration, and completeness checks. |

Step page: [`Step 09 — Parameterization Templates`](../steps/step-09-parameterization-templates.md)

## Step-specific acceptance checklist

- [ ] Creating a template from the ceria compiled model produces exactly the expected sets/rules.
- [ ] Applying the template to the same model succeeds and yields the same compiled values.
- [ ] A changed model produces a signature mismatch with a useful diff.
- [ ] Rebase copies unchanged values, defaults new ones, and reports removed ones.
- [ ] Chiral pair values are mirrored and cannot diverge.
- [ ] Site energy overrides apply and can be reset.
- [ ] Missing/zero values produce warnings, not silent success.
- [ ] Template JSON is canonical and round-trips.
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
