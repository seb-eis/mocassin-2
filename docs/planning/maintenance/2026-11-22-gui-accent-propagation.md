# GUI accent propagation — fixed `#007ACC` family (2026-11-22)

An out-of-step maintenance change under `AGENTS.md` §3.5. This record supersedes the optional multi-accent
design in the earlier revision of this page. The owner changed the scope: the accent is a **fixed family**,
not a user-selectable palette. The record is the design and test plan for one
`fix/gui-accent-propagation` branch and one owner `--no-ff` merge approval.

| Field | Value |
| --- | --- |
| Type | Maintenance fix / fixed accent family, settings removal |
| Branch | `fix/gui-accent-propagation` |
| Base | `main` at `19cb6ffe190e02449ce8b827706f365f2ea7afe2` |
| Worktree | `/workspace` (clean, in place) |
| Normative visual contract | [`docs/development/ui-visual-contract.md`](../../development/ui-visual-contract.md) §3.6 |
| Status | Implemented; local gates passed; awaiting owner `--no-ff` merge approval. |
| Tracking | One branch; local gates; one owner `--no-ff` merge approval; no per-feature CI |

## 1. Owner decision

The owner removed accent selectability and fixed the family:

- Remove the Settings accent choice, the `AccentPalette` selection path, and all per-accent overlay logic.
- Fixed fill/status `#007ACC`; on-accent white (4.51:1; keep the white VS Code treatment); dark focus
  `#248DD3`; light focus `#007ACC`; light accent text `#005A9E`; dark accent text `#4DA6FF`; selected
  fill/text from the existing palette (`#094771`/white dark, `#CCE5FF`/`#1F1F1F` light).
- Existing settings that contain an `accent` value must not break. The reader accepts the property as a
  no-op and ignores it; the writer stops writing it. No schema bump; the settings file is app-local.
- The fixed family must work in both themes and under high contrast.

This supersedes the optional-accent matrix, the `AccentPalette` keys, and the per-accent overlay. The earlier
round-1/round-2 records remain the review history and are not rewritten.

## 2. Current fault line

On `main`, [`AvaloniaThemeApplier.ApplyAccent`](../../../src/dotnet/Mocassin.App.Views/Theming/AvaloniaThemeApplier.cs)
(`:104-127`) only patches `SystemAccentColor`, `Mocassin.AccentBrush`, and `Mocassin.OnAccentBrush`. The rest of
the accent family stays at the palette `#007ACC` values. That produced the reported partial propagation when an
optional accent was selected. Even after removing selectability, the fixed family has a second defect: Fluent
12.1.6 captures several base brushes in `StaticResource` aliases at theme load, so a fixed `#007ACC` palette
alone does not reach the realized shipped controls.

The shipped controls are:

| Surface | Control | Location |
| --- | --- | --- |
| Settings | `CheckBox` | `SettingsDialogView.axaml` |
| Settings | `Slider` | `SettingsDialogView.axaml` |
| Settings | `ComboBox` | `SettingsDialogView.axaml` |
| Validation | `CheckBox` | `ProblemsView.axaml` |
| Validation | `ComboBox` | `ProblemsView.axaml` |
| Projects | `TreeView` | `ProjectsRegionView.axaml` |
| Progress dialog | `ProgressBar` | `ProgressDialogView.axaml:11` |

`ToggleSwitch`, `RadioButton`, `Calendar`, and `TableView` are not shipped. Their Fluent aliases remain
inventory/test accuracy only. The shared selected/hover text key `SystemControlHighlightAltBaseHighBrush` also
has `TableViewRow*` and `DateTimePickerShared.xaml:63` consumers; `SystemAccentColorDark2` has an unshipped
`CalendarButton` consumer. These are listed for inventory accuracy, not as app surfaces.

## 3. Fixed values

| Role | Dark | Light | Notes |
| --- | --- | --- | --- |
| `Mocassin.AccentBrush`, `Mocassin.StatusBarBackground` | `#007ACC` | `#007ACC` | fill/status |
| `Mocassin.OnAccentBrush`, `Mocassin.StatusBarForeground` | `#FFFFFF` | `#FFFFFF` | 4.51:1 on `#007ACC` |
| `Mocassin.FocusBrush`, `SystemControlHighlightAccentBrush`, `TextControlBorderBrushFocused` | `#248DD3` | `#007ACC` | 3:1 on `#3C3C3C`/`#37373D` |
| `Mocassin.AccentTextBrush` | `#4DA6FF` | `#005A9E` | 4.5:1 on chrome/workspace/card |
| `Mocassin.SelectedBackground` / `SelectedForeground` | `#094771` / `#FFFFFF` | `#CCE5FF` / `#1F1F1F` | existing palette values |
| `SystemAccentColorLight1` / `Light2` / `Light3` | `#3A9CDD` | `#0090F0` | derived hover |
| `SystemAccentColorDark1` | `#248CD1` | `#005994` | derived press |
| `SystemAccentColorDark2` / `Dark3` | `#248CD1` | `#003F6A` / `#002138` | completeness; `Dark2` has an unshipped `CalendarButton` consumer |
| `AccentButtonBackground` / `AccentButtonBorderBrush` | `#007ACC` | `#007ACC` | accent button |
| `AccentButtonForeground` | `#FFFFFF` | `#FFFFFF` | on-accent |

