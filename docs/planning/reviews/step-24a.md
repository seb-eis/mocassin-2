# Step 24A Design Review

> Review agent: write the evidence, findings, and verdict directly into
> `docs/planning/reviews/step-24a.md`. You may edit only your own metadata (`Review agent`, `Review date`, `Record state`), the checklist completion marks and inline verification notes (not item text), the
> `Evidence`, `Findings`, and `Verdict` sections, and commit only that file with a documentation-only message
> and `[skip ci]`. Do not edit the request scope, checklist item text, resolutions, code, tests, configuration,
> status dashboard, step page, or any other file.

## Request

| Field | Value |
| --- | --- |
| Step | 24A — Shared Editor Framework, Particles, and Structure |
| Request owner | Design agent (design freeze); the dev takes ownership of resolutions at the start gate |
| Lead | Lead agent, which spawned the design request on 2026-11-23 |
| Dev | Not assigned — created from the reviewed `main` commit after the start-gate approval |
| Review initiated by | Lead, on request of the design agent |
| Design class | Full |
| Reviewed commit | `b98a56f824002bc3cdbb4ace9e0b5cf9dd464ba9` |
| Docs branch | `docs/step-24-design` |
| Review date | — |
| Review agent | — |

Step page: [`Step 24A — Shared Editor Framework, Particles, and Structure`](../steps/step-24a-model-editors-particles-and-structure.md)

**Freeze note.** The freeze is the commit that introduces this request — the Step 24A/24B split, the 24A design,
the closed layout gate in ADR 0003, and this record. The record was part of that commit so the branch has no
dangling links; a documentation-only follow-up then filled the `Reviewed commit` cell with the freeze SHA before
the lead requests the review. The freeze contains no code, test, configuration, contract, generated file, or
feature-work edit.

**Split note.** The human owner split Step 24 into **24A** (this step) and **24B** (transition editors) on
2026-11-22 under the complexity rule (class High, more than one independent acceptance story, an audit surface too
large for one sitting), at the same gate that recorded the editor layout. 24A contains no 24B task; 24B depends on
24A and defines its own design, which is **not** frozen, not reviewed, and not started. The superseded unsplit
design is
[`step-24-model-editors-particle-structure-transitions.md`](../steps/step-24-model-editors-particle-structure-transitions.md);
it has no review record, and the page is retained as the closed layout-gate history.

> Coordination: the lead spawns a fresh review agent after the design agent has frozen the design and completed
> this request, and relays the verdict and owner decisions. The design agent and dev update the design docs, apply
> fixes on the design docs or a maintenance branch, add resolutions without altering review text, and ask the
> lead for re-review until the review agent no longer identifies Blocker or Major issues. The lead does not edit
> the design or the review record. The human owner approves the final verdict and the start of implementation.
>
> Design depth: 24A is a `Full` design review. Use the full checklist and stop when no Blocker or Major remains;
> Minor and Note findings are accepted or deferred and do not trigger another round. A third Full round needs an
> owner decision.

## Step summary

| Field | Value |
| --- | --- |
| Phase | F. GUI (first half of the split Step 24) |
| Depends on | 23B, 02, 03, 04 (05/06 read-only for usage counts) — all `Done`, merged, and tagged. Not dependent on Steps 08–11 or 13–22 |
| Delivers | The ADR 0003 editor surface and shared model-editor framework; particle and particle-set editors; space-group and cell-parameter editors with a pre-apply compatibility preview; the cell-site table with the P1 preview; dummy positions; the structure summary; and the 24A tests, documentation, and closeout. No transition editor (24B) |
| Complexity and split | Class **High** on cross-step interfaces and test-matrix breadth; the other dimensions are Medium or Low. **Owner acceptance as one High unit is required at the start gate**; no further split is proposed (page §16). The split itself was approved by the owner on 2026-11-22 |

## Request scope — what this review covers

Review these design artifacts at the frozen commit:

- [`Step 24A — Shared Editor Framework, Particles, and Structure`](../steps/step-24a-model-editors-particles-and-structure.md):
  goal, scope, non-goals, §1–§18 (framework, table/column policy and focus, reference picker and expression,
  particle/set editors, structure documents, validation/navigation/commands, state ownership, settings, the
  library change, dependency checks, documentation, tests, performance, risks, classes, decisions, closeout).
