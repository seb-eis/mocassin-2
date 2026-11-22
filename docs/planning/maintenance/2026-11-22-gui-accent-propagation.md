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
| `SystemAccentColor` | not in the palette | written (`:108`, `:115`) | Fluent base accent / hyperlink |
| `SystemAccentColorLight1` / `Light2` / `Light3` / `Dark1` / `Dark2` / `Dark3` | not in the palette; Fluent derives from the platform accent | none | Fluent derived states: 12.1.6 consumes `Light1`/`Dark1` for checked checkbox/radio/toggle hover and press and slider-thumb hover/press; `Dark2` is consumed by `CalendarButton.xaml` pointer-over/pressed border (unshipped); `Light2`/`Light3`/`Dark3` have no listed 12.1.6 control-resource consumer |
| `Mocassin.AccentBrush` | `21` / `183` / `345` | optional only (`:125`) | [`ShellStyles.axaml:32-36`](../../../src/dotnet/Mocassin.App.Views/Styles/ShellStyles.axaml) `Button.accent`; [`ShellView.axaml:51,76,98`](../../../src/dotnet/Mocassin.App.Views/Shell/ShellView.axaml) splitter focus adorners |
| `Mocassin.AccentTextBrush` | `22` / `184` / `346` | none | no current view consumer; resource-only under A3 |
| `Mocassin.OnAccentBrush` | `23` / `185` / `347` | optional only (`:126`) | `Button.accent` foreground; status-bar/selection pairs |
| `Mocassin.FocusBrush` | absent | none | proposed focus role; palette Dark must be `#248DD3`, Light `#007ACC` |
| `Mocassin.StatusBarBackground` | `40` / `202` / `364` | none | [`StatusBarView.axaml:6`](../../../src/dotnet/Mocassin.App.Views/Shell/StatusBarView.axaml) |
| `Mocassin.StatusBarForeground` | `41` / `203` / `365` | none | `StatusBarView.axaml:13,27,31,45` |
| `Mocassin.SelectedBackground` | `28` / `190` / `352` | none | palette role; not consumed directly by the current views |
| `Mocassin.SelectedForeground` | `29` / `191` / `353` | none | palette role; not consumed directly by the current views |
| `SystemControlHighlightAccentBrush` | `54` / `216` / `378` | none | Fluent focus and accent-control base |
| `SystemControlHighlightAltBaseHighBrush` | `55` / `217` / `379` | none | shared selected/hover text for `ListBoxItem`, `TreeViewItem`, ComboBox focused/selected, MenuFlyout, TabItem, `TableViewRow*`, and `DateTimePickerShared.xaml:63`; **not** an accent-control glyph key |
| `SystemControlForegroundAccentBrush` | not in the palette; Fluent uses `SystemAccentColor` | none | slider thumb |
| `SystemControlHighlightAccent2RevealBackgroundBrush` | not in the palette; Fluent uses `SystemAccentColor` at 0.8 opacity | none | `TreeViewItemBackgroundSelectedPointerOver` |
| `SystemControlHighlightAccent3RevealBackgroundBrush` | not in the palette; Fluent uses `SystemAccentColor` at 0.6 opacity | none | `TreeViewItemBackgroundSelected` |
| `SystemControlHighlightAltChromeWhiteBrush` | not in the palette; Fluent uses `SystemChromeWhiteColor` | none | checkbox checked glyph, toggle-switch knob, toggle-button checked foreground, calendar today |
| `SystemControlHighlightChromeWhiteBrush` | not in the palette; Fluent uses `SystemChromeWhiteColor` | none | toggle-switch knob pointer-over glyph (unshipped; inventory/HC completeness) |
| `SystemControlForegroundChromeWhiteBrush` | `46` / `208` / `370` | none | checkbox/radio checked hover, pressed, and indeterminate glyph |
| `SystemControlHighlightListAccentLowBrush` | `58` / `220` / `382` | none | Fluent list/table selected fill and ComboBox selected mapping |
| `SystemControlHighlightListAccentMediumBrush` | `59` / `221` / `383` | none | Fluent list/table selected fill and ComboBox selected-pointer-over mapping |
| `SystemControlHighlightListAccentHighBrush` | `60` / `222` / `384` | none | Fluent list/table selected fill and ComboBox selected-pressed mapping |
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
`:38-39` (status bar), `:47` (`SystemControlForegroundChromeWhiteBrush`; currently `#FFFFFF` and needs
`#000000`), `:52-58` (Fluent accent/selection), `:71-82` (accent button), `:99` (text selection), `:121-124`
(combo selection), and `:147` (tab pipe). There is no `Mocassin.FocusBrush`, no
`SystemAccentColorLight1`/`Dark1` family, no `SystemControlForegroundAccentBrush`, no accent reveal brush, and
no `SystemControlHighlightAltChromeWhiteBrush` entry today. The light palette
`SystemControlHighlightAltBaseHighBrush` is `#FFFFFF` at line `217`; it must become `#1F1F1F` for selected/hover
text contrast. The dark `SystemControlHighlightAccentBrush` and `TextControlBorderBrushFocused` are `#007ACC`
at lines `378` and `420`; they must become the default `FocusAccent` `#248DD3` so `null`/`blue` focus passes
3:1 on `#3C3C3C` without the optional-accent overlay.

