# Step Amendments

Amendments capture design changes that emerge during implementation without reopening a frozen step. They are
planned, reviewed, integrated into the parent step branch, and audited as part of the parent step.

Amendments are not a way to bypass the design review or audit gates. They are a way to keep a late design
change explicit, reviewable, and integrated before the parent step is frozen.

## 1. When to use an amendment

Use an amendment when implementation reveals:

- a public API or contract change that was not in the frozen design;
- a cross-step interface or format change;
- a design decision that is expensive to reverse;
- a temporary implementation that should be replaced before the parent step is complete.

Do not use an amendment for:

- ordinary bug fixes;
- small refactors that fit the existing design;
- unrelated scope;
- changes to a step that is already merged or tagged, unless the post-merge fallback in §7 applies.

## 2. Naming

| Item | Convention |
| --- | --- |
| Amendment ID | `step-NN-amend-M` |
| Plan page | `docs/planning/amendments/step-NN-amend-M-<slug>.md` |
| Design review record | `docs/planning/reviews/step-NN-amend-M.md` |
| Feature branch | `feature/step-NN-<step-slug>/amend-M-<slug>` |
| Large-amendment branch | `step/NN-amend-M-<slug>` with normal feature branches below it |
| Parent audit | `docs/planning/audits/step-NN.md` |
| Tag | The parent `step-NN` tag on the combined merged commit |

## 3. Status values

| Status | Meaning |
| --- | --- |
| **Proposed** | The change is recorded but not planned. |
| **Planned** | The amendment plan page is complete and ready for design review. |
| **Design review** | A fresh planning auditor is reviewing the amendment delta. |
| **Changes required** | The design review found blocking or material issues. |
| **Re-review required** | The design was updated and needs another review. |
| **Ready with notes** | No blockers or majors remain; the owner accepts the notes and implementation may proceed. |
| **Ready for implementation** | The amendment design is accepted. |
| **In progress** | The amendment is being implemented. |
| **Integrated** | The amendment is squash-merged into the parent step branch. |
| **Audited** | The parent audit passed at the combined commit. |
| **Merged** | The parent step was merged and tagged. |
| **Withdrawn** | The change is no longer needed; the resolving reason and commit are recorded in the amendment page. |
| **Waived** | The human owner explicitly accepted that no amendment review is required. |

## 4. Required sequence

1. Record the amendment while the parent step is still active and before its audit freeze.
2. Create the plan page from [`step-amendment-template.md`](step-amendment-template.md).
3. Run a focused pre-implementation design review under the normal design-review process. Record it in
   `docs/planning/reviews/step-NN-amend-M.md` and link it from the parent design review record.
4. Implement on the amendment feature branch from the parent step branch. Run the normal local gates.
5. After the design review is accepted and the local gates pass, squash-merge the amendment into the parent
   step branch before the parent audit freeze.
6. Update the parent step page, amendment page, audit request, and design review record with the combined
   commit.
7. Freeze the combined step branch. The parent audit covers the parent step and every integrated amendment at
   that commit. Do not create a separate audit record or tag for an integrated amendment.
8. On a passed audit, merge the parent step and create the `step-NN` tag on the combined commit. Mark the
   amendment `Merged`.

If the parent audit is already running, do not add commits. Stop the audit, integrate the amendment, and
restart the audit at the new frozen commit.

## 5. Design review

- The amendment uses the same statuses, severity, verdict, and reviewer write scope as
  [`docs/planning/reviews/README.md`](../reviews/README.md).
- The review scope is the amendment delta and its impact on the parent step, not a full re-review of the
  parent design.
- The parent review record links to the amendment review record.
- The review record is the only file the reviewer may commit.
- A small amendment may be `Waived` only by explicit human-owner approval, with the rationale recorded in the
  amendment page.
- When an amendment merge or tag is presented for approval, include the cumulative approval report required
  by [`AGENTS.md` §3.12](../../../AGENTS.md), covering every amendment review round, finding, and resolution.

## 6. Integration and audit

- The amendment is integrated into the parent step branch using the normal feature squash rules.
- The parent audit request lists the amendment IDs, the combined commit, and any temporary implementation that
  the amendment replaces.
- The auditor reviews the entire solution: parent step plus integrated amendments.
- There is one audit verdict, one merge, and one `step-NN` tag for the combined state.
- The amendment page links to the parent audit record; no separate amendment audit file is created.
- The parent merge/tag request includes the cumulative report for the combined solution: parent review and audit
  rounds, every amendment round, every finding and resolution, accepted notes and waivers, and residual risk.

## 7. Post-merge fallback

If the parent step is already merged or tagged:

