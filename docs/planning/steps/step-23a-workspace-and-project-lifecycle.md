# Step 23A — Workspace and Project Lifecycle

**Phase:** F. GUI
**Depends on:** 01, 03, 04, 05, 06, 07, 12 (all `Done`, merged, and tagged). Not dependent on 02/08–11 and
**not** on Steps 13–22.
**Delivers:** The Avalonia application host and composition root, the shell layout, the document and navigation
model, the read-only project documents, project create/open/save/save-as/close/reload with recent files, locks,
read-only mode and external-change detection, versioned application settings with theming, density and
accessibility, the dialog/picker/console/notification/progress surfaces, structured logging with a crash
boundary, and the headless test harness. No editing, undo/redo, validation surface, autosave, or recovery —
those are Step 23B.

> **Split note.** The human owner split Step 23 into **23A** and **23B** on 2026-11-20 because the unsplit step
> was complexity class High with three independent acceptance stories and an audit surface too large for one
> sitting. 23A is the workspace and lifecycle half; 23B is the editing, diagnostics, and recovery half. The
> boundary is dependency-clean: 23B builds on 23A and 23A contains no 23B task. The superseded unsplit design
> and its review record are [`step-23-avalonia-shell-and-project-lifecycle.md`](step-23-avalonia-shell-and-project-lifecycle.md)
> and [`reviews/step-23.md`](../reviews/step-23.md).

## Implementation status

- **Status:** In progress
- **Step branch:** `step/23a-workspace-and-project-lifecycle`
- **Step CI:** — (no CI runs until closeout)
- **Main merge:** —
- **Step tag:** —
- **Audit:** Not requested
- **Design review:** Ready with notes — **accepted by the human owner 2026-11-20** at `b9c56fe`
  ([record](../reviews/step-23a.md), round 3 record commit `b6a472c`). Notes n-12/n-13 are corrected in
  feature 5.
- **Feature list:** Approved by the human owner 2026-11-20 (five features; the list below is the approved
  scope)
- **Complexity class:** **High** — cross-step interfaces and test-matrix breadth; the class and one-paragraph
  justification are in §18. The human owner accepted the class at the start gate on 2026-11-20; a further
  split is not proposed and §18 states why.
- **Last updated:** 2026-11-20

| Feature | Branch | Status | Local gates | Step CI | Squash commit |
| --- | --- | --- | --- | --- | --- |
| — | — | — | — | — | — |

### Approved feature list (owner-approved 2026-11-20)

Each branch is `feature/step-23a-workspace-and-project-lifecycle/<slug>` from the step branch. Every feature
depends only on the features above it and on the merged 01/03–07/12 libraries; 23A has no task from 23B.

| # | Feature | Slug | Planned scope | Done when |
| --- | --- | --- | --- | --- |
| 1 | Host, composition, boundaries, and surfaces | `app-host-and-boundaries` | Avalonia app project (`Program`, `App.axaml(.cs)`, `ViewLocator`), `IHost` composition root, explicit registration, `TimeProvider`/`IUiDispatcher`, Serilog file+console logging with retention, `--project`/`--log-level`/`--data-dir` startup options, shell window with the four regions and splitters, menu/status skeleton, the in-shell `IDialogService` overlay host (confirm/choose/prompt/error/crash view), `IStorageService` pickers with an in-memory/default last-directory seam (feature 3 wires persistence), console host with the Application channel at the default log level (feature 3 wires the settings level), notification host, progress surface, global exception handlers (log + crash dialog with "open log folder"/"exit"; the recovery-writing action is added by 23B), the internal-visibility rule with the documented public exceptions | The app starts and renders the shell headless; DI resolves every registered service; a dialog round-trips through the real overlay; a forced unhandled exception is logged and reported; `Mocassin.App.ViewModels` has no Avalonia/`System.IO` use per the exact token test; `App` is the only new public App type (`Program` is pre-existing) and neither is ApiCompat-checked |
| 2 | Workspace: documents, navigation, commands | `workspace-documents-and-navigation` | `DocumentViewModel`/`DocumentKey`/`DocumentCollectionViewModel`, tab strip (title, tooltip, reorder, close, overflow; the dirty marker is added by 23B), `INavigationService` with bounded history, `INavigationTargetProvider` registry, project-scoped keys, project switcher, project explorer, welcome document, read-only Project Overview and Project JSON documents, command catalog, menus, status items, command palette, framework-neutral gesture descriptors, shortcut cheat sheet | Tabs open/activate/reuse/close and back/forward work; an unknown navigation domain produces a console notice, never an exception; the palette lists every registered command with its platform gesture; every primary action is reachable by keyboard |
| 3 | Settings, theming, accessibility | `settings-theming-and-accessibility` | `mocassin.settings` v1 model, validator, migrations, source-generated JSON context, and the atomic+fsync+size-capped store (store in `Mocassin.App`, model/pure logic in ViewModels), app-data layout, settings dialog (reset, open config folder, clear logs/cache), theme/density/font-scale/high-contrast/reduce-motion applier in Views, persisted last directories and log level, accessibility metadata | Settings round-trip, defaults-on-invalid, unknown-field tolerance, future-major rejection, migration, and failed/oversized-write safety pass on real files; the theme/density/contrast switch applies without restart; primary controls expose names/roles |
| 4 | Project session lifecycle | `project-session-lifecycle` | `ProjectSession`/`IProjectSessionService` over `ProjectStore` **configured with `ValidateOnLoad = false`** and retaining `ProjectLoadResult.Diagnostics` on the session; new via `ProjectTemplateFactory` + shared `ProjectPathRules`; open/open-or-activate/save/save-as/close/reload; recents; locks and read-only mode; external-change detection; multi-project workspace; window close; a read-only Project Overview with project identity and file metadata (editing is 23B) | New → save → close → reopen is content-identical; save-as migrates the lock; a second session opens read-only; an external change or a deleted file is detected before any overwrite; the store never validates and the load diagnostics are retained for 23B |
| 5 | 23A tests, documentation, and closeout | `tests-docs-and-closeout` | Avalonia headless harness, `ManualTimeProvider`/inline dispatcher/recording dialog/stub picker/real App-layer settings-store test support, visual baselines in three themes, accessibility-tree assertions, platform checklist, the 23A documentation column (§14), parity/status updates, `docs/planning/closeout/step-23a.json` (`"step": "step-23a"`), and the audit request | `./tools/verify.sh all --require` passes; the strict docs build passes; `check_closeout_evidence.py` validates once the `step-NN[a-z]` tooling prerequisite has landed; the audit request is complete |

The owner approves this list once at the start gate; individual features are then squash-merged without further
approval. 23B is not part of this list and must not appear in a 23A branch.

## Goal

Build the application frame that every later GUI step plugs into, and make project persistence trustworthy
before any editor exists. A user must be able to create, open, inspect, save, save-as, close, and reload
projects; see whether the file changed underneath them; and reach every primary action from the keyboard. The
shell must feel like a modern scientific IDE without containing scientific logic: it consumes the same public
library APIs as the CLI.

23A deliberately ends before editing. There is no metadata editor, no undo/redo, no dirty marker or unsaved
prompt, no validation panel, and no autosave or recovery.

## Scope

**In scope**

- The Avalonia application project, composition root, startup options, logging, crash boundary, and shell.
- Shell regions, splitters, panel visibility, and persisted layout.
- The document model, tab strip, navigation service with history, navigation-target registry, command catalog,
  menus, status items, and the command palette.
- The project switcher and the project-level explorer.
- Two read-only documents: Project Overview (identity, timestamps, schema/symmetry metadata, file path) and
  Project JSON (canonical JSON, copy, "Save a copy…").
- Project lifecycle: new from template, open, open-or-activate, save, save-as, close, reload, recent files,
  locks, read-only mode, external-change detection, multi-project workspace, window close.
- Versioned application settings, app-data layout, theming/density/font scale/high contrast/reduce motion,
  accessibility metadata, the shortcut cheat sheet, and application diagnostics.
- Dialogs, file pickers, the Application console channel, notifications, and the progress surface.
- Structured logging and the global exception boundary (without recovery writing).
- The shared `ProjectPathRules` helper and the CLI refactor that uses it (§12).
- The 23A headless harness, tests, documentation, parity/status updates, and closeout evidence.

**Non-goals (explicitly out of scope)**

- **No editing of any kind.** No metadata editing, no model editors, no `ProjectEditor`/`ProjectDocument.Apply`
  use from the UI, no `ProjectEditCoordinator`, no undo/redo (Step 23B and Steps 24–25).
- **No dirty state, unsaved-changes prompt, autosave, or crash recovery.** A 23A document cannot be modified, so
  closing never prompts; `ProjectDocument` is only the value holder and the save-point bookkeeper (Step 23B).
- **No validation surface.** No `ProjectValidationPipeline` registration, no status-bar validity indicator, no
  Problems channel, no statistics panel, no diagnostic navigation targets (Step 23B). Step 07's pipeline is
  consumed in 23B.
- **No recovery types.** `IRecoveryStore`, `RecoveryStore`, `CrashRecoveryWriter`, and the `recovery/` writes are
  23B (n-14 resolved by scoping). 23A's app-data layout reserves the directory but writes nothing to it.
- **No model, parameterization, job-template, build, deploy, 3D, run-monitoring, results, or analysis UI**
  (Steps 24–28 and 20–22).
- **No new public *format*, no manifest change, no schema version bump.** `mocassin.settings` is
  application-local; it is not in `contracts/manifest.json`.
- **No change to `.mocprj`, `.msl`, `.msr`, `.mcs`, CLI reports, exit codes, or diagnostic codes.** The
  `ProjectPathRules` extraction must leave the CLI's observable behaviour identical.
- **No new UI control package, no data grid, no paid control, no `MsBox.Avalonia`.** Dialogs are an in-shell
  overlay we own.
