# Step 23B — Editing, Diagnostics, and Recovery

**Phase:** F. GUI
**Depends on:** 23A (workspace and project lifecycle) plus the merged library surfaces of 01, 03–07, and 12.
Not dependent on Steps 08–11 or 13–22.
**Delivers:** The single-project edit pipeline with undo/redo, the validation and Problems surface, dirty state
and save-point handling, autosave, crash recovery, and unclean-exit session restore — on top of the 23A shell.
**Status:** Design frozen for review; **not design-reviewed and not started.** No step branch exists. The
review record is [`reviews/step-23b.md`](../reviews/step-23b.md).

> **Split note.** Step 23 was split into **23A** and **23B** by the owner on 2026-11-20 (complexity class High,
> three independent acceptance stories, oversized audit surface). 23B is the editing/diagnostics/recovery half.
> It needs nothing from 23A beyond delivered code, and 23A contains no 23B task. The superseded unsplit design
> is [`step-23-avalonia-shell-and-project-lifecycle.md`](step-23-avalonia-shell-and-project-lifecycle.md); its
> review record is [`reviews/step-23.md`](../reviews/step-23.md).

## Implementation status

- **Status:** Design ready for review — not started, no step branch
- **Step branch:** —
- **Step CI:** —
- **Main merge:** —
- **Step tag:** —
- **Audit:** Not requested
- **Design review:** **Ready for review** — [`reviews/step-23b.md`](../reviews/step-23b.md), frozen at the
  request commit; the reviewer is not assigned
- **Feature list:** Proposed (four features) — to be approved at the 23B start gate
- **Complexity class:** **High** (cross-step interfaces and test-matrix breadth; §16). **Owner acceptance of the
  class is required at the 23B start gate**; a further split is not proposed (§16).
- **Last updated:** 2026-11-21

| Feature | Branch | Status | Local gates | Step CI | Squash commit |
| --- | --- | --- | --- | --- | --- |
| — | — | — | — | — | — |

### Proposed feature list (to be approved at the 23B start gate)

Each branch is `feature/step-23b-editing-diagnostics-and-recovery/<slug>` from the 23B step branch. The order is
the implementation order; 23B adds no shell, workspace, settings, or lifecycle capability beyond the documented
seam fills in §1.

| # | Feature | Slug | Planned scope | Done when |
| --- | --- | --- | --- | --- |
| 1 | Edit pipeline and undo/redo | `edit-pipeline-and-undo-redo` | `ProjectEditCoordinator` per project over `ProjectDocument`; `Apply`/`Undo`/`Redo`; bounded undo depth; `ProjectChanged` and `EditRejected`; read-only refusal; the Project Overview metadata editor (name/description/author/notes, one undo entry per committed field) | Undo/redo crosses the save point correctly in both directions; a rejected structural edit changes nothing and reports diagnostics; a read-only session refuses every edit; the coordinator is the only write path |
| 2 | Validation surface | `validation-surface` | `ValidationCoordinator` seeded from the session's retained `ProjectSession.Validation`, 300 ms debounce after `ProjectChanged`, stale-result discard, status-bar validity item, Problems channel with severity/code/text/source filters, sorting, help anchors, `Related` navigation, revalidate, edit diagnostics, on-demand statistics in the Overview | A 23A load/migration fixture's retained result reaches Problems; a clean load leaves `Load` `Skipped`; opening never runs a second validation; a stale result is discarded; no double validation on open |
| 3 | Dirty state, autosave, crash recovery | `dirty-autosave-and-recovery` | Save-point-correct dirty tracking and markers, unsaved-changes prompts, `AutosaveScheduler` with an injectable tick, `.autosave` sidecar, crash snapshots and manifests in `recovery/`, recovery prompt, Reload/Recover/Discard/Keep operations, unclean-exit session restore, retention, bounded asynchronous crash-recovery writer | Autosave writes only dirty documents and deletes its file on save; a simulated crash leaves a recoverable `recovery/` snapshot; recovery restores the exact in-memory project; recovery files are swept after 7 days and sidecars are not |
| 4 | 23B tests, documentation, and closeout | `tests-docs-and-closeout` | Harness extension, dirty/save-point/autosave/recovery/validation/crash tests, the 23B documentation column (§12), parity/status updates, `closeout/step-23b.json` (`"step": "step-23b"`), and the audit request | `./tools/verify.sh all --require` passes; the strict docs build passes; `closeout/step-23b.json` validates (the `step-NN[a-z]` checker support is already on `main`); the audit request is complete |

### Inherited 23A deviations and how 23B respects them

| # | 23A deviation | 23B consequence |
| --- | --- | --- |
| 1 | `Avalonia.Headless.XUnit` is not used; tests drive `HeadlessUnitTestSession` from `Avalonia.Headless` | 23B adds no xunit v3 package and no `Avalonia.Headless.XUnit` reference; new view tests use the existing `HeadlessSession` |
| 2 | ANGLE licence exception for `Avalonia.Angle.Windows.Natives` | No new package; the exception is unchanged and 23B adds no licence surface |
| 3 | `InternalsVisibleTo` includes `Mocassin.App` and `Mocassin.App.Views` | All new types are `internal`; `Mocassin.App.ViewModels`/`Views` keep their `PublicAPI.*` and baselines unchanged |
| 4 | Tab drag-reorder is not wired | 23B does not add drag-reorder; the dirty marker is added to the existing tab template |
| 5 | Xvfb real-window evidence exists on Linux X11 | 23B adds Problems/recovery captures only through that harness; screenshots remain Ubuntu-only and PR-medium |
| 6 | `ProjectPathRules` with `ExistingTarget`; CLI behaviour unchanged | 23B does not change New/Open path rules; recovery opens an existing project path through the session service |
| 7 | 23A feature 4 performs exactly one internal load-validation pass and retains `ProjectSession.Validation` | 23B **consumes** that result; it never runs the pipeline again on open. This supersedes the earlier draft wording that implied 23B performs the first load validation |
| 8 | 23A's crash dialog has no recovery action | 23B adds the recovery action and the bounded writer; no other crash-boundary behaviour changes |
| 9 | `mocassin.settings` v1 is normative in 23A §6.1 | 23B adds only `autosave` and `session` additively, with no schema bump |
| 10 | 23A's design names `ProjectSession.LastWritten`; the delivered type exposes `LastWrittenUtc` only | 23B uses the file stamp and `LastWrittenUtc` for the persisted time and adds no saved-project property. The no-arg save-point design does not need one |

