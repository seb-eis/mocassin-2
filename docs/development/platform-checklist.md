# Platform Checklist

This page records cross-platform verification for the Avalonia workspace. It covers the automated headless
gates, the automated Xvfb real-window capture, and the optional manual spot-check.

## Automated platform gate

`CI` → `Run workflow` → `platforms=windows` or `platforms=macos` runs the focused platform job:

- restore and Release build of `Mocassin.slnx`;
- `Mocassin.App.Tests` (Avalonia headless shell, settings, lifecycle, accessibility, screenshots where the
  platform supports pixel comparison);
- `Mocassin.Packages.Tests`, `Mocassin.Results.Tests`, and `Mocassin.Cli.Tests` for file, path, lock, and CLI
  signal paths.

Ubuntu runs the complete suite, including the screenshot comparison. Windows and macOS do not pixel-compare
screenshots because text rendering differs.

Dispatch the Ubuntu, Windows, and macOS runs one at a time. `ci.yml` uses the concurrency group
`ci-${{ github.ref }}` with `cancel-in-progress: true`, so a second dispatch for the same ref cancels the run
already in progress.

| Runner | Automated headless suite | Screenshot comparison |
| --- | --- | --- |
| Ubuntu | Yes — full suite | Yes — checked-in light/dark/high-contrast baselines with a 5 % pixel tolerance |
| Windows | Yes — focused platform job | No — rendering-dependent |
| macOS | Yes — focused platform job | No — rendering-dependent |

## Automated Xvfb real-window evidence (automated, not manual)

The real desktop shell is started on the Avalonia X11 platform under Xvfb. The harness drives appearance,
workspace, dialog, and lifecycle steps and fails if any assertion or screenshot is missing. **This is automated
Xvfb evidence, not a manual human checklist.** The human owner accepted it in place of the manual real-window
checklist for Step 23A on 2026-11-21.

Command (from the repository root, .NET 11 SDK, Debian 13):

```sh
rm -rf artifacts/xvfb && mkdir -p artifacts/xvfb
xvfb-run -a -s "-screen 0 1280x800x24" \
  env MOCASSIN_XVFB_CAPTURE="$PWD/artifacts/xvfb" \
  dotnet test src/dotnet/Mocassin.App.Tests/Mocassin.App.Tests.csproj -c Debug \
  --filter "FullyQualifiedName~XvfbUiSmokeTests"
```

The test is inert unless `MOCASSIN_XVFB_CAPTURE` is set, so normal `dotnet test` runs skip it.

Recorded run:

| Field | Value |
| --- | --- |
| Date (UTC) | 2026-11-21T09:15:51Z |
| OS | Debian GNU/Linux 13 (trixie), kernel 6.12.116.13 |
| Runtime | .NET 11.0.0 |
| Display | Xvfb, 1280×800×24 |
| Window | 1280×800 |
| Result | Passed — 1 test, 0 failed |

Steps exercised and asserted:

1. The shell starts on X11 with the welcome document.
2. Light, dark, and high-contrast appearances render; the three images differ by SHA-256.
3. The ceria template is created, the project is opened in the explorer, and the Overview document is shown.
4. The project saves through the real store (`Saved`).
5. The real in-shell confirmation dialog opens, is captured, and confirms.
6. The project closes and reopens from the same path (`Opened`).
7. Every captured image is non-trivial and larger than 4 KiB.

Screenshots are written to `artifacts/xvfb/` (not committed). The run report records these SHA-256 values:

| Image | Bytes | SHA-256 |
| --- | --- | --- |
| `01-light-welcome.png` | 61832 | `a55dac4cdeea04344e884782a9d7d2ae50c2eec0ffeea57687c69c41521ddd55` |
| `02-dark-welcome.png` | 65939 | `86460d0ea7df8e7581a5b191d2a8e7538d9996e3facb57f7e038edeafc8252f2` |
| `03-high-contrast-welcome.png` | 52981 | `590d58f773c86e22aba65e042bf0fb2b9a7eea81d506ec1adab6efb005343f4b` |
| `04-workspace-ceria.png` | 121530 | `20bf0f734b356fa22751e04b575d0db9d0e76ba84c49a99ce6dd38098e82db91` |
| `05-lifecycle-saved.png` | 121530 | `20bf0f734b356fa22751e04b575d0db9d0e76ba84c49a99ce6dd38098e82db91` |
| `06-dialog-confirm.png` | 120868 | `58e21aca37185e17f0d71e5d00dcee016be2d009db2b3d9b507009cf57843561` |
| `07-lifecycle-reopened.png` | 121530 | `20bf0f734b356fa22751e04b575d0db9d0e76ba84c49a99ce6dd38098e82db91` |

The saved, reopened, and workspace images are byte-identical because the same Overview content is shown; that
is expected. The theme images differ, which shows the appearance switch changes rendering.

## Optional manual spot-check

Not required for Step 23A, and not recorded as executed. It remains the suggested check for later GUI steps on
a real desktop:

1. Start the app; confirm the welcome tab, menu, four regions, and status bar.
2. `Ctrl+N`: create a blank project; confirm the Overview and JSON tabs.
3. Create a ceria project; switch projects and confirm the last document is restored.
4. `Ctrl+S`, `Ctrl+Shift+S`, `F5`, and Close; confirm the lock and last-saved values.
5. Open a second instance of the same project; confirm the read-only banner and disabled Save.
6. Change the file externally; confirm Save refuses and Reload recovers.
7. Open Settings; change theme, density, font scale, high contrast, and reduce motion; confirm the
   change is visible and survives a restart.
8. Confirm a rejected future-major settings file is preserved and reported in the settings banner.
9. Use the command palette (`Ctrl+Shift+P`) and shortcut sheet (`F1`); reach every primary action by keyboard.
10. Open a recent project; confirm missing entries are disabled and removable.
11. Trigger a controlled test exception in a debug build; confirm the crash dialog, log entry, and exit code.

## Recording

A platform run is evidence only when its run ID, `head_sha`, platform, and conclusion are recorded in the
closeout evidence. The Xvfb run is recorded here with its command, environment, date, steps, and image hashes;
the automated headless gates are recorded by CI run ID.
