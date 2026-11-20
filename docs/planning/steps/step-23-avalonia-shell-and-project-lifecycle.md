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
- **Design review:** Ready for review — the design freeze is the commit that introduces this page; the review
  request and frozen SHA are recorded in the follow-up record `docs/planning/reviews/step-23.md`
- **Feature list:** Proposed (eight features) — requires the single owner approval described in
  [`AGENTS.md` §3.2](../../../AGENTS.md)
- **Sizing:** 3.75–4.5 person-weeks (base ≈3.75 pw + 0.75 pw contingency). The upper bound exceeds the
  four-week §3.1 trigger, so this design is presented with a **requested single-step exception** and a clean
  23A/23B split line at feature 5. The owner decides at the start gate; see §23.
- **Last updated:** 2026-11-20

| Feature | Branch | Status | Local gates | Step CI | Squash commit |
| --- | --- | --- | --- | --- | --- |
| — | — | — | — | — | — |

### Proposed feature list (requires one owner approval)

Each branch is `feature/step-23-avalonia-shell-and-project-lifecycle/<slug>` from the step branch.

| # | Feature | Slug | Planned scope | Done when |
| --- | --- | --- | --- | --- |
| 1 | Shell host, composition, and logging | `shell-host-and-composition` | Avalonia app project (`Program`, `App.axaml(.cs)`, `ViewLocator`), `IHost` composition root, explicit service registration, `TimeProvider`/`IUiDispatcher` boundaries, Serilog file+console logging with retention, global exception handlers, `--project`/`--log-level`/`--data-dir` startup options, shell window skeleton with the four regions and splitters, welcome document, `Code structure` layout for the App projects | The app starts headless and renders the shell; DI resolves every registered service; a deliberately thrown unhandled exception is logged and reported instead of killing the process silently; `Mocassin.App.ViewModels` has no Avalonia reference (architecture test) |
| 2 | Document model, tabs, navigation, command palette | `documents-navigation-and-commands` | `DocumentViewModel`/`DocumentKey`/`DocumentCollectionViewModel`, tab strip with dirty marker/reorder/close/overflow, `INavigationService` with history, `INavigationTargetProvider` registry, project-scoped document keys, project switcher, project explorer tree, the read-only Project Overview document (editable metadata) and Project JSON document, menu/status/console contribution registries, command catalog and command palette (`Ctrl+Shift+P`) | Open/activate/reuse/close tabs and back/forward work against real project documents; an unknown navigation domain produces a non-blocking console notice, never an exception; the palette lists every registered command with its gesture |
| 3 | Settings, theming, shortcuts, accessibility | `settings-theming-and-accessibility` | `mocassin.settings` v1 (parse/validate/migrate/atomic write, unknown fields ignored, future major rejected), app-data layout, settings dialog (reset, open config folder, clear logs/cache), light/dark/system theme, accent, density, font scale, high-contrast path, reduce-motion, shortcut catalog with platform primary modifier, accessible names/roles/help text on every shell control, Help → shortcut cheat sheet and application diagnostics | Settings round-trip, migration, defaults-on-invalid, and unknown-field tolerance are covered by real-file tests; every shell command is reachable by keyboard; the accessibility tree exposes names/roles for the primary controls |
| 4 | Project session lifecycle | `project-session-lifecycle` | `ProjectSession`/`IProjectSessionService` over `ProjectStore`: new from `ProjectTemplateFactory` with shared `ProjectNameRules`, open, save, save-as, close, recent files, lock acquisition and read-only banner, external-modification detection, multi-project workspace, unsaved-changes prompts, window-close coordination | New → edit → save → close → reopen yields an identical project through the real store; a second instance opens read-only; a modified file on disk is detected before overwrite |
| 5 | Dirty state, autosave, recovery | `dirty-autosave-and-recovery` | Save-point-correct dirty tracking, title markers, autosave scheduler over `TimeProvider` with the `.autosave` sidecar for saved projects and `recovery/` snapshots for untitled documents, recovery prompt on open/startup, unclean-exit session restore, retention (logs 14 days/100 MB, recovery 7 days) | Autosave writes only dirty documents; a simulated crash leaves a recoverable snapshot; recovery restores the exact in-memory project; a clean save deletes the sidecar |
| 6 | Edit pipeline and undo/redo | `edit-pipeline-and-undo-redo` | Single `ProjectEditCoordinator` per project over `ProjectDocument`: `Apply`/`Undo`/`Redo` commands, bounded undo depth, project-changed event, stale-result-guarded background validation, transient edit diagnostics, metadata editing in the Project Overview document | Undo/redo crosses the save point correctly in both directions; a rejected edit leaves the document clean and shows diagnostics; a stale validation result is discarded |
| 7 | Dialogs, consoles, and error boundaries | `dialogs-consoles-and-errors` | In-shell `IDialogService` overlay host (confirm/choose/prompt/error/progress/crash), `IStorageService` over Avalonia `IStorageProvider`, Problems channel bound to validation, Application log channel, notification/progress surface, crash dialog with "open log folder" and "write recovery files" | Dialog and console flows run against the real overlay in headless tests; only the OS file picker is substituted; a crash report contains the project path and stack trace |
| 8 | Test harness, documentation, and closeout | `tests-docs-and-closeout` | Avalonia headless harness, `ManualTimeProvider`/inline dispatcher/recording dialog/stub picker test support, visual baselines in three themes, accessibility-tree assertions, manual platform checklist, the six documentation pages plus reference/dev updates, parity/status/closeout evidence and the audit request | `./tools/verify.sh all --require` passes; the strict docs build passes; closeout evidence validates; the audit request is complete |

