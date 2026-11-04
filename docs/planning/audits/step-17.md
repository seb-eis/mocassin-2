# Step 17 Audit Request

## Request

| Field | Value |
| --- | --- |
| Step | 17 — Metropolis Monte Carlo Engine |
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
| Delivers | A Metropolis MMC engine with two-site exchanges, equilibrium break detection, time-independent state evolution, and shared outcome/statistics infrastructure. |

Step page: [`Step 17 — Metropolis Monte Carlo Engine`](../steps/step-17-mmc-engine.md)

## Step-specific acceptance checklist

- [ ] A two-state system converges to the expected Boltzmann ratio.
- [ ] A small Ising-like lattice reaches the known ground state at low temperature.
- [ ] Detailed balance holds for random pair exchanges.
- [ ] The break check stops a converging system at the configured tolerance.
- [ ] A zero/near-zero lattice energy does not cause an immediate false equilibrium; the guard logs a warning.
- [ ] MMC state files contain lattice + counters only; no time/trackers.
- [ ] Same seed produces identical trajectories.
- [ ] No allocations in the cycle loop.
- [ ] Acceptance rate and energy trace are physically sensible across temperatures.
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