`SelectedFill`/`SelectedText` are not derived per accent any more. They are the existing fixed palette values:
dark `#094771`/`#FFFFFF`; light `#CCE5FF`/`#1F1F1F`. There is no 30/70 blend, no per-accent tint, and no
rounding-mode dependency.

## 4. M-6: Fluent alias delivery proof

Fluent 12.1.6 defines the shipped control parts through alias keys such as
`CheckBoxCheckBackgroundFillChecked`, `SliderThumbBackground`, `SliderTrackValueFill`, and
`TreeViewItemBackgroundSelected*`. The aliases are `StaticResource` captures. Three probes were run against
Avalonia 12.1.6 headless (scratch, uncommitted):

1. **Load order.** Defining `SystemAccentColor` before `FluentTheme` loaded made
   `CheckBoxCheckBackgroundFillChecked`, `SliderThumbBackground`, `SliderTrackValueFill`, and
   `TreeViewItemBackgroundSelected*` capture the app colour; glyphs captured white, and the tree captured the
   colour at 0.6/0.8 opacity. This proves load-order capture exists, but it cannot express the distinct
   selected fill/focus values and cannot follow Light/Dark or high contrast. It is rejected as the sole
   mechanism.
2. **Palette alias keys for CheckBox/Slider.** Defining the alias keys in the palette theme dictionaries
   reached the realized `CheckBox.NormalRectangle.Background`, `CheckGlyph.Fill`,
   `Slider.Thumb.Background`, and `PART_DecreaseButton.Background`, and switched with the Light/Dark theme
   dictionaries. The same alias keys in a merged high-contrast dictionary reached the realized parts as
   `#FFD700`/`#000000`. A targeted style cannot replace the Slider thumb because Fluent's template sets its
   `Background` as a local value; the alias key is the correct delivery point.
3. **Targeted TreeView styles.** The palette alias key reached `PART_LayoutRoot.Background` but **not**
   `PART_HeaderPresenter.Foreground` (the probe kept `#FFFFFF`/`#000000` instead of the palette alias value).
   The exact working selectors are:
   - `TreeViewItem:selected /template/ Border#PART_LayoutRoot`
   - `TreeViewItem:selected /template/ Border#PART_LayoutRoot:pointerover`
   - `TreeViewItem:pressed:selected /template/ Border#PART_LayoutRoot:pointerover`
   - `TreeViewItem:selected /template/ ContentPresenter#PART_HeaderPresenter` (direct; the Fluent-shaped
     `Border#PART_LayoutRoot > ContentPresenter#PART_HeaderPresenter` does **not** match because the presenter's
     visual parent is `Grid#PART_Header`)
   - `TreeViewItem:selected /template/ ToggleButton#PART_ExpandCollapseChevron /template/ Path#ChevronPath`
     (the chevron uses `TreeViewItemForeground`, so its `Fill` must be set explicitly)

   The probes reached the realized layout root, header presenter, and chevron. This is a targeted style, not a
   control template.

The fixed-family delivery also resolves the remaining sub-surfaces:

- **HC neutral focus pair (M-10).** `HighContrast.axaml` defines
  `SystemControlFocusVisualPrimaryBrush` `#FFD700` and `SystemControlFocusVisualSecondaryBrush` `#000000`; the
  2 px primary is gold on black and the secondary is black on gold. Tests render the focus adorner in HC Light
  and Dark.
- **HC selected-tree chevron (M-11).** The targeted `Path#ChevronPath` fill uses
  `Mocassin.SelectedForeground` (`#000000` in HC), so the chevron is not white on gold.
- **TextBox selection foreground (M-12).** `ShellStyles.axaml` sets
  `TextBox.SelectionBrush="{DynamicResource Mocassin.AccentBrush}"` and
  `TextBox.SelectionForegroundBrush="{DynamicResource Mocassin.OnAccentBrush}"`. The pair is
  `#FFFFFF` on `#007ACC` (4.51:1) and `#000000` on `#FFD700` in HC (14.97:1); the rendered `TextBox` test reads
  both properties.
- **HC unchecked-pressed/disabled check-box sub-states (M-13/m-14).** The unchecked-pressed **box** fill is
  `CheckBoxCheckBackgroundFillUncheckedPressed` `#FFD700`, but its label is `PART_ContentPresenter` on the
  transparent row/card (`#000000`), so `CheckBoxForegroundUncheckedPressed` is `#FFFFFF`, not black. The
  non-HC pressed-label alias is `#D4D4D4` dark / `#333333` light. HC defines the three disabled label aliases
  `CheckBoxForegroundUncheckedDisabled`/`CheckedDisabled`/`IndeterminateDisabled` `#A0A0A0`; these are
  **HC-only overrides**, not palette Light/Dark keys, because no shipped surface disables a CheckBox and the
  non-HC captured values are low contrast. Checked/indeterminate disabled fills are `#000000` and disabled
  glyphs `#A0A0A0`. The disabled-label overrides are component-level future-proofing; tests read the realized
  pressed label foreground as well as the box/glyph, and the HC disabled component states.
