# Your First Project

This page walks through the workspace that Step 23A provides. It is deliberately read-only: the model editors,
validation surface, undo/redo, autosave, and recovery belong to later steps.

## Starters

| Starter | Contents |
| --- | --- |
| `Blank` | A valid empty project with no model content. |
| `Ceria` | The ceria model definition used by the samples and tutorials, without a job template. |

Both starters come from the same embedded `ProjectTemplateFactory` used by `mocassin new`. A new project gets a
fresh UUIDv7 identity, a fresh master seed, the requested name, and fresh timestamps.

## Workspace regions

The shell uses fixed, resizable regions:

```text
Menu
┌──────────────┬────────────────────────────────┬──────────────┐
│ Projects     │ Documents (tabs)               │ Inspector    │
│ switcher     │                                │              │
│ explorer     │                                │              │
├──────────────┴────────────────────────────────┴──────────────┤
│ Console: Application                                         │
├──────────────────────────────────────────────────────────────┤
│ Status: document · project/path · appearance · version       │
└──────────────────────────────────────────────────────────────┘
```

- `Alt+1`, `Alt+2`, and `Alt+0` toggle the Projects, Inspector, and Console regions. Visibility is persisted in
  the layout settings.
- The project switcher activates an open project and its last document.
- The explorer opens the project-level Overview and Data nodes; later steps contribute model nodes.

## Tabs

- Open a document from the welcome page, the explorer, or the command palette.
- Opening the same project-scoped key activates the existing tab.
- The close button, middle-click, `Ctrl+W`, and the tab context menu close a tab.
- `Ctrl+Tab` and `Ctrl+Shift+Tab` move to the next/previous tab; `Ctrl+1`–`Ctrl+9` activate a tab by position.
- A tab shows the document title and its full path as a tooltip.

## Commands and keyboard

The command palette (`Ctrl+Shift+P`) searches every registered shell command by title, id, category, or gesture.
The shortcut cheat sheet (`F1`) groups the keyboard-reachable commands by category. `Primary` gestures map to
`Ctrl` on Windows/Linux and `Cmd` on macOS.

Common gestures:

| Action | Gesture |
| --- | --- |
| New project | `Primary+N` |
| Open project | `Primary+O` |
| Save project | `Primary+S` |
| Save project as | `Primary+Shift+S` |
| Command palette | `Primary+Shift+P` |
| Settings | `Primary+,` |
| Close document | `Primary+W` |
| Back/forward navigation | `Alt+Left` / `Alt+Right` |
| Reload project | `F5` |

## Next steps

- Read the [project lifecycle reference](../user-guide/projects.md) for locks, recents, and external changes.
- Read [Settings and appearance](../user-guide/settings.md) for the settings file and platform layout.
- Use the [CLI quickstart](../getting-started/quickstart.md) for the headless workflow.
