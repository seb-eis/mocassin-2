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
- Save is unavailable;
- the lock path is shown in the Overview document.

A stale lock is reclaimed by the shared store after its configured stale-lock timeout. A missing, oversized,
unreadable, or invalid file produces a typed outcome (`NotFound`, `TooLarge`, `Io`, `InvalidContent`) and a
diagnostic; the shell does not crash.

`--project <path>` opens a project at startup. A missing or invalid startup path is reported in the Application
console and the shell continues with the welcome flow.

## Save and save-as

**Save** writes through the project store, including its atomic temporary-file replacement, and updates the
document's save point and last-written timestamp. The Overview's last-saved value and the file stamp refresh.

**Save As** writes to a new path and migrates the lock: the new lock is acquired before the old lock is released.
A target owned by another open session is refused. Unlike New, Save As may replace an existing file after the
operating-system picker's overwrite confirmation.

## Reload and external changes

The session records a cheap file stamp (length and last-write time). The shell checks it on activation and before
Save:

| Change | Behaviour |
| --- | --- |
| Unchanged | Save proceeds. |
| Modified by another program | Save refuses to overwrite; Reload or Save As is required. |
| Deleted | Save refuses; Save As is required. |

Reload re-reads the file and replaces the project document. No file-system watcher is used.

## Recents and last directories

The settings file stores at most ten recent project paths and the last open, save, and folder directories.
Recent entries are de-duplicated and recorded when a project is opened or saved-as. The welcome document lists
them with **Open** and **Remove** actions; a missing file is disabled and marked “Missing”. The settings dialog
also provides a clear-all action. Removing an entry never deletes a project file.

## Close

Close releases the project lock, closes the project's documents, and removes the project from the switcher. Step
23A does not prompt because no document is editable. Step 23B adds dirty tracking, the unsaved-changes prompt,
autosave, and recovery.

## Validation

Opening a project performs exactly one internal load-validation pass: the store loads with validation disabled,
and the session passes `ProjectLoadResult.Diagnostics` to the shared validation pipeline. A clean load has a
skipped `Load` stage; diagnostics produce a completed/failed `Load` stage. The validation *surface* — status
indicator, Problems channel, filters, and editable diagnostics — is Step 23B work.
