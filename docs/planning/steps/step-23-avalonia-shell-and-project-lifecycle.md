# Step 23 — Avalonia Shell and Project Lifecycle

**Phase:** F. GUI
**Depends on:** 01, 03, 04, 05, 06, 07, 12 (all `Done`, merged, and tagged). Not dependent on 02/08–11 and
**not** on Steps 13–22.
**Delivers:** The Avalonia application shell, the composition root, the document/navigation model, project
open/create/save/save-as/close with recent files, locks and external-change detection, dirty tracking,
autosave and crash recovery, the single-project edit pipeline with undo/redo, the validation/problems surface,
versioned application settings, theming and density, keyboard shortcuts and accessibility metadata, dialogs and
in-app consoles, structured logging with a crash boundary, the headless test harness, and the shell/project
documentation. No model editors, no build UI, no 3D, no run monitoring.

> **Parallel-track note (2026-11-20).** The human owner explicitly started the GUI track in parallel with the
> active solver step (Step 13). This step's design assumes only the library surfaces of Steps 01–07 and 12.
> It must not reference solver, state, or results APIs from Steps 13–22; §18 lists the exact seams that those
> steps plug into later.

## Implementation status

- **Status:** Not started — design frozen for review; no step branch exists
- **Step branch:** — (created from the reviewed `main` commit after the start gate)
- **Step CI:** —
- **Main merge:** —
- **Step tag:** —
- **Audit:** Not requested
- **Design review:** Rounds 1 and 2 returned `Changes required` (round 1 at `9916166`: M-1–M-3, m-1–m-11,
  n-1–n-8; round 2 at `1b82fde`: M-4, m-12–m-17, n-9–n-13, all recorded in `docs/planning/reviews/step-23.md`).
  Those findings are resolved in this revision; the record state is `Re-review required`. The round-3 freeze is
  the commit that introduces this revision; the request record carries its SHA.
- **Feature list:** Proposed (eight dependency-ordered features, grouped 1–4 / 5–8 for the split decision) —
  requires the single owner approval described in [`AGENTS.md` §3.2](../../../AGENTS.md)
- **Complexity class:** **High** — assessed against the seven §3.1 dimensions on 2026-11-20; the highest
  dimension is High on cross-step interfaces, test-matrix breadth, independent acceptance stories, and evidence
  size. Class, evidence, and justification are recorded in §23.
- **Split decision:** **Pending owner decision at the start gate.** §23 proposes a dependency-clean split after
  feature 4 (23A = features 1–4, 23B = features 5–8) and recommends it; the alternative is owner acceptance of
  the step as one High unit, with the acceptance case and risk guard recorded in §23. Person-weeks are a
  non-gating forecast (roadmap §3.1); no time waiver is requested.
- **Last updated:** 2026-11-20

| Feature | Branch | Status | Local gates | Step CI | Squash commit |
| --- | --- | --- | --- | --- | --- |
| — | — | — | — | — | — |

### Proposed feature list (requires one owner approval)

Each branch is `feature/step-23-avalonia-shell-and-project-lifecycle/<slug>` from the step branch. The order is
the implementation order: every feature depends only on the features above it, on the merged library surfaces of
01/03–07/12, and on nothing from Steps 13–22. The claimed split boundary (after feature 4) is therefore
dependency-clean: 23B builds on 23A and 23A contains no task from 23B. The earlier draft inverted this — features
3–6 needed the dialog/picker/Problems surface that only a later feature delivered, which round 1 recorded as
M-2; the reorder below is the fix, and the dialog/picker/console/notification/progress surfaces now land in
feature 1.

| # | Grp | Feature | Slug | Planned scope | Done when |
| --- | --- | --- | --- | --- | --- |
| 1 | A | Host, composition, boundaries, and surfaces | `app-host-and-boundaries` | Avalonia app project (`Program`, `App.axaml(.cs)`, `ViewLocator`), `IHost` composition root, explicit registration, `TimeProvider`/`IUiDispatcher`, Serilog file+console logging with retention, `--project`/`--log-level`/`--data-dir` startup options, shell window with the four regions and splitters, menu/status skeleton, the in-shell `IDialogService` overlay host (confirm/choose/prompt/error/crash *view*), `IStorageService` pickers whose last-directory state is an **in-memory/default** seam in feature 1 (feature 3 wires it to the persisted settings model), a console host with the Application channel at the default log level (feature 3 wires the settings level), notification host, progress surface, global exception handlers (log + crash dialog; recovery writing behind `IRecoveryWriter` with a null implementation until feature 7), the internal-only type rule with the documented public marker/entry-point exceptions | The app starts and renders the shell headless; DI resolves every registered service; a dialog round-trips through the real overlay; a forced unhandled exception is logged and reported, not silently fatal; `Mocassin.App.ViewModels` has no Avalonia/`System.IO` use (architecture test) |
| 2 | A | Workspace: documents, navigation, commands | `workspace-documents-and-navigation` | `DocumentViewModel`/`DocumentKey`/`DocumentCollectionViewModel`, tab strip (dirty marker slot, reorder, close, overflow), `INavigationService` with bounded history, `INavigationTargetProvider` registry, project-scoped keys, project switcher, project explorer, welcome document, read-only Project Overview (metadata form present, editing wired in feature 5) and Project JSON documents, command catalog, menus, status items, command palette, shortcut catalog with the framework-neutral gesture type | Tabs open/activate/reuse/close and back/forward work; an unknown navigation domain produces a console notice, never an exception; the palette lists every registered command with its platform gesture |
| 3 | A | Settings, theming, accessibility | `settings-theming-and-accessibility` | `mocassin.settings` v1 model, validator, migrations, source-generated JSON context, and the atomic+fsync+size-capped store (store in `Mocassin.App`, model/pure logic in ViewModels), app-data layout, settings dialog (reset, open config folder, clear logs/cache), theme/density/font-scale/high-contrast/reduce-motion applier in Views, shortcut cheat sheet, accessible names/roles/help text on shell controls | Settings round-trip, defaults-on-invalid, unknown-field tolerance, future-major rejection, and migration pass on real files; the theme/density/contrast switch applies without restart; primary controls expose names/roles |
| 4 | A | Project session lifecycle | `project-session-lifecycle` | `ProjectSession`/`IProjectSessionService` over `ProjectStore` **configured with `ValidateOnLoad = false`** and seeded with `ProjectLoadResult.Diagnostics`; new via `ProjectTemplateFactory` + shared `ProjectPathRules`; open/open-or-activate/save/save-as/close/reload; recents; locks and read-only mode; external-change detection; multi-project workspace; unsaved-changes and window-close coordination; 23A documentation scoped to 23A capabilities (§19) | New → save → close → reopen is content-identical; a second session opens read-only; an external change is detected before overwrite; a load/migration fixture populates the `Load` stage and a clean load leaves it `Skipped` |
| 5 | B | Edit pipeline and undo/redo | `edit-pipeline-and-undo-redo` | Single `ProjectEditCoordinator` per project over `ProjectDocument`: `Apply`/`Undo`/`Redo`, bounded depth, `ProjectChanged`, rejected-edit diagnostics, metadata editing in the Project Overview document (one undo entry per committed field) | Undo/redo crosses the save point correctly in both directions; a rejected edit changes nothing and reports diagnostics; the coordinator is the only write path |
| 6 | B | Validation surface | `validation-surface` | `ValidationCoordinator` over `ProjectValidationPipeline` (in-memory entry with the stored load diagnostics, `ValidationOptions.Default`), 300 ms debounce, stale-result discard, status-bar indicator, Problems channel with severity/code/text filters, sorting, help anchors, `Related` navigation, revalidate, edit diagnostics, on-demand statistics | Load/migration diagnostics reach Problems on open; a stale result is discarded; double-click navigates through a registered provider and produces a notice otherwise; no double validation on open |
| 7 | B | Dirty state, autosave, crash recovery | `dirty-autosave-and-recovery` | Save-point-correct dirty tracking and markers, `AutosaveScheduler` over `TimeProvider`, `.autosave` sidecar for saved projects, recovery store for untitled projects (implementation in `Mocassin.App`), recovery prompt, Reload/Recover/Discard/Keep operations, unclean-exit session restore, retention (logs 14 days/100 MB, recovery 7 days), asynchronous crash-recovery writes with a bounded budget | Autosave writes only dirty documents and deletes its file on save; a simulated crash leaves a recoverable snapshot; recovery restores the exact in-memory project; recovery files are swept after 7 days and sidecars are not |
| 8 | B | Tests, documentation, and closeout | `tests-docs-and-closeout` | Avalonia headless harness, `ManualTimeProvider`/inline dispatcher/recording dialog/stub picker/App-boundary store fakes, visual baselines in three themes, accessibility-tree assertions, platform checklist, the 23B documentation columns and navigation, parity/status/closeout evidence and the audit request | `./tools/verify.sh all --require` passes; the strict docs build passes; closeout evidence validates; the audit request is complete |

The feature list is proposed, not approved. The owner approves it once at the start gate, together with the
split decision in §23; individual features are then squash-merged without further approval. **If the owner
approves the split**, 23A is features 1–4 plus its documentation and its own closeout evidence (added to feature
4), and 23B is features 5–8 with feature 8 as its closeout; the roadmap step index, dependency graph, and the
Step 24 dependency are updated in the same change. **If the owner accepts one unit**, features 1–8 run in order
under one step branch and one closeout.

## Goal

Build the application frame that every later editor plugs into, and make project persistence trustworthy. A
user must be able to create, open, inspect, edit metadata, save, save-as, close, and recover projects;
understand at a glance whether the model is valid and whether the document is saved; and reach every primary
action from the keyboard. The step must feel like a modern scientific IDE without containing any scientific
logic: the shell is a consumer of the same public library APIs as the CLI.

The step deliberately ends before the model editors (24/25), parameterization and build UI (26), the 3D viewer
(27), and run monitoring (28).

## Scope

**In scope**

- The Avalonia application project, its composition root, and the `App`/`ViewModels`/`Views` split.
- The shell layout, regions, splitters, panel visibility, and persisted layout.
- The document model, tab strip, navigation service with history, navigation-target registry, and command
  palette.
- The project explorer tree (project-level nodes only) and the project switcher.
- Two read-only-by-default documents that make the shell usable without editors: Project Overview (with
  editable project metadata) and Project JSON (debug/export view).
- Project lifecycle: new from template, open, save, save-as, close, recent files, locks, read-only mode,
  external-change detection, multi-project workspace.
- Dirty tracking, save point, autosave, crash recovery, unclean-exit session restore.
- The single write path per project (`ProjectEditCoordinator`), undo/redo commands, and bounded undo depth.
- Live validation surfaces: status-bar indicator, Problems channel, diagnostic navigation.
- Application settings (versioned file, migration, defaults, retention), app-data layout, settings dialog.
- Theming (light/dark/system, accent, density, font scale, high contrast), reduce motion.
- Keyboard shortcuts, menus, command catalog, shortcut cheat sheet, accessibility metadata.
- Dialogs, file pickers, in-app consoles, notifications, progress surface, crash dialog.
- Structured logging and the global exception boundary.
- The library additions listed in §16 (four additive, behaviour-preserving changes).
- The headless test harness, visual baselines, documentation, parity/status updates, and closeout evidence.

**Non-goals (explicitly out of scope)**

- **No model editors.** Particles, particle sets, structure/sites/cell/space group, transitions, energy,
  lattice/doping, and simulations are Steps 24–25. The project explorer shows project-level nodes and
  navigation targets that no provider claims; it must not grow ad-hoc editors.
- **No parameterization, job-template, build, or deploy UI.** Step 26 owns them, including the Build menu and
  the Build console channel. Step 23 ships the contribution seams and no `Model`/`Build`/`Run` menu until a
  later step contributes items (§10).
- **No 3D viewer, rendering host, picking, or camera UI.** Step 27. `Mocassin.Rendering` is referenced but not
  used.
- **No solver launcher, run queue, live monitoring, charts, or MMCFE UI.** Step 28. No `ISimulationService`, no
  process launcher, no run-log channel producer.
- **No results, `.msr`, `.mcs`, evaluation, or analysis UI.** Steps 20–22 and 28.
- **No build/compile invocation.** No call into `Mocassin.Packages.Build` or `Mocassin.Compilation` from the
  shell; Step 26 composes the build UI over the existing pipeline.
- **No new public *format*, no manifest change, no schema version bump.** `mocassin.settings` is an
  application-local file, not a cross-language contract; it is documented in
  [`docs/reference/file-layout.md`](../../reference/file-layout.md) and is not registered in
  `contracts/manifest.json` (§17).
- **No change to `.mocprj`, `.msl`, `.msr`, `.mcs`, CLI reports, exit codes, or diagnostics codes.** The
  packages, contracts, and CLI behaviour are consumed as-is; the CLI refactor in §16 must leave every
  externally observable behaviour identical.
- **No new UI control package, no data grid, no paid control.** No `Avalonia.Controls.DataGrid`, no
  `TreeDataGrid` Pro, no third-party grid, no `MsBox.Avalonia`. Dialogs are an in-shell overlay we own
  (ADR 0003 rules).
- **No localization.** Invariant English only; the settings carry a culture field for number formatting, and
  strings are structured so localization can be added later (deferred post-1.0).
- **No auto-update, no telemetry, no remote deploy, no plugin loading.**
- **No platform-specific installer or packaging.** Step 31.
- **No step branch, feature branch, or code in this design change.** Docs only.

## Original reference

Read `/workspace/mocassin-original` as a read-only reference (GPL-3.0-or-later). Do not copy code or data.

- `Mocassin.UI.GUI/MainWindow.xaml` is a fixed three-column/two-row `Grid`: menu bar (row 0), project browser
  (left), work tab control (centre), console tab control (bottom of centre), two `GridSplitter`s, and a status
  bar (last row). The window opens maximized, centred, and sized from `SystemParameters`.
- `MainWindowViewModel` implements `IProjectAppControl`, the service locator that every view model receives. It
  owns the menu bar, status bar, browser, console, work tabs, and project manager view models, creates the
  model project service, and deploys resources on construction.
- `StartupWindow` is an undecorated splash with the logo, attribution, and a "Loading …" line; the library is
  selected before the main window appears.
- Project lifecycle is open/create/close/save over a *library*: one `.mocprj` (SQLite) held multiple projects,
  and the browser is the library. Copying `DbSet.Local` detached the duplicate from the database
  (`Original reference` in the roadmap; do not reproduce).
- Settings are an XML file under `Documents/Mocassin`; the symmetry database path is a hard-coded Windows path.
  Logging is `Console.WriteLine`; dialogs are `MessageBox`; there is no undo/redo, autosave, or crash recovery.
