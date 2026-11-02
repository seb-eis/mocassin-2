# Step 23 — Avalonia Shell and Project Lifecycle

**Phase:** F. GUI  
**Depends on:** 01
**Delivers:** The Avalonia application shell, dependency injection, navigation, document tabs, project
open/save/close, dirty tracking, undo/redo, settings, logging, and crash recovery.

## Goal

Build the application frame that every editor plugs into. It must feel like a modern scientific IDE: a project
tree, document tabs, an inspector, a console, a status bar, keyboard navigation, and reliable project
persistence.

## Original reference

- WPF `MainWindow` with a fixed grid: menu bar, solution explorer, work tabs, console tabs, status bar.
- Hand-written MVVM, service-locator `IProjectAppControl`, views stored in view models, `MessageBox` dialogs,
  `Console.WriteLine` logging.
- Project lifecycle: open/create/close/save; unsaved-change prompt; multiple projects per `.mocprj` database.
- No undo/redo, no autosave, no crash recovery, no structured logging.
- Detached `DbSet.Local` copy caused project duplicate/delete to miss the database.
- Settings in an XML file under `Documents/Mocassin`; symmetry DB path hard-coded with Windows separators.

## Design

### 1. Application composition

```csharp
public static class Program
{
    public static int Main(string[] args);
}

public sealed class App : Application
{
    public override void Initialize();
    public override void OnFrameworkInitializationCompleted();
}
```

- `Microsoft.Extensions.Hosting` builds the service provider.
- Services:
  - `IProjectSessionService` — open projects, active project, recent files, save/close.
  - `IProjectStore` — load/save/lock (step 01).
  - `IBuildService` — build orchestration (step 12).
  - `ISimulationService` — launch/monitor solver (step 28).
  - `IDialogService` — message boxes, confirmations, progress dialogs.
  - `IStorageService` — file/folder pickers via Avalonia `IStorageProvider`.
  - `ISettingsService` — app settings (theme, culture, paths, solver path, autosave).
  - `ILogService` / `ILogger<T>` — Serilog file + console.
  - `INavigationService` — open documents/editors.
  - `IUndoRedoService` — per-project command stacks.
- View models receive explicit interfaces; only `App` resolves the provider.
- `ViewLocator : IDataTemplate` maps view-model types to views; views are never stored in view models.

### 2. Shell layout

```text
┌─────────────────────────────────────────────────────────────────────────────┐
│ Menu: File  Edit  View  Model  Build  Run  Tools  Help                      │
├──────────────┬───────────────────────────────────────────────┬──────────────┤
│ Project      │ Document tabs                                 │ Inspector    │
│ Explorer     │ ┌───────────────────────────────────────────┐ │ (selection)  │
│              │ │ Active editor                             │ │              │
│ - Model      │ │                                           │ │              │
│   Particles  │ │                                           │ │              │
│   Structure  │ │                                           │ │              │
│   ...        │ └───────────────────────────────────────────┘ │              │
├──────────────┤                                               │              │
│ Templates    │                                               │              │
│ - Parameters │                                               │              │
│ - Jobs       │                                               │              │
│ - Builds     │                                               │              │
├──────────────┴───────────────────────────────────────────────┴──────────────┤
│ Console / Problems / Build log tabs                                       │
├─────────────────────────────────────────────────────────────────────────────┤
│ Status: model valid | jobs: 1,000 | solver: idle | memory | progress       │
└─────────────────────────────────────────────────────────────────────────────┘
```

- Use `Grid` + `GridSplitter` for the first version; `Dock.Avalonia` can be added later for floating panes.
- Panels are collapsible; layout persists in app settings.
- Document tabs are `DocumentViewModel` objects with title, icon, dirty flag, close command, and content VM.
- The welcome tab closes when the first document opens.
- Tabs can be reordered; drag-out to a window is deferred.

### 3. Navigation