The feature list is proposed, not approved. The owner approves it once, together with the sizing decision in
§23; individual features are then squash-merged without further approval. Features 1–4 are a clean 23A
boundary and features 5–8 a clean 23B boundary if the owner prefers to split.

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
  - singleton: `AppPaths`, `ISettingsService`, `ILoggerFactory`, `TimeProvider`, `IUiDispatcher`,
    `IDialogService`, `IStorageService`, `IConsoleService`, `ICommandCatalog`, `IProjectSessionService`,
    `INavigationService`, `ShellViewModel`, the shell window;
  - created per project by `ProjectSessionService` with explicit constructor arguments (not a DI scope):
    `ProjectSession`, `ProjectDocument`, `ProjectEditor`, `ProjectEditCoordinator`, the validation
    coordinator, and the project-scoped view models.
  - This avoids DI child scopes and captive dependencies: there is exactly one place that constructs a
    session, and it passes the store, clock, dispatcher, and settings it needs.
- Dependency direction (enforced by an architecture test, §20):

```text
Mocassin.App        -> App.Views -> App.ViewModels -> Packages / Results / Contracts / Core
        \-> Avalonia, Microsoft.Extensions.*, Serilog
Mocassin.App.ViewModels -> CommunityToolkit.Mvvm only; no Avalonia, no views, no controls
Mocassin.App.Views      -> Avalonia controls; no scientific logic; no direct Packages calls
```

`Mocassin.App.ViewModels` already references `Mocassin.Packages`, `Mocassin.Results`, and (transitively)
`Mocassin.Core`. Step 23 uses `Packages` and `Core` only; the `Results` reference stays in the project graph
as the Step 28 target and is not called from Step 23 code. `Mocassin.Cli` is never referenced: any behaviour
the GUI needs must live in a library.

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
  project. Minimum window size 900×600. The window restores size/state from settings; a monitor that no longer
  exists falls back to the primary screen (verified in feature 1, §24).
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

**Contribution seams** (all in `Mocassin.App.ViewModels`, all registered explicitly in the composition root):

```csharp
public interface IInspectorContributor { object? CreateInspector(object selection); }
public interface IExplorerNodeProvider { IEnumerable<ExplorerNode> CreateNodes(ProjectSession session); }
public interface IMenuItemProvider { IEnumerable<ShellCommand> CreateMenuItems(string menu); }
public interface IConsoleChannelProvider { IEnumerable<ConsoleChannelViewModel> CreateChannels(); }
public interface IStatusBarItemProvider { IEnumerable<StatusBarItemViewModel> CreateItems(); }
```

Step 23 registers the `project` navigation domain, the project-level explorer nodes, the Problems and
Application console channels, the shell menus, and the validation/status items. Every seam returns plain view
models or commands; no seam takes an Avalonia type, so later steps stay headless-testable. The shell validates
at startup that no two providers claim the same navigation domain and that no provider is registered twice.

### 5. Project lifecycle

```csharp
// Mocassin.App.ViewModels/Projects
public interface IProjectSessionService
{
    IReadOnlyList<ProjectSession> Sessions { get; }
    ProjectSession? ActiveSession { get; }
    Task<ProjectSessionResult> CreateAsync(ProjectTemplateKind kind, string name, string path, CancellationToken cancellationToken = default);
    Task<ProjectSessionResult> OpenAsync(string path, CancellationToken cancellationToken = default);
    Task<ProjectSessionResult> SaveAsync(ProjectSession session, CancellationToken cancellationToken = default);
    Task<ProjectSessionResult> SaveAsAsync(ProjectSession session, string path, CancellationToken cancellationToken = default);
    Task<CloseSessionResult> CloseAsync(ProjectSession session, CancellationToken cancellationToken = default);
    Task<RecoveryScanResult> ScanRecoveryAsync(CancellationToken cancellationToken = default);
}
```

- **Services never prompt.** `IProjectSessionService` returns typed results (`Created`, `Opened`,
  `OpenedReadOnly`, `Saved`, `SaveFailed`, `UnsavedChanges`, `Locked`, `Canceled`, with diagnostics); the
  shell layer owns every dialog. This keeps the service headless-testable and prevents a message box from
  appearing inside library-like code. This also replaces the step page's earlier `IProjectStore` service
  interface: the concrete `ProjectStore` is injected as the persistence API; there is no second store
  abstraction.
- **New**: uses `ProjectTemplateFactory.CreateProject(kind, name, utcNow)` — the same factory as
  `mocassin new` — after validating the name and target path with the shared `ProjectNameRules` (§16). Step 23
  ships exactly the kinds `ProjectTemplateKind` defines today (`blank`, `ceria`) with the same display names as
  the CLI, so the two surfaces cannot drift; a later starter kind appears in both automatically because the
  factory is shared. An existing target is refused with the same rule as the CLI's `CLI003` case (never
  replace).
