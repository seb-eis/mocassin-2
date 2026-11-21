# Settings and Appearance

The desktop application stores one versioned settings document at
`<app-data>/Mocassin.Settings.json`. It is application-local: it is not part of the project format, the contract
manifest, or the package/results formats.

## Where the settings live

The application-data root is the platform local-application-data directory plus `Mocassin`:

| Platform | Default root |
| --- | --- |
| Windows | `%LOCALAPPDATA%\Mocassin` |
| Linux | `~/.local/share/Mocassin` |
| macOS | `~/Library/Application Support/Mocassin` |

`--data-dir` overrides the root for one run; `MOCASSIN_DATA_DIR` provides an environment override, and the
command-line value wins. Subdirectories are `logs/`, `cache/`, and the reserved `recovery/`. Step 23A writes logs
and settings; `recovery/` is reserved for Step 23B.

## The settings dialog

**View → Settings…** (`Ctrl+,`) edits:

- theme (`system`, `light`, `dark`);
- accent from a small fixed palette, or the Fluent default;
- density (`comfortable`, `compact`);
- font scale `0.85`–`1.5`;
- high contrast and reduce motion;
- the minimum log level;
- external-change confirmation and welcome-on-start;
- the recent-file list (clear all);
- **Open config folder** and **Clear logs and cache**.

**Reset to defaults** changes the dialog state; **Save** validates, writes atomically, and applies the theme,
accent, density, font scale, contrast, and motion resources without restarting. A failed write leaves the previous
file intact, keeps the dialog open, and shows an error banner; a successful save shows a short status banner.

## Read and write policy

- Unknown properties are ignored and dropped on the next write.
- A missing key takes its documented default.
- A wrong type, unknown enum, or out-of-range value falls back to that field's default and records a warning.
- A wrong `format` or a future major is rejected without overwriting the file; the shell uses defaults, reports
  the rejection in the Application console, and shows the preserved-file status in the settings dialog.
- Migrations are pure functions keyed by `schemaVersion`; version 1 is the current identity migration.
- The document is capped at 4 MiB and the recent list at ten entries.
- Writes use a sibling temporary file, flush to disk, and replace the target. A failed or oversized write leaves
  the previous file intact.

The complete field table and example are in
[Repository and file layout](../reference/file-layout.md#mocassinsettings-v1).

## Appearance

The theme variant follows the selection; `system` follows the platform. Accent keys map to fixed accessible
colours. Font scale changes the base text size. Density, high contrast, and reduce motion are published as
application resources for the shell styles; the full visual-tree sweep and screenshot baselines are a later
accessibility/visual task, so this page does not claim pixel-verified high-contrast rendering. The settings and
resources are applied by the shell, not by scientific code.

## Logs

Log files roll daily under `<app-data>/logs/mocassin-YYYYMMDD.log` with a 14-file/100 MB retention policy. The
active file is never deleted by **Clear logs and cache**. The Application console shows the same structured
messages at the configured minimum level.
