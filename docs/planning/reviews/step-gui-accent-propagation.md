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
| Reviewer | DeepSeek V4.1 Flash 1017 (opencode-go) — fresh context, no shared conversation with the planner, implementor, or orchestrator |

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

- [x] Accent overlay completeness and precedence (default/`blue`, optional accents, high contrast) — Reviewed; the precedence model is sound, but the overlay set is incomplete for Fluent-derived states (M-3) and the shared selected-text key (M-4); the inventory is inaccurate (m-2).
- [x] `OnAccent`, `AccentText`, `FocusAccent`, `SelectedFill`, and `SelectedText` derivation — Reviewed and recomputed. `OnAccent`, `AccentText`, `FocusAccent`, and `SelectedText` match the table and the stated floors; `SelectedFill` does not match its own formula (M-2). Focus floors are based on the wrong worst surface (M-5).
- [x] `Mocassin.FocusBrush` as the contrast-safe focus/indicator role — Reviewed; the role is well motivated, but the dark input/hover surfaces are not in the contrast oracle (M-5).
- [x] Status-bar background/foreground and progress contrast — Reviewed; all six accents pass 4.5:1 (`blue` 4.511 white, `teal`/`amber` black, `green`/`violet`/`rose` white) and the progress bar uses `StatusBarForeground` on a transparent track.
- [x] Selected list/tree/combo and text-selection treatment — Reviewed; combo and TextBox pairs are coherent, but list/tree selected and hover text share `SystemControlHighlightAltBaseHighBrush` with Fluent accent-control glyphs (M-4), and tree selected fill uses the Fluent Reveal keys rather than the documented list keys (m-2).
- [x] Default restoration and live reapply — Reviewed; the remove-then-publish logic and `StartupCoordinator.Changed` reapply are sound for explicit `Light`/`Dark`. `ThemeSelection.System` is not a fixed light fallback (M-1).
- [x] Test plan, contrast matrix, and screenshot scope — Reviewed; the matrix and screenshots are a good start, but the omissions would let the M-3/M-4/M-5 surfaces ship (m-1).
- [x] Documentation plan and acceptance criteria — Reviewed; the documentation touches the right sections, but acceptance criteria 1 and 4 are not met by the design as written (M-4, M-5).
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

**State:** Re-review required

The design is frozen for round 1 at `6ed4858f562ec9f29e1042b6ae4294bd3cac6fef`; the branch head
`5fb01f8ee4aecd6bd20333ccc25995fb9f5cfbcd` differs only by the one-line review-target update.

`Re-review required` is a record state, not a verdict.

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