### 2.2.1 Shipped-control alias keys

Fluent 12.1.6 defines these keys as `StaticResource` aliases to the base brushes. The control templates read
the alias keys with `DynamicResource`, but the alias object is captured when the Fluent theme loads, so
overlaying only the base key changes the resource lookup and not the realized template part. The applier must
overlay the alias keys themselves. A scratch headless probe confirmed that direct application-resource alias
overrides reach `CheckBox.NormalRectangle.Background`, `CheckGlyph.Fill`, `Slider.Thumb.Background`,
`Slider.TrackBackground.Background`, and `TreeViewItem.PART_LayoutRoot.Background`.

| Shipped control | Alias keys to overlay | Realized part |
| --- | --- | --- |
| CheckBox (Settings, Validation) | `CheckBoxCheckBackgroundFillChecked`, `CheckBoxCheckBackgroundFillIndeterminate`, `CheckBoxCheckBackgroundStrokeIndeterminate`, `CheckBoxCheckGlyphForegroundChecked`, `CheckBoxCheckGlyphForegroundCheckedPointerOver`, `CheckBoxCheckGlyphForegroundCheckedPressed`, `CheckBoxCheckGlyphForegroundIndeterminate`, `CheckBoxCheckGlyphForegroundIndeterminatePointerOver`, `CheckBoxCheckGlyphForegroundIndeterminatePressed` | `NormalRectangle` background/border, `CheckGlyph.Fill` |
| Slider (Settings) | `SliderThumbBackground`, `SliderTrackValueFill`, `SliderTrackValueFillPointerOver`, `SliderTrackValueFillPressed` | `Thumb.Background`, value-track fill |
| TreeView (Projects) | `TreeViewItemBackgroundSelected`, `TreeViewItemBackgroundSelectedPointerOver`, `TreeViewItemBackgroundSelectedPressed`, `TreeViewItemForegroundSelected`, `TreeViewItemForegroundSelectedPointerOver`, `TreeViewItemForegroundSelectedPressed` | `PART_LayoutRoot.Background`, `PART_HeaderPresenter.Foreground` |
| ComboBox (Settings, Validation) | The existing `ComboBoxItemBackgroundSelected*` / `ComboBoxItemForegroundSelected*` keys are already the alias keys named in the base overlay | ComboBox item selected fill/text |

The `CheckBoxCheckBackgroundFillCheckedPointerOver`/`Pressed`, the slider thumb hover/press, and the tree
stroke/text hover/press aliases that Fluent defines as dynamic `SolidColorBrush` objects follow
`SystemAccentColorLight1`/`Dark1`. The alias overlay is still required for the static aliases above and for the
`TreeViewItemForegroundSelected*` static aliases. `SystemControlHighlightChromeWhiteBrush` remains in the
inventory/HC mapping for the unshipped toggle knob.

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
- The `System` cases at `:203`, `:228`, and `:296` pass `ThemeVariant.Default` explicitly and never assert
  `Application.ActualThemeVariant`, so they cannot distinguish System-light from System-dark.
- No test covers the Fluent derived states (`SystemAccentColorLight1`/`Dark1`), the shared
  `SystemControlHighlightAltBaseHighBrush` selected/hover text, the tree reveal fills, the check/toggle glyph
  keys, the `#3C3C3C`/`#37373D` focus surfaces, or the realized template parts behind Fluent's captured
  `StaticResource` aliases.
- No test exercises a live `Application.ActualThemeVariantChanged`; the app has no such reapply subscription
  today.

