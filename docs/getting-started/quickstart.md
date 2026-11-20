# Quickstart

This quickstart uses the desktop shell and the same project files as the CLI. It covers the workspace and
project lifecycle delivered by Step 23A. Editing, validation surfaces, autosave, and recovery are later work.

## Create a project

1. Start the application and choose **File → New Project…** (`Ctrl+N`).
2. Pick a target folder.
3. Enter a project name. A trailing `.mocprj` is optional.
4. Choose the **Blank** or **Ceria** starter.

The project is written through the real project store and opens in a project-scoped Overview tab. New never
overwrites an existing file; the same name, path, and message rules are used by `mocassin new`.

## Open a project

Choose **File → Open Project…** (`Ctrl+O`) and select a `.mocprj` file. Opening a file that is already open
activates its existing session instead of creating a second one. The open path and the last directory are
recorded in the recent-file list and application settings.

## Inspect the workspace

- **Projects** contains the project switcher and the project-level explorer (Overview and Data).
- **Documents** contains the tab strip. Each project has its own Overview and Project JSON tabs.
- **Inspector** shows a short summary for the active document.
- **Console** shows the Application channel.
- **Status** shows the active document, project path/lock state, appearance, and version.

The Project Overview and Project JSON documents are read-only in this step. The JSON document is the canonical,
indented project document and can be copied to the clipboard.

## Save and close

- **File → Save Project** (`Ctrl+S`) writes through the atomic store.
- **File → Save Project As…** (`Ctrl+Shift+S`) writes to a new path and migrates the project lock.
- **File → Reload Project** (`F5`) re-reads the file.
- **File → Close Project** releases the lock and closes the project's documents.

There is no unsaved-changes prompt in Step 23A because nothing is editable. The prompt and dirty markers arrive
with Step 23B.

## Locks and read-only mode

A project opened by another session is shown read-only with the lock path in the Overview document. Save is
unavailable; Save As remains available and acquires a new lock. Stale locks are reclaimed by the shared store
after its configured timeout.

If the file changes on disk, Save refuses to overwrite it. Reload replaces the in-memory document; Save As writes
a new file.

## Settings and appearance

Choose **View → Settings…** (`Ctrl+,`) to change theme, accent, density, font scale, high contrast, reduced motion,
the log level, external-change confirmation, the welcome document, and the recent-file list. Settings are saved
atomically and applied without restarting the application.

See [Settings and appearance](../user-guide/settings.md) and the
[project lifecycle reference](../user-guide/projects.md).
