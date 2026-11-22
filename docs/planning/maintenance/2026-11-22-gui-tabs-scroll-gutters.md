# GUI tab headers and scroll gutters (2026-11-22)

An out-of-step maintenance change under `AGENTS.md` §3.5. This record is the design and test plan for one
`fix/gui-tabs-scroll-gutters` branch and one owner `--no-ff` merge approval. It covers two owner-reported
visual defects: the document/console tab headers do not read as tabs, and some text sits under a scrollbar.

| Field | Value |
| --- | --- |
| Type | Maintenance fix / tab header treatment and scroll gutters |
| Branch | `fix/gui-tabs-scroll-gutters` |
| Base | `main` at `ec889212a60aa81c8b6f51f01c35867d0fb14be9` |
| Worktree | `/workspace` (clean, in place) |
| Normative contract | [`docs/development/ui-visual-contract.md`](../../development/ui-visual-contract.md) §3.1, §3.2, §5, §5.1, §6, §7.1, §7.4, §7.5, §8, §10, §11, §13 |
| Status | Design frozen; implementation pending owner approval. Docs-only commit on the branch; one owner merge approval. |
| Tracking | One branch; local gates; no per-feature CI; no step tag; no ADR |

## 1. Owner report and decisions

The owner reported:

1. The document/control tab headers and the console tab headers do not fit the design. In Visual Studio Code,
   tabs have rounded top corners; the selected tab has a seamless visual transition into the open content; the
   active tab header has the same background as the content surface; the inactive tab header is a slightly
   different colour (slightly lighter grey in the dark theme, slightly darker in light). The owner approved
   keeping the 2 px accent top line on the active tab.
2. Some text overlaps the scrollbar. The command palette list and the keyboard shortcut help are the reported
   cases. Every scroll surface must reserve at least a small right/bottom gutter so content never sits under
   the scrollbar.

Owner decisions already taken:

| ID | Decision |
| --- | --- |
| T1 | Tab treatment: seamless active tab plus a 2 px accent top line; rounded top corners; active background equals the content; inactive header slightly lighter in dark and slightly darker in light. Apply to both document/control tabs and console tabs. |
| S1 | Scroll gutters apply to every scroll surface, not only the two reported ones. No custom scrollbar template is required if a content gutter solves it. |
| I1 | The Phosphor Icons pass (MIT; explorer tree plus every open document/control tab) is a **separate later branch**. This pass adds no icon or asset, but the tab header must reserve a 16 px leading icon slot and a binding point so the icon pass can populate it without reworking the template. |
| B1 | One `fix/gui-tabs-scroll-gutters` branch from `main` at `ec88921`, then one owner `--no-ff` merge approval. No independent design review unless the planner concludes one is needed. |

## 2. Current state

### 2.1 Tab realization

Both strips are a stock `TabControl` with `TabStripPlacement="Top"`:

| Surface | View | Contents |
| --- | --- | --- |
| Document tabs | `src/dotnet/Mocassin.App.Views/Shell/DocumentsRegionView.axaml:10-13` | `ItemTemplate` at `:14-40`: horizontal `StackPanel` (`Spacing="8"`), title `TextBlock` (`:17`), dirty `*` (`:18-22`), compact close `Button` (`:23-30`), per-tab `ContextMenu` (`:31-37`) |
| Console tabs | `src/dotnet/Mocassin.App.Views/Consoles/ConsoleView.axaml:9-14` | `ItemTemplate` at `:15-19`: one `TextBlock` bound to `TabTitle`; content templates at `:20-27` |
| Console card | `src/dotnet/Mocassin.App.Views/Shell/ConsoleRegionView.axaml:8-11` | region card directly wrapping `ConsoleView`; no separate title row |

Tab styling is split between `ShellStyles.axaml:96-105` and the palette/Fluent keys:

- `TabControl` sets `Background="Transparent"` (`ShellStyles.axaml:97`).
- `TabItem` sets `MinHeight` from `Mocassin.TabHeight` (`:101`), `Padding` from `Mocassin.RegionHeaderPadding`
  (`:102`, so 12,8 comfortable / 8,6 compact), `CornerRadius` from `Mocassin.RadiusTabTop` (`:103`, `4,4,0,0`),
  and `FontSize` from `Mocassin.BodyFontSize` (`:104`).
- The realized colours come from the Fluent `TabItemHeader*` keys, not the `Mocassin.Tab*` roles:
  `Palette.axaml:136-152` (light), `:332-348` (default, mirroring light), `:528-544` (dark);
  `HighContrast.axaml:133-147`; compact height/margin in `Density.axaml:13-17`.
- `Mocassin.TabStripBackground`, `Mocassin.TabActiveBackground`, `Mocassin.TabInactiveBackground`,
  `Mocassin.TabActiveForeground`, and `Mocassin.TabInactiveForeground` are declared in `Palette.axaml:12-25`,
  `:208-221`, `:404-417` and `HighContrast.axaml:10-23` **but no view or style consumes them**. Only
  `ThemeContrastTests.cs:28-30,41-42` reads them. The realized tab colours are the Fluent keys; the contract's
  `Mocassin.Tab*` roles and the realized values have drifted apart.

Avalonia 12.1.6 Fluent `TabItem` facts that the design depends on, verified against the pinned upstream
source (`src/Avalonia.Themes.Fluent/Controls/TabItem.xaml`, `TabControl.xaml`, `ScrollViewer.xaml`,
`ScrollBar.xaml`, `ListBox.xaml`, `TreeView.xaml`, `TextBox.xaml`, `ComboBox.xaml`, `Accents/FluentControlResources.xaml`):