The current suite therefore cannot see the reported bug. The fix adds a full three-group overlay matrix,
realized-template-part assertions, and a live-OS-variant test.

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
  blend toward white/black that passes. The worst dark surface is now `#3C3C3C` (input fill; `#37373D` hover is
  slightly darker and covered by the same choice); the worst light surface remains `#F3F3F3`. This makes the
  default dark focus value `#248DD3`, not `#007ACC`, and changes the optional dark values in the contract table.
- `SelectedFill(a, theme)` keeps the approved `#094771`/`#CCE5FF` for `null`/`blue`. For an optional accent it
  is `round(0.30 * a + 0.70 * chrome(theme))`; the contract table was corrected from the accidental 70/30
  table to the 30% accent blend. Dark values are `#153F3F`/`#233B24`/`#4A3515`/`#352C41`/`#4A152D` for
  teal/green/amber/violet/rose; light values are `#AAD4D4`/`#B8D0B9`/`#E0CAAA`/`#CAC1D6`/`#DFAAC2`. The blend
  uses `MidpointRounding.AwayFromZero`; the light `green` row is `#B8D0B9`, not `#AAD0B9`.
- `SelectedText(fill, theme)` is the theme's `ForegroundStrongBrush` when it passes 4.5:1 on the fill;
  otherwise its opposite (`#000000` in dark, `#FFFFFF` in light). With the 30% fills, dark is white for every
  accent and light is `#1F1F1F` for every accent, including light `rose` (`#DFAAC2`); the earlier white
  light-`rose` row was a consequence of the 70% table error.
- `AccentGlyph` is `#FFFFFF`; every `FocusAccent` value keeps at least 3:1 against it. This is the check,
  toggle, and radio glyph colour.
- `FluentState` applies the Fluent 12.1.6 HSL offsets to `FocusAccent` and clamps the offset toward the base
  when the state would fall below 3:1 against the worst theme surface or `AccentGlyph`. 12.1.6 consumes
  `Light1`/`Dark1`; `Light2`/`Light3`/`Dark2`/`Dark3` are overlaid for completeness. The clamp can make a
  state equal the base for accents already on the floor; that is deliberate and avoids a platform accent or a
  sub-3:1 state.

The exact optional-accent values are in visual contract §3.6 and are the test oracle. They are derived values,
not new palette entries. The theme column is selected from `Application.ActualThemeVariant` after applying
`RequestedThemeVariant`; `ThemeSelection.System` therefore uses the OS Light or Dark column, not a fixed light
fallback.

### 4.2 Overlay and restoration

`AvaloniaThemeApplier` gains one `RemoveAccentOverlay` path and three publish groups: base accent, derived
Fluent state, and shipped-control alias. `ApplyAccent` always removes all three groups first, then:

1. sets `SystemAccentColor` to the raw accent, or `#FFD700` under high contrast;
2. returns for high contrast, so the high-contrast dictionary owns every accent role, derived state, alias,
   reveal, and glyph key;
3. for `null` and `blue`, leaves the **base** group absent so the palette owns the approved default base
   values, but publishes the **derived Fluent state** and **shipped-control alias** groups from
   `#007ACC`/`#248DD3`, because Fluent would otherwise derive or capture the platform accent;
4. for an optional accent, publishes all three groups for the actual light/dark column.

Switching between optional accents replaces all three groups. Switching to default/`blue` removes the base
group and replaces the derived and alias groups with the `#007ACC` values. Switching to high contrast removes
all three groups, and the high-contrast dictionary supplies the base, derived, alias, reveal, and glyph keys.
A theme switch with the same accent recomputes all three groups from `Application.ActualThemeVariant`.

`StartupCoordinator` subscribes to `Application.Current.ActualThemeVariantChanged` in addition to
`settingsService.Changed`, and calls `Apply(settingsService.Current)` when the actual variant changes. This is
the concrete live OS Light/Dark trigger; it lives next to the existing settings-change subscription. The tests
bind a controllable `IPlatformSettings` before app setup, flip its `ThemeVariant`, raise `ColorValuesChanged`,
and assert the production `ActualThemeVariantChanged` path recomputes all three groups. There is no test seam
that bypasses the `Application.ActualThemeVariant` read. Density is not part of any overlay.

### 4.3 New focus role

