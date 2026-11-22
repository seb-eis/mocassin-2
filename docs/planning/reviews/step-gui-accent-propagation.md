# GUI Accent Propagation Design Review

> Reviewer: write the evidence, findings, and verdict directly into this record. You may edit only your own
> metadata (`Reviewer`, `Review date`, `Record state`), the checklist completion marks and inline verification
> notes (not item text), the `Evidence`, `Findings`, and `Verdict` sections, and commit only this file with a
> documentation-only message and `[skip ci]`. Do not edit the request scope, checklist item text, resolutions,
> code, tests, configuration, the maintenance record, or any other file.

## Request

| Field | Value |
| --- | --- |
| Review ID | `MAINT-2026-11-22-GUI-ACCENT` |
| Type | Out-of-step maintenance design review (not a roadmap step) |
| Target commit | `e7a62275c372d5f7485b6310646ea66b8fed2c78` |
| Docs branch | `fix/gui-accent-propagation` |
| Request owner | Planner |
| Orchestrator | — |
| Step agent (implementor) | — (no step; the fix is a bounded maintenance branch) |
| Review initiated by | Orchestrator, on request of the planner |
| Review date | 2026-11-22 |
| Reviewer | Round 1: DeepSeek V4.1 Flash 1017 (opencode-go), fresh context. Round 2: DeepSeek V4.1 Flash 1017 (opencode-go), a separate fresh context with no shared conversation with round 1, the planner, implementor, or orchestrator |

Maintenance record:
[`2026-11-22-gui-accent-propagation.md`](../maintenance/2026-11-22-gui-accent-propagation.md)

Normative contract:
[`docs/development/ui-visual-contract.md`](../../development/ui-visual-contract.md) §3.6

> This is an out-of-step maintenance design review under `AGENTS.md` §3.5, not a roadmap-step design review.
> The file uses the `step-*` prefix so the reviewer harness can edit it. The reviewed design is the visual
> contract and the maintenance record at the target commit.

## Scope

The review covers the design delta only:

- the accent propagation model in visual contract §2 and §3.2–§3.6;
- the status bar, focus, selected-control, tab-pipe, and contrast rules in §6, §7.1, §7.3, §7.4, §11.1, and
  §13;
- the maintenance record's owner report, verified fault line, full accent-resource inventory, fix shape,
  expected overlay set, test plan, documentation plan, acceptance criteria, risks, and in-scope/deferred list;
- the owner decisions A1–A3 recorded in the maintenance record.

Non-goals of the design review:

- implementation code, tests, configuration, or `contracts/manifest.json`;
- the UI interaction contract, control templates, a theme redesign, or new accent keys;
- the parked `fix/gui-palette-freeze` branch or its record;
- step status, parity, audit, or tag changes.

## Design decisions reviewed

- [x] Accent overlay completeness and precedence (default/`blue`, optional accents, high contrast) — Reviewed; the precedence model is sound, but the overlay set is incomplete for Fluent-derived states (M-3) and the shared selected-text key (M-4); the inventory is inaccurate (m-2). Round 2: the two-group model is present, but §2 still says the overlay is optional-accent-only (M-7) and Fluent alias capture defeats the base-key overlays (M-6).
- [x] `OnAccent`, `AccentText`, `FocusAccent`, `SelectedFill`, and `SelectedText` derivation — Reviewed and recomputed. `OnAccent`, `AccentText`, `FocusAccent`, and `SelectedText` match the table and the stated floors; `SelectedFill` does not match its own formula (M-2). Focus floors are based on the wrong worst surface (M-5). Round 2: `FocusAccent`, `AccentText`, and all 24 `FluentState` values reproduce exactly; the light `green` `SelectedFill` still does not (m-3).
- [x] `Mocassin.FocusBrush` as the contrast-safe focus/indicator role — Reviewed; the role is well motivated, but the dark input/hover surfaces are not in the contrast oracle (M-5). Round 2: every `FocusAccent` and `#248DD3` passes 3:1 on `#3C3C3C`, `#37373D`, `#2D2D30`, `#252526`, `#1E1E1E`, and the light surfaces; the Fluent slider thumb does not receive `FocusAccent` (M-6).
- [x] Status-bar background/foreground and progress contrast — Reviewed; all six accents pass 4.5:1 (`blue` 4.511 white, `teal`/`amber` black, `green`/`violet`/`rose` white) and the progress bar uses `StatusBarForeground` on a transparent track.
- [x] Selected list/tree/combo and text-selection treatment — Reviewed; combo and TextBox pairs are coherent, but list/tree selected and hover text share `SystemControlHighlightAltBaseHighBrush` with Fluent accent-control glyphs (M-4), and tree selected fill uses the Fluent Reveal keys rather than the documented list keys (m-2). Round 2: `AltBaseHigh` is removed from the overlays and the list/combo text passes; Fluent captures the tree/reveal aliases at load, so the reveal overlay does not set the tree fill and the HC tree text/glyph values do not reach the controls (M-6).
- [x] Default restoration and live reapply — Reviewed; the remove-then-publish logic and `StartupCoordinator.Changed` reapply are sound for explicit `Light`/`Dark`. `ThemeSelection.System` is not a fixed light fallback (M-1). Round 2: the initial System resolution now uses `Application.ActualThemeVariant`; a live OS Light/Dark switch still has no reapply trigger (M-8).
- [x] Test plan, contrast matrix, and screenshot scope — Reviewed; the matrix and screenshots are a good start, but the omissions would let the M-3/M-4/M-5 surfaces ship (m-1). Round 2: the matrix and rendered cases were expanded, but there is no rendered high-contrast Fluent-control check and no realized-template-part assertion, and the System seam can bypass the production read (m-5); `SystemControlHighlightChromeWhiteBrush` is still missing (m-6).
- [x] Documentation plan and acceptance criteria — Reviewed; the documentation touches the right sections, but acceptance criteria 1 and 4 are not met by the design as written (M-4, M-5). Round 2: acceptance criteria 1/3/4 are still not established for the aliased Fluent controls (M-6), and §2 was not updated to match the two-group model (M-7).
- [x] Owner decisions A1–A3 and the superseded refresh wording — Reviewed; A1–A3 are recorded, internally consistent, and the supersession of the fixed-white status-bar and maximum-contrast `blue` wording is explicit.
- [x] No ADR, interaction-contract, control-template, dependency, or format change — Reviewed; no ADR, interaction-contract, control-template, dependency, `contracts/manifest.json`, or format change is proposed. The `TextBox` selection pair is a property setter, not a template.

## Dependency and interface checks

- [x] The base is current `main` at `19cb6ffe190e02449ce8b827706f365f2ea7afe2` — Verified: `git rev-parse main` is `19cb6ffe...`; the branch is based on it and `main` has not moved.
- [x] The design uses only existing accent keys and resources, plus the new derived `Mocassin.FocusBrush` — Verified: the six accent keys and five optional hex values are unchanged, and `FocusBrush` is a derived role in `Palette.axaml`/`HighContrast.axaml`, not a new selectable accent.
- [x] The overlay/removal interface with `AvaloniaThemeApplier` is defined — Reviewed: `RemoveAccentOverlay` and `PublishAccentOverlay` are specified, and the current `ApplyAccent` failure mode (`AvaloniaThemeApplier.cs:104-127`) matches the record. The key list is incomplete (M-3, m-2).
- [x] High-contrast and density precedence remain authoritative — Reviewed: the applier removes the overlay under high contrast, high contrast and density merged dictionaries keep their precedence, and density owns only spacing/size tokens. `ThemeSelection.System` remains a separate precedence defect (M-1).
- [x] No unresolved cross-language, format, or public-API interface question remains — Verified: no `contracts/manifest.json`, project/package/results/state format, cross-language fixture, or public API change is proposed. The remaining issues are visual-contract and Fluent-resource issues, not format/API ones.

