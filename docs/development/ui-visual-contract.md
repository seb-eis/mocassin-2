# UI Visual Contract

This page is the normative home for the desktop application's visual language: colour roles, typography,
spacing, shape, states, surface chrome, and the accessibility floor. Behaviour at the keyboard and window edge is
normative in the [UI interaction contract](ui-interaction-contract.md); the project split and composition rules
are in [UI architecture](ui-architecture.md). When this page and a view disagree, this page wins.

It applies to `Mocassin.App.Views` and the view models those views bind to. It is a developer contract, not a
published style guide for third parties. The visual reference is the Visual Studio Code workbench structure; the
provenance and legal boundary are in §12.

## 1. Principles

1. **Structure over decoration.** The application is a chrome backdrop with separated cards, 1 px borders, and
   small gaps. There are no drop shadows, gradients, glass effects, or decorative illustrations.
2. **One token set, two themes.** Every colour is a named role. Light and dark define the same role names; a view
   never branches on the theme.
3. **Density and contrast are overrides, not forks.** Compact density changes spacing and control height only.
   High contrast changes colour and border thickness only. Neither may change layout structure, labels, focus
   order, or behaviour.
4. **No new control templates in this pass.** Fluent 12.1.6 remains the control implementation. The palette
   overrides Fluent resource keys and the app style sheet sets control properties from app tokens. Custom
   scrollbar, menu, tooltip, and context-menu templates are deferred.
5. **The interaction contract is unchanged.** Tabs select with arrows/clicks, `Enter` on a tab strip is a no-op,
   splitters keep their 6 px hit target and keyboard behaviour, and overlays keep their layer/focus rules.
6. **Text and focus first.** A surface that cannot meet the contrast and focus floor in §11 is not acceptable,
   even if it matches the reference more closely.

## 2. Token architecture and precedence

| Layer | Home | Owns |
| --- | --- | --- |
| Base dimensions and typography fallback | `src/dotnet/Mocassin.App/App.axaml` | theme-independent doubles and thicknesses used before the first settings apply |
| Palette | `src/dotnet/Mocassin.App.Views/Themes/Palette.axaml` | `Mocassin.*` colour roles and the Fluent keys in §3.4, with `Light`, `Dark`, and a `Default` fallback |
| High contrast | `src/dotnet/Mocassin.App.Views/Themes/HighContrast.axaml` | every colour role and every overridden Fluent key, with the high-contrast values in §3.5 |
| Compact density | `src/dotnet/Mocassin.App.Views/Themes/Density.axaml` | compact spacing and control-height values |
| Runtime settings | `AvaloniaThemeApplier` | theme variant, font scale, accent, comfortable/compact spacing, border thickness, and motion |
| Control styles | `src/dotnet/Mocassin.App.Views/Styles/ShellStyles.axaml` | padding, alignment, radius, border, and state setters that consume the tokens |

Rules:

- Views consume `Mocassin.*` tokens. A literal colour in a view is a defect, except `Transparent`; the modal
  scrim uses the `Mocassin.OverlayScrim` token.
- Every colour token lives in the palette, not in `App.axaml`. A direct application resource outranks a theme
  dictionary, so leaving a colour in `App.axaml` would silently defeat the palette.
- `App.axaml` keeps only theme-independent fallbacks: font sizes, radius, padding, gap, and transitions. Runtime
  values written by `AvaloniaThemeApplier` are authoritative after settings load.
- The palette also provides a `Default` theme dictionary that mirrors `Light`, for hosts that report no theme
  variant. `Light` and `Dark` are authoritative.
- High contrast is a merged dictionary applied after the palette. It must define every key in §3.4, not a subset,
  so that no palette colour leaks into a high-contrast session.
- Compact density is a merged dictionary and is also written by `AvaloniaThemeApplier`. The two must agree; the
  applier's direct values win at runtime.
- Card chrome is opt-in. A region or overlay card sets its background, border, and radius explicitly or through
  a `region-card`/`overlay-card` class. Do not keep a universal `Border` selector that adds a border to every
  nested `Border`, banner, or separator.
- Effective precedence, highest first:
  1. direct application resources written by `AvaloniaThemeApplier` (font scale, the accent overlay, runtime
     density, border thickness, motion);
  2. the high-contrast merged dictionary;
  3. the compact-density merged dictionary;
  4. the active light/dark palette theme dictionary;
  5. the stock Fluent theme.
- The accent overlay has three direct-resource layers, and they do not all follow the same selection rule:
  1. the **base accent overlay** is published only for an optional accent;
  2. the **derived Fluent state overlay** and the **Fluent alias overlay** are published for every
     non-high-contrast accent, including `null` and the explicit `blue` key, because Fluent otherwise derives
     or captures the platform accent;
  3. high contrast removes all three layers and the high-contrast dictionary is authoritative.

  The palette remains the default/`blue` source for the base roles, and density never changes an accent role.
  §3.6 is the complete accent model.

## 3. Colour roles

### 3.1 Surfaces

| Token | Dark | Light | Use |
| --- | --- | --- | --- |
| `Mocassin.ChromeBackground` | `#1E1E1E` | `#F3F3F3` | shell backdrop, menu bar, tab strips, splitter gaps |
| `Mocassin.WorkspaceBackground` | `#252526` | `#FFFFFF` | region cards, document content, editor surface |
| `Mocassin.CardBackground` | `#2D2D30` | `#F3F3F3` | palette, settings, dialogs, notifications, nested cards |
| `Mocassin.BorderBrush` | `#3C3C3C` | `#E0E0E0` | 1 px card and control borders |
| `Mocassin.InputBackground` | `#3C3C3C` | `#FFFFFF` | `TextBox` and `ComboBox` fill |
| `Mocassin.InputBorderBrush` | `#3C3C3C` | `#CECECE` | input border when not focused |
| `Mocassin.MenuBackground` | `#1E1E1E` | `#F3F3F3` | menu bar |
| `Mocassin.TabStripBackground` | `#1E1E1E` | `#F3F3F3` | document and console tab strips |
| `Mocassin.TabActiveBackground` | `#252526` | `#FFFFFF` | selected tab |
| `Mocassin.TabInactiveBackground` | `#1E1E1E` | `#F3F3F3` | unselected tab |
| `Mocassin.OverlayScrim` | `#66000000` | `#66000000` | modal scrim behind palette, settings, and dialogs |

The shell backdrop and the gaps between cards use `ChromeBackground`. A region card uses
`WorkspaceBackground` and a 1 px `BorderBrush`. A nested card or modal surface uses `CardBackground`. This is
what makes the separated parts read as raised boxes without shadows.

### 3.2 Text and accent

| Token | Dark | Light | Use |
| --- | --- | --- | --- |
| `Mocassin.ForegroundBrush` | `#D4D4D4` | `#333333` | body text |
| `Mocassin.ForegroundStrongBrush` | `#FFFFFF` | `#1F1F1F` | headings, active tab, emphasised values |
| `Mocassin.ForegroundMutedBrush` | `#9D9D9D` | `#6F6F6F` | captions, secondary text, inactive tabs |
| `Mocassin.DisabledForegroundBrush` | `#6B6B6B` | `#9A9A9A` | disabled text |
| `Mocassin.DisabledBackgroundBrush` | `#2A2A2A` | `#EDEDED` | disabled control fill |
| `Mocassin.AccentBrush` | `#007ACC` | `#007ACC` | accent fill, accent-button fill, decorative splitter line |
| `Mocassin.AccentTextBrush` | `#4DA6FF` | `#005A9E` | accent-coloured text and links on a card or workspace |
| `Mocassin.OnAccentBrush` | `#FFFFFF` | `#FFFFFF` | text and glyphs on an accent fill |
| `Mocassin.FocusBrush` | `#248DD3` | `#007ACC` | focus ring, focus border, selected-pipe indicator |
| `Mocassin.TabActiveForeground` | `#FFFFFF` | `#1F1F1F` | selected tab text |
| `Mocassin.TabInactiveForeground` | `#9D9D9D` | `#6F6F6F` | unselected tab text |

