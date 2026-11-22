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
| Runtime settings | `AvaloniaThemeApplier` | theme variant, font scale, fixed `SystemAccentColor`, comfortable/compact spacing, border thickness, and motion |
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
  1. direct application resources written by `AvaloniaThemeApplier` (font scale, `SystemAccentColor`, runtime
     density, border thickness, motion);
  2. the high-contrast merged dictionary;
  3. the compact-density merged dictionary;
  4. the active light/dark palette theme dictionary;
  5. the stock Fluent theme.
- The accent is a **fixed family**, not a runtime selection. There is no user-facing accent setting and no
  per-accent overlay. The fixed base roles, the fixed Fluent derived colours, and the shipped-control alias
  keys live in the palette `Light`/`Dark` theme dictionaries (with `Default` mirroring `Light`).
  `HighContrast.axaml` overrides the same keys after the palette. The one direct runtime accent resource is
  `SystemAccentColor` (`#007ACC`, or `#FFD700` under high contrast), because Fluent's resource provider would
  otherwise supply the platform accent. Target styles for the selected `TreeViewItem` use the palette tokens;
  they are not a runtime accent overlay.
- Precedence for accent values: high-contrast merged dictionary over the palette theme dictionary; the
  palette over the stock Fluent theme; direct `SystemAccentColor` for the Fluent base provider. Density never
  changes an accent role. §3.6 is the complete accent model.

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

The accent family is fixed: `AccentBrush` is `#007ACC`; `OnAccentBrush` is white; `FocusBrush` is `#248DD3`
dark / `#007ACC` light; `AccentTextBrush` is `#4DA6FF` dark / `#005A9E` light. These are contrast-safe roles,
not selectable accents. A filled accent surface always uses `OnAccentBrush`; a focus ring or selected-pipe
indicator uses `FocusBrush`. The accent setting is removed; §3.6 defines the fixed values, palette delivery,
Fluent alias delivery, high-contrast overrides, and theme-switch behaviour.

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

These are the fixed values in both themes. `StatusBarBackground` is `#007ACC` with white
`StatusBarForeground`; the selected row/tree/combo roles use the existing `SelectedBackground` /
`SelectedForeground` palette values. There is no per-accent selection tint. The status bar is the one
full-accent chrome surface. Severity is conveyed by its existing glyph and text, not by colour: all status-bar
glyphs and text use `StatusBarForeground`. This deliberately replaces the old per-severity colours, which did
not meet contrast on a dark bar.

### 3.4 Fluent resource keys

The palette overrides these Fluent 12.1.6 keys so stock controls follow the palette without new templates.
These are the keys the palette is expected to override; adding a key is a contract change and a test update.