- The `TabItem` template is `Border#PART_LayoutRoot` (background, border, `CornerRadius` and `Padding` from
  template bindings) containing `ContentPresenter#PART_ContentPresenter` and `Border#PART_SelectedPipe`.
- The pipe is a child of the padded layout root. For `TabStripPlacement=Top` it is
  `Height=2`, `HorizontalAlignment=Stretch`, `VerticalAlignment=Bottom`, `Margin="0,0,0,2"`, so the current
  2 px line is at the **bottom** and inset by the tab padding, not at the top edge.
- `TabControl` docks the `ItemsPresenter` at the top with `TabControlTopPlacementItemMargin = 0 0 0 2`, so a
  2 px gap separates the strip from the content.
- `TabItem` also exposes `Icon`, `IconTemplate`, and `IndicatorTemplate` properties, but the Fluent template
  does not render `Icon`; it is not a usable icon slot without a template.
- `TabItem` has no Fluent focus style and no default `FocusAdorner`; `OnGotFocus` selects the tab. Keyboard
  focus therefore has no visible ring today (pre-existing accessibility gap).

Measured from the checked-in baselines (read-only inspection; Linux reference):

| Evidence | Observation |
| --- | --- |
| `Visual/Baselines/dark.png` rows `y=69-70`, columns `x=222-311` | the selected document tab's 2 px line is `#007ACC` near the **bottom** of the 32 px strip, inset 12 px each side |
| `Visual/Baselines/dark.png` rows `y=39-71`, `x=208-339` | the whole strip is `#252526`; the selected tab is indistinguishable from the strip and content, so the 4 px corner radius is invisible |
| `Visual/Baselines/dark-validation.png` rows `y=471-482`, `x=210-285` vs `x=286+` | the inactive `Console` tab is `#1E1E1E`, i.e. **darker** than the `#252526` content and equal to the chrome; the active `Validation` tab is `#252526` |
| `Visual/Baselines/dark-validation.png` rows `y=471-474`, `x=283-285` | the corner radius is realized and antialiased where the adjacent colour differs; the fill values, not the radius, are the defect |
| `Visual/Baselines/light-validation.png` rows `y=471-474` | the light inactive tab `#F3F3F3` is already darker than the `#FFFFFF` content and the active tab is merged; light keeps that direction and only moves the chip/text values in §3.2 so the chip stays distinct and meets contrast |

The realized document content is the region card's `WorkspaceBackground` (`ShellStyles.axaml:111-116`), because
the `TabControl` is transparent. The strip and the content therefore already share one surface; the contract's
"tab strip uses `ChromeBackground`" (`ui-visual-contract.md` §3.1/§7.1) is not realized.

### 2.2 Scroll surfaces

The scroll surfaces an application view owns are:

| Surface | Control | Location | Axes |
| --- | --- | --- | --- |
| Palette results | `ListBox` | `Commands/CommandPaletteView.axaml:20-40` | vertical |
| Cheat sheet | `ScrollViewer` | `Commands/ShortcutCheatSheetView.axaml:19-39` | vertical |
| Settings | `ScrollViewer` | `Settings/SettingsDialogView.axaml:15-95` | vertical |
| Validation filters | `ScrollViewer` | `Consoles/ProblemsView.axaml:14-64` | horizontal |
| Validation entries | `ListBox` | `Consoles/ProblemsView.axaml:73-107` | vertical |
| Application console | `ListBox` | `Consoles/ApplicationConsoleView.axaml:18-37` | vertical |
| Project overview | `ScrollViewer` | `Documents/Project/ProjectOverviewView.axaml:7-126` | vertical (content has `Margin="20"`) |
| Project JSON | `TextBox` | `Documents/Project/ProjectJsonView.axaml:13-19` | both (read-only, `NoWrap`, `AcceptsReturn`) |
| Crash details | `ScrollViewer` | `Dialogs/CrashDialogView.axaml:13-15` | vertical |
| Choice dialog | `ListBox` | `Dialogs/ChoiceDialogView.axaml:10-19` | vertical |
| Open projects | `ListBox` | `Shell/ProjectsRegionView.axaml:16-44` | vertical |
| Explorer | `TreeView` | `Shell/ProjectsRegionView.axaml:51-65` | both |
| ComboBox drop-downs | internal `ScrollViewer` | `SettingsDialogView.axaml:19,26,34`; `ProblemsView.axaml:19,36,46` | vertical |

The overlap is structural, not a coincidence: Fluent 12.1.6 `ScrollViewer.AllowAutoHide` defaults to `true`,
which makes the template's `ScrollContentPresenter` span both grid columns/rows (`ScrollViewer.xaml`,
`AllowAutoHide=True` style), so the scrollbar floats **over** the content. `ScrollBarSize` is **16** in both
theme dictionaries. `ListBox` and `TreeView` forward their own `Padding` to the internal items presenter;
`TextBox` does not forward `Padding` to `PART_ScrollViewer`, and `ComboBox`'s popup `ScrollViewer` has no
padding binding at all.

The palette is the clearest case: the item template's gesture text is the right column of a `*,Auto` grid
(`CommandPaletteView.axaml:29-37`), so its right edge is exactly the list viewport's content edge, which is
also the scrollbar's right edge; the 16 px overlay covers the last 16 px of the text column. The cheat sheet
has the same shape (`ShortcutCheatSheetView.axaml:28-31`). Settings' right-aligned `Clear recent` button ends
at the `ScrollViewer`'s content edge, so its right end is inside the 16 px overlay track; the resting 2 px
thumb (`dark-settings.png`, `x=819-820`) leaves a 6 px gap to the button, but an expanded track from `x=807`
covers it.