## Goal

Make editing safe and diagnosable. A user must be able to change project metadata with undo/redo, see whether the
model is valid, navigate from a diagnostic to its source, and rely on autosave and crash recovery. 23B adds no
new shell surface: it fills the seams 23A delivered.

## Scope

**In scope**

- `ProjectEditCoordinator`, `ProjectChanged`, bounded undo/redo, rejected-edit diagnostics, and read-only refusal.
- The Project Overview metadata editor (name, description, author, notes).
- `ValidationCoordinator`, the validity status item, the Problems channel and its filters/sort/help
  anchors/related navigation/revalidate, edit diagnostics, and on-demand statistics.
- Dirty tracking, title/tab/status markers, the unsaved-changes prompt, save-point correctness.
- Autosave (sidecar plus a `recovery/` fallback/crash snapshot), the recovery prompt,
  Reload/Recover/Discard/Keep, unclean-exit session restore, retention, and the bounded asynchronous
  crash-recovery writer.
- Two additive Core APIs and the additive settings fields (§10).
- The 23B tests, documentation, parity/status updates, closeout evidence, and audit request.

**Non-goals**

- **No shell, workspace, settings, theming, or lifecycle work beyond the documented seam fills.** 23B uses the
  23A deliverables as-is. The only view-surface additions are the console channel selector needed to expose the
  Problems channel, the Problems view, the Overview metadata form, the recovery prompt, the crash dialog's
  recovery action, and the autosave control in the existing settings dialog. If any other seam proves
  insufficient, the step stops and records an amendment rather than editing the shell ad hoc.
- **No untitled-project model.** 23A's New creates and saves the project file in one operation; there is no
  untitled session. 23B introduces none, and `recovery/` is not an untitled-project store. The recovery
  sources are the Step 01 sidecar and a crash snapshot under `recovery/` (§6).
- **No model editors** (particles, structure, transitions, energy, lattice, simulations; Steps 24–25), no build
  UI (26), no 3D (27), no run monitoring (28).
- No change to `.mocprj`/`.msl`/`.msr`/`.mcs`, CLI behaviour, or diagnostic codes; no new format; the settings
  additions are additive with **no schema bump**.

## Design

### 1. Position in the shell

23B registers `AddMocassinEditing` in `App` and fills existing seams:

| 23A seam | 23B use |
| --- | --- |
| `ProjectSession`, `ProjectDocument`, `LoadDiagnostics`, `Validation` | the coordinator wraps the document; validation starts from the retained result |
| `DocumentViewModel` | gains `IsDirty`; the Overview gains the metadata form; the JSON view stays read-only but refreshes |
| `IExplorerNodeProvider` | a Diagnostics node |
| `IConsoleChannelProvider` | the Problems channel |
| `IStatusBarItemProvider` | the validity and autosave items |
| `IProjectSessionService` | gains `RecoverAsync`/`DiscardRecoveryAsync`, the `Recovered`/`Discarded` outcomes, and a `discardChanges` guard on close |
| `AppSettings`/`ISettingsService` | gains the `autosave` and `session` blocks additively |
| `settings`/`IAppSettingsStore`, `IProjectFileProbe` | unchanged; 23B adds `IRecoveryStore` in the same split pattern |
| `ProjectNavigationTargetProvider` | maps project metadata properties to the Overview and focuses the field |
| `ConsoleViewModel`/`ConsoleView` | gains the channel selector; the Problems channel uses the existing console region |
| `CrashHandler`/`CrashDialogViewModel` | gains "Write recovery files" and the bounded writer |

**Seam fills versus redesign.** A *seam fill* adds state, a view, or an implementation behind an interface 23A
already declared, or an additive optional member/parameter listed in §11. It does not remove or rename a 23A
member, change a region, change the command model, change the settings document shape beyond §9, or change the
project lifecycle beyond the save/close/reload hooks named in §11. Changing any of those is a redesign and stops
the step for an amendment. The console channel selector and the autosave control are seam fills because 23A
reserved both surfaces but did not render them.

### 2. Edit pipeline and undo/redo

```csharp
internal sealed partial class ProjectEditCoordinator : ObservableObject
{
    public ProjectDocument Document { get; }
    public bool CanEdit { get; }
    public bool IsDirty { get; }
    public bool CanUndo { get; }
    public bool CanRedo { get; }
    public event EventHandler<ProjectChangedEventArgs>? ProjectChanged;
    public event EventHandler<EditRejectedEventArgs>? EditRejected;

    public ProjectEditResult Apply(string description, Func<Project, Project> mutation);
    public bool Undo();
    public bool Redo();
    public void NotifySaved();
    public void ReplaceDocument(ProjectDocument document);
}
```

- One coordinator per project, created by `ProjectSessionService` and owned by `ProjectSession`. The GUI
  constructs its `ProjectDocument` with `MaxUndoDepth = 200` in the session factories (`CreateFromLoad`,
  reload, recovery); the standalone test factory keeps the library default. It is the **only** write path: view
  models never mutate a `Project` or a domain object, never call `ProjectHasher`, and never build a
  `ProjectChange`.
- `Apply` delegates to `ProjectDocument.Apply` → `ProjectEditor.Apply`, whose validator is the Step 01
  `ProjectValidator`. That is the **structural** validator (identity, seed, metadata, required fields, references
  within the document), not the full `ProjectValidationPipeline`: an edit that is structurally valid but
  cross-domain or feasibility-invalid is accepted and then reported by the validation surface. The UI must not
  claim that an edit is "fully validated" because `Apply` succeeded.
- On failure, `Apply` returns the unchanged project, raises `EditRejected` with the diagnostics, and leaves the
  project, undo/redo stacks, save point, and dirty state unchanged. The initiating document shows the inline
  error; the active Problems channel appends the same diagnostics with source `edit`. The coordinator does not
  reach into the Problems view model itself.
- `Undo`/`Redo` delegate to `ProjectDocument`; the stack is bounded by `MaxUndoDepth` (`0` = unbounded by
  default; the GUI constructs its documents with 200, §10), oldest entries dropped first. The stack survives
  saves; the save point is content-based.
- `CanEdit` is false for a read-only session; `Apply` returns a `ReadOnly` diagnostic and changes nothing.
- Every successful apply/undo/redo raises `ProjectChanged` with the previous and current project values and the
  description. The shell refreshes titles, markers, status bar, the Overview and JSON documents, and schedules
  validation; Step 27's viewport will subscribe to the same event. No Core event is added: the coordinator is
  the single writer.