| Group | Keys |
| --- | --- |
| Global text and surfaces | `SystemControlForegroundBaseHighBrush`, `SystemControlForegroundBaseMediumBrush`, `SystemControlForegroundBaseMediumLowBrush`, `SystemControlForegroundBaseLowBrush`, `SystemControlForegroundChromeWhiteBrush`, `SystemControlBackgroundAltHighBrush`, `SystemControlBackgroundChromeMediumLowBrush`, `SystemControlTransientBackgroundBrush`, `SystemControlTransientBorderBrush`, `SystemControlDisabledBaseMediumLowBrush`, `SystemControlDisabledBaseLowBrush`, `SystemControlDisabledChromeDisabledLowBrush` |
| Accent and selection | `SystemAccentColorLight1`, `SystemAccentColorLight2`, `SystemAccentColorLight3`, `SystemAccentColorDark1`, `SystemAccentColorDark2`, `SystemAccentColorDark3`, `SystemControlHighlightAccentBrush`, `SystemControlHighlightAltBaseHighBrush`, `SystemControlForegroundAccentBrush`, `SystemControlHighlightAccent2RevealBackgroundBrush`, `SystemControlHighlightAccent3RevealBackgroundBrush`, `SystemControlHighlightAltChromeWhiteBrush`, `SystemControlHighlightChromeWhiteBrush`, `SystemControlFocusVisualPrimaryBrush`, `SystemControlFocusVisualSecondaryBrush`, `SystemControlHighlightListLowBrush`, `SystemControlHighlightListMediumBrush`, `SystemControlHighlightListAccentLowBrush`, `SystemControlHighlightListAccentMediumBrush`, `SystemControlHighlightListAccentHighBrush` |
| Button | `ButtonBackground`, `ButtonBackgroundPointerOver`, `ButtonBackgroundPressed`, `ButtonBackgroundDisabled`, `ButtonForeground`, `ButtonForegroundPointerOver`, `ButtonForegroundPressed`, `ButtonForegroundDisabled`, `ButtonBorderBrush`, `ButtonBorderBrushPointerOver`, `ButtonBorderBrushPressed`, `ButtonBorderBrushDisabled`, `ButtonBorderThemeThickness` |
| Accent button | `AccentButtonBackground`, `AccentButtonBackgroundPointerOver`, `AccentButtonBackgroundPressed`, `AccentButtonBackgroundDisabled`, `AccentButtonForeground`, `AccentButtonForegroundPointerOver`, `AccentButtonForegroundPressed`, `AccentButtonForegroundDisabled`, `AccentButtonBorderBrush`, `AccentButtonBorderBrushPointerOver`, `AccentButtonBorderBrushPressed`, `AccentButtonBorderBrushDisabled` |
| Text input | `TextControlForeground`, `TextControlForegroundPointerOver`, `TextControlForegroundFocused`, `TextControlForegroundDisabled`, `TextControlBackground`, `TextControlBackgroundPointerOver`, `TextControlBackgroundFocused`, `TextControlBackgroundDisabled`, `TextControlBorderBrush`, `TextControlBorderBrushPointerOver`, `TextControlBorderBrushFocused`, `TextControlBorderBrushDisabled`, `TextControlPlaceholderForeground`, `TextControlPlaceholderForegroundPointerOver`, `TextControlPlaceholderForegroundFocused`, `TextControlPlaceholderForegroundDisabled`, `TextControlSelectionHighlightColor` |
| Combo box | `ComboBoxBackground`, `ComboBoxBackgroundPointerOver`, `ComboBoxBackgroundPressed`, `ComboBoxBackgroundDisabled`, `ComboBoxForeground`, `ComboBoxForegroundFocused`, `ComboBoxForegroundDisabled`, `ComboBoxBorderBrush`, `ComboBoxBorderBrushPointerOver`, `ComboBoxBorderBrushPressed`, `ComboBoxBorderBrushDisabled`, `ComboBoxDropDownBackground`, `ComboBoxDropDownBorderBrush`, `ComboBoxDropDownGlyphForeground`, `ComboBoxDropDownGlyphForegroundFocused`, `ComboBoxDropDownGlyphForegroundFocusedPressed`, `ComboBoxDropDownGlyphForegroundDisabled`, `ComboBoxItemBackground`, `ComboBoxItemBackgroundPointerOver`, `ComboBoxItemBackgroundPressed`, `ComboBoxItemBackgroundDisabled`, `ComboBoxItemBackgroundSelected`, `ComboBoxItemBackgroundSelectedPointerOver`, `ComboBoxItemBackgroundSelectedPressed`, `ComboBoxItemBackgroundSelectedDisabled`, `ComboBoxItemForeground`, `ComboBoxItemForegroundPointerOver`, `ComboBoxItemForegroundPressed`, `ComboBoxItemForegroundDisabled`, `ComboBoxItemForegroundSelected`, `ComboBoxItemForegroundSelectedPointerOver`, `ComboBoxItemForegroundSelectedPressed`, `ComboBoxItemForegroundSelectedDisabled` |
| Tab item | `TabItemHeaderBackgroundUnselected`, `TabItemHeaderBackgroundUnselectedPointerOver`, `TabItemHeaderBackgroundUnselectedPressed`, `TabItemHeaderBackgroundSelected`, `TabItemHeaderBackgroundSelectedPointerOver`, `TabItemHeaderBackgroundSelectedPressed`, `TabItemHeaderBackgroundDisabled`, `TabItemHeaderForegroundUnselected`, `TabItemHeaderForegroundUnselectedPointerOver`, `TabItemHeaderForegroundUnselectedPressed`, `TabItemHeaderForegroundSelected`, `TabItemHeaderForegroundSelectedPointerOver`, `TabItemHeaderForegroundSelectedPressed`, `TabItemHeaderForegroundDisabled`, `TabItemHeaderSelectedPipeFill`, `TabItemMinHeight`, `TabItemHeaderMargin` |
| Menu and flyout | `MenuFlyoutPresenterBackground`, `MenuFlyoutPresenterBorderBrush`, `MenuFlyoutItemBackgroundPointerOver`, `MenuFlyoutItemBackgroundPressed`, `MenuFlyoutItemForeground`, `MenuFlyoutItemForegroundPointerOver`, `MenuFlyoutItemForegroundPressed`, `MenuFlyoutItemForegroundDisabled`, `MenuFlyoutItemKeyboardAcceleratorTextForeground`, `MenuFlyoutItemKeyboardAcceleratorTextForegroundPointerOver`, `MenuFlyoutItemKeyboardAcceleratorTextForegroundPressed`, `MenuFlyoutItemKeyboardAcceleratorTextForegroundDisabled` |
| Shipped-control aliases | `CheckBoxCheckBackgroundFillChecked`, `CheckBoxCheckBackgroundFillIndeterminate`, `CheckBoxCheckBackgroundStrokeIndeterminate`, `CheckBoxCheckGlyphForegroundChecked`, `CheckBoxCheckGlyphForegroundCheckedPointerOver`, `CheckBoxCheckGlyphForegroundCheckedPressed`, `CheckBoxCheckGlyphForegroundIndeterminate`, `CheckBoxCheckGlyphForegroundIndeterminatePointerOver`, `CheckBoxCheckGlyphForegroundIndeterminatePressed`, `SliderThumbBackground`, `SliderTrackValueFill`, `SliderTrackValueFillPointerOver`, `SliderTrackValueFillPressed`, `TreeViewItemBackgroundSelected`, `TreeViewItemBackgroundSelectedPointerOver`, `TreeViewItemBackgroundSelectedPressed`, `CheckBoxForegroundUncheckedPressed`, `CheckBoxCheckBackgroundFillUncheckedPressed`, `CheckBoxCheckBackgroundFillCheckedDisabled`, `CheckBoxCheckBackgroundFillIndeterminateDisabled`, `CheckBoxCheckGlyphForegroundCheckedDisabled`, `CheckBoxCheckGlyphForegroundIndeterminateDisabled` |