## 3. Tab header design

### 3.1 Surface model

The tab strip is **not** a separate chrome surface. The surface behind and below the strip is
`WorkspaceBackground`, so the selected tab is continuous with the content; the region card supplies that
surface and no strip paint is added.

- `Mocassin.TabStripBackground` is retired: it has no consumer and its value contradicts the realized strip.
  The contract states that the strip shares the content surface (`WorkspaceBackground`) instead.
- `Mocassin.TabActiveBackground` = `WorkspaceBackground` and `Mocassin.TabActiveForeground` remain; they are
  the normative roles for the Fluent `TabItemHeaderBackgroundSelected` / `TabItemHeaderForegroundSelected`
  keys. Tests keep each pair equal so the two sources cannot drift again.
- The 2 px strip/content gap is removed (`ItemsPresenter#PART_ItemsPresenter` margin `0`), so the selected
  tab's bottom edge and the content's top edge coincide.
- No bottom border is added to the selected tab and the strip has no bottom border.

This is the smallest route that satisfies "active background equals the content" and "seamless transition".
The alternative — a chrome band (`TabStripBackground` `#1E1E1E`/`#F3F3F3`) behind the tabs — would make the
active tab's top corners visible against the band, but it either needs a view-level band or an
`ItemsPanel` background plus a content-surface invariant, and the owner did not ask for a different band. It is
recorded in §10 as the one alternative the owner may choose instead; it changes only `TabStripBackground` and
the band delivery.

### 3.2 Tokens

| Role | Dark | Light | High contrast | Realized by |
| --- | --- | --- | --- | --- |
| Active tab fill | `#252526` | `#FFFFFF` | `#000000` | `TabItemHeaderBackgroundSelected` (and `*PointerOver`/`*Pressed`, unchanged) = `Mocassin.TabActiveBackground` = `WorkspaceBackground` |
| Active tab text | `#FFFFFF` | `#1F1F1F` | `#FFFFFF` | `TabItemHeaderForegroundSelected*` = `Mocassin.TabActiveForeground` |
| Inactive tab fill | `#2D2D30` | `#ECECEC` | `#000000` | `TabItemHeaderBackgroundUnselected` = `Mocassin.TabInactiveBackground` |
| Inactive tab text | `#9D9D9D` | `#616161` | `#E0E0E0` | `TabItemHeaderForegroundUnselected` = `Mocassin.TabInactiveForeground` |
| Inactive hover fill/text | `#37373D` / `#D4D4D4` | `#E8E8E8` / `#333333` | `#FFD700` / `#000000` | `TabItemHeaderBackground/ForegroundUnselectedPointerOver` = `HoverBackground` |
| Inactive pressed fill/text | `#2A2D2E` / `#D4D4D4` | `#DCDCDC` / `#333333` | `#FFD700` / `#000000` | `TabItemHeaderBackground/ForegroundUnselectedPressed` = `PressedBackground` |
| Selected hover/pressed | active values | active values | active values | selected pointer-over/pressed keys; no fill change |
| Disabled | `#2A2A2A` / `#6B6B6B` | `#EDEDED` / `#9A9A9A` | `#000000` / `#A0A0A0` | `TabItemHeaderBackground/ForegroundDisabled`; no shipped disabled tab |
| Accent indicator | `#248DD3` | `#007ACC` | `#FFD700` | `TabItemHeaderSelectedPipeFill` = `Mocassin.FocusBrush` |

Contrast (measured with the WCAG formula): dark inactive `#9D9D9D` on `#2D2D30` = 5.06:1; light inactive
`#616161` on `#ECECEC` = 5.24:1. The light inactive foreground changes because the old `#6F6F6F` on `#ECECEC`
is 4.25:1, below the 4.5:1 floor; `#6F6F6F` on the old `#F3F3F3` chip was 4.53:1. Active text is 15.31:1
dark and 16.48:1 light. Hover text is 7.98:1 dark and 10.31:1 light.

The light chip moves from `#F3F3F3` to `#ECECEC` so the inactive tab is a visible chip against the
`#FFFFFF` content and the selected tab; the dark chip moves from `#1E1E1E` to `#2D2D30` so it is lighter than
the content, as the owner specified. High contrast keeps every fill `#000000` and carries the selected state
with the `#FFD700` top line and the white text, which is the existing high-contrast rule and is not a
colour-only cue because the line has thickness.

### 3.3 Geometry and the indicator

| Property | Comfortable | Compact | Delivery |
| --- | --- | --- | --- |
| Header height (minimum) | `Mocassin.TabHeight` = 32 | 28 | `TabItem.MinHeight` in `ShellStyles`; `TabItemMinHeight` mirrors it |
| Header padding | `TabItemHeaderMargin` = `8,3` | `6,2` | `ContentPresenter#PART_ContentPresenter.Margin`; `TabItem.Padding` becomes `0` so the indicator spans the tab |
| Top corner radius | `Mocassin.RadiusTab` = 4 as `4,4,0,0` | same | `TabItem.CornerRadius` = `Mocassin.RadiusTabTop` (already set) |
| Indicator height | 2 | 2 | `TabItemPipeThickness` (Fluent default 2) |
| Indicator position | top edge, `VerticalAlignment=Top` | same | targeted style on `Border#PART_SelectedPipe` |
| Indicator end inset | `Mocassin.TabIndicatorInset` = `4,0,4,0` | same | targeted `Margin`; the line stops at the corner radius ends instead of crossing them |
| Indicator corner radius | `0` | same | targeted style; the Fluent pipe's `ControlCornerRadius` is inappropriate for a 2 px line |
| Strip/content gap | 0 | 0 | `ItemsPresenter#PART_ItemsPresenter.Margin` = `0` |

