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
| Design review status | Re-review required |
| Reviewed commit | `aad0835be9a0f0534e58a29392b64bf6aac2d588` (`aad0835`) |
| Docs branch | `main` |
| Review date | 2026-11-07 |
| Reviewer | Independent planning auditor |

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
- `docs/development/architecture.md`
- `docs/development/agent-workflow.md`
- `src/dotnet/Mocassin.Core/Geometry/Vector3.cs`
- `src/dotnet/Mocassin.Core/Geometry/Vector4I.cs`
- `src/dotnet/Mocassin.Core/Geometry/Cartesian3.cs`
- `src/dotnet/Mocassin.Core/Geometry/Fractional3.cs`
- `src/dotnet/Mocassin.Symmetry/Int3x3.cs`
- `src/dotnet/Mocassin.Symmetry/CellGeometry.cs`
- `src/dotnet/Mocassin.Core.Tests/GeometryTests.cs`
- `src/dotnet/Mocassin.Symmetry.Tests/CellGeometryTests.cs`
- `src/dotnet/Mocassin.Core/PublicAPI.Unshipped.txt`
- `src/dotnet/Mocassin.Symmetry/PublicAPI.Unshipped.txt`

Commands run:

```text
git status --short
git log --oneline -8 main
git show main:docs/planning/amendments/step-02-amend-1-public-math.md
git diff main HEAD -- docs/planning/amendments
git diff --stat main HEAD
git ls-tree -r --name-only main -- docs/planning/reviews
grep -n "Generic math is an implementation detail\|internal readonly record struct Vector3\|Public types stay semantic" docs/planning/steps/step-02-symmetry-and-geometry.md
grep -n "Generic math" docs/planning/01-target-architecture.md
sed -n '1,120p' src/dotnet/Mocassin.Core/Geometry/Vector3.cs
sed -n '1,120p' src/dotnet/Mocassin.Symmetry/CellGeometry.cs
dotnet test src/dotnet/Mocassin.Symmetry.Tests/Mocassin.Symmetry.Tests.csproj -c Release --filter "FullyQualifiedName~CellGeometryTests" --nologo
dotnet test src/dotnet/Mocassin.Core.Tests/Mocassin.Core.Tests.csproj -c Release --filter "FullyQualifiedName~GeometryTests" --nologo
```

