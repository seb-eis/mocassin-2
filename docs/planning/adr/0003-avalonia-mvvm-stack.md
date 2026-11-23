# ADR 0003 — Avalonia MVVM Stack

- **Status:** Accepted — revised 2026-11-22 (the Step 24 editor layout is decided; see
  [Editor layout decision](#editor-layout-decision-2026-11-22))
- **Date:** 2026-11-02
- **Deciders:** Project owner

## Context

The original GUI is a 30k-line WPF application with a hand-written MVVM layer, a service-locator
(`IProjectAppControl`) passed to every view model, views stored inside view models, `MessageBox` dialogs,
`Console.WriteLine` logging, and WPF-only command/validation plumbing.

The new GUI must run on Windows, Linux, and macOS, support a large scientific editor workflow, and remain
testable without a display.

The original plan used `Avalonia.Controls.DataGrid` for tabular editors. That is no longer a good foundation:
the Avalonia data grid is deprecated for this target, the maintained `TreeDataGrid` is a Pro/paid control, and
the project does not want a paid control or a third-party grid dependency. The model editors are not
spreadsheet-style grids; they are lists of model objects with a rich, type-specific editor.

## Decision

Use:

- **Avalonia UI 12.x** as the UI framework. Pin `Avalonia`, `Avalonia.Desktop`, `Avalonia.Fonts.Inter`, and
  `Avalonia.Themes.Fluent` to the same 12.1.x version in `Directory.Packages.props`.
- **CommunityToolkit.Mvvm 8.x** for observable properties, commands, and validation.
- **Microsoft.Extensions.DependencyInjection/Hosting/Configuration/Logging** for composition and services.
- **Serilog** (or the Microsoft logging file sink) for structured file logging.
- **ViewLocator/DataTemplate** resolution instead of storing views in view models.
- **Explicit constructor injection**; no service locator in view models.
- **Master-detail model editors:** a table/list selection surface plus a type-specific editor selected by the
  current item. Since 2026-11-22 the surface is the `ListBox` table composed with a resizable detail pane
  ([Editor layout decision](#editor-layout-decision-2026-11-22)).
- **No general-purpose data-grid dependency.** Do not adopt `Avalonia.Controls.DataGrid`, `TreeDataGrid` Pro,
  or a third-party grid unless a later ADR records the owner decision and the dependency review.
- **LiveCharts2** (`LiveChartsCore.SkiaSharpView.Avalonia`) for scientific charts, with SkiaSharp for headless
  rendering/export. Its Avalonia 12 compatibility must be verified before Step 27; the fallback is a small
  SkiaSharp-based chart control.
- **Silk.NET.OpenGL** behind a renderer abstraction for 3D (see ADR 0006).

## Model editor layout options

These were the POC options for the Step 24 spike. The spike was built and reviewed, and the owner picked the
layout on 2026-11-22; the decision is recorded below and is the normative editor layout from Step 24 onward.

| Option | Shape | Strengths | Weaknesses |
| --- | --- | --- | --- |
| **ListBox table + detail editor** | Header `Grid` + virtualized `ListBox` rows; `ContentControl` shows the selected item's editor | Built-in controls, selection/keyboard, virtualization, clean MVVM, no dependency | Sorting/filtering and column sizing live in the view model; no built-in resize/reorder |
| **TreeView + detail editor** | Hierarchical `TreeView` for model/domain navigation; detail editor on the right | Good for model structure and nested objects; built-in | Weak for dense tabular comparison and cross-object sorting |
| **TabControl + list + detail** | One tab per domain; each tab has a table/list and detail editor | Keeps large model screens manageable; familiar navigation | More clicks; cross-domain context is split |
| **SplitView / GridSplitter master-detail** | Resizable list and editor panes in one workspace | Users can allocate space; good for wide editors | Still composition work; no table features |
| **Custom TableView `TemplatedControl`** | Reusable table surface with headers, selection, sorting hooks, virtualization | Consistent across editors; can add resize/reorder later | Must implement and test selection, accessibility, headers, and column policy |
| **Custom matrix control** | Dedicated site-energy matrix, not a general table | Correct UX for a matrix; virtualization/cell editing can be specialized | Only for matrix-shaped data |

The table is a selection surface only; editing happens in the type-specific editor. This keeps complex validation,
undo/redo, and reference pickers out of inline cells, and it survives the pick.

### Editor layout decision (2026-11-22)

**Decision.** The model editors use **option 1, the `ListBox` table + detail editor, as the selection-surface
base**, composed with **option 4's `GridSplitter` master-detail**: the selection table | a focusable splitter |
the type-specific detail pane. The table keeps one shared column/header helper and the production 20 px scroll
gutter; the splitter makes the detail pane resizable within documented minima. The custom matrix control
(option 6) is used inside the detail pane where matrix-shaped data appears; it is not a layout and not a
separate window shape.

**Why this composition.**

- Option 1 met every §0.2 requirement of the Step 24 spike at the lowest marginal size (59 option lines on top
  of the shared surface), with a 1,024-row build of 84 ms — within the 29–84 ms band of the four non-tree
  options and about 18× faster than the 1,521 ms tree — and exact header/row alignment through the shared width
  policy plus the scroll gutter.
- Option 4 adds the one thing option 1 lacks that the Step 24 detail panes need: width for the occupation and
  exchange matrices. It changes only the container around the same table surface, so the two compose without a
  second table implementation.
- The options that add a different surface lost on measured evidence, not taste: the tree realized 1,058
  containers for 1,024 rows and took 1,521 ms (about 18× option 1) and broke header/row alignment by
  construction through indentation; the nested tab layout added a second tab level under the shell's document
  tabs and a second `Ctrl+Tab` owner without solving cross-domain comparison; the custom `TableView` cost 333
  lines while still delegating virtualization and row keyboard handling to an inner list.
- **Option 5 remains the honest choice if column resize/reorder becomes a first-release requirement.** It is
  deferred, not rejected: the shared column policy makes it addable behind the same surface without rewriting
  the editors. A later step that needs resize/reorder must re-open this ADR rather than fork the table.

**Evidence.** The out-of-step POC report and its bounded PNG subset are the durable decision input:
`docs/planning/maintenance/2026-11-22-step-24-layout-poc.md` with
`docs/planning/maintenance/assets/step-24-layout-poc/`. They live on `fix/step-24-layout-poc` until that branch's
**documentation-and-assets-only** owner-approved `--no-ff` merge under [`AGENTS.md` §3.5](../../../AGENTS.md) lands
them on `main`; the spike code was already removed there (`fbdae6c`). **The Step 24A start gate is conditional on
that merge**: a fresh auditor must be able to read the decision input from `main`. The report is decision-input
evidence, not a normative design. The spike code is **discarded** by owner decision (2026-11-22) and does not
seed the implementation: Step 24 re-derives the surfaces from its frozen step page and this ADR.

**Consequences of the pick.**

- The editor detail pane is a resizable region, so it needs a persisted **fraction** rather than a fixed width
  ([`ui-interaction-contract.md` §6](../../development/ui-interaction-contract.md#6-shell-layout)). The fraction
  is app-local settings state (`layout.editorFraction`), additive with no schema bump and owned by the shell's
  serialized layout state so that Reset Layout and settings changes reach every open editor.
- Reference pickers are composed from a search box plus a filtered combo box; Avalonia has no searchable combo.
  Keep that one small reusable control instead of repeating two controls per reference field.
- Focus and `Enter`/`Escape` behaviour stays under
  [`ui-interaction-contract.md` §8](../../development/ui-interaction-contract.md#8-rules-for-steps-2428-editors).
  Selecting a row opens or focuses the detail editor; the keyboard map does not replace it.

## Avoid

- **`Avalonia.Controls.DataGrid`** — deprecated for this target; do not build new editors on it.
- **`TreeDataGrid` Pro / any paid Avalonia control** — requires a paid license.
- **Third-party grid controls** — adds a dependency, styling integration, licensing review, and upgrade risk for
  a small set of model editors.
- **Inline cell editing for complex model objects** — it fights validation, undo/redo, and type-specific editors.
- **`ItemsControl` inside a plain `ScrollViewer` for large lists** — no virtualization.
- **Two independently auto-sized `Grid` layouts for header and rows** — columns drift. Use fixed/star widths or
  one shared column-width policy.
- **A `TreeView` as the primary selection surface for a flat, large domain.** Measured in the Step 24 spike: a
  1,024-row tree realized 1,058 containers in 1,521 ms (about 18× the list), and indentation breaks header/row
  alignment by construction. Trees remain valid for small hierarchical surfaces such as domain navigation,
  chain patterns, and reference browsing; they must not be the 1,000-row editor table.

- **Binding editable fields directly to mutable domain objects** — edits must go through `ProjectEditor` and the
  undo/redo/validation pipeline.
- **A general-purpose table control as the first implementation** — start from the built-in composition and
  only extract a `TableView` if the spike shows a real need.

## Consequences

**Positive**

- One UI codebase for all target platforms on a current Avalonia release.
- No paid grid or third-party grid dependency.
- Master-detail editors are simpler to validate, navigate, and keep undoable than inline grids.
- Source-generated MVVM reduces boilerplate and is trimming/AOT friendly.
- Boundary services (dialogs, process launcher, clock) are replaceable; view models are testable headlessly
  against real project documents and domain objects.
- Modern async/cancellation and logging are first-class.

**Negative**

- The project owns the table/list composition: sorting, filtering, column sizing, and keyboard behavior must be
  implemented or standardized.
- Very wide, spreadsheet-like comparisons will be less capable than a dedicated data grid.
- The Step 24 layout spike is complete; the measured evidence is the durable record, and column resize/reorder
  remains unimplemented until a step needs it.
- Avalonia's ecosystem is smaller than WPF's; some controls need custom implementations.
- 3D rendering must be built or integrated, not inherited from HelixToolkit.Wpf.SharpDX.

## Rules

- No `System.Windows.*`, `Microsoft.Win32`, or WPF types in shared/view-model projects.
- View models never reference views or controls.
- All dialogs and file pickers go through interfaces (`IDialogService`, `IStorageService`).
- UI state that must persist is stored in the project's `viewSettings` or app settings, not in ad-hoc files.
- Long operations take a `CancellationToken` and report `IProgress<T>`.
- Every user-visible command has a keyboard-accessible path and an automation-friendly name.
- No new UI control dependency without an ADR that records the owner decision, licence, maintenance risk, and
  Avalonia 12 compatibility.
- The Step 24 layout spike was a gate: the owner reviewed the POCs on 2026-11-22 and the layout is recorded in
  [Editor layout decision](#editor-layout-decision-2026-11-22). Do not re-open the layout choice in a step; a
  change of table surface or a new control dependency re-opens this ADR.
