# Projects and Lifecycle

A MOCASSIN project is one versioned UTF-8 JSON file (`*.mocprj`). The workspace can hold several open projects
at once; each project has its own tabs and lock. The file format is documented in
[Project format](../reference/project-format.md).

## New project

**File → New Project…** asks for a target directory, a name, and a starter (`Blank` or `Ceria`). The shared
`ProjectPathRules` apply the same rules as `mocassin new`:

- the name is trimmed; one trailing `.mocprj` is removed;
- the name must not be empty, longer than 128 characters, contain path separators, contain `..`, contain control
  characters, or contain `: * ? " < > |`;
- a target that already exists is refused; New never overwrites.

The project is created through `ProjectTemplateFactory`, assigned a fresh UUIDv7 identity and master seed, and
written atomically. The GUI and the CLI use the same message text; the CLI keeps its `CLI001`/`CLI002`/`CLI003`
codes and exit codes.

## Open and activate

**File → Open Project…** opens a `.mocprj`. An already-open path activates its existing session. Opening a
project acquires its `.lock` file. If another session owns the lock, the project opens read-only:

- the project can be inspected and save-as'd;
- Save is unavailable and the metadata form is disabled;
- an edit attempt is refused with the app-level “This project is open read-only; changes cannot be saved.”
  message and emits no diagnostic code;
- the lock path is shown in the Overview document.

A stale lock is reclaimed by the shared store after its configured stale-lock timeout. A missing, oversized,
unreadable, or invalid file produces a typed outcome (`NotFound`, `TooLarge`, `Io`, `InvalidContent`) and a
diagnostic; the shell does not crash.

`--project <path>` opens a project at startup. A missing or invalid startup path is reported in the Application
console and the shell continues with the welcome flow.

## Save and save-as

**Save** writes through the project store, including its atomic temporary-file replacement, and updates the
document's save point and last-written timestamp. The Overview's last-saved value and the file stamp refresh.
Saving clears the dirty markers and deletes the `.autosave` sidecar and any recovery snapshot when no deferred
recovery candidate is pending. A save with a deferred sidecar candidate preserves that candidate until Recover or
Discard.

**Save As** writes to a new path and migrates the lock: the new lock is acquired before the old lock is released.
A target owned by another open session is refused. Unlike New, Save As may replace an existing file after the
operating-system picker's overwrite confirmation. The old path's sidecar is removed after a successful move.

## Reload and external changes

The session records a cheap file stamp (length and last-write time). The shell checks it on activation and before
Save:

| Change | Behaviour |
| --- | --- |
| Unchanged | Save proceeds. |
| Modified by another program | Save refuses to overwrite; Reload or Save As is required. |
| Deleted | Save refuses; Save As is required. |

Reload re-reads the file and replaces the project document. No file-system watcher is used. Reloading a dirty
project prompts “Reload and discard changes?” with Reload/Keep; Keep leaves the in-memory document untouched and
Save As remains the recovery path.

## Recents and last directories

The settings file stores at most ten recent project paths and the last open, save, and folder directories.
Recent entries are de-duplicated and recorded when a project is opened or saved-as. The welcome document lists
them with **Open** and **Remove** actions; a missing file is disabled and marked “Missing”. The settings dialog
also provides a clear-all action. Removing an entry never deletes a project file.

## Overview metadata and undo/redo

The Overview document edits the project name, description, author, and notes through the shared edit pipeline.
One committed field is one undo entry; focus loss or Enter commits, not each keystroke. The name follows
`ProjectPathRules` (trimmed, one `.mocprj` removed, maximum 128 characters, no separators/`..`/control/reserved
characters); an invalid or empty name shows an inline error and leaves the project unchanged. A structurally
rejected edit keeps the rejected text in the field for correction. The Edit menu exposes Undo/Redo across the
save point; saving, undoing, and redoing in either direction leaves the document clean exactly when it matches
the save point.

The Overview also displays the validation banner and an explicit **Show/Hide statistics** action. Statistics are
never computed on open; the first activation runs one validation/statistics pass and caches it for the current
content. Validation runs automatically after edits with a 300 ms debounce; the Problems channel always shows the
active project only.

## Dirty state and close

The title bar, tab, project switcher, and status bar show `*`/“Unsaved changes” for a dirty project. Closing a
dirty project prompts Save / Discard / Cancel:

- **Save** writes the project, then closes it;
- **Discard** closes it and deletes its sidecar/recovery snapshot;
- **Cancel** aborts the close and leaves autosave running.

The prompt is per project. Closing the window uses the same per-project decisions; Cancel keeps the window open.
A clean window close writes `cleanExit = true`; a cancel or failed shutdown leaves it `false`.

## Autosave and recovery

Autosave is enabled by default every 5 minutes (configurable 1–60 minutes in Settings). Only writable dirty
documents are written; the scheduler skips a busy or closing session. A saved project writes `<file>.autosave`;
if that write fails, a crash/fallback snapshot is written under `<app-data>/recovery/`. Autosave never clears
dirty state or moves the save point. On a successful save, a return to the save point, a clean close, or an
explicit discard, the sidecar and recovery snapshot are removed — except while a deferred recovery candidate is
pending. A no-op edit does not clear a deferred candidate; the first real content edit does.

When a project opens with a content-different sidecar or `recovery/` manifest, the shell offers **Recover**,
**Discard**, or **Decide later**. Recover loads the snapshot as a dirty document with the saved file as its clean
point and recomputes its load diagnostics/validation before the document is replaced; the next save writes the
main file and deletes the snapshot. Discard deletes the candidate. Decide later preserves it and shows
“Recovery available”; a deferred `recovery/` candidate can expire after seven days (an accepted limitation), while
a `.autosave` sidecar is never swept.

After an unclean exit, startup offers “Restore session and recover changes” after the recovery scan. It opens the
remembered projects that still exist, applies their recovery candidates automatically, reports missing paths, and
restores the active project. A clean exit does not offer restore. If the main project file is missing, an ordinary
open reports the load failure; session restore reports the missing path and leaves the recovery file for manual
handling or a later discard (a dedicated missing-file recovery dialog is not provided).

## Validation

Opening a project performs exactly one internal load-validation pass: the store loads with validation disabled,
and the session passes `ProjectLoadResult.Diagnostics` to the shared validation pipeline. A clean load has a
skipped `Load` stage; diagnostics produce a completed/failed `Load` stage. The status bar and Problems channel
consume that retained result without running the pipeline again. Subsequent edits are validated off-thread with
a 300 ms debounce and a stale-result guard. The Problems channel supports filters, sorting, help anchors,
`Related` navigation, revalidate, and transient edit diagnostics; double-click/Enter navigates through the
registered target provider or shows the normal “not available” notice.