- `ProjectStatusBarView.xaml` is an empty `Grid`: the status-bar slot exists but is never populated.

**Preserved intents:** a single main window with a fixed, resizable region layout; project browser on the
left; tabbed work area with a console beneath it; status bar; a clear loading/empty state before a project is
open; menu-driven commands; keyboard-first operation.

**Corrected in this step:** immutable `Project` values with one validated write path instead of mutable graphs
and a service locator; explicit constructor injection instead of `IProjectAppControl`; one project per file
with a workspace of open files instead of a database library; versioned, migratable settings in the platform
app-data directory instead of XML in `Documents`; undo/redo, autosave, recovery, and structured logging;
dialogs and pickers behind interfaces; no `MessageBox`, no `System.Windows.*`; a populated status bar.

## Design

### 1. Composition root and dependency direction

```csharp
// Mocassin.App/Program.cs
public static class Program
{
    public static int Main(string[] args);          // parses AppStartupOptions, runs the host, returns 0/1
}

// Mocassin.App/App.axaml.cs
public sealed class App : Application
{
    public override void Initialize();
    public override void OnFrameworkInitializationCompleted();
}
```

- `AppStartupOptions` (App/Composition) parses `--project <path>` (open on start), `--log-level <level>`
  (override the settings value for this run), `--data-dir <path>` (override the app-data root; also honours
  `MOCASSIN_DATA_DIR`), and `--no-session-restore`. Unknown arguments exit `2` with usage text on stderr; a
  failed startup exits `1`; a normal exit returns `0`. The GUI has no other exit-code contract.
- `AppHost` builds a `HostApplicationBuilder` (Microsoft.Extensions.Hosting), registers services, builds the
  provider, and owns disposal. Only `Program`/`App` resolve the provider; every view model takes explicit
  constructor arguments.
- Registration is **explicit and enumerable** in `AppServiceRegistration` (`AddMocassinApp`, `AddMocassinShell`,
  `AddMocassinViewModels`, `AddMocassinViews`). There is no assembly scanning, no reflection-based discovery,
  and no service locator. Later steps add their own `AddMocassinEditors/…` calls in `App` (§18).
- Lifetimes:
  - singleton: `AppPaths`, `IAppSettingsStore`, `ISettingsService`, `ILoggerFactory`, `TimeProvider`,
    `IUiDispatcher`, `IDialogService`, `IStorageService`, `IConsoleService`, `ICommandCatalog`,
    `IProjectSessionService`, `INavigationService`, `ShellViewModel`, the shell window;
  - singleton library services: one `ProjectStore` constructed with
    `new ProjectStoreOptions { ValidateOnLoad = false }` and one `ProjectValidationPipeline` (documented
    stateless/thread-safe, so concurrent session validations share it). Both are constructor-injected into
    `ProjectSessionService`; the M-1 test resolves the registered `ProjectStore` and asserts its options. The
    recovery/settings stores are singletons too (`IAppSettingsStore`, `IRecoveryStore`, `IProjectFileProbe`);
  - created per project by `ProjectSessionService` with explicit constructor arguments (not a DI scope):
    `ProjectSession`, `ProjectDocument`, `ProjectEditor`, `ProjectEditCoordinator`, the validation
    coordinator, and the project-scoped view models.
  - This avoids DI child scopes and captive dependencies: there is exactly one place that constructs a
    session, and it passes the store, clock, dispatcher, and settings it needs.
- Dependency direction (enforced by an architecture test, §20):

```text
Mocassin.App        -> App.Views -> App.ViewModels -> Packages / Results / Contracts / Core
        \-> Avalonia, Microsoft.Extensions.*, Serilog, System.IO
Mocassin.App.ViewModels -> CommunityToolkit.Mvvm + public library APIs; no Avalonia, no views, no controls,
                           no System.IO, no Environment.SpecialFolder, no resource dictionaries
Mocassin.App.Views      -> Avalonia controls and resource dictionaries; no scientific logic; no direct
                           Packages calls; no persistence decisions
```

**The I/O boundary (round-1 M-3).** `Mocassin.App.ViewModels` may call public library APIs that perform I/O —
`ProjectStore`, `ProjectValidationPipeline`, `ProjectTemplateFactory` — because those are the shared contracts
the CLI uses. It may not touch the file system, the environment, the theme, or the dispatcher directly. App-local
state that is not a library contract is split into a ViewModels contract plus an implementation in
`Mocassin.App` or `Mocassin.App.Views`:

| Concern | ViewModels (contract/state/pure logic) | Implementation |
| --- | --- | --- |
| Settings document | `AppSettings`, `LayoutState`, `SessionState`, `AppSettingsValidator`, `SettingsMigration`, `AppSettingsJsonContext`, `IAppSettingsStore` (read/write the document) | `AppSettingsStore` in `Mocassin.App` (atomic write, fsync, size cap, app-data path) |
| Recovery snapshots | `IRecoveryStore` (list/write/delete), `RecoveryCandidate`, retention policy | `RecoveryStore` in `Mocassin.App` |
| External-change probe | `IProjectFileProbe` (length, last-write UTC, content bytes) and the pure `FileStamp` comparison | `ProjectFileProbe` in `Mocassin.App` (`FileInfo`/`File.ReadAllBytesAsync`) |
| Theme/density/contrast/motion | `ThemeSelection`, `Density`, `ThemeService` state + `IThemeApplier` | `AvaloniaThemeApplier` + resource dictionaries in `Mocassin.App.Views` |
| App-data root | `AppDataOverride` (the parsed `--data-dir`/`MOCASSIN_DATA_DIR` value) | `AppPaths` resolves `<LocalApplicationData>/Mocassin` and its subfolders from `Environment.SpecialFolder` in `Mocassin.App` |

The rule is testable without an analyzer, but the token list must be exact (round-2 m-13): a file-text scan of
`Mocassin.App.ViewModels/**/*.cs`, with `//` and `/* */` comments and string literals stripped first, fails on any
of `System.IO`, `File.`, `FileInfo`, `Directory.`, `DirectoryInfo`, `Path.Combine`, `Path.Get`, `FileStream`,
`StreamReader`, `StreamWriter`, `Environment.`, `Avalonia`, `Dispatcher.UIThread`, `Application.Current`, and
`ResourceDictionary`. The bare `Dispatcher` identifier is **not** banned because `IUiDispatcher` and its members
are permitted ViewModels types; the bare `Application` is not banned for the same reason. The permitted library
APIs (`ProjectStore`, `ProjectValidationPipeline`, `ProjectTemplateFactory`, `ProjectJson`, `SHA256`) are not on
the list. §20 defines the positive control that proves the scanner detects a seeded violation, and the
architecture pages state the same list.

`Mocassin.App.ViewModels` references `Mocassin.Contracts`, `Mocassin.Packages`, and `Mocassin.Results`
directly, and `Mocassin.Core` transitively through them (round-2 n-11). Step 23 uses `Packages`, `Contracts`
(for the workspace/report DTOs it consumes) and `Core`; the `Results` reference stays in the project graph as the
Step 28 target and is not called from Step 23 code. `Mocassin.Cli` is never referenced: any behaviour the GUI
needs must live in a library.

- ADR 0003/0006/0008 stand. ADR 0003's "no new UI control dependency without an ADR" is honoured: the step adds
  no control package.

### 2. Shell layout and regions

```text
┌─────────────────────────────────────────────────────────────────────────────┐
│ Menu: File  Edit  View  [Model]  [Build]  [Run]  Tools  Help   (see §10)    │
├──────────────┬───────────────────────────────────────────────┬──────────────┤
│ Projects     │ Document tabs                                 │ Inspector    │
│ (switcher)   │ ┌───────────────────────────────────────────┐ │ (selection)  │
│ ──────────   │ │ Active document (ViewLocator)             │ │              │
│ Explorer     │ │                                           │ │              │
│  Project     │ └───────────────────────────────────────────┘ │              │
│  (tree)      │                                               │              │
│              │                                               │              │
├──────────────┴───────────────────────────────────────────────┴──────────────┤
│ Console: Problems | Application   (later: Build, Run)                       │
├─────────────────────────────────────────────────────────────────────────────┤
│ Status: validity · errors/warnings · active document · saved/autosave ·     │
│         progress slot · version                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

- `ShellView` is a `Grid` with `GridSplitter`s: left column (`ProjectsRegion`, a project switcher above the
  explorer tree), centre (`DocumentsRegion` + `ConsoleRegion` split vertically), right column
  (`InspectorRegion`), and two fixed rows for menu and status. No docking library and no floating panes. Step 26
  adds template/job nodes to the same left region through `IExplorerNodeProvider`; Step 23 does not reserve an
  empty panel for them.
- Each region host is a `ContentControl` whose `DataTemplate` is chosen by the region view model. Regions are
  collapsible through `View` menu commands and `ShellViewModel` toggles with accelerators
  (`Alt+1/2/3` left/centre/right, `Alt+0` console); sizes and visibility persist in settings (`layout`, §17).
- Panel sizes are clamped to sane minima (left 160–600 px, right 160–800 px, console 80–600 px) so a corrupt
  settings file cannot produce an unusable window. When the persisted size is invalid, the default is used and
  the file is repaired on the next save.
- Window title binds `"<project name><*> — MOCASSIN"` for the active document, or `"MOCASSIN"` with no
  project. Minimum window size 900×600. The window restores size/state from settings; a saved placement whose
  bounds do not intersect any current screen falls back to the primary screen. Feature 1 covers it with a
  headless view-model test over the placement resolver, because a real monitor topology cannot be simulated
  headless (verification item V-5).
- **Not in this step:** no splash window. The original splash existed because it built a database-backed
  library synchronously; the new startup is short, and a splash would add a second window to headless tests.
  The welcome document covers the loading/empty state. Recorded as a deliberate deviation in §24.

### 3. Document model, tabs, and documents shipped in this step

```csharp
// Mocassin.App.ViewModels/Documents
public abstract partial class DocumentViewModel : ObservableObject
{
    public DocumentKey Key { get; }
    public abstract string Title { get; }              // bindable, raises change notifications
    public virtual string? ToolTip => null;
    public bool IsDirty { get; protected set; }
    public bool IsReadOnly { get; protected set; }
    public virtual Task OnClosingAsync(CancellationToken cancellationToken);
}

public readonly record struct DocumentKey(string Kind, Guid? ScopeId = null);
```

- `Kind` values are stable strings declared as constants: `welcome`, `project.overview`, `project.json`, and
  later `model.particles` … (§18). `ScopeId` is the project id for project-scoped documents so two open
  projects can each have a Project Overview tab.
- `DocumentCollectionViewModel` owns `ObservableCollection<DocumentViewModel>`, `ActiveDocument`,
  `OpenOrActivate(document)` (reuse by `Key`, otherwise append and activate), `CloseAsync(document)`,
  `CloseOthersAsync`, `CloseAllAsync`, `HasDirtyDocuments`, and an `ActiveDocumentChanged` event.
- Tab strip behaviour: dirty marker, tooltip with the full file path, close button, middle-click close,
  `Ctrl+W`, `Ctrl+Tab`/`Ctrl+Shift+Tab`, `Ctrl+1..9`, drag reorder within the strip, and a context menu
  (Close, Close Others, Close All, Copy path). Dragging a tab out to its own window is deferred (no
  multi-window architecture in this step).
- The welcome document is the only document that exists on a fresh start, and it closes when the first real
  document opens. It offers New/Open/Recent and the shortcut cheat sheet.
- Documents shipped in this step:
  - **Project Overview** (`project.overview`): project identity (id, name, description, author, notes),
    read-only creation/save timestamps from the session, read-only master seed with copy, schema and symmetry
    metadata, model counts from `ModelStatistics` (loaded on demand, §8), and the validation summary.
    Name/description/author/notes are **editable** through the edit pipeline (§7). This is the step's only
    editing surface and it exists so undo/redo, dirty state, save, autosave, and recovery are exercised
    end-to-end without pre-empting Step 24.
  - **Project JSON** (`project.json`): the indented canonical project JSON (`ProjectJson`) in a read-only,
    searchable text view with copy and "Save a copy…" (export). It replaces the original JSON/XML data viewer
    as a debug/export surface and is never parsed back into the editor.
- Inspector region: for this step it shows the active document's summary, or a neutral empty state when the
  active document contributes nothing. Steps 24/25 replace the content with the type-specific inspector. The
  shell contract is a `ContentControl` bound to `ShellViewModel.InspectorContent`, set by the active document's
  optional `IInspectorContributor`.

### 4. Navigation, project explorer, and editor registration seam

```csharp
// Mocassin.App.ViewModels/Navigation
public enum NavigationIntent { Activate, SelectObject, FocusProperty }

public interface INavigationService
{
    Task OpenDocumentAsync(DocumentViewModel document, CancellationToken cancellationToken = default);
    Task<bool> ActivateAsync(DocumentKey key, CancellationToken cancellationToken = default);
    Task<bool> NavigateToAsync(ModelPath path, NavigationIntent intent, CancellationToken cancellationToken = default);
    bool CanGoBack { get; }
    bool CanGoForward { get; }
    Task GoBackAsync(CancellationToken cancellationToken = default);
    Task GoForwardAsync(CancellationToken cancellationToken = default);
}

