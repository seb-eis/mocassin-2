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
| Target commit | `5b2956b26e5cd4047edbbce52ecc191f01064916` |
| Docs branch | `fix/gui-accent-propagation` |
| Request owner | Planner |
| Orchestrator | — |
| Step agent (implementor) | — (no step; the fix is a bounded maintenance branch) |
| Review initiated by | Orchestrator, on request of the planner |
| Review date | 2026-11-22 |
| Reviewer | Round 1: DeepSeek V4.1 Flash 1017 (opencode-go), fresh context. Round 2: DeepSeek V4.1 Flash 1017 (opencode-go), a separate fresh context with no shared conversation with round 1, the planner, implementor, or orchestrator. Round 3: DeepSeek V4.1 Flash 1017 (opencode-go), a separate fresh context with no shared conversation with rounds 1–2, the planner, implementor, or orchestrator. Round 4: DeepSeek V4.1 Flash 1017 (opencode-go), a separate fresh context with no shared conversation with rounds 1–3, the planner, implementor, or orchestrator. Round 5: DeepSeek V4.1 Flash 1017 (opencode-go), a separate fresh context with no shared conversation with rounds 1–4, the planner, implementor, or orchestrator |

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

- [x] Accent overlay completeness and precedence (default/`blue`, optional accents, high contrast) — Reviewed; the precedence model is sound, but the overlay set is incomplete for Fluent-derived states (M-3) and the shared selected-text key (M-4); the inventory is inaccurate (m-2). Round 2: the two-group model is present, but §2 still says the overlay is optional-accent-only (M-7) and Fluent alias capture defeats the base-key overlays (M-6). Round 3: the fixed family removes the overlay; the palette alias keys deliver the CheckBox/Slider parts and the tree background, but the tree-foreground selector is ambiguous and the Fluent `Border > ContentPresenter` shape does not match (M-9); the HC focus pair and selected-tree chevron are not overridden (M-10, M-11). Round 4: M-9 selectors, M-10 HC pair, and M-11 chevron are present; the m-11 HC unchecked-pressed CheckBox label is black on the black HC surface (M-13). Round 5: the realized HC label is `#FFFFFF` on the black card in HC Light and Dark, the box is `#FFD700`, and the disabled aliases are `#A0A0A0` (probe); M-13/m-14 resolved, with the focus-ring test basis (m-16) and non-HC alias values (n-10) noted.
- [x] `OnAccent`, `AccentText`, `FocusAccent`, `SelectedFill`, and `SelectedText` derivation — Reviewed and recomputed. `OnAccent`, `AccentText`, `FocusAccent`, and `SelectedText` match the table and the stated floors; `SelectedFill` does not match its own formula (M-2). Focus floors are based on the wrong worst surface (M-5). Round 2: `FocusAccent`, `AccentText`, and all 24 `FluentState` values reproduce exactly; the light `green` `SelectedFill` still does not (m-3). Round 3: the fixed values replace the per-accent matrix; the recomputed floors pass (white on `#007ACC` 4.51, `#248DD3` on `#3C3C3C` 3.06, accent text 5.37–7.10, selected 9.76/12.73, white on the dark `Light1` 3.009). Round 4: the fixed floors and the realized TextBox selection pair reproduce (M-12 resolved).
- [x] `Mocassin.FocusBrush` as the contrast-safe focus/indicator role — Reviewed; the role is well motivated, but the dark input/hover surfaces are not in the contrast oracle (M-5). Round 2: every `FocusAccent` and `#248DD3` passes 3:1 on `#3C3C3C`, `#37373D`, `#2D2D30`, `#252526`, `#1E1E1E`, and the light surfaces; the Fluent slider thumb does not receive `FocusAccent` (M-6). Round 3: the focus values pass, but `Mocassin.FocusBrush` has no view consumer (the splitter adorner and tab pipe still use `AccentBrush`/`#007ACC`, m-8), and the HC neutral focus pair is not overridden (M-10). Round 4: splitter and tab-pipe consumers are specified; the tab pipe is resource-tested, the splitter adorner is not realized-tested (n-9).
- [x] Status-bar background/foreground and progress contrast — Reviewed; all six accents pass 4.5:1 (`blue` 4.511 white, `teal`/`amber` black, `green`/`violet`/`rose` white) and the progress bar uses `StatusBarForeground` on a transparent track. Round 3: status bar `#007ACC`/white passes 4.51; the shipped `ProgressDialogView` progress bar gets the dark focus `#248DD3` because `SystemControlHighlightAccentBrush` is mapped to `FocusBrush` (m-7). Round 4: the dialog ProgressBar realizes #007ACC/#248DD3/#FFD700 (m-7 resolved).
- [x] Selected list/tree/combo and text-selection treatment — Reviewed; combo and TextBox pairs are coherent, but list/tree selected and hover text share `SystemControlHighlightAltBaseHighBrush` with Fluent accent-control glyphs (M-4), and tree selected fill uses the Fluent Reveal keys rather than the documented list keys (m-2). Round 2: `AltBaseHigh` is removed from the overlays and the list/combo text passes; Fluent captures the tree/reveal aliases at load, so the reveal overlay does not set the tree fill and the HC tree text/glyph values do not reach the controls (M-6). Round 3: the list/combo keys and the targeted tree style are present; the HC selected-tree chevron remains white on gold (M-11), and the TextBox selection foreground is unset, so the §11.1 selection pair is 2.82/3.03/1.35 (M-12). Round 4: the direct header/chevron selectors and the realized TextBox pair verify (M-9, M-11, M-12); the HC disabled label gap remains (m-14). Round 5: the HC disabled labels/glyphs realize `#A0A0A0` and the TextBox pair is unchanged; m-14 resolved.
- [x] Default restoration and live reapply — Reviewed; the remove-then-publish logic and `StartupCoordinator.Changed` reapply are sound for explicit `Light`/`Dark`. `ThemeSelection.System` is not a fixed light fallback (M-1). Round 2: the initial System resolution now uses `Application.ActualThemeVariant`; a live OS Light/Dark switch still has no reapply trigger (M-8). Round 3: the fixed family in the palette theme dictionaries switches the realized alias and derived parts on a live Light/Dark change; no runtime overlay; M-8 is resolved.
- [x] Test plan, contrast matrix, and screenshot scope — Reviewed; the matrix and screenshots are a good start, but the omissions would let the M-3/M-4/M-5 surfaces ship (m-1). Round 2: the matrix and rendered cases were expanded, but there is no rendered high-contrast Fluent-control check and no realized-template-part assertion, and the System seam can bypass the production read (m-5); `SystemControlHighlightChromeWhiteBrush` is still missing (m-6). Round 3: the realized-part oracle is specified, but the focus/derived 3:1 sentence is an unsatisfiable cross-product (m-9), and the HC focus/chevron, ProgressBar, text selection, and palette `Color` type are missing (m-7, m-10). Round 4: the realized probes pass, but the HC unchecked-pressed label is not read (M-13). Round 5: §8.2 now reads `PART_ContentPresenter.Foreground`, the unchecked-pressed label, and the disabled labels/glyphs; the neutral focus ring's secondary-vs-primary basis is not stated (m-16).
- [x] Documentation plan and acceptance criteria — Reviewed; the documentation touches the right sections, but acceptance criteria 1 and 4 are not met by the design as written (M-4, M-5). Round 2: acceptance criteria 1/3/4 are still not established for the aliased Fluent controls (M-6), and §2 was not updated to match the two-group model (M-7). Round 3: §2, §3.4, and §3.6 now state one fixed-family precedence; acceptance criteria 5 and 7 are still not established for the HC chevron/focus and the TextBox selection (M-10–M-12). Round 4: M-10/M-12 are established; the HC unchecked-pressed label is black on black (M-13). Round 5: acceptance criteria 18–21 cover the HC label, disabled labels, splitter/tab-pipe, focus pair, and doc cleanup; n-7 is resolved.
- [x] Owner decisions A1–A3 and the superseded refresh wording — Reviewed; A1–A3 are recorded, internally consistent, and the supersession of the fixed-white status-bar and maximum-contrast `blue` wording is explicit. Round 3: A1–A4 are recorded; the fixed family, legacy no-op, and accent-text-resource-only decisions are consistent.
- [x] No ADR, interaction-contract, control-template, dependency, or format change — Reviewed; no ADR, interaction-contract, control-template, dependency, `contracts/manifest.json`, or format change is proposed. The `TextBox` selection pair is a property setter, not a template. Round 3: no ADR/template/format change; the Settings accent removal does remove a focus stop and change the Settings Tab order, which the maintenance record §13 calls unchanged (m-12). Round 4: the Tab-order delta is documented and tested; no other focus target changes.

## Dependency and interface checks

- [x] The base is current `main` at `19cb6ffe190e02449ce8b827706f365f2ea7afe2` — Verified: `git rev-parse main` is `19cb6ffe...`; the branch is based on it and `main` has not moved. Round 3: HEAD `7a8f5bc`; `73a297f..7a8f5bc` is only the review record; `main` is unchanged. Round 4: HEAD e8983ea; f1a8c1a..e8983ea is only this record; main is still 19cb6ffe. Round 5: HEAD 37cbce4; `5b2956b..37cbce4` is only this record; main is still `19cb6ffe`.
- [x] The design uses only existing accent keys and resources, plus the new derived `Mocassin.FocusBrush` — Verified: the six accent keys and five optional hex values are unchanged, and `FocusBrush` is a derived role in `Palette.axaml`/`HighContrast.axaml`, not a new selectable accent. Round 3: `FocusBrush` is added, the derived/alias keys are existing Fluent keys, but the neutral focus pair `SystemControlFocusVisualPrimaryBrush`/`SecondaryBrush` is not in the override set (M-10).
- [x] The overlay/removal interface with `AvaloniaThemeApplier` is defined — Reviewed: `RemoveAccentOverlay` and `PublishAccentOverlay` are specified, and the current `ApplyAccent` failure mode (`AvaloniaThemeApplier.cs:104-127`) matches the record. The key list is incomplete (M-3, m-2). Round 3: there is no runtime overlay; `ApplyAccent` sets `SystemAccentColor` only and the fixed family lives in the palette theme dictionaries; the alias/derived delivery is in the palette, not an overlay.
- [x] High-contrast and density precedence remain authoritative — Reviewed: the applier removes the overlay under high contrast, high contrast and density merged dictionaries keep their precedence, and density owns only spacing/size tokens. `ThemeSelection.System` remains a separate precedence defect (M-1). Round 3: the HC dictionary is merged after the palette and the probe confirms it wins; density is untouched; the HC focus pair and selected-tree chevron are still not overridden (M-10, M-11). Round 4: HC focus pair #FFD700/#000000 and the selected chevron #000000 on gold verify. Round 5: the HC label/focus-pair probes pass in both HC Light and Dark; density `.axaml` has no accent keys and is untouched.
- [x] No unresolved cross-language, format, or public-API interface question remains — Verified: no `contracts/manifest.json`, project/package/results/state format, cross-language fixture, or public API change is proposed. The remaining issues are visual-contract and Fluent-resource issues, not format/API ones. Round 3: the settings file remains app-local with no schema bump; the legacy `accent` key is ignored by the tolerant reader and omitted by the source-generated writer.

## Standard design-review checklist