`AccentBrush` is the raw selected brand accent. `AccentTextBrush`, `FocusBrush`, and `OnAccentBrush` are
contrast-safe derived roles, not additional brand accents. A filled accent surface always uses
`OnAccentBrush`; a focus ring or selected-pipe indicator uses `FocusBrush`. The five non-default optional
settings accents (`teal #008B8B`, `green #2E7D32`, `amber #B26A00`, `violet #6A4C93`, `rose #B0004F`) keep
their existing keys and hex values. The default and the `blue` key are `#007ACC`, and they are treated
identically, including `OnAccentBrush` = `#FFFFFF`. §3.6 defines the derivation, the complete runtime overlay
set, and the optional-accent values.

### 3.3 States and status

| Token | Dark | Light | Use |
| --- | --- | --- | --- |
| `Mocassin.HoverBackground` | `#37373D` | `#E8E8E8` | pointer-over fill |
| `Mocassin.PressedBackground` | `#2A2D2E` | `#DCDCDC` | pressed fill |
| `Mocassin.SelectedBackground` | `#094771` | `#CCE5FF` | selected list row, combo item, or tree row |
| `Mocassin.SelectedForeground` | `#FFFFFF` | `#1F1F1F` | text on `SelectedBackground` |
| `Mocassin.ErrorForeground` | `#F48771` | `#A1260D` | inline errors and error glyphs |
| `Mocassin.WarningForeground` | `#CCA700` | `#A15C00` | warnings |
| `Mocassin.SuccessForeground` | `#89D185` | `#1B5E20` | success text |
| `Mocassin.InfoForeground` | `#75BEFF` | `#0B4F8A` | informational text |
| `Mocassin.ErrorBackground` | `#3A1D1D` | `#FDECEA` | error banner fill |
| `Mocassin.WarningBackground` | `#3A2F12` | `#FFF4CE` | read-only or warning banner fill |
| `Mocassin.SuccessBackground` | `#1E3A24` | `#E8F5E9` | success banner fill |
| `Mocassin.ErrorBorderBrush` | `#F48771` | `#C62828` | error banner border |
| `Mocassin.WarningBorderBrush` | `#CCA700` | `#B26A00` | warning banner border |
| `Mocassin.SuccessBorderBrush` | `#89D185` | `#2E7D32` | success banner border |
| `Mocassin.StatusBarBackground` | `#007ACC` | `#007ACC` | status bar |
| `Mocassin.StatusBarForeground` | `#FFFFFF` | `#FFFFFF` | status bar text, glyphs, and progress |

The table gives the default/`blue` values. Under an optional accent, §3.6 replaces `StatusBarBackground` with
the selected accent and `StatusBarForeground` with its contrast-safe on-accent value; `SelectedBackground` and
`SelectedForeground` become the accent-derived selection tint and its contrast-safe text. The status bar is the
one full-accent chrome surface. Severity is conveyed by its existing glyph and text, not by colour: all
status-bar glyphs and text use `StatusBarForeground`. This deliberately replaces the old per-severity colours,
which did not meet contrast on a dark bar.

### 3.4 Fluent resource keys

The palette overrides these Fluent 12.1.6 keys so stock controls follow the palette without new templates.
These are the keys the palette is expected to override; adding a key is a contract change and a test update.

| Group | Keys |
| --- | --- |
| Global text and surfaces | `SystemControlForegroundBaseHighBrush`, `SystemControlForegroundBaseMediumBrush`, `SystemControlForegroundBaseMediumLowBrush`, `SystemControlForegroundBaseLowBrush`, `SystemControlForegroundChromeWhiteBrush`, `SystemControlBackgroundAltHighBrush`, `SystemControlBackgroundChromeMediumLowBrush`, `SystemControlTransientBackgroundBrush`, `SystemControlTransientBorderBrush`, `SystemControlDisabledBaseMediumLowBrush`, `SystemControlDisabledBaseLowBrush`, `SystemControlDisabledChromeDisabledLowBrush` |
| Accent and selection | `SystemAccentColorLight1`, `SystemAccentColorLight2`, `SystemAccentColorLight3`, `SystemAccentColorDark1`, `SystemAccentColorDark2`, `SystemAccentColorDark3`, `SystemControlHighlightAccentBrush`, `SystemControlHighlightAltBaseHighBrush`, `SystemControlForegroundAccentBrush`, `SystemControlHighlightAccent2RevealBackgroundBrush`, `SystemControlHighlightAccent3RevealBackgroundBrush`, `SystemControlHighlightAltChromeWhiteBrush`, `SystemControlHighlightChromeWhiteBrush`, `SystemControlHighlightListLowBrush`, `SystemControlHighlightListMediumBrush`, `SystemControlHighlightListAccentLowBrush`, `SystemControlHighlightListAccentMediumBrush`, `SystemControlHighlightListAccentHighBrush` |
| Button | `ButtonBackground`, `ButtonBackgroundPointerOver`, `ButtonBackgroundPressed`, `ButtonBackgroundDisabled`, `ButtonForeground`, `ButtonForegroundPointerOver`, `ButtonForegroundPressed`, `ButtonForegroundDisabled`, `ButtonBorderBrush`, `ButtonBorderBrushPointerOver`, `ButtonBorderBrushPressed`, `ButtonBorderBrushDisabled`, `ButtonBorderThemeThickness` |
| Accent button | `AccentButtonBackground`, `AccentButtonBackgroundPointerOver`, `AccentButtonBackgroundPressed`, `AccentButtonBackgroundDisabled`, `AccentButtonForeground`, `AccentButtonForegroundPointerOver`, `AccentButtonForegroundPressed`, `AccentButtonForegroundDisabled`, `AccentButtonBorderBrush`, `AccentButtonBorderBrushPointerOver`, `AccentButtonBorderBrushPressed`, `AccentButtonBorderBrushDisabled` |
| Text input | `TextControlForeground`, `TextControlForegroundPointerOver`, `TextControlForegroundFocused`, `TextControlForegroundDisabled`, `TextControlBackground`, `TextControlBackgroundPointerOver`, `TextControlBackgroundFocused`, `TextControlBackgroundDisabled`, `TextControlBorderBrush`, `TextControlBorderBrushPointerOver`, `TextControlBorderBrushFocused`, `TextControlBorderBrushDisabled`, `TextControlPlaceholderForeground`, `TextControlPlaceholderForegroundPointerOver`, `TextControlPlaceholderForegroundFocused`, `TextControlPlaceholderForegroundDisabled`, `TextControlSelectionHighlightColor` |
| Combo box | `ComboBoxBackground`, `ComboBoxBackgroundPointerOver`, `ComboBoxBackgroundPressed`, `ComboBoxBackgroundDisabled`, `ComboBoxForeground`, `ComboBoxForegroundFocused`, `ComboBoxForegroundDisabled`, `ComboBoxBorderBrush`, `ComboBoxBorderBrushPointerOver`, `ComboBoxBorderBrushPressed`, `ComboBoxBorderBrushDisabled`, `ComboBoxDropDownBackground`, `ComboBoxDropDownBorderBrush`, `ComboBoxDropDownGlyphForeground`, `ComboBoxDropDownGlyphForegroundFocused`, `ComboBoxDropDownGlyphForegroundFocusedPressed`, `ComboBoxDropDownGlyphForegroundDisabled`, `ComboBoxItemBackground`, `ComboBoxItemBackgroundPointerOver`, `ComboBoxItemBackgroundPressed`, `ComboBoxItemBackgroundDisabled`, `ComboBoxItemBackgroundSelected`, `ComboBoxItemBackgroundSelectedPointerOver`, `ComboBoxItemBackgroundSelectedPressed`, `ComboBoxItemBackgroundSelectedDisabled`, `ComboBoxItemForeground`, `ComboBoxItemForegroundPointerOver`, `ComboBoxItemForegroundPressed`, `ComboBoxItemForegroundDisabled`, `ComboBoxItemForegroundSelected`, `ComboBoxItemForegroundSelectedPointerOver`, `ComboBoxItemForegroundSelectedPressed`, `ComboBoxItemForegroundSelectedDisabled` |
| Tab item | `TabItemHeaderBackgroundUnselected`, `TabItemHeaderBackgroundUnselectedPointerOver`, `TabItemHeaderBackgroundUnselectedPressed`, `TabItemHeaderBackgroundSelected`, `TabItemHeaderBackgroundSelectedPointerOver`, `TabItemHeaderBackgroundSelectedPressed`, `TabItemHeaderBackgroundDisabled`, `TabItemHeaderForegroundUnselected`, `TabItemHeaderForegroundUnselectedPointerOver`, `TabItemHeaderForegroundUnselectedPressed`, `TabItemHeaderForegroundSelected`, `TabItemHeaderForegroundSelectedPointerOver`, `TabItemHeaderForegroundSelectedPressed`, `TabItemHeaderForegroundDisabled`, `TabItemHeaderSelectedPipeFill`, `TabItemMinHeight`, `TabItemHeaderMargin` |
| Menu and flyout | `MenuFlyoutPresenterBackground`, `MenuFlyoutPresenterBorderBrush`, `MenuFlyoutItemBackgroundPointerOver`, `MenuFlyoutItemBackgroundPressed`, `MenuFlyoutItemForeground`, `MenuFlyoutItemForegroundPointerOver`, `MenuFlyoutItemForegroundPressed`, `MenuFlyoutItemForegroundDisabled`, `MenuFlyoutItemKeyboardAcceleratorTextForeground`, `MenuFlyoutItemKeyboardAcceleratorTextForegroundPointerOver`, `MenuFlyoutItemKeyboardAcceleratorTextForegroundPressed`, `MenuFlyoutItemKeyboardAcceleratorTextForegroundDisabled` |
| Shipped-control aliases | `CheckBoxCheckBackgroundFillChecked`, `CheckBoxCheckBackgroundFillIndeterminate`, `CheckBoxCheckBackgroundStrokeIndeterminate`, `CheckBoxCheckGlyphForegroundChecked`, `CheckBoxCheckGlyphForegroundCheckedPointerOver`, `CheckBoxCheckGlyphForegroundCheckedPressed`, `CheckBoxCheckGlyphForegroundIndeterminate`, `CheckBoxCheckGlyphForegroundIndeterminatePointerOver`, `CheckBoxCheckGlyphForegroundIndeterminatePressed`, `SliderThumbBackground`, `SliderTrackValueFill`, `SliderTrackValueFillPointerOver`, `SliderTrackValueFillPressed`, `TreeViewItemBackgroundSelected`, `TreeViewItemBackgroundSelectedPointerOver`, `TreeViewItemBackgroundSelectedPressed`, `TreeViewItemForegroundSelected`, `TreeViewItemForegroundSelectedPointerOver`, `TreeViewItemForegroundSelectedPressed` |

