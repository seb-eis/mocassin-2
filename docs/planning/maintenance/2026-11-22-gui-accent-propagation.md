# GUI accent propagation (2026-11-22)

An out-of-step maintenance change under `AGENTS.md` §3.5. It is not a new step, has no step branch, no step tag,
and no amendment. The owner reported that selecting the amber accent changed some accent elements but left other
accent surfaces default blue. This record is the design and test plan for one `fix/gui-accent-propagation`
branch and one owner `--no-ff` merge approval.

| Field | Value |
| --- | --- |
| Type | Maintenance fix / bounded accent-propagation change |
| Branch | `fix/gui-accent-propagation` |
| Base | `main` at `19cb6ffe190e02449ce8b827706f365f2ea7afe2` |
| Worktree | `/workspace` (clean, in place) |
| Normative visual contract | [`docs/development/ui-visual-contract.md`](../../development/ui-visual-contract.md) §3.6 |
| Interaction behaviour | [`docs/development/ui-interaction-contract.md`](../../development/ui-interaction-contract.md) — unchanged |
| Status | Design frozen for review. No implementation yet. |
| Tracking | One branch; local gates; one owner `--no-ff` merge approval; no per-feature CI |

The visual contract is the single home for the values and precedence rules. This record holds the defect
evidence, inventory, fix shape, test plan, acceptance criteria, risks, and owner decisions. If the two disagree,
the visual contract wins and this record is corrected.

## 1. Owner report and expectation

The owner selected **amber** in Settings. Some accent surfaces changed, but other accent surfaces stayed the
default blue. The owner expects a selected accent to apply to **all** accent surfaces:

- accent fills and accent buttons;
- focus rings and focus borders;
- the document and console selected-tab indicator;
- links and accent-coloured text;
- selected list, tree, combo, and text-selection states;
- the status bar.

Default/`null` and the explicit `blue` key remain `#007ACC`. High contrast still wins over any selected accent.
The five optional accent hex values must not change unless a contrast requirement forces it:

| Key | Value |
| --- | --- |
| `teal` | `#008B8B` |
| `green` | `#2E7D32` |
| `amber` | `#B26A00` |
| `violet` | `#6A4C93` |
| `rose` | `#B0004F` |

The parked `fix/gui-palette-freeze` branch and its record are unrelated and are not touched.

## 2. Verified fault line

The report is reproducible from the code on `main` at `19cb6ff`.

### 2.1 Applier behaviour

[`AvaloniaThemeApplier.ApplyAccent`](../../../src/dotnet/Mocassin.App.Views/Theming/AvaloniaThemeApplier.cs)
(`AvaloniaThemeApplier.cs:104-127`) only touches three resources:

1. writes `SystemAccentColor` (`:108` under high contrast, `:115` otherwise);
2. for high contrast, removes `Mocassin.AccentBrush` and `Mocassin.OnAccentBrush` (`:109-110`);
3. for `null`, removes `Mocassin.AccentBrush` and `Mocassin.OnAccentBrush` (`:120-121`);
4. for an optional accent, writes only `Mocassin.AccentBrush` and `Mocassin.OnAccentBrush` (`:125-126`).

It never writes or removes `Mocassin.AccentTextBrush`, `Mocassin.StatusBarBackground`,
`Mocassin.StatusBarForeground`, `Mocassin.SelectedBackground`, `Mocassin.SelectedForeground`,
`SystemControlHighlightAccentBrush`, `SystemControlHighlightAltBaseHighBrush`,
`SystemControlHighlightListAccentLowBrush` / `Medium` / `High`, the `AccentButton*` family,
`TextControlBorderBrushFocused`, `TextControlSelectionHighlightColor`, the
`ComboBoxItemBackgroundSelected*` / `ComboBoxItemForegroundSelected*` family, or
`TabItemHeaderSelectedPipeFill`. Those keys therefore keep the palette's fixed `#007ACC`/`#094771`/`#CCE5FF`
values, which is exactly the reported symptom.