- [x] Scope, non-goals, and acceptance criteria are explicit and testable — Reviewed; scope and non-goals are explicit, but acceptance criteria 1 (all accent surfaces) and 4 (3:1 focus floor) are not satisfied by the design as written (M-4, M-5). Round 3: the fixed-family scope is bounded and testable, but criteria 5 (HC glyph) and 7 (focus floors) are not established for the selected-tree chevron, the HC neutral focus pair, and the TextBox selection (M-10–M-12). Round 5: criterion 18 adds the realized label; the neutral-ring measurement basis remains m-16.
- [x] The change is bounded to one maintenance branch and one reviewable diff; no step sizing applies — Verified: one `fix/gui-accent-propagation` maintenance branch, docs only at this freeze, no code/tests yet, no step sizing. Round 3: still one docs branch at the freeze; the request commit adds only this record.
- [x] Design decisions are complete and internally consistent — Reviewed; M-1, M-2, and M-4 record contradictions between the stated rules, the table, and the actual Fluent keys. Round 3: §2/§3.4/§3.6 agree on the fixed-family precedence, but the TreeView selector (M-9), the HC focus pair (M-10), the HC chevron (M-11), and the TextBox selection (M-12) remain. Round 5: the sub-state aliases and their HC values agree with the Fluent template; the only inconsistency is the unstated non-HC alias values (n-10).
- [x] Required tests, contrast checks, and rendered-surface checks are defined — Reviewed; the plan is defined but incomplete for the Fluent-derived states, the tree row, and the actual `System` variant (m-1). Round 3: the realized-part tests are defined, but they omit the HC focus ring, the selected-tree chevron, the ProgressDialog progress bar, and the TextBox selection pair; the focus/derived 3:1 sentence is an unsatisfiable cross-product (m-7, m-9). Round 5: the realized label and disabled sub-states are added; the focus pair's light/dark measurement basis is m-16.
- [x] Documentation, evidence, and owner-approval updates are planned — Reviewed; §7 of the record names the right contract sections and the branch/merge evidence path. Round 3: the plan names §2/§3.4/§3.6/§6/§7.3/§11.1/§13 and the user/settings pages; `testing-and-parity.md` still lists `OnAccentBrush` as a runtime resource and §2's table still says "accent" (n-5). Round 5: §3.5/§3.6/§11.1 and acceptance criteria 18–21 are updated; quickstart/platform checklist are fixed; no stale user instruction remains.
- [x] Licensing, provenance, and dependency checks are resolved (no new dependency) — Verified: no new dependency, font, icon package, or third-party theme; the work uses Avalonia Fluent 12.1.6 and first-party tokens. Round 3: unchanged; no new dependency or copied asset.
- [x] Risks and mitigations are realistic — Reviewed; the risk table correctly names the shared-key conflict and the System assumption, but its mitigations are insufficient (M-1, M-4). Round 3: the risk table's "HC tree selected text is white on gold" mitigation covers the header text but not the selected-tree chevron (M-11), and the neutral focus ring is not mitigated for HC (M-10). Round 5: the HC label/disabled/focus risks are recorded; the disabled-checkbox risk is explicitly future-only.
- [x] Code layout and namespace plan follow the structure policy (no new source area) — Verified: no new source area; the `TextBox` changes belong in `ShellStyles.axaml`, the splitter changes in `ShellView.axaml`, and the applier changes in `Theming/AvaloniaThemeApplier.cs`. Round 3: the implementation shape omits the splitter adorner swap (it still uses `Mocassin.AccentBrush`, m-8) and the Settings Tab-order/automation delta (m-12).
- [x] No hidden scope, stale placeholders, or unowned decisions — Reviewed; no hidden scope, no stale placeholder text, and A1–A3 are owned. OS-dark support is explicitly deferred but the deferral assumes a light `System` variant that the app does not implement (M-1). Round 3: A1–A4 are owned; the maintenance record §15 still has a round-3 request placeholder and omits `82d3422`, and the claimed `AltBaseHigh` consumer list is not in the frozen docs (n-4). Round 4: the commit/consumer list is updated; the maintenance status row still says round 3 (n-7); quickstart/platform checklist still mention the accent (m-15). Round 5: the status row says round 5, the round-5 design/request commits are in §15, and quickstart/platform checklist no longer mention the accent; n-7/m-15 resolved.

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

**State:** Ready with notes

Round 1 froze the design at `6ed4858f562ec9f29e1042b6ae4294bd3cac6fef`; its branch head
`5fb01f8ee4aecd6bd20333ccc25995fb9f5cfbcd` differed only by the one-line review-target update. Round 2
reviewed the revised design at `e7a62275c372d5f7485b6310646ea66b8fed2c78`; the re-review request at
`0b8beeac086d460349f4e49a10d031c06b60e3c4` adds only the request-owner resolutions and the re-review
request to this record.

Round 3 reviewed the fixed-family design at `73a297ff7d73978a58c4f34d1eb24396a44457a2`; the round-3
request commit `7a8f5bc490f187773e54c03f65b6acfbb0cbb0ec` differs only by this record. Four Majors remain:
the TreeView targeted-selector ambiguity (M-9), the HC neutral focus pair (M-10), the HC selected-tree
chevron (M-11), and the unimplemented TextBox selection pair (M-12). M-6 is resolved for the CheckBox/Slider
parts and the tree background, and M-7/M-8 are resolved.

`Changes required` is the round-2 verdict; `Re-review required` was the request state before round 2.

Round 4 reviewed the delivery-detail design at `f1a8c1a7d1ee704b121964a8c7d030773488d859`; the round-4 request
`e8983eac4b22d5cb9b5b54be0ee30ca0ebef31c6` differs only by this record. M-9, M-10, and M-12 are resolved, and
m-7, m-9, m-10, m-12, n-4, n-5, and n-6 are resolved; m-8 is design-resolved with a test-coverage note. A new
Major remains: the HC unchecked-pressed CheckBox label is black on the black HC surface (M-13). Two Minors
(m-14, m-15) and three Notes (n-7, n-8, n-9) also remain. `Changes required` is the round-3 verdict and the
round-4 verdict.

Round 5 reviewed the HC-label delivery design at `5b2956b26e5cd4047edbbce52ecc191f01064916`; the round-5
request `37cbce4bc3d0e93fd19034e8ceb8f0879097d1be` differs only by this record. M-13, m-14, m-15, n-7, n-8,
and n-9 are resolved. One Minor (m-16, the neutral focus-ring measurement basis) and one Note (n-10, the
non-HC values of the four check-box label sub-state aliases) remain; no Major remains. `Ready with notes` is
the round-5 verdict.

## Reviewer independence

I am a fresh context. I did not author, plan, or implement the reviewed design and I have no shared
conversation with the planner, implementor, or orchestrator. I read the repository at the frozen commit and
worked only in `docs/planning/reviews/step-gui-accent-propagation.md`; I did not edit the visual contract,
the maintenance record, any code, test, configuration, or other record.

## Verdict

- [ ] Ready for implementation
- [x] Ready with notes
- [ ] Changes required
- [ ] Waived

Follow-up owner: Planner (clarify m-16/n-10 or record their acceptance); the human owner may accept the notes
and approve the start.
Follow-up step: — (out-of-step maintenance fix; no step tag)

Round 3: `Changes required`. M-6 is resolved for the CheckBox/Slider parts and the tree background, and
M-7/M-8 are resolved, but four Majors remain: the TreeView targeted-selector ambiguity (M-9), the HC neutral
focus pair (M-10), the HC selected-tree chevron (M-11), and the unimplemented TextBox selection pair (M-12).
The fixed-family values themselves pass the recomputed floors.

Round 4: `Changes required`. M-9, M-10, and M-12 are resolved, and m-7, m-9, m-10, m-12, n-4, n-5, and n-6
are resolved; m-8 is design-resolved with a test-coverage note. The m-11 HC unchecked-pressed CheckBox label is
black on the black HC surface (M-13), so the design must be corrected and re-frozen. The fixed-family values and
the recomputed floors still pass.

Round 5: `Ready with notes`. M-13, m-14, m-15, n-7, n-8, and n-9 are resolved: the realized HC unchecked-pressed
label is `#FFFFFF` on the black card, the pressed box is `#FFD700`, the three disabled label aliases and glyphs
are `#A0A0A0`, the neutral focus pair is explicit, and the splitter/tab-pipe are in the realized test list. One
Minor (m-16, the neutral focus-ring measurement basis) and one Note (n-10, the non-HC values of the four
check-box label sub-state aliases) remain; no Major remains. The human owner may accept the notes and approve
the start.

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
| M-6 | Resolved by fixed-family delivery proof | Planner | `82d3422` |
| M-7 | Superseded by the fixed-family precedence | Planner | `82d3422` |
| M-8 | Superseded; palette theme dictionaries switch automatically | Planner | `82d3422` |
| m-3 | Superseded; no per-accent 30/70 blend remains | Planner | `82d3422` |
| m-4 | Resolved; HC derived keys are `Color` | Planner | `82d3422` |
| m-5 | Resolved; realized-part and production theme-switch tests specified | Planner | `82d3422` |
| m-6 | Resolved; inventory corrected and unshipped controls marked | Planner | `82d3422` |
| n-2 | Resolved; commit list filled | Planner | `82d3422` |
| n-3 | Resolved; neutral Fluent focus ring accepted and tested | Planner | `82d3422` |
| M-9 | Resolved; exact direct TreeView selectors named | Planner | `f1a8c1a` |
| M-10 | Resolved; HC focus pair `#FFD700`/`#000000` added and tested | Planner | `f1a8c1a` |
| M-11 | Resolved; selected chevron `Fill` set and tested | Planner | `f1a8c1a` |
| M-12 | Resolved; TextBox selection foreground pair set and tested | Planner | `f1a8c1a` |
| m-7 | Resolved; dialog ProgressBar added to inventory/tests | Planner | `f1a8c1a` |
| m-8 | Resolved; `FocusBrush` consumed by splitter and tab pipe | Planner | `f1a8c1a` |
| m-9 | Resolved; paired focus surfaces stated | Planner | `f1a8c1a` |
| m-10 | Resolved; palette `SystemAccentColor*` type stated | Planner | `f1a8c1a` |
| m-11 | Resolved; HC unchecked-pressed/disabled sub-states specified | Planner | `f1a8c1a` |
| m-12 | Resolved; Settings Tab-order delta documented and tested | Planner | `f1a8c1a` |
| n-4 | Resolved; commit list and AltBaseHigh consumers updated | Planner | `f1a8c1a` |
| n-5 | Resolved; stale testing/contract rows updated | Planner | `f1a8c1a` |
| n-6 | Resolved; dead applier helpers listed for removal | Planner | `f1a8c1a` |
| M-13 | Resolved; HC pressed label white on black card | Planner | `5b2956b` |
| m-14 | Resolved; HC disabled label aliases `#A0A0A0` | Planner | `5b2956b` |
| m-15 | Resolved; quickstart/platform checklist accent text removed | Planner | `5b2956b` |
| n-7 | Resolved; record status says round 5 | Planner | `5b2956b` |
| n-8 | Resolved; neutral focus pair specified for Light/Dark/HC | Planner | `5b2956b` |
| n-9 | Resolved; splitter FocusAdorner added to realized tests | Planner | `5b2956b` |

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

### Scope change and round 2 resolutions (request owner)