These keys are the palette baseline, including the default/`blue` accent. `AvaloniaThemeApplier` overlays three
groups in §3.6: the base accent group only for an optional accent; the derived Fluent state group and the
shipped-control alias group for every non-high-contrast accent, including `null` and `blue`. The key names do
not change, and the palette remains the source for default restoration while high contrast supplies all three
groups from `HighContrast.axaml`. Adding a key to this table remains a contract change and a test update.

Check boxes, radio buttons, sliders, toggles, progress bars, and tooltips keep their Fluent templates. Their
text, accent, and selection keys resolve through the global entries above, and the shipped-control alias group
is overlaid because Fluent's `StaticResource` aliases capture the base brushes at load. A control that still
shows a Fluent blue or the platform accent after the palette and the §3.6 overlays are applied is a defect in
the key list, not a reason to add a template.

### 3.5 High contrast

When high contrast is enabled, every token in §3.1–§3.3 and every key in §3.4 is overridden by
`HighContrast.axaml`:

| Role | High-contrast value |
| --- | --- |
| All backgrounds (`Chrome`, `Workspace`, `Card`, `Input`, menu, tab strip, status bar, banners) | `#000000` |
| All borders, focus rings, accent fill, selected pipe, active tab indicator | `#FFD700` |
| Body and heading text, status text | `#FFFFFF` |
| Muted text | `#E0E0E0` |
| Disabled foreground | `#A0A0A0` |
| Text and glyphs on an accent fill | `#000000` |
| Hover and pressed fill | `#FFD700` with `#000000` foreground |
| Selected fill | `#FFD700` with `#000000` foreground |
| Overlay scrim | `#000000` |
| Border and focus thickness | `2` |

High contrast keeps the dark/light selection underneath; it is a colour and border override, not a third layout.
The existing `HighContrast` and `Density` dictionaries remain merged dictionaries applied by
`AvaloniaThemeApplier`; their precedence over the palette is mandatory and tested. `HighContrast.axaml` also
defines `Mocassin.FocusBrush` (`#FFD700`), the six `SystemAccentColorLight1`/`Light2`/`Light3`/
`Dark1`/`Dark2`/`Dark3` colours as `<Color>` resources (`#FFD700`), `SystemControlForegroundAccentBrush`
(`#FFD700`), the two accent
reveal brushes (`#FFD700`), the shipped-control alias keys from §3.6 (`#FFD700` fills/tracks and `#000000`
glyphs/text), `SystemControlHighlightAltChromeWhiteBrush` (`#000000`),
`SystemControlHighlightChromeWhiteBrush` (`#000000`), and `SystemControlForegroundChromeWhiteBrush`
(`#000000`). When high contrast is enabled, the applier removes every direct accent-overlay resource in §3.6,
so a selected accent cannot leak into the high-contrast values.

### 3.6 Accent propagation

The accent setting is a runtime overlay, not a second palette. The palette in §3.1–§3.5 is the default/`blue`
baseline and the high-contrast source. There are three overlay groups:

1. **Base accent overlay** — the base accent roles and base Fluent keys. Published for an optional accent;
   removed for `null`, `blue`, and high contrast.
2. **Derived Fluent accent-state overlay** — the Fluent 12.1.6 platform-derived accent shades and the
   accent-control glyph/state keys. Published for every non-high-contrast accent, including `null` and `blue`,
   because Fluent derives these from the platform accent rather than from the palette. Removed for high
   contrast.
3. **Fluent alias overlay for shipped controls** — the `CheckBoxCheck*`, `Slider*`, and `TreeViewItem*` alias
   keys consumed by the shipped Settings/Validation check boxes, Settings slider, and Projects tree. Fluent
   defines these as `StaticResource` aliases that capture the base brush objects at theme load, so base-key
   overlays alone do not reach the realized template parts. Published for every non-high-contrast accent,
   including `null` and `blue`; removed for high contrast, where `HighContrast.axaml` defines the same alias
   keys.

`SystemAccentColor` is always set for a non-high-contrast accent (the raw accent); high contrast sets it to
`#FFD700`. Density never changes an accent role.

**Accent values.** `null` and `blue` are `#007ACC`. The five optional keys keep their values: `teal #008B8B`,
`green #2E7D32`, `amber #B26A00`, `violet #6A4C93`, `rose #B0004F`. The hex values do not change. The
contrast-safe values below are derived role values, not new accent-palette entries.

**Derivation.** `C(a, b)` is the WCAG contrast ratio of colours `a` and `b`. `worstSurface(dark)` is
`#3C3C3C` (the input fill and the brightest dark surface; it is the worst case for the focus/indicator and
input-focus floors). `worstSurface(light)` is `#F3F3F3` (the darkest light surface).

- `OnAccent(a)` is `#FFFFFF` when `C(#FFFFFF, a) >= 4.5`; otherwise `#000000`. The white-first rule preserves
  the approved `#007ACC` white foreground and makes explicit `blue` identical to the default.