The same code has a latent default/`blue` inconsistency: `OnAccent` (`:166-167`) chooses the **maximum** of
white and black contrast. For `#007ACC`, black is 4.66:1 and white is 4.51:1, so an explicit `blue` currently
publishes a black on-accent foreground, while `null` removes the resource and the palette supplies white. The
fix makes `null` and `blue` identical and preserves the approved white foreground.

### 2.2 Accent-resource inventory

The palette has three theme dictionaries (`Default`, `Light`, `Dark`) with the same keys. Line numbers below are
the `Default`/`Light`/`Dark` entries in
[`Palette.axaml`](../../../src/dotnet/Mocassin.App.Views/Themes/Palette.axaml).

| Resource | Palette baseline (`Default` / `Light` / `Dark`) | Applier today | Consumer |
| --- | --- | --- | --- |
| `SystemAccentColor` | not in the palette | written (`:108`, `:115`) | Fluent accent derivation |
| `Mocassin.AccentBrush` | `21` / `183` / `345` | optional only (`:125`) | [`ShellStyles.axaml:32-36`](../../../src/dotnet/Mocassin.App.Views/Styles/ShellStyles.axaml) `Button.accent`; [`ShellView.axaml:51,76,98`](../../../src/dotnet/Mocassin.App.Views/Shell/ShellView.axaml) splitter focus adorners |
| `Mocassin.AccentTextBrush` | `22` / `184` / `346` | none | no current view consumer; reserved for accent text and links |
| `Mocassin.OnAccentBrush` | `23` / `185` / `347` | optional only (`:126`) | `Button.accent` foreground |
| `Mocassin.FocusBrush` | absent | none | not yet defined; proposed focus role |
| `Mocassin.StatusBarBackground` | `40` / `202` / `364` | none | [`StatusBarView.axaml:6`](../../../src/dotnet/Mocassin.App.Views/Shell/StatusBarView.axaml) |
| `Mocassin.StatusBarForeground` | `41` / `203` / `365` | none | `StatusBarView.axaml:13,27,31,45` |
| `Mocassin.SelectedBackground` | `28` / `190` / `352` | none | contract role; the Fluent list/combo keys below carry the same values |
| `Mocassin.SelectedForeground` | `29` / `191` / `353` | none | contract role; same note |
| `SystemControlHighlightAccentBrush` | `54` / `216` / `378` | none | Fluent focus, slider, toggle, progress, selected accent |
| `SystemControlHighlightAltBaseHighBrush` | `55` / `217` / `379` | none | Fluent text/glyphs on an accent control |
| `SystemControlHighlightListAccentLowBrush` | `58` / `220` / `382` | none | Fluent list/tree selected fill |
| `SystemControlHighlightListAccentMediumBrush` | `59` / `221` / `383` | none | Fluent list/tree selected fill |
| `SystemControlHighlightListAccentHighBrush` | `60` / `222` / `384` | none | Fluent list/tree selected fill |
| `AccentButtonBackground` | `74` / `236` / `398` | none | Fluent accent button |
| `AccentButtonBackgroundPointerOver` | `75` / `237` / `399` | none | Fluent accent button |
| `AccentButtonBackgroundPressed` | `76` / `238` / `400` | none | Fluent accent button |
| `AccentButtonBorderBrush` | `82` / `244` / `406` | none | Fluent accent button |
| `AccentButtonBorderBrushPointerOver` | `83` / `245` / `407` | none | Fluent accent button |
| `AccentButtonBorderBrushPressed` | `84` / `246` / `408` | none | Fluent accent button |
| `AccentButtonForeground` | `78` / `240` / `402` | none | Fluent accent button |
| `AccentButtonForegroundPointerOver` | `79` / `241` / `403` | none | Fluent accent button |
| `AccentButtonForegroundPressed` | `80` / `242` / `404` | none | Fluent accent button |
| `TextControlBorderBrushFocused` | `96` / `258` / `420` | none | TextBox focus border |
| `TextControlSelectionHighlightColor` | `102` / `264` / `426` | none | TextBox selection fallback |
| `ComboBoxItemBackgroundSelected` | `124` / `286` / `448` | none | ComboBox selected item |
| `ComboBoxItemBackgroundSelectedPointerOver` | `125` / `287` / `449` | none | ComboBox selected item |
| `ComboBoxItemBackgroundSelectedPressed` | `126` / `288` / `450` | none | ComboBox selected item |
| `ComboBoxItemForegroundSelected` | `132` / `294` / `456` | none | ComboBox selected item |
| `ComboBoxItemForegroundSelectedPointerOver` | `133` / `295` / `457` | none | ComboBox selected item |
| `ComboBoxItemForegroundSelectedPressed` | `134` / `296` / `458` | none | ComboBox selected item |
| `TabItemHeaderSelectedPipeFill` | `150` / `312` / `474` | none | document and console selected-tab pipe |