**Owner decision (2026-11-22).** Accent selectability is removed. The design is one fixed `#007ACC` family:
fill/status `#007ACC`, on-accent white, dark focus `#248DD3`, light focus `#007ACC`, accent text
`#4DA6FF`/`#005A9E`, selected `#094771`/white dark and `#CCE5FF`/`#1F1F1F` light. The Settings choice,
`AccentPalette`, `AppSettings.Accent`, the reader accent branch, and the validator path are removed. Existing
settings with an `accent` value are accepted as a silent no-op and are not written; no schema bump.

**M-3 — per-accent derived states: superseded.** There is no selectable accent and no per-accent overlay. The
fixed derived Fluent values remain for the shipped controls (dark `Light1` `#3A9CDD`, `Dark1` `#248CD1`; light
`Light1` `#0090F0`, `Dark1` `#005994`), but they live in the palette theme dictionaries rather than a runtime
overlay.

**M-4 — shared selected/hover text: resolved by the fixed family.** `SystemControlHighlightAltBaseHighBrush`
stays the shared selected/hover text role at `#FFFFFF` dark / `#1F1F1F` light / `#000000` HC. The TreeView
selected foreground is delivered by the targeted `PART_HeaderPresenter` style, so it no longer depends on the
captured alias.

**M-5 — focus values: retained, per-accent matrix removed.** Dark focus is `#248DD3`, light focus `#007ACC`;
the values pass 3:1 on `#3C3C3C`/`#37373D` and the light surfaces. The optional-accent focus matrix is
withdrawn.

**M-7 — precedence contradiction: superseded.** There are no overlay groups. The fixed base roles, derived
Fluent colours, and CheckBox/Slider alias keys live in the palette `Light`/`Dark`/`Default` dictionaries;
`HighContrast.axaml` overrides them; the only direct runtime accent resource is `SystemAccentColor`
(`#007ACC`/`#FFD700` HC). §2, §3.4, and §3.6 now state the same model.

**M-8 — live OS variant: superseded.** With a fixed family in palette theme dictionaries, an actual Light/Dark
switch updates the base roles, derived colours, and alias keys automatically. There is no runtime accent
overlay and no `ActualThemeVariantChanged` accent reapply. Tests switch the theme and assert the realized
CheckBox/Slider/Tree parts.

**M-6 — Fluent alias capture: gating finding, now resolved by proof.** The design includes the concrete
delivery proof:

- Load-order probe: defining `SystemAccentColor` before `FluentTheme` loaded made
  `CheckBoxCheckBackgroundFillChecked`, `SliderThumbBackground`, `SliderTrackValueFill`, and
  `TreeViewItemBackgroundSelected*` capture the app colour, but glyphs stayed white and the tree captured
  0.6/0.8 opacity; it cannot express the distinct selected/focus values or follow Light/Dark/HC. Rejected as
  the sole mechanism.
- Palette alias keys: `CheckBoxCheckBackgroundFillChecked`/`Indeterminate`,
  `CheckBoxCheckBackgroundStrokeIndeterminate`, the checked/indeterminate `CheckBoxCheckGlyphForeground*`,
  `SliderThumbBackground`, and `SliderTrackValueFill*` reached the realized `NormalRectangle`,
  `CheckGlyph`, `Thumb`, and `PART_DecreaseButton` in the probe, and switched with the Light/Dark theme
  dictionaries. The high-contrast merged dictionary reached the same CheckBox/Slider parts as
  `#FFD700`/`#000000`.
- Targeted TreeView styles: the palette alias key reached `PART_LayoutRoot.Background` but not
  `PART_HeaderPresenter.Foreground`; the targeted `ShellStyles.axaml` selectors reached both realized parts.
  Mirroring Fluent's selected/pointer-over/pressed selectors is in the plan.

No broad custom template is required, and no shipped control keeps the platform accent.

**m-3 — 30/70 blend:** superseded; the fixed selected fill/text are the existing palette values, so the
green row and rounding-mode finding no longer apply.

**m-4 — HC `Color` type:** the six `SystemAccentColor*` HC keys are specified as `<Color>` resources, not
brushes; tests assert the type and a brush would throw `InvalidCastException`.

**m-5 — realized HC and production System path:** the test plan renders CheckBox/Slider/Tree in HC and reads
the realized parts; theme-switch tests read the palette dictionaries and realized parts. There is no runtime
seam that bypasses the production theme path.

**m-6 — inventory:** `SystemControlHighlightChromeWhiteBrush` is added to the inventory/HC mapping;
`SystemAccentColorDark2` is recorded as a `CalendarButton` consumer; `TableViewRow*` and
`DateTimePickerShared.xaml:63` are added to the `AltBaseHigh` consumer list; unshipped controls are marked as
inventory-only.

**n-2 — commit list:** `d359839`, `e7a6227`, `0b8beea`, `cbf2d90`, `787c4ed`, `c7532fb`, `6db42cf`, and
`82d3422` are recorded in the maintenance record's commit list.

**n-3 — neutral focus ring:** the contract now states that Fluent's default focus adorner uses
`SystemControlFocusVisualPrimaryBrush`/`SecondaryBrush`, accepts the neutral ring, and tests it at 3:1. It is
not accent-coloured.

**Re-review request.** The fixed-family design is frozen at
`73a297ff7d73978a58c4f34d1eb24396a44457a2` on `fix/gui-accent-propagation`; the request commit adds only this
record. A fresh planning auditor should verify the fixed-family contract and the M-6 delivery proof.

### Round 4 delivery resolutions (request owner)

**M-9 — exact TreeView selectors.** The contract now names the exact working selectors:
`TreeViewItem:selected /template/ Border#PART_LayoutRoot`,
`TreeViewItem:selected /template/ Border#PART_LayoutRoot:pointerover`,
`TreeViewItem:pressed:selected /template/ Border#PART_LayoutRoot:pointerover`,
`TreeViewItem:selected /template/ ContentPresenter#PART_HeaderPresenter` (direct descendant), and
`TreeViewItem:selected /template/ ToggleButton#PART_ExpandCollapseChevron /template/ Path#ChevronPath`. The
Fluent-shaped `Border#PART_LayoutRoot > ContentPresenter#PART_HeaderPresenter` is explicitly rejected because
the presenter's visual parent is `Grid#PART_Header`; the probe confirmed the direct selector works and the `>`
shape does not. Tests render the header foreground in Light, Dark, and HC.

**M-10 — HC focus visual pair.** `HighContrast.axaml` now defines
`SystemControlFocusVisualPrimaryBrush` `#FFD700` and `SystemControlFocusVisualSecondaryBrush` `#000000`, and
§3.4 includes both. The 2 px primary is gold on black and the secondary is black on gold. A rendered focus
adorner test in HC Light and Dark asserts 3:1.

**M-11 — HC selected-tree chevron.** The targeted style now sets
`Path#ChevronPath.Fill` from `Mocassin.SelectedForeground`; HC is `#000000` on `#FFD700`, not white on gold.
The rendered selected-tree test reads the chevron fill in HC Light and Dark at 3:1.

**M-12 — TextBox selection foreground.** `ShellStyles.axaml` sets
`TextBox.SelectionBrush="{DynamicResource Mocassin.AccentBrush}"` and
`TextBox.SelectionForegroundBrush="{DynamicResource Mocassin.OnAccentBrush}"`. The rendered pair is white on
`#007ACC` (4.51:1) and black on HC gold (14.97:1). This is a fix, not a deferral.

**m-7 — ProgressBar inventory.** The `ProgressDialog` `ProgressBar` is added to the shipped-control table and
the realized-part tests; its `Foreground` is `#007ACC` light, `#248DD3` dark, and `#FFD700` HC through
`SystemControlHighlightAccentBrush`.

**m-8 — `Mocassin.FocusBrush` consumer.** The three splitter `FocusAdorner` borders now use
`Mocassin.FocusBrush`, and `TabItemHeaderSelectedPipeFill` uses the same value (`#248DD3` dark / `#007ACC`
light / `#FFD700` HC). The role is no longer unused.

**m-9 — paired focus surfaces.** The record now states the paired test: dark focus values on `#3C3C3C`,
`#37373D`, `#2D2D30`, `#252526`, `#1E1E1E`; light focus values on `#F3F3F3`/`#FFFFFF`. It is not an
unsatisfiable cross-product.

**m-10 — palette `SystemAccentColor*` type.** §3.4 and §3.6 state that the six keys are `Color` resources in
`Default`/`Light`/`Dark` and HC; tests assert the type.

**m-11 — HC check-box sub-states.** HC now defines `CheckBoxForegroundUncheckedPressed` `#000000`,
`CheckBoxCheckBackgroundFillUncheckedPressed` `#FFD700`, checked/indeterminate disabled fills `#000000`, and
disabled glyph aliases `#A0A0A0`; rendered tests cover unchecked-pressed and checked/indeterminate-disabled.

**m-12 — Settings Tab order.** The Accent ComboBox removal is documented as an intentional accessibility
delta: the focus stop and `Accent` automation name are gone; the new deterministic order is tested and no
other focus target changes. The interaction-contract rules do not change.

**n-4 — record accuracy.** The maintenance commit list now includes `82d3422`, `73a297f`, `7a8f5bc`,
`7ba3600`, and the round-4 commits; the inventory names `TableViewRow*`/`DateTimePickerShared.xaml:63` and the
unshipped `CalendarButton` `Dark2` consumer.

**n-5 — stale documentation.** `testing-and-parity.md` no longer lists `OnAccentBrush` as a runtime resource,
and the contract runtime-settings row no longer says “accent”.

**n-6 — dead applier helpers.** The record lists `AccentColor`, `OnAccent`, `Contrast`, `RelativeLuminance`,
`Linear`, and the `lightOnAccent`/`darkOnAccent` constants for removal with the accent path.

**Re-review request.** The delivery-detail design is frozen at
`f1a8c1a7d1ee704b121964a8c7d030773488d859` on `fix/gui-accent-propagation`; the request commit adds only this
record. A fresh planning auditor should verify the fixed-family contract and the M-9–M-12 delivery details.

### Round 5 HC-label resolutions (request owner)

**M-13 — HC unchecked-pressed CheckBox label.** The pressed **box** is
`CheckBoxCheckBackgroundFillUncheckedPressed` `#FFD700`, but the label is `PART_ContentPresenter` on the
transparent row/card (`#000000`). `CheckBoxForegroundUncheckedPressed` is therefore `#FFFFFF`, not black. The
realized label foreground, not only `NormalRectangle`/`CheckGlyph`, is read in HC Light and Dark.

**m-14 — HC disabled CheckBox labels.** HC now overrides the captured Fluent aliases
`CheckBoxForegroundUncheckedDisabled`, `CheckBoxForegroundCheckedDisabled`, and
`CheckBoxForegroundIndeterminateDisabled` to `#A0A0A0`; checked/indeterminate disabled fills remain `#000000`
and disabled glyphs `#A0A0A0`. No shipped surface currently disables a CheckBox, so these are
component-level future-proofing tests and a recorded risk for future disabled controls.

**m-15 — stale user instructions.** `docs/getting-started/quickstart.md:57` and
`docs/development/platform-checklist.md:95` no longer tell users to change the removed accent.

**n-7 — record status.** The maintenance record status now says “Design frozen for round 5 review”.

**n-8 — neutral focus pair.** The palette now specifies `SystemControlFocusVisualPrimaryBrush`/
`SecondaryBrush`: light `#000000`/`#FFFFFF`, dark `#FFFFFF`/`#000000`, HC `#FFD700`/`#000000`. The realized
focus ring is tested in Light, Dark, and HC at 3:1.

