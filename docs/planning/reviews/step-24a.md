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

**State:** Changes required

`Re-review required` is a record state, not a verdict.

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

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| — | — | — | — |

The dev fills this section after the verdict. Do not alter the review agent-authored findings, evidence, or
verdict.

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
