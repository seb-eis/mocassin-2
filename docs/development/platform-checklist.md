# Platform Checklist

This checklist records the cross-platform verification for the Avalonia workspace. The automated headless
suite runs without a display; the manual real-window checklist is executed on a desktop session.

## Automated platform gate

`CI` → `Run workflow` → `platforms=windows` or `platforms=macos` runs the focused platform job:

- restore and Release build of `Mocassin.slnx`;
- `Mocassin.App.Tests` (Avalonia headless shell, settings, lifecycle, accessibility, screenshots where the
  platform supports pixel comparison);
- `Mocassin.Packages.Tests`, `Mocassin.Results.Tests`, and `Mocassin.Cli.Tests` for file, path, lock, and CLI
  signal paths.

Ubuntu runs the complete suite, including the screenshot comparison. Windows and macOS do not pixel-compare
screenshots because text rendering differs; their screenshot evidence is manual and recorded with the platform
run.

Dispatch the Ubuntu, Windows, and macOS runs one at a time. `ci.yml` uses the concurrency group
`ci-${{ github.ref }}` with `cancel-in-progress: true`, so a second dispatch for the same ref cancels the run
already in progress.

| Runner | Automated headless suite | Screenshot comparison | Real-window checklist |
| --- | --- | --- | --- |
| Ubuntu | Yes — full suite | Yes — checked-in light/dark/high-contrast baselines with a 5 % pixel tolerance | Manual desktop session |
| Windows | Yes — focused platform job | No (manual platform evidence) | Manual desktop session |
| macOS | Yes — focused platform job | No (manual platform evidence) | Manual desktop session |

## Manual real-window checklist

Run once on a desktop Windows, Linux, and macOS session:

1. Start the app; confirm the welcome tab, menu, four regions, and status bar.
2. `Ctrl+N`: create a blank project; confirm the Overview and JSON tabs.
3. Create a ceria project; switch projects and confirm the last document is restored.
4. `Ctrl+S`, `Ctrl+Shift+S`, `F5`, and Close; confirm the lock and last-saved values.
5. Open a second instance of the same project; confirm the read-only banner and disabled Save.
6. Change the file externally; confirm Save refuses and Reload recovers.
7. Open Settings; change theme, accent, density, font scale, high contrast, and reduce motion; confirm the
   change is visible and survives a restart.
8. Confirm a rejected future-major settings file is preserved and reported.
9. Use the command palette (`Ctrl+Shift+P`) and shortcut sheet (`F1`); reach every primary action by keyboard.
10. Open a recent project; confirm missing entries are disabled and removable.
11. Trigger a controlled test exception in a debug build; confirm the crash dialog, log entry, and exit code.
12. Record the OS, runner/desktop version, app commit, and any rendering differences in the step page or audit
    evidence.

## Recording

A platform run is evidence only when its run ID, `head_sha`, platform, and conclusion are recorded in the
closeout evidence. The manual checklist is recorded in the step page/audit request with the date and tester.