**n-9 — splitter FocusAdorner.** The three splitter `FocusAdorner` borders use `Mocassin.FocusBrush` and are
added to the §8.2 realized-part test list, together with the selected-tab pipe.

**Re-review request.** The HC-label delivery design is frozen at
`5b2956b26e5cd4047edbbce52ecc191f01064916` on `fix/gui-accent-propagation`; the request commit adds only this
record. A fresh planning auditor should verify the fixed-family HC label/focus details.

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

---

### Re-review round 3

#### Independence and frozen state

I am a separate fresh context. I did not author, plan, implement, or review rounds 1–2, and I have no shared
conversation with rounds 1–2, the planner, the implementor, or the orchestrator. I read the repository at the
frozen fixed-family design `73a297ff7d73978a58c4f34d1eb24396a44457a2` and worked only in
`docs/planning/reviews/step-gui-accent-propagation.md`; I did not edit the visual contract, the maintenance
record, code, tests, configuration, or any other record.

- Frozen design: `73a297ff7d73978a58c4f34d1eb24396a44457a2`.
- Branch head / round-3 request: `7a8f5bc490f187773e54c03f65b6acfbb0cbb0ec`.
- `git diff --name-status 73a297f..7a8f5bc` reported only
  `M docs/planning/reviews/step-gui-accent-propagation.md`; `git diff --stat` is 89 insertions and 2 deletions.
  The diff is the target metadata, the new resolution-status rows, the fixed-family resolutions, and the
  round-3 request. No contract, maintenance record, code, test, or configuration change was made after the
  frozen design.
- Base `main` is `19cb6ffe190e02449ce8b827706f365f2ea7afe2`; `git merge-base --is-ancestor main HEAD` exited 0.

#### Resolution verification (round 3)

| Finding | Round-3 status | Evidence |
| --- | --- | --- |
| M-1 | **Superseded** by the fixed family. There is no runtime accent overlay; the palette theme dictionaries follow the actual variant, and `SystemAccentColor` is theme-independent. | Contract §3.6; live-switch probe. |
| M-2 | **Superseded.** The fixed selected values are the existing palette values; there is no 30/70 blend. | Contract §3.3; record §3. |
| M-3 | **Superseded.** The fixed derived values live in the palette; `SystemAccentColorLight1`/`Dark1` reach the CheckBox/Slider hover/press brushes via their dynamic `Color` bindings. | Probe; contract §3.6. |
| M-4 | **Mostly resolved.** `AltBaseHigh` is #FFFFFF dark / #1F1F1F light / #000000 HC, and the ListBox/ComboBox consumers pass. The tree foreground depends on the targeted selector (M-9), and the HC chevron is new (M-11). | Probe; contract §3.4/§3.6. |
| M-5 | **Values resolved.** `#248DD3` passes 3:1 on `#3C3C3C`/`#37373D`; light `#007ACC` passes on `#F3F3F3`/`#FFFFFF`. The HC neutral focus pair is not overridden (M-10). | Recomputation; probe. |
| m-1 | **Partially resolved.** The realized-part oracle is specified, but the HC focus ring, selected-tree chevron, ProgressDialog progress bar, and TextBox selection pair are not rendered/read (M-10, M-11, M-12, m-7). | Record §8. |
| m-2 | **Mostly resolved.** `SystemControlHighlightChromeWhiteBrush` and the `Dark2` CalendarButton consumer are recorded; the claimed `AltBaseHigh` TableViewRow/DateTimePicker consumers are not in the frozen docs (n-4). | Record §2/§4; contract §3.4. |
| n-1 | **Confirmed.** `Mocassin.AccentTextBrush` remains resource-only and no rendered link/accent-text surface is claimed. | Contract §3.6; no view consumer. |
| M-6 | **Resolved for the CheckBox/Slider parts and the tree background.** The palette alias keys reach `NormalRectangle`, `CheckGlyph`, `Thumb`, `PART_DecreaseButton`, and `PART_LayoutRoot`; HC reaches the same parts with `#FFD700`/`#000000`. The tree **foreground** delivery is not unambiguous (M-9), the HC selected-tree chevron is not covered (M-11), and the ProgressDialog progress bar is on the focus tint (m-7). | Probe; Avalonia 12.1.6 sources. |
| M-7 | **Resolved.** §2, §3.4, and §3.6 state one fixed-family precedence: HC over palette over stock Fluent, with `SystemAccentColor` the one direct runtime accent resource. | Contract §2/§3.4/§3.6. |
| M-8 | **Resolved.** The fixed family in the palette theme dictionaries updates the realized alias and derived parts on a live Light/Dark switch; there is no runtime accent overlay or `ActualThemeVariantChanged` reapply. | Live-switch probe; contract §3.6. |
| m-3 | **Superseded.** No per-accent 30/70 blend remains. | Contract §3.3. |
| m-4 | **Resolved for HC `Color`.** The six HC keys are `<Color>`. The palette table does not state the type (m-10). My brush-in-`SystemAccentColor*` probe did not throw on the normal/hover path; the type assertion, not the exception, is the reliable oracle. | Contract §3.5; record §5/§7/§8.1; probe. |
| m-5 | **Partially resolved.** The HC realized parts and the theme-switch parts are specified; the HC focus ring and selected-tree chevron are still missing (M-10, M-11). The `System` test uses the production platform settings, not an `ActualThemeVariant` seam. | Record §8.3; contract §3.6. |
| m-6 | **Partially resolved.** `SystemControlHighlightChromeWhiteBrush` and `Dark2` are recorded; the `AltBaseHigh` consumer claim is not (n-4). | Record §2/§4. |
| n-2 | **Partially resolved.** The maintenance record §15 still has a round-3 request placeholder and omits `82d3422` (n-4). | Maintenance record §15. |
| n-3 | **Stated, not delivered for HC.** The neutral Fluent pair is accepted and named; HC does not override it (M-10). | Contract §6; probe. |

#### Commands and results (round 3)

- `git rev-parse HEAD` — `7a8f5bc490f187773e54c03f65b6acfbb0cbb0ec`.
- `git rev-parse 73a297ff7d73978a58c4f34d1eb24396a44457a2` — the frozen fixed-family design.
- `git rev-parse main` — `19cb6ffe190e02449ce8b827706f365f2ea7afe2`; `git merge-base --is-ancestor main HEAD` —
  exit 0.
- `git diff --name-status 73a297f..7a8f5bc` — only
  `M docs/planning/reviews/step-gui-accent-propagation.md`; `git diff --stat` — 89 insertions, 2 deletions.
- `git diff --check` — clean; `git status --short` — clean before the record edit.
- `python3 tools/check_docs_links.py` — `Documentation links OK (204 files scanned)`.
- `python3 tools/check_doc_math.py` — `Documentation math conventions OK (201 files scanned)`.
- `python3 tools/check_process_consistency.py` — `Process consistency: OK`.
- `python3 -m unittest discover -s tools/tests -t .` — 60 tests, all passed.
- `./tools/verify.sh docs` — exit 0; strict Zensical build, DocFX 465 models with 0 warnings and 0 errors,
  rustdoc workspace built.
- `./tools/verify.sh policy` — exit 0; ApiCompat found no breaking changes, contract artifacts current
  (26 files), space-group catalog current, 60 policy tests passed, SPDX/secret scan/`cargo-deny` clean.
- A scratch Avalonia 12.1.6 headless probe under `/home/node/tmp/r3probe` (not committed, `dotnet run -c
  Release`) for the alias delivery, the tree selectors, the HC merged dictionary, the HC focus pair, the
  `ProgressBar`, and the TextBox selection pair.
- Avalonia 12.1.6 source inspection from the extracted package XAML under `/home/node/tmp/fluent1216` and the
  NuGet `Avalonia.Themes.Fluent/12.1.6` package.

#### Probe evidence

- **Alias delivery (M-6).** A palette-shaped `Default`/`Light`/`Dark` dictionary defined the fixed base roles,
  the six `SystemAccentColor*` `Color` values, and the §3.4 alias keys; the app used a real `FluentTheme`,
  `CheckBox`, `Slider`, `TreeView`, and `ProgressBar`.
  - Light: `CheckBox.NormalRectangle.Background` `#007ACC`, `CheckGlyph.Fill` `#FFFFFF`,
    `Slider.Thumb.Background` `#007ACC`, `PART_DecreaseButton.Background` `#007ACC`; the palette (or target
    style) `PART_LayoutRoot.Background` is theme-scoped.
  - Live Light→Dark switch: the same parts kept the fixed fill; the slider thumb on pointer-over changed to
    the dark `Light1` `#3A9CDD` (the light `Light1` is `#0090F0`), proving the derived `Color` follows the
    active theme dictionary.
  - HC merged dictionary added after the palette: `NormalRectangle.Background`/`BorderBrush` `#FFD700`,
    `CheckGlyph.Fill` `#000000`, `Thumb.Background` `#FFD700`, `PART_DecreaseButton.Background` `#FFD700`;
    `TryGetResource("SystemAccentColor")`, `SystemAccentColorLight1`, and `SystemAccentColorDark1` returned
    `Color #FFD700`; the alias keys returned `SolidColorBrush #FFD700`. The HC dictionary did override the
    palette theme dictionary.
  - The `ProgressDialog`-shaped `ProgressBar` had `Foreground` `#007ACC` light, `#248DD3` dark, `#FFD700` HC,
    because Fluent's `ProgressBar` setter uses `SystemControlHighlightAccentBrush`.
- **Tree selectors (M-9).** With only the palette alias keys (no targeted style), selected
  `PART_LayoutRoot.Background` was `#CCE5FF` light / `#094771` dark and the header text inherited
  `#000000`/`#FFFFFF`; the palette `TreeViewItemForegroundSelected` override did not reach the header.
  - A direct `TreeViewItem:selected /template/ ContentPresenter#PART_HeaderPresenter` style set the realized
    `ContentPresenter.Foreground` and the generated `TextBlock.Foreground` (`#112233`, `#445566`).
  - A descendant `… /template/ Border#PART_LayoutRoot ContentPresenter#PART_HeaderPresenter` style also
    applied (`#112233`).
  - A child-combinator style of Fluent's shape,
    `TreeViewItem:selected /template/ Border#PART_LayoutRoot > ContentPresenter#PART_HeaderPresenter`, did
    **not** apply: the header foreground stayed the inherited `#000000`. The `PART_HeaderPresenter` visual
    parent is `Grid#PART_Header`, not `Border#PART_LayoutRoot`, so the `>` combinator cannot match.
  - A targeted style with `DynamicResource` `Mocassin.SelectedBackground`/`SelectedForeground` followed the
    HC merge (`PART_LayoutRoot` `#FFD700`, header `#000000`).
- **HC focus pair (M-10).** With only `FluentTheme`, `SystemControlFocusVisualPrimaryBrush` resolved to
  `#000000` (Light) / `#FFFFFF` (Dark) and `SystemControlFocusVisualSecondaryBrush` to `#FFFFFF` (Light) /
  `#000000` (Dark). Adding the HC merged dictionary without the pair left `HC + Light` primary `#000000` on
  the HC `#000000` surface and `HC + Dark` secondary `#000000` on black. `HighContrast.axaml` has no
  `SystemControlFocusVisual*` entries; the design's HC additions do not add them.