## Standard design-review checklist

- [x] Scope, non-goals, and acceptance criteria are explicit and testable — Reviewed; scope and non-goals are explicit, but acceptance criteria 1 (all accent surfaces) and 4 (3:1 focus floor) are not satisfied by the design as written (M-4, M-5).
- [x] The change is bounded to one maintenance branch and one reviewable diff; no step sizing applies — Verified: one `fix/gui-accent-propagation` maintenance branch, docs only at this freeze, no code/tests yet, no step sizing.
- [x] Design decisions are complete and internally consistent — Reviewed; M-1, M-2, and M-4 record contradictions between the stated rules, the table, and the actual Fluent keys.
- [x] Required tests, contrast checks, and rendered-surface checks are defined — Reviewed; the plan is defined but incomplete for the Fluent-derived states, the tree row, and the actual `System` variant (m-1).
- [x] Documentation, evidence, and owner-approval updates are planned — Reviewed; §7 of the record names the right contract sections and the branch/merge evidence path.
- [x] Licensing, provenance, and dependency checks are resolved (no new dependency) — Verified: no new dependency, font, icon package, or third-party theme; the work uses Avalonia Fluent 12.1.6 and first-party tokens.
- [x] Risks and mitigations are realistic — Reviewed; the risk table correctly names the shared-key conflict and the System assumption, but its mitigations are insufficient (M-1, M-4).
- [x] Code layout and namespace plan follow the structure policy (no new source area) — Verified: no new source area; the `TextBox` changes belong in `ShellStyles.axaml`, the splitter changes in `ShellView.axaml`, and the applier changes in `Theming/AvaloniaThemeApplier.cs`.
- [x] No hidden scope, stale placeholders, or unowned decisions — Reviewed; no hidden scope, no stale placeholder text, and A1–A3 are owned. OS-dark support is explicitly deferred but the deferral assumes a light `System` variant that the app does not implement (M-1).

## Evidence

Files reviewed:

- `docs/development/ui-visual-contract.md`
- `docs/planning/maintenance/2026-11-22-gui-accent-propagation.md`
- `src/dotnet/Mocassin.App.Views/Theming/AvaloniaThemeApplier.cs`
- `src/dotnet/Mocassin.App.Views/Themes/Palette.axaml`
- `src/dotnet/Mocassin.App.Views/Themes/HighContrast.axaml`
- `src/dotnet/Mocassin.App.Views/Styles/ShellStyles.axaml`
- `src/dotnet/Mocassin.App.Views/Shell/ShellView.axaml`
- `src/dotnet/Mocassin.App.Views/Shell/StatusBarView.axaml`
- `src/dotnet/Mocassin.App.ViewModels/Settings/AccentPalette.cs`
- `src/dotnet/Mocassin.App.Tests/Settings/ThemeApplierTests.cs`
- `src/dotnet/Mocassin.App.Tests/Visual/ThemeContrastTests.cs`

Commands run by the request owner:

- `git rev-parse main` — `19cb6ffe190e02449ce8b827706f365f2ea7afe2`
- `./tools/verify.sh docs` — exit 0. Strict Zensical build passed; DocFX built 465 models with 0 warnings and
  0 errors; rustdoc built the workspace.
- `./tools/verify.sh policy` — exit 0. ApiCompat found no breaking changes; contract artifacts current;
  space-group catalog current; process consistency OK; 60 policy tests passed; SPDX, secret scan, and
  `cargo-deny` clean.
- `python tools/check_docs_links.py` — `Documentation links OK (204 files scanned)`.
- `python tools/check_doc_math.py` — `Documentation math conventions OK (201 files scanned)`.

Reviewer evidence (round 1), independent of the request-owner evidence above:

- Frozen state. `git rev-parse HEAD` was `5fb01f8ee4aecd6bd20333ccc25995fb9f5cfbcd`; `git rev-parse
  6ed4858f562ec9f29e1042b6ae4294bd3cac6fef` was the frozen design commit. `git diff --name-status
  6ed4858..5fb01f8` reported only `M docs/planning/reviews/step-gui-accent-propagation.md`, and `git diff
  6ed4858..5fb01f8` showed exactly one changed line: the review `Target commit` metadata from `c0628c2` to
  `6ed4858`. `git rev-parse main` was `19cb6ffe190e02449ce8b827706f365f2ea7afe2`, matching the stated base.
- Additional files read by the reviewer: `src/dotnet/Mocassin.App/App.axaml`,
  `src/dotnet/Mocassin.App/Startup/StartupCoordinator.cs`,
  `src/dotnet/Mocassin.App.ViewModels/Settings/AppSettings.cs`,
  `src/dotnet/Mocassin.App.ViewModels/Settings/Dialog/SettingsViewModel.cs`,
  `src/dotnet/Mocassin.App.Views/Themes/Density.axaml`,
  `src/dotnet/Mocassin.App.Views/Settings/SettingsDialogView.axaml`,
  `src/dotnet/Mocassin.App.Views/Consoles/ProblemsView.axaml`,
  `src/dotnet/Mocassin.App.Views/Shell/ProjectsRegionView.axaml`,
  `src/dotnet/Mocassin.App.Views/Dialogs/ProgressDialogView.axaml`,
  `docs/development/ui-interaction-contract.md` §3–§6, and the exact Avalonia 12.1.6 Fluent theme sources
  listed below.
- Fluent source check. The app targets Avalonia.Fluent 12.1.6
  (`src/dotnet/Directory.Packages.props:12`). Against the 12.1.6 Fluent theme source/NuGet package:
  `Accents/SystemAccentColors.cs` computes `SystemAccentColorLight1`, `Dark1`, `Light2`, `Dark2`,
  `Light3`, and `Dark3` from `platformSettings.GetColorValues().AccentColor1`, not from an application
  `SystemAccentColor` override. `Accents/FluentControlResources.xaml` uses `SystemAccentColorLight1`/
  `Dark1` for `CheckBoxCheckBackgroundFillCheckedPointerOver`/`Pressed` (check boxes), the
  indeterminate equivalents, `RadioButtonOuterEllipseCheckedFillPointerOver`/`Pressed`,
  `SliderThumbBackgroundPointerOver`/`Pressed`, and `ToggleSwitchFillOnPointerOver`/`Pressed`.
  `Controls/ListBoxItem.xaml` uses `SystemControlHighlightAltBaseHighBrush` for pointer-over, pressed, and
  selected foreground. `Controls/TreeViewItem.xaml` and `Accents/FluentControlResources.xaml:711-728` map
  `TreeViewItemBackgroundSelected` to `SystemControlHighlightAccent3RevealBackgroundBrush` (the
  `SystemAccentColor` colour at 0.6 opacity) and the selected foreground to
  `SystemControlHighlightAltBaseHighBrush`. `Controls/CheckBox.xaml` uses
  `SystemControlHighlightAltChromeWhiteBrush` for the check glyph. `Styling/ThemeVariant.cs`
  `UpdateActualThemeVariant` resolves `ThemeVariant.Default` on an application root through
  `GetPlatformThemeVariant()`, so `ThemeSelection.System` follows the OS Light/Dark variant rather than a
  fixed `Default`/Light dictionary.