`Mocassin.FocusBrush` is added as a derived accent role (`#248DD3` in the dark palette, `#007ACC` in the light
and default palettes, `#FFD700` in high contrast). It is not an accent-palette expansion: the six selectable
accents and their hex values are unchanged. It exists because the raw accent cannot meet the 3:1
focus/indicator/input-focus floor on the bright dark surfaces (`#3C3C3C` input fill and `#37373D` hover fill)
for any accent, and the explicit splitter focus adorners need a resource that is distinct from the accent fill.
The `#248DD3` dark value is the first 1% white blend of `#007ACC` that reaches 3:1 on `#3C3C3C`.

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
| `AvaloniaThemeApplier` | Add the three overlay/removal groups; add `AccentText`, `FocusAccent`, `SelectedFill`, `SelectedText`, `AccentGlyph`, `FluentState`, and a white-first `OnAccent`; resolve the theme column from `Application.ActualThemeVariant`; overlay the shipped-control alias keys directly. |
| `StartupCoordinator` | Add the `Application.Current.ActualThemeVariantChanged` subscription next to `settingsService.Changed`; reapply `settingsService.Current` on a live actual-variant change. |
| `Palette.axaml` | Add `Mocassin.FocusBrush` = `#248DD3` (Dark) / `#007ACC` (Light/Default). Set the dark `SystemControlHighlightAccentBrush` and `TextControlBorderBrushFocused` to the default `FocusAccent` `#248DD3` (light stays `#007ACC`) so default/`blue` focus passes on `#3C3C3C`. Correct `SystemControlHighlightAltBaseHighBrush` in Light/Default from `#FFFFFF` to `#1F1F1F`; Dark stays `#FFFFFF`. Do not change the five optional accent hex values or any other approved palette value. |
| `HighContrast.axaml` | Add `Mocassin.FocusBrush` = `#FFD700`. Add `SystemAccentColorLight1`/`Light2`/`Light3`/`Dark1`/`Dark2`/`Dark3` as **`Color`** resources (`<Color x:Key=...>#FFD700</Color>`), not brushes. Add `SystemControlForegroundAccentBrush`, `SystemControlHighlightAccent2RevealBackgroundBrush`/`Accent3RevealBackgroundBrush`, and the shipped-control alias keys (`CheckBoxCheck*`, `Slider*`, `TreeViewItemBackgroundSelected*`, `TreeViewItemForegroundSelected*`) as `#FFD700`/`#000000` brushes. Add `SystemControlHighlightAltChromeWhiteBrush` and `SystemControlHighlightChromeWhiteBrush` = `#000000`; change `SystemControlForegroundChromeWhiteBrush` to `#000000`. |
| `ShellStyles.axaml` | Add the `TextBox.SelectionBrush` / `SelectionForegroundBrush` pair. |
| `ShellView.axaml` | Use `Mocassin.FocusBrush` for the three splitter focus adorners. |
| Tests | Add the accent matrix, contrast, realized-template-part, restoration, live-OS-variant, and high-contrast tests in §6. |
| Documentation | Visual contract §2/§3.2/§3.3/§3.4/§3.5/§3.6/§6/§7.1/§7.3/§7.4/§11.1/§13 and this record. |

No control template, no new dependency, no `contracts/manifest.json` change, no ADR, no interaction-contract
change, no public API change.

## 6. Test plan

All tests are PR-fast headless unless named PR-medium. They use the real `AvaloniaThemeApplier`, the real
palette and high-contrast dictionaries, the real `ShellWindow`, and the existing `HeadlessSession`.

### 6.1 Resource matrix

For each accent in `null`, `blue`, `teal`, `green`, `amber`, `violet`, `rose`, and each **actual** theme in
Light/Dark:

- Apply and assert `SystemAccentColor`.
- Assert every base-overlay resource resolves to the §3.6 table value, with the correct type.
- Assert every derived Fluent state resource resolves to the table/`FluentState` value, including
  `SystemAccentColorLight1`/`Dark1` and the `Light2`/`Light3`/`Dark2`/`Dark3` completeness keys. The six
  `SystemAccentColor*` values are `Color` resources, not brushes.
- Assert every shipped-control alias key resolves to the expected brush and that `null`/`blue` publish the
  alias group from `#007ACC`/`#248DD3`, not the platform `#0078D7`.
- Assert `SystemControlHighlightAltBaseHighBrush` is not a direct overlay key and resolves to `#FFFFFF` dark /
  `#1F1F1F` light / `#000000` high contrast.