- Bulk operations are one `Apply` call and therefore one undo entry. Metadata editing is one `Apply` per
  committed field (focus loss or Enter), not per keystroke; an empty project name is refused by
  `ProjectValidator` and surfaces as an inline error. The rejected text stays in the field for correction; the
  model is unchanged.
- Dirty state is computed on change and cached in the coordinator, never on every binding read.

#### 2.1 `ProjectChanged` consumers and lifetime

| Consumer | What it does | Subscription lifetime |
| --- | --- | --- |
| `ShellViewModel` | active title/window title, dirty marker, project-switcher marker, status item, command `CanExecute` | active session only; resubscribes on session switch |
| `ProjectOverviewViewModel` | refreshes identity/metadata/validation/statistics; updates the inline error and banner | document lifetime; unsubscribes in `OnClosingAsync` |
| `ProjectJsonViewModel` | regenerates the canonical JSON, now an observable property | document lifetime; unsubscribes in `OnClosingAsync` |
| `ValidationCoordinator` | debounces and starts a validation run; caches statistics by document hash | session lifetime; resubscribes after `ReplaceDocument` |
| `AutosaveScheduler` | polls `IsDirty`/`IsAutosaveDue` on its tick; it does not need to subscribe | app lifetime |
| Step 27 viewport | subscribes when it lands; no 23B code depends on it | later step |

`ReplaceDocument` (reload/recovery) clears the stacks, resets the cached dirty state, and raises a
`ProjectChanged` with both values equal to the new document's project; consumers re-read the document. The
validation coordinator is re-seeded from the replacement session state, not from a stale hash.

#### 2.2 Save point

- A save calls `ProjectDocument.MarkSaved()` (no-arg) so the clean point is the exact in-memory value. The store
  returns a project with an updated `modifiedUtc`; 23B **does not install that value** into the document. The
  file stamp and `ProjectSession.LastWrittenUtc` record the persisted time for display. Consequences, all
  tested: save → undo → redo is clean; save → edit → undo is clean; save → edit → save is clean; undo below the
  save point is dirty.
- The in-memory `Metadata.ModifiedUtc` therefore remains the value that was loaded until the next reload; the
  Overview labels the file-stamp value as "last saved" and does not claim that the in-memory timestamp was
  rewritten. This is deliberate: installing the store's value would make a redo replay an older `modifiedUtc`
  and dirty the document immediately. Excluding `modifiedUtc` from the dirty hash was rejected because it
  would change Step 01's dirty semantics and add a second hash definition.
- 23A's `SaveAsync`/`SaveAsAsync` currently call `MarkSaved(result.SavedProject!)`; feature 3 changes both calls
  to `MarkSaved()` and keeps the returned value only for the stamp. This is an internal 23A seam change, listed
  in §11.
- `ProjectDocument.CreateRecovered(current, lastSaved, maxUndoDepth: 200)` is the recovery entry point: the
  current snapshot is the document value and the saved file is the clean point. It clears both stacks.

### 3. Validation surface

- **Initial result.** 23A already runs the pipeline exactly once at load/create/reload and stores the result in
  `ProjectSession.Validation` (with `ProjectLoadResult.Diagnostics`). 23B's `ValidationCoordinator` starts from
  that result and **never runs the pipeline on open**. If the result is null (standalone test/document), the
  status is `Not validated`. This is the no-double-validation contract and it is tested by setting the retained
  result to a known value and asserting the coordinator does not replace it until an edit.
- **Subsequent runs.** `ProjectChanged` schedules a run after a 300 ms debounce on an immutable snapshot, with
  `ValidationOptions.Default` (no statistics, no build stage, `compiledModel: null`). A newer edit cancels the
  previous run. The result is applied only if its `DocumentHash` equals the current document hash; otherwise it
  is discarded as stale. The pipeline is the singleton 23A registered; 23B does not construct a second one, does
  not call domain validators, does not use `ValidateFileAsync`, and does not let the store validate on load.
- **`Load` stage semantics (M-1, m-14).** The stage is `Completed`/`Failed` only when
  `ProjectLoadResult.Diagnostics` is non-empty (migration or load diagnostics); a clean current-version load
  correctly yields `Skipped` with count 0 per Step 07 §2. 23B displays both cases and adds no stage logic.
- **Surfaces.** The status-bar validity item (`Not validated` → `Validating…` → `Valid` /
  `n errors, m warnings, k infos`, colour **plus** a text glyph and text); the Problems channel (severity glyph,
  `Code`, `Message`, path fields, `Hint`, source); sortable columns; severity/code/text/source filters; help
  anchors where a code has one; double-click/Enter navigates via `NavigateToAsync(path, SelectObject)`;
  `Related` paths in the context menu; "Revalidate" with a timestamp; never mutates the project; an error banner
  in the Overview; and edit diagnostics from §2.
- **Problems scope.** The channel shows the **active project's** diagnostics, with the project name in the
  header; switching projects replaces the list. It is not an aggregate of every open project and it is not a
  log. No project open → empty state, not a stale list.
- **Help anchors.** A pure `DiagnosticHelpLinks` map resolves a known code to the published diagnostics anchor
  (`https://seb-eis.github.io/mocassin-2/reference/diagnostics/#<lowercase-code>`, the `site_url` in
  `zensical.toml`). `IShellLauncher` gains an internal `OpenUriAsync` implementation in `Mocassin.App.Views`; a
  code without a documented anchor renders unlinked. No code, message, or anchor is invented in the UI.
- **Navigation.** `ProjectNavigationTargetProvider` maps `project/<id>/name`, `description`, `author`, and
  `notes` to the Overview and focuses the field; any other `project` property opens the Overview without a
  focus target. A domain with no provider (particle, structure, …) produces the 23A information notice, not an
  exception and not a placeholder editor.
- **Statistics.** Opt-in: the Overview requests `IncludeStatistics = true` in the background and caches the
  result by document hash. The Step 07 budgets (200 ms / 2 s for ceria) are non-gating evidence (§14). In 23B
  statistics are display-only; clicking a count to an editor is Steps 24–25 and is not claimed here.
- Diagnostics are never invented in the UI: no new codes, no reworded messages.

### 4. Dirty state, markers, and the unsaved-changes prompt

- Dirty is content identity: `ProjectDocument.IsDirty` compares the canonical document hash of the current and
  last-saved values. The coordinator caches it; the shell and documents read the cache.
