# Step 20 Audit Request

## Request

| Field | Value |
| --- | --- |
| Step | 20 — State Reader, Result Collection, and Evaluation API |
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
| Depends on | 18, 11 |
| Delivers | A safe `.mcs` reader, transactional result collection into the `.msr` sidecar, job querying, and the public evaluation API for transport, displacement, diffusion, correlation, coordination, and jump statistics. |

Step page: [`Step 20 — State Reader, Result Collection, and Evaluation API`](../steps/step-20-state-reader-and-evaluation-api.md)

## Step-specific acceptance checklist

- [ ] A collected ceria job can be queried and evaluated by ID/metadata.
- [ ] Particle counts, MSD, D, mobility, conductivity, and correlation match analytic/hand calculations.
- [ ] `D_σ` is dimensionally correct and consistent with `σ = n q² D/(k_B T)`.
- [ ] Per-species and total transport values are reported separately; `N_s` is the tracked ensemble size.
- [ ] MSD is origin-based and documented as cumulative displacement from the initial state.
- [ ] Zero-field jobs return `n/a` transport values, not NaN/Inf.
- [ ] Correlation/Onsager matrices are symmetric.
- [ ] `collect` is idempotent, validates states before writing, and writes `.msr`, never `.msl`.
- [ ] A `.msr` from a different package is rejected.
- [ ] Corrupt/truncated states are rejected with job context.
- [ ] Evaluation of 1,000 jobs streams without loading all blobs.
- [ ] CLI CSV/JSON output includes units and metadata.
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