- **ProgressBar (m-7).** The `ProgressDialog` `ProgressBar` is a shipped surface; its `Foreground` follows
  `SystemControlHighlightAccentBrush` (`#007ACC` light, `#248DD3` dark, `#FFD700` HC) and is in the
  realized-part test list.

The concrete delivery is therefore:

- Palette `Light`/`Dark`/`Default` dictionaries define the fixed base roles, the six derived Fluent colours,
  and the CheckBox/Slider alias keys.
- `HighContrast.axaml` defines the same keys with high-contrast values.
- `ShellStyles.axaml` adds targeted `TreeViewItem` selected-state styles for
  `Border#PART_LayoutRoot` and `ContentPresenter#PART_HeaderPresenter`, using
  `Mocassin.SelectedBackground`/`SelectedForeground`.
- The `ComboBoxItem*` alias keys are already palette keys; they reach the realized ComboBox parts.

No broad custom template is needed, and no shipped control keeps the platform accent. The targeted TreeView
styles are the only template-selector styles.

## 5. Settings removal and compatibility

Remove the user-facing setting and its data path:

- `SettingsDialogView.axaml:25-27`: remove the Accent label/ComboBox and reflow the grid.
- `SettingsViewModel.cs:26,109,150,190`: remove `selectedAccent`, `Accents`, `SelectedAccent`, and the
  save/load mapping.
- `AccentPalette.cs`: delete the class.
- `AppSettings.cs:35`: remove the `Accent` property.
- `AppSettingsJsonReader.cs:89,501-...`: remove the `Accent` read and the unknown-accent warning.
- `AppSettingsValidator.cs:35-39,93`: remove accent validation and copying.
- The source-generated writer no longer emits `accent` because the property is gone.
- Tests: remove the accent-palette/validation tests; add a legacy compatibility test that an existing
  `accent` property is accepted and ignored, and that a save omits it.

Compatibility decision: a present `accent` property is an **accepted no-op**. Any well-formed string value is
ignored silently; it is not validated, not surfaced, and not written. A non-string value is ignored by the
same no-op path. There is no schema bump: `Mocassin.Settings.json` is app-local, not a project/package/
results/state contract. The next settings save removes the property. No migration code is required beyond
ignoring the key.

Removing the Accent ComboBox also removes one Settings focus stop and the `Accent` automation name. This is an
intentional, owner-approved accessibility delta: the remaining Settings Tab order is unchanged relative to
itself, the new deterministic order is tested, and no other focus target changes. The interaction-contract
rules themselves do not change.

## 6. Theme switch and high contrast

The fixed Light/Dark values live in the palette theme dictionaries. An actual Light/Dark switch updates the
base roles, the six derived Fluent colours, and the CheckBox/Slider alias keys automatically. The targeted
TreeView styles consume the same palette tokens and update with the theme. `SystemAccentColor` is fixed
`#007ACC` (or `#FFD700` under high contrast); there is no `ActualThemeVariantChanged` accent reapply and no
runtime accent overlay. Fluent's default neutral focus ring uses
`SystemControlFocusVisualPrimaryBrush`/`SecondaryBrush`, is accepted as a neutral ring, and is tested at 3:1;
it is not accent-coloured. High contrast overrides the pair to `#FFD700`/`#000000`.

High contrast is a merged dictionary applied after the palette. It defines the base roles, the six
`SystemAccentColor*` values as **`Color`** resources (`#FFD700`), the derived-state brushes, the
`CheckBoxCheck*`/`Slider*` alias keys (`#FFD700` fills/tracks, `#000000` glyphs), the focus pair
(`#FFD700`/`#000000`), the unchecked-pressed/disabled check-box sub-states, the TextBox selection pair
(`#FFD700`/`#000000`), and the tree selected background (`#FFD700`) plus targeted style foreground
(`#000000`) and chevron fill. A high-contrast brush written into a `SystemAccentColor*` key throws
`InvalidCastException`; the design and tests keep the type distinction.

## 7. Implementation shape

| Area | Change |
| --- | --- |
| `Palette.axaml` | Fixed accent family in `Default`/`Light`/`Dark`: base roles, derived `Color` keys, CheckBox/Slider alias keys, ComboBox selected keys, `TabItemHeaderSelectedPipeFill` = `FocusBrush`, `SystemControlFocusVisualPrimaryBrush`/`SecondaryBrush` (`#000000`/`#FFFFFF` light, `#FFFFFF`/`#000000` dark), focus/status values. No per-accent overlay. |
| `HighContrast.axaml` | Fixed HC values for the same keys; six derived `Color` resources; alias keys as brushes; focus pair `#FFD700`/`#000000`; unchecked-pressed/disabled check-box sub-states; TextBox selection pair; tree selected background/foreground tokens. |
| `AvaloniaThemeApplier` | Remove the per-accent overlay and the `AccentPalette` path. Set `SystemAccentColor` to `#007ACC` or `#FFD700` under HC; keep theme/density/high-contrast/typography application. Remove the now-dead `AccentColor`, `OnAccent`, `Contrast`, `RelativeLuminance`, `Linear` helpers and the `lightOnAccent`/`darkOnAccent` constants. No `ActualThemeVariantChanged` accent reapply. |
| `ShellStyles.axaml` | Add the exact targeted `TreeViewItem` selectors in §4 for `Border#PART_LayoutRoot`, direct `ContentPresenter#PART_HeaderPresenter`, and `Path#ChevronPath`; set `TextBox.SelectionBrush`/`SelectionForegroundBrush` to `Mocassin.AccentBrush`/`Mocassin.OnAccentBrush`. |
| `ShellView.axaml` | Use `Mocassin.FocusBrush` for the three splitter `FocusAdorner` borders, so the focus role has a consumer. |
| Settings | Remove the UI, model property, palette class, reader/validator path, and writer output; add legacy-ignore tests. |
| Tests | Add fixed-family resource, realized-template-part, theme-switch, high-contrast, and settings-removal/compatibility tests in §8. |
| Documentation | Visual contract §2/§3.2–§3.6/§6/§7.3/§11.1/§13; user settings page; this record. |

