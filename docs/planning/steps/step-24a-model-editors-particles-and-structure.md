# Step 24A — Shared Editor Framework, Particles, and Structure

**Phase:** F. GUI
**Depends on:** 23B (workspace, editing, diagnostics, recovery) and the merged surfaces of 02 (symmetry preview
and validation services), 03, 04, 05, and 06. Steps 04–06 are consumed **read-only** by the usage index; 24A adds
no transition, energy, lattice, or simulation UI. Not dependent on Steps 08–11 or 13–22.
**Delivers:** The ADR 0003 editor surface and the shared model-editor framework, the particle and particle-set
editors, the space-group and cell-parameter editor with its pre-apply compatibility preview, the cell-site table
with the P1 preview, the dummy-position editor, and the structure summary — all on the 23A/23B shell with undo,
inline validation, diagnostic navigation, and keyboard access.
**Design class:** **Full** — the shared editor framework is the GUI architecture boundary that Step 25 builds on
and Steps 26–28 inherit, the ADR 0003 layout pick is expensive to reverse once the editors exist, and the step
adds one public Core API. No format change and no new dependency. Justification in
[§16](#16-complexity-class-and-split-decision).

> **Split note.** The human owner split Step 24 into **24A** and **24B** on 2026-11-22 under the complexity rule
> (class High, more than one independent acceptance story, an audit surface too large for one sitting). 24A is
> the framework, particle, and structure half; 24B is the transition half. 24A contains no 24B task, and 24B
> consumes the framework delivered here. The superseded unsplit design is
> [`step-24-model-editors-particle-structure-transitions.md`](step-24-model-editors-particle-structure-transitions.md);
> the superseded page keeps the closed layout-gate record and has no review record.

## Implementation status

- **Status:** In audit — features 1–6 squashed (`c24d1cf`, `0c749c2`, `82cd326`, `034981d`, `5128f9f`, `9d0cfd7`); audit round 1 returned **Fixes required** (F-1…F-8) and the fixes are squashed as `10c7206`; round 2 is requested [audit record](../audits/step-24a.md)
- **Step branch:** `step/24a-model-editors-particles-and-structure`
- **Step CI:** Round 2 at `10c7206`: [Ubuntu 45491888826](https://github.com/seb-eis/mocassin-2/actions/runs/45491888826), [Windows 45492513811](https://github.com/seb-eis/mocassin-2/actions/runs/45492513811), [macOS 45493153286](https://github.com/seb-eis/mocassin-2/actions/runs/45493153286) green. Round 1 at `5dd70e6`: [Ubuntu 45477848106](https://github.com/seb-eis/mocassin-2/actions/runs/45477848106), [Windows 45478516289](https://github.com/seb-eis/mocassin-2/actions/runs/45478516289), [macOS 45479315065](https://github.com/seb-eis/mocassin-2/actions/runs/45479315065) green
- **Main merge:** —
- **Step tag:** —
- **Audit:** [Passed with notes — owner-accepted 2026-11-24](../audits/step-24a.md); round-2 verdict at `10c7206`; F-9/F-10/F-11 follow-up owner `fix/step-24a-notes` or 24B/Step 25; closeout evidence [`step-24a.json`](../closeout/step-24a.json)
- **Design review:** **Ready with notes — accepted by the human owner 2026-11-23** at
  `b75efea4997eab9e89ebe62297efeca1f0c2f26e` ([record](../reviews/step-24a.md), round 2 `f4cc809`); class High
  accepted as one unit, the six-feature list approved, and revised D-2 confirmed. m-13, m-14, and m-15 are
  accepted for implementation-time resolution and audit verification; n-3, n-4, and n-5 were corrected before
  the start gate.
- **Layout gate:** **Closed** — ADR 0003
  [records the composition](../adr/0003-avalonia-mvvm-stack.md#editor-layout-decision-2026-11-22) (option 1
  `ListBox` table + detail composed with option 4's `GridSplitter`); the POC report
  `docs/planning/maintenance/2026-11-22-step-24-layout-poc.md` is the evidence and the spike code is discarded.
  **The 24A start gate is conditional on that report and its PNGs reaching `main`**: the `fix/step-24-layout-poc`
  branch is now documentation-and-assets only (the spike code was removed at `fbdae6c`) and lands through the
  owner-approved `--no-ff` merge recorded in ADR 0003.
- **Feature list:** **Approved** (six features, §18) — owner 2026-11-23
- **Design class:** **Full** (§16)
- **Complexity class:** **High** — cross-step interfaces and test-matrix breadth; **owner acceptance as one High
  unit is required at the start gate**, and no further split is proposed (§16)
- **Last updated:** 2026-11-24 (audit round 1 fixes applied; re-audit pending)

| Feature | Branch | Status | Local gates | Step CI | Squash commit |
| --- | --- | --- | --- | --- | --- |
| 1. Shared editor framework | `feature/step-24a-model-editors-particles-and-structure/shared-editor-framework` | **Squashed** | `./tools/verify.sh dotnet` (394 App + all suites), `api`, `structure`, `docs` green 2026-11-23 | — (no per-feature run) | `c24d1cf`; m-13 doc alignment `f77bfd3` |
| 2. Particle and set editors | `feature/step-24a-model-editors-particles-and-structure/particle-and-set-editors` | **Squashed** | `./tools/verify.sh dotnet` (412 App + all suites), `structure`, `docs` green 2026-11-23; `ParticleEditorTests`, `ParticleSetEditorTests`, `ModelEditorAcceptanceTests`, `EditorIntegrationTests` | — (no per-feature run) | `0c749c2` |
| 3. Space group and cell parameters | `feature/step-24a-model-editors-particles-and-structure/space-group-and-cell-parameters` | **Squashed** | `./tools/verify.sh dotnet` (426 App + all suites), `structure`, `docs` green 2026-11-23; `SpaceGroupPreviewTests`, `CellParameterTests`, `EditorIntegrationTests` | — (no per-feature run) | `82cd326` |
| 4. Site table and P1 preview | `feature/step-24a-model-editors-particles-and-structure/site-table-and-p1-preview` | **Squashed** | `./tools/verify.sh dotnet` (435 App + all suites), `structure`, `docs` green 2026-11-23; `CellSiteEditorTests` (m-14 overlap refusal, preview-index equality, D-2 energy-only delete, stability warning, read-only) | — (no per-feature run) | `034981d` |
| 5. Dummy positions and structure summary | `feature/step-24a-model-editors-particles-and-structure/dummy-positions-and-structure-summary` | **Squashed** | `./tools/verify.sh dotnet` (440 App + all suites), `structure`, `docs` green 2026-11-23; `DummyPositionTests` (distinct glyph, undo, save/load round-trip, read-only), `StructureSummaryTests` (recomputed equality, no statistics dependency) | — (no per-feature run) | `5128f9f` |
| 6. 24A tests, documentation, and closeout | `feature/step-24a-model-editors-particles-and-structure/tests-docs-and-closeout` | **Squashed** | `./tools/verify.sh all --require` green at `9d0cfd7`; App 451 tests; `docs`, `structure`, `policy` green; parity/24A-24B mapped | Ubuntu 45477848106, Windows 45478516289, macOS 45479315065 green at `5dd70e6` | `9d0cfd7` |
| 7. Audit round 1 fixes | `feature/step-24a-model-editors-particles-and-structure/fix-audit-round-1` | **Squashed** | `./tools/verify.sh dotnet`, `api`, `structure`, `docs`, then `all --require` green at `10c7206`; App 467 tests; F-1…F-8 regression tests added (`CellSiteEditorTests`, `EditorKeyboardTests`, `EditorReadOnlyTests`, `EditorAccessibilityTests`, `EditorPerformanceTests`, `EditorIntegrationTests`, `ParticleEditorTests`, `ParticleSetEditorTests`) | Round 2: Ubuntu 45491888826, Windows 45492513811, macOS 45493153286 green at `10c7206` | `10c7206` |

## Goal

Give a user a productive, keyboard-complete way to author the first half of the model. Particles, sets, the space
group, the cell, the sites, and the visual-only dummy positions must be editable without the CLI, with the
symmetry consequences visible *before* a change is applied, and with precise error navigation. The framework
built here is the editing surface every later model editor reuses; it must not teach a lesson Step 25 has to
unlearn.

## Scope

**In scope**

- The ADR 0003 editor surface: the shared virtualized table, the shared column/header policy, the resizable
  selection/detail splitter, focus and keyboard behaviour, and the reusable reference picker.
- `ModelEditorDocumentViewModel` / `ModelEditorViewModel` / detail-editor base, row projection, multi-select,
  search/filter/sort, empty states, and the local (unsaved) field state.
- The app-side `ModelUsageIndex` that answers usage counts and dependents from a `Project` snapshot.
- Particle and particle-set editors, including drag-and-drop plus a keyboard-only equivalent.
- Space-group picker with settings, operation details, compatibility and orbit-size preview, and apply/revert.
- Cell-parameter editor with the in-house numeric expression, crystal-system locks, live vectors/volume, and
  invalid-geometry messages from `CellGeometry`.
- Cell-site table with the P1 preview (orbit list, whole-structure list, 2D projection), overlap warnings, and
  the dependents list for blocked deletion.
- Dummy positions and the structure summary.
- Navigation domains, explorer nodes, inspector contributions, and document kinds for these editors.
- Inline validation, the domain summary with navigation, and undo/redo integration through the 23B coordinator.
- One additive Core API (§10), one additive app-local settings field (§9), and the 24A documentation, tests,
  closeout evidence, and audit request.

**Non-goals**

- **No transition editors** (state changes, groups, chains, KMC, MMC, transition summary): 24B. 24A reads
  transitions only to count references.
- **No energy, lattice, simulation, parameterization, job-template, build, 3D, run-monitoring, results, or
  analysis UI** (Steps 25–28 and 20–22).
- **No broad cascade deletion.** A referenced object's deletion is blocked with a dependents list and navigation;
  the user deletes dependents in the domain that owns them. The single exception is a site whose **only**
  dependents are energy references: the editor offers "delete site and energy references" through the existing
  `ProjectEditor.RemoveCellSiteAndEnergyReferences` (§6). There is no cascade for MMC transitions, KMC resolved
  paths, occupation exchanges, or dopings. See §17 D-2.
- **No new data-grid control, no dependency, no `Avalonia.Controls.DataGrid`/`TreeDataGrid`/third-party grid.**
  ADR 0003 stands; a new control dependency would need its own ADR.
- **No shell, workspace, settings, theming, or lifecycle redesign.** 24A fills the documented 23A/23B seams; a
  seam change beyond them stops the step for an amendment.
- **No `.mocprj`/`.msl`/`.msr`/`.mcs` format, schema, version, CLI-behaviour, diagnostic-code, or manifest
  change.** The settings addition is app-local and additive; the Core addition (§10) is an API overload, not a
  format.
- **No 3D.** The P1 preview is a deterministic 2D projection; Step 27 owns the viewport.

## Original reference

Read `/workspace/moccasin-original` as a read-only reference (GPL-3.0-or-later). Do not copy code or data.

- **Particles:** two WPF `DataGrid`s — particles (name, symbol, charge, vacancy) and sets (name plus
  drag-and-drop particles from a row header). Void is implicit at index 0.
- **Structure:** a space-group table with 274 entries and an operations list; a cell-parameter editor that locks
  dependent fields; a cell-site table (A, B, C, stability, particle set) with a live P1 expansion preview; dummy
  positions.
- Validation lives in a separate "Model Validator" console tab; the editors do not show inline errors
  consistently, and there is no undo/redo.

**Preserved intents:** implicit read-only void particle; sets as ordered occupation lists with a default
occupation; a searchable space-group chooser with visible operations; crystal-system field locking; the P1
expansion preview as the way a user understands symmetry; dummy positions as visual-only helpers; a structure
summary.

**Corrected in this step:** immutable model values edited only through `ProjectEditCoordinator`; undo/redo for
every edit; inline validation with navigation instead of a separate validator tab; a pre-apply compatibility
preview for space-group changes; exact P1 canonical order from the Step 02 services (never a re-derived order);
no `DataGrid`; no `System.Windows.*`.

## Design

### 1. Position in the shell

24A adds one registration method and fills existing seams. It changes no region, no command model, no document
key shape, no lifecycle, and no settings schema beyond the additive field in §9.

| 23A/23B seam | 24A use |
| --- | --- |
| `DocumentKinds`, `DocumentViewModel` | five document kinds and five editor documents (four master-detail, one form) (§2) |
| `INavigationTargetProvider` | the `particle`, `particleSet`, `structure`, `cellSite`, and `cellDummyPosition` domains |
| `IExplorerNodeProvider` | a `Model` node with Particles, Particle sets, Structure, Sites, and Dummy positions |
| `IInspectorContributor` | usage counts, dependents, and the selected object's key facts |
| `ICommandCatalog`, `IMenuItemProvider` | Model-menu entries that open each editor and its navigation commands; **gestures that act on the focused editor stay on the document's own commands**, not the global catalog (D-7) |
| `ProjectEditCoordinator`, `ProjectSession` | the only write path; `ProjectChanged`/`EditRejected` drive refresh and inline errors |
| `ValidationCoordinator` | the domain's diagnostics are filtered out of the existing validation state; 24A never runs the pipeline itself |
| `LayoutSettings` | the additive `editorFraction` (§9) |
| `AppServiceRegistration` | a new `AddMocassinModelEditors` call from `AppHost` after `AddMocassinViewModels`, registering the Symmetry preview/validator services for the editors (§11) |

The editor view models live in `Mocassin.App.ViewModels/Editors/**` and the views in
`Mocassin.App.Views/Editors/**`, so the existing `ViewLocator` convention (`…ViewModels.…ViewModel` →
`…Views.…View`) resolves the detail templates without a registry.

**Library reference and service registration (n-1).** `Mocassin.Symmetry` is today reachable only transitively
(`App.ViewModels → Packages → Compilation → Symmetry`). 24A adds an **explicit `Mocassin.Symmetry` project
reference** to `Mocassin.App.ViewModels` — an in-repo project reference to a merged step, not a package or
external dependency — and updates [`01-target-architecture.md`](../01-target-architecture.md) §3.2 accordingly.
`AddMocassinModelEditors` registers `IStructurePreviewService` (`StructurePreviewService`) and
`IStructureValidator` (`StructureValidator`) as singletons for the editor view models; their constructors'
optional catalog/version parameters resolve to the embedded-catalog defaults, so no extra service registration
is needed. The view models consume these services and the `TransitionPathResolver` (§2); they never re-derive
exact symmetry or resolved paths.

### 2. Editor framework

```csharp
// Mocassin.App.ViewModels/Editors (internal)
internal abstract partial class ModelEditorViewModel : ObservableObject, IDisposable
{
    public ProjectSession Session { get; }
    public ProjectEditCoordinator Coordinator { get; }
    public ModelTableViewModel Table { get; }                  // rows, columns, search, filter, sort, multi-select
    public ModelUsageIndex Usages { get; }                      // rebuilt lazily per Project snapshot
    public IReadOnlyList<Diagnostic> Diagnostics { get; }       // this editor's domains only
    public ModelEditorState State { get; }                      // Empty, Ready, ReadOnly
    public IRelayCommand AddCommand { get; }
    public IRelayCommand RemoveCommand { get; }
    public IRelayCommand DuplicateCommand { get; }
    public IRelayCommand ShowUsagesCommand { get; }
    public void RequestSelection(Guid objectId);
    public void RequestFocus(string property);
}

internal abstract class ModelEditorViewModel<TModel> : ModelEditorViewModel
{
    protected abstract ModelTableRow ToRow(TModel model);
    protected abstract ModelDetailViewModel CreateDetail(TModel model);
}

internal abstract partial class ModelDetailViewModel : ObservableValidator
{
    public bool HasUncommittedChanges { get; }                  // multi-field forms only
    public IRelayCommand ApplyCommand { get; }
    public IRelayCommand RevertCommand { get; }
    public void FocusField(string property);
}

internal sealed class ModelEditorDocumentViewModel : DocumentViewModel
{
    // Key/Title/Icon/ToolTip from the domain descriptor; hosts a ModelEditorViewModel;
    // implements IInspectorContributor and handles NavigationIntent.
}

internal sealed class StructureEditorDocumentViewModel : DocumentViewModel
{
    // The structure document is a form, not a master-detail: two ModelDetailViewModel-style forms
    // (space group, cell parameters) plus the summary. It shares the framework's detail/validation/
    // coordinator plumbing but has no table and no row selection.
}
```

- **Two document shapes.** Particles, sets, sites, and dummy positions are master-detail documents
  (`ModelEditorDocumentViewModel`). The structure document is a **form** document
  (`StructureEditorDocumentViewModel`): two detail forms (space group, cell parameters) and the summary, sharing
  the detail/validation/coordinator plumbing but with no table or row selection.
- **One tab per domain document.** `DocumentKinds` gains `model.particles`, `model.particleSets`,
  `model.structure`, `model.sites`, and `model.dummyPositions`. The explorer's `Model` node maps each to its
  `ModelPath` domain.
- **The table selects; the detail edits.** The document is `selection surface | splitter | ContentControl`. The
  `ContentControl`'s data template is resolved by `ViewLocator` from the detail view-model type.
- **The list framework is `ObservableObject`, the detail is `ObservableValidator`.** The phase-1 sketch made the
  list base an `ObservableValidator`; validation belongs to the editable field values, which live in the detail
  view model, so the base is split (D-4).
- **Immediate versus explicit apply.** A single-field edit commits on `Enter` or focus loss as one undo entry. A
  multi-field form (particle add, set add, site add, cell parameters) assembles all fields into **one**
  `ProjectEditResult` and one undo entry behind an explicit Apply button; Revert restores the last committed
  values. Nothing is written before Apply.
- **Editing path.** The detail calls the same `ProjectEditor` typed operations the CLI uses
  (`AddParticle`, `UpdateParticle`, `RemoveParticle`, `AddParticleSet`, …, `SetSpaceGroup`, `SetCellParameters`,
  `AddCellSite`, `AddDummyPosition`) and commits the returned value through the additive
  `ProjectEditCoordinator.Apply(description, operation)` overload (§10), which records one undo entry and raises
  `EditRejected` with the operation's diagnostics when the operation fails. The view model never mutates a
  `Project` or a domain object and never builds a `ProjectChange`.
- **Local state refresh (`ProjectChanged` kinds, D-14).** `ProjectChanged` carries a `ProjectChangeKind`. The
  editor reacts by kind:
  - `Saved` — values are unchanged: update only the dirty/save-point state and the tab title; do **not** rebuild
    rows or invalidate an open form.
  - `Edited`, `Undo`, `Redo` — rebuild rows and preserve selection by object id. An open form with uncommitted
    changes compares its object by id in `Previous` and `Current`:
    - value-equal → keep the form and refresh only the reference-picker options;
    - value-changed → keep the fields, refresh options, and show "The project changed elsewhere"; Apply stays
      enabled (last writer wins, explicitly) and Revert restores the current committed value;
    - removed → Apply is disabled and the form offers to close; it never silently rebases.
  - `Replaced` (reload/recovery) — discard uncommitted forms, rebuild all rows and options from the new snapshot.
  - A change in another domain never invalidates a form; it only refreshes reference-picker options.
- **Read-only sessions** disable Add/Remove/Duplicate/Apply, show the 23B read-only banner, and never call the
  coordinator (a reached `Apply` would return `ReadOnly`; the editor also guards locally).
- **Empty states** name the next action ("No particles yet — Add a particle or open a sample project") and keep
  the Add command reachable by keyboard.

**Usage index (`ModelUsageIndex`, D-12).** The index is built lazily per immutable `Project` reference and answers
two questions: "how many objects reference this one" for the usage column, and "which objects are these" for the
blocked-delete list and the inspector. It walks the project value and attributes:

- particles → particle sets, state changes (donor/acceptor), transitions by chain → group → state change;
- particle sets → cell sites;
- cell sites → MMC transitions (`SiteAId`/`SiteBId`), KMC transitions **through their resolved reference path**
  (each point resolved with `Mocassin.Symmetry.Transitions.TransitionPathResolver`, which yields the owning
  `CellSiteId`), occupation exchanges (and the dopings that reference them), and the energy references (stable
  and unstable environment filters, cluster center sites, site-energy sets).

The index is an **affordance and a pre-check**, not a validator: it uses the same Step 02/04/05/06 services the
compiler uses and never re-implements their rules. The KMC path resolution is required because the Core structural
validator deliberately does not check resolved paths (`TRN020`/`TRN012` belong to the Symmetry path validator, which
runs only in the full pipeline), so the index is the only place a site edit can see that dependency before applying.

### 3. Table surface, column policy, and focus

The production surface is one `UserControl` (`ModelTableSurface`) in `Mocassin.App.Views/Editors`, bound to a
`ModelTableViewModel`. It is a composition of a header `Grid`, a virtualized `ListBox`, and the production
`scroll-gutter-right` class — **not** a `TemplatedControl` (the POC measured 333 lines for that control and no
Step 24 requirement needs it).

- **One column policy.** `ModelColumn` is a view-model value: `{ Header, ModelColumnWidth Width, double Minimum,
  ModelSortKind SortKind, bool IsSortable }` with `ModelColumnWidth { Fixed(double Pixels), Star(double Weight) }`.
  The view maps it to `GridLength`; the view models never reference `Avalonia.Controls`. Header and rows consume
  the *same* list, so columns cannot drift. The 20 px scroll gutter is reserved as in the shell baseline.
- **Sorting is typed, not over formatted text.** `ModelSortKind { Text, Number, Id }` gives name, charge, orbit
  size, and coordinate columns the right comparer. The POC's ordinal-ignore-case sort over formatted cells is a
  recorded spike limitation, not the design.
- **Search and filter** are app-side over the row projection (one search box, one severity filter), with the
  match count announced to assistive technology.
- **Focus and tab order (framework requirement, POC finding 1).** The selection surface is **exactly one tab
  stop**, whatever the selection state. Row containers are not tab stops
  (`ListBoxItem.IsTabStop="False"` in the row style) and the list uses one-shot tab navigation
  (`KeyboardNavigation.TabNavigation="Once"`); `Tab` from the list always moves to the detail pane's first
  field, including immediately after `Home` or `End`. If one-shot mode does not produce that behaviour on
  Avalonia 12.1.6, the fallback is a deterministic `KeyDown` handler on the list that moves focus to the detail
  pane on `Tab` (and only then). A real-key headless regression test proves `Home` → `Tab` → detail focused and
  `End` → `Tab` → detail focused.
- **Row activation.** `Enter` on a focused row opens/focuses the detail editor; double-click is the same action
  ([`ui-interaction-contract.md` §8](../../development/ui-interaction-contract.md#8-rules-for-steps-2428-editors)
  rule 4). Arrow keys keep list selection; `Ctrl`/`Shift` extend it where multi-select is enabled.
- **Multi-select** is enabled for particles, sets, and sites, where bulk deletion is safe under the same
  blocked-delete checks.
- **Bulk operations are one undo entry, all-or-nothing (D-13).** The framework composes a bulk action as **one**
  `Func<ProjectEditor, Project, ProjectEditResult>` (§10): it folds the typed single-object operation over a
  candidate `Project`, accumulating diagnostics, and returns one result whose `ProjectChange` spans the original
  and the final value. A mid-batch rejection discards the whole candidate — the project, undo stack, redo stack,
  save point, and dirty state are untouched — and the inline summary names the failing object and reason. Bulk
  delete of `n` objects is one entry; if any selected object is referenced, the batch is refused and the
  dependents of every selected object are listed.
- **Accessibility.** Every control has an automation name and role; the table exposes a list role and the row's
  severity as text; status is never colour-only; the splitter is named and keyboard-resizable
  ([`ui-visual-contract.md`](../../development/ui-visual-contract.md)).

### 4. Reference pickers and the numeric expression

**Reference picker (one reusable control).** Avalonia has no searchable combo, so
`ReferencePickerViewModel<TReference>` composes a search box with a filtered combo: `SearchText`, `Options`
(each with a display name plus the disambiguating details), `Selected`, `Placeholder`, `IsEnabled`, and a
`ValidationMessage`. Keyboard: typing filters, `Down`/`Up` moves, `Enter` selects, `Escape` closes. It is used
for the particle set on a site, the particle members of a set, and (24B) every group/site/chain reference.

**Numeric expression (`NumericExpression`).** Cell lengths, angles, and fractional coordinates accept a small
arithmetic expression: `+ - * / ^`, parentheses, unary minus, the constants `pi` and `e`, and the functions
`sqrt`, `ln`, `exp`, `abs`. Parsing is culture-invariant (`double.Parse` with `NumberStyles.Float` and
`CultureInfo.InvariantCulture`); the parser is a hand-written recursive-descent parser with no dependency, a
bounded input length (128) and depth (16), and it rejects non-finite results (`NaN`, `±Infinity`) as an inline
error.

- **Lexing rule (m-6).** A token is a number, an identifier, an operator, or a parenthesis. A number is
  `digits [ "." digits ] [ ("e"|"E") [ "+"|"-" ] digits ]`: exponent notation belongs to the number and requires
  at least one exponent digit, so `1e5` and `2e-3` are numbers while `1e` and `2e` are malformed numbers (an
  error), not `1 * e` or `2 * e`. The identifier `e` is the constant only when it is not preceded by a digit
  sequence; there is no implicit multiplication (`2 pi` is an error, not `2 * pi`), and no variable names exist.
- **No trigonometric functions in v1.** The fields that would use them are angles in degrees while an expression
  is unitless, so `sin(30)` has two defensible meanings. The step does not guess: trigonometric input is out
  until a design decides the unit convention, and that limitation is documented and tested.
- **Non-exact fractions are warned, not corrected.** `1/3` lands on a double; the P1 preview surfaces the Step 02
  exact-grid rounding diagnostic (`SYM002`) as a warning. The editor never rewrites the user's value.

### 5. Particle and set editors

**Particles document** (`model.particles`)

- Columns: name, symbol, charge, vacancy, usage count (sets and transitions), and a row severity badge.
- The implicit void particle is pinned first, read-only, with an explanation; it is never persisted and never
  counted as an authored particle.
- Add creates a particle with a unique default name (`Particle 1`, `Particle 2`, … case-insensitive) and charge
  `0`, as one undo entry.
- Charge/symbol edits commit as one undo entry per field and refresh dependents immediately.
- The vacancy toggle warns when the default occupation is charged (Step 03 `PAR004`).
- Duplicate copies the fields with a new id and a unique name; bulk "set charge" is one composed operation and
  one undo entry (D-13).
- Delete is **blocked** when the particle is referenced: the editor lists dependents from the usage index and
  offers "Show usages" (navigates to the dependent domain). The Core structural validator also rejects a dangling
  particle reference, so the pre-check and the rejection must agree; a test asserts both.

**Particle-sets document** (`model.particleSets`)

- Columns: name, member count, usage count.
- Detail: an ordered member editor with a "Project particles" drag source, the ordered member list, the first
  member badge "default occupation", and Add/Remove/Move-up/Move-down buttons for keyboard users.
- Drag-and-drop and the keyboard path are both first-class; each drag action has a button equivalent and a test.
- Void cannot be a member and an empty set is invalid (`PAR005`); the error is inline, and Apply is disabled.
- "Occupation chips" preview the allowed occupations in member order, with the default badge.
- Deleting a set used by a site is blocked with the dependent site list (`STR006`).

**Drag source reconciliation (D-3).** The indicative design said "drag particles from the particle list into the
set". Two documents cannot portably be side by side in one shell, and cross-document drag is fragile. The set
detail therefore carries its own "Project particles" list as the drag source, so the workflow works inside one
document, keyboard included. The particles document remains the place to edit particle fields.

### 6. Structure documents

**Structure document** (`model.structure`) — a form document, not a master-detail.

- **Space group.** A searchable picker over the 274 catalog settings by number, Hermann–Mauguin, Hall symbol, or
  crystal system; an axis/origin settings selector for the resolved setting; a details panel with the operation
  count, general multiplicity, crystal system, and the operation list in matrix/translation and human-readable
  form. Changing the setting never applies immediately: the editor builds the *candidate* `StructureModel`
  value, runs `IStructurePreviewService.TryCreatePreview` and `IStructureValidator.Validate` on it, and shows
  the new orbit sizes, any site that would overlap (`STR007`), and any cell-parameter violation (`STR003`)
  before the user confirms. Confirmation applies one undo entry.
- **Cell parameters.** Six fields with units (Å, degrees) and the expression grammar (§4). Dependent fields are
  locked by the resolved crystal system; a tooltip explains why. Live cell vectors and volume come from
  `CellGeometry.TryCreate` on the candidate values. An invalid angle combination shows the violated
  inequality from the geometry diagnostics, not a re-written message. "Reset to cubic" and "Reset to defaults"
  are explicit commands with confirmation. Field locks are an affordance only: `CellGeometry` remains the
  validity authority, and a test cross-checks the lock table against the geometry diagnostics for every crystal
  system so the two cannot drift.
- **Structure summary (m-1: named sources only).** Every value comes from the preview the editor already builds
  or from the structure value — 24A does **not** call `ModelStatistics` (23B keeps statistics an explicit
  on-demand action):
  - reference-site count ← `StructureModel.Sites.Length` (equal to `StructurePreview.Orbits.Length`);
  - P1 position count ← `StructurePreview.PositionList.Length`;
  - **stable/unstable P1 position** counts ← counted from `StructurePreview.PositionList` by the owning reference
    site's `Stability` (`ExpandedSite.ReferenceSiteIndex` indexes the input reference sites in
    `StructureModel.Sites` order), labelled "positions", never "sites";
  - cell volume ← `StructurePreview.CellVolumeAngstrom3`;
  - orbit table ← `StructurePreview.Orbits` (representative, multiplicity, position indices);
  - **reference-site density** `= ReferenceSiteCount / CellVolumeAngstrom3` in Å⁻³, labelled as such.
    Particle-number density is a lattice/simulation quantity (it needs a compiled lattice) and is **not** shown in
    24A;
  - large-P1 performance warning ← `STR009` from `IStructureValidator`.

**Sites document** (`model.sites`) — the master-detail surface.

- Columns: name, A/B/C, stability, particle set, orbit size, multiplicity, and Cartesian position.
- Detail: name, three fractional fields (expression-capable), stability, particle set (reference picker), orbit
  size and multiplicity (read-only, from the preview), and the Cartesian position (read-only).
- **P1 preview panel** below the fields, as a toggle, not a tab (D-5). "This site's orbit" rows show the
  fractional position, the Cartesian position, the **canonical `P` index**, and the **orbit-member index**;
  "Whole structure" lists every canonical P1 position in `P` order plus a deterministic 2D projection on the
  selected a–b, a–c, or b–c plane. Overlap positions are marked with a glyph and text.
- **No generating-operation label (M-1/D-11).** No Symmetry preview API exposes the operation that maps a
  reference position onto an orbit member: `StructurePreview` carries positions, Cartesian positions, volume,
  orbits, and the encoder; `SiteOrbitInfo` carries the representative, positions, and position indices; and
  `ExpandedSite` carries `P`, encoding, fractional values, the reference index, and `OrbitMemberIndex`. 24A
  therefore shows positions and indices only. It does not re-derive the operation in the app — that would
  duplicate Step 02's exact symmetry in a view model, contrary to §1, `01-target-architecture.md` §3.2, and
  `AGENTS.md` §5 — and it adds no Symmetry API. A labelled operation is deferred to a later step that opens its
  own preview API with an owner-approved design change.
- Add site: the form requires stability, a particle set, and three coordinates; the site is refused with
  `STR005` (empty/duplicate name) or `STR006` (missing set) and is never added in a half-valid state.
- **Deletion (D-2, corrected in round 2).** The dependents come from the usage index (§2) and are exactly:
  MMC transitions that select the site (`SiteAId`/`SiteBId`); KMC transitions whose **resolved reference path**
  lands on the site (each point resolved through `TransitionPathResolver`, giving the owning `CellSiteId`);
  occupation exchanges at the site (and the dopings that reference them); and the energy references (stable and
  unstable environment filters, cluster centre sites, site-energy sets). Two actions:
  1. **Delete site** (default) — blocked when any dependent exists, with the dependents list and navigation.
  2. **Delete site and energy references** — enabled **only** when the only dependents are energy references;
     one undo entry through `ProjectEditor.RemoveCellSiteAndEnergyReferences`; the confirmation lists the energy
     objects that will be removed. It is never offered while an MMC/KMC/exchange/doping dependent exists.
  The editor's index is the blocking authority because the Core structural validator does not check every case:
  `ProjectValidator` validates a dangling `OccupationExchange.SiteId` and the Core-owned energy references, but
  MMC site ids and KMC resolved paths are Symmetry rules (`TRN016`/`TRN020`, `TRN012`) that run only in the full
  validation pipeline. A test asserts the index and the Core rejection agree wherever the Core checks apply, and
  that the energy-reference action is refused when a non-energy dependent exists.
- **Stability changes** warn when an MMC transition selects the site or a resolved KMC path point lands on it,
  because the change can invalidate `TRN012`/`TRN020` rules without the structural validator blocking the edit;
  the Problems channel reports the resulting diagnostics after the edit. 24A warns, it does not block a stability
  change.

**Dummy-positions document** (`model.dummyPositions`) — a small master-detail with name and three fractional
fields, a distinct marker glyph in every list and preview, and no physics. A "dummy positions tab" inside the
structure editor would add a nested tab level, which the layout POC rejected; it is its own document (D-5).

### 7. Validation, navigation, and commands

- **Diagnostics.** Each editor filters the current `ValidationCoordinator` state to its own domains (by
  `ModelPath.Domain`) and shows a summary bar with error/warning counts. It does not run validation, does not
  duplicate the Problems channel, and does not claim an edit is "fully validated" because `Apply` succeeded
  (23B §2). The summary's "Open Problems" activates the existing channel; inline entries navigate to the field.
- **Navigation.** Each provider handles `NavigationIntent`:
  `Activate` opens or activates the domain document; `SelectObject` selects the row by `ObjectId`;
  `FocusProperty` selects the row and focuses the named field. The `structure` domain has no authored object id
  (its diagnostics carry `Guid.Empty` plus a property such as `name`, `spaceGroup`, or `cell`), so the structure
  provider activates the document and focuses the property regardless of the id. A stale path (unknown object
  id) returns `null` and the shell reports it; nothing throws. The 23A rule that an unregistered domain produces
  a console notice still holds for 24B's domains until 24B registers them.
- **Commands.** Document-scoped commands carry the editor gestures: `Enter` applies the focused field or form,
  `Escape` reverts it, `Ctrl+D` duplicates the selected row, and `Delete` removes after confirmation. Global
  catalog entries are limited to opening the editors from the Model menu and to "Show usages", so gestures that
  act on the focused editor stay document-local and two editors cannot claim the same key (D-7).
- **Dirty state** is owned by 23B; 24A adds no second dirty tracker. A committed edit marks the document dirty
  through the coordinator; an uncommitted form does not.

### 8. State ownership and threading

- The authoritative project value is the `ProjectSession`'s document; the editor holds no mutable model state.
- Everything UI-facing runs on the UI thread through `IUiDispatcher`. The P1/structure preview is a pure CPU
  computation over an immutable snapshot: it is debounced (250 ms), cancellable, run off the UI thread, and its
  result is discarded when a newer structure value supersedes it (a monotonically increasing token, no hashing).
  The debounce affects **display only**; Apply re-validates the candidate synchronously (m-14/F-1).
- The usage index is rebuilt lazily per `Project` reference; `Project` is immutable, so reference identity is a
  sound cache key.
- Subscriptions to `ProjectChanged`/`EditRejected` are removed on `DocumentViewModel.OnClosingAsync`/`Dispose`;
  a leaked subscription is a test failure (the harness asserts the coordinator's handler count returns to the
  baseline).

### 9. Settings addition (no schema bump)

`LayoutSettings.EditorFraction` (app-local, additive): the selection pane's share of the editor width, default
`0.42`, clamped `0.25–0.70`, one value for every editor document. `AppSettingsValidator` clamps it like the shell
fractions and an absent value reads as the default — no migration and no `defaultsRevision` bump. Per-domain
fractions are deliberately not stored: they multiply the settings surface for a cosmetic preference.

**Ownership and the reset path (m-9/D-9).** The value is **shell-owned layout state**, not per-document state, so
there is exactly one writer (`ShellViewModel`'s serialized layout-persist path) and every open editor observes the
same value:

1. The shell's layout state gains `EditorFraction` (a property on the existing `ShellRegionsViewModel` layout
   state, or a one-property `IEditorPaneLayout` that the shell layout state implements; the interface is internal
   to `Mocassin.App.ViewModels` and has two real consumer groups — the shell and every editor document).
2. `ShellViewModel.CaptureLayout()` includes `EditorFraction`, `ApplySettings` sets it, and the existing
   region-property-changed hook persists it on a drag or a reset — so a shell region change can never drop the
   editor fraction.
3. `ShellViewModel.ResetLayout()` sets `EditorFraction` to `SettingsDefaults.DefaultEditorFraction` alongside the
   three region fractions; the property change reaches every open editor document, which applies it to its
   splitter immediately (no document restart, no per-document write).
4. Editor documents read the value on open and write it back on splitter release through the same shared state;
   they never call `ISettingsService.UpdateAsync` themselves.

### 10. Library change owned by this step

One additive Core API, needed because the GUI must run the **same typed `ProjectEditor` operations as the CLI**
inside the **one** coordinator write path (m-7/D-8):

```csharp
// Mocassin.Core.Editing.ProjectDocument (additive overload)
public ProjectEditResult Apply(
    string description,
    Func<ProjectEditor, Project, ProjectEditResult> operation);
```

**The delegate receives the document's own `ProjectEditor`.** That makes the precondition structural: the
operation is a typed `ProjectEditor` operation over the document's current project, not a hand-built mutation.
`ProjectDocument` bypasses its own validator for this overload, so the documented caller contract is *"pass the
provided editor's typed operation"*; the dev adds the guard below and a test for the failure path.

- **Semantics.** The overload is an instance method on `ProjectDocument`, so it invokes `operation(editor, Project)`
  with the document's own `ProjectEditor` (the field the constructor already stores; no new accessor is added).
  If the result has an error diagnostic, return it and change nothing (undo stack, redo stack, save point, and
  dirty state untouched). If it succeeded with
  `Change == null` (the typed operation's own no-op case), return it with no stack change. Otherwise verify
  `ReferenceEquals(result.Change.After, result.Project)` and `ReferenceEquals(result.Change.Before, Project)`
  (a malformed result is an `InvalidOperationException`, i.e. a programmer error), then record **one**
  `ProjectChange(description, Project, result.Project)` — the caller's description, not the typed operation's —
  append it to the undo stack, **`TrimUndoStack()` (`MaxUndoDepth`)** and **clear the redo stack**, set
  `Project`, and return the result. This mirrors the existing overload exactly, so save-point and dirty behaviour
  are unchanged.
- **API gate.** The overload is a new public Core member: `Mocassin.Core/PublicAPI.Unshipped.txt` gains its line
  and `./tools/verify.sh api` must pass (ApiCompat is additive-only, so the baseline is unchanged). This is a
  feature-1 gate, alongside `verify.sh dotnet`.
- `ProjectEditCoordinator` gains the matching internal overload
  `Apply(string, Func<ProjectEditor, Project, ProjectEditResult>)`: it performs the read-only check, raises
  `EditRejected` with the operation's diagnostics, and announces `ProjectChanged` on success. It remains the
  **only** write path. A bulk action composes several typed operations into **one** delegate of this shape
  (§3/D-13).
- Rejected alternative: running the typed operation and then calling the existing `Apply(_ => result.Project)`
  validates every successful edit twice and loses the operation's precise rejection diagnostics. Rejected:
  building mutations in the view models duplicates the Core operations and would drift from the CLI. Rejected:
  re-validating inside the overload for the same double-validation cost; the structural delegate plus the
  identity guards cover the trust gap.
- No other library change. No new package, no format, no diagnostic code, no `Mocassin.Compilation` reference.
  `Mocassin.Symmetry` is a new **project** reference (§1/n-1), not a package.

### 11. Dependency and interface checks

- **Dependencies** 23B and 02–06 are `Done`, merged, and tagged; their model, preview, and edit APIs are stable.
  24A uses no Step 08–22 API. No package is added. The only new reference is an **in-repo
  `Mocassin.Symmetry` project reference** on `Mocassin.App.ViewModels` (n-1), recorded in
  [`01-target-architecture.md`](../01-target-architecture.md) §3.2; it is not a package dependency and it does not
  change the design class.
- **Services registered by `AddMocassinModelEditors`** — `IStructurePreviewService` and `IStructureValidator` as
  singletons (their optional constructor parameters default to the embedded catalog and versions). The usage index
  uses `TransitionPathResolver.TryCreate` per structure snapshot; no other service is added.
- **Inputs/outputs** are `Project` snapshots in and `ProjectEditResult` values with their diagnostics out; no
  persisted type, schema, or file is introduced.
- **Versioning and migration** are not applicable: the only persisted surface is the additive app-local setting,
  which reads a default when absent and needs no migration.
- **No unresolved cross-step interface question remains for 24A.** Round 1's M-1 is resolved *inside* the existing
  boundary: the P1 preview shows positions and indices only, so no Symmetry API is needed and no interface question
  is open (§6/D-11). 24B's compile-preview API question is recorded on the 24B page and is not a 24A dependency.

### 12. Documentation plan

| Page | 24A content |
| --- | --- |
| `docs/user-guide/particles.md` | Editing particles and sets in the GUI: pinned void, usage counts, blocked delete, drag/keyboard membership, default occupation, occupation chips |
| `docs/user-guide/structure.md` | Space-group picker and compatibility preview, cell parameters and expressions/locks, site table, P1 preview and projection, dummies, summary |
| `docs/development/ui-architecture.md` | The editor framework, table surface and column policy, reference picker, expression grammar and its deliberate limits, usage index, threading |
| `docs/development/ui-interaction-contract.md` | Confirm §8 covers the row-not-a-tab-stop rule and add it if the existing text does not; record the editor's `Enter`/`Escape`/`Tab` mapping |
| `docs/troubleshooting/errors.md` | Inline editor errors versus diagnostic codes, blocked delete, read-only editing, expression failures, overlap/symmetry warnings |

"Documentation written" is part of the acceptance criteria. `docs/planning/` stays out of the published site.

### 13. Test plan

| Test | Assertion | Tier |
| --- | --- | --- |
| `ModelEditorAcceptanceTests` | **From an empty project** (`samples/blank/blank.mocprj`, whose `model` is `{}`) through `ProjectStore`, author the ceria particles, sets, structure, and sites without the CLI, then compare the authored values by name/value to `samples/ceria/ceria.mocprj` (ids legitimately differ); undo every edit back to the loaded blank value. A second case opens ceria and edits one existing field, proving the edit path on a populated project (m-3) | PR-fast |
| `ModelTableSurfaceTests` | Header and rows share the column list; columns stay aligned at 1280 and 720 px with the scroll gutter; 1,000 sites realize a bounded container count and the last row is selectable | PR-fast |
| `ModelTableKeyboardTests` | Arrows/Home/End/Tab/Enter/Escape; `Home` → `Tab` and `End` → `Tab` both focus the detail pane's first field; row containers are not tab stops | PR-fast |
| `ParticleEditorTests` | Add/rename/charge/symbol/duplicate/bulk; the void row is pinned and read-only; `PAR001`, `PAR004`, `PAR008` surface inline; a referenced delete is refused and the project is unchanged | PR-fast |
| `ParticleSetEditorTests` | Ordered membership, default badge, void refused (`PAR005`), empty set refused, duplicate gets a new id, drag and keyboard produce the same order, use by a site blocks deletion (`STR006`) | PR-fast |
| `NumericExpressionTests` (property-based) | Precedence, associativity, unary minus, constants, functions, `1/3`; the lexing cases `1e5`, `2e-3`, `1e`, `e`, `2e`, `2 pi` (numbers vs identifiers vs malformed, no implicit multiplication, §4); malformed input, over-long input, deep nesting, `NaN`/`Inf` are rejected with a message; culture-invariant parsing; the same input parses identically under `de-DE` and `en-US` (m-6) | PR-fast |
| `SpaceGroupPreviewTests` | A candidate setting previews orbit sizes and overlap before apply; cancelling changes nothing; applying is one undo entry; `STR003`/`STR007` are shown for an incompatible candidate | PR-fast |
| `CellParameterTests` | Locks match `CellGeometry` for every crystal system (cross-check table test); invalid angle combinations show the geometry diagnostic; reset commands are undoable; live vectors/volume match `CellGeometry` | PR-fast |
| `CellSiteEditorTests` | Coordinate expressions, stability and particle-set rules (`STR005`, `STR006`), overlap inline (`STR007`), orbit size/multiplicity match `StructurePreview`; **deletion**: the dependents list contains the MMC transitions, the KMC transitions whose resolved path lands on the site, the occupation exchanges/dopings, and the energy references; the default delete is blocked; the energy-reference action is offered only when the remaining dependents are energy references and is one undo entry; the index agrees with the Core rejection wherever the Core validator checks; a stability change warns when a KMC/MMC transition uses the site (m-2) | PR-fast |
| `P1PreviewTests` | The whole-structure list equals `StructurePreview.PositionList` (canonical `P` order); each per-site orbit row's canonical `P` index and orbit-member index equal the values in `StructurePreview.Orbits[i].PositionIndices` and `PositionList[...].OrbitMemberIndex`; the row projection has **no operation field**, and the view renders no operation label (M-1); the 2D projection is deterministic for a fixed structure and plane; a >1,000-position preview is virtualized and cancellable | PR-fast |
| `DummyPositionTests`, `StructureSummaryTests` | Dummy positions round-trip and render with the distinct glyph; the summary's reference-site count, P1 position count, stable/unstable **position** counts, volume, orbit table, and reference-site density equal the values recomputed from `StructureModel.Sites` and `StructurePreview` (§6); the summary calls no `ModelStatistics` service (m-1) | PR-fast |
| `EditorNavigationTests` | `particle/<id>`, `particleSet/<id>`, `structure`, `cellSite/<id>`, `cellDummyPosition/<id>` open/select/focus; a stale id returns `null` without throwing; every domain diagnostic in a ceria fixture navigates to its field | PR-fast |
| `EditorUndoTests` | Undo/redo across every editor operation, save point intact, dirty marker follows the coordinator; **bulk delete of n objects and bulk set-charge are each one entry**; a mid-batch failure changes nothing (project, undo, redo, save point, dirty state) (m-8) | PR-fast |
| `ProjectDocumentApplyOverloadTests` (Core, n-5) | The additive `ProjectDocument.Apply` overload (§10): an accepted edit records **one** `ProjectChange` with the caller's description, `Before` the document value and `After` the result project, and clears the redo stack; a rejected operation returns its error result and leaves the project, undo stack, redo stack, save point, and dirty state untouched; a successful no-op (`Change == null`) returns without a stack change; a malformed result whose `Change.Before`/`Change.After` is not reference-equal to the document/result project throws `InvalidOperationException` | PR-fast |
| `EditorReadOnlyTests` | In a read-only session Add/Remove/Duplicate/Apply and `Enter` are disabled, the 23B banner is rendered, the coordinator is never called, no undo entry and no dirty change appear, and no `EditRejected` diagnostic is raised; a forced coordinator `Apply` returns `ReadOnly` and changes nothing (m-4; F-5). Both the master-detail and the structure form assert the rendered banner | PR-fast |
| `EditorRefreshTests` | On `ProjectChanged`: `Saved` does not rebuild rows or invalidate an open form; an edit in another domain refreshes reference options only; an edit to the open form's object shows the conflict with Apply/Revert as specified; removal disables Apply; `Replaced` rebuilds and discards forms (m-10) | PR-fast |
| `EditorScreenshotTests` | Light, dark, and high-contrast captures of the particles, sets, structure, sites, and P1-preview states, plus a narrow-window capture | PR-medium (Ubuntu) |
| `EditorAccessibilityTests` | Every interactive control has an accessible name; the table exposes a list role; severity is text not colour; the splitter is named, focusable, keyboard-resizable, and its committed ratio is persisted to the shell layout state | PR-fast headless |
| `EditorPerformanceTests` | **Functional only, no absolute duration (m-5)**: the realized row-container count is bounded and does not grow with row count (compare 100 vs 1,000 sites); all 1,000 rows are present and the last row is selectable; with a fake clock, rapid edits coalesce into one preview and a superseded preview result is discarded; no row is dropped after a rebuild | PR-fast |
| `EditorPerformanceEvidence` (not a test) | Non-gating measured evidence on the Linux dev container (Release, xunit, 2026-11-24): 1,000-site editor construction **13–44 ms**, first selection **28–57 ms**, one candidate edit + synchronous validation/preview **22–26 ms**; the 100-site comparison was **20 ms** select / **1.3 ms** edit. Timings are indicative, not asserted (`03-quality-strategy.md` §11.1) | non-gating evidence |

**The 1,000-row limit (POC finding 2).** `StructureModel.MaxReferenceSites` is 1,000 and `STR012` rejects more, so
no valid project can hold "more than 1,000 reference sites". The virtualization acceptance uses a **1,000-site**
structure (the legal maximum) for the site table, and a **>1,000-position** P1 preview (positions are not
reference sites) for the larger case. A test asserts that a 1,001-site structure is rejected with `STR012`
rather than silently accepted, so the limit is documented where a test can prove it.

### 14. Performance expectations (non-gating)

- Virtualized tables: realized containers bounded by viewport, not row count; the 1,000-site surface selects and
  scrolls without dropping rows or misaligning columns.
- P1/structure preview: 250 ms debounce, cancellation, off-thread computation; a progress indicator above 100 ms;
  no preview work while a text field is mid-edit.
- Usage index: one lazy build per project snapshot; a 1,000-site, 64-particle project stays well under a frame.
- No validation or compile work on keystrokes; only on commit and on the debounced validation the shell already
  owns.
- **No PR-fast test asserts an absolute duration.** The functional properties are tested (bounded realized
  containers, coalescing, superseded-result discard, no dropped rows); measured build/select and preview times are
  recorded as non-gating evidence (`03-quality-strategy.md` §11.1, m-5).

### 15. Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| The production table drifts from the POC surface | One `ModelTableSurface` + one `ModelColumn` policy; the POC's alignment, tab-order, and gutter assertions become production tests |
| Expression parser scope creep or locale bugs | Fixed grammar, bounded input, no trig in v1, property tests plus a two-culture test |
| Symmetry preview performance on large cells | Debounce, cancel, off-thread, cache by immutable structure reference, progress above 100 ms |
| Drag-and-drop excludes keyboard users | Every drag has a button equivalent and a test; the interaction contract's keyboard rules apply |
| Blocked deletes frustrate users | Dependents list, "Show usages", precise inline text, and the energy-reference action when it is the only dependency; no silent cascade |
| A site edit silently invalidates a KMC path or MMC site reference that the Core validator does not check | The usage index resolves paths through `TransitionPathResolver` and blocks the delete; stability changes warn; the full pipeline reports the residual diagnostics (§6/D-12) |
| The layout choice is re-litigated in implementation | ADR 0003 and §3 fix the surface; a change re-opens the ADR, not the step |
| Divergence from the CLI edit path | Editors call the same `ProjectEditor` operations through the structural overload; the acceptance test compares behaviour on a real project |
| Settings churn or a lost editor fraction | One global `editorFraction` owned by the shell layout state with one serialized writer; Reset Layout and Apply both reach open editors (D-9) |

### 16. Complexity class and split decision

Recorded 2026-11-22 at the split; re-recorded 2026-11-23 at this freeze. A class never moves down after the
freeze without an owner decision.

**Design class: Full.** [`design-proportionality.md`](../../development/design-proportionality.md) §1 triggers:

1. **Architecture boundary.** The shared editor framework — table surface and column policy, detail-editor base,
   reference picker, usage index, navigation/inspector wiring — is the editing boundary Step 25 builds on and
   Steps 26–28 inherit. A late change rewrites every later editor.
2. **Irreversibility.** The ADR 0003 layout pick and the framework's interaction rules are expensive to reverse
   once the model domains are built on them; the POC exists precisely because that cost is real.
3. **Public API.** The additive `ProjectDocument.Apply` overload (§10) is a public Core surface.

No format, dependency, migration, or security/concurrency trigger applies. The round-2 corrections do not
change the class: M-1 is resolved by dropping an element the existing API cannot supply (no new Symmetry API), and
the `Mocassin.Symmetry` reference is an in-repo project reference to a merged step, not a package or external
dependency. Review depth: the full checklist, stopping when no Blocker or Major remains.

**Complexity class: High.** Measured against [`04-roadmap.md` §3.1](../04-roadmap.md#31-step-sizing-and-split-triggers):

| Dimension | Class | Evidence |
| --- | --- | --- |
| Contracts, formats, and public surfaces | **Medium** | One additive Core overload (§10) and one additive app-local setting; no format, manifest, diagnostic-code, or CLI change |
| New dependencies | **Low** | None; the table is an in-repo composition and no package is added |
| Cross-step interfaces | **High** | The editor framework, table surface, and reference/expression primitives are consumed by Steps 25–28 |
| Irreversibility | **Medium–High** | The ADR-recorded layout and the framework's interaction rules are costly to reverse after six domains exist |
| Test-matrix breadth and failure modes | **High** | Five documents × (view-model, real headless view, keyboard, accessibility, screenshots, undo/validation, drag-and-drop) on the 23A/23B harness |
| Independent acceptance stories | **Medium** | Particles/sets and structure/sites are demonstrable separately, but they are one authoring half and share one acceptance run |
| Evidence size for a fresh auditor | **Medium** | Six features, five documents, one API, one setting, screenshot baselines, and one performance record |

**Class: High** (highest dimension: cross-step interfaces and test-matrix breadth). The split already removed the
transition half, the third acceptance story, and the compile-preview evidence from this step.

**Owner acceptance is required** for this High step at the start gate
([§3.1](../04-roadmap.md#31-step-sizing-and-split-triggers)). **No further split is proposed:** separating
particles/sets from structure/sites would leave the framework's acceptance proven only by the simplest domain,
duplicate the harness and screenshot baselines, and produce two Medium-evidence steps that are each still High on
cross-step interfaces — more overhead without a smaller audit surface.

24B is a separate step (six features) and is not part of this list.

### 17. Decisions recorded and owner input

| ID | Decision | Rationale |
| --- | --- | --- |
| D-1 | Layout: option 1 table + detail composed with option 4's splitter; matrix control inside the detail pane | Owner decision 2026-11-22 on the POC evidence; recorded in ADR 0003 |
| D-2 | Site deletion is **blocked** by the usage-index dependents list with navigation; the only offered cascade is **"delete site and energy references"**, enabled solely when the remaining dependents are energy references | **Corrected in round 2 (m-2):** `RemoveCellSiteAndEnergyReferences` *does* exist for energy references, but there is no cascade for MMC site ids, KMC resolved paths, or occupation exchanges. A full cascade would need new Core APIs and could remove rules the user cannot see yet. The block is the index's, because the structural validator does not check MMC site ids or KMC paths |
| D-3 | The set detail carries its own "Project particles" drag source | Cross-document drag is fragile; the workflow and its keyboard equivalent stay in one document |
| D-4 | `ModelEditorViewModel` is `ObservableObject`; `ModelDetailViewModel` is `ObservableValidator` | Field values are what validate; a validator on the list base would validate nothing |
| D-5 | Dummy positions are their own document; the P1 preview is a panel toggle, not a tab | Nested tabs were rejected by the layout POC; the shell already has document tabs |
| D-6 | The numeric expression has no trigonometric functions in v1, and exponent notation is part of a number (`1e5`) while the bare identifier `e` is the constant; there is no implicit multiplication | The degrees/radians convention would be a guess; the lexing rule (m-6) removes the `e` ambiguity and is tested |
| D-7 | Editor gestures are document-local; the global catalog holds only open/navigation commands | Two editors must not claim the same global key |
| D-8 | One additive Core `ProjectDocument.Apply` overload whose delegate receives the document's `ProjectEditor`; the typed `ProjectEditor` operations remain the edit API | Single validation, the CLI's exact operations, one write path; the structural delegate and identity guards close the trust gap (m-7), and `PublicAPI.Unshipped.txt` + `verify.sh api` are the API gate |
| D-9 | One global `editorFraction`, owned by the shell layout state with one serialized writer; editors observe it and never write settings directly | Bounded settings surface, and Reset Layout/Apply reach open editors without a second writer (m-9) |
| D-10 | The site-table virtualization acceptance uses exactly 1,000 sites; the >1,000 case uses P1 positions | `MaxReferenceSites` = 1,000 and `STR012` rejects more (POC finding 2) |
| D-11 | The P1 preview shows positions and indices only — no generating-operation label | No Symmetry preview API exposes the operation; re-deriving it in a view model would duplicate exact symmetry (M-1). A labelled operation needs a later Symmetry preview API and an owner-approved design change |
| D-12 | The usage index resolves KMC reference paths through `TransitionPathResolver` and attributes each point to its owning site; it also counts MMC site ids, occupation exchanges/dopings, and energy references | The structural validator deliberately leaves resolved-path and MMC site rules to the Symmetry validator, so the index is the only pre-apply view of those dependencies (m-2/n-1) |
| D-13 | A bulk action is one composed `Func<ProjectEditor, Project, ProjectEditResult>` with all-or-nothing semantics and one undo entry | `ProjectEditor` has no bulk operation; a partial batch would be a half-applied edit (m-8) |
| D-14 | `ProjectChanged` is handled by kind: `Saved` never invalidates a form; other-domain edits refresh options only; the open object's edit shows a conflict; `Replaced` rebuilds | A save or a foreign edit must not warn about an unchanged form (m-10) |

**Implementation-time resolutions (accepted by the owner 2026-11-23; audit-verified).**

- **m-13 — bulk composition.** The one `ProjectChange` is constructed in the **Core
  `ProjectDocument.Apply(string, Func<ProjectEditor, Project, ProjectEditResult>)` overload** (the permitted
  construction place), not in a view model. The app-side fold (`BulkEditComposer`) runs the typed operations over
  a candidate and returns the last successful typed result; it never constructs a `ProjectChange`. A mid-batch
  rejection returns the **document project** with `Change == null` and the accumulated diagnostics, so project,
  undo, redo, save point, and dirty state are untouched. The overload's identity guard requires
  `ReferenceEquals(result.Change.After, result.Project)` (a malformed result is an `InvalidOperationException`) and
  always records `ProjectChange(description, documentProject, result.Project)` with the caller's description.
  Evidence: `ProjectDocumentApplyOverloadTests.ComposedOperationRecordsOneCanonicalChange...` and
  `ModelEditorFrameworkTests.BulkEditIsOneUndoEntryAndAPartialRejectionChangesNothing`.
- **m-15 — unstable-environment dependents.** `ModelUsageIndex` adds the `UnstableEnvironment` itself through its
  own `SiteId` before walking its filters, so a filterless environment is still a dependent and the
  energy-reference-only cascade is offered. Evidence:
  `ModelUsageIndexTests.AFilterlessUnstableEnvironmentIsADependentThroughItsSiteId` (the ceria sample's environment
  has no filters).
- **m-14 — site-overlap gate (resolved in feature 4; hardened after audit F-1).** `CellSiteDetailViewModel` validates
  the candidate `StructureModel` with `IStructureValidator` before Apply **synchronously**: the 250 ms preview is
  display-only, and `ApplyCore` recomputes the candidate validation so a field edit made inside the debounce window
  cannot commit an overlap. A candidate whose symmetry expansion overlaps (`STR007`) has no preview because
  `TryCreatePreview` returns `null`; the form therefore shows the `STR007` diagnostics inline, shows no non-existent
  orbit rows, refuses Apply, and keeps `ApplyCommand` disabled until the current candidate is validated. Other
  candidate errors (`STR003`, `STR005`, `STR006`, `STR004`) are shown the same way. Evidence:
  `CellSiteEditorTests.AnOverlappingCandidateIsShownAndRefused` (synchronous editor) and
  `CellSiteEditorTests.TheProductionDebouncedFormRefusesAnOverlapBeforeThePreviewRuns` (real `SitesEditorViewModel`
  with a manual clock and inline dispatcher), plus the preview-index test
  `TheTableAndP1PreviewMatchTheEngineData`.

**Owner input needed at the 24A start gate**

1. Accept **class High** as one unit with no further split (§16).
2. Approve the six-feature list below.
3. Confirm D-2 as revised in round 2: a blocked delete with a dependents list, plus the **energy-reference-only**
   cascade. The earlier "no cascade at all" premise was wrong (`RemoveCellSiteAndEnergyReferences` exists); the
   approved scope now includes that one bounded cascade and nothing wider. D-2, D-3, D-5, D-6, and D-11 reshape or
   narrow the indicative sketch; the other decisions are internal choices with no scope effect.
4. Approve the feature-branch list and the step-branch name.

### 18. Closeout and audit plan

- **Feature list (proposed).** Each branch is `feature/step-24a-model-editors-particles-and-structure/<slug>` from
  `step/24a-model-editors-particles-and-structure`.

| # | Feature | Slug | Scope | Done when |
| --- | --- | --- | --- | --- |
| 1 | Shared editor framework | `shared-editor-framework` | ADR 0003 surface: `ModelTableSurface`, `ModelColumn` policy, scroll gutter, tab-order fix, multi-select; `ModelEditorDocumentViewModel`/`ModelEditorViewModel`/`ModelDetailViewModel`; `ReferencePickerViewModel`; `NumericExpression`; `ModelUsageIndex` (path resolution included); inline validation summary; empty states; keyboard map; `AddMocassinModelEditors` with the direct Symmetry reference and preview/validator registration; the additive Core overload and coordinator overload (with `PublicAPI.Unshipped.txt` and `verify.sh api`); the shell-owned `editorFraction`; document kinds and navigation/explorer/inspector/command registration for the 24A domains | CRUD and undo work headlessly on a real project through the framework; `Home`/`End` → `Tab` reaches the detail pane; a diagnostic navigates into an editor and focuses the offending field; the usage index agrees with a blocked-delete rejection; `verify.sh api` passes with the new unshipped member |
| 2 | Particle and set editors | `particle-and-set-editors` | Particles table (pinned read-only void, usage counts, charge/symbol/vacancy warnings, blocked delete with dependents, duplicate, bulk charge); sets with drag and keyboard membership, default-occupation badge, empty-set error, occupation chips | The particle/set acceptance rows pass; drag and keyboard-only flows produce the same result |
| 3 | Space group and cell parameters | `space-group-and-cell-parameters` | Searchable space-group picker (number/HM/Hall/crystal system), settings, operation details, compatibility and orbit-size preview before apply; cell parameters with expressions, crystal-system locks, live vectors/volume, geometry diagnostics, resets | A space-group change previews the new orbits and overlaps before applying; locks match `CellGeometry` for every crystal system |
| 4 | Site table and P1 preview | `site-table-and-p1-preview` | Cell-site table; add/edit with expressions and reference pickers; P1 preview (orbit rows with position, Cartesian, `P` index and orbit-member index; whole-structure list; 2D projection) with overlap warnings; dependency-aware deletion (blocked list plus the energy-reference-only action); stability warnings | Overlaps are shown inline and the candidate is refused; the preview's indices equal the engine's orbit/position data and no operation label is rendered |
| 5 | Dummy positions and structure summary | `dummy-positions-and-structure-summary` | Dummy-positions document with distinct glyph; structure summary (reference-site count, P1 position count, stable/unstable **position** counts, cell volume, reference-site density from `Sites/volume`, orbit table, large-P1 warning), all from `StructureModel` and `StructurePreview` | Summary values equal the values recomputed from `StructureModel.Sites` and `StructurePreview`; no `ModelStatistics` call; dummies render distinctly and round-trip |
| 6 | 24A tests, documentation, and closeout | `tests-docs-and-closeout` | The §13 suite, the §12 documentation, **the parity-matrix rows for the 24A/24B split mapped to the delivering half** (n-2), status updates, `docs/planning/closeout/step-24a.json` (`"step": "step-24a"`), the pre-audit gate, and the audit request | `./tools/verify.sh all --require` passes; the strict docs build passes; `check_closeout_evidence.py` validates; the parity rows name 24A/24B; the audit request is complete |

- **Pre-audit gate.** The dev maps every ticked acceptance criterion to its evidence, confirms that the step page,
  dashboard, feature log, and closeout evidence agree, **maps the parity-matrix rows to 24A/24B** (n-2),
  **confirms the POC report and PNGs are on `main`** before the start gate (m-11), runs the
  structure/link/secret/licence checks, and records the design class and any waiver in the step page
  ([`design-proportionality.md` §3](../../development/design-proportionality.md#3-pre-audit-gate)).
- **Platform gates.** Windows and macOS are required because the step touches GUI keyboard, focus, drag-and-drop,
  and screenshot behaviour; the platform gate is recorded in the audit request. Ubuntu is the closeout run.
- **Audit** is independent and covers the frozen branch; findings are recorded with origin (**design**,
  **implementation**, **evidence**, **documentation**).

## Acceptance criteria

- [x] The layout is used as recorded in ADR 0003: the table selects, the splitter resizes the detail pane, and the
      detail pane edits. No new grid/control dependency is added. — `ModelTableSurfaceTests`,
      `ModelEditorScreenshotTests`, ADR 0003.
- [x] A user can author the ceria particles, sets, structure, and sites **from an empty project**
      (`samples/blank`) without the CLI, and the authored values match the ceria sample. (The oxygen migration is
      24B's criterion; 24A does not claim it.) — `ModelEditorAcceptanceTests` (full authoring and undo-to-blank).
- [x] Drag-and-drop and keyboard-only flows both work for particle-set membership and produce the same order. —
      `ParticleSetEditorTests.DragAndKeyboardPathsProduceTheSameOrder`.
- [x] Changing the space group previews the new orbit sizes and overlaps **before** applying, and cancelling
      changes nothing. — `SpaceGroupPreviewTests`.
- [x] Overlapping sites are shown inline and cannot be applied; the rejection changes nothing. —
      `CellSiteEditorTests.AnOverlappingCandidateIsShownAndRefused` (m-14).
- [x] The P1 preview shows positions with their canonical `P` index and orbit-member index, matches the engine's
      orbit/position data, and renders no generating-operation label (M-1). — `CellSiteEditorTests.TheTableAndP1PreviewMatchTheEngineData`.
- [x] Every particle/structure diagnostic navigates to the offending field by keyboard and by click. —
      `ModelEditorFrameworkTests.ADiagnosticNavigatesIntoAnEditorAndRequestsTheField`,
      `EditorIntegrationTests` (all five domain providers and a stale id), and the `NavigationRequested` relay
      covered by `CellSiteEditorTests.OpeningADependentRequestsShellNavigation`.
- [x] Undo/redo works across every 24A edit, and the save point stays correct. — `ModelEditorFrameworkTests`,
      `ParticleEditorTests`, `ParticleSetEditorTests`, `SpaceGroupPreviewTests`, `CellParameterTests`,
      `CellSiteEditorTests`, `DummyPositionTests`, `EditorRefreshTests`, and the Core
      `ProjectDocumentApplyOverloadTests`.
- [x] `Tab` reaches the detail pane from the selection surface after `Home`/`End`; the surface is one tab stop. —
      `ModelTableSurfaceTests.HomeOrEndThenTabReachesTheDetailPane`.
- [x] The site table stays aligned and virtualizes exactly 1,000 sites; a preview of more than 1,000 P1 positions
      is bounded and cancellable. — `CellSiteEditorTests.OneThousandSitesVirtualizeAndTheLastRowIsSelectable`,
      `CellSiteEditorTests.APreviewOfMoreThanOneThousandPositionsIsVirtualized`, and
      `StructurePreviewSchedulerTests` (coalescing and superseded/disposed schedules).
- [x] Read-only sessions disable every edit path and show the 23B banner, proved by `EditorReadOnlyTests`. —
      `EditorReadOnlyTests.AReadOnlySessionShowsTheBannerAndDisablesEveryEditPath` and
      `EditorReadOnlyTests.TheStructureFormShowsTheBannerAndDisablesBothForms` (rendered banner, disabled commands,
      forced `ReadOnly` apply), plus the per-domain `ParticleEditorTests.ReadOnlySessionDisablesEveryParticlePath`,
      `ParticleSetEditorTests.ReadOnlySessionDisablesSetEdits`, `CellParameterTests.ReadOnlyDisablesBothStructureForms`,
      `CellSiteEditorTests.ReadOnlyDisablesTheSiteFormAndActions`, `DummyPositionTests.ReadOnlyDisablesTheDummyDocument`.
- [x] Documentation written (the §12 pages). — `particles.md`, `structure.md`, `ui-architecture.md`,
      `ui-interaction-contract.md`, `errors.md`, and `tutorials/ceria-model.md`; strict docs build below.

## Pre-audit gate and closeout record

Recorded 2026-11-24 by the dev (`dev`; `dev-free` free-limit fallback); re-run 2026-11-24 after the audit
round-1 fixes. Design class: **Full**; no waiver.

| Check | Result |
| --- | --- |
| Every ticked acceptance criterion maps to evidence | Mapped below |
| Step page, dashboard, feature log, closeout evidence agree | Step page/dashboard/feature log updated; closeout evidence binds `10c7206` |
| Structure / link / secret / licence checks | `./tools/verify.sh all --require` green at `10c7206` (App 467 tests; structure/docs/policy; AOT evidence recorded) |
| Design class / waiver recorded | **Full**, no waiver |
| POC report and PNGs on `main` (m-11) | Present on `main` at `3e904ed` (POC merge `3e904ed`) |
| Parity rows mapped to 24A/24B (n-2) | 24A rows name the particle/set/structure/dummy/P1/summary/framework work; transition/editor rows name 24B |
| `python tools/check_closeout_evidence.py docs/planning/closeout/step-24a.json` | **Pass** — `Closeout evidence: OK` at the frozen audited commit `10c7206`. |

**Evidence map**

| Acceptance criterion | Evidence |
| --- | --- |
| ADR 0003 layout, one table/splitter/detail, no grid dependency | `ModelTableSurfaceTests`, `ModelEditorScreenshotTests`, ADR 0003 |
| Author the ceria particles, sets, structure, and sites from `samples/blank` | `ModelEditorAcceptanceTests.CeriaParticlesAndSetsAreAuthoredFromBlankAndFullyUndone` (full structure/site authoring and undo-to-blank) |
| Drag and keyboard set membership produce the same order | `ParticleSetEditorTests.DragAndKeyboardPathsProduceTheSameOrder` |
| Space-group preview/cancel | `SpaceGroupPreviewTests` |
| Overlap inline and refused (synchronous and debounced production paths) | `CellSiteEditorTests.AnOverlappingCandidateIsShownAndRefused` (m-14), `CellSiteEditorTests.TheProductionDebouncedFormRefusesAnOverlapBeforeThePreviewRuns` (F-1) |
| P1 preview indices and no operation label | `CellSiteEditorTests.TheTableAndP1PreviewMatchTheEngineData` |
| Diagnostic navigation | `ModelEditorFrameworkTests`, `EditorIntegrationTests`, `CellSiteEditorTests.OpeningADependentRequestsShellNavigation` |
| Undo/redo and save point | `ModelEditorFrameworkTests`, `ParticleEditorTests`, `ParticleSetEditorTests`, `SpaceGroupPreviewTests`, `CellParameterTests`, `CellSiteEditorTests`, `DummyPositionTests`, `EditorRefreshTests`, `ProjectDocumentApplyOverloadTests` |
| `Home`/`End` → `Tab`, one tab stop | `ModelTableSurfaceTests.HomeOrEndThenTabReachesTheDetailPane` |
| 1,000-site table; >1,000 P1 positions bounded; coalescing | `CellSiteEditorTests.OneThousandSitesVirtualizeAndTheLastRowIsSelectable`, `CellSiteEditorTests.APreviewOfMoreThanOneThousandPositionsIsVirtualized`, `StructurePreviewSchedulerTests` |
| Read-only paths | `EditorReadOnlyTests` (rendered banner, disabled commands, forced `ReadOnly` apply), `ParticleEditorTests`, `ParticleSetEditorTests`, `CellParameterTests`, `CellSiteEditorTests`, `DummyPositionTests` |
| Editor accessibility | `EditorAccessibilityTests` (control names, table list role, textual severity, named/keyboard-resizable splitter, committed pane fraction) |
| Editor performance (non-gating numbers in §13) | `EditorPerformanceTests` (100 vs 1,000 realized containers, all rows, last row selectable, coalescing, no dropped rows) |
| Documentation | `particles.md`, `structure.md`, `ui-architecture.md`, `ui-interaction-contract.md`, `errors.md`, `tutorials/ceria-model.md`; strict docs build |

**Closeout status.** `./tools/verify.sh all --require` is **green at `10c7206`** (App 467 tests; docs, structure,
policy, contracts, secret/licence checks, AOT evidence). The round-2 closeout CI run
[45491888826](https://github.com/seb-eis/mocassin-2/actions/runs/45491888826) and the Windows/macOS platform gates
[45492513811](https://github.com/seb-eis/mocassin-2/actions/runs/45492513811) /
[45493153286](https://github.com/seb-eis/mocassin-2/actions/runs/45493153286) are green at
`10c72066bd71e121c9deef7fb92a6b6efe14be3a`. `python tools/check_closeout_evidence.py
docs/planning/closeout/step-24a.json` reports `Closeout evidence: OK`. The audit record
[`../audits/step-24a.md`](../audits/step-24a.md) is `Re-audit required`; the branch is frozen at the round-2
audited commit for the review agent.

## Documentation

See §12. The user-guide pages are updated (not newly created): `particles.md` and `structure.md` already exist as
reference pages and gain the editor workflows. `ui-architecture.md` gains the framework section;
`ui-interaction-contract.md` gains the master-detail tab-order rule if it is not already covered; `errors.md`
gains the inline-error versus diagnostic-code guidance.

## Tests

See §13. Every acceptance criterion names its test there; the property tests cover the expression parser, the
column/lock tables, and the usage index; the acceptance test uses the real ceria sample and the real
`ProjectEditCoordinator`; only the clock, the UI dispatcher, and the OS file-picker/dialog boundary are test
doubles.

## Risks and mitigations

See §15.

## Done when

A user can author particles, sets, space group, cell, sites, dummy positions, and read the structure summary in a
responsive, keyboard-complete editor with undo, inline validation, and precise navigation from every diagnostic —
and every later model editor can reuse the same surface without rewriting it.