public interface INavigationTargetProvider
{
    IReadOnlyCollection<string> Domains { get; }        // ModelPath.Domain values, e.g. "project", "particle"
    Task<DocumentViewModel?> CreateAsync(ModelPath path, NavigationIntent intent, CancellationToken cancellationToken);
}
```

- `NavigateToAsync` resolves the `ModelPath.Domain` against the registered providers of the **active project's**
  scope. Exactly one provider per domain; a duplicate registration is a startup error (fail fast in the
  architecture test and in `AppServiceRegistration` validation).
- No provider for a domain: append an information entry to the Application console ("Editing <domain> is not
  available in this version"), do not throw, do not open a placeholder editor. This is what makes the
  parallel-track contract safe: a diagnostic from Step 07 with a `particle` path is navigable the day Step 24
  registers the particle provider, and harmless before that.
- Step 23 registers one provider: `project` (→ Project Overview / JSON) plus the direct document commands.
  Steps 24–28 register their own; the shell does not change.
- History is a bounded back/forward stack (100 entries) of `(ProjectId, DocumentKey, ModelPath?, intent)`;
  `Alt+Left`/`Alt+Right`; a navigation that resolves to the already-active document is not pushed.
- The project explorer is a `TreeView` with project-level nodes only: project → (Overview, Data (JSON),
  Diagnostics). Model-domain nodes are added by Steps 24–26 through an `IExplorerNodeProvider` registration,
  so the shell never hard-codes editor names.
- The project switcher lists open projects with their dirty marker and path; activating one activates its last
  active document. Closing a project closes its documents after the unsaved-changes prompt.

**Contribution seams** (all in `Mocassin.App.ViewModels` as `internal` types, all registered explicitly in the
composition root; Steps 24–28 edit these same assemblies, so `internal` is sufficient and the public API stays
empty — see §15):

```csharp
internal interface IInspectorContributor { object? CreateInspector(InspectorSelection selection); }
internal interface IExplorerNodeProvider { IEnumerable<ExplorerNode> CreateNodes(ProjectSession session); }
internal interface IMenuItemProvider { IEnumerable<ShellCommand> CreateMenuItems(MenuKind menu); }
internal interface IConsoleChannelProvider { IEnumerable<ConsoleChannelViewModel> CreateChannels(); }
internal interface IStatusBarItemProvider { IEnumerable<StatusBarItemViewModel> CreateItems(); }
```

`InspectorSelection` is the shell's typed selection value, not `object` (round-1 n-3): it carries the active
`DocumentKey`, the project id, the selected `ModelPath` list (authoritative objects and properties), and an
optional domain tag. The shell builds it from the document and the group/list selection, validates that every
path belongs to the owning project, and hands it to the single inspector contributor that claims the tag. An
unclaimed tag shows the neutral empty state; it is never passed an arbitrary object graph.

`ExplorerNode` is a small immutable node record (key, label, icon key, `ModelPath?`, children, command), and
`StatusBarItemViewModel` is the base for the version, progress, validation, dirty, and autosave slots. Both are
listed in §15 with the other types that earlier drafts named but did not place (round-1 m-8).

Step 23 registers the `project` navigation domain, the project-level explorer nodes, the Application console
channel, the shell menus, and the shell status items; feature 6 adds the Problems channel binding. Every seam
returns plain view models, records, or commands; no seam takes an Avalonia type, so later steps stay
headless-testable. The shell validates at startup that no two providers claim the same navigation domain or the
same inspector tag and that no provider is registered twice.

### 5. Project lifecycle

```csharp
// Mocassin.App.ViewModels/Projects (internal)
internal interface IProjectSessionService
{
    IReadOnlyList<ProjectSession> Sessions { get; }
    ProjectSession? ActiveSession { get; }

    Task<ProjectSessionResult> CreateAsync(ProjectTemplateKind kind, string name, string path, CancellationToken cancellationToken = default);
    Task<ProjectSessionResult> OpenAsync(string path, CancellationToken cancellationToken = default);
    Task<ProjectSessionResult> ActivateAsync(string path, CancellationToken cancellationToken = default);
    Task<ProjectSessionResult> SaveAsync(ProjectSession session, CancellationToken cancellationToken = default);
    Task<ProjectSessionResult> SaveAsAsync(ProjectSession session, string path, CancellationToken cancellationToken = default);
    Task<ProjectSessionResult> ReloadAsync(ProjectSession session, CancellationToken cancellationToken = default);
    Task<CloseSessionResult> CloseAsync(ProjectSession session, CancellationToken cancellationToken = default);

    Task<RecoveryScanResult> ScanRecoveryAsync(CancellationToken cancellationToken = default);
    Task<ProjectSessionResult> RecoverAsync(ProjectSession session, RecoveryCandidate candidate, CancellationToken cancellationToken = default);
    Task DiscardRecoveryAsync(RecoveryCandidate candidate, CancellationToken cancellationToken = default);
}

internal enum ProjectSessionOutcome
{
    Created, Opened, Activated, OpenedReadOnly, Saved, Reloaded, Recovered, Closed,
    NotFound, TooLarge, Io, InvalidContent,          // mirrors ProjectLoadFailureKind
    Locked, UnsavedChanges, SaveFailed, Canceled,
}
```

- **Services never prompt.** `IProjectSessionService` returns a typed `ProjectSessionResult`, whose
  `ProjectSessionOutcome` covers success, the four load failures mapped one-to-one from
  `ProjectLoadResult.FailureKind` (`NotFound`, `TooLarge`, `Io`, `InvalidContent`; round-1 n-2, round-2 m-12),
  `Locked`, `UnsavedChanges`, `SaveFailed`, and `Canceled`, plus the diagnostics. `TooLarge` is the
  `ProjectStoreOptions.MaxDocumentBytes` (64 MiB) rejection; `Io` is the operating-system/sharing failure. The
  mapping is a pure switch with no default that silently falls back to `Io`. The shell layer owns every dialog. This keeps the service
  headless-testable and prevents a message box from appearing inside library-like code. This also replaces the
  step page's earlier `IProjectStore` service interface: the concrete `ProjectStore` is injected as the
  persistence API; there is no second store abstraction.
- **Store configuration (round-1 M-1).** The single `ProjectStore` instance registered in the composition root
  is constructed with `new ProjectStoreOptions { ValidateOnLoad = false }`. The load result's
  `ProjectLoadResult.Diagnostics` are stored on the session as `LoadDiagnostics` and passed as
  `loadDiagnostics` to every `ProjectValidationPipeline.Validate` call (§8). There is therefore exactly one
  validation per load: the store does not validate, and the pipeline's `Load` stage is populated instead of
  skipped. `ProjectStore.ValidateOnLoad = true` (the default) is never used by the GUI; a test asserts the
  option value and that a migration/load diagnostic reaches the Problems list.
- **Operations.** `OpenAsync` opens a new session or returns `Locked`/load failures; `ActivateAsync` activates
  an existing session for an already-open path and is what the open path, recents, and drag-and-drop call
  (round-1 m-10); `ReloadAsync` re-reads the file and replaces the session's `ProjectDocument` with a fresh
  instance over the reloaded value, which discards the old undo/redo stacks because they no longer describe the
  file's content, then refreshes the stamp and the load diagnostics; the recovery operations (§6) are
  `RecoverAsync` and `DiscardRecoveryAsync`.
- **New**: uses `ProjectTemplateFactory.CreateProject(kind, name, utcNow)` — the same factory as
  `mocassin new` — after validating the name **and the target path** with the shared `ProjectPathRules` (§16:
  name normalisation, invalid characters, length, existing target, directory creation). Step 23 ships exactly
  the kinds `ProjectTemplateKind` defines today (`blank`, `ceria`) with the same display names as the CLI, so
  the two surfaces cannot drift; a later starter kind appears in both automatically because the factory is
  shared. An existing target is refused with the same rule as the CLI's `CLI003` case (never replace), and the
  shared helper returns the message the CLI prints, so the GUI cannot invent a different one (round-1 n-6).
- **Open**: picker / recent list / drag-and-drop onto the window. Drag-and-drop is restricted to `.mocprj`
  files (and directories are ignored), because accepting arbitrary paths would be a new trust surface. The
  picker starts in the last-used directory from settings (§17) and updates it on success.
- **Save**: `ProjectStore.SaveAsync(current, path)`; on success call `ProjectDocument.MarkSaved()` (no-arg,
  §7) and store the returned value as `ProjectSession.LastWritten` for the file stamp and the displayed
  timestamp; delete the `.autosave` sidecar and the untitled recovery snapshot; refresh `modifiedUtc` display.
- **Save As**: `ProjectStore.SaveAsync(current, newPath)`; on success migrate the lock (acquire the new path
  before releasing the old one; if the new lock cannot be acquired, keep the old path and report), update the
  session path, recent list, file stamp, and document keys' display data. Undo history is preserved.
- **Close**: if dirty, the shell prompts Save/Discard/Cancel; then dispose the lock, delete an orphaned
  sidecar only if it is ours and the document was saved, and close the project's documents. A read-only session
  closes without a prompt and without deleting another session's sidecar.
- **Recent files**: at most 10 entries `{path, lastOpenedUtc}` in settings; de-duplicated by full path
  (case-insensitive on Windows/macOS, ordinal on Linux — decided by `OperatingSystem.IsWindows()/IsMacOS()`);
  a missing entry renders disabled with a "Remove from list" action and is never auto-removed silently;
  clearing the list is a settings action.
- **Locks**: `ProjectStore.AcquireLockAsync` before the first edit. Success → normal. Failure → open the
  project **read-only** with a banner in the status bar and on the document, disable edit/save commands, allow
  Save As (which acquires a lock on the new path) and Close. The stale-lock timeout stays at the store default
  (`ProjectStoreOptions.StaleLockTimeout`, 10 minutes); the UI shows the lock path and the acquisition time
  from `ProjectFileLock.Diagnostics`.
- **External modification**: at open and after each save, `ProjectFileStamp` records `{length, lastWriteUtc,
  documentHash}` where the hash is the canonical document hash of `ProjectSession.LastWritten` (the value the
  store returned), not of the in-memory project. Re-check on window activation and immediately before Save, using `{length,
  lastWriteUtc}` as a cheap filter and hashing only when they differ. Outcomes:
  - unchanged → nothing;
  - changed and the document is clean → offer Reload / Keep (Keep turns the document dirty);
  - changed and the document is dirty → offer Save As (new path) / Reload (discard) / Cancel; **never**
    overwrite silently.
  No file-system watcher is used: watching would add platform-specific behaviour and a second event source;
  activation-time checks are sufficient and testable.
- **Already-open paths**: opening a path that belongs to an existing session activates that session and its
  last active document; it never creates a second session. Save As onto a path that another session has open is
  refused with an explanatory dialog (and never steals the lock).
- **Window close**: close requests run through one coordinator. With one dirty project it prompts
  Save/Discard/Cancel; with several it prompts per project with an "apply to all" choice, and Cancel aborts the
  shutdown without closing any document. A read-only project closes without a prompt. A clean shutdown writes
  `cleanExit = true`; any cancelled or failed shutdown leaves it `false`.
- **`--project` at startup**: a missing, unreadable, or invalid path does not prevent startup. The shell opens,
  reports the failure (code, message, path) in the Application console and a notification, and continues with
  the normal welcome/session flow.
- **Multi-project workspace**: one global tab strip, one session per open project, one edit coordinator and
  one undo stack per project. The left panel lists open projects. This is the recorded choice over per-project
  tab sets (§24).

### 6. Dirty state, autosave, and crash recovery

- Dirty is defined by **content identity**, not file bytes: `ProjectDocument.IsDirty` compares the canonical
  document hash of the current and last-saved values. The window title and document tab show `*`, and the
  project switcher entry and status bar show a dirty marker.
- **Save point correctness.** `ProjectDocument` exposes `MarkSaved(savedProject)`, which replaces both values.
  Replacing the current value with a fresh `modifiedUtc` would make the document dirty again after any
  undo→redo round trip, because the redo entry holds the pre-save timestamp. Step 23 therefore uses the
  no-argument `MarkSaved()` (clean point = the exact in-memory value) and keeps the store's returned value as
  `ProjectSession.LastWritten` for the file stamp and the displayed `modifiedUtc`. Consequences, all tested:
  save → undo → redo is clean; save → edit → undo is clean; save → edit → save is clean; undo below the save
  point is dirty. The alternative (excluding `modifiedUtc` from the dirty hash) was rejected because it would
  change the Step 01 dirty semantics and create a second document-hash definition.
- **Autosave** runs on a `TimeProvider`-backed timer owned by the shell (`AutosaveScheduler`), not by the
  project store, because the interval is a user setting and the decision needs the document. Default interval
  5 minutes, configurable 1–60 minutes, can be disabled. Each tick, for every session with a dirty document:
  - saved project → `ProjectStore.WriteAutosaveAsync(current, path)` (the Step 01 sidecar `<file>.autosave`);
  - untitled project → `ProjectStore.WriteAutosaveToPathAsync(current, recoveryPath)` where
    `recoveryPath = <app-data>/recovery/<projectId>.mocprj.autosave` (§16 addition).
  The immutable snapshot is written asynchronously; the status bar shows "Auto-saving…" then "Auto-saved
  HH:mm". A per-session `SemaphoreSlim(1,1)` serialises save and autosave so the two writes never interleave.
  Autosave never clears dirty state and never updates the save point. On successful save, the sidecar or
  recovery file is deleted.
- **Recovery** on open or startup: if a sidecar exists and its content differs from the saved file, offer
  Recover / Discard / Decide later through `IProjectSessionService.RecoverAsync`/`DiscardRecoveryAsync`
  (round-1 m-10). Recovering loads the sidecar, builds a `ProjectDocument` with the saved file value as the
  clean point (`ProjectDocument.CreateRecovered`, §16), and leaves the document dirty; the next save writes the
  main file and deletes the sidecar. Discarding deletes it. "Decide later" keeps it and reappears next time.
  Untitled recovery files are listed in the same startup flow and open as dirty untitled documents that prompt
  for a path on first save. The scan itself goes through `IRecoveryStore` (implementation in `Mocassin.App`,
  §1), never `System.IO` in the view-model project.
- **Read-only sessions never write**: a project opened without a lock does not autosave, does not write a
  recovery file, and does not run the external-change prompt; its only write paths are Save As and Close. This
  prevents a second instance from leaving files beside a project another instance owns.
- **Deleted or replaced file**: a missing project file is handled as an external change; a clean document offers
  Save As (the default) or Close, and a dirty document offers Save As or Discard. The app never recreates a
  deleted file implicitly.
- **Retention**: recovery files older than 7 days are deleted at startup; log files follow the 14 days/100 MB
  rule (§12). A `.autosave` sidecar next to a user's project is **never** deleted by the retention sweep —
  only by a successful save or an explicit discard — because it sits in the user's folder. Projects,
  packages, results, and job folders are never auto-deleted.
- **Unclean exit**: settings carry a `session` block with the open project paths, the active project, and a
  `cleanExit` flag written on a clean shutdown. If the previous run ended uncleanly, startup offers
  "Restore session and recover changes" after the recovery scan. Restoring opens the listed projects that
  still exist and reports the ones that do not.
- **Crash**: the crash dialog offers "Write recovery files", "Open log folder", and "Exit". Writing is
  asynchronous (the store exposes only async writes) and bounded: `CrashRecoveryWriter` starts the snapshot
  writes for every dirty session, awaits them with a 2 s budget (`Task.WhenAny` against a
  `CancellationTokenSource` timeout), and reports per-session success, timeout, or failure in the dialog. There
  is no `.Result`, `GetAwaiter().GetResult()`, or other sync-over-async call on this path (round-1 m-11). If the
  budget expires, the log records the unfinished sessions and the app exits without claiming they were saved.
  No automatic restart. Until feature 7 implements the writer, feature 1 registers a null `IRecoveryWriter`
  whose button reports "no recovery snapshots in this build"; it is a seam, not a dead control.

### 7. Edit pipeline and undo/redo

```csharp
// Mocassin.App.ViewModels/Editing
public sealed partial class ProjectEditCoordinator : ObservableObject
{
    public ProjectDocument Document { get; }
    public bool IsDirty { get; }
    public bool CanUndo { get; }
    public bool CanRedo { get; }
    public ValidationResult? LastValidation { get; }
    public event EventHandler<ProjectChangedEventArgs>? ProjectChanged;

