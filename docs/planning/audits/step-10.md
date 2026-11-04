# Step 10 Audit Request

## Request

| Field | Value |
| --- | --- |
| Step | 10 — Job Templates and Supercell Generation |
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
| Depends on | 06, 08, 09 |
| Delivers | Job template collections/configurations, deterministic job expansion, and doping-aware supercell generation with charge compensation. |

Step page: [`Step 10 — Job Templates and Supercell Generation`](../steps/step-10-job-templates-and-supercells.md)

## Step-specific acceptance checklist

- [ ] The ceria KMC sample expands to the expected number of jobs with deterministic IDs.
- [ ] For a 10×10×10 ceria cell and 1 % Ce doping, the applied counts and vacancy compensation are correct.
- [ ] A requested fraction that cannot be realized is adjusted downward and reported.
- [ ] An impossible charge compensation fails with `JOB012`.
- [ ] Same job descriptor + seed produces an identical lattice across runs and OSes.
- [ ] Changing a simulation-only setting does not change the lattice for the same job.
- [ ] Optimizers remove only start options, not lattice particles.
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
