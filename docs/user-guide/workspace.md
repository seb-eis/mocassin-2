# Workspace, Tabs, and Commands

The shell is a fixed, resizable workspace rather than a docking system.

## Regions

| Region | Contents |
| --- | --- |
| Projects | Project switcher and project-level explorer. |
| Documents | Tab strip and the active document. |
| Inspector | Summary for the active document or a neutral empty state. |
| Console | Application and Problems channels with a selector; the Application channel has severity/text filtering and clear. |
| Status bar | Active document, project/path/lock and dirty state, validation status, autosave status, appearance, version, and progress. |

`Alt+1`, `Alt+2`, and `Alt+0` toggle Projects, Inspector, and Console. Region visibility and the stored
`layout` width/height values are applied from the settings block. Manual splitter resizing does not yet feed back
into the settings file; this is recorded as a Step 23A limitation. The console and inspector regions are
collapsible.

## Documents and tabs

A document has a stable key `(Kind, ProjectScopeId)`. The scope is the project id, so two open projects can each
have an Overview or Project JSON tab.

- Opening an existing key activates its tab instead of creating a duplicate.
- The welcome tab closes when the first real document opens.
- A tab has a title, a path tooltip, a dirty `*` marker, a close button, a context menu, and keyboard access.
- The window title and the project switcher also mark the active project with `*` while it has unsaved changes.
- `Ctrl+W` closes the active document; `Ctrl+Tab`/`Ctrl+Shift+Tab` move to the next/previous tab.
- `Ctrl+1`–`Ctrl+9` activate a tab by position.
- Middle-click closes a tab.

Drag-reorder between tab positions is not wired in the Avalonia tab strip yet. The document collection already
exposes `Move(from, to)`, but the drag gesture and its accessibility story need a dedicated tab-strip control;
this is recorded as a Step 23A limitation rather than a silent omission.

## Project explorer and switcher

The switcher lists open projects with their path, lock state, dirty marker, and last active document. Activating
a project restores its last active document. A read-only project also shows a banner above the workspace. The
explorer currently exposes project-level nodes:

- **Overview** — editable project name, description, author, and notes through the shared edit pipeline, plus
  project identity, seed, schema/symmetry information, file path, lock state, validation banner, and the explicit
  statistics panel.
- **Data (JSON)** — the canonical indented project JSON, read-only, with copy; it refreshes after a content
  change and does not reload on a save-only event.

Later steps add model, parameterization, job-template, build, and results nodes through the same provider seam.

## Commands and palette

The command catalog is explicit and duplicate ids are rejected. The palette searches every registered command by
title, id, category, or gesture. The shortcut cheat sheet groups commands with gestures.

Framework-neutral gestures use a `Primary` modifier that maps to `Ctrl` on Windows/Linux and `Cmd` on macOS. The
view layer translates the same descriptor into Avalonia key bindings, so menus, the palette, and the cheat sheet
show the same gesture.

The shell registers File, Edit, View, and Help menus. The Edit menu contains Undo (`Ctrl+Z`) and Redo (`Ctrl+Y`),
which follow the active project and are disabled for a read-only project or an empty stack. `Model`, `Build`, and
`Run` menus appear only when later steps contribute items.

## Console channels

The console selector switches between the Application channel and the Problems channel. Problems shows the
active project's validation and transient edit diagnostics with severity, code, message, path, and source;
it supports severity/code/text/source filters, sorting, revalidate, help links, and navigation. Opening a project
with a retained load/migration result shows it immediately; opening a clean project leaves the `Load` stage
skipped. See [Projects and lifecycle](projects.md#validation) for details.