- **No localization, auto-update, telemetry, remote deploy, or plugin loading.**
- **No platform installer or packaging** (Step 31).
- **No step branch, feature branch, or code in this design change.** Docs only.

## Original reference

Read `/workspace/mocassin-original` as a read-only reference (GPL-3.0-or-later). Do not copy code or data.

- `Mocassin.UI.GUI/MainWindow.xaml` is a fixed three-column/two-row `Grid`: menu bar, project browser, work tab
  control, console tab control, two `GridSplitter`s, and a status bar. The window opens maximized, centred, and
  sized from `SystemParameters`.
- `MainWindowViewModel` implements `IProjectAppControl`, the service locator every view model receives, and owns
  the menu bar, status bar, browser, console, work tabs, and project manager.
- `StartupWindow` is an undecorated splash; the library is selected before the main window appears.
- Project lifecycle is open/create/close/save over a *library*: one `.mocprj` (SQLite) held multiple projects.
  Copying `DbSet.Local` detached duplicates from the database (do not reproduce).
- Settings are an XML file under `Documents/Mocassin`; the symmetry database path is hard-coded with Windows
  separators. Logging is `Console.WriteLine`; dialogs are `MessageBox`; there is no autosave or crash recovery.
- `ProjectStatusBarView.xaml` is an empty `Grid`: the status-bar slot exists but is never populated.

**Preserved intents:** a single main window with a fixed, resizable region layout; project browser on the left;
tabbed work area with a console beneath it; status bar; a clear loading/empty state before a project is open;
menu-driven commands; keyboard-first operation.

**Corrected in this step:** immutable `Project` values and explicit constructor injection instead of mutable
graphs and `IProjectAppControl`; one project per file with a workspace of open files instead of a database
library; versioned, migratable settings in the platform app-data directory; structured logging; dialogs and
pickers behind interfaces; no `MessageBox`, no `System.Windows.*`; a populated status bar; no splash window (the
welcome document covers the empty/loading state).

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
  `MOCASSIN_DATA_DIR`). Unknown arguments exit `2` with usage text on stderr; a failed startup exits `1`; a
  normal exit returns `0`. The GUI has no other exit-code contract.
- `AppHost` builds a `HostApplicationBuilder`, registers services, builds the provider, and owns disposal. Only
  `Program`/`App` resolve the provider; every view model takes explicit constructor arguments.
- Registration is **explicit and enumerable** in `AppServiceRegistration` (`AddMocassinApp`,
  `AddMocassinShell`, `AddMocassinViewModels`, `AddMocassinViews`). There is no assembly scanning, no
  reflection-based discovery, and no service locator. 23B adds its own `AddMocassinEditing/…` call in `App`.
- Lifetimes:
  - singleton: `AppPaths`, `IAppSettingsStore`, `ISettingsService`, `ILoggerFactory`, `TimeProvider`,
    `IUiDispatcher`, `IDialogService`, `IStorageService`, `IConsoleService`, `ICommandCatalog`,
    `IProjectSessionService`, `INavigationService`, `ShellViewModel`, the shell window;
  - singleton library service: one `ProjectStore` constructed with
    `new ProjectStoreOptions { ValidateOnLoad = false }`, constructor-injected into `ProjectSessionService`. The
    store is the only project persistence path; the session retains `ProjectLoadResult.Diagnostics` for 23B.
    23B registers the `ProjectValidationPipeline`; 23A does not construct it.
  - created per project by `ProjectSessionService` with explicit constructor arguments (not a DI scope):
    `ProjectSession`, `ProjectDocument`, the project-scoped view models. 23B adds the edit coordinator.
- Dependency direction (enforced by the architecture test, §15):

```text
Mocassin.App        -> App.Views -> App.ViewModels -> Packages / Results / Contracts / Core
        \-> Avalonia, Microsoft.Extensions.*, Serilog, System.IO
Mocassin.App.ViewModels -> CommunityToolkit.Mvvm + public library APIs; no Avalonia, no views, no controls,
                           no System.IO, no Environment, no Application, no resource dictionaries
Mocassin.App.Views      -> Avalonia controls and resource dictionaries; no scientific logic; no direct
                           Packages calls; no persistence decisions
```

`Mocassin.App.ViewModels` references `Mocassin.Contracts`, `Mocassin.Packages`, and `Mocassin.Results` directly
and `Mocassin.Core` transitively. 23A uses `Packages`, `Contracts`, and `Core`; the `Results` reference stays in
the graph as the Step 28 target and is not called from 23A code. `Mocassin.Cli` is never referenced: any
behaviour the GUI needs must live in a library.

**The I/O boundary.** `Mocassin.App.ViewModels` may call public library APIs that perform I/O — `ProjectStore`,
`ProjectTemplateFactory` — because those are the shared contracts the CLI uses. It may not touch the file
system, the environment, the theme, or the dispatcher directly. App-local state that is not a library contract
is split into a ViewModels contract plus an implementation in `Mocassin.App` or `Mocassin.App.Views`:

| Concern | ViewModels (contract/state/pure logic) | Implementation |
| --- | --- | --- |
| Settings document | `AppSettings`, `LayoutState`, `PathState`, `AppSettingsValidator`, `SettingsMigration`, `AppSettingsJsonContext`, `IAppSettingsStore` | `AppSettingsStore` in `Mocassin.App` (atomic write, fsync, size cap, app-data path) |
| External-change probe | `IProjectFileProbe` (length, last-write UTC, content bytes) and the pure `FileStamp` comparison | `ProjectFileProbe` in `Mocassin.App` |
| Theme/density/contrast/motion | `ThemeSelection`, `Density`, `ThemeService` state + `IThemeApplier` | `AvaloniaThemeApplier` + resource dictionaries in `Mocassin.App.Views` |
| App-data root | `AppDataOverride` is a pure value only | `AppStartupOptions` parses `--data-dir` and reads `MOCASSIN_DATA_DIR`; `AppPaths` resolves `<LocalApplicationData>/Mocassin` and its subfolders from `Environment.SpecialFolder`. Both types and the environment read live in `Mocassin.App`; ViewModels never reads the environment |
| Picker and dispatcher | `IStorageService`, `IUiDispatcher` | `AvaloniaStorageService`, `AvaloniaUiDispatcher` in `Mocassin.App.Views` |

**The I/O token test and its limits.** A structure test scans `Mocassin.App.ViewModels/**/*.cs` after stripping
`//` and `/* */` comments and string literals, and fails on a **whole-identifier** match (negative lookbehind, so
`LastOpenDirectory.Trim` does not match `Directory.`) of `System.IO`, `File.`, `FileInfo`, `Directory.`,
`DirectoryInfo`, `Path.Combine`, `Path.Get`, `FileStream`, `StreamReader`, `StreamWriter`, `Environment.`,
`Avalonia`, `Dispatcher.UIThread`, `Application.Current`, or `ResourceDictionary`, and on
`using static System.IO`/`using static System.Environment` (round-3 n-15). `IUiDispatcher`/`Dispatcher` and bare
`Application` are permitted. A **positive control** feeds the scanner a seeded violation and asserts it is
reported, so the test cannot pass by matching nothing. The scan is a tripwire, not a compiler guarantee: the
direct-reference check and review remain the primary enforcement, and the test documentation states that
limitation.

**Visibility.** The pre-existing public types stay public: `Mocassin.App.ViewModels.AppViewModelsModule` and
`Mocassin.App.Views.AppViewsModule` (module markers listed in `PublicAPI.Shipped.txt`, baselined under
`tests/contracts/api-baseline/net11.0/`, checked by ApiCompat) and `Mocassin.App.Program` (the existing entry
point; `Mocassin.App` is not in `tools/api-projects.txt` and has no public-API baseline or list). Every **new**
type **other than `App`** is `internal`, with `[assembly: InternalsVisibleTo("Mocassin.App.Tests")]`. `App` is
the only new public App type; `Program` is pre-existing.

Round-3 m-18: `Mocassin.App` currently contains only `Mocassin.App.csproj` and `Program.cs`. 23A **adds**
`App.axaml`, `App.axaml.cs`, and `public sealed class App : Application`; that type is new, but it is not part of
any ApiCompat project, `PublicAPI.*` list, or API baseline, so no baseline refresh is needed and none is
proposed. The only ApiCompat-checked App assemblies remain the pre-existing `Mocassin.App.ViewModels` and
`Mocassin.App.Views`, whose `PublicAPI.*` files and baselines are unchanged.

- ADR 0003/0006/0008 stand. ADR 0003's "no new UI control dependency without an ADR" is honoured: no control
  package is added.

### 2. Shell layout and regions

```text
┌─────────────────────────────────────────────────────────────────────────────┐
│ Menu: File  Edit  View  Tools  Help            (Model/Build/Run: see §4)    │
├──────────────┬───────────────────────────────────────────────┬──────────────┤
│ Projects     │ Document tabs                                 │ Inspector    │
│ (switcher)   │ ┌───────────────────────────────────────────┐ │ (selection)  │
│ ──────────   │ │ Active document (ViewLocator)             │ │              │
│ Explorer     │ │                                           │ │              │
│  Project     │ └───────────────────────────────────────────┘ │              │
│  (tree)      │                                               │              │
├──────────────┴───────────────────────────────────────────────┴──────────────┤
│ Console: Application                       (Problems/Build/Run: later steps) │
├─────────────────────────────────────────────────────────────────────────────┤
│ Status: active document · file path/lock · theme/settings · version         │
└─────────────────────────────────────────────────────────────────────────────┘
```

- `ShellView` is a `Grid` with `GridSplitter`s: left column (`ProjectsRegion`: project switcher above the
  explorer), centre (`DocumentsRegion` + `ConsoleRegion` split vertically), right column (`InspectorRegion`),
  and fixed rows for menu and status. No docking library and no floating panes.