- Assert the disabled keys are not direct overlay resources and still resolve to the theme disabled values.
- Assert `Mocassin.AccentBrush` remains the raw accent; `Mocassin.FocusBrush` is `#248DD3` dark / `#007ACC`
  light for `null`/`blue`, and `FocusAccent` for optional accents.
- Assert high contrast defines every base, derived, alias, reveal, and glyph key; the six
  `SystemAccentColor*` entries are `Color`; the alias/glyph keys are brushes.
- Assert `SystemControlHighlightChromeWhiteBrush` resolves `#000000` in high contrast (unshipped toggle knob
  inventory).

### 6.2 Contrast

- `AccentText` on chrome, workspace, and card: at least 4.5:1 in both themes for all accents.
- `OnAccent(raw)` on raw accent (accent button and status bar): at least 4.5:1.
- `SelectedText` on `SelectedFill`: at least 4.5:1 for every row; assert light `rose` uses `#1F1F1F` on
  `#DFAAC2`. Assert the corrected light `green` fill is `#B8D0B9` and dark `green` is `#233B24` with the
  documented `MidpointRounding.AwayFromZero` blend rule.
- `SystemControlHighlightAltBaseHighBrush` on every `SelectedFill`, `HoverBackground`, and
  `PressedBackground`: at least 4.5:1. Include light `green`/`violet`, dark `teal`/`amber`, tree/list hover,
  TableViewRow, and DateTimePicker cases where the controls are exercised.
- `AccentGlyph` (`#FFFFFF`) on `FocusAccent` and every `FluentState`: at least 3:1.
- `FocusAccent` and every `FluentState` against `#3C3C3C`, `#37373D`, `#2D2D30`, `#252526`, and `#1E1E1E` in
  dark and `#F3F3F3`/`#FFFFFF` in light: at least 3:1.
- Fluent's neutral default focus ring (`SystemControlFocusVisualPrimaryBrush`/`SecondaryBrush`) against the
  adjacent surfaces: at least 3:1.
- `StatusBarForeground` on `StatusBarBackground`, including the progress foreground: at least 4.5:1.
- Text selection pair: at least 4.5:1.
- High-contrast pairs remain at least 7:1.

### 6.3 Realized template parts and shipped controls

- Realized-part assertions must read the rendered control, not only `TryGetResource`:
  - checked/indeterminate `CheckBox`: `NormalRectangle.Background`/`BorderBrush` and `CheckGlyph.Fill`;
  - `Slider`: `Thumb.Background` and the value-track `Background`;
  - selected `TreeViewItem`: `PART_LayoutRoot.Background` and `PART_HeaderPresenter.Foreground`;
  - selected `ComboBoxItem`: selected background and foreground.
- Amber/dark: the status bar background is amber and its text/progress foreground is black; the accent button
  is amber with black text; the document and console selected-tab pipes are the amber focus variant; the
  realized check box/slider/tree parts use the amber `FocusAccent`/`SelectedFill`/`SelectedText` values.
- Violet/dark and green/light: the list/tree selected and hover text is the theme strong text, not
  `OnAccent(FocusAccent)`; assert the realized foreground contrast.
- Teal/dark and amber/dark: the same list/tree selected-text assertion; assert the realized foreground is the
  theme strong text and meets 4.5:1.
- Checked and indeterminate `CheckBox`, `Slider` normal/pointer-over/pressed, and `TreeView` selected
  normal/pointer-over/pressed: assert the realized brush equals `FocusAccent`, `Light1`, `Dark1`, or
  `SelectedFill` according to the table, and that it is not the platform accent.
- `ComboBox` selected item: assert the realized selected fill/text pair.
- Focus a `TextBox` and each splitter on the `#3C3C3C` input/card surface; assert the focus border/adorner
  resolves to `FocusBrush` and meets 3:1. Assert the neutral Fluent focus ring on a button/list item meets 3:1.
- Select text in a `TextBox`; assert the rendered selection uses the accent/on-accent pair.
- Document and console tab strips use the same pipe and selection values.
- Unshipped `ToggleSwitch`/`RadioButton`/`Calendar`/`TableView` may be covered by component-level alias tests
  for inventory accuracy, but they are not app surfaces and must not be claimed as shipped.

### 6.4 Live OS variant, restoration, and high contrast

- Apply teal, then `null`: the base group is absent; the derived and alias groups are present with `#007ACC`
  values; the resolved base values equal a fresh default start.
- Apply amber, then `blue`: the base group is absent; the derived and alias groups equal the `null`/`blue`
  values.