`CheckBoxForegroundUncheckedPressed` is a palette `Light`/`Dark` alias at `#333333`/`#D4D4D4` (HC
`#FFFFFF`). The three `CheckBoxForeground*Disabled` label aliases are HC-only overrides (`#A0A0A0`), not
palette `Light`/`Dark` keys. No control template, no new dependency, no `contracts/manifest.json` change, no
ADR, no interaction-contract change, no public API change.

## 8. Test plan

All tests are PR-fast headless unless named PR-medium. They use the real palette, high-contrast dictionary,
`ShellStyles`, `ShellWindow`, and the existing `HeadlessSession`.

### 8.1 Resources and contrast

- Resolve the fixed base roles, six derived `Color` keys, CheckBox/Slider alias keys, ComboBox selected keys,
  status bar, and tab pipe in Light, Dark, and high contrast.
- Assert the six derived keys are `Color`, not `ISolidColorBrush`, in `Default`, `Light`, `Dark`, and HC.
- Contrast: accent text 4.5:1; on-accent white on `#007ACC` 4.5:1; selected text 4.5:1; status bar 4.5:1;
  the dark focus values (`#248DD3`, `#3A9CDD`, `#248CD1`) are tested on the dark surfaces (`#3C3C3C`,
  `#37373D`, `#2D2D30`, `#252526`, `#1E1E1E`); the light values (`#007ACC`, `#0090F0`, `#005994`) are tested
  on the light surfaces (`#F3F3F3`, `#FFFFFF`). This is a paired test, not a cross-product. HC pairs 7:1;
  HC focus pair `#FFD700`/`#000000` 3:1; the non-HC neutral pair is `#000000`/`#FFFFFF` light and
  `#FFFFFF`/`#000000` dark. Measure by layer: the 2 px primary against the adjacent surface; the inner 1 px
  secondary against the 2 px primary, not against the card surface.

### 8.2 Realized template parts

Create a real `ShellWindow`/Settings/Validation/Projects surface and read the rendered controls:

- checked/indeterminate `CheckBox`: `NormalRectangle.Background`/`BorderBrush`, `CheckGlyph.Fill`,
  `PART_ContentPresenter.Foreground` (label), plus the HC unchecked-pressed label (white on black) and the
  component-level HC disabled labels/glyphs (`#A0A0A0`);
- `Slider`: `Thumb.Background`, `PART_DecreaseButton.Background`;
- selected `TreeViewItem`: `PART_LayoutRoot.Background`, direct `ContentPresenter#PART_HeaderPresenter.Foreground`,
  `Path#ChevronPath.Fill`;
- selected `ComboBoxItem`: selected fill/text;
- splitter `FocusAdorner` border brush (`Mocassin.FocusBrush`) and selected-tab pipe;
- dialog `ProgressBar.Foreground`;
- `TextBox.SelectionBrush`/`SelectionForegroundBrush`.

Assert Light/Dark/HC values and that the parts are not the platform `#0078D7`. The tests must read the
realized part, not only `TryGetResource`. Render the neutral focus adorner on a real control and assert the
2 px primary against the adjacent surface and the inner 1 px secondary against the primary; render the
selected tree header/chevron in HC Light and Dark.

### 8.3 Theme switch, HC, and settings

- Switch `Application.Current.RequestedThemeVariant` (with a controllable `IPlatformSettings` for System)
  Light→Dark and assert the realized parts update through the palette theme dictionaries; no runtime accent
  reapply is expected.
- High contrast: realized CheckBox/Slider/Tree parts are `#FFD700`/`#000000`; HC glyph/tree text meets contrast.
- Settings removal: no Accent control; `AppSettings` has no `Accent`; legacy JSON with
  `accent: "teal"`/`"orange"`/non-string loads without an accent surface, and a save omits `accent`.
- Settings accessibility: the new deterministic Tab order is asserted; the `Accent` automation name is absent;
  no other focus target changes.
- Grep/source scan: no `AccentPalette`, `SelectedAccent`, `Accents`, or `AppSettings.Accent` references remain.

### 8.4 Screenshots