Related high-contrast entries are in
[`HighContrast.axaml:19-21`](../../../src/dotnet/Mocassin.App.Views/Themes/HighContrast.axaml) (accent roles),
`:38-39` (status bar), `:52-58` (Fluent accent/selection), `:71-82` (accent button), `:99` (text selection),
`:121-124` (combo selection), and `:147` (tab pipe). There is no `Mocassin.FocusBrush` entry today.

The accent is selected through
[`AccentPalette.cs:8-20`](../../../src/dotnet/Mocassin.App.ViewModels/Settings/AccentPalette.cs),
stored as `AppSettings.Accent` ([`AppSettings.cs:35`](../../../src/dotnet/Mocassin.App.ViewModels/Settings/AppSettings.cs)),
and applied live by
[`StartupCoordinator.cs:48-50`](../../../src/dotnet/Mocassin.App/Startup/StartupCoordinator.cs)
(`settingsService.Changed += (_, updated) => themeApplier.Apply(updated)`).

### 2.3 Existing tests and their gaps

- [`ThemeApplierTests.cs:51-63`](../../../src/dotnet/Mocassin.App.Tests/Settings/ThemeApplierTests.cs) checks
  the light theme and default accent; it asserts only that `Mocassin.AccentBrush` and `Mocassin.OnAccentBrush`
  are absent, not that the rest of the accent family follows.
- [`ThemeContrastTests.cs:37-57`](../../../src/dotnet/Mocassin.App.Tests/Visual/ThemeContrastTests.cs) fixes the
  default palette values; `:254-305` applies high contrast and optional accents but checks only
  `Mocassin.AccentBrush`/`Mocassin.OnAccentBrush`; `:341-348` checks the default `#007ACC` focus ring only.

The current suite therefore cannot see the reported bug. The fix adds a full overlay matrix.

## 3. Root cause

`AvaloniaThemeApplier` treats the accent as a three-resource patch instead of a complete runtime overlay. The
palette deliberately owns every accent-family key at the default `#007ACC` baseline, and the applier does not
outrank it for the other keys. Any surface that consumes a palette-only key therefore stays blue. The selected
accent reaches only the surfaces that consume `Mocassin.AccentBrush`, `Mocassin.OnAccentBrush`, or
`SystemAccentColor`.

## 4. Design

The normative rules and values are in visual contract §3.6. The implementation shape is:

### 4.1 Applied values

- The visual accent is the selected raw hex: `#007ACC` for `null`/`blue`, otherwise the five fixed optional
  values. The hex values do not change.
- `OnAccent(a)` is white when white on `a` is at least 4.5:1; otherwise black. This keeps default/`blue` white
  and makes explicit `blue` identical to `null`.
- `AccentText(a, theme)` keeps the approved default `#4DA6FF`/`#005A9E`. For optional accents it is the raw
  accent when it passes 4.5:1 on the worst theme surface; otherwise the first 1% sRGB blend toward white (dark)
  or black (light) that passes. The worst surfaces are the dark card `#2D2D30` and the light chrome `#F3F3F3`.