- Each region host is a `ContentControl` whose `DataTemplate` is chosen by the region view model. Regions are
  collapsible through `View` menu commands and `ShellViewModel` toggles with accelerators
  (`Alt+1/2/3` left/centre/right, `Alt+0` console); sizes and visibility persist in settings (`layout`, §6).
- Panel sizes are clamped (left 160–600 px, right 160–800 px, console 80–600 px) so a corrupt settings file
  cannot produce an unusable window; an invalid value falls back to the default and is repaired on the next
  save.
- Window title binds `"<project name> — MOCASSIN"` for the active document, or `"MOCASSIN"` with no project
  (the `*` dirty marker is added by 23B). Minimum size 900×600. Size/state are restored from settings; a saved
  placement whose bounds intersect no current screen falls back to the primary screen (pure
  `WindowPlacementResolver`, test in §15).
- The status bar in 23A shows the active document, the project path and lock state, the theme/density, and the
  version. It reserves the validity/progress/autosave slots for 23B and later steps; it shows no placeholder
  text for them.
- **No splash window.** Startup is short and a splash would add a second window to headless tests; the welcome
  document covers the empty state.

### 3. Document model, tabs, and the documents shipped in this step

```csharp
// Mocassin.App.ViewModels/Documents (internal)
internal abstract partial class DocumentViewModel : ObservableObject
{
    public DocumentKey Key { get; }
    public abstract string Title { get; }              // bindable, raises change notifications
    public virtual string? ToolTip => null;
    public virtual Task OnClosingAsync(CancellationToken cancellationToken);
}

internal readonly record struct DocumentKey(string Kind, Guid? ScopeId = null);
```

- `Kind` values are stable strings: `welcome`, `project.overview`, `project.json`; 23B adds none, Steps 24+
  add `model.*`. `ScopeId` is the project id, so two open projects can each have a Project Overview tab.
- `DocumentCollectionViewModel` owns `ObservableCollection<DocumentViewModel>`, `ActiveDocument`,
  `OpenOrActivate(document)` (reuse by `Key`, otherwise append and activate), `CloseAsync`, `CloseOthersAsync`,
  `CloseAllAsync`, and `ActiveDocumentChanged`.
- Tab strip: title, tooltip with the full path, close button, middle-click close, `Ctrl+W`,
  `Ctrl+Tab`/`Ctrl+Shift+Tab`, `Ctrl+1..9`, drag reorder, context menu (Close, Close Others, Close All, Copy
  path). Dragging a tab out is deferred. The dirty marker is a 23B addition; 23A's tab strip has no dirty state.
- The welcome document exists on a fresh start and closes when the first real document opens. It offers
  New/Open/Recent and the shortcut cheat sheet.
- **Project Overview** (read-only in 23A): project id, name, description/author/notes (display only), created
  timestamp, last-saved timestamp from `ProjectSession.LastWritten`, the master seed (copy), schema version,
  symmetry metadata, the file path, and the lock/read-only state. 23B adds the metadata editor and the
  validation/statistics panels.
- **Project JSON**: the indented canonical project JSON (`ProjectJson`) in a read-only, searchable text view
  with copy and "Save a copy…" (export). It is never parsed back into the editor.
- Inspector region: shows the active document summary or a neutral empty state. The contract is a
  `ContentControl` bound to `ShellViewModel.InspectorContent`, set by the document's optional
  `IInspectorContributor`; Steps 24/25 replace the content. No editor inspector exists in 23A.

### 4. Navigation, explorer, commands, and contribution seams

```csharp
internal enum NavigationIntent { Activate, SelectObject, FocusProperty }

internal interface INavigationService
{
    Task OpenDocumentAsync(DocumentViewModel document, CancellationToken cancellationToken = default);
    Task<bool> ActivateAsync(DocumentKey key, CancellationToken cancellationToken = default);
    Task<bool> NavigateToAsync(ModelPath path, NavigationIntent intent, CancellationToken cancellationToken = default);
    bool CanGoBack { get; }
    bool CanGoForward { get; }
    Task GoBackAsync(CancellationToken cancellationToken = default);
    Task GoForwardAsync(CancellationToken cancellationToken = default);
}

internal interface INavigationTargetProvider
{
    IReadOnlyCollection<string> Domains { get; }
    Task<DocumentViewModel?> CreateAsync(ModelPath path, NavigationIntent intent, CancellationToken cancellationToken);
}
```

- `NavigateToAsync` resolves the `ModelPath.Domain` against the registered providers of the active project's
  scope. Exactly one provider per domain; a duplicate registration is a startup error.
- No provider for a domain: append an information entry to the Application console ("Editing `<domain>` is not
  available in this version"); never throw, never open a placeholder editor. This is what makes the parallel
  track safe: a Step 07 diagnostic with a `particle` path becomes navigable the day Step 24 registers the
  provider, and is harmless before that. 23A registers only the `project` provider.
- History is a bounded back/forward stack (100 entries) of `(ProjectId, DocumentKey, ModelPath?, intent)`;
  `Alt+Left`/`Alt+Right`; navigating to the already-active document is not pushed.
- Explorer: project-level nodes only (Overview, Data (JSON)). `IExplorerNodeProvider` lets 23B (diagnostics) and
  Steps 24–26 add nodes without the shell changing.
- Project switcher: open projects with path and lock state; activating one activates its last document. 23A
  closes projects without an unsaved prompt because nothing can be modified; 23B adds the prompt.

**Contribution seams** (all internal in `Mocassin.App.ViewModels`, all registered explicitly):

```csharp
internal interface IInspectorContributor { object? CreateInspector(InspectorSelection selection); }
internal interface IExplorerNodeProvider { IEnumerable<ExplorerNode> CreateNodes(ProjectSession session); }
internal interface IMenuItemProvider { IEnumerable<ShellCommand> CreateMenuItems(MenuKind menu); }
internal interface IConsoleChannelProvider { IEnumerable<ConsoleChannelViewModel> CreateChannels(); }
internal interface IStatusBarItemProvider { IEnumerable<StatusBarItemViewModel> CreateItems(); }
```

`InspectorSelection` is the shell's typed selection value (active `DocumentKey`, project id, validated
`ModelPath` list, optional domain tag) — not `object`. `ExplorerNode` is an immutable node record (key, label,
icon key, `ModelPath?`, children, command); `StatusBarItemViewModel` is the base for the version, progress,
validity, and autosave slots that later steps fill. The shell validates at startup that no two providers claim
the same navigation domain or inspector tag and that nothing is registered twice.

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
}

