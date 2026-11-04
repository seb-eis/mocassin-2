# Step 16 Audit Request

## Request

| Field | Value |
| --- | --- |
| Step | 16 — Kinetic Monte Carlo Engine |
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
| Phase | D. Solver |
| Depends on | 15 |
| Delivers | A rejection KMC engine with direction selection, rule lookup, normalization pre-run, time evolution, all cycle outcomes, abort conditions, and deterministic seeding. |

Step page: [`Step 16 — Kinetic Monte Carlo Engine`](../steps/step-16-kmc-engine.md)

## Step-specific acceptance checklist

- [ ] A single-barrier system reproduces the analytic residence time within 1 %.
- [ ] A 3-site migration runs to completion and conserves mass/charge.
- [ ] All six outcomes are exercised by targeted fixtures.
- [ ] Normalization converges and `K_norm` is stable.
- [ ] Pre-run reset zeroes time/counters/trackers but preserves `RawMax`.
- [ ] Abort conditions trigger at the expected boundaries.
- [ ] Resume from `prerun.mcs` continues correctly.
- [ ] Same seed produces identical counters and state across runs.
- [ ] No allocations in the cycle loop.
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
