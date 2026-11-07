# Step 02 Audit Request

## Request

| Field | Value |
| --- | --- |
| Step | 02 — Symmetry and Geometry |
| Request owner | Step agent |
| Audit status | Ready for audit |
| Audited commit | `d6eac6f` (combined solution; docs-only freeze commit follows) |
| Step branch | `step/02-symmetry-and-geometry` |
| Step tag | — |
| CI run | [42427151237](https://github.com/seb-eis/mocassin-2/actions/runs/42427151237) — green at `d6eac6f` |
| Audit date | 2026-11-07 |
| Auditor | Fresh out-of-context auditor (to be assigned) |

## Step 02 amendments

The Step 02 audit covers the parent step plus these integrated amendments at the combined frozen commit:

- `step-02-amend-1` — public generic math — plan
  [`step-02-amend-1-public-math.md`](../amendments/step-02-amend-1-public-math.md), review
  [`step-02-amend-1.md`](../reviews/step-02-amend-1.md).
- `step-02-amend-2` — KaTeX documentation math — plan
  [`step-02-amend-2-docs-math.md`](../amendments/step-02-amend-2-docs-math.md), review
  [`step-02-amend-2.md`](../reviews/step-02-amend-2.md).

Combined audit commit: `d6eac6f`. Both amendments are integrated; the human owner accepted the amendment notes.

## Step summary

| Field | Value |
| --- | --- |
| Phase | A. Foundations |
| Depends on | 01 |
| Delivers | A validated space-group catalog, exact symmetry operations, coordinate systems, P1 expansion, orbit/permutation reduction, periodic geometry, version records, caches, and the expansion benchmark. |

Step page: [`Step 02 — Symmetry and Geometry`](../steps/step-02-symmetry-and-geometry.md)

## Step-specific acceptance checklist

- [x] All 230 international groups and 274 settings load.
- [x] Every setting passes identity, inverse, closure, determinant, and translation-normalization checks.
- [x] Group 47 is valid and matches the authoritative spglib `Pmmm` setting.
- [x] General-position multiplicities match the International Tables for every setting.
- [x] P1 expansion of every Step 02 synthetic fixture site matches a brute-force expansion.
- [x] The canonical position order is stable across runs, OSes, and input ordering.
- [x] The triclinic cell test produces the correct C vector and angles.
- [x] Pair and cluster reductions match brute-force orbit enumeration for random geometries.
- [x] The `P1Expansion20Cubed` fixture (64 P1 positions, 20×20×20) meets its under-1-second target.
- [x] Documentation written

## Known notes for the auditor

- The audited solution is `d6eac6f`; a documentation-only audit-request/status commit follows it. The CI run
  above is for the solution commit.
- `periodic-geometry` was implemented before `orbit-reduction` because pair reduction consumes radial search.
  The approved feature set is unchanged.
- The catalog uses a compact positional JSON encoding rather than the reviewed keyed/pretty form, at the human
  owner's request. The file is ~171 KB; the checksum covers the canonical settings payload.
- `CellConditionLimit = 1e12` is defensive; within the v1 length range and positive-determinant cutoff,
  realizable cells stay below it. The predicate is unit-tested directly.
- The `SymmetryVersionInfo` record is ready, but writing it into the compiled `.msl` payload belongs to Step 11;
  the corresponding Step 02 acceptance item is intentionally unchecked.
- The benchmark was recorded locally at 965.3 µs mean on the development container; the declared runner class
  is `linux-x64-github-ubuntu-latest-dotnet10`.
- `SymmetryCache` is internal for now; caches are scoped per compilation and bounded at 4096 entries.
- Amendment 1 (`cd796b0`) adds public `Vector3<T>`/`Vector4<T>`/`Matrix3<T>`/`Matrix4<T>` and migrates
  `CellGeometry` to `Matrix3<double>`.
- Amendment 2 (`d6eac6f`) adds KaTeX 0.19.1 rendering and converts the published symmetry formulas.

## Standard audit checklist

- [x] Required tests from the step page pass.
- [x] Public/protected XML documentation policy is followed.
- [x] Documentation, status dashboard, feature log, and parity matrix are current.
- [x] CI is green at the audited commit.
- [x] Format/version/fixture changes are documented and tested.
- [x] Security, dependency, licence, and secret-scan gates pass.
- [x] No unrelated changes or unapproved scope.
- [x] Branch, tag, and status hygiene are correct.

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