- Apply an optional accent dark, then light: all three groups follow the actual-variant table.
- Bind a controllable `IPlatformSettings` before app setup with a mutable `ThemeVariant`; apply
  `ThemeSelection.System`; set it Light, then Dark, raising `ColorValuesChanged` each time. Assert the
  production `Application.ActualThemeVariantChanged` path re-runs `Apply` and recomputes all three groups and
  the realized template parts. The test reads `Application.ActualThemeVariant`; no test seam bypasses the
  production read.
- Drive a real settings change through the settings service and assert the live `Changed` handler reapplies
  all three overlays without a restart.
- Apply an optional accent, then high contrast: all three groups are absent; every base/derived/alias/reveal/
  glyph key resolves to `#FFD700`/`#000000`/`#FFFFFF`; the six `SystemAccentColor*` entries are `Color`;
  border thickness is 2; compact density survives. Render a checked `CheckBox`, a `Slider`, and a selected
  `TreeViewItem` in high contrast and assert the realized parts are `#FFD700`/`#000000`.
- Apply back to the optional accent and assert restoration.
- Enumerate every overlay key and assert high contrast never leaks a selected or platform accent.
- Explicitly assert `Mocassin.AccentTextBrush` is resource-only: the resource resolves and meets contrast, but
  no rendered accent-text/link surface is asserted.

### 6.5 Screenshots

Add representative PR-medium baselines rather than a twelve-image matrix:

- `light-amber.png` and `dark-amber.png`: full shell with the status bar, an active document tab, and a
  selected list row.
- `dark-violet.png`: full shell with a focused splitter or text input, to show the contrast-safe focus variant
  next to the raw accent button.
- `light-settings-amber.png` and `dark-settings-amber.png`: settings overlay with a checked check box and the
  accent slider, to show the realized Fluent alias states.
- `dark-projects-amber.png`: Projects tree with a selected node, to show the realized tree selected fill/text.

Review each capture against the contract. The resource/contrast/realized-part matrix covers all six accents and
both actual System variants; the screenshots are a visual spot-check. Do not claim a rendered accent-text
surface; `Mocassin.AccentTextBrush` remains resource-only under A3.

## 7. Documentation

- Update visual contract §2 (precedence), §3.2 (accent roles and `FocusBrush`), §3.3 (status bar and selection
  roles), §3.4 (runtime accent groups, derived Fluent keys, and shipped-control aliases), §3.5 (high-contrast
  removal, derived keys, alias keys, and glyph keys), §3.6 (new accent model, three overlay groups, alias
  mechanism, derivation, values, live actual-variant reapply), §6 (focus/selected states and neutral Fluent
  focus ring), §7.1 (splitter focus), §7.3 (status bar), §7.4 (tab pipe), §11.1 (contrast pairs including
  `#3C3C3C`/`#37373D` and shared selected text), and §13 (verification).
- Keep this record as the implementation/test evidence home; append the commit SHAs, exact commands/results,
  and any deviation.
- No published user-guide page is needed: the accent setting is already documented where the settings are
  described, and the visual contract is a developer contract. If the implementor finds a user-facing settings
  description that names the status bar or accent text, update it in the same branch.
- No `docs/planning/` page is published.

## 8. Acceptance criteria

1. Selecting each of the six accents updates every shipped surface in §1 in both light and dark themes,
   including the realized template parts of the Settings/Validation check boxes, the Settings slider, the
   Projects tree, the combo boxes, the status bar, focus rings/borders, tab pipes, accent text, accent
   buttons, selected rows, and text selection.
2. `null` and `blue` both resolve to `#007ACC` base values and identical base surfaces; the derived and alias
   groups remain #007ACC-focused instead of the platform accent. Selecting `blue` after another accent
   restores the palette base and the derived/alias values.
3. High contrast wins for every accent, including the derived colours, alias keys, reveal brushes, and glyph
   keys; the six derived accent colours are `Color` resources; no selected or platform accent leaks; density is
   unchanged.
4. The contrast floors pass: accent text 4.5:1 on chrome/workspace/card; on-accent text 4.5:1; selected and
   hover text 4.5:1; selected text 4.5:1; status bar 4.5:1; `FocusAccent`/`FluentState` and `AccentGlyph`
   3:1, including against `#3C3C3C` and `#37373D`; the neutral Fluent focus ring 3:1; high contrast 7:1.
