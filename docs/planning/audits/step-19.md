# Step 19 Audit Request

## Request

| Field | Value |
| --- | --- |
| Step | 19 — Solver CLI, HPC Workflows, and Extensions |
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
| Depends on | 18 |
| Delivers | A production `mocassin-sim` CLI, batch/worker execution, SLURM/PBS-friendly scripts, graceful signals, deterministic seeds, and a versioned extension ABI (including MMCFE parity). |

Step page: [`Step 19 — Solver CLI, HPC Workflows, and Extensions`](../steps/step-19-solver-cli-hpc-and-extensions.md)

## Step-specific acceptance checklist

- [ ] `mocassin-sim --db ceria.msl --job 1 --io ./run1` produces `run.mcs`, logs, and `solver.json`.
- [ ] A completed job exits `18` on rerun, not silent success.
- [ ] SIGINT during a run writes a resumable state and exits `15`.
- [ ] `batch --jobs 1-10 --parallel 4` runs all jobs with independent seeds.
- [ ] The SLURM array script runs the ceria sample on a scheduler test harness.
- [ ] A sample extension loads, logs, and modifies a position; an incompatible ABI is rejected.
- [ ] MMCFE runs an annealing schedule and produces a resumable `mmcfe.db` matching the contract.
- [ ] Logs are structured and contain job/model identifiers.
- [ ] No job can write outside its I/O directory (path validation).
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
