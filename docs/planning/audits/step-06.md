# Step 06 Audit Request

## Request

| Field | Value |
| --- | --- |
| Step | 06 — Lattice and Simulation Models |
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
| Delivers | Occupation exchanges, ordered dopings, and MMC/KMC simulation definitions with defaults and validation. |

Step page: [`Step 06 — Lattice and Simulation Models`](../steps/step-06-lattice-and-simulation-models.md)

## Step-specific acceptance checklist

- [ ] The ceria reduction doping (Ce⁴⁺→Ce³⁺ primary, O²⁻→V_O dependent) validates and previews charge neutrality.
- [ ] An isovalent doping without a dependent exchange validates.
- [ ] A dependent exchange with the wrong charge sign is rejected.
- [ ] Doping priorities are unique and applied in ascending order.
- [ ] A KMC simulation cannot select an MMC transition.
- [ ] All original simulation settings are represented and validated.
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