internal enum ProjectSessionOutcome
{
    Created, Opened, Activated, OpenedReadOnly, Saved, Reloaded, Closed,
    NotFound, TooLarge, Io, InvalidContent,          // mirrors ProjectLoadFailureKind
    Locked, SaveFailed, Canceled,
    UnsavedChanges,                                   // reserved for 23B; never produced in 23A
}
```

- **Services never prompt.** `IProjectSessionService` returns a typed `ProjectSessionResult` covering success,
  the four load failures mapped one-to-one from `ProjectLoadResult.FailureKind` (`NotFound`, `TooLarge` for the
  64 MiB `MaxDocumentBytes` limit, `Io`, `InvalidContent`), `Locked`, `SaveFailed`, and `Canceled`, plus the
  diagnostics. `UnsavedChanges` is defined now as a reserved outcome so the result type does not change shape
  in 23B; 23A never produces it because no document is editable, and 23B adds the prompt that does. The shell
  owns every dialog. There is no second store abstraction.
- **Store configuration.** The single `ProjectStore` is constructed with
  `new ProjectStoreOptions { ValidateOnLoad = false }`. The load result's `ProjectLoadResult.Diagnostics` are
  retained on the session as `LoadDiagnostics` and handed to 23B's validation pipeline; 23A runs no validation
  and never calls `ProjectValidationPipeline` (the M-1 double-validation contract is completed in 23B).
- **New**: `ProjectTemplateFactory.CreateProject(kind, name, utcNow)` — the same factory as `mocassin new` —
  after validating the name **and target path** with the shared `ProjectPathRules` (§12). Step 23A ships exactly
  the kinds `ProjectTemplateKind` defines today (`blank`, `ceria`) with the CLI's display names. For **New
  only**, an existing target is refused with the same decision and message as the CLI's `CLI003` case; New never
  overwrites.
- **Open/Activate**: picker, recent list, or drag-and-drop of a `.mocprj`. `OpenAsync` creates a session or
  returns a load failure; opening an already-open path calls `ActivateAsync` and never creates a second session.
  The picker starts in `paths.lastOpenDirectory` and updates it on success.
- **Save**: `ProjectStore.SaveAsync(current, path)`; on success call `ProjectDocument.MarkSaved()` (no-arg),
  store the returned value as `ProjectSession.LastWritten` for the file stamp and displayed timestamp, refresh
  the file stamp, and close the "save as" dialog state. 23B adds sidecar deletion.
- **Save As**: acquire the new lock before releasing the old one; on success update the session path, recent
  list, file stamp, and document display data. A target that another session has open is refused. Unlike New,
  Save As to an existing on-disk file is allowed once the OS picker's overwrite confirmation is accepted; the
  store replaces the file atomically.
- **Reload**: re-read the file and replace the session's `ProjectDocument` with a fresh instance over the
  reloaded value (23A has no undo history to discard), refresh the stamp and `LoadDiagnostics`. Used by the
  external-change flow and by "Reload" in the File menu.
- **Close**: dispose the lock and close the project's documents. 23A has no unsaved-changes prompt because no
  document can be modified; 23B adds it. A read-only session closes without touching another session's files.
- **Recent files**: at most 10 entries `{path, lastOpenedUtc}` in settings; de-duplicated by full path
  (case-insensitive on Windows/macOS, ordinal on Linux); a missing entry renders disabled with "Remove from
  list" and is never auto-removed silently.
- **Locks**: `ProjectStore.AcquireLockAsync` at open. Failure → open read-only, show a banner, disable Save,
  allow Save As (which acquires a new lock) and Close. Stale-lock reclaim stays at the store default
  (`StaleLockTimeout`, 10 minutes); the UI shows the lock path and acquisition time.
- **External modification**: `ProjectFileStamp` records `{length, lastWriteUtc, documentHash}` where the hash is
  the canonical document hash of `ProjectSession.LastWritten`. Re-check on window activation and before Save,
  using `{length, lastWriteUtc}` as a cheap filter. Outcomes: unchanged → nothing; changed → offer Reload /
  Keep (Keep is only meaningful once editing exists; in 23A it reads "Reload and discard in-memory view"); a
  deleted file → offer Save As or Close. **Never** overwrite silently. No file-system watcher is used.
- **Window close**: with no editable documents, 23A closes the window after releasing locks. There is no
  unsaved-changes flow and no `cleanExit`/session block in 23A; 23B adds both with its dirty-state work.
- **`--project` at startup**: a missing, unreadable, or invalid path does not prevent startup; the shell
  reports the failure in the Application console and a notification and continues with the welcome flow.

### 6. Settings, theming, density, and accessibility

- **Where the pieces live.** The settings model, pure validator/migrations, the source-generated
  `AppSettingsJsonContext`, `IAppSettingsStore`, and `ISettingsService` (the state machine the view models
  observe) are in `Mocassin.App.ViewModels`. The file store (`AppSettingsStore`) is in `Mocassin.App`. The theme
  applier (`AvaloniaThemeApplier` and the resource dictionaries) is in `Mocassin.App.Views`. ViewModels has no
  `System.IO`, no `Environment`, no `Application`, and no resource dictionaries (§1).
- Settings live in `<app-data>/Mocassin.Settings.json`; the root is `<LocalApplicationData>/Mocassin`
  (`%LOCALAPPDATA%`, `~/.local/share`, `~/Library/Application Support`), overridable by
  `--data-dir`/`MOCASSIN_DATA_DIR`. Subfolders: `logs/`, `cache/` (reserved for Step 27), `recovery/` (reserved;
  23B is the first writer). The document shape is normative in §6.1.
- Validation on load: an unknown enum, out-of-range number, or malformed entry falls back to the default for
  that field and logs a warning; a document whose `format` is wrong or whose `schemaVersion` is a future major
  is not loaded and **not overwritten** (defaults, banner, "Save as new file"). Unknown fields are tolerated and
  not preserved on rewrite; older documents are migrated by pure functions.
- Write discipline: serialise through `AppSettingsJsonContext` to UTF-8 without BOM (2-space indent, `\n`),
  write a sibling temp file, flush and `Flush(flushToDisk: true)`, then replace. `MaxSettingsBytes = 4 MiB` and
  a 10-entry `recentFiles` cap bound the document; a failed or oversized write leaves the previous file intact.
- Theme: `ThemeSelection` (`system`/`light`/`dark`) maps to `Application.RequestedThemeVariant`; the accent
  overrides the Fluent accent resources; density (`comfortable`/`compact`) swaps an app resource dictionary;
  font scale 0.85–1.5 scales font sizes; reduce motion disables non-essential transitions. High contrast uses an
  app-owned `HighContrast.axaml` with WCAG-verified pairs, selected from settings and also followed when the
  platform requests a high-contrast variant if Avalonia exposes it (V-2).
- Accessibility target **WCAG 2.2 AA**: accessible names on every interactive control, names/help text on
  icon-only buttons, visual-order focus, visible focus never trapped, dialogs move and restore focus, keyboard-
  operable trees/lists, status conveyed by name/role not colour alone, and text that scales with the font-scale
  setting. Tab headers, splitters, the console list, the explorer tree, and the palette are the risk points.
- The settings dialog: theme, accent (small fixed palette), density, font scale, high contrast, reduce motion,
  confirm-on-external-change, recent-file list with clear, and
  Reset to defaults / Open config folder / Clear logs and cache / Close. The autosave enable/interval control is
  added by 23B with the `autosave` settings block. "Clear logs and cache" deletes only
  `logs/` and `cache/` files that are not the active log, and reports bytes freed; no project, package, results,
  or job-folder path is touched.

#### 6.1 `mocassin.settings` v1 (normative)

`Mocassin.Settings.json` is application-local and single-consumer. It is **not** in `contracts/manifest.json`,
has no cross-language fixtures, and is not a public format. [`docs/reference/file-layout.md`](../../reference/file-layout.md)
**will mirror** this section for users once the 23A documentation column lands (§14); at this design freeze that
page has no settings/app-data section. This section is the normative design shape. 23B adds two objects to the
same v1 document additively (§23B §9) with **no** `schemaVersion` bump.

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
  "paths": { "lastOpenDirectory": null, "lastSaveDirectory": null, "lastFolderDirectory": null },
  "recentFiles": [],
  "layout": { "leftWidth": 260, "rightWidth": 320, "consoleHeight": 180,
              "leftVisible": true, "rightVisible": true, "consoleVisible": true,
              "activeConsoleChannel": "application" }
}
```

| Key | Type | Default | Rule |
| --- | --- | --- | --- |
| `format` | string | — (required) | Must equal `"mocassin.settings"`; any other value makes the document invalid and it is not loaded or overwritten |
| `schemaVersion` | integer | `1` | Current major is `1`; a value above the current major is rejected without overwriting; a lower value is migrated |
| `theme` | string enum | `"system"` | `system` / `light` / `dark` |
| `accent` | string or null | `null` | One of the small fixed palette keys; `null` means the Fluent default |
| `density` | string enum | `"comfortable"` | `comfortable` / `compact` |
| `fontScale` | number | `1.0` | 0.85–1.5 inclusive; outside the range falls back to `1.0` with a warning |
| `highContrast` | boolean | `false` | Uses the app-owned high-contrast dictionary |
| `reduceMotion` | boolean | `false` | Disables non-essential transitions |
| `culture` | string | `"en"` | BCP-47 tag used for number/date formatting; the UI text stays invariant English (localization is deferred) |
| `logging.level` | string enum | `"information"` | `trace` / `debug` / `information` / `warning` / `error` / `fatal` |
| `ui.confirmOnExternalChange` | boolean | `true` | Ask before reloading after an external modification |
| `ui.showWelcomeOnStart` | boolean | `true` | Open the welcome document on a fresh start |
| `paths.lastOpenDirectory` | string or null | `null` | Last accepted open-picker directory |
| `paths.lastSaveDirectory` | string or null | `null` | Last accepted save-picker directory |
| `paths.lastFolderDirectory` | string or null | `null` | Last accepted folder-picker directory (export, config folder) |
| `recentFiles` | array of objects | `[]` | At most 10 entries `{ "path": string, "lastOpenedUtc": ISO-8601 UTC }`, de-duplicated by path, newest first |
| `layout.leftWidth` / `rightWidth` / `consoleHeight` | integer (px) | `260` / `320` / `180` | Clamped to 160–600 / 160–800 / 80–600; an out-of-range value falls back to the default |
| `layout.leftVisible` / `rightVisible` / `consoleVisible` | boolean | `true` | Panel visibility |
| `layout.activeConsoleChannel` | string | `"application"` | Channel id; an unknown id falls back to the first available channel. 23B may persist `"problems"` |

There are no other keys or nesting levels in v1; any object not listed is unknown (see the rules below).

**Read and migration policy.** Unknown properties are **ignored** (the settings reader does not use
`JsonUnmappedMemberHandling.Disallow`, unlike the strict `.mocprj` reader) and are dropped on the next write. A
missing key takes its default. A value with the wrong type, an unknown enum member, or an out-of-range number
falls back to that field's default and writes one warning to the log. A malformed or unreadable document loads
all defaults, shows a banner, and is not overwritten. Migrations are pure functions in `SettingsMigration`
keyed by `schemaVersion` and are idempotent; version 1 is the first version, so the v1 migration is the identity.

**Size and write discipline.** `MaxSettingsBytes = 4 MiB` bounds the serialized document and `recentFiles` is
capped at 10 entries; a larger document is rejected as invalid and is not loaded, so a corrupt or hostile file
cannot exhaust memory. Writing serialises through the source-generated `AppSettingsJsonContext` to UTF-8 without
BOM, 2-space indent, `\n` line endings, writes a sibling temp file, flushes and `Flush(flushToDisk: true)`, then
replaces the target. A failed or interrupted write leaves the previous file intact.

### 7. Dialogs, pickers, consoles, and notifications

```csharp
internal interface IDialogService
{
    Task<bool> ConfirmAsync(ConfirmDialogRequest request, CancellationToken cancellationToken = default);
    Task<DialogChoice> ChooseAsync(ChoiceDialogRequest request, CancellationToken cancellationToken = default);
    Task<string?> PromptAsync(TextPromptRequest request, CancellationToken cancellationToken = default);
    Task ShowErrorAsync(ErrorDialogRequest request, CancellationToken cancellationToken = default);
    IProgressDialog ShowProgress(ProgressDialogRequest request);
}

internal interface IStorageService
{
    Task<string?> PickOpenProjectAsync(CancellationToken cancellationToken = default);
    Task<string?> PickSaveProjectAsync(string suggestedName, CancellationToken cancellationToken = default);
    Task<string?> PickFolderAsync(CancellationToken cancellationToken = default);
}
```

- Dialogs are an **in-shell overlay** in `Mocassin.App.Views`: `DialogHost` over the shell content with a scrim,
  focus capture/restore, `Esc` = cancel, default button on `Enter`, one modal at a time. Each dialog has a plain
  `DialogViewModel`, so the real path is exercised headless. No `MessageBox`, no `MsBox.Avalonia`.
- `IStorageService` wraps Avalonia's `IStorageProvider` in `Mocassin.App.Views`, returns plain strings, and reads
  and updates the last directory from settings. A null return means "cancelled", never an error.
- Progress: one `IProgressDialog` per long operation with a determinate/indeterminate bar, status line,
  cancellation, and a details expander; the status bar mirrors it. 23A uses it for open and save-all.