- do not move or delete the existing `step-NN` tag;
- use a short-lived maintenance branch from `main`;
- audit the amendment separately;
- create a new tag such as `step-NN.1` or `step-NN-amend-M` only with explicit human-owner approval;
- record the amendment as a post-step change in the amendment page.

## 8. Amendment index

| Amendment | Parent step | Status | Branch | Design review | Parent audit / tag |
| --- | --- | --- | --- | --- | --- |
| `step-02-amend-1` (`public-math`) | 02 — Symmetry and Geometry | **Integrated** | `feature/step-02-symmetry-and-geometry/amend-1-public-math` | [Review](../reviews/step-02-amend-1.md) | `dbf5691`; audit `6341b56` |
| `step-02-amend-2` (`docs-math`) | 02 — Symmetry and Geometry | **Integrated** | `feature/step-02-symmetry-and-geometry/amend-2-docs-math` | [Review](../reviews/step-02-amend-2.md) | `dbf5691`; audit `6341b56` |
| `step-08-amend-1` (`canonical-projection`) | 08 — Model Compilation Pipeline | **Integrated** | `feature/step-08-model-compilation-pipeline/amend-1-canonical-projection` | [Review](../reviews/step-08-amend-1.md) | `28e741f`; audit passed with notes; merged and tagged `step-08` |
| `step-11-amend-1` (`dotnet-11-native-zstd`) | 11 — Simulation Package and `.msl` Writer | **Integrated** | `feature/step-11-simulation-package-and-msl-writer/amend-1-dotnet-11-native-zstd` | [Review](../reviews/step-11-amend-1.md) — `Ready with notes`, accepted 2026-11-15 | `00753e7`; the parent audit has not started |
| `step-11-amend-2` (`cluster-code-expansion`) | 11 — Simulation Package and `.msl` Writer | **Re-review required** — round 1 `Changes required` (A2-01 Major, A2-02…A2-11 Minors, A2-12…A2-16 Notes); corrections in the re-review freeze | `docs/step-11-amend-2` (design); `fix/step-11.1-cluster-code-expansion` after approval | [Review](../reviews/step-11-amend-2.md) | `step-11` stays; post-merge audit `docs/planning/audits/step-11-amend-2.md`; tag `step-11.1` proposed, evidence in the audit record (A2-07) |
| `step-13-amend-1` (`fixture-properties`) | 13 — Rust Solver Foundation and Package Reader | **Withdrawn** — resolved by the Step 08 fix `1f5b473` | `feature/step-13-rust-solver-foundation-and-package-reader/fix-audit-round-1` (historical) | — (withdrawn; no review requested) | Audit round 1 `Fixes required`; F-1 resolved by `step-13-centered`; re-audit pending |

`step-02-amend-1` covers the public generic math layer (`Vector3<T>`, `Vector4<T>`, `Matrix3<T>`,
`Matrix4<T>`) and the replacement of the temporary `CellGeometry` array-based inverse. See
[`step-02-amend-1-public-math.md`](step-02-amend-1-public-math.md).

`step-02-amend-2` enables KaTeX math rendering for the published documentation and replaces the Step 02
monospace formula blocks. See [`step-02-amend-2-docs-math.md`](step-02-amend-2-docs-math.md).

`step-13-amend-1` is **withdrawn**: the Step 08 fix `1f5b473` produces translation-equivalent starts, and the
real-writer `step-13-centered` fixture now carries both the cross-bucket `jump_mapping` and the per-node mask
difference. The amendment is retained for history; its Step 11 verifier observation remains a separate deferred
item. See [`step-13-amend-1-fixture-properties.md`](step-13-amend-1-fixture-properties.md).

`step-11-amend-1` moves the repository to the .NET 11 toolchain and replaces the `ZstdSharp.Port` payload codec
with the in-box `System.IO.Compression` Zstandard API, before the Step 11 audit freeze and before feature 7
generates the checked-in `.msl` fixture. It is a toolchain and dependency change, not a `.msl` format, signature,
or solver-contract change. See
[`step-11-amend-1-dotnet-11-native-zstd.md`](step-11-amend-1-dotnet-11-native-zstd.md).

`step-11-amend-2` is a **post-merge** amendment. It closes Step 14 design-review F-29 by adding one optional
`code_expansions` member to each `.msl` cluster table, mapping every raw slot-tuple code to its canonical
occupation index and applied energy. It follows the post-merge fallback in §7: a maintenance branch from `main`,
a focused design review, a separate audit, and a proposed `step-11.1` tag. The existing `step-11` tag is not
moved. See
[`step-11-amend-2-cluster-code-expansion.md`](step-11-amend-2-cluster-code-expansion.md).
