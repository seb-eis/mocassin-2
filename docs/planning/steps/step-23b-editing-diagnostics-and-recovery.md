# Step 23B — Editing, Diagnostics, and Recovery

**Phase:** F. GUI
**Depends on:** 23A (workspace and project lifecycle) plus the merged library surfaces of 01, 03–07, and 12.
Not dependent on Steps 08–11 or 13–22.
**Delivers:** The single-project edit pipeline with undo/redo, the validation and Problems surface, dirty state
and save-point handling, autosave, crash recovery, and unclean-exit session restore — on top of the 23A shell.
**Status:** Design drafted; **not design-reviewed and not started.** No review record exists by design; the
review is created from the template when the step is scheduled, and the step page is refrozen at that point.

> **Split note.** Step 23 was split into **23A** and **23B** by the owner on 2026-11-20 (complexity class High,
> three independent acceptance stories, oversized audit surface). 23B is the editing/diagnostics/recovery half.
> It needs nothing from 23A beyond delivered code, and 23A contains no 23B task. The superseded unsplit design
> is [`step-23-avalonia-shell-and-project-lifecycle.md`](step-23-avalonia-shell-and-project-lifecycle.md); its
> review record is [`reviews/step-23.md`](../reviews/step-23.md).

## Implementation status

- **Status:** Not started — design drafted, not reviewed, no step branch
- **Step branch:** —
- **Step CI:** —
- **Main merge:** —
- **Step tag:** —
- **Audit:** Not requested
- **Design review:** **Not requested** — 23B has no review record yet; one is created from
  [the template](../reviews/step-design-review-template.md) when the step is scheduled after 23A is merged
- **Feature list:** Draft (four features) — to be approved at the 23B start gate
- **Complexity class:** **High** (cross-step interface and test-matrix breadth; §16). **Owner acceptance of the
  class is required at the 23B start gate**; a further split is not proposed (§16).
- **Last updated:** 2026-11-20

| Feature | Branch | Status | Local gates | Step CI | Squash commit |
| --- | --- | --- | --- | --- | --- |
| — | — | — | — | — | — |

### Draft feature list (to be approved at the 23B start gate)

Each branch will be `feature/step-23b-editing-diagnostics-and-recovery/<slug>` from the 23B step branch. The
order is the implementation order; 23B adds no shell, workspace, settings, or lifecycle capability.