    public ProjectEditResult Apply(string description, Func<Project, Project> mutation);
    public bool Undo();
    public bool Redo();
}
```

- One coordinator per project, created by `ProjectSessionService`. It is the **only** write path: view models
  never mutate a `Project` or a domain object, never call `ProjectHasher`, and never build a `ProjectChange`.
  Editors in Steps 24/25 call `Apply` with a `ProjectEditor` helper or a pure mutation.
- `Apply` delegates to the existing `ProjectDocument.Apply` → `ProjectEditor.Apply`, which validates the
  candidate and refuses error states. On failure the coordinator raises a transient edit diagnostic set (shown
  inline in the initiating document and appended to Problems with source `edit`), keeps the project unchanged,
  and leaves the document's dirty state untouched.
- `Undo`/`Redo` delegate to `ProjectDocument`; the stack is bounded by `MaxUndoDepth` (default 200, §16),
  oldest entries dropped first. The stack survives saves; the save point is content-based, so undo may cross
  it in both directions.
- Every successful change (apply/undo/redo) raises `ProjectChanged` with the previous and current values and
  the change description. The shell uses it to refresh titles, dirty markers, status bar, the Project Overview
  and JSON documents, and to schedule validation; Step 27's viewport will subscribe to the same event.
  No Core event or observer was added: the coordinator is the single writer, so a local notification is
  sufficient and cannot miss an edit.
- Bulk operations (create from template, duplicate, paste, renumber in later steps) are one `Apply` call and
  therefore one undo entry.
- Metadata editing in the Project Overview document is one `Apply` per committed field change (on focus loss
  or Enter), with descriptions such as "Rename project" and "Edit project notes". Typing does not create an
  entry per keystroke. An empty project name is refused by `ProjectValidator` and surfaces as an inline error.
- Performance: dirty state is computed on change and cached — never on every binding read. A hash of the ceria
  sample must stay well below the 500 ms open/save budget; the actual figure is recorded as non-gating evidence
  at feature 7 closeout (§21).

### 8. Validation and diagnostics surface

- The shell consumes `ProjectValidationPipeline` exactly as Step 07 §8 defines, in the **in-memory** form:
  `pipeline.Validate(project, loadDiagnostics: session.LoadDiagnostics, ValidationOptions.Default, compiledModel:
  null, cancellationToken)` — no statistics, no build stage — debounced 300 ms after the last `ProjectChanged`.
  `session.LoadDiagnostics` is `ProjectLoadResult.Diagnostics` captured at open (and again at Reload/Recover).
  The store is configured with `ValidateOnLoad = false` (§5), so:
  - the store never validates, and no domain validator is called twice;
  - the pipeline's `Load` stage reports the load diagnostics instead of being skipped **when
    `ProjectLoadResult.Diagnostics` is non-empty** (`Completed`, or `Failed` on a load error). A clean
    current-version load legitimately yields `Load` = `Skipped` with count 0, per Step 07 §2; the design does not
    require otherwise (round-2 m-14);
  - `ValidateFileAsync` is not used by the GUI, because the GUI already holds the parsed project and must not
    read the file a second time.
  This is the round-1 M-1 correction. Tests assert the store option, a `Load`-stage fixture with a
  migration/load diagnostic (the stage is `Completed`/`Failed` and its diagnostic reaches Problems), a clean
  fixture (the stage is `Skipped` with count 0), and that no diagnostic is duplicated.
- Validation runs off the UI thread on an immutable snapshot. When the result arrives, it is applied only if
  the current document hash equals the snapshot hash; otherwise it is discarded as stale. A newer edit
  cancels the previous run.
- Surfaces:
  - status bar: `Not validated` → `Validating…` → `Valid` / `n errors, m warnings, k infos` (raw counts, per
    Step 07), with severity colour **plus** an icon and text (no colour-only information);
  - Problems channel: rows show severity icon, `Code`, `Message`, `Path.Domain`, `Path.ObjectId`,
    `Path.Property`, `Hint`; sortable by severity/code; filter box; double-click or Enter navigates via
    `NavigateToAsync(path, SelectObject)`; `Related` paths are secondary navigation targets in the context
    menu; "Revalidate" re-runs with cancellation and a timestamp; the operation never mutates the project;
  - document-level: an error banner in the Project Overview whose entries navigate to Problems.
- Statistics are opt-in: the Project Overview requests `IncludeStatistics = true` and computes them in the
  background, cached by document hash. The Step 07 budgets (`/stats` < 2 s, live validation < 200 ms for
  ceria) are design budgets, not PR-fast assertions (round-1 m-4, §21).
- Diagnostics are never invented in the UI: no new codes, no re-worded messages. Help links map `Code` to the
  documented anchor when one exists; an unknown code renders unlinked.
- Step 07 §8 assigns "the panels and commands" to Steps 24–25. Step 23 builds the status indicator, the
  Problems list with its filters/sort/help anchors/related navigation/revalidate, the navigation plumbing, and
  the on-demand statistics panel header because the shell needs them for its own acceptance; Steps 24/25 add the
  editor side of each navigation target and the per-domain statistics navigation. This is the same contract
  delivered earlier in part, not a change to it. Feature 6 is the single owning feature for this whole surface
  (round-1 n-4), so no part of it is orphaned across features 2, 6, and 7.
- One settings file per data directory: a second application instance sharing the same data directory is not
  coordinated, so the last writer wins for settings. This is accepted for a single-user desktop app and is
  documented; project files are protected by their own lock, which is the part that matters.

### 9. Settings, theming, density, and accessibility

- **Where the pieces live (round-1 M-3).** The settings *model*, the pure validator/migrations, the
  source-generated `AppSettingsJsonContext`, the `IAppSettingsStore` contract, and `ISettingsService`
  (the state machine the view models observe) are in `Mocassin.App.ViewModels`. The file store
  (`AppSettingsStore`) is in `Mocassin.App`: it resolves the app-data path, reads and writes the document
  atomically, applies the size cap, and owns `Environment.SpecialFolder`/`Path.Combine`. The theme *applier*
  (`AvaloniaThemeApplier` and the resource dictionaries) is in `Mocassin.App.Views`. `Mocassin.App.ViewModels`
  contains no `System.IO`, no `Environment.SpecialFolder`, no `Application`, and no resource dictionaries
  (§1, and the architecture test in §20).
- Settings live in `<app-data>/Mocassin.Settings.json`; the app-data root is
  `<LocalApplicationData>/Mocassin` (`%LOCALAPPDATA%`, `~/.local/share`, `~/Library/Application Support`),
  overridable by `--data-dir`/`MOCASSIN_DATA_DIR`. Subfolders: `logs/`, `cache/` (reserved for Step 27),
  `recovery/`. No separator is hard-coded; the App-layer store builds every path. The format is specified in
  §17.
- The settings service validates on load: an unknown enum, out-of-range number, or malformed entry falls back
  to the default for that field and logs a warning; a document whose `format` is wrong or whose
  `schemaVersion` is a *future major* is not loaded and **not overwritten** (the app starts with defaults,
  shows a banner, and Save Settings becomes "Save as new file"). Unknown fields are tolerated on read and are
  not preserved when the file is rewritten, so a newer minor writer cannot corrupt an older reader; older
  documents are migrated by pure functions.
- **Write discipline (round-1 m-6).** The store writes through the same atomic protocol as the project file:
  serialise with the source-generated `AppSettingsJsonContext` to UTF-8 without BOM, write a sibling temp file,
  flush and `Flush(flushToDisk: true)`, then replace the target; a failed write leaves the previous file
  intact. It rejects a document above `MaxSettingsBytes = 4 MiB` (a corrupt or hostile file cannot exhaust
  memory) and a `recentFiles` list above its documented cap. It never writes on a failed load of a
  forward-versioned file.
- Theme: the applier maps `ThemeSelection` (`system`/`light`/`dark`) to `Application.RequestedThemeVariant`,
  overrides the Fluent accent resources when an accent is selected, and swaps the density resource dictionary
  (`comfortable` default, `compact`) and the font-scale token set (0.85–1.5, applied to font sizes, not fixed
  pixel heights). Reduce motion disables non-essential transitions. The applier is the only code that touches
  theme resources; `ThemeService` in ViewModels only holds the selection and raises a change.
- High contrast: a dedicated app-owned resource dictionary with WCAG-verified pairs, selectable in settings and
  also followed when the platform requests a high-contrast variant if Avalonia exposes it (verification item
  V-2). Do not assume a Fluent high-contrast dictionary exists in 12.1.6.
- Accessibility target **WCAG 2.2 AA**: every interactive control has an accessible name (visible label or
  `AutomationProperties.Name`), icon-only buttons have names and help text, focus order follows the visual
  order, focus is visible and never trapped, dialogs move focus to themselves and restore it on close, trees
  and lists are keyboard-operable, status is announced through names/roles rather than colour, and text scales
  with the font-scale setting. Tab headers, splitters (with keyboard resize), the console list, the explorer
  tree, and the palette are the risk points and are covered by the accessibility assertions in §20.
- The settings dialog is a real view: theme (radio), accent (a small fixed palette, not a full colour picker),
  density, font scale (slider with live preview), high contrast, reduce motion, autosave enable/interval
  (feature 7), confirm-on-external-change, recent-file list with clear, and buttons Reset to defaults / Open
  config folder / Clear logs and cache / Close. Reset asks for confirmation and never deletes projects. The
  dialog is a view over `SettingsViewModel`; the confirmation and the folder opening go through
  `IDialogService`/the App layer, not through `System.IO` in the view model.
- "Clear logs and cache" deletes only files under `logs/` and `cache/` that are not the currently open log
  file, and reports how many bytes were freed. No project, package, result, or job-folder path is touched. The
  sweep is an App-layer operation behind an interface, so it is testable against a real temporary data
  directory.

### 10. Keyboard, commands, menus, and the command palette

- `ShellCommand` (ViewModels/Commands) = `{ Id, Label, Gesture, Category, CanExecute, Execute }`. The
  `ICommandCatalog` aggregates commands registered by the shell and by the active document. Commands are
  `IAsyncRelayCommand`s with cancellation; `CanExecute` is the single source of truth for menu, palette,
  toolbar, and key binding state, so a disabled command is visibly disabled everywhere.
- **Gesture type (round-1 m-7).** `Gesture` is the framework-neutral record
  `KeyGestureDescriptor(ModifierKeys Modifiers, KeyName Key)` with a `ModifierKeys` flags enum
  (`Primary`, `Shift`, `Alt`, `Control`, `Meta`). `Primary` means `Control` on Windows/Linux and `Meta` on macOS
  and is resolved once from `OperatingSystem` in the key-binding layer. `Mocassin.App.Views` converts the
  descriptor to an Avalonia `KeyGesture`/`KeyBinding`; the view-model project never names an Avalonia key type.
  The display text and the cheat sheet are produced from the same descriptor per platform, so the table below
  uses `primary` uniformly and cannot drift from the macOS binding.
- Shortcuts (Step 23 set); `primary` is platform-resolved as above:

| Action | Shortcut |
| --- | --- |
| New / Open / Save / Save As / Close document | `primary+N` / `primary+O` / `primary+S` / `primary+Shift+S` / `primary+W` |
| Undo / Redo | `primary+Z` / `primary+Y` and `primary+Shift+Z` |
| Command palette | `primary+Shift+P` |
| Find in document | `primary+F` |
| Next / previous document, document 1–9 | `primary+Tab` / `primary+Shift+Tab`, `primary+1..9` |
| Back / forward navigation | `Alt+Left` / `Alt+Right` |
| Settings / shortcut cheat sheet | `primary+,` / `F1` |
| Toggle left / centre / right region, console | `Alt+1` / `Alt+2` / `Alt+3`, `Alt+0` |
| Validate now | `primary+Shift+V` (Step 26 adds `F5` build and `F6` run) |
| Quit | `primary+Q` (macOS) / window-manager close on Windows and Linux |

- Menus: `File`, `Edit`, `View`, `Tools`, `Help` are always present. `Model`, `Build`, and `Run` headers are
  contributed by `IMenuItemProvider` and appear **only** when they contain at least one enabled item, so
  Step 23 shows no dead menus and Steps 24–28 add them by registering items. The original step-page sketch
  listed all eight menus; the shell shows five until the editors and build UI land. `File`/`Edit`/`View` items
  are Step 23 commands; `Tools` holds application diagnostics and the config/cache actions; `Help` holds
  the cheat sheet, the log folder, the diagnostics folder, and the About box (name, version, MIT licence, the
  original-project attribution required by `NOTICE.md`).
- Command palette: a modal overlay with a search box, fuzzy filtering over label/category/gesture,
  `Enter` executes, `Esc` cancels, arrow keys move. The palette lists only currently executable or explicitly
  disabled-with-reason commands; a command whose provider is not loaded yet is absent rather than shown
  broken.
- Menu and palette never contain placeholder commands for later steps; the shell must not claim unimplemented
  capability.
- Every control reachable by mouse is reachable by keyboard; `Tab` order is the reading order; splitters
  support arrow-key resizing when focused.

### 11. Dialogs, pickers, consoles, and error boundaries

```csharp
// Mocassin.App.ViewModels/Dialogs
public interface IDialogService
{
    Task<bool> ConfirmAsync(ConfirmDialogRequest request, CancellationToken cancellationToken = default);
    Task<DialogChoice> ChooseAsync(ChoiceDialogRequest request, CancellationToken cancellationToken = default);
    Task<string?> PromptAsync(TextPromptRequest request, CancellationToken cancellationToken = default);
    Task ShowErrorAsync(ErrorDialogRequest request, CancellationToken cancellationToken = default);
    IProgressDialog ShowProgress(ProgressDialogRequest request);
}