- **Open**: picker / recent list / drag-and-drop onto the window. Drag-and-drop is restricted to `.mocprj`
  files (and directories are ignored), because accepting arbitrary paths would be a new trust surface.
- **Save**: `ProjectStore.SaveAsync(current, path)`; on success call `ProjectDocument.MarkSaved()` (no-arg,
  §7) and store the returned value as `ProjectSession.LastWritten` for the file stamp and the displayed
  timestamp; delete the `.autosave` sidecar and the untitled recovery snapshot; refresh `modifiedUtc` display.
- **Save As**: `ProjectStore.SaveAsync(current, newPath)`; on success migrate the lock (acquire the new path
  before releasing the old one; if the new lock cannot be acquired, keep the old path and report), update the
  session path, recent list, file stamp, and document keys' display data. Undo history is preserved.
- **Close**: if dirty, the shell prompts Save/Discard/Cancel; then dispose the lock, delete an orphaned
  sidecar only if it is ours and the document was saved, and close the project's documents.
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
  Recover / Discard / Decide later. Recovering loads the sidecar, builds a `ProjectDocument` with the saved
  file value as the clean point (`ProjectDocument.CreateRecovered`, §16), and leaves the document dirty; the
  next save writes the main file and deletes the sidecar. Discarding deletes it. "Decide later" keeps it and
  reappears next time. Untitled recovery files are listed in the same startup flow and open as dirty untitled
  documents that prompt for a path on first save.
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
- **Crash**: the crash dialog offers "Write recovery files" (synchronous sidecar/recovery writes for all dirty
  sessions, best-effort with per-session reporting), "Open log folder", and "Exit". No automatic restart.

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
  sample must stay well below the 500 ms open/save budget; the actual figure is recorded in feature 6.

### 8. Validation and diagnostics surface

- The shell consumes `ProjectValidationPipeline` exactly as Step 07 §8 requires: `Validate(project)` with
  `ValidationOptions.Default` (no statistics, no build stage) and a `CancellationToken`, debounced 300 ms
  after the last `ProjectChanged`. It does not call domain validators and does not let `ProjectStore` validate
  on load.
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
  background, cached by document hash, because `/stats` is budgeted at < 2 s for ceria while live validation is
  < 200 ms.
- Diagnostics are never invented in the UI: no new codes, no re-worded messages. Help links map `Code` to the
  documented anchor when one exists; an unknown code renders unlinked.
- Step 07 §8 assigns "the panels and commands" to Steps 24–25. Step 23 builds the status indicator, the
  Problems list, the navigation plumbing, and the revalidate command because the shell needs them for its own
  acceptance, and Steps 24/25 add the editor side of each navigation target plus the statistics panels. This is
  the same contract delivered earlier in part, not a change to it.
- One settings file per data directory: a second application instance sharing the same data directory is not
  coordinated, so the last writer wins for settings. This is accepted for a single-user desktop app and is
  documented; project files are protected by their own lock, which is the part that matters.

### 9. Settings, theming, density, and accessibility

- Settings live in `<app-data>/Mocassin.Settings.json`; the app-data root is
  `<LocalApplicationData>/Mocassin` (`%LOCALAPPDATA%`, `~/.local/share`, `~/Library/Application Support`),
  overridable by `--data-dir`/`MOCASSIN_DATA_DIR`. Subfolders: `logs/`, `cache/` (reserved for Step 27),
  `recovery/`. Paths are built with `Path.Combine`/`Environment.SpecialFolder`; no separators are hard-coded.
  The format is specified in §17.
- The settings service validates on load: an unknown enum, out-of-range number, or malformed entry falls back
  to the default for that field and logs a warning; a document whose `format` is wrong or whose
  `schemaVersion` is a *future major* is not loaded and **not overwritten** (the app starts with defaults,
  shows a banner, and Save Settings becomes "Save as new file"). Unknown fields are tolerated on read and are
  not preserved when the file is rewritten, so a newer minor writer cannot corrupt an older reader; older
  documents are migrated by pure functions.
- Theme: `system` (follows `Application.RequestedThemeVariant` default), `light`, `dark`. Accent overrides the
  Fluent accent resources; a null accent uses the Fluent default. Density: `comfortable` (default) or
  `compact`, implemented as an app resource dictionary that overrides spacing/size tokens; font scale
  0.85–1.5 multiplies the base font sizes (text stays scalable, no fixed pixel heights). Reduce motion
  disables non-essential transitions.
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
  density, font scale (slider with live preview), high contrast, reduce motion, autosave enable/interval,
  confirm-on-external-change, recent-file list with clear, and buttons Reset to defaults / Open config folder /
  Clear logs and cache / Close. Reset asks for confirmation and never deletes projects.
- "Clear logs and cache" deletes only files under `logs/` and `cache/` that are not the currently open log
  file, and reports how many bytes were freed. No project, package, result, or job-folder path is touched.

### 10. Keyboard, commands, menus, and the command palette

- `ShellCommand` (ViewModels/Commands) = `{ Id, Label, Gesture, Category, CanExecute, Execute }`. The
  `ICommandCatalog` aggregates commands registered by the shell and by the active document. Commands are
  `IAsyncRelayCommand`s with cancellation; `CanExecute` is the single source of truth for menu, palette,
  toolbar, and key binding state, so a disabled command is visibly disabled everywhere.
