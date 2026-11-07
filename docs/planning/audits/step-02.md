# Step 02 Audit Request

## Request

| Field | Value |
| --- | --- |
| Step | 02 — Symmetry and Geometry |
| Request owner | Step agent |
| Audit status | Not requested |
| Audited commit | — |
| Step branch | — |
| Step tag | — |
| CI run | — |
| Audit date | — |
| Auditor | — |

## Step 02 amendments

The Step 02 audit covers the parent step plus these integrated amendments at the combined frozen commit:

- `step-02-amend-1` — public generic math — plan
  [`step-02-amend-1-public-math.md`](../amendments/step-02-amend-1-public-math.md), review
  [`step-02-amend-1.md`](../reviews/step-02-amend-1.md).
- `step-02-amend-2` — KaTeX documentation math — plan
  [`step-02-amend-2-docs-math.md`](../amendments/step-02-amend-2-docs-math.md), review
  [`step-02-amend-2.md`](../reviews/step-02-amend-2.md).

Combined audit commit: pending. Amendment records are `Ready with notes`; the notes were resolved and implementation may proceed after
owner acceptance.

## Step summary

| Field | Value |
| --- | --- |
| Phase | A. Foundations |
| Depends on | 01 |
| Delivers | A validated space-group catalog, exact symmetry operations, coordinate systems, P1 expansion, orbit/permutation reduction, and periodic geometry kernels. |

Step page: [`Step 02 — Symmetry and Geometry`](../steps/step-02-symmetry-and-geometry.md)

## Step-specific acceptance checklist

- [ ] All 230 international groups and 274 settings load.
- [ ] Every setting passes identity, inverse, closure, determinant, and translation-normalization checks.
- [ ] Group 47 is valid and matches the authoritative setting.
- [ ] General-position multiplicities match the International Tables for every setting.
- [ ] P1 expansion of every sample site matches a brute-force expansion.
- [ ] The canonical position order is stable across runs, OSes, and input ordering.
- [ ] The triclinic cell test produces the correct C vector and angles.
- [ ] Pair and cluster reductions match brute-force orbit enumeration for random geometries.
- [ ] A 20×20×20 cell's position expansion completes in under 1 second.
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