These keys are the fixed palette baseline. The fixed accent family, the six derived Fluent colours, and the
shipped-control alias keys live in the palette `Light`/`Dark` theme dictionaries (with `Default` mirroring
`Light`); `HighContrast.axaml` overrides the same keys. There is no per-accent overlay and no
`ActualThemeVariantChanged` reapply. The `TreeViewItemForegroundSelected*` aliases are deliberately not in this
table: the palette alias key does not reach `PART_HeaderPresenter.Foreground`, so §3.6 delivers the selected
TreeView foreground with targeted styles instead. The six `SystemAccentColorLight1`/`Light2`/`Light3`/
`Dark1`/`Dark2`/`Dark3` keys are **`Color`** resources in `Default`, `Light`, `Dark`, and high contrast, not
`SolidColorBrush` resources; the tests assert the type. Adding a key to this table remains a contract change
and a test update.

Check boxes, radio buttons, sliders, toggles, progress bars, and tooltips keep their Fluent templates. Their
text, accent, and selection keys resolve through the global entries above; the CheckBox/Slider alias keys are
defined in the palette because Fluent's `StaticResource` aliases capture the base brushes at load. The TreeView
selected foreground uses the targeted style in §3.6. A control that still shows a Fluent blue or the platform
accent after these palette/targeted-style rules is a defect in the key list, not a reason to add a template.

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
(`#FFD700`), the two accent reveal brushes (`#FFD700`), the shipped-control alias keys from §3.6
(`#FFD700` fills/tracks and `#000000` glyphs/text), `SystemControlFocusVisualPrimaryBrush` (`#FFD700`),
`SystemControlFocusVisualSecondaryBrush` (`#000000`), `SystemControlHighlightAltChromeWhiteBrush`
(`#000000`), `SystemControlHighlightChromeWhiteBrush` (`#000000`), and
`SystemControlForegroundChromeWhiteBrush` (`#000000`). The unchecked-pressed check-box fill is `#FFD700` with
`#000000` label text; checked/indeterminate disabled fills are `#000000` with `#A0A0A0` glyphs. The
high-contrast dictionary is merged after the palette, so it overrides the fixed accent family; the targeted
TreeView style consumes `Mocassin.SelectedBackground`/`SelectedForeground`, which are black/gold in
high contrast. There is no selected-accent state to leak.

