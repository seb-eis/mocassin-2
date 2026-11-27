# Editor visual-baseline comparison gap — 2026-11-27

**Status:** Implemented with an interim visual reference; Step 24A visual correction deferred until after Step 24B
**Branch:** `fix/editor-visual-baselines`
**Original base:** `895b20b33e8d747c6875bfa603c9b891da1dd4ad`
**Synced main:** `804b26b038713095a686530b21a729a2e42be4dd` (merge `40a9275c17f01bbcfcfe53a59425ffb495b3c860`)
**Design class:** **None** — test-only comparison helper, assertions, deterministic capture support, baselines, and evidence; no production API, format, dependency, or architecture change.
**Complexity class:** **Low** — one test seam, fast comparer unit tests, and a bounded Ubuntu screenshot matrix.

## Owner decision and limits

The human owner decided on 2026-11-27 to:

1. use the current render as the **interim visual reference** and regenerate all 31 baselines;
2. adopt the evidence-based interim threshold of channel delta 8, 1,024 changed pixels per whole image, and 64
   changed pixels per 100×100 region;
3. correct Step 24A visual defects only **after Step 24B is complete**, when a new baseline set will be created.

The interim baselines are **not an approved defect-free target** and must not be cited as visual acceptance for
Step 24. They intentionally preserve the current render, including known Step 24A visual defects, until the
post-24B correction and replacement baseline set.

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
instrumentation was committed.

After the comparer was wired into the editor test, the same blank-baseline probe failed for the intended reason:

```text
particles-light.png: Changed 336529 pixels.
Maximum channel delta: 255.
Test Run Failed. Total tests: 1; failed: 1.
```

The original baseline was again restored and hash-verified. The comparer unit suite retains a zero-budget
single-pixel red proof alongside the approved interim-policy tests.

## Diagnosis: capture conditions versus stale reference

The original screenshot tests from parent commit `895b20b` were restored temporarily, instrumented only to save
frames, run, and restored. Window size, render scale, theme call, pump sequence, and capture timing were the
original ones.

| Case | Baseline vs pre-fix captures | Baseline vs new helper | Pre-fix vs new helper |
| --- | ---: | ---: | ---: |
| `particles-light` | 65,720 / 67,823 / 67,919 | 68,494 | 10,972 |
| shell `dark` | 4,829 / 2,796 / 6,558 | 6,430 | 5,956 |

The pre-fix captures also differed by roughly 0.4–8 %, so the helper was not the cause of the all-baseline
failure:

- `particles-light` differed by 5.70–5.90 % under pre-fix conditions. In the latest run, 56,833 changed pixels
  were outside the project-card, console, and status-bar regions. This is persistent stale-reference or
  renderer/UI drift.
- shell `dark` differed by 0.40–0.94 % under pre-fix conditions, entirely in the console/status bands
  `y=552..692`; there were zero changed pixels elsewhere. This was localized volatile text/font rasterization,
  not a large layout region hidden by 5 %.
- The helper-only delta was also confined to volatile regions: 10,960 of 10,972 editor pixels and all 5,956 shell
  pixels. The helper changed motion, fixed volatile text/paths, and added one pump; it did not change window size
  or render scale.

## Measured threshold basis

Two independent normalized runs of all 31 captures produced **0 changed pixels**, with maximum channel delta 0.
Before regeneration, the stored references differed from the current capture by 3,493–92,166 pixels, and their
worst 100×100 regions differed by 795–6,329 pixels. The owner-approved interim policy therefore rejects all 31
old references while allowing small localized raster variance:

```text
maximum per-channel delta:             8
maximum changed pixels per image:      1,024
maximum changed pixels per 100×100 region: 64
```

A 16×16 displaced block is 256 pixels and fails the region budget. A 100 px one-pixel horizontal shift affects
about 200 edge pixels and fails. A 100×100 layout defect is 10,000 pixels and fails both budgets. The retired 5 %
shell allowance was 35,000 pixels and would also hide that 100×100 defect, which is only 1.43 % of a 1000×700
shell image.

Absolute zero remains a useful same-condition determinism assertion, but it is not the acceptance gate because
the stored reference and renderer drift made it unusable before regeneration.

## Implementation