| # | Feature | Slug | Planned scope | Done when |
| --- | --- | --- | --- | --- |
| 1 | Edit pipeline and undo/redo | `edit-pipeline-and-undo-redo` | `ProjectEditCoordinator` per project over `ProjectDocument`; `Apply`/`Undo`/`Redo`; bounded undo depth; `ProjectChanged`; rejected-edit diagnostics; the Project Overview metadata editor (name/description/author/notes, one undo entry per committed field) | Undo/redo crosses the save point correctly in both directions; a rejected edit changes nothing and reports diagnostics; the coordinator is the only write path |
| 2 | Validation surface | `validation-surface` | `ValidationCoordinator` over `ProjectValidationPipeline` (in-memory entry seeded with the session's load diagnostics, `ValidationOptions.Default`), 300 ms debounce, stale-result discard, status-bar validity item, Problems channel with severity/code/text filters, sorting, help anchors, `Related` navigation, revalidate, edit diagnostics, on-demand statistics in the Overview | A load/migration fixture populates the `Load` stage and reaches Problems; a clean load leaves it `Skipped`; a stale result is discarded; no double validation on open |
| 3 | Dirty state, autosave, crash recovery | `dirty-autosave-and-recovery` | Save-point-correct dirty tracking and markers, unsaved-changes prompts, `AutosaveScheduler` over `TimeProvider`, `.autosave` sidecar, untitled-project recovery in `recovery/`, recovery prompt, Reload/Recover/Discard/Keep operations, unclean-exit session restore, retention, bounded asynchronous crash-recovery writer | Autosave writes only dirty documents and deletes its file on save; a simulated crash leaves a recoverable snapshot; recovery restores the exact in-memory project; recovery files are swept after 7 days and sidecars are not |
| 4 | 23B tests, documentation, and closeout | `tests-docs-and-closeout` | Harness extension, dirty/save-point/autosave/recovery/validation/crash tests, the 23B documentation column (§12), parity/status updates, `closeout/step-23b.json` (`"step": "step-23b"`), and the audit request | `./tools/verify.sh all --require` passes; the strict docs build passes; closeout evidence validates once the `step-NN[a-z]` tooling prerequisite has landed; the audit request is complete |

## Goal

Make editing safe and diagnosable. A user must be able to change project metadata with undo/redo, see whether the
model is valid, navigate from a diagnostic to its source, and rely on autosave and crash recovery. 23B adds no
new shell surface: it fills the seams 23A delivered.

## Scope

**In scope**

- `ProjectEditCoordinator`, `ProjectChanged`, bounded undo/redo, rejected-edit diagnostics.
- The Project Overview metadata editor (name, description, author, notes).
- `ValidationCoordinator`, the validity status item, the Problems channel and its filters/sort/help
  anchors/related navigation/revalidate, edit diagnostics, and on-demand statistics.
- Dirty tracking, title/tab/status markers, the unsaved-changes prompt, save-point correctness.
- Autosave (sidecar and untitled recovery), the recovery prompt, Reload/Recover/Discard/Keep, unclean-exit
  session restore, retention, and the bounded asynchronous crash-recovery writer.
- Three additive library APIs and the additive settings fields (§10).
- The 23B tests, documentation, parity/status updates, closeout evidence, and audit request.

**Non-goals**

- **No shell, workspace, settings, theming, or lifecycle work.** 23B uses the 23A deliverables as-is; if a seam
  proves insufficient, the step stops and records an amendment rather than editing the shell ad hoc.
- **No model editors** (particles, structure, transitions, energy, lattice, simulations; Steps 24–25), no build
  UI (26), no 3D (27), no run monitoring (28).
- No change to `.mocprj`/`.msl`/`.msr`/`.mcs`, CLI behaviour, or diagnostic codes; no new format; the settings
  additions are additive with **no schema bump**.

## Design

### 1. Position in the shell

23B registers `AddMocassinEditing` in `App` and fills existing seams:

| 23A seam | 23B use |
| --- | --- |
| `ProjectSession`, `ProjectDocument`, `LoadDiagnostics` | the coordinator wraps the document; validation seeds the load diagnostics |
| `DocumentViewModel` | the Overview document gains an editable metadata form; the JSON view stays read-only |
| `IExplorerNodeProvider` | a Diagnostics node |
| `IConsoleChannelProvider` | the Problems channel |
| `IStatusBarItemProvider` | the validity and autosave items |
| `IProjectSessionService` | gains `RecoverAsync`/`DiscardRecoveryAsync` and the `Recovered` outcome |
| `AppSettings`/`ISettingsService` | gains the `autosave` and `session` blocks additively |
| `settings`/`IAppSettingsStore`, `IProjectFileProbe` | unchanged; 23B adds `IRecoveryStore` in the same split pattern |

### 2. Edit pipeline and undo/redo

```csharp
internal sealed partial class ProjectEditCoordinator : ObservableObject
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

- One coordinator per project, created by `ProjectSessionService` (23A). It is the **only** write path: view
  models never mutate a `Project` or a domain object, never call `ProjectHasher`, and never build a
  `ProjectChange`.
- `Apply` delegates to `ProjectDocument.Apply` → `ProjectEditor.Apply`, which validates the candidate and refuses
  error states. On failure the coordinator raises a transient edit diagnostic set (inline in the initiating
  document and appended to Problems with source `edit`), keeps the project unchanged, and leaves dirty state
  untouched.
- `Undo`/`Redo` delegate to `ProjectDocument`; the stack is bounded by `MaxUndoDepth` (default 200, §10), oldest
  entries dropped first. The stack survives saves; the save point is content-based.
- Every successful change raises `ProjectChanged` with the previous/current values and the description. The shell
  refreshes titles, markers, status bar, the Overview and JSON documents, and schedules validation; Step 27's
  viewport will subscribe to the same event. No Core event is added: the coordinator is the single writer.
- Bulk operations are one `Apply` call and therefore one undo entry. Metadata editing is one `Apply` per
  committed field (focus loss or Enter), not per keystroke; an empty project name is refused by
  `ProjectValidator` and surfaces as an inline error.
- Dirty state is computed on change and cached, never on every binding read.

### 3. Validation surface

- The coordinator consumes Step 07's contract in the in-memory form:
  `pipeline.Validate(project, loadDiagnostics: session.LoadDiagnostics, ValidationOptions.Default, compiledModel:
  null, cancellationToken)` — no statistics, no build stage — debounced 300 ms after `ProjectChanged`. It does
  not call domain validators, does not use `ValidateFileAsync`, and the 23A store is already configured with
  `ValidateOnLoad = false`, so exactly one validation runs per load.
- **`Load` stage semantics (M-1, m-14).** The stage is `Completed`/`Failed` only when
  `ProjectLoadResult.Diagnostics` is non-empty (migration or load diagnostics); a clean current-version load
  correctly yields `Skipped` with count 0 per Step 07 §2. The design requires both cases, not an unconditional
  "not skipped".
- Validation runs off the UI thread on an immutable snapshot; the result is applied only if the current document
  hash equals the snapshot hash, otherwise it is discarded as stale. A newer edit cancels the previous run.
- Surfaces: the status-bar validity item (`Not validated` → `Validating…` → `Valid` / `n errors, m warnings,
  k infos`, colour **plus** icon and text); the Problems channel (severity icon, `Code`, `Message`, path
  fields, `Hint`; sortable; severity/code/text filters; help anchors where a code has one; double-click/Enter
  navigates via `NavigateToAsync(path, SelectObject)`; `Related` paths in the context menu; "Revalidate" with a
  timestamp; never mutates the project); an error banner in the Overview; and edit diagnostics from §2.
- Statistics are opt-in: the Overview requests `IncludeStatistics = true` in the background, cached by document
  hash. The Step 07 budgets (200 ms / 2 s for ceria) are non-gating evidence (§14).
- Diagnostics are never invented in the UI: no new codes, no reworded messages; an unknown code renders
  unlinked.

### 4. Dirty state and the save point

- Dirty is content identity: `ProjectDocument.IsDirty` compares the canonical document hash of the current and
  last-saved values. The window title, tab, project switcher, and status bar show the marker.
- **Save point.** A save calls `ProjectDocument.MarkSaved()` (no-arg) so the clean point is the exact in-memory
  value; the store's returned value stays `ProjectSession.LastWritten` for the file stamp and displayed
  timestamp. Consequences, all tested: save → undo → redo is clean; save → edit → undo is clean; save → edit →
  save is clean; undo below the save point is dirty. Excluding `modifiedUtc` from the dirty hash was rejected
  because it would change Step 01's dirty semantics and add a second hash definition.
- **Unsaved changes.** Closing a document or the window with dirty documents prompts Save/Discard/Cancel; with
  several, per project with "apply to all"; Cancel aborts the close and closes nothing. A clean shutdown writes
  `cleanExit = true`; a cancelled or failed shutdown leaves it `false`.

### 5. Autosave

- `AutosaveScheduler` (23B adds the `Scheduling/` folder; 23A has no autosave) runs on a `TimeProvider`
  timer, default 5 minutes, configurable 1–60, disableable via the settings `autosave` block.
- Each tick, for every session with a dirty document:
  - saved project → `ProjectStore.WriteAutosaveAsync(current, path)` (the Step 01 `<file>.autosave` sidecar);
  - untitled project → `ProjectStore.WriteAutosaveToPathAsync(current, recoveryPath)` where
    `recoveryPath = <app-data>/recovery/<projectId>.mocprj.autosave` (§10 addition).
- The immutable snapshot is written asynchronously; the status bar shows "Auto-saving…" then "Auto-saved HH:mm".
  A per-session `SemaphoreSlim(1,1)` serialises save and autosave. Autosave never clears dirty state and never
  updates the save point. On a successful save, the sidecar or recovery file is deleted.
- Read-only sessions never autosave and never write recovery files.

### 6. Recovery and crash handling

- `IRecoveryStore` (list/write/delete) with `RecoveryStore` in `Mocassin.App`; `RecoveryCandidate` and the
  retention policy stay in ViewModels (pure). This is the same contract/implementation split as 23A's settings
  store (n-14: there is no separate `IRecoveryWriter`; the write half is `IRecoveryStore.WriteAsync` and
  `CrashRecoveryWriter` orchestrates it).
- **Recovery** on open or startup: if a sidecar exists and its content differs from the saved file, offer
  Recover / Discard / Decide later through `IProjectSessionService.RecoverAsync`/`DiscardRecoveryAsync`.
  Recovering builds a `ProjectDocument` with the saved file value as the clean point
  (`ProjectDocument.CreateRecovered`, §10), leaves the document dirty, and the next save writes the main file and
  deletes the sidecar. "Decide later" keeps it. Untitled recovery files open as dirty untitled documents that
  prompt for a path on first save.
- **Retention:** recovery files older than 7 days are deleted at startup; log files keep 23A's 14 days/100 MB.
  A `.autosave` sidecar next to a user's project is **never** swept — only a successful save or an explicit
  discard removes it. Projects, packages, results, and job folders are never auto-deleted.
- **Unclean exit:** the settings `session` block records open project paths, the active project, and
  `cleanExit`; if the previous run ended uncleanly, startup offers "Restore session and recover changes" after
  the recovery scan, opening the projects that still exist and reporting the rest.
- **Crash:** the crash dialog gains "Write recovery files". `CrashRecoveryWriter` starts the snapshot writes for
  every dirty session, awaits them with a 2 s budget (`Task.WhenAny` against a `CancellationTokenSource`
  timeout), and reports per-session success/timeout/failure. No `.Result`, no `GetAwaiter().GetResult()`: the
  store exposes only async writes. If the budget expires, the log records unfinished sessions and the app exits
  without claiming they were saved. No automatic restart.

### 7. State ownership and threading additions

| State | Owner | Persisted |
| --- | --- | --- |
| Undo/redo stacks, save point | `ProjectDocument` | memory |
| Dirty state | `ProjectDocument` hash, cached by the coordinator | derived |
| Validation result, statistics cache | per-session `ValidationCoordinator` (subscribes to `ProjectChanged`) | memory |
| Autosave timestamps | `AutosaveScheduler` | memory |
| Recovery snapshots | `IRecoveryStore` | `recovery/` |

Rules: all `ProjectDocument` mutations happen on the UI thread; validation runs off-thread over an immutable
snapshot and is applied only when not stale; `IUiDispatcher`/`TimeProvider` are used exactly as in 23A; long
operations take a `CancellationToken` and never block; the crash path is asynchronous and bounded.

### 8. View-model and view additions

- ViewModels: `Editing/` (`ProjectEditCoordinator`, `EditDiagnosticSet`, `ProjectChangedEventArgs`),
  `Diagnostics/` (`ValidationCoordinator`, `ProblemsViewModel`, `DiagnosticItemViewModel`, `ValidationState`),
  `Recovery/` (`IRecoveryStore`, `RecoveryRetentionPolicy`, `RecoveryCandidate`), `Scheduling/`
  (`AutosaveScheduler`, `Debouncer`), and the Overview metadata form view model.
- Views: `Consoles/ProblemsView`, the Overview metadata form, the recovery prompt dialog, and the crash dialog's
  recovery action. No new shell or settings view.
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
               "cleanExit": true, "savedUtc": "2026-11-20T10:00:00Z" }
}
```

| Key | Type | Default | Rule |
| --- | --- | --- | --- |
| `autosave.enabled` | boolean | `true` | Autosave on/off |
| `autosave.intervalMinutes` | integer | `5` | 1–60 inclusive; out of range falls back to `5` with a warning |
| `session.openProjects` | array of string | `[]` | Paths of the projects open at the last shutdown; missing paths are reported and skipped on restore |
| `session.activeProject` | string or null | `null` | Path of the active project, or null |
| `session.cleanExit` | boolean | `true` | Written `false` while running and set `true` on a clean shutdown; a stale `false` triggers the restore offer |
| `session.savedUtc` | string or null | `null` | ISO-8601 UTC time the session block was written |

Details that follow from the base shape:

- **No `schemaVersion` bump.** Missing keys mean the defaults above, unknown keys stay ignored, and the writer
  still emits `format`/`schemaVersion`. A 23A document without `autosave`/`session` loads unchanged.
- **No new top-level key beyond these two.** No other v1 key changes type, meaning, or default.
- `layout.activeConsoleChannel` may now persist `"problems"` in addition to `"application"`; the 23A
  unknown-channel fallback still applies.
- `recovery/` becomes active in the app-data layout; the 4 MiB cap and atomic write/fsync discipline are
  inherited unchanged.
- The document remains application-local and is **not** in `contracts/manifest.json`.

### 10. Library changes owned by this step

Three additive changes; no App public surface changes.

| Change | Where | Why the shell cannot own it | Contract effect |
| --- | --- | --- | --- |
| `ProjectDocument.MaxUndoDepth` (init-only, default 200) | `Mocassin.Core.Editing` | The stack lives in the document; a UI-level bound would shadow it | Additive property; default preserves Step 01 behaviour |
| `ProjectDocument.CreateRecovered(Project current, Project lastSaved, ProjectEditor? editor = null)` | `Mocassin.Core.Editing` | Recovery needs a clean point different from the current value; `MarkSaved(value)` overwrites both | Additive static factory; constructors unchanged |
| `ProjectStore.WriteAutosaveToPathAsync(Project, string autosavePath, CancellationToken)` | `Mocassin.Packages.Projects` | Untitled projects have no project path to derive `<path>.autosave`; re-implementing the atomic write in the app duplicates Step 01 | Additive overload; reuses `AutosaveFailed`; existing sidecar API unchanged |

All three are listed in `PublicAPI.Unshipped.txt`; no baseline refresh is expected. 23A's `ProjectPathRules`
change is not repeated.

### 11. Dependency and interface checks

- **23A (required):** `ProjectSession`/`ProjectDocument`/`LoadDiagnostics`, the document model and Overview/JSON
  documents, `INavigationService` and target registry, `IExplorerNodeProvider`, `IConsoleChannelProvider`,
  `IStatusBarItemProvider`, `IProjectSessionService`, the settings model/store and `IProjectFileProbe`, and the
  harness. 23B edits these assemblies to fill the seams; it does not redesign them.
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
| `docs/user-guide/projects.md` | undo/redo, dirty state and save point, autosave, recovery, retention |
| `docs/development/ui-architecture.md` | edit pipeline, validation surface, recovery, harness/screenshot/a11y chapters |
| `docs/troubleshooting/installation.md` | recovery prompts and recovery-file problems |
| `docs/reference/file-layout.md` | `recovery/`, the autosave sidecar, retention rules |
| `docs/development/testing-and-parity.md` | screenshot, a11y, recovery and crash-path policy |

No page describes a model editor, build UI, 3D, or run monitoring.

### 13. Test plan

| Area | Tier | Assertion |
| --- | --- | --- |
| Validation contract (M-1/m-14) | PR-fast | With a load/migration fixture the `Load` stage is `Completed`/`Failed` and reaches Problems; with a clean fixture it is `Skipped` with count 0; no duplicate diagnostics; `ValidateFileAsync` is unused; the store has `ValidateOnLoad == false` |
| Undo/redo | PR-fast | Multi-step apply/undo/redo through the Overview document on the shared stack; save → undo → redo clean; save → edit → undo clean; undo below the save point dirty; bounded depth drops the oldest; a new edit clears redo |
| Rejected edit | PR-fast | Validation failure leaves the project and dirty state unchanged and reports diagnostics |
| Validation surface | PR-fast | Debounce coalesces edits; a stale result is discarded; counts/codes match `ValidationResult`; double-click requests a navigation target; an unknown domain produces a notice |
| Dirty/autosave | PR-fast | Only dirty documents are written; the interval comes from `TimeProvider`; save and autosave never interleave; a clean save deletes the sidecar/recovery file; read-only sessions never write |
| Recovery | PR-fast | A simulated crash leaves a sidecar; startup offers recovery; Recover yields a dirty document equal to the snapshot; Discard deletes; Reload replaces the document; retention removes only old recovery files; sidecars are never swept |
| Untitled recovery | PR-fast | An untitled dirty project writes `<app-data>/recovery/<projectId>.mocprj.autosave` and is offered at next start |
| Crash path | PR-fast | The writer completes within its budget or reports a timeout; no blocking call; the dialog reports per-session results |
| Settings additions | PR-fast | The `autosave`/`session` blocks round-trip in the v1 document without a schema bump; a 23A document without them still loads with defaults |
| Screenshots/a11y | PR-medium | Problems view and recovery prompt render; the new status items have names/roles |
| Platform gate | manual (Windows, macOS) | Recovery prompt and crash dialog on each OS |

### 14. Performance expectations (non-gating)

Live validation (ceria, no statistics) < 200 ms and statistics < 2 s are Step 07 design budgets recorded as
evidence, not PR-fast assertions. Autosave of the ceria sample < 500 ms is recorded at feature 3 closeout. No
absolute duration is asserted in a PR-fast test; the only test time limit is a hang guard.

### 15. Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Undo/redo complexity | Reuses the Step 01 `ProjectDocument`; no second stack; save-point tests |
| Autosave/save races | Per-session semaphore; immutable snapshots; atomic writes from the store |
| Validation cost/threading | 300 ms debounce; stale-result guard; off-thread; `ProjectValidationPipeline` is stateless/thread-safe |
| Crash-path hang | Async writes with a 2 s budget and per-session reporting; no sync-over-async |
| Scope creep back into the shell | 23B adds no shell/settings/lifecycle capability; an insufficient seam stops the step for an amendment |
| Platform recovery differences | Paths via `IProjectFileProbe`/`IRecoveryStore`; manual platform checklist |

### 16. Complexity class and split decision

| Dimension | Class | Evidence |
| --- | --- | --- |
| Contracts, formats, and public surfaces | **Medium** | Three additive Core/Packages APIs; the settings additions are additive with no schema bump; no format change |
| New dependencies | **Low** | None; the harness and Avalonia stack come from 23A |
| Cross-step interfaces | **High** | `ProjectEditCoordinator`/`ProjectChanged` and the validation surface are consumed by Steps 24, 25, 26, and 27; a late change rewrites editor code |
| Irreversibility | **Medium** | Undo/save-point and autosave semantics are documented and testable; reversible with a recorded decision; no scientific or persisted-project change |
| Test-matrix breadth and failure modes | **High** | Dirty/save-point, autosave timing, recovery/crash, validation staleness, and platform recovery paths, on top of the headless harness |
| Independent acceptance stories | **Medium** | One coherent story: editing is safe and diagnosable (undo/validation) and recoverable (autosave/recovery); the facets are not separately approvable deliverables |
| Evidence size for a fresh auditor | **Medium** | Four features, no new application project, three additive APIs, seven documentation sections; one sitting is feasible |

**Class: High** (cross-step interface and test-matrix breadth). *Justification:* 23B defines the edit/validation
interfaces that four later steps consume and spans a broad failure matrix (save point, timers, crash, staleness,
platform recovery), while its contracts, dependencies, irreversibility, acceptance story, and evidence size stay
Medium because no format or production dependency changes and the outcome is one editing-safety story.

**Owner acceptance is required** for this High step at its own start gate. **No further split is proposed:**
separating editing from recovery would strand autosave without a dirty state, and separating validation from
editing would leave the Problems channel with nothing to report; both halves would be unusable alone.

### 17. Decisions and open items

| Decision | Record |
| --- | --- |
| 23B extends the 23A shell; it adds no shell/settings/lifecycle capability | This page §1 |
| The edit coordinator is the only write path; no Core event | This page §2; [`05-open-decisions.md`](../05-open-decisions.md) |
| `Load` stage is conditional on load diagnostics (clean load is `Skipped`) | This page §3/§13 |
| Recovery uses `IRecoveryStore` + `CrashRecoveryWriter`; there is no `IRecoveryWriter` type | This page §6 |
| `autosave`/`session` are additive settings fields with no schema bump | This page §9 |
| Three additive library changes and no others | This page §10 |
| Class High; owner acceptance at the start gate; no further split | This page §16 |

Open item for the 23B start gate: confirm that the 23A seams were sufficient in practice (no shell edit needed)
and that the `ProjectDocument` additions are still additive after 23A merged.

### 18. Closeout and audit plan

- **Feature 4 is the only closeout feature.** It owns the 23B documentation column, parity/status updates,
  `docs/planning/closeout/step-23b.json` (`"step": "step-23b"`), and the audit request.
- **Before the freeze:** all four features are squash-merged; current `main` is merged; `./tools/verify.sh all
  --require` passes on the integration tree; one closeout `CI` dispatch (Ubuntu) is recorded, with the Windows
  platform dispatch and the macOS manual gate as for 23A.
- **Closeout identity and prerequisite (M-1):** the evidence file is `docs/planning/closeout/step-23b.json`, its
  `step` field is exactly `"step-23b"`, the tag is `step-23b`, and the audit record is
  `docs/planning/audits/step-23b.md`. There is no `step-23` alias. `tools/check_closeout_evidence.py` must first
  accept `^step-\d{2}[a-z]$` (checker, tests, README and template), which is a separate tooling change landing
  before either closeout; 23B does not work around it.
- **Evidence and audit:** the same canonical closeout evidence and fresh-auditor process as 23A, with
  `audits/step-23b.md` created on demand at that point, not pre-seeded. Merge with `--no-ff` and tag `step-23b`
  only after a `Passed` audit, owner approval, the tooling prerequisite, and a green automatic `main` CI run.
- **Design review:** 23B has no review record yet. One is created from the template when the step is scheduled;
  this page is refrozen at that point and the record carries the freeze SHA.
- **Dashboard:** the step agent updates the `23B` row when the step starts; the planner does not edit
  `status.md`.

## Implementation tasks

- [ ] Implement `ProjectEditCoordinator` with apply/undo/redo, bounded depth, `ProjectChanged`, rejected-edit
      diagnostics, and the Overview metadata editor.
- [ ] Implement `ValidationCoordinator`, the validity status item, the Problems channel (filters, sort, help
      anchors, related navigation, revalidate), edit diagnostics, and on-demand statistics.
- [ ] Implement dirty state, the save point, unsaved-changes prompts, the autosave scheduler, the sidecar and
      untitled recovery, the recovery prompt, Reload/Recover/Discard/Keep, session restore, and retention.
- [ ] Implement `IRecoveryStore`, `RecoveryStore` (App), `CrashRecoveryWriter`, and the crash-dialog recovery
      action.
- [ ] Add the three library changes with tests and `PublicAPI.Unshipped.txt` entries; verify no App API change.
- [ ] Add the 23B tests, documentation column, parity/status updates, `closeout/step-23b.json`, and request the
      audit.

## Acceptance criteria

- [ ] Metadata editing works with undo/redo across the save point in both directions; a rejected edit changes
      nothing.
- [ ] The validity indicator and Problems channel show code, severity, and path; double-click navigates through a
      registered provider and produces a notice otherwise; a load/migration fixture reaches Problems and a clean
      load leaves the `Load` stage `Skipped`.
- [ ] Dirty state is correct across save, undo, and redo; closing with unsaved changes prompts.
- [ ] Autosave writes only dirty documents and deletes its file on save; crash recovery restores a dirty document
      for saved and untitled projects; the crash path completes within its budget or reports the timeout.
- [ ] Recovery files are swept after 7 days; sidecars are never swept; unclean-exit session restore works.
- [ ] The `autosave`/`session` settings fields round-trip without a schema bump and older documents still load.
- [ ] No unhandled exception kills the process silently, and the crash dialog reports recovery results.
- [ ] `./tools/verify.sh all --require` passes, the strict docs build passes, and `closeout/step-23b.json` with
      `"step": "step-23b"` validates once the `step-NN[a-z]` checker support has landed.
- [ ] Documentation written (§12), and no page claims a later step's capability.

## Documentation

See §12. These sections are added to the pages 23A created; 23B does not create the base pages.

## Tests

See §13. Required evidence: real-object undo/save-point tests, both `Load`-stage fixtures, dirty/autosave and
recovery tests with `ManualTimeProvider`, the crash-path budget test, settings-addition round-trips, and the
manual platform recovery check.

## Risks and mitigations

See §15.

## Done when

Editing is safe and diagnosable: metadata edits are undoable across the save point, validation is live and
navigable, dirty state and unsaved prompts are correct, autosave and crash recovery restore the exact in-memory
project, and the shell delivers all of it without a 23A redesign — with every acceptance criterion, test tier,
documentation section, parity/status update, closeout evidence, and audit complete for 23B alone.