- Key gestures: `primary` means `Ctrl` on Windows/Linux and `Meta` (`Cmd`) on macOS, resolved once from
  `OperatingSystem`. The menu renders the platform gesture text; the palette shows it in a column.
- Shortcuts (Step 23 set):

| Action | Shortcut |
| --- | --- |
| New / Open / Save / Save As / Close document | `Ctrl+N` / `Ctrl+O` / `Ctrl+S` / `Ctrl+Shift+S` / `Ctrl+W` |
| Undo / Redo | `Ctrl+Z` / `Ctrl+Y` and `Ctrl+Shift+Z` |
| Command palette | `Ctrl+Shift+P` |
| Find in document | `Ctrl+F` |
| Next / previous document, document 1–9 | `Ctrl+Tab` / `Ctrl+Shift+Tab`, `Ctrl+1..9` |
| Back / forward navigation | `Alt+Left` / `Alt+Right` |
| Settings / shortcut cheat sheet | `Ctrl+,` / `F1` |
| Toggle left / centre / right region, console | `Alt+1` / `Alt+2` / `Alt+3`, `Alt+0` |
| Validate now | `Ctrl+Shift+V` (Step 26 adds `F5` build and `F6` run) |
| Quit | `primary+Q` (macOS) / `Alt+F4` (Windows/Linux, window manager) |

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
  `IStorageProvider` (`TopLevel.StorageProvider`), returns plain strings, remembers the last directory per
  operation in settings, and never leaks Avalonia types into view models.
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
  builds). Structured properties
  include project id, document key, operation, and duration where applicable. No credentials, no remote sinks,
  no telemetry.
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
| Panel sizes, visibility, theme, density, recent files | `ShellViewModel`/settings region VMs | `Mocassin.Settings.json` |
| Per-project view state (camera, visibility, styles) | later steps | project `viewSettings` |

Rules:

- No scientific logic in any view model: no arithmetic on energies/geometry, no unit conversion, no
  validation rules, no JSON parsing, no hashing beyond calling the coordinator. View models translate between
  library values and observable properties.
- UI state that must persist goes to app settings (app-scoped) or the project `viewSettings`
  (document-scoped). No ad-hoc state files. Step 23 writes nothing to `viewSettings`.
- All `ProjectDocument` mutations happen on the UI thread. Validation runs on the thread pool over an
  immutable snapshot; the result is applied on the UI thread only if it is not stale.
- `IUiDispatcher` (ViewModels) abstracts `Dispatcher.UIThread` for the few callbacks that arrive off-thread
  (timers, file checks, background validation completion). Tests use an inline implementation; that is a
  UI-thread boundary fake, which the mocks policy allows.
- `TimeProvider` is injected everywhere time is read or scheduled (`ProjectStore`, autosave, stamps,
  logging timestamps). No `DateTime.Now`, no `Task.Delay` in production code.
- Long operations take a `CancellationToken`, report `IProgress<T>`, and never block the UI thread. No
  sync-over-async, no `Dispatcher.UIThread.Invoke` from a command body.
- One writer per project: the coordinator. One owner per open project: the session. One collection owns the
  tab set.

### 14. View-model and view contracts

- `Mocassin.App.ViewModels` is UI-framework-free (`ObservableObject`, `IAsyncRelayCommand`,
  `ObservableValidator`) and holds every view model, dialog contract, navigation contract, session service,
  and settings model. It references `CommunityToolkit.Mvvm` only.
- `Mocassin.App.Views` holds `UserControl`s/`Window`s, the `DialogHost`, the shell window, the region views,
  the console/problems views, the settings dialog view, and the resource dictionaries. Views bind to view
  models; a view never calls a library or contains scientific logic. Code-behind is limited to view-only
  concerns (focus management, drag-reorder plumbing, splitter keyboard handling).
- `Mocassin.App` holds `Program`, `App.axaml(.cs)`, `ViewLocator`, and the composition root. `ViewLocator`
  maps a view-model type to `Mocassin.App.Views.<same path minus "ViewModels">.<TypeName minus "ViewModel">View`
  by convention, with three safeguards: the type must live in `Mocassin.App.ViewModels` (or a later
  `*.ViewModels` assembly), the resolved type must be a control, and a missing view logs an error and renders
  an explicit "view not found" panel in Release (a plain text block in Debug). A silent blank control is a bug.
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
  Startup/         StartupCoordinator.cs  RecoveryScanPresenter.cs  CrashHandler.cs