- **HC selected-tree chevron (M-11).** The Fluent `TreeViewItem` realizes `PART_ExpandCollapseChevron` as a
  `ToggleButton`; its `Path#ChevronPath` uses `Fill="{DynamicResource TreeViewItemForeground}"`.
  `TreeViewItemForeground` is `SystemControlForegroundBaseHighBrush`; with the HC selected fill `#FFD700` and
  `SystemControlForegroundBaseHighBrush` `#FFFFFF`, the chevron is white on gold. The targeted style changes
  only `PART_LayoutRoot.Background` and `PART_HeaderPresenter.Foreground`.
- **TextBox selection pair (M-12).** A real `TextBox` reported `SelectionForegroundBrush=null` in Light, Dark,
  and an HC-like state; the `TextPresenter` inherited the control `Foreground`. Fluent's TextBox sets
  `SelectionBrush` from `TextControlSelectionHighlightColor` but only template-binds
  `SelectionForegroundBrush`.
- **Palette `Color` type (m-10).** A brush written into `SystemAccentColorLight1` in a theme dictionary did
  not throw on the normal or hover path in my probe; the pointer-over fill used the brush's colour
  `#3A9CDD`. The six keys are still required to be `Color` by the test plan and HC design; the type
  assertion is the reliable oracle.

#### Recomputation

- Fixed floors: `#FFFFFF` on `#007ACC` 4.5106; `#248DD3` on `#3C3C3C` 3.0601, `#37373D` 3.2796, `#2D2D30`
  3.8085, `#252526` 4.2483, `#1E1E1E` 4.6247; `#007ACC` on `#F3F3F3` 4.0650 and `#FFFFFF` 4.5106; `#4DA6FF`
  on `#1E1E1E` 6.5208, `#252526` 5.9901, `#2D2D30` 5.3700; `#005A9E` on `#F3F3F3` 6.4020 and `#FFFFFF`
  7.1037; white on `#094771` 9.7613; `#1F1F1F` on `#CCE5FF` 12.7305.
- Derived states: white on `#3A9CDD` 3.0089 (passes 3:1 by 0.009), `#248CD1` 3.6541, `#248DD3` 3.6048,
  `#0090F0` 3.3613, `#005994` 7.3515; `#3A9CDD` on `#3C3C3C` 3.6662 and `#2D2D30` 4.5628; `#248CD1` on
  `#3C3C3C` 3.0188; `#0090F0` on `#F3F3F3` 3.0292.
- HC: black on `#FFD700` 14.9722; white on `#000000` 21.0000; `#FFD700` on `#000000` 14.9722.
- Records §8.1 cross-product errors: `#007ACC` on `#3C3C3C` is 2.446 and on `#37373D` 2.621 (below 3:1);
  dark `#3A9CDD` on `#F3F3F3` is 2.712. The contract §11.1 pairs dark values with `#3C3C3C` and light values
  with `#F3F3F3`, so the test must state the pairing (m-9).
- Failures: white on `#FFD700` 1.3460 (HC selected-tree chevron, and the HC TextBox selection foreground);
  `#D4D4D4` on `#007ACC` 3.03; `#333333` on `#007ACC` 2.82 (text selection pair vs the 4.5 floor).

#### Findings (round 3)

| Severity | Finding | Evidence |
| --- | --- | --- |
| Major | **M-9 — The TreeView targeted-selector is ambiguous and Fluent's own shape does not match.** §3.6 lists the `Border#PART_LayoutRoot` styles and “the corresponding `ContentPresenter#PART_HeaderPresenter` selectors”; the record §4 says “mirroring Fluent's selected/pointer-over/pressed selectors”. Fluent's selected-header selector is `… /template/ Border#PART_LayoutRoot > ContentPresenter#PART_HeaderPresenter`, but the ContentPresenter is a child of `Grid#PART_Header`, so the `>` combinator cannot match. A probe built that shape and the header foreground did not change; the descendant and direct `ContentPresenter#PART_HeaderPresenter` selectors did reach. An implementor copying Fluent's shape gets no selected-tree foreground; HC selected text then remains the captured base-high white on gold (1.35:1). | Contract §3.6 `:250-258`; record §4 `:94-97`; Avalonia 12.1.6 `Controls_TreeViewItem.xaml:88-96,140-155`; probe. |
| Major | **M-10 — HC does not override the neutral focus visual pair.** `SystemControlFocusVisualPrimaryBrush` is `SystemBaseHighColor` (Light `#000000`, Dark `#FFFFFF`); `SecondaryBrush` is `SystemAltMediumColor` (Light `#FFFFFF`, Dark `#000000`). The HC dictionary and the design's HC additions do not define either key, and they are not in §3.4. In HC + Light the 2 px primary is black on the HC `#000000` surface (1:1); in HC + Dark the 1 px secondary is black on black. The contract §6 accepts the neutral pair and requires 3:1, §11.1 lists the pair at 3:1, and acceptance criterion 7 claims the focus floors pass. | `BaseResources.xaml:102-104`; `ctl/AdornerLayer.xaml:9-17`; `HighContrast.axaml`; probe; contract §6 `:343-348`, §11.1 `:558`; record §10.7. |
| Major | **M-11 — The HC selected-tree chevron is white on gold.** Fluent's `Path#ChevronPath` uses `Fill="{DynamicResource TreeViewItemForeground}"`; `TreeViewItemForeground` is `SystemControlForegroundBaseHighBrush` (`#FFFFFF` in HC). The targeted TreeView style sets only `PART_LayoutRoot.Background` and `PART_HeaderPresenter.Foreground`, so the chevron remains `#FFFFFF` on `#FFD700` (1.35:1). The realized test plan reads only those two parts and would not catch it. | `Controls_TreeViewItem.xaml:38-43`; probe; recomputation; contract §11.1; record §10.5. |
| Major | **M-12 — The TextBox selection pair is not implemented.** Fluent sets `SelectionBrush` from `TextControlSelectionHighlightColor` but only template-binds `SelectionForegroundBrush`; the app never sets `SelectionForegroundBrush`, so the selected text uses the control foreground: `#D4D4D4` on `#007ACC` 3.03 dark, `#333333` on `#007ACC` 2.82 light, `#FFFFFF` on `#FFD700` 1.35 HC. Contract §11.1 claims white on `#007ACC` at 4.5 for both themes. The design's §3.5 “text on an accent fill `#000000`” is not applied to the HC selection. Pre-existing, but the design's §11.1 row and acceptance evidence claim it; fix or record an explicit owner deferral. | `Controls_TextBox.xaml:101,177-178`; probe; `TextControlForeground`/`TextControlSelectionHighlightColor` palette and HC; recomputation; contract §11.1 `:555`. |
| Minor | **m-7 — The shipped `ProgressDialog` progress bar is on the dark focus tint, and the inventory/test plan omit it.** `ProgressDialogView.axaml:11` uses the default `ProgressBar`; Fluent sets `Foreground` from `SystemControlHighlightAccentBrush`, which the design maps to `Mocassin.FocusBrush` (`#248DD3` dark). The probe read `#007ACC` light, `#248DD3` dark, `#FFD700` HC. The record §2 shipped-control table and §8.2 realized-part list omit the ProgressBar. | `ProgressDialogView.axaml:11`; `ctl/ProgressBar.xaml:29`; probe; record §2/§8.2. |
| Minor | **m-8 — `Mocassin.FocusBrush` has no view consumer; the splitter adorner and tab pipe use `AccentBrush`.** The three splitter `FocusAdorner` templates use `Mocassin.AccentBrush` (`ShellView.axaml:49-55,74-80,96-102`); `TabItemHeaderSelectedPipeFill` is `#007ACC` in all palette dictionaries (and `#FFD700` HC), not the dark `FocusBrush` `#248DD3`. Contract §3.2/§6/§7.4 say the focus ring and selected-pipe use `FocusBrush`. The values still pass 3:1 (`#007ACC` on `#1E1E1E` 3.696; on `#2D2D30` 3.044), so this is a role/implementation-shape inconsistency, not a contrast failure. | `ShellView.axaml`; `Palette.axaml:150,312,474`; no `Mocassin.FocusBrush` consumer in `src/dotnet`; contract §3.2/§6/§7.4. |
| Minor | **m-9 — The record's focus/derived 3:1 sentence is an unsatisfiable cross-product.** §8.1 says focus `#248DD3`/`#007ACC` and the derived states must be 3:1 on `#3C3C3C`, `#37373D`, `#2D2D30`, `#252526`, `#1E1E1E`, `#F3F3F3`, `#FFFFFF`; `#007ACC` on `#3C3C3C` is 2.446 and on `#37373D` 2.621, and dark `#3A9CDD` on `#F3F3F3` is 2.712. The contract §11.1 pairs dark values with the dark worst surface and light values with the light worst surface; state that pairing in §8.1. | Record §8.1; contract §11.1; recomputation. |
| Minor | **m-10 — The palette `SystemAccentColor*` type is not stated in the normative table.** §3.6/§3.5 specify `Color` for HC; §3.4 and the §3.6 value table do not state the palette type, while the palette pattern is all `SolidColorBrush`. Record §7 says “derived `Color` keys” and §8.1 asserts it, but the table should say `Color` for `Default`/`Light`/`Dark` too. The type assertion is the test oracle; my brush-in-key probe did not reproduce the “brush would throw” claim on the normal/hover path. | Contract §3.4 `:155`, §3.6 `:229-231`; record §5 `:146-147`, §7, §8.1; probe. |
| Minor | **m-11 — HC does not override the Fluent unchecked-pressed/disabled sub-states.** `CheckBoxCheckBackgroundFillUncheckedPressed` uses `SystemControlBackgroundBaseMediumLowBrush`; the checked-disabled fill uses `SystemControlBackgroundBaseLowBrush`; the checked/indeterminate-disabled glyph uses `SystemControlForegroundChromeWhiteBrush`. The HC dictionary does not override the two base fills, so those sub-states remain stock Fluent (a 40 %/20 % base fill, not `#FFD700`/`#000000`). The §3.5 “hover and pressed fill `#FFD700`” / “disabled” rules are not delivered for those states, and the realized tests do not render them. | `FluentControlResources.xaml:353-385`; design §3.4/§3.5; `HighContrast.axaml`; record §8.2. |
| Minor | **m-12 — Removing the Accent control changes the Settings Tab order/automation, but §13 says “Tab order … unchanged”.** `SettingsDialogView.axaml:25-27` is a focus stop and an `Accent` automation name; removing it changes the Settings Tab order. The maintenance record §13 says “Tab order … unchanged”; the visual contract §11.2 says a visual change must not remove a focus target. The test plan §8.3 asserts “no Accent control” but not the new order. Record the intentional accessibility delta and update the deterministic Tab-order/accessibility test; the interaction-contract rules themselves need not change. | `SettingsDialogView.axaml:25-27`; record §13 `:282`; contract §11.2 `:570`; `testing-and-parity.md` accessibility sweep. |
| Note | **n-4 — The round-2 resolution claims are only partly reflected in the frozen docs.** The n-2 resolution says `82d3422` is in the maintenance commit list; §15 does not list it and still has `(round 3 request commit)`. The m-6 resolution says `TableViewRow*`/`DateTimePickerShared.xaml:63` were added to the `AltBaseHigh` consumer list; the current contract/record has no such list and does not name those consumers (`TableView` is marked unshipped). Record accuracy only. | Maintenance record §15 `:312-326`; record §4 `:463-466`. |
| Note | **n-5 — Two documentation rows remain stale.** `docs/development/testing-and-parity.md:186` still lists `OnAccentBrush` as a runtime resource; `docs/development/ui-visual-contract.md:37` still says the runtime settings own “accent”. The design §9 says both pages / §2 are updated. | `testing-and-parity.md:186`; contract `:37`; record §9. |
| Note | **n-6 — The applier cleanup is not listed.** The fixed family removes the per-accent path, leaving `AccentColor`, `OnAccent`, `Contrast`, `RelativeLuminance`, `Linear`, and the `lightOnAccent`/`darkOnAccent` constants unused in `AvaloniaThemeApplier.cs:21-24,166-195`; record §7 says remove the `AccentPalette` path but not these helpers. Remove them with the accent tests. | `AvaloniaThemeApplier.cs`; record §7. |