The indicator is the existing `PART_SelectedPipe`, moved from the bottom to the top. Keeping the part keeps
`TabItemHeaderSelectedPipeFill` and `Mocassin.FocusBrush` as the realized accent consumer; no accent key is
retired. The 4 px end inset is a deliberate detail so the line never squares off the rounded corners. The
full-width variant (`Margin=0`) is a one-value alternative if the owner prefers it; both are style-only.

The header is a minimum height: at 1.5× font scale the 20 × 1.5 = 30 px line plus the 2 × 3 px margin is 36 px
comfortable (34 px compact), so the tab grows and never clips. `TabHeight` remains the floor, not a fixed
height.

No `TabItem` control template is created or replaced. The stock Fluent theme already provides
`Border#PART_LayoutRoot` with the `CornerRadius` binding, the content presenter, and the pipe; the app styles
change the padding split, move and inset the pipe, and add the focus adorner. This is the smallest route: a
custom template would duplicate the Fluent state setters and risk the arrow/`Enter`/focus behaviour that the
interaction contract owns, for one alignment change.

The header padding replaces the current `RegionHeaderPadding` (12,8 comfortable / 8,6 compact) with the
Fluent key's intended `TabItemHeaderMargin` (8,3 / 6,2). Tabs become 8 px narrower and the vertical inset is
reduced; the height floor is unchanged and the header still centres its content.

### 3.4 State transitions

- Inactive: `TabInactiveBackground`/`TabInactiveForeground`; pointer-over and pressed use the existing
  `HoverBackground`/`PressedBackground` pairs; no geometry change.
- Active: `TabActiveBackground`/`TabActiveForeground`, a 2 px `FocusBrush` top line, no bottom border, no fill
  change on hover or press (matching the fixed selected keys).
- Focus: a 2 px `FocusBrush` `FocusAdorner` on the tab, inside the tab bounds, with the same top-corner radius.
  The tab is focusable in Fluent; today it has no visible ring, so this closes the contract's §11.2 floor
  without changing focus order, selection, or the interaction contract. The indicator is not a focus ring and
  the ring is not a selection cue.
- Disabled: keys are defined but no shipped tab is disabled; the styling is inventory-only.

### 3.5 Shared document/console treatment

Document and console strips use the same `TabControl` theme and the same app styles, so the treatment is
shared automatically. Their content surfaces differ in composition but agree on the surface the seam depends
on:

- Documents: the content is the region card's `WorkspaceBackground`; the welcome document, project overview,
  and JSON views are transparent over it or paint it.
- Console: the channel views (`ApplicationConsoleView`, `ProblemsView`) paint `WorkspaceBackground` and are
  placed in the console region card.
- Rule: the surface immediately behind and below a tab strip is `WorkspaceBackground`. A future editor that
  paints a different surface at its top edge is a contract change; it may not introduce a visible seam under
  the active tab.

The documents header template keeps its title, dirty `*`, compact close button, and context menu. The console
header template keeps its visible `TabTitle` and gains the same leading slot as the documents header.

### 3.6 Icon-slot reservation (later Phosphor pass)

No icon or asset is added in this pass. The template reserves the slot so the later branch does not rework it:

- Both item templates start with a `ContentControl` named `TabIconSlot`, 16 × 16 logical pixels, vertically
  centred, `Content="{Binding Icon}"`, `IsVisible` bound to `Icon` being non-null with
  `{x:Static ObjectConverters.IsNotNull}`, `IsHitTestVisible="False"`, `Focusable="False"`, and
  `AutomationProperties.AccessibilityView="Raw"`.
- `DocumentViewModel` gains `public virtual object? Icon => null;` and `ConsoleChannelViewModel` gains
  `public object? Icon { get; init; }`. Both classes are internal, so this adds no public API surface and no
  `PublicAPI.Unshipped.txt` entry. Icons are set at construction, so no change notification is needed.
- The slot is collapsed while `Icon` is null, so this pass has no empty leading gap and no tab-width change.
  The later pass only supplies the icon source (and, if needed, a `DataTemplate` for it); the tab template is
  unchanged. The alternative — an always-visible empty 16 px slot now — is rejected because it would indent
  every title for no content.
- `TabItem.Icon`/`IconTemplate` exist in Avalonia 12.1.6 but the Fluent template does not render them, so the
  item-template slot is the binding point, not the container property.
- Decorative only: the slot carries no accessible name and no text, so a tab is never identified by an icon
  alone; the visible header text remains the tab's name and the automation peer's label.

## 4. Scroll-gutter design

### 4.1 Rule and tokens

Every application-authored scroll surface reserves a right gutter on each vertical scroll axis and a bottom
gutter on each horizontal scroll axis. With `ScrollBarSize = 16` in Fluent 12.1.6 and a 4 px margin on the
4 px grid, the gutter is **20 px**:

| Token | Value | Use |
| --- | --- | --- |
| `Mocassin.ScrollGutterRight` | `0,0,20,0` | vertical-only surfaces |
| `Mocassin.ScrollGutterBottom` | `0,0,0,20` | horizontal-only surfaces |
| `Mocassin.ScrollGutterBoth` | `0,0,20,20` | surfaces with both axes |

