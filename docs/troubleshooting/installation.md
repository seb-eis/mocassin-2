# Installation and Startup Troubleshooting

## The window does not appear

- Verify a desktop session with X11 or Wayland is available on Linux. The headless test suite runs without a
  display, but the real window does not.
- Start from a terminal and inspect the log under `<app-data>/logs/`.
- Confirm the .NET SDK matches `global.json` (`dotnet --version`).
- If the settings file is rejected, the Application console and log show the reason; the application continues
  with defaults.

## The data directory is not writable

Use `--data-dir <writable-path>` or set `MOCASSIN_DATA_DIR` to a writable directory. The application creates the
root, `logs/`, `cache/`, and `recovery/` directories. A settings write failure leaves the previous file intact.

## The settings file is corrupt or from a newer version

A wrong `format`, malformed JSON, an oversized document, or a future `schemaVersion` is rejected without being
overwritten. The shell uses documented defaults and reports the rejection in the Application console. Open
**View → Settings…** to save a new valid file, or remove/rename the old file while the application is closed.

## The project is read-only or locked

A `.mocprj.lock` file owned by another session makes the project read-only. The Overview document shows the lock
path. Close the other session; the shared store reclaims a stale lock after its timeout. Save As acquires a new
lock and remains available from a read-only session.

## Save refuses because the file changed

The project changed on disk after it was opened. Choose **File → Reload Project** to replace the in-memory
document, or **Save Project As…** to write a new file. Step 23A never overwrites an externally modified file
silently.

## The project path is missing at startup

`--project` with a missing or invalid path does not stop the application. The failure appears in the Application
console and the welcome flow remains available.

## Logs and cache cleanup

Use **View → Settings… → Clear logs and cache**. It removes closed log files and cache files but keeps the active
log. Project, package, results, and job-folder paths are never touched.

See [Settings and appearance](../user-guide/settings.md) and
[Repository and file layout](../reference/file-layout.md#application-data-layout).