#### Verdict (round 3)

**Changes required.** The fixed-family values pass the recomputed floors, and the fixed-family precedence
(M-7), the live theme switch (M-8), and the CheckBox/Slider/tree-background alias delivery (M-6) are
established. Four Majors remain: the TreeView targeted-selector ambiguity (M-9), the HC neutral focus pair
(M-10), the HC selected-tree chevron (M-11), and the unimplemented TextBox selection pair (M-12). The design
must be corrected and re-frozen; no implementation code exists on the branch.

Follow-up owner: Planner (design corrections); the implementor starts only after a fresh re-review passes.
Follow-up step: — (out-of-step maintenance fix; no step tag).

---

### Re-review round 4

#### Independence and frozen state

I am a separate fresh context. I did not author, plan, implement, or review rounds 1–3, and I have no shared
conversation with rounds 1–3, the planner, the implementor, or the orchestrator. I read the repository at the
frozen delivery-detail design `f1a8c1a7d1ee704b121964a8c7d030773488d859` and worked only in
`docs/planning/reviews/step-gui-accent-propagation.md`; I did not edit the visual contract, the maintenance
record, code, tests, configuration, or any other record.

- Frozen design: `f1a8c1a7d1ee704b121964a8c7d030773488d859`.
- Branch head / round-4 request: `e8983eac4b22d5cb9b5b54be0ee30ca0ebef31c6`.
- `git diff --name-status f1a8c1a..e8983ea` reported only
  `M docs/planning/reviews/step-gui-accent-propagation.md`; `git diff --stat` is 78 insertions and 2 deletions.
  The diff changes the target metadata, the record state, the resolution-status rows, the round-4 resolutions,
  and the round-4 request; earlier rounds are unchanged.
- Base `main` is `19cb6ffe190e02449ce8b827706f365f2ea7afe2`; `git merge-base --is-ancestor main HEAD` exited 0.
  The branch has no code, test, configuration, or contract change; it is docs-only.

#### Resolution verification (round 4)

| Finding | Round-4 status | Evidence |
| --- | --- | --- |
| M-9 | **Resolved.** The contract names the five exact selectors and explicitly rejects the Fluent `Border#PART_LayoutRoot > ContentPresenter#PART_HeaderPresenter` shape. A probe with a direct selector set the realized `PART_HeaderPresenter.Foreground` in Light, Dark, and HC; the child-combinator style did not apply (the header stayed at the direct value), and the presenter's visual parent is `Grid#PART_Header`. The test plan reads the realized presenter foreground. | Contract §3.6 `:261-275`; `Controls_TreeViewItem.xaml:73-98,138-155`; probe; record §8.2. |
| M-10 | **Resolved.** `HighContrast.axaml` is specified to define `SystemControlFocusVisualPrimaryBrush` `#FFD700` and `SystemControlFocusVisualSecondaryBrush` `#000000`; both are in §3.4. A probe with the HC dictionary rendered the focus adorner as a 2 px `#FFD700` outer border and a 1 px `#000000` inner border (14.97:1 and 14.97:1). The test plan renders the realized HC adorner. | Contract §3.4 `:155`, §3.5 `:204-205`, §3.6 `:282-283`; `ctl/AdornerLayer.xaml`; record §8.2; probe. |
| M-11 | **Partially resolved / new Major.** The selected chevron fill is set from `Mocassin.SelectedForeground` and the realized HC chevron is `#000000` on `#FFD700` (14.97:1), so the original white-on-gold defect is fixed. The new m-11 checkbox-label override is wrong: `CheckBoxForegroundUncheckedPressed` `#000000` is applied to the `PART_ContentPresenter`, which sits on the transparent row/card, not on the gold 20 px box; in HC the label is black on the black `#000000` card (1:1). | Contract §3.5 `:207`; `Controls_CheckBox.xaml:43-76,115-126`; `Accents_FluentControlResources.xaml:285,299`; probe; record §8.2; finding M-13. |
| M-12 | **Resolved.** `ShellStyles.axaml` sets `TextBox.SelectionBrush`/`SelectionForegroundBrush` from `Mocassin.AccentBrush`/`OnAccentBrush`; a real `TextBox` realized `#007ACC`/`#FFFFFF` in Light and Dark and `#FFD700`/`#000000` in HC (4.51:1 and 14.97:1). The test reads both properties. | Contract §3.6 `:233`; record §4/§8.2; probe. |
| m-7 | **Resolved.** The `ProgressDialog.ProgressBar` is in the shipped-control inventory and the realized-part list; a `ProgressBar` probe read `#007ACC` Light, `#248DD3` Dark, `#FFD700` HC. | Record §2/§8.2; `ProgressDialogView.axaml:11`; `ctl/ProgressBar.xaml:29`; probe. |
| m-8 | **Design resolved; test coverage note.** The splitter `FocusAdorner` borders and `TabItemHeaderSelectedPipeFill` are specified to use `Mocassin.FocusBrush`. The tab pipe is in the resource test; the §8.2 realized list does not include the splitter adorner, so the "role is no longer unused" claim is source-level only. | Contract §3.6 `:239-241`; record §7/§8.1-§8.2; `ShellView.axaml:49-55,74-80,96-102`; finding n-9. |
| m-9 | **Resolved.** Record §8.1 now pairs the dark focus/derived values with `#3C3C3C`/`#37373D`/`#2D2D30`/`#252526`/`#1E1E1E` and the light values with `#F3F3F3`/`#FFFFFF`; the contract §11.1 row is paired. Recomputation confirms the cross-products would fail (`#007ACC` on `#3C3C3C` 2.446, `#3A9CDD` on `#F3F3F3` 2.712), so the pairing is necessary. | Record §8.1; contract §11.1 `:580`; recomputation. |
| m-10 | **Resolved.** §3.4 and §3.6 state that the six `SystemAccentColor*` keys are `Color` in `Default`/`Light`/`Dark` and HC; §8.1 tests the type. | Contract §3.4 `:169-171`, §3.6 `:235-237`; record §8.1. |
| m-11 | **Partially resolved.** HC fills/glyphs for unchecked-pressed and checked/indeterminate-disabled are specified and the probe reads `#FFD700`/`#000000` for the box and `#A0A0A0` for the disabled glyph, but the unchecked-pressed label is black on black (M-13) and the disabled label aliases are not overridden (m-14). | Contract §3.5 `:207`; record §4/§8.2; probe; findings M-13/m-14. |
| m-12 | **Resolved.** The removal is documented as an intentional accessibility delta in record §5/§13 and contract §11.2; §8.3 tests the new deterministic order and the absent `Accent` automation name; the interaction contract does not prohibit the removal and needs no text change. | Record §5/§8.3/§13; contract §11.2 `:592-595`; `SettingsDialogView.axaml:25-27`; interaction contract §6.5/§7. |
| n-4 | **Resolved for the commit/consumer list.** §15 lists `82d3422`, `73a297f`, `7a8f5bc`, `7ba3600`, and `cc06a67`; §2 names the `TableViewRow*`/`DateTimePickerShared.xaml:63` consumers and the unshipped `CalendarButton` `Dark2` consumer. The record status row is stale (n-7). | Maintenance record §2/§15. |
| n-5 | **Resolved.** `testing-and-parity.md` now says the accent/selection roles including `OnAccentBrush` come from the palette; the contract §2 runtime-settings row says "fixed `SystemAccentColor`", not accent. | `testing-and-parity.md:186-187`; contract `:37`. |
| n-6 | **Resolved.** Record §7 lists `AccentColor`, `OnAccent`, `Contrast`, `RelativeLuminance`, `Linear`, and the `lightOnAccent`/`darkOnAccent` constants for removal; they are the current `AvaloniaThemeApplier.cs:21-24,166-195` helpers. | Maintenance record §7; `AvaloniaThemeApplier.cs`. |

#### Commands and results (round 4)

- `git rev-parse HEAD` — `e8983eac4b22d5cb9b5b54be0ee30ca0ebef31c6`.
- `git rev-parse f1a8c1a7d1ee704b121964a8c7d030773488d859` — the frozen delivery-detail design.
- `git rev-parse main` — `19cb6ffe190e02449ce8b827706f365f2ea7afe2`; `git merge-base --is-ancestor main HEAD` — exit 0.
- `git diff --name-status f1a8c1a..e8983ea` — only
  `M docs/planning/reviews/step-gui-accent-propagation.md`; `git diff --stat` — 78 insertions, 2 deletions.
- `git diff --check` — clean; `git status --short` — clean before the record edit.
- `python3 tools/check_docs_links.py` — `Documentation links OK (204 files scanned)`.
- `python3 tools/check_doc_math.py` — `Documentation math conventions OK (201 files scanned)`.
- `python3 tools/check_process_consistency.py` — `Process consistency: OK`.
- `python3 -m unittest discover -s tools/tests -t .` — 60 tests, all passed.
- `./tools/verify.sh docs` — exit 0; strict Zensical build, DocFX 465 models with 0 warnings/0 errors, rustdoc workspace built.
- `./tools/verify.sh policy` — exit 0; ApiCompat no breaking changes, contract artifacts current (26 files), space-group catalog current, 60 policy tests passed, SPDX/secret scan/`cargo-deny` clean.
- A scratch Avalonia 12.1.6 headless probe under `/home/node/tmp/r4probe` (not committed, `dotnet run -c Release`)
  for the tree selectors, chevron, TextBox pair, HC focus pair/adorners, HC CheckBox sub-states, ProgressBar, and
  a Light→Dark switch.
- Avalonia 12.1.6 source inspection from the extracted Fluent package under `/home/node/tmp/fluent1216`
  (`Controls_TreeViewItem.xaml`, `Controls_CheckBox.xaml`, `ctl/AdornerLayer.xaml`,
  `ctl/ProgressBar.xaml`, `Accents_FluentControlResources.xaml`, `BaseResources.xaml`).

#### Probe evidence

- **M-9 tree selectors.** A real `TreeView`/`TreeViewItem` with a child rendered in Light, Dark, and HC. The
  direct `TreeViewItem:selected /template/ ContentPresenter#PART_HeaderPresenter` style set the realized
  `ContentPresenter.Foreground` and the generated `TextBlock.Foreground`; the child-combinator style
  `TreeViewItem:selected /template/ Border#PART_LayoutRoot > ContentPresenter#PART_HeaderPresenter` did not
  apply (the realized header stayed at the direct value). The presenter's visual parent was
  `Grid#PART_Header`. The row style set `PART_LayoutRoot.Background` to `#CCE5FF`/`#094771`/`#FFD700`, and the
  chevron selector set `Path#ChevronPath.Fill` to `#1F1F1F`/`#FFFFFF`/`#000000`.