- Contrast recomputation. WCAG 2.x relative-luminance and contrast-ratio calculations were re-run for
  every table row and for the extra surfaces. The `OnAccent(raw)` values and status-bar pairs pass
  4.5:1: `blue` white 4.511, `teal` black 5.066, `green` white 5.127, `amber` black 4.955, `violet`
  white 6.845, `rose` white 7.097. The `AccentText` table rows pass 4.5:1 (lowest: dark `green` 4.518,
  dark `teal` 4.527, dark `amber` 4.528; light `amber` 4.581, light `teal` 4.534, light `green` 4.621).
  The `FocusAccent` table rows pass 3.0:1 against `#2D2D30` (lowest: dark `green` 3.014, dark `rose`
  3.017, dark `violet` 3.068), but fail against the dark input background `#3C3C3C` (green 2.42, violet
  2.47, rose 2.42, blue 2.45) and the dark hover background `#37373D` (2.60–2.85). The
  `SelectedText` table rows pass 4.5:1 against the table's `SelectedFill` values (lowest: light `violet`
  4.593, light `rose` 4.553; dark `teal` 6.400). The `SelectedFill` table, however, equals
  `round(0.70 * a + 0.30 * chrome)`, not the stated `round(0.30 * a + 0.70 * chrome)`: for dark `teal`
  the formula gives `#153F3F` and the table gives `#096A6A`; for light `violet` the formula gives
  `#CAC1D6` and the table gives `#937EB0`. The `SelectedText` rule then depends on which fill is chosen
  (for light `rose`, the formula fill `#DFAAC2` passes with `#1F1F1F`, while the table fill `#C44980`
  requires white).
- System-variant check. `AvaloniaThemeApplier.Apply` maps `ThemeSelection.System` to
  `ThemeVariant.Default` (`AvaloniaThemeApplier.cs:38-43`), while `App.axaml:4` also starts with
  `RequestedThemeVariant="Default"`. In Avalonia 12.1.6 that means the application root takes the
  platform variant (`ActualThemeVariant` is `Dark` on a dark OS). The existing `System` test
  (`ThemeContrastTests.cs:203`, `:228`, `:296`) passes `ThemeVariant.Default` explicitly to
  `TryGetResource`, which reads the `Default` fallback dictionary and never exercises
  `Application.ActualThemeVariant`.
- Re-run commands. `git diff --check` — clean; `python tools/check_docs_links.py` — `Documentation links
  OK (204 files scanned)`; `python tools/check_doc_math.py` — `Documentation math conventions OK (201
  files scanned)`; `python tools/check_process_consistency.py` — `Process consistency: OK`; `python -m
  unittest discover -s tools/tests -t .` — 60 tests OK; `./tools/verify.sh docs` — exit 0, strict site
  build, DocFX 465 models with 0 warnings/0 errors, rustdoc workspace built. `git status --short` was
  clean before the record edit.

## Findings

| Severity | Finding | Evidence |
| --- | --- | --- |
| Major | **M-1 — `ThemeSelection.System` is not a fixed Light fallback.** The design says `System` uses the `Default`/Light dictionary and that OS-dark detection is deferred, but the app maps `System` to `ThemeVariant.Default`, which resolves to the platform variant. On a dark OS the overlay would publish the light `AccentText`/`SelectedFill`/`FocusAccent` values while the palette serves Dark. | `AvaloniaThemeApplier.cs:38-43`; `App.axaml:4`; Avalonia 12.1.6 `Styling/ThemeVariant.cs` `UpdateActualThemeVariant`; contract `:288-289`; record `:185-186`, `:328`; tests `ThemeContrastTests.cs:203,228,296` pass `ThemeVariant.Default` explicitly. Example: light `AccentText` `#005A9E` on dark card `#2D2D30` is 1.93:1. |
| Major | **M-2 — `SelectedFill` derivation and the test-oracle table encode different blends.** The rule says 30% accent + 70% chrome; every optional-accent `SelectedFill` row equals 70% accent + 30% chrome. The difference is visible and changes `SelectedText` (for example light `rose`). | Contract `:216-218` (`round(0.30 * a + 0.70 * chrome)`); record `§4.1` (`:164-168`); contract table `:275-282`. Dark `teal`: formula `#153F3F`, table `#096A6A`; light `violet`: formula `#CAC1D6`, table `#937EB0`; light `rose`: formula fill `#DFAAC2` passes with `#1F1F1F`, table fill `#C44980` needs white. |
| Major | **M-3 — Fluent accent-derived hover/pressed states are omitted and revert to the platform/default blue.** The overlay includes `SystemAccentColor` but not the derived `SystemAccentColorLight1`/`Dark1` that Fluent 12.1.6 uses for checked check-box hover/press, slider thumb hover/press, radio buttons, and toggles. `SystemAccentColors` derives those shades from the platform accent, not from the app's `SystemAccentColor` override. | Avalonia 12.1.6 `Accents/SystemAccentColors.cs`; `Accents/FluentControlResources.xaml` `CheckBoxCheckBackgroundFillCheckedPointerOver`/`Pressed`, `SliderThumbBackgroundPointerOver`/`Pressed`, `ToggleSwitchFillOnPointerOver`/`Pressed`, radio-button equivalents; the app has `CheckBox` at `SettingsDialogView.axaml:49-53` and `ProblemsView.axaml:51` and a `Slider` at `SettingsDialogView.axaml:33`; test plan `§6.1`/`§6.3` does not render those states. |
| Major | **M-4 — `SystemControlHighlightAltBaseHighBrush` is overloaded; overlaying it to `OnAccent(FocusAccent)` breaks list/tree selected and hover text.** Fluent 12.1.6 uses the same key for `ListBoxItem` pointer-over, pressed, and selected foreground and for `TreeViewItemForegroundSelected*`; the overlay sets it to `OnAccent(FocusAccent)` while selected fills use `SelectedFill`. For light `green`/`violet` this is white on `#69A06C`/`#937EB0` (3.07:1 and 3.59:1, below 4.5); for dark `teal`/`amber` it is black on `#096A6A`/`#865309` (3.28:1 and 3.26:1, below 4.5) even though the table says dark selected text is white; and hover text on the neutral light fills (`#E8E8E8`/`#DCDCDC`) is near-invisible. The rendered test uses amber/dark only; tree is not rendered. | Overlay table `:240`; Avalonia 12.1.6 `Controls/ListBoxItem.xaml` (pointer-over, pressed, selected); `Controls/TreeViewItem.xaml` and `Accents/FluentControlResources.xaml:711-728`; contract table `:275-282`; record `§6.3` renders list/combo but not tree, and only amber/dark. |
| Major | **M-5 — The focus/indicator 3:1 floor is measured against the wrong worst surface.** `worstSurface(dark)` is defined as `#2D2D30`, but the dark input background and border are `#3C3C3C` and the hover fill is `#37373D`. `TextControlBorderBrushFocused` and Fluent focus adorners are adjacent to those surfaces. The table `FocusAccent` values pass 3.0:1 on `#2D2D30` but fail on `#3C3C3C` and `#37373D`. | Contract `:205-206`, `:213-215`; `Palette.axaml:24-26` (`InputBackground`/`InputBorderBrush`/`BorderBrush` `#3C3C3C`), `:42` (`HoverBackground` `#37373D`); computed values on `#3C3C3C` are 2.42–2.66 for all six accents (blue 2.45, teal 2.66, green 2.42, amber 2.60, violet 2.47, rose 2.42) and 2.60–2.85 on `#37373D`; `ThemeContrastTests.cs:64` omits both; acceptance criterion 4 in the record (`:307-308`) therefore cannot be met. |
| Minor | **m-1 — The test plan can let the missing keys and the System variant ship.** The matrix asserts only the §3.6 keys, the rendered checks do not cover checkbox/slider/toggle/tree, the high-contrast enumeration checks only overlay keys, and the System test is not an actual-variant test. | Record `§6.1` `:227-237`, `§6.3` `:249-260`, `§6.4` `:262-272`, `§6.5` `:274-284`; `ThemeContrastTests.cs:203,228,296`; `ThemeContrastTests.cs:64`. |
| Minor | **m-2 — The accent-resource inventory is inaccurate/incomplete.** It calls `SystemControlHighlightListAccentLow`/`Medium`/`High` the tree selected fill, but Fluent 12.1.6 tree rows use `SystemControlHighlightAccent3RevealBackgroundBrush` (0.6 opacity) and `Accent2` (0.8). It omits `SystemControlHighlightAltChromeWhiteBrush` (check-box/toggle glyphs) and `SystemControlForegroundAccentBrush` (slider thumb), and it says `Mocassin.SelectedBackground`/`SelectedForeground` are carried by the Fluent list/combo keys, which is not true for tree and not true for the default light `SystemControlHighlightAltBaseHighBrush` value. | Record `§2.2` table `:82-115`, especially `:91-97`; Avalonia 12.1.6 `Accents/FluentControlResources.xaml:711-728`, `:348-371`, `:520-529`, `:552-567`; `HighContrast.axaml` has no `SystemControlHighlightAltChromeWhiteBrush` entry (its `:53` is the shared `SystemControlHighlightAltBaseHighBrush`); `Palette.axaml:55`. |
| Note | **n-1 — Accent text is resource-only.** `Mocassin.AccentTextBrush` has no current view consumer, so A3's overlay-and-test scope proves the token, not a rendered link/accent-text surface. This is consistent with A3, but the test evidence and screenshots should not be read as proving a rendered accent-text surface. | Record `§2.2` `:86`; record `§10` `:349-350`; contract `§3.2` `:101-113`; no `DynamicResource Mocassin.AccentTextBrush` use in `src/dotnet/Mocassin.App.Views`. |