5. The five optional accent hex values are unchanged. The 30% accent / 70% chrome blend uses
   `MidpointRounding.AwayFromZero`; light `green` is `#B8D0B9`.
6. A live settings change and a live actual-theme-variant change each re-apply all three overlay groups without
   a restart; no stale key remains. System-light and System-dark use the production
   `Application.ActualThemeVariant` path.
7. Realized-template-part tests, not only resource lookups, prove the shipped check box, slider, and tree use
   the selected accent and high-contrast values.
8. No control template, new dependency, `contracts/manifest.json` change, ADR, interaction-contract change, or
   public API change.
9. `Mocassin.AccentTextBrush` is explicitly recorded as resource-only; no rendered link/accent-text surface is
   claimed.
10. The new tests pass; `./tools/verify.sh all --require` passes on the branch; the branch is pushed for owner
    review.
11. This record is updated with the commit SHAs, exact commands/results, screenshot list, and deviations.
12. The parked freeze branch and record are untouched.

## 9. Risks

| Risk | Mitigation |
| --- | --- |
| A Fluent key is missed, leaving a surface blue | The §3.6 overlay table is the single list; a test enumerates every key and asserts its value in both themes and all accents. |
| High contrast leaks a selected accent | The applier removes the entire overlay under high contrast; a test applies every accent with high contrast and checks the high-contrast values. |
| Fluent uses `SystemControlHighlightAccentBrush` as both focus and fill | The contract documents `FocusAccent` as the Fluent accent-control variant; accent buttons and explicit fills keep the raw accent. Rendered tests cover both. If the owner rejects the tonal difference, stop and ask before adding a focus-only template. |
| `SystemControlHighlightAltBaseHighBrush` is shared by selected/hover text and was overlaid to `OnAccent(FocusAccent)` | The key is removed from all overlays and set to `ForegroundStrongBrush` in the palette (`#1F1F1F` light / `#FFFFFF` dark / `#000000` HC). Rendered list/tree/combo/menu/tab/TableView/DateTimePicker selected and hover tests assert 4.5:1. Accent-control glyphs use the separate ChromeWhite keys. |
| Fluent derived states (`SystemAccentColorLight1`/`Dark1`) fall back to the platform accent | All three overlay groups include the six derived colours, the alias keys, `SystemControlForegroundAccentBrush`, the reveal brushes, and both glyph keys. Rendered realized-template-part tests assert the selected accent, not the platform accent. |
| Fluent `StaticResource` aliases capture the base brushes and ignore base-key overlays | The applier overlays the alias keys themselves; the realized `CheckBox.NormalRectangle`/`CheckGlyph`, `Slider.Thumb`/value track, and `TreeViewItem.PART_LayoutRoot`/`PART_HeaderPresenter` are asserted. The scratch probe confirmed the alias override reaches the parts. |
| The derived-state clamp collapses a hover/pressed fill to the base | The clamp is required to keep the state at or above the 3:1 floor on the bright dark surfaces and on the glyph. The platform accent is never used; the state remains distinguishable through the glyph and the Fluent state transition. Recorded as a visual limitation, not a contrast exception. |
| Text selection ignores the style setters | A rendered selection test is required; if Fluent cannot be paired without a template, stop and ask. |
| Selected tints for optional accents are a visual change | The owner chose the 30% accent-derived tint; the default/`blue` values are unchanged; the corrected table and screenshots are reviewed. The blend uses `MidpointRounding.AwayFromZero`; light `green` is `#B8D0B9`. |
| `ThemeSelection.System` resolves the OS variant, not a fixed light fallback | `StartupCoordinator` subscribes to `Application.ActualThemeVariantChanged` and reapplies. Tests bind a controllable `IPlatformSettings`, flip Light/Dark, raise `ColorValuesChanged`, and assert the production path of all three groups. |
| High contrast uses a brush for a `SystemAccentColor*` key | The six HC derived keys are `<Color>` resources; the tests assert the type, not only resolution. |
| The neutral Fluent default focus ring is not accent-coloured | It is explicitly accepted and tested for 3:1; `TextBox` and splitter focus rings use `FocusBrush`. |
| The overlay removal path is incomplete | Test the transitions optional→`null`, optional→`blue`, optional→optional, optional→high contrast, and high contrast→optional. |
| A new `Mocassin.FocusBrush` is missed in one palette dictionary or high contrast | Add it to `Default` (`#007ACC`), `Light` (`#007ACC`), `Dark` (`#248DD3`), and `HighContrast` (`#FFD700`); the role test enumerates it in all four. |
| The overlay removal path is incomplete | Test the transitions optional→`null`, optional→`blue`, optional→optional, optional→high contrast, and high contrast→optional. |
| TextBox selection style changes existing behavior | The rendered selection pair and the screenshot baselines are reviewed; no template change. |
| The change grows into a palette redesign or template work | The deferred list in §10 is explicit; stop and ask if a surface cannot pass without a template. |