### 3.6 Accent propagation

The accent is a fixed family. There is no user-facing accent selection, no `AccentPalette`, no per-accent
derivation, and no runtime accent overlay. The fixed values live in the palette `Light`/`Dark` theme
dictionaries (with `Default` mirroring `Light`), and `HighContrast.axaml` overrides the same keys. The one
direct runtime accent resource is `SystemAccentColor`: `#007ACC` normally, `#FFD700` under high contrast.
Fluent's `SystemAccentColors` provider derives its shades from the platform accent, so the palette must define
the derived colours and the shipped-control alias keys itself. Targeted `TreeViewItem` styles use the palette
tokens for the realized selected foreground.

**Fixed values.**

| Role | Dark | Light | Notes |
| --- | --- | --- | --- |
| `Mocassin.AccentBrush`, `Mocassin.StatusBarBackground` | `#007ACC` | `#007ACC` | fixed fill/status |
| `Mocassin.OnAccentBrush`, `Mocassin.StatusBarForeground` | `#FFFFFF` | `#FFFFFF` | 4.51:1 on `#007ACC`; keep the white VS Code treatment |
| `Mocassin.FocusBrush`, `SystemControlHighlightAccentBrush`, `TextControlBorderBrushFocused` | `#248DD3` | `#007ACC` | splitter focus adorner, tab selected pipe, text-input focus; 3:1+ on the paired surfaces |
| `Mocassin.AccentTextBrush` | `#4DA6FF` | `#005A9E` | 4.5:1+ on chrome/workspace/card |
| `Mocassin.SelectedBackground` / `SelectedForeground` | `#094771` / `#FFFFFF` | `#CCE5FF` / `#1F1F1F` | existing palette values |
| `SystemControlHighlightAccent2/3RevealBackgroundBrush` | `#094771` | `#CCE5FF` | tree selected fill aliases |
| `TextBox.SelectionBrush` / `SelectionForegroundBrush` | `#007ACC` / `#FFFFFF` | `#007ACC` / `#FFFFFF` | text selection pair |
| `ProgressBar.Foreground` (dialog progress) | `#248DD3` | `#007ACC` | `SystemControlHighlightAccentBrush` |
| `SystemAccentColorLight1` / `Light2` / `Light3` | `#3A9CDD` | `#0090F0` | derived hover states; type `Color` |
| `SystemAccentColorDark1` | `#248CD1` | `#005994` | derived pressed state; type `Color` |
| `SystemAccentColorDark2` / `Dark3` | `#248CD1` | `#003F6A` / `#002138` | completeness; type `Color`; `Dark2` has an unshipped `CalendarButton` consumer |

`Mocassin.FocusBrush` is consumed by the splitter focus adorners, the document/console selected-tab pipe, and
the text-input focus border; it is not an unused role. The `TabItemHeaderSelectedPipeFill` palette value is the
same `FocusBrush` value in each theme. The dialog `ProgressBar` uses `SystemControlHighlightAccentBrush`, which
is the same fixed focus/control value.

**Fluent alias delivery (M-6).** Avalonia 12.1.6 resolves the shipped control template parts through alias keys
that Fluent defines as `StaticResource` aliases. Overlaying only the upstream base key does not reach the
realized part. The design uses three proven mechanisms:

1. **Load-order capture is rejected as the sole mechanism.** A scratch probe defined `SystemAccentColor` before
   `FluentTheme` loaded. `CheckBoxCheckBackgroundFillChecked`, `SliderThumbBackground`,
   `SliderTrackValueFill`, and `TreeViewItemBackgroundSelected` captured that colour; glyphs captured white,
   and the tree captured the colour at 0.6/0.8 opacity. That proves load-order capture exists, but it cannot
   express the distinct selected fill/focus values and cannot follow Light/Dark or high contrast.
2. **Palette alias keys for the shipped CheckBox and Slider.** The palette `Light`/`Dark` dictionaries define
   `CheckBoxCheckBackgroundFillChecked`, `CheckBoxCheckBackgroundFillIndeterminate`,
   `CheckBoxCheckBackgroundStrokeIndeterminate`, the checked/indeterminate `CheckBoxCheckGlyphForeground*`
   keys, `SliderThumbBackground`, and `SliderTrackValueFill*`. A probe confirmed the realized
   `CheckBox.NormalRectangle.Background`, `CheckGlyph.Fill`, `Slider.Thumb.Background`, and slider
   `PART_DecreaseButton.Background` use these values and switch with the Light/Dark dictionaries. A targeted
   style cannot override the Slider thumb because the Fluent template sets its `Background` as a local value;
   the alias key is the correct delivery point.