Severity values: **Blocker**, **Major**, **Minor**, **Note**. The planning auditor writes the findings and
verdict directly. The request owner adds resolutions in the separate `Resolution status` section without
altering the auditor's findings, evidence, or verdict.

### Finding details

- **M-1.** `App.axaml:4` requests `ThemeVariant.Default`, and `AvaloniaThemeApplier.Apply` maps
  `ThemeSelection.System` to the same `ThemeVariant.Default`. In Avalonia 12.1.6, a `ThemeVariant.Default`
  request on the application root is resolved by `ThemeVariant.UpdateActualThemeVariant` through
  `GetPlatformThemeVariant()`; `ActualThemeVariant` becomes `Light` or `Dark`. `ThemeVariant.Default` is
  the fallback dictionary key, not a promise of Light. The record's `System` tests pass
  `ThemeVariant.Default` directly to `TryGetResource`, so they read the fallback dictionary and do not
  emulate the app's actual resolution. On a dark desktop the overlay's light `AccentText` (`#005A9E`),
  light `SelectedFill`/`SelectedText`, and light `FocusAccent` values would be mixed with the dark
  palette; the light accent text on the dark card is 1.93:1. Either map `System` to
  `ThemeVariant.Light` while OS-dark is deferred, or derive the overlay from
  `Application.ActualThemeVariant` and test the actual dark/light resolution. Do not leave the stated
  assumption as written.
- **M-2.** The table is currently a 70% accent tint, not the 30% tint in the rule. `SelectedText` is
  derived from the fill, so the choice changes both the visual weight and a table row (light `rose`).
  Pick one weight, update the rule and the table together, and regenerate the `SelectedText` column from
  the chosen fills. The tests should assert the table values only after the table and rule agree.
- **M-3.** `SystemAccentColors.TryGetResource` returns `SystemAccentColorLight1`/`Dark1` from
  `_systemAccentColor` computed from `platformSettings.GetColorValues().AccentColor1`, not from the
  application's `SystemAccentColor` resource. The overlay's `SystemAccentColor` entry therefore does not
  reach those Fluent hover/pressed brushes. The Settings dialog's check boxes and slider are shipped
  surfaces; hovering a checked box or dragging the slider will show the platform accent (normally blue)
  instead of the selected accent. The test plan must either add those keys to the overlay or explicitly
  accept and render-test the platform-derived states.
- **M-4.** Fluent uses `SystemControlHighlightAltBaseHighBrush` for three different jobs: list/tree
  selected text, list/tree hover/pressed text, and some accent-control glyphs. The overlay's
  `OnAccent(FocusAccent)` value is correct only for the accent-control fill, not for the
  `SelectedFill` or the neutral hover fills. The design's own table says dark selected text is white, but
  the shared key would produce black for dark `teal`/`amber` (3.28:1 and 3.26:1 on the table fills) and
  white for light `green`/`violet` (3.07:1 and 3.59:1), so the table and the key disagree in both
  directions. The rendered amber/dark check should fail if it asserts the actual list foreground; the
  plan does not specify that assertion, and tree is not rendered. A property setter on
  `ListBoxItem`/`TreeViewItem` selected state (or a separate selected-foreground resource) is needed
  without adding a control template.
- **M-5.** The new focus role is motivated by the 3:1 focus floor, but the floor is measured on
  `#2D2D30` while the primary text input fill is `#3C3C3C`. The existing focus test's `focusSurfaces`
  list also omits `#3C3C3C` and `#37373D`. Either add the brighter dark surfaces to `worstSurface` and
  the test, or document why the input fill is not an adjacent surface; as written, acceptance criterion
  4 is not established.
- **m-1.** The specific holes are: no rendered check-box, slider, toggle, or tree-row state; no
  light-`green`/`violet` or dark-`teal`/`amber` selected-row text check; no actual
  `ActualThemeVariant` System test; no high-contrast render of the Fluent controls; and no assertion that
  the platform-derived `SystemAccentColorLight1`/`Dark1` values are unchanged or intentionally excluded.
- **m-2.** The inventory should list the keys by the Fluent role they actually serve in 12.1.6, not by
  the analogous list keys. In particular, `SystemControlHighlightAccent3RevealBackgroundBrush` and
  `SystemControlHighlightAccent2RevealBackgroundBrush` are the tree row fills and derive from
  `SystemAccentColor` at 0.6/0.8 opacity; `SystemControlHighlightAltChromeWhiteBrush` is the checked
  check-box/toggle glyph; `SystemControlForegroundAccentBrush` is the slider thumb and derives from
  `SystemAccentColor`. The `Mocassin.SelectedBackground`/`SelectedForeground` roles are not consumed by
  the current views.
- **n-1.** A3 deliberately defers a link surface; the note only prevents the acceptance evidence from
  claiming a rendered accent-text surface that does not exist.

## Record state

**State:** Changes required

Round 1 froze the design at `6ed4858f562ec9f29e1042b6ae4294bd3cac6fef`; its branch head
`5fb01f8ee4aecd6bd20333ccc25995fb9f5cfbcd` differed only by the one-line review-target update. Round 2
reviewed the revised design at `e7a62275c372d5f7485b6310646ea66b8fed2c78`; the re-review request at
`0b8beeac086d460349f4e49a10d031c06b60e3c4` adds only the request-owner resolutions and the re-review
request to this record.

`Changes required` is the round-2 verdict; `Re-review required` was the request state before round 2.

## Reviewer independence

I am a fresh context. I did not author, plan, or implement the reviewed design and I have no shared
conversation with the planner, implementor, or orchestrator. I read the repository at the frozen commit and
worked only in `docs/planning/reviews/step-gui-accent-propagation.md`; I did not edit the visual contract,
the maintenance record, any code, test, configuration, or other record.

## Verdict

- [ ] Ready for implementation
- [ ] Ready with notes
- [x] Changes required
- [ ] Waived

Follow-up owner: Planner (design corrections); the implementor starts only after a fresh re-review passes.
Follow-up step: — (out-of-step maintenance fix; no step tag)

## Approval summary

The request owner completes this section before the orchestrator presents a merge, tag, or Blocker/Major waiver
request. It is not required for the design-review verdict itself.

- [ ] Requested decision and frozen commit recorded
- [ ] Every review/audit round recorded (round, date, commit, reviewer, verdict)
- [ ] Every finding recorded (ID, severity, issue, evidence, resolution, fix commit, status)
- [ ] Accepted notes and waivers recorded with rationale
- [ ] Residual risks, deferred items, and follow-up owners recorded
- [ ] Explicit no-Blocker/no-Major statement, or the exact exceptions being waived
- [ ] Final CI and documentation-build results recorded

