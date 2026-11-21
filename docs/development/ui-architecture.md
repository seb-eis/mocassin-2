# UI Architecture

The Avalonia workspace is split into three projects so that view models remain framework-free and the
composition root is the only place that resolves services.

## Project split

```text
Mocassin.App  ──►  Mocassin.App.Views  ──►  Mocassin.App.ViewModels  ──►  Packages / Results / Contracts / Core
      │
      ├──► Avalonia, Microsoft.Extensions.*, Serilog
      └──► Mocassin.Rendering (not used by the Step 23A shell)
```

| Project | Owns | Must not own |
| --- | --- | --- |
| `Mocassin.App` | `Program`, `App`, `ViewLocator`, host composition, startup options, app paths, logging, crash boundary, settings store, recovery store, crash-recovery writer, autosave timer, and project file probe | View models, views, scientific logic |
| `Mocassin.App.ViewModels` | Shell/region/document view models, navigation, project sessions, edit/validation/recovery coordination, autosave scheduler, settings model and pure logic, dialogs/consoles/commands, theme contracts | Avalonia types, views, `System.IO`, `Environment`, `Application`, resource dictionaries |
| `Mocassin.App.Views` | Shell and region views, Problems/recovery views, dialog overlay, settings view, console/palette/cheat-sheet views, theme applier, pickers, dispatcher, key-binding translation | Library calls, scientific logic, persistence decisions |

## Composition root

`AppHost` builds a `HostApplicationBuilder`, registers services explicitly, builds the provider, and owns
disposal. Registration is enumerable in `AppServiceRegistration`:

- `AddMocassinApp` — lifetime, crash boundary, settings environment, project file probe, project store,
  validation pipeline, project session service, recovery store, autosave scheduler/timer, crash-recovery writer,
  appearance platform.
- `AddMocassinViewModels` — console + Problems channel, validation/autosave status items, notifications,
  progress, command catalog, workspace, document collection, navigation, explorer, settings dialog.
- `AddMocassinViews` — picker, clipboard, dialog service, Problems/recovery/crash views, theme applier.
- `AddMocassinShell` — shell view model and main window.

There is no assembly scanning, no service locator, and no reflection-based discovery. Only `Program`/`App`
resolve the provider; every view model receives explicit constructor arguments.

## The I/O boundary

View models may call public library APIs that perform I/O — `ProjectStore`, `ProjectTemplateFactory`,
`ProjectValidationPipeline` — because those are the contracts the CLI also uses. App-local state goes through
ViewModels contracts implemented in `Mocassin.App`:

| Concern | Contract | Implementation |
| --- | --- | --- |
| Settings file | `IAppSettingsStore`, `ISettingsService` | `AppSettingsStore` |
| Recovery files | `IRecoveryStore`, `ICrashRecoveryWriter` | `RecoveryStore`, `CrashRecoveryWriter` |
| Project file facts | `IProjectFileProbe` | `ProjectFileProbe` |
| App-data paths | `AppPaths` value | `AppPaths.Resolve` in `Mocassin.App` |
| Theme | `IThemeApplier` | `AvaloniaThemeApplier` |
| Picker, clipboard, dispatcher | `IStorageService`, `IClipboardService`, `IUiDispatcher` | Avalonia view-layer implementations |

A source-token architecture test scans `Mocassin.App.ViewModels/**/*.cs` and fails on file-system, environment,
Avalonia, or resource-dictionary tokens. The scan strips comments and literals, uses whole-identifier matching,
checks `using static`, and has a positive control. It is a tripwire, not a compiler guarantee; the direct
`.csproj` reference check and review remain the primary enforcement.

## Shell and documents

`ShellViewModel` owns the document collection, workspace, navigation, explorer, console, command catalog,
command palette, shortcut cheat sheet, status bar, progress state, and settings dialog state. The welcome document lists recent projects, including missing entries, and the shell records Save As paths.
The document key is `(Kind, ProjectScopeId)`:

- `welcome`
- `project.overview`
- `project.json`

Later steps add `model.*` and other document kinds without changing the key shape. The tab strip is one global
collection; the project switcher activates a project's last document. The welcome document closes when the first
real document opens. The application `ViewLocator` claims only classes named `*ViewModel` in the App view-model
assembly and maps them to the matching view; enums, settings records, and other model objects keep their ordinary
content rendering.

## Navigation and commands

`INavigationService` opens documents and navigates `ModelPath` targets through registered
`INavigationTargetProvider` instances. History is a bounded 100-entry back/forward stack. An unknown domain
writes an Application-console notice and never throws; this is what lets a Step 07 diagnostic with a model path
remain harmless until the corresponding provider is registered.

Commands are explicit `ShellCommand` entries with a stable id, category, optional menu, framework-neutral
gesture, and `ICommand`. The palette and cheat sheet read the same catalog. `Primary` maps to Control on
Windows/Linux and Command on macOS.

## Project sessions

`ProjectSessionService` is the lifecycle state machine. `ProjectStore` is registered with
`ValidateOnLoad = false`; a session loads once, retains the store diagnostics, and calls the shared validation
pipeline once with those diagnostics. The session owns the `ProjectDocument`, lock, last-written value, file
stamp, load diagnostics, and validation result. Services never prompt; the shell owns dialogs and pickers.

## Edit pipeline and undo/redo

`ProjectEditCoordinator` is the single write path for a project. It wraps the session-owned
`ProjectDocument`, delegates every mutation to `ProjectEditor`/`ProjectValidator`, records one bounded undo
entry per committed field, and raises `ProjectChanged` (`Edited`, `Undo`, `Redo`, `Saved`, `Replaced`) and
`EditRejected`. `ProjectDocument.MaxUndoDepth` defaults to unbounded (`0`) for the library; the GUI constructs
its documents with 200. Metadata editing is one `Apply` per committed field, not per keystroke.

