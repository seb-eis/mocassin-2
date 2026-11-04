# Step 05 Audit Request

## Request

| Field | Value |
| --- | --- |
| Step | 05 — Energy Model |
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
| Depends on | 03, 04 |
| Delivers | Stable/unstable interaction environments, cutoff filters, multi-body clusters, and site energies, with validation and cost previews. |

Step page: [`Step 05 — Energy Model`](../steps/step-05-energy-model.md)

## Step-specific acceptance checklist

- [ ] Ceria's stable environment and oxygen migration-edge cluster validate.
- [ ] A filter that removes a specific sublattice pair is applied symmetrically.
- [ ] A cluster with 8 offsets is accepted; 9 offsets are rejected.
- [ ] Duplicate/ring/out-of-range clusters are rejected with paths.
- [ ] The cost preview reports pair and permutation counts consistent with step 08.
- [ ] Chiral pair partners are detected and cannot receive independent energies.
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