- `FocusAccent(a, theme)` is the raw accent when it passes 3:1 on the worst theme surface; otherwise the first 1%
  blend toward white/black that passes. This is a focus/indicator/Fluent-control variant, not the accent fill.
  Green, violet, and rose need it on dark; blue, teal, and amber already pass.
- `SelectedFill(a, theme)` keeps the approved `#094771`/`#CCE5FF` for `null`/`blue`. For an optional accent it
  is the 30% accent / 70% chrome blend, so the selected row reads as the chosen accent instead of blue.
- `SelectedText(fill, theme)` is the theme's `ForegroundStrongBrush` when it passes 4.5:1 on the fill;
  otherwise white. Dark is white for every accent; light is `#1F1F1F` except rose, which needs white.

The exact optional-accent values are in visual contract §3.6 and are the test oracle. They are derived values,
not new palette entries.

### 4.2 Overlay and restoration

`AvaloniaThemeApplier` gains one `RemoveAccentOverlay` path and one `PublishAccentOverlay` path. `ApplyAccent`
always removes the whole overlay first, then:

1. sets `SystemAccentColor`;
2. returns for high contrast, so the high-contrast dictionary owns every accent role;
3. returns for `null` and `blue`, so the palette owns the approved default values;
4. otherwise publishes the complete §3.6 table for the active light/dark values.

Switching between optional accents replaces every key. Switching to default/`blue` or high contrast removes
every key, so the resolved values match a fresh start. A theme change with the same optional accent recomputes
the theme-specific values. `ThemeSelection.System` uses the `Default` dictionary, which mirrors `Light`, so the
light column applies. Density is not part of the overlay.

### 4.3 New focus role

`Mocassin.FocusBrush` is added as a derived accent role (`#007ACC` in the default palette, `#FFD700` in high
contrast). It is not an accent-palette expansion: the six selectable accents and their hex values are unchanged.
It exists because the raw accent cannot meet the 3:1 focus/indicator floor for green, violet, and rose on the
dark card, and the explicit splitter focus adorners need a resource that is distinct from the accent fill.

The explicit `GridSplitter` focus adorners in `ShellView.axaml` change from `Mocassin.AccentBrush` to
`Mocassin.FocusBrush`. The decorative hover line keeps `Mocassin.AccentBrush`. Fluent's
`SystemControlHighlightAccentBrush` and `TextControlBorderBrushFocused` are overlaid to `FocusAccent`; no control
template is replaced.

### 4.4 Text selection

`ShellStyles.axaml` adds a `TextBox` style setting `SelectionBrush` to `Mocassin.AccentBrush` and
`SelectionForegroundBrush` to `Mocassin.OnAccentBrush`. This guarantees the matching pair at 4.5:1 for every
accent. `TextControlSelectionHighlightColor` is overlaid to the raw accent as the Fluent fallback, matching the
palette's existing resource type. This is a property setter, not a control template.

## 5. Fix shape

| Area | Change |
| --- | --- |
| `AvaloniaThemeApplier` | Add the complete overlay/removal table; add `AccentText`, `FocusAccent`, `SelectedFill`, `SelectedText`, and a white-first `OnAccent`; use the light values for `System`. |
| `Palette.axaml` | Add `Mocassin.FocusBrush` = `#007ACC` to `Default`, `Light`, and `Dark`. Do not change the five optional accent hex values or any approved palette value. |
| `HighContrast.axaml` | Add `Mocassin.FocusBrush` = `#FFD700`. The existing high-contrast entries already cover the other overlay keys. |
| `ShellStyles.axaml` | Add the `TextBox.SelectionBrush` / `SelectionForegroundBrush` pair. |
| `ShellView.axaml` | Use `Mocassin.FocusBrush` for the three splitter focus adorners. |
| Tests | Add the accent matrix, contrast, rendered-surface, restoration, live-switch, and high-contrast tests in §6. |
| Documentation | Visual contract §2/§3.2/§3.3/§3.4/§3.5/§3.6/§6/§7.1/§7.3/§7.4/§11.1/§13 and this record. |

