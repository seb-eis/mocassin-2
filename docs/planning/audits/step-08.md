# Step 08 Audit Request

## Request

| Field | Value |
| --- | --- |
| Step | 08 — Model Compilation Pipeline |
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
| Depends on | 02–07 |
| Delivers | A pure, staged compiler that turns the validated model into a P1-extended, symmetry-reduced, densely indexed `CompiledModel` with environments, pair/cluster interactions, transition rules, and simulation encoding. |

Step page: [`Step 08 — Model Compilation Pipeline`](../steps/step-08-model-compilation-pipeline.md)

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