- `INavigationService.OpenDocument(DocumentViewModel)` selects or creates a tab.
- `OpenObject(ModelPath)` opens the relevant editor and selects the object/property.
- Editors register navigation targets (e.g. `ParticleEditor` handles `particles`).
- Validation diagnostics and 3D selection use `OpenObject`.
- Back/forward navigation history (Alt+Left/Right).
- Command palette (Ctrl+Shift+P) for all commands, with searchable labels and shortcuts.

### 4. Project lifecycle

- `File > New` — choose blank/ceria template, name, location; creates a `.mocprj`.
- `File > Open` — file picker, recent list, drag-and-drop.
- `File > Save` / `Save As` — atomic write from step 01.
- `File > Close` — prompt if dirty; release lock.
- `File > Recent` — persisted list with missing-file handling.
- Workspace: multiple open projects, each with its own document set and undo stack.
- Dirty tracking:
  - `ProjectDocument` compares current project hash to the saved hash;
  - title shows `*`;
  - autosave writes a recovery file every N minutes;
  - on crash, the next start offers recovery.
- Lock file prevents concurrent edits; read-only open is allowed with a banner.
- External modification detection (file timestamp/hash) prompts reload/overwrite.

### 5. Undo/redo

- `IUndoRedoService` with per-project stacks.
- Every editor mutation is a `ProjectChange` with a description and inverse.
- `Ctrl+Z`/`Ctrl+Y` (and platform equivalents).
- Undo/redo updates the project hash, dirty state, validation, and 3D scene.
- Bulk operations (create from template, duplicate, paste, renumber) are single changes.
- The stack is bounded (e.g. 200 entries) and kept across saves; the save point is marked so undo can cross it.

### 6. Settings

- `Mocassin.Settings.json` in the platform app-data directory.
- The file has `format = "mocassin.settings"` and `schemaVersion = 1`; new readers migrate older versions and
  ignore unknown fields. A future major version is rejected without overwriting the file.
- Fields: theme (light/dark/system), accent, culture, recent files, autosave interval, solver path, package
  output directory, results directory, log level, max parallel builds/runs, 3D quality, default units,
  accessibility options, update-check policy (offline only).
- App-data layout: `settings.json`, `logs/`, `recovery/`, `cache/`, `scripts/`. The exact paths are documented
  and never mixed with project or package files.
- Paths use `Environment.SpecialFolder`/`Path.Combine`; no Windows separators.
- Settings validation with fallback to defaults and a log entry.
- Retention defaults: logs 14 days or 100 MB (whichever first), recovery files 7 days; projects, packages,
  results, and job folders are never auto-deleted.
- A settings dialog with reset-to-defaults, "open config folder", and "clear cache/logs".

### 7. Dialogs and notifications

- `IDialogService`:
  - `ConfirmAsync`, `PromptAsync`, `ShowErrorAsync`, `ShowProgressAsync`.
- Use `MsBox.Avalonia` or a custom content dialog; no `MessageBox`.
- File/folder pickers via `IStorageProvider`; remember last directories.
- Toast/notification center for background completions.
- Console tabs:
  - **Problems** — validation diagnostics with navigation.
  - **Build log** — build events.
  - **Run log** — solver stdout/stderr.
- All messages include severity, timestamp, source, and optional action.

### 8. Logging and crash handling

- Serilog to rolling files in app-data `logs/` and console (debug builds).
- Global handlers:
  - `AppDomain.UnhandledException`;
  - `TaskScheduler.UnobservedTaskException`;
  - Avalonia `Dispatcher.UIThread.UnhandledException`.
- On unhandled exception:
  - log with stack trace and project path;
  - show a crash dialog with "open log folder" and "attempt recovery";
  - do not silently kill the process.
- A `doctor`-style diagnostics command in Help.

### 9. Accessibility and keyboard