No control template, no new dependency, no `contracts/manifest.json` change, no ADR, no interaction-contract
change, no public API change.

## 6. Test plan

All tests are PR-fast headless unless named PR-medium. They use the real `AvaloniaThemeApplier`, the real
palette and high-contrast dictionaries, the real `ShellWindow`, and the existing `HeadlessSession`.

### 6.1 Resource matrix

For each accent in `null`, `blue`, `teal`, `green`, `amber`, `violet`, `rose`, and each theme in Light/Dark:

- Apply and assert `SystemAccentColor`.
- Assert every §3.6 resource resolves to the table value, with the correct type.
- Assert the disabled keys are not direct overlay resources and still resolve to the theme disabled values.
- Assert `null` and `blue` resolve identically and leave no direct accent overlay except `SystemAccentColor`.
- Assert `Mocassin.AccentBrush` remains the raw accent and `Mocassin.FocusBrush` is the focus variant where the
  raw accent fails 3:1.

### 6.2 Contrast

- `AccentText` on chrome, workspace, and card: at least 4.5:1 in both themes for all accents.
- `OnAccent(raw)` on raw accent (accent button and status bar): at least 4.5:1.
- `OnAccent(FocusAccent)` on `FocusAccent` (Fluent accent control): at least 4.5:1.
- `SelectedText` on `SelectedFill`: at least 4.5:1.
- `StatusBarForeground` on `StatusBarBackground`, including the progress foreground: at least 4.5:1.
- `FocusBrush` on the dark/light worst surfaces: at least 3:1.
- Text selection pair: at least 4.5:1.
- High-contrast pairs remain at least 7:1.

### 6.3 Rendered shell and controls

- Amber/dark: the status bar background is amber and its text/progress foreground is black; the accent button
  is amber with black text; the document and console selected-tab pipes are the amber focus variant; the
  selected list/combo row is the amber selection tint with the derived text.
- Violet/dark: the raw accent button remains `#6A4C93` with white text; the focus ring and Fluent accent control
  use `#856CA6` with black text/glyphs; no control template is involved.
- Teal/dark: the status bar/button foreground is black; the focus ring remains raw teal.
- Focus a `TextBox` and each splitter; assert the focus border/adorner resolves to `FocusBrush`.
- Select text in a `TextBox`; assert the rendered selection uses the accent/on-accent pair.
- Select a list row and a combo item; assert the selected fill/text pair.
- The document and console tab strips both use the same pipe and selection values.

### 6.4 Restoration, live switch, and high contrast

- Apply teal, then `null`: every overlay key is absent; the resolved values equal a fresh default start.
- Apply amber, then `blue`: the resolved tree equals `null`, including white on-accent text.
- Apply an optional accent dark, then light: the derived text/focus/selection values follow the table.
- Drive a real settings change through the settings service and assert the live `Changed` handler reapplies the
  overlay without a restart.
- Apply an optional accent, then high contrast: every overlay key is absent; all accent roles resolve to
  `#FFD700`/`#000000`/`#FFFFFF`; border thickness is 2; compact density survives. Apply back to the optional
  accent and assert restoration.
- Enumerate every overlay key and assert high contrast never leaks a selected accent.

### 6.5 Screenshots

Add representative PR-medium baselines rather than a twelve-image matrix:

- `light-amber.png` and `dark-amber.png`: full shell with the status bar, an active document tab, and a
  selected list row.
- `dark-violet.png`: full shell with a focused splitter or text input, to show the contrast-safe focus variant
  next to the raw accent button.

Review each capture against the contract. The resource/contrast matrix covers all six accents; the screenshots
are a visual spot-check.

## 7. Documentation