- **Markers.** The window title gains `*` for the active dirty project; the tab title gains a `*` glyph and an
  accessible "unsaved changes" state; the project switcher marks dirty entries; the status bar shows
  "Unsaved changes" / "Saved". The marker never relies on colour alone. `DocumentViewModel` gains a virtual
  `IsDirty` (default false) and the two project documents override it, raising change notifications from the
  coordinator's state.
- **Unsaved changes.** `IProjectSessionService.CloseAsync` gains `bool discardChanges = false`:
  `CloseAsync(session, discardChanges: false, cancellationToken)`. With a dirty document and
  `discardChanges == false` it returns `ProjectSessionOutcome.UnsavedChanges` and closes nothing. The shell then
  prompts Save/Discard/Cancel; with several dirty projects it prompts per project with "apply to all"; Cancel
  aborts the close and closes nothing. Closing a project updates the settings `session.openProjects` list;
  `cleanExit` stays `false` while the application is running and is written `true` only by a clean application
  shutdown. A cancelled or failed window close leaves it `false`.
- Save, Save As, Reload, and recovery update the cached dirty state through `NotifySaved`/`ReplaceDocument`. A
  read-only session is never dirty and never prompts.
- Reload while dirty prompts "Reload and discard changes?"; confirming replaces the document and clears the
  stacks. "Keep" leaves the in-memory document untouched; Save then refuses because the file changed
  externally, and Save As is the recovery path.

### 5. Autosave

- `AutosaveScheduler` is tick-driven, not a second clock: it exposes
  `Task RunOnceAsync(DateTimeOffset now, CancellationToken)` and uses `ProjectDocument.IsAutosaveDue(now,
  interval)` / `MarkAutosaved(now)`, which already exist in Step 01. A thin `AutosaveTimer` in `Mocassin.App`
  creates the `TimeProvider` timer and calls `RunOnceAsync` on the UI dispatcher; it owns no scheduling policy.
  Tests call `RunOnceAsync` directly after advancing `ManualTimeProvider`, so there is no real sleep and no
  fake `ITimer`.
- The interval comes from `autosave.intervalMinutes` (default 5, range 1–60). `ProjectStoreOptions.AutosaveInterval`
  remains the library's suggested value and is not a second source of truth for the UI.
- Each tick, for every writable session with a dirty document:
  - saved project → `ProjectStore.WriteAutosaveAsync(current, session.Path)` writes
    `<file>.autosave` (the Step 01 sidecar);
  - if that write fails (I/O), → `IRecoveryStore.WriteAsync(current, path, source: autosave-fallback)` writes
    `<app-data>/recovery/<projectId>.mocprj.autosave` plus the manifest (§6) and reports the fallback.
- The immutable snapshot is written asynchronously; the status bar shows "Auto-saving…" then
  "Auto-saved HH:mm". A per-session `SemaphoreSlim(1,1)` on `ProjectSession` serialises save and autosave.
  Autosave never clears dirty state and never updates the save point. On a successful save, the sidecar (and any
  recovery file for that project) is deleted.
- Read-only sessions never autosave and never write recovery files. A failed autosave logs a warning, updates
  the status item, and leaves the dirty state and save point untouched.
- The scheduler is a singleton; it enumerates sessions through `IProjectSessionService`, not by holding a
  second session list. It stops cleanly at shutdown and never races a close (a closing session is skipped).

### 6. Recovery and crash handling

```csharp
internal interface IRecoveryStore
{
    Task<IReadOnlyList<RecoveryCandidate>> ListAsync(CancellationToken cancellationToken = default);
    Task<RecoveryWriteResult> WriteAsync(Project project, string projectPath, RecoverySource source, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(RecoveryCandidate candidate, CancellationToken cancellationToken = default);
    Task<int> SweepAsync(DateTimeOffset now, TimeSpan retention, CancellationToken cancellationToken = default);
}

internal enum RecoverySource { AutosaveFallback, Crash }

internal sealed record RecoveryCandidate(
    Guid ProjectId,
    string ProjectPath,
    string SnapshotPath,
    DateTimeOffset SavedUtc,
    RecoverySource Source);

internal sealed record RecoveryWriteResult(
    bool IsSuccess,
    string? SnapshotPath,
    ImmutableArray<Diagnostic> Diagnostics);
```

`IProjectSessionService` gains two operations and the `Recovered` outcome:

```csharp
Task<ProjectSessionResult> RecoverAsync(
    ProjectSession session, RecoveryCandidate candidate, CancellationToken cancellationToken = default);
Task<ProjectSessionResult> DiscardRecoveryAsync(
    ProjectSession session, RecoveryCandidate candidate, CancellationToken cancellationToken = default);
```

`Recovered` and `Discarded` are success outcomes in `ProjectSessionResult.IsSuccess`. The shell owns the
prompts; the service never prompts.

- `IRecoveryStore` is the ViewModels contract; `RecoveryStore` lives in `Mocassin.App` and uses the real
  `ProjectStore.WriteAutosaveAsync` with the base path `<app-data>/recovery/<projectId>.mocprj`, so no new
  `ProjectStore` overload is needed. `RecoveryCandidate`, `RecoverySource`, and `RecoveryRetentionPolicy` stay
  in ViewModels (pure). The crash writer is `CrashRecoveryWriter` (App); there is no separate `IRecoveryWriter`.
- **Sources.** (1) The Step 01 sidecar `<projectPath>.autosave` is the primary periodic snapshot. (2) A
  `recovery/` snapshot plus `<projectId>.json` manifest is the location-independent fallback for a failed
  sidecar write and the guaranteed crash snapshot. The manifest is app-local
  (`format: "mocassin.recovery"`, `version: 1`, project id, project path, `savedUtc`, source); it is not in
  `contracts/manifest.json`. A manifest without its snapshot is ignored; a snapshot without a manifest is not
  offered.
- **Recovery on open.** After a successful load, the session checks the sidecar. If it exists and its canonical
  content differs from the saved file, the session exposes a `PendingRecovery` candidate; the shell offers
  Recover / Discard / Decide later through `IProjectSessionService.RecoverAsync`/`DiscardRecoveryAsync`. At
  startup the shell also scans `recovery/`; when an opened or restored project has a manifest candidate, the
  shell composes the offer from the sidecar and the manifest candidate (newest valid wins) and passes the chosen
  candidate to the same operations. Recovering builds a document with
  `ProjectDocument.CreateRecovered(snapshot, savedFile)`, replaces the document, leaves it dirty, and the next
  save writes the main file and deletes the snapshot. "Decide later" keeps it and the status bar shows "Recovery
  available". A snapshot for a different project id is rejected.