Round history:

| Round | Date | Commit | Reviewer | Verdict |
| --- | --- | --- | --- | --- |
| — | — | — | — | — |

Findings and resolutions:

| Finding | Severity | Issue | Resolution | Commit | Status |
| --- | --- | --- | --- | --- | --- |
| — | — | — | — | — | — |

## Human approval

- [ ] Accepted: Ready for implementation
- [ ] Accepted: Ready with notes
- [ ] Changes required
- [ ] Waived

Approved by:
Date:

## Resolution status

| Finding | Status | Owner | Commit |
| --- | --- | --- | --- |
| M-1 | Resolved in design | Planner | `e7a6227` |
| M-2 | Resolved in design | Planner | `e7a6227` |
| M-3 | Resolved in design | Planner | `e7a6227` |
| M-4 | Resolved in design | Planner | `e7a6227` |
| M-5 | Resolved in design | Planner | `e7a6227` |
| m-1 | Resolved in design | Planner | `e7a6227` |
| m-2 | Resolved in design | Planner | `e7a6227` |
| n-1 | Recorded; no design change required | Planner | `e7a6227` |

The request owner fills this section after the verdict. Do not alter the reviewer-authored findings, evidence,
or verdict.

### Resolutions to round 1 (request owner)

**M-1 — System uses the actual resolved variant.** Visual contract §3.6 now derives the theme column from
`Application.ActualThemeVariant` after `RequestedThemeVariant` is applied. `ThemeVariant.Dark` uses the dark
column; `Light` (including an unresolved `Default` fallback) uses the light column. `ThemeSelection.System`
therefore follows the OS variant instead of a fixed Light fallback. The applier gets an internal actual-variant
test seam; tests cover System-light and System-dark and assert the palette dictionary plus `AccentText`,
`SelectedFill`, `FocusAccent`, and `FluentState` columns. Maintenance record §4.1, §4.2, §6.4 and risks updated.

**M-2 — Selection blend reconciled at 30% accent / 70% chrome.** The intended A2 blend is 30% accent, so the
formula is retained and every optional `SelectedFill` row is corrected: dark `#153F3F`/`#233B24`/`#4A3515`/
`#352C41`/`#4A152D` and light `#AAD4D4`/`#AAD0B9`/`#E0CAAA`/`#CAC1D6`/`#DFAAC2` for
teal/green/amber/violet/rose. `SelectedText` is regenerated from the fills: dark is `#FFFFFF`; light is
`#1F1F1F` for every accent, including light `rose` on `#DFAAC2`. The contract table, record §4.1, and §6.2
tests now agree.

**M-3 — Derived Fluent accent states are overlaid, not left to the platform.** Visual contract §3.6 adds a
derived Fluent accent-state overlay published for every non-high-contrast accent, including `null`/`blue`:
`SystemAccentColorLight1`/`Light2`/`Light3`/`Dark1`/`Dark2`/`Dark3` (`Color`), `SystemControlForegroundAccentBrush`,
`SystemControlHighlightAccent2RevealBackgroundBrush`/`Accent3RevealBackgroundBrush`,
`SystemControlHighlightAltChromeWhiteBrush`, and `SystemControlForegroundChromeWhiteBrush`. The six colours are
`FluentState` values: Fluent 12.1.6's HSL offsets applied to `FocusAccent`, clamped to 3:1 against the worst
theme surface and `AccentGlyph`. `Light1`/`Dark1` are the keys consumed by checkbox, radio, toggle, and slider
hover/press in 12.1.6; `Light2`/`Light3`/`Dark2`/`Dark3` are overlaid for completeness and have no 12.1.6
control-resource consumer. High contrast removes the group and the dictionary supplies `#FFD700`/`#000000`.
Rendered checkbox/radio/toggle/slider tests are added. No platform-accent limitation is accepted; the only
recorded limitation is that the clamp can make a state equal the base when the base already sits on the 3:1
floor, which is a contrast-safe visual choice rather than a fallback.

**M-4 — Shared selected/hover text key no longer overloaded.** `SystemControlHighlightAltBaseHighBrush` is
removed from both overlays. It remains the shared selected/hover text role for ListBoxItem, TreeViewItem,
ComboBox focused/selected, MenuFlyout, and TabItem, and the palette is corrected to `#FFFFFF` dark /
`#1F1F1F` light (was `#FFFFFF` in light) / `#000000` high contrast. It meets 4.5:1 on every `SelectedFill` and
on the neutral hover/pressed fills. Accent-control glyphs moved to the dedicated
`SystemControlHighlightAltChromeWhiteBrush`/`SystemControlForegroundChromeWhiteBrush` keys (`#FFFFFF`, 3:1 on
`FocusAccent` and the derived states). Rendered light `green`/`violet` and dark `teal`/`amber` list/tree
selected and hover text tests are added, plus tree reveal fills.

**M-5 — Focus floor uses the bright dark surfaces.** `worstSurface(dark)` is now `#3C3C3C` (input fill); the
`#37373D` hover fill is darker and covered by the same choice. `FocusAccent` and the default dark
`Mocassin.FocusBrush` are recomputed: dark `#248DD3`/`#179595`/`#549457`/`#B97717`/`#927CB0`/`#CE6192` for
blue/teal/green/amber/violet/rose, light unchanged. Contract §3.6/§11.1 and record §4.1/§4.3/§6.2 are updated;
tests assert 3:1 on `#3C3C3C`, `#37373D`, `#2D2D30`, `#252526`, `#1E1E1E`, and light `#F3F3F3`/`#FFFFFF`.

**m-1 — Test plan expanded.** Added actual System-light/System-dark, rendered checkbox/radio/toggle/slider
normal/hover/press, tree selected normal/hover, list/combo selected and hover text, `#3C3C3C`/`#37373D` focus
surfaces, high-contrast rendered Fluent controls, derived-state overlay presence, and the explicit
resource-only `AccentTextBrush` assertion.

**m-2 — Inventory corrected.** The tree selected fill is `SystemControlHighlightAccent3RevealBackgroundBrush`
(normal) / `Accent2` (pointer-over); `SystemControlForegroundAccentBrush` (slider thumb),
`SystemControlHighlightAltChromeWhiteBrush` (check/toggle glyph), and `SystemControlForegroundChromeWhiteBrush`
consumers are listed; the six `SystemAccentColor` derived keys are listed; `Mocassin.SelectedBackground`/
`SelectedForeground` are marked not directly consumed; and `SystemControlHighlightAltBaseHighBrush` is listed
with its real selected/hover text consumers. High-contrast gaps for the derived/reveal/glyph keys are recorded.

**n-1 — Accent text is explicitly resource-only.** A3 stands: `Mocassin.AccentTextBrush` is overlaid and
contrast-tested but has no rendered surface today. The contract, record, and test plan now say so explicitly;
no rendered link/accent-text surface is claimed and no new link surface is added. No owner decision is needed.

**Re-review request.** The corrected design is frozen at `e7a62275c372d5f7485b6310646ea66b8fed2c78` on
`fix/gui-accent-propagation`. The branch head before this record update is `e7a6227`; the review request commit
adds only this record. A fresh planning auditor should verify the corrected contract and record against these
resolutions.

## Review rounds

Round 1 is recorded below. Append each later round below and never overwrite an earlier round.

### Review round 1

#### Independence and frozen state

Fresh reviewer (DeepSeek V4.1 Flash 1017, opencode-go), no shared conversation with the planner,
implementor, or orchestrator, and no authoring role in this design. Reviewed the frozen design at
`6ed4858f562ec9f29e1042b6ae4294bd3cac6fef` on `fix/gui-accent-propagation`. The branch head
`5fb01f8ee4aecd6bd20333ccc25995fb9f5cfbcd` differs from the frozen commit only by the one-line
review-target metadata update in this record (`git diff --name-status 6ed4858..5fb01f8` and `git diff
6ed4858..5fb01f8`). Base `main` is `19cb6ffe190e02449ce8b827706f365f2ea7afe2`, matching the record.