The Overview metadata form uses `ProjectPathRules` for the project name before applying the edit. A structural
validation failure leaves the project, stacks, save point, and dirty state unchanged and shows the inline error;
the Problems channel receives the same diagnostics with source `edit`. A read-only session returns the app-level
`EditApplyOutcome.ReadOnly` message and never emits a diagnostic code.

Undo/redo are exposed in the Edit menu (`Ctrl+Z`/`Ctrl+Y`), follow the active project, and are disabled for a
read-only session or an empty stack. The save point is content-based: save calls the no-arg
`ProjectDocument.MarkSaved()` so save → undo → redo remains clean.

## Validation coordinator

`ProjectSession` owns one `ValidationCoordinator`. It is seeded from the single `ProjectSession.Validation`
result that 23A produced at load/create/reload, so opening never runs a second validation. After a content
change it debounces 300 ms on a `TimeProvider`, runs the shared `ProjectValidationPipeline` off the UI thread on
an immutable snapshot, and applies the result only when the captured document version and snapshot still match.
A newer edit cancels the previous run; a late result is discarded. Statistics are an explicit Overview action,
cached per immutable snapshot, and are never requested on open.

The status bar shows a text glyph and severity plus the validation text. The Problems console channel follows
the active project, shows severity/code/message/path/source, supports severity/code/text/source filters and
sorting, navigates via `INavigationService` on double-click/Enter, opens catalog help anchors, and offers the
`Related` paths. Diagnostics are never invented in the UI; an unknown code is rendered unlinked.

## Recovery, autosave, and the session gate

`ProjectSession` owns a `SemaphoreSlim` gate and an `IsClosing` flag. Save, autosave, close, reload, and recovery
await the gate; an autosave tick uses a non-blocking wait and skips busy or closing sessions. `CloseAsync` and
`ReloadAsync` take a `discardChanges` guard and return `UnsavedChanges` without closing when a dirty session has
not been confirmed; `IsClosing` is cleared on cancel, `UnsavedChanges`, and failure.

`AutosaveScheduler` is tick-driven through `TimeProvider`; `AutosaveTimer` in `Mocassin.App` owns the timer and
the UI dispatch. A due writable dirty session writes the `<file>.autosave` sidecar; a failed sidecar write falls
back to `IRecoveryStore` in `recovery/`, and both the failure and the fallback are logged. The scheduler returns
to the UI context after each asynchronous write and marshals the status item and `MarkAutosaved` through
`IUiDispatcher`, so no view-bound state or document mutation runs on a thread-pool continuation. Autosave never
clears dirty state or moves the save point. A
content change that returns to the save point, a clean close, and an explicit discard remove the sidecar and
recovery snapshot; a deferred `PendingRecovery` candidate is preserved across a clean close until Recover or
Discard. A successful save removes the snapshots only when no candidate is pending. The first accepted content
edit clears a deferred candidate; a no-op same-hash edit does not. Snapshot cleanup is triggered on the UI
thread and keeps its state checks on that thread; the file deletion itself stays asynchronous.

`IRecoveryStore`/`RecoveryStore` own the `recovery/` snapshots and `mocassin.recovery` manifests. Recovery
candidates are compared with the saved file using `ProjectDocument.IsDirty` after normalizing `modifiedUtc`
(no ViewModels hashing), so a timestamp-only snapshot is not offered. The newest content-different sidecar or
manifest candidate wins. A timestamp tie is deterministic: the adjacent `.autosave` sidecar is preferred over a
`recovery/` manifest, then the ordinal snapshot path breaks any remaining tie. The seven-day retention sweep
applies only to `recovery/`; a `.autosave` sidecar next to a user project is never swept (m-14 accepts that a
deferred `recovery/` candidate can expire).

`ShellLifecycleCoordinator` owns the Save/Discard/Cancel decisions, clean-exit session state, and the unclean-exit
restore offer. A window close is two-phase: it first collects a decision for every dirty project and aborts
before saving or closing anything if any prompt returns Cancel; only then does it apply saves and close the
sessions. Cancel therefore closes no project and deletes no sidecar. `ShellProjectOperations` owns
open/close/reload result handling, recovery prompts, and navigation. A clean window close closes sessions,
writes the `session` settings block, and sets `cleanExit = true`; a cancel leaves it false. The restore offer
reads the session block captured before a `--project` startup open, so that open cannot manufacture an
"unexpected exit" prompt or replace the list being offered. The crash path captures dirty sessions on the UI
thread and `CrashRecoveryWriter` writes them asynchronously with a 2 s budget; the dead-UI path writes no
recovery files.

## Settings and logging

Settings are read before logging is configured so the persisted log level selects the minimum level, with
`--log-level` as a run override. The settings file is application-local and not part of
`contracts/manifest.json`. Theme, density, font-scale, contrast, and motion are applied through
`IThemeApplier` when settings load or change.

The additive `autosave` and `session` blocks stay in the same v1 document with no schema bump. `autosave`
controls the periodic sidecar; `session` records open projects, the active project, `cleanExit`, and the write
time. Older 23A documents load with defaults. The blocks are not in the contract manifest and have no
cross-language fixtures.

## Testing

`Mocassin.App.Tests` uses Avalonia headless rendering, real temporary files, the real project store, the real
validation pipeline, and the real dialog overlay. Fakes are limited to the UI/OS boundaries: dialogs, pickers,
clipboard, dispatcher, time, and in-memory settings stores. The solution uses xunit 2; the Avalonia 12
`Avalonia.Headless.XUnit` package targets xunit v3 and is therefore not used. The suite drives the first-party
`HeadlessUnitTestSession` directly instead.
