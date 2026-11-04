# Step 21 Audit Request

## Request

| Field | Value |
| --- | --- |
| Step | 21 — Analysis, Plots, and Reports |
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
| Phase | E. Results |
| Depends on | 20 |
| Delivers | Aggregation across job sets, Arrhenius and transport fits, plot-ready series, headless PNG/SVG/CSV/JSON/Parquet exports, and reproducible analysis reports. |

Step page: [`Step 21 — Analysis, Plots, and Reports`](../steps/step-21-analysis-plots-and-reports.md)

## Step-specific acceptance checklist

- [ ] Arrhenius analysis of a synthetic dataset recovers the input activation energy within 1e-6 eV.
- [ ] Grouping by collection/config/temperature is deterministic.
- [ ] Plots render headlessly on all three OSes with labels/units.
- [ ] A report contains manifest, tables, figures, and data with matching values.
- [ ] Same package/command produces identical CSV/JSON.
- [ ] Large job sets stream within memory budget.
- [ ] Zero-field/empty groups are excluded with a reported reason.
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