- `AccentText(a, theme)` is `#4DA6FF` (dark) / `#005A9E` (light) for `null`/`blue`. For an optional accent it is
  `a` when `C(a, worstSurface(theme)) >= 4.5`; otherwise it is the first 1% sRGB blend of `a` toward `#FFFFFF`
  (dark) or `#000000` (light) that reaches 4.5.
- `FocusAccent(a, theme)` is `a` when `C(a, worstSurface(theme)) >= 3.0`; otherwise it is the first 1% sRGB
  blend toward `#FFFFFF` (dark) or `#000000` (light) that reaches 3.0. This is the contrast-safe variant for
  focus rings, 2 px indicators, text-input focus, and Fluent accent controls. It is not the accent fill.
- `SelectedFill(a, theme)` is `#094771` (dark) / `#CCE5FF` (light) for `null`/`blue`. For an optional accent it
  is the channel-wise sRGB blend `round(0.30 * a + 0.70 * chrome(theme))`, with `chrome(dark) = #1E1E1E` and
  `chrome(light) = #F3F3F3`.
- `SelectedText(fill, theme)` is `ForegroundStrongBrush` for the theme when its contrast on `fill` is at least
  4.5; otherwise its opposite (`#000000` in dark, `#FFFFFF` in light).
- `AccentGlyph` is `#FFFFFF`. A check, toggle, or radio glyph on an accent fill must keep at least 3:1 against
  that fill. Every `FocusAccent` value in the table below passes; if a future accent does not, stop and ask
  before changing the glyph rule.
- `FluentState(step, a, theme)` starts from `FocusAccent` and applies the Fluent 12.1.6 HSL offset: `Dark1`
  −28.5/255, `Dark2` −49/255, `Dark3` −74.5/255, `Light1` +39/255, `Light2` +70/255, `Light3` +103/255,
  clamping lightness to [0, 1]. If the result is below 3:1 against `worstSurface(theme)` or against
  `AccentGlyph`, reduce the offset magnitude by 1/255 until both pass; zero is the base. The clamp can make a
  state equal the base when the base already sits on the floor; that is deliberate and is safer than a
  platform accent or a sub-3:1 state. Fluent 12.1.6 consumes `Light1` and `Dark1`; `Light2`/`Light3`/`Dark2`/
  `Dark3` are overlaid for completeness and have no consumer in the 12.1.6 control resources.

The 1% blend rule is deterministic: for `t = 1, 2, …, 100`, `channel = round(a + (target - a) * t / 100)`, and
the first `t` that meets the floor is used. All channel rounding in the accent derivations, including the
30/70 selection blend, uses half away from zero (`MidpointRounding.AwayFromZero`). Tests assert the resolved
values in the tables below, not the intermediate search.

**Base accent overlay.** For an optional accent, the applier writes these direct resources. For `null`,
`blue`, and high contrast it removes them, so the palette or the high-contrast dictionary wins. The default
palette's dark `SystemControlHighlightAccentBrush` and `TextControlBorderBrushFocused` are the default
`FocusAccent` `#248DD3` (light `#007ACC`), so the default focus/input-focus ring passes the 3:1 floor on
`#3C3C3C` without the optional-accent overlay.

| Resource | Type | Value | Consumed by |
| --- | --- | --- | --- |
| `Mocassin.AccentBrush` | brush | raw accent | `Button.accent`, accent fills, decorative splitter line |
| `Mocassin.AccentTextBrush` | brush | `AccentText` | accent-coloured text and links (resource-only today; see below) |
| `Mocassin.OnAccentBrush` | brush | `OnAccent(raw)` | text/glyphs on `AccentBrush` and accent buttons |
| `Mocassin.FocusBrush` | brush | `FocusAccent` | explicit focus adorners, focus borders, selected-pipe indicator |
| `Mocassin.StatusBarBackground` | brush | raw accent | status bar |
| `Mocassin.StatusBarForeground` | brush | `OnAccent(raw)` | status text, glyphs, progress |
| `Mocassin.SelectedBackground` | brush | `SelectedFill` | selected row/tree/combo role |
| `Mocassin.SelectedForeground` | brush | `SelectedText` | text on `SelectedBackground` |
| `SystemControlHighlightAccentBrush` | brush | `FocusAccent` | Fluent focus and accent-control base |
| `SystemControlHighlightListAccentLowBrush` | brush | `SelectedFill` | Fluent list/table selected fill |
| `SystemControlHighlightListAccentMediumBrush` | brush | `SelectedFill` | Fluent list/table selected fill |
| `SystemControlHighlightListAccentHighBrush` | brush | `SelectedFill` | Fluent list/table selected fill |
| `AccentButtonBackground` | brush | raw accent | Fluent accent button |
| `AccentButtonBackgroundPointerOver` | brush | raw accent | Fluent accent button |
| `AccentButtonBackgroundPressed` | brush | raw accent | Fluent accent button |
| `AccentButtonBorderBrush` | brush | raw accent | Fluent accent button |
| `AccentButtonBorderBrushPointerOver` | brush | raw accent | Fluent accent button |
| `AccentButtonBorderBrushPressed` | brush | raw accent | Fluent accent button |
| `AccentButtonForeground` | brush | `OnAccent(raw)` | Fluent accent button |
| `AccentButtonForegroundPointerOver` | brush | `OnAccent(raw)` | Fluent accent button |
| `AccentButtonForegroundPressed` | brush | `OnAccent(raw)` | Fluent accent button |
| `TextControlBorderBrushFocused` | brush | `FocusAccent` | text-input focus border |
| `TextControlSelectionHighlightColor` | brush (the palette type) | raw accent | TextBox selection fallback |
| `ComboBoxItemBackgroundSelected` | brush | `SelectedFill` | combo selected item |
| `ComboBoxItemBackgroundSelectedPointerOver` | brush | `SelectedFill` | combo selected item |
| `ComboBoxItemBackgroundSelectedPressed` | brush | `SelectedFill` | combo selected item |
| `ComboBoxItemForegroundSelected` | brush | `SelectedText` | combo selected item |
| `ComboBoxItemForegroundSelectedPointerOver` | brush | `SelectedText` | combo selected item |
| `ComboBoxItemForegroundSelectedPressed` | brush | `SelectedText` | combo selected item |
| `TabItemHeaderSelectedPipeFill` | brush | `FocusAccent` | document/console selected-tab pipe |

`SystemControlHighlightAltBaseHighBrush` is deliberately **not** in either overlay. It is the shared
selected/hover text role described below.

**Derived Fluent accent-state overlay.** For every non-high-contrast accent, including `null` and `blue`, the
applier writes these direct resources because Fluent's own values come from the platform accent. High contrast
removes them and the high-contrast dictionary supplies the values.

| Resource | Type | Value | Consumer in Fluent 12.1.6 |
| --- | --- | --- | --- |
| `SystemAccentColorLight1` | `Color` | `FluentState(Light1)` | checked check-box/radio/toggle hover, slider-thumb hover |
| `SystemAccentColorLight2` | `Color` | `FluentState(Light2)` | no 12.1.6 consumer; overlaid for completeness |
| `SystemAccentColorLight3` | `Color` | `FluentState(Light3)` | no 12.1.6 consumer; overlaid for completeness |
| `SystemAccentColorDark1` | `Color` | `FluentState(Dark1)` | checked check-box/radio/toggle pressed, slider-thumb pressed |
| `SystemAccentColorDark2` | `Color` | `FluentState(Dark2)` | no 12.1.6 consumer; overlaid for completeness |
| `SystemAccentColorDark3` | `Color` | `FluentState(Dark3)` | no 12.1.6 consumer; overlaid for completeness |
| `SystemControlForegroundAccentBrush` | brush | `FocusAccent` | slider thumb |
| `SystemControlHighlightAccent2RevealBackgroundBrush` | brush | `SelectedFill` | tree selected pointer-over |
| `SystemControlHighlightAccent3RevealBackgroundBrush` | brush | `SelectedFill` | tree selected normal |
| `SystemControlHighlightAltChromeWhiteBrush` | brush | `AccentGlyph` (`#FFFFFF`) | check/toggle/radio glyph and toggle-button checked foreground |
| `SystemControlForegroundChromeWhiteBrush` | brush | `AccentGlyph` (`#FFFFFF`) | check-box/radio checked hover, pressed, and indeterminate glyph |