- **Unclean exit.** The settings `session` block records the open project paths, the active project, and
  `cleanExit`; it is written on open/close/activate and at shutdown. At startup, a stale `cleanExit = false`
  produces "Restore session and recover changes" after the recovery scan. Restoring opens the paths that still
  exist and reports the rest; a per-project recovery candidate is applied automatically in that flow (no prompt
  storm). A clean exit writes `cleanExit = true`. A failed settings write never blocks startup; the previous
  document is preserved and the failure is logged.
- **Missing project file.** Recovery is offered only when the main file still exists and opens. If it is
  missing, the recovery dialog reports the snapshot path and offers Discard/Open folder; 23B does not invent an
  untitled session or a Save-As-from-snapshot lifecycle. This is a recorded limitation (§17), not hidden scope.
- **Crash.** The crash dialog gains "Write recovery files". `CrashHandler` captures the dirty sessions' projects
  on the UI thread before showing the dialog; `CrashRecoveryWriter` starts the `IRecoveryStore` writes for every
  captured snapshot, awaits them with a 2 s budget (`Task.WhenAny` against a `CancellationTokenSource`), and
  reports per-session success/timeout/failure. The dialog disables its exit action while a write is in flight and
  the handler awaits the result before shutting down, so a user click cannot be lost to the exit. No `.Result`,
  no `GetAwaiter().GetResult()`: the store exposes only async writes. If the budget expires, the log records
  unfinished sessions and the app exits without claiming they were saved. There is no automatic restart. If the
  UI thread is dead, there is no dialog and no recovery write: the crash report is written and the process
  exits; this is recorded as a limitation because the document snapshots are UI-thread-confined.
- **Retention.** The startup sequence sweeps `recovery/` first, then scans it. Recovery files older than 7 days
  are deleted by `RecoveryRetentionPolicy` through `IRecoveryStore.SweepAsync`; log files keep 23A's
  14 days/100 MB. A `.autosave` sidecar next to a user's project is **never** swept — only a successful save or
  an explicit discard removes it. Projects, packages, results, and job folders are never auto-deleted. The sweep
  deletes only the known snapshot/manifest patterns and leaves unknown files alone; failures are logged, not
  fatal.
- Recovery files are untrusted input: the store bounds the manifest size and entry count, the snapshot goes
  through the same `ProjectStore` load path (schema, migration, 64 MiB cap), and the manifest's project path is
  passed through the session's normal path validation before any open. No path from a manifest is written to.

### 7. State ownership and threading additions

| State | Owner | Persisted |
| --- | --- | --- |
| Undo/redo stacks, save point | `ProjectDocument` | memory |
| Dirty state | `ProjectDocument` hash, cached by the coordinator | derived |
| Validation result, statistics cache | per-session `ValidationCoordinator` (subscribes to `ProjectChanged`) | memory |
| Autosave timestamps | `ProjectDocument` (`LastAutosaveUtc`/`MarkAutosaved`) | memory |
| Save/autosave semaphore | `ProjectSession` | memory |
| Autosave scheduling | `AutosaveScheduler`/`AutosaveTimer` | memory |
| Recovery snapshots and manifest | `IRecoveryStore` | `recovery/` |
| Pending recovery candidate | `ProjectSession` | memory + sidecar/`recovery/` |

Rules: all `ProjectDocument` mutations happen on the UI thread; validation runs off-thread over an immutable
snapshot and is applied only when not stale; `IUiDispatcher`/`TimeProvider` are used exactly as in 23A; long
operations take a `CancellationToken` and never block; the crash path is asynchronous and bounded; the autosave
tick is serialized per session and never overlaps a save or close.

### 8. View-model and view additions

- ViewModels: `Editing/` (`ProjectEditCoordinator`, `EditDiagnosticSet`, `ProjectChangedEventArgs`,
  `EditRejectedEventArgs`), `Diagnostics/` (`ValidationCoordinator`, `ProblemsChannelViewModel`,
  `DiagnosticItemViewModel`, `ValidationState`, `DiagnosticHelpLinks`), `Recovery/` (`IRecoveryStore`,
  `RecoveryRetentionPolicy`, `RecoveryCandidate`, `RecoverySource`), `Scheduling/` (`AutosaveScheduler`), and
  the Overview metadata form view model. `DocumentViewModel` gains `IsDirty`; `StatusBarItemViewModel` gains an
  optional status glyph/severity for the validity item, keeping the existing constructor. The Overview metadata
  form exposes a focus request by property name for diagnostic navigation.
- Views: `Consoles/ProblemsView` and the console channel selector, the Overview metadata form, the recovery
  prompt dialog, the crash dialog's recovery action, and the autosave control in the existing settings dialog.
  No new shell region or settings page.
- All new types are `internal` with `InternalsVisibleTo("Mocassin.App.Tests")`, matching 23A. No App
  `PublicAPI.*` or ApiCompat baseline changes.

### 9. Settings additions (no schema bump)

The base document is **23A §6.1**, which is normative for `format`, `schemaVersion`, every v1 key and nesting
level, defaults, the unknown-key/migration policy, the 4 MiB cap, the source-generated
`AppSettingsJsonContext`, and the atomic write/fsync discipline. 23B adds exactly two top-level objects to that
same v1 document and changes no existing key:

```json
{
  "autosave": { "enabled": true, "intervalMinutes": 5 },
  "session": { "openProjects": ["/work/ceria.mocprj"], "activeProject": "/work/ceria.mocprj",
               "cleanExit": true, "savedUtc": "2026-11-21T10:00:00Z" }
}
```

| Key | Type | Default | Rule |
| --- | --- | --- | --- |
| `autosave.enabled` | boolean | `true` | Autosave on/off |
| `autosave.intervalMinutes` | integer | `5` | 1–60 inclusive; out of range falls back to `5` with a warning |
| `session.openProjects` | array of string | `[]` | Paths of the projects open at the last shutdown; missing paths are reported and skipped on restore |
| `session.activeProject` | string or null | `null` | Path of the active project, or null; must be one of `openProjects` after normalization |
| `session.cleanExit` | boolean | `true` | Written `false` while running and set `true` on a clean shutdown; a stale `false` triggers the restore offer |
| `session.savedUtc` | string or null | `null` | ISO-8601 UTC time the session block was written |

Details that follow from the base shape:

- **No `schemaVersion` bump.** Missing keys mean the defaults above, unknown keys stay ignored, and the writer
  still emits `format`/`schemaVersion`. A 23A document without `autosave`/`session` loads unchanged.