#### Commands and results

- `git rev-parse HEAD` — `5fb01f8ee4aecd6bd20333ccc25995fb9f5cfbcd`.
- `git rev-parse 6ed4858f562ec9f29e1042b6ae4294bd3cac6fef` — the frozen design commit.
- `git rev-parse main` — `19cb6ffe190e02449ce8b827706f365f2ea7afe2`.
- `git diff --name-status 6ed4858..5fb01f8` — only
  `M docs/planning/reviews/step-gui-accent-propagation.md`; `git diff` shows one changed line.
- `git diff --check` — clean.
- `python tools/check_docs_links.py` — `Documentation links OK (204 files scanned)`.
- `python tools/check_doc_math.py` — `Documentation math conventions OK (201 files scanned)`.
- `python tools/check_process_consistency.py` — `Process consistency: OK`.
- `python -m unittest discover -s tools/tests -t .` — 60 tests, all passed.
- `./tools/verify.sh docs` — exit 0; strict Zensical build, DocFX 465 models with 0 warnings/0 errors,
  rustdoc workspace built.
- WCAG recomputation for every table row and the additional dark surfaces; Fluent 12.1.6 source inspection
  for `SystemAccentColors`, `FluentControlResources`, `ListBoxItem`, `TreeViewItem`, `CheckBox`,
  `ProgressBar`, and `ThemeVariant` (details in the `Evidence` section).
- `git status --short` — clean before the record edit.

#### Findings

M-1 (Major): `ThemeSelection.System` is not a fixed Light fallback; `ThemeVariant.Default` follows the
OS variant, and the System test reads the fallback dictionary rather than `ActualThemeVariant`.
M-2 (Major): `SelectedFill`'s stated 30/70 blend and the 70/30 table are different; the table and rule
must be reconciled, and `SelectedText` changes with the choice.
M-3 (Major): Fluent `SystemAccentColorLight1`/`Dark1` states for checked check-box hover/press, slider
thumb hover/press, radio buttons, and toggles are not overlaid and revert to the platform/default blue.
M-4 (Major): `SystemControlHighlightAltBaseHighBrush` is shared by list/tree selected and hover text
and Fluent accent-control glyphs; the `OnAccent(FocusAccent)` overlay gives light `green`/`violet`
white selected text and dark `teal`/`amber` black selected text, neither of which matches the table or
the 4.5:1 floor, and it also affects hover text on neutral fills.
M-5 (Major): the 3:1 focus floor uses `#2D2D30` as the dark worst surface; the input fill `#3C3C3C` and
hover fill `#37373D` are brighter, and every `FocusAccent`/default `FocusBrush` value falls below 3:1 on
them.
m-1 (Minor): the test plan would let the M-1/M-3/M-4/M-5 surfaces ship because it does not render
checkbox/slider/toggle/tree states, does not test the actual System variant, and omits the input/hover
surfaces.
m-2 (Minor): the inventory mislabels the tree selected fill (Fluent Reveal keys) and omits
`SystemControlHighlightAltChromeWhiteBrush` and `SystemControlForegroundAccentBrush`.
n-1 (Note): `Mocassin.AccentTextBrush` is resource-only under A3; it has no rendered surface today.

#### Verdict

**Changes required.** Five Major findings remain: the System theme assumption (M-1), the `SelectedFill`
rule/table contradiction (M-2), the missing Fluent-derived accent states (M-3), the shared
selected-text key conflict (M-4), and the focus-floor surface error (M-5). The design must be corrected and
re-frozen before implementation; no implementation code exists on the branch.

---

### Re-review round 2

#### Independence and frozen state

I am a separate fresh context. I did not author, plan, implement, or review round 1; I have no shared
conversation with round 1, the planner, the implementor, or the orchestrator. I read the repository at the
revised frozen design and worked only in `docs/planning/reviews/step-gui-accent-propagation.md`. I did not edit
the visual contract, the maintenance record, code, tests, configuration, or any other record.

- Revised design freeze: `e7a62275c372d5f7485b6310646ea66b8fed2c78`.
- Branch head / re-review request: `0b8beeac086d460349f4e49a10d031c06b60e3c4`.
- `git diff --name-status e7a6227..0b8beea` reported only
  `M docs/planning/reviews/step-gui-accent-propagation.md`. The diff is the request-target update, the
  resolution-status table, the request-owner resolutions, and the re-review request; round 1's findings,
  evidence, verdict, and finding details are unchanged. No contract, record, code, test, or configuration
  change was made after the revised freeze.
- Base `main` is `19cb6ffe190e02449ce8b827706f365f2ea7afe2`; the branch is based on it.

#### Resolution verification