src/dotnet/Mocassin.App.ViewModels/
  Shell/           ShellViewModel.cs  ShellRegionsViewModel.cs  StatusBarViewModel.cs  ShellWindowState.cs
  Documents/       DocumentViewModel.cs  DocumentKey.cs  DocumentCollectionViewModel.cs  WelcomeDocumentViewModel.cs
  Documents/Project/ ProjectOverviewViewModel.cs  ProjectJsonViewModel.cs  ProjectMetadataEditViewModel.cs
  Projects/        ProjectSession.cs  ProjectSessionService.cs  IProjectSessionService.cs  ProjectSessionResult.cs
                   CloseSessionResult.cs  ProjectFileStamp.cs  ExternalChangeDetector.cs  RecoveryService.cs
                   RecentFilesViewModel.cs
  Scheduling/      AutosaveScheduler.cs  Debouncer.cs
  Editing/         ProjectEditCoordinator.cs  EditDiagnosticSet.cs  ProjectChangedEventArgs.cs
  Diagnostics/     ValidationCoordinator.cs  ProblemsViewModel.cs  DiagnosticItemViewModel.cs  ValidationState.cs
  Navigation/      INavigationService.cs  NavigationService.cs  INavigationTargetProvider.cs  NavigationIntent.cs
                   NavigationHistory.cs  ProjectNavigationTargetProvider.cs  IExplorerNodeProvider.cs
  Settings/        AppSettings.cs  ISettingsService.cs  SettingsService.cs  SettingsMigration.cs  AppSettingsValidator.cs
                   SettingsViewModel.cs  ThemeService.cs  LayoutState.cs  SessionState.cs
  Commands/        ShellCommand.cs  ICommandCatalog.cs  CommandCatalog.cs  KeyGestureCatalog.cs  IMenuItemProvider.cs
                   IConsoleChannelProvider.cs  IStatusBarItemProvider.cs  CommandPaletteViewModel.cs
  Consoles/        ConsoleViewModel.cs  ConsoleChannelViewModel.cs  ConsoleEntryViewModel.cs  IConsoleService.cs
  Dialogs/         IDialogService.cs  IStorageService.cs  IProgressDialog.cs  DialogViewModel.cs  DialogRequest.cs
                   ConfirmDialogRequest.cs  ChoiceDialogRequest.cs  TextPromptRequest.cs  ErrorDialogRequest.cs
                   ProgressDialogRequest.cs  DialogChoice.cs

src/dotnet/Mocassin.App.Views/
  Shell/           ShellWindow.axaml(.cs)  ShellView.axaml(.cs)  MenuBarView  StatusBarView  ProjectSwitcherView
  Documents/       DocumentTabStripView  DocumentHostView  WelcomeView  ProjectOverviewView  ProjectJsonView
  Navigation/      ProjectExplorerView
  Consoles/        ConsoleView  ProblemsView
  Settings/        SettingsView
  Dialogs/         DialogHost  ConfirmDialogView  ChoiceDialogView  PromptDialogView  ErrorDialogView
                   ProgressDialogView  CrashDialogView  CommandPaletteView
  Controls/        SplitterHost  NotificationHost
  Themes/          Density.axaml  HighContrast.axaml  Accent.axaml  Metrics.axaml

src/dotnet/Mocassin.App.Tests/
  TestSupport/     ManualTimeProvider.cs  InlineUiDispatcher.cs  RecordingDialogService.cs  StubStorageService.cs
                   TempWorkspace.cs  HeadlessAppBuilder.cs  ProjectFixtures.cs
  ViewModels/…  Views/…  Visual/…  Visual/Baselines/…
```

No `Internal/` folder is added at a project root (`Internal` is nested-only per the structure policy); the
debouncer and autosave scheduler live in `Scheduling/`, and any other helper lives in the concept folder that
owns it.

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
baseline refresh is expected; if `./tools/verify.sh api` reports a removed or changed member, the change is
wrong and must be redesigned rather than baselined away.

| Change | Where | Why the shell cannot own it | Contract effect |
| --- | --- | --- | --- |
| `ProjectDocument.MaxUndoDepth` (init-only, default 200; oldest entries dropped) | `Mocassin.Core.Editing` | The stack lives in the document; a UI-level bound would duplicate or shadow the stack | Additive property; no signature change; default preserves Step 01 behaviour |
| `ProjectDocument.CreateRecovered(Project current, Project lastSaved, ProjectEditor? editor = null)` | `Mocassin.Core.Editing` | Recovery needs a clean point different from the current value; `MarkSaved(value)` overwrites both | Additive static factory; existing constructors unchanged |
| `ProjectStore.WriteAutosaveToPathAsync(Project, string autosavePath, CancellationToken)` | `Mocassin.Packages.Projects` | Untitled projects have no project path to derive `<path>.autosave` from; re-implementing the atomic JSON write in the app would duplicate Step 01 | Additive overload; reuses `AutosaveFailed`; existing sidecar API unchanged |
| `ProjectNameRules.Normalize/Validate` + `NewProjectCommand` refactor to call it | `Mocassin.Packages.Projects.Templates` and `Mocassin.Cli.NewProject` | `File > New` must enforce exactly the CLI's name/path rules; duplicating them in the app guarantees drift | New public helper; CLI output and `CLI003`/3 behaviour must remain byte-identical (existing CLI tests pin the messages) |

No other library change is allowed in this step. If implementing a feature appears to need a fifth change, the
step agent stops and records an amendment rather than editing a library ad hoc.

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
  "autosave": { "enabled": true, "intervalMinutes": 5 },
  "logging": { "level": "information" },
  "ui": { "confirmOnExternalChange": true, "showWelcomeOnStart": true },
  "recentFiles": [ { "path": "/work/ceria.mocprj", "lastOpenedUtc": "2026-11-20T10:00:00Z" } ],
  "session": { "openProjects": ["/work/ceria.mocprj"], "activeProject": "/work/ceria.mocprj",
               "cleanExit": true, "savedUtc": "2026-11-20T10:00:00Z" },
  "layout": { "leftWidth": 260, "rightWidth": 320, "consoleHeight": 180,
              "leftVisible": true, "rightVisible": true, "consoleVisible": true,
              "activeConsoleChannel": "problems" }
}
```