- **No new top-level key beyond these two.** No other v1 key changes type, meaning, or default.
- `session.openProjects` is capped at 20 entries, de-duplicated with the platform path comparison, and
  empty/invalid entries are dropped with a warning; `session.activeProject` is normalized to an entry or null.
- `layout.activeConsoleChannel` may now persist `"problems"` in addition to `"application"`; the 23A
  unknown-channel fallback still applies.
- `recovery/` becomes active in the app-data layout; the 4 MiB cap and atomic write/fsync discipline are
  inherited unchanged.
- The document remains application-local and is **not** in `contracts/manifest.json`.

### 10. Library changes owned by this step

Two additive Core changes; no Packages change and no App public surface change.

| Change | Where | Why the shell cannot own it | Contract effect |
| --- | --- | --- | --- |
| `ProjectDocument.MaxUndoDepth` (init-only, default `0` = unbounded) | `Mocassin.Core.Editing` | The stack lives in the document; a UI-level bound would shadow it | Additive property; the default preserves Step 01's unbounded behaviour. The GUI constructs its documents with `MaxUndoDepth = 200`. Negative values are rejected when an edit is applied; `0` is unbounded |
| `ProjectDocument.CreateRecovered(Project current, Project lastSaved, ProjectEditor? editor = null, int maxUndoDepth = 0)` | `Mocassin.Core.Editing` | Recovery needs a clean point different from the current value; `MarkSaved(value)` overwrites both | Additive static factory; constructors unchanged. The GUI passes `maxUndoDepth: 200`; the default `0` is unbounded |

- **No `ProjectStore.WriteAutosaveToPathAsync`.** The Step 01 `WriteAutosaveAsync(project, projectPath)` derives
  `<projectPath>.autosave`; passing the recovery base path `<app-data>/recovery/<projectId>.mocprj` writes the
  recovery snapshot without a new API. `IRecoveryStore` wraps that call and owns the manifest. This keeps the
  library delta at two APIs and reuses the existing `AutosaveFailed` diagnostics.
- Both are listed in `Mocassin.Core/PublicAPI.Unshipped.txt`; no baseline refresh is expected. 23A's
  `ProjectPathRules` change is not repeated.
- The save-point change in §2.2 and the close guard in §4 are internal `Mocassin.App.ViewModels` edits, not
  library API changes.

### 11. Dependency and interface checks

- **23A (required):** `ProjectSession`/`ProjectDocument`/`LoadDiagnostics`/`Validation`, the document model and
  Overview/JSON documents, `INavigationService` and target registry, `IExplorerNodeProvider`,
  `IConsoleChannelProvider`, `IStatusBarItemProvider`, `IProjectSessionService`, the settings model/store and
  `IProjectFileProbe`, `ConsoleViewModel`/`ConsoleView`, the crash boundary, and the harness. 23B edits these
  assemblies to fill the seams; it does not redesign them.
- **23A seam edits owned by 23B (all internal):**

| Seam | 23B edit |
| --- | --- |
| `ProjectSessionService.SaveAsync`/`SaveAsAsync` | call `MarkSaved()` (no-arg) and delete the sidecar/recovery file on success; keep the stamp |
| `ProjectSessionService.CloseAsync` | add the `discardChanges` guard returning `UnsavedChanges` |
| `ProjectSession` | own the coordinator, expose `PendingRecovery`, replace the document on reload/recovery |
| `DocumentViewModel`/tab template | add `IsDirty` and the dirty marker |
| `ProjectOverviewViewModel`/`ProjectJsonViewModel` | subscribe to `ProjectChanged`; metadata form; observable JSON |
| `ProjectNavigationTargetProvider` | map project metadata properties to Overview focus |
| `IShellLauncher` | add `OpenUriAsync` for diagnostic help anchors |
| `ConsoleViewModel`/`ConsoleView` | channel selector and Problems template |
| `CrashHandler`/`CrashDialogViewModel`/`DialogService` | recovery action and writer injection |
| `SettingsViewModel`/settings view | autosave control |

- **Steps 24–28 (consumers):** the edit coordinator and `ProjectChanged` (24, 25, 26, 27), the validation
  surface's navigation (24, 25), and the console/status seams (26, 28).
- **Not consumed:** Steps 13–22 APIs; no solver, results, or analysis types in this step.
- **Parallel track:** this design touches planning docs only; the implementor merges `main` when 23B starts and
  re-reads the 23A surface.

### 12. Documentation plan

| Page | 23B content |
| --- | --- |
| `docs/getting-started/quickstart.md` | validation/Problems, metadata editing, undo/redo, autosave/recovery sections |
| `docs/getting-started/first-project.md` | undo/redo, dirty/save point, autosave, crash recovery |
| `docs/user-guide/projects.md` | undo/redo, dirty state and save point, autosave, recovery, retention; replace the 23A "later work" statements |
| `docs/development/ui-architecture.md` | edit pipeline, validation surface, recovery, harness/screenshot/a11y chapters |
| `docs/development/design-decisions.md` | the save-point, `ProjectChanged`, autosave-tick, and recovery-source decisions |
| `docs/development/testing-and-parity.md` | 23B harness, tiers, and the recovery/crash policy; move the save-point coverage from the 23A list |
| `docs/troubleshooting/installation.md` | recovery prompts and recovery-file problems |
| `docs/reference/file-layout.md` | `recovery/` layout and manifest, the autosave sidecar, retention rules; the `autosave`/`session` settings fields |
| `docs/reference/diagnostics.md` | no new codes; the help-anchor convention is already documented |

No page describes a model editor, build UI, 3D, or run monitoring. No page claims that 23B offers untitled
projects.

### 13. Test plan

Harness: the 23A `Mocassin.App.Tests` harness, `Avalonia.Headless` with `HeadlessSession`, plain xUnit for
view-model tests, real `ProjectStore`/`ProjectValidationPipeline`/temporary directories, and
`ManualTimeProvider`. Fakes are limited to the true boundaries: dialogs in view-model tests, pickers, clipboard,
dispatcher, time, and the in-memory `IRecoveryStore`/`IAppSettingsStore` for non-persistence tests. No fake
`Project`, no fake validation result, no fake store.