Results: the working tree was clean; the two review records are on `main` (`ce6a563`) and absent
from the closeout branch; `CellGeometryTests` passed 14/14 and `GeometryTests` passed 22/22. A
scratch .NET check under `/tmp/opencode` confirmed that `decimal` implements
`IFloatingPoint<decimal>` (`True`). No build or test of the amendment was run because the amendment is
planned, not implemented.

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
| **Major** | **A1-1 — The amendment changes a frozen design decision without superseding it.** The frozen Step 02 design says public types remain semantic and non-generic, the generic kernel is `internal`, and “Generic math is an implementation detail, not a public format or API contract.” The target architecture lists generic math under “Internal vector/matrix kernels”. The amendment makes `Vector3<T>`, `Vector4<T>`, `Matrix3<T>`, and `Matrix4<T>` public and exposes `CellGeometry.Matrix`/`Inverse`, but its Documentation checklist updates only `docs/development/design-decisions.md`, `docs/development/architecture.md`, and `docs/planning/05-open-decisions.md`. | `docs/planning/steps/step-02-symmetry-and-geometry.md:295-305`; `docs/planning/01-target-architecture.md:279`; `docs/planning/amendments/step-02-amend-1-public-math.md:40-46, 256-262`. Required: explicitly supersede the old statement and update the Step 02 page (generic math strategy, public/internal API boundary, implementation tasks) and `01-target-architecture.md`; add both to the Documentation checklist. |
| **Major** | **A1-2 — `IFloatingPoint<T>` admits `decimal`, contradicting the plan’s non-goal.** The plan says “No integer or `decimal` matrix inverse” and “decimal and integer matrices are not part of this API”, but the matrix constraints are `T : unmanaged, IFloatingPoint<T>`. `decimal` implements `IFloatingPoint<decimal>` in .NET 10, so `Matrix3<decimal>` and `Matrix4<decimal>` would compile. | `docs/planning/amendments/step-02-amend-1-public-math.md:53-54, 66-71`; scratch .NET 10 check: `typeof(decimal).GetInterfaces()` contains `IFloatingPoint<>` (`True`); `docs/planning/01-target-architecture.md:279` already names `IFloatingPointIeee754<T>`. Required: use `IFloatingPointIeee754<T>` for matrices or explicitly document and test decimal matrix support; do not leave the constraint and non-goal contradictory. |
| **Minor** | **A1-3 — 4×4 is not justified by a concrete Step 02 consumer.** `CellGeometry` needs only `Matrix3<double>`; `Vector4<T>`/`Matrix4<T>` add public API for later rendering, UX, and extension use without a named near-term consumer. | `docs/planning/amendments/step-02-amend-1-public-math.md:40-43, 50-51, 84-104, 130-160`; `src/dotnet/Mocassin.Symmetry/CellGeometry.cs` uses only 3×3 math. Required: cite a specific near-term consumer or defer the 4D types; if kept, add explicit `Matrix4` `FromRows`, `FromColumns`, `Transform`, and `ConditionNumber` tests. |
| **Minor** | **A1-4 — API-baseline and performance evidence are underspecified.** The Documentation checklist says “API baseline” without naming both `Mocassin.Core` and `Mocassin.Symmetry`; `DOTNET_JitDisasm` is a JIT environment variable, not a check; the benchmark/inlining acceptance has no threshold or recording location. | `docs/planning/amendments/step-02-amend-1-public-math.md:44, 227, 234, 242-254, 258-262`; both `src/dotnet/Mocassin.Core/PublicAPI.Unshipped.txt` and `src/dotnet/Mocassin.Symmetry/PublicAPI.Unshipped.txt` exist. Required: name both baselines, use `DisassemblyDiagnoser` or a benchmark assertion, and define the measurable no-allocation/no-unexpected-call criterion and where the evidence is recorded. |
| **Minor** | **A1-5 — Parent review/audit links and branch state are not handled.** The amendment process requires the parent review record to link to the amendment review and the parent audit request to list amendment IDs; neither is in the Documentation checklist. The current closeout branch still shows the amendment as `Planned` and lacks the review records, which are on `main`. | `docs/planning/amendments/README.md:69-87`; `docs/planning/reviews/step-02.md` has no amendment link; `docs/planning/audits/step-02.md` has no amendment list; `git diff main HEAD` shows closeout status `Planned` versus main `Design review`. Required: add both links/updates and synchronize the parent step branch before implementation. |
| **Note** | **A1-6 — Clarify singular-matrix and inlining behavior.** `Inverse` throws on a zero determinant and `CellGeometry` relies on `CellDeterminantTolerance`/`CellConditionLimit`; the plan does not state what `ConditionNumber` does for a singular matrix or whether `Inverse` uses a tolerance. `AggressiveInlining` on `Matrix3<T>.Inverse` may increase code size and should be reserved for measured hot paths. | `docs/planning/amendments/step-02-amend-1-public-math.md:150-180, 220-224`; `src/dotnet/Mocassin.Symmetry/CellGeometry.cs` computes `ConditionNumber` after a successful inverse and rejects via `IsConditionWithinLimit`. Required: document the singular/condition-number contract and limit `AggressiveInlining` to measured hot methods. |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The planning auditor writes the findings and
verdict directly. The step agent adds resolutions in the separate `Resolution status` section without altering
the auditor's findings, evidence, or verdict.

## Verdict

- [ ] Ready for implementation
- [ ] Ready with notes
- [x] Changes required
- [ ] Re-review required
- [ ] Waived

**Changes required.** Two major findings remain: the amendment changes the frozen internal-generic-math
decision without superseding the Step 02 page and target architecture (A1-1), and the matrix constraint admits
`decimal` despite the plan’s non-goal (A1-2). The 4×4 scope, API-baseline/performance evidence, and parent
review/audit links are minor corrections. No blocker was found; the core design is sound and the current cell
geometry tests pass.

Follow-up owner: Planner / step agent

Follow-up step: Update the amendment plan and affected design docs (Step 02 page and
`01-target-architecture.md`), replace `IFloatingPoint<T>` with `IFloatingPointIeee754<T>` or explicitly support
and test `decimal`, justify or defer `Vector4<T>`/`Matrix4<T>`, name both API baselines, define the
benchmark/inlining evidence, add the parent review/audit links, synchronize the parent step branch, and request
a fresh re-review.

## Resolution status

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| A1-1 | Fixed — Step 02 generic-math rule and target architecture superseded | Planner | `aad0835` |
| A1-2 | Fixed — matrices use `IFloatingPointIeee754<T>`; `decimal` excluded | Planner | `aad0835` |
| A1-3 | Fixed — 4×4 consumer justification and tests added | Planner | `aad0835` |
| A1-4 | Fixed — both API baselines and benchmark/disassembly evidence defined | Planner | `aad0835` |
| A1-5 | Fixed — parent review/audit links and branch-sync prerequisite added | Planner | `aad0835` |
| A1-6 | Fixed — singular/condition-number contract and inlining limits documented | Planner | `aad0835` |

The step agent fills this section after the verdict. Do not alter the reviewer-authored findings, evidence, or
verdict.
