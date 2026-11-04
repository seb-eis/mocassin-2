# Step 15 Audit Request

## Request

| Field | Value |
| --- | --- |
| Step | 15 — Energy Engine |
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
| Depends on | 14 |
| Delivers | The transition energy calculator: S0/S1/S2, pair/cluster deltas, electric-field terms, state application, and lattice-energy bookkeeping. |

Step page: [`Step 15 — Energy Engine`](../steps/step-15-energy-engine.md)

## Step-specific acceptance checklist

- [ ] Hand-computed pair, cluster, and site-energy systems match exactly.
- [ ] A 3-site migration produces the expected S0/S1/S2.
- [ ] Dynamic and static S2 agree for every rule in the sample models.
- [ ] Field terms have the correct sign and magnitude for a known charge displacement.
- [ ] Incremental lattice energy equals a full recomputation after every accepted step (debug).
- [ ] Fast-exp is off by default and bounded when on.
- [ ] No allocations in the energy hot path.
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