- Update visual contract §2 (precedence), §3.2 (accent roles and `FocusBrush`), §3.3 (status bar and selection
  roles), §3.4 (runtime accent subset), §3.5 (high-contrast removal and `FocusBrush`), §3.6 (new accent model,
  overlay set, derivation, values), §6 (focus/selected states), §7.1 (splitter focus), §7.3 (status bar), §7.4
  (tab pipe), §11.1 (contrast pairs), and §13 (verification).
- Keep this record as the implementation/test evidence home; append the commit SHAs, exact commands/results,
  and any deviation.
- No published user-guide page is needed: the accent setting is already documented where the settings are
  described, and the visual contract is a developer contract. If the implementor finds a user-facing settings
  description that names the status bar or accent text, update it in the same branch.
- No `docs/planning/` page is published.

## 8. Acceptance criteria

1. Selecting each of the six accents updates every surface in §1 in both light and dark themes: accent fills,
   focus rings and borders, document/console tab pipes, accent text, accent buttons, selected list/tree/combo
   rows, text selection, and the status bar.
2. `null` and `blue` both resolve to `#007ACC` and produce identical surfaces. Selecting `blue` after another
   accent fully restores the palette defaults, including white on-accent text.
3. High contrast wins for every accent; no selected accent leaks into any accent role; density is unchanged.
4. The contrast floors pass: accent text 4.5:1 on chrome/workspace/card, on-accent 4.5:1, selected text 4.5:1,
   status bar 4.5:1, focus/indicator 3:1, high contrast 7:1.
5. The five optional accent hex values are unchanged.
6. A live settings change re-applies the overlay without a restart; no stale key remains.
7. No control template, new dependency, `contracts/manifest.json` change, ADR, interaction-contract change, or
   public API change.
8. The new tests pass; `./tools/verify.sh all --require` passes on the branch; the branch is pushed for owner
   review.
9. This record is updated with the commit SHAs, exact commands/results, screenshot list, and deviations.
10. The parked freeze branch and record are untouched.

## 9. Risks

| Risk | Mitigation |
| --- | --- |
| A Fluent key is missed, leaving a surface blue | The §3.6 overlay table is the single list; a test enumerates every key and asserts its value in both themes and all accents. |
| High contrast leaks a selected accent | The applier removes the entire overlay under high contrast; a test applies every accent with high contrast and checks the high-contrast values. |
| Fluent uses `SystemControlHighlightAccentBrush` as both focus and fill | The contract documents `FocusAccent` as the Fluent accent-control variant; accent buttons and explicit fills keep the raw accent. Rendered tests cover both. If the owner rejects the tonal difference, stop and ask before adding a focus-only template. |
| `SystemControlHighlightAltBaseHighBrush` is also used for selected text | Rendered list/combo/text-selection tests catch a conflict. If it breaks selection, keep the TextBox pair on `SelectionBrush`/`SelectionForegroundBrush` and revisit the key. |
| Text selection ignores the style setters | A rendered selection test is required; if Fluent cannot be paired without a template, stop and ask. |
| Selected tints for optional accents are a visual change | The owner chose the accent-derived tint; the default/`blue` values are unchanged; screenshots are reviewed. |
| `ThemeSelection.System` resolves the Default/Light dictionary, not the OS variant | The contract already states that `Default` mirrors `Light`; the test covers `System`. OS-dark support is a separate change. |
| A new `Mocassin.FocusBrush` is missed in one palette dictionary or high contrast | Add it to `Default`, `Light`, `Dark`, and `HighContrast`; the role test enumerates it in all four. |
| The overlay removal path is incomplete | Test the transitions optional→`null`, optional→`blue`, optional→optional, optional→high contrast, and high contrast→optional. |
| TextBox selection style changes existing behavior | The rendered selection pair and the screenshot baselines are reviewed; no template change. |
| The change grows into a palette redesign or template work | The deferred list in §10 is explicit; stop and ask if a surface cannot pass without a template. |

## 10. In scope and deferred

**In scope**

- The complete accent overlay and removal in `AvaloniaThemeApplier`.
- `Mocassin.FocusBrush` as a derived focus/indicator role in the palette and high contrast.
- The `TextBox` selection pair and the splitter focus-adornment change.
- The visual contract update and this record.
- The tests in §6 and the evidence.