- Full keyboard navigation for the tree, tabs, grids, and dialogs.
- Accessible names/help text on controls; screen-reader labels for icon buttons.
- High-contrast theme; scalable fonts; UI scales with DPI.
- Shortcuts:
  - Ctrl+N/O/S/Shift+S, Ctrl+W, Ctrl+Z/Y, Ctrl+Shift+P, F5 (build), F6 (run), Ctrl+F (search).
- A shortcuts cheat sheet in Help.

### 10. Headless testing

- `Mocassin.App.Tests` uses Avalonia headless.
- Test view-model commands, navigation, dirty tracking, undo/redo, and settings against real project documents;
  fake only the dialog/picker and clock boundaries. Assert observable project state, not collaborator calls.
- Screenshot tests for the shell in light/dark/high-contrast.
- Accessibility tree assertions for key controls. Target **WCAG 2.2 AA**: keyboard access, focus order,
  contrast, scalable text, names/roles, and no color-only information.
- Manual screen-reader checks with NVDA (Windows), Orca (Linux), and VoiceOver (macOS) in the release checklist.

## Implementation tasks

- [ ] Create the Avalonia app project, `Program`, `App`, DI, and logging.
- [ ] Implement the shell layout, panels, tabs, and status bar.
- [ ] Implement `ViewLocator` and document view models.
- [ ] Implement project session, open/save/close/recent, locks, and autosave.
- [ ] Implement dirty tracking and crash recovery.
- [ ] Implement undo/redo and command palette.
- [ ] Implement settings and the settings dialog.
- [ ] Implement dialogs, file pickers, and notification consoles.
- [ ] Implement keyboard shortcuts and accessibility metadata.
- [ ] Add headless tests and screenshot baselines.

## Acceptance criteria

- [ ] The app starts on Windows, Linux, and macOS with a welcome tab.
- [ ] A project can be created, edited, saved, closed, and reopened without loss.
- [ ] Dirty state, autosave, and crash recovery work.
- [ ] Undo/redo works across editors.
- [ ] Recent files, settings, and locks behave correctly.
- [ ] Keyboard-only navigation can reach every primary action.
- [ ] No unhandled exception kills the process silently.
- [ ] Headless tests pass on all three OSes.
- [ ] Documentation written

## Documentation

At this point, document installation and the GUI shell:

- `docs/getting-started/installation.md`, `quickstart.md`, `first-project.md` — install and first workflow.
- `docs/user-guide/projects.md` — project lifecycle, workspace, undo/redo, settings, recovery.
- `docs/development/ui-architecture.md` — Avalonia composition, MVVM, navigation, documents.
- `docs/troubleshooting/installation.md` — platform/startup issues.

## Tests

| Test | Assertion |
| --- | --- |
| Real-object commands | Lifecycle commands run against real project documents; only dialogs/picker/clock are faked |
| Acceptance | New → edit → save → reopen through the real store yields the same project state |
| Startup | DI resolves, shell renders |
| Project lifecycle | New/open/save/close/reopen |
| Dirty/autosave | Prompt and recovery |
| Undo/redo | Multi-step edits |
| Navigation | Open/reuse/close tabs, history |
| Settings | Persist/validate/defaults; schema version/migration; unknown fields ignored |
| Dialogs | Justified fake dialog/picker boundary; commands assert observable project state |
| Accessibility | Names/roles on key controls |
| Screenshots | Light/dark/high-contrast baselines |

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Avalonia ecosystem gaps | Prefer built-in controls; custom only where needed |
| UI thread violations | Async commands, `Dispatcher.UIThread` only at boundaries |
| Large project tree performance | Virtualized tree, lazy children, debounced updates |
| Undo complexity | Single immutable `Project` + change records |
| Platform differences | CI matrix + screenshots; path abstractions |

## Done when

The Avalonia shell provides a reliable, keyboard-accessible workspace with project lifecycle, undo/redo,
settings, logging, and crash recovery, ready for model editors.