- Notifications: transient toasts for background completions and non-blocking failures, plus the Application
  console for anything the user may want to read later.
- Consoles: `ConsoleViewModel` with channels. 23A ships the **Application** channel
  (`{severity, timestampUtc, source, message, optional action}`, 2,000-entry ring buffer, severity/text filter,
  clear, copy). 23B adds **Problems**, Step 26 **Build**, Step 28 **Run** through `IConsoleChannelProvider`; no
  channel is a dead tab.
- Error presentation: library failures carry their `Diagnostic` list; the dialog shows the first code/message
  plus a "Details" expander. No stack trace in a normal dialog; stack traces go to the log and the crash dialog.

### 8. Logging and crash handling

- `Microsoft.Extensions.Logging` with Serilog sinks: a date-suffixed rolling file in
  `<app-data>/logs/mocassin-<date>.log` (daily rolling, minimum level from settings, console sink only in Debug
  builds). Structured properties include project id, document key, operation, and duration where applicable.
  The sink owns record timestamps; `TimeProvider` is for documents, stamps, and retention decisions, not for
  logging. No credentials, no remote sinks, no telemetry.
- Retention: 14 days or 100 MB, whichever comes first. The file sink uses a daily interval and a retained count
  of 14; the size cap uses the sink's retained-size option if the pinned version has it, otherwise an explicit
  startup sweep (`LogRetention`) deletes the oldest closed files until under 100 MB (V-3). The active file is
  never deleted.
- Global handlers, installed before the first window: `AppDomain.UnhandledException`,
  `TaskScheduler.UnobservedTaskException`, and `Dispatcher.UIThread.UnhandledException`. Each logs the exception
  with stack trace, active project path, and last command id.
- On an unhandled exception: if the UI thread is alive, log and show the crash dialog ("Open log folder",
  "Exit"), then exit `1`. If the UI thread is dead, write a crash report next to the logs and exit `1` with a
  stderr message. The process never dies silently. **23A's crash dialog has no recovery action**; 23B adds
  "Write recovery files" with the bounded asynchronous writer (n-14 resolved: `IRecoveryStore`/
  `CrashRecoveryWriter` are 23B types and are not referenced in 23A).
- `Help → Application diagnostics` shows a copyable report: app and .NET version, OS/RID, data-dir path and
  writability, settings parse result and version, log-folder state and size, theme resources, recent-file
  existence, open-project lock states. It is not the CLI `doctor`; the GUI does not reference `Mocassin.Cli`.

### 9. State ownership and threading rules

| State | Owner | Persisted |
| --- | --- | --- |
| Project content | `Project` (immutable) held by `ProjectDocument` | `.mocprj` |
| Save point and last-written value | `ProjectDocument` / `ProjectSession.LastWritten` | derived |
| Open projects, paths, locks, file stamps, load diagnostics | `ProjectSession` | lock file; paths in settings |
| Documents, tabs, active document | `DocumentCollectionViewModel`/`ShellViewModel` | open paths in settings |
| Panel sizes, visibility, theme selection, density, recent files, last directories | `ShellViewModel`/settings region VMs (state only) | `Mocassin.Settings.json`, written by `AppSettingsStore` |
| File bytes, app-data paths, theme resources, dispatcher | `Mocassin.App` / `Mocassin.App.Views` | the file system / Avalonia |

Rules:

- No scientific logic in any view model: no arithmetic on energies/geometry, no unit conversion, no validation
  rules, no JSON parsing, no hashing. View models translate between library values and observable properties.
- No direct I/O in `Mocassin.App.ViewModels`: library APIs are allowed; `System.IO`, `Environment`,
  `Application`, and resource dictionaries are not. App-local files go through `IAppSettingsStore` and
  `IProjectFileProbe`; theme changes go through `IThemeApplier`. The token test in §1 enforces the ban.
- All `ProjectDocument` operations happen on the UI thread. 23A has no background project work beyond file
  reads/writes performed by `ProjectStore` and the file probe.
- `IUiDispatcher` abstracts `Dispatcher.UIThread` for the few off-thread callbacks (timers, file checks, probe
  completions); tests use an inline implementation, which is a permitted UI-thread boundary fake.
- `TimeProvider` is injected for stamps, retention, and session decisions. No `DateTime.Now`, no `Task.Delay`
  in production code.
- Long operations take a `CancellationToken`, report `IProgress<T>`, and never block the UI thread. No
  sync-over-async, no `Dispatcher.UIThread.Invoke` from a command body.
- One owner per open project: the session. One collection owns the tab set.

### 10. View-model and view contracts

- `Mocassin.App.ViewModels` is UI-framework-free (`ObservableObject`, `IAsyncRelayCommand`,
  `ObservableValidator`) and holds the shell/region view models, document model, navigation, session service,
  settings model/pure logic, dialog/picker/dispatcher contracts, and the command catalog. It references
  `CommunityToolkit.Mvvm` and the public library APIs; it has no Avalonia reference and no `System.IO` use.
- `Mocassin.App.Views` holds the shell window/views, region hosts, `DialogHost`, console/problems views, settings
  and palette views, theme/density/high-contrast resource dictionaries, `AvaloniaThemeApplier`,
  `AvaloniaStorageService`, `AvaloniaUiDispatcher`, and the key-binding binder. A view never calls a library or
  contains scientific logic; code-behind is limited to focus, splitter keyboard handling, and drag-reorder
  plumbing.
- `Mocassin.App` holds `Program`, `App.axaml(.cs)`, `ViewLocator`, the composition root, and the app-local I/O
  implementations (`AppSettingsStore`, `ProjectFileProbe`, `AppPaths`, log retention). `ViewLocator` maps a
  view-model type to `Mocassin.App.Views.<same path minus "ViewModels">.<TypeName minus "ViewModel">View` by
  convention, with three safeguards: the type must live in an App view-model assembly, the resolved type must be
  a control, and a missing view logs an error and renders an explicit "view not found" panel in Release.
- **Visibility (M-4/m-18).** The existing public `AppViewModelsModule`/`AppViewsModule` markers and the
  `Mocassin.App.Program` entry point stay public. 23A adds `public sealed class App : Application` and the
  `App.axaml(.cs)` files; `Mocassin.App` is not an ApiCompat project and has no `PublicAPI`/baseline, so this is
  not a compatibility change. Every new type **other than `App`** is `internal` with
  `InternalsVisibleTo("Mocassin.App.Tests")`; `App` is the only new public App type and `Program` is
  pre-existing. The ApiCompat-checked App assemblies (`Mocassin.App.ViewModels`, `Mocassin.App.Views`) keep
  their `PublicAPI.*` and baselines unchanged.
- Every view model is constructed with explicit dependencies; no `Application.Current` lookups, no static
  service access, no view references, no `System.Windows.*`.
- Observable properties are source-generated (`[ObservableProperty]`); commands use `[RelayCommand]`.

### 11. Project and folder layout

Following [`docs/development/code-structure.md`](../../development/code-structure.md) §2.3: roots hold only
entry points/metadata, folders are concept-named, no catch-all folders, 20 files per folder, depth ≤4, ≤800
lines per file, namespace equals folder, single-type files.

```text
src/dotnet/Mocassin.App/
  Program.cs  App.axaml  App.axaml.cs  ViewLocator.cs  AssemblyInfo.cs
  Composition/     AppHost.cs  AppServiceRegistration.cs  AppStartupOptions.cs  AppPaths.cs  LoggingSetup.cs  LogRetention.cs
  Settings/        AppSettingsStore.cs  SettingsWritePolicy.cs
  Files/           ProjectFileProbe.cs
  Startup/         StartupCoordinator.cs  CrashHandler.cs  WindowPlacementResolver.cs

src/dotnet/Mocassin.App.ViewModels/
  AppViewModelsModule.cs   # existing public marker (PublicAPI.Shipped.txt); retained unchanged
  Shell/           ShellViewModel.cs  ShellRegionsViewModel.cs  StatusBarViewModel.cs  ShellWindowState.cs
                   StatusBarItemViewModel.cs  IUiDispatcher.cs
  Documents/       DocumentViewModel.cs  DocumentKey.cs  DocumentCollectionViewModel.cs  WelcomeDocumentViewModel.cs
  Documents/Project/ ProjectOverviewViewModel.cs  ProjectJsonViewModel.cs
  Projects/        ProjectSession.cs  ProjectSessionService.cs  IProjectSessionService.cs  ProjectSessionResult.cs
                   ProjectSessionOutcome.cs  CloseSessionResult.cs  FileStamp.cs  ExternalChangeDetector.cs
                   RecentFilesViewModel.cs
  Navigation/      INavigationService.cs  NavigationService.cs  INavigationTargetProvider.cs  NavigationIntent.cs
                   NavigationHistory.cs  ProjectNavigationTargetProvider.cs  IExplorerNodeProvider.cs
                   ExplorerNode.cs  InspectorSelection.cs  IInspectorContributor.cs
  Settings/        AppSettings.cs  ISettingsService.cs  SettingsService.cs  SettingsMigration.cs  AppSettingsValidator.cs
                   AppSettingsJsonContext.cs  IAppSettingsStore.cs  SettingsViewModel.cs  ThemeService.cs
                   ThemeSelection.cs  IThemeApplier.cs  LayoutState.cs  PathState.cs
  Commands/        ShellCommand.cs  ICommandCatalog.cs  CommandCatalog.cs  KeyGestureDescriptor.cs
                   ModifierKeys.cs  KeyName.cs  KeyGestureCatalog.cs  IMenuItemProvider.cs  MenuKind.cs
                   IConsoleChannelProvider.cs  IStatusBarItemProvider.cs  CommandPaletteViewModel.cs
  Consoles/        ConsoleViewModel.cs  ConsoleChannelViewModel.cs  ConsoleEntryViewModel.cs  IConsoleService.cs
                   ConsoleService.cs
  Dialogs/         IDialogService.cs  IStorageService.cs  IProgressDialog.cs  DialogViewModel.cs  DialogRequest.cs
                   ConfirmDialogRequest.cs  ChoiceDialogRequest.cs  TextPromptRequest.cs  ErrorDialogRequest.cs
                   ProgressDialogRequest.cs  DialogChoice.cs

src/dotnet/Mocassin.App.Views/
  AppViewsModule.cs        # existing public marker (PublicAPI.Shipped.txt); retained unchanged
  Shell/  Documents/  Navigation/  Consoles/  Settings/  Dialogs/  Controls/
  Theming/         AvaloniaThemeApplier.cs  ThemeResources.axaml
  Themes/          Density.axaml  HighContrast.axaml  Accent.axaml  Metrics.axaml
  Platform/        AvaloniaStorageService.cs  AvaloniaUiDispatcher.cs  ViewKeyBindingBinder.cs

src/dotnet/Mocassin.App.Tests/
  TestSupport/     ManualTimeProvider.cs  InlineUiDispatcher.cs  RecordingDialogService.cs  StubStorageService.cs
                   TempWorkspace.cs  HeadlessAppBuilder.cs  ProjectFixtures.cs
  ViewModels/…  Views/…  Visual/…  Visual/Baselines/…
```

