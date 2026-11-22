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
| Status | Design frozen for round 3 review. No implementation yet. |
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

`ToggleSwitch`, `RadioButton`, `Calendar`, and `TableView` are not shipped. Their Fluent aliases remain
inventory/test accuracy only.

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
   A targeted style in `ShellStyles.axaml`, mirroring Fluent's selectors, reached both the realized background
   and foreground. This is a targeted style, not a control template.

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

## 6. Theme switch and high contrast

The fixed Light/Dark values live in the palette theme dictionaries. An actual Light/Dark switch updates the
base roles, the six derived Fluent colours, and the CheckBox/Slider alias keys automatically. The targeted
TreeView styles consume the same palette tokens and update with the theme. `SystemAccentColor` is fixed
`#007ACC` (or `#FFD700` under high contrast); there is no `ActualThemeVariantChanged` accent reapply and no
runtime accent overlay. Fluent's default neutral focus ring uses
`SystemControlFocusVisualPrimaryBrush`/`SecondaryBrush`, is accepted as a neutral ring, and is tested at 3:1;
it is not accent-coloured.

High contrast is a merged dictionary applied after the palette. It defines the base roles, the six
`SystemAccentColor*` values as **`Color`** resources (`#FFD700`), the derived-state brushes, the
`CheckBoxCheck*`/`Slider*` alias keys (`#FFD700` fills/tracks, `#000000` glyphs), and the tree selected
background (`#FFD700`) plus targeted style foreground (`#000000`). A high-contrast brush written into a
`SystemAccentColor*` key throws `InvalidCastException`; the design and tests keep the type distinction.

## 7. Implementation shape

| Area | Change |
| --- | --- |
| `Palette.axaml` | Fixed accent family in `Default`/`Light`/`Dark`: base roles, derived `Color` keys, CheckBox/Slider alias keys, ComboBox selected keys, tab pipe, focus/status values. No per-accent overlay. |
| `HighContrast.axaml` | Fixed HC values for the same keys; six derived `Color` resources; alias keys as brushes; tree selected background/foreground tokens. |
| `AvaloniaThemeApplier` | Remove the per-accent overlay and the `AccentPalette` path. Set `SystemAccentColor` to `#007ACC` or `#FFD700` under HC; keep theme/density/high-contrast/typography application. No `ActualThemeVariantChanged` accent reapply. |
| `ShellStyles.axaml` | Add targeted `TreeViewItem:selected` / `:pointerover` / `:pressed` styles for `Border#PART_LayoutRoot` and `ContentPresenter#PART_HeaderPresenter`, using `Mocassin.SelectedBackground`/`SelectedForeground`. |
| Settings | Remove the UI, model property, palette class, reader/validator path, and writer output; add legacy-ignore tests. |
| Tests | Add fixed-family resource, realized-template-part, theme-switch, high-contrast, and settings-removal/compatibility tests in §8. |
| Documentation | Visual contract §2/§3.2–§3.6/§6/§7.3/§11.1/§13; user settings page; this record. |

No control template, no new dependency, no `contracts/manifest.json` change, no ADR, no interaction-contract
change, no public API change.

## 8. Test plan

All tests are PR-fast headless unless named PR-medium. They use the real palette, high-contrast dictionary,
`ShellStyles`, `ShellWindow`, and the existing `HeadlessSession`.

### 8.1 Resources and contrast

- Resolve the fixed base roles, six derived `Color` keys, CheckBox/Slider alias keys, ComboBox selected keys,
  status bar, and tab pipe in Light, Dark, and high contrast.
- Assert the six derived keys are `Color`, not `ISolidColorBrush`, in both palette and high contrast.
- Contrast: accent text 4.5:1; on-accent white on `#007ACC` 4.5:1; selected text 4.5:1; status bar 4.5:1;
  focus `#248DD3`/`#007ACC` and derived states 3:1 on `#3C3C3C`, `#37373D`, `#2D2D30`, `#252526`, `#1E1E1E`,
  `#F3F3F3`, `#FFFFFF`; HC pairs 7:1; neutral focus ring 3:1.

