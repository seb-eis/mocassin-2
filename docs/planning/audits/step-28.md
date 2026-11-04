# Step 28 Audit Request

## Request

| Field | Value |
| --- | --- |
| Step | 28 — Run Monitoring, Analysis UI, and MMCFE |
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
| Depends on | 26, 19–21 |
| Delivers | A run queue that launches and monitors `mocassin-sim`, live progress/statistics/charts, result collection, analysis dashboards, the MMCFE workflow, and the lattice-recycle tool. |

Step page: [`Step 28 — Run Monitoring, Analysis UI, and MMCFE`](../steps/step-28-run-monitoring-and-mmcfe-ui.md)

## Step-specific acceptance checklist

- [ ] A local KMC/MMC job can be launched, monitored, cancelled, resumed, and collected into `.msr` from the GUI.
- [ ] Progress, ETA, energy, and rate update correctly from solver events.
- [ ] Aggregate status is accurate for 100+ jobs.
- [ ] Collection is idempotent and reports invalid states.
- [ ] Arrhenius/jump/MMC dashboards render from collected results.
- [ ] MMCFE can be configured, run, resumed, imported, and plotted.
- [ ] Lattice recycle validates compatibility and builds a new package with the imported initial lattice; it never mutates an existing `.msl`.
- [ ] App exit cleans up child processes; orphans are detected next start.
- [ ] No UI freezes during long runs.
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