- **M-11 chevron.** The realized `Path#ChevronPath` in HC was `#000000` and visible on the `#FFD700` selected
  row; 14.97:1. The Light/Dark values were `#1F1F1F`/`#FFFFFF`.
- **M-12 TextBox pair.** A real `TextBox` with `SelectAll()` realized `SelectionBrush`/`SelectionForegroundBrush`
  `#007ACC`/`#FFFFFF` in Light and Dark and `#FFD700`/`#000000` in HC; the `TextPresenter` carried the same
  pair.
- **M-10 HC focus pair.** The HC dictionary resolved `SystemControlFocusVisualPrimaryBrush` `#FFD700` and
  `SystemControlFocusVisualSecondaryBrush` `#000000`; a focused `Button` produced a focus adorner with a 2 px
  `#FFD700` outer border and a 1 px `#000000` inner border.
- **m-7 ProgressBar.** A dialog-shaped `ProgressBar` read `Foreground` `#007ACC` Light, `#248DD3` Dark, and
  `#FFD700` HC.
- **m-11/M-13 CheckBox.** With the design's HC keys, a checked-disabled `CheckBox` realized
  `NormalRectangle.Background` `#000000` and `CheckGlyph.Fill` `#A0A0A0`; an unchecked pressed `CheckBox`
  realized `NormalRectangle.Background` `#FFD700`, `PART_ContentPresenter.Foreground` `#000000`, and
  `PART_Border.Background` transparent over the `#000000` card. The label is therefore `#000000` on
  `#000000` (1:1) in both HC Light and HC Dark. The disabled label was `#000000` in HC Light (1:1 on black)
  and `#FFFFFF` in HC Dark; adding `SystemControlDisabledBaseMediumLowBrush` `#000000` to the HC dictionary
  did not change the captured `CheckBoxForegroundCheckedDisabled` alias.
- **Theme switch.** A Light→Dark switch updated the realized `PART_LayoutRoot.Background` `#CCE5FF`→`#094771`,
  the header foreground `#1F1F1F`→`#FFFFFF`, and the chevron `#1F1F1F`→`#FFFFFF` through `DynamicResource`;
  the TextBox selection pair stayed `#007ACC`/`#FFFFFF`. This matches the design's theme-switch claim.

#### Recomputation

- Fixed floors: `#FFFFFF` on `#007ACC` 4.5106; `#000000` on `#FFD700` 14.9722; white on `#FFD700` 1.4026;
  `#A0A0A0` on `#000000` 8.0307; `#000000` on `#000000` 1.0000.
- Focus/derived dark: `#248DD3` on `#3C3C3C` 3.0601, `#37373D` 3.2796, `#2D2D30` 3.8085, `#252526` 4.2483,
  `#1E1E1E` 4.6247; `#3A9CDD` on `#3C3C3C` 3.6662 and `#2D2D30` 4.5628; `#248CD1` on `#3C3C3C` 3.0188.
- Light: `#007ACC` on `#F3F3F3` 4.0650 and `#FFFFFF` 4.5106; `#0090F0` on `#F3F3F3` 3.0292; `#1F1F1F` on
  `#CCE5FF` 12.7305; `#FFFFFF` on `#094771` 9.7613; `#4DA6FF` on `#2D2D30` 5.3700 and `#1E1E1E` 6.5208;
  `#005A9E` on `#F3F3F3` 6.4020 and `#FFFFFF` 7.1037.
- On-accent glyphs: white on `#3A9CDD` 3.0089, on `#248CD1` 3.6541, on `#0090F0` 3.3613, on `#005994` 7.3515.
- Cross-product checks: `#007ACC` on `#3C3C3C` 2.4456 and `#3A9CDD` on `#F3F3F3` 2.7116 are below 3:1, so the
  paired test surfaces in record §8.1 are required.
- HC: white on `#000000` 21.0000; gold on black 14.9722; the M-13 label is 1.0000.

#### Findings (round 4)

| Severity | Finding | Evidence |
| --- | --- | --- |
| Major | **M-13 — The HC unchecked-pressed CheckBox label is black on the black HC surface.** The design overrides `CheckBoxForegroundUncheckedPressed` to `#000000` on the assumption that the pressed fill is the label background. The Fluent CheckBox template puts the label in `PART_ContentPresenter` outside the 20 px `NormalRectangle`, and `CheckBoxBackgroundUncheckedPressed` is `SystemControlTransparentBrush`; only the box is `#FFD700`. On the HC `#000000` card the label is `#000000` on `#000000` (1:1) in both HC Light and HC Dark. A press on an unchecked shipped CheckBox (Settings/Problems) is a reachable interaction. The §8.2 realized test reads `NormalRectangle`/`CheckGlyph` and does not read the content foreground, so it would pass. | Contract §3.5 `:207`; `Controls_CheckBox.xaml:43-76,115-126`; `Accents_FluentControlResources.xaml:285,299`; probe; record §8.2; recomputation. |
| Minor | **m-14 — HC disabled CheckBox label foreground is not overridden.** `CheckBoxForegroundCheckedDisabled`/`UncheckedDisabled`/`IndeterminateDisabled` are `StaticResource` aliases to `SystemControlDisabledBaseMediumLowBrush`; §3.4 lists the disabled fills/glyphs but not these label aliases. In HC Light the label is `#000000` on the `#000000` card (1:1); in HC Dark it is `#FFFFFF` (21:1). Adding the base key to HC did not change the captured alias. The §8.2 disabled case reads the fill/glyph, not the label; no current shipped surface disables a CheckBox, so this is inventory/test accuracy rather than a live shipped regression. | `Accents_FluentControlResources.xaml:286,290,295`; contract §3.4 `:154-162`, §3.5 `:207`; probe. |
| Minor | **m-15 — Two documentation pages still instruct the user to change the removed accent.** `docs/getting-started/quickstart.md:57` and `docs/development/platform-checklist.md:95` still say "change theme, accent, density…"; the record §9 documentation plan lists the visual contract, `settings.md`, and `testing-and-parity.md` only. | `quickstart.md:57`; `platform-checklist.md:95`; maintenance record §9. |
| Note | **n-7 — The maintenance record status row is stale.** §Status says "Design frozen for round 3 review. No implementation yet." while the round-4 design is frozen at `f1a8c1a`. The §15 commit list still has the `(round 4 request commit)` placeholder (expected at freeze) and lists only `cc06a67` of the round-4 commits. | Maintenance record `:15`, §15. |
| Note | **n-8 — The neutral focus pair is listed as a palette key but its Light/Dark values are not specified.** §3.4 says the palette overrides the keys in the table, including `SystemControlFocusVisualPrimaryBrush`/`SecondaryBrush`, but the design accepts the stock neutral pair in Light/Dark and only specifies the HC values; the current `Palette.axaml` does not define the pair. A test that requires every §3.4 key to resolve from the palette in Light/Dark would fall back to Fluent (which is the intended neutral pair). State the stock values or mark the pair HC-only. | Contract §3.4 `:149-172`, §3.5 `:204-205`; `Palette.axaml`; probe. |
| Note | **n-9 — The splitter `FocusBrush` consumer is not in the realized test list.** The design says the three splitter `FocusAdorner` borders use `Mocassin.FocusBrush` (acceptance criterion 9), but §8.1 resolves the `FocusBrush` resource and the tab pipe only, and §8.2's realized list omits the splitter adorner. The m-8 role claim is therefore source-level, not realized-test-covered. | Record §8.1-§8.2, acceptance criterion 9; `ShellView.axaml:49-55,74-80,96-102`; contract §3.6 `:239-241`. |

#### Verdict (round 4)

**Changes required.** M-9, M-10, and M-12 are resolved; m-7, m-9, m-10, m-12, n-4, n-5, and n-6 are resolved;
m-8 is design-resolved with a test-coverage note. One new Major remains: the HC unchecked-pressed CheckBox label
is black on the black HC surface (M-13). Two Minors (m-14, m-15) and three Notes (n-7, n-8, n-9) also remain.
The design must be corrected and re-frozen; no implementation code exists on the branch.

Follow-up owner: Planner (design corrections); the implementor starts only after a fresh re-review passes.
Follow-up step: — (out-of-step maintenance fix; no step tag).

---

### Re-review round 5

#### Independence and frozen state

I am a separate fresh context. I did not author, plan, implement, or review rounds 1–4, and I have no shared
conversation with rounds 1–4, the planner, the implementor, or the orchestrator. I read the repository at the
frozen HC-label delivery design `5b2956b26e5cd4047edbbce52ecc191f01064916` and worked only in
`docs/planning/reviews/step-gui-accent-propagation.md`; I did not edit the visual contract, the maintenance
record, code, tests, configuration, or any other record.

- Frozen design: `5b2956b26e5cd4047edbbce52ecc191f01064916`.
- Branch head / round-5 request: `37cbce4bc3d0e93fd19034e8ceb8f0879097d1be`.
- `git diff --name-status 5b2956b..37cbce4` reported only
  `M docs/planning/reviews/step-gui-accent-propagation.md`; `git diff --stat` is 37 insertions and 2 deletions.
  The diff changes the target metadata, the record state, the resolution-status rows, the round-5 resolutions,
  and the round-5 request; earlier rounds are unchanged.
- The design delta since round 4 is `27dceb0` (contract §3.4/§3.5/§3.6/§11.1/§13, quickstart, platform
  checklist, maintenance record) plus `5b2956b` (the commit-list entry); `657da5b` is the round-4 record.
  `git diff --name-only main...HEAD` lists eight docs files and no code, test, configuration, or contract file.
  The branch is docs-only at the freeze.
- Base `main` is `19cb6ffe190e02449ce8b827706f365f2ea7afe2`; `git merge-base --is-ancestor main HEAD` exited 0.

#### Resolution verification (round 5)

| Finding | Round-5 status | Evidence |
| --- | --- | --- |
| M-13 | **Resolved.** The HC unchecked-pressed **box** is `#FFD700`; the realized label (`PART_ContentPresenter` and its `TextBlock`) is `#FFFFFF` on the black card in both HC Light and HC Dark, 21:1. The record now reads the realized label, not only `NormalRectangle`/`CheckGlyph`. | Contract §3.5 `:207-210`; record §8.2 `:232-233`, criterion 18 `:308-309`; Fluent `Controls_CheckBox.xaml:99-102`; probe; recomputation. |
| m-14 | **Resolved.** The three HC disabled label aliases are `#A0A0A0`; the checked/indeterminate disabled fills realize `#000000` and the glyphs `#A0A0A0` (8.03:1 on black). The unchecked disabled fill is Fluent-transparent over the black card, with an `#A0A0A0` label. No shipped surface disables a CheckBox; the future-control risk is recorded in §11 and the record prose. | Contract §3.5 `:208-210`; record `:125-132`, §8.2 `:232-233`, §11 `:330`; Fluent `Controls_CheckBox.xaml:119-138,200-219,282-301`, `Accents_FluentControlResources.xaml:286,290,295`; probe; recomputation. |
| m-15 | **Resolved.** `quickstart.md:57` and `platform-checklist.md:95` no longer mention the removed accent setting. A repository-wide search outside `docs/planning/` finds only pages that describe the fixed family (contract, testing-and-parity, user settings) or the legacy setting no-op (`file-layout.md`); no user instruction remains. | `git show 27dceb0 -- docs/getting-started/quickstart.md docs/development/platform-checklist.md`; `grep -rn -i accent docs --include='*.md'` outside planning. |
| n-7 | **Resolved.** The maintenance record status row says "Design frozen for round 5 review. No implementation yet."; the §15 commit list records `27dceb0`, the round-4 commits, and the expected round-5 request placeholder. | Maintenance record `:15`, §15 `:413-420`. |
| n-8 | **Resolved for the values and the realized ring.** The palette specifies Light `#000000`/`#FFFFFF`, Dark `#FFFFFF`/`#000000`, HC `#FFD700`/`#000000`; a focused real control realizes a 2 px primary outer border and a 1 px secondary inner border in Light (`#000000`/`#FFFFFF`), Dark (`#FFFFFF`/`#000000`), and HC (`#FFD700`/`#000000`). The secondary meets 3:1 only against the primary, not against the light/dark surface (m-16). | Contract §3.6 `:232`, §11.1 `:585`; record §7 `:198`, §8.1 `:224`; `ctl/AdornerLayer.xaml:11-15`; probe. |
| n-9 | **Resolved.** The three splitter `FocusAdorner` borders use `Mocassin.FocusBrush` and are in the §8.2 realized list with the selected-tab pipe; criterion 19 states the same. | `ShellView.axaml:49-55,74-80,96-102`; record §8.2 `:238`, criterion 19 `:310-311`. |

