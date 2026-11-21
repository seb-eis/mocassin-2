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
command-line value wins. Subdirectories are `logs/`, `cache/`, and `recovery/`. The application writes logs,
settings, and the `recovery/` crash/fallback snapshots; logs and cache are disposable, while a `.autosave`
sidecar next to a user project and the settings document are user state.

## The settings dialog

**View → Settings…** (`Ctrl+,`) edits:

- theme (`system`, `light`, `dark`);
- accent from a small fixed palette, or the Fluent default;
- density (`comfortable`, `compact`);
- font scale `0.85`–`1.5`;
- high contrast and reduce motion;
- the minimum log level;
- external-change confirmation and welcome-on-start;
- **Enable autosave** and the interval in minutes (1–60);
- the recent-file list (clear all);
- **Open config folder** and **Clear logs and cache**.

**Reset to defaults** changes the dialog state; **Save** validates, writes atomically, and applies the theme,
accent, density, font scale, contrast, and motion resources without restarting. A failed write leaves the previous
file intact, keeps the dialog open, and shows an error banner; a successful save shows a short status banner.

## Autosave and session blocks

The settings document gains two additive top-level objects, with **no schema bump**:

```json
{
  "autosave": { "enabled": true, "intervalMinutes": 5 },
  "session": {
    "openProjects": ["/work/ceria.mocprj"],
    "activeProject": "/work/ceria.mocprj",
    "cleanExit": true,
    "savedUtc": "2026-11-21T10:00:00Z"
  }
}
```

`autosave.enabled` toggles periodic sidecar writes; `autosave.intervalMinutes` is 1–60 (out of range falls back
to 5 with a warning). `session.openProjects` is capped at 20, de-duplicated with the platform path comparison, and
`session.activeProject` is normalized to one of those entries or null. `cleanExit` is written false while the
application is running and true by a clean window close; a stale false produces the restore offer at the next
startup. `savedUtc` records the write time. These fields are application-local, are not in
`contracts/manifest.json`, and have no cross-language fixtures.

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

The theme variant follows the selection; `system` follows the platform. Both themes use the shared `Mocassin.*`
role palette from [UI visual contract](../development/ui-visual-contract.md): light is the Light+ equivalent,
dark uses the owner-approved `#1e1e1e`/`#252526`/`#2d2d30` surfaces, and high contrast overrides every role and
control resource with black, yellow, and white plus 2 px borders. Accent keys map to fixed accessible colours;
the default and `blue` are `#007ACC`, and the app derives a contrast-safe foreground for every optional accent.
Font scale changes every text token. Density changes spacing and control heights only.
`AvaloniaThemeApplier` publishes the runtime values; the palette, density, and high-contrast dictionaries remain
the declarative source. The appearance is applied by the shell, not by scientific code.

## Layout defaults and reset

The shell starts at 20 % projects, 22 % inspector, and 32 % console. These values are stored as fractions in the
`layout` block and are also written with `layout.defaultsRevision`. A saved document from before the refresh is
upgraded once when the document has no revision marker **and** all three fractions still equal the old 260/320/180
look; a custom layout is preserved. **View → Reset Layout** restores the new default shares, shows every region,
and persists them. It does not change the theme, density, recent files, or active console tab. See
[Repository and file layout](../reference/file-layout.md#mocassinsettings-v1) for the exact fields.

## Logs

Log files roll daily under `<app-data>/logs/mocassin-YYYYMMDD.log` with a 14-file/100 MB retention policy. The
active file is never deleted by **Clear logs and cache**. The Application console shows the same structured
messages at the configured minimum level.