The tokens live in `Mocassin.App/App.axaml` with the other theme-independent dimensions. They are
density-independent (the scrollbar is 16 px in both densities) and theme-independent; high contrast changes
the scrollbar colours, not its extent.

`ShellStyles.axaml` adds one class per axis and applies it to the three control shapes that accept `Padding`:

```xml
<Style Selector="ScrollViewer.scroll-gutter-right, ListBox.scroll-gutter-right, TreeView.scroll-gutter-right">
  <Setter Property="Padding" Value="{DynamicResource Mocassin.ScrollGutterRight}" />
</Style>
```

plus `scroll-gutter-bottom` and `scroll-gutter-both`. `ScrollViewer.Padding` is the content presenter's
padding; `ListBox.Padding` and `TreeView.Padding` are forwarded to the internal `ItemsPresenter` margin, so
the realized item edge and row highlight stop 20 px before the viewport edge. The scrollbar keeps its own
16 px width and hit target.

`TextBox` does not forward its `Padding` to the internal `ScrollViewer`, and the `ComboBox` popup
`ScrollViewer` has no padding binding, so those two need targeted styles that consume the same token:

```xml
<Style Selector="TextBox.scroll-gutter-both /template/ ScrollViewer#PART_ScrollViewer">
  <Setter Property="Padding" Value="{DynamicResource Mocassin.ScrollGutterBoth}" />
</Style>
<Style Selector="ComboBox /template/ ScrollViewer">
  <Setter Property="Padding" Value="{DynamicResource Mocassin.ScrollGutterRight}" />
</Style>
```

The rule is: **the realized content edge must stay at least 3 px clear of the realized scrollbar edge**
(the 20 px gutter against the 16 px bar gives 4 px; a 3 px assertion absorbs sub-pixel rounding). An existing
inset that already meets 20 px satisfies the rule without a new class; the project overview's `Margin="20"`
is the only such surface.

`AllowAutoHide` stays at the Fluent default (`true`). Setting it to `false` would reserve the bar's own
16 px and remove the overlay, but it changes the stock scrollbar into a permanently visible track and is a
larger visual change than the owner asked for. It is the recorded alternative, not the design.

### 4.2 Surface list

| # | Surface | Change |
| --- | --- | --- |
| 1 | `CommandPaletteView.axaml:20` results `ListBox` | `scroll-gutter-right`; fixes the reported overlap |
| 2 | `ShortcutCheatSheetView.axaml:19` `ScrollViewer` | `scroll-gutter-right`; fixes the reported overlap |
| 3 | `SettingsDialogView.axaml:15` `ScrollViewer` | `scroll-gutter-right` |
| 4 | `ProblemsView.axaml:14` filter `ScrollViewer` | `scroll-gutter-bottom` (horizontal bar only) |
| 5 | `ProblemsView.axaml:73` entries `ListBox` | `scroll-gutter-right` |
| 6 | `ApplicationConsoleView.axaml:18` entries `ListBox` | `scroll-gutter-right` |
| 7 | `ProjectOverviewView.axaml:7` root `ScrollViewer` | **no class**: the content's `Margin="20"` already meets the floor; the measurement test covers it |
| 8 | `ProjectJsonView.axaml:13` read-only `TextBox` | `scroll-gutter-both` class + internal `PART_ScrollViewer` style |
| 9 | `CrashDialogView.axaml:13` details `ScrollViewer` | `scroll-gutter-right` |
| 10 | `ChoiceDialogView.axaml:10` choices `ListBox` | `scroll-gutter-right` |
| 11 | `ProjectsRegionView.axaml:16` open-projects `ListBox` | `scroll-gutter-right` |
| 12 | `ProjectsRegionView.axaml:51` explorer `TreeView` | `scroll-gutter-both` (horizontal labels can appear) |
| 13 | `ComboBox` drop-downs (settings and Validation filters) | targeted popup `ScrollViewer` right gutter |

Excluded with rationale, not overlooked:

- The tab strip has no `ScrollViewer` in the Fluent `TabControl` template, so it is not a scroll surface
  today. The contract's "scrolls horizontally" sentence (§7.4) is unrealized and is recorded as an open
  observation below; adding tab-strip scrolling is out of this branch.
- Menu and context-menu flyouts use Fluent's internal `MenuScrollViewer`; the shipped menus do not overflow
  and are not app-authored surfaces. They are deferred until a menu scrollbar is reported.
- Tooltips have no scrolling content.
- Single-line `TextBox` instances never show a bar; only the JSON view is multi-line and scrollable, so the
  `TextBox` style is class-scoped and does not shift every input.

## 5. Accessibility

- **Tab names.** Each tab's accessible name remains its visible header: the document `Title` and the console
  `TabTitle` (`Console`/`Validation`). The console view model keeps its `ToString()` override, so the
  `TabItem.Header` value that `AccessibilityTests` reads is unchanged even when the item template wraps the
  text; `ConsoleTabsTests` reads the visible label and is unchanged. The icon slot has
  `AccessibilityView="Raw"` and no name, so it never contributes text to the name.
- **Selected state.** The `TabItem` selection pattern announces the selected tab; the state is carried by the
  tab fill relative to the inactive tabs and by the 2 px line, so it is not colour-only. High contrast keeps
  the gold line and white text.
- **Focus.** A tab shows the 2 px `FocusBrush` ring; focus order, arrow selection, and the "plain `Enter` is a
  no-op" rule are unchanged (interaction contract §5.2). The close button is the only extra focus stop inside
  a document tab and is unchanged.
