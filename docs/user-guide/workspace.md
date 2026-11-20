# Workspace, Tabs, and Commands

The shell is a fixed, resizable workspace rather than a docking system.

## Regions

| Region | Contents |
| --- | --- |
| Projects | Project switcher and project-level explorer. |
| Documents | Tab strip and the active document. |
| Inspector | Summary for the active document or a neutral empty state. |
| Console | Application channel with severity/text filtering and clear. |
| Status bar | Active document, project/path/lock state, appearance, version, and progress. |

`Alt+1`, `Alt+2`, and `Alt+0` toggle Projects, Inspector, and Console. Region visibility and the stored
`layout` width/height values are applied from the settings block. Manual splitter resizing does not yet feed back
into the settings file; this is recorded as a Step 23A limitation. The console and inspector regions are
collapsible.

## Documents and tabs

A document has a stable key `(Kind, ProjectScopeId)`. The scope is the project id, so two open projects can each
have an Overview or Project JSON tab.

- Opening an existing key activates its tab instead of creating a duplicate.
- The welcome tab closes when the first real document opens.
- A tab has a title, a path tooltip, a close button, a context menu, and keyboard access.
- `Ctrl+W` closes the active document; `Ctrl+Tab`/`Ctrl+Shift+Tab` move to the next/previous tab.
- `Ctrl+1`–`Ctrl+9` activate a tab by position.
- Middle-click closes a tab.

Drag-reorder between tab positions is not wired in the Avalonia tab strip yet. The document collection already
exposes `Move(from, to)`, but the drag gesture and its accessibility story need a dedicated tab-strip control;
this is recorded as a Step 23A limitation rather than a silent omission.

## Project explorer and switcher

The switcher lists open projects with their path, lock state, and active document. Activating a project restores
its last active document. The explorer currently exposes project-level nodes:

- **Overview** — read-only project identity, metadata, seed, schema/symmetry information, file path, and lock
  state.
- **Data (JSON)** — the canonical indented project JSON, read-only, with copy.

Later steps add model, parameterization, job-template, build, and results nodes through the same provider seam.

## Commands and palette

The command catalog is explicit and duplicate ids are rejected. The palette searches every registered command by
title, id, category, or gesture. The shortcut cheat sheet groups commands with gestures.

Framework-neutral gestures use a `Primary` modifier that maps to `Ctrl` on Windows/Linux and `Cmd` on macOS. The
view layer translates the same descriptor into Avalonia key bindings, so menus, the palette, and the cheat sheet
show the same gesture.

The shell currently registers File/View/Help menus; an empty Edit menu is omitted until editing commands exist.
`Model`, `Build`, and `Run` menus appear only when later steps contribute items.