**Fluent alias overlay for shipped controls.** Fluent 12.1.6 resolves the control template parts through these
alias keys. A direct application resource with the alias key replaces the captured brush and reaches the
realized part. The overlay is published for every non-high-contrast accent, including `null` and `blue`; high
contrast removes it and `HighContrast.axaml` supplies the same keys.

| Alias | Value | Realized part |
| --- | --- | --- |
| `CheckBoxCheckBackgroundFillChecked` | `FocusAccent` | checked `CheckBox` `NormalRectangle` background/border |
| `CheckBoxCheckBackgroundFillIndeterminate` | `FocusAccent` | indeterminate fill |
| `CheckBoxCheckBackgroundStrokeIndeterminate` | `FocusAccent` | indeterminate stroke |
| `CheckBoxCheckGlyphForegroundChecked` | `AccentGlyph` | checked `CheckGlyph.Fill` |
| `CheckBoxCheckGlyphForegroundCheckedPointerOver` | `AccentGlyph` | checked hover glyph |
| `CheckBoxCheckGlyphForegroundCheckedPressed` | `AccentGlyph` | checked press glyph |
| `CheckBoxCheckGlyphForegroundIndeterminate` | `AccentGlyph` | indeterminate glyph |
| `CheckBoxCheckGlyphForegroundIndeterminatePointerOver` | `AccentGlyph` | indeterminate hover glyph |
| `CheckBoxCheckGlyphForegroundIndeterminatePressed` | `AccentGlyph` | indeterminate press glyph |
| `SliderThumbBackground` | `FocusAccent` | `Thumb.Background` |
| `SliderTrackValueFill` | `FocusAccent` | slider value-track fill |
| `SliderTrackValueFillPointerOver` | `FocusAccent` | slider value-track hover |
| `SliderTrackValueFillPressed` | `FocusAccent` | slider value-track press |
| `TreeViewItemBackgroundSelected` | `SelectedFill` | selected `PART_LayoutRoot.Background` |
| `TreeViewItemBackgroundSelectedPointerOver` | `SelectedFill` | selected hover background |
| `TreeViewItemBackgroundSelectedPressed` | `SelectedFill` | selected press background |
| `TreeViewItemForegroundSelected` | `SelectedText` | selected `PART_HeaderPresenter.Foreground` |
| `TreeViewItemForegroundSelectedPointerOver` | `SelectedText` | selected hover text |
| `TreeViewItemForegroundSelectedPressed` | `SelectedText` | selected press text |

The hover/press aliases that Fluent defines as `SolidColorBrush` objects with `Color="{DynamicResource
SystemAccentColorLight1}"` / `Dark1` follow the derived Colour overlay and do not need a separate alias resource.
The `ComboBoxItem*` alias keys are already the keys named in the base overlay; they reach the ComboBox realized
parts through the same direct-resource replacement.

**Shared selected and hover text.** `SystemControlHighlightAltBaseHighBrush` is the ListBoxItem,
TreeViewItem, ComboBox focused/selected, MenuFlyout, and TabItem selected/hover text role. It is **not** an
accent-control glyph key and is not part of either overlay. The palette uses `ForegroundStrongBrush`:
`#FFFFFF` dark, `#1F1F1F` light (the light value is corrected from the old `#FFFFFF`), and high contrast uses
`#000000`. This value meets 4.5:1 on every `SelectedFill` in the tables below and on the neutral
`HoverBackground`/`PressedBackground` fills. Accent-control glyphs use the separate
`SystemControlHighlightAltChromeWhiteBrush`/`SystemControlForegroundChromeWhiteBrush` keys above.

**Optional-accent values.** The tables are the test oracle. `null`/`blue` use the palette values in §3.2/§3.3.
The dark `SelectedText` is `#FFFFFF` for every row; the light `SelectedText` is `#1F1F1F` for every row.

| Accent | `OnAccent(raw)` | `AccentText` dark | `AccentText` light | `FocusAccent` dark | `FocusAccent` light | `SelectedFill` dark | `SelectedFill` light |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `null` / `blue` | `#FFFFFF` | `#4DA6FF` | `#005A9E` | `#248DD3` | `#007ACC` | `#094771` | `#CCE5FF` |
| `teal` | `#000000` | `#36A3A3` | `#007C7C` | `#179595` | `#008B8B` | `#153F3F` | `#AAD4D4` |
| `green` | `#FFFFFF` | `#69A16B` | `#2E7D32` | `#549457` | `#2E7D32` | `#233B24` | `#B8D0B9` |
| `amber` | `#000000` | `#C28936` | `#A05F00` | `#B97717` | `#B26A00` | `#4A3515` | `#E0CAAA` |
| `violet` | `#FFFFFF` | `#A08CBA` | `#6A4C93` | `#927CB0` | `#6A4C93` | `#352C41` | `#CAC1D6` |
| `rose` | `#FFFFFF` | `#D578A2` | `#B0004F` | `#CE6192` | `#B0004F` | `#4A152D` | `#DFAAC2` |

`SelectedText` is derived from each fill as above; all rows meet 4.5:1. For light `rose`, the 30% fill
`#DFAAC2` now passes with `#1F1F1F`; white is no longer needed.

**Derived Fluent state values (consumed keys).** `Dark1` and `Light1` are the only derived colours consumed
by the 12.1.6 control resources. `FluentState` is applied to the theme's `FocusAccent` and clamped as above.

| Accent | Dark theme `Dark1` | Dark theme `Light1` | Light theme `Dark1` | Light theme `Light1` |
| --- | --- | --- | --- | --- |
| `null` / `blue` | `#248CD1` | `#3A9CDD` | `#005994` | `#0090F0` |
| `teal` | `#179595` | `#19A5A5` | `#005353` | `#009B9B` |
| `green` | `#549457` | `#60A463` | `#1F5422` | `#3BA040` |
| `amber` | `#B97717` | `#CE851A` | `#7A4900` | `#CC7900` |
| `violet` | `#927CB0` | `#9F8CBA` | `#4F396E` | `#9175B8` |
| `rose` | `#CE6192` | `#D576A1` | `#780036` | `#FE0072` |

**Live reapply and restoration.** `StartupCoordinator` calls `Apply` for the initial settings and on every
settings change. It also subscribes to `Application.Current.ActualThemeVariantChanged` and calls
`Apply(settingsService.Current)` when the actual variant changes, so a live OS Light/Dark switch recomputes the
overlay. The derivation theme is the **actual** resolved variant: after setting `RequestedThemeVariant`, the
applier reads `Application.ActualThemeVariant`; `Dark` selects the dark column, and `Light` (including the
`Default` fallback) selects the light column. `ThemeSelection.System` therefore follows the OS variant rather
than a fixed light fallback. The test binds a controllable `IPlatformSettings` before application setup, flips
its `ThemeVariant`, raises `ColorValuesChanged`, and asserts that the production `ActualThemeVariantChanged`
path recomputes all three groups; there is no test seam that bypasses the production read.

The applier removes all three overlay groups before writing a new set. Switching between optional accents
replaces all three groups. Switching to `null`/`blue` removes the base group but keeps the derived Fluent state
and alias groups, so hover/pressed Fluent states and the shipped check box/slider/tree parts stay on the app
accent rather than the platform accent. Switching to high contrast removes all three groups and the
high-contrast dictionary supplies every base, derived, alias, and glyph key. A live actual-variant switch with
the same accent recomputes all three groups.

**Accent text is resource-only.** Under owner decision A3, `Mocassin.AccentTextBrush` has no current view
consumer. The overlay and its contrast test prove the token and the derived value; they do not prove a rendered
link or accent-text surface, and the screenshot set must not claim one. Adding a link surface is deferred.