- **Hit targets.** The 32/28 px header height is unchanged; the close button keeps its compact 26/22 px
  minimum inside it; the icon slot is not hit-testable.
- **Scroll surfaces.** The 20 px content gutter does not change the scrollbar's 16 px hit target or its
  keyboard behaviour. No gutter removes or adds a focus stop.
- **No clipping.** At 1.5× font scale in compact density the tab header grows above 28 px rather than
  clipping, and the gutter keeps list and dialog content clear of the bar.

## 6. Tests

All new tests are PR-fast headless using the real `ShellWindow`, palette, high-contrast dictionary, and
`ShellStyles`; the screenshot rows are PR-medium. The tab and scroll assertions read realized controls and
bounds, not only `TryGetResource`.

### 6.1 Realized tab parts and geometry

- For the selected and an inactive tab in light, dark, and high contrast:
  - `Border#PART_LayoutRoot.CornerRadius` is `4,4,0,0` and `Border#PART_LayoutRoot.Background` equals
    `Mocassin.TabActiveBackground` / `Mocassin.TabInactiveBackground`;
  - `ContentPresenter#PART_ContentPresenter.Foreground` equals the active/inactive foreground role;
  - the selected tab's `Border#PART_SelectedPipe` is visible, `Background` equals
    `TabItemHeaderSelectedPipeFill` (`FocusBrush`), `Height` is 2, `VerticalAlignment` is `Top`, and
    `Margin` equals `Mocassin.TabIndicatorInset`; it is not visible on the inactive tab;
  - `ContentPresenter#PART_ContentPresenter.Margin` equals `TabItemHeaderMargin` (`8,3`/`6,2`), and
    `TabItem.Padding` is `0`;
  - the selected tab's bottom edge equals the content host's top edge within 1 px (no 2 px gap);
  - the indicator's top edge equals the tab's top edge within 1 px.
- Hover and pressed: move the pointer over an inactive console tab in each theme and assert the realized
  `PART_LayoutRoot.Background` is `HoverBackground`; press and assert `PressedBackground`. The selected tab
  keeps the active fill in both states.
- Focus: focus a tab, build its `FocusAdorner`, and assert a 2 px `Mocassin.FocusBrush` border with the tab
  corner radius; assert the tab selection and the Tab order are unchanged.
- Icon slot: both headers contain a 16 × 16 `TabIconSlot`, it is not visible while `Icon` is null, it is not
  focusable and not hit-testable, and the tab's accessible name is unchanged; a console tab's `Header`
  `ToString()` is still `Console`/`Validation`.
- Equality guards: for each theme, `TabItemHeaderBackgroundSelected` equals `Mocassin.TabActiveBackground`,
  `TabItemHeaderBackgroundUnselected` equals `Mocassin.TabInactiveBackground`, and the foreground keys equal
  the corresponding `Mocassin.Tab*Foreground` roles, so the palette and the realized Fluent keys cannot drift.
- Token retirement: `Mocassin.TabStripBackground` is absent from the palette, high contrast, and the contract
  role table; the contrast test's role dictionary is updated.

### 6.2 Scroll-edge measurements

For every surface in §4.2, at 1000 × 700, at the 900 × 600 window minimum, and at 1.5× font scale in compact
density:

- find the realized `ScrollBar` (`PART_VerticalScrollBar` / `PART_HorizontalScrollBar`) and translate its
  bounds to the window;
- take the content edge from the realized item container (`ListBoxItem`, `TreeViewItem`), the scroll content
  root (`ScrollViewer`), or the `ScrollContentPresenter` padding (`TextBox`);
- assert `verticalBar.Left - contentRight >= 3` and, where a horizontal bar can appear,
  `horizontalBar.Top - contentBottom >= 3`;
- for the `TextBox` and `ComboBox` popup, assert the realized `ScrollContentPresenter.Padding` equals the
  token (a long JSON line is clipped, so a text-edge measurement is not meaningful);
- assert the palette and cheat sheet are actually scrolled (`Extent > Viewport`) so the measurement is not
  vacuous, and assert the palette's right-aligned gesture text stays left of the bar.

### 6.3 Screenshots

- Regenerate and review `light.png`, `dark.png`, `highcontrast.png`, `light-validation.png`,
  `dark-validation.png`, `light-settings.png`, `dark-settings.png`; the tab treatment and the gutters change
  them.
- Add `light-palette.png`, `dark-palette.png` (palette with enough results to scroll, showing the gesture
  text clear of the bar).
- Add `light-cheatsheet.png`, `dark-cheatsheet.png` (scrolled cheat sheet with the right gutter).
- Add `dark-tab-hover.png` (pointer over the inactive console tab, proving the lighter chip and hover fill).
- Every shell capture shows the console strip with one active and one inactive tab, so active/inactive is
  covered in light, dark, and high contrast; the document strip uses the same styles and is covered by the
  realized-part tests and the hover baseline.
- Baseline regeneration is `MOCASSIN_REGENERATE_VISUAL_BASELINES=1 dotnet test ... --filter
  "FullyQualifiedName~ScreenshotTests"`; the 5 % tolerance and Linux-only comparison are unchanged.

### 6.4 Existing tests to update

- `Visual/ThemeContrastTests.cs`: the `Mocassin.TabStripBackground`/`TabInactiveBackground`/
  `TabInactiveForeground` expected values; add the new active/inactive and hover contrast pairs; keep the six
  `SystemAccentColor*` type checks untouched.