Bounded PR-medium baselines: `light-settings.png`, `dark-settings.png`, `highcontrast-settings.png` with the
checked check box and slider; `light-projects.png`, `dark-projects.png` with a selected tree node. No rendered
accent-text surface is claimed; `Mocassin.AccentTextBrush` remains resource-only.

## 9. Documentation

- Visual contract §2 (fixed-family precedence), §3.2 (fixed accent roles), §3.3 (fixed status/selection),
  §3.4 (palette keys and alias keys), §3.5 (HC values and `Color` types), §3.6 (fixed family, M-6 delivery,
  alias table, targeted Tree style, theme switch), §6 (focus/selected, neutral ring), §7.3 (status bar),
  §11.1 (fixed contrast pairs), §13 (verification).
- `docs/user-guide/settings.md`: remove the accent choice and describe the fixed family if the page mentions it.
- `docs/development/testing-and-parity.md`: replace optional-accent checks with fixed-family checks.
- No schema, contracts, or interaction-contract change.

## 10. Acceptance criteria

1. The Settings UI has no accent choice; no `AccentPalette`, `SelectedAccent`, `Accents`, or
   `AppSettings.Accent` path remains.
2. Existing settings with an `accent` value load without an error or warning and save without the property;
   no schema bump.
3. The fixed family resolves in Light, Dark, and high contrast: `#007ACC` fill/status, white on-accent,
   `#248DD3` dark focus, `#007ACC` light focus, `#4DA6FF`/`#005A9E` accent text, selected
   `#094771`/white and `#CCE5FF`/`#1F1F1F`.
4. The realized shipped CheckBox, Slider, TreeView, dialog ProgressBar, and TextBox selection parts use the
   fixed family in Light, Dark, and HC; no shipped control keeps the platform `#0078D7` accent.
5. The selected TreeView styles use the exact direct `ContentPresenter#PART_HeaderPresenter` selector (not the
   Fluent `Border > ContentPresenter` shape) and set the chevron `Fill`; the realized header and chevron meet
   3:1 in HC Light and Dark.
6. The six `SystemAccentColor*` keys are `Color` in `Default`/`Light`/`Dark`/HC; no `InvalidCastException`;
   HC glyph/tree text meets 3:1 and selected text 4.5:1.
7. HC defines `SystemControlFocusVisualPrimaryBrush`/`SecondaryBrush` gold/black; the realized focus adorner
   meets 3:1 in HC Light and Dark.
8. `TextBox.SelectionForegroundBrush` is set to `Mocassin.OnAccentBrush`; the rendered selection pair is
   white on `#007ACC` and black on HC gold at 4.5:1.
9. `Mocassin.FocusBrush` is consumed by the splitter focus adorner and the selected-tab pipe, not unused.
10. Theme switch updates the realized parts through the palette dictionaries; no runtime accent overlay or
    `ActualThemeVariantChanged` accent reapply.
11. Contrast floors pass with the paired surfaces (dark values on dark surfaces, light values on light
    surfaces), including `#3C3C3C`/`#37373D` focus, HC focus pair, status bar, selected text, accent text.
12. Removing the Accent control intentionally changes the Settings Tab order and removes the `Accent`
    automation name; the new order is tested and no other focus target changes.
13. No control template, new dependency, `contracts/manifest.json` change, ADR, interaction-contract change,
    or public API change.
14. `Mocassin.AccentTextBrush` is recorded as resource-only; no rendered accent-text surface is claimed.
15. `./tools/verify.sh all --require` passes; the branch is pushed for owner review.
16. This record is updated with the implementation commit SHAs, exact commands/results, screenshot list, and
    deviations.
17. The parked freeze branch and record are untouched.
18. HC unchecked-pressed check-box label is `#FFFFFF` on the black row/card; checked/indeterminate disabled
    labels/glyphs are `#A0A0A0`; the realized `PART_ContentPresenter.Foreground` is tested.
19. The splitter `FocusAdorner` and selected-tab pipe consume `Mocassin.FocusBrush` and are in the realized
    test list.
20. The neutral focus pair is explicit: light `#000000`/`#FFFFFF`, dark `#FFFFFF`/`#000000`, HC
    `#FFD700`/`#000000`. The realized test measures the 2 px primary against the adjacent surface and the
    1 px secondary against the primary, both at 3:1.
21. Quickstart/platform checklist no longer mention the removed accent setting.

## 11. Risks