| Area | Tier | Assertion |
| --- | --- | --- |
| Validation initial result | PR-fast | Opening a session uses the retained `ProjectSession.Validation`; the coordinator does not run the pipeline on open; a null retained result shows `Not validated`; an edit starts exactly one run |
| Validation contract (M-1/m-14) | PR-fast | A load/migration fixture yields `Load` `Completed`/`Failed` and reaches Problems; a clean fixture yields `Skipped` with count 0; no duplicate diagnostics; `ValidateFileAsync` is unused; the store has `ValidateOnLoad == false` |
| Undo/redo | PR-fast | Multi-step apply/undo/redo through the Overview document; save → undo → redo clean; save → edit → undo clean; undo below the save point dirty; the GUI document uses 200 and drops the oldest; a new edit clears redo; a negative `MaxUndoDepth` is rejected and 0 is unbounded |
| Rejected edit | PR-fast | A structural failure leaves the project, stacks, dirty state, and save point unchanged and raises `EditRejected`; a cross-domain-invalid but structurally valid edit is accepted and reported by validation |
| Read-only | PR-fast | Every `Apply` on a read-only session returns `ReadOnly` and changes nothing |
| Validation surface | PR-fast | Debounce coalesces edits; a stale result is discarded by `DocumentHash`; counts/codes match `ValidationResult`; double-click requests the registered target; an unknown domain produces a notice; a metadata path focuses the Overview field; an unknown code is unlinked |
| Dirty/autosave | PR-fast | Only dirty writable documents are written; the interval comes from settings and the tick uses `IsAutosaveDue`; save and autosave never interleave; a clean save deletes the sidecar/recovery file; read-only sessions never write; a sidecar failure falls back to `recovery/` |
| Recovery | PR-fast | A simulated crash leaves a `recovery/` snapshot and manifest; startup offers recovery; Recover yields a dirty document equal to the snapshot; Discard deletes; Reload replaces the document; retention removes only old recovery files; sidecars are never swept; a missing project file is reported without an untitled session |
| Unclean exit | PR-fast | `cleanExit=false` offers restore; `cleanExit=true` does not; missing paths are reported; the active project is restored; a clean shutdown writes `true` |
| Crash path | PR-fast | The writer completes within its budget or reports a timeout; no blocking call; the dialog reports per-session results; a dead UI thread writes no recovery files |
| Settings additions | PR-fast | The `autosave`/`session` blocks round-trip in the v1 document without a schema bump; a 23A document without them still loads with defaults; the interval range and open-project cap normalize with warnings |
| Screenshots/a11y | PR-medium | Problems channel, recovery prompt, and metadata form render; the new status items and dirty marker have names/roles; Linux baselines use the 23A tolerance |
| Platform gate | manual (Windows, macOS) | Recovery prompt, crash dialog, and autosave on each OS |

### 14. Performance expectations (non-gating)

Live validation (ceria, no statistics) < 200 ms and statistics < 2 s are Step 07 design budgets recorded as
evidence, not PR-fast assertions. Autosave of the ceria sample < 500 ms and the recovery scan of a small
`recovery/` directory < 100 ms are recorded at feature 3 closeout. No absolute duration is asserted in a
PR-fast test; the only test time limit is a hang guard.

### 15. Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Undo/redo complexity | Reuses the Step 01 `ProjectDocument`; no second stack; save-point matrix |
| `MaxUndoDepth` semantics | Default 0 (unbounded) preserves Step 01; the GUI sets 200; negative values rejected; the boundary is tested |
| Edit validation is structural only | `Apply` uses `ProjectValidator`; the full pipeline is the advisory surface and the step page states the difference |
| Validation cost/threading | Initial result is reused; 300 ms debounce; stale-result guard; off-thread; one pipeline |
| Autosave/save races | Per-session semaphore; immutable snapshots; atomic writes from the store; tick skips closing sessions |
| Crash-path hang | Async writes with a 2 s budget and per-session reporting; no sync-over-async; dead-UI path writes no recovery files |
| Recovery file corruption/path abuse | Manifest and snapshot bounds; real `ProjectStore` load path; manifest paths validated; sweep only known patterns |
| Missing project file | Reported as a limitation; no untitled session or new lifecycle path |
| 23A seam insufficiency | The documented seam fills are bounded; any other signature/region change stops for an amendment |
| Console channel selector scope | One view addition in the existing console region; no new region or command model |
| Settings write failure | Never blocks startup; previous document intact; restore offer may be unavailable and is reported |
| Help-anchor drift | `DiagnosticHelpLinks` is checked against the documented code list; unknown codes unlinked |
| Platform recovery differences | Paths via `IProjectFileProbe`/`IRecoveryStore`; manual platform checklist |

### 16. Complexity class and split decision

| Dimension | Class | Evidence |
| --- | --- | --- |
| Contracts, formats, and public surfaces | **Medium** | Two additive Core APIs; the settings additions are additive with no schema bump; no format change |
| New dependencies | **Low** | None; the harness and Avalonia stack come from 23A |
| Cross-step interfaces | **High** | `ProjectEditCoordinator`/`ProjectChanged` and the validation surface are consumed by Steps 24, 25, 26, and 27; a late change rewrites editor code |
| Irreversibility | **Medium** | Undo/save-point and autosave semantics are documented and testable; reversible with a recorded decision; no scientific or persisted-project change |
| Test-matrix breadth and failure modes | **High** | Dirty/save-point, autosave timing, recovery/crash, validation staleness, and platform recovery paths, on top of the headless harness |
| Independent acceptance stories | **Medium** | One coherent story: editing is safe and diagnosable (undo/validation) and recoverable (autosave/recovery); the facets are not separately approvable deliverables |
| Evidence size for a fresh auditor | **Medium** | Four features, no new application project, two additive APIs, nine documentation sections; one sitting is feasible |

**Class: High** (cross-step interface and test-matrix breadth). *Justification:* 23B defines the edit/validation
interfaces that four later steps consume and spans a broad failure matrix (save point, timers, crash, staleness,
platform recovery), while its contracts, dependencies, irreversibility, acceptance story, and evidence size stay
Medium because no format or production dependency changes and the outcome is one editing-safety story. The class
was **reconfirmed at the 23B design freeze (2026-11-21)**; it is not revised.

**One-unit case.** Splitting editing/validation from recovery would strand autosave without a dirty state and
leave the Problems channel with nothing to report; it would also duplicate the session/document fixtures and
separate the edit pipeline from its only consumer. A split would add a roadmap step and change Step 24's
dependency chain without reducing the interface or test-matrix breadth that makes the step High.

**Risk guard.** If a 23A seam needs a signature, region, command-model, or lifecycle change beyond the §11
list — or if the edit pipeline/validation contract has to change after feature 1 — the step stops and records an
amendment before feature 2. **No further split is proposed.**