- `Visual/AccentDeliveryTests.cs`: the selected-tab pipe test now also asserts the top alignment and inset, not
  only the fill.
- `Visual/AccessibilityTests.cs`: add the tab focus-adorner and icon-slot assertions; the existing
  `Header?.ToString()` console assertion still passes because it reads the data item's `ToString()`.
- `Views/ConsoleTabsTests.cs`: the visible-label helper still finds the first `TextBlock`; assert the leading
  slot is present and collapsed.
- `Views/WorkspaceRenderTests.cs` / `SettingsDialogView`: no behavioural change expected; run them.

## 7. Documentation

- `docs/development/ui-visual-contract.md`: §3.1 (strip surface, inactive/active roles, `TabStripBackground`
  retirement), §3.2 (inactive foreground), §5.1 (new scroll-gutter subsection), §5 (tab geometry tokens),
  §6 (selected/focus states), §7.1 (strip surface), §7.4 (full tab treatment, states, geometry, indicator,
  icon slot, accessibility), §7.5/§8 (shared console treatment, no extra strip padding), §10 (palette/settings/
  dialogs use the gutter), §11.1 (tab text contrast), §11.2/§11.3 (focus ring, names), §11.4 (1.5× growth),
  §13 (verification rows).
- `docs/development/testing-and-parity.md`: the screenshot inventory gains the palette, cheat-sheet, and hover
  baselines; add the realized tab and scroll-edge test rows.
- No user-guide change: the user-facing tab behaviour is unchanged (`workspace.md` already says there is no
  drag-reorder and that the console strip is the card header).
- No ADR, no `contracts/manifest.json`, no interaction-contract, no public API, no new dependency.

## 8. Acceptance criteria

1. Document and console tabs have top-only rounded corners, the active tab shares the content background, and
   there is no gap, border, or colour break between the active tab and the content.
2. The active tab shows a 2 px `FocusBrush` line at its top edge, inset by `Mocassin.TabIndicatorInset`, and
   no line at the bottom.
3. In dark, the inactive tab is `#2D2D30` with `#9D9D9D` text; in light, `#ECECEC` with `#616161` text; in
   high contrast, `#000000` with `#E0E0E0`/`#FFFFFF` text and the `#FFD700` line. Hover and pressed use the
   existing `HoverBackground`/`PressedBackground` values.
4. `Mocassin.TabActiveBackground`/`TabInactiveBackground`/`TabActiveForeground`/`TabInactiveForeground` equal
   the realized `TabItemHeader*` keys per theme; `Mocassin.TabStripBackground` is retired and no page or test
   claims it.
5. A tab shows a visible 2 px focus ring; the tab's accessible name, selection announcement, hit target, and
   Tab order are unchanged.
6. Both item templates reserve the 16 px `TabIconSlot`, collapsed while `Icon` is null, with no accessible
   name and no focus stop; `Icon` exists on both view models; no icon asset or package is added.
7. Every surface in §4.2 keeps its realized content edge at least 3 px clear of the realized scrollbar edge at
   1000 × 700, 900 × 600, and 1.5× compact; the palette and cheat sheet are actually scrolled in the test.
8. The project overview keeps its existing 20 px content margin and satisfies the same measurement; the JSON
   view and ComboBox popup reserve the gutter through the scoped styles.
9. No custom scrollbar or `TabItem` control template is added; `AllowAutoHide` stays `true`; the scrollbar hit
   target and keyboard behaviour are unchanged.
10. The seven existing shell baselines are regenerated and reviewed; the four palette/cheat-sheet and one
    hover baselines are added; the screenshot inventory page is updated.
11. `./tools/verify.sh docs --require` and `./tools/verify.sh policy --require` pass on the docs commit;
    `./tools/verify.sh all --require` passes on the implementation commit.
12. The branch is pushed for owner review; the owner approves one `--no-ff` merge; no step tag, parity row, or
    amendment is created.

## 9. Risks

| Risk | Mitigation |
| --- | --- |
| The stock `TabItem` template cannot move the indicator without a template | The pipe is a template child; a targeted style sets `VerticalAlignment`/`Margin`/`CornerRadius`, and `Padding=0` plus a presenter margin lets it span the tab. Verified by a realized-part test. If the selector does not reach the part, stop and ask before a template. |
| The indicator inset looks wrong or the line clips at the corners | The 4 px inset is a token; the full-width variant is one value. The screenshots decide; both are style-only. |
| The removed 2 px strip gap changes the content position | Expected: the content moves up 2 px. The screenshot baselines are regenerated and reviewed. |
| The inactive chip is too subtle in dark | `#2D2D30` on `#252526` is a 10-unit step; the hover `#37373D` is stronger. The dark hover baseline proves the state. |
| Light inactive text contrast | `#616161` on `#ECECEC` = 5.24:1, tested; the old `#6F6F6F` would be 4.25:1. |
| A new tab style hides the focus ring | The focus adorner is added explicitly and tested; the accent line is not a focus cue. |
| The icon slot changes the layout now | It is collapsed while `Icon` is null; the test asserts no visible tab-width change in this pass. |
| The icon slot changes the accessible name | `AccessibilityView="Raw"`, no name, no text; the tab name test is unchanged. |
| A `ListBox`/`TreeView` gutter shortens the row highlight | Accepted; the gutter is the owner's requirement. Measured against the bar edge, not the highlight width. |
| The ComboBox popup selector does not reach the popup `ScrollViewer` | The test opens a drop-down and reads the realized padding; if it cannot be reached, the stock popup is deferred with a recorded note rather than adding a template. |
| A `TextBox` gutter shifts single-line fields | The style is scoped to the JSON view class; no other `TextBox` carries it. |
| The palette/cheat-sheet captures do not scroll at 700 px height | The test reduces the window to 900 × 600 or uses the palette's `MaxHeight`; the capture asserts `Extent > Viewport` before saving. |
| The 5 % screenshot tolerance hides a regression | Baselines are regenerated from the reviewed tree, not accepted from a failing run; the tab and scroll assertions are separate from the images. |
| Scope creeps into tab-strip scrolling or menu popups | §4.2 lists the exclusions; a new scroll surface is a new branch. |