| Risk | Mitigation |
| --- | --- |
| A shipped control still uses the platform accent | Realized-template-part tests for CheckBox/Slider/Tree in Light/Dark/HC; assert not `#0078D7`. The palette alias keys and targeted Tree style are the delivery points. |
| A palette alias key does not reach a realized part | The probe covers CheckBox/Slider alias keys and Tree background; Tree foreground uses the targeted style. Tests read the realized parts, not `TryGetResource`. |
| HC uses a brush for a `SystemAccentColor*` key | The six HC keys are `<Color>`; tests assert type and catch `InvalidCastException`. |
| HC tree selected text is white on gold | The targeted TreeView style uses `Mocassin.SelectedForeground` (`#000000` in HC); rendered HC test. |
| Settings removal breaks existing files | `accent` is accepted as a silent no-op and omitted on save; legacy tests cover string/unknown/non-string. |
| Theme switch leaves captured alias values | Fixed alias keys live in palette theme dictionaries; probe confirms Light/Dark realized-part switch; no runtime overlay. |
| A targeted TreeView style selector misses the header or chevron | Use the direct `ContentPresenter#PART_HeaderPresenter` selector (the Fluent `Border > ContentPresenter` shape cannot match) and the nested `ToggleButton#PART_ExpandCollapseChevron /template/ Path#ChevronPath` selector; render header and chevron in Light/Dark/HC. |
| HC focus visual pair is black on black | HC defines `SystemControlFocusVisualPrimaryBrush` `#FFD700` and `SecondaryBrush` `#000000`; render the realized adorner in HC Light/Dark at 3:1. |
| HC selected-tree chevron is white on gold | The targeted `Path#ChevronPath` fill uses `Mocassin.SelectedForeground` (`#000000` HC); rendered chevron test. |
| HC unchecked-pressed label is black on the black card | The label uses `CheckBoxForegroundUncheckedPressed` `#FFFFFF`; the box remains gold; the realized `PART_ContentPresenter.Foreground` is tested. |
| HC disabled check-box labels are 1:1 | The three disabled label aliases are overridden to `#A0A0A0`; component-level HC tests cover them, and no shipped surface disables a CheckBox today. |
| The neutral focus pair is unset outside HC | The palette defines light `#000000`/`#FFFFFF` and dark `#FFFFFF`/`#000000`; HC `#FFD700`/`#000000`. Realized tests measure the primary against the surface and the secondary against the primary. |
| The splitter FocusAdorner is not tested | It consumes `Mocassin.FocusBrush` and is in the realized test list. |
| TextBox selected text is the control foreground | `TextBox.SelectionForegroundBrush` is set to `Mocassin.OnAccentBrush`; rendered selection pair test. |
| Removing the Accent control silently changes Tab order | Document the intentional delta; test the new deterministic order and absence of the `Accent` automation name. |
| `Mocassin.FocusBrush` is an unused role | Consume it in the splitter focus adorner and the selected-tab pipe; palette `TabItemHeaderSelectedPipeFill` uses the same value. |
| The fixed family is mistaken for a selectable palette | Contract §3.6 and the record say one fixed family; no `AccentPalette`/per-accent tests remain. |
| Scope grows into broad templates or a redesign | No broad template; only the targeted TreeView selected styles. Stop if a shipped part cannot be delivered without one. |

## 12. In scope and deferred

**In scope**

- Removing accent selectability and the `AccentPalette` data path.
- Fixed palette values, derived Fluent colours, CheckBox/Slider alias keys, and HC overrides.
- Targeted TreeView selected-state styles.
- Realized-template-part, theme-switch, HC, and settings-compatibility tests.
- The visual contract, user-settings page, testing-and-parity page, and this record.

**Deferred**

- Any new accent setting, palette selection, or per-accent overlay.
- Broad custom control templates, a theme redesign, a third high-contrast theme, or density changes.
- New hyperlink/accent-text surfaces; `Mocassin.AccentTextBrush` is resource-only.
- Charts, 3D views, rendering, and `Mocassin.Rendering`.
- A new OS theme detector.
- The parked command-palette freeze branch and its record.
- Any project/package/results/state format, `contracts/manifest.json`, public API, or interaction-contract
  change.

## 13. Gates and owner decisions

| Gate | Decision |
| --- | --- |
| ADR | **Not required.** App-local visual tokens and settings removal; no format, dependency, architecture, or public API change. |
| Amendment | **Not applicable.** Out-of-step maintenance under `AGENTS.md` §3.5. |
| `ui-interaction-contract.md` | **No change.** The interaction rules are unchanged; the Settings Accent focus stop is intentionally removed with its control. |
| Settings Tab order | **Intentional accessibility delta.** The Accent ComboBox focus stop and `Accent` automation name are gone; the remaining order is tested and no other focus target changes. |
| Control templates | **None.** Targeted TreeView selected-state styles only; no control templates. |
| Design review | **Round 5 `Ready with notes`.** The notes are accepted; implementation is complete. |
| Step tag | **None.** |
| Parity matrix | **No change.** |
| Merge | One owner `--no-ff` approval after local gates and the evidence below. No per-feature CI. |
| Platform runs | Not a merge gate; headless on the platform matrix when dispatched; screenshots Linux-only. |

Owner decisions:

| ID | Decision |
| --- | --- |
| A1 | **Remove accent selectability.** Fixed `#007ACC` family; no Settings choice, `AccentPalette`, or per-accent logic. |
| A2 | **Fixed values.** Fill/status `#007ACC`; on-accent white; dark focus `#248DD3`; light focus `#007ACC`; accent text `#4DA6FF`/`#005A9E`; selected `#094771`/white and `#CCE5FF`/`#1F1F1F`. |
| A3 | **Legacy `accent` is an accepted no-op.** Reader ignores, writer omits, no schema bump. |
| A4 | **Accent text remains resource-only.** No rendered link/accent-text surface is claimed. |

## 14. Done when

- The fixed family is in the palette and high-contrast dictionaries, and the targeted TreeView styles are in
  `ShellStyles.axaml`.