## 4. Typography

| Token | Comfortable | Compact | Use |
| --- | --- | --- | --- |
| `Mocassin.DisplayFontSize` | `28 × scale` | `28 × scale` | welcome title |
| `Mocassin.TitleFontSize` | `20 × scale` | `20 × scale` | settings and dialog titles |
| `Mocassin.SubtitleFontSize` | `16 × scale` | `16 × scale` | welcome subtitle, dialog subtitle |
| `Mocassin.BodyFontSize` | `14 × scale` | `14 × scale` | body, controls, menus |
| `Mocassin.CaptionFontSize` | `12 × scale` | `12 × scale` | captions, hints, path text, status bar |

- Font family is the application default (Inter with the platform fallback). No font is shipped or downloaded.
- Body line height is `20 × scale`; caption line height is `16 × scale`; titles use the font's natural line
  height.
- Weights are `Normal` and `SemiBold` only. A heading is `SemiBold` and uses `ForegroundStrongBrush`. Captions
  use `ForegroundMutedBrush`.
- Font scale applies to every token above, including captions. A view must not hard-code `12`, `14`, `16`, `20`,
  or `28` for text size.
- Text wrapping and trimming behaviour is unchanged from the current views. Path text ellipsises; dialog and
  banner text wraps.

## 5. Spacing, shape, and density

| Token | Comfortable | Compact | Use |
| --- | --- | --- | --- |
| `Mocassin.RegionGap` | `6` | `4` | gap and splitter track between cards |
| `Mocassin.RegionPadding` | `12` | `8` | region content inset |
| `Mocassin.RegionHeaderPadding` | `12,8` | `8,6` | region header inset |
| `Mocassin.CardPadding` | `16` | `12` | palette, settings, dialog, notification inset |
| `Mocassin.ControlPadding` | `8` | `4` | button and input padding |
| `Mocassin.ControlMinHeight` | `34` | `26` | button and input minimum height |
| `Mocassin.ItemPadding` | `4,2` | `2,1` | list, tree, and combo item padding |
| `Mocassin.CompactButtonPadding` | `8,2` | `6,2` | dense row buttons such as recent-project Open/Remove |
| `Mocassin.CompactButtonMinHeight` | `26` | `22` | dense row buttons |
| `Mocassin.TabHeight` | `32` | `28` | document and console tab strip height |
| `Mocassin.MenuHeight` | `32` | `28` | menu bar height |
| `Mocassin.StatusBarHeight` | `24` | `22` | status bar height |
| `Mocassin.RadiusCard` | `6` | `6` | region cards, modals, notifications |
| `Mocassin.RadiusControl` | `4` | `4` | buttons, inputs, combo boxes |
| `Mocassin.RadiusTab` | `4` | `4` | top corners of tabs |
| `Mocassin.BorderThickness` | `1` | `1` | card and region border |
| `Mocassin.ControlBorderThickness` | `1` | `1` | control border |
| `Mocassin.FocusThickness` | `2` | `2` | focus ring |

- Spacing follows a 4 px grid. The values above are the only permitted exceptions.
- There is no elevation shadow. Separation comes from the chrome gap, the 1 px border, and the card radius.
- The 6 px gap is the splitter track. The splitter remains a 6 px hit target; the gap is not padding on the
  cards, so the regions keep their full fractions and minima.
- High contrast sets card and control border thickness to `2`; it does not change radius, gap, or padding except
  through density.

## 6. Interaction states

| State | Rule |
| --- | --- |
| Normal | `CardBackground` or `InputBackground`; `BorderBrush`; `ForegroundBrush`. |
| Hover | `HoverBackground`; the border stays `BorderBrush`; text stays `ForegroundBrush`. |
| Pressed | `PressedBackground`; no scale transform; text stays `ForegroundBrush`. |
| Selected | `SelectedBackground` and `SelectedForeground` for list, tree, and combo rows; both follow the selected accent per §3.6. The active tab uses `TabActiveBackground`, `TabActiveForeground`, and a 2 px `FocusBrush` indicator. |
| Focus | A visible 2 px `FocusBrush` ring, inside or outside the control, with at least 3:1 contrast against the adjacent surface. Focus is never removed for a keyboard-reachable control. |
| Disabled | `DisabledBackground` and `DisabledForeground`; the control stays legible. Opacity alone is not a disabled treatment. |
| Dirty/unsaved | The existing `*` marker plus its accessible name; no new colour-only signal. |

Focus indicators for splitters keep the existing adorner but use `FocusBrush` and `FocusThickness`. Fluent
12.1.6's default focus adorner uses the neutral `SystemControlFocusVisualPrimaryBrush` /
`SystemControlFocusVisualSecondaryBrush` pair, not `SystemControlHighlightAccentBrush`; the neutral ring is
accepted and must meet 3:1 against the adjacent surface. It is deliberately not accent-coloured. Controls with
an explicit accent focus border or adorner (text inputs and splitters) use `FocusBrush`. A control without a
visible focus ring gets an explicit `FocusAdorner` from `ShellStyles.axaml`. Replacing a control template to
obtain focus is not acceptable in this pass.

