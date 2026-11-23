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
| Review date | 2026-11-23 |
| Review agent | DeepSeek V4.1 Flash 1017 (fresh design-review context; `opencode-go/deepseek-v4.1-flash`) |

Step page: [`Step 24A — Shared Editor Framework, Particles, and Structure`](../steps/step-24a-model-editors-particles-and-structure.md)

**Freeze note.** The freeze is the commit that introduces this request — the Step 24A/24B split, the 24A design,
the closed layout gate in ADR 0003, and this record. The record was part of that commit so the branch has no
dangling links; a documentation-only follow-up then filled the `Reviewed commit` cell with the freeze SHA before
the lead requests the review. The freeze contains no code, test, configuration, contract, generated file, or
feature-work edit.

**Post-freeze delta (documentation only).** One later commit on this branch adds a test-planning paragraph to the
**24B draft** page (the 1,000-reference-site limit does not bound the 24B tables, and `TRN019` caps transitions at
100) and this note. It changes no 24A design text, so the reviewed commit remains the freeze SHA above; the 24B
page is out of this review's scope beyond the split-boundary check.

**Round-2 resolution note.** Round 1 returned `Changes required` for **M-1** (Major) with m-1…m-12 and n-1/n-2.
The design was corrected and re-frozen at the commit recorded in `Resolution status` below. Changed files: the 24A
step page, ADR 0003, `01-target-architecture.md` §3.2 and `05-open-decisions.md` (design), plus `status.md` (the
dashboard row and the phase narrative — status-only, no design text). The reviewer's round-1 text (findings,
evidence, verdict, checklists) is unchanged. M-1 is resolved **inside** the existing boundary — the preview no
longer promises an element the API cannot supply — so there is no scope, dependency, or design-class change and no
new public API. Per the Full stop rule the Minor and Note findings do not by themselves justify a new round; they
are resolved or accepted in `Resolution status` because the commit was required for M-1 anyway.

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
- **Reconciliation of the indicative design.** D-2 (site deletion is blocked with a dependents list, and the only
  cascade is the energy-reference-only "delete site and energy references"), D-3 (set detail owns its drag
  source), D-4 (validator on the detail, not the list base), D-5 (dummy positions as their own document; P1
  preview as a panel toggle, not a tab), D-6 (no trigonometric functions in the expression in v1), D-7
  (document-local gestures), D-9 (one global `editorFraction`), D-10 (1,000-site virtualization acceptance).
- **Scope control.** No transition editor, no broad cascade delete (only the energy-reference-only site action),
  no energy/lattice/simulation/3D UI, no format or diagnostic-code change, no new dependency. One additive Core
  API and one additive app-local setting only.
- **Evidence plan.** §13 names the tests and tiers for every acceptance criterion, including the 1,000-site and
  >1,000-P1-position virtualization cases, the two-culture expression tests, the lock-table-versus-`CellGeometry`
  cross-check, and the real ceria acceptance test.

## Known limitations and deliberate omissions

- **Site deletion is blocked except for the energy-reference-only action (D-2).** The indicative design offered
  general cascade removal for a referenced site; 24A instead blocks the delete with a dependents list and
  navigation. The single exception is a site whose only dependents are energy references, where the editor offers
  "delete site and energy references" through the existing `ProjectEditor.RemoveCellSiteAndEnergyReferences`. There
  is no cascade for MMC transitions, KMC resolved paths, occupation exchanges, or dopings. This narrowing plus the
  one bounded cascade is the revised D-2 the owner confirms at the start gate, not an oversight.
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

- [x] Step page design decisions — Reviewed the goal, scope, non-goals, §1–§18, the D-1…D-10 table, the
      acceptance criteria, and the six-feature plan. Findings m-1–m-10 and n-1–n-2; M-1 on the P1 preview API.
- [x] Data contracts and format changes — No `.mocprj`/`.msl`/`.msr`/`.mcs`, schema, manifest, CLI, or
      diagnostic-code change; `contracts/manifest.json` is untouched and no version bump is required. The only
      public-surface change is the additive `ProjectDocument.Apply` overload (m-7).
- [x] Architecture and module boundaries — Checked the 23A/23B seams against the delivered code:
      `DocumentKinds`, `INavigationTargetProvider`, `IExplorerNodeProvider`, `IInspectorContributor`,
      `ICommandCatalog`/`MenuKind.Model`, `ProjectEditCoordinator`, `ValidationCoordinator`,
      `LayoutSettings`/`AppSettingsValidator`, `DocumentViewModel.OnClosingAsync`, and `ViewLocator` all exist
      and are used as described. The `Mocassin.App.ViewModels` → `Mocassin.Symmetry` reference and the
      `IStructurePreviewService`/`IStructureValidator` registration are not stated (n-1).
- [x] ADRs and open decisions — ADR 0003's layout decision is consistent with §1/§3 and its `Avoid` list; the
      `05-open-decisions.md` rows for the layout, the split, the framework, the edit path, the site cascade, and
      the 24A/24B start gates are present. ADR measurement claim m-12.
- [x] Quality strategy and test plan — §13's tiers and the real-object/headless rules checked against
      `03-quality-strategy.md` §11.1; the PR-fast timing row conflicts (m-5) and the read-only criterion has no
      named test (m-4).
- [x] Parity matrix and documentation plan — §12's five pages all exist (`user-guide/particles.md`,
      `user-guide/structure.md`, `development/ui-architecture.md`, `development/ui-interaction-contract.md`,
      `troubleshooting/errors.md`). The parity matrix still maps the split domains to "24" (n-2, pre-audit
      bookkeeping).
- [x] Licensing and provenance — No package or dependency change (`Directory.Packages.props` untouched); no
      GPL code or data is copied; the POC report is not on `main`/the docs branch (m-11).
- [x] Post-implementation audit plan — §18 records the closeout, the pre-audit gate, the Windows/macOS platform
      gate for keyboard/focus/drag-and-drop/screenshots, and the finding-origin rule; consistent with the audits
      README.

## Dependency and interface checks

- [x] Dependencies are `Done` and their contracts are stable — 02–06 and 23A/23B are `Done`/tagged. Verified the
      APIs the design calls: `ProjectEditor` (`AddParticle`, `UpdateParticle`, `RemoveParticle`, `AddParticleSet`,
      `UpdateParticleSet`, `RemoveParticleSet`, `SetSpaceGroup`, `SetCellParameters`, `AddCellSite`,
      `UpdateCellSite`, `RemoveCellSite`, `AddDummyPosition`, `UpdateDummyPosition`, `RemoveDummyPosition`),
      `IStructurePreviewService.TryCreatePreview`, `IStructureValidator.Validate`, `CellGeometry.TryCreate`,
      `StructurePreview` (`PositionList`, `CartesianPositions`, `CellVolumeAngstrom3`, `Orbits`),
      `ModelStatistics`, `ProjectEditor`, and the `ModelPath.Domain` values (`particle`, `particleSet`,
      `structure`, `cellSite`, `cellDummyPosition`). `StructureModel.MaxReferenceSites` = 1,000, `STR012`, and
      `TRN019` exist as the page states.
- [x] Inputs, outputs, types, and formats are defined — `Project` snapshots in, `ProjectEditResult`/diagnostics
      out; the one additive API, the app-local setting, the new view-model types, and the five document kinds
      are specified. One interface gap remains: the P1 preview's "generating operation" has no preview API and
      would contradict "no other library change" (M-1).
- [x] Versioning and migration behavior is defined — No persisted format change; the absent `editorFraction`
      reads the default and needs no `defaultsRevision` bump, as `AppSettingsValidator` already clamps the shell
      fractions.
- [x] No unresolved cross-step interface question remains — One remains (M-1). The 24B compile-preview API is
      out of scope and correctly deferred to the 24B freeze.

## Standard design-review checklist

Use this full checklist for a `Full` step. A `Light` step answers the five design questions only.

- [x] Design class (`Full` or `Light`) is recorded and matches the triggers — **Full**; the three triggers
      (the editor-framework architecture boundary, the irreversible ADR 0003 layout, and the public Core API)
      are recorded in §16 and match `design-proportionality.md` §1.