public interface IStorageService
{
    Task<string?> PickOpenProjectAsync(CancellationToken cancellationToken = default);
    Task<string?> PickSaveProjectAsync(string suggestedName, CancellationToken cancellationToken = default);
    Task<string?> PickFolderAsync(CancellationToken cancellationToken = default);
}
```

- Dialogs are an **in-shell overlay** owned by `Mocassin.App.Views`: a `DialogHost` panel over the shell
  content with a scrim, focus capture/restore, `Esc` = cancel, default button on `Enter`, and a stacking
  queue (one modal at a time). The view model of each dialog is a plain `DialogViewModel`, so the real dialog
  path is exercised in headless tests without a real window. This replaces `MessageBox` and avoids adding
  `MsBox.Avalonia`; an in-app overlay also matches ADR 0003's "no new UI control dependency".
- `IStorageService` is the only UI-boundary abstraction that needs a test fake: it wraps Avalonia's
  `IStorageProvider` (`TopLevel.StorageProvider`) in `Mocassin.App.Views`, returns plain strings, reads and
  updates the last directory per operation from settings (`paths.lastOpenDirectory`, `paths.lastSaveDirectory`,
  `paths.lastFolderDirectory`, §17 — round-1 m-5), and never leaks Avalonia types into view models. A null
  return means "cancelled", never an error.
- Progress: one `IProgressDialog` per long operation with a determinate or indeterminate bar, a status line,
  cancellation, and a details expander. Step 23 uses it for open/validate-statistics/save-all; Step 26 reuses
  it for builds. Only one progress operation is shown at a time; the status bar carries the same state in a
  progress slot.
- Notifications: transient toasts (top-right of the document region) for background completions and
  non-blocking failures, plus the Application console for anything the user may want to read later.
- Consoles: `ConsoleViewModel` with channels. **Problems** is bound to the validation result (§8).
  **Application** is an append-only log channel fed by `IConsoleService` with
  `{severity, timestampUtc, source, message, optional action}`; it keeps at most 2,000 entries (ring buffer),
  supports severity filter, text filter, clear, and copy. Step 26 adds a **Build** channel and Step 28 a
  **Run** channel through the same `IConsoleChannelProvider` seam; Step 23 ships only Problems and
  Application so no channel is a dead tab.
- Every message includes severity, timestamp, source, and an optional action — the recorded contract from the
  step page.
- Error presentation: failures from library calls carry their `Diagnostic` list; the UI shows the first
  error's code and message plus a "Details" expander with all diagnostics and the affected path. No raw stack
  trace in a normal dialog; stack traces go to the log and the crash dialog.

### 12. Logging and crash handling

- `Microsoft.Extensions.Logging` with Serilog sinks: a date-suffixed rolling file in
  `<app-data>/logs/mocassin-<date>.log` (daily rolling, minimum level from settings, console sink only in Debug
  builds). Structured properties include project id, document key, operation, and duration where applicable.
  The log sink owns record timestamps; `TimeProvider` is injected for document/autosave/stamp time, not for
  logging (round-1 n-7). Tests assert log level, scope, and message, not sink timestamps. No credentials, no
  remote sinks, no telemetry.
- Retention: 14 days or 100 MB, whichever comes first. The Serilog file sink is configured with a daily
  rolling interval and a retained file count of 14; the size cap uses the sink's retained-size option if it is
  available in the pinned version and otherwise an explicit startup sweep (`LogRetention`) that deletes the
  oldest closed log files until under 100 MB (verification item V-3). The active file is never deleted.
- Global handlers, installed before the first window: `AppDomain.UnhandledException`,
  `TaskScheduler.UnobservedTaskException`, and the Avalonia `Dispatcher.UIThread.UnhandledException`. Each logs
  the exception with stack trace, the active project path, and the last command id.
- On an unhandled exception: if the UI thread is alive, log, write recovery files (best effort), show the crash
  dialog ("Write recovery files", "Open log folder", "Exit"), and exit `1` when the user closes it. If the UI
  thread is dead, write a crash report file next to the logs and exit `1` with a message on stderr. The process
  must never die silently; a test forces the handler and asserts the log entry and report.
- `Help → Application diagnostics` runs app-side checks and shows a copyable report: app and .NET version, OS
  and RID, data-dir path and writability, settings parse result and version, log-folder state and size, theme
  resources loaded, recent-file existence, open-project lock states. It is **not** the CLI `doctor`: the GUI
  does not reference `Mocassin.Cli`, and the two reports serve different scopes (environment vs. application).

### 13. State ownership and threading rules

| State | Owner | Persisted |
| --- | --- | --- |
| Scientific model and project content | `Project` (immutable value) held by `ProjectDocument` | `.mocprj` |
| Undo/redo stacks, save point | `ProjectDocument` (Core) | memory |
| Dirty state | `ProjectDocument` hash, cached by the coordinator | derived |
| Validation result, statistics cache | per-session `ValidationCoordinator` (subscribes to `ProjectChanged`) | memory |
| Open projects, paths, locks, file stamps | `ProjectSession` | lock file; paths in settings |
| Documents, tabs, active document | `DocumentCollectionViewModel`/`ShellViewModel` | open paths in settings |
| Panel sizes, visibility, theme selection, density, recent files, last directories | `ShellViewModel`/settings region VMs (state only) | `Mocassin.Settings.json`, written by `AppSettingsStore` |
| Settings/recovery file bytes, app-data paths, theme resources, dispatcher | `Mocassin.App` (`AppSettingsStore`, `RecoveryStore`, `ProjectFileProbe`) and `Mocassin.App.Views` (`AvaloniaThemeApplier`) | the file system / Avalonia |
| Per-project view state (camera, visibility, styles) | later steps | project `viewSettings` |

Rules:

- No scientific logic in any view model: no arithmetic on energies/geometry, no unit conversion, no
  validation rules, no JSON parsing, no hashing beyond calling the coordinator. View models translate between
  library values and observable properties.
- No direct I/O in `Mocassin.App.ViewModels` (round-1 M-3): library APIs (`ProjectStore`,
  `ProjectValidationPipeline`, `ProjectTemplateFactory`) are allowed, `System.IO`, `Environment`,
  `Application`, and resource dictionaries are not. App-local files go through `IAppSettingsStore`,
  `IRecoveryStore`, and `IProjectFileProbe`; theme changes go through `IThemeApplier`. The architecture test
  in §20 enforces the token ban.
- UI state that must persist goes to app settings (app-scoped) or the project `viewSettings`
  (document-scoped). No ad-hoc state files. Step 23 writes nothing to `viewSettings`.
- All `ProjectDocument` mutations happen on the UI thread. Validation runs on the thread pool over an
  immutable snapshot; the result is applied on the UI thread only if it is not stale.
- `IUiDispatcher` (ViewModels) abstracts `Dispatcher.UIThread` for the few callbacks that arrive off-thread
  (timers, file checks, background validation completion). Tests use an inline implementation; that is a
  UI-thread boundary fake, which the mocks policy allows.
- `TimeProvider` is injected everywhere time is read or scheduled (`ProjectStore`, autosave, file stamps,
  session/retention decisions). Log record timestamps come from the logging sink, not from an injected clock
  (round-1 n-7). No `DateTime.Now`, no `Task.Delay` in production code.
- Long operations take a `CancellationToken`, report `IProgress<T>`, and never block the UI thread. No
  sync-over-async, no `Dispatcher.UIThread.Invoke` from a command body. This includes the crash path: the
  recovery writer awaits with a bounded budget instead of blocking (round-1 m-11, §6).
- One writer per project: the coordinator. One owner per open project: the session. One collection owns the
  tab set.

### 14. View-model and view contracts

- `Mocassin.App.ViewModels` is UI-framework-free (`ObservableObject`, `IAsyncRelayCommand`,
  `ObservableValidator`) and holds every view model, dialog/picker/dispatcher contract, navigation contract,
  session service, edit and validation coordinator, settings model and pure settings/recovery logic, and the
  command catalog. It references `CommunityToolkit.Mvvm` and the public library APIs; it has no Avalonia
  reference and no `System.IO` use (round-1 M-3, §1).
- `Mocassin.App.Views` holds `UserControl`s/`Window`s, the `DialogHost`, the shell window, the region views,
  the console/problems views, the settings dialog view, the palette, the theme/density/high-contrast resource
  dictionaries, and the `AvaloniaThemeApplier`. Views bind to view models; a view never calls a library or
  contains scientific logic. Code-behind is limited to view-only concerns (focus management, drag-reorder
  plumbing, splitter keyboard handling).
- `Mocassin.App` holds `Program`, `App.axaml(.cs)`, `ViewLocator`, the composition root, and the app-local I/O
  implementations that ViewModels must not own (`AppSettingsStore`, `RecoveryStore`, `ProjectFileProbe`,
  `AppPaths`, log retention). `ViewLocator` maps a view-model type to
  `Mocassin.App.Views.<same path minus "ViewModels">.<TypeName minus "ViewModel">View` by convention, with
  three safeguards: the type must live in `Mocassin.App.ViewModels` (or a later `*.ViewModels` assembly), the
  resolved type must be a control, and a missing view logs an error and renders an explicit "view not found"
  panel in Release (a plain text block in Debug). A silent blank control is a bug.
- **Visibility (round-2 M-4/n-12).** All **new** Step 23 types in the three App projects are `internal`, and
  `Mocassin.App.Tests` gets access through `[assembly: InternalsVisibleTo("Mocassin.App.Tests")]`. The only
  public types are the ones that already existed and are part of the compatibility contract or the process
  entry point: `Mocassin.App.ViewModels.AppViewModelsModule` and `Mocassin.App.Views.AppViewsModule` (public
  module markers, listed in `PublicAPI.Shipped.txt`, baselined under `tests/contracts/api-baseline/net11.0/`,
  and checked by ApiCompat) and `Mocassin.App.Program`/`App` (the process and Avalonia entry points;
  `Mocassin.App` is not an ApiCompat project). No public type is added or removed in Step 23, so those
  baselines and `PublicAPI.*` lists stay unchanged.
- Every view model is constructed with explicit dependencies; no `Application.Current` lookups, no static
  service access, no view references, no `System.Windows.*`.
- Observable properties are source-generated (`[ObservableProperty]`); commands use `[RelayCommand]`; computed
  bindable state is exposed as explicit properties with manual notifications rather than property chains in
  XAML bindings that re-derive on every read.

### 15. Project and folder layout

Following [`docs/development/code-structure.md`](../../development/code-structure.md) (`§2.3` is added by this
step): project roots hold only entry points/metadata, folders are concept-named, no catch-all folders, at most
20 files per folder, depth ≤4, ≤800 lines per file, namespace equals folder.

```text
src/dotnet/Mocassin.App/
  Program.cs  App.axaml  App.axaml.cs  ViewLocator.cs  AssemblyInfo.cs
  Composition/     AppHost.cs  AppServiceRegistration.cs  AppStartupOptions.cs  AppPaths.cs  LoggingSetup.cs  LogRetention.cs
  Settings/        AppSettingsStore.cs  AppSettingsPaths.cs  SettingsWritePolicy.cs
  Recovery/        RecoveryStore.cs  CrashRecoveryWriter.cs  RecoveryScan.cs
  Files/           ProjectFileProbe.cs  FileStampProvider.cs
  Startup/         StartupCoordinator.cs  RecoveryPromptPresenter.cs  CrashHandler.cs

src/dotnet/Mocassin.App.ViewModels/
  AppViewModelsModule.cs   # existing public marker (PublicAPI.Shipped.txt); retained unchanged
  Shell/           ShellViewModel.cs  ShellRegionsViewModel.cs  StatusBarViewModel.cs  ShellWindowState.cs
                   StatusBarItemViewModel.cs  WindowPlacementResolver.cs  IUiDispatcher.cs
  Documents/       DocumentViewModel.cs  DocumentKey.cs  DocumentCollectionViewModel.cs  WelcomeDocumentViewModel.cs
  Documents/Project/ ProjectOverviewViewModel.cs  ProjectJsonViewModel.cs  ProjectMetadataEditViewModel.cs
  Projects/        ProjectSession.cs  ProjectSessionService.cs  IProjectSessionService.cs  ProjectSessionResult.cs
                   ProjectSessionOutcome.cs  CloseSessionResult.cs  RecoveryScanResult.cs  RecoveryCandidate.cs
                   FileStamp.cs  ExternalChangeDetector.cs  RecentFilesViewModel.cs
  Scheduling/      AutosaveScheduler.cs  Debouncer.cs
  Editing/         ProjectEditCoordinator.cs  EditDiagnosticSet.cs  ProjectChangedEventArgs.cs
  Diagnostics/     ValidationCoordinator.cs  ProblemsViewModel.cs  DiagnosticItemViewModel.cs  ValidationState.cs
  Navigation/      INavigationService.cs  NavigationService.cs  INavigationTargetProvider.cs  NavigationIntent.cs
                   NavigationHistory.cs  ProjectNavigationTargetProvider.cs  IExplorerNodeProvider.cs
                   ExplorerNode.cs  InspectorSelection.cs  IInspectorContributor.cs
  Settings/        AppSettings.cs  ISettingsService.cs  SettingsService.cs  SettingsMigration.cs  AppSettingsValidator.cs
                   AppSettingsJsonContext.cs  IAppSettingsStore.cs  SettingsViewModel.cs  ThemeService.cs
                   ThemeSelection.cs  IThemeApplier.cs  LayoutState.cs  SessionState.cs  PathState.cs
  Recovery/        IRecoveryStore.cs  RecoveryRetentionPolicy.cs
  Commands/        ShellCommand.cs  ICommandCatalog.cs  CommandCatalog.cs  KeyGestureDescriptor.cs
                   ModifierKeys.cs  KeyName.cs  KeyGestureCatalog.cs  IMenuItemProvider.cs  MenuKind.cs
                   IConsoleChannelProvider.cs  IStatusBarItemProvider.cs  CommandPaletteViewModel.cs
  Consoles/        ConsoleViewModel.cs  ConsoleChannelViewModel.cs  ConsoleEntryViewModel.cs  IConsoleService.cs
                   ConsoleService.cs
  Dialogs/         IDialogService.cs  IStorageService.cs  IProgressDialog.cs  DialogViewModel.cs
                   DialogRequest.cs  ConfirmDialogRequest.cs  ChoiceDialogRequest.cs  TextPromptRequest.cs
                   ErrorDialogRequest.cs  ProgressDialogRequest.cs  DialogChoice.cs

src/dotnet/Mocassin.App.Views/
  AppViewsModule.cs        # existing public marker (PublicAPI.Shipped.txt); retained unchanged
  Shell/           ShellWindow.axaml(.cs)  ShellView.axaml(.cs)  MenuBarView  StatusBarView  ProjectSwitcherView
  Documents/       DocumentTabStripView  DocumentHostView  WelcomeView  ProjectOverviewView  ProjectJsonView
  Navigation/      ProjectExplorerView
  Consoles/        ConsoleView  ProblemsView
  Settings/        SettingsView
  Dialogs/         DialogHost  ConfirmDialogView  ChoiceDialogView  PromptDialogView  ErrorDialogView
                   ProgressDialogView  CrashDialogView  CommandPaletteView
  Controls/        SplitterHost  NotificationHost
  Theming/         AvaloniaThemeApplier.cs  ThemeResources.axaml
  Themes/          Density.axaml  HighContrast.axaml  Accent.axaml  Metrics.axaml
  Platform/        AvaloniaStorageService.cs  AvaloniaUiDispatcher.cs  ViewKeyBindingBinder.cs