- Written atomically (temp + rename), UTF-8 without BOM, 2-space indent, `\n` line endings.
- Reading: `format` must equal `mocassin.settings`; `schemaVersion` above the current major is rejected without
  overwriting; unknown properties are ignored; each field is validated and falls back to its default with a
  logged warning.
- Migrations are pure functions (`SettingsMigration`) keyed by `schemaVersion`. Version 1 is the first.
- **Step 23 ships only fields it consumes.** Later steps add fields additively (missing = default) without a
  schema bump; a bump is required only for a breaking change (renamed/removed field or changed meaning).
  Solver path, package output, and results directories are deliberately absent until Steps 26/28 consume them.
- The window size/state and splitter positions are app-scoped, so they live here, not in `viewSettings`.
  Nothing in this step writes `viewSettings`.

### 18. Dependency and interface checks (parallel track)

**Consumed library surfaces (all merged, no changes requested):**

| Capability | Library API | What the shell owns |
| --- | --- | --- |
| Load/save/migrate/lock/autosave | `ProjectStore`, `ProjectStoreOptions`, `ProjectLoadResult`, `ProjectSaveResult`, `ProjectLockResult`, `ProjectFileLock` | prompts, paths, recent list, file stamps, read-only mode, scheduling |
| Project value and editing | `Project`, `ProjectDocument`, `ProjectEditor`, `ProjectChange`, `ProjectEditResult`, `ProjectHasher`, `ProjectJson` | commands, dirty presentation, undo/redo wiring |
| Templates | `ProjectTemplateFactory`, `ProjectTemplate`, `ProjectTemplateKind` | the New dialog, name/path validation via `ProjectNameRules`, recent/list wiring |
| Validation | `ProjectValidationPipeline`, `ValidationResult`, `ValidationOptions`, `DiagnosticSummary`, `ModelStatisticsService` | debounce, staleness guard, Problems surface, navigation |
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

| Page | Content | Written in |
| --- | --- | --- |
| `docs/getting-started/installation.md` | prerequisites, per-OS install, first launch, data-dir location, GPU-free operation | feature 1/8 |
| `docs/getting-started/quickstart.md` | create/open → inspect → validate → save/recover; what the editors add later | feature 4/8 |
| `docs/getting-started/first-project.md` | guided first project: templates, workspace, tabs, undo, autosave, recovery | feature 5/8 |
| `docs/user-guide/projects.md` | lifecycle reference: new/open/save/save-as/close, recents, locks, read-only, external changes, undo/redo, settings, recovery, retention | features 4–6 |
| `docs/development/ui-architecture.md` | composition root, dependency direction, document/navigation model, state ownership, threading, contribution seams, test harness, theming/a11y, settings format | features 1–3, finalized in 8 |
| `docs/troubleshooting/installation.md` | startup failures, missing display/Wayland nuance, unwritable data dir, corrupt settings, locked project, recovery prompts | feature 8 |
| `docs/reference/file-layout.md` | app-data layout, `Mocassin.Settings.json` field table and example, retention rules | feature 3/8 |
| `docs/development/index.md`, `docs/user-guide/index.md`, `docs/troubleshooting/index.md`, `zensical.toml` | navigation entries for the new pages (required for the strict build) | feature 8 |
| `docs/development/testing-and-parity.md` | UI harness, tiers, screenshot policy | feature 8 |

No page may describe an editor, build UI, 3D, or run monitoring as available. Screenshots in the user guide
must be regenerated from the real app; a placeholder is not acceptable.

### 20. Test plan

Harness: `Mocassin.App.Tests` uses Avalonia headless for view tests and plain xUnit for view-model tests.
Real objects everywhere: real `ProjectStore`, real temporary directories, real `Project` values, real
validation pipeline, real dialog overlay. Permitted fakes are exactly the true boundaries: `IDialogService`
(for view-model-level tests only), `IStorageService`, `IUiDispatcher`, and `TimeProvider`. No fake `Project`,
no fake validator, no fake store, no fake JSON.

| Area | Tier | Assertion |
| --- | --- | --- |
| Startup/composition | PR-fast | `AddMocassinApp` resolves every registered service; the shell view model constructs; the shell renders in headless without a display; duplicate navigation-provider registration fails fast |
| Architecture | PR-fast | `Mocassin.App.ViewModels` has no reference to Avalonia/`Mocassin.App.Views`; no view type is referenced from a view model; `Mocassin.App` does not reference `Mocassin.Cli`/`Mocassin.Compilation` |
| New/open/save/close | PR-fast | New from both templates creates a file the store reopens; save → close → open is content-identical; save-as migrates the lock and preserves undo history; an existing target is refused |
| Dirty/save point | PR-fast | `*` appears and clears; save → undo → redo is clean; save → edit → undo is clean; undo below the save point is dirty |
| Autosave | PR-fast | Only dirty documents are written; the interval comes from `TimeProvider`; save and autosave never interleave; a clean save deletes the sidecar/recovery file |
| Recovery | PR-fast | A simulated crash leaves a sidecar; startup offers recovery; Recover produces a dirty document whose content equals the snapshot; Discard deletes; retention removes only old recovery files |
| Untitled recovery | PR-fast | An untitled dirty project writes `<app-data>/recovery/<projectId>.mocprj.autosave` and is offered at next start |
| Locks/read-only | PR-fast | A second session opens read-only with a banner; edit/save are disabled; save-as acquires the new lock; a stale lock is reclaimed per store policy |
| External change | PR-fast | Clean + changed file → prompt and reload; dirty + changed file → no silent overwrite; unchanged file → no prompt |
| Recent files | PR-fast | Order, de-duplication, missing-file disable/remove, persistence across restart |
| Settings | PR-fast | Round-trip; defaults on invalid field; unknown field ignored; future major rejected and file not overwritten; migration is idempotent |
| Undo/redo | PR-fast | Multi-step apply/undo/redo through the Project Overview document on the shared project stack (the cross-editor proof is Step 24); bounded depth drops the oldest entry; redo is cleared by a new edit |
| Rejected edit | PR-fast | Validation failure leaves the project and dirty state unchanged and surfaces diagnostics |
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

