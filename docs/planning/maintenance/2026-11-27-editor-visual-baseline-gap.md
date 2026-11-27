# Editor visual-baseline comparison gap — 2026-11-27

**Status:** Implemented; existing baseline corrections await owner review
**Branch:** `fix/editor-visual-baselines`
**Base:** `origin/main` at `895b20b33e8d747c6875bfa603c9b891da1dd4ad`
**Design class:** **None** — test-only comparison helper, assertions, deterministic capture support, and evidence; no production API, format, dependency, or architecture change.
**Complexity class:** **Low** — one test seam, fast comparer unit tests, and a bounded Ubuntu screenshot matrix.

## Defect

`ModelEditorScreenshotTests` loaded each checked-in baseline and asserted only equal `PixelSize`. A same-size
blank or unrelated image therefore passed. `ScreenshotTests` compared pixels, but allowed changes in 5 % of the
whole image, which can hide a concentrated layout defect. Step 24A audit F-5 accepted the presence of screenshot
baselines without proving that the editor assertion compared their contents.

The captures also contained volatile text: the console startup timestamp, temporary settings/data paths, the
project path, and the assembly informational version (including the commit). Those values made an uncalibrated
pixel threshold nondeterministic even before baseline drift was considered.

## Red-capable proof

Before changing code, the focused editor test passed on the untouched tree:

```text
dotnet test src/dotnet/Mocassin.App.Tests/Mocassin.App.Tests.csproj -c Release \
  --filter "FullyQualifiedName~ModelEditorScreenshotTests" --logger "console;verbosity=normal"
Passed: 1, failed: 0 (about 7 s)
```

A scratch standard-library PNG script then replaced only
`src/dotnet/Mocassin.App.Tests/Visual/Baselines/particles-light.png` with an opaque blank image preserving its
1280 × 900 IHDR. The same test, rebuilt unchanged, still passed:

```text
original SHA-256: 592add41ad8bd1bafb09db0fbaafaa4b9c3e2b9707e890b39951044c187ed3da
blank SHA-256:    9f7b09544bd6acb5a25424459576d6a7f37ef314bca601e1c60bcd2fdadf2da6
dotnet test ... --no-build --filter "FullyQualifiedName~ModelEditorScreenshotTests"
Passed: 1, failed: 0 (about 7 s)
```

The original baseline was restored immediately and its SHA-256 matched the pre-probe value. No blank image or
instrumentation is committed. Scratch scripts and logs are under `.tmp/editor-visual-baseline-gap/`.

After the comparer was wired into the editor test, the same blank-baseline probe failed for the intended reason:

```text
particles-light.png: Changed 336529 pixels; the absolute budget is 0.
Maximum channel delta: 255.
Test Run Failed. Total tests: 1; failed: 1.
```

The original baseline was again restored and hash-verified. This is the minimized red/green regression proof.

## Ranked falsifiable hypotheses and outcomes

1. **Missing editor pixel assertion — confirmed.** The size-only assertion accepted the blank image; the shared
   pixel comparer rejects it with 336,529 changed pixels.
2. **Channel-order mismatch — controlled.** The actual `WriteableBitmap` is saved and decoded through PNG before
   comparison, so baseline and actual use the same decoded BGRA representation. The measured comparer counts
   match the independent PNG measurement script.
3. **Animation timing — partly confirmed.** Initial repeat captures differed in opacity-animated text. Screenshot
   settings now use `ReduceMotion = true`; the remaining repeat differences were volatile text/paths, not layout.
4. **Localized baseline drift — confirmed.** The old 5 % shell allowance accepted concentrated changes. The new
   absolute budget rejects every changed pixel above the channel threshold, regardless of image size or region.
5. **Benign raster noise — not observed after normalization.** Two independent normalized runs of all 31 captures
   produced zero changed pixels. No tolerance headroom was added beyond that evidence.

## Measured tolerance basis

Calibration ran on the Linux worktree runner with .NET 11.0.0, Avalonia 12.1.6, and the resolved SkiaSharp
3.119.4 stack. Two independent `dotnet test` processes captured all 16 editor and 15 shell images. An independent
standard-library PNG decoder measured decoded RGB pixels; a pixel counted as changed when any channel delta was
greater than 8.

Before normalization, repeat-run differences were localized to the console timestamp, settings paths, project
path, and animated/volatile text. The final captures stabilize those inputs:

- console: one `Information` entry, `MOCASSIN shell started.`, at fixed `2026-01-01T00:00:00Z`;
- status bar version: fixed `visual-test`;
- app settings/data workspace: fixed `/tmp/mocassin-visual-baselines/visual-baselines`;
- project: real ceria fixture copied to and loaded from
  `/tmp/mocassin-visual-baselines/project/ceria.mocprj`;
- motion: `ReduceMotion = true` for every light/dark/high-contrast capture.

Final repeat-run result: **0 changed pixels in all 31 comparisons**, with maximum channel delta 0. The policy is
therefore:

```text
maximum per-channel delta: 8
maximum changed pixels:    0 (absolute, not a percentage)
```

A difference of 4 in one channel passes; a difference of 9 fails. A size mismatch fails separately. The 5 %
whole-image shell allowance is removed. No baseline was regenerated to obtain these measurements.

## Fix

- Added `Visual/VisualComparer.cs`: one shared BGRA comparison core, PNG-normalized baseline wrapper, size-mismatch
  result, absolute changed-pixel budget, and failure-artifact writer.
- Both `ScreenshotTests` and `ModelEditorScreenshotTests` now call
  `VisualComparer.AssertBaselineMatches(..., VisualComparer.DefaultTolerance)`.
- Added `Visual/VisualCaptureSupport.cs` and deterministic `TempWorkspace`/project-fixture support so volatile
  text and paths are stable across runs and checkout locations.
- Added five PR-fast `VisualComparerTests`: identical images, below-threshold difference, above-threshold
  difference, size mismatch, and an absolute budget that does not scale with image size. They use in-memory decoded
  images and need no headless session.
- On failure, the comparer saves `<baseline>.actual.png` and a magenta-highlighted `<baseline>.diff.png` under
  `.tmp/visual-baseline-failures/` and includes both paths in the assertion message.
- Updated `docs/development/testing-and-parity.md` and `docs/development/platform-checklist.md` to describe the
  shared strict comparison and owner-approved regeneration requirement.

## Affected tests and baselines

The strict visual filter runs five facts. Each fact fails fast on its first mismatched baseline:

| Test | First rejected baseline | Changed pixels |
| --- | --- | ---: |
| `ModelEditorScreenshotTests.TheModelEditorsHaveVisualBaselines` | `particles-light.png` | 68,494 |
| `ScreenshotTests.TheShellRendersLightDarkAndHighContrastBaselines` | `light.png` | 6,301 |
| `ScreenshotTests.TheValidationAndSettingsOverlaysHaveLightAndDarkBaselines` | `light-validation.png` | 3,584 |
| `ScreenshotTests.TheOpenProjectIconSurfacesHaveLightDarkAndHighContrastBaselines` | `light-project.png` | 11,234 |
| `ScreenshotTests.ThePaletteCheatSheetAndTabHoverHaveBaselines` | `light-palette.png` | 23,342 |

The offline normalized-capture comparison checked every baseline, not only the fail-fast examples. All 31
existing baselines differ from the stabilized current capture. No baseline file is modified by this branch.