- [`Step 24B — Transition Editors`](../steps/step-24b-model-editors-transitions.md) — **only** to confirm that the
  split boundary is dependency-clean and that no 24B task is smuggled into 24A. 24B is a draft, not part of this
  review.
- [`ADR 0003 — Avalonia MVVM Stack`](../adr/0003-avalonia-mvvm-stack.md), especially
  [Editor layout decision (2026-11-22)](../adr/0003-avalonia-mvvm-stack.md#editor-layout-decision-2026-11-22) and
  `Avoid`.
- The superseded page
  [`step-24-model-editors-particle-structure-transitions.md`](../steps/step-24-model-editors-particle-structure-transitions.md)
  — the retained §0 layout-gate record and the §0.8 outcome, as the evidence trail for the pick.
- [`04-roadmap.md`](../04-roadmap.md) §3, §3.1 (recorded Step 24 split), §5; [`05-open-decisions.md`](../05-open-decisions.md)
  (resolved and gated rows); [`status.md`](../status.md); [`README.md`](../README.md) step index.
- Dependency contracts: [`02-data-contracts.md`](../02-data-contracts.md) (no change expected),
  [`01-target-architecture.md`](../01-target-architecture.md) §3.2 (App boundaries), the 23A/23B step pages'
  seam tables, and the Step 03/04/05/06 model and `ProjectEditor` surfaces the editors call.
- Quality and interaction contracts:
  [`03-quality-strategy.md`](../03-quality-strategy.md),
  [`ui-interaction-contract.md`](../../development/ui-interaction-contract.md) §8,
  [`ui-visual-contract.md`](../../development/ui-visual-contract.md),
  [`code-structure.md`](../../development/code-structure.md),
  [`design-proportionality.md`](../../development/design-proportionality.md).
- Licensing: no new dependency is proposed; confirm that against `Directory.Packages.props` and the POC record.

## Design decisions and scope (request summary)

- **Layout (D-1).** Option 1 (`ListBox` table + detail) composed with option 4's `GridSplitter`; one shared column
  policy and the production 20 px scroll gutter; the matrix control is a detail-pane data-shape control. Recorded
  by the owner in ADR 0003 on 2026-11-22; the POC report and its PNG subset are the evidence and the spike code is
  discarded.
- **Framework.** `ModelEditorDocumentViewModel` + `ModelEditorViewModel` (`ObservableObject`) +
  `ModelDetailViewModel` (`ObservableValidator`); `ModelTableSurface` (a `UserControl`, not a `TemplatedControl`);
  typed sort keys; one tab stop per selection surface with `Tab` reaching the detail pane after `Home`/`End`;
  multi-select where bulk delete is safe.
- **Editing path (D-8).** The typed `ProjectEditor` operations the CLI uses, committed through the one
  `ProjectEditCoordinator` write path; one additive Core `ProjectDocument.Apply` overload so an edit is validated
  once and records one undo entry.
- **Reconciliation of the indicative design.** D-2 (no cascade delete in 24A), D-3 (set detail owns its drag
  source), D-4 (validator on the detail, not the list base), D-5 (dummy positions as their own document; P1
  preview as a panel toggle, not a tab), D-6 (no trigonometric functions in the expression in v1), D-7
  (document-local gestures), D-9 (one global `editorFraction`), D-10 (1,000-site virtualization acceptance).
- **Scope control.** No transition editor, no cascade delete, no energy/lattice/simulation/3D UI, no format or
  diagnostic-code change, no new dependency. One additive Core API and one additive app-local setting only.
- **Evidence plan.** §13 names the tests and tiers for every acceptance criterion, including the 1,000-site and
  >1,000-P1-position virtualization cases, the two-culture expression tests, the lock-table-versus-`CellGeometry`
  cross-check, and the real ceria acceptance test.

## Known limitations and deliberate omissions

- **No cascade deletion in 24A (D-2).** The indicative design offered cascade removal for a referenced site;
  24A blocks the delete with a dependents list and navigation instead. This is a deliberate scope narrowing for
  the owner to confirm at the start gate, not an oversight.
- **No trigonometric functions in the numeric expression (D-6).** The degrees/radians convention is not decided,
  so the step ships `+ - * / ^`, parentheses, `pi`, `e`, `sqrt`, `ln`, `exp`, `abs` and documents the limit.
- **No column resize/reorder.** ADR 0003 defers option 5 (the custom `TableView`) until a step needs
  resize/reorder; the shared column policy keeps that addable without a rewrite.
- **The 2D projection is a preview, not a viewport.** Step 27 replaces it with the 3D viewer; 24A does not claim
  a 3D preview.
- **24B is not reviewed here.** Its compile-preview API question is recorded on the 24B page and is decided at the
  24B freeze; it is not a 24A dependency.
- **Platform evidence is planned, not yet produced.** Windows and macOS keyboard/focus/DnD gates are required at
  the audit (page §18); no CI run exists at the design freeze.

## Design decisions reviewed

- [ ] Step page design decisions
- [ ] Data contracts and format changes
- [ ] Architecture and module boundaries
- [ ] ADRs and open decisions
- [ ] Quality strategy and test plan
- [ ] Parity matrix and documentation plan
- [ ] Licensing and provenance
- [ ] Post-implementation audit plan

## Dependency and interface checks

- [ ] Dependencies are `Done` and their contracts are stable
- [ ] Inputs, outputs, types, and formats are defined
- [ ] Versioning and migration behavior is defined
- [ ] No unresolved cross-step interface question remains

## Standard design-review checklist

Use this full checklist for a `Full` step. A `Light` step answers the five design questions only.

- [ ] Design class (`Full` or `Light`) is recorded and matches the triggers
- [ ] Scope, non-goals, and acceptance criteria are explicit and testable
- [ ] Complexity class and split decision are recorded and justified per
      [`04-roadmap.md` §3.1](../04-roadmap.md#31-step-sizing-and-split-triggers)
- [ ] Design decisions are complete and internally consistent
- [ ] Required tests, property tests, golden fixtures, and performance targets are defined
- [ ] Documentation, status, parity, and audit updates are planned
- [ ] Licensing, provenance, and dependency checks are resolved
- [ ] Risks and mitigations are realistic
- [ ] Code layout and namespace plan follow the structure policy
- [ ] No hidden scope, stale placeholders, or unowned decisions

## Evidence

Files reviewed:

Commands run:

## Findings

| Severity | Origin | Finding | Evidence |
| --- | --- | --- | --- |
| — | — | — | — |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. Origin: **design**, **implementation**,
**evidence**, or **documentation**. The review agent writes the findings and verdict directly. The dev adds
resolutions in the separate `Resolution status` section without altering the review agent's findings, evidence,
or verdict.

## Record state

**State:** Ready for review

`Re-review required` is a record state, not a verdict.

## Reviewer independence

> I am a fresh context. I did not author or implement the reviewed work and have no shared conversation with the
> design agent, dev, or lead.

## Verdict

- [ ] Ready for implementation
- [ ] Ready with notes
- [ ] Changes required
- [ ] Waived

Follow-up owner:
Follow-up step:

## Approval summary

The request owner (dev or design agent) completes this section before the lead presents the approval
request. It must cover the full history, not only the latest round.

- [ ] Requested decision and frozen commit recorded
- [ ] Every review/audit round recorded (round, date, commit, review agent, verdict)
- [ ] Every finding recorded (ID, severity, issue, evidence, resolution, fix commit, status)
- [ ] Accepted notes and waivers recorded with rationale
- [ ] Residual risks, deferred items, and follow-up owners recorded
- [ ] Explicit no-Blocker/no-Major statement, or the exact exceptions being waived
- [ ] Final CI and documentation-build results recorded

Round history:

| Round | Date | Commit | Review agent | Verdict |
| --- | --- | --- | --- | --- |
| — | — | — | — | — |

Findings and resolutions:

| Finding | Severity | Issue | Resolution | Commit | Status |
| --- | --- | --- | --- | --- | --- |
| — | — | — | — | — | — |

## Human approval

- [ ] Accepted: Ready for implementation
- [ ] Accepted: Ready with notes
- [ ] Changes required
- [ ] Waived

Approved by:
Date:

## Resolution status

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| — | — | — | — |

The dev fills this section after the verdict. Do not alter the review agent-authored findings, evidence, or
verdict.

## Review rounds

Append each new round below. Never overwrite an earlier round.

### Review round 1

#### Independence and frozen state

#### Commands and results

#### Findings

#### Verdict
