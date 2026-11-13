# ADR 0003 — Avalonia MVVM Stack

- **Status:** Accepted — revised 2026-11-13
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
  current item. The baseline is a virtualized `ListBox` table and a `ContentControl` whose `DataTemplate` is
  selected by the editor view-model type. The final layout is chosen after the Step 24 layout spike.
- **No general-purpose data-grid dependency.** Do not adopt `Avalonia.Controls.DataGrid`, `TreeDataGrid` Pro,
  or a third-party grid unless a later ADR records the owner decision and the dependency review.
- **LiveCharts2** (`LiveChartsCore.SkiaSharpView.Avalonia`) for scientific charts, with SkiaSharp for headless
  rendering/export. Its Avalonia 12 compatibility must be verified before Step 27; the fallback is a small
  SkiaSharp-based chart control.
- **Silk.NET.OpenGL** behind a renderer abstraction for 3D (see ADR 0006).

## Model editor layout options

These are the POC options for the Step 24 spike. The spike is a gated decision; the owner reviews the POCs
before the final editor layout is frozen.

| Option | Shape | Strengths | Weaknesses |
| --- | --- | --- | --- |
| **ListBox table + detail editor** | Header `Grid` + virtualized `ListBox` rows; `ContentControl` shows the selected item's editor | Built-in controls, selection/keyboard, virtualization, clean MVVM, no dependency | Sorting/filtering and column sizing live in the view model; no built-in resize/reorder |
| **TreeView + detail editor** | Hierarchical `TreeView` for model/domain navigation; detail editor on the right | Good for model structure and nested objects; built-in | Weak for dense tabular comparison and cross-object sorting |
| **TabControl + list + detail** | One tab per domain; each tab has a table/list and detail editor | Keeps large model screens manageable; familiar navigation | More clicks; cross-domain context is split |
| **SplitView / GridSplitter master-detail** | Resizable list and editor panes in one workspace | Users can allocate space; good for wide editors | Still composition work; no table features |
| **Custom TableView `TemplatedControl`** | Reusable table surface with headers, selection, sorting hooks, virtualization | Consistent across editors; can add resize/reorder later | Must implement and test selection, accessibility, headers, and column policy |
| **Custom matrix control** | Dedicated site-energy matrix, not a general table | Correct UX for a matrix; virtualization/cell editing can be specialized | Only for matrix-shaped data |

Baseline until the spike closes: **ListBox table + detail editor**. The table is a selection surface only;
editing happens in the type-specific editor. This keeps complex validation, undo/redo, and reference pickers out
of inline cells.

## Avoid

- **`Avalonia.Controls.DataGrid`** — deprecated for this target; do not build new editors on it.
- **`TreeDataGrid` Pro / any paid Avalonia control** — requires a paid license.
- **Third-party grid controls** — adds a dependency, styling integration, licensing review, and upgrade risk for
  a small set of model editors.
- **Inline cell editing for complex model objects** — it fights validation, undo/redo, and type-specific editors.
- **`ItemsControl` inside a plain `ScrollViewer` for large lists** — no virtualization.
- **Two independently auto-sized `Grid` layouts for header and rows** — columns drift. Use fixed/star widths or
  one shared column-width policy.
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
- The Step 24 spike is required before the final editor design is frozen.
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
- The Step 24 layout spike is a gate: do not freeze the final editor layout before the owner reviews the POCs.