| Baseline | Group | Changed pixels | Total pixels | Maximum channel delta | Repeat-run changed pixels |
| --- | --- | ---: | ---: | ---: | ---: |
| `dummies-dark.png` | editor | 11,354 | 1,152,000 | 255 | 0 |
| `dummies-highcontrast.png` | editor | 11,684 | 1,152,000 | 255 | 0 |
| `dummies-light.png` | editor | 58,127 | 1,152,000 | 255 | 0 |
| `particles-dark.png` | editor | 80,163 | 1,152,000 | 255 | 0 |
| `particles-highcontrast.png` | editor | 52,508 | 1,152,000 | 255 | 0 |
| `particles-light.png` | editor | 68,494 | 1,152,000 | 255 | 0 |
| `sets-dark.png` | editor | 11,546 | 1,152,000 | 255 | 0 |
| `sets-highcontrast.png` | editor | 11,879 | 1,152,000 | 255 | 0 |
| `sets-light.png` | editor | 82,583 | 1,152,000 | 255 | 0 |
| `sites-dark.png` | editor | 92,166 | 1,152,000 | 255 | 0 |
| `sites-highcontrast.png` | editor | 65,078 | 1,152,000 | 255 | 0 |
| `sites-light.png` | editor | 78,900 | 1,152,000 | 255 | 0 |
| `sites-narrow-light.png` | editor | 56,614 | 810,000 | 255 | 0 |
| `structure-dark.png` | editor | 11,652 | 1,152,000 | 255 | 0 |
| `structure-highcontrast.png` | editor | 11,985 | 1,152,000 | 255 | 0 |
| `structure-light.png` | editor | 13,443 | 1,152,000 | 255 | 0 |
| `dark-cheatsheet.png` | shell | 3,493 | 700,000 | 153 | 0 |
| `dark-palette.png` | shell | 24,271 | 700,000 | 167 | 0 |
| `dark-project.png` | shell | 11,324 | 700,000 | 255 | 0 |
| `dark-settings.png` | shell | 52,792 | 700,000 | 255 | 0 |
| `dark-tab-hover.png` | shell | 6,326 | 700,000 | 255 | 0 |
| `dark-validation.png` | shell | 3,584 | 700,000 | 255 | 0 |
| `dark.png` | shell | 6,430 | 700,000 | 255 | 0 |
| `highcontrast-project.png` | shell | 11,738 | 700,000 | 255 | 0 |
| `highcontrast.png` | shell | 6,556 | 700,000 | 255 | 0 |
| `light-cheatsheet.png` | shell | 3,493 | 700,000 | 153 | 0 |
| `light-palette.png` | shell | 23,342 | 700,000 | 192 | 0 |
| `light-project.png` | shell | 11,234 | 700,000 | 255 | 0 |
| `light-settings.png` | shell | 51,330 | 700,000 | 255 | 0 |
| `light-validation.png` | shell | 3,584 | 700,000 | 255 | 0 |
| `light.png` | shell | 6,301 | 700,000 | 255 | 0 |

Actual and diff images for all 31 are saved under `.tmp/visual-baseline-failures/`; `summary.json` there records
the counts and paths. The full two-run measurements are under
`.tmp/editor-visual-baseline-gap/calibration-normalized/measurements.json`. These directories are gitignored.

The comparison proves that the baselines are stale or environment-dependent; it does not by itself decide
whether each pixel difference is a product layout defect or reference-renderer drift. The owner must review the
actual/diff pairs before approving any regeneration.

## Commands and results

| Command | Result |
| --- | --- |
| `dotnet test src/dotnet/Mocassin.App.Tests/Mocassin.App.Tests.csproj -c Release --filter "FullyQualifiedName~VisualComparerTests" --logger "console;verbosity=normal"` | **Pass:** 5/5 |
| `dotnet test src/dotnet/Mocassin.App.Tests/Mocassin.App.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~ScreenshotTests" --logger "console;verbosity=normal"` | **Fail as expected pending baseline approval:** 0 passed, 5 failed |
| `dotnet test src/dotnet/Mocassin.App.Tests/Mocassin.App.Tests.csproj -c Release --no-build --filter "FullyQualifiedName!~ScreenshotTests" --logger "console;verbosity=minimal"` | **Pass:** 467/467 |
| `dotnet test src/dotnet/Mocassin.App.Tests/Mocassin.App.Tests.csproj -c Release --no-build --logger "console;verbosity=minimal"` | **Fail as expected pending baseline approval:** 467 passed, 5 failed, 472 total |
| `dotnet build Mocassin.slnx -c Release` | **Pass:** 0 warnings, 0 errors |
| `dotnet format Mocassin.slnx --verify-no-changes` | **Pass:** no formatting changes; workspace-load warning only |
| `./tools/verify.sh structure` | **Pass:** 9/9 architecture/structure tests; `git diff --check` clean |
| `./tools/verify.sh docs --require` | **Pass:** Release build, math/link checks, strict Zensical, DocFX, and rustdoc; required checks enabled |

No CI run was dispatched. This is a maintenance branch, not a step closeout.

## Residual platform limitation

The repository's UI evidence policy pixel-compares screenshots on Ubuntu only. Windows and macOS remain manual
platform-gate evidence; text rendering is not compared across operating systems. The fixed `/tmp` capture paths
are used only after the Linux guard returns, so they do not affect Windows/macOS test execution.

## Owner decision on baseline changes

**No baseline has been regenerated or changed.** All 31 baselines listed above need owner review. Regeneration
requires an explicit owner decision recorded in this maintenance record; until then the strict visual tests stay
red and the actual/diff evidence stays available under `.tmp/`.

No merge, tag, or `main` push is requested by this record.