- `Visual/VisualComparer.cs` compares decoded BGRA pixels after PNG-normalizing the captured frame. It enforces
  the whole-image and fixed-region budgets, reports both maxima, fails size mismatches, and writes actual/diff
  artifacts on failure.
- Both screenshot test classes use `VisualComparer.DefaultTolerance`.
- Eight PR-fast `VisualComparerTests` cover identical images, below-threshold differences, the approved small
  difference allowance, whole-image budget failure, region budget failure, size mismatch, absolute-budget
  semantics, and the retained zero-budget red proof. They need no headless session.
- `VisualCaptureSupport` fixes the console timestamp, status-bar version, settings/data paths, and project path;
  screenshot settings disable motion.
- All 31 baselines were regenerated from the current render with the owner-approved command:

  ```sh
  MOCASSIN_REGENERATE_VISUAL_BASELINES=1 dotnet test \
    src/dotnet/Mocassin.App.Tests/Mocassin.App.Tests.csproj -c Release \
    --filter "FullyQualifiedName~ScreenshotTests"
  ```

- The same visual suite then passed without regeneration: **5/5 facts**, covering all 31 baselines.

## Historical pre-regeneration mismatch

These counts explain why the old references were replaced. They are historical measurements, not the interim
acceptance result.

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

The actual/diff evidence remains outside the repository at
`/home/node/tmp/visual-baseline-review/actual-diff/`; the full mismatch table and isolation measurements are under
`/home/node/tmp/visual-baseline-review/measurements/`.

## Commands and results

| Command | Result |
| --- | --- |
| `dotnet test src/dotnet/Mocassin.App.Tests/Mocassin.App.Tests.csproj -c Release --filter "FullyQualifiedName~VisualComparerTests" --logger "console;verbosity=normal"` | **Pass:** 8/8 |
| `MOCASSIN_REGENERATE_VISUAL_BASELINES=1 dotnet test ... --filter "FullyQualifiedName~ScreenshotTests"` | **Pass:** 5/5; regenerated 31 baselines |
| `dotnet test src/dotnet/Mocassin.App.Tests/Mocassin.App.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~ScreenshotTests" --logger "console;verbosity=normal"` | **Pass:** 5/5 without regeneration |
| `dotnet test src/dotnet/Mocassin.App.Tests/Mocassin.App.Tests.csproj -c Release --logger "console;verbosity=minimal"` | **Pass:** 475/475 |
| `dotnet build Mocassin.slnx -c Release` | **Pass:** 0 warnings, 0 errors |
| `dotnet format Mocassin.slnx --verify-no-changes` | **Pass:** no formatting changes; workspace-load warning only |
| `./tools/verify.sh structure` | **Pass:** 9/9 architecture/structure tests; `git diff --check` clean |
| `./tools/verify.sh docs --require` | **Pass:** Release build, math/link checks, strict Zensical, DocFX, and rustdoc; required checks enabled |
| `./tools/verify.sh policy --require` | **Pass:** API compatibility, contract artifacts, projection checks, catalog provenance, process consistency, link checks, 94 tool tests, SPDX, secrets, and advisories; required checks enabled |

All gates above were re-run after merging `origin/main` at `804b26b`. The visual suite also passed a second
run without regeneration before that merge, with no capture exceeding the threshold.

No CI run was dispatched. This is a maintenance branch, not a step closeout.

## Dev-binding fallback

The `dev-free` binding regenerated the baselines and updated the documentation but was rate-limited after
merging `main`, before committing. The assignment continued on the paid `dev` fallback in the same worktree and
branch; no regenerated baseline or documentation edit was discarded. The fallback recorded its own build, test,
format, structure, docs, and policy results in the table above.

## Residual platform limitation and risk

The repository's UI evidence policy pixel-compares screenshots on Ubuntu only. Windows and macOS remain manual
platform-gate evidence; text rendering is not compared across operating systems. The fixed `/tmp` capture paths
are used only after the Linux guard returns, so they do not affect Windows/macOS test execution.

The interim baselines include known Step 24A visual defects by owner decision. They prove deterministic capture
and comparison behavior only; they do not prove Step 24 visual correctness. Step 24A visual corrections and a new
baseline set remain mandatory after Step 24B.

No merge, tag, or `main` push is requested by this record.