`Enter`, `Escape`, Tab order, and layer modality do not change. In particular, a `TabControl` selects with
arrow keys or a click and ignores plain `Enter`, as specified in
[UI interaction contract §5.2](ui-interaction-contract.md#52-mapping-by-control-class).

## 7. Shell surfaces

### 7.1 Backdrop and region cards

```text
┌ chrome ────────────────────────────────────────────────┐
│ menu bar                                               │
│ ┌ projects ┐ ┌ documents ──────────────┐ ┌ inspector ┐ │
│ │  card    │ │  card                   │ │  card     │ │
│ │          │ ├─────────────────────────┤ │           │ │
│ │          │ │  console card           │ │           │ │
│ └──────────┘ └─────────────────────────┘ └───────────┘ │
│ status bar                                             │
└────────────────────────────────────────────────────────┘
```

- The window and the workspace gaps use `ChromeBackground`.
- Each region is a `Border` with `WorkspaceBackground`, `BorderBrush`, `BorderThickness`, and
  `RadiusCard`. The card owns the region header and content.
- The workspace grid keeps star tracks, the existing minima, and the 6 px splitter tracks. Card margins are
  zero; the splitter gap is the visual gap. The workspace itself is inset by `RegionGap` from the window chrome
  so the outer card radius is visible; this inset is chrome and does not change the fraction model.
- The splitter is transparent at rest. Hover shows a 1 px `AccentBrush` line; focus shows the 2 px
  `FocusBrush` ring.
- The documents card contains the tab strip and the active document. The document surface uses
  `WorkspaceBackground`; the tab strip uses `ChromeBackground`.
- The console card contains the console tab strip and the active channel. The former separate "Console" title
  is removed; the tab strip is the header.
- The inspector and projects cards use the same header rhythm: `RegionHeaderPadding`, `SemiBold` heading, then
  the content inset.

### 7.2 Menu bar

- Height `MenuHeight`; background `MenuBackground`; text `ForegroundBrush`; body size.
- Top-level items use `HoverBackground` on pointer-over and `CardBackground` while their menu is open.
- The menu bar has no bottom border; the chrome backdrop and the card borders separate it from the workspace.
- Dropdowns use `MenuFlyoutPresenterBackground` and `MenuFlyoutPresenterBorderBrush` with
  `RadiusCard` and 1 px border. Menu item hover/pressed use the state tokens. Custom menu templates are
  deferred.

### 7.3 Status bar

- Height `StatusBarHeight`; background `StatusBarBackground`; all text, glyphs, and progress use
  `StatusBarForeground`.
- The default/`blue` background is `#007ACC` with a white foreground. Under an optional accent, the background
  is the selected accent and the foreground is its contrast-safe `OnAccent` value: white for `green`, `violet`,
  and `rose`; black for `teal` and `amber`. Text and progress stay at or above 4.5:1.
- Text is caption size. The application name is `SemiBold`; the remaining items use normal weight.
- The progress bar sits on the right, 140 px wide and 4 px high, with `StatusBarForeground` on the transparent
  track. Its foreground/background pair meets 4.5:1.
- The status bar has no border; the chrome backdrop and the card borders separate it from the workspace.

### 7.4 Document tab strip

- Height `TabHeight`; background `TabStripBackground`; tabs use `RadiusTab` on the top corners only.
- Inactive tab: `TabInactiveBackground`, `TabInactiveForeground`. Active tab: `TabActiveBackground`,
  `TabActiveForeground`, plus a 2 px `FocusBrush` indicator along the top edge. The existing dirty
  `*` and close button remain.
- Tabs keep their current order, close button, context menu, and keyboard shortcuts. Drag-reorder remains
  deferred.
- The tab strip scrolls horizontally; it never wraps to a second row.

### 7.5 Console tab strip

See §8. The console tab strip uses the same tab tokens as the document strip so the two surfaces read as one
system.

### 7.6 Regions

- Projects: the project switcher list, "Explorer" heading, tree, and pinned "Open selected" button keep their
  structure. Rows use `ItemPadding`; selected rows use `SelectedBackground`/`SelectedForeground`; captions use
  `ForegroundMutedBrush`.
- Documents: see §7.4. The welcome document is inside this card.
- Inspector: the heading, empty state, and summary use the card inset. The empty state is muted; the summary is
  body text.
- Console: see §8.

### 7.7 Read-only banner

The read-only banner is a full-width strip between the menu and the workspace. It uses
`WarningBackground`, `WarningBorderBrush`, `WarningForeground`, and a 1 px bottom border. It is not a card and
does not get a radius. Inline field and validation errors use `ErrorForeground`; an error or success banner uses
the matching background, border, and foreground tokens.

### 7.8 Layout defaults and reset

The splitter behaviour, fraction model, minima, and keyboard rules remain normative in
[UI interaction contract §6](ui-interaction-contract.md#6-shell-layout). This section records the visual
refresh's default values and the one-time migration; the interaction contract's numeric defaults were updated
to these values on 2026-11-21.

| Value | Default | Nominal at 1200 × 800 | Nominal at 900 × 600 | Persisted range |
| --- | --- | --- | --- | --- |
| `LeftFraction` | `0.20` | 240 px | 180 px | 0.08–0.45 |
| `RightFraction` | `0.22` | 264 px | 198 px | 0.08–0.45 |
| centre | remainder | 684 px after gaps | 510 px after gaps | existing minimum |
| `ConsoleFraction` | `0.32` | 256 px | 192 px | 0.08–0.60 |
| documents | remainder | 544 px | 408 px | existing minimum |

The nominal pixels are shares of the workspace extent before the outer `RegionGap` inset. The existing minima
(160 / 160 / 360 / 80 / 200 and the 900 × 600 window minimum) and the 6 px splitter tracks are unchanged.

One additive application-local layout field, `layout.defaultsRevision`, makes the change once:

1. Read the layout as today, including the existing pixel-to-fraction migration. That migration runs first and
   is unchanged.
2. An absent revision is `0`. A non-integer or negative value is a settings warning and falls back to `0`.
3. If the revision is below `1` and all three post-read fractions equal the old defaults within `1e-6`
   (`260/1200`, `320/1200`, `180/800`), replace all three with the new defaults. This is the never-dragged case.
4. If any fraction differs from the old defaults, keep the user's fractions. The migration never overrides a
   custom layout silently. This is the deliberate trade-off: the never-dragged saved layouts get the new
   proportions, while a layout the user changed is treated as intent and preserved.
5. Return `defaultsRevision = 1`; the writer emits it. A later load sees the marker and never auto-adjusts
   again. A second load is idempotent.
6. No schema bump: the field is additive, application-local, and not in `contracts/manifest.json`, like the
   existing `autosave` and `session` blocks.

**View → Reset layout** restores the three fractions to these defaults, makes all regions visible, writes the
current revision, and persists. It does not change the theme, density, recent files, or active console tab. A
user can re-drag at any time; the drag persistence path is unchanged.

## 8. Console tab model

The console region has exactly two fixed tabs, in this order:

| Position | Visible label | Internal channel id | Content |
| --- | --- | --- | --- |
| 0 | `Console` | `application` | the existing application console |
| 1 | `Validation` | `problems` | the existing Problems view |

- The view is a `TabControl` with `TabStripPlacement="Top"` and accessible name `Console tabs`. It binds
  `ItemsSource` to the channel collection and `SelectedItem` to the active channel.
- `ConsoleChannelViewModel` gains a presentation-only tab title. The application channel uses `Console`; the
  validation channel uses `Validation`. The stable `Id` and the content heading remain `application` and
  `problems` internally so the settings key, type names, and existing navigation are unchanged.
- User-facing labels and automation names in the validation content use `Validation` (for example
  `Validation text filter` and `Validation entries`). Internal type names (`ProblemsView`,
  `ProblemsChannelViewModel`) and the persisted id `problems` do not change. This avoids a mixed
  "Validation tab, Problems filter" announcement.
- The active tab is persisted in `layout.activeConsoleChannel` using the existing ids. An unknown id falls back
  to the first tab. Switching tabs schedules the existing debounced layout persist, not only the next splitter
  gesture or settings save.
- There is no close button, no reorder, no overflow menu, and no per-tab context menu.
- Keyboard: arrow keys move the selection between the two tabs; click selects; `Tab` leaves the strip; plain
  `Enter` is a no-op, consistent with the interaction contract. `Escape` is unaffected and never closes the
  console.
- The old `ComboBox` labelled "Console channel" is removed. Its focus stop and automation name disappear; the
  console filter field, clear button, and validation filters keep their existing names and tab order relative
  to each other. No overlay or dialog captures or restores the removed combo box.
- The console card has no separate title row. The tab strip is the header, with `RegionHeaderPadding` applied
  to the strip.

## 9. Welcome page

- The welcome content is centred in the documents card with a maximum width of 520 px and `16` px spacing
  between blocks. It has no extra card behind it; the documents card is the container.
- The title uses `DisplayFontSize`, `SemiBold`, `ForegroundStrongBrush`. The subtitle uses
  `SubtitleFontSize`, `ForegroundMutedBrush`. The body uses `BodyFontSize`, `ForegroundBrush`, centred and
  wrapped.
- "New project", "Open project", and "Keyboard shortcuts" use the default button style. Their labels are
  horizontally and vertically centred by the style, not by per-button padding.
- Recent-project rows use a `Grid` with the path, an optional "Missing" marker in `ErrorForeground`, and the
  `Open` and `Remove` buttons. Those two buttons use `CompactButtonPadding`, `CompactButtonMinHeight`, and the
  same explicit content alignment as the default style. The local `Padding="8,2"` override is removed.
- The previous defect is a metrics mismatch: the global button style owns `MinHeight` and padding, while the
  recent buttons override only `Padding` and do not declare content alignment. The fix is one secondary/compact
  button style that owns padding, minimum height, and both content alignments, so the label's centre matches
  the button's centre in both themes and both densities. The main buttons use the same alignment rule.
- The recent list is a compact list, not a table; paths ellipsise and the row action buttons stay on one line.

## 10. Palette, settings, dialogs, and notifications

| Surface | Rule |
| --- | --- |
| Command palette | `OverlayScrim` behind a `CardBackground` card with `RadiusCard`, 1 px border, `CardPadding`, 620 px width, and the existing search/list structure. The selected result uses `SelectedBackground`/`SelectedForeground`. |
| Shortcut cheat sheet | Same card chrome; title uses `TitleFontSize`; groups use `SemiBold` headings and caption gestures. |
| Settings | Same card chrome; 680 px width; `CardPadding`; section spacing on the 4 px grid. Labels use `ForegroundBrush`; captions use `ForegroundMutedBrush`; the recent-file list uses caption size. |
| Dialogs | `DialogHost` uses the same card chrome, `CardPadding`, and scrim. Dialog titles use `TitleFontSize`; messages use body size. The default action uses the accent button style; a destructive confirm uses the error outline style and is never the initial focus. |
| Notifications | `CardBackground`, `BorderBrush`, `RadiusCard`, 1 px border, `CardPadding`, right-bottom margin 16, max width 380. The close button is a compact control. Severity icons or left strips are deferred. |
| Error and status banners | Use the matching background, border, and foreground tokens. The old hard-coded light-only banner colours are removed. |

Overlay layout, focus, modality, `Escape`, and `Enter` remain governed by the interaction contract. This section
changes only colour, border, radius, padding, and type.

## 11. Accessibility floor

### 11.1 Contrast

The following minimum ratios are measured on the final composited colours. Normal text is 4.5:1; large text is
3:1; focus rings and other meaningful non-text boundaries are 3:1.

| Pair | Dark | Light | Minimum |
| --- | --- | --- | --- |
| Body text on chrome, workspace, and card | `#D4D4D4` on `#1E1E1E` / `#252526` / `#2D2D30` | `#333333` on `#F3F3F3` / `#FFFFFF` / `#F3F3F3` | 4.5:1 |
| Strong text on card | `#FFFFFF` on `#2D2D30` | `#1F1F1F` on `#F3F3F3` | 4.5:1 |
| Muted text on chrome, workspace, and card | `#9D9D9D` on the three surfaces | `#6F6F6F` on the three surfaces | 4.5:1 |
| Accent text on chrome, workspace, and card | default `#4DA6FF`; optional `AccentText` per §3.6 | default `#005A9E`; optional `AccentText` per §3.6 | 4.5:1 |
| On-accent text on accent fill (accent button, status bar) | `OnAccent(raw)`: white for blue/green/violet/rose, black for teal/amber | the same values | 4.5:1 |
| Selected and hover text (`SystemControlHighlightAltBaseHighBrush`) on selected/hover fills | `#FFFFFF` | `#1F1F1F` | 4.5:1 |
| On-accent glyphs on Fluent accent controls and derived states | `AccentGlyph` `#FFFFFF` on `FocusAccent` and `FluentState` | `AccentGlyph` `#FFFFFF` on `FocusAccent` and `FluentState` | 3:1 |
| Selected-row text on selected fill | default `#FFFFFF` on `#094771`; optional `SelectedText` on `SelectedFill` per §3.6 | default `#1F1F1F` on `#CCE5FF`; optional `SelectedText` on `SelectedFill` per §3.6 | 4.5:1 |
| Status-bar text, glyphs, and progress on status bar | `OnAccent(raw)` on raw accent | `OnAccent(raw)` on raw accent | 4.5:1 |
| Text selection pair (`SelectionBrush`/`SelectionForegroundBrush`) | `OnAccent(raw)` on raw accent | `OnAccent(raw)` on raw accent | 4.5:1 |
| Error, warning, success, and info text on card | the dark semantic tokens | the light semantic tokens | 4.5:1 |
| Focus ring, selected-pipe indicator, and Fluent state fill against adjacent surfaces | default `#248DD3`; optional `FocusAccent`/`FluentState` per §3.6 on `#3C3C3C` (worst dark) | default `#007ACC`; optional `FocusAccent`/`FluentState` per §3.6 on `#F3F3F3` (worst light) | 3:1 |
| Fluent default neutral focus ring (`SystemControlFocusVisualPrimaryBrush`/`SecondaryBrush`) against adjacent surfaces | the neutral Fluent pair | the neutral Fluent pair | 3:1 |

High contrast pairs are `#FFFFFF` on `#000000` (21:1), `#000000` on `#FFD700` (about 15:1), and `#FFD700`
against `#000000` (about 15:1). They are tested separately.

### 11.2 Focus and keyboard

- Every keyboard-reachable control shows a visible focus ring. This includes buttons, inputs, combo boxes,
  list and tree rows, tabs, splitters, check boxes, sliders, and the document close button.
- Focus is not indicated by colour alone: the ring has thickness. Selected state is not indicated by colour
  alone: tabs have an indicator and list rows have a fill.
- The interaction contract's initial-focus, `Escape`, `Enter`, and layer rules are unchanged. A visual change
  must not add a new focus target or remove an existing one.
- Disabled controls are skipped by Tab and never receive focus.

### 11.3 Names and roles

- Region cards expose accessible names: `Projects panel`, `Documents panel`, `Inspector panel`, and
  `Console panel`.
- The document tab strip keeps `Document tabs`; the console strip is `Console tabs`. Each tab's accessible name
  is its visible header.
- The validation content uses `Validation` in user-facing automation names; the tab itself is `Validation`.
- Overlay roots keep `Command palette`, `Keyboard shortcuts`, and `Settings`; `DialogHost` keeps the active
  dialog title; the notification host is never announced as a dialog and never takes focus.
- Icon-only buttons, including the document close button and the notification close button, keep an
  `AutomationProperties.Name`.
- A new icon or glyph is not the only carrier of meaning. Status severity keeps its glyph and text; the dirty
  marker keeps `*` and its accessible name.

### 11.4 Density, scale, and motion

- At 1.5× font scale in compact density, no primary surface clips its label, tab header, status text, or
  dialog button. Text wraps or the surface scrolls; it never overlaps.
- Reduce motion disables the non-essential opacity transition. No state change depends on an animation
  completing.
- High contrast keeps the 2 px border and focus thickness and never falls back to a palette colour.

## 12. Provenance and boundary

- Visual Studio Code is a **visual reference only**: its workbench hierarchy of chrome, workspace, cards,
  borders, and one accent. The reference themes are the default **Dark+** and **Light+** role hierarchy as
  reviewed by the owner on 2026-11-21; the dark values are the owner-approved set and the light values are the
  Light+ equivalent defined in this page. The reference snapshot is that review, not a pinned VS Code build; a
  future VS Code release does not change this contract.
- No Visual Studio Code asset, icon, font, trademark, screenshot, theme file, or stylesheet content is copied
  into the repository. The implementation uses Avalonia Fluent 12.1.6 control templates and the token values
  above.
- No new dependency, font download, icon package, or third-party theme is introduced.
- The palette values are application tokens. They are not part of `contracts/manifest.json`, a project,
  package, results, or state format, and they have no cross-language fixtures.
- The high-contrast and compact dictionaries are first-party and remain the precedence overrides described in
  §2.

## 13. Verification

The detailed test plan lives in the internal maintenance record for the refresh. This page is the durable
requirement; the record supplies the implementation and evidence plan. At minimum:

| Check | Tier |
| --- | --- |
| Palette role values, light/dark resolution, and high-contrast precedence | PR-fast headless |
| WCAG contrast for the pairs in §11.1, both themes and all six accent keys | PR-fast unit |
| Accent propagation: all three §3.6 overlay groups, status bar, accent text, focus, tab pipe, selection, derived Fluent states, alias keys, default restoration, live switch, high-contrast precedence | PR-fast headless |
| Realized template-part brushes for the shipped CheckBox (`NormalRectangle`, `CheckGlyph`), Slider (`Thumb`, value track), and TreeView (`PART_LayoutRoot`, `PART_HeaderPresenter`) in optional, default/blue, and high-contrast modes | PR-fast headless |
| System theme actual Light and Dark variants through the production `ActualThemeVariantChanged` reapply; `#3C3C3C`/`#37373D` focus surfaces; neutral default focus ring | PR-fast headless |
| Region card radius/border/gap, tab labels/order, removed channel combo | PR-fast headless |
| Console tab persistence, unknown-id fallback, `Enter` no-op, focus order | PR-fast headless |
| Welcome button label centring at comfortable and compact density | PR-fast headless |
| 1.5× font scale with compact density: no clipping on primary surfaces | PR-fast headless |
| Screenshot baselines for light, dark, high contrast, console tabs, and settings | PR-medium |
| Screen-reader pass, 125 %/150 % display scaling, Windows/macOS keyboard pass | Manual; recorded as platform evidence |