No `Internal/` folder is added at a project root (`Internal` is nested-only). Every listed file is a single-type
file; the implementation runs `./tools/verify.sh structure` and does not weaken `tools/structure-policy.json`.

`Mocassin.App.ViewModels.csproj` gains `CommunityToolkit.Mvvm`; `Mocassin.App.csproj` gains `Avalonia`,
`Avalonia.Desktop`, `Avalonia.Fonts.Inter`, `Avalonia.Themes.Fluent`, `Microsoft.Extensions.*`, `Serilog` +
sinks; `Mocassin.App.Views.csproj` gains `Avalonia` and `Avalonia.Themes.Fluent`; `Mocassin.App.Tests.csproj`
gains `Avalonia.Headless` and `Avalonia.Headless.XUnit` (test-only). All versions come from
`Directory.Packages.props`; Avalonia stays pinned to 12.1.6.

### 12. Library change owned by this step

One additive, behaviour-preserving change:

| Change | Where | Why the shell cannot own it | Contract effect |
| --- | --- | --- | --- |
| `ProjectPathRules.NormalizeName/ValidateTarget` + `NewProjectCommand` refactor to call it | `Mocassin.Packages.Projects.Templates` and `Mocassin.Cli.NewProject` | `File > New` must enforce exactly the CLI's name **and target-path** rules (trimmed name, `.mocprj` suffix, length, separators, `..`, control and reserved characters, existing target, directory creation); duplicating them guarantees drift | New public helper; CLI codes, exit codes, file behaviour, and message text stay identical, and **new CLI tests pin the exact message text** moved into the helper |

**API (n-7).** The helper separates pure name rules from the file-system check so the CLI and the GUI share one
decision without either doing I/O in the UI or duplicating the rules:

```csharp
namespace Mocassin.Packages.Projects.Templates;

public enum ProjectPathFailure
{
    None, EmptyName, NameTooLong, PathSeparator, ParentDirectory, ControlCharacter,
    ReservedCharacter, InvalidTargetPath, ExistingTarget, IoFailure,
}

public readonly record struct ProjectNameResult(
    bool IsSuccess, string Name, ProjectPathFailure Failure, string? Message);

public readonly record struct ProjectTargetResult(
    bool IsSuccess, string FullPath, ProjectPathFailure Failure, string? Message);

// The only file-system fact the caller supplies; every other input is pure.
public enum ProjectTargetState { Absent, File, Directory }

public static class ProjectPathRules
{
    // Pure: trims, removes one ".mocprj" suffix, enforces length <= 128, rejects separators, "..",
    // control characters and : * ? " < > |. No I/O. On failure Name is the trimmed input for the message.
    public static ProjectNameResult NormalizeName(string? rawName);

    // Pure: combines the directory with the normalized name and maps File -> ExistingTarget and
    // Directory -> IoFailure. InvalidTargetPath is GUI-only (an empty/invalid directory chosen in a
    // picker); the CLI keeps its own empty-output-directory error (CLI002/3) and never produces it.
    public static ProjectTargetResult ValidateTarget(
        string? directory, ProjectNameResult name, ProjectTargetState targetState);

    // One message per failure, used verbatim by the CLI and the GUI; optional detail is appended.
    public static string GetMessage(ProjectPathFailure failure, string? detail = null);
}
```

**CLI mapping.** `NewProjectCommand` keeps its current observable behaviour. `InvalidTargetPath` is listed for
completeness even though the CLI never produces it:

| Helper result | CLI diagnostic / exit |
| --- | --- |
| `NormalizeName` failure (`EmptyName`, `NameTooLong`, `PathSeparator`, `ParentDirectory`, `ControlCharacter`, `ReservedCharacter`) | `CLI001` usage error, exit `2` |
| `ValidateTarget` `ExistingTarget` (`targetState == File`) | `CLI003` output-exists, exit `3` |
| `ValidateTarget` `IoFailure` (`targetState == Directory`) or a `Directory.CreateDirectory` failure | `CLI002` I/O, exit `3` |
| `ValidateTarget` `InvalidTargetPath` | **not produced by the CLI.** GUI-only: an empty or invalid directory chosen in the picker shows an inline message and opens the picker again |

The CLI continues to call `File.Exists`/`Directory.Exists`/`Directory.CreateDirectory` itself and passes only
the resulting `ProjectTargetState` into the pure helper; the GUI does the same through its picker. The CLI's
**empty or syntactically invalid output directory** keeps its existing behaviour — `CLI002` I/O, exit `3`, pinned
by `AnEmptyOutputDirectoryIsCli002AndExitThree` — because the CLI validates and normalises its output path before
it calls the helper. The helper's `InvalidTargetPath` is therefore reserved for the GUI and never changes a CLI
code, exit code, or message. `GetMessage` owns the text for the failures the helper produces, and new
`Mocassin.Cli.Tests` cases pin each of those messages so the extraction cannot reword the CLI; the CLI's existing
empty-output-directory message stays unchanged and stays pinned by `AnEmptyOutputDirectoryIsCli002AndExitThree`.
The helper is listed in `Mocassin.Packages'` `PublicAPI.Unshipped.txt`; no baseline refresh is expected. No other
library change is allowed in 23A; if one appears necessary, the step agent stops and records an amendment.

### 13. Dependency and interface checks (parallel track)

| Capability | Library API | What the shell owns |
| --- | --- | --- |
| Load/save/migrate/lock | `ProjectStore` **with `ProjectStoreOptions { ValidateOnLoad = false }`**, `ProjectLoadResult` (project + `Diagnostics` + `FailureKind`), `ProjectSaveResult`, `ProjectLockResult`, `ProjectFileLock` | prompts, paths, recent list, load-diagnostic retention, file stamps, read-only mode |
| Project value | `Project`, `ProjectDocument`, `ProjectHasher`, `ProjectJson` | display, save-point bookkeeping, JSON view |
| Templates | `ProjectTemplateFactory`, `ProjectTemplate`, `ProjectTemplateKind` | the New dialog, name/path validation via `ProjectPathRules`, recent/list wiring |
| Settings/logging | `TimeProvider`, `Microsoft.Extensions.*`, Serilog | the whole settings and logging policy |

The shell does not re-implement these: no second JSON reader/writer, no second template definition, no second
lock, no second hash, no second `ProjectStore` wrapper interface. 23A **does not** construct or call
`ProjectValidationPipeline`; that is 23B's contract (M-1).

**23B is the only successor step for the 23A seams; Steps 24–28 consume them too.**

| Step | Adds | 23A seam it uses |
| --- | --- | --- |
| 23B | edit pipeline, validation surface, dirty/autosave/recovery | `ProjectSession`/`LoadDiagnostics`, `DocumentViewModel`, `INavigationService`, `IExplorerNodeProvider`, `IConsoleChannelProvider`, `IStatusBarItemProvider`, the settings model, `ProjectDocument` |
| 24 | particle/structure/transition editors | `INavigationTargetProvider`, `IExplorerNodeProvider`, `IInspectorContributor`, `DocumentViewModel`, `IProjectSessionService` |
| 25 | energy/lattice/simulation editors | same seams; new domains |
| 26 | parameterization/job-template editors, build UI | `IMenuItemProvider`, `IConsoleChannelProvider`, `IStatusBarItemProvider`, `IProjectSessionService` |
| 27 | 3D viewer and picking | `IInspectorContributor`, document host, `ProjectSession` |
| 28 | run monitoring, charts, MMCFE UI | `IConsoleChannelProvider`, `IStatusBarItemProvider`, `IMenuItemProvider` |

Rules: 23A code references no `Compilation`/`Results` API, no `Mocassin.Cli`, and no solver package; no
provider is registered for a domain 23A does not implement; the shell is usable with zero editors (create, open,
inspect, save, reload, close); a later step adds a provider and a registration call without editing the shell.

### 14. Documentation plan

These pages are created during implementation; paths are code so this page does not link to pages that do not
exist. 23B completes the sections marked as 23B; 23A must not ship a section for a 23B capability.

| Page | 23A content |
| --- | --- |
| `docs/getting-started/installation.md` | prerequisites, per-OS install, first launch, data-dir location, GPU-free operation |
| `docs/getting-started/quickstart.md` | create/open/inspect/save/close/reopen, recents, locks, read-only mode, external-change reload |
| `docs/getting-started/first-project.md` | templates, workspace, tabs, settings/theme, project lifecycle |
| `docs/user-guide/projects.md` | lifecycle reference: new/open/save/save-as/close/reload, recents, locks, read-only, external changes, settings |
| `docs/development/ui-architecture.md` | composition root, dependency direction and the I/O boundary, document/navigation model, state ownership, seams, settings format |
| `docs/troubleshooting/installation.md` | startup failures, missing display/Wayland nuance, unwritable data dir, corrupt settings, locked project |
| `docs/reference/file-layout.md` | app-data layout, `Mocassin.Settings.json` v1 field table and example |
| `docs/development/index.md`, `docs/user-guide/index.md`, `docs/troubleshooting/index.md`, `zensical.toml` | navigation entries for the pages that exist at 23A closeout |
| `docs/development/testing-and-parity.md` | harness and PR-fast/PR-medium split for 23A |

