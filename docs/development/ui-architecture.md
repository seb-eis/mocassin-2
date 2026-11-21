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
| `Mocassin.App` | `Program`, `App`, `ViewLocator`, host composition, startup options, app paths, logging, crash boundary, settings store and project file probe | View models, views, scientific logic |
| `Mocassin.App.ViewModels` | Shell/region/document view models, navigation, project sessions, settings model and pure logic, dialogs/consoles/commands, theme contracts | Avalonia types, views, `System.IO`, `Environment`, `Application`, resource dictionaries |
| `Mocassin.App.Views` | Shell and region views, dialog overlay, settings view, console/palette/cheat-sheet views, theme applier, pickers, dispatcher, key-binding translation | Library calls, scientific logic, persistence decisions |

## Composition root

`AppHost` builds a `HostApplicationBuilder`, registers services explicitly, builds the provider, and owns
disposal. Registration is enumerable in `AppServiceRegistration`:

- `AddMocassinApp` — lifetime, crash boundary, settings environment, project file probe, project store,
  validation pipeline, project session service, appearance platform.
- `AddMocassinViewModels` — console, notifications, progress, command catalog, workspace, document collection,
  navigation, explorer, settings dialog.
- `AddMocassinViews` — picker, clipboard, dialog service, theme applier.
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

## Settings and logging

Settings are read before logging is configured so the persisted log level selects the minimum level, with
`--log-level` as a run override. The settings file is application-local and not part of
`contracts/manifest.json`. Theme, density, font-scale, contrast, and motion are applied through
`IThemeApplier` when settings load or change.

## Testing

`Mocassin.App.Tests` uses Avalonia headless rendering, real temporary files, the real project store, the real
validation pipeline, and the real dialog overlay. Fakes are limited to the UI/OS boundaries: dialogs, pickers,
clipboard, dispatcher, time, and in-memory settings stores. The solution uses xunit 2; the Avalonia 12
`Avalonia.Headless.XUnit` package targets xunit v3 and is therefore not used. The suite drives the first-party
`HeadlessUnitTestSession` directly instead.