3. **Targeted `TreeViewItem` styles for the realized selected foreground.** A probe confirmed the palette alias
   key reaches `PART_LayoutRoot.Background`, but not `PART_HeaderPresenter.Foreground`. `ShellStyles.axaml`
   therefore uses these exact selectors:
   - `TreeViewItem:selected /template/ Border#PART_LayoutRoot`
   - `TreeViewItem:selected /template/ Border#PART_LayoutRoot:pointerover`
   - `TreeViewItem:pressed:selected /template/ Border#PART_LayoutRoot:pointerover`
   - `TreeViewItem:selected /template/ ContentPresenter#PART_HeaderPresenter` (direct descendant; **not**
     `Border#PART_LayoutRoot > ContentPresenter#PART_HeaderPresenter`, which cannot match because the
     presenter's visual parent is `Grid#PART_Header`)
   - `TreeViewItem:selected /template/ ToggleButton#PART_ExpandCollapseChevron /template/ Path#ChevronPath`
     (the chevron uses `TreeViewItemForeground`, so the selected row must set the chevron `Fill` explicitly)

   The row styles set `Background` from `Mocassin.SelectedBackground`; the header and chevron styles set
   `Foreground`/`Fill` from `Mocassin.SelectedForeground`. The probes reached the realized layout root, header
   presenter, and chevron. This is a targeted style, not a control template.

No shipped control keeps the platform accent. No broad custom template is required. The `ComboBoxItem*` keys
are already palette keys and reach the ComboBox realized parts.

**High contrast.** `HighContrast.axaml` defines the same fixed family: base roles, `SystemAccentColorLight1`/
`Light2`/`Light3`/`Dark1`/`Dark2`/`Dark3` as **`Color`** resources (`#FFD700`), `SystemControlForegroundAccentBrush`,
the reveal brushes, `SystemControlFocusVisualPrimaryBrush` (`#FFD700`), `SystemControlFocusVisualSecondaryBrush`
(`#000000`), `SystemControlHighlightAltChromeWhiteBrush`, `SystemControlHighlightChromeWhiteBrush`,
`SystemControlForegroundChromeWhiteBrush` (`#000000`), and the shipped-control alias keys (`#FFD700`
fills/tracks and `#000000` glyphs/text). The unchecked-pressed check-box fill is `#FFD700` with `#000000` label
text; checked/indeterminate disabled fills are `#000000` with `#A0A0A0` glyphs. The palette alias keys are
overridden by the merged high-contrast dictionary; the targeted TreeView style uses
`Mocassin.SelectedBackground`/`SelectedForeground` (black/gold) and also sets the selected chevron `Fill`. The
TextBox selection pair is `#FFD700`/`#000000`.

**Theme switch.** The fixed Light/Dark role values live in the palette theme dictionaries, so an actual
Light/Dark switch updates the base roles, the derived Fluent colours, and the CheckBox/Slider alias keys
automatically. The targeted TreeView style consumes the same palette tokens and updates with the theme. The
only runtime `SystemAccentColor` value is theme-independent (`#007ACC`), except high contrast (`#FFD700`).
There is no `ActualThemeVariantChanged` accent reapply because there is no runtime accent overlay.

**Accent text is resource-only.** `Mocassin.AccentTextBrush` has no current view consumer. The palette and
contrast tests prove the token; they do not prove a rendered link or accent-text surface, and the screenshot
set must not claim one. Adding a link surface is deferred.

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
| Selected | `SelectedBackground` and `SelectedForeground` for list, tree, and combo rows; the TreeView selected foreground is delivered by the targeted style in §3.6. The active tab uses `TabActiveBackground`, `TabActiveForeground`, and a 2 px `FocusBrush` indicator. |
| Focus | A visible 2 px `FocusBrush` ring, inside or outside the control, with at least 3:1 contrast against the adjacent surface. Focus is never removed for a keyboard-reachable control. |
| Disabled | `DisabledBackground` and `DisabledForeground`; the control stays legible. Opacity alone is not a disabled treatment. |
| Dirty/unsaved | The existing `*` marker plus its accessible name; no new colour-only signal. |

Focus indicators for splitters keep the existing adorner but use `FocusBrush` and `FocusThickness`. Fluent
12.1.6's default focus adorner uses the neutral `SystemControlFocusVisualPrimaryBrush` /
`SystemControlFocusVisualSecondaryBrush` pair, not `SystemControlHighlightAccentBrush`; the neutral ring is
accepted and must meet 3:1 against the adjacent surface. It is deliberately not accent-coloured. High contrast
overrides the pair to `#FFD700`/`#000000`, so the 2 px primary is gold on black and the secondary is black on
gold. Controls with an explicit accent focus border or adorner (text inputs and splitters) use `FocusBrush`. A
control without a visible focus ring gets an explicit `FocusAdorner` from `ShellStyles.axaml`. Replacing a
control template to obtain focus is not acceptable in this pass.

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
- The background is fixed `#007ACC` with a white foreground; text and progress are 4.51:1. High contrast keeps
  the black/gold pair.
- Text is caption size. The application name is `SemiBold`; the remaining items use normal weight.
- The progress bar sits on the right, 140 px wide and 4 px high, with `StatusBarForeground` on the transparent
  track. Its foreground/background pair meets 4.5:1.
- The status bar has no border; the chrome backdrop and the card borders separate it from the workspace.

### 7.4 Document tab strip

- Height `TabHeight`; background `TabStripBackground`; tabs use `RadiusTab` on the top corners only.
- Inactive tab: `TabInactiveBackground`, `TabInactiveForeground`. Active tab: `TabActiveBackground`,
  `TabActiveForeground`, plus a 2 px `TabItemHeaderSelectedPipeFill` indicator along the top edge. The palette
  value is `FocusBrush` (`#248DD3` dark, `#007ACC` light, `#FFD700` HC). The existing dirty
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
| Accent text on chrome, workspace, and card | `#4DA6FF` | `#005A9E` | 4.5:1 |
| On-accent text on accent fill (accent button, status bar) | `#FFFFFF` on `#007ACC` | `#FFFFFF` on `#007ACC` | 4.5:1 |
| Selected and hover text (`SystemControlHighlightAltBaseHighBrush`) on selected/hover fills | `#FFFFFF` | `#1F1F1F` | 4.5:1 |
| On-accent glyphs on Fluent accent controls and derived states | `#FFFFFF` on `#248DD3`/`#3A9CDD`/`#248CD1` | `#FFFFFF` on `#007ACC`/`#0090F0`/`#005994` | 3:1 |
| Selected-row text on selected fill | `#FFFFFF` on `#094771` | `#1F1F1F` on `#CCE5FF` | 4.5:1 |
| Status-bar text, glyphs, and progress on status bar | `#FFFFFF` on `#007ACC` | `#FFFFFF` on `#007ACC` | 4.5:1 |
| Text selection pair (`SelectionBrush`/`SelectionForegroundBrush`) | `#FFFFFF` on `#007ACC`; HC `#000000` on `#FFD700` | `#FFFFFF` on `#007ACC`; HC `#000000` on `#FFD700` | 4.5:1 |
| Error, warning, success, and info text on card | the dark semantic tokens | the light semantic tokens | 4.5:1 |
| Focus ring, selected-pipe indicator, and Fluent state fill against adjacent surfaces | `#248DD3`/`#3A9CDD`/`#248CD1` on `#3C3C3C`/`#37373D`/`#2D2D30`/`#252526`/`#1E1E1E` | `#007ACC`/`#0090F0`/`#005994` on `#F3F3F3`/`#FFFFFF` | 3:1 |
| Fluent default neutral focus ring (`SystemControlFocusVisualPrimaryBrush`/`SecondaryBrush`) against adjacent surfaces | the neutral Fluent pair; HC `#FFD700`/`#000000` | the neutral Fluent pair; HC `#FFD700`/`#000000` | 3:1 |

High contrast pairs are `#FFFFFF` on `#000000` (21:1), `#000000` on `#FFD700` (about 15:1), and `#FFD700`
against `#000000` (about 15:1). They are tested separately.

### 11.2 Focus and keyboard

- Every keyboard-reachable control shows a visible focus ring. This includes buttons, inputs, combo boxes,
  list and tree rows, tabs, splitters, check boxes, sliders, and the document close button.
- Focus is not indicated by colour alone: the ring has thickness. Selected state is not indicated by colour
  alone: tabs have an indicator and list rows have a fill.
- The interaction contract's initial-focus, `Escape`, `Enter`, and layer rules are unchanged. A visual change
  must not add a new focus target or remove an existing one, except for the owner-approved accent-setting
  removal: the Settings `Accent` ComboBox focus stop and its `Accent` automation name are intentionally gone,
  the remaining order is tested, and no other focus target changes.
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
| WCAG contrast for the fixed family in §11.1, both themes and high contrast | PR-fast unit |
| Fixed accent delivery: palette Light/Dark/Default, derived Fluent colours, shipped-control alias keys, status bar, accent text, focus, tab pipe, selection, high-contrast precedence | PR-fast headless |
| Realized template-part brushes for the shipped CheckBox (`NormalRectangle`, `CheckGlyph`), Slider (`Thumb`, value track), TreeView (`PART_LayoutRoot`, direct `PART_HeaderPresenter`, `Path#ChevronPath`), dialog `ProgressBar.Foreground`, and `TextBox` selection pair in light, dark, and high contrast | PR-fast headless |
| Theme switch changes the realized parts through the palette dictionaries; no runtime accent reapply; `#3C3C3C`/`#37373D` focus surfaces; HC focus pair `#FFD700`/`#000000`; neutral default focus ring | PR-fast headless |
| The six `SystemAccentColor*` keys are `Color` in `Default`/`Light`/`Dark`/HC; no `InvalidCastException` | PR-fast headless |
| Settings removal: no Accent control, no `AccentPalette` path; legacy `accent` values are accepted no-ops, not written; new Settings Tab order and absence of the `Accent` automation name are tested | PR-fast headless |
| Region card radius/border/gap, tab labels/order, removed channel combo | PR-fast headless |
| Console tab persistence, unknown-id fallback, `Enter` no-op, focus order | PR-fast headless |
| Welcome button label centring at comfortable and compact density | PR-fast headless |
| 1.5× font scale with compact density: no clipping on primary surfaces | PR-fast headless |
| Screenshot baselines for light, dark, high contrast, console tabs, and settings | PR-medium |
| Screen-reader pass, 125 %/150 % display scaling, Windows/macOS keyboard pass | Manual; recorded as platform evidence |
