# Step 02 Amendment 1 Design Review

> Reviewer: write the evidence, findings, and verdict directly into
> `docs/planning/reviews/step-02-amend-1.md`. You may edit only your own metadata (`Reviewer`, `Review date`,
> `Design review status`), the `Evidence`, `Findings`, and `Verdict` sections, and commit only that file with
> a documentation-only message and `[skip ci]`. Do not edit the request scope, checklists, resolutions, code,
> tests, configuration, status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 02 — Symmetry and Geometry |
| Amendment | `step-02-amend-1` — Public Generic Math Types |
| Request owner | Step agent |
| Design review status | Ready for review |
| Reviewed commit | `bd4876314ed962da64c49ea747d7e3e4ed2e7552` (`bd48763`) |
| Docs branch | `main` |
| Review date | 2026-11-07 |
| Reviewer | — |

Parent step page: [`Step 02 — Symmetry and Geometry`](../steps/step-02-symmetry-and-geometry.md)

Amendment plan: [`Step 02 Amendment 1 — Public Generic Math Types`](../amendments/step-02-amend-1-public-math.md)

## Step summary

| Field | Value |
| --- | --- |
| Parent phase | A. Foundations |
| Parent depends on | 01 — Domain Kernel and Project Format |
| Amendment delivers | Public generic vector and matrix math in `Mocassin.Core.Geometry`; `CellGeometry` migrated from `double[]` to `Matrix3<double>` |

## Design decisions reviewed

- [ ] Public `Vector3<T>` and `Vector4<T>` with `INumber<T>` constraints
- [ ] Public row-based `Matrix3<T>` and `Matrix4<T>` with `IFloatingPoint<T>` constraints
- [ ] `Int3x3` remains the exact integer specialization
- [ ] `CellGeometry` migration to `Matrix3<double>` with no behavior change
- [ ] Row-major convention and `M * v` semantics
- [ ] Inverse, determinant, condition-number, and singular-matrix behavior
- [ ] XML documentation and public-API baseline coverage
- [ ] Tests, benchmarks, and inlining checks
- [ ] No project/package/results/state format changes

## Dependency and interface checks

- [ ] Step 02 cell geometry is implemented and its design is stable
- [ ] The Step 02 audit is not yet frozen
- [ ] Public API additions are additive and intentional
- [ ] `Int3x3` and exact symmetry arithmetic remain unchanged
- [ ] No unresolved cross-step interface question remains

## Standard design-review checklist

- [ ] Scope, non-goals, and acceptance criteria are explicit and testable
- [ ] Design decisions are complete and internally consistent
- [ ] Required tests, property tests, golden fixtures, and performance targets are defined
- [ ] Documentation, status, parity, and audit updates are planned
- [ ] Licensing, provenance, and dependency checks are resolved
- [ ] Risks and mitigations are realistic
- [ ] No hidden scope, stale placeholders, or unowned decisions

## Evidence

Files reviewed:

- `AGENTS.md`
- `docs/planning/amendments/README.md`
- `docs/planning/amendments/step-02-amend-1-public-math.md`
- `docs/planning/steps/step-02-symmetry-and-geometry.md`
- `docs/planning/01-target-architecture.md`
- `docs/planning/02-data-contracts.md`
- `docs/planning/03-quality-strategy.md`
- `docs/planning/05-open-decisions.md`
- `docs/development/design-decisions.md`
- `docs/development/agent-workflow.md`
- `src/dotnet/Mocassin.Core/Geometry/Vector3.cs`
- `src/dotnet/Mocassin.Core/Geometry/Vector4I.cs`
- `src/dotnet/Mocassin.Core/Geometry/Cartesian3.cs`
- `src/dotnet/Mocassin.Core/Geometry/Fractional3.cs`
- `src/dotnet/Mocassin.Symmetry/Int3x3.cs`
- `src/dotnet/Mocassin.Symmetry/CellGeometry.cs`
- `src/dotnet/Mocassin.Core/PublicAPI.Unshipped.txt`
- `src/dotnet/Mocassin.Symmetry/PublicAPI.Unshipped.txt`

Commands run:

```text

```

## Known notes for the reviewer

- The amendment is **planned, not implemented**. The Step 02 implementor is active on the closeout feature.
- The reviewed design commit is `bd48763`. This review record is metadata added after that commit and does not
  change the design.
- `CellGeometry` currently uses a temporary `double[] inverse`; the amendment replaces it with
  `Matrix3<double>`.
- The public generic math layer is intentionally broader than the immediate cell-geometry need because it will
  be reused by later compilation, rendering, UX, and extension code.
- Check whether `IFloatingPoint<T>` is the right matrix constraint, whether `Int3x3` should stay separate, and
  whether 4×4 is justified now rather than deferred.
- Check row-major versus column-major conventions, `FromColumns`, `M * v`, inverse behavior for singular
  matrices, and the condition-number definition.
- Check that the amendment introduces no project/package/results/state format changes and no scientific
  behavior changes.
- The amendment must be integrated before the Step 02 audit freeze; the parent audit then covers the combined
  solution.

## Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| — | — | — |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The planning auditor writes the findings and
verdict directly. The step agent adds resolutions in the separate `Resolution status` section without altering
the auditor's findings, evidence, or verdict.

## Verdict

- [ ] Ready for implementation
- [ ] Ready with notes
- [ ] Changes required
- [ ] Re-review required
- [ ] Waived

Follow-up owner:
Follow-up step:

## Resolution status

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| — | — | — | — |

The step agent fills this section after the verdict. Do not alter the reviewer-authored findings, evidence, or
verdict.