### 8.2 Realized template parts

Create a real `ShellWindow`/Settings/Validation/Projects surface and read the rendered controls:

- checked/indeterminate `CheckBox`: `NormalRectangle.Background`/`BorderBrush`, `CheckGlyph.Fill`;
- `Slider`: `Thumb.Background`, `PART_DecreaseButton.Background`;
- selected `TreeViewItem`: `PART_LayoutRoot.Background`, `PART_HeaderPresenter.Foreground`;
- selected `ComboBoxItem`: selected fill/text.

Assert Light/Dark/HC values and that the parts are not the platform `#0078D7`. The tests must read the
realized part, not only `TryGetResource`.

### 8.3 Theme switch, HC, and settings

- Switch `Application.Current.RequestedThemeVariant` (with a controllable `IPlatformSettings` for System)
  Light→Dark and assert the realized parts update through the palette theme dictionaries; no runtime accent
  reapply is expected.
- High contrast: realized CheckBox/Slider/Tree parts are `#FFD700`/`#000000`; HC glyph/tree text meets contrast.
- Settings removal: no Accent control; `AppSettings` has no `Accent`; legacy JSON with
  `accent: "teal"`/`"orange"`/non-string loads without an accent surface, and a save omits `accent`.
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
4. The realized shipped CheckBox, Slider, and TreeView parts use the fixed family in Light, Dark, and HC; no
   shipped control keeps the platform `#0078D7` accent.
5. The six `SystemAccentColor*` HC keys are `Color`; no `InvalidCastException`; HC glyph/tree text meets 3:1
   and selected text 4.5:1.
6. Theme switch updates the realized parts through the palette dictionaries; no runtime accent overlay or
   `ActualThemeVariantChanged` accent reapply.
7. Contrast floors pass, including `#3C3C3C`/`#37373D` focus, neutral Fluent ring, status bar, selected text,
   accent text, and HC.
8. No control template, new dependency, `contracts/manifest.json` change, ADR, interaction-contract change, or
   public API change.
9. `Mocassin.AccentTextBrush` is recorded as resource-only; no rendered accent-text surface is claimed.
10. `./tools/verify.sh all --require` passes; the branch is pushed for owner review.
11. This record is updated with the implementation commit SHAs, exact commands/results, screenshot list, and
    deviations.
12. The parked freeze branch and record are untouched.

## 11. Risks

| Risk | Mitigation |
| --- | --- |
| A shipped control still uses the platform accent | Realized-template-part tests for CheckBox/Slider/Tree in Light/Dark/HC; assert not `#0078D7`. The palette alias keys and targeted Tree style are the delivery points. |
| A palette alias key does not reach a realized part | The probe covers CheckBox/Slider alias keys and Tree background; Tree foreground uses the targeted style. Tests read the realized parts, not `TryGetResource`. |
| HC uses a brush for a `SystemAccentColor*` key | The six HC keys are `<Color>`; tests assert type and catch `InvalidCastException`. |
| HC tree selected text is white on gold | The targeted TreeView style uses `Mocassin.SelectedForeground` (`#000000` in HC); rendered HC test. |
| Settings removal breaks existing files | `accent` is accepted as a silent no-op and omitted on save; legacy tests cover string/unknown/non-string. |
| Theme switch leaves captured alias values | Fixed alias keys live in palette theme dictionaries; probe confirms Light/Dark realized-part switch; no runtime overlay. |
| A targeted TreeView style selector misses hover/press | Mirror Fluent selectors for selected, selected pointer-over, and pressed; render all three states. |
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
| `ui-interaction-contract.md` | **No change.** Focus, `Escape`, `Enter`, layers, Tab order, and splitter behaviour are unchanged. |
| Control templates | **None.** Targeted TreeView selected-state styles only; no control templates. |
| Design review | **Round 3 requested.** The optional-accent design is withdrawn; the fixed-family design and M-6 proof are the new target. |
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
| (round 3 request commit) | `docs: request GUI accent propagation round 3 review [skip ci]` — review record at `Re-review required` |