## 10. In scope and deferred

**In scope**

- All three accent overlay groups and their removal in `AvaloniaThemeApplier`, including the system-derived
  Fluent state colours, the shipped-control alias keys, and the glyph/reveal keys.
- The `Application.Current.ActualThemeVariantChanged` live reapply trigger in `StartupCoordinator`.
- `Mocassin.FocusBrush` as a derived focus/indicator role in the palette and high contrast, with the corrected
  `#3C3C3C` dark oracle.
- The light `SystemControlHighlightAltBaseHighBrush` correction to `#1F1F1F` and the high-contrast derived,
  alias, and glyph entries, including the six `Color` resources.
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
- A new OS theme detector; the fix uses Avalonia's existing `Application.ActualThemeVariant`.
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
| A1 | **Contrast-safe focus variant.** Keep the five accent hex values. On the bright dark surfaces (`#3C3C3C` input, `#37373D` hover), focus/indicator/Fluent-control keys use the derived `FocusAccent`; the default dark focus value becomes `#248DD3`. Accent fills and accent buttons keep the raw selected accent. |
| A2 | **Accent-derived selection tint, 30% accent / 70% chrome.** Default/`blue` keep the approved `#094771`/`#CCE5FF` tint; optional accents use the corrected 30% accent blend with `SelectedText` regenerated from the fill. The review's 70/30 table was the error. |
| A3 | **Accent text is overlay-and-test only.** `Mocassin.AccentTextBrush` gets the derived value and resource/contrast tests; it has no rendered surface today, and no new link surface or link style is invented. |

These decisions supersede the earlier refresh record's fixed-white status-bar wording and its
"maximum-contrast" on-accent behaviour for explicit `blue`; the 2026-11-21 record is historical and is not
rewritten.

## 12. Done when

- The visual contract §3.6 is the complete accent model and the sections in §7 point to it consistently.
- The applier publishes and removes all three overlay groups; default/`blue` keeps the base palette and the
  #007ACC-derived Fluent states/aliases; high contrast removes all three and wins.
- The palette and high-contrast dictionaries define `Mocassin.FocusBrush` (`#248DD3` dark / `#007ACC` light /
  `#FFD700` HC), the derived Fluent state keys, and the glyph/reveal keys.
- The tests in §6 pass, including the actual System-light/System-dark cases, the `#3C3C3C`/`#37373D` focus
  floor, the shared selected-text key, and the derived Fluent states.
- `./tools/verify.sh all --require` passes; the branch is pushed for owner review.
- This record is updated with the implementation commit SHAs, exact commands/results, screenshot list, and any
  deviation.
- The owner approves the `--no-ff` merge under §3.5; no step tag is created.

## 13. Commit list

| Commit | Content |
| --- | --- |
| `c0628c2` | `docs: define GUI accent propagation [skip ci]` — initial contract and record |
| `6ed4858` | `docs: clarify accent selected-text fallback [skip ci]` — round 1 frozen design |
| `f355490` | `docs: record GUI accent propagation design review [skip ci]` — round 1 findings |
| `d359839` | `docs: resolve GUI accent propagation review findings [skip ci]` — round 1 corrections, part 1 |
| `e7a6227` | `docs: resolve GUI accent propagation review findings [skip ci]` — round 1 corrections, part 2 |
| `0b8beea` | `docs: request GUI accent propagation re-review [skip ci]` — round 2 request |
| `cbf2d90` | `docs: record GUI accent propagation re-review [skip ci]` — round 2 findings |
| `787c4ed` | `docs: resolve GUI accent propagation round 2 findings [skip ci]` — alias, precedence, live-variant, and inventory corrections |
| (round 3 request commit) | `docs: request GUI accent propagation round 3 review [skip ci]` — review record at `Re-review required` |

No code, test, or configuration commit exists yet. The branch base is
`19cb6ffe190e02449ce8b827706f365f2ea7afe2`.