| Finding | Round-2 status | Evidence |
| --- | --- | --- |
| M-1 | **Resolved for the initial apply.** The derivation reads the resolved `Application.ActualThemeVariant` after `RequestedThemeVariant` is set; `ThemeVariant.UpdateActualThemeVariant` resolves `Default` through the platform and updates synchronously. A live OS Light/Dark switch is still unhandled (M-8), and the test-seam wiring is not specified to fail a requested-variant regression (m-5). | Contract §3.6 `:342-347`; Avalonia 12.1.6 `src/Avalonia.Base/Styling/ThemeVariant.cs` `UpdateActualThemeVariant`; record §4.1/§4.2/§6.4. |
| M-2 | **Partially resolved.** The 30 % accent / 70 % chrome rule is retained and eight of the ten optional rows match. Light `green` is wrong (`#AAD0B9` instead of `#B8D0B9`); dark `green` depends on an unspecified rounding mode (`#233B24` table, `#233A24` with .NET's default `Math.Round`). `SelectedText` is correct: dark white on every fill, light `#1F1F1F` on every fill including light `rose` `#DFAAC2` (8.37:1). | Contract §3.6 `:227-231`, `:315-328`; record §4.1 `:184-191`; recomputation below. |
| M-3 | **Partially resolved.** The six derived `SystemAccentColor` colours and the clamped `FluentState` table are exact, and `Light1`/`Dark1` reach checkbox/radio/toggle/slider hover/press through Fluent's dynamic `Color` bindings. The `SystemControlForegroundAccentBrush` (slider thumb) and `SystemControlHighlightAccentBrush` (accent-control fill) overlays do not reach the controls, because Fluent captures those keys in `StaticResource` aliases; the reveal fill and glyph overlays have the same problem (M-6). | Probe; Avalonia 12.1.6 `BaseResources.xaml:105,132,201-203`; `FluentControlResources.xaml:348,520,711-727`; `Controls/CheckBox.xaml:147-152`; `Controls/Slider.xaml:179,319,345`; `Controls/TreeViewItem.xaml:137-159`. |
| M-4 | **Partially resolved.** `SystemControlHighlightAltBaseHighBrush` is removed from both overlays and the palette is corrected to `#FFFFFF` dark / `#1F1F1F` light / `#000000` HC. The ListBoxItem/ComboBox/MenuFlyout/TabItem dynamic consumers pass 4.5:1; tree text uses the Fluent-captured value (black light / white dark), which still passes non-HC. In high contrast, the HC `#000000` value does not reach the captured tree/checkbox glyph aliases, so the HC tree text and checked glyph can fail (M-6). | Contract §3.6 `:307-313`; record §4.2; recomputation; probe. |
| M-5 | **Values resolved; delivery not resolved.** Every `FocusAccent` and the default dark `Mocassin.FocusBrush` `#248DD3` passes 3:1 on `#3C3C3C`, `#37373D`, `#2D2D30`, `#252526`, `#1E1E1E` and on light `#F3F3F3`/`#FFFFFF` (lowest: amber 3.001 on `#3C3C3C`; light teal 3.736). The Fluent slider thumb/track receives the raw accent instead of `FocusAccent`, so the shipped Settings slider is below 3:1 for dark `violet` (2.01), `rose` (1.93), and `green` (2.68) on the card (M-6). | Recomputation below; probe; contract §11.1 `:635`. |
| m-1 | **Partially resolved.** The rendered matrix now covers checkbox/radio/toggle/slider normal/hover/press, tree/list/combo selected and hover, the actual System Light/Dark cases, and the bright dark focus surfaces. Missing: a rendered high-contrast Fluent-control check, an assertion on the realized template-part brush (not only `TryGetResource`), and a System seam that would fail a requested-variant regression (m-5). | Record §6.1-§6.4; checklist notes above. |
| m-2 | **Partially resolved.** The reveal/Accent2-3, `SystemControlForegroundAccentBrush`, `SystemControlHighlightAltChromeWhiteBrush`, the six derived keys, and the real `AltBaseHigh` consumers are now listed. Still missing: `SystemControlHighlightChromeWhiteBrush` (toggle-knob pointer-over), the alias keys the controls actually consume, and the claim that `Light2`/`Light3`/`Dark2`/`Dark3` have no 12.1.6 consumer is false (`CalendarButton.xaml` uses `SystemAccentColorDark2`) (m-6). | Record §2.2; Avalonia 12.1.6 `Controls/CalendarButton.xaml:59,79,88`. |
| n-1 | **Confirmed honest.** The contract and record say `Mocassin.AccentTextBrush` is resource-only, the overlay and contrast test prove only the token, and no rendered link/accent-text surface is claimed; the screenshot section repeats the restriction. | Contract §3.6 `:355-357`; record §6.4-§6.5; no `DynamicResource Mocassin.AccentTextBrush` consumer in `src/dotnet/Mocassin.App.Views`. |

#### Commands and results (round 2)

- `git rev-parse HEAD` — `0b8beeac086d460349f4e49a10d031c06b60e3c4`.
- `git rev-parse e7a62275c372d5f7485b6310646ea66b8fed2c78` — the revised frozen design commit.
- `git rev-parse main` — `19cb6ffe190e02449ce8b827706f365f2ea7afe2`; `git merge-base --is-ancestor main HEAD` — exit 0.
- `git diff --name-status e7a6227..0b8beea` — only
  `M docs/planning/reviews/step-gui-accent-propagation.md`; the diff contains only the target metadata,
  the resolution table/resolutions, and the re-review request.
- `git diff --check` — clean; `git status --short` — clean before the record edit.
- `python3 tools/check_docs_links.py` — `Documentation links OK (204 files scanned)`.
- `python3 tools/check_doc_math.py` — `Documentation math conventions OK (201 files scanned)`.
- `python3 tools/check_process_consistency.py` — `Process consistency: OK`.
- `python3 -m unittest discover -s tools/tests -t .` — 60 tests, all passed.
- `./tools/verify.sh docs` — exit 0; strict Zensical build, DocFX 465 models with 0 warnings and 0 errors,
  rustdoc workspace built.
- `./tools/verify.sh policy` — exit 0; ApiCompat no breaking changes, contract artifacts current,
  space-group catalog current, process consistency OK, 60 policy tests passed, SPDX/secret scan/`cargo-deny`
  clean.
- WCAG recomputation (below) for every table row, the `SelectedText` pairs, every `FocusAccent` on every
  listed surface, the `FluentState` clamp, and the raw-accent slider surfaces.
- Avalonia 12.1.6 source inspection for `SystemAccentColors`, `ThemeVariant`, `BaseResources`,
  `FluentControlResources`, `CheckBox`, `Slider`, `TreeViewItem`, `RadioButton`, `ToggleSwitch`, and
  `CalendarButton`.
- A scratch Avalonia 12.1.6 headless probe (not committed, under `/home/node/tmp/aliasprobe`) applied an
  app-level `SystemAccentColor` `#B26A00` and red brush overrides for the base keys, then read the alias keys.
  Exact results: `CheckBoxCheckBackgroundFillChecked` and `SliderThumbBackground` resolved to `#B26A00`, not
  the red `SystemControlHighlightAccentBrush`/`SystemControlForegroundAccentBrush`; `TreeViewItemBackgroundSelected`
  and `...PointerOver` resolved to `#B26A00` at opacity 0.6/0.8, not the red reveal brushes;
  `CheckBoxCheckGlyphForegroundChecked` and `...PointerOver` stayed `#FFFFFF`, not the red
  `SystemControlHighlightAltChromeWhiteBrush`/`SystemControlForegroundChromeWhiteBrush`; and
  `SliderThumbBackgroundPointerOver` followed the red `SystemAccentColorLight1` (so the derived-state overlay
  does work). Writing a brush into a `SystemAccentColor*` key produced
  `InvalidCastException: Unable to cast object of type 'Avalonia.Media.SolidColorBrush' to type
  'Avalonia.Media.Color'` from the Fluent brush's `Color` binding.

#### Recomputation

- `SelectedFill(a, theme) = round(0.30 * a + 0.70 * chrome)`, `chrome(dark) = #1E1E1E`,
  `chrome(light) = #F3F3F3`. Teal `#153F3F`/`#AAD4D4`, amber `#4A3515`/`#E0CAAA`, violet
  `#352C41`/`#CAC1D6`, and rose `#4A152D`/`#DFAAC2` match exactly. Green: dark `#233A24` (table
  `#233B24`; the channel value is 58.5), light `#B8D0B9` (table `#AAD0B9`; R is 183.9, not 170).
- `SelectedText`: dark `#FFFFFF` on every dark fill is 11.57–14.62:1; light `#1F1F1F` on every light fill is
  8.37–10.36:1; light `rose` `#1F1F1F` on `#DFAAC2` is 8.37:1; white on `#DFAAC2` is 1.97:1.
- `FocusAccent` and `#248DD3`: dark values pass 3:1 on `#3C3C3C`/`#37373D`/`#2D2D30`/`#252526`/`#1E1E1E`
  (blue 3.060/3.280/3.809/4.248/4.625; teal 3.031/3.248; green 3.021/3.237; amber 3.001/3.217;
  violet 3.011/3.227; rose 3.020/3.237). Light values pass on `#F3F3F3`/`#FFFFFF` (blue 4.065/4.511,
  teal 3.736/4.145, green 4.621/5.127, amber 3.820/4.238, violet 6.169/6.845, rose 6.396/7.097).
- `AccentText` and the clamped `FluentState` values reproduce the contract table exactly (all 6 × 2
  `AccentText` and all 6 × 2 × 2 `Dark1`/`Light1` values, using the HSL offsets and the clamp toward the base).
- Raw accents on the Settings card `#2D2D30`: blue 3.04, teal 3.31, green 2.68, amber 3.24, violet 2.01,
  rose 1.93. On `#3C3C3C`: blue 2.45, teal 2.66, green 2.15, amber 2.60, violet 1.61, rose 1.55. The Fluent
  slider thumb/track uses the raw accent, so the 3:1 non-text floor is not met for the dark violet/rose/green
  cases on the card (and for all six on the input surface).
- High contrast: `#FFFFFF` on `#FFD700` is 1.35:1; `#FFFFFF` on the composited 60 % `#FFD700` over `#000000`
  (`#998100`) is 3.8:1; `#000000` on `#FFD700` is about 15.5:1.

#### Findings (round 2)

| Severity | Finding | Evidence |
| --- | --- | --- |
| Major | **M-6 — The base-key overlays do not reach Fluent controls that consume captured alias keys.** Fluent's `StaticResource` aliases (`CheckBoxCheckBackgroundFillChecked`, `SliderThumbBackground`, `SliderTrackValueFill`, `TreeViewItemBackgroundSelected*`, `CheckBoxCheckGlyphForeground*`, `RadioButtonCheckGlyphFill*`, `ToggleSwitchKnobFillOn*`) capture the base brush objects when the Fluent theme loads. Overlaying `SystemControlHighlightAccentBrush`, `SystemControlForegroundAccentBrush`, the reveal brushes, or the ChromeWhite brushes at application level therefore does not change the rendered controls. The normal checkbox/toggle/radio/slider fill is the raw `SystemAccentColor`, not `FocusAccent`; the slider thumb/track is below 3:1 for dark violet/rose/green on the card; the tree fill is the raw accent at 0.6/0.8, not `SelectedFill`; and the HC `AltChromeWhite`/`ForegroundChromeWhite`/`AltBaseHigh`/reveal values do not reach the captured aliases, leaving a white checked-checkbox glyph on gold (1.35:1) and, with HC + Dark, white tree selected text on 60 % gold (`#998100`, 3.8:1). The rendered test-plan wording ("assert the fill resolves to ...") does not require the realized template-part brush, so a `TryGetResource` assertion would pass while the control uses the captured brush. | Probe output above; Avalonia 12.1.6 `BaseResources.xaml:105,132,150-151,195-205`; `FluentControlResources.xaml:348-355,488-506,520-522,552-566,711-727`; `Controls/CheckBox.xaml:147-152`; `Controls/Slider.xaml:179,319,345`; `Controls/TreeViewItem.xaml:137-159`; contract `:263,276,301-305,313`; record §6.3. |
| Major | **M-7 — Contract §2 and §3.4 still describe the overlay as optional-accent-only and removed for `null`/`blue`, contradicting §3.6's two-group model.** §2 says "The accent overlay is an optional-accent-only direct-resource layer" and "For `null`, the explicit `blue` key, and high contrast, the applier removes every overlay key". §3.6 says the derived Fluent state group is published for every non-high-contrast accent, including `null`/`blue`, and the record §4.2 step 3 repeats that. §3.4's note also says the applier "for an optional accent" adds the §3.6 subset. An implementor following §2 would remove the derived group for `null`/`blue` and reintroduce M-3 for the default accent. | Contract `:57-67`, `:159-162`, `:201-206`, `:289-291`, `:349-351`; record §4.2 `:207-224`. |
| Major | **M-8 — A live OS Light/Dark switch is not reapplied to the accent overlay.** `StartupCoordinator` subscribes only to `settingsService.Changed`; it has no `ActualThemeVariantChanged` hook, and `AvaloniaThemeApplier.Apply` is only called for initial settings and settings changes. With `ThemeSelection.System`, the palette theme changes when the OS variant changes but the direct accent overlay keeps the old column. That reproduces the M-1 mismatch at runtime: light `AccentText` `#005A9E` on the dark card `#2D2D30` is 1.93:1, and the light `SelectedFill`/`FocusAccent`/`FluentState` values remain. The contract says "A theme switch with the same accent recomputes both groups", but no trigger or test is defined. | `src/dotnet/Mocassin.App/Startup/StartupCoordinator.cs:48-50`; `AvaloniaThemeApplier.cs:104-127`; contract §3.6 `:342-353`; record §6.4 `:326-331`; contrast computation. |
| Minor | **m-3 — Light `green` `SelectedFill` still does not match the 30/70 formula; dark `green` exposes an unspecified rounding mode.** Formula `round(0.30 * #2E7D32 + 0.70 * #F3F3F3)` = `#B8D0B9`; the contract and record both say `#AAD0B9`. Dark `green` is `#233A24` with .NET's default `Math.Round(58.5)` (banker's) and `#233B24` with round-half-up; the contract does not state which. `SelectedText` is unaffected (both light fills pass with `#1F1F1F`), so M-2 is not fully closed but not a contrast failure. | Contract `:227-229`, `:315-328`; record §4.1 `:184-191`; recomputation above. |
| Minor | **m-4 — The high-contrast six derived keys must be `Color`, but the design does not say so and the HC file's pattern is all brushes.** `SystemAccentColorLight1`/`Light2`/`Light3`/`Dark1`/`Dark2`/`Dark3` are consumed by `Color="{DynamicResource ...}"` bindings; a `SolidColorBrush` value throws `InvalidCastException` (observed in the probe). The contract §3.6 table says `Color` for the overlay, but record §5's "add ... as `#FFD700`" and the HC dictionary's existing `SolidColorBrush` entries do not state the type, and the HC test plan only asserts that the keys resolve. | Contract §3.6 `:293-305`; record §5 `:252`; `HighContrast.axaml` value pattern; probe exception. |
| Minor | **m-5 — The test plan still lacks rendered high-contrast Fluent controls and a realized-template-part oracle, and the System seam can bypass the production read.** §6.4 enumerates resources only; it does not render a checked CheckBox, slider, or tree in HC. The rendered cases say "assert the fill resolves to ..." without saying they read the realized control's brush, so M-6 would not be caught. The `System` seam "only supplies the resolved variant"; if it replaces the `Application.ActualThemeVariant` read, a regression to `RequestedThemeVariant` would still pass the System-dark test. | Record §6.1-§6.4, especially §6.4 `:328-339`; `ThemeContrastTests.cs` `TryGetResource` pattern; contract `:346-347`. |
| Minor | **m-6 — The inventory is still incomplete and one consumer claim is wrong.** `SystemControlHighlightChromeWhiteBrush` (the toggle-knob pointer-over glyph, Fluent `FluentControlResources.xaml:565,1411`) is absent from the inventory and the HC mapping, so it stays Fluent white in HC (white on gold, 1.35:1). `SystemAccentColorDark2` is consumed by `CalendarButton.xaml` (pointer-over/pressed border), so "no 12.1.6 consumer" for `Dark2` is false; `Light2`/`Light3`/`Dark3` also have no listed consumer, but the design overlays all six anyway. The `AltBaseHigh` consumer list also omits `TableViewRow*` and `DateTimePickerShared.xaml:63`. The app does not ship ToggleSwitch/RadioButton/Calendar/TableView today, so this is inventory/test accuracy, not a shipped-surface regression beyond the M-6 items. | Record §2.2 `:85`; Avalonia 12.1.6 `FluentControlResources.xaml:564-566`; `CalendarButton.xaml:59,79,88`; `BaseResources.xaml:150-151`; `DateTimePickerShared.xaml:63`; `FluentControlResources.xaml:838-846`. |
| Note | **n-2 — The maintenance record's commit list still has placeholders.** §13 says "(resolution design commit)" and "(re-review request commit)" instead of `e7a6227` and `0b8beea` (and `d359839` is also unlisted). The reviewer cannot edit the maintenance record; the planner should fill the SHAs at closeout. | Record `:481-492`. |
| Note | **n-3 — The Fluent default focus adorner is not `SystemControlHighlightAccentBrush`.** Contract §6 says the default adorner is acceptable "where it resolves to `SystemControlHighlightAccentBrush`"; Avalonia 12.1.6's default `FocusAdornerTemplate` uses `SystemControlFocusVisualPrimaryBrush`/`SecondaryBrush` (`SystemBaseHighColor`/`SystemAltMediumColor`), so the accent overlay does not colour it. The neutral ring still meets the 3:1 floor, but the §6 sentence should be corrected or the default ring explicitly accepted. | Contract `:421-425`; Avalonia 12.1.6 `Controls/AdornerLayer.xaml:9-17`; `BaseResources.xaml:102-104`. |

#### Verdict (round 2)

**Changes required.** M-1 and M-5 are now correct as values, and M-2/M-3/M-4 are materially improved, but three
Majors remain: the Fluent alias-capture gap (M-6), the §2/§3.4 precedence contradiction (M-7), and the missing
live OS-variant reapply (M-8). The design must be corrected and re-frozen. No implementation code exists on the
branch.

Follow-up owner: Planner (design corrections); the implementor starts only after a fresh re-review passes.
Follow-up step: — (out-of-step maintenance fix; no step tag).