### 21. Performance budgets

| Scenario | Budget | Evidence |
| --- | --- | --- |
| Cold start (empty session, warm file cache) | < 2 s | manual release measurement recorded with OS/CPU; not a CI gate |
| Open the ceria sample | < 500 ms | PR-fast test with a stopwatch over the real store, asserted on the same runner |
| Save the ceria sample | < 500 ms | same |
| Live validation (ceria, no statistics) | < 200 ms (Step 07 budget) | PR-fast test |
| Statistics (ceria) | < 2 s (Step 07 budget) | background only, PR-medium |
| Shell render (headless) | < 100 ms | PR-medium smoke assertion, generous threshold |
| Autosave write (ceria) | < 500 ms, non-blocking | PR-fast test with `ManualTimeProvider` |

No benchmark harness is added; `Mocassin.Benchmarks` may later include save/load. Numbers are recorded in the
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

### 23. Sizing and split-trigger check

- **Estimate:** 3.75–4.5 person-weeks. Base ≈3.75 pw (feature 1 ≈0.5, feature 2 ≈0.55, feature 3 ≈0.55,
  feature 4 ≈0.55, feature 5 ≈0.4, feature 6 ≈0.4, feature 7 ≈0.35, feature 8 ≈0.45, integration ≈0.1) plus
  0.75 pw contingency for the Avalonia/platform unknowns.
- **§3.1 triggers:** eight features is at the top of the "6–8" range; the audit is a bounded shell review and
  can be read in one sitting. The upper estimate (**4.5 pw**) is **beyond the four-week trigger**, so this
  step does not fit the default size.
- **Requested decision (owner, start gate):** either
  1. approve a **single-step exception** at 3.75–4.5 pw with eight features and no split, following the
     Step 09/12/13 precedent and recording the exception in [`04-roadmap.md`](../04-roadmap.md) §3.1 and
     [`05-open-decisions.md`](../05-open-decisions.md) §3; or
  2. split into **23A** (features 1–4: shell, documents/navigation, settings/theming/a11y, project lifecycle,
     ≈2.15 pw) and **23B** (features 5–8: dirty/autosave/recovery, edit pipeline, dialogs/consoles, tests/docs/
     closeout, ≈1.6 pw + contingency). A split adds one roadmap step and makes Step 24 depend on 23B; the
     Phase F plan and the Phase F cross-cutting checkpoint would be updated accordingly.
- **Recommendation:** the single-step exception. The eight features form one coherent shell + lifecycle
  contract; a split would leave 23A with an unusable app (no autosave/recovery/dialogs) and duplicate test and
  closeout effort. This mirrors the rejected splits in Steps 12 and 13.
- **Guard:** if features 1–4 exceed **2.2 pw**, the step agent stops and reports to the orchestrator before
  feature 5.
- **No owner approval is recorded yet.** The design is complete under either choice; features 1–4 are exactly
  23A. The orchestrator must obtain the sizing decision and the feature-list approval at the start gate
  before any branch is created (AGENTS §3.2, §3.9 step 7).

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

**Verification items** (must be confirmed by the named feature; not guessed here):

| ID | Question | Feature | Fallback |
| --- | --- | --- | --- |
| V-1 | Exact Avalonia 12.1.6 `AppBuilder`/headless setup, and whether `UseSkia()` is available through the `Mocassin.App` reference for screenshot rendering | 1/8 | Add an explicit `Avalonia.Skia` reference at the pinned version and record it as a dependency delta |
| V-2 | Whether Avalonia 12.1.6 exposes a platform high-contrast theme variant/resource dictionary | 3 | Ship the app-owned `HighContrast.axaml` and select it from settings only |
| V-3 | Whether the pinned `Serilog.Sinks.File` supports `retainedFileSizeLimitBytes` | 1 | Implement `LogRetention` as an explicit startup sweep |
| V-4 | `IStorageProvider` picker behaviour for "save as" on each OS (extension filter, overwrite confirmation) | 4 | Own filtering and confirmation in `IStorageService` and document the per-OS behaviour |

**New ADRs:** none. ADR 0003 (Avalonia/MVVM/DI) and ADR 0006 (3D, untouched) stand; ADR 0008 and 0011/0013 are
not affected. The shell decisions are step-level and are recorded here, in the architecture, the quality
strategy, the open-decisions page, and the code-structure page.