## 10. Gates and owner decisions

| Gate | Decision |
| --- | --- |
| ADR | **Not required.** Application-local visual tokens and styles; no format, dependency, architecture, public API, or interaction-contract change. |
| `ui-interaction-contract.md` | **No change.** Tab arrow selection, `Tab`/`Enter` behaviour, focus order, and splitter/overlay rules are untouched; the tab focus ring is a visual completion of the existing rule. |
| Control templates | **None.** Stock Fluent `TabItem`/`TabControl`/`ScrollViewer`/`ListBox`/`TreeView`/`TextBox`/`ComboBox`; targeted styles only. |
| New dependency | **None.** No icon package; Phosphor is the later branch. |
| Format / contracts | **None.** No `contracts/manifest.json`, project/package/results/state change. |
| Public API | **None.** The `Icon` properties are on internal view models; no `PublicAPI.*.txt` change. |
| Design review | **Not required.** Bounded two-surface visual fix; the owner fixed the direction, the token values and bounds are testable, and the precedent is the 2026-11-21 visual refresh, which recorded the same conclusion. The planner stops and asks if a template, a custom scrollbar, or a new surface rule becomes necessary. |
| Step tag / parity / amendment | **None.** Out-of-step maintenance under §3.5. |
| Merge | One owner `--no-ff` approval after the local gates and the evidence below. No per-feature CI. |
| Platform runs | Not a merge gate; screenshots are Linux-only, as today. |

Owner confirmations to carry into the merge request:

| ID | Question | Recommended default |
| --- | --- | --- |
| O1 | Keep the strip on the content surface (no separate band), or restore a chrome band behind the tabs so the active tab's top corners show against it? | Keep the content surface (§3.1). The chrome-band variant re-adds `TabStripBackground`, a band delivery (view-level band or `ItemsPanel` background plus a content-surface invariant), and the §3.1/§7.1 surface text; the owner can request it as a follow-up. |
| O2 | Is the 4 px indicator end inset acceptable, or should the 2 px line span the full tab width? | Inset (§3.3); it preserves the rounded corners. One token value changes it. |
| O3 | Should the icon slot be collapsed now (no layout change) or always reserve 16 px? | Collapsed until the Phosphor pass supplies an icon (§3.6). |

Open observation (not fixed here): the contract says the tab strip "scrolls horizontally; it never wraps"
(§7.4), but the Fluent `TabControl` template has no `ScrollViewer` around the `ItemsPresenter`. The claim is
unrealized today. Recording it here keeps the gap visible; tab-strip scrolling is out of this branch.

## 11. In scope and deferred

**In scope**

- Tab geometry, fills, foregrounds, hover/pressed/disabled states, the top indicator, the seam removal, and the
  tab focus ring, for document and console strips.
- Retiring `Mocassin.TabStripBackground` and aligning the `Mocassin.Tab*` roles with the realized Fluent keys.
- The 16 px icon-slot reservation and the `Icon` binding points in both view models.
- The scroll-gutter tokens, classes, the scoped `TextBox` and `ComboBox` popup styles, and the surface changes
  in §4.2.
- The realized-part, geometry, scroll-edge, and screenshot tests; the visual contract and the test-inventory
  page; this record.

**Deferred**

- Phosphor Icons, the explorer tree icons, and any icon asset or package.
- Tab-strip horizontal scrolling/overflow and tab drag-reorder.
- Menu/flyout scroll gutters and any custom scrollbar template.
- Any theme redesign, accent change, density change, or new surface colour beyond §3.2.
- Step 24 editors, the parked command-palette freeze branch, frozen records, and the `main` history.

## 12. Done when

- The tab styles and tokens in §3 are implemented with no control template, and the document and console
  strips both show the rounded chip, the seamless active tab, and the 2 px top line in light, dark, and high
  contrast.
- The `Mocassin.Tab*`/`TabItemHeader*` pairs are equal per theme and `Mocassin.TabStripBackground` is gone from
  the palette, high contrast, the contract, and the tests.
- Both item templates reserve the collapsed `TabIconSlot`; no icon asset or dependency is added.
- Every surface in §4.2 keeps its realized content edge at least 3 px clear of the realized scrollbar edge at
  1000 × 700, 900 × 600, and 1.5× compact.
- The realized-part, geometry, scroll-edge, and screenshot tests pass; the baselines are regenerated and
  reviewed; `testing-and-parity.md` lists them.
- `./tools/verify.sh all --require` passes; the branch is pushed; the owner approves the `--no-ff` merge; no
  step tag is created.
- This record is updated with the implementation commit SHAs, exact commands/results, screenshot list, and any
  deviation.

## 13. Evidence

To be completed by the implementor:

| Field | Value |
| --- | --- |
| Implementation commits | — |
| Full gate | — |
| App tests | — |
| .NET tests | — |
| Screenshots | — |
| `docs`/`policy` on the final tree | — |
| Merge commit | — |