- The Settings accent choice and the `AccentPalette`/reader/writer/validator path are gone; legacy `accent`
  values load as no-ops and are not written.
- Realized-template-part tests pass for CheckBox, Slider, and TreeView in Light/Dark/HC.
- Theme switch and high contrast update the realized parts; the six HC derived keys are `Color`.
- `./tools/verify.sh all --require` passes; the branch is pushed for owner review.
- This record is updated with the implementation commit SHAs, exact commands/results, screenshot list, and any
  deviation.
- The owner approves the `--no-ff` merge under §3.5; no step tag is created.

## 15. Commit list

| Commit | Content |
| --- | --- |
| `c0628c2` | `docs: define GUI accent propagation [skip ci]` — initial optional-accent design |
| `6ed4858` | `docs: clarify accent selected-text fallback [skip ci]` — round 1 freeze |
| `f355490` | `docs: record GUI accent propagation design review [skip ci]` — round 1 findings |
| `d359839` | `docs: resolve GUI accent propagation review findings [skip ci]` — round 1 corrections, part 1 |
| `e7a6227` | `docs: resolve GUI accent propagation review findings [skip ci]` — round 1 corrections, part 2 |
| `0b8beea` | `docs: request GUI accent propagation re-review [skip ci]` — round 2 request |
| `cbf2d90` | `docs: record GUI accent propagation re-review [skip ci]` — round 2 findings |
| `787c4ed` | `docs: resolve GUI accent propagation round 2 findings [skip ci]` — optional-accent corrections, now superseded |
| `c7532fb` | `docs: record round 2 resolution commit [skip ci]` — reviewed, now superseded |
| `6db42cf` | `docs: replace optional accent with fixed family [skip ci]` — fixed-family contract and record |
| `82d3422` | `docs: record fixed-accent design commit [skip ci]` — commit list update |
| `73a297f` | `docs: clarify fixed accent precedence [skip ci]` — precedence clarification |
| `7a8f5bc` | `docs: request fixed-accent round 3 review [skip ci]` — round 3 request |
| `7ba3600` | `docs: record fixed-accent round 3 review [skip ci]` — round 3 findings |
| `cc06a67` | `docs: resolve fixed-accent delivery findings [skip ci]` — selectors, HC focus/chevron, TextBox selection, FocusBrush |
| `f1a8c1a` | `docs: record fixed-accent delivery commit [skip ci]` — commit list update |
| `e8983ea` | `docs: request fixed-accent round 4 review [skip ci]` — round 4 request |
| `657da5b` | `docs: record fixed-accent round 4 review [skip ci]` — round 4 findings |
| `27dceb0` | `docs: resolve HC check-box label findings [skip ci]` — M-13/m-14 and docs/test gaps |
| (round 5 request commit) | `docs: request fixed-accent round 5 review [skip ci]` — review record at `Re-review required` |
| `8021cb9` | `app: deliver fixed accent family and remove accent selection` — palette, HC, applier, targeted styles, settings removal |
| `e3a3f4e` | `test: cover fixed accent delivery and legacy accent no-op` — realized parts, contrast, theme switch, screenshots, legacy settings |

## 16. Implementation evidence

| Field | Value |
| --- | --- |
| Branch | `fix/gui-accent-propagation` |
| Implementation commits | `8021cb9` (app), `e3a3f4e` (tests) |
| Base | `main` at `19cb6ffe190e02449ce8b827706f365f2ea7afe2` |
| Full gate | `./tools/verify.sh all --require` — exit 0 on the final code+test+evidence tree (`4e49a23`); 301 App tests, 2,189 .NET tests |
| Final docs re-check | `./tools/verify.sh docs --require` and `./tools/verify.sh policy --require` — exit 0 after the last evidence-only edit |
| App tests | 301 passed, 0 failed (`Mocassin.App.Tests`, net11.0) |
| All .NET tests | 2,189 passed, 0 failed |
| Docs/policy | Part of the full gate: strict Zensical, DocFX, rustdoc, `check_docs_links.py`, `check_doc_math.py`, `check_process_consistency.py`, 60 policy tests, ApiCompat, contracts, SPDX, secret scan, `cargo-deny` — all clean |
| Format | `dotnet format Mocassin.slnx --verify-no-changes --no-restore` — exit 0 |

### 16.1 What landed

- `Palette.axaml` `Default`/`Light`/`Dark`: fixed base roles, `Mocassin.FocusBrush`, the six derived
  `SystemAccentColor*` **`Color`** keys, `SystemControlForegroundAccentBrush`, the two reveal brushes, the
  CheckBox/Slider alias keys, the neutral focus pair, and the corrected shared selected/hover text key
  (`#FFFFFF` dark / `#1F1F1F` light).
- `HighContrast.axaml`: the same keys with `#FFD700`/`#000000` values, the six derived keys as `<Color>`, the
  HC focus pair, the HC CheckBox sub-states (including the three disabled label aliases), the HC TextBox
  selection pair through the accent roles, and `SystemControlForegroundChromeWhiteBrush`/`Highlight*ChromeWhite`
  `#000000` per contract §3.5/§3.6.