#### Commands and results (round 5)

- `git rev-parse HEAD` — `37cbce4bc3d0e93fd19034e8ceb8f0879097d1be`.
- `git rev-parse 5b2956b26e5cd4047edbbce52ecc191f01064916` — the frozen HC-label design.
- `git rev-parse main` — `19cb6ffe190e02449ce8b827706f365f2ea7afe2`; `git merge-base --is-ancestor main HEAD` —
  exit 0.
- `git diff --name-status 5b2956b..37cbce4` — only
  `M docs/planning/reviews/step-gui-accent-propagation.md`; `git diff --stat` — 37 insertions, 2 deletions.
- `git diff --name-only main...HEAD` — `docs/development/platform-checklist.md`,
  `docs/development/testing-and-parity.md`, `docs/development/ui-visual-contract.md`,
  `docs/getting-started/quickstart.md`, the maintenance record, this record, `docs/reference/file-layout.md`,
  `docs/user-guide/settings.md`; no source, test, configuration, contract, or generated file.
- `git diff --check` — clean; `git status --short` — clean before the record edit.
- `python3 tools/check_docs_links.py` — `Documentation links OK (204 files scanned)`.
- `python3 tools/check_doc_math.py` — `Documentation math conventions OK (201 files scanned)`.
- `python3 tools/check_process_consistency.py` — `Process consistency: OK`.
- `python3 -m unittest discover -s tools/tests -t .` — 60 tests, all passed.
- `./tools/verify.sh docs` — exit 0; strict Zensical build, DocFX 465 models with 0 warnings and 0 errors,
  rustdoc workspace built.
- `./tools/verify.sh policy` — exit 0; contract artifacts current (26 files), process consistency OK, 60 policy
  tests passed, SPDX headers present in 1105 source files, no secret patterns in 1623 tracked text files,
  `cargo-deny` advisories/bans/licenses/sources OK.
- A scratch Avalonia 12.1.6 headless probe under `/home/node/tmp/r5probe` (not committed,
  `dotnet run -c Release -- light|dark|hclight|hcdark`) for the HC CheckBox label/sub-states, the disabled
  aliases, and the realized focus ring.
- Avalonia 12.1.6 source inspection from the extracted Fluent package under `/home/node/tmp/fluent1216`
  (`Controls_CheckBox.xaml`, `Accents_FluentControlResources.xaml`, `ctl/AdornerLayer.xaml`,
  `BaseColorsPalette.xaml`).

#### Probe evidence

The probe used a real `FluentTheme`, palette-shaped `Light`/`Dark` theme dictionaries, and the design's HC
merged dictionary; a black window stood in for the HC card.

- **HC Light and HC Dark (M-13/m-14).**
  `cb.pressed.fill=#FFFFD700 glyph=#FFFFFFFF label=#FFFFFFFF textBlock=#FFFFFFFF rowBg=#00FFFFFF`;
  `cb.uncheckedDisabled.fill=#00FFFFFF glyph=#FFFFFFFF label=#FFA0A0A0 textBlock=#FFA0A0A0`;
  `cb.checkedDisabled.fill=#FF000000 glyph=#FFA0A0A0 label=#FFA0A0A0 textBlock=#FFA0A0A0`;
  `cb.indeterminateDisabled.fill=#FF000000 glyph=#FFA0A0A0 label=#FFA0A0A0 textBlock=#FFA0A0A0`.
  The 20 px box is gold while the label sits on the transparent row over the black card, so the label is white,
  not black. `CheckBoxForegroundUncheckedPressed`/`CheckedDisabled`/`IndeterminateDisabled` resolved to
  `#FFFFFFFF`/`#FFA0A0A0`/`#FFA0A0A0`; the same values appeared in HC Light and HC Dark.
- **HC focus ring (n-8).** The HC dictionary resolved `SystemControlFocusVisualPrimaryBrush` `#FFFFD700` and
  `SystemControlFocusVisualSecondaryBrush` `#FF000000`; a focused real `Button` produced a 2 px `#FFD700` outer
  border and a 1 px `#000000` inner border.
- **Light/Dark focus ring and palette values (n-8).** With the explicit `Light`/`Dark` variant the palette
  resolved the pair `#FF000000`/`#FFFFFFFF` and `#FFFFFFFF`/`#FF000000`; the realized adorner was 2 px
  `#000000` + 1 px `#FFFFFF` (Light) and 2 px `#FFFFFF` + 1 px `#000000` (Dark). A variant-less
  `Application.TryFindResource` call returned Fluent's stock `#99FFFFFF`/`#99000000` secondary, which is why the
  realized adorner, not a variant-less resource lookup, is the correct test oracle.
- **Light/Dark CheckBox fallbacks (n-10).** Without palette overrides the pressed fill/label were
  `#66000000`/`#FF000000` (Light) and `#66FFFFFF`/`#FFFFFFFF` (Dark); the disabled labels were `#66000000`
  (Light) and `#66FFFFFF` (Dark). These are Fluent's captured base values; the design gives no non-HC alias
  values for the four foreground sub-states.
- **Template shape.** `CheckBox.xaml:99-102` binds the pressed `PART_ContentPresenter.Foreground` to
  `CheckBoxForegroundUncheckedPressed`; lines `:119-123`, `:200-204`, and `:282-286` bind the disabled labels to
  the three disabled aliases. `Accents_FluentControlResources.xaml:285-295` defines those aliases and
  `:345` maps the unchecked pressed fill to `SystemControlBackgroundBaseMediumLowBrush` (the design overrides it
  to `#FFD700` in HC). `ctl/AdornerLayer.xaml:11-15` draws the 2 px primary outer and 1 px secondary inner
  border.

#### Recomputation

- HC label and disabled values: `#FFFFFF` on `#000000` 21.0000; `#A0A0A0` on `#000000` 8.0307; `#000000` on
  `#FFD700` 14.9722; `#FFD700` on `#000000` 14.9722.
- Neutral focus ring: Light primary `#000000` on `#F3F3F3` 18.9254 and `#FFFFFF` 21.0000; Light secondary
  `#FFFFFF` on the primary `#000000` 21.0000 (against `#FFFFFF`/`#F3F3F3` it is 1.0000/1.1096). Dark primary
  `#FFFFFF` on `#1E1E1E` 16.6712, `#252526` 15.3142, `#2D2D30` 13.7290, `#3C3C3C` 11.0310, `#37373D` 11.8223;
  Dark secondary `#000000` on the primary `#FFFFFF` 21.0000 (against the dark surfaces it is 1.2597–1.9037,
  below 3:1). HC primary `#FFD700` on `#000000` 14.9722; HC secondary `#000000` on `#FFD700` 14.9722.
- The `#FFFFFF`/`#000000` Light pair and the `#FFFFFF`/`#000000` Dark pair pass 3:1 only when the secondary is
  paired with the primary; this is the m-16 point.

#### Findings (round 5)

| Severity | Finding | Evidence |
| --- | --- | --- |
| Minor | **m-16 — The neutral focus-ring test basis is not stated.** The contract §11.1 row and record §8.1 say the pair is "against adjacent surfaces"/"rendered at 3:1", but the inner 1 px secondary is adjacent to the 2 px primary ring, not to the control surface. The Light secondary `#FFFFFF` is 1.0000 against `#FFFFFF` and 1.1096 against `#F3F3F3`; the Dark secondary `#000000` is 1.2597–1.9037 against the dark surfaces. Both pass 21.0000 against their primary, which is the actual geometry. An implementor who asserts each brush against the surface will fail the test at 1:1; the design should state the layer pairing (primary vs surface, secondary vs primary) in §8.1/§11.1. | Contract §11.1 `:585`; record §8.1 `:224`; `ctl/AdornerLayer.xaml:11-15`; probe; recomputation. |
| Note | **n-10 — The four check-box label sub-state aliases are listed as palette keys, but only their HC values are specified.** §3.4 `:162` adds `CheckBoxForegroundUncheckedPressed`/`UncheckedDisabled`/`CheckedDisabled`/`IndeterminateDisabled` to the "Shipped-control aliases" group, and §3.4 `:164-171` says the palette overrides these keys in `Default`/`Light`/`Dark`; §3.5 and §8.2 give only the HC values (`#FFFFFF`, `#A0A0A0`). The non-HC values (Fluent base-high for the pressed label and Fluent's captured 60 % base for the disabled labels) are not stated, so the "palette baseline" claim and the Light/Dark resolution test have no defined expectation. State the non-HC values or mark the four aliases HC-only. No shipped surface disables a CheckBox, and the non-HC pressed label falls back to base-high and passes. | Contract §3.4 `:162`, `:164-171`, §3.5 `:208-210`; record §7 `:198`, §8.1, §8.2; probe. |

#### Verdict (round 5)

**Ready with notes.** M-13, m-14, m-15, n-7, n-8, and n-9 are resolved. The realized HC unchecked-pressed label
is `#FFFFFF` on the black card (21:1), the pressed box is `#FFD700`, the three disabled label aliases and glyphs
are `#A0A0A0` (8.03:1), the neutral focus pair is explicit and the realized ring is 21:1 layer-to-layer in
Light/Dark and 14.97:1 in HC, and the splitter/tab-pipe are in the realized test list. One Minor (m-16) and one
Note (n-10) remain; no Major remains, and no implementation code exists on the branch.

Follow-up owner: Planner (clarify m-16/n-10 or record their acceptance); the human owner may accept the notes
and approve the start.
Follow-up step: — (out-of-step maintenance fix; no step tag).

## Note acceptance (2026-11-22)

The owner accepted the round-5 **Ready with notes** verdict and approved starting the implementor. The two
notes are resolved by this documentation commit: **m-16** now states the focus-ring layer pairing (2 px primary
against the adjacent surface; inner 1 px secondary against the primary, not the surface) in the contract §3.6/
§11.1 and the maintenance record §8.1/§8.2; **n-10** now states `CheckBoxForegroundUncheckedPressed` at
`#D4D4D4` dark / `#333333` light / `#FFFFFF` HC and marks the three `CheckBoxForeground*Disabled` label
aliases as HC-only overrides, not palette Light/Dark keys. No reviewer finding, evidence, or verdict text is
changed, and no re-review is requested.