### 17. Decisions and open items

| Decision | Record |
| --- | --- |
| 23B extends the 23A shell; it adds no shell/settings/lifecycle capability beyond the §1 seam fills | This page §1 |
| The edit coordinator is the only write path; no Core event | This page §2; [`05-open-decisions.md`](../05-open-decisions.md) |
| `Apply` uses the structural `ProjectValidator`; the full pipeline is advisory | This page §2 |
| The save point is `MarkSaved()` no-arg; the store's returned value is not installed | This page §2.2; [`05-open-decisions.md`](../05-open-decisions.md) |
| Validation starts from `ProjectSession.Validation`; no second validation on open | This page §3/§13 |
| `Load` stage is conditional on load diagnostics (clean load is `Skipped`) | This page §3/§13 |
| No untitled-project model; `recovery/` holds crash snapshots and manifest-backed fallbacks | This page §5/§6 |
| `IRecoveryStore` + `CrashRecoveryWriter`; there is no `IRecoveryWriter` type | This page §6 |
| Two additive Core APIs; no Packages API change | This page §10 |
| `autosave`/`session` are additive settings fields with no schema bump | This page §9 |
| Problems shows the active project only; statistics are display-only in 23B | This page §3 |
| Class High; owner acceptance at the start gate; no further split | This page §16 |

Open items for the 23B start gate: confirm that the 23A seams proved sufficient in practice (no shell edit
beyond the §11 list), that the two Core additions are still additive after 23A merged, and that the owner accepts
the `MaxUndoDepth` GUI bound (default 0/unbounded, GUI sets 200) and the no-untitled recovery scope.

### 18. Closeout and audit plan

- **Feature 4 is the only closeout feature.** It owns the 23B documentation column, parity/status updates,
  `docs/planning/closeout/step-23b.json` (`"step": "step-23b"`), and the audit request.
- **Before the freeze:** all four features are squash-merged; current `main` is merged; `./tools/verify.sh all
  --require` passes on the integration tree; one closeout `CI` dispatch (Ubuntu) is recorded, with the Windows
  platform dispatch and the macOS manual gate as for 23A.
- **Closeout identity and prerequisite (M-1):** the evidence file is `docs/planning/closeout/step-23b.json`, its
  `step` field is exactly `"step-23b"`, the tag is `step-23b`, and the audit record is
  `docs/planning/audits/step-23b.md`. There is no `step-23` alias. `tools/check_closeout_evidence.py` accepts
  `^step-\d{2}[a-z]?$` and this support is already on `main`; 23B does not work around it.
- **Evidence and audit:** the same canonical closeout evidence and fresh-auditor process as 23A, with
  `audits/step-23b.md` created on demand at that point, not pre-seeded. Merge with `--no-ff` and tag `step-23b`
  only after a `Passed` audit, owner approval, and a green automatic `main` CI run.
- **Design review:** [`reviews/step-23b.md`](../reviews/step-23b.md) is the request record; the freeze SHA is
  recorded there. A fresh planning auditor reviews the frozen design and writes the verdict.
- **Dashboard:** the step agent uses the existing lowercase `23b` row (matching
  `check_process_consistency.py`'s `\d{2}[a-z]?` parser) when the step starts; the planner does not edit
  `status.md`.

## Implementation tasks

- [ ] Implement `ProjectEditCoordinator` with apply/undo/redo, bounded depth, `ProjectChanged`/`EditRejected`,
      read-only refusal, and the Overview metadata editor.
- [ ] Implement `ValidationCoordinator` from the retained result, the validity status item, the Problems channel
      (filters, sort, help anchors, related navigation, revalidate), edit diagnostics, and on-demand statistics.
- [ ] Implement dirty state, the save point, markers, unsaved-changes prompts, the autosave scheduler, the
      sidecar and `recovery/` fallback, the recovery prompt, Reload/Recover/Discard/Keep, session restore, and
      retention.
- [ ] Implement `IRecoveryStore`, `RecoveryStore` (App), `CrashRecoveryWriter`, and the crash-dialog recovery
      action.
- [ ] Add the two Core changes with tests and `PublicAPI.Unshipped.txt` entries; verify no App or Packages API
      change.
- [ ] Add the 23B tests, documentation column, parity/status updates, `closeout/step-23b.json`, and request the
      audit.

## Acceptance criteria

- [ ] Metadata editing works with undo/redo across the save point in both directions; a rejected structural edit
      changes nothing; a read-only session refuses edits.
- [ ] The validity indicator and Problems channel show code, severity, path, source, and help; double-click
      navigates through a registered provider and produces a notice otherwise; the retained load/migration
      result reaches Problems and a clean load leaves the `Load` stage `Skipped`; opening does not run a second
      validation.
- [ ] Dirty state is correct across save, undo, and redo; closing with unsaved changes prompts; Cancel closes
      nothing.
- [ ] Autosave writes only dirty documents and deletes its file on save; a simulated crash leaves a `recovery/`
      snapshot; recovery restores a dirty document equal to the snapshot; the crash path completes within its
      budget or reports the timeout.
- [ ] Recovery files are swept after 7 days; sidecars are never swept; unclean-exit session restore works;
      missing project files are reported without an untitled session.
- [ ] The `autosave`/`session` settings fields round-trip without a schema bump and older documents still load.
- [ ] No unhandled exception kills the process silently, and the crash dialog reports recovery results; the
      dead-UI path is documented.
- [ ] `./tools/verify.sh all --require` passes, the strict docs build passes, and `closeout/step-23b.json` with
      `"step": "step-23b"` validates.
- [ ] Documentation written (§12), and no page claims a later step's capability or an untitled-project model.

## Documentation

See §12. These sections are added to the pages 23A created; 23B does not create the base pages.

## Tests

See §13. Required evidence: real-object undo/save-point tests, both `Load`-stage fixtures, the retained-result
no-double-validation test, dirty/autosave and recovery tests with `ManualTimeProvider`, the crash-path budget
test, settings-addition round-trips, and the manual platform recovery check.

## Risks and mitigations

See §15.

## Done when

Editing is safe and diagnosable: metadata edits are undoable across the save point, validation is live and
navigable, dirty state and unsaved prompts are correct, autosave and crash recovery restore the exact in-memory
project, and the shell delivers all of it without a 23A redesign — with every acceptance criterion, test tier,
documentation section, parity/status update, closeout evidence, and audit complete for 23B alone.