- `AvaloniaThemeApplier`: one direct `SystemAccentColor` (`#007ACC`, HC `#FFD700`); no per-accent overlay; the
  `AccentColor`/`OnAccent`/`Contrast`/`RelativeLuminance`/`Linear` helpers and the two on-accent constants are
  removed. The merged-dictionary slot now re-adds a dictionary if an external reset dropped it.
- `ShellStyles.axaml`: the exact targeted `TreeViewItem:selected` selectors for `Border#PART_LayoutRoot`,
  direct `ContentPresenter#PART_HeaderPresenter`, and `Path#ChevronPath`; `TextBox.SelectionBrush`/
  `SelectionForegroundBrush`.
- `ShellView.axaml`: all three splitter `FocusAdorner` borders use `Mocassin.FocusBrush`.
- Settings: the Accent ComboBox, `AccentPalette`, `SelectedAccent`, `Accents`, `AppSettings.Accent`, the reader
  branch, and the validator path are gone. The Settings grid was reflowed. A legacy `accent` property is an
  accepted silent no-op and is omitted on the next write; no schema bump.

### 16.2 Tests

- `Visual/AccentDeliveryTests` (new, 11 PR-fast headless tests): realized `CheckBox`
  `NormalRectangle.Background`/`BorderBrush`, `CheckGlyph.Fill`, and `PART_ContentPresenter.Foreground`; realized
  `Slider` thumb and `PART_DecreaseButton`; the selected `TreeViewItem` layout root/header/chevron; the selected
  `ComboBoxItem` `PART_ContentPresenter`; the dialog `ProgressBar`; `TextBox` selection pair; splitter focus
  adorner and selected-tab pipe; HC unchecked-pressed and disabled labels; the Light→Dark switch; the neutral
  focus pair layer ratios; and a source scan for the removed selection path.
- `ThemeContrastTests`: fixed roles, fixed family values, the six `Color` types in Default/Light/Dark/HC, the
  extended Fluent/alias key set, fixed-family contrast floors, and the neutral focus pair.
- `AppSettingsReaderTests`: legacy `accent` strings, unknown strings, and non-string values load silently with
  no warning and are omitted on write; the old accent round-trip/validation assertions are removed.
- `SettingsViewModel`/settings-surface tests: no Accent control or automation name and the deterministic
  Settings Tab order is asserted.
- `ThemeApplierTests`: the fixed `SystemAccentColor` and no direct accent brush resources.

### 16.3 Realized-template coverage

The probes and tests read the realized template parts, not only `TryGetResource`:
`CheckBox.NormalRectangle.Background`/`BorderBrush`, `CheckBox.CheckGlyph.Fill`,
`CheckBox.PART_ContentPresenter.Foreground` (including the HC pressed/disabled sub-states),
`Slider` thumb `Background` and `PART_DecreaseButton.Background`, `TreeViewItem` `PART_LayoutRoot.Background`
and the direct `PART_HeaderPresenter.Foreground`/`Path#ChevronPath.Fill`, `ComboBoxItem.PART_ContentPresenter`,
dialog `ProgressBar.Foreground`, `TextBox.SelectionBrush`/`SelectionForegroundBrush`, and the splitter focus
adorner/selected-tab pipe. No realized shipped part is `#0078D7`.

### 16.4 Screenshots

- Regenerate command:
  `MOCASSIN_REGENERATE_VISUAL_BASELINES=1 dotnet test src/dotnet/Mocassin.App.Tests/Mocassin.App.Tests.csproj -c Release --filter "FullyQualifiedName~ScreenshotTests"`.
- Affected and committed: `light-settings.png`, `dark-settings.png` (the Accent row is gone and the Settings
  grid is reflowed). The regenerator writes all seven baselines; the other five kept their committed bytes
  because their pixel comparison still passed and their content did not change.

### 16.5 Deviations and residual risk

- The Fluent neutral focus adorner is not exposed as a visual control in Avalonia 12.1.6 headless; the test
  resolves the explicit `SystemControlFocusVisualPrimaryBrush`/`SecondaryBrush` pair from the theme
  dictionaries and measures the 2 px primary against the paired surface and the 1 px secondary against the
  primary. The realized splitter focus adorner and selected-tab pipe are still read directly. This is the
  accepted m-16 measurement basis rather than a claimed rendered Fluent adorner.
- `Mocassin.AccentTextBrush` remains resource-only; no rendered accent-text/link surface is claimed.
- The non-HC disabled CheckBox label aliases remain Fluent's captured low-contrast values; only HC is
  overridden (`#A0A0A0`), because no shipped surface disables a CheckBox. This is the recorded future-only risk.
- The ComboBox selected fill is read on the realized `PART_ContentPresenter.Background`; the `ComboBoxItem`
  itself delegates its fill to that presenter.
- Platform Windows/macOS runs are not a merge gate. Both manual platform gates were dispatched on this branch
  and passed:
  - Windows: run `45016997359` — success (`https://github.com/seb-eis/mocassin-2/actions/runs/45016997359`).
  - macOS: run `45017296087` — success (`https://github.com/seb-eis/mocassin-2/actions/runs/45017296087`).