- [x] Scope, non-goals, and acceptance criteria are explicit and testable — Eleven criteria, all testable in
      principle. The read-only criterion has no named test (m-4) and the ceria acceptance fixture already
      contains the model it is asked to "create" (m-3).
- [x] Complexity class and split decision are recorded and justified per
      [`04-roadmap.md` §3.1](../04-roadmap.md#31-step-sizing-and-split-triggers) — **High** on cross-step
      interfaces and test-matrix breadth; owner acceptance as one unit is required at the start gate; no further
      split is justified (§16). The split is recorded in roadmap §3.1, the roadmap table, the dashboard, and the
      README index.
- [x] Design decisions are complete and internally consistent — D-1…D-10 reviewed. D-2's premise is inaccurate
      (m-2), the structure-summary statistic is undefined (m-1), the expression lexer is ambiguous (m-6), and the
      bulk-operation undo claim is unstated (m-8).
- [x] Required tests, property tests, golden fixtures, and performance targets are defined — §13 covers all five
      domains, the two POC findings, the expression/culture and lock-table property tests, the real ceria
      acceptance test, screenshots, and performance. Gaps: m-4 (read-only), m-5 (PR-fast timing), M-1 (no test
      for the promised operation).
- [x] Documentation, status, parity, and audit updates are planned — §12 pages exist and are named; the
      dashboard and step page are updated; the parity mapping is deferred to feature 6 (n-2); §18 plans the
      audit and platform gate.
- [x] Licensing, provenance, and dependency checks are resolved — No package, no dependency, no GPL content;
      `Directory.Packages.props` unchanged. The layout evidence is only on an unmerged branch (m-11).
- [x] Risks and mitigations are realistic — §15 covers table drift, expression/locale bugs, preview performance,
      drag-and-drop accessibility, blocked deletes, layout re-litigation, CLI divergence, and settings churn.
      The remaining risks are the API/evidence findings above, not missing mitigations.
- [x] Code layout and namespace plan follow the structure policy — `Mocassin.App.ViewModels/Editors/**` and
      `Mocassin.App.Views/Editors/**` with the `ViewLocator` convention match `code-structure.md`; `Editors` is
      not a forbidden folder and the 20-file limit is handled by subfolders. Subfolder layout and file counts are
      a structure-gate item per the review README, not this review.
- [x] No hidden scope, stale placeholders, or unowned decisions — Scope and owner inputs are explicit (§17);
      the 24B compile-preview API is deferred to the 24B freeze; the freeze itself is docs-only (eight files).

## Evidence

**Frozen state reviewed.** `b98a56f824002bc3cdbb4ace9e0b5cf9dd464ba9` on `docs/step-24-design`
(the 24A/24B split, the 24A design, the ADR 0003 layout record, the review request, and the roadmap/open-decision/
README/status updates). Branch tip at review time `e64247f`; the two later commits are documentation-only:
`9f1ba4e` fills the request's `Reviewed commit` cell and the 24A page's frozen-SHA status line; `e64247f` adds a
test-planning paragraph to the **24B draft** page (the 1,000-reference-site limit does not bound the 24B tables;
`TRN019` caps transitions at 100) and the request's post-freeze-delta note. Neither changes 24A design text, so the
reviewed commit remains `b98a56f` (README §8.1: recorded as a note, no re-review).

**Files reviewed (frozen commit).**

- `docs/planning/steps/step-24a-model-editors-particles-and-structure.md` (all sections), the superseded
  `step-24-model-editors-particle-structure-transitions.md` (retained §0 gate record and §0.8 outcome), and the
  draft `step-24b-model-editors-transitions.md` (split boundary only).
- `docs/planning/reviews/step-24a.md` (request, scope, checklists, limitations, audit plan).
- `docs/planning/adr/0003-avalonia-mvvm-stack.md` (layout options, the 2026-11-22 decision, `Avoid`, `Rules`),
  `docs/planning/04-roadmap.md` §3/§3.1/§5, `docs/planning/05-open-decisions.md`, `docs/planning/README.md`,
  `docs/planning/status.md`, `docs/planning/appendix-b-parity-matrix.md`, `docs/planning/02-data-contracts.md`,
  `docs/planning/01-target-architecture.md` §3.2, `docs/planning/03-quality-strategy.md` §11/§11.1,
  `docs/planning/audits/README.md`, the POC report
  `docs/planning/maintenance/2026-11-22-step-24-layout-poc.md` (read from `fix/step-24-layout-poc`; not on `main`
  or the docs branch), `NOTICE.md`/`Directory.Packages.props`, `docs/development/code-structure.md`,
  `ui-interaction-contract.md` §6/§8, `ui-visual-contract.md` (gutter, splitter, no colour-only status),
  `design-proportionality.md`, and `model-economics.md` §6.
- Implementation surfaces spot-checked read-only: `Mocassin.Core/Editing/ProjectDocument.cs`,
  `ProjectEditor*.cs`, `ProjectChange.cs`, `ProjectEditResult.cs`, `PublicAPI.Unshipped.txt`;
  `Mocassin.Core/Model/{Particles,Structure,Transitions,Energy,Lattice}/*`;
  `Mocassin.Core/Diagnostics/{CoreDiagnosticCatalog,ModelPath}.cs`;
  `Mocassin.Symmetry/{Structure,Geometry,SpaceGroups,Transitions}/*` (`StructurePreview`, `ExpandedSite`,
  `SiteOrbitInfo`, `StructurePreviewService`, `StructureValidator`, `CellGeometry`, `SpaceGroupCatalog`,
  `SpaceGroupSetting`, `SpaceGroupOperations`); `Mocassin.Packages/Projects/Validation/ModelStatistics.cs`;
  `Mocassin.App.ViewModels/{Documents,Navigation,Editing,Diagnostics,Commands,Settings,Shell}/*`;
  `Mocassin.App.ViewModels.csproj`, `Mocassin.App/Composition/{AppServiceRegistration,AppHost}.cs`,
  `Mocassin.Architecture.Tests/AppBoundaryChecks/*`; `samples/ceria/ceria.mocprj` and
  `samples/blank/blank.mocprj`; `tools/structure-policy.json`.
- Post-freeze commit delta `git diff b98a56f..e64247f` (3 docs files) and the freeze's own changelist
  `git show --name-only b98a56f` (8 docs files; `main` has moved since the branch's merge base, so a naive
  `main..freeze` diff also shows unrelated `.opencode/agents` and other later `main` changes).

**Commands run (worktree `/home/node/.local/share/opencode/worktree/c2378f/step-24-design`, branch
`docs/step-24-design`).**

| Command | Result |
| --- | --- |
| `git log --oneline -4` / `git status --short` | tip `e64247f`; clean tree; freeze `b98a56f` |
| `git diff --stat b98a56f..e64247f` | 3 files, 17 insertions, 5 deletions — documentation only |
| `git show --name-only b98a56f` | 8 files, all under `docs/planning/` — the split, 24A, 24B, ADR-adjacent pages, roadmap/open-decisions/README/status, and this record |
| `git diff --name-only main..b98a56f` | 12 files, but the extra files are later `main` changes (`main` is not an ancestor of the freeze; `ea53c47` post-dates the branch's merge base) — not freeze edits |
| `git ls-tree main docs/planning/maintenance/`; `git ls-tree docs/step-24-design …` | no POC report/PNGs on either branch |
| `git show fix/step-24-layout-poc:docs/planning/maintenance/2026-11-22-step-24-layout-poc.md` | read the POC report (§5/§7/§9/§10) read-only |
| `./tools/verify.sh docs` | **exit 0** — math conventions OK (210 files), links OK (213 files), Zensical strict "No issues found", DocFX 0 warnings/0 errors, rustdoc built |
| `./tools/verify.sh policy` | **exit 0** — ApiCompat "no breaking changes" (all baselined assemblies), contract artifacts current (26 files), model/parameterization/lattice projections OK, space-group catalog current (274 settings, sha256 `1f3bf67d…`), process consistency OK (62 tests), SPDX headers in 1114 files, no secret patterns in 1645 files, cargo-deny advisories/bans/licenses/sources OK |
| Targeted greps of the APIs named in §10/§11 and the domain strings in `ModelPath` | confirmed (all present; see the dependency checklist) |

The docs build does **not** catch m-11: the ADR and step page cite the POC report as plain code spans, not
markdown links, so a missing evidence artefact leaves the link gate green.

**Round-2 evidence (re-review, 2026-11-23).** Frozen design `b75efea4997eab9e89ebe62297efeca1f0c2f26e` on
`docs/step-24-design`; branch and record tip at review time `1eaf720`. The worktree was clean before and after the
gates (`git status --short` empty; the docs build writes only ignored `site/`, `artifacts/`, and `target/`).

| Command (worktree `/home/node/.local/share/opencode/worktree/c2378f/step-24-design`) | Result |
| --- | --- |
| `./tools/verify.sh docs` | **exit 0** — math conventions OK (210 files), links OK (213 files), Zensical strict "No issues found", DocFX 0 warnings / 0 errors, rustdoc built |
| `./tools/verify.sh policy` | **exit 0** — "Public API is compatible with the checked-in baseline"; contract artifacts current (26 files); model/parameterization/lattice projections OK; space-group catalog current (274 settings, sha256 `1f3bf67d…`); process consistency OK (62 tests); SPDX headers in 1114 source files; no secret patterns in 1645 tracked text files; cargo-deny advisories/bans/licenses/sources OK |
| `git diff --stat b98a56f..b75efea` | 7 files, +519/−133, all under `docs/planning/` — no code, test, configuration, contract, generated, or dependency file |
| `git show --name-only b75efea` | 6 files: the 24A page, ADR 0003, `01-target-architecture.md`, `05-open-decisions.md`, `status.md`, this record |
| `git diff b75efea..1eaf720` | 2 files, 2 changed lines: the record's `ROUND2_SHA` placeholder and the step page's freeze SHA — documentation-only, recorded as a note per README §8.1 |
| `git diff 90ac8ab..b75efea -- docs/planning/reviews/step-24a.md` | additions only (request round-2 note, `Record state`, resolution table). No round-1 findings/evidence/verdict/checklist line is modified; the only non-additive changes are the `Record state` value (README §4.6 lets the request owner mark `Re-review required`) and the pre-round-1 `Resolution status` placeholder rows |
| `git diff --stat $(git merge-base main fix/step-24-layout-poc)..fix/step-24-layout-poc` | 16 files: the POC report + 15 PNGs, +354 lines; no code (spike removed at `fbdae6c`); the report is absent from `main` |

Files read at the frozen commit: the 24A page (all sections), ADR 0003, `01-target-architecture.md` §3.2,
`05-open-decisions.md`, `status.md`, the draft 24B page (split boundary only), `reviews/README.md`,
`design-proportionality.md`, `02-data-contracts.md` (no change expected), `03-quality-strategy.md` §11.1,
`audits/README.md`, `code-structure.md`, `ui-interaction-contract.md` §8, `ui-visual-contract.md`,
`appendix-b-parity-matrix.md`, `Directory.Packages.props`, and the POC report from `fix/step-24-layout-poc`.
Read-only code checks are listed per finding in `Re-review round 2` and the closure table there.

## Findings

| Severity | Origin | Finding | Evidence |
| --- | --- | --- | --- |
| **Major** | **design** | **M-1 — The P1 preview promises "the generating operation", but no preview API exposes it and the design forbids adding one.** §6 says "This site's orbit" shows "positions with fractional and Cartesian coordinates and the generating operation". `StructurePreview` carries `PositionList`, `CartesianPositions`, `CellVolumeAngstrom3`, `Orbits`, and `Encoder`; `SiteOrbitInfo` carries reference index, representative, positions, and position indices; `ExpandedSite` carries `P`, `Encoding`, `Fractional`, `ReferenceSiteIndex`, and `OrbitMemberIndex`. None carries the operation, and `OrbitMemberIndex` is the deduplicated orbit index, not an operation index. §10 ends "No other library change", the scope says "One additive Core API", and §11 says no interface question remains, so the dev must either drop the element, add a `Mocassin.Symmetry` API (a scope/class change), or re-derive the exact symmetry mapping in the app — the last contradicts the page's own "exact P1 canonical order from the Step 02 services (never a re-derived order)" rule and AGENTS §5 "scientific algorithms never live in view models". §13's `P1PreviewTests` does not test the operation. Resolve explicitly before implementation (drop the element, or add and record the API) and add the test. | 24A page lines 331–334, 443, 405; `src/dotnet/Mocassin.Symmetry/Structure/StructurePreview.cs:24-32`; `Geometry/SiteOrbitInfo.cs:15-25`; `Geometry/ExpandedSite.cs:15-20`; `SpaceGroups/SpaceGroupOperations.cs` (public methods return positions only) |
| **Minor** | **design** | **m-1 — The structure summary names sources that cannot supply the values, and "particle-number density" is undefined.** §6 promises "P1 position count, stable/unstable site counts, cell volume, particle-number density (`count / volume`)" and §13 asserts "summary counts, volume, and density agree with `ModelStatistics`". `ModelStatistics` has `P1PositionCount`, `StablePositionCount`, `UnstablePositionCount`, `ReferenceSiteCount`, `ParticleCount`, and `ParticleSetCount` — no volume and no density; volume is `StructurePreview.CellVolumeAngstrom3`/`CellGeometry.VolumeAngstrom3`, and the stable/unstable fields are P1 *positions*, not sites. The numerator of the density is not defined (particles? P1 positions? per set?). Under "no silent scientific corrections", either name the sources and define the statistic in §6/§13, or the dev may add a `ModelStatistics` field (an unplanned `Mocassin.Packages` API change). | 24A page lines 323–324, 444, 554; `Mocassin.Packages/Projects/Validation/ModelStatistics.cs:35-51`; `StructurePreview.cs:28`; `CellGeometry.cs:92` |
| **Minor** | **design** | **m-2 — D-2's premise is factually wrong, and the site-dependents list names a dependency that does not exist while omitting the one that does.** D-2 says "`ProjectEditor` has no site-cascade path"; `RemoveCellSiteAndEnergyReferences` exists and removes a site plus its energy references. §6 lists site dependents as "transitions, environments, clusters, dopings" and warns if "a KMC/MMC transition uses the site": `KineticTransition` stores a fractional `ReferencePath` and a `ChainId`, never a site id, and Core explicitly delegates resolved-path rules to `Mocassin.Symmetry.Transitions.TransitionPathValidator`; conversely, `OccupationExchange` holds the `SiteId` that dopings reference and is omitted. If a resolved KMC path point is meant, the block cannot rely on the Core rejection (`ProjectEditor` validates with `ProjectValidator`, which does not run the Symmetry path validator) and the usage index must name the resolver; if not, the warning is vacuous. Correct the premise/list and state the authority for a transition-related block. | 24A page lines 83–85, 337–338, 525; `src/dotnet/Mocassin.Core/Editing/ProjectEditor.Energy.cs:284`; `Editing/ProjectEditor.cs:320`; `Model/Transitions/KineticTransition.cs:17-22`; `Model/Lattice/OccupationExchange.cs:20-27`; `Model/Transitions/TransitionModelValidator.cs:14` |
| **Minor** | **design** | **m-3 — The primary acceptance test cannot demonstrate authoring on the named fixture.** §13's `ModelEditorAcceptanceTests` says "On the real `samples/ceria/ceria.mocprj` through `ProjectStore`, create the ceria particles, sets, structure, and sites without the CLI". That sample already contains 3 authored particles, 3 particle sets, 3 cell sites, 1 dummy position, and space group 225 (plus one implicit void row); running the "create" flow on it hits `PAR001`/`STR005` duplicate rules, and a literal implementation fails. Name the starting state (a new project from `samples/blank/blank.mocprj`, whose `model` is `{}`, or delete-and-recreate each domain) so acceptance criterion 2 proves authoring rather than editing one field. | 24A page lines 434, 570; `samples/ceria/ceria.mocprj` (`particles`=3, `particleSets`=3, `structure.sites`=3, `dummyPositions`=1, SG 225); `samples/blank/blank.mocprj` (`model: {}`) |
| **Minor** | **evidence** | **m-4 — The read-only acceptance criterion has no named test.** Criterion 10 ("Read-only sessions disable every edit path and show the 23B banner") has no row in §13, although 23B's plan carries an explicit read-only row. Add a read-only editor test: Add/Remove/Duplicate/Apply/`Enter` disabled, the banner shown, no coordinator call, no undo entry, no `EditRejected` diagnostic. The request's "Evidence plan" claim that §13 names a test for every criterion is otherwise accurate. | 24A page line 581; §13 table lines 432–449; `docs/planning/steps/step-23b-editing-diagnostics-and-recovery.md` §13 "Read-only" row |
| **Minor** | **design** | **m-5 — A PR-fast test asserts an absolute duration, contrary to the quality strategy.** `EditorPerformanceTests` is tiered PR-fast with "1,000 sites: build+select bounded (record the measured budget in the step page)". `03-quality-strategy.md` §11.1 says "PR-fast tests assert functionality, ordering, and asynchrony, never an absolute duration; timing is recorded as non-gating evidence", and both 23A and 23B pages repeat "no absolute wall-clock duration is asserted in a PR-fast test". Split the row: PR-fast asserts functional properties (bounded realized containers, coalescing, superseded-result discard, no dropped rows); timing stays non-gating recorded evidence, or the row moves to a benchmark/PR-medium tier. | 24A page lines 449, 457–465; `docs/planning/03-quality-strategy.md:732-734`; `step-23a-…md:950-952`; `step-23b-…md:863` |
| **Minor** | **design** | **m-6 — The numeric-expression lexer is ambiguous between the constant `e` and exponent notation.** §4 lists `e` as a constant and says numbers parse with `double.Parse(..., NumberStyles.Float, InvariantCulture)`, which allows exponent notation; the grammar does not say whether `1e5`/`2e-3` are numbers or an error, or how `e` is tokenised next to a digit. `NumericExpressionTests` only covers culture and malformed input. State the lexical rule (numbers first, `e` only as a standalone identifier, no implicit multiplication is a defensible choice) and add `1e5`, `2e-3`, `1e`, `e`, and `2e` cases. | 24A page lines 260–271, 439 |
| **Minor** | **design** | **m-7 — The additive Core API omits the API gate and key undo/validation semantics.** §10 does not mention `Mocassin.Core/PublicAPI.Unshipped.txt` (the analyzer is build-enforced; the existing `Apply` line is listed there) or `./tools/verify.sh api`; 23A/23B design pages state both. The stated semantics also omit clearing the redo stack and applying `MaxUndoDepth`, which the existing `ProjectDocument.Apply` does, and they do not state that the overload records the delegate's `Project` without re-validating — `ProjectDocument`'s own `editor`/validator is bypassed, so the operation must be a `ProjectEditor` typed operation (or the overload should validate against the document's editor). Add the API-gate step, the redo/trim semantics, the returned-change identity, and the precondition. | 24A page lines 384–405; `src/dotnet/Mocassin.Core/Editing/ProjectDocument.cs:14-40,111-140`; `Mocassin.Core/PublicAPI.Unshipped.txt:51`; `step-23b-…md:765-766` |
| **Minor** | **design** | **m-8 — The one-undo-entry claim for bulk operations has no typed operation or composition.** §5 says a bulk "set charge" applies "one undo entry per confirmed batch" and §3 enables multi-select bulk delete, but `ProjectEditor` has no bulk operation and the design says the view model never builds a `ProjectChange`. State that a bulk action is one `Func<Project, ProjectEditResult>` inside the new overload that applies the typed operations to a candidate, how a mid-batch failure is reported, and whether bulk delete is one entry or one per object. | 24A page lines 246–247, 284–285; `Mocassin.Core/Editing/ProjectEditor.cs` (single-object operations only) |
| **Minor** | **design** | **m-9 — `Reset Layout` cannot reach `editorFraction` as described.** §9 says "View → Reset Layout resets it", but `ShellViewModel.ResetLayout()` resets only `Regions.LeftFraction`/`RightFraction`/`ConsoleFraction` and persists that capture; the design does not say where `EditorFraction` lives or how an open editor document observes the reset. State the ownership (shell layout persisted with the region fractions vs per-editor) and the notification/read path. | 24A page lines 376–382; `Mocassin.App.ViewModels/Shell/ShellViewModel.Layout.cs:53-72` |
| **Minor** | **design** | **m-10 — `ProjectChanged` granularity would spuriously invalidate an uncommitted form on save.** §2 says any `ProjectChanged` rebuilds rows and, for an uncommitted form, shows "The project changed" with Revert/Apply. The coordinator raises `ProjectChanged` for `Edited`, `Undo`, `Redo`, `Saved` ("values are unchanged"), and `Replaced`. A save while a form is open would warn although the content did not change; a content change in another editor should refresh reference options without necessarily invalidating the form. State which kinds invalidate a form and which only refresh options. | 24A page lines 210–213; `Mocassin.App.ViewModels/Editing/ProjectChangeKind.cs`; `Editing/ProjectEditCoordinator.cs` (events) |
| **Minor** | **evidence** | **m-11 — The layout decision's durable evidence is not on `main` or the docs branch.** ADR 0003 and the 24A page call `docs/planning/maintenance/2026-11-22-step-24-layout-poc.md` (and the 15 PNGs) the durable evidence and say they land on `main` through the spike branch's owner-approved `--no-ff` merge; no such merge exists at the freeze. The spike branch now contains only docs + PNGs (the spike code was removed at `fbdae6c`), so landing it is a documentation merge. Until then a fresh auditor reading `main`/the step branch cannot see the decision input; the docs gates pass because the references are code spans. Land the branch before the start gate or record the branch-only precedent and SHA (as `fix/gui-palette-freeze` was recorded in `status.md`). | `git ls-tree main docs/planning/maintenance/`; `git ls-tree docs/step-24-design …`; `git diff --stat main...fix/step-24-layout-poc`; ADR 0003 lines 103–110; 24A page lines 40–41; POC §13 |
| **Minor** | **evidence** | **m-12 — ADR 0003's "second-fastest" measurement claim is wrong.** The ADR says option 1 had "the second-fastest 1,024-row build (84 ms)"; the POC §7 table records 84 ms for option 1 and 29/58/73 ms for options 4/3/5, i.e. 84 ms is the slowest non-tree option. The POC §9 recommendation repeats "the second-fastest". The decision reasons (marginal size and shared behaviour) are unaffected; correct the ranking or drop it so the normative ADR carries accurate evidence. | `docs/planning/adr/0003-avalonia-mvvm-stack.md:72`; POC §7 `Build + pump 1,024 rows` row and §9 item 1 |
| **Note** | **design** | **n-1 — The ViewModels→Symmetry reference and the preview/validator DI registration are unstated.** §6 calls `IStructurePreviewService.TryCreatePreview`/`IStructureValidator.Validate`/`CellGeometry`/`StructurePreview` from `Mocassin.App.ViewModels`, which today has direct references to Contracts/Packages/Results only (Symmetry arrives transitively through Packages→Compilation). `01-target-architecture.md` §3.2 lists ViewModels' libraries as Packages/Results/Contracts/Core. `AppServiceRegistration.AddMocassinViewModels` exists as stated, but the Symmetry services are not registered for the app. State whether 24A adds a direct `Mocassin.Symmetry` reference and registers the services in `AddMocassinModelEditors`, and update §3.2 if a direct reference is intended. | 24A page lines 305–322, 399–405; `src/dotnet/Mocassin.App.ViewModels/Mocassin.App.ViewModels.csproj`; `docs/planning/01-target-architecture.md` §3.2; `Mocassin.App/Composition/AppServiceRegistration.cs:69` |
| **Note** | **documentation** | **n-2 — The parity matrix was not mapped to the 24A/24B split.** Rows for the particle/set editor, structure editor, transition editor, space-group operations, cell locks, P1 preview, dummy positions, state changes, groups, chains, and reference paths still say "24". Feature 6 plans "parity/status updates", but the split means the matrix cannot tell which half delivers each item. Per the reviews README this is bookkeeping for the pre-audit gate, not a design blocker; map the rows to 24A/24B before closeout. | `docs/planning/appendix-b-parity-matrix.md:47-78,233-235`; 24A page line 555 |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. Origin: **design**, **implementation**,
**evidence**, or **documentation**. The review agent writes the findings and verdict directly. The dev adds
resolutions in the separate `Resolution status` section without altering the review agent's findings, evidence,
or verdict.

## Record state

**State:** Ready with notes — round-2 verdict (see `Re-review round 2`); the human owner's acceptance is pending.
Round 1's `Changes required` verdict is preserved in `Review round 1`.

`Ready with notes` is the round-2 design verdict; it is not an implementation-start record and creates no step
branch.

## Reviewer independence

> I am a fresh context. I did not author or implement the reviewed work and have no shared conversation with the
> design agent, dev, or lead.

## Verdict

- [ ] Ready for implementation
- [ ] Ready with notes
- [x] Changes required
- [ ] Waived

Follow-up owner: Design agent for the design-origin findings (M-1 and m-1…m-10, n-1); the dev owns the
resolutions at the start gate and the POC landing (m-11) before closeout.
Follow-up step: Step 24A — a new commit with the M-1 resolution is required; a fresh review agent then appends
round 2. The Minor and Note findings do not by themselves justify another round (reviews README §7).

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

The request owner fills this section after the verdict. Do not alter the review agent-authored findings, evidence,
or verdict.

**Round-2 resolution commit:** `b75efea4997eab9e89ebe62297efeca1f0c2f26e` (the design re-freeze; recorded by the follow-up status commit).

| Finding | Severity | Resolution | Status | Owner |
| --- | --- | --- | --- | --- |
| **M-1** | **Major** | The P1 preview now promises **positions and indices only**: each orbit row shows the fractional and Cartesian position, the canonical `P` index and the orbit-member index. No Symmetry API is added; the design explicitly forbids re-deriving the operation in the app (it would duplicate Step 02 exact symmetry, contrary to §1, `01-target-architecture.md` §3.2 and `AGENTS.md` §5). A labelled operation is deferred to a later step that opens a preview API. `P1PreviewTests` asserts the row indices equal `StructurePreview.Orbits[i].PositionIndices` / `ExpandedSite.OrbitMemberIndex` and that the row projection has no operation field; a new acceptance criterion states it. §11 now records that no interface question remains. **No scope or class change, no new public API — the hard stop was not triggered.** | Fixed | Design agent |
| **m-1** | Minor | The structure summary names real sources and defines the statistic: reference-site count from `StructureModel.Sites`; P1 count and stable/unstable **position** counts from `StructurePreview.PositionList` (labelled positions, never sites); volume from `StructurePreview.CellVolumeAngstrom3`; orbit table from `StructurePreview.Orbits`; **reference-site density** `= sites / volume` (Å⁻³). Particle-number density is a lattice/simulation quantity and is not shown; `ModelStatistics` is not called (23B keeps statistics on demand). `StructureSummaryTests` compares against the recomputed preview values. | Fixed | Design agent |
| **m-2** | Minor | **D-2 re-resolved.** The false premise is corrected: `RemoveCellSiteAndEnergyReferences` exists, but only for energy references. The dependents are now exactly MMC transitions (`SiteAId`/`SiteBId`), KMC transitions through their resolved reference path (`TransitionPathResolver` → owning `CellSiteId`), occupation exchanges and their dopings, and the energy references. The default delete is blocked; the **only** cascade offered is "delete site and energy references", enabled solely when the remaining dependents are energy references. The usage index is the blocking authority because the structural validator does not check MMC site ids or KMC paths (`TRN016`/`TRN020`/`TRN012` are Symmetry rules that run only in the full pipeline); the Core rejection remains the backstop where it checks (occupation exchanges, Core-owned energy references) and a test asserts they agree. The KMC/MMC stability warning is retained with its resolution path named. **D-2 outcome for the owner: a blocked delete plus one bounded energy-reference cascade; no broader cascade; the owner confirms this at the start gate.** | Fixed | Design agent |
| **m-3** | Minor | The acceptance test starts from an **empty project** (`samples/blank`) and authors the ceria model from scratch, comparing the authored values to `samples/ceria` by name/value (ids differ); a second case edits one field on the populated ceria sample. Acceptance criterion 2 now says "from an empty project". | Fixed | Design agent |
| **m-4** | Minor | Added `EditorReadOnlyTests`: Add/Remove/Duplicate/Apply/`Enter` disabled, banner shown, no coordinator call, no undo/dirty change, no `EditRejected`, and a forced `Apply` returns `ReadOnly`. The read-only acceptance criterion names the test. | Fixed | Design agent |
| **m-5** | Minor | `EditorPerformanceTests` is **functional only** (bounded realized containers independent of row count, all rows present, coalescing and superseded-result discard with a fake clock, no dropped rows). Timing is moved to `EditorPerformanceEvidence` as non-gating recorded evidence per `03-quality-strategy.md` §11.1; §14 states no PR-fast test asserts a duration. | Fixed | Design agent |
| **m-6** | Minor | The lexing rule is stated: exponent notation belongs to a number and requires exponent digits (`1e5`, `2e-3` are numbers; `1e`, `2e` are malformed, not `2 * e`); the identifier `e` is the constant only when not preceded by a digit sequence; there is no implicit multiplication (`2 pi` is an error). Test cases `1e5`, `2e-3`, `1e`, `e`, `2e`, `2 pi` are added. | Fixed | Design agent |
| **m-7** | Minor | §10 now requires `Mocassin.Core/PublicAPI.Unshipped.txt` and `./tools/verify.sh api`; it states the redo-clear and `TrimUndoStack()` (`MaxUndoDepth`) semantics, the change identity (`Before` = the document value, `After` = the result project, the caller's description wins), and a structural precondition: the delegate now receives the document's **own `ProjectEditor`** (`Func<ProjectEditor, Project, ProjectEditResult>`), with identity guards and a test for the rejected-operation path. Feature 1's done-when includes `verify.sh api`. | Fixed | Design agent |
| **m-8** | Minor | A bulk action is **one composed delegate** that folds the typed single-object operations over a candidate project, returns one `ProjectChange`, and is all-or-nothing: a mid-batch rejection changes nothing and names the failing object. Bulk delete (n objects), bulk set-charge and multi-select delete are each one undo entry; `EditorUndoTests` asserts both the one-entry behaviour and the mid-batch rollback. | Fixed | Design agent |
| **m-9** | Minor | `editorFraction` is **shell-owned layout state** with one serialized writer: it is captured by `CaptureLayout()`, applied by `ApplySettings`, reset by `ResetLayout()` and persisted by the existing region-property hook; editor documents observe the shared value and never write settings themselves. §9 states the ownership, the reset path and the observation path; a shell region change cannot drop the field. | Fixed | Design agent |
| **m-10** | Minor | `ProjectChanged` is handled by kind (D-14): `Saved` never rebuilds or invalidates a form; `Edited`/`Undo`/`Redo` rebuild rows, refresh options, and compare the open object by id to decide between "keep", "conflict" and "removed"; `Replaced` discards forms and rebuilds. `EditorRefreshTests` covers each case. | Fixed | Design agent |
| **m-11** | Minor | Recorded as merge sequencing: the POC report and PNGs are branch-only today; the `fix/step-24-layout-poc` branch is now documentation-and-assets only (spike code removed at `fbdae6c`) and lands on `main` through its owner-approved `--no-ff` merge. **ADR 0003 and the 24A page now state that the 24A start gate is conditional on that merge**, and the pre-audit gate re-checks it. No merge was performed by the design agent. | Fixed by sequencing | Lead/owner (merge), dev (pre-audit check) |
| **m-12** | Minor | ADR 0003 no longer claims "second-fastest": it states the measured 84 ms as within the 29–84 ms non-tree band and about 18× faster than the 1,521 ms tree. The decision reasons are unchanged. The POC record's own phrasing is branch-only evidence and was not edited. | Fixed | Design agent |
| **n-1** | Note | `Mocassin.App.ViewModels` gains an **explicit `Mocassin.Symmetry` project reference** (in-repo, not a package), `01-target-architecture.md` §3.2 is updated, and `AddMocassinModelEditors` registers `IStructurePreviewService`/`IStructureValidator` as singletons; the usage index uses `TransitionPathResolver`. The page §1 and §11 state the reference, registration and the "consume, never own an algorithm" rule. | Fixed | Design agent |
| **n-2** | Note | Accepted as pre-audit bookkeeping: feature 6 now includes mapping the parity-matrix rows to the delivering half, and the pre-audit gate checks it. The mapping is recorded for the dev: 24A — drag-and-drop set membership, space-group selection/operations, cell locks, P1 preview, dummy positions, numeric expression, particle/set editor, structure editor, and the particle/structure domain providers; 24B — state changes, groups, chains, reference paths, transition rule-count preview, transition editor, and the transition domain providers. | Accepted (deferred) | Dev (pre-audit) |

## Review rounds

Append each new round below. Never overwrite an earlier round.

### Review round 1

#### Independence and frozen state

I am a fresh review context. I did not author or implement the reviewed work and have no shared conversation with
the design agent, the dev, the lead, or any other review agent. I was spawned by the lead with the frozen commit,
this record, and the repository; no desired verdict was supplied. My model is DeepSeek V4.1 Flash 1017
(`opencode-go/deepseek-v4.1-flash`).

I reviewed `docs/step-24-design` at the frozen commit `b98a56f824002bc3cdbb4ace9e0b5cf9dd464ba9`. The branch tip
at review time was `e64247f`. The two commits after the freeze are documentation-only and I verified their diff:

- `9f1ba4e` — fills the request's `Reviewed commit` cell and the 24A page's frozen-SHA status line;
- `e64247f` — adds a test-planning paragraph to the **24B draft** page (the 1,000-reference-site limit does not
  bound the 24B tables; `TRN019` caps transitions at 100) and the request's post-freeze-delta note.

The 24A design text is unchanged, so the review remains at `b98a56f`; per `reviews/README.md` §8.1 this is
recorded as a note, not a re-review. The 24B page is out of scope beyond the split-boundary check, which is
dependency-clean: 24A contains no state-change/chain/KMC/MMC/compile-preview task, and 24B consumes only the
framework, pickers, expression, usage index, and coordinator delivered by 24A feature 1.

The freeze itself is docs-only: `git show --name-only b98a56f` lists eight files under `docs/planning/`. A naive
`git diff main..b98a56f` shows unrelated `.opencode/agents` edits only because `main` moved at `ea53c47` after the
branch's merge base; they are not part of the freeze and do not affect the reviewed design.

#### Commands and results

Run in this worktree on `docs/step-24-design`:

- `./tools/verify.sh docs` → **exit 0**. Math conventions OK (210 files), link check OK (213 files), Zensical
  strict build "No issues found", DocFX 0 warnings / 0 errors, rustdoc built.
- `./tools/verify.sh policy` → **exit 0**. ApiCompat "no breaking changes" for every baselined assembly;
  contract artifacts current (26 files); model/parameterization/lattice projections OK; space-group catalog
  current (274 settings, sha256 `1f3bf67d…`); process consistency OK (62 tests); SPDX headers in 1114 source
  files; no secret patterns in 1645 tracked text files; cargo-deny advisories/bans/licenses/sources OK.
- `git log --oneline -4` → `e64247f`, `9f1ba4e`, `b98a56f`, `cbf9f2d`; `git status --short` clean.
- `git diff --stat b98a56f..e64247f` → 3 files, 17 insertions, 5 deletions (documentation only).
- `git show --name-only b98a56f` → 8 files, all under `docs/planning/`; the 12-file `main..b98a56f` diff also
  contains later `main` changes (`ea53c47`) because `main` is not an ancestor of the freeze.
- `git show fix/step-24-layout-poc:docs/planning/maintenance/2026-11-22-step-24-layout-poc.md` → POC report read
  read-only (§5/§7/§9/§10); `git ls-tree` shows it is not on `main` or the docs branch.
- Targeted read-only checks of the APIs the design names (`ProjectEditor` operations, `IStructurePreviewService`,
  `IStructureValidator`, `CellGeometry`, `StructurePreview`/`SiteOrbitInfo`/`ExpandedSite`, `ModelStatistics`,
  `ModelPath` domains, `DocumentKinds`, the navigation/explorer/inspector/command seams, `LayoutSettings`,
  `ProjectChangeKind`, `ResetLayout`, `ViewLocator`) and of `samples/ceria` / `samples/blank`.

The full list is in `Evidence`.

#### Findings

No Blocker. One Major remains:

- **M-1 (Major, design)** — the P1 preview promises "the generating operation", but no preview API exposes it,
  the design forbids any other library change, and deriving it in the app re-derives exact symmetry contrary to
  the page's own rule and AGENTS §5. Resolve explicitly (drop the element or add and record the API) and test it.

Twelve Minor and two Note findings are recorded in the findings table (m-1…m-12, n-1, n-2): structure-summary
sources/definition, the D-2 premise and site-dependents list, the ceria acceptance fixture, the missing read-only
test, the PR-fast timing assertion, the expression lexer, the Core API gate/undo/trust semantics, bulk-operation
undo composition, `editorFraction` reset wiring, `ProjectChanged` granularity, the branch-only POC evidence, the
ADR "second-fastest" claim, the ViewModels→Symmetry reference/DI, and the parity-matrix split mapping. Per the
review stop rule they are accepted or deferred and do not by themselves justify another round; they should be
fixed or explicitly accepted with the M-1 resolution.

#### Verdict

**Changes required.** The design is well scoped, the split boundary is dependency-clean, the dependencies and
domain strings match the delivered code, both required documentation gates pass, and the test plan is unusually
complete. It cannot start as written because M-1 leaves a promised structure-preview element without a data
source while the design forbids the library change that would supply it, and §11 claims no interface question
remains. Fix M-1 on a new commit (and, while doing so, address or explicitly accept m-1…m-12/n-1/n-2); a fresh
review agent then appends round 2. The human owner approves the final verdict and the start gate.

### Re-review round 2

#### Independence and frozen state

I am a fresh review context. I did not author or implement the reviewed work and have no shared conversation with
the design agent, the dev, the lead, or the round-1 review agent. I was spawned by the lead with the frozen commit,
this record, and the repository; no desired verdict, summary, or finding list was supplied. My model is DeepSeek
V4.1 Flash 1017 (`opencode-go/deepseek-v4.1-flash`).

I reviewed the design at `b75efea4997eab9e89ebe62297efeca1f0c2f26e` on `docs/step-24-design` (the round-2
re-freeze: the 24A page corrections plus ADR 0003, `01-target-architecture.md` §3.2, `05-open-decisions.md`, and
`status.md`). Branch and record tip at review time is `1eaf720`; `git diff b75efea..1eaf720` is two changed lines
in two documentation files — the record's `ROUND2_SHA` placeholder and the step page's freeze SHA — so the design
text under review is `b75efea`, and the tip delta is recorded as a note (README §8.1).

Round 1's text is unchanged. `git diff 90ac8ab..b75efea -- docs/planning/reviews/step-24a.md` contains only
additions to the request-side notes, the `Record state` value (README §4.6 lets the request owner mark
`Re-review required`), and the dev-owned `Resolution status` table; no round-1 finding, evidence line, verdict
sentence, or checklist note was edited or removed. Round 1's verdict is preserved verbatim in `Review round 1`.

The round-2 delta is documentation-only: the seven files in `b98a56f..b75efea` are all under `docs/planning/`, and
`git diff b75efea..1eaf720` is documentation-only. `contracts/manifest.json`, `Directory.Packages.props`, the
solution, and every test project are untouched, so the "no format, dependency, or contract change" claim in the
request still holds. The re-freeze does not move the design class: M-1 is resolved by dropping an element, the
`Mocassin.Symmetry` reference is an in-repo project reference to a merged step, and the only public surface added
is still the one additive Core overload.

#### Round-1 findings: closure verification

| Finding | Round-2 status | Verification |
| --- | --- | --- |
| **M-1** (Major, design) | **Closed** | §6 lines 399–406 and D-11 (line 674) drop the generating-operation label; acceptance criterion 6 (lines 724–726) and `P1PreviewTests` (line 574) assert the `P` and orbit-member indices and the absence of an operation field; §11 lines 545–549 record that no interface question remains. I re-verified that the API genuinely cannot supply it: `StructurePreview` carries positions, Cartesian positions, volume, orbits, and the encoder (`StructurePreview.cs:24–32`); `SiteOrbitInfo` carries the representative, positions, and position indices (`SiteOrbitInfo.cs:15–25`); `ExpandedSite` carries `P`, encoding, fractional values, reference index, and `OrbitMemberIndex` (`ExpandedSite.cs:15–20`). `SpaceGroupSetting.Operations` lists the group operations only, and mapping a member to its generating operation in the app would re-derive exact symmetry, which §1, `01-target-architecture.md` §3.2, and `AGENTS.md` §5 forbid. Dropping the element is the sound resolution; no new API, scope, or class change. |
| **m-1** (Minor, design) | **Closed** | The named sources exist and the statistic is defined. `StructureModel.Sites` is `ImmutableArray<CellSite>` (`StructureModel.cs:27`); the preview builds its references from `Sites` in order, so `ExpandedSite.ReferenceSiteIndex` indexes `Sites`; `ExpandedSite` carries no stability, and the summary therefore reads `Sites[ReferenceSiteIndex].Stability` (`CellSite.cs:20–25`, `SiteStability`); volume comes from `StructurePreview.CellVolumeAngstrom3`; `Orbits` groups the expanded sites by `ReferenceSiteIndex` (`UnitCellVectorEncoder.cs:242–254`), so `Orbits.Length` does equal `Sites.Length`; reference-site density is defined as sites/volume. `ModelStatistics` is not used. |
| **m-2** (Minor, design) | **Closed except for m-15** | `RemoveCellSiteAndEnergyReferences` exists and removes the site plus stable filters, unstable environments and their filters, clusters, and site energies (`ProjectEditor.Energy.cs:284`, `:338–365`). `MetropolisTransition` carries `SiteAId`/`SiteBId` (`:22–27`); `KineticTransition` carries only `ReferencePath`/`ChainId` (`:20–26`); no Core transition validator reads either site id (`grep` over `Mocassin.Core/Model/Transitions` finds only the record declarations). `TransitionPathResolver.TryCreate`/`TryResolve` resolve path points to the owning `CellSite` (`TransitionPathResolver.cs:50–88`, `:170–230`), and `ResolvedPathPosition` exposes `SiteId`. `OccupationExchange` carries `SiteId` (`:20–27`), validated by `LatticeModelValidator` (`:85–110`); `EnergyModelValidator` validates the unstable-environment `SiteId` (`:111`) and the filters (`:175`). The "index is the blocking authority, Core is the backstop" claim holds. The one residual is the enumeration gap in m-15. |
| **m-3** (Minor, evidence) | **Closed** | `samples/blank/blank.mocprj` has `model: {}`; `samples/ceria/ceria.mocprj` already contains 3 particles, 3 sets, 3 sites, 1 dummy, and SG 225. Criterion 2 now says "from an empty project", and §13 keeps the populated-project edit as a second case. |
| **m-4** (Minor, evidence) | **Closed** | `EditorReadOnlyTests` (§13) covers Add/Remove/Duplicate/Apply/`Enter` disabled, the banner, no coordinator call, no undo/dirty change, no `EditRejected`, and a forced `Apply` returning `ReadOnly`; criterion 11 names the test. `ProjectEditCoordinator.Apply` returns `EditApplyOutcome.ReadOnly` before any write (`ProjectEditCoordinator.cs:91–99`). |
| **m-5** (Minor, design) | **Closed** | `EditorPerformanceTests` is functional only, timing moved to the non-gating `EditorPerformanceEvidence` row, and §14 states that no PR-fast test asserts a duration. Consistent with `03-quality-strategy.md` §11.1. |
| **m-6** (Minor, design) | **Closed** | §4 states the number grammar, the malformed `1e`/`2e` rule, the standalone `e` constant, and the absence of implicit multiplication; `NumericExpressionTests` lists `1e5`, `2e-3`, `1e`, `e`, `2e`, `2 pi`. |
| **m-7** (Minor, design) | **Closed** | §10 adds the `PublicAPI.Unshipped.txt` + `verify.sh api` gate, the redo-clear and `TrimUndoStack()`/`MaxUndoDepth` semantics, the returned-change identity, and the structural precondition. Verified: `ProjectDocument` stores `editor` privately (`ProjectDocument.cs:24`), the existing `Apply` records one `ProjectChange(description, Project, result.Project)` and trims/clears (`:111–140`), `TrimUndoStack` honours `MaxUndoDepth` (`:163–175`), `MaxUndoDepth` is init-only (`:73–93`), and `PublicAPI.Unshipped.txt:51` lists the existing overload. |
| **m-8** (Minor, design) | **Closed in substance** | D-13 states one composed delegate, one undo entry, all-or-nothing, and names the mid-batch rollback and the failing object. The composed result's interface is the residual m-13. |
| **m-9** (Minor, design) | **Closed** | §9 states shell ownership, `CaptureLayout`/`ApplySettings`/`ResetLayout`, and the region-property hook. Verified in `ShellViewModel.Layout.cs:20–45` (hook), `:56–72` (reset), `:82–104` (capture), `:110–127` (apply) over `ShellRegionsViewModel`; `AppSettingsValidator` normalises the fractions (`:36–77`, `:156–170`). |
| **m-10** (Minor, design) | **Closed** | D-14 matches `ProjectChangeKind` exactly (`ProjectChangeKind.cs:8–34`); `NotifySaved` raises `Saved` with equal previous/current values (`ProjectEditCoordinator.cs:175–180`); `ReplaceDocument` raises `Replaced` (`:188–197`). |
| **m-11** (Minor, evidence) | **Closed by recorded sequencing** | The `fix/step-24-layout-poc` delta from its merge base is the POC report plus 15 PNGs (+354 lines) with no code (spike removed at `fbdae6c`); the report is absent from `main`. ADR 0003 and the step page now make the 24A start gate conditional on that documentation-and-assets merge, and §18's pre-audit gate re-checks it. |
| **m-12** (Minor, evidence) | **Closed** | POC §7 records 84 ms (option 1), 1,521 ms (tree), and 58/29/73 ms (options 3/4/5); the ADR now states the 29–84 ms band and ~18× (1521/84 ≈ 18.1) and no longer claims "second-fastest". |
| **n-1** (Note, design) | **Closed** | `01-target-architecture.md` §3.2 now records the direct `Mocassin.Symmetry` reference and the service registrations; `StructurePreviewService` and `StructureValidator` default their optional catalog/version parameters (`StructurePreviewService.cs:36–48`, `StructureValidator.cs:26–29`); `TransitionPathResolver` is a public Symmetry type; `AppBoundaryTests` forbids only Avalonia, `App.Views`, and `App` (`:13–20`), so the project reference does not break the boundary gate. |
| **n-2** (Note, documentation) | **Accepted (deferred)** | `appendix-b-parity-matrix.md` still maps seven rows to "24" and is untouched by the delta; feature 6 and the pre-audit gate own the 24A/24B mapping. Correctly deferred. |

#### Full-checklist re-verification

Round 1's checklist marks apply to the design as a whole and remain satisfied; I re-checked each item against the
round-2 frozen state rather than re-marking round-1 text.

| Checklist item | Round-2 result |
| --- | --- |
| Design-class triggers | **Satisfied** — still `Full` (framework architecture boundary, ADR 0003 irreversibility, public Core overload); §16 records that the round-2 corrections change neither the class nor the scope. |
| Scope, non-goals, testable criteria | **Satisfied** — twelve criteria, each mapped in §13; the added energy-reference cascade is an explicit non-goal exception in the scope and an explicit owner input (§17.3). |
| Complexity class and split | **Satisfied** — `High`, one unit, no further split; owner acceptance still required at the start gate. |
| Design decisions complete and consistent | **Mostly satisfied** — D-11…D-14 added and D-2 corrected; m-13, m-14, and m-15 are residual inconsistencies. |
| Tests, property tests, fixtures, performance | **Satisfied with notes** — every criterion has a named test and tier; m-14 affects the site-overlap test wording; n-5 asks for an explicit Core-overload test row. |
| Documentation, status, parity, audit planning | **Satisfied** — §12 pages exist and are named, the dashboard and step page are updated, parity is deferred to feature 6, §18 plans the audit and platform gate; n-3 is request-text bookkeeping. |
| Licensing, provenance, dependencies | **Satisfied** — no package or dependency change, an in-repo project reference only, no GPL content; `Directory.Packages.props` untouched. |
| Risks and mitigations | **Satisfied** — §15 now covers the resolved-path invalidation risk and the shell-owned fraction; no unmitigated risk is left unowned. |
| Code layout and namespaces | **Satisfied** — `Editors/**` under the existing conventions; no structure-policy change. |
| No hidden scope, placeholders, unowned decisions | **Satisfied** — no 24B task, no format or diagnostic change, the cascade scope is explicit, the 24B compile-preview question stays deferred. |
| Dependencies `Done` and stable | **Satisfied** — 02–06 and 23A/23B verified again on the named APIs; no Step 08–22 use. |
| Inputs, outputs, types, formats | **Satisfied with notes** — the composed-result shape is m-13 and the type spelling is n-4; otherwise complete. |
| Versioning and migration | **Satisfied** — no persisted change; the additive app-local setting reads a default. |
| No unresolved cross-step interface question | **Satisfied** — M-1 is closed and §11 says so; the 24B question is out of scope. |

#### Findings

No Blocker. No Major. Three Minor and three Note findings were introduced by the round-2 delta or left open by it;
each is localized and none is start-blocking.

| ID | Severity | Origin | Finding | Evidence |
| --- | --- | --- | --- | --- |
| **m-13** | **Minor** | **design** | **Bulk composition has no permitted place to build the one `ProjectChange`, and the §10 identity guard rejects a folded result.** §3/D-13 requires the framework to "fold the typed single-object operation over a candidate `Project` … and return one result whose `ProjectChange` spans the original and the final value", while §2, `01-target-architecture.md` §3.2, the 23B page §2, and the `05-open-decisions.md` edit-path row all say no view model builds a `ProjectChange` — the same row states both rules. Mechanically, a fold of typed operations returns a change whose `Before` is the intermediate candidate (`ProjectEditor.Apply` constructs `ProjectChange(description, project, candidate)`), so §10's `ReferenceEquals(result.Change.Before, Project)` check throws `InvalidOperationException` for a legitimately composed bulk result. The only ways out are to construct `ProjectChange(description, Project, final)` in the App framework (needing an explicit exception to the rule) or to let the Core overload synthesise the change and drop the identity checks for composed results. Also unstated: which `Project` a mid-batch rejection returns — the fold's natural error result carries the intermediate candidate, not the document value. Fix one sentence in §10 and align §2/§3.2/05. | 24A page lines 225, 288–291, 504–515; `docs/planning/01-target-architecture.md:214`; `step-23b-editing-diagnostics-and-recovery.md` §2 ("never build a `ProjectChange`"); `05-open-decisions.md:124`; `Mocassin.Core/Editing/ProjectEditor.cs:37–66`; `ProjectDocument.cs:111–140` |
| **m-14** | **Minor** | **design** | **The site-overlap display and apply gate are not implementable as written.** §6 says "Overlap positions are marked with a glyph and text" in the P1 preview panel, but `IStructurePreviewService.TryCreatePreview` returns `null` for any candidate whose encoder reports an overlap, and the encoder fails on the first `STR007` (`bag.HasErrors` → `return false`), so an overlapping candidate has no preview and no rows to mark. Core does not refuse it either: `ProjectEditor.AddCellSite`/`UpdateCellSite` validate with `ProjectValidator`, which runs the Core model validators but not the Symmetry encoder (`ProjectValidator.cs:55–64`), so an overlapping site is accepted. The design names `IStructureValidator` only for the space-group candidate. State that the site form validates the candidate with `IStructureValidator`, refuses Apply on error, and lists the `STR007` diagnostics (the diagnostic carries the `cellSite` path and the related site), and reword the "marked in the preview" sentence. Acceptance criterion 5 stays achievable that way. | 24A page lines 391–398, 407–408; `Mocassin.Symmetry/Structure/StructurePreviewService.cs:143–155`; `Mocassin.Symmetry/Geometry/UnitCellVectorEncoder.cs:180–214`; `Mocassin.Core/Project/ProjectValidator.cs:55–64`; `Mocassin.Symmetry/Structure/StructureValidator.cs:31–56` |
| **m-15** | **Minor** | **design** | **D-2's dependents enumeration omits the unstable-environment object.** §6 lists the energy references as "stable and unstable environment filters, cluster centre sites, site-energy sets". An `UnstableEnvironment` is itself a dependent through its own `SiteId`, and `RemoveCellSiteAndEnergyReferences` removes the whole environment whose `SiteId` matches. Empty filter lists are valid, so a filterless unstable environment at the site is missed by a filter-only scan: the usage index reports no dependents, enables the default delete, and Core then rejects it (the environment `SiteId` is a Core-owned energy reference), while the energy-reference-only cascade that would have been the correct action is never offered. Add the environment objects (stable environment has no site id; unstable environments do) to the enumeration and to `CellSiteEditorTests`. | 24A page lines 409–425; `Mocassin.Core/Model/Energy/UnstableEnvironment.cs:15–18`; `ProjectEditor.Energy.cs:337–349`; `Mocassin.Core/Model/Energy/EnergyModelValidator.cs:88–118`, `:165–176` |
| **n-3** | Note | documentation | **The request's own summary still denies the cascade the design now offers.** The "Reconciliation of the indicative design" bullet and the "Known limitations" section say "D-2 (no cascade delete in 24A)" and "24A blocks the delete with a dependents list and navigation instead", which contradicts the revised D-2, §6, and owner input §17.3 that asks the owner to approve the energy-reference-only cascade. Update the request text (dev-owned) before the start gate so the owner is not asked to approve the cascade against a summary that denies it. | This record lines 116, 120, 128 vs. 24A page lines 87–91, 409–425, 682–686 |
| **n-4** | Note | documentation | **The new overload is spelled with two type names.** §10 writes `Func<ProjectEditor, ProjectModel, ProjectEditResult>` while §3/D-13 and `05-open-decisions.md` write `Func<ProjectEditor, Project, ProjectEditResult>`; `ProjectModel` exists only as a source alias (`using ProjectModel = Mocassin.Core.Project.Project` in `ProjectChange.cs`/`ProjectDocument.cs`), so the `PublicAPI.Unshipped.txt` line will read `Mocassin.Core.Project.Project`. Cosmetic; use the public type name in the sketch. | 24A page lines 289, 496; `05-open-decisions.md:124`; `Mocassin.Core/Editing/ProjectChange.cs:3` |
| **n-5** | Note | evidence | **§10 requires a guard test but §13 has no row for the new Core overload.** §10 says "the dev adds the guard below and a test for the failure path", and the overload has four behaviours worth pinning (accepted edit records the caller's description; operation rejection leaves every stack untouched; no-op returns without a change; a malformed `Before`/`After` identity throws). Add a Core test row (for example `ProjectDocumentApplyOverloadTests`) to §13 so feature 1's evidence is explicit rather than prose-only. | 24A page lines 504–518; §13 table lines 563–583 |

#### Verdict

- No Blocker and no Major remains. Round 1's M-1 and all twelve Minor and two Note findings are closed, verified
  closed, or explicitly accepted with a recorded deferral (n-2). The round-2 delta is documentation-only, the
  design class and scope are unchanged, and the two required gates pass.
- **Ready with notes.** The three Minor findings (m-13, m-14, m-15) and the three Notes (n-3, n-4, n-5) are
  localized; they need an explicit owner acceptance, or a small documentation fix by the design agent/dev before
  the start gate. Per the Full stop rule they do not justify another review round, and none of them blocks
  implementation once accepted.
- The start gate still has the conditions the design records: the human owner accepts class High as one unit, the
  six-feature list, the revised D-2 (blocked site delete plus the energy-reference-only cascade), and the
  feature-branch list (§17.1–§17.4), and the `fix/step-24-layout-poc` documentation-and-assets merge lands the
  POC report on `main` before the step starts (m-11, §18 pre-audit re-check).
- This verdict does not authorize implementation. After the owner's start-gate approval the dev creates the step
  branch from the reviewed `main` commit, not from this docs branch, per `AGENTS.md` §3.9.7. Resolutions for
  m-13…m-15 and n-3…n-5 belong in the dev-owned `Resolution status` table; the review agent's text is unchanged.
- The human owner approves the final verdict and the start gate.