No page may describe editing, validation, autosave, recovery, a model editor, build UI, 3D, or run monitoring.

### 15. Test plan

Harness: `Mocassin.App.Tests` uses Avalonia headless for view tests and plain xUnit for view-model tests. Real
objects everywhere: real `ProjectStore`, real temporary directories, real `Project` values, the real dialog
overlay, and the real `AppSettingsStore`/`ProjectFileProbe` against a temporary data directory where the test is
about file behaviour. Permitted fakes are exactly the true boundaries: `IDialogService` (view-model tests only),
`IStorageService`, `IUiDispatcher`, `TimeProvider`, and an in-memory `IAppSettingsStore` for non-persistence
tests. No fake `Project`, no fake JSON, no fake store.

| Area | Tier | Assertion |
| --- | --- | --- |
| Startup/composition | PR-fast | `AddMocassinApp` resolves every registered service; the shell view model constructs and renders headless; duplicate navigation-provider registration fails fast; the registered `ProjectStore` has `ValidateOnLoad == false` |
| Architecture | PR-fast | **Direct** `.csproj` references only: `Mocassin.App.ViewModels` has no direct Avalonia/`Mocassin.App.Views` reference; `Mocassin.App` has no direct `Mocassin.Cli`/`Mocassin.Compilation` reference (the `App → Packages → Compilation` transitive edge is allowed). Exact I/O token test with whole-identifier lookbehind, `using static` detection, and a positive control (§1/n-15) |
| Window placement | PR-fast | A saved placement that intersects no current screen resolves to the primary screen; one that intersects is kept (V-5) |
| New/open/save/close | PR-fast | New from both templates creates a file the store reopens; save → close → open is content-identical; save-as migrates the lock; an already-open path activates; **New** refuses an existing target, while Save As to an existing file follows the picker confirmation |
| Reload/external change | PR-fast | A changed file prompts Reload; a deleted file offers Save As/Close; no silent overwrite; reload replaces the document and clears the old value |
| Locks/read-only | PR-fast | A second session opens read-only with a banner; Save is disabled; save-as acquires the new lock; a stale lock is reclaimed per store policy |
| Recent files | PR-fast | Order, de-duplication, missing-file disable/remove, persistence across restart |
| Settings | PR-fast | Every §6.1 key round-trips through the real `AppSettingsStore` and takes its documented default when missing; a wrong type/unknown enum/out-of-range value falls back to the default with a warning; unknown keys are ignored; a future major is rejected without overwriting; migration is idempotent; the 4 MiB cap rejects an oversized document; a failed write leaves the previous file intact; last directories persist and update |
| Name/path rules | PR-fast | GUI and CLI produce the same message and decision for every rejected name/path case via `ProjectPathRules` |
| Navigation | PR-fast | Open/reuse by key, activate, close, back/forward, unknown domain notice, provider registration per project scope |
| Command catalog/palette | PR-fast | Every shell command appears with its platform gesture; `CanExecute` disables menu and palette together; `primary` resolves to Ctrl/Meta |
| Dialogs | PR-fast | Real overlay flow: confirm/choose/prompt/error; focus captured and restored; the scripted dialog fake is used only in view-model tests |
| Accessibility | PR-fast (shell), PR-medium (full tree) | Names/roles on primary controls, no icon-only control without a name, tab order matches visual order, status not colour-only |
| Headless views | PR-medium | Shell, tab strip, explorer, settings dialog render and respond to keyboard input |
| Screenshots | PR-medium (Linux) | Light/dark/high-contrast shell baselines with a documented tolerance and a regeneration command |
| Platform gate | manual (Windows, macOS) | Headless suite plus a short checklist: start, new/open/save, theme switch, keyboard-only pass, crash dialog |
| Manual screen reader | release checklist | NVDA (Windows), Orca (Linux), VoiceOver (macOS) — recorded, not automated |

Screenshot policy: baselines are generated and compared on `ubuntu-latest` only with a fixed headless render
scale; Windows/macOS screenshots are manual platform-gate evidence and are not pixel-compared (text rendering
differs per platform). Tier declaration uses `[Trait("Tier", "PR-fast"|"PR-medium")]`.

### 16. Performance expectations (non-gating)

No absolute wall-clock duration is asserted in a PR-fast test; budgets are design targets recorded as evidence
on the benchmark runner, per `03-quality-strategy.md` §13.

| Scenario | Design budget | How observed | Gated? |
| --- | --- | --- | --- |
| Cold start (empty session, warm cache) | < 2 s | manual release measurement with OS/CPU | no |
| Open the ceria sample | < 500 ms | recorded measurement at feature 4 closeout | no |
| Save the ceria sample | < 500 ms | same | no |
| Shell render (headless) | < 100 ms | generous hang-guard timeout in the headless test | hang guard only |

PR-fast asserts functionality, ordering, and asynchrony. No benchmark harness is added.

### 17. Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Avalonia 12.1.6 API/behaviour uncertainty (headless screenshots, high contrast, storage provider) | Verification items V-1…V-5; each has a fallback and is resolved in the feature that needs it |
| UI thread violations | One dispatcher boundary, no blocking waits, no background project work in 23A, arch test |
| Large project tree/serialization | Project-level tree only; JSON view on demand; no statistics in 23A |
| Settings corrupt/forward-versioned file | Never overwrite on read failure; banner + save-as-new; defaults with logged warnings |
| Platform differences (paths, case, primary modifier, fonts) | `Path.Combine`/`SpecialFolder` in App, explicit case-sensitivity rule, `OperatingSystem` checks, manual platform gate |
| Parallel track: Step 13 changes shared files | This design touches planning docs only; the implementor merges `main` before starting and re-reads the library surfaces |
| Scope creep into 23B/24 | Non-goals list; no placeholder editors or panels; 23B owns every editing/validation/recovery task |

### 18. Complexity class and split decision

| Dimension | Class | Evidence |
| --- | --- | --- |
| Contracts, formats, and public surfaces | **Medium** | One app-local settings file (single consumer, not in the manifest) and one additive Packages helper. No change to `.mocprj`/`.msl`/`.mcs`, signatures, CLI reports/exit codes, or diagnostic codes |
| New dependencies | **Medium** | No new production package (Avalonia, CommunityToolkit, Microsoft.Extensions, Serilog already pinned); two test-only first-party Avalonia headless packages; possible explicit `Avalonia.Skia` (V-1) |
| Cross-step interfaces | **High** | The document model, navigation/explorer/inspector/menu/console/status seams, session service, and dialog/picker contracts are consumed by 23B and Steps 24–28 |
| Irreversibility | **Medium** | Settings format is migratable; the seam shape is the least reversible part and is covered by the cross-step row; no scientific or persisted-project contract change |
| Test-matrix breadth and failure modes | **High** | Headless view-model, headless view, screenshot, accessibility-tree, manual platform, file-I/O lifecycle, settings migration, and platform path/gesture failure modes |
| Independent acceptance stories | **Medium** | One coherent story: a usable, keyboard-accessible workspace that creates/opens/inspects/saves/reloads/closes projects. Settings, theme, and a11y are properties of that story, not separately approvable deliverables |
| Evidence size for a fresh auditor | **Medium** | Five features, one new application project plus the initial harness and screenshot baselines, one library change, six documentation pages; an auditor can read the diff, tests, and evidence in one sitting |

**Class: High** (highest dimension: cross-step interfaces and test-matrix breadth). *Justification:* 23A
introduces the interface surface that 23B and Steps 24–28 consume and spans a new UI test matrix with
platform-specific failure modes; its contracts, dependencies, irreversibility, acceptance-story count, and
evidence size are only Medium because the settings file is app-local, no production package is added, and the
outcome is one coherent workspace story.

**Owner acceptance is required** for this High step at the start gate, per [`04-roadmap.md`](../04-roadmap.md)
§3.1. **No further split is proposed:** splitting host/workspace from settings/lifecycle would leave either a
shell that cannot open a project or settings/lifecycle code **without a host to run in**; both halves would be
unusable alone, so the extra step would add overhead without reducing the audit surface.

### 19. Decisions recorded and owner input