**Deferred**

- New accent keys or changes to the five optional hex values.
- A theme redesign, a third high-contrast theme, or a density change.
- Custom control templates, custom scrollbars, menus, tooltips, or context menus.
- New hyperlink surfaces or a reusable link style; `Mocassin.AccentTextBrush` is overlaid and tested, but no new
  link is invented.
- Charts, 3D views, rendering, and any `Mocassin.Rendering` surface.
- OS dark-mode detection for `ThemeSelection.System`.
- The parked command-palette freeze branch and its record.
- Any project/package/results/state format, `contracts/manifest.json`, public API, or interaction-contract
  change.

## 11. Gates and owner decisions

| Gate | Decision |
| --- | --- |
| ADR | **Not required.** This is an application-local visual token and runtime overlay change. It does not change a project/package/results/state format, a dependency, an architecture boundary, or a public API. |
| Amendment | **Not applicable.** Out-of-step maintenance under `AGENTS.md` §3.5. |
| `ui-interaction-contract.md` | **No change.** Focus is still visible; `Escape`, `Enter`, layers, Tab order, and splitter behaviour are unchanged. |
| Control templates | **None.** Only resource overlays and control property setters. |
| Design review | **Requested.** See [`docs/planning/reviews/step-gui-accent-propagation.md`](../reviews/step-gui-accent-propagation.md). The file uses the `step-*` prefix so the reviewer harness can edit it; it is a maintenance review, not a roadmap step. |
| Step tag | **None.** No step is created and no `step-NN` tag is moved. |
| Parity matrix | **No change.** This is a visual maintenance fix, not a feature. |
| Merge | One owner `--no-ff` approval after the local gates and the evidence in this record. No per-feature CI. |
| Platform runs | Not a merge gate. The headless suite runs on the platform matrix when dispatched; screenshots remain Linux-only. A Windows/macOS visual spot-check is recommended and recorded if performed. |

Owner decisions from the 2026-11-22 design discussion:

| ID | Decision |
| --- | --- |
| A1 | **Contrast-safe focus variant.** Keep the five accent hex values. For green, violet, and rose on dark, focus/indicator/Fluent-control keys use the derived `FocusAccent`; accent fills and accent buttons keep the raw selected accent. |
| A2 | **Accent-derived selection tint.** Default/`blue` keep the approved `#094771`/`#CCE5FF` tint; optional accents use the 30% accent/70% chrome tint with a contrast-safe selected foreground. |
| A3 | **Accent text is overlay-and-test only.** `Mocassin.AccentTextBrush` gets the derived value and tests; no new link surface or link style is invented. |

These decisions supersede the earlier refresh record's fixed-white status-bar wording and its
"maximum-contrast" on-accent behaviour for explicit `blue`; the 2026-11-21 record is historical and is not
rewritten.

## 12. Done when

- The visual contract §3.6 is the complete accent model and the sections in §7 point to it consistently.
- The applier publishes and removes the complete overlay; default/`blue` and high contrast restore the palette.
- The palette and high-contrast dictionaries define `Mocassin.FocusBrush`.
- The tests in §6 pass, including the all-accent contrast matrix and the restored/live/high-contrast cases.
- `./tools/verify.sh all --require` passes; the branch is pushed for owner review.
- This record is updated with the implementation commit SHAs, exact commands/results, screenshot list, and any
  deviation.
- The owner approves the `--no-ff` merge under §3.5; no step tag is created.

## 13. Commit list

| Commit | Content |
| --- | --- |
| (design commit) | `docs: define GUI accent propagation [skip ci]` — visual contract §3.6 and this record |
| (review request) | `docs: request GUI accent propagation review [skip ci]` — review record at `Ready for review` |

No code, test, or configuration commit exists yet. The branch base is
`19cb6ffe190e02449ce8b827706f365f2ea7afe2`.