**Owner input required at the start gate:** the sizing decision (§23), the eight-feature list, and the
acknowledgement of the two test-only Avalonia headless packages and the four additive library changes in §16.
No public *format*, manifest, or CLI contract changes.

## Implementation tasks

- [ ] Create the Avalonia app project shell: `Program`, `App`, `ViewLocator`, `AppHost`, service registration,
      startup options, logging, retention, and global exception handlers.
- [ ] Implement the shell layout, regions, splitters, panel visibility, layout persistence, menu bar, and status
      bar.
- [ ] Implement the document model, tab strip, welcome document, Project Overview and Project JSON documents,
      and the project switcher.
- [ ] Implement `INavigationService`, history, the navigation-target registry, the explorer node seam, and the
      unknown-domain notice.
- [ ] Implement the command catalog, menus, shortcut catalog with the platform primary modifier, and the command
      palette.
- [ ] Implement `ProjectSessionService` and `ProjectSession`: new/open/save/save-as/close, recents, locks,
      read-only mode, external-change detection, multi-project workspace, and window-close coordination.
- [ ] Implement dirty state, save-point handling, the autosave scheduler, recovery, unclean-exit session
      restore, and retention.
- [ ] Implement `ProjectEditCoordinator` with apply/undo/redo, bounded depth, `ProjectChanged`, and stale-guard
      validation; implement metadata editing in the Project Overview document.
- [ ] Implement the validation surface: status bar, Problems channel, diagnostic navigation, statistics on
      demand, and edit diagnostics.
- [ ] Implement `mocassin.settings` v1 with validation, migration, atomic write, the settings dialog, app-data
      layout, theme/density/font-scale/high-contrast/reduce-motion, and clear-cache/logs.
- [ ] Implement `IDialogService` and the overlay dialog host, `IStorageService`, progress, notifications, the
      console channels, and the crash dialog.
- [ ] Implement accessibility metadata and the keyboard pass across shell controls; add the cheat sheet and
      application diagnostics.
- [ ] Add the four library changes in §16 with their tests and `PublicAPI.Unshipped.txt` entries.
- [ ] Add the headless test harness, view-model tests, view tests, accessibility assertions, screenshot
      baselines, and the manual platform checklist.
- [ ] Write the six documentation pages plus the reference/dev/nav updates; update the parity matrix, status,
      step page, and `docs/planning/closeout/step-23.json`; run
      `python tools/check_closeout_evidence.py`; request the audit.

## Acceptance criteria

- [ ] The projects build on all three target platforms and the headless suite starts the shell on Ubuntu
      automatically. The Windows/macOS headless suite passes in the manual platform gate, and a manual
      real-window smoke checklist is recorded for all three OSes; no claim of a real-window run on a platform
      without that evidence.
- [ ] The app starts with a welcome document; the shell regions, menu bar, and status bar render.
- [ ] A project can be created from both embedded templates, opened, edited (metadata), saved, closed, and
      reopened; reopening yields identical content through the real store.
- [ ] Save-as works, preserves undo history, and migrates the lock.
- [ ] Dirty state is correct across save, undo, and redo, including both crossings of the save point.
- [ ] Autosave writes only dirty documents and deletes its file on a successful save; crash recovery restores a
      dirty document for both saved and untitled projects.
- [ ] Recent files, locks, read-only mode, and external-change detection behave as designed; no silent
      overwrite is possible.
- [ ] Undo/redo works through the single project stack and is bounded; a rejected edit changes nothing.
- [ ] Validation diagnostics appear in the status bar and Problems channel with code, severity, and path;
      double-click navigates through a registered provider and produces a non-blocking notice otherwise.
- [ ] Settings round-trip, validate, migrate, ignore unknown fields, and reject a future major without
      overwriting; the settings dialog performs reset, open-folder, and clear-logs/cache safely.
- [ ] Every primary action is reachable by keyboard alone; the palette lists the shell commands with gestures;
      `Model`/`Build`/`Run` menus do not appear.
- [ ] Accessibility names/roles are present on the primary controls; the accessibility-tree assertions pass.
- [ ] No unhandled exception kills the process silently; the crash dialog and log entry are verified.
- [ ] Headless tests pass on Linux in CI; the Windows/macOS platform-gate results are recorded.
- [ ] `./tools/verify.sh all --require` passes, the strict docs build passes, and closeout evidence validates.
- [ ] Documentation written (see §19), and no page claims a later step's capability.

## Documentation

These pages are **created during implementation**, not by this design change; the paths are listed as code so
this page does not link to pages that do not exist yet. Their planned content is in §19.

- `docs/getting-started/installation.md` — installation and first launch.
- `docs/getting-started/quickstart.md` — create/open, inspect, validate, save, recover.
- `docs/getting-started/first-project.md` — guided first project.
- `docs/user-guide/projects.md` — lifecycle, workspace, undo/redo, settings, recovery, retention.
- `docs/development/ui-architecture.md` — composition, MVVM rules, documents/navigation, state ownership,
  threading, seams, test harness, theming/accessibility, settings.
- `docs/troubleshooting/installation.md` — platform/startup issues.
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
evidence is complete.