| Decision | Record |
| --- | --- |
| 23A is the workspace/lifecycle half of the split Step 23; it contains no editing, validation, autosave, or recovery task | This page; [`04-roadmap.md`](../04-roadmap.md) §3.1 |
| `ProjectStore` is registered with `ValidateOnLoad = false`; the pipeline and the M-1 contract are 23B's | This page §1/§5; [`05-open-decisions.md`](../05-open-decisions.md) |
| The recovery types (`IRecoveryStore`, `CrashRecoveryWriter`, `recovery/` writes) are 23B; 23A's crash dialog has no recovery action | This page §1/§8 |
| Visibility: existing markers/`Program` stay public; 23A adds public `App : Application` (not ApiCompat-checked); all other new types are `internal` | This page §1/§10; [`01-target-architecture.md`](../01-target-architecture.md) §3.2 |
| The I/O token test is a bounded tripwire with whole-identifier matching, `using static` detection, a positive control, and a documented limitation | This page §1/§15; [`code-structure.md`](../../development/code-structure.md) §2.3 |
| `mocassin.settings` v1 is application-local, versioned, migratable, not in `contracts/manifest.json`; the normative field table and example are §6.1; `autosave`/`session` are added additively by 23B with no schema bump | This page §6.1; [`docs/reference/file-layout.md`](../../reference/file-layout.md) |
| One global tab strip with project-scoped document keys and a project switcher | This page §3/§5 |
| Dialogs are an in-shell overlay; no `MsBox.Avalonia`, no new control package | This page §7; ADR 0003 |
| App-data root `<LocalApplicationData>/Mocassin` with `logs/`, `cache/`, `recovery/` (reserved); override via `--data-dir`/`MOCASSIN_DATA_DIR` | This page §6 |
| No splash window; the welcome document covers the empty state | This page §2 |
| `Model`/`Build`/`Run` menus appear only when a later step contributes items | This page §4 |
| Screenshot pixel comparison is Ubuntu-only; other platforms are manual evidence | This page §15 |
| One additive library change (`ProjectPathRules` + CLI refactor) and no others | This page §12 |
| Complexity class High with the dimension table; owner acceptance requested; no further split (settings/lifecycle needs the host) | This page §18 |
| Closeout identity is `step-23a` in both the evidence `step` field and the tag; the `step-NN[a-z]` checker/README/template support is a prerequisite landing before closeout; no `step-23` alias | This page §20 |
| `ProjectPathRules` splits pure name/directory rules from a caller-supplied `ProjectTargetState`; `InvalidTargetPath` is GUI-only, and the CLI keeps its existing empty-output-directory `CLI002`/3 behaviour and messages | This page §12 |

**Verification items** (confirmed by the named feature; not guessed here):

| ID | Question | Feature | Fallback |
| --- | --- | --- | --- |
| V-1 | Exact Avalonia 12.1.6 `AppBuilder`/headless setup and whether `UseSkia()` is available for screenshot rendering | 1/5 | Add `Avalonia.Skia` at the pinned version to **`Mocassin.App.Tests` only** (test-only; Skia is reachable through `Avalonia.Desktop` but a headless screenshot host should not rely on that), and record it as a test-only dependency. Production stays on the already-pinned Avalonia packages; no production package is added |
| V-2 | Whether Avalonia 12.1.6 exposes a platform high-contrast variant/resource dictionary | 3 | Ship the app-owned `HighContrast.axaml` and select it from settings only |
| V-3 | Whether the pinned `Serilog.Sinks.File` supports `retainedFileSizeLimitBytes` | 1 | Implement `LogRetention` as an explicit startup sweep |
| V-4 | `IStorageProvider` "save as" behaviour per OS (filter, overwrite confirmation) | 4 | Own filtering and confirmation in `IStorageService` and document the per-OS behaviour |
| V-5 | Whether a saved window placement can be validated headlessly against a real monitor topology | 1 | Test the pure `WindowPlacementResolver` with synthetic screen rectangles; record the real multi-monitor check as manual platform evidence |

**New ADRs:** none. ADR 0003/0006/0008 stand. **Owner input required at the start gate:** the complexity-class
acceptance (§18), the five-feature list, and acknowledgement of the two test-only Avalonia headless packages,
the possible `Avalonia.Skia` reference, and the single additive library change. No public format, manifest, or
CLI contract changes.

### 20. Closeout and audit plan

- **Feature 5 is the only closeout feature.** No other feature edits the step page status, parity matrix, or
  closeout evidence; if a fix is needed after the freeze it goes through a new feature branch and a new audit.
- **Before the freeze:** all five features are squash-merged; current `main` is merged into the step branch;
  `./tools/verify.sh all --require` passes on the integration tree.
- **Closeout CI:** the only step-branch CI run is one `CI` dispatch (Ubuntu) after the final merge of `main`. Because
  23A introduces GUI code, which is a platform-specific trigger in `AGENTS.md` §3.13, the closeout evidence also
  includes one `platforms=windows` dispatch; macOS remains a manual platform gate. Record the run URLs/IDs and the
  full `head_sha`.
- **Closeout identity (M-1).** The evidence file is `docs/planning/closeout/step-23a.json` and its `step` field is
  exactly `"step-23a"`; the tag is `step-23a` and the audit record is `docs/planning/audits/step-23a.md`. There is
  **no** `step-23` alias, and the superseded unsplit `step-23` identity stays unused.
- **Tooling prerequisite (M-1).** `tools/check_closeout_evidence.py` currently accepts only `^step-\d{2}$`
  (`STEP_RE`) and rejects a letter suffix. A separate tooling change extends the checker, its tests, and the
  closeout README/template to `^step-\d{2}[a-z]$` with a letter-suffix positive case. That change must land
  **before either 23A or 23B closeout**; until it does, `python tools/check_closeout_evidence.py` cannot accept
  `step-23a` and the closeout gate must not be claimed. The design does not work around it.
- **Evidence:** `docs/planning/closeout/step-23a.json`, created from the template, binds the audited commit, the
  closeout CI `head_sha`, the `main` SHA at freeze, and the merge candidate; `python
  tools/check_closeout_evidence.py` must pass once the tooling prerequisite has landed. The platform-gate and
  screenshot results are cited from the step page feature log.
- **Audit:** a fresh out-of-context auditor reviews the frozen branch and writes `docs/planning/audits/step-23a.md`
  (created on demand from the template — no pre-seeded record). The auditor commits only that file; the step
  agent adds resolutions and status afterwards without altering reviewer text.
- **Merge and tag:** only after a `Passed` audit and owner approval, merge with `--no-ff` and tag `step-23a` after
  the automatic `main` CI run is green.
- **Dashboard:** `docs/planning/status.md` is step-agent-owned. When the step starts, the step agent replaces the
  superseded `23` row with **`23a`** and **`23b`** rows — lowercase, matching `check_process_consistency.py`'s
  `\d{2}[a-z]?` step-id parser — and records `23a` as `In progress`; the planner does not edit the dashboard.
  Page titles and prose keep the `23A`/`23B` spelling.

## Implementation tasks

- [ ] **1 — Host and boundaries.** App project (`Program`, `App`, `ViewLocator`), `AppHost`, registration,
      startup options, logging + retention, exception handlers, shell window/regions/menu/status, dialog overlay,
      pickers, console host with the Application channel, notifications, progress.
- [ ] **2 — Workspace.** Document model, tab strip, welcome, Project Overview (read-only) and JSON documents,
      project switcher/explorer, navigation + history + registry, command catalog, menus, status items, gesture
      descriptors, palette, cheat sheet.
- [ ] **3 — Settings, theming, accessibility.** Settings v1 model/validator/migrations/context, the App-layer
      store, app-data layout, settings dialog, theme/density/font-scale/high-contrast/reduce-motion applier,
      persisted directories/log level, accessibility metadata.
- [ ] **4 — Project lifecycle.** `ProjectSessionService`/`ProjectSession` with the `ValidateOnLoad = false` store
      and retained load diagnostics; new via `ProjectPathRules`; open/activate/save/save-as/close/reload;
      recents; locks/read-only; external-change detection; multi-project workspace; window close.
- [ ] **5 — Tests, docs, closeout.** Harness and test support, view-model/view tests, a11y and screenshots,
      platform checklist, the 23A documentation column, parity/status updates, `closeout/step-23a.json`, audit.
- [ ] Add the `ProjectPathRules` change with its tests (including CLI message pinning) and the
      `PublicAPI.Unshipped.txt` entry; verify no App `PublicAPI.*`/baseline changes.
- [ ] Run `python tools/check_closeout_evidence.py` after the `step-NN[a-z]` checker/template/README change has
      landed, and request the audit (`step-23a`).

## Acceptance criteria

- [ ] The projects build on all three target platforms; the headless suite starts the shell on Ubuntu
      automatically; the Windows/macOS headless suite and the manual real-window checklist are recorded.
- [ ] The app starts with a welcome document; the shell regions, menu bar, and status bar render.
- [ ] A project can be created from both embedded templates, opened, inspected, saved, closed, and reopened;
      reopening yields identical content through the real store.
- [ ] Save-as works and migrates the lock; an already-open path activates its session; **New** refuses an
      existing target with the CLI's message, while Save As to an existing file follows the picker confirmation.
- [ ] The composed `ProjectStore` has `ValidateOnLoad = false` and retains `ProjectLoadResult.Diagnostics`; no
      validation pipeline is registered or called in 23A.
- [ ] Reload and external-change detection behave as designed; no silent overwrite is possible.
- [ ] Recent files, locks, read-only mode, and the project switcher behave as designed.
- [ ] Settings round-trip through the real store, validate, migrate, ignore unknown fields, reject a future
      major without overwriting, survive a failed/oversized write, and persist last directories.
- [ ] Every primary action is reachable by keyboard alone; the palette lists the shell commands with their
      platform gestures; `Model`/`Build`/`Run` menus do not appear.
- [ ] Accessibility names/roles are present on the primary controls; the accessibility-tree assertions pass.
- [ ] No unhandled exception kills the process silently; the crash dialog and log entry are verified.
- [ ] `./tools/verify.sh all --require` passes, the strict docs build passes, and `closeout/step-23a.json` with
      `"step": "step-23a"` validates once the `step-NN[a-z]` checker support has landed.
- [ ] Documentation written (§14), and no page claims a 23B or later capability.

## Documentation

See §14. The pages are created during implementation; this design change does not link to pages that do not
exist. [`docs/development/code-structure.md`](../../development/code-structure.md) §2.3 and the architecture page
gain the 23A layout in this design change.

## Tests

See §15. Required evidence: real-object lifecycle/reload/lock/settings tests, the architecture and token tests,
navigation, palette, dialog-overlay, accessibility, Linux screenshot baselines, and the manual platform
checklist.

## Risks and mitigations

See §17.

## Done when

The Avalonia workspace creates, opens, inspects, saves, save-as, reloads, and closes projects with correct locks,
recent files, external-change handling, versioned settings, theming and accessibility, structured logging, and a
crash boundary — with no editing, validation, autosave, or recovery — and every acceptance criterion, test tier,
documentation page, parity/status update, closeout evidence, and audit is complete for 23A alone.