src/dotnet/Mocassin.App.Tests/
  TestSupport/     ManualTimeProvider.cs  InlineUiDispatcher.cs  RecordingDialogService.cs  StubStorageService.cs
                   InMemoryAppSettingsStore.cs  InMemoryRecoveryStore.cs  TempWorkspace.cs  HeadlessAppBuilder.cs
                   ProjectFixtures.cs
  ViewModels/…  Views/…  Visual/…  Visual/Baselines/…
```

No `Internal/` folder is added at a project root (`Internal` is nested-only per the structure policy); the
debouncer and autosave scheduler live in `Scheduling/`, and any other helper lives in the concept folder that
owns it.

**Visibility and PublicAPI (round-1 m-8, corrected by round-2 M-4).** Every **new** type in the three App
projects is `internal`; the shell, its services, and the contribution seams are consumed by Steps 24–28, which
edit these same assemblies, so no later step needs a new public type from them. `Mocassin.App.Tests` gets access
through `[assembly: InternalsVisibleTo("Mocassin.App.Tests")]` in each project's `AssemblyInfo.cs`.

The existing public types are **exceptions, not part of the internal rule**:

| Type | Why it stays public | Guard |
| --- | --- | --- |
| `Mocassin.App.ViewModels.AppViewModelsModule` | pre-existing public module marker, listed in that project's `PublicAPI.Shipped.txt` | ApiCompat baseline `tests/contracts/api-baseline/net11.0/Mocassin.App.ViewModels.dll` unchanged |
| `Mocassin.App.Views.AppViewsModule` | same, for the views project | ApiCompat baseline `…/Mocassin.App.Views.dll` unchanged |
| `Mocassin.App.Program`, `Mocassin.App.App` | process entry point and the Avalonia `Application` subclass; `Mocassin.App` is not in `tools/api-projects.txt` and has no baseline | not a compatibility surface |

Step 23 therefore adds and removes **no** public type in the App projects: `PublicAPI.Shipped.txt`,
`PublicAPI.Unshipped.txt`, and the two App ApiCompat baselines stay unchanged, and `./tools/verify.sh policy`
continues to pass without a baseline refresh. Only the Core/Packages surface in §16 is new public API, listed in
those projects' `PublicAPI.Unshipped.txt`. Making an existing public marker internal, or adding a public App
type, is an intentional public-API change that needs `PublicAPI`/baseline updates and owner approval; if a later
step needs one, it records an amendment rather than widening the surface silently.

- The `Mocassin.App.ViewModels.csproj` gains `CommunityToolkit.Mvvm`; `Mocassin.App.csproj` gains
  `Avalonia`, `Avalonia.Desktop`, `Avalonia.Fonts.Inter`, `Avalonia.Themes.Fluent`, `Microsoft.Extensions.*`,
  `Serilog` + sinks; `Mocassin.App.Views.csproj` gains `Avalonia` and `Avalonia.Themes.Fluent`;
  `Mocassin.App.Tests.csproj` gains `Avalonia.Headless` and `Avalonia.Headless.XUnit` (test-only, §20).
  All versions come from `Directory.Packages.props`; the Avalonia packages stay pinned to 12.1.6 and the
  headless packages must use the same version.
- Every listed file is a single-type file; the design does not rely on a multi-type suffix. If a feature needs
  an additional concept type, it adds a file. The implementing feature runs `./tools/verify.sh structure` and
  does not weaken `tools/structure-policy.json` to make a folder or file name pass.

### 16. Library changes owned by this step

Four additive, behaviour-preserving changes. All are small, land with their own tests, and are listed in the
`PublicAPI.Unshipped.txt` of `Mocassin.Core`/`Mocassin.Packages`. Because every change is additive, no ApiCompat
baseline refresh is expected for Core/Packages; if `./tools/verify.sh api` reports a removed or changed member,
the change is wrong and must be redesigned rather than baselined away.

`Mocassin.App*` adds and removes **no** public type. Its two existing public module markers and the
`Mocassin.App.Program`/`App` entry points stay exactly as they are today, so the App `PublicAPI.*` lists and the
`tests/contracts/api-baseline/net11.0/Mocassin.App.*.dll` baselines are unchanged (§14/§15, round-2 M-4). The
policy gate therefore runs unmodified; making a marker internal or adding a public App type is a separate,
owner-approved public-API change with a baseline refresh.

| Change | Where | Why the shell cannot own it | Contract effect |
| --- | --- | --- | --- |
| `ProjectDocument.MaxUndoDepth` (init-only, default 200; oldest entries dropped) | `Mocassin.Core.Editing` | The stack lives in the document; a UI-level bound would duplicate or shadow the stack | Additive property; no signature change; default preserves Step 01 behaviour |
| `ProjectDocument.CreateRecovered(Project current, Project lastSaved, ProjectEditor? editor = null)` | `Mocassin.Core.Editing` | Recovery needs a clean point different from the current value; `MarkSaved(value)` overwrites both | Additive static factory; existing constructors unchanged |
| `ProjectStore.WriteAutosaveToPathAsync(Project, string autosavePath, CancellationToken)` | `Mocassin.Packages.Projects` | Untitled projects have no project path to derive `<path>.autosave` from; re-implementing the atomic JSON write in the app would duplicate Step 01 | Additive overload; reuses `AutosaveFailed`; existing sidecar API unchanged |
| `ProjectPathRules.NormalizeName/ValidateTarget` + `NewProjectCommand` refactor to call it | `Mocassin.Packages.Projects.Templates` and `Mocassin.Cli.NewProject` | `File > New` must enforce exactly the CLI's name **and target-path** rules (trimmed name, `.mocprj` suffix, length, separators, `..`, control and reserved characters, existing target, directory creation); duplicating them in the app guarantees drift | New public helper; CLI codes, exit codes, and file behaviour must remain identical, and **new CLI regression tests pin the exact message text** moved into the helper, so the extraction cannot silently reword it (round-1 m-3) |

No other library change is allowed in this step. If implementing a feature appears to need a fifth change, the
step agent stops and records an amendment rather than editing a library ad hoc.

The CLI refactor is a **behaviour-preserving move**, not a rewrite: `NewProjectCommand` keeps its `CLI001`/2
usage errors, `CLI002`/3 I/O errors, `CLI003`/3 existing-target error, its exit codes, and its JSON report.
The new tests are added to `Mocassin.Cli.Tests` for the message text of each rejected name/path case, because
the existing tests assert only the code and exit code (round-1 m-3), and the GUI tests assert that the GUI shows
the same message for the same input.

### 17. Settings file format (`mocassin.settings` v1)

Application-local, single-consumer, not a cross-language contract. It is therefore **not** in
`contracts/manifest.json`, has no cross-language fixtures, and is documented in
[`docs/reference/file-layout.md`](../../reference/file-layout.md) with a checked-in example. It is a
versioned format with the same discipline as the project file: atomic write, explicit version, pure-function
migrations, forward-compatible reading.

```json
{
  "format": "mocassin.settings",
  "schemaVersion": 1,
  "theme": "system",
  "accent": null,
  "density": "comfortable",
  "fontScale": 1.0,
  "highContrast": false,
  "reduceMotion": false,
  "culture": "en",
  "logging": { "level": "information" },
  "ui": { "confirmOnExternalChange": true, "showWelcomeOnStart": true },
  "paths": { "lastOpenDirectory": "/work", "lastSaveDirectory": "/work", "lastFolderDirectory": "/work" },
  "recentFiles": [ { "path": "/work/ceria.mocprj", "lastOpenedUtc": "2026-11-20T10:00:00Z" } ],
  "layout": { "leftWidth": 260, "rightWidth": 320, "consoleHeight": 180,
              "leftVisible": true, "rightVisible": true, "consoleVisible": true,
              "activeConsoleChannel": "problems" },
  "autosave": { "enabled": true, "intervalMinutes": 5 },
  "session": { "openProjects": ["/work/ceria.mocprj"], "activeProject": "/work/ceria.mocprj",
               "cleanExit": true, "savedUtc": "2026-11-20T10:00:00Z" }
}
```

- Written atomically: serialise through the source-generated `AppSettingsJsonContext` to UTF-8 without BOM,
  2-space indent, `\n` line endings; write a sibling temp file, flush and `Flush(flushToDisk: true)`, then
  replace the target (round-1 m-6). A failed or interrupted write leaves the previous file intact.
- `MaxSettingsBytes = 4 MiB` bounds the document (and `recentFiles` is capped at 10 entries); a larger file is
  rejected as invalid, not loaded, so a corrupt or hostile settings file cannot exhaust memory.
- Reading: `format` must equal `mocassin.settings`; `schemaVersion` above the current major is rejected without
  overwriting; unknown properties are ignored; each field is validated and falls back to its default with a
  logged warning.
- Migrations are pure functions (`SettingsMigration`) keyed by `schemaVersion`. Version 1 is the first.
- **Step 23 ships only fields it consumes.** Later steps add fields additively (missing = default) without a
  schema bump; a bump is required only for a breaking change (renamed/removed field or changed meaning).
  Solver path, package output, and results directories are deliberately absent until Steps 26/28 consume them.
  `paths` is owned by feature 3/4, and `autosave`/`session` by feature 7: under an approved split, 23B adds those
  two objects to the same v1 document with **no schema bump**, which is exactly the additive rule in action.
- The window size/state and splitter positions are app-scoped, so they live here, not in `viewSettings`.
  Nothing in this step writes `viewSettings`.

### 18. Dependency and interface checks (parallel track)

**Consumed library surfaces (all merged, no changes requested):**

| Capability | Library API | What the shell owns |
| --- | --- | --- |
| Load/save/migrate/lock/autosave | `ProjectStore` **with `ProjectStoreOptions { ValidateOnLoad = false }`**, `ProjectLoadResult` (project + `Diagnostics` + `FailureKind`), `ProjectSaveResult`, `ProjectLockResult`, `ProjectFileLock`, `ProjectStore.GetAutosavePath/AcquireLockAsync` | prompts, paths, recent list, load-diagnostic retention, file stamps, read-only mode, scheduling |
| Project value and editing | `Project`, `ProjectDocument`, `ProjectEditor`, `ProjectChange`, `ProjectEditResult`, `ProjectHasher`, `ProjectJson` | commands, dirty presentation, undo/redo wiring |
| Templates | `ProjectTemplateFactory`, `ProjectTemplate`, `ProjectTemplateKind` | the New dialog, name/path validation via `ProjectPathRules`, recent/list wiring |
| Validation | `ProjectValidationPipeline.Validate(project, loadDiagnostics, …)`, `ValidationResult`, `ValidationOptions`, `DiagnosticSummary`, `ModelStatisticsService` | the `Load` stage seeding, debounce, staleness guard, Problems surface, navigation |
| Diagnostics | `Diagnostic`, `DiagnosticSeverity`, `ModelPath`, catalog codes | severity icons, filters, help anchors, navigation targets |
| Settings/logging | `TimeProvider`, `Microsoft.Extensions.*`, Serilog | the whole settings and logging policy |

The shell does not re-implement any of these. Concretely: no second JSON reader/writer, no second validator,
no second template definition, no second lock, no second hash, no second diagnostic catalog, no second
`ProjectStore` wrapper interface.

**Not consumed (later steps):**

| Step | Adds | Shell seam it uses |
| --- | --- | --- |
| 24 | particle/structure/transition editors, explorer nodes, inspector content | `INavigationTargetProvider` (`particle`, `particleSet`, `structure`, `site`, `transition`), `IExplorerNodeProvider`, `IInspectorContributor` |
| 25 | energy/lattice/simulation editors | same seams; new domains |
| 26 | parameterization/job-template editors, build UI, Build menu and channel | `IMenuItemProvider`, `IConsoleChannelProvider`, `IStatusBarItemProvider`, `IProjectSessionService`/`ProjectEditCoordinator` for the selected project |
| 27 | 3D viewer and picking | `IInspectorContributor`/document host, `ProjectEditCoordinator.ProjectChanged`, `ProjectSession` |
| 28 | run monitoring, charts, MMCFE UI | `IConsoleChannelProvider`, `IStatusBarItemProvider`, `IMenuItemProvider`, session-scoped services |
| 19–21 | solver CLI, state reader, evaluation | none in this step; Steps 26/28 compose them |

**Rules that keep the tracks independent**

- Step 23 code must not reference `Mocassin.Compilation`, `Mocassin.Results` APIs, `Mocassin.Cli`, or any
  solver package. The only `Results` reference is the pre-existing project-reference edge.
- No provider is registered for a domain Step 23 does not implement; an unresolved domain is a notice, never
  a crash and never a fake editor.
- The shell must be usable with zero editors: new/open/inspect-metadata/validate/save/recover all work.
- A later step may add a provider without touching the shell; if a later step needs a new shell seam, it adds
  the interface in `Mocassin.App.ViewModels` and registers it, and that is recorded in that step's page.

### 19. Documentation plan

| Page | 23A content (features 1–4) | 23B completion (features 5–7) |
| --- | --- | --- |
| `docs/getting-started/installation.md` | prerequisites, per-OS install, first launch, data-dir location, GPU-free operation | — |
| `docs/getting-started/quickstart.md` | create/open/inspect/save/close/reopen, recents, locks, read-only mode, external-change reload | validation/Problems, metadata editing, undo/redo, autosave/recovery sections |
| `docs/getting-started/first-project.md` | templates, workspace, tabs, settings/theme, project lifecycle | undo/redo, dirty/save point, autosave, crash recovery |
| `docs/user-guide/projects.md` | lifecycle reference: new/open/save/save-as/close/reload, recents, locks, read-only, external changes, settings | undo/redo, dirty state and save point, autosave, recovery, retention |
| `docs/development/ui-architecture.md` | composition root, dependency direction and the I/O boundary, document/navigation model, state ownership, seams, settings format | edit pipeline, validation surface, recovery, harness/screenshot/a11y chapters |
| `docs/troubleshooting/installation.md` | startup failures, missing display/Wayland nuance, unwritable data dir, corrupt settings, locked project | recovery prompts and recovery-file problems |
| `docs/reference/file-layout.md` | app-data layout, `Mocassin.Settings.json` field table and example | `recovery/`, autosave sidecar, retention rules |
| `docs/development/index.md`, `docs/user-guide/index.md`, `docs/troubleshooting/index.md`, `zensical.toml` | navigation entries for the pages that exist at 23A closeout | no further change beyond new anchors |
| `docs/development/testing-and-parity.md` | harness and PR-fast/PR-medium split | screenshot, a11y, recovery and crash-path policy |

Under the split, 23A writes only the 23A column and 23B adds the 23B column; no page ships a section for a
capability its step has not implemented (round-2 m-17). Under one unit, features 4 and 8 write both columns in
order.

No page may describe an editor, build UI, 3D, or run monitoring as available. Screenshots in the user guide
must be regenerated from the real app; a placeholder is not acceptable.

### 20. Test plan

Harness: `Mocassin.App.Tests` uses Avalonia headless for view tests and plain xUnit for view-model tests.
Real objects everywhere: real `ProjectStore`, real temporary directories, real `Project` values, real
validation pipeline, real dialog overlay, and the real `AppSettingsStore`/`RecoveryStore`/`ProjectFileProbe`
against a temporary data directory where the test is about file behaviour. Permitted fakes are exactly the true
boundaries: `IDialogService` (view-model-level tests only), `IStorageService`, `IUiDispatcher`, `TimeProvider`,
and in-memory `IAppSettingsStore`/`IRecoveryStore` **only** for tests that are not about persistence (the
persistence tests use the real App-layer stores). No fake `Project`, no fake validator, no fake store, no fake
JSON.

| Area | Tier | Assertion |
| --- | --- | --- |
| Startup/composition | PR-fast | `AddMocassinApp` resolves every registered service; the shell view model constructs; the shell renders in headless without a display; duplicate navigation-provider registration fails fast |
| Architecture | PR-fast | **Direct** project references only (read from the `.csproj`, not the transitive assembly graph): `Mocassin.App.ViewModels` has no direct Avalonia or `Mocassin.App.Views` reference, and `Mocassin.App` has no direct `Mocassin.Cli` or `Mocassin.Compilation` reference (the `App → Packages → Compilation` transitive edge is expected and allowed — round-1 m-9). **I/O token scan (M-3/m-13):** after stripping comments and string literals, no file in `Mocassin.App.ViewModels/**/*.cs` contains `System.IO`, `File.`, `FileInfo`, `Directory.`, `DirectoryInfo`, `Path.Combine`, `Path.Get`, `FileStream`, `StreamReader`, `StreamWriter`, `Environment.`, `Avalonia`, `Dispatcher.UIThread`, `Application.Current`, or `ResourceDictionary`; `IUiDispatcher`/`Dispatcher` and `Application` as bare identifiers are permitted. A **positive control** feeds the scanner a synthetic source line with a banned token and asserts it is reported, so the test cannot pass by matching nothing (round-2 m-13) |
| Window placement | PR-fast | A saved placement that intersects no current screen resolves to the primary screen; a saved placement that intersects one is kept (round-1 n-5) |
| New/open/save/close | PR-fast | New from both templates creates a file the store reopens; save → close → open is content-identical; save-as migrates the lock and preserves undo history; an existing target is refused |
| Dirty/save point | PR-fast | `*` appears and clears; save → undo → redo is clean; save → edit → undo is clean; undo below the save point is dirty |
| Autosave | PR-fast | Only dirty documents are written; the interval comes from `TimeProvider`; save and autosave never interleave; a clean save deletes the sidecar/recovery file |
| Recovery | PR-fast | A simulated crash leaves a sidecar; startup offers recovery; `RecoverAsync` produces a dirty document whose content equals the snapshot; `DiscardRecoveryAsync` deletes; `ReloadAsync` replaces the document and clears the stacks; retention removes only old recovery files; the crash writer completes within its budget or reports the timeout without blocking |
| Untitled recovery | PR-fast | An untitled dirty project writes `<app-data>/recovery/<projectId>.mocprj.autosave` and is offered at next start |
| Locks/read-only | PR-fast | A second session opens read-only with a banner; edit/save are disabled; save-as acquires the new lock; a stale lock is reclaimed per store policy |
| External change | PR-fast | Clean + changed file → prompt and reload; dirty + changed file → no silent overwrite; unchanged file → no prompt |
| Recent files | PR-fast | Order, de-duplication, missing-file disable/remove, persistence across restart |
| Settings | PR-fast | Round-trip through the real `AppSettingsStore`; defaults on invalid field; unknown field ignored; future major rejected and file not overwritten; migration is idempotent; a failed/oversized write leaves the previous file intact; last directories persist and are updated by the picker |
| Name/path rules | PR-fast | GUI and CLI produce the same message and decision for every rejected name/path case (invalid characters, `..`, over-length, existing target), via the shared `ProjectPathRules` (round-1 m-3/n-6) |
| Undo/redo | PR-fast | Multi-step apply/undo/redo through the Project Overview document on the shared project stack (the cross-editor proof is Step 24); bounded depth drops the oldest entry; redo is cleared by a new edit |
| Rejected edit | PR-fast | Validation failure leaves the project and dirty state unchanged and surfaces diagnostics |
| Validation contract (M-1) | PR-fast | The composed `ProjectStore` has `ValidateOnLoad == false`; with a load/migration fixture the pipeline's `Load` stage is `Completed`/`Failed` and carries `ProjectLoadResult.Diagnostics`; with a clean fixture it is `Skipped` with count 0; the migration/load diagnostic reaches the Problems list; no diagnostic is duplicated; `ValidateFileAsync` is not used by the GUI (round-2 m-14) |
| Validation surface | PR-fast | Debounce coalesces edits; a stale result is discarded; counts and codes match `ValidationResult`; navigation targets are requested for double-click |
| Navigation | PR-fast | Open/reuse by key, activate, close, back/forward, unknown domain notice, provider registration per project scope |
| Command catalog/palette | PR-fast | Every shell command appears with its gesture; `CanExecute` disables menu and palette together; `primary` resolves to Ctrl/Meta as designed |
| Dialogs | PR-fast | Real overlay flow: confirm/choose/prompt/error/close-cancel; focus is captured and restored; a scripted dialog fake is used only in view-model tests |
| Accessibility | PR-fast (shell), PR-medium (full tree) | Names/roles on the primary controls, no icon-only control without a name, tab order matches visual order, status is not colour-only |
| Headless views | PR-medium | Shell, tab strip, explorer, Problems, settings dialog render and respond to keyboard input |
| Screenshots | PR-medium (Linux) | Light/dark/high-contrast shell baselines with a documented tolerance and a regeneration command |
| Platform gate | manual (Windows, macOS) | The headless suite plus a short manual checklist: start, new/open/save, theme switch, keyboard-only pass, crash dialog |
| Manual screen reader | release checklist | NVDA (Windows), Orca (Linux), VoiceOver (macOS) — recorded, not automated |

Screenshot policy: baselines are generated on `ubuntu-latest` with a fixed headless render scale and are
compared only in the Ubuntu job. Windows/macOS produce screenshots as manual evidence in the platform gate and
are not pixel-compared, because text rendering differs per platform and a cross-OS pixel gate would be a
permanent false failure. This is a deliberate narrowing of the step page's "screenshot tests on all three OSes"
and is recorded in §24.

Tier declaration uses the existing `[Trait("Tier", "PR-fast"|"PR-medium")]` convention; screenshots and any
longer matrix use `PR-medium`. No new test infrastructure or package beyond the Avalonia headless packages.

### 21. Performance expectations (non-gating)

No absolute wall-clock timing is asserted in a PR-fast test (round-1 m-4). The quality strategy allows absolute
comparisons only on the same runner class, and the tier table puts performance in nightly/release. Step 23
therefore separates *budgets* (design targets used to judge a manual or nightly measurement) from *assertions*
(a behaviour that fails the build):

| Scenario | Design budget | How it is observed | Gated? |
| --- | --- | --- | --- |
| Cold start (empty session, warm file cache) | < 2 s | manual release measurement recorded with OS/CPU | no |
| Open the ceria sample | < 500 ms | recorded measurement on the benchmark runner and in the feature log | no |
| Save the ceria sample | < 500 ms | same | no |
| Live validation (ceria, no statistics) | < 200 ms (Step 07 budget) | recorded measurement at feature 6 closeout | no |
| Statistics (ceria) | < 2 s (Step 07 budget) | recorded measurement; statistics are opt-in and off the UI thread | no |
| Shell render (headless) | < 100 ms | a generous hang-guard timeout in the headless view test (seconds, not a perf assertion) | only as a hang guard |
| Autosave write (ceria) | < 500 ms, non-blocking | functional test asserts that the UI thread is not blocked and that the write completes; timing is recorded | functional only |

The PR-fast suite asserts functionality, ordering, and that long operations are asynchronous; it does not assert
a duration. If a measurement later needs to become a gate, it is added as a same-runner-class benchmark entry
with a noise floor, per `03-quality-strategy.md` §13, not as an absolute stopwatch assertion. No benchmark
harness is added in this step; `Mocassin.Benchmarks` may include save/load later. Numbers are recorded in the
step page feature log at closeout.

### 22. Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Avalonia 12.1.6 API/behaviour uncertainty (headless screenshots, high contrast, storage provider) | Verification items V-1…V-4 in §24; each has a fallback and is resolved in the feature that needs it, before the design is relied on |
| UI thread violations | One dispatcher boundary, no blocking waits, background validation with a hash guard, arch test for view-model purity |
| Large project tree/serialization performance | Project-level tree only in this step; statistics opt-in; dirty state cached; lazy document content |
| Undo/redo complexity | Uses the Step 01 immutable `ProjectDocument`; no second stack; bounded depth; save-point tests |
| Autosave/save races | Per-session semaphore, immutable snapshots, atomic writes from the store |
| Platform differences (paths, case, primary modifier, fonts) | `Path.Combine`/`SpecialFolder`, explicit case-sensitivity decision, `OperatingSystem` checks, manual platform gate |
| Scope creep into editors/build/3D | Non-goals list, contribution seams instead of placeholder UI, per-feature scope in the feature table |
| Parallel track: Step 13 changes shared files | This design touches planning docs only; the implementor merges `main` before starting and re-reads the library surfaces |
| Hidden coupling to a later step's API | §18 allow-list; an implementor who needs a later API stops and reports |
| Accessibility regressions | Names/roles in the same commit as the control; accessibility assertions in the PR-fast set for the primary controls |
| Test flakiness (timers, screenshots) | `ManualTimeProvider` for scheduling; no real sleeps; screenshot tolerance and Linux-only pixel comparison |
| Settings corrupt/forward-versioned file | Never overwrite on read failure; banner + "save as new"; defaults with logged warnings |

### 23. Complexity class and split decision

Assessed on 2026-11-20 against [`04-roadmap.md`](../04-roadmap.md) §3.1, after the complexity rule replaced the
person-week trigger. The class is the **highest** dimension, not an average.

| Dimension | Class | Evidence |
| --- | --- | --- |
| Contracts, formats, and public surfaces | **Medium** | One new app-local versioned file (`mocassin.settings` v1, single consumer, not in the manifest) and four additive library APIs in Core/Packages. No change to `.mocprj`, `.msl`, `.msr`, `.mcs`, signatures, CLI reports/exit codes, or diagnostic codes |
| New dependencies | **Medium** | No new production package: Avalonia 12.1.6, CommunityToolkit.Mvvm, Microsoft.Extensions.*, and Serilog are already pinned. Two first-party test-only Avalonia headless packages (same pinned version), plus a possible explicit `Avalonia.Skia` reference (V-1). All confined to the isolated App projects |
| Cross-step interfaces | **High** | The document model, navigation/explorer/inspector/menu/console/status seams, session and edit coordinator, and dialog/picker contracts are consumed by Steps 24, 25, 26, 27, and 28. A late change rewrites editor code in five later steps |
| Irreversibility | **Medium** | The settings format and additive APIs are shipped surfaces but reversible by a migration or a new overload; no scientific, signature, or persisted project contract changes. The seam *shape* is the least reversible part and is covered by the cross-step row |
| Test-matrix breadth and failure modes | **High** | Headless view-model, headless view, screenshot, accessibility-tree, manual platform (Windows/macOS), file-I/O lifecycle/recovery, settings migration, UI-thread/staleness, and a new test harness; failures include path case/separators, timers, locks, crash handling, and platform gesture mapping |
| Independent acceptance stories | **High** | Three stories the owner could accept or reject separately: (a) the workspace shell renders, navigates, and is keyboard/accessible; (b) project lifecycle is trustworthy (new/open/save/save-as/close/reload/recents/locks/external change); (c) editing safety and diagnostics (undo/redo, dirty/save point, autosave/recovery, validation surface) |
| Evidence size for a fresh auditor | **High** | Eight features, a new application project plus a headless harness and screenshot baselines, four library changes, six documentation pages, and a design page of roughly 1,400 lines; the diff spans App/ViewModels/Views/Tests plus Core/Packages |

**Class: High.** *Justification (normative record):* Step 23 is High because it introduces a cross-step
interface surface that five later GUI steps consume, spans a broad test matrix with platform-specific failure
modes and a new headless/screenshot/accessibility harness, delivers three independent acceptance stories, and
produces an evidence set an auditor cannot realistically hold in one sitting; its contract and dependency
dimensions are only Medium because the settings file is app-local and no production package is added.

#### Split decision

The rule splits a High step unless the owner accepts it as one unit. **The planner proposes a split after
feature 4 and recommends it**, because the boundary is dependency-clean and the two halves are independently
demonstrable:

| Group | Features | Acceptable outcome on its own |
| --- | --- | --- |
| **23A — Workspace and project lifecycle** | 1–4 plus the 23A documentation column (§19) and closeout | Create/open/navigate/save/save-as/close/reload with recents, locks, read-only mode, and external-change detection; settings, theme, density and accessibility in place; keyboard-operable shell |
| **23B — Editing safety, diagnostics, and closeout** | 5–8 | Project metadata editing with undo/redo; live validation with navigable Problems and on-demand statistics; save-point-correct dirty state; autosave and crash recovery; the 23B documentation column, shared harness, screenshots, and the final closeout |

- **Dependency-clean:** after the M-2 reorder, every feature depends only on lower-numbered features; 23A
  contains no task from 23B, and 23B needs nothing that 23A does not deliver (the dialog/picker/console/
  notification/progress surfaces are in feature 1). The 23B features do not require the 23A closeout.
- **Cost of the split:** one new roadmap step and dependency edge (Step 24 would depend on 23B), a Phase F index
  and milestone update, two closeout evidence files, and a duplicated (small) documentation sweep. The shared
  harness is built once in 23A and extended in 23B, not duplicated.
- **Why not split at a different line:** splitting before lifecycle (e.g. features 1–3) leaves an app that cannot
  open a project, so it has no acceptance story; splitting inside 23B (edit vs recovery) strands dirty tracking
  without an edit surface or recovery without dirty state.

#### Alternative: owner acceptance of the High step as one unit

If the owner prefers one unit to avoid the extra step, dependency edge, and duplicated closeout, §3.1 allows an
explicit acceptance recorded here and in the roadmap. The case for one unit is: Step 24 needs the whole shell
anyway (documents, navigation, edit coordinator, validation, recovery), the harness and screenshot baselines are
shared across all eight features, and the three acceptance stories are delivered under one step branch and one
audit rather than two. The case against is the evidence size above: the auditor must hold the whole UI
foundation, the harness, four library changes, and the docs at once.

**Risk guard if accepted as one unit:** the step agent stops and reports to the orchestrator before feature 5 if
any of the following holds — the headless/screenshot harness (feature 1–2) is not green on Ubuntu; the M-1
validation contract cannot be demonstrated with a real load diagnostic; or the M-3 I/O boundary cannot be
enforced by the architecture test without an allow-list that defeats it. In any of those cases the step is
re-cut before more code lands, because the later features would build on an unproven foundation.

#### Recording and status

- The class and this paragraph are the record required by §3.1. They are repeated in the implementation-status
  block; the §3 table stays a non-gating forecast.
- **No owner decision is recorded yet.** The owner chooses split or one-unit acceptance at the start gate; if
  the split is approved, the roadmap index, dependency graph, §3 forecasts, Phase F milestone, and the Step 24
  dependency are updated in the same change, and each half records its own closeout evidence.
- This replaces the previous person-week framing entirely: there is no time-waiver request, no contingency
  arithmetic, and no person-week guard. The §3 row remains a scheduling forecast only.

### 24. Decisions recorded and owner input

| Decision | Record |
| --- | --- |
| Depends on 01 + 03–07 + 12, not on 08–11 or 13–22; the step page's earlier "Depends on: 01" was understated | This page header/§18; [`04-roadmap.md`](../04-roadmap.md) §3 |
| Undo/redo is `Mocassin.Core.Editing.ProjectDocument`; Step 23 adds only the VM adapter, bounded depth, and recovery factory | This page §7/§16; [`appendix-b-parity-matrix.md`](../appendix-b-parity-matrix.md) |
| No `IProjectStore`, `IBuildService`, `ISimulationService`, or `IUndoRedoService` interfaces are invented; concrete library APIs plus session-level services only | This page §5/§18 |
| Services return typed outcomes and never prompt; only the shell prompts | This page §5 |
| Save uses `MarkSaved()` (no-arg) and keeps `LastWritten` separately, so undo→redo across a save is clean | This page §6/§7 |
| One global tab strip with project-scoped document keys and a project switcher | This page §3/§5 |
| Dialogs are an in-shell overlay; no `MsBox.Avalonia`, no new control dependency | This page §11; ADR 0003 |
| `mocassin.settings` is application-local, versioned, migratable, not in `contracts/manifest.json` | This page §17; [`05-open-decisions.md`](../05-open-decisions.md) |
| App-data root is `<LocalApplicationData>/Mocassin` with `logs/`, `cache/`, `recovery/`; override via `--data-dir`/`MOCASSIN_DATA_DIR` | This page §9/§17; [`docs/reference/file-layout.md`](../../reference/file-layout.md) |
| No splash window; the welcome document covers the empty/loading state | This page §2 |
| `Model`/`Build`/`Run` menus appear only when a later step contributes items | This page §10 |
| Screenshot pixel comparison is Ubuntu-only; other platforms are manual evidence | This page §20 |
| Untitled-project recovery lives in `recovery/`; saved-project autosave stays the Step 01 sidecar; sidecars are never swept by retention | This page §6 |
| Four additive library changes and no others | This page §16 |
| Complexity class **High** with the dimension table and justification; split proposed after feature 4 and recommended; one-unit acceptance case and risk guard recorded | This page §23; [`04-roadmap.md`](../04-roadmap.md) §3.1 |
| UI I/O boundary: ViewModels may call library APIs that do I/O but never `System.IO`/`Environment`/`Application`/resource dictionaries; app-local settings, recovery, file probing, and theming are implemented in `App`/`Views` behind ViewModels contracts, enforced by a source-token test | This page §1/§13/§15; [`01-target-architecture.md`](../01-target-architecture.md) §3.2; [`code-structure.md`](../../development/code-structure.md) §2.3 |
| Validation is the in-memory pipeline entry with `ValidateOnLoad = false` and the stored load diagnostics; no double validation, no skipped `Load` stage | This page §5/§8/§20 |
| Session operations include `Activate`, `Reload`, `Recover`, and `DiscardRecovery`; outcomes include `NotFound`/`InvalidContent`/`IoFailure` | This page §5/§6 |
| Key gestures are described by a framework-neutral `KeyGestureDescriptor` with a `Primary` modifier; only the Views layer binds Avalonia gestures | This page §10 |
| Every **new** App type is `internal` with `InternalsVisibleTo` for tests; the pre-existing public `AppViewModelsModule`/`AppViewsModule` markers and `Program`/`App` entry points stay public and baselined, so no App `PublicAPI` or ApiCompat baseline changes | This page §14/§15/§16; [`01-target-architecture.md`](../01-target-architecture.md) §3.2; [`code-structure.md`](../../development/code-structure.md) §2.3 |
| The I/O token list is exact (`System.IO`, `File.`/`FileInfo`, `Directory.`/`DirectoryInfo`, `Path.Combine`/`Path.Get`, `FileStream`, `StreamReader`, `StreamWriter`, `Environment.`, `Avalonia`, `Dispatcher.UIThread`, `Application.Current`, `ResourceDictionary`), with `IUiDispatcher`/`Dispatcher` and bare `Application` permitted, and a positive control proves the scanner works | This page §1/§20 |
| The `Load` stage is `Completed`/`Failed` only when load diagnostics exist; a clean load is `Skipped` with count 0 | This page §8/§20 |

**Verification items** (must be confirmed by the named feature; not guessed here):

| ID | Question | Feature | Fallback |
| --- | --- | --- | --- |
| V-1 | Exact Avalonia 12.1.6 `AppBuilder`/headless setup, and whether `UseSkia()` is available through the `Mocassin.App` reference for screenshot rendering | 1/8 | Add an explicit `Avalonia.Skia` reference at the pinned version and record it as a dependency delta |
| V-2 | Whether Avalonia 12.1.6 exposes a platform high-contrast theme variant/resource dictionary | 3 | Ship the app-owned `HighContrast.axaml` and select it from settings only |
| V-3 | Whether the pinned `Serilog.Sinks.File` supports `retainedFileSizeLimitBytes` | 1 | Implement `LogRetention` as an explicit startup sweep |
| V-4 | `IStorageProvider` picker behaviour for "save as" on each OS (extension filter, overwrite confirmation) | 4 | Own filtering and confirmation in `IStorageService` and document the per-OS behaviour |
| V-5 | Whether a saved window placement can be validated headlessly against a real monitor topology | 1 | Test the pure `WindowPlacementResolver` against synthetic screen rectangles and record the real multi-monitor check as manual platform-gate evidence |

**New ADRs:** none. ADR 0003 (Avalonia/MVVM/DI) and ADR 0006 (3D, untouched) stand; ADR 0008 and 0011/0013 are
not affected. The shell decisions are step-level and are recorded here, in the architecture, the quality
strategy, the open-decisions page, and the code-structure page.

**Owner input required at the start gate:** the complexity-class decision (§23: split as recommended, or
accept one High unit with the recorded guard), the eight-feature list, and the acknowledgement of the two
test-only Avalonia headless packages and the four additive library changes in §16. No public *format*, manifest,
or CLI contract changes, and no time waiver is requested.

## Implementation tasks

Tasks are grouped by the feature that owns them and follow that order (feature 1 first). The dialog/picker/
console/notification/progress surfaces, the settings I/O split, and the validation contract are prerequisites,
not follow-ups.

- [ ] **1 — Host and boundaries.** Create the Avalonia app project: `Program`, `App`, `ViewLocator`, `AppHost`,
      service registration, startup options, logging + retention, global exception handlers, shell window with
      regions/splitters/menu/status skeleton, the dialog overlay host, pickers, console host with the
      Application channel, notifications, and the progress surface.
- [ ] **2 — Workspace.** Document model, tab strip, welcome document, Project Overview (read-only) and Project
      JSON documents, project switcher; `INavigationService` with history, target registry, explorer seam, and
      the unknown-domain notice; command catalog, menus, status items, framework-neutral gesture descriptors,
      and the command palette.
- [ ] **3 — Settings, theming, accessibility.** `mocassin.settings` v1 model/validator/migrations/source-generated
      context, the App-layer atomic+fsync+size-capped store, app-data layout, settings dialog, theme/density/
      font-scale/high-contrast/reduce-motion applier, cheat sheet, and accessibility metadata.
- [ ] **4 — Project lifecycle.** `ProjectSessionService`/`ProjectSession` with the `ValidateOnLoad = false`
      store and stored load diagnostics; new via `ProjectPathRules`; open/activate/save/save-as/close/reload;
      recents; locks and read-only mode; external-change detection; multi-project workspace; window close; the
      23A documentation pages.
- [ ] **5 — Edit pipeline.** `ProjectEditCoordinator` with apply/undo/redo, bounded depth, `ProjectChanged`,
      rejected-edit diagnostics, and metadata editing in the Project Overview document.
- [ ] **6 — Validation surface.** `ValidationCoordinator` over the in-memory pipeline entry with the load
      diagnostics, debounce, stale guard, status bar, Problems channel (filters, sort, help anchors, related
      navigation, revalidate), edit diagnostics, and on-demand statistics.
- [ ] **7 — Dirty/autosave/recovery.** Save point and markers, autosave scheduler, sidecar and recovery store,
      recovery prompt, reload/recover/discard/keep operations, session restore, retention, and the bounded
      asynchronous crash-recovery writer.
- [ ] **8 — Tests, documentation, closeout.** Headless harness and test support, view-model and view tests,
      accessibility and screenshot baselines, platform checklist, the remaining docs and navigation, parity/
      status updates, closeout evidence, and the audit request.
- [ ] Add the four library changes in §16 with their tests (including the CLI message-pinning tests) and
      `PublicAPI.Unshipped.txt` entries; verify that no App `PublicAPI.*` file or App ApiCompat baseline changes
      (the module markers and entry points are the only public App types, §15).
- [ ] Update `docs/planning/closeout/step-23.json` (or one file per step if the split is approved); run
      `python tools/check_closeout_evidence.py`.

## Acceptance criteria

- [ ] The projects build on all three target platforms and the headless suite starts the shell on Ubuntu
      automatically. The Windows/macOS headless suite passes in the manual platform gate, and a manual
      real-window smoke checklist is recorded for all three OSes; no claim of a real-window run on a platform
      without that evidence.
- [ ] The app starts with a welcome document; the shell regions, menu bar, and status bar render.
- [ ] A project can be created from both embedded templates, opened, edited (metadata), saved, closed, and
      reopened; reopening yields identical content through the real store.
- [ ] Save-as works, preserves undo history, and migrates the lock; reload replaces the document and clears the
      undo/redo stacks.
- [ ] The composed `ProjectStore` has `ValidateOnLoad = false`; opening a project with load/migration
      diagnostics seeds the pipeline's `Load` stage with them (`Completed`/`Failed`) and a clean load leaves it
      `Skipped`; the load diagnostic is visible in Problems and no diagnostic is duplicated (M-1, m-14).
- [ ] Dirty state is correct across save, undo, and redo, including both crossings of the save point.
- [ ] Autosave writes only dirty documents and deletes its file on a successful save; crash recovery restores a
      dirty document for both saved and untitled projects; the crash path completes within its budget or reports
      the timeout without blocking.
- [ ] Recent files, locks, read-only mode, and external-change detection behave as designed; no silent
      overwrite is possible.
- [ ] Undo/redo works through the single project stack and is bounded; a rejected edit changes nothing.
- [ ] Validation diagnostics appear in the status bar and Problems channel with code, severity, and path;
      double-click navigates through a registered provider and produces a non-blocking notice otherwise.
- [ ] Settings round-trip through the real store, validate, migrate, ignore unknown fields, reject a future
      major without overwriting, and survive a failed/oversized write; the settings dialog performs reset,
      open-folder, and clear-logs/cache safely; last directories persist.
- [ ] Every primary action is reachable by keyboard alone; the palette lists the shell commands with their
      platform gestures; `Model`/`Build`/`Run` menus do not appear.
- [ ] Accessibility names/roles are present on the primary controls; the accessibility-tree assertions pass.
- [ ] No unhandled exception kills the process silently; the crash dialog and log entry are verified.
- [ ] Headless tests pass on Linux in CI; the Windows/macOS platform-gate results are recorded.
- [ ] `./tools/verify.sh all --require` passes, the strict docs build passes, and closeout evidence validates.
- [ ] Documentation written (see §19), and no page claims a later step's capability.

## Documentation

These pages are **created during implementation**, not by this design change; the paths are listed as code so
this page does not link to pages that do not exist yet. Their planned content is in §19.

- `docs/getting-started/installation.md` — installation and first launch (23A).
- `docs/getting-started/quickstart.md` — create/open/inspect/save/close/reopen, recents, locks, reload (23A);
  validation, editing, undo, autosave and recovery sections (23B).
- `docs/getting-started/first-project.md` — guided first project with templates, workspace and settings (23A);
  undo, dirty/autosave and recovery (23B).
- `docs/user-guide/projects.md` — lifecycle, recents, locks, external changes, settings (23A); undo/redo,
  dirty/save point, autosave, recovery, retention (23B).
- `docs/development/ui-architecture.md` — composition, MVVM rules, documents/navigation, state ownership,
  threading, I/O boundary, seams, settings format (23A); edit/validation/recovery/harness chapters (23B).
- `docs/troubleshooting/installation.md` — platform/startup/lock/settings issues (23A); recovery prompts (23B).
- Updates: `docs/reference/file-layout.md` (app-data layout and settings format),
  `docs/development/testing-and-parity.md` (UI harness and tiers), the three `index.md` files, and
  `zensical.toml` navigation. [`docs/development/code-structure.md`](../../development/code-structure.md) gains
  the App layout in this design change.

## Tests

See §20. Summary of the required evidence: real-object lifecycle and recovery tests, save-point/dirty tests,
lock/read-only and external-change tests, settings migration tests, undo/redo tests, validation/staleness tests,
navigation tests, dialog-overlay tests, accessibility assertions, Linux screenshot baselines, and the manual
platform checklist.

## Risks and mitigations

See §22.

## Done when

The Avalonia shell provides a reliable, keyboard-accessible workspace with correct project lifecycle, dirty and
save-point handling, autosave and crash recovery, a single-project undo/redo pipeline, a live validation
surface, versioned settings, structured logging, and a crash boundary — with no editors, build UI, 3D, or run
monitoring — and every acceptance criterion, test tier, documentation page, parity/status update, and closeout
evidence is complete. The complexity class and split decision required by [`04-roadmap.md`](../04-roadmap.md)
§3.1 are recorded in §23 and approved by the owner at the start gate; a split adds the 23A/23B roadmap entries
and separate closeout evidence before either half starts.
